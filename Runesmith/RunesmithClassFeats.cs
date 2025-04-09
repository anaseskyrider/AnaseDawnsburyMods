using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunesmithClassFeats
{
    // 1sts
    public static Feat BackupRunicEnhancement;
    public static Feat EngravingStrike;
    public static Feat RemoteDetonation;
    public static Feat RuneSinger;
    // 2nds
    public static Feat FortifyingKnock;
    public static Feat InvisibleInk; // Probably not going to exist. Maybe Trace Rune breaks stealth, and this stops that?
    public static Feat RunicTattoo;
    public static Feat SmithingWeaponsFamiliarity;
    // 4ths
    public static Feat ArtistsAttendance;
    public static Feat GhostlyResonance;
    public static Feat TerrifyingInvocation;
    public static Feat TransposeEtching;
    // 6ths
    public static Feat RunicReprisal;
    public static Feat TracingTrance;
    public static Feat VitalCompositeInvocation;
    public static Feat WordsFlyFree;
    // 8ths
    public static Feat DrawnInRed;
    public static Feat ElementalRevision;
    public static Feat ReadTheBones;
    
    public static void CreateFeats()
    {
        /* 1st Level Feats */
        BackupRunicEnhancement = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatBackupRunicEnhancement", "Backup Runic Enhancement"),
            1,
            "While you are not a spellcaster, you have a working knowledge of the most fundamental of runic magic.",
            "Once per day, you can cast your choice of either runic body or runic weapon as an innate spell. The rank of these spells is equal to half your level, rounded up" + "." + /*TODO: ", (NYI) and the tradition can be any tradition for which you are at least trained in the related skill." +*/ "\n\n{i}(Clarification: This is a shared innate casting, rather than one of each of these spells.){/i}",
            [ModTraits.Runesmith])
            .WithOnSheet(sheet =>
            {
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            })
            .WithOnCreature(creature =>
            {
                Trait classOfOrigin = ModTraits.Runesmith;
                creature.GetOrCreateSpellcastingSource(
                    SpellcastingKind.Innate,
                    classOfOrigin,
                    Ability.Charisma,
                    Trait.Arcane) // PUBLISH: The tradition is always arcane
                    .WithSpells(
                        [SpellId.MagicWeapon, SpellId.MagicFang],
                        creature.MaximumSpellRank);
            })
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AfterYouExpendSpellcastingResources = (qfThis, action) => // Fires with innate spells too
                {
                    SpellcastingSource? spellcastingSource = action.SpellcastingSource;
                    if (spellcastingSource != null && spellcastingSource.ClassOfOrigin == ModTraits.Runesmith) // If source is Backup Runic Enhancement,
                    {
                        switch (action.SpellId)
                        {
                            case SpellId.MagicWeapon: // and if cast the weapon spell,
                            {
                                CombatAction? magicFang = spellcastingSource.Spells.FirstOrDefault(
                                    act => act.SpellId == SpellId.MagicFang); // then find the unarmed spell,
                                if (magicFang != null)
                                    spellcastingSource.Spells.Remove(magicFang); // and remove the unarmed spell.
                                break;
                            }
                            case SpellId.MagicFang: // and if cast the unarmed spell,
                                CombatAction? magicWeapon = spellcastingSource.Spells.FirstOrDefault(
                                    act => act.SpellId == SpellId.MagicWeapon); // then find the weapon spell,
                                if (magicWeapon != null)
                                    spellcastingSource.Spells.Remove(magicWeapon); // and remove the weapon spell.
                                break;
                        }
                    }
                };
            });
        ModManager.AddFeat(BackupRunicEnhancement);
        
        EngravingStrike = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatEngravingStrike", "Engraving Strike"),
            1,
            "You draw a rune onto the surface of your weapon in reverse, the mark branding or bruising itself into your target in the moment of impact.",
            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a melee weapon and {i}(due to Trace Rune){/i} have a free hand\n\nMake a melee strike with the weapon. On a success, you Trace a Rune onto the target of the Strike.",
            [ModTraits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Make a melee Strike. On a hit, Trace a Rune on the target.", qfFeat =>
            {
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee) || !item.HasTrait(Trait.Weapon))
                        return null;
                    
                    CombatAction engravingStrike = qfFeat.Owner.CreateStrike(item)
                        .WithActionCost(1)
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        {
                            if (result >= CheckResult.Success)
                            {
                                await caster.FictitiousSingleTileMove(caster.Occupies); // So that you aren't blocking the target's square during the trace await
                                await Rune.PickACreatureAndDrawARune(thisAction, caster, target); // TODO: the traceable rune dropdown description is a bit off
                            }

                            qfFeat.UsedThisTurn = true;
                        });
                    engravingStrike.Name = "Engraving Strike";
                    engravingStrike.Illustration =
                        new SideBySideIllustration(item.Illustration, RunesmithPlaytest.TraceRuneIllustration);
                    engravingStrike.Description = StrikeRules.CreateBasicStrikeDescription4(
                        engravingStrike.StrikeModifiers,
                        prologueText: "{b}Frequency{/b} once per round\n{b}Requirements{/b} {i}(Trace Rune){/i} You have a free hand\n",
                        additionalSuccessText:"Trace a Rune onto the target.",
                        additionalCriticalSuccessText:"Trace a Rune onto the target.");
                    engravingStrike.Traits.Add(Trait.Basic);
                    (engravingStrike.Target as CreatureTarget)!
                        .WithAdditionalConditionOnTargetCreature( (attacker, defender) => 
                            qfFeat.UsedThisTurn ? Usability.NotUsable("Already used this round") : Usability.Usable)
                        .WithAdditionalConditionOnTargetCreature( (attacker, defender) => 
                            attacker.HasFreeHand ? Usability.Usable : Usability.NotUsable("You must have a free hand to trace a rune"));
                    
                    return engravingStrike;
                };
            });
        ModManager.AddFeat(EngravingStrike);
        
        // TODO: this feat
        /*RemoteDetonation = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatRemoteDetonation", "Remote Detonation"),
            1,
            "You whisper an invocation over an arrow or sling bullet as you fire it, and the hissing of the missile through the air sounds just like your murmured voice.",
            "If your next action is a successful ranged Strike using physical ammunition against a target within the first range increment of your weapon, you also invoke all the runes on the target as the missile's whispering sets off the runes. If the Strike was a critical success, the target takes a –1 circumstance penalty on any saving throws against the runes invoked by your Remote Detonation.",
            [ModTraits.Invocation, ModTraits.Runesmith])
            .WithActionCost(0);
        ModManager.AddFeat(RemoteDetonation);*/
        
        // TODO: this feat
        /*RuneSinger = new TrueFeat(
                ModManager.RegisterFeatName("RunesmithPlaytest.FeatRuneSinger", "Rune-Singer"),
                1,
                "You practice the lost art of using music to guide the act of carving your runes, singing them into existence as much as crafting them.",
                /*"You can use Performance instead of Crafting when attempting Crafting checks related to runes. " + *///"Once per minute, you can Trace a Rune with song alone, removing the need to have a free hand, removing the manipulate trait from Trace Rune, and allowing you to use the 2-action version of Trace Rune as a single action. You don’t need to be able to move your hands when Tracing a Rune using song, but you do need to be able to sing in a clear voice.",
                //[ModTraits.Runesmith]);
        //ModManager.AddFeat(RuneSinger);*/
        
        /* 2nd Level Feats */
        FortifyingKnock = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatFortifyingKnock", "Fortifying Knock"),
            2,
            "Your shield is a natural canvas for your art.",
            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a shield and {i}(due to Trace Rune){/i} have a free hand\n\nIn one motion, you Raise a Shield and Trace a Rune on your shield.",
            [ModTraits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Raise a Shield and Trace a Rune on your shield.", qfFeat =>
            {
                // PETR: action into Raise Shield section
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.ItemActions)
                        return null;
                    
                    Item? shieldItem = qfThis.Owner.HeldItems.FirstOrDefault(
                        item => item.HasTrait(Trait.Shield));
                    
                    Illustration shieldIll = shieldItem?.Illustration ?? IllustrationName.SteelShield;
                    
                    // PUBLISH: mention free hand requirements interpretation of trace a rune subsidiaries.
                    CombatAction fortifyingKnockAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(shieldIll, RunesmithPlaytest.TraceRuneIllustration),
                            "Fortifying Knock",
                            [ModTraits.Runesmith, Trait.Basic],
                            "{b}Frequency{/b} once per round\n{b}Requirements{/b} {i}(Trace Rune){/i} You have a free hand\n\nIn one motion, you Raise a Shield and Trace a Rune on your shield.",
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                {
                                    string hasShieldReason = "You must have a shield equipped";
                                    string freeHandReason = "You must have a free hand to trace a rune";
                                    string usedReason = "Already used this round";
                                    return shieldItem != null ? (self.HasFreeHand ? (qfThis.UsedThisTurn ? usedReason : null) : freeHandReason) : hasShieldReason;
                                }))
                        .WithActionCost(1)
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        { 
                            // Why did I overcomplicate this? Holtrik code here in case I don't come up with a reason for why I did this the hard way.
                            /*
                             * bool hasShieldBlock = target.HasEffect(QEffectId.ShieldBlock) || target.WieldsItem(Trait.AlwaysOfferShieldBlock);
                             * target.AddQEffect(QEffect.RaisingAShield(hasShieldBlock));
                             */
                            Possibilities shieldActions = Possibilities.Create(caster)
                                .Filter( ap =>
                                {
                                    if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                                        return false;
                                    ap.CombatAction.ActionCost = 0;
                                    ap.RecalculateUsability();
                                    return true;
                                });
                            List<Option> actions = await caster.Battle.GameLoop.CreateActions(caster, shieldActions, null);
                            await caster.Battle.GameLoop.OfferOptions(caster, actions, true);
                            
                            await Rune.PickACreatureAndDrawARune(
                                thisAction,
                                caster,
                                caster,
                                runeFilter: (rune) => rune.DrawTechnicalTraits.Contains(Trait.Shield));
                        });
                    
                    return new ActionPossibility(fortifyingKnockAction, PossibilitySize.Half);
                };
            });
        ModManager.AddFeat(FortifyingKnock);
        
        // Invisible Ink // Implementation Idea: tracing a rune doesn't break stealth
        // Runic Tattoo

        SmithingWeaponsFamiliarity = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatSmithingWeaponsFamiliarity", "Smithing Weapons Familiarity"),
            2,
            "Though you are an artisan, you are well versed in using the tools of the trade to fend off enemies.",
            "You have familiarity with weapons in the hammer, pick, and knife weapon groups -- for the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.\n\n{i}(There are no weapons in the base game that this trains Runesmiths in){/i}",
            [ModTraits.Runesmith])
            .WithOnSheet(sheet =>
            {
                // Treat martials as simple
                sheet.Proficiencies.AddProficiencyAdjustment(Traits =>
                        (Traits.Contains(Trait.Hammer) || Traits.Contains(Trait.Pick) || Traits.Contains(Trait.Knife)) && Traits.Contains(Trait.Martial), Trait.Simple
                );

                // Treat advanced as martials
                sheet.Proficiencies.AddProficiencyAdjustment(Traits =>
                        (Traits.Contains(Trait.Hammer) || Traits.Contains(Trait.Pick) || Traits.Contains(Trait.Knife)) && Traits.Contains(Trait.Advanced), Trait.Martial
                );
            });
        ModManager.AddFeat(SmithingWeaponsFamiliarity);
        
        /* 4th Level Feats */
        // Artist's Attendance
        // Ghostly Resonance
        
        TerrifyingInvocation = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatTerrifyingInvocation", "Terrifying Invocation"),
            4,
            "You spit and roar as you pronounce your rune’s terrible name.",
            "You attempt to Demoralize a single target within range, and then Invoke one Rune upon the target. You can Demoralize the target as long as they are within range of your invocation, and you don’t take a penalty if the creature doesn't understand your language.",
            [ModTraits.Invocation, ModTraits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Demoralize a creature, then Invoke one Rune on them.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction scaryInvoke = new CombatAction(
                        qfThis.Owner,
                        new SideBySideIllustration(IllustrationName.Demoralize,
                            RunesmithPlaytest.InvokeRuneIllustration),
                        "Terrifying Invocation",
                        [ModTraits.Invocation, ModTraits.Runesmith, Trait.Basic],
                        "{i}You spit and roar as you pronounce your rune’s terrible name.{/i}" +
                        "You attempt to Demoralize a single target within range, and then Invoke one Rune upon the target. You can Demoralize the target as long as they are within range of your invocation, and you don’t take a penalty if the creature doesn't understand your language.",
                        Target.RangedCreature(6)
                            .WithAdditionalConditionOnTargetCreature((atk, def) =>
                            {
                                return def.QEffects.FirstOrDefault(qf => qf is DrawnRune) == null
                                    ? Usability.NotUsableOnThisCreature("not a rune-bearer")
                                    : Usability.Usable;
                            }))
                        .WithActionCost(1)
                        .WithEffectOnEachTarget( async (thisAction, caster, target, result) =>
                        {
                            CombatAction demoralize = CommonCombatActions.Demoralize(caster).WithActionCost(0);
                            QEffect tempGlare = new QEffect()
                            {
                                ExpiresAt = ExpirationCondition.Never,
                                Id = QEffectId.IntimidatingGlare,
                            };
                            caster.AddQEffect(tempGlare);
                            await caster.Battle.GameLoop.FullCast(demoralize, ChosenTargets.CreateSingleTarget(target));
                            caster.RemoveAllQEffects(qf => qf == tempGlare);
                            await Rune.PickARuneToInvokeOnTarget(thisAction, caster, target);
                        });

                    scaryInvoke = Rune.WithImmediatelyRemovesImmunity(scaryInvoke);
                    
                    return new ActionPossibility(scaryInvoke, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(TerrifyingInvocation);
        
        TransposeEtching = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatTransposeEtching", "Transpose Etching"),
            4,
            "With a pinching gesture, you pick up a word and move it elsewhere.",
            "You move any one of your runes within 30 feet to a different target within 30 feet.\n\n{i}(This can be used on traced runes, not just etched ones.){/i}",
            [Trait.Manipulate, ModTraits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Move a rune from one target to another, both within 30 feet.", qfFeat =>
            {
                // TODO: Less clicky implementation
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction transposeAction = new CombatAction(
                        qfThis.Owner,
                        RunesmithPlaytest.TransposeEtchingIllustration,
                        "Transpose Etching",
                        [Trait.Manipulate, ModTraits.Runesmith, Trait.Basic],
                        "You move any one of your runes within 30 feet to a different target within 30 feet.",
                        Target.RangedCreature(6)
                            .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                            {
                                return defender.QEffects.FirstOrDefault(qfFind => qfFind is DrawnRune && qfFind.Source == attacker) != null ?
                                        Usability.Usable : Usability.NotUsableOnThisCreature("Doesn't bear one of your runes");
                            }))
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.OminousActivation)
                        .WithEffectOnEachTarget(async (transposeAction, caster, transposeFrom, result) =>
                            {
                                /*
                                 * Go through every rune on the target to create an action option usable against their rune.
                                 */
                                List<Option> options = [];
                                foreach (DrawnRune runeQf in (transposeFrom.QEffects.Where(qfFind => qfFind is DrawnRune &&  qfFind.Source == caster)).Select(qf => qf as DrawnRune)) // Loop through all the runes the target bears
                                {
                                    Rune thisRune = runeQf.Rune;
                                    
                                    CombatAction transposeThisRune = new CombatAction( // Create an action for that rune
                                        caster,
                                        thisRune.Illustration!,
                                        "Transpose " + thisRune.Name,
                                        [Trait.Manipulate, ModTraits.Runesmith, Trait.Basic, Trait.DoNotShowInCombatLog],
                                        "You move any one of your runes within 30 feet to a different target within 30 feet.",
                                        Target.RangedCreature(6).WithAdditionalConditionOnTargetCreature( 
                                            (attacker, defender) =>
                                            {
                                                QEffect? foundQf = defender.QEffects.FirstOrDefault(
                                                    qfToFind => qfToFind == runeQf); // The target of this action must have this specific drawn rune
                                                return foundQf != null ? Usability.Usable : Usability.NotUsableOnThisCreature($"Does not have {thisRune.Name} applied");
                                            }))
                                        {
                                            Tag = thisRune,
                                        }
                                        .WithActionCost(0)
                                        .WithEffectOnEachTarget(async (thisTransposeAction, caster, transposeFrom, result) =>
                                        {
                                            List<Option> transposeToOptions = new List<Option>();
                                            foreach (Creature transposeTo in caster.Battle.AllCreatures)
                                            {
                                                if (thisRune.UsageCondition == null ||
                                                    thisRune.UsageCondition.Invoke(caster, transposeTo) != Usability.Usable)
                                                    continue;
                                                Option transposeOption = Option.ChooseCreature(
                                                    $"Apply {thisRune.Name}",
                                                    transposeTo,
                                                    async () =>
                                                    {
                                                        // Call NewDrawnRune in case it prompts for a selection, such as if the rune applies to an item, so we can move this rune to that creature AND its item.
                                                        DrawnRune pretendNewRune = (await thisRune.NewDrawnRune!.Invoke(thisTransposeAction, caster, transposeTo, thisRune))!;
                                                        await runeQf.MoveRuneToTarget(transposeTo, pretendNewRune.DrawnOn);
                                                        Sfxs.Play(SfxName.AncientDust); // TODO: Consider better SFX for Transpose Etching. Uses the same sound as drawing a rune.

                                                    })
                                                    .WithIllustration(thisRune.Illustration!);
                                                transposeToOptions.Add(transposeOption);
                                            }
                            
                                            if (transposeToOptions.Count <= 0)
                                                return; // End if no options.
                                            transposeToOptions.Add(new CancelOption(true)); // allow us to cancel it.
                                            transposeToOptions.Add(new PassViaButtonOption(" Confirm no transposition "));
                        
                                            // Await which option to take.
                                            Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
                                                new AdvancedRequest(caster, "Choose a rune to transpose.", transposeToOptions)
                                                {
                                                    TopBarText = "Choose target to transpose this rune to, or right-click to cancel.",
                                                    TopBarIcon = thisTransposeAction.Illustration,
                                                })).ChosenOption;

                                            switch (chosenOption)
                                            {
                                                case CreatureOption creatureOption:
                                                    break;
                                                case CancelOption:
                                                    thisTransposeAction.RevertRequested = true;
                                                    return;
                                                case PassViaButtonOption:
                                                    return;
                                            }

                                            await chosenOption.Action();
                                        });
                                    GameLoop.AddDirectUsageOnCreatureOptions(transposeThisRune, options);
                                }
                                
                                if (options.Count <= 0)
                                    return; // End if no options.
                                options.Add(new CancelOption(true)); // allow us to cancel it.
                                options.Add(new PassViaButtonOption(" Confirm no transposition "));
                            
                                // Await which option to take.
                                Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
                                    new AdvancedRequest(caster, "Choose a rune to transpose.", options)
                                    {
                                        TopBarText = "Choose a rune to transpose, or right-click to cancel.",
                                        TopBarIcon = transposeAction.Illustration,
                                    })).ChosenOption;

                                switch (chosenOption)
                                {
                                    case CreatureOption creatureOption:
                                        break;
                                    case CancelOption:
                                        transposeAction.RevertRequested = true;
                                        return;
                                    case PassViaButtonOption:
                                        return;
                                }

                                await chosenOption.Action();
                            });
                    return new ActionPossibility(transposeAction, PossibilitySize.Half);
                };
            });
        ModManager.AddFeat(TransposeEtching);
        
        /* 6th Level Feats */
        // Runic Reprisal
        // Tracing Trance
        // Vital Composite Invocation
        // Words, Fly Free
        
        /* 8th Level Feats */
        // Drawn In Red
        // Elemental Revision
        // Read The Bones
        
        // TODO: If 2 feats per level, remove these
        bool isDebug = true;
        if (isDebug)
        {
            for (int i = 1; i < 5; i++)
            {
                Feat DebugNoneFeat = new TrueFeat(
                    ModManager.RegisterFeatName("RunesmithDebugNoneFeat"+i, "No Class Feat "+i),
                    1,
                    "When wasting your time becomes a passion,",
                    "Then nothing is impossible for you.",
                    [ModTraits.Runesmith],
                    null);
                ModManager.AddFeat(DebugNoneFeat);
            }
        }
    }
}