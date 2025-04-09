using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithPlaytest;

// I need this link. A lot.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments

public class Rune
{
    # region Instance Properties
    
    /// <summary>
    /// The name of the rune.
    /// <code>
    /// newRune.Name = "Atryl, Rune of Fire"
    /// </code>
    /// </summary>
    public string Name { get; set; } 
    
    /// <summary>
    /// The unique trait which corresponds to instances of this particular kind of rune, such as Atryl, Rune of Fire.
    /// </summary>
    public Trait RuneId { get; set; }
    
    /// <summary>
    /// The original level of the Rune, before it increases with character level. This corresponds to the CHARACTER LEVEL required to learn the Rune. 
    /// </summary>
    public int BaseLevel  { get; set; }
    
    /// <summary>
    /// The unformatted text describing the usage of the rune.
    /// <code>
    /// newRune.UsageText = "drawn on a shield"
    /// </code>
    /// </summary>
    public string UsageText { get; set; }

    /// <summary>
    /// Gets the rune's <see cref="UsageText"/> with formatting.
    /// </summary>
    /// <returns>(string) The original text prepended with "{b}Usage{/b} ".</returns>
    public string UsageTextWithFormatting()
    {
        return "{b}Usage{/b} " + this.UsageText;
    }
    
    /// <summary>
    /// The unformatted flavor text of the rune.
    /// <code>
    /// newRune.FlavorText = "This serrated rune, when placed on a blade, ensures it will never go dull."
    /// </code>
    /// </summary>
    public string? FlavorText { get; set; }

