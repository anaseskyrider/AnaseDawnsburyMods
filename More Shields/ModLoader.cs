using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;
using HarmonyLib;

//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreShields;
public class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Harmony shieldsHarmony = new Harmony("MoreShields");
        shieldsHarmony.PatchAll();
        
        ModData.LoadData();
        ShieldModifications.LoadModifications();
        OldShields.ModifyOldFeats();
        OldShields.ModifyOldShields();
        NewShields.LoadShields();

        // Update the items shop
        // PETR: This might not be necessary in the future.
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            // Adjust shop shields
            Items.ShopItems = Items.ShopItems
                .Select(OldShields.ShieldAlterations)
                .ToList();
            List<Item> allShields = Items.ShopItems
                .Where(item => item.HasTrait(Trait.Shield) && !item.Name.Contains("sturdy shield"))
                .ToList();
            List<Item> allPlatedShields = [];
            allShields.ForEach(shield =>
            {
                ShieldModifications.AllShieldPlates.ForEach(plate =>
                {
                    Item platedShield = ShieldModifications.CreatePlatedShield(shield.ItemName, plate)
                        .WithItemGreaterGroup(ModData.ItemGreaterGroups.PlatedShields);
                    allPlatedShields.Add(platedShield);
                });
            });
            // Add plated variants to the shop
            Items.ShopItems.InsertRange(
                Items.ShopItems.FindIndex(item => item.ItemName is ItemName.SturdyShield13),
                allPlatedShields);
            // Remove sturdy shields
            Items.ShopItems
                .RemoveAll(item => item.Name.Contains("sturdy shield"));
        };
    }
}