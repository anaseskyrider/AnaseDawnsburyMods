using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;

//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.StrategistSubclasses;
public class ModLoader
{
    
    // TODO: Add base game improvements like shorter/combined stat blocks.
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();

        // Shorten "Declare Person of Interest" and "Devise a Stratagem" descriptions.
        Feat investigator = AllFeats.GetFeatByFeatName(FeatName.Investigator)
            .WithOnCreature((values, self) =>
            {
                // Description shortening
                self.AddQEffect(new QEffect()
                {
                    Name = "[STRATEGIST SUBCLASSES: DESCRIPTION MODIFIER]",
                    ModifyActionPossibility = (qfThis, action) =>
                    {
                        switch (action.Name)
                        {
                            case "Declare Person of Interest":
                                action.ShortDescription = action.ShortDescription!
                                    .Replace(
                                        "As an action, ",
                                        "")
                                    .Replace(
                                        "as an action, ",
                                        "")
                                    .Replace(
                                        "Once per encounter,",
                                        "(Once per encounter)")
                                    .Replace(
                                        "{Blue}Twice{/} per encounter,",
                                        "({Blue}Twice{/} per encounter)")
                                    .Replace(
                                        "designate an enemy as a person of interest.",
                                        "Designate an enemy.")
                                    .Replace(
                                        "and all enemies with the same name",
                                        "{Blue}and all enemies with the same name{/Blue}");
                                break;
                            case "Devise a Stratagem":
                                action.ShortDescription = action.ShortDescription!
                                    .Replace(
                                        "As an action, once per round, you choose an enemy and roll d20",
                                        "(Once per round) Choose an enemy and roll 1d20")
                                    .Replace(
                                        "use the rolled number",
                                        "use that result")
                                    .Replace(
                                        "Intelligence bonus",
                                        "INT")
                                    .Replace(
                                        "Strength or Dexterity",
                                        "STR or DEX");
                                break;
                        }
                    },
                });
            });
        
        // Remove Group of Interest description which is redundant with embedded
        // description updates to Declare Person of Interest.
        Feat groupPoI = AllFeats.GetFeatByFeatName(FeatName.GroupOfInterest);
        groupPoI.OnCreature = null;
        groupPoI.WithPermanentQEffect(null, qfFeat => qfFeat.Id = QEffectId.GroupOfInterest);
        
        Feat strategist = AllFeats.GetFeatByFeatName(FeatName.Investigator);
        strategist.Subfeats = [];
        AlchemicalSciences.Load(strategist);
        Empiricism.Load(strategist);
        ForensicMedicine.Load(strategist);
        Interrogation.Load(strategist);
    }
}