using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace AndroidAncestry;

public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.RegisterBooleanSettingsOption(
            "AndroidAncestry.RemoveNaniteSurgeAura",
            "Android: Remove Nanite Surge Glow",
            "Nanite Surge emits a temporary glow after the reaction is taken. This is purely cosmetic in Dawnsbury Days, and can be safely disabled by enabling this option.",
            false);
        
        AndroidAncestry.LoadAncestry();
        AncestryFeats.LoadFeats();
    }
}