using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

/// <summary>
/// <seealso cref="Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.Agnostic.Archer"/>
/// </summary>
public static class Archer
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft/*, ModData.Traits.ModName*/);
        
        ModManager.RegisterFeatNameReplacement(
            "MoreDedications.Class.Fighter.PartingShot",
            ModData.FeatNames.PartingShot);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Remaster Archer
        if (ModLoader.GetDedication(Trait.Archer) is { } archDed)
        {
            ModData.FeatNames.ArcherDedication = archDed.FeatName;
            archDed.ReplaceDedicationBehavior(
                Trait.Archer,
                null,
                $$"""
                  You have {{ModData.Tooltips.CommonWeaponFamiliarity("familiarity")}} with all weapons in the {tooltip:criteffect}bow and crossbow{/} weapon groups.
                  """,
                values =>
                {
                    values.Proficiencies.Autoupgrade(
                        [Trait.Martial],
                        [Trait.Advanced, Trait.Bow]);
                    values.Proficiencies.Autoupgrade(
                        [Trait.Simple],
                        [Trait.Martial, Trait.Bow]);
                },
                null);
        }
        
        // Crossbow Ace
        yield return new TrueFeat(
                ModData.FeatNames.CrossbowAceRemastered, 1,
                "Your deep understanding of the crossbow allows you to reload efficiently while moving yourself out of the line of return fire.",
                """
                {b}Requirements{/b} You are wielding a crossbow with reload 1 or higher.

                Either Create a Diversion or Take Cover, then Interact to reload.
                """,
                [Trait.Ranger])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Create a Diversion or Take Cover, then Reload.";
                
                qfFeat.ProvideActionsIntoPossibilitySection = (qfThis, section) =>
                {
                    List<Possibility> possibilities = [];
                    if (section.PossibilitySectionId is not PossibilitySectionId.ItemActions)
                        return possibilities;
                    
                    foreach (Item weapon in qfThis.Owner.HeldItems
                                 .Where(item =>
                                     ItemIsCrossbow(item)
                                     && item.Traits.ContainsOneOf([Trait.Reload1, Trait.Reload2])
                                     && item.WeaponProperties!.RangeIncrement > 0
                                     && item.EphemeralItemProperties.NeedsReload))
                    {
                        CombatAction reload = qfThis.Owner.CreateReload(weapon);
                        reload.WithFullRename(reload.Name.Replace("Reload", "Crossbow Ace"));
                        reload.Description = "{Blue}Create a Diversion or Take Cover.{/Blue}" + "\n\n" + reload.Description;
                        reload.WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, self, _) =>
                        {
                            await self.Battle.GameLoop.OfferOptions2(
                                self,
                                ap =>
                                    ap.CombatAction.ActionId is ActionId.CreateADiversion
                                    || ap.CombatAction.Name.ToLower() == "take cover",
                                true);
                            if (self.Actions.ActionHistoryThisTurn.LastOrDefault() is { Name: "Pass" })
                            {
                                action.RevertRequested = true;
                                action.EffectOnChosenTargets = null;
                            }
                        });
                        possibilities.Add(new ActionPossibility(reload));
                    }

                    return possibilities;
                };
            });
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.CrossbowAceRemastered, Trait.Archer, 4);
        
        // Quick Shot: Add Quick Draw to Archer
        if (ModLoader.SafelyDuplicateAsArchetype(FeatName.QuickDraw, Trait.Archer, 4) is { } qDraw)
            yield return qDraw;

        // Crossbow Terror
        yield return new TrueFeat(
                ModData.FeatNames.CrossbowTerror, 6,
                "Your skill with a crossbow strikes terror into your opponents when you threaten them with the next bolt.",
                "Reload a crossbow, then attempt to Demoralize. You gain a +2 circumstance bonus to this check if you succeeded at a Strike with a crossbow this turn.",
                [])
            .WithAvailableAsArchetypeFeat(Trait.Archer)
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ =>
                    qfFeat.Name!.WithTag("b")
                    + " Reload a crossbow, then Demoralize. You have a +2 circumstance bonus to this check if you succeeded at a crossbow Strike this turn.";
                qfFeat.ProvideActionsIntoPossibilitySection = (qfThis, section) =>
                {
                    List<Possibility> possibilities = [];
                    if (section.PossibilitySectionId is not PossibilitySectionId.ItemActions)
                        return possibilities;
                        
                    foreach (Item weapon in qfThis.Owner.HeldItems
                                 .Where(item =>
                                     ItemIsCrossbow(item)
                                     && item.WeaponProperties!.RangeIncrement > 0
                                     && item.EphemeralItemProperties.NeedsReload))
                    {
                        CombatAction reload = qfThis.Owner.CreateReload(weapon);
                        reload.WithFullRename(reload.Name.Replace("Reload", "Crossbow Terror"));
                        reload.Description += "\n\n{Blue}Attempt to Demoralize. You gain a +2 circumstance bonus to this check if you succeeded at a Strike with a crossbow this turn.{/Blue}";
                        reload.WithEffectOnChosenTargets(async (self, _) =>
                        {
                            CombatAction demoralize = CommonCombatActions.Demoralize(self)
                                .WithActionCost(0);
                            QEffect? plusTwo = null;
                            if (self.Actions.ActionHistoryThisTurn.Any(ca =>
                                    ca.HasTrait(Trait.Strike)
                                    && ca.Item is not null
                                    && ItemIsCrossbow(ca.Item)))
                                plusTwo = new QEffect()
                                {
                                    BonusToSkillChecks = (_, action, _) =>
                                        action == demoralize
                                            ? new Bonus(2, BonusType.Circumstance, "Crossbow terror")
                                            : null,
                                };
                            self.AddQEffect(plusTwo);
                            await self.Battle.GameLoop.FullCast(demoralize);
                            plusTwo?.ExpiresAt = ExpirationCondition.Immediately;
                        });
                        possibilities.Add(new ActionPossibility(reload));
                    }

                    return possibilities;
                };
            });
        
        // Parting Shot
        yield return new TrueFeat(
                ModData.FeatNames.PartingShot, 4,
                "You jump back and fire a quick shot that catches your opponent by surprise.",
                """
                {b}Requirements{/b} You are wielding a loaded ranged weapon or a ranged weapon without reload 1 or reload 2.

                You Step and then make a ranged Strike with the required weapon. Your target is {r:flat-footed}off-guard{/r} against the attack.
                """,
                [Trait.Fighter])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + " Step, then make a ranged Strike with a loaded or reload 0 weapon. They are off-guard against this attack.";
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Ranged)
                        || item.HasTrait(Trait.Thrown))
                        return null;
                    
                    CombatAction basicStrike = qfFeat.Owner.CreateStrike(item)
                        .WithActionCost(0)
                        .WithTargetingTooltip((action, tar, _) =>
                        {
                            tar.AddQEffect(new QEffect()
                            {
                                Name = "[OFF-GUARD TO PARTING SHOT]",
                                ExpiresAt = ExpirationCondition.ExpiresAtEndOfAnyTurn,
                                IsFlatFootedTo = (_, _, strike) =>
                                    strike == action
                                        ? "Parting Shot"
                                        : null,
                            });
                            CheckBreakdown breakdown = CombatActionExecution.BreakdownAttackForTooltip(action, tar);
                            return breakdown.TooltipDescription;
                        });
                    
                    CombatAction partingShot = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(IllustrationName.Walk, item.Illustration),
                            "Parting Shot",
                            [/*ModData.Traits.ModName*/ ModData.ModTrait, Trait.Fighter],
                            StrikeRules.CreateBasicStrikeDescription3(
                                basicStrike.StrikeModifiers,
                                prologueText: "Step.",
                                additionalAttackRollText: "The target is off-guard against this attack."),
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                    new WeaponIsLoadedCreatureTargetingRequirement(item)
                                        .Satisfied(self, self)
                                        .UnusableReason))
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, caster, _) =>
                        {
                            if (!await caster.StepAsync(
                                    "Choose where to Step with Parting Shot.",
                                    allowCancel: true, allowPass: true))
                                action.RevertRequested = true;
                            else
                                await caster.Battle.GameLoop.FullCast(basicStrike);
                        });
                    
                    return partingShot;
                };
            });
        if (ModLoader.SafelyDuplicateAsArchetype(ModData.FeatNames.PartingShot, Trait.Archer, 6) is { } pShot)
            yield return pShot;
        
        // Archer's Aim
        // Update original feat.
        // Adds a stat block description, a targeting tooltip
        TrueFeat archersAim = (AllFeats.GetFeatByFeatName(FeatName.ArchersAim) as TrueFeat)!;
        archersAim.OnCreature = null;
        archersAim.WithPermanentQEffect(qfFeat =>
        {
            qfFeat.AddToOffenseBlock = qfThis =>
                qfThis.Name!.WithTag("b")
                + " Make a ranged bow or crossbow Strike. You gain a +2 circumstance bonus to the attack roll, ignore the concealed condition, and reduce amy hidden flat checks to DC 5.";
            qfFeat.ProvideStrikeModifier = item =>
            {
                if (!item.HasTrait(Trait.Ranged) || !item.HasTrait(Trait.Bow))
                    return null;
                
                CombatAction strike = qfFeat.Owner.CreateStrike(item, -1, new StrikeModifiers()
                    {
                        HuntersAim = true,
                        AdditionalBonusesToAttackRoll = [new Bonus(2, BonusType.Circumstance, "Archer's Aim")],
                        QEffectForStrike = new QEffect() { Id = QEffectId.BlindFight },
                    })
                    .WithIllustration(new SideBySideIllustration(
                        item.Illustration,
                        IllustrationName.TargetSheet))
                    .WithName("Archer's Aim")
                    .WithExtraTrait(0, /*ModData.Traits.ModName*/ ModData.ModTrait)
                    .WithExtraTrait(Trait.Concentrate)
                    .WithActionCost(2)
                    .WithTargetingTooltip((action, target, _) =>
                    {
                        QEffect blindFight = new QEffect()
                        {
                            Name = "[ARCHER'S AIM: BLINDFIGHT TOOLTIP]",
                            Id = QEffectId.BlindFight
                        };
                        action.Owner.AddQEffect(blindFight);
                        CheckBreakdown breakdown = CombatActionExecution.BreakdownAttackForTooltip(action, target);
                        blindFight.ExpiresAt = ExpirationCondition.Immediately;
                        action.Owner.RemoveAllQEffects(qf => qf == blindFight);
                        return breakdown.TooltipDescription;
                    });
                strike.WithFullRename("Archer's Aim");
                strike.Description = StrikeRules.CreateBasicStrikeDescription4(
                    strike.StrikeModifiers,
                    "You gain a +2 circumstance bonus to the attack roll, ignore the target's concealed condition, and reduce any flat checks due to hidden to DC 5.");
                
                return strike;
            };
        });
        
        // Unobstructed Shot
        yield return new TrueFeat(
                ModData.FeatNames.UnobstructedShot, 10,
                "With a quick use of brute force, you remove an obstacle and take a calculated shot as part of the same motion.",
                "Attempt to Shove or Trip one adjacent creature, then make a ranged Strike with a bow or crossbow you're wielding. The Strike is made at the same multiple attack penalty as the Shove or Trip attempt, and this activity counts as one attack when calculating your multiple attack penalty.",
                [Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(Trait.Archer)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + " Shove or Trip, then make a ranged Strike with a bow or crossbow.";
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Bow)
                        && !ItemIsCrossbow(item))
                        return null;
                    
                    CombatAction basicStrike = qfFeat.Owner.CreateStrike(item)
                        .WithActionCost(0);
                    
                    CombatAction obShot = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(IllustrationName.Shove, item.Illustration),
                            "Unobstructed Shot",
                            [ModData.ModTrait, Trait.Flourish],
                            StrikeRules.CreateBasicStrikeDescription3(
                                basicStrike.StrikeModifiers,
                                prologueText: "Attempt to Shove or Trip one adjacent creature.",
                                additionalAttackRollText: "This Strike is made at the same multiple attack penalty as the Shove or Trip.",
                                additionalAftertext: "This activity counts as one attack when calculating your multiple attack penalty."),
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                {
                                    foreach (CombatAction grappleAndTripOption in CombatManeuverPossibilities
                                                 .GetAllShoveGrappleAndTripOptions(self)
                                                 .Where(ca => ca.ActionId is not ActionId.Grapple))
                                    {
                                        if (grappleAndTripOption.CanBeginToUse(self).CanBeUsed)
                                            return null;
                                    }
                                    return "There is no nearby enemy or you can't make any combat maneuver against that enemy.";
                                }))
                        .WithActionCost(2)
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            int map = caster.Actions.AttackedThisManyTimesThisTurn;
                            
                            var poss = CombatManeuverPossibilities
                                .GetAllShoveGrappleAndTripOptions(caster)
                                .Where(ca => ca.ActionId is not ActionId.Grapple);

                            List<Option> options = [new CancelOption(true)];
                            foreach (CombatAction act in poss)
                            {
                                GameLoop.AddDirectUsageOnCreatureOptions(act.WithActionCost(0), options, false);
                            }

                            options = options
                                .Where(opt =>
                                    opt is not CreatureOption cOpt
                                    || cOpt.Creature.IsAdjacentTo(caster))
                                .ToList();

                            if (options.Count < 2)
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            
                            Option chosenOption = (await caster.Battle.SendRequest(
                                new AdvancedRequest(caster, "Choose a creature to Shove or Trip.", options)
                                {
                                    TopBarText = "Choose a creature to Shove or Trip.",
                                    TopBarIcon = (Illustration)IllustrationName.BlackFist
                                })).ChosenOption;

                            if (chosenOption is CancelOption)
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            await chosenOption.Action();

                            caster.Actions.AttackedThisManyTimesThisTurn = map;

                            if (!await caster.Battle.GameLoop.FullCast(basicStrike))
                            {
                                caster.Battle.Log("Unobstructed Shot was converted to a simple maneuver.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                            }

                            caster.Actions.AttackedThisManyTimesThisTurn = map + 1;
                        });
                    
                    return obShot;
                };
            })
            .WithPrerequisite(
                values => values.HasFeat(FeatName.ExpertAthletics),
                "You must be expert in Athletics.");

        // PETR: Higher level Archer feats
        /* Higher level feats
         * @18 (really: 16) Multishot Stance
         * @20 (really: 20) Impossible Volley
         */
    }

    public static bool ItemIsCrossbow(Item crossbow)
    {
        return crossbow.Traits.ContainsOneOf([
            Trait.Crossbow, Trait.HandCrossbow, Trait.HeavyCrossbow, Trait.RepeatingHandCrossbow, Trait.SimpleCrossbow
        ]);
    }
}