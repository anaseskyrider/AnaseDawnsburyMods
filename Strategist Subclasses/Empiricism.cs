using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.StrategistSubclasses;

public static class Empiricism
{
    public static void Load(Feat strategist)
    {
        ModManager.AddFeat(CreateSubclass(strategist));
    }
    
    public static Feat CreateSubclass(Feat strategist)
    {
        Feat empiricism = new Feat(
                ModData.FeatNames.Empiricism,
                "Everything comes down to data. Calculating statistics, running numbers, and using inductive reasoning allows you to determine the most likely outcome of any scenario, and anything out of place draws your keen attention.",
                $"You are trained in one Intelligence-based skill of your choice. You gain the {FeatName.ImprovedPerception.ToLink("Improved Perception")} strategist feat, and you gain the Expeditious Inspection free action.",
                [],
                null)
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.ImprovedPerception);
                List<FeatName> skills = [
                    Skills.SkillToFeat(Skill.Arcana),
                    Skills.SkillToFeat(Skill.Crafting),
                    Skills.SkillToFeat(Skill.Occultism),
                    Skills.SkillToFeat(Skill.Society),
                ];
                values.AddSelectionOptionRightNow(new SingleFeatSelectionOption("EmpiricismSkill", "Intelligence skill", 1, ft => skills.Contains(ft.FeatName)));
            })
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction inspect = new CombatAction(
                        qfThis.Owner,
                        ModData.Illustrations.ExpeditiousInspection,
                        "Expeditious Inspection",
                        [ModData.ModTrait, Trait.Investigator],
                        $$"""
                          {i}You observe and assess your surroundings with great speed.{/i}

                          {b}Frequency{/b} once per combat

                          Your next Seek or {{ModData.Tooltips.RecallWeakness("Recall Weakness")}} is a {icon:FreeAction} free action.
                          """,
                        Target.Self())
                        .WithActionCost(0)
                        .WithShortDescription("(Once per combat) Seek or Recall Weakness as a free action.")
                        .WithEffectOnSelf(async (_, self) =>
                        {
                            if (self.HasEffect(ModData.QEffectIds.ExpeditiousInspection))
                            {
                                self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.ExpeditiousInspection);
                                return;
                            }
                            
                            self.AddQEffect(new QEffect(
                                "Expeditious Inspection",
                                "Your next Seek or Recall Weakness is a free action.",
                                ExpirationCondition.Never,
                                self,
                                ModData.Illustrations.ExpeditiousInspection)
                            {
                                Id = ModData.QEffectIds.ExpeditiousInspection,
                                ModifyActionPossibility = (_, action) =>
                                {
                                    if (action.ActionId == ActionId.Seek
                                        || action.Name.Contains("Recall Weakness"))
                                        action.ActionCost = 0;
                                },
                                AfterYouTakeAction = async (qfThis2, action) =>
                                {
                                    if (action.ActionId == ActionId.Seek
                                        || action.Name.Contains("Recall Weakness"))
                                    {
                                        qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                    }
                                },
                            });
                        });
                    return new ActionPossibility(inspect);
                };
            })
            .WithRulesBlockForCombatAction(cr =>
            {
                CombatAction inspect = new CombatAction(
                    cr,
                    ModData.Illustrations.ExpeditiousInspection,
                    "Expeditious Inspection",
                    [ModData.ModTrait, Trait.Investigator],
                    $$"""
                      {i}You observe and assess your surroundings with great speed.{/i}

                      {b}Frequency{/b} once per combat

                      Your next Seek or {{ModData.Tooltips.RecallWeakness("Recall Weakness")}} is a {icon:FreeAction} free action.
                      """,
                    Target.Self())
                    .WithActionCost(0);
                return inspect;
            });
        strategist.Subfeats!.Add(empiricism);
        return empiricism;
    }
}