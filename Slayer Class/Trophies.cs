using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Controls;
using Dawnsbury.Display.Notifications;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using SpiritDamage;

namespace Dawnsbury.Mods.SlayerClass;

public static class Trophies
{
    public static ItemName TrophyCase;

    public static ItemName TrophyItem;

    /// <summary>
    /// This ItemModification contains all the data associated with a trophy, allowing for easy serialization and deserialization. A <see cref="TrophyItem"/> instance should always have a TrophyModification instance, and should only have 1 of them.
    /// </summary>
    public static ItemModificationKind TrophyModification;

    /// <summary>
    /// Some hunting tools have a reinforced benefit which requires you to choose one damage type from the ones on the trophy when you Reinforce your Arsenal. This item modification tracks that chosen damage type. 
    /// </summary>
    public static ItemModificationKind ChosenDamageKindModification;

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
        // TODO: Determine if these traits actually must be stored as one of the trophy's traits, or if only the associated traditions are needed.
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
                    "trophy case",
                    0, 0,
                    ModData.Traits.ModName, ModData.Traits.Slayer)
                .WithDescription(
                    "This case of light bulk is used by slayers to hold their unused trophies.",
                    """
                    A slayer's trophy case can hold up to 5 trophies. Out-of-combat, you can drag-and-drop trophies into the case to save inventory space.

                    You can get one or more starting trophies from the shop.
                    """)
                .WithItemGreaterGroup(ModData.ItemGreaterGroups.ClassItems)
                .WithItemGroup("Slayer")
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
                    "trophy",
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
                            : "You can only attach trophies to items designated as one of your hunting tools.")));
        
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
                            {b}Origin{/b} {{tagData.quarryName}}
                            {b}Traits{/b} {{string.Join(", ", tagData.traits.Select(t =>
                                t.ToStringOrTechnical()))}}
                            {b}Damage Types{/b} {{string.Join(", ", tagData.kinds.Select(kind =>
                                kind.ToStringOrTechnical().WithColor(kind.DamageKindToColor())))}}
                            {b}Traditions{/b} {{string.Join(", ", tagData.traditions.Select(trad =>
                                trad.ToStringOrTechnical().WithColor(trad.TraditionTraitToColor())))}}
                            """;
                        item.Traits.AddRange(tagData.traits);
                    }
                };
            });

        ChosenDamageKindModification = ItemModifications.RegisterItemModification(
            "chosenDamageKind_",
            mod =>
                "chosenDamageKind_" + ((DamageKind)mod.Tag).ToString(),
            (tag, modKind) =>
            {
                if (!tag.StartsWith("chosenDamageKind_"))
                    return null;
                string subTag = tag["chosenDamageKind_".Length..];
                if (subTag == "Spirit")
                    return null;
                DamageKind chosenKind = DamageKind.Parse(subTag);
                string bonusText = "\n\n{b}Chosen Damage Type{/b} " + chosenKind.ToStringOrTechnical().WithColor(chosenKind.DamageKindToColor());
                return new ItemModification(modKind)
                {
                    Tag = chosenKind,
                    ModifyItem = item =>
                    {
                        item.Description += bonusText;
                    },
                    UnmodifyItem = item =>
                    {
                        item.Description = item.Description!.Replace(bonusText, "");
                    }
                };
            });
        
        // Trophy modifications
        InventoryContextMenu.Options.Add(new InventoryContextMenuOption((slot, item, inv) =>
        {
            // Options are only to modify a trophy
            if (slot.Item is null || item is null || slot.CharacterSheet is null
                || HuntingTools.GetTools(slot.CharacterSheet.Calculated) is not {} tools)
                return null;

            bool itemIsTrophyItself = item.HasTrait(ModData.Traits.Trophy);
            Item? trophy = itemIsTrophyItself
                ? item
                : Trophies.GetTrophy(item);

            if (trophy is null)
                return null;
            
            List<ContextMenuItem> options = [];
            HuntingTools.ToolId? specificTool = HuntingTools.GetToolId(item);
            
            // Damage Kind selections (all):
            // - (Signature) Bloodseeking Blade
            // - (Signature) Warded Mail
            // - (Secondary) Paired Bloodseeker, as Bloodseeking Blade
            // - (Secondary) Spirit Oil
            if (itemIsTrophyItself
                || specificTool is HuntingTools.ToolId.BloodseekingBlade or HuntingTools.ToolId.WardedMail or HuntingTools.ToolId.PairedBloodseeker or HuntingTools.ToolId.SpiritOil)
            {
                DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                foreach (DamageKind dk in Trophies.GetTrophyData(trophy).kinds)
                    SetDamageKind(dk, dk == chosenKind);
            }
            
            // Damage Kind selections (non-physical):
            // - (Signature) Chymist's Vials (only non-physical types)
            // - (Secondary) Bloodburst Phial (only non-physical types)
            if (itemIsTrophyItself
                || specificTool is HuntingTools.ToolId.ChymistsVials or HuntingTools.ToolId.BloodburstPhial)
            {
                DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                foreach (DamageKind dk in Trophies.GetTrophyData(trophy).kinds)
                    SetDamageKind(dk, dk == chosenKind, true);
            }
            
            // When the user is right-clicking a trophy, only allow them to remove ItemModifications.
            // They must be right-clicking on a hunting tool with an attached trophy to set choices.
            if (itemIsTrophyItself)
                options.RemoveAll(opt => opt.Name.StartsWith("Reinforce"));
            
            return options.Count > 0
                ? options.ToArray()
                : null;

            void SetDamageKind(DamageKind dk, bool alreadySelected, bool noPhysical = false)
            {
                string select = (alreadySelected ? "Unr" : "R") + "einforce";
                string dName = dk.ToStringOrTechnical();
                
                options.Add(new ContextMenuItem(
                    IllustrationName.PersistentDamage,
                    select + ": " + dName + " damage",
                    select + " this Arsenal with " + dName.ToLower() + $" damage for the purposes of this tool's reinforced benefits that require you to choose a{(noPhysical ? " non-physical " : " ")}damage type.",
                    () =>
                    {
                        Trophies.SetChosenDamageKind(trophy, alreadySelected ? null : dk);
                        /*if (!itemIsTrophyItself)
                        {
                            Item newBaseItem = RunestoneRules.RecreateWithUnattachedSubitem(item, trophy, true);
                            RunestoneRules.AddRuneTo(trophy, newBaseItem);
                            //RunestoneRules.AttachSubitem(trophy, newBaseItem);
                            //item = newBaseItem;
                            slot.ReplaceSelf(newBaseItem);
                        }*/
                        /*foreach (GamePhase phase in Root.PhaseStack)
                        {
                            if (phase is CharacterBuilderPhase characterBuilderPhase)
                                characterBuilderPhase.RefreshPlan();
                        }*/
                        /*if (!itemIsTrophyItself && specificTool.HasValue)
                        {
                            HuntingTool tool = tools.First(tool => tool.Id == specificTool.Value);
                            tool.UndesignateAsTool(item);
                            tool.DesignateAsTool(item);
                        }*/
                        Sfxs.Play(SfxName.ReactionQuestion);
                        Toasts.CreateNew(
                            "{b}Technical Limitation{/b}\nYou must detach and reattach this trophy to apply your changes.",
                            Color.FromNonPremultiplied(252, 199, 214, byte.MaxValue),
                            Color.Black,
                            Root.Mouse_NewState_Update.Position + new Point(0, 0));
                    }));
            }
        }));

        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            List<Item> startingTrophies =
            [
                CreateTrophy(TrophyDataToString(
                        "{i}something unspeakable{/i}",
                        [Trait.Aberration],
                        [DamageKind.Cold],
                        [Trait.Occult]))
                    .With(item => item.Nickname = "starting trophy (horrifying aberration)"),
                CreateTrophy(TrophyDataToString(
                        "{i}an electric beast{/i}",
                        [Trait.Beast],
                        [DamageKind.Electricity],
                        [Trait.Primal]))
                    .With(item => item.Nickname = "starting trophy (sparking beast)"),
                CreateTrophy(TrophyDataToString(
                        "{i}a fiery dragon{/i}",
                        [Trait.Dragon],
                        [DamageKind.Fire],
                        [Trait.Arcane]))
                    .With(item => item.Nickname = "starting trophy (flame dragon)"),
                CreateTrophy(TrophyDataToString(
                        "{i}an icy giant{/i}",
                        [Trait.Giant],
                        [DamageKind.Cold],
                        [Trait.Primal]))
                    .With(item => item.Nickname = "starting trophy (frost giant)"),
                CreateTrophy(TrophyDataToString(
                        "{i}a ghostly undead{/i}",
                        [Trait.Undead],
                        [DamageSpirit.Spirit],
                        [Trait.Divine]))
                    .With(item => item.Nickname = "starting trophy (ghostly undead)"),
            ];
            foreach (Item trophy in startingTrophies)
                trophy
                    //.With(item => item.Traits.Add(Trait.DoNotAddToCampaignShop))
                    .WithItemGreaterGroup(ModData.ItemGreaterGroups.ClassItems)
                    .WithItemGroup("Slayer");
            Items.ShopItems.AddRange(startingTrophies);
        };
    }

    #region String Parsing and De/Serialization

    /// <summary>
    /// Turns a given set of data into a string. Used for creating new instances of <see cref="TrophyModification"/>.
    /// </summary>
    /// <param name="quarryName">The name of the creature this trophy came from.</param>
    /// <param name="traits">The list of traits on this trophy.</param>
    /// <param name="kinds">The list of damage types on this trophy.</param>
    /// <param name="traditions">The list of traditions on this trophy. Must include at least one tradition.</param>
    /// <returns>The final data-string of the trophy, to be prepended with "trophy_" or added to the ItemModification's Tag.</returns>
    private static string TrophyDataToString(
        string quarryName,
        List<Trait> traits,
        List<DamageKind> kinds,
        List<Trait> traditions)
    {
        string finalQuarry = "quarry*" + quarryName/*.Replace(" ", "")*/;
        List<Trait> filteredTraits = traits.ToList();
        filteredTraits.RemoveAll(t => t is Trait.Small or Trait.Large or Trait.Huge or Trait.Gargantuan or Trait.Colossal5 or Trait.Colossal6 or Trait.Colossal7 or Trait.Colossal8 or Trait.Uncommon or Trait.Unique);
        string finalTraits = "traits*" + string.Join('-', filteredTraits.Select(t => t.ToString()));
        string finalKinds = "damagekinds*" + string.Join('-', kinds.Select(t => t.ToString()));
        string finalTraditions =  "traditions*" + string.Join('-', traditions.Select(t => t.ToString()));
        
        string tag = finalQuarry + "_" + finalTraits + "_" + finalKinds + "_" + finalTraditions;

        return tag;
    }
    
    /// <summary>
    /// Parses a given trophy data-string and turns it into usable data.
    /// </summary>
    /// <param name="trophyTag">The data-string of the trophy (the string without "trophy_").</param>
    /// <returns>A tuple containing all the trophy's properties.</returns>
    private static (
        string quarryName,
        List<Trait> traits,
        List<DamageKind> kinds,
        List<Trait> traditions)
        TrophyStringToData(string trophyTag)
    {
        // Example tag:
        // - quarry*OrcWarrior_traits*Chaotic-Evil-Orc-MetalArmor_damagekinds*Slashing_traditions*Occult

        string[] lists = trophyTag.Split('_');

        string quarry = /*Regex.Replace(
            lists[0]["quarry*".Length..],
            @"(?<=[a-z0-9])(?=[A-Z])",
            " ");*/
            lists[0]["quarry*".Length..];
        
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
    
    /// <summary>
    /// Turns a creature into its associated trophy data-string.
    /// </summary>
    private static string CreatureToTrophyString(Creature cr)
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

    #endregion

    #region Creating Trophies

    private static Item CreateTrophy(string trophyData)
    {
        if (trophyData.StartsWith("trophy_"))
            trophyData = trophyData.Remove(0, "trophy_".Length);
        Item trophy = Items.CreateNew(Trophies.TrophyItem)
            .WithModification(ItemModification.Create("trophy_" + trophyData));
        return trophy;
    }

    public static Item CreateTrophy(Creature cr)
    {
        return CreateTrophy(CreatureToTrophyString(cr));
    }
    
    public static Item CreateStartingTrophy()
    {
        Item trophy = Trophies.CreateTrophy(
            Trophies.TrophyDataToString(
                "Unknown",
                [Trait.Undead],
                [DamageSpirit.Spirit],
                [Trait.Divine]));
        trophy.ProsaicName = "Starting Trophy";
        return trophy;
    }

    #endregion

    public static Item? GetTrophy(Item item)
    {
        return item.ActiveRunes.FirstOrDefault(r => r.HasTrait(ModData.Traits.Trophy));
    }
    
    public static (string quarryName, List<Trait> traits, List<DamageKind> kinds, List<Trait> traditions)
        GetTrophyData(Item trophy)
    {
        ItemModification? trophyMod = trophy.ItemModifications.FirstOrDefault(mod => mod.Kind == TrophyModification);
        if (trophyMod is null)
            throw new NullReferenceException("Item does not have an ItemModification with a Kind of TrophyModification");
        if (trophyMod.Tag is not string tagString)
            throw new InvalidCastException("TrophyModification.Tag cannot be cast to string");
        return TrophyStringToData(tagString);
    }

    public static DamageKind? GetChosenDamageKind(Item trophy)
    {
        var chosenDamage = trophy.ItemModifications.FirstOrDefault(mod =>
            mod.Kind == ChosenDamageKindModification);
        return chosenDamage?.Tag is DamageKind tag
            ? tag
            : null;
    }

    /// <summary>
    /// Applies a <see cref="ChosenDamageKindModification"/> to the trophy of the given <see cref="DamageKind"/>. Removes all other such modifcations (only 1 at a time).
    /// </summary>
    public static void SetChosenDamageKind(Item trophy, DamageKind? kind)
    {
        foreach (ItemModification mod in trophy.ItemModifications
                     .Where(mod =>
                         mod.Kind == ChosenDamageKindModification)
                     .ToList())
            trophy.WithoutModification(mod);
        
        if (kind is not null)
            trophy.WithModification(ItemModification.Create("chosenDamageKind_" + kind.Value.ToString()));
    }
}