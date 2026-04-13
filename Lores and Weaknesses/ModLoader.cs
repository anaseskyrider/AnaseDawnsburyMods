using Dawnsbury.Modding;
using HarmonyLib;

namespace Dawnsbury.Mods.LoresAndWeaknesses;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Harmony loresHarmony = new Harmony("LoresAndWeaknesses");
        loresHarmony.PatchAll();
        
        RecallWeakness.Load();
        Lores.Load();
        NewSpells.Load();
    }
}