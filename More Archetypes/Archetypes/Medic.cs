using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Medic
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Improve Battle Medicine action
        // Delay for compatibility with DawnniExpanded and Remaster Expanded.
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            TrueFeat battleMedicine = (AllFeats.GetFeatByFeatName(FeatName.BattleMedicine) as TrueFeat)!;
            battleMedicine.Traits.Insert(0, ModData.ModTrait);
            battleMedicine.OnCreature = null;
            battleMedicine.WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                {
                    Proficiency prof = qfThis.Owner.PersistentCharacterSheet?.Calculated.GetProficiency(Trait.Medicine) ?? Proficiency.Trained;
                    
                    string name = qfThis.Name!.WithTag("b");
                    string basic =
                        $" [Flourish] Heal {(qfThis.Owner.HasFeat(ModData.FeatNames.WardMedic) ? "1-" + prof switch { >= Proficiency.Legendary => 4, Proficiency.Master => 3, _ => 2 } + " allies" : "an ally")} with a Medicine check as an 'other action'.";
                    string? continual = qfThis.Owner.HasFeat(ModData.FeatNames.ContinualRecovery)
                        ? $" On a crit, the temporary immunity is reduced to {(prof >= Proficiency.Legendary ? "1 round".WithColor("Blue") : "1 encounter")}."
                        : null;
                    string? medic = (qfThis.Owner.HasFeat(ModData.FeatNames.MedicDedication)
                        ? " " + ((prof >= Proficiency.Master ? "{Blue}Once per encounter{/Blue}" : "Once per day") + ", you can use Battle Medicine on a creature that's temporarily immune.").WithTag(qfThis.Owner.HasEffect(QEffectId.BattleMedicineImmunityBypassUsedThisEncounter) ? "strike" : null)
                        : null);
                    
                    return name + basic + continual + medic;
                };

                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                        return null;

                    Proficiency prof = Proficiency.Trained;
                    if (qfThis.Owner.PersistentCharacterSheet == null)
                        return new ActionPossibility(BattleMedicineAction(prof));
                    
                    prof = qfThis.Owner.PersistentCharacterSheet.Calculated.GetProficiency(Trait.Medicine);
                    List<ActionPossibility> possibilityList = new[] { Proficiency.Trained, Proficiency.Expert, Proficiency.Master, Proficiency.Legendary }
                        .Select(tier =>
                            prof < tier
                                ? null
                                : (ActionPossibility)BattleMedicineAction(tier))
                        .WhereNotNull()
                        .ToList();

                    if (possibilityList.Count > 1)
                        return new SubmenuPossibility(IllustrationName.HealersTools, "Battle Medicine")
                        {
                            Subsections =
                            {
                                new PossibilitySection("Battle Medicine")
                                {
                                    Possibilities = possibilityList.Cast<Possibility>().ToList()
                                }
                            }
                        };
                    else
                        return possibilityList[0];

                    CombatAction BattleMedicineAction(Proficiency proficiency)
                    {
                        CombatAction badMed = BattleMedicine.CreateBattleMedicineAction(qfThis.Owner, proficiency);
                        
                        Proficiency innerProf = qfThis.Owner.PersistentCharacterSheet is not null
                            ? prof : proficiency;
                        
                        // Adjust targeting
                        if (qfThis.Owner.HasFeat(ModData.FeatNames.WardMedic))
                        {
                            int multi = innerProf switch
                            {
                                >= Proficiency.Legendary => 4,
                                Proficiency.Master => 3,
                                _ => 2
                            };
                            badMed.Target = qfThis.Owner.HeldItems.Count == 0
                                ? Target.MultipleCreatureTargets(multi, SingleTarget)
                                : SingleTarget();
                            badMed.Description = badMed.Description.Replace("{b}Range{/b} touch", "{b}Range{/b} touch\n{b}Targets{/b} Up to " + multi + " allies if both hands are free");
                        }
                        else
                            badMed.Target = SingleTarget();
                        
                        // Adjust description.
                        (int flat, int bonus) = proficiency switch
                        {
                            Proficiency.Expert => (10, 5),
                            Proficiency.Master => (30, 10),
                            Proficiency.Legendary => (50, 15),
                            _ => (0, 0)
                        };
                        // Fix missing space
                        badMed.Description = badMed.Description
                            .Replace("{b}Requirements{/b}You", "{b}Requirements{/b} You");
                        // Color the Medic dedication bonus to blue.
                        if (!(flat == 0 || bonus == 0))
                            badMed.Description = badMed.Description
                                .Replace($"4d8+{flat}", $"4d8{{Blue}}+{flat+bonus}{{/Blue}}")
                                .Replace($"2d8+{flat}", $"2d8{{Blue}}+{flat+bonus}{{/Blue}}");

                        if (qfThis.Owner.HasFeat(ModData.FeatNames.ContinualRecovery))
                            badMed.Description = badMed.Description.Replace(
                                "Regardless of your result, the target is then temporarily immune to your Battle Medicine for the rest of the day.",
                                $"The target is then temporarily immune to your Battle Medicine for the rest of the day, or for {(innerProf >= Proficiency.Legendary ? "1 round".WithColor("Blue") : "the rest of the encounter")} on a critical success.");
                        
                        // Inform user of consumed immunity-bypass.
                        /*badMed.WithEffectOnEachTarget(async (_, caster, _, _) =>
                        {
                            
                        });*/
                        
                        return badMed;

                        CreatureTarget SingleTarget()
                        {
                            return Target.AdjacentFriendOrSelf()
                                // Use known object type
                                .WithAdditionalConditionOnTargetCreature(new FreeHandTargetingRequirement())
                                // Fix the targeting requirements to deal with QEffectId.Medic
                                // going unused.
                                .WithAdditionalConditionOnTargetCreature((a, d) =>
                                    IsImmuneToBattleMedicine(a, d)
                                        ? Usability.NotUsableOnThisCreature("immune")
                                        : Usability.Usable)
                                // Damage check has been moved to later in the list in order to display
                                // to the player that an immunity exists before planning to take damage.
                                .WithAdditionalConditionOnTargetCreature((a, d) =>
                                    d.Damage == 0
                                    && !(a.HasEffect(QEffectId.ParagonBattleMedicine)
                                         && CanBeParagoned(a, d))
                                        ? Usability.NotUsableOnThisCreature("healthy")
                                        : Usability.Usable);
                        }
                    }
                };
            });
        };
        
        // Remaster Medic Dedication
        // - Bonus to healing is a circumstance bonus.
        // - Bonus healing from Medic Dedication is blue in action blocks.
        // - Immunity restriction on Battle Medicine shows before healthy, allowing more reliable preplanning.
        if (ArchetypeFeats.GetDedicationFromArchetypeTrait(Trait.Medic) is { } medDed)
        {
            ModData.FeatNames.MedicDedication = medDed.FeatName;
            medDed.Traits.Insert(0, ModData.ModTrait);
            medDed.RulesText = medDed.RulesText.Replace(
                    "the target heals 5 additional HP at DC 20, 10 HP at DC 30, or 15 HP at DC 40.",
                    "the target gains a circumstance bonus to the healing received equal to 5 HP at DC 20, 10 HP at DC 30, or 15 HP at DC 40.\n\n");
            medDed.OnCreature = null;
            medDed.WithPermanentQEffect(qfFeat =>
            {
                // Don't set this ID. Instead, add a circumstance healing bonus.
                //qfFeat.Id = QEffectId.Medic;
                
                /*qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + $" Your Battle Medicine heals for more. {(qfFeat.Owner.Proficiencies.Get(Trait.Medicine) >= Proficiency.Master ? "{Blue}Once per encounter{/Blue}" : "Once per day")}, you can use Battle Medicine on a creature that's temporarily immune.";*/
                
                // Add healing to the target
                qfFeat.YouBeginAction = async (qfThis, action) =>
                {
                    if (action.ActionId is not ActionId.BattleMedicine
                        && !action.Name.Contains("Battle Medicine"))
                        return;

                    string prof = action.Name.Contains("DC 20") ? "expert"
                        : action.Name.Contains("DC 30") ? "master"
                        : action.Name.Contains("DC 40") ? "legendary"
                        : "";
                    int bonus = action.Name.Contains("DC 20") ? 5
                        : action.Name.Contains("DC 30") ? 10
                            : action.Name.Contains("DC 40") ? 15
                                : 0;

                    if (bonus == 0)
                        return;
                    
                    QEffect bonusHealing = new QEffect(ExpirationCondition.ExpiresAtEndOfSourcesTurn)
                    {
                        Name = "[MEDIC DEDICATION BONUS HEALING]",
                        Source = qfThis.Owner,
                        BonusToSelfHealing = (_, action2) =>
                            action2 == action
                                ? new Bonus(bonus, BonusType.Circumstance,
                                    "Medic dedication" + (string.IsNullOrEmpty(prof) ? null : $" ({prof})"))
                                : null,
                    };
                    
                    action.ChosenTargets.ChosenCreatures.ForEach(cr => cr.AddQEffect(bonusHealing));
                    
                    // Remove after action resolves
                    action.WithEffectOnEachTarget(async (_, _, _, _) =>
                        bonusHealing.ExpiresAt = ExpirationCondition.Immediately);
                };
            });
        }
        
        // Lv2: Continual Recovery
        // DOC: This is changed to have an in-combat effect in reducing the recovery time for Battle Medicine.
        yield return new TrueFeat(
                ModData.FeatNames.ContinualRecovery, 2,
                "Your patients have a harder time resisting repeat treatments.",
                "When you critically succeed with Battle Medicine, the target becomes temporarily immune for 1 encounter instead of 1 day. If you're legendary in Medicine, the immunity lasts for 1 round instead.",
                [Trait.General, Trait.Rebalanced, Trait.Skill])
            .WithPermanentQEffect(qfFeat =>
            {
                /*qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + $" On a critical success with Battle Medicine, the temporary immunity is reduced to {(qfFeat.Owner.Proficiencies.Get(Trait.Medicine) >= Proficiency.Legendary ? "1 round".WithColor("Blue") : "1 encounter")}.";*/
                
                qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                {
                    if (action.ActionId is not ActionId.BattleMedicine
                        || action.CheckResult < CheckResult.CriticalSuccess)
                        return;

                    bool isLegendary = qfFeat.Owner.Proficiencies.Get(Trait.Medicine) >= Proficiency.Legendary;

                    action.ChosenTargets.ChosenCreatures.ForEach(cr =>
                    {
                        cr.Battle.Log($"{cr.Name}'s temporary immunity to {action.Owner.Name}'s Battle Medicine has been reduced from 1 day to {(isLegendary ? "1 round" : "the rest of the encounter")}.");
                        
                        cr.AddQEffect(new QEffect()
                        {
                            ExpiresAt = isLegendary ? ExpirationCondition.ExpiresAtStartOfSourcesTurn : ExpirationCondition.Never,
                            Source = action.Owner,
                            EndOfCombat = async (qfImmune, _) =>
                            {
                                RemoveDailyImmunityToBattleMedicine(action.Owner, cr);
                            },
                            // Remove the daily immunity if this is expiring during combat
                            WhenExpires = qfImmune =>
                            {
                                RemoveDailyImmunityToBattleMedicine(action.Owner, cr);
                            }
                        });
                    });
                };
            })
            .WithPrerequisite(
                sheet => sheet.GetProficiency(Trait.Medicine) >= Proficiency.Expert,
                "You must be an expert in Medicine");
        
        // Lv2: Ward Medic
        yield return new TrueFeat(
                ModData.FeatNames.WardMedic, 2,
                "You’ve studied in large medical wards, treating several patients at once and tending to all their needs.",
                $$"""
                    {b}Requirements{/b} Both your hands are free.

                    When you use {{FeatName.BattleMedicine.ToLink("Battle Medicine")}} or Treat Poison, you can target up to two targets. If you're a master in Medicine, increase this to three targets; and if you're legendary, increase this to four targets.
                    """,
                    [Trait.General, Trait.Rebalanced, Trait.Skill])
            .WithPrerequisite(
                sheet => sheet.GetProficiency(Trait.Medicine) >= Proficiency.Expert,
                "You must be an expert in Medicine");
        
        // Lv4: Treat Condition
        // (Skill feat variant)
        Feat treatCondition = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
                FeatName.TreatCondition, Trait.Medic, 4)
            .WithEquivalent(values => values.HasFeat(FeatName.TreatCondition));
        // Normal list order: 0:Healing, 1:Manipulate, 2:Archetype
        treatCondition.Traits.Insert(2, Trait.Skill);
        treatCondition.RulesText += "\n\n" + ModData.Illustrations.DdSun.IllustrationAsIconString + "{b}More Archetypes{/b} This is a skill feat variant of Treat Condition which can be taken as a general feat or skill feat.";
        ModData.FeatNames.TreatConditionSkillVariant = treatCondition.FeatName;
        yield return treatCondition;

        // Lv4: Doctor's Visitation
        yield return new TrueFeat(
                ModData.FeatNames.DoctorsVisitation, 4,
                "You move to provide immediate care to those who need it.",
                $$"""
                You Stride, then use one of the following based on the number of actions you spent:
                {icon:Action} {{FeatName.BattleMedicine.ToLink("Battle Medicine")}} or Treat Poison.
                {icon:TwoActions} Stabilize, Staunch Bleeding, or {{FeatName.TreatCondition.ToLink("Treat Condition")}} {i}(if you have that feat){/i}.
                """,
                [Trait.Flourish])
            .WithAvailableAsArchetypeFeat(Trait.Medic)
            .WithActionCost(Constants.ACTION_COST_VARIABLE_ACTION_COST_ONE_OR_TWO)
            .WithPermanentQEffect(
                "Stride, then {icon:Action} use Battle Medicine or Treat Poison; or {icon:TwoActions} use Stabilize or Staunch Bleeding.",
                qfFeat =>
                {
                    if (qfFeat.Owner.HasFeat(FeatName.TreatCondition))
                        qfFeat.Description = qfFeat.Description!.Replace(
                            "use Stabilize or Staunch Bleeding",
                            "use Stabilize, Staunch Bleeding, {Blue}or Treat Condition{/Blue}");
                    
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                            return null;
                        
                        IllustrationName medicineIllustration = qfThis.Owner.CarriesItem(ItemName.ExpandedHealersTools)
                            ? IllustrationName.ExpandedHealersTools
                            : IllustrationName.HealersTools;

                        CombatAction doctorVisit = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(
                                    IllustrationName.FleetStep,
                                    medicineIllustration),
                                "Doctor's Visitation",
                                [Trait.Basic, Trait.Flourish],
                                """
                                {i}You move to provide immediate care to those who need it.{/i}
                                
                                You Stride, then use one of the following based on the number of actions you spent:
                                {icon:Action} Battle Medicine or Treat Poison.
                                {icon:TwoActions} Stabilize, Staunch Bleeding, or Treat Condition {i}(if you have that feat){/i}.
                                """,
                                Target.DependsOnActionsSpent(
                                    Target.Self()
                                        .WithAdditionalRestriction(cr =>
                                        {
                                            List<Creature> frens = cr.Battle.AllCreatures
                                                .Where(cr.FriendOf)
                                                .ToList();
                                            // Battle Medicine
                                            if (frens.Any(fren =>
                                                    fren.Damage > 0
                                                    && !IsImmuneToBattleMedicine(cr, fren)))
                                                return null;
                                            // Treat Poison
                                            if (frens.Any(fren => fren.QEffects.Any(qf => qf.RepresentsPoison)))
                                                return null;
                                            return "No allies to heal nor poisons to end";
                                        }),
                                    Target.Self()
                                        .WithAdditionalRestriction(cr =>
                                        {
                                            List<Creature> frens = cr.Battle.AllCreatures
                                                .Where(cr.FriendOf)
                                                .ToList();
                                            // Stabilize
                                            if (frens.Any(fren => fren.HasEffect(QEffectId.Dying)))
                                                return null;
                                            // Staunch Bleeding
                                            if (frens.Any(fren =>
                                                    fren.QEffects.Any(qf =>
                                                        qf.Id == QEffectId.PersistentDamage
                                                        && qf.GetPersistentDamageKind() == DamageKind.Bleed)))
                                                return null;
                                            if (frens.Any(fren => CanBeTreated(cr, fren)))
                                                return null;
                                            return "No bleeding, dying, or treatable allies";
                                        }),
                                    null!))
                            .WithCreateVariantDescription((actionCost, _) =>
                            {
                                return "{i}You move to provide immediate care to those who need it.{/i}\n\nYou Stride, then use " + actionCost switch
                                {
                                    1 => "Battle Medicine or Treat Poison.",
                                    2 => "Stabilize, Staunch Bleeding or Treat Condition {i}(if you have it){/i}.",
                                    _ => "exception"
                                };
                            })
                            .WithActionCost(Constants.ACTION_COST_VARIABLE_ACTION_COST_ONE_OR_TWO)
                            .WithEffectOnChosenTargets(async (thisAction, caster, _) =>
                            {
                                // Stride
                                if (!await caster.StrideAsync("Make a Stride.", allowCancel: true, allowStep: false))
                                {
                                    thisAction.RevertRequested = true;
                                    return;
                                }

                                // Do stuff based on actions spent
                                await TakeOptions(thisAction, thisAction.SpentActions switch
                                {
                                    2 => ap =>
                                    {
                                        if (!IsStabilize(ap.CombatAction)
                                            && !IsStaunchBleeding(ap.CombatAction)
                                            && !IsTreatCondition(ap.CombatAction))
                                            return false;
                                        ap.CombatAction.ActionCost = 0;
                                        ap.RecalculateUsability();
                                        return true;
                                    },
                                    _ => ap => // Default to this so that both 1-costs and other modifications fallback to this.
                                    {
                                        if (!IsBattleMedicine(ap.CombatAction)
                                            && !IsTreatPoison(ap.CombatAction))
                                            return false;
                                        ap.CombatAction.ActionCost = 0;
                                        ap.RecalculateUsability();
                                        return true;
                                    },
                                });
                            });
                        
                        return Possibilities.CreateSpellPossibility(doctorVisit)
                            .WithPossibilitySize(PossibilitySize.Full);

                        async Task TakeOptions(CombatAction sourceAction, Func<ActionPossibility, bool> keepOnlyWhat)
                        {
                            Possibilities poss = sourceAction.Owner.Possibilities.Filter(keepOnlyWhat);
    
                            var active = sourceAction.Owner.Battle.ActiveCreature;
                            sourceAction.Owner.Battle.ActiveCreature = sourceAction.Owner;
                            sourceAction.Owner.Possibilities = poss;
    
                            List<Option> actions = await sourceAction.Owner.Battle.GameLoop.CreateActions(sourceAction.Owner, poss, null);

                            if (!actions.Any(opt => opt is not PassOption))
                            {
                                sourceAction.Owner.Battle.Log("{b}Doctor's Visitation{/b} was converted to a simple Stride.");
                                sourceAction.RevertRequested = true;
                                sourceAction.Owner.Actions.UseUpActions(1, ActionDisplayStyle.UsedUp);
                                return;
                            }
                            
                            sourceAction.Owner.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                            await sourceAction.Owner.Battle.GameLoop.OfferOptions(sourceAction.Owner, actions, true);
    
                            sourceAction.Owner.Battle.ActiveCreature = active;
                        }
                    };
                });
        
        // Lv4: Holistic Care
        // (Skill feat variant)
        Feat holisticCare = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
                FeatName.HolisticCare, Trait.Medic, 6)
            .WithEquivalent(values => values.HasFeat(FeatName.HolisticCare));
        // Normal list order: 0:Archetype
        holisticCare.Traits.Add(Trait.Skill);
        holisticCare.RulesText += "\n\n" + ModData.Illustrations.DdSun.IllustrationAsIconString + "{b}More Archetypes{/b} This is a skill feat variant of Holistic Care which can be taken as a general feat or skill feat.";
        ModData.FeatNames.HolisticCareSkillVariant = holisticCare.FeatName;
        yield return holisticCare;
        
        // TODO: Lv16 Resuscitate
        // Might not be doable unless there's a meaningful way to prevent death as a game-over state while reducing all healing.
    }

    /// <summary>
    /// Returns whether a creature is immune to Battle Medicine, accounting for the circumstance bonus remaster of Medic Dedication which removes QEffectId.Medic.
    /// </summary>
    public static bool IsImmuneToBattleMedicine(Creature medic, Creature patient) =>
        patient.PersistentUsedUpResources.UsedUpActions.Contains("BattleMedicineFrom:" + medic.Name)
        && (!(medic.HasEffect(QEffectId.Medic) || medic.HasFeat(ModData.FeatNames.MedicDedication))
            || medic.HasEffect(QEffectId.BattleMedicineImmunityBypassUsedThisEncounter)
            || medic.Proficiencies.Get(Trait.Medicine) < Proficiency.Master
            && medic.PersistentUsedUpResources.UsedUpActions.Contains("BattleMedicineImmunityBypassUsed"));
    
    /// <summary>
    /// Removes the daily immunity to Battle Medicine from MEDIC on PATIENT. 
    /// </summary>
    /// <returns>Whether the immunity was successfully removed from PATIENT.</returns>
    public static bool RemoveDailyImmunityToBattleMedicine(Creature medic, Creature patient)
    {
        return
            // Base game Battle Medicine
            patient.PersistentUsedUpResources.UsedUpActions.Remove("BattleMedicineFrom:" + medic.Name)
            // Dawnni Battle Medicine
            || patient.PersistentUsedUpResources.UsedUpActions.Remove("BattleMedicine:"+medic.Name);
    }
    
    public static bool CanBeTreated(Creature medic, Creature patient)
    {
        List<QEffectId> effects =
        [
            QEffectId.Clumsy,
            QEffectId.Enfeebled,
            QEffectId.Sickened
        ];
        if (medic.HasEffect(QEffectId.HolisticCare))
            effects.AddRange([QEffectId.Frightened, QEffectId.Stupefied, QEffectId.Stunned]);
        return patient.QEffects.Any(qf =>
            effects.Contains(qf.Id)
            && qf.ExpiresAt != ExpirationCondition.Ephemeral
            && qf is { Value: > 0, SourceAction: not null });
    }

    public static bool CanBeParagoned(Creature medic, Creature patient)
    {
        List<QEffectId> effects =
        [
            QEffectId.Clumsy,
            QEffectId.Enfeebled,
            QEffectId.Sickened,
        ];
        if (medic.Proficiencies.Get(Trait.Medicine) >= Proficiency.Legendary)
            effects.AddRange([QEffectId.Frightened, QEffectId.Stunned]);
        if (medic.PersistentCharacterSheet?.Calculated.AllFeats.Any(ft =>
                ft.FeatName.ToStringOrTechnical().Contains("GodlessHealing")) ?? false)
            effects.AddRange([QEffectId.Stupefied, QEffectId.Drained]);
        return patient.QEffects.Any(qf =>
            effects.Contains(qf.Id)
            && qf.ExpiresAt != ExpirationCondition.Ephemeral
            && qf is { Value: > 0, SourceAction: not null });
    }

    public static bool IsBattleMedicine(CombatAction action) => action.ActionId is ActionId.BattleMedicine || action.Name.Contains("Battle Medicine");
    public static bool IsTreatPoison(CombatAction action) => action.ActionId == ActionId.TreatPoison;
    public static bool IsStabilize(CombatAction action) => action.Name == "Stabilize";
    public static bool IsStaunchBleeding(CombatAction action) => action.Name == "Staunch bleeding";
    public static bool IsTreatCondition(CombatAction action) => action.Name.Contains("Treat ") && !IsTreatPoison(action);
}