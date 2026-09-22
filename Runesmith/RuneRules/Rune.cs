using System.Diagnostics.CodeAnalysis;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Illustrations;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

// I need this link. A lot.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments

public class Rune
{
    #region Instance Data
    
    /// <summary>The unique trait which corresponds to instances of this particular kind of rune, such as Atryl, Rune of Fire.</summary>
    public RuneId Id { get; }
    
    /// <summary> The original level of the Rune, before it increases with character level. This corresponds to the CHARACTER LEVEL required to learn the Rune.</summary>
    public int BaseLevel  { get; }
    
    /// <summary>The Rune's icon.</summary>
    public Illustration Illustration { get; }
    
    /// <summary>
    /// Gets the traits associated with the rune. By default, all runes have at least the Rune, Runesmith, and Magical traits.
    /// </summary>
    public List<Trait> Traits { get; } = [ModData.Traits.Rune, ModData.Traits.Runesmith, Trait.Magical];

    /// <summary>
    /// Gets whether this rune is a diacritic rune.
    /// </summary>
    public bool IsDiacriticRune => this.HasTrait(ModData.Traits.Diacritic);
    
    public RuneDrawProperties DrawProperties { get; }
    
    public RunePassiveProperties PassiveProperties { get; }
    
    public RuneInvocationProperties InvocationProperties { get; }
    
    /// <summary>
    /// If a rune can be etched onto players or their items (under practical circumstances), this function provides the ContextMenuItem for that option to etch it.
    /// </summary>
    /// <param name="Rune">A self-reference to this rune.</param>
    /// <param name="CalculatedCharacterSheetValues">The player character sheet whose inventory is being inspected.</param>
    /// <param name="Item">(nullable) The item being inspected.</param>
    /// <param name="ContextMenuItem">The context menu option for this rune.</param>
    public Func<Rune, CalculatedCharacterSheetValues, Item?, ContextMenuItem>? EtchOption { get; set; }
    
    #endregion
    
    #region String Properties
    
    /// <summary>
    /// Gets or sets the full name of the rune.
    /// </summary>
    /// <example>"Atryl, Rune of Fire"</example>
    public string Name { get; set; }

    /// <summary>
    /// Gets the base name of the rune.
    /// </summary>
    /// <example>"Atryl"</example>
    public string BaseName => this.Id.ToWord();

    /// <summary>
    /// The unformatted flavor text of the rune.
    /// </summary>
    /// <example>"This cuspate rune, when placed on a blade, ensures it won’t go dull."</example>
    /// <seealso cref="GetFlavorText"/>
    public string? FlavorText
    {
        [Obsolete("Use GetFlavorText instead.")]
        private get;
        set;
    }

    /// <summary>Gets the rune's <see cref="FlavorText"/>, possibly with italics formatting.</summary>
    public string? GetFlavorText(
        [NotNullIfNotNull("alternativeDescription")]
        string? alternativeDescription = null,
        bool withFormatting = true)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        string? flavor = alternativeDescription ?? this.FlavorText;
#pragma warning restore CS0618 // Type or member is obsolete
        if (flavor is null)
            return null;
        return flavor.WithTag(withFormatting ? "i" : null);
    }
    
    /// <summary>
    /// The text describing how the rune changes as level increases.
    /// </summary>
    /// <remarks>
    /// This generally begins with {b}Level (+2){/b}, or lists a specific level of increase such as {b}Level (17th){/b}.
    /// </remarks>
    /// <example><code>
    /// newRune.LevelText = "{b}Level (+2){/b} The damage increases by 1, and the damage of the invocation increases by 2d6."
    /// </code></example>
    public string? LevelText { get; set; }
    
    /// <summary>
    /// The numeric part of the formatted level-up text.
    /// </summary>
    /// <remarks>
    /// Don't use any parentheses.
    /// </remarks>
    /// <example>"+2" or "17th"</example>
    public string? LevelFormat { get; set; }
    
    /// <summary>
    /// Get the rune's <see cref="LevelText"/> with formatting.
    /// </summary>
    /// <returns>(string) The original text prepended with "{b}Level (<see cref="LevelFormat"/>){/b} ".</returns>
    public string? GetFormattedLevelText(string? text = null)
    {
        string? levelText = text ?? (this.LevelText ?? null);
        return levelText != null ? "{b}Level (" + this.LevelFormat + "){/b} " + levelText : null;
    }
    
    #endregion

    #region Methods

    public Rune WithAdjustment(Action<Rune> adjustment)
    {
        adjustment(this);
        return this;
    }
    
    /// <summary>
    /// Checks whether a <see cref="Trait"/> is listed among <see cref="Traits"/>.
    /// </summary>
    public bool HasTrait(Trait trait)
    {
        return this.Traits.Contains(trait);
    }

    /// <summary>
    /// Replaces the generated name with a custom name.
    /// </summary>
    public Rune WithName(string name)
    {
        this.Name = name;
        return this;
    }
    
    /// <summary>Overrides the default <see cref="Traits"/> expected of a Rune to the list given. (Such as if for some reason you need a Rune without the Rune trait.)</summary>
    public Rune WithOverrideTraits(List<Trait> newTraits)
    {
        this.Traits.Clear();
        this.Traits.AddRange(newTraits);
        return this;
    }

    /// <summary>
    /// Sets <see cref="LevelText"/> and <see cref="LevelFormat"/>.
    /// </summary>
    public Rune WithLevelText(string? levelText, string? levelFormat)
    {
        this.LevelText = levelText;
        this.LevelFormat = levelFormat;
        return this;
    }

    public (int BaseValue, int BonusValue, int FinalValue) CalculateHeightening(int baseValue, int levelsPerIncrease, int increasePerLevel, int runesmithLevel)
    {
        int levelDelta = Math.Max(runesmithLevel - this.BaseLevel, 0);
        int numIncreases = levelDelta / levelsPerIncrease;
        int bonusValue = numIncreases * increasePerLevel;
        return (
            baseValue,
            bonusValue,
            baseValue+bonusValue);
    }
    
    #endregion

    #region Initializers

    /// <summary>
    /// Initializes a new Rune object.
    /// </summary>
    /// <param name="runeId">The unique identifier for this rune. This determines the rune's name (this can be changed later by writing to <see cref="Rune.Name"/> or calling <see cref="WithName"/>).</param>
    /// <param name="icon">The illustration of the rune.</param>
    /// <param name="baseLevel">The base level of the Rune, before increasing with character level.</param>
    /// <param name="flavorText">The flavor-text of the Rune.</param>
    /// <param name="drawProperties">This rune's draw properties.</param>
    /// <param name="passiveProperties">This rune's passive effect properties.</param>
    /// <param name="invocationProperties">The rune's invocation properties.</param>
    /// <param name="additionalTraits">(nullable) The list of additional traits associated with the Rune. By default, Runes have the Rune, Runesmith, and Magical traits. To overwrite these, write directly to the Traits field or call <see cref="WithOverrideTraits"/></param>
    public Rune(
        RuneId runeId,
        int baseLevel,
        Illustration icon,
        string flavorText,
        RuneDrawProperties drawProperties,
        RunePassiveProperties passiveProperties,
        RuneInvocationProperties invocationProperties,
        List<Trait>? additionalTraits = null)
    {
        this.Name = runeId.ToFullName();
        this.Id = runeId;
        this.Illustration = icon;
        this.BaseLevel = baseLevel;
        this.FlavorText = flavorText;
        drawProperties.Self = this;
        this.DrawProperties = drawProperties;
        passiveProperties.Self = this;
        this.PassiveProperties = passiveProperties;
        invocationProperties.Self = this;
        this.InvocationProperties = invocationProperties;
        if (additionalTraits != null)
            this.Traits = this.Traits.Concat(additionalTraits).ToList();
    }
    
    #endregion
}