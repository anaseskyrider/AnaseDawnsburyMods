using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        Trophy.Load();
        CoreClass.Load();
        ClassFeats.Load();
        HuntingTools.Load();
    }
}