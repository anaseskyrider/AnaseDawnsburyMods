using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class LongJump
{
    /// The acceptable range of deviation from the initial Stride for the follow-up Leap.
    public const int LEAP_RANGE = 45;

    public static void LoadLongJump()
    {
        // Add Long Jump to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect longJumpLoader = new QEffect()
            {
                Name = "LongJumpGranter",
                Key = "LongJumpGranter",
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.Movement)
                        return null;

                    return (ActionPossibility) CreateLongJump(
                        qfThis.Owner,
                        qfThis.Owner.HasFeat(ModData.FeatNames.QuickJump));
                },
            };
            
            cr.AddQEffect(longJumpLoader);
        });
        
        foreach (Feat ft in LoadFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> LoadFeats()
    {
        // Quick Jump
        yield return new TrueFeat(
                ModData.FeatNames.QuickJump, 1,
                null,
                "High Jump costs a single action instead of 2, and no longer performs an initial Stride.",
                [ModData.ModTrait, Trait.General, Trait.Skill])
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Athletics),
                "You must be trained in Athletics.");
        
        // Acrobat - Graceful Leaper
        yield return new TrueFeat(
                ModData.FeatNames.GracefulLeaper, 7,
                "Mass and muscle are meaningless when you leap; only grace and balance matter.",
                "You can use Acrobatics instead of Athletics to Long Jump if it's better.",
                [ModData.ModTrait, Trait.Archetype, Trait.Skill])
            .WithAvailableAsArchetypeFeat(Trait.Acrobat)
            .WithRulesBlockForCombatAction(cr => CreateLongJump(cr, false))
            .WithPrerequisite(
                values => values.HasFeat(FeatName.MasterAcrobatics),
                "You must be a master in Acrobatics");
    }

    public static CombatAction CreateLongJump(Creature owner, bool isQuickJump = false)
    {
        Skill bestSkill = owner.HasFeat(ModData.FeatNames.GracefulLeaper)
            ? new[] {Skill.Acrobatics, Skill.Athletics}.MaxBy(skill => owner.Skills.Get(skill))
            : Skill.Athletics;
        
        return new CombatAction(
                owner,
                ModData.Illustrations.LongJump,
                "Long Jump",
                [ModData.ModTrait, Trait.Move, Trait.DoesNotProvoke],
                null!,
                isQuickJump
                    ? Target.Self() // Skips the Stride part
                    : Target.Tile((cr, t) =>
                                t.LooksFreeTo(cr)
                                && t.DistanceTo(cr) > 1
                                && cr.Space.Tiles.All(t2 => t2.HasLineOfEffectToIgnoreLesser(t) == CoverKind.None),
                            (_, _) => int.MinValue)
                        .WithPathfindingGuidelines(cr =>
                            new PathfindingDescription { Squares = cr.Speed }))
            .WithDescription(
                (isQuickJump ? "You " : "With a running start, you ")
                    + "attempt to jump through the air.",
                (!isQuickJump ? "Stride at least 10 feet in a straight line. At the end of your Stride, " : null)
                    + $"Leap {(!isQuickJump ? "in the direction of your Stride " : null)}with a DC 15 {(bestSkill is Skill.Athletics ? "Athletics" : "{Blue}"+bestSkill.HumanizeTitleCase2()+"{/Blue}")} check ({S.SkillBonus(owner, bestSkill)}) to increase the distance you jump, up to your Speed."
                    + S.FourDegreesOfSuccess(
                        null,   
                        "You Leap up to a distance equal to your check result (round down to the nearest square).",
                        "You Leap normally.",
                        "You Leap normally and land prone."))
            .WithActionCost(isQuickJump ? 1 : 2)
            .WithActionId(ModData.ActionIds.LongJump) // Bonuses to checks to leap should use ActionId.Leap.
            .WithEffectOnChosenTargets(async (action, caster, targets) =>
            {
                bool hasQuickJump = action.ActionCost == 1 && isQuickJump;
                
                #region Minimum Stride

                // Stride in a straight line
                Tile? strideStart = null;
                if (!hasQuickJump)
                {
                    strideStart = caster.Space.TopLeftTile;
                    await caster.MoveTo(
                        targets.ChosenTile!,
                        action,
                        new MovementStyle()
                        {
                            ShortestPath = true,
                            MaximumSquares = 100,
                            Shifting =
                                (caster.HasEffect(QEffectId.Mobility)
                                 || caster.HasEffect(QEffectId.IgnoreAoOWhenMoving))
                                && !(targets.ChosenTile?.InIteration.RequiresProvokingAttackOfOpportunity ?? false)
                        });
                    if (caster.Space.TopLeftTile.DistanceTo(strideStart) < 2)
                    {
                        caster.Battle.Log("Long Jump disrupted. Failed to Stride at least 10 feet.");
                        return;
                    }
                }

                #endregion

                #region Leap after Stride

                // Leap after Stride
                CombatAction leap = CommonCombatActions.Leap(
                        caster,
                        CalculateMaximumJumpDistance(caster))
                    .WithActionCost(0)
                    .WithExtraTrait(Trait.DoNotShowInCombatLog);
                
                // Must Leap in the same direction of your Stride
                if (strideStart != null)
                {
                    Tile leapStart = caster.Space.CenterTile;
                    var angleFromStride = Math.Atan2(
                                              leapStart.Y - strideStart.Y,
                                              leapStart.X - strideStart.X)
                                          * (180.0 / Math.PI);
                    ((TileTarget)leap.Target).WithAdditionalTargetingRequirement((_, t) =>
                    {
                        var angleToLeap = Math.Atan2(
                                              t.Y - leapStart.Y,
                                              t.X - leapStart.X)
                                          * (180.0 / Math.PI);
                        return angleToLeap <= angleFromStride+LEAP_RANGE
                               && angleToLeap >= angleFromStride-LEAP_RANGE
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Not in the same direction");
                    });
                }
                
                // Prepare check data
                TaggedCalculatedNumberProducer jumpSkill = owner.HasFeat(ModData.FeatNames.GracefulLeaper)
                    ? TaggedChecks.SkillCheck(Skill.Athletics, Skill.Acrobatics)
                    : TaggedChecks.SkillCheck(Skill.Athletics);
                CheckBreakdown breakdownForTooltip = CombatActionExecution.BreakdownAttackForTooltip(
                    leap.WithActiveRollSpecification(new ActiveRollSpecification(
                        jumpSkill,
                        new TaggedCalculatedNumberProducer((_, _, _) =>
                            new CalculatedNumber(15, "Long Jump DC", [])))),
                    caster);
                
                // Targeting tooltip showing chances of success
                ((TileTarget)leap.Target).WithTooltip((target, tile) =>
                {
                    Creature self = target.OwnerAction.Owner;
                    int distanceToTarget = self.DistanceTo(tile);

                    List<int> rollDistances = [];
                    for (int i = 1; i < 21; i++)
                    {
                        CheckResult result =
                            i <= breakdownForTooltip.CritMisses ? CheckResult.CriticalFailure
                            : i <= breakdownForTooltip.Misses ? CheckResult.Failure
                            : i <= breakdownForTooltip.Hits ? CheckResult.Success
                            : CheckResult.CriticalSuccess;
                        int rollDistance = CalculateJumpDistanceResult(
                            self, i + breakdownForTooltip.TotalCheckBonus, result);
                        rollDistances.Add(rollDistance);
                    }

                    int atLeastThatDistance = rollDistances.Count(dist => dist >= distanceToTarget);

                    return breakdownForTooltip.TooltipDescription
                        .Replace(
                            ProbabilityBar(
                                breakdownForTooltip.CritMisses, breakdownForTooltip.Misses,
                                breakdownForTooltip.Hits, breakdownForTooltip.CritHits),
                            ProbabilityBar(
                                0, rollDistances.Count - atLeastThatDistance,
                                atLeastThatDistance, 0))
                        .Replace(
                            "vs. {b}15{/b}",
                            $"vs. {{b}}{distanceToTarget * 5}{{/b}} feet")
                        .Replace(
                            "{b}15 DC",
                            $"{{b}}{distanceToTarget * 5} DC")
                        + $"\n{Math.Max(0, (distanceToTarget * 5) - 15).Greenify()} Adjusted distance";

                    string ProbabilityBar(int fumbles, int fails, int hits, int crits)
                    {
                        return $"{{probabilitybar:check:{fumbles}:{fails}:{hits}:{crits}}}";
                    }
                });
                
                // Roll to Jump
                //leap.EffectOnChosenTargets = null;
                leap.EffectOnChosenTargets = async (innerLeap, leaper, leapTargets) =>
                {
                    // Roll spec is added later so that it can be rolled without lost info.
                    // Breakdown is done manually to improve combat log printings.
                    CheckBreakdown breakdown = CombatActionExecution.BreakdownAttack(
                        leap.WithActiveRollSpecification(new ActiveRollSpecification(
                            jumpSkill,
                            new TaggedCalculatedNumberProducer((_, _, _) =>
                                new CalculatedNumber(15, "Long Jump DC", [])))),
                        caster);
                    CheckBreakdownResult result = new CheckBreakdownResult(breakdown);
                    
                    int leapDistance = CalculateJumpDistanceResult(
                        leaper,
                        result.TotalRollValue,
                        result.CheckResult);

                    QEffect quickFlight = QEffect.Flying(); // Permits leaping over chasms
                    leaper.AddQEffect(quickFlight);
                    List<Tile>? leapPath = Pathfinding
                        .GetPath(
                            leaper,
                            leapTargets.ChosenTile!,
                            leaper.Battle,
                            new PathfindingDescription() { Squares = 99 })
                        ?.ToList();
                    List<Tile>? truePath = leapPath
                        ?.Where(tile => tile.DistanceTo(leaper) <= leapDistance)
                        .ToList();
                    Tile? finalTile = truePath?.LastOrDefault();
                    leaper.RemoveAllQEffects(qf => qf == quickFlight);
                    
                    // Chasm Warning!
                    if (leapPath != null
                        && !leaper.HasEffect(QEffectId.Flying)
                        && leapPath.Any(tile =>
                            leaper.DistanceTo(tile) > 2 // Ignore chasms inside the minimum leap distance
                            && tile.Kind is TileKind.Chasm))
                    {
                        if (!await leaper.AskForConfirmation(
                                action.Illustration,
                                "{b}Long Jump " + RulesBlock.GetIconTextFromNumberOfActions(action.ActionCost) +
                                "{b}\nYou're about to jump over a chasm and can't fly to stay aloft. Landing in a chasm will {Red}result in death{/Red}.",
                                "Jump"))
                        {
                            if (!hasQuickJump)
                            {
                                caster.Battle.Log("Long Jump was converted to a simple Stride.");
                                action.SpentActions = 1;
                            }
                            action.RevertRequested = true;
                            return;
                        }
                    }
                    
                    // Log the check result
                    string whatHappens = result.CheckResult switch
                    {
                        CheckResult.CriticalFailure => $"leaps normally ({leapDistance * 5} feet), then falls prone",
                        CheckResult.Failure => $"leaps normally ({leapDistance * 5} feet)",
                        _ => $"jumps {leapDistance * 5} feet",
                    } + ".";
                    leaper.Overhead(
                        result.CheckResult.HumanizeTitleCase2(),
                        Color.WhiteSmoke,
                        $"{leaper} gets a {result.CheckResult.Greenify()} and " + whatHappens,
                        innerLeap.Name,
                        $"""
                          {breakdown.DescribeWithFinalRollTotal(result)}
                          
                          {ExplainJumpResult(leaper, leapDistance, result)}
                          """);
                    
                    // Perform Leap
                    if (finalTile != null)
                        await leaper.SingleTileMove(finalTile, action);
                    
                    // Land prone if crit fail
                    if (result.CheckResult == CheckResult.CriticalFailure)
                        await leaper.FallProne();

                    // Die, if you're a superhero trying to get their mojo back by leaping across buildings.
                    if (leaper.Space.Tiles.All(tile => tile.Kind is TileKind.Chasm)
                        && !leaper.HasEffect(QEffectId.Flying))
                    {
                        leaper.Battle.Log(leaper + " falls to their demise.");
                        leaper.Die();
                    }
                };

                #endregion
                
                // Execute, revert if you didn't Leap.
                if (!await caster.Battle.GameLoop.FullCast(leap))
                {
                    if (!hasQuickJump)
                    {
                        caster.Battle.Log("Long Jump was converted to a simple Stride.");
                        action.SpentActions = 1;
                    }
                    action.RevertRequested = true;
                }
            });
    }
    
    public static int CalculateJumpDistanceResult(Creature self, int rollTotal, CheckResult result)
    {
        // Basic leap with jump roll
        int basicLeap = self.Speed >= 6 ? 3 : 2;
        int rolledLeap = Math.Max(
            basicLeap,
            result >= CheckResult.Success ? rollTotal / 5 : 0); // Rolled increase;
        int finalLeap = Math.Min(
            rolledLeap + (self.HasEffect(QEffectId.PowerfulLeap) ? 1 : 0) // Powerful Leap
                       + (self.HasEffect(QEffectId.PowerfulLeap2) ? 1 : 0),
            self.Speed); // Capped to speed
        
        // Better leap from special effect
        int overriddenLeap = Math.Max(
            self.HasEffect(QEffectId.SteamKnight) ? self.Speed : 0,
            self.HasEffect(QEffectId.JumpSpell) ? 6 : 0);
        
        // Use the best of your overridden Leap or your increased Leap
        return Math.Max(finalLeap, overriddenLeap);
    }
    
    /// Gets your maximum possible Jump distance.
    public static int CalculateMaximumJumpDistance(Creature self)
    {
        return CalculateJumpDistanceResult(
            self,
            20 + self.Skills.Get(Skill.Athletics),
            CheckResult.CriticalSuccess);
    }

    public static string ExplainJumpResult(Creature leaper, int finalLeapDistance, CheckBreakdownResult breakResult)
    {
        int normalLeapFeet = CalculateJumpDistanceResult(leaper, 0, CheckResult.Failure) * 5;
        int finalLeapFeet = finalLeapDistance * 5;
        int speedFeet = leaper.Speed * 5;
        string jumpExplanation = 
            $$"""
              {b}{{finalLeapFeet}} feet breakdown:{/b}
              {b}{{normalLeapFeet}}{b} Leap{{(leaper.HasEffect(QEffectId.PowerfulLeap) || leaper.HasEffect(QEffectId.PowerfulLeap2) ? " (Powerful)" : null)}}
              """;
        if (breakResult.CheckResult < CheckResult.Success)
            jumpExplanation += $"\n{{b}}{{Red}}+0{{/Red}}{{/b}} Roll ({breakResult.CheckResult.ToStringOrTechnical().Capitalize()})";
        else
        {
            int rolledFeet = breakResult.TotalRollValue / 5 * 5;
            jumpExplanation += $"\n{(rolledFeet - normalLeapFeet).Greenify()} Roll (Nearest square)";
            if (rolledFeet > speedFeet)
                jumpExplanation += $"\n{(speedFeet - rolledFeet).Greenify()} Your speed";
        }

        return jumpExplanation;
    }
}