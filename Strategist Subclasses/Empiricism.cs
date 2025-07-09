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
    public static Feat LoadSubclass()
    {
        ModManager.RegisterActionOnEachActionPossibility(thisAction =>
        {
            if (!thisAction.Owner.HasEffect(ModData.QEffectIds.ExpeditiousInspection))
                return;
            
            if (thisAction.ActionId == ActionId.Seek)
                thisAction.ActionCost = 0;
            
            if (thisAction.Name.Contains("Recall Weakness"))
                thisAction.ActionCost = 0;
        });
        
        Feat empiricism = new Feat(
                ModData.FeatNames.Empiricism,
                "Everything comes down to data. Calculating statistics, running numbers, and using inductive reasoning allows you to determine the most likely outcome of any scenario, and anything out of place draws your keen attention.",
                "You are trained in one Intelligence-based skill of your choice. You gain the Improved Perception strategist feat, and you gain the Expeditious Inspection free action.",
                [ModData.Traits.StrategistSubclasses],
                [])
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
                        [Trait.Investigator],
                        $"{{i}}You observe and assess your surroundings with great speed.{{/i}}\n\n{{b}}Frequency{{/b}} once per combat\n\nYour next Seek or {ModData.Tooltips.RecallWeakness("Recall Weakness")} is a {{icon:FreeAction}} free action.",
                        Target.Self())
                        .WithActionCost(0)
                        .WithEffectOnSelf(async (thisAction, self) =>
                        {
                            if (self.HasEffect(ModData.QEffectIds.ExpeditiousInspection))
                            {
                                self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.ExpeditiousInspection);
                                return;
                            } // else
                            self.AddQEffect(new QEffect(
                                "Expeditious Inspection",
                                "Your next Seek or Recall Weakness is a free action.",
                                ExpirationCondition.Never,
                                self,
                                ModData.Illustrations.ExpeditiousInspection)
                            {
                                Id = ModData.QEffectIds.ExpeditiousInspection,
                                AfterYouTakeAction = async (qfThis2, thisAction2) =>
                                {
                                    if (thisAction2.ActionId == ActionId.Seek
                                        || thisAction2.Name.Contains("Recall Weakness"))
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
                    [Trait.Investigator],
                    $"{{i}}You observe and assess your surroundings with great speed.{{/i}}\n\n{{b}}Frequency{{/b}} once per combat\n\nYour next Seek or {ModData.Tooltips.RecallWeakness("Recall Weakness")} is a {{icon:FreeAction}} free action.",
                    Target.Self())
                    .WithActionCost(0);
                return inspect;
            });
        ModManager.AddFeat(empiricism);
        return empiricism;
    }
}