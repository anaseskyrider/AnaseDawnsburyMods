using System.Text.RegularExpressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
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
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class RunesmithRunes
{
    public static List<Rune> AllRunes { get; } = [];
    public static List<RuneFeat> AllRuneFeats { get; } = [];

    public static void LoadRunes()
    {
        // TODO: Alter Rune class to store FullName, BaseName, Title. Delete Name, so as to generate errors to then fix.
        // Use this to enhance some text-based processes.
        // Use this to enhance the create-then-load workflow I now use.
        
        /* TODO: Consider altering the way runes apply Item effects based on these Item fields to look into:
         * WithPermanentQEffectWhenWorn
         * WithOnCreatureWhenWorn
         * StateCheckWhenWielded
         */
        
        // TODO: Consolidate item-targeting routines with task functions in CommonRuneRules
        
        // BUG: Some runes incorrectly scale with non-striking die increases.
        // "Counting Damage Dice: Effects base on a weapon's number of damage dice include only the weapon's damage die plus any extra dice from a striking rune. They don't count extra dice from abilities, critical specialization effects, property runes, weapon traits, or the like."

        #region Level 1 Runes

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
            .WithHeightenedText(
                (thisRune, level) =>
                {
                    const int baseValue = 6;
                    int bonusValue = (level - thisRune.BaseLevel) / 2; // Increase by 1 every 2 character levels
                    int totalValue = 6 + bonusValue;
                    string heightenedVar = S.HeightenedVariable(totalValue, baseValue);
                    return $"The bearer's fire resistance, if any, is reduced by {heightenedVar}. Its immunities are unaffected.";
                },
                (thisRune, level) =>
                {
                    const int baseValue = 2;
                    int roundHalfLevel = ((level - thisRune.BaseLevel) / 2);
                    int damageAmount = 2 + roundHalfLevel * 2;
                    string heightenedVar = S.HeightenedVariable(damageAmount, baseValue);
                    return $"The bearer takes {heightenedVar}d6 fire damage, with a basic Fortitude save; on a critical failure, they are dazzled for 1 round.";
                },
                "+2")
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnEnemies())
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                int resistReductionAmount = 6 + (caster.Level - thisRune.BaseLevel) / 2;
                DrawnRune atrylPassive = new DrawnRune(
                    thisRune,
                    "Fire resistance reduced by " + resistReductionAmount + ".",
                    caster)
                {
                    Value = resistReductionAmount, // Value might be an unnecessary field, aesthetically.
                    StateCheck = qfSelf =>
                    {
                        if ((qfSelf as DrawnRune)!.Disabled)
                            return;

                        Resistance? fireResist =
                            qfSelf.Owner.WeaknessAndResistance.Resistances.FirstOrDefault(res =>
                                res.DamageKind == DamageKind.Fire);
                        if (fireResist is { Value: > 0 })
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
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                {
                    int roundHalfLevel = ((caster.Level - 1) / 2);
                    int damageAmount = 2 + roundHalfLevel * 2;
                    CheckResult result = CommonSpellEffects.RollSavingThrow(
                        target,
                        sourceAction,
                        Defense.Fortitude,
                        caster.ClassDC(ModData.Traits.Runesmith));
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
            })
            .WithDetrimentalPassiveTechnical()
            .WithDamagingInvocationTechnical()
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneAtryl", runeAtryl);

        // BUG: Add ignores concealment to the invocation... or should I?
        // BUG: Invoking the rune doesn't remove the rune if it misses due to concealment.
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
            .WithHeightenedText(
                null,
                (thisRune, level) =>
                {
                    const int baseValue = 2;
                    int roundHalfLevel = (level - thisRune.BaseLevel) / 2;
                    int damageAmount = 2 + roundHalfLevel * 2;
                    string heightenedVar = S.HeightenedVariable(damageAmount, baseValue);
                    return $"The essence of sharpness is released outwards from the rune, dealing {heightenedVar}d6 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.";
                },
                "+2")
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnAllies())
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune? MakeEsvadirPassive(Item targetItem)
                {
                    DrawnRune esvadirPassive = new DrawnRune(
                            thisRune,
                            "The target item or piercing and slashing unarmed strikes deal 2 persistent bleed damage per weapon damage die.",
                            caster)
                        {
                            Name = $"{thisRune.Name} ({targetItem.Name})", // Custom name
                            AfterYouTakeAction = async (qfThis, action) => // Add bleed
                            {
                                if ((qfThis as DrawnRune)!.Disabled)
                                    return;

                                Item? qfItem = (qfThis as DrawnRune)?.DrawnOn as Item;
                                Item? actionItem = action.Item;

                                // This many complex conditionals is really hard to work out so I did it the long way.
                                // Fail to bleed if,
                                if (actionItem == null || qfItem == null || // either item is blank
                                    !action.HasTrait(Trait.Strike) || // or the action isn't a strike
                                    action.ChosenTargets.ChosenCreature == null || // or null targets
                                    action.ChosenTargets.ChosenCreature ==
                                    qfThis.Owner || // or I'm my target for any reason
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
                                    QEffect pBleed = QEffect.PersistentDamage(bleedAmount, DamageKind.Bleed);
                                    pBleed.SourceAction = sourceAction; // Store the action that drew this rune so that data about this drawn rune is available to the persistent damage
                                    action.ChosenTargets.ChosenCreature.AddQEffect(pBleed);
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

                if (sourceAction.Target is AreaTarget)
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
                
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    $"{{b}}{sourceAction.Name}{{/b}}\nWhich item, or unarmed strikes, would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                    validItemsString.ToArray()
                );

                Item targetItem = validItems[chosenOption.Index];

                return MakeEsvadirPassive(targetItem);
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
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
                    .WithSavingThrow(new SavingThrow(Defense.Fortitude, caster.ClassDC(ModData.Traits.Runesmith)))
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
            })
            .WithDamagingInvocationTechnical()
            .WithFortitudeSaveInvocationTechnical()
            .WithTargetDoesNotSaveTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneEsvadir", runeEsvadir);

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
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnAllyItems(item => item.HasTrait(Trait.Shield), "no shield equipped"))
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune? MakeHoltrikPassive(Item targetItem)
                {
                    DrawnRune drawnHoltrik = new DrawnRune(
                        thisRune,
                        "The circumstance bonus from Raising a Shield is increased by 1.",
                        caster)
                    {
                        Name = $"{thisRune.Name} ({targetItem.Name})", // Custom name
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
                        if (sourceAction.Target is AreaTarget)
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
                            $"{{b}}{sourceAction.Name}{{/b}}\nWhich shield would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                            target.HeldItems[0].Name,
                            target.HeldItems[1].Name)
                            ? target.HeldItems[0]
                            : target.HeldItems[1];

                        return MakeHoltrikPassive(targetItem);
                }
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
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
            })
            .WithDrawnOnShieldTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneHoltrik", runeHoltrik);

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
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnAllies()) // Since everyone has unarmed strikes, it only requires the target is an ally.
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune? MakeMarssylPassive(Item targetItem)
                {
                    DrawnRune marssylPassive = new DrawnRune(
                        thisRune,
                        $"The target item or bludgeoning unarmed strikes deal 1 bludgeoning splash damage per weapon damage die." +
                        (targetItem.HasTrait(Trait.Melee)
                            ? "\n\nMelee weapon: The rune-bearer is immune to this splash damage."
                            : null),
                        caster)
                    {
                        Name = $"{thisRune.Name} ({targetItem.Name})", // Custom name
                        AfterYouTakeAction = async (qfSelf, action) => // Add splash
                        {
                            #region vars and validation checks
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
                            #endregion

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
                                    foreach (Creature target2 in qfSelf.Owner.Battle.AllCreatures.Where(cr =>
                                                 action.ChosenTargets.ChosenCreature
                                                     .IsAdjacentTo(cr))) // Loop through all adjacent creatures,
                                    {
                                        if (target2 != qfSelf.Owner ||
                                            !actionItem.HasTrait(Trait
                                                .Melee)) // And if it's a melee attack, skip me, otherwise include me when I,
                                            await CommonSpellEffects.DealDirectSplashDamage(action, splashAmount,
                                                target2, DamageKind.Bludgeoning); // splash them too.
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

                if (sourceAction.Target is AreaTarget)
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

                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    $"{{b}}{sourceAction.Name}{{/b}}\nWhich item, or unarmed strikes, would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                    validItemsString.ToArray()
                );

                Item targetItem = validItems[chosenOption.Index];

                return MakeMarssylPassive(targetItem);
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
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
                                action.Owner.ClassDC(ModData.Traits.Runesmith));
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
            });
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneMarssyl", runeMarssyl);

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
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnAllyItems(item => item.HasTrait(Trait.Shield), "no shield equipped"))
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune? MakeOljinexPassive(Item targetItem)
                {
                    DrawnRune drawnOljinex = new DrawnRune(
                        thisRune,
                        "While the shield is raised, you have a +1 status bonus to AC against physical ranged attacks.",
                        caster)
                    {
                        Name = $"{thisRune.Name} ({targetItem.Name})", // Custom name
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
                        if (sourceAction.Target is AreaTarget)
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
                            $"{{b}}{sourceAction.Name}{{/b}}\nWhich shield would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                            target.HeldItems[0].Name, target.HeldItems[1].Name)
                            ? target.HeldItems[0]
                            : target.HeldItems[1];

                        return MakeOljinexPassive(targetItem);
                }
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
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
                                    (_,_) => int.MinValue)) // TODO: encourage the action?
                            .WithActiveRollSpecification(new ActiveRollSpecification(
                                Checks.Perception(),
                                (action, attacker, defender) =>
                                    new CalculatedNumber(defender!.ClassDC(ModData.Traits.Runesmith), "Class DC", [])))
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
            })
            .WithDrawnOnShieldTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneOljinex", runeOljinex);

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
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                const float emanationSize = 4f; // 20 feet

                DrawnRune pluunaPassive = new DrawnRune(
                    thisRune,
                    "Can't become undetected, and all creatures in a 20-foot emanation takes a -1 item penalty to Stealth checks.",
                    caster)
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
                                cr.AddQEffect(new QEffect("Pluuna's Light",
                                    "You have a -1 item penalty to Stealth checks.", ExpirationCondition.Ephemeral,
                                    qfThis.Owner, IllustrationName.Light)
                                {
                                    Key = "Pluuna's Light",
                                    BonusToSkills = skill => skill == Skill.Stealth
                                        ? new Bonus(-1, BonusType.Item, thisRune.Name)
                                        : null
                                }));
                    },
                };

                return pluunaPassive;
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                const float emanationSize = 4f; // 20 feet
                
                // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                CombatAction invokePluunaOnEveryone = new CombatAction(
                        target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                        thisRune.Illustration,
                        $"Invoke {thisRune.Name}",
                        [..thisRune.Traits, Trait.DoNotShowInCombatLog],
                        thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                        Target.Emanation((int)emanationSize))
                    .WithActionCost(0)
                    .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                    .WithSoundEffect(ModData.SfxNames.InvokedPluuna)
                    .WithSavingThrow(new SavingThrow(Defense.Fortitude, caster.ClassDC(ModData.Traits.Runesmith)))
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
            })
            //.WithTargetDoesNotSaveTechnical() // Debatable. The target does save, but so does everyone else.
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RunePluuna", runePluuna);

        // BUG: Much like how Jurroz retroactively tracks damage taken, this should retroactively track moves made.
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
            .WithHeightenedText(
                (thisRune, charLevel) =>
                {
                    int bonusDamage = (charLevel - thisRune.BaseLevel) / 2;
                    string damage = "1d4" + (bonusDamage > 0 ? $"+{S.HeightenedVariable(bonusDamage, 0)}" : null);
                    return $"If the bearer does not take a move action at least once on its turn, lightning finds it at the end of its turn, dealing {damage} electricity damage.";
                },
                (thisRune, charLevel) =>
                {
                    int numDice = 2 + (int)Math.Floor((charLevel - thisRune.BaseLevel) / 2d) * 2;
                    string heightenedVar = S.HeightenedVariable(numDice, 2);
                    return $"The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes {heightenedVar}d6 electricity damage, with a basic Fortitude save.";
                },
                "+2")
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnEnemies())
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                int bonusDamage = (caster.Level - thisRune.BaseLevel) / 2;
                DiceFormula immobilityDamage = DiceFormula.FromText($"1d4+{bonusDamage}");
                DrawnRune ranshuPassive = new DrawnRune(
                    thisRune,
                    $"If you don't take a move action at least once during your turn, you take {immobilityDamage} electricity damage.",
                    caster)
                {
                    AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.HasTrait(Trait.Move))
                            qfThis.UsedThisTurn = true; // Has moved this turn
                    },
                    EndOfYourTurnDetrimentalEffect = async (qfThis, self) =>
                    {
                        if ((qfThis as DrawnRune)!.Disabled)
                            return;
                        if (qfThis.UsedThisTurn) // If you have moved this turn,
                            return; // don't take any damage.
                        await CommonSpellEffects.DealDirectDamage(
                            CombatAction.CreateSimple(caster, "Ranshu, Rune of Thunder", [..thisRune.Traits])
                                .WithTag(qfThis),
                            immobilityDamage,
                            self,
                            CheckResult.Failure,
                            DamageKind.Electricity);
                        Sfxs.Play(ModData.SfxNames.PassiveRanshu);
                    },
                };
                return ranshuPassive;
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                {
                    int numDice = 2 + (int)Math.Floor((caster.Level - thisRune.BaseLevel) / 2d) * 2;
                    DiceFormula invocationDamage = DiceFormula.FromText($"{numDice}d6");
                    CheckResult result = CommonSpellEffects.RollSavingThrow(
                        target,
                        sourceAction,
                        Defense.Fortitude,
                        caster.ClassDC(ModData.Traits.Runesmith));
                    await CommonSpellEffects.DealBasicDamage(sourceAction, caster, target, result,
                        invocationDamage, DamageKind.Electricity);
                    Sfxs.Play(ModData.SfxNames.InvokedRanshu);
                }

                CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                CommonRuneRules.ApplyImmunity(target, thisRune);
            })
            .WithDetrimentalPassiveTechnical()
            .WithDamagingInvocationTechnical()
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneRanshu", runeRanshu);

        Rune runeSunDiacritic = new Rune(
                "Sun-, Diacritic Rune of Preservation",
                ModData.Traits.SunDiacritic,
                IllustrationName.DisruptingRunestone,
                1,
                "drawn on a rune",
                "This spiraling diacritic channels the magic of a rune outwards, then back to the same location, allowing a rune to reconstitute itself.",
                "After the base rune is invoked, the rune automatically Traces itself back upon the same target.\n\n{b}Special{/b} You can have only one copy of {i}sun-, diacritic rune of preservation{/i} applied at a given time, and once you invoke it, you cannot Etch or Trace it again this combat.",
                additionalTraits: [ModData.Traits.Diacritic])
            .WithUsageCondition(Rune.UsabilityConditions.CombinedUsability(
                (atk, _) => atk.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.SunDiacritic)
                    ? Usability.NotUsable("already invoked this combat")
                    : Usability.Usable,
                Rune.UsabilityConditions.UsableOnDiacritics(dr => !dr.Traits.Contains(ModData.Traits.Diacritic))))
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
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
                            "The base rune is automatically Traced again after being invoked.",
                            caster)
                        {
                            Name = $"{thisRune.Name} ({targetRune.Name})", // Custom name
                            BeforeInvokingRune = async (thisDr, sourceAction2, drInvoked) =>
                            {
                                if (thisDr.Disabled)
                                    // TODO: check if this needs an extra check
                                    // || drInvoked != thisDr.DrawnOn
                                    return;
                                /*CombatAction sunRedraw = CombatAction.CreateSimple(
                                drInvoked.Source!,
                                "Sun, Diacritic Rune of Preservation",
                                [ModData.Traits.Traced]); // <- Even if it WAS etched before, it's now traced.*/
                                CombatAction? sunRedraw = CommonRuneRules.CreateTraceAction(drInvoked.Source!, drInvoked.Rune, 0, 99);
                                if (sunRedraw == null)
                                {
                                    thisDr.Owner.Battle.Log("Sun- failed to trace rune due to unknown reason. Usage of Sun was not consumed.");
                                    return;
                                }
                                if (await CommonRuneRules.DrawRuneOnTarget(sunRedraw, thisDr.Source!,
                                        drInvoked.Owner, drInvoked.Rune, false) != null)
                                    Sfxs.Play(ModData.SfxNames.InvokedSun);
                                thisDr.Source!.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.SunDiacritic);
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

                if (sourceAction.Target is AreaTarget)
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
                
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    $"{{b}}{sourceAction.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                    validRunesString.ToArray()
                );

                DrawnRune targetRune = validRunes[chosenOption.Index];

                return CreateSunPassive(targetRune);
            })
            .WithDrawnOnRuneTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneSunDiacritic", runeSunDiacritic);

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
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnDiacritics(dr => dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile)))
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune? CreateUrPassive(DrawnRune targetRune)
                {
                    DrawnRune drawnUr = new DrawnRune(
                        thisRune,
                        $"The base rune's invocation damage gains a +{caster.Abilities.Intelligence} status bonus.",
                        caster)
                    {
                        Name = $"{thisRune.Name} ({targetRune.Name})", // Custom name
                        BeforeInvokingRune = async (thisDr, sourceAction2, drInvoked) =>
                        {
                            if (thisDr.Disabled)
                                // TODO: check if this needs an extra check
                                // || drInvoked != thisDr.DrawnOn
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

                if (sourceAction.Target is AreaTarget)
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
                
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    $"{{b}}{sourceAction.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                    validRunesString.ToArray()
                );

                DrawnRune targetRune = validRunes[chosenOption.Index];

                return CreateUrPassive(targetRune);
            })
            .WithDrawnOnRuneTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneUrDiacritic", runeUrDiacritic);

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
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune drawnZohk = new DrawnRune(
                    thisRune,
                    $"You can Stride with a +15-foot status bonus if your destination space is closer to {caster.Name}.",
                    caster)
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
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                if (!CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                {
                    CheckResult result = CheckResult.Failure;
                    if (!target.FriendOf(caster))
                    {
                        result = CommonSpellEffects.RollSavingThrow(
                            target,
                            sourceAction,
                            Defense.Will,
                            caster.ClassDC(ModData.Traits.Runesmith));
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
                        Option selectedOption = (await caster.Battle.SendRequest(
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
            })
            .WithWillSaveInvocationTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneZohk", runeZohk);

        #endregion

        #region Level 9 Runes

        Rune runeEnDiacritic = new Rune(
                "En-, Diacritic Rune of Expansion",
                ModData.Traits.EnDiacritic,
                IllustrationName.UnderwaterRunestone,
                9,
                "drawn on a rune that deals damage (and not already in an area)",
                "This diacritic surrounds a rune with outward-facing arrows to magnify and direct power outward.",
                "When the base rune is invoked, its damage applies in a 15-foot burst, centered on the rune-bearer. If any creatures are also within the area, they are subject to the base rune's effects (including any saving throw).",
                additionalTraits:[ModData.Traits.Diacritic])
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnDiacritics(dr =>
                dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile) && !dr.Rune.InvokeTechnicalTraits.Contains(Trait.Splash))) // Target rune must deal damage and not already deal area damage.
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
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

                if (sourceAction.Target is AreaTarget)
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
                
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    $"{{b}}{sourceAction.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                    validRunesString.ToArray());

                DrawnRune targetRune = validRunes[chosenOption.Index];

                return CreateEnPassive(targetRune);

                // Create rune
                DrawnRune? CreateEnPassive(DrawnRune drawnOnto)
                {
                    DrawnRune drawnUr = new DrawnRune(
                        thisRune,
                        "When the base rune is invoked, its damage applies in a 15-foot burst, centered on the rune-bearer. If any creatures are also within the area, they are subject to the base rune's effects (including any saving throw).",
                        caster)
                    {
                        Name = $"{thisRune.Name} ({drawnOnto.Name})", // Custom name
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
                                foreach (Creature cr in drInvoked.Owner.Battle.AllCreatures
                                             .Where(cr => cr != drInvoked.Owner && cr.DistanceTo(drInvoked.Owner) <= 3))
                                {
                                    await invocation.Invoke(sourceAction2, drInvoked.Rune, sourceAction2.Owner, cr, drInvoked);
                                }
                        },
                    }.WithDiacriticRegulator(drawnOnto);

                    return drawnUr;
                }
            })
            .WithDrawnOnRuneTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneEnDiacritic", runeEnDiacritic);
        
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
                levelText: "The item bonus increases to +3.",
                additionalTraits: [Trait.Arcane])
            .WithHeightenedText(
                (thisRune, charLevel) =>
                {
                    int bonus = charLevel >= 17 ? 3 : 2;
                    return $"The rune-bearer gains a +{bonus} item bonus to Athletics checks.";
                },
                null,
                "17th")
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune feikrisPassive = new DrawnRune(
                    thisRune,
                    $"You have a +{(caster.Level >= 17 ? 3 : 2)} item bonus to Athletics checks.",
                    caster)
                {
                    BonusToSkills = skill =>
                        skill is Skill.Athletics ? new Bonus(caster.Level >= 17 ? 3 : 2, BonusType.Item, "Feikris") : null,
                };
                return feikrisPassive;
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                CombatAction invokeFeikrisOnEveryone = new CombatAction(
                        target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                        thisRune.Illustration,
                        $"Invoke {thisRune.Name}",
                        [..thisRune.Traits, Trait.DoNotShowInCombatLog],
                        thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                        Target.Emanation(3)
                            .WithIncludeOnlyIf((tar, cr) => cr != target))
                    .WithActionCost(0)
                    .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                    .WithSoundEffect(ModData.SfxNames.InvokedFeikris)
                    .WithSavingThrow(new SavingThrow(Defense.Fortitude, caster.ClassDC(ModData.Traits.Runesmith)))
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
            })
            .WithFortitudeSaveInvocationTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneFeikris", runeFeikris);

        Rune runeIchelsu = new Rune(
                "Ichelsu, Rune of Observation",
                ModData.Traits.Ichelsu,
                IllustrationName.GhostTouchRunestone,
                9,
                "drawn on a creature",
                "A ring of dotted circles, this rune allows a creature marked with it to see all.",
                $"The target is affected by {AllSpells.CreateSpellLink(SpellId.SeeInvisibility, ModData.Traits.Runesmith).Replace("see invisibility", "see the unseen")} and gains {ModData.Tooltips.MiscAllAroundVision("all-around vision")}.",
                invocationText: "The eyes of the rune fly outwards, attaching to all creatures in a 20-foot emanation. Each of these creatures that was invisible becomes concealed instead, and each one that was concealed for any other reason is no longer concealed. This effect lasts for 2 rounds.",
                additionalTraits:[Trait.Occult])
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune ichelsuPassive = new DrawnRune(
                    thisRune,
                    "You gain the effects of see the unseen and all-around vision.\n\n{b}See the Unseen{/b} You see invisible creatures as though they were just concealed, not invisible.\n\n{b}All-Around Vision{/b} You can't be flanked.",
                    caster)
                {
                    StateCheck = async qfThis =>
                    {
                        QEffect see = QEffect.SeeInvisibility()
                            .WithExpirationEphemeral();
                        see.HideFromPortrait = true;
                        QEffect vision = QEffect.AllAroundVision()
                            .WithExpirationEphemeral();
                        vision.HideFromPortrait = true;
                        vision.Innate = false;
                        qfThis.Owner
                            .AddQEffect(see)
                            .AddQEffect(vision);
                    }
                };
                return ichelsuPassive;
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                CombatAction invokeIchelsuOnEveryone = new CombatAction(
                        target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                        thisRune.Illustration,
                        $"Invoke {thisRune.Name}",
                        [..thisRune.Traits, Trait.DoNotShowInCombatLog],
                        thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                        Target.Emanation(4))
                    .WithActionCost(0)
                    .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                    .WithSoundEffect(ModData.SfxNames.InvokedIchelsu)
                    .WithNoSaveFor((thisAction, cr) =>
                        cr == target || CommonRuneRules.IsImmuneToThisInvocation(cr, thisRune))
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
                            invokedIchelsu.Id = QEffectId.FaerieFire;
                            invokedOnto.AddQEffect(invokedIchelsu);
                            CommonRuneRules.ApplyImmunity(invokedOnto, thisRune);
                        }
                    });

                if (await caster.Battle.GameLoop.FullCast(invokeIchelsuOnEveryone))
                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                else
                    sourceAction.RevertRequested = true;
            });
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneIchelsu", runeIchelsu);
        
        // "the target takes 1d4 persistent fire damage" has some ambiguity between Esvadir's invocation and Pluuna's invocation.
        // Wording changed to specify that the rune-bearer takes the persistent damage.
        // TODO: Unholy damage benefits to persistent damage
        Rune runeInthDiacritic = new Rune(
                "Inth-, Diacritic Rune of Corruption",
                ModData.Traits.InthDiacritic,
                IllustrationName.KeenRunestone,
                9,
                "drawn on a rune",
                "This set of angular accents around the base rune channels the essence of fiendish corruption.",
                "The base rune gains the unholy trait, as does any damage it deals. If applied to a holy creature, that creature is enfeebled 1 for as long as the rune is applied to it.",
                "(unholy) When the base rune is invoked, it burns away in unholy black fire that lingers on its bearer. In addition to the base rune's normal effect, the rune-bearer takes 1d4 persistent fire damage.",
                "The damage increases by 1d4.",
                [ModData.Traits.Diacritic, Trait.Divine, Trait.Fiend, Trait.Fire, Trait.Evil])
            .WithHeightenedText(
                null,
                (thisRune, charLevel) =>
                {
                    int currentLevel = Math.Max(charLevel, 9);
                    int bonusLevel = (currentLevel - 9) / 2;
                    int numDice = 1 + bonusLevel;
                    string heightenedVar = S.HeightenedVariable(numDice, 1);
                    return $"(unholy) When the base rune is invoked, it burns away in unholy black fire that lingers on its target. In addition to the base rune's normal effect, the target takes {heightenedVar}d4 persistent fire damage.";
                },
                "+2")
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnDiacritics())
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
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
                        return new DrawnRune(thisRune);
                    default:
                        await caster.FictitiousSingleTileMove(caster.Occupies); // Move back into place
                        DrawnRune? chosenRune = await CommonRuneRules.ChooseADrawnRune(
                            caster,
                            [target],
                            thisRune.Illustration,
                            $"Pick a rune to draw {{Blue}}{thisRune.Name}{{/Blue}} onto.",
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
                        thisRune,
                        "The base rune gains the unholy trait, as does any damage it deals.",
                        caster)
                    {
                        Name = $"{thisRune.Name} ({targetRune.Name})", // Custom name
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
                                            (action, _) =>
                                                action?.Tag == drawnInth.DrawnOn,
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
        RuneFeat inthFeat = AddRuneAsRuneFeat(ModData.IdPrepend+"RuneInthDiacritic", runeInthDiacritic);
        inthFeat.RulesText += "\n\n" + ModData.Illustrations.DawnsburySun.IllustrationAsIconString + " {b}Compatibility{/b} For the purposes of being holy, creatures with weakness to evil damage are considered holy, and this diacritic's persistent damage uses the better of fire or evil damage. {i}(NYI: Esvadir's bleed damage is not yet unholy.){/i}";

        Rune runeJurroz = new Rune(
                "Jurroz, Rune of Dragon Fury",
                ModData.Traits.Jurroz,
                IllustrationName.CorrosiveRunestone,
                9,
                $"etched onto a creature ({new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString} cannot be traced)", //or armor",
                "This angular rune channels the fury of dragon kind.",
                "Whenever a creature Strikes the rune-bearer, draconic sanction fully focuses on them, causing the striking creature to become off-guard for 1 round.",
                invocationText: "As a {icon:FreeAction} free action, the rune-bearer can Fly up to 60 feet toward a creature that has damaged them in the last minute. If they end this movement adjacent to the creature, the creature becomes off-guard until the end of the rune-bearer's next turn.",
                additionalTraits: [Trait.Dragon])
            .WithUsageCondition(Rune.UsabilityConditions.UsableOnAllies())
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune jurrozPassive = new DrawnRune(
                    thisRune,
                    "Whenever a creature Strikes you, they become off-guard for 1 round.",
                    caster)
                {
                    AfterYouTakeDamage = async (qfThis, amount, kind, action, critical) =>
                    {
                        if (action == null || !action.HasTrait(Trait.Strike))
                            return;
                            
                        QEffect jurrozFooted = QEffect.FlatFooted("Jurroz, Rune of Dragon Fury")
                            .WithExpirationAtStartOfSourcesTurn(action.Owner, 1);
                        jurrozFooted.Key = ModData.IdPrepend+"JurrozPassive";
                        action.Owner.AddQEffect(jurrozFooted);
                    },
                };
                return jurrozPassive;
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
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
                    CombatAction? moveAction = (target.Possibilities
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Stride)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            })
                            .CreateActions(true)
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
                                jurrozFooted.Key = ModData.IdPrepend+"JurrozInvocation";
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
            })
            .WithOnlyEtchedTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneJurroz", runeJurroz)
            .WithPermanentQEffect(
                null,
                qfFeat =>
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
            .WithHeightenedText(
                (thisRune, charLevel) =>
                {
                    int currentLevel = Math.Max(charLevel, 9);
                    int bonusLevel = (currentLevel - 9) / 4;
                    int resistAmount = 5 + (bonusLevel * 5);
                    int damageAmount = 2 + (bonusLevel * 1);
                    return $"The rune-bearer gains resistance {S.HeightenedVariable(resistAmount, 5)} to your choice of cold, electricity, or fire, and any other creature that touches them or damages them with a melee unarmed attack or non-reach melee weapon takes {S.HeightenedVariable(damageAmount, 2)}d6 damage. The rune has the trait of the energy type chosen and deals damage of that type.";
                },
                (thisRune, charLevel) =>
                {
                    int currentLevel = Math.Max(charLevel, 9);
                    int bonusLevel = (currentLevel - 9) / 4;
                    int damageAmount = 4 + (bonusLevel * 2);
                    return $"Any creature that has the rune-bearer grabbed or restrained takes {S.HeightenedVariable(damageAmount, 4)}d6 damage with a basic Reflex save. On a failure, it also releases the rune-bearer.";
                },
                "+4")
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
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
                    $"You have resistance {GetResistanceAmount(caster.Level)} to {chosenKind.ToStringOrTechnical().ToLower()} damage, and creatures take {GetPassiveDamageAmount(caster.Level)} damage of the chosen type when they touch you.",
                    caster)
                {
                    Name = $"{thisRune.Name} ({chosenKind.ToStringOrTechnical().ToLower()})",
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
                                CombatAction.CreateSimple(qfThis.Owner, thisRune.Name, [..qfThis.Traits]).WithTag(qfThis),
                                DiceFormula.FromText(GetPassiveDamageAmount(caster.Level)),
                                action.Owner,
                                CheckResult.Success,
                                chosenKind);
                        }
                    },
                };
                kojastriPassive.Traits.Add(chosenTrait);
                return kojastriPassive;

                // Local functions
                int GetResistanceAmount(int level) {
                    int currentLevel = Math.Max(level, 9);
                    int bonusLevel = (currentLevel - 9) / 4;
                    return 5 + (bonusLevel * 5);
                }
                string GetPassiveDamageAmount(int level) {
                    int currentLevel = Math.Max(level, 9);
                    int bonusLevel = (currentLevel - 9) / 4;
                    return (2 + (bonusLevel * 1)) + "d6";
                }
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                Sfxs.Play(ModData.SfxNames.InvokedKojastri);
                target.QEffects.ForEach(qf =>
                {
                    if (qf.Id is not QEffectId.Grappled || invokedRune.Tag is not DamageKind damageType || CommonRuneRules.IsImmuneToThisInvocation(target, thisRune))
                        return;

                    Creature grappler = qf.Source!;
                    CombatAction invocationDetails = CombatAction.CreateSimple(caster, sourceAction.Name, [..invokedRune.Traits])
                        .WithTag(invokedRune)
                        .WithSavingThrow(new SavingThrow(Defense.Reflex, caster.ClassDC(ModData.Traits.Runesmith)));
                    CheckResult result = CommonSpellEffects.RollSavingThrow(
                        grappler,
                        invocationDetails,
                        Defense.Reflex,
                        caster.ClassDC(ModData.Traits.Runesmith));
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
                
                // Local function
                string GetInvocationDamageAmount(int level)
                {
                    int currentLevel = Math.Max(level, 9);
                    int bonusLevel = (currentLevel - 9) / 4;
                    return (4 + (bonusLevel * 2)) + "d6";
                }
            })
            .WithDamagingInvocationTechnical()
            .WithDamagingAreaInvocationTechnical()
            .WithReflexSaveInvocationTechnical()
            .WithTargetDoesNotSaveTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneKojastri", runeKojastri);

        // Faction alignment is treated as "enemies to the runesmith", regardless of the rune-bearer's faction.
        // Changed to a 10-foot size, but always works.
        // BUG: The difficult terrain effect doesn't interact with immunity to emotion or mental effects. No known way to fix this at this time.
        Rune runeTrolistri = new Rune(
                "Trolistri, Rune of Forlorn Sorrow",
                ModData.Traits.Trolistri,
                IllustrationName.NightmareRunestone,
                9,
                "etched on a creature", // etched onto a creature or armor
                "This rune calls to mind the forlorn nature of elves, and the beauty within it. While this rune is beautiful, sorrow is best admired from a distance, discouraging approach.",
                "Enemies treat all squares within 10 feet of the rune-bearer as difficult terrain.",
                "Sorrow blots out the capacity for any other action. Each enemy within 20 feet of the rune-bearer must succeed at a Will saving throw or be slowed 1 as it spends the first action of its next turn sobbing (or slowed 2 on a critical failure). Regardless of the outcome, the creature is then temporarily immune to this invocation for the rest of the encounter.",
                null,
                [Trait.Arcane, Trait.Elf, Trait.Emotion, Trait.Mental])
            .WithDrawnRuneCreator(async (sourceAction, caster, target, thisRune) =>
            {
                const int radius = 2; // Changed to 10 feet instead of 20 feet.
                DrawnRune trolistriPassive = new DrawnRune(
                    thisRune,
                    "Squares within 10 feet of you are difficult terrain to {Blue}"+caster+"'s{/Blue} enemies.",
                    caster);
                Zone trolistriZone = Zone.Spawn(trolistriPassive, ZoneAttachment.Aura(radius))
                    .With(zone =>
                    {
                        zone.TileEffectCreator = tile =>
                            new TileQEffect(tile)
                            {
                                Illustration = IllustrationName.IllusoryRubble,
                                // Terrain is always difficult, instead of directional and individual
                                StateCheck = tqfThis =>
                                    tqfThis.Owner.DifficultTerrainToComputerControlledCreatures = true
                            };
                    });

                return trolistriPassive;
            })
            .WithInvocationBehavior(async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                // Create action wrapper for targeting and roll-inspection of invoking from target to emanation creatures.
                CombatAction invokeTrolistriOnEveryone = new CombatAction(
                        target, // Get creatures near the rune, who is the creature with the drawn rune being invoked
                        thisRune.Illustration,
                        $"Invoke {thisRune.Name}",
                        [..thisRune.Traits, Trait.DoNotShowInCombatLog],
                        thisRune.InvocationTextWithHeightening(thisRune, caster.Level) ?? thisRune.InvocationText!,
                        Target.Emanation(4)
                            .WithIncludeOnlyIf((tar, cr) => cr.EnemyOf(caster))
                            // Not sure if the rune-bearer counts as an enemy within 20 feet of the bearer.
                            // Because sometimes, obvious English linguistics isn't actually that obvious.
                            /*.WithIncludeOnlyIf((tar, cr) => cr != target)*/)
                    .WithActionCost(0)
                    .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                    .WithSoundEffect(ModData.SfxNames.InvokedTrolistri)
                    .WithSavingThrow(new SavingThrow(Defense.Will, caster.ClassDC(ModData.Traits.Runesmith)))
                    .WithNoSaveFor((thisAction, cr) => /*cr == target ||*/ CommonRuneRules.IsImmuneToThisInvocation(cr, thisRune) || cr.FriendOf(caster))
                    .WithEffectOnEachTarget(async (selfAction, invokeEE, invokedOnto, result) =>
                    {
                        if (!CommonRuneRules.IsImmuneToThisInvocation(invokedOnto, thisRune))
                        {
                            int value = result switch
                            {
                                CheckResult.CriticalFailure => 2,
                                CheckResult.Failure => 1,
                                _ => 0
                            };
                            if (value > 0)
                                invokedOnto.AddQEffect(QEffect.Slowed(value).WithExpirationAtEndOfOwnerTurn());
                            CommonRuneRules.ApplyImmunity(invokedOnto, thisRune, true);
                        }
                    });

                if (await caster.Battle.GameLoop.FullCast(invokeTrolistriOnEveryone))
                    CommonRuneRules.RemoveDrawnRune(invokedRune, thisRune);
                else
                    sourceAction.RevertRequested = true;
            })
            .WithWillSaveInvocationTechnical();
        AddRuneAsRuneFeat(ModData.IdPrepend+"RuneTrolistri", runeTrolistri);

        #endregion
        
        #region Level 17 Runes
        
        // Rune runeAiuen = new Rune();
        // AddRuneAsRuneFeat(ModData.IdPrepend+"RuneAiuen", runeAiuen);
        
        // Rune runeRovan = new Rune();
        // AddRuneAsRuneFeat(ModData.IdPrepend+"RuneRovan", runeRovan);
        
        #endregion
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
        runeFeat.FeatGroup = rune.BaseLevel switch
        {
            17 => ModData.FeatGroups.Level17Rune,
            9 => ModData.FeatGroups.Level9Rune,
            1 => ModData.FeatGroups.Level1Rune,
            _ => null
        };
        if (!AllRunes.Contains(rune))
            AllRunes.Add(rune);
        AllRuneFeats.Add(runeFeat);
        ModManager.AddFeat(runeFeat);
        return runeFeat;
    }
}