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
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithPlaytest;

// I need this link. A lot.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments

public class Rune
{
    #region Core Properties
    
    /// <summary>
    /// The unique trait which corresponds to instances of this particular kind of rune, such as Atryl, Rune of Fire.
    /// </summary>
    public Trait RuneId { get; set; }
    
    /// <summary>
    /// The original level of the Rune, before it increases with character level. This corresponds to the CHARACTER LEVEL required to learn the Rune. 
    /// </summary>
    public int BaseLevel  { get; set; }
    
    /// <summary>
    /// The Rune's icon.
    /// </summary>
    public Illustration Illustration { get; set; }
    
    /// <summary>
    /// The traits associated with the rune. By default, all runes have at least the Rune, Runesmith, and Magical traits.
    /// </summary>
    public List<Trait> Traits { get; set; } = [Enums.Traits.Rune, Enums.Traits.Runesmith, Trait.Magical];

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
    public Func<CombatAction?, Creature?, Creature, Rune, Task<DrawnRune?>>? NewDrawnRune { get; set; }
    
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
    
    /// <summary>
    /// The name of the rune.
    /// <code>
    /// newRune.Name = "Atryl, Rune of Fire"
    /// </code>
    /// </summary>
    public string Name { get; set; } 
    
    /// <summary>
    /// The unformatted text describing the usage of the rune.
    /// <code>
    /// newRune.UsageText = "drawn on a shield"
    /// </code>
    /// </summary>
    public string UsageText { get; set; }

    /// <summary>
    /// Gets the rune's <see cref="UsageText"/> with bolded formatting.
    /// </summary>
    /// <returns>(string) The original text prepended with "{b}Usage{/b} ".</returns>
    public string WithUsageTextFormatting(string? text = null)
    {
        return "{b}Usage{/b} " + (text ?? this.UsageText);
    }
    
    /// <summary>
    /// The unformatted flavor text of the rune.
    /// <code>
    /// newRune.FlavorText = "This serrated rune, when placed on a blade, ensures it will never go dull."
    /// </code>
    /// </summary>
    public string? FlavorText { get; set; }

    /// <summary>
    /// Gets the rune's <see cref="FlavorText"/> with italics formatting.
    /// </summary>
    /// <returns>(string) The original text surrounded with "{i}" and "{/i}".</returns>
    public string WithFlavorTextFormatting(string? text = null)
    {
        return "{i}" + (text ?? this.FlavorText) + "{/i}";
    }
    
    /// <summary>
    /// The text describing the passive behavior of the rune.
    /// <code>
    /// newRune.PassiveText = "A shield bearing this rune increases its circumstance bonus to AC by 1."
    /// </code>
    /// </summary>
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
    
    /// <summary>
    /// The numeric part of the formatted level-up text. E.g. "+2" or "17th". Don't use any parentheses.
    /// </summary>
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

    /// <summary>
    /// Generates a description block for this rune's Trace actions.
    /// </summary>
    /// <param name="traceAction">The CombatAction to check against. Used for owner level.</param>
    /// <param name="withFlavorText">Whether to include flavor text in the description (typically false for dropdown options).</param>
    /// <param name="prologueText">The paragraph to add at the top of the description (includes one line-break after).</param>
    /// <param name="afterFlavorText">The text to add at the end of the flavor text paragraph.</param>
    /// <param name="afterUsageText">The text to add at the end of the usage text paragraph.</param>
    /// <param name="afterPassiveText">The text to add at the end of the passive text paragraph.</param>
    /// <param name="afterInvocationText">The text to add at the end of the invocation text paragraph.</param>
    /// <param name="epilogueText">The paragraph to add at the bottom of the description (includes one line-break before).</param>
    /// <returns></returns>
    public string CreateTraceActionDescription(
        CombatAction traceAction,
        bool withFlavorText = true,
        string? prologueText = null,
        string? afterFlavorText = null,
        string? afterUsageText = null,
        string? afterPassiveText = null,
        string? afterInvocationText = null,
        string? epilogueText = null)
    {
        int lvl = traceAction.Owner.Level;
        string usageText = this.WithUsageTextFormatting() + afterUsageText;
        string? flavorText = (withFlavorText ? this.WithFlavorTextFormatting() : null) + afterFlavorText;
        string passiveText = this.PassiveTextWithHeightening(this, lvl) + afterPassiveText;
        string? invocationText = this.WithInvocationTextFormatting(this.InvocationTextWithHeightening(this, lvl)) + afterInvocationText;
        //string? levelText = this.WithLevelTextFormatting(); // Should have heightening, so this shouldn't be necessary.
        return (!string.IsNullOrEmpty(prologueText) ? $"{prologueText}\n" : null)
               + (!string.IsNullOrEmpty(flavorText) ? $"{flavorText}\n\n": null)
               + $"{usageText}\n\n{passiveText}"
               + (!string.IsNullOrEmpty(invocationText) ? $"\n\n{invocationText}" : null)
               + (!string.IsNullOrEmpty(epilogueText) ? $"\n{epilogueText}" : null);
               //+ (levelText != null ? $"\n\n{levelText}" : null);
    }
    
