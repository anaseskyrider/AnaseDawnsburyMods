using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.LoresAndWeaknesses;

public static class RecallWeakness
{
    #region Enum Data

    /// <summary>
    /// The effect that is applied to the target of Recall Weakness.
    /// </summary>
    public static QEffectId RecallWeaknessEffect;
    
    /// <summary>
    /// A hidden effect which tracks the number of Recall Weakness attempts against the creature (stored as the QF's Value).
    /// </summary>
    public static QEffectId RecallWeaknessAttempts;
    
    /// <summary>
    /// The bonus to Recall Weakness applied by a successful critical hit with Combat Assessment's Strike.
    /// </summary>
    public static QEffectId CombatAssessmentBonus;

    /// <summary>
    /// The Recall Weakness action. Uses the same ID as DawnniExpanded.
    /// </summary>
    public static ActionId RWActionId;
    
    /// <summary>
    /// The Combat Assessment action. Uses the same ID as DawnniExpanded.
    /// </summary>
    public static ActionId CombatAssessment;

    /// <summary>
    /// The Additional Lore feat from Player Core. Lores are automatically added to this feat's subfeats when registered using this mod.
    /// </summary>
    public static readonly FeatName FNAdditionalLore = ModManager.RegisterFeatName(
        ModData.IdPrepend + "AdditionalLore",
        "Additional Lore");

    /// <summary>
    /// The feat that is granted to all characters to give them Recall Weakness. Also contains a detailed description. Used for tooltips to Recall Weakness actions (see: <see cref="GetActionLink"/>).
    /// </summary>
    public static readonly FeatName FNRecallWeakness = ModManager.RegisterFeatName(
        ModData.IdPrepend + "RecallWeakness",
        "Recall Weakness {icon:Action}");

    public static readonly FeatName FNDubiousKnowledge = ModManager.RegisterFeatName(
        ModData.IdPrepend + "DubiousKnowledge",
        "Dubious Knowledge");

    public static readonly FeatName FNCombatAssessment = ModManager.RegisterFeatName(
        "CombatAssessment",
        "Combat Assessment");

    public const SfxName SFXRecallWeakness = SfxName.OpenPage;

    #endregion

    /// <summary>
    /// The basic, default description of the action. Used in multiple places and modified for the creature at the end by feats.
    /// </summary>
    public static readonly string DefaultActionDescription =
        $$"""
        {b}Range{/b} 30 feet
        
        Attempt a skill check against a foe within range. The skill used depends on the creature (see table below), and the DC is based on the target's level.{{S.FourDegreesOfSuccess(
            "The creature gains a -2 circumstance penalty to its next saving throw or to its saving throw DCs on the next attack against it (such as a Trip or a Grapple).",
            "As critical success, but the penalty is -1.",
            null,
            "As success, but the penalty is a +1 circumstance bonus instead.")}}
        
        This effect lasts until the end of your next turn, and the DC for your Recall Weakness increases on that creature each time (+2/+5/+10). You can't attempt to Recall Weakness on that creature after attempting a check at DC+10.
        
        {b}Arcana{/b} Constructs, Beasts, Dragons, Elementals.
        {b}Crafting{/b} Constructs, Objects.
        {b}Nature{/b} Animals, Beasts, Elementals, Fey, Fungi, Leshies, Plants.
        {b}Occultism{/b} Aberrations, Oozes, Spirits.
        {b}Religion{/b} Celestials, Fiends, Monitors, Undead.
        {b}Society{/b} Humanoids.
        {b}Lore{/b} As described by any lore. Lores have a +2 bonus to Recall Weakness, or +5 if it's a specific lore. 
        """;

    /// <summary>
    /// The constant name of the Slightest Glance Weakness feat, used as an ID due to the feat's design predating <see cref="ModManager.RegisterFeatName"/>.
    /// </summary>
    public const string SlightestGlanceWeaknessId = "Slightest Glance Weakness";
    
