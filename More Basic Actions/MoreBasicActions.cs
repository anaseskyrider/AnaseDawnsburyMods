using Dawnsbury.Modding;

namespace MoreBasicActions;

public class MoreBasicActions
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Aid.LoadMod();
    }
}