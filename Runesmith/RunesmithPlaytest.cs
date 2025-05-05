using System.Diagnostics;
using System.Text.RegularExpressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes.Multiclass;
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
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunesmithPlaytest
{
    public static Feat? RunesmithClassFeat;
    public static Feat? RunesmithRunicRepertoireFeat;
    public static Feat? RunesmithTraceRune;
    public static Feat? RunesmithInvokeRune;
    public static Feat? RunesmithEtchRune;
    public static Feat? RunesmithRunicCrafter;
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        /////////////////
        // Mod Options //
        /////////////////
        ModManager.RegisterBooleanSettingsOption("RunesmithPlaytest.EsvadirOnEnemies",
            "Runesmith: Allow Tracing Esvadir On Enemies",
            "In Dawnsbury Days, the rune \"Esvadir, Rune of Whetstones\" is normally only traceable on allies because its passive effect increases the bearer's damage. Enabling this option allows you to trace Esvadir onto enemies anyway, for when you want to be able to immediately invoke the rune onto a second adjacent enemy before the end of your turn.",
            false);
        ModManager.RegisterBooleanSettingsOption("RunesmithPlaytest.OljinexOnEnemies",
            "Runesmith: Allow Tracing Oljinex On Enemies",
            "In Dawnsbury Days, the rune \"Oljinex, Rune of Cowards' Bane\" is normally only traceable on allies because its passive effect increases the bearer's defenses. Enabling this option allows you to trace Oljinex onto enemies anyway, for when you want to penalize the movemenet of the creatures around a shield-using enemy.",
            false);
        
        ////////////////////
        // Class Features //
        ////////////////////
        RunesmithRunicRepertoireFeat = new RunicRepertoireFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.RunesmithRepertoire", null),
            null,
            "",
            [],
            ModTraits.Runesmith);
        ModManager.AddFeat(RunesmithRunicRepertoireFeat);
        
        // TODO: Populate target dropdowns with trace rune actions
        RunesmithTraceRune = new Feat(
            ModManager.RegisterFeatName("RunesmithPlaytest.TraceRune", "Trace Rune"),
            "Your fingers dance, glowing light leaving behind the image of a rune.",
            "You apply one rune to an adjacent target matching the rune’s Usage description. The rune remains until the end of your next turn. If you spend 2 actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.",
            [Trait.Concentrate, Trait.Magical, Trait.Manipulate],
            null)
            .WithPermanentQEffect("You apply one rune to an adjacent target as an action, or to within 30 feet as two actions.", qfFeat =>
            {
                qfFeat.Name += " {icon:Action}–{icon:TwoActions}"; // No WithActionCost method, so update the sheet name to have actions.
                
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                        return null;
                    
                    List<Possibility> traceActionSections = [];
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(qfThis.Owner);
                    if (repertoire == null)
                        return null;
                    foreach (Rune rune in repertoire.GetRunesKnown(qfThis.Owner))
                    {
                        /*bool CanPayForCombatAction(CombatAction theAction, int actionCost)
                        {
                            Actions ownerActions = theAction.Owner.Actions;
                            if (ownerActions.ActionsLeft >= actionCost)
                                return true;
                            return !ownerActions.UsedQuickenedAction && ownerActions.QuickenedForActions != null && ownerActions.QuickenedForActions.Invoke(theAction) && ownerActions.ActionsLeft + 1 >= actionCost;
                        }*/
                        
                        // BUG: Quickened action from Tracing Trance not usable on the basic Trace actions with insufficient regular actions.
                        
                        CombatAction drawRuneAction = rune.CreateTraceAction(qfThis.Owner, -3);
                        
                        List<Possibility> drawRuneActionPossibilities = 
                        [
                            new ChooseActionCostThenActionPossibility(
                                drawRuneAction,
                                IllustrationName.Action,
                                "One Action",
                                1,
                                /*!CanPayForCombatAction(drawRuneAction, 1) ? Usability.CommonReasons.NoActions :*/ drawRuneAction.Owner.Actions.ActionsLeft < 1 ?
                                    Usability.CommonReasons.NoActions :
                                    (drawRuneAction.Target is DependsOnActionsSpentTarget target1 ?
                                        target1.TargetFromActionCount(1).CanBeginToUse(drawRuneAction.Owner) :
                                        drawRuneAction.Target.CanBeginToUse(drawRuneAction.Owner)),
                                PossibilitySize.Full),
                                
                            new ChooseActionCostThenActionPossibility(
                                drawRuneAction,
                                IllustrationName.TwoActions,
                                "Two Actions",
                                2,
                                /*!CanPayForCombatAction(drawRuneAction, 2) ? Usability.CommonReasons.NoActions :*/ drawRuneAction.Owner.Actions.ActionsLeft < 2 ?
                                    Usability.CommonReasons.NoActions : 
                                    (drawRuneAction.Target is DependsOnActionsSpentTarget target2 ?
                                        target2.TargetFromActionCount(2).CanBeginToUse(drawRuneAction.Owner) :
                                        drawRuneAction.Target.CanBeginToUse(drawRuneAction.Owner)),
                                PossibilitySize.Full),
                        ];
                        
                        SubmenuPossibility runeSubmenu = new SubmenuPossibility(rune.Illustration, rune.Name, PossibilitySize.Half)
                        {
                            SpellIfAny = drawRuneAction,
                            Subsections =
                            {
                                new PossibilitySection(rune.Name) // rune.Name is how features like Drawn In Red find these sections.
                                {
                                    Possibilities = drawRuneActionPossibilities
                                }
                            }
                        };
                        
                        traceActionSections.Add(runeSubmenu);
                    }

                    SubmenuPossibility traceMenu = new SubmenuPossibility(
                        ModIllustrations.TraceRune,
                        "Trace Rune")
                    {
                        SpellIfAny = new CombatAction(qfThis.Owner, ModIllustrations.TraceRune, "Trace Rune", [Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModTraits.Runesmith], "{b}Requirements{b} You have a hand free.\n\nYour fingers dance, glowing light leaving behind the image of a rune. You apply one rune to an adjacent target matching the rune's Usage description. The rune remains until the end of your next turn. If you spend 2 actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.", Target.Self()).WithActionCost(-3), // This doesn't DO anything, it's just to provide description to the menu.
                        Subsections = { new PossibilitySection("Trace Rune")
                        {
                            Possibilities = traceActionSections,
                        }}
                    };
                    return traceMenu;
                };
            });
        ModManager.AddFeat(RunesmithTraceRune);

        RunesmithInvokeRune = new Feat(
            ModManager.RegisterFeatName("RunesmithPlaytest.InvokeRune", "Invoke Rune"),
            "",
            "You utter the name of one or more of your runes within 30 feet. The rune blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed. You can invoke any number of runes with a single Invoke Rune action, but creatures that would be affected by multiple copies of the same specific rune are affected only once, as normal for duplicate effects.",
            [ModTraits.Invocation, Trait.Magical],
            null
            ).WithPermanentQEffect("You invoke any number of runes within 30 feet.", qfFeat =>
            {
                qfFeat.Name += " {icon:Action}";
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction invokeRuneAction = new CombatAction(
                    qfThis.Owner, 
                    ModIllustrations.InvokeRune,
                    "Invoke Rune", 
                    [ModTraits.Invocation, Trait.Magical, ModTraits.Runesmith, Trait.Spell, Trait.Basic, Trait.DoNotShowOverheadOfActionName, Trait.UnaffectedByConcealment],
                    "You utter the name of one or more of your runes within 30 feet. The rune blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed. You can invoke any number of runes with a single Invoke Rune action, but creatures that would be affected by multiple copies of the same specific rune are affected only once, as normal for duplicate effects.",
                    Target.Self().WithAdditionalRestriction(caster =>
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
                                        qfToFind is DrawnRune && // -that is a DrawnRune,
                                        qfToFind.Source == caster && // that is created by us,
                                        qfToFind.Traits.Contains(ModTraits.Rune) && // with the rune trait,
                                        !qfToFind.Traits.Contains(ModTraits.Invocation) // but not the invocation trait.
                                    ) != null
                                )
                            {
                                return null;
                            }
                        }

                        return "No rune-bearers within range";
                    }))
                    .WithActionCost(1)
                    .WithEffectOnEachTarget(async (flurryOfInvokes, self, target, irrelevantResult) =>
                    {   
                        // See Monk.cs, line 106
                        
                        // TODO: try with implementing the Rune.PickARuneToInvokeOnTarget() function I made for Terrifying Invocation
                        
                        /*
                         * Find all the runes in play.
                         */
                        int numberOfRunes = 0; // Number of runes on the field.
                        self.Battle.AllCreatures.ForEach( (Creature cr) =>  // Loop through all the creatures in combat.
                        {
                            if (self.DistanceTo(cr) <= 6) // If the creature is in range,
                            {
                                cr.QEffects.ForEach((QEffect qfOnCreature) => // then loop through its QFs.
                                {
                                    if (qfOnCreature is DrawnRune && // If valid QF,
                                        qfOnCreature.Source == self &&
                                        qfOnCreature.Traits.Contains(ModTraits.Rune) &&
                                        !qfOnCreature.Traits.Contains(ModTraits.Invocation))
                                        numberOfRunes++; // then count it.
                                });
                            }
                        });
                        
                        /*
                         * For each valid rune in play, attempt to take an invoke action, up to all our runes.
                         * We'll do a lot of state-check-like behavior each time we go, in case the field changes
                         * between loops (such as a creature dying, or all instances of a rune-type being invoked).
                         */
                        List<Creature> chosenCreatures = new List<Creature>(); // Unused, but kept in case it's useful later.
                        int i = 0; // In case it screws up.
                        while (numberOfRunes > 0 && i < 100)
                        {
                            await self.Battle.GameLoop.StateCheck(); // Idk why but they all do this so keep it.
                            
                            /*
                             * Regenerate the list of creatures with runes left, in case the field changes too much
                             * between each invocation. E.g. dead creatures, depleted QFs, position changes due to
                             * reaction with forced movement, etc.
                             */
                            numberOfRunes = 0;
                            Dictionary<Creature, List<DrawnRune>> creaturesWithRunes = new Dictionary<Creature, List<DrawnRune>>(); // Reset.
                            self.Battle.AllCreatures.ForEach( (Creature cr) =>
                            {
                                List<DrawnRune> creatureRunes = new List<DrawnRune>();
                                cr.QEffects.ForEach(
                                    qfToFind =>
                                    {
                                        if (qfToFind is DrawnRune drawnRune &&
                                            drawnRune.Source == self &&
                                            drawnRune.Traits.Contains(ModTraits.Rune) &&
                                            !drawnRune.Traits.Contains(ModTraits.Invocation))
                                        {
                                            creatureRunes.Add(drawnRune);
                                        }
                                    });
                                if (creatureRunes.Count > 0 && self.DistanceTo(cr) <= 6)
                                {
                                    creaturesWithRunes[cr] = creatureRunes;
                                    numberOfRunes++;
                                }
                            });
                            
                            /*
                             * Go through every rune-bearer to create an action option usable against their rune.
                             */
                            List<Option> options = new List<Option>(); // List of options to check against every creature.
                            foreach (var crWithRune in creaturesWithRunes) // Loop through all the rune-bearers
                            {
                                foreach (DrawnRune runeQf in crWithRune.Value) // Loop through all the runes one of them bears
                                {
                                    Rune thisRune = runeQf.Rune;
                                    CombatAction? invokeThisRune = thisRune.CreateInvokeAction(flurryOfInvokes, self, runeQf);
                                    if (invokeThisRune != null)
                                    {
                                        GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, options, false);
                                    }
                                }
                            }
                            
                            if (options.Count <= 0) continue; // Go to next loop if no options.
                            
                            if (i == 0) // If at the beginning of the action,
                                options.Add(new CancelOption(true)); // allow us to cancel it.
                            
                            options.Add(new PassViaButtonOption(" Confirm no additional runes "));
                            
                            // Await which option (even if just 1) to take.
                            Option chosenOption = (await self.Battle.SendRequest( // Send a request to pick an option
                                new AdvancedRequest(self, "Choose a rune to invoke.", options)
                                {
                                    TopBarText = "Choose a rune to invoke" + (i==0 ? " or right-click to cancel" : null) + $". ({numberOfRunes})",
                                    TopBarIcon = flurryOfInvokes.Illustration,
                                })).ChosenOption;

                            switch (chosenOption)
                            {
                                case CreatureOption creatureOption:
                                {
                                    chosenCreatures.Add(creatureOption.Creature);
                                    break;
                                }
                                case CancelOption:
                                    flurryOfInvokes.RevertRequested = true;
                                    chosenCreatures = null;
                                    return;
                                case PassViaButtonOption:
                                    chosenCreatures = null;
                                    return;
                            }

                            await chosenOption.Action();
                            
                            i++;
                        }

                        chosenCreatures = null;
                    })
                    .WithEffectOnChosenTargets(async (caster, targets) =>
                    {
                        foreach (Creature cr in caster.Battle.AllCreatures)
                        {
                            Rune.RemoveAllImmunities(cr);
                        }
                    });
                    
                    return new ActionPossibility(invokeRuneAction);
                };
            });
        ModManager.AddFeat(RunesmithInvokeRune);
        
        // BUG: Action triggers before animal companions spawn.
        RunesmithEtchRune = new Feat(
            ModManager.RegisterFeatName("RunesmithPlaytest.EtchRune", "Etch Rune"),
            "An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.",
            "At the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they’re expended or removed. You can etch up to 2 runes, and you can etch an additional rune at levels 5, 9, 13, and 17.",
            [], // No traits, we don't want anything inside the encounter to try to interact with this action at the start of combat.
            null)
            .WithPermanentQEffect("You apply runes to your allies at the start of combat which last until the end of combat or consumed.", qfFeat =>
            {
                qfFeat.StartOfCombat = async qfThis =>
                {
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(qfThis.Owner);
                    if (repertoire == null)
                        return;
                    
                    // Runic Tattoo first
                    if (qfThis.Owner.HasFeat(ClassFeats.RunicTattoo!.FeatName))
                    {
                        QEffect? runicTattooFeat = qfFeat.Owner.QEffects.FirstOrDefault(qf =>
                            qf.Name is { } name && name.Contains("Runic Tattoo"));
                        if (runicTattooFeat != null)
                            await runicTattooFeat.StartOfCombat!.Invoke(runicTattooFeat);
                    }
                    
                    List<Rune> runesKnown = repertoire.GetRunesKnown(qfFeat.Owner);
                    int etchLimit = repertoire.EtchLimit;

                    for (int i = 0; i < etchLimit; i++)
                    {
                        await qfThis.Owner.Battle.GameLoop.StateCheck(); // Idk why but they all do this so keep it.
                        
                        List<Option> options = new List<Option>();
                        foreach (Rune rune in runesKnown)
                        {
                            CombatAction etchThisRune = rune.CreateEtchAction(qfThis.Owner);
                            etchThisRune.Traits.Add(Trait.DoNotShowOverheadOfActionName);
                            GameLoop.AddDirectUsageOnCreatureOptions(etchThisRune, options);
                        }
                        
                        if (options.Count <= 0)
                            continue; // Go to next loop if no options.
                            
                        //if (i == 0) // If at the beginning of the action,
                            //options.Add(new CancelOption(true)); // allow us to cancel it.
                            
                        options.Add(new PassViaButtonOption(" Confirm no additional etchings "));
                        
                        // Await which option (even if just 1) to take.
                        Option chosenOption = (await qfThis.Owner.Battle.SendRequest( // Send a request to pick an option
                            new AdvancedRequest(qfThis.Owner, "Etch a rune on yourself or an ally.", options)
                            {
                                TopBarText = $"Etch a rune on yourself or an ally. ({i+1}/{etchLimit})",
                                TopBarIcon = ModIllustrations.EtchRune,
                            })).ChosenOption;
                        
                        switch (chosenOption)
                        {
                            case CreatureOption creatureOption:
                            {
                                break;
                            }
                            //case CancelOption:
                                //return;
                            case PassViaButtonOption:
                                return;
                        }

                        await chosenOption.Action();
                    }

                    qfThis.Tag = true; // True means the runes have been etched.
                };
            });
        ModManager.AddFeat(RunesmithEtchRune);

        RunesmithRunicCrafter = new Feat(
            ModManager.RegisterFeatName("RunesmithPlaytest.RunicCrafter", "Runic Crafter"),
            "Your study of secret runes leaves you well practiced in crafting their more common cousins.",
            "Your equipment gains the effects of the highest level fundamental armor and weapon runes for your level. This does not count as having runes for the purposes of other rules (you must still have potency runes to apply property runes).",
            [],
            null)
            .WithPermanentQEffect("INCOMPLETE TEXT", qfFeat =>
            {
                int lvl = qfFeat.Owner.Level;
                
                // Uses code from ABP //
                // Will need to fix Striking if Dawnsbury increases to levels beyond 8th.
                int attackPotency = lvl switch
                {
                    <= 1 => 0,
                    <= 9 => 1,
                    <= 15 => 2,
                    _ => 3
                };
                
                int defensePotency = lvl switch
                {
                    <= 4 => 0,
                    <= 10 => 1,
                    <= 17 => 2,
                    _ => 3
                };
                
                int savingThrowPotency = lvl switch
                {
                    <= 7 => 0,
                    <= 13 => 1,
                    <= 19 => 2,
                    _ => 3
                };
                
                // Attack Potency
                qfFeat.BonusToAttackRolls = (qfSelf, combatAction, defender) =>
                {
                    if (combatAction.HasTrait(Trait.Attack) && combatAction.Item != null &&
                        (combatAction.Item.HasTrait(Trait.Weapon) || combatAction.Item.HasTrait(Trait.Unarmed)) &&
                        attackPotency > 0)
                    {
                        return new Bonus(attackPotency, BonusType.Item, "Runic Crafter");
                    }

                    return null;
                };
                
                // Striking
                qfFeat.IncreaseItemDamageDieCount = (qfSelf, item) =>
                {
                    if (lvl < 4) // At 4 or higher, add striking.
                        return false;
                    
                    return item.WeaponProperties?.DamageDieCount == 1;
                };
                
                qfFeat.BonusToDefenses = (effect, action, defense) =>
                {
                    // AC Potency
                    if (defense == Defense.AC && defensePotency > 0)
                    {
                        var itemBonus = effect.Owner.Armor.Item?.ArmorProperties?.ItemBonus ?? 0;
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
                };

                qfFeat.Description = "" +
                                     (attackPotency > 0 ? $"You have a +{attackPotency} item bonus to weapon attack rolls" : null) +
                                     (lvl >= 4 ? ", your Strikes deal two damage dice instead of one" : null) +
                                     (defensePotency > 0 ? $", a +{defensePotency} item bonus to AC" : null) +
                                     (savingThrowPotency > 0 ? $", and a +{savingThrowPotency} item bonus to all saving throws" : null) +
                                     ".";
            });
        ModManager.AddFeat(RunesmithRunicCrafter);
        
        /////////////////////
        // Runesmith Class //
        /////////////////////
        RunesmithClassFeat = new ClassSelectionFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.RunesmithClass", "Runesmith"),
            "At the heart of all communication is the word, and at the heart of all magic is the rune. Equal parts scholar and artist, you devote yourself to the study of these mystic symbols, learning to carve, etch, brand, and paint the building blocks of magic to channel powers greater than yourself.",
            ModTraits.Runesmith,
            new EnforcedAbilityBoost(Ability.Intelligence),
            8,
            [Trait.Perception, Trait.Reflex, Trait.Unarmed, Trait.Simple, Trait.Martial, Trait.UnarmoredDefense, Trait.LightArmor, Trait.MediumArmor, Trait.Crafting],
            [Trait.Fortitude, Trait.Will],
            2,
            "{b}1. Runic Repertoire.{/b} A runesmith doesn't cast spells, but they can use {tooltip:Runesmith.Trait.Rune}runesmith runes{/}. You learn 4 runes of 1st-level. You learn additional runes at higher levels. Your runes are the same level you are, regardless when you learn them {i}(some runes increase in power at higher levels, as listed in their Level entry){/i}. Runes use your class DC, which is based on Intelligence." +
            "\r\n\r\n{b}2. Applying Runes.{/b} You can apply runes in one of two ways: {i}tracing{/i} the rune with the {tooltip:Runesmith.Action.TraceRune}Trace Rune{/} action, or by {i}etching{/i} the rune at the start of combat with the {tooltip:Runesmith.Action.EtchRune}Etch Rune{/} activity." +
            "\r\n\r\n{b}3. Invoking Runes.{/b} You can also invoke a rune with the {tooltip:Runesmith.Action.InvokeRune}Invoke Rune{/} action." +
            "\r\n\r\n{b}4. Runesmith feat.{/b}" +
            "\r\n\r\n{b}5. Shield block {icon:Reaction}.{/b} You can use your shield to reduce damage you take from attacks" +
            "\r\n\r\n{b}At higher levels:{/b}" +
            "\r\n{b}Level 2:{/b} Runesmith feat, {tooltip:Runesmith.Features.RunicCrafter}runic crafter{/}" +
            "\r\n{b}Level 3:{/b} General feat, skill increase, additional level 1 rune known" +
            "\r\n{b}Level 4:{/b} Runesmith feat" +
            "\r\n{b}Level 5:{/b} Attribute boosts, ancestry feat, skill increase, {tooltip:Runesmith.Features.SmithsWeaponExpertise}smith's weapon expertise{/}, additional level 1 rune known, additional maximum etched rune" +
            "\r\n{b}Level 6:{/b} Runesmith feat" +
            "\r\n{b}Level 7:{/b} General feat, skill increase, expert class DC, expert in Reflex saves, {tooltip:Runesmith.Features.RunicOptimization}runic optimization{/} ({Red}NYI{/Red}, uses Weapon Specialization), additional level 1 rune known" + // TODO: adjust text with Runic Optimization implementation
            "\r\n{b}Level 8:{/b} Runesmith feat",
            null)
            .WithOnSheet( values =>
            {
                // extra skill
                values.AddSelectionOption(new SingleFeatSelectionOption(
                    "runesmithSkills",
                    "Runesmith skill",
                    1,
                    ft =>
                        ft.FeatName is FeatName.Arcana or FeatName.Nature or FeatName.Occultism or FeatName.Religion)
                    .WithIsOptional());
                
                // level 1 class feat
                values.AddSelectionOption(new SingleFeatSelectionOption(
                    "RunesmithFeat1",
                    "Runesmith feat",
                    1,
                    ft =>
                        ft.HasTrait(ModTraits.Runesmith)));
                
                // other feats
                values.GrantFeat(FeatName.ShieldBlock);
                
                // runic repertoire
                values.GrantFeat(RunesmithRunicRepertoireFeat.FeatName);
                values.AddSelectionOption(new MultipleFeatSelectionOption(
                    "initialRunes",
                    "Initial level 1 runes",
                    1,
                    ft =>
                        ft is RuneFeat, 4)
                    .WithIsOptional());
                for (int i=3; i<=7; i=i+2) // Gain a new Rune every other level.
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("rune"+i, "Level 1 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                for (int i=9; i<=15; i=i+2) // Gain a new Rune every other level.
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("rune"+i, "Level 9 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                for (int i=17; i<=19; i=i+2) // Gain a new Rune every other level.
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("rune"+i, "Level 17 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                
                // class features
                values.GrantFeat(RunesmithTraceRune.FeatName);
                values.GrantFeat(RunesmithInvokeRune.FeatName);
                values.GrantFeat(RunesmithEtchRune.FeatName);
                
                // higher levels
                values.AddAtLevel(2, values =>
                {
                    values.GrantFeat(RunesmithRunicCrafter.FeatName);
                });
                values.AddAtLevel(5, values =>
                {
                    values.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                    values.SetProficiency(Trait.Simple, Proficiency.Expert);
                    values.SetProficiency(Trait.Martial, Proficiency.Expert);
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values)!;
                    repertoire.EtchLimit = 3;
                });
                values.AddAtLevel(7, values =>
                {
                    values.SetProficiency(Trait.Reflex, Proficiency.Expert);
                    values.SetProficiency(ModTraits.Runesmith, Proficiency.Expert);
                });
                // Future content
                values.AddAtLevel(9, values =>
                {
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values)!;
                    repertoire.EtchLimit = 4;
                });
                values.AddAtLevel(11, values =>
                {
                    values.SetProficiency(Trait.Fortitude, Proficiency.Master);
                    // See WithOnCreature for the success->critical effect.
                });
                values.AddAtLevel(13, values =>
                {
                    values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Expert);
                    values.SetProficiency(Trait.LightArmor, Proficiency.Expert);
                    values.SetProficiency(Trait.MediumArmor, Proficiency.Expert);
                    
                    values.SetProficiency(Trait.Perception, Proficiency.Expert);
                    
                    values.SetProficiency(Trait.Unarmed, Proficiency.Master);
                    values.SetProficiency(Trait.Simple, Proficiency.Master);
                    values.SetProficiency(Trait.Martial, Proficiency.Master);
                    
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values)!;
                    repertoire.EtchLimit = 5;
                });
                values.AddAtLevel(15, values =>
                {
                    values.SetProficiency(ModTraits.Runesmith, Proficiency.Master);
                });
                values.AddAtLevel(17, values =>
                {
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values)!;
                    repertoire.EtchLimit = 6;
                });
                values.AddAtLevel(19, values =>
                {
                    values.SetProficiency(ModTraits.Runesmith, Proficiency.Legendary);
                    
                    values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Master);
                    values.SetProficiency(Trait.LightArmor, Proficiency.Master);
                    values.SetProficiency(Trait.MediumArmor, Proficiency.Master);
                });
            })
            .WithOnCreature(cr =>
            {
                if (cr.Level >= 7)
                {
                    // TODO: Runic Optimization, which is slightly different from weapon spec.
                    cr.AddQEffect(QEffect.WeaponSpecialization());
                }
                
                // Higher level content
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
                
                if (cr.Level >= 15)
                {
                    // Lv15: greater weapon spec "greater runic optimization"
                }
            });
        RunesmithClassFeat.RulesText = RunesmithClassFeat.RulesText
            .Replace("Key ability", "Key attribute")
            .Replace("trained in Crafting", "trained in Crafting; as well as your choice of Arcana, Nature, Occultism, or Religion;");
        ModManager.AddFeat(RunesmithClassFeat);
        
        /////////////////////////
        // Runesmith Archetype //
        /////////////////////////
        Feat runesmithDedication = ArchetypeFeats.CreateMulticlassDedication(
            ModTraits.Runesmith,
            "You have dabbled in the scholarly art at the heart of all magic, the rune.",
            "You become trained in Crafting; if you were already trained in Crafting, you instead become trained in a skill of your choice.\n\nYou can use {tooltip:Runesmith.Trait.Rune}runes{/} like a runesmith. You gain a runic repertoire with two 1st-level runes from the runesmith's rune list. The DCs for these runes is based on your class DC and your Intelligence.\n\nYou can use the {tooltip:Runesmith.Action.TraceRune}Trace Rune{/} {icon:Action}–{icon:TwoActions} and {tooltip:Runesmith.Action.InvokeRune}Invoke Rune{/} {icon:Action} actions.")
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
                values.GrantFeat(RunesmithRunicRepertoireFeat.FeatName);
                values.AddSelectionOptionRightNow(new MultipleFeatSelectionOption(
                        "initialRunes",
                        "Initial level 1 runes",
                        values.CurrentLevel,
                        ft =>
                            ft is RuneFeat, 2)
                    .WithIsOptional());
                values.GrantFeat(RunesmithTraceRune.FeatName);
                values.GrantFeat(RunesmithInvokeRune.FeatName);
                values.SetProficiency(ModTraits.Runesmith, Proficiency.Trained); // Might be redundant, but just in case...
                values.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(ModTraits.Runesmith), values.Class!.ClassTrait);
            })
            .WithPrerequisite(values => // Can't use the built-in WithDemandsAbility, so as to avoid non-ORC text.
                values.FinalAbilityScores.TotalScore(Ability.Intelligence) >= 14,
                "You must have Intelligence +2 or more.");
        runesmithDedication.Traits.Add(Trait.Homebrew);
        ModManager.AddFeat(runesmithDedication);

        foreach (Feat feat in ArchetypeFeats.CreateBasicAndAdvancedMulticlassFeatGrantingArchetypeFeats(ModTraits.Runesmith, "Runic Technique"))
        {
            ModManager.AddFeat(feat);
        }

        TrueFeat runesmithLearnRune = (new TrueFeat(
                ModManager.RegisterFeatName("RunesmithPlaytest.Archetype.ExpandKnowledge", "Expand Knowledge"),
                2,
                null,
                "You add a 1st-level runesmith rune of your choice to your runic repertoire.",
                [])
            .WithMultipleSelection()
            .WithAvailableAsArchetypeFeat(ModTraits.Runesmith)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption("rune"+values.CurrentLevel, "Level 1 rune", values.CurrentLevel, ft => ft is RuneFeat rf && rf.Rune.BaseLevel == 1).WithIsOptional());
            }) as TrueFeat)!;
        ModManager.AddFeat(runesmithLearnRune);
        
        ////////////////
        // Load Calls //
        ////////////////
        ModTooltips.RegisterTooltips();
        RunesmithClassRunes.LoadRunes();
        ClassFeats.CreateFeats();
    }
    
    public static int RunesmithDC(Creature runesmith)
    {
        return runesmith.PersistentCharacterSheet?.Class != null
            ? runesmith.Proficiencies.Get([ModTraits.Runesmith]).ToNumber(runesmith.Level) + runesmith.Abilities.Intelligence + 10
            : Checks.DetermineClassProficiencyFromMonsterLevel(runesmith.Level).ToNumber(runesmith.Level) + runesmith.Abilities.GetTop() + 10;
    }
    
    /// <summary>
    /// Asynchronously gets a user selected tile that is closer to an enemy
    /// </summary>
    /// <param name="self">The creature being used to compare distance</param>
    /// <param name="enemy">(nullable) The creature, if any, to get closer to. Overrides checking if the creature is an enemy of self.</param>
    /// <param name="messageString">The user prompt message</param>
    /// <returns>The selected tile or null</returns>
    public static async Task<bool> StrideCloserToEnemyAsync(Creature self, Creature? enemy, string messageString)
    {
        self.Actions.NextStrideIsFree = true;
        self.RegeneratePossibilities();
        
        if (self.Speed <= 0)
        {
            self.Actions.NextStrideIsFree = false;
            return true;
        }
        
        // Determines the starting tile, all enemy tiles and initatlizes the options list
        Tile startingTile = self.Occupies;
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
        
        List<Option> options = new List<Option>();
        
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
        
        Option selectedOption = null;
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
            int num = await selectedOption.Action() ? 1 : 0;
            self.Battle.MovementConfirmer = (MovementConfirmer) null;
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
}

/*
Added the ability for mods to add settings options with  ModManager.RegisterBooleanSettingsOption(string technicalName, string caption, string longDescription, bool default) for registration API and PlayerProfile.Instance.IsBooleanOptionEnabled(string technicalName) for reading API.

You can now use the many new methods in the CommonQuestions class to add dialogue and other player interactivity choices.
*/

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