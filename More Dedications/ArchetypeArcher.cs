using System.Diagnostics;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;

public class ArchetypeArcher
{
    public static Feat ArcherDedicationFeat;
    public static Feat ArcherAssistingShotFeat;
    public static Feat FighterPointBlankShotFeat;
    public static Feat ArcherPointBlankShotFeat;
    public static Feat ArcherQuickDrawFeat;
    public static Feat ArcherAdvancedBowTrainingFeat;
    public static Feat ArcherCrossbowTerrorFeat;
    public static Feat FighterPartingShotFeat;
    public static Feat ArcherPartingShotFeat;
    public static Feat ArcherDoubleShotFeat;
    public static Feat RangerRunningReloadFeat;
    public static Feat ArcherRunningReloadFeat;
    public static Feat ArcherArchersAimFeat;
    public static Feat FighterTripleShotFeat;
    public static Feat ArcherTripleShotFeat;
    
    public static void LoadMod()
    {
        // Dedication Feat
        ArcherDedicationFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherDedication", "Archer Dedication"),
            2,
            "You specialize in certain ranged weapons.",
            "You use your class's best weapon proficiency for the purposes of determining your proficiency with all simple and martial bows.\n\nIf you are at least an expert with the bow, you gain access to the critical specialization effect with that bow.",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, FeatArchetype.DedicationTrait])
            .WithOnSheet(values =>
            {
                values.Proficiencies.AddProficiencyAdjustment(traits =>
                        traits.Contains(Trait.Bow) && (traits.Contains(Trait.Simple) || traits.Contains(Trait.Martial)), Trait.Unarmed);
                    
                // Fighter compatibility
                // If refactored: apply the refactor to Mauler Dedication, and Advanced Bow Training
                values.AtEndOfRecalculation += sheet =>
                {
                    Feat? fighterWeaponMastery = sheet.AllFeats
                        .Where((Feat f) => f.HasTrait(Trait.FighterWeaponMasteryWeaponGroup))
                        .FirstOrDefault((Feat?)null);

                    if (fighterWeaponMastery == null)
                        return;
                    Trait fighterWeaponTrait = ((FighterWeaponMasteryWeaponGroupFeat)fighterWeaponMastery).WeaponGroup;
                    sheet.Proficiencies.AddProficiencyAdjustment(Traits =>
                            Traits.Contains(Trait.Bow) && (Traits.Contains(Trait.Simple) || Traits.Contains(Trait.Martial)), fighterWeaponTrait
                    );
                };
            })
            .WithOnCreature(cr =>
            {
                cr.AddQEffect(new QEffect()
                {
                    YouHaveCriticalSpecialization = (qfSelf, weapon, _, _) =>
                        weapon.HasTrait(Trait.Melee) && weapon.HasTrait(Trait.TwoHanded) && cr.Proficiencies.Get(weapon.Traits) >= Proficiency.Expert
                });
            });
        ModManager.AddFeat(ArcherDedicationFeat);

        Feat? assistingShotFeat = AllFeats.All.FirstOrDefault((Feat f) => f.FeatName == FeatName.AssistingShot);
        if (assistingShotFeat != null)
        {
            ArcherAssistingShotFeat = new TrueFeat(
                ModManager.RegisterFeatName("MoreDedications.ArcherAssistingShot", $"{assistingShotFeat.Name} (Archer)"),
                4,
                assistingShotFeat.FlavorText,
                assistingShotFeat.RulesText,
                [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, Trait.Press])
                .WithOnSheet(values =>
                {
                    values.GrantFeat(FeatName.AssistingShot);
                })
                .WithPrerequisite(values =>
                    Enumerable.Contains(values.AllFeats, ArcherDedicationFeat),
                    "You must have the Archer Dedication feat.")
                .WithEquivalent(values =>
                    values.AllFeats.Contains(assistingShotFeat));
            ModManager.AddFeat(ArcherAssistingShotFeat);
        }
        
        // Point-Blank Shot
        FighterPointBlankShotFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.FighterPointBlankShot", "Point-Blank Shot"),
            1,
            "You take aim to pick off nearby enemies quickly.",
            "{b}Requirements{/b} You are wielding a ranged weapon.\n\nWhen using a ranged volley weapon while you are in this stance, you don't take the penalty to your attack rolls from the volley trait. When using a ranged weapon that doesn't have the volley trait, you gain a +2 circumstance bonus to damage rolls on attacks against targets within the weapon's first range increment.",
            [MoreDedications.ModNameTrait, Trait.Fighter, Trait.Open, Trait.Stance])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                Illustration icon = new ScrollIllustration(IllustrationName.HuntPrey, IllustrationName.Crossbow);
                //Illustration icon = new ScrollIllustration(IllustrationName.Shove, IllustrationName.Crossbow);
                string stanceName = "Point-Blank Shot";
                string fullDescription =
                    "{b}Requirements{/b} You are wielding a ranged weapon.\n\nWhen using a ranged volley weapon while you are in this stance, you don't take the penalty to your attack rolls from the volley trait. When using a ranged weapon that doesn't have the volley trait, you gain a +2 circumstance bonus to damage rolls on attacks against targets within the weapon's first range increment.";
                string effectDescription =
                    "Ranged volley weapons don't take volley's penalty, and ranged weapons without volley gain a +2 circumstance bonus to damage within its first range increment.";
                string actionDescription = "Enter a stance where " + effectDescription.Uncapitalize();
                
                qfFeat.ProvideActionIntoPossibilitySection = (qfSelf, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                        return null;
                    CombatAction stanceAction = new CombatAction(
                        qfSelf.Owner, icon, stanceName,
                        [Trait.Fighter, Trait.Open, Trait.Stance],
                        fullDescription,
                        Target.Self()
                            .WithAdditionalRestriction(self =>
                            {
                                foreach (QEffect qfInLoop in self.QEffects)
                                {
                                    if (qfInLoop.Tag is string && qfInLoop.Tag.ToString() == "Point-Blank Shot")
                                    {
                                        return "You're already in this stance.";
                                    }
                                }
                                
                                foreach (Item heldItem in self.HeldItems)
                                {
                                    if (heldItem != null && heldItem.HasTrait(Trait.Weapon) &&
                                        heldItem.HasTrait(Trait.Ranged))
                                    {
                                        return null;
                                    }
                                }

                                return "You must be wielding a ranged weapon.";
                            }))
                        .WithActionCost(1)
                        .WithEffectOnSelf(async self =>
                        {
                            QEffect stanceQEffect = KineticistCommonEffects.EnterStance(self, icon, stanceName, effectDescription);
                            stanceQEffect.StateCheck += (thisQf =>
                            {
                                foreach (Item heldItem in self.HeldItems)
                                {
                                    if (heldItem.HasTrait(Trait.Weapon) &&
                                        heldItem.HasTrait(Trait.Ranged))
                                    {
                                        return;
                                    }
                                }
                                thisQf.ExpiresAt = ExpirationCondition.Immediately;
                            });
                            stanceQEffect.Tag = "Point-Blank Shot";
                            stanceQEffect.BonusToDamage = (QEffect stanceQf, CombatAction combatAction, Creature defender) =>
                            {
                                Item? primaryWeapon = stanceQf.Owner.PrimaryWeaponIncludingRanged;
                                return primaryWeapon != null && !primaryWeapon.HasTrait(Trait.Volley30Feet) && stanceQf.Owner.DistanceTo(defender) <= primaryWeapon.WeaponProperties.RangeIncrement
                                    ? new Bonus(2, BonusType.Circumstance, "Point-Blank Shot")
                                    : null;
                            };
                            stanceQEffect.BonusToAttackRolls = (QEffect stanceQf, CombatAction combatAction, Creature? defender) =>
                            {
                                Item? primaryWeapon = stanceQf.Owner.PrimaryWeaponIncludingRanged;
                                return primaryWeapon != null && combatAction.HasTrait(Trait.Attack) && primaryWeapon.HasTrait(Trait.Volley30Feet) && defender != null && stanceQf.Owner.DistanceTo(defender) <= 6  
                                    ? new Bonus(2, BonusType.Untyped, "Point-Blank Shot")
                                    : null;
                            };
                        });
                    stanceAction.ShortDescription = actionDescription;
                        
                    ActionPossibility stancePossibility = new ActionPossibility(stanceAction);
                    stancePossibility.PossibilityGroup = "Enter a stance";
                    return stancePossibility;
                };
            }); //.WithIllustration(IllustrationName.Longbow);
        ModManager.AddFeat(FighterPointBlankShotFeat);

        ArcherPointBlankShotFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherPointBlankShot", "Point-Blank Shot (Archer) {icon:Action}"),
            4,
            FighterPointBlankShotFeat.FlavorText,
            FighterPointBlankShotFeat.RulesText,
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, Trait.Open, Trait.Stance])
            .WithOnSheet(values =>
            {
                values.GrantFeat(FighterPointBlankShotFeat.FeatName);
            })
            .WithPrerequisite(values =>
                Enumerable.Contains(values.AllFeats, ArcherDedicationFeat),
                "You must have the Archer Dedication feat.")
            .WithEquivalent(values =>
                values.AllFeats.Contains(FighterPointBlankShotFeat));
        ModManager.AddFeat(ArcherPointBlankShotFeat);
        
        // Quick Shot -> Quick Draw
        Feat? quickDrawFeat = AllFeats.All.FirstOrDefault((Feat f) => f.FeatName == FeatName.QuickDraw);
        if (quickDrawFeat != null)
        {
            ArcherQuickDrawFeat = new TrueFeat(
                ModManager.RegisterFeatName("MoreDedications.ArcherQuickDraw", $"{quickDrawFeat.Name} (Archer)"),
                4,
                quickDrawFeat.FlavorText,
                quickDrawFeat.RulesText,
                [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait])
                .WithOnSheet(sheet =>
                {
                    sheet.GrantFeat(FeatName.QuickDraw);
                })
                .WithPrerequisite(sheet =>
                    Enumerable.Contains(sheet.AllFeats, ArcherDedicationFeat),
                    "You must have the Archer Dedication feat.")
                .WithEquivalent(values =>
                    values.AllFeats.Contains(quickDrawFeat));
            ModManager.AddFeat(ArcherQuickDrawFeat);
        }
        
        // Advanced Bow Training
        ArcherAdvancedBowTrainingFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherAdvancedBowTraining", "Advanced Bow Training"),
            6,
            "Through constant practice and the crucible of experience, you increase your skill with advanced bows.",
            "You gain proficiency with all advanced bows as if they were martial weapons in the bow weapon group.",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait])
            .WithPrerequisite(values =>
                Enumerable.Contains(values.AllFeats, ArcherDedicationFeat),
                "You must have the Archer Dedication feat.")
            .WithOnSheet(values =>
            {
                values.Proficiencies.AddProficiencyAdjustment(Traits =>
                        Traits.Contains(Trait.Bow) && Traits.Contains(Trait.Advanced), Trait.Martial
                );
                    
                // Fighter compatibility
                values.AtEndOfRecalculation += sheet =>
                {
                    Feat? fighterWeaponMastery = sheet.AllFeats
                        .Where((Feat f) => f.HasTrait(Trait.FighterWeaponMasteryWeaponGroup))
                        .FirstOrDefault((Feat?)null);
                    
                    if (fighterWeaponMastery != null)
                    {
                        Trait fighterWeaponTrait = ((FighterWeaponMasteryWeaponGroupFeat)fighterWeaponMastery).WeaponGroup;
                        sheet.Proficiencies.AddProficiencyAdjustment(Traits =>
                                Traits.Contains(Trait.Bow) && Traits.Contains(Trait.Advanced), fighterWeaponTrait
                        );
                    }
                };
            })
            .WithOnCreature(cr =>
            {
                cr.AddQEffect(new QEffect()
                {
                    YouHaveCriticalSpecialization = (QEffect self, Item weapon, CombatAction _, Creature _) => weapon.HasTrait(Trait.Melee) && weapon.HasTrait(Trait.TwoHanded) && cr.Proficiencies.Get(weapon.Traits) >= Proficiency.Expert
                });
            });
        ModManager.AddFeat(ArcherAdvancedBowTrainingFeat);
        
        // Crossbow Terror
        ArcherCrossbowTerrorFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherCrossbowTerror", "Crossbow Terror"),
            6,
            "You are a dynamo with the crossbow.",
            "You gain a +2 circumstance bonus to damage with crossbows. If the crossbow is a simple weapon, also increase the damage die size for your attacks made with that crossbow by one step. As normal, this damage die increase can't be combined with other abilities that alter the weapon damage die (such as the ranger feat Crossbow Ace).",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait])
            .WithPermanentQEffect(
                "+2 circumstance bonus to Crossbow damage, increment Simple Crossbow die.",
                qfFeat =>
                {
                    qfFeat.IncreaseItemDamageDie = (qfThis, item) =>
                    {
                        if (!item.HasTrait(Trait.Crossbow) || !item.HasTrait(Trait.Simple))
                            return false;
                        
                        foreach (QEffect qfInLoop in qfFeat.Owner.QEffects)
                        {
                            if (qfInLoop != qfFeat && qfInLoop.IncreaseItemDamageDie != null)
                                return false;
                        }
                        return true;

                    };
                    qfFeat.BonusToDamage = (qfThis, action, defender) =>
                        action.HasTrait(Trait.Crossbow) ?
                        new Bonus(2, BonusType.Circumstance, "Crossbow Terror") :
                        null;
                })
            .WithPrerequisite(values =>
                Enumerable.Contains(values.AllFeats, ArcherDedicationFeat),
                "You must have the Archer Dedication feat.");
        ModManager.AddFeat(ArcherCrossbowTerrorFeat);
        
        // Double Shot
        Feat? doubleShotFeat = AllFeats.All.FirstOrDefault(feat =>
            feat.FeatName == FeatName.DoubleShot);
        if (doubleShotFeat != null)
        {
            ArcherDoubleShotFeat = new TrueFeat(
                ModManager.RegisterFeatName("MoreDedications.ArcherDoubleShot", $"{doubleShotFeat.Name} (Archer)"),
                4,
                doubleShotFeat.FlavorText,
                doubleShotFeat.RulesText,
                [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, Trait.Flourish])
                .WithOnSheet(sheet =>
                {
                    sheet.GrantFeat(FeatName.DoubleShot);
                })
                .WithPrerequisite(values =>
                    Enumerable.Contains(values.AllFeats, ArcherDedicationFeat),
                    "You must have the Archer Dedication feat.")
                .WithEquivalent(values =>
                    values.AllFeats.Contains(doubleShotFeat));
            ModManager.AddFeat(ArcherDoubleShotFeat);
        }
        
        // Parting Shot
        FighterPartingShotFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.FighterPartingShot", "Parting Shot"),
            4,
            "You jump back and fire a quick shot that catches your opponent off guard.",
            "{b}Requirements{/b} You are wielding a loaded ranged weapon or a ranged weapon without reload 1 or reload 2.\n\nYou Step and then make a ranged Strike with the required weapon. Your target is flat-footed against the attack.",
            [MoreDedications.ModNameTrait, Trait.Fighter])
            .WithActionCost(2)
            .WithPermanentQEffect("You jump back and fire a quick shot that catches your opponent off guard.",
            async qfFeat =>
            {
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Ranged) || ((item.HasTrait(Trait.Reload1) || item.HasTrait(Trait.Reload2)) && item.EphemeralItemProperties.NeedsReload))
                        return null;
                    CombatAction basicStrike = qfFeat.Owner.CreateStrike(item).WithActionCost(0);
                    CombatAction partingShot = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(IllustrationName.Walk, item.Illustration),
                            "Parting Shot",
                            [Trait.Fighter, Trait.Basic],
                            StrikeRules.CreateBasicStrikeDescription3(basicStrike.StrikeModifiers, additionalAttackRollText: "You Step before you Strike. Your target is flat-footed against the attack."),
                            Target.Self())
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, caster, targets) =>
                        {
                            if (!await caster.StepAsync("Choose where to Step with Parting Shot.", allowCancel: true, allowPass: true))
                            {
                                action.RevertRequested = true;
                            }
                            else
                            {
                                QEffect temporarilyFlatFooted = new QEffect()
                                {
                                    IsFlatFootedTo = (qfSelf, attacker, action) =>
                                        attacker != caster ? null : "Parting Shot" 
                                }.WithExpirationNever();
                                caster.Battle.AllCreatures.ForEach(cr => cr.AddQEffect(temporarilyFlatFooted));
                                await caster.Battle.GameLoop.FullCast(basicStrike);
                                caster.Battle.AllCreatures.ForEach(cr => cr.RemoveAllQEffects(qfToRemove => qfToRemove == temporarilyFlatFooted));
                            }
                        })
                        .WithTargetingTooltip((power, target, index) =>
                            power.Description);
                    
                    return partingShot;
                };
            });
        ModManager.AddFeat(FighterPartingShotFeat);

        ArcherPartingShotFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherPartingShot", FighterPartingShotFeat.Name + " (Archer)"),
            6,
            FighterPartingShotFeat.FlavorText,
            FighterPartingShotFeat.RulesText,
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait]
        ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
        {
            sheet.GrantFeat(FighterPartingShotFeat.FeatName);
        }).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, ArcherDedicationFeat), "You must have the Archer Dedication feat.").WithEquivalent((CalculatedCharacterSheetValues values) => values.AllFeats.Contains(FighterPartingShotFeat));
        ModManager.AddFeat(ArcherPartingShotFeat);
        
        // Running Reload
        // Code from SudoProgramming's Gunslinger.cs.
        // This isn't an attribution, I just need to know the reference for when I inevitably forget how this code works after needing to make changes.
        RangerRunningReloadFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.RangerRunningReload", "Running Reload {icon:Action}"),
            4,
            "You can reload your weapon on the move.",
            "You Stride, Step, or Sneak, then Interact to reload.\n\n{i}(This feat may not support firearms from any mods.){/i}",
            [MoreDedications.ModNameTrait, Trait.Ranger]
        ).WithPermanentQEffect("Stride and reload", delegate(QEffect qf)
        {   // Adds a permanent Running Reload action if the appropriate weapon is held
            qf.ProvideActionIntoPossibilitySection =
                (QEffect runningReloadEffect, PossibilitySection possibilitySection) =>
                {
                    if (possibilitySection.PossibilitySectionId == PossibilitySectionId.MainActions)
                    {
                        SubmenuPossibility runningReloadMenu = new SubmenuPossibility(IllustrationName.WarpStep, "Running Reload");
                        
                        foreach (Item heldItem in runningReloadEffect.Owner.HeldItems)
                        {
                            bool isReloadable = heldItem.HasTrait(Trait.Reload1) || heldItem.HasTrait(Trait.Reload2) || heldItem.HasTrait(Trait.Repeating); // Modify for compatibility.
                            if (isReloadable && heldItem.WeaponProperties != null)
                            {
                                PossibilitySection runningReloadSection = new PossibilitySection(heldItem.Name);
                                CombatAction itemAction = new CombatAction(runningReloadEffect.Owner, new SideBySideIllustration(heldItem.Illustration, IllustrationName.WarpStep), "Running Reload", [Trait.Basic], "You Stride, Step, or Sneak, then Interact to reload.", Target.Self()
                                .WithAdditionalRestriction((Creature user) =>
                                {
                                    bool needsReload = heldItem.EphemeralItemProperties != null && heldItem.EphemeralItemProperties.NeedsReload;
                                    bool isLowMag = (heldItem.HasTrait(Trait.Repeating) && heldItem.EphemeralItemProperties.AmmunitionLeftInMagazine < 5) || false; // Modify for later compatibility.
                                    if (!needsReload && !isLowMag)
                                    {
                                        return "Can not be reloaded.";
                                    }
                                    return null;
                                })).WithActionCost(1).WithItem(heldItem).WithEffectOnSelf(async (action, self) =>
                                {
                                    if (!await self.StrideAsync("Choose where to Stride with Running Reload.", allowCancel: true))
                                    {
                                        action.RevertRequested = true;
                                    }
                                    else
                                    {
                                        await self.CreateReload(heldItem).WithActionCost(0).WithItem(heldItem).AllExecute();
                                    }
                                });
                                ActionPossibility itemPossibility = new ActionPossibility(itemAction);

                                runningReloadSection.AddPossibility(itemPossibility);
                                runningReloadMenu.Subsections.Add(runningReloadSection);
                            }
                        }

                        return runningReloadMenu;
                    }

                    return null;
                };
        });
        ModManager.AddFeat(RangerRunningReloadFeat);

        ArcherRunningReloadFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherRunningReload", RangerRunningReloadFeat.Name + " (Archer)"),
            6,
            RangerRunningReloadFeat.FlavorText,
            RangerRunningReloadFeat.RulesText,
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait]
        ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
        {
            sheet.GrantFeat(RangerRunningReloadFeat.FeatName);
        }).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, ArcherDedicationFeat), "You must have the Archer Dedication feat.").WithEquivalent((CalculatedCharacterSheetValues values) => values.AllFeats.Contains(RangerRunningReloadFeat));
        ModManager.AddFeat(ArcherRunningReloadFeat);

        // Archer's Aim
        ArcherArchersAimFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherArchersAim", "Archer's Aim"),
            8,
            "You slow down, focus, and take a careful shot.",
            "Make a ranged Strike with a weapon in the bow weapon group. You gain a +2 circumstance bonus to the attack roll and ignore the target's concealed condition. If the target is hidden, reduce the flat check from being hidden from 11 to 5.",
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait, Trait.Concentrate]
        ).WithActionCost(2
        ).WithPermanentQEffect("You can make a careful shot.", (Action<QEffect>) (qf =>
        {
            const string actionName = "Archer's Aim";
            qf.ProvideStrikeModifier = (Func<Item, CombatAction>)( item =>
            {
                if (!item.HasTrait(Trait.Ranged) || !item.HasTrait(Trait.Bow))
                    return null;
                CombatAction strike = qf.Owner.CreateStrike(item).WithActionCost(2);
                strike.Name = actionName;
                strike.Illustration = (Illustration)new SideBySideIllustration(
                    strike.Illustration,
                    (Illustration)IllustrationName.TargetSheet);
                strike.Description = StrikeRules.CreateBasicStrikeDescription2(strike.StrikeModifiers,
                    "You gain a +2 circumstance bonus to the attack roll, ignore the target's concealed condition, and reduce flat checks due to hidden to 5.\n\n(NOTE: Accuracy preview against hidden creatures doesn't use a lower DC.)");
                strike.StrikeModifiers.HuntersAim = true;
                strike.StrikeModifiers.AdditionalBonusesToAttackRoll =
                    [new Bonus(2, BonusType.Circumstance, "Archer's Aim")];
                // Apply BlindFight before strike is made.
                strike.WithPrologueEffectOnChosenTargetsBeforeRolls(async (CombatAction, Creature, ChosenTargets) =>
                {
                    CombatAction.Owner.AddQEffect(new QEffect() { Id = QEffectId.BlindFight, Tag = actionName});
                });
                // Remove BlindFight after strike is made.
                strike.StrikeModifiers.OnEachTarget = async (Attacker, Defender, CheckResult) =>
                {
                    Attacker.RemoveAllQEffects((QEffect qfToRemove) => qfToRemove.Tag is actionName);
                };
                return strike;
            });
        }));
        ModManager.AddFeat(ArcherArchersAimFeat);
        
        // Triple Shot
        FighterTripleShotFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.FighterTripleShot", "Triple Shot"),
            6,
            "You can quickly fire multiple shots with greater control.",
            "When you use Double Shot, you can make the attacks against the same target. You can add an additional action to Double Shot to make three ranged Strikes instead of two. If you do, the penalty is –4. All attacks count toward your multiple attack penalty, but the penalty doesn’t increase until after you’ve made all of them.",
            [MoreDedications.ModNameTrait, Trait.Fighter]
        ).WithPermanentQEffect("Triple Shot Test.", (qf =>
        {
            // Modify Double Shot
            // Might be incompatible with any future localizations.
            QEffect? doubleShotFeatQf = qf.Owner.QEffects.FirstOrDefault((qfToFind) => qfToFind.Description == "You shoot twice in blindingly fast succession.");
            if (doubleShotFeatQf != null)
            {
                // This is a copy-paste of Double Shot. Updates may require this to be updated as well.
                doubleShotFeatQf.ProvideStrikeModifier = (item =>
                {
                    if (!item.HasTrait(Trait.Ranged) || item.HasTrait(Trait.Reload1) || item.HasTrait(Trait.Reload2))
                        return (CombatAction)null;
                    CombatAction basicStrike = qf.Owner.CreateStrike(item);
                    return new CombatAction(qf.Owner,
                            (Illustration)new SideBySideIllustration(item.Illustration, IllustrationName.Twinshot),
                            "Double Shot",
                            [Trait.Fighter, Trait.Flourish, Trait.Basic, Trait.AlwaysHits], // <-- Base game forgot to add Flourish.
                            "Make two Strikes, each with a –2 penalty. Both attacks count toward your multiple attack penalty, but the penalty doesn't increase until after you've made both of them.",
                            (Target)Target.MultipleCreatureTargets(item.DetermineStrikeTarget(RangeKind.Ranged),
                                item.DetermineStrikeTarget(RangeKind.Ranged)))
                        .WithActionCost(2)
                        .WithGoodnessAgainstEnemy((Func<Target, Creature, Creature, float>)((target, a, d) =>
                        {
                            AI ai = a.AI;
                            Creature target2 = d;
                            DiceFormula trueDamageFormula = basicStrike.StrikeModifiers.CalculatedTrueDamageFormula;
                            double expectedDamage = trueDamageFormula != null
                                ? (double)trueDamageFormula.ExpectedValueMinimumOne
                                : 0.0;
                            CombatAction ownerAction = target.OwnerAction;
                            return ai.DealDamage(target2, (float)expectedDamage, ownerAction);
                        })).WithEffectOnChosenTargets((Func<Creature, ChosenTargets, Task>)(async (fighter, targets) =>
                        {
                            int map = fighter.Actions.AttackedThisManyTimesThisTurn;
                            QEffect qPenalty = new QEffect("Double Shot penalty", "[this condition has no description]",
                                ExpirationCondition.Never, fighter, (Illustration)IllustrationName.None)
                            {
                                BonusToAttackRolls = ((qf2, ca, de) =>
                                    new Bonus(-2, BonusType.Untyped, "Double Shot penalty"))
                            };
                            fighter.AddQEffect(qPenalty);
                            int num11 = (int)await fighter.MakeStrike(targets.ChosenCreatures[0], item, map);
                            int num12 = (int)await fighter.MakeStrike(targets.ChosenCreatures[1], item, map);
                            fighter.RemoveAllQEffects((Func<QEffect, bool>)(qfr => qfr == qPenalty));
                        })).WithTargetingTooltip(
                            (Func<CombatAction, Creature, int, string>)((power, target, index) => power.Description));
                });

            }
            
            // Create Triple Shot
            // This is a copy-paste of Double Shot. Updates may require this to be updated as well.
            qf.ProvideStrikeModifier = (Func<Item, CombatAction>)(item =>
                {
                    if (!item.HasTrait(Trait.Ranged) || item.HasTrait(Trait.Reload1) || item.HasTrait(Trait.Reload2))
                        return (CombatAction)null;
                    CombatAction basicStrike = qf.Owner.CreateStrike(item);
                    return new CombatAction(qf.Owner,
                            (Illustration)new SideBySideIllustration(item.Illustration, (Illustration)IllustrationName.Twinshot),
                            "Triple Shot", 
                            [Trait.Fighter, Trait.Flourish, Trait.Basic, Trait.AlwaysHits], // <-- Base game forgot to add Flourish.
                        "Make three Strikes, each with a –4 penalty. All attacks count toward your multiple attack penalty, but the penalty doesn't increase until after you've made all of them.",
                            (Target)Target.MultipleCreatureTargets(item.DetermineStrikeTarget(RangeKind.Ranged),
                                item.DetermineStrikeTarget(RangeKind.Ranged),
                                item.DetermineStrikeTarget(RangeKind.Ranged)))
                        .WithActionCost(3)
                        .WithGoodnessAgainstEnemy((Func<Target, Creature, Creature, float>)((target, a, d) =>
                        {
                            AI ai = a.AI;
                            Creature target2 = d;
                            DiceFormula trueDamageFormula = basicStrike.StrikeModifiers.CalculatedTrueDamageFormula;
                            double expectedDamage = trueDamageFormula != null
                                ? (double)trueDamageFormula.ExpectedValueMinimumOne
                                : 0.0;
                            CombatAction ownerAction = target.OwnerAction;
                            return ai.DealDamage(target2, (float)expectedDamage, ownerAction);
                        })).WithEffectOnChosenTargets((Func<Creature, ChosenTargets, Task>)(async (fighter, targets) =>
                        {
                            int map = fighter.Actions.AttackedThisManyTimesThisTurn;
                            QEffect qPenalty = new QEffect("Triple Shot penalty", "[this condition has no description]",
                                ExpirationCondition.Never, fighter, (Illustration)IllustrationName.None)
                            {
                                BonusToAttackRolls = (Func<QEffect, CombatAction, Creature, Bonus>)((qf2, ca, de) =>
                                    new Bonus(-3, BonusType.Untyped, "Triple Shot penalty"))
                            };
                            fighter.AddQEffect(qPenalty);
                            int num11 = (int)await fighter.MakeStrike(targets.ChosenCreatures[0], item, map);
                            int num12 = (int)await fighter.MakeStrike(targets.ChosenCreatures[1], item, map);
                            int num13 = (int)await fighter.MakeStrike(targets.ChosenCreatures[2], item, map);
                            fighter.RemoveAllQEffects((Func<QEffect, bool>)(qfr => qfr == qPenalty));
                        })).WithTargetingTooltip(
                            (Func<CombatAction, Creature, int, string>)((power, target, index) => power.Description));
                });
        })).WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.HasFeat(FeatName.DoubleShot) || sheet.HasFeat(ArcherDoubleShotFeat), "You must have the Double Shot feat.");
        ModManager.AddFeat(FighterTripleShotFeat);
        
        ArcherTripleShotFeat = new TrueFeat(
            ModManager.RegisterFeatName("MoreDedications.ArcherTripleShot", FighterTripleShotFeat.Name + " (Archer)"),
            8,
            FighterTripleShotFeat.FlavorText,
            FighterTripleShotFeat.RulesText,
            [MoreDedications.ModNameTrait, FeatArchetype.ArchetypeTrait]
        ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
        {
            sheet.GrantFeat(FighterTripleShotFeat.FeatName);
        }).WithPrerequisite((CalculatedCharacterSheetValues sheet) => Enumerable.Contains(sheet.AllFeats, ArcherDedicationFeat), "You must have the Archer Dedication feat."
        ).WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.HasFeat(FeatName.DoubleShot) || sheet.HasFeat(ArcherDoubleShotFeat), "You must have the Double Shot feat."
        ).WithEquivalent((CalculatedCharacterSheetValues values) => values.AllFeats.Contains(FighterTripleShotFeat));
        ModManager.AddFeat(ArcherTripleShotFeat);
    }
}