using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;

// Credits to SilchasRuin for providing prewritten code.
public static class ArchetypeDualWeaponWarrior
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        // Archetype
        Feat dwwArchetype = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.DualWeaponWarriorArchetype,
                "You're exceptional in your use of two weapons.",
                "You gain the Double Slice fighter feat.")
            .WithRulesBlockForCombatAction(cr =>
                    CombatAction.CreateSimple(
                            cr,
                            "Double Slice",
                            [Trait.Fighter])
                        .WithDescription(
                            "You lash out at your foe with both weapons.",
                            "Make two Strikes against the same target, one with each of your two melee weapons, each using your current multiple attack penalty.\n\nIf the second Strike is made with a non-agile weapon it takes a –2 penalty. Combine the damage for the purposes of weakness and resistance. This counts as two attacks when calculating your multiple attack penalty.")
                        .WithActionCost(2))
            .WithOnSheet(sheet => sheet.GrantFeat(FeatName.DoubleSlice));
        dwwArchetype.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return dwwArchetype;
        
        // Dual Thrower
        yield return new TrueFeat(
                ModData.FeatNames.DualThrower,
                4,
                "You know how to throw two weapons as easily as strike with them.",
                "Whenever a dual-weapon warrior feat allows you to make a melee Strike, you can instead make a ranged Strike with a thrown weapon or a one-handed ranged weapon you are wielding. Any effects from these feats that apply to one-handed melee weapons or melee Strikes also apply to one-handed ranged weapons and ranged Strikes.",
                [ModData.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(ModData.Traits.DualWeaponWarriorArchetype)
            .WithPermanentQEffect(
                "You can use double slice and other Dual-Weapon Warrior actions with ranged weapons.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.HeldItems.Count(item =>
                                item.HasTrait(Trait.Weapon)) != 2)
                            return null;
                        Target strike1 = qfThis.Owner.CreateStrike(qfThis.Owner.HeldItems[0]).Target;
                        Target strike2 = qfThis.Owner.CreateStrike(qfThis.Owner.HeldItems[1]).Target;
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
                                [Trait.Basic, Trait.AlwaysHits, Trait.IsHostile, ModData.Traits.MoreDedications, Trait.Archetype, Trait.Fighter],
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

        // Twin Parry
        if (ModManager.TryParse("Twin Parry", out FeatName twinParry))
            yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
                twinParry, ModData.Traits.DualWeaponWarriorArchetype, 6);
        
        // Flensing Slice
        yield return new TrueFeat(
                ModData.FeatNames.FlensingSlice,
                8,
                "When you hit with both attacks with Double Slice, you flense the target, making it bleed and creating a weak spot.", 
                "{b}Requirements{/b} Your last action was a Double Slice, and both attacks hit the target.\n\nThe target takes 1d8 persistent bleed damage per weapon damage die of whichever of the weapons you used that has the most weapon damage dice (maximum 4d8 for a major striking weapon).\n\nThe target also becomes flat-footed and reduces its physical damage resistances (if any) by 5 until the start of your next turn.",
                [ModData.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(ModData.Traits.DualWeaponWarriorArchetype)
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    qfFeat.YouBeginAction = async (qfThis, action) =>
                    {
                        if (!action.Name.StartsWith("Double Slice"))
                            return;
                        List<Item> weapons = qfThis.Owner.HeldItems.ToList();
                        qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
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
                        });
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
                            || enemy.Alive == false)
                            return null;
                        
                        int dice = weapons.MaxBy(weapon =>
                            weapon.WeaponProperties?.DamageDieCount)
                            ?.WeaponProperties
                            ?.DamageDieCount ?? 1;

                        CombatAction flense = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.FlensingSlice,
                                "Flensing Slice",
                                [Trait.Basic, ModData.Traits.MoreDedications, Trait.Archetype],
                                $"{{b}}Requirements{{/b}} Your last action was a Double Slice, and both attacks hit the target.\n\nThe target takes {dice}d8 persistent bleed damage. \n\nThe target also becomes flat-footed and reduces its physical damage resistances (if any) by 5 until the start of your next turn.",
                                Target.Self())
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.Boneshaker)
                            .WithEffectOnSelf(async caster =>
                            {
                                enemy.AddQEffect(QEffect.PersistentDamage(dice + "d8", DamageKind.Bleed));
                                QEffect flatFooted = QEffect.FlatFooted("Flensed")
                                    .WithExpirationAtStartOfSourcesTurn(caster, 1);
                                flatFooted.Name = "Flensed";
                                flatFooted.Description = flatFooted.Description?.Replace(".",
                                    " and your resistances to all physical damage types are reduced by 5.");
                                flatFooted.StateCheck = qfThis2 =>
                                {
                                    Creature owner = qfThis2.Owner;
                                    foreach (Resistance resistance in owner.WeaknessAndResistance.Resistances
                                                 .Where(resist =>
                                                     resist.DamageKind.IsPhysical()))
                                        resistance.Value = Math.Max(0, resistance.Value-5);
                                };
                                flatFooted.Illustration = ModData.Illustrations.FlensingSlice;
                                enemy.AddQEffect(flatFooted);
                            });
                        
                        return new ActionPossibility(flense)
                            .WithPossibilityGroup("Abilities"); 
                    };
                });
        
        /* Higher Level Feats
         * @10 Dual-Weapon Blitz
         * @12 (really: 10) Twin Riposte
         * @14 Dual Onslaught
         * @16 (really: 14) Improved Twin Riposte
         * @16 (really: 14) Two-Weapon Flurry
         * @18 (really: 16) Twinned Defense
         */
    }
}