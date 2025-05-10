using System.Drawing;
using System.Text.RegularExpressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.AuraAnimations;
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
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunesmithClassRunes
{
    public static List<Rune> AllRunes { get; } = new List<Rune>();
    public static List<RuneFeat> AllRuneFeats { get; } = new List<RuneFeat>();
    
    // Rune Feats
    public static Feat? RuneFeatAtryl;
    public static Feat? RuneFeatEsvadir;
    public static Feat? RuneFeatHoltrik;
    public static Feat? RuneFeatMarssyl;
    public static Feat? RuneFeatOljinex;
    public static Feat? RuneFeatPluuna;
    public static Feat? RuneFeatRanshu;
    public static Feat? RuneFeatSunDiacritic;
    public static Feat? RuneFeatUrDiacritic;
    public static Feat? RuneFeatZohk;
    
    public static void LoadRunes()
    {
        /* TODO: Consider altering the way runes apply Item effects based on these Item fields to look into:
         * WithPermanentQEffectWhenWorn
         * WithOnCreatureWhenWorn
         * StateCheckWhenWielded
         */
        
        Rune runeAtryl = new Rune(
            "Atryl, Rune of Fire",
            Enums.Traits.Atryl, 
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
                    CheckResult result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Fortitude, RunesmithPlaytest.RunesmithDC(caster));
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
        }
        .WithDetrimentalPassiveTechnical()
        .WithDamagingInvocationTechnical()
        .WithFortitudeSaveInvocationTechnical();
        RuneFeatAtryl = CreateAndAddRuneFeat("RunesmithPlaytest.RuneAtryl", runeAtryl);
        
        // BUG: Add ignores concealment to the invocation... or should I?
        Rune runeEsvadir = new Rune(
            "Esvadir, Rune of Whetstones",
            Enums.Traits.Esvadir,
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
                return
                    $"The essence of sharpness is released outwards from the rune, dealing {heightenedVar}d6 slashing damage to a creature adjacent to the rune-bearer, with a basic Fortitude save.";
            },
            LevelFormat = "+2",
            UsageCondition = (attacker, defender) =>
            {
                if (PlayerProfile.Instance.IsBooleanOptionEnabled("RunesmithPlaytest.EsvadirOnEnemies"))
                    return Usability.Usable;
                bool isAlly = defender.FriendOf(attacker);
                Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                return isAlly ? Usability.Usable : allyNotUsable; // Can always do Unarmed Strikes, so always drawable.
            },
            InvokeTechnicalTraits = [
                Trait.DoesNotRequireAttackRollOrSavingThrow, // Indicates the initial invocation doesn't have a saving throw.
            ],
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
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

                            
                            // Determine weapon damage dice count
                            string weaponDamageDiceCount = actionItem.WeaponProperties!.DamageDieCount.ToString();;
                            if (action.TrueDamageFormula is { } trueDamage)
                            {
                                Capture diceCountCapture = Regex.Match(trueDamage.ToString(), @"(\d+)d\d+").Groups[1];
                                if (diceCountCapture.Value != "")
                                    weaponDamageDiceCount = diceCountCapture.Value;
                            }
                            DiceFormula bleedAmount = DiceFormula.FromText(
                                (2 * int.Parse(weaponDamageDiceCount) * (action.CheckResult == CheckResult.CriticalSuccess ? 2 : 1)).ToString(),
                                thisRune.Name);
                            
                            //DiceFormula bleedAmount = DiceFormula.FromText(
                                //((action.CheckResult == CheckResult.CriticalSuccess ? 2 : 1) * 2 * actionItem.WeaponProperties!.DamageDieCount).ToString(),
                                //thisRune.Name);
                            
                            if (action.CheckResult >= CheckResult.Success)
                            {
                                action.ChosenTargets.ChosenCreature.AddQEffect(QEffect.PersistentDamage(bleedAmount, DamageKind.Bleed));
                            }
                        },
                    }
                        .WithItemOrUnarmedRegulator(targetItem);
                    
                    return esvadirPassive;
                }
                
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
                
                if (sourceAction?.Target is AreaTarget)
                {
                    foreach (Item validItem in validItems)
                    {
                        DrawnRune? esvadirPassive = MakeEsvadirPassive(validItem);
                        // Determine the way the rune is being applied.
                        if (sourceAction.HasTrait(Enums.Traits.Etched))
                            esvadirPassive = esvadirPassive.WithIsEtched();
                        else if (sourceAction.HasTrait(Enums.Traits.Traced))
                            esvadirPassive = esvadirPassive.WithIsTraced();
        
                        target.AddQEffect(esvadirPassive);
                    }

                    return new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                }
                else
                {
                    ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{sourceAction.Name}{{/b}}\nWhich item, or unarmed strikes, would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                        validItemsString.ToArray()
                    );
                
                    Item targetItem = validItems[chosenOption.Index];

                    return MakeEsvadirPassive(targetItem);
                }
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
                    thisRune.InvocationTextWithHeightening(thisRune, caster.Level)!,
                    Target.RangedCreature(1)/*AdjacentCreature()*/.WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                    {
                        bool isEnemy = defender.EnemyOf(caster);
                        bool isAdjacent = defender.IsAdjacentTo(target);
                        return isEnemy ? (isAdjacent ? Usability.Usable : Usability.NotUsableOnThisCreature("Not adjacent")) : Usability.NotUsableOnThisCreature("Not enemy");
                    }))
                        .WithActionCost(0)
                        .WithProjectileCone(VfxStyle.BasicProjectileCone(thisRune.Illustration))
                        .WithSoundEffect(SfxName.RayOfFrost)
                        .WithSavingThrow(new SavingThrow(Defense.Fortitude, RunesmithPlaytest.RunesmithDC(caster)))
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
        }
        .WithDamagingInvocationTechnical()
        .WithFortitudeSaveInvocationTechnical();
        RuneFeatEsvadir = CreateAndAddRuneFeat("RunesmithPlaytest.RuneEsvadir", runeEsvadir);

        Rune runeHoltrik = new Rune(
            "Holtrik, Rune of Dwarven Ramparts",
            Enums.Traits.Holtrik,
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
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
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
                            
                            // If they aren't raising a shield,
                            if (!qfSelf.Owner.HasEffect(QEffectId.RaisingAShield))
                                return null; // no bonus.
                            
                            // If not the first of multiple duplicate effects,
                            if (qfSelf != qfSelf.Owner.QEffects.First(qf =>
                                    qf is DrawnRune dr && dr.Rune.RuneId == Enums.Traits.Holtrik))
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
                        break;
                    default:
                        if (sourceAction?.Target is AreaTarget)
                        {
                            foreach (Item validItem in target.HeldItems)
                            {
                                DrawnRune? holtrikPassive = MakeHoltrikPassive(validItem);
                                // Determine the way the rune is being applied.
                                if (sourceAction.HasTrait(Enums.Traits.Etched))
                                    holtrikPassive = holtrikPassive.WithIsEtched();
                                else if (sourceAction.HasTrait(Enums.Traits.Traced))
                                    holtrikPassive = holtrikPassive.WithIsTraced();
        
                                target.AddQEffect(holtrikPassive);
                            }

                            return new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                        }
                        else
                        {
                            Item targetItem = await target.Battle.AskForConfirmation(caster, IllustrationName.MagicWeapon,
                                $"{{b}}{sourceAction.Name}{{/b}}\nWhich shield would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                                target.HeldItems[0].Name,
                                target.HeldItems[1].Name) ? target.HeldItems[0] : target.HeldItems[1];

                            return MakeHoltrikPassive(targetItem);
                        }
                        break;
                }
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    // Raise their Shield
                    Possibilities shieldActions = Possibilities.Create(target)
                        .Filter( ap =>
                        {
                            if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                                return false;
                            ap.CombatAction.ActionCost = 0;
                            ap.RecalculateUsability();
                            return true;
                        });
                    List<Option> actions = await target.Battle.GameLoop.CreateActions(target, shieldActions, null);
                    await target.Battle.GameLoop.OfferOptions(target, actions, true);
                    
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
                
                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            },
        }
        .WithDrawnOnShieldTechnical();
        RuneFeatHoltrik = CreateAndAddRuneFeat("RunesmithPlaytest.RuneHoltrik", runeHoltrik);
        
        Rune runeMarssyl = new Rune(
            "Marssyl, Rune of Impact",
            Enums.Traits.Marssyl, 
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
                        Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
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
                                action.ChosenTargets == null || action.ChosenTargets.ChosenCreature == null || // or null targets
                                action.ChosenTargets.ChosenCreature == qfSelf.Owner || // or I'm my target for any reason
                                !actionItem.DetermineDamageKinds().Contains(DamageKind.Bludgeoning)) // or it's not bludgeoning damage
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
                                    qf is DrawnRune dr && dr.Rune.RuneId == Enums.Traits.Marssyl))
                                return;
                            
                            // Determine weapon damage dice count
                            string weaponDamageDiceCount = actionItem.WeaponProperties!.DamageDieCount.ToString();;
                            if (action.TrueDamageFormula is { } trueDamage)
                            {
                                Capture diceCountCapture = Regex.Match(trueDamage.ToString(), @"(\d+)d\d+").Groups[1];
                                if (diceCountCapture.Value != "")
                                    weaponDamageDiceCount = diceCountCapture.Value;
                            }
                            DiceFormula splashAmount = DiceFormula.FromText(weaponDamageDiceCount, thisRune.Name);
                            
                            // If the strike at least failed,
                            if (action.CheckResult > CheckResult.CriticalFailure)
                            {
                                await CommonSpellEffects.DealDirectSplashDamage(action /*CombatAction.CreateSimple(qfSelf.Owner, "Marssyl")*/, splashAmount, action.ChosenTargets.ChosenCreature, DamageKind.Bludgeoning); // deal damage to the target.
                                
                                if (action.CheckResult > CheckResult.Failure) // If the strike also at least succeeded,
                                {
                                    foreach (Creature target in qfSelf.Owner.Battle.AllCreatures.Where(cr =>
                                                 action.ChosenTargets.ChosenCreature.IsAdjacentTo(cr))) // Loop through all adjacent creatures,
                                    {
                                        if (target != qfSelf.Owner || !actionItem.HasTrait(Trait.Melee)) // And if it's a melee attack, skip me, otherwise include me when I,
                                            await CommonSpellEffects.DealDirectSplashDamage(CombatAction.CreateSimple(qfSelf.Owner, "Marssyl"), splashAmount, target, DamageKind.Bludgeoning); // splash them too.
                                    }
                                }
                            }
                        },
                    }.WithItemOrUnarmedRegulator(targetItem);
                    
                    return marssylPassive;
                }
                
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
                
                if (sourceAction?.Target is AreaTarget)
                {
                    foreach (Item validItem in validItems)
                    {
                        DrawnRune? marssylPassive = MakeMarssylPassive(validItem);
                        // Determine the way the rune is being applied.
                        if (sourceAction.HasTrait(Enums.Traits.Etched))
                            marssylPassive = marssylPassive.WithIsEtched();
                        else if (sourceAction.HasTrait(Enums.Traits.Traced))
                            marssylPassive = marssylPassive.WithIsTraced();
        
                        target.AddQEffect(marssylPassive);
                    }

                    return new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                }
                else
                {
                    ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                        thisRune.Illustration,
                        $"{{b}}{sourceAction.Name}{{/b}}\nWhich item, or unarmed strikes, would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                        validItemsString.ToArray()
                    );
                
                    Item targetItem = validItems[chosenOption.Index];

                    return MakeMarssylPassive(targetItem);
                }
            },
            InvocationBehavior = async (sourceAction, thisRune, caster, target, invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    QEffect invokedMarssyl = invokedRune.NewInvocationEffect(
                        $"The next successful Strike made with {(invokedRune.DrawnOn as Item)?.Name} before the end of your next turn deals an additional die of damage, and the target must succeed at a Fortitude save against your class DC or be pushed 10 feet in a straight line backwards, or 20 feet on a critical failure.",
                        ExpirationCondition.ExpiresAtEndOfYourTurn);
                    invokedMarssyl.Tag = invokedRune.DrawnOn as Item;
                    invokedMarssyl.IncreaseItemDamageDieCount = (qfSelf, item) =>
                    {
                        return item == qfSelf.Tag as Item ||
                               (item.HasTrait(Trait.Unarmed) && (qfSelf.Tag as Item).HasTrait(Trait.Unarmed));
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
                            action.CheckResult >= CheckResult.Success) // and it at least succeeds.
                        {
                            action.Owner.RemoveAllQEffects(qfToRemove => qfToRemove == qfSelf);
                            CheckResult result = CommonSpellEffects.RollSavingThrow(action.ChosenTargets.ChosenCreature,
                                CombatAction.CreateSimple(action.Owner, $"Invoked {thisRune.Name}"), Defense.Fortitude,
                                RunesmithPlaytest.RunesmithDC(action.Owner));
                            int tilePush = result <= CheckResult.Failure
                                ? (result == CheckResult.CriticalFailure ? 4 : 2)
                                : 0;
                            await action.Owner.PushCreature(action.ChosenTargets.ChosenCreature, tilePush);
                        }
                    };
                    
                    target.AddQEffect(invokedMarssyl);
                }

                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            },
        };
        RuneFeatMarssyl = CreateAndAddRuneFeat("RunesmithPlaytest.RuneMarssyl", runeMarssyl);

        Rune runeOljinex = new Rune(
            "Oljinex, Rune of Cowards' Bane",
            Enums.Traits.Oljinex,
            IllustrationName.FearsomeRunestone,
            1,
            "drawn on a shield",
            "This rune resembles a broken arrow.",
            "While the shield is raised, it also grants the bearer a +1 status bonus to AC against physical ranged attacks. {i}(NYI: doesn't check for damage types, works against any ranged attack.){/i}",
            "(illusion, mental, visual) The rune creates an illusion in the minds of all creatures adjacent to the rune-bearer that lasts for 1 round. The illusion is of a impeding terrain. Creatures affected by this invocation must succeed at a DC 5 flat check when they take a move action or else it's lost. The DC is 11 instead if they attempt to move further away from the rune-bearer. This lasts for 1 round or until they disbelieve the illusion by using a Seek action against your class DC.",
            additionalTraits: [Trait.Occult, Enums.Traits.Rune])
        {
            UsageCondition = (attacker, defender) =>
            {
                bool hasShield = defender.HeldItems.Any(item => item.HasTrait(Trait.Shield));
                Usability shieldNotUsable = Usability.NotUsableOnThisCreature("doesn't have a shield");
                bool isAlly = defender.FriendOf(attacker);
                Usability allyNotUsable = Usability.NotUsableOnThisCreature("enemy creature");
                if (PlayerProfile.Instance.IsBooleanOptionEnabled("RunesmithPlaytest.OljinexOnEnemies"))
                    return hasShield ? Usability.Usable : shieldNotUsable;
                return isAlly ? (hasShield ? Usability.Usable : shieldNotUsable) : allyNotUsable;
            },
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
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
                        Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
                        BonusToDefenses = (qfSelf, attackAction, targetDefense) =>
                        {
                            if ((qfSelf as DrawnRune)!.Disabled)
                                return null;
                            
                            // TODO: enforce the physical damage part of ranged physical attacks.
                            if (targetDefense != Defense.AC || (attackAction != null && !attackAction.HasTrait(Trait.Ranged)) || !qfSelf.Owner.HasEffect(QEffectId.RaisingAShield))
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
                        break;
                    default:
                        if (sourceAction?.Target is AreaTarget)
                        {
                            foreach (Item validItem in target.HeldItems)
                            {
                                DrawnRune? oljinexPassive = MakeOljinexPassive(validItem);
                                // Determine the way the rune is being applied.
                                if (sourceAction.HasTrait(Enums.Traits.Etched))
                                    oljinexPassive = oljinexPassive.WithIsEtched();
                                else if (sourceAction.HasTrait(Enums.Traits.Traced))
                                    oljinexPassive = oljinexPassive.WithIsTraced();
        
                                target.AddQEffect(oljinexPassive);
                            }

                            return new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of items.
                        }
                        else
                        {
                            Item targetItem = await target.Battle.AskForConfirmation(caster, (Illustration) IllustrationName.MagicWeapon, $"{{b}}{sourceAction.Name}{{/b}}\nWhich shield would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?", target.HeldItems[0].Name, target.HeldItems[1].Name) ? target.HeldItems[0] : target.HeldItems[1];

                            return MakeOljinexPassive(targetItem);
                        }
                        break;
                }
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                // Define this in case it needs to change for behavioral logic
                Tile cannotMoveAwayFrom = target.Occupies; //invokedRune.Owner.Occupies;
                
                foreach (Creature cr in target.Battle.AllCreatures.Where(cr => cr.IsAdjacentTo(target) && !thisRune.IsImmuneToThisInvocation(target)))
                {
                    if (cr.IsImmuneTo(Trait.Illusion) || cr.IsImmuneTo(Trait.Mental) || cr.IsImmuneTo(Trait.Visual))
                        continue;
                    
                    QEffect invokedOljinex = invokedRune.NewInvocationEffect(
                        "INCOMPLETE TEXT. COMPLAIN AT ANASE IF YOU SEE THIS TEXT!",
                        ExpirationCondition.ExpiresAtStartOfSourcesTurn);
                    
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
                    invokedOljinex.Description = $"If you attempt a move action, you must succeed at a DC 5 flat check or it is lost. The DC is 11 instead if you attempt to move further away from {{Blue}}{target.Name}{{/Blue}}.";
                    invokedOljinex.FizzleOutgoingActions = async (qfThis, action, stringBuilder) =>
                    {
                        if (!action.HasTrait(Trait.Move))
                            return false;

                        // Define this in case it needs to change for behavioral logic
                        Tile cannotMoveFrom = invokedRune.Owner.Occupies;//target.Occupies;
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
                                (action, attacker, defender) => new CalculatedNumber(RunesmithPlaytest.RunesmithDC(defender!), "Class DC", [])))
                            .WithActionId(ActionId.Seek)
                            .WithActionCost(1)
                            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                            {
                                if (result > CheckResult.Failure)
                                    caster.RemoveAllQEffects(qf => qf == qfThis);
                            });
                        return new ActionPossibility(seekOljinex, PossibilitySize.Full).WithPossibilityGroup("remove debuff");
                    };
                    
                    cr.AddQEffect(invokedOljinex);
                    thisRune.ApplyImmunity(cr);
                }
                
                thisRune.RemoveDrawnRune(invokedRune);
            },
        }
        .WithDrawnOnShieldTechnical();
        RuneFeatOljinex = CreateAndAddRuneFeat("RunesmithPlaytest.RuneOljinex", runeOljinex);

        Rune runePluuna = new Rune(
            "Pluuna, Rune of Illumination",
            Enums.Traits.Pluuna,
            IllustrationName.HolyRunestone,
            1,
            "drawn on a creature", // or armor
            "While many runes are enchanted to glow, light is the focus of this simple rune.",
            "This rune sheds a revealing light in a 20-foot emanation. Creatures inside it take a -1 item penalty to Stealth checks, and the rune-bearer can't be undetected.",
            "Each creature in the emanation must succeed at a Fortitude save or be dazzled for 1 round. The light fades, but leaves behind a dim glow which prevents the target from being undetected for 1 round.",
            additionalTraits: [Trait.Light])
        {
            InvokeTechnicalTraits = [
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
                            MagicCircleAuraAnimation(IllustrationName.AngelicHaloCircle, Microsoft.Xna.Framework.Color.Gold, emanationSize);
                    },
                    StateCheck = qfThis =>
                    {
                        qfThis.Owner.DetectionStatus.Undetected = false;
                        qfThis.Owner.Battle.AllCreatures
                            .Where(cr =>
                                cr.DistanceTo(qfThis.Owner) <= emanationSize)
                            .ForEach(cr =>
                                cr.AddQEffect(new QEffect("Pluuna's Light", "You have a -1 item penalty to Stealth checks.", ExpirationCondition.Ephemeral, qfThis.Owner, IllustrationName.Light)
                                {
                                    BonusToSkills = skill =>
                                    {
                                        return skill == Skill.Stealth
                                            ? new Bonus(-1, BonusType.Item, thisRune.Name)
                                            : null;
                                    }
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
                        .WithSoundEffect(SfxName.MinorAbjuration)
                        .WithSavingThrow(new SavingThrow(Defense.Fortitude, RunesmithPlaytest.RunesmithDC(caster)))
                        .WithEffectOnEachTarget(async (selfAction, invokeEE, invokedOnto, result) =>
                        {
                            // foreach (Creature cr in caster.Battle.AllCreatures.Where(cr => cr.DistanceTo(target) <= emanationSize))
                            // {
                            //     if (thisRune.IsImmuneToThisInvocation(cr))
                            //         continue;
                            //
                            //     CheckResult result = CommonSpellEffects.RollSavingThrow(cr, sourceAction, Defense.Fortitude, caster.ClassOrSpellDC());
                            //     if (result <= CheckResult.Failure)
                            //     {
                            //         cr.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 1));
                            //     }
                            //
                            //     thisRune.ApplyImmunity(cr);
                            // }
                            
                            if (!thisRune.IsImmuneToThisInvocation(invokedOnto))
                            {
                                if (result <= CheckResult.Failure)
                                {
                                    invokedOnto.AddQEffect(QEffect.Dazzled().WithExpirationAtStartOfSourcesTurn(caster, 1));
                                }

                                thisRune.ApplyImmunity(invokedOnto);
                            }
                        });

                if (await caster.Battle.GameLoop.FullCast(
                        invokePluunaOnEveryone /*, ChosenTargets.CreateSingleTarget(target)*/))
                {
                    QEffect invokedPluuna = invokedRune.NewInvocationEffect(
                        "This dim light prevents you from being undetected.",
                        ExpirationCondition.ExpiresAtStartOfSourcesTurn);
                    invokedPluuna.StateCheck = qfThis => { qfThis.Owner.DetectionStatus.Undetected = false; };

                    thisRune.RemoveDrawnRune(invokedRune);
                    target.AddQEffect(invokedPluuna);
                }
                else
                    sourceAction.RevertRequested = true;
            },
        }
        .WithFortitudeSaveInvocationTechnical();
        RuneFeatPluuna = CreateAndAddRuneFeat("RunesmithPlaytest.RunePluuna", runePluuna);

        Rune runeRanshu = new Rune(
            "Ranshu, Rune of Thunder",
            Enums.Traits.Ranshu,
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
            PassiveTextWithHeightening = (Rune thisRune, int charLevel) =>
            {
                int bonusDamage = (charLevel - thisRune.BaseLevel) / 2;
                string damage = "1d4" + 
                                (bonusDamage > 0 ?
                                    $"+{S.HeightenedVariable(bonusDamage, 0)}" :
                                    null);
                return $"If the bearer does not take a move action at least once on its turn, lightning finds it at the end of its turn, dealing {damage} electricity damage.";
            },
            InvocationTextWithHeightening = (Rune thisRune, int charLevel) =>
            {
                int numDice = 2 + (int)Math.Floor((charLevel - thisRune.BaseLevel) / 2d)*2;
                string heightenedVar = S.HeightenedVariable(numDice, 2);

                return
                    $"The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes {heightenedVar}d6 electricity damage, with a basic Fortitude save.";
            },
            UsageCondition = ((attacker, defender) =>
            {
                return defender.EnemyOf(attacker) ? Usability.Usable : Usability.NotUsableOnThisCreature("not an enemy");
            }),
            NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
            {
                int bonusDamage = ((caster?.Level ?? 1) - thisRune.BaseLevel) / 2;
                DiceFormula immobilityDamage = DiceFormula.FromText($"1d4+{bonusDamage}");
                DrawnRune ranshuPassive = new DrawnRune(thisRune, thisRune.Name, $"If you don't take a move action at least once during your turn, you take {immobilityDamage} electricity damage.")
                {
                    Illustration = thisRune.Illustration,
                    Source = caster,
                    //Value = immobilityDamage.ExpectedValue, // Value might be an unnecessary field, aesthetically. // TODO: use Key field?
                    Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
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
                        await CommonSpellEffects.DealDirectDamage(null, immobilityDamage, self,  CheckResult.Failure, DamageKind.Electricity);
                    },
                };
                return ranshuPassive;
            },
            InvocationBehavior = async (CombatAction sourceAction, Rune thisRune, Creature caster, Creature target, DrawnRune invokedRune) =>
            {
                if (!thisRune.IsImmuneToThisInvocation(target))
                {
                    int numDice = 2 + (int)Math.Floor((caster.Level - thisRune.BaseLevel) / 2d)*2;
                    DiceFormula invocationDamage = DiceFormula.FromText($"{numDice}d6");
                    CheckResult result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Fortitude,
                        RunesmithPlaytest.RunesmithDC(caster));
                    await CommonSpellEffects.DealBasicDamage(sourceAction, caster, target, result,
                        invocationDamage, DamageKind.Electricity);
                }
                
                thisRune.RemoveDrawnRune(invokedRune);
                thisRune.ApplyImmunity(target);
            },
        }
        .WithDetrimentalPassiveTechnical()
        .WithDamagingInvocationTechnical()
        .WithFortitudeSaveInvocationTechnical();
        RuneFeatRanshu = CreateAndAddRuneFeat("RunesmithPlaytest.RuneRanshu", runeRanshu);

        Rune runeSunDiacritic = new Rune(
            "Sun-, Diacritic Rune of Preservation",
            Enums.Traits.SunDiacritic,
            IllustrationName.DisruptingRunestone,
            1,
            "drawn on a rune",
            "This spiraling diacritic channels the magic of a rune outwards, then back to the same location, allowing a rune to reconstitute itself.",
            "After the base rune is invoked, the rune automatically Traces itself back upon the same target.\n\n{b}Special{/b} You can have only one copy of {i}sun-, diacritic rune of preservation{/i} applied at a given time, and once you invoke it, you cannot Etch or Trace it again this combat.",
            additionalTraits: [Enums.Traits.Diacritic])
            {
                UsageCondition = (attacker, defender) =>
                {
                    if (attacker.PersistentUsedUpResources.UsedUpActions.Contains("SunDiacritic"))
                        return Usability.NotUsable("already invoked this combat");
                    if (DrawnRune.GetDrawnRunes(null, defender) is { } drawnRunes)
                        if (drawnRunes.Count == 0)
                            return Usability.NotUsableOnThisCreature("not a rune-bearer");
                        else if (drawnRunes.Find(dr => dr.AttachedDiacritic == null) == null)
                            return Usability.NotUsableOnThisCreature("all runes have diacritics");
                    return Usability.Usable;
                },
                NewDrawnRune = async (sourceAction, caster, target, thisRune) =>
                {
                    // Only one instance allowed when drawn
                    foreach (Creature cr in caster.Battle.AllCreatures)
                        cr.RemoveAllQEffects(qf => qf.Traits.Contains(Enums.Traits.SunDiacritic));

                    DrawnRune CreateSunPassive(DrawnRune targetRune)
                    {
                        DrawnRune drawnSun = new DrawnRune(
                            thisRune,
                            $"{thisRune.Name} ({targetRune.Name})",
                            "The base rune is automatically Traced again after being invoked.",
                            ExpirationCondition.Ephemeral,
                            caster,
                            thisRune.Illustration)
                            {
                                Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
                                BeforeInvokingRune = async (thisDr, drInvoked) =>
                                {
                                    if (thisDr.Disabled)
                                        return;
                                    CombatAction sunRedraw = CombatAction.CreateSimple(
                                        drInvoked.Source!,
                                        "Sun, Diacritic Rune of Preservation",
                                        [Enums.Traits.Traced]); // <- Even if it WAS etched before, it's now traced.
                                    await drInvoked.Rune.DrawRuneOnTarget(sunRedraw, thisDr.Source!, drInvoked.Owner, false);
                                    thisDr.Source!.PersistentUsedUpResources.UsedUpActions.Add("SunDiacritic");
                                },
                            }
                            .WithDiacriticRegulator(targetRune);
                
                        return drawnSun;
                    }
                    
                    // Target a specific rune
                    List<string> validRunesString = new List<string>();
                    List<DrawnRune> validRunes = new List<DrawnRune>();
                    foreach (DrawnRune dr in DrawnRune.GetDrawnRunes(null, target).Where(dr => dr.AttachedDiacritic == null))
                    {
                        validRunesString.Add(dr.Name);
                        validRunes.Add(dr);
                    }
                    
                    if (sourceAction?.Target is AreaTarget)
                    {
                        foreach (DrawnRune validRune in validRunes)
                        {
                            DrawnRune? sunPassive = CreateSunPassive(validRune);
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(Enums.Traits.Etched))
                                sunPassive = sunPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(Enums.Traits.Traced))
                                sunPassive = sunPassive.WithIsTraced();
        
                            target.AddQEffect(sunPassive);
                        }

                        return new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of runes.
                    }
                    else
                    {
                        ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                            thisRune.Illustration,
                            $"{{b}}{sourceAction.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                            validRunesString.ToArray()
                        );
                    
                        DrawnRune targetRune = validRunes[chosenOption.Index];
                        
                        return CreateSunPassive(targetRune);
                    }
                }
            }
            .WithDrawnOnRuneTechnical();
        RuneFeatSunDiacritic = CreateAndAddRuneFeat("RunesmithPlaytest.RuneSunDiacritic", runeSunDiacritic);

        // TODO: Make final decision on whether this buffs Marssyl's invocation.
        Rune runeUrDiacritic = new Rune(
            "Ur-, Diacritic Rune of Intensity",
            Enums.Traits.UrDiacritic,
            IllustrationName.DemolishingRunestone,
            1,
            "drawn on a rune",
            "This diacritic accentuates the base rune with bolder lines to give greater weight to its effects.",
            "When the base rune is invoked, its invocation gains a status bonus to damage equal to your Intelligence modifier.")
            {
                UsageCondition = (attacker, defender) =>
                {
                    if (DrawnRune.GetDrawnRunes(null, defender) is { } drawnRunes)
                        if (drawnRunes.Count == 0)
                            return Usability.NotUsableOnThisCreature("not a rune-bearer");
                        else if (drawnRunes.Find(dr => dr.AttachedDiacritic == null) == null)
                            return Usability.NotUsableOnThisCreature("all runes have diacritics");
                        else if (drawnRunes.Find(dr => dr.Rune.InvokeTechnicalTraits.Contains(Trait.IsHostile)) == null)
                            return Usability.NotUsableOnThisCreature("no damaging runes");
                    return Usability.Usable;
                },
                NewDrawnRune = async (CombatAction? sourceAction, Creature? caster, Creature target, Rune thisRune) =>
                {
                    DrawnRune CreateUrPassive(DrawnRune targetRune)
                    {
                        DrawnRune drawnUr = new DrawnRune(
                            thisRune,
                            $"{thisRune.Name} ({targetRune.Name})",
                            $"The base rune's invocation damage gains a +{caster.Abilities.Intelligence} status bonus.",
                            ExpirationCondition.Ephemeral,
                            caster,
                            thisRune.Illustration)
                        {
                            Traits = new List<Trait>(thisRune.Traits), //[..thisRune.Traits],
                            BeforeInvokingRune = async (thisDr, drInvoked) =>
                            {
                                if (thisDr.Disabled)
                                    return;
                                QEffect invokeBonus = new QEffect()
                                {
                                    ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                                    BonusToDamage = (qfThis, action, defender) =>
                                    {
                                        if (thisDr.Disabled)
                                            return null;

                                        if (!action.HasTrait(Enums.Traits.Invocation)
                                            || action.Tag is not Rune
                                            || drInvoked != thisDr.DrawnOn)
                                            return null;
                                        
                                        return new Bonus(caster.Abilities.Intelligence, BonusType.Status,
                                                "Ur, Diacritic Rune of Intensity");
                                    },
                                };
                                thisDr.Source.AddQEffect(invokeBonus);
                            },
                        }.WithDiacriticRegulator(targetRune);
                
                        return drawnUr;
                    }
                    
                    // Target a specific rune
                    List<string> validRunesString = new List<string>();
                    List<DrawnRune> validRunes = new List<DrawnRune>();
                    foreach (DrawnRune dr in DrawnRune.GetDrawnRunes(null, target).Where(dr => dr.AttachedDiacritic == null))
                    {
                        validRunesString.Add(dr.Name);
                        validRunes.Add(dr);
                    }
                    
                    if (sourceAction?.Target is AreaTarget)
                    {
                        foreach (DrawnRune validRune in validRunes)
                        {
                            DrawnRune? urPassive = CreateUrPassive(validRune);
                            // Determine the way the rune is being applied.
                            if (sourceAction.HasTrait(Enums.Traits.Etched))
                                urPassive = urPassive.WithIsEtched();
                            else if (sourceAction.HasTrait(Enums.Traits.Traced))
                                urPassive = urPassive.WithIsTraced();
        
                            target.AddQEffect(urPassive);
                        }

                        return new DrawnRune(thisRune); // Return an ephemeral DrawnRune since we just applied this to a whole batch of runes.
                    }
                    else
                    {
                        ChoiceButtonOption chosenOption = await caster.AskForChoiceAmongButtons(
                            thisRune.Illustration,
                            $"{{b}}{sourceAction.Name}{{/b}}\nWhich rune would you like to apply {{Blue}}{thisRune.Name}{{/Blue}} to?",
                            validRunesString.ToArray()
                        );
                    
                        DrawnRune targetRune = validRunes[chosenOption.Index];
                        
                        return CreateUrPassive(targetRune);
                    }
                }
            }
            .WithDrawnOnRuneTechnical();
        RuneFeatUrDiacritic = CreateAndAddRuneFeat("RunesmithPlaytest.RuneUrDiacritic", runeUrDiacritic);

        // Last possible minute, I thought of a somewhat-accurate implementation, MAYBE for a future update?
        // At the start of the bearer's turn, get the distance to the caster.
        // For the rest of the bearer's turn, any time they take the new stride action, it filters out tiles that
        // aren't closer than that distance. THAT would be a bonus which only requires the turn's movement be closer.
        // The exact nature of how you're supposed to apply Zohk's bonus is a little unclear to me from the original wording anyway.
        Rune runeZohk = new Rune(
            "Zohk, Rune of Homecoming",
            Enums.Traits.Zohk,
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
                        result = CommonSpellEffects.RollSavingThrow(target, sourceAction, Defense.Will, RunesmithPlaytest.RunesmithDC(caster));
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
        }
        .WithWillSaveInvocationTechnical();
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