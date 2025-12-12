using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeMauler
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Dedication Feat
        Feat maulerDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.MaulerArchetype,
                "You shove your way through legions of foes, knock enemies on all sides to the ground, and deal massive blows to anyone or anything that comes near.",
                "You become trained in all simple and martial melee weapons that require two hands to wield or that have the two-hand trait.\n\nWhenever you become expert, master, or legendary in any weapon, you also gain that proficiency rank in these weapons.\n\nAs long as you're at least expert in such a weapon, that weapon triggers {tooltip:criteffect}critical specialization effects{/}.")
            .WithOnSheet(values =>
            {
                values.Proficiencies.Set(
                    [Trait.Simple, Trait.TwoHanded, Trait.Melee],
                    Proficiency.Trained);
                values.Proficiencies.Set(
                    [Trait.Martial, Trait.TwoHanded, Trait.Melee],
                    Proficiency.Trained);
                values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                    [Trait.Simple, Trait.TwoHanded, Trait.Melee]);
                values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                    [Trait.Martial, Trait.TwoHanded, Trait.Melee]);
                if (ModManager.TryParse("Two-Hand 1d12", out Trait thd12))
                {
                    values.Proficiencies.Set(
                        [Trait.Simple, thd12, Trait.Melee],
                        Proficiency.Trained);
                    values.Proficiencies.Set(
                        [Trait.Martial, thd12, Trait.Melee],
                        Proficiency.Trained);
                    values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                        [Trait.Simple, thd12, Trait.Melee]);
                    values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                        [Trait.Martial, thd12, Trait.Melee]);
                }
                if (ModManager.TryParse("Two-Hand 1d10", out Trait thd10))
                {
                    values.Proficiencies.Set(
                        [Trait.Simple, thd10, Trait.Melee],
                        Proficiency.Trained);
                    values.Proficiencies.Set(
                        [Trait.Martial, thd10, Trait.Melee],
                        Proficiency.Trained);
                    values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                        [Trait.Simple, thd10, Trait.Melee]);
                    values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                        [Trait.Martial, thd10, Trait.Melee]);
                }
            })
            .WithPermanentQEffect(
                "As long as you're at least expert in the two-handed melee weapon you're using, that triggers {tooltip:criteffect}critical specialization effects{/}.",
                qfFeat =>
                {
                    qfFeat.YouHaveCriticalSpecialization = (qfThis, weapon, _, _) =>
                        IsMaulerWeapon(weapon) && qfThis.Owner.Proficiencies.Get(weapon.Traits) >= Proficiency.Expert;
                })
            .WithDemandsAbility14(Ability.Strength);
        maulerDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return maulerDedication;
        
        // Add Knockdown to Mauler
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.Knockdown, ModData.Traits.MaulerArchetype, 4);

        // Add Power Attack to Mauler
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.PowerAttack, ModData.Traits.MaulerArchetype, 4);

        // Clear the Way
        yield return new TrueFeat(
                ModData.FeatNames.ClearTheWay,
                6,
                "You put your body behind your massive weapon and swing, shoving enemies to clear a wide path.",
                "{b}Requirements{/b} You're wielding a melee weapon with the Shove trait in two hands.\n\nYou attempt to Shove up to five creatures adjacent to you, rolling a separate Athletics check for each target. Then Stride up to half your Speed.\n\nThis movement doesn't trigger reactions from any of the creatures you successfully Shoved.",
                [ModData.Traits.MoreDedications])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MaulerArchetype)
            .WithPermanentQEffect(
                "Attempt to Shove up to 5 adjacent creatures, then Stride without provoking reactions.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        Item? primaryItem = qfFeat.Owner.PrimaryItem;
                        
                        if (primaryItem == null || !primaryItem.HasTrait(Trait.Melee) ||
                            !primaryItem.HasTrait(Trait.TwoHanded) || !primaryItem.HasTrait(Trait.Shove))
                            return null;
                        
                        CombatAction combatAction = new CombatAction(
                                qfFeat.Owner,
                                new SideBySideIllustration(primaryItem.Illustration, IllustrationName.Shove),
                                "Clear the Way",
                                [Trait.Basic, Trait.IsHostile, Trait.AlwaysHits, Trait.Attack, Trait.AttackDoesNotIncreaseMultipleAttackPenalty],
                                "Attempt to Shove up to five creatures adjacent to you, rolling a separate Athletics check for each target.",
                                Target.MultipleCreatureTargets(Target.Touch(), Target.Touch(), Target.Touch(), Target.Touch(), Target.Touch())
                                    .WithMinimumTargets(1)
                                    .WithMustBeDistinct())
                            .WithActionCost(2)
                            .WithEffectOnChosenTargets(async (attacker, targets) =>
                            {
                                QEffect noMoveReactionsFromShove = new QEffect(
                                    "Shoved by Clear the Way",
                                    "Cannot take reactions against Clear the Way's Stride.",
                                    ExpirationCondition.Never,
                                    attacker,
                                    IllustrationName.ReactionUsedUp)
                                {
                                    Id = QEffectId.CannotTakeReactions
                                };

                                foreach (Creature cr in targets.ChosenCreatures)
                                {
                                    CombatAction shoveAction = CombatManeuverPossibilities.CreateShoveAction(attacker, primaryItem)
                                        .WithActionCost(0);

                                    await attacker.Battle.GameLoop.FullCast(shoveAction, ChosenTargets.CreateSingleTarget(cr));
                                    
                                    if (shoveAction.CheckResult >= CheckResult.Success)
                                        cr.AddQEffect(noMoveReactionsFromShove);
                                }

                                await attacker.StrideAsync(
                                    "Stride up to half your speed. This movement doesn't trigger reactions from any of the creatures you successfully Shoved.",
                                    allowStep: false,
                                    allowCancel: false,
                                    allowPass: true,
                                    maximumHalfSpeed: true);

                                foreach (Creature c in targets.ChosenCreatures)
                                    c.RemoveAllQEffects(qf => qf == noMoveReactionsFromShove);
                            })
                            .WithTargetingTooltip((power, target, index) => power.Description);

                        return new ActionPossibility(combatAction).WithPossibilityGroup("Abilities");
                    };
                });

        // Shoving Sweep
        yield return new TrueFeat(
                ModData.FeatNames.ShovingSweep,
                8,
                "You swing your weapon at a fleeing foe, rebuffing them back.",
                "{b}Requirements{/b} You're wielding a melee weapon in two hands.\n\nWhen a creature within your reach leaves a square during a move action it's using, you can spend a {icon:Reaction} reaction to attempt to Shove the triggering creature, ignoring the requirement that you have a hand free. {i}({Red}NYI:{/Red} The creature continues its movement after the Shove.){/i}",
                [ModData.Traits.MoreDedications])
            .WithActionCost(-2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MaulerArchetype)
            .WithPermanentQEffect(
                "After a creature within your reach leaves a square during its move action, you can spend a reaction to Shove it.",
                qfThis =>
                {
                    qfThis.Id = QEffectId.AttackOfOpportunity;
                    qfThis.WhenProvoked = async (attackOfOpportunityQEffect, provokingAction) =>
                    {
                        Creature owner = attackOfOpportunityQEffect.Owner;
                        Item? primaryWeapon = owner.PrimaryWeapon;
                        if (primaryWeapon != null && primaryWeapon.HasTrait(Trait.TwoHanded) &&
                            primaryWeapon.HasTrait(Trait.Melee)
                            && provokingAction.HasTrait(Trait.Move) && provokingAction.TilesMoved > 0)
                        {
                            bool hadShoveTrait = primaryWeapon.HasTrait(Trait.Shove);
                            int storeItemBonus = primaryWeapon.WeaponProperties!.ItemBonus;
                            if (!hadShoveTrait)
                            {
                                primaryWeapon.Traits.Add(Trait.Shove);
                                primaryWeapon.WeaponProperties.ItemBonus = 0;
                            }

                            CombatAction shoveAction = CombatManeuverPossibilities
                                .CreateShoveAction(owner, primaryWeapon)
                                .WithActionCost(0);
                            shoveAction.Traits.Add(Trait
                                .AttackDoesNotIncreaseMultipleAttackPenalty); // Might not be necessary.

                            if (shoveAction.CanBeginToUse(owner))
                            {
                                if (await owner.Battle.AskToUseReaction(owner,
                                        "A creature within your reach just left a square. Use {i}Shoving Sweep{/i} to Shove it?"))
                                {
                                    await owner.Battle.GameLoop.FullCast(shoveAction,
                                        ChosenTargets.CreateSingleTarget(provokingAction.Owner));
                                }
                            }

                            if (!hadShoveTrait)
                            {
                                primaryWeapon.Traits.Remove(Trait.Shove);
                                primaryWeapon.WeaponProperties.ItemBonus = storeItemBonus;
                            }
                        }
                    };
                })
            .WithPrerequisite(
                values => values.GetProficiency(Trait.Athletics) >= Proficiency.Expert,
                "You must be expert in Athletics.");
        
        /* Higher Level Feats
         * @12 (really: 10) Improved Knockdown
         * @14 (really: 12) Brutal Finish
         * @14 Hammer Quake
         * @14 Unbalancing Sweep
         * @16 Avalanche Strike
         */
    }

    public static bool IsMaulerWeapon(Item item)
    {
        return item.HasTrait(Trait.Melee) && (item.HasTrait(Trait.TwoHanded) || HasTwoHandTrait(item));
    }

    public static bool HasTwoHandTrait(Item item)
    {
        return HasTwoHandTrait(item.Traits);
    }

    public static bool HasTwoHandTrait(List<Trait> traits)
    {
        if (ModManager.TryParse("Two-Hand 1d12", out Trait thd12))
        {
            return traits.Contains(thd12);
        }

        if (ModManager.TryParse("Two-Hand 1d10", out Trait thd10))
        {
            return traits.Contains(thd10);
        }

        return false;
    }
}