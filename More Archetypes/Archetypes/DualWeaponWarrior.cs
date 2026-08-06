using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class DualWeaponWarrior
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Rebuild Dual-Weapon Warrior.
        // Users have to switch the dedication, not just individual archetype feats
        // ArchetypeFeats.CreateOrUpdateDedication
        Feat dwwArchetype = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.DualWeaponWarrior,
                "You're exceptional in your use of two weapons.",
                "You gain the Double Slice fighter feat.")
            .WithRulesBlockForCombatAction(cr =>
                CombatAction.CreateSimple(
                        cr,
                        "Double Slice",
                        Trait.Fighter)
                    .WithDescription(
                        "You lash out at your foe with both weapons.",
                        """
                        Make two Strikes against the same target, one with each of your two melee weapons, each using your current multiple attack penalty.

                        If the second Strike is made with a non-agile weapon it takes a –2 penalty. Combine the damage for the purposes of weakness and resistance. This counts as two attacks when calculating your multiple attack penalty.
                        """)
                    .WithActionCost(2)
                    .With(ca => ca.Illustration = new SideBySideIllustration(
                        IllustrationName.Dagger,
                        IllustrationName.Dagger)))
            .WithOnSheet(sheet => sheet.GrantFeat(FeatName.DoubleSlice));
        ModData.FeatNames.DualWeaponWarriorDedication = dwwArchetype.FeatName;
        yield return dwwArchetype;
        
        // Dual Thrower
        yield return new TrueFeat(
                ModData.FeatNames.DualThrower, 4,
                "You know how to throw two weapons as easily as strike with them.",
                "Whenever a feat you gained from the dual-weapon warrior archetype allows you to make a melee Strike, you can instead make a ranged Strike with a thrown weapon or a one-handed ranged weapon you are wielding. Any effects from these feats that apply to one-handed melee weapons or melee Strikes also apply to one-handed ranged weapons and ranged Strikes.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.DualWeaponWarrior)
            .WithPermanentQEffect(
                "You can use double slice and other Dual-Weapon Warrior actions with ranged weapons.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.HeldItems.Count(item =>
                                ItemIsDualCompatible(item, true) is not null)
                            < 2)
                            return null;
                        // Create strikes, use their targeting
                        Target strike1 = qfThis.Owner.CreateStrike(qfThis.Owner.HeldItems[0]).Target;
                        Target strike2 = qfThis.Owner.CreateStrike(qfThis.Owner.HeldItems[1]).Target;
                        // If throwable, replace with thrown targeting
                        if (qfThis.Owner.HeldItems[0].WeaponProperties?.Throwable ?? false)
                            strike1 = StrikeRules.CreateStrike(
                                qfThis.Owner,
                                qfThis.Owner.HeldItems[0],
                                RangeKind.Ranged,
                                -1, true)
                                .Target;
                        if (qfThis.Owner.HeldItems[1].WeaponProperties?.Throwable ?? false)
                            strike2 = StrikeRules.CreateStrike(
                                qfThis.Owner,
                                qfThis.Owner.HeldItems[1],
                                RangeKind.Ranged,
                                -1, true)
                                .Target;
                        CombatAction doubleThrow = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(
                                    IllustrationName.Throw,
                                    IllustrationName.Throw),
                                "Double Slice (throw)",
                                [Trait.Basic, Trait.AlwaysHits, Trait.IsHostile, ModData.ModTrait, Trait.Archetype, Trait.Fighter],
                                null!,
                                (strike1 as CreatureTarget)!
                                .WithAdditionalConditionOnTargetCreature((a, d) =>
                                {
                                    if (!strike2.CanBeginToUse(a))
                                        return Usability.NotUsable("You must be able to make a strike.");
                                    if (!((CreatureTarget)strike2).IsLegalTarget(a, d))
                                        return Usability.NotUsableOnThisCreature(
                                            "The target must be in range of both weapons.");
                                    return Usability.Usable;
                                }))
                            .WithDescription(
                                "You lash out at your foe with both weapons.",
                                "Make two Strikes against the same target, one with each of your two weapons, each using your current multiple attack penalty.\n\nIf the second Strike is made with a non-agile weapon it takes a –2 penalty. Combine the damage for the purposes of weakness and resistance. This counts as two attacks when calculating your multiple attack penalty."
                                + "\n\n{b}Special{/b} If the weapons used can be thrown, they will be thrown.")
                            .WithActionCost(2)
                            .WithTargetingTooltip(
                                (_, _, _) => "Make two Strikes against the same target, one with each of your two weapons, each using your current multiple attack penalty.\n\nIf the second Strike is made with a non-agile weapon it takes a –2 penalty. Combine the damage for the purposes of weakness and resistance. This counts as two attacks when calculating your multiple attack penalty." + "\n\n{b}Special{/b} If the weapons used can be thrown, they will be thrown.")
                            .WithEffectOnChosenTargets(async (action, caster, targets) =>
                            {
                                int map = caster.Actions.AttackedThisManyTimesThisTurn;
                                if (targets.ChosenCreature is not { } enemy)
                                {
                                    action.RevertRequested = true;
                                    return;
                                }

                                QEffect dsPenalty = new QEffect(
                                    "Double Slice penalty",
                                    "[NO DESCRIPTION]",
                                    ExpirationCondition.Never,
                                    caster,
                                    IllustrationName.None)
                                {
                                    BonusToAttackRolls = (_, ca, _) =>
                                        !ca.HasTrait(Trait.Agile)
                                            ? new Bonus(-2, BonusType.Untyped, "Double Slice penalty")
                                            : null,
                                };
                                Item first = caster.HeldItems[0];
                                Item second = caster.HeldItems[1];
                                bool firstThrown = first.WeaponProperties?.Throwable ?? false;
                                bool secondThrown = second.WeaponProperties?.Throwable ?? false;
                                CombatAction throw1 = StrikeRules
                                    .CreateStrike(qfThis.Owner, first, RangeKind.Ranged, map, true)
                                    .WithActionCost(0);
                                CombatAction throw2 = StrikeRules
                                    .CreateStrike(qfThis.Owner, second, RangeKind.Ranged, map, true)
                                    .WithActionCost(0);
                                if (!firstThrown)
                                    await caster.MakeStrike(enemy, first, map);
                                else
                                    await caster.Battle.GameLoop.FullCast(throw1, ChosenTargets.CreateSingleTarget(enemy));

                                caster.AddQEffect(dsPenalty);
                                if (!secondThrown)
                                    await caster.MakeStrike(enemy, second, map);
                                else
                                    await caster.Battle.GameLoop.FullCast(throw2, ChosenTargets.CreateSingleTarget(enemy));
                                dsPenalty.ExpiresAt = ExpirationCondition.Immediately;
                            });
                        return new ActionPossibility(doubleThrow)
                            .WithPossibilityGroup("Abilities");
                    };
                });
        
        // Quick Draw
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.QuickDraw, ModData.Traits.DualWeaponWarrior, 4);

        // Twin Parry
        if (ModManager.TryParse("Twin Parry", out FeatName twinParry1))
            yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
                twinParry1, ModData.Traits.DualWeaponWarrior, 6);
        if (ModManager.TryParse("TwinParry", out FeatName twinParry2))
            yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
                twinParry2, ModData.Traits.DualWeaponWarrior, 6);
        
        // Flensing Slice
        yield return new TrueFeat(
                ModData.FeatNames.FlensingSlice, 8,
                "When you hit with both attacks with Double Slice, you flense the target, making it bleed and creating a weak spot.", 
                """
                {b}Requirements{/b} Your last action was a Double Slice, and both attacks hit the target.

                The target takes 1d8 persistent bleed damage per weapon damage die of whichever of the weapons you used that has the most weapon damage dice (maximum 4d8 for a major striking weapon).

                The target also becomes off-guard and reduces its physical damage resistances (if any) by 5 until the start of your next turn.
                """,
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.DualWeaponWarrior)
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (!action.Name.StartsWith("Double Slice"))
                        return;
                    List<Item> weapons = qfThis.Owner.HeldItems.ToList();
                    QEffect flenseCounter = qfThis.Owner.FindQEffect(ModData.QEffectIds.FlenseCounter) ?? new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                    {
                        AfterYouTakeActionAgainstTarget = async (qfThis2, action2, _, result) =>
                        {
                            if (!action2.HasTrait(Trait.Strike)
                                || result <= CheckResult.Failure)
                                return;
                            qfThis2.Value += 1;
                        },
                        Id = ModData.QEffectIds.FlenseCounter,
                        Tag = action
                    };
                    // Reset in the nearly-impossible event of two Double Slices in one turn
                    flenseCounter.Value = 0;
                    qfThis.Owner.AddQEffect(flenseCounter);
                    qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                    {
                        Id = ModData.QEffectIds.FlenseWeapons,
                        Tag = weapons
                    });
                };
                qfFeat.ProvideContextualAction = qfThis =>
                {
                    if (qfThis.Owner.FindQEffect(ModData.QEffectIds.FlenseCounter) is not
                            { Value: 2, Tag: CombatAction { ChosenTargets.ChosenCreature: { } enemy } sliceAction }
                        || qfThis.Owner.Actions.ActionHistoryThisTurn.Last() != sliceAction
                        || qfThis.Owner.FindQEffect(ModData.QEffectIds.FlenseWeapons)?.Tag is not List<Item> weapons
                        || weapons.Count(weapon => weapon.WeaponProperties != null) < 2
                        || !enemy.Alive)
                        return null;
                    
                    int dice = weapons.MaxBy(weapon =>
                        weapon.WeaponProperties?.DamageDieCount)
                        ?.WeaponProperties
                        ?.DamageDieCount ?? 1;

                    CombatAction flense = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.FlensingSlice,
                            "Flensing Slice",
                            [Trait.Basic, ModData.ModTrait, Trait.Archetype],
                            $$"""
                              {b}Requirements{/b} Your last action was a Double Slice, and both attacks hit the target.

                              The target takes {{S.HeightenedVariable(dice, 1)}}d8 persistent bleed damage.

                              The target also becomes off-guard and reduces its physical damage resistances (if any) by 5 until the start of your next turn.
                              """,
                            Target.Self())
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.Boneshaker)
                        .WithEffectOnSelf(async caster =>
                        {
                            enemy.AddQEffect(QEffect.PersistentDamage(dice + "d8", DamageKind.Bleed));
                            QEffect offguard = QEffect.FlatFooted("Flensed")
                                .WithExpirationAtStartOfSourcesTurn(caster, 1);
                            offguard.Name = "Flensed";
                            offguard.Description = offguard.Description?.Replace(".",
                                " and your resistances to all physical damage types are reduced by 5.");
                            offguard.StateCheckLayer = 1; // Ensure this reduces resistances after adding them
                            offguard.StateCheck = qfThis2 =>
                            {
                                Creature owner = qfThis2.Owner;
                                foreach (Resistance resistance in owner.WeaknessAndResistance.Resistances
                                             .Where(resist =>
                                                 resist.DamageKind.IsPhysical()
                                                 || (resist is SpecialResistance spec
                                                 && spec.Name.ToLower().Contains("physical"))))
                                    resistance.Value = Math.Max(0, resistance.Value-5);
                            };
                            offguard.Illustration = ModData.Illustrations.FlensingSlice;
                            enemy.AddQEffect(offguard);
                        });
                    
                    return new ActionPossibility(flense)
                        .WithPossibilityGroup("Abilities"); 
                };
            });
        
        // Dual-Weapon Blitz
        yield return new TrueFeat(
                ModData.FeatNames.DualWeaponBlitz, 10,
                "You attack as you weave your way around the battlefield.",
                """
                {b}Requirements{/b} You are wielding two one handed melee weapons, each in a different hand.

                Stride up to your Speed. During this movement, you can Strike once with each of the two one-handed melee weapons. Each of these Strikes can be made at any point during your movement.
                """,
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.DualWeaponWarrior)
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                /*qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Stride up to your Speed, attacking once with each of your dual weapons at any point during the movement.";*/

                qfFeat.ProvideMainAction = qfThis =>
                {
                    bool hasDualThrower = qfThis.Owner.HasFeat(ModData.FeatNames.DualThrower);
                    
                    CombatAction blitz = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.DualWeaponBlitz,
                            "Dual-Weapon Blitz",
                            [ModData.ModTrait, Trait.Archetype],
                            null!,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                {
                                    if (self.HeldItems.Count < 2)
                                        return "Not dual-wielding";
                                    string? reason = null;
                                    if (ItemIsDualCompatible(self.HeldItems[0], hasDualThrower) is {} leftReason)
                                        reason += "(Left) " + leftReason;
                                    if (ItemIsDualCompatible(self.HeldItems[1], hasDualThrower) is {} rightReason)
                                        reason += (reason is not null ? "; " : null) + "(Right) " + rightReason;
                                    return reason;
                                }))
                        .WithDescription(
                            "You attack as you weave your way around the battlefield.",
                            """
                            {b}Requirements{/b} You are wielding two one handed melee weapons, each in a different hand.

                            Stride up to your Speed. During this movement, you can Strike once with each of the two one-handed melee weapons. Each of these Strikes can be made at any point during your movement.
                            """)
                        .WithShortDescription("Stride up to your Speed, attacking once with each of your dual weapons at any point during the movement.")
                        .WithActionCost(2)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            // Preliminary. Start counting movement taken so far, initialize weapons.
                            QEffect counter = MovementCounter();
                            self.AddQEffect(counter);
                            int movedSoFar = 0;
                            Option chosen;
                            List<Item> weaponsAvailable = [self.HeldItems[0], self.HeldItems[1]];
                            bool canRevert = true;

                            // Do once, showing movement options and strike options.
                            do
                            {
                                movedSoFar += counter.Value;
                                counter.Value = 0; // Reset so it doesn't keep adding
                                
                                // Can fully revert if no attacks and no movement was made.
                                if (weaponsAvailable.Count < 2
                                    || movedSoFar > 0)
                                    canRevert = false;
                                
                                List<Option> options = [
                                    new CancelOption(true),
                                    new PassViaButtonOption(canRevert ? "Cancel" : "Pass")
                                ];
                                
                                // Get the hidden basic Stride from your possibilities
                                CombatAction? moveAction = Possibilities
                                        .Create(self)
                                        .Filter(ap =>
                                        {
                                            if (ap.CombatAction.ActionId != ActionId.Stride)
                                                return false;
                                            ap.CombatAction.ActionCost = 0;
                                            ap.RecalculateUsability();
                                            return true;
                                        })
                                        .CreateActions(true)
                                        .FirstOrDefault(ica =>
                                            ica.Action.ActionId == ActionId.Stride)
                                    as CombatAction;
                                
                                // Add move options to the list
                                if (moveAction is not null
                                    && moveAction.Target.CanBeginToUse(self))
                                {
                                    IList<Tile> floodfill = Pathfinding
                                        .Floodfill(
                                            self, self.Battle,
                                            new PathfindingDescription()
                                            {
                                                Squares = self.Speed - movedSoFar,
                                                Style = new MovementStyle()
                                                    { MaximumSquares = self.Speed - movedSoFar }
                                            })
                                        .Where(tile => tile.LooksFreeTo(self))
                                        .ToList();

                                    options.AddRange(floodfill
                                        .Select(tile => moveAction
                                            .CreateUseOptionOn(tile)
                                            .WithIllustration(moveAction.Illustration))
                                        .ToList());
                                }
                                
                                // Add both weapons to the list
                                foreach (Item weapon in weaponsAvailable)
                                {
                                    // Get strike options that use this weapon
                                    options.AddRange(CommonCombatActions.GetStrikePossibilities(
                                        self,
                                        strike =>
                                            strike.Item == weapon
                                            && (strike.HasTrait(Trait.Melee)
                                                || (strike.HasTrait(Trait.Ranged) && hasDualThrower)),
                                        strike =>
                                            strike.WithEffectOnEachTarget(async (_,_,_,_) =>
                                                weaponsAvailable.Remove(weapon)),
                                        null));
                                }

                                if (options.Count == 2)
                                    chosen = options[0];
                                else
                                    chosen = (await self.Battle.SendRequest(new AdvancedRequest(
                                            self,
                                            "Choose where to move and who to Strike with Dual-Weapon Blitz, or right-click to cancel.",
                                            options)
                                        {
                                            IsMainTurn = false,
                                            IsStandardMovementRequest = true,
                                            TopBarIcon = action.Illustration,
                                            TopBarText = "Choose where to move and who to Strike with Dual-Weapon Blitz, or right-click to cancel."
                                        }))
                                        .ChosenOption;

                                if (chosen is CancelOption or PassViaButtonOption)
                                {
                                    if (canRevert)
                                        action.RevertRequested = true;
                                    else if (weaponsAvailable.Count == 1 && movedSoFar == 0)  // Only struck once
                                    {
                                        action.RevertRequested = true;
                                        action.SpentActions = 1;
                                        self.Battle.Log("Dual-Weapon Blitz converted into a simple Strike.");
                                    }
                                    else if (weaponsAvailable.Count == 2 && movedSoFar > 0) // Only moved
                                    {
                                        action.RevertRequested = true;
                                        action.SpentActions = 1;
                                        self.Battle.Log("Dual-Weapon Blitz converted into a simple Stride.");
                                    }
                                    return;
                                }

                                await chosen.Action();
                            }
                            // End loop whenever you cancel it.
                            while (chosen is not CancelOption and not PassViaButtonOption);

                            counter.ExpiresAt = ExpirationCondition.Immediately;
                        });

                    return new ActionPossibility(blitz);
                };
            });
        
        // Twin Riposte (Level 12, really 10)
        
        // Dual Onslaught (Level 14)
        yield return new TrueFeat(
            ModData.FeatNames.DualOnslaught, 14,
            "When you lash out with both weapons, you leave no room for the target to escape your attack.",
            "When you use Double Slice, if you miss with both Strikes, choose any one of the two weapons that didn't critically fail and apply the effects of a hit with that weapon.",
            [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.DualWeaponWarrior)
            .WithPermanentQEffect(
                "When you Double Slice, you can hit once when you would miss (but not critically fail) twice.",
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        // Stop tracking Strikes
                        if (action.Name.ToLower().Contains("double slice"))
                        {
                            List<CombatAction> strikes = qfThis.Owner.Actions
                                .ActionHistoryThisTurn
                                .Where(ca =>
                                    ca.HasTrait(Trait.Strike)
                                    && !ca.HasAnyTraits([Trait.ReactiveAttack, Trait.AttackOfOpportunity]))
                                .ToList();
                            
                            // Safety
                            if (strikes.Count < 2)
                                return;

                            // Get first two Strikes, if more than one by any means.
                            strikes = strikes.GetRange(0, 2);

                            // Must have missed both Strikes, and have not fumbled at least one.
                            foreach (CombatAction strike_iter in strikes.ToList())
                            {
                                if (strike_iter.CheckResult > CheckResult.Failure)
                                    strikes.Clear();
                                else if (strike_iter.CheckResult < CheckResult.Failure)
                                    strikes.Remove(strike_iter);
                            }

                            if (strikes.Count == 0)
                                return;

                            int choice = (await qfThis.Owner.AskForChoiceAmongButtons(
                                IllustrationName.Swords,
                                "{b}Dual Onslaught{/b}\nYou missed with both Strikes using Double Slice. Choose one that didn't critically miss to apply the effects of a hit.",
                                strikes.Select(strike => strike.Illustration.IllustrationAsIconString + (strike.Item?.Name ?? "Unknown"))
                                    .Append("Pass")
                                    .ToArray())).Index;

                            if (choice == 3)
                                return;

                            qfThis.Owner.Battle.Log(
                                qfThis.Owner.Name + " uses {b}Dual Onslaught{/b} to apply the effects of a hit with one of their weapons.",
                                "Dual Onslaught",
                                "{i}When you lash out with both weapons, you leave no room for the target to escape your attack.{/i}\n\nWhen you use Double Slice, if you miss with both Strikes, choose any one of the two weapons that didn't critically fail and apply the effects of a hit with that weapon.",
                                new Traits([ModData.ModTrait, Trait.Archetype]));
                            CombatAction strike = strikes[choice];
                            await strike.EffectOnOneTarget!.Invoke(strike, qfThis.Owner, strike.ChosenTargets.ChosenCreature!, CheckResult.Success);
                        }
                        
                    };
                });

        /* Higher Level Feats
         * @16 (really: 14) Improved Twin Riposte
         * @16 (really: 14) Two-Weapon Flurry
         * @18 (really: 16) Twinned Defense
         */
    }

    public static string? ItemIsDualCompatible(Item weapon, bool hasDualThrower)
    {
        if (!weapon.HasTrait(Trait.Weapon))
            return "Not a weapon";
        if (weapon.WieldedInTwoHands || weapon.HasTrait(Trait.OneHandPlus))
            return "Not a one-handed weapon";
        if (!weapon.HasTrait(Trait.Melee))
        {
            if (hasDualThrower)
            {
                if (!(weapon.WeaponProperties?.Throwable ?? false)
                    && !weapon.HasTrait(Trait.Ranged))
                {
                    return "Not a melee, ranged, or throwable weapon";
                }
            }
            else
                return "Not a melee weapon";
        }
        return null;
    }
    
    // Code courtesy of SilchasRuin
    public static QEffect MovementCounter()
    {
        return new QEffect()
        {
            StateCheckWithVisibleChanges = async qfThis =>
            {
                Creature innerSelf = qfThis.Owner;
                if (innerSelf.AnimationData.LongMovement?.Path == null)
                    return;
                var move = 0;
                var diagonals = 0;
                for (var index = 0;
                     index < innerSelf.AnimationData.LongMovement.Path
                         .Count;
                     index++)
                {
                    Tile tile =
                        innerSelf.AnimationData.LongMovement.Path[index];
                    List<Tile> tiles = innerSelf.AnimationData.LongMovement.Path.ToList();
                    if (tile.GetWalkDifficulty(innerSelf) >= 1)
                        move += tile.GetWalkDifficulty(innerSelf);
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
                                    innerSelf.AnimationData.LongMovement.OriginalTile) ||
                                Equals(tile.Neighbours.BottomRight?.Tile,
                                    innerSelf.AnimationData.LongMovement.OriginalTile) ||
                                Equals(tile.Neighbours.TopLeft?.Tile,
                                    innerSelf.AnimationData.LongMovement.OriginalTile) ||
                                Equals(tile.Neighbours.TopRight?.Tile,
                                    innerSelf.AnimationData.LongMovement.OriginalTile))
                                diagonals += 1;
                            break;
                        }
                    }
                }
                if (diagonals > 1)
                    move += diagonals / 2;
                
                qfThis.Value = move;
            },
            Id = ModData.QEffectIds.MovementCounter
        };
    }
}