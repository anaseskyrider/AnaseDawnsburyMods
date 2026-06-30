using System.Reflection;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
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
    /// <summary>The Raise a Shield CombatAction is improved to work with AC bonuses other than +2, to work with the Hefty trait, removes the action ID for the Devoted Guardian activity, to only disallow the action when the specific shield is already raised instead of any shield, and to enhance the action description.</summary>
    [HarmonyPatch(typeof(Fighter), nameof(Fighter.CreateRaiseShieldCore))]
    internal static class PatchCreateRaiseShieldCore
    {
        internal static void Postfix(Creature self, Item shield, bool devotedGuardian, ref CombatAction __result)
        {
            bool hasShieldBlock = self.HasEffect(QEffectId.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
            int acBonus = (int)CommonShieldRules.GetAC(shield)!; // Suppress. Only gets called on an item that is a shield.
            bool isCoverable = shield.HasTrait(ModData.Traits.CoverShield);
            bool isRaised = self.QEffects.Any(qf =>
                qf.Id is QEffectId.RaisingAShield && qf.Tag == shield);
            int theirBonus = isCoverable ? 2 : 1;

            // Add mod trait
            __result.Traits = new Traits([ModData.ModTrait, ..__result.Traits], __result);
            
            // Address hefty trait
            if (shield.HasTrait(ModData.Traits.Hefty14))
            {
                __result.ActionCost++;
                __result.Traits.Add(ModData.Traits.Hefty14);
            }
            
            // Remove ActionId if Devoted Guardian.
            // It's so that you can't use this when offered to raise a shield.
            if (devotedGuardian)
                __result.ActionId = ActionId.None;
            
            // Enhance targeting.
            // Account for different possible AC bonuses in AI goodness.
            // Add "must not already be raised" requirement.
            __result.Target = devotedGuardian
                ? Target.AdjacentFriend()
                : Target.Self((_, ai) => ai.GainBonusToAC(acBonus))
                    .WithAdditionalRestriction(self2 =>
                        self2.QEffects.Any(qf =>
                            qf.Id is QEffectId.RaisingAShield
                            && qf.Tag == shield)
                            ? "Already raised"
                            : null);
            
            // Update description.
            // Flavor text is more dynamic.
            // Uses cover shield trait instead of tower shield reference.
            // Refers to actual shield AC instead of +2 assumption.
            // Other wording tweaks.
            string flavor = devotedGuardian
                ? "{i}You adopt a wide stance, ready to defend both yourself and your chosen ward.{/i}"
                : $"{{i}}You raise the {shield.Name} you're wielding, readying it to deflect blows.{{/i}}";
            string rules = devotedGuardian
                ? "Choose an adjacent ally. Until the start of your next turn, "
                  + (isRaised
                      ? $"your ally gains a {{Blue}}+{theirBonus}{{/Blue}} circumstance bonus to AC"
                      : acBonus != theirBonus
                          ? $"you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC and the ally gains a {{Blue}}+{theirBonus}{{/Blue}} circumstance bonus to AC"
                          : $"both of you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC")
                  + (hasShieldBlock && !isRaised
                      ? ", and can Shield Block {icon:Reaction} with this shield"
                      : null)
                  + ".\n\nYour ally loses the bonus if they're no longer adjacent to you."
                  + (isRaised
                      ? "\n\n{icon:Action} {Green}(Last action was to Raise this Shield){/Green}"
                      : null)
                : $"Until the start of your next turn, you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC{(hasShieldBlock
                    ? " and can Shield Block {icon:Reaction} with this shield"
                    : "")}.";
            __result.Description = $"{flavor}\n\n{rules}";
        }
    }

    /// <summary>
    /// This function has been altered to work for different bonus shields, plus minor cosmetic improvements; and handling for <see cref="ModData.Traits.CoverShield"/>, not just tower shields.
    /// </summary>
    /// <para>Devoted Guardian's effect tooltip gained some textual enhancements, has CountAsABuff set to true, and works with any cover shield instead of just tower shields.</para>
    /// <para>Shield Warden's reaction prompt is also changed in caption to be Shield Warden.</para>
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
            
            QEffect qfRaised = QEffect.RaisingAShield(shieldBlock, shield)
                .WithName("Shield raised (" + shield.Name + ")")
                .With(qfThis =>
                {
                    // Update the description to reflect this shield.
                    qfThis.Description = qfThis.Description?.Replace("+2", "+" + acBonus);
                    // Update the StateCheck to handle worn shields.
                    Action<QEffect>? oldSC = qfThis.StateCheck;
                    qfThis.StateCheck = qfThis2 =>
                    {
                        // If the shield is worn (always false for non-wearables), don't end
                        if (qfThis2.Tag is Item { IsWorn: true })
                            return;
                        oldSC?.Invoke(qfThis2);
                    };
                    // Associates defensive bonuses to the raised shield
                    qfThis.BonusToDefenses = (qfThis2, attackAction, targetDefense) =>
                    {
                        // Unchanged behavior
                        if (targetDefense != Defense.AC
                            && (!qfThis2.Owner.HasEffect(QEffectId.SparklingTarge)
                                || !qfThis2.Owner.HasEffect(QEffectId.ArcaneCascade)
                                || !targetDefense.IsSavingThrow()
                                || attackAction is not { CountsAsMagical: true })
                            && (!qfThis2.Owner.HasEffect(QEffectId.NecromanticDeflection)
                                || attackAction == null
                                || !attackAction.HasTrait(Trait.Necromancy)
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
                    // If you can block with this shield for any reason, add this reduction reaction
                    if (shieldBlock)
                    {
                        if (caster.HasFeat(FeatName.ShieldWarden))
                            qfThis.AddGrantingOfTechnical(
                                ally =>
                                    ally.FriendOfAndNotSelf(caster) && ally.IsAdjacentTo(caster),
                                qfAlly => qfAlly.YouAreDealtDamageReaction = (qfWard, damageEvent) =>
                                {
                                    ReactionOptions? returns = Fighter.ShieldBlockYouAreDealtDamageReaction(
                                        damageEvent, qfWard.Owner, caster, shield);
                                    // Shield Warden has a new caption.
                                    if (returns?.FirstOrDefault() is { } block)
                                        block.Caption = block.Caption.Replace("Shield Block", "Shield Warden");
                                    return returns;
                                });
                        
                        // Unchanged from base.
                        qfThis.YouAreDealtDamageReaction = (qEffect, damageEvent) =>
                            Fighter.ShieldBlockYouAreDealtDamageReaction(
                                damageEvent, qEffect.Owner, qEffect.Owner, shield);
                    }
                });
            
            // Adds devoted guardian to the target.
            // Bonus amount is accounted for.
            // Guardian caster's name is now blue.
            if (devotedGuardian)
            {
                bool isCoverShield = shield.HasAnyTraits([ModData.Traits.CoverShield, Trait.TowerShield]);
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
    
    /// <summary>The Shield Block reaction now keys into an executed CombatAction, with prettier UI prompting and stat block displays.</summary>
    [HarmonyPatch(typeof(Fighter), nameof(Fighter.ShieldBlockYouAreDealtDamageReaction))]
    internal static class PatchShieldBlockDamageReaction
    {
        internal static void Postfix(
            DamageEvent damageEvent,
            Creature targetedCreature,
            Creature blockingCreature,
            Item shield,
            ref ReactionOptions? __result)
        {
            if (__result?.FirstOrDefault() is not { } block)
                return;
            
            string preventWhat;
            int preventHowMuch;
            if (block.EffectSummary?.Contains("all") ?? false)
            {
                preventWhat = "{b}all{/b}";
                preventHowMuch = damageEvent.TotalResolvedDamage;
            }
            else
            {
                // Get bonus hardness from modded content and add it on top
                int bonus = CommonShieldRules.GetShieldBlockHardnessBonuses(
                    damageEvent.Source,
                    damageEvent,
                    targetedCreature,
                    blockingCreature);
                string prevent = block.EffectSummary?
                    .Replace("Prevent ", "")
                    .Replace("{b}", "")
                    .Replace("{/b}", "")
                    .Replace(" of this damage.", "") ?? "0";
                preventHowMuch = int.TryParse(prevent, out int result) ? (result + bonus) : 0;
                preventWhat = S.AllOrNumber(preventHowMuch, damageEvent.TotalResolvedDamage);
            }

            CombatAction displayReaction = CommonShieldRules.ShieldBlockAction(
                damageEvent,
                targetedCreature,
                blockingCreature,
                shield,
                shield.Hardness,
                preventHowMuch);
            
            // If Targe response, then add in a note about triggering Targe's benefits.
            string whatDamage = CommonShieldRules.DoesSparklingTargeShieldBlockApply(damageEvent.CombatAction, blockingCreature)
                ? "{Blue}magical{/Blue} damage"
                : "damage";

            // Replace with a combat action display instead, for UEX.
            ReactionOption reaction = ReactionOption.WrapFullcast(
                    displayReaction,
                    $"{shield.Illustration.IllustrationAsIconString} Prevent {preventWhat} of this {whatDamage}.");
            
            __result = reaction;
        }
    }

    /// <summary>
    /// The ShieldBlock ability now has a stat block description.
    /// </summary>
    [HarmonyPatch(typeof(QEffect), nameof(QEffect.ShieldBlock))]
    internal static class PatchShieldBlock
    {
        internal static void Postfix(ref QEffect __result)
        {
            __result.Description = "If you take physical damage while a shield is raised, you can block with it to reduce the damage.";
        }
    }

    /// <summary>
    /// Reactive Shield now allows you to specify which shield to raise if you have more than one option. Includes enhanced log information.
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
                    question += $"{action.Owner.ToColoredName()} is about to hit you with ";
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
                    """
                    {i}You can snap your shield into place just as you would take a blow, avoiding the hit at the last second.{/i}

                    If you'd be hit by a melee Strike, you immediately Raise a Shield as a reaction.
                    """,
                    new Traits([..AllFeats.GetFeatByFeatName(FeatName.ReactiveShield).Traits, ModData.Traits.ReactiveAction]));
                
                Fighter.RaiseShield(defender, chosenShield, defender, false);
                
                return true;
            };
        }
    }

    /// <summary>
    /// Aggressive Block now uses AfterYouTakeActionReaction on the expectation that there is a Shield Block CombatAction that is being executed (change made by this mod).
    /// </summary>
    /// <remarks>
    /// This change also completely avoids using <see cref="Doorwarden.CreateAggressiveBlockTemporaryQEffect"/>.
    /// </remarks>
    [HarmonyPatch(typeof(Doorwarden), nameof(Doorwarden.CreateAggressiveBlock))]
    internal static class PatchAggressiveBlockPopup
    {
        internal static void Postfix(ref QEffect __result)
        {
            __result.WhenYouUseShieldBlock = null;
            
            __result.AfterYouTakeActionReaction = (qfThis, action) =>
            {
                if (!action.HasTrait(Trait.ShieldBlock)
                    || action.Tag is not DamageEvent dEvent
                    || action.Item is not {} shield)
                    return null;

                Creature defender = action.Owner;
                Creature attacker = dEvent.Source;
                
                if (attacker.Space.Size > defender.Space.Size
                    || !defender.IsAdjacentTo(attacker))
                    return null;
                
                CombatAction shoveAct = new CombatAction(
                        defender,
                        IllustrationName.Shove,
                        "Aggressive Block",
                        [ModData.ModTrait, Trait.Fighter],
                        """
                        {i}You push back as you block the attack, knocking your foe away or off balance.{/i}

                        When you use the Shield Block reaction against an attack of an adjacent enemy of your size or smaller, you can choose to automatically Shove that enemy 5 feet. If it can't be pushed away, it's instead flat-footed until the start of your next turn.
                        """,
                        Target.Self())
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (aggroAct, caster, _, _) =>
                    {
                        // Delay shove until after damage is taken.
                        attacker.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                        {
                            AfterYouTakeAction = async (_, _) =>
                            {
                                Tile previousPosition = attacker.Space.TopLeftTile;

                                CombatAction shove = CombatManeuverPossibilities
                                    .CreateShoveAction(defender, shield)
                                    .WithActionCost(0)
                                    .WithActiveRollSpecification(null)
                                    .With(ca =>
                                    {
                                        // Remove free hand requirement
                                        ca.Target = Target.Reach(shield)
                                            .WithAdditionalConditionOnTargetCreature(
                                                new TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement());
                                
                                        // Automatic success result
                                        var oldEffect = ca.EffectOnOneTarget;
                                        ca.EffectOnOneTarget = async (shove, caster2, target2, _) =>
                                            await oldEffect?.Invoke(shove, caster2, target2, CheckResult.Success)!;
                                    });
                                await caster.Battle.GameLoop.FullCast(shove, ChosenTargets.CreateSingleTarget(attacker));
                
                                if (ReferenceEquals(previousPosition, attacker.Space.TopLeftTile))
                                    attacker.AddQEffect(QEffect.FlatFooted("Aggressive Block")
                                        .WithExpirationAtStartOfSourcesTurn(caster, 1));
                            }
                        });
                    });

                ReactionOption reactOpt = ReactionOption.WrapFullcastWithChosenTargets(
                        shoveAct,
                        ChosenTargets.CreateSingleTarget(attacker),
                        $"Shove {attacker.ToColoredName()} 5 feet away (automatic success), or knock flat-footed if they can't be moved.")
                    .WithIsFreeAction();

                return reactOpt;
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