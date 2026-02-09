
using System.Reflection;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Monsters.L5;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreShields;

/// <summary>
/// Uses Harmony to overhaul internal behavior related to raising a shield, shield blocking, and more. Listed alterations are as follows:
/// </summary>
public static class ShieldPatches
{
    /// <summary>Possibilities now includes the ability to get a worn shield, not just a held shield.</summary>
    [HarmonyPatch(
        typeof(Possibilities),
        nameof(Possibilities.Create),
        [typeof(Creature), typeof(PossibilitiesRegenerationSpecifics)])]
    internal static class PatchPossibilitiesCreation
    {
        internal static void Postfix(Creature self, Possibilities __instance, ref Possibilities __result)
        {
            // TODO: Reorder list if possible.
            PossibilitySection? mainActions = __result.Sections.FirstOrDefault(sect =>
                sect.PossibilitySectionId == PossibilitySectionId.MainActions);
            PossibilitySection? itemActions = __result.Sections.FirstOrDefault(sect =>
                sect.PossibilitySectionId == PossibilitySectionId.ItemActions);

            Traverse TravPoss = Traverse.Create(typeof(Possibilities));
            
            foreach (Item shield in CommonShieldRules.GetWieldedShields(self))
            {
                // Handle worn shields
                if (shield.HasTrait(Trait.Worn))
                {
                    // Shield bash
                    Traverse? CreateStrike = TravPoss.Method(
                        "CreateItemStrikePossibility",
                        new Type[] { typeof(Creature), typeof(Item) });
                    if (shield.HasTrait(Trait.Weapon)
                        && shield.HasTrait(Trait.Melee)
                        && (self.HasFreeHand
                            || self.HeldItems.Any(item =>
                                !(item.HasTrait(Trait.Weapon) || item.HasTrait(Trait.Grapplee)))))
                    {
                        Possibility shieldBash = (Possibility)CreateStrike.GetValue(self, shield);
                        mainActions?.AddPossibility(shieldBash);
                        mainActions?.CollapseReadyPossibilities();
                    }
                
                    // Raise shield
                    // Hide only if this specific shield is being raised
                    if (self.QEffects.Any(qf =>
                            qf.Id is QEffectId.RaisingAShield
                            && qf.Tag == shield))
                        continue;
                    Possibility raiseShield = Fighter.CreateRaiseShield(self, shield);
                    itemActions?.AddPossibility(raiseShield);
                    itemActions?.CollapseReadyPossibilities();
                    var AddCustom = TravPoss.Method(
                        "AddIntoCustomSections",
                        new Type[] { typeof(List<PossibilitySection>), typeof(Creature) });
                    if (raiseShield is SubmenuPossibility shieldMenu)
                        AddCustom.GetValue(shieldMenu.Subsections, self);
                }
                // Readd shields back to the UI for the ones specifically not being raised
                else
                {
                    // Add raise shield only if shields are raised but not this specific shield
                    if (self.QEffects
                            .Where(qf => qf.Id is QEffectId.RaisingAShield)
                            .ToList()
                            is not { Count: > 0 } raisedShields
                        || raisedShields.Any(qf => qf.Tag == shield))
                        continue;
                    
                    Possibility raiseShield = Fighter.CreateRaiseShield(self, shield);
                    itemActions?.AddPossibility(raiseShield);
                    itemActions?.CollapseReadyPossibilities();
                    var AddCustom = TravPoss.Method(
                        "AddIntoCustomSections",
                        new Type[] { typeof(List<PossibilitySection>), typeof(Creature) });
                    if (raiseShield is SubmenuPossibility shieldMenu)
                        AddCustom.GetValue(shieldMenu.Subsections, self);
                }
            }
        }
    }
    
    /// <summary>Raise a Shield is now a dramatically simpler action that flexibly becomes a submenu with the <see cref="ModData.Traits.ShieldActionFeat"/> trait.</summary>
    [HarmonyPatch(typeof(Fighter), nameof(Fighter.CreateRaiseShield))]
    internal static class PatchCreateRaiseShield
    {
        internal static bool Prefix(Creature self, Item shield, ref Possibility __result)
        {
            bool hasShieldBlock = self.HasEffect(QEffectId.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
            
            // Create action
            CombatAction raiseShield = CommonShieldRules.CreateRaiseShieldCore(self, shield, hasShieldBlock);
            Possibility possibleShield = new ActionPossibility(raiseShield)
                .WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ITEM_IN_HAND);
            
            // Return possibility of action
            // Create submenu if any feat has ShieldActionFeat trait (those feats are now responsible for inserting their actions into the submenu).
            if (self.PersistentCharacterSheet?.Calculated.AllFeats.Any(ft => ft.HasTrait(ModData.Traits.ShieldActionFeat)) ?? false)
                __result = new SubmenuPossibility(shield.Illustration, "Raise shield")
                {
                    SpellIfAny = raiseShield,
                    Subsections = [
                        new PossibilitySection("Raise shield")
                        {
                            Possibilities = [possibleShield]
                        }
                    ],
                    PossibilityGroup = Constants.POSSIBILITY_GROUP_ITEM_IN_HAND,
                };
            else
                __result = possibleShield;
            
            // Always overwrite the function.
            return false;
        }
    }

    /// <summary>
    /// This function has been altered to track which shield is associated with this effect, while removing the behavior of adding YouAreDealtDamage functionality to it.
    /// </summary>
    /// <para>Devoted Guardian's effect tooltip gained some textual enhancements, has CountAsABuff set to true, and works with any cover shield instead of just tower shields.</para>
    /// <seealso cref="PatchShieldBlock"/>
    [HarmonyPatch(typeof(Fighter), nameof(Fighter.RaiseShield))]
    internal static class PatchRaiseShieldExecution
    {
        internal static bool Prefix(
            Creature caster,
            Item shield,
            Creature target,
            bool devotedGuardian)
        {
            if (CommonShieldRules.GetAC(shield) is not {} acBonus)
                throw new ArgumentException("Cannot get AC bonus from this item. See CommonShieldRules.GetAC().", nameof(shield));
            
            bool shieldBlock = caster.HasEffect(QEffectId.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
            
            QEffect qfRaised = QEffect.RaisingAShield(shieldBlock)
                .With(qfThis =>
                {
                    qfThis.Name += " (" + shield.Name + ")";
                    // Closely associate this effect with a shield.
                    qfThis.Tag = shield;
                    // Update the description to reflect this shield.
                    qfThis.Description = qfThis.Description?.Replace("+2", "+" + acBonus);
                    // Replace state check to end this specific effect when we no longer possess this specific shield.
                    qfThis.StateCheck = qfThis2 =>
                    {
                        if (qfThis2.Tag is not Item tagShield ||
                            !CommonShieldRules.IsShieldWielded(qfThis2.Owner, tagShield))
                            qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                    };
                    // Associates defensive bonuses to the raised shield
                    qfThis.BonusToDefenses = (qfThis2, attackAction, targetDefense) =>
                    {
                        // Unchanged behavior
                        if (targetDefense != Defense.AC
                            && (!qfThis2.Owner.HasEffect(QEffectId.SparklingTarge)
                                || !qfThis2.Owner.HasEffect(QEffectId.ArcaneCascade)
                                || !targetDefense.IsSavingThrow()
                                || attackAction == null
                                || !attackAction.HasTrait(Trait.Spell)))
                            return null;
                    
                        // Gets shield associated with effect
                        if ((qfThis2.Tag as Item) is not { } shield2
                            || CommonShieldRules.GetAC(shield2) is not { } shieldAC)
                            return null;
                
                        return shield2.HasTrait(ModData.Traits.CoverShield) && qfThis2.Owner.HasEffect(QEffectId.TakingCover)
                            ? new Bonus(4, BonusType.Circumstance, "raised shield in cover")
                            : new Bonus(shieldAC, BonusType.Circumstance, "raised shield");

                    };
                });
            
            // Adds devoted guardian to the target
            if (devotedGuardian)
            {
                bool isCoverShield = shield.HasAnyTraits(ModData.Traits.CoverShield!, Trait.TowerShield!);
                int bonus = isCoverShield ? 2 : 1;
                target.AddQEffect(new QEffect(
                    "Devoted Guardian",
                    $"You have a +{bonus} circumstance bonus to AC as long as you're adjacent to {{Blue}}{caster}{{/Blue}}.",
                    ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                    caster,
                    shield.Illustration)
                {
                    CountsAsABuff = true,
                    BonusToDefenses = (_, _, defense) =>
                        defense != Defense.AC
                            ? null
                            : new Bonus(bonus, BonusType.Circumstance, "Devoted Guardian"),
                    StateCheck = qfSelf =>
                    {
                        if (caster.IsAdjacentTo(qfSelf.Owner))
                            return;
                        qfSelf.ExpiresAt = ExpirationCondition.Immediately;
                    }
                });
            }
            
            // Only raise once
            if (!caster.QEffects.Any(qf => qf.Id == QEffectId.RaisingAShield && qf.Tag == shield))
                caster.AddQEffect(qfRaised);
            
            // Always overwrite the function.
            return false;
        }
    }

    /// <summary>
    /// The ShieldBlock ability provides a wood-impact sound when blocking the attack, and always contains YouAreDealtDamage functionality (requires you to have a shield raised, consolidates multiple raised shields to a single prompt if you have multiple). Does not rely on any action or function to add anything beyond an effect with the <see cref="QEffectId.RaisingAShield"/> id and an Item in its .Tag field.
    /// </summary>
    [HarmonyPatch(typeof(QEffect), nameof(QEffect.ShieldBlock))]
    internal static class PatchShieldBlock
    {
        internal static void Postfix(ref QEffect __result)
        {
            __result.WhenYouUseShieldBlock = async (_, _, _, _) =>
                Sfxs.Play(ModData.SfxNames.ShieldBlockWooodenImpact);
            
            // qfThis.Owner is the creature receiving damage reduction.
            // defender is the creature reducing the damage.
            // ShieldWarden passes the shield-user as the defender to the effect-owner.
            __result.YouAreDealtDamage = async (qfThis, attacker, damageStuff, defender) =>
            {
                // Still performs standard checks for standard shield blocking
                if ((!damageStuff.Kind.IsPhysical()
                     || damageStuff.Power == null
                     || !damageStuff.Power.HasTrait(Trait.Attack))
                    && !Magus.DoesSparklingTargeShieldBlockApply(damageStuff, defender))
                    return null;

                /* Does not look for a raised shield
                 * OfferAndMakeShieldBlock returns null if there's no shield
                 */
                
                // Uses async function to pick just one shield to block with.
                return await CommonShieldRules.OfferAndMakeShieldBlock(attacker, defender, damageStuff, qfThis.Owner);
            };
        }
    }

    /// <summary>
    /// Improves the behavior of Reactive Shield. Now allows you to specify which shield to raise if you have more than one option. Includes enhanced log information.
    /// </summary>
    [HarmonyPatch(typeof(QEffect), nameof(QEffect.ReactiveShield))]
    internal static class PatchReactiveShield
    {
        internal static void Postfix(ref QEffect __result)
        {
            __result.YouAreTargetedByARoll = async (qfThis, action, breakdownResult) =>
            {
                if (breakdownResult.CheckResult < CheckResult.Success
                    || !action.HasTrait(Trait.Strike)
                    || action.ActiveRollSpecification == null
                    || !action.HasTrait(Trait.Melee)) // Basic validity check
                    return false;
                
                Creature defender = qfThis.Owner;
                int threshold = breakdownResult.GetCircumstanceBonusThresholdNeededToDowngrade();

                if (CommonShieldRules.GetWieldedShields(defender) is not { Count: > 0 } shields)
                    return false;

                List<Item> raisableShields = shields
                    .Except(CommonShieldRules.GetRaisedShields(defender))
                    .ToList();
                List<Item> downgradeShields = raisableShields
                    .Where(shield =>
                        threshold <= CommonShieldRules.GetAC(shield))
                    .ToList();
                bool canBeDowngraded = downgradeShields.Count > 0;
                List<Item> shieldOptions = canBeDowngraded
                    ? downgradeShields
                    : raisableShields;

                if (shieldOptions.Count == 0)
                    return false;

                // Prettied text
                string question = "{b}Reactive Shield{/b} {icon:Reaction}\n";
                if (action.Owner == defender.Battle.Pseudocreature)
                    question += "You're about to be hit by ";
                else
                    question += "{Blue}" + action.Owner + "{/Blue} is about to hit you with ";
                question += "{Blue}" + action.Name + "{/Blue}.\nRaise a Shield";
                if (canBeDowngraded)
                    question += $" and downgrade the {breakdownResult.CheckResult.Greenify()} into a {(breakdownResult.CheckResult - 1).Greenify()}?";
                // If you have a bonus reaction you could use
                else if (defender.Actions.DetermineReactionToUse(
                             question + "? {i}(You will still be hit but you'll be able to Shield Block.){/i}",
                             [Trait.ShieldBlock]) is not null)
                    question += "? {i}(You will still be hit but you'll be able to Shield Block.){/i}";
                else
                    return false;
                
                string[] stringOptions = shieldOptions
                    .Select(shield =>
                        shield.Illustration.IllustrationAsIconString + shield.Name)
                    .ToArray();
                
                if (await defender.Battle.AskToUseReaction(
                        defender,
                        question,
                        ModData.Illustrations.ReactiveShield, // New icon
                        stringOptions) is not {} chosenIndex) // Lets you choose which shield to raise
                    return false;
                
                Item chosenShield = shieldOptions[chosenIndex];
                    
                // Custom overhead
                qfThis.Owner.Overhead(
                    "reactive shield",
                    Color.Lime,
                    defender + " uses {b}Reactive Shield{/b}.",
                    "Reactive Shield {icon:Reaction}",
                    "{i}You can snap your shield into place just as you would take a blow, avoiding the hit at the last second.{/i}\n\nIf you'd be hit by a melee Strike, you immediately Raise a Shield as a reaction.",
                    new Traits([..AllFeats.GetFeatByFeatName(FeatName.ReactiveShield).Traits, ModData.Traits.ReactiveAction]));
                
                Fighter.RaiseShield(defender, chosenShield, defender, false);
                /*await CommonShieldRules.OfferToRaiseAShield(
                    qfThis.Owner,
                    raiseAction => raiseAction.Item == chosenShield)*/;
                
                return true;
            };
        }
    }

    // TODO: Delay execution for other actions such as Disarming Block
    /// <summary>Adjusts Aggressive Block to provide a shove sound effect, a prettier prompt, and combat logging.</summary>
    [HarmonyPatch(typeof(Doorwarden), nameof(Doorwarden.CreateAggressiveBlockTemporaryQEffect))]
    internal static class PatchAggressiveBlock
    {
        internal static void Postfix(Creature attacker, Creature defender, ref QEffect __result)
        {
            __result.AfterYouTakeAction = async (qfThis, action) =>
            {
                // Pretties the reaction prompt
                if (!await defender.Battle.AskForConfirmation(
                        defender,
                        IllustrationName.SteelShield,
                        $"{{b}}Aggressive Block{{/b}} {{icon:FreeAction}}\nYou just used Shield Block. Push {{Blue}}{attacker}{{/Blue}} 5 feet away from you? {{i}}(They could become flat-footed if they cannot be moved.){{/i}}", // Pretties the prompt
                        "Push"))
                    return;
                
                // Adds some combat logging for this ability
                defender.Overhead(
                    "Aggressive Block",
                    Color.Black,
                    "{DarkBlue}{b}" + defender + "{/b}{/DarkBlue} uses {b}Aggressive Block{/b} {icon:FreeAction} against {DarkBlue}{b}" + attacker + "{/b}{/DarkBlue}",
                    "Aggressive Block {icon:FreeAction}",
                    "{i}You push back as you block the attack, knocking your foe away or off balance.{/i}\n\nWhen you use the Shield Block reaction against an attack of an adjacent enemy, you can choose to push that enemy 5 feet. If it can't be pushed away, it's instead flat-footed until the start of your next turn.",
                    new Traits([..AllFeats.GetFeatByFeatName(FeatName.AggressiveBlock).Traits, ModData.Traits.ReactiveAction]));
                
                Tile previousPosition = attacker.Space.TopLeftTile;
                
                Sfxs.Play(SfxName.Shove); // Adds shove sound
                
                await defender.PushCreature(attacker, 1);
                if (ReferenceEquals(previousPosition, attacker.Space.TopLeftTile))
                    attacker.AddQEffect(QEffect.FlatFooted("Aggressive Block")
                        .WithExpirationAtStartOfSourcesTurn(defender, 1));
            };
        }
    }

    /// Speed calculation now uses the worst of Fortress Shield or Tower Shield.
    [HarmonyPatch(typeof(Creature), nameof(Creature.RecalculateLandSpeedAndInitiative))]
    internal static class PatchTowerShieldSpeedPenalty
    {
        internal static void Postfix(Creature __instance/*, ref int ___Speed*/)
        {
            // Harmony Traverse causes errors with Thaumaturge's Mirror Reflection which is a subclass of Creature.
            // Old code will be kept for austerity
            
            PropertyInfo? Speed = typeof(Creature).GetProperty("Speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            //Traverse Speed = Traverse.Create(__instance).Property("Speed");

            if (Speed is null)
                return;
            
            bool unburdenedIron = __instance.HasEffect(QEffectId.UnburdenedIron);
            bool hasTowerShield = __instance.HeldItems.Any(itm => itm.HasTrait(Trait.TowerShield));
            bool hasFortressShield = __instance.HeldItems.Any(itm => itm.HasTrait(ModData.Traits.FortressShield));
            
            int worstPenalty = hasFortressShield ? -2 : hasTowerShield ? -1 : 0;
            int finalPenalty = unburdenedIron ? Math.Min(worstPenalty+1, 0) : worstPenalty;

            /*if (hasTowerShield && !unburdenedIron && Speed.GetValue() is int value1)
                Speed.SetValue(value1 + 1); // reverse the original Tower Shield penalty
            if (Speed.GetValue() is int value2)
                Speed.SetValue(value2 + finalPenalty); // Apply final penalty*/
            
            if (hasTowerShield && !unburdenedIron && Speed.GetValue(__instance) is int value1)
                Speed.SetValue(__instance, value1 + 1); // reverse the original Tower Shield penalty
            if (Speed.GetValue(__instance) is int value2)
                Speed.SetValue(__instance, value2 + finalPenalty); // Apply final penalty
        }
    }
}