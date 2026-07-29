using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Marshal
{
    public static int BaseAuraSize = 3; // 15-foot
    
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Rebuild marshal.
        // Users have to switch the dedication, not just individual archetype feats
        Feat marshalDed = ArchetypeFeats.CreateOrUpdateDedication(
                ModData.Traits.Marshal,
                "Marshals are leaders, first and foremost. They can come from any class or background, though they all share a willingness to sacrifice their own glory for the greater good of the team.",
                """
                Choose Diplomacy or Intimidation. You become trained in that skill or become an expert if you were already trained in it.

                In addition, you're surrounded by a marshal's aura in a 15-foot emanation. Your aura has the emotion, mental, and visual traits and grants you and allies within the aura a +1 status bonus to saving throws against fear.
                """,
                null,
                dedication =>
                {
                    foreach (Prerequisite req in dedication.Prerequisites
                                 .Where(req =>
                                     req.Description.Contains("martial weapons")
                                     || req.Description.Contains("Diplomacy"))
                                 .ToList())
                        dedication.Prerequisites.Remove(req);
                })
            .WithOnSheet(values =>
            {
                List<FeatName> options =
                [
                    values.HasFeat(FeatName.Diplomacy)
                        ? FeatName.ExpertDiplomacy
                        : FeatName.Diplomacy,
                    values.HasFeat(FeatName.Intimidation)
                        ? FeatName.ExpertIntimidation
                        : FeatName.Intimidation
                ];
                values.AddSelectionOptionRightNow(new SingleFeatSelectionOption(
                    "Marshal.DedicationSkill",
                    "Marshal skill",
                    values.CurrentLevel,
                    feat => options.Contains(feat.FeatName)));
            })
            .WithPermanentQEffect(
                "You have a marshal's aura. You and allies in it have a +1 status bonus to saves against fear.",
                qfFeat =>
                {
                    qfFeat.Name = "Marshal's Aura";
                    qfFeat.Id = ModData.QEffectIds.MarshalsAuraProvider;
                    qfFeat.Tag = BaseAuraSize; // aura's range.
                    qfFeat.SpawnsAura = qfThis =>
                    {
                        float size = qfThis.Tag as int? ?? 0f;
                        return new MagicCircleAuraAnimation(IllustrationName.KineticistAuraCircle, Color.Azure, size)
                            { MaximumOpacity = 0.75f };
                    };
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            cr.FriendOf(qfFeat.Owner)
                            && !cr.IsImmuneTo(Trait.Emotion)
                            && !cr.IsImmuneTo(Trait.Mental)
                            && !cr.IsImmuneTo(Trait.Visual)
                            && cr.DistanceTo(qfFeat.Owner) <= (qfFeat.Tag as int? ?? 0),
                        qfTech =>
                        {
                            qfTech.Name = "Marshal's Aura";
                            qfTech.Description = "You have a +1 status bonus to saving throws against fear.";
                            qfTech.Illustration = IllustrationName.InspireCourage;
                            qfTech.Id = ModData.QEffectIds.MarshalsAuraEffect;
                            qfTech.BonusToDefenses = (_, action, def) =>
                                def.IsSavingThrow() && (action?.HasTrait(Trait.Fear) ?? false)
                                    ? new Bonus(1, BonusType.Status, "Marshal's aura")
                                    : null;
                        });
                })
            .WithPrerequisite(values =>
                values.GetProficiency(Trait.Martial) > Proficiency.Untrained,
                "Must be trained in martial weapons")
            .WithPrerequisite(values =>
                values.HasFeat(FeatName.Diplomacy) || values.HasFeat(FeatName.Intimidation),
                "Must be trained in Diplomacy or Intimidation");
        ModData.FeatNames.MarshalDedication = marshalDed.FeatName;
        yield return marshalDed;
        
        // Lv4: Dread Marshal Stance
        yield return new TrueFeat(
                ModData.FeatNames.DreadMarshalStance, 4,
                "Putting on a grim face for the battle ahead, you encourage your allies to strike fear into their foes with vicious attacks.",
                $"Attempt an Intimidation check. The DC is an {ModData.Tooltips.LeveledDC("easy DC of your level")}. The effect depends on the result of your check." + S.FourDegreesOfSuccess(
                    null,
                    "You and allies in your marshal's aura have a status bonus to damage rolls (equal to the number of weapon damage dice of an unarmed attack or weapon you're wielding) and their critical hits with Strikes cause enemies to become {r}frightened 1{/r}.",
                    "You fail to enter the stance.",
                    "You fail to enter the stance and can't take this action again in this encounter."),
                [Trait.Stance])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.HasEffect(ModData.QEffectIds.DreadMarshalStance))
                        return null;

                    CombatAction enterStance = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.DreadMarshalStance,
                            "Dread Marshal Stance",
                            [ModData.ModTrait, Trait.Archetype, ModData.Traits.Marshal, Trait.Stance],
                            $$"""
                              {i}Putting on a grim face for the battle ahead, you encourage your allies to strike fear into their foes with vicious attacks.{/i}

                              Attempt a {{ModData.Tooltips.LeveledDC("DC " + Checks.LevelBasedDC(qfThis.Owner.Level, SimpleDCAdjustment.Easy))}} Intimidation check.{{S.FourDegreesOfSuccess(
                                  null,
                                  "You and allies in your marshal's aura have a status bonus to damage rolls (equal to the number of weapon damage dice of an unarmed attack or weapon you're wielding) and their critical hits with Strikes cause enemies to become {r}frightened 1{/r}.",
                                  "You fail to enter the stance.",
                                  "You fail to enter the stance and can't take this action again in this encounter.")}}
                              """,
                            Target.Self())
                        .WithActionCost(1)
                        .WithShortDescription($"vs DC {Checks.LevelBasedDC(qfThis.Owner.Level, SimpleDCAdjustment.Easy)}; Enter a stance where your marshal aura grants bonus damage and frightens enemies on critical Strikes.")
                        .WithActionId(ModData.ActionIds.DreadMarshalStance)
                        .WithActiveRollSpecification(
                            new ActiveRollSpecification(
                                TaggedChecks.SkillCheck(Skill.Intimidation),
                                new TaggedCalculatedNumberProducer((_, _, _) =>
                                    new CalculatedNumber(Checks.LevelBasedDC(qfThis.Owner.Level), "Level-based DC",
                                        [ new Bonus(-2, BonusType.Untyped, "Easy adjustment") ]))))
                        .WithEffectOnEachTarget(async (enterStance, caster, _, result) =>
                        {
                            if (result < CheckResult.Success)
                            {
                                // Do nothing on a failure
                                if (result == CheckResult.Failure)
                                    return;
                                
                                // Can't use again if critical failure
                                qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                caster.Battle.Log(caster.Name + " can no longer use Dread Marshal Stance for this encounter.");
                                return;
                            }

                            QEffect dmStance = KineticistCommonEffects.EnterStance(
                                    qfThis.Owner,
                                    ModData.Illustrations.DreadMarshalStance,
                                    "Dread Marshal Stance",
                                    "You and all allies in your marshal's aura have a status bonus to damage rolls equal to the number of weapon dice of your best unarmed attack or of a weapon you're wielding. On a critical hit with a Strike, the target is frightened 1.",
                                    ModData.QEffectIds.DreadMarshalStance)
                                .AddGrantingOfTechnical(
                                    cr => cr.HasEffect(ModData.QEffectIds.MarshalsAuraEffect),
                                    qfTech =>
                                    {
                                        qfTech.Name = "Dread Marshal Aura";
                                        qfTech.Description =
                                            $"You have a status bonus to damage rolls equal to the number of weapon dice of {qfThis.Owner}'s best unarmed attack or of a weapon they're wielding. On a critical hit with a Strike, the target is frightened 1.";
                                        qfTech.Illustration = ModData.Illustrations.DreadMarshalStance;
                                        // Can apply if the damage doesn't include a roll, but this
                                        // is in-line with similar effects like DD's Inspire Courage.
                                        qfTech.BonusToDamage = (_,_,_) =>
                                            new Bonus(
                                                caster.Weapons.Max(item =>
                                                    item.WeaponProperties?.DamageDieCount ?? 1),
                                                BonusType.Status,
                                                "Dread marshal aura");
                                        qfTech.AfterYouTakeActionAgainstTarget =
                                            async (_, action, target2, result2) =>
                                            {
                                                if (action.HasTrait(Trait.Strike) &&
                                                    result2 == CheckResult.CriticalSuccess)
                                                    target2.AddQEffect(QEffect.Frightened(1)
                                                        .WithSourceAction(action));
                                            };
                                        qfTech.StateCheckLayer = 1;
                                    });
                            dmStance.HideFromPortrait = true;
                            // Link the stance to the action
                            enterStance.Tag = dmStance;
                            dmStance.SourceAction = enterStance;
                        });
                    
                    return new ActionPossibility(enterStance)
                        .WithPossibilityGroup(ModData.PossibilityGroups.MARSHAL);
                };
            })
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Intimidation),
                "Must be trained in Intimidation");
        
        // Lv4: Inspiring Marshal Stance
        yield return new TrueFeat(
                ModData.FeatNames.InspiringMarshalStance, 4,
                "You become a brilliant example of dedication and poise in battle, encouraging your allies to follow suit.",
                $"Attempt a Diplomacy check. The DC is an {ModData.Tooltips.LeveledDC("easy DC of your level")}. The effect depends on the result of your check." + S.FourDegreesOfSuccess(
                    null,
                    "You and allies in your marshal's aura have a +1 status bonus to attack rolls and saves against mental effects.",
                    "You fail to enter the stance.",
                    "You fail to enter the stance and can't take this action again in this encounter."),
                [Trait.Stance])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.HasEffect(ModData.QEffectIds.InspiringMarshalStance))
                        return null;

                    CombatAction enterStance = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.InspiringMarshalStance,
                            "Inspiring Marshal Stance",
                            [ModData.ModTrait, Trait.Archetype, ModData.Traits.Marshal, Trait.Stance],
                            $$"""
                              {i}You become a brilliant example of dedication and poise in battle, encouraging your allies to follow suit.{/i}

                              Attempt a {{ModData.Tooltips.LeveledDC("DC " + Checks.LevelBasedDC(qfThis.Owner.Level, SimpleDCAdjustment.Easy))}} Diplomacy check.{{S.FourDegreesOfSuccess(
                                  null,
                                  "You and allies in your marshal's aura have a +1 status bonus to attack rolls and saves against mental effects.",
                                  "You fail to enter the stance.",
                                  "You fail to enter the stance and can't take this action again in this encounter.")}}
                              """,
                            Target.Self())
                        .WithActionCost(1)
                        .WithShortDescription($"vs DC {Checks.LevelBasedDC(qfThis.Owner.Level, SimpleDCAdjustment.Easy)}; Enter a stance where your marshal aura grants a +1 status bonus to attack rolls and saves against mental effects.")
                        .WithActionId(ModData.ActionIds.InspiringMarshalStance)
                        .WithActiveRollSpecification(
                            new ActiveRollSpecification(
                                TaggedChecks.SkillCheck(Skill.Diplomacy),
                                new TaggedCalculatedNumberProducer((_, _, _) =>
                                    new CalculatedNumber(Checks.LevelBasedDC(qfThis.Owner.Level), "Level-based DC",
                                        [ new Bonus(-2, BonusType.Untyped, "Easy adjustment") ]))))
                        .WithEffectOnEachTarget(async (enterStance, caster, _, result) =>
                        {
                            if (result < CheckResult.Success)
                            {
                                // Do nothing on a failure
                                if (result == CheckResult.Failure)
                                    return;
                                
                                // Can't use again if critical failure
                                qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                caster.Battle.Log(caster.Name + " can no longer use Inspiring Marshal Stance for this encounter.");
                                return;
                            }

                            // Normal effects
                            QEffect imStance = KineticistCommonEffects.EnterStance(
                                    qfThis.Owner,
                                    ModData.Illustrations.InspiringMarshalStance,
                                    "Inspiring Marshal Stance",
                                    "You and all allies in your marshal's aura gain a +1 status bonus to attack rolls and saves against mental effects.",
                                    ModData.QEffectIds.InspiringMarshalStance)
                                .AddGrantingOfTechnical(
                                    cr => cr.HasEffect(ModData.QEffectIds.MarshalsAuraEffect),
                                    qfTech =>
                                    {
                                        qfTech.Name = "Inspiring Marshal Aura";
                                        qfTech.Description =
                                            "You have a +1 status bonus to attack rolls and saves against mental effects.";
                                        qfTech.Illustration = ModData.Illustrations.InspiringMarshalStance;
                                        qfTech.BonusToAttackRolls = (_, action, _) =>
                                            action.HasTrait(Trait.Attack)
                                                ? new Bonus(1, BonusType.Status, "inspiring marshal aura")
                                                : null;
                                        qfTech.BonusToDefenses = (_, action, def) =>
                                            def.IsSavingThrow()
                                            && action != null
                                            && action.HasTrait(Trait.Mental)
                                                ? new Bonus(1, BonusType.Status, "inspiring marshal aura")
                                                : null;
                                        qfTech.StateCheckLayer = 1;
                                    });
                            imStance.HideFromPortrait = true;
                            // Link the stance to the action
                            enterStance.Tag = imStance;
                            imStance.SourceAction = enterStance;
                        });
                    return new ActionPossibility(enterStance)
                        .WithPossibilityGroup(ModData.PossibilityGroups.MARSHAL);
                };
            })
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Diplomacy),
                "Must be trained in Diplomacy");
        
        // TODO: Snap Out of It! (Marshal) @lv4
        // Can this even be implemented?
        
        // Lv4: Steel Yourself!
        // Difference from tabletop:
        // - No expiration on temp HP.
        // - Doubles at level 12.
        // PETR: Temp HP rework?
        yield return new TrueFeat(
                ModData.FeatNames.SteelYourself, 4,
                "You encourage an ally to toughen up, giving them a fighting chance.",
                "Choose one ally within your marshal's aura. The ally gains temporary Hit Points equal to your Charisma modifier, as well as a +2 circumstance bonus to Fortitude saves until the start of your next turn. If you're at least 12th level, double the amount of temporary Hit Points gained.",
                [Trait.Auditory, Trait.Emotion, Trait.Mental, Trait.Rebalanced])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(qfFeat =>
            {
                int tempHealing = qfFeat.Owner.Abilities.Charisma * (qfFeat.Owner.Level >= 12 ? 2 : 1);
                string tHPformatted = ("+" + tempHealing).WithColor("Blue");
                
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + $" Grant {tHPformatted} temp HP and a +2 circumstance bonus to Fortitude saves to an ally in your marshal's aura.";
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction steelAction = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.SteelYourself,
                            "Steel Yourself",
                            [ModData.ModTrait, Trait.Auditory, Trait.Emotion, Trait.Mental, Trait.Basic],
                            $$"""
                              {i}You encourage an ally to toughen up, giving them a fighting chance.{/i}

                              Choose one ally in your marshal's aura. The ally gains {{tHPformatted}} temporary Hit Points, as well as a +2 circumstance bonus to Fortitude saves until the start of your next turn.
                              """,
                            Target.RangedFriend(99)
                                .WithAdditionalConditionOnTargetCreature(IsInMarshalAura()))
                        .WithActionCost(1)
                        .WithSoundEffect(qfThis.Owner.HasTrait(Trait.Female) ? SfxName.TripFemale : SfxName.TripMale)
                        .WithEffectOnEachTarget(async (steel, caster, target, _) =>
                        {
                            target.GainTemporaryHP(tempHealing);
                            target.AddQEffect(
                                new QEffect(
                                    "Steel Yourself",
                                    "You have a +2 circumstance bonus to Fortitude saving throws.",
                                    ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                    caster,
                                    ModData.Illustrations.SteelYourself)
                                {
                                    BonusToDefenses = (_, _, def) =>
                                        def is Defense.Fortitude
                                            ? new Bonus(2, BonusType.Circumstance, "Steel yourself")
                                            : null,
                                    DoNotShowUpOverhead = true,
                                    SourceAction = steel,
                                });
                        });
                    return new ActionPossibility(steelAction)
                        .WithPossibilityGroup(ModData.PossibilityGroups.MARSHAL);
                };
            });
        
        // Lv4: Strategist Stance
        // TODO: Lores and Weaknesses compatibility
        // Tian Xia Character Guide
        /*yield return new TrueFeat(
                ModData.FeatNames.StrategistStance, 4,
                "",
                "",
                [Trait.Stance, Trait.Rebalanced])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.HasEffect(ModData.QEffectIds.StrategistStance))
                            return null;

                        CombatAction enterStance = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.StrategistStance,
                                "Strategist Stance",
                                [ModData.ModTrait, Trait.Archetype, ModData.Traits.Marshal, Trait.Stance, Trait.Rebalanced],
                                $$"""
                                  {i}...{/i}

                                  Attempt a {{ModData.Tooltips.LeveledDC("DC " + Checks.LevelBasedDC(qfThis.Owner.Level, SimpleDCAdjustment.Easy))}} Society or Warfare Lore check.{{S.FourDegreesOfSuccess(
                                      null,
                                      "You and allies in your marshal's aura ...",
                                      "You fail to enter the stance.",
                                      "You fail to enter the stance and can't take this action again in this encounter.")}}
                                  """,
                                Target.Self())
                            .WithActionCost(1)
                            .WithShortDescription($"vs DC {Checks.LevelBasedDC(qfThis.Owner.Level, SimpleDCAdjustment.Easy)}; Enter a stance where ...")
                            .WithActiveRollSpecification(
                                new ActiveRollSpecification(
                                    TaggedChecks.SkillCheck(Skill.Intimidation),
                                    new TaggedCalculatedNumberProducer((_, _, _) =>
                                        new CalculatedNumber(Checks.LevelBasedDC(qfThis.Owner.Level), "Level-based DC",
                                            [ new Bonus(-2, BonusType.Untyped, "Easy adjustment") ]))))
                            .WithEffectOnEachTarget(async (_, caster, _, result) =>
                            {
                                if (result < CheckResult.Success)
                                {
                                    // Do nothing on a failure
                                    if (result == CheckResult.Failure)
                                        return;
                                    
                                    // Can't use again if critical failure
                                    qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                    caster.Battle.Log(caster.Name + " can no longer use Strategist Stance for this encounter.");
                                    return;
                                }

                                
                                // Link the stance to the action
                                action.Tag = dmStance;
                                dmStance.SourceAction = action;
                            });
                        return new ActionPossibility(enterStance)
                            .WithPossibilityGroup("Enter a stance");
                    };
                })
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Society) || values.HasFeat(FeatName.WarfareLore),
                "Must be trained in Diplomacy or Warfare Lore");*/
        
        // Lv6: Booming Presence
        yield return new TrueFeat(
                ModData.FeatNames.BoomingPresence, 6,
                "You command a large presence on the field of battle.",
                "When you critically succeed on a check to enter a marshal stance, the radius of your marshal's aura expands to 30 feet. This lasts until the stance ends.",
                [Trait.Homebrew])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(
                "When you critically succeed to enter a marshal stance, your marshal's aura expands to 30 feet.",
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (!action.HasTrait(ModData.Traits.Marshal)
                            || !action.HasTrait(Trait.Stance)
                            || action.CheckResult < CheckResult.CriticalSuccess
                            || GetMarshalAura(qfThis.Owner) is not { } aura
                            || action.Tag is not QEffect stance
                            || stance.SourceAction != action)
                            return;
                        
                        // Expand aura
                        aura.Tag = 5; // 30-foot
                        aura.AssociatedAura?.MoveTo(aura.Tag as int? ?? 0f);
                        
                        // Reduce when stance ends
                        stance.WhenExpires += _ =>
                        {
                            aura.Tag = BaseAuraSize;
                            aura.AssociatedAura?.MoveTo(aura.Tag as int? ?? 0f);
                        };
                    };
                });
        
        // Lv6: Cadence Call
        yield return new TrueFeat(
                ModData.FeatNames.CadenceCall, 6,
                "You call out a quick cadence, guiding your allies into a more efficient rhythm.",
                $$"""
                {b}Frequency{/b} Once per encounter.

                Each willing ally in your marshal’s aura is quickened until the end of their next turn, and they can use the extra action only to Stride. If an ally uses this extra action, at the end of its turn that ally becomes {r}slowed 1{/r} until the end of its following turn.
                
                {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {i}Ending your turn with 1+ actions remaining, or not consuming your quickened action, will not slow you.{/i}
                """,
                [Trait.Auditory, Trait.Flourish])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "(Once per encounter) Choose allies in your aura to become quickened for 1 round (only to move). Using this extra action makes an ally slowed 1 next round.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        Illustration icon = IllustrationName.QuickenTime;
                        
                        CombatAction call = new CombatAction(
                                qfThis.Owner,
                                icon,
                                "Cadence Call",
                                [ModData.ModTrait, Trait.Archetype, Trait.Auditory, Trait.Flourish, Trait.Basic],
                                $$"""
                                  {i}You call out a quick cadence, guiding your allies into a more efficient rhythm.{/i}
                                  
                                  {b}Frequency{/b} Once per encounter.

                                  Each willing ally in your marshal’s aura is quickened until the end of their next turn, and they can use the extra action only to take a move action. If an ally uses this extra action, at the end of its turn that ally becomes {r}slowed 1{/r} until the end of its following turn.

                                  {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {i}Ending your turn with 1+ actions remaining, or not consuming your quickened action, will not slow you.{/i}
                                  """,
                                ((EmanationTarget)Target.AlliesOnlyEmanation(GetMarshalAuraRange(qfThis.Owner)))
                                .WithIncludeOnlyIf((_,cr) => cr.FriendOfAndNotSelf(qfThis.Owner)))
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.DeathsCall)
                            .WithProjectileCone(icon, 15, ProjectileKind.Ray)
                            .WithEffectOnEachTarget(async (call, _, target, _) =>
                            {
                                bool hasTacticalCadence = call.Owner.HasFeat(ModData.FeatNames.TacticalCadence);
                                    
                                QEffect quickened = QEffect.Quickened(ca => ca.HasTrait(Trait.Move))
                                    .WithExpirationAtEndOfOwnerTurn();
                                quickened.Name = "Quickened (Cadence Call)";
                                quickened.Description = "You have an extra action each turn. It can only be used to take ";
                                if (!hasTacticalCadence)
                                {
                                    quickened.Description += "move actions. Using this extra action causes you to become slowed 1 until the end of your next turn.";
                                    quickened.EndOfYourTurnDetrimentalEffect = async (qfQuick, self) =>
                                    {
                                        if (!self.Actions.UsedQuickenedAction
                                            || self.Actions.ActionsLeft > 0)
                                            return;

                                        QEffect slowed = QEffect.Slowed(1)
                                            .WithExpirationAtEndOfOwnersNextTurn();
                                        qfQuick.Owner.AddQEffect(slowed);
                                    };
                                }
                                else
                                    quickened.Description += "Strike and move actions.";
                                quickened.SourceAction = call;
                                
                                target.AddQEffect(quickened);
                                
                                // Usable once
                                qfThis.ProvideMainAction = null;
                                qfThis.Description = qfThis.Description!.WithTag("{strike}");
                            });

                        return new ActionPossibility(call)
                            .WithPossibilityGroup(ModData.PossibilityGroups.MARSHAL);
                    };
                })
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal);
        
        // Lv6: Rallying Charge
        // Difference from tabletop:
        // - No expiration on temp HP.
        // - Doubles on a crit.
        // - Doubles at level 12.
        yield return new TrueFeat(
                ModData.FeatNames.RallyingCharge, 6,
                "Your fearless charge into battle reinvigorates your allies to carry on the fight.",
                "You Stride up to your Speed and make a melee Strike. If your Strike hits and damages an enemy, each ally within 60 feet who saw you hit gains temporary Hit Points equal to your Charisma modifier (double your modifier on a critical hit). If you're at least 12th level, double this amount (quadruple on a critical hit).",
                [Trait.Visual, Trait.Rebalanced])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    int tempHealing = qfThis.Owner.Abilities.Charisma * (qfThis.Owner.Level >= 12 ? 2 : 1);
                    string tHPformatted = ("+" + tempHealing).WithColor("Blue");
                    
                    CombatAction charge = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.RallyingCharge,
                            "Rallying Charge",
                            [ModData.ModTrait, Trait.Visual],
                            $$"""
                              {/i}Your fearless charge into battle reinvigorates your allies to carry on the fight.{/i}

                              You Stride up to your Speed and make a melee Strike. If your Strike hits and damages an enemy, each ally within 60 feet who saw you hit gains {{tHPformatted}} temporary Hit Points (double on a critical hit).
                              """,
                            Target.Self())
                        .WithShortDescription($"Stride and then make a melee Strike, granting {tHPformatted} temp HP to allies within 60 feet.")
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            if (!await self.StrideAsync("Choose where to Stride with Rallying Charge. You should end your movement within melee reach of an enemy. (1/2)", allowCancel: true))
                            {
                                action.RevertRequested = true;
                                return;
                            }
                                
                            if (!await CommonCombatActions.StrikeCreature(
                                    self,
                                    strike => strike.HasTrait(Trait.Melee),
                                    strike => strike.WithHitAndDealDamage(async (a, strike_2, _) =>
                                    {
                                        // Double on a crit
                                        if (strike_2.CheckResult == CheckResult.CriticalSuccess)
                                            tempHealing *= 2;
                                        a.Battle.AllCreatures
                                            .Where(cr =>
                                                cr.FriendOfAndNotSelf(a) && cr.HasLineOfEffectTo(a) < CoverKind.Blocked && !cr.IsImmuneTo(Trait.Visual) && cr.DistanceTo(a) <= 12)
                                            .ForEach(cr =>
                                            {
                                                cr.GainTemporaryHP(tempHealing);
                                                cr.Overhead(tempHealing.WithPlus(), Color.Aquamarine);
                                            });
                                    }),
                                    null,
                                    action.Illustration,
                                    "Choose an enemy to Strike with Rallying Charge. (2/2)",
                                    false,
                                    "Convert to simple Stride"))
                            {
                                self.Battle.Log("Rallying Charge was converted to a simple Stride.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                            }
                        });
                    return new ActionPossibility(charge)
                        .WithPossibilityGroup(ModData.PossibilityGroups.MARSHAL);
                };
            });

        // Lv8: Back to Back
        // Special thanks to SilchasRuin
        yield return new TrueFeat(
                ModData.FeatNames.BackToBack,
                8,
                "You excel at watching your allies' backs and helping them watch yours.",
                """
                You gain the following benefits: You cannot be {r:flat-footed}off-guard{/r} due to flanking while none of your adjacent allies are flanked.

                Your adjacent allies gain the following benefits: You cannot be {r:flat-footed}off-guard{/r} due to flanking while the marshal with this feat isn't flanked.
                """,
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(
                "You and your adjacent allies can't be flanked unless both you and another adjacent ally are flanked.",
                qfFeat =>
                {
                    Creature marshal = qfFeat.Owner;
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            (cr.IsAdjacentTo(marshal) && cr.FriendOf(marshal)) || cr == marshal,
                        qfTech =>
                        {
                            qfTech.StateCheck = qfTech2 =>
                            {
                                List<Creature> adjacentFriends = marshal.Battle.AllCreatures
                                    .Where(cr =>
                                        cr.FriendOf(marshal) && cr.IsAdjacentTo(marshal))
                                    .ToList();
                                
                                if (adjacentFriends.Count == 0)
                                    return;
                                
                                // If marshal is flanked and any ally is flanked,
                                if (IsFlankedByAnyEnemy(marshal)
                                    && adjacentFriends.Any(IsFlankedByAnyEnemy))
                                    return; // no benefits.
                                
                                qfTech2.Owner.AddQEffect(new QEffect(
                                    "Back to Back",
                                    $"You cannot be off-guard due to flanking while adjacent to {(qfTech.Owner == marshal ? "{Blue}your allies{/Blue}" : marshal.ToColoredName())}, unless they are also flanked.",
                                    ExpirationCondition.Ephemeral,
                                    marshal,
                                    IllustrationName.MirrorImage)
                                {
                                    Id = QEffectId.AllAroundVision
                                });
                            };
                        });
                    
                    return;

                    bool IsFlankedByAnyEnemy(Creature cr)
                    {
                        //return cr.HasEffect(QEffectId.FlankedBy); // Source is always an enemy
                        return cr.Cache.FlankedBy.Count > 0;
                    }
                });
        
        // Reactive Strike
        #pragma warning disable CS0618 // Type or member is obsolete
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
                FeatName.AttackOfOpportunity, ModData.Traits.Marshal, 8)
            .WithEquivalent(values => values.HasFeat(FeatName.Fighter))
            .WithCustomName("Reactive Strike");
        #pragma warning restore CS0618 // Type or member is obsolete
        
        // TODO: Lv.8 Know your Enemy
        // Requires: Strategist Stance, Lores and Weaknesses
        // Tian Xia Character Guide

        // To Battle!
        yield return new TrueFeat(
                ModData.FeatNames.ToBattle, 8,
                "With a resounding cry, you rally your ally to take to the offensive.",
                $$"""
                  Spend {{RulesBlock.GetIconTextFromNumberOfActions(-3)}} 1-2 actions and choose one ally within your marshal's aura who has a reaction available.

                  {icon:Action} That ally can Stride as a {icon:Reaction} reaction.
                  {icon:TwoActions} That can ally Strike as a {icon:Reaction} reaction.
                  """,
                [Trait.Auditory, Trait.Flourish])
            .WithActionCost(-3)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(
                "Choose an ally, who will use their reaction to Stride or Strike.", 
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction toBattleAction = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.ToBattle,
                                "To Battle!",
                                [ModData.ModTrait, Trait.Auditory, Trait.Flourish, Trait.Basic],
                                $$"""
                                  {i}With a resounding cry, you rally your ally to the offensive.{/i}

                                  Spend {{RulesBlock.GetIconTextFromNumberOfActions(-3)}} 1-2 actions and choose one ally within your marshal's aura who has a reaction available.

                                  {icon:Action} That ally can Stride as a {icon:Reaction} reaction.
                                  {icon:TwoActions} That ally can Strike as a {icon:Reaction} reaction.
                                  """,
                                Target.DependsOnActionsSpent(
                                    BasicTargeting()
                                        .WithAdditionalConditionOnTargetCreature((_,d) =>
                                            d.HasEffect(QEffectId.Immobilized)
                                                ? Usability.NotUsableOnThisCreature("immobilized")
                                                : Usability.Usable),
                                    BasicTargeting()
                                        .WithAdditionalConditionOnTargetCreature((_,d) =>
                                        {
                                            if (d.HasEffect(QEffectId.Restrained))
                                                return Usability.NotUsableOnThisCreature("restrained");
                                            if (d.HasEffect(QEffectId.CalmEmotions))
                                                return Usability.NotUsableOnThisCreature("calm");
                                            return Usability.Usable;
                                        }),
                                    null!))
                            .WithActionCost(-3)
                            .WithSoundEffect(qfThis.Owner.HasTrait(Trait.Female)
                                ? SfxName.Intimidate : SfxName.MaleIntimidate)
                            .WithCreateVariantDescription((actionCost, _) =>
                            {
                                if (actionCost == 1)
                                    return "Choose one ally within your marshal's aura who can {Blue}Stride{/Blue} as a {icon:Reaction} reaction.";
                                if (actionCost == 2)
                                    return "Choose one ally within your marshal's aura who can {Blue}Strike{/Blue} as a {icon:Reaction} reaction.";
                                throw new ArgumentException("Unknown action cost.");
                            })
                            .WithEffectOnEachTarget(async (action, _, target, _) =>
                            {
                                if (!await target.AskToUseReaction($"{{b}}To Battle!{{b}}\nUse your reaction to {(action.SpentActions == 1 ? "Stride" : "Strike")}?", action))
                                {
                                    action.RevertRequested = true;
                                    return;
                                }
                                if ((action.SpentActions == 1
                                     && !await target.StrideAsync("Choose where to Stride.", allowCancel: true))
                                    || (action.SpentActions == 2
                                        && !await CommonCombatActions.StrikeAnyCreature(target, null,
                                            allowCancel: true)))
                                {
                                    target.Actions.RefundReaction();
                                    action.RevertRequested = true;
                                }
                            });
                        Possibility battlePossibility = Possibilities.CreateSpellPossibility(toBattleAction);
                        battlePossibility.PossibilitySize = PossibilitySize.Full;
                        battlePossibility.PossibilityGroup = ModData.PossibilityGroups.MARSHAL;
                        return battlePossibility;

                        CreatureTarget BasicTargeting()
                        {
                            return Target.RangedFriend(GetMarshalAuraRange(qfThis.Owner))
                                .WithAdditionalConditionOnTargetCreature(IsInMarshalAura())
                                .WithAdditionalConditionOnTargetCreature((a, d) =>
                                {
                                    if (a == d)
                                        return Usability.NotUsableOnThisCreature("not ally");
                                    if (!d.Actions.CanTakeReaction())
                                        return Usability.NotUsableOnThisCreature("no reaction");
                                    if (!d.Actions.CanTakeActions())
                                        return Usability.NotUsableOnThisCreature("can't take actions");
                                    return Usability.Usable;
                                });
                        }
                    };
                });
        
        // TODO: Lv10. Form Up!
        // Tian Xia Character Guide
        
        // Lv10. Topple Foe
        yield return new TrueFeat(
                ModData.FeatNames.ToppleFoe, 10,
                "You take advantage of the opening created by your ally to tip your foe off their feet.",
                """
                {b}Trigger{/b} An ally succeeds at a melee Strike against an enemy you are both adjacent to you.
                
                Attempt an Athletics check to Trip the target of the triggering Strike.
                """,
                [])
            .WithActionCost(-2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(
                "Attempt to Trip an adjacent enemy that an ally adjacent to them hit with a melee Strike.",
                qfFeat =>
                {
                    qfFeat.AddGrantingOfTechnical(
                        cr => cr.FriendOfAndNotSelf(qfFeat.Owner),
                        qfTech =>
                        {
                            qfTech.AfterYouTakeActionReaction = (qfTech2, action) =>
                            {
                                if (!(action.HasTrait(Trait.Strike) && action.HasTrait(Trait.Melee))
                                    || !action.ChosenTargets.ChosenCreature!.IsAdjacentTo(action.Owner)
                                    || !action.ChosenTargets.ChosenCreature!.IsAdjacentTo(qfFeat.Owner)
                                    || action.CheckResult < CheckResult.Success)
                                    return null;
                                
                                CombatAction tripCheck = CombatManeuverPossibilities.ChooseBestManeuver(CombatManeuverPossibilities.CreateTripPossibility(qfFeat.Owner))
                                    .WithActionCost(0);

                                if (!tripCheck.CanBeginToUse(qfFeat.Owner))
                                    return null;

                                CombatAction topple = new CombatAction(
                                        qfFeat.Owner,
                                        IllustrationName.Trip,
                                        "Topple Foe",
                                        // Trait.ProxyAttack is mostly only used by the game to affect movement animations. I'm using it here to stop you from moving twice for the same action while retaining targeting functionality.
                                        [ModData.ModTrait, Trait.Archetype, Trait.AlwaysHits, Trait.ProxyAttack],
                                        null!,
                                        Target.AdjacentCreature())
                                    .WithActionCost(-2)
                                    .WithDescription(
                                        "You take advantage of the opening created by your ally to tip your foe off their feet.",
                                        """
                                        {b}Trigger{/b} An ally succeeds at a melee Strike against an enemy you are both adjacent to you.

                                        Attempt an Athletics check to Trip the target of the triggering Strike.
                                        """)
                                    .WithEffectOnEachTarget(async (topple, caster, target, _) =>
                                    {
                                        List<Option> options = [ new CancelOption(true) ];
                                        
                                        // Add all Trips
                                        foreach (CombatAction trip in CombatManeuverPossibilities.GetAllOptions(CombatManeuverPossibilities.CreateTripPossibility(qfFeat.Owner)))
                                        {
                                            trip.WithActionCost(0);
                                            GameLoop.AddDirectUsageOnCreatureOptions(trip, options);
                                        }

                                        // Only on the target
                                        options.RemoveAll(opt =>
                                            opt is CreatureOption crOpt
                                            && crOpt.Creature != target);
                                        
                                        if (options.Count == 1)
                                        {
                                            topple.RevertRequested = true;
                                            caster.Actions.RefundReaction(topple.Traits.ToArray());
                                            return;
                                        }
                                        
                                        Option chosenOption = (await caster.Battle.SendRequest(new AdvancedRequest(caster, "Trip as part of Topple Foe or right-click to cancel.", options)
                                        {
                                            TopBarText ="Trip as part of Topple Foe or right-click to cancel.",
                                            TopBarIcon = topple.Illustration
                                        })).ChosenOption;
                                        
                                        
                                        if (chosenOption is CancelOption)
                                        {
                                            topple.RevertRequested = true;
                                            caster.Actions.RefundReaction(topple.Traits.ToArray());
                                            return;
                                        }

                                        await chosenOption.Action();
                                    });

                                Creature defender = action.ChosenTargets.ChosenCreature!;
                                ReactionOption reactOpt = ReactionOption.WrapFullcastWithChosenTargets(
                                    topple,
                                    ChosenTargets.CreateSingleTarget(defender),
                                    $"Attempt to {{b}}Trip{{/b}} {defender.ToColoredName()}.")
                                    .WithTriggerReason(action.Owner.ToColoredBoldedName() + " hit with a melee Strike against an enemy you're both adjacent to.");

                                return reactOpt;
                            };
                        });
                })
            .WithPrerequisite(
                values => values.HasFeat(FeatName.Athletics),
                "You must be trained in Athletics.");
        
        // Lv12. Coordinated Charge
        yield return new TrueFeat(
                ModData.FeatNames.CoordinatedCharge, 12,
                "You heroically dash into the fray, inspiring your allies to follow.",
                "Stride and then make a melee Strike. If your Strike hits and deals damages, each ally within 60 feet who saw you hit can use a {icon:Reaction} reaction to Stride, ending closer to the struck creature.",
                [Trait.Flourish, Trait.Visual])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction coord = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                IllustrationName.FleetStep,
                                IllustrationName.Reaction),
                            "Coordinated Charge",
                            [ModData.ModTrait, Trait.Archetype, Trait.Flourish, Trait.Visual],
                            null!,
                            Target.Self())
                        .WithActionCost(2)
                        .WithDescription(
                            "You heroically dash into the fray, inspiring your allies to follow.",
                            "Stride and then make a melee Strike. If your Strike hits and deals damages, each ally within 60 feet who saw you hit can use a {icon:Reaction} reaction to Stride, ending closer to the struck creature.")
                        .WithShortDescription("Stride, then make a melee Strike. On a damaging hit, allies within 60 feet can Stride towards the target as a reaction.")
                        .WithEffectOnSelf(async (coord, self) =>
                        {
                            if (!await self.StrideAsync("Choose where to Stride with Coordinated Charge. You should end your movement within melee reach of an enemy. (1/2)", allowCancel: true))
                                coord.RevertRequested = true;
                            else if (!await CommonCombatActions.StrikeCreature(
                                 self,
                                 strike => strike.HasTrait(Trait.Melee),
                                 strike => strike.WithHitAndDealDamage(async (attacker,_, defender) =>
                                 {
                                     await attacker.FictitiousSingleTileMoveBack();
                                     await CommonQuestions.OfferReactionsAsync(
                                         new ReactionRequestStyle(
                                             attacker.Battle,
                                             () => attacker.ToColoredName() + " used {b}Coordinated Charge {icon:TwoActions}{/b} which allows allies to Stride towards " + defender.ToColoredName() + " as a reaction.",
                                             false,
                                             attacker,
                                             () => attacker.Battle.AllCreatures
                                                 .Where(cr =>
                                                     cr.FriendOfAndNotSelf(attacker)
                                                     && cr.HasLineOfEffectTo(attacker) < CoverKind.Blocked
                                                     && cr.DistanceTo(attacker) <= 12
                                                     && !cr.IsImmuneTo(Trait.Visual))
                                                 .Select(friend => ReactionOption.CreateCustom(
                                                     "Coordinated Charge",
                                                     "Stride, ending closer to " + defender.ToColoredName() + ".",
                                                     coord.Illustration,
                                                     friend,
                                                     async () =>
                                                     {
                                                         if (!await friend.StrideOrStepAdvancedAsync(
                                                                 "Choose where to Stride.",
                                                                 allowCancel: true,
                                                                 allowPass: true,
                                                                 permissibleTarget: tile => tile.DistanceTo(defender) < friend.DistanceTo(defender)))
                                                             friend.Actions.RefundReaction();
                                                     })
                                                     .WithIsReaction())
                                                 .ToList()));
                                 }),
                                 null,
                                 coord.Illustration,
                                 "Choose a creature to Strike with Coordinated Charge. (2/2)",
                                 false,
                                 "Convert to simple Stride"))
                            {
                                self.Battle.Log("Coordinated Charge was converted to a simple Stride.");
                                coord.SpentActions = 1;
                                coord.RevertRequested = true;
                            }
                        });

                    return new ActionPossibility(coord)
                        .WithPossibilityGroup(ModData.PossibilityGroups.MARSHAL);
                };
            });
        
        // Lv12. General's Gambit
        // Requires Five-breath Vanguard Dedication
        // Tian Xia Character Guide
        
        // Lv14. Tactical Cadence
        yield return new TrueFeat(
                ModData.FeatNames.TacticalCadence, 14,
                "Your remarkable breath control and concise instructions allow you to coordinate your allies more effectively, even in desperate situations.",
                $$"""
                  When you use Cadence Call, that action's quickened condition can also be used to Strike, and using it no longer causes allies to become {r}slowed 1{/r}.
                  """,
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Marshal)
            .WithPrerequisite(ModData.FeatNames.CadenceCall, "Cadence Call");

        // TODO: Lv14. Target of Opportunity
    }

    public static QEffect? GetMarshalAura(Creature marshal)
    {
        return marshal.FindQEffect(ModData.QEffectIds.MarshalsAuraProvider);
    }

    public static LegacyCreatureTargetingRequirement IsInMarshalAura()
    {
        return new LegacyCreatureTargetingRequirement((attacker, defender) =>
            defender.QEffects.Any(qf =>
                qf.Id == ModData.QEffectIds.MarshalsAuraEffect
                && qf.Source == attacker)
                ? Usability.Usable
                : Usability.NotUsableOnThisCreature("Not in marshal's aura"));
    }

    public static int GetMarshalAuraRange(Creature marshal)
    {
        return GetMarshalAura(marshal)?.Tag as int? ?? BaseAuraSize;
    }
}