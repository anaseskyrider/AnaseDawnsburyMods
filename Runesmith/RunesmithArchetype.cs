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
                "You become trained in Crafting; if you were already trained in Crafting, you instead become trained in a skill of your choice.\n\nYou can use "+ModData.Tooltips.TraitRune("runes")+" like a runesmith. You gain a runic repertoire with two 1st-level runes from the runesmith's rune list. The DCs for these runes is based on your class DC and your Intelligence.\n\nYou can use the "+ModData.Tooltips.ActionTraceRune("Trace Rune "+RulesBlock.GetIconTextFromNumberOfActions(-3))+" and "+ModData.Tooltips.ActionInvokeRune("Invoke Rune "+RulesBlock.GetIconTextFromNumberOfActions(1))+" actions.")
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
                            ft is RuneFeat { Rune.BaseLevel: <= 8 },
                        2)
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
                "You've memorized additional runes.",
                "Add a 1st-level rune to your runic repertoire. If you are an expert in runesmith class DC, you can select a 9th-level rune instead.",
                [])
            .WithMultipleSelection()
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithOnSheet(values =>
            {
                int maxRuneLevel = values.GetProficiency(ModData.Traits.Runesmith) >= Proficiency.Expert ? 9 : 1;
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "rune"+values.CurrentLevel,
                        "Level "+maxRuneLevel+" rune",
                        values.CurrentLevel,
                        ft => ft is RuneFeat rFeat && rFeat.Rune.BaseLevel <= maxRuneLevel)
                    .WithIsOptional());
            });
        ModManager.AddFeat(runesmithLearnRune);

        Feat runesmithExpertDC = new TrueFeat(
                ModManager.RegisterFeatName("RunesmithPlaytest.Archetype.ExpertRunicApplication", "Expert Runic Application"),
                12,
                "Your expertise in magical scripting lends further power to your runic magic.", // "Expert Runes" level 7 feature flavor-text
                "You become an expert in runesmith class DC. Add a 1st- or 9th-level rune to your runic repertoire.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithOnSheet(values =>
            {
                values.SetProficiency(ModData.Traits.Runesmith, Proficiency.Expert);
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "rune"+values.CurrentLevel,
                        "Level 9 rune",
                        values.CurrentLevel,
                        ft => ft is RuneFeat { Rune.BaseLevel: <= 9 })
                    .WithIsOptional());
            });
        ModManager.AddFeat(runesmithExpertDC);
    }
}