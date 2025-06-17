using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class RunesmithRunes
{
    public static List<Rune> AllRunes { get; } = [];
    public static List<RuneFeat> AllRuneFeats { get; } = [];

    public static void LoadRunes()
    {
        /* TODO: Consider altering the way runes apply Item effects based on these Item fields to look into:
         * WithPermanentQEffectWhenWorn
         * WithOnCreatureWhenWorn
         * StateCheckWhenWielded
         */

        // Level 1 Runes
        Rune runeAtryl = new Rune(
                "Atryl, Rune of Fire",
                ModData.Traits.Atryl,
                IllustrationName.FlamingRunestone,
                1,
                "drawn on a creature or object",
                "This rune is often placed on a stone in a hearth to ensure a fire does not go out in the night, its power enabling even stone to burn.",
                "The bearer's fire resistance, if any, is reduced by 6. Its immunities are unaffected.",
                "The bearer takes 2d6 fire damage, with a basic Fortitude save; on a critical failure, they are dazzled for 1 round.",
                "The reduction in fire resistance increases by 1, and the damage of the invocation increases by 2d6.",
                [Trait.Fire, Trait.Primal])
            {
                LevelFormat = "+2",
                PassiveTextWithHeightening = (thisRune, level) =>
                {
                    const int baseValue = 6;
                    int bonusValue = (level - thisRune.BaseLevel) / 2; // Increase by 1 every 2 character levels
                    int totalValue = 6 + (level - thisRune.BaseLevel) / 2;
                    string heightenedVar = S.HeightenedVariable(totalValue, baseValue);
                    return $"The bearer's fire resistance, if any, is reduced by {heightenedVar}. Its immunities are unaffected.";
                },
                InvocationTextWithHeightening = (thisRune, level) =>
                {
                    const int baseValue = 2;
                    int roundHalfLevel = ((level - thisRune.BaseLevel) / 2);
                    int damageAmount = 2 + roundHalfLevel * 2;
                    string heightenedVar = S.HeightenedVariable(damageAmount, baseValue);
                    return $"The bearer takes {heightenedVar}d6 fire damage, with a basic Fortitude save; on a critical failure, they are dazzled for 1 round.";
                },
                UsageCondition = (attacker, defender) =>
                {
                    return defender.EnemyOf(attacker)
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("not an enemy");
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    int resistReductionAmount = 6 + (caster != null ? ((caster.Level - thisRune.BaseLevel) / 2) : 0);
                    DrawnRune atrylPassive = new DrawnRune(thisRune, thisRune.Name,
                        "Fire resistance reduced by " + resistReductionAmount + ".")
                    {
                        Illustration = thisRune.Illustration,
                        Source = caster,
                        Value = resistReductionAmount, // Value might be an unnecessary field, aesthetically. // TODO: use Key field?
                        Traits = [..thisRune.Traits],
                        StateCheck = qfSelf =>
                        {
                            if ((qfSelf as DrawnRune)!.Disabled)
                                return;

                            Resistance? fireResist =
                                qfSelf.Owner.WeaknessAndResistance.Resistances.FirstOrDefault(res =>
                                    res.DamageKind == DamageKind.Fire);
                            if (fireResist != null && fireResist.Value > 0)
                            {
                                QEffect? existingAtryl = qfSelf.Owner.QEffects.FirstOrDefault(qfSearch =>
                                        qfSearch.Name == "Atryl, Rune of Fire" && qfSearch != qfSelf);
                                if (existingAtryl != null && existingAtryl.Value >= qfSelf.Value &&
                                    existingAtryl.AppliedThisStateCheck)
                                    return;
                                fireResist.Value = Math.Max(0, fireResist.Value - qfSelf.Value);
                            }
                        },
                    };
                    return atrylPassive;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                    {
                        int roundHalfLevel = ((caster.Level - 1) / 2);
                        int damageAmount = 2 + roundHalfLevel * 2;
                        CheckResult result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Fortitude,
                            RunesmithClass.RunesmithDC(caster));
                        await CommonSpellEffects.DealBasicDamage(sourceAction, caster, target, result,
                            damageAmount + "d6", DamageKind.Fire);
                        Sfxs.Play(ModData.SfxNames.InvokedAtryl);
                        if (result == CheckResult.CriticalFailure)
                        {
                            target.AddQEffect(QEffect.Dazzled()
                                .WithExpirationOneRoundOrRestOfTheEncounter(caster, false));
                        }
                    }

                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    CommonRuneRules.ApplyImmunity(target, thisRune);
                },
            }
            .WithDetrimentalPassiveTechnical()
            .WithDamagingInvocationTechnical()
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneAtryl", runeAtryl);

        // BUG: Add ignores concealment to the invocation... or should I?
        Rune runeEsvadir = new Rune(
                "Esvadir, Rune of Whetstones",
                ModData.Traits.Esvadir,
                IllustrationName.WoundingRunestone,
                1,
                "drawn on a piercing or slashing weapon or unarmed Strike",
                "This serrated rune, when placed on a blade, ensures it will never go dull.",
                "On a successful Strike, the weapon deals an additional 2 persistent bleed damage per weapon damage die.",
                "The essence of sharpness is released outwards from the rune, dealing 2d6 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.",
                "The damage of the invocation increases by 2d6.")
            {
                InvocationTextWithHeightening = (thisRune, level) =>
                {
                    const int baseValue = 2;
                    int roundHalfLevel = ((level - thisRune.BaseLevel) / 2);
                    int damageAmount = 2 + roundHalfLevel * 2;
                    string heightenedVar = S.HeightenedVariable(damageAmount, baseValue);
                    return $"The essence of sharpness is released outwards from the rune, dealing {heightenedVar}d6 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.";
                },
                LevelFormat = "+2",
                UsageCondition = (attacker, defender) =>
                {
                    if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.EsvadirOnEnemies))
                        return Usability.Usable;
                    bool isAlly = defender.FriendOf(attacker);
                    Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                    return isAlly
                        ? Usability.Usable
                        : allyNotUsable; // Can always do Unarmed Strikes, so always drawable.
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune? MakeEsvadirPassive(Item targetItem)
                    {
                        DrawnRune esvadirPassive = new DrawnRune(
                                thisRune,
                                $"{thisRune.Name} ({targetItem.Name})",
                                $"The target item or piercing and slashing unarmed strikes deal 2 persistent bleed damage per weapon damage die.",
                                ExpirationCondition.Ephemeral,
                                caster,
                                thisRune.Illustration)
                            {
                                Source = caster,
                                Traits = [..thisRune.Traits], //[..thisRune.Traits],
                                AfterYouTakeAction = async (QEffect qfSelf, CombatAction action) => // Add bleed
                                {
                                    if ((qfSelf as DrawnRune)!.Disabled)
                                        return;

                                    Item? qfItem = (qfSelf as DrawnRune)?.DrawnOn as Item;
                                    Item? actionItem = action.Item;

                                    // This many complex conditionals is really hard to work out so I did it the long way.
                                    // Fail to bleed if,
                                    if (actionItem == null || qfItem == null || // either item is blank
                                        !action.HasTrait(Trait.Strike) || // or the action isn't a strike
                                        action.ChosenTargets.ChosenCreature == null || // or null targets
                                        action.ChosenTargets.ChosenCreature ==
                                        qfSelf.Owner || // or I'm my target for any reason
                                        !(actionItem.DetermineDamageKinds().Contains(DamageKind.Piercing) ||
                                          actionItem.DetermineDamageKinds()
                                              .Contains(DamageKind
                                                  .Slashing))) // or it's not piercing or slashing damage
                                        return;
                                    // Fail to bleed if,
                                    if (actionItem.HasTrait(Trait.Unarmed)) // attacking with an unarmed,
                                    {
                                        if (!qfItem.HasTrait(Trait.Unarmed)) // that is unbuffed.
                                            return;
                                    }
                                    else // attacking with a regular weapon,
                                    {
                                        if (actionItem != qfItem) // that is unbuffed.
                                            return;
                                    }


                                    // Determine weapon damage dice count
                                    string weaponDamageDiceCount =
                                        actionItem.WeaponProperties!.DamageDieCount.ToString();
                                    ;
                                    if (action.TrueDamageFormula is { } trueDamage)
                                    {
                                        Capture diceCountCapture =
                                            Regex.Match(trueDamage.ToString(), @"(\d+)d\d+").Groups[1];
                                        if (diceCountCapture.Value != "")
                                            weaponDamageDiceCount = diceCountCapture.Value;
                                    }

                                    DiceFormula bleedAmount = DiceFormula.FromText(
                                        (2 * int.Parse(weaponDamageDiceCount) *
                                         (action.CheckResult == CheckResult.CriticalSuccess ? 2 : 1)).ToString(),
                                        thisRune.Name);

                                    //DiceFormula bleedAmount = DiceFormula.FromText(
                                    //((action.CheckResult == CheckResult.CriticalSuccess ? 2 : 1) * 2 * actionItem.WeaponProperties!.DamageDieCount).ToString(),
                                    //thisRune.Name);

                                    if (action.CheckResult >= CheckResult.Success)
                                    {
                                        action.ChosenTargets.ChosenCreature.AddQEffect(
                                            QEffect.PersistentDamage(bleedAmount, DamageKind.Bleed));
                                    }
                                },
                            }
                            .WithItemOrUnarmedRegulator(targetItem);

                        return esvadirPassive;
                    }

                    // Target a specific item
                    List<string> validItemsString = [];
                    List<Item> validItems = [];
                    foreach (Item item in target.HeldItems.Where(item =>
                                 item.WeaponProperties != null && item.WeaponProperties.DamageKind != null &&
                                 (item.DetermineDamageKinds().Contains(DamageKind.Piercing) ||
                                  item.DetermineDamageKinds().Contains(DamageKind.Slashing))))
                    {
                        validItemsString.Add(item.Name);
                        validItems.Add(item);
                    }

                    validItems.Add(target.UnarmedStrike);
                    validItemsString.Add("unarmed strikes");

                    if (sourceAction?.Target is AreaTarget)
                    {
                        foreach (DrawnRune? esvadirPassive in validItems.Select(MakeEsvadirPassive))
                        {
                            if (esvadirPassive == null)
                                continue;
                            
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(ModData.Traits.Etched))
                                esvadirPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                esvadirPassive.WithIsTraced();

                            target.AddQEffect(esvadirPassive);
                        }

                        return
                            new DrawnRune(
                                thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                    }

                    ArgumentNullException.ThrowIfNull(caster);
                    ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{sourceAction?.Name}{{/b}}\nWhich item, or unarmed strikes, would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                        validItemsString.ToArray()
                    );

                    Item targetItem = validItems[chosenOption.Index];

                    return MakeEsvadirPassive(targetItem);
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    // Create action wrapper for targeting and roll-inspection of invoking from target to adjacent creature.
                    CombatAction invokeEsvadirOnToAdjacentCreature = new CombatAction(
                            target, // Get creatures adjacent to the rune, who is the creature with the drawn rune being invoked
                            thisRune.Illustration,
                            $"Invoke {thisRune.Name}",
                            [..thisRune.Traits, Trait.DoNotShowInCombatLog],
                            thisRune.InvocationTextWithHeightening(thisRune, caster.Level)!,
                            Target.RangedCreature(1) /*AdjacentCreature()*/
                                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                                {
                                    bool isEnemy = defender.EnemyOf(caster);
                                    bool isAdjacent = defender.IsAdjacentTo(target);
                                    return isEnemy
                                        ? (isAdjacent
                                            ? Usability.Usable
                                            : Usability.NotUsableOnThisCreature("Not adjacent"))
                                        : Usability.NotUsableOnThisCreature("Not enemy");
                                }))
                        .WithActionCost(0)
                        //.WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                        .WithSoundEffect(ModData.SfxNames.InvokedEsvadir)
                        .WithSavingThrow(new SavingThrow(Defense.Fortitude, RunesmithClass.RunesmithDC(caster)))
                        .WithEffectOnEachTarget(async (selfAction, caster2, target2, result) =>
                        {
                            if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                            {
                                int roundHalfLevel = ((caster.Level - thisRune.BaseLevel) / 2);
                                int damageAmount = 2 + roundHalfLevel * 2;
                                await CommonSpellEffects.DealBasicDamage(
                                    sourceAction,
                                    caster2,
                                    target2,
                                    result,
                                    damageAmount + "d6",
                                    DamageKind.Slashing);
                            }

                            CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                            CommonRuneRules.ApplyImmunity(target2, thisRune);
                        });

                    if (sourceAction.HasTrait(ModData.Traits.InvokeAgainstGivenTarget))
                        await caster.Battle.GameLoop.FullCast(invokeEsvadirOnToAdjacentCreature,
                            ChosenTargets.CreateSingleTarget(target));
                    else
                    {
                        invokeEsvadirOnToAdjacentCreature
                            .WithProjectileCone(
                                VfxStyle.BasicProjectileCone(thisRune
                                    .Illustration)); // Add extra animation when going from A to B.
                        await caster.Battle.GameLoop.FullCast(invokeEsvadirOnToAdjacentCreature);
                    }
                },
            }
            .WithDamagingInvocationTechnical()
            .WithFortitudeSaveInvocationTechnical()
            .WithTargetDoesNotSaveTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneEsvadir", runeEsvadir);

        Rune runeHoltrik = new Rune(
                "Holtrik, Rune of Dwarven Ramparts",
                ModData.Traits.Holtrik,
                IllustrationName.ArmorPotencyRunestone,
                1,
                "drawn on a shield",
                "Similarity in the Dwarven words for “wall” and “shield” ensure that this angular rune, once used to shore up tunnels, can apply equally well in the heat of battle.",
                "A shield bearing this rune increases its circumstance bonus to AC by 1.",
                "You call the shield to its rightful place. You Raise the Shield bearing the rune, as if the rune-bearer had used Raise a Shield, and the shield retains the increased bonus to AC until the beginning of the creature's next turn.",
                additionalTraits: [Trait.Dwarf])
            {
                UsageCondition = (attacker, defender) =>
                {
                    bool hasShield = defender.HeldItems.Any(item => item.HasTrait(Trait.Shield));
                    Usability shieldNotUsable = Usability.NotUsableOnThisCreature("doesn't have a shield");
                    bool isAlly = defender.FriendOf(attacker);
                    Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                    return isAlly ? (hasShield ? Usability.Usable : shieldNotUsable) : allyNotUsable;
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune? MakeHoltrikPassive(Item targetItem)
                    {
                        DrawnRune drawnHoltrik = new DrawnRune(
                            thisRune,
                            $"{thisRune.Name} ({targetItem.Name})",
                            "The circumstance bonus from Raising a Shield is increased by 1.",
                            ExpirationCondition.Ephemeral,
                            caster,
                            thisRune.Illustration)
                        {
                            Traits = [..thisRune.Traits], //[..thisRune.Traits],
                            BonusToDefenses = (qfSelf, attackAction, targetDefense) =>
                            {
                                if ((qfSelf as DrawnRune)!.Disabled)
                                    return null;
                                // Copied from Raise a Shield
                                if (targetDefense != Defense.AC &&
                                    (!qfSelf.Owner.HasEffect(QEffectId.SparklingTarge) ||
                                     !qfSelf.Owner.HasEffect(QEffectId.ArcaneCascade) ||
                                     !targetDefense.IsSavingThrow() || attackAction == null ||
                                     !attackAction.HasTrait(Trait.Spell)))
                                    return null;

                                // If they aren't raising a shield,
                                if (!qfSelf.Owner.HasEffect(QEffectId.RaisingAShield))
                                    return null; // no bonus.

                                // If not the first of multiple duplicate effects,
                                if (qfSelf != qfSelf.Owner.QEffects.First(qf =>
                                        qf is DrawnRune dr && dr.Rune.RuneId == ModData.Traits.Holtrik))
                                    return null; // no bonus.

                                return new Bonus(1, BonusType.Untyped, "Holtrik (raised shield)");
                            },
                        }.WithItemRegulator(targetItem);

                        return drawnHoltrik;
                    }

                    // Target a specific item
                    switch (target.HeldItems.Count(item => item.HasTrait(Trait.Shield)))
                    {
                        case 0:
                            return null;
                        case 1:
                            return MakeHoltrikPassive(target.HeldItems.First(item => item.HasTrait(Trait.Shield)));
                        default:
                            if (sourceAction?.Target is AreaTarget)
                            {
                                foreach (DrawnRune? holtrikPassive in target.HeldItems.Select(MakeHoltrikPassive))
                                {
                                    if (holtrikPassive == null)
                                        continue;
                                    
                                    // Determine the way the rune is being applied.
                                    if (sourceAction.HasTrait(ModData.Traits.Etched))
                                        holtrikPassive.WithIsEtched();
                                    else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                        holtrikPassive.WithIsTraced();

                                    target.AddQEffect(holtrikPassive);
                                }

                                return
                                    new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                            }

                            Item targetItem = await target.Battle.AskForConfirmation(
                                caster ?? throw new ArgumentNullException(nameof(caster)),
                                thisRune.Illustration,
                                $"{{b}}{sourceAction?.Name}{{/b}}\nWhich shield would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                                target.HeldItems[0].Name,
                                target.HeldItems[1].Name)
                                ? target.HeldItems[0]
                                : target.HeldItems[1];

                            return MakeHoltrikPassive(targetItem);
                    }
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                    {
                        // Raise their Shield
                        // Copied from Shielding Strike
                        Possibilities shieldActions = Possibilities.Create(target)
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            });
                        List<Option> actions = await target.Battle.GameLoop.CreateActions(target, shieldActions, null);
                        await target.Battle.GameLoop.OfferOptions(target, actions, true);

                        // Adding the QF doesn't let you Shield Block.
                        //bool hasShieldBlock = target.HasEffect(QEffectId.ShieldBlock) || target.WieldsItem(Trait.AlwaysOfferShieldBlock);
                        //target.AddQEffect(QEffect.RaisingAShield(hasShieldBlock));

                        // The rune-bearer retains the bonus
                        QEffect retainedBonus = invokedRune.NewInvocationEffect(
                            "The circumstance bonus from Raising a Shield is increased by 1.",
                            ExpirationCondition.ExpiresAtStartOfYourTurn);
                        retainedBonus.BonusToDefenses = (qfSelf, attackAction, targetDefense) =>
                        {
                            // Copied from Raise a Shield
                            if (targetDefense != Defense.AC &&
                                (!qfSelf.Owner.HasEffect(QEffectId.SparklingTarge) ||
                                 !qfSelf.Owner.HasEffect(QEffectId.ArcaneCascade) ||
                                 !targetDefense.IsSavingThrow() || attackAction == null ||
                                 !attackAction.HasTrait(Trait.Spell)))
                                return null;

                            if (!qfSelf.Owner.HasEffect(QEffectId.RaisingAShield) || // If they aren't raising a shield,
                                target.QEffects.FirstOrDefault( // or already have a Holtrik,
                                    qfSearch => qfSearch.Name == qfSelf.Name && qfSearch != qfSelf) != null)
                                return null; // No bonus.

                            return new Bonus(1, BonusType.Untyped, "Holtrik (raised shield)");
                        };
                        target.AddQEffect(retainedBonus);
                    }

                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    CommonRuneRules.ApplyImmunity(target, thisRune);
                },
            }
            .WithDrawnOnShieldTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneHoltrik", runeHoltrik);

        // BUG: Gunslinger's Fakeout triggers splash damage.
        Rune runeMarssyl = new Rune(
            "Marssyl, Rune of Impact",
            ModData.Traits.Marssyl,
            IllustrationName.ThunderingRunestone,
            1,
            "drawn on a bludgeoning weapon or unarmed Strike",
            "This rune magnifies force many times over as it passes through the rune's concentric rings.",
            "The weapon deals 1 bludgeoning splash damage per weapon damage die. If the weapon is a melee weapon, the rune-bearer is immune to this splash damage.",
            "The weapon vibrates as power concentrates within it. The next successful Strike made with the weapon before the end of its wielder's next turn deals an additional die of damage and the target must succeed at a Fortitude save against your class DC or be pushed 10 feet in a straight line backwards, or 20 feet on a critical failure.")
        {
            UsageCondition = (attacker, defender) =>
            {
                bool isAlly = defender.FriendOf(attacker);
                Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                return isAlly ? Usability.Usable : allyNotUsable; // Can always do Unarmed Strikes, so always drawable.
            },
            NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune? MakeMarssylPassive(Item targetItem)
                {
                    DrawnRune marssylPassive = new DrawnRune(
                        thisRune,
                        $"{thisRune.Name} ({targetItem.Name})",
                        $"The target item or bludgeoning unarmed strikes deal 1 bludgeoning splash damage per weapon damage die." +
                        (targetItem.HasTrait(Trait.Melee)
                            ? "\n\nMelee weapon: The rune-bearer is immune to this splash damage."
                            : null),
                        ExpirationCondition.Ephemeral,
                        caster,
                        thisRune.Illustration)
                    {
                        Source = caster,
                        Traits = [..thisRune.Traits], //[..thisRune.Traits],
                        AfterYouTakeAction = async (qfSelf, action) => // Add splash
                        {
                            if ((qfSelf as DrawnRune)!.Disabled)
                                return;
                            Item? qfItem = (qfSelf as DrawnRune)?.DrawnOn as Item;
                            Item? actionItem = action.Item;

                            // This many complex conditionals is really hard to work out so I did it the long way.
                            // Fail to splash if,
                            if (actionItem == null || qfItem == null || // either item is blank
                                !action.HasTrait(Trait.Strike) || // or the action isn't a strike
                                action.ChosenTargets == null ||
                                action.ChosenTargets.ChosenCreature == null || // or null targets
                                action.ChosenTargets.ChosenCreature ==
                                qfSelf.Owner || // or I'm my target for any reason
                                !actionItem.DetermineDamageKinds()
                                    .Contains(DamageKind.Bludgeoning)) // or it's not bludgeoning damage
                                return;
                            // Fail to splash if,
                            if (actionItem.HasTrait(Trait.Unarmed)) // attacking with an unarmed,
                            {
                                if (!qfItem.HasTrait(Trait.Unarmed)) // that is unbuffed.
                                    return;
                            }
                            else // attacking with a regular weapon,
                            {
                                if (actionItem != qfItem) // that is unbuffed.
                                    return;
                            }

                            // Fail to splash if not the first of multiple duplicate effects
                            if (qfSelf != qfSelf.Owner.QEffects.First(qf =>
                                    qf is DrawnRune dr && dr.Rune.RuneId == ModData.Traits.Marssyl))
                                return;

                            // Determine weapon damage dice count
                            string weaponDamageDiceCount = actionItem.WeaponProperties!.DamageDieCount.ToString();
                            if (action.TrueDamageFormula is { } trueDamage)
                            {
                                Capture diceCountCapture = Regex.Match(trueDamage.ToString(), @"(\d+)d\d+").Groups[1];
                                if (diceCountCapture.Value != "")
                                    weaponDamageDiceCount = diceCountCapture.Value;
                            }

                            DiceFormula splashAmount = DiceFormula.FromText(weaponDamageDiceCount, thisRune.Name);

                            // Make the strike magical while dealing splash damage (backfire mantle integration)
                            action.WithExtraTrait(Trait.Magical);

                            // If the strike at least failed,
                            if (action.CheckResult > CheckResult.CriticalFailure)
                            {
                                await CommonSpellEffects.DealDirectSplashDamage(
                                    action /*CombatAction.CreateSimple(qfSelf.Owner, "Marssyl")*/, splashAmount,
                                    action.ChosenTargets.ChosenCreature,
                                    DamageKind.Bludgeoning); // deal damage to the target.

                                if (action.CheckResult > CheckResult.Failure) // If the strike also at least succeeded,
                                {
                                    foreach (Creature target in qfSelf.Owner.Battle.AllCreatures.Where(cr =>
                                                 action.ChosenTargets.ChosenCreature
                                                     .IsAdjacentTo(cr))) // Loop through all adjacent creatures,
                                    {
                                        if (target != qfSelf.Owner ||
                                            !actionItem.HasTrait(Trait
                                                .Melee)) // And if it's a melee attack, skip me, otherwise include me when I,
                                            await CommonSpellEffects.DealDirectSplashDamage(action, splashAmount,
                                                target, DamageKind.Bludgeoning); // splash them too.
                                    }
                                }
                            }

                            // Make the strike no longer magical
                            action.Traits.Remove(Trait.Magical);
                        },
                    }.WithItemOrUnarmedRegulator(targetItem);

                    return marssylPassive;
                }

                // Target a specific item
                List<string> validItemsString = [];
                List<Item> validItems = [];
                foreach (Item item in target.HeldItems.Where(item =>
                             item.WeaponProperties != null && item.WeaponProperties.DamageKind != null &&
                             item.DetermineDamageKinds().Contains(DamageKind.Bludgeoning)))
                {
                    validItemsString.Add(item.Name);
                    validItems.Add(item);
                }

                validItems.Add(target.UnarmedStrike);
                validItemsString.Add("unarmed strikes");

                if (sourceAction?.Target is AreaTarget)
                {
                    foreach (DrawnRune? marssylPassive in validItems.Select(MakeMarssylPassive))
                    {
                        if (marssylPassive == null)
                            continue;
                        
                        if (sourceAction.HasTrait(ModData.Traits.Etched))
                            marssylPassive.WithIsEtched();
                        else if (sourceAction.HasTrait(ModData.Traits.Traced))
                            marssylPassive.WithIsTraced();

                        target.AddQEffect(marssylPassive);
                    }
                    // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                    return new DrawnRune(thisRune);
                }

                ArgumentNullException.ThrowIfNull(caster);
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    $"{{b}}{sourceAction?.Name}{{/b}}\nWhich item, or unarmed strikes, would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                    validItemsString.ToArray()
                );

                Item targetItem = validItems[chosenOption.Index];

                return MakeMarssylPassive(targetItem);
            },
            InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                {
                    QEffect invokedMarssyl = invokedRune.NewInvocationEffect(
                        $"The next successful Strike made with {(invokedRune.DrawnOn as Item)?.Name} before the end of your next turn deals an additional die of damage, and the target must succeed at a Fortitude save against your class DC or be pushed 10 feet in a straight line backwards, or 20 feet on a critical failure.",
                        ExpirationCondition.ExpiresAtEndOfYourTurn);
                    invokedMarssyl.Tag = invokedRune.DrawnOn as Item;
                    invokedMarssyl.IncreaseItemDamageDieCount = (qfSelf, item) =>
                    {
                        Item? tagItem = qfSelf.Tag as Item;
                        return tagItem != null
                               && (item == tagItem ||
                                   (item.HasTrait(Trait.Unarmed) && tagItem.HasTrait(Trait.Unarmed)));
                    };
                    invokedMarssyl.AfterYouTakeAction = async (qfSelf, action) =>
                    {
                        Item? qfItem = qfSelf.Tag as Item;
                        Item? actionItem = action.Item;
                        // Do invoke effect if:
                        if (actionItem != null && qfItem != null && // stuff isn't null,
                            (actionItem == qfItem ||
                             (actionItem.HasTrait(Trait.Unarmed) &&
                              qfItem.HasTrait(Trait.Unarmed))) && // and the items match or are any unarmed strikes
                            action.HasTrait(Trait.Strike) && // the action is a strike,
                            action.ChosenTargets.ChosenCreature != null && // the action targets a creature,
                            action.CheckResult >= CheckResult.Success) // and it at least succeeds.
                        {
                            action.Owner.RemoveAllQEffects(qfToRemove => qfToRemove == qfSelf);
                            CheckResult result = CommonSpellEffects.RollSavingThrow(action.ChosenTargets.ChosenCreature,
                                CombatAction.CreateSimple(action.Owner, $"Invoked {thisRune.Name}"), Defense.Fortitude,
                                RunesmithClass.RunesmithDC(action.Owner));
                            int tilePush = result <= CheckResult.Failure
                                ? (result == CheckResult.CriticalFailure ? 4 : 2)
                                : 0;
                            Sfxs.Play(ModData.SfxNames.InvokedMarssylShove);
                            await action.Owner.PushCreature(action.ChosenTargets.ChosenCreature, tilePush);
                        }
                    };

                    target.AddQEffect(invokedMarssyl);
                }

                CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                CommonRuneRules.ApplyImmunity(target, thisRune);
            },
        };
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneMarssyl", runeMarssyl);

        Rune runeOljinex = new Rune(
                "Oljinex, Rune of Cowards' Bane",
                ModData.Traits.Oljinex,
                IllustrationName.FearsomeRunestone,
                1,
                "drawn on a shield",
                "This rune resembles a broken arrow.",
                "While the shield is raised, it also grants the bearer a +1 status bonus to AC against physical ranged attacks. {i}(NYI: doesn't check for damage types, works against any ranged attack.){/i}",
                "(illusion, mental, visual) The rune creates an illusion in the minds of all creatures adjacent to the rune-bearer that lasts for 1 round. The illusion is of a impeding terrain. Creatures affected by this invocation must succeed at a DC 5 flat check when they take a move action or else it's lost. The DC is 11 instead if they attempt to move further away from the rune-bearer. This lasts for 1 round or until they disbelieve the illusion by using a Seek action against your class DC.",
                additionalTraits: [Trait.Occult])
            {
                UsageCondition = (attacker, defender) =>
                {
                    bool hasShield = defender.HeldItems.Any(item => item.HasTrait(Trait.Shield));
                    Usability shieldNotUsable = Usability.NotUsableOnThisCreature("doesn't have a shield");
                    bool isAlly = defender.FriendOf(attacker);
                    Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                    if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.OljinexOnEnemies))
                        return hasShield ? Usability.Usable : shieldNotUsable;
                    return isAlly ? (hasShield ? Usability.Usable : shieldNotUsable) : allyNotUsable;
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune? MakeOljinexPassive(Item targetItem)
                    {
                        DrawnRune drawnOljinex = new DrawnRune(
                            thisRune,
                            $"{thisRune.Name} ({targetItem.Name})",
                            "While the shield is raised, you have a +1 status bonus to AC against physical ranged attacks.",
                            ExpirationCondition.Ephemeral,
                            caster,
                            thisRune.Illustration)
                        {
                            Traits = [..thisRune.Traits],
                            BonusToDefenses = (qfSelf, attackAction, targetDefense) =>
                            {
                                if ((qfSelf as DrawnRune)!.Disabled)
                                    return null;

                                // TODO: enforce the physical damage part of ranged physical attacks.
                                if (targetDefense != Defense.AC ||
                                    (attackAction != null && !attackAction.HasTrait(Trait.Ranged)) ||
                                    !qfSelf.Owner.HasEffect(QEffectId.RaisingAShield))
                                    return null;

                                return new Bonus(1, BonusType.Status, "Oljinex (raised shield)");
                            },
                        }.WithItemRegulator(targetItem);

                        return drawnOljinex;
                    }

                    // Target a specific item
                    switch (target.HeldItems.Count(item => item.HasTrait(Trait.Shield)))
                    {
                        case 0:
                            return null;
                        case 1:
                            return MakeOljinexPassive(target.HeldItems.First(item => item.HasTrait(Trait.Shield)));
                        default:
                            if (sourceAction?.Target is AreaTarget)
                            {
                                foreach (DrawnRune? oljinexPassive in target.HeldItems.Select(MakeOljinexPassive))
                                {
                                    if (oljinexPassive == null)
                                        continue;
                                    
                                    // Determine the way the rune is being applied.
                                    if (sourceAction.HasTrait(ModData.Traits.Etched))
                                        oljinexPassive.WithIsEtched();
                                    else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                        oljinexPassive.WithIsTraced();

                                    target.AddQEffect(oljinexPassive);
                                }

                                return
                                    new DrawnRune(
                                        thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                            }

                            Item targetItem = await target.Battle.AskForConfirmation(
                                caster ?? throw new ArgumentNullException(nameof(caster)),
                                IllustrationName.MagicWeapon,
                                $"{{b}}{sourceAction?.Name}{{/b}}\nWhich shield would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                                target.HeldItems[0].Name, target.HeldItems[1].Name)
                                ? target.HeldItems[0]
                                : target.HeldItems[1];

                            return MakeOljinexPassive(targetItem);
                    }
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    // Define this in case it needs to change for behavioral logic
                    Tile cannotMoveAwayFrom = target.Occupies; //invokedRune.Owner.Occupies;

                    foreach (Creature cr in target.Battle.AllCreatures.Where(cr =>
                                 cr.IsAdjacentTo(target) && !CommonRuneRules.IsImmuneToThisInvocation(target, thisRune)))
                    {
                        if (cr.IsImmuneTo(Trait.Illusion) || cr.IsImmuneTo(Trait.Mental) || cr.IsImmuneTo(Trait.Visual))
                            continue;

                        Sfxs.Play(ModData.SfxNames.InvokedOljinex);

                        QEffect invokedOljinex = invokedRune.NewInvocationEffect(
                            "INCOMPLETE TEXT. COMPLAIN AT ANASE IF YOU SEE THIS TEXT!",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn);
                        invokedOljinex.CountsAsADebuff = true;

                        /* Oljinex can't reliably work as written. Here's some designs instead. */

                        // Ojinex prevents steps?
                        /*invokedOljinex.Description = $"You can't Step. You can attempt to disbelieve the illusion.";
                        invokedOljinex.PreventTakingAction = action => action.ActionId == ActionId.Step ? "Oljinex, Rune of Cowards' Bane" : null;*/

                        // Oljinex prevents steps and fizzles move actions with a flat check?
                        /*invokedOljinex.Description = $"You can't Step. If you attempt a move action, you must succeed at a DC 5 flat check or it is lost. You can attempt to disbelieve the illusion.";
                        invokedOljinex.PreventTakingAction = action => action.ActionId == ActionId.Step ? "Oljinex, Rune of Cowards' Bane" : null;
                        invokedOljinex.FizzleOutgoingActions = async (qfThis, action, stringBuilder) =>
                        {
                            if (action.ActionId != ActionId.Stride || !action.HasTrait(Trait.Move))
                                return false;
                            var result = Checks.RollFlatCheck(5);
                            stringBuilder.AppendLine($"Use move action while debuffed: {result.Item2}");
                            return result.Item1 < CheckResult.Success;
                        };*/

                        // Oljinex fizzles move actions that target too far away?
                        /*invokedOljinex.FizzleOutgoingActions = async (qfThis, action, stringBuilder) =>
                        {
                            // Define this in case it needs to change for behavioral logic
                            Tile cannotMoveAwayFrom = invokedRune.Owner.Occupies;//target.Occupies;

                            if (!action.HasTrait(Trait.Move) || action.ChosenTargets.ChosenTile is not { } chosenTile)
                                return false;
                            if (cannotMoveAwayFrom.DistanceTo(chosenTile) <= qfThis.Owner.DistanceTo(cannotMoveAwayFrom))
                                return false;
                            stringBuilder.AppendLine("Cannot willingly move away");
                            return true;
                        };*/

                        // Oljinex actually tries to create ephemeral walls?
                        /*invokedOljinex.Tag = new Dictionary<Tile,TileKind>(); // Tiles which have been modified
                        invokedOljinex.StateCheck = qfThis =>
                        {
                            foreach (Tile tile in qfThis.Owner.Battle.Map.AllTiles)
                            {
                                if (tile.DistanceTo(target.Occupies) >
                                    qfThis.Owner.Occupies.DistanceTo(target.Occupies))
                                {
                                    (qfThis.Tag as Dictionary<Tile,TileKind>)![tile] = tile.Kind;
                                    tile.Kind = TileKind.Wall;
                                }
                            }
                        };
                        invokedOljinex.WhenExpires = qfThis =>
                        {
                            foreach (var tile in ((qfThis.Tag as Dictionary<Tile,TileKind>)!))
                            {
                                tile.Key.Kind = tile.Value;
                            }
                        };*/

                        // Oljinex reduces speed and prevents steps?
                        /*invokedOljinex.PreventTakingAction = action => action.ActionId == ActionId.Step ? "Oljinex, Rune of Cowards' Bane" : null;
                        invokedOljinex.BonusToAllSpeeds = qfThis =>
                        {
                            if (qfThis.Tag is true)
                                return null; // Prevent infinite recursion
                            qfThis.Tag = true;
                            qfThis.Owner.RecalculateLandSpeedAndInitiative();
                            qfThis.Tag = false;
                            int penalty = - (qfThis.Owner.Speed - 1);
                            return new Bonus(penalty, BonusType.Untyped, "Oljinex");
                        };*/

                        // Oljinex prevents step and normal stride, replaces stride?
                        /*invokedOljinex.PreventTakingAction = action => /*action.ActionId == ActionId.Step ||#1# (action.ActionId == ActionId.Stride && action.Tag != invokedOljinex) ? "Oljinex, Rune of Cowards' Bane" : null;
                        invokedOljinex.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                        {
                            if (section.Name != "Invisible actions")
                                return null;

                            // locals
                            Creature self = qfThis.Owner;
                            Func<Creature, Tile, Usability> legality = (cr, t) => cr.DistanceTo(t) <= cr.DistanceTo(cannotMoveAwayFrom) ? Usability.Usable : Usability.NotUsable("Oljinex, Rune of Cowards' Bane");

                            // Requires copying the basic Stride.
                            CombatAction oljinexStride = new CombatAction(
                                    self,
                                    IllustrationName.None,
                                    self.HasEffect(QEffectId.Flying) ? "Fly" : "Stride",
                                    [Trait.Move, Trait.Basic],
                                    "Move up to your Speed.",
                                    Target.Tile(
                                            (cr, t) => t.LooksFreeTo(cr),
                                            (_, _) =>
                                                int.MinValue)
                                        .WithAdditionalTargetingRequirement(legality)
                                        .WithPathfindingGuidelines(cr =>
                                            new PathfindingDescription { Squares = 2, Style = new MovementStyle() {MaximumSquares = 2} })) //cr.Speed }))
                                .WithActionId(ActionId.Stride)
                                .WithActionCost(!self.Actions.NextStrideIsFree ? 1 : 0)
                                .WithEffectOnChosenTargets(async (action, self2, targets) =>
                                    await self2.MoveToUsingEarlierFloodfill(targets.ChosenTile, action,
                                        new MovementStyle()
                                        {
                                            MaximumSquares = 2, //self2.Speed,
                                            Shifting = self2.HasEffect(QEffectId.Mobility) && !targets.ChosenTile
                                                .InIteration.RequiresProvokingAttackOfOpportunity
                                        }));
                            oljinexStride.Tag = invokedOljinex;

                            return new ActionPossibility(oljinexStride);
                        };*/

                        // Oljinex updates normal moves?
                        /*invokedOljinex.StateCheck = async qfThis => // StateCheck is run before possibilities are generated.
                        {
                            (qfThis.Tag as List<CombatAction>)?.Clear(); // Just in case extra garbage collection is needed.
                            qfThis.Tag = new List<CombatAction>();
                        };
                        invokedOljinex.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                        {
                            // This gets called after a possibility is generated.
                            // Gets called multiple times, so make sure not to do too much recursion.
                            // That's what the StateCheck and .Tag is used for.

                            Creature self = qfThis.Owner;
                            Func<Creature, Tile, bool> isNotTooFar = (cr, t) => cr.DistanceTo(t) <= cr.DistanceTo(cannotMoveAwayFrom);

                            Possibilities movePossibilities = self.Possibilities.Filter(possibility =>
                                possibility.CombatAction.HasTrait(Trait.Move) && possibility.CombatAction.Target is TileTarget);
                            foreach (PossibilitySection allSection in movePossibilities.Sections)
                            {
                                foreach (ActionPossibility movePossibility in allSection.Possibilities.Where(possibility => possibility is ActionPossibility).Cast<ActionPossibility>())
                                {
                                    CombatAction moveAction = movePossibility.CombatAction;

                                    List<CombatAction>? alreadyModified = (qfThis.Tag as List<CombatAction>);
                                    if (alreadyModified == null || alreadyModified.Contains(moveAction))
                                        continue;

                                    TileTarget oldTarget = (moveAction.Target as TileTarget)!; // Checked during .Filter().
                                    Func<Creature, Tile, Usability>? oldExtraPrereq =
                                        oldTarget.AdditionalTargetingRequirement;
                                    moveAction.Target = oldTarget.WithAdditionalTargetingRequirement(
                                        (cr, t) =>
                                        {
                                            if (oldExtraPrereq != null)
                                                return oldExtraPrereq.Invoke(cr, t) && isNotTooFar.Invoke(cr, t) ? Usability.Usable : Usability.CommonReasons.NotUsableForComplexReason;
                                            else
                                                return isNotTooFar.Invoke(cr, t) ? Usability.Usable : Usability.CommonReasons.NotUsableForComplexReason;
                                        });

                                    alreadyModified.Add(moveAction);
                                }
                            }

                            return null;
                        };*/

                        // Oljinex fizzles move actions with a flat check, or greater if that target is too far away?
                        invokedOljinex.Description =
                            $"If you attempt a move action, you must succeed at a DC 5 flat check or it is lost. The DC is 11 instead if you attempt to move further away from {{Blue}}{target.Name}{{/Blue}}.";
                        invokedOljinex.FizzleOutgoingActions = async (qfThis, action, stringBuilder) =>
                        {
                            if (!action.HasTrait(Trait.Move))
                                return false;

                            // Define this in case it needs to change for behavioral logic
                            Tile cannotMoveFrom = invokedRune.Owner.Occupies; //target.Occupies;
                            int flatDC = 5;
                            if (action.ChosenTargets.ChosenTile is { } chosenTile
                                && cannotMoveFrom.DistanceTo(chosenTile) >
                                cannotMoveFrom.DistanceTo(qfThis.Owner.Occupies))
                                flatDC = 11;
                            (CheckResult, string) result = Checks.RollFlatCheck(flatDC);
                            stringBuilder.AppendLine($"Moved while debuffed: {result.Item2}" +
                                                     $"\n\n{{b}}{flatDC} DC breakdown:\n5{{/b}} Flat DC" +
                                                     (flatDC == 11 ? "\n{b}{Red}+6{/Red}{/b} moved further away" : ""));

                            if (result.Item1 < CheckResult.Success)
                                return true;

                            // Certain basic actions don't reach the code block where this log is announced,
                            // so this manually announces them anyway.
                            if (action.Name is "Stride" or "Step" or "Fly")
                                action.Owner.Battle.Log("Flat check passed.", action.Name, stringBuilder.ToString());

                            return false;
                        };

                        // Seek to disbelieve the illusion.
                        invokedOljinex.ProvideContextualAction = qfThis =>
                        {
                            CombatAction seekOljinex = new CombatAction(
                                    qfThis.Owner,
                                    new SideBySideIllustration(IllustrationName.FearsomeRunestone,
                                        IllustrationName.Seek),
                                    "Disbelieve Oljinex",
                                    [
                                        Trait.Basic,
                                        Trait.IsNotHostile,
                                        Trait.DoesNotBreakStealth,
                                        Trait.UsesPerception
                                    ],
                                    "Attempt to disbelieve Oljinex' illusory terrain with a Seek action against the caster's class DC.",
                                    Target.Self(
                                        (cr, ai) => int.MinValue)) // TODO: encourage the action?
                                .WithActiveRollSpecification(new ActiveRollSpecification(
                                    Checks.Perception(),
                                    (action, attacker, defender) =>
                                        new CalculatedNumber(RunesmithClass.RunesmithDC(defender!), "Class DC", [])))
                                .WithActionId(ActionId.Seek)
                                .WithActionCost(1)
                                .WithEffectOnEachTarget(async (thisAction, caster2, target2, result) =>
                                {
                                    if (result > CheckResult.Failure)
                                        caster2.RemoveAllQEffects(qf => qf == qfThis);
                                });
                            return new ActionPossibility(seekOljinex, PossibilitySize.Full).WithPossibilityGroup(
                                "remove debuff");
                        };

                        cr.AddQEffect(invokedOljinex);
                        CommonRuneRules.ApplyImmunity(cr, thisRune);
                    }

                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                },
            }
            .WithDrawnOnShieldTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneOljinex", runeOljinex);

        Rune runePluuna = new Rune(
                "Pluuna, Rune of Illumination",
                ModData.Traits.Pluuna,
                IllustrationName.HolyRunestone,
                1,
                "drawn on a creature", // or armor
                "While many runes are enchanted to glow, light is the focus of this simple rune.",
                "This rune sheds a revealing light in a 20-foot emanation. Creatures inside it take a -1 item penalty to Stealth checks, and the rune-bearer can't be undetected.",
                "Each creature in the emanation must succeed at a Fortitude save or be dazzled for 1 round. The light fades, but leaves behind a dim glow which prevents the target from being undetected for 1 round.",
                additionalTraits: [Trait.Light])
            {
                InvokeTechnicalTraits =
                [
                    //Trait.DoesNotRequireAttackRollOrSavingThrow, // Debatable. The target does save, but so does everyone else.
                ],
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    const float emanationSize = 4f; // 20 feet

                    DrawnRune pluunaPassive = new DrawnRune(
                        thisRune,
                        thisRune.Name,
                        "Can't become undetected, and all creatures in a 20-foot emanation takes a -1 item penalty to Stealth checks.",
                        ExpirationCondition.Ephemeral,
                        caster,
                        thisRune.Illustration)
                    {
                        SpawnsAura = qfThis =>
                        {
                            return new
                                MagicCircleAuraAnimation(IllustrationName.AngelicHaloCircle,
                                    Microsoft.Xna.Framework.Color.Gold, emanationSize);
                        },
                        StateCheck = qfThis =>
                        {
                            qfThis.Owner.DetectionStatus.Undetected = false;
                            qfThis.Owner.Battle.AllCreatures
                                .Where(cr =>
                                    cr.DistanceTo(qfThis.Owner) <= emanationSize)
                                .ForEach(cr =>
                                    cr.AddQEffect(new QEffect("Pluuna's Light",
                                        "You have a -1 item penalty to Stealth checks.", ExpirationCondition.Ephemeral,
                                        qfThis.Owner, IllustrationName.Light)
                                    {
                                        BonusToSkills = skill => skill == Skill.Stealth
                                            ? new Bonus(-1, BonusType.Item, thisRune.Name)
                                            : null
                                    }));
                        },
                    };

                    return pluunaPassive;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    const float emanationSize = 4f; // 20 feet

                    // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                    CombatAction invokePluunaOnEveryone = new CombatAction(
                            target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                            thisRune.Illustration,
                            $"Invoke {thisRune.Name}",
                            new List<Trait>(thisRune.Traits).Append(Trait.DoNotShowInCombatLog).ToArray(),
                            thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                            Target.Emanation((int)emanationSize))
                        .WithActionCost(0)
                        .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                        .WithSoundEffect(ModData.SfxNames.InvokedPluuna)
                        .WithSavingThrow(new SavingThrow(Defense.Fortitude, RunesmithClass.RunesmithDC(caster)))
                        .WithNoSaveFor((thisAction, cr) => CommonRuneRules.IsImmuneToThisInvocation(cr, thisRune))
                        .WithEffectOnEachTarget(async (selfAction, invokeEE, invokedOnto, result) =>
                        {
                            /*foreach (Creature cr in caster.Battle.AllCreatures.Where(cr => cr.DistanceTo(target) <= emanationSize))
                            {
                                if (thisRune.IsImmuneToThisInvocation(cr))
                                    continue;
                            
                                CheckResult result = CommonSpellEffects.RollSavingThrow(cr, sourceAction, Defense.Fortitude, caster.ClassOrSpellDC());
                                if (result <= CheckResult.Failure)
                                {
                                    cr.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 1));
                                }
                            
                                thisRune.ApplyImmunity(cr);
                            }*/

                            if (!CommonRuneRules.IsImmuneToThisInvocation(invokedOnto, thisRune))
                            {
                                if (result <= CheckResult.Failure)
                                {
                                    invokedOnto.AddQEffect(QEffect.Dazzled()
                                        .WithExpirationAtStartOfSourcesTurn(caster, 1));
                                }

                                CommonRuneRules.ApplyImmunity(invokedOnto, thisRune);
                            }
                        });

                    if (await caster.Battle.GameLoop.FullCast(
                            invokePluunaOnEveryone /*, ChosenTargets.CreateSingleTarget(target)*/))
                    {
                        QEffect invokedPluuna = invokedRune.NewInvocationEffect(
                            "This dim light prevents you from being undetected.",
                            ExpirationCondition.ExpiresAtStartOfSourcesTurn);
                        invokedPluuna.StateCheck = qfThis => { qfThis.Owner.DetectionStatus.Undetected = false; };

                        CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                        target.AddQEffect(invokedPluuna);
                    }
                    else
                        sourceAction.RevertRequested = true;
                },
            }
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RunePluuna", runePluuna);

        Rune runeRanshu = new Rune(
                "Ranshu, Rune of Thunder",
                ModData.Traits.Ranshu,
                IllustrationName.ShockRunestone,
                1,
                "drawn on a creature", // or object
                "This vertical rune is often carved on tall towers to draw lightning and shield the buildings below it.",
                "If the bearer does not take a move action at least once on its turn, lightning finds it at the end of its turn, dealing 1d4 electricity damage.",
                "The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes 2d6 electricity damage, with a basic Fortitude save.",
                "The damage increases by 1, and the damage of the invocation increases by 2d6.",
                [Trait.Electricity, Trait.Primal])
            {
                LevelFormat = "+2",
                PassiveTextWithHeightening = (thisRune, charLevel) =>
                {
                    int bonusDamage = (charLevel - thisRune.BaseLevel) / 2;
                    string damage = "1d4" +
                                    (bonusDamage > 0 ? $"+{S.HeightenedVariable(bonusDamage, 0)}" : null);
                    return
                        $"If the bearer does not take a move action at least once on its turn, lightning finds it at the end of its turn, dealing {damage} electricity damage.";
                },
                InvocationTextWithHeightening = (thisRune, charLevel) =>
                {
                    int numDice = 2 + (int)Math.Floor((charLevel - thisRune.BaseLevel) / 2d) * 2;
                    string heightenedVar = S.HeightenedVariable(numDice, 2);

                    return
                        $"The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes {heightenedVar}d6 electricity damage, with a basic Fortitude save.";
                },
                UsageCondition = (attacker, defender) =>
                {
                    return defender.EnemyOf(attacker)
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("not an enemy");
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    int bonusDamage = ((caster?.Level ?? 1) - thisRune.BaseLevel) / 2;
                    DiceFormula immobilityDamage = DiceFormula.FromText($"1d4+{bonusDamage}");
                    DrawnRune ranshuPassive = new DrawnRune(thisRune, thisRune.Name,
                        $"If you don't take a move action at least once during your turn, you take {immobilityDamage} electricity damage.")
                    {
                        Illustration = thisRune.Illustration,
                        Source = caster,
                        //Value = immobilityDamage.ExpectedValue, // Value might be an unnecessary field, aesthetically. // TODO: use Key field?
                        Traits = [..thisRune.Traits],
                        AfterYouTakeAction = async (qfThis, action) =>
                        {
                            if (action.HasTrait(Trait.Move))
                                qfThis.UsedThisTurn = true; // Has moved this turn
                        },
                        EndOfYourTurnDetrimentalEffect = async (qfThis, self) =>
                        {
                            if ((qfThis as DrawnRune)!.Disabled)
                                return;
                            if (qfThis.UsedThisTurn == true) // If you have moved this turn,
                                return; // don't take any damage.
                            await CommonSpellEffects.DealDirectDamage(null, immobilityDamage, self, CheckResult.Failure,
                                DamageKind.Electricity);
                            Sfxs.Play(ModData.SfxNames.PassiveRanshu);
                        },
                    };
                    return ranshuPassive;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                    {
                        int numDice = 2 + (int)Math.Floor((caster.Level - thisRune.BaseLevel) / 2d) * 2;
                        DiceFormula invocationDamage = DiceFormula.FromText($"{numDice}d6");
                        CheckResult result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Fortitude,
                            RunesmithClass.RunesmithDC(caster));
                        await CommonSpellEffects.DealBasicDamage(sourceAction, caster, target, result,
                            invocationDamage, DamageKind.Electricity);
                        Sfxs.Play(ModData.SfxNames.InvokedRanshu);
                    }

                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    CommonRuneRules.ApplyImmunity(target, thisRune);
                },
            }
            .WithDetrimentalPassiveTechnical()
            .WithDamagingInvocationTechnical()
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneRanshu", runeRanshu);

        Rune runeSunDiacritic = new Rune(
                "Sun-, Diacritic Rune of Preservation",
                ModData.Traits.SunDiacritic,
                IllustrationName.DisruptingRunestone,
                1,
                "drawn on a rune",
                "This spiraling diacritic channels the magic of a rune outwards, then back to the same location, allowing a rune to reconstitute itself.",
                "After the base rune is invoked, the rune automatically Traces itself back upon the same target.\n\n{b}Special{/b} You can have only one copy of {i}sun-, diacritic rune of preservation{/i} applied at a given time, and once you invoke it, you cannot Etch or Trace it again this combat.",
                additionalTraits: [ModData.Traits.Diacritic])
            {
                UsageCondition = (attacker, defender) =>
                {
                    if (attacker.PersistentUsedUpResources.UsedUpActions.Contains("SunDiacritic"))
                        return Usability.NotUsable("already invoked this combat");
                    if (DrawnRune.GetDrawnRunes(null, defender) is { } drawnRunes)
                        if (drawnRunes.Count == 0)
                            return Usability.NotUsableOnThisCreature("not a rune-bearer");
                        else if (drawnRunes.Any(dr => dr.AttachedDiacritic == null))
                            return Usability.Usable;
                        else
                            return Usability.NotUsableOnThisCreature("no valid runes");
                    return Usability.Usable;
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    // Only one instance allowed when drawn
                    foreach (Creature cr in target.Battle.AllCreatures)
                    {
                        cr.RemoveAllQEffects(qf => qf.Traits.Contains(ModData.Traits.SunDiacritic));
                        DrawnRune.GetDrawnRunes(caster, cr).ForEach(dr =>
                        {
                            if (dr.AttachedDiacritic != null &&
                                dr.AttachedDiacritic.Traits.Contains(ModData.Traits.SunDiacritic))
                                dr.AttachedDiacritic = null;
                        });
                    }

                    DrawnRune? CreateSunPassive(DrawnRune targetRune)
                    {
                        DrawnRune drawnSun = new DrawnRune(
                                thisRune,
                                $"{thisRune.Name} ({targetRune.Name})",
                                "The base rune is automatically Traced again after being invoked.",
                                ExpirationCondition.Ephemeral,
                                caster,
                                thisRune.Illustration)
                            {
                                Traits = [..thisRune.Traits],
                                BeforeInvokingRune = async (thisDr, sourceAction2, drInvoked) =>
                                {
                                    if (thisDr.Disabled)
                                        return;
                                    /*CombatAction sunRedraw = CombatAction.CreateSimple(
                                        drInvoked.Source!,
                                        "Sun, Diacritic Rune of Preservation",
                                        [ModData.Traits.Traced]); // <- Even if it WAS etched before, it's now traced.*/
                                    CombatAction sunRedraw = CommonRuneRules.CreateTraceAction(drInvoked.Source!, drInvoked.Rune, 0, 99);
                                    if (await CommonRuneRules.DrawRuneOnTarget(sunRedraw, thisDr.Source!,
                                            drInvoked.Owner, drInvoked.Rune, false) != null)
                                        Sfxs.Play(ModData.SfxNames.InvokedSun);
                                    thisDr.Source!.PersistentUsedUpResources.UsedUpActions.Add("SunDiacritic");
                                },
                            }
                            .WithDiacriticRegulator(targetRune);

                        return drawnSun;
                    }

                    // Target a specific rune
                    List<string> validRunesString = [];
                    List<DrawnRune> validRunes = [];
                    foreach (DrawnRune dr in DrawnRune.GetDrawnRunes(null, target)
                                 .Where(dr => dr.AttachedDiacritic == null))
                    {
                        validRunesString.Add(dr.Name ?? dr.ToString());
                        validRunes.Add(dr);
                    }

                    if (validRunes.Count == 0)
                        return null;

                    if (sourceAction?.Target is AreaTarget)
                    {
                        foreach (DrawnRune? sunPassive in validRunes.Select(CreateSunPassive))
                        {
                            if (sunPassive == null)
                                continue;
                            
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(ModData.Traits.Etched))
                                sunPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                sunPassive.WithIsTraced();

                            target.AddQEffect(sunPassive);
                        }
                        // Return an ephemeral DrawnRune since we just applied this to a whole batch of runes.
                        return new DrawnRune(thisRune); 
                    }
                    
                    ArgumentNullException.ThrowIfNull(caster);
                    ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{sourceAction?.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                        validRunesString.ToArray()
                    );

                    DrawnRune targetRune = validRunes[chosenOption.Index];

                    return CreateSunPassive(targetRune);
                }
            }
            .WithDrawnOnRuneTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneSunDiacritic", runeSunDiacritic);

        // TODO: Make final decision on whether this buffs Marssyl's invocation.
        Rune runeUrDiacritic = new Rune(
                "Ur-, Diacritic Rune of Intensity",
                ModData.Traits.UrDiacritic,
                IllustrationName.DemolishingRunestone,
                1,
                "drawn on a rune that deals damage",
                "This diacritic accentuates the base rune with bolder lines to give greater weight to its effects.",
                "When the base rune is invoked, its invocation gains a status bonus to damage equal to your Intelligence modifier.",
                additionalTraits: [ModData.Traits.Diacritic])
            {
                UsageCondition = (attacker, defender) =>
                {
                    if (DrawnRune.GetDrawnRunes(null, defender) is { } drawnRunes)
                        if (drawnRunes.Count == 0)
                            return Usability.NotUsableOnThisCreature("not a rune-bearer");
                        else if (drawnRunes.Any(dr =>
                                     dr.AttachedDiacritic == null &&
                                     dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile)))
                            return Usability.Usable;
                        else
                            return Usability.NotUsableOnThisCreature("no valid runes");
                    return Usability.Usable;
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune? CreateUrPassive(DrawnRune targetRune)
                    {
                        ArgumentNullException.ThrowIfNull(caster);
                        
                        DrawnRune drawnUr = new DrawnRune(
                            thisRune,
                            $"{thisRune.Name} ({targetRune.Name})",
                            $"The base rune's invocation damage gains a +{caster.Abilities.Intelligence} status bonus.",
                            ExpirationCondition.Ephemeral,
                            caster,
                            thisRune.Illustration)
                        {
                            Traits = [..thisRune.Traits],
                            BeforeInvokingRune = async (thisDr, sourceAction2, drInvoked) =>
                            {
                                if (thisDr.Disabled)
                                    return;
                                QEffect invokeBonus = new QEffect()
                                {
                                    // No expiration because it needs to exist longer for invocations such as Esvadir which have hidden subsidiaries going on
                                    // Is removed on its own when the rune is invoked, and it only applies to the same type, so it should be safe to manually expire that way in this callback structure.
                                    //ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                                    BonusToDamage = (qfThis, action, defender) =>
                                    {
                                        if (thisDr.Disabled)
                                            return null;

                                        if (!action.HasTrait(ModData.Traits.Invocation)
                                            || action.Tag is not Rune
                                            || drInvoked != thisDr.DrawnOn)
                                            return null;

                                        return new Bonus(caster.Abilities.Intelligence, BonusType.Status,
                                            "Ur, Diacritic Rune of Intensity");
                                    },
                                };
                                ArgumentNullException.ThrowIfNull(thisDr.Source);
                                thisDr.Source.AddQEffect(invokeBonus);
                            },
                        }.WithDiacriticRegulator(targetRune);

                        return drawnUr;
                    }

                    // Target a specific rune
                    List<string> validRunesString = [];
                    List<DrawnRune> validRunes = [];
                    foreach (DrawnRune dr in DrawnRune.GetDrawnRunes(null, target)
                                 .Where(dr =>
                                     dr.AttachedDiacritic == null &&
                                     dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile)))
                    {
                        validRunesString.Add(dr.Name ?? dr.ToString());
                        validRunes.Add(dr);
                    }

                    if (validRunes.Count == 0)
                        return null;

                    if (sourceAction?.Target is AreaTarget)
                    {
                        foreach (DrawnRune? urPassive in validRunes.Select(CreateUrPassive))
                        {
                            if (urPassive == null)
                                continue;
                            
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(ModData.Traits.Etched))
                                urPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                urPassive.WithIsTraced();

                            target.AddQEffect(urPassive);
                        }
                        // Return an ephemeral DrawnRune since we just applied this to a whole batch of runes.
                        return new DrawnRune(thisRune);
                    }
                    
                    ArgumentNullException.ThrowIfNull(caster);
                    ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{sourceAction?.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                        validRunesString.ToArray()
                    );

                    DrawnRune targetRune = validRunes[chosenOption.Index];

                    return CreateUrPassive(targetRune);
                }
            }
            .WithDrawnOnRuneTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneUrDiacritic", runeUrDiacritic);

        Rune runeZohk = new Rune(
                "Zohk, Rune of Homecoming",
                ModData.Traits.Zohk,
                IllustrationName.ReturningRunestone,
                1,
                "drawn on a creature",
                "This circular mark is meant to allow travelers to always find their way home.",
                "The target can Stride with a +15-foot status bonus, but only if their destination space is closer to you than when they started their turn.",
                "(teleportation) You call the rune-bearer to your side. You teleport the target to any unoccupied square adjacent to you. If the bearer is unwilling, they can attempt a Will save to negate the effect.",
                null,
                [Trait.Arcane])
            {
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune drawnZohk = new DrawnRune(
                        thisRune,
                        thisRune.Name,
                        $"You can Stride with a +15-foot status bonus if your destination space is closer to {caster?.Name ?? "(...)"}.",
                        ExpirationCondition.Ephemeral,
                        caster,
                        thisRune.Illustration)
                    {
                        Tag = target.Occupies,
                        StartOfYourEveryTurn = async (qfThis, self) =>
                        {
                            qfThis.Tag = self.Occupies;
                        },
                        ProvideContextualAction = qfThis =>
                        {
                            if ((qfThis as DrawnRune)!.Disabled || qfThis.Tag is not Tile startTile)
                                return null;

                            CombatAction zohkStride = new CombatAction(
                                    qfThis.Owner,
                                    new SideBySideIllustration(IllustrationName.FleetStep, thisRune.Illustration),
                                    "Stride (Zohk)",
                                    [Trait.Move],
                                    qfThis.Description!,
                                    Target
                                        .Self() // Behavior is somewhat unreliable. Removed since the world doesn't end if it immediately reverts.
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
                                .WithEffectOnSelf(async (thisAction, self) =>
                                {
                                    self.AddQEffect(new QEffect()
                                    {
                                        ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                                        BonusToAllSpeeds = qfSpeed => new Bonus(3, BonusType.Status, qfThis.Name!),
                                    });

                                    Creature runeCaster = qfThis.Source!;
                                    if (!await RunesmithClass.StrideCloserToEnemyAsync(qfThis.Owner, startTile, runeCaster,
                                            $"Stride closer to {runeCaster.Name} or right-click to cancel."))
                                    {
                                        thisAction.RevertRequested = true;
                                    }
                                });

                            return new ActionPossibility(zohkStride, PossibilitySize.Full);
                        },
                    };

                    return drawnZohk;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                    {
                        CheckResult result = CheckResult.Failure;
                        if (!target.FriendOf(caster))
                        {
                            result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Will,
                                RunesmithClass.RunesmithDC(caster));
                        }

                        if (result <= CheckResult.Failure)
                        {
                            List<Option> options = [];

                            // Populate options with empty adjacent tiles
                            foreach (Tile tile in caster.Battle.Map.Tiles)
                            {
                                if (tile.IsFree && tile.IsAdjacentTo(caster.Occupies))
                                {
                                    options.Add(new TileOption(tile, "Tile (" + tile.X + "," + tile.Y + ")",
                                        async () => {},
                                        int.MinValue, 
                                        true));
                                }
                            }

                            // Prompts the user for their desired tile and returns it or null
                            Option? selectedOption = (await caster.Battle.SendRequest(
                                new AdvancedRequest(caster, $"Choose a tile to teleport {target.Name} to.", options)
                                {
                                    IsMainTurn = false,
                                    IsStandardMovementRequest = false,
                                    TopBarIcon = thisRune.Illustration,
                                    TopBarText = $"Choose a tile to teleport {target.Name} to.",
                                })).ChosenOption;

                            if (selectedOption is TileOption selectedTile)
                            {
                                await CommonSpellEffects.Teleport(target, selectedTile.Tile);
                                Sfxs.Play(ModData.SfxNames.InvokedZohk);
                            }
                        }
                    }

                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    CommonRuneRules.ApplyImmunity(target, thisRune);
                }
            }
            .WithWillSaveInvocationTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneZohk", runeZohk);

        // Level 9 Runes
        Rune runeEnDiacritic = new Rune(
            "En-, Diacritic Rune of Expansion",
            ModData.Traits.EnDiacritic,
            IllustrationName.UnderwaterRunestone,
            9,
            "drawn on a rune that deals damage (and not already in an area)",
            "This diacritic surrounds a rune with outward-facing arrows to magnify and direct power outward.",
            "When the base rune is invoked, its damage applies in a 15-foot burst, centered on the rune-bearer. If any creatures are also within the area, they are subject to the base rune's effects (including any saving throw).",
            additionalTraits:[ModData.Traits.Diacritic])
            {
                UsageCondition = (attacker, defender) =>
                {
                    if (DrawnRune.GetDrawnRunes(null, defender) is { } drawnRunes)
                        if (drawnRunes.Count == 0)
                            return Usability.NotUsableOnThisCreature("not a rune-bearer");
                        else if (drawnRunes.Any(dr =>
                                     dr.AttachedDiacritic == null &&
                                     dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile) &&
                                     !dr.Rune.InvokeTechnicalTraits.Contains(Trait.Splash)))
                            return Usability.Usable;
                        else
                            return Usability.NotUsableOnThisCreature("no valid runes");
                    return Usability.Usable;
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    // Target a specific rune
                    List<string> validRunesString = [];
                    List<DrawnRune> validRunes = [];
                    foreach (DrawnRune dr in DrawnRune.GetDrawnRunes(null, target)
                                 .Where(dr =>
                                     dr.AttachedDiacritic == null
                                     && dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile)
                                     && !dr.Rune.InvokeTechnicalTraits.Contains(Trait.Splash)))
                    {
                        validRunesString.Add(dr.Name ?? dr.ToString());
                        validRunes.Add(dr);
                    }

                    if (validRunes.Count == 0)
                        return null;

                    if (sourceAction?.Target is AreaTarget)
                    {
                        foreach (DrawnRune? urPassive in validRunes.Select(CreateEnPassive))
                        {
                            if (urPassive == null)
                                continue;
                            
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(ModData.Traits.Etched))
                                urPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(ModData.Traits.Traced))
                                urPassive.WithIsTraced();

                            target.AddQEffect(urPassive);
                        }
                        // Return an ephemeral DrawnRune since we just applied this to a whole batch of runes.
                        return new DrawnRune(thisRune);
                    }
                    
                    ArgumentNullException.ThrowIfNull(caster);
                    ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{sourceAction?.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                        validRunesString.ToArray());

                    DrawnRune targetRune = validRunes[chosenOption.Index];

                    return CreateEnPassive(targetRune);

                    // Create rune
                    DrawnRune? CreateEnPassive(DrawnRune drawnOnto)
                    {
                        ArgumentNullException.ThrowIfNull(caster);
                        
                        DrawnRune drawnUr = new DrawnRune(
                            thisRune,
                            $"{thisRune.Name} ({drawnOnto.Name})",
                            "When the base rune is invoked, its damage applies in a 15-foot burst, centered on the rune-bearer. If any creatures are also within the area, they are subject to the base rune's effects (including any saving throw).",
                            ExpirationCondition.Never,
                            caster,
                            thisRune.Illustration)
                        {
                            Traits = [..thisRune.Traits],
                            BeforeInvokingRune = async (thisDr, sourceAction2, drInvoked) =>
                            {
                                if (thisDr.Disabled || drInvoked != thisDr.DrawnOn)
                                    return;

                                sourceAction2.WithExtraTrait(ModData.Traits.InvokeAgainstGivenTarget);

                                CombatAction emanationTargeting = new CombatAction(
                                    drInvoked.Owner,
                                    thisDr.Rune.Illustration,
                                    thisDr.Rune.Name,
                                    [..thisDr.Traits, Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName],
                                    "",
                                    Target.Emanation(3))
                                    .WithSoundEffect(ModData.SfxNames.InvokeRune)
                                    .WithProjectileCone(VfxStyle.BasicProjectileCone(drInvoked.Rune.Illustration))
                                    .WithActionCost(0);
                                if (!await sourceAction2.Owner.Battle.GameLoop.FullCast(emanationTargeting))
                                {
                                    sourceAction2.RevertRequested = true;
                                    return;
                                }
                                
                                var invocation = drInvoked.Rune.InvocationBehavior;
                                if (invocation != null)
                                    drInvoked.Owner.Battle.AllCreatures.Where(cr => cr != drInvoked.Owner && cr.DistanceTo(drInvoked.Owner) <= 3).ForEach(async void (cr) =>
                                    {
                                        await invocation.Invoke(sourceAction2, drInvoked.Rune, sourceAction2.Owner, cr, drInvoked);
                                    });
                            },
                        }.WithDiacriticRegulator(drawnOnto);

                        return drawnUr;
                    }
                },
            }
            .WithDrawnOnRuneTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneEnDiacritic", runeEnDiacritic);
        
        Rune runeFeikris = new Rune(
            "Feikris, Rune of Gravity",
            ModData.Traits.Feikris,
            IllustrationName.ResilientRunestone,
            9,
            "drawn on a creature" /* or armor"*/,
            "The lines of this rune overlap strangely, making it seem larger than it really is.",
            "The rune-bearer gains a +2 item bonus to Athletics checks." /* and gains the benefits of the Titan Wrestler feat.*/,
            invocationText:
            "All creatures in a 15-foot emanation around the rune-bearer must succeed at a Fortitude save or be pulled 5 feet towards the rune-bearer (or 10 feet on a critical failure).",
            /*levelText: "The item bonus increases to +3.",*/
            additionalTraits: [Trait.Arcane])
            {
                //LevelFormat = "17th",
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune feikrisPassive = new DrawnRune(
                        thisRune,
                        "Feikris, Rune of Gravity",
                        "You have a +2 item bonus to Athletics checks.")
                    {
                        Illustration = thisRune.Illustration,
                        Source = caster,
                        Traits = [..thisRune.Traits],
                        BonusToSkills = skill =>
                            skill is Skill.Athletics ? new Bonus(caster?.Level == 17 ? 3 : 2, BonusType.Item, "Feikris") : null,
                    };
                    return feikrisPassive;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                    CombatAction invokeFeikrisOnEveryone = new CombatAction(
                            target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                            thisRune.Illustration,
                            $"Invoke {thisRune.Name}",
                            new List<Trait>(thisRune.Traits).Append(Trait.DoNotShowInCombatLog).ToArray(),
                            thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                            Target.Emanation(3)
                                .WithIncludeOnlyIf((tar, cr) => cr != target))
                        .WithActionCost(0)
                        .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                        .WithSoundEffect(ModData.SfxNames.InvokedFeikris)
                        .WithSavingThrow(new SavingThrow(Defense.Fortitude, RunesmithClass.RunesmithDC(caster)))
                        .WithNoSaveFor((thisAction, cr) => cr == target || CommonRuneRules.IsImmuneToThisInvocation(cr, thisRune))
                        .WithEffectOnEachTarget(async (selfAction, invokeEE, invokedOnto, result) =>
                        {
                            if (!CommonRuneRules.IsImmuneToThisInvocation(invokedOnto, thisRune))
                            {
                                int distance = result switch
                                {
                                    CheckResult.CriticalFailure => 2,
                                    CheckResult.Failure => 1,
                                    _ => 0
                                };
                                if (distance > 0)
                                    await RunesmithClass.PullCreatureByDistance(target, invokedOnto, distance);
                                CommonRuneRules.ApplyImmunity(invokedOnto, thisRune);
                            }
                        });

                    if (await caster.Battle.GameLoop.FullCast(invokeFeikrisOnEveryone))
                        CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    else
                        sourceAction.RevertRequested = true;
                },
            }
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneFeikris", runeFeikris);

        /*Rune runeIchelsu = new Rune(
            "Ichelsu, Rune of Observation",
            ModData.Traits.Ichelsu,
            IllustrationName.GhostTouchRunestone,
            9,
            "drawn on a creature",
            "A ring of dotted circles, this rune allows a creature marked with it to see all.",
            $"The target is affected by {AllSpells.CreateSpellLink(SpellId.SeeInvisibility, ModData.Traits.Runesmith).Replace("see invisibility", "see the unseen")} and gains {{tooltip:Runesmith.Misc.AllAroundVision}}all-around vision{{/}}.",
            invocationText: "The eyes of the rune fly outwards, attaching to all creatures in a 20-foot emanation. Each of these creatures that was invisible becomes concealed instead, and each one that was concealed for any other reason is no longer concealed. This effect lasts for 2 rounds.",
            additionalTraits:[Trait.Occult])
            {
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    QEffect see = QEffect.SeeInvisibility();
                    QEffect vision = QEffect.AllAroundVision();
                    DrawnRune ichelsuPassive = new DrawnRune(
                        thisRune,
                        thisRune.Name,
                        "You gain the effects of see the unseen and all-around vision.",
                        ExpirationCondition.Never,
                        caster,
                        thisRune.Illustration)
                    {
                        StateCheck = async qfThis =>
                        {
                            if (!qfThis.Owner.HasEffect(see))
                                qfThis.Owner.AddQEffect(see);
                            if (!qfThis.Owner.HasEffect(vision))
                                qfThis.Owner.AddQEffect(vision);
                        },
                        WhenExpires = async qfThis =>
                        {
                            see.ExpiresAt = ExpirationCondition.Immediately;
                            vision.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    };
                    return ichelsuPassive;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                    CombatAction invokeIchelsuOnEveryone = new CombatAction(
                            target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                            thisRune.Illustration,
                            $"Invoke {thisRune.Name}",
                            new List<Trait>(thisRune.Traits).Append(Trait.DoNotShowInCombatLog).ToArray(),
                            thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                            Target.Emanation(4))
                        .WithActionCost(0)
                        .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                        .WithSoundEffect(ModData.SfxNames.InvokedIchelsu)
                        .WithNoSaveFor((thisAction, cr) => cr == target || CommonRuneRules.IsImmuneToThisInvocation(cr, thisRune))
                        .WithEffectOnEachTarget(async (selfAction, invokeEE, invokedOnto, result) =>
                        {
                            if (!CommonRuneRules.IsImmuneToThisInvocation(invokedOnto, thisRune))
                            {
                                invokedOnto.DetectionStatus.HiddenTo.Clear();
                                invokedOnto.DetectionStatus.Undetected = false;
                                QEffect invokedIchelsu = invokedRune.NewInvocationEffect(
                                    "If you are invisible, you are concealed instead. If you were concealed for any other reason, you are no longer concealed.",
                                    ExpirationCondition.Never)
                                    .WithExpirationAtStartOfSourcesTurn(caster, 2);
                                QEffect faerie = QEffect.FaerieFire(invokedIchelsu.Name!, IllustrationName.None)
                                    .WithExpirationEphemeral();
                                invokedIchelsu.StateCheck = async qfThis =>
                                {
                                    if (!qfThis.Owner.HasEffect(faerie))
                                        qfThis.Owner.AddQEffect(faerie);
                                };
                                invokedIchelsu.WhenExpires = async qfThis =>
                                {
                                    faerie.ExpiresAt = ExpirationCondition.Immediately;
                                };
                                invokedOnto.AddQEffect(invokedIchelsu);
                                CommonRuneRules.ApplyImmunity(invokedOnto, thisRune);
                            }
                        });

                    if (await caster.Battle.GameLoop.FullCast(invokeIchelsuOnEveryone))
                        CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    else
                        sourceAction.RevertRequested = true;
                },
            };
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneIchelsu", runeIchelsu);*/

        Rune runeJurroz = new Rune(
            "Jurroz, Rune of Dragon Fury",
            ModData.Traits.Jurroz,
            IllustrationName.CorrosiveRunestone,
            9,
            "etch onto a creature", //or armor",
            "This angular rune channels the fury of dragon kind.",
            "Whenever a creature Strikes the rune-bearer, draconic sanction fully focuses on them, causing them to become off-guard for 1 round.",
            invocationText: "As a {icon:FreeAction} free action, the rune-bearer can Fly up to 60 feet toward a creature that has damaged them in the last minute. If they end this movement adjacent to the creature, the creature becomes off-guard until the end of the rune-bearer's next turn.",
            additionalTraits: [Trait.Dragon])
            {
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    DrawnRune jurrozPassive = new DrawnRune(
                        thisRune,
                        "Jurroz, Rune of Dragon Fury",
                        "Whenever a creature Strikes you, you become off-guard for 1 round.")
                    {
                        Illustration = thisRune.Illustration,
                        Source = caster,
                        Traits = [..thisRune.Traits],
                        AfterYouTakeDamage = async (qfThis, amount, kind, action, critical) =>
                        {
                            if (action == null || !action.HasTrait(Trait.Strike))
                                return;
                            
                            QEffect jurrozFooted = QEffect.FlatFooted("Jurroz, Rune of Dragon Fury")
                                .WithExpirationAtStartOfSourcesTurn(action.Owner, 1);
                            jurrozFooted.Key = "RunesmithPlaytest.JurrozPassive";
                            qfThis.Owner.AddQEffect(jurrozFooted);
                        },
                    };
                    return jurrozPassive;
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                    {
                        if (caster.FindQEffect(ModData.QEffectIds.JurrozDamageTracker) is not { Tag: Dictionary<Creature, List<Creature>> damageHistory } || !damageHistory.TryGetValue(target, out var damagedBy))
                            return;
                        
                        // Add flying effects and ensure you have enough speed
                        QEffect bigFly = QEffect.Flying()
                            .WithExpirationNever();
                        bigFly.BonusToAllSpeeds = qfThis => new Bonus(12, BonusType.Untyped, "Jurroz");
                        target.AddQEffect(bigFly);
                        
                        // Get a floodfill for movement using striding, after making the rune-bearer flying
                        List<Option> tileOptions = [
                            new CancelOption(true),
                            new PassViaButtonOption("Pass (consumes rune)"),
                        ];
                        CombatAction? moveAction = (target.Possibilities.CreateActions(true)
                            .FirstOrDefault(pw => pw.Action.ActionId == ActionId.Stride) as CombatAction)
                            ?.WithActionCost(0);
                        IList<Tile> floodFill = Pathfinding.Floodfill(target, target.Battle, new PathfindingDescription()
                            {
                                Squares = 12,
                                Style = { MaximumSquares = 12 }
                            })
                            .Where(tile =>
                                tile.LooksFreeTo(target) && damagedBy.Any(cr2 => cr2.DistanceTo(tile) < target.DistanceTo(cr2)))
                            .ToList();
                        floodFill.ForEach(tile =>
                        {
                            if (moveAction == null || !(bool)moveAction.Target.CanBeginToUse(target)) return;
                            tileOptions.Add(moveAction.CreateUseOptionOn(tile).WithIllustration(moveAction.Illustration));
                        });
                        
                        // Pick a tile to fly to
                        Option chosenTile = (await target.Battle.SendRequest(
                            new AdvancedRequest(target, "Choose where to Fly to or right-click to cancel. You should end your movement next to a creature who has damaged you.", tileOptions)
                            {
                                IsMainTurn = false,
                                IsStandardMovementRequest = true,
                                TopBarIcon = thisRune.Illustration,
                                TopBarText = "Choose where to Fly to or right-click to cancel. You should end your movement next to a creature who has damaged you.",
                            })).ChosenOption;
                        switch (chosenTile)
                        {
                            case CancelOption:
                                sourceAction.RevertRequested = true;
                                break;
                            case TileOption tOpt:
                                // Perform fly
                                Sfxs.Play(ModData.SfxNames.InvokedJurroz);
                                await tOpt.Action();
                                target.RemoveAllQEffects(qf => qf == bigFly);
                                
                                // Apply off-guard to a creature
                                List<Creature> validCreatures = target.Battle.AllCreatures
                                    .Where(cr =>
                                        target.EnemyOf(cr) && target.IsAdjacentTo(cr) && damagedBy.Contains(cr))
                                    .ToList();
                                Creature? chosenCreature = await target.Battle.AskToChooseACreature(
                                    target,
                                    validCreatures,
                                    thisRune.Illustration,
                                    "Choose a creature who has damaged you to make off-guard.",
                                    "This creature becomes off-guard until the end of your next turn.",
                                    " Don't make off-guard ");
                                if (chosenCreature != null)
                                {
                                    QEffect jurrozFooted = QEffect.FlatFooted("Jurroz, Rune of Dragon Fury")
                                        .WithExpirationAtEndOfSourcesNextTurn(target, true);
                                    jurrozFooted.Key = "RunesmithPlaytest.JurrozInvocation";
                                    chosenCreature.AddQEffect(jurrozFooted);
                                }
                                CommonRuneRules.ApplyImmunity(target, thisRune);
                                CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                                break;
                            case PassViaButtonOption:
                                CommonRuneRules.ApplyImmunity(target, thisRune);
                                CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                                break;
                        }
                    }
                }
            };
        RuneFeat jurrozFeat = AddRuneAsRuneFeat("RunesmithPlaytest.RuneJurroz", runeJurroz);
        jurrozFeat.WithPermanentQEffect(null, qfFeat =>
        {
            qfFeat.Id = ModData.QEffectIds.JurrozDamageTracker;
            Dictionary<Creature, List<Creature>> damageHistory = []; // Key: Creature who's taken damage, Value: Creatures who damaged them
            qfFeat.Tag = damageHistory;
            qfFeat.AddGrantingOfTechnical(
                cr => true,
                qfTech =>
                {
                    qfTech.AfterYouTakeDamage = async (qfThis, amount, kind, action, critical) =>
                    {
                        if (amount <= 0 || action == null)
                            return;
                        
                        if (damageHistory.TryGetValue(qfTech.Owner, out List<Creature>? damagedBy))
                        {
                            if (!damagedBy.Contains(action.Owner))
                                damagedBy.Add(action.Owner);
                        }
                        else
                            damageHistory[qfTech.Owner] = [action.Owner];
                    };
                });
        });
        
        Rune runeKojastri = new Rune(
            "Kojastri, Rune of Insulation",
            ModData.Traits.Kojastri,
            IllustrationName.FrostRunestone,
            9,
            "drawn on a creature", /*drawn on armor*/
            "This rune insulates from harmful energy of all kinds.",
            "The rune-bearer gains resistance 5 to your choice of cold, electricity, or fire, and any other creature that touches them or damages them with a melee unarmed attack or non-reach melee weapon takes 2d6 damage. The rune has the trait of the energy type chosen and deals damage of that type.",
            invocationText: "Any creature that has the rune-bearer grabbed or restrained takes 4d6 damage with a basic Reflex save. On a failure, it also releases the rune-bearer." /*releases the armor's wearer.*/ /*engulfed, grabbed, restrained, or swallowed whole*/,
            levelText: "The resistance increases by 5, and the damage increases by 1d6, or 2d6 for the invocation.",
            additionalTraits: [Trait.Arcane])
            {
                LevelFormat = "+4",
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    ArgumentNullException.ThrowIfNull(caster);

                    ChoiceButtonOption choice = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{thisRune.Name}{{/b}}\nChoose which damage type to insulate against.",
                        "Cold", "Electricity", "Fire"); // TODO: inline icons?

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
                    
                    DrawnRune kojastriPassive = new DrawnRune(
                        thisRune,
                        thisRune.Name + $" ({chosenKind.ToStringOrTechnical().ToLower()})",
                        $"You have resistance {GetResistanceAmount(caster.Level)} to {chosenKind.ToStringOrTechnical().ToLower()} damage, and creatures take {GetPassiveDamageAmount(caster.Level)} damage of the chosen type when they touch you.",
                        ExpirationCondition.Never,
                        caster,
                        thisRune.Illustration)
                        {
                            Tag = chosenKind,
                            CountsAsABuff = true,
                            StateCheck = qfThis =>
                                qfThis.Owner.WeaknessAndResistance.AddResistance(chosenKind, GetResistanceAmount(caster.Level)),
                            AfterYouAreTargeted = async (qfThis, action) =>
                            {
                                if (action == sourceAction || action.Owner == qfThis.Owner)
                                    return;

                                // Fail to do thorns if not the first of multiple duplicate effects
                                if (qfThis != qfThis.Owner.QEffects.First(qf =>
                                        qf is DrawnRune dr && dr.Rune.RuneId == thisRune.RuneId))
                                    return;
                                
                                if (action.Target is CreatureTarget { RangeKind: RangeKind.Melee } && !(action.HasTrait(Trait.Weapon) && action.HasTrait(Trait.Reach)))
                                {
                                    await CommonSpellEffects.DealDirectDamage(
                                        CombatAction.CreateSimple(qfThis.Owner, thisRune.Name, [..qfThis.Traits]),
                                        DiceFormula.FromText(GetPassiveDamageAmount(caster.Level)),
                                        action.Owner,
                                        CheckResult.Success,
                                        chosenKind);
                                }
                            },
                        };
                    kojastriPassive.Traits.Add(chosenTrait); // TODO: Go remove the [..Traits]s from the other runes. It's already part of the constructor (i forgor).
                    return kojastriPassive;

                    // Local functions
                    int GetResistanceAmount(int level) {
                        int currentLevel = Math.Min(level, 9);
                        int bonusLevel = (currentLevel - 9) / 4;
                        return 5 + (bonusLevel * 5);
                    }
                    string GetPassiveDamageAmount(int level) {
                        int currentLevel = Math.Min(level, 9);
                        int bonusLevel = (currentLevel - 9) / 4;
                        return (2 + (bonusLevel * 1)) + "d6";
                    }
                },
                InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
                {
                    Sfxs.Play(ModData.SfxNames.InvokedKojastri);
                    target.QEffects.ForEach(qf =>
                    {
                        if (qf.Id is not QEffectId.Grappled || invokedRune.Tag is not DamageKind damageType || CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                            return;

                        Creature grappler = qf.Source!;
                        CombatAction invocationDetails = CombatAction.CreateSimple(caster, sourceAction.Name, [..invokedRune.Traits])
                            .WithSavingThrow(new SavingThrow(Defense.Reflex, RunesmithClass.RunesmithDC(caster)));
                        CheckResult result = CommonSpellEffects.RollSavingThrow(
                            grappler,
                            invocationDetails,
                            Defense.Reflex,
                            RunesmithClass.RunesmithDC(caster));
                        CommonSpellEffects.DealBasicDamage(
                            invocationDetails,
                            caster,
                            grappler,
                            result,
                            GetInvocationDamageAmount(caster.Level),
                            damageType);
                        if (result < CheckResult.Success)
                            qf.ExpiresAt = ExpirationCondition.Immediately;
                        CommonRuneRules.ApplyImmunity(grappler, thisRune);
                    });
                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                    return;

                    string GetInvocationDamageAmount(int level)
                    {
                        int currentLevel = Math.Min(level, 9);
                        int bonusLevel = (currentLevel - 9) / 4;
                        return (4 + (bonusLevel * 2)) + "d6";
                    }
                },
            }
            .WithDamagingInvocationTechnical()
            .WithDamagingAreaInvocationTechnical()
            .WithReflexSaveInvocationTechnical()
            .WithTargetDoesNotSaveTechnical();
        AddRuneAsRuneFeat("RunesmithPlaytest.RuneKojastri", runeKojastri);
    }

    /// <summary>
    /// Creates a new <see cref="RuneFeat"/> from a given instance of a <see cref="Rune"/>, adds it to <see cref="RunesmithRunes.AllRunes"/>, adds it to <see cref="RunesmithRunes.AllRuneFeats"/>, then adds it to the ModManager.
    /// </summary>
    /// <param name="technicalName">The technical name to be passed into <see cref="ModManager.RegisterFeatName"/>.</param>
    /// <param name="rune">The <see cref="Rune"/> to create a RuneFeat for.</param>
    /// <returns>(<see cref="RuneFeat"/>) The feat associated with the Rune.</returns>
    public static RuneFeat AddRuneAsRuneFeat(
        string technicalName,
        Rune rune)
    {
        RuneFeat runeFeat = RuneFeat.CreateRuneFeatFromRune(technicalName, rune);
        if (!AllRunes.Contains(rune))
            AllRunes.Add(rune);
        AllRuneFeats.Add(runeFeat);
        ModManager.AddFeat(runeFeat);
        return runeFeat;
    }
}