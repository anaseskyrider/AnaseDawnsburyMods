using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreShields;

public static class ShieldModifications
{
    public static List<ItemName> AllShieldPlates = [
        ModData.ItemNames.SturdyShieldPlatingMinor,
        ModData.ItemNames.SturdyShieldPlatingLesser,
        ModData.ItemNames.SturdyShieldPlatingModerate,
        ModData.ItemNames.SturdyShieldPlatingGreater,
        ModData.ItemNames.SturdyShieldPlatingMajor,
        ModData.ItemNames.SturdyShieldPlatingSupreme,
    ];
    
    public static void LoadModifications()
    {
        // TODO: Switch off debug for workshop
        const bool DEBUG = false;
        
        // Plating
        ModData.ItemNames.SturdyShieldPlatingMinor = RegisterNewShieldPlating(
            "SturdyShieldPlatingMinor" + (DEBUG ? "(DEBUG)" : null),
            "minor",
            4,
            75,
            3); // If OP: +2
        ModData.ItemNames.SturdyShieldPlatingLesser = RegisterNewShieldPlating(
            "SturdyShieldPlatingLesser" + (DEBUG ? "(DEBUG)" : null),
            "lesser",
            7,
            300,
            5); // If OP: +4 or +3
        ModData.ItemNames.SturdyShieldPlatingModerate = RegisterNewShieldPlating(
            "SturdyShieldPlatingModerate" + (DEBUG ? "(DEBUG)" : null),
            "moderate",
            10,
            900,
            8); // If OP: +6 or +5
        ModData.ItemNames.SturdyShieldPlatingGreater = RegisterNewShieldPlating(
            "SturdyShieldPlatingGreater" + (DEBUG ? "(DEBUG)" : null),
            "greater",
            13,
            2500,
            10); // If OP: +7 or +6
        ModData.ItemNames.SturdyShieldPlatingMajor = RegisterNewShieldPlating(
            "SturdyShieldPlatingMajor" + (DEBUG ? "(DEBUG)" : null),
            "major",
            16,
            8000,
            12); // If OP: +8 or +7
        ModData.ItemNames.SturdyShieldPlatingSupreme = RegisterNewShieldPlating(
            "SturdyShieldPlatingSupreme" + (DEBUG ? "(DEBUG)" : null),
            "supreme",
            19,
            32000,
            15); // If OP: +10 or +9
        
        // Augmentations
        ModData.ItemNames.ShieldAugmentationBackswing = RegisterNewShieldAugmentation(
            "ShieldAugmentationBackswing",
            "backswing",
            [Trait.Backswing]);
        ModData.ItemNames.ShieldAugmentationForceful = RegisterNewShieldAugmentation(
            "ShieldAugmentationForceful",
            "forceful",
            [Trait.Forceful]);
        ModData.ItemNames.ShieldAugmentationManeuverable = RegisterNewShieldAugmentation(
            "ShieldAugmentationManeuverable",
            "disarm, shove, and trip",
            [Trait.Disarm, Trait.Shove, Trait.Trip]);
        ModData.ItemNames.ShieldAugmentationVersatile = RegisterNewShieldAugmentation(
            "ShieldAugmentationVersatile",
            "versatile P and versatile S",
            [Trait.VersatileP, Trait.VersatileS]);
    }
    
    public static ItemName RegisterNewShieldPlating(string technicalName, string tier, int level, int price, int bonusHardness, params Trait[] traits)
    {
        return ModManager.RegisterNewItemIntoTheShop(
            technicalName,
            iName => new Item(
                    iName,
                    ModData.Illustrations.ShieldPlating,
                    "shield plating ("+tier+")",
                    level,
                    price,
                    [ModData.Traits.MoreShields, Trait.Runestone, ..traits])
                .WithRuneProperties(new RuneProperties(
                        tier + " plated",
                        ModData.RuneKinds.ShieldPlating,
                        "Magically enchanted plating makes the shield much sturdier.",
                        $"The enchanted shield's hardness increases by {bonusHardness}. For example, a {tier} plated steel shield has a hardness of {5 + bonusHardness} instead of 5.",
                        item1 => item1.Hardness += bonusHardness)
                    .WithCanBeAppliedTo((_, shield) =>
                    {
                        if (shield.Name.Contains("sturdy shield"))
                            return "Shield plating cannot be applied to sturdy shields";
                        if (!shield.HasTrait(Trait.Shield))
                            return "Shield plating can only be applied to shields.";
                        return null;
                    }))
                .WithItemGreaterGroup(ModData.ItemGreaterGroups.ShieldModifications)
                .WithItemGroup("Plating"));
    }
    
    public static Item CreatePlatedShield(ItemName item, ItemName plate)
    {
        return Items.CreateNew(item)
            .WithItemGroup("Plated ("+plate.ToStringOrTechnical().Replace("SturdyShieldPlating", "").Replace("2","").ToLower()+") shields")
            .WithModificationRune(plate);
    }

    /// <summary>
    /// Registers a shield augmentation.
    /// </summary>
    /// <param name="technicalName">The technical name for the <see cref="ItemName"/>.</param>
    /// <param name="humanizedTraits">A humanized description of the added traits, such as "versatile S and versatile P". If the text "and" or "," is found, it will pluralize the word "trait" in the description.</param>
    /// <param name="addedTraits">The traits to add to the shield.</param>
    /// <exception cref="ArgumentException">The addedTraits array is empty.</exception>
    public static ItemName RegisterNewShieldAugmentation(string technicalName, string humanizedTraits, params Trait[] addedTraits)
    {
        if (addedTraits.Length == 0)
            throw new ArgumentException("You must provide at least one Trait", nameof(addedTraits));
        string traitDescription =
            humanizedTraits
            + " trait"
            + (humanizedTraits.Contains("and") || humanizedTraits.Contains(",")
                ? "s"
                : null);
        string variantName = technicalName.Replace("ShieldAugmentation", "").ToLower();
        return ModManager.RegisterNewItemIntoTheShop(
            technicalName,
            iName => new Item(
                    iName,
                    ModData.Illustrations.ShieldAugmentation,
                    "shield augmentation ("+variantName+")",
                    0,
                    1,
                    [ModData.Traits.MoreShields, Trait.Runestone])
                .WithRuneProperties(new RuneProperties(
                        "augmented",
                        ModData.RuneKinds.ShieldAugmentation,
                        "There are numerous methods to modify shields — snarling rods to catch weapons, bladed edges, padding for nonlethal strikes, and so on — but all share basic functionality.",
                        $"The shield gains the {traitDescription}.",
                        item => { item.Traits.AddRange(addedTraits); })
                    .WithCanBeAppliedTo((_, shield) =>
                        shield.HasTrait(Trait.Shield) ? null : "Shield augmentations can only be applied to shields."))
                .WithItemGreaterGroup(ModData.ItemGreaterGroups.ShieldModifications)
                .WithItemGroup("Augmentations"));
    }
}