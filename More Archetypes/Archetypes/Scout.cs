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

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Scout
{
    /// <summary>
    /// An italicized link to the Exploration Activities workshop mod page.
    /// </summary>
    public const string EXPLORATION_ACTIVITIES_MOD_LINK =
        "{i}{link:https://steamcommunity.com/sharedfiles/filedetails/?id=3527574947}Exploration Activities{/}{/i}";
    
    /// <summary>
    /// The ExplorationActivity trait from the Exploration Activities mod. Null if not found.
    /// </summary>
    public static Trait? ExplorationActivity = ModManager.TryParse("ExplorationActivity", out Trait innerOut) ? innerOut : null;
    
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        bool expLoaded = ExplorationActivity.HasValue;

        string? ReqLine()
        {
            return expLoaded
                ? null
                : $"{{b}}Requirements{{/b}} You have the {EXPLORATION_ACTIVITIES_MOD_LINK} mod installed.\n\n";
        }
        string ModLine(string ifLoaded)
        {
            return ModData.Illustrations.DdSun.IllustrationAsIconString + " " + (expLoaded
                ? "{b}Exploration Activities{/b} " + ifLoaded
                : $"{{b}}Modding{{/b}} This gains additional functionality if the {EXPLORATION_ACTIVITIES_MOD_LINK} mod is loaded.");
        }
        
        // Lv4: Scout's Warning
        // [Ranger] [Rogue]
        QEffectId? scoutQF = ModManager.TryParse("EA_Scouting", out QEffectId innerName1)
            ? innerName1
            : null;
        yield return new TrueFeat(
                ModData.FeatNames.ScoutsWarning, 4,
                "You warn your allies of danger.",
                $$"""
                Your allies (but not you) gain a +1 circumstance bonus to their initiative rolls.

                {{ModLine("This bonus increases to +2 if you're using the Scout activity.")}}
                """,
                [Trait.Ranger, Trait.Rogue])
            .WithPermanentQEffect(
                "Your allies gain a +1 circumstance bonus to their initiative rolls.",
                qfFeat =>
                {
                    // Add details for modded users.
                    if (scoutQF.HasValue)
                        qfFeat.Description = qfFeat.Description!.Replace(
                            "+1 circumstance bonus",
                            qfFeat.Owner.HasEffect(scoutQF.Value)
                                ? "{Green}(Scouting){/Green} +2 circumstance bonus"
                                : "+1 circumstance bonus {Red}(+2 while you are Scouting){/Red}");
                    
                    qfFeat.StartOfCombatBeforeOpeningCutscene = async qfThis =>
                    {
                        // Increase bonus if the Scout activity is in effect
                        int bonus = scoutQF.HasValue && qfThis.Owner.HasEffect(scoutQF.Value)
                            ? 2
                            : 1;
                        
                        // Apply Scout's Warning to everyone
                        foreach (Creature ally in qfThis.Owner.Battle.AllCreatures
                                     .Where(cr =>
                                         cr.FriendOfAndNotSelf(qfThis.Owner))
                                     .ToList())
                        {
                            ally.AddQEffect(new QEffect()
                            {
                                Name = "Scout's Warning",
                                BonusToInitiative = _ =>
                                    new Bonus(bonus, BonusType.Circumstance, "Scout's warning"),
                            });
                        }

                        // Redo initiative order
                        List<Creature> oldList = qfThis.Owner.Battle.InitiativeOrder.ToList();
                        qfThis.Owner.Battle.InitiativeOrder.Clear();
                        oldList.ForEach(cr =>
                        {
                            if (cr.EntersInitiativeOrder)
                                cr.EnterInitiativeOrderNow();
                        });
                    };
                });
        
        // Avoid Notice and Scout, created as part of Scout Dedication.
        FeatName? avoidNotice = ModManager.TryParse("AvoidNotice", out FeatName innerName3) ? innerName3 : null;
        FeatName? scoutActivity = ModManager.TryParse("ScoutActivity", out FeatName innerName2) ? innerName2 : null;
        if (ExplorationActivity.HasValue && avoidNotice.HasValue && scoutActivity.HasValue)
        {
            Feat avoidAndScout = new Feat(
                    ModData.FeatNames.AvoidNoticeAndScout,
                    "You are a highly skilled scout, capable of guiding your allies, detecting imminent threats, and avoiding the attention of dangerous enemies.",
                    $"You perform the {avoidNotice.Value.ToLink("Avoid Notice")} and {scoutActivity.Value.ToLink("Scout")} activities at the same time.",
                    [ExplorationActivity.Value],
                    null)
                .WithOnSheet(values =>
                {
                    values.GrantFeat(avoidNotice.Value);
                    values.GrantFeat(scoutActivity.Value);
                })
                .WithPrerequisite(
                    values => values.HasFeat(ModData.FeatNames.ScoutDedication),
                    "You must have the Scout Dedication feat.");
            avoidAndScout.FeatGroup = ModData.FeatGroups.Archetypes;
            yield return avoidAndScout;
        }
        
        // Lv2: Scout Dedication
        Feat scoutDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.Scout,
                "You're an expert in espionage and reconnaissance, able to skulk silently through the wilderness to gather intelligence, sneak through enemy lines to report to your comrades, or suddenly and decisively strike your foes. Your skills ease the difficulty of travel for you and your companions and keep you all alert to approaching danger.",
                $$"""
                  {{ReqLine()}}The bonus to initiative you grant when you Scout is increased to +2. You can also Avoid Notice and Scout at the same time.
                  """)
            .WithPermanentQEffect(null, qfFeat =>
            {
                // Silchas checks for this to increase it to a +2.
                qfFeat.Id = ModData.QEffectIds.GreaterScoutActivity; 
            })
            .WithPrerequisite(values =>
                    values.HasFeat(FeatName.Stealth),
                "Must be trained in Stealth")
            .WithPrerequisite(values =>
                    values.HasFeat(FeatName.Survival),
                "Must be trained in Survival");
        ModData.FeatNames.ScoutDedication = scoutDedication.FeatName;
        yield return scoutDedication;
        
        // Lv4: Scout's Charge
        yield return new TrueFeat(
                ModData.FeatNames.ScoutsCharge, 4,
                "You meander around unpredictably, and then ambush your opponents without warning.",
                "Choose one enemy. Stride, Feint them, and then Strike them. For your Feint, you can use Stealth instead of Deception, utilizing the terrain around you to surprise your foe.",
                [Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Scout)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction chargeAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(IllustrationName.FleetStep, IllustrationName.Feint),
                            "Scout's Charge",
                            [ModData.ModTrait, Trait.Archetype, Trait.Flourish],
                            """
                            {i}You meander around unpredictably, and then ambush your opponents without warning.{/i}

                            Choose one enemy. Stride, Feint them, and then Strike them. For your Feint, you can attempt a Stealth check instead of using Deception, using the terrain around you to surprise your foe.
                            """,
                            Target.Self())
                        .WithActionCost(2)
                        .WithShortDescription("Choose an enemy. Stride, Feint, and Strike them.")
                        .WithEffectOnSelf(async (thisAction, self) =>
                        {
                            if (await self.StrideAsync(
                                    "Choose where to Stride with Scout's Charge or right-click to cancel. You should end your movement adjacent to an enemy.",
                                    allowCancel: true, allowPass: true))
                            {
                                CombatAction feint = CombatManeuverPossibilities.CreateFeintAction(self)
                                    .WithActionCost(0)
                                    .WithActiveRollSpecification(new ActiveRollSpecification(
                                        TaggedChecks.BestRoll(
                                            TaggedChecks.SkillCheck(Skill.Stealth),
                                            TaggedChecks.SkillCheck(Skill.Deception)),
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
                ModData.FeatNames.FleetingShadow, 6,
                "You're able to quickly disappear and then move about without drawing the attention of your enemies.",
                "You Hide, then Sneak twice.",
                [Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Scout)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction fleetAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(IllustrationName.Hide, IllustrationName.Sneak64),
                            "Fleeting Shadow",
                            [ModData.ModTrait, Trait.Archetype, Trait.Flourish, Trait.DoesNotBreakStealth],
                            """
                            {i}You're able to quickly disappear and then move about without drawing the attention of your enemies.{/i}

                            You Hide, then Sneak twice.
                            """,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                {
                                    if (HiddenRules.IsHiddenFromAllEnemies(self))
                                        return "You're already hidden from all enemies.";
                                    // TODO: replace .Occupies
                                    return self.Battle.AllCreatures.Any(cr =>
                                        cr.EnemyOf(self) && cr.Occupies.FogOfWar != FogOfWar.Blackened &&
                                        HiddenRules.CountsAsHavingCoverOrConcealment(self, cr))
                                        ? null
                                        : "You don't have cover or concealment from any enemy.";
                                }))
                        .WithActionCost(2)
                        .WithShortDescription("Hide, then Sneak twice.")
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
                ModData.FeatNames.ScoutsSpeed, 6,
                "You move faster, allowing you to scout out ahead and report back without slowing your allies.",
                "You gain a +10-foot status bonus to your Speed.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Scout)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.BonusToAllSpeeds = _ =>
                    new Bonus(2, BonusType.Status, "Scout's Speed");
            });
        
        // Lv10: Scout's Pounce
        yield return new TrueFeat(
                ModData.FeatNames.ScoutsPounce, 10,
                "You leap from the shadows to strike at your foes.",
                """
                {b}Requirements{/b} You are at least hidden from all enemies, and you aren't within 10 feet of any enemy.
                
                Stride up to your Speed, then Strike twice. If you were at least hidden to the target of these Strikes, the target is off-guard against both attacks. {i}(Your multiple attack penalty applies normally for both attacks.){/i}
                """,
                [Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Scout)
            .WithPermanentQEffect(qfFeat =>
            {
                // Use OffenseBlock to show it on the block while making the action itself contextual.
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] If you are hidden from everyone and are at least 10 feet away, then you can Stride and Strike twice. Your targets are {r:flat-footed}off-guard{/} against both attacks.";

                qfFeat.ProvideContextualAction = qfThis =>
                {
                    if (!HiddenRules.IsHiddenFromAllEnemies(qfThis.Owner))
                        return null;

                    CombatAction pounce = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                IllustrationName.FleetStep,
                                IllustrationName.Swipe),
                            "Scout's Pounce",
                            [ModData.ModTrait, Trait.Archetype, Trait.Flourish, Trait.DoesNotBreakStealth],
                            null!,
                            Target.Self()
                                // If you're hidden, this action should still show up to warn you
                                // if you're not meeting the other criterion
                                .WithAdditionalRestriction(self =>
                                    self.Battle.AllCreatures
                                        .Where(self.EnemyOf)
                                        .All(cr => cr.DistanceTo(qfThis.Owner) > 2)
                                    ? null : "Enemies within 10 feet"))
                        .WithActionCost(2)
                        .WithDescription(
                            "You leap from the shadows to strike at your foes.",
                            """
                            {b}Requirements{/b} You are at least hidden from all enemies, and you aren't within 10 feet of any enemy.

                            Stride up to your Speed, then Strike twice. If you were at least hidden to the target of these Strikes, the target is off-guard against both attacks. {i}(Your multiple attack penalty applies normally for both attacks.){/i}
                            """)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            List<Creature> wasHiddenTo = self.Battle.AllCreatures
                                .Where(cr =>
                                    self.EnemyOf(cr)
                                    && HiddenRules.DetermineHidden(cr, self) >= DetectionStrength.Hidden)
                                .ToList();
                            
                            if (!await self.StrideAsync("Choose where to Stride with Scout's Pounce. You should end your movement within your reach or range of an enemy. (1/3)", allowCancel: true))
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            for (int i = 0; i < 2; i++)
                            {
                                bool completed = await CommonCombatActions.StrikeCreature(
                                    self,
                                    null,
                                    strike =>
                                    {
                                        strike.WithTargetingTooltip((innerStrike, target, _) =>
                                            {
                                                QEffect pounceGuard = new QEffect()
                                                {
                                                    // You are flat-footed to Owner's strike if they were hidden
                                                    // to you and are attacking with this activity's strike.
                                                    Name = "[SCOUT'S POUNCE: OFF-GUARD]",
                                                    IsFlatFootedTo = (qfFlat, _, flatAction) =>
                                                        wasHiddenTo.Contains(qfFlat.Owner)
                                                        && flatAction == strike
                                                            ? "Scout's Pounce"
                                                            : null
                                                };
                                                target.AddQEffect(pounceGuard);
                                                var breakdown =
                                                    CombatActionExecution.BreakdownAttackForTooltip(innerStrike,
                                                        target);
                                                target.RemoveAllQEffects(qf => qf == pounceGuard);
                                                return breakdown.TooltipDescription;
                                            })
                                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (_,_,
                                                chosen) =>
                                            {
                                                QEffect pounceGuard = new QEffect()
                                                {
                                                    // You are flat-footed to Owner's strike if they were hidden
                                                    // to you and are attacking with this activity's strike.
                                                    Name = "[SCOUT'S POUNCE: OFF-GUARD]",
                                                    IsFlatFootedTo = (qfFlat, _, flatAction) =>
                                                        wasHiddenTo.Contains(qfFlat.Owner)
                                                        && flatAction == strike
                                                            ? "Scout's Pounce"
                                                            : null
                                                };
                                                chosen.ChosenCreature?.AddQEffect(pounceGuard);
                                            })
                                            .WithEffectOnEachTarget(async (_, _, target, _) =>
                                            {
                                                target.RemoveAllQEffects(qf =>
                                                    qf.Name == "[SCOUT'S POUNCE: OFF-GUARD]");
                                            });

                                    },
                                    null,
                                    action.Illustration,
                                    $"Choose an enemy to Strike with Scout's Pounce. ({i+2}/3)",
                                    false,
                                    i == 0 ? "Convert to simple Stride" : "Skip 2nd Strike");
                                
                                if (i == 0 && !completed)
                                {
                                    self.Battle.Log("Scout's Pounce was converted to a simple Stride.");
                                    action.SpentActions = 1;
                                    action.RevertRequested = true;
                                }
                            }
                        });

                    return new ActionPossibility(pounce);
                };
            });
        
        // Lv12: Camouflage for Scout
        TrueFeat camoForScout = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.Camouflage, ModData.Traits.Scout, 12);
        ModData.FeatNames.CamouflageForScout = camoForScout.FeatName;
        yield return camoForScout;
    }
}