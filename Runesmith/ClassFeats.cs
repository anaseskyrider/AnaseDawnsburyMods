using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CharacterBuilder.Selections;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Controls.Portraits;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;
using Dawnsbury.Mods.RunesmithClass.TargetingRequirements;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass;

public static class ClassFeats
{
    public const string RUNIC_TATTOO_KEY = "RUNIC_TATTOO";
    
    public static void LoadFeats()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        #region 1st-Level
        
        // Backup Runic Enhancement
        yield return new TrueFeat(
                ModData.FeatNames.BackupRunicEnhancement, 1,
                "While you are not a spellcaster, you have a working knowledge of the most fundamental runic magic.",
                $"Choose either {SpellId.MagicFang.ToLink("runic body", Trait.Runesmith, null)} or {SpellId.MagicWeapon.ToLink("runic weapon", Trait.Runesmith, null)}. You can cast this spell once per day as an innate spell, and its rank is equal to half your level, rounded up.",
                [Trait.Runesmith])
            .WithOnSheet(values =>
            {
                Trait origin = Trait.Runesmith;
                values.SetProficiency(Trait.Spell, Proficiency.Trained);
                InnateSpells innates = values.InnateSpells.GetOrCreate(
                    origin,
                    // Ease of implementation, the tradition is always arcane
                    () => new InnateSpells(Trait.Arcane));
                values.AddSelectionOptionRightNow(new AddInnateSpellOption(
                    "BackupRunicEnhancement",
                    "Backup Runic Enhancement",
                    -1,
                    origin,
                    values.MaximumSpellLevel,
                    spell => spell.SpellId is SpellId.MagicWeapon or SpellId.MagicFang));
            })
            .WithOnCreature(self =>
            {
                if (self.Spellcasting?.GetSourceByOrigin(Trait.Runesmith)
                    is not { } source)
                    return;
                
                /*SpellcastingSource source = self.GetOrCreateSpellcastingSource(
                    SpellcastingKind.Innate,
                    Trait.Runesmith,
                    Ability.Charisma,
                    Trait.Arcane) // Ease of implementation, the tradition is always arcane
                    /*.WithSpells(
                        [SpellId.MagicWeapon, SpellId.MagicFang],
                        creature.MaximumSpellRank)#1#;*/

                if (source.Spells.FirstOrDefault(spell =>
                        spell.SpellId is SpellId.MagicWeapon or SpellId.MagicFang) 
                    is not { } first)
                    return;

                source.Spells.RemoveFirst(spell => spell.SpellId == first.SpellId);
                source.WithSpells([first.SpellId], self.MaximumSpellRank);
            })
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Arcana) || values.HasFeat(FeatName.Nature) || values.HasFeat(FeatName.Occultism) || values.HasFeat(FeatName.Religion),
                "You must be trained in Arcana, Nature, Occultism, or Religion");
        
        // Engraving Strike
        yield return new TrueFeat(
                ModData.FeatNames.EngravingStrike, 1,
                "You draw a rune onto the surface of your weapon in reverse, the mark branding or bruising itself into your target at the moment of impact.",
                $$"""
                  {b}Requirements{/b} You are wielding a melee weapon.

                  Make a melee Strike with the weapon. On a success, you {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} onto the target{{ModData.Tooltips.InfoEngravingStrikeTarget}} of the Strike.
                  """,
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ => "{b}Engraving Strike {icon:Action}{/b} Make a melee Strike. On a hit, Trace a Rune on the target.";
                
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee) || item.HasTrait(Trait.Unarmed))
                        return null;

                    StrikeModifiers strikeMods = new StrikeModifiers();
                    
                    CombatAction engravingStrike = qfFeat.Owner.CreateStrike(
                            item, -1, strikeMods)
                        .WithStrikeNameAndIllustrationChange(
                            "Engraving Strike",
                            ModData.Illustrations.TraceRune,
                            false)
                        .WithActionCost(1)
                        .WithExtraTrait(0, ModData.ModTrait)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(Trait.ProvokesAfterActionCompletion)
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                                strikeMods,
                                additionalSuccessText: "Trace a Rune onto the target."))
                        .WithActionId(ModData.ActionIds.EngravingStrike)
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            if (result < CheckResult.Success)
                                return;
                            
                            // So that you aren't blocking the target's square during the trace await
                            await caster.FictitiousSingleTileMoveBack();

                            // If this Strike kills the target, don't trace a rune,
                            // but also don't get the chance to convert to a simple Strike 
                            if (!target.DeathScheduledForNextStateCheck)
                            {
                                if (await CommonRuneRules.AskToTraceARune(caster,
                                            // Must target creatures
                                            runeFilter: rune => rune.DrawProperties.IsDrawnOnlyOnCreatures,
                                            targetFilter: cr => cr == target,
                                            canBeCanceled: true)
                                        is not { } chosenOption
                                    || chosenOption is CancelOption or PassViaButtonOption)
                                {
                                    caster.Battle.Log("Engraving Strike was converted to a simple Strike.");
                                    action.Traits.Remove(Trait.Flourish);
                                }
                            }
                        })
                        .WithAdjustTarget<CreatureTarget>(crTar =>
                        {
                            var cache = crTar.CreatureGoodness;
                            crTar.CreatureGoodness = AI.CreateModifiedGoodness((target, self, defender) =>
                            {
                                // Minimum invokeable rune damage of 1d4, or 2.5 avg.
                                // Doesn't account for saving throw accuracy but this
                                // is a high-value action anyway.
                                float bonus = self.MaximumSpellRank * 2.5f;
                                return cache(target, self, defender)
                                       + bonus;
                            });
                        });
                    
                    return engravingStrike;
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory(RequiresEngraving);
        
        // Glyph Familiar
        yield return new TrueFeat(
                ModData.FeatNames.GlyphFamiliar, 1,
                "You have taken a living rune as a familiar to aid you in your adventures.",
                $"You gain a {FeatName.ClassFamiliar.ToLink("combat familiar")}. You choose one ability per day instead of two, but it always has three additional familiar abilities prepared: {{link:Familiars.FamiliarAbilityConstruct}}construct{{/}}, {{link:Familiars.FamiliarAbilityFlier}}flier{{/}}, and {{link:Familiars.FamiliarAbilityTough}}tough{{/}}."
                + (ModLoader.FamiliarCreature.HasValue ? null : $"\n\n{ModData.Illustrations.DdSun} {{b}}Modding{{/b}} This feat is designed to work with the {{link:https://steamcommunity.com/sharedfiles/filedetails/?id=3508129973}}Deployable Familiars{{/}} mod. Without it, this grants two abilities instead of one, but grants no free abilities."),
                [Trait.Runesmith, ModData.Traits.DeployableFamiliarFeat])
            .WithIllustration(ModData.Illustrations.RuneFamiliar)
            .WithEquivalent(values => values.Tags.ContainsKey(Familiars.FAMILIAR_KEY))
            .WithOnSheet(values =>
            {
                FamiliarTag runey = new FamiliarTag()
                {
                    // Fewer choices than normal due to pre-selected ability.
                    // Grants standard 2 abilities if you don't have Deployable Familiars.
                    FamiliarAbilities = 1 + (ModLoader.FamiliarCreature.HasValue ? 0 : 1),
                    Illustration = ModData.Illustrations.RuneFamiliar,
                };
                values.Tags[Familiars.FAMILIAR_KEY] = runey;
                // Display
                values.AddSelectionOptionRightNow(
                    new SingleFeatSelectionOption(
                            "FamiliarIllustrationDisplay",
                            "Show familiar",
                            -1,
                            ft => ft.HasTrait(Trait.FamiliarIllustrationDisplay))
                        .WithIsOptional());
                // Identity
                values.AddSelectionOptionRightNow(
                    new CompanionIdentitySelectionOption(
                            "FamiliarName",
                            "Familiar identity",
                            -1,
                            "You can name your familiar.\n\nIf you don't choose a name, it will be called {b}Runey{/b}.",
                            "Runey",
                            ModData.Illustrations.RuneFamiliar,
                            [PortraitCategory.Familiars, PortraitCategory.AnimalCompanions, PortraitCategory.Custom],
                            (val, txt) =>
                            {
                                if (!val.Tags.TryGetValue(Familiars.FAMILIAR_KEY, out object? obj)
                                    || obj is not FamiliarTag famTag)
                                    return;
                                CompanionIdentitySelectionOption.SetFamiliarDataFromSection(famTag, txt);
                            })
                        .WithIsOptional());
                values.AtEndOfRecalculationBeforeMorningPreparations += values2 =>
                {
                    if (!values2.Tags.TryGetValue(Familiars.FAMILIAR_KEY, out object? obj)
                        || obj is not FamiliarTag famTag2)
                        return;
                    values2.HasMorningPreparations = true;
                    values2.AddSelectionOption(
                        new MultipleFeatSelectionOption(
                            "FamiliarAbilities",
                            "Familiar abilities",
                            SelectionOption.MORNING_PREPARATIONS_LEVEL,
                            (ft, values3) =>
                            {
                                if (!ft.HasTrait(Trait.CombatFamiliarAbility))
                                    return false;
                                if (ft.Tag is not Trait tag2
                                    || values3.AdditionalClassTraits.Contains(tag2))
                                    return true;
                                ClassSelectionFeat? classSelectionFeat = values3.Class;
                                return classSelectionFeat != null
                                       && classSelectionFeat.ClassTrait == tag2;
                            },
                            famTag2.FamiliarAbilities)
                        {
                            DoNotApplyEffectsInsteadOfRemovingThem = true
                        });
                };
                // Granted abilities
                //// Grant Tough in order to meet prereqs first.
                if (ModManager.TryParse("Familiars.FamiliarAbilityTough", out FeatName toughFamiliar))
                    values.GrantFeat(toughFamiliar);
                if (ModManager.TryParse("Familiars.FamiliarAbilityFlier", out FeatName flyFamiliar))
                    values.GrantFeat(flyFamiliar);
                if (ModManager.TryParse("Familiars.FamiliarAbilityConstruct", out FeatName constructFamiliar))
                    values.GrantFeat(constructFamiliar);
            });
        
        // Remote Detonation
        yield return new TrueFeat(
                ModData.FeatNames.RemoteDetonation, 1,
                "You whisper an invocation over your ammunition as you shoot it, and the hissing of the projectile sounds just like your murmured voice.",
                // Make a ranged Strike that uses physical ammunition
                $$"""
                Make a ranged Strike that uses ammunition against a target within the first range increment of your weapon. If it hits, you {{ModData.FeatNames.InvokeRune.ToLink("Invoke Runes")}} on the target as whisper of the ammunition's flight sets them off. You can invoke up to two runes on the target of your Strike in this way. On a critical hit, the target takes a –1 circumstance penalty on any saving throws against the runes invoked by your Remote Detonation.
                """,
                [Trait.Flourish, Trait.Invocation, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ =>
                    "{b}Remote Detonation {icon:Action}{/b} Make a ranged Strike. On a hit, Invoke up to 2 Runes on the target. On a crit, it has a -1 circumstance penalty to their saves against these invocations.";
                
                // Must use physical ammunition. Ranged thrown attacks not included.
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (item.HasTrait(Trait.Unarmed)
                        || !item.HasTrait(Trait.Ranged)
                        || item.WeaponProperties == null
                        || item.WeaponProperties.RangeIncrement == -1)
                        return null;

                    StrikeModifiers strikeMods = new StrikeModifiers();
                    
                    CombatAction remoteDet = qfFeat.Owner.CreateStrike(
                            item, -1, strikeMods)
                        .WithStrikeNameAndIllustrationChange(
                            "Remote Detonation",
                            ModData.Illustrations.InvokeRune,
                            false)
                        .WithActionCost(1)
                        .WithExtraTrait(0, ModData.ModTrait)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(Trait.Invocation)
                        .WithExtraTrait(Trait.Runesmith)
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            strikeMods,
                            additionalSuccessText: " Invoke up to 2 Runes on the target.",
                            additionalCriticalSuccessText: " The target also has a -1 circumstance penalty to any saving throws against these invocations."))
                        .WithAdjustTarget<CreatureTarget>(crTar => crTar
                            .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                            {
                                if (DrawnRune.GetDrawnRunes(attacker, defender).Count == 0)
                                    return Usability.NotUsableOnThisCreature("not a rune-bearer");
                                if (attacker.DistanceTo(defender) > item.WeaponProperties.RangeIncrement)
                                    return Usability.CommonReasons.TargetOutOfRange;
                                return Usability.Usable;
                            }))
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            if (result < CheckResult.Success)
                                return;
                            
                            if (result == CheckResult.CriticalSuccess)
                            {
                                QEffect detPenalty = new QEffect()
                                {
                                    Name = "Remote Detonation Critical Success",
                                    ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn,
                                    BonusToDefenses = (qfPenalty, invokeAction, defense) =>
                                    {
                                        if (invokeAction != null
                                            && invokeAction.HasTrait(Trait.Invocation)
                                            && defense.IsSavingThrow())
                                        {
                                            return new Bonus(-1, BonusType.Circumstance, "Remote Detonation Critical Success");
                                        }

                                        return null;
                                    },
                                };
                                
                                target.AddQEffect(detPenalty);
                            }

                            if (target.DeathScheduledForNextStateCheck)
                                return;

                            for (int i=0; i < 2; i++)
                            {
                                bool invoked = await CommonRuneRules.ChooseARuneToInvoke(
                                    caster,
                                    targetFilter: cr => cr == target,
                                    overrideRange: item.WeaponProperties.RangeIncrement,
                                    passText: "Don't invoke any runes",
                                    additionalTopText: $" ({i+1}/2)");
                                if (i > 0 || invoked)
                                    continue;
                                caster.Battle.Log("Remote Detonation was converted to a simple Strike.");
                                action.Traits.Remove(Trait.Flourish);
                                break;
                            }

                            target.RemoveAllQEffects(qf => qf.Name == "Remote Detonation Critical Success");
                        });

                    remoteDet = CommonRuneRules.WithImmediatelyRemovesImmunity(remoteDet);

                    return remoteDet;
                };
            })
            .WithInappropriateBecauseOfBadInventory(RequiresPhysicalProjectile);
        
        // Rune Ward
        yield return new TrueFeat(
                ModData.FeatNames.RuneWard, 1,
                "You quickly sketch a rune in the air to gain a modicum of protection against the triggering magic.",
                """
                {b}Trigger{/b} You are targeted by a spell and are trained in the skill associated with its tradition.
                
                You gain a +1 circumstance bonus to your saving throw and AC against the spell.
                """,
                [Trait.Runesmith])
            .WithActionCost(-2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " When you are targeted by a spell whose tradition skill you're trained in, gain a +1 circumstance bonus to your defenses against it.";
                
                qfFeat.YouAreTargeted = async (qfThis, action) =>
                {
                    if (!action.HasTrait(Trait.Spell)
                        || action.SpellcastingSource is null)
                        return;
                    
                    bool isTrained = action.SpellcastingSource.SpellcastingTradition switch
                    {
                        Trait.Arcane when IsTrained(Trait.Arcana) => true,
                        Trait.Divine when IsTrained(Trait.Religion) => true,
                        Trait.Primal when IsTrained(Trait.Nature) => true,
                        Trait.Occult when IsTrained(Trait.Occultism) => true,
                        _ => false,
                    };
                    if (!isTrained)
                        return;

                    if (await qfThis.Owner.AskToUseReaction(
                            $$"""
                              {b}Rune Ward {icon:Reaction}{/b}
                              You have been targeted by {{action.Owner.ToColoredBoldedName()}}'s {{action.Name.WithColor("Blue")}}. Gain a +1 circumstance bonus to your AC and saving throws against this spell?
                              """,
                            ModData.Illustrations.RuneWard))
                        qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfAnyTurn)
                        {
                            BonusToDefenses = (qfDef, action2, def) =>
                                action2 == action
                                && (def is Defense.AC || def.IsSavingThrow())
                                    ? new Bonus(1, BonusType.Circumstance, "Rune Ward")
                                    : null,
                        });
                };

                return;

                bool IsTrained(Trait trait) =>
                    qfFeat.Owner.Proficiencies.Get(trait) >= Proficiency.Trained;
            });
        
        // Rune-Singer
        yield return new TrueFeat(
                ModData.FeatNames.RuneSinger, 1,
                "You practice the lost art of using music to guide your rune-carving, singing the runes into existence as much as crafting them.",
                /*"You can use Performance instead of Crafting when attempting Crafting checks related to runes. " + */
                $"Once per combat, you can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with song alone, removing the manipulate trait from Trace Rune, and allowing you to use the {{icon:TwoActions}} 2-action version of Trace Rune as a single {{icon:Action}} action." /*+" You don't need to be able to move your hands when Tracing a Rune using song, but you do need to be able to sing in a clear voice."*/,
                [Trait.Runesmith])
            .WithPermanentQEffect(
                "Once per combat, you can Trace a Rune without the manipulate trait on a target up to 30 feet away as {icon:Action} a single action.",
                qfFeat =>
                {
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.GenerationalRuneSinger))
                    {
                        qfFeat.Id = ModData.QEffectIds.RuneSinger;
                        qfFeat.Name = "Generational Rune-Singer";
                        qfFeat.Description =
                            "Trace Rune always costs {icon:Action} 1 action, loses the manipulate trait, and has a range of 60 feet.";
                        return;
                    }

                    bool isProdigal = qfFeat.Owner.HasFeat(ModData.FeatNames.ProdigalRuneSinger);
                    if (isProdigal)
                    {
                        qfFeat.Description = qfFeat.Description!
                            .Replace("Once per combat", "Once per round");
                    }
                    
                    qfFeat.Id = ModData.QEffectIds.RuneSingerCreator;
                    
                    qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                    {
                        if (submenu.SubmenuId != ModData.SubmenuIds.TraceRune
                            || qfThis.UsedUpPermanently)
                            return null;

                        bool singerIsActive = qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger);

                        CombatAction toggleSinger = new CombatAction(
                                qfThis.Owner,
                                new CornerIllustration(
                                    ModData.Illustrations.RuneSinger,
                                    singerIsActive
                                        ? ModData.Illustrations.CheckSymbol
                                        : ModData.Illustrations.NoSymbol,
                                    Direction.Southwest),
                                $"Rune-Singer ({(singerIsActive ? "disable" : "enable")})",
                                [ModData.ModTrait, Trait.Runesmith],
                                $$"""
                                {i}You practice the lost art of using music to guide your rune-carving, singing the runes into existence as much as crafting them.{/i}

                                {b}Frequency{/b} Once per {{(isProdigal ? "{Blue}round{/Blue}" : "encounter")}}.
                                
                                The next time you Trace a Rune will be with song alone, removing the manipulate trait from Trace Rune, and allowing you to use the {icon:TwoActions} 2-action version of Trace Rune as a single {icon:Action} action.
                                """,
                                Target.Self()
                                    // Prodigal Rune-Singer, if present, will set this.
                                    .WithAdditionalRestriction(self =>
                                        qfThis.UsedThisTurn
                                        ? "Once per round"
                                        : null))
                            .WithActionCost(0)
                            .WithSoundEffect(ModData.SfxNames.TOGGLE_RUNE_SINGER)
                            .WithEffectOnSelf(self =>
                            {
                                if (singerIsActive)
                                    self.RemoveAllQEffects(qf =>
                                        qf.Id == ModData.QEffectIds.RuneSinger);
                                else
                                {
                                    QEffect singerEffect = new QEffect(
                                        "Rune-Singer",
                                        "{Red}{b}Frequency{/b} Once per combat{/Red}\nThe next time you Trace a Rune, it won't have the manipulate trait, and you can use the 2-action version as a single action.",
                                        ExpirationCondition.Never,
                                        self,
                                        ModData.Illustrations.RuneSinger)
                                    {
                                        Id = ModData.QEffectIds.RuneSinger,
                                        DoNotShowUpOverhead = true,
                                    };
                                    self.AddQEffect(singerEffect);
                                }
                            });

                        return new PossibilitySection("Rune-Singer")
                        {
                            PossibilitySectionId = ModData.PossibilitySectionIds.RuneSinger,
                            Possibilities =
                            {
                                new ActionPossibility(toggleSinger)
                                {
                                    Caption = $"Rune-Singer ({(singerIsActive ? "on" : "off")})"
                                }
                            }
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
                })
            .WithTraceRunePrereq()
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Performance),
                "You must be trained in Performance.");
        
        // TODO: Seek the Hidden Glyphs
        
        // Smithing Weapons
        yield return new TrueFeat(
                ModData.FeatNames.SmithingWeapons, 1,
                "Though you are an artisan, you are well versed in using the tools of your trade to fend off enemies.",
                """
                While wielding a weapon in the hammer, knife, or pick weapon group, you can add its item bonus to attack rolls to your Crafting checks.

                Additionally, your Strikes with such weapons deal 1 additional fire damage to enemies bearing one of your runes, as sparks fly on impact like a hammer to an anvil.
                """,
                [Trait.Runesmith])
            .WithPermanentQEffect(
                "Your hammer/knife/pick Strikes deal +1 fire damage to bearers of your runes. Your Crafting checks gain these weapons' item bonus to hit.",
                qfFeat =>
                {
                    qfFeat.AddExtraKindedDamageOnStrike = (action, target) =>
                    {
                        if (action.Item is null
                            || !action.Item.Traits.ContainsOneOf([Trait.Hammer, Trait.Knife, Trait.Pick])
                            || !DrawnRune.IsARuneBearer(action.Owner, target))
                            return null;
                        return new KindedDamage(
                            DiceFormula.FromText("1", "Smithing Weapons"),
                            DamageKind.Fire);
                    };
                    qfFeat.BonusToSkillChecks = (skill, action, _) =>
                        skill is Skill.Crafting
                        && action.Owner.HeldItems.MaxOrZero(item => item.WeaponProperties?.ItemBonus ?? 0)
                            is var bonus and > 0
                            ? new Bonus(bonus, BonusType.Item, "Smithing Weapons")
                            : null;
                })
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                FeatInventoryRequirements.RequiresOne(
                    inventory,
                    item => item.Traits.ContainsOneOf([Trait.Hammer, Trait.Knife, Trait.Pick]),
                    "a weapon in the hammer, knife, or pick weapon group"));
        
        #endregion
        
        #region 2nd-Level
        
        // Enhanced Glyph Familiar
        yield return new TrueFeat(
                ModData.FeatNames.EnhancedGlyphFamiliar, 2,
                "Your living rune is imbued with more magical power.",
                $"You can select one additional familiar ability each day. In addition, when you {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with {{icon:Action}} a single action, your target can be adjacent to your familiar instead of adjacent to you. Your familiar must be within 30 feet of you for this benefit."
                + (ModLoader.FamiliarCreature.HasValue ? null : $"\n\n{ModData.Illustrations.DdSun} {{b}}Modding{{/b}} This feat is designed to work with the {{link:https://steamcommunity.com/sharedfiles/filedetails/?id=3508129973}}Deployable Familiars{{/}} mod. Without it, this grants two additional abilities instead of one, but grants no free abilities."),
                [Trait.Runesmith])
            // I generally prefer "they" over "it", but the singular
            // helps remove some amount of ambiguity on the sentence subject.
            // The familiar must be within 30 feet, not your Trace targets.
            .WithPermanentQEffect(
                "You can Trace a Rune {icon:Action} from your familiar's space if it's within 30 feet.",
                qfFeat =>
                {
                    if (!ModLoader.FamiliarCreature.HasValue
                        || !qfFeat.Owner.HasFeat(ModData.FeatNames.TraceRune))
                        return;
                    
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (AllRunes.All.FirstOrDefault(rune =>
                                    rune.FullName == section.Name)
                                is not { } foundRune)
                            return null;

                        Creature? familiar = qfThis.Owner.Battle.AllCreatures.FirstOrDefault(cr =>
                            cr.QEffects.Any(qf =>
                                qf.Id == ModLoader.FamiliarCreature.Value
                                && qf.Source == qfThis.Owner));

                        CombatAction familiarTrace = CommonRuneRules
                            .CreateTraceAction(
                                qfThis.Owner, foundRune, 1, null,
                                "Glyph Rune", "redirects")
                            .WithAdjustTarget<CreatureTarget>(crTar =>
                            {
                                crTar.CreatureTargetingRequirements.RemoveAll(req =>
                                    req is NaturalReachCreatureTargetingRequirement
                                        or MaximumRangeCreatureTargetingRequirement);
                                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                                    familiar is null
                                        ? Usability.NotUsable("Familiar doesn't exist")
                                        : Usability.Usable);
                                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                                    familiar is null || !familiar.Actions.CanTakeActions()
                                        ? Usability.NotUsable("Familiar can't take actions")
                                        : Usability.Usable);
                                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                                    familiar is null || a.DistanceTo(familiar) > 6
                                        ? Usability.NotUsable("Familiar out of range")
                                        : Usability.Usable);
                                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                                    familiar is null || !(d.DistanceTo(familiar) <= 1)
                                        ? Usability.NotUsableOnThisCreature("Not adjacent to familiar")
                                        : Usability.Usable);
                                crTar.WithAdditionalConditionOnTargetCreature(
                                    new UnblockedLineOfEffectCreatureTargetingRequirement());
                                crTar.AlternateTileOfOrigin = familiar?.Space.CenterTile;
                            });
                        familiarTrace.Illustration = new SideBySideIllustration(
                            familiarTrace.Illustration,
                            ModData.Illustrations.RuneFamiliar);
                        familiarTrace.AlternateCreatureOfOrigin = familiar;
                        familiarTrace.Description = CommonRuneRules.StatBlocks.CreateDrawActionDescription(
                            foundRune, familiarTrace,
                            customRangeLine: "Adjacent to your glyph familiar");
                        familiarTrace.ContextMenuName = $"{{icon:Action}} {familiarTrace.ContextMenuName}";

                        return new ActionPossibility(familiarTrace)
                        {
                            Caption = "From Glyph Familiar",
                            Illustration = ModData.Illustrations.RuneFamiliar,
                        };
                    };
                })
            .WithOnSheet(values =>
            {
                if (!values.Tags.TryGetValueAs("CombatFamiliar", out FamiliarTag? famTag)
                    || famTag is null)
                    return;
                famTag.FamiliarAbilities += 1 + (ModLoader.FamiliarCreature.HasValue ? 0 : 1);
            })
            .WithPrerequisite(ModData.FeatNames.GlyphFamiliar, "Glyph Familiar");
        
        // Fortifying Knock
        yield return new TrueFeat(
                ModData.FeatNames.FortifyingKnock, 2,
                "Your shield is a natural canvas for your art.",
                $$"""
                  {b}Requirements{/b} You are wielding a shield.

                  In one motion, you Raise a Shield and {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} on your shield.
                  """,
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis => "{b}Fortifying Knock {icon:Action}{/b} [flourish] Raise a Shield and Trace a Rune on your shield."
                    + (qfThis.Owner.HasFeat(ModData.FeatNames.RunicReprisal) ? " {Blue}You can also trace a damaging rune, which is invoked onto the attacker when you Shield Block an adjacent melee Strike.{/Blue}" : null);
                
                qfFeat.ProvideBonusRaiseShieldPossibility = (qfThis, shield) =>
                {
                    if (RunicRepertoireTag.GetRepertoire(qfThis.Owner) is not {} repertoire)
                        return null;
                    
                    Illustration shieldIll = shield.Illustration;

                    PossibilitySection fortSection = new PossibilitySection("Fortifying Knock")
                    {
                        PossibilitySectionId = ModData.PossibilitySectionIds.FortifyingKnock,
                        Possibilities = repertoire.GetTraceableRunes(qfThis.Owner)
                            .Where(rune => rune.DrawProperties.IsDrawnOnThisItem(qfThis.Owner, Trait.Shield))
                            .Select(rune => CreateFortifyingKnockAction(
                                qfThis.Owner,
                                rune,
                                shield,
                                null, null))
                            .Select(action => new ActionPossibility((action)))
                            .Cast<Possibility>()
                            .ToList()
                    };
                    
                    SubmenuPossibility fortifyingKnockSubmenu = new SubmenuPossibility(
                        new SideBySideIllustration(
                            shieldIll, 
                            ModData.Illustrations.TraceRune),
                        "Fortifying Knock")
                    {
                        SubmenuId = ModData.SubmenuIds.FortifyingKnock,
                        // This doesn't DO anything, it's just to provide description to the menu.
                        SpellIfAny = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                shieldIll,
                                ModData.Illustrations.TraceRune),
                            "Fortifying Knock",
                            [ModData.ModTrait, Trait.Flourish, Trait.Runesmith],
                            """
                            {i}Your shield is a natural canvas for your art.{/i}

                            {b}Requirements{/b} You are wielding a shield.

                            In one motion, you Raise a Shield and Trace a Rune on your shield.
                            """,
                            Target.Self()),
                        Subsections = { fortSection },
                        Tag = shield, // Unused
                    };
                    
                    return fortifyingKnockSubmenu;
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory(FeatInventoryRequirements.RequiresShield);
        
        // Invisible Ink
        yield return new TrueFeat(
                ModData.FeatNames.InvisibleInk, 2,
                "Your ink is as vanishing as your movements.",
                $$"""
                  Use the {icon:TwoActions} 2-action version of {{ModData.FeatNames.TraceRune.ToLink("Trace Rune")}}, then attempt to Hide or Sneak.

                  {b}Special{/b} Tracing a Rune doesn't cause you to cease being hidden.
                  """,
                [Trait.Runesmith, Trait.Rebalanced])
            .WithActionCost(2)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ =>
                    "{b}Invisible Ink {icon:TwoActions}{/b} Trace a Rune as 2 actions, then Hide or Sneak.";
                
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (action.HasTrait(Trait.Traced)
                        || action.HasTrait(Trait.Etched))
                        action.WithExtraTrait(Trait.DoesNotBreakStealth);
                };
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction inkHide = CreateInvisibleInkAction(
                            qfThis.Owner,
                            "Hide",
                            IllustrationName.Hide,
                            "Hide",
                            self =>
                            {
                                if (HiddenRules.IsHiddenFromAllEnemies(self))
                                    return "You're already hidden from all enemies.";
                                return self.Battle.AllCreatures.Any(cr =>
                                    cr.EnemyOf(self)
                                    && cr.Space.Tiles.Any(tile => tile.FogOfWar != FogOfWar.Blackened)
                                    && HiddenRules.CountsAsHavingCoverOrConcealment(self, cr))
                                    ? null
                                    : "You don't have cover or concealment from any enemy.";
                            },
                            async self =>
                                await self.Battle.GameLoop.FullCast(CommonStealthActions
                                    .CreateHide(self)
                                    .WithActionCost(0)));
                    
                    CombatAction inkSneak = CreateInvisibleInkAction(
                            qfThis.Owner,
                            "Sneak",
                            IllustrationName.Sneak64,
                            "Sneak",
                            self =>
                                self.DetectionStatus.IsHiddenToAnEnemy ? null : "You're not hidden",
                            async self =>
                                await self.Battle.GameLoop.FullCast(CommonStealthActions
                                        .CreateSneak(self)
                                        .WithActionCost(0)));
                    
                    SubmenuPossibility inkMenu = new SubmenuPossibility(
                        new SideBySideIllustration(
                            ModData.Illustrations.TraceRune,
                            IllustrationName.Hide),
                        "Invisible Ink")
                    {
                        SpellIfAny = CreateInvisibleInkAction(
                            qfThis.Owner,
                            null, null, null, null, null),
                        Subsections = [
                            new PossibilitySection("Invisible Ink")
                            {
                                Possibilities = [
                                    new ActionPossibility(inkHide),
                                    new ActionPossibility(inkSneak)
                                ]
                            }
                        ],
                        PossibilityGroup = ModData.PossibilityGroups.DRAWING_RUNES,
                    };
                    
                    return inkMenu;

                    CombatAction CreateInvisibleInkAction(
                        Creature owner,
                        string? subtitle,
                        Illustration? icon,
                        string? attemptToWhat,
                        Func<Creature,string?>? restriction,
                        Func<Creature, Task>? onSuccess)
                    {
                        CombatAction inkAction = new CombatAction(
                                owner,
                                icon is null
                                    ? new BagOfIllustrationsIllustration(
                                        ModData.Illustrations.TraceRune,
                                        IllustrationName.Hide,
                                        IllustrationName.Sneak64)
                                    : new SideBySideIllustration(
                                        ModData.Illustrations.TraceRune,
                                        icon),
                                "Invisible Ink" + (subtitle is not null ? $" ({subtitle})" : null),
                                [ModData.ModTrait, Trait.Runesmith, Trait.DoesNotBreakStealth, Trait.Basic],
                                $$"""
                                  {i}Your ink is as vanishing as your movements.{/i}

                                  Use the {icon:TwoActions} 2-action version of {{ModData.FeatNames.TraceRune.ToLink("Trace Rune")}}, then attempt to {{(attemptToWhat is null ? "Hide or Sneak" : attemptToWhat.WithColor("Blue"))}}.

                                  {b}Special{/b} Tracing a Rune doesn't cause you to cease being hidden.
                                  """,
                                Target.Self())
                            .WithActionCost(2);

                        if (restriction is not null)
                            inkAction.WithAdjustTarget<SelfTarget>(sTar => sTar
                                .WithAdditionalRestriction(restriction));

                        if (onSuccess is not null)
                            inkAction.WithEffectOnSelf(async (thisAction, self) =>
                            {
                                if (await CommonRuneRules.AskToTraceARune(self)
                                    is CancelOption or PassOption)
                                {
                                    thisAction.RevertRequested = true;
                                    return;
                                }

                                await onSuccess(self);
                            });
                        
                        return inkAction;
                    }
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                values.GetProficiency(Trait.Stealth)
                    is Proficiency.UntrainedWithLevel
                    or > Proficiency.Untrained
                    ? null
                    : "This feat works best if you can at least add your level to Stealth checks.");
        
        // Pattern Flight
        yield return new TrueFeat(
                ModData.FeatNames.PatternFlight, 2,
                "You place a bit of magic in a physical projectile, causing it to fly in a runic pattern through the air once you loose it.",
                "Make a ranged Strike against a target within your weapon's first range increment. Because of the erratic nature of its flight, this Strike ignores any circumstance bonus to AC from cover. After your Strike, you can Trace a Rune on one target in a straight line between you and the target of your Strike (including the original target).",
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Ranged)
                        || item.HasTrait(Trait.Thrown)
                        || item.HasTrait(Trait.Unarmed)
                        || item.WeaponProperties!.RangeIncrement < 1)
                        return null;

                    StrikeModifiers strikeMods = new StrikeModifiers()
                    {
                        AdditionalTraits = [ModData.ModTrait, Trait.Flourish, Trait.Runesmith, Trait.IgnoreAllCover]
                    };
                    CombatAction strike = StrikeRules.CreateStrike(
                            qfFeat.Owner, item,
                            RangeKind.Ranged,
                            -1, false,
                            strikeMods)
                        .WithActionCost(0)
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            strikeMods,
                            additionalAttackRollText: "This ignores any circumstance bonus to AC from cover.",
                            additionalAftertext: "Trace a Rune on one target in a straight line between you and the target of your Strike (including the original target)."));
                    
                    CombatAction patternFlight = new CombatAction(
                            qfFeat.Owner,
                            item.Illustration,
                            "Pattern Flight",
                            [ModData.ModTrait, Trait.Flourish, Trait.Runesmith],
                            strike.Description,
                            Target.Line(item.WeaponProperties!.RangeIncrement)
                                .WithLesserDistanceIsOkay()
                                .WithAdditionalRequirementOnCaster(self => 
                                    strike.CanBeginToUse(self)))
                        .WithActionCost(2)
                        .WithStrikeNameAndIllustrationChange(
                            "Pattern Flight",
                            ModData.Illustrations.TraceRune,
                            false)
                        .WithEffectOnChosenTargets(async (action, caster, lineTargets) =>
                        {
                            List<Creature> validStrikes = lineTargets
                                .GetAllTargetCreatures()
                                .Where(cr =>
                                    ((CreatureTarget)strike.Target)
                                    .IsLegalTarget(strike.Owner, cr))
                                .ToList();

                            if (validStrikes.Count == 0)
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            
                            strike.WithAdjustTarget<CreatureTarget>(crTar =>
                                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                                    validStrikes.Contains(d)
                                        ? Usability.Usable
                                        : Usability.NotUsableOnThisCreature("Not in the line's area")));

                            Creature? strikeCreature = await caster.Battle.AskToChooseACreature(
                                caster,
                                validStrikes,
                                action.Illustration,
                                "Choose an enemy to Strike with Pattern Flight.",
                                cr =>
                                    CombatActionExecution.BreakdownAttackForTooltip(strike, cr).TooltipDescription,
                                "Revert");

                            if (strikeCreature is null)
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            
                            strike.WithEffectOnChosenTargets(async (strike2, caster2, strikeTargets) =>
                            {
                                if (strikeTargets.ChosenCreature is not { } target)
                                    return;
                                
                                List<Creature> validTraces = lineTargets
                                    .ChosenTiles
                                    .TakeWhile(tile => !strikeCreature.Space.Tiles.Contains(tile))
                                    .Select(tile => tile.PrimaryOccupant)
                                    .Append(strikeCreature)
                                    .WhereNotNull()
                                    .ToList();

                                if (await CommonRuneRules.AskToTraceARune(
                                        strike.Owner,
                                        null,
                                        validTraces.Contains,
                                        null,
                                        item.WeaponProperties!.RangeIncrement,
                                        overridePassButton: "Convert to a simple Strike")
                                    is null or CancelOption or PassViaButtonOption)
                                {
                                    action.RevertRequested = true;
                                    action.SpentActions = 1;
                                    action.Traits.Remove(Trait.Flourish);
                                    strike.Traits.Remove(Trait.Flourish);
                                    caster.Battle.Log("Pattern Flight converted to a simple Strike.");
                                    return;
                                }
                            });

                            await caster.MakeStrike(strike, strikeCreature);
                        });

                    return patternFlight;
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory(RequiresPhysicalProjectile);
        
        // Runic Tattoo
        yield return new TrueFeat(
                ModData.FeatNames.RunicTattoo, 2,
                "By drawing your favorite rune in your flesh, you know you'll never be without it.",
                $$"""
                  Choose one rune you know to tattoo onto your body. The rune is {{ModData.FeatNames.EtchRune.ToLink("etched")}} onto yourself{{ModData.Tooltips.InfoRunicTattooRestrictions}} at the beginning of each encounter. This doesn't count toward your maximum limit of etched runes. You can invoke this rune like any of your other runes, but once invoked, the rune fades significantly and is drained of power until your next daily preparations.

                  {b}Downtime{/b} You can magically alter your tattoo to become another rune you know (regardless of the level you learned this feat). {i}(If playing in Free Encounter Mode, you must select a rune you know at the level of the encounter or else this will fail to apply.){/i}
                  """,
                [Trait.Runesmith])
            .WithOnSheet(values =>
            {
                values.AtEndOfRecalculationBeforeMorningPreparations = valuesBefore =>
                {
                    valuesBefore.AddSelectionOption(new LimitedTextSelectionOption(
                        "RunicTattoo",
                        "Runic Tattoo",
                        SelectionOption.DOWNTIME_PREPARATIONS_LEVEL,
                        AllRunes.AllRuneFeats
                            // Must know the rune
                            .Where(valuesBefore.HasFeat)
                            .Where(runeFeat =>
                            {
                                Rune rune = (runeFeat.Tag as Rune)!;
                                return
                                    // Cannot be a rune that draws on items or other runes
                                    rune.DrawProperties.IsDrawnOnlyOnCreatures
                                    // Cannot be a harmful passive effect
                                    && !rune.PassiveProperties.PassiveEffectIsDebuff;
                            })
                            .Select(runeFeat =>
                            {
                                Rune rune = (runeFeat.Tag as Rune)!;
                                return new FeatlikeChoice(
                                    $"RunicTattoo.{rune.WordName}",
                                    rune.FullName)
                                {
                                    Illustration = runeFeat.Illustration,
                                    // PETR: Traits
                                    // Present the same as learning a rune.
                                    TextCreator = () =>
                                        $"{(runeFeat.FlavorText != null ? $"{runeFeat.FlavorText.WithTag("i")}\n\n" : null)}{runeFeat.RulesText}",
                                    // Add this key to your sheet
                                    Apply = valuesApply =>
                                        valuesApply.Tags.TryAdd(RUNIC_TATTOO_KEY, rune)
                                };
                            })
                            .ToArray()));
                };
            })
            .WithOnCreature((values, self) =>
            {
                if (RunicRepertoireTag.GetRepertoire(self) is not { } repertoire)
                    return;
                if (!values.Tags.TryGetValueAs(RUNIC_TATTOO_KEY, out Rune? rune)
                    || rune is null)
                {
                    // Error or user-mistake detection system
                    self.Overhead(
                        "*NO RUNIC TATTOO*",
                        Color.Red,
                        "{Red}ERROR:{/Red} Runic Tattoo not found.",
                        $$"""
                          {{self}}'s {b}Runic Tattoo{/b} was not found. This can happen for one of the following reasons:

                          1. The {i}Runic Tattoo{i} selection is empty.

                          2. You are in Free Encounter Mode and selected a rune that's known at a higher level than the encounter's level.
                          """);
                    return;
                }

                QEffect runicTattoo = new QEffect()
                {
                    Name = "[RUNIC TATTOO: ETCH AT THE START OF COMBAT]",
                    // Etch that rune at the start of combat
                    StartOfCombat = async qfThis =>
                    {
                        // Invokeable once per day
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions
                            .Contains(ModData.PersistentActions.RUNIC_TATTOO))
                            return;
                        
                        CombatAction etchTattoo = CommonRuneRules
                            .CreateEtchAction(qfThis.Owner, rune)
                            .WithName($"Tattoo {rune.FullName}")
                            .With(ca => ca.Traits.Remove(Trait.Etched))
                            .WithExtraTrait(ModData.Traits.Tattooed);
                        
                        DrawnRune? appliedRune = (await CommonRuneRules.DrawRuneOnCreature(
                                etchTattoo,
                                qfThis.Owner,
                                rune,
                                doNotApplyImmediately: true))?
                            .LastOrDefault();

                        if (appliedRune is null)
                        {
                            self.Overhead(
                                "*NO RUNIC TATTOO*",
                                Color.Red,
                                "{Red}ERROR:{/Red} Runic Tattoo failed to apply.",
                                $$"""
                                  {{self}}'s {b}Runic Tattoo{/b} failed to apply.

                                  This isn't supposed to be possible. If you see this error, please report it immediately to the {link:https://steamcommunity.com/sharedfiles/filedetails/?id=3460180524}Runesmith Class{/} mod page.
                                  """);
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                            return;
                        }

                        appliedRune.Id = ModData.QEffectIds.TattooedRune;
                        // Invokeable once per day
                        appliedRune.AfterInvokingRune += async (drThis, invokeAction, drInvoked) =>
                        {
                            if (drInvoked == drThis)
                                drInvoked.Owner.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.RUNIC_TATTOO);
                        };
                        qfThis.Owner.AddQEffect(appliedRune);
                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                    }
                };

                // This is guaranteed to occur before etching at the start of combat.
                self.AddQEffectAtPriority(runicTattoo, true);
            });
        
        #endregion
        
        #region 4th-Level
        
        // Artist's Attendance
        // DOC: "within reach of a creature" is interpreted as being YOUR reach
        yield return new TrueFeat(
                ModData.FeatNames.ArtistsAttendance, 4,
                "Your runes call you to better attend to your art.",
                $"Stride twice. If you end your movement within your reach of a creature that is bearing one of your runes{ModData.Tooltips.InfoArtistsAttendanceSelfBearer}, you can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} upon that creature or another target adjacent to you.",
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(2)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction attendAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                IllustrationName.FleetStep, 
                                ModData.Illustrations.TraceRune),
                            "Artist's Attendance",
                            [ModData.ModTrait, Trait.Flourish, Trait.Runesmith],
                            """
                            {i}Your runes call you to better attend to your art.{/i}

                            Stride twice. If you end your movement within your reach of a creature that is bearing one of your runes, you can Trace a Rune upon that creature or another target adjacent to you.
                            """,
                            Target.Self())
                        .WithActionCost(2)
                        .WithShortDescription("Stride twice. If you stop within your reach of a rune-bearer, then Trace a Rune on them or any adjacent creature.")
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithEffectOnEachTarget(async (action, caster, _, _) =>
                        {
                            // Stride twice
                            if (!await caster.StrideAsync("Choose where to Stride with Artist's Attendance. (1/2)", allowCancel: true))
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            if (!await caster.StrideAsync(
                                         "Choose where to Stride with Artist's Attendance. You should end your movement within your reach of a rune-bearer. (2/2)",
                                         allowPass: true))
                            {
                                caster.Battle.Log("Artist's Attendance was converted to a simple Stride.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                                return;
                            }

                            int reach = caster.Space.NaturalReach;
                            List<Creature> runeBearers = caster.Battle.AllCreatures
                                .Where(cr =>
                                    DrawnRune.IsARuneBearer(caster, cr)
                                    && cr.DistanceToWith10FeetException(caster) <= reach)
                                .ToList();
                            List<Creature> validTargets = runeBearers
                                .Union(caster.Neighbours.Creatures)
                                .ToList();

                            if (runeBearers.Count == 0
                                || await CommonRuneRules.AskToTraceARune(caster,
                                    targetFilter: validTargets.Contains,
                                    overrideRange: reach)
                                    is not {} chosenOption
                                || chosenOption is CancelOption or PassViaButtonOption)
                            {
                                caster.Battle.Log("Artist's Attendance was converted to two simple Strides.");
                                action.Traits.Remove(Trait.Flourish);
                            }
                        });

                    return new ActionPossibility(attendAction)
                        .WithPossibilityGroup(ModData.PossibilityGroups.DRAWING_RUNES);
                };
            })
            .WithTraceRunePrereq();
        
        // Ghostly Resonance
        yield return new TrueFeat(
                ModData.FeatNames.GhostlyResonance, 4,
                "Your runes don’t just draw power from the world of the spirits — they can let even the most mundane objects harm spirits as well.",
                // Wording is slightly different to reflect the simplified, automated behavior of this logic.
                $$"""
                  While an ally or a weapon they're wielding has a {{ModData.Tooltips.RuleRuneTradition("divine or occult rune")}} drawn on them, you grant the target of the rune a {{ItemName.GhostTouchRunestone.ToLink("ghost touch").WithTag("i")}} property rune {i}(this doesn't count toward any limits on property runes){/i}.
                  • {b}Weapon{/b} The weapon gains the rune.
                  • {b}Creature{/b} All the creature's unarmed Strikes gain the rune.
                  
                  This benefit lasts as long as the rune remains.
                  """,
                [Trait.Runesmith])
            .WithPermanentQEffect(
                "Your divine and occult runes grant the benefits of a {i}ghost touch{/i} rune to allied creatures or items.",
                qfFeat =>
                {
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                        cr.FriendOf(qfFeat.Owner)
                        && DrawnRune.GetDrawnRunes(qfFeat.Owner, cr)
                            .Any(drawnRune => drawnRune.Traditions.Any(trait =>
                                trait is Trait.Divine or Trait.Occult)),
                        qfTech =>
                        {
                            qfTech.AdjustStrikeAction = (qfTech2, action) =>
                            {
                                if (action.HasTrait(Trait.GhostTouch)
                                    || action.Item is null
                                    || DrawnRune.GetDrawnRunes(qfFeat.Owner, action.Owner)
                                        .Where(drawnRune =>
                                            drawnRune.Traditions.Any(trait =>
                                                trait is Trait.Divine or Trait.Occult))
                                        .ToList()
                                        is not { } drawnRunes
                                    || drawnRunes.Count == 0)
                                    return;

                                if (action.HasTrait(Trait.Unarmed))
                                {
                                    if (!drawnRunes.Any(drawnRune =>
                                            drawnRune.DrawnOn is Item drawnItem
                                            && drawnItem.HasTrait(Trait.Unarmed)))
                                        return;
                                }
                                else if (drawnRunes.All(drawnRune =>
                                             drawnRune.DrawnOn != action.Item))
                                    return;

                                action.WithExtraTrait(Trait.GhostTouch);
                            };
                        }
                    );
                })
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
            {
                if (RunicRepertoireTag.GetRepertoire(values) is not { } repertoire)
                    return "This feat only works if you have a runic repertoire.";

                if (repertoire.GetKnownRunes(values) is not { } knownRunes
                    || knownRunes.Count == 0)
                    return "This feat only works if you know any runes.";

                bool knowsTraditionRunes = knownRunes.Any(rune =>
                    rune.Traits.Any(trait =>
                        trait is Trait.Divine or Trait.Occult));

                if (knowsTraditionRunes)
                    return null;

                bool hasTraining =
                    values.GetProficiency(Trait.Religion) > Proficiency.Untrained
                    || values.GetProficiency(Trait.Occultism) > Proficiency.Untrained;

                bool knowsGenericRunes = knownRunes.Any(rune =>
                    rune.Traits.All(trait =>
                        !trait.IsTraditionTrait()));

                if (hasTraining && knowsGenericRunes)
                    return null;

                return "This feat only works if you know a divine or occult rune; or are trained in Religion or Occultism and know a rune that doesn't have a specific tradition.";
            });
        
        // Song of Glorious Invocation
        // DOC: Lacked the Invocation trait, and has been added.
        yield return new TrueFeat(
                ModData.FeatNames.SongOfGloriousInvocation, 4,
                "You weave the true names of several runes you've drawn into a beautiful song, invoking them all simultaneously.",
                $$"""
                {b}Frequency{/b} Once per encounter.

                Choose up to three rune-bearers within 30 feet and {{ModData.FeatNames.InvokeRune.ToLink("Invoke one Rune")}} on each of them. The song also inspires the rune-bearers, granting them a +1 status bonus to skill checks and saves against fear effects for 1 minute.
                """,
                [Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.Invocation, Trait.Mental, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " " + UsedUpDescription("[invocation] (Once per combat) Invoke one Rune on each of up to 3 rune-bearers, and grant a +1 status bonus to skill checks and saves against fear for the rest of the encounter.", !qfThis.UsedUpPermanently, "combat");
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.UsedUpPermanently)
                        return null;
                    
                    (int range, string rangeDesc) = CommonRuneRules.GetInvocationRange(qfThis.Owner, 6);

                    CombatAction song = new CombatAction(
                            qfThis.Owner,
                            new SuperimposedIllustration(
                                ModData.Illustrations.RuneSinger,
                                new BagOfIllustrationsIllustration(
                                    ModData.Illustrations.TraceRune,
                                    ModData.Illustrations.TraceRune,
                                    ModData.Illustrations.TraceRune)),
                            "Song of Glorious Invocation",
                            [ModData.ModTrait, Trait.Auditory, Trait.Concentrate, Trait.Emotion, Trait.Invocation, Trait.Mental, Trait.Runesmith, Trait.Basic],
                            null!,
                            Target.Self()
                                .WithAdditionalRestriction(a =>
                                    a.Battle.AllCreatures.Any(cr =>
                                        cr.FriendOf(a)
                                        && cr.DistanceTo(a) <= range)
                                        ? null
                                        : "No allied rune-bearers within range"))
                        .WithDescription(
                                "You weave the true names of several runes you've drawn into a beautiful song, invoking them all simultaneously.",
                                $$"""
                                  {b}Frequency{/b} Once per encounter.

                                  Choose up to three rune-bearers within {{rangeDesc}} and {{ModData.FeatNames.InvokeRune.ToLink("Invoke one Rune")}} on each of them. The song also inspires the rune-bearers, granting them a +1 status bonus to skill checks and saves against fear effects for 1 minute.
                                  """)
                        // Built as WithEffectOnSelf to consolidate choosing
                        // a creature with choosing a creature option.
                        .WithEffectOnSelf(async (action, caster) =>
                        {
                            List<Creature> allCreatures = caster.Battle.AllCreatures
                                .Where(cr =>
                                    DrawnRune.IsARuneBearer(caster, cr)
                                    && cr.FriendOf(caster)
                                    && cr.DistanceTo(caster) <= range)
                                .ToList();
                            List<DrawnRune> chosenRunes = [];
                            
                            // Choose up to 3 invocations, filtering targets each time,
                            // delaying invocation until afterward.
                            for (int i=0; i<3; i++)
                            {
                                (DrawnRune? chosenRune, Option chosenOption) = await CommonRuneRules.ChooseADrawnRune(
                                    caster,
                                    allCreatures.Except(chosenRunes.Select(dr => dr.Owner)),
                                    action.Illustration,
                                    $"Choose a rune on an ally to invoke, or right-click to cancel. ({i + 1}/3)",
                                    dr => $"Invoke {dr.Name}",
                                    dr => dr.Rune.InvocationProperties.InvocationTextWithFormattedHeightening
                                        ?.Invoke(dr.Rune, dr.Source!.Level) ?? "[NO INVOCATION DESCRIPTION]",
                                    i == 0 ? "Revert" : " Confirm no additional invocations ",
                                    true,
                                    dr => dr.Rune.InvocationProperties.IsLegalTarget(caster, dr.Owner));

                                // Revert only if it's null on the first choice.
                                // This is because you choose UP TO 3 allies.
                                if (chosenRune is null)
                                {
                                    if (i == 0 || chosenOption is CancelOption)
                                    {
                                        action.RevertRequested = true;
                                        return;
                                    }
                                    if (chosenOption is PassViaButtonOption)
                                        break;
                                }
                                else
                                {
                                    chosenRunes.Add(chosenRune);
                                    Sfxs.Play(SfxName.OminousActivation);
                                }
                            }
                            
                            // Fallback. This should never execute.
                            if (chosenRunes.Count == 0)
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            // End the selection UI that seems to persist during these animations for some reason.
                            caster.Battle.Request.PostProcess(caster.Battle.Request.AffectedTiles.ToList());
                            caster.Battle.Request = null!;

                            qfThis.UsedUpPermanently = true;

                            // Play SFX and VFX
                            Sfxs.Play(ModData.SfxNames.SING_RUNE);
                            await CommonRuneRules.PlayGroupInvocationOverheadAnimation(caster, chosenRunes);
                            await CommonRuneRules.PlayGroupInvocationSplashAnimation(
                                caster.Battle,
                                chosenRunes.Select(dr =>
                                        (dr.Owner, dr.Rune.Illustration))
                                    .ToList());
                            
                            // Invoke each rune and buff each bearer
                            foreach (DrawnRune dr in chosenRunes)
                            {
                                // Capture before it gets removed
                                Creature owner = dr.Owner;
                                await CommonRuneRules.InvokeDrawnRune(
                                    action, dr,
                                    await CommonRuneRules.GetInvocationTargets(action, dr));
                                owner.AddQEffect(new QEffect(
                                    "Glorious Song",
                                    "You have a +1 status bonus to skill checks as well as saves against fear.",
                                    action.Illustration)
                                {
                                    BonusToSkills = _ =>
                                        new Bonus(1, BonusType.Status, "Song of Glorious Invocation"),
                                    BonusToDefenses = (_, fearAction, def) =>
                                        def.IsSavingThrow()
                                        && fearAction?.HasTrait(Trait.Fear) == true
                                            ? new Bonus(1, BonusType.Status, "Song of Glorious Invocation")
                                            : null,
                                });
                            }
                        });

                    CommonRuneRules.WithImmediatelyRemovesImmunity(song);

                    return new ActionPossibility(song)
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
                };
            })
            .WithPrerequisite(
                ModData.FeatNames.RuneSinger,
                "Rune-Singer");
        
        // Terrifying Invocation
        yield return new TrueFeat(
                ModData.FeatNames.TerrifyingInvocation, 4,
                "You spit and roar as you pronounce your rune's terrible name.",
                $"You attempt to Demoralize a single target within 30 feet, and then {ModData.FeatNames.InvokeRune.ToLink("Invoke one Rune")} upon that target. You don't take a penalty to your check if the creature doesn't understand your language.",
                [Trait.Invocation, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                (int range, string rangeDesc) = CommonRuneRules.GetInvocationRange(qfFeat.Owner, 6);
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction scaryInvoke = new CombatAction(
                        qfThis.Owner,
                        new SideBySideIllustration(
                            IllustrationName.Demoralize,
                            ModData.Illustrations.InvokeRune),
                        "Terrifying Invocation",
                        [ModData.ModTrait, Trait.Invocation, Trait.Runesmith],
                        $$"""
                          {i}You spit and roar as you pronounce your rune's terrible name.{/i}

                          You attempt to Demoralize a single target within {{rangeDesc}}, and then Invoke one Rune upon that target. You don't take a penalty to your check if the creature doesn't understand your language.
                          """,
                        Target.RangedCreature(range)
                            .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement())
                            .WithAdditionalConditionOnTargetCreature(new TargetIsARuneBearer()))
                        .WithActionCost(1)
                        .WithShortDescription("Demoralize and Invoke one Rune on a creature.")
                        .WithTargetingTooltip((action, target, _) =>
                        {
                            QEffect tempGlare = new QEffect()
                                { Id = QEffectId.IntimidatingGlare };
                            action.Owner.AddQEffect(tempGlare);
                            
                            CombatAction demoralize = CommonCombatActions
                                .Demoralize(action.Owner)
                                .WithActionCost(0);
                                            
                            action.Owner.RemoveAllQEffects(qf => qf == tempGlare);

                            return "{b}Demoralize{/b}\n" + CombatActionExecution
                                .BreakdownAttackForTooltip(demoralize, target)
                                .TooltipDescription;
                        })
                        .WithEffectOnEachTarget(async (action, caster, target, _) =>
                        {
                            if (!await CommonRuneRules.ChooseARuneToInvoke(
                                    caster,
                                    targetFilter: cr => cr == target,
                                    adjustInvocation: invokeAction =>
                                    {
                                        invokeAction.WithPrologueEffectOnChosenTargetsBeforeRolls(async (invokeAction2, caster2, targets) =>
                                        {
                                            CombatAction demoralize = CommonCombatActions
                                                .Demoralize(caster2)
                                                .WithActionCost(0)
                                                // Alter the range of this Demoralize
                                                .WithAdjustTarget<CreatureTarget>(crTar =>
                                                    crTar.CreatureTargetingRequirements
                                                        .OfType<MaximumRangeCreatureTargetingRequirement>()
                                                        .FirstOrDefault()
                                                        ?.Range = range);
                                            
                                            QEffect tempGlare = new QEffect()
                                            { Id = QEffectId.IntimidatingGlare };
                                            caster.AddQEffect(tempGlare);
                                            
                                            await caster.Battle.GameLoop.FullCast(
                                                demoralize,
                                                targets);
                                            
                                            caster.RemoveAllQEffects(qf => qf == tempGlare);
                                        });
                                    },
                                    canBeCanceled: true,
                                    passText: "Cancel action"))
                                action.RevertRequested = true;
                        });

                    scaryInvoke = CommonRuneRules.WithImmediatelyRemovesImmunity(scaryInvoke);
                    
                    return new ActionPossibility(scaryInvoke)
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
                };
            })
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                values.GetProficiency(Trait.Intimidation)
                    is Proficiency.UntrainedWithLevel
                    or > Proficiency.Untrained
                    ? null
                    : "This feat works best if your proficiency bonus with Intimidation includes your level.");
        
        // Transpose Etching
        yield return new TrueFeat(
                ModData.FeatNames.TransposeEtching, 4,
                "With a pinching gesture, you pick up a word and move it elsewhere.",
                $$"""
                You move any one of your runes{{ModData.Tooltips.InfoTransposeEtchingName}} within 30 feet to a different eligible target within 30 feet.

                {b}Special{/b} (homebrew) When a creature bearing one of your runes dies, you can use Transpose Etching to move one of your runes from that creature as a {icon:FreeAction} free action.
                """,
                [Trait.Manipulate, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction transposeAction = CreateTransposeAction(qfThis);
                    return new ActionPossibility(transposeAction)
                        .WithPossibilityGroup(ModData.PossibilityGroups.DRAWING_RUNES);
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
                                CreateTransposeAction(qfFeat)
                                    .WithActionCost(0)
                                    .WithTag(qfTech2.Owner)); // Filters targets,
                            qfTech2.Owner.DeathScheduledForNextStateCheck = true;
                            //await qfTech2.Owner.Battle.GameLoop.StateCheck();
                        };
                    });
                return;

                CombatAction CreateTransposeAction(QEffect qfThis)
                {
                    return new CombatAction(
                        qfThis.Owner,
                        ModData.Illustrations.TransposeEtching,
                        "Transpose Etching",
                        [ModData.ModTrait, Trait.Manipulate, Trait.Runesmith],
                        "You move any one of your runes within 30 feet to a different target within 30 feet.",
                        Target.Self()
                            .WithAdditionalRestriction(self =>
                            {
                                List<Creature> creatures = self.Battle.AllCreatures;
                                var runeBearers = creatures
                                    .Where(cr => DrawnRune.GetDrawnRunes(self, cr).Count > 0)
                                    .ToList();
                                if (!runeBearers.Any())
                                    return "no rune-bearers";
                                var validBearers = runeBearers
                                    .Where(cr =>
                                        DrawnRune.GetDrawnRunes(self, cr)
                                            .Any(IsTransposableRune))
                                    .ToList();
                                if (!validBearers.Any())
                                    return "no valid runes";
                                if (!validBearers.Any(cr => cr.DistanceTo(self) <= 6))
                                    return "none in range";
                                return null;
                            }))
                        .WithActionCost(1)
                        .WithShortDescription("Move a rune from one target to another, both within 30 feet.")
                        .WithSoundEffect(ModData.SfxNames.TRANSPOSE_ETCHING_START)
                        .WithEffectOnEachTarget(async (transposeAction, caster, _,_) =>
                        {
                            List<Creature> possiblePickups = caster.Battle.AllCreatures
                                .Where(cr =>
                                    cr.DistanceTo(caster) <= 6
                                    && DrawnRune.IsARuneBearer(caster, cr))
                                .ToList();
                            
                            if (transposeAction.Tag is Creature target)
                                possiblePickups = possiblePickups
                                    .Where(cr => cr == target)
                                    .ToList();
                            
                            DrawnRune? chosenRune = (await CommonRuneRules.ChooseADrawnRune(
                                    caster,
                                    possiblePickups,
                                    transposeAction.Illustration,
                                    "Choose one of your runes to move to another creature within 30 feet or right-click to cancel.",
                                    dr => $"Pick up {{Blue}}{dr.Rune.FullName}{{/Blue}}",
                                    null, "Revert", true,
                                    IsTransposableRune))
                                .ChosenRune;

                            if (chosenRune == null)
                            {
                                transposeAction.RevertRequested = true;
                                return;
                            }

                            // This action has additional requirements that are unique to
                            // the rune, such that the target must have legal subtargets
                            // for runes that target items or runes. This is how I ensure
                            // that there are legal subtargets on the Creature.
                            var drawTarget = CommonRuneRules.CreateDrawActionTarget(
                                    chosenRune.Rune, 2, 6);
                            drawTarget.SetOwnerAction(chosenRune.SourceAction!);

                            List<Creature> possibleDropoffs = caster.Battle.AllCreatures
                                /*.Where(cr =>
                                    chosenRune.Rune.DrawProperties.IsLegalTarget(caster, cr))*/
                                .Where(cr =>
                                    drawTarget?.IsLegalTarget(caster, cr)
                                    ?? false)
                                .ToList();
                            
                            Creature? chosenCreature = await caster.Battle.AskToChooseACreature(
                                caster,
                                possibleDropoffs,
                                transposeAction.Illustration,
                                $"Choose a creature to bear {chosenRune}.",
                                $"Move {chosenRune} to this creature.",
                                "Revert");
                            
                            if (chosenCreature == null)
                            {
                                transposeAction.RevertRequested = true;
                                return;
                            }
                            
                            if (await chosenRune.Rune.PassiveProperties.DrawnRuneCreator!.Invoke(
                                    chosenRune.SourceAction!,
                                    chosenRune.Rune,
                                    chosenCreature,
                                    chosenRune.Rune.DrawProperties.ChooseRuneSubtargets(caster, [chosenCreature]))
                                is not {} pretendNewRunes
                                || pretendNewRunes.FirstOrDefault() is not {} pretendNewRune)
                            {
                                transposeAction.RevertRequested = true;
                                return;
                            }

                            Sfxs.Play(ModData.SfxNames.TRANSPOSE_ETCHING_END);
                            
                            await caster.Battle.SpawnOverairProjectileParticlesAsync(
                                1,
                                chosenRune.Owner, chosenCreature,
                                Color.White,
                                chosenRune.Rune.Illustration,
                                ParticleKind.ExactProjectile);
                            
                            /*await CommonAnimations.CreateConeAnimation(
                                caster.Battle,
                                chosenRune.Owner.Space.CenterVector,
                                chosenCreature.Space.Tiles.ToList(),
                                1, ProjectileKind.Arrow,
                                chosenRune.Rune.Illustration);*/
                            
                            /*await*/ CommonRuneRules.MoveRuneToTarget(
                                chosenRune,
                                chosenCreature,
                                pretendNewRune.DrawnOn);
                        });
                }

                bool IsTransposableRune(DrawnRune dr)
                {
                    return
                        dr.DrawTrait != ModData.Traits.Tattooed
                        && !dr.Traits.Contains(ModData.Traits.Reprised);
                }
            });
        
        // Writing on the Wall
        // Not very useful and I don't have homebrew replacement concepts.
        
        #endregion
        
        #region 6th-Level
        
        // Diacritic Fluency
        yield return new TrueFeat(
                ModData.FeatNames.DiacriticFluency, 6,
                "When you intensify your attention, you can modify a rune with great speed.",
                """
                {b}Frequency{/b} Once per encounter.

                The next time you Trace a Rune this turn, you can also Trace a diacritic Rune on that rune.
                """,
                [Trait.Concentrate, Trait.Runesmith])
            .WithActionCost(0)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " " + UsedUpDescription("[concentrate] (Once per combat) The next time you Trace a Rune this turn, Trace a diacritic Rune on it.", !qfFeat.UsedUpPermanently, "combat");

                // Yes, yes, yes, I know it's already false.
                // "Initializing" it like this just helps me to see that
                // this is an important component of this QEffect.
                qfFeat.UsedUpPermanently = false;

                qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                {
                    if (qfThis.UsedUpPermanently
                        || submenu.SubmenuId != ModData.SubmenuIds.TraceRune
                        || AvailableDiacritics() is not {} diacritics)
                        return null;

                    QEffect? fluencyBuff = qfThis.Owner.FindQEffect(ModData.QEffectIds.DiacriticFluency);
                    bool isActive = fluencyBuff is not null;

                    CombatAction fluencyToggle = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.DiacriticFluency,
                            $"Diacritic Fluency (toggle {(isActive ? "off" : "on")})",
                            [Trait.Concentrate, Trait.Runesmith],
                            """
                            {i}When you intensify your attention, you can modify a rune with great speed.{/i}

                            {b}Frequency{/b} Once per encounter.

                            The next time you Trace a Rune this turn, you can also Trace a diacritic Rune on that rune.
                            """,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                    diacritics.Count == 0
                                        ? "All diacritics used up" : null))
                        .WithActionCost(0)
                        .WithSoundEffect(SfxName.OminousActivation)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            if (isActive)
                                fluencyBuff!.ExpiresAt = ExpirationCondition.Immediately;
                            else
                                self.AddQEffect(new QEffect(
                                    "Diacritic Fluency",
                                    "The next time you Trace a Rune this turn, you can also Trace a diacritic Rune on that rune.",
                                    ExpirationCondition.ExpiresAtEndOfYourTurn,
                                    self,
                                    action.Illustration)
                                {
                                    Id = ModData.QEffectIds.DiacriticFluency,
                                    AfterYouTakeActionReaction = (qfBuff, traceAction) =>
                                    {
                                        if (traceAction.ActionId != ModData.ActionIds.TraceRune
                                            || (traceAction.Tag as RuneActionTag)?.CreatedDrawnRune is not { } dr
                                            || dr.Rune.IsDiacriticRune
                                            || dr.AttachedDiacritic is not null)
                                            return null;

                                        List<Rune> legalDiacritics = diacritics
                                            .Where(rune =>
                                                rune.DrawProperties.IsLegalTarget(qfBuff.Owner, dr))
                                            .ToList();

                                        if (legalDiacritics.Count == 0)
                                            return null;

                                        string runeName =
                                            $"{dr.Illustration!.IllustrationAsIconString} {dr.Name!.WithColor("Blue")}";
                                        string runeTarget = dr.DrawnOn switch
                                        {
                                            Item item => $"{item.Illustration.IllustrationAsIconString} {item.Name}",
                                            _ => $"{dr.Owner.Illustration.IllustrationAsIconString} {dr.Owner.ToColoredBoldedName()}"
                                        };

                                        ReactionOption reactOpt = ReactionOption.CreateCustom(
                                                "Diacritic Fluency",
                                                $"Trace a diacritic Rune on {runeName}.",
                                                action.Illustration,
                                                qfBuff.Owner,
                                                async () =>
                                                {
                                                    if (await CommonRuneRules.AskToTraceARune(
                                                            qfBuff.Owner,
                                                            runeFilter: legalDiacritics.Contains,
                                                            targetFilter: cr => cr == dr.Owner,
                                                            canBeCanceled: true,
                                                            adjustAction: diacriticTrace =>
                                                                (diacriticTrace.Tag as RuneActionTag)?.ChosenDrawnRune = dr)
                                                        is not null or CancelOption or PassViaButtonOption)
                                                    {
                                                        qfBuff.ExpiresAt = ExpirationCondition.Immediately;
                                                        qfThis.UsedUpPermanently = true;
                                                    }
                                                })
                                            // Free action isn't triggered.
                                            // This is a bonus effect you choose to use.
                                            .WithDoesNotCountAsYourTriggerResponse()
                                            .WithTriggerReason($"You Traced {runeName} on {runeTarget}.");

                                        return reactOpt;
                                    },
                                });
                        });

                    return new PossibilitySection("Diacritic Fluency")
                    {
                        Possibilities = [
                            new ActionPossibility(fluencyToggle)
                            {
                                Caption = $"Diacritic Fluency ({(isActive ? "on" : "off")})",
                                Illustration = new CornerIllustration(
                                    ModData.Illustrations.DiacriticFluency,
                                    isActive ? ModData.Illustrations.CheckSymbol : ModData.Illustrations.NoSymbol,
                                    Direction.Southwest)
                            }
                        ]
                    };

                    List<Rune> AvailableDiacritics()
                    {
                        return RunicRepertoireTag
                            .GetRepertoire(qfThis.Owner)?
                            .GetKnownRunes(qfThis.Owner)
                            .Where(rune =>
                                rune.IsDiacriticRune
                                && !ModData.PersistentActions.RuneIsUsedUp(qfThis.Owner, rune))
                            .ToList()
                            ?? [];
                    }
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory((values, _) => RequiresOneRune(
                values,
                rune => rune.IsDiacriticRune,
                "a diacritic rune"));
        
        // Engraving Maneuver
        yield return new TrueFeat(
                ModData.FeatNames.EngravingManeuver, 6,
                "After quickly drawing a rune on your weapon, you can transfer the rune to a foe as you knock them down, push them away, or disarm their weapon.",
                $$"""
                {b}Requirements{/b} You are wielding a melee weapon with the disarm, shove, or trip trait.

                Attempt to Disarm, Shove, or Trip a target; your weapon must have the corresponding trait. If your skill check is successful, you {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} onto the target of the action even if the target is pushed out of range.
                """,
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] Disarm/Shove/Trip with a weapon of that trait. On a success, Trace a Rune on the target.";

                qfFeat.ProvideStrikeModifierAsPossibilities = (qfThis, item) =>
                {
                    if (item.HasTrait(Trait.Unarmed))
                        return [];
                    
                    Trait[] maneuvers = [Trait.Disarm, Trait.Shove, Trait.Trip];

                    return maneuvers
                        .Where(item.HasTrait)
                        .Select(Maneuver)
                        .Select(action => (ActionPossibility)action)
                        .ToList();

                    CombatAction Maneuver(Trait maneuver)
                    {
                        return (maneuver switch
                            {
                                Trait.Disarm => CombatManeuverPossibilities
                                    .CreateDisarmAction(qfThis.Owner, item),
                                Trait.Shove => CombatManeuverPossibilities
                                    .CreateShoveAction(qfThis.Owner, item),
                                Trait.Trip => CombatManeuverPossibilities
                                    .CreateTripAction(qfThis.Owner, item),
                                _ => throw new Exception("Trait for Engraving Maneuver must be Disarm, Shove, or Trip.")
                            })
                            .WithExtraTrait(0, ModData.ModTrait)
                            .WithExtraTrait(Trait.Flourish)
                            .With(ca =>
                            {
                                ca.Illustration = new TriplePortraitIllustration(
                                    item.Illustration,
                                    ((SideBySideIllustration)ca.Illustration).Left,
                                    ModData.Illustrations.TraceRune);
                                ca.WithFullRename($"Engraving Maneuver ({maneuver.ToStringOrTechnical()})");
                                ca.Description +=
                                    "\n\n{Blue}{b}Engraving Maneuver{/b} On a success, Trace a Rune onto the target of the action even if the target is pushed out of range.{/Blue}";
                            })
                            .WithEffectOnChosenTargets(async (action, self, targets) =>
                            {
                                if (targets.ChosenCreature is null)
                                {
                                    action.Traits.Remove(Trait.Flourish);
                                    self.Battle.Log("Engraving Maneuver was converted to a simple action.");
                                }

                                if (action.CheckResult < CheckResult.Success)
                                    return;
                                
                                if (await CommonRuneRules.AskToTraceARune(
                                        self,
                                        runeFilter: rune => rune.DrawProperties.IsDrawnOnlyOnCreatures,
                                        targetFilter: cr => cr == targets.ChosenCreature,
                                        overrideRange: 99,
                                        overridePassButton: $"Convert to simple {maneuver.ToStringOrTechnical()}",
                                        canBeCanceled: true)
                                    is null or CancelOption or PassViaButtonOption)
                                {
                                    action.Traits.Remove(Trait.Flourish);
                                    self.Battle.Log("Engraving Maneuver was converted to a simple action.");
                                }
                            });
                    }
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                RequiresOneCreatureRune(values)
                ?? FeatInventoryRequirements.RequiresOne(
                    inventory,
                    item =>
                        item.HasTrait(Trait.Melee)
                        && item.Traits.ContainsOneOf([Trait.Disarm, Trait.Shove, Trait.Trip]),
                    "a weapon with the disarm, shove, or trip trait"));
        
        // Runic Reprisal
        // DOC: If you don't get disrupted, the rune is eventually applied and then the delayed post-action Shield Block reaction sees the newly applied rune and attempts to invoke it, despite the damage and the block event occuring before the rune was traced. This will not be fixed due to ease of play and avoidance of player confusion.
        yield return new TrueFeat(
                ModData.FeatNames.RunicReprisal, 6,
                "When you Raise your Shield, you can bury a runic trap into it, which is set off by the clash of an enemy weapon.",
                $"""
                 When you use {ModData.FeatNames.FortifyingKnock.ToLink("Fortifying Knock {icon:Action}")}, you can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with a damaging invocation on your shield, even if it normally couldn’t be applied to a shield. The traced rune doesn't have its normal effect, instead fading into your shield.

                 If you {FeatName.ShieldBlock.ToLink("Shield Block {icon:Reaction}")} with the shield against a melee Strike, you can {ModData.FeatNames.InvokeRune.ToLink("Invoke the Rune")} as part of the reaction, causing the rune to detonate outward and apply its invocation effect to the attacking creature.
                 """,
                [Trait.Invocation, Trait.Runesmith])
            .WithPermanentQEffect(qfFeat =>
            {
                /*qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>*/
                qfFeat.ProvideBonusRaiseShieldPossibility = (qfThis, shield) =>
                {
                    if (RunicRepertoireTag.GetRepertoire(qfThis.Owner) is not {} repertoire)
                        return null;
                    
                    PossibilitySection repriseSection = new PossibilitySection("Runic Reprisal")
                    {
                        PossibilitySectionId = ModData.PossibilitySectionIds.RunicReprisal,
                        Possibilities = repertoire.GetTraceableRunes(qfThis.Owner)
                            .Where(rune => rune.InvocationProperties.DealsDamage)
                            .Select(rune => CreateFortifyingKnockAction(
                                qfThis.Owner,
                                rune,
                                shield,
                                knockAction =>
                                {
                                    knockAction.Description = knockAction.Description
                                        .Replace(
                                            rune.PassiveProperties.PassiveTextWithHeightening(rune,
                                                knockAction.Owner.Level),
                                            "{Blue}{b}Runic Reprisal{/b} When you use Shield Block against an adjacent attacker, this rune's invocation effects are detonated outward onto the attacker.{/Blue}");
                                },
                                drawnRune =>
                                {
                                    drawnRune.Traits.Add(ModData.Traits.Reprised);
                                    drawnRune.DisableRune(true);
                                    const string newDescription =
                                        "{Blue}{b}(Runic Reprisal){/b}{/Blue} When you use Shield Block against an adjacent attacker, this rune's invocation effects are detonated outward onto the attacker.";
                                    if (drawnRune.ItemDescriptionGenerator is not null)
                                        drawnRune.ItemDescriptionGenerator = (_,_) => newDescription;
                                    else
                                        drawnRune.Description = newDescription;
                                    drawnRune.UpdateDescription();
                                }))
                            .Select(action => new ActionPossibility(action))
                            .Cast<Possibility>()
                            .ToList()
                    };

                    SubmenuPossibility runicReprisal = new SubmenuPossibility(
                        new SideBySideIllustration(
                            shield.Illustration, 
                            ModData.Illustrations.TraceRune),
                        "Runic Reprisal")
                    {
                        SubmenuId = ModData.SubmenuIds.FortifyingKnock,
                        // This doesn't DO anything, it's just to provide description to the menu.
                        SpellIfAny = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                shield.Illustration,
                                ModData.Illustrations.TraceRune),
                            "Runic Reprisal",
                            [ModData.ModTrait, Trait.Flourish, Trait.Invocation, Trait.Runesmith],
                            $$"""
                              {i}When you Raise your Shield, you can bury a runic trap into it, which is set off by the clash of an enemy weapon.{/i}

                              When you use {{ModData.FeatNames.FortifyingKnock.ToLink("Fortifying Knock {icon:Action}")}}, you can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} with a damaging invocation on your shield, even if it normally couldn’t be applied to a shield. The traced rune doesn't have its normal effect, instead fading into your shield.

                              If you {{FeatName.ShieldBlock.ToLink("Shield Block {icon:Reaction}")}} with the shield against a melee Strike, you can {{ModData.FeatNames.InvokeRune.ToLink("Invoke the Rune")}} as part of the reaction, causing the rune to detonate outward and apply its invocation effect to the attacking creature.
                              """,
                            Target.Self()),
                        Subsections = { repriseSection },
                        Tag = shield, // Unused
                    };
                    
                    return runicReprisal;
                };
                
                qfFeat.AfterYouTakeActionReaction = (qfThis, action) =>
                {
                    if (!action.HasTrait(Trait.ShieldBlock)
                        || action.Item is not { } shield
                        || action.Tag is not DamageEvent dEvent
                        || !dEvent.Source.IsAdjacentTo(qfThis.Owner)
                        || dEvent.CombatAction is not { } meleeStrike
                        || !meleeStrike.HasTrait(Trait.Melee)
                        || !meleeStrike.HasTrait(Trait.Strike))
                        return null;
                    
                    // Get drawn runes
                    List<DrawnRune> reprisals = DrawnRune
                        .GetDrawnRunes(qfThis.Owner, qfThis.Owner, true)
                        .Where(dr =>
                            dr.DrawnOn == shield
                            && dr.Traits.Contains(ModData.Traits.Reprised))
                        .ToList();

                    return new ReactionOptions(
                        reprisals
                        .Select(reprisalDr => new CombatAction(
                                action.Owner,
                                ModData.Illustrations.InvokeRune,
                                $"Runic Reprisal ({reprisalDr.Rune.WordName})",
                                [Trait.Invocation, Trait.Runesmith, Trait.UnaffectedByConcealment, Trait.ProxyAttack],
                                $$"""
                                  {i}When you Raise your Shield, you can bury a runic trap into it, which is set off by the clash of an enemy weapon.{/i}
                                  
                                  When you use {{ModData.FeatNames.FortifyingKnock.ToLink("Fortifying Knock {icon:Action}")}}, you can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} with a damaging invocation on your shield, even if it normally couldn’t be applied to a shield. The traced rune doesn't have its normal effect, instead fading into your shield.

                                  If you {{FeatName.ShieldBlock.ToLink("Shield Block {icon:Reaction}")}} with the shield against a melee Strike, you can {{ModData.FeatNames.InvokeRune.ToLink("Invoke the Rune")}} as part of the reaction, causing the rune to detonate outward and apply its invocation effect to the attacking creature.
                                  """,
                                Target.AdjacentCreature()
                                    .WithAdditionalConditionOnTargetCreature(
                                        new EnemyCreatureTargetingRequirement()))
                            .WithActionCost(0)
                            .WithTag(reprisalDr)
                            .WithCastsOrUses("reprises")
                            .WithEffectOnEachTarget(async (reprisalAction, caster, target, _) =>
                            {
                                target.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                                {
                                    AfterYouTakeAction = async (_, _) =>
                                    {
                                        await CommonRuneRules.InvokeDrawnRuneAgainstTarget(
                                            caster,
                                            reprisalDr,
                                            target,
                                            $"Reprise ({reprisalDr.Rune.WordName})");
                                    }
                                });
                            })
                            .With(reprisalAction =>
                            {
                                reprisalAction.ShortName = $"{reprisalDr.ToWordString()}";
                            }))
                        .Select(repriseThisRune =>
                        {
                            //DrawnRune reprisalDr = (repriseThisRune.Tag as RuneActionTag)?.ChosenDrawnRune!;
                            DrawnRune reprisalDr = (repriseThisRune.Tag as DrawnRune)!;
                            return ReactionOption.WrapFullcastWithChosenTargets(
                                    repriseThisRune,
                                    ChosenTargets.CreateSingleTarget(dEvent.Source),
                                    $"Invoke {reprisalDr.Illustration!.IllustrationAsIconString} {reprisalDr.Rune.FullName.WithTag("Blue")} from your shield against {dEvent.Source.ToColoredName()}.")
                                .WithDoesNotCountAsYourTriggerResponse()
                                .WithTriggerReason($"{qfThis.Owner.ToColoredBoldedName()} used {action.Name} against a melee Strike from an adjacent attacker.");
                        }));
                };
            })
            .WithInappropriateBecauseOfBadInventory((values, _) => RequiresOneDamagingRune(values))
            .WithPrerequisite(ModData.FeatNames.FortifyingKnock, "Fortifying Knock")
            // This ability makes very little sense without access to Shield Block.
            .WithPrerequisite(FeatName.ShieldBlock, "Shield Block");
        
        // Tracing Trance
        yield return new TrueFeat(
                ModData.FeatNames.TracingTrance, 6,
                "Your hands flow unbidden, tracing runes as if by purest instinct.",
                $$"""
                  {b}Trigger{/b} Your turn begins.

                  You become quickened until the end of your turn and can use the extra action only to {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} (including to supply {icon:Action} 1 action if using the {icon:TwoActions} 2-action version of Trace Rune). Focused on the act of creation, you can't use {{ModData.Tooltips.TraitInvocation("invocation")}} actions this turn.
                  """,
                [Trait.Runesmith])
            .WithActionCost(0)
            .WithPermanentQEffect(
                "At the start of your turn, you can forgo taking invocation actions to become quickened 1 for that turn (only to Trace Runes).",
                qfFeat =>
                {
                    qfFeat.StartOfYourPrimaryTurn = async (qfThis, caster) =>
                    {
                        int expiringRunes = caster.Battle.AllCreatures
                            .SelectMany(cr =>
                                DrawnRune.GetDrawnRunes(caster, cr))
                            .Count(dr =>
                                dr.DrawTrait == Trait.Traced
                                && !dr.CannotExpireThisTurn);
                        
                        if (await caster.Battle.AskForConfirmation(
                                caster,
                                IllustrationName.Haste,
                                $$"""
                                {b}Tracing Trance {icon:FreeAction}{/b}
                                Become {r}quickened{/r} this turn? This extra action can only be used to Trace Runes. You can't use any invocation actions this turn.{{(expiringRunes > 0 ? $"\n{{Red}}You have {expiringRunes} runes expiring this turn.{{/Red}}" : null)}}
                                """,
                                "Yes".WithColor(expiringRunes > 0 ? "Red" : null),
                                "No".WithColor(expiringRunes > 0 ? null : "Red")))
                        {
                            QEffect tranceEffect = QEffect.Quickened(action =>
                            {
                                // Code not shortened in case I need to expand the logic.
                                if (action.HasTrait(Trait.Traced))
                                    return true;
                                return false;
                            })
                            .With(qf =>
                            {
                                qf.PreventTakingAction = action =>
                                {
                                    // Code not shortened in case I need to expand the logic.
                                    if (action.HasTrait(Trait.Invocation))
                                        return "(tracing trance) can't take invocation actions";
                                    return null;
                                };
                                qf.Name = "Tracing Trance";
                                qf.Description = "You have an extra action you can use to Trace Runes. You can't use invocation actions.";
                                qf.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                            });
                            
                            caster.AddQEffect(tranceEffect);
                            
                            // Actually GIVE YOU the quickened action this turn.
                            if (caster.Actions.QuickenedForActions == null)
                                caster.Actions.QuickenedForActions = new DisjunctionDelegate<CombatAction>(tranceEffect.QuickenedFor!);
                            else
                                caster.Actions.QuickenedForActions.Add(tranceEffect.QuickenedFor!);
                            CombatAction dummyTraceAction =
                                CombatAction.CreateSimple(caster, "Dummy Trace Action", [Trait.Traced]);
                            dummyTraceAction.UsedQuickenedAction = true;
                            caster.Actions.RevertExpendingOfResources(1, dummyTraceAction);
                            
                            //caster.Actions.ResetToFull(); // <-- Has bug: Bypasses stunned and slowed (or at least acts as if taking a 2nd turn when generating actions)
                        }
                    };
                })
            .WithTraceRunePrereq();
        
        // Vital Compound Invocation
        yield return new TrueFeat(
                ModData.FeatNames.VitalCompoundInvocation, 6,
                "You can invoke runes from traditions that manipulate vital energy to restore flesh.",
                $"You {ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")} — one must be a {ModData.Tooltips.RuleRuneTradition("divine rune")}, and one must be a {ModData.Tooltips.RuleRuneTradition("primal rune")}. In addition to the runes' normal effects, one creature that's within 30 feet of both invoked runes regains Hit Points equal to 5 + double your level.",
                [Trait.Healing, Trait.Invocation, Trait.Runesmith, Trait.Positive])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                (int invokeRange, string invokeDesc) = CommonRuneRules.GetInvocationRange(qfFeat.Owner, (30 / 5));
                int healing = 5 + (qfFeat.Owner.Level * 2);
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction vci = CompoundInvocation(
                            qfThis.Owner,
                            IllustrationName.Heal,
                            IllustrationName.Bless,
                            "Vital Compound Invocation",
                            [ModData.ModTrait, Trait.Healing, Trait.Invocation, Trait.Runesmith, Trait.Positive],
                            "You can invoke runes from traditions that manipulate vital energy to restore flesh.",
                            $"You Invoke two Runes — one must be a divine rune, and one must be a primal rune. In addition to the runes' normal effects, one creature that's within {/*invokeDesc*/ "range"} of both invoked runes regains {healing.WithColor("Blue")} Hit Points.",
                            $"Invoke a divine and primal rune, then heal an ally within {invokeDesc} of both.",
                            Target.RangedFriend(invokeRange),
                            (a, d, runesInRange) =>
                            {
                                if (d.Damage == 0)
                                    return Usability.NotUsableOnThisCreature("healthy");
                                bool hasDivine = runesInRange.Any(dr => dr.Traditions.Contains(Trait.Divine));
                                bool hasPrimal = runesInRange.Any(dr => dr.Traditions.Contains(Trait.Primal));
                                if (!hasDivine && !hasPrimal)
                                    return Usability.NotUsableOnThisCreature("No divine or primal runes within range");
                                if (!hasDivine)
                                    return Usability.NotUsableOnThisCreature("No divine runes within range");
                                if (!hasPrimal)
                                    return Usability.NotUsableOnThisCreature("No primal runes within range");
                                return Usability.Usable;
                            },
                            invokeRange,
                            Trait.Divine, Trait.Primal,
                            "Choose a divine and a primal rune to invoke",
                            async (action, caster, target, _) =>
                            {
                                // Do healing
                                Sfxs.Play(SfxName.Healing);
                                await target.HealAsync(healing.ToString(), action);
                            });
                    
                    CommonRuneRules.WithImmediatelyRemovesImmunity(vci);
                    
                    return new ActionPossibility(vci)
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
                };
            });
        
        // Words, Fly Free
        yield return new TrueFeat(
                ModData.FeatNames.WordsFlyFree, 6,
                "Just because your runes are tattooed on your very body doesn't mean they need to remain there.",
                $$"""
                  {b}Requirements{/b} Your Runic Tattoo is not faded.

                  You fling your hand out, the rune from your {{ModData.FeatNames.RunicTattoo.ToLink("Runic Tattoo")}} flowing down it and flying through the air in a crescent. You {{ModData.FeatNames.TraceRune.ToLink("Trace the Rune")}} onto all targets within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.
                  """,
                [Trait.Manipulate, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + $" {"Expend your Runic Tattoo by tracing it in a 15-ft cone".WithTag(qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.RUNIC_TATTOO) ? "strike" : null)}.";
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.RUNIC_TATTOO))
                        return null;

                    // Can't find it the easy way due to not being found in the private QIDs list
                    // (possibly due to its ID being edited after being applied)
                    if (qfThis.Owner.FindQEffect(ModData.QEffectIds.TattooedRune)
                        is not DrawnRune tattooedRune)
                        return null;
                    
                    CombatAction flyFreeAction = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                tattooedRune.Illustration ?? IllustrationName.Action,
                                IllustrationName.SeekCone),
                            "Words, Fly Free",
                            [ModData.ModTrait, Trait.Manipulate, Trait.Runesmith, Trait.Traced, Trait.Basic],
                            """
                            {i}Just because your runes are tattooed on your very body doesn't mean they need to remain there.{/i}

                            {b}Requirements{/b} Your Runic Tattoo is not faded.

                            You fling your hand out, the rune from your Runic Tattoo flowing down it and flying through the air in a crescent. You trace the rune onto all targets within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.
                            """,
                            Target.Cone(3))
                        .WithActionCost(1)
                        //.WithShortDescription("Expend your Runic Tattoo by tracing it in a 15-ft cone.")
                        .WithProjectileCone(VfxStyle.BasicProjectileCone(tattooedRune.Illustration ?? IllustrationName.Action))
                        .WithSoundEffect(ModData.SfxNames.WORDS_FLY_FREE)
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            // Draw on each target
                            List<DrawnRune> drawings = [];
                            foreach (Creature cr in targets.ChosenCreatures.ToList())
                                drawings.AddRange(await CommonRuneRules.DrawRuneOnCreature(action, cr, tattooedRune.Rune) ?? []);
                                            
                            // Log the event
                            caster.Battle.Log(
                                $"{caster.Name} traces {tattooedRune.ToWordString()} onto:");
                            caster.Battle.LogIndent++;
                            foreach (DrawnRune dr in drawings.Cast<DrawnRune>())
                            {
                                string owner = dr.Owner.Name;
                                string? subTarget = dr.DrawnOn switch
                                {
                                    Item subItem =>
                                        $"{subItem.Illustration.IllustrationAsIconString}{subItem.ShortName}",
                                    DrawnRune subRune =>
                                        subRune.ToWordString(),
                                    _ => null
                                };
                                caster.Battle.Log(
                                    $"{owner}{(subTarget is null ? null : $"'s {subTarget}")}.");
                            }
                            caster.Battle.LogIndent--;
                            
                            tattooedRune.ExpiresAt = ExpirationCondition.Immediately;
                            qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.RUNIC_TATTOO);
                        });
                    
                    return new ActionPossibility(flyFreeAction)
                        .WithPossibilityGroup(ModData.PossibilityGroups.DRAWING_RUNES);
                };
            })
            .WithTraceRunePrereq()
            .WithPrerequisite(ModData.FeatNames.RunicTattoo, "Runic Tattoo");
        
        #endregion
        
        #region 8th-Level
        
        // Drawn in Vital Ink
        yield return new TrueFeat(
                ModData.FeatNames.DrawnInVitalInk, 8,
                "After striking the target, you run a brush or finger along your striking implement to collect a bit of its blood.",
                $$"""
                  {b}Requirements{/b} During your last action, you succeeded at a melee Strike that dealt physical damage to a creature that can bleed.

                  For the encounter, you can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} targeting the creature you drew blood from at a range of 60 feet (even if you’re Tracing a Rune as a single action). Using Drawn in Vital Ink against a different creature ends the effect for the previous creature.
                  """,
                [Trait.Runesmith])
            .WithActionCost(0)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ =>
                    "{b}Drawn in Vital Ink {icon:FreeAction}{/b} (After a successful physical melee Strike) Collect the target's blood to Trace Runes on them up to 60 feet away as a single action.";

                // Stores the last legal CombatAction.
                qfFeat.Tag = null;

                // Reset your enabled flag if you start a new activity.
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (!IsValidStrike(action, null)
                        && action.ActionCost > 0)
                        qfThis.Tag = null;
                };

                // When you meet the requirements, cache that action.
                qfFeat.AfterYouDealDamageAgainstPrimaryTargetQ = async (qfThis, action, _, _, result, dEvent) =>
                {
                    if (result > CheckResult.Failure
                        && IsValidStrike(action, dEvent))
                        qfThis.Tag = action;
                };

                qfFeat.ProvideContextualAction = qfThis =>
                {
                    if (qfThis.Tag is not CombatAction action
                        || action.ChosenTargets.ChosenCreature is not { } bloodTarget
                        || qfThis.Owner.Actions.ActionHistoryThisTurn.Count < 1)
                        return null;

                    CombatAction drawnInInk = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.DrawnInVitalInk,
                            "Drawn In Vital Ink",
                            [ModData.ModTrait, Trait.Runesmith, Trait.Basic],
                            $$"""
                              {i}After striking the target, you run a brush or finger along your striking implement to collect a bit of its blood.{/i}

                              {b}Requirements{/b} During your last action, you succeeded at a melee Strike that dealt physical damage to a creature that can bleed.

                              For the encounter, you can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} targeting {{bloodTarget.ToColoredBoldedName()}} at a range of 60 feet (even if you’re Tracing a Rune as a single action). Using Drawn in Vital Ink against a different creature ends the effect for the previous creature.
                              """,
                            Target.Self()
                                // Instead of hiding the action,
                                // warn the user that this target is immune to bleed
                                .WithAdditionalRestriction(_ =>
                                    bloodTarget.WeaknessAndResistance.Immunities.Contains(DamageKind.Bleed)
                                        ? "Target can't bleed"
                                        : null))
                        .WithActionCost(0)
                        .WithSoundEffect(SfxName.ItemAction)
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        {
                            caster.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.DrawnInVitalInk);

                            QEffect drawnInInk = new QEffect(
                                "Drawn In Vital Ink",
                                $"You can Trace a Rune, targeting {bloodTarget.ToColoredBoldedName()}, as 1 action with a range of 60 feet.",
                                ExpirationCondition.Never,
                                caster,
                                ModData.Illustrations.DrawnInVitalInk)
                            {
                                Tag = bloodTarget,
                                Key = "DrawnInVitalInk",
                                Id = ModData.QEffectIds.DrawnInVitalInk,
                            };

                            caster.AddQEffect(drawnInInk);

                            qfThis.Tag = null;
                        });

                    return new ActionPossibility(drawnInInk);
                };

                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (qfThis.Owner.QEffects.FirstOrDefault(qf =>
                                qf.Id == ModData.QEffectIds.DrawnInVitalInk)
                            is not { Tag: Creature bloodTarget }
                        || AllRunes.All.FirstOrDefault(rune =>
                                rune.FullName == section.Name)
                            is not { } foundRune)
                        return null;

                    CombatAction bloodTrace = CommonRuneRules
                        .CreateTraceAction(
                            qfThis.Owner, foundRune, 2, 12,
                            // The usage of the "Draw" verbiage is external.
                            // I otherwise use "draw" as the most generic verb for applying runes,
                            // but that's an internal coding decision unrelated to this feat.
                            "Draw Rune", "draws")
                        .WithIllustration(new SuperimposedIllustration(
                            foundRune.Illustration,
                            ModData.Illustrations.DrawnInVitalInk))
                        .WithActionCost(1)
                        .WithAdjustTarget<CreatureTarget>(crTar => crTar
                            // Communicate creature-targeting limitation with an error
                            .WithAdditionalConditionOnTargetCreature((_, _) =>
                                foundRune.DrawProperties.IsDrawnOnlyOnCreatures
                                    ? Usability.Usable
                                    : Usability.NotUsable("Rune must target a creature"))
                            .WithAdditionalConditionOnTargetCreature((_, d) =>
                                d == bloodTarget
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("not Drawn In Vital Ink target")));

                    bloodTrace.ContextMenuName = "{icon:Action} " + bloodTrace.ContextMenuName;
                    bloodTrace.Description = CommonRuneRules.StatBlocks.CreateDrawActionDescription(
                        foundRune, bloodTrace,
                        customRangeLine:
                            "60 feet"
                            + (qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger)
                               && !qfThis.Owner.HasFeat(ModData.FeatNames.GenerationalRuneSinger)
                               ? $"\n{{b}}Frequency{{/b}} Once per {(qfThis.Owner.HasFeat(ModData.FeatNames.ProdigalRuneSinger) ? "round" : "combat")} (Rune-Singer)."
                               : null));

                    // Update the usage for a legal rune
                    if (foundRune.DrawProperties.IsDrawnOnlyOnCreatures)
                        bloodTrace.Description = bloodTrace.Description
                            .Replace(
                                foundRune.DrawProperties.UsageText,
                                $"drawn on {bloodTarget.Name}".WithColor("Blue"));

                    ActionPossibility bloodPossibility = new ActionPossibility(bloodTrace)
                    {
                        Caption = "Drawn In Vital Ink",
                        Illustration = new SuperimposedIllustration(
                            IllustrationName.Action,
                            ModData.Illustrations.DrawnInVitalInk),
                    };
                    return bloodPossibility;

                };

                return;

                bool IsValidStrike(CombatAction action, DamageEvent? dEvent)
                {
                    return action.HasTrait(Trait.Melee)
                           && action.HasTrait(Trait.Strike)
                           && action.Item?.WeaponProperties is not null
                           && (dEvent?.KindedDamages.Select(kd => kd.DamageKind) ?? action.Item.DetermineDamageKinds())
                           .Any(dk => dk.IsPhysical());
                }
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory((values, _) => RequiresOneCreatureRune(values));
        
        // Edifying Trace
        yield return new TrueFeat(
                ModData.FeatNames.EdifyingTrace, 8,
                "When you apply a rune to a foe, it reveals something about them to you.",
                $$"""
                {{(ModLoader.RecallWeaknessAction is not null && ModLoader.RecallWeaknessFeat is not null
                    ? null
                    : "{b}Prerequisites{/b} You must have the {link:https://steamcommunity.com/sharedfiles/filedetails/?id=3710730920}Lores and Weaknesses{/link} mod installed.\n\n")}}{{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} onto an adjacent enemy and then attempt a skill check to {{(ModLoader.RecallWeaknessFeat is not null ? ModLoader.RecallWeaknessFeat.Value.ToLink("Recall a Weakness") : "Recall a Weakness")}} on that target. If you succeed, you leverage this knowledge when you invoke any of your runes on that target; that target takes a –1 status penalty to saving throws against your invocations for the rest of the encounter.
                
                If you use Edifying Trace on another enemy, the effect ends for the previous enemy.
                """,
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                if (ModLoader.RecallWeaknessAction is null
                    || ModLoader.RecallWeaknessFeat is null)
                    return;
                
                OptionalDependencies.FinishEdifyingTrace(qfFeat);
            })
            .WithInappropriateBecauseOfBadInventory((values, _) =>
                RequiresOneCreatureRune(values)
                ?? (ModLoader.RecallWeaknessAction is not null
                    && ModLoader.RecallWeaknessFeat is not null
                    ? null
                    : "You must have the {link:https://steamcommunity.com/sharedfiles/filedetails/?id=3710730920}Lores and Weaknesses{/link} mod installed."));
        
        // Elemental Revision
        // DOC: This permanently changes the rune.
        yield return new TrueFeat(
                ModData.FeatNames.ElementalRevision, 8,
                "You can scratch out and rewrite part of an elemental rune to temporarily change the type of power it channels.",
                // "an unattended item or one held by a willing creature"
                // "The revision lasts until the end of combat before the rune's original magic reasserts itself."
                $"You touch an adjacent {ItemName.CorrosiveRunestone.ToLink("{i}corrosive{/i}")}, {ItemName.FlamingRunestone.ToLink("{i}flaming{/i}")}, {ItemName.FrostRunestone.ToLink("{i}frost{/i}")}, {ItemName.ShockRunestone.ToLink("{i}shock{/i}")}, or {ItemName.ThunderingRunestone.ToLink("{i}thundering{/i}")} property rune on an item held by you or an ally, and you permanently change it to any other property rune from that list. You can also revise the greater version of any of the above runes into the other greater versions on the list.",
                [Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction revisionAction = new CombatAction(
                            qfThis.Owner,
                            IllustrationName.ResistEnergy,
                            "Elemental Revision",
                            [ModData.ModTrait, Trait.Runesmith],
                            """
                            {i}You can scratch out and rewrite part of an elemental rune to temporarily change the type of power it channels.{/i}

                            You touch an adjacent {i}corrosive{/i}, {i}flaming{/i}, {i}frost{/i}, {i}shock{/i}, or {i}thundering{/i} property rune on an item held by you or an ally, and you permanently change it to any other property rune from that list. You can also revise the greater version of any of the above runes into the other greater versions on the list.
                            """,
                            // Ranged 1 is used instead of adjacent in order to play the animation later.
                            Target.RangedFriend(1)
                                .WithAdditionalConditionOnTargetCreature((_, d) =>
                                {
                                    if (d.HeldItems.Any(item =>
                                            item.Runes.Any(runestone =>
                                                runestone.ItemName
                                                    is ItemName.CorrosiveRunestone
                                                    or ItemName.CorrosiveRunestoneGreater
                                                    or ItemName.FlamingRunestone
                                                    or ItemName.FlamingRunestoneGreater
                                                    or ItemName.FrostRunestone
                                                    or ItemName.FrostRunestoneGreater
                                                    or ItemName.ShockRunestone
                                                    or ItemName.ShockRunestoneGreater
                                                    or ItemName.ThunderingRunestone
                                                    or ItemName.ThunderingRunestoneGreater)))
                                        return Usability.Usable;
                                    return Usability.NotUsableOnThisCreature("no valid runestone");
                                }))
                        .WithActionCost(1)
                        .WithShortDescription("Swap an adjacent corrosive, flaming, frost, shock, or thundering rune.")
                        .WithEffectOnEachTarget(async (action, caster, target, _) =>
                        {
                            List<ItemName> runestones =
                            [
                                ItemName.CorrosiveRunestone,
                                ItemName.FlamingRunestone,
                                ItemName.FrostRunestone,
                                ItemName.ShockRunestone,
                                ItemName.ThunderingRunestone,
                            ];
                            List<ItemName> runestonesGreater =
                            [
                                ItemName.CorrosiveRunestoneGreater,
                                ItemName.FlamingRunestoneGreater,
                                ItemName.FrostRunestoneGreater,
                                ItemName.ShockRunestoneGreater,
                                ItemName.ThunderingRunestoneGreater,
                            ];

                            var requestFirst = (await caster.Battle.SendRequest(
                                    new ComboBoxInputRequest<Item>(
                                        caster,
                                        $"Choose a rune on {target.Illustration.IllustrationAsIconString} {target.ToColoredBoldedName()}'s items...",
                                        action.Illustration,
                                        "Fulltext search...",
                                        target.HeldItems
                                            .SelectMany(item => item.Runes
                                                .Select(rune =>
                                                {
                                                    Item duplicate = rune.Duplicate();
                                                    duplicate.Nickname =
                                                        $"{rune.RuneProperties?.Prefix.WithTag("i") ?? "???"} rune";
                                                    duplicate.Tag = (item, rune); // Store references
                                                    return duplicate;
                                                }))
                                            .Where(rune =>
                                                runestones.Contains(rune.ItemName)
                                                || runestonesGreater.Contains(rune.ItemName))
                                            .ToArray(),
                                        item =>
                                        {
                                            (Item heldItem, Item rune) = ((Item Weapon, Item Rune))item.Tag!;
                                            return new ComboBoxInformation(
                                                item.Illustration,
                                                item.Nickname ?? item.Name,
                                                $"{heldItem.Illustration.IllustrationAsIconString} {heldItem.ShortName}",
                                                item.GetItemDescriptionWithoutUsability(),
                                                item.ItemName.ToStringOrTechnical(),
                                                item.Traits.ToList());
                                        },
                                        item =>
                                            $"Select {(item.RuneProperties?.Prefix ?? item.ProsaicName).WithTag("i")} rune",
                                        "Cancel")))
                                .ChosenOption;

                            if (requestFirst is CancelOption
                                || requestFirst is not ComboBoxInputOption<Item>
                                    { SelectedObject.Tag: (Item weapon, Item originalRune) })
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            Sfxs.Play(SfxName.OpenPage);

                            var requestSecond = (await caster.Battle.SendRequest(
                                    new ComboBoxInputRequest<Item>(
                                        caster,
                                        $"Pick a rune to revise the {originalRune.RuneProperties?.Prefix.WithTag("i") ?? originalRune.Name} rune to...",
                                        action.Illustration,
                                        "Fulltext search...",
                                        (originalRune.ItemName.ToStringOrTechnical().Contains("Greater")
                                            ? runestonesGreater
                                            : runestones)
                                        .Except([originalRune.ItemName])
                                        .Select(Items.CreateNew)
                                        .ToArray(),
                                        item => new ComboBoxInformation(
                                            item.Illustration,
                                            (item.RuneProperties?.Prefix.WithTag("i") ?? item.Name) + " rune",
                                            null,
                                            item.GetItemDescriptionWithoutUsability(),
                                            item.ItemName.ToStringOrTechnical(),
                                            item.Traits.ToList()),
                                        item =>
                                            $"Revise to {((item.RuneProperties?.Prefix ?? item.ProsaicName).WithTag("i") + " rune").WithIndefiniteArticle()}",
                                        "Cancel")))
                                .ChosenOption;

                            if (requestSecond is CancelOption
                                || requestSecond is not ComboBoxInputOption<Item> { SelectedObject: { } newRune })
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            Item newWeapon = RunestoneRules.RecreateWithUnattachedSubitem(
                                weapon,
                                originalRune,
                                true)
                                .WithModificationRune(newRune.ItemName);

                            target.HeldItems[target.HeldItems.IndexOf(weapon)] = newWeapon;
                            Sfxs.Play(ModData.SfxNames.ELEMENTAL_REVISION);
                            await caster.FictitiousSingleTileMove(target.Space.GetClosestTileTo(target));
                            target.Overhead(newWeapon.Name, Color.Black);
                            await caster.FictitiousSingleTileMoveBack();

                            // TODO: Salvage this code for the item-targeting routines
                            
                            /*List<Option> options = [];
                            foreach (Item item in target.HeldItems)
                            {
                                foreach (Item runestone in item.Runes.Where(runestone => runestones.Contains(runestone.ItemName)))
                                {
                                    foreach (ItemName runestoneOption in runestones)
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
                                                    Sfxs.Play(ModData.SfxNames.ELEMENTAL_REVISION);
                                                    // TODO: replace .Occupies
                                                    await caster.FictitiousSingleTileMove(target.Occupies);
                                                    target.Overhead(newItem.Name, Color.Black);
                                                    // TODO: replace .Occupies
                                                    await caster.FictitiousSingleTileMove(caster.Occupies);
                                                })
                                            .WithTooltip(newRunestone.Description!)
                                            .WithIllustration(action.Illustration);
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
                                    TopBarIcon = action.Illustration,
                                })).ChosenOption;

                            switch (chosenOption)
                            {
                                case CreatureOption creatureOption:
                                {
                                    break;
                                }
                                case CancelOption:
                                    action.RevertRequested = true;
                                    return;
                                case PassViaButtonOption:
                                    return;
                            }

                            await chosenOption.Action();*/
                        });
                    
                    return new ActionPossibility(revisionAction);
                };
            });
        
        // Swiping Trace
        yield return new TrueFeat(
                ModData.FeatNames.SwipingTrace, 8,
                // Flavor text implies a melee weapon requirement. Mechanics do not enforce this.
                "You prepare a rune on the end of your weapon so that it transfers to your enemies.",
                // Like DD's swipe instead. Damage for each, attack for each.
                $$"""
                Make a melee Strike against two enemies within your reach. You can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} on each creature you hit and deal damage to, but they must be the same rune. A Swiping Trace counts as two attacks for your multiple attack penalty.

                If you're using a weapon with the sweep trait, its bonus applies to these attacks.
                """,
                [Trait.Flourish, Trait.Runesmith])
            .WithActionCost(3)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] Make a melee Strike on two enemies. On each hit, Trace the same Rune on them.";

                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee)
                        || RunicRepertoireTag.GetRepertoire(qfFeat.Owner)
                            is not {} repertoire)
                        return null;

                    List<Rune> traceableRunes = repertoire.GetTraceableRunes(qfFeat.Owner)
                        .Where(rune => rune.DrawProperties.IsDrawnOnlyOnCreatures)
                        .ToList();

                    CombatAction thisStrike = StrikeRules.CreateStrike(
                        qfFeat.Owner, item, RangeKind.Melee, -1);
                    
                    CombatAction swipingTrace = new CombatAction(
                            qfFeat.Owner,
                            new TriplePortraitIllustration(
                                item.Illustration,
                                IllustrationName.Swipe,
                                ModData.Illustrations.TraceRune),
                            "Swiping Trace",
                            [ModData.ModTrait, Trait.Flourish, Trait.Runesmith, Trait.IsHostile, Trait.AlwaysHits],
                            null!,
                            Target.MultipleCreatureTargets(
                                    Target.Reach(item),
                                    Target.Reach(item))
                                .WithMinimumTargets(2)
                                .WithAdditionalRestrictionsOnEachTarget((caster, prev, next) =>
                                    prev.All(cr => cr != next)
                                    && traceableRunes.Any(rune =>
                                        rune.DrawProperties.IsLegalTarget(caster, next))))
                        .WithActionCost(3)
                        .WithDescription(
                            "You prepare a rune on the end of your weapon so that it transfers to your enemies.",
                            """
                            Make a melee Strike against two enemies within your reach. You can Trace a Rune on each creature you hit and deal damage to, but they must be the same rune. A Swiping Trace counts as two attacks for your multiple attack penalty.

                            If you're using a weapon with the sweep trait, its bonus applies to your Swiping Trace attacks.
                            """)
                        .WithTargetingTooltip((action, target, _) =>
                            CombatActionExecution
                                .BreakdownAttackForTooltip(
                                    StrikeRules.CreateStrike(
                                        action.Owner, item, RangeKind.Melee, -1),
                                    target)
                                .TooltipDescription)
                        .WithGoodness((t, a, d) =>
                            thisStrike.TrueDamageFormula!.ExpectedValueMinimumOne
                            + a.AiLevelMinimum1 // Trace Rune is worth at least 1 point per level
                            + (item.HasTrait(Trait.Sweep) ? 0.2f : 0.0f))
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            if (targets.ChosenCreatures.Count < 2)
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            // Choose a rune to trace
                            Rune? chosenRune = null;
                            List<Option> options = [];
                            foreach (Rune rune in traceableRunes)
                            {
                                CombatAction traceAction = CommonRuneRules
                                    .CreateTraceAction(caster, rune, 2)
                                    .WithActionCost(0);
                                List<Option> newOptions = targets.ChosenCreatures
                                    .Where(cr =>
                                        (traceAction.Target as CreatureTarget)?.IsLegalTarget(caster, cr) ?? false)
                                    .Select(cr =>
                                        new CreatureOption(
                                            cr,
                                            traceAction.TooltipCreator?.Invoke(traceAction, cr, 0)
                                            // This overload excludes the range description
                                            ?? CommonRuneRules.StatBlocks.CreateDrawActionDescription(
                                                rune, caster.Level, null),
                                            async () => chosenRune = rune,
                                            0f, false)
                                        {
                                            Illustration = rune.Illustration,
                                            ContextMenuText = traceAction.Name,
                                        })
                                    .Cast<Option>()
                                    .ToList();
                                if (newOptions.Count == targets.ChosenCreatures.Count)
                                    options.AddRange(newOptions);
                            }
                            
                            options.Add(new CancelOption(true));
                            options.Add(new PassViaButtonOption("Revert"));

                            Option chosenOption = (await caster.Battle.SendRequest(new AdvancedRequest(
                                caster,
                                "Choose which rune to trace on all targets of Swiping Trace, or right-click to cancel.",
                                options)
                            {
                                TopBarIcon = action.Illustration,
                                TopBarText = "Choose which rune to trace on all targets of Swiping Trace, or right-click to cancel.",
                            })).ChosenOption;
                            
                            await chosenOption.Action();
                            
                            if (chosenOption is CancelOption or PassViaButtonOption
                                || chosenRune is null)
                            {
                                action.RevertRequested = true;
                                return;
                            }

                            // Make attacks
                            int map = caster.Actions.AttackedThisManyTimesThisTurn;
                            bool hasSweep = item.HasTrait(Trait.Sweep);
                            List<Creature> hits = [];
                            foreach (Creature cr in targets.ChosenCreatures)
                            {
                                caster.Actions.AttackedThisManyTimesThisTurn = map;
                                await CommonCombatActions.StrikeCreature(
                                    caster,
                                    strike =>
                                        strike.HasTrait(Trait.Melee) && strike.Item == item,
                                    strike =>
                                    {
                                        if (hasSweep)
                                            strike.StrikeModifiers.QEffectForStrike = new QEffect()
                                            {
                                                BonusToAttackRolls = (_, strike2, _) =>
                                                    action == strike
                                                        ? new Bonus(1, BonusType.Circumstance, "Sweep")
                                                        : null
                                            };
                                        strike.WithHitAndDealDamage(async (caster2, strike2, target) =>
                                            hits.Add(target));
                                    },
                                    target =>
                                        target == cr,
                                    action.Illustration,
                                    "Choose a creature to Strike and Trace a Rune on.",
                                    false,
                                    null);
                            }

                            caster.Actions.AttackedThisManyTimesThisTurn = map + targets.ChosenCreatures.Count;
                            
                            if (hits.Count > 0)
                            {
                                // Play custom animation
                                Sfxs.Play(ModData.SfxNames.TRACE_RUNE);
                                await caster.Battle.WaitForProjectiles(hits
                                    .SelectMany(cr =>
                                        cr.Battle.SpawnOvercreatureProjectileParticles(
                                            1, caster, cr, Color.White, chosenRune.Illustration, true))
                                    .ToList());

                                // Trace the runes
                                bool overheadOnce = false; // Show overhead just the first time
                                foreach (Creature cr in hits)
                                {
                                    CombatAction traceAction = CommonRuneRules
                                        .CreateTraceAction(caster, chosenRune, 2)
                                        .WithActionCost(0)
                                        .WithExtraTrait(Trait.AlwaysHits)
                                        .WithExtraTrait(Trait.ProxyAttack);
                                    traceAction.SoundEffectName = null;
                                    traceAction.ProjectileKind = ProjectileKind.None;
                                    if (overheadOnce)
                                        traceAction.WithExtraTrait(Trait.DoNotShowOverheadOfActionName);

                                    await caster.Battle.GameLoop.FullCast(traceAction,
                                        ChosenTargets.CreateSingleTarget(cr));
                                    overheadOnce = true;
                                }
                            }
                        });

                    return swipingTrace;
                };
            })
            .WithTraceRunePrereq()
            .WithInappropriateBecauseOfBadInventory((values, _) => RequiresOneCreatureRune(values));
        
        #endregion
        
        #region 10th-Level
        
        // Chain of Words
        yield return new TrueFeat(
                ModData.FeatNames.ChainOfWords, 10,
                "You see the thin lines of magic between your creations, and you can exploit that connection with a small shift of power from one rune to another.",
                $$"""
                {b}Frequency{/b} Once per encounter.
                
                You {{ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")}} within 60 feet. In addition to the runes' normal effects, a chain of glowing script flows between them, dealing 5d6 force damage to all creatures in a straight line between them with a basic Reflex saving throw; creatures bearing the invoked runes are not in the area of this line.
                
                A creature who critically fails its save is also immobilized for 1 round or until it Escapes, as the runes seal its movements. The DC to Escape is your class DC.
                
                At 12th level, and every 2 levels thereafter, the damage increases by 1d6.
                """,
                [Trait.Force, Trait.Invocation, Trait.Runesmith])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                int numDice = qfFeat.Owner.Level / 2;
                (int range, string rangeDesc) = CommonRuneRules.GetInvocationRange(qfFeat.Owner, 12);
                
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + UsedUpDescription($" [invocation] (Once per combat) Invoke two Runes within {rangeDesc}. Creatures in-between in a line take {numDice}d6 force damage (basic Reflex save).", !qfThis.UsedUpPermanently, "combat");

                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.UsedUpPermanently)
                        return null;

                    CombatAction chain = new CombatAction(
                            qfThis.Owner,
                            new BagOfIllustrationsIllustration(
                                ModData.Illustrations.InvokeRune,
                                ModData.Illustrations.InvokeRune,
                                IllustrationName.LightningBolt),
                            "Chain of Words",
                            [ModData.ModTrait, Trait.Force, Trait.Invocation, Trait.Runesmith, Trait.Basic, Trait.AlwaysHits, Trait.Zone],
                            null!,
                            Target.MultiplePointLine(
                                2, range,
                                tile =>
                                    tile.PrimaryOccupant is not null
                                    && DrawnRune.IsARuneBearer(qfThis.Owner, tile.PrimaryOccupant)
                                    && (qfThis.Owner.HasLineOfEffectTo(tile) < CoverKind.Blocked
                                        || qfThis.Owner == tile.PrimaryOccupant),
                                useStrictLineInsteadOfCorners: true))
                        .WithActionCost(2)
                        .WithDescription(
                            "You see the thin lines of magic between your creations, and you can exploit that connection with a small shift of power from one rune to another.",
                            $$"""
                              {b}Frequency{/b} Once per encounter.

                              You {{ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")}} within 60 feet. In addition to the runes' normal effects, a chain of glowing script flows between them, dealing {{S.HeightenedVariable(numDice, 5)}}d6 force damage to all creatures in a straight line between them with a basic Reflex saving throw; creatures bearing the invoked runes are not in the area of this line.

                              A creature who critically fails its save is also immobilized for 1 round or until it Escapes, as the runes seal its movements. The DC to Escape is your class DC.
                              """)
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            if (targets.ChosenTiles.Count < 2
                                || targets.ChosenTiles[^1].PrimaryOccupant is not {} first
                                || targets.ChosenTiles[^2].PrimaryOccupant is not {} last
                                || first == last
                                || targets.ChosenCreatures
                                    .Where(cr => cr != first && cr != last)
                                    .ToList() is not {} inBetween
                                || inBetween.Count == 0)
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            
                            // Invoke 2 runes
                            foreach (Creature target in (Creature[])[first, last])
                            {
                                if (!await CommonRuneRules.ChooseARuneToInvoke(
                                        caster,
                                        cr => cr == target,
                                        overrideRange: range,
                                        skipConfirmation: true))
                                {
                                    action.SpentActions = 1;
                                    caster.Battle.Log("Chain of Words converted to a simple Invoke Rune action.");
                                    return;
                                }
                            }

                            // Sound effect
                            Sfxs.Play(SfxName.MagicMissile);
                            
                            // Particle splash on each tile
                            const int numParticles = 10;
                            List<Particle> projectiles = [];
                            foreach (Tile tile in targets.ChosenTiles
                                         .Except(((Creature[])[first, last])
                                             .SelectMany(cr => cr.Space.Tiles))
                                         .ToList())
                            {
                                projectiles.AddRange(caster.Battle.SpawnOvercreatureProjectileParticles(
                                    numParticles, tile, tile, Color.White, IllustrationName.LightningBolt));
                            }
                            await caster.Battle.WaitForProjectiles(projectiles);
                            
                            // Deal damage in-between targets
                            foreach (Creature target in inBetween)
                            {
                                CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                                    target,
                                    action,
                                    new SavingThrow(
                                        Defense.Reflex,
                                        caster.ClassDC(Trait.Runesmith)));

                                await CommonSpellEffects.DealBasicDamage(
                                    action, caster, target, result,
                                    DiceFormula.FromText($"{numDice}d6", "Chain of Words"),
                                    DamageKind.Force);

                                if (result == CheckResult.CriticalFailure)
                                {
                                    QEffect immobilized = QEffect.Immobilized()
                                        .WithExpirationInOneRound(caster);
                                    immobilized.ProvideContextualAction = qfEscape =>
                                        new ActionPossibility(
                                                Possibilities.CreateEscapeAgainstEffect(
                                                    qfEscape.Owner,
                                                    qfEscape,
                                                    "Chain of Words",
                                                    caster.ClassDC(Trait.Runesmith)))
                                            .WithPossibilityGroup(Constants
                                                .POSSIBILITY_GROUP_CONTEXTUAL_GET_RID_OF_DEBUFF);
                                    target.AddQEffect(immobilized);
                                }
                            }

                            qfThis.UsedUpPermanently = true;
                        });

                    return new ActionPossibility(chain)
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
                };
            });
        
        // Clashing Compound Invocation
        yield return new TrueFeat(
                ModData.FeatNames.ClashingCompoundInvocation, 10,
                "As you invoke runes from disparate traditions of magic, their diametrically opposed effects repel each other in a destructive backlash.",
                $"You {ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")}, which must be from opposed {ModData.Tooltips.RuleRuneTradition("traditions")} of magic; either 1 arcane and 1 divine rune, or 1 occult and 1 primal rune. In addition to the runes' normal effects, one creature within 30 feet of both invoked runes must also attempt a Fortitude saving throw as destructive magical harmonics clash.{S.FourDegreesOfSuccess(
                    "The target is unaffected.",
                    "The target is {r}sickened 1{/r}, but automatically succeeds on any check to retch.",
                    "The target is {r}sickened 1{/r}.",
                    "The target is {r}sickened 2{/r}")}",
                [Trait.Invocation, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                (int invokeRange, string invokeDesc) = CommonRuneRules.GetInvocationRange(qfFeat.Owner, (30 / 5));
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction cciOne = CCI(
                        IllustrationName.MagicMissile, IllustrationName.Bless,
                        Trait.Arcane, Trait.Divine);
                    CommonRuneRules.WithImmediatelyRemovesImmunity(cciOne);
                    CombatAction cciTwo = CCI(
                        IllustrationName.Bane,
                        IllustrationName.ElementalForm,
                        Trait.Occult, Trait.Primal);
                    CommonRuneRules.WithImmediatelyRemovesImmunity(cciOne);
                    
                    return new SubmenuPossibility(
                            new SideBySideIllustration(
                                IllustrationName.Sickened,
                                ModData.Illustrations.InvokeRune),
                            "Clashing Compound Invocation")
                        {
                            SpellIfAny = new CombatAction(
                                    qfThis.Owner,
                                    new SideBySideIllustration(
                                        IllustrationName.Sickened,
                                        ModData.Illustrations.InvokeRune),
                                    "Clashing Compound Invocation",
                                    [ModData.ModTrait, Trait.Invocation, Trait.Runesmith],
                                    null!,
                                    Target.Self())
                                .WithDescription(
                                    "As you invoke runes from disparate traditions of magic, their diametrically opposed effects repel each other in a destructive backlash.",
                                    $"You {ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")}, which must be from opposed {ModData.Tooltips.RuleRuneTradition("traditions")} of magic; either 1 arcane, and 1 divine rune, or 1 occult and 1 primal rune. In addition to the runes' normal effects, one creature within 30 feet of both invoked runes must also attempt a Fortitude saving throw as destructive magical harmonics clash.{S.FourDegreesOfSuccess(
                                        "The target is unaffected.",
                                        "The target is {r}sickened 1{/r}, but automatically succeeds on any check to retch.",
                                        "The target is {r}sickened 1{/r}.",
                                        "The target is {r}sickened 2{/r}")}"),
                            Subsections = [
                                new PossibilitySection("Clashing Compound Invocation")
                                {
                                    Possibilities = [
                                        new ActionPossibility(cciOne)
                                        {
                                            Caption = "Arcane & Divine"
                                        },
                                        new ActionPossibility(cciTwo)
                                        {
                                            Caption = "Occult & Primal"
                                        }
                                    ]
                                }
                            ]
                        }
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);

                    CombatAction CCI(Illustration left, Illustration right, Trait tradition1, Trait tradition2)
                    {
                        return CompoundInvocation(
                                qfThis.Owner,
                                left, right,
                                "Clashing Compound Invocation",
                                [ModData.ModTrait, Trait.Invocation, Trait.Runesmith],
                                "As you invoke runes from disparate traditions of magic, their diametrically opposed effects repel each other in a destructive backlash.",
                                $"You {ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")}, which must be from opposed {ModData.Tooltips.RuleRuneTradition("traditions")} of magic; either 1 arcane and 1 divine rune, or 1 occult and 1 primal rune. In addition to the runes' normal effects, one creature within 30 feet of both invoked runes must also attempt a Fortitude saving throw as destructive magical harmonics clash.{S.FourDegreesOfSuccess(
                                    "The target is unaffected.",
                                    "The target is {r}sickened 1{/r}, but automatically succeeds on any check to retch.",
                                    "The target is {r}sickened 1{/r}.",
                                    "The target is {r}sickened 2{/r}")}",
                                $"Invoke either an arcane and divine rune, or an occult and primal rune, then make an enemy within {invokeDesc} of both {{r}}sickened{{/r}} with a Fortitude save.",
                                Target.RangedCreature(invokeRange)
                                    .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement()),
                                (a, d, runesInRange) =>
                                {
                                    bool hasTrad1 = runesInRange.Any(dr => dr.Traditions.Contains(tradition1));
                                    bool hasTrad2 = runesInRange.Any(dr => dr.Traditions.Contains(tradition2));
                                    if (!hasTrad1 && !hasTrad2)
                                        return Usability.NotUsableOnThisCreature($"No {tradition1.ToStringOrTechnical().ToLower()} or {tradition2.ToStringOrTechnical().ToLower()} runes within range");
                                    if (!hasTrad1)
                                        return Usability.NotUsableOnThisCreature($"No {tradition1.ToStringOrTechnical().ToLower()} runes within range");
                                    if (!hasTrad2)
                                        return Usability.NotUsableOnThisCreature($"No {tradition2.ToStringOrTechnical().ToLower()} runes within range");
                                    return Usability.Usable;
                                },
                                invokeRange,
                                tradition1, tradition2,
                                $"Choose a {tradition1.ToStringOrTechnical().ToLower()} rune and a {tradition2.ToStringOrTechnical().ToLower()} rune to invoke",
                                async (action, caster, target, _) =>
                                {
                                    Sfxs.Play(SfxName.Mental);
                                    
                                    int dc = caster.ClassDC(Trait.Runesmith);
                                    CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                                        target,
                                        action,
                                        new SavingThrow(Defense.Fortitude, dc));

                                    if (result == CheckResult.CriticalSuccess)
                                        return;

                                    QEffect sickened = QEffect.Sickened(
                                            result == CheckResult.CriticalFailure ? 2 : 1,
                                            dc)
                                        .With(qf =>
                                        {
                                            qf.Source = caster;
                                            qf.SourceAction = action;

                                            // Encourage automatically-succeeding Retch action.
                                            if (result == CheckResult.Success)
                                            {
                                                qf.Description += "\n\nYou automatically succeed if you Retch.";
                                                qf.ModifyActionPossibility = (_, combatAction) =>
                                                {
                                                    if (combatAction.ActionId is not ActionId.Retch)
                                                        return;
                                                    (combatAction.Target as SelfTarget)?.SelfGoodness = _ =>
                                                        AIConstants.VERY_PREFERRED;
                                                };
                                                /*qf.AdditionalGoodness = (_, combatAction, _) =>
                                                {
                                                    if (combatAction.ActionId is ActionId.Retch)
                                                        return AIConstants.VERY_PREFERRED;
                                                    else
                                                        return 0f;
                                                };*/
                                                qf.AdjustSavingThrowCheckResult = (_,_, combatAction, checkResult) =>
                                                {
                                                    if (combatAction.ActionId is ActionId.Retch)
                                                        return CheckResult.Success;
                                                    else
                                                        return checkResult;
                                                };
                                            }
                                        });

                                    target.AddQEffect(sickened);
                                })
                            .WithTargetingTooltip((action, target, _) =>
                            {
                                int dc = action.Owner.ClassDC(Trait.Runesmith);
                                return CombatActionExecution.BreakdownSavingThrowForTooltip(
                                        action,
                                        target,
                                        new SavingThrow(Defense.Fortitude, dc))
                                    .TooltipDescription;
                            });
                    }
                };
            });
        
        // Overloaded Ammunition
        // PETR: Special ammunition, explosive ammunition
        
        // Prodigal Rune-Singer
        yield return new TrueFeat(
                ModData.FeatNames.ProdigalRuneSinger, 10,
                "You have mastered the art of singing your runes.",
                $"You can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with song once per round instead of once per encounter.",
                [Trait.Runesmith])
            .WithPrerequisite(
                ModData.FeatNames.RuneSinger,
                "Rune-Singer")
            .WithPrerequisite(
                values => values.HasFeat(FeatName.ExpertPerformance),
                "You must be an expert in Performance.");
        
        // Runic Correspondence
        // Has no discernible DD-gameplay value.
        
        #endregion
        
        #region 12th-Level
        
        // Astral Compound Invocation
        yield return new TrueFeat(
                ModData.FeatNames.AstralCompoundInvocation, 12,
                "Invoking runes from two schools of magic that both manipulate the intangible realm of thought results in waves of mental interference that stagger your foes.",
                $"You {ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")}; one must be an {ModData.Tooltips.RuleRuneTradition("arcane rune")}, and one must be an {ModData.Tooltips.RuleRuneTradition("occult rune")}. In addition to the runes' normal effects, one creature within 30 feet of both invoked runes must attempt a Will saving throw.{S.FourDegreesOfSuccess(
                    "The target is unaffected.",
                    "The target is {r}stupefied 1{/r} until the end of your next turn.",
                    "The target is {r}stupefied 2{/r} until the end of your next turn.",
                    "The target is {r}stupefied 3{/r} until the end of your next turn.")}",
                [Trait.Invocation, Trait.Mental, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                (int invokeRange, string invokeDesc) = CommonRuneRules.GetInvocationRange(qfFeat.Owner, 6);
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction aci = CompoundInvocation(
                            qfThis.Owner,
                            IllustrationName.MagicMissile,
                            IllustrationName.Bane,
                            "Astral Compound Invocation",
                            [ModData.ModTrait, Trait.Invocation, Trait.Mental, Trait.Runesmith],
                            "Invoking runes from two schools of magic that both manipulate the intangible realm of thought results in waves of mental interference that stagger your foes.",
                            $"You Invoke two Runes; one must be an arcane rune, and one must be an occult rune. In addition to the runes' normal effects, one creature within 30 feet of both invoked runes must attempt a Will saving throw.{S.FourDegreesOfSuccess(
                                "The target is unaffected.",
                                "The target is {r}stupefied 1{/r} until the end of your next turn.",
                                "The target is {r}stupefied 2{/r} until the end of your next turn.",
                                "The target is {r}stupefied 3{/r} until the end of your next turn.")}",
                            $"Invoke an arcane and occult rune, then stupefy an enemy within {invokeDesc} of both.",
                            Target.RangedCreature(invokeRange)
                                .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement()),
                            (a, d, runesInRange) =>
                            {
                                bool hasArcane = runesInRange.Any(dr => dr.Traditions.Contains(Trait.Arcane));
                                bool hasOccult = runesInRange.Any(dr => dr.Traditions.Contains(Trait.Occult));
                                if (!hasArcane && !hasOccult)
                                    return Usability.NotUsableOnThisCreature("No arcane or occult runes within range");
                                if (!hasArcane)
                                    return Usability.NotUsableOnThisCreature("No arcane runes within range");
                                if (!hasOccult)
                                    return Usability.NotUsableOnThisCreature("No occult runes within range");
                                return Usability.Usable;
                            },
                            invokeRange,
                            Trait.Arcane, Trait.Occult,
                            "Choose an arcane and an occult rune to invoke",
                            async (action, caster, target, _) =>
                            {
                                Sfxs.Play(SfxName.Mental);
                                
                                int dc = caster.ClassDC(Trait.Runesmith);
                                CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                                    target,
                                    action,
                                    new SavingThrow(Defense.Will, dc));

                                if (result == CheckResult.CriticalSuccess)
                                    return;

                                QEffect stupefy = QEffect.Stupefied(
                                        result switch
                                        {
                                            CheckResult.CriticalFailure => 3,
                                            CheckResult.Failure => 2,
                                            _ => 1
                                        })
                                    .WithExpirationAtEndOfSourcesNextTurn(caster, false)
                                    .With(qf =>
                                    {
                                        qf.Source = caster;
                                        qf.SourceAction = action;
                                    });

                                target.AddQEffect(stupefy);
                            })
                        .WithTargetingTooltip((action, target, _) =>
                        {
                            int dc = action.Owner.ClassDC(Trait.Runesmith);
                            return CombatActionExecution.BreakdownSavingThrowForTooltip(
                                    action,
                                    target,
                                    new SavingThrow(Defense.Will, dc))
                                .TooltipDescription;
                        });
                    
                    CommonRuneRules.WithImmediatelyRemovesImmunity(aci);
                    
                    return new ActionPossibility(aci)
                        .WithPossibilityGroup(ModData.PossibilityGroups.INVOKING_RUNES);
                };
            });
        
        // Distant Invocation
        yield return new TrueFeat(
            ModData.FeatNames.DistantInvocation, 12,
            "Your connection to your runes stretches over even greater distances.",
            "Add 30 feet to the range of any of your invocation abilities (typically increasing the range from 30 to 60 feet).",
            [Trait.Runesmith]);

        // Expanded Glossary
        yield return new TrueFeat(
                ModData.FeatNames.ExpandedGlossary, 12,
                "You have memorized more runes than many in your craft.",
                $"Add two {ModData.Tooltips.TraitRune("runes")} of 9th level or lower to your runic repertoire.",
                [Trait.Runesmith])
            .WithOnSheet(values =>
            {
                RunicRepertoireTag.AddRuneSelectionOption(
                    values,
                    "ExpandedGlossaryRune",
                    "Expanded Glossary",
                    9,
                    2);
            });

        // Orbiting Runestone
        yield return new TrueFeat(
                ModData.FeatNames.OrbitingRunestone, 12,
                "You create a runestone that floats around and protects you.",
                $$"""
                {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} with a damaging invocation on the runestone, even if the rune could not normally be applied to an object. The runestone has 10 Hit Points.
                
                Each time a creature successfully Strikes you, attempt a DC 11 flat check. On a success, the runestone blocks the attack, so the attack first damages the runestone and then applies any remaining damage to you.
                
                If this damage reduces the runestone to 0 Hit Points, it explodes outward toward the attacker. If the attacker is within 15 feet, the rune etched upon the runestone invokes automatically, applying its invocation effect to the attacker. The attacker is the only one affected by the invocation, even if the rune could normally affect multiple creatures.
                
                You can maintain only a single orbiting rune at any given time. If you try to create a second, the first one shatters.
                """,
                [Trait.Concentrate, Trait.Earth, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [concentrate] Trace a Rune on an orbiting stone. It has 10 HP. Strikes have a 50% chance to hit the stone, and if it's destroyed, the rune is invoked onto the attacker if they're within 15 feet.";
                
                qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                {
                    if (submenu.SubmenuId != ModData.SubmenuIds.TraceRune
                        || RunicRepertoireTag.GetRepertoire(qfThis.Owner)
                                ?.GetKnownRunes(qfThis.Owner)
                                .Where(rune => rune.InvocationProperties.DealsDamage)
                                .ToList()
                            is not {} knownRunes
                        || knownRunes.Count < 1)
                        return null;

                    CombatAction orbitForDisplay = OrbitingRunestone(null);
                    SubmenuPossibility subMenu = new SubmenuPossibility(
                        orbitForDisplay.Illustration,
                        "Orbiting Runestone")
                    {
                        SpellIfAny = orbitForDisplay,
                        Subsections = [
                            new PossibilitySection("Orbiting Runestone")
                            {
                                Possibilities = knownRunes
                                    .Select(OrbitingRunestone)
                                    .Select(action => new ActionPossibility(action))
                                    .ToList<Possibility>()
                            }
                        ] 
                    };

                    return new PossibilitySection("Orbiting Runestone")
                    {
                        Possibilities = [subMenu]
                    };

                    CombatAction OrbitingRunestone(Rune? rune)
                    {
                        CombatAction orbit = new CombatAction(
                                qfThis.Owner,
                                rune is null
                                    ? ModData.Illustrations.EtchRune
                                    : new SuperimposedIllustration(
                                        ModData.Illustrations.EtchRune,
                                        new SmallerIllustration(
                                            rune.Illustration,
                                            0.666f)),
                                "Orbiting Runestone" + (rune is not null ? $" ({rune.WordName})" : null),
                                [ModData.ModTrait, Trait.Concentrate, Trait.Earth, Trait.Runesmith],
                                $$"""
                                 {i}You create a runestone that floats around and protects you.{/i}
                                 
                                 Trace {{(rune is null ? "a Rune with a damaging invocation on the runestone, even if the rune could not normally be applied to an object" : $"{rune.Illustration.IllustrationAsIconString}{rune.FullName.WithTag("i")} on the runestone".WithColor("Blue"))}}. The runestone has 10 Hit Points.

                                 Each time a creature successfully Strikes you, attempt a DC 11 flat check. On a success, the runestone blocks the attack, so the attack first damages the runestone and then applies any remaining damage to you.

                                 If this damage reduces the runestone to 0 Hit Points, it explodes outward toward the attacker. If the attacker is within 15 feet, the rune etched upon the runestone invokes automatically, applying its invocation effect to the attacker. The attacker is the only one affected by the invocation, even if the rune could normally affect multiple creatures.

                                 You can maintain only a single orbiting rune at any given time. If you try to create a second, the first one shatters.
                                 """,
                                Target.Self())
                            .WithActionCost(1)
                            /*.WithSoundEffect(SfxName.Abjuration)*/;

                        if (rune is not null)
                            orbit.WithEffectOnSelf(async (action, caster) =>
                            {
                                Item runestone = new Item(ModData.Illustrations.EtchRune, "Orbiting Runestone", Trait.CannotBeHeldInHands, Trait.Worn, Trait.EncounterEphemeral);
                                caster.CarriedItems.Add(runestone);
                                CombatAction traceRune = CommonRuneRules
                                    .CreateTraceAction(caster, rune, 1)
                                    .WithActionCost(0);
                                
                                bool hasManipulate = traceRune.Traits.Remove(Trait.Manipulate);

                                // If the rune failed to apply due to an error, do not raise the shield
                                if (await CommonRuneRules.DrawRuneOnCreature(
                                            traceRune, caster, rune, runestone,
                                            // You can apply illegal runes to yourself
                                            ignoreUsageRequirements: true,
                                            // Apply it if the action is not disrupted
                                            doNotApplyImmediately: true)
                                        is not { } drawnRunes
                                    || drawnRunes.LastOrDefault() is not { } drawnRune)
                                {
                                    action.RevertRequested = true;
                                    return;
                                }

                                // Disrupt it
                                if (hasManipulate)
                                {
                                    CombatAction phantomManipulate = CombatAction.CreateSimple(
                                            action.Owner, action.Name,
                                            Trait.Manipulate, Trait.DoNotShowInCombatLog)
                                        .WithIllustration(action.Illustration)
                                        .WithActionCost(0);
                                    await phantomManipulate.Fullcast(phantomManipulate.Owner);
                                    if (phantomManipulate.Disrupted)
                                    {
                                        action.Disrupted = true;
                                        return;
                                    }
                                }
                                
                                // Remove existing 
                                caster.RemoveAllQEffects(qf =>
                                    qf.Traits.Contains(ModData.Traits.Orbiting));

                                Sfxs.Play(traceRune.SoundEffectName);

                                drawnRune.Traits.Add(ModData.Traits.Orbiting);
                                drawnRune.DrawnOn = runestone;
                                drawnRune.DisableRune(true);
                                // It's okay to do this because Atryl is the only
                                // rune that uses .Value, and its functionality is
                                // being suppressed as a runestone.
                                drawnRune.HideValue = false;
                                drawnRune.Value = 10;
                                const string newDescription =
                                    "{Blue}{b}(Orbiting Runestone){/b}{/Blue} When you are hit with a Strike, attempt a DC 11 flat check. On a success, the runestone blocks the attack, taking the damage. If destroyed, and the attacker is within 15 feet, detonate the rune's invocation effects onto the attacker.";
                                if (drawnRune.ItemDescriptionGenerator is not null)
                                    drawnRune.ItemDescriptionGenerator = (_, _) => newDescription;
                                else
                                    drawnRune.Description = newDescription;
                                drawnRune.UpdateDescription();
                                drawnRune.SpawnsAura = _ => new OrbitingRunestoneAuraAnimation();
                                drawnRune.DoNotShowUpOverhead = true;
                                drawnRune.WhenExpires += _ => caster.CarriedItems.Remove(runestone);
                                
                                caster.AddQEffect(drawnRune);

                                CommonRuneRules.LogNewRune(caster, drawnRune.Rune, caster, runestone);
                            });

                        return orbit;
                    }
                };

                qfFeat.YouAreDealtDamageEvent = async (qfThis, dEvent) =>
                {
                    if (dEvent.CombatAction is not { } action
                        || !action.HasTrait(Trait.Strike)
                        || qfThis.Owner.QEffects.FirstOrDefault(qf =>
                                qf is DrawnRune dr
                                && dr.Traits.Contains(ModData.Traits.Orbiting))
                            is not DrawnRune drOrbit)
                        return;

                    // Make a flat check
                    (CheckResult result, string rollText, int roll) = Checks.RollFlatCheckWithRoll(11);
                    result = (CheckResult)Math.Clamp((int)result, (int)CheckResult.Failure, (int)CheckResult.Success);
                    qfThis.Owner.Overhead(
                        $"{((SimpleIllustration)(IllustrationName.BlueD20)).IllustrationAsIconString} {result.GreenifySentenceCaseNoBold()}",
                        Color.White,
                        $"{qfThis.Owner.Name} {(result > CheckResult.Failure ? "succeeds".WithColor("Green") : "fails".WithColor("Red"))} on a flat check for their orbiting runestone to absorb the hit: rolled {roll.WithTag("b")} vs {"DC 11".WithTag("b")}.");

                    if (result < CheckResult.Success)
                        return;

                    int incomingDamage = dEvent.TotalResolvedDamage;
                    int reduceBy = Math.Min(drOrbit.Value, incomingDamage);
                    
                    dEvent.ReduceBy(reduceBy, "Orbiting Runestone");
                    drOrbit.Value -= reduceBy;
                    
                    if (drOrbit.Value < 1)
                    {
                        // This occurs before you are actually taking the damage,
                        // which is appropriate since going down shouldn't prevent this.
                        if (dEvent.Source.DistanceTo(qfThis.Owner) <= 3)
                        {
                            await CommonRuneRules.InvokeDrawnRuneAgainstTarget(
                                qfThis.Owner,
                                drOrbit,
                                dEvent.Source);
                        }
                        
                        drOrbit.ExpiresAt = ExpirationCondition.Immediately;
                    }
                };
            })
            .WithInappropriateBecauseOfBadInventory((values, _) => RequiresOneDamagingRune(values));

        #endregion

        #region 14th-Level

        // Dance of Bloody Ink
        yield return new TrueFeat(
                ModData.FeatNames.DanceOfBloodyInk, 14,
                "As you lash out, your weapon is your brush, wounds forming words of power in your target's flesh.",
                $$"""
                You {{ModData.FeatNames.InvokeRune.ToLink("Invoke a Rune")}} on a melee weapon you are wielding and Strike. Then, you can Step and Strike again with the same weapon. If the second Strike hits, you {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} onto the target of the Strike.

                Both attacks count toward your multiple attack penalty, but you do not increase your penalty until you have made all your attacks.
                """,
                [Trait.Runesmith])
            .WithActionCost(3)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Invoke a Rune on your melee weapon, Strike with it, Step, Strike again, and Trace a Rune on the target if you hit.";

                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee)
                        || item.HasTrait(Trait.Unarmed))
                        return null;

                    List<DrawnRune> runesOnWeapon = DrawnRune
                        .GetDrawnRunes(qfFeat.Owner, qfFeat.Owner)
                        .Where(dr => dr.DrawnOn == item)
                        .ToList();

                    CombatAction basicStrike = qfFeat.Owner.CreateStrike(item, qfFeat.Owner.Actions.AttackedThisManyTimesThisTurn)
                        .WithActionCost(0);

                    CombatAction dance = new CombatAction(
                            qfFeat.Owner,
                            /*new BagOfIllustrationsIllustration(
                                ModData.Illustrations.InvokeRune,
                                item.Illustration,
                                IllustrationName.Walk,
                                item.Illustration)*/
                            new TriplePortraitIllustration(
                                ModData.Illustrations.InvokeRune,
                                ModData.Illustrations.DanceOfBloodyInk,
                                ModData.Illustrations.TraceRune),
                            "Dance of Bloody Ink",
                            [ModData.ModTrait, Trait.Runesmith],
                            """
                            {i}As you lash out, your weapon is your brush, wounds forming words of power in your target's flesh.{/i}

                            You Invoke a Rune on a melee weapon you are wielding and Strike. Then, you can Step and Strike again with the same weapon. If the second Strike hits, you Trace a Rune onto the target of the Strike.

                            Both attacks count toward your multiple attack penalty, but you do not increase your penalty until you have made all your attacks.
                            """,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                {
                                    if (runesOnWeapon.Count == 0)
                                        return "No runes on weapon";
                                    if (!basicStrike.CanBeginToUse(self))
                                        return "Can't Strike";
                                    if (!self.WouldBeAbleToStride())
                                        return "Can't move";
                                    if ((basicStrike.Target as CreatureTarget)!.GetLegalTargetCreatures(self).Count == 0)
                                        return "No target in range";
                                    return null;
                                }))
                        .WithActionCost(3)
                        .WithEffectOnSelf(async (action, caster) =>
                        {
                            if (!await CommonRuneRules.ChooseARuneToInvoke(
                                    caster,
                                    cr => cr == caster,
                                    rune => runesOnWeapon.Select(dr => dr.Rune).Contains(rune),
                                    canBeCanceled: true,
                                    passText: "Abort",
                                    additionalTopText: " This must be a rune on your weapon. (1/5)"))
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            
                            int cacheMap = qfFeat.Owner.Actions.AttackedThisManyTimesThisTurn;
                            Creature? chosenCr1 = null;
                            
                            if (!await CommonCombatActions.StrikeCreature(
                                    caster,
                                    strike => strike.Item == item,
                                    strike => strike
                                        .WithEffectOnEachTarget(async (_, _, target, _) =>
                                            chosenCr1 = target),
                                    null,
                                    action.Illustration,
                                    "Choose a creature to Strike as part of Dance of Bloody Ink. (2/5)",
                                    true,
                                    "Convert to simple Invoke Rune",
                                    cacheMap))
                            {
                                action.SpentActions = 1;
                                caster.Battle.Log("Dance of Bloody Ink converted to a simple Invoke Rune.");
                                return;
                            }

                            if (!await caster.StepAsync($"Choose where to Step as part of Dance of Bloody Ink. You should end your movement near an enemy within reach of your {item.Illustration.IllustrationAsIconString} {item.ShortName}. (3/5)", true, true))
                            {
                                // Don't allow you to revert just because the initial target died.
                                if (chosenCr1?.DeathScheduledForNextStateCheck == true)
                                    return;
                                action.SpentActions = 2;
                                caster.Battle.Log("Dance of Bloody Ink converted to a simple Invoke Rune and Strike.");
                                return;
                            }
                            
                            // No more reversions from here

                            await CommonCombatActions.StrikeCreature(
                                caster,
                                strike => strike.Item == item,
                                strike => strike
                                    .WithEffectOnEachTarget(async (_, caster2, target, result) =>
                                    {
                                        if (result < CheckResult.Success)
                                            return;
                                        await caster2.FictitiousSingleTileMoveBack();
                                        await CommonRuneRules.AskToTraceARune(
                                            caster2,
                                            runeFilter: rune => rune.DrawProperties.IsDrawnOnlyOnCreatures,
                                            targetFilter: cr => cr == target);
                                    }),
                                null,
                                action.Illustration,
                                "Choose a creature to Strike as part of Dance of Bloody Ink. (4/5)",
                                true,
                                "Convert to simple Invoke Rune",
                                cacheMap);
                        });

                    return dance;
                };
            })
            .WithInappropriateBecauseOfBadInventory(FeatInventoryRequirements.RequiresMeleeWeapon);

        // Define the Canvas
        yield return new TrueFeat(
                ModData.FeatNames.DefineTheCanvas, 14,
                "You produce rune-inscribed stones, which whirl in the air before darting away to form a perimeter for a runic canvas.",
                $$"""
                  {b}Frequency{/b} Once per encounter.
                  
                  Place a 20-foot burst on the ground centered on a corner in your space. As long as you're in or adjacent to the canvas, you can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} {icon:Action} directly into the ground, which causes the rune to expand across the entire space, tracing it onto any number of eligible targets within the area.
                  """,
                [Trait.Earth, Trait.Magical, Trait.Runesmith])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + UsedUpDescription(
                        $" (Once per combat) Place a 20-foot burst in your space. While in or adjacent to this canvas, you can Trace a Rune {{icon:Action}} to apply the rune on up to any eligible targets in the area.",
                        !qfThis.UsedUpPermanently,
                        "combat");
                
                // Define the Canvas
                qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                {
                    if (qfThis.UsedUpPermanently
                        || submenu.SubmenuId != ModData.SubmenuIds.TraceRune)
                        return null;

                    CombatAction defineCanvas = new CombatAction(
                            qfThis.Owner,
                            new ScrollIllustration(
                                IllustrationName.Scroll,
                                ModData.Illustrations.TraceRune),
                            "Define the Canvas",
                            [ModData.ModTrait, Trait.Earth, Trait.Magical, Trait.Runesmith],
                            """
                              {i}You produce rune-inscribed stones, which whirl in the air before darting away to form a perimeter for a runic canvas.{/i}

                              {b}Frequency{/b} Once per encounter.

                              Place a 20-foot burst on the ground centered on a corner in your space. As long as you're in or adjacent to the canvas, you can {b}Trace a Rune {icon:Action}{/b} directly into the ground, which causes the rune to expand across the entire space, tracing it onto any number of eligible targets within the area.
                              """,
                            Target.Burst(1, 4)
                                /*.WithAdditionalRequirementOnCenterPoint((burstTar, point) =>
                                    burstTar.OwnerAction.Owner.Space.Tiles
                                        .Select(tile => tile.ToPoint())
                                        .Contains(point))*/)
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Abjuration)
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            if (targets.ChosenTiles.Count == 0)
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            
                            qfThis.UsedUpPermanently = true;

                            Vector2 center = new Vector2(
                                (float)(targets.ChosenTiles.Average(tile => tile.X)),
                                (float)(targets.ChosenTiles.Average(tile => tile.Y))
                            );
                            List<Tile> ring = targets.ChosenTiles
                                .GroupBy(tile =>
                                {
                                    float num1 = Math.Abs(center.X - tile.X);
                                    float num2 = Math.Abs(center.Y - tile.Y);
                                    return num1 >= num2 ? num1 + num2 / 2 : num2 + num1 / 2;
                                })
                                .OrderByDescending(grp => grp.Key)
                                .First()
                                .ToList();
                            
                            // Particle splash on each tile
                            const int numParticles = 10;
                            List<Particle> canvasParticles = [];
                            foreach (Tile tile in ring)
                            {
                                canvasParticles.AddRange(caster.Battle.SpawnOvercreatureProjectileParticles(
                                    numParticles,
                                    new Vector2((float)(center.X + 0.5), (float)(center.Y + 0.5)),
                                    tile, Color.White, ModData.Illustrations.EtchRune));
                            }
                            await caster.Battle.WaitForProjectiles(canvasParticles);
                            
                            // Zone
                            QEffect controller = new QEffect()
                            {
                                Id = ModData.QEffectIds.DefineCanvasController,
                                ProvideActionIntoPossibilitySection = (qfCanvas, section) =>
                                {
                                    if (qfCanvas.Zone is null
                                        || AllRunes.All.FirstOrDefault(rune =>
                                                rune.FullName == section.Name)
                                            is not { } foundRune)
                                        return null;

                                    CombatAction traceRune = CommonRuneRules.CreateTraceAction(
                                            qfCanvas.Owner, foundRune, 2, 99)
                                        .WithActionCost(0);

                                    CombatAction canvasTrace = new CombatAction(
                                            qfCanvas.Owner,
                                            new ScrollIllustration(
                                                IllustrationName.Scroll,
                                                foundRune.Illustration),
                                            $"Canvas{(qfCanvas.Owner.HasEffect(ModData.QEffectIds.RuneSinger) ? " & Sing" : null)} Rune ({foundRune.WordName})",
                                            traceRune.Traits.ToArray(),
                                            traceRune.Description.Replace(
                                                traceRune.Target.ToDescription() ?? "Replaces nothing",
                                                "{Blue}{b}Range{/b} Any targets within your canvas{/Blue}"
                                                + (qfCanvas.Owner.HasEffect(ModData.QEffectIds.RuneSinger)
                                                   && !qfCanvas.Owner.HasFeat(ModData.FeatNames.GenerationalRuneSinger)
                                                    ? $"\n{{Blue}}{{b}}Frequency{{/b}} Once per {(qfCanvas.Owner.HasFeat(ModData.FeatNames.ProdigalRuneSinger) ? "round" : "combat")} (Rune-Singer){{/Blue}}\n"
                                                    : null)),
                                            Target.MultipleCreatureTargets(
                                                qfCanvas.Zone.CreaturesInZone.Count(cr =>
                                                    IsLegalTarget(qfCanvas.Owner, cr)),
                                                () =>
                                                {
                                                    return CommonRuneRules.CreateDrawActionTarget(foundRune, 2, 99)
                                                        // You must be in or adjacent to your canvas
                                                        .WithAdditionalConditionOnTargetCreature((a, _) =>
                                                        {
                                                            return qfCanvas.Zone.AffectedTiles.Any(tile =>
                                                                qfCanvas.Owner.IsAdjacentTo(tile)
                                                                || qfCanvas.Owner.Space.Tiles.Contains(tile))
                                                                ? Usability.Usable
                                                                : Usability.NotUsable(
                                                                    "Must be in or adjacent to your canvas");
                                                        })
                                                        // Target must be in your canvas.
                                                        .WithAdditionalConditionOnTargetCreature((_, d) =>
                                                        {
                                                            return qfCanvas.Zone.CreaturesInZone.Contains(d)
                                                                ? Usability.Usable
                                                                : Usability.NotUsableOnThisCreature(
                                                                    "Not in your canvas");
                                                        });
                                                }))
                                        .WithActionId(ModData.ActionIds.TraceRune)
                                        .WithActionCost(1)
                                        .WithSoundEffect(traceRune.SoundEffectName!)
                                        .WithProjectileCone(VfxStyle.BasicProjectileCone(foundRune.Illustration))
                                        .WithEffectOnChosenTargets(async (canvasAction, caster2, targets2) =>
                                        {
                                            // Draw on each target
                                            List<DrawnRune> drawings = [];
                                            foreach (Creature cr in targets2.ChosenCreatures.ToList())
                                                drawings.AddRange(await CommonRuneRules.DrawRuneOnCreature(canvasAction, cr, foundRune) ?? []);
                                            
                                            // Log the event
                                            caster2.Battle.Log(
                                                $"{caster2.Name} traces {foundRune.ToWordString()} onto:");
                                            caster2.Battle.LogIndent++;
                                            foreach (DrawnRune dr in drawings.Cast<DrawnRune>())
                                            {
                                                string owner = dr.Owner.Name;
                                                string? subTarget = dr.DrawnOn switch
                                                {
                                                    Item subItem =>
                                                        $"{subItem.Illustration.IllustrationAsIconString} {subItem.ShortName}",
                                                    DrawnRune subRune =>
                                                        subRune.ToWordString(),
                                                    _ => null
                                                };
                                                caster2.Battle.Log(
                                                    $"{owner}{(subTarget is null ? null : $"'s {subTarget}")}.");
                                            }
                                            caster2.Battle.LogIndent--;
                                        });

                                    ActionPossibility bloodPossibility = new ActionPossibility(canvasTrace)
                                    {
                                        Caption = "Define the Canvas"
                                    };
                                    return bloodPossibility;

                                    bool IsLegalTarget(Creature runesmith, Creature target) =>
                                        (traceRune.Target as CreatureTarget)
                                        ?.IsLegalTarget(runesmith, target).CanBeUsed == true;
                                }
                            };
                            caster.AddQEffect(controller);
                            Zone canvas = Zone.SpawnStaticAndApply(
                                controller, targets.ChosenTiles,
                                zone =>
                                {
                                    zone.TileEffectCreator = tile => new TileQEffect(tile)
                                    {
                                        Illustration = new AlphaIllustration(
                                            new SmallerIllustration(
                                                new ScrollIllustration(
                                                    IllustrationName.Scroll,
                                                    ModData.Illustrations.TraceRune),
                                                0.666f),
                                            0.5f)
                                    };
                                });
                        });

                    return new PossibilitySection("Define the Canvas")
                    {
                        Possibilities = [
                            new ActionPossibility(defineCanvas)
                        ]
                    };
                };
            });

        // Henge Gate
        yield return new TrueFeat(
                ModData.FeatNames.HengeGate, 14,
                "You hold a rune in your heart and strike the earth, causing two standing stones to rise nearby, your chosen rune carved upon their surfaces and blazing in the air above them.",
                """
                {b}Frequency{/b} Once per encounter.

                Place two stones in two unoccupied 5-foot spaces within 30 feet of you and within 15 feet of each other. When you raise the henge gate, choose one rune that can be drawn on a creature to carve into the stones.
                
                If you or an ally makes a successful ranged Strike using physical ammunition and that attack passes through the henge gate, the ammunition gains that rune in flight and automatically traces it on the target.
                """,
                [Trait.Earth, Trait.Runesmith])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + UsedUpDescription(
                        " (Once per combat) Place two stones in two spaces within 30 feet of you and 15 feet of each other, and choose a rune. Successful ranged Strikes with ammunition passing through this line automatically trace this rune on the target.",
                        !qfThis.UsedUpPermanently,
                        "combat");
                
                // Henge Gate
                qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                {
                    if (qfThis.UsedUpPermanently
                        || submenu.SubmenuId != ModData.SubmenuIds.TraceRune
                        || RunicRepertoireTag.GetRepertoire(qfThis.Owner)
                                ?.GetKnownRunes(qfThis.Owner)
                                .Where(rune => rune.DrawProperties.IsDrawnOnlyOnCreatures)
                                .ToList()
                            is not {} knownRunes
                        || knownRunes.Count < 1)
                        return null;

                    CombatAction hengeGateForDisplay = HengeGate(null);
                    SubmenuPossibility subMenu = new SubmenuPossibility(
                        hengeGateForDisplay.Illustration,
                        "Henge Gate")
                    {
                        SpellIfAny = hengeGateForDisplay,
                        Subsections = [
                            new PossibilitySection("Henge Gate")
                            {
                                Possibilities = knownRunes
                                    .Select(HengeGate)
                                    .Select(action => new ActionPossibility(action))
                                    .ToList<Possibility>()
                            }
                        ] 
                    };

                    return new PossibilitySection("Henge Gate")
                    {
                        Possibilities = [subMenu]
                    };

                    CombatAction HengeGate(Rune? rune)
                    {
                        string? aOrAn = rune?.FullName.WithIndefiniteArticle()
                            .Replace(rune.FullName, "")
                            .Replace(" ", "");
                        
                        CombatAction hengeGate = new CombatAction(
                            qfThis.Owner,
                            new DoublePortraitIllustration(
                                rune?.Illustration ?? ModData.Illustrations.EtchRune,
                                rune?.Illustration ?? ModData.Illustrations.EtchRune),
                            "Henge Gate" + (rune is not null ? $" ({rune.WordName})" : null),
                            [ModData.ModTrait, Trait.Earth, Trait.Runesmith],
                            $$"""
                              {i}You hold a rune in your heart and strike the earth, causing two standing stones to rise nearby, your chosen rune carved upon their surfaces and blazing in the air above them.{/i}

                              {b}Frequency{/b} Once per encounter.

                              Place two stones in two unoccupied 5-foot spaces within 30 feet of you and within 15 feet of each other. When you raise the henge gate, {{(rune is null ? "choose one rune that can be drawn on a creature to carve into the stones" : $"it is carved with {aOrAn} {rune.Illustration.IllustrationAsIconString} {rune.FullName.WithTag("i")} rune".WithColor("Blue"))}}.
                              
                              If you or an ally makes a successful ranged Strike using physical ammunition and that attack passes through the henge gate, the ammunition gains that rune in flight and automatically traces it on the target.
                              """,
                            Target.MultiplePointLine(
                                2, 3 + 1 /*Idk why 3 is 10-foot instead of 15*/,
                                tile =>
                                    tile.PrimaryOccupant == null
                                    && !tile.AlwaysBlocksLineOfEffect
                                    && tile.DistanceTo(qfThis.Owner) <= 6
                                    && qfThis.Owner.HasLineOfEffectToIgnoreLesser(tile) < CoverKind.Blocked,
                                false, true))
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.ElementalBlastEarth);
                        
                        if (rune is not null)
                            hengeGate.WithEffectOnChosenTargets(async (action, caster, targets) =>
                            {
                                if (targets.ChosenTiles.Count < 2
                                    // First and second seem to be the last two tiles
                                    // for multi-point-line targets. Not sure in which order,
                                    // but this use-case doesn't care which one is which.
                                    || targets.ChosenTiles[^1] is not { } first
                                    || targets.ChosenTiles[^2] is not { } second)
                                {
                                    action.RevertRequested = true;
                                    return;
                                }

                                qfThis.UsedUpPermanently = true;

                                // Rising stones animation, as particle splash
                                // Uses 10pt density for bigger animation
                                List<Particle> pillarParticles = [];
                                foreach (Tile tile in (Tile[])[first, second])
                                {
                                    pillarParticles.AddRange(caster.Battle
                                        .SpawnOvercreatureProjectileParticles(
                                            10, tile, tile, Color.White, ModData.Illustrations.EtchRune));
                                }
                                await caster.Battle.WaitForProjectiles(pillarParticles);

                                // Redetermine the area.
                                LineAreaTarget gateLine = Target.Line(first.DistanceTo(second))
                                    .WithLesserDistanceIsOkay();
                                gateLine.IncludeCaster = true;
                                // Using the pseudocreature here avoids some instances where the
                                // runesmith's tile won't be included in the area between the
                                // two pillars.
                                gateLine.SetOwnerAction(CombatAction.CreateSimple(
                                    caster.Battle.Pseudocreature, ""));
                                HashSet<Tile> gateTiles = Areas.DetermineTiles(
                                        gateLine,
                                        first,
                                        second.ToCenterVector())
                                    .TargetedTiles;

                                // Zone
                                QEffect controller = new QEffect()
                                    {
                                        Id = ModData.QEffectIds.HengeGateController,
                                        Tag = rune
                                    }
                                    .AddGrantingOfTechnical(
                                        (qfZone, cr) =>
                                            cr.FriendOf(qfFeat.Owner)
                                            && qfFeat.Owner.HasEffect(ModData.QEffectIds.HengeGateController),
                                        (qfZone, qfTech) => qfTech.AfterYouTakeAction = async (qfTech2, strike) =>
                                        {
                                            if (!strike.HasTrait(Trait.Strike)
                                                || !strike.HasTrait(Trait.Ranged)
                                                || !strike.HasTrait(Trait.Weapon)
                                                || strike.HasTrait(Trait.Unarmed)
                                                || strike.CheckResult < CheckResult.Success
                                                || strike.ChosenTargets.ChosenCreature is not {} target
                                                || qfZone.Zone is null
                                                || qfZone.Tag is not Rune inscribedRune)
                                                return;

                                            LineAreaTarget strikeLine = Target.Line(strike.Owner.DistanceTo(target))
                                                .WithLesserDistanceIsOkay();
                                            strikeLine.IncludeCaster = true;
                                            strikeLine.SetOwnerAction(CombatAction.CreateSimple(
                                                caster.Battle.Pseudocreature, ""));
                                            HashSet<Tile> strikePath = Areas.DetermineTiles(
                                                    strikeLine,
                                                    strike.Owner.Space.CenterTile,
                                                    target.Space.CenterTile.ToCenterVector())
                                                .TargetedTiles;
                                        
                                            if (!strikePath.Any(qfZone.Zone.AffectedTiles.Contains))
                                                return;

                                            if (await CommonRuneRules.DrawRuneOnCreature(
                                                        CommonRuneRules.CreateTraceAction(
                                                            hengeGate.Owner, inscribedRune,
                                                            2, 99),
                                                        target,
                                                        inscribedRune)
                                                    is not {} runesDrawn
                                                || runesDrawn.LastOrDefault() is not {} runeDrawn)
                                                return;

                                            Sfxs.Play(ModData.SfxNames.TRACE_RUNE);
                                            CommonRuneRules.LogNewRune(hengeGate.Owner, runeDrawn.Rune, target, runeDrawn.DrawnOn);
                                        });
                                caster.AddQEffect(controller);
                                Zone gateZone = Zone.SpawnStaticAndApply(controller, gateTiles.ToList(), zone =>
                                {
                                    zone.TileEffectCreator = tile =>
                                    {
                                        bool isPillar =
                                            // ReSharper disable once PossibleUnintendedReferenceComparison
                                            tile == first
                                            // ReSharper disable once PossibleUnintendedReferenceComparison
                                            || tile == second;
                                        return new TileQEffect(tile)
                                        {
                                            Illustration = new AlphaIllustration(
                                                new SmallerIllustration(
                                                    isPillar
                                                        ? new SuperimposedIllustration(
                                                            ModData.Illustrations.EtchRune,
                                                            new SmallerIllustration(
                                                                rune.Illustration,
                                                                0.666f))
                                                        : ModData.Illustrations.EtchRune,
                                                    isPillar ? 0.75f : 0.4f),
                                                0.5f)
                                        };
                                    };
                                });
                            });

                        return hengeGate;
                    }
                };
            })
            .WithInappropriateBecauseOfBadInventory((values, _) =>
                RequiresOneRune(
                    values,
                    rune => rune.DrawProperties.IsDrawnOnlyOnCreatures,
                    "a rune that is drawn on creatures"));

        // Unerring Runic Attraction
        yield return new TrueFeat(
                ModData.FeatNames.UnerringRunicAttraction, 14,
                "Your runes act as beacons for your arrows and bolts.",
                "Make a ranged Strike against a foe you're aware of that's currently bearing a rune and within your first range increment. The ammunition homes in on your target, even going around corners. Your Strike doesn't require line of sight, ignores the target's concealed condition, and ignores all circumstance bonuses to AC granted from cover and shields.",
                [Trait.Magical, Trait.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Make a ranged Strike against a rune-bearer within your first range increment. Ignore concealed, cover, shields, and line of sight.";

                qfFeat.ProvideStrikeModifierIncludingForThrownStrike = (item, thrown) =>
                {
                    if ((!item.HasTrait(Trait.Ranged)
                         && !thrown)
                        || item.WeaponProperties!.RangeIncrement < 1
                        || item.HasTrait(Trait.Unarmed))
                        return null;

                    StrikeModifiers strikeMods = new StrikeModifiers()
                    {
                        AdditionalTraits = [Trait.IgnoreAllCover],
                        HuntersAim = true, // Ignore concealed but not hidden
                    };
                    CombatAction unerr = StrikeRules
                        .CreateStrike(
                            qfFeat.Owner,
                            item,
                            RangeKind.Ranged,
                            -1,
                            thrown,
                            strikeMods)
                        .WithStrikeNameAndIllustrationChange(
                            "Unerring Runic Attraction",
                            IllustrationName.TargetSheet,
                            thrown)
                        .WithExtraTrait(0, ModData.ModTrait)
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            strikeMods,
                            prologueText: "{b}Requirements{/b} A creature within the first range increment of your weapon is bearing one of your runes.",
                            additionalAttackRollText: "Ignore the required target's {r}concealed{/r} condition, circumstance bonuses to AC from cover and shields, and line of sight requirements.")
                            .Replace("\n\n{Blue}{b}Requirements", "\n{Blue}{b}Requirements")
                            .Replace("{/Blue} Make", "{/Blue}\n\nMake"))
                        .WithAdjustTarget<CreatureTarget>(crTar =>
                        {
                            crTar.CreatureTargetingRequirements.RemoveAll(req =>
                                req is UnblockedLineOfEffectCreatureTargetingRequirement);
                            crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                                a.DistanceTo(d) <= item.WeaponProperties.RangeIncrement
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("Not within 1st range increment"));
                            crTar.WithAdditionalConditionOnTargetCreature(new TargetIsARuneBearer());
                        })
                        .WithTemporaryEffectOnCreatures((_,_) => IgnoreShields())
                        .WithTargetingTooltip((action, target, index) =>
                        {
                            QEffect ignore = IgnoreShields();
                            target.AddQEffect(ignore);
                            string description = CombatActionExecution
                                .BreakdownAttackForTooltip(action, target)
                                .TooltipDescription;
                            target.RemoveAllQEffects(qf => qf == ignore);
                            return description;
                        });
                    if (!unerr.HasTrait(Trait.Magical))
                        unerr.WithExtraTrait(0, ModData.ModTrait);

                    return unerr;

                    QEffect IgnoreShields() => new QEffect()
                    {
                        BeforeBonusesAreFlattened = (qfStrike, list, reason, action, defender) =>
                        {
                            if (reason != BonusCalculationReason.Defense
                                || action?.HasTrait(Trait.Strike) != true
                                || action.Item != item)
                                return;
                            list.RemoveAll(bonus =>
                                bonus is { Amount: > 0, BonusType: BonusType.Circumstance }
                                && bonus.BonusSource.ToLower() is { } name
                                && (name.Contains("raised shield") || name.Contains("shield spell")));
                        }
                    };
                };
            })
            .WithInappropriateBecauseOfBadInventory(RequiresPhysicalProjectile);

        #endregion

        #region 16th-Level

        // By Your Name

        // Maze of Runes

        // Return unto Runes

        // Runesight

        #endregion

        #region 18th-Level

        // Annihilating Compound Invocation

        // Living Lexicon

        // Unbounded Invocations
        yield return new TrueFeat(
            ModData.FeatNames.UnboundedInvocations, 18,
            "Your words can shake the very foundations of the world.",
            "When you Invoke Runes, you can invoke any number of runes within 30 feet instead of just two.",
            [Trait.Runesmith]);

        #endregion

        #region 20th-Level

        // Forge New Word

        // Generational Rune-Singer
        yield return new TrueFeat(
                ModData.FeatNames.GenerationalRuneSinger, 20,
                "You are a once-in-a-generation genius.",
                "You can Trace a Rune with song at will and at a range of 60 feet.",
                [Trait.Runesmith])
            .WithPrerequisite(
                ModData.FeatNames.ProdigalRuneSinger,
                "Prodigal Rune-Singer")
            .WithPrerequisite(
                values => values.HasFeat(FeatName.LegendaryPerformance),
                "You must be an expert in Performance.");

        // Shades of Meaning

        // Stone Forge of the First

        #endregion
    }

    public static string UsedUpDescription(string textIfUsable, bool isUsable, string usableHowOften = "day")
    {
        return isUsable ? textIfUsable : $"(Used this {usableHowOften})".WithTag("strike");
    }

    public static CombatAction CreateFortifyingKnockAction(
        Creature runesmith,
        Rune rune,
        Item shield,
        Action<CombatAction>? modifyTraceAction,
        Action<DrawnRune>? doWhatIfDrawn)
    {
        // Make instances of each rune with description adjustments
        CombatAction knockThisRune = CommonRuneRules
            .CreateTraceAction(
                runesmith, rune,
                actionVersion: 0,
                actionName: "Fortifying Knock",
                castsOrUses: "knocks")
            .WithIllustration(new SideBySideIllustration(
                shield.Illustration,
                rune.Illustration))
            .WithActionCost(1)
            .WithExtraTrait(Trait.Flourish)
            .WithSoundEffect(null)
            .WithNewTarget(Target.Self((self, ai) =>
                ai.GainBonusToAC(
                    CommonShieldRules.GetAC(shield)!.Value
                    + 1 // Always better than standard Raise a Shield
                    + (rune.Id is RuneId.Holtrik && !self.HasEffect(qf => qf is DrawnRune dr && dr.Rune.Id == RuneId.Holtrik) ? 1 : 0) // Include status bonus from Holtrik
                    )));
        
        knockThisRune.Description = CommonRuneRules.StatBlocks
            .CreateDrawActionDescription(
                rune, knockThisRune,
                customUsageLine: "drawn on your raised shield");

        bool hasManipulate = knockThisRune.Traits.Remove(Trait.Manipulate);
        
        // Replace effect behavior
        knockThisRune.EffectOnOneTarget = async (knockAction, caster, target, _) =>
        {
            // Trace the rune, but do not apply it right away
            // If the rune failed to apply due to an error, do not raise the shield
            Rune rune2 = (knockAction.Tag as RuneActionTag)?.Rune!;
            if (await CommonRuneRules.DrawRuneOnCreature(
                        knockAction, target, rune2,
                        // You can apply illegal runes to your shield with Runic Reprisal
                        ignoreUsageRequirements: true,
                        // Apply it if the action is not disrupted
                        doNotApplyImmediately: true,
                        alternativeTarget: shield)
                    is not { } drawnRunes
                || drawnRunes.LastOrDefault() is not { } drawnRune)
            {
                knockAction.RevertRequested = true;
                return;
            }

            // Raise a shield
            Fighter.RaiseShield(caster, shield, caster, false);
            caster.Battle.Log($"{caster.Name} raises their {shield.ShortName}.");
            Sfxs.Play(SfxName.RaiseShield);

            // Now disrupt it
            if (hasManipulate)
            {
                CombatAction phantomManipulate = CombatAction.CreateSimple(
                        knockAction.Owner, knockAction.Name,
                        Trait.Manipulate, Trait.DoNotShowInCombatLog)
                    .WithIllustration(knockAction.Illustration)
                    .WithActionCost(0);
                await phantomManipulate.Fullcast(phantomManipulate.Owner);
                if (phantomManipulate.Disrupted)
                {
                    knockAction.Disrupted = true;
                    return;
                }
            }

            Sfxs.Play(ModData.SfxNames.TRACE_RUNE);
            target.AddQEffect(drawnRune);

            drawnRune.DrawnOn = shield;
            doWhatIfDrawn?.Invoke(drawnRune);

            CommonRuneRules.LogNewRune(caster, drawnRune.Rune, target, shield);
        };
        
        modifyTraceAction?.Invoke(knockThisRune);

        return knockThisRune;
    }

    public static CombatAction CompoundInvocation(
        Creature runesmith,
        Illustration iconLeft,
        Illustration iconRight,
        string name,
        Trait[] traits,
        string flavorText,
        string rulesText,
        string shortDescription,
        CreatureTarget creatureTarget,
        Func<Creature,Creature,List<DrawnRune>,Usability> additionalRequirement,
        int invokeRange,
        Trait tradition1,
        Trait tradition2,
        string topBarText,
        Delegates.EffectOnEachTarget toTargetOnInvocation)
    {
        CombatAction compound = new CombatAction(
                runesmith,
                new BagOfIllustrationsIllustration(
                    iconLeft,
                    iconRight,
                    ModData.Illustrations.InvokeRune),
                name,
                traits,
                $$"""
                {i}{{flavorText}}{/i}

                {{rulesText}}
                """,
                creatureTarget.WithAdditionalConditionOnTargetCreature((a, d) =>
                {
                    List<DrawnRune> allRunes = DrawnRune.GetAllDrawnRunes(a);
                    if (allRunes.Count == 0)
                        return Usability.NotUsable("No runes");
                    List<DrawnRune> runesInRange = allRunes
                        .Where(dr => dr.Owner.DistanceTo(d) <= invokeRange)
                        .ToList();
                    if (runesInRange.Count == 0)
                        return Usability.NotUsableOnThisCreature("No runes within range");
                    return additionalRequirement(a, d, runesInRange);
                }))
            .WithActionCost(1)
            .WithShortDescription(shortDescription)
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                List<DrawnRune> runesInRange = DrawnRune.GetAllDrawnRunes(caster)
                    .Where(dr =>
                        dr.Traditions.Count > 0
                        && dr.Owner.DistanceTo(target) <= invokeRange)
                    .ToList();
                List<DrawnRune> runesOfTrad1 = runesInRange
                    .Where(dr => dr.Traditions.Contains(tradition1))
                    .ToList();
                List<DrawnRune> runesOfTrad2 = runesInRange
                    .Where(dr => dr.Traditions.Contains(tradition2))
                    .ToList();
                List<DrawnRune> allRunes = runesOfTrad1
                    .Concat(runesOfTrad2)
                    .Distinct()
                    .ToList();
                    
                // Revert if you can't begin the invocation.
                if (allRunes.Count < 2
                    || runesOfTrad1.Count == 0
                    || runesOfTrad2.Count == 0)
                {
                    action.RevertRequested = true;
                    return;
                }

                // Choose two runes to invoke
                (Option Option, DrawnRune DrawnRune)? firstRune = null;
                (Option Option, DrawnRune DrawnRune)? secondRune = null;
                for (int i=0; i < 2; i++)
                {
                    // List of options linked to drawn runes
                    List<(Option Option, DrawnRune DrawnRune)> runeOptions = [];
                    
                    // Filter valid options if second execution
                    List<DrawnRune> validRunes;
                    if (firstRune is null)
                        validRunes = allRunes.ToList();
                    else
                    {
                        bool isTrad1 = runesOfTrad1.Contains(firstRune.Value.DrawnRune);
                        bool isTrad2 = runesOfTrad2.Contains(firstRune.Value.DrawnRune);
                        if (isTrad1 && !isTrad2)
                            validRunes = runesOfTrad2.ToList();
                        else if (isTrad2 && !isTrad1)
                            validRunes = runesOfTrad1.ToList();
                        else
                            validRunes = allRunes.ToList();
                        validRunes.Remove(firstRune.Value.DrawnRune);
                    }

                    // Transform DrawnRune into CreatureOption
                    foreach (DrawnRune dr in validRunes)
                    {
                        // Create action to execute on the bearer
                        if (CommonRuneRules.CreateInvokeAction(caster, dr)
                            is not { } invokeThis)
                            continue;
                        
                        invokeThis.WithActionCost(0);
                        invokeThis.ContextMenuName = $"{invokeThis.Name} ({string.Join(", ", dr.Traditions.Select(trait => trait.ToStringOrTechnical()))})";
                        
                        // Collect the new options and link them to a DrawnRune
                        List<Option> thisOptions = [];
                        GameLoop.AddDirectUsageOnCreatureOptions(invokeThis, thisOptions);
                        runeOptions.AddRange(thisOptions.Select(opt => (opt, dr)));
                    }
                    
                    // Reverts if insufficient options
                    if (runeOptions.Count == 0)
                        break;

                    Sfxs.Play(SfxName.OminousActivation);
                    caster.Overhead($"Choose a rune ({i+1}/2)", Color.White);

                    List<Option> awaitableOptions = runeOptions
                        .Select(pair => pair.Option)
                        .Append(new CancelOption(true))
                        .Append(new PassViaButtonOption(" Revert action "))
                        .ToList();
                    
                    // Select an invocation
                    Option chosenOption = (await caster.Battle.SendRequest(
                        new AdvancedRequest(
                            caster,
                            $"{topBarText.Trim('.')}.",
                            awaitableOptions)
                        {
                            TopBarText = $"{topBarText.Trim('.')}, or right-click to cancel ({i+1}/2)",
                            TopBarIcon = action.Illustration,
                        })).ChosenOption;

                    // Revert if canceled
                    if (chosenOption is CancelOption or PassViaButtonOption)
                    {
                        action.RevertRequested = true;
                        return;
                    }

                    // Get rune option from chosen option
                    (Option Option, DrawnRune DrawnRune) runeOption =
                        runeOptions[awaitableOptions.IndexOf(chosenOption)];

                    if (firstRune is null)
                        firstRune = runeOption;
                    else
                        secondRune = runeOption;
                }
                
                // Revert if choices were not made
                if (firstRune is null || secondRune is null)
                {
                    action.RevertRequested = true;
                    return;
                }
                
                // Invoke runes
                await firstRune.Value.Option.Action();
                await secondRune.Value.Option.Action();

				// Then the thing it does after invocation
                await toTargetOnInvocation(action, caster, target, result);
            });
        CommonRuneRules.WithImmediatelyRemovesImmunity(compound);

        return compound;
    }

    public static QEffect TemporaryRunicRepertoire(Creature? runesmith, RuneId[] runesKnown)
    {
        if (runesmith?.FindQEffect(ModData.QEffectIds.TemporaryRunicRepertoire)
            is { Tag: List<Rune> tempRunes } tempRep)
        {
            tempRep.Tag = tempRunes
                .Union(ToRunes(runesKnown))
                .ToList();
            return tempRep;
        }
        
        return new QEffect()
        {
            Id = ModData.QEffectIds.TemporaryRunicRepertoire,
            Tag = ToRunes(runesKnown),
        };

        List<Rune> ToRunes(RuneId[] ids) =>
            ids.Select(AllRunes.GetRune).WhereNotNull().ToList();
    }

    public static string? RequiresOneRune(CalculatedCharacterSheetValues values, Func<Rune, bool> matches, string youMustKnowWhat)
    {
        if (RunicRepertoireTag.GetRepertoire(values) is not { } repertoire)
            return "This feat only works if you have a runic repertoire.";
        if (repertoire.GetKnownRunes(values) is not { } knownRunes
            || knownRunes.Count < 1
            || !knownRunes.Any(matches))
            return $"This feat only works if you know {youMustKnowWhat}.";
        return null;
    }

    public static string? RequiresOneCreatureRune(CalculatedCharacterSheetValues values)
    {
        return RequiresOneRune(
            values,
            rune => rune.DrawProperties.IsDrawnOnlyOnCreatures,
            "a rune that is drawn on creatures");
    }

    public static string? RequiresOneDamagingRune(CalculatedCharacterSheetValues values)
    {
        return RequiresOneRune(
            values,
            rune => rune.InvocationProperties.DealsDamage,
            "a rune whose invocation deals damage");
    }

    public static string? RequiresEngraving(CalculatedCharacterSheetValues values, Inventory inventory)
    {
        return RequiresOneCreatureRune(values)
               ?? FeatInventoryRequirements.RequiresMeleeWeapon(values, inventory);
    }

    public static string? RequiresPhysicalProjectile(CalculatedCharacterSheetValues values, Inventory inventory)
    {
        return FeatInventoryRequirements.RequiresOne(
            inventory,
            item =>
                item.HasTrait(Trait.Weapon)
                && item.HasTrait(Trait.Ranged)
                && !item.HasTrait(Trait.Unarmed)
                // Apparently I've been misinterpreting it.
                /*&& !item.HasTrait(Trait.Thrown)*/,
            "a ranged weapon that uses ammunition");
    }

    public static TrueFeat WithTraceRunePrereq(this TrueFeat ft)
    {
        ft.WithPrerequisite(
            values =>
                values.HasFeat(ModData.FeatNames.TraceRune)
                || values.HasFeat(ModData.FeatNames.TracingStudies),
            "You must be able to Trace Runes.");
        return ft;
    }
}