using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeArcher
{
    public static void LoadMod()
    {
        // Quick Shot: Add Quick Draw to Archer Dedication
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(FeatName.QuickDraw, Trait.Archer, 4));
        
        // Advanced Bow Training
        TrueFeat advancedBowTrainingFeat = (new TrueFeat(
            Enums.FeatNames.AdvancedBowTraining,
            6,
            "Through constant practice and the crucible of experience, you increase your skill with advanced bows.",
            "You gain proficiency with all advanced bows as if they were martial weapons in the bow weapon group.",
            [Enums.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(Trait.Archer)
            .WithOnSheet(values =>
            {
                values.Proficiencies.AddProficiencyAdjustment(traits =>
                        traits.Contains(Trait.Bow) && traits.Contains(Trait.Advanced), Trait.Martial
                );

                // Fighter compatibility
                values.AtEndOfRecalculation += values2 =>
                {
                    Feat? fighterWeaponMastery = values2.AllFeats
                        .Where(feat => feat.HasTrait(Trait.FighterWeaponMasteryWeaponGroup))
                        .FirstOrDefault((Feat?)null);

                    if (fighterWeaponMastery != null)
                    {
                        Trait fighterWeaponTrait =
                            ((FighterWeaponMasteryWeaponGroupFeat)fighterWeaponMastery).WeaponGroup;
                        values2.Proficiencies.AddProficiencyAdjustment(traits =>
                                traits.Contains(Trait.Bow) && traits.Contains(Trait.Advanced), fighterWeaponTrait
                        );
                    }
                };
            }) as TrueFeat)!;
        ModManager.AddFeat(advancedBowTrainingFeat);
        
        // Crossbow Terror
        TrueFeat crossbowTerrorFeat = new TrueFeat(
                Enums.FeatNames.CrossbowTerror,
            6,
            "You are a dynamo with the crossbow.",
            "You gain a +2 circumstance bonus to damage with crossbows. If the crossbow is a simple weapon, also increase the damage die size for your attacks made with that crossbow by one step. As normal, this damage die increase can't be combined with other abilities that alter the weapon damage die (such as the ranger feat Crossbow Ace).",
            [Enums.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(Trait.Archer)
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
                });
        ModManager.AddFeat(crossbowTerrorFeat);
        
        // Parting Shot
        TrueFeat fighterPartingShotFeat = new TrueFeat(
                Enums.FeatNames.FighterPartingShot,
            4,
            "You jump back and fire a quick shot that catches your opponent off guard.",
            "{b}Requirements{/b} You are wielding a loaded ranged weapon or a ranged weapon without reload 1 or reload 2.\n\nYou Step and then make a ranged Strike with the required weapon. Your target is flat-footed against the attack.",
            [Trait.Fighter, Enums.Traits.MoreDedications])
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
        ModManager.AddFeat(fighterPartingShotFeat);
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(Enums.FeatNames.FighterPartingShot, Trait.Archer, 6));
        
        // Running Reload
        // Code from SudoProgramming's Gunslinger.cs.
        // This isn't an attribution, I just need to know the reference for when I inevitably forget how this code works after needing to make changes.
        TrueFeat rangerRunningReload = new TrueFeat(
                Enums.FeatNames.RangerRunningReload,
            4,
            "You can reload your weapon on the move.",
            "You Stride, Step, or Sneak, then Interact to reload.\n\n{i}(This feat might not support modded firearms.){/i}",
            [Trait.Ranger, Enums.Traits.MoreDedications])
            .WithActionCost(1)
            .WithPermanentQEffect("Stride and reload", qfFeat =>
            {
                // Adds a permanent Running Reload action if the appropriate weapon is held
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, possibilitySection) =>
                {
                    if (possibilitySection.PossibilitySectionId != PossibilitySectionId.MainActions)
                        return null;
                    
                    SubmenuPossibility runningReloadMenu = new SubmenuPossibility(IllustrationName.WarpStep, "Running Reload");
                        
                    foreach (Item heldItem in qfThis.Owner.HeldItems)
                    {
                        bool isReloadable = heldItem.HasTrait(Trait.Reload1) || heldItem.HasTrait(Trait.Reload2) || heldItem.HasTrait(Trait.Repeating); // Modify for compatibility.
                        if (isReloadable && heldItem.WeaponProperties != null)
                        {
                            PossibilitySection runningReloadSection = new PossibilitySection(heldItem.Name);
                            CombatAction itemAction = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(heldItem.Illustration, IllustrationName.WarpStep),
                                "Running Reload",
                                [Trait.Basic],
                                "You Stride, Step, or Sneak, then Interact to reload.",
                                Target.Self()
                                    .WithAdditionalRestriction(user =>
                                    {
                                        bool needsReload = heldItem.EphemeralItemProperties != null && heldItem.EphemeralItemProperties.NeedsReload;
                                        bool isLowMag = (heldItem.HasTrait(Trait.Repeating) && heldItem.EphemeralItemProperties.AmmunitionLeftInMagazine < 5) || false; // Modify for later compatibility.
                                        if (!needsReload && !isLowMag)
                                        {
                                            return "Can not be reloaded.";
                                        }
                                        return null;
                                    }))
                                .WithActionCost(1)
                                .WithItem(heldItem)
                                .WithEffectOnSelf(async (action, self) =>
                                {
                                    if (!await self.StrideAsync("Choose where to Stride with Running Reload.", allowCancel: true))
                                        action.RevertRequested = true;
                                    else
                                        await self.CreateReload(heldItem).WithActionCost(0).WithItem(heldItem).AllExecute();
                                });
                            ActionPossibility itemPossibility = new ActionPossibility(itemAction);

                            runningReloadSection.AddPossibility(itemPossibility);
                            runningReloadMenu.Subsections.Add(runningReloadSection);
                        }
                    }

                    return runningReloadMenu;

                };
            });
        ModManager.AddFeat(rangerRunningReload);
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(Enums.FeatNames.RangerRunningReload, Trait.Archer, 6));
        
        // TODO: Staggering Fire (lv6)
        
        // Archer's Aim
        TrueFeat archersAim = new TrueFeat(
                Enums.FeatNames.ArchersAim,
            8,
            "You slow down, focus, and take a careful shot.",
            "Make a ranged Strike with a weapon in the bow weapon group. You gain a +2 circumstance bonus to the attack roll and ignore the target's concealed condition. If the target is hidden, reduce the flat check from being hidden from 11 to 5.",
            [Trait.Concentrate, Enums.Traits.MoreDedications])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(Trait.Archer)
            .WithPermanentQEffect("You can make a careful shot.", qfFeat =>
            {
                const string actionName = "Archer's Aim";
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Ranged) || !item.HasTrait(Trait.Bow))
                        return null;
                    CombatAction strike = qfFeat.Owner.CreateStrike(item).WithActionCost(2);
                    strike.Name = actionName;
                    strike.Illustration = new SideBySideIllustration(strike.Illustration, IllustrationName.TargetSheet);
                    strike.Description = StrikeRules.CreateBasicStrikeDescription2(strike.StrikeModifiers,
                        "You gain a +2 circumstance bonus to the attack roll, ignore the target's concealed condition, and reduce flat checks due to hidden to 5.\n\n(NOTE: Accuracy preview against hidden creatures doesn't use a lower DC.)");
                    strike.StrikeModifiers.HuntersAim = true;
                    strike.StrikeModifiers.AdditionalBonusesToAttackRoll =
                        [new Bonus(2, BonusType.Circumstance, "Archer's Aim")];
                    // Apply BlindFight before strike is made.
                    strike.WithPrologueEffectOnChosenTargetsBeforeRolls(async (combatAction, creature, chosenTargets) =>
                    {
                        combatAction.Owner.AddQEffect(new QEffect() { Id = QEffectId.BlindFight, Tag = actionName});
                    });
                    // Remove BlindFight after strike is made.
                    strike.StrikeModifiers.OnEachTarget = async (attacker, defender, checkResult) =>
                    {
                        attacker.RemoveAllQEffects(qf => qf.Tag is actionName);
                    };
                    return strike;
                };
            });
        ModManager.AddFeat(archersAim);
    }
}