    /// <summary>
    /// Associates creature traits to the skills that can be used to Recall a Weakness on them.
    /// </summary>
    public static Dictionary<Skill, List<Trait>> CreatureSkills { get; } = new Dictionary<Skill, List<Trait>>
    {
        {
            Skill.Arcana,
            [Trait.Arcane, Trait.Arcana, Trait.Beast, Trait.Construct, Trait.Dragon, Trait.Elemental]
        },
        {
            Skill.Crafting,
            [Trait.Construct, Trait.Object, Trait.AnimatedObject]
        },
        {
            Skill.Nature,
            [Trait.Animal, Trait.Beast, Trait.Elemental, Trait.Fey, Trait.Fungus, Trait.Leshy, Trait.Nature, Trait.Plant, Trait.Primal]
        },
        {
            Skill.Occultism,
            [Trait.Aberration, Trait.Occult, Trait.Occultism, Trait.Ooze, Trait.Spirit]
        },
        {
            Skill.Religion,
            [Trait.Celestial, Trait.Divine, Trait.Fiend, Trait.Monitor, Trait.Religion, Trait.Undead,
                // Redundant extra subtypes, just in case.
                Trait.Demon, Trait.Devil, Trait.Starborn]
        },
        {
            Skill.Society,
            [Trait.Humanoid,
                // Include sub-types, even if you might accidentally include an Undead Goblin,
                // simply because creatures like the Goblin Drake Rider (Dragon, Goblin) exist.
                Trait.Goblin, Trait.Human, Trait.Kobold, Trait.Merfolk, Trait.Orc]
        }
    };
    
