using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeMedic
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Make skill feat variant of Treat Condition.
        Feat treatCondition = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
                FeatName.TreatCondition, Trait.Medic, 4)
            .WithEquivalent(values => values.HasFeat(FeatName.TreatCondition));
        // Normal list order: 0:Healing, 1:Manipulate, 2:Archetype
        treatCondition.Traits.Insert(2, Trait.Skill);
        treatCondition.Traits.Insert(0, ModData.Traits.MoreDedications);
        treatCondition.RulesText += ModData.Illustrations.DawnsburySun.IllustrationAsIconString + "{b}More Dedications{/b} This is a skill feat variant of Treat Condition which can be taken as a general feat or skill feat (requires the {i}Skill Feats for Everyone{/i} mod).";
        ModData.FeatNames.TreatConditionSkillVariant = treatCondition.FeatName;
        yield return treatCondition;

        // Doctor's Visitation
        yield return new TrueFeat(
                ModData.FeatNames.DoctorsVisitation,
                4,
                "You move to provide immediate care to those who need it.",
                """
                Stride, then use one of the following based on the number of actions you spent:
                {icon:Action} Battle Medicine or Treat Poison.
                {icon:TwoActions} Stabilize, Staunch Bleeding, or Treat Condition {i}(if you have it){/i}.
                """,
                [ModData.Traits.MoreDedications, Trait.Flourish])
            .WithAvailableAsArchetypeFeat(Trait.Medic)
            .WithActionCost(Constants.ACTION_COST_VARIABLE_ACTION_COST_ONE_OR_TWO)
            .WithPermanentQEffect(
                "Stride, then {icon:Action} use Battle Medicine or Treat Poison, or {icon:TwoActions} use Stabilize, Staunch Bleeding, or Treat Condition",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.OtherManeuvers)
                            return null;
                        
                        IllustrationName medicineIllustration = qfThis.Owner.CarriesItem(ItemName.ExpandedHealersTools)
                            ? IllustrationName.ExpandedHealersTools
                            : IllustrationName.HealersTools;

                        const string flavorText = "{i}You move to provide immediate care to those who need it.{/i}\n\n";

                        CombatAction doctorVisit = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(
                                    IllustrationName.FleetStep,
                                    medicineIllustration),
                                "Doctor's Visitation",
                                [Trait.Basic, Trait.Flourish],
                                flavorText + """
                                Stride, then use one of the following based on the number of actions you spent:
                                {icon:Action} Battle Medicine or Treat Poison.
                                {icon:TwoActions} Stabilize, Staunch Bleeding, or Treat Condition {i}(if you have it){/i}.
                                """,
                                Target.DependsOnActionsSpent(
                                    Target.Self(),
                                    Target.Self(),
                                    null!))
                            .WithCreateVariantDescription((actionCost, _) =>
                            {
                                return flavorText + actionCost switch
                                {
                                    1 => "Stride, then use Battle Medicine or Treat Poison.",
                                    2 => "Stride, then use Stabilize, Staunch Bleeding or Treat Condition {i}(if you have it){/i}.",
                                    _ => "exception"
                                };
                            })
                            .WithActionCost(Constants.ACTION_COST_VARIABLE_ACTION_COST_ONE_OR_TWO)
                            .WithEffectOnChosenTargets(async (thisAction, caster, _) =>
                            {
                                // Stride
                                if (!await caster.StrideAsync("Make a stride.", allowCancel: true, allowStep: false))
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

                            if (actions.Any(opt => opt is not PassOption))
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
        
        // Make skill feat variant of Holistic Care.
        Feat holisticCare = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
                FeatName.HolisticCare, Trait.Medic, 4)
            .WithEquivalent(values => values.HasFeat(FeatName.TreatCondition));
        // Normal list order: 0:Archetype
        holisticCare.Traits.Add(Trait.Skill);
        holisticCare.Traits.Insert(0, ModData.Traits.MoreDedications);
        holisticCare.RulesText += ModData.Illustrations.DawnsburySun.IllustrationAsIconString + "{b}More Dedications{/b} This is a skill feat variant of Holistic Care which can be taken as a general feat or skill feat (requires the {i}Skill Feats for Everyone{/i} mod).";
        ModData.FeatNames.HolisticCareSkillVariant = holisticCare.FeatName;
        yield return holisticCare;
        
        // TODO: Lv8. Preventative Treatment
        // Can this even be done? Is it even any good?
        
        /* Higher Level Feats
         * @16 Resuscitate
         */
    }

    public static bool IsBattleMedicine(CombatAction action) => action.Name.Contains("Battle Medicine");
    public static bool IsTreatPoison(CombatAction action) => action.ActionId == ActionId.TreatPoison;
    public static bool IsStabilize(CombatAction action) => action.Name == "Stabilize";
    public static bool IsStaunchBleeding(CombatAction action) => action.Name == "Staunch bleeding";
    public static bool IsTreatCondition(CombatAction action) => action.Name.Contains("Treat ") && !IsTreatPoison(action);
}