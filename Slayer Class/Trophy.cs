using System.Text.RegularExpressions;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using SpiritDamage;

namespace Dawnsbury.Mods.SlayerClass;

public static class Trophy
{
    public static ItemName TrophyCase;

    public static ItemName TrophyItem;

    public static ItemModificationKind TrophyModification;

    public static readonly List<Trait> TraitBlacklist = [
        // Size traits
        /*Trait.Tiny,*/
        Trait.Small,
        Trait.Large,
        Trait.Huge,
        Trait.Gargantuan,
        Trait.Colossal5,
        Trait.Colossal6,
        Trait.Colossal7,
        Trait.Colossal8,
        // Rarity traits
        Trait.Uncommon,
        /*Trait.Rare,*/
        Trait.Unique,
        // Various technical traits
        Trait.AnimatedObject,
        Trait.AssumesDirectControl,
        Trait.BasicallyNeverWantsToMakeBasicUnarmedStrike,
        Trait.BecomesVisibleCorpseOnDeath,
        Trait.DoesNotBreathe,
        Trait.Farmer,
        Trait.Female,
        Trait.Homebrew,
        Trait.Indestructible,
        Trait.Male,
        Trait.MetalArmor,
        Trait.MetalArmorInvisible,
        Trait.Mod,
        Trait.MustSurvive,
        Trait.NativeOutsider,
        Trait.NeverSetsOccupant,
        Trait.NoDeathOverhead,
        Trait.NoDeathScream,
        Trait.NoPhysicalUnarmedAttack,
        Trait.Object,
        Trait.Outsider,
        Trait.PossessedChild,
        Trait.Pseudocreature,
        Trait.ThirdParty,
        Trait.Trap,
        Trait.UnimportantForVictoryCondition,
        // Nonsense traits to obviously exclude
        Trait.Summoned,
        Trait.NonSummonable,
        Trait.Conjuration,
        // Tradition traits (stored elsewhere on a trophy)
        Trait.Arcane,
        Trait.Divine,
        Trait.Primal,
        Trait.Occult,
    ];

    public static void Load()
    {
        TrophyCase = ModManager.RegisterNewItemIntoTheShop(
            ModData.IdPrepend + "TrophyCase",
            iN => new Item(
                    iN, 
                    ModData.Illustrations.TrophyCase,
                    "Trophy Case",
                    0, 0,
                    ModData.Traits.ModName, ModData.Traits.Slayer)
                .WithDescription(
                    "This case of light bulk is used by slayers to hold their unused trophies.",
                    "A slayer's trophy case can hold 5 unused trophies at a time. Out-of-combat, you can drag-and-drop trophies into the case to reduce inventory space.\n\nYour first trophy case comes with a starting trophy.")
                .WithStoresItem((tCase, trophy) =>
                {
                    if (!trophy.HasTrait(ModData.Traits.Trophy))
                        return "You can only store trophies in a trophy case";
                    if (tCase.StoredItems.Count >= 5)
                        return "You can only hold 5 trophies at a time";
                    return null;
                }));
        
        TrophyItem = ModManager.RegisterNewItemIntoTheShop(
            ModData.IdPrepend + "Trophy",
            iN => new Item(
                    iN,
                    ModData.Illustrations.Trophy,
                    "Trophy",
                    0, 0,
                    ModData.Traits.ModName, ModData.Traits.Trophy, Trait.DoNotAddToShop)
                /*.WithDescription(
                    "The nature of your trophies varies depending on your quarry, but each has certain common characteristics, determined by the creature from which it was claimed.",
                    "You can Reinforce your Arsenal by dragging-and-dropping this trophy onto one of your hunting tools. Other slayer abilities refer to the following properties when determining the effects of being reinforced with this trophy:")*/
                .WithRuneProperties(new RuneProperties(
                        "reinforced",
                        ModData.RuneKinds.SlayerTrophy,
                        "The nature of your trophies varies depending on your quarry, but each has certain common characteristics, determined by the creature from which it was claimed.",
                        "Your hunting tool gains benefits from your features based on this trophy's properties.",
                        (rune, baseItem) =>
                        {
                            
                        })
                    .WithCanBeAppliedTo((rune, baseItem) =>
                        HuntingTools.IsATool(baseItem)
                            ? null
                            : "Not a hunting tool")));
        
        TrophyModification = ItemModifications.RegisterItemModification(
            "trophy",
            mod =>
                "trophy_" + mod.Tag,
            (tag, modKind) =>
            {
                // Not my modification
                if (!tag.StartsWith("trophy_"))
                    return null;
                string subTag = tag["trophy_".Length..];
                return new ItemModification(modKind)
                {
                    Tag = subTag,
                    ModifyItem = item =>
                    {
                        var tagData = TrophyStringToData(subTag);
                    
                        item.Description += "\n\n" +
                            $$"""
                            {b}Collected From{/b} {{tagData.quarryName}}
                            {b}Traits{/b} {{S.ConstructOrList(tagData.traits.Select(t => t.ToStringOrTechnical()), "")}}
                            {b}Damage Types{/b} {{S.ConstructOrList(tagData.kinds.Select(kind => kind.ToStringOrTechnical()), "")}}
                            {b}Traditions{/b} {{S.ConstructOrList(tagData.traditions.Select(trads => trads.ToStringOrTechnical()), "")}}
                            """;
                        item.Traits.AddRange(tagData.traits);
                    }
                };
            });

        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            Item? trophyCase = Items.ShopItems.FirstOrDefault(item => item.ItemName == TrophyCase);
            trophyCase?.StoredItems.Add(CreateStartingTrophy());
        };
    }

    public static Item CreateStartingTrophy()
    {
        Item trophy = Items.CreateNew(Trophy.TrophyItem)
            .WithModification(ItemModification.Create(
                "trophy_" +
                Trophy.TrophyDataToString(
                    "Unknown",
                    [Trait.Undead],
                    [DamageSpirit.Spirit],
                    [Trait.Divine])));
        trophy.ProsaicName = "Starting Trophy";
        return trophy;
    }

    public static string CreatureToTrophyString(Creature cr)
    {
        List<Trait> traits = cr.Traits
            .Except(TraitBlacklist)
            .ToList();
        
        List<Trait> traditions = [
            ..cr.Traits.Where(trait =>
                trait is Trait.Arcane or Trait.Divine or Trait.Primal or Trait.Occult),
            ..cr.Spellcasting?.Sources.Select(src =>
                src.SpellcastingTradition) ?? []
        ];
        traditions.RemoveDuplicates();
        if (traditions.Count == 0)
            traditions.Add(Trait.Occult);

        List<DamageKind> types = [
            // Strikes
            ..cr.Possibilities
                ?.Filter(ap => ap.CombatAction.HasTrait(Trait.Strike))
                .CreateActions(false)
                .SelectMany(ca =>
                {
                    /*string desc = (ca.Action.ShortDescription ?? ca.Action.Description).ToLower();
                    if (string.IsNullOrEmpty(desc))
                        return [];
                    List<DamageKind> kinds = [];
                    foreach (var dk in DamageKind.GetValues())
                        if (desc.Contains(dk.ToStringOrTechnical().ToLower()))
                            kinds.Add(dk);
                    return kinds;*/
                    
                    if (ca.Action.Item is not { } item
                        || item.WeaponProperties is null)
                        return [];
                    
                    List<DamageKind> kinds = [
                        ..item.DetermineDamageKinds(),
                        ..item.WeaponProperties.AdditionalDamage.Select(set => set.Item2),
                        item.WeaponProperties.AdditionalSplashDamageKind,
                    ];
                    return kinds;
                }) ?? [],
            
            // TODO: Damage types from non-spellcasting abilities
            // ..cr.,
            
            // Immunities
            ..cr.WeaknessAndResistance.Immunities.Where(dk =>
                dk is not DamageKind.Bleed and not DamageKind.Untyped)
        ];
        types.RemoveDuplicates();
        
        return TrophyDataToString(
            cr.CreatureId == CreatureId.None
                ? "Unknown"
                : cr.CreatureId.ToStringOrTechnical(),
            traits, types, traditions);
    }

    public static string TrophyDataToString(
        string quarryName,
        List<Trait> traits,
        List<DamageKind> kinds,
        List<Trait> traditions)
    {
        string finalQuarry = "quarry*" + quarryName.Replace(" ", "");
        List<Trait> filteredTraits = traits.ToList();
        filteredTraits.RemoveAll(t => t is Trait.Small or Trait.Large or Trait.Huge or Trait.Gargantuan or Trait.Colossal5 or Trait.Colossal6 or Trait.Colossal7 or Trait.Colossal8 or Trait.Uncommon or Trait.Unique);
        string finalTraits = "traits*" + string.Join('-', filteredTraits.Select(t => t.ToString()));
        string finalKinds = "damagekinds*" + string.Join('-', kinds.Select(t => t.ToString()));
        string finalTraditions =  "traditions*" + string.Join('-', traditions.Select(t => t.ToString()));
        
        string tag = finalQuarry + "_" + finalTraits + "_" + finalKinds + "_" + finalTraditions;

        return tag;
    }

    public static (string quarryName, List<Trait> traits, List<DamageKind> kinds, List<Trait> traditions)
        TrophyStringToData(string trophyTag)
    {
        // Example tag:
        // - quarry*OrcWarrior_traits*Chaotic-Evil-Orc-MetalArmor_damagekinds*Slashing_traditions*Occult

        string[] lists = trophyTag.Split('_');

        string quarry = Regex.Replace(
            lists[0]["quarry*".Length..],
            @"(?<=[a-z0-9])(?=[A-Z])",
            " ");
        
        List<Trait> traits = lists[1]["traits*".Length..]
            .Split('-')
            .Select(Trait.Parse)
            .ToList();
        
        List<DamageKind> kinds = lists[2]["damagekinds*".Length..]
            .Split('-')
            .Select(DamageKind.Parse)
            .ToList();
        
        List<Trait> traditions = lists[3]["traditions*".Length..]
            .Split('-')
            .Select(Trait.Parse)
            .ToList();

        return (quarry, traits, kinds, traditions);
    }
}