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
using Dawnsbury.Core.CharacterBuilder.Selections;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;
using Microsoft.Xna.Framework;

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
                RuneId.Atryl, 1,
                IllustrationName.FlamingRunestone,
                "This rune is often placed on a stone in a hearth, its power ensuring the stone remains warm through the night.",
                new RuneDrawProperties("drawn on a creature") /*or object*/
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
                            const int baseNumDice = 1;
                            int bonusNumDice = (level - rune.BaseLevel) / 2; // +1d8 every 2 levels
                            int finalNumDice = baseNumDice + bonusNumDice;
                            string heightenedVar = S.HeightenedVariable(finalNumDice, baseNumDice);
                            return $"The bearer takes {heightenedVar}d8 fire damage, with a basic Fortitude save. On a critical failure, it's also dazzled for 1 round.";
                        })
                    .WithDealsDamage()
                    .WithDefense(Defense.Fortitude)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                            effectTarget,
                            invokeAction,
                            invokedRune.Rune.InvocationProperties.Defense!.Value,
                            invokeAction.Owner.ClassDC(ModData.Traits.Runesmith));
                        
                        // 1d8 per rank
                        int numDice = 1 + (invokeAction.Owner.Level / 2);
                        
                        await CommonSpellEffects.DealBasicDamage(
                            invokeAction, invokeAction.Owner, effectTarget, result,
                            numDice + "d8",
                            DamageKind.Fire);
                        
                        if (result is CheckResult.CriticalFailure)
                        {
                            effectTarget.AddQEffect(QEffect.Dazzled()
                                .WithExpirationOneRoundOrRestOfTheEncounter(invokeAction.Owner, false)
                                .With(qf =>
                                {
                                    qf.Source = invokedRune.Source;
                                    qf.SourceAction = invokeAction;
                                }));
                        }

                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_ATRYL),
                [Trait.Fire, Trait.Primal])
            .WithLevelText(
                "The reduction in fire resistance increases by 1, and the damage of the invocation increases by 1d8.",
                "+2")
            .ToFeat();
        
        // TODO: Baruiel, Rune of Hold's Bravery
        
        // TODO: Camonica, Rune of Perplexity

        // Esvadir, Rune of Whetstones
        yield return new Rune(
                RuneId.Esvadir, 1,
                IllustrationName.WoundingRunestone,
                "This cuspate rune, when placed on a blade, ensures it won't go dull.",
                new RuneDrawProperties(
                        "drawn on a weapon or unarmed attack that deals piercing or slashing damage")
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
                    .WithAdditionalRequirement((a, d) =>
                        d.EnemyOf(a)
                        && (d.Neighbours.Creatures.Any(cr => cr.EnemyOf(a))
                            || RunicRepertoireTag.GetRepertoire(a)?.IsKnown(RuneId.En, a.Level) == true)
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("No enemies in range of invocation")
                    ),
                new RunePassiveProperties(
                        "Strikes with the weapon or unarmed attack deal an extra 2 persistent bleed damage per weapon damage die.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return await CommonRuneRules.ChooseAnItemToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is Item itemTarget
                                ? item => item == itemTarget
                                : item =>
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
                                            || drawnItem.WeaponProperties is null)
                                            return;
                                        
                                        // Determine weapon damage dice count
                                        int bleedAmount = 2 * drawnItem.WeaponProperties.DamageDieCount;

                                        await CommonSpellEffects.DealAttackRollPersistentDamage(
                                            dEvent.CombatAction,
                                            dEvent.TargetCreature,
                                            dEvent.CombatAction.CheckResult,
                                            bleedAmount.ToString(),
                                            DamageKind.Bleed);
                                    },
                                };

                            return esvadirPassive;
                        }
                    }),
                new RuneInvocationProperties(
                        "A blast of cutting energy is released outward from the rune, dealing 1d8 slashing damage to a creature adjacent to the rune-bearer, with a basic Reflex save.",
                        (thisRune, level) =>
                        {
                            const int baseValue = 1;
                            int bonusValue = (level - thisRune.BaseLevel) / 2;
                            int numDice = baseValue + bonusValue;
                            string heightenedVar = S.HeightenedVariable(numDice, baseValue);
                            return $"The essence of sharpness is released outwards from the rune, dealing {heightenedVar}d8 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.";
                        })
                    .WithDealsDamage()
                    .WithHideTooltip()
                    .WithDefense(Defense.Reflex)
                    .WithAdditionalRequirement((runesmith, runeBearer) =>
                    {
                        if (runeBearer.Space.GetNeighbours()
                            .All(tile => tile.PrimaryOccupant?.EnemyOf(runesmith) != true))
                            return Usability.NotUsableOnThisCreature("No enemy adjacent to the target");
                        return Usability.Usable;
                    })
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        return await CommonRuneRules.ExecuteInnerInvokeAction(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            Target.RangedCreature(1)
                                // Enemy of invoker, not action-taker
                                .WithAdditionalConditionOnTargetCreature((a, d) =>
                                    d.EnemyOf(invokeAction.Owner)
                                        ? Usability.Usable
                                        : Usability.CommonReasons.TargetIsNotEnemy),
                            false,
                            ModData.SfxNames.INVOKED_ESVADIR,
                            true,
                            async (innerInvoke, runeBearer, innerTarget, result) =>
                            {
                                const int baseValue = 1;
                                int bonusValue = (invokeAction.Owner.Level - invokedRune.Rune.BaseLevel) / 2;
                                int numDice = baseValue + bonusValue;
                                await CommonSpellEffects.DealBasicDamage(
                                    innerInvoke,
                                    invokeAction.Owner,
                                    innerTarget,
                                    result,
                                    numDice + "d8",
                                    DamageKind.Slashing);
                            },
                            // The effect target is normally the rune bearer, so you
                            // go through the target-choosing routine as normal.
                            // If a different target is passed by a more specific ability,
                            // then this means the rune is being invoked onto a specific target
                            // instead of being invoked normally.
                            effectTarget == invokedRune.Owner
                                ? null
                                : ChosenTargets.CreateSingleTarget(effectTarget),
                            innerInvoke =>
                            {
                                // Invokes from rune-bearer to adjacent target,
                                // So the rune-bearer is the action owner.
                                innerInvoke.Owner = invokedRune.Owner;
                                
                                // When invoking onto a specific target,
                                // remove the extra animations.
                                if (effectTarget != invokedRune.Owner)
                                {
                                    innerInvoke.ProjectileKind = ProjectileKind.None;
                                    innerInvoke.ProjectileCount = 0;
                                }
                            });
                    }))
            .WithLevelText(
                "The damage of the invocation increases by 1d8.",
                "+2")
            .ToFeat();

        // Holtrik, Rune of Dwarven Ramparts
        yield return new Rune(
                RuneId.Holtrik, 1,
                IllustrationName.ArmorPotencyRunestone,
                "Similarity in the Dwarven words for “wall” and “shield” ensure that this angular rune, once used to shore up tunnels, can apply equally well in the heat of battle.",
                new RuneDrawProperties("drawn on a shield")
                    .WithAllyRequirement()
                    .WithHoldsItemRequirement(
                        item => item.HasTrait(Trait.Shield),
                        "no shield"),
                new RunePassiveProperties(
                        "A creature that has the shield raised gains a +1 status bonus to AC.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return await CommonRuneRules.ChooseAnItemToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is Item itemTarget
                                ? item => item == itemTarget
                                : item => item.HasTrait(Trait.Shield),
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
                        d.QEffects.Any(qf =>
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

        // TODO: Ledria, Rune of Appeal
        
        // TODO: Lyskel, Rune of Frost
        
        // Marssyl, Rune of Impact
        yield return new Rune(
                RuneId.Marssyl, 1,
                IllustrationName.ThunderingRunestone,
                "This rune magnifies force many times over as it passes through the rune's concentric rings.",
                new RuneDrawProperties("drawn on a weapon or unarmed attack that deals bludgeoning damage")
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
                        return await CommonRuneRules.ChooseAnItemToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is Item itemTarget
                                ? item => item == itemTarget
                                : item =>
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
                                            $"Splash damage ({rune.Name})");

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
                                    $"Splash damage ({rune.Name})");

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
                                            CombatAction.CreateSimple(action.Owner, $"Invoked {invokedRune.Rune.Name}"),
                                            invokedRune.Rune.InvocationProperties.Defense!.Value,
                                            invokeAction.Owner.ClassDC(ModData.Traits.Runesmith));
                                        
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
                RuneId.Oljinex, 1,
                IllustrationName.FearsomeRunestone,
                "This rune resembles a broken arrow.",
                new RuneDrawProperties("drawn on a shield")
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
                        
                        return await CommonRuneRules.ChooseAnItemToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is Item itemTarget
                                ? item => item == itemTarget
                                : item => item.HasTrait(Trait.Shield),
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
                                            ? new Bonus(bonusHardness, BonusType.Status, rune.Name)
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
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        const float emanationSize = 1f; // 5 feet

                        return await CommonRuneRules.ExecuteInnerInvokeAction(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            Target.Emanation((int)emanationSize)
                                .WithIncludeOnlyIf((_, cr) => IsValidTarget(cr))
                                // This is mildly inefficient, but necessary to limit bad attempts to invoke.
                                .WithAdditionalRequirementOnCaster(self =>
                                    effectTarget.Neighbours.Creatures.Any(IsValidTarget)
                                        ? Usability.Usable
                                        : Usability.NotUsable("No valid creatures in range")),
                            false,
                            ModData.SfxNames.INVOKED_OLJINEX,
                            false,
                            async (invokeOljinex, _, invokeTarget, _) =>
                            {
                                QEffect invokedOljinex = invokedRune.NewInvocationEffect(
                                    $"You must first succeed at a Perception check against {invokeAction.Owner.ToColoredBoldedName()+"'s"} class DC before you can willingly move further away from {invokedRune.Owner.ToColoredBoldedName()}. On a failure, the action is lost. On a success, you disbelieve the illusion.",
                                    ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                    invokeQf =>
                                    {
                                        invokeQf.Source = invokeAction.Owner;
                                        invokeQf.SourceAction = invokeOljinex;
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
                                                    Checks.FlatDC(qfThis.Source!.ClassDC(ModData.Traits.Runesmith))))
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

                                invokeTarget.AddQEffect(invokedOljinex);
                            },
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
                                                        qf.Name == $"Invoked {invokedRune.Rune.Name}"
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
                            });
                        
                        bool IsValidTarget(Creature cr) =>
                            cr != invokedRune.Owner
                            && !CommonRuneRules.IsImmuneToThisInvocation(cr, invokedRune.Rune)
                            && !(cr.IsImmuneTo(Trait.Illusion)
                                 || cr.IsImmuneTo(Trait.Mental)
                                 || cr.IsImmuneTo(Trait.Visual));
                    }),
                additionalTraits: [Trait.Arcane])
            .WithLevelText(
                "The status bonus to Hardness increases by 1.",
                "+4")
            .ToFeat();

        // Pluuna, Rune of Illumination
        yield return new Rune(
                RuneId.Pluuna, 1,
                IllustrationName.HolyRunestone,
                "While many runes emit a faint glow, illumination is the focus of this simple rune.",
                new RuneDrawProperties("drawn on a creature" /*or armor*/),
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
                                                ? new Bonus(-1, BonusType.Item, rune.Name)
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
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        const float emanationSize = 4f; // 20 feet

                        return await CommonRuneRules.ExecuteInnerInvokeAction(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            Target.Emanation((int)emanationSize),
                            true,
                            ModData.SfxNames.INVOKED_PLUUNA,
                            true,
                            async (_,_, invokeTarget, result) =>
                            {
                                if (result <= CheckResult.Failure)
                                    invokeTarget.AddQEffect(QEffect.Dazzled()
                                        .WithExpirationAtStartOfSourcesTurn(invokeAction.Owner, 1));
                            },
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

                                    effectTarget.AddQEffect(invokedPluuna);
                                });
                            });
                    }),
                [Trait.Light, Trait.Rebalanced])
            .ToFeat();

        // Ranshu, Rune of Thunder
        yield return new Rune(
                RuneId.Ranshu, 1,
                IllustrationName.ShockRunestone,
                "This vertical rune is often carved on tall towers to draw lightning and shield the buildings below it.",
                new RuneDrawProperties("drawn on a creature" /*or object*/)
                    .WithEnemyRequirement(),
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
                                            [..rune.Traits])
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
                            int numDice = 1 + ((level - rune.BaseLevel) / 2);
                            string heightenedVar = S.HeightenedVariable(numDice, 2);
                            return $"The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes {heightenedVar}d8 electricity damage with a basic Fortitude save.";
                        })
                    .WithDefense(Defense.Fortitude)
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        int numDice = 1 + ((invokeAction.Owner.Level - invokedRune.Rune.BaseLevel) / 2);
                        DiceFormula invocationDamage = DiceFormula.FromText($"{numDice}d8", "Ranshu, Rune of Thunder");
                        
                        CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                            effectTarget,
                            invokeAction,
                            invokedRune.Rune.InvocationProperties.Defense!.Value,
                            invokeAction.Owner.ClassDC(ModData.Traits.Runesmith));
                        
                        await CommonSpellEffects.DealBasicDamage(invokeAction, invokeAction.Owner, effectTarget, result, invocationDamage, DamageKind.Electricity);
                        
                        return effectTarget;
                    })
                    .WithSoundAfterInvocation(ModData.SfxNames.INVOKED_RANSHU),
                [Trait.Electricity, Trait.Primal])
            .WithLevelText(
                "The damage from the bolt of static increases by 1, and the damage of the invocation increases by 1d8.",
                "+2")
            .ToFeat();

        // TODO: Rehgog, Rune of Bestial Might
        
        // TODO: Sertum, Rune of Prepardness
        
        // TODO: Thullax, Rune of Corrosion
        
        // TODO: Tilus, Rune of Vocabulary

        // Zohk, Rune of Homecoming
        yield return new Rune(
                RuneId.Zohk, 1,
                IllustrationName.ReturningRunestone,
                "This circular mark allows travelers to always find their way home.",
                new RuneDrawProperties("drawn on a creature")
                    .WithAdditionalRequirement((a, _) =>
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
                                invokeAction.Owner.ClassDC(ModData.Traits.Runesmith));
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

        // TODO: Av-, Diacritic Rune of Succession
        
        // En-, Diacritic Rune of Expansion
        yield return new Rune(
                RuneId.En, 5,
                IllustrationName.RunestoneEnergyAdaptive, //IllustrationName.UnderwaterRunestone,
                "This diacritic surrounds a rune with outward-facing arrows to magnify and direct power outward.",
                new RuneDrawProperties("drawn on a rune that deals damage")
                    .WithDiacriticTargetingRequirements(dr =>
                    {
                        if (!dr.Rune.InvocationProperties.DealsDamage)
                            return "Base rune doesn't deal damage";
                        if (dr.Rune.InvocationProperties.AffectsArea)
                            return "Base rune already affects an area";
                        return null;
                    }),
                new RunePassiveProperties(
                        "When the base rune is invoked, the rune-bearer is affected by it as usual, and each other creature in a 15-foot emanation around the rune-bearer also takes the damage and other effects from the base rune (and can attempt a saving throw if possible). An individual creature can be affected by the rune only once, even if the rune could normally affect more creatures than just the rune-bearer.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        return await CommonRuneRules.ChooseARuneToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is DrawnRune runeTarget
                                ? drOnto =>
                                    drOnto == runeTarget
                                : drOnto =>
                                    !ModData.PersistentActions.RuneIsUsedUp(drOnto.Source!, drOnto.Rune.Id)
                                    && drOnto.Rune.InvocationProperties.DealsDamage
                                    && !drOnto.Rune.InvocationProperties.AffectsArea,
                            CreateEnPassive,
                            "Choose a rune whose invocation will radiate outward in a 15-foot emanation from its bearer.",
                            rune);

                        // Create rune
                        DrawnRune? CreateEnPassive(DrawnRune? drawnOnto)
                        {
                            if (drawnOnto is null)
                                return null;
                            
                            return new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"""
                                     When {drOnto.Illustration!.IllustrationAsIconString} {drOnto.Name!.WithColor("Blue")} is invoked, each other creature in a 15-foot emanation around {drOnto.Owner.ToColoredBoldedName()} also takes the base rune's damage and other effects (and can attempt a saving throw if possible).

                                     This doesn't allow a creature to be affected by the rune more than once, even if it would affect more.
                                     
                                     """,
                                drawnOnto)
                            {
                                AfterInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn
                                        || drInvoked.Rune.InvocationProperties.EffectOnOneTarget
                                            is not {} invocationOnEachTarget)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }

                                    if (await CommonRuneRules.ExecuteInnerInvokeAction(
                                        invokeAction,
                                        drInvoked,
                                        drInvoked.Owner,
                                        Target.Emanation(3),
                                        false,
                                        drInvoked.Rune.InvocationProperties.SoundEffectAfterInvocation
                                        ?? drInvoked.Rune.InvocationProperties.SoundEffectBeforeInvocation
                                        ?? ModData.SfxNames.INVOKE_RUNE,
                                        false, //drInvoked.Rune.InvocationProperties.Defense.HasValue,
                                        async (innerInvoke, caster, emTarget, result) =>
                                        {
                                            List<Creature> affectedCreatures = (await invocationOnEachTarget.Invoke(
                                                    innerInvoke,
                                                    drInvoked,
                                                    emTarget))
                                                ?.WhereNotNull()
                                                .ToList() ?? [emTarget];
                                            // Make the affected creature immune to further invocations of this rune
                                            foreach (Creature affectedCreature in affectedCreatures)
                                                affectedCreature.AddQEffect(CommonRuneRules.ImmunityToInvocation(drInvoked.Rune));
                                        })
                                        is null)
                                    {
                                        invokeAction.RevertRequested = true;
                                        return;
                                    }
                                },
                            };
                        }
                    }),
                new RuneInvocationProperties(null, null),
                [ModData.Traits.Diacritic])
            .ToFeat();
        
        // TODO: Fob-, Diacritic Rune of Doubling
        
        // TODO: Kit-, Diacritic Rune of Mercy
        
        // TODO: Per-, Diacritic Rune of Continuum
        
        // Sun-, Diacritic Rune of Preservation
        yield return new Rune(
                RuneId.Sun, 5,
                IllustrationName.RunestoneWinged, //IllustrationName.DisruptingRunestone,
                "This spiraling diacritic channels the magic of a rune outwards, then back to the same location, allowing a rune to reconstitute itself.",
                new RuneDrawProperties("drawn on a rune")
                    .WithDiacriticTargetingRequirements()
                    .WithInvokeableOncePerCombatRequirement(RuneId.Sun),
                new RunePassiveProperties(
                        """
                        After the base rune is invoked, the base rune automatically traces itself back upon the same target.

                        {b}Special{/b} You can have only one copy of {i}sun-, diacritic rune of preservation{/i} applied at a given time, and once you invoke it, you cannot Etch or Trace it again for the rest of this encounter.
                        """,
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        DrawnRune? sunPassive = await CommonRuneRules.ChooseARuneToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is DrawnRune runeTarget
                                ? drOnto =>
                                    drOnto == runeTarget
                                : drOnto =>
                                    !ModData.PersistentActions.RuneIsUsedUp(drOnto.Source!, drOnto.Rune.Id),
                            CreateSunPassive,
                            "Choose a rune that will be retraced after being invoked.",
                            rune);

                        // Only one instance allowed when drawn
                        if (sunPassive is not null)
                            CommonRuneRules.RemoveAllOtherInstancesOf(drawAction.Owner, rune.Id);

                        return sunPassive;

                        DrawnRune? CreateSunPassive(DrawnRune? drawnOnto)
                        {
                            if (drawnOnto is null)
                                return null;
                            
                            return new DrawnRune(
                                    drawAction,
                                    rune,
                                    (drThis, drOnto) =>
                                        $"After {drOnto.Illustration!.IllustrationAsIconString} {drOnto.Name!.WithColor("Blue")} is invoked, it traces itself back upon the same target.\n",
                                    drawnOnto)
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
                                                99)
                                            .WithActionCost(0)
                                            .WithSoundEffect(ModData.SfxNames.INVOKED_SUN)
                                            .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
                                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (traceAction, caster, targets) =>
                                            {
                                                if (targets.ChosenCreature is null)
                                                    return;
                                                // Animation on rune-bearer
                                                await CommonRuneRules.PlayInvocationAnimation(targets.ChosenCreature, drThis.Rune.Illustration);
                                            });

                                        sunRedraw.Name = sunRedraw.Name.Replace("Trace", "Retrace");
                                        sunRedraw.ProjectileKind = ProjectileKind.None;
                                        sunRedraw.EffectOnOneTarget = async (traceAction, caster, creature, result) =>
                                        {
                                            if (await CommonRuneRules.DrawRuneOnTarget(
                                                    sunRedraw,
                                                    drInvoked.Owner,
                                                    drInvoked.Rune,
                                                    drInvoked.DrawnOn,
                                                    false) is not {} newDrawnRune)
                                            {
                                                traceAction.RevertRequested = true;
                                                //invokeAction.RevertRequested = true;
                                                return;
                                            }
                                            traceAction.Tag = newDrawnRune;
                                            ModData.PersistentActions.UseUpRune(drThis.Source!, drThis.Rune.Id);
                                        };

                                        await drThis.Owner.Battle.GameLoop.FullCast(
                                            sunRedraw,
                                            ChosenTargets.CreateSingleTarget(drInvoked.Owner));
                                    },
                                };
                        }
                    }),
                new RuneInvocationProperties(null, null),
                [ModData.Traits.Diacritic])
            .ToFeat();

        // TODO: Ti-, Diacritic Rune of Fundaments
        
        // Ur-, Diacritic Rune of Intensity
        yield return new Rune(
                RuneId.Ur, 5,
                IllustrationName.DemolishingRunestone,
                "This diacritic accentuates the base rune with bolder lines to give greater weight to its effects.",
                new RuneDrawProperties("drawn on a rune that deals damage")
                    .WithDiacriticTargetingRequirements(dr =>
                    {
                        if (!dr.Rune.InvocationProperties.DealsDamage
                            || dr.Rune.Id == RuneId.Marssyl)
                            return "Base rune doesn't deal damage";
                        return null;
                    }),
                new RunePassiveProperties(
                        "When the base rune is invoked, its invocation gains a status bonus to damage equal to 2 plus half your level.",
                        null)
                    .WithDrawnRuneCreator(async (drawAction, rune, target, subTarget) =>
                    {
                        int bonusAmount = (2 + (drawAction.Owner.Level / 2));
                        
                        return await CommonRuneRules.ChooseARuneToDrawOn(
                            drawAction,
                            drawAction.Owner,
                            target,
                            subTarget is DrawnRune runeTarget
                                ? drOnto =>
                                    drOnto == runeTarget
                                : drOnto =>
                                    !ModData.PersistentActions.RuneIsUsedUp(drOnto.Source!, drOnto.Rune.Id)
                                    && (drOnto.Rune.InvocationProperties.DealsDamage
                                        || drOnto.Rune.Id == RuneId.Marssyl),
                            CreateUrPassive,
                            $"Choose a rune to gain a +{bonusAmount} status bonus to its invocation's damage.",
                            rune);

                        DrawnRune? CreateUrPassive(DrawnRune? drawnOnto)
                        {
                            if (drawnOnto is null)
                                return null;
                            
                            return new DrawnRune(
                                drawAction,
                                rune,
                                (drThis, drOnto) =>
                                    $"The invocation of {drOnto.Illustration!.IllustrationAsIconString} {drOnto.Name!.WithColor("Blue")} gains a +{bonusAmount} status bonus to its damage.",
                                drawnOnto)
                            {
                                BeforeInvokingRune = async (drThis, invokeAction, drInvoked) =>
                                {
                                    if (drThis.Disabled
                                        || drInvoked != drThis.DrawnOn)
                                        return;
                                    QEffect invokeBonus = new QEffect()
                                    {
                                        // No expiration because it needs to exist longer for invocations such as Esvadir which have hidden subsidiaries going on
                                        // Is removed on its own when the rune is invoked, and it only applies to the same type, so it should be safe to manually expire that way in this callback structure.
                                        //ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                                        BonusToDamage = (qfThis, action, defender) =>
                                        {
                                            if (drThis.Disabled)
                                                return null;

                                            Bonus urBonus = new Bonus(bonusAmount, BonusType.Status,
                                                "Ur, Diacritic Rune of Intensity");

                                            // Apply to an invocation action's damage
                                            if (action.HasTrait(ModData.Traits.Invocation)
                                                && action.Tag is DrawnRune drInvokingDamage
                                                && drInvokingDamage == drInvoked)
                                                return urBonus;
                                            
                                            // Apply to a Strike being buffed by Marssyl
                                            if (action.HasTrait(Trait.Strike)
                                                && qfThis.Owner.QEffects.Any(qf =>
                                                    qf.Traits.Any(tt => tt == ModData.Traits.Invocation)
                                                    && (qf.Name?.ToLower().Contains("marssyl") ?? false)
                                                    && qf.Tag is Item marssylItem 
                                                    && marssylItem == action.Item))
                                                return urBonus;

                                            return null;
                                        },
                                    };
                                    drThis.Source!.AddQEffect(invokeBonus);
                                },
                            };
                        }
                    }),
                new RuneInvocationProperties(null, null),
                [ModData.Traits.Diacritic])
            .ToFeat();

        #endregion

        #region 9th-Level
        
        // TODO: Astillu, Rune of Submersion
        
        // TODO: Cruonign, Rune of Leeching
        
        // Feikris, Rune of Gravity
        yield return new Rune(
                RuneId.Feikris, 9,
                IllustrationName.ResilientRunestone,
                "The lines of this rune overlap strangely, making it seem larger than it really is.",
                new RuneDrawProperties("drawn on a creature wearing armor")
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
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        const float emanationSize = 3f; // 15 feet

                        return await CommonRuneRules.ExecuteInnerInvokeAction(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            Target.Emanation((int)emanationSize),
                            false,
                            ModData.SfxNames.INVOKED_FEIKRIS,
                            true,
                            async (_, _, emanationTarget, result) =>
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

                    }),
                [Trait.Arcane])
            .WithLevelText("The status bonus increases to +3.", "17th")
            .ToFeat();

        // TODO: Germantria, Rune of Partnership
        
        // Ichelsu, Rune of Observation
        yield return new Rune(
                RuneId.Ichelsu, 9,
                IllustrationName.GhostTouchRunestone,
                "A ring of dotted circles, this rune allows a creature marked with it to see all.",
                new RuneDrawProperties("drawn on a creature")
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
                                QEffect see = QEffect.SeeInvisibility()
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
                                    .AddQEffect(vision);
                                CommonStealthActions.ThisCreatureGainedTheAbilityToSeeInvisible(qfThis.Owner);
                            },
                            WhenExpires = qfThis =>
                            {
                                qfThis.Owner.RemoveAllQEffects(qf =>
                                    qf.Id is QEffectId.SeeInvisibility 
                                        or QEffectId.AllAroundVision
                                    && qf.Supereffect == qfThis);
                            }
                        };
                    }),
                new RuneInvocationProperties(
                        "The eyes of the rune fly outwards, attaching to all creatures in a 20-foot emanation. Each of these creatures that would be invisible is concealed instead, and one that would be concealed for any other reason is not concealed. This effect lasts for 2 rounds.",
                        null)
                    .WithAffectsArea()
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        const float emanationSize = 4f; // 20 feet

                        return await CommonRuneRules.ExecuteInnerInvokeAction(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            Target.Emanation((int)emanationSize),
                            true,
                            ModData.SfxNames.INVOKED_ICHELSU,
                            false,
                            async (_,_, emanationTarget, _) =>
                            {
                                emanationTarget.DetectionStatus.HiddenTo.Clear();
                                emanationTarget.DetectionStatus.Undetected = false;
                                QEffect invokedIchelsu = invokedRune.NewInvocationEffect(
                                    "If you're invisible, you're concealed instead. If you're concealed for any other reason, you're no longer concealed.",
                                    ExpirationCondition.Never,
                                    qf =>
                                    {
                                        qf.Id = QEffectId.FaerieFire;
                                        qf.WithExpirationAtStartOfSourcesTurn(invokeAction.Owner, 2);
                                    });
                                emanationTarget.AddQEffect(invokedIchelsu);
                            });
                    }),
                [Trait.Occult])
            .ToFeat();

        // Jurroz, Rune of Dragon Fury
        yield return new Rune(
                RuneId.Jurroz, 9,
                IllustrationName.CorrosiveRunestone,
                "This craggy rune channels the fury of dragon kind.",
                new RuneDrawProperties("drawn onto a creature")
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
                                $"Choose where to Fly for invoked {invokedRune.Rune.Id.ToFullName()}, or right-click to pass. You should end your movement next to a creature who has damaged you.",
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
                RuneId.Kojastri, 9,
                IllustrationName.FrostRunestone,
                "This rune insulates from harmful energy of all kinds.",
                new RuneDrawProperties("drawn a creature wearing armor") // drawn on armor
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
                        ChoiceButtonOption choice = await drawAction.Owner.AskForChoiceAmongButtons(
                            rune.Illustration,
                            $$"""
                              {b}{{rune.Name}}{/b}
                              Choose which damage type to insulate against.
                              """,
                            $"{{icon:RayOfFrost}} {"Cold".WithColor(DamageKind.Cold.DamageKindToColor())}",
                            $"{{icon:ElectricArc}} {"Electricity".WithColor(DamageKind.Electricity.DamageKindToColor())}",
                            $"{{icon:ProduceFlame}} {"Fire".WithColor(DamageKind.Fire.DamageKindToColor())}");

                        DamageKind chosenKind;
                        Trait chosenTrait;
                        switch (choice.Index)
                        {
                            case 0:
                                chosenKind = DamageKind.Cold;
                                chosenTrait = Trait.Cold;
                                break;
                            case 1:
                                chosenKind = DamageKind.Electricity;
                                chosenTrait = Trait.Electricity;
                                break;
                            case 2:
                                chosenKind = DamageKind.Fire;
                                chosenTrait = Trait.Fire;
                                break;
                            default:
                                return null;
                        }
                        
                        var resist = rune.CalculateHeightening(4, 2, 1, drawAction.Owner.Level);
                        var thorns = rune.CalculateHeightening(2, 2, 1, drawAction.Owner.Level);
                        
                        return new DrawnRune(
                            drawAction,
                            rune,
                            $"You have resistance {resist.FinalValue.WithColor("Blue")} to {chosenKind.ToStringOrTechnical().ToLower().WithColor(chosenKind.DamageKindToColor())} damage, and creatures take {thorns.FinalValue.WithColor("Blue")} {chosenKind.ToStringOrTechnical().ToLower().WithColor(chosenKind.DamageKindToColor())} damage when they touch you, damage you with an unarmed attack, or damage you with a non-reach melee weapon.")
                        {
                            Name = $"{rune.Name} ({chosenKind.ToStringOrTechnical().ToLower()})",
                            Tag = chosenKind,
                            CountsAsABuff = true,
                            StateCheck = qfThis =>
                                qfThis.Owner.WeaknessAndResistance.AddResistance(chosenKind, resist.FinalValue),
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
                        .WithTrait(chosenTrait);

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
                                        drThis.Owner, rune.Name,
                                        [..drThis.Traits])
                                    .WithTag(drThis)
                                    .WithOrigin(new ActionOrigin
                                    {
                                        QEffect = drThis,
                                        Source = drawAction.Owner
                                    }),
                                DiceFormula.FromText(
                                    thorns.FinalValue.ToString(),
                                    rune.Name),
                                enemy,
                                CheckResult.Success,
                                chosenKind);
                        }
                    }),
                new RuneInvocationProperties(
                        // Normally I'd cross out "engulfed", but leaving it in means I might get reports
                        // if it's later implemented and this rune ceases to affect engulfing foes.
                        "Any creature that has the armor's wearer engulfed, grabbed, restrained, or swallowed whole takes 8d4 damage of the rune's chosen type with a basic Reflex save. On a failure, it also releases the armor's wearer.",
                        (rune, level) =>
                        {
                            var numDice = rune.CalculateHeightening(8, 2, 2, level);
                            return
                                $"Any creature that has the armor's wearer engulfed, grabbed, restrained, or swallowed whole takes {S.HeightenedVariable(numDice.FinalValue, numDice.BaseValue)}d4 damage of the rune's chosen type with a basic Reflex save. On a failure, it also releases the armor's wearer.";
                        })
                    .WithAdditionalRequirement((a, d) =>
                        d.HasEffect(qf =>
                            qf.Id is QEffectId.Grappled && qf.Source != null)
                        || d.Space.Surface?.Swallower != null
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Not grappled or swallowed whole by a creature"))
                    .WithDealsDamage()
                    .WithDefense(Defense.Reflex)
                    .WithHideTooltip()
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        if (invokedRune.Tag is not DamageKind chosenKind)
                        {
                            invokeAction.RevertRequested = true;
                            return null;
                        }
                        
                        var numDice = invokedRune.Rune.CalculateHeightening(8, 2, 2, invokeAction.Owner.Level);
                        string damageExpression = $"{numDice.FinalValue}d4";
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
                                    invokeAction.Owner.ClassDC(ModData.Traits.Runesmith)));
                            
                            CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                                enemy,
                                invocationDetails,
                                invocationDetails.SavingThrow!);
                            
                            await CommonSpellEffects.DealBasicDamage(
                                invocationDetails,
                                invokeAction.Owner,
                                enemy,
                                result,
                                damageExpression,
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
            .WithLevelText(
                "The resistance and damage granted by insulation both increase by 1, and the damage dealt by the invocation increases by 2d4.",
                "+2")
            .ToFeat();
        
        // TODO: Oraloq, Rune of Inarticulateness
        
        // TODO: Piteregrin, Rune of Transposition

        // Trolistri, Rune of Forlorn Sorrow
        // Faction alignment is treated as "enemies to the runesmith", regardless of the rune-bearer's faction.
        // DOC: Changed to a 10-foot size, but always works.
        // BUG: The difficult terrain effect doesn't interact with immunity to emotion or mental effects. No known way to fix this at this time.
        yield return new Rune(
                RuneId.Trolistri, 9,
                IllustrationName.NightmareRunestone,
                "This rune calls to mind the beauty hidden in sorrow. While this rune is beautiful, sorrow is best admired from a distance, discouraging approach.",
                new RuneDrawProperties("drawn onto a creature")
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
                    .WithInvocationOnEachTarget(async (invokeAction, invokedRune, effectTarget) =>
                    {
                        const float emanationSize = 4f; // 20 feet

                        return await CommonRuneRules.ExecuteInnerInvokeAction(
                            invokeAction,
                            invokedRune,
                            effectTarget,
                            Target.Emanation((int)emanationSize)
                                .WithIncludeOnlyIf((tar, cr) =>
                                    cr.EnemyOf(invokeAction.Owner)
                                    && !cr.IsImmuneTo(Trait.Mental)
                                    && !cr.IsImmuneTo(Trait.Emotion)),
                            true,
                            ModData.SfxNames.INVOKED_TROLISTRI,
                            true,
                            async (_, _, invokedOnto, result) =>
                            {
                                invokedOnto.AddQEffect(CommonRuneRules
                                    .ImmunityToInvocation(invokedRune.Rune, true));
                                if (result > CheckResult.Failure)
                                    return;
                                invokedOnto.AddQEffect(QEffect
                                    .Slowed(result == CheckResult.CriticalFailure ? 2 : 1)
                                    .WithExpirationInOneRound(invokedOnto.Battle));
                            });
                    }),
                [Trait.Arcane, Trait.Emotion, Trait.Mental])
            .ToFeat();
        
        // TODO: Ulgatus, Rune of Restraint
        
        // TODO: Yudici, Rune of Remonstrance

        #endregion

        #region 13th-Level (skipped for now)
        
        // TODO: Eck-, Diacritic Rune of Phantasma
        
        // Inth-, Diacritic Rune of Corruption
        // "the target takes 1d4 persistent fire damage" has some ambiguity between Esvadir's invocation and Pluuna's invocation.
        // DOC: Wording changed to specify that the rune-bearer takes the persistent damage.
        /*Rune runeInthDiacritic = new Rune(
                "Inth-, Diacritic Rune of Corruption",
                ModData.Traits.InthDiacritic,
                IllustrationName.KeenRunestone,
                13,
                "drawn on a rune",
                "This set of angular accents around the base rune channels the essence of fiendish corruption.",
                "The base rune gains the unholy trait, as does any damage it deals. If applied to a holy creature, that creature is enfeebled 1 for as long as the rune is applied to it.",
                "(unholy) When the base rune is invoked, it burns away in unholy black fire that lingers on its bearer. In addition to the base rune's normal effect, the rune-bearer takes 1d4 persistent fire damage.",
                "The damage increases by 1d4.",
                [ModData.Traits.Diacritic, Trait.Divine, Trait.Fiend, Trait.Fire, Trait.Evil])
            .WithHeightenedText(
                null,
                (rune, charLevel) =>
                {
                    int currentLevel = Math.Max(charLevel, 9);
                    int bonusLevel = (currentLevel - 9) / 2;
                    int numDice = 1 + bonusLevel;
                    string heightenedVar = S.HeightenedVariable(numDice, 1);
                    return $"(unholy) When the base rune is invoked, it burns away in unholy black fire that lingers on its target. In addition to the base rune's normal effect, the target takes {heightenedVar}d4 persistent fire damage.";
                },
                "+2")
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnDiacritics())
            .WithDrawnRuneCreator(async (sourceAction, caster, target, rune) =>
            {
                switch (sourceAction.Target)
                {
                    case AreaTarget:
                        foreach (DrawnRune? inthPassive in DrawnRune
                                     .GetDrawnRunes(null, target)
                                     .Where(IsValidRune)
                                     .Select(CreateInthPassive))
                        {
                            if (inthPassive == null)
                                continue;
                        
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(ModData.Traits.Etched))
                                inthPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                inthPassive.WithIsTraced();

                            target.AddQEffect(inthPassive);
                        }
                        // Return an ephemeral DrawnRune since we just applied this to a whole batch of runes.
                        return new DrawnRune(rune);
                    default:
                        // TODO: replace .Occupies
                        await caster.FictitiousSingleTileMove(caster.Occupies); // Move back into place
                        DrawnRune? chosenRune = await CommonRuneRules.ChooseADrawnRune(
                            caster,
                            [target],
                            rune.Illustration,
                            $"Pick a rune to draw {{Blue}}{rune.Name}{{/Blue}} onto.",
                            dr => "Draw onto {Blue}" + dr.Rune.Name + "{/Blue}",
                            null, "Pass", true,
                            IsValidRune);

                        if (chosenRune is null)
                        {
                            sourceAction.RevertRequested = true;
                            return null;
                        }

                        return CreateInthPassive(chosenRune);
                }

                bool IsValidRune(DrawnRune dr)
                {
                    return dr.AttachedDiacritic == null;
                }

                DrawnRune? CreateInthPassive(DrawnRune targetRune)
                {
                    int currentLevel = Math.Max(caster.Level, 9);
                    int bonusLevel = (currentLevel - 9) / 2;
                    int numDice = 1 + bonusLevel;
                    DrawnRune drawnInth = new DrawnRune(
                        rune,
                        "The base rune gains the unholy trait, as does any damage it deals.",
                        caster)
                    {
                        Name = $"{rune.Name} ({targetRune.Name})", // Custom name
                        StateCheck = qfThis =>
                        {
                            qfThis.Description = "The base rune gains the unholy trait, as does any damage it deals.";
                            if (IsHoly(qfThis.Owner))
                            {
                                qfThis.Description += "\n\n{Blue}Holy:{/Blue} You are enfeebled 1.";
                                qfThis.Owner.AddQEffect(QEffect.Enfeebled(1).WithExpirationEphemeral());
                            }
                            DrawnRune dr = (qfThis as DrawnRune)!;
                            if (dr.DrawnOn is DrawnRune onto && !onto.Traits.Contains(Trait.Evil))
                                onto.Traits.Add(Trait.Evil);
                        },
                        AfterInvokingRune = async (drThis, action, drInvoked) =>
                        {
                            if (drThis.Disabled || drInvoked != drThis.DrawnOn)
                                return;
                            if (drInvoked.Owner.IsImmuneTo(Trait.Evil))
                                return;
                            QEffect pFire = QEffect.PersistentDamage(
                                numDice + "d4",
                                drInvoked.Owner.WeaknessAndResistance.WhatDamageKindIsBestAgainstMe([DamageKind.Fire, DamageKind.Evil]));
                            pFire.Traits.Add(Trait.Evil);
                            pFire.SourceAction = action;
                            drInvoked.Owner.AddQEffect(pFire);
                        }
                    }
                    .WithDiacriticRegulator(targetRune);
                    drawnInth.AddGrantingOfTechnical(
                        _ => true,
                        qfTech =>
                        {
                            qfTech.StateCheck += qfThis =>
                            {
                                if (IsHoly(qfThis.Owner))
                                    qfThis.Owner.WeaknessAndResistance.Weaknesses.Add(
                                        new SpecialResistance(
                                            "unholy (inth-)",
                                            (action, dk) =>
                                            {
                                                // The action dealing damage was an invocation of a rune,
                                                // and the rune invoked was the base rune of this diacritic;
                                                if (action?.Tag == drawnInth.DrawnOn)
                                                    return true;
                                                
                                                // The action has an origin QEffect,
                                                if (action?.Origin?.QEffect is { } originQf)
                                                {
                                                    // and is persistent damage that was applied via this diacritic's base rune.
                                                    if (originQf.Id == QEffectId.PersistentDamage
                                                        && originQf.SourceAction?.Tag == drawnInth.DrawnOn)
                                                        return true;
                                                    
                                                    // and the origin of this damage is the base rune itself.
                                                    if (originQf == drawnInth.DrawnOn)
                                                        return true;
                                                    
                                                    return false;
                                                }
                                                
                                                // The action was a Strike being buffed by Marssyl
                                                if ((action?.HasTrait(Trait.Strike) ?? false)
                                                    && action.Owner.QEffects.Any(qf =>
                                                        qf is DrawnRune dr
                                                        && dr.Rune.Id == RuneId.Marssyl
                                                        && dr.DrawnOn == action.Item))
                                                    return true;

                                                return false;
                                            },
                                            // The unholy weakness amount is equal to their existing evil weakness.
                                            qfThis.Owner.WeaknessAndResistance.Weaknesses.Max(weak =>
                                                weak.DamageKind == DamageKind.Evil ? weak.Value : 0),
                                            null));
                            };
                        });
                    return drawnInth;
                }

                bool IsHoly(Creature cr)
                {
                    return cr.HasTrait(Trait.Good)
                           && cr.WeaknessAndResistance.Weaknesses.Any(weak =>
                               weak.DamageKind is DamageKind.Evil);
                }
            })
            .WithDrawnOnRuneTechnical();
        RuneFeat inthFeat = AddRuneAsRuneFeat(ModData.ID_PREPEND+"RuneInthDiacritic", runeInthDiacritic);
        inthFeat.RulesText += "\n\n" + ModData.Illustrations.DdSun.IllustrationAsIconString + " {b}Compatibility{/b} For the purposes of being holy, creatures with weakness to evil damage are considered holy, and this diacritic's persistent damage uses the better of fire or evil damage.";*/
        
        // TODO: Nesh-, Diacritic Rune of Contingency
        
        // TODO: Sar-, Diacritic Rune of Righteousness

        #endregion

        #region 17th-Level

        // Aiuen, the Elf-Gate Key
        
        // Ochygholl, the Poisoned Star
        
        // Rovan, Seal of the Dead Vault
        
        // Xinsala, the Well of Virtues

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
            $"RunesmithRuneSelection.{rune.Id.ToWord()}",
            rune.Id.ToFullName())
        {
            Illustration = rune.Illustration,
            TextCreator = () =>
                CommonRuneRules.GetFormattedFeatDescription(rune),
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
                    ModData.ID_PREPEND+"Rune."+rune.Id.ToWord(),
                    rune.Id.ToFullName()),
                rune.GetFlavorText(null, false),
                CommonRuneRules.GetFormattedFeatDescription(rune, false),
                rune.Traits
                    .Except([ModData.Traits.Runesmith])
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
}