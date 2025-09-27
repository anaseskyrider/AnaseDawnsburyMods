using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.Multiclass;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.GuardianClass;

public static class GuardianArchetype
{
    public static void LoadArchetype()
    {
        Feat guardianDedication = ArchetypeFeats.CreateMulticlassDedication(
                ModData.Traits.Guardian,
                "You've learned the advantages of wearing the sturdiest of armor and keeping your enemies' attention focused on you instead of your allies. You can eventually put yourself in harm's way to protect your friends.",
                "You become trained in Athletics; if you were already trained in Athletics, you instead become trained in a skill of your choice.\n\nYou become trained in guardian class DC, and you can use the "+ModData.Tooltips.ActionTaunt("Taunt {icon:Action}")+" action.\n\nYou become trained in light armor and medium armor, or trained in heavy armor if you were already trained in both.\n\nYour proficiency in the armor types granted to you by this feat is equal to your best proficiency in any type of armor (but not unarmored defense). If you are expert in unarmored defense and are at least 13th-level, you also become an expert in the armor types granted to you by this feat.")
            .WithPrerequisite(values => // Can't use the built-in WithDemandsAbility, to avoid non-ORC text.
                    values.FinalAbilityScores.TotalScore(Ability.Strength) >= 14,
                "You must have Strength +2 or more.")
            .WithPrerequisite(values => // Can't use the built-in WithDemandsAbility, to avoid non-ORC text.
                    values.FinalAbilityScores.TotalScore(Ability.Constitution) >= 14,
                "You must have Constitution +2 or more.")
            .WithOnSheet(values =>
            {
                // Athletics
                values.TrainInThisOrSubstitute(Skill.Athletics);
                
                // Taunt
                values.GrantFeat(ModData.FeatNames.Taunt);
                
                // Armor
                List<Trait> grantedArmors = [];
                if (values.Proficiencies.Get(Trait.MediumArmor) >= Proficiency.Trained)
                {
                    values.SetProficiency(Trait.HeavyArmor, Proficiency.Trained);
                    values.Proficiencies.Autoupgrade(
                        [Trait.LightArmor, Trait.MediumArmor, Trait.HeavyArmor, Trait.Armor],
                        [Trait.LightArmor]);
                    values.Proficiencies.Autoupgrade(
                        [Trait.LightArmor, Trait.MediumArmor, Trait.HeavyArmor, Trait.Armor],
                        [Trait.MediumArmor]);
                    values.Proficiencies.Autoupgrade(
                        [Trait.LightArmor, Trait.MediumArmor, Trait.HeavyArmor, Trait.Armor],
                        [Trait.HeavyArmor]);
                    grantedArmors.Add(Trait.HeavyArmor);
                }
                else
                {
                    values.SetProficiency(Trait.LightArmor, Proficiency.Trained);
                    values.SetProficiency(Trait.MediumArmor, Proficiency.Trained);
                    values.Proficiencies.Autoupgrade(
                        [Trait.LightArmor, Trait.MediumArmor, Trait.HeavyArmor, Trait.Armor],
                        [Trait.LightArmor]);
                    values.Proficiencies.Autoupgrade(
                        [Trait.LightArmor, Trait.MediumArmor, Trait.HeavyArmor, Trait.Armor],
                        [Trait.MediumArmor]);
                    grantedArmors.Add(Trait.LightArmor);
                    grantedArmors.Add(Trait.MediumArmor);
                }
                values.AddAtLevel(13, values2 =>
                {
                    if (values2.Proficiencies.Get(Trait.UnarmoredDefense) >= Proficiency.Expert)
                        foreach (Trait armor in grantedArmors)
                            values2.SetProficiency(armor, Proficiency.Expert);
                });
            });
        ModData.FeatNames.GuardianDedication = guardianDedication.FeatName;
        ModManager.AddFeat(guardianDedication);
        
        foreach (Feat feat in ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(ModData.Traits.Guardian, "Defender"))
            ModManager.AddFeat(feat);
        
        ModManager.AddFeat(MulticlassArchetypeFeats.CreateResiliencyFeat(ModData.Traits.Guardian, 10));

        Feat guardiansIntercept = new TrueFeat(
                ModData.FeatNames.GuardiansIntercept,
                6,
                null,
                "You can use the "+ModData.Tooltips.ActionInterceptAttack("Intercept Attack {icon:Reaction}")+" reaction once per combat.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Guardian)
            .WithOnSheet(values =>
            {
                values.GrantFeat(ModData.FeatNames.InterceptAttack);
            })
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.ActionId == ModData.ActionIds.InterceptAttack)
                            qfThis.Owner.AddQEffect(new QEffect()
                            {
                                PreventTakingAction = action2 => action2.ActionId == ModData.ActionIds.InterceptAttack
                                    ? "Already used this encounter"
                                    : null
                            });
                    };
                });
        ModManager.AddFeat(guardiansIntercept);

        Feat armoredResistance = new TrueFeat(
                ModData.FeatNames.ArmoredResistance,
                8,
                null,
                "{b}Requirements{/b} You are wearing medium or heavy armor.\n\nYou gain resistance to physical damage equal to half your character level when you use the Intercept Attack reaction to take damage instead of your ally.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Guardian)
            .WithPrerequisite(ModData.FeatNames.GuardiansIntercept, "Guardian's Intercept")
            .WithPermanentQEffect(
                "You resist some of the physical damage you take when you use Intercept Attack.",
                qfFeat =>
                {
                    if (!ModData.CommonRequirements.MustWearMediumOrHeavyArmor().Satisfied(
                            qfFeat.Owner, qfFeat.Owner))
                        qfFeat.Description = "{Red}(Must be wearing medium or heavy armor){/Red}.";
                    
                    qfFeat.YouBeginAction = async (qfThis, action) =>
                    {
                        if (action.ActionId != ModData.ActionIds.InterceptAttack
                            || !ModData.CommonRequirements.IsWearingMediumOrHeavyArmor(action.Owner))
                            return;
                        
                        qfThis.Owner.AddQEffect(new QEffect()
                        {
                            StateCheck = qfResist =>
                                qfResist.Owner.WeaknessAndResistance.AddSpecialResistance(
                                    "physical",
                                    (_, dk) => dk.IsPhysical(), qfResist.Owner.Level / 2, null),
                            AfterYouTakeAction = async (qfResist, action2) =>
                            {
                                if (action.ActionId != ModData.ActionIds.InterceptAttack)
                                    return;

                                qfResist.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        });
                    };
                });
        ModManager.AddFeat(armoredResistance);

        Feat ironcladFortitude = new TrueFeat(
                ModData.FeatNames.IroncladFortitude,
                12,
                null,
                "Your proficiency rank in Fortitude saves increases to master.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Guardian)
            .WithRulesTextCreator(sheet =>
                "Your proficiency rank in Fortitude saves increases to master."
                + (sheet.Calculated.GetProficiency(Trait.Fortitude) >= Proficiency.Master
                    ? "\n\n{icon:YellowWarning} You are already at least a master in Fortitude"
                    : null))
            .WithPrerequisite(
                values => values.GetProficiency(Trait.Fortitude) >= Proficiency.Expert,
                "You must be an expert in Fortitude saves")
            /*.WithPrerequisite(
                values => values.GetProficiency(Trait.Fortitude) != Proficiency.Master,
                "You are already a master in Fortitude saves")
            .WithPrerequisite(
                values => values.GetProficiency(Trait.Fortitude) != Proficiency.Legendary,
                "You are already legendary in Fortitude saves")*/
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Fortitude, Proficiency.Master);
            });
        ModManager.AddFeat(ironcladFortitude);
    }
}