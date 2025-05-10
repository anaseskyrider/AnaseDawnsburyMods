using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class ModItems
{
    public static ItemName ArtisansHammer;
    public static void LoadItems()
    {
        ArtisansHammer = ModManager.RegisterNewItemIntoTheShop(
            "RunesmithPlaytest.ArtisansHammer",
            iName =>
                new Item(iName, Enums.Illustrations.ArtisansHammer, "Artisan's Hammer", 1, 4,
                        [Enums.Traits.CountsAsRunesmithFreeHand, Trait.Hammer, Trait.Homebrew, /*Trait.Martial,*/ Trait.Mod, /*Trait.Melee,*/ Trait.Razing, Enums.Traits.Runesmith, Trait.Uncommon])
                    .WithMainTrait(Enums.Traits.ArtisansHammer)
                    .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Bludgeoning))
                    .WithDescription("{i}This blacksmith's hammer has an especially long haft and bears special runic engravings which allows a runesmith to wield it with ease whilst practicing their craft, whether in battle or at a workbench.{/i}\n\nWielding this weapon counts as having a free hand for the purposes of {tooltip:Runesmith.Action.TraceRune}Tracing Runes{/}."));
    }
}