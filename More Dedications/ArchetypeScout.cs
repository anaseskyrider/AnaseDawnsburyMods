using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeScout
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        const string explorationModName = "Exploration Activities";
        
        yield return new TrueFeat(
                ModData.FeatNames.ScoutsWarning,
                4,
                "You visually or audibly warn your allies of danger.",
                "Your allies (but not you) gain a +1 circumstance bonus to their initiative rolls.",
                [ModData.Traits.MoreDedications, Trait.Ranger, Trait.Rogue])
            .WithPermanentQEffect(
                "Your allies gain a +1 circumstance bonus to their initiative rolls.",
                qfFeat =>
                {
                    qfFeat.StartOfCombat = async qfThis =>
                    {
                        foreach (Creature ally in qfThis.Owner.Battle.AllCreatures.Where(cr =>
                                     cr.FriendOfAndNotSelf(qfThis.Owner)))
                        {
                            ally.AddQEffect(new QEffect()
                            {
                                Name = "Scout's Warning",
                                BonusToInitiative = qfThis2 =>
                                    new Bonus(1, BonusType.Circumstance, "Scout's warning"),
                            });
                        }
                    };
                });
        
        // Load if SilchasRuin's Exploration Activities mod is loaded
        /*if (!ModManager.TryParse("ExplorationActivity", out Trait explorationActivity))
            return;*/

        Feat scoutDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.ScoutArchetype,
                "You're an expert in espionage and reconnaissance, able to skulk silently through the wilderness to gather intelligence, sneak through enemy lines to report to your comrades, or suddenly and decisively strike your foes. Your skills ease the difficulty of travel for you and your companions and keep you all on guard when you're approaching danger.",
                "You gain the Scout's Warning ranger feat.\n\n" + ModData.Illustrations.DawnsburySun.IllustrationAsIconString + " {b}Modding{/b} If the {i}"+explorationModName+"{/i} mod is installed, you gain the following benefit: When you're using the Scout exploration activity, you grant your allies a +2 circumstance bonus to their initiative rolls instead of a +1 circumstance bonus.")
            .WithPrerequisite(values =>
                values.HasFeat(FeatName.Stealth) && values.HasFeat(FeatName.Survival),
                "Must be trained in Stealth and Survival")
            .WithOnSheet(values =>
            {
                values.GrantFeat(ModData.FeatNames.ScoutsWarning);
            })
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.Id = ModData.QEffectIds.GreaterScoutActivity; // Silchas checks for this to increase it to a +2.
            });
        scoutDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return scoutDedication;
        
        // Lv4: Scout's Charge
        yield return new TrueFeat(
                ModData.FeatNames.ScoutsCharge,
                4,
                "You meander around unpredictably, and then ambush your opponents without warning.",
                "Choose one enemy. Stride, Feint against that opponent, and then make a Strike against it. For your Feint, you can attempt a Stealth check instead of the Deception check that's usually required, using the terrain around you to surprise your foe.",
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.ScoutArchetype)
            .WithPermanentQEffect(
                "Choose an enemy. Stride, Feint, and Strike them.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction chargeAction = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(IllustrationName.FleetStep, IllustrationName.Feint),
                                "Scout's Charge",
                                [ModData.Traits.MoreDedications, Trait.Basic, Trait.Archetype, Trait.Flourish],
                                "{i}You meander around unpredictably, and then ambush your opponents without warning.{/i}\n\nChoose one enemy. Stride, Feint against that opponent, and then make a Strike against it. For your Feint, you can attempt a Stealth check instead of the Deception check that's usually required, using the terrain around you to surprise your foe.",
                                Target.Self())
                            .WithActionCost(2)
                            .WithEffectOnSelf(async (thisAction, self) =>
                            {
                                if (await self.StrideAsync(
                                        "Choose where to Stride with Scout's Charge or right-click to cancel. You should end your movement adjacent to an enemy.",
                                        allowCancel: true, allowPass: true))
                                {
                                    CombatAction feint = CombatManeuverPossibilities.CreateFeintAction(self)
                                        .WithActionCost(0)
                                        .WithActiveRollSpecification(new ActiveRollSpecification(
                                            TaggedChecks.SkillCheck(Skill.Stealth),
                                            TaggedChecks.DefenseDC(Defense.Perception)));
                                    if (await self.Battle.GameLoop.FullCast(feint))
                                        await CommonCombatActions.StrikeAdjacentCreature(self, cr => cr == feint.ChosenTargets.ChosenCreature, true);
                                    else
                                    {
                                        self.Battle.Log("Scout's Charge was converted to a simple Stride.");
                                        thisAction.SpentActions = 1;
                                        thisAction.RevertRequested = true;
                                    }
                                }
                                else
                                    thisAction.RevertRequested = true;
                            });
                        
                        return new ActionPossibility(chargeAction);
                    };
                });

        // Lv4: Terrain Scout (probably no)

        // Lv6: Fleeting Shadow
        yield return new TrueFeat(
                ModData.FeatNames.FleetingShadow,
                6,
                "You're able to quickly disappear and then move about without drawing the attention of your enemies.",
                "You Hide, then Sneak twice.",
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.ScoutArchetype)
            .WithPermanentQEffect(
                "Hide, then Sneak twice.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction fleetAction = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(IllustrationName.Hide, IllustrationName.Sneak64),
                                "Fleeting Shadow",
                                [ModData.Traits.MoreDedications, Trait.Basic, Trait.Archetype, Trait.Flourish, Trait.DoesNotBreakStealth],
                                "{i}You're able to quickly disappear and then move about without drawing the attention of your enemies.{/i}\n\nYou Hide, then Sneak twice.",
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                    {
                                        if (HiddenRules.IsHiddenFromAllEnemies(self))
                                            return "You're already hidden from all enemies.";
                                        return self.Battle.AllCreatures.Any(cr =>
                                            cr.EnemyOf(self) && cr.Occupies.FogOfWar != FogOfWar.Blackened &&
                                            HiddenRules.CountsAsHavingCoverOrConcealment(self, cr))
                                            ? null
                                            : "You don't have cover or concealment from any enemy.";
                                    }))
                            .WithActionCost(2)
                            .WithEffectOnSelf(async (_, self) =>
                            {
                                CombatAction hide = CommonStealthActions.CreateHide(self)
                                    .WithActionCost(0);
                                CombatAction sneak = CommonStealthActions.CreateSneak(self)
                                    .WithActionCost(0);
                                await self.Battle.GameLoop.FullCast(hide);
                                await self.Battle.GameLoop.StateCheck();
                                if (self.Battle.AllCreatures.Any(cr => HiddenRules.DetermineHidden(cr, self) >= DetectionStrength.Hidden))
                                {
                                    await self.Battle.GameLoop.FullCast(sneak);
                                    await self.Battle.GameLoop.FullCast(sneak);
                                }
                                else
                                {
                                    bool hasSwiftSneak = self.HasEffect(QEffectId.SwiftSneak);
                                    await self.StrideAsync("Hide failed. Choose where to move up to half your speed. (1/2)", maximumHalfSpeed: !hasSwiftSneak, allowPass: true);
                                    await self.StrideAsync("Hide failed. Choose where to move up to half your speed. (2/2)", maximumHalfSpeed: !hasSwiftSneak, allowPass: true);
                                }
                            });
                        
                        return new ActionPossibility(fleetAction);
                    };
                });

        // Lv6: Scout's Speed
        yield return new TrueFeat(
                ModData.FeatNames.ScoutsSpeed,
                6,
                "You move faster, allowing you to scout ahead and report back without slowing your allies.",
                "You gain a +10-foot status bonus to your Speed.",
                [ModData.Traits.MoreDedications, Trait.Archetype])
            .WithAvailableAsArchetypeFeat(ModData.Traits.ScoutArchetype)
            .WithPermanentQEffect(
                "Gain a +10-foot status bonus to your Speed.",
                qfFeat =>
                {
                    qfFeat.BonusToAllSpeeds = qfThis =>
                        new Bonus(2, BonusType.Status, "Scout's Speed");
                });
        
        /* Higher Level Feats
         * @10 Scout's Pounce
         * @12 Camouflage
         */
    }
}