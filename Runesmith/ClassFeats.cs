using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
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
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class ClassFeats
{
    // 1sts
    public static Feat? BackupRunicEnhancement;
    public static Feat? EngravingStrike;
    public static Feat? RemoteDetonation;
    public static Feat? RuneSinger;
    // 2nds
    public static Feat? FortifyingKnock;
    public static Feat? InvisibleInk;
    public static Feat? RunicTattoo;
    public static Feat? SmithingWeaponsFamiliarity;
    // 4ths
    public static Feat? ArtistsAttendance;
    public static Feat? GhostlyResonance;
    public static Feat? TerrifyingInvocation;
    public static Feat? TransposeEtching;
    // 6ths
    public static Feat? RunicReprisal;
    public static Feat? TracingTrance;
    public static Feat? VitalCompositeInvocation;
    public static Feat? WordsFlyFree;
    // 8ths
    public static Feat? DrawnInRed;
    public static Feat? ElementalRevision;
    public static Feat? ReadTheBones;
    
    // TODO: More enums for stuff, per Sudo's recommendations.
    /* public static readonly QEffectId HitTheDirt = ModManager.RegisterEnumMember<QEffectId>("Hit the Dirt QEID");
    caster.AddQEffect(new QEffect() { Id = HitTheDirt; }
    caster.RemoveAllQEffects(qe => qe.Id == HitTheDirt);*/
    // Dinglebob:
    // public readonly static QEffectId FormID = ModManager.RegisterEnumMember<QEffectId>("ShifterForm");
    
    public static void CreateFeats()
    {
        /////////////////////
        // 1st Level Feats //
        /////////////////////
        BackupRunicEnhancement = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatBackupRunicEnhancement", "Backup Runic Enhancement"),
            1,
            "While you are not a spellcaster, you have a working knowledge of the most fundamental of runic magic.",
            "Once per day, you can cast your choice of either " + AllSpells.CreateModernSpellTemplate(SpellId.MagicFang, Trait.Wizard).ToSpellLink().Replace("magic fang", "runic body") + " or " + AllSpells.CreateModernSpellTemplate(SpellId.MagicWeapon, Trait.Wizard).ToSpellLink().Replace("magic weapon", "runic weapon") + " as an innate spell. The rank of these spells is equal to half your level, rounded up" + "." + /*TODO: ", (NYI) and the tradition can be any tradition for which you are at least trained in the related skill." +*/ "\n\n"+new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString+" {b}Reminder{/b} You choose which one to cast. You cannot cast both in a day.",
            [Enums.Traits.Runesmith])
            .WithOnSheet(sheet =>
            {
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            })
            .WithOnCreature(creature =>
            {
                Trait classOfOrigin = Enums.Traits.Runesmith;
                creature.GetOrCreateSpellcastingSource(
                    SpellcastingKind.Innate,
                    classOfOrigin,
                    Ability.Charisma,
                    Trait.Arcane) // Ease of implementation, the tradition is always arcane
                    .WithSpells(
                        [SpellId.MagicWeapon, SpellId.MagicFang],
                        creature.MaximumSpellRank);
            })
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AfterYouExpendSpellcastingResources = (qfThis, action) => // Fires with innate spells too
                {
                    SpellcastingSource? spellcastingSource = action.SpellcastingSource;
                    if (spellcastingSource != null && spellcastingSource.ClassOfOrigin == Enums.Traits.Runesmith) // If source is Backup Runic Enhancement,
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
            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a melee weapon and {i}(due to Trace Rune){/i} have a free hand\n\nMake a melee Strike with the weapon. On a success, you {tooltip:Runesmith.Action.TraceRune}Trace a Rune{/} onto the target of the Strike.\n\n"+new ModdedIllustration(Enums.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Playtest Ruling{/b} You can Trace runes that draw onto the target's equipment, not just the creature itself.",
            [Enums.Traits.Runesmith])
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
                                await Rune.PickACreatureAndDrawARune(thisAction, caster, (filterTarget => filterTarget == target));
                            }

                            qfFeat.UsedThisTurn = true;
                        });
                    engravingStrike.Name = "Engraving Strike";
                    engravingStrike.Illustration =
                        new SideBySideIllustration(item.Illustration, Enums.Illustrations.TraceRune);
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
                            attacker.HasFreeHand || attacker.HeldItems.Any(item => item.HasTrait(Enums.Traits.CountsAsRunesmithFreeHand)) ? Usability.Usable : Usability.NotUsable("You must have a free hand to trace a rune"));
                    
                    return engravingStrike;
                };
            });
        ModManager.AddFeat(EngravingStrike);
        
        RemoteDetonation = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatRemoteDetonation", "Remote Detonation"),
            1,
            "You whisper an invocation over an arrow or sling bullet as you fire it, and the hissing of the missile through the air sounds just like your murmured voice.",
            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a ranged weapon\n\nMake a ranged Strike against a target within the weapon's first range increment" /*+ " using physical ammunition"*/ + ". On a success, you invoke all the runes on the target as the missile's whispering sets off the runes. On a critical success, the target also takes a –1 circumstance penalty on any saving throws against the runes invoked by your Remote Detonation.",
            [Enums.Traits.Invocation, Enums.Traits.Runesmith, Trait.Spell])
            .WithActionCost(1)
            .WithPermanentQEffect("Make a ranged Strike. On a hit, invokes all runes on the target. On a crit, it also takes a -1 circumstance penalty to its saving throws against these invocations.", qfFeat =>
            {
                Illustration remoteDetIllustration = new SideBySideIllustration(IllustrationName.LooseTimesArrow,
                    Enums.Illustrations.InvokeRune);

                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Weapon) || !item.HasTrait(Trait.Ranged) || item.WeaponProperties == null || item.WeaponProperties.RangeIncrement == -1)
                        return null;

                    CombatAction remoteDet = qfFeat.Owner.CreateStrike(item).WithActionCost(1);
                    remoteDet.Name = "Remote Detonation";
                    remoteDet.Illustration =
                        new SideBySideIllustration(item.Illustration, Enums.Illustrations.InvokeRune);
                    remoteDet.Traits.Add(Enums.Traits.Invocation);
                    remoteDet.Traits.Add(Enums.Traits.Runesmith);
                    remoteDet.Description =
                        StrikeRules.CreateBasicStrikeDescription4(remoteDet.StrikeModifiers, prologueText:"{b}Frequency{/b} once per round", additionalSuccessText: " Invoke all runes on the target.", additionalCriticalSuccessText: " The target also has a -1 circumstance penalty to any saving throws against these invocations.");
                    (remoteDet.Target as CreatureTarget)!.WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                    {
                        if (qfFeat.UsedThisTurn)
                            return Usability.CommonReasons.AlreadyUsedThisAbilityThisTurn;
                        if (attacker.DistanceTo(defender) <= item.WeaponProperties.RangeIncrement)
                            return Usability.Usable;
                        return Usability.CommonReasons.TargetOutOfRange;
                    });
                    remoteDet.WithEffectOnEachTarget( async (remoteDetAction, caster, target, result) =>
                    {
                        qfFeat.UsedThisTurn = true;

                        if (result < CheckResult.Success)
                            return;
                        
                        if (result == CheckResult.CriticalSuccess)
                        {
                            QEffect detPenalty = new QEffect()
                            {
                                Name = "Remote Detonation Critical Success",
                                ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn,
                                BonusToDefenses = (detPenaltyQf, invokeAction, defense) =>
                                {
                                    if (invokeAction != null &&
                                        invokeAction.HasTrait(Enums.Traits.Invocation) &&
                                        defense.IsSavingThrow())
                                    {
                                        return new Bonus(-1, BonusType.Circumstance,
                                            "Remote Detonation Critical Success");
                                    }

                                    return null;
                                },
                            };
                            
                            target.AddQEffect(detPenalty);
                        }

                        List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(caster, target);
                        foreach (DrawnRune drawnRune in drawnRunes)
                        {
                            CombatAction? invokeThisRune = drawnRune.Rune.CreateInvokeAction(remoteDetAction, caster, drawnRune, (int)item.WeaponProperties.RangeIncrement);
                            if (invokeThisRune != null)
                                await caster.Battle.GameLoop.FullCast(invokeThisRune, ChosenTargets.CreateSingleTarget(target));
                        }
                        
                        //Rune.RemoveAllImmunities(target);

                        target.RemoveAllQEffects(qf => qf.Name == "Remote Detonation Critical Success");
                    });

                    Rune.WithImmediatelyRemovesImmunity(remoteDet);

                    return remoteDet;
                };
            });
        ModManager.AddFeat(RemoteDetonation);
        
        // TODO: Adjust implementation to work with subsidiary Trace Runes?
        RuneSinger = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatRuneSinger", "Rune-Singer"),
            1,
            "You practice the lost art of using music to guide the act of carving your runes, singing them into existence as much as crafting them.",
            /*"You can use Performance instead of Crafting when attempting Crafting checks related to runes. " + */"Once per minute, you can {tooltip:Runesmith.Action.TraceRune}Trace a Rune{/} with song alone, removing the need to have a free hand, removing the manipulate trait from Trace Rune, and allowing you to use the {icon:TwoActions} 2-action version of Trace Rune as a single {icon:Action} action. You don't need to be able to move your hands when Tracing a Rune using song, but you do need to be able to sing in a clear voice.",
            [Enums.Traits.Runesmith])
            .WithPermanentQEffect("Once per combat, you can Trace a Rune without a free hand on a target up to 30 feet away.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains("Rune-Singer"))
                            return null;

                        Rune? foundRune = null;
                        foreach (Rune rune in RunesmithClassRunes.AllRunes)
                        {
                            if (section.Name == rune.Name)
                                foundRune = rune;
                        }

                        if (foundRune is null)
                            return null;

                        CombatAction runeSingerAction = foundRune.CreateTraceAction(qfThis.Owner, 2)
                            .WithActionCost(1)
                            .WithExtraTrait(Trait.Basic);
                        runeSingerAction.Name = runeSingerAction.Name.Remove(0, "Trace".Length).Insert(0, "Sing");
                        runeSingerAction.Traits.Remove(Trait.Manipulate);
                        // Manually recreate target to remove the free hand requirement
                        CreatureTarget newTarget = Target.RangedCreature(6);
                        if (foundRune.UsageCondition != null)
                            newTarget = newTarget.WithAdditionalConditionOnTargetCreature(foundRune.UsageCondition);
                        runeSingerAction.Target = newTarget;
                        runeSingerAction.Description =
                            foundRune.CreateTraceActionDescription(runeSingerAction, prologueText:"{Blue}{b}Range{/b} 30 feet{/Blue}\n", withFlavorText: false, afterFlavorText:"{Blue}{b}Frequency{/b} once per combat{/Blue}");
                        runeSingerAction.WithEffectOnSelf(self => { self.PersistentUsedUpResources.UsedUpActions.Add("Rune-Singer"); });
                        
                        ActionPossibility singPossibility = new ActionPossibility(runeSingerAction)
                        {
                            Caption = "Rune-Singer",
                            Illustration = new SideBySideIllustration(IllustrationName.Action,
                                Enums.Illustrations.RuneSinger)
                        };
                        
                        return singPossibility;
                    };
                });
        ModManager.AddFeat(RuneSinger);
        
        /////////////////////
        // 2nd Level Feats //
        /////////////////////
        FortifyingKnock = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatFortifyingKnock", "Fortifying Knock"),
            2,
            "Your shield is a natural canvas for your art.",
            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a shield and {i}(due to Trace Rune){/i} have a free hand\n\nIn one motion, you Raise a Shield and {tooltip:Runesmith.Action.TraceRune}Trace a Rune{/} on your shield.",
            [Enums.Traits.Runesmith, Trait.Spell])
            .WithActionCost(1)
            .WithPrerequisite(FeatName.ShieldBlock, "Shield Block")
            .WithPermanentQEffect("Raise a Shield and Trace a Rune on your shield.", qfFeat =>
            {
                // PETR: action into Raise Shield section
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.ItemActions)
                        return null;
                    
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(qfThis.Owner);
                    
                    if (repertoire == null)
                        return null;
                    
                    Item? shieldItem = qfThis.Owner.HeldItems.FirstOrDefault(
                        item => item.HasTrait(Trait.Shield));
                    Illustration shieldIll = shieldItem?.Illustration ?? IllustrationName.SteelShield;

                    PossibilitySection fortSection = new PossibilitySection("Fortifying Knock");
                    PossibilitySection repriseSection = new PossibilitySection("Runic Reprisal");

                    // Generate fortifying knock and runic reprisal options
                    foreach (Rune rune in repertoire.GetRunesKnown(qfThis.Owner))
                    {
                        bool validReprisal = qfThis.Owner.HasFeat(RunicReprisal!.FeatName) &&
                                             rune.InvokeTechnicalTraits.Contains(Trait.IsHostile);
                        bool validKnock = rune.DrawTechnicalTraits.Contains(Trait.Shield);// &&
                                          //(rune.UsageCondition == null || rune.UsageCondition.Invoke(qfThis.Owner, qfThis.Owner) == Usability.Usable);
                        if (!validReprisal && !validKnock)
                            continue;

                        // Disable this rune if it isn't a proper shield rune.
                        bool isDisabledRune = !rune.DrawTechnicalTraits.Contains(Trait.Shield);
                        
                        CombatAction knockThisRune = rune.CreateTraceAction(qfThis.Owner, 1)
                            .WithExtraTrait(Trait.DoesNotProvoke) // Provoke manually later
                            .WithExtraTrait(Trait.DoNotShowInCombatLog); // Too much text spam.
                        knockThisRune.Name = $"Knock {rune.Name}";
                        knockThisRune.Illustration = new SideBySideIllustration(shieldIll, rune.Illustration);
                        knockThisRune.Description = rune.CreateTraceActionDescription(
                            knockThisRune,
                            withFlavorText: false,
                            afterFlavorText:"{b}Frequency{/b} once per round");
                        if (isDisabledRune)
                        {
                            string oldPassiveText = rune.PassiveTextWithHeightening(rune, knockThisRune.Owner.Level);
                            int start = knockThisRune.Description.IndexOf(oldPassiveText);
                            if (start != -1)
                                knockThisRune.Description = knockThisRune.Description.Replace(oldPassiveText, "{Blue}(Runic Reprisal) When you use Shield Block against an adjacent attacker, this rune's invocation effects are detonated outward onto the attacker.{/Blue}");
                        }
                        knockThisRune.Target = Target.Self()
                            .WithAdditionalRestriction(self =>
                            {
                                string hasShieldReason = "You must have a shield equipped";
                                string freeHandReason = "You must have a free hand to trace a rune";
                                string usedReason = "Already used this round";
                                return shieldItem != null
                                    ? (self.HasFreeHand || self.HeldItems.Any(item => item.HasTrait(Enums.Traits.CountsAsRunesmithFreeHand)) ? (qfThis.UsedThisTurn ? usedReason : null) : freeHandReason)
                                    : hasShieldReason;
                            });
                        knockThisRune.EffectOnOneTarget = null; // Reset behavior so we can hard code this
                        knockThisRune.WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                            {
                                // Raise a shield
                                Possibilities shieldActions = Possibilities.Create(caster)
                                    .Filter( ap =>
                                    {
                                        if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                                            return false;
                                        ap.CombatAction.ActionCost = 0;
                                        ap.CombatAction.WithExtraTrait(Trait.DoNotShowOverheadOfActionName); // Too much text spam.
                                        ap.RecalculateUsability();
                                        return true;
                                    });
                                List<Option> actions = await caster.Battle.GameLoop.CreateActions(caster, shieldActions, null);
                                await caster.Battle.GameLoop.OfferOptions(caster, actions, true);
                                
                                // Provoke manipulate
                                await CombatAction.CreateSimple(caster, $"Trace {rune.Name}", Trait.DoNotShowInCombatLog,
                                    Trait.Manipulate).WithActionCost(0).AllExecute();
                                
                                // Trace the rune
                                Rune actionRune = (thisAction.Tag as Rune)!;
                                if (await actionRune.DrawRuneOnTarget(thisAction, caster, target, true) is { } drawnRune)
                                {
                                    if (isDisabledRune)
                                    {
                                        drawnRune.DisableRune(true);
                                        drawnRune.Description = "{Blue}(Runic Reprisal) When you use Shield Block against an adjacent attacker, this rune's invocation effects are detonated outward onto the attacker.{/Blue}" + "\n\n{i}{Blue}Traced: lasts until the end of " + drawnRune.Source?.Name + "'s next turn.{/Blue}{/i}";
                                    }
                                    
                                    // If it didn't have it already (because idk my own code anymore),
                                    // it needs to know there's an action with "Knock" in the name that traced it.
                                    drawnRune.SourceAction = thisAction;
                                }
                                else
                                    thisAction.RevertRequested = true;

                                qfThis.UsedThisTurn = true;
                            });

                        if (isDisabledRune)
                        {
                            repriseSection.AddPossibility(new ActionPossibility(knockThisRune, PossibilitySize.Full));
                        }
                        else
                        {
                            fortSection.AddPossibility(new ActionPossibility(knockThisRune, PossibilitySize.Full));
                        }
                    }
                    
                    SubmenuPossibility fortifyingKnockSubmenu = new SubmenuPossibility(
                        new SideBySideIllustration(shieldIll, Enums.Illustrations.TraceRune),
                        "Fortifying Knock",
                        PossibilitySize.Half)
                    {
                        SpellIfAny = new CombatAction(qfThis.Owner, new SideBySideIllustration(shieldIll, Enums.Illustrations.TraceRune), "Fortifying Knock", [Enums.Traits.Runesmith], "{i}Your shield is a natural canvas for your art.{/i}\n\n"+
                            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a shield and {i}(due to Trace Rune){/i} have a free hand\n\nIn one motion, you Raise a Shield and Trace a Rune on your shield.", Target.Self()).WithActionCost(1), // This doesn't DO anything, it's just to provide description to the menu.
                        Subsections = { fortSection, repriseSection },
                    };
                    
                    return fortifyingKnockSubmenu;
                };
            });
        ModManager.AddFeat(FortifyingKnock);
        
        InvisibleInk = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatInvisibleInk", "Invisible Ink"),
            2,
            "When your rune is drawn, it leaves only the barest mark.",
            "You no longer cease being hidden when you {tooltip:Runesmith.Action.TraceRune}Trace a Rune{/}.",
            [Enums.Traits.Runesmith])
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (action.HasTrait(Enums.Traits.Traced) || action.HasTrait(Enums.Traits.Etched))
                        action.WithExtraTrait(Trait.DoesNotBreakStealth);
                };
            });
        ModManager.AddFeat(InvisibleInk);
        
        // Runic Tattoo
        // TODO: Let you retrain for future learned runes.
        RunicTattoo = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatRunicTattoo", "Runic Tattoo"),
            2,
            "Drawing your favorite rune in your flesh, you know you'll never be without it.",
            "Choose one rune you know, which you apply as a tattoo to your body. The rune is etched at the beginning of combat and doesn't count toward your maximum limit of etched runes. You can invoke this rune like any of your other runes, but once invoked, the rune fades significantly and is drained of power until your next daily preparations.\n\n{b}Special{/b} {Red}{b}(NYI){/b}{/Red} You can retrain this feat to select any runes you learned at higher levels.\n\n"+new ModdedIllustration(Enums.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Implementation{/b} At level 6, the feat {tooltip:Runesmith.Feats.WordsFlyFree}Words, Fly Free{/} offers a way to offensively use the available tattoo options that would otherwise be detrimental to yourself.",
            [Enums.Traits.Runesmith],
            []);
        foreach (RuneFeat runeFeat in RunesmithClassRunes.AllRuneFeats)
        {
            Feat tattooFeat = new Feat(
                ModManager.RegisterFeatName("RunesmithPlaytest.FeatRunicTattoo"+runeFeat.Rune.RuneId.ToStringOrTechnical(), runeFeat.Name),
                runeFeat.FlavorText,
                runeFeat.RulesText,
                runeFeat.Traits,
                null)
                .WithTag(runeFeat.Rune)
                .WithIllustration(runeFeat.Illustration!)
                .WithPrerequisite(values =>
                {
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values);
                    return repertoire != null;
                },
                    "You must have a runic repertoire.")
                .WithPrerequisite(values =>
                {
                    RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnSheet(values);
                    
                    // TODO: Future update
                    /*
                    // Getting this to work has been incredibly difficult, with low native documentation,
                    // so I'm sorting this out into something I can understand even if it seems redundant.
                    
                    // All: the level at which the feat was acquired.
                    int levelAtFeatAcquisition = values.CurrentLevel;
                    
                    // Free Encounter Mode: value seems to be the highest seen sheet level at any point after loading the game, even if you select a different level.
                    // Campaign Mode (retraining): value seems to be the feat acquisition level.
                    int persistMaxLevel = values.Sheet.MaximumLevel;
                    
                    // Free Encounter Mode: value is the current dropdown level selection
                    // Campaign Mode (retraining): value is -1
                    int freePlayEditorLevel = values.Sheet.EditingInventoryAtLevel; // The level in the inspector for freeplay. ==-1 in campaign play.
                    
                    int levelsToSearch = -1;
                    if (freePlayEditorLevel != -1) // Free Encounter Mode
                    {
                        levelsToSearch = Math.Max(levelAtFeatAcquisition, freePlayEditorLevel);
                    }
                    else if (CampaignState.Instance != null) // Campaign Mode
                    {
                        levelsToSearch = Math.Max(levelAtFeatAcquisition, CampaignState.Instance.CurrentLevel);
                    }
                    
                    // Create self using a higher level
                    Creature maximumLevelSelf = values.Sheet.ToCreature(levelsToSearch);
                    
                    // Use the higher level self's calculated sheet.
                    return repertoire != null && repertoire.KnowsRune(maximumLevelSelf, runeFeat.Rune);
                    */
                    return repertoire != null && repertoire.KnowsRune(values, runeFeat.Rune);
                },
                "You must know this rune at this level.") //"You must know this rune (even at later levels).")
                .WithPermanentQEffect("You have a rune tattooed on yourself. If invoked, this rune will be inactive until your next daily preparations.",
                    qfFeat =>
                    {
                        Creature runesmith = qfFeat.Owner;
                        qfFeat.Name = $"Runic Tattoo ({runeFeat.Rune.Name})";
                        qfFeat.StartOfCombat = async qfThis =>
                        {
                            if (runesmith.PersistentUsedUpResources.UsedUpActions.Contains("RunicTattoo"))
                                return;

                            if (qfThis.UsedThisTurn)
                                return;

                            // Etch that rune at the start of combat
                            CombatAction etchTattoo = runeFeat.Rune.CreateEtchAction(runesmith);
                            etchTattoo.Name = "Runic Tattoo";
                            DrawnRune? appliedRune = await runeFeat.Rune.DrawRuneOnTarget(
                                CombatAction.CreateSimple(
                                    runesmith,
                                    $"Runic Tattoo ({runeFeat.Rune.Name})",
                                    [Enums.Traits.Etched]),
                                runesmith,
                                runesmith,
                                true); //runesmith.Battle.GameLoop.FullCast(etchTattoo, ChosenTargets.CreateSingleTarget(runesmith));

                            if (appliedRune != null)
                            {
                                qfThis.Tag = appliedRune;
                                appliedRune.AfterInvokingRune = async (thisDrawnRune, invokedRune) =>
                                {
                                    if (invokedRune == thisDrawnRune)
                                        invokedRune.Owner.PersistentUsedUpResources.UsedUpActions.Add("RunicTattoo");
                                };
                                appliedRune.Description = appliedRune.Description!.Replace(
                                    "Etched: lasts until the end of combat.",
                                    "Tattooed: lasts until the end of combat. If invoked, this rune won't be available until your next daily preparations.");
                            }

                            qfThis.UsedThisTurn = true;
                        };
                    });
            RunicTattoo.Subfeats?.Add(tattooFeat);
        }
        ModManager.AddFeat(RunicTattoo);

        SmithingWeaponsFamiliarity = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatSmithingWeaponsFamiliarity", "Smithing Weapons Familiarity"),
            2,
            "Though you are an artisan, you are well versed in using the tools of the trade to fend off enemies.",
            "You have familiarity with weapons in the hammer, pick, and knife weapon groups -- for the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.\n\n"+new ModdedIllustration(Enums.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Modding{/b} Other mods which add advanced weapons are required to benefit from this feat.",
            [Enums.Traits.Runesmith])
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
        
        /////////////////////
        // 4th Level Feats //
        /////////////////////
        ArtistsAttendance = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatArtistsAttendance", "Artist's Attendance"),
            4,
            "Your runes call you to better attend to your art.",
            "{b}Frequency{/b} once per round\n\nStride twice. If you end your movement within reach of a creature that is bearing one of your runes, you can {tooltip:Runesmith.Action.TraceRune}Trace a Rune{/} upon any creature adjacent to you (even a different creature).\n\n"+new ModdedIllustration(Enums.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Playtest Ruling{/b} You can also be a rune-bearer within your reach, and your reach can be based on a weapon or unarmed attack with the Reach trait. The Trace target must still be adjacent.",
            [Enums.Traits.Runesmith])
            .WithActionCost(2)
            .WithPermanentQEffect("Stride twice towards a rune-bearing creature, then Trace a Rune upon {b}any{/b} adjacent creature.",
            qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfFeat.UsedThisTurn)
                        return null;

                    CombatAction attendAction = new CombatAction(
                        qfThis.Owner,
                        new SideBySideIllustration(IllustrationName.FleetStep, Enums.Illustrations.TraceRune),
                        "Artist's Attendance",
                        [Enums.Traits.Runesmith, Trait.Basic],
                        "{i}Your runes call you to better attend to your art.{/i}\n\n{b}Frequency{/b} once per round\n\nStride twice. If you end your movement within reach of a creature that is bearing one of your runes, you can Trace a Rune upon any creature adjacent to you (even a different creature).",
                        Target.Self())
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        {
                            if (!await caster.StrideAsync("Choose where to Stride with Artist's Attendance. (1/2)"))
                                thisAction.RevertRequested = true;
                            else
                            {
                                await caster.StrideAsync(
                                    "Choose where to Stride with Artist's Attendance. You should end your movement within reach of a rune-bearer. (2/2)",
                                    allowPass: true);
                                
                                // Find highest reach
                                int reach = 1;
                                foreach (Item heldItem in caster.HeldItems)
                                {
                                    if (heldItem.HasTrait(Trait.Reach))
                                        reach = 2;
                                }

                                if (caster.UnarmedStrike.HasTrait(Trait.Reach))
                                    reach = 2;
                                
                                foreach (QEffect qf in caster.QEffects.Where(qf => qf.AdditionalUnarmedStrike != null))
                                {
                                    if (qf.AdditionalUnarmedStrike!.HasTrait(Trait.Reach))
                                        reach = 2;
                                }
                                
                                foreach (Creature cr in caster.Battle.AllCreatures.Where(c => c.DistanceTo(caster) <= reach))
                                {
                                    if (DrawnRune.GetDrawnRunes(caster, cr).Count() > 0)
                                    {
                                        await Rune.PickACreatureAndDrawARune(thisAction, caster, (filterTarget => filterTarget != caster), 1);
                                        break;
                                    }
                                }
                                qfThis.UsedThisTurn = true;
                            }
                        });

                    return new ActionPossibility(attendAction, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(ArtistsAttendance);
        
        // TODO: item tooltip
        GhostlyResonance = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatGhostlyResonance", "Ghostly Resonance"),
            4,
            "Your runes can not only draw power from the world of the spirits, but they can let even the most mundane objects harm spiritual beings as well.",
            "Any ally, or any items your allies wield, which bears one of your divine or occult runes gains the benefits of a ghost touch rune for as long as they are bearing your rune.\n\n"+new ModdedIllustration(Enums.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Implementation{/b} Any rune which lacks a tradition trait (Arcane, Divine, Primal, or Occult) is considered Divine if you're trained in Religion, or Occult if you're trained in Occultism, or both.",
            [Enums.Traits.Runesmith])
            .WithPermanentQEffect("Your divine and occult runes grant the benefits of a ghost touch rune to allied creatures or items.",
            qfFeat =>
            {
                Trait traitToUse = Trait.GhostTouch;
                
                qfFeat.Tag = new List<IEnumerable<Trait>>(); // trait lists that were given a GhostTouch by this feat.
                qfFeat.AddGrantingOfTechnical(
                    creature =>
                    {
                        // Don't help enemies in any way at any runtime, not even if they pick up your friend's weapon.
                        if (!creature.FriendOf(qfFeat.Owner)) 
                            return false;
                        
                        List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(qfFeat.Owner, creature);
                        foreach (DrawnRune drawnRune in drawnRunes)
                        {
                            if (drawnRune.Traits.Any(trait => trait is Trait.Arcane or Trait.Primal)) // Ignore QFs that have an incorrect trait.
                                continue;
                            if (drawnRune.Traits.Any(trait => trait is Trait.Divine or Trait.Occult)) // DrawnRune is correct trait
                                return true;
                            if (qfFeat.Owner.Skills.IsTrained(Skill.Religion) || qfFeat.Owner.Skills.IsTrained(Skill.Occultism)) // DrawnRune could still be divine or occult
                                return true;
                        }

                        return false; // no requisite DrawnRunes found.
                    },
                    effect =>
                    {
                        effect.ExpiresAt = ExpirationCondition.Never;
                        
                        effect.YouBeginAction = async (qfThis, action) =>
                        {
                            // Ghost Touch only benefits Strikes and Strike damage.
                            if (!action.HasTrait(Trait.Strike))
                                return;
                            
                            // Ensure this is being buffed by one of your runes.
                            List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(qfFeat.Owner, qfThis.Owner);
                            bool canBuffThis = false;
                            foreach (DrawnRune drawnRune in drawnRunes)
                            {
                                // Can't be a diacritic because the rune would be the rune-bearer, not the creature or item.
                                if (drawnRune.DrawnOn is DrawnRune)
                                    continue;
                                
                                // Is drawn on an item
                                if (drawnRune.DrawnOn is Item itemTarget && action.Item != null)
                                {
                                    if (itemTarget == action.Item 
                                        || (itemTarget.HasTrait(Trait.Unarmed) && action.Item.HasTrait(Trait.Unarmed)))
                                        canBuffThis = true;
                                }
                                else // Is drawn on the creature itself. TODO: Make non-item runes actually use a Creature DrawnOn so that this can reference that instead of treating it as null==Creature.
                                {
                                    canBuffThis = true;
                                }
                            }
                            if (!canBuffThis) return;
                            
                            // Go ahead and buff it.
                            if (qfFeat.Tag is not List<IEnumerable<Trait>> tagList)
                                return;
                            if (action.Item != null && !tagList.Contains(action.Item.Traits)) // Only add once.
                            {
                                action.Item.Traits.Add(traitToUse);
                                tagList.Add(action.Item.Traits);
                            }
                            if (!tagList.Contains(action.Traits)) // Only add once.
                            {
                                action.Traits.Add(traitToUse);
                                tagList.Add(action.Traits);
                            }
                        };

                        effect.AfterYouTakeAction = async (caster, action) => { effect.ExpiresAt = ExpirationCondition.Ephemeral; };
                        
                        effect.WhenExpires = async qfThis =>
                        {
                            if (qfFeat.Tag is not List<IEnumerable<Trait>> tagList) return;
                            foreach (IEnumerable<Trait> traitList in tagList)
                            {
                                switch (traitList) // Removes only once; shouldn't remove Ghost Touch from other sources.
                                {
                                    case List<Trait> list:
                                        list.Remove(traitToUse);
                                        break;
                                    case Traits list:
                                        list.Remove(traitToUse);
                                        break;
                                }
                            }
                            tagList.Clear();
                        };
                    }
                );
            });
        ModManager.AddFeat(GhostlyResonance);
        
        TerrifyingInvocation = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatTerrifyingInvocation", "Terrifying Invocation"),
            4,
            "You spit and roar as you pronounce your rune's terrible name.",
            "You attempt to Demoralize a single target within range, and then {tooltip:Runesmith.Action.InvokeRune}Invoke one Rune{/} upon the target. You can Demoralize the target as long as they are within range of your invocation, and you don't take a penalty if the creature doesn't understand your language.",
            [Enums.Traits.Invocation, Enums.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Demoralize a creature, then Invoke one Rune on them.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction scaryInvoke = new CombatAction(
                        qfThis.Owner,
                        new SideBySideIllustration(IllustrationName.Demoralize,
                            Enums.Illustrations.InvokeRune),
                        "Terrifying Invocation",
                        [Enums.Traits.Invocation, Enums.Traits.Runesmith, Trait.Spell, Trait.Basic],
                        "{i}You spit and roar as you pronounce your rune’s terrible name.{/i}\n\n" +
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
            "You move any one of your runes within 30 feet to a different target within 30 feet.\n\n"+new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString+" {b}Reminder{/b} Despite the name, this can be used on traced runes, not just etched ones.",
            [Trait.Manipulate, Enums.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Move a rune from one target to another, both within 30 feet.", qfFeat =>
            {
                // TODO: Less clicky implementation
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction transposeAction = new CombatAction(
                        qfThis.Owner,
                        Enums.Illustrations.TransposeEtching,
                        "Transpose Etching",
                        [Trait.Manipulate, Enums.Traits.Runesmith, Trait.Spell, Trait.Basic],
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
                                foreach (DrawnRune? runeQf in (transposeFrom.QEffects.Where(qfFind => qfFind is DrawnRune && qfFind.Source == caster)).Select(qf => qf as DrawnRune)) // Loop through all the runes the target bears
                                {
                                    Rune thisRune = runeQf!.Rune;
                                    
                                    CombatAction transposeThisRune = new CombatAction( // Create an action for that rune
                                        caster,
                                        thisRune.Illustration!,
                                        "Transpose " + thisRune.Name,
                                        [Trait.Manipulate, Enums.Traits.Runesmith, Trait.Basic, Trait.DoNotShowInCombatLog],
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
                                                        Sfxs.Play(SfxName.GaleBlast);

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
        
        /////////////////////
        // 6th Level Feats //
        /////////////////////
        RunicReprisal = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatRunicReprisal", "Runic Reprisal"),
            6,
            "When you raise your shield, you bury a runic trap into it, to be set off by the clash of an enemy weapon.",
            "When you use {tooltip:Runesmith.Feats.FortifyingKnock}Fortifying Knock {icon:Action}{/}, you can trace a damaging rune on your shield, even if it could not normally be applied to a shield. The traced rune doesn't have its normal effect, instead fading into your shield. If you Shield Block {icon:Reaction} with the shield against an adjacent target, you can {tooltip:Runesmith.Action.InvokeRune}Invoke the Rune{/} as part of the reaction, causing the rune to detonate outwards and apply its invocation effect to the attacking creature.",
            [Enums.Traits.Invocation, Enums.Traits.Runesmith])
            .WithPrerequisite(FortifyingKnock.FeatName, "Fortifying Knock")
            .WithPermanentQEffect("You can use Fortifying Knock with damaging runes. You invoke the rune on your attacker when you Shield Block.", qfFeat =>
            {
                // This captures the RaisedShield qf to allow it to call its internal behavior, with
                // the stipulation of adding the reprisal functionality after shield blocking.
                qfFeat.AfterYouAcquireEffect = async (qfThis, qfAcquired) =>
                {
                    if (qfAcquired.Id == QEffectId.RaisingAShield && qfThis.Owner.HasFeat(FeatName.ShieldBlock))
                    {
                        var oldDamageDealt = qfAcquired.YouAreDealtDamage;
                        qfAcquired.YouAreDealtDamage = async (qfRaisedShield, attacker, dealt, defender) =>
                        {
                            if (oldDamageDealt == null)
                                return null;
                            
                            // Get normal shield block stuff
                            DamageModification? result = await oldDamageDealt.Invoke(qfRaisedShield, attacker, dealt, defender);

                            if (result == null) // Didn't shield block
                                return result;
                            
                            // Has to be an adjacent attacker
                            if (!defender.IsAdjacentTo(attacker))
                                return result;
                            
                            // Do runic reprisal stuff
                            List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(defender, defender);
                            DrawnRune? reprisalDr = drawnRunes.FirstOrDefault(dr => 
                                dr.SourceAction != null
                                && dr.SourceAction.Name.Contains("Knock")
                                && dr.SourceAction.Tag is Rune reprisalRune
                                && reprisalRune.InvokeTechnicalTraits.Contains(Trait.IsHostile));
                            if (reprisalDr != null)
                            {
                                
                                if (await defender.Battle.AskForConfirmation(defender,
                                        reprisalDr.Illustration ?? Enums.Illustrations.InvokeRune,
                                        $"{{b}}Runic Reprisal{{/b}}\nYou just Shield Blocked. Invoke {{Blue}}{reprisalDr.Rune.Name}{{/Blue}} from your shield against {attacker.Name}?",
                                        "Invoke", "Pass"))
                                {
                                    await attacker.FictitiousSingleTileMove(attacker.Occupies); // Move them back, so the invoke animation looks good
                                    
                                    CombatAction? invokeThisRune = reprisalDr.Rune.CreateInvokeAction(null, defender, reprisalDr, 1, true, false);
                                    
                                    if (invokeThisRune == null)
                                        return result;
                                    
                                    invokeThisRune.Name = $"Runic Reprisal ({reprisalDr.Name})";
                                    await defender.Battle.GameLoop.FullCast(invokeThisRune, ChosenTargets.CreateSingleTarget(attacker));
                                }
                            }
                            
                            // Return normal shield block stuff
                            return result;
                        };
                    }
                };
            });
        ModManager.AddFeat(RunicReprisal);
        
        TracingTrance = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatTracingTrance", "Tracing Trance"),
            6,
            "Your hands flow unbidden, tracing runes as if by purest instinct.",
            "{b}Trigger{/b} Your turn begins.\n\nYou become quickened until the end of your turn and can use the extra action only to {tooltip:Runesmith.Action.TraceRune}Trace Runes{/}, including to supply {icon:Action} 1 action if using the {icon:TwoActions} 2-action version of Trace Rune. Absorbed in the act of creation, you can't use any {tooltip:Runesmith.Trait.Invocation}invocation{/} actions this turn.",
            [Enums.Traits.Runesmith])
            .WithActionCost(0)
            .WithPermanentQEffect("At the start of your turn, you can give up taking any invocation actions to become quickened 1 for that turn (only to Trace Runes).",
            qfFeat =>
            {
                qfFeat.StartOfYourPrimaryTurn = async (qfThis, caster) =>
                {
                    if (await caster.Battle.AskForConfirmation(caster, IllustrationName.Haste, "{b}Tracing Trance {icon:FreeAction}{/b}\nBecome {Blue}quickened{/Blue} this turn? This extra action is only to Trace Runes. You can't use any invocation actions this turn.", "Yes", "No"))
                    {
                        QEffect tranceEffect = QEffect.Quickened(action =>
                        {
                            // Code not shortened in case I need to expand the logic.
                            if (action.HasTrait(Enums.Traits.Traced))
                                return true;
                            return false;
                        });
                        tranceEffect.PreventTakingAction = action =>
                        {
                            // Code not shortened in case I need to expand the logic.
                            if (action.HasTrait(Enums.Traits.Invocation))
                                return "(tracing trance) can't take invocation actions";
                            return null;
                        };
                        tranceEffect.Name = "Tracing Trance";
                        tranceEffect.Description = "You have an extra action you can use to Trace Runes. You can't use invocation actions.";
                        tranceEffect.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                        caster.AddQEffect(tranceEffect);
                        
                        // Actually GIVE YOU the quickened action this turn.
                        if (caster.Actions.QuickenedForActions == null)
                            caster.Actions.QuickenedForActions = new DisjunctionDelegate<CombatAction>(tranceEffect.QuickenedFor!);
                        else
                            caster.Actions.QuickenedForActions.Add(tranceEffect.QuickenedFor!);
                        CombatAction dummyTraceAction =
                            CombatAction.CreateSimple(caster, "Dummy Trace Action", [Enums.Traits.Traced]);
                        dummyTraceAction.UsedQuickenedAction = true;
                        caster.Actions.RevertExpendingOfResources(1, dummyTraceAction);
                        //caster.Actions.ResetToFull(); // <-- Has bug: Bypasses stunned and slowed (or at least acts as if taking a 2nd turn when generating actions)
                    }
                };
            });
        ModManager.AddFeat(TracingTrance);
        
        VitalCompositeInvocation = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatVitalCompositeInvocation", "Vital Composite Invocation"),
            6,
            "As you invoke runes from traditions that manipulate vital energy, you can release that energy to restore flesh.",
            "{b}Frequency{/b} once per combat\n\nYou {tooltip:Runesmith.Action.InvokeRune}Invoke two Runes{/} of your choice on a single creature or on any items it's wielding; one must be a divine rune, and one must be a primal rune. In addition to the runes’ normal effects, the creature also regains Hit Points equal to your Intelligence modifier + double your level.\n\n"+new ModdedIllustration(Enums.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Implementation{/b} Any rune without a tradition trait (Arcane, Divine, Primal, or Occult) is considered Divine if you're trained in Religion, or Primal if you're trained in Nature, or both.",
            [Trait.Healing, Enums.Traits.Invocation, Enums.Traits.Runesmith, Trait.Positive])
            .WithActionCost(2)
            .WithPermanentQEffect("You can invoke a divine and primal rune on an ally to also heal them.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if ((qfFeat.Tag is bool) == true)
                        return null;
                    
                    CombatAction vci = new CombatAction(
                        qfThis.Owner,
                        new SideBySideIllustration(IllustrationName.Heal, IllustrationName.Bless),
                        "Vital Composite Invocation",
                        [Trait.Healing, Enums.Traits.Invocation, Enums.Traits.Runesmith, Trait.Positive, Trait.Basic],
                        "{i}As you invoke runes from traditions that manipulate vital energy, you can release that energy to restore flesh.{/i}\n\n" + "{b}Frequency{/b} once per combat\n\nYou invoke two runes of your choice on a single creature or on any items it's wielding; one must be a divine rune, and one must be a primal rune. In addition to the runes’ normal effects, the creature also regains Hit Points equal to your Intelligence modifier + double your level.",
                        Target.RangedFriend(6)
                            .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                            {
                                List<DrawnRune> foundRunes = DrawnRune.GetDrawnRunes(attacker, defender);
                                if (foundRunes.Count > 1)
                                {
                                    if (foundRunes.Any(dr => dr.IsDivineRune()) && foundRunes.Any(dr => dr.IsPrimalRune()))
                                        return Usability.Usable;
                                    else
                                        return Usability.NotUsableOnThisCreature("divine and primal runes not found");
                                }
                                else
                                {
                                    return Usability.NotUsableOnThisCreature("less than 2 runes");
                                }
                            }))
                        .WithActionCost(2)
                        .WithEffectOnEachTarget( async (thisAction, caster, target, result) =>
                        {
                            List<Option> GetVitalCompositeOptions(bool includeDivine = false, bool includePrimal = false)
                            {
                                List<Option> divineOptions = [];
                                List<Option> primalOptions = [];
                                List<Option> bothOptions = [];
                                foreach (Creature cr in caster.Battle.AllCreatures)
                                {
                                    List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(caster, cr);
                                    foreach (DrawnRune runeQf in drawnRunes)
                                    {
                                        if (includeDivine && !includePrimal && runeQf.IsDivineRune())
                                        {
                                            CombatAction? invokeThisRune =
                                                runeQf.Rune.CreateInvokeAction(thisAction, caster, runeQf);
                                            if (invokeThisRune != null)
                                                GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, divineOptions,
                                                    false);
                                        }
                                        else if (includePrimal && !includeDivine && runeQf.IsPrimalRune())
                                        {
                                            CombatAction? invokeThisRune =
                                                runeQf.Rune.CreateInvokeAction(thisAction, caster, runeQf);
                                            if (invokeThisRune != null)
                                                GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, primalOptions,
                                                    false);
                                        }
                                        else if (includeDivine && includePrimal &&
                                            (runeQf.IsDivineRune() || runeQf.IsPrimalRune()))
                                        {
                                            CombatAction? invokeThisRune =
                                                runeQf.Rune.CreateInvokeAction(thisAction, caster, runeQf);
                                            if (invokeThisRune != null)
                                                GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, bothOptions,
                                                    false);
                                        }
                                    }
                                }

                                return includeDivine && includePrimal 
                                    ? bothOptions
                                    : includeDivine
                                        ? divineOptions
                                        : includePrimal
                                            ? primalOptions
                                            : [];
                            }
                            
                            // Get both for the first invocation
                            List<Option> bothRunes = GetVitalCompositeOptions(true, true);
                            
                            if (bothRunes.Count < 2) // Must have at least 1 divine rune and 1 primal rune.
                                return;
                            bothRunes.Add(new CancelOption(true));
                            bothRunes.Add(new PassViaButtonOption(" Confirm no additional runes "));
                            
                            // Await the first invocation
                            Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
                                new AdvancedRequest(caster, "Choose a divine or primal rune to invoke.", bothRunes)
                                {
                                    TopBarText = "Choose a divine or primal rune to invoke, or right-click to cancel (1/2)",
                                    TopBarIcon = thisAction.Illustration,
                                })).ChosenOption;

                            switch (chosenOption)
                            {
                                case CancelOption:
                                    thisAction.RevertRequested = true;
                                    return;
                                case PassViaButtonOption:
                                    return;
                            }

                            await chosenOption.Action();
                            
                            // Find the opposite invocation to do next
                            Rune? chosenRune = null;
                            foreach (Rune rune in RunesmithClassRunes.AllRunes)
                            {
                                if (chosenOption.Text.Contains(rune.Name))
                                {
                                    chosenRune = rune;
                                    break;
                                }
                            }

                            List<Option> nextOptions = [];
                            string chosenTrait = "";
                            if (chosenRune != null && chosenRune.HasTrait(Trait.Divine))
                            {
                                nextOptions = GetVitalCompositeOptions(true, false);
                                chosenTrait = "Divine";
                            }
                            else if (chosenRune != null && chosenRune.HasTrait(Trait.Primal))
                            {
                                
                                nextOptions = GetVitalCompositeOptions(false, true);
                                chosenTrait = "Primal";
                            }
                            else if (chosenRune != null)
                            {
                                nextOptions = GetVitalCompositeOptions(true, true);
                                chosenTrait = "Divine or Primal";
                            }
                            
                            // Await the second invocation
                            if (nextOptions.Count <= 0)
                                return;
                            nextOptions.Add(new PassViaButtonOption(" Confirm incomplete action "));
                            
                            Option chosenOption2 = (await caster.Battle.SendRequest( // Send a request to pick an option
                                new AdvancedRequest(caster, $"Choose a {chosenTrait} rune to invoke.", nextOptions)
                                {
                                    TopBarText = $"Choose a {chosenTrait} rune to invoke. (2/2)",
                                    TopBarIcon = thisAction.Illustration,
                                })).ChosenOption;

                            switch (chosenOption2)
                            {
                                case CancelOption:
                                    thisAction.RevertRequested = true;
                                    return;
                                case PassViaButtonOption:
                                    return;
                            }

                            await chosenOption2.Action();

							// Do healing
							int healingAmount = caster.Abilities.Intelligence + (caster.Level * 2);
                            await target.HealAsync(healingAmount.ToString(), thisAction);

                            qfFeat.Tag = true;
                        });
                    Rune.WithImmediatelyRemovesImmunity(vci);
                    
                    return new ActionPossibility(vci);
                };
            });
        ModManager.AddFeat(VitalCompositeInvocation);
        
        WordsFlyFree = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatWordsFlyFree", "Words, Fly Free"),
            6,
            "Just because your runes are tattooed on your body doesn't mean they need to remain there.",
            "{b}Requirements{/b} Your Runic Tattoo is not faded.\n\nYou fling your hand out, the rune from your {tooltip:Runesmith.Feats.RunicTattoo}Runic Tattoo{/} flowing down it and flying through the air in a crescent. You {tooltip:Runesmith.Action.TraceRune}Trace the Rune{/} onto all creatures or objects within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.",
            [Trait.Manipulate, Enums.Traits.Runesmith])
            .WithActionCost(1)
            .WithPrerequisite(RunicTattoo.FeatName, "Runic Tattoo")
            .WithPermanentQEffect("You can expend your Runic Tattoo by tracing it on all valid targets in a 15-foot cone.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains("RunicTattoo") || RunicTattoo.Subfeats == null)
                            return null;
                        
                        Feat? selectedTattoo = null;
                        foreach (Feat ft in RunicTattoo.Subfeats.Where(ft => qfThis.Owner.HasFeat(ft.FeatName)))
                            selectedTattoo = ft;

                        if (selectedTattoo is not { Tag: Rune tattooedRune })
                            return null;
                        
                        CombatAction flyFreeAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(selectedTattoo.Illustration ?? IllustrationName.Action, IllustrationName.SeekCone),
                            "Words, Fly Free",
                            [Trait.Manipulate, Enums.Traits.Runesmith, Enums.Traits.Traced, Trait.Basic],
                            "{i}Just because your runes are tattooed on your body doesn't mean they need to remain there.{/i}\n\n{b}Requirements{/b} Your Runic Tattoo is not faded.\n\nYou fling your hand out, the rune from your Runic Tattoo flowing down it and flying through the air in a crescent. You trace the rune onto all creatures or objects within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.",
                            Target.Cone(3))
                            .WithActionCost(1)
                            .WithProjectileCone(VfxStyle.BasicProjectileCone(selectedTattoo.Illustration ?? IllustrationName.Action))
                            .WithSoundEffect(SfxName.AncientDust)
                            .WithEffectOnEachTarget( async (thisAction, caster, target, result) =>
                            {
                                await tattooedRune.DrawRuneOnTarget(thisAction, caster, target, false);
                                
                                // Find the tattoo feat QF
                                if (qfThis.Owner.QEffects.FirstOrDefault(qf => qf.Name != null && qf.Name.Contains("Runic Tattoo"))
                                    is { } tattooQf)
                                {
                                    // get the associated etched DrawnRune
                                    if (tattooQf.Tag is DrawnRune tattooRuneQf)
                                    {
                                        caster.RemoveAllQEffects(qf => qf == tattooRuneQf);
                                    }
                                }
                                
                                qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Add("RunicTattoo");
                            });
                        
                        return new ActionPossibility(flyFreeAction, PossibilitySize.Full);
                    };
                });
        ModManager.AddFeat(WordsFlyFree);
        
        /////////////////////
        // 8th Level Feats //
        /////////////////////
        DrawnInRed = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatDrawnInRed", "Drawn In Red"),
            8,
            "After striking the target, you run a brush or finger along your weapon to collect a bit of its blood.",
            "{b}Requirements{/b} Your last action was a successful melee Strike that dealt physical damage.\n\nFor the encounter, when you {tooltip:Runesmith.Action.TraceRune}Trace a Rune{/} and the target is that creature, you can do so at a range of 60 feet as a single {icon:Action} action. Using Drawn in Red against a different creature ends the benefits against the previous creature.",
            [Enums.Traits.Runesmith])
            .WithActionCost(0)
            .WithPermanentQEffect("After a successful physical melee strike, you can use the target's blood to Trace Runes up to 60 feet away as a single action.",
            qfFeat =>
            {
                qfFeat.Tag = false; // If true, action is allowed. If combataction, also is allowed, and use its data.
                
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    qfThis.Tag = action.HasTrait(Trait.Strike) && action.HasTrait(Trait.Melee);
                };

                qfFeat.AfterYouDealDamageOfKind = async (caster, action, dmgKind, target) =>
                {
                    // The begun action was legal.
                    if (qfFeat.Tag is true)
                        if (dmgKind is DamageKind.Bludgeoning or DamageKind.Piercing or DamageKind.Slashing ||
                            action.Item?.WeaponProperties?.AdditionalPersistentDamageKind is DamageKind.Bleed)
                            qfFeat.Tag = action;
                };
                
                qfFeat.ProvideContextualAction = qfThis =>
                {
                    if (qfThis.Tag is false || qfThis.Tag is not CombatAction || qfThis.Owner.Actions.ActionHistoryThisTurn.Count < 1)
                        return null;

                    Creature bloodTarget = (qfThis.Tag as CombatAction)!.ChosenTargets.ChosenCreature!;

                    CombatAction drawnInRedAction = new CombatAction(
                        qfThis.Owner,
                        Enums.Illustrations.DrawnInRed,
                        "Drawn In Red",
                        [Enums.Traits.Runesmith, Trait.Basic],
                        "{i}After striking the target, you run a brush or finger along your weapon to collect a bit of its blood.{/i}\n\n{b}Requirements{/b} Your last action was a successful melee Strike that dealt physical damage.\n\nFor the next minute, when you Trace a Rune and the target is that creature, you can do so at a range of 60 feet as a single action. Using Drawn in Red against a different creature ends the benefits against the previous creature.",
                        Target.Self())
                        .WithActionCost(0)
                        .WithEffectOnEachTarget( async (thisAction, caster, target, result) =>
                        {
                            caster.RemoveAllQEffects(qf => qf.Name is "Drawn In Red");
                            
                            QEffect drawnInRedEffect = new QEffect(
                                "Drawn In Red",
                                $"You can Trace Runes onto {{Blue}}{bloodTarget.Name}{{\\Blue}} as 1 action with a range of 60 feet.",
                                ExpirationCondition.Never,
                                caster,
                                IllustrationName.BloodVendetta)
                            {
                                Tag = bloodTarget,
                            };

                            caster.AddQEffect(drawnInRedEffect);

                            qfThis.Tag = false;
                        });

                    return new ActionPossibility(drawnInRedAction, PossibilitySize.Full);
                };

                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (qfThis.Owner.QEffects.FirstOrDefault(qf => qf.Name is "Drawn In Red") is not { } bloodQf
                        || bloodQf.Tag is not Creature bloodTarget)
                        return null;

                    Rune? foundRune = null;
                    foreach (Rune rune in RunesmithClassRunes.AllRunes)
                    {
                        if (section.Name == rune.Name)
                            foundRune = rune;
                    }

                    if (foundRune is null)
                        return null;

                    if (foundRune.UsageCondition?.Invoke(qfFeat.Owner, bloodTarget) == Usability.Usable)
                    {
                        CombatAction bloodTrace = foundRune.CreateTraceAction(qfThis.Owner, 2, 12)
                            .WithActionCost(1)
                            .WithExtraTrait(Trait.Basic);
                        ((CreatureTarget)bloodTrace.Target).WithAdditionalConditionOnTargetCreature(
                            (attacker, defender) => defender == bloodTarget
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature("not Drawn In Blood target"));
                        bloodTrace.Name = bloodTrace.Name.Remove(0, "Trace".Length).Insert(0, "Draw");
                        bloodTrace.Description =
                            foundRune.CreateTraceActionDescription(bloodTrace, prologueText:"{Blue}{b}Range{/b} 60 feet{/Blue}\n", withFlavorText: false, afterUsageText:$" {{Blue}}(only against {bloodTarget.Name}){{/Blue}}");
                        ActionPossibility bloodPossibility = new ActionPossibility(bloodTrace)
                        {
                            Caption = "Drawn In Red",
                            Illustration = new SideBySideIllustration(IllustrationName.Action, Enums.Illustrations.DrawnInRed),
                        };
                        return bloodPossibility;
                    }
                    
                    return null;
                };
            });
        ModManager.AddFeat(DrawnInRed);
        
        // TODO: item tooltips
        ElementalRevision = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatElementalRevision", "Elemental Revision"),
            8,
            "You can scratch out and rewrite part of an elemental rune to temporarily change the type of power it channels.",
            "You touch an adjacent {i}corrosive{/i}, {i}flaming{/i}, {i}frost{/i}, {i}shock{/i}, or {i}thundering{/i} property rune on an item held by an ally, and you change it to any other property rune from that list. The revision lasts until the end of combat, before the property rune's original magic reasserts itself. When you do so, the ally wielding the item becomes immune to this ability until your next daily preparations.",
            [Enums.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Replace an ally's corrosive, flaming, frost, shock, or thundering rune. Once per day per ally.",
            qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction revisionAction = new CombatAction(
                        qfFeat.Owner,
                        IllustrationName.ResistEnergy,
                        "Elemental Revision",
                        [Enums.Traits.Runesmith],
                        "{i}You can scratch out and rewrite part of an elemental runestone to temporarily change the type of power it channels.{/i}\n\nYou touch an adjacent {Blue}corrosive{/Blue}, {Blue}flaming{/Blue}, {Blue}frost{/Blue}, {Blue}shock{/Blue}, or {Blue}thundering{/Blue} runestone on an item held by an ally, and you change it to any other runestone from that list. The revision lasts until the end of combat, before the runestone's original magic reasserts itself. You can only revise a runestone on an ally's item once per day.",
                        Target.RangedFriend(1) // Ranged 1 is used instead of adjacent in order to avoid an animation.
                            .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                            {
                                if (defender.PersistentUsedUpResources.UsedUpActions.Contains("ElementalRevision"))
                                    return Usability.NotUsableOnThisCreature("immune");
                                
                                foreach (Item item in defender.HeldItems)
                                {
                                    foreach (Item runestone in item.Runes)
                                    {
                                        if (runestone.ItemName is ItemName.CorrosiveRunestone or ItemName.FlamingRunestone
                                            or ItemName.FrostRunestone or ItemName.ShockRunestone
                                            or ItemName.ThunderingRunestone)
                                            return Usability.Usable;
                                    }
                                }
                                
                                return Usability.NotUsableOnThisCreature("no valid runestone");
                            }))
                        .WithActionCost(1)
                        .WithEffectOnEachTarget( async (thisAction, caster, target, result) =>
                        {
                            List<ItemName> swappableRunestones =
                            [
                                ItemName.CorrosiveRunestone,
                                ItemName.FlamingRunestone,
                                ItemName.FrostRunestone,
                                ItemName.ShockRunestone,
                                ItemName.ThunderingRunestone,
                            ];
                            List<Option> options = [];
                            foreach (Item item in target.HeldItems)
                            {
                                foreach (Item runestone in item.Runes.Where(runestone => swappableRunestones.Contains(runestone.ItemName)))
                                {
                                    foreach (ItemName runestoneOption in swappableRunestones)
                                    {
                                        if (runestone.ItemName == runestoneOption)
                                            continue; // Don't swap rune to itself.
                                            
                                        Item newRunestone = Items.CreateNew(runestoneOption); // Used purely for description
                                        Option runeOption = Option.ChooseCreature(
                                                $"Rewrite {item.BaseItemName}'s {runestone.RuneProperties!.Prefix} to {newRunestone.RuneProperties!.Prefix}",
                                                target,
                                                async () =>
                                                {
                                                    Item newItem = RunestoneRules.RecreateWithUnattachedSubitem(item, runestone, true);
                                                    newItem.WithModificationRune(runestoneOption);
                                                    target.HeldItems[target.HeldItems.IndexOf(item)] = newItem;
                                                    target.PersistentUsedUpResources.UsedUpActions.Add("ElementalRevision");
                                                    Sfxs.Play(SfxName.ShieldSpell);
                                                    await caster.FictitiousSingleTileMove(target.Occupies);
                                                    target.Occupies.Overhead(newItem.Name, Color.Black);
                                                    await caster.FictitiousSingleTileMove(caster.Occupies);
                                                })
                                            .WithTooltip(newRunestone.Description!)
                                            .WithIllustration(thisAction.Illustration);
                                        options.Add(runeOption);
                                    }
                                }
                            }
                            
                            if (options.Count <= 0)
                                return;
                            options.Add(new CancelOption(true)); // allow us to cancel it.
                            
                            Option chosenOption = (await caster.Battle.SendRequest(
                                new AdvancedRequest(caster, "Choose a runestone to revise.", options)
                                {
                                    TopBarText = "Choose a runestone to revise, or right-click to cancel.",
                                    TopBarIcon = thisAction.Illustration,
                                })).ChosenOption;

                            switch (chosenOption)
                            {
                                case CreatureOption creatureOption:
                                {
                                    break;
                                }
                                case CancelOption:
                                    thisAction.RevertRequested = true;
                                    return;
                                case PassViaButtonOption:
                                    return;
                            }

                            await chosenOption.Action();
                        });
                    
                    return new ActionPossibility(revisionAction, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(ElementalRevision);
        
        ReadTheBones = new TrueFeat(
            ModManager.RegisterFeatName("RunesmithPlaytest.FeatReadTheBones", "Read the Bones"),
            8,
            "Using ancient scripts, you carve a question into bone before casting it into fire, where it cracks.",
            "You gain a permanent +1 status bonus to initiative rolls.",
            [Enums.Traits.Runesmith])
            .WithPermanentQEffect("You gain a +1 status bonus to initiative rolls.", qfFeat =>
            {
                qfFeat.BonusToInitiative = qfThis => new Bonus(1, BonusType.Status, "Read The Bones");
            });
        ModManager.AddFeat(ReadTheBones);
    }
}