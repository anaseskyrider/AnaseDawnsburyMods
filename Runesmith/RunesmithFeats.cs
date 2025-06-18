using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
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

public static class RunesmithFeats
{
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
        Feat backupRunicEnhancement = new TrueFeat(
                ModData.FeatNames.BackupRunicEnhancement,
                1,
                "While you are not a spellcaster, you have a working knowledge of the most fundamental of runic magic.",
                "Once per day, you can cast your choice of either " + AllSpells.CreateModernSpellTemplate(SpellId.MagicFang, Trait.Wizard).ToSpellLink().Replace("magic fang", "runic body") + " or " + AllSpells.CreateModernSpellTemplate(SpellId.MagicWeapon, Trait.Wizard).ToSpellLink().Replace("magic weapon", "runic weapon") + " as an innate spell. The rank of these spells is equal to half your level, rounded up" + "." + /*TODO: ", (NYI) and the tradition can be any tradition for which you are at least trained in the related skill." +*/ "\n\n"+new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString+" {b}Reminder{/b} You choose which one to cast. You cannot cast both in a day.",
                [ModData.Traits.Runesmith])
            .WithOnSheet(sheet =>
            {
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            })
            .WithOnCreature(creature =>
            {
                Trait classOfOrigin = ModData.Traits.Runesmith;
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
                    if (spellcastingSource != null && spellcastingSource.ClassOfOrigin == ModData.Traits.Runesmith) // If source is Backup Runic Enhancement,
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
        ModManager.AddFeat(backupRunicEnhancement);
        
        Feat engravingStrike = new TrueFeat(
                ModData.FeatNames.EngravingStrike,
                1,
                "You draw a rune onto the surface of your weapon in reverse, the mark branding or bruising itself into your target in the moment of impact.",
                "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a melee weapon and {i}(due to Trace Rune){/i} have a free hand\n\nMake a melee Strike with the weapon. On a success, you "+ModTooltips.ActionTraceRune+"Trace a Rune{/} onto the target of the Strike.\n\n"+new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Playtest Ruling{/b} You can Trace runes that draw onto the target's equipment, not just the creature itself.",
                [ModData.Traits.Runesmith])
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
                                if (!target.DeathScheduledForNextStateCheck)
                                    await CommonRuneRules.PickACreatureAndDrawARune(thisAction, caster, (filterTarget => filterTarget == target));
                            }

                            qfFeat.UsedThisTurn = true;
                        });
                    engravingStrike.Name = "Engraving Strike";
                    engravingStrike.Illustration =
                        new SideBySideIllustration(item.Illustration, ModData.Illustrations.TraceRune);
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
                            RunesmithClass.IsRunesmithHandFree(attacker) ? Usability.Usable : Usability.NotUsable("You must have a free hand to trace a rune"));
                    
                    return engravingStrike;
                };
            });
        ModManager.AddFeat(engravingStrike);
        
        Feat remoteDetonation = new TrueFeat(
                ModData.FeatNames.RemoteDetonation,
                1,
                "You whisper an invocation over an arrow or sling bullet as you fire it, and the hissing of the missile through the air sounds just like your murmured voice.",
                "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a ranged weapon\n\nMake a ranged Strike against a target within the weapon's first range increment" /*+ " using physical ammunition"*/ + ". On a success, you invoke all the runes on the target as the missile's whispering sets off the runes. On a critical success, the target also takes a –1 circumstance penalty on any saving throws against the runes invoked by your Remote Detonation.",
                [ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.Spell])
            .WithActionCost(1)
            .WithPermanentQEffect("Make a ranged Strike. On a hit, invokes all runes on the target. On a crit, it also takes a -1 circumstance penalty to its saving throws against these invocations.", qfFeat =>
            {
                Illustration remoteDetIllustration = new SideBySideIllustration(IllustrationName.LooseTimesArrow,
                    ModData.Illustrations.InvokeRune);

                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Weapon) || !item.HasTrait(Trait.Ranged) || item.WeaponProperties == null || item.WeaponProperties.RangeIncrement == -1)
                        return null;

                    CombatAction remoteDet = qfFeat.Owner.CreateStrike(item).WithActionCost(1);
                    remoteDet.Name = "Remote Detonation";
                    remoteDet.Illustration =
                        new SideBySideIllustration(item.Illustration, ModData.Illustrations.InvokeRune);
                    remoteDet.Traits.Add(ModData.Traits.Invocation);
                    remoteDet.Traits.Add(ModData.Traits.Runesmith);
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
                                        invokeAction.HasTrait(ModData.Traits.Invocation) &&
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
                            CombatAction? invokeThisRune = CommonRuneRules.CreateInvokeAction(remoteDetAction, caster, drawnRune, drawnRune.Rune, (int)item.WeaponProperties.RangeIncrement);
                            if (invokeThisRune != null)
                                await caster.Battle.GameLoop.FullCast(invokeThisRune, ChosenTargets.CreateSingleTarget(target));
                        }
                        
                        //Rune.RemoveAllImmunities(target);

                        target.RemoveAllQEffects(qf => qf.Name == "Remote Detonation Critical Success");
                    });

                    CommonRuneRules.WithImmediatelyRemovesImmunity(remoteDet);

                    return remoteDet;
                };
            });
        ModManager.AddFeat(remoteDetonation);
        
        Feat runeSinger = new TrueFeat(
                ModData.FeatNames.RuneSinger,
                1,
                "You practice the lost art of using music to guide the act of carving your runes, singing them into existence as much as crafting them.",
                /*"You can use Performance instead of Crafting when attempting Crafting checks related to runes. " + */"Once per combat, you can "+ModTooltips.ActionTraceRune+"Trace a Rune{/} with song alone, removing the need to have a free hand, removing the manipulate trait from Trace Rune, and allowing you to use the "+RulesBlock.GetIconTextFromNumberOfActions(2)+" 2-action version of Trace Rune as a single "+RulesBlock.GetIconTextFromNumberOfActions(1)+" action."/*+" You don't need to be able to move your hands when Tracing a Rune using song, but you do need to be able to sing in a clear voice."*/,
                [ModData.Traits.Runesmith])
            .WithPermanentQEffect("Once per combat, you can Trace a Rune without a free hand on a target up to 30 feet away.",
                qfFeat =>
                {
                    qfFeat.Id = ModData.QEffectIds.RuneSingerCreator;
                    
                    qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                    {
                        if (submenu.SubmenuId != ModData.SubmenuIds.TraceRune)
                            return null;

                        CombatAction toggleSinger = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.RuneSinger,
                                $"Rune-Singer {(qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger) ? "(off)" : "(on)")}",
                                [ModData.Traits.Runesmith, Trait.Basic],
                                "{i}You practice the lost art of using music to guide the act of carving your runes, singing them into existence as much as crafting them.{/i}\n\n"+"The next time you "+ModTooltips.ActionTraceRune+"Trace a Rune{/} will be with song alone, removing the need to have a free hand, removing the manipulate trait from Trace Rune, and allowing you to use the "+RulesBlock.GetIconTextFromNumberOfActions(2)+" 2-action version of Trace Rune as a single "+RulesBlock.GetIconTextFromNumberOfActions(1)+" action."+/*" You don't need to be able to move your hands when Tracing a Rune using song, but you do need to be able to sing in a clear voice."+*/"\n\nOnce you Trace a Rune in this way, you can't do so again for the rest of this combat.",
                                Target.Self())
                            .WithActionCost(0)
                            .WithSoundEffect(ModData.SfxNames.ToggleRuneSinger)
                            .WithEffectOnSelf(self =>
                            {
                                switch (self.HasEffect(ModData.QEffectIds.RuneSinger))
                                {
                                    case true:
                                        self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.RuneSinger);
                                        break;
                                    case false:
                                        QEffect singerEffect = new QEffect(
                                                "Rune-Singer",
                                                "{Red}{b}Frequency{/b} once per combat{/Red}\nThe next time you Trace a Rune, it won't require a free hand, it won't have the manipulate trait, and you can use the 2-action version as a single action.",
                                                ExpirationCondition.Never,
                                                self,
                                                ModData.Illustrations.RuneSinger)
                                            {
                                                Id = ModData.QEffectIds.RuneSinger,
                                                DoNotShowUpOverhead = true,
                                            };
                                        self.AddQEffect(singerEffect);
                                        break;
                                }
                            });

                        return new PossibilitySection("Rune-Singer")
                        {
                            PossibilitySectionId = ModData.PossibilitySectionIds.RuneSinger,
                            Possibilities = { new ActionPossibility(toggleSinger, PossibilitySize.Full)}
                        };
                    };
                    
                    // Held onto for now.
                    /*qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        /*if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains("Rune-Singer"))
                            return null;#1#

                        Rune? foundRune = null;
                        foreach (Rune rune in RunesmithRunes.AllRunes)
                        {
                            if (section.Name == rune.Name)
                                foundRune = rune;
                        }

                        if (foundRune is null)
                            return null;

                        CombatAction runeSingerAction = CommonRuneRules.CreateTraceAction(qfThis.Owner, foundRune, 2)
                            .WithActionCost(1)
                            .WithExtraTrait(Trait.Basic);
                        runeSingerAction.Name = runeSingerAction.Name
                            .Remove(0, "Trace".Length)
                            .Insert(0, "Sing");
                        runeSingerAction.Traits.Remove(Trait.Manipulate);
                        // Manually recreate target to remove the free hand requirement
                        CreatureTarget newTarget = Target.RangedCreature(6);
                        if (foundRune.UsageCondition != null)
                            newTarget = newTarget.WithAdditionalConditionOnTargetCreature(foundRune.UsageCondition);
                        runeSingerAction.Target = newTarget;
                        runeSingerAction.Description =
                            foundRune.CreateTraceActionDescription(runeSingerAction, prologueText:"{Blue}{b}Range{/b} 30 feet{/Blue}\n", withFlavorText: false, afterFlavorText:"{Blue}{b}Frequency{/b} once per combat{/Blue}");
                        runeSingerAction.WithEffectOnSelf(self =>
                        {
                            qfFeat.ExpiresAt = ExpirationCondition.Immediately;
                        });
                        
                        ActionPossibility singPossibility = new ActionPossibility(runeSingerAction)
                        {
                            Caption = "Rune-Singer",
                            Illustration = new SideBySideIllustration(IllustrationName.Action,
                                ModData.Illustrations.RuneSinger)
                        };
                        
                        return singPossibility;
                    };*/
                });
        ModManager.AddFeat(runeSinger);
        
        /////////////////////
        // 2nd Level Feats //
        /////////////////////
        Feat fortifyingKnock = new TrueFeat(
                ModData.FeatNames.FortifyingKnock,
                2,
                "Your shield is a natural canvas for your art.",
                "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a shield and {i}(due to Trace Rune){/i} have a free hand\n\nIn one motion, you Raise a Shield and "+ModTooltips.ActionTraceRune+"Trace a Rune{/} on your shield.",
                [ModData.Traits.Runesmith, Trait.Spell])
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
                        bool validReprisal = qfThis.Owner.HasFeat(ModData.FeatNames.RunicReprisal) &&
                                             rune.InvokeTechnicalTraits.Contains(Trait.IsHostile);
                        bool validKnock = rune.DrawTechnicalTraits.Contains(Trait.Shield);// &&
                                          //(rune.UsageCondition == null || rune.UsageCondition.Invoke(qfThis.Owner, qfThis.Owner) == Usability.Usable);
                        if (!validReprisal && !validKnock)
                            continue;

                        // Disable this rune if it isn't a proper shield rune.
                        bool isDisabledRune = !rune.DrawTechnicalTraits.Contains(Trait.Shield);
                        
                        CombatAction knockThisRune = CommonRuneRules.CreateTraceAction(qfThis.Owner, rune, 0)
                            .WithActionCost(1)
                            .WithExtraTrait(Trait.DoNotShowInCombatLog); // Too much text spam.
                        if (knockThisRune.HasTrait(Trait.Manipulate)) // Don't bother adding if it doesn't have manipulate
                            knockThisRune.WithExtraTrait(Trait.DoesNotProvoke); // Provoke manually later
                        knockThisRune.Name = $"Knock {rune.Name}";
                        knockThisRune.Illustration = new SideBySideIllustration(shieldIll, rune.Illustration);
                        knockThisRune.Description = CommonRuneRules.CreateTraceActionDescription(
                            knockThisRune,
                            rune,
                            withFlavorText: false,
                            afterFlavorText:"{b}Frequency{/b} once per round");
                        knockThisRune.Description = knockThisRune.Description.Replace(rune.UsageText, "{Blue}drawn on your raised shield.{/Blue}");
                        if (isDisabledRune)
                        {
                            string oldPassiveText = rune.PassiveTextWithHeightening(rune, knockThisRune.Owner.Level);
                            int start = knockThisRune.Description.IndexOf(oldPassiveText);
                            if (start != -1)
                                knockThisRune.Description = knockThisRune.Description.Replace(oldPassiveText, "{Blue}(Runic Reprisal) When you use Shield Block against an adjacent attacker, this rune's invocation effects are detonated outward onto the attacker.{/Blue}");
                        }

                        var oldRestriction = (knockThisRune.Target as SelfTarget)!.AdditionalRestriction;
                        knockThisRune.Target = Target.Self()
                            .WithAdditionalRestriction(self =>
                            {
                                if (shieldItem == null)
                                    return "You must have a shield equipped";
                                if (qfThis.UsedThisTurn)
                                    return "Already used this round";
                                return oldRestriction?.Invoke(self);
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
                                
                                // Provoke manipulate, if the action is supposed to (e.g. Rune-Singer)
                                CombatAction phantomManipulate = CombatAction.CreateSimple(
                                        caster,
                                        $"Trace {rune.Name}",
                                        Trait.DoNotShowInCombatLog,
                                        Trait.Manipulate)
                                    .WithActionCost(0);
                                if (thisAction.HasTrait(Trait.Manipulate))
                                    await phantomManipulate.AllExecute();

                                if (phantomManipulate.Disrupted)
                                    return;
                                
                                // Trace the rune
                                Rune actionRune = (thisAction.Tag as Rune)!;
                                if (await CommonRuneRules.DrawRuneOnTarget(thisAction, caster, target, actionRune, true) is { } drawnRune)
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
                            repriseSection.AddPossibility(new ActionPossibility(knockThisRune, PossibilitySize.Full));
                        else
                            fortSection.AddPossibility(new ActionPossibility(knockThisRune, PossibilitySize.Full));
                    }
                    
                    SubmenuPossibility fortifyingKnockSubmenu = new SubmenuPossibility(
                        new SideBySideIllustration(shieldIll, ModData.Illustrations.TraceRune),
                        "Fortifying Knock",
                        PossibilitySize.Half)
                    {
                        SpellIfAny = new CombatAction(qfThis.Owner, new SideBySideIllustration(shieldIll, ModData.Illustrations.TraceRune), "Fortifying Knock", [ModData.Traits.Runesmith], "{i}Your shield is a natural canvas for your art.{/i}\n\n"+
                            "{b}Frequency{/b} once per round\n{b}Requirements{/b} You are wielding a shield and {i}(due to Trace Rune){/i} have a free hand\n\nIn one motion, you Raise a Shield and Trace a Rune on your shield.", Target.Self()).WithActionCost(1), // This doesn't DO anything, it's just to provide description to the menu.
                        Subsections = { fortSection, repriseSection },
                    };
                    
                    return fortifyingKnockSubmenu;
                };
            });
        ModManager.AddFeat(fortifyingKnock);
        
        Feat invisibleInk = new TrueFeat(
                ModData.FeatNames.InvisibleInk,
                2,
                "When your rune is drawn, it leaves only the barest mark.",
                "You no longer cease being hidden when you "+ModTooltips.ActionTraceRune+"Trace a Rune{/}.",
                [ModData.Traits.Runesmith])
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (action.HasTrait(ModData.Traits.Traced) || action.HasTrait(ModData.Traits.Etched))
                        action.WithExtraTrait(Trait.DoesNotBreakStealth);
                };
            });
        ModManager.AddFeat(invisibleInk);
        
        // Runic Tattoo
        // BUG: Feature fails to apply without warning if the chosen option is too high for your current level in free encounter mode.
        Feat runicTattoo = new TrueFeat(
                ModData.FeatNames.RunicTattoo, // "RunesmithPlaytest.FeatRunicTattoo"
                2,
                "Drawing your favorite rune in your flesh, you know you'll never be without it.",
                "Choose one rune you know. The rune is etched at the beginning of combat and doesn't count toward your maximum limit of etched runes. You can invoke this rune like any of your other runes, but once invoked, the rune fades significantly and is drained of power until your next daily preparations.\n\n{b}Special{/b} This feat can be retrained to select runes which were learned at higher levels. {i}(May fail to apply if playing at a level that's too low for your selection in Free Encounter Mode.){/i}\n\n"+new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Implementation{/b} This feat is expanded to allow you to etch onto items you're wielding each combat, rather than only runes drawn onto creatures. At level 6, the feat "+ModTooltips.FeatsWordsFlyFree+"Words, Fly Free{/} offers a way to use tattoo options that would otherwise be detrimental or useless on yourself.",
                [ModData.Traits.Runesmith])
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption(
                    "RunesmithPlaytest.RunicTattooSelection",
                    "Tattooed Rune",
                    values.Sheet.MaximumLevel,
                    ft =>
                        ft.Tag is Rune rune
                        && ft.ToTechnicalName().Contains("FeatTattooed")));
            });
        foreach (RuneFeat runeFeat in RunesmithRunes.AllRuneFeats)
        {
            Feat tattooFeat = new Feat(
                ModManager.RegisterFeatName("RunesmithPlaytest.FeatTattooed"+runeFeat.Rune.RuneId.ToStringOrTechnical(), runeFeat.Name),
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
                    return repertoire != null && repertoire.KnowsRune(values, runeFeat.Rune);
                },
                "You must know this rune.")
                .WithPrerequisite(values =>
                    {
                        return !runeFeat.HasTrait(ModData.Traits.Diacritic);
                    },
                "Diacritic runes can't be tattooed onto yourself.")
                .WithPermanentQEffect("You have a rune tattoo. Invoking it deactivates it until your next daily preparations.",
                    qfFeat =>
                    {
                        Creature runesmith = qfFeat.Owner;
                        qfFeat.Name = $"Runic Tattoo ({runeFeat.Rune.RuneId.ToStringOrTechnical()})";
                        qfFeat.StartOfCombat = async qfThis =>
                        {
                            if (runesmith.PersistentUsedUpResources.UsedUpActions.Contains("RunicTattoo"))
                                return;

                            if (qfThis.UsedThisTurn)
                                return;

                            if (RunicRepertoireFeat.GetRepertoireOnCreature(runesmith) is not {} rep
                                || !rep.KnowsRune(runesmith, runeFeat.Rune))
                            {
                                // BUG: see previous bug at the top of this feat
                                runesmith.Battle.Log($"{runesmith}'s Runic Tattoo failed to apply: {{i}}{runeFeat.Name}{{/i}} is not known at this level.");
                                return;
                            }
                            // Etch that rune at the start of combat
                            CombatAction etchTattoo = CommonRuneRules.CreateEtchAction(runesmith, runeFeat.Rune);
                            etchTattoo.Name = "Runic Tattoo";
                            DrawnRune? appliedRune = await CommonRuneRules.DrawRuneOnTarget(
                                CombatAction.CreateSimple(
                                    runesmith,
                                    $"Runic Tattoo ({runeFeat.Rune.Name})",
                                    [ModData.Traits.Etched]),
                                runesmith,
                                runesmith,
                                runeFeat.Rune,
                                true);

                            if (appliedRune != null)
                            {
                                qfThis.Tag = appliedRune;
                                appliedRune.AfterInvokingRune += async (thisDrawnRune, sourceAction, invokedRune) =>
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
            ModManager.AddFeat(tattooFeat);
        }
        ModManager.AddFeat(runicTattoo);

        Feat smithingWeaponsFamiliarity = new TrueFeat(
                ModData.FeatNames.SmithingWeaponsFamiliarity,
                2,
                "Though you are an artisan, you are well versed in using the tools of the trade to fend off enemies.",
                "You have familiarity with weapons in the hammer, pick, and knife weapon groups -- for the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.\n\n"+new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Modding{/b} Other mods which add advanced weapons are required to benefit from this feat.",
                [ModData.Traits.Runesmith])
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
        ModManager.AddFeat(smithingWeaponsFamiliarity);
        
        /////////////////////
        // 4th Level Feats //
        /////////////////////
        Feat artistsAttendance = new TrueFeat(
                ModData.FeatNames.ArtistsAttendance,
                4,
                "Your runes call you to better attend to your art.",
                "{b}Frequency{/b} once per round\n\nStride twice. If you end your movement within reach of a creature that is bearing one of your runes, you can "+ModTooltips.ActionTraceRune+"Trace a Rune{/} upon any creature adjacent to you (even a different creature).\n\n"+new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Playtest Ruling{/b} You can also be a rune-bearer within your reach, and your reach can be based on a weapon or unarmed attack with the Reach trait. The Trace target must still be adjacent.",
                [ModData.Traits.Runesmith])
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
                        new SideBySideIllustration(IllustrationName.FleetStep, ModData.Illustrations.TraceRune),
                        "Artist's Attendance",
                        [ModData.Traits.Runesmith, Trait.Basic],
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
                                        await CommonRuneRules.PickACreatureAndDrawARune(thisAction, caster, (filterTarget => filterTarget != caster), 1);
                                        break;
                                    }
                                }
                                qfThis.UsedThisTurn = true;
                            }
                        });

                    return new ActionPossibility(attendAction, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(artistsAttendance);
        
        // TODO: item tooltip
        Feat ghostlyResonance = new TrueFeat(
                ModData.FeatNames.GhostlyResonance,
                4,
                "Your runes can not only draw power from the world of the spirits, but they can let even the most mundane objects harm spiritual beings as well.",
                "Any ally, or any items your allies wield, which bears one of your divine or occult runes gains the benefits of a ghost touch rune for as long as they are bearing your rune.\n\n"+new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Implementation{/b} Any rune which lacks a tradition trait (Arcane, Divine, Primal, or Occult) is considered Divine if you're trained in Religion, or Occult if you're trained in Occultism, or both.",
                [ModData.Traits.Runesmith])
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
        ModManager.AddFeat(ghostlyResonance);
        
        Feat terrifyingInvocation = new TrueFeat(
                ModData.FeatNames.TerrifyingInvocation,
                4,
                "You spit and roar as you pronounce your rune's terrible name.",
                "You attempt to Demoralize a single target within range, and then "+ModTooltips.ActionInvokeRune+"Invoke one Rune{/} upon the target. You can Demoralize the target as long as they are within range of your invocation, and you don't take a penalty if the creature doesn't understand your language.",
                [ModData.Traits.Invocation, ModData.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Demoralize a creature, then Invoke one Rune on them.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction scaryInvoke = new CombatAction(
                        qfThis.Owner,
                        new SideBySideIllustration(IllustrationName.Demoralize,
                            ModData.Illustrations.InvokeRune),
                        "Terrifying Invocation",
                        [ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.Spell, Trait.Basic],
                        "{i}You spit and roar as you pronounce your rune's terrible name.{/i}\n\n" +
                        "You attempt to Demoralize a single target within range, and then Invoke one Rune upon the target. You can Demoralize the target as long as they are within range of your invocation, and you don't take a penalty if the creature doesn't understand your language.",
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
                            await CommonRuneRules.PickARuneToInvokeOnTarget(thisAction, caster, target);
                        });

                    scaryInvoke = CommonRuneRules.WithImmediatelyRemovesImmunity(scaryInvoke);
                    
                    return new ActionPossibility(scaryInvoke, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(terrifyingInvocation);
        
        Feat transposeEtching = new TrueFeat(
                ModData.FeatNames.TransposeEtching,
                4,
                "With a pinching gesture, you pick up a word and move it elsewhere.",
                "You move any one of your runes within 30 feet to a different target within 30 feet.\n\n"+new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString+" {b}Reminder{/b} Despite the name, this can be used on traced runes, not just etched ones.\n\n{b}Special{/b} (homebrew) When a creature bearing one of your runes dies, you can use this action to move one of its runes as a {icon:FreeAction} free action.",
                [Trait.Manipulate, ModData.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect("Move a rune from one target to another, both within 30 feet.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction transposeAction = CreateTransposeAction(qfThis);
                    return new ActionPossibility(transposeAction, PossibilitySize.Half);
                };
                qfFeat.AddGrantingOfTechnical(
                    cr =>
                        DrawnRune.GetDrawnRunes(qfFeat.Owner, cr).Count > 0,
                    qfTech =>
                    {
                        qfTech.WhenCreatureDiesAtStateCheckAsync = async qfTech2 =>
                        {
                            qfTech2.Owner.DeathScheduledForNextStateCheck = false;
                            await qfFeat.Owner.Battle.GameLoop.FullCast(
                                CreateTransposeAction(qfFeat).WithActionCost(0),
                                ChosenTargets.CreateSingleTarget(qfTech2.Owner));
                            qfTech2.Owner.DeathScheduledForNextStateCheck = true;
                        };
                    });
                return;

                // TODO: Less clicky implementation
                CombatAction CreateTransposeAction(QEffect qfThis)
                {
                    return new CombatAction(
                        qfThis.Owner,
                        ModData.Illustrations.TransposeEtching,
                        "Transpose Etching",
                        [Trait.Manipulate, ModData.Traits.Runesmith, Trait.Spell, Trait.Basic],
                        "You move any one of your runes within 30 feet to a different target within 30 feet.",
                        Target.Self()
                            .WithAdditionalRestriction(self =>
                            {
                                return self.Battle.AllCreatures.Any(cr =>
                                    DrawnRune.GetDrawnRunes(self, cr).Where(dr => dr.Description != null && !dr.Description.Contains("Tattooed") && !dr.Description.Contains("Runic Reprisal")).ToList().Count > 0)
                                    ? null
                                    : "No rune-bearers";
                            }))
                        .WithActionCost(1)
                        .WithSoundEffect(ModData.SfxNames.TransposeEtchingStart)
                        .WithEffectOnEachTarget(async (transposeAction, caster, _,_) =>
                        {
                            List<Creature> possiblePickups = caster.Battle.AllCreatures
                                .Where(cr => cr.DistanceTo(caster) <= 6 && cr.QEffects.Any(qf => qf is DrawnRune))
                                .ToList();
                            DrawnRune? chosenRune = await CommonRuneRules.AskToChooseADrawnRune(
                                caster,
                                possiblePickups,
                                transposeAction.Illustration,
                                "Choose one of your runes to move to another creature within 30 feet or right-click to cancel.",
                                "Cancel choosing a rune",
                                true,
                                dr => dr.Source == caster && dr.Description != null && !(dr.Description.Contains("Tattooed") ||
                                    dr.Description.Contains("Runic Reprisal")));
                            if (chosenRune != null)
                            {
                                List<Creature> possibleDropoffs = caster.Battle.AllCreatures
                                    .Where(cr =>
                                        chosenRune.Rune.UsageCondition!.Invoke(caster, cr) == Usability.Usable) // TODO: Get around to removing null checks for UsageCondition. It has a default always-usable function now.
                                    .ToList();
                                Creature? chosenCreature = await caster.Battle.AskToChooseACreature(
                                    caster,
                                    possibleDropoffs,
                                    transposeAction.Illustration,
                                    $"Choose a creature to bear {{Blue}}{chosenRune.Rune.Name}{{/Blue}}",
                                    $"Move {{Blue}}{chosenRune.Rune.Name}{{/Blue}} to this creature.",
                                    "Cancel moving rune");
                                if (chosenCreature != null)
                                {
                                    DrawnRune pretendNewRune = (await chosenRune.Rune.NewDrawnRune!.Invoke(transposeAction, caster, chosenCreature, chosenRune.Rune))!;
                                    Sfxs.Play(ModData.SfxNames.TransposeEtchingEnd);
                                    /*await*/ chosenRune.MoveRuneToTarget(chosenCreature, pretendNewRune.DrawnOn);
                                }
                                else
                                    transposeAction.RevertRequested = true;
                            }
                            else
                                transposeAction.RevertRequested = true;
                        });
                }
            });
        ModManager.AddFeat(transposeEtching);
        
        /////////////////////
        // 6th Level Feats //
        /////////////////////
        Feat runicReprisal = new TrueFeat(
                ModData.FeatNames.RunicReprisal,
                6,
                "When you raise your shield, you bury a runic trap into it, to be set off by the clash of an enemy weapon.",
                "When you use "+ModTooltips.FeatsFortifyingKnock+"Fortifying Knock "+RulesBlock.GetIconTextFromNumberOfActions(1)+"{/}, you can trace a damaging rune on your shield, even if it could not normally be applied to a shield. The traced rune doesn't have its normal effect, instead fading into your shield. If you Shield Block "+RulesBlock.GetIconTextFromNumberOfActions(-2)+" with the shield against an adjacent target, you can "+ModTooltips.ActionInvokeRune+"Invoke the Rune{/} as part of the reaction, causing the rune to detonate outwards and apply its invocation effect to the attacking creature.",
                [ModData.Traits.Invocation, ModData.Traits.Runesmith])
            .WithPrerequisite(ModData.FeatNames.FortifyingKnock, "Fortifying Knock")
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
                                        reprisalDr.Illustration ?? ModData.Illustrations.InvokeRune,
                                        $"{{b}}Runic Reprisal{{/b}}\nYou just Shield Blocked. Invoke {{Blue}}{reprisalDr.Rune.Name}{{/Blue}} from your shield against {attacker.Name}?",
                                        "Invoke", "Pass"))
                                {
                                    await attacker.FictitiousSingleTileMove(attacker.Occupies); // Move them back, so the invoke animation looks good
                                    
                                    CombatAction? invokeThisRune = CommonRuneRules.CreateInvokeAction(null, defender, reprisalDr, reprisalDr.Rune, 6, true, false)?
                                        .WithExtraTrait(ModData.Traits.InvokeAgainstGivenTarget);
                                    
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
        ModManager.AddFeat(runicReprisal);
        
        Feat tracingTrance = new TrueFeat(
                ModData.FeatNames.TracingTrance,
                6,
                "Your hands flow unbidden, tracing runes as if by purest instinct.",
                "{b}Trigger{/b} Your turn begins.\n\nYou become quickened until the end of your turn and can use the extra action only to "+ModTooltips.ActionTraceRune+"Trace Runes{/}, including to supply "+RulesBlock.GetIconTextFromNumberOfActions(1)+" 1 action if using the "+RulesBlock.GetIconTextFromNumberOfActions(2)+" 2-action version of Trace Rune. Absorbed in the act of creation, you can't use any "+ModTooltips.TraitInvocation+"invocation{/} actions this turn.",
                [ModData.Traits.Runesmith])
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
                            if (action.HasTrait(ModData.Traits.Traced))
                                return true;
                            return false;
                        });
                        tranceEffect.PreventTakingAction = action =>
                        {
                            // Code not shortened in case I need to expand the logic.
                            if (action.HasTrait(ModData.Traits.Invocation))
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
                            CombatAction.CreateSimple(caster, "Dummy Trace Action", [ModData.Traits.Traced]);
                        dummyTraceAction.UsedQuickenedAction = true;
                        caster.Actions.RevertExpendingOfResources(1, dummyTraceAction);
                        //caster.Actions.ResetToFull(); // <-- Has bug: Bypasses stunned and slowed (or at least acts as if taking a 2nd turn when generating actions)
                    }
                };
            });
        ModManager.AddFeat(tracingTrance);
        
        Feat vitalCompositeInvocation = new TrueFeat(
                ModData.FeatNames.VitalCompositeInvocation,
                6,
                "As you invoke runes from traditions that manipulate vital energy, you can release that energy to restore flesh.",
                "{b}Frequency{/b} once per combat\n\nYou "+ModTooltips.ActionInvokeRune+"Invoke two Runes{/} of your choice on a single creature or on any items it's wielding; one must be a divine rune, and one must be a primal rune. In addition to the runes' normal effects, the creature also regains Hit Points equal to your Intelligence modifier + double your level.\n\n"+new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString+" {b}Implementation{/b} Any rune without a tradition trait (Arcane, Divine, Primal, or Occult) is considered Divine if you're trained in Religion, or Primal if you're trained in Nature, or both.",
                [Trait.Healing, ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.Positive])
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
                        [Trait.Healing, ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.Positive, Trait.Basic],
                        "{i}As you invoke runes from traditions that manipulate vital energy, you can release that energy to restore flesh.{/i}\n\n" + "{b}Frequency{/b} once per combat\n\nYou invoke two runes of your choice on a single creature or on any items it's wielding; one must be a divine rune, and one must be a primal rune. In addition to the runes' normal effects, the creature also regains Hit Points equal to your Intelligence modifier + double your level.",
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
                                                CommonRuneRules.CreateInvokeAction(thisAction, caster, runeQf, runeQf.Rune);
                                            if (invokeThisRune != null)
                                                GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, divineOptions,
                                                    false);
                                        }
                                        else if (includePrimal && !includeDivine && runeQf.IsPrimalRune())
                                        {
                                            CombatAction? invokeThisRune =
                                                CommonRuneRules.CreateInvokeAction(thisAction, caster, runeQf, runeQf.Rune);
                                            if (invokeThisRune != null)
                                                GameLoop.AddDirectUsageOnCreatureOptions(invokeThisRune, primalOptions,
                                                    false);
                                        }
                                        else if (includeDivine && includePrimal &&
                                            (runeQf.IsDivineRune() || runeQf.IsPrimalRune()))
                                        {
                                            CombatAction? invokeThisRune =
                                                CommonRuneRules.CreateInvokeAction(thisAction, caster, runeQf, runeQf.Rune);
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
                            foreach (Rune rune in RunesmithRunes.AllRunes)
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
                    CommonRuneRules.WithImmediatelyRemovesImmunity(vci);
                    
                    return new ActionPossibility(vci);
                };
            });
        ModManager.AddFeat(vitalCompositeInvocation);
        
        Feat wordsFlyFree = new TrueFeat(
                ModData.FeatNames.WordsFlyFree,
                6,
                "Just because your runes are tattooed on your body doesn't mean they need to remain there.",
                "{b}Requirements{/b} Your Runic Tattoo is not faded.\n\nYou fling your hand out, the rune from your "+ModTooltips.FeatsRunicTattoo+"Runic Tattoo{/} flowing down it and flying through the air in a crescent. You "+ModTooltips.ActionTraceRune+"Trace the Rune{/} onto all creatures or objects within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.",
                [Trait.Manipulate, ModData.Traits.Runesmith])
            .WithActionCost(1)
            .WithPrerequisite(ModData.FeatNames.RunicTattoo, "Runic Tattoo")
            .WithPermanentQEffect("You can expend your Runic Tattoo by tracing it on all valid targets in a 15-foot cone.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains("RunicTattoo") || runicTattoo.Subfeats == null)
                            return null;
                        
                        Feat? selectedTattoo = null;
                        foreach (Feat ft in runicTattoo.Subfeats.Where(ft => qfThis.Owner.HasFeat(ft.FeatName)))
                            selectedTattoo = ft;

                        if (selectedTattoo is not { Tag: Rune tattooedRune })
                            return null;
                        
                        CombatAction flyFreeAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(selectedTattoo.Illustration ?? IllustrationName.Action, IllustrationName.SeekCone),
                            "Words, Fly Free",
                            [Trait.Manipulate, ModData.Traits.Runesmith, ModData.Traits.Traced, Trait.Basic],
                            "{i}Just because your runes are tattooed on your body doesn't mean they need to remain there.{/i}\n\n{b}Requirements{/b} Your Runic Tattoo is not faded.\n\nYou fling your hand out, the rune from your Runic Tattoo flowing down it and flying through the air in a crescent. You trace the rune onto all creatures or objects within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.",
                            Target.Cone(3))
                            .WithActionCost(1)
                            .WithProjectileCone(VfxStyle.BasicProjectileCone(selectedTattoo.Illustration ?? IllustrationName.Action))
                            .WithSoundEffect(ModData.SfxNames.WordsFlyFree)
                            .WithEffectOnEachTarget( async (thisAction, caster, target, result) =>
                            {
                                await CommonRuneRules.DrawRuneOnTarget(thisAction, caster, target, tattooedRune, false);
                                
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
        ModManager.AddFeat(wordsFlyFree);
        
        /////////////////////
        // 8th Level Feats //
        /////////////////////
        Feat drawnInRed = new TrueFeat(
                ModData.FeatNames.DrawnInRed,
                8,
                "After striking the target, you run a brush or finger along your weapon to collect a bit of its blood.",
                "{b}Requirements{/b} Your last action was a successful melee Strike that dealt physical damage.\n\nFor the encounter, when you "+ModTooltips.ActionTraceRune+"Trace a Rune{/} and the target is that creature, you can do so at a range of 60 feet as a single "+RulesBlock.GetIconTextFromNumberOfActions(1)+" action. Using Drawn in Red against a different creature ends the benefits against the previous creature.",
                [ModData.Traits.Runesmith])
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
                        ModData.Illustrations.DrawnInRed,
                        "Drawn In Red",
                        [ModData.Traits.Runesmith, Trait.Basic],
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
                                Id = ModData.QEffectIds.DrawnInRed,
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
                    foreach (Rune rune in RunesmithRunes.AllRunes)
                    {
                        if (section.Name == rune.Name)
                            foundRune = rune;
                    }

                    if (foundRune is null)
                        return null;

                    if (foundRune.UsageCondition?.Invoke(qfFeat.Owner, bloodTarget) == Usability.Usable)
                    {
                        CombatAction bloodTrace = CommonRuneRules.CreateTraceAction(qfThis.Owner, foundRune, 2, 12)
                            .WithActionCost(1)
                            .WithExtraTrait(Trait.Basic);
                        ((CreatureTarget)bloodTrace.Target).WithAdditionalConditionOnTargetCreature(
                            (attacker, defender) => defender == bloodTarget
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature("not Drawn In Blood target"));
                        bloodTrace.Name = bloodTrace.Name
                            .Replace("Trace", "Draw")
                            .Replace("Sing", "Draw & Sing");
                        bloodTrace.Description = CommonRuneRules.CreateTraceActionDescription(
                            bloodTrace,
                            foundRune,
                            prologueText:"{Blue}{b}Range{/b} 60 feet{/Blue}\n" + (qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger) ? "{Blue}{b}Frequency{/b} Once per combat (Rune-Singer){/Blue}\n" : null),
                            withFlavorText: true,
                            afterUsageText:$" {{Blue}}(only against {bloodTarget.Name}){{/Blue}}");
                        bloodTrace.ContextMenuName = "{icon:Action} " + bloodTrace.Name;
                        ActionPossibility bloodPossibility = new ActionPossibility(bloodTrace)
                        {
                            Caption = "Drawn In Red",
                            Illustration = new SideBySideIllustration(IllustrationName.Action, ModData.Illustrations.DrawnInRed),
                        };
                        return bloodPossibility;
                    }
                    
                    return null;
                };
            });
        ModManager.AddFeat(drawnInRed);

        Feat earlyAccess = new TrueFeat(
            ModData.FeatNames.EarlyAccess,
            8,
            "Through intense dedication, you've gained knowledge heretofore unseen at your stage of academic acumen.",
            "Add a level 9 Rune to your runic repertoire, without needing to meet the level requirement.",
            [Trait.Homebrew, ModData.Traits.Runesmith])
            .WithOnSheet(values =>
            {
                values.AddSelectionOptionRightNow(new SingleFeatSelectionOption("earlyAccessRune", "Level 9 rune", 8, ft => ft is RuneFeat { Rune.BaseLevel: 9 }));
            });
        ModManager.AddFeat(earlyAccess);
        
        // TODO: item tooltips
        Feat elementalRevision = new TrueFeat(
                ModData.FeatNames.ElementalRevision,
                8,
                "You can scratch out and rewrite part of an elemental rune to temporarily change the type of power it channels.",
                "You touch an adjacent {i}corrosive{/i}, {i}flaming{/i}, {i}frost{/i}, {i}shock{/i}, or {i}thundering{/i} property rune on an item held by an ally, and you change it to any other property rune from that list. The revision lasts until the end of combat, before the property rune's original magic reasserts itself. When you do so, the ally wielding the item becomes immune to this ability until your next daily preparations.",
                [ModData.Traits.Runesmith])
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
                        [ModData.Traits.Runesmith],
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
                                                    Sfxs.Play(ModData.SfxNames.ElementalRevision);
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
        ModManager.AddFeat(elementalRevision);
        
        Feat readTheBones = new TrueFeat(
                ModData.FeatNames.ReadTheBones,
                8,
                "Using ancient scripts, you carve a question into bone before casting it into fire, where it cracks.",
                "You gain a permanent +1 status bonus to initiative rolls.",
                [ModData.Traits.Runesmith])
            .WithPermanentQEffect("You gain a +1 status bonus to initiative rolls.", qfFeat =>
            {
                qfFeat.BonusToInitiative = qfThis => new Bonus(1, BonusType.Status, "Read The Bones");
            });
        ModManager.AddFeat(readTheBones);
    }
}