    public static void Load()
    {
        RecallWeaknessEffect = ModData.SafelyRegister<QEffectId>("RecallWeaknessEffect");
        RecallWeaknessAttempts = ModData.SafelyRegister<QEffectId>("IncreasedRecallWeaknessDC");
        CombatAssessmentBonus = ModData.SafelyRegister<QEffectId>("CombatAssessmentBonus");
        RWActionId = ModData.SafelyRegister<ActionId>("RecallWeaknessActionID"); // Backwards compatible ID
        CombatAssessment = ModData.SafelyRegister<ActionId>("Combat Assessment"); // Backwards compatible ID
        
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
        
        // Replace DawnniExpanded's Recall Weakness
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            // TODO: Creatures can RW: Check for HasFeat, replace with QFIDs instead
            if (cr.PersistentCharacterSheet == null
                || cr.HasTrait(Trait.Mindless))
                return;
            
            cr.RemoveAllQEffects(qf => qf.Name == "Recall Weakness Granter");
            cr.WithFeat(FNRecallWeakness);
        });
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Recall Weakness
        // Formatted as a basic action for tooltip-linking purposes.
        Feat recallWeakness = new Feat(
            FNRecallWeakness,
            "You attempt to recall a weakness of a foe within 30 feet, sharing it with your allies to use to your advantage.",
            DefaultActionDescription,
            [Trait.Homebrew, Trait.Concentrate, Trait.Linguistic],
            null);
        recallWeakness.WithPermanentQEffect(null, qfFeat =>
        {
            // Identifiable name in the debugger
            qfFeat.Name = "[LORES AND WEAKNESSES: RECALL WEAKNESS GRANTER]";
            qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
            {
                if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                    return null;
                return (ActionPossibility) CreateRecallWeaknessAction(qfThis.Owner);
            };
        });
        yield return recallWeakness;
        
        // Dubious Knowledge
        if (AllFeats.All.FirstOrDefault(ft => ft is
            {
                FeatName: FeatName.CustomFeat,
                Name: "In-depth Weakness"
            })
            is { } dawnniDepth)
        {
            dawnniDepth.Traits.Clear();
            dawnniDepth.RulesText =
                "{b}Lores and Weaknesses{/b} This feat has been replaced. Use {b}Dubious Knowledge{/b} instead.";
        }
        yield return new TrueFeat(
                FNDubiousKnowledge,
                1,
                "You\'re a treasure trove of information, but not all of it comes from reputable sources.",
                $"When you fail (but don't critically fail) a {recallWeakness.ToLink("Recall Weakness")} check with any skill, the DC for repeated attempts doesn't increase. In addition, you repeat Recall Weakness checks on a creature any number of times. The DC doesn't increase beyond +10 when you do so.",
                [Trait.General, Trait.Rebalanced, Trait.Skill])
            .WithPrerequisite(
                values =>
                {
                    List<Trait> validTraits =
                    [
                        Trait.Arcana,
                        Trait.Crafting,
                        Trait.Nature,
                        Trait.Occultism,
                        Trait.Religion,
                        Trait.Society,
                        ..Lores.AllLores.Select(lore => lore.Trait)
                    ];
                    return validTraits.Any(t => values.GetProficiency(t) >= Proficiency.Trained);
                },
                "You must be trained in a Recall Weakness skill {i}(any Lore skill; or Arcana, Crafting, Nature, Occultism, Religion, or Society){/i}.")
            .WithPermanentQEffect(
                "Failing (but not critically failing) to Recall Weakness {icon:Action} doesn't increase the DC, and creatures never become immune to it.",
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.ActionId != RWActionId
                            || action.ChosenTargets.ChosenCreature is not { } target)
                            return;

                        // Remove immunity
                        target.RemoveAllQEffects(qf =>
                            qf.Id == QEffectId.ImmunityToTargeting
                            && qf.Source == qfThis.Owner
                            && qf.Tag is ActionId id
                            && id == action.ActionId);
                        
                        // Reverse the attempt-increase
                        if (action.CheckResult is not CheckResult.Failure
                            || target.QEffects.FirstOrDefault(qf =>
                                    qf.Id == RecallWeaknessAttempts
                                    && qf.Source == qfThis.Owner)
                                is not { } attempts)
                            return;

                        attempts.Value--;
                    };
                });

        // TODO: Automatic Knowledge

        // Slightest Glance Weakness
        const string glanceFlavor = "You can more easily observe and convey your foe's weaknesses.";
        const string glanceRules = "Increase the range of {b}Recall Weakness{/b} {icon:Action} to 60 feet. If you are a master in Perception, increase the range to 120 feet. If you are legendary in Perception, you don't need line of sight to your target (they must not be undetected to you).";
        Trait[] glanceTraits = [Trait.General, Trait.Homebrew, Trait.Skill];
        const string shortDesc = "Increase the range of {b}Recall Weakness{/b} {icon:Action} to 60 feet.";
        Action<QEffect> permQ = qfFeat =>
        {
            if (qfFeat.Owner.PersistentCharacterSheet?.Calculated is not { } values)
                return;
            Proficiency perception = values.GetProficiency(Trait.Perception);
            bool hasGlance = qfFeat.Owner.PersistentCharacterSheet?.Calculated.AllFeats.Any(ft =>
                ft is { FeatName: FeatName.CustomFeat, Name: SlightestGlanceWeaknessId }) ?? false;
            // 30 ft normally, 60 with glance, 120 with glance and expert perception
            int range = hasGlance
                ? perception > Proficiency.Expert
                    ? 24 // 120 feet
                    : 12 // 60 feet
                : 6; // 30 feet

            if (hasGlance)
            {
                if (perception > Proficiency.Expert)
                    qfFeat.Description = qfFeat.Description!.Replace(
                        "60 feet",
                        (range * 5 + " feet").WithColor("Blue"));
                if (perception >= Proficiency.Legendary)
                    qfFeat.Description += " {Blue}You don\'t need line of sight to the target.{/Blue}";
            }
        };
        if (AllFeats.All.FirstOrDefault(ft => ft is
            {
                FeatName: FeatName.CustomFeat,
                Name: SlightestGlanceWeaknessId
            })
            is { } dawnniGlance)
        {
            dawnniGlance.Traits.Clear();
            dawnniGlance.Traits.AddRange(glanceTraits);
            dawnniGlance.FlavorText = glanceFlavor;
            dawnniGlance.RulesText = glanceRules;
            dawnniGlance.OnCreature = null;
            dawnniGlance.WithPermanentQEffect(shortDesc, permQ);
        }
        else
            #pragma warning disable CS0618 // Type or member is obsolete
            yield return new TrueFeat(
                    FeatName.CustomFeat, // Backwards compatible with DawnniExpanded
                    2,
                    glanceFlavor, glanceRules, glanceTraits)
                .WithCustomName(SlightestGlanceWeaknessId) // Backwards compatible with DawnniExpanded
            #pragma warning restore CS0618 // Type or member is obsolete
                .WithPrerequisite(
                    values => values.GetProficiency(Trait.Perception) >= Proficiency.Expert,
                    "You must be an expert in Perception")
                .WithRulesBlockForCombatAction(CreateRecallWeaknessAction)
                .WithPermanentQEffect(shortDesc, permQ);

        // Combat Assessment
        if (AllFeats.All.FirstOrDefault(ft => ft is
            {
                FeatName: FeatName.CustomFeat,
                Name: "Combat Assessment"
            })
            is { } dawnniAssessment)
        {
            dawnniAssessment.Traits.Clear();
            dawnniAssessment.RulesText =
                "{b}Lores and Weaknesses{/b} This feat has been replaced with a newer version. Use it instead.";
        }
        yield return new TrueFeat(
                FNCombatAssessment,
                1,
                "You make a telegraphed attack to learn about your foe.",
                $"Make a melee Strike. On a hit, you can immediately attempt a check to {GetActionLink("Recall a Weakness {icon:FreeAction}")} on the target. On a critical hit, you gain a +2 circumstance bonus to the check. The target is then immune to Combat Assessment.",
                [Trait.Fighter])
            .WithActionCost(1)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.WithDisplayActionInOffenseSection(
                    "Combat Assessment",
                    "Make a melee Strike. On a hit, you Recall their Weakness (you gain a +2 bonus on a crit).");
                
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee))
                        return null;
                    StrikeModifiers mods = new StrikeModifiers()
                    {
                        OnEachTarget = async (caster, target, result) =>
                        {
                            target.AddQEffect(QEffect.ImmunityToTargeting(CombatAssessment));
                            
                            if (result < CheckResult.Success)
                                return;

                            // Critical hit bonus
                            if (result >= CheckResult.CriticalSuccess)
                                caster.AddQEffect(new QEffect(
                                    "Combat Assessment (critical success)",
                                    "You gain a +2 circumstance bonus to the check to Recall Weakness as part of Combat Assessment.",
                                    ExpirationCondition.ExpiresAtEndOfAnyTurn,
                                    caster,
                                    null /*Hides this effect on the user*/)
                                {
                                    Id = CombatAssessmentBonus,
                                    BonusToSkillChecks = (_, action, _) =>
                                        action.ActionId == RWActionId
                                        && action.Tag is ActionId id
                                        && id == CombatAssessment
                                            ? new Bonus(2, BonusType.Circumstance, "Combat Assessment (critical success)", true)
                                            : null,
                                    AfterYouTakeAction = async (qfThis, action) =>
                                    {
                                        if (action.ActionId != RWActionId)
                                            return;
                                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                    }
                                });
                            
                            await caster.Battle.GameLoop.FullCast(
                                CreateRecallWeaknessAction(caster)
                                    .WithActionCost(0)
                                    .WithTag(CombatAssessment),
                                ChosenTargets.CreateSingleTarget(target));
                        }
                    };
                    CombatAction comAssess = qfFeat.Owner
                        .CreateStrike(item, -1, mods)
                        .WithActionId(CombatAssessment)
                        .WithExtraTrait(Trait.Basic)
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription2(
                            mods,
                            additionalSuccessText: "Recall a Weakness on the target.",
                            additionalCriticalSuccessText:
                            "Gain a +2 circumstance bonus to the check to Recall Weakness.",
                            additionalAftertext: "The target becomes immune to Combat Assessment."))
                        .WithTargetingTooltip((action, target, _) =>
                            "{b}Strike{/b}\n"
                            + CombatActionExecution.BreakdownAttackForTooltip(action, target).TooltipDescription
                            + "\n\n{b}Recall Weakness{/b}\n"
                            + CombatActionExecution.BreakdownAttackForTooltip(CreateRecallWeaknessAction(action.Owner), target).TooltipDescription);
                    comAssess.WithFullRename("Combat Assessment");
                    comAssess.Illustration = new SideBySideIllustration(
                        comAssess.Illustration,
                        IllustrationName.NarratorBook);

                    return comAssess;
                };
            });
    }

    /// <summary>
    /// Gets a tooltip link to the Recall Weakness action, handled through a hidden Feat.
    /// </summary>
    /// <param name="caption">The caption of the action. (Default: "Recall Weakness {icon:Action}")</param>
    /// <returns>The fully enclosed feat link to Recall Weakness.</returns>
    public static string GetActionLink(string caption = "Recall Weakness {icon:Action}") =>
        AllFeats.GetFeatByFeatName(FNRecallWeakness).ToLink(caption);

    /// <summary>
    /// Creates the basic Recall Weakness action.
    /// </summary>
    /// <param name="owner">The action owner.</param>
    public static CombatAction CreateRecallWeaknessAction(Creature owner)
    {
        Proficiency perception = owner.Proficiencies.Get(Trait.Perception);
        bool hasGlance = owner.PersistentCharacterSheet?.Calculated.AllFeats.Any(ft =>
            ft is { FeatName: FeatName.CustomFeat, Name: SlightestGlanceWeaknessId }) ?? false;
        // 30 ft normally, 60 with glance, 120 with glance and expert perception
        int range = hasGlance
            ? perception > Proficiency.Expert 
                ? 24 // 120 feet
                : 12 // 60 feet
            : 6; // 30 feet
        string rulesText = DefaultActionDescription;
        
        if (owner.HasFeat(FNDubiousKnowledge))
            rulesText = rulesText.Replace(
                " You can't attempt to Recall Weakness on that creature after attempting a check at DC+10.",
                " {Blue}You can attempt this check any number of times.{/Blue}");
        
        CreatureTarget crTar = RecallWeaknessTarget(range, hasGlance && perception >= Proficiency.Legendary);
        
        if (hasGlance)
        {
            rulesText = rulesText.Replace("30 feet", range * 5 + " feet");
            if (perception >= Proficiency.Legendary)
                rulesText = rulesText.Replace(
                    "Attempt a skill check against a foe within range.",
                    "Attempt a skill check against a foe within range. {Blue}You don\'t need line of sight to the target.{/Blue}");
        }
        
        return new CombatAction(
                owner,
                IllustrationName.NarratorBook,
                "Recall Weakness",
                [Trait.Basic, ModData.Traits.ModName, Trait.Homebrew, Trait.Concentrate, Trait.Skill],
                $$"""
                {i}You attempt to spot or remember a foe's weakness to use to your advantage.{/i}
                
                {{rulesText}}
                """,
                crTar)
            .WithActionCost(1)
            .WithActionId(RWActionId)
            .WithSoundEffect(SFXRecallWeakness)
            .WithActiveRollSpecification(
                new ActiveRollSpecification(
                    new TaggedCalculatedNumberProducer((tcnp, action, attacker, target) =>
                    {
                        if (target is null)
                            return new CalculatedNumber(0, "NO TARGET FOUND", []);

                        List<TaggedCalculatedNumberProducer> bestSkills = [];

                        // Go through skills
                        foreach ((Skill skill, List<Trait> traits) in CreatureSkills)
                        {
                            if (target.Traits.ContainsOneOf(traits))
                                bestSkills.Add(TaggedChecks.SkillCheck(skill));
                            else
                                // Handle traits that aren't in the game currently 
                                switch (skill)
                                {
                                    case Skill.Occultism:
                                        if (target.Traits.Any(to => to.ToStringOrTechnical()
                                                is "Astral" or "Dream" or "Ethereal" or "Time"))
                                            bestSkills.Add(TaggedChecks.SkillCheck(Skill.Occultism));
                                        break;
                                    case Skill.Religion:
                                        if (target.Traits.Any(to => to.ToStringOrTechnical()
                                                is "Shade"))
                                            bestSkills.Add(TaggedChecks.SkillCheck(Skill.Religion));
                                        break;
                                    default:
                                        continue;
                                }
                        }

                        // Go through lores
                        foreach (Lore lore in Lores.AllLores)
                        {
                            if (lore.ValidRecallTarget is null
                                // RULING: You must at least be adding your level, such as from
                                // Untrained Improvisation, to be able to use a lore.
                                // Hidden lores you didn't properly acquire are filtered out,
                                // so Improv feats can't give you like Thaum's lore.
                                || (attacker.Proficiencies.Get(lore.Trait) is var prof
                                    && (prof == Proficiency.Untrained ||
                                        (prof == Proficiency.UntrainedWithLevel && lore.IsHidden))))
                                continue;
                            // Find the first function in each lore that applies,
                            // breaking the loop through each function on the first true return.
                            foreach (Func<Creature, Creature, bool> func in lore.ValidRecallTarget
                                         .GetInvocationList()
                                         .Select(del => del as Func<Creature, Creature, bool>)
                                         .WhereNotNull()
                                         .ToList())
                                if (func.Invoke(attacker, target))
                                {
                                    int bonus = lore.IsSpecific ? 5 : 2;
                                    string src = lore.IsSpecific ? "Specific lore" : "Unspecific lore";
                                    bestSkills.Add(TaggedChecks.SkillCheck(lore.Skill)
                                        .WithExtraBonus((_, _, _) =>
                                            new Bonus(bonus, BonusType.Untyped, src, true)));
                                    break;
                                }
                        }

                        // Add Society as a fallback skill
                        if (bestSkills.Count == 0)
                            bestSkills.Add(TaggedChecks.SkillCheck(Skill.Society));

                        TaggedCalculatedNumberProducer bestSkill = TaggedChecks.BestRoll([..bestSkills]);

                        tcnp.InvolvedSkill = bestSkill.InvolvedSkill;
                        tcnp.IsPerception =  bestSkill.IsPerception;

                        return bestSkill.CalculatedNumberProducer.Invoke(action, attacker, target);
                    }),
                    new TaggedCalculatedNumberProducer((tcnp, _, attacker, target) =>
                    {
                        List<Bonus?> bonuses =
                        [
                            target?.QEffects.FirstOrDefault(qf =>
                                    qf.Id == RecallWeaknessAttempts
                                    && qf.Source == attacker)
                                ?.Value is { } attempts
                                ? new Bonus(
                                    AttemptsToDC(attempts),
                                    BonusType.Untyped,
                                    (attempts + 1).Ordinalize2() + " attempt",
                                    true)
                                : null
                        ];

                        return new CalculatedNumber(
                            Checks.LevelBasedDC(target!.Level),
                            "Level " + target.Level + " DC",
                            bonuses);
                    })
                )
            )
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                // Increase the DC for future RWs from the caster
                if (target.QEffects.FirstOrDefault(qf =>
                        qf.Id == RecallWeaknessAttempts
                        && qf.Source == caster)
                    is not { } attempts)
                {
                    attempts = new QEffect()
                    {
                        Name = "[RECALL WEAKNESS ATTEMPTS]",
                        Id = RecallWeaknessAttempts,
                        Value = 0,
                        Source = caster,
                    };
                    target.AddQEffect(attempts);
                }

                // Add immunity once the check is attempted at -10.
                if (AttemptsToDC(attempts.Value) == 10)
                    target.AddQEffect(QEffect.ImmunityToTargeting(action.ActionId, caster));

                // Increment the attempts
                attempts.Value++;

                // Do nothing on a failure.
                if (result == CheckResult.Failure)
                    return;

                // Add a bonus or penalty to the target's next save (DoS determines)
                int modifier = result switch
                {
                    CheckResult.CriticalFailure => 1,
                    CheckResult.Success => -1,
                    CheckResult.CriticalSuccess => -2,
                    _ => 0
                };

                target.AddQEffect(new QEffect(
                    "Exposed Weakness",
                    "You have a " + modifier.Greenify() + " circumstance " + (modifier > 0 ? "bonus".WithColor("Green") : "penalty".WithColor("Red")) + " to your next saving throw, or to the next attack against one of your saving throw DCs.",
                    ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                    caster,
                    IllustrationName.NarratorBook)
                {
                    Id = RecallWeaknessEffect,
                    Key = caster.Name + "RecallWeakness",
                    CannotExpireThisTurn = true, // Until end of NEXT turn
                    BonusToDefenses = (_, _, def) => 
                        def is Defense.Fortitude or Defense.Reflex or Defense.Will
                        ? new Bonus(modifier, BonusType.Circumstance, "Recall weakness (" + result.HumanizeLowerCase2() + ")", modifier > 0)
                        : null,
                    AfterYouMakeSavingThrow = (qfThis, _, _) =>
                    {
                        /*if (hasIndepth == false || EffectValue == 1)*/
                        {
                            qfThis.CannotExpireThisTurn = false;
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    },
                    AfterYouAreTargeted = async (qfThis, combatAction) =>
                    {
                        if (combatAction.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense
                            is not (Defense.Fortitude or Defense.Reflex or Defense.Will))
                            return;
                        
                        /*if (hasIndepth == false || EffectValue == 1)*/
                        {
                            qfThis.CannotExpireThisTurn = false;
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    }
                });
            });

        int AttemptsToDC(int attempts)
        {
            return attempts switch
            {
                <= 0 => 0,
                1 => 2,
                2 => 5,
                >= 3 => 10,
            };
        }
    }

    public static CreatureTarget RecallWeaknessTarget(int range, bool ignoresLoE = false)
    {
        CreatureTarget crTar = new CreatureTarget(
            RangeKind.Ranged,
            [
                new MaximumRangeCreatureTargetingRequirement(range),
                new EnemyCreatureTargetingRequirement()
            ],
            (tg, a, d) =>
                !d.HasEffect(RecallWeaknessEffect)
                    ? a.Level
                    : 0.1f);
        
        if (!ignoresLoE)
            crTar.CreatureTargetingRequirements.Add(new UnblockedLineOfEffectCreatureTargetingRequirement());

        return crTar;
    }
}