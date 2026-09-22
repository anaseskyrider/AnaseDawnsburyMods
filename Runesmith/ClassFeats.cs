using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
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
        
        // TODO: Phase 2, level 1 class feats.
        
        // Backup Runic Enhancement
        yield return new TrueFeat(
                ModData.FeatNames.BackupRunicEnhancement, 1,
                "While you are not a spellcaster, you have a working knowledge of the most fundamental runic magic.",
                $"Choose either {SpellId.MagicFang.ToLink("runic body", ModData.Traits.Runesmith, null)} or {SpellId.MagicWeapon.ToLink("runic weapon", ModData.Traits.Runesmith, null)}. You can cast this spell once per day as an innate spell, and its rank is equal to half your level, rounded up.",
                [ModData.Traits.Runesmith])
            .WithOnSheet(values =>
            {
                Trait origin = ModData.Traits.Runesmith;
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
                if (self.Spellcasting?.GetSourceByOrigin(ModData.Traits.Runesmith)
                    is not { } source)
                    return;
                
                /*SpellcastingSource source = self.GetOrCreateSpellcastingSource(
                    SpellcastingKind.Innate,
                    ModData.Traits.Runesmith,
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
                [Trait.Flourish, ModData.Traits.Runesmith])
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
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            if (result >= CheckResult.Success)
                            {
                                // So that you aren't blocking the target's square during the trace await
                                await caster.FictitiousSingleTileMoveBack();
                                
                                // If this Strike kills the target, don't trace a rune,
                                // but also don't get the chance to convert to a simple Strike 
                                if (!target.DeathScheduledForNextStateCheck)
                                {
                                    if (await CommonRuneRules.ChooseACreatureToDrawOn(caster,
                                                // No runes that target items
                                                runeFilter: rune =>
                                                    !rune.DrawProperties.IsDrawnOnAnyItem,
                                                targetFilter: cr => cr == target,
                                                canBeCanceled: true)
                                            is not { } chosenOption
                                        || chosenOption is CancelOption or PassViaButtonOption)
                                    {
                                        caster.Battle.Log("Engraving Strike was converted to a simple Strike.");
                                        action.Traits.Remove(Trait.Flourish);
                                    }
                                }
                            }
                        });
                    
                    return engravingStrike;
                };
            })
            .WithInappropriateBecauseOfBadInventory(FeatInventoryRequirements.RequiresMeleeWeapon);
        
        // Glyph Familiar
        
        // Remote Detonation
        yield return new TrueFeat(
                ModData.FeatNames.RemoteDetonation, 1,
                "You whisper an invocation over your ammunition as you shoot it, and the hissing of the projectile sounds just like your murmured voice.",
                // Make a ranged Strike that uses physical ammunition
                $$"""
                Make a ranged Strike that uses ammunition against a target within the first range increment of your weapon. If it hits, you {{ModData.FeatNames.InvokeRune.ToLink("Invoke Runes")}} on the target as whisper of the ammunition's flight sets them off. You can invoke up to two runes on the target of your Strike in this way. On a critical hit, the target takes a –1 circumstance penalty on any saving throws against the runes invoked by your Remote Detonation.
                """,
                [Trait.Flourish, ModData.Traits.Invocation, ModData.Traits.Runesmith])
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
                        .WithExtraTrait(ModData.Traits.Invocation)
                        .WithExtraTrait(ModData.Traits.Runesmith)
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
                                            && invokeAction.HasTrait(ModData.Traits.Invocation)
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
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                FeatInventoryRequirements.RequiresOne(
                    inventory,
                    item => item.HasTrait(Trait.Ranged) && !item.HasTrait(Trait.Thrown),
                    "a ranged weapon that uses ammunition"));
        
        // Rune Ward
        
        // Rune-Singer
        yield return new TrueFeat(
                ModData.FeatNames.RuneSinger, 1,
                "You practice the lost art of using music to guide your rune-carving, singing the runes into existence as much as crafting them.",
                /*"You can use Performance instead of Crafting when attempting Crafting checks related to runes. " + */
                $"Once per combat, you can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with song alone, removing the manipulate trait from Trace Rune, and allowing you to use the {{icon:TwoActions}} 2-action version of Trace Rune as a single {{icon:Action}} action." /*+" You don't need to be able to move your hands when Tracing a Rune using song, but you do need to be able to sing in a clear voice."*/,
                [ModData.Traits.Runesmith])
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
                    else if (qfFeat.Owner.HasFeat(ModData.FeatNames.ProdigalRuneSinger))
                    {
                        qfFeat.Description = qfFeat.Description!
                            .Replace("Once per combat", "Once per round");
                    }
                    
                    qfFeat.Id = ModData.QEffectIds.RuneSingerCreator;
                    
                    qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
                    {
                        if (submenu.SubmenuId != ModData.SubmenuIds.TraceRune
                            || qfThis.Owner.PersistentUsedUpResources.UsedUpActions
                                .Contains(ModData.PersistentActions.RUNESINGER))
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
                                [ModData.ModTrait, ModData.Traits.Runesmith],
                                """
                                {i}You practice the lost art of using music to guide your rune-carving, singing the runes into existence as much as crafting them.{/i}

                                {b}Frequency{/b} Once per encounter.
                                
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
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Performance),
                "You must be trained in Performance.");
        
        // Seek the Hidden Glyphs
        
        // Smithing Weapons
        yield return new TrueFeat(
                ModData.FeatNames.SmithingWeapons, 1,
                "Though you are an artisan, you are well versed in using the tools of your trade to fend off enemies.",
                """
                While wielding a weapon in the hammer, knife, or pick weapon group, you can add its item bonus to attack rolls to your Crafting checks.

                Additionally, your Strikes with such weapons deal 1 additional fire damage to enemies bearing one of your runes, as sparks fly on impact like a hammer to an anvil.
                """,
                [ModData.Traits.Runesmith])
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
                        && action.Owner.HeldItems.Max(item => item.WeaponProperties?.ItemBonus ?? 0)
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
        
        // TODO: Phase 2, level 2 class feats.
        
        // Enhanced Glyph Familiar
        
        // Fortifying Knock
        yield return new TrueFeat(
                ModData.FeatNames.FortifyingKnock, 2,
                "Your shield is a natural canvas for your art.",
                $$"""
                  {b}Requirements{/b} You are wielding a shield.

                  In one motion, you Raise a Shield and {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} on your shield.
                  """,
                [Trait.Flourish, ModData.Traits.Runesmith])
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
                            .Where(rune => rune.DrawProperties.IsDrawnOnThisItem(Trait.Shield))
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
                            [ModData.ModTrait, Trait.Flourish, ModData.Traits.Runesmith],
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
            .WithInappropriateBecauseOfBadInventory(FeatInventoryRequirements.RequiresShield);
        
        // Invisible Ink
        yield return new TrueFeat(
                ModData.FeatNames.InvisibleInk, 2,
                "Your ink is as vanishing as your movements.",
                $$"""
                  Use the {icon:TwoActions} 2-action version of {{ModData.FeatNames.TraceRune.ToLink("Trace Rune")}}, then attempt to Hide or Sneak.

                  {b}Special{/b} Tracing a Rune doesn't cause you to cease being hidden.
                  """,
                [ModData.Traits.Runesmith, Trait.Rebalanced])
            .WithActionCost(2)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToOffenseBlock = _ =>
                    "{b}Invisible Ink {icon:TwoActions}{/b} Trace a Rune as 2 actions, then Hide or Sneak.";
                
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (action.HasTrait(ModData.Traits.Traced)
                        || action.HasTrait(ModData.Traits.Etched))
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
                                [ModData.ModTrait, ModData.Traits.Runesmith, Trait.DoesNotBreakStealth, Trait.Basic],
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
                                if (await CommonRuneRules.ChooseACreatureToDrawOn(self)
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
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                values.GetProficiency(Trait.Stealth)
                    is Proficiency.UntrainedWithLevel
                    or > Proficiency.Untrained
                    ? null
                    : "This feat works best if you can at least add your level to Stealth checks.");
        
        // Pattern Flight
        
        // Runic Tattoo
        yield return new TrueFeat(
                ModData.FeatNames.RunicTattoo, 2,
                "By drawing your favorite rune in your flesh, you know you'll never be without it.",
                $$"""
                  Choose one rune you know to tattoo onto your body. The rune is {{ModData.FeatNames.EtchRune.ToLink("etched")}} onto yourself{{ModData.Tooltips.InfoRunicTattooRestrictions}} at the beginning of each encounter. This doesn't count toward your maximum limit of etched runes. You can invoke this rune like any of your other runes, but once invoked, the rune fades significantly and is drained of power until your next daily preparations.

                  {b}Downtime{/b} You can magically alter your tattoo to become another rune you know (regardless of the level you learned this feat). {i}(If playing in Free Encounter Mode, you must select a rune you know at the level of the encounter or else this will fail to apply.){/i}
                  """,
                [ModData.Traits.Runesmith])
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
                                    // Cannot be a rune that draws on items
                                    !rune.DrawProperties.IsDrawnOnAnyItem
                                    // Cannot be a harmful passive effect
                                    && !rune.PassiveProperties.PassiveEffectIsDebuff
                                    // Cannot be a diacritic rune
                                    && !rune.IsDiacriticRune;
                            })
                            .Select(runeFeat =>
                            {
                                Rune rune = (runeFeat.Tag as Rune)!;
                                return new FeatlikeChoice(
                                    $"RunicTattoo.{rune.Id.ToWord()}",
                                    rune.Name)
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
                            .WithName($"Tattoo {rune.Name}")
                            .With(ca => ca.Traits.Remove(ModData.Traits.Etched))
                            .WithExtraTrait(ModData.Traits.Tattooed);
                        
                        DrawnRune? appliedRune = await CommonRuneRules.DrawRuneOnTarget(
                            etchTattoo,
                            qfThis.Owner,
                            rune,
                            doNotApplyImmediately: true);

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
        
        // TODO: Phase 2, level 4 class feats.
        
        // Artist's Attendance
        // DOC: "within reach of a creature" is interpreted as being YOUR reach
        yield return new TrueFeat(
                ModData.FeatNames.ArtistsAttendance, 4,
                "Your runes call you to better attend to your art.",
                $"Stride twice. If you end your movement within your reach of a creature that is bearing one of your runes{ModData.Tooltips.InfoArtistsAttendanceSelfBearer}, you can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} upon that creature or another target adjacent to you.",
                [Trait.Flourish, ModData.Traits.Runesmith])
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
                            [ModData.ModTrait, Trait.Flourish, ModData.Traits.Runesmith],
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
                                || await CommonRuneRules.ChooseACreatureToDrawOn(caster,
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
            });
        
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
                [ModData.Traits.Runesmith])
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
        
        // Terrifying Invocation
        yield return new TrueFeat(
                ModData.FeatNames.TerrifyingInvocation, 4,
                "You spit and roar as you pronounce your rune's terrible name.",
                $"You attempt to Demoralize a single target within 30 feet, and then {ModData.FeatNames.InvokeRune.ToLink("Invoke one Rune")} upon that target. You don't take a penalty to your check if the creature doesn't understand your language.",
                [ModData.Traits.Invocation, ModData.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(null, qfFeat =>
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
                        [ModData.ModTrait, ModData.Traits.Invocation, ModData.Traits.Runesmith],
                        $$"""
                          {i}You spit and roar as you pronounce your rune's terrible name.{/i}

                          You attempt to Demoralize a single target within {{rangeDesc}}, and then Invoke one Rune upon that target. You don't take a penalty to your check if the creature doesn't understand your language.
                          """,
                        Target.RangedCreature(range)
                            .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement())
                            .WithAdditionalConditionOnTargetCreature(new IsARuneBearer()))
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
                [Trait.Manipulate, ModData.Traits.Runesmith])
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
                        [ModData.ModTrait, Trait.Manipulate, ModData.Traits.Runesmith],
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
                                    && DrawnRune.GetDrawnRunes(caster, cr).Count != 0)
                                .ToList();
                            if (transposeAction.Tag is Creature target)
                                possiblePickups = possiblePickups
                                    .Where(cr => cr == target)
                                    .ToList();
                            DrawnRune? chosenRune = await CommonRuneRules.ChooseADrawnRune(
                                caster,
                                possiblePickups,
                                transposeAction.Illustration,
                                "Choose one of your runes to move to another creature within 30 feet or right-click to cancel.",
                                dr => $"Pick up {{Blue}}{dr.Rune.Name}{{/Blue}}",
                                null, "Don\'t choose a rune", true,
                                IsTransposableRune);
                            if (chosenRune != null)
                            {
                                List<Creature> possibleDropoffs = caster.Battle.AllCreatures
                                    .Where(cr =>
                                        chosenRune.Rune.DrawProperties.IsLegalTarget(caster, cr) == Usability.Usable)
                                    .ToList();
                                Creature? chosenCreature = await caster.Battle.AskToChooseACreature(
                                    caster,
                                    possibleDropoffs,
                                    transposeAction.Illustration,
                                    $"Choose a creature to bear {{Blue}}{chosenRune.Rune.Name}{{/Blue}}",
                                    $"Move {chosenRune.Rune.Illustration} {{Blue}}{chosenRune.Rune.Name}{{/Blue}} to this creature.",
                                    "Don\'t move rune");
                                if (chosenCreature != null)
                                {
                                    DrawnRune pretendNewRune = (await chosenRune.Rune.PassiveProperties.DrawnRuneCreator!.Invoke(transposeAction, chosenRune.Rune, chosenCreature, null))!;
                                    Sfxs.Play(ModData.SfxNames.TRANSPOSE_ETCHING_END);
                                    /*await*/ CommonRuneRules.MoveRuneToTarget(chosenRune, chosenCreature, pretendNewRune.DrawnOn);
                                }
                                else
                                    transposeAction.RevertRequested = true;
                            }
                            else
                                transposeAction.RevertRequested = true;
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
        
        #endregion
        
        #region 6th-Level
        
        // TODO: Phase 2, level 6 class feats.
        
        // Diacritic Fluency
        
        // Engraving Maneuver
        
        // Runic Reprisal
        yield return new TrueFeat(
                ModData.FeatNames.RunicReprisal, 6,
                "When you Raise your Shield, you can bury a runic trap into it, which is set off by the clash of an enemy weapon.",
                $"""
                 When you use {ModData.FeatNames.FortifyingKnock.ToLink("Fortifying Knock {icon:Action}")}, you can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with a damaging invocation on your shield, even if it normally couldn’t be applied to a shield. The traced rune doesn't have its normal effect, instead fading into your shield.

                 If you {FeatName.ShieldBlock.ToLink("Shield Block {icon:Reaction}")} with the shield against a melee Strike, you can {ModData.FeatNames.InvokeRune.ToLink("Invoke the Rune")} as part of the reaction, causing the rune to detonate outward and apply its invocation effect to the attacking creature.
                 """,
                [ModData.Traits.Invocation, ModData.Traits.Runesmith])
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
                                            "{Blue}(Runic Reprisal) When you use Shield Block against an adjacent attacker, this rune's invocation effects are detonated outward onto the attacker.{/Blue}");
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
                            [ModData.ModTrait, Trait.Flourish, ModData.Traits.Invocation, ModData.Traits.Runesmith],
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
                                "Runic Reprisal",
                                [ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.UnaffectedByConcealment, Trait.ProxyAttack],
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
                            .WithEffectOnEachTarget(async (_, caster, target, _) =>
                            {
                                target.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                                {
                                    AfterYouTakeAction = async (_, _) =>
                                    {
                                        CombatAction? invokeThisRune = CommonRuneRules.CreateInvokeAction(
                                                caster,
                                                reprisalDr,
                                                1,
                                                true,
                                                false)?
                                            .WithName($"Reprise ({reprisalDr.Name})");

                                        if (invokeThisRune == null)
                                            return;

                                        // Move them back, so the invoke animation looks good
                                        await target.FictitiousSingleTileMoveBack();
                                        
                                        await caster.Battle.GameLoop.FullCast(
                                            invokeThisRune,
                                            ChosenTargets.CreateSingleTarget(target));
                                    }
                                });
                            }))
                        .Select(repriseThisRune =>
                        {
                            DrawnRune reprisalDr = (repriseThisRune.Tag as DrawnRune)!;
                            return ReactionOption.WrapFullcastWithChosenTargets(
                                    repriseThisRune,
                                    ChosenTargets.CreateSingleTarget(dEvent.Source),
                                    $"Invoke {reprisalDr.Illustration!.IllustrationAsIconString} {reprisalDr.Rune.Name.WithTag("Blue")} from your shield against {dEvent.Source.ToColoredName()}.")
                                .WithDoesNotCountAsYourTriggerResponse()
                                .WithTriggerReason($"{qfThis.Owner.ToColoredBoldedName()} used {action.Name} against a melee Strike from an adjacent attacker.");
                        }));
                };
            })
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
                [ModData.Traits.Runesmith])
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
                                dr.DrawTrait == ModData.Traits.Traced
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
                                if (action.HasTrait(ModData.Traits.Traced))
                                    return true;
                                return false;
                            })
                            .With(qf =>
                            {
                                qf.PreventTakingAction = action =>
                                {
                                    // Code not shortened in case I need to expand the logic.
                                    if (action.HasTrait(ModData.Traits.Invocation))
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
                                CombatAction.CreateSimple(caster, "Dummy Trace Action", [ModData.Traits.Traced]);
                            dummyTraceAction.UsedQuickenedAction = true;
                            caster.Actions.RevertExpendingOfResources(1, dummyTraceAction);
                            
                            //caster.Actions.ResetToFull(); // <-- Has bug: Bypasses stunned and slowed (or at least acts as if taking a 2nd turn when generating actions)
                        }
                    };
                });
        
        // Vital Compound Invocation
        yield return new TrueFeat(
                ModData.FeatNames.VitalCompoundInvocation, 6,
                "You can invoke runes from traditions that manipulate vital energy to restore flesh.",
                $"You {ModData.FeatNames.InvokeRune.ToLink("Invoke two Runes")} — one must be a {ModData.Tooltips.RuleRuneTradition("divine rune")}, and one must be a {ModData.Tooltips.RuleRuneTradition("primal rune")}. In addition to the runes' normal effects, one creature that's within 30 feet of both invoked runes regains Hit Points equal to 5 + double your level.",
                [Trait.Healing, ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.Positive])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                (int invokeRange, string invokeDesc) = CommonRuneRules.GetInvocationRange(qfFeat.Owner, (30 / 5));
                int healing = 5 + (qfFeat.Owner.Level * 2);
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction vci = new CombatAction(
                        qfThis.Owner,
                        new BagOfIllustrationsIllustration(
                            IllustrationName.Heal,
                            IllustrationName.Bless,
                            ModData.Illustrations.InvokeRune),
                        "Vital Compound Invocation",
                        [ModData.ModTrait, Trait.Healing, ModData.Traits.Invocation, ModData.Traits.Runesmith, Trait.Positive],
                        $$"""
                        {i}You can invoke runes from traditions that manipulate vital energy to restore flesh.{/i}

                        You Invoke two Runes — one must be a divine rune, and one must be a primal rune. In addition to the runes' normal effects, one creature that's within {{/*invokeDesc*/ "range"}} of both invoked runes regains {{healing.WithColor("Blue")}} Hit Points.
                        """,
                        Target.RangedFriend(invokeRange)
                            .WithAdditionalConditionOnTargetCreature((a, d) =>
                            {
                                if (d.Damage == 0)
                                    return Usability.NotUsableOnThisCreature("healthy");
                                List<DrawnRune> allRunes = DrawnRune.GetAllDrawnRunes(a);
                                if (allRunes.Count == 0)
                                    return Usability.NotUsable("No runes");
                                List<DrawnRune> runesInRange = allRunes
                                    .Where(dr => dr.Owner.DistanceTo(d) <= invokeRange)
                                    .ToList();
                                if (runesInRange.Count == 0)
                                    return Usability.NotUsableOnThisCreature("No runes within range");
                                bool hasDivine = runesInRange.Any(dr => dr.Traditions.Contains(Trait.Divine));
                                bool hasPrimal = runesInRange.Any(dr => dr.Traditions.Contains(Trait.Primal));
                                if (!hasDivine && !hasPrimal)
                                    return Usability.NotUsableOnThisCreature("No divine or primal runes within range");
                                if (!hasDivine)
                                    return Usability.NotUsableOnThisCreature("No divine runes within range");
                                if (!hasPrimal)
                                    return Usability.NotUsableOnThisCreature("No primal runes within range");
                                return Usability.Usable;
                            }))
                        .WithActionCost(1)
                        .WithShortDescription($"Invoke a divine and primal rune, then heal an ally within {invokeDesc} of both.")
                        .WithEffectOnEachTarget(async (action, caster, target, _) =>
                        {
                            List<DrawnRune> runesInRange = DrawnRune.GetAllDrawnRunes(caster)
                                .Where(dr => dr.Owner.DistanceTo(target) <= invokeRange)
                                .ToList();
                            List<DrawnRune> divineRunes = runesInRange
                                .Where(dr => dr.IsDivine)
                                .ToList();
                            List<DrawnRune> primalRunes = runesInRange
                                .Where(dr => dr.IsPrimal)
                                .ToList();
                            List<DrawnRune> allRunes = divineRunes
                                .Concat(primalRunes)
                                .Distinct()
                                .ToList();
                                
                            // Revert if you can't begin the invocation.
                            if (allRunes.Count < 2
                                || divineRunes.Count == 0
                                || primalRunes.Count == 0)
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
                                    bool isDivine = firstRune.Value.DrawnRune.IsDivine;
                                    bool isPrimal = firstRune.Value.DrawnRune.IsPrimal;
                                    if (isDivine && !isPrimal)
                                        validRunes = primalRunes.ToList();
                                    else if (isPrimal && !isDivine)
                                        validRunes = divineRunes.ToList();
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
                                        "Choose a divine and a primal rune to invoke.",
                                        awaitableOptions)
                                    {
                                        TopBarText = $"Choose a divine and a primal rune to invoke, or right-click to cancel ({i+1}/2)",
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
                [Trait.Manipulate, ModData.Traits.Runesmith])
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
                            [ModData.ModTrait, Trait.Manipulate, ModData.Traits.Runesmith, ModData.Traits.Traced, Trait.Basic],
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
                        .WithEffectOnEachTarget( async (action, caster, target, _) =>
                        {
                            await CommonRuneRules.DrawRuneOnTarget(action, target, tattooedRune.Rune);
                        })
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            tattooedRune.ExpiresAt = ExpirationCondition.Immediately;
                            qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.RUNIC_TATTOO);
                        });
                    
                    return new ActionPossibility(flyFreeAction)
                        .WithPossibilityGroup(ModData.PossibilityGroups.DRAWING_RUNES);
                };
            })
            .WithPrerequisite(ModData.FeatNames.RunicTattoo, "Runic Tattoo");
        
        #endregion
        
        #region 8th-Level
        
        // TODO: Phase 2, level 8 class feats.
        
        // Drawn in Vital Ink
        yield return new TrueFeat(
                ModData.FeatNames.DrawnInVitalInk, 8,
                "After striking the target, you run a brush or finger along your striking implement to collect a bit of its blood.",
                $$"""
                  {b}Requirements{/b} During your last action, you succeeded at a melee Strike that dealt physical damage to a creature that can bleed.

                  For the encounter, you can {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} targeting the creature you drew blood from at a range of 60 feet (even if you’re Tracing a Rune as a single action). Using Drawn in Vital Ink against a different creature ends the effect for the previous creature.
                  """,
                [ModData.Traits.Runesmith])
            .WithActionCost(0)
            .WithPermanentQEffect(null, qfFeat =>
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
                            [ModData.ModTrait, ModData.Traits.Runesmith, Trait.Basic],
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
                                rune.Name == section.Name)
                            is not { } foundRune)
                        return null;

                    CombatAction bloodTrace = CommonRuneRules
                        .CreateTraceAction(qfThis.Owner, foundRune, 2, 12)
                        .WithIllustration(new SuperimposedIllustration(
                            foundRune.Illustration,
                            ModData.Illustrations.DrawnInVitalInk))
                        .WithActionCost(1)
                        .WithAdjustTarget<CreatureTarget>(crTar => crTar
                            // Communicate creature-targeting limitation with an error
                            .WithAdditionalConditionOnTargetCreature((_, _) =>
                                foundRune.DrawProperties.IsDrawnOnAnyItem
                                    ? Usability.NotUsable("Rune must target a creature")
                                    : Usability.Usable)
                            .WithAdditionalConditionOnTargetCreature((_, d) =>
                                d == bloodTarget
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("not Drawn In Vital Ink target")));

                    // The usage of the "Draw" verbiage is external.
                    // I otherwise use "draw" as the most generic verb for applying runes,
                    // but that's an internal coding decision unrelated to this feat.
                    bloodTrace.Name = bloodTrace.Name
                        .Replace("Trace", "Draw")
                        .Replace("Sing", "Draw & Sing");
                    bloodTrace.ContextMenuName = "{icon:Action} " + bloodTrace.Name;
                    bloodTrace.Description = CommonRuneRules.CreateTraceActionDescription(
                        bloodTrace,
                        foundRune,
                        prologueText:
                        "{Blue}{b}Range{/b} 60 feet{/Blue}\n"
                        + (qfThis.Owner.HasEffect(ModData.QEffectIds.RuneSinger)
                           && !qfThis.Owner.HasFeat(ModData.FeatNames.GenerationalRuneSinger)
                            ? $"{{Blue}}{{b}}Frequency{{/b}} Once per {(qfThis.Owner.HasFeat(ModData.FeatNames.ProdigalRuneSinger) ? "round" : "combat")} (Rune-Singer){{/Blue}}\n"
                            : null),
                        withFlavorText: false);

                    // Update the usage for a legal rune
                    if (!foundRune.DrawProperties.IsDrawnOnAnyItem)
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
            .WithInappropriateBecauseOfBadInventory((values, _) =>
                RunicRepertoireTag.GetRepertoire(values) is { } repertoire
                && repertoire.GetKnownRunes(values).Any(rune =>
                    !rune.DrawProperties.IsDrawnOnAnyItem)
                    ? null
                    : "This feat only works if you know a rune that's drawn on creatures.");
        
        // Edifying Trace
        
        // Elemental Revision
        // DOC: This permanently changes the rune.
        yield return new TrueFeat(
                ModData.FeatNames.ElementalRevision, 8,
                "You can scratch out and rewrite part of an elemental rune to temporarily change the type of power it channels.",
                // "an unattended item or one held by a willing creature"
                // "The revision lasts until the end of combat before the rune's original magic reasserts itself."
                $"You touch an adjacent {ItemName.CorrosiveRunestone.ToLink("{i}corrosive{/i}")}, {ItemName.FlamingRunestone.ToLink("{i}flaming{/i}")}, {ItemName.FrostRunestone.ToLink("{i}frost{/i}")}, {ItemName.ShockRunestone.ToLink("{i}shock{/i}")}, or {ItemName.ThunderingRunestone.ToLink("{i}thundering{/i}")} property rune on an item held by you or an ally, and you permanently change it to any other property rune from that list. You can also revise the greater version of any of the above runes into the other greater versions on the list.",
                [ModData.Traits.Runesmith])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction revisionAction = new CombatAction(
                            qfThis.Owner,
                            IllustrationName.ResistEnergy,
                            "Elemental Revision",
                            [ModData.ModTrait, ModData.Traits.Runesmith],
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
        
        #endregion
        
        #region 10th-Level
        
        // TODO: Phase 3, level 10 class feats.
        
        // Chain of Words
        
        // Clashing Compound Invocation
        
        // Overloaded Ammunition
        
        // Prodigal Rune-Singer
        yield return new TrueFeat(
                ModData.FeatNames.ProdigalRuneSinger, 10,
                "You have mastered the art of singing your runes.",
                $"You can {ModData.FeatNames.TraceRune.ToLink("Trace a Rune")} with song once per round instead of once per encounter.",
                [ModData.Traits.Runesmith])
            .WithPrerequisite(
                ModData.FeatNames.RuneSinger,
                "Rune-Singer")
            .WithPrerequisite(
                values => values.HasFeat(FeatName.ExpertPerformance),
                "You must be an expert in Performance.");
        
        // Runic Correspondence
        
        #endregion
        
        #region 12th-Level
        
        // TODO: Phase 3, level 12 class feats.
        
        // Astral Compound Invocation
        
        // Distant Invocation
        yield return new TrueFeat(
            ModData.FeatNames.DistantInvocation, 12,
            "Your connection to your runes stretches over even greater distances.",
            "Add 30 feet to the range of any of your invocation abilities (typically increasing the range from 30 to 60 feet).",
            [ModData.Traits.Runesmith]);

        // Expanded Glossary
        yield return new TrueFeat(
                ModData.FeatNames.ExpandedGlossary, 12,
                "You have memorized more runes than many in your craft.",
                $"Add two {ModData.Tooltips.TraitRune("runes")} of 9th level or lower to your runic repertoire.",
                [ModData.Traits.Runesmith])
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

        #endregion

        #region 14th-Level
        
        // TODO: Phase 3, level 14 class feats.

        // Dance of Bloody Ink

        // Define the Canvas

        // Henge Gate

        // Unerring Runic Attraction

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
            [ModData.Traits.Runesmith]);

        #endregion

        #region 20th-Level

        // Forge New Word

        // Generational Rune-Singer
        yield return new TrueFeat(
                ModData.FeatNames.GenerationalRuneSinger, 20,
                "You are a once-in-a-generation genius.",
                "You can Trace a Rune with song at will and at a range of 60 feet.",
                [ModData.Traits.Runesmith])
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

    public static CombatAction CreateFortifyingKnockAction(
        Creature runesmith,
        Rune rune,
        Item shield,
        Action<CombatAction>? modifyTraceAction,
        Action<DrawnRune>? doWhatIfDrawn)
    {
        // Make instances of each rune with description adjustments
        CombatAction knockThisRune = CommonRuneRules
            .CreateTraceAction(runesmith, rune, actionVersion: 0)
            .WithIllustration(new SideBySideIllustration(
                shield.Illustration,
                rune.Illustration))
            .WithName($"Knock {rune.Name}")
            .WithActionCost(1)
            .WithExtraTrait(Trait.Flourish)
            .WithNewTarget(Target.Self());
        
        knockThisRune.Description = CommonRuneRules
            .CreateTraceActionDescription(
                knockThisRune,
                rune,
                withFlavorText: false)
            .Replace(
                rune.DrawProperties.UsageText,
                "{Blue}drawn on your raised shield{/Blue}");

        bool hasManipulate = knockThisRune.Traits.Remove(Trait.Manipulate);
        
        // Replace effect behavior
        knockThisRune.EffectOnOneTarget = async (knockAction, caster, target, _) =>
        {
            // Trace the rune, but do not apply it right away
            Rune rune2 = (knockAction.Tag as Rune)!;
            if (await CommonRuneRules.DrawRuneOnTarget(
                    knockAction, target, rune2,
                    // You can apply illegal runes to your shield with Runic Reprisal
                    ignoreUsageRequirements: true,
                    // Apply it if the action is not disrupted
                    doNotApplyImmediately: true,
                    alternativeTarget: shield)
                is { } drawnRune)
            {
                // Raise a shield
                Fighter.RaiseShield(caster, shield, caster, false);
                
                // Now disrupt it
                if (hasManipulate)
                {
                    CombatAction phantomManipulate = CombatAction.CreateSimple(
                            knockAction.Owner, "Manipulate",
                            Trait.Manipulate, Trait.DoNotShowInCombatLog)
                        .WithActionCost(0);
                    await phantomManipulate.Fullcast(phantomManipulate.Owner);
                    if (phantomManipulate.Disrupted)
                    {
                        knockAction.Disrupted = true;
                        return;
                    }
                }

                target.AddQEffect(drawnRune);
                
                drawnRune.DrawnOn = shield;
                doWhatIfDrawn?.Invoke(drawnRune);
            }
            // If the rune failed to apply due to an error, do not raise the shield
            else
                knockAction.RevertRequested = true;
        };
        
        modifyTraceAction?.Invoke(knockThisRune);

        return knockThisRune;
    }
}