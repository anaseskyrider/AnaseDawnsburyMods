using Dawnsbury.Modding;

namespace Dawnsbury.Mods.AndroidAncestry;

public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        AndroidAncestry.LoadAncestry();
        AncestryFeats.LoadFeats();
    }
}