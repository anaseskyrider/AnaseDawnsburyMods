using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        Aid.LoadAid();
        Ready.LoadReady();
        HelpUp.LoadHelpUp();
        QuickRepair.LoadFeat();
        LongJump.LoadLongJump();
        Reposition.LoadReposition();
        DropProne.LoadDropProne();
        Reload.LoadReload();
    }
}