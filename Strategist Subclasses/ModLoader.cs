using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Modding;

//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.StrategistSubclasses;
public class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        
        Feat strategist = AllFeats.GetFeatByFeatName(FeatName.Investigator);
        strategist.Subfeats = [];
        AlchemicalSciences.Load(strategist);
        Empiricism.Load(strategist);
        ForensicMedicine.Load(strategist);
        Interrogation.Load(strategist);
    }
}