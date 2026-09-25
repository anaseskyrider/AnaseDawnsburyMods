using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Selections;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass;

public static class Runesmith
{
    public const string ASSURED_RUNIC_CRAFTER_TAG = "ASSURED_RUNIC_CRAFTER";
    
    public static void LoadClass()
    {
        // Class Features
        foreach (Feat ft in CreateFeatures())
            ModManager.AddFeat(ft);
        
        // Class
        ModManager.AddFeat(CreateClass());
    }

    public static Feat CreateClass()
    {
        Feat runesmithClassFeat = new ClassSelectionFeat(
                ModData.FeatNames.RunesmithClass,
                "At the heart of all communication is the word, and at the heart of all magic is the rune. Equal parts scholar and artist, you devote yourself to the study of these mystic symbols, learning to carve, etch, brand, and paint the building blocks of magic to channel powers greater than yourself.",
                ModData.Traits.Runesmith,
                new EnforcedAbilityBoost(Ability.Intelligence),
                8,
                [Trait.Perception, Trait.Reflex, Trait.Unarmed, Trait.Simple, Trait.Martial, Trait.UnarmoredDefense, Trait.LightArmor, Trait.MediumArmor, Trait.Crafting],
                [Trait.Fortitude, Trait.Will],
                2,
                $$"""
                  {b}1. Runic Repertoire.{/b} Through meticulous study, you have carved the knowledge of secret {{ModData.Tooltips.TraitRune("runesmith runes")}} into your mind and body. You know 4 runes of 1st level, and learn 2 additional runes every 4 levels thereafter. Runes use your runesmith class DC for any saving throws, which is based on your Intelligence.

                  {b}2. Applying Runes.{/b} You can apply runes in one of two ways: {i}tracing{/i} the rune with the {{ModData.FeatNames.TraceRune.ToLink("Trace Rune {icon:Action}–{icon:TwoActions}")}} action, or by {i}etching{/i} the rune at the start of combat with the {{ModData.FeatNames.EtchRune.ToLink("Etch Rune")}} activity.

                  {b}3. Invoking Runes.{/b} You can activate one or more runes' invocation entry with the {{ModData.FeatNames.InvokeRune.ToLink("Invoke Rune {icon:Action}")}} action.

                  {b}4. Runesmith feat.{/b}

                  {b}5. Shield Block {icon:Reaction}{/b} You can use your shield to reduce damage you take from attacks.
                  """,
                null)
            .WithEffectiveClassFeatures(cf =>
            {
                // Features are listed in the order added for each level //
                cf.AddFeature(2, ClassFeature.FromFeat(ModData.FeatNames.RunicCrafter));
                cf.AddFeature(5, new ClassFeature(
                        "Expert in weapons",
                        "unarmed, simple, martial")
                    .WithOnSheet(values =>
                    {
                        values.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                        values.SetProficiency(Trait.Simple, Proficiency.Expert);
                        values.SetProficiency(Trait.Martial, Proficiency.Expert);
                    }));
                cf.AddFeature(7, WellKnownClassFeature.ExpertInClassDC);
                cf.AddFeature(7, WellKnownClassFeature.ExpertInReflex);
                cf.AddFeature(7, ClassFeature.FromFeat(ModData.FeatNames.RunicOptimization));
                cf.AddFeature(9, ClassFeature.FromFeat(ModData.FeatNames.AssuredRunicCrafter));
                cf.AddFeature(11, new ClassFeature(ModData.Tooltips.FeatureForgedEndurance("Forged Endurance"))
                    .WithOnSheet(values =>
                        values.SetProficiency(Trait.Fortitude, Proficiency.Master))
                    .WithOnCreature((values, cr) =>
                        cr.AddQEffect(new QEffect(
                            "Forged Endurance",
                            "When you roll a success on a Fortitude save, you get a critical success instead.")
                        {
                            Innate = false,
                            AddToDefenseBlock = _ => "{b}Forged Endurance.{/b} When you roll a success on a Fortitude save, you get a critical success instead.",
                            AdjustSavingThrowCheckResult = (_, defense, _, checkResult) =>
                                defense != Defense.Fortitude || checkResult != CheckResult.Success
                                    ? checkResult
                                    : CheckResult.CriticalSuccess
                        })));
                cf.AddFeature(13, new ClassFeature(
                        "Expert in defenses",
                        "unarmored, light, medium")
                    .WithOnSheet(values =>
                    {
                        values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Expert);
                        values.SetProficiency(Trait.LightArmor, Proficiency.Expert);
                        values.SetProficiency(Trait.MediumArmor, Proficiency.Expert);
                    }));
                cf.AddFeature(13, WellKnownClassFeature.ExpertInPerception);
                cf.AddFeature(13, new ClassFeature(
                        "Master in weapons",
                        "unarmed, simple, martial")
                    .WithOnSheet(values =>
                    {
                        values.SetProficiency(Trait.Unarmed, Proficiency.Master);
                        values.SetProficiency(Trait.Simple, Proficiency.Master);
                        values.SetProficiency(Trait.Martial, Proficiency.Master);
                    }));
                cf.AddFeature(15, ClassFeature.FromFeat(ModData.FeatNames.GreaterRunicOptimization));
                cf.AddFeature(15, WellKnownClassFeature.MasterInClassDC);
                cf.AddFeature(19, WellKnownClassFeature.LegendaryInClassDC);
                cf.AddFeature(19, new ClassFeature("Master in defenses", "unarmored, light, medium")
                    .WithOnSheet(values =>
                    {
                        values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Master);
                        values.SetProficiency(Trait.LightArmor, Proficiency.Master);
                        values.SetProficiency(Trait.MediumArmor, Proficiency.Master);
                    }));
                
                // Known and etched rune increases
                // Levels 5, 9, 13, 17
                for (int lv = 5; lv <= 20; lv+=4)
                {
                    // Starting at 1 at level 1, every 4 levels, add 4 to the number.
                    string levelName = "level " + (((lv / 4) * 4) + 1);
                    cf.AddFeature(lv, new ClassFeature("2 additional runes known", levelName)
                        .WithOnSheet(values =>
                        {
                            RunicRepertoireTag.AddRuneSelectionOption(
                                values,
                                $"RunesmithRepertoireLevel{values.CurrentLevel}Runes",
                                $"Level {values.CurrentLevel} Runes",
                                values.CurrentLevel,
                                2);
                        }));
                    
                    int numEtch = 2/*Base*/ + (lv / 4)/*+1 every 4 levels*/;
                    cf.AddFeature(lv, new ClassFeature("Etch limit increase", numEtch + " runes")
                        .WithOnSheet(values =>
                            RunicRepertoireTag.GetRepertoire(values)?.IncreaseEtchLimit(values.CurrentLevel)));
                }
            })
            .WithOnSheet(values =>
            {
                // Bonus tradition skill
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "runesmithSkills",
                        "Runesmith skill",
                        1,
                        ft =>
                            ft.FeatName is FeatName.Arcana or FeatName.Nature or FeatName.Occultism or FeatName.Religion)
                    .WithIsOptional());
                
                // Runic repertoire
                RunicRepertoireTag repertoire = RunicRepertoireTag.GetOrCreateRepertoire(
                    values,
                    ModData.Traits.Runesmith,
                    2);
                
                // 4 initial runes
                RunicRepertoireTag.AddRuneSelectionOption(
                    values,
                    "RunesmithRepertoireLevel1Runes",
                    "Level 1 Runes",
                    1,
                    4);
                
                // Class features
                values.GrantFeat(ModData.FeatNames.TraceRune);
                values.GrantFeat(ModData.FeatNames.InvokeRune);
                values.GrantFeat(ModData.FeatNames.EtchRune);
                
                // Bonus feats
                values.GrantFeat(FeatName.ShieldBlock);
                
                // Class feat
                values.AddClassFeatOption("RunesmithFeat1", ModData.Traits.Runesmith, 1);
            });
        runesmithClassFeat.RulesText = runesmithClassFeat.RulesText
            .Replace("Key ability", "Key attribute")
            .Replace("trained in Crafting", "trained in Crafting; as well as your choice of Arcana, Nature, Occultism, or Religion;")
            .Replace("Ability boosts", "Attribute boosts");
        
        return runesmithClassFeat;
    }
    
    public static IEnumerable<Feat> CreateFeatures()
    {
        // Trace Rune
        yield return new Feat(
                ModData.FeatNames.TraceRune,
                CommonRuneRules.ACTION_DESCRIPTION_TRACE_RUNE.FLAVOR,
                CommonRuneRules.ACTION_DESCRIPTION_TRACE_RUNE.RULES,
                [Trait.Concentrate, Trait.Magical, Trait.Manipulate],
                null)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ => "{b}Trace Rune {icon:Action}–{icon:TwoActions}{/b} [concentrate, manipulate] Apply a rune to an adjacent target, or up to 30 feet away.";
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    RunicRepertoireTag? repertoire = RunicRepertoireTag.GetRepertoire(qfThis.Owner);
                    if (repertoire == null)
                        return null;
                    
                    List<Possibility> traceRunePossibilities = [];
                    // Don't make the 1-action version if you have RuneSinger.
                    bool hasRuneSinger = qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger);
                    
                    foreach (Rune rune in repertoire.GetTraceableRunes(qfThis.Owner))
                    {
                        List<Possibility> specificRunePossibilities = [];
                        if (!hasRuneSinger)
                        {
                            CombatAction traceRuneOne = CommonRuneRules
                                .CreateTraceAction(qfThis.Owner, rune, 1)
                                .WithExtraTrait(Trait.Basic);
                            
                            traceRuneOne.Description = CommonRuneRules.CreateTraceActionDescription(rune, qfThis.Owner.Level,
                                withFlavorText: false,
                                withUsageText: false);
                            traceRuneOne.ShortName = traceRuneOne.Name; // Combat log asks for ShortName, then ContextMenuName, then Name. This prevents it from printing the action symbols more than once.
                            traceRuneOne.ContextMenuName = $"{{icon:Action}} {traceRuneOne.Name}";
                            traceRuneOne.WithAdjustTarget<CreatureTarget>(crTar => crTar
                                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                    attacker.FindQEffect(ModData.QEffectIds.DrawnInVitalInk)?.Tag == defender
                                        ? Usability.NotUsableOnThisCreature("use Drawn in Vital Ink")
                                        : Usability.Usable));
                            
                            ActionPossibility tracePossOne = new ActionPossibility(traceRuneOne)
                            {
                                Caption = "Touch",
                                Illustration = IllustrationName.Action
                            };
                            specificRunePossibilities.Add(tracePossOne);
                        }
                        
                        CombatAction traceRuneTwo = CommonRuneRules
                            .CreateTraceAction(qfThis.Owner, rune, 2)
                            .WithExtraTrait(Trait.Basic);
                        
                        if (!hasRuneSinger)
                        {
                            // Declutter your options by removing the ranged option while in melee.
                            traceRuneTwo.WithAdjustTarget<CreatureTarget>(crTar => crTar
                                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                    attacker.DistanceTo(defender) <= 1
                                        ? Usability.NotUsableOnThisCreature("use the 1-action version")
                                        : Usability.Usable));
                        }
                        traceRuneTwo.ShortName = traceRuneTwo.Name;
                        traceRuneTwo.ContextMenuName = $"{RulesBlock.GetIconTextFromNumberOfActions(traceRuneTwo.ActionCost)} {traceRuneTwo.Name}";
                        traceRuneTwo
                            .WithDescription(CommonRuneRules.CreateTraceActionDescription(rune, qfThis.Owner.Level,
                                withFlavorText: false,
                                withUsageText: false))
                            .WithAdjustTarget<CreatureTarget>(crTar => crTar
                                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                    attacker.FindQEffect(ModData.QEffectIds.DrawnInVitalInk)?.Tag == defender
                                        ? Usability.NotUsableOnThisCreature("use Drawn in Vital Ink")
                                        : Usability.Usable));
                        
                        ActionPossibility tracePossTwo = new ActionPossibility(traceRuneTwo)
                        {
                            Caption = "30 feet",
                            Illustration = hasRuneSinger
                                ? new SideBySideIllustration(
                                    IllustrationName.Action,
                                    ModData.Illustrations.RuneSinger)
                                : IllustrationName.TwoActions
                        };
                        specificRunePossibilities.Add(tracePossTwo);

                        SubmenuPossibility specificRuneMenu = new SubmenuPossibility(
                            rune.Illustration,
                            rune.FullName,
                            PossibilitySize.Half)
                        {
                            // variable action trace rune
                            SpellIfAny = CommonRuneRules.CreateTraceAction(qfThis.Owner, rune, -3),
                            Subsections =
                            {
                                // rune.Name is how features like Drawn In Vital Ink find these sections.
                                new PossibilitySection(rune.FullName)
                                {
                                    Possibilities = specificRunePossibilities,
                                }
                            }
                        };
                        
                        traceRunePossibilities.Add(specificRuneMenu);
                    }

                    SubmenuPossibility traceRuneMenu = new SubmenuPossibility(
                        ModData.Illustrations.TraceRune,
                        "Trace Rune")
                    {
                        SubmenuId = ModData.SubmenuIds.TraceRune,
                        // This doesn't DO anything, it's just to provide description to the menu.
                        SpellIfAny = new CombatAction(
                            qfThis.Owner, ModData.Illustrations.TraceRune, "Trace Rune", [Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModData.Traits.Runesmith],
                            $$"""
                            {i}{{CommonRuneRules.ACTION_DESCRIPTION_TRACE_RUNE.FLAVOR}}{/i}

                            {{CommonRuneRules.ACTION_DESCRIPTION_TRACE_RUNE.RULES}}
                            """, Target.Self()).WithActionCost(-3),
                        Subsections = { new PossibilitySection("Trace Rune")
                        {
                            Possibilities = traceRunePossibilities,
                        }},
                        PossibilityGroup = ModData.PossibilityGroups.DRAWING_RUNES,
                    };
                    return traceRuneMenu;
                };
            });
        
        // Invoke Rune
        yield return new Feat(
                ModData.FeatNames.InvokeRune,
                CommonRuneRules.ACTION_DESCRIPTION_INVOKE_RUNE.FLAVOR,
                CommonRuneRules.ACTION_DESCRIPTION_INVOKE_RUNE.RULES,
                [ModData.Traits.Invocation, Trait.Magical],
                null)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                {
                    int maxInvocations = 2;
                    if (qfThis.Owner.HasFeat(ModData.FeatNames.UnboundedInvocations))
                        maxInvocations = 99;
                    (int _, string rangeDesc) = CommonRuneRules.GetInvocationRange(qfThis.Owner, 6);
                    return ("Invoke Rune {icon:Action}").WithTag("b") +
                           $" [{ModData.Tooltips.TraitInvocation("invocation")}] Invoke {maxInvocations switch {
                               //1 => "1 rune",
                               2 => "up to 2 runes",
                               _ => "up to any number of runes" }} within {rangeDesc}.";
                };
                
                qfFeat.ProvideMainAction = qfThis =>
                    new ActionPossibility(InvokeRuneActivity(qfThis.Owner))
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
            });
        
        // Etch Rune
        yield return new Feat(
                ModData.FeatNames.EtchRune,
                "An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.",
                "At the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to 2 runes, and you can etch an additional rune at levels 5, 9, 13, and 17.",
                [], null)
            .WithPermanentQEffect(
                "At the start of combat, apply runes to your party (lasts until consumed).",
                qfFeat =>
                {
                    qfFeat./*StartOfCombat =*/StartOfCombatAfterInitiativeOrderIsSetUp = async qfThis =>
                    {
                        RunicRepertoireTag? repertoire = RunicRepertoireTag.GetRepertoire(qfThis.Owner);
                        if (repertoire == null)
                            return;
                        
                        /*// Runic Tattoo first
                        if (qfThis.Owner.HasFeat(ModData.FeatNames.RunicTattoo))
                        {
                            QEffect? runicTattooFeat = qfFeat.Owner.QEffects.FirstOrDefault(qf =>
                                qf.Name is { } name && name.Contains("Runic Tattoo"));
                            if (runicTattooFeat != null)
                                await runicTattooFeat.StartOfCombat!.Invoke(runicTattooFeat);
                        }*/
                        
                        // Get etch data
                        List<Rune> runesKnown = repertoire.GetKnownRunes(qfFeat.Owner);
                        int etchLimit = repertoire.GetEtchLimit(qfThis.Owner);
                        
                        qfThis.Owner.Battle.Log(
                            $"{qfThis.Owner.Name} begins {{b}}Etching Runes{{/b}}.",
                            "Etch Rune",
                            $$"""
                              {i}An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.{/i}

                              At the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to {{etchLimit.WithColor("Blue")}} runes.
                              """,
                            new Traits([Trait.Manipulate, Trait.DoesNotProvoke, ModData.Traits.Runesmith]));
                        
                        // Old implementation
                        /*qfThis.Owner.Overhead(
                            "Etching Runes",
                            Color.Black,
                            $"{qfThis.Owner.Name} begins {{b}}Etching Runes{{/b}}.",
                            "Etch Rune",
                            $"{{i}}An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.{{/i}}\n\nAt the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to {etchLimit} runes.",
                            new Traits([Trait.Manipulate, Trait.DoesNotProvoke, ModData.Traits.Runesmith]));*/

                        for (int i = 0; i < etchLimit; i++)
                        {
                            // Old implementation
                            /*await qfThis.Owner.Battle.GameLoop.StateCheck();
                            
                            List<Option> options = [];
                            foreach (Rune rune in runesKnown)
                            {
                                CombatAction etchThisRune = CommonRuneRules.CreateEtchAction(qfThis.Owner, rune);
                                etchThisRune.Traits.Add(Trait.DoNotShowOverheadOfActionName);
                                GameLoop.AddDirectUsageOnCreatureOptions(etchThisRune, options);
                            }
                            
                            if (options.Count <= 0)
                                continue;
                                
                            options.Add(new PassViaButtonOption(" Confirm no additional etchings "));
                            
                            // Await which option (even if just 1) to take.
                            Option chosenOption = (await qfThis.Owner.Battle.SendRequest( // Send a request to pick an option
                                new AdvancedRequest(qfThis.Owner, "Etch a rune on yourself or an ally.", options)
                                {
                                    TopBarText = $"Etch a rune on yourself or an ally. ({i+1}/{etchLimit})",
                                    TopBarIcon = ModData.Illustrations.EtchRune,
                                })).ChosenOption;
                            
                            switch (chosenOption)
                            {
                                case CreatureOption:
                                    break;
                                case PassViaButtonOption:
                                    return;
                            }

                            await chosenOption.Action();*/

                            if (qfThis.Owner.Actions.ActionHistoryThisEncounter.LastOrDefault() is { Tag: "PassEtching" })
                                break;
                            
                            qfThis.Owner.Overhead("Etching runes ("+(i+1)+"/"+etchLimit+")", Color.Black);

                            PossibilitySection etchRunes = new PossibilitySection(
                                "Etch Rune " + string.Join("", Enumerable.Repeat("{icon:spontaneousspellslot}", etchLimit-i)));
                            foreach (Rune rune in runesKnown)
                            {
                                CombatAction etchThisRune = CommonRuneRules
                                    .CreateEtchAction(qfThis.Owner, rune)
                                    .WithExtraTrait(Trait.DoNotShowOverheadOfActionName);
                                ActionPossibility etchPoss = new ActionPossibility(etchThisRune)
                                {
                                    Caption = (etchThisRune.Tag as RuneActionTag)?.Rune.FullName ?? "[ERROR]"
                                };
                                etchRunes.AddPossibility(etchPoss);
                            }
                            Possibilities etchableRunes = qfThis.Owner.Possibilities.FilterAnyPossibility(_ => false);
                            etchableRunes.Sections.Add(etchRunes);
                            etchableRunes.CannotPass = false;
                            
                            etchableRunes.Sections.Add(new PossibilitySection("Pass")
                            {
                                Possibilities = [new ActionPossibility(new CombatAction(
                                        qfThis.Owner,
                                        IllustrationName.EndTurn,
                                        "Pass",
                                        [Trait.Basic, Trait.UsableEvenWhenUnconsciousOrParalyzed, Trait.DoesNotPreventDelay],
                                        "Do nothing.",
                                        Target.Self())
                                    .WithTag("PassEtching")
                                    .WithActionCost(0))]
                            });
                            
                            var active = qfThis.Owner.Battle.ActiveCreature;
                            qfThis.Owner.Battle.ActiveCreature = qfThis.Owner;
                            qfThis.Owner.Possibilities = etchableRunes;
        
                            List<Option> actions = await qfThis.Owner.Battle.GameLoop.CreateActions(
                                qfThis.Owner,
                                etchableRunes,
                                null);
                            qfThis.Owner.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                            await qfThis.Owner.Battle.GameLoop.OfferOptions(qfThis.Owner, actions, true);
        
                            qfThis.Owner.Battle.ActiveCreature = active;
                        }

                        qfThis.Tag = true; // True means the runes have been etched.
                    };
                });
        
        // Runic Crafter
        yield return new Feat(
                ModData.FeatNames.RunicCrafter,
                "Your study of secret runes leaves you well practiced in crafting their more common cousins.",
                "Your equipment gains the effects of the highest level fundamental armor, shield, and weapon runes for your level. This does not count as having runes for the purposes of other rules (you must still have potency runes to apply property runes).\n\n{Red}(NYI){/Red} Fundamental shield runes.",
                [], null)
            .WithOnCreature(RunicCrafterEffect)
            .WithLevel(2);
        
        // Runic Optimization
        yield return new Feat(
                ModData.FeatNames.RunicOptimization,
                "You've learned to draw upon the magic of weapon runes to deal greater damage.",
                $"You deal 2 additional damage with weapons bearing a {ItemName.StrikingRunestone.ToLink("striking rune").WithTag("i")}. This damage increases to 3 if the weapon bears a {ItemName.GreaterStrikingRunestone.ToLink("greater striking rune").WithTag("i")} and 4 if it bears a {ItemName.MajorStrikingRunestone.ToLink("major striking rune").WithTag("i")}.",
                [], null)
            .WithOnCreature(cr =>
                RunicOptimization(cr.Level >= 15))
            .WithLevel(7);
        
        // Assured Runic Crafter
        yield return new Feat(
                ModData.FeatNames.AssuredRunicCrafter,
                "You're so used to tracing and etching runes in the field that when given the peace and quiet of a proper workshop, it’s hard for things to go too astray.",
                $"{{b}}Precombat Preparations{{/b}} You can select one ally to gain the benefits of your {ModData.FeatNames.RunicCrafter.ToLink("Runic Crafter")} feature.",
                [], null)
            .WithOnSheet(values =>
            {
                values.AtEndOfRecalculationBeforeMorningPreparations = valuesBefore =>
                {
                    List<CharacterSheet?> heroes;
                    if (CampaignState.Instance?.Heroes is { } apHeroes)
                        heroes = apHeroes
                            .Select(hero => hero.CharacterSheet)
                            .Cast<CharacterSheet?>()
                            .ToList();
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    else if (CharacterLibrary.Instance?.SelectedRandomEncounterParty is { } library)
                        heroes = library.ToList();
                    else
                        return;
                    
                    valuesBefore.AddSelectionOption(new LimitedTextSelectionOption(
                        "AssuredRunicCrafter",
                        "Assured Runic Crafter",
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                        heroes
                            .Where(sheet =>
                                // Can't be a runesmith with Runic Crafter
                                sheet?.Calculated.HasFeat(ModData.FeatNames.RunicCrafter) != true
                                // Fallback: Can't be yourself
                                && sheet != values.Sheet)
                            .Select(sheet =>
                            {
                                if (sheet is null)
                                    return null;
                                int index = heroes.IndexOf(sheet);
                                return new FeatlikeChoice(
                                    $"AssuredRunicCrafter.Player{index}",
                                    //$"Player Character {index}")
                                    $"Grant Runic Crafter to {sheet.Name}")
                                {
                                    Illustration = sheet.Illustration,
                                    TextCreator = () =>
                                        $"{sheet.Name} will gain the benefits of your {ModData.FeatNames.RunicCrafter.ToLink("Runic Crafter")} feature at the start of combat.",
                                    // Add this key to your sheet
                                    Apply = valuesApply =>
                                        valuesApply.Tags.TryAdd(ASSURED_RUNIC_CRAFTER_TAG, index)
                                };
                            })
                            .WhereNotNull()
                            .ToArray()));
                };
            })
            .WithPermanentQEffect(
                "An ally of your choice gains the effects of your Runic Crafter feature.",
                qfFeat =>
                {
                    qfFeat.StartOfCombat = async qfThis =>
                    {
                        if (qfThis.Owner.PersistentCharacterSheet?.Calculated.GetTagOrNull<int>(
                                ASSURED_RUNIC_CRAFTER_TAG) is not { } index
                            || CharacterSheet.GetCharacterSheetFromPartyMember(index) is not {} sheet
                            || qfThis.Owner.Battle.AllCreatures.FirstOrDefault(cr =>
                                cr.PersistentCharacterSheet == sheet) is not {} ally
                            || ally.HasEffect(ModData.QEffectIds.RunicCrafter))
                            return;
                        
                        QEffect crafter = RunicCrafterEffect(qfFeat.Owner, ally.Level);
                        crafter.Name = "Assured Runic Crafter";
                        crafter.DoNotShowUpOverhead = false;
                        crafter.Innate = false;
                        crafter.Illustration = ModData.Illustrations.AssuredRunicCrafter;
                        crafter.Description = $"{{i}}(Granted by {qfThis.Owner.ToColoredBoldedName()}){{/i}}\n" + crafter.Description;
                        ally.AddQEffect(crafter);
                    };
                })
            .WithLevel(9);
        
        // Greater Runic Optimization
        yield return new Feat(
            ModData.FeatNames.GreaterRunicOptimization,
            "Your weapon runes harmonize with your own innate magic, multiplying their damage even further.",
            $"Your extra damage from {ModData.FeatNames.RunicOptimization.ToLink("runic optimization")} increases to 4 with weapons bearing a {ItemName.StrikingRunestone.ToLink("striking rune").WithTag("i")}, 6 for a {ItemName.GreaterStrikingRunestone.ToLink("greater striking rune").WithTag("i")}, and 8 for a {ItemName.MajorStrikingRunestone.ToLink("major striking rune").WithTag("i")}.",
            [], null)
            .WithLevel(15);
    }

    public static CombatAction InvokeRuneActivity(Creature runesmith, int maxInvocations = 2)
    {
        const int baseRange = 6;
        (int range, string rangeDesc) = CommonRuneRules.GetInvocationRange(runesmith, baseRange);

        if (runesmith.HasFeat(ModData.FeatNames.UnboundedInvocations))
            maxInvocations = 99;
        
        CombatAction invokeRunes = new CombatAction(
                runesmith,
                ModData.Illustrations.InvokeRune,
                "Invoke Rune",
                // Deafened is interpreted to not 'the DC5-check' for spellcasting in Dawnsbury,
                // so nothing applies here either.
                [ModData.ModTrait, ModData.Traits.Invocation, Trait.Magical, ModData.Traits.Runesmith, Trait.Spell, Trait.Basic, Trait.DoNotShowOverheadOfActionName, Trait.UnaffectedByConcealment],
                $$"""
                {i}{{CommonRuneRules.ACTION_DESCRIPTION_INVOKE_RUNE.FLAVOR}}{/i}
                
                {{CommonRuneRules.ACTION_DESCRIPTION_INVOKE_RUNE.RULES.Replace("30 feet", rangeDesc)}}
                """,
                Target.Self()
                    .WithAdditionalRestriction(caster =>
                    {
                        // PETR: Can't use if Silenced.
                        //bool cannotSpeak = caster.HasEffect(QEffectId.) != null;
                        //if (cannotSpeak)
                        //  return "Cannot speak in a strong voice";

                        return NumRunesInRange(caster) > 0 ? null : "No rune-bearers within range";
                    }))
            .WithTag((range, rangeDesc)) // Store the range info to make the archetype easier to adjust
            /*.WithShortDescription($"Invoke {maxInvocations switch
            {
                1 => "1 rune",
                2 => "up to 2 runes",
                _ => "up to any number of runes"
            }} within {rangeDesc}.")*/
            .WithActionCost(1)
            .WithEffectOnEachTarget(async (thisAction, self, _,_) =>
            {
                // Number of runes on the field.
                int numberOfRunes = NumRunesInRange(self);

                // For each valid rune in play, attempt to take an invoke action, up to all our runes.
                int whileProtection = 0;
                int invoked = 0;
                while (numberOfRunes > 0 && whileProtection < 100 && invoked < maxInvocations)
                {
                    await self.Battle.GameLoop.StateCheck(); // Idk why but they all do this so keep it.
                    if (!await CommonRuneRules.ChooseARuneToInvoke(
                            self,
                            // This is redundant, but might as well use it due to precalculation
                            overrideRange: range,
                            canBeCanceled: whileProtection == 0,
                            passText: invoked == 0 ? " Revert action " : " Confirm no additional runes ",
                            additionalTopText: maxInvocations > 1
                                ? $" You should avoid invoking the same rune on the same creature more than once.{(maxInvocations < 99 ? $" ({invoked + 1}/{maxInvocations})" : null)}"
                                : null))
                    {
                        thisAction.RevertRequested = true;
                        return;
                    }
                    whileProtection++;
                    invoked++;
                    numberOfRunes = NumRunesInRange(self); // Regenerate the list of runes.
                }
            });
        CommonRuneRules.WithImmediatelyRemovesImmunity(invokeRunes); 
        
        return invokeRunes;

        int NumRunesInRange(Creature caster)
        {
            return caster.Battle.AllCreatures
                .Where(cr =>
                    caster.DistanceTo(cr) <= range) // Must be within range.
                .Sum(cr =>
                    DrawnRune.GetDrawnRunes(caster, cr)
                        .Where(dr => DrawnRune.IsInvokeableRune(caster, dr))
                        .ToList()
                        .Count);
        }
    }
    
    public static QEffect RunicOptimization(bool greater)
    {
        string specializationName = (greater ? "Greater " : null) + "Runic Optimization";
        string description = $"You deal an additional {BonusFromDice(2)} damage with weapons and unarmed attacks bearing a {{i}}striking rune{{/i}}. This damage increases to {BonusFromDice(3)} if it bears a {{i}}greater striking rune{{/i}}, and {BonusFromDice(4)} if it bears a {{i}}major striking rune{{/i}}.";
        return new QEffect(specializationName, description)
        {
            BonusToDamage = (qfThis, action, target) =>
            {
                if (action.Item?.WeaponProperties is null)
                    return null;
                int bonusAmount = BonusFromDice(action.Item.WeaponProperties.DamageDieCount);
                return bonusAmount > 0
                    ? new Bonus(bonusAmount, BonusType.Untyped, specializationName)
                    : null;
            }
        };

        // Striking = 2 dice, Greater = 3 dice, Major = 4 dice.
        // This bonus maps to the base feature, and double it maps to the greater feature.
        // This requires at least striking, just as trained attacks have no bonuses.
        int BonusFromDice(int numDice)
        {
            if (numDice < 2)
                return 0;
            return numDice * (greater ? 2 : 1);
        }
    }

    public static QEffect RunicCrafterEffect(Creature runesmith)
    {
        return RunicCrafterEffect(runesmith, runesmith.Level);
    }
    
    // TODO: Reinforcing shield rune bonus. They're pretty complicated as they are increases atop the base value instead of "set to" values, except they also have a cap that increases as well.
    public static QEffect RunicCrafterEffect(Creature runesmith, int ownerLevel)
    {
        // Get values
        int atkBonus = GetAttackBonus(ownerLevel);
        int striking = GetDiceBonus(ownerLevel);
        int acBonus = GetACBonus(ownerLevel);
        int saveBonus = GetSaveBonus(ownerLevel);
        /*int (int Bonus, int Cap) reinforcing = GetReinforcing(owner.Level);*/
        
        // Create Description
        string[] descriptionStack = [];
        if (atkBonus > 0)
            descriptionStack = descriptionStack.Append($"You have a +{atkBonus} item bonus to weapon attack rolls").ToArray();
        if (acBonus > 0)
            descriptionStack = descriptionStack.Append($"a +{acBonus} item bonus to AC").ToArray();
        if (saveBonus > 0)
            descriptionStack = descriptionStack.Append($"a +{saveBonus} item bonus to saves").ToArray();
        if (striking > 0)
            descriptionStack = descriptionStack.Append($"your Strikes deal {striking+1} damage dice").ToArray();
        string description = S.ConstructOrList(descriptionStack, "and") + ".";
        
        return new QEffect(
            "Runic Crafter",
            description,
            ExpirationCondition.Never,
            runesmith,
            IllustrationName.None)
        {
            Id = ModData.QEffectIds.RunicCrafter,
            Innate = true,
            // Log of items that were affected, what change was made, and the += adjustment.
            Tag = new List<(Item Item, string Kind, int Difference)>(),
            /*StartOfCombatAfterInitiativeOrderIsSetUp = async qfThis =>
            {
                List<Item> myItems = new List<Item?>([
                        ..qfThis.Owner.AllItems,
                        ..qfThis.Owner.Weapons,
                        qfThis.Owner.BaseArmor,
                    ])
                    .WhereNotNull()
                    .Concat(qfThis.Owner.PersistentCharacterSheet?.InventoryForView.AllItems ?? [])
                    .Distinct()
                    .ToList();
                
                foreach (Item item in myItems)
                    ReapplyRunes(item);
            },*/
            StateCheck = qfThis =>
            {
                if (qfThis.Tag is not List<(Item Item, string Kind, int Difference)> changes)
                    return;

                List<Item> myItems = new List<Item?>([
                        ..qfThis.Owner.AllItems,
                        ..qfThis.Owner.Weapons,
                        qfThis.Owner.BaseArmor,
                    ])
                    .WhereNotNull()
                    //.Concat(qfThis.Owner.PersistentCharacterSheet?.InventoryForView.AllItems ?? [])
                    .Distinct()
                    .ToList();
                
                // Loop through all your items
                foreach (Item item in myItems)
                {
                    if (item.ArmorProperties is not null)
                    {
                        AddChange(qfThis, item, "armor potency", acBonus);
                        AddChange(qfThis, item, "resilient", saveBonus);
                    }
                    
                    // And here's where I'd put my reinforcing logic, IF I HAD ONE!
                    /*if (item.HasTrait(Trait.Shield) && item.Hardness > 0)
                    {
                        
                    }*/

                    if (item.WeaponProperties is not null)
                    {
                        AddChange(qfThis, item, "weapon potency", atkBonus);
                        // +1 to Striking because this is setting the final dice, rather than the bonus
                        AddChange(qfThis, item, "striking", striking+1);
                    }
                }
                
                // Get the list of changed items that are no longer yours
                List<(Item Item, string Kind, int Difference)> lostChanges = changes
                    .Where(change => !myItems.Contains(change.Item))
                    .ToList();

                foreach ((Item Item, string Kind, int Difference) change in lostChanges)
                    RemoveChange(qfThis, change);
                
                /*foreach (Item item in lostChanges
                             .Select(tup => tup.Item)
                             .Distinct()
                             .ToList())
                    ReapplyRunes(item);*/
            },
        };
        
        int GetAttackBonus(int level)
        {
            return level switch
            {
                >= 16 => 3,
                >= 10 => 2,
                >= 2 => 1,
                _ => 0
            };
        }
        int GetDiceBonus(int level)
        {
            return level switch
            {
                >= 19 => 3,
                >= 12 => 2,
                >= 4 => 1,
                _ => 0
            };
        }
        int GetACBonus(int level)
        {
            return level switch
            {
                >= 18 => 3,
                >= 11 => 2,
                >= 5 => 1,
                _ => 0
            };
        }
        int GetSaveBonus(int level)
        {
            return level switch
            {
                >= 20 => 3,
                >= 14 => 2,
                >= 8 => 1,
                _ => 0
            };
        }
        /*(int Bonus, int Cap) GetReinforcing(int level)
        {
            return level switch
            {
                >= 19 => (7, 20),
                >= 16 => (5, 17),
                >= 13 => (5, 15),
                >= 10 => (3, 13),
                >= 7 => (3, 10),
                >= 4 => (3, 9),
                _ => (0, 0)
            };
        }*/

        int? GetValue(Item item, string kind)
        {
            switch (kind)
            {
                case "armor potency":
                    return item.ArmorProperties?.ItemBonus;
                case "resilient":
                    return item.ArmorProperties?.ItemBonusToSavingThrows;
                case "reinforcing":
                    return (item.Hardness > 0 ? item.Hardness : null);
                case "weapon potency":
                    return item.WeaponProperties?.ItemBonus;
                case "striking":
                    return item.WeaponProperties?.DamageDieCount;
                default:
                    throw new Exception($"Change kind \"{kind}\" is not a valid case.");
            }
        }

        void SetValue(Item item, string kind, int setTo)
        {
            switch (kind)
            {
                case "armor potency":
                    item.ArmorProperties?.ItemBonus = setTo;
                    break;
                case "resilient":
                    item.ArmorProperties?.ItemBonusToSavingThrows = setTo;
                    break;
                case "reinforcing" when item.Hardness > 0:
                    item.Hardness = setTo;
                    break;
                case "weapon potency":
                    item.WeaponProperties?.ItemBonus = setTo;
                    break;
                case "striking":
                    item.WeaponProperties?.DamageDieCount = setTo;
                    break;
                default:
                    throw new Exception($"Change kind \"{kind}\" is not a valid case.");
            }
        }

        void AddChange(
            QEffect runicCrafter,
            Item item,
            string kind,
            int setTo)
        {
            // Safely get the change log
            if (runicCrafter.Tag is not List<(Item Item, string Kind, int Difference)> changes)
                return;

            // Get the current value from the kind identifier
            if (GetValue(item, kind) is not { } current
                || current >= setTo)
                return;

            // Get the relative increase from the current to the new value
            int difference = setTo - current;

            // Only make a change if it doesn't already have an entry
            if (changes.Any(change =>
                    change.Item == item
                    && change.Kind == kind))
                return;
            
            // Using the same identifier, modify that value
            SetValue(item, kind, setTo);
            
            // Log the change
            changes.Add((item, kind, difference));
        }

        void RemoveChange(
            QEffect runicCrafter,
            (Item Item, string Kind, int Difference) change)
        {
            // Safely get the change log
            if (runicCrafter.Tag is not List<(Item Item, string Kind, int Difference)> changes)
                return;
            
            // Get the current value from the kind identifier
            if (GetValue(change.Item, change.Kind) is not { } current)
                return;

            // Only reverse the change if it's found in the list
            if (!changes.Remove(change))
                return;
            
            // Return the value to its original value
            SetValue(change.Item, change.Kind, current - change.Difference);
        }

        /*void ReapplyRunes(Item item)
        {
            List<Item> runes = item.Runes.ToList();
            foreach (Item rune in runes)
                RunestoneRules.AddRuneTo(rune, item);
        }*/
    }
}