    /// <summary>
    /// Gets the full description block for the Rune with formatting, optionally with the flavor text.
    /// </summary>
    /// <param name="withFlavorText">Whether to include <see cref="WithFlavorTextFormatting"/> in the return.</param>
    /// <returns>(string) The full description with formatting.</returns>
    public string GetFormattedFeatDescription(bool withFlavorText = true)
    {
        string description = 
            (withFlavorText ? this.WithFlavorTextFormatting() + "\n\n" : null) +
            this.WithUsageTextFormatting() + "\n\n" +
            this.PassiveText +
            (this.WithInvocationTextFormatting() != null ? "\n\n" + this.WithInvocationTextFormatting() : null) +
            (this.WithLevelTextFormatting() != null ? "\n\n" + this.WithLevelTextFormatting() : null);
        return description;
    }
    
    #endregion

    #region Instance Property Methods

    /// <summary>
    /// Overrides the default <see cref="Traits"/> expected of a Rune to the list given. (Such as if for some reason you need a Rune without the Rune trait.)
    /// </summary>
    /// <returns>The Rune being modified.</returns>
    public Rune WithOverrideTraits(List<Trait> newTraits)
    {
        this.Traits = newTraits;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="UsageCondition"/> on the Rune instance.
    /// </summary>
    /// <param name="condition"></param>
    /// <returns>The Rune being modified.</returns>
    public Rune WithUsageCondition(Func<Creature, Creature, Usability> condition)
    {
        this.UsageCondition = condition;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="UsageCondition"/> on the Rune instance with a lambda that always returns Usability.Usable when called.
    /// </summary>
    /// <returns>The Rune being modified.</returns>
    public Rune WithAlwaysUsableCondition()
    {
        Func<Creature, Creature, Usability> condition = (Creature attacker, Creature defender) => Usability.Usable;
        this.UsageCondition = condition;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="NewDrawnRune"/> on the Rune instance.
    /// </summary>
    /// <returns>The Rune being modified.</returns>
    public Rune WithNewDrawnRune(Func<CombatAction?, Creature?, Creature, Rune, Task<DrawnRune?>> drawRuneLambda)
    {
        this.NewDrawnRune = drawRuneLambda;
        return this;
    }

    /// <summary>
    /// A helper function that sets <see cref="InvocationBehavior"/> on the Rune instance.
    /// </summary>
    /// <returns>The Rune being modified.</returns>
    public Rune WithInvocationBehavior(Func<CombatAction, Rune, Creature, Creature, DrawnRune, Task> newInvocationBehavior )
    {
        this.InvocationBehavior = newInvocationBehavior;
        return this;
    }

    /// <summary>
    /// Adds Trait.Shield to DrawTechnicalTraits, which indicates to other parts of the mod that the rune is drawn onto a shield.
    /// </summary>
    /// <returns></returns>
    public Rune WithDrawnOnShieldTechnical()
    {
        this.DrawTechnicalTraits = this.DrawTechnicalTraits.Concat([Trait.Shield]).ToList();
        return this;
    }
    
    /// <summary>
    /// Adds Enums.Traits.Rune to DrawTechnicalTraits, which indicates to other parts of the mod that the rune is drawn onto a rune.
    /// </summary>
    /// <returns></returns>
    public Rune WithDrawnOnRuneTechnical()
    {
        this.DrawTechnicalTraits = this.DrawTechnicalTraits.Concat([Enums.Traits.Rune]).ToList();
        return this;
    }

    /// <summary>
    /// Adds Trait.IsHostile to DrawTechnicalTraits, which indicates to other parts of the mod that the passive is detrimental to the bearer.
    /// </summary>
    public Rune WithDetrimentalPassiveTechnical()
    {
        this.DrawTechnicalTraits = this.DrawTechnicalTraits.Concat([Trait.IsHostile]).ToList();
        return this;
    }
    
    /// <summary>
    /// Adds Trait.IsHostile to InvokeTechnicalTraits, which indicates to other parts of the mod that the invocation deals damage when invoked.
    /// </summary>
    public Rune WithDamagingInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.IsHostile]).ToList();
        return this;
    }