    /// <summary>
    /// Gets the rune's <see cref="FlavorText"/> with formatting.
    /// </summary>
    /// <returns>(string) The original text surrounded with "{b}" and "{/b}".</returns>
    public string FlavorTextWithFormatting()
    {
        return "{i}" + this.FlavorText + "{/i}";
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
    public string? InvocationTextWithFormatting()
    {
        return this.InvocationText != null ? "{b}Invocation{/b} " + this.InvocationText : null;
    }
    
    /// <summary>
    /// Every Rune defines how it displays its text when heightened. Unless otherwise defined, this lambda returns InvocationText by default.
    /// </summary>
    /// <param name="Rune">The rune's text to use.</param>
    /// <param name="int">The CHARACTER LEVEL to heighten the rune to.</param>
    /// <returns>(string) The InvocationText with heightened behavior.</returns>
    /// <seealso cref="Dawnsbury.Display.Text.S"/>
    public Func<Rune, int, string> InvocationTextWithHeightening { get; set; } =
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
    public string? LevelTextWithFormatting()
    {
        return this.LevelText != null ? "{b}Level (" + this.LevelFormat + "){/b} " + this.LevelText : null;
    }
    
    /// <summary>
    /// The Rune's icon.
    /// </summary>
    public Illustration Illustration { get; set; }
    
    /// <summary>
    /// The traits associated with the rune. By default, all runes have at least the Rune, Runesmith, and Magical traits.
    /// </summary>
    public List<Trait> Traits { get; set; } = [ModTraits.Rune, ModTraits.Runesmith, Trait.Magical];

    /// <summary>
    /// A very-abstract list of traits to associate with attempts to draw runes. Examples include the <see cref="Trait.Shield"/> trait on Holtrik to represent that the rune is used on a shield, which is used by Fortifying Knock when filtering allowable runes.
    /// </summary>
    public List<Trait> DrawTechnicalTraits { get; set; } = [];
    
    /// <summary>
    /// A very-abstract list of traits to associate with attempts to invoke runes. Examples include the <see cref="Trait.IsHostile"/> trait on Atryl to represent that the invocation deals damage, which is used by Runic Reprisal when filtering allowable runes.
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
    
    #endregion
    
    #region Instance Methods

    /// <summary>
    /// Gets the full description block for the Rune with formatting, optionally with the flavor text.
    /// </summary>
    /// <param name="withFlavorText">Whether to include <see cref="FlavorTextWithFormatting"/> in the return.</param>
    /// <returns>(string) The full description with formatting.</returns>
    public string GetFullyFormattedDescription(bool withFlavorText = true)
    {
        string description = 
            (withFlavorText ? this.FlavorTextWithFormatting() + "\n\n" : null) +
            this.UsageTextWithFormatting() + "\n\n" +
            this.PassiveText +  "\n\n" +
            (this.InvocationTextWithFormatting() != null ? this.InvocationTextWithFormatting() + "\n\n" : null) +
            (this.LevelTextWithFormatting() != null ? this.LevelTextWithFormatting() : null);
        return description;
    }
    
    /// <summary>
    /// Generates a fully-formatted description with an inserted piece of text into the final string.
    /// </summary>
    /// <param name="insertionPoint">Where to insert an extra string. 0 = before flavor text, 1 = before usage text, 2 = before passive text, 3 = before invocation text, 4 = before level text, 5 = end of the description</param>
    /// <param name="textToInsert">Text to insert into the description.</param>
    /// <returns></returns>
    public string GetFullyFormattedDescriptionWithInsertion(int insertionPoint, string textToInsert)
    {
        List<string> disassembledDescription = [
            this.FlavorTextWithFormatting() + "\n\n",
            this.UsageTextWithFormatting() + "\n\n",
            this.PassiveText +  "\n\n",
            (this.InvocationTextWithFormatting() != null ? this.InvocationTextWithFormatting() + "\n\n" : ""),
            ((this.LevelTextWithFormatting() != null ? this.LevelTextWithFormatting() : "")!)];
        disassembledDescription.Insert(insertionPoint, textToInsert);

        return disassembledDescription.Aggregate("", (current, text) => current + (text != "" ? text : null));
    }
    
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
            Illustration = new SuperimposedIllustration(this.Illustration, RunesmithPlaytest.NoSymbolIllustration),
            Tag = this,
            Traits = [ModTraits.InvocationImmunity, this.RuneId], // ImmunityQFs are identified by these traits.
            ExpiresAt = ExpirationCondition.ExpiresAtEndOfAnyTurn, // This QF is supposed to be removed when the activity making invokeActions completes. This is a back-up safety for developer-error.
            DoNotShowUpOverhead = true,
        };
        invokeTarget.AddQEffect(runeInvocationImmunity);
        return runeInvocationImmunity;
    }
    
    /// <summary>
    /// Determines whether a TARGET Creature is immune to the invocation effects of this Rune by searching for a QEffect with the <see cref="ModTraits.InvocationImmunity"/> trait and a trait matching this Rune's <see cref="RuneId"/>.
    /// </summary>
    /// <param name="target">The CREATURE to check.</param>
    /// <returns>(bool) Returns true if the immunity QEffect is present on the TARGET.</returns>
    public bool IsImmuneToThisInvocation(Creature target)
    {
        QEffect? thisRunesImmunity = target.QEffects.FirstOrDefault(qfToFind =>
            qfToFind.Traits.Contains(ModTraits.InvocationImmunity) &&
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
            }
            );
        return (removals > 0);
    }
    
    /// <summary>
    /// The CASTER uses an ACTION to apply the RUNE's <see cref="NewDrawnRune"/> to the TARGET, which might IGNORE targeting restrictions.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which is applying the rune. This action should have either the <see cref="ModTraits.Traced"/> or the <see cref="ModTraits.Etched"/> traits to determine the duration of the effect being applied.</param>
    /// <param name="caster">The Creature applying the rune.</param>
    /// <param name="target">The Creature the rune is applying to.</param>
    /// <param name="ignoreUsageRequirements">(Default: false) If false, then the DrawnRune is applied only when its Rune's <see cref="UsageCondition"/> is valid for the target. This is true for cases like the Runic Reprisal feat which allows a Runesmith to apply any damaging rune to their shield, taking none of the passive effects, but allowing it to be invoked on a creature when they Shield Block.</param>
    /// <returns>(bool) True if the effect was successfully applied to the target, false otherwise.</returns>
    public async Task<bool> DrawRuneOnTarget(
        CombatAction sourceAction,
        Creature caster,
        Creature target,
        bool ignoreUsageRequirements = false)
    {
        // Apply the QF if "ignoreTargetRestrictions is True", or "UsageCondition isn't null, and it returns Usable on the target".
        if (!ignoreUsageRequirements &&
            (this.UsageCondition == null || this.UsageCondition.Invoke(caster, target) != Usability.Usable)) 
            return false;
        
        DrawnRune qfToApply = await this.NewDrawnRune.Invoke(sourceAction, caster, target, this);

        if (qfToApply == null)
            return false;
        
        // Determine the way the rune is being applied.
        if (sourceAction.HasTrait(ModTraits.Etched))
            qfToApply = qfToApply.WithIsEtched();
        else if (sourceAction.HasTrait(ModTraits.Traced))
            qfToApply = qfToApply.WithIsTraced();
        
        target.AddQEffect(qfToApply);

        return true;
    }

    /// <summary>
    /// Creates a generic CombatAction which when executed, calls <see cref="DrawRuneOnTarget"/> on each target using this Rune. This action inherits the mechanics of Tracing a Rune, such as the <see cref="ModTraits.Traced"/> and Manipulate traits.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <param name="actions">The number of actions for this variant. If actions==-3, a 1-2 action variable target is used. If actions==1, an adjacent target is used. If actions==2, a ranged target is used (6 tiles). Otherwise, a Self target is used. The action cost can still be altered afterward (such as for use in subsidiary actions).</param>
    /// <returns>(CombatAction) The action which traces the given rune on the target.</returns>
    public CombatAction CreateTraceAction(
        Creature owner,
        int actions = 0)
    {
        // Determine description based on actions preset
        string actionDescription;
        switch (actions)
        {
            case -3:
                actionDescription = this.GetFullyFormattedDescriptionWithInsertion(2,
                    "{icon:Action} The range is touch.\n{icon:TwoActions} The range is 30 feet.\n\n");
                break;
            case 1:
                actionDescription = this.GetFullyFormattedDescriptionWithInsertion(0,
                    "{b}Range{/b} touch\n\n");
                break;
            case 2:
                actionDescription = this.GetFullyFormattedDescriptionWithInsertion(0,
                    "{b}Range{/b} 30 feet\n\n");
                break;
            default:
                actionDescription = this.GetFullyFormattedDescription();
                break;
        }

        // Determine Target Properties
        DependsOnActionsSpentTarget varyTarget;
        CreatureTarget adjacentTarget = Target.AdjacentCreatureOrSelf();
        CreatureTarget rangedTarget = Target.RangedCreature(6);
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
                    attacker.HasFreeHand ? Usability.Usable : Usability.NotUsable("You must have a free hand to trace a rune"));
            if (this.UsageCondition != null)
                crTar.WithAdditionalConditionOnTargetCreature(this.UsageCondition); // UsageCondition
        }
        
        // Determine traits
        Trait[] traits = this.Traits.ToArray().Concat([Trait.Concentrate, Trait.Magical, Trait.Manipulate, ModTraits.Traced]).ToArray();
        
        // Create action
        CombatAction drawRuneAction = new CombatAction(
            owner,
            this.Illustration!, // Suppress
            "Trace " + this.Name,
            traits,
            actionDescription,
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
                if (!await actionRune.DrawRuneOnTarget(thisAction, caster, target))
                    thisAction.RevertRequested = true;
            });
        if (actions == -3)
        {
            drawRuneAction.WithCreateVariantDescription((actions, spellVariant) =>
            { // Just having this gives the variant range information.
                return actions switch
                {
                    //1 => rune.GetFullyFormattedDescription(false),
                    //2 => rune.GetFullyFormattedDescription(false),
                    _ => this.GetFullyFormattedDescription(false)
                };
            });
        }
        
        return drawRuneAction;
    }

    /// <summary>
    /// Creates a variant of <see cref="CreateTraceAction"/> that inherits the mechanics of Etching a Rune, such as the <see cref="ModTraits.Etched"/> trait and a map-sized range limit, and only applying runes to allies.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <returns>(CombatAction) The action which etches the given rune on the target.</returns>
    public CombatAction CreateEtchAction(
        Creature owner)
    {
        CombatAction etchAction = this.CreateTraceAction(owner, 2)
            .WithActionCost(0).WithSoundEffect(SfxName.AttachRune);
        etchAction.Name = $"Etch {this.Name}";
        etchAction.Traits.Remove(ModTraits.Traced);
        etchAction.Traits.Remove(Trait.Manipulate); // Just in case this might provoke a reaction.
        etchAction.Traits.Remove(Trait.Concentrate); // Just in case this might provoke a reaction.
        etchAction.Traits.Add(ModTraits.Etched);
        etchAction.Target = Target.RangedFriend(99);
        if (this.UsageCondition != null) // Do this again since we just replaced the target.
            (etchAction.Target as CreatureTarget)!.WithAdditionalConditionOnTargetCreature(this.UsageCondition);
        /* We don't add a free hand requirement here since this "technically" happened "before" combat. */
        return etchAction;
    }

    /// <summary>
    /// DOC: CreateInvokeAction
    /// </summary>
    /// <param name="sourceAction"></param>
    /// <param name="caster"></param>
    /// <param name="runeTarget"></param>
    /// <param name="range"></param>
    /// <param name="withImmediatelyRemoveImmunity"></param>
    /// <returns></returns>
    public CombatAction? CreateInvokeAction(
        CombatAction sourceAction,
        Creature caster,
        DrawnRune runeTarget,
        int range = 6,
        bool withImmediatelyRemoveImmunity = false)
    {
        if (this.InvocationBehavior == null)
            return null;
        
        Trait[] traits = this.Traits.ToArray();
        traits = traits.Append(ModTraits.Invocation).ToArray();
        
        CombatAction invokeThisRune = new CombatAction(
            caster,
            this.Illustration,
            "Invoke " + this.Name,
            traits,
            this.InvocationTextWithHeightening(this, caster.Level),
            Target.RangedCreature(range)
                .WithAdditionalConditionOnTargetCreature((attacker, defender) =>
                {
                    QEffect? foundQf = defender.QEffects.FirstOrDefault(
                        qfToFind => qfToFind == runeTarget);
                    return foundQf != null ? Usability.Usable : Usability.NotUsableOnThisCreature($"{this.Name} not applied");
                })
            )
            .WithActionCost(0)
            .WithSoundEffect(SfxName.DazzlingFlash) // TODO: Consider better SFX for Invoke Rune.
            .WithEffectOnEachTarget(async (thisInvokeAction, caster, target, result) =>
            {
                await this.InvocationBehavior.Invoke(sourceAction, this, caster, target, runeTarget);
            });

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
    /// <returns>(bool) True if at least one QEffect with the <see cref="ModTraits.InvocationImmunity"/> trait was removed, false otherwise.</returns>
    public static bool RemoveAllImmunities(Creature cr)
    {
        int removals = cr.RemoveAllQEffects(
            qf =>
                qf.Traits.Contains(ModTraits.InvocationImmunity)
        );
        return (removals > 0);
    }

    /// <summary>
    /// DOC: pick a creature and draw a rune
    /// </summary>
    /// <param name="caster"></param>
    /// <param name="target">(nullable) The creature to draw the rune onto. If null, then any creature within 6 tiles can be a valid target.</param>
    public static async Task PickACreatureAndDrawARune(
        CombatAction? sourceAction,
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
        foreach (Rune rune in repertoireFeat.RunesKnown)
        {
            if (runeFilter == null || runeFilter.Invoke(rune) == true)
                GameLoop.AddDirectUsageOnCreatureOptions(
                    rune.CreateTraceAction(caster, 2).WithActionCost(0), // Use at normal range.
                    options,
                    false);
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
        string topBarText = "Choose a rune to Trace"
                            + (target != null ? $" on {target.Name}" : null)
                            + (canBeCanceled == true ? " or right-click to cancel" : null)
                            + ".";
        Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
            new AdvancedRequest(caster, "Choose a rune to Trace.", options)
            {
                TopBarText = topBarText,
                TopBarIcon = RunesmithPlaytest.TraceRuneIllustration,
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
    
    /// <summary>
    /// The CASTER performs INVOKE ACTION on TARGET which might require they HAVE THE RUNE applied to them.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which invoked the rune.</param>
    /// <param name="caster">The Creature invoking the Rune.</param>
    /// <param name="target">The Creature taking the effects of the Rune's invocation.</param>
    /// <param name="runeTarget">The <see cref="DrawnRune"/> being invoked, to be removed after this action completes.</param>
    /// <param name="targetMustHaveRune">(Default: true) If true, then the target creature must have the qfPassiveInstance for the method to complete. This is false for cases like the Runic Reprisal feat which allows a Runesmith to apply any damaging rune to their shield, but only for the purposes of invoking the rune on a creature whose attack they Shield Blocked.</param>
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
                    || !dRune.Traits.Contains(ModTraits.Rune)
                    || dRune.Traits.Contains(ModTraits.Invocation))
                    continue;
                
            }
        }
        
        foreach (Rune rune in repertoireFeat.RunesKnown)
        {
            if (runeFilter == null || runeFilter.Invoke(rune) == true)
                foreach (Creature cr in caster.Battle.AllCreatures)
                {
                    foreach (QEffect runeQf in cr.QEffects.Where(qf =>
                                 qf is DrawnRune dRune 
                                 && dRune.Rune == rune
                                 && dRune.Source == caster
                                 && dRune.Traits.Contains(ModTraits.Rune)
                                 && !dRune.Traits.Contains(ModTraits.Invocation)))
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
                TopBarIcon = RunesmithPlaytest.TraceRuneIllustration,
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
    /// DOC: ImmediatelyRemovesImmunity. Note that it replaces the action's WithEffectOnChosenTargets.
    /// </summary>
    /// <param name="anyCombatAction"></param>
    /// <returns></returns>
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
        string? invocationText,
        string? levelText,
        List<Trait>? additionalTraits)
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

/* QEffect Properties to utilize
 * .Key     for anti-stacking behavior
 * .AppliedThisStateCheck
 * .Hidden
 * .HideFromPortrait
 * .Tag
 * .UsedThisTurn
 * .Value
 * .Source
 * .SourceAction
 * .Owner
 */