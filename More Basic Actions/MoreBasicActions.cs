using Dawnsbury.Core.CombatActions;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public class MoreBasicActions
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
    }
}