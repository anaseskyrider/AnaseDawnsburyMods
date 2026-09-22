using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass;

public static class RunesmithArchetype
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        // Lv2: Runesmith Dedication
        Feat runesmithDedication = ArchetypeFeats.CreateMulticlassDedication(
                ModData.Traits.Runesmith,
                "You dabble in the fundamental magic of runes and explore the writing at the heart of the world.",
                $$"""
                 You become trained in runesmith class DC and Crafting; if you were already trained in Crafting, you instead become trained in a skill of your choice.
                 
                 You gain a runic repertoire with two 1st-level {{ModData.Tooltips.TraitRune("runes")}} of your choice.
                 
                 You can {{ModData.FeatNames.EtchRune.ToLink("etch runes")}}. Your magic can sustain up to 1 etched rune at a time. At 9th-level, increase this maximum to 2. At 17th-level, increase it to 3.
                 
                 Additionally, you gain the Solitary Invocation action.
                 """)
            .WithRulesBlockForCombatAction(CreateSolitaryInvocation)
            .WithOnSheet(values =>
            {
                // Dedication skills
                //// DC Might be redundant, but just in case...
                values.SetProficiency(ModData.Traits.Runesmith, Proficiency.Trained);
                //values.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(ModData.Traits.Runesmith), values.Class!.ClassTrait);
                values.TrainInThisOrSubstitute(Skill.Crafting);

                // Runic repertoire
                RunicRepertoireTag repertoire = RunicRepertoireTag.GetOrCreateRepertoire(
                    values,
                    ModData.Traits.Runesmith,
                    1);
                
                // 2 initial runes
                RunicRepertoireTag.AddRuneSelectionOption(
                    values,
                    "RunesmithMulticlassRepertoireLevel1Runes",
                    "Level 1 Runes",
                    1,
                    2);
                
                // Class features
                values.GrantFeat(ModData.FeatNames.EtchRune);
                
                // Increasing etch limit
                values.AddAtLevel(9, values9 =>
                    RunicRepertoireTag.GetRepertoire(values9)?.IncreaseEtchLimit(values9.CurrentLevel));
                values.AddAtLevel(17, values17 =>
                    RunicRepertoireTag.GetRepertoire(values17)?.IncreaseEtchLimit(values17.CurrentLevel));
            })
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                {
                    int maxInvocations = 1;
                    if (qfThis.Owner.HasFeat(ModData.FeatNames.UnboundedInvocations))
                        maxInvocations = 99;
                    (int _, string rangeDesc) = CommonRuneRules.GetInvocationRange(qfThis.Owner, 6);
                    return ("Solitary Invocation {icon:Action}").WithTag("b") +
                           $" [{ModData.Tooltips.TraitInvocation("invocation")}] Invoke {maxInvocations switch {
                               1 => "1 rune",
                               //2 => "up to 2 runes",
                               _ => "up to any number of runes" }} within {rangeDesc}.";
                };
                
                qfFeat.ProvideMainAction = qfThis =>
                    new ActionPossibility(CreateSolitaryInvocation(qfThis.Owner))
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
            })
            // I'd prefer to avoid non-ORC text, so I won't use the built-in WithDemandsAbility.
            .WithPrerequisite(
                values =>
                    (values.HasFeat(FeatName.Multitalented)
                     && values.Ancestries.Contains(Trait.HalfElf))
                    || values.FinalAbilityScores.TotalScore(Ability.Intelligence) >= 14,
                "You must have Intelligence +2 or more.");
        ModData.FeatNames.RunesmithDedication = runesmithDedication.FeatName;
        yield return runesmithDedication;

        // Lv2: Basic Runic Magic
        // Lv4: Advanced Runic Magic
        foreach (Feat feat in ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(ModData.Traits.Runesmith, "Rune Magic"))
        {
            if (feat.Name.Contains("Advanced"))
                ModData.FeatNames.AdvancedRunicMagic = feat.FeatName;
            else
                ModData.FeatNames.BasicRunicMagic = feat.FeatName;
            yield return feat;
        }
        
        // Lv6: Tracing Studies
        yield return new TrueFeat(
                ModData.FeatNames.TracingStudies, 6,
                "You can now create runes in the midst of combat.",
                "You gain the Trace Rune action.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithRulesBlockForCombatAction(cr =>
                new CombatAction(
                        cr,
                        ModData.Illustrations.TraceRune,
                        "Trace Rune",
                        [Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModData.Traits.Runesmith],
                        $$"""
                          {i}{{CommonRuneRules.ACTION_DESCRIPTION_TRACE_RUNE.FLAVOR}}{/i}

                          {{CommonRuneRules.ACTION_DESCRIPTION_TRACE_RUNE.RULES}}
                          """,
                        Target.Self())
                    .WithActionCost(-3))
            .WithOnSheet(values => values.GrantFeat(ModData.FeatNames.TraceRune));
        
        // Lv10: Expanded Repertoire
        yield return new TrueFeat(
                ModData.FeatNames.ExpandedRepertoire, 10,
                "Your knowledge of runes grows.",
                $"Add two {ModData.Tooltips.TraitRune("runes")} of 5th level or lower to your runic repertoire.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithOnSheet(values =>
            {
                RunicRepertoireTag.AddRuneSelectionOption(
                    values,
                    "ExpandedRepertoireRunes",
                    "Expanded Repertoire",
                    5,
                    2);
            });
        
        // Lv12: Runic Expertise
        yield return new TrueFeat(
                ModData.FeatNames.RunicExpertise, 12,
                // Homebrew flavor text
                "Your expertise in magical scripting lends further power to your runic magic.",
                "You become an expert in runesmith class DC.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithOnSheet(values =>
                values.SetProficiency(ModData.Traits.Runesmith, Proficiency.Expert));
        
        // Lv18: Greater Expanded Repertoire
        yield return new TrueFeat(
                ModData.FeatNames.GreaterExpandedRepertoire, 18,
                "You have gained extensive knowledge in the art of creating runes.",
                $"Add two {ModData.Tooltips.TraitRune("runes")} of 9th level or lower to your runic repertoire.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Runesmith)
            .WithOnSheet(values =>
            {
                RunicRepertoireTag.AddRuneSelectionOption(
                    values,
                    "GreaterExpandedRepertoireRunes",
                    "Greater Expanded Repertoire",
                    9,
                    2);
            });
    }

    public static CombatAction CreateSolitaryInvocation(Creature multismith)
    {
        CombatAction invokeRunes = Runesmith
            .InvokeRuneActivity(multismith, 1)
            .WithName("Solitary Invocation");
        
        if (invokeRunes.Tag is (int range, string rangeDesc))
            invokeRunes.WithDescription(
                $"You utter the name of one of your runes within {rangeDesc}, which blazes with power, applying the effect in its Invocation entry. An invoked rune then fades away, its task complete.");
        
        return invokeRunes;
    }
}