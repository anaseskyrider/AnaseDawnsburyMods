using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.GuardianClass;
public class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ////////////////
        // Load Calls //
        ////////////////
        ModData.LoadData();
        GuardianClass.LoadClass();
        GuardianFeats.LoadFeats();
        
        // Update class language
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            Feat? guardianClass = AllFeats.All.FirstOrDefault(ft => ft.FeatName == ModData.FeatNames.GuardianClass);
            guardianClass!.RulesText = guardianClass.RulesText.Replace("Ability boosts", "Attribute boosts");
        };
    }

    /// <summary>Create a new instance of <see cref="SleepRequest"/> using Reflection.</summary>
    public static AdvancedRequest NewSleepRequest(int sleepTime)
    {
        Type? sleepRequest = typeof(AdvancedRequest).Assembly.GetType("Dawnsbury.Core.Coroutines.Requests.SleepRequest");
        var constructor = sleepRequest?.GetConstructor([typeof(int)]);
        var sleep = constructor?.Invoke([sleepTime]);
        sleep?.GetType().GetProperty("CanBeClickedThrough")?.SetMethod?.Invoke(sleep, [false]);
        return (AdvancedRequest)sleep!;
    }

    /// <summary>Causes a QEffect to put an action in the Offense section with the given short description, but without listing any attack statistics.</summary>
    public static void DisplaysAsOffenseAction(QEffect qfFeat, string actionName, string shortDescription, int cost = 1)
    {
        qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
        {
            // Inserts into invisible section
            if (section.PossibilitySectionId != PossibilitySectionId.InvisibleActions)
                return null;
            CombatAction statBlockOnly = CombatAction.CreateSimple(
                    qfThis.Owner,
                    actionName,
                    [])
                .WithShortDescription(shortDescription)
                .WithActionCost(cost);
            statBlockOnly.Illustration = IllustrationName.None;
            return new ActionPossibility(statBlockOnly);
        }; 
    }
}