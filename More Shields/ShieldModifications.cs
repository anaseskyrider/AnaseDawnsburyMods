using System.Reflection;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreShields;

public static class ShieldModifications
{
    public static readonly List<ItemName> AllShieldPlates = [
        ModData.ItemNames.SturdyShieldPlatingMinor,
        ModData.ItemNames.SturdyShieldPlatingLesser,
        ModData.ItemNames.SturdyShieldPlatingModerate,
        ModData.ItemNames.SturdyShieldPlatingGreater,
        ModData.ItemNames.SturdyShieldPlatingMajor,
        ModData.ItemNames.SturdyShieldPlatingSupreme,
    ];
    
    public static void LoadModifications()
    {
        // Plating
        // Bonus amounts are equivalent to the amount it takes to
        // raise a steel shield's hardness to an equivalent level
        // sturdy shield.
        (ModData.ItemNames.SturdyShieldPlatingMinor, var minorFactory) = RegisterNewShieldPlating(
            "SturdyShieldPlatingMinor",
            "minor",
            4, 75, 3, 22, 1,
            null);
        (ModData.ItemNames.SturdyShieldPlatingLesser, var lesserFactory) = RegisterNewShieldPlating(
            "SturdyShieldPlatingLesser",
            "lesser",
            7, 300, 5, 30, 2,
            ModData.ItemNames.SturdyShieldPlatingMinor); // If OP: +4 or +3
        (ModData.ItemNames.SturdyShieldPlatingModerate, var moderateFactory) = RegisterNewShieldPlating(
            "SturdyShieldPlatingModerate",
            "moderate",
            10, 900, 8, 42, 3,
            ModData.ItemNames.SturdyShieldPlatingLesser); // If OP: +6 or +5
        (ModData.ItemNames.SturdyShieldPlatingGreater, var greaterFactory) = RegisterNewShieldPlating(
            "SturdyShieldPlatingGreater",
            "greater",
            13, 2500, 10, 50, 4,
            ModData.ItemNames.SturdyShieldPlatingModerate); // If OP: +7 or +6
        (ModData.ItemNames.SturdyShieldPlatingMajor, var majorFactory) = RegisterNewShieldPlating(
            "SturdyShieldPlatingMajor",
            "major",
            16, 8000, 12, 58, 5,
            ModData.ItemNames.SturdyShieldPlatingGreater); // If OP: +8 or +7
        (ModData.ItemNames.SturdyShieldPlatingSupreme, var supremeFactory) = RegisterNewShieldPlating(
            "SturdyShieldPlatingSupreme",
            "supreme",
            19, 32000, 15, 70, 6,
            ModData.ItemNames.SturdyShieldPlatingMajor); // If OP: +10 or +9
        
        Func<ItemName, Item>[] factories = [minorFactory, lesserFactory, moderateFactory, greaterFactory, majorFactory, supremeFactory];

        // Replace plates from other mods
        // (just benefits local instances of MoreShields mod, not workshop installs due to optional-depts)
        var itemFactories = typeof(ModManager)
                .GetProperty("ModdedItemFactories", BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetMethod
                ?.Invoke(null, null)
            as Dictionary<ItemName, Func<ItemName, Item>>;
        if (itemFactories is not null)
        {
            foreach (ItemName plate in AllShieldPlates)
            {
                if (!itemFactories.TryGetValue(plate, out var factory))
                    continue;
                
                itemFactories[plate] = factories[AllShieldPlates.IndexOf(plate)];
            }
        }
        
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
    
    public static (ItemName, Func<ItemName,Item>) RegisterNewShieldPlating(string technicalName, string tier, int level, int price, int bonusHardness, int bonusHP, int fundamentalLevel, ItemName? upgradesFrom, params Trait[] traits)
    {
        Func<ItemName, Item> factory = iName => new Item(
                iName,
                ModData.Illustrations.ShieldPlating,
                "shield plating (" + tier + ")",
                level, price,
                [ModData.ModTrait, Trait.Runestone, ..traits])
            .WithRuneProperties(new RuneProperties(
                    tier + " plated",
                    ModData.RuneKinds.ShieldPlating,
                    "Magically enchanted plating makes the shield much sturdier.",
                    $"The enchanted shield's hardness increases by {bonusHardness} {{i}}(up to {bonusHardness + 6}){{/i}}, and its HP increases by {bonusHP} {{i}}(up to {bonusHP + 12}){{/i}}. For example, a {tier} plated steel shield has a hardness of {5 + bonusHardness} and {10 + bonusHardness} HP instead of 5 and 10.",
                    item1 =>
                    {
                        item1.Hardness = Math.Min(
                            // The base hardness plus the bonus,
                            item1.Hardness + bonusHardness,
                            // cannot exceed the highest of its base hardness
                            // or a Fortress Shield with this bonus
                            Math.Max(item1.Hardness, 6 + bonusHardness));
                    },
                    fundamentalLevel)
                .WithCanBeAppliedTo((_, shield) =>
                {
                    if (shield.Name.Contains("sturdy shield"))
                        return "Shield plating cannot be applied to sturdy shields";
                    if (!shield.HasTrait(Trait.Shield))
                        return "Shield plating can only be applied to shields.";
                    if (shield.Hardness >= 5 + bonusHardness
                        && (shield.GetShieldProperties()?.MaxHP ?? OldShields.GetShieldMaxHP(shield)) >= 12 + bonusHP)
                        return "This item has higher hardness and HP.";
                    return null;
                })
                .WithUpgradesFrom(upgradesFrom))
            .WithItemGreaterGroup(ModData.ItemGreaterGroups.ShieldModifications)
            .WithItemGroup(ModData.Illustrations.ShieldPlating.IllustrationAsIconString + "Plating");
        return (ModManager.RegisterNewItemIntoTheShop(technicalName, factory), factory);
    }
    
    public static Item CreatePlatedShield(ItemName item, ItemName plate)
    {
        ItemModification modificationRune = ItemModification.Create("rune_" + plate.ToStringOrTechnical());
        return Items.CreateNew(item)
            .WithItemGroup($"Plated ({plate.ToStringOrTechnical().Replace("SturdyShieldPlating", "").Replace("2", "").ToLower()}) shields")
            .WithModification(modificationRune);
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
            throw new ArgumentException("You must provide at least one Trait.", nameof(addedTraits));
        string traitDescription =
            humanizedTraits
            + " trait"
            + (humanizedTraits.Contains("and") || humanizedTraits.Contains(',')
                ? "s"
                : null);
        string variantName = technicalName.Replace("ShieldAugmentation", "").ToLower();
        return ModManager.RegisterNewItemIntoTheShop(
            technicalName,
            iName => new Item(
                    iName,
                    ModData.Illustrations.ShieldAugmentation,
                    $"shield augmentation ({variantName})",
                    0, 1,
                    [ModData.ModTrait, Trait.Runestone])
                .WithRuneProperties(new RuneProperties(
                        "augmented",
                        ModData.RuneKinds.ShieldAugmentation,
                        "There are numerous methods to modify shields — snarling rods to catch weapons, bladed edges, padding for nonlethal strikes, and so on — but all share basic functionality.",
                        $"The shield gains the {traitDescription}, and its damage die is reduced from a d6 to a d4.",
                        item =>
                        {
                            item.Traits.AddRange(addedTraits);
                            item.WeaponProperties?.DamageDieSize = DamageDiceUtils.DecreaseDamageDiceByOneStep(item.WeaponProperties.DamageDieSize);
                            item.StateCheckWhenWielded += (cr, item1) =>
                            {
                                if (cr.HasEffect(QEffectId.AgileShieldGrip))
                                    cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                    {
                                        Key = "ShieldAugmentationAgileShieldGripRemover",
                                        AdjustStrikeAction = (qfThis, action) =>
                                        {
                                            if (action.Item?.Traits.Contains(Trait.Shield) != true)
                                                return;
                                            
                                            // Remove the agile trait from Agile Shield Grip.
                                            if (action.HasTrait(Trait.Agile)
                                                && !action.Item.HasTrait(Trait.Agile))
                                                action.Traits.Remove(Trait.Agile);
                                        }
                                    });
                            };
                        })
                    .WithCanBeAppliedTo((_, shield) =>
                        shield.HasTrait(Trait.Shield) ? null : "Shield augmentations can only be applied to shields."))
                .WithItemGreaterGroup(ModData.ItemGreaterGroups.ShieldModifications)
                .WithItemGroup(ModData.Illustrations.ShieldAugmentation.IllustrationAsIconString + "Augmentations"));
    }
}