using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
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
        // PETR: When grappling is more well supported, expand logic for "what is my reach"
        // and what is the reach of the weapon or free hand that is doing the repositioning.
        // At minimum, if you have a creature grappled first, you should be able to reposition
        // it with that weapon.
        
        return new CombatAction(
                owner,
                ModData.Illustrations.Reposition,
                "Reposition",
                [Trait.Basic, ModData.Traits.MoreBasicActions, Trait.Attack, Trait.AttackDoesNotTargetAC],
                "{i}You forcefully relocate a creature.{/i}\n\n{b}Requirements{/b} You have a free hand or are holding the target.\n\nMake an Athletics check against the target's Fortitude DC."
                + S.FourDegreesOfSuccess("You move the creature up to 10 feet along any unobstructed path within your reach.",
                "As critical success, but you move the creature 5 feet.",
                null,
                "The target Repositions you to a random square instead, as if a success.")
                + "\n\n{b}Special{/b} You automatically get one degree of success better when targeting an ally with Reposition.",
                /*new CreatureTarget(
                    RangeKind.Melee,
                    [
                        new GrappleCreatureTargetingRequirement(),
                        new LegacyCreatureTargetingRequirement((_,d) =>
                            d.WeaknessAndResistance.ImmunityToForcedMovement
                                ? Usability.NotUsableOnThisCreature("immune to forced movement")
                                : Usability.Usable)
                    ],
                    (_,_,_) => int.MinValue)*/
                Target.AdjacentCreature()
                    .WithAdditionalConditionOnTargetCreature((_,d) =>
                        d.WeaknessAndResistance.ImmunityToForcedMovement
                            ? Usability.NotUsableOnThisCreature("immune to forced movement")
                            : Usability.Usable))
            .WithActionCost(1)
            .WithSoundEffect(SfxName.Shove)
            .WithActionId(ModData.ActionIds.Reposition)
            .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics),
                TaggedChecks.DefenseDC(Defense.Fortitude)))
            .WithEffectOnEachTarget(async (_,caster, target, result) =>
            {
                if (target.FriendOf(caster))
                    result.ImproveByOneStep();
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
                        await ExecuteRepositionLogic(target, caster, 1, true);
                        break;
                }
            });
    }
    
    public static async Task ExecuteRepositionLogic(Creature attacker, Creature defender, int distance, bool randomTile = false)
    {
        int reach = 1 /*attacker.MeleeWeapons.Any(item => item.HasTrait(Trait.Reach))
            ? 2
            : 1*/;
        List<Tile> tiles = attacker.Battle.Map.AllTiles
            .Where(tile =>
                tile.IsTrulyGenuinelyFreeTo(defender)
                && tile.DistanceTo(defender.Occupies) <= distance
                && tile.DistanceToReachSpecial(attacker.Occupies) <= reach)
            .ToList();
        Tile? moveTo = null;
        if (randomTile)
            moveTo = tiles.GetRandomForAi();
        else
            moveTo = await attacker.Battle.AskToChooseATile(
                attacker, tiles,
                ModData.Illustrations.Reposition, //IllustrationName.GenericCombatManeuver,
                "Choose where to relocate " + defender.Name + ".",
                "",
                false, false);
        if (moveTo is not null)
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