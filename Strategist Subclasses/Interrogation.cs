using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.StrategistSubclasses;

public static class Interrogation
{
    public static Feat LoadSubclass()
    {
        Feat interrogation = new Feat(
                ModData.FeatNames.Interrogation,
                "People can't help but trust you, whether through your inherent likableness or your firm insistence on sticking to the truth. You have a way about you that gets others talking, and you've developed interrogative techniques to help you get to the truth of your investigations.",
                "You are trained in Diplomacy. At the start of combat, you can use Declare Person of Interest as a {icon:FreeAction} free action.\n\n" + new ModdedIllustration(ModData.Illustrations.DDSunPath).IllustrationAsIconString + " {b}Modding{b} If you have the {i}Bundle of Backgrounds{/i} mod installed, you gain its No Cause for Alarm skill feat.",
                [ModData.Traits.StrategistSubclasses],
                [])
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Diplomacy);
                if (ModManager.TryParse("No Cause for Alarm {icon:TwoActions}", out FeatName causeAlarm))
                {
                    if (!values.HasFeat(causeAlarm))
                        values.GrantFeat(causeAlarm);
                }
            })
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.StartOfCombat = async qfThis =>
                {
                    Possibilities poiActions = Possibilities.Create(qfThis.Owner)
                        .Filter( ap =>
                        {
                            if (!ap.CombatAction.Name.Contains("Declare Person of Interest"))
                                return false;
                            ap.CombatAction.ActionCost = 0;
                            ap.RecalculateUsability();
                            return true;
                        });
                    List<Option> actions = await qfThis.Owner.Battle.GameLoop.CreateActions(qfThis.Owner, poiActions, null);
                    
                    if (actions.Count <= 0)
                        return;
                    
                    actions.Add(new CancelOption(true));
                    actions.Add(new PassViaButtonOption(" Don't declare "));
                            
                    Option chosenOption = (await qfThis.Owner.Battle.SendRequest(
                        new AdvancedRequest(qfThis.Owner, "Choose target for Declare Person of Interest or right click to cancel.", actions)
                        {
                            TopBarText = "Choose target for Declare Person of Interest or right click to cancel.",
                            TopBarIcon = IllustrationName.HuntPrey,
                        })).ChosenOption;
                    
                    await chosenOption.Action();
                };
            });
        ModManager.AddFeat(interrogation);
        return interrogation;
    }
}