    /// <summary>
    /// Adds Trait.Fortitude to InvokeTechnicalTraits, which indicates a fortitude save roll breakdown before invoking the rune.
    /// </summary>
    /// <returns></returns>
    public Rune WithFortitudeSaveInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Fortitude]).ToList();
        return this;
    }

    /// <summary>
    /// Adds Trait.Reflex to InvokeTechnicalTraits, which indicates a reflex save roll breakdown before invoking the rune.
    /// </summary>
    /// <returns></returns>
    public Rune WithReflexSaveInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Reflex]).ToList();
        return this;
    }

    /// <summary>
    /// Adds Trait.Will to InvokeTechnicalTraits, which indicates a will save roll breakdown before invoking the rune.
    /// </summary>
    /// <returns></returns>
    public Rune WithWillSaveInvocationTechnical()
    {
        this.InvokeTechnicalTraits = this.InvokeTechnicalTraits.Concat([Trait.Will]).ToList();
        return this;
    }
    
    #endregion
    
    #region Instance Methods
    
    /// <summary>
    /// Checks whether a Trait is listed among the Rune's Traits. This searches both its Traits field and its RuneId field.
    /// </summary>
    /// <param name="trait">The Trait to find.</param>
    /// <returns>(bool) Returns true if the trait was found.</returns>
    public bool HasTrait(Trait trait)
    {
        return this.Traits.Contains(trait) || trait == this.RuneId;
    }

    /// <summary>
    /// Creates and applies an immunity against this rune's invocation effects to a given creature. This QEffect needs to be removed manually with <see cref="RemoveAllImmunities"/> at the end of any activity with subsidiary invocation actions.
    /// </summary>
    /// <param name="invokeTarget">The <see cref="Creature"/> to become immune to this rune's invocation.</param>
    /// <returns>(<see cref="QEffect"/>) The immunity which was applied to the target.</returns>
    public QEffect ApplyImmunity(Creature invokeTarget)
    {
        QEffect runeInvocationImmunity = new QEffect()
        {
            Name = "Invocation Immunity: " + this.Name,
            Description = "Cannot be affected by another instance of this invocation until the end of this action.",
            Illustration = new SuperimposedIllustration(this.Illustration, Enums.Illustrations.NoSymbol),
            Tag = this,
            Traits = [Enums.Traits.InvocationImmunity, this.RuneId], // ImmunityQFs are identified by these traits.
            ExpiresAt = ExpirationCondition.ExpiresAtEndOfAnyTurn, // This QF is supposed to be removed when the activity making invokeActions completes. This is a back-up safety for developer-error.
            DoNotShowUpOverhead = true,
        };
        invokeTarget.AddQEffect(runeInvocationImmunity);
        return runeInvocationImmunity;
    }
    
    /// <summary>
    /// Determines whether a TARGET Creature is immune to the invocation effects of this Rune by searching for a QEffect with the <see cref="Enums.Traits.InvocationImmunity"/> trait and a trait matching this Rune's <see cref="RuneId"/>.
    /// </summary>
    /// <param name="target">The CREATURE to check.</param>
    /// <returns>(bool) Returns true if the immunity QEffect is present on the TARGET.</returns>
    public bool IsImmuneToThisInvocation(Creature target)
    {
        QEffect? thisRunesImmunity = target.QEffects.FirstOrDefault(qfToFind =>
            qfToFind.Traits.Contains(Enums.Traits.InvocationImmunity) &&
            qfToFind.Traits.Contains(this.RuneId));
        return thisRunesImmunity != null;
    }

    /// <summary>
    /// Removes a given DrawnRune from its owner, if it corresponds to a DrawnRune created by an instance of this Rune.
    /// </summary>
    /// <param name="runeToRemove">The DrawnRune to be removed from its Owner.</param>
    /// <returns>(bool) True if the DrawnRune was removed, false otherwise.</returns>
    public bool RemoveDrawnRune(DrawnRune runeToRemove)
    {
        int removals = runeToRemove.Owner.RemoveAllQEffects(
            qfToRemove =>
            {
                if (qfToRemove != runeToRemove || runeToRemove.Rune != this)
                    return false;
                
                runeToRemove.DrawnOn = null;
                return true;
            });
        return (removals > 0);
    }
    
    /// <summary>
    /// The CASTER uses an ACTION to apply the RUNE's <see cref="NewDrawnRune"/> to the TARGET, which might IGNORE targeting restrictions.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which is applying the rune. This action should have either the <see cref="Enums.Traits.Traced"/> or the <see cref="Enums.Traits.Etched"/> traits to determine the duration of the effect being applied.</param>
    /// <param name="caster">The Creature applying the rune.</param>
    /// <param name="target">The Creature the rune is applying to.</param>
    /// <param name="ignoreUsageRequirements">(Default: false) If false, then the DrawnRune is applied only when its Rune's <see cref="UsageCondition"/> is valid for the target. This is true for cases like the Runic Reprisal feat which allows a Runesmith to apply any damaging rune to their shield, taking none of the passive effects, but allowing it to be invoked on a creature when they Shield Block.</param>
    /// <returns>(bool) True if the effect was successfully applied to the target, false otherwise.</returns>
    public async Task<DrawnRune?> DrawRuneOnTarget(
        CombatAction sourceAction,
        Creature caster,
        Creature target,
        bool ignoreUsageRequirements = false)
    {
        // Apply the QF if "ignoreTargetRestrictions is True", or "UsageCondition isn't null, and it returns Usable on the target".
        if (!ignoreUsageRequirements &&
            (this.UsageCondition == null || this.UsageCondition.Invoke(caster, target) != Usability.Usable)) 
            return null;
        
        DrawnRune? qfToApply = await this.NewDrawnRune?.Invoke(sourceAction, caster, target, this);

        if (qfToApply == null)
            return null;
        
        /*// Event callback
        foreach (Creature cr in caster.Battle.AllCreatures)
        {
            foreach (QEffect qf in cr.QEffects)
            {
                if (qf is DrawnRune drawnRune)
                {
                    qfToApply.BeforeApplyingDrawnRune.Invoke();
                }
            }
        }*/
        
        // Determine the way the rune is being applied.
        if (sourceAction.HasTrait(Enums.Traits.Etched))
            qfToApply = qfToApply.WithIsEtched();
        else if (sourceAction.HasTrait(Enums.Traits.Traced))
            qfToApply = qfToApply.WithIsTraced();
        
        target.AddQEffect(qfToApply);

        return qfToApply;
    }

    /// <summary>
    /// Creates a generic CombatAction which when executed, calls <see cref="DrawRuneOnTarget"/> on each target using this Rune. This action inherits the mechanics of Tracing a Rune, such as the <see cref="Enums.Traits.Traced"/> and Manipulate traits.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <param name="actions">The number of actions for this variant. If actions==-3, a 1-2 action variable target is used. If actions==1, an adjacent target is used. If actions==2, a ranged target is used (6 tiles). Otherwise, a Self target is used. The action cost can still be altered afterward (such as for use in subsidiary actions).</param>
    /// <param name="range">The range (in tiles) to use for the 2-action version. Default is 6.</param>
    /// <returns>(CombatAction) The action which traces the given rune on the target.</returns>
    public CombatAction CreateTraceAction(
        Creature owner,
        int actions = 0,
        int? range = 6)
    {
        // Determine range to target (logic maybe expanded later)
        int rangeToTarget = range ?? 6;

        // Determine Target Properties
        DependsOnActionsSpentTarget varyTarget;
        CreatureTarget adjacentTarget = Target.AdjacentCreatureOrSelf();
        CreatureTarget rangedTarget = Target.RangedCreature(rangeToTarget);
        varyTarget = Target.DependsOnActionsSpent(
            adjacentTarget,
            rangedTarget,
            null);
                        
        // Add extra usage requirements
        foreach (Target tar in varyTarget.Targets)
        {
            if (tar is not CreatureTarget crTar)
                continue;
            crTar.WithAdditionalConditionOnTargetCreature( // Free hand
                (attacker, defender) =>
                    attacker.HasFreeHand || attacker.HeldItems.Any(item => item.HasTrait(Enums.Traits.CountsAsRunesmithFreeHand)) ? Usability.Usable : Usability.NotUsable("You must have a free hand to trace a rune"));
            if (this.UsageCondition != null)
                crTar.WithAdditionalConditionOnTargetCreature(this.UsageCondition); // UsageCondition
        }
        
        // Determine traits
        Trait[] traits = this.Traits.ToArray().Concat(
            [Trait.Concentrate,
                Trait.Magical,
                Trait.Manipulate,
                Enums.Traits.Traced,
                Trait.Spell] // <- Should apply magic immunity.
            ).ToArray();
        
        // Create action
        CombatAction drawRuneAction = new CombatAction(
            owner,
            this.Illustration!, // Suppress
            "Trace " + this.Name,
            traits,
            "ERROR: INCOMPLETE DESCRIPTION",
            actions switch
            {
                2 => rangedTarget,
                1 => adjacentTarget,
                -3 => varyTarget,
                _ => Target.Self()
            })
            {
                Tag = this,
            }
            .WithActionCost(actions)
            .WithSoundEffect(SfxName.AncientDust) // TODO: Consider alternative SFX for Trace Rune.
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                Rune actionRune = (thisAction.Tag as Rune)!;
                if (await actionRune.DrawRuneOnTarget(thisAction, caster, target) == null)
                    thisAction.RevertRequested = true;
            });

        if (actions != 1) // Isn't the melee one
        {
            drawRuneAction.WithProjectileCone(VfxStyle.BasicProjectileCone(this.Illustration));
        }
        
        if (actions == -3)
        {
            drawRuneAction.WithCreateVariantDescription((actions, spellVariant) =>
            { // Just having this gives the variant range information.
                return actions switch
                {
                    //1 => this.CreateTraceActionDescription(drawRuneAction, withFlavorText:false),
                    //2 => this.CreateTraceActionDescription(drawRuneAction, withFlavorText:false),
                    _ => this.CreateTraceActionDescription(drawRuneAction, withFlavorText:false)
                };
            });
        }
        
        // Determine description based on actions preset
        switch (actions)
        {
            case -3:
                drawRuneAction.Description = this.CreateTraceActionDescription(drawRuneAction, afterUsageText:$"\n\n{{icon:Action}} The range is touch.\n{{icon:TwoActions}} The range is {rangeToTarget*5} feet.");
                break;
            case 1:
                drawRuneAction.Description = this.CreateTraceActionDescription(drawRuneAction, prologueText:"{b}Range{/b} touch\n");
                break;
            case 2:
                drawRuneAction.Description = this.CreateTraceActionDescription(drawRuneAction, prologueText:$"{{b}}Range{{/b}} {rangeToTarget*5} feet\n");
                break;
            default:
                drawRuneAction.Description = this.CreateTraceActionDescription(drawRuneAction);
                break;
        }
        
        return drawRuneAction;
    }

    /// <summary>
    /// Creates a variant of <see cref="CreateTraceAction"/> that inherits the mechanics of Etching a Rune, such as the <see cref="Enums.Traits.Etched"/> trait and a map-sized range limit, and only applying runes to allies.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <returns>(CombatAction) The action which etches the given rune on the target.</returns>
    public CombatAction CreateEtchAction(
        Creature owner)
    {
        CombatAction etchAction = this.CreateTraceAction(owner, 2)
            .WithActionCost(0)
            .WithSoundEffect(Enums.SfxNames.EtchRune);
        etchAction.Name = $"Etch {this.Name}";
        etchAction.Description = this.CreateTraceActionDescription(etchAction, false, prologueText:"{Blue}Etched: lasts until the end of combat.{/Blue}\n");
        etchAction.Traits.Remove(Enums.Traits.Traced);
        etchAction.Traits.Remove(Trait.Manipulate); // Just in case this might provoke a reaction.
        etchAction.Traits.Remove(Trait.Concentrate); // Just in case this might provoke a reaction.
        etchAction.Traits.Add(Enums.Traits.Etched);
        
        // Usable across the whole map
        etchAction.Target = Target.RangedFriend(99); // BUG: Is blocked by line of effect. I don't currently know a way around this.
        if (this.UsageCondition != null) // Do this again since we just replaced the target.
            (etchAction.Target as CreatureTarget)!.WithAdditionalConditionOnTargetCreature(this.UsageCondition);
            // Don't add a free hand requirement; this "technically" happened "before" combat.
        
        // Remove tedious animations
        etchAction.ProjectileIllustration = null;
        etchAction.ProjectileCount = 0;
        etchAction.ProjectileKind = ProjectileKind.None;
        
        return etchAction;
    }

    /// <summary>
    /// Creates and returns the CombatAction wrapper which executes the InvocationBehavior of the given runeTarget.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which executes this internal action.</param>
    /// <param name="caster">The Creature invoking the rune.</param>
    /// <param name="runeTarget">The DrawnRune to be invoked.</param>
    /// <param name="range">The range to the creature target. If no range is specified, the range is 30 feet.</param>
    /// <param name="withImmediatelyRemoveImmunity">If true, then <see cref="WithImmediatelyRemovesImmunity"/> is called on the new CombatAction.</param>
    /// <param name="requiresTargetHasDrawnRune">If true, this action can only be used against creatures who own the supplied runeTarget.</param>
    /// <returns></returns>
    public CombatAction? CreateInvokeAction(
        CombatAction? sourceAction,
        Creature caster,
        DrawnRune runeTarget,
        int range = 6,
        bool withImmediatelyRemoveImmunity = false,
        bool requiresTargetHasDrawnRune = true)
    {
        if (this.InvocationBehavior == null)
            return null;

        Trait drawTrait = runeTarget.DrawTrait ?? Trait.None;
        string initialDescription = $"{{b}}{runeTarget.Name}{{/b}}\n"
                                    + (runeTarget.Description!.Contains("Tattoo") 
                                        ? "{i}Tattooed{/i}\n" 
                                        : $"{{i}}{drawTrait.ToStringOrTechnical()}{{/i}}\n");

        Trait[] traits = this.Traits.ToArray().Concat(
            [
                Enums.Traits.Invocation,
                Trait.UnaffectedByConcealment,
                Trait.Spell, // <- Should apply magic immunity.
            ])
            .ToArray();
        
        CreatureTarget invokeTarget = Target.RangedCreature(range);
        if (requiresTargetHasDrawnRune)
            invokeTarget.WithAdditionalConditionOnTargetCreature((attacker, defender) =>
            {
                QEffect? foundQf = defender.QEffects.FirstOrDefault(
                    qfToFind => qfToFind == runeTarget);
                return foundQf != null
                    ? Usability.Usable
                    : Usability.NotUsableOnThisCreature($"{this.Name} not applied");
            });
        
        CombatAction invokeThisRune = new CombatAction(
            caster,
            this.Illustration,
            "Invoke " + this.Name,
            traits,
            initialDescription + (this.InvocationTextWithHeightening(this, caster.Level) ?? "[No invocation entry]"),
            invokeTarget)
            {
                Tag = this,
            }
            .WithActionCost(0)
            .WithProjectileCone(VfxStyle.BasicProjectileCone(this.Illustration))
            .WithSoundEffect(SfxName.DazzlingFlash) // TODO: Consider better SFX for Invoke Rune.
            .WithEffectOnEachTarget(async (thisInvokeAction, caster, target, result) =>
            {
                await Rune.InvokeDrawnRune(thisInvokeAction, caster, target, runeTarget);
            });

        // Saving Throw Tooltip Creator
        Trait saveTrait =
            this.InvokeTechnicalTraits.FirstOrDefault(trait => trait is Trait.Reflex or Trait.Fortitude or Trait.Will);
        if (!this.InvokeTechnicalTraits.Contains(Trait.DoesNotRequireAttackRollOrSavingThrow) && saveTrait != Trait.None)
        {
            Defense def = saveTrait switch
            {
                Trait.Reflex => Defense.Reflex,
                Trait.Fortitude => Defense.Fortitude,
                Trait.Will => Defense.Will
            };
            invokeThisRune.WithTargetingTooltip((thisInvokeAction, target, index) =>
            {
                string tooltip = CombatActionExecution.BreakdownSavingThrowForTooltip(thisInvokeAction, target,
                    new SavingThrow(def, RunesmithPlaytest.RunesmithDC(caster))).TooltipDescription;
                return initialDescription
                       + this.WithInvocationTextFormatting(this.InvocationTextWithHeightening(this, caster.Level))
                       + "\n" + tooltip;
            });
        }

        if (withImmediatelyRemoveImmunity)
        {
            invokeThisRune = WithImmediatelyRemovesImmunity(invokeThisRune);
        }

        return invokeThisRune;
    }
    
    #endregion

    #region Static Methods

    /// <summary>
    /// Removes all invocation immunities from a creature.
    /// </summary>
    /// <param name="cr">The <see cref="Creature"/> whose QEffects will be searched.</param>
    /// <returns>(bool) True if at least one QEffect with the <see cref="Enums.Traits.InvocationImmunity"/> trait was removed, false otherwise.</returns>
    public static bool RemoveAllImmunities(Creature cr)
    {
        int removals = cr.RemoveAllQEffects(
            qf =>
                qf.Traits.Contains(Enums.Traits.InvocationImmunity)
        );
        return (removals > 0);
    }

    public static async Task InvokeDrawnRune(
        CombatAction sourceAction,
        Creature caster,
        Creature runeBearer,
        DrawnRune runeToInvoke
        )
    {
        Rune thisRune = runeToInvoke.Rune;
        
        if (thisRune.InvocationBehavior == null || runeToInvoke.Disabled)
            return;
        
        foreach (Creature cr in caster.Battle.AllCreatures)
        {
            List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null, cr);
            foreach (DrawnRune dr in drawnRunes)
            {
                if (dr.BeforeInvokingRune != null)
                {
                    await dr.BeforeInvokingRune.Invoke(dr, runeToInvoke);
                }
            }
        }
        
        await thisRune.InvocationBehavior.Invoke(sourceAction, thisRune, caster, runeBearer, runeToInvoke);
        
        foreach (Creature cr in caster.Battle.AllCreatures)
        {
            List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null, cr);
            foreach (DrawnRune dr in drawnRunes)
            {
                if (dr.AfterInvokingRune != null)
                {
                    await dr.AfterInvokingRune.Invoke(dr, runeToInvoke);
                }
            }
        }
    }

    /// <summary>
    /// The CASTING creature uses the SOURCE combat action to attempt to DRAW a rune on a CREATURE.
    /// </summary>
    /// <param name="sourceAction">The <see cref="CombatAction"/> which is attempting to draw the rune.</param>
    /// <param name="caster">The <see cref="Creature"/> drawing the rune.</param>
    /// <param name="targetFilter">(nullable) A lambda which returns true if the Creature is a valid option to target.</param>
    /// <param name="range">(default: 6) if a target is not provided, this range is used for tracing a rune.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to draw.</param>
    /// <param name="canBeCanceled">Whether the attempt to draw the rune can be canceled.</param>
    public static async Task PickACreatureAndDrawARune(
        CombatAction? sourceAction,
        Creature caster,
        Func<Creature, bool>? targetFilter = null,
        int? range = 6,
        Func<Rune, bool>? runeFilter = null,
        bool? canBeCanceled = false)
    {
        // Get available runes
        RunicRepertoireFeat? repertoireFeat = RunicRepertoireFeat.GetRepertoireOnCreature(caster);
        if (repertoireFeat == null)
            return;
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in repertoireFeat.GetRunesKnown(caster))
        {
            if (runeFilter == null || runeFilter.Invoke(rune) == true)
            {
                CombatAction traceThisRuneAction = rune.CreateTraceAction(caster, 2, range).WithActionCost(0);
                traceThisRuneAction.Description = rune.CreateTraceActionDescription(traceThisRuneAction, withFlavorText:false);
                GameLoop.AddDirectUsageOnCreatureOptions(
                    traceThisRuneAction, // Use at normal range.
                    options,
                    false);
            }
        }
        
        // Remove options if a target is specified
        if (targetFilter != null)
            options.RemoveAll(
                option => option is CreatureOption crOpt && !targetFilter.Invoke(crOpt.Creature));
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return;
        if (canBeCanceled == true)
            options.Add(new CancelOption(true));
        options.Add(new PassViaButtonOption(" Confirm no trace action "));
        
        // Pick a target
        string topBarText = "Choose a rune to Trace"
                            //+ (target != null ? $" on {target.Name}" : null)
                            + (canBeCanceled == true ? " or right-click to cancel" : null)
                            + ".";
        Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
            new AdvancedRequest(caster, "Choose a rune to Trace.", options)
            {
                TopBarText = topBarText,
                TopBarIcon = Enums.Illustrations.TraceRune,
            })).ChosenOption;
        
        // Do stuff based on specific type of choice
        switch (chosenOption)
        {
            case CreatureOption creatureOption:
            {
                break;
            }
            case CancelOption:
                if (sourceAction != null)
                    sourceAction.RevertRequested = true;
                return;
            case PassViaButtonOption:
                return;
        }
        
        // Execute chosen option
        await chosenOption.Action();
    }

    public static async Task<DrawnRune?> PickARuneToDrawOnCreature(
        CombatAction? sourceAction,
        Creature caster,
        Creature target,
        int? range = 6,
        Func<Rune, bool>? runeFilter = null,
        bool? canBeCanceled = false
        )
    {
        // Get available runes
        RunicRepertoireFeat? repertoireFeat = RunicRepertoireFeat.GetRepertoireOnCreature(caster);
        if (repertoireFeat == null)
            return null;
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in repertoireFeat.GetRunesKnown(caster))
        {
            if (runeFilter == null || runeFilter.Invoke(rune) == true)
            {
                CombatAction traceThisRuneAction = rune.CreateTraceAction(caster, 2, range).WithActionCost(0);
                traceThisRuneAction.Description = rune.CreateTraceActionDescription(traceThisRuneAction, withFlavorText:false);
                GameLoop.AddDirectUsageOnCreatureOptions(
                    traceThisRuneAction, // Use at normal range.
                    options,
                    false);
            }
        }
        
        // Remove options not on the target
            options.RemoveAll(option => 
                option is CreatureOption crOpt && crOpt.Creature != target);
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return null;
        if (canBeCanceled == true)
            options.Add(new CancelOption(true));
        options.Add(new PassViaButtonOption(" Confirm no trace action "));
        
        // Pick a target
        string topBarText = "Choose a rune to Trace"
                            + $" on {target.Name}"
                            + (canBeCanceled == true ? " or right-click to cancel" : null)
                            + ".";
        Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
            new AdvancedRequest(caster, "Choose a rune to Trace.", options)
            {
                TopBarText = topBarText,
                TopBarIcon = Enums.Illustrations.TraceRune,
            })).ChosenOption;
        
        // Do stuff based on specific type of choice
        switch (chosenOption)
        {
            case CreatureOption creatureOption:
            {
                break;
            }
            case CancelOption:
                if (sourceAction != null)
                    sourceAction.RevertRequested = true;
                return null;
            case PassViaButtonOption:
                return null;
        }
        
        // Execute chosen option
        if (await chosenOption.Action())
        {
            
        }

        return null;
    }

    /// <summary>
    /// The CASTING creature uses the SOURCE combat action to INVOKE a DrawnRune on the TARGET creature.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which invoked the rune.</param>
    /// <param name="caster">The Creature invoking the DrawnRune.</param>
    /// <param name="target">The Creature whose DrawnRune will be invoked. If null, you'll be asked to select a Creature with a DrawnRune.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to invoke.</param>
    /// <param name="canBeCanceled">Whether the attempt to invoke the rune can be canceled.</param>
    public static async Task PickARuneToInvokeOnTarget(
        CombatAction sourceAction,
        Creature caster,
        Creature? target = null,
        Func<Rune, bool>? runeFilter = null,
        bool? canBeCanceled = false)
    {
        // Get available runes
        RunicRepertoireFeat? repertoireFeat = RunicRepertoireFeat.GetRepertoireOnCreature(caster);
        if (repertoireFeat == null)
            return;
        
        // Generate options
        List<Option> options = [];
        foreach (Creature cr in caster.Battle.AllCreatures)
        {
            foreach (QEffect qf in cr.QEffects)
            {
                if (qf is not DrawnRune dRune)
                    continue;
                if (dRune.Source != caster
                    || !dRune.Traits.Contains(Enums.Traits.Rune)
                    || dRune.Traits.Contains(Enums.Traits.Invocation))
                    continue;
                
            }
        }
        
        foreach (Rune rune in repertoireFeat.GetRunesKnown(caster))
        {
            if (runeFilter == null || runeFilter.Invoke(rune) == true)
                foreach (Creature cr in caster.Battle.AllCreatures)
                {
                    foreach (QEffect runeQf in cr.QEffects.Where(qf =>
                                 qf is DrawnRune dRune 
                                 && dRune.Rune == rune
                                 && dRune.Source == caster
                                 && dRune.Traits.Contains(Enums.Traits.Rune)
                                 && !dRune.Traits.Contains(Enums.Traits.Invocation)))
                    {
                        if (runeQf is not DrawnRune dRune)
                            continue;
                        CombatAction? newInvokeAction =
                            rune.CreateInvokeAction(sourceAction, caster, dRune)
                                ?.WithActionCost(0); // Use at normal range.
                        if (newInvokeAction != null)
                            GameLoop.AddDirectUsageOnCreatureOptions(
                                newInvokeAction,
                                options,
                                false);
                    }
                }
        }
        
        // Remove options if a target is specified
        if (target != null)
            options.RemoveAll(
                option => option is CreatureOption crOpt && crOpt.Creature != target);
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return;
        if (canBeCanceled == true)
            options.Add(new CancelOption(true));
        options.Add(new PassViaButtonOption(" Confirm no trace action "));
        
        // Pick a target
        string topBarText = "Choose a rune to Invoke"
                            + (target != null ? $" on {target.Name}" : null)
                            + (canBeCanceled == true ? " or right-click to cancel" : null)
                            + ".";
        Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
            new AdvancedRequest(caster, "Choose a rune to Invoke.", options)
            {
                TopBarText = topBarText,
                TopBarIcon = Enums.Illustrations.TraceRune,
            })).ChosenOption;
        
        // Do stuff based on specific type of choice
        switch (chosenOption)
        {
            case CreatureOption creatureOption:
            {
                break;
            }
            case CancelOption:
                if (sourceAction != null)
                    sourceAction.RevertRequested = true;
                return;
            case PassViaButtonOption:
                return;
        }
        
        // Execute chosen option
        await chosenOption.Action();
        
        /*if (targetMustHaveRune == true &&
            target.QEffects.FirstOrDefault(qfToFind => qfToFind == runeTarget) == null) 
            return;

        if (!this.IsImmuneToThisInvocation(target))
        {
            await runeTarget.Rune.InvocationBehavior?.Invoke(sourceAction, runeTarget.Rune, caster, target, runeTarget)!;
        }
        
        // Remove drawn rune after invoking it
        runeTarget.Rune.RemoveDrawnRune(runeTarget);
        // Apply immunity to the creature it was invoked on
        runeTarget.Rune.ApplyImmunity(target); // THIS WILL NOT WORK. SOME RUNES HAVE SUBSIDIARY BEHAVIOR THAT INVOKES ON ANOTHER CREATURE.*/
    }

    /// <summary>
    /// Adds (or replaces, if it already exists) the WithEffectOnChosenTargets behavior of the given CombatAction with a function that removes immunity from simultaneous invocations at the end of the action.
    /// </summary>
    /// <param name="anyCombatAction"></param>
    /// <returns>(CombatAction) the anyCombatAction that was passed, with the new WithEffectOnChosenTargets.</returns>
    public static CombatAction WithImmediatelyRemovesImmunity(CombatAction anyCombatAction)
    {
        anyCombatAction.WithEffectOnChosenTargets(async (caster, targets) =>
        {
            foreach (Creature cr in caster.Battle.AllCreatures)
            {
                Rune.RemoveAllImmunities(cr);
            }
        });
        
        return anyCombatAction;
    }

    public static Skill? GetSkillFromTraditionTrait(Trait traditionTrait)
    {
        switch (traditionTrait)
        {
            case Trait.Arcane:
                return Skill.Arcana;
            case Trait.Divine:
                return Skill.Religion;
            case Trait.Occultism:
                return Skill.Occultism;
            case Trait.Primal:
                return Skill.Nature;
            default:
                return null;
        }
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