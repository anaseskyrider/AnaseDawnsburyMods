using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class QuickRepair
{
    public static void LoadFeat()
    {
        Feat repairFeat = new TrueFeat(
            ModData.FeatNames.QuickRepair,
            1,
            "You can repair damage, even in combat.",
            "{b}Range{/b} touch\n{b}Requirements{/b} You must have a hand free.\n\nChoose a construct. Make a Crafting check against DC 15."
                + S.FourDegreesOfSuccess(
                    "The construct regains 4d8 HP.",
                    "The construct regains 2d8 HP.",
                    null,
                    "The construct takes 1d8 damage.")
                + "\n\nRegardless of your result, the construct is then temporarily immune to your Quick Repair for the rest of the day.\n\nIf you are expert or higher with Crafting, you can choose to make the check at a higher DC for additional HP restored. At expert, you can choose DC 20 for +10 HP, master can choose DC 30 for +30 HP, and legendary can choose DC 40 for +50 HP.\n\n"
                + new ModdedIllustration(ModData.Illustrations.DDSunPath).IllustrationAsIconString + "{b}Modding{/b} This skill feat is intended for use with mods which provide a construct companion, or for other summons",
            [Trait.General, Trait.Manipulate, Trait.Skill])
            .WithActionCost(1)
            .WithPrerequisite(values => values.GetProficiency(Trait.Crafting) >= Proficiency.Trained, "You must be trained in Crafting.")
            .WithPermanentQEffect("You can repair constructs as an 'other action'.", qfFeat =>
            {
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers || qfThis.Owner.PersistentCharacterSheet == null)
                        return null;

                    Proficiency craftingTraining = qfThis.Owner.PersistentCharacterSheet.Calculated.GetProficiency(Trait.Crafting);

                    List<Possibility> repairLevels = [];
                    
                    for (Proficiency i = Proficiency.Trained; i <= Proficiency.Legendary; i += 2)
                    {
                        if (i <= craftingTraining)
                            repairLevels.Add((ActionPossibility) CreateQuickRepairAction(qfThis.Owner, i));
                    }
                    
                    return new SubmenuPossibility(ModData.Illustrations.QuickRepair, "Quick Repair")
                    {
                        SpellIfAny = CreateQuickRepairAction(qfThis.Owner, null),
                        Subsections =
                        {
                            new PossibilitySection("Quick Repair")
                                { Possibilities = repairLevels}
                        }
                    };
                };
            });
        ModManager.AddFeat(repairFeat);
    }

    public static CombatAction CreateQuickRepairAction(Creature owner, Proficiency? prof)
    {
        int bonusHealing;
        int dc;
        switch (prof)
        {
            case Proficiency.Expert:
                bonusHealing = 10;
                dc = 20;
                break;
            case Proficiency.Master:
                bonusHealing = 30;
                dc = 30;
                break;
            case Proficiency.Legendary:
                bonusHealing = 50;
                dc = 40;
                break;
            default:
                bonusHealing = 0;
                dc = 15;
                break;
        }
        CombatAction repairAction = new CombatAction(
                owner,
                ModData.Illustrations.QuickRepair,
                $"Quick Repair (DC {dc})",
                [ModData.Traits.MoreBasicActions, Trait.Manipulate, Trait.Basic],
                "{i}You can repair damage, even in combat.{/i}\n"
                    + "{b}Range{/b} touch\n{b}Requirements{/b} You must have a hand free.\n\nChoose a construct. Make a Crafting check against DC "+dc+"."
                    + S.FourDegreesOfSuccess(
                        "The construct regains 4d8"+(bonusHealing > 0 ? "{b}+"+bonusHealing+"{/b}" : null)+" HP.",
                        "The construct regains 2d8"+(bonusHealing > 0 ? "{b}+"+bonusHealing+"{/b}" : null)+" HP.",
                        null,
                        "The construct takes 1d8 damage.")
                    + "\n\nRegardless of your result, the target is then temporarily immune to your Quick Repair for the rest of the day."
                    + (prof == null ? "\n\nIf you are at least expert with Crafting, you can choose to make the check at a higher DC for additional HP restored. At expert, you can choose DC 20 for +10 HP, master can choose DC 30 for +30 HP, and legendary can choose DC 40 for +50 HP." : null),
                Target.AdjacentFriendOrSelf()
                    .WithAdditionalConditionOnTargetCreature((a, d) =>
                    {
                        if (!a.HasFreeHand)
                            return Usability.CommonReasons.NoFreeHandForManeuver;
                        if (d.Damage == 0)
                            return Usability.NotUsableOnThisCreature("healthy");
                        if (!d.HasTrait(Trait.Construct))
                            return Usability.NotUsableOnThisCreature("not a construct");
                        if (d.PersistentUsedUpResources.UsedUpActions.Contains("QuickRepairFrom:" + a.Name))
                            return Usability.NotUsableOnThisCreature("immune");
                        return Usability.Usable;
                    }))
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.QuickRepair)
            .WithSoundEffect(SfxName.SwordStrike)
            .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Crafting), Checks.FlatDC(dc)))
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                switch (result)
                {
                    case CheckResult.CriticalFailure:
                        await CommonSpellEffects.DealDirectDamage(thisAction, DiceFormula.FromText("1d8", "Quick Repair (critical failure)"), target, CheckResult.Failure, DamageKind.Bludgeoning); // Feels more appropriate than slashing
                        break;
                    case >= CheckResult.Success:
                        DiceFormula healingAmount = result == CheckResult.CriticalSuccess 
                            ? DiceFormula.FromText("4d8", "Quick Repair (critical success)")
                            : DiceFormula.FromText("2d8", "Quick Repair");
                        if (bonusHealing > 0)
                            healingAmount = healingAmount.Add(DiceFormula.FromText(bonusHealing.ToString(), "Quick Repair (" + prof!.HumanizeLowerCase2() + ")")); // Suppress. If prof is null, bonusHealing is 0.
                        await target.HealAsync(healingAmount, thisAction);
                        Sfxs.Play(SfxName.Healing);
                        break;
                }
                
                target.PersistentUsedUpResources.UsedUpActions.Add("QuickRepairFrom:" + caster.Name);
            });
        return repairAction;
    }
}