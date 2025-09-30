using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreShields;

public static class NewShields
{
    public static List<ItemName> AllNewShields = [
        ModData.ItemNames.Buckler,
        ModData.ItemNames.CastersTarge,
        ModData.ItemNames.HeavyRondache,
        ModData.ItemNames.MeteorShield,
        ModData.ItemNames.FortressShield,
    ];
    
    /// <summary>Load new shields into Dawnsbury Days. Called by <see cref="ModLoader"/>.</summary>
    public static void LoadShields()
    {
        // Create new shields
        ModData.ItemNames.Buckler = ModManager.RegisterNewItemIntoTheShop(
            "Buckler",
            iName => new Item(
                    iName,
                    ModData.Illustrations.Buckler,
                    "buckler",
                    0, 1,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.LightShield, Trait.Worn, ModData.Traits.WornShield)
                .WithMainTrait(ModData.Traits.Buckler)
                .WithDescription("This very small shield is a favorite of duelists and quick, lightly armored warriors. It's typically made of steel and strapped to your forearm.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(3));
        ModData.ItemNames.FortressShield = ModManager.RegisterNewItemIntoTheShop(
            "FortressShield",
            iName => new Item(
                    iName,
                    ModData.Illustrations.FortressShield,
                    "fortress shield",
                    1, 20,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.HeavyShield, ModData.Traits.CoverShield, ModData.Traits.Hefty14)
                .WithMainTrait(ModData.Traits.FortressShield)
                .WithDescription("Also known as portable walls, these thick and heavy shields are slightly larger than tower shields. Like tower shields, they're typically made from wood reinforced with metal, but many are made from larger amounts of metal or even stone.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(6));
        ModData.ItemNames.MeteorShield = ModManager.RegisterNewItemIntoTheShop(
            "MeteorShield",
            iName => new Item(
                    iName,
                    ModData.Illustrations.MeteorShield,
                    "meteor shield",
                    0, 4,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, Trait.Thrown30Feet, ModData.Traits.MediumShield)
                .WithMainTrait(ModData.Traits.MeteorShield)
                .WithDescription("Meteor shields are specifically designed with throwing in mind. A meteor shield is made from thin steel and has quick-release straps, allowing for easy, long-distance throws.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning)
                {
                    VfxStyle = new VfxStyle(1, ProjectileKind.Arrow, IllustrationName.WoodenShieldBoss)
                })
                .WithShieldProperties(4));
        ModData.ItemNames.HeavyRondache = ModManager.RegisterNewItemIntoTheShop(
            "HeavyRondache",
            iName => new Item(
                    iName,
                    ModData.Illustrations.HeavyRondache,
                    "heavy rondache",
                    1, 5,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.LightShield, Trait.Worn, ModData.Traits.WornShield)
                .WithMainTrait(ModData.Traits.HeavyRondache)
                .WithDescription("Similar in size to a buckler, this shield is intended to absorb blows instead of deflecting attacks. It features multiple layers of metal and is reinforced with additional wood.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(5));
        // TODO: Caster's Targe
        /*ModData.ItemNames.CastersTarge = ModManager.RegisterNewItemIntoTheShop(
            "CastersTarge",
            iName => new Item(
                    iName,
                    ModData.Illustrations.CastersTarge,
                    "Caster's Targe",
                    0, 2,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, Trait.NonMetallic, ModData.Traits.LightShield)
                .WithMainTrait(ModData.Traits.CastersTarge)
                .WithDescription("This small shield is made from wood. It features a special panel of parchment along the inside surface that allows it to store a single spell scroll. This scroll can be activated from the shield while it is being held.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(3)
                .WithStoresItem((bag, heldItem) =>
                    !heldItem.HasTrait(Trait.Scroll) || bag.StoredItems.Count >= 1 ? "This shield can only hold 1 spell scroll." : null));*/
    }
}