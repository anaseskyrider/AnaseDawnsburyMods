using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Assassin
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft/*, ModData.Traits.ModName*/);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Rebuild Assassin.
        // Users have to switch the dedication, not just individual archetype feats
        Feat assDed = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.Assassin,
                "Targeted killing through stealth and subterfuge is the expertise of an assassin. While assassins are skilled in ending lives and many are evil, some live by a moral code, preying on the wicked, the cruel, or those who revel in unchecked aggression or power.",
                "You gain the {b}Mark for Death {icon:ThreeActions}{/b} activity, which you can use as a {icon:FreeAction} free action at the start of combat.")
            .WithPermanentQEffect(qfFeat =>
            {
                bool hasSneak = qfFeat.Owner.HasEffect(QEffectId.SneakAttack);
                string damageFormula = hasSneak
                    ? qfFeat.Owner.Level >= 6 ? "2" : "1"
                    : qfFeat.Owner.Level >= 6 ? "1d6" : "1d4";
                qfFeat.AddToOffenseBlock = _ =>
                    $"{{b}}Mark for Death {{icon:ThreeActions}}{{/b}} Mark a creature. You gain a +2 circumstance bonus to Seeking and Feinting them, and they take a -2 circumstance penalty to Seek you. You deal {{Blue}}{damageFormula}{{/Blue}}{(hasSneak ? " extra" : null)} sneak attack damage against them.";
                
                qfFeat.YourStrikeMayDealPrecisionDamage = (_, action, defender) =>
                {
                    if ((action.HasTrait(Trait.Agile)
                         || action.HasTrait(Trait.Finesse)
                         || action.HasTrait(Trait.Unarmed)
                         || action.HasTrait(Trait.Ranged)
                         || action.HasTrait(Trait.Simple)
                         && action.Owner.HasEffect(QEffectId.RuffianRacket))
                        && defender.HasEffect(ModData.QEffectIds.MarkedForDeathTarget)
                        & defender.IsFlatFootedTo(action.Owner, action))
                    {
                        action.UsedSneakAttack = true;
                        return DiceFormula.FromText(damageFormula, "Sneak attack (Marked for death)");
                    }
                    return null;
                };
                
                qfFeat.BonusToAttackRolls = (_, action, target) =>
                    action.ActionId is ActionId.Seek
                    && action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is null
                    && target is not null
                    && IsMyMark(action.Owner, target)
                    && action.ChosenTargets.GetAllTargetCreatures().Contains(target)
                        ? new Bonus(2, BonusType.Circumstance, "Marked for death")
                        : null;
                
                qfFeat.BonusToSkillChecks = (skill, action, target) =>
                    skill is Skill.Deception
                    && action.ActionId is ActionId.Feint
                    && IsMyMark(action.Owner, target)
                        ? new Bonus(2, BonusType.Circumstance, "Marked for death")
                        : null;
                
                qfFeat.StartOfCombatReaction = qfThis =>
                {
                    CombatAction markForDeath = CreateMarkForDeathAction(qfThis.Owner, false);
                    if (!markForDeath.CanBeginToUse(qfThis.Owner))
                        return null;
                    ReactionOption react = ReactionOption.CreateFromCombatActionCustom(
                        markForDeath,
                        "Choose a creature to mark before combat begins.",
                        async () => await qfThis.Owner.Battle.GameLoop.FullCast(CreateMarkForDeathAction(qfThis.Owner, false).WithActionCost(0)));
                    return react;
                };
                
                qfFeat.ProvideMainAction = qfThis =>
                    new ActionPossibility(CreateMarkForDeathAction(qfThis.Owner, false));
            })
            .WithRulesBlockForCombatAction(CreateMarkForDeathAction)
            .WithPrerequisite(values =>
                    values.HasFeat(FeatName.Deception),
                "You must be trained in Deception.")
            .WithPrerequisite(values =>
                    values.HasFeat(FeatName.Stealth),
                "You must be trained in Stealth.");
        ModData.FeatNames.AssassinDedication = assDed.FeatName;
        yield return assDed;

        // Expert Backstabber
        yield return new TrueFeat(
                ModData.FeatNames.ExpertBackstabber, 4,
                null,
                "The backstabber trait deals twice as much damage.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Assassin)
            .WithPermanentQEffectAndSameRulesText(qfFeat =>
            {
                qfFeat.YouDealDamageEvent = async (qfThis, dEvent) =>
                {
                    KindedDamage mainKd = dEvent.KindedDamages[0];
                    if (mainKd.DiceFormula is ComplexDiceFormula complex
                        && complex.List.FirstOrDefault(form => form.Source?.ToLower().Contains("backstabber") ?? false)
                            is { } backstabber)
                    {
                        complex.List[complex.List.IndexOf(backstabber)] = DiceFormula.FromText(
                            // ReSharper disable once SpecifyACultureInStringConversionExplicitly
                            (backstabber.ExpectedValue * 2).ToString(),
                            "Expert backstabber precision damage");
                    }
                };
            });
        
        // Poison Resistance
        Feat poisonResistance = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.PoisonResistanceDruid, ModData.Traits.Assassin, 4);
        poisonResistance.FlavorText = "Your affinity for the natural world grants you protection against some of its dangers.";
        yield return poisonResistance;

        // Surprise Attack
        Feat surpriseAttack = new TrueFeat(
                ModData.FeatNames.SurpriseAttack, 4,
                "You act before foes can react.",
                "On the first round of combat, creatures that haven't acted yet are {r:flat-footed}off-guard{/r} to you.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Assassin)
            .WithEquivalent(values =>
                values.Class?.ClassTrait is Trait.Rogue
                || values.AdditionalClassTraits.Contains(Trait.Rogue))
            .WithOnCreature(creature =>
                creature.AddQEffect(Rogue.SurpriseAttackQEffect()));
        yield return surpriseAttack;

        // Poison Weapon
        TrueFeat poisonWeapon;
        if (AllFeats.GetFeatByFeatNameOptional(ModData.FeatNames.PoisonWeapon) is {} moddedPoison)
        {
            poisonWeapon = (moddedPoison as TrueFeat)!;
            poisonWeapon.OnCreature = null;
        }
        else
        {
            poisonWeapon = new TrueFeat(
                    ModData.FeatNames.PoisonWeapon, 4,
                    null!, null!,
                    [Trait.Manipulate, Trait.Rogue]);
        }

        poisonWeapon.FlavorText = "You are adept at drawing and and applying injury poisons.";
        poisonWeapon.RulesText =
            $$"""
               Drawing an {{ModData.Tooltips.InjuryPoison("injury poison")}} from your inventory is a {icon:FreeAction} free action for you, and you can apply them as {icon:Action} an action instead of the normal number of actions.

              {b}Special{/b} Each day, you prepare a number of simple injury poisons equal to your level. These poisons automatically deal 1d4 poison damage (no saving throw), and only you can apply them.
              """;
        poisonWeapon.WithPermanentQEffect(
            "You can draw injury poisons as a free action and can apply them as one action.",
            qfFeat =>
            {
                bool hasImproved = qfFeat.Owner.HasFeat(ModData.FeatNames.ImprovedPoisonWeapon);
                string damage = (hasImproved ? 2 : 1) + "d4";
                qfFeat.Description +=
                    $" You prepare {{Blue}}{qfFeat.Owner.Level}{{/Blue}} simple injury poisons each day (they deal {(hasImproved ? "{Blue}2d4{/Blue}" : "1d4")} poison damage).";

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

                // On-demand simple injury poison button
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
                                [/*ModData.Traits.ModName*/ ModData.ModTrait, Trait.Basic, Trait.Manipulate],
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
                                                item.WeaponProperties?.DamageKind is DamageKind.Piercing
                                                    or DamageKind.Slashing))
                                            return "No piercing or slashing weapon";
                                        if (helds.All(item => item.HasTrait(Trait.Poisoned)))
                                            return "All weapons are poisoned";
                                        return null;
                                    }))
                            .WithEffectOnEachTarget(async (action, caster, _, _) =>
                            {
                                List<Item> validWeapons = caster.HeldItems
                                    .Where(item =>
                                        item.WeaponProperties?.DamageKind is DamageKind.Piercing
                                            or DamageKind.Slashing
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
                                            [
                                                ..validWeapons.Select(item =>
                                                    $"{item.Illustration.IllustrationAsIconString} {item.Name}")
                                            ]);
                                        chosenWeapon = validWeapons[chosenButton.Index];
                                        break;
                                    }
                                }

                                QEffect qfPoison = new QEffect(
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
                                };

                                // Lacks self reference, applied separately.
                                qfPoison.AfterYouDealDamage = async (attacker, action2, defender) =>
                                {
                                    if (!action2.HasTrait(Trait.Strike)
                                        || action2.Item != chosenWeapon
                                        || action2.CheckResult < CheckResult.Success)
                                        return;

                                    CombatAction poisonAction = new CombatAction(attacker.Battle.Pseudocreature,
                                        IllustrationName.DragonClaws, "simple injury poison", [Trait.Poison],
                                        "",
                                        Target.Self());

                                    await CommonSpellEffects.DealDirectDamage(
                                        poisonAction,
                                        DiceFormula.FromText(damage, "Simple injury poison"),
                                        defender,
                                        CheckResult.Failure,
                                        DamageKind.Poison);

                                    qfPoison.ExpiresAt = ExpirationCondition.Immediately;
                                };

                                if (!hasImproved)
                                {
                                    qfPoison.Description +=
                                        " This effect ends early if you critically fail the attack.";
                                    qfPoison.AfterYouTakeAction += async (qfThis2, action2) =>
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
                                caster.AddQEffect(qfPoison);
                                AddUsedPoisonWeaponCharge(caster);
                            }))
                        .WithPossibilityGroup("Use item");
                };
            });
        yield return poisonWeapon;
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.PoisonWeapon, ModData.Traits.Assassin, 6);
        
        // Improved Poison Weapon
        // No changes in the remaster, so recreate if not found,
        // and otherwise just duplicate to this archetype if found.
        if (AllFeats.GetFeatByFeatNameOptional(ModData.FeatNames.ImprovedPoisonWeapon) is null)
        {
            yield return new TrueFeat(
                    ModData.FeatNames.ImprovedPoisonWeapon, 8,
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
        }
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.ImprovedPoisonWeapon, ModData.Traits.Assassin, 10);
        
        // Assassinate
        yield return new TrueFeat(
                ModData.FeatNames.Assassinate, 12,
                "You strike with one swift movement, trying to instantly slay your mark.",
                """
                {b}Frequency{/b} Once per encounter.
                {b}Requirements{/b} You have designated a mark using Mark for Death and are undetected to your mark.

                Make a Strike against your mark. If you hit, your mark takes an additional 6d6 precision damage with a basic Fortitude save against the higher of your class DC or spell DC. If the mark critically fails, it dies unless its level is higher than yours.
                """,
                [])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Assassin)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " (Once per encounter) Strike your mark while you're undetected. On a hit, they take +6d6 precision damage, and must make a Fortitude save against instant death if they critically fail.".WithTag(qfThis.ProvideStrikeModifier == null ? "strike" : null);
                qfFeat.ProvideStrikeModifier = item =>
                {
                    CombatAction assassinate = qfFeat.Owner.CreateStrike(item, -1, new StrikeModifiers()
                    {
                        QEffectForStrike = new QEffect()
                        {
                            YourStrikeMayDealPrecisionDamage = (qfThis, action, defender) =>
                                IsMyMark(qfThis.Owner, defender)
                                    ? DiceFormula.FromText("6d6", "Assassinate")
                                    : null,
                        },
                        OnEachTarget = async (self, target, result) =>
                        {
                            if (result < CheckResult.Success
                                || target.Level > self.Level)
                                return;
                            
                            if (await CommonSpellEffects.RollSavingThrowAsync(
                                    target,
                                    CombatAction.CreateSimple(self, "Assassinate"),
                                    new SavingThrow(Defense.Fortitude, self.ClassOrSpellDC()))
                                == CheckResult.CriticalFailure)
                                target.Die();
                            
                            // Usable once
                            qfFeat.ProvideStrikeModifier = null;
                        }
                    })
                    .WithActionCost(2)
                    .WithExtraTrait(0, ModData.ModTrait);
                    assassinate.WithFullRename("Assassinate");
                    assassinate.WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                        assassinate.StrikeModifiers,
                        additionalAttackRollText: "The target must be marked by your Mark for Death.",
                        additionalSuccessText: "Deal 6d6 additional precision damage, and the target must make a Fortitude save against the higher of your class DC or spell DC. On a critical failure, it dies unless its level is higher than yours."));
                    ((CreatureTarget)assassinate.Target).WithAdditionalConditionOnTargetCreature((a, d) =>
                    {
                        if (!IsMyMark(a, d))
                            return Usability.NotUsableOnThisCreature("Not your mark");
                        if (!a.DetectionStatus.IsUndetectedTo(d))
                            return Usability.NotUsableOnThisCreature("Not undetected to your mark");
                        return Usability.Usable;
                    });
                    return assassinate;
                };
            });
    }

    public static CombatAction CreateMarkForDeathAction(Creature self) =>
        CreateMarkForDeathAction(self, true);

    public static CombatAction CreateMarkForDeathAction(Creature self, bool forDisplay)
    {
        bool hasSneak = self.HasEffect(QEffectId.SneakAttack);
        string formattedDamage = (hasSneak
            ? self.Level >= 6 ? "2" : "1"
            : self.Level >= 6 ? "1d6" : "1d4")
            .WithColor("Blue");
        CombatAction markForDeath = new CombatAction(
            self,
            ModData.Illustrations.MarkedForDeath,
            "Mark for Death",
            [/*ModData.Traits.ModName*/ ModData.ModTrait, Trait.Basic, Trait.DoesNotBreakStealth],
            $$"""
            {i}You've trained to assassinate your foes, and you do so with tenacity and precision.{/i}

            {b}Requirements{/b} You can clearly observe the creature you intend to mark.

            You designate a single creature as your mark. This lasts until the mark dies or you use Mark for Death again. You gain a +2 circumstance bonus to Perception checks to Seek your mark and on Deception checks to Feint against your mark. Your mark takes a -2 circumstance penalty to all Perception checks to Seek you.
            
            When attacking your mark, {{(forDisplay
                ? $"you deal precision damage (as if the {FeatName.SneakAttacker.ToLink("Sneak Attack")} archetype feat). If you already have the Sneak Attack class feature, then your sneak attack deals 1 additional precision damage against your mark (2 more at 6th level)"
                : hasSneak
                    ? $"your Sneak Attack feature deals {formattedDamage} additional precision damage"
                    : $"you have the Sneak Attack feature, except it deals {formattedDamage} precision damage")}}.
            """,
            Target.Ranged(99)
                .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement())
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    HiddenRules.DetermineHidden(a, d) > DetectionStrength.Observed
                    ? Usability.NotUsableOnThisCreature("Not clearly observed") : Usability.Usable))
            .WithActionCost(3)
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                QEffect qfMark = new QEffect(
                    "Marked for Death",
                    $"You've been marked by {caster.ToColoredName()}, granting additional benefits against you. You have a -2 circumstance penalty on Perception checks to Seek them.",
                    ExpirationCondition.Never,
                    caster,
                    ModData.Illustrations.MarkedForDeath)
                {
                    Id = ModData.QEffectIds.MarkedForDeathTarget,
                    SourceAction = action,
                    BonusToAttackRolls = (qfThis, action2, target2) =>
                        action2.ActionId is ActionId.Seek
                        && action2.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is null
                        && action2.ChosenTargets.GetAllTargetCreatures().Contains(qfThis.Source!)
                            ? new Bonus(-2, BonusType.Circumstance, "Marked for death")
                            : null
                };

                foreach (Creature cr in caster.Battle.AllCreatures)
                    cr.RemoveAllQEffects(qf =>
                        qf.Source == caster
                        && qf.Id == ModData.QEffectIds.MarkedForDeathTarget);

                target.AddQEffect(qfMark);
            });

        return markForDeath;
    }

    public static bool IsMyMark(Creature assassin, Creature? mark) =>
        mark is not null
        && mark.HasEffect(
            ModData.QEffectIds.MarkedForDeathTarget,
            qf => qf.Source == assassin);

    public static int GetUsedPoisonWeaponCharges(Creature owner)
    {
        const string poisonUse = ModData.PersistentActions.POISON_WEAPON_CHARGE;
        return owner.PersistentUsedUpResources.UsedUpActions.Count(act => act == poisonUse);
    }

    public static void AddUsedPoisonWeaponCharge(Creature owner)
    {
        const string poisonUse = ModData.PersistentActions.POISON_WEAPON_CHARGE;
        owner.PersistentUsedUpResources.UsedUpActions.Add(poisonUse);
    }
}