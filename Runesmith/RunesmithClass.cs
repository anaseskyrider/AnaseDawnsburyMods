using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class RunesmithClass
{
    public static void LoadClass()
    {
        ////////////////////
        // Class Features //
        ////////////////////
        Feat runesmithRepertoire = new RunicRepertoireFeat(
            ModData.FeatNames.RunesmithRepertoire,
            // Ability.Intelligence,
            ModData.Traits.Runesmith,
            2);
        ModManager.AddFeat(runesmithRepertoire);
        
        Feat traceRune = new Feat(
                ModData.FeatNames.TraceRune,
                "Your fingers dance, glowing light leaving behind the image of a rune.",
                "You apply one rune to an adjacent target matching the rune's Usage description. The rune remains until the end of your next turn. If you spend 2 actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.",
                [Trait.Concentrate, Trait.Magical, Trait.Manipulate],
                null)
            .WithPermanentQEffect("You apply one rune to an adjacent target as an action, or to within 30 feet as two actions.", qfFeat =>
            {
                qfFeat.Name += " {icon:Action}–{icon:TwoActions}"; // No WithActionCost method, so update the sheet name to have actions.
                qfFeat.Innate = false;
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    List<Possibility> traceRunePossibilities = [];
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(qfThis.Owner);
                    if (repertoire == null)
                        return null;
                    foreach (Rune rune in repertoire.GetRunesKnown(qfThis.Owner)
                                 .Where(rune => !rune.DrawTechnicalTraits.Contains(ModData.Traits.Etched)))
                    {
                        List<Possibility> specificRunePossibilities = [];
                        
                        // Don't make the 1-action version if you have RuneSinger.
                        bool hasRuneSinger = qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger);
                        if (!hasRuneSinger)
                        {
                            CombatAction? oneActionTraceRune = CommonRuneRules.CreateTraceAction(qfThis.Owner, rune, 1);
                            if (oneActionTraceRune == null)
                                continue;
                            oneActionTraceRune.ContextMenuName = "{icon:Action} " + oneActionTraceRune.Name;
                            (oneActionTraceRune.Target as CreatureTarget)!
                                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                    attacker.FindQEffect(ModData.QEffectIds.DrawnInRed)?.Tag == defender
                                        ? Usability.NotUsableOnThisCreature("use Drawn in Red")
                                        : Usability.Usable);
                            ActionPossibility traceRunePossibility1 = new ActionPossibility(oneActionTraceRune)
                                { Caption = "Touch", Illustration = IllustrationName.Action };
                            specificRunePossibilities.Add(traceRunePossibility1);
                        }
                        
                        CombatAction? twoActionTraceRune = CommonRuneRules.CreateTraceAction(qfThis.Owner, rune, 2);
                        if (twoActionTraceRune == null)
                            continue;
                        if (!hasRuneSinger)
                        {
                            // Declutter your options by removing the ranged option while in melee.
                            (twoActionTraceRune.Target as CreatureTarget)!
                                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                    attacker.DistanceTo(defender) <= 1
                                        ? Usability.NotUsableOnThisCreature("use the 1-action version")
                                        : Usability.Usable);
                        }
                        twoActionTraceRune.ContextMenuName = RulesBlock.GetIconTextFromNumberOfActions(twoActionTraceRune.ActionCost) + " " + twoActionTraceRune.Name;
                        (twoActionTraceRune.Target as CreatureTarget)!
                            .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                attacker.FindQEffect(ModData.QEffectIds.DrawnInRed)?.Tag == defender
                                    ? Usability.NotUsableOnThisCreature("use Drawn in Red")
                                    : Usability.Usable);
                        ActionPossibility traceRunePossibility2 = new ActionPossibility(twoActionTraceRune)
                        {
                            Caption = "30 feet",
                            Illustration = hasRuneSinger
                                ? new SideBySideIllustration(IllustrationName.Action, ModData.Illustrations.RuneSinger)
                                : IllustrationName.TwoActions
                        };
                        specificRunePossibilities.Add(traceRunePossibility2);

                        SubmenuPossibility specificRuneMenu = new SubmenuPossibility(
                            rune.Illustration,
                            rune.Name,
                            PossibilitySize.Half)
                        {
                            SpellIfAny = CommonRuneRules.CreateTraceAction(qfThis.Owner, rune, -3), // variable action trace rune
                            Subsections =
                            {
                                new PossibilitySection(rune.Name) // rune.Name is how features like Drawn In Red find these sections.
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
                        SpellIfAny = new CombatAction(qfThis.Owner, ModData.Illustrations.TraceRune, "Trace Rune", [Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModData.Traits.Runesmith], "{i}Your fingers dance, glowing light leaving behind the image of a rune.{/i}\n\n{b}Requirements{b} You have a hand free.\n\nYou apply one rune to an adjacent target matching the rune's Usage description. The rune remains until the end of your next turn. If you spend {icon:TwoActions} two actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.", Target.Self()).WithActionCost(-3), // This doesn't DO anything, it's just to provide description to the menu.
                        Subsections = { new PossibilitySection("Trace Rune")
                        {
                            Possibilities = traceRunePossibilities,
                        }},
                        PossibilityGroup = ModData.PossibilityGroups.DrawingRunes,
                    };
                    return traceRuneMenu;
                };
            });
        ModManager.AddFeat(traceRune);

        Feat invokeRune = new Feat(
                ModData.FeatNames.InvokeRune,
                "",
                "You utter the name of one or more of your runes within 30 feet. The rune blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed.\n\nYou can invoke any number of runes with a single Invoke Rune action, but creatures that would be affected by multiple copies of the same specific rune are {Red}affected only once{/Red}, as normal for duplicate effects.",
                [ModData.Traits.Invocation, Trait.Magical],
                null)
            .WithPermanentQEffect("You invoke any number of runes within 30 feet.", qfFeat =>
            {
                qfFeat.Name += " {icon:Action}";
                qfFeat.Innate = false;
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction invokeRuneAction = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.InvokeRune,
                            "Invoke Rune",
                            [ModData.Traits.Invocation, Trait.Magical, ModData.Traits.Runesmith, Trait.Spell, Trait.Basic, Trait.DoNotShowOverheadOfActionName, Trait.UnaffectedByConcealment],
                            "You utter the name of one or more of your runes within 30 feet. The rune blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed.\n\nYou can invoke any number of runes with a single Invoke Rune action, but creatures that would be affected by multiple copies of the same specific rune are {Red}affected only once{/Red}, as normal for duplicate effects.",
                            Target.Self()
                                .WithAdditionalRestriction(caster =>
                                {
                                    // PETR: Can't use if Silenced. Deafened does not DC5-check spellcasting in Dawnsbury, so it does not here.
                                    //bool cannotSpeak = caster.HasEffect(QEffectId.) != null;
                                    //if (cannotSpeak)
                                    //  return "Cannot speak in a strong voice";

                                    foreach (Creature cr in caster.Battle.AllCreatures)
                                    {
                                        if (caster.DistanceTo(cr) <= 6 && // Make sure creatures are in range.
                                            cr.QEffects.FirstOrDefault( // Find a Qf-
                                                qfToFind =>
                                                    qfToFind is DrawnRune dr // -that is a DrawnRune,
                                                    && dr.Source == caster // that is created by us,
                                                    && dr.Traits.Contains(ModData.Traits.Rune) // with the rune trait,
                                                    && !dr.Traits.Contains(ModData.Traits
                                                        .Invocation) // but not the invocation trait.
                                                    && !dr.Disabled
                                            ) != null
                                           )
                                        {
                                            return null;
                                        }
                                    }

                                    return "No rune-bearers within range";
                                }))
                        .WithActionCost(1)
                        .WithEffectOnEachTarget(async (thisAction, self, _,_) =>
                        {
                            // Number of runes on the field.
                            int numberOfRunes = GetRunesInRange(self);

                            // For each valid rune in play, attempt to take an invoke action, up to all our runes.
                            int whileProtection = 0;
                            while (numberOfRunes > 0 && whileProtection < 100)
                            {
                                await self.Battle.GameLoop.StateCheck(); // Idk why but they all do this so keep it.
                                numberOfRunes = GetRunesInRange(self); // Regenerate the list of creatures with runes left.
                                if (!await CommonRuneRules.PickARuneToInvokeOnTarget(
                                        thisAction, self, null, null,
                                        whileProtection == 0,
                                        " Confirm no additional runes ",
                                        $" You should avoid invoking the same rune on the same creature more than once. (Runes: {numberOfRunes})"))
                                    return; // Task handles `RevertRequested = true;`.
                                whileProtection++;
                            }
                            return;

                            int GetRunesInRange(Creature caster)
                            {
                                return caster.Battle.AllCreatures
                                    .Where(cr => caster.DistanceTo(cr) <= 6) // Must be within range.
                                    .Sum(cr => DrawnRune.GetDrawnRunes(caster, cr).Count(dr => !dr.Disabled));
                            }
                        });
                    CommonRuneRules.WithImmediatelyRemovesImmunity(invokeRuneAction); 
                    return new ActionPossibility(invokeRuneAction)
                        .WithPossibilityGroup(ModData.PossibilityGroups.InvokingRunes);
                };
            });
        ModManager.AddFeat(invokeRune);
        
        // BUG: Action triggers before animal companions spawn.
        Feat etchRune = new Feat(
                ModData.FeatNames.EtchRune,
                "An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.",
                "At the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to 2 runes, and you can etch an additional rune at levels 5, 9, 13, and 17.",
                [], // No traits, we don't want anything inside the encounter to try to interact with this action at the start of combat.
                null)
            /*.WithOnSheet(values =>
            {
                RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values);
                if (repertoire == null)
                    return;
                for (int i = 0; i < repertoire.EtchLimit; i++)
                {
                    //values.AddSelectionOption(new SelectionOption );
                    //SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL
                }
            })*/
            .WithPermanentQEffect("You apply runes to your allies at the start of combat which last until the end of combat or consumed.", qfFeat =>
            {
                qfFeat.Innate = false;
                qfFeat.StartOfCombat = async qfThis =>
                {
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(qfThis.Owner);
                    if (repertoire == null)
                        return;
                    
                    // Runic Tattoo first
                    if (qfThis.Owner.HasFeat(ModData.FeatNames.RunicTattoo))
                    {
                        QEffect? runicTattooFeat = qfFeat.Owner.QEffects.FirstOrDefault(qf =>
                            qf.Name is { } name && name.Contains("Runic Tattoo"));
                        if (runicTattooFeat != null)
                            await runicTattooFeat.StartOfCombat!.Invoke(runicTattooFeat);
                    }
                    
                    List<Rune> runesKnown = repertoire.GetRunesKnown(qfFeat.Owner);
                    int etchLimit = repertoire.GetEtchLimit(qfThis.Owner.Level);
                    
                    qfThis.Owner.Overhead(
                        "Etching Runes",
                        Color.Black,
                        $"{qfThis.Owner.Name} begins {{b}}Etching Runes{{/b}}.",
                        "Etch Rune",
                        $"{{i}}An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.{{/i}}\n\nAt the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to {etchLimit} runes.",
                        new Traits([Trait.Manipulate, Trait.DoesNotProvoke, ModData.Traits.Runesmith]));

                    for (int i = 0; i < etchLimit; i++)
                    {
                        await qfThis.Owner.Battle.GameLoop.StateCheck();
                        
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

                        await chosenOption.Action();
                    }

                    qfThis.Tag = true; // True means the runes have been etched.
                };
            });
        ModManager.AddFeat(etchRune);

        Feat runicCrafter = new Feat(
                ModData.FeatNames.RunicCrafter,
                "Your study of secret runes leaves you well practiced in crafting their more common cousins.",
                "Your equipment gains the effects of the highest level fundamental armor and weapon runes for your level. This does not count as having runes for the purposes of other rules (you must still have potency runes to apply property runes).",
                [],
                null)
            .WithOnCreature(AddRunicCrafterEffect);
        ModManager.AddFeat(runicCrafter);

        Feat assuredRunicCrafter = new Feat(
                ModData.FeatNames.AssuredRunicCrafter,
                "You’re so used to tracing and etching runes in the field that when given the peace and quiet of a proper workshop, it’s hard for things to go astray.",
                "{b}Precombat Preparations{/b}You can select one ally to gain the benefits of your " +
                ModData.Tooltips.FeatureRunicCrafter("Runic Crafter") + " feature.",
                [],
                null)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "RunesmithPlaytest.AssuredRunicCrafter",
                        "Assured Runic Crafter",
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                        ft => ft.Tag is "Assured Runic Crafter")
                    .WithIsOptional());
            });
        ModManager.AddFeat(assuredRunicCrafter);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            Feat assuredChoice = new Feat(
                    ModManager.RegisterFeatName(ModData.FeatNames.AssuredRunicCrafterChoice + (i + 1),
                        "Player Character " + (i + 1)),
                    null,
                    "",
                    [],
                    null)
                .WithNameCreator(_ =>
                    $"Grant Runic Crafter to {GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"}")
                .WithRulesTextCreator(_ =>
                    $"{GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"} will gain the benefits of your {ModData.Tooltips.FeatureRunicCrafter("Runic Crafter")} feature at the start of combat.")
                .WithIllustrationCreator(_ =>
                    GetCharacterSheetFromPartyMember(index)?.Illustration ?? ModData.Illustrations.TraceRune)
                .WithTag("Assured Runic Crafter")
                .WithPermanentQEffect(
                    "An ally chosen before the start of combat gains the effects of your Runic Crafter feature.",
                    qfFeat =>
                    {
                        qfFeat.StartOfCombat = async qfThis =>
                        {
                            if (GetCharacterSheetFromPartyMember(index) is {} hero
                                && qfThis.Owner.Battle.AllCreatures.FirstOrDefault(cr2 =>
                                    cr2 != qfThis.Owner &&
                                    cr2.PersistentCharacterSheet == hero) is { } chosenCreature)
                            {
                                QEffect crafter = AddRunicCrafterEffect(chosenCreature);
                                crafter.Name = "Assured Runic Crafter";
                                crafter.DoNotShowUpOverhead = false;
                            }
                        };
                    })
                .WithPrerequisite(_ => // Can't select another Runesmith
                    GetCharacterSheetFromPartyMember(index)?.Class?.ClassTrait != ModData.Traits.Runesmith,
                    "Can't select another Runesmith")
                .WithPrerequisite(values => // Can't select yourself
                    GetCharacterSheetFromPartyMember(index) != values.Sheet,
                    "Can't select yourself");
            ModManager.AddFeat(assuredChoice);
        }
        
        /////////////////////
        // Runesmith Class //
        /////////////////////
        Feat runesmithClassFeat = new ClassSelectionFeat(
                ModData.FeatNames.RunesmithClass,
                "At the heart of all communication is the word, and at the heart of all magic is the rune. Equal parts scholar and artist, you devote yourself to the study of these mystic symbols, learning to carve, etch, brand, and paint the building blocks of magic to channel powers greater than yourself.",
                ModData.Traits.Runesmith,
                new EnforcedAbilityBoost(Ability.Intelligence),
                8,
                [Trait.Perception, Trait.Reflex, Trait.Unarmed, Trait.Simple, Trait.Martial, Trait.UnarmoredDefense, Trait.LightArmor, Trait.MediumArmor, Trait.Crafting],
                [Trait.Fortitude, Trait.Will],
                2,
                "{b}1. Runic Repertoire.{/b} A runesmith doesn't cast spells, but they can use "+ModTooltips.TraitRune("runesmith runes")+". You start with 4 runes of 1st level, and learn an additional rune every 2nd level. Your runes are the same level you are, regardless of when you learn them {i}(some runes increase in power at higher levels, as listed in their Level entry){/i}. Runes use your class DC, which is based on Intelligence. If a rune lists a tradition trait (such as Arcane), then it belongs to that tradition; it otherwise belongs to any tradition "+ModTooltips.FeatureTraditionSkill("whose skill")+" you're trained in for the purposes of abilities which require a rune of a specific tradition." +
                "\r\n\r\n{b}2. Applying Runes.{/b} You can apply runes in one of two ways: {i}tracing{/i} the rune with the "+ModTooltips.ActionTraceRune("Trace Rune")+" action, or by {i}etching{/i} the rune at the start of combat with the "+ModTooltips.ActionEtchRune("Etch Rune")+" activity." +
                "\r\n\r\n{b}3. Invoking Runes.{/b} You can activate one or more runes' invocation feature with the "+ModTooltips.ActionInvokeRune("Invoke Rune")+" action." +
                "\r\n\r\n{b}4. Runesmith feat.{/b}" +
                "\r\n\r\n{b}5. Shield block {icon:Reaction}.{/b} You can use your shield to reduce damage you take from attacks" +
                "\r\n\r\n{b}At higher levels:{/b}" +
                "\r\n{b}Level 2:{/b} Runesmith feat, "+ModTooltips.FeatureRunicCrafter("runic crafter") +
                "\r\n{b}Level 3:{/b} General feat, skill increase, additional rune known ({i}level 1{/i})" +
                "\r\n{b}Level 4:{/b} Runesmith feat" +
                "\r\n{b}Level 5:{/b} Attribute boosts, ancestry feat, skill increase, expert in simple and martial weapons and in unarmed attacks, additional rune known ({i}level 1{/i}), etch limit increase ({i}3 runes{/i})" +
                "\r\n{b}Level 6:{/b} Runesmith feat" +
                "\r\n{b}Level 7:{/b} General feat, skill increase, expert in runesmith DC, expert in Reflex saves, "+ModTooltips.FeatureRunicOptimization("runic optimization")+", additional rune known ({i}level 1{/i})" +
                "\r\n{b}Level 8:{/b} Runesmith feat"
                    + (Constants.CharacterLevelCap > 8 ? "\r\n\r\n" + new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString + " (INCOMPLETE CONTENT BEYOND THIS POINT: level 9 & 17 runes, level 10+ class feats)\r\n\r\n{b}Level 9:{/b} Ancestry feat, "+ModTooltips.FeatureAssuredRunicCrafter("assured runic crafter")+", skill increase, additional rune known ({i}level 9{/i}), etch limit increase ({i}4 runes{/i})" : null)
                    + (Constants.CharacterLevelCap > 9 ? "\r\n{b}Level 10:{/b} Attribute boosts, runesmith feat" : null)
                    + (Constants.CharacterLevelCap > 10 ? "\r\n{b}Level 11:{/b} General feat, skill increase, " + ModTooltips.FeatureSmithsEndurance("smith's endurance") + ", additional rune known ({i}level 9{/i})" : null)
                    + (Constants.CharacterLevelCap > 11 ? "\r\n{b}Level 12:{/b} Runesmith feat" : null)
                    + (Constants.CharacterLevelCap > 12 ? "\r\n{b}Level 13:{/b} Ancestry feat, expert in light and medium armor and in unarmored defense, expert in Perception, skill increase, master in simple and martial weapons and in unarmed attacks, additional rune known ({i}level 9{/i}), etch limit increase ({i}5 runes{/i})" : null)
                    + (Constants.CharacterLevelCap > 13 ? "\r\n{b}Level 14:{/b} Runesmith feat" : null)
                    + (Constants.CharacterLevelCap > 14 ? "\r\n{b}Level 15:{/b} Attribute boosts, general feat, " + ModTooltips.FeatureRunicOptimization("greater runic optimization") + ", master in runesmith DC, skill increase, additional rune known ({i}level 9{/i})": null)
                    + (Constants.CharacterLevelCap > 15 ? "\r\n{b}Level 16:{/b} Runesmith feat": null)
                    + (Constants.CharacterLevelCap > 16 ? "\r\n{b}Level 17:{/b} Ancestry feat, skill increase, additional rune known ({i}level 17{/i}), etch limit increase ({i}6 runes{/i})" : null)
                    + (Constants.CharacterLevelCap > 17 ? "\r\n{b}Level 18:{/b} Runesmith feat" : null)
                    + (Constants.CharacterLevelCap > 18 ? "\r\n{b}Level 19:{/b} General feat, legendary in runesmith DC, master in light and medium armor and in unarmored defense, skill increase, additional rune known ({i}level 17{/i})" : null)
                    + (Constants.CharacterLevelCap > 19 ? "\r\n{b}Level 20:{/b} Attribute boosts, runesmith feat" : null),
                null)
            .WithOnSheet(values =>
            {
                #region Level 1 Features
                // Bonus skills
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "runesmithSkills",
                        "Runesmith skill",
                        1,
                        ft =>
                            ft.FeatName is FeatName.Arcana or FeatName.Nature or FeatName.Occultism or FeatName.Religion)
                    .WithIsOptional());
                // Runic repertoire
                values.GrantFeat(ModData.FeatNames.RunesmithRepertoire);
                values.AddSelectionOption(new MultipleFeatSelectionOption(
                        "initialRunes",
                        "Initial level 1 runes",
                        1,
                        ft =>
                            ft is RuneFeat { Rune.BaseLevel: <= 1 }, 4)
                    .WithIsOptional());
                for (int i=3; i<=7; i+=2) // Gain a new Rune every other level.
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("rune"+i, "Level 1 rune", i, ft => ft is RuneFeat { Rune.BaseLevel: <= 8 }).WithIsOptional());
                }
                for (int i=9; i<=15; i+=2) // Gain a new Rune every other level.
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("rune"+i, "Level 9 rune", i, ft => ft is RuneFeat { Rune.BaseLevel: <= 16 }).WithIsOptional());
                }
                for (int i=17; i<=19; i+=2) // Gain a new Rune every other level.
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("rune"+i, "Level 17 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                // Class features
                values.GrantFeat(traceRune.FeatName);
                values.GrantFeat(invokeRune.FeatName);
                values.GrantFeat(etchRune.FeatName);
                // Bonus feats
                values.GrantFeat(FeatName.ShieldBlock);
                // Class feat
                values.AddClassFeatOption("RunesmithFeat1", ModData.Traits.Runesmith, 1);
                #endregion
                
                #region Higher Levels
                values.AddAtLevel(2, values2 =>
                {
                    values2.GrantFeat(runicCrafter.FeatName);
                });
                values.AddAtLevel(5, values2 =>
                {
                    values2.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                    values2.SetProficiency(Trait.Simple, Proficiency.Expert);
                    values2.SetProficiency(Trait.Martial, Proficiency.Expert);
                    values2.SetProficiency(ModData.Traits.ArtisansHammer, Proficiency.Expert);
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values2)!;
                    repertoire.IncreaseEtchLimit(5, 1);
                });
                values.AddAtLevel(7, values2 =>
                {
                    values2.SetProficiency(Trait.Reflex, Proficiency.Expert);
                    values2.SetProficiency(ModData.Traits.Runesmith, Proficiency.Expert);
                });
                #endregion
                
                #region Future Content
                values.AddAtLevel(9, values2 =>
                {
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values2)!;
                    repertoire.IncreaseEtchLimit(9, 1);
                    values2.GrantFeat(assuredRunicCrafter.FeatName);
                });
                #endregion

                #region Post-Game Content
                values.AddAtLevel(11, values2 =>
                {
                    values2.SetProficiency(Trait.Fortitude, Proficiency.Master);
                    // See WithOnCreature for the success->critical effect.
                });
                values.AddAtLevel(13, values2 =>
                {
                    values2.SetProficiency(Trait.UnarmoredDefense, Proficiency.Expert);
                    values2.SetProficiency(Trait.LightArmor, Proficiency.Expert);
                    values2.SetProficiency(Trait.MediumArmor, Proficiency.Expert);
                    
                    values2.SetProficiency(Trait.Perception, Proficiency.Expert);
                    
                    values2.SetProficiency(Trait.Unarmed, Proficiency.Master);
                    values2.SetProficiency(Trait.Simple, Proficiency.Master);
                    values2.SetProficiency(Trait.Martial, Proficiency.Master);
                    values2.SetProficiency(ModData.Traits.ArtisansHammer, Proficiency.Expert);
                    
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values2)!;
                    repertoire.IncreaseEtchLimit(13, 1);
                });
                values.AddAtLevel(15, values2 =>
                {
                    values2.SetProficiency(ModData.Traits.Runesmith, Proficiency.Master);
                });
                values.AddAtLevel(17, values2 =>
                {
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values2)!;
                    repertoire.IncreaseEtchLimit(17, 1);
                });
                values.AddAtLevel(19, values2 =>
                {
                    values2.SetProficiency(ModData.Traits.Runesmith, Proficiency.Legendary);
                    
                    values2.SetProficiency(Trait.UnarmoredDefense, Proficiency.Master);
                    values2.SetProficiency(Trait.LightArmor, Proficiency.Master);
                    values2.SetProficiency(Trait.MediumArmor, Proficiency.Master);
                });
                #endregion
            })
            .WithOnCreature(cr =>
            {
                #region Higher Levels
                if (cr.Level >= 7)
                {
                    QEffect optimize = RunicOptimization(cr.Level >= 15);
                    cr.AddQEffect(optimize);
                }
                #endregion

                #region Post-Game Content
                if (cr.Level >= 11)
                {
                    cr.AddQEffect(new QEffect("Smith's Endurance", "When you roll a success on a Fortitude save, you get a critical success instead.")
                    {
                        AdjustSavingThrowCheckResult = (effect, defense, action, checkResult) =>
                            defense != Defense.Fortitude || checkResult != CheckResult.Success
                                ? checkResult
                                : CheckResult.CriticalSuccess
                    });
                    // See WithOnSheet for the Master proficiency increase.
                }
                #endregion
            });
        runesmithClassFeat.RulesText = runesmithClassFeat.RulesText
            .Replace("Key ability", "Key attribute")
            .Replace("trained in Crafting", "trained in Crafting; as well as your choice of Arcana, Nature, Occultism, or Religion;");
        ModManager.AddFeat(runesmithClassFeat);
    }
    
    public static QEffect RunicOptimization(bool greater)
    {
        int basicIncrease = greater ? 4 : 2;
        const int step = 1;
        string specializationName = greater ? "Greater runic optimization" : "Runic optimization";
        string description = $"You deal an additional {basicIncrease} damage with weapons and unarmed attacks in which you have expert proficiency. This damage increases to {basicIncrease + step} if you're a master, and {basicIncrease + 2 * step} if you're legendary.";
        return new QEffect(specializationName, description)
        {
            BonusToDamage = (qfThis, action, target) =>
            {
                if (action.Item == null)
                    return null;
                Proficiency proficiency = action.Owner.Proficiencies.Get(action.Item.Traits);
                int bonusAmount = proficiency switch
                {
                    Proficiency.Expert => basicIncrease,
                    Proficiency.Master => basicIncrease + step,
                    Proficiency.Legendary => basicIncrease + 2 * step,
                    _ => 0
                };
                return proficiency >= Proficiency.Expert
                    ? new Bonus(bonusAmount, BonusType.Untyped, specializationName)
                    : null;
            }
        };
    }
    
    public static QEffect AddRunicCrafterEffect(Creature owner)
    {
        // Uses code from ABP //
        QEffect RCFX = new QEffect("Runic Crafter", "INCOMPLETE", ExpirationCondition.Never, owner, IllustrationName.None)
        {
            Innate = true,
            BonusToAttackRolls = (qfThis, combatAction, defender) =>
            {
                int attackPotency = GetAttackPotency(qfThis.Owner.Level);
                if (combatAction.HasTrait(Trait.Attack) && combatAction.Item != null &&
                    (combatAction.Item.HasTrait(Trait.Weapon) || combatAction.Item.HasTrait(Trait.Unarmed)) &&
                    attackPotency > 0)
                {
                    return new Bonus(attackPotency, BonusType.Item, "Runic Crafter");
                }

                return null;
            },
            BonusToDefenses = (qfThis, action, defense) =>
            {
                int defensePotency = GetACPotency(qfThis.Owner.Level);
                int savingThrowPotency = GetResilience(qfThis.Owner.Level);
                
                // AC Potency
                if (defense == Defense.AC && defensePotency > 0)
                {
                    var itemBonus = qfThis.Owner.Armor.Item?.ArmorProperties?.ItemBonus ?? 0;
                    if (defensePotency > itemBonus)
                    {
                        return new Bonus(defensePotency - itemBonus, BonusType.Untyped, "Runic Crafter");
                    }
                } // Saving Throw Potency
                else if (defense.IsSavingThrow() && savingThrowPotency > 0)
                {
                    return new Bonus(savingThrowPotency, BonusType.Item, "Runic Crafter");
                }

                return null;
            },
        };

        // Update Description
        int attackPotency = GetAttackPotency(owner.Level);
        int defensePotency = GetACPotency(owner.Level);
        int savingThrowPotency = GetResilience(owner.Level);
        string[] descriptionStack = [];
        if (attackPotency > 0)
            descriptionStack = descriptionStack.Append($"You have a +{attackPotency} item bonus to weapon attack rolls").ToArray();
        if (owner.Level >= 4)
            descriptionStack = descriptionStack.Append("your Strikes deal two damage dice instead of one").ToArray();
        if (defensePotency > 0)
            descriptionStack = descriptionStack.Append($"a +{defensePotency} item bonus to AC").ToArray();
        if (savingThrowPotency > 0)
            descriptionStack = descriptionStack.Append($"a +{savingThrowPotency} item bonus to all saving throws").ToArray();
        string description = "";
        switch (descriptionStack.Length)
        {
            case 0:
                break;
            case 1:
                description = descriptionStack.First();
                break;
            case 2:
                description = string.Join(" and ", descriptionStack);
                break;
            default:
                descriptionStack[^1] = descriptionStack[^1].Insert(0, "and ");
                description = string.Join(", ", descriptionStack);
                break;
        }
        description += ".";
        
        RCFX.Description = description;
        
        owner.AddQEffect(RCFX);

        // Add Striking
        if (owner.Level >= 4)
            AddCumulativeStriking(owner, 1);
        if (owner.Level >= 12)
            AddCumulativeStriking(owner, 2);
        if (owner.Level >= 19)
            AddCumulativeStriking(owner, 3);
        
        return RCFX;
        
        // Locals
        QEffect AddCumulativeStriking(Creature owner2, int bonusDice)
        {
            QEffect striking = new QEffect("Runic Crafter (Striking)", "INCOMPLETE", ExpirationCondition.Never,
                owner2, IllustrationName.None)
            {
                IncreaseItemDamageDieCount = (qfSelf, item) =>
                    item.WeaponProperties?.DamageDieCount <= bonusDice,
            };
            owner2.AddQEffect(striking);
            return striking;
        }
        int GetAttackPotency(int level)
        {
            switch (level)
            {
                case <= 1:
                    return 0;
                case <= 9:
                    return 1;
                case <= 15:
                    return 2;
                default:
                    return 3;
            }
        }
        int GetACPotency(int level)
        {
            switch (level)
            {
                case <= 4:
                    return 0;
                case <= 10:
                    return 1;
                case <= 17:
                    return 2;
                default:
                    return 3;
            }
        }
        int GetResilience(int level)
        {
            switch (level)
            {
                case <= 7:
                    return 0;
                case <= 13:
                    return 1;
                case <= 19:
                    return 2;
                default:
                    return 3;
            }
        }
    }

    /// <summary>Returns whether the Creature has a hand free for the purposes of tracing runes.</summary>
    public static bool IsRunesmithHandFree(Creature runesmith)
    {
        return runesmith.HasFreeHand
               || runesmith.HeldItems.Any(item => item.HasTrait(ModData.Traits.CountsAsRunesmithFreeHand))
               || runesmith.HasEffect(ModData.QEffectIds.RuneSinger);
    }

    /// <summary>
    /// Asynchronously gets a user selected tile that is closer to an enemy
    /// </summary>
    /// <param name="self">The creature being used to compare distance.</param>
    /// <param name="fromStart">(nullable) Tile to measure distances from.</param>
    /// <param name="enemy">(nullable) The creature, if any, to get closer to. Overrides checking if the creature is an enemy of self.</param>
    /// <param name="messageString">The user prompt message.</param>
    /// <returns>The selected tile or null</returns>
    public static async Task<bool> StrideCloserToEnemyAsync(Creature self, Tile? fromStart, Creature? enemy, string messageString)
    {
        self.Actions.NextStrideIsFree = true;
        self.RegeneratePossibilities();
        
        if (self.Speed <= 0)
        {
            self.Actions.NextStrideIsFree = false;
            return true;
        }
        
        // Determines the starting tile, all enemy tiles and initatlizes the options list
        Tile startingTile = fromStart ?? self.Occupies;
        List<Tile> enemyTiles;
        if (enemy != null)
        {
            enemyTiles = self.Battle.AllCreatures
                .Where(creature => self != creature && creature == enemy)
                .Select(creature => creature.Occupies)
                .ToList();
        }
        else
        {
            enemyTiles = self.Battle.AllCreatures
                .Where(creature => self != creature && !self.FriendOf(creature))
                .Select(creature => creature.Occupies)
                .ToList();
        }
        
        List<Option> options = [];
        
        IList<Tile> floodFill = Pathfinding.Floodfill(self, self.Battle, new PathfindingDescription()
        {
            Squares = self.Speed,
            Style = {
                PermitsStep = false,
            }
        });

        // Cycles through all map tiles and determines if the tile is closer to an enemy and if the user can actually reach the tile
        foreach (Tile tile in floodFill)
        {
            if (!tile.IsFree || tile == self.Occupies || !IsTileCloserToAnyOfTheseTiles(startingTile, tile, enemyTiles))
                continue;

            CombatAction? moveAction = self.Possibilities.CreateActions(true)
                .FirstOrDefault(pw => pw.Action.ActionId == ActionId.Stride) as CombatAction;
            moveAction?.WithActionCost(0);
            
            if (moveAction == null || !(bool)moveAction.Target.CanBeginToUse(self))
                continue;
            
            Option option = moveAction.CreateUseOptionOn(tile).WithIllustration(moveAction.Illustration);
            options.Add(option);
        }
        
        // Add a Cancel Option
        options.Add(new CancelOption(true));
        
        Option? selectedOption = null;
        if (options.Count == 1)
            selectedOption = options[0];
        else if (options.Count > 0)
        {
            if (self.HasEffect(QEffectId.AiDoNotStrideAfterEscape))
            {
                Option? option = options.FirstOrDefault( opt => opt is PassViaButtonOption);
                if (option != null)
                {
                    selectedOption = option;
                    goto label_34;
                }
            }
            selectedOption = (await self.Battle.SendRequest(
                new AdvancedRequest(self, messageString, options)
                {
                    IsMainTurn = false,
                    IsStandardMovementRequest = true,
                    TopBarIcon = (Illustration)IllustrationName.WarpStep,
                    TopBarText = messageString,
                })).ChosenOption;
        }
        label_34:
        if (selectedOption != null)
        {
            await selectedOption.Action();
            self.Battle.MovementConfirmer = null;
            if (selectedOption is CancelOption or PassViaButtonOption)
            {
                self.Actions.NextStrideIsFree = false;
                return false;
            }
            selectedOption = null;
        }
        else
        {
            self.Actions.NextStrideIsFree = false;
            return true;
        }

        self.Actions.NextStrideIsFree = false;
        return true;
    }
    
    /// <summary>
    /// Determines if the potentially closer tile is in fact closer to any of the tiles to check than the original tile
    /// </summary>
    /// <param name="originalTile">The original Tile</param>
    /// <param name="potentialCloserTile">The potentially closer tile</param>
    /// <param name="tilesToCheck">The list of possible tiles to check</param>
    /// <returns>True if the potential closer tiles if closer to any of the tiles to check and False otherwise</returns>
    public static bool IsTileCloserToAnyOfTheseTiles(Tile originalTile, Tile potentialCloserTile, List<Tile> tilesToCheck)
    {
        // Determines if the potentially closer tile is in fact closer to any of the tiles to check than the original tile
        foreach (Tile tileToCheck in tilesToCheck)
        {
            if (potentialCloserTile.DistanceTo(tileToCheck) < originalTile.DistanceTo(tileToCheck))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The PULLER moves a TARGET closer to it by a NUMBER OF SQUARES.</summary>
    public static async Task PullCreatureByDistance(Creature puller, Creature target, int squareCount)
    {
        if (target.WeaknessAndResistance.ImmunityToForcedMovement)
        {
            target.Overhead("{i}immune{/i}", Color.White, target + " is immune to forced movement and can't be pulled.");
            return;
        }
        
        Point finalPoint = new Point(
            target.Occupies.X,
            target.Occupies.Y);
        Point pullerPoint = new Point(
            puller.Occupies.X,
            puller.Occupies.Y);
        Point towardPullerInit = new Point(
            Math.Sign(puller.Occupies.X - target.Occupies.X),
            Math.Sign(puller.Occupies.Y - target.Occupies.Y));
        
        int maxDistance;
        if (towardPullerInit.X != 0 && towardPullerInit.Y != 0)
        {
            switch (squareCount)
            {
                case 1:
                    maxDistance = 1;
                    break;
                case 2:
                case 3:
                    maxDistance = 2;
                    break;
                case 4:
                    maxDistance = 3;
                    break;
                case 5:
                case 6:
                    maxDistance = 4;
                    break;
                case 7:
                    maxDistance = 5;
                    break;
                case 8:
                case 9:
                    maxDistance = 6;
                    break;
                default:
                    maxDistance = squareCount / 2 + 2;
                    break;
            }
        }
        else
            maxDistance = squareCount;

        int countedDistance;
        for (countedDistance = 1; countedDistance <= maxDistance; ++countedDistance)
        {
            Point towardPuller = new Point(
                Math.Sign(pullerPoint.X - finalPoint.X),
                Math.Sign(pullerPoint.Y - finalPoint.Y));
            Point towardX = new Point(
                Math.Sign(pullerPoint.X - finalPoint.X), 
                0);
            Point towardY = new Point(
                0,
                Math.Sign(pullerPoint.Y - finalPoint.Y));
            Point lastPoint = finalPoint;
            Point[] pointArray = [towardPuller, towardX, towardY];
            foreach (Point pointInArray in pointArray)
            {
                Point nextPoint = new Point(
                    finalPoint.X + pointInArray.X,
                    finalPoint.Y + pointInArray.Y);
                if (nextPoint == finalPoint)
                    continue;
                Tile? tile = puller.Battle.Map.GetTile(nextPoint.X, nextPoint.Y);
                if (tile != null && tile.IsGenuinelyFreeTo(target))
                {
                    finalPoint = nextPoint;
                    break;
                }
            }
            if (lastPoint == finalPoint)
                break;
        }
        int distanceToMove = countedDistance - 1;
        if (distanceToMove <= 0)
            return;
        await target.MoveTo(puller.Battle.Map.GetTile(finalPoint.X, finalPoint.Y)!, null, new MovementStyle()
        {
            Shifting = true,
            ShortestPath = true,
            MaximumSquares = 100,
            ForcedMovement = true
        });
    }

    /// <summary>If a character sheet is available at the execution time of this function, it will return a character sheet of a party member either during campaign play or in free encounter play.</summary>
    /// <param name="index">The 0th-indexed party member.</param>
    public static CharacterSheet? GetCharacterSheetFromPartyMember(int index)
    {
        CharacterSheet? hero = null;
        if (CampaignState.Instance is { } campaign)
            hero = campaign.Heroes[index].CharacterSheet;
        else if (CharacterLibrary.Instance is { } library)
            hero = library.SelectedRandomEncounterParty[index];
        return hero;
    }
}

// Kept just in case.
/*Option runeOption = Option.ChooseCreature( // Add an option with this creature for its rune.
    thisRune.Name,
    crWithRune.Key,
    async () =>
    {
        await thisRune.InvocationBehavior.Invoke(action, thisRune, self,
            crWithRune.Key, runeQf);
        Sfxs.Play(SfxName.DazzlingFlash);
    })
    .WithIllustration(thisRune.Illustration);
options.Add(runeOption);*/

/* QEffect Properties to utilize
 * .Key     for anti-stacking behavior
 * .AppliedThisStateCheck
 * .Hidden
 * .HideFromPortrait
 * .Tag
 * .UsedThisTurn
 * .Value
 * .Source
 * .SourceAction
 * .Owner
 */