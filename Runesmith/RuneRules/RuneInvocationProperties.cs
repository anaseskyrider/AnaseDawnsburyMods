using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

/// <summary>
/// The properties of a <see cref="Rune"/> as it relates to invoking that rune.
/// </summary>
/// <param name="invocationText">The description of the rune's invocation entry with sentence case formatting and with trailing punctuation.</param>
public class RuneInvocationProperties(
    string? invocationText,
    Func<Rune, int, string>? heightening)
{
    public Rune Self { get; set; } = null!;

    #region Text Entry

    /// <summary>
    /// Gets whether this rune's description has an invocation entry.
    /// </summary>
    public bool HasInvocationEntry => InvocationText != null;
    
    /// <summary>
    /// Gets or sets the unformatted static invocation entry of the rune.
    /// </summary>
    /// <remarks>
    /// This should be in sentence case, and should have punctuation.
    /// </remarks>
    /// <example>"The bearer takes 1d8 fire damage with a basic Fortitude save. On a critical failure, it’s also dazzled for 1 round."</example>
    /// <value>
    /// The description of the rune's invocation entry with sentence case formatting and with trailing punctuation.
    /// </value>
    /// <seealso cref="GetInvocationEntry"/>
    public string? InvocationText
    {
        private get;
        set
        {
            if (value is null)
            {
                field = null;
                return;
            }
            string input = value;
            input = input.Capitalize();
            input = input.TrimEnd(' ');
            if (!input.EndsWith('.'))
                input += '.';
            field = input;
        }
    } = invocationText;
    
    /// <summary>
    /// Gets the formatted invocation entry of the rune.
    /// </summary>
    /// <example>"{b}Invocation{/b} The bearer takes 1d8 fire damage with a basic Fortitude save. On a critical failure, it’s also dazzled for 1 round."</example>
    public string? InvocationTextWithFormatting =>
        InvocationText is null
            ? null
            : $"{{b}}Invocation{{/b}} {this.InvocationText}";

    /// <summary>
    /// Gets or sets the generator for the invocation entry with applied heightening.
    /// </summary>
    /// <remarks>
    /// Every Rune defines how it displays its text when heightened. Unless otherwise defined, this lambda returns <see cref="InvocationText"/>.
    /// </remarks>
    /// <param name="Rune">The rune's text to use.</param>
    /// <param name="int">The CHARACTER LEVEL to heighten the rune to.</param>
    /// <returns>(string) The InvocationText with heightened behavior.</returns>
    /// <seealso cref="Dawnsbury.Display.Text.S"/>
    public Func<Rune,int,string>? InvocationTextWithHeightening
    {
        get => field ?? (this.InvocationText is null
            ? null
            : new Func<Rune,int,string>((_,_) => this.InvocationText));
        set;
    } = heightening;

    /// <summary>
    /// Gets the generator for the invocation entry with formatting and applied heightening.
    /// </summary>
    public Func<Rune,int,string>? InvocationTextWithFormattedHeightening =>
        InvocationTextWithHeightening is null
            ? null
            : (rune, level) =>
                $"{{b}}Invocation{{/b}} {this.InvocationTextWithHeightening(rune, level)}";

    #endregion

    #region Targeting

    /// <summary>
    /// The requirements necessary to invoke this rune.
    /// </summary>
    /// <remarks>
    /// This should make it easier to detect when a rune has complex invocation requirements or would be wasteful to invoke under standard conditions. Because of Compound Invocations having utility, you sometimes might want to waste a rune's invocation.
    /// </remarks>
    public List<CreatureTargetingRequirement> TargetingRequirements { get; } = [];
    
    /// <summary>
    /// Whether this rune can be applied by YOU onto the TARGET.
    /// </summary>
    public Usability IsLegalTarget(Creature runesmith, Creature target)
    {
        foreach (CreatureTargetingRequirement req in TargetingRequirements)
        {
            // Return the first unusable reason found
            if (req.Satisfied(runesmith, target)
                is { CanBeUsed: false } usable)
                return usable;
        }

        return Usability.Usable;
    }

    /// <summary>
    /// Removes all requirements to invoke the rune.
    /// </summary>
    public RuneInvocationProperties WithNoRequirements()
    {
        this.TargetingRequirements.Clear();
        return this;
    }
    
    /// <summary>
    /// Constructs and adds an additional <see cref="LegacyCreatureTargetingRequirement"/>.
    /// </summary>
    public RuneInvocationProperties WithAdditionalRequirement(
        Func<Creature, Creature, Usability> additionalConditionOnTarget)
    {
        return this.WithAdditionalRequirement(new LegacyCreatureTargetingRequirement(additionalConditionOnTarget));
    }
    
    /// <summary>
    /// Adds an additional <see cref="CreatureTargetingRequirement"/>.
    /// </summary>
    public RuneInvocationProperties WithAdditionalRequirement(
        CreatureTargetingRequirement creatureTargetingRequirement)
    {
        this.TargetingRequirements.Add(creatureTargetingRequirement);
        return this;
    }

    #endregion
    
    /// <summary>
    /// An asynchronous lambda that executes the logic of a Rune's invocation effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should execute the inner-most logic of the rune's invocation. Everything else will wrap around this logic, performing targeting validation and the like.
    /// </para>
    /// <para>
    /// At the end, return the affected target or targets of the invocation, which might be different from the rune-bearer.
    /// </para>
    /// </remarks>
    /// <param name="CombatAction">The <see cref="CombatAction"/> INVOKING the rune.</param>
    /// <param name="DrawnRune">The <see cref="DrawnRune"/> representing the rune being INVOKED.</param>
    /// <param name="Creature">The target <see cref="Creature"/> of the INVOCATION effects.</param>
    /// <returns>The list of creatures affected by this invocation. This is usually the effect target, or all chosen targets for a subsidiary area effect.</returns>
    public Func<CombatAction,DrawnRune,Creature,Task<List<Creature>?>>? EffectOnOneTarget { get; set; }

    /// <summary>
    /// Set <see cref="EffectOnOneTarget"/> for this rune's invocation.
    /// </summary>
    public RuneInvocationProperties WithInvocationOnEachTarget(Func<CombatAction,DrawnRune,Creature,Task<List<Creature>?>> effectOnOne)
    {
        this.EffectOnOneTarget = effectOnOne;
        return this;
    }

    /// <summary>
    /// Set <see cref="EffectOnOneTarget"/> for this rune's invocation.
    /// </summary>
    public RuneInvocationProperties WithInvocationOnEachTarget(Func<CombatAction,DrawnRune,Creature,Task<Creature?>> effectOnOne)
    {
        this.EffectOnOneTarget = async (invokeAction, invokedRune, effectTarget) =>
        {
            Creature? affectedCreature = await effectOnOne.Invoke(invokeAction, invokedRune, effectTarget);
            if (affectedCreature is null)
                return null;
            return [affectedCreature];
        };
        return this;
    }

    #region Additional Properties

    /// <summary>
    /// Gets or sets the sound that plays just before the rune is invoked.
    /// </summary>
    public SfxName? SoundEffectBeforeInvocation { get; set; }

    /// <summary>
    /// Sets <see cref="SoundEffectBeforeInvocation"/>
    /// </summary>
    public RuneInvocationProperties WithSoundBeforeInvocation(SfxName? sound)
    {
        this.SoundEffectBeforeInvocation = sound;
        return this;
    }
    
    /// <summary>
    /// Gets or sets the sound that plays just after the rune is invoked.
    /// </summary>
    public SfxName? SoundEffectAfterInvocation { get; set; }

    /// <summary>
    /// Sets <see cref="SoundEffectBeforeInvocation"/>
    /// </summary>
    public RuneInvocationProperties WithSoundAfterInvocation(SfxName? sound)
    {
        this.SoundEffectAfterInvocation = sound;
        return this;
    }
    
    /// <summary>
    /// Gets or sets whether this rune deals damage when invoked.
    /// </summary>
    public bool DealsDamage { get; set; }

    /// <summary>
    /// Sets <see cref="DealsDamage"/> to true.
    /// </summary>
    public RuneInvocationProperties WithDealsDamage()
    {
        this.DealsDamage = true;
        return this;
    }

    /// <summary>
    /// Gets or sets whether this rune affects an area, usually an emanation, if any.
    /// </summary>
    /// <remarks>
    /// Useful for indicating to diacritic runes whether this rune already affects a same or larger area.
    /// </remarks>
    public bool AffectsArea { get; set; }

    /// <summary>
    /// Sets that this invocation affects an area and prevents display of roll breakdown tooltips.
    /// </summary>
    public RuneInvocationProperties WithAffectsArea()
    {
        this.AffectsArea = true;
        this.HideBreakdownTooltip = true;
        return this;
    }
    
    /// <summary>
    /// The defense (usually a saving throw) used for its invocation.
    /// </summary>
    public Defense? Defense { get; set; }
    
    /// <summary>
    /// Sets the <see cref="Defense"/> that this rune targets.
    /// </summary>
    public RuneInvocationProperties WithDefense(Defense defense)
    {
        this.Defense = defense;
        return this;
    }
    
    /// <summary>
    /// If <see cref="Defense"/> is not null, this prevents an invocation from generating a roll breakdown.
    /// </summary>
    /// <remarks>
    /// This is useful for invocations which have an area of effect, or that have indirect saving throws (such as Esvadir, which invokes from the rune-bearer onto a secondary creature).
    /// </remarks>
    public bool HideBreakdownTooltip { get; set; }
    
    /// <summary>
    /// As <see cref="HideBreakdownTooltip"/>, but hides breakdowns for allies only.
    /// </summary>
    public bool HideBreakdownTooltipForAllies { get; set; }

    /// <summary>
    /// Sets <see cref="HideBreakdownTooltip"/> to true.
    /// </summary>
    public RuneInvocationProperties WithHideTooltip()
    {
        this.HideBreakdownTooltip = true;
        return this;
    }

    /// <summary>
    /// Sets <see cref="HideBreakdownTooltip"/> to true.
    /// </summary>
    public RuneInvocationProperties WithHideTooltipForAllies()
    {
        this.HideBreakdownTooltipForAllies = true;
        return this;
    }

    #endregion

    /// <summary>
    /// Execute an action that makes miscellaneous changes.
    /// </summary>
    public RuneInvocationProperties WithAdjustments(Action<RuneInvocationProperties> adjustments)
    {
        adjustments(this);
        return this;
    }
}