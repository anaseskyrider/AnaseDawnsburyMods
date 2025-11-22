using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class Reposition
{
    public static void LoadReposition()
    {
        // Add Reposition to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect repositionLoader = new QEffect()
            {
                Name = "RepositionLoader",
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.TrueAttackManeuvers)
                        return null;
                    
                    return (ActionPossibility)CreateReposition(qfThis.Owner);
                },
            };
            cr.AddQEffect(repositionLoader);
        });
    }

    public static CombatAction CreateReposition(Creature owner)
    {
        const string improvName = "[REPOSITION IMPROVEMENT]";
        return new CombatAction(
                owner,
                ModData.Illustrations.Reposition,
                "Reposition",
                [Trait.Basic, ModData.Traits.MoreBasicActions, Trait.Attack, Trait.AttackDoesNotTargetAC],
                "{i}You forcefully relocate a creature.{/i}\n\n{b}Requirements{/b} You have a free hand, are holding the target or have a grapple weapon, and the target isn't more than one size larger than you.\n\nMake an Athletics check against the target's Fortitude DC."
                + S.FourDegreesOfSuccess("You move the creature up to 10 feet along any unobstructed path within your reach.",
                "As critical success, but you move the creature 5 feet.",
                null,
                "The target Repositions you to a random square instead, as if a success.")
                + "\n\n{b}Special{/b} You automatically get one degree of success better when targeting an ally with Reposition.",
                new CreatureTarget( // Custom target that will let you target allies
                    RangeKind.Melee, 
                    [
                        MeleeReachCreatureTargetingRequirement.WithWeaponOfTrait(Trait.Grapple),
                        new TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement(),
                        new LegacyCreatureTargetingRequirement((a, d) =>
                        {
                            if (a == d) // Cannot be self
                                return Usability.NotUsableOnThisCreature("self");
                            if (!a.HasFreeHand && !a.WieldsItem(Trait.Grapple)) // Need a free hand or a grapple weapon
                                return Usability.CommonReasons.NoFreeHandForManeuver;
                            if (d.WeaknessAndResistance.ImmunityToForcedMovement) // Mustn't be immune
                                return Usability.NotUsableOnThisCreature("immune to forced movement");
                            return Usability.Usable;
                        })
                    ],
                    (_, _, _) => int.MinValue))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.Shove)
            .WithActionId(ModData.ActionIds.Reposition)
            .WithActiveRollSpecification(new ActiveRollSpecification(
                TaggedChecks.SkillCheck(Skill.Athletics),
                TaggedChecks.DefenseDC(Defense.Fortitude)))
            .WithTargetingTooltip((action, target, index) =>
            {
                QEffect? improve = null;
                if (target.FriendOf(action.Owner))
                {
                    improve = new QEffect()
                    {
                        AdjustActiveRollCheckResult = (qfThis, action2, target2, result) =>
                            action2 == action && target2 == target
                                ? result.ImproveByOneStep()
                                : result
                    };
                    action.Owner.AddQEffect(improve);
                }
                CheckBreakdown result = CombatActionExecution.BreakdownAttackForTooltip(action, target);
                if (improve is not null)
                    improve.ExpiresAt = ExpirationCondition.Immediately;
                return result.TooltipDescription;
            })
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, self, targets) =>
            {
                if (targets.ChosenCreature?.FriendOf(action.Owner) ?? false)
                    self.AddQEffect(new QEffect()
                    {
                        Name = improvName,
                        AdjustActiveRollCheckResult = (qfThis, action2, target2, result) =>
                            action2 == action && target2 == targets.ChosenCreature
                                ? result.ImproveByOneStep()
                                : result
                    });
            })
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                caster.FictitiousSingleTileMove(caster.Occupies); // Do not await, as it adds unnecessary delays.
                switch (result)
                {
                    case CheckResult.CriticalSuccess:
                        await ExecuteRepositionLogic(caster, target, 2);
                        break;
                    case CheckResult.Success:
                        await ExecuteRepositionLogic(caster, target, 1);
                        break;
                    case CheckResult.CriticalFailure:
                        if (!target.FriendOf(caster))
                            await ExecuteRepositionLogic(target, caster, 1, true);
                        break;
                }

                caster.RemoveAllQEffects(qf => qf.Name is improvName);
            });
    }
    
    public static async Task ExecuteRepositionLogic(Creature attacker, Creature defender, int distance, bool randomTile = false)
    {
        int reach = GrappleTag.GetGrappleReach(attacker);
        List<Tile> tiles = attacker.Battle.Map.AllTiles
            .Where(tile =>
                !ReferenceEquals(defender.Space.TopLeftTile, tile)
                && tile.IsTrulyGenuinelyFreeTo(defender)
                && tile.DistanceTo(defender.Occupies) <= distance
                && attacker.DistanceToWith10FeetException(tile) <= reach)
            .ToList();
        if (tiles.Count == 0)
        {
            attacker.Overhead("*no valid tiles*", Color.Red, "Reposition failed: No free spaces.");
            return;
        }
        Tile? moveTo;
        if (randomTile)
            moveTo = tiles.GetRandomForAi();
        else
            moveTo = await attacker.Battle.AskToChooseATile(
                attacker, tiles,
                ModData.Illustrations.Reposition, //IllustrationName.GenericCombatManeuver,
                "Choose where to relocate " + defender.Name + ".",
                "",
                false, true, defender,
                "Don\'t reposition");
        if (moveTo is null)
            return;
        await defender.MoveTo(moveTo, null,
            new MovementStyle()
            {
                ForcedMovement = true,
                Shifting = true,
                ShortestPath = true,
                MaximumSquares = 100,
            });
    }
}