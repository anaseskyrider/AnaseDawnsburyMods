using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public class MoreBasicActions
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Aid.LoadAid();
        Ready.LoadReady();
    }
}