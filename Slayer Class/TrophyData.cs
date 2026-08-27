using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;

/// <summary>
/// A data class that links together information for a slayer trophy.
/// </summary>
/// <param name="name">The name of the creature this trophy came from. If null or empty, value is "Unknown".</param>
/// <param name="id">The creature's CreatureId.</param>
/// <param name="traits">The list of traits on this trophy.</param>
/// <param name="kinds">The list of damage types on this trophy.</param>
/// <param name="traditions">The list of traditions on this trophy. Must include at least one tradition.</param>
/// <param name="tags">The list of additional miscellaneous tags, such as features it has or other singular properties of like its highest modifier.</param>
public class TrophyData(
    string? name,
    CreatureId? id,
    List<Trait>? traits,
    List<DamageKind>? kinds,
    List<Trait>? traditions,
    List<string>? tags)
{
    /// <summary>
    /// The name of the creature this trophy came from.
    /// </summary>
    public readonly string Name = name ?? "Unknown";
    
    /// <summary>
    /// The CreatureId of the Creature the trophy came from.
    /// </summary>
    public readonly CreatureId Id = id ?? CreatureId.None;
    
    /// <summary>
    /// The traits on the trophy.
    /// </summary>
    public readonly List<Trait> Traits = traits ?? [];
    
    /// <summary>
    /// The damage types on the trophy.
    /// </summary>
    public readonly List<DamageKind> Kinds = kinds ?? [];
    
    /// <summary>
    /// The spell traditions on the trophy.
    /// </summary>
    public readonly List<Trait> Traditions = traditions ?? [];
    
    /// <summary>
    /// The list of additional miscellaneous tags, such as features it has or other singular properties of like its highest modifier.
    /// </summary>
    public readonly List<string> Tags = tags ?? [];

    #region TrophyData from Types

    /// <summary>
    /// Attempt to find the TrophyData on an item that could be linked to a HuntingTool or that is itself a Trophy.
    /// </summary>
    public static implicit operator TrophyData?(Item? toolOrTrophy)
    {
        if (toolOrTrophy is null)
            return null;
        
        // If the item is a trophy, then the trophy is the item.
        // Otherwise, check the item's runes for a trophy item rune.
        Item? trophy = toolOrTrophy.HasTrait(ModData.Traits.Trophy)
            ? toolOrTrophy
            : toolOrTrophy.ActiveRunes.FirstOrDefault(r =>
                r.HasTrait(ModData.Traits.Trophy));

        // Can't find a trophy, therefore no data.
        if (trophy is null)
            return null;
        
        // Look for the TrophyModification on an item that is a known trophy.
        ItemModification? trophyMod = trophy.ItemModifications
            .FirstOrDefault(mod =>
                mod.Kind == Trophies.TrophyModification);
        
        // Get the tag string from the trophy's TrophyModification's Tag.
        if (trophyMod is null
            || trophyMod.Tag is not string tagString)
            return null;

        // Create a new TrophyData instance from a tag string.
        TrophyData data = FromDataString(tagString);
        
        // Last minute check in case this completely fails to parse.
        if ((string.IsNullOrEmpty(data.Name) || data.Name == "Unknown")
            && data.Id is CreatureId.None
            && data.Traits.Count == 0
            && data.Kinds.Count == 0
            && data.Traditions.Count == 0
            && data.Tags.Count == 0)
            return null;
        
        return data;
    }

    /// <summary>
    /// Get the TrophyData that's on one of your HuntingTools from a list of Items to search through.
    /// </summary>
    public static TrophyData? FromHuntingTool(HuntingTool tool, List<Item> inventoryToSearch)
    {
        foreach (Item item in inventoryToSearch
                     .Where(tool.IsMyTool)
                     .ToList())
        {
            TrophyData? attempt = item;
            if (attempt is not null)
                return attempt;
        }

        return null;
    }

    /// <summary>
    /// Transform a creature into TrophyData.
    /// </summary>
    public static TrophyData FromCreature(Creature cr)
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
            // Actions
            ..cr.Possibilities
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                ?.Filter(ap =>
                {
                    // Exclude: Spells, most item-y attacks
                    if (ap.CombatAction.Traits.ContainsOneOf([Trait.Spell, Trait.Bomb, Trait.Elixir, Trait.Potion]))
                        return false;
                    // Exclude some basic buttons
                    if (ap.CombatAction.ActionId is ActionId.Trip or ActionId.DrawItem or ActionId.DropItem or ActionId.PickUpItem or ActionId.Delay)
                        return false;
                    // Exclude more basic and specific abilities with damage-words.
                    if (ap.CombatAction.Name.ToLower() is "drop prone" or "reposition" or "deal 1000 damage" or "fire bomb" or "enter badger rage" or "death-stealing gaze")
                        return false;
                    // Allow everything else.
                    return true;
                })
                .CreateActions(false)
                .SelectMany(ica =>
                {
                    List<DamageKind> kinds = [];
                    if (ica.Action.Item is { WeaponProperties: {} props } item)
                    {
                        kinds.AddRange(item.DetermineDamageKinds());
                        kinds.AddRange(props.AdditionalDamage.Select(set => set.Item2));
                        if (props.AdditionalSplashDamageFormula is not null)
                            kinds.Add(props.AdditionalSplashDamageKind);
                    }
                    else
                    {
                        kinds.AddRange(GetKindsInDescription(ica.Action.Description));
                    }
                    
                    return kinds;
                }) ?? [],
            
            // QEffects
            ..cr.QEffects
                .SelectMany(qf =>
                {
                    // Ignore effects that can't be parsed with
                    if (qf.Description is null)
                        return [];
                    // Ignore effects that aren't about dealing typed damage to other creatures
                    // This also excludes afflictions
                    if (qf.Name?.ToLower() is { } name
                        && new List<string>(["immunity", "resistance", "weakness", "regeneration", "vulnerability", "resilience", "poison", "venom", "rot", "badger rage", "head regrowth", "split", "death-stealing gaze", "aversion", "healing"])
                            .Any(name.Contains))
                        return [];
                    return GetKindsInDescription(qf.Description);
                }),
            
            // Immunities
            ..cr.WeaknessAndResistance.Immunities.Where(dk =>
                dk is not DamageKind.Bleed and not DamageKind.Untyped)
        ];
        types.RemoveDuplicates();
        types = types
            .OrderBy(dk => dk.ToString())
            .ToList();
        
        // Handle various stat block observations as extra tags
        List<string> tags = [];
        if (cr.QEffects.Any(qf =>
                qf is { Id: QEffectId.AllAroundVision, ExpiresAt: ExpirationCondition.Never, Dispellable: null }))
            tags.Add(DataConstants.TAGS_ALL_AROUND_VISION);
        if (cr.QEffects.Any(qf =>
                qf is { Id: QEffectId.FogVision, ExpiresAt: ExpirationCondition.Never, Dispellable: null }))
            tags.Add(DataConstants.TAGS_SMOKE_VISION);
        List<Defense> saves = [Defense.Fortitude, Defense.Reflex, Defense.Will];
        tags.AddRange(saves
            .GroupBy(def => cr.Defenses.GetBaseValue(def))
            .OrderByDescending(grp => grp.Key)
            .First()
            .Select(def => DataConstants.TAGS_HIGHEST_SAVE + def)
        );

        return new TrophyData(
            cr.BaseName,
            cr.CreatureId,
            traits,
            types,
            traditions,
            tags);
        
        List<DamageKind> GetKindsInDescription(string desc)
        {
            List<DamageKind> kinds = [];
            string valid = desc.ToLower();
            string[] allWords = valid.Split([' ', '.', ',', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
            string[] noBeforeWords = ["persistent", "life"];
            string[] noAfterWords = ["energy", "resistance", "resistant", "weakness", "effect", "effects", "curse"];
            foreach (DamageKind kind in DamageKind.GetValues())
            {
                bool found = false;
                if (kind is DamageKind.Bleed)
                    continue;
                string dkStr = kind.ToStringOrTechnical().ToLower();
                for (int i=0; i < allWords.Length; i++)
                {
                    // Must be found
                    if (allWords[i] != dkStr)
                        continue;
                    // Check previous word
                    if (i > 0)
                    {
                        string previous = allWords[i-1];
                        if (noBeforeWords.Contains(previous))
                            continue;
                        // skip errors from certain list-sentences
                        if (previous is "and" or "or")
                        {
                            // "persistent X (and|or) Y" for Y
                            if (i > 2 && allWords[i-3] == "persistent")
                                continue;
                            // "malice and evil" for evil
                            if (i > 1 && allWords[i-2] == "malice")
                                continue;
                        }
                    }
                    // Check next word
                    if (i < allWords.Length-1 && noAfterWords.Contains(allWords[i + 1]))
                        continue;
                    found = true;
                }
                if (found)
                    kinds.Add(kind);
            }

            return kinds;
        }
    }

    /// <summary>
    /// Parses a given trophy data-string and turns it into usable data.
    /// </summary>
    /// <param name="trophyTag">The data-string of the trophy (the string without "trophy_").</param>
    /// <returns>A tuple containing all the trophy's properties.</returns>
    public static TrophyData FromDataString(string trophyTag)
    {
        // Example tag:
        // - quarry*Orc Warrior_crid*OrcWarrior_traits*Chaotic-Evil-Orc-MetalArmor_damagekinds*Slashing_traditions*Occult_Tags*HighestSaveFortitude

        string[] lists = trophyTag.Split(DataConstants.LIST_SEPARATOR);
        
        /*Regex.Replace(
            lists[0]["quarry*".Length..],
            @"(?<=[a-z0-9])(?=[A-Z])",
            " ");*/
        string finalName = GetString(DataConstants.CREATURE_NAME) ?? "Unknown";
        
        CreatureId finalId = ModManager.TryParse(
            GetString(DataConstants.CREATURE_ID)
                ?.Replace(DataConstants.UNDERSCORE_SUBSTITUTE, DataConstants.UNDERSCORE)
            ?? "None",
            out CreatureId iId)
                ? iId
                : CreatureId.None;

        List<Trait>? finalTraits = GetString(DataConstants.TRAITS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .Select(t => ModManager.TryParse(t, out Trait iTrait) ? iTrait : (Trait?)null)
            .Where(t => t.HasValue)
            .Cast<Trait>()
            .ToList();
        
        List<DamageKind>? finalKinds = GetString(DataConstants.DAMAGE_KINDS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .Select(dk => ModManager.TryParse(dk, out DamageKind iDk) ? iDk : (DamageKind?)null)
            .Where(dk => dk.HasValue)
            .Cast<DamageKind>()
            .ToList();
        
        List<Trait>? finalTraditions = GetString(DataConstants.TRADITIONS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .Select(t => ModManager.TryParse(t, out Trait iTrait) ? iTrait : (Trait?)null)
            .Where(t => t.HasValue)
            .Cast<Trait>()
            .ToList();
        
        List<string>? finalTags = GetString(DataConstants.TAGS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .ToList();

        return new TrophyData(finalName, finalId, finalTraits, finalKinds, finalTraditions, finalTags);

        string? GetString(string header)
        {
            string? list = lists.FirstOrDefault(list => list.Contains(header));

            if (list is null || list.Length <= header.Length)
                return null;
            
            return list.Substring(header.Length);
        }
    }

    #endregion

    #region TrophyData to Types
    
    /// <summary>
    /// Turns the trophy data into a data string (the serializable string containing the trophy's data).
    /// </summary>
    /// <remarks>
    /// Used for creating new instances of <see cref="Trophies.TrophyModification"/>.
    /// </remarks>
    /// <returns>The final data-string of the trophy, to be prepended with "trophy_" or added to the ItemModification's Tag.</returns>
    public string ToDataString()
    {
        // Collect all relevant data
        List<string> data = [];
        
        // Name
        string finalName = DataConstants.CREATURE_NAME + (string.IsNullOrEmpty(Name) ? "Unknown" : Name);
        data.Add(finalName);
        
        // Creature Id
        string finalId = (DataConstants.CREATURE_ID + Id.ToStringOrTechnical())
            .Replace(
                DataConstants.UNDERSCORE,
                DataConstants.UNDERSCORE_SUBSTITUTE);
        data.Add(finalId);
        
        // Traits
        List<Trait> exceptTraits = [Trait.Small, Trait.Large, Trait.Huge, Trait.Gargantuan, Trait.Colossal5, Trait.Colossal6, Trait.Colossal7, Trait.Colossal8, Trait.Uncommon, Trait.Unique];
        List<Trait> filteredTraits = Traits.ToList();
        filteredTraits.RemoveAll(t => exceptTraits.Contains(t));
        if (filteredTraits.Count > 0)
        {
            string finalTraits =
                DataConstants.TRAITS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    filteredTraits.Select(t => t.ToStringOrTechnical()));
            data.Add(finalTraits);
        }
        
        // Damage kinds
        if (Kinds.Count > 0)
        {
            string finalKinds =
                DataConstants.DAMAGE_KINDS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    Kinds.Select(t => t.ToStringOrTechnical()));
            data.Add(finalKinds);
        }

        // Magical traditions
        if (Traditions.Count > 0)
        {
            string finalTraditions =
                DataConstants.TRADITIONS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    Traditions.Select(t => t.ToStringOrTechnical()));
            data.Add(finalTraditions);
        }
        
        // Special tags
        if (Tags.Count > 0)
        {
            string finalTags =
                DataConstants.TAGS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    Tags);
            data.Add(finalTags);
        }
        
        // Combine data into a final string
        string finalDataString = string.Join(DataConstants.LIST_SEPARATOR, data);
        
        return finalDataString;
    }

    #endregion
    
    #region String Parsing and De/Serialization

    /// <summary>
    /// The string constants which make up the de/serialized data of a trophy.
    /// </summary>
    public static class DataConstants
    {
        // Design Note: The characters ':' and ',' are illegal to use for trophies.
        
        // Example tag:
        // - quarry*OrcWarrior_traits*Chaotic-Evil-Orc-MetalArmor_damagekinds*Slashing_traditions*Occult

        /// <summary>
        /// Begins the identifier and tag string of the trophy ItemModificationKind.
        /// </summary>
        public const string TROPHY_MODIFICATION = "trophy_";
        
        /// <summary>
        /// Separates each list.
        /// </summary>
        public const char LIST_SEPARATOR = '_';

        /// <summary>
        /// Separates items in a list.
        /// </summary>
        public const char ITEM_SEPARATOR = '-';
        
        /// <summary>
        /// The humanized creature name.
        /// </summary>
        public const string CREATURE_NAME = "quarry*";

        /// <summary>
        /// The creature's CreatureId.
        /// </summary>
        public const string CREATURE_ID = "crid*";

        /// <summary>
        /// The underscore character.
        /// </summary>
        /// <remarks>This character is checked in a creature ID string and substituted for <see cref="UNDERSCORE_SUBSTITUTE"/>.</remarks>
        public const string UNDERSCORE = "_";

        /// <summary>
        /// The character that underscores in creature IDs are substituted for during serialization.
        /// </summary>
        public const string UNDERSCORE_SUBSTITUTE = "%";
        
        /// <summary>
        /// The list of the trophy's traits.
        /// </summary>
        public const string TRAITS = "traits*";

        /// <summary>
        /// The list of the trophy's damage kinds.
        /// </summary>
        public const string DAMAGE_KINDS = "damagekinds*";

        /// <summary>
        /// The list of the trophy's associated traditions.
        /// </summary>
        public const string TRADITIONS = "tradition*";

        /// <summary>
        /// Begins a list of any other special tags.
        /// </summary>
        public const string TAGS = "tags*";

        /// <summary>
        /// The fog/smoke special vision.
        /// </summary>
        public const string TAGS_SMOKE_VISION = "SmokeVision";

        /// <summary>
        /// The All-Around Vision special vision.
        /// </summary>
        public const string TAGS_ALL_AROUND_VISION = "AllAroundVision";

        /// <summary>
        /// The highest saving throw. This tag can appear more than once, with a different save each time. The Defense is added directly to the end of this constant (implicit invocation of ToString()).
        /// </summary>
        public const string TAGS_HIGHEST_SAVE = "HighestSave";
    }

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
#pragma warning disable CS0618 // Type or member is obsolete
        Trait.Mod,
#pragma warning restore CS0618 // Type or member is obsolete
        Trait.MustSurvive,
        Trait.NativeOutsider,
        Trait.NeedNotSurvive,
        Trait.NeverSetsOccupant,
        Trait.NoDeathOverhead,
        Trait.NoDeathScream,
        Trait.NoPhysicalUnarmedAttack,
        Trait.Object,
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

    /// <summary>
    /// Gets the humanized name, description, or entry of a value stored in <see cref="DataConstants.TAGS"/>. This gets a portion of the data from <see cref="ItemModification.Tag"/>, unrelated to the whole tag itself.
    /// </summary>
    public static string TrophyDataTagToString(string tag)
    {
        if (tag == DataConstants.TAGS_ALL_AROUND_VISION)
            return "All-Around Vision";
        if (tag == DataConstants.TAGS_SMOKE_VISION)
            return "Smoke Vision";
        if (tag.Contains(DataConstants.TAGS_HIGHEST_SAVE))
            return Enum.TryParse(tag[DataConstants.TAGS_HIGHEST_SAVE.Length..], true, out Defense defense)
                ? "highest save is " + defense.ToStringOrTechnical().WithColor(defense.ToColor())
                : throw new Exception("Unknown Defense for Data Tag HighestSave: " + tag);
        throw new Exception("Unknown Trophy Data Tag: " + tag);
    }

    #endregion
}