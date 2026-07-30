using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Mauler
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Mauler Dedication.
        Feat maulerDedication = ArchetypeFeats.CreateOrUpdateDedication(
                ModData.Traits.Mauler,
                "You shove your way through legions of foes, knock enemies on all sides to the ground, and deal massive blows to anyone or anything that comes near.",
                $$"""
                  You have {{ModData.Tooltips.CommonWeaponFamiliarity("familiarity")}} with mauler weapons — melee weapons that require two hands to wield or that have the two-hand trait.

                  While you're at least expert in a mauler weapon, it triggers {tooltip:criteffect}critical specialization effects{/}.
                  """,
                null,
                dedication =>
                {
                    foreach (Prerequisite req in dedication.Prerequisites
                                 .Where(req =>
                                     req.Description.Contains("14 or more"))
                                 .ToList())
                        dedication.Prerequisites.Remove(req);
                })
            .WithOnSheet(values =>
            {
                foreach (Trait trait in (Trait[])[Trait.TwoHanded, Trait.TwoHand1d8, Trait.TwoHand1d10,Trait.TwoHand1d12])
                {
                    values.Proficiencies.Autoupgrade(
                        [Trait.Simple],
                        [Trait.Martial, Trait.Melee, trait]);
                    values.Proficiencies.Autoupgrade(
                        [Trait.Martial],
                        [Trait.Advanced, Trait.Melee, trait]);
                }
            })
            .WithPermanentQEffect(
                "You trigger {tooltip:criteffect}critical specialization effects{/} with mauler weapons you're at least expert with.",
                qfFeat =>
                {
                    qfFeat.YouHaveCriticalSpecialization = (qfThis, weapon, _, _) =>
                        IsMaulerWeapon(weapon)
                        && qfThis.Owner.Proficiencies.Get(weapon.Traits) >= Proficiency.Expert;
                })
            // ORC-like ability requirement
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Multitalented)
                    && values.Ancestries.Contains(Trait.HalfElf)
                    || values.FinalAbilityScores.TotalScore(Ability.Strength) >= 14,
                $"You must have {nameof(Ability.Strength)} +2 or more.");
        ModData.FeatNames.MaulerDedication = maulerDedication.FeatName;
        yield return maulerDedication;
        
        // Lv4: Slam Down for Mauler
        TrueFeat slamDownForMauler = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.Knockdown, ModData.Traits.Mauler, 4);
        //slamDownForMauler.CustomName = "Slam Down {icon:TwoActions}";
        ModData.FeatNames.SlamDownForMauler = slamDownForMauler.FeatName;
        yield return slamDownForMauler;

        // Lv4: Vicious Swing for Mauler
        TrueFeat viciousSwingForMauler = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.PowerAttack, ModData.Traits.Mauler, 4);
        //viciousSwingForMauler.CustomName = "Vicious Swing {icon:TwoActions}";
        ModData.FeatNames.ViciousSwingForMauler = viciousSwingForMauler.FeatName;
        yield return viciousSwingForMauler;

        // Lv6: Clear the Way
        // DOC: This explicitly uses the two-handed weapon to make the shoves.
        yield return new TrueFeat(
                ModData.FeatNames.ClearTheWay, 6,
                "You put your body behind your massive weapon and swing, shoving enemies to clear a wide path.",
                """
                {b}Requirements{/b} You're wielding a melee weapon in two hands.

                You attempt to Shove up to five creatures adjacent to you using the required weapon, rolling a separate Athletics check for each target and ignoring the requirement that you have a free hand. You can't Stride as part of the Shove. Your multiple attack penalty increases normally after all Shoves are made.
                
                Then Stride up to half your Speed. This movement doesn't trigger reactions from any of the creatures you successfully Shoved.
                """,
                [])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Mauler)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Attempt to Shove up to 5 adjacent creatures, then Stride half your speed, and don't provoke reactions from each success.";
                
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee)
                        || item is not { TwoHandCapable: true, EphemeralItemProperties.SingleGrip: false })
                        return null;
                    
                    CombatAction clearTheWay = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(
                                IllustrationName.Shove,
                                IllustrationName.FleetStep ),
                            "Clear the Way",
                            [ModData.ModTrait, Trait.Archetype],
                            null!,
                            Target.MultipleCreatureTargets(Target.Touch(), Target.Touch(), Target.Touch(), Target.Touch(), Target.Touch())
                                .WithMinimumTargets(1)
                                .WithMustBeDistinct())
                        .WithDescription(
                            "You put your body behind your massive weapon and swing, shoving enemies to clear a wide path.",
                            """
                            {b}Requirements{/b} You're wielding a melee weapon in two hands.

                            You attempt to Shove up to five creatures adjacent to you, rolling a separate Athletics check for each target and ignoring the requirement that you have a free hand. Your multiple attack penalty increases normally after all Shoves are made.

                            Then Stride up to half your Speed. This movement doesn't trigger reactions from any of the creatures you successfully Shoved.
                            """)
                        .WithActionCost(2)
                        .WithTargetingTooltip((action, target, _) =>
                        {
                            CombatAction shoveAction = CombatManeuverPossibilities.CreateShoveAction(action.Owner, item);
                            return CombatActionExecution
                                .BreakdownAttackForTooltip(shoveAction, target)
                                .TooltipDescription;
                        })
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            // Can't Stride until after all Shoves are complete
                            QEffect noStride = new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                            {
                                PreventTakingAction = action2 => action2.HasTrait(Trait.Move) ? "Can't Stride until all Shoves are complete" : null
                            };
                            caster.AddQEffect(noStride);
                            
                            // Cache MAP, adjust later
                            int cachedMAP = caster.Actions.AttackedThisManyTimesThisTurn;
                            
                            // QEffects to remove after activity is complete
                            List<QEffect> removeAll = [];
                            
                            // Shove each target
                            foreach (Creature cr in targets.ChosenCreatures)
                            {
                                CombatAction shoveAction = CombatManeuverPossibilities.CreateShoveAction(caster, item)
                                    .WithActionCost(0)
                                    // Don't increase MAP
                                    .WithExtraTrait(Trait.AttackDoesNotIncreaseMultipleAttackPenalty)
                                    .WithEffectOnEachTarget(async (_, _, target, result) =>
                                    {
                                        if (result < CheckResult.Success)
                                            return;
                                        QEffect noMoveReactionsFromShove = new QEffect(
                                            "Shoved by Clear the Way",
                                            "Cannot take reactions against Clear the Way's Stride.",
                                            ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                                            caster,
                                            IllustrationName.ReactionUsedUp)
                                        {
                                            Source = caster,
                                            Id = QEffectId.CannotTakeReactions
                                        };
                                        removeAll.Add(noMoveReactionsFromShove);
                                        target.AddQEffect(noMoveReactionsFromShove);
                                    });
                                
                                if (((CreatureTarget)shoveAction.Target).CreatureTargetingRequirements.FirstOrDefault(req =>
                                        req.Satisfied(caster, cr) == Usability.CommonReasons.NoFreeHandForManeuver)
                                    is { } freeHandReq)
                                    ((CreatureTarget)shoveAction.Target).CreatureTargetingRequirements.Remove(freeHandReq);

                                await caster.Battle.GameLoop.FullCast(shoveAction, ChosenTargets.CreateSingleTarget(cr));
                            }
                            
                            // Increase MAP after
                            caster.Actions.AttackedThisManyTimesThisTurn = cachedMAP + targets.ChosenCreatures.Count;
                            
                            // Stride
                            caster.RemoveAllQEffects(qf => qf == noStride);
                            
                            if (!await caster.StrideAsync(
                                    "Stride up to half your speed. This movement doesn't trigger reactions from any of the creatures you successfully Shoved.",
                                    allowStep: false,
                                    allowCancel: false,
                                    allowPass: true,
                                    maximumHalfSpeed: true)
                                // Allow the user to revert if they had a shove weapon and
                                // only shoved one creature.
                                && targets.ChosenCreatures.Count == 1
                                && item.HasTrait(Trait.Shove))
                            {
                                caster.Battle.Log("Clear the Way was converted to a simple Shove.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                            }

                            // Remove disabled-reactions
                            foreach (QEffect qf in removeAll)
                                qf.ExpiresAt = ExpirationCondition.Immediately;
                        });

                    return clearTheWay;
                };
            });
        
        // Lv8: Shoving Sweep
        yield return new TrueFeat(
                ModData.FeatNames.ShovingSweep,
                8,
                "You swing your weapon at a fleeing foe, rebuffing them back.",
                """
                {b}Requirements{/b} You're wielding a melee weapon in two hands.
                {b}Trigger{/b} An enemy within your reach leaves a square during a move action it's using.

                Attempt to Shove the triggering creature, ignoring the requirement that you have a hand free. On a critical success, you disrupt that action; otherwise the movement continues.
                """,
                [])
            .WithActionCost(-2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Mauler)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " When a creature within your reach moves to leave a square, you can Shove it.";
                
                qfFeat.Id = QEffectId.AttackOfOpportunity;
                qfFeat.WhenProvokedReactions = (qfThis, provokingAction) =>
                {
                    // Only specific triggers:
                    // - Must be using a move action,
                    // - Must be in the process of moving through spaces
                    if (!provokingAction.HasTrait(Trait.Move)
                        || provokingAction.Owner.AnimationData.LongMovement?.Path is not { Count: > 0 })
                        return null;

                    ReactionOptions shoves = [];
                    foreach (Item weapon in qfThis.Owner.MeleeWeapons
                                 .Where(weapon => weapon.WieldedInTwoHands))
                    {
                        CombatAction shoveAction = CombatManeuverPossibilities
                                .CreateShoveAction(qfThis.Owner, weapon)
                                .WithActionCost(0)
                                .WithExtraTrait(Trait.AttackDoesNotIncreaseMultipleAttackPenalty);
                    
                            // Remove free hand requirement
                            if (((CreatureTarget)shoveAction.Target).CreatureTargetingRequirements.FirstOrDefault(req =>
                                    req.Satisfied(qfThis.Owner, provokingAction.Owner) == Usability.CommonReasons.NoFreeHandForManeuver)
                                is { } freeHandReq)
                                ((CreatureTarget)shoveAction.Target).CreatureTargetingRequirements.Remove(freeHandReq);

                            if (!shoveAction.CanBeginToUse(qfThis.Owner))
                                return null;

                            CombatAction shovingSweep = new CombatAction(
                                    qfThis.Owner,
                                    shoveAction.Illustration,
                                    "Shoving Sweep",
                                    [ModData.ModTrait, Trait.Archetype],
                                    null!,
                                    Target.Self())
                                .WithActionCost(-2)
                                .WithDescription(
                                    "You swing your weapon at a fleeing foe, rebuffing them back.",
                                    """
                                    {b}Requirements{/b} You're wielding a melee weapon in two hands.
                                    {b}Trigger{/b} An enemy within your reach leaves a square during a move action it's using.

                                    Attempt to Shove the triggering creature, ignoring the requirement that you have a hand free. On a critical success, you disrupt that action; otherwise the movement continues.
                                    """)
                                .WithEffectOnEachTarget(async (_, _, _, _) =>
                                {
                                    // Cache movement/speed info before shove
                                    int speed = provokingAction.Owner.Speed;
                                    int movedSoFar = CountMovement(provokingAction.Owner.AnimationData.LongMovement);
                                    int remaining = speed - movedSoFar;

                                    // Shove provoking creature
                                    await shoveAction.Fullcast(provokingAction.Owner);

                                    // A failure won't move the target, and a critical success disrupts.
                                    // Result must be a success to resume their movement.
                                    if (shoveAction.CheckResult != CheckResult.Success)
                                        return;

                                    // Delay so that movement properly ends before beginning new movement.
                                    provokingAction.Owner.AddQEffect(new QEffect()
                                    {
                                        Name = "[SHOVING SWEEP: DELAYED RESUME OF ACTION]",
                                        AfterYouTakeAction = async (qfWait, _) =>
                                        {
                                            qfWait.ExpiresAt = ExpirationCondition.Immediately;
                                            qfThis.Owner.Battle.Log("Shoving Sweep: Movement resumed due to non-critical success.");
                                            
                                            // I have no idea what this is if not a tile target.
                                            if (provokingAction.Target is not TileTarget target)
                                            {
                                                qfThis.Owner.Battle.Log("Shoving Sweep failed to resume target's movement: Triggering action does not have a TileTarget.");
                                                return;
                                            }

                                            // Restrict this action to within the remaining movement.
                                            // Results may be off by a diagonal. Close enough.
                                            target.WithAdditionalTargetingRequirement((mover, tile) =>
                                                mover.DistanceTo(tile) <= remaining
                                                    ? Usability.Usable
                                                    : Usability.NotUsableOnThisCreature("Not enough movement remaining"));

                                            // Clear targeting so that new targets will be chosen again.
                                            provokingAction.ChosenTargets.ChosenTile = null;
                                            provokingAction.ChosenTargets.ChosenTiles.Clear();
                                            provokingAction.ActionCost = 0; // Free
                                            provokingAction.Disrupted = false; // Will execute again
                                            
                                            await provokingAction.Owner.Battle.GameLoop.FullCast(provokingAction);
                                        }
                                    });

                                });

                            ReactionOption reactOpt = ReactionOption.WrapFullcastWithChosenTargets(
                                shovingSweep,
                                ChosenTargets.CreateSingleTarget(provokingAction.Owner),
                                $"Shove {provokingAction.Owner.ToColoredName()} with your {weapon.ShortName}.");

                            shoves.Add(reactOpt);
                    }

                    return shoves;
                };
            })
            .WithPrerequisite(
                values => values.GetProficiency(Trait.Athletics) >= Proficiency.Expert,
                "You must be expert in Athletics.");

        // Lv12: Add Crashing Slam to Mauler
        TrueFeat crashingSlamForMauler = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.ImprovedKnockdown, ModData.Traits.Mauler, 12);
        ModData.FeatNames.CrashingSlamForMauler = crashingSlamForMauler.FeatName;
        yield return crashingSlamForMauler;
        
        // PETR: Lv14: Brutal Finish
        
        // TODO: Lv14: Hammer Quake
        
        // TODO: Lv14: Unbalancing Sweep
        // // Inspect existing feat:
        // // FeatName.UnbalancingSweep
        
        // TODO: Lv16: Avalanche Strike
    }

    public static bool IsMaulerWeapon(Item item)
    {
        return item.HasTrait(Trait.Melee)
               && (item.HasTrait(Trait.TwoHanded)
                   || item.HasTrait(Trait.TwoHand1d8)
                   || item.HasTrait(Trait.TwoHand1d10)
                   || item.HasTrait(Trait.TwoHand1d12));
    }

    // Based on code by SilchasRuin
    public static int CountMovement(LongMovement? longMove)
    {
        if (longMove?.Path?.Count is 0 or null)
            return 0;
        
        Creature mover = longMove.Creature;
        IList<Tile> path = longMove.Path.ToList();
        // If current position is found in the path
        if (path.IndexOf(mover.Space.TopLeftTile) is var last and > -1)
        {
            // Then don't count the rest of the path that hasn't been traversed
            path = path.Take(Math.Min(last + 1, path.Count - 1)).ToList();
        }
        else // Otherwise, movement just began so don't count the whole path
            return 0;
        
        var move = 0;
        var diagonals = 0;
        for (var index = 0;
             index < path.Count;
             index++)
        {
            Tile tile = path[index];
            List<Tile> tiles = path.ToList();
            if (tile.GetWalkDifficulty(mover) >= 1)
                move += tile.GetWalkDifficulty(mover);
            switch (index)
            {
                case >= 1 when tiles.Count > 1:
                {
                    if (Equals(tile.Neighbours.BottomLeft?.Tile,
                            tiles[index - 1]) ||
                        Equals(tile.Neighbours.BottomRight?.Tile,
                            tiles[index - 1]) ||
                        Equals(tile.Neighbours.TopLeft?.Tile,
                            tiles[index - 1]) ||
                        Equals(tile.Neighbours.TopRight?.Tile,
                            tiles[index - 1]))
                        diagonals += 1;
                    break;
                }
                case 0 when tiles.Count > 1:
                {
                    if (Equals(tile.Neighbours.BottomLeft?.Tile,
                            longMove.OriginalTile) ||
                        Equals(tile.Neighbours.BottomRight?.Tile,
                            longMove.OriginalTile) ||
                        Equals(tile.Neighbours.TopLeft?.Tile,
                            longMove.OriginalTile) ||
                        Equals(tile.Neighbours.TopRight?.Tile,
                            longMove.OriginalTile))
                        diagonals += 1;
                    break;
                }
            }
        }
        if (diagonals > 1)
            move += diagonals / 2;
        
        return move;
    }
}