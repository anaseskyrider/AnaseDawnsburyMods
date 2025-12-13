using Dawnsbury.Modding;

namespace AndroidAncestry;

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