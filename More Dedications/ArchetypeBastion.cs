using System.Text;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Monsters.L5;
using Dawnsbury.Modding;
//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeBastion
{
    
    //public static readonly FeatName DedicationFeat = ModManager.RegisterFeatName("MoreDedications.Archetype.Bastion.Dedication", "Bastion Dedication");
    //public static readonly FeatName EverstandStanceFeat; // Character Guide content. https://2e.aonprd.com/Feats.aspx?ID=1087&ArchLevel=4
    //public static readonly FeatName EverstandStrikeFeat; // Character Guide content. https://2e.aonprd.com/Feats.aspx?ID=1088&ArchLevel=6
    //public static readonly FeatName NimbleShieldHand; // TODO?
    //public static readonly FeatName DriveBack; // Knights of Lastwall content. https://2e.aonprd.com/Feats.aspx?ID=3617

    public static void LoadMod()
    {
        // Dedication Feat
        TrueFeat bastionDedication = (ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.BastionArchetype,
                "Some say that a good offense is the best defense, but you find such boasting smacks of overconfidence. In your experience, the best defense is a good, solid shield between you and your enemies.",
                "You gain the Reactive Shield {icon:Reaction} fighter feat.")
            .WithPrerequisite(FeatName.ShieldBlock, "Shield Block")
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.ReactiveShield);
            }) as TrueFeat)!;
        bastionDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        ModManager.AddFeat(bastionDedication);

        // Add Agile Shield Grip to Bastion
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            Champion.AgileShieldGripFeatName,
            ModData.Traits.BastionArchetype, 4));

        // Disarming Block
        // TODO: Adjust held items so that you can only disarm them of the attacking weapon
        TrueFeat disarmingBlockFeat = (new TrueFeat(
            ModData.FeatNames.DisarmingBlock,
            4,
            null,
            "{b}Trigger{/b} You Shield Block a melee Strike made with a held weapon.\n\nYou attempt to Disarm the creature whose attack you blocked of the weapon they attacked you with. You can do so even if you don't have a hand free.",
            [ModData.Traits.MoreDedications])
            .WithActionCost(0)
            .WithAvailableAsArchetypeFeat(ModData.Traits.BastionArchetype)
            .WithPrerequisite(FeatName.Athletics, "Trained in Athletics")
            .WithPermanentQEffect("You attempt to Disarm melee attackers when you Shield Block.", qfFeat =>
            {
                // This captures the RaisedShield qf to allow it to call its internal behavior, with
                // the stipulation of executing the disarm functionality after shield blocking.
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
                            
                            // Has to be a melee strike with a disarmable item
                            if (dealt.Power == null || !dealt.Power.HasTrait(Trait.Melee) || !dealt.Power.HasTrait(Trait.Strike) || attacker.HeldItems.Count == 0)
                                return result;
                            
                            // Do disarm stuff
                            int oldMAP = defender.Actions.AttackedThisManyTimesThisTurn;
                            defender.Actions.AttackedThisManyTimesThisTurn = 0;
                            if (defender.HeldItems.FirstOrDefault(item => item.HasTrait(Trait.Disarm)) != null)
                            {
                                Item disarmWeapon =
                                    defender.HeldItems.FirstOrDefault(item => item.HasTrait(Trait.Disarm))!;
                                CombatAction specialDisarm = CombatManeuverPossibilities
                                    .CreateDisarmAction(defender, disarmWeapon)
                                    .WithActionCost(0);
                                await defender.Battle.GameLoop.FullCast(specialDisarm,
                                    ChosenTargets.CreateSingleTarget(attacker));
                            }
                            else
                            {
                                CheckResult disarmResult = CommonSpellEffects.RollCheck(
                                    "Disarming Block",
                                    new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics),
                                        TaggedChecks.DefenseDC(Defense.Reflex)),
                                    defender,
                                    attacker);
                                
                                await CommonAbilityEffects.Disarm(
                                    CombatAction.CreateSimple(
                                        defender,
                                        "Disarming Block"), // Don't assign an item, it'll try to drop it if you crit fail.
                                    defender,
                                    attacker,
                                    disarmResult);
                            }
                            defender.Actions.AttackedThisManyTimesThisTurn = oldMAP;
                            
                            // Return normal shield block stuff
                            return result;
                        };
                    }
                };
            }) as TrueFeat)!;
        ModManager.AddFeat(disarmingBlockFeat);

        // Shielded Stride
        TrueFeat fighterShieldedStrideFeat = new TrueFeat(
            ModData.FeatNames.FighterShieldedStride,
            4,
            "When your shield is up, your enemies' blows can't touch you.",
            "When you have your shield raised, you can Stride to move half your Speed without triggering reactions that are triggered by your movement.",
            [Trait.Fighter, ModData.Traits.MoreDedications])
            .WithPermanentQEffect("While your shield is raised, Striding half your speed doesn't provoke reactions.",
                qfFeat =>
                {
                    qfFeat.StateCheck = async qfThis =>
                    {
                        if (qfThis.Owner.FindQEffect(QEffectId.RaisingAShield) != null)
                        {
                            qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral){ Id = QEffectId.Mobility });
                        }
                    };
                });
        ModManager.AddFeat(fighterShieldedStrideFeat);
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(ModData.FeatNames.FighterShieldedStride, ModData.Traits.BastionArchetype, 6));

        // Reflexive Shield
        TrueFeat fighterReflexiveShieldFeat = new TrueFeat(
            ModData.FeatNames.FighterReflexiveShield,
            6,
            "You can use your shield to fend off the worst of area effects and other damage.",
            "When you Raise your Shield, you gain your shield's circumstance bonus to Reflex saves. If you have the Shield Block reaction, damage you take as a result of a Reflex save can trigger that reaction, even if the damage isn't physical damage.",
            [Trait.Fighter, ModData.Traits.MoreDedications])
            .WithPermanentQEffect("Raise a Shield benefits your Reflex saves. If you have Shield Block, you can block any damage from a Reflex save.",
                qfFeat =>
                {
                    // This captures the RaisedShield qf to allow it to call its internal behavior,
                    // with the addition of executing the reflexive functionality for shield blocking.
                    qfFeat.AfterYouAcquireEffect = async (qfThis, qfAcquired) =>
                    {
                        if (qfAcquired.Id == QEffectId.RaisingAShield)
                        {
                            // Reflex Save Bonus
                            var oldDefensiveBonus = qfAcquired.BonusToDefenses;
                            qfAcquired.BonusToDefenses = (qfThis2, action, defense) =>
                            {
                                if (oldDefensiveBonus == null)
                                    return null;
                                
                                // Get normal raised shield stuff
                                Bonus? oldResult = oldDefensiveBonus.Invoke(qfThis2, action, defense);
                                if (defense != Defense.Reflex)
                                    return oldResult;
                                
                                // Get reflexive bonus
                                Bonus newResult = qfThis2.Owner.HeldItems.Any(itm =>
                                    itm.HasTrait(Trait.TowerShield) && qfThis2.Owner.HasEffect(QEffectId.TakingCover)) ? 
                                    new Bonus(4, BonusType.Circumstance, "raised tower shield in cover") :
                                    new Bonus(2, BonusType.Circumstance, "raised shield");
                                return newResult;
                            };
                            
                            // Shield Block Update
                            if (qfThis.Owner.HasFeat(FeatName.ShieldBlock))
                            {
                                var oldDamageDealt = qfAcquired.YouAreDealtDamage;
                                qfAcquired.YouAreDealtDamage = async (qfRaisedShield, attacker, dealt, defender) =>
                                {
                                    if (oldDamageDealt == null)
                                        return null;
                                    
                                    // Get normal shield block stuff
                                    DamageModification? result = await oldDamageDealt.Invoke(qfRaisedShield, attacker, dealt, defender);

                                    if (result != null) // Shield blocked normally
                                        return result;
                                    
                                    // Check Reflexive Shield Block
                                    Item? shield = defender.HeldItems.FirstOrDefault(itm => itm.HasTrait(Trait.Shield));
                                    return await ReflexiveShieldBlockYouAreDealtDamage(attacker, dealt, defender, defender);
                                    
                                    // Copied from decompiled code.
                                    // Not very modular, doesn't play nice with other things that want to interact with this.
                                    async Task<DamageModification?> ReflexiveShieldBlockYouAreDealtDamage(
                                      Creature attacker2,
                                      DamageStuff damageStuff,
                                      Creature targetedCreature,
                                      Creature blockingCreature)
                                    {
                                        if (shield == null || damageStuff.Power is not { } action ||
                                            ((action.SavingThrow is not { } save || save.Defense != Defense.Reflex)
                                             && (action.ActiveRollSpecification is not { } rollSpec ||
                                                 rollSpec.TaggedDetermineDC.InvolvedDefense !=
                                                 Defense.Reflex)))
                                            return null;
                                        int preventHowMuch = Math.Min(shield.Hardness + (blockingCreature.HasEffect(QEffectId.ShieldAlly) ? 2 : 0) + (Magus.DoesSparklingTargeShieldBlockApply(damageStuff, blockingCreature) ? (blockingCreature.Level >= 7 ? 2 : 1) : 0), damageStuff.Amount);
                                         string promptText = (targetedCreature == blockingCreature ? "You are" : targetedCreature?.ToString() + " is") +
                                                          $" about to be dealt {damageStuff.Amount} damage by {damageStuff.Power?.Name}.\nUse Shield Block to prevent "
                                                          + (preventHowMuch == damageStuff.Amount ? "all" : preventHowMuch.ToString())
                                                          + " of that damage?";
                                        bool flag1;
                                        bool flag2;
                                        if (blockingCreature.HasEffect(QEffectId.QuickShieldBlock) && !blockingCreature.HasEffect(QEffectId.QuickShieldBlockUsedUp))
                                        {
                                            flag1 = await blockingCreature.Battle.AskForConfirmation(blockingCreature, IllustrationName.FreeAction, promptText, "{icon:FreeAction} Take free action");
                                            if (flag1)
                                              blockingCreature.AddQEffect(new QEffect()
                                              {
                                                  Id = QEffectId.QuickShieldBlockUsedUp
                                              }.WithExpirationAtStartOfSourcesTurn(blockingCreature, 1));
                                            flag2 = true;
                                        }
                                        else
                                        {
                                            flag1 = await blockingCreature.Battle.AskToUseReaction(blockingCreature, promptText);
                                            flag2 = false;
                                        }
                                        if (flag1)
                                        {
                                            if (!flag2)
                                                qfRaisedShield.YouAreDealtDamage = null;
                                            if (blockingCreature.HasEffect(QEffectId.AggressiveBlock) && blockingCreature.IsAdjacentTo(attacker2))
                                                attacker2.AddQEffect(Doorwarden.CreateAggressiveBlockTemporaryQEffect(attacker2, blockingCreature));
                                            return new ReduceDamageModification(preventHowMuch, "Shield block");
                                        }
                                        return null;
                                    }
                                };
                            }
                        }
                    };
                });
        ModManager.AddFeat(fighterReflexiveShieldFeat);
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(ModData.FeatNames.FighterReflexiveShield, ModData.Traits.BastionArchetype, 8));

        // Add Shield Warden to Bastion
        TrueFeat bastionShieldWarden = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.ShieldWarden,
            ModData.Traits.BastionArchetype,
            8);
        // Removes the requirement, "You must be a Fighter, or you must have Shield Ally as your divine ally." .
        bastionShieldWarden.Prerequisites.RemoveAll(req =>
            req.Description.Contains("must have Shield Ally") || req.Description.Contains("must be a Fighter,"));
        ModManager.AddFeat(bastionShieldWarden);
    }
}