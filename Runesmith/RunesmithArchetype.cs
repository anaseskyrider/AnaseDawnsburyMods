using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class RunesmithArchetype
{
    public static void LoadArchetype()
    {
        RunicRepertoireFeat dedicationRepertoire = new RunicRepertoireFeat(
            ModData.FeatNames.DedicationRepertoire,
            ModData.Traits.Runesmith,
            0);
        ModManager.AddFeat(dedicationRepertoire);
        
        Feat runesmithDedication = ArchetypeFeats.CreateMulticlassDedication(
                ModData.Traits.Runesmith,
                "You have dabbled in the scholarly art at the heart of all magic, the rune.",
                "You become trained in Crafting; if you were already trained in Crafting, you instead become trained in a skill of your choice.\n\nYou can use "+ModTooltips.TraitRune("runes")+" like a runesmith. You gain a runic repertoire with two 1st-level runes from the runesmith's rune list. The DCs for these runes is based on your class DC and your Intelligence.\n\nYou can use the "+ModTooltips.ActionTraceRune("Trace Rune "+RulesBlock.GetIconTextFromNumberOfActions(-3))+" and "+ModTooltips.ActionInvokeRune("Invoke Rune "+RulesBlock.GetIconTextFromNumberOfActions(1))+" actions.")
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
                /*values.AddSelectionOptionRightNow(new SingleFeatSelectionOption(
                        "runesmithSkills",
                        "Runesmith skill",
                        values.CurrentLevel,
                        ft =>
                            ft.FeatName is FeatName.Arcana or FeatName.Nature or FeatName.Occultism or FeatName.Religion)
                    .WithIsOptional());*/
                values.GrantFeat(dedicationRepertoire.FeatName);
                values.AddSelectionOptionRightNow(new MultipleFeatSelectionOption(
                        "initialRunes",
                        "Initial level 1 runes",
                        values.CurrentLevel,
                        ft =>
                            ft is RuneFeat, 2)
                    .WithIsOptional());
                values.GrantFeat(ModData.FeatNames.TraceRune);
                values.GrantFeat(ModData.FeatNames.InvokeRune);
                values.SetProficiency(ModData.Traits.Runesmith, Proficiency.Trained); // Might be redundant, but just in case...
                values.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(ModData.Traits.Runesmith), values.Class!.ClassTrait);
            })
            .WithPrerequisite(values => // Can't use the built-in WithDemandsAbility, to avoid non-ORC text.
                values.FinalAbilityScores.TotalScore(Ability.Intelligence) >= 14,
                "You must have Intelligence +2 or more.");
        runesmithDedication.Traits.Add(Trait.Homebrew);
        ModManager.AddFeat(runesmithDedication);

        foreach (Feat feat in ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(ModData.Traits.Runesmith, "Runic Technique"))
        {
            ModManager.AddFeat(feat);
        }

        Feat runesmithLearnRune = new TrueFeat(
                ModManager.RegisterFeatName("RunesmithPlaytest.Archetype.ExpandKnowledge", "Expand Knowledge"),
                2,
                null,
                "You add a 1st-level runesmith rune of your choice to your runic repertoire.",
                [])
            .WithMultipleSelection()
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "rune"+values.CurrentLevel,
                        "Level 1 rune",
                        values.CurrentLevel,
                        ft => ft is RuneFeat { Rune.BaseLevel: 1 })
                    .WithIsOptional());
            });
        ModManager.AddFeat(runesmithLearnRune);
    }
}