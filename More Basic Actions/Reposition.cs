using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
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
                Name = "RepositionGranter",
                Key = "RepositionGranter", // Last mod to add this effect will win out.
                Value = 5, // Higher precedent than Commander
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
                [ModData.ModTrait, Trait.Attack, Trait.AttackDoesNotTargetAC],
                """
                {i}You forcefully relocate a creature.{/i}

                {b}Requirements{/b} You have a free hand, are holding the target, or have a grapple weapon; and the target isn't more than one size larger than you.

                Make an Athletics check against the target's Fortitude DC.
                """
                + S.FourDegreesOfSuccess("You move the creature up to 10 feet along any unobstructed path, staying within your reach.",
                "As critical success, but you move the creature 5 feet.",
                null,
                "The target Repositions you randomly, as if a success.")
                + "\n\n{b}Special{/b} You automatically get one degree of success better when targeting an ally with Reposition.",
                new CreatureTarget( // Custom target that will let you target allies
                    RangeKind.Melee, 
                    [
                        // Cannot be self
                        new LegacyCreatureTargetingRequirement((a, d) =>
                            a == d
                                ? Usability.NotUsableOnThisCreature("self")
                                : Usability.Usable),
                        // Creature can't be immune to forced movement
                        new LegacyCreatureTargetingRequirement((a, d) =>
                            d.WeaknessAndResistance.ImmunityToForcedMovement 
                                ? Usability.NotUsableOnThisCreature("immune to forced movement")
                                : Usability.Usable),
                        // Can't reposition incorporeals
                        new CombatManeuverCorporealityCreatureTargetingRequirement(),
                        // Use the same size limitation scaling as other maneuvers
                        new TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement(),
                        // Adjacent or within reach of Grapple weapon
                        MeleeReachCreatureTargetingRequirement.WithWeaponOfTrait(Trait.Grapple),
                        // Must have a free hand, holding the target, or be wielding a grapple weapon
                        new LegacyCreatureTargetingRequirement((a, d) =>
                        {
                            // If you are holding the target, it is usable.
                            if (a.HeldItems.Any(item => item.Grapplee == d))
                                return Usability.Usable;
                            // For non-grappled targets, you must have a free hand
                            // or wield a grapple weapon.
                            // This is a NotUsableOnThisCreature instead of NotUsable
                            // reason to prevent weird usability interactions.
                            if (!a.HasFreeHand && !a.WieldsItem(Trait.Grapple))
                                return Usability.NotUsableOnThisCreature(Usability.CommonReasons
                                    // It's a good idea to reuse this text for interactability
                                    // with removing or ignoring requirements.
                                    .NoFreeHandForManeuver.UnusableReason);
                            return Usability.Usable;
                        }),
                        // Must at least have open spaces for a critical success
                        new LegacyCreatureTargetingRequirement((a, d) =>
                            TilesYouCanBeRepositionedTo(a, d, 2).Count < 1
                                ? Usability.NotUsableOnThisCreature("no space to move")
                                : Usability.Usable
                            )
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
                    action.Owner.RemoveAllQEffects(qf => qf == improve);
                return result.TooltipDescription;
            })
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, caster, targets) =>
            {
                if (targets.ChosenCreature is not { } target)
                {
                    action.RevertRequested = true;
                    return;
                }
                
                if (TilesYouCanBeRepositionedTo(caster, target, 1).Count == 0
                    && TilesYouCanBeRepositionedTo(caster, target, 2).Count > 0)
                {
                    action.RevertRequested = !await caster.AskForConfirmation(
                        action.Illustration,
                        $$"""
                        {b}Reposition {icon:Action}{/b}
                        {{target.Illustration.IllustrationAsIconString}}{{target.ToColoredName()}} cannot be moved anywhere except on a {Red}{b}Critical Success{/b}{/Red}.
                        """,
                        "Attempt reposition anyway",
                        "Cancel");
                }
                
                if (targets.ChosenCreature?.FriendOf(action.Owner) ?? false)
                    caster.AddQEffect(new QEffect()
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
                if (action.RevertRequested || result == CheckResult.Failure)
                    return;
                
                await caster.FictitiousSingleTileMoveBack();

                int distance = 1;
                bool randomTile = false;
                bool resolved = false;
                switch (result)
                {
                    case CheckResult.CriticalSuccess:
                        distance = 2;
                        goto case CheckResult.Success;
                    case CheckResult.CriticalFailure:
                        if (!target.FriendOf(caster))
                        {
                            randomTile = true;
                            goto case CheckResult.Success;
                        }
                        break;
                    case CheckResult.Success:
                        resolved = await RepositionCreature(caster, target, distance, result, randomTile);
                        break;
                }

                caster.RemoveAllQEffects(qf => qf.Name is improvName);

                if (!resolved)
                    action.RevertRequested = true;
            });
    }

    public static List<Tile> TilesYouCanBeRepositionedTo(Creature attacker, Creature defender, int distance)
    {
        int reach = GrappleTag.GetGrappleReach(attacker);
        List<Tile> tiles = attacker.Battle.Map.AllTiles
            .Where(tile =>
                !ReferenceEquals(defender.Space.TopLeftTile, tile)
                && tile.IsTrulyGenuinelyFreeTo(defender)
                && defender.Space.TopLeftTile.DistanceTo(tile) <= distance
                && tile
                    .TilesToTheBottomRight(defender.Space.SizeInSquares)
                    .Any(subspace => attacker.DistanceToWith10FeetException(subspace) <= reach))
            .ToList();
        return tiles;
    }
    
    /// <summary>
    /// The ATTACKER moves the DEFENDER a DISTANCE based on a CHECK RESULT, possibly to a random tile.
    /// </summary>
    /// <param name="attacker">The move-ee.</param>
    /// <param name="defender">The mover.</param>
    /// <param name="distance">The maximum allowed distance (such as up to 2 tiles).</param>
    /// <param name="result">The check result of the action doing the moving. This is only used to allow a reposition to fully revert if a critical success lacked available spaces.</param>
    /// <param name="randomTile">Whether the movement should choose a tile to move to, or be chosen at random.</param>
    /// <returns>Whether the action resolved without errors. On a FALSE result, the action should be reverted or this function should execute again.</returns>
    public static async Task<bool> RepositionCreature(Creature attacker, Creature defender, int distance, CheckResult result, bool randomTile = false)
    {
        List<Tile> tiles = TilesYouCanBeRepositionedTo(attacker, defender, distance);
        if (tiles.Count == 0)
        {
            bool reverted = result == CheckResult.CriticalSuccess;
            attacker.Overhead("*no valid tiles*", Color.Red, $"Reposition {(reverted ? "reverted" : "failed")}: {(reverted ? "No possible free spaces" : "No free spaces on a success")}.");
            // If false, this action should revert or try again.
            // This should only occur if even a critical success lacked any open spaces,
            // as you sometimes have to gamble on wasting a success.
            // ReSharper disable once SimplifyConditionalTernaryExpression
            return !reverted;
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
                "Confirm no reposition");
        
        if (moveTo is null)
            return true;
        
        await defender.MoveTo(moveTo, null,
            new MovementStyle()
            {
                ForcedMovement = true,
                Shifting = true,
                ShortestPath = true,
                MaximumSquares = 20
            });

        return true;
    }
}