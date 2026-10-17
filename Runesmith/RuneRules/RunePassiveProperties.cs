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
    public Func<CombatAction,Rune,Creature,object?,Task<List<DrawnRune>?>>? DrawnRuneCreator { get; set; }
    
    /// <summary>
    /// Sets <see cref="DrawnRuneCreator"/> with a new function.
    /// </summary>
    public RunePassiveProperties WithDrawnRuneCreator(Func<CombatAction,Rune,Creature,object?,Task<DrawnRune?>> drawnCreator)
    {
        this.DrawnRuneCreator = async (drawAction, rune, target, subTarget) =>
            new List<DrawnRune?>([await drawnCreator(drawAction, rune, target, subTarget)])
            .WhereNotNull()
            .ToList();
        return this;
    }
    
    /// <summary>
    /// Sets <see cref="DrawnRuneCreator"/> with a new function.
    /// </summary>
    public RunePassiveProperties WithDrawnRuneCreator(Func<CombatAction,Rune,Creature,object?,Task<List<DrawnRune>?>> drawnCreator)
    {
        this.DrawnRuneCreator = drawnCreator;
        return this;
    }

    /// <summary>
    /// Sets <see cref="DrawnRuneCreator"/> with a function for creating diacritics.
    /// </summary>
    /// <remarks>
    /// This function is helpful for consolidating common code for diacritics, including handling null target DrawnRunes and preset target DrawnRunes.
    /// </remarks>
    /// <param name="additionalRuneRequirements">Whether the targeted DrawnRune is legal. Always prevents targeting runes that are diacritics and runes that have a diacritic already.</param>
    /// <param name="diacriticPassive">The function which creates a new diacritic DrawnRune from a given DrawnRune target, the action drawing the rune, and the base rune to create from.</param>
    /// <param name="question">The question to present to the user. This already includes the title of the diacritic in the string.</param>
    public RunePassiveProperties WithDiacriticCreator(
        Func<CombatAction,DrawnRune,bool>? additionalRuneRequirements,
        Func<CombatAction,Rune,DrawnRune,DrawnRune> diacriticPassive,
        Func<CombatAction,Rune,string> question)
    {
        this.DrawnRuneCreator = async (drawAction, rune, target, subTarget) =>
        {
            List<DrawnRune> drawings = [];
            
            // Cut straight to the passed-in rune target instead
            // of validating legality.
            if (subTarget is DrawnRune runeTarget
                && diacriticPassive(drawAction, rune, runeTarget) is {} newDiacritic)
                drawings.Add(newDiacritic);
            // Otherwise, go through choice-making as normal.
            else
                drawings.AddRange(await CommonRuneRules.ChooseARuneToDrawOn(
                    drawAction, drawAction.Owner, target,
                    drOnto =>
                        !ModData.PersistentActions.RuneIsUsedUp(drOnto.Source!, drOnto.Rune)
                        // null is true
                        && additionalRuneRequirements?.Invoke(drawAction, drOnto) is not false,
                    drOnto => drOnto is null ? null : diacriticPassive(drawAction, rune, drOnto),
                    question(drawAction, rune),
                    rune) ?? []);
            
            if (drawings.Count == 0)
                return null;
            
            // If you can only have one, only ever return one.
            if (rune.PassiveProperties.OnlyOneInExistenceAtATime)
                return [drawings.Last()];
            
            return drawings;
        };
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
    
    /// <summary>
    /// If true, then a runesmith can only have one DrawnRune instance of this rune at a time.
    /// </summary>
    public bool OnlyOneInExistenceAtATime { get; set; }

    /// <summary>
    /// Sets <see cref="OnlyOneInExistenceAtATime"/>. Default: true
    /// </summary>
    private RunePassiveProperties WithOnlyOneInExistenceAtATime(bool value = true)
    {
        this.OnlyOneInExistenceAtATime = value;
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

    /// <summary>
    /// Creates a properties instance for diacritic runes.
    /// </summary>
    /// <param name="passiveText">The passive text of the diacritic.</param>
    /// <param name="invokeableOnce">Whether only one instance of this rune is allowed and can only be invoked once per combat. Sets <see cref="OnlyOneInExistenceAtATime"/>.</param>
    /// <param name="heightening">The heightening of the diacritic rune, if any.</param>
    /// <returns></returns>
    public static RunePassiveProperties Diacritic(
        string passiveText,
        bool invokeableOnce = false,
        Func<Rune,int,string>? heightening = null)
    {
        var props = new RunePassiveProperties(
                passiveText, heightening)
            .WithOnlyOneInExistenceAtATime(invokeableOnce);
        return props;
    }
}