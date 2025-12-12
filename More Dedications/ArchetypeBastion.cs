using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeBastion
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
        
        ModManager.RegisterActionOnEachActionPossibility(ca =>
        {
            if (!ca.Owner.HasEffect(ModData.QEffectIds.NimbleShieldHand))
                return;
            if (ca.ActionId is not ActionId.ReplaceItemInHand and not ActionId.DrawItem and not ActionId.PickUpItem)
                return;
            if (!ca.Item!.HasTrait(Trait.Shield)
                || ca.Item!.HasTrait(Trait.TowerShield)
                // More Shields mod compatibility, apply to Fortress Shields too.
                || (ModManager.TryParse("CoverShield", out Trait coverShield)
                    && ca.Item!.HasTrait(coverShield)))
                return;

            ca.ActionCost = 0;
        });
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Dedication Feat
        Feat bastionDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.BastionArchetype,
                "Some say that a good offense is the best defense, but you find such boasting smacks of overconfidence. In your experience, the best defense is a good, solid shield between you and your enemies.",
                "You gain the Reactive Shield {icon:Reaction} fighter feat.")
            .WithPrerequisite(FeatName.ShieldBlock, "Shield Block")
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.ReactiveShield);
            });
        bastionDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return bastionDedication;

        // Add Agile Shield Grip to Bastion
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            Champion.AgileShieldGripFeatName, ModData.Traits.BastionArchetype, 4);

        // Disarming Block
        // PETR: Disarm the attacking item
        yield return new TrueFeat(
                ModData.FeatNames.DisarmingBlock,
                4,
                null,
                "{b}Trigger{/b} You Shield Block a melee Strike made with a held weapon.\n\nYou attempt to Disarm the creature whose attack you blocked of the weapon they attacked you with. You can do so even if you don't have a hand free.\n\n" + ModData.Illustrations.DawnsburySun.IllustrationAsIconString + " {b}NYI{/b} This does not target a specific item to Disarm.",
                [ModData.Traits.MoreDedications])
            .WithActionCost(0)
            .WithAvailableAsArchetypeFeat(ModData.Traits.BastionArchetype)
            .WithPrerequisite(
                FeatName.Athletics,
                "Trained in Athletics")
            .WithPermanentQEffect(
                "You attempt to Disarm melee attackers when you Shield Block.",
                qfFeat =>
                {
                    // This captures the RaisedShield qf to allow it to call its internal behavior, with
                    // the stipulation of executing the disarm functionality after shield blocking.
                    qfFeat.WhenYouUseShieldBlock = async (qfBlocker, attacker, target, mitigated) => ApplyDelayedShieldBlockEvent(attacker, async action =>
                    {
                        if (!action.HasTrait(Trait.Melee) // Must be melee
                            || !action.HasTrait(Trait.Strike) // Must be a Strike
                            || action.Item is null // Must be with an item
                            || attacker.HeldItems.Count(hi => !hi.HasTrait(Trait.Grapplee)) == 0)
                            return;

                        Creature defender = qfBlocker.Owner;

                        // Ask for confirmation
                        if (!await defender.Battle.AskForConfirmation(
                                defender,
                                action.Item?.Illustration ?? IllustrationName.SteelShield,
                                $"{{b}}Disarming Block{{/b}} {{icon:FreeAction}}\nYou just used Shield Block. Attempt to Disarm {{Blue}}{attacker}{{/Blue}}?",
                                "Disarm"))
                            return;
                    
                        CombatAction disarmingAction = new CombatAction(
                                defender,
                                new SideBySideIllustration(action.Illustration, IllustrationName.Disarm),
                                "Disarming Block",
                                [ModData.Traits.MoreDedications, Trait.Archetype],
                                "{b}Trigger{/b} You Shield Block a melee Strike made with a held weapon.\n\nYou attempt to Disarm the creature whose attack you blocked of the weapon they attacked you with. You can do so even if you don't have a hand free.",
                                Target.Self())
                            .WithActionCost(0)
                            .WithEffectOnEachTarget(async (action2, self, _, _) =>
                            {
                                // Store MAP
                                int oldMAP = self.Actions.AttackedThisManyTimesThisTurn;
                                self.Actions.AttackedThisManyTimesThisTurn = 0;
                                
                                // Choose a suitable disarm option
                                List<Option> options = [
                                    new CancelOption(true)
                                ];
                                foreach (CombatAction disarmOption in CombatManeuverPossibilities
                                             .GetAllOptions(CombatManeuverPossibilities.CreateDisarmPossibility(defender)))
                                {
                                    disarmOption.WithActionCost(0);
                                    // Remove free hand requirement by rebuilding targeting
                                    disarmOption.Target = Target.Reach(disarmOption.Item!)
                                        .WithAdditionalConditionOnTargetCreature(
                                            new TargetWieldsAnItemCreatureTargetingRequirement());
                                    GameLoop.AddDirectUsageOnCreatureOptions(disarmOption, options, true);
                                }
                                options.RemoveAll(option =>
                                    option is CreatureOption crOpt && crOpt.Creature != attacker);
                                
                                // Execute option
                                Option chosenOption = (await self.Battle.SendRequest(
                                    new AdvancedRequest(self, "Choose a Disarm option.", options)
                                    {
                                        TopBarText = "Choose a Disarm option or right-click to cancel.",
                                        TopBarIcon = action2.Illustration,
                                    })).ChosenOption;
                                
                                await chosenOption.Action();
                                
                                // Restore MAP
                                self.Actions.AttackedThisManyTimesThisTurn = oldMAP;
                            });
                        
                        await defender.Battle.GameLoop.FullCast(disarmingAction, ChosenTargets.CreateSingleTarget(attacker));
                    });
                    /*qfFeat.AfterYouAcquireEffect = async (qfThis, qfAcquired) =>
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
                    };*/
                });
        
        // Everstand Stance
        // Character Guide content.
        // https://2e.aonprd.com/Feats.aspx?ID=1087&ArchLevel=4
        
        // Everstand Strike
        // Character Guide content.
        // https://2e.aonprd.com/Feats.aspx?ID=1088&ArchLevel=6
        
        // Nimble Shield Hand
        yield return new TrueFeat(
                ModData.FeatNames.NimbleShieldHand,
                6,
                "You are so used to wielding a shield that you can use another item and switch back to it effortlessly.",
                "You can Draw or Pick Up a shield, or Replace an item with a shield, as a {icon:FreeAction} free action.\n\nThis benefit doesn't apply to tower shields, which are still too cumbersome.",
                [ModData.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(ModData.Traits.BastionArchetype)
            .WithPermanentQEffect(
                "You can Draw, Pick Up, or Replace a shield as a {icon:FreeAction} free action. Except for tower shields.",
                qfFeat => qfFeat.Id = ModData.QEffectIds.NimbleShieldHand);
        
        // Driveback
        // Knights of Lastwall content.
        // https://2e.aonprd.com/Feats.aspx?ID=3617

        // Shielded Stride
        yield return new TrueFeat(
                ModData.FeatNames.FighterShieldedStride,
                4,
                "When your shield is up, your enemies' blows can't touch you.",
                "When you have your shield raised, you can Stride to move half your Speed without triggering reactions that are triggered by your movement.",
                [ModData.Traits.MoreDedications, Trait.Fighter])
            .WithPermanentQEffect(
                "While your shield is raised, Striding half your speed doesn't provoke reactions.",
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
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.FighterShieldedStride, ModData.Traits.BastionArchetype, 6);

        // Reflexive Shield
        yield return new TrueFeat(
                ModData.FeatNames.FighterReflexiveShield,
                6,
                "You can use your shield to fend off the worst of area effects and other damage.",
                "When you Raise your Shield, you gain your shield's circumstance bonus to Reflex saves. If you have the Shield Block reaction, damage you take as a result of a Reflex save can trigger that reaction, even if the damage isn't physical damage.",
                [ModData.Traits.MoreDedications, Trait.Fighter])
            .WithPermanentQEffect(
                "Raise a Shield benefits your Reflex saves. If you have Shield Block, you can block any damage from a Reflex save.",
                qfFeat =>
                {
                    // Bonus to the shield's reflex defenses
                    qfFeat.BonusToDefenses = (qfThis, _, def) =>
                    {
                        Creature defender = qfThis.Owner;
                        
                        if (def is not Defense.Reflex
                            || !defender.HasEffect(QEffectId.RaisingAShield))
                            return null;

                        // Determine bonus
                        bool hasTowerShield = defender.HeldItems.Any(itm =>
                            itm.HasTrait(Trait.TowerShield) && defender.HasEffect(QEffectId.TakingCover));

                        return hasTowerShield
                            ? new Bonus(4, BonusType.Circumstance, "raised tower shield in cover")
                            : new Bonus(2, BonusType.Circumstance, "raised shield");
                    };
                    
                    // Trigger when you take damage from a Reflex save
                    qfFeat.YouAreDealtDamage = async (qfThis, attacker, dStuff, defender) =>
                    {
                        // Checks included in local func
                        if (/*(dStuff.Power?.SavingThrow?.Defense is not Defense.Reflex
                             && dStuff.Power?.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense is not Defense.Reflex)
                            || */!qfThis.Owner.HasEffect(QEffectId.RaisingAShield))
                            return null;

                        // Get beefiest shield
                        if (qfThis.Owner.HeldItems
                                .Where(item => item.HasTrait(Trait.Shield))
                                .MaxBy(item => item.Hardness)
                            is not { } shield)
                            return null;
                        
                        /*// Force it to trigger through Magus effects via DoesSparklingTargeShieldBlockApply
                        QEffect tempQf1 = new QEffect() { Id = QEffectId.SparklingTarge };
                        QEffect tempQf2 = new QEffect() { Id = QEffectId.ArcaneCascade };
                        qfThis.Owner
                            .AddQEffect(tempQf1)
                            .AddQEffect(tempQf2);
                        // Add the trait even if it already has it, Remove() will apply only once, this is safe
                        dStuff.Power.WithExtraTrait(Trait.Spell);

                        DamageModification? blockReduction = await Fighter.ShieldBlockYouAreDealtDamage(
                            attacker,
                            dStuff,
                            defender,
                            qfThis.Owner,
                            shield.Hardness);
                        
                        // Remove work-arounds
                        qfThis.Owner.RemoveAllQEffects(qf => qf == tempQf1 || qf == tempQf2);
                        dStuff.Power.Traits.Remove(Trait.Spell);*/
                        
                        DamageModification? blockReduction = await ReflexiveShieldBlockYouAreDealtDamage(
                            attacker,
                            dStuff,
                            defender,
                            qfThis.Owner,
                            shield);
                        
                        return blockReduction;
                    };
                    
                    // PETR: The Improved Reflexive Shield feat should reduce some amount of UI prompts, since they have mechanical overlap.
                    // Delayed in case of feat load orders
                    qfFeat.StartOfCombatAfterInitiativeOrderIsSetUp = async qfThis =>
                    {
                        // Fire once
                        qfThis.StartOfCombatAfterInitiativeOrderIsSetUp = null;
    
                        // Shield Warden compatibility
                        if (qfFeat.Owner.HasEffect(QEffectId.ShieldWarden))
                        {
                            qfFeat.AddGrantingOfTechnical(
                                ally =>
                                    ally.FriendOfAndNotSelf(qfFeat.Owner) && ally.IsAdjacentTo(qfFeat.Owner),
                                qfAlly =>
                                    qfAlly.YouAreDealtDamage = async (_, attacker, dStuff, defender) =>
                                    {
                                        // Checks included in local func
                                        if (/*(dStuff.Power?.SavingThrow?.Defense is not Defense.Reflex
                                             && dStuff.Power?.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense is not Defense.Reflex)
                                            || */!qfFeat.Owner.HasEffect(QEffectId.RaisingAShield))
                                            return null;
                                        
                                        // Get beefiest shield
                                        if (qfFeat.Owner.HeldItems
                                                .Where(item => item.HasTrait(Trait.Shield))
                                                .MaxBy(item => item.Hardness)
                                            is not { } shield)
                                            return null;
                                        
                                        return await ReflexiveShieldBlockYouAreDealtDamage(
                                            attacker,
                                            dStuff,
                                            defender,
                                            qfFeat.Owner,
                                            shield);
                                    });
                        }
                    };

                    return;
                    
                    // Copied from decompiled code.
                    // Not very modular, doesn't play nice with other things that want to interact with this.
                    async Task<DamageModification?> ReflexiveShieldBlockYouAreDealtDamage(
                      Creature attacker,
                      DamageStuff damageStuff,
                      Creature targetedCreature,
                      Creature blockingCreature,
                      Item shield)
                    {
                        if (damageStuff.Power?.SavingThrow is not { Defense: Defense.Reflex }
                            && damageStuff.Power?.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense != Defense.Reflex)
                            return null;
                        
                        int preventHowMuch = Math.Min(
                            shield.Hardness
                                + (blockingCreature.HasEffect(QEffectId.ShieldAlly) ? 2 : 0)
                                + (Magus.DoesSparklingTargeShieldBlockApply(damageStuff, blockingCreature)
                                    ? blockingCreature.Level >= 15 ? 3 : blockingCreature.Level >= 7 ? 2 : 1 : 0),
                            damageStuff.Amount);
                        
                        if (await blockingCreature.Battle.AskToUseReaction(
                                blockingCreature,
                                $"{(targetedCreature == blockingCreature ? "You are" : targetedCreature + " is")} about to be dealt {damageStuff.Amount} damage by {damageStuff.Power?.Name}.\nUse Shield Block to prevent {(preventHowMuch == damageStuff.Amount ? "all" : preventHowMuch.ToString())} of that damage?",
                                [Trait.ShieldBlock]))
                        {
                            foreach (QEffect qf in blockingCreature.QEffects.ToList())
                                await qf.WhenYouUseShieldBlock.InvokeIfNotNull(qf, attacker, targetedCreature, preventHowMuch);
                            
                            blockingCreature.Overhead(
                                "shield block", Color.White,
                                blockingCreature + " uses {b}Shield Block{/b} to mitigate {b}" + preventHowMuch + "{/b} damage.");
                            
                            return new ReduceDamageModification(preventHowMuch, "Shield block");
                        }
                        return null;
                    }
                });
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.FighterReflexiveShield, ModData.Traits.BastionArchetype, 8);

        // Add Shield Warden to Bastion
        TrueFeat bastionShieldWarden = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.ShieldWarden, ModData.Traits.BastionArchetype, 8);
        // Removes the requirement, "You must be a Fighter, or you must have Shield Ally as your divine ally." .
        bastionShieldWarden.Prerequisites.RemoveAll(req =>
            req.Description.Contains("must have Shield Ally") || req.Description.Contains("must be a Fighter,"));
        yield return bastionShieldWarden;
        
        // Add Quick Shield Block to Bastion
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.QuickShieldBlock, ModData.Traits.BastionArchetype, 10);
        
        /* Higher Level Feats
         * @10 Destructive Block
         * @12 (really: 10) Mirror Shield
         */
    }

    public static void ApplyDelayedShieldBlockEvent(Creature attacker, Func<CombatAction, Task> doWhat)
    {
        attacker.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
        {
            Name = "[DELAYED SHIELD BLOCK RESPONSE]", // Identifier
            Tag = null, // Track whether this has already been executed against a given action
            AfterYouTakeAction = async (qfThis, action) =>
            {
                // End of ANY action, as this is applied during an attack execution
                if (qfThis.Tag != action)
                {
                    await doWhat.Invoke(action);
                    qfThis.Tag = action;
                }
            }
        });
    }
}