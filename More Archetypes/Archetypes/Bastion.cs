using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.MoreShields;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Bastion
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Lv2: Bastion Dedication
        // ArchetypeFeats.CreateOrUpdateDedication
        Feat basDed = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.Bastion,
                "Some say that a good offense is the best defense, but you find such boasting smacks of overconfidence. In your experience, the best defense is a good, solid shield between you and your enemies.",
                "You gain the Reactive Shield {icon:Reaction} fighter feat.",
                null/*,
                dedication =>
                {
                    foreach (Prerequisite req in dedication.Prerequisites
                                 .Where(req =>
                                     req.Description.Contains("Shield Block"))
                                 .ToList())
                        dedication.Prerequisites.Remove(req);
                }*/)
            .WithPrerequisite(FeatName.ShieldBlock, "Shield Block")
            .WithOnSheet(values =>
                values.GrantFeat(FeatName.ReactiveShield));
        ModData.FeatNames.BastionDedication = basDed.FeatName;
        yield return basDed;

        // Lv4: Agile Shield Grip for Bastion
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            Champion.AgileShieldGripFeatName, ModData.Traits.Bastion, 4);

        // Lv4: Disarming Block
        // PETR: Disarm the attacking item
        yield return new TrueFeat(
                ModData.FeatNames.DisarmingBlock, 4,
                "With deft and practiced movement, you block at an angle to potentially dislodge the weapon.",
                $$"""
                  {b}Trigger{/b} You Shield Block a melee Strike made with a held weapon.

                  You attempt to Disarm the creature whose attack you blocked of the weapon they attacked you with. You can do so even if you don't have a hand free.

                  {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}NYI{/b} This does not target a specific item to Disarm.
                  """,
                [])
            .WithActionCost(0)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Bastion)
            .WithPrerequisite(
                FeatName.Athletics,
                "Trained in Athletics")
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " When you block a melee Strike, attempt to Disarm that weapon.";

                // Relies on MoreShields, where Shield Block executes a CombatAction.
                qfFeat.AfterYouTakeActionReaction = (qfThis, blockAct) =>
                {
                    if (!blockAct.HasTrait(Trait.ShieldBlock)
                        || blockAct.Tag is not DamageEvent dEvent)
                        return null;

                    Creature attacker = dEvent.Source;
                    CombatAction? strike = dEvent.CombatAction;

                    if (strike is null
                        || !strike.HasTrait(Trait.Melee) // Must be melee
                        || !strike.HasTrait(Trait.Strike) // Must be a Strike
                        || strike.Item is null // Must be with an item
                        || attacker.HeldItems.Count(hi => !hi.HasTrait(Trait.Grapplee)) == 0)
                        return null;

                    Creature defender = qfThis.Owner;
                
                    CombatAction disarmingAction = new CombatAction(
                            defender,
                            new SideBySideIllustration(blockAct.Illustration, IllustrationName.Disarm),
                            "Disarming Block",
                            [ModData.ModTrait, Trait.Archetype],
                            """
                            {i}With deft and practiced movement, you block at an angle to potentially dislodge the weapon.{/i}
                            
                            {b}Trigger{/b} You Shield Block a melee Strike made with a held weapon.

                            You attempt to Disarm the creature whose attack you blocked of the weapon they attacked you with. You can do so even if you don't have a hand free.
                            """,
                            Target.Self())
                        .WithActionCost(0)
                        .WithEffectOnEachTarget(async (action2, self, _, _) =>
                        {
                            // Delay execution until triggering damage is resolved
                            attacker.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                            {
                                AfterYouTakeAction = async (_, _) =>
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
                                }
                            });
                        });

                    ReactionOption reactOpt = ReactionOption.WrapFullcastWithChosenTargets(
                            disarmingAction,
                            ChosenTargets.CreateSingleTarget(attacker),
                            $"Attempt to Disarm {attacker.ToColoredName()}.")
                        .WithIsFreeAction();

                    return reactOpt;
                };
            });
        
        // Lv6: Nimble Shield Hand
        yield return new TrueFeat(
                ModData.FeatNames.NimbleShieldHand, 6,
                "You are so used to wielding a shield that you can use another item and switch back to it effortlessly.",
                """
                You can Draw or Pick Up a shield, or Replace an item with a shield, as a {icon:FreeAction} free action.

                This benefit doesn't apply to tower shields, which are still too cumbersome.
                """,
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Bastion)
            .WithPermanentQEffect(
                "You can Draw, Pick Up, or Replace a shield as a {icon:FreeAction} free action. Except for tower shields.",
                qfFeat =>
                {
                    qfFeat.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId is not ActionId.ReplaceItemInHand and not ActionId.DrawItem and not ActionId.PickUpItem)
                            return;
                        if (!action.Item!.HasTrait(Trait.Shield)
                            || action.Item!.HasTrait(Trait.TowerShield)
                            // More Shields mod compatibility, apply to Fortress Shields too.
                            || (ModManager.TryParse("CoverShield", out Trait coverShield)
                                && action.Item!.HasTrait(coverShield)))
                            return;

                        action.ActionCost = 0;
                    };
                });

        // Lv4: Shielded Stride
        yield return new TrueFeat(
                ModData.FeatNames.ShieldedStride, 4,
                "When your shield is up, your enemies' blows can't touch you.",
                "When you have your shield raised, you can Stride to move half your Speed without triggering reactions that are triggered by your movement.",
                [Trait.Fighter])
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis => 
                    qfThis.Name!.WithTag("b") + ". While your shield is raised, Striding half your speed doesn't provoke reactions.";
                
                qfFeat.StateCheck = qfThis =>
                {
                    if (qfThis.Owner.HasEffect(QEffectId.RaisingAShield))
                        qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            { Id = QEffectId.Mobility });
                };
            });
        
        // Lv6: Shielded Stride for Bastion
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.ShieldedStride, ModData.Traits.Bastion, 6);
        
        // Lv6: Reflexive Shield
        yield return new TrueFeat(
                ModData.FeatNames.ReflexiveShield, 6,
                "You can use your shield to fend off explosions and the like.",
                """
                When you Raise your Shield, you gain your shield's circumstance bonus to Reflex saves.

                {b}Special{/b} If you can use the Shield Block reaction, damage you take as a result of a Reflex save can trigger that reaction, even if the damage isn't physical damage.
                """,
                [Trait.Fighter])
            .WithPermanentQEffect(
                "Raise a Shield benefits your Reflex saves. If you have Shield Block, you can block any damage from a Reflex save.",
                qfFeat =>
                {
                    // Apply best shield AC to Reflex saves.
                    qfFeat.BonusToDefenses = (qfThis, _, def) =>
                    {
                        Creature defender = qfThis.Owner;
                        
                        if (def != Defense.Reflex
                            || CommonShieldRules.GetRaisedShields(defender) is not { Count: > 0 } shields
                            || shields.MaxBy(CommonShieldRules.GetAC) is not {} bestShield)
                            return null;

                        bool takingCover = defender.HasEffect(QEffectId.TakingCover)
                            && shields.Any(shield => shield.HasTrait(MoreShields.ModData.Traits.CoverShield));

                        // Use a higher bonus for the nearly-impossible circumstance you have a better AC from one shield but also have a lower-AC cover shield raised
                        int acBonus = takingCover
                            ? 4
                            : CommonShieldRules.GetAC(bestShield) ?? 0;

                        return new Bonus(acBonus, BonusType.Circumstance, "raised shield" + (takingCover ? " in cover" : null));
                    };

                    qfFeat.YourShieldBlockWorksAlsoAgainst = (_, dEvent) =>
                        CommonShieldRules.DoesReflexiveShieldApply(dEvent.CombatAction);
                });
        
        // Lv8: Reflexive Shield for Bastion
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.ReflexiveShield, ModData.Traits.Bastion, 8);

        // Lv8: Shield Warden for Bastion
        TrueFeat bastionShieldWarden = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.ShieldWarden, ModData.Traits.Bastion, 8);
        // Removes the requirement, "You must be a Fighter, or you must have Shield Ally as your divine ally." .
        bastionShieldWarden.Prerequisites.RemoveAll(req =>
            req.Description.Contains("must have Shield Ally")
            || req.Description.Contains("must be a Fighter,"));
        yield return bastionShieldWarden;
        
        // Lv10: Destructive Block
        // TODO: Implement Destructive Block as a 1/encounter ability to double your hardness.
        
        // Lv10: Quick Shield Block for Bastion
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.QuickShieldBlock, ModData.Traits.Bastion, 10);
        
        // Lv12: Mirror Shield for Bastion
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            FeatName.MirrorShield, ModData.Traits.Bastion, 12);
        
        // Lv12: Shield Salvation
        // TODO: Implement with a Destructive Block requirement and make it 2/encounter.
        
        // PETR: Lv16 Improved Reflexive Shield (Lv18 for Bastion)
    }
}