using Dawnsbury.Audio;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.JustCrossTheBridge;
public static class ModLoader
{
    public static SfxName RiverAmbiance;
    
    // TODO: Check ORC notice for publish year. Could be 2025 or 2026.
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        RiverAmbiance = ModManager.RegisterNewSoundEffect("JustCrossTheBridgeAssets/Vistula(Very)Short.ogg", 0.18f);
        ModManager.RegisterEncounter<JustCrossTheBridge>("JustCrossTheBridge.tmx");
    }
}