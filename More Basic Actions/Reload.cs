using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class Reload
{
    public static void LoadReload()
    {
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            cr.AddQEffect(new QEffect()
            {
                Name = "[GRIP AFTER RELOAD]",
                Key = "GripAfterReload",
                Value = 1,
                AfterYouTakeAction = async (qfThis, action) =>
                {
                    if (action.ActionId is not ActionId.Reload
                        || action.Item is null
                        || action.Item.EphemeralItemProperties.NeedsReload
                        || !action.Item.TwoHandCapable
                        || action.Item.WieldedInTwoHands
                        || !qfThis.Owner.HasFreeHand)
                        return;
                    HandednessRules.MakeDoubleGrip(action.Item);
                    qfThis.Owner.Overhead(
                        "Add hand {icon:FreeAction}",
                        Color.White,
                        "{b}" + qfThis.Owner.Name + "{/b} adds a hand to their weapon as part of reloading.",
                        "Add hand",
                        "\"Switching your grip to free a hand and then to place your hands in the grip necessary to wield the weapon are both included in the actions you spend to reload a weapon.\"",
                        new Traits([ModData.Traits.MoreBasicActions]));
                }
            });
        });
    }
}