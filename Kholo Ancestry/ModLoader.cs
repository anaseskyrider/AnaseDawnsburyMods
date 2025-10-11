using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.KholoAncestry;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // Fix the Flail Weapon to distinguish it from the Flail Group.
        ModManager.RegisterActionOnEachItem(item =>
        {
            if (item.MainTrait is Trait.Flail)
                item.Traits.Add(ModData.Traits.FlailItself);
            return item;
        });
        
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            Items.ShopItems = Items.ShopItems
                .Select(item =>
                {
                    if (item.MainTrait is Trait.Flail)
                        item.Traits.Add(ModData.Traits.FlailItself);
                    return item;
                })
                .ToList();
        };
        
        // Our regularly scheduled load functions
        ModData.LoadData();
        KholoAncestry.LoadAncestry();
        AncestryFeats.LoadFeats();
    }

    /// <summary>Create a new instance of <see cref="SleepRequest"/> using Reflection.</summary>
    public static AdvancedRequest NewSleepRequest(int sleepTime, bool clickedThrough = true)
    {
        Type? sleepRequest = typeof(AdvancedRequest).Assembly.GetType("Dawnsbury.Core.Coroutines.Requests.SleepRequest");
        var constructor = sleepRequest?.GetConstructor([typeof(int)]);
        var sleep = constructor?.Invoke([sleepTime]);
        sleep?.GetType().GetProperty("CanBeClickedThrough")?.SetMethod?.Invoke(sleep, [clickedThrough]);
        return (AdvancedRequest)sleep!;
    }
}