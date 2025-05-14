using Dawnsbury.Modding;

namespace MoreBasicActions;

public class MoreBasicActions
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Aid.LoadMod();
        //Ready.LoadMod(); // TODO: "Readying an attack is useful under two circumstances. First, an enemy comes within reach/range. This would be easy to implement. Second, an enemy is made flat-footed or flanked (most relevant for rogues)." - Dinglebob
    }
}