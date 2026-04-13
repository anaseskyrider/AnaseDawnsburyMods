using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        Trophies.Load();
        Core.Load();
        ClassFeats.Load();
        HuntingTools.Load();
    }
}