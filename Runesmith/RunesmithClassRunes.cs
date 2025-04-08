using System.Text.RegularExpressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
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
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunesmithClassRunes
{
    public static List<Rune> AllRunes { get; } = new List<Rune>();
    public static List<Feat> AllRuneFeats { get; } = new List<Feat>();
    
    // Rune Feats
    public static Feat? RuneFeatAtryl;
    public static Feat? RuneFeatEsvadir;
    public static Feat? RuneFeatHoltrik;
    public static Feat? RuneFeatMarssyl;
    public static Feat? RuneFeatOljinex;
    public static Feat? RuneFeatPluuna;
    public static Feat? RuneFeatRanshu;
    public static Feat? RuneFeatDiacriticSun;
    public static Feat? RuneFeatDiacriticUr;
    public static Feat? RuneFeatZohk;
    
    public static void LoadRunes()
    {
        Rune runeAtryl = new Rune(
            "Atryl, Rune of Fire",
            ModTraits.Atryl, 
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
                return
                    $"The bearer takes {heightenedVar}d6 fire damage, with a basic Fortitude save; on a critical failure, they are dazzled for 1 round.";
            },
            InvokeTechnicalTraits = [Trait.IsHostile],
            UsageCondition = ((attacker, defender) =>
            {
                return defender.EnemyOf(attacker) ? Usability.Usable : Usability.NotUsableOnThisCreature("not an enemy");
            }),
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
            {
                int resistReductionAmount = 6 + (caster!=null ? ((caster.Level-thisRune.BaseLevel) / 2) : 0);
                DrawnRune atrylPassive = new DrawnRune(thisRune, thisRune.Name, "Fire resistance reduced by " + resistReductionAmount + ".")
                {
                    Illustration = thisRune.Illustration,
                    Source = caster,
                    Value = resistReductionAmount, // Value might be an unnecessary field, aesthetically. // TODO: use Key field?
                    Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
                    StateCheck = qfSelf =>
                    {
                        if ((qfSelf as DrawnRune)!.Disabled)
                            return;
                        
                        Resistance? fireResist = qfSelf.Owner.WeaknessAndResistance.Resistances.FirstOrDefault( (res => res.DamageKind == DamageKind.Fire));
                        if (fireResist != null && fireResist.Value > 0)
                        {
                            QEffect? existingAtryl =
                                qfSelf.Owner.QEffects.FirstOrDefault(qfSearch => qfSearch.Name == "Atryl, Rune of Fire" && qfSearch != qfSelf);
                            if (existingAtryl != null && existingAtryl.Value >= qfSelf.Value && existingAtryl.AppliedThisStateCheck)
                                return;
                            fireResist.Value = Math.Max(0, fireResist.Value - qfSelf.Value);
                        }
                    },
                };
                return atrylPassive;
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    int roundHalfLevel = ((caster.Level - 1) / 2);
                    int damageAmount = 2 + roundHalfLevel * 2;
                    CheckResult result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Fortitude,
                        caster.ClassOrSpellDC());
                    await CommonSpellEffects.DealBasicDamage(sourceAction, caster, target, result,
                        damageAmount + "d6", DamageKind.Fire);
                    if (result == CheckResult.CriticalFailure)
                    {
                        target.AddQEffect(QEffect.Dazzled().WithExpirationOneRoundOrRestOfTheEncounter(caster, false));
                    }
                }
                
                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            },
        };
        RuneFeatAtryl = CreateAndAddRuneFeat("RunesmithPlaytest.RuneAtryl", runeAtryl);
        
        Rune runeEsvadir = new Rune(
            "Esvadir, Rune of Whetstones",
            ModTraits.Esvadir,
            IllustrationName.WoundingRunestone,
            1,
            "drawn on a piercing or slashing weapon or unarmed Strike", 
            "This serrated rune, when placed on a blade, ensures it will never go dull.",
            "On a successful Strike, the weapon deals an additional 2 persistent bleed damage per weapon damage die.",
            "The essence of sharpness is released outwards from the rune, dealing 2d6 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.",
            "The damage of the invocation increases by 2d6.",
            null /*No additional traits*/) 
        {
            InvocationTextWithHeightening = (thisRune, level) =>
            {
                const int baseValue = 2;
                int roundHalfLevel = ((level - thisRune.BaseLevel) / 2);
                int damageAmount = 2 + roundHalfLevel * 2;
                string heightenedVar = S.HeightenedVariable(damageAmount, baseValue);
                return
                    $"The essence of sharpness is released outwards from the rune, dealing {heightenedVar}d6 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.";
            },
            LevelFormat = "+2",
            UsageCondition = (attacker, defender) =>
            {
                bool isAlly = defender.FriendOf(attacker);
                Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                return isAlly ? Usability.Usable : allyNotUsable; // Can always do Unarmed Strikes, so always drawable.
                
            },
            InvokeTechnicalTraits = [Trait.IsHostile],
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
            {
                // TODO: Check if sourceAction has like, `sourceAction.Target is AreaTarget` or something, so as to potentially apply to all items instead of making a prompt at all.
                
                // Target a specific item
                List<string> validItemsString = new List<string>();
                List<Item> validItems = new List<Item>();
                foreach (Item item in target.HeldItems.Where(item =>
                     item.WeaponProperties != null && item.WeaponProperties.DamageKind != null &&
                     (item.DetermineDamageKinds().Contains(DamageKind.Piercing) || item.DetermineDamageKinds().Contains(DamageKind.Slashing))))
                {
                    validItemsString.Add(item.Name);
                    validItems.Add(item);
                }
                validItems.Add(caster.UnarmedStrike);
                validItemsString.Add("unarmed strikes");
                
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    "Which item, or unarmed strikes, would you like to apply this rune to?",
                    validItemsString.ToArray()
                );
                
                Item targetItem = validItems[chosenOption.Index];

                DrawnRune esvadirPassive = new DrawnRune(
                    thisRune,
                    $"{thisRune.Name} ({targetItem.Name})",
                    $"The target item or piercing and slashing unarmed strikes deal 2 persistent bleed damage per weapon damage die.",
                    ExpirationCondition.Ephemeral,
                    caster,
                    thisRune.Illustration)
                {
                    Source = caster,
                    Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
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
                            action.ChosenTargets == null || action.ChosenTargets.ChosenCreature == null || // or null targets
                            action.ChosenTargets.ChosenCreature == qfSelf.Owner || // or I'm my target for any reason
                            !(actionItem.DetermineDamageKinds().Contains(DamageKind.Piercing) || actionItem.DetermineDamageKinds().Contains(DamageKind.Slashing))) // or it's not piercing or slashing damage
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

                        DiceFormula bleedAmount = DiceFormula.FromText(
                            ((action.CheckResult == CheckResult.CriticalSuccess ? 2 : 1) * 2 * actionItem.WeaponProperties!.DamageDieCount).ToString(),
                            thisRune.Name);
                        
                        if (action.CheckResult >= CheckResult.Success)
                        {
                            action.ChosenTargets.ChosenCreature.AddQEffect(QEffect.PersistentDamage(bleedAmount, DamageKind.Bleed));
                        }
                    },
                }
                    .WithItemOrUnarmedRegulator(targetItem);
                
                return esvadirPassive;
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                List<Creature> adjacentCreatures = new List<Creature>(caster.Battle.AllCreatures.Where(cr => cr.IsAdjacentTo(target)));
                
                // Create action wrapper for targeting and roll-inspection of invoking from target to adjacent creature.
                CombatAction invokeEsvadirOnToAdjacentCreature = new CombatAction(
                    target, // Get creatures adjacent to the rune, who is the creature with the drawn rune being invoked
                    thisRune.Illustration,
                    $"Invoke {thisRune.Name}",
                    new List<Trait>(thisRune.Traits).Append(Trait.DoNotShowInCombatLog).ToArray(),
                    thisRune.InvocationTextWithHeightening(thisRune, caster.Level),
                    Target.AdjacentCreature().WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                    {
                        bool isEnemy = defender.EnemyOf(caster);
                        bool isAdjacent = defender.IsAdjacentTo(target);
                        return isEnemy ? (isAdjacent ? Usability.Usable : Usability.NotUsableOnThisCreature("Not adjacent")) : Usability.NotUsableOnThisCreature("Not enemy");
                    }))
                        .WithActionCost(0)
                        .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration)) // TODO: doesn't work
                        .WithSavingThrow(new SavingThrow(Defense.Fortitude, caster.ClassOrSpellDC()))
                        .WithEffectOnEachTarget(async (selfAction, caster, target, result) =>
                        {
                            if (!thisRune.IsImmuneToThisInvocation(target))
                            {
                                // CheckResult result = CommonSpellEffects.RollSavingThrow(
                                //     target, 
                                //     CombatAction.CreateSimple(caster, $"Invoked {thisRune.Name}"),
                                //     Defense.Fortitude,
                                //     caster.ClassOrSpellDC());
                                int roundHalfLevel = ((caster.Level - thisRune.BaseLevel) / 2);
                                int damageAmount = 2 + roundHalfLevel * 2;
                                await CommonSpellEffects.DealBasicDamage(
                                    sourceAction,
                                    caster,
                                    target,
                                    result,
                                    damageAmount + "d6",
                                    DamageKind.Slashing);
                            }
                            
                            thisRune.RemoveDrawnRune(invokedRune);
                            thisRune.ApplyImmunity(target);
                        });
                
                await caster.Battle.GameLoop.FullCast(invokeEsvadirOnToAdjacentCreature);
            },
            
        };
        RuneFeatEsvadir = CreateAndAddRuneFeat("RunesmithPlaytest.RuneEsvadir", runeEsvadir);

        Rune runeHoltrik = new Rune(
            "Holtrik, Rune of Dwarven Ramparts",
            ModTraits.Holtrik,
            IllustrationName.ArmorPotencyRunestone,
            1,
            "drawn on a shield",
            "Similarity in the Dwarven words for “wall” and “shield” ensure that this angular rune, once used to shore up tunnels, can apply equally well in the heat of battle.",
            "A shield bearing this rune increases its circumstance bonus to AC by 1.",
            "You call the shield to its rightful place. You Raise the Shield bearing the rune, as if the rune-bearer had used Raise a Shield, and the shield retains the increased bonus to AC until the beginning of the creature's next turn.",
            null,
            [Trait.Dwarf])
        {
            DrawTechnicalTraits = [Trait.Shield],
            UsageCondition = (attacker, defender) =>
            {
                bool hasShield = defender.HeldItems.Any(item => item.HasTrait(Trait.Shield));
                Usability shieldNotUsable = Usability.NotUsableOnThisCreature("doesn't have a shield");
                bool isAlly = defender.FriendOf(attacker);
                Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                return isAlly ? (hasShield ? Usability.Usable : shieldNotUsable) : allyNotUsable;
            },
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
            {
                // TODO: Check if sourceAction has like, `sourceAction.Target is AreaTarget` or something, so as to potentially apply to all items instead of making a prompt at all.
                
                // Target a specific item
                Item targetItem;
                switch (target.HeldItems.Count(item => item.HasTrait(Trait.Shield)))
                {
                    case 0:
                        return null;
                    case 1: 
                        targetItem = target.HeldItems.First(item => item.HasTrait(Trait.Shield));
                        break;
                    default:
                        targetItem = await target.Battle.AskForConfirmation(caster, (Illustration) IllustrationName.MagicWeapon, "Which shield would you like to apply this rune to?", target.HeldItems[0].Name, target.HeldItems[1].Name) ? target.HeldItems[0] : target.HeldItems[1];
                        break;
                }
                
                DrawnRune drawnHoltrik = new DrawnRune(
                    thisRune,
                    $"{thisRune.Name} ({targetItem.Name})",
                    "The circumstance bonus from Raising a Shield is increased by 1.",
                    ExpirationCondition.Ephemeral,
                    caster,
                    thisRune.Illustration)
                {
                    Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
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

                        if (!qfSelf.Owner.HasEffect(QEffectId.RaisingAShield) || // If they aren't raising a shield,
                            target.QEffects.FirstOrDefault( // or already have a Holtrik,
                                qfSearch => qfSearch.Name == qfSelf.Name && qfSearch != qfSelf) != null)
                            return null; // No bonus.

                        return new Bonus(1, BonusType.Untyped, "Holtrik (raised shield)");
                    },
                }.WithItemRegulator(targetItem);
                
                return drawnHoltrik;
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    // Raise their Shield
                    bool hasShieldBlock = target.HasEffect(QEffectId.ShieldBlock) || target.WieldsItem(Trait.AlwaysOfferShieldBlock);
                    target.AddQEffect(QEffect.RaisingAShield(hasShieldBlock));
                
                    // Since the rune-bearer retains the bonus, we remake the rune QF,
                    QEffect? modifiedPassive = await thisRune.NewDrawnRune!.Invoke(sourceAction, caster, target, thisRune);
                    modifiedPassive!.Name = "Invoked " + modifiedPassive.Name;
                    // modify its duration,
                    modifiedPassive.ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn;
                    modifiedPassive.CannotExpireThisTurn = false;
                    // modify its traits so that it cannot be invoked again,
                    modifiedPassive.Traits.Remove(ModTraits.Etched);
                    modifiedPassive.Traits.Remove(ModTraits.Traced);
                    modifiedPassive.Traits.Add(ModTraits.Invocation);
                    // and finally add it.
                    target.AddQEffect(modifiedPassive);
                }
                
                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            },
        };
        RuneFeatHoltrik = CreateAndAddRuneFeat("RunesmithPlaytest.RuneHoltrik", runeHoltrik);
        
        Rune runeMarssyl = new Rune(
            "Marssyl, Rune of Impact",
            ModTraits.Marssyl, 
            IllustrationName.ThunderingRunestone,
            1,
            "drawn on a bludgeoning weapon or unarmed Strike",
            "This rune magnifies force many times over as it passes through the rune’s concentric rings.",
            "The weapon deals 1 bludgeoning splash damage per weapon damage die. If the weapon is a melee weapon, the rune-bearer is immune to this splash damage.",
            "The weapon vibrates as power concentrates within it. The next successful Strike made with the weapon before the end of its wielder's next turn deals an additional die of damage and the target must succeed at a Fortitude save against your class DC or be pushed 10 feet in a straight line backwards, or 20 feet on a critical failure.",
            null,
            null /*No special extra traits*/)
        { 
            UsageCondition = (attacker, defender) =>
            {
                bool isAlly = defender.FriendOf(attacker);
                Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                return isAlly ? Usability.Usable : allyNotUsable; // Can always do Unarmed Strikes, so always drawable.
            },
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
            {
                // TODO: Check if sourceAction has like, `sourceAction.Target is AreaTarget` or something, so as to potentially apply to all items instead of making a prompt at all.
                
                // Target a specific item
                List<string> validItemsString = new List<string>();
                List<Item> validItems = new List<Item>();
                foreach (Item item in target.HeldItems.Where(item => 
                             item.WeaponProperties != null && item.WeaponProperties.DamageKind != null &&
                             item.DetermineDamageKinds().Contains(DamageKind.Bludgeoning)))
                {
                    validItemsString.Add(item.Name);
                    validItems.Add(item);
                }
                validItems.Add(caster.UnarmedStrike);
                validItemsString.Add("unarmed strikes");
                
                ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                    thisRune.Illustration,
                    "Which item, or unarmed strikes, would you like to apply this rune to?",
                    validItemsString.ToArray()
                );
                
                Item targetItem = validItems[chosenOption.Index];

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
                    Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
                    AfterYouTakeAction = async (QEffect qfSelf, CombatAction action) => // Add splash
                    {
                        if ((qfSelf as DrawnRune)!.Disabled)
                            return;
                        Item? qfItem = (qfSelf as DrawnRune)?.DrawnOn as Item;
                        Item? actionItem = action.Item;
                        
                        // This many complex conditionals is really hard to work out so I did it the long way.
                        // Fail to bleed if,
                        if (actionItem == null || qfItem == null || // either item is blank
                            !action.HasTrait(Trait.Strike) || // or the action isn't a strike
                            action.ChosenTargets == null || action.ChosenTargets.ChosenCreature == null || // or null targets
                            action.ChosenTargets.ChosenCreature == qfSelf.Owner || // or I'm my target for any reason
                            !actionItem.DetermineDamageKinds().Contains(DamageKind.Bludgeoning)) // or it's not bludgeoning damage
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
                        
                        Item buffedItem = action.Item; // Use the action item in case it's any unarmed strike.
                        
                        DiceFormula splashAmount = DiceFormula.FromText(buffedItem.WeaponProperties!.DamageDieCount.ToString(), thisRune.Name);
                        
                        // If the strike at least failed,
                        if (action.CheckResult > CheckResult.CriticalFailure)
                        {
                            await CommonSpellEffects.DealDirectSplashDamage(CombatAction.CreateSimple(qfSelf.Owner, "Marssyl"), splashAmount, action.ChosenTargets.ChosenCreature, DamageKind.Bludgeoning); // deal damage to the target.
                            
                            if (action.CheckResult > CheckResult.Failure) // If the strike also at least succeeded,
                            {
                                foreach (Creature target in qfSelf.Owner.Battle.AllCreatures.Where(cr =>
                                             action.ChosenTargets.ChosenCreature.IsAdjacentTo(cr))) // Loop through all adjacent creatures,
                                {
                                    if (target != qfSelf.Owner || !buffedItem.HasTrait(Trait.Melee)) // And if it's a melee attack, skip me, otherwise include me when I,
                                        await CommonSpellEffects.DealDirectSplashDamage(CombatAction.CreateSimple(qfSelf.Owner, "Marssyl"), splashAmount, target, DamageKind.Bludgeoning); // splash them too.
                                }
                            }
                        }
                    },
                }.WithItemOrUnarmedRegulator(targetItem);
                
                return marssylPassive;
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    QEffect invokeEffect = new QEffect(
                        $"Invoked {thisRune.Name}",
                        $"The next successful Strike made with {(invokedRune.DrawnOn as Item)?.Name} before the end of your next turn deals an additional die of damage, and the target must succeed at a Fortitude save against your class DC or be pushed 10 feet in a straight line backwards, or 20 feet on a critical failure.",
                        ExpirationCondition.ExpiresAtEndOfYourTurn,
                        caster,
                        thisRune.Illustration)
                    {
                        Tag = invokedRune.DrawnOn as Item,
                        IncreaseItemDamageDieCount = (qfSelf, item) =>
                        {
                            return item == qfSelf.Tag as Item || (item.HasTrait(Trait.Unarmed) && (qfSelf.Tag as Item).HasTrait(Trait.Unarmed));
                        },
                        AfterYouTakeAction = async (qfSelf, action) =>
                        {
                            Item? qfItem = qfSelf.Tag as Item;
                            Item? actionItem = action.Item;
                            // Do invoke effect if:
                            if (actionItem != null && qfItem != null && // stuff isn't null,
                                (actionItem == qfItem || (actionItem.HasTrait(Trait.Unarmed) && qfItem.HasTrait(Trait.Unarmed))) && // and the items match or are any unarmed strikes
                                action.HasTrait(Trait.Strike) && // the action is a strike,
                                action.CheckResult >= CheckResult.Success) // and it at least succeeds.
                            {
                                action.Owner.RemoveAllQEffects(qfToRemove => qfToRemove == qfSelf);
                                CheckResult result = CommonSpellEffects.RollSavingThrow(action.ChosenTargets.ChosenCreature, CombatAction.CreateSimple(action.Owner, $"Invoked {thisRune.Name}"), Defense.Fortitude, action.Owner.ClassOrSpellDC());
                                int tilePush = result <= CheckResult.Failure ? (result == CheckResult.CriticalFailure ? 4 : 2) : 0;
                                await action.Owner.PushCreature(action.ChosenTargets.ChosenCreature, tilePush);
                            }
                        }
                    };
                    
                    target.AddQEffect(invokeEffect);
                }

                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            },
        };
        RuneFeatMarssyl = CreateAndAddRuneFeat("RunesmithPlaytest.RuneMarssyl", runeMarssyl);
        Match pushEffectEnd = Regex.Match(RuneFeatMarssyl.RulesText, "or 20 feet on a critical failure"); // Quick adjustment for rules clarification, but only in the feat text.
        RuneFeatMarssyl.RulesText = RuneFeatMarssyl.RulesText.Insert((pushEffectEnd.Index + pushEffectEnd.Length),
            " {i}(Dawnsbury: Pushing ignores diagonal cost)");

        // TODO: Oljinex

        // TODO: Pluuna

        // TODO: Ranshu

        // TODO: Sun-

        // TODO: Ur-

        // PUBLISH: Zohk's restrictions are applied for each Stride, rather than across multiple actions.
            // Last possible minute, I thought of a somewhat-accurate implementation, MAYBE for a future update?
            // At the start of the bearer's turn, get the distance to the caster.
            // For the rest of the bearer's turn, any time they take the new stride action, it filters out tiles that
            // aren't closer than that distance. THAT would be a bonus which only requires the turn's movement be closer.
            // The exact nature of how you're supposed to apply Zohk's bonus is a little unclear to me from the original wording anyway.
        Rune runeZohk = new Rune(
            "Zohk, Rune of Homecoming",
            ModTraits.Zohk,
            IllustrationName.ReturningRunestone,
            1,
            "drawn on a creature",
            "This circular mark is meant to allow travelers to always find their way home.",
            "The target can Stride with a +15-foot status bonus, but only if their destination space is closer to you than when they started.",
            "(teleportation) You call the rune-bearer to your side. You teleport the target to any unoccupied square adjacent to you. If the bearer is unwilling, they can attempt a Will save to negate the effect.",
            null,
            [Trait.Arcane])
        {
            NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
            {
                DrawnRune drawnZohk = new DrawnRune(
                    thisRune,
                    thisRune.Name,
                    $"You can Stride with a +15-foot status bonus if your destination space is closer to {caster.Name}.",
                    ExpirationCondition.Ephemeral,
                    caster,
                    thisRune.Illustration)
                {
                    ProvideContextualAction = qfThis =>
                    {
                        if ((qfThis as DrawnRune)!.Disabled)
                            return null;
                        
                        CombatAction zohkStride = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(IllustrationName.FleetStep, thisRune.Illustration),
                            "Stride (Zohk)",
                            [Trait.Move],
                            qfThis.Description!,
                            Target.Self() // Behavior is somewhat unreliable. Removed since the world doesn't end if it immediately reverts.
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
                            .WithEffectOnSelf( async (thisAction, self) =>
                            {
                                self.AddQEffect( new QEffect()
                                {
                                    ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                                    BonusToAllSpeeds = qfSpeed => new Bonus(3, BonusType.Status, qfThis.Name!),
                                });
                                
                                Creature runeCaster = qfThis.Source!;
                                if (!await RunesmithPlaytest.StrideCloserToEnemyAsync(qfThis.Owner, runeCaster, $"Stride closer to {runeCaster.Name} or right-click to cancel."))
                                {
                                    thisAction.RevertRequested = true;
                                }
                            });
                        
                        return new ActionPossibility(zohkStride, PossibilitySize.Full);
                    },
                };
                
                return drawnZohk;
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    CheckResult result = CheckResult.Failure;
                    if (!target.FriendOf(caster))
                    {
                        result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Will, caster.ClassOrSpellDC());
                    }

                    if (result <= CheckResult.Failure)
                    {
                        List<Option> options = [];
                        
                        // Populate options with empty adjacent tiles
                        foreach (Tile tile in caster.Battle.Map.Tiles)
                        {
                            if (tile.IsFree && tile.IsAdjacentTo(caster.Occupies))
                            {
                                options.Add(new TileOption(tile, "Tile (" + tile.X + "," + tile.Y + ")", null, (AIUsefulness)int.MinValue, true));
                            }
                        }
                        
                        // Prompts the user for their desired tile and returns it or null
                        Option selectedOption = (await caster.Battle.SendRequest(new AdvancedRequest(caster, $"Choose a tile to teleport {target.Name} to.", options)
                        {
                            IsMainTurn = false,
                            IsStandardMovementRequest = false,
                            TopBarIcon = thisRune.Illustration,
                            TopBarText = $"Choose a tile to teleport {target.Name} to.",
                        })).ChosenOption;
                        
                        if (selectedOption != null)
                        {
                            if (selectedOption is TileOption selectedTile)
                            {
                                await CommonSpellEffects.Teleport(target, selectedTile.Tile);
                                Sfxs.Play(SfxName.PhaseBolt);
                            }
                        }
                    }
                }
                
                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            }
        };
        RuneFeatZohk = CreateAndAddRuneFeat("RunesmithPlaytest.RuneZohk", runeZohk);
    }

    /// <summary>
    /// Creates a new <see cref="RuneFeat"/> from a given instance of a <see cref="Rune"/>, adds it to <see cref="RunesmithClassRunes.AllRunes"/>, adds it to <see cref="RunesmithClassRunes.AllRuneFeats"/>, then adds it to the ModManager.
    /// </summary>
    /// <param name="technicalName">The technical name to be passed into <see cref="ModManager.RegisterFeatName"/>.</param>
    /// <param name="rune">The <see cref="Rune"/> to create a RuneFeat for.</param>
    /// <returns>(<see cref="RuneFeat"/>) The feat associated with the Rune.</returns>
    public static RuneFeat CreateAndAddRuneFeat(
        string technicalName,
        Rune rune)
    {
        RuneFeat runeFeat = new RuneFeat(
            ModManager.RegisterFeatName(technicalName, rune.Name),
            rune);
        if (!AllRunes.Contains(rune))
            AllRunes.Add(rune);
        AllRuneFeats.Add(runeFeat);
        ModManager.AddFeat(runeFeat);
        return runeFeat;
    }
}