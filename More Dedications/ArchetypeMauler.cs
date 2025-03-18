using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
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
using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;

public class ArchetypeMauler
{
    public static Feat MaulerDedicationFeat;
    public static Feat MaulerKnockdownFeat;
    public static Feat MaulerPowerAttackFeat;
    public static Feat MaulerClearTheWayFeat;
    public static Feat MaulerShovingSweepFeat;
    
    public static void LoadMod()
    {
        // Dedication Feat
        MaulerDedicationFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.MaulerDedication", "Mauler Dedication"),
            2,
            "You specialize in certain weapons that require two hands.",
            "You use your class's best weapon proficiency for the purposes of determining your proficiency with all simple and martial melee weapons that require two hands to wield.\n\nIf you are at least an expert in such a weapon, you gain access to the critical specialization effect with that weapon.",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, FeatArchetype.DedicationTrait]
            ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
            {
                // If refactored: apply the refactor to Archer Dedication
                sheet.Proficiencies.AddProficiencyAdjustment(Traits =>
                        Traits.Contains(Trait.Melee) && Traits.Contains(Trait.TwoHanded) && (Traits.Contains(Trait.Simple) || Traits.Contains(Trait.Martial)), Trait.Unarmed
                );
                
                // Fighter compatibility
                sheet.AtEndOfRecalculation += (sheet =>
                    {
                        Feat? fighterWeaponMastery = sheet.AllFeats
                            .Where((Feat f) => f.HasTrait(Trait.FighterWeaponMasteryWeaponGroup))
                            .FirstOrDefault((Feat?)null);
                        
                        if (fighterWeaponMastery != null)
                        {
                            Trait fighterWeaponTrait = ((FighterWeaponMasteryWeaponGroupFeat)fighterWeaponMastery).WeaponGroup;
                            sheet.Proficiencies.AddProficiencyAdjustment(Traits =>
                                    Traits.Contains(Trait.Melee) && Traits.Contains(Trait.TwoHanded) && (Traits.Contains(Trait.Simple) || Traits.Contains(Trait.Martial)), fighterWeaponTrait
                            );
                        }
                    }
                );
            }).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect()
                {
                    YouHaveCriticalSpecialization = (QEffect self, Item weapon, CombatAction _, Creature _) => weapon.HasTrait(Trait.Melee) && weapon.HasTrait(Trait.TwoHanded) && cr.Proficiencies.Get(weapon.Traits) >= Proficiency.Expert
                });
            }).WithPrerequisite(values => values.FinalAbilityScores.TotalScore(Ability.Strength) >= 14, "You must have at least 14 Strength.");
        ModManager.AddFeat(MaulerDedicationFeat);
        
        // Knockdown
        Feat? knockdownFeat = AllFeats.All.FirstOrDefault((Feat f) => f.FeatName == FeatName.Knockdown);
        if (knockdownFeat != null)
        {
            MaulerKnockdownFeat = new TrueFeat(
                ModManager.RegisterFeatName("MoreDedications.MaulerKnockdown", (knockdownFeat.Name + " (Mauler)")),
                4,
                knockdownFeat.FlavorText,
                knockdownFeat.RulesText,
                [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, Trait.Flourish]
            ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
            {
                sheet.GrantFeat(FeatName.Knockdown);
            }
            ).WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.GetProficiency(Trait.Athletics) >= Proficiency.Trained, "You must be trained in Athletics."
            ).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, MaulerDedicationFeat), "You must have the Mauler Dedication feat.").WithEquivalent((CalculatedCharacterSheetValues values) => values.AllFeats.Contains(knockdownFeat));
            ModManager.AddFeat(MaulerKnockdownFeat);
        }

        // Power Attack
        Feat? powerAttackFeat = AllFeats.All.FirstOrDefault((Feat f) => f.FeatName == FeatName.PowerAttack);
        if (powerAttackFeat != null)
        {
            MaulerPowerAttackFeat = new TrueFeat(
                ModManager.RegisterFeatName("MoreDedications.MaulerPowerAttack", (powerAttackFeat.Name + " (Mauler)")),
                4,
                powerAttackFeat.FlavorText,
                powerAttackFeat.RulesText,
                [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, Trait.Flourish]
            ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
            {
                sheet.GrantFeat(FeatName.PowerAttack);
            }
            ).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, MaulerDedicationFeat), "You must have the Mauler Dedication feat.").WithEquivalent((CalculatedCharacterSheetValues sheet) => sheet.AllFeats.Contains(powerAttackFeat));
            ModManager.AddFeat(MaulerPowerAttackFeat);
        }

        // Clear the Way
        MaulerClearTheWayFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.MaulerClearTheWay", "Clear the Way"),
            6,
            "You put your body behind your massive weapon and swing, shoving enemies to clear a wide path.",
            "{b}Requirements{/b} You're wielding a melee weapon with the Shove trait in two hands.\n\nYou attempt to Shove up to five creatures adjacent to you, rolling a separate Athletics check for each target. Then Stride up to half your Speed.\n\nThis movement doesn't trigger reactions from any of the creatures you successfully Shoved.",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait]
        ).WithActionCost(2).WithPermanentQEffect("You put your body behind your massive weapon.", async delegate (QEffect qf)
        {
            Possibility EvaluateAction(QEffect qfSelf)
            {
                Item? primaryItem = qf.Owner.PrimaryItem;
                if (primaryItem != null && primaryItem.HasTrait(Trait.Melee) && primaryItem.HasTrait(Trait.TwoHanded) && primaryItem.HasTrait(Trait.Shove))
                {
                    CombatAction combatAction = new CombatAction(
                        qf.Owner,
                        new SideBySideIllustration(primaryItem.Illustration, IllustrationName.Shove),
                        "Clear the Way",
                        [Trait.Basic, Trait.IsHostile, Trait.AlwaysHits, Trait.Attack, Trait.AttackDoesNotIncreaseMultipleAttackPenalty],
                        "Attempt to Shove up to five creatures adjacent to you, rolling a separate Athletics check for each target.",
                        Target.MultipleCreatureTargets(Target.Touch(), Target.Touch(), Target.Touch(), Target.Touch(), Target.Touch())
                        .WithMinimumTargets(1).WithMustBeDistinct()
                    ).WithActionCost(2).WithEffectOnChosenTargets(async delegate (Creature fighter, ChosenTargets targets)
                    {
                        QEffect noMoveReactionsFromShove = new QEffect("Shoved by Clear the Way", "Cannot take reactions against Clear the Way's Stride.", ExpirationCondition.Never, fighter, IllustrationName.ReactionUsedUp)
                        {
                            Id = QEffectId.CannotTakeReactions
                        };

                        foreach (Creature c in targets.ChosenCreatures)
                        {
                            CombatAction shoveAction = CombatManeuverPossibilities.CreateShoveAction(fighter, primaryItem).WithActionCost(0);

                            await fighter.Battle.GameLoop.FullCast(shoveAction, ChosenTargets.CreateSingleTarget(c));
                            if (shoveAction.CheckResult >= CheckResult.Success)
                            {
                                c.AddQEffect(noMoveReactionsFromShove);
                            }
                        }

                        await fighter.StrideAsync("Stride up to half your speed. This movement doesn't trigger reactions from any of the creatures you successfully Shoved.", allowStep: false, allowCancel: false, allowPass: true, maximumHalfSpeed: true);

                        foreach (Creature c in targets.ChosenCreatures)
                        {
                            c.RemoveAllQEffects((QEffect qf) => qf == noMoveReactionsFromShove);
                        }
                    }
                    ).WithTargetingTooltip((CombatAction power, Creature target, int index) => power.Description);

                    return new ActionPossibility(combatAction).WithPossibilityGroup("Abilities");
                }
                return null;
            }

            qf.ProvideMainAction = EvaluateAction;

        }).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, MaulerDedicationFeat), "You must have the Mauler Dedication feat.");
        ModManager.AddFeat(MaulerClearTheWayFeat);

        // Shoving Sweep lv8
        MaulerShovingSweepFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.MaulerShovingSweep", "Shoving Sweep"),
            8,
            "You swing your weapon at a fleeing foe, rebuffing them back.",
            "{b}Requirements{/b} You're wielding a melee weapon in two hands.\n\nWhen a creature within your reach leaves a square during a move action it's using, you can spend a {icon:Reaction} reaction to attempt to Shove the triggering creature, ignoring the requirement that you have a hand free. {i}(NYI: The creature continues its movement after the Shove.){/i}",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait]
        ).WithActionCost(-2
        ).WithPermanentQEffect("After a creature within your reach leaves a square during its move action, you can spend a reaction to Shove it.", delegate (QEffect qf)
        {
            qf.Id = QEffectId.AttackOfOpportunity;
            qf.WhenProvoked = async delegate (QEffect attackOfOpportunityQEffect, CombatAction provokingAction)
            {
                Creature owner = attackOfOpportunityQEffect.Owner;
                Item? primaryWeapon = owner.PrimaryWeapon;
                if (primaryWeapon != null && primaryWeapon.HasTrait(Trait.TwoHanded) && primaryWeapon.HasTrait(Trait.Melee)
                    && provokingAction.HasTrait(Trait.Move) && provokingAction.TilesMoved > 0)
                {
                    bool hadShoveTrait = primaryWeapon.HasTrait(Trait.Shove);
                    int storeItemBonus = primaryWeapon.WeaponProperties.ItemBonus;
                    if (!hadShoveTrait)
                    {
                        primaryWeapon.Traits.Add(Trait.Shove);
                        primaryWeapon.WeaponProperties.ItemBonus = 0;
                    }

                    CombatAction shoveAction = CombatManeuverPossibilities.CreateShoveAction(owner, primaryWeapon).WithActionCost(0);
                    shoveAction.Traits.Add(Trait.AttackDoesNotIncreaseMultipleAttackPenalty); // Might not be necessary.

                    if (shoveAction.CanBeginToUse(owner))
                    {
                        if (await owner.Battle.AskToUseReaction(owner, "A creature within your reach just left a square. Use {i}Shoving Sweep{/i} to Shove it?"))
                        {
                            await owner.Battle.GameLoop.FullCast(shoveAction, ChosenTargets.CreateSingleTarget(provokingAction.Owner));
                        }
                    }

                    if (!hadShoveTrait)
                    {
                        primaryWeapon.Traits.Remove(Trait.Shove);
                        primaryWeapon.WeaponProperties.ItemBonus = storeItemBonus;
                    }
                }
            };
        }
        ).WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.GetProficiency(Trait.Athletics) >= Proficiency.Expert, "You must be expert in Athletics."
        ).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, MaulerDedicationFeat), "You must have the Mauler Dedication feat.");
        ModManager.AddFeat(MaulerShovingSweepFeat);
    }
}