using System.Diagnostics;
using System.Text.RegularExpressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
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
    public static Feat RunesmithClassFeat;
    public static Feat RunesmithRunicRepertoireFeat;
    public static Feat RunesmithTraceRune;
    public static Feat RunesmithInvokeRune;
    public static Feat RunesmithEtchRune;
    
    // PUBLISH: Don't forget these attributions on the workshop page.
    public static Illustration TraceRuneIllustration = new ModdedIllustration("RunesmithAssets/trace rune.png");
    public static Illustration InvokeRuneIllustration = new ModdedIllustration("RunesmithAssets/invoke rune.png");
    public static Illustration EtchRuneIllustration = new ModdedIllustration("RunesmithAssets/handcraft.png");
    public static Illustration NoSymbolIllustration = new ModdedIllustration("RunesmithAssets/no symbol.png");
    public static Illustration TransposeEtchingIllustration = new ModdedIllustration("RunesmithAssets/hand.png");
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        RunesmithRunicRepertoireFeat = new RunicRepertoireFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.RunesmithRepertoire", null),
            null,
            "",
            [],
            ModTraits.Runesmith
        );
        ModManager.AddFeat(RunesmithRunicRepertoireFeat);
        
        // TODO: Populate target dropdowns with trace rune actions
        RunesmithTraceRune = new Feat(
            ModManager.RegisterFeatName("RunesmithPlaytest.TraceRune", "Trace Rune"),
            "Your fingers dance, glowing light leaving behind the image of a rune.",
            "You apply one rune to an adjacent target matching the rune’s Usage description. The rune remains until the end of your next turn. If you spend 2 actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.",
            [Trait.Concentrate, Trait.Magical, Trait.Manipulate],
            null
            ).WithPermanentQEffect("You apply one rune to an adjacent target as an action, or to within 30 feet as two actions.", (QEffect qf) =>
            {
                qf.Name += " {icon:Action}–{icon:TwoActions}";
                
                qf.ProvideActionIntoPossibilitySection = (qf, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                        return null;
                    
                    List<Possibility> traceActionSections = [];
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(qf.Owner);
                    if (repertoire == null)
                        return null;
                    foreach (Rune rune in repertoire.RunesKnown)
                    {
                        Trait[] traits = rune.Traits.ToArray();
                        foreach (Trait trt in (List<Trait>)[Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModTraits.Traced])
                        {
                            traits = traits.Append(trt).ToArray();
                        }
                        
                        // TODO: Change methods to allow for heightened text in combat actions
                        // TODO: use helper trace action creation
                        CombatAction drawRuneAction = new CombatAction(
                            qf.Owner,
                            rune.Illustration,
                            "Trace " + rune.Name,
                            traits,
                            rune.GetFullyFormattedDescriptionWithInsertion(2, "{icon:Action} The range is touch.\n{icon:TwoActions} The range is 30 feet.\n\n"),
                            Target.DependsOnActionsSpent(
                                    Target.AdjacentCreatureOrSelf(),
                                    Target.RangedCreature(6),
                                    null!))
                            {
                                Tag = rune,
                            }
                            .WithActionCost(-3)
                            .WithCreateVariantDescription((actions, spellVariant) =>
                            { // Just having this gives the variant range information.
                                return actions switch
                                {
                                    //1 => rune.GetFullyFormattedDescription(false),
                                    //2 => rune.GetFullyFormattedDescription(false),
                                    _ => rune.GetFullyFormattedDescription(false)
                                };
                            })
                            .WithSoundEffect(SfxName.AncientDust) // TODO: Consider alternative SFX for Trace Rune.
                            .WithEffectOnEachTarget(async (action, caster, target, result) =>
                            {
                                Rune actionRune = (action.Tag as Rune)!;
                                if (!await actionRune.DrawRuneOnTarget(action, caster, target))
                                    action.RevertRequested = true;
                            });
                        
                        // Extra requirements
                        foreach (Target tar in (drawRuneAction.Target as DependsOnActionsSpentTarget)!.Targets)
                        {
                            if (tar is not CreatureTarget crTar)
                                continue;
                            crTar.WithAdditionalConditionOnTargetCreature( // Free hand
                                (attacker, defender) =>
                                    attacker.HasFreeHand ? Usability.Usable : Usability.NotUsable("You must have a free hand to trace a rune"));
                            if (rune.UsageCondition != null)
                                crTar.WithAdditionalConditionOnTargetCreature(rune.UsageCondition); // Usage condition
                        }
                        
                        List<Possibility> drawRuneActionPossibilities = 
                        [
                            new ChooseActionCostThenActionPossibility(drawRuneAction, IllustrationName.Action, "One Action", 1, drawRuneAction.Owner.Actions.ActionsLeft < 1 ? Usability.CommonReasons.NoActions : (drawRuneAction.Target is DependsOnActionsSpentTarget target1 ? target1.TargetFromActionCount(1).CanBeginToUse(drawRuneAction.Owner) : drawRuneAction.Target.CanBeginToUse(drawRuneAction.Owner)), PossibilitySize.Full),
                                
                            new ChooseActionCostThenActionPossibility(drawRuneAction, IllustrationName.TwoActions, "Two Actions", 2, drawRuneAction.Owner.Actions.ActionsLeft < 2 ? Usability.CommonReasons.NoActions : (drawRuneAction.Target is DependsOnActionsSpentTarget target2 ? target2.TargetFromActionCount(2).CanBeginToUse(drawRuneAction.Owner) : drawRuneAction.Target.CanBeginToUse(drawRuneAction.Owner)), PossibilitySize.Full),
                        ];
                        
                        SubmenuPossibility runeSubmenu = new SubmenuPossibility(rune.Illustration, rune.Name, PossibilitySize.Half)
                        {
                            SpellIfAny = drawRuneAction,
                            Subsections =
                            {
                                new PossibilitySection(rune.Name)
                                {
                                    Possibilities = drawRuneActionPossibilities
                                }
                            }
                        };
                        
                        traceActionSections.Add(runeSubmenu);
                    }

                    Illustration traceRune = TraceRuneIllustration;
                    SubmenuPossibility traceMenu = new SubmenuPossibility(
                        traceRune,
                        "Trace Rune")
                    {
                        SpellIfAny = new CombatAction(qf.Owner, traceRune, "Trace Rune", [Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModTraits.Runesmith], "{b}Requirements{b} You have a hand free.\n\nYour fingers dance, glowing light leaving behind the image of a rune. You apply one rune to an adjacent target matching the rune's Usage description. The rune remains until the end of your next turn. If you spend 2 actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.", Target.Self()).WithActionCost(-3), // This doesn't DO anything, it's just to provide description to the menu.
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
            ).WithPermanentQEffect("You invoke any number of runes within 30 feet.", (QEffect qf) =>
            {
                qf.Name += " {icon:Action}";
                
                CombatAction invokeRuneAction = new CombatAction(
                    qf.Owner, 
                    InvokeRuneIllustration,
                    "Invoke Rune", 
                    [ModTraits.Invocation, Trait.Magical, ModTraits.Runesmith, Trait.Basic], 
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
                                        GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, options, false); // pass false so it still prompts with 1 option
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
                qf.ProvideMainAction = (qf) => new ActionPossibility(invokeRuneAction);
            });
        ModManager.AddFeat(RunesmithInvokeRune);
        
        // PUBLISH: A free action at the start of combat to etch runes up to your maximum.
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
                    
                    List<Rune> runesKnown = repertoire.RunesKnown;
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
                                TopBarIcon = EtchRuneIllustration,
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
                };
            });
        ModManager.AddFeat(RunesmithEtchRune);
        
        RunesmithClassFeat = new ClassSelectionFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.RunesmithClass", "Runesmith"),
            "At the heart of all communication is the word, and at the heart of all magic is the rune. Equal parts scholar and artist, you devote yourself to the study of these mystic symbols, learning to carve, etch, brand, and paint the building blocks of magic to channel powers greater than yourself.",
            ModTraits.Runesmith,
            new EnforcedAbilityBoost(Ability.Intelligence),
            8,
            [Trait.Perception, Trait.Reflex, Trait.Unarmed, Trait.Simple, Trait.Martial, Trait.UnarmoredDefense, Trait.LightArmor, Trait.MediumArmor, Trait.Crafting],
            [Trait.Fortitude, Trait.Will],
            2,
            "{b}1. Runic Repertoire.{/b} You learn 4 runes of your choice from the list of 1st-level runes, which you can use any number of times per day with the Trace Rune {icon:Action}–{icon:TwoActions} and Invoke Rune {icon:Action} actions. You gain additional runes at higher levels. Any rune you use is the same level you are, regardless of the level you acquire them, sometimes increasing in power as you level-up. Runes use your class DC, which is based on Intelligence." +
            "\r\n\r\n{b}2. Trace Rune {icon:Action}–{icon:TwoActions}{/b} If you have a free hand, you apply one rune to an adjacent creature, or a creature within 30 feet for {icon:TwoActions} 2 actions. The rune remains until the end of your next turn." + 
            "\r\n\r\n{b}3. Invoke Rune {icon:Action}{/b} You utter the name of one or more of your runes within 30 feet. This applies the effects of its Invocation entry, and then the rune fades away. You can invoke multiple runes as part of this action, but a creature is immune to additional instances of the same invocation until the action completes." + 
            "\r\n\r\n{b}4. Etch Rune{/b} At the start of combat, you can etch up to 2 of your runes on yourself or your allies. These last until the end of combat, or until you invoke them. You can etch additional runes at higher levels." + 
            "\r\n\r\n{b}5. Runesmith feat.{/b}" +
            "\r\n\r\n{b}6. Shield block {icon:Reaction}.{/b} You can use your shield to reduce damage you take from attacks." +
            "\r\n\r\n{b}At higher levels:{/b}" +
            "\r\n{b}Level 2:{/b} Runesmith Feat, (NYI)Runic Crafter." + // TODO: find something for Runic Crafter to do
            "\r\n{b}Level 3:{/b} General feat, skill increase, additional level 1 rune known" +
            "\r\n{b}Level 4:{/b} Runesmith feat" +
            "\r\n{b}Level 5:{/b} Attribute boosts, ancestry feat, skill increase, smith's weapon expertise {i}(expert in simple weapons, martial weapons, and unarmed attacks){/i}, additional level 1 rune known, additional maximum etched rune" +
            "\r\n{b}Level 6:{/b} Runesmith feat" +
            "\r\n{b}Level 7:{/b} General feat, skill increase, expert class DC, expert in Reflex saves, runic optimization (NYI, uses regular Weapon Specialization) {i}(you deal 2 additional damage with weapons bearing a striking rune, or 3 damage with greater striking runes, or 4 damage with major striking runes){/i}, additional level 1 rune known" + // TODO: adjust text with Runic Optimization implementation
            "\r\n{b}Level 8:{/b} Runesmith feat",
            null
            ).WithOnSheet( sheet =>
            {
                // skills
                sheet.AddSelectionOption(new SingleFeatSelectionOption("runesmithSkills", "Runesmith Skill", 1, (ft) => ft.FeatName is FeatName.Arcana or FeatName.Nature or FeatName.Occultism or FeatName.Religion).WithIsOptional());
                
                // class feat
                sheet.AddSelectionOption((SelectionOption) new SingleFeatSelectionOption("RunesmithFeat1", "Runesmith feat", 1, (Func<Feat, bool>) (ft => ft.HasTrait(ModTraits.Runesmith))));
                
                // other feats
                sheet.GrantFeat(FeatName.ShieldBlock);
                
                // class features
                sheet.GrantFeat(RunesmithTraceRune.FeatName);
                sheet.GrantFeat(RunesmithInvokeRune.FeatName);
                sheet.GrantFeat(RunesmithEtchRune.FeatName);
                
                // PUBLISH: No ability to assign tradition traits
                
                // runic repertoire
                // TODO: Remove WithIsOptional when enough runes are added.
                sheet.GrantFeat(RunesmithRunicRepertoireFeat.FeatName);
                sheet.AddSelectionOption((SelectionOption) new MultipleFeatSelectionOption("initialRunes", "Initial level 1 runes", 1, ft => ft is RuneFeat, 4).WithIsOptional());
                for (int i=3; i<=7; i=i+2) // Gain a new Rune every other level.
                {
                    sheet.AddSelectionOption((SelectionOption) new SingleFeatSelectionOption("rune"+i, "Level 1 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                for (int i=9; i<=15; i=i+2) // Gain a new Rune every other level.
                {
                    sheet.AddSelectionOption((SelectionOption) new SingleFeatSelectionOption("rune"+i, "Level 9 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                for (int i=17; i<=19; i=i+2) // Gain a new Rune every other level.
                {
                    sheet.AddSelectionOption((SelectionOption) new SingleFeatSelectionOption("rune"+i, "Level 17 rune", i, ft => ft is RuneFeat).WithIsOptional());
                }
                
                // higher levels
                sheet.AddAtLevel(5, values =>
                {
                    values.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                    values.SetProficiency(Trait.Simple, Proficiency.Expert);
                    values.SetProficiency(Trait.Martial, Proficiency.Expert);
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values.Sheet)!;
                    repertoire.EtchLimit = 3;
                });
                sheet.AddAtLevel(7, values =>
                {
                    values.SetProficiency(Trait.Reflex, Proficiency.Expert);
                    values.SetProficiency(ModTraits.Runesmith, Proficiency.Expert);
                });
                sheet.AddAtLevel(9, values =>
                {
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values.Sheet)!;
                    repertoire.EtchLimit = 4;
                });
                sheet.AddAtLevel(11, values =>
                {
                    values.SetProficiency(Trait.Fortitude, Proficiency.Master);
                    // See WithOnCreature for the success->critical effect.
                });
                sheet.AddAtLevel(13, values =>
                {
                    values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Expert);
                    values.SetProficiency(Trait.LightArmor, Proficiency.Expert);
                    values.SetProficiency(Trait.MediumArmor, Proficiency.Expert);
                    
                    values.SetProficiency(Trait.Perception, Proficiency.Expert);
                    
                    values.SetProficiency(Trait.Unarmed, Proficiency.Master);
                    values.SetProficiency(Trait.Simple, Proficiency.Master);
                    values.SetProficiency(Trait.Martial, Proficiency.Master);
                    
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values.Sheet)!;
                    repertoire.EtchLimit = 5;
                });
                sheet.AddAtLevel(15, values =>
                {
                    values.SetProficiency(ModTraits.Runesmith, Proficiency.Master);
                });
                sheet.AddAtLevel(17, values =>
                {
                    RunicRepertoireFeat repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values.Sheet)!;
                    repertoire.EtchLimit = 6;
                });
                sheet.AddAtLevel(19, values =>
                {
                    values.SetProficiency(ModTraits.Runesmith, Proficiency.Legendary);
                    
                    values.SetProficiency(Trait.UnarmoredDefense, Proficiency.Master);
                    values.SetProficiency(Trait.LightArmor, Proficiency.Master);
                    values.SetProficiency(Trait.MediumArmor, Proficiency.Master);
                });
            }).WithOnCreature( (Creature cr) =>
            {
                if (cr.Level >= 7)
                {
                    // TODO: Runic Optimization, which is slightly different from weapon spec.
                    // PUBLISH: just regular weapon spec
                    cr.AddQEffect(QEffect.WeaponSpecialization());
                }
                
                if (cr.Level >= 11)
                {
                    cr.AddQEffect(new QEffect("Smith's Endurance", "When you roll a success on a Fortitude save, you get a critical success instead.")
                    {
                        AdjustSavingThrowCheckResult = (Func<QEffect, Defense, CombatAction, CheckResult, CheckResult>) ((effect, defense, action, checkResult) => defense != Defense.Fortitude || checkResult != CheckResult.Success ? checkResult : CheckResult.CriticalSuccess)
                    });
                    // See WithOnSheet for the Master proficiency increase.
                }
                
                if (cr.Level >= 15)
                {
                    // Lv15: greater weapon spec "greater runic optimization"
                }
            });
        // Regex Text Fixes
        // Skill fixes
        var skillProfText = Regex.Match(RunesmithClassFeat.RulesText, "{b}Skill proficiencies\\.{\\/b} You're trained in Crafting");
        RunesmithClassFeat.RulesText = RunesmithClassFeat.RulesText
            .Insert(skillProfText.Index + skillProfText.Length, "; as well as your choice of Arcana, Nature, Occultism, or Religion;");
        // ORC fixes
        var illegalNotORCText = Regex.Match(RunesmithClassFeat.RulesText, "Key ability:");
        var legalORCText = RunesmithClassFeat.RulesText
            .Remove(illegalNotORCText.Index, illegalNotORCText.Length)
            .Insert(illegalNotORCText.Index, "Key attribute:");
        RunesmithClassFeat.RulesText = legalORCText;
        ModManager.AddFeat(RunesmithClassFeat);
        
        RunesmithClassFeats.CreateFeats();
        RunesmithClassRunes.LoadRunes();
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