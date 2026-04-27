using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.LoresAndWeaknesses;

/// <summary>
/// This class silos any references to optional assemblies, not calling them unless these mods are actually loaded by the user.
/// </summary>
public static class OptionalDependencies
{
    internal static void LoadAssuranceLores()
    {
        Feat autoKnow = new TrueFeat(
                RecallWeakness.FNAutomaticKnowledge,
                2,
                "You know basic facts off the top of your head.",
                $$"""
                  {b}Prerequisites{/b} expert in a skill that can {{RecallWeakness.GetActionLink("Recall a Weakness {icon:Action}")}} and that you have {link:Assurance}Assurance{/} for.
                  {b}Frequency{/b} Once per round.

                  Recall a Weakness {icon:Action} with the requisite skill, using Assurance on the skill check.
                  
                  {/b}Special{/b} You can select Automatic Knowledge multiple times, choosing a different skill each time. You can still only use Automatic Knowledge with any skill once per round.
                  """,
                [Trait.General, Trait.Skill],
                [/*To be filled later*/])
            .WithActionCost(0)
            .WithPermanentQEffect(
                "Once per round, Recall a Weakness using Assurance",
                qfFeat =>
                {
                    // Only apply this feat once.
                    if (qfFeat.Owner.HasEffect(RecallWeakness.AutomaticKnowledge))
                    {
                        qfFeat.Innate = false;
                        qfFeat.Description = null;
                        return;
                    }
                    
                    // Consolidate all Automatic Knowledges into one place
                    if (qfFeat.Owner.PersistentCharacterSheet?.Calculated.AllFeats is { } myFeats)
                    {
                        qfFeat.Description +=
                            " with "
                            + S.ConstructOrList(
                                myFeats
                                    .Where(ft => ft.FeatName.ToStringOrTechnical().Contains("Assurance") && ft.Tag is Skill)
                                    .Select(ft => ((Skill)ft.Tag!).ToStringOrTechnical().WithColor("Blue")));
                    }
                    qfFeat.Description += ".";
                    
                    qfFeat.Id = RecallWeakness.AutomaticKnowledge;
                    qfFeat.Value = 0; // Number of automatic knowledge feats
                    qfFeat.HideValue = true;
                    qfFeat.AdjustActiveRollCheckResult = (qfThis, action, target, result1) =>
                    {
                        if (action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is not { } skill
                            || !action.Name.ToLower().Contains("automatic knowledge"))
                            return result1;

                        int check = qfThis.Owner.Proficiencies
                            .Get(Skills.SkillToTrait(skill))
                            .ToNumber(qfThis.Owner.ProficiencyLevel) + 10 - action.ActiveRollSpecification
                            .DetermineDC(action, action.Owner, target)
                            .TotalNumber;

                        switch (check)
                        {
                            case >= 0 and < 10:
                                return CheckResult.Success;
                            case < 0 and > -10:
                                return CheckResult.Failure;
                            case >= 10:
                                return CheckResult.CriticalSuccess;
                            case <= -10:
                                return CheckResult.CriticalFailure;
                        }
                    };
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                            return null;
                        // Only create this submenu if there is more than one skill known.
                        if (qfThis.Value < 2)
                            return null;

                        return new SubmenuPossibility(
                            new ScrollIllustration(
                                IllustrationName.NarratorBook,
                                IllustrationName.FreeAction),
                            "Automatic Knowledge")
                        {
                            Subsections = [
                                new PossibilitySection("Automatic Knowledge")
                            ]
                        };
                    };
                });
        autoKnow.CanSelectMultipleTimes = true;
        ModManager.AddFeat(autoKnow, ModData.Traits.ModName);
        
        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            foreach ((Skill skill, _) in RecallWeakness.CreatureSkills)
            {
                Feat subKnow = CreateAutomaticKnowledgeSubfeat(skill);
                autoKnow.Subfeats!.Add(subKnow);
                ModManager.AddFeat(subKnow, ModData.Traits.ModName);
            }
            
            foreach (Lore lore in Lores.AllLores.OrderBy(lore => lore.Skill.ToStringOrTechnical()))
            {
                // Assurance
                Feat assurance = New_Skill_Feats_and_Items.ModLoader.RegisterNewAssurance(
                    lore.Skill,
                    subFeat =>
                    {
                        subFeat.ZOrder = 5;
                        subFeat.CustomName = Lores.DisplayOffset + "{icon:NarratorBook} " + lore.Name;
                    });
                
                // Automatic Knowledge
                Feat subKnow = CreateAutomaticKnowledgeSubfeat(lore.Skill, true);
                autoKnow.Subfeats!.Add(subKnow);
                ModManager.AddFeat(subKnow, ModData.Traits.ModName);
            }
            
            /*New_Skill_Feats_and_Items.SkillFeats.Assurance?.Subfeats = New_Skill_Feats_and_Items.SkillFeats.Assurance!
                .Subfeats!
                .OrderBy(ft => ft.ZOrder)
                .ThenBy(ft => ((Skill)ft.Tag!).ToStringOrTechnical())
                .ToList();*/
        };
    }

    /// <summary>
    /// Creates an Automatic Knowledge feat with a given skill.
    /// </summary>
    /// <param name="skill">The skill being used to Recall Weakness. The action it generates relies on <see cref="CreateAutomaticKnowledgeAction"/> and <see cref="RecallWeakness.SkillCanRecallOnTarget"/> for usability and functionality.</param>
    /// <param name="isLore">If the skill is a lore, add a unicode character to sort it to the bottom.</param>
    /// <returns></returns>
    public static Feat CreateAutomaticKnowledgeSubfeat(Skill skill, bool? isLore = false)
    {
        Feat autoKnow = new Feat(
                ModManager.RegisterFeatName(
                    ModData.IdPrepend + "AutomaticKnowledge." + skill.ToStringOrTechnical(),
                    (isLore is true ? (Lores.DisplayOffset + "{icon:NarratorBook} ") : null) + skill.ToStringOrTechnical()),
                "You know basic facts off the top of your head.",
                "You can use " + skill.ToStringOrTechnical() + " with Automatic Knowledge {icon:FreeAction}.",
                [], null)
            .WithTag(skill)
            .WithZOrder(isLore is true ? 1 : 0)
            .WithPrerequisite(
                values => values.GetProficiency(Skills.SkillToTrait(skill)) >= Proficiency.Expert,
                "You must be an expert in " + skill.ToStringOrTechnical() + ".")
            .WithPrerequisite(
                values => values.AllFeats.Any(ft =>
                    ft.FeatName.ToStringOrTechnical().Contains("Assurance")
                    && ft.Tag is Skill sTag
                    && sTag == skill),
                "You must have Assurance with " + skill.ToStringOrTechnical() + ".")
            .WithOnCreature(self =>
            {
                if (self.FindQEffect(RecallWeakness.AutomaticKnowledge) is not { } autoKnowQf)
                    return;
                autoKnowQf.Value++;
                QEffect grantAction = new QEffect()
                {
                    Name = "[AUTOMATIC KNOWLEDGE GRANTER: " + skill.ToStringOrTechnical() + "]",
                    ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (autoKnowQf.Value == 0)
                            return null;
                        if (autoKnowQf.Value > 1 && section.Name != "Automatic Knowledge")
                            return null;
                        if (autoKnowQf.Value == 1 && section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                            return null;
                        return new ActionPossibility(CreateAutomaticKnowledgeAction(qfThis.Owner, skill));
                    }
                };
                self.AddQEffect(grantAction);
            });

        return autoKnow;
    }

    public static CombatAction CreateAutomaticKnowledgeAction(Creature owner, Skill skill)
    {
        CombatAction recall = RecallWeakness.CreateRecallWeaknessAction(owner)
            .WithActionCost(0)
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, self, _) =>
            {
                if (!ModManager.TryParse("AssuranceOn", out QEffectId qfId))
                    return;
                QEffect assuranceOn = new QEffect() { Id = qfId };
                self.AddQEffect(assuranceOn);
                action.Tag = assuranceOn;
            })
            .WithEffectOnChosenTargets(async (action, self, _) =>
            {
                self.FindQEffect(RecallWeakness.AutomaticKnowledge)?.UsedThisTurn = true;
                (action.Tag as QEffect)?.ExpiresAt = ExpirationCondition.Immediately;
            });
        recall.Illustration = new ScrollIllustration(
            IllustrationName.NarratorBook,
            IllustrationName.FreeAction);
        recall.Description = recall.Description
            .Replace(
                "{b}Range{/b}",
                "{Blue}{b}Frequency{/b} Once per round{/Blue}\n{b}Range{/b}")
            .Replace(
                "Attempt a skill check",
                "Attempt a skill check {Blue}using Assurance{/Blue}");
        recall.WithFullRename("Automatic Knowledge (" + skill.ToStringOrTechnical() + ")");
        ((CreatureTarget)recall.Target)!
            .WithAdditionalConditionOnTargetCreature((a, d) =>
                a.FindQEffect(RecallWeakness.AutomaticKnowledge)?.UsedThisTurn ?? false
                    ? Usability.NotUsable("Used this round")
                    : Usability.Usable)
            .WithAdditionalConditionOnTargetCreature((a, d) =>
                RecallWeakness.SkillCanRecallOnTarget(a, skill, d)
                    ? Usability.Usable
                    : Usability.NotUsableOnThisCreature(skill.ToStringOrTechnical() + " doesn\'t apply"));
        recall
            .WithActiveRollSpecification(
                new ActiveRollSpecification(
                    new TaggedCalculatedNumberProducer((tcnp, action, attacker, target) =>
                    {
                        var check = TaggedChecks.SkillCheck(skill);
                        if (Lores.AllLores.FirstOrDefault(lore2 => lore2.Skill == skill) is { } lore)
                        {
                            int bonus = lore.IsSpecific ? 5 : 2;
                            string src = lore.IsSpecific ? "Specific lore" : "Unspecific lore";
                            check = check.WithExtraBonus((_, _, _) =>
                                    new Bonus(bonus, BonusType.Untyped, src, true));
                        }
                        tcnp.InvolvedSkill = skill;
                        return check.CalculatedNumberProducer.Invoke(action, attacker, target);;

                        /*const int baseAssurance = 10;
                        int prof = attacker.Proficiencies
                            .Get(Skills.SkillToTrait(skill))
                            .ToNumber(attacker.ProficiencyLevel);

                        return new CalculatedNumber(
                            baseAssurance,
                            "Assurance",
                            [
                                new Bonus(prof, BonusType.Untyped, "Proficiency", true)
                            ]);*/
                    }),
                    recall.ActiveRollSpecification!.TaggedDetermineDC));
        
        return recall;
    }
}