
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
            // Uses the feat ID as an identifier instead of the QEffect Id.
            bool hasShieldBlock = self.HasFeat(FeatName.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
            
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
    /// The ShieldBlock ability now always contains YouAreDealtDamage information, requiring you to have a shield raised, and consolidating multiple raised shields to a single option if you have multiple. Does not rely on the RaiseShield action to handle anything beyond adding a single effect.
    /// </summary>
    [HarmonyPatch(typeof(QEffect), nameof(QEffect.ShieldBlock))]
    internal static class PatchShieldBlock
    {
        internal static void Postfix(ref QEffect __result)
        {
            // qfThis.Owner is the creature receiving damage reduction.
            // defender is the creature reducing the damage.
            // ShieldWarden passes the shield-user as the defender to the effect-owner.
            __result.YouAreDealtDamage = async (qfThis, attacker, damageStuff, defender) =>
            {
                // Extra handler, since this effect is now always listening
                if (!qfThis.Owner.HasEffect(QEffectId.RaisingAShield))
                    return null;
                
                // Still performs standard checks for standard shield blocking
                if ((!damageStuff.Kind.IsPhysical()
                     || damageStuff.Power == null
                     || !damageStuff.Power.HasTrait(Trait.Attack))
                    && !Magus.DoesSparklingTargeShieldBlockApply(damageStuff, defender))
                    return null;

                // Uses async function to pick just one shield to block with.
                return await CommonShieldRules.OfferAndReactWithShieldBlock(attacker, defender, damageStuff, qfThis.Owner);
            };
        }
    }

    /// <summary>
    /// Improves the behavior of Reactive Shield. Now checks against other circumstances bonuses to see if raising a shield would downgrade a threshold. Now raises a shield properly, allowing you to Shield Block (if you have an extra reaction to do so). Includes enhanced log information.
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
                
                // TODO: nat-20s still trigger (and don't help sometimes)
                
                Creature defender = qfThis.Owner;
                Creature attacker = action.Owner;
                
                int highestCircumstance = action.ActiveRollSpecification.TaggedDetermineDC.CalculatedNumberProducer
                    .Invoke(action, attacker, qfThis.Owner)
                    .Bonuses
                    .Where(bonus => bonus is { BonusType: BonusType.Circumstance, Amount: > 0 })
                    .MaxBy(bonus => bonus?.Amount)
                    ?.Amount ?? 0; // Find the highest circumstance bonuses, if any
                
                List<Item> raisableShields = CommonShieldRules
                    .GetWieldedShields(defender)
                    .Except(CommonShieldRules.GetRaisedShields(defender))
                    .Where(shield =>
                    {
                        if (CommonShieldRules.GetAC(shield) is not { } acBonus)
                            return false;
                        
                        int threshold = Math.Max(0, acBonus - highestCircumstance); // Find threshold

                        if (threshold <= 0
                            || breakdownResult.ThresholdToDowngrade > threshold)
                            return false;

                        return true;
                    })
                    .ToList();

                if (raisableShields.Count == 0)
                    return false;
                
                CheckResult input = breakdownResult.CheckResult - 1;
                if (!await defender.Battle.AskToUseReaction(
                        defender,
                        $"{{b}}Reactive Shield{{/b}} {{icon:Reaction}}\nYou're about to be hit by {{Blue}}{action.Name}{{/Blue}}.\nUse reactive shield to Raise a Shield and downgrade the {breakdownResult.CheckResult.HumanizeLowerCase2()} into a {input.HumanizeLowerCase2()}?"))
                    return false;
                    
                // Custom overhead
                qfThis.Owner.Overhead(
                    "reactive shield",
                    Color.Lime,
                    defender + " uses {b}Reactive Shield{/b}.",
                    "Reactive Shield {icon:Reaction}",
                    "{i}You can snap your shield into place just as you would take a blow, avoiding the hit at the last second.{/i}\n\nIf you'd be hit by a melee Strike, you immediately Raise a Shield as a reaction.",
                    new Traits([..AllFeats.GetFeatByFeatName(FeatName.ReactiveShield).Traits, ModData.Traits.ReactiveAction, Trait.DoNotShowOverheadOfActionName, Trait.DoNotShowInCombatLog]));
                
                await CommonShieldRules.OfferToRaiseAShield(
                    qfThis.Owner,
                    raiseAction =>
                        raiseAction.Item is not null && raisableShields.Contains(raiseAction.Item));
                
                return true;
            };
        }
    }
    
    // TODO: ^ Magus Emergency Targe

    // TODO: Delay execution for other actions such as Disarming Block
    /// <summary>Adjusts Aggressive Block to provide a shove sound effect and a prettier prompt.</summary>
    [HarmonyPatch(typeof(Doorwarden), nameof(Doorwarden.CreateAggressiveBlockTemporaryQEffect))]
    internal static class PatchAggressiveBlock
    {
        internal static void Postfix(Creature attacker, Creature defender, ref QEffect __result)
        {
            __result.AfterYouTakeAction = async (qfThis, action) =>
            {
                if (!await defender.Battle.AskForConfirmation(
                        defender,
                        IllustrationName.SteelShield,
                        $"{{b}}Aggressive Block{{/b}} {{icon:FreeAction}}\nYou just used Shield Block. Push {{Blue}}{attacker}{{/Blue}} 5 feet away from you? {{i}}(They could become flat-footed if they cannot be moved.){{/i}}", // Pretties the prompt
                        "Push"))
                    return;
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
    
    /// Improves the item description of shields.
    [HarmonyPatch(typeof(RulesBlock), nameof(RulesBlock.GetItemDescriptionWithoutUsability))]
    internal static class PatchShieldDescriptions
    {
        internal static void Postfix(Item item, ref string __result)
        {
            // Formatted hardness
            string hardness = "{b}Hardness{/b} " + item.Hardness + "\n";
            
            // If the item has no hardness, skip.
            int index = __result.IndexOf(hardness);
            if (index == -1)
                return;

            // Formatted AC
            int? acBonus = CommonShieldRules.GetAC(item); // Suppressed. Would never execute unless it was a shield.
            string? acString = acBonus != null ? "{b}AC{/b} +" + acBonus + (item.HasTrait(ModData.Traits.CoverShield) ? " (+4)" : null) + "\n" : null;
            
            // Formatted speed penalty
            int speedPenalty = item.HasTrait(ModData.Traits.FortressShield)
                ? -2
                : item.HasTrait(Trait.TowerShield)
                    ? -1
                    : 0;
            string? speedString = speedPenalty < 0
                ? "{b}Speed Penalty{/b} " + speedPenalty*5 + " ft.\n"
                : null;

            // New format
            string[] details = [];
            if (acString != null)
                details = details.Append(acString).ToArray();
            details = details.Append(hardness).ToArray();
            if (speedString != null)
                details = details.Append(speedString).ToArray();

            __result = __result.Replace(hardness, details.Join(null, ""));
        }
    }
}