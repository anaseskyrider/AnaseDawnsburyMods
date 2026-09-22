using System.Diagnostics.CodeAnalysis;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

/// <summary>
/// The properties of a <see cref="Rune"/> as it relates to the passive effects of that rune.
/// </summary>
/// <param name="passiveText">The description of the rune's main passive text with sentence case formatting and with trailing punctuation.</param>
public class RunePassiveProperties(
    string passiveText,
    Func<Rune, int, string>? heightening)
{
    public Rune Self { get; set; } = null!;

    #region Text Entry

    /// <summary>
    /// Gets or sets the unformatted static invocation entry of the rune.
    /// </summary>
    /// <remarks>
    /// This should be in sentence case, and should have punctuation.
    /// </remarks>
    /// <example>"The rune-bearer gains a +1 status bonus to Deception, Diplomacy, and Performance checks."</example>
    /// <value>
    /// The description of the rune's invocation entry with sentence case formatting and with trailing punctuation.
    /// </value>
    public string PassiveText
    {
        get;
        set
        {
            string input = value;
            input = input.Capitalize();
            input = input.TrimEnd(' ');
            if (!input.EndsWith('.'))
                input += '.';
            field = input;
        }
    } = passiveText;
    
    /// <summary>
    /// Gets or sets the generator of passive text with applied heightening.
    /// </summary>
    /// <remarks>
    /// Every Rune defines how it displays its text when heightened. If set to null, this always returns <see cref="PassiveText"/>.
    /// </remarks>
    /// <param name="Rune">The referenced rune.</param>
    /// <param name="int">The CHARACTER LEVEL to heighten the rune to.</param>
    /// <returns>(string) The PassiveText with heightened changes.</returns>
    /// <seealso cref="Dawnsbury.Display.Text.S"/>
    [AllowNull]
    public Func<Rune, int, string> PassiveTextWithHeightening
    {
        get => field ?? ((_, _) => this.PassiveText);
        set;
    } = heightening;

    #endregion
    
    /// <summary>
    /// A lambda that creates and returns a new <see cref="DrawnRune"/>, usually the passive effects on a rune-bearer. This is called by actions to get the DrawnRune representing the effects of a rune being placed.
    /// </summary>
    /// <list type="bullet">
    /// <item><see cref="CombatAction"/>: The CombatAction which is drawing the rune.</item>
    /// <item><see cref="Rune"/>: The Rune representing this DrawnRune.</item>
    /// <item><see cref="Creature"/>: The TARGET which will bear the Rune's DrawnRune.</item>
    /// <item><see cref="object"/>?: The secondary target of the drawn rune, if it's different from the creature target.</item>
    /// </list>
    /// <returns>(DrawnRune) The last DrawnRune this lambda generates.</returns>
    public Func<CombatAction,Rune,Creature,object?,Task<DrawnRune?>>? DrawnRuneCreator { get; set; }
    
    /// <summary>
    /// Sets <see cref="DrawnRuneCreator"/> with a new function.
    /// </summary>
    public RunePassiveProperties WithDrawnRuneCreator(Func<CombatAction,Rune,Creature,object?,Task<DrawnRune?>> drawnCreator)
    {
        this.DrawnRuneCreator = drawnCreator;
        return this;
    }

    #region Additional Properties
    
    /// <summary>
    /// If true, the passive effect of this rune is harmful to the bearer.
    /// </summary>
    /// <remarks>
    /// This is useful to filter out runes that would be obviously harmful when choosing a rune to apply (or tattoo) to yourself or an ally.
    /// </remarks>
    public bool PassiveEffectIsDebuff { get; set; }

    /// <summary>
    /// Sets <see cref="PassiveEffectIsDebuff"/> to true.
    /// </summary>
    public RunePassiveProperties WithIsDebuff()
    {
        this.PassiveEffectIsDebuff = true;
        return this;
    }

    #endregion

    /// <summary>
    /// Execute an action that makes miscellaneous changes.
    /// </summary>
    public RunePassiveProperties WithAdjustments(Action<RunePassiveProperties> adjustments)
    {
        adjustments(this);
        return this;
    }
}