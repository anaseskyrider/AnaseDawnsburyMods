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
        ModManager.RegisterInlineTooltip("StrategistSubclasses.RecallWeakness", "Requires the {i}DawnniExpanded{/i} mod loaded and installed before this mod.");
        
        AllFeats.GetFeatByFeatName(FeatName.Investigator)
            .Subfeats = [
                AlchemicalSciences.LoadSubclass(),
                Empiricism.LoadSubclass(),
                ForensicMedicine.LoadSubclass(),
                Interrogation.LoadSubclass(),
            ];
    }
}