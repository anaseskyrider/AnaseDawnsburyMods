using System.ComponentModel.DataAnnotations;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CharacterBuilder.Selections;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
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
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;
using Dawnsbury.Mods.RunesmithClass.TargetingRequirements;
using Microsoft.Xna.Framework;
using SpiritDamage;

namespace Dawnsbury.Mods.RunesmithClass;

public static class AllRunes
{
    public static List<Rune> All { get; } = [];
    public static List<Feat> AllRuneFeats { get; } = [];

    public static void LoadRunes()
    {
        foreach (Feat runeFeat in CreateRuneFeats())
        {
            All.Add((runeFeat.Tag as Rune)!);
            AllRuneFeats.Add(runeFeat);
            ModManager.AddFeat(runeFeat);
        }
    }

    public static IEnumerable<Feat> CreateRuneFeats()
    {
        /* TODO: Consider altering the way runes apply Item effects based on these Item fields to look into:
         * WithPermanentQEffectWhenWorn
         * WithOnCreatureWhenWorn
         * StateCheckWhenWielded
         */
        
        // TODO: For full-release of Runesmith, refactor runes to produce ItemModification s using ModifyItem and UnmodifyItem.

        #region 1st-Level
        
        // Atryl, Rune of Fire
        yield return new Rune(
                RuneId.Atryl,
                "This rune is often placed on a stone in a hearth, its power ensuring the stone remains warm through the night.",
                new RuneDrawProperties(
                        "drawn on a creature", /*or object*/
                        drawnOnCreature: true)
                    .WithEnemyRequirement(),
                new RunePassiveProperties(
                        "The bearer's fire resistance, if any, is reduced by 5. Its immunities are unaffected.",
                        (thisRune, level) =>
                        {
                            const int baseValue = 5;
                            int bonusValue = (level - thisRune.BaseLevel) / 2; // Increase by 1 every 2 character levels
                            int totalValue = baseValue + bonusValue;
                            string heightenedVar = S.HeightenedVariable(totalValue, baseValue);
                            return $"The bearer's fire resistance, if any, is reduced by {heightenedVar}. Its immunities are unaffected.";
                        })
                    .WithIsDebuff()
                    .WithDrawnRuneCreator(async (sourceAction, rune, target, subTarget) =>
                    {
                        int resistReductionAmount = 5 + (sourceAction.Owner.Level - rune.BaseLevel) / 2;
                        DrawnRune atrylPassive = new DrawnRune(
                            sourceAction,
                            rune,
                            $"Your Fire resistance reduced by {resistReductionAmount}.")
                        {
                            Value = resistReductionAmount,
                            HideValue = true,
                            StateCheckLayer = 1,
                            StateCheck = qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;

                                Resistance? fireResist =
                                    qfThis.Owner.WeaknessAndResistance.Resistances.FirstOrDefault(res =>
                                        res.DamageKind == DamageKind.Fire);
                                
                                if (fireResist is not { Value: > 0 })
                                    return;
                                
                                fireResist.Value = Math.Max(0, fireResist.Value - qfThis.Value);
                            },
                        };
                        return atrylPassive;
                    }),
                new RuneInvocationProperties(
                        "The bearer takes 1d8 fire damage, with a basic Fortitude save. On a critical failure, it's also {r}dazzled{/r} for 1 round.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return $"The bearer takes {S.HeightenedVariable(finalValue, baseValue)}d8 fire damage, with a basic Fortitude save. On a critical failure, it's also dazzled for 1 round.";
                        })
                    .WithDealsDamage((rune, level) =>
                        ($"{rune.CalculateHeightening(1, 2, 1, level).FinalValue}d8", DamageKind.Fire))
                    .WithDefense(Defense.Fortitude)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            async (invokeAction2, effectTarget2, result) =>
                            {
                                if (result > CheckResult.CriticalFailure)
                                    return;
                                effectTarget.AddQEffect(QEffect.Dazzled()
                                    .WithExpirationOneRoundOrRestOfTheEncounter(invokeAction.Owner, false)
                                    .With(qf =>
                                    {
                                        qf.Source = invokedRune.Source;
                                        qf.SourceAction = invokeAction;
                                    }));
                            });

                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_ATRYL),
                [Trait.Fire, Trait.Primal])
            .WithLevelText("+2", "The reduction in fire resistance increases by 1, and the damage of the invocation increases by 1d8.")
            .ToFeat();
        
        // Baruiel, Rune of Hold's Bravery
        yield return new Rune(
                RuneId.Baruiel,
                "This scratched rune reminds the bearer to stand strong against fear when near allies.",
                new RuneDrawProperties(
                        "drawn on a willing creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement()
                    .WithCreatureRequirement((a, d) =>
                    {
                        if (d.IsImmuneTo(Trait.Emotion))
                            return Usability.NotUsableOnThisCreature("Immune to emotion");
                        if (d.IsImmuneTo(Trait.Mental))
                            return Usability.NotUsableOnThisCreature("Immune to mental");
                        return Usability.Usable;
                    }),
                new RunePassiveProperties(
                        "As long as the rune-bearer is within 10 feet of one of its allies, whenever it would reduce its {r}frightened{/r} condition by 1, it instead reduces it by 2",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            "While you're within 10 feet of one of your allies, reduce your frightened condition by 2 instead of 1 whenever you'd reduce its value.")
                        {
                            StateCheck = qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;
                                if (qfThis.Owner.Battle.AllCreatures
                                    .Any(cr =>
                                        cr.FriendOfAndNotSelf(qfThis.Owner)
                                        && cr.DistanceTo(qfThis.Owner) <= 2 ))
                                    qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                        { Id = QEffectId.CalmAndCentered });
                            },
                        };
                    }),
                new RuneInvocationProperties(
                        "The essence of the rune is released outward, easing the minds of others. Each ally of the rune-bearer within a 10-foot emanation reduces their {r}frightened{/r} condition by 2.",
                        null)
                    .WithAffectsArea()
                    .WithAdditionalRequirement((a, d) =>
                        d.Battle.AllCreatures.Any(cr =>
                            d.FriendOfAndNotSelf(cr)
                            && cr.DistanceTo(d) <= 2
                            && !cr.IsImmuneTo(Trait.Emotion)
                            && !cr.IsImmuneTo(Trait.Mental)
                            && cr.HasEffect(QEffectId.Frightened))
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("No cureable frightened allies within 10 feet"))
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            ((EmanationTarget)Target.AlliesOnlyEmanation(2))
                            .WithIncludeOnlyIf((tar, cr) =>
                                cr != tar.OwnerAction.Owner
                                && !cr.IsImmuneTo(Trait.Emotion)
                                && !cr.IsImmuneTo(Trait.Mental)
                                && cr.HasEffect(QEffectId.Frightened)),
                            false,
                            ModData.SfxNames.INVOKED_BARUIEL,
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (effectTarget.FindQEffect(QEffectId.Frightened) is not { } frightened
                            || effectTarget.IsImmuneTo(Trait.Emotion)
                            || effectTarget.IsImmuneTo(Trait.Mental))
                            return null;
                        
                        /*Fighter.ReduceFrightenedValueOfFrightened(
                                    target,
                                    target.FindQEffect(QEffectId.Frightened));
                                Fighter.ReduceFrightenedValueOfFrightened(
                                    target,
                                    target.FindQEffect(QEffectId.Frightened));*/
                        
                        // Do this manually, treating CalmAndCentered as
                        // extra -1 value instead of -(2x).
                        
                        int oldValue = frightened.Value;
                        if (effectTarget.HasEffect(QEffectId.DirgeOfDoomFrightenedSustainer))
                        {
                            frightened.Value = Math.Max(1, frightened.Value - 2);
                        }
                        else
                        {
                            frightened.Value -= 2;
                            if (effectTarget.HasEffect(QEffectId.CalmAndCentered))
                                frightened.Value -= 3;
                        }

                        effectTarget.Battle.Log($"{effectTarget.Name}'s frightened value has been reduced from {oldValue} to {Math.Max(0, frightened.Value)}.");

                        if (frightened.Value > 0)
                            return null;
                                
                        frightened.ExpiresAt = ExpirationCondition.Immediately;

                        return effectTarget;
                    }),
                [Trait.Emotion, Trait.Mental])
            .ToFeat();
        
        // Camonica, Rune of Perplexity
        yield return new Rune(
                RuneId.Camonica,
                "This rune of looping swirls is difficult to look straight at and stirs up a psychic cacophony in a foe's mind.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithEnemyRequirement()
                    .WithCreatureRequirement((a, d) =>
                        d.IsImmuneTo(Trait.Mental)
                        ? Usability.NotUsableOnThisCreature("Immune to mental")
                        : Usability.Usable),
                new RunePassiveProperties(
                        "The first time each round the rune-bearer takes an action with the concentrate trait, the turmoil increases, dealing it 2 mental damage.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(2, 2, 1, level);
                            return
                                $"The first time each round the rune-bearer takes an action with the concentrate trait, the turmoil increases, dealing it {S.HeightenedVariable(finalValue, baseValue)} mental damage.";
                        })
                    .WithIsDebuff()
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        (int _, _, int finalValue) = rune.CalculateHeightening(2, 2, 1, drawAction.Owner.Level);
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"The first time each round you take a concentrate action, you take {finalValue} mental damage.")
                        {
                            UsedThisTurn = false,
                            AfterYouTakeAction = async (qfThis, action) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;
                                if (qfThis.UsedThisTurn
                                    || !action.HasTrait(Trait.Concentrate))
                                    return;

                                qfThis.UsedThisTurn = true;

                                Sfxs.Play(ModData.SfxNames.PASSIVE_CAMONICA);
                                
                                await CommonSpellEffects.DealDirectDamage(
                                    CombatAction.CreateSimple(
                                            qfThis.Source!,
                                            rune.FullName,
                                            [..rune.Traits])
                                        .WithTag(qfThis),
                                    DiceFormula.FromText(
                                        finalValue.ToString(),
                                        rune.FullName),
                                    qfThis.Owner,
                                    CheckResult.Success,
                                    DamageKind.Mental);
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "The cacophony fragments and expands rapidly in a mental explosion, dealing 1d4 mental damage to the rune-bearer with a basic Will save. On a failure, the rune-bearer is {r}stupefied 1{/r} for 1 round (or {r}stupefied 2{/r} on a critical failure).",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return
                                $"The cacophony fragments and expands rapidly in a mental explosion, dealing {S.HeightenedVariable(finalValue, baseValue)}d4 mental damage to the rune-bearer with a basic Will save. On a failure, the rune-bearer is {{r}}stupefied 1{{/r}} for 1 round (or {{r}}stupefied 2{{/r}} on a critical failure).";
                        })
                    .WithDefense(Defense.Will)
                    .WithDealsDamage((rune, level) => (
                        $"{rune.CalculateHeightening(1, 2, 1, level).FinalValue}d4",
                        DamageKind.Mental))
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            async (invokeAction2, effectTarget2, result) =>
                            {
                                if (result > CheckResult.Failure)
                                    return;
                                effectTarget2.AddQEffect(QEffect
                                    .Stupefied(result == CheckResult.CriticalFailure ? 2 : 1)
                                    .WithExpirationInOneRound(invokeAction2.Owner)
                                    .With(qf =>
                                    {
                                        qf.Source = invokeAction2.Owner;
                                        qf.SourceAction = invokeAction2;
                                    }));
                            });

                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_CAMONICA),
                [Trait.Mental, Trait.Occult])
            .WithLevelText(
                "+2",
                "The damage from the mental turmoil increases by 1 and the damage of the invocation increases by 1d4.")
            .ToFeat();

        // Esvadir, Rune of Whetstones
        yield return new Rune(
                RuneId.Esvadir,
                "This cuspate rune, when placed on a blade, ensures it won't go dull.",
                new RuneDrawProperties(
                        "drawn on a weapon or unarmed attack that deals piercing or slashing damage",
                        drawnOnItem: true)
                    .WithAllyRequirement(hasEnemyUseCases: true)
                    .WithHoldsItemRequirement(
                        item =>
                            item.WeaponProperties is not null
                            && item.DetermineDamageKinds().Any(kind =>
                                kind is DamageKind.Piercing or DamageKind.Slashing),
                        "no piercing or slashing weapon or unarmed attack")
                    // If you are able to draw this on an enemy, and can therefore see
                    // this targeting requirement, then this adds a requirement that
                    // there be another enemy adjacent to them to avoid accidents.
                    // This requirement is ignored if you know En to be able to increase the emanation.
                    .WithCreatureRequirement((a, d) =>
                    {
                        if (d.EnemyOf(a)
                            && !PlayerProfile.Instance.IsBooleanOptionEnabled(
                                ModData.BooleanOptions.UnrestrictedInvocation))
                        {
                            if (d.Neighbours.Creatures.Any(cr => cr.EnemyOf(a))
                                || RunicRepertoireTag.GetRepertoire(a)?.IsKnown(RuneId.En, a.Level) == true)
                                return Usability.Usable;
                            else
                                return Usability.NotUsableOnThisCreature("No enemies in range of invocation");
                        }
                        else
                            return Usability.Usable;
                    }),
                new RunePassiveProperties(
                        "Strikes with the weapon or unarmed attack deal an extra 2 persistent bleed damage per weapon damage die.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return subTarget is Item itemTarget
                            ? [MakeEsvadirPassive(itemTarget)!]
                            : await CommonRuneRules.ChooseAnItemToDrawOn(
                                drawAction,
                                drawAction.Owner,
                                target,
                                item =>
                                    item.WeaponProperties is not null
                                    && item.DetermineDamageKinds().Any(kind =>
                                        kind is DamageKind.Piercing or DamageKind.Slashing),
                                MakeEsvadirPassive,
                                "Choose a weapon or unarmed attack whose Strikes will deal additional persistent bleed damage.",
                                rune);

                        DrawnRune? MakeEsvadirPassive(Item? targetItem)
                        {
                            if (targetItem is null)
                                return null;
                            
                            DrawnRune esvadirPassive = new DrawnRune(
                                    drawAction,
                                    rune,
                                    (drThis, item) => $"Strikes with {item.Illustration.IllustrationAsIconString} {item.Name.WithColor("Blue")} deal 2 persistent bleed damage per weapon damage die.",
                                    targetItem)
                                {
                                    YouDealDamageEvent = async (qfThis, dEvent) =>
                                    {
                                        DrawnRune drThis = (qfThis as DrawnRune)!;
                                        if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                            return;
                                        
                                        // Must deal damage using a strike with the item this is drawn on
                                        if (dEvent.CombatAction?.HasTrait(Trait.Strike) != true
                                            || dEvent.CombatAction.CheckResult < CheckResult.Success
                                            || drThis.DrawnOn is not Item drawnItem
                                            || dEvent.CombatAction.Item != drawnItem
                                            || drawnItem.WeaponProperties is null
                                            || dEvent.TargetCreature.WeaknessAndResistance.Immunities.Contains(DamageKind.Bleed))
                                            return;
                                        
                                        // Determine weapon damage dice count
                                        int bleedAmount = 2 * drawnItem.WeaponProperties.DamageDieCount;

                                        QEffect? bleed = (await CommonSpellEffects.DealAttackRollPersistentDamage(
                                                dEvent.CombatAction,
                                                dEvent.TargetCreature,
                                                dEvent.CombatAction.CheckResult,
                                                bleedAmount.ToString(),
                                                // Hard compatibility to turn this bleed into persistent good/evil damage in order to trigger weaknesses
                                                /*drThis.AttachedDiacritic?.Rune.Id is RuneId.Inth
                                                    ? GetBestDamageKind(target, [DamageKind.Bleed, DamageKind.Evil])
                                                    : drThis.AttachedDiacritic?.Rune.Id is RuneId.Sar
                                                        ? GetBestDamageKind(target, [DamageKind.Bleed, DamageKind.Good])
                                                        : DamageKind.Bleed*/
                                                DamageKind.Bleed))
                                            ?.With(qf =>
                                            {
                                                if (drThis.AttachedDiacritic?.DamageDealtByRunesGainTrait
                                                    is { } trait)
                                                    qf.WithTrait(trait);
                                                qf.Supereffect = drThis;
                                            });
                                        
                                    },
                                };

                            return esvadirPassive;
                        }
                    }),
                new RuneInvocationProperties(
                        "A blast of cutting energy is released outward from the rune, dealing 1d8 slashing damage to a creature adjacent to the rune-bearer, with a basic Reflex save.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return $"The essence of sharpness is released outwards from the rune, dealing {S.HeightenedVariable(finalValue, baseValue)}d8 slashing damage to a creature adjacent to the rune-bearer, with a basic Reflex save.";
                        })
                    .WithAdditionalRequirement((runesmith, runeBearer) =>
                    {
                        return PlayerProfile.Instance.IsBooleanOptionEnabled(
                                   ModData.BooleanOptions.UnrestrictedInvocation)
                               || runeBearer.Space.GetNeighbours()
                                   .Any(tile => tile.PrimaryOccupant?.EnemyOf(runesmith) == true)
                               || RunicRepertoireTag.GetRepertoire(runesmith)?.IsKnown(RuneId.En, runesmith.Level) == true
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("No enemy adjacent to the target");
                    })
                    .WithDealsDamage((rune, level) =>
                        ($"{rune.CalculateHeightening(1, 2, 1, level).FinalValue}d8", DamageKind.Slashing))
                    .WithDefense(Defense.Reflex)
                    .WithHideTooltip()
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.RangedCreature(1)
                                // Enemy of invoker, not action-taker
                                .WithAdditionalConditionOnTargetCreature((a, d) =>
                                    d.EnemyOf(invokeAction.Owner)
                                        ? Usability.Usable
                                        : Usability.CommonReasons.TargetIsNotEnemy),
                            false,
                            ModData.SfxNames.INVOKED_ESVADIR,
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget);

                        return effectTarget;
                    }))
            .WithLevelText(
                "+2",
                "The damage of the invocation increases by 1d8.")
            .ToFeat();

        // Holtrik, Rune of Dwarven Ramparts
        yield return new Rune(
                RuneId.Holtrik,
                "Similarity in the Dwarven words for “wall” and “shield” ensure that this angular rune, once used to shore up tunnels, can apply equally well in the heat of battle.",
                new RuneDrawProperties(
                        "drawn on a shield",
                        drawnOnItem: true)
                    .WithAllyRequirement()
                    .WithHoldsItemRequirement(
                        item => item.HasTrait(Trait.Shield),
                        "no shield"),
                new RunePassiveProperties(
                        "A creature that has the shield raised gains a +1 status bonus to AC.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return subTarget is Item itemTarget
                            ? [MakeHoltrikPassive(itemTarget)!]
                            : await CommonRuneRules.ChooseAnItemToDrawOn(
                                drawAction,
                                drawAction.Owner,
                                target,
                                item => item.HasTrait(Trait.Shield),
                                MakeHoltrikPassive,
                                "Choose a shield to give a +1 status bonus to the bearer while raised.",
                                rune);
                        
                        DrawnRune? MakeHoltrikPassive(Item? targetItem)
                        {
                            if (targetItem is null)
                                return null;
                            
                            DrawnRune drawnHoltrik = new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, item) => $"You gain a +1 status bonus to AC while your {item.Illustration.IllustrationAsIconString} {item.Name.WithColor("Blue")} is raised.",
                                targetItem)
                            {
                                BonusToDefenses = (qfThis,_,_) =>
                                {
                                    DrawnRune dr = (qfThis as DrawnRune)!;
                                    if (dr.Disabled || dr.DrawnOn is null)
                                        return null;

                                    // Must be raising a shield.
                                    if (!qfThis.Owner.QEffects.Any(qf =>
                                            qf.Id == QEffectId.RaisingAShield
                                            && qf.Tag == dr.DrawnOn))
                                        return null;

                                    return new Bonus(1, BonusType.Status, "Holtrik (raised shield)");
                                },
                            };

                            return drawnHoltrik;
                        }
                    }),
                new RuneInvocationProperties(
                        "You call the shield to its rightful place. You Raise the Shield bearing the rune, as if the rune-bearer had used Raise a Shield, and the shield's status bonus to AC lasts until the beginning of the creature's next turn.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        !PlayerProfile.Instance.IsBooleanOptionEnabled(
                            ModData.BooleanOptions.UnrestrictedInvocation)
                        || d.QEffects.Any(qf =>
                            qf.Id == QEffectId.RaisingAShield
                            && DrawnRune.GetDrawnRunes(a, d)
                                .Any(dr =>
                                    dr.Rune.Id == RuneId.Holtrik
                                    && dr.DrawnOn == qf.Tag))
                            ? Usability.NotUsableOnThisCreature("Shield already raised")
                            : Usability.Usable)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (invokedRune.DrawnOn is not Item shield)
                            return null;
                        
                        // Raise their Shield
                        Fighter.RaiseShield(
                            effectTarget,
                            shield,
                            effectTarget,
                            false);

                        // The rune-bearer retains the bonus
                        QEffect retainedBonus = invokedRune.NewInvocationEffect(
                            $"You gain a +1 status bonus to AC while your {shield.Illustration.IllustrationAsIconString} {shield.Name.WithColor("Blue")} is raised.",
                            ExpirationCondition.ExpiresAtStartOfYourTurn,
                            qf =>
                            {
                                qf.BonusToDefenses = (qfThis,_,_) =>
                                {
                                    // Must be raising a shield.
                                    if (!qfThis.Owner.QEffects.Any(qfRaise =>
                                            qfRaise.Id == QEffectId.RaisingAShield
                                            && qfRaise.Tag == shield))
                                        return null;
                                    return new Bonus(1, BonusType.Status, "Holtrik (raised shield)");
                                };
                            });
                        
                        effectTarget.AddQEffect(retainedBonus);

                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(SfxName.RaiseShield))
            .ToFeat();

        // Ledria, Rune of Appeal
        // Faction alignment is treated as "allies of the runesmith", regardless of the rune-bearer's faction.
        // DOC: Invocation affects allies, rather than all creatures, for simplicity.
        yield return new Rune(
                RuneId.Ledria,
                "No matter what skin, shell, or scale this rune is drawn on, it shines like gold.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(true),
                new RunePassiveProperties(
                        "The rune-bearer gains a +1 status bonus to Deception, Diplomacy, and Performance checks.",
                        (rune, level) =>
                        {
                            return
                                $"The rune-bearer gains a +{S.HeightenedVariable(level >= 17 ? 3 : level >= 9 ? 2 : 1, 1)} status bonus to Deception, Diplomacy, and Performance checks.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int bonus = drawAction.Owner.Level >= 17 ? 3 : drawAction.Owner.Level >= 9 ? 2 : 1;
                        return new DrawnRune(
                            drawAction,
                            rune,
                            "You gain a +1 status bonus to Deception, Diplomacy, and Performance checks.")
                        {
                            BonusToSkills = skill =>
                                skill is Skill.Deception or Skill.Diplomacy or Skill.Performance
                                    ? new Bonus(bonus, BonusType.Status, rune.FullName)
                                    : null
                        };
                    }),
                new RuneInvocationProperties(
                        $"The rune draws allies closer to the bearer. You and each ally{ModData.Tooltips.InfoLedriaAlly} within 30 feet of the rune-bearer can Step closer to the rune-bearer as a {{icon:FreeAction}} free action.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        a.Battle.AllCreatures.Any(cr =>
                            // Include YOUR allies (and you)
                            a.FriendOf(cr)
                            // Exclude the rune-bearer
                            && a != d
                            && d.DistanceTo(cr) <= 6)
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("None of your allies within 30 feet"))
                    .WithAffectsArea()
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 6f; // 30 feet

                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize)
                                .WithIncludeOnlyIf((_, cr) =>
                                    cr.FriendOf(invokedRune.Source!)),
                            false,
                            ModData.SfxNames.INVOKED_LEDRIA,
                            overrideTargets: overrideTargets,
                            finalAdjustments: subAction =>
                            {
                                if (overrideTargets?.ChosenCreatures.Count is < 2)
                                    return;
                                
                                // For Ledria to not be a PITA to use, you need to be able to control
                                // which ally moves in what order, so that's what this reaction option
                                // is for. This blends subtargeting and EffectOnOneTarget like logic.
                                
                                // To manage this, a function is employed in two places: (1) The inner
                                // invocation for normal conditions that affect an area of allies,
                                // allowing you to control the order of executed Step actions; and (2)
                                // the OnEachTarget area for complex invocations caused by diacritics
                                // when it otherwise affects just one target.
                                
                                // This is handled by manually adding an invocation immunity, and
                                // checking it redundantly later.
                                subAction.WithEffectOnChosenTargets(async (self, chosen) =>
                                {
                                    await CommonQuestions.OfferReactionsAsync(new ReactionRequestStyle(
                                        self.Battle,
                                        () => $"{invokeAction.Owner.ToColoredName()} has invoked {invokedRune}, allowing each ally within 30 feet of {invokedRune.Owner.ToColoredName()} to Step towards them as a {{icon:FreeAction}} free action.",
                                        true,
                                        invokeAction.Owner,
                                        () => chosen.ChosenCreatures
                                            .Select(ally =>
                                                ReactionOption.CreateCustom(
                                                        "Step",
                                                        $"Step towards {invokedRune.Owner.ToColoredName()}.",
                                                        invokedRune.Rune.Illustration,
                                                        ally,
                                                        async () => await LedriaStepTowardsRuneBearer(
                                                            invokedRune, ally))
                                                    .WithIsFreeAction())
                                            .ToList()
                                    ));
                                    chosen.ChosenCreatures.ForEach(ally =>
                                        ally.AddQEffect(CommonRuneRules.ImmunityToInvocation(invokedRune.Rune)));
                                });
                            });
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await LedriaStepTowardsRuneBearer(invokedRune, effectTarget);
                        return effectTarget;
                    }),
                [Trait.Occult])
            .WithLevelText("9th", "The bonus increases to +2.\n{b}Level (17th){/b} The bonus increases to +3.")
            .ToFeat();
        
        // Lyskel, Rune of Frost
        yield return new Rune(
                RuneId.Lyskel,
                "This rune is often placed on chests and boxes to keep their contents from spoiling, even in the hottest of climates.",
                new RuneDrawProperties(
                        "drawn on a creature" /*or object*/,
                        true)
                    .WithEnemyRequirement()
                    .WithAdjustments(drawProps => drawProps.AdditionalGoodness = (tar, self, defender) =>
                    {
                        float value = 0f;
                        // Incentivize Lyskel when the target will probably want to Stand.
                        if (defender.HasEffect(QEffectId.Prone))
                            // Sicken and Enfeeble is worth *1, Fear is worth *5.
                            value += self.AiLevelMinimum1;
                        return value;
                    }),
                new RunePassiveProperties(
                        "The rune-bearer takes a -5-foot circumstance penalty to its Speeds and becomes so cold that movement becomes dangerous; if the bearer takes a move action, it becomes {r}clumsy 1{/r} until the beginning of its next turn.",
                        null)
                    .WithIsDebuff()
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            "You have a -5-foot circumstance penalty to your Speeds. If you take a move action, you become {r}clumsy 1{/r} until the beginning of your next turn.")
                        {
                            CountsAsADebuff = true,
                            BonusToAllSpeeds = _ =>
                                new Bonus(-1, BonusType.Circumstance, rune.FullName),
                            //AfterYouMoveOneSquare = async (qfThis, action, style, before, after) =>
                            YouBeginAction = async (qfThis, action) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;
                                if (!action.HasTrait(Trait.Move)
                                    || (action.Target as TileTarget)?
                                    .CreatePathfindingDescriptionFunction?
                                    .Invoke(action.Owner).Style.ForcedMovement == true)
                                    return;
                                // Announce if it wasn't already present
                                if (!qfThis.Owner.HasEffect(QEffectId.Clumsy))
                                    qfThis.Owner.Battle.Log($"{{b}}Lyskel:{{/b}} {qfThis.Owner.Name} is {{r}}clumsy 1{{/r}} until the beginning of their next turn.");
                                // But still let the game automate whether it's appropriate
                                // to add this specific instance, even if this lasts longer.
                                qfThis.Owner.AddQEffect(QEffect.Clumsy(1)
                                    .WithExpirationAtStartOfOwnerTurn());
                            },
                        };
                    }),
                new RuneInvocationProperties(
                        "The rune-bearer takes 1d4 cold damage, with a basic Fortitude save. All squares on the ground in the bearer's space and adjacent squares are covered in snow, becoming {r}difficult terrain{/r} for 1 round.",
                        (rune, level) =>
                        {
                            (int baseValue, int _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return
                                $"The rune-bearer takes {S.HeightenedVariable(finalValue, baseValue)}d4 cold damage, with a basic Fortitude save. All squares on the ground in the bearer's space and adjacent squares are covered in snow, becoming {{r}}difficult terrain{{/r}} for 1 round.";
                        })
                    .WithDealsDamage((rune, level) => (
                        rune.CalculateHeightening(1, 2, 1, level).FinalValue + "d4",
                        DamageKind.Cold))
                    .WithDefense(Defense.Fortitude)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction, invokedRune, effectTarget,
                            async (invokeAction2, effectTarget2, result) =>
                            {
                                Zone.SpawnStaticAndApply(
                                    invokedRune.Source!,
                                    // Space plus adjacent squares
                                    effectTarget2.Space.Tiles
                                        .SelectMany(tile => tile.Neighbours.TilesPlusSelf)
                                        .Distinct()
                                        .ToList(),
                                    zone =>
                                    {
                                        zone.ControllerQEffect.WithExpirationInOneRound(invokeAction2.Owner);
                                        zone.TileEffectCreator = tile =>
                                            new TileQEffect(tile)
                                            {
                                                //Illustration = IllustrationName.NewSnow,
                                                Illustration = new Illustration[]
                                                {
                                                    IllustrationName.SnowTile1,
                                                    IllustrationName.SnowTile2,
                                                    IllustrationName.SnowTile3,
                                                    IllustrationName.SnowTile4
                                                }.GetRandomVisualOnly(),
                                                TransformsTileIntoDifficultTerrain = true
                                            };
                                    });
                            });

                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_LYSKEL),
                [Trait.Cold, Trait.Primal])
            .WithLevelText("+2", "The damage of the invocation increases by 1d4.")
            .ToFeat();
        
        // Marssyl, Rune of Impact
        yield return new Rune(
                RuneId.Marssyl,
                "This rune magnifies force many times over as it passes through the rune's concentric rings.",
                new RuneDrawProperties(
                        "drawn on a weapon or unarmed attack that deals bludgeoning damage",
                        drawnOnItem: true)
                    .WithAllyRequirement()
                    .WithHoldsItemRequirement(
                        item =>
                            item.WeaponProperties is not null
                            && item.DetermineDamageKinds()
                                .Contains(DamageKind.Bludgeoning),
                        "no bludgeoning weapon or unarmed attack"),
                new RunePassiveProperties(
                        "The weapon or unarmed attack deals 1 bludgeoning splash damage per weapon damage die. If the rune is on a melee weapon or melee unarmed attack, you and the rune-bearer are immune to this splash damage.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return subTarget is Item itemTarget
                            ? [MakeMarssylPassive(itemTarget)!]
                            : await CommonRuneRules.ChooseAnItemToDrawOn(
                                drawAction,
                                drawAction.Owner,
                                target,
                                item =>
                                    item.WeaponProperties is not null
                                    && item.DetermineDamageKinds()
                                        .Contains(DamageKind.Bludgeoning),
                                MakeMarssylPassive,
                                "Choose a weapon or unarmed attack whose Strikes will deal 1 bludgeoning splash damage per weapon damage die.",
                                rune);
                        
                        DrawnRune? MakeMarssylPassive(Item? targetItem)
                        {
                            if (targetItem is null)
                                return null;
                            
                            DrawnRune marssylPassive = new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, item) =>
                                    $"Strikes with {item.Illustration.IllustrationAsIconString} {item.Name.WithColor("Blue")} deal 1 bludgeoning splash damage per weapon damage die.{(item.HasTrait(Trait.Melee) ? $"\n\nMelee: {(drThis.Owner != drThis.Source ? $"Both you and {drawAction.Owner.ToColoredName()}" : "You")} are immune to this splash damage." : null)}",
                                targetItem)
                            {
                                // Splash to adjacent creatures
                                AdjustStrikeAction = (qfThis, strike) =>
                                {
                                    DrawnRune drThis = (qfThis as DrawnRune)!;
                                    if (drThis.Disabled
                                        || drThis.DrawnOn is not Item drawnItem
                                        || strike.Item is null
                                        || strike.Item != drawnItem
                                        || drawnItem.WeaponProperties is null
                                        || !drThis.IsFirstInstanceOf())
                                        return;

                                    // Fail to splash if not the first of multiple duplicate effects
                                    if (qfThis != qfThis.Owner.QEffects.First(qf =>
                                            qf is DrawnRune { Rune.Id: RuneId.Marssyl }))
                                        return;

                                    strike.WithEffectOnChosenTargets(async (action, caster, targets) =>
                                    {
                                        // Don't splash on a fumble
                                        if (action.CheckResult < CheckResult.Failure)
                                            return;

                                        // Determine weapon damage dice count
                                        DiceFormula splashAmount = DiceFormula.FromText(
                                            drawnItem.WeaponProperties!.DamageDieCount.ToString(),
                                            $"Splash damage ({rune.FullName})");

                                        // Get splash targets.
                                        // On a success, this is the adjacent creatures.
                                        // Otherwise, this is just the target.
                                        List<Creature> splashTargets = action.CheckResult > CheckResult.Failure
                                            ? targets.ChosenCreature!
                                                .Neighbours
                                                .Creatures // Does not include Self
                                                .ToList()
                                            : [targets.ChosenCreature!];

                                        // Remove rune-bearer and runesmith for a melee attack.
                                        // (Check action and not item, because thrown weapons are
                                        // ranged weapons while being thrown.)
                                        if (action.HasTrait(Trait.Melee))
                                            splashTargets.RemoveAll(cr =>
                                                cr == action.Owner
                                                || cr == drawAction.Owner);
                                        
                                        // Deal splash damage
                                        foreach (Creature cr in splashTargets)
                                        {
                                            await CommonSpellEffects.DealTrueDirectSplashDamage(
                                                action,
                                                splashAmount,
                                                cr,
                                                DamageKind.Bludgeoning);
                                        }
                                    });
                                },
                            };
                            // Apply outside of construction, to capture reference.
                            // Combine splash damage with initial damage.
                            marssylPassive.AddExtraKindedDamageOnStrike = (strike, strikeTarget) =>
                            {
                                if (marssylPassive.Disabled
                                    || marssylPassive.DrawnOn is not Item drawnItem
                                    || strike.Item is null
                                    || strike.Item != drawnItem
                                    || drawnItem.WeaponProperties is null
                                    || !marssylPassive.IsFirstInstanceOf())
                                    return null;

                                // Determine weapon damage dice count
                                DiceFormula splashAmount = DiceFormula.FromText(
                                    drawnItem.WeaponProperties.DamageDieCount.ToString(),
                                    $"Splash damage ({rune.FullName})");

                                return new KindedDamage(splashAmount, DamageKind.Bludgeoning)
                                {
                                    ApplyCriticalAndHalvingEffects = false,
                                    IsTrueSplashDamage = true
                                };
                            };

                            return marssylPassive;
                        }
                    }),
                new RuneInvocationProperties(
                        // "your class DC" phrasing remains because it's more clear that the effect scales
                        // with the Runesmith's DC, not the Striker's statistics.
                        "The rune vibrates as power concentrates within it. The next successful Strike made with the weapon or unarmed attack before the end of its wielder's next turn deals an additional die of damage, and the target must succeed at a Fortitude save against your class DC or be {r:Push}pushed{/r} 10 feet away from the attacker (20 feet on a critical failure).",
                        null)
                    .WithDefense(Defense.Fortitude)
                    .WithHideTooltip()
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (invokedRune.DrawnOn is not Item drawnOn)
                            return null;
                        
                        QEffect invokedMarssyl = invokedRune.NewInvocationEffect(
                            $"Your next successful Strike made with {drawnOn.Illustration.IllustrationAsIconString} {drawnOn.Name.WithColor("Blue")} before the end of your next turn deals an additional die of damage, and the target must succeed at a Fortitude save against {invokeAction.Owner.ToColoredName()}'s class DC or be pushed 10 feet in a straight line backwards (20 feet on a critical failure).",
                            ExpirationCondition.ExpiresAtEndOfYourTurn,
                            qfInvoked =>
                            {
                                qfInvoked.Tag = invokedRune.DrawnOn as Item;
                                // Store the invocation action to expose data to Ur-.
                                qfInvoked.SourceAction = invokeAction;
                                qfInvoked.IncreaseItemDamageDieCount = (qfThis, item) =>
                                {
                                    if (qfThis.Owner.QEffects.FirstOrDefault(qf =>
                                            qf.Name == qfThis.Name) != qfThis)
                                        return false;
                                    Item? tagItem = qfThis.Tag as Item;
                                    return tagItem != null
                                           && (item == tagItem
                                               || (item.HasTrait(Trait.Unarmed)
                                                   && tagItem.HasTrait(Trait.Unarmed)));
                                };
                                qfInvoked.AdjustStrikeAction = (qfThis, strike) =>
                                {
                                    if (strike.Item != drawnOn
                                        || qfThis.Owner.QEffects.FirstOrDefault(qf =>
                                            qf.Name == qfThis.Name) != qfThis)
                                        return;
                                    
                                    strike.WithEffectOnEachTarget(async (action, caster, target, result) =>
                                    {
                                        if (result < CheckResult.Success)
                                            return;
                                        
                                        action.Owner.RemoveAllQEffects(qfToRemove => qfToRemove == qfThis);
                                        
                                        CheckResult pushResult = await CommonSpellEffects.RollSavingThrowAsync(
                                            target,
                                            CombatAction.CreateSimple(action.Owner, $"Invoked {invokedRune.Rune.FullName}"),
                                            invokedRune.Rune.InvocationProperties.Defense!.Value,
                                            invokeAction.Owner.ClassDC(Trait.Runesmith));
                                        
                                        int tilePush = pushResult <= CheckResult.Failure
                                            ? pushResult == CheckResult.CriticalFailure ? 4 : 2
                                            : 0;
                                        
                                        Sfxs.Play(ModData.SfxNames.INVOKED_MARSSYL_SHOVE);
                                        
                                        await action.Owner.PushCreature(target, tilePush);
                                    });
                                };
                            });

                        effectTarget.AddQEffect(invokedMarssyl);

                        return effectTarget;
                    }))
            .ToFeat();

        // Oljinex, Rune of Cowards' Bane
        yield return new Rune(
                RuneId.Oljinex,
                "This rune resembles a broken arrow.",
                new RuneDrawProperties(
                        "drawn on a shield",
                        drawnOnItem: true)
                    .WithAllyRequirement(true)
                    .WithHoldsItemRequirement(
                        item => item.HasTrait(Trait.Shield),
                        "no shield"),
                new RunePassiveProperties(
                        "The shield bearing the rune gains a +2 status bonus to its Hardness against physical damage from ranged attacks.",
                        (rune, level) =>
                        {
                            const int baseValue = 2;
                            int bonusValue = (level - rune.BaseLevel) / 4;
                            
                            return
                                $"The shield bearing the rune gains a +{S.HeightenedVariable(baseValue+bonusValue, baseValue)} status bonus to its Hardness against physical damage from ranged attacks.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int bonusHardness = 2 + ((drawAction.Owner.Level - rune.BaseLevel) / 4);
                        
                        return subTarget is Item itemTarget
                            ? [MakeOljinexPassive(itemTarget)!]
                            : await CommonRuneRules.ChooseAnItemToDrawOn(
                                drawAction,
                                drawAction.Owner,
                                target,
                                item => item.HasTrait(Trait.Shield),
                                MakeOljinexPassive,
                                $"Choose a shield to gain a +{bonusHardness} status bonus to its Hardness against physical damage from ranged attacks.",
                                rune);
                        
                        DrawnRune? MakeOljinexPassive(Item? targetItem)
                        {
                            if (targetItem is null)
                                return null;
                            
                            DrawnRune drawnOljinex = new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, item) => $"Your {item.Illustration.IllustrationAsIconString} {item.Name.WithColor("Blue")} has a +{bonusHardness} status bonus to its Hardness against physical damage from ranged attacks.",
                                targetItem);
                            drawnOljinex.StateCheck += qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                
                                if (drThis.Disabled
                                    || drThis.DrawnOn is not Item drawnShield)
                                    return;
                                
                                qfThis.Owner.AddQEffect(CommonShieldRules.BonusToShieldHardness((_, dEvent, _, _, shield) =>
                                        shield == drawnShield
                                        && dEvent.CombatAction?.Item?.WeaponProperties is not null
                                        && dEvent.CombatAction.HasTrait(Trait.Ranged)
                                        && dEvent.CombatAction.Item
                                            .DetermineDamageKinds()
                                            .Any(dk => dk.IsPhysical())
                                            ? new Bonus(bonusHardness, BonusType.Status, rune.FullName)
                                            : null)
                                    .WithExpirationEphemeral());
                            };

                            return drawnOljinex;
                        }
                    }),
                new RuneInvocationProperties(
                        "(illusion, mental, visual) The rune creates an illusion in the minds of all creatures adjacent to the rune-bearer that lasts for 1 round. The illusion is of a large, impassable wall blocking all paths away from the rune-bearer. Creatures affected by this invocation who attempt to willingly move farther away from the rune-bearer must succeed on a Perception check against your class DC to disbelieve the illusion, or else the action is disrupted.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        d.Neighbours.Creatures.Any(cr =>
                            cr.EnemyOf(a)
                            && !(cr.IsImmuneTo(Trait.Illusion)
                                 || cr.IsImmuneTo(Trait.Mental)
                                 || cr.IsImmuneTo(Trait.Visual)))
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("No adjacent enemies that aren't immune to illusion, mental, or visual effects"))
                    .WithDefense(Defense.Perception)
                    .WithHideTooltip() // Is an AoE that does not affect the rune-bearer
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 1f; // 5 feet
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize)
                                .WithIncludeOnlyIf((_, cr) => IsValidTarget(cr))
                                // This is mildly inefficient, but necessary to limit bad attempts to invoke.
                                .WithAdditionalRequirementOnCaster(self =>
                                    invokedRune.Owner.Neighbours.Creatures.Any(IsValidTarget)
                                        ? Usability.Usable
                                        : Usability.NotUsable("No valid creatures in range")),
                            false,
                            ModData.SfxNames.INVOKED_OLJINEX,
                            finalAdjustments: innerInvoke =>
                            {
                                innerInvoke.WithEffectOnChosenTargets(async (invokeOljinex, caster, targets) =>
                                {
                                    invokedRune.Owner.AddQEffect(
                                        new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                        {
                                            SpawnsAura = qfThis =>
                                                new MagicCircleAuraAnimation(IllustrationName.BaneCircle, Color.Purple,
                                                        1.08f)
                                                    .WithMaximumOpacity(0.5f),
                                            StateCheck = qfThis =>
                                            {
                                                int furthest = qfThis.Owner.Battle.AllCreatures
                                                    .Where(cr => cr.HasEffect(qf =>
                                                        qf.Name == $"Invoked {invokedRune.Rune.FullName}"
                                                        && qf.Source == invokeAction.Owner))
                                                    .MaxOrZeroInt(cr => cr.DistanceTo(qfThis.Owner));

                                                if (furthest == 0)
                                                {
                                                    qfThis.AssociatedAura?.MoveTo(0);
                                                    qfThis.AssociatedAura?.MaximumOpacity = 0;
                                                    qfThis.AssociatedAura = null;
                                                }

                                                qfThis.AssociatedAura?.MoveTo(furthest * 1.08f);
                                            }
                                        });
                                });
                            },
                            overrideTargets: overrideTargets);
                        
                        bool IsValidTarget(Creature cr) =>
                            cr != invokedRune.Owner
                            && !CommonRuneRules.IsImmuneToThisInvocation(cr, invokedRune.Rune)
                            && !(cr.IsImmuneTo(Trait.Illusion)
                                 || cr.IsImmuneTo(Trait.Mental)
                                 || cr.IsImmuneTo(Trait.Visual));
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (effectTarget.IsImmuneTo(Trait.Illusion)
                            || effectTarget.IsImmuneTo(Trait.Mental)
                            || effectTarget.IsImmuneTo(Trait.Visual))
                            return null;
                        
                        QEffect invokedOljinex = invokedRune.NewInvocationEffect(
                            $"You must first succeed at a Perception check against {invokeAction.Owner.ToColoredBoldedName()+"'s"} class DC before you can willingly move further away from {invokedRune.Owner.ToColoredBoldedName()}. On a failure, the action is lost. On a success, you disbelieve the illusion.",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                            invokeQf =>
                            {
                                invokeQf.Source = invokeAction.Owner;
                                invokeQf.SourceAction = invokeAction;
                                invokeQf.CountsAsADebuff = true;
                                // TODO: Consider recoding with tedious StateCheckWithVisibleChanges and LongMovement inspections.
                                invokeQf.AfterYouMoveOneSquare = async (qfThis, action, style, before, after) =>
                                {
                                    if (action is null
                                        || action.Owner != qfThis.Owner
                                        || !action.HasTrait(Trait.Move)
                                        || style is null
                                        || style.ForcedMovement
                                        || after.DistanceTo(qfThis.Source!) <= before.DistanceTo(qfThis.Source!))
                                        return;
                                    
                                    CombatAction seekOljinex = DisbelieveOljinex(qfThis, action, before);

                                    await qfThis.Owner.Battle.GameLoop.FullCast(seekOljinex);
                                };
                                
                                return;

                                CombatAction DisbelieveOljinex(QEffect qfThis, CombatAction? moveAction, Tile? before)
                                {
                                    CombatAction seekOljinex = new CombatAction(
                                            qfThis.Owner,
                                            new SideBySideIllustration(
                                                invokedRune.Rune.Illustration,
                                                IllustrationName.Seek),
                                            "Disbelieve Oljinex",
                                            [Trait.Basic, Trait.IsNotHostile, Trait.DoesNotBreakStealth, Trait.UsesPerception],
                                            $"Attempt to disbelieve Oljinex' illusory walls with a Perception check against the runesmith's class DC.{S.FourDegreesOfSuccess(
                                                null,
                                                "You disbelieve the illusion, allowing you to move normally.",
                                                "You don't disbelieve the illusion, wasting any actions you spent attempting to move through the walls.",
                                                null)}",
                                            Target.Self())
                                        .WithActiveRollSpecification(new ActiveRollSpecification(
                                            TaggedChecks.Perception(),
                                            Checks.FlatDC(qfThis.Source!.ClassDC(Trait.Runesmith))))
                                        .WithActionId(ActionId.Seek)
                                        .WithActionCost(0);
                                    if (moveAction is not null && before is not null)
                                    {
                                        seekOljinex.WithEffectOnEachTarget(async (_, disbeliever, _, result) =>
                                        {
                                            if (result > CheckResult.Failure)
                                                disbeliever.RemoveAllQEffects(qf => qf == qfThis);
                                            else
                                            {
                                                moveAction.Disrupted = true;
                                                await disbeliever.SingleTileMove(before, null, null);
                                            }
                                        });
                                    }

                                    return seekOljinex;
                                }
                            });

                        effectTarget.AddQEffect(invokedOljinex);

                        return effectTarget;
                    }),
                additionalTraits: [Trait.Arcane])
            .WithLevelText("+4", "The status bonus to Hardness increases by 1.")
            .ToFeat();

        // Pluuna, Rune of Illumination
        yield return new Rune(
                RuneId.Pluuna,
                "While many runes emit a faint glow, illumination is the focus of this simple rune.",
                new RuneDrawProperties(
                    "drawn on a creature" /*or armor*/,
                    drawnOnCreature: true),
                new RunePassiveProperties(
                        "The rune sheds revealing light in a 20-foot emanation. Creatures in the emanation take a –1 item penalty to Stealth checks, and the rune-bearer can't be undetected.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        const float emanationSize = 4f; // 20 feet

                        DrawnRune pluunaPassive = new DrawnRune(
                            drawAction,
                            rune,
                            "You can't become undetected, and all creatures in a 20-foot emanation take a -1 item penalty to Stealth checks.")
                        {
                            SpawnsAura = qfThis =>
                                new MagicCircleAuraAnimation(
                                    IllustrationName.AngelicHaloCircle,
                                    Color.Gold, emanationSize),
                            StateCheck = qfThis =>
                            {
                                qfThis.Owner.DetectionStatus.Undetected = false;
                                qfThis.Owner.Battle.AllCreatures
                                    .Where(cr =>
                                        cr.DistanceTo(qfThis.Owner) <= emanationSize)
                                    .ForEach(cr =>
                                        cr.AddQEffect(new QEffect("Pluuna's light",
                                            "You have a -1 item penalty to Stealth checks.", ExpirationCondition.Ephemeral,
                                            qfThis.Owner, IllustrationName.Light)
                                        {
                                            Key = "PluunasLight",
                                            BonusToSkills = skill => skill == Skill.Stealth
                                                ? new Bonus(-1, BonusType.Item, rune.FullName)
                                                : null
                                        }));
                            },
                        };

                        return pluunaPassive;
                    }),
                new RuneInvocationProperties(
                        "Each creature in the emanation must succeed at a Fortitude save or be {r}dazzled{/r} for 1 round. The light fades, but leaves behind a dim glow which prevents the target from being undetected for 1 round.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        d.Battle.AllCreatures.Any(cr =>
                            cr.EnemyOf(a)
                            && cr.DistanceTo(d) <= 4)
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("No enemies within 20 feet"))
                    .WithDefense(Defense.Fortitude)
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 4f; // 20 feet

                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize),
                            true,
                            ModData.SfxNames.INVOKED_PLUUNA,
                            finalAdjustments: innerInvoke =>
                            {
                                innerInvoke.WithEffectOnChosenTargets(async (_,_,_) =>
                                {
                                    QEffect invokedPluuna = invokedRune.NewInvocationEffect(
                                        "This dim light prevents you from being undetected.",
                                        ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                        invokeQf =>
                                        {
                                            invokeQf.StateCheck = qfThis =>
                                                qfThis.Owner.DetectionStatus.Undetected = false;
                                        });

                                    invokedRune.Owner.AddQEffect(invokedPluuna);
                                });
                            },
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        /*if (effectTarget.IsImmuneTo(Trait.Light))
                            return null;*/
                        
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            async (_, invokeTarget, result) =>
                            {
                                if (result <= CheckResult.Failure)
                                    invokeTarget.AddQEffect(QEffect.Dazzled()
                                        .WithExpirationAtStartOfSourcesTurn(invokeAction.Owner, 1));
                            });

                        return effectTarget;
                    }),
                [Trait.Light, Trait.Rebalanced])
            .ToFeat();

        // Ranshu, Rune of Thunder
        yield return new Rune(
                RuneId.Ranshu,
                "This vertical rune is often carved on tall towers to draw lightning and shield the buildings below it.",
                new RuneDrawProperties(
                        "drawn on a creature", /*or object*/
                        drawnOnCreature: true)
                    .WithEnemyRequirement()
                    .WithAdjustments(drawProps => drawProps.AdditionalGoodness = (tar, self, defender) =>
                    {
                        int value = 0;
                        // Is more valuable if you know the target cannot move
                        if (defender.HasEffect(QEffectId.Immobilized))
                            value += drawProps.Self.CalculateHeightening(3, 2, 1, self.Level).FinalValue;
                        return value;
                    }),
                new RunePassiveProperties(
                        "If the rune-bearer doesn't take a move action at least once on its turn, a small bolt of static finds it, dealing 3 electricity damage.",
                        (rune, level) =>
                        {
                            const int baseDamage = 3;
                            int bonusDamage = (level - rune.BaseLevel) / 2;
                            string damage = $"+{S.HeightenedVariable((baseDamage + bonusDamage), baseDamage)}";
                            return $"If the rune-bearer doesn't take a move action at least once on its turn, a small bolt of static finds it, dealing {damage} electricity damage.";
                        })
                    .WithIsDebuff()
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int damage = 3 + ((drawAction.Owner.Level - rune.BaseLevel) / 2);
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"If you don't take a move action before the end of your turn, you take {damage} electricity damage.")
                        {
                            EndOfYourTurnDetrimentalEffect = async (qfThis, self) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled
                                    || drThis.Owner.Actions.ActionHistoryThisTurn.Any(action =>
                                        action.HasTrait(Trait.Move)))
                                    return;
                                
                                await CommonSpellEffects.DealDirectDamage(
                                    CombatAction.CreateSimple(
                                            qfThis.Source!,
                                            "Ranshu, Rune of Thunder",
                                            [..drThis.Traits])
                                        .WithTag(qfThis),
                                    DiceFormula.FromText(damage.ToString(), "Ranshu, Rune of Thunder"),
                                    self,
                                    CheckResult.Failure,
                                    DamageKind.Electricity);
                                
                                Sfxs.Play(ModData.SfxNames.PASSIVE_RANSHU);
                            },
                        };
                    }),
                new RuneInvocationProperties(
                        "The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes 1d8 electricity damage with a basic Fortitude save.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return $"The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes {S.HeightenedVariable(finalValue, baseValue)}d8 electricity damage with a basic Fortitude save.";
                        })
                    .WithDefense(Defense.Fortitude)
                    .WithDealsDamage((rune, level) =>
                        ($"{rune.CalculateHeightening(1, 2, 1, level).FinalValue}d8", DamageKind.Electricity))
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget);
                        
                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_RANSHU),
                [Trait.Electricity, Trait.Primal])
            .WithLevelText("+2", "The damage from the bolt of static increases by 1, and the damage of the invocation increases by 1d8.")
            .ToFeat();

        // Rehgog, Rune of Bestial Might
        yield return new Rune(
                RuneId.Rehgog,
                "The jagged edges of this rune evoke the claws and talons of mighty beasts.",
                new RuneDrawProperties(
                        "drawn on a willing creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(),
                new RunePassiveProperties(
                        "The target's nose becomes an animal-like snout, giving the rune-bearer an imprecise scent with a range of 30 feet.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            "As a morph effect, you gain a snout. You have an imprecise scent with a range of 30 feet {i}(creatures within that range can't be undetected){/i}.")
                        {
                            StateCheck = qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;
                                drThis.Owner.AddQEffect(QEffect.Tremorsense(6)
                                    .With(qf =>
                                    {
                                        qf.Innate = false;
                                        qf.Description = null;
                                        qf.ExpiresAt = ExpirationCondition.Ephemeral;
                                    }));
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "The rune-bearer's muscles bulge with the strength of a ferocious beast. The bearer can immediately Step as a {icon:FreeAction} free action. The next unarmed Strike the bearer attempts before the start of your next turn deals an additional die of damage.",
                        null)
                    .WithSoundBeforeInvocation(SfxName.Footsteps)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await effectTarget.StepAsync(
                            $"Choose where to Step as part of invoking {invokedRune}, or right-click to cancel.",
                            true, true);

                        effectTarget.AddQEffect(invokedRune.NewInvocationEffect(
                            $"The next unarmed Strike you attempt before the start of {invokedRune.Source!.ToColoredBoldedName()}'s next turn deals an additional die of damage.",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                            qf =>
                            {
                                qf.IncreaseItemDamageDieCount = (qfThis, item) =>
                                    item.HasTrait(Trait.Unarmed);
                                qf.AfterYouTakeAction = async (qfThis, action) =>
                                {
                                    if (action.HasTrait(Trait.Strike)
                                        && action.HasTrait(Trait.Unarmed))
                                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                };
                            }));
                        
                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_REHGOG),
                [Trait.Morph, Trait.Primal])
            .ToFeat();
        
        // Sertum, Rune of Preparedness
        yield return new Rune(
                RuneId.Sertum,
                "The wavy lines of this rune evoke fields of reeds, the branches of trees blowing in the wind, or other natural phenomena.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(),
                new RunePassiveProperties(
                        $"The rune-bearer gains a +1 status bonus to Survival checks. While in natural terrain, this bonus also applies to their Lore checks. If etched at the beginning of an encounter, the rune-bearer can use Survival to roll initiative, and the status bonus applies to this roll{ModData.Tooltips.InfoSertumReroll}.",
                        (rune, level) =>
                        {
                            return
                                $"The rune-bearer gains a +{S.HeightenedVariable(level >= 17 ? 3 : level >= 9 ? 2 : 1, 1)} status bonus to Survival checks. While in natural terrain, this bonus also applies to their Lore checks. The rune-bearer can use Survival to roll initiative, and the status bonus applies to this roll.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int bonus = drawAction.Owner.Level >= 17 ? 3 : drawAction.Owner.Level >= 9 ? 2 : 1;
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"You gain a +1 status bonus to Survival checks{(target.Battle.Map.FinalTerrain is TerrainKind.Civilization ? "." : ", as well as to Lore checks.")}")
                        {
                            OfferAlternateSkillForInitiative = qfThis => Skill.Survival,
                            BonusToSkills = skill =>
                            {
                                if (skill is Skill.Survival)
                                    return new Bonus(bonus, BonusType.Status, rune.FullName);
                                if (skill.ToStringOrTechnical().ToLower().Contains("lore")
                                    && target.Battle.Map.FinalTerrain
                                        is not TerrainKind.Civilization)
                                    return new Bonus(bonus, BonusType.Status, rune.FullName);
                                return null;
                            },
                            WhenYouAcquireThis = qfThis =>
                            {
                                if (qfThis.Owner.Battle.RoundNumber > 0)
                                    return;

                                // Redo initiative order
                                List<Creature> oldList = qfThis.Owner.Battle.InitiativeOrder.ToList();
                                qfThis.Owner.Battle.InitiativeOrder.Clear();
                                oldList.ForEach(cr =>
                                {
                                    if (cr.EntersInitiativeOrder)
                                        cr.EnterInitiativeOrderNow();
                                });
                            },
                        };
                    }),
                new RuneInvocationProperties(
                        "The rune reaches out to the surrounding terrain, shifting it to the rune-bearer's advantage. The rune-bearer can Step up to three times as a {icon:FreeAction} free action.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                    {
                        var step = CombatAction.CreateSimple(
                                d, "Step", [Trait.Move, Trait.DoesNotProvoke])
                            .WithActionCost(0)
                            .WithActionId(ActionId.Step);
                        bool canStep = d.QEffects.All(qf => qf.PreventTakingAction?.Invoke(step) == null);
                        return canStep
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Can't Step");
                    })
                    .WithSoundBeforeInvocation(SfxName.Footsteps)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        for (int i=0; i<3; i++)
                        {
                            await effectTarget.StepAsync(
                                $"Choose where to Step as part of invoking {invokedRune}{(i==0 ? ", or right-click to cancel" : null)}. ({i+1}/3)",
                                i==0, true);
                        }

                        return effectTarget;
                    }),
                [Trait.Primal])
            .WithLevelText("9th", "The bonus increases to +2.\n{b}Level (17th){/b} The bonus increases to +3.")
            .ToFeat();
        
        // Thullax, Rune of Corrosion
        yield return new Rune(
                RuneId.Thullax,
                "The swooping lines of this rune seem to melt into each other as if their edges were hazy and indistinct, though the meaning is clear to you.",
                new RuneDrawProperties(
                        "drawn on a creature", /*or object*/
                        drawnOnCreature: true)
                    .WithEnemyRequirement()
                    .WithAdjustments(drawProps => drawProps.AdditionalGoodness = (target, self, defender) =>
                    {
                        float value = 0;
                        int reduction = drawProps.Self.CalculateHeightening(1, 2, 1, self.Level).FinalValue;
                        foreach (Resistance res in defender.WeaknessAndResistance
                                     .Resistances
                                     .Where(res =>
                                         res.DamageKind.IsPhysical()
                                         || (res as SpecialResistance)?.Name.ToLower().Contains("physical") == true))
                        {
                            // Add the amount you reduce, up to that reduction amount
                            value += Math.Min(res.Value, reduction);
                        }
                        
                        return value;
                    }),
                new RunePassiveProperties(
                        "The rune-bearer's resistance to each physical damage type is reduced by 1 (if it has any such resistances). Its immunities are unaffected.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return
                                $"The rune-bearer's resistance to each physical damage type is reduced by {S.HeightenedVariable(finalValue, baseValue)} (if it has any such resistances). Its immunities are unaffected.";
                        })
                    .WithIsDebuff()
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        (_,_, int finalValue) = rune.CalculateHeightening(1, 2, 1, drawAction.Owner.Level);
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"Each of your resistances to a physical damage type is reduced by {finalValue}.")
                        {
                            StateCheckLayer = 1,
                            StateCheck = qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;
                                foreach (Resistance res in qfThis.Owner.WeaknessAndResistance
                                             .Resistances
                                             .Where(res =>
                                                 res.DamageKind.IsPhysical()
                                                 || (res as SpecialResistance)?.Name.ToLower().Contains("physical") == true))
                                {
                                    res.Value = Math.Max(0, res.Value - finalValue);
                                }
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "The bearer takes 1d6 acid damage with a basic Fortitude save; on a critical failure, it takes 1 persistent acid damage.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(1, 2, 1, level);
                            return
                                $"The bearer takes {S.HeightenedVariable(finalValue, baseValue)}d6 acid damage with a basic Fortitude save; on a critical failure, it takes {S.HeightenedVariable(finalValue, baseValue)} persistent acid damage.";
                        })
                    .WithDealsDamage((rune, level) => (
                        $"{rune.CalculateHeightening(1, 2, 1, level).FinalValue}d6",
                        DamageKind.Acid))
                    .WithDefense(Defense.Fortitude)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            async (invokeAction2, effectTarget2, result) =>
                            {
                                if (result > CheckResult.CriticalFailure)
                                    return;
                                QEffect? acid = (await CommonSpellEffects.DealAttackRollPersistentDamage(
                                        invokeAction2,
                                        effectTarget2,
                                        // Not an attack, but my overload adds source info.
                                        CheckResult.Success,
                                        invokedRune.Rune.CalculateHeightening(1, 2, 1, invokedRune.Source!.Level).FinalValue
                                            .ToString(),
                                        // Hard compatibility to turn this acid into persistent good/evil damage in order to trigger weaknesses
                                        /*invokedRune.AttachedDiacritic?.Rune.Id is RuneId.Inth
                                            ? GetBestDamageKind(effectTarget2, [DamageKind.Acid, DamageKind.Evil])
                                            : invokedRune.AttachedDiacritic?.Rune.Id is RuneId.Sar
                                                ? GetBestDamageKind(effectTarget2, [DamageKind.Acid, DamageKind.Good])
                                                : DamageKind.Acid*/
                                        DamageKind.Acid))
                                    ?.With(qf =>
                                    {
                                        if (invokedRune.AttachedDiacritic?.DamageDealtByRunesGainTrait
                                            is { } trait)
                                            qf.WithTrait(trait);
                                        qf.Supereffect = invokedRune;
                                    });
                            });
                        
                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_THULLAX),
                [Trait.Acid, Trait.Arcane])
            .WithLevelText("+2", "The reduction in physical resistance increases by 1, the damage of the invocation increases by 1d6, and the persistent damage increases by 1.")
            .ToFeat();
        
        // Tilus, Rune of Vocabulary
        // DOC: Substantially more powerful due to niche use-cases. The invocation protects against any outgoing fizzles to Auditory actions, and slaps on DoesNotProvoke universally to Auditory actions as well.
        yield return new Rune(
                RuneId.Tilus,
                "Upon close inspection, this rune comprises hundreds of smaller characters of various runic languages.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(),
                new RunePassiveProperties(
                        "The raw magic of language flows through the rune-bearer, negating any penalties to their actions, as well as any bonuses to saving throws they impose, due to not sharing a language with a target. In addition, they gain a +1 status bonus to Society checks. While not in natural terrain, this bonus also applies to their Lore checks.",
                        (rune, level) =>
                        {
                            return
                                $"The raw magic of language flows through the rune-bearer, negating penalties to their actions and any saving throws they impose due to not sharing a language with their targets. In addition, they gain a +{S.HeightenedVariable(level >= 17 ? 3 : level >= 9 ? 2 : 1, 1)} status bonus to Society checks. While not in natural terrain, this bonus also applies to their Lore checks.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int bonusAmount = drawAction.Owner.Level >= 17 ? 3 : drawAction.Owner.Level >= 9 ? 2 : 1;
                        return (new DrawnRune(
                                drawAction,
                                rune,
                                $"You ignore penalties caused by not sharing a language. You also gain a +1 status bonus to Society checks{(target.Battle.Map.FinalTerrain is TerrainKind.Civilization ? ", as well as to Lore checks." : ".")}")
                            {
                                BonusToSkills = skill =>
                                {
                                    if (skill is Skill.Society)
                                        return new Bonus(bonusAmount, BonusType.Status, rune.FullName);
                                    if (skill.ToStringOrTechnical().ToLower().Contains("lore")
                                        && target.Battle.Map.FinalTerrain
                                            is TerrainKind.Civilization)
                                        return new Bonus(bonusAmount, BonusType.Status, rune.FullName);
                                    return null;
                                },
                                StateCheck = qfThis =>
                                {
                                    qfThis.Owner
                                        .AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                        {
                                            Id = QEffectId.IntimidatingGlare
                                        })
                                        .AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                        {
                                            Id = QEffectId.SpeakWithAnimals
                                        });
                                },
                                BeforeBonusesAreFlattened = (qfThis, list, reason, action, defender) =>
                                {
                                    if (reason is not BonusCalculationReason.SkillCheck)
                                        return;
                                    list.RemoveAll(bonus =>
                                        bonus is not null
                                        && bonus.BonusType is BonusType.Circumstance
                                        && bonus.Amount < 0
                                        && bonus.BonusSource.ToLower() == "no shared language");
                                },
                            }
                            .AddGrantingOfTechnical(
                                (qfThis, cr) => cr.EnemyOf(qfThis.Owner),
                                (qfThis, qfTech) =>
                                {
                                    qfTech.BeforeBonusesAreFlattened = (qfTech2, list, reason, action, defender) =>
                                    {
                                        if (reason is not BonusCalculationReason.Defense
                                            || action?.Owner != qfThis.Owner)
                                            return;
                                        list.RemoveAll(bonus =>
                                            bonus is not null
                                            && bonus.BonusType is BonusType.Circumstance
                                            && bonus.Amount > 0
                                            && bonus.BonusSource.ToLower() == "no shared language");
                                    };
                                })
                            as DrawnRune)!;
                    }),
                new RuneInvocationProperties(
                        "The rune-bearer's voice rings clear and true, allowing them to automatically succeed when an effect would attempt to fizzle or disrupt an auditory action they're taking for 1 round.",
                        null)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        QEffect invokedTilus = invokedRune.NewInvocationEffect(
                            "You automatically succeed when an effect would attempt to fizzle or disrupt an auditory action you take, and such actions do not provoke reactions.",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                            qfInvoked =>
                            {
                                qfInvoked.SourceAction = invokeAction;
                                qfInvoked.Source = invokedRune.Source;
                                qfInvoked.Tag = new HashSet<QEffect>();
                                qfInvoked.ModifyActionPossibility = (qfThis, action) =>
                                {
                                    if (!action.HasTrait(Trait.Auditory))
                                        return;
                                    action.WithExtraTrait(Trait.DoesNotProvoke);
                                };
                                qfInvoked.StateCheckLayer = 1;
                                qfInvoked.StateCheck = qfThis =>
                                {
                                    if (qfThis.Tag is not HashSet<QEffect> modifiedEffects)
                                        return;
                                    
                                    // Stop tracking effects you no longer own
                                    modifiedEffects.RemoveWhere(qf =>
                                        !qfThis.Owner.QEffects.Contains(qf));
                                    
                                    // For each effect you have that has outgoing fizzles,
                                    // modify it so that checks against auditory occur first.
                                    foreach (QEffect qf in qfThis.Owner.QEffects
                                                 .Where(qf => !modifiedEffects.Contains(qf))
                                                 .ToList())
                                    {
                                        if (qf.FizzleOutgoingActions is not { } cached)
                                            continue;
                                        qf.FizzleOutgoingActions = async (qfFizz, action, strBuild) =>
                                        {
                                            if (action.HasTrait(Trait.Auditory))
                                            {
                                                if (await cached(qfFizz, action, strBuild))
                                                    qfFizz.Owner.Battle.Log(
                                                        $"{qfFizz.Owner} automatically succeeded against an effect that attempted to fizzle their auditory action.",
                                                        invokedRune.Rune.FullName,
                                                        CommonRuneRules.StatBlocks.CreateRuneDescription(
                                                            invokedRune.Rune, invokedRune.Source!.Level),
                                                        new Traits(invokedRune.Rune.Traits.ToList()));
                                                return false;
                                            }
                                            return await cached(qfFizz, action, strBuild);
                                        };
                                        modifiedEffects.Add(qf);
                                    }
                                    
                                };
                            })
                            .WithExpirationInOneRound(invokeAction.Owner);

                        effectTarget.AddQEffect(invokedTilus);

                        return effectTarget;
                    }),
                [Trait.Arcane, Trait.Mental, Trait.Rebalanced])
            .WithLevelText("9th", "The bonus increases to +2.\n{b}Level (17th){/b} The bonus increases to +3.")
            .ToFeat();

        // Zohk, Rune of Homecoming
        yield return new Rune(
                RuneId.Zohk,
                "This circular mark allows travelers to always find their way home.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithCreatureRequirement((a, _) =>
                        a.Neighbours.Tiles.Any(tile => tile.IsFree)
                            ? Usability.Usable
                            : Usability.NotUsable("No open adjacent spaces")),
                new RunePassiveProperties(
                        "The rune-bearer can Stride with a +15-foot status bonus to its Speeds, but only to move closer to you than where they started their turn.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"You can Stride with a +15-foot status bonus to your Speeds, but only to move closer to {drawAction.Owner.ToColoredBoldedName()} than where you started your turn.")
                        {
                            // Record distance to runesmith at start of each turn
                            Tag = target.DistanceTo(drawAction.Owner),
                            StartOfYourEveryTurn = async (qfThis, self) =>
                                qfThis.Tag = self.DistanceTo(drawAction.Owner),
                            // Generate special Stride action
                            ProvideContextualAction = qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || drThis.Tag is not int distance)
                                    return null;

                                CombatAction zohkStride = new CombatAction(
                                        qfThis.Owner,
                                        new SideBySideIllustration(
                                            IllustrationName.FleetStep,
                                            rune.Illustration),
                                        "Stride (Zohk)",
                                        [Trait.Move],
                                        qfThis.Description!,
                                        Target.Self()
                                        // Behavior is somewhat unreliable. Removed since the world doesn't end if it immediately reverts.
                                        /*.WithAdditionalRestriction(self =>
                                        {
                                            // Code repeated from StrideCloserToEnemyAsync.
                                            // Go there for slightly better documentation and less consolidated code.
                                            List<Tile> casterTile = [caster.Occupies];
                                            IList<Tile> floodFill = Pathfinding.Floodfill(self, self.Battle, new PathfindingDescription()
                                                {
                                                    Squares = self.Speed,
                                                    Style = { PermitsStep = false }
                                                });
                                            bool hasAtLeastOneOption = false;
                                            foreach (Tile tile in floodFill)
                                            {
                                                if (!tile.IsFree || tile == self.Occupies || RunesmithPlaytest.IsTileCloserToAnyOfTheseTiles(self.Occupies, tile, casterTile))
                                                    continue;

                                                CombatAction? moveAction = self.Possibilities.CreateActions(true)
                                                    .FirstOrDefault(pw => pw.Action.ActionId == ActionId.Stride) as CombatAction;
                                                moveAction?.WithActionCost(0);

                                                if (moveAction == null || !(bool)moveAction.Target.CanBeginToUse(self))
                                                    continue;

                                                hasAtLeastOneOption = true;
                                            }

                                            return hasAtLeastOneOption ? null : "No legal squares to Stride to";
                                        })*/)
                                    .WithActionCost(1)
                                    .WithEffectOnSelf(async (action, self) =>
                                    {
                                        QEffect speedBoost = new QEffect()
                                        {
                                            ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                                            BonusToAllSpeeds = _ =>
                                                new Bonus(3, BonusType.Status, drThis.Name!),
                                        };
                                        self.AddQEffect(speedBoost);
                                        
                                        if (!await self.StrideOrStepAdvancedAsync(
                                                $"Stride closer to {drThis.Source!.ToColoredBoldedName()} or right-click to cancel.",
                                                allowCancel: true,
                                                permissibleTarget: tile =>
                                                    tile.DistanceTo(drThis.Source!) < distance))
                                            action.RevertRequested = true;

                                        self.RemoveAllQEffects(qf => qf == speedBoost);
                                    });

                                return new ActionPossibility(zohkStride);
                            },
                        };
                    }),
                new RuneInvocationProperties(
                        "(teleportation) You call the rune-bearer to your side. You teleport the target to any unoccupied square adjacent to you. If the bearer is unwilling, it must succeed at a Will save to negate the effect.",
                        null)
                    .WithDefense(Defense.Will)
                    .WithHideTooltipForAllies()
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        Tile? chosenTile = await invokeAction.Owner.Battle.AskToChooseATile(
                            invokeAction.Owner,
                            invokeAction.Owner.Battle.Map.AllTiles
                                .Where(tile =>
                                    tile.IsFree
                                    && tile.IsAdjacentTo(invokeAction.Owner))
                                .ToList(),
                            invokedRune.Rune.Illustration,
                            $"Choose a tile to teleport {effectTarget.ToColoredBoldedName()} to.",
                            $"Teleport {effectTarget.ToColoredBoldedName()} to here.",
                            true,
                            false,
                            effectTarget);

                        if (chosenTile is null)
                            return null;
                        
                        CheckResult result = CheckResult.Failure;
                        if (effectTarget.EnemyOf(invokeAction.Owner))
                        {
                            result = await CommonSpellEffects.RollSavingThrowAsync(
                                effectTarget,
                                CombatAction.CreateSimple(invokeAction.Owner, invokeAction.Name, [..invokeAction.Traits, Trait.Teleportation]),
                                invokedRune.Rune.InvocationProperties.Defense!.Value,
                                invokeAction.Owner.ClassDC(Trait.Runesmith));
                        }

                        if (result > CheckResult.Failure)
                            return effectTarget;
                        
                        await CommonSpellEffects.Teleport(effectTarget, chosenTile);

                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_ZOHK),
                [Trait.Arcane])
            .ToFeat();

        #endregion

        #region 5th-Level

        // Av-, Diacritic Rune of Succession
        yield return new Rune(
                RuneId.Av,
                "This diacritic surrounds the base rune with similar-looking smaller runes to give the impression of distant echoes.",
                RuneDrawProperties.Diacritic(null, true),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, you can Invoke a single Rune on a different rune-bearer within 15 feet of the original rune-bearer as a {icon:FreeAction} free action.",
                        true)
                    .WithDiacriticCreator(
                        // Has no particular requirements not covered elsewhere
                        null,
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                    drawAction,
                                    rune,
                                    (drThis, drOnto) =>
                                        $"After {drOnto} is invoked, {drThis.Source?.ToColoredBoldedName() ?? "the runesmith"} can Invoke a single Rune {{icon:FreeAction}} on a different rune-bearer within 15 feet of this one.\n\nCan only be invoked once per encounter.\n",
                                    drBase)
                                {
                                    AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                    {
                                        if (drThis.Disabled
                                            || drInvoked != drThis.DrawnOn)
                                            return;

                                        // For generosity-sake, not following through on the invocation
                                        // doesn't waste your usage of the diacritic for this encounter.
                                        if (!await CommonRuneRules.ChooseARuneToInvoke(
                                                drThis.Source!, // Whoever applied the diacritic gets the invoke.
                                                targetFilter: cr => cr != drInvoked.Owner, // Different rune-bearer
                                                overrideRange: 3, // 15 feet
                                                passText: " Confirm no additional invocation ",
                                                additionalTopText:
                                                $" This rune-bearer must be a different bearer within 15 feet of {drInvoked.Owner.ToColoredName()}."
                                            ))
                                        {
                                            return;
                                        }
                                        
                                        ModData.PersistentActions.UseUpRune(drThis.Source!, drThis.Rune);
                                    },
                                },
                        (_,_) => "Choose a rune that, when invoked, causes you to Invoke a single Rune {icon:FreeAction} on a different rune-bearer within 15 feet of the original rune."),
                null,
                [Trait.Diacritic])
            .WithIllustration(new CornerIllustration(
                IllustrationName.RunestoneWinged,
                ModData.Illustrations.InvokeRune,
                Direction.Southwest))
            .ToFeat();
        
        // En-, Diacritic Rune of Expansion
        yield return new Rune(
                RuneId.En,
                "This diacritic surrounds a rune with outward-facing arrows to magnify and direct power outward.",
                RuneDrawProperties.Diacritic("that deals damage")
                    .WithBaseRuneMustDealDamage()
                    .WithBaseRuneMustNotBeArea(),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, the rune-bearer is affected by it as usual, and each other creature in a 15-foot emanation around the rune-bearer also takes the damage and other effects from the base rune (and can attempt a saving throw if possible). An individual creature can be affected by the rune only once, even if the rune could normally affect more creatures than just the rune-bearer.")
                    .WithDiacriticCreator(
                        (drawAction, drOnto) =>
                            drOnto.Rune.InvocationProperties is
                                { DealsDamage: true, AffectsArea: false },
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"""
                                     When {drOnto} is invoked, each other creature in a 15-foot emanation around {drOnto.Owner.ToColoredBoldedName()} also takes the base rune's damage and other effects (and can attempt a saving throw if possible).

                                     This doesn't allow a creature to be affected by the rune more than once, even if it would affect more.
                                     
                                     """,
                                drBase)
                            {
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn
                                        || drInvoked.Rune.InvocationProperties.EffectOnOneTarget
                                            is not {} invocationOnEachTarget)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    if (await CommonRuneRules.GetInnerInvokeTargets(
                                            invokeAction,
                                            drInvoked,
                                            Target.Emanation(3),
                                            false,
                                            drInvoked.Rune.InvocationProperties.SoundEffectAfterInvocation
                                            ?? drInvoked.Rune.InvocationProperties.SoundEffectBeforeInvocation
                                            ?? ModData.SfxNames.INVOKE_RUNE)
                                        is not { } chosenTargets)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    drThis.Tag = chosenTargets;
                                },
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn
                                        || drInvoked.Rune.InvocationProperties.EffectOnOneTarget
                                            is not {} invocationOnEachTarget
                                        || drThis.Tag is not ChosenTargets chosenTargets)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    foreach (Creature cr in chosenTargets.ChosenCreatures.ToList())
                                    {
                                        List<Creature> affectedCreatures = (await invocationOnEachTarget.Invoke(
                                                invokeAction, drInvoked, cr))
                                            ?.WhereNotNull()
                                            .ToList() ?? [cr];
                                        // Make the affected creature immune to further invocations of this rune
                                        foreach (Creature affectedCreature in affectedCreatures)
                                            affectedCreature.AddQEffect(CommonRuneRules.ImmunityToInvocation(drInvoked.Rune));
                                    }
                                    
                                    ModData.PersistentActions.UseUpRune(drThis.Source!, drThis.Rune);
                                },
                            },
                        (_,_) => "Choose a rune whose invocation will radiate outward in a 15-foot emanation from its bearer."),
                null,
                [Trait.Diacritic])
            .ToFeat();
        
        // Fob-, Diacritic Rune of Doubling
        yield return new Rune(
                RuneId.Fob,
                "This diacritic consists of pairs of diamonds in opposite positions around the base rune.",
                RuneDrawProperties.Diacritic("that doesn't deal damage")
                    .WithBaseRuneDoesNotDealDamage(true)
                    .WithDrawnRuneRequirement((a, dr) =>
                        dr.Rune.InvocationProperties.EffectOnOneTarget is not null
                        && dr.Rune.InvocationProperties.IsLegalTarget(a, a).CanBeUsed
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("You cannot be affected by this invocation")),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, the rune-bearer is affected by it as usual, and you can choose to also be affected by the invocation if possible.")
                    .WithDiacriticCreator(
                        (drawAction, drOnto) =>
                            drOnto.Rune.InvocationProperties.EffectOnOneTarget is not null
                            && !drOnto.Rune.InvocationProperties.DealsDamage
                            && drOnto.Rune.Id != RuneId.Marssyl
                            && drOnto.Rune.InvocationProperties.IsLegalTarget(drawAction.Owner, drawAction.Owner),
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"When {drOnto} is invoked, you can choose to also be affected by the invocation if possible.",
                                drBase)
                            {
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn
                                        || drInvoked.Rune.InvocationProperties.EffectOnOneTarget
                                            is not {} invocationOnEachTarget)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    if (!drInvoked.Rune.InvocationProperties.IsLegalTarget(
                                            drThis.Source!, drThis.Source!))
                                    {
                                        drThis.Source!.Battle.Log($"{drThis.Source!.Name} is not a legal target for this invocation.");
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }
                                },
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (!await drThis.Source!.AskForConfirmation(
                                            drThis.Rune.Illustration,
                                            $"{{b}}{drThis.Rune.FullName}{{/b}}\nWould you like to be affected by the following invocation?\n\n{drInvoked.Rune.InvocationProperties.InvocationTextWithHeightening?.Invoke(drInvoked.Rune, drInvoked.Source!.Level) ?? "UNKNOWN INVOCATION."}",
                                            "Yes", "No"))
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    // Apply the effects with yourself as the target.
                                    List<Creature> affectedCreatures = (await drInvoked.Rune.InvocationProperties
                                            .EffectOnOneTarget!.Invoke(
                                                invokeAction,
                                                drInvoked,
                                                drThis.Source!))
                                        ?.WhereNotNull()
                                        .ToList() ?? [];
                                    
                                    // Make the affected creatures immune to further invocations of this rune
                                    foreach (Creature affectedCreature in affectedCreatures)
                                        affectedCreature.AddQEffect(CommonRuneRules.ImmunityToInvocation(drInvoked.Rune));
                                },
                            },
                        (_,_) => "Choose a rune to also affect you when invoked."),
                null,
                [Trait.Diacritic])
            .WithIllustration(new DoublePortraitIllustration(
                IllustrationName.RunestoneWinged,
                IllustrationName.RunestoneWinged))
            .ToFeat();
        
        // Kit-, Diacritic Rune of Mercy
        yield return new Rune(
                RuneId.Kit,
                "This diacritic frames a rune with soft curves that mitigate its deadlier effects but make them harder to resist.",
                RuneDrawProperties.Diacritic(
                        "rune that deals damage and doesn't have the death or void trait")
                    .WithBaseRuneMustDealDamage()
                    .WithDrawnRuneRequirement((a, dr) =>
                    {
                        if (dr.Traits.Contains(Trait.Death))
                            return Usability.NotUsableOnThisCreature("Rune has the death trait");
                        if (dr.Traits.Contains(Trait.Negative))
                            return Usability.NotUsableOnThisCreature("Rune has the void trait");
                        return Usability.Usable;
                    }),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, it has the nonlethal trait, but the rune-bearer takes a –1 status penalty to any saving throw.")
                    .WithDiacriticCreator(
                        (drawAction, drOnto) =>
                            !drOnto.Traits.ContainsOneOf([Trait.Death, Trait.Negative])
                            && drOnto.Rune.InvocationProperties.DealsDamage,
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"The invocation of {drOnto} is nonlethal and the rune-bearer has a -1 status penalty to any saving throws against it.",
                                drBase)
                            {
                                BonusToDefenses = (qfThis, action, def) =>
                                    def.IsSavingThrow()
                                    && action?.HasTrait(Trait.Invocation) == true
                                    && action.Tag is RuneActionTag tag
                                    && tag.ChosenDrawnRune == drBase
                                        ? new Bonus(-1, BonusType.Status, (qfThis as DrawnRune)!.Rune.FullName)
                                        : null,
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;
                                    invokeAction.WithExtraTrait(Trait.Nonlethal);
                                }
                            },
                        (_,_) => "Choose a rune to become nonlethal and impose a -1 status penalty to its saving throws on the rune-bearer."),
                null,
                [Trait.Diacritic])
            .ToFeat();
        
        // Per-, Diacritic Rune of Continuum
        yield return new Rune(
                RuneId.Per,
                "This whirling diacritic recirculates the magic of the base rune onto another creature.",
                RuneDrawProperties.Diacritic(null, true),
                RunePassiveProperties.Diacritic(
                        "After the base rune is invoked, it automatically traces itself on a different target of your choice within 15 feet of the original rune-bearer.",
                        true)
                    .WithDiacriticCreator(
                        null,
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                drawAction, rune,
                                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                                (drThis, drOnto) => $"After {drOnto} is invoked, it automatically traces itself onto a different target of your choice within 15 feet of {drThis.Owner?.ToColoredName() ?? "the original rune-bearer"}.\n\nCan only be invoked once per encounter.\n",
                                drBase)
                            {
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;
                                    
                                    // Normally the invoke animation happens, then the effects and SFX play.
                                    // This adds a small delay after that for the sound to finish before
                                    // immediately beginning sun-'s SFX and VFX so that it's easier to process.
                                    await Task.Delay(500);

                                    await CommonRuneRules.AskToTraceARune(
                                        // Just in-case fallbacks
                                        drInvoked.Source!.Alive
                                            ? drInvoked.Source!
                                            : drThis.Source!.Alive
                                                ? drThis.Source!
                                                : drInvoked.Owner,
                                        // ReSharper disable once VariableHidesOuterVariable
                                        runeFilter: rune => rune == drInvoked.Rune,
                                        targetFilter: cr => cr.DistanceTo(drInvoked.Owner) <= 3,
                                        adjustAction: traceAction =>
                                        {
                                            traceAction.Name =
                                                traceAction.Name.Replace("Trace", "Retrace");
                                            traceAction.ContextMenuName =
                                                traceAction.ContextMenuName?.Replace("Trace", "Retrace");
                                            traceAction.CastsOrUses = "retraces";
                                            var cachedSound = traceAction.SoundEffectName;
                                            traceAction
                                                .WithSoundEffect(null)
                                                .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
                                                .WithAdjustTarget<CreatureTarget>(crTar => crTar
                                                    .AlternateTileOfOrigin = drInvoked.Owner.Space.CenterTile)
                                                .WithPrologueEffectOnChosenTargetsBeforeRolls(async (_, _, _) =>
                                                {
                                                    // Animation on rune-bearer
                                                    Sfxs.Play(ModData.SfxNames.INVOKED_PER);
                                                    await CommonRuneRules.PlayInvocationSplashAnimation(drInvoked.Owner, drThis.Rune.Illustration);
                                                    
                                                    Sfxs.Play(cachedSound);
                                                });
                                            traceAction.EffectOnOneTarget = async (traceAction2, caster, target, _) =>
                                            {
                                                if (await CommonRuneRules.DrawRuneOnCreature(
                                                            traceAction2, target, drInvoked.Rune)
                                                        is not {} newDrawnRunes
                                                    || newDrawnRunes.LastOrDefault() is not {} newDrawnRune)
                                                {
                                                    traceAction.RevertRequested = true;
                                                    return;
                                                }

                                                (traceAction.Tag as RuneActionTag)?.CreatedDrawnRune = newDrawnRune;
                                                ModData.PersistentActions.UseUpRune(drThis.Source!, drThis.Rune);
                                            };
                                        },
                                        overrideRange: 3,
                                        overridePassButton: " Confirm no retrace ",
                                        question: $"Choose a target to Trace {drInvoked} onto.");
                                },
                            },
                        (drawAction, rune) => "Choose a rune that, when invoked, will automatically trace itself on another target within 15 feet of the rune-bearer."),
                null,
                [Trait.Diacritic])
            .WithIllustration(new CornerIllustration(
                IllustrationName.RunestoneWinged,
                ModData.Illustrations.TraceRune,
                Direction.Southwest))
            .ToFeat();
        
        // Sun-, Diacritic Rune of Preservation
        yield return new Rune(
                RuneId.Sun,
                "This spiraling diacritic channels the magic of a rune outwards, then back to the same location, allowing a rune to reconstitute itself.",
                RuneDrawProperties.Diacritic(null, true),
                RunePassiveProperties.Diacritic(
                        "After the base rune is invoked, the base rune automatically traces itself back upon the same target.",
                        true)
                    .WithDiacriticCreator(
                        // No particular extra requirements
                        null,
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                    drawAction,
                                    rune,
                                    (drThis, drOnto) =>
                                        $"After {drOnto} is invoked, it traces itself back upon the same target.\n\nCan only be invoked once per encounter.\n",
                                    drBase)
                                {
                                    AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                    {
                                        if (drThis.Disabled
                                            || drInvoked != drThis.DrawnOn
                                            || drInvoked.Owner.DeathScheduledForNextStateCheck)
                                            return;
                                        
                                        // Normally the invoke animation happens, then the effects and SFX play.
                                        // This adds a small delay after that for the sound to finish before
                                        // immediately beginning sun-'s SFX and VFX so that it's easier to process.
                                        await Task.Delay(500);
                                        
                                        CombatAction sunRedraw = CommonRuneRules.CreateTraceAction(
                                                drInvoked.Source!,
                                                drInvoked.Rune,
                                                2,
                                                99,
                                                "Retrace Rune",
                                                "retraces")
                                            .WithActionCost(0)
                                            .WithSoundEffect(ModData.SfxNames.INVOKED_SUN)
                                            .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
                                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (traceAction, caster, targets) =>
                                            {
                                                if (targets.ChosenCreature is null)
                                                    return;
                                                // Animation on rune-bearer
                                                await CommonRuneRules.PlayInvocationSplashAnimation(targets.ChosenCreature, drThis.Rune.Illustration);
                                            });

                                        sunRedraw.ProjectileKind = ProjectileKind.None;
                                        sunRedraw.EffectOnOneTarget = async (traceAction, caster, creature, result) =>
                                        {
                                            if (await CommonRuneRules.DrawRuneOnCreature(
                                                    sunRedraw,
                                                    drInvoked.Owner,
                                                    drInvoked.Rune,
                                                    drInvoked.DrawnOn,
                                                    false) is not {} newDrawnRunes
                                                || newDrawnRunes.LastOrDefault() is not {} newDrawnRune)
                                            {
                                                traceAction.RevertRequested = true;
                                                return;
                                            }

                                            (traceAction.Tag as RuneActionTag)?.CreatedDrawnRune = newDrawnRune;
                                            ModData.PersistentActions.UseUpRune(drThis.Source!, drThis.Rune);
                                        };

                                        await drThis.Owner.Battle.GameLoop.FullCast(
                                            sunRedraw,
                                            ChosenTargets.CreateSingleTarget(drInvoked.Owner));
                                    },
                                },
                        (_,_) => "Choose a rune that will be retraced after being invoked."),
                null,
                [Trait.Diacritic])
            .ToFeat();

        // Ti-, Diacritic Rune of Fundaments
        yield return new Rune(
                RuneId.Ti,
                "This wavering diacritic encompasses the base rune to slightly change its meaning.",
                RuneDrawProperties.Diacritic(
                        "that deals acid, cold, electricity, or fire damage")
                    .WithBaseRuneMustDealDamageOfTypes([DamageKind.Acid, DamageKind.Cold, DamageKind.Electricity, DamageKind.Fire]),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, you can alter the damage its invocation deals, choosing acid, cold, electricity, or fire.")
                    .WithDiacriticCreator(
                        (drawAction, drOnto) =>
                            TiGetBaseRuneDamage(drOnto)
                                is DamageKind.Acid
                                or DamageKind.Cold
                                or DamageKind.Electricity
                                or DamageKind.Fire,
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                drawAction, rune,
                                (drThis, drOnto) => $"When {drOnto} is invoked, you can alter the damage its invocation deals, choosing acid, cold, electricity, or fire.",
                                drBase)
                            {
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;

                                    if (await TiAskDamageChoice(
                                            drThis.Source!,
                                            drThis.Rune,
                                            $"Choose a damage type for {drInvoked} to deal.",
                                            [DamageKind.Acid, DamageKind.Cold, DamageKind.Electricity, DamageKind.Fire]
                                        )
                                        is not { } choice)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    invokeAction.Owner.AddQEffect(new QEffect()
                                    {
                                        Name = "[TI- DAMAGE CHANGER]",
                                        ConvertAllOutgoingDamageToDamageKind = choice.Kind
                                    });
                                },
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;
                                    invokeAction.Owner.RemoveAllQEffects(qf =>
                                        qf.Name == "[TI- DAMAGE CHANGER]");
                                }
                            },
                        (drawAction, rune) => "Choose a rune to be able to alter its invocation damage type to acid, cold, electricity, or fire."),
                null,
                [Trait.Diacritic])
            .ToFeat();
        
        // Ur-, Diacritic Rune of Intensity
        yield return new Rune(
                RuneId.Ur,
                "This diacritic accentuates the base rune with bolder lines to give greater weight to its effects.",
                RuneDrawProperties.Diacritic("that deals damage")
                    .WithBaseRuneMustDealDamage(true),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, its invocation gains a status bonus to damage equal to 2 plus half your level.",
                        heightening: (rune, level) =>
                        {
                            return
                                $"When the base rune is invoked, its invocation gains a +{S.HeightenedVariable(2 + (level / 2), 2 + (rune.BaseLevel / 2))} status bonus to damage.";
                        })
                    .WithDiacriticCreator(
                        (drawAction, drOnto) =>
                            drOnto.Rune.InvocationProperties.DealsDamage
                            || drOnto.Rune.Id == RuneId.Marssyl,
                        (drawAction, rune, drBase) =>
                        {
                            int bonusAmount = 2 + (drawAction.Owner.Level / 2);
                            return new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"The invocation of {drOnto} gains a +{bonusAmount} status bonus to its damage.",
                                drBase)
                            {
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;
                                    QEffect invokeBonus = new QEffect()
                                    {
                                        BonusToDamage = (qfThis, action, defender) =>
                                        {
                                            if (drThis.Disabled)
                                                return null;

                                            Bonus urBonus = new Bonus(bonusAmount, BonusType.Status,
                                                "Ur, Diacritic Rune of Intensity");

                                            // Apply to an invocation action's damage
                                            if ((action.HasTrait(Trait.Invocation)
                                                 || action.ActionId == ModData.ActionIds.InvokeRune)
                                                && (action.Tag as RuneActionTag)?.ChosenDrawnRune is
                                                { } drInvokingDamage
                                                && drInvokingDamage == drInvoked)
                                                return urBonus;

                                            // Apply to a Strike being buffed by Marssyl
                                            if (action.HasTrait(Trait.Strike)
                                                && qfThis.Owner.QEffects.Any(qf =>
                                                    qf.Traits.Contains(Trait.Invocation)
                                                    && (qf.Name?.ToLower().Contains("marssyl") ?? false)
                                                    && qf.Tag is Item marssylItem
                                                    && marssylItem == action.Item))
                                                return urBonus;

                                            return null;
                                        },
                                        /*AfterYouTakeAction = async (qfThis, action) =>
                                        {
                                            if (action == invokeAction)
                                                qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                        },*/
                                    }
                                    // No expiration because it needs to exist longer for invocations such as Esvadir which have hidden subsidiaries going on and Oraloq which last a round.
                                    // Is removed on its own when the rune is invoked, and it only applies to the same type, so it should be safe to manually expire that way in this callback structure.
                                    .WithExpirationAtEndOfOwnersNextTurn();
                                    invokeAction.Owner.AddQEffect(invokeBonus);
                                },
                            };
                        },
                        (drawAction, rune) =>
                            $"Choose a rune to gain a +{2 + (drawAction.Owner.Level / 2)} status bonus to its invocation's damage."),
                null,
                [Trait.Diacritic])
            .ToFeat();

        #endregion

        #region 9th-Level
        
        // Astillu, Rune of Submersion
        yield return new Rune(
                RuneId.Astillu,
                "Originating from ancient runic scripts, this wavy rune is popular with practitioners of rune magic who want to easily traverse their watery environs.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(),
                new RunePassiveProperties(
                        "The rune-bearer gains {r}swimming{/r}. Its melee Strikes don't take the usual circumstance penalty for passing through water.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            "You gain swimming, and your melee Strikes don't take the usual circumstance penalty for passing through water.")
                        {
                            /*BeforeBonusesAreFlattened = (qfThis, bonuses, reason, action, defender) =>
                            {
                                if (reason is not BonusCalculationReason.AttackRoll
                                    || action?.Owner != qfThis.Owner
                                    || !action.HasTrait(Trait.Melee)
                                    || !action.HasTrait(Trait.Strike))
                                    return;
                                bonuses.RemoveAll(bonus =>
                                    bonus.BonusSource == "Aquatic Combat"
                                    && bonus.BonusType is BonusType.Circumstance);
                            }*/
                            StateCheck = qfThis =>
                            {
                                if (DrawnRune.ShouldNotPerformPassiveEffect(qfThis))
                                    return;
                                qfThis.Owner.AddQEffect(QEffect.Swimming()
                                    .WithExpirationEphemeral());
                                // All this does is remove the attack penalties
                                qfThis.Owner.AddQEffect(new QEffect()
                                    { Id = QEffectId.ReturnToTheSea }
                                    .WithExpirationEphemeral());
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "The rune's power pushes the bearer forward in a surprising surge of speed. The rune-bearer can Stride underwater up to 60 feet as a free action, and this movement doesn't trigger reactions.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        d.Space.AnyTile(tile => tile.IsWater)
                        || d.Battle.Map.FinalTerrain == TerrainKind.Aquatic
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Not in water"))
                    .WithAdditionalRequirement((a, d) =>
                        d.WouldBeAbleToStride()
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Can't Stride"))
                    .WithSoundBeforeInvocation(SfxName.Footsteps)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        QEffect mobility = new QEffect()
                        {
                            Id = QEffectId.IgnoreAoOWhenMoving
                        };
                        effectTarget.AddQEffect(mobility);
                        await effectTarget.StrideOrStepAdvancedAsync(
                            $"Choose where to Stride underwater as part of invoking {invokedRune}, or right-click to cancel.",
                            permissibleTarget: tile =>
                                tile.IsWater
                                || tile.Battle.Map.FinalTerrain == TerrainKind.Aquatic,
                            allowCancel: true,
                            allowPass: true);
                        effectTarget.RemoveAllQEffects(qf => qf == mobility);
                        
                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(SfxName.TidalSurge),
                [Trait.Water])
            .ToFeat();
        
        // Cruonign, Rune of Leeching
        yield return new Rune(
                RuneId.Cruonign,
                "The jagged shape of this rune resembles a vampire's fangs.",
                new RuneDrawProperties(
                        "drawn on a slashing or piercing weapon",
                        drawnOnItem: true)
                    .WithAllyRequirement(hasEnemyUseCases: true)
                    .WithHoldsItemRequirement(
                        item =>
                            item.WeaponProperties is not null
                            && !item.HasTrait(Trait.Unarmed)
                            && item.DetermineDamageKinds().Any(kind =>
                                kind is DamageKind.Piercing or DamageKind.Slashing),
                        "no piercing or slashing weapon")
                    // If you are able to draw this on an enemy, and can therefore see
                    // this targeting requirement, then this adds a requirement that
                    // there be another enemy within 30 feet and line of sight to avoid accidents.
                    // This requirement is ignored if you know En to be able to increase the emanation.
                    .WithCreatureRequirement((a, d) =>
                    {
                        if (d.EnemyOf(a))
                        {
                            if (d.Battle.AllCreatures.Any(cr =>
                                    cr.EnemyOf(a)
                                    && a.HasLineOfEffectTo(cr) < CoverKind.Blocked
                                    && cr.DistanceTo(a) <= 6)
                                || RunicRepertoireTag.GetRepertoire(a)?.IsKnown(RuneId.En, a.Level) == true)
                                return Usability.Usable;
                            else
                                return Usability.NotUsableOnThisCreature("No enemies in range of invocation");
                        }
                        else
                            return Usability.Usable;
                    }),
                new RunePassiveProperties(
                        "Strikes with the rune-bearing weapon deal an additional 3 void damage. Whenever a living creature is damaged by one of these Strikes, the rune-bearer gains temporary Hit Points equal to the void damage the target took (after applying resistances and the like).",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(3, 2, 1, level);
                            return
                                $"Strikes with the rune-bearing weapon deal an additional {S.HeightenedVariable(finalValue, baseValue)} void damage. Whenever a living creature is damaged by one of these Strikes, the rune-bearer gains temporary Hit Points equal to the void damage the target took (after applying resistances and the like).";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int bonusDamage = rune.CalculateHeightening(3, 2, 1, drawAction.Owner.Level).FinalValue;
                        
                        return subTarget is Item itemTarget
                            ? [MakeCruonignPassive(itemTarget)!]
                            : await CommonRuneRules.ChooseAnItemToDrawOn(
                                drawAction,
                                drawAction.Owner,
                                target,
                                item =>
                                    item.WeaponProperties is not null
                                    && !item.HasTrait(Trait.Unarmed)
                                    && item.DetermineDamageKinds().Any(kind =>
                                        kind is DamageKind.Piercing or DamageKind.Slashing),
                                MakeCruonignPassive,
                                $"Choose a weapon whose Strikes will deal additional {bonusDamage} void damage and generate temporary Hit Points.",
                                rune);

                        DrawnRune? MakeCruonignPassive(Item? targetItem)
                        {
                            if (targetItem is null)
                                return null;
                            
                            DrawnRune cruonignPassive = new DrawnRune(
                                    drawAction,
                                    rune,
                                    (drThis, item) => $"Strikes with {item.Illustration.IllustrationAsIconString} {item.Name.WithColor("Blue")} deal an additional {bonusDamage} void damage, and you gain temporary Hit Points equal to the void damage taken by the target of the Strike.",
                                    targetItem)
                                {
                                    AfterYouDealDamageAgainstPrimaryTargetQ = async (qfThis, action, attacker, defender, result, dEvent) =>
                                    {
                                        DrawnRune drThis = (qfThis as DrawnRune)!;
                                        if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                            return;
                                        
                                        // Must deal damage using a strike with the item this is drawn on
                                        if (action.HasTrait(Trait.Strike) != true
                                            || result < CheckResult.Success
                                            || drThis.DrawnOn is not Item drawnItem
                                            || action.Item != drawnItem
                                            || drawnItem.WeaponProperties is null
                                            || !defender.IsLivingCreature)
                                            return;
                                        
                                        // Determine weapon damage dice count
                                        int voidAmount = dEvent.KindedDamages
                                            .FirstOrDefault(kd => kd.DamageKind is DamageKind.Negative)
                                            ?.ResolvedDamage ?? 0;

                                        if (voidAmount == 0)
                                            return;

                                        attacker.GainTemporaryHP(
                                            DiceFormula.FromText(
                                                voidAmount.ToString(),
                                                rune.FullName));
                                    },
                                    AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                    {
                                        if (drThis != drInvoked)
                                            return;

                                        invokeAction.Owner.RemoveAllQEffects(qf =>
                                            qf.Name == "[Cruonign Healing]");
                                    },
                                };
                            // Done separately to capture references
                            cruonignPassive.AddExtraKindedDamageOnStrike = (strike, defender) =>
                            {
                                if (DrawnRune.ShouldNotPerformPassiveEffect(cruonignPassive))
                                    return null;
                                return new KindedDamage(
                                    DiceFormula.FromText(
                                        bonusDamage.ToString(),
                                        rune.FullName),
                                    DamageKind.Negative);
                            };

                            return cruonignPassive;
                        }
                    }),
                new RuneInvocationProperties(
                        "The rune lashes out in a 30-foot cone from the wielder of the weapon, dealing 4d10 void damage to each creature in the area, with a basic Fortitude save. If any creatures take damage, the wielder regains 12 Hit Points.",
                        (rune, level) =>
                        {
                            var damage = rune.CalculateHeightening(4, 2, 1, level);
                            var healing = rune.CalculateHeightening(12, 2, 3, level);
                            return
                                $"The rune lashes out in a 30-foot cone from the wielder of the weapon, dealing {S.HeightenedVariable(damage.FinalValue, damage.BaseValue)}d10 void damage to each creature in the area, with a basic Fortitude save. If any creatures take damage, the wielder regains {S.HeightenedVariable(healing.FinalValue, healing.BaseValue)} Hit Points.";
                        })
                    .WithAdditionalRequirement((runesmith, runeBearer) =>
                    {
                        if (runeBearer.Battle.AllCreatures
                            .Any(cr =>
                                cr.EnemyOf(runesmith)
                                && cr.DistanceTo(runeBearer) <= 6))
                            return Usability.Usable;
                        return Usability.NotUsableOnThisCreature("No enemy within range of the target");
                    })
                    .WithDealsDamage((rune, level) =>
                        ($"{rune.CalculateHeightening(4, 2, 1, level).FinalValue}d10", DamageKind.Negative))
                    .WithDefense(Defense.Fortitude)
                    .WithAffectsArea()
                    .WithHideTooltip()
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Cone(6),
                            false,
                            SfxName.MajorNegative,
                            innerInvoke =>
                            {
                                innerInvoke.WithEffectOnSelf(async (innerInvoke2, caster) =>
                                {
                                    QEffect healAfter = new QEffect(ExpirationCondition.ExpiresAtEndOfAnyTurn)
                                    {
                                        Name = "[Cruonign Healing]",
                                        AfterYouDealDamageQ = async (qfThis, attacker, damagingAction, defender) =>
                                        {
                                            if (damagingAction.Tag is not RuneActionTag tag
                                                || tag.ChosenDrawnRune != invokedRune
                                                || qfThis.UsedUpPermanently)
                                                return;
                                
                                            int healingAmount = invokedRune.Rune.CalculateHeightening(12, 2, 3, invokedRune.Source!.Level).FinalValue;
                                            await qfThis.Owner.HealAsync(
                                                DiceFormula.FromText(
                                                    healingAmount.ToString(),
                                                    invokedRune.Rune.FullName),
                                                damagingAction);

                                            qfThis.UsedUpPermanently = true;
                                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                        },
                                    };
                                    caster.AddQEffect(healAfter);
                                });
                            },
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget);

                        return effectTarget;
                    }),
                [Trait.Death, Trait.Negative])
            .WithLevelText("+2", "The additional void damage increases by 1, the damage of the invocation increases by 1d10, and the healing increases by 3.")
            .ToFeat();
        
        // Feikris, Rune of Gravity
        yield return new Rune(
                RuneId.Feikris,
                "The lines of this rune overlap strangely, making it seem larger than it really is.",
                new RuneDrawProperties(
                        "drawn on a creature wearing armor",
                        drawnOnCreature: true)
                    .WithAllyRequirement(true)
                    .WithWearsArmorRequirement(),
                new RunePassiveProperties(
                        "The rune-bearer gains a +2 status bonus to Athletics checks and gains the benefits of the Titan Wrestler feat.",
                        (rune, level) =>
                        {
                            int bonus = level >= 17 ? 3 : 2;
                            return $"The rune-bearer gains a +{S.HeightenedVariable(bonus, 2)} item bonus to Athletics checks.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int athleticsBonus = drawAction.Owner.Level >= 17 ? 3 : 2;
                        QEffectId titanWrestler = target.Proficiencies.Get(Trait.Athletics) >= Proficiency.Legendary
                            ? QEffectId.TitanWrestlerLegendary
                            : QEffectId.TitanWrestler;
                        DrawnRune feikrisPassive = new DrawnRune(
                            drawAction,
                            rune,
                            $"You have a {$"+{athleticsBonus}".WithColor(athleticsBonus > 2 ? "Blue" : null)} status bonus to Athletics checks and gain the {(titanWrestler == QEffectId.TitanWrestlerLegendary ? "legendary " : null)}benefits of the Titan Wrestler feat.")
                        {
                            BonusToSkills = skill =>
                                skill is Skill.Athletics
                                    ? new Bonus(athleticsBonus, BonusType.Status, "Feikris")
                                    : null,
                            StateCheck = qfThis =>
                                qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                    { Id = titanWrestler })
                        };
                        return feikrisPassive;
                    }),
                new RuneInvocationProperties(
                        "All creatures in a 15-foot emanation around the rune-bearer must succeed at a Fortitude save or be pulled 5 feet toward the rune-bearer (or 10 feet on a critical failure).",
                        null)
                    .WithAffectsArea()
                    .WithDefense(Defense.Fortitude)
                    .WithHideTooltip() // Doesn't affect rune-bearer
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 3f; // 15 feet
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize),
                            false,
                            ModData.SfxNames.INVOKED_FEIKRIS,
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            async (_, emanationTarget, result) =>
                            {
                                int distance = result switch
                                {
                                    CheckResult.CriticalFailure => 2,
                                    CheckResult.Failure => 1,
                                    _ => 0
                                };
                                if (distance > 0)
                                    await emanationTarget.PullTowards(
                                        effectTarget.Space.GetClosestTileTo(emanationTarget),
                                        distance);
                            });

                        return effectTarget;
                    }),
                [Trait.Arcane])
            .WithLevelText("17th", "The status bonus increases to +3.")
            .ToFeat();

        // Germantria, Rune of Partnership
        yield return new Rune(
                RuneId.Germantria,
                "When drawn, this knobby rune pulses with your own heartbeat, creating a vital connection between you and the bearer.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(excludeSelf: true),
                new RunePassiveProperties(
                        "The rune-bearer takes half damage from all effects that deal Hit Point damage, and you take the remainder of the damage. When you take this damage, you don’t apply any resistances, weaknesses, or other abilities you have to that damage; you simply take that amount of damage. If either you or the rune-bearer is reduced to 0 Hit Points, the rune fades without being invoked.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction, rune,
                            $"You take half as much damage, and {drawAction.Owner.ToColoredName()} takes the rest.")
                        {
                            // Cached damage to deal
                            Tag = 0,
                            YouAreDealtDamageEvent = async (qfThis, dEvent) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                
                                // Halve the damage, rounding down.
                                int halvedDown = (int)Math.Round(
                                    dEvent.TotalResolvedDamage / 2d,
                                    MidpointRounding.ToZero);
                                int halvedUp = (dEvent.TotalResolvedDamage - halvedDown);
                                
                                // Cache it, rounding up for the runesmith.
                                drThis.Tag = halvedUp;
                                
                                // Apply reduction, using rounded-up value for reduction
                                // because that means the rounded-down total.
                                dEvent.ReduceBy(halvedUp, $"Half ({drThis.Rune.FullName})");
                            },
                            AfterYouTakeDamage = async (qfThis, _, _, _, _) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                Creature runesmith = drThis.Source!;
                                if (drThis.Disabled
                                    || !drThis.IsFirstInstanceOf()
                                    || drThis.Tag is not int amount
                                    || amount < 1)
                                    return;
                                
                                // Deal directly, do not construct an event.
                                if (runesmith.TemporaryHP > 0)
                                {
                                    int takeableTempHP = Math.Min(runesmith.TemporaryHP, amount);
                                    int totalDamage1 = amount - takeableTempHP;
                                    runesmith.TemporaryHP -= takeableTempHP;
                                    runesmith.TakeDamage(totalDamage1);
                                }
                                else
                                    runesmith.TakeDamage(amount);
                                
                                // Log the event.
                                runesmith.Battle.Log(
                                    $"{runesmith} takes {amount.WithTag("b")} damage from {drThis.ToWordString()}'s effect.",
                                    drThis.Rune.FullName,
                                    CommonRuneRules.StatBlocks.CreateRuneDescription(drThis.Rune, runesmith.Level),
                                    new Traits(drThis.Rune.Traits.ToArray()));

                                // Reset cached damage
                                drThis.Tag = 0;
                                
                                // Trigger what happens after taking damage.
                                // Don't include the action nor count as a critical, that
                                // seems like it'd be too much to critically down 2 people.
                                // Especially for an effect that simply 'takes the damage'.
                                await DeathRules.CheckReachingZeroHP(runesmith, null, false);
                            },
                            StateCheck = qfThis =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Owner.HP <= 0 || drThis.Source!.HP <= 0)
                                {
                                    drThis.ExpiresAt = ExpirationCondition.Immediately;
                                    CommonRuneRules.LogInvokedRune(drThis.Source!, drThis, drThis.Owner);
                                }
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "(healing) The connection between you and the rune-bearer is severed in a burst of healing energy. Both you and the rune-bearer regain 20 Hit Points.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(20, 2, 5, level);
                            return
                                $"(healing) The connection between you and the rune-bearer is severed in a burst of healing energy. Both you and the rune-bearer regain {S.HeightenedVariable(finalValue, baseValue)} Hit Points.";
                        })
                    .WithSoundBeforeInvocation(SfxName.Healing)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        int healingAmount = invokedRune.Rune.CalculateHeightening(20, 2, 5, invokedRune.Source!.Level).FinalValue;
                        
                        CombatAction healAction = CombatAction.CreateSimple(
                                invokedRune.Source!,
                                invokedRune.Rune.FullName,
                                [..invokedRune.Traits, Trait.Healing])
                            .WithIllustration(invokedRune.Rune.Illustration);
                        
                        List<Creature> heals = new List<Creature>([effectTarget, invokedRune.Source!])
                            // Because this effect is performing its own kind of targeting irrespective
                            // of any ChosenTargets effects or what have you, this is a good fallback.
                            // It might be redundant, it's hard to say since these implementations are
                            // kind of complex.
                            .Where(cr => !CommonRuneRules.IsImmuneToThisInvocation(cr, invokedRune.Rune))
                            .ToList();
                        foreach (Creature cr in heals)
                            await cr.HealAsync(healingAmount.ToString(), healAction);

                        return heals;
                    }))
            .ToFeat();
        
        // Ichelsu, Rune of Observation
        yield return new Rune(
                RuneId.Ichelsu,
                "A ring of dotted circles, this rune allows a creature marked with it to see all.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(true),
                new RunePassiveProperties(
                        $"The target is affected by {SpellId.SeeInvisibility.ToLink("see the unseen", null, null)} and gains {ModData.Tooltips.MiscAllAroundVision("all-around vision")}.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            """
                            You gain the effects of {i}see the unseen{/i} and all-around vision.
                            {b}See the Unseen{/b} You see invisible creatures as though they were merely concealed.
                            {b}All-Around Vision{/b} You can't be flanked.
                            """)
                        {
                            WhenYouAcquireThis = qfThis =>
                            {
                                /*QEffect see = QEffect.SeeInvisibility()
                                    .With(qf =>
                                    {
                                        qf.HideFromPortrait = true;
                                        qf.Supereffect = qfThis;
                                    });
                                QEffect vision = QEffect.AllAroundVision()
                                    .With(qf =>
                                    {
                                        qf.Innate = false;
                                        qf.HideFromPortrait = true;
                                        qf.Supereffect = qfThis;
                                    });
                                qfThis.Owner
                                    .AddQEffect(see)
                                    .AddQEffect(vision);*/
                                CommonStealthActions.ThisCreatureGainedTheAbilityToSeeInvisible(qfThis.Owner);
                            },
                            StateCheck = qfThis =>
                            {
                                QEffect see = QEffect.SeeInvisibility()
                                    .WithExpirationEphemeral()
                                    .With(qf =>
                                    {
                                        qf.HideFromPortrait = true;
                                        qf.Supereffect = qfThis;
                                    });
                                QEffect vision = QEffect.AllAroundVision()
                                    .WithExpirationEphemeral()
                                    .With(qf =>
                                    {
                                        qf.Innate = false;
                                        qf.HideFromPortrait = true;
                                        qf.Supereffect = qfThis;
                                    });
                                qfThis.Owner
                                    .AddQEffect(see)
                                    .AddQEffect(vision);
                            },
                            /*WhenExpires = qfThis =>
                            {
                                qfThis.Owner.RemoveAllQEffects(qf =>
                                    qf.Id is QEffectId.SeeInvisibility 
                                        or QEffectId.AllAroundVision
                                    && qf.Supereffect == qfThis);
                            }*/
                        };
                    }),
                new RuneInvocationProperties(
                        "The eyes of the rune fly outwards, attaching to all creatures in a 20-foot emanation. Each of these creatures that would be invisible is concealed instead, and one that would be concealed for any other reason is not concealed. This effect lasts for 2 rounds.",
                        null)
                    .WithAffectsArea()
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 4f; // 20 feet
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize),
                            true,
                            ModData.SfxNames.INVOKED_ICHELSU,
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        effectTarget.DetectionStatus.HiddenTo.Clear();
                        effectTarget.DetectionStatus.Undetected = false;
                        QEffect invokedIchelsu = invokedRune.NewInvocationEffect(
                            "If you're invisible, you're concealed instead. If you're concealed for any other reason, you're no longer concealed.",
                            ExpirationCondition.Never,
                            qf =>
                            {
                                qf.Id = QEffectId.FaerieFire;
                                qf.WithExpirationAtStartOfSourcesTurn(invokeAction.Owner, 2);
                            });
                        effectTarget.AddQEffect(invokedIchelsu);

                        return effectTarget;
                    }),
                [Trait.Occult])
            .ToFeat();

        // Jurroz, Rune of Dragon Fury
        yield return new Rune(
                RuneId.Jurroz,
                "This craggy rune channels the fury of dragon kind.",
                new RuneDrawProperties(
                        "drawn onto a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(),
                new RunePassiveProperties(
                        /*or Steals from*/
                        "Whenever an enemy Strikes the rune-bearer, that enemy becomes {r:flat-footed}off-guard{/r} for 1 round.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction,
                            rune,
                            /*or Steals from*/
                            "Whenever a creature Strikes you, they become off-guard for 1 round.")
                        {
                            AfterYouTakeDamage = async (qfThis, amount, _, action, _) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (drThis.Disabled || !drThis.IsFirstInstanceOf())
                                    return;
                                if (amount < 1 || action?.HasTrait(Trait.Strike) != true)
                                    return;
                                action.Owner.AddQEffect(QEffect
                                    .FlatFooted("Jurroz, Rune of Dragon Fury")
                                    .WithExpirationInOneRound(qfThis.Owner.Battle)
                                    .With(qf =>
                                    {
                                        qf.Supereffect = qfThis;
                                        qf.Key = "JurrozPassive";
                                        qf.SourceAction = drawAction;
                                    }));
                            },
                        };
                    }),
                new RuneInvocationProperties(
                        /*or stolen from it*/
                        "As a {icon:FreeAction} free action, the rune-bearer can Fly up to 60 feet toward an enemy that has damaged it this encounter. If it ends this movement adjacent to the enemy, the enemy becomes {r:flat-footed}off-guard{/r} until the end of the rune-bearer's next turn.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        JurrozWhoDamagedMe(d)?.Count > 0
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Hasn't been damaged by any living enemies"))
                    .WithAdditionalRequirement((a, d) =>
                        d.WouldBeAbleToStride()
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Can't Stride"))
                    .WithSoundBeforeInvocation(ModData.SfxNames.INVOKED_JURROZ)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (JurrozWhoDamagedMe(effectTarget) is not { } damagedMe)
                        {
                            invokeAction.RevertRequested = true;
                            return null;
                        }
                        
                        // Add flying effects and ensure you have enough speed
                        QEffect bigFly = QEffect.Flying()
                            .WithExpirationAtEndOfThisTurn()
                            /*.With(qf => qf.BonusToAllSpeeds = qfFly =>
                                new Bonus(12, BonusType.Untyped, "Jurroz"))*/;
                        effectTarget.AddQEffect(bigFly);

                        // Perform flight
                        if (!await effectTarget.StrideOrStepAdvancedAsync(
                                $"Choose where to Fly for {invokedRune}, or right-click to pass. You should end your movement next to a creature who has damaged you.",
                                maximumSpeed: 12,
                                allowCancel: true,
                                allowPass: true,
                                passText: " Don't Fly ",
                                permissibleTarget: tile =>
                                    // I think I can enter this tile
                                    tile.LooksFreeTo(effectTarget)
                                    // For each creature who damaged me,
                                    // Allow tiles that are closer and don't get further away
                                    && damagedMe.Any(enemy =>
                                    {
                                        // Tiles must be closer to the enemy than I started to the enemy
                                        if (enemy.DistanceTo(tile) >= effectTarget.DistanceTo(enemy))
                                            return false;

                                        // Every step in the path to this tile must require you
                                        // to not move further away from the enemy.
                                        IList<Tile>? path = Pathfinding.GetPath(
                                            effectTarget, tile, effectTarget.Battle,
                                            new PathfindingDescription()
                                            {
                                                Squares = 12,
                                                Style = new MovementStyle() { MaximumSquares = 12 }
                                            });

                                        if (path is null)
                                            return false;

                                        // Return true if all tiles are as close or closer than the next tile
                                        return path
                                            .Select((int Current, int? Next) (pathTile, i) =>
                                                (pathTile.DistanceTo(enemy),
                                                    i < path.Count
                                                        ? path[i].DistanceTo(enemy)
                                                        : null))
                                            .Where(tup => tup.Next is not null)
                                            .All(tup => tup.Current <= tup.Next);
                                    })))
                        {
                            effectTarget.RemoveAllQEffects(qf => qf == bigFly);
                            invokeAction.RevertRequested = true;
                            return null;
                        }
                        
                        effectTarget.RemoveAllQEffects(qf => qf == bigFly);
                                    
                        // Apply off-guard to a creature
                        if (await effectTarget.Battle.AskToChooseACreature(
                                effectTarget,
                                damagedMe.Where(effectTarget.IsAdjacentTo),
                                invokedRune.Rune.Illustration,
                                "Choose a creature who has damaged you to make off-guard.",
                                "This creature becomes off-guard until the end of your next turn.",
                                " Don't inflict off-guard ")
                            is {} chosenCreature)
                        {
                            chosenCreature.AddQEffect(QEffect
                                .FlatFooted("Jurroz, Rune of Dragon Fury")
                                .WithExpirationAtEndOfSourcesNextTurn(effectTarget, true)
                                .With(qf => qf.Key = "JurrozInvocation"));
                        }

                        return effectTarget;
                    }))
            .ToFeat()
            .WithOnCreature(self =>
            {
                self.AddQEffect(new QEffect()
                {
                    StartOfCombatAfterInitiativeOrderIsSetUp = async qfThis =>
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        if (self.Battle.Pseudocreature is null
                            || self.Battle.Pseudocreature.HasEffect(ModData.QEffectIds.JurrozDamageTracker))
                            return;

                        QEffect damageTracker = new QEffect
                        {
                            Id = ModData.QEffectIds.JurrozDamageTracker,
                            Key = "JurrozDamageTracker",
                            Tag = new Dictionary<
                                Creature, // Has taken damage
                                HashSet<Creature> // Who damaged me
                            >(),
                        }.AddGrantingOfTechnical(
                            (_,_) => true,
                            (qfFeat2, qfTech) =>
                            {
                                if (qfFeat2.Tag is not Dictionary<Creature, HashSet<Creature>> damageHistory)
                                    return;
                                qfTech.AfterYouTakeDamage = async (qfTech2, _,_, action, _) =>
                                {
                                    if (action?.HasTrait(Trait.Strike) != true)
                                        return;

                                    if (!damageHistory.TryGetValue(qfTech2.Owner, out HashSet<Creature>? damagedBy))
                                    {
                                        damagedBy = [];
                                        damageHistory[qfTech2.Owner] = damagedBy;
                                    }
                                    damagedBy.Add(action.Owner);
                                };
                            });

                        self.Battle.Pseudocreature.AddQEffect(damageTracker);
                    } 
                });
            });
        
        // Kojastri, Rune of Insulation
        yield return new Rune(
                RuneId.Kojastri, //IllustrationName.FrostRunestone,
                "This rune insulates from harmful energy of all kinds.",
                new RuneDrawProperties(
                        "drawn a creature wearing armor",
                        drawnOnCreature: true) // drawn on armor
                    .WithAllyRequirement()
                    .WithWearsArmorRequirement(),
                new RunePassiveProperties(
                        "The creature wearing the armor gains resistance 4 to your choice of cold, electricity, or fire; and any creature that touches the rune-bearer or damages it with an unarmed attack or non-reach melee weapon takes 2 damage of the chosen type. The rune has the trait of the energy type chosen.",
                        (rune, level) =>
                        {
                            var resist = rune.CalculateHeightening(4, 2, 1, level);
                            var thorns = rune.CalculateHeightening(2, 2, 1, level);
                            return $"The creature wearing the armor gains resistance {S.HeightenedVariable(resist.FinalValue, resist.BaseValue)} to your choice of cold, electricity, or fire; and any creature that touches the rune-bearer or damages it with an unarmed attack or non-reach melee weapon takes {S.HeightenedVariable(thorns.FinalValue, thorns.BaseValue)} damage of the chosen type. The rune has the trait of the energy type chosen.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        if (await TiAskDamageChoice(
                                drawAction.Owner,
                                rune,
                                "Choose which damage type to insulate against.",
                                [DamageKind.Cold, DamageKind.Electricity, DamageKind.Fire])
                            is not { } choice
                            || choice.Trait is null)
                            return null;
                        
                        var resist = rune.CalculateHeightening(4, 2, 1, drawAction.Owner.Level);
                        var thorns = rune.CalculateHeightening(2, 2, 1, drawAction.Owner.Level);
                        
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"You have resistance {resist.FinalValue.WithColor("Blue")} to {choice.Kind.ToStringOrTechnical().ToLower().WithColor(choice.Kind.DamageKindToColor())} damage, and creatures take {thorns.FinalValue.WithColor("Blue")} {choice.Kind.ToStringOrTechnical().ToLower().WithColor(choice.Kind.DamageKindToColor())} damage when they touch you, damage you with an unarmed attack, or damage you with a non-reach melee weapon.")
                        {
                            Name = $"{rune.FullName} ({choice.Kind.ToStringOrTechnical().ToLower()})",
                            Tag = choice.Kind,
                            CountsAsABuff = true,
                            StateCheck = qfThis =>
                                qfThis.Owner.WeaknessAndResistance.AddResistance(choice.Kind, resist.FinalValue),
                            // Damage when merely touched
                            AfterYouAreTargeted = async (qfThis, action) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (action == drawAction
                                    || action.Owner == qfThis.Owner
                                    || drThis.Disabled
                                    || !drThis.IsFirstInstanceOf())
                                    return;

                                // Do not attempt to trigger on an attack. You must be damaged first.
                                if (ActionIsValidAttack(action)
                                    || action.Target is not CreatureTarget { RangeKind: RangeKind.Melee })
                                    return;
                                
                                await DealThornsDamage(drThis, action.Owner);
                            },
                            // Damage when damaged
                            AfterYouTakeDamage = async (qfThis, _, _, action, _) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (action == null || !ActionIsValidAttack(action)
                                    || drThis.Disabled
                                    || !drThis.IsFirstInstanceOf())
                                    return;
                                
                                await DealThornsDamage(drThis, action.Owner);
                            }
                        }
                        .WithTrait(choice.Trait.Value);

                        bool ActionIsValidAttack(CombatAction action)
                        {
                            return action.Target is CreatureTarget { RangeKind: RangeKind.Melee }
                                && action.HasTrait(Trait.Weapon)
                                && (action.HasTrait(Trait.Unarmed) || !action.HasTrait(Trait.Reach));
                        }

                        async Task DealThornsDamage(DrawnRune drThis, Creature enemy)
                        {
                            await CommonSpellEffects.DealDirectDamage(
                                CombatAction.CreateSimple(
                                        drThis.Owner, rune.FullName,
                                        [..drThis.Traits])
                                    .WithTag(drThis)
                                    .WithOrigin(new ActionOrigin
                                    {
                                        QEffect = drThis,
                                        Source = drawAction.Owner
                                    }),
                                DiceFormula.FromText(
                                    thorns.FinalValue.ToString(),
                                    rune.FullName),
                                enemy,
                                CheckResult.Success,
                                choice.Kind);
                        }
                    }),
                new RuneInvocationProperties(
                        // Normally I'd cross out "engulfed", but leaving it in means I might get reports
                        // if it's later implemented and this rune ceases to affect engulfing foes.
                        "Any creature that has the armor's wearer engulfed, grabbed, restrained, or swallowed whole takes 8d4 damage of the rune's chosen type with a basic Reflex save. On a failure, it also releases the armor's wearer.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(8, 2, 2, level);
                            return
                                $"Any creature that has the armor's wearer engulfed, grabbed, restrained, or swallowed whole takes {S.HeightenedVariable(finalValue, baseValue)}d4 damage of the rune's chosen type with a basic Reflex save. On a failure, it also releases the armor's wearer.";
                        })
                    .WithAdditionalRequirement((a, d) =>
                        d.HasEffect(qf =>
                            qf.Id is QEffectId.Grappled && qf.Source != null)
                        || d.Space.Surface?.Swallower != null
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Not grappled or swallowed whole by a creature"))
                    .WithDealsDamage((rune, level) =>
                        ($"{rune.CalculateHeightening(8, 2, 2, level).FinalValue}d4", DamageKind.Untyped))
                    .WithDefense(Defense.Reflex)
                    .WithHideTooltip()
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (invokedRune.Tag is not DamageKind chosenKind)
                        {
                            invokeAction.RevertRequested = true;
                            return null;
                        }
                        
                        List<Creature> targets = [];
                        
                        // Grapplers
                        targets.AddRange(
                            effectTarget
                                .QEffects
                                .Where(qf => qf.Id is QEffectId.Grappled)
                                .Select(qf => qf.Source)
                                .WhereNotNull());
                        
                        // Swallower
                        if (effectTarget.Space.Surface?.Swallower is {} swallower)
                            targets.Add(swallower);
                        
                        // Remove duplicates
                        targets.RemoveDuplicates();
                        
                        // Must not be immune
                        targets.RemoveAll(cr =>
                            CommonRuneRules.IsImmuneToThisInvocation(cr, invokedRune.Rune));

                        // Revert if no valid targets
                        if (targets.Count == 0)
                        {
                            invokeAction.RevertRequested = true;
                            return null;
                        }

                        // Save and damage each grappler-and-such
                        foreach (Creature enemy in targets)
                        {
                            CombatAction invocationDetails = CombatAction.CreateSimple(
                                    invokeAction.Owner,
                                    invokeAction.Name,
                                    [..invokedRune.Traits])
                                .WithIllustration(invokedRune.Rune.Illustration)
                                .WithTag(invokedRune)
                                .WithSavingThrow(new SavingThrow(
                                    invokedRune.Rune.InvocationProperties.Defense!.Value,
                                    invokeAction.Owner.ClassDC(Trait.Runesmith)));
                            
                            CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                                enemy,
                                invocationDetails,
                                invocationDetails.SavingThrow!);
                            
                            await CommonSpellEffects.DealBasicDamage(
                                invocationDetails,
                                invokeAction.Owner,
                                enemy,
                                result,
                                invokedRune.Rune.InvocationProperties.GetKindedDamage!.Invoke(invokedRune.Rune, invokedRune.Source!.Level).DiceExpression,
                                chosenKind);
                            
                            if (result < CheckResult.Success)
                            {
                                effectTarget.RemoveAllQEffects(qf =>
                                    qf.Source == enemy
                                    && qf.Id is QEffectId.Grappled or QEffectId.RemovedFromPlay);
                                // Log the release event
                                enemy.Overhead(
                                    "*release*", Color.White,
                                    $"{enemy.Name} releases {effectTarget.Name}.");
                            }
                        }
                        
                        // The effect of the invocation is on the grapplers-and-such, not the bearer.
                        return targets;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_KOJASTRI),
                [Trait.Arcane])
            .WithLevelText("+2", "The resistance and damage granted by insulation both increase by 1, and the damage dealt by the invocation increases by 2d4.")
            .ToFeat();
        
        // Oraloq, Rune of Inarticulateness
        // DOC: The rune-bearer does not take this effect into consideration for their actions.
        // DOC: This affects any Cast a Spell, not just non-Subtle ones.
        yield return new Rune(
                RuneId.Oraloq,
                "This hard-to-read rune makes language difficult for the unfortunate rune-bearer.",
                new RuneDrawProperties(
                        "drawn on a creature",
                        drawnOnCreature: true)
                    .WithEnemyRequirement()
                    .WithCreatureRequirement((a, d) =>
                        d.IsImmuneTo(Trait.Mental)
                            ? Usability.NotUsableOnThisCreature("Immune to mental")
                            : Usability.Usable),
                new RunePassiveProperties(
                        "When the rune-bearer attempts an ability with the linguistic trait or attempts to Cast a spell, they must succeed on a DC 5 flat check, or the action is lost.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction, rune,
                            "You must succeed on a DC 5 flat check each time you attempt an ability with the linguistic trait or Cast a Spell, or the action is lost.")
                        {
                            FizzleOutgoingActions = async (qfThis, action, strBuild) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                if (DrawnRune.ShouldNotPerformPassiveEffect(drThis))
                                    return false;
                                
                                bool isSpell = action.CountsAsSpell;
                                bool isLing = action.HasTrait(Trait.Linguistic);
                                if (!isSpell && !isLing)
                                    return false;

                                var result = Checks.RollFlatCheckWithRoll(5);
                                strBuild.AppendLine($"{(isSpell ? "Cast a spell" : isLing ? "Use linguistic action" : "???")} under the effects of {drThis}: {result.Item2}");
                                
                                return result.Item1 < CheckResult.Success;
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "The rune-bearer must attempt a Will save as the rune puts a strong mental block on the part of their mind that interprets language. If the rune-bearer fails their save, they take 20 mental damage (or double damage on a critical failure) the first time they take a linguistic action or Cast a Spell. The effect ends once the damage is taken or at the end of the rune-bearer's next turn, whichever comes first.",
                        (rune, level) =>
                        {
                            (int baseValue, _, int finalValue) = rune.CalculateHeightening(20, 2, 5, level);
                            return
                                $"The rune-bearer must attempt a Will save as the rune puts a strong mental block on the part of their mind that interprets language. If the rune-bearer fails their save, they take {S.HeightenedVariable(finalValue, baseValue)} mental damage (or double damage on a critical failure) the first time they take a linguistic action or Cast a Spell. The effect ends once the damage is taken or at the end of the rune-bearer's next turn, whichever comes first.";
                        })
                    .WithAdditionalRequirement((a, d) =>
                        d.IsImmuneTo(Trait.Mental)
                            ? Usability.NotUsableOnThisCreature("Immune to mental")
                            : Usability.Usable)
                    .WithDealsDamage((rune, level) => (
                        rune.CalculateHeightening(
                                20, 2, 5, level)
                            .FinalValue.ToString(),
                        DamageKind.Mental))
                    .WithDefense(Defense.Will)
                    .WithSoundBeforeInvocation(ModData.SfxNames.INVOKED_ORALOQ)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction, invokedRune, effectTarget,
                            async (innerInvoke, target, result) =>
                            {
                                if (result > CheckResult.Failure)
                                    return;
                                
                                int damageAmount = invokedRune.Rune.CalculateHeightening(
                                        20, 2, 5, invokedRune.Source!.Level)
                                    .FinalValue;
                        
                                effectTarget.AddQEffect(invokedRune
                                    .NewInvocationEffect(
                                        $"The first time you use a linguistic action or Cast a Spell, you take {(result is CheckResult.CriticalFailure ? (damageAmount * 2).WithColor("Red").WithTag("b") : damageAmount)} mental damage.",
                                        ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                                        qf =>
                                        {
                                            qf.Source = invokedRune.Source!;
                                            qf.CannotExpireThisTurn = true;
                                            qf.SourceAction = invokeAction;
                                            qf.Supereffect = invokedRune;
                                            qf.AfterYouTakeAction = async (qfThis, action) =>
                                            {
                                                if (!(action.CountsAsSpell
                                                     || action.HasTrait(Trait.Linguistic))
                                                    || qfThis.UsedUpPermanently)
                                                    return;

                                                Sfxs.Play(ModData.SfxNames.INVOKED_ORALOQ);
                                                
                                                // Deal damage
                                                await CommonSpellEffects.DealBasicDamage(
                                                    invokeAction, invokeAction.Owner, target,
                                                    // DoS: Fumble/Fail/Pass
                                                    result == CheckResult.Success
                                                        ? CheckResult.CriticalSuccess
                                                        : result,
                                                    DiceFormula.FromText(
                                                        damageAmount.ToString(),
                                                        invokedRune.Rune.FullName),
                                                    DamageKind.Mental);
                                                
                                                // Prevent it from triggering again immediately.
                                                qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                                qfThis.UsedUpPermanently = true;
                                            };
                                        }));
                            },
                            true);
                        
                        return effectTarget;
                    }),
                [Trait.Mental])
            .WithLevelText("+2", "The damage of the invocation increases by 5.")
            .ToFeat();
        
        // Piteregrin, Rune of Transposition
        yield return new Rune(
                RuneId.Piteregrin,
                "The slanted lines and odd curls of this rune give the impression that it is trying to escape.",
                new RuneDrawProperties(
                        "drawn onto a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(true),
                new RunePassiveProperties(
                        "The rune-bearer can use a single action to swap places with any other willing rune-bearer within 15 feet. {i}(The other rune-bearer can be bearing any rune.){/i} Each creature must have room to appear in its new location or the effect is disrupted. This action has the concentrate and teleportation traits.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return new DrawnRune(
                            drawAction, rune,
                            "You can swap places with another willing rune-bearer as an {icon:Action} action.")
                        {
                            ProvideContextualAction = qfThis =>
                            {
                                CombatAction transposition = PiteregrinTranspose(qfThis.Owner);
                                
                                return new ActionPossibility(transposition)
                                    .WithPossibilityGroup(Constants.POSSIBILITY_GROUP_CONTEXTUAL_EFFECT_ACTIONS);
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "(teleportation) The rune grows to an enormous size, swallowing up the rune-bearer and each Large or smaller creature adjacent to them, and then immediately spits them back out. Choose a square within a 15-foot emanation from the rune-bearer's original position for each swallowed creature to reappear. A Large creature doesn’t necessarily need to reappear completely within the emanation, as long as one of its squares is. An unwilling creature avoids being teleported if it succeeds at a Will save against your class DC.",
                        null)
                    .WithAffectsArea()
                    .WithDefense(Defense.Will)
                    .WithHideTooltipForAllies()
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 3f; // 15 feet

                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize)
                                .WithIncludeOnlyIf((_, cr) =>
                                    cr == invokedRune.Owner
                                    || cr.Space.Size <= Size.Large),
                            true,
                            ModData.SfxNames.INVOKED_PITEREGRIN,
                            overrideTargets: overrideTargets,
                            finalAdjustments: subAction =>
                            {
                                if (overrideTargets?.ChosenCreatures.Count is < 2)
                                    return;
                                
                                // This design is based on Ledria. See that rune for code comments
                                // documenting why this is designed this way.
                                
                                subAction.WithEffectOnChosenTargets(async (self, chosen) =>
                                {
                                    // Collect allies and failed saves
                                    List<Creature> teleportees = [];
                                    foreach (Creature cr in chosen.ChosenCreatures)
                                    {
                                        if (!cr.FriendOf(self)
                                            && await CommonRuneRules.SaveAgainstInvocation(
                                                invokeAction, invokedRune, cr)
                                            >= CheckResult.Success)
                                            continue;
                                        teleportees.Add(cr);
                                        cr.Space.PreviousTile = cr.Space.TopLeftTile;
                                        cr.Space.RemoveSelfFromAllTiles();
                                        cr.Space.OnTheMap = true;
                                    }

                                    List<Tile> emanationTiles = invokeAction.Owner.Battle
                                        .Map.AllTiles
                                        .Where(tile =>
                                            tile.DistanceTo(invokedRune.Owner) <= 3f)
                                        .ToList();

                                    // Store this to avoid errors with complex diacritic
                                    // interactions.
                                    invokedRune.Tag = emanationTiles;
                                    
                                    // Teleport everyone
                                    List<Creature> reacted = [];
                                    await CommonQuestions.OfferReactionsAsync(new ReactionRequestStyle(
                                        self.Battle,
                                        () => $"{invokeAction.Owner.ToColoredName()} has invoked {invokedRune}, teleporting each creature (willing, or failed their Will save) within 15 feet of {invokedRune.Owner.ToColoredName()} to any space within that range.",
                                        true,
                                        invokeAction.Owner,
                                        // In order to give player control over the enemy creature reaction order,
                                        // this tracks who has already reacted and manually removes them from
                                        // the list. Each action has the player as the controller instead of
                                        // the reacting creature.
                                        () => teleportees
                                            .Except(reacted)
                                            .Select(cr =>
                                            {
                                                ReactionOption reactOpt = ReactionOption.CreateCustom(
                                                        $"{invokedRune.Rune.Illustration.IllustrationAsIconString} Transpose",
                                                        $"Teleport to a new location within range of {invokedRune.Owner.ToColoredName()}.",
                                                        cr.Illustration,
                                                        self,
                                                        async () =>
                                                        {
                                                            await PiteregrinTransposeWithinEmanation(invokeAction.Owner,
                                                                invokedRune, cr, emanationTiles);
                                                            reacted.Add(cr);
                                                        })
                                                    .WithTraits(Trait.ExecuteEvenIfCasterCannotTakeActions)
                                                    .WithDoesNotCountAsYourTriggerResponse();
                                                reactOpt.MouseOverStatblock = new CombatActionStatblock(
                                                    invokeAction, 0, null, null);
                                                return reactOpt;
                                            })
                                            .ToList()
                                    ));
                                    
                                    // Make the emanation immune
                                    chosen.ChosenCreatures.ForEach(ally =>
                                        ally.AddQEffect(CommonRuneRules.ImmunityToInvocation(invokedRune.Rune)));
                                });
                            });
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        List<Tile> emanationTiles = 
                            invokedRune.Tag as List<Tile>
                            ?? invokeAction.Owner.Battle
                                .Map.AllTiles
                                .Where(tile =>
                                    tile.DistanceTo(invokedRune.Owner) <= 3f)
                                .ToList();
                        invokedRune.Tag = emanationTiles;
                        await PiteregrinTransposeWithinEmanation(
                            invokeAction.Owner, invokedRune,
                            effectTarget, emanationTiles);
                        return effectTarget;
                    }))
            .ToFeat()
            .WithRulesBlockForCombatAction(PiteregrinTranspose);

        // Trolistri, Rune of Forlorn Sorrow
        // Faction alignment is treated as "enemies to the runesmith", regardless of the rune-bearer's faction.
        // DOC: Changed to a 10-foot size, but always works.
        // BUG: The difficult terrain effect doesn't interact with immunity to emotion or mental effects. No known way to fix this at this time.
        yield return new Rune(
                RuneId.Trolistri,
                "This rune calls to mind the beauty hidden in sorrow. While this rune is beautiful, sorrow is best admired from a distance, discouraging approach.",
                new RuneDrawProperties(
                        "drawn onto a creature",
                        drawnOnCreature: true)
                    .WithAllyRequirement(true),
                new RunePassiveProperties(
                        /*"Your enemies within 20 feet of the rune-bearer treat all spaces between them and the rune-bearer as {r}difficult terrain{/r}."*/
                        "Your enemies treat all spaces within 10 feet of the rune-bearer as {r}difficult terrain{/r}.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        const int radius = 2; // Changed to 10 feet instead of 20 feet.
                        
                        DrawnRune trolistriPassive = new DrawnRune(
                            drawAction,
                            rune,
                            $"Spaces within {radius * 5} feet of you are difficult terrain to {drawAction.Owner.ToColoredBoldedName()}'s enemies. This is an emotion and mental effect.")
                        {
                            SpawnsAura = qfThis =>
                                new MagicCircleAuraAnimation(IllustrationName.BaneCircle, Color.DarkBlue, (radius + 0.18f)),
                        };
                        
                        Zone trolistriZone = Zone.Spawn(trolistriPassive, ZoneAttachment.Aura(radius))
                            .With(zone =>
                            {
                                zone.TileEffectCreator = tile =>
                                    new TileQEffect(tile)
                                    {
                                        Illustration = IllustrationName.IllusoryRubble,
                                        // Terrain is always difficult, instead of directional and individual
                                        StateCheck = tqfThis =>
                                            tqfThis.Owner.DifficultTerrainToComputerControlledCreatures = true,
                                    };
                            });

                        return trolistriPassive;
                    }),
                new RuneInvocationProperties(
                        "Sorrow blots out the capacity for any other action. Each enemy in a 15-foot emanation around the rune-bearer must succeed at a Will saving throw or be slowed 1 for 1 round as it spends the first action of its next turn sobbing (slowed 2 on a critical failure). Regardless of the outcome, the creature is then temporarily immune to the invocation of {i}trolistri, rune of forlorn sorrow{/i} for the rest of the encounter.",
                        null)
                    .WithAdditionalRequirement((a, d) =>
                        a.Battle.AllCreatures.Any(cr =>
                            cr.EnemyOf(a)
                            && cr.DistanceTo(d) <= 3
                            && !cr.IsImmuneTo(Trait.Mental)
                            && !cr.IsImmuneTo(Trait.Emotion))
                            ? Usability.Usable
                            : Usability.NotUsable("No enemies within 15 feet that aren't immune to emotion and mental effects"))
                    .WithDefense(Defense.Will)
                    .WithHideTooltipForAllies()
                    .WithAffectsArea()
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const float emanationSize = 4f; // 20 feet
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.Emanation((int)emanationSize)
                                .WithIncludeOnlyIf((tar, cr) =>
                                    cr.EnemyOf(invokeAction.Owner)
                                    && !cr.IsImmuneTo(Trait.Mental)
                                    && !cr.IsImmuneTo(Trait.Emotion)),
                            true,
                            ModData.SfxNames.INVOKED_TROLISTRI,
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        await CommonRuneRules.SaveAgainstInvocation(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            async (innerInvoke, innerTarget, result) =>
                            {
                                if (result < CheckResult.Success)
                                {
                                    innerTarget.AddQEffect(QEffect
                                        .Slowed(result == CheckResult.CriticalFailure ? 2 : 1)
                                        .WithExpirationInOneRound(innerTarget.Battle));
                                }
                            });
                        
                        return effectTarget;
                    }),
                [Trait.Arcane, Trait.Emotion, Trait.Mental])
            .ToFeat();
        
        // Ulgatus, Rune of Restraint
        yield return new Rune(
                RuneId.Ulgatus,
                "A faction of constructed beings developed this rune to avoid the weaknesses of flesh.",
                new RuneDrawProperties(
                        "drawn on a creature wearing armor",
                        drawnOnCreature: true)
                    .WithAllyRequirement()
                    .WithWearsArmorRequirement(),
                new RunePassiveProperties(
                        "When the creature wearing the armor would gain or increase the value of their {r}drained{/r}, {r}enfeebled{/r}, or {r}sickened{/r} condition, the rune attempts to prevent the imposition. The bearer attempts a DC 15 flat check. On a success, it doesn’t gain or increase the value of that condition, but the rune goes temporarily dormant for 10 minutes. While dormant, the rune can't protect the bearer from conditions but can be invoked.",
                        (rune, level) =>
                        {
                            return $"When the creature wearing the armor would gain or increase the value of their {{r}}drained{{/r}}, {{r}}enfeebled{{/r}}, or {{r}}sickened{{/r}} condition, the rune attempts to prevent the imposition. The bearer attempts a DC {S.HeightenedVariable((level >= 17 ? 10 : 15), 15)} flat check. On a success, it doesn’t gain or increase the value of that condition, but the rune goes temporarily dormant for 10 minutes. While dormant, the rune can't protect the bearer from conditions but can be invoked.";
                        })
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int dc = drawAction.Owner.Level >= 17 ? 10 : 15;
                        return new DrawnRune(
                            drawAction, rune,
                            $"When you gain or inrcease the value of your drained, enfeebled, or sickened condition, make a DC {dc} flat check. On a success, don't gain or increase its value, and then this rune goes dormant.")
                        {
                            Tag = new Dictionary<QEffectId, int>{
                                [QEffectId.Drained] = 0,
                                [QEffectId.Enfeebled] = 0,
                                [QEffectId.Sickened] = 0,
                            },
                            StateCheckLayer = 1,
                            StateCheck = qfThis =>
                            {
                                var conditions = (qfThis.Tag as Dictionary<QEffectId, int>)!;
                                foreach ((QEffectId condition, int value) in conditions)
                                    conditions[condition] = qfThis.Owner.FindQEffect(condition)?.Value ?? 0;
                            },
                            YouAcquireQEffect = (qfThis, qfNew) =>
                            {
                                DrawnRune drThis = (qfThis as DrawnRune)!;
                                var conditions = (qfThis.Tag as Dictionary<QEffectId, int>)!;
                                if (drThis.UsedUpPermanently
                                    || DrawnRune.ShouldNotPerformPassiveEffect(drThis)
                                    || !conditions.TryGetValue(qfNew.Id, out int oldValue)
                                    || oldValue >= qfNew.Value
                                    || qfNew.ExpiresAt is ExpirationCondition.Ephemeral
                                        or ExpirationCondition.EphemeralAtEndOfImmediateAction
                                        or ExpirationCondition.Immediately)
                                    return qfNew;

                                var result = Checks.RollFlatCheckWithRoll(dc);
                                drThis.UsedUpPermanently = true;
                                drThis.YouAcquireQEffect = null;
                                drThis.DescriptionGenerator = _ => "{Red}{b}Used up.{/b}{/Red} You can still invoke this rune as normal.";
                                drThis.UpdateDescription();
                                
                                qfThis.Owner.Battle.Log(
                                    $"{qfThis.Owner.Name} makes a flat check to avoid {(oldValue == 0 ? "gaining the" : "increasing their")} {qfNew.Id.ToStringOrTechnical().ToLower()} condition: {result.Item2}",
                                    drThis.Rune.FullName,
                                    CommonRuneRules.StatBlocks.CreateRuneDescription(
                                        drThis.Rune, drThis.Source!.Level),
                                    new Traits(rune.Traits.ToArray()));

                                // Add as normal if you fail the check.
                                if (result.Item1 < CheckResult.Success)
                                    return qfNew;
                                
                                // If you don't have the effect because it was removed just
                                // before adding the new one, such as with CumulativeCondition(),
                                // then add the new effect with the lower old value.
                                if (!qfThis.Owner.HasEffect(qfNew.Id) && oldValue > 0)
                                {
                                    qfNew.Value = oldValue;
                                    return qfNew;
                                }
                                else
                                    return null;
                            },
                            // Development code
                            /*ProvideContextualAction = qfThis =>
                                new ActionPossibility(
                                    new CombatAction(
                                            qfThis.Owner,
                                            IllustrationName.YellowWarning,
                                            "Test Cumulative Enfeebled",
                                            [],
                                            "Cumulatively add {r}enfeebled 1{/r} (up to 4).",
                                            Target.Any())
                                        .WithActionCost(0)
                                        .WithEffectOnEachTarget(async (_, _, creature, _) =>
                                            CommonSpellEffects.CumulativeEnfeebled(creature, 1))),*/
                        };
                    }),
                new RuneInvocationProperties(
                        "The rune causes the armor to grow around the creature wearing it, fortifying any vulnerable spots. For the rest of the encounter, the wearer gains resistance 10 to critical hits.",
                        (rune, level) =>
                        {
                            int resist = level >= 17 ? 20 : 10;
                            return
                                $"The rune causes the armor to grow around the creature wearing it, fortifying any vulnerable spots. For the rest of the encounter, the wearer gains resistance {S.HeightenedVariable(resist, 10)} to critical hits.";
                        })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        int resist = invokedRune.Source!.Level >= 17 ? 20 : 10;
                        
                        effectTarget.AddQEffect(invokedRune
                            .NewInvocationEffect(
                                $"You have resistance {resist} to critical hits.",
                                ExpirationCondition.Never,
                                qf =>
                                {
                                    qf.StateCheck = qfThis =>
                                    {
                                        qfThis.Owner.WeaknessAndResistance.AddSpecialResistance(
                                            SpecialResistance.Create(
                                                "critical hits",
                                                (_, _, dEvent) =>
                                                    dEvent.CheckResult == CheckResult.CriticalSuccess,
                                                resist));
                                    };
                                }));
                        
                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_ULGATUS))
            .WithLevelText("17th", "The DC of the flat check decreases to 10 and the resistance to critical hits increases to 20.")
            .ToFeat();
        
        // Yudici, Rune of Remonstrance
        yield return new Rune(
                RuneId.Yudici,
                "This majestic rune grants the shield's wielder the conviction to aid their allies.",
                new RuneDrawProperties(
                        "drawn on a shield",
                        drawnOnItem: true)
                    .WithAllyRequirement()
                    .WithHoldsItemRequirement(
                        item => item.HasTrait(Trait.Shield),
                        "no shield"),
                new RunePassiveProperties(
                        "While a shield bearing this rune is raised, its bearer emits an aura in a 10-foot emanation that grants their allies a +1 circumstance bonus to AC.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return subTarget is Item itemTarget
                            ? [MakeYudiciPassive(itemTarget)!]
                            : await CommonRuneRules.ChooseAnItemToDrawOn(
                                drawAction,
                                drawAction.Owner,
                                target,
                                item => item.HasTrait(Trait.Shield),
                                MakeYudiciPassive,
                                "Choose a shield to radiate a 10-foot aura that grants your allies a +1 circumstance bonus to AC it's while raised.",
                                rune);
                        
                        DrawnRune? MakeYudiciPassive(Item? targetItem)
                        {
                            if (targetItem is null)
                                return null;
                            
                            DrawnRune drawnYudici = new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, item) => $"While your {item.Illustration.IllustrationAsIconString} {item.Name.WithColor("Blue")} is raised, your allies in a 10-foot emanation gain a +1 circumstance bonus to AC.",
                                targetItem)
                            {
                                SpawnsAura = qfThis =>
                                {
                                    DrawnRune dr = (qfThis as DrawnRune)!;
                                    if (dr.Disabled || dr.DrawnOn is null)
                                        return null;
                                    return new MagicCircleAuraAnimation(
                                        IllustrationName.BlessCircle,
                                        Color.White,
                                        qfThis.Owner.QEffects.Any(qf =>
                                            qf.Id is QEffectId.RaisingAShield
                                            && qf.Tag == dr.DrawnOn)
                                            ? 2.15f
                                            : 0f);
                                },
                                StateCheck = qfThis =>
                                {
                                    DrawnRune dr = (qfThis as DrawnRune)!;
                                    if (dr.Disabled || dr.DrawnOn is null)
                                        return;

                                    if (!qfThis.Owner.QEffects.Any(qf =>
                                            qf.Id == QEffectId.RaisingAShield
                                            && qf.Tag == dr.DrawnOn))
                                    {
                                        qfThis.AssociatedAura?.MoveTo(0f);
                                        return;
                                    }
                                    
                                    qfThis.AssociatedAura?.MoveTo(2.2f);
                                    
                                    foreach (Creature ally in qfThis.Owner.Battle.AllCreatures
                                                 .Where(qfThis.Owner.FriendOfAndNotSelf)
                                                 .Where(ally => ally.DistanceTo(qfThis.Owner) <= 2)
                                                 .ToList())
                                    {
                                        ally.AddQEffect(new QEffect(
                                            "Yudici's Majesty",
                                            $"You have a +1 circumstance bonus to AC while in the aura emanating from {qfThis.Owner.ToColoredBoldedName()}'s {rune.Illustration.IllustrationAsIconString} {rune.FullName.WithColor("Blue")}.",
                                            ExpirationCondition.Ephemeral,
                                            qfThis.Owner,
                                            IllustrationName.Shield)
                                        {
                                            BonusToDefenses = (_, _, _) =>
                                                new Bonus(1, BonusType.Circumstance,
                                                    $"{rune.WordName} (raised shield)")
                                        });
                                    }
                                },
                                BonusToDefenses = (qfThis,_,_) =>
                                {
                                    DrawnRune dr = (qfThis as DrawnRune)!;
                                    if (dr.Disabled || dr.DrawnOn is null)
                                        return null;

                                    // Must be raising a shield.
                                    if (!qfThis.Owner.QEffects.Any(qf =>
                                            qf.Id == QEffectId.RaisingAShield
                                            && qf.Tag == dr.DrawnOn))
                                        return null;

                                    return new Bonus(1, BonusType.Status, "Holtrik (raised shield)");
                                },
                            };

                            return drawnYudici;
                        }
                    }),
                new RuneInvocationProperties(
                        "All of the bearer's allies in a 10-foot emanation gain a +1 status bonus to attack rolls and skill checks for 1 round.",
                        null)
                    .WithChosenTargeting(async (invokeAction, invokedRune, overrideTargets) =>
                    {
                        const int emanationSize = 2;
                        return await CommonRuneRules.GetInnerInvokeTargets(
                            invokeAction,
                            invokedRune,
                            Target.AlliesOnlyEmanation(emanationSize),
                            false,
                            SfxName.HolyWard,
                            overrideTargets: overrideTargets);
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        effectTarget.AddQEffect(invokedRune.NewInvocationEffect(
                            "You have a +1 status bonus to attack rolls and skill checks.",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                            qf =>
                            {
                                qf.Key = "YudiciInvocation";
                                qf.WithExpirationInOneRound(invokeAction.Owner);
                                qf.BonusToAttackRolls = (_, action, _) =>
                                    action.HasTrait(Trait.Attack)
                                    || action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is not null
                                        ? new Bonus(1, BonusType.Status, invokedRune.Rune.FullName)
                                        : null;
                            }));

                        return effectTarget;
                    }),
                [Trait.Aura, Trait.Divine])
            .ToFeat();

        #endregion

        #region 13th-Level
        
        // Eck-, Diacritic Rune of Phantasma
        yield return new Rune(
                RuneId.Eck,
                "This diacritic frames a base rune with almost invisible lines that turn the rune's effects against the bearer's soul.",
                RuneDrawProperties.Diacritic("that deals damage"),
                RunePassiveProperties.Diacritic(
                        "When the base rune is invoked, the damage it deals becomes spirit damage, and each of your enemies adjacent to the rune-bearer takes 1d8 persistent spirit damage.",
                        heightening: (rune, level) =>
                        {
                            int numDice = level >= 17 ? 2 : 1;
                            return
                                $"The damage dealt by the base rune becomes spirit damage. When the base rune is invoked, each of your enemies adjacent to the rune-bearer takes {S.HeightenedVariable(numDice, 1)}d8 persistent spirit damage.";
                        })
                    .WithDiacriticCreator(
                        (drawAction, drOnto) =>
                            drOnto.Rune.InvocationProperties.DealsDamage,
                        (drawAction, rune, drBase) =>
                        {
                            int numDice = drawAction.Owner.Level >= 17 ? 2 : 1;
                            return new DrawnRune(
                                drawAction, rune,
                                (drThis, drOnto) =>
                                    $"When {drOnto} is invoked, the damage it deals becomes spirit damage, and {drThis.Source!.ToColoredName()}'s enemies adjacent to this bearer take {numDice}d8 persistent spirit damage.",
                                drBase)
                            {
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;

                                    invokeAction.Owner.AddQEffect(new QEffect()
                                    {
                                        Name = "[ECK- DAMAGE CHANGER]",
                                        ConvertAllOutgoingDamageToDamageKind = DamageSpirit.Spirit
                                    });
                                },
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;
                                    
                                    List<Creature> targets = drThis.Owner.Battle
                                        .AllCreatures
                                        .Where(cr =>
                                            drThis.Source!.EnemyOf(cr)
                                            && cr.IsAdjacentTo(drThis.Owner)
                                            && !CommonRuneRules.IsImmuneToThisInvocation(cr, drThis))
                                        .ToList();
                                    
                                    Sfxs.Play(ModData.SfxNames.INVOKED_ECK);
                                    await CommonRuneRules.PlayGroupInvocationSplashAnimation(
                                        drThis.Owner.Battle,
                                        targets
                                            .Select(cr => (cr, drThis.Rune.Illustration))
                                            .ToList());
                                    
                                    foreach (Creature enemy in targets)
                                    {
                                        await CommonSpellEffects.DealAttackRollPersistentDamage(
                                            invokeAction, enemy,
                                            CheckResult.Success,
                                            $"{numDice}d8",
                                            DamageSpirit.Spirit);
                                        enemy.AddQEffect(CommonRuneRules.ImmunityToInvocation(drThis.Rune));
                                    }
                                    
                                    invokeAction.Owner.RemoveAllQEffects(qf =>
                                        qf.Name == "[ECK- DAMAGE CHANGER]");
                                }
                            };
                        },
                        (drawAction, rune) => "Choose a rune whose invocation damage type will become spirit."),
                null,
                [Trait.Diacritic])
            .WithLevelText("17th", "The persistent spirit damage increases by 1d8.")
            .ToFeat();
        
        // Inth-, Diacritic Rune of Corruption
        // DOC: Since the invocation refers to the base rune's target, this causes you to choose one creature affected by the invocation to set on fire.
        yield return new Rune(
                RuneId.Inth,
                "This set of angular accents around the base rune channels the essence of fiendish corruption.",
                RuneDrawProperties.Diacritic()
                    .WithCreatureRequirement((a, d) =>
                        PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.UnrestrictedTrace)
                        || (InthIsHoly(d) && a.FriendOf(d))
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Not a holy enemy")),
                RunePassiveProperties.Diacritic(
                        "The base rune gains the unholy trait, as does any damage it deals. If applied to a holy creature, that creature is {r}enfeebled 2{/r} for as long as it bears the rune."
                        /*$"The base rune gains the unholy trait, as does any damage{ModData.Tooltips.InfoInthDamage} it deals. If applied to a holy creature, that creature is {{r}}enfeebled 2{{/r}} for as long as it bears the rune."*/)
                    .WithDiacriticCreator(
                        null,
                        (drawAction, rune, drBase) =>
                        {
                            int numDice = rune.CalculateHeightening(2, 2, 2, drawAction.Owner.Level).FinalValue;
                            
                            DrawnRune drawnInth = new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"{drOnto} gains the unholy trait, as does any damage it deals.{(InthIsHoly(drOnto.Owner) ? "\n\n{Blue}Holy:{/Blue} You are enfeebled 2." : null)}",
                                drBase)
                            {
                                DamageDealtByRunesGainTrait = UnholyTrait.Unholy,
                                // Using this instead of StateCheck since diacritics use them.
                                StateCheckWithVisibleChanges = async qfThis =>
                                {
                                    DrawnRune drThis = (qfThis as DrawnRune)!;
                                    if (drThis.Disabled)
                                        return;
                                    
                                    if (drThis.DrawnOn is DrawnRune drOnto
                                        && !drOnto.Traits.Contains(UnholyTrait.Unholy))
                                        drOnto.Traits.Add(UnholyTrait.Unholy);
                                    
                                    if (InthIsHoly(qfThis.Owner))
                                        qfThis.Owner.AddQEffect(QEffect
                                            .Enfeebled(2)
                                            .WithExpirationEphemeral()
                                            .With(qf =>
                                            {
                                                qf.Supereffect = drThis;
                                            }));
                                },
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn
                                        || drInvoked.Owner.IsImmuneTo(Trait.Evil)
                                        || drInvoked.Owner.IsImmuneTo(UnholyTrait.Unholy)
                                        || invokeAction.Tag is not RuneActionTag tag
                                        || tag.AffectedCreatures is null)
                                        return;

                                    if (await invokeAction.Owner.Battle.AskToChooseACreature(
                                            invokeAction.Owner,
                                            tag.AffectedCreatures,
                                            drThis.Rune.Illustration,
                                            $"Choose a creature affected by {drInvoked} to take {numDice}d4 persistent fire damage from {drThis}.",
                                            cr => $"Choose {cr.ToColoredName()} to take {numDice}d4 persistent fire damage.",
                                            "Confirm no persistent fire damage")
                                        is not { } target)
                                    {
                                        return;
                                    }

                                    // Play sound and animation
                                    Sfxs.Play(ModData.SfxNames.INVOKED_INTH);
                                    await CommonRuneRules.PlayInvocationSplashAnimation(
                                        target, drThis.Rune.Illustration);
                                    
                                    QEffect pFire = QEffect.PersistentDamage(
                                            $"{numDice}d4",
                                            /*GetBestDamageKind(target, [DamageKind.Fire, DamageKind.Evil])*/
                                            DamageKind.Fire)
                                        .WithTrait(Trait.Fire)
                                        .WithTrait(UnholyTrait.Unholy)
                                        .WithSourceAction(invokeAction)
                                        .With(qf =>
                                        {
                                            qf.Supereffect = drThis;
                                        });
                                    
                                    target.AddQEffect(pFire);
                                },
                                AdjustStrikeAction = (qfThis, strike) =>
                                {
                                    DrawnRune drThis = (qfThis as DrawnRune)!;
                                    if (drThis.Disabled
                                        || drThis.DrawnOn is not DrawnRune drOnto)
                                        return;
                                    // Mark the splash damage dealt by Marssyl as unholy.
                                    if (drOnto.Rune.Id is RuneId.Marssyl
                                        && strike.Item is not null
                                        && drOnto.DrawnOn == strike.Item)
                                        strike.WithExtraTrait(UnholyTrait.Unholy);
                                }
                            };
                            /*drawnInth.AddGrantingOfTechnical(
                                _ => true,
                                qfTech =>
                                {
                                    qfTech.StateCheckLayer = 1;
                                    qfTech.StateCheck += qfThis =>
                                    {
                                        if (!InthIsHoly(qfThis.Owner))
                                            return;
                                        CombatAction unholy = CombatAction.CreateSimple(qfThis.Owner, "Unholy", UnholyTrait.Unholy);
                                        KindedDamage kd = new KindedDamage(DiceFormula.FromText("1"), DamageKind.Evil);
                                        DamageEvent dEvent = new DamageEvent(unholy, qfThis.Owner, CheckResult.Failure, [kd]);
                                        int amount = qfThis.Owner.WeaknessAndResistance.Weaknesses.Max(weak => weak.Matches(unholy, kd, dEvent) ? weak.Value : 0);
                                        if (amount < 1)
                                            return;
                                        qfThis.Owner.WeaknessAndResistance.Weaknesses.Add(
                                            new SpecialResistance(
                                                "unholy (inth-)",
                                                (action, dk) =>
                                                {
                                                    if (action?.ActionId is not ActionId.PersistentDamage
                                                        || action.Origin?.QEffect?.Id is not QEffectId.PersistentDamage
                                                        || action.Origin?.QEffect?.Traits.Contains(UnholyTrait.Unholy) != true)
                                                        return false;
                                                    return true;
                                                },
                                                // The unholy weakness amount is equal to their existing evil weakness.
                                                amount,
                                                null));
                                    };
                                });*/
                            return drawnInth;
                        },
                        (drawAction, rune) => "Choose a rune to gain the unholy trait, making any damage it deals unholy and leaving persistent fire damage on the rune-bearer when invoked."),
                new RuneInvocationProperties(
                    // Not sure if it's RAW, but I think unholy is inherited from the above, and is included for clarity.
                    "(fire) When the base rune is invoked, it burns away in unholy black fire that lingers on its target. In addition to the base rune's normal effect, the target takes 2d4 persistent fire damage.",
                    (rune, level) =>
                    {
                        (int baseValue, _, int finalValue) = rune.CalculateHeightening(2, 2, 2, level);
                        return
                            $"(fire) When the base rune is invoked, it burns away in unholy black fire that lingers on its target. In addition to the base rune's normal effect, the target takes {S.HeightenedVariable(finalValue, baseValue)}d4 persistent fire damage.";
                    }),
                [Trait.Diacritic, Trait.Divine, UnholyTrait.Unholy])
            .WithLevelText("+2", "The persistent fire damage increases by 2d4.")
            .ToFeat();
        
        // TODO: Nesh-, Diacritic Rune of Contingency
        // Contingency is complicated as hell. Do this one last.
        /*yield return new Rune(
                RuneId.Nesh,
                "The wavy lines of this diacritic seem to stretch the base rune into a wider area.",
                RuneDrawProperties.Diacritic(null, true),
                RunePassiveProperties.Diacritic(
                        $$"When you draw this rune, choose a trigger that when met will invoke the base rune, using the same restrictions as for the trigger of a Ready action. When the trigger is met, you can {{ModData.FeatNames.InvokeRune.ToLink("Invoke the base Rune") as a {icon:Reaction} reaction; you can’t invoke any additional runes with that reaction.",
                        null,
                        true)
                    .WithDiacriticCreator(
                        null,
                        (drawAction, rune, drBase) =>
                            new DrawnRune(
                                drawAction, rune,
                                (drThis, drOnto) => $"\n\nCan only be invoked once per encounter.\n",
                                drBase)
                            {
                                
                            },
                        _,),
                null,
                [Trait.Diacritic])
            .ToFeat();*/
        
        // Sar-, Diacritic Rune of Righteousness
        // DOC: Since the invocation refers to the base rune's target, this causes you to choose one creature affected by the invocation to drain.
        yield return new Rune(
                RuneId.Sar,
                "Clean arcs of gold faintly glow on the base rune, evoking celestial order and serenity.",
                RuneDrawProperties.Diacritic()
                    .WithCreatureRequirement((a, d) =>
                        PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.UnrestrictedTrace)
                        || (SarIsUnholy(d) && a.FriendOf(d))
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Not an unholy enemy")),
                RunePassiveProperties.Diacritic(
                        "The base rune gains the holy trait, as does any damage it deals. If applied to an unholy creature, that creature is {r}enfeebled 2{/r} for as long as it bears the rune.")
                    .WithDiacriticCreator(
                        null,
                        (drawAction, rune, drBase) =>
                        {
                            DrawnRune drawnSar = new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"{drOnto} gains the holy trait, as does any damage it deals.{(SarIsUnholy(drOnto.Owner) ? "\n\n{Blue}Unholy:{/Blue} You are enfeebled 2." : null)}",
                                drBase)
                            {
                                DamageDealtByRunesGainTrait = HolyTrait.Holy,
                                // Using this instead of StateCheck since diacritics use them.
                                StateCheckWithVisibleChanges = async qfThis =>
                                {
                                    DrawnRune drThis = (qfThis as DrawnRune)!;
                                    if (drThis.Disabled)
                                        return;
                                    
                                    if (drThis.DrawnOn is DrawnRune drOnto
                                        && !drOnto.Traits.Contains(HolyTrait.Holy))
                                        drOnto.Traits.Add(HolyTrait.Holy);
                                    
                                    if (SarIsUnholy(qfThis.Owner))
                                        qfThis.Owner.AddQEffect(QEffect
                                            .Enfeebled(2)
                                            .WithExpirationEphemeral()
                                            .With(qf =>
                                            {
                                                qf.Supereffect = drThis;
                                            }));
                                },
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn
                                        || drInvoked.Owner.IsImmuneTo(Trait.Good)
                                        || drInvoked.Owner.IsImmuneTo(HolyTrait.Holy)
                                        || invokeAction.Tag is not RuneActionTag tag
                                        || tag.AffectedCreatures is null)
                                        return;

                                    if (await invokeAction.Owner.Battle.AskToChooseACreature(
                                            invokeAction.Owner,
                                            tag.AffectedCreatures,
                                            drThis.Rune.Illustration,
                                            $"Choose a creature affected by {drInvoked} to become {{r}}drained 1{{/r}} from {drThis}.",
                                            cr => $"Choose {cr.ToColoredName()} to become {{r}}drained 1{{/r}}.",
                                            "Confirm no drained 1")
                                        is not { } target)
                                    {
                                        return;
                                    }

                                    // Play sound and animation
                                    Sfxs.Play(ModData.SfxNames.INVOKED_SAR);
                                    await CommonRuneRules.PlayInvocationSplashAnimation(
                                        target, drThis.Rune.Illustration);
                                    
                                    QEffect drained = QEffect.Drained(1)
                                        .WithTrait(Trait.Fire)
                                        .WithTrait(HolyTrait.Holy)
                                        .WithSourceAction(invokeAction)
                                        .With(qf =>
                                        {
                                            qf.Supereffect = drThis;
                                        });
                                    
                                    target.AddQEffect(drained);
                                },
                                AdjustStrikeAction = (qfThis, strike) =>
                                {
                                    DrawnRune drThis = (qfThis as DrawnRune)!;
                                    if (drThis.Disabled
                                        || drThis.DrawnOn is not DrawnRune drOnto)
                                        return;
                                    // Mark the splash damage dealt by Marssyl as holy.
                                    if (drOnto.Rune.Id is RuneId.Marssyl
                                        && strike.Item is not null
                                        && drOnto.DrawnOn == strike.Item)
                                        strike.WithExtraTrait(HolyTrait.Holy);
                                }
                            };
                            /*drawnInth.AddGrantingOfTechnical(
                                _ => true,
                                qfTech =>
                                {
                                    qfTech.StateCheckLayer = 1;
                                    qfTech.StateCheck += qfThis =>
                                    {
                                        if (!InthIsHoly(qfThis.Owner))
                                            return;
                                        CombatAction unholy = CombatAction.CreateSimple(qfThis.Owner, "Unholy", HolyTrait.Holy);
                                        KindedDamage kd = new KindedDamage(DiceFormula.FromText("1"), DamageKind.Evil);
                                        DamageEvent dEvent = new DamageEvent(unholy, qfThis.Owner, CheckResult.Failure, [kd]);
                                        int amount = qfThis.Owner.WeaknessAndResistance.Weaknesses.Max(weak => weak.Matches(unholy, kd, dEvent) ? weak.Value : 0);
                                        if (amount < 1)
                                            return;
                                        qfThis.Owner.WeaknessAndResistance.Weaknesses.Add(
                                            new SpecialResistance(
                                                "unholy (inth-)",
                                                (action, dk) =>
                                                {
                                                    if (action?.ActionId is not ActionId.PersistentDamage
                                                        || action.Origin?.QEffect?.Id is not QEffectId.PersistentDamage
                                                        || action.Origin?.QEffect?.Traits.Contains(HolyTrait.Holy) != true)
                                                        return false;
                                                    return true;
                                                },
                                                // The unholy weakness amount is equal to their existing evil weakness.
                                                amount,
                                                null));
                                    };
                                });*/
                            return drawnSar;
                        },
                        (drawAction, rune) => "Choose a rune to gain the holy trait, making any damage it deals holy and making the rune-bearer {r}drained 1{/r} when invoked."),
                new RuneInvocationProperties(
                        "When the base rune is invoked, it shines with holy light that harshly judges the target for any of its sins. In addition to the base rune's normal effect, the target is {r}drained 1{/r}.",
                        null),
                [Trait.Diacritic, Trait.Divine, HolyTrait.Holy])
            .ToFeat();

        #endregion

        #region 17th-Level

        // Aiuen, the Elf-Gate Key
        // This rune almost certainly needs either a new name and/or new description.
        // "This masterful circular rune is a key to the aiudara network of elf gates that stretch across the stars."
        
        // Ochygholl, the Poisoned Star
        // This rune almost certainly needs either a new name and/or new description.
        // "This awe-inspiring rune of pointed curves is rumored to have been part of the magic that alghollthus used to call down the meteoroids of the Earthfall cataclysm."
        
        // Rovan, Seal of the Dead Vault
        // This rune almost certainly needs either a new name and/or new description.
        // "This mighty, jagged rune is just one of several that keeps the Destroyer sealed in his prison, deep in Golarion’s core."
        
        // Xinsala, the Well of Virtues
        // This rune almost certainly needs either a new name and/or new description.
        // "This complex rune comprises many different ligatures from the ancient Thassilonian runes representing the seven virtues of rule, before they were corrupted by the runelords."

        #endregion
    }

    #region FeatlikeChoice (unused implementation)

    /// <summary>
    /// Gets a list of runes to choose from at or up to this level.
    /// </summary>
    /// <param name="level">The level of rune to select from.</param>
    /// <param name="exactlyThisLevel">If true, the rune must be exactly the level parameter. If false, the rune must be up to that level or lower.</param>
    public static FeatlikeChoice[] GetRuneChoices(
        [Range(1, 17)]
        int level,
        bool exactlyThisLevel = false)
    {
        Func<Rune,bool> filter = exactlyThisLevel
            ? rune => rune.BaseLevel == level
            : rune => rune.BaseLevel <= level;
        return GetRuneChoices(filter);
    }
    
    /// <summary>
    /// Gets a list of runes to choose from.
    /// </summary>
    /// <param name="runeFilter">Whether a specific rune is allowed (true) or not (false).</param>
    public static FeatlikeChoice[] GetRuneChoices(
        Func<Rune,bool>? runeFilter = null)
    {
        List<Rune> allowedRunes;
        if (runeFilter is not null)
            allowedRunes = All
                .Where(runeFilter)
                .ToList();
        else
            allowedRunes = All
                .ToList();
        
        FeatlikeChoice[] choices = allowedRunes
            .Select(RuneToChoice)
            .ToArray();

        return choices;
    }

    public static FeatlikeChoice RuneToChoice(Rune rune)
    {
        return new FeatlikeChoice(
            $"RunesmithRuneSelection.{rune.WordName}",
            rune.FullName)
        {
            Illustration = rune.Illustration,
            TextCreator = () =>
                CommonRuneRules.StatBlocks.CreateRuneDescription(rune, null),
            Apply = values =>
                RunicRepertoireTag.GetRepertoire(values)
                    ?.AddRune(values.CurrentLevel, rune)
        };
    }

    #endregion

    public static Feat ToFeat(this Rune rune, Action<Feat>? adjustFeat = null)
    {
        Feat runeFeat = new Feat(
                ModManager.RegisterFeatName(
                    // Remove the '-' for backwards compatibility with changes
                    ModData.ID_PREPEND+"Rune."+rune.WordName.TrimEnd('-'),
                    rune.FullName),
                rune.GetFlavorText(null, false),
                CommonRuneRules.StatBlocks.CreateRuneDescription(
                    rune, null,
                    withFlavorText: false),
                rune.Traits
                    .Except([Trait.Runesmith])
                    .ToList(),
                null)
            .WithIllustration(rune.Illustration)
            .WithLevel(rune.BaseLevel)
            .WithTag(rune)
            .WithPrerequisite(
                values => RunicRepertoireTag.GetRepertoire(values) is not null,
                "You must have a runic repertoire.")
            .WithOnSheet(values =>
                RunicRepertoireTag.GetRepertoire(values)
                    ?.AddRune(values.CurrentLevel, rune));
        
        runeFeat.FeatGroup = rune.BaseLevel switch
        {
            17 => ModData.FeatGroups.Level17Rune,
            13 => ModData.FeatGroups.Level13Rune,
            9 => ModData.FeatGroups.Level9Rune,
            5 => ModData.FeatGroups.Level5Rune,
            1 => ModData.FeatGroups.Level1Rune,
            _ => null
        };

        adjustFeat?.Invoke(runeFeat);

        return runeFeat;
    }

    public static Rune? GetRune(RuneId runeId)
    {
        return All.FirstOrDefault(rune => rune.Id == runeId);
    }

    public static async Task<bool> LedriaStepTowardsRuneBearer(DrawnRune invokedRune, Creature ally)
    {
        if (CommonRuneRules.IsImmuneToThisInvocation(ally, invokedRune.Rune))
            return false;
        
        Sfxs.Play(SfxName.Footsteps);
                        
        Creature? active = ally.Battle.ActiveCreature;
        ally.Battle.ActiveCreature = ally;
                        
        bool result = await ally.StrideOrStepAdvancedAsync(
            $"Choose where to Step as part of invoking {invokedRune}, or right-click to cancel. You must end this movement closer to {invokedRune.Owner.ToColoredName()}.",
            allowStep: true, allowStride: false,
            passText: "Don't Step",
            // Allows equidistance in order to avoid situations where you're KIND OF
            // moving closer but there's an obstacle in the way, or it's a very
            // shallow angle of being slightly closer that isn't visible on a grid.
            permissibleTarget: tile => tile.DistanceTo(invokedRune.Owner) <= ally.DistanceTo(invokedRune.Owner));
                        
        ally.Battle.ActiveCreature = active;

        if (result)
            ally.AddQEffect(CommonRuneRules.ImmunityToInvocation(invokedRune.Rune));

        return result;
    }

    public static DamageKind? TiGetBaseRuneDamage(DrawnRune drawnOnto)
    {
        return
            drawnOnto.Rune.InvocationProperties.DealsDamage
                ? drawnOnto.Tag as DamageKind? ?? drawnOnto.Rune.InvocationProperties.GetKindedDamage?.Invoke(drawnOnto.Rune, drawnOnto.Source!.Level).Kind
                : null;
    }

    public static string[] TiDamageKindsToButtons(DamageKind[] kinds)
    {
        return kinds
            // Don't reorder, preserve index information
            /*.OrderByDescending(kind => kind.ToStringOrTechnical())*/
            .Select(kind =>
            {
                Illustration icon = kind switch
                {
                    DamageKind.Acid => IllustrationName.AcidSplash,
                    DamageKind.Cold => IllustrationName.RayOfFrost,
                    DamageKind.Electricity => IllustrationName.ElectricArc,
                    DamageKind.Fire => IllustrationName.ProduceFlame,
                    _ => IllustrationName.YellowWarning,
                };
                string name = kind.ToStringOrTechnical().Capitalize().WithColor(kind.DamageKindToColor());
                return icon.IllustrationAsIconString + " " + name;
            })
            .ToArray();
    }

    public static async Task<(DamageKind Kind, Trait? Trait)?> TiAskDamageChoice(Creature runesmith, Rune rune, string question, DamageKind[] kinds)
    {
        ChoiceButtonOption choice = await runesmith.AskForChoiceAmongButtons(
            rune.Illustration,
            $$"""
              {b}{{rune.Illustration.IllustrationAsIconString}}{{rune.FullName}}{/b}
              {{question}}
              """,
            TiDamageKindsToButtons(kinds));

        if (choice.Index > kinds.Length - 1)
            return null;

        DamageKind chosenKind = kinds[choice.Index];
        Trait? chosenTrait = KojastriKindToTrait(chosenKind);

        return (chosenKind, chosenTrait);
    }

    public static Trait? KojastriKindToTrait(DamageKind kind)
    {
        switch (kind)
        {
            case DamageKind.Acid:
                return Trait.Acid;
            case DamageKind.Cold:
                return Trait.Cold;
            case DamageKind.Electricity:
                return Trait.Electricity;
            case DamageKind.Fire:
                return Trait.Fire;
            case DamageKind.Mental:
                return Trait.Mental;
            case DamageKind.Negative:
                return Trait.Negative;
            case DamageKind.Positive:
                return Trait.Positive;
            case DamageKind.Poison:
                return Trait.Poison;
            case DamageKind.Sonic:
                return Trait.Sonic;
            default:
                return null;
        }
    }

    public static HashSet<Creature>? JurrozWhoDamagedMe(Creature me)
    {
        return (me.Battle
            .Pseudocreature?
            .FindQEffect(ModData.QEffectIds.JurrozDamageTracker)?
            .Tag as Dictionary<Creature, HashSet<Creature>>)?
            .TryGetValue(me, out HashSet<Creature>? damagedMe) == true
            ? damagedMe.Where(cr => cr.Alive && cr.EnemyOf(me)).ToHashSet()
            : null;
    }

    public static CombatAction PiteregrinTranspose(Creature owner)
    {
        CombatAction transposition = new CombatAction(
                owner,
                RuneId.Piteregrin.ToIcon(),
                "Piteregrin's Transposition",
                [ModData.ModTrait, Trait.Concentrate, Trait.Teleportation],
                """
                {b}Range{/b} 15 feet
                {b}Target{/b} 1 willing rune-bearer (any rune)

                Swap places with the target creature. Each of you must have room to fit in your new locations, or else the action fails.
                """,
                Target.RangedCreature(3)
                    .WithAdditionalConditionOnTargetCreature(
                        new FriendCreatureTargetingRequirement())
                    .WithAdditionalConditionOnTargetCreature(
                        new TargetIsARuneBearer(true))
                    .WithAdditionalConditionOnTargetCreature((a, d) =>
                        d.Space.Tiles.Any(tile =>
                            tile.AccommodatesSize(a.Space.SizeInSquares))
                        && a.Space.Tiles.Any(tile =>
                            tile.AccommodatesSize(d.Space.SizeInSquares))
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Spaces don't fit")))
            .WithEffectOnChosenTargets(async (action, caster, targets) =>
            {
                if (targets.ChosenCreature is not {} chosenCr)
                {
                    action.RevertRequested = true;
                    return;
                }

                TBattle battle = caster.Battle;
                int sizeDiff = Math.Abs(caster.Space.SizeInSquares - chosenCr.Space.SizeInSquares);

                List<Tile> space1 =
                    (caster.Space.SizeInSquares > chosenCr.Space.SizeInSquares
                        ? caster.Space.Tiles
                        : battle.Map.TilesInRectangle(
                            caster.Space.TopLeftTile.X - sizeDiff,
                            caster.Space.TopLeftTile.Y - sizeDiff,
                            caster.Space.TopLeftTile.X + (caster.Space.SizeInSquares - 1),
                            caster.Space.TopLeftTile.Y + (caster.Space.SizeInSquares - 1)))
                    .ToList();
                caster.Space.PreviousTile = caster.Space.TopLeftTile;
                caster.Space.RemoveSelfFromAllTiles();
                caster.Space.OnTheMap = true;
                
                List<Tile> space2 =
                    (chosenCr.Space.SizeInSquares > caster.Space.SizeInSquares
                        ? chosenCr.Space.Tiles
                        : battle.Map.TilesInRectangle(
                            chosenCr.Space.TopLeftTile.X - sizeDiff,
                            chosenCr.Space.TopLeftTile.Y - sizeDiff,
                            chosenCr.Space.TopLeftTile.X + (chosenCr.Space.SizeInSquares - 1),
                            chosenCr.Space.TopLeftTile.Y + (chosenCr.Space.SizeInSquares - 1)))
                    .ToList();
                chosenCr.Space.PreviousTile = chosenCr.Space.TopLeftTile;
                chosenCr.Space.RemoveSelfFromAllTiles();
                chosenCr.Space.OnTheMap = true;

                List<Tile> targets1 = space1
                    .Where(tile => tile.IsTrulyGenuinelyFreeTo(chosenCr))
                    .ToList();
                List<Tile> targets2 = space2
                    .Where(tile => tile.IsTrulyGenuinelyFreeTo(caster))
                    .ToList();
                
                if (targets1.Count < 1 || targets2.Count < 1)
                {
                    caster.TranslateTo(caster.Space.PreviousTile);
                    chosenCr.TranslateTo(chosenCr.Space.PreviousTile);
                    action.RevertRequested = true;
                    return;
                }
                
                Creature? anchored =
                    caster.HasEffect(QEffectId.DimensionalAnchor) ? caster
                    : chosenCr.HasEffect(QEffectId.DimensionalAnchor) ? chosenCr
                    : null;
                if (anchored is not null)
                {
                    caster.TranslateTo(caster.Space.PreviousTile);
                    chosenCr.TranslateTo(chosenCr.Space.PreviousTile);
                    anchored.AnimationData.ColorBlink(Color.Red);
                    anchored.Overhead("*teleport failure*", Color.Red, anchored + " failed to teleport because it's dimensionally locked.");
                    return;
                }

                Tile? target2 = targets2.Count == 1
                    ? targets2[0]
                    : await battle.AskToChooseATile(
                        caster, targets2, action.Illustration,
                        $"Choose where to teleport within {chosenCr.ToColoredName()}'s space, or right-click to cancel.",
                        "Teleport to this space.",
                        true, true,
                        caster);

                if (target2 is null)
                {
                    caster.TranslateTo(caster.Space.PreviousTile);
                    chosenCr.TranslateTo(chosenCr.Space.PreviousTile);
                    action.RevertRequested = true;
                    return;
                }

                Tile? target1 = targets1.Count == 1
                    ? targets1[0]
                    : await battle.AskToChooseATile(
                        chosenCr, targets1, action.Illustration,
                        $"Choose where to teleport within {caster.ToColoredName()}'s space, or right-click to cancel.",
                        "Teleport to this space.",
                        true, true,
                        chosenCr);

                if (target1 is null)
                {
                    caster.TranslateTo(caster.Space.PreviousTile);
                    chosenCr.TranslateTo(chosenCr.Space.PreviousTile);
                    action.RevertRequested = true;
                    return;
                }
                
                Sfxs.Play(SfxName.PhaseBolt);
                
                caster.TranslateTo(target2.GetShuntoffTileIfNecessary(caster));
                caster.AnimationData.ColorBlink(Color.LightBlue);
                caster.Battle.SmartCenterCreatureAlways(caster);
                
                chosenCr.TranslateTo(target1.GetShuntoffTileIfNecessary(chosenCr));
                chosenCr.AnimationData.ColorBlink(Color.LightBlue);
                chosenCr.Battle.SmartCenterCreatureAlways(chosenCr);
            });
        
        return transposition;
    }

    public static async Task<Tile?> PiteregrinTransposeWithinEmanation(Creature runesmith, DrawnRune invokedRune, Creature teleportee, List<Tile> emanationTiles)
    {
        if (CommonRuneRules.IsImmuneToThisInvocation(teleportee, invokedRune.Rune))
        {
            Tile previous = teleportee.Space.PreviousTile ?? teleportee.Space.TopLeftTile;
            teleportee.TranslateTo(previous);
            return null;
        }
                        
        Creature? active = teleportee.Battle.ActiveCreature;
        teleportee.Battle.ActiveCreature = teleportee;

        Tile? result = await runesmith.Battle.AskToChooseATile(
            runesmith,
            emanationTiles
                .SelectMany(tile =>
                {
                    int offset = teleportee.Space.SizeInSquares - 1;
                    return runesmith.Battle.Map.TilesInRectangle(
                            tile.X - offset,
                            tile.Y - offset,
                            tile.X/* + offset*/,
                            tile.Y/* + offset*/);
                })
                .Where(tile => tile.IsTrulyGenuinelyFreeTo(teleportee))
                .Distinct()
                .ToList(),
            invokedRune.Rune.Illustration,
            $"Choose where to teleport to as part of invoking {invokedRune}, or right-click to cancel.",
            "Teleport to this space.",
            true, true,
            teleportee);
                        
        teleportee.Battle.ActiveCreature = active;

        if (result is not null)
        {
            teleportee.AddQEffect(CommonRuneRules.ImmunityToInvocation(invokedRune.Rune));
        
            Sfxs.Play(ModData.SfxNames.INVOKED_PITEREGRIN_EACH);
                
            teleportee.TranslateTo(result.GetShuntoffTileIfNecessary(teleportee));
            teleportee.AnimationData.ColorBlink(Color.LightBlue);
            teleportee.Battle.SmartCenterCreatureAlways(teleportee);
        }
        else
        {
            Tile previous = teleportee.Space.PreviousTile ?? teleportee.Space.TopLeftTile;
            teleportee.TranslateTo(previous);
        }

        return result;
    }
    
    public static bool InthIsHoly(Creature cr)
    {
        return cr.HasTrait(HolyTrait.Holy);
        /*return cr.HasTrait(Trait.Good)
               && cr.WeaknessAndResistance.Weaknesses.Any(weak =>
                   weak.DamageKind is DamageKind.Evil);*/
    }

    public static bool SarIsUnholy(Creature cr)
    {
        return cr.HasTrait(UnholyTrait.Unholy);
    }

    public static DamageKind GetBestDamageKind(Creature target, DamageKind[] kinds)
    {
        var cache1 = target.WeaknessAndResistance.AppliedWeaknesses.ToList();
        var cache2 = target.WeaknessAndResistance.DamageKindsWithAppliedResistance.ToDictionary();
        target.WeaknessAndResistance.AppliedWeaknesses.Clear();
        target.WeaknessAndResistance.DamageKindsWithAppliedResistance.Clear();
        DamageKind kind = target.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe(kinds);
        target.WeaknessAndResistance.AppliedWeaknesses.AddRange(cache1);
        cache2.ForEach(kvp => target.WeaknessAndResistance.DamageKindsWithAppliedResistance.Add(kvp.Key, kvp.Value));
        return kind;
    }
}