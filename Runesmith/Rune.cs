using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

// I need this link. A lot.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments

public class Rune
{
    #region Core Properties
    /// <summary>The unique trait which corresponds to instances of this particular kind of rune, such as Atryl, Rune of Fire.</summary>
    public Trait RuneId { get; set; }
    
    /// <summary> The original level of the Rune, before it increases with character level. This corresponds to the CHARACTER LEVEL required to learn the Rune.</summary>
    public int BaseLevel  { get; set; }
    
    /// <summary>The Rune's icon.</summary>
    public Illustration Illustration { get; set; }
    
    /// <summary>The traits associated with the rune. By default, all runes have at least the Rune, Runesmith, and Magical traits.</summary>
    public List<Trait> Traits { get; set; } = [ModData.Traits.Rune, ModData.Traits.Runesmith, Trait.Magical];

    /// <summary>
    /// A very-abstract list of traits to associate with attempts to draw runes.<br></br>
    /// <list type="bullet"><listheader>Documented Uses:</listheader>
    /// <item><see cref="Trait.IsHostile"/> is used when looking for a rune whose passive effect is negative, such as to filter out options with Runic Tattoo.</item>
    /// <item><see cref="Trait.Shield"/> is used by Fortifying Knock when looking for a rune that is traced on a shield.</item>
    /// </list>
    /// </summary>
    public List<Trait> DrawTechnicalTraits { get; set; } = [];
    
    /// <summary>
    /// A very-abstract list of traits to associate with attempts to invoke runes.<br></br>
    /// <list type="bullet"><listheader>Documented Uses:</listheader>
    /// <item><see cref="Trait.IsHostile"/> is used when looking for a rune whose invocation deals damage, such as by Runic Reprisal.</item>
    /// <item><see cref="Trait.Reflex"/>/<see cref="Trait.Fortitude"/>/<see cref="Trait.Will"/> is used to tell <see cref="CreateInvokeAction"/> to generate a roll breakdown of that type.</item>
    /// <item><see cref="Trait.DoesNotRequireAttackRollOrSavingThrow"/> indicates that the initial invocation doesn't make a saving throw, to be used with a saving throw it does make at some point (such as Esvadir, which invokes from the target onto an adjacent creature).</item>
    /// </list>
    /// </summary>
    public List<Trait> InvokeTechnicalTraits { get; set; } = [];

    /// <summary>
    /// The lambda which determines whether a target is valid according to a Rune's usage. (This acts as the functional implementation of <see cref="UsageText"/>.) If no function is supplied, then the rune is always usable (highly permissive targeting requirements like "drawn on a creature").
    /// </summary>
    /// <param name="Creature">This is the CASTER of the Rune.</param>
    /// <param name="Creature">This is the TARGET of the Rune.</param>
    /// <returns> (Usability) The Usability outcomes.</returns>
    public Func<Creature, Creature, Usability>? UsageCondition { get; set; } = (_, _) => Usability.Usable;
    
    /// <summary>
    /// A lambda that creates and returns a new <see cref="DrawnRune"/>, usually the passive effects on a rune-bearer. This is called by actions to get the DrawnRune representing the effects of a rune being placed.
    /// </summary>
    /// <param name="CombatAction">The CombatAction which is applying the effect (nullable).</param>
    /// <param name="Creature">The CASTER which is applying the Rune.</param>
    /// <param name="Creature">The TARGET which will bear the Rune's DrawnRune.</param>
    /// <param name="Rune">The Rune representing this DrawnRune.</param>
    /// <returns>(DrawnRune) The DrawnRune this lambda generates.</returns>
    public Func<CombatAction, Creature, Creature, Rune, Task<DrawnRune?>>? NewDrawnRune { get; set; }
    
    /// <summary>
    /// <para>An asynchronous lambda that executes the logic of a Rune's invocation effects. Each type of Rune should have its own InvocationBehavior set, such as the Atryl instance of Rune, whose InvocationBehavior forces a target to make a basic Fortitude save against Fire damage. CombatActions and other code which executes InvocationBehavior figure out the other nuances.</para>
    /// <para>Before the execution of logic, the behavior needs to check if the TARGET is immune to this with <see cref="IsImmuneToThisInvocation"/>, otherwise the effects are not applied (but the DrawnRune is still removed).</para>
    /// <para>After the general execution logic completes, <see cref="RemoveDrawnRune"/> should be called to remove the invoked rune, and then a call to <see cref="ApplyImmunity"/> on the TARGET. The action using InvocationBehavior should call <see cref="Rune.RemoveAllImmunities"/> when the action completes.</para>
    /// </summary>
    /// <param name="CombatAction">The <see cref="CombatAction"/> INVOKING the rune.</param>
    /// <param name="Rune">The <see cref="Rune"/> whose DrawnRune is being INVOKED.</param>
    /// <param name="Creature">The CASTER <see cref="Creature"/> INVOKING the rune.</param>
    /// <param name="Creature">The TARGET <see cref="Creature"/> of the INVOCATION.</param>
    /// <param name="DrawnRune">The <see cref="DrawnRune"/> representing the rune being INVOKED.</param>
    public Func<CombatAction, Rune, Creature, Creature, DrawnRune, Task>? InvocationBehavior { get; set; }
    #endregion
    
    #region String Properties
    /// <summary>The name of the rune, such as "Atryl, Rune of Fire".</summary>
    public string Name { get; set; }

    /// <summary>Returns the base name of the rune, such as "Atryl".</summary>
    public string BaseName => this.RuneId.ToStringOrTechnical();

    /// <summary>The unformatted text describing the usage of the rune, such as "drawn on a shield".</summary>
    public string UsageText { get; set; }

    /// <summary>Gets the rune's <see cref="UsageText"/> with bolded formatting.</summary>
    /// <returns>(string) The original text prepended with "{b}Usage{/b} ".</returns>
    public string WithUsageTextFormatting(string? text = null)
    {
        return "{b}Usage{/b} " + (text ?? this.UsageText);
    }
    
    /// <summary>The unformatted flavor text of the rune, such as "This serrated rune, when placed on a blade, ensures it will never go dull."</summary>
    public string? FlavorText { get; set; }

    /// <summary>Gets the rune's <see cref="FlavorText"/> with italics formatting.</summary>
    public string WithFlavorTextFormatting(string? text = null)
    {
        return "{i}" + (text ?? this.FlavorText) + "{/i}";
    }
    
    /// <summary>The text describing the passive behavior of the rune, such as "A shield bearing this rune increases its circumstance bonus to AC by 1."</summary>
    public string PassiveText { get; set; }
    
    /// <summary>
    /// Every Rune defines how it displays its text when heightened. Unless otherwise defined, this lambda returns PassiveText by default.
    /// </summary>
    /// <param name="Rune">The rune's text to use.</param>
    /// <param name="int">The CHARACTER LEVEL to heighten the rune to.</param>
    /// <returns>(string) The PassiveText with heightened behavior.</returns>
    /// <seealso cref="Dawnsbury.Display.Text.S"/>
    public Func<Rune, int, string> PassiveTextWithHeightening { get; set; } =
        (thisRune, level) => thisRune.PassiveText;
    
    /// <summary>
    /// The unformatted text describing the invocation behavior of the rune.
    /// <code>
    /// newRune.InvocationText = "The preliminary streaks of lightning braid together into a powerful bolt. The rune-bearer takes 2d6 electricity damage, with a basic Fortitude save."
    /// </code>
    /// </summary>
    public string? InvocationText { get; set; }
    
    /// <summary>
    /// Gets the rune's <see cref="InvocationText"/> with formatting.
    /// </summary>
    /// <returns>(string) The original text prepended with "{b}Invocation{/b} ".</returns>
    public string? WithInvocationTextFormatting(string? text = null)
    {
        string? invocationText = text ?? (this.InvocationText ?? null);
        return invocationText != null ? "{b}Invocation{/b} " + invocationText : null;
    }
    
    /// <summary>
    /// Every Rune defines how it displays its text when heightened. Unless otherwise defined, this lambda returns InvocationText by default.
    /// </summary>
    /// <param name="Rune">The rune's text to use.</param>
    /// <param name="int">The CHARACTER LEVEL to heighten the rune to.</param>
    /// <returns>(string) The InvocationText with heightened behavior.</returns>
    /// <seealso cref="Dawnsbury.Display.Text.S"/>
    public Func<Rune, int, string?> InvocationTextWithHeightening { get; set; } =
        (thisRune, level) => thisRune.InvocationText;
    
    /// <summary>
    /// The text describing how the rune changes as level increases. This generally begins with {b}Level (+2){/b}, or lists a specific level of increase such as {b}Level (17th){/b}.
    /// <code>
    /// newRune.LevelText = "{b}Level (+2){/b} The damage increases by 1, and the damage of the invocation increases by 2d6."
    /// </code>
    /// </summary>
    public string? LevelText { get; set; }
    
    /// <summary>The numeric part of the formatted level-up text. E.g. "+2" or "17th". Don't use any parentheses.</summary>
    public string? LevelFormat { get; set; }
    
    /// <summary>
    /// Get the rune's <see cref="LevelText"/> with formatting.
    /// </summary>
    /// <returns>(string) The original text prepended with "{b}Level (<see cref="LevelFormat"/>){/b} ".</returns>
    public string? WithLevelTextFormatting(string? text = null)
    {
        string? levelText = text ?? (this.LevelText ?? null);
        return levelText != null ? "{b}Level (" + this.LevelFormat + "){/b} " + levelText : null;
    }
    #endregion

    #region Methods
    /// <summary>Checks whether a Trait is listed among the Rune's Traits. This searches both its Traits field and its RuneId field.</summary>
    /// <param name="trait">The Trait to find.</param>
    /// <returns>(bool) Returns true if the trait was found.</returns>
    public bool HasTrait(Trait trait)
    {
        return this.Traits.Contains(trait) || trait == this.RuneId;
    }
    
    /// <summary>Overrides the default <see cref="Traits"/> expected of a Rune to the list given. (Such as if for some reason you need a Rune without the Rune trait.)</summary>
    public Rune WithOverrideTraits(List<Trait> newTraits)
    {
        this.Traits = newTraits;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="UsageCondition"/> on the Rune instance.
    /// </summary>
    /// <param name="condition"></param>
    public Rune WithUsageCondition(Func<Creature, Creature, Usability> condition)
    {
        this.UsageCondition = condition;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="NewDrawnRune"/> on the Rune instance.
    /// </summary>
    /// <returns>The Rune being modified.</returns>
    public Rune WithNewDrawnRune(Func<CombatAction, Creature, Creature, Rune, Task<DrawnRune?>> drawRuneLambda)
    {
        this.NewDrawnRune = drawRuneLambda;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="InvocationBehavior"/> on the Rune instance.
    /// </summary>
    /// <returns>The Rune being modified.</returns>
    public Rune WithInvocationBehavior(Func<CombatAction, Rune, Creature, Creature, DrawnRune, Task>? newInvocationBehavior )
    {
        this.InvocationBehavior = newInvocationBehavior;
        return this;
    }

    /// <summary>Adds Trait.Shield to DrawTechnicalTraits, which indicates to other parts of the mod that the rune is drawn onto a shield.</summary>
    public Rune WithDrawnOnShieldTechnical()
    {
        this.DrawTechnicalTraits = this.DrawTechnicalTraits.Concat([Trait.Shield]).ToList();
        return this;
    }
    
    /// <summary>Adds Enums.Traits.Rune to DrawTechnicalTraits, which indicates to other parts of the mod that the rune is drawn onto a rune.</summary>
    public Rune WithDrawnOnRuneTechnical()
    {
        this.DrawTechnicalTraits = this.DrawTechnicalTraits.Concat([ModData.Traits.Rune]).ToList();
        return this;
    }

    /// <summary>Adds Trait.IsHostile to DrawTechnicalTraits, which indicates to other parts of the mod that the passive is detrimental to the bearer.</summary>
    public Rune WithDetrimentalPassiveTechnical()
    {
        this.DrawTechnicalTraits = this.DrawTechnicalTraits.Concat([Trait.IsHostile]).ToList();
        return this;
    }
    
    /// <summary>Adds Trait.IsHostile to InvokeTechnicalTraits, which indicates to other parts of the mod that the invocation deals damage when invoked.</summary>
    public Rune WithDamagingInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.IsHostile]).ToList();
        return this;
    }
    
    /// <summary>Adds Trait.Splash to InvokeTechnicalTraits, which indicates to other parts of the mod that the invocation deals damage in an area when invoked.</summary>
    public Rune WithDamagingAreaInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Splash]).ToList();
        return this;
    }

    /// <summary>Adds Trait.Fortitude to InvokeTechnicalTraits, which indicates a fortitude save roll breakdown before invoking the rune.</summary>
    public Rune WithFortitudeSaveInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Fortitude]).ToList();
        return this;
    }

    /// <summary>Adds Trait.Reflex to InvokeTechnicalTraits, which indicates a reflex save roll breakdown before invoking the rune.</summary>
    public Rune WithReflexSaveInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Reflex]).ToList();
        return this;
    }

    /// <summary>Adds Trait.Will to InvokeTechnicalTraits, which indicates a will save roll breakdown before invoking the rune.</summary>
    public Rune WithWillSaveInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Will]).ToList();
        return this;
    }
    /// <summary>Adds Adds Trait.DoesNotRequireAttackRollOrSavingThrow to InvokeTechnicalTraits, which indicates the initial invocation target doesn't have a saving throw.</summary>
    public Rune WithTargetDoesNotSaveTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.DoesNotRequireAttackRollOrSavingThrow]).ToList();
        return this;
    }
    #endregion

    #region Initializers
    /// <summary>
    /// Initializes a new Rune object.
    /// </summary>
    /// <param name="name">Name of the Rune.</param>
    /// <param name="icon">The illustration of the rune.</param>
    /// <param name="runeId">The unique trait which identifies this rune. This is also added to the rune's <see cref="Traits"/>.</param>
    /// <param name="baseLevel">The base level of the Rune, before increasing with character level.</param>
    /// <param name="usageText">The targeting requirements for the Rune.</param>
    /// <param name="flavorText">The flavor-text of the Rune.</param>
    /// <param name="passiveText">The passive effect of the Rune on the bearer.</param>
    /// <param name="invocationText">(nullable) The text describing what happens when the rune is invoked.</param>
    /// <param name="levelText">The text describing how the rune changes at certain levels.</param>
    /// <param name="additionalTraits">(nullable) The list of additional traits associated with the Rune. By default, Runes have the Rune, Runesmith, and Magical traits. To overwrite these, write directly to the Traits field or call WithOverrideTraits().</param>
    public Rune(
        string name,
        Trait runeId,
        Illustration icon,
        int baseLevel,
        string usageText,
        string flavorText,
        string passiveText,
        string? invocationText = null,
        string? levelText = null,
        List<Trait>? additionalTraits = null)
    {
        this.Name = name;
        this.RuneId = runeId;
        this.Illustration = icon;
        this.BaseLevel = baseLevel;
        this.UsageText = usageText;
        this.FlavorText = flavorText;
        this.PassiveText = passiveText;
        this.InvocationText = invocationText;
        this.LevelText = levelText;
        if (additionalTraits != null)
            this.Traits = this.Traits.Concat(additionalTraits).ToList();
        Traits.Add(runeId);
    }
    #endregion
}