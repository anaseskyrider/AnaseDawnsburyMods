using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreDedications.Archetypes;

public static class Assassin
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Dedication
        Feat assassinDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.AssassinArchetype,
                "Targeted killing through stealth and subterfuge is the expertise of an assassin. While assassins are skilled in ending lives and many are evil, some live by a moral code, preying on the wicked, the cruel, or those who revel in unchecked aggression or power.",
                "You gain the Mark for Death activity, which you can use as a {icon:FreeAction} free action at the start of combat.")
            .WithRulesBlockForCombatAction(CreateMarkForDeathAction)
            .WithPrerequisite(values =>
                values.HasFeat(FeatName.Crafting),
                "You must be trained in Crafting.")
            .WithPrerequisite(values =>
                values.HasFeat(FeatName.Deception),
                "You must be trained in Deception.")
            .WithPrerequisite(values =>
                values.HasFeat(FeatName.Stealth),
                "You must be trained in Stealth.")
            .WithPermanentQEffect(
                "You mark a creature, gaining a +2 circumstance bonus to Seeking and Feinting it. Your agile and finesse weapons and unarmed attacks gain the backstabber and deadly d6 traits against your mark.",
                qfFeat =>
                {
                    qfFeat.StartOfCombat = async qfThis =>
                    {
                        await qfThis.Owner.Battle.GameLoop.FullCast(CreateMarkForDeathAction(qfThis.Owner)
                            .WithActionCost(0));
                    };
                    qfFeat.ProvideMainAction = qfThis =>
                        new ActionPossibility(CreateMarkForDeathAction(qfThis.Owner));
                });
        ModData.FeatNames.AssassinDedication = assassinDedication.FeatName;
        yield return assassinDedication;

        // Expert Backstabber
        yield return new TrueFeat(
                ModData.FeatNames.ExpertBackstabber,
                4,
                null,
                "Double the amount of damage dealt by the backstabber weapon trait.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.AssassinArchetype)
            .WithPermanentQEffectAndSameRulesText(qfFeat =>
            {
                qfFeat.Id = ModData.QEffectIds.ExpertBackstabber;
            });
        
        // Poison Resistance
        Feat poisonResistance = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.PoisonResistanceDruid, ModData.Traits.AssassinArchetype, 4);
        poisonResistance.FlavorText = "Your body has become fortified against toxins.";
        ModData.FeatNames.PoisonResistance = poisonResistance.FeatName;
        yield return poisonResistance;

        // Surprise Attack
        Feat surpriseAttack = new TrueFeat(
                ModData.FeatNames.SurpriseAttack,
                4,
                "You act before foes can react.",
                "On the first round of combat, creatures that haven't acted yet are flat-footed to you.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.AssassinArchetype)
            .WithEquivalent(values =>
                values.Class?.ClassTrait is Trait.Rogue
                || values.AdditionalClassTraits.Contains(Trait.Rogue))
            .WithOnCreature(creature =>
                creature.AddQEffect(Rogue.SurpriseAttackQEffect()));
        yield return surpriseAttack;

        // Poison Weapon
        yield return new TrueFeat(
                ModData.FeatNames.PoisonWeapon,
                4,
                "You are adept at drawing and and applying injury poisons.",
                $$"""
                   Drawing an {{ModData.Tooltips.InjuryPoison("injury poison")}} from your inventory is a {icon:FreeAction} free action for you, and you can apply them as {icon:Action} an action instead of the normal number of actions.

                  {b}Special{/b} Each day, you prepare a number of simple injury poisons equal to your level. These poisons automatically deal 1d4 poison damage. Only you can apply them, and they last until the end of your next turn when you do.
                  """,
                [Trait.Manipulate, Trait.Rogue])
            .WithPermanentQEffect(
                "You can draw injury poisons as a free action and can apply them as one action.",
                qfFeat =>
                {
                    bool hasImproved = qfFeat.Owner.HasFeat(ModData.FeatNames.ImprovedPoisonWeapon);
                    string damage = (hasImproved ? 2 : 1) + "d4";
                    qfFeat.Description += $" You prepare {{Blue}}{qfFeat.Owner.Level}{{/Blue}} simple injury poisons each day (they deal {(hasImproved ? "{Blue}2d4{/Blue}" : "1d4")} poison damage).";
                    
                    // Reduce apply-cost to 1
                    qfFeat.Id = QEffectId.QuickApplyPoison;
                    
                    // Drawing poisons is free
                    qfFeat.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId is not ActionId.DrawItem
                            || !(action.Item?.HasTrait(Trait.Poison) ?? false))
                            return;
                        action.ActionCost = 0;
                    };
                    
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.ItemActions)
                            return null;
                        
                        int usedCharges = GetUsedPoisonWeaponCharges(qfThis.Owner);
                        int maxCharges = qfThis.Owner.Level;
                        if (usedCharges >= maxCharges)
                            return null;
                        int remainingCharges = maxCharges - usedCharges;

                        return new ActionPossibility(new CombatAction(
                                qfThis.Owner,
                                IllustrationName.AlchemicalPoison,
                                "Poison weapon (simple injury poison)",
                                [ModData.Traits.ModName, Trait.Basic, Trait.Manipulate],
                                $$"""
                                  {b}Prepared Poisons{/b} {{remainingCharges}}/{{maxCharges}}
                                  
                                  Apply your simple injury poison to a piercing or slashing weapon in your hand.
                                  
                                  The poison automatically deals {{S.HeightenedVariable(hasImproved ? 2 : 1, 1)}}d4 poison damage on a hit or critical hit.{{(hasImproved ? "\n\n{Blue}{b}Improved Poison Weapon{/b} Keep this poison even on a critical failure.{/Blue}" : null)}}
                                  """,
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                    {
                                        if (!self.HasFreeHand)
                                            return "You need a free hand";
                                        List<Item> helds = self.HeldItems.ToList();
                                        if (!helds.Any(item =>
                                                item.WeaponProperties?.DamageKind is DamageKind.Piercing or DamageKind.Slashing))
                                            return "No piercing or slashing weapon";
                                        if (helds.All(item => item.HasTrait(Trait.Poisoned)))
                                            return "All weapons are poisoned";
                                        return null;
                                    }))
                            .WithEffectOnEachTarget(async (action, caster, _, _) =>
                            {
                                List<Item> validWeapons = caster.HeldItems
                                    .Where(item =>
                                        item.WeaponProperties?.DamageKind is DamageKind.Piercing or DamageKind.Slashing
                                        && !item.HasTrait(Trait.Poisoned))
                                    .ToList();
                                
                                // Choose a weapon to apply to
                                Item chosenWeapon;
                                switch (validWeapons.Count)
                                {
                                    case 0:
                                        action.RevertRequested = true;
                                        return;
                                    case 1:
                                        chosenWeapon = validWeapons[0];
                                        break;
                                    default:
                                    {
                                        ChoiceButtonOption chosenButton = await caster.AskForChoiceAmongButtons(
                                            IllustrationName.AlchemicalPoison,
                                            """
                                            {b}Poison Weapon{/b} {icon:Action}
                                            Choose a weapon to poison.
                                            """,
                                            [..validWeapons.Select(item =>
                                                $"{item.Illustration.IllustrationAsIconString} {item.Name}")]);
                                        chosenWeapon = validWeapons[chosenButton.Index];
                                        break;
                                    }
                                }
                                
                                QEffect poisonWeapon = new QEffect(
                                    "Poisoned Weapon",
                                    $"Your next attack with your {{Blue}}{chosenWeapon.Name}{{/Blue}} that hits exposes the target to your simple injury poison (deals {damage} poison damage without a saving throw).",
                                    ExpirationCondition.Never,
                                    caster,
                                    IllustrationName.AlchemicalPoison)
                                {
                                    DoNotShowUpOverhead = true,
                                    WhenExpires = qfThis2 =>
                                    {
                                        AlchemicalItems.DestroyAllPoisonsOn(chosenWeapon);
                                    },
                                }.WithExpirationAtEndOfSourcesNextTurn(caster, true);

                                // Lacks self reference, applied separately.
                                poisonWeapon.AfterYouDealDamage = async (attacker, action2, defender) =>
                                {
                                    if (!action2.HasTrait(Trait.Strike)
                                        || action2.Item != chosenWeapon
                                        || action2.CheckResult < CheckResult.Success)
                                        return;

                                    CombatAction poisonAction = new CombatAction(attacker.Battle.Pseudocreature,
                                        IllustrationName.DragonClaws, "simple injury poison", [Trait.Poison], "",
                                        Target.Self());

                                    await CommonSpellEffects.DealDirectDamage(
                                        poisonAction,
                                        DiceFormula.FromText(damage, "Simple injury poison"),
                                        defender,
                                        CheckResult.Failure,
                                        DamageKind.Poison);
                                    
                                    poisonWeapon.ExpiresAt = ExpirationCondition.Immediately;
                                };
                                
                                if (!hasImproved)
                                {
                                    poisonWeapon.Description += " This effect ends early if you critically fail the attack.";
                                    poisonWeapon.AfterYouTakeAction += async (qfThis2, action2) =>
                                    {
                                        if (action2.HasTrait(Trait.Strike)
                                            && action2.Item == chosenWeapon
                                            && action2.CheckResult == CheckResult.CriticalFailure)
                                        {
                                            qfThis2.Owner.Overhead(
                                                "*poison lost*",
                                                Color.Red,
                                                "Prepared poison from {b}Poison Weapon{/b} lost due to critical failure.");
                                            qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    };
                                }
                                
                                chosenWeapon.Traits.Add(Trait.Poisoned);
                                caster.AddQEffect(poisonWeapon);
                                AddUsedPoisonWeaponCharge(caster);
                            }))
                            .WithPossibilityGroup("Use item");
                    };
                });
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.PoisonWeapon, ModData.Traits.AssassinArchetype, 6);

        // Sneak Attacker
        Feat sneakAttacker = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.SneakAttacker, ModData.Traits.AssassinArchetype, 4);
        yield return sneakAttacker;
        
        // Improved Poison Weapon
        yield return new TrueFeat(
                ModData.FeatNames.ImprovedPoisonWeapon,
                8,
                "You deliver poisons in ways that maximize their harmful effects.",
                $"""
                The damage of your prepared injury poisons increases to 2d4, and are no longer wasted on a critically failed attack roll.

                For other poisons you apply, you gain the benefits of the {AllFeats.GetFeatByFeatName(FeatName.StickyPoison).ToLink("Sticky Poison")} feat.
                """,
                [Trait.Rogue])
            .WithPrerequisite(
                ModData.FeatNames.PoisonWeapon, "Poison Weapon")
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.StickyPoison);
            });
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.ImprovedPoisonWeapon, ModData.Traits.AssassinArchetype, 10);
        
        // TODO: Lv8: Public Execution?
        // License Firebrands!
        
        /* Higher level feats
         * @12 Assassinate
         */
    }

    public static CombatAction CreateMarkForDeathAction(Creature self)
    {
        Illustration icon = IllustrationName.RequiemOfDeath;
        CombatAction markForDeath = new CombatAction(
            self,
            icon,
            "Mark for Death",
            [Trait.Basic, Trait.DoesNotBreakStealth],
            "{i}You've trained to assassinate your foes, and you do so with tenacity and precision.{/i}\n\n{b}Requirements{/b} You can see and hear the creature you intend to mark\n\nYou designate a single creature as your mark. This lasts until the mark dies or you use Mark for Death again. You gain a +2 circumstance bonus to Perception checks to Seek your mark and on Deception checks to Feint against your mark. Your agile and finesse weapons and unarmed attacks gain the backstabber and deadly d6 weapon traits when you're attacking your mark. If the weapon or unarmed attack already has the deadly trait, increase the size of the deadly damage die by one step instead of giving it deadly d6.",
            Target.Ranged(99)
                .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement())
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                {
                    if (a.QEffects.Any(qf => qf.Name == "Deafened"))
                        return Usability.NotUsable("Deafened");
                    if (d.HasEffect(QEffectId.Invisible) && !(a.HasEffect(QEffectId.SeeInvisibility) || a.HasEffect(QEffectId.TrueSeeing)))
                        return Usability.NotUsableOnThisCreature("Invisible");
                    return Usability.Usable;
                }))
            .WithActionCost(3)
            .WithEffectOnEachTarget(async (thisAction, markCaster, markTarget, result) =>
            {
                QEffect markEffectCaster = new QEffect()
                {
                    Name = "Marked for Death Technical Effect",
                    Source = markCaster,
                    Id = ModData.QEffectIds.MarkForDeathCaster,
                    BonusToAttackRolls = (qfThis, action, actionTarget) =>
                    {
                        // Bonus to Seek (doesn't currently only apply to Perception)
                        if (action.ActionId is ActionId.Seek && actionTarget == markTarget)
                            return new Bonus(2, BonusType.Circumstance, "Marked for death");
                        return null;
                    },
                    BonusToSkillChecks = (skill, action, actionTarget) => 
                    {
                        // Bonus to Deception to Feint
                        if (skill is Skill.Deception && action.ActionId is ActionId.Feint && actionTarget == markTarget)
                            return new Bonus(2, BonusType.Circumstance, "Marked for death");
                        return null;
                    },
                    YouDealDamageEvent = async (qfThis, @event) =>
                    {
                        if (@event.CombatAction is not { } action || !action.HasTrait(Trait.Attack))
                            return;
                        if (@event.TargetCreature is not { } actionTarget || actionTarget != markTarget)
                            return;
                        if (action.Item is not { } actionItem || !actionItem.HasTrait(Trait.Weapon) || !(actionItem.HasTrait(Trait.Agile) || actionItem.HasTrait(Trait.Finesse)))
                            return;

                        KindedDamage kd = @event.KindedDamages[0];
                        
                        // Add Backstabber
                        if (kd.DiceFormula is ComplexDiceFormula hits && actionTarget.IsFlatFootedTo(action.Owner, action) && !actionTarget.IsImmuneTo(Trait.PrecisionDamage))
                        {
                            DiceFormula? foundBackstabber = hits.List.FirstOrDefault(form =>
                                form.Source != null && form.Source.Contains("Backstabber"));

                            bool isExpert = action.Owner.HasEffect(ModData.QEffectIds.ExpertBackstabber);
                            int damageAmount = GetBackstabberDamage(action.Owner, actionItem, isExpert);
                            
                            if (foundBackstabber == null) // Added by Marked for Death
                            {
                                DiceFormula newBackstabber = DiceFormula.FromText(
                                    damageAmount.ToString(),
                                    (isExpert ? "Expert backstabber": "Backstabber")+" precision damage (marked for death)");
                                kd.DiceFormula = kd.DiceFormula!.Add(newBackstabber);
                            }
                        }
                        
                        // Add Deadly
                        if (kd.PostCriticalModifierFormula is { } crits) // Not null (has Deadly or something else)
                        {
                            // If not already complex, make it complex.
                            DiceFormula? temporaryDummy = null;
                            if (crits is not ComplexDiceFormula)
                            {
                                temporaryDummy = DiceFormula.FromText("0", "REMOVE ME");
                                kd.AddPostCriticalDamage(temporaryDummy);
                            }

                            // Cast to complex
                            ComplexDiceFormula complexCrits = (kd.PostCriticalModifierFormula as ComplexDiceFormula)!;

                            // Try to find deadly damage
                            DiceFormula? foundDeadly = complexCrits.List.FirstOrDefault(form =>
                                form.Source != null && form.Source.ToLower().Contains("deadly"));
                            
                            if (temporaryDummy != null) // Remove dummy damage formula
                                complexCrits.List.Remove(temporaryDummy);
                        
                            // Add or upgrade deadly
                            int? newDeadlySize;
                            if (foundDeadly == null) // Add d6 if not found
                                newDeadlySize = 6;
                            else if (foundDeadly.Source!.ToLower().Contains("d12")) // Do nothing if it was a d12.
                                newDeadlySize = null;
                            else if (int.TryParse(foundDeadly.Source[^1].ToString(), out int foundDeadlySize)) // Upgrade
                                newDeadlySize = foundDeadlySize + 2;
                            else // Fallback
                                newDeadlySize = null;
                        
                            if (newDeadlySize != null) // Add new deadly
                            {
                                if (foundDeadly != null) // Remove old deadly, if found
                                    complexCrits.List.Remove(foundDeadly);
                                DiceFormula newDeadly = DiceFormula.FromText(
                                    "1d" + newDeadlySize,
                                    "Deadly d" + newDeadlySize + " (marked for death)");
                                kd.AddPostCriticalDamage(newDeadly);
                            }
                        }
                        else // Doesn't have Deadly, so add d6.
                            kd.AddPostCriticalDamage(DiceFormula.FromText(
                                "1d6",
                                "Deadly d6 (marked for death)"));
                    },
                    /*YouBeginAction = async (qfThis, action) =>
                    {
                        if (!action.HasTrait(Trait.Attack)
                            || action.Item is not {} actionItem
                            || !actionItem.HasTrait(Trait.Weapon)
                            || !(actionItem.HasTrait(Trait.Agile) || actionItem.HasTrait(Trait.Finesse)))
                            return;

                        QEffect temporaryTraits = new QEffect()
                        {
                            Name = "Mark for Death Temporary Traits",
                            Tag = actionItem,
                            Source = markCaster,
                            SourceAction = action,
                            AfterYouTakeAction = async (qfThis2, action2) =>
                            {
                                if (action2 == qfThis2.SourceAction)
                                    qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                            },
                            WhenExpires = async qfThis2 =>
                            {
                                if (qfThis2.Tag is not Item tagItem) // Null check
                                    return;
                                
                                // Decrement Deadly
                                if (qfThis2.Traits.Contains(Trait.DeadlyD8)) // D8 back to
                                    tagItem.Traits.Add(Trait.DeadlyD6); // D6.
                                else if (qfThis2.Traits.Contains(Trait.DeadlyD10)) // D10 back to
                                    tagItem.Traits.Add(Trait.DeadlyD8); // D8.
                                else if (qfThis2.Traits.Contains(Trait.DeadlyD12)) // D12 back to
                                    tagItem.Traits.Add(Trait.DeadlyD10); // D10
                                // Else if (qfThis2.Contains Deadly D6): Will be removed via qf's traits.
                                // Else if (tagItem.Contains Deadly D12): Won't in the qf's traits.
                                
                                // Remove all temporary traits
                                foreach (Trait trait in qfThis2.Traits)
                                    tagItem.Traits.Remove(trait);
                            },
                        };
                        
                        // Add Backstabber
                        if (!actionItem.HasTrait(Trait.Backstabber))
                            temporaryTraits.Traits.Add(Trait.Backstabber);
                        
                        // Add Deadly
                        if (actionItem.HasTrait(Trait.DeadlyD10)) // Upgrade d10
                            temporaryTraits.Traits.Add(Trait.DeadlyD12); // to d12
                        else if (actionItem.HasTrait(Trait.DeadlyD8)) // Upgrade d8
                            temporaryTraits.Traits.Add(Trait.DeadlyD10); // to d10
                        else if (actionItem.HasTrait(Trait.DeadlyD6)) // Upgrade d6
                            temporaryTraits.Traits.Add(Trait.DeadlyD8); // to d8
                        else if (!actionItem.HasTrait(Trait.DeadlyD12)) // If no other deadlys,
                            temporaryTraits.Traits.Add(Trait.DeadlyD6); // then add d6

                        actionItem.Traits.RemoveAll(trait =>
                            trait is Trait.DeadlyD10 or Trait.DeadlyD8 or Trait.DeadlyD6);
                        foreach (Trait trait in temporaryTraits.Traits)
                            actionItem.Traits.Add(trait);

                        if (temporaryTraits.Traits.Count != 0)
                            qfThis.Owner.AddQEffect(temporaryTraits);
                    },*/
                };
                
                QEffect markEffectTarget = new QEffect(
                    "Marked for Death",
                    "You've been marked by {Blue}"+markCaster.Name+"{/Blue}, granting additional benefits against you.",
                    ExpirationCondition.Never,
                    markCaster,
                    icon)
                {
                    Id = ModData.QEffectIds.MarkForDeathTarget,
                };
                
                markCaster.Battle.AllCreatures.ForEach(cr =>
                {
                    cr.RemoveAllQEffects(qf =>
                        qf.Source == markCaster && (qf.Id == ModData.QEffectIds.MarkForDeathCaster || qf.Id == ModData.QEffectIds.MarkForDeathTarget));
                });

                markCaster.AddQEffect(markEffectCaster);
                markTarget.AddQEffect(markEffectTarget);
            });

        return markForDeath;
    }

    public static int GetBackstabberDamage(Creature owner, Item weapon, bool? isExpert = null)
    {
        int baseDamage = weapon.WeaponProperties?.ItemBonus == 3 ? 2 : 1;
        int multiplier = isExpert == null && owner.HasEffect(ModData.QEffectIds.ExpertBackstabber)
            ? 2
            : isExpert == true
                ? 2
                : 1;
        return baseDamage * multiplier;
    }

    public static int GetUsedPoisonWeaponCharges(Creature owner)
    {
        const string poisonUse = ModData.PersistentActions.PoisonWeaponCharge;
        return owner.PersistentUsedUpResources.UsedUpActions.Count(act => act == poisonUse);
    }

    public static void AddUsedPoisonWeaponCharge(Creature owner)
    {
        const string poisonUse = ModData.PersistentActions.PoisonWeaponCharge;
        owner.PersistentUsedUpResources.UsedUpActions.Add(poisonUse);
    }
}