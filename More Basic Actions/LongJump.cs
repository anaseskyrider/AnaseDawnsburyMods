using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class LongJump
{
    public static void LoadLongJump()
    {
        // Add Long Jump to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect longJumpLoader = new QEffect()
            {
                Name = "LongJumpLoader",
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    PossibilitySectionId sectionId = PossibilitySectionId.Movement;
                    if (section.PossibilitySectionId != sectionId)
                        return null;
                    
                    // Held onto for later Quick Jump reasons.
                    /*SubmenuPossibility aidMenu = new SubmenuPossibility(
                        ModData.Illustrations.Aid,
                        "Prepare to Aid")
                    {
                        SubmenuId = ModData.SubmenuIds.PrepareToAid,
                        Subsections =
                        {
                            new PossibilitySection("Skill checks")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.AidSkills,
                                Possibilities = CreatePrepareToAidSkills(cr),
                            },
                            new PossibilitySection("Attack rolls")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.AidAttacks,
                                Possibilities = CreatePrepareToAidAttacks(cr),
                            },
                        },
                        SpellIfAny = new CombatAction(
                            cr,
                            ModData.Illustrations.Aid,
                            "Prepare to Aid",
                            [],
                            BasicPrepareToAidDescription+"\n\n"+BasicAidReactionDescription,
                            Target.AdjacentCreature()),
                    };

                    return aidMenu;*/

                    return (ActionPossibility)CreateLongJump(qfThis.Owner);
                },
            };
            
            cr.AddQEffect(longJumpLoader);
        });
    }

    public static CombatAction CreateLongJump(Creature owner)
    {
        return new CombatAction(
                owner,
                new SideBySideIllustration(IllustrationName.FleetStep, IllustrationName.Jump),
                "Long Jump",
                [Trait.Basic],
                "{i}With a running start, you attempt to jump through the air.{/i}\n\nStride in a straight line. At the end of your Stride, Leap with a DC 15 Athletics check to increase the distance you jump, up to your Speed."+S.FourDegreesOfSuccess(
                    null,   
                    "You Leap up to a distance equal to your check result (round down to the nearest square).",
                    "You Leap normally.",
                    "You Leap normally and land prone."),
                Target.Line(owner.Speed)
                    .WithLesserDistanceIsOkay())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.LongJump)
            .WithEffectOnChosenTargets(async (action, caster, targets) =>
            {
                // Stride in a straight line
                Tile? finalTileStride = LineAreaTarget.DetermineFinalTile(caster.Occupies, targets.ChosenTiles);
                if (finalTileStride == null)
                {
                    action.RevertRequested = true;
                    return;
                }
                await caster.MoveTo(finalTileStride, action, new MovementStyle()
                {
                    ShortestPath = true,
                    MaximumSquares = 100,
                });
                
                // Choose line direction for Leap
                CombatAction preLeap = new CombatAction(
                        caster,
                        IllustrationName.Jump,
                        "Leap",
                        [Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName],
                        "[NO DESCRIPTION]",
                        Target.Line(owner.Speed)
                            .WithLesserDistanceIsOkay()
                            .WithBlockedByCreatures())
                    .WithActionId(ActionId.Leap);
                await caster.Battle.GameLoop.FullCast(preLeap);

                if (preLeap.ChosenTargets.ChosenTiles.Count == 0)
                {
                    caster.Battle.Log("Long Jump was converted to a simple Stride.");
                    action.SpentActions = 1;
                    action.RevertRequested = true;
                    return;
                }
                
                // Roll to Jump
                // Roll spec is added later so that it can be rolled without lost info.
                // Breakdown is done manually to improve combat log printings.
                CheckBreakdown breakdown = CombatActionExecution.BreakdownAttack(
                    preLeap
                        .WithActiveRollSpecification(new ActiveRollSpecification(
                            TaggedChecks.SkillCheck(Skill.Athletics),
                            new TaggedCalculatedNumberProducer((action2, attacker, target) => new CalculatedNumber(15, "Long Jump DC", [])))), caster);
                CheckBreakdownResult result = new CheckBreakdownResult(breakdown);
                int leapDistance = CalculateJumpDistanceResult(caster, result.TotalRollValue, result.CheckResult);
                List<Tile> leapTiles = preLeap.ChosenTargets.ChosenTiles
                    .Where(tile => tile.DistanceTo(caster) <= leapDistance)
                    .ToList();
                Tile chosenTileLeap = LineAreaTarget.DetermineFinalTile(caster.Occupies, leapTiles)!;
                
                // Log the check result
                caster.Overhead(
                    result.CheckResult.HumanizeTitleCase2(),
                    Color.WhiteSmoke,
                    $"{preLeap.Owner} rolls {result.CheckResult.Greenify()} to jump up to {leapDistance*5} feet.",
                    preLeap.Name,
                    breakdown.DescribeWithFinalRollTotal(result));
                
                // Perform Leap
                CombatAction leap = CommonCombatActions.Leap(caster, leapDistance)
                    .WithActionCost(0);
                leap.Description += "\n\n{b}Roll:{/b} "+result.TotalRollValue+(caster.HasEffect(QEffectId.PowerfulLeap) ? " + 5 {Blue}(Powerful Leap){/Blue}" : null);
                await caster.Battle.GameLoop.FullCast(leap, ChosenTargets.CreateTileTarget(chosenTileLeap));
                
                // Land prone if crit fail
                if (result.CheckResult == CheckResult.CriticalFailure)
                    await caster.FallProne();
            });
    }

    
    public static int CalculateJumpDistanceResult(Creature leaper, int rollTotal, CheckResult result)
    {
        // Use highest of your determined distance or your rolled distance,
        // and include Powerful Leap in that roll,
        // but cap to your move speed.
        int basicLeap = CommonCombatActions.DetermineLeapDistance(leaper);
        int rolledLeap = (rollTotal + (leaper.HasEffect(QEffectId.PowerfulLeap) ? 5 : 0) + (leaper.HasEffect(QEffectId.PowerfulLeap2) ? 5 : 0)) / 5;
        int finalLeap = Math.Max(basicLeap, rolledLeap);
        if (result >= CheckResult.Success)
            return Math.Min(leaper.Speed, finalLeap);
        return Math.Min(leaper.Speed, basicLeap);
    }
}