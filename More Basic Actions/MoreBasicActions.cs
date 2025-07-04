using Dawnsbury.Core.CombatActions;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public class MoreBasicActions
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // Ensures compatibility with other mods registering the same ID, regardless of load order.
        if (ModManager.TryParse("PrepareToAid", out ActionId prepareAid))
            ModData.ActionIds.PrepareToAid = prepareAid;
        else
            ModData.ActionIds.PrepareToAid = ModManager.RegisterEnumMember<ActionId>("PrepareToAid");
        if (ModManager.TryParse("AidReaction", out ActionId aidReaction))
            ModData.ActionIds.AidReaction = aidReaction;
        else
            ModData.ActionIds.AidReaction = ModManager.RegisterEnumMember<ActionId>("AidReaction");
        
        Aid.LoadAid();
        Ready.LoadReady();
        HelpUp.LoadHelpUp();
        QuickRepair.LoadFeat();
    }
}