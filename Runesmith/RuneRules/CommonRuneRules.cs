using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Monsters.L10;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Controls;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.TargetingRequirements;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

public static class CommonRuneRules
{
    #region String Constants

    public const string TRAIT_DESCRIPTION_RUNE_TRADITION =
        """
        Some runes have a specific tradition trait, such as {i}ranshu, rune of thunder{/i} which is a primal rune. If a rune lacks a specific tradition, then it qualifies as any tradition whose skill you're trained in that matches that tradition. For example, if you were trained in both Nature and Religion, then a {i}pluuna, rune of illumination{/i} rune would count as both a Primal and a Divine rune. This is a deviation from tabletop.
        {b}Arcane{/b} Arcana
        {b}Divine{/b} Religion 
        {b}Nature{/b} Nature
        {b}Occult{/b} Occultism
        """;
    
    public const string TRAIT_DESCRIPTION_RUNE =
        $$"""
          Runesmith runes are magical effects applied to objects and creatures, and they're affected by things which also affect spells (such as being prevented from casting spells).

          Runes can be applied via etching or tracing. Etched runes are applied before combat, last indefinitely, and you can only have a limited number of them; while traced runes last only until the end of your next turn and you can have any number at a time. Their effects, however, are the same.

          A rune automatically heightens to your level, and some runes increase in power when they do, as described in their {b}Level{/b} entry.

          Several abilities refer to creatures bearing one of your runes, known as rune-bearers: this is any creature who has one of your runes applied to its body or to any gear it is wearing or holding.

          {{TRAIT_DESCRIPTION_RUNE_TRADITION}}
          """;

    public const string TRAIT_DESCRIPTION_INVOCATION =
        """
        An invocation action allows a runesmith to surge power through their rune by uttering its true name.
         
        Invocation requires you to be able to speak clearly in a strong voice and requires that you be within 30 feet of the target rune or runes unless another ability changes this. These actions include the Invoke Rune action, which makes the rune’s invocation take place, then causes the rune to fade away.

        Using Invoke Rune normally allows you to invoke two runes, but other abilities that include the Invoke Rune action often change that number or the specific runes you can invoke.
        """;

    public const string TRAIT_DESCRIPTION_DIACRITIC =
        """
        A diacritic is a special type of rune that’s not applied directly to a creature or object, but rather drawn on another rune itself, modifying or empowering that base rune.

        A diacritic can never be applied by itself, and any effect that would remove or invoke the base rune always also removes or invokes the diacritic rune.

        A rune can have only one diacritic. A base rune with a diacritic counts as one rune for the purposes of invoking.
        """;

    // ReSharper disable once InconsistentNaming
    public static readonly (string FLAVOR, string RULES) ACTION_DESCRIPTION_TRACE_RUNE = (
        "Your fingers dance, glowing light leaving behind the image of a rune.",
        """
        You apply one rune to an adjacent target matching the rune's Usage entry. The rune remains until the end of your next turn. If you spend {icon:TwoActions} 2 actions to Trace a Rune, you draw the rune in the air, causing it to appear on a target within 30 feet.

        You can have any number of runes traced at a time.
        """
    );
    
    // ReSharper disable once InconsistentNaming
    public static readonly (string FLAVOR, string RULES) ACTION_DESCRIPTION_INVOKE_RUNE = (
        "Magic on your lips, you pronounce the true name of a rune you have applied, calling it to power.",
        """
        You utter the name of up to two of your runes within 30 feet, and each of which blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed.

        No matter how many runes you invoke with a single action, creatures that would be affected by multiple copies of the same rune are {Red}affected only once{/Red}, as normal for duplicate effects.
        """);

    #endregion
    
    #region Action Information

    /// <summary>
    /// Generates a description block for this rune's Trace actions.
    /// </summary>
    /// <param name="rune">The rune being traced.</param>
    /// <param name="smithLevel">The level of the runesmith tracing the rune.</param>
    /// <param name="withFlavorText">Whether to include flavor text in the description (typically false for dropdown options).</param>
    /// <param name="prologueText">The paragraph to add at the top of the description (includes one line-break after).</param>
    /// <param name="afterFlavorText">The text to add at the end of the flavor text paragraph.</param>
    /// <param name="withUsageText">Whether to include the usage text in the description (typically false for dropdown options).</param>
    /// <param name="afterUsageText">The text to add at the end of the usage text paragraph.</param>
    /// <param name="afterPassiveText">The text to add at the end of the passive text paragraph.</param>
    /// <param name="afterInvocationText">The text to add at the end of the invocation text paragraph.</param>
    /// <param name="epilogueText">The paragraph to add at the bottom of the description (includes one line-break before).</param>
    /// <returns></returns>
    public static string CreateTraceActionDescription(
        Rune rune,
        int smithLevel,
        bool withFlavorText = true,
        string? prologueText = null,
        string? afterFlavorText = null,
        bool withUsageText = true,
        string? afterUsageText = null,
        string? afterPassiveText = null,
        string? afterInvocationText = null,
        string? epilogueText = null)
    {
        string usageText = (withUsageText ? rune.DrawProperties.UsageTextWithFormatting : null) + afterUsageText;
        string flavorText = (withFlavorText ? rune.GetFlavorText() : null) + afterFlavorText;
        string passiveText = rune.PassiveProperties.PassiveTextWithHeightening(rune, smithLevel) + afterPassiveText;
        string invocationText = rune.InvocationProperties.InvocationTextWithFormattedHeightening?.Invoke(rune, smithLevel) + afterInvocationText;
        //string? levelText = this.WithLevelTextFormatting(); // Should have heightening, so this shouldn't be necessary.
        return (!string.IsNullOrEmpty(prologueText) ? $"{prologueText}\n" : null)
               + (!string.IsNullOrEmpty(flavorText) ? $"{flavorText}\n\n": null)
               + (!string.IsNullOrEmpty(usageText) ? $"{usageText}\n\n" : null)
               + passiveText
               + (!string.IsNullOrEmpty(invocationText) ? $"\n\n{invocationText}" : null)
               + (!string.IsNullOrEmpty(epilogueText) ? $"\n{epilogueText}" : null);
               //+ (levelText != null ? $"\n\n{levelText}" : null);
    }

    /// <summary>
    /// Gets the full description block for the Rune with formatting, optionally with the flavor text.
    /// </summary>
    /// <param name="rune">The rune to use.</param>
    /// <param name="withFlavorText">Whether to include <see cref="Rune.GetFlavorText"/> in the return.</param>
    /// <returns>(string) The full description with formatting.</returns>
    public static string GetFormattedFeatDescription(Rune rune, bool withFlavorText = true)
    {
        string description = 
            (withFlavorText ? rune.GetFlavorText() + "\n\n" : null) +
            rune.DrawProperties.UsageTextWithFormatting + "\n\n" +
            rune.PassiveProperties.PassiveText +
            (rune.InvocationProperties.HasInvocationEntry ? "\n\n" + rune.InvocationProperties.InvocationTextWithFormatting : null) +
            (rune.LevelText != null ? "\n\n" + rune.GetFormattedLevelText() : null);
        return description;
    }

    /// <summary>
    /// Adds a <see cref="RuneActionTag"/> to this CombatAction.
    /// </summary>
    /// <param name="action">The action gaining this information.</param>
    /// <param name="rune">The inner Rune which is being drawn or invoked.</param>
    /// <param name="createdRune">The DrawnRune instance created by this action, if any.</param>
    /// <param name="chosenCreature">The Creature targeted by this rune-drawing CombatAction, if any.</param>
    /// <param name="chosenRune">The DrawnRune targeted by this invocation or diacritic-drawing action, if any.</param>
    /// <param name="chosenItem">The Item targeted by this invocation or </param>
    public static CombatAction WithRuneTag(
        this CombatAction action,
        Rune rune,
        DrawnRune? createdRune = null,
        Creature? chosenCreature = null,
        DrawnRune? chosenRune = null,
        Item? chosenItem = null)
    {
        RuneActionTag tag = new RuneActionTag(rune, createdRune, chosenCreature, chosenRune, chosenItem)
        {
            Owner = action
        };
        return action.WithTag(tag);
    }
    
    #endregion

    #region Drawing Runes

    public static List<Item> GetCommonTargetableItems(Creature itemBearer)
    {
        // Armor is handled as a creature property requirement
        // rather than an item target.
        return itemBearer.HeldItems
            .Union(itemBearer.CarriedItems.Where(item => item.IsWorn)) // This will handle certain worn items, such as Bucklers from MoreShields.
            .Union(itemBearer.Weapons) // This will handle unarmed attacks.
            .ToList();
    }
    
    /// <summary>
    /// Creates a generic CombatAction that executes <see cref="DrawRuneOnTarget"/> on each target using this Rune.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <param name="rune">The rune to create an action for.</param>
    /// <param name="actions">The number of actions for this variant. If actions==-3, a 1-2 action variable target is used. If actions==1, an adjacent target is used. If actions==2, a ranged target is used (6 tiles). Otherwise, a Self target is used. The action cost can still be altered afterward (such as for use in subsidiary actions).</param>
    /// <param name="range">The range (in tiles) to use for the 2-action version. Default is 6.</param>
    /// <returns>(CombatAction) The action which draws the given rune on the target.</returns>
    internal static CombatAction CreateDrawAction(
        Creature owner,
        Rune rune,
        int actions = 0,
        int? range = 6)
    {
        #region Determine Range

        // Determine range to target (logic maybe expanded later)
        int rangeToTarget = range ?? 6;

        #endregion

        #region Determine Target

        CreatureTarget adjacentTarget = Target.AdjacentCreatureOrSelf();
        CreatureTarget rangedTarget = Target.RangedCreature(rangeToTarget);
        DependsOnActionsSpentTarget varyTarget = Target.DependsOnActionsSpent(
            adjacentTarget,
            rangedTarget,
            // This shouldn't be possible, so this should ideally throw some kind of exception
            null!);

        // Add extra usage requirements.
        // Don't add the requirement directly, as any changes to the requirement
        // will affect the rune as a whole. Checking requirements specific to
        // the rune should be done by inspecting the rune itself, not any of its
        // actions.
        foreach (CreatureTarget crTar in varyTarget.Targets.OfType<CreatureTarget>())
        {
            // Wrap a new requirement for each creature requirement
            foreach (var crReq in rune.DrawProperties.CreatureTargetingRequirements)
                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                    crReq.Satisfied(a, d));

            // Create a new requirement that requires any one item
            // to meet all item requirements.
            if (rune.DrawProperties.ItemTargetingRequirements.Count > 0)
                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                {
                    List<Item> items = CommonRuneRules.GetCommonTargetableItems(d);
                    List<Usability> usability = items
                        .Select(item => rune.DrawProperties.IsLegalTarget(a, item))
                        .ToList();

                    return usability.Any(use => use.CanBeUsed)
                        ? Usability.Usable
                        : (usability.FirstOrDefault(use => !use.CanBeUsed) ?? Usability.NotUsableOnThisCreature("No valid item targets"));
                });

            // Create a new requirement that requires any one drawn rune
            // to meet all drawn rune requirements.
            if (rune.DrawProperties.RuneTargetingRequirements.Count > 0)
                crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                {
                    List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null, d/*, true*/);
                    List<Usability> usability = drawnRunes
                        .Select(dr => rune.DrawProperties.IsLegalTarget(a, dr))
                        .ToList();

                    return usability.Any(use => use.CanBeUsed)
                        ? Usability.Usable
                        : (usability.FirstOrDefault(use => !use.CanBeUsed) ?? Usability.NotUsableOnThisCreature("No valid rune targets"));
                });
        }
            
        Target drawTarget = actions switch
        {
            2 => rangedTarget,
            1 => adjacentTarget,
            -3 => varyTarget,
            _ => Target.Self()
        };

        #endregion
        
        // Determine traits
        Trait[] traits = [
                ModData.ModTrait,
                ..rune.Traits,
                //Trait.Magical, // <- Gained from rune's traits
                Trait.Spell, // <- Should apply magic immunity.
                Trait.CountsAsSpellForImmunities, // <- Should also help.
                Trait.SomaticOnly, // <- Avoids swallow whole suffocation.
            ];
        
        // Create action
        CombatAction drawRuneAction = new CombatAction(
                owner,
                rune.Illustration,
                $"Draw {rune.FullName}",
                traits,
                "ERROR: INCOMPLETE DESCRIPTION",
                drawTarget)
            .WithTag(rune) // TODO: Look at .Tag and replace with RuneActionTag instead
            .WithActionCost(actions)
            .WithSoundEffect(ModData.SfxNames.TRACE_RUNE)
            .WithRuneTag(rune) // Only contains Rune before execution, then DrawnRune after.
            // Execute subtargeting routine, collect chosen targets
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (drawAction, caster, chosenTargets) =>
            {
                if (drawAction.Tag is not RuneActionTag tag
                    // Let diacritics and item-runes handle their subtargeting if this is drawing on everything in the whole area
                    || drawAction.Target is AreaTarget)
                    return;
                
                // If the subtargets were already chosen before this action began, skip.
                if (tag.ChosenDrawnRune is not null || tag.ChosenItem is not null)
                    return;
                
                var drawProps = tag.Rune.DrawProperties;

                // If this only targets a creature, then immediately move on.
                // (This is a temporary implementation, which can be changed
                // if the base Target of a draw action is ever anything other than
                // a CreatureTarget)
                if (drawProps.TargetsCreatures
                    && !(drawProps.TargetsItems || drawProps.TargetsRunes))
                {
                    tag.ChosenCreature = chosenTargets.ChosenCreatures.LastOrDefault();
                    return;
                }
                
                var subTargets = await tag.Rune.DrawProperties.ChooseRuneSubtargets(
                    caster,
                    chosenTargets.ChosenCreatures);
                
                if (subTargets is null)
                {
                    drawAction.RevertRequested = true;
                    return;
                }

                tag.ChosenItem = subTargets.Value.ChosenItem;
                tag.ChosenDrawnRune = subTargets.Value.ChosenRune;
            })
            .WithEffectOnEachTarget(async (drawAction, caster, target, result) =>
            {
                if (drawAction.Tag is not RuneActionTag tag 
                    || tag.IsEmpty
                    || drawAction.RevertRequested)
                    return;

                if (await CommonRuneRules.DrawRuneOnTarget(
                        drawAction, target, tag.Rune,
                        (object?)tag.ChosenItem ?? tag.ChosenDrawnRune ?? null)
                    is { } newDrawnRune)
                    tag.CreatedDrawnRune = newDrawnRune;
                else
                    drawAction.RevertRequested = true;
            });
        
        return drawRuneAction;
    }

    /// <summary>
    /// The CASTER uses an ACTION to apply the RUNE's <see cref="RunePassiveProperties.DrawnRuneCreator"/> to the TARGET, which might IGNORE targeting restrictions.
    /// </summary>
    /// <param name="drawAction">The CombatAction which is applying the rune. This action should have either the <see cref="ModData.Traits.Traced"/> or the <see cref="ModData.Traits.Etched"/> traits to determine the duration of the effect being applied.</param>
    /// <param name="target">The Creature the rune is applying to.</param>
    /// <param name="rune">The rune to draw.</param>
    /// <param name="alternativeTarget">Some runes affect targets other than creatures, such as an item wielded by the target creature. If this is of type <see cref="Creature"/>, then this replaces the target parameter and a null value is given to DrawnRuneCreator. If this is of any other type, then this value is passed to DrawnRuneCreator.</param>
    /// <param name="ignoreUsageRequirements">(Default: false) If false, then the DrawnRune is applied only when its Rune's <see cref="RuneDrawProperties.IsLegalTarget"/> is usable for the target. This is true for cases like the Runic Reprisal feat which allows a Runesmith to apply any damaging rune to their shield, taking none of the passive effects, but allowing it to be invoked on a creature when they Shield Block.</param>
    /// <param name="doNotApplyImmediately">If true, then this rune won't be applied to the target, and must be applied manually later.</param>
    /// <returns>(bool) True if the effect was successfully applied to the target, false otherwise.</returns>
    public static async Task<DrawnRune?> DrawRuneOnTarget(
        CombatAction drawAction,
        Creature target,
        Rune rune,
        object? alternativeTarget = null,
        bool ignoreUsageRequirements = false,
        bool doNotApplyImmediately = false)
    {
        // Apply the QF if
        // - ignoreTargetRestrictions is true,
        // - or
        // - the rune's draw properties target requirements is legal for this target.
        if (!ignoreUsageRequirements
            && !rune.DrawProperties.IsLegalTarget(drawAction.Owner, target))
        {
            if (rune.DrawProperties.TargetsCreatures
                && !rune.DrawProperties.IsLegalTarget(drawAction.Owner, target))
                return null;
            if (rune.DrawProperties.TargetsItems)
            {
                if (alternativeTarget is Item itemTarget)
                {
                    if (!rune.DrawProperties.IsLegalTarget(drawAction.Owner, itemTarget))
                        return null;
                }
                else if (!CommonRuneRules.GetCommonTargetableItems(target).Any(item =>
                             rune.DrawProperties.IsLegalTarget(drawAction.Owner, item)))
                    return null;
            }

            if (rune.DrawProperties.TargetsRunes)
            {
                if (alternativeTarget is DrawnRune runeTarget)
                {
                    if (!rune.DrawProperties.IsLegalTarget(drawAction.Owner, runeTarget))
                        return null;
                }
                else if (!DrawnRune.GetDrawnRunes(null, target).Any(dr =>
                             rune.DrawProperties.IsLegalTarget(drawAction.Owner, dr)))
                    return null;
            }
        }

        DrawnRune? newDrawnRune = null;
        if (rune.PassiveProperties.DrawnRuneCreator != null)
            newDrawnRune = await rune.PassiveProperties.DrawnRuneCreator.Invoke(
                drawAction,
                rune,
                // If the alt target is a creature, pass that in, otherwise use the original.
                alternativeTarget as Creature ?? target,
                // If the alt target is a non-creature (such as Item), pass that in.
                alternativeTarget is Creature ? null : alternativeTarget);

        if (newDrawnRune == null)
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
        newDrawnRune.WithDrawDuration(DrawnRune.GetDrawTraitFromList(drawAction.Traits));

        if (newDrawnRune.DrawnOn is DrawnRune drawnOnto
            && drawnOnto.AttachedDiacritic is null)
            drawnOnto.AttachedDiacritic = newDrawnRune;
        
        if (!doNotApplyImmediately)
            target.AddQEffect(newDrawnRune);

        return newDrawnRune;
    }

    /// <summary>
    /// The CASTING creature uses the SOURCE combat action to attempt to DRAW a rune on a CREATURE.
    /// </summary>
    /// <param name="runesmith">The <see cref="Creature"/> drawing the rune.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to draw.</param>
    /// <param name="targetFilter">(nullable) A lambda which returns true if the Creature is a valid option to target.</param>
    /// <param name="adjustAction">(nullable) Adjustments to make to the generated trace actions, if any.</param>
    /// <param name="overrideRange">If this rune should be traced at a specific range, this is that range.</param>
    /// <param name="overridePassButton">(Default: " Confirm no trace action ") If the text of the <see cref="PassViaButtonOption"/> should be different, this is that text.</param>
    /// <param name="canBeCanceled">Whether the attempt to draw the rune can be canceled.</param>
    /// <param name="doNotImmediatelyExecute">Whether to immediately execute the chosen option, or wait.</param>
    /// <returns>The chosen Option. Use this to decide whether the action should be reverted or not.</returns>
    public static async Task<Option?> TraceAnyRuneOnACreature(
        Creature runesmith,
        Func<Rune, bool>? runeFilter = null,
        Func<Creature, bool>? targetFilter = null,
        Action<CombatAction>? adjustAction = null,
        int? overrideRange = null,
        string? overridePassButton = null,
        bool? canBeCanceled = false,
        bool? doNotImmediatelyExecute = false)
    {
        // Get available runes
        List<Rune>? knownRunes = RunicRepertoireTag
            .GetRepertoire(runesmith)
            ?.GetTraceableRunes(runesmith);
        if (knownRunes == null)
            return null;
        if (runeFilter is not null)
            knownRunes = knownRunes
                .Where(runeFilter)
                .ToList();
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in knownRunes)
        {
            CombatAction traceThisRuneAction = CommonRuneRules
                .CreateTraceAction(
                    runesmith,
                    rune,
                    // Use a touch animation if the range is 1
                    overrideRange == 1 ? 1 : 2,
                    overrideRange)
                .WithActionCost(0);
            traceThisRuneAction.Description = CommonRuneRules.CreateTraceActionDescription(rune, runesmith.Level,
                withFlavorText: false);
            adjustAction?.Invoke(traceThisRuneAction);
            GameLoop.AddDirectUsageOnCreatureOptions(traceThisRuneAction, options);
        }
        
        // Remove options if a target is specified
        if (targetFilter != null)
            options.RemoveAll(option =>
                option is CreatureOption crOpt && !targetFilter.Invoke(crOpt.Creature));
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return null;
        if (canBeCanceled == true)
            options.Add(new CancelOption(true));
        options.Add(new PassViaButtonOption(overridePassButton ?? " Confirm no trace action "));
        
        // Pick a target
        string topBarText = $"Choose a rune to Trace{(canBeCanceled == true ? " or right-click to cancel" : null)}.";
        // Send a request to pick an option
        Option chosenOption = (await runesmith.Battle.SendRequest(
            new AdvancedRequest(runesmith, "Choose a rune to Trace.", options)
            {
                TopBarText = topBarText,
                TopBarIcon = ModData.Illustrations.TraceRune,
            }))
            .ChosenOption;
        
        // Execute chosen option
        if (doNotImmediatelyExecute != true)
            await chosenOption.Action();

        return chosenOption;
    }

    /// <summary>
    /// Filter for all items on this target and ask which item to draw the given rune onto.
    /// </summary>
    /// <param name="sourceAction">The action which is drawing this rune. If this action has an <see cref="AreaTarget"/>, then it will draw on all items and return the last one instead.</param>
    /// <param name="runesmith">The runesmith drawing this rune.</param>
    /// <param name="itemHolder">The creature holding the items to choose from.</param>
    /// <param name="itemFilter">Which items are allowed to be drawn on.</param>
    /// <param name="runeCreator">The function which creates a rune attached to this item.</param>
    /// <param name="question">The question to ask the runesmith. E.g., (esvadir) "Choose a weapon or unarmed attack whose Strikes will deal additional persistent bleed damage."</param>
    /// <param name="rune">The root rune being drawn of.</param>
    public static async Task<DrawnRune?> ChooseAnItemToDrawOn(
        CombatAction sourceAction,
        Creature runesmith,
        Creature itemHolder,
        Func<Item, bool> itemFilter,
        Func<Item?,DrawnRune?> runeCreator,
        string question,
        Rune rune)
    {
        List<Item> validItems = itemHolder.Weapons
            .Where(itemFilter)
            .ToList();
        
        // If the source action has an area targeting, then this is drawing
        // on all targets within range. Therefor, draw on all items too.
        if (sourceAction.Target is AreaTarget)
        {
            DrawnRune? lastDraw = null;
            foreach (DrawnRune? newDrawnRune in validItems.Select(runeCreator))
            {
                if (newDrawnRune == null)
                    continue;
                
                // Determine the way the rune is being applied.
                newDrawnRune.WithDrawDuration(DrawnRune.GetDrawTraitFromList(sourceAction.Traits));

                itemHolder.AddQEffect(newDrawnRune);
                lastDraw = newDrawnRune;
            }

            // Return the last one that was drawn.
            return lastDraw;
        }
        else
        {
            Item? chosenItem = await runesmith.AskForChoiceAmongItems(
                rune.Illustration,
                $$"""
                  {b}{{sourceAction.Name}}{/b}
                  {{question}}
                  """,
                validItems,
                false);

            return runeCreator(chosenItem);
        }
    }

    public static async Task<DrawnRune?> ChooseARuneToDrawOn(
        CombatAction sourceAction,
        Creature runesmith,
        Creature runeBearer,
        Func<DrawnRune, bool> targetRuneFilter,
        Func<DrawnRune?,DrawnRune?> runeCreator,
        string question,
        Rune rune)
    {
        List<DrawnRune> drawnRunes = DrawnRune
            .GetDrawnRunes(runesmith, runeBearer)
            .Where(dr =>
                dr.AttachedDiacritic is null
                && !dr.Rune.IsDiacriticRune
                && targetRuneFilter.Invoke(dr))
            .ToList();
        
        // If the source action has an area targeting, then this is drawing
        // on all targets within range. Therefor, draw on all runes too.
        if (sourceAction.Target is AreaTarget)
        {
            DrawnRune? lastDraw = null;
            foreach (DrawnRune? newDrawnRune in drawnRunes.Select(runeCreator))
            {
                if (newDrawnRune == null)
                    continue;
                
                // Determine the way the rune is being applied.
                newDrawnRune.WithDrawDuration(DrawnRune.GetDrawTraitFromList(sourceAction.Traits));

                runeBearer.AddQEffect(newDrawnRune);
                lastDraw = newDrawnRune;
            }

            // Return the last one that was drawn.
            return lastDraw;
        }
        else
        {
            DrawnRune? chosenRune;
            
            List<string> choices = drawnRunes
                .Select(dr =>
                    $"{dr.Illustration!.IllustrationAsIconString}{dr.Rune.WordName}")
                .ToList();

            // Return nothing, no options
            if (choices.Count == 0)
                chosenRune = null;
            /*if (canPass)
                choices.Add("Pass");*/
            // Immediately return only option
            else if (choices.Count == 1)
                chosenRune = drawnRunes.First();
            else
            {
                ChoiceButtonOption choice = await runesmith.AskForChoiceAmongButtons(
                    rune.Illustration,
                    $$"""
                      {b}{{sourceAction.Name}}{/b}
                      {{question}}
                      """,
                    choices.ToArray());

                // Passed
                if (choice.Caption == "Pass")
                    chosenRune = null;
                else
                    chosenRune = drawnRunes[choice.Index];
            }

            return runeCreator(chosenRune);
        }
    }

    #region Etching Runes

    /// <summary>Creates a variant of <see cref="CreateDrawAction"/> with modified mechanics for Etching a Rune, such as the <see cref="ModData.Traits.Etched"/> trait and a map-sized range limit, and only applying runes to allies.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <param name="rune">The rune to create an etch action for.</param>
    /// <returns>(CombatAction) The action which etches the given rune on the target.</returns>
    public static CombatAction CreateEtchAction(
        Creature owner,
        Rune rune)
    {
        CombatAction etchAction = CommonRuneRules
            .CreateDrawAction(
                owner,
                rune,
                2,
                // Usable across whole map
                99)
            .WithName($"Etch {rune.FullName}")
            .WithIllustration(new CornerIllustration(
                rune.Illustration,
                ModData.Illustrations.EtchRune,
                Direction.Southeast))
            .WithActionCost(0)
            .WithExtraTrait(ModData.Traits.Etched)
            .WithSoundEffect(ModData.SfxNames.ETCH_RUNE)
            .WithActionId(ModData.ActionIds.EtchRune);
        etchAction.Description = CommonRuneRules.CreateTraceActionDescription(rune, owner.Level, false,
            prologueText: "{Blue}Etched: lasts until the end of combat.{/Blue}\n");
        
        // Custom targeting; this "technically" happened "before" combat.
        etchAction.WithAdjustTarget<CreatureTarget>(crTar =>
        {
            crTar.CreatureTargetingRequirements.RemoveAll(req =>
                req is UnblockedLineOfEffectCreatureTargetingRequirement
                    or NaturalReachCreatureTargetingRequirement
                    or AdjacencyCreatureTargetingRequirement);
            // Usable across whole map
            /*crTar.CreatureTargetingRequirements
                .Add(new MaximumRangeCreatureTargetingRequirement(99));*/
            // You could only pre-etch on you or your allies
            crTar.CreatureTargetingRequirements
                .Add(new FriendOrSelfCreatureTargetingRequirement());
        });
        /*etchAction.Target = new CreatureTarget(
            RangeKind.Ranged,
            [
                new MaximumRangeCreatureTargetingRequirement(99), // Usable across whole map
                new FriendOrSelfCreatureTargetingRequirement(),
                ..rune.DrawProperties.CreatureTargetingRequirements, // Repeat rune usage requirements
                // No line of effect requirement,
            ],
            (_,_,_) => int.MinValue);*/
        
        return etchAction;
    }

    // TODO: Delayed refactorization.
    /*public static InventoryContextMenuOption GetEtchRuneOptions()
    {
        return new InventoryContextMenuOption((slot, item, inventory) =>
        {
            if (item is null)
                return null;
            if (slot.CharacterSheet?.Calculated is not {} values)
                return null;
            if (RunicRepertoireTag.GetRepertoire(values) is not { } repertoire)
                return null;

            IEnumerable<ContextMenuItem> items = [];
            foreach (Rune rune in repertoire.GetKnownRunes(values))
            {
                if (rune.EtchOption is null)
                    continue;
                items = items.Append(rune.EtchOption.Invoke(rune, values, item));
            }
            
            return items.ToArray();
        });
    }*/

    #endregion

    #region Tracing Runes

    /// <summary>
    /// Creates a version of <see cref="CreateDrawAction"/> designed for Tracing runes.
    /// </summary>
    /// <remarks>
    /// This adds <see cref="ModData.Traits.Traced"/> and <see cref="Trait.Manipulate"/>.
    /// </remarks>
    /// <param name="owner">The runesmith tracing the rune.</param>
    /// <param name="rune">The rune being traced.</param>
    /// <param name="actionVersion">The 1 action or 2 action version of Trace Rune.</param>
    /// <param name="overrideRange">If this version has a specific range, this is that range. Otherwise, the range is calculated from the action version and with your feats.</param>
    ///// <exception cref="Exception"><see cref="RuneDrawProperties.IsEtchedOnly"/> must be false.</exception>
    public static CombatAction CreateTraceAction(
        Creature owner,
        Rune rune,
        int actionVersion = 0,
        int? overrideRange = null)
    {
        /*if (rune.DrawProperties.IsEtchedOnly)
            throw new Exception($"You can't create a Trace Rune action with rune {rune.Name} because it can only be etched at the start of combat.");*/
        
        bool hasRuneSinger = owner.HasEffect(ModData.QEffectIds.RuneSinger);
        bool isGenerational = owner.HasFeat(ModData.FeatNames.GenerationalRuneSinger);
        
        // Determine range to target.
        // Uses given range, or increased range if not specified.
        int rangeToTarget = overrideRange ?? (isGenerational ? 12 : 6);
        
        CombatAction traceRune = CreateDrawAction(
                owner,
                rune,
                // Make it the 2-action version with Rune-Singer
                hasRuneSinger
                    ? 2
                    : actionVersion,
                overrideRange)
            .WithName($"Trace {rune.FullName}")
            // Revert the cost back, regardless of what version was made.
            .WithActionCost(actionVersion)
            .WithExtraTrait(Trait.Concentrate)
            .WithExtraTrait(Trait.Manipulate)
            .WithExtraTrait(ModData.Traits.Traced)
            .WithActionId(ModData.ActionIds.TraceRune);
        
        if (actionVersion != 1) // Isn't the melee one
            //traceRune.WithProjectileCone(VfxStyle.BasicProjectileCone(rune.Illustration));
            traceRune.WithProjectileCone(rune.Illustration, 1, ProjectileKind.Arrow);
        
        if (actionVersion == -3)
            traceRune.WithCreateVariantDescription((actions2, spellVariant) =>
            { 
                // Just having this gives the variant range information.
                return actions2 switch
                {
                    //1 => this.CreateTraceActionDescription(traceRune, withFlavorText:false),
                    //2 => this.CreateTraceActionDescription(traceRune, withFlavorText:false),
                    _ => CommonRuneRules.CreateTraceActionDescription(rune, owner.Level, withFlavorText: false)
                };
            });
        
        // Determine description based on actions preset
        switch (actionVersion)
        {
            case -3:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(rune, owner.Level, afterUsageText: $"\n\n{{icon:Action}} The range is touch.\n{{icon:TwoActions}} The range is {rangeToTarget*5} feet.");
                break;
            case 1:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(rune, owner.Level, prologueText: "{b}Range{/b} touch\n");
                break;
            case 2:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(rune, owner.Level, prologueText: $"{{b}}Range{{/b}} {rangeToTarget*5} feet\n");
                break;
            default:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(rune, owner.Level, prologueText: "{b}Range{/b} self\n");
                break;
        }

        // Modify according to Rune-Singer
        if (hasRuneSinger)
        {
            traceRune = traceRune
                .WithName(traceRune.Name.Replace("Trace", "Sing"))
                .WithActionCost(actionVersion == 0 ? 0 : 1)
                .WithDescription(traceRune.Description.Replace(
                    "{b}Range{/b} 30 feet",
                    $"{{Blue}}{{b}}Range{{/b}} {rangeToTarget*5} feet{{/Blue}}\n{{Blue}}{{b}}Frequency{{/b}} Once per combat.{{/Blue}}"))
                .WithSoundEffect(ModData.SfxNames.SING_RUNE);
            //drawRuneAction.Illustration = new SideBySideIllustration(drawRuneAction.Illustration, ModData.Illustrations.RuneSinger);
            traceRune.Traits.Remove(Trait.Manipulate);
            if (!owner.HasFeat(ModData.FeatNames.GenerationalRuneSinger))
                traceRune.WithEffectOnSelf(self =>
                {
                    if (self.HasFeat(ModData.FeatNames.ProdigalRuneSinger))
                        self.QEffects.FirstOrDefault(qf =>
                                qf.Id == ModData.QEffectIds.RuneSingerCreator)
                            ?.UsedThisTurn = true;
                    else
                        self.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.RUNESINGER);

                    // Remove the temporary Rune-Singer effect
                    self.RemoveAllQEffects(qf =>
                        qf.Id == ModData.QEffectIds.RuneSinger
                        && !qf.Innate);
                });
        }
        
        return traceRune;
    }

    #endregion

    #endregion

    #region Invoking Runes

    /// <summary>
    /// Creates and returns the CombatAction wrapper which executes the InvocationBehavior of the given runeTarget.
    /// </summary>
    /// <param name="runesmith">The Creature invoking the rune.</param>
    /// <param name="drawnRune">The DrawnRune to be invoked.</param>
    /// <param name="overrideRange">The range of this specific invocation. If a value isn't given, the range is 30 feet and can be increased by runesmith effects and features.</param>
    /// <param name="immediatelyRemoveImmunity">If true, then <see cref="WithImmediatelyRemovesImmunity"/> is called on the new CombatAction.</param>
    /// <param name="requiresTargetHasDrawnRune">If true, this action can only be used against creatures who own the supplied runeTarget.</param>
    /// <returns></returns>
    public static CombatAction? CreateInvokeAction(
        Creature runesmith,
        DrawnRune drawnRune,
        int? overrideRange = null,
        bool immediatelyRemoveImmunity = false,
        bool requiresTargetHasDrawnRune = true)
    {
        Rune rune = drawnRune.Rune;
        if (rune.InvocationProperties.EffectOnOneTarget == null)
            return null;
        
        // Increase range with Distant Invocation
        int finalRange = overrideRange ?? GetInvocationRange(runesmith, 6).Range;

        Trait drawTrait = drawnRune.DrawTrait;
        string initialDescription =
            $$"""
              {b}{{drawnRune.Name}}{/b}
              {{DrawnRune.DrawTraitToDescription(drawTrait)}}
              """;

        List<Trait> traits = rune.Traits
            .Concat(
            [
                ModData.Traits.Invocation,
                Trait.UnaffectedByConcealment,
                Trait.Spell, // <- Should apply magic immunity.
                Trait.DoNotShowOverheadOfActionName,
            ])
            .ToList();
        traits.Sort(
            (x, y) => string.Compare(x.ToStringOrTechnical(),
                y.ToStringOrTechnical(),
                StringComparison.Ordinal));
        if (!traits.Contains(ModData.ModTrait))
            traits.Insert(0, ModData.ModTrait);
        
        CreatureTarget invokeTarget = Target.RangedCreature(finalRange);
        // Don't add the requirement directly, as any changes to the
        // requirement will affect the rune as a whole.
        // Checking requirements specific to the rune should be done
        // by inspecting the rune itself, not any of its actions.
        foreach (CreatureTargetingRequirement req in rune.InvocationProperties.TargetingRequirements)
            invokeTarget.WithAdditionalConditionOnTargetCreature((a,d) =>
                req.Satisfied(a,d));
        if (requiresTargetHasDrawnRune)
            invokeTarget.WithAdditionalConditionOnTargetCreature((attacker, defender) =>
            {
                QEffect? foundQf = defender.QEffects.FirstOrDefault(
                    qfToFind => qfToFind == drawnRune);
                return foundQf != null
                    ? Usability.Usable
                    : Usability.NotUsableOnThisCreature($"{rune.FullName} not applied");
            });

        CombatAction invokeThisRune = new CombatAction(
                runesmith,
                rune.Illustration,
                "Invoke " + rune.FullName,
                traits.ToArray(),
                initialDescription
                + (rune.InvocationProperties.HasInvocationEntry
                    ? rune.InvocationProperties.InvocationTextWithHeightening!.Invoke(
                        rune,
                        runesmith.Level)
                    : null),
                invokeTarget)
            //.WithTag(drawnRune)
            .WithRuneTag(drawnRune.Rune, chosenRune: drawnRune)
            .WithActionId(ModData.ActionIds.InvokeRune)
            .WithActionCost(0)
            // Cone animation replaced with splashy target animation below.
            //.WithProjectileCone(VfxStyle.BasicProjectileCone(rune.Illustration))
            .WithSoundEffect(ModData.SfxNames.INVOKE_RUNE)
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (invokeAction, caster, targets) =>
            {
                /*if (!PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.HideRuneDialogs))
                    await caster2.Battle.Cinematics.ShowQuickBubble(
                        caster2,
                        runeTarget.Rune.Illustration.IllustrationAsIconString + " {b}"+runeTarget.Rune.BaseName+"!{/b}",
                        null);*/
                string word =
                    $"{drawnRune.Rune.Illustration.IllustrationAsIconString}{{b}}{drawnRune.Rune.WordName}!{{/b}}";
                if (drawnRune.AttachedDiacritic is not null)
                    word =
                        $"{drawnRune.AttachedDiacritic.Rune.Illustration.IllustrationAsIconString}{{b}}{drawnRune.AttachedDiacritic.Rune.WordName}-{{/b}}{word}";
                caster.Overhead(word, Color.MediumPurple);

                if (targets.ChosenCreature is null)
                    return;
                
                // Animation on rune-bearer
                await PlayInvocationAnimation(targets.ChosenCreature!, rune.Illustration);
            })
            .WithEffectOnEachTarget(async (invocation, caster2, target, result) =>
            {
                if (!await CommonRuneRules.InvokeDrawnRune(invocation, drawnRune, target))
                    invocation.RevertRequested = true;
            });
            /*.WithEffectOnChosenTargets(async (invocation, caster2, targets) =>
            {
                foreach (Creature target in targets.GetAllTargetCreatures())
                    await CommonRuneRules.InvokeDrawnRune(invocation, caster2, runeTarget, target);
            });*/

        // Saving Throw Tooltip Creator
        if (rune.InvocationProperties is { Defense: {} def, HideBreakdownTooltip: false })
        {
            invokeThisRune.WithTargetingTooltip((thisInvokeAction, target, index) =>
            {
                string? tooltip =
                    rune.InvocationProperties.HideBreakdownTooltipForAllies
                    && target.FriendOf(thisInvokeAction.Owner)
                        ? null
                        : CombatActionExecution.BreakdownSavingThrowForTooltip(
                            thisInvokeAction,
                            target,
                            new SavingThrow(
                                def,
                                runesmith.ClassDC(ModData.Traits.Runesmith)))
                    .TooltipDescription;
                return initialDescription
                       + rune.InvocationProperties.InvocationTextWithFormattedHeightening!.Invoke(rune, runesmith.Level)
                       + (tooltip is not null ? ("\n\n" + tooltip) : null);
            });
        }

        if (immediatelyRemoveImmunity)
        {
            invokeThisRune = CommonRuneRules.WithImmediatelyRemovesImmunity(invokeThisRune);
        }

        return invokeThisRune;
    }

    public static async Task PlayInvocationAnimation(Creature target, Illustration runeIcon)
    {
        int numParticles = 6; // was 10
        
        List<Tile> splashedTiles = target.Battle.Map.AllTiles
            .Where(tl =>
                    target.DistanceTo(tl) <= 0
                /*&& tl.PrimaryOccupant != target*/)
            .ToList();
        List<Particle> projectiles = [];
        foreach (Tile splashedTile in splashedTiles)
        {
            projectiles.AddRange(target.Battle.SpawnOvercreatureProjectileParticles(
                numParticles, target, splashedTile, Color.White, runeIcon));
        }
        await target.Battle.WaitForProjectiles(projectiles);
    }
    
    /// <summary>
    /// Gets the range of an invocation from its original range after applying common modifiers.
    /// </summary>
    public static (int Range, string Description) GetInvocationRange(Creature runesmith, int initialRange)
    {
        int finalRange = initialRange + (runesmith.HasFeat(ModData.FeatNames.DistantInvocation) ? 6 : 0);
        string rangeDesc = ((finalRange * 5) + " feet").WithColor(finalRange > initialRange ? "Blue" : null);
        return (finalRange, rangeDesc);
    }
    
    /// <summary>
    /// Adds (or replaces, if it already exists) the WithEffectOnChosenTargets behavior of the given CombatAction with a function that removes immunity from simultaneous invocations at the end of the action.
    /// </summary>
    /// <param name="anyCombatAction"></param>
    /// <returns>(CombatAction) the anyCombatAction that was passed, with the new WithEffectOnChosenTargets.</returns>
    public static CombatAction WithImmediatelyRemovesImmunity(CombatAction anyCombatAction)
    {
        anyCombatAction = anyCombatAction.WithEffectOnChosenTargets(async (caster, targets) =>
        {
            caster.Battle.AllCreatures.ForEach(cr =>
                CommonRuneRules.RemoveAllTemporaryImmunities(cr));
        });
        
        return anyCombatAction;
    }
    
    /// <summary>
    /// An INVOCATION ACTION invokes invoke this DRAWN RUNE, possibly onto this TARGET. 
    /// </summary>
    /// <param name="sourceAction">The action invoking this rune.</param>
    /// <param name="drawnRune">The rune being invoked, which will be removed from its owner.</param>
    /// <param name="alternativeTarget">If this invocation needs to affect a specific creature, this is that creature. This behavior is determined by each rune's invocation code.</param>
    /// <returns>Whether the invocation was successfully completed.</returns>
    public static async Task<bool> InvokeDrawnRune(
        CombatAction sourceAction,
        DrawnRune drawnRune,
        Creature? alternativeTarget = null)
    {
        Creature invocationTarget = alternativeTarget ?? drawnRune.Owner;
        
        if (drawnRune.Rune.InvocationProperties.EffectOnOneTarget == null
            || drawnRune.Hidden
            || CommonRuneRules.IsImmuneToThisInvocation(invocationTarget, drawnRune.Rune))
            return false;
        
        List<DrawnRune> callbackRunes = new List<DrawnRune?>
            ([
                drawnRune.AttachedDiacritic,
                drawnRune,
            ])
            .WhereNotNull()
            .ToList();
        
        // BeforeInvokingRune
        foreach (DrawnRune dr in callbackRunes)
        {
            if (dr.BeforeInvokingRune == null)
                continue;
            IEnumerable<Task> tasks = dr.BeforeInvokingRune
                .GetInvocationList()
                .Cast<Func<DrawnRune, CombatAction, DrawnRune, Task>>()
                .Select(before =>
                    before.Invoke(dr, sourceAction, drawnRune));
            await Task.WhenAll(tasks);
        }

        if (sourceAction.RevertRequested)
            return false;
        
        if (drawnRune.Rune.InvocationProperties.SoundEffectBeforeInvocation is {} sfxBefore)
            Sfxs.Play(sfxBefore);
        
        // Invoke the rune
        List<Creature> affectedCreatures = (await drawnRune.Rune.InvocationProperties.EffectOnOneTarget.Invoke(
                sourceAction,
                drawnRune,
                invocationTarget))
            ?.WhereNotNull()
            .ToList() ?? [invocationTarget];
        
        // TODO: When doing targeting rewrite, allow for targeting to occur before execution so that cancellation can mean returning false before calling `BeforeInvokingRune` and without removing the rune.
        
        // Make the affected creature immune to further invocations of this rune
        foreach (Creature affectedCreature in affectedCreatures)
            affectedCreature.AddQEffect(CommonRuneRules.ImmunityToInvocation(drawnRune.Rune));
        
        if (drawnRune.Rune.InvocationProperties.SoundEffectAfterInvocation is {} sfxAfter)
            Sfxs.Play(sfxAfter);
        
        // AfterInvokingRune
        foreach (DrawnRune dr in callbackRunes)
        {
            if (dr.AfterInvokingRune == null)
                continue;
            IEnumerable<Task> tasks = dr.AfterInvokingRune
                .GetInvocationList()
                .Cast<Func<DrawnRune, CombatAction, DrawnRune, Task>>()
                .Select(after =>
                    after.Invoke(dr, sourceAction, drawnRune));
            await Task.WhenAll(tasks);
        }
        
        // Remove this rune
        CommonRuneRules.RemoveDrawnRune(drawnRune);
        if (drawnRune.AttachedDiacritic is { } drawnDiacritic)
            CommonRuneRules.RemoveDrawnRune(drawnDiacritic);

        return true;
    }

    /// <summary>
    /// The CASTING creature uses the SOURCE combat action to INVOKE a DrawnRune on the TARGET creature.
    /// </summary>
    /// <param name="caster">The Creature invoking the DrawnRune.</param>
    /// <param name="targetFilter">The Creature whose DrawnRune will be invoked. If null, you'll be asked to select a Creature with a DrawnRune.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to invoke.</param>
    /// <param name="adjustInvocation">Adjustments to make to each invocation action as they're being created.</param>
    /// <param name="overrideRange">If this invocation occurs at a specific range, this is that range. Otherwise, use default rules for determining the range.</param>
    /// <param name="canBeCanceled">Whether the attempt to invoke the rune can be canceled.</param>
    /// <param name="passText">String to use for the pass text. If no value is given, you cannot pass.</param>
    /// <param name="additionalTopText">Additional text to display after "Choose a rune to invoke."</param>
    /// <returns>(bool) False if the action was canceled or passed, otherwise true.</returns>
    public static async Task<bool> ChooseARuneToInvoke(
        Creature caster,
        Func<Creature, bool>? targetFilter = null,
        Func<Rune, bool>? runeFilter = null,
        Action<CombatAction>? adjustInvocation = null,
        int? overrideRange = null,
        bool? canBeCanceled = false,
        string? passText = null,
        string? additionalTopText = null)
    {
        // Get available runes
        List<Rune>? knownRunes = RunicRepertoireTag
            .GetRepertoire(caster)
            ?.GetKnownRunes(caster);
        if (knownRunes is null)
            return false;
        if (runeFilter is not null)
            knownRunes = knownRunes.Where(runeFilter).ToList();
        if (knownRunes.Count == 0)
            return false;
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in knownRunes)
        {
            foreach (Creature cr in caster.Battle.AllCreatures)
            {
                foreach (DrawnRune dr in DrawnRune.GetDrawnRunes(caster, cr)
                             .Where(dr => dr.Rune == rune)
                             .ToList())
                {
                    CombatAction? newInvokeAction = (overrideRange.HasValue
                            ? CommonRuneRules.CreateInvokeAction(caster, dr, overrideRange.Value)
                            : CommonRuneRules.CreateInvokeAction(caster, dr))
                        ?.WithActionCost(0);
                    
                    if (newInvokeAction == null)
                        continue;
                    
                    adjustInvocation?.Invoke(newInvokeAction);
                    GameLoop.AddDirectUsageOnCreatureOptions(newInvokeAction, options);
                }
            }
        }
        
        // Remove options if a target is specified
        if (targetFilter != null)
            options.RemoveAll(option =>
                option is CreatureOption crOpt
                && !targetFilter(crOpt.Creature));
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return false;
        if (canBeCanceled == true)
            options.Add(new CancelOption(true));
        if (passText is not null)
            options.Add(new PassViaButtonOption(passText));
        
        // Pick a target
        string topBarText =
            "Choose a rune to invoke"
            + (canBeCanceled == true ? " or right-click to cancel" : null)
            + "."
            + additionalTopText;
        Option chosenOption = (await caster.Battle.SendRequest( // Send a request to pick an option
            new AdvancedRequest(caster, "Choose a rune to invoke.", options)
            {
                TopBarText = topBarText,
                TopBarIcon = ModData.Illustrations.InvokeRune,
            })).ChosenOption;
        
        // Do stuff based on specific type of choice
        switch (chosenOption)
        {
            case CreatureOption:
                break;
            case CancelOption:
            case PassViaButtonOption:
                return false;
        }
        
        // Execute chosen option
        return await chosenOption.Action();
    }
    
    /// <summary>
    /// Removes a given DrawnRune from its owner.
    /// </summary>
    /// <param name="drawnRune">The DrawnRune to be removed from its Owner.</param>
    /// <returns>(bool) True if the DrawnRune was removed, false otherwise.</returns>
    public static bool RemoveDrawnRune(DrawnRune drawnRune)
    {
        int removals = drawnRune.Owner.RemoveAllQEffects(
            qfToRemove =>
            {
                if (qfToRemove != drawnRune)
                    return false;
                if (drawnRune.DrawnOn is DrawnRune drawnOnto)
                    drawnOnto.AttachedDiacritic = null;
                drawnRune.DrawnOn = null;
                return true;
            });
        return removals > 0;
    }

    public static async Task<CheckResult> SaveAgainstInvocation(
        CombatAction invokeAction,
        DrawnRune invokedRune,
        Creature effectTarget,
        Func<Rune,int,(string diceExpression, DamageKind Kind)>? getKindedDamage = null,
        Func<CombatAction, Creature, CheckResult, Task>? onResult = null)
    {
        if (invokedRune.Rune.InvocationProperties.Defense is null)
            throw new NullReferenceException($"Saving throw for invocation of {invokedRune.Rune.Id.ToWord()} was attempted, but no saving throw defense was found. Use InvocationProperties.WithDefense(Defense) to set a defense for this rune's invocations.");
        
        CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
            effectTarget,
            invokeAction,
            invokedRune.Rune.InvocationProperties.Defense.Value,
            invokeAction.Owner.ClassDC(ModData.Traits.Runesmith));
        
        if (getKindedDamage is not null)
        {
            var damage = getKindedDamage.Invoke(
                invokedRune.Rune,
                invokedRune.Source!.Level);
            await CommonSpellEffects.DealBasicDamage(
                invokeAction, invokeAction.Owner,
                effectTarget, result,
                damage.diceExpression,
                damage.Kind);
        }

        onResult?.Invoke(invokeAction, effectTarget, result);
        
        return result;
    }

    /// <summary>
    /// Executes an inner action for invocations that have complex targeting or area effects.
    /// </summary>
    /// <param name="outerInvoke">The outer action invoking this rune.</param>
    /// <param name="invokedRune">The rune being invoked.</param>
    /// <param name="invokeTarget">The creature targeted by the effect of the invocation.</param>
    /// <param name="target">The inner action's Target. Always includes a requirement that a creature is not immune to this invocation. If this is a <see cref="EmanationTarget"/>, the alternate origin is the invokeTarget.</param>
    /// <param name="includeRuneBearer">Whether to include the rune-bearer as a valid target of the effect.</param>
    /// <param name="soundEffect">The sound effect to play on invocation, if any.</param>
    /// <param name="hasSavingThrow">Whether to include <see cref="RuneInvocationProperties.Defense"/> in a saving throw on each target.</param>
    /// <param name="effectOnEachTarget">What to do on each target of the invocation's effect.</param>
    /// <param name="chosen">If this action has a predetermined effect target, then FullCast against this target instead of deciding.</param>
    /// <param name="finalAdjustments">Changes to make to the inner invocation.</param>
    /// <returns></returns>
    public static async Task<List<Creature>?> ExecuteInnerInvokeAction(
        CombatAction outerInvoke,
        DrawnRune invokedRune,
        Creature invokeTarget,
        Target target,
        bool includeRuneBearer,
        SfxName? soundEffect,
        bool hasSavingThrow,
        Delegates.EffectOnEachTarget effectOnEachTarget,
        ChosenTargets? chosen = null,
        Action<CombatAction>? finalAdjustments = null)
    {
        // Automate target behavior
        switch (target)
        {
            // Emanation targets always emit from the invoke target
            // and cannot affect anyone immune to the invocation.
            case EmanationTarget emanationTarget:
            {
                emanationTarget
                    .WithAlternateCreatureOfOrigin(invokeTarget)
                    .WithIncludeOnlyIf((tar, cr) =>
                        !CommonRuneRules.IsImmuneToThisInvocation(cr, invokedRune.Rune));

                if (!includeRuneBearer)
                    emanationTarget.WithIncludeOnlyIf((tar, cr) =>
                        cr != invokedRune.Owner);
                break;
            }
            // Creature targets cannot affect anyone immune to the
            // invocation.
            case CreatureTarget creatureTarget:
            {
                creatureTarget
                    .WithAdditionalConditionOnTargetCreature(
                        new IsNotImmuneToInvocation(invokedRune.Rune));

                if (!includeRuneBearer)
                    creatureTarget.WithAdditionalConditionOnTargetCreature((a, d) =>
                        d == invokedRune.Owner
                            ? Usability.NotUsableOnThisCreature("Cannot target rune-bearer")
                            : Usability.Usable);
            }
                break;
        }
        
        // Create action wrapper.
        // Assists with targeting and tooltip breakdowns.
        CombatAction innerInvoke = new CombatAction(
                outerInvoke.Owner,
                invokedRune.Rune.Illustration,
                $"Invoke {invokedRune.Rune.FullName}",
                [..invokedRune.Rune.Traits, ModData.Traits.Invocation, Trait.DoNotShowInCombatLog, Trait.UnaffectedByConcealment],
                invokedRune.Rune.InvocationProperties.InvocationTextWithFormattedHeightening?.Invoke(invokedRune.Rune, outerInvoke.Owner.Level) ?? "",
                target)
            .WithActionCost(0)
            .WithProjectileCone(VfxStyle.BasicProjectileCone(invokedRune.Rune.Illustration))
            .WithSoundEffect(soundEffect!)
            .WithEffectOnEachTarget(effectOnEachTarget);

        if (hasSavingThrow)
            innerInvoke.WithSavingThrow(new SavingThrow(
                invokedRune.Rune.InvocationProperties.Defense!.Value,
                outerInvoke.Owner.ClassDC(ModData.Traits.Runesmith)));

        if (outerInvoke.Tag is RuneActionTag outerTag)
            innerInvoke.WithRuneTag(outerTag.Rune, chosenRune: outerTag.ChosenDrawnRune);
        
        finalAdjustments?.Invoke(innerInvoke);
        
        bool result = chosen is null
            ? await outerInvoke.Owner.Battle.GameLoop.FullCast(innerInvoke)
            : await outerInvoke.Owner.Battle.GameLoop.FullCast(innerInvoke, chosen);

        if (!result)
        {
            outerInvoke.RevertRequested = true;
            return null;
        }
        
        return [..innerInvoke.ChosenTargets.ChosenCreatures];
    }

    #region Immunities
    
    /// <summary>
    /// Creates an immunity against this rune's invocation effects to a given creature. This QEffect needs to be removed manually with <see cref="RemoveAllTemporaryImmunities"/> at the end of any activity with subsidiary invocation actions.
    /// </summary>
    /// <param name="rune">The rune whose invocation to become immune to.</param>
    /// <param name="immuneForEncounter">If true, then this immunity lasts for the duration of the encounter, and won't be removed when invocation immunities would normally be removed.</param>
    /// <returns>(<see cref="QEffect"/>) The immunity which was applied to the target.</returns>
    public static QEffect ImmunityToInvocation(Rune rune, bool immuneForEncounter = false)
    {
        QEffect runeInvocationImmunity = new QEffect()
        {
            Illustration = new SuperimposedIllustration(
                rune.Illustration,
                ModData.Illustrations.NoSymbol),
            Name = $"Invocation Immunity: {rune.FullName}",
            Description = $"You cannot be affected by another invocation of a {rune.FullName.WithColor("Blue")} rune{(immuneForEncounter ? " for the rest of the encounter" : " until the end of this action")}.",
            Id = ModData.QEffectIds.ImmuneToInvocation,
            Tag = rune, // The rune that you're immune to
            ExpiresAt = immuneForEncounter
                ? ExpirationCondition.Never // This QF won't be removed later on.
                : ExpirationCondition.ExpiresAtEndOfAnyTurn, // This QF is supposed to be removed when the activity making invokeActions completes. This is a back-up safety for developer-error.
            DoNotShowUpOverhead = true,
            Key = "InvocationImmunity:" + rune.Id.ToWord(),
        };
        return runeInvocationImmunity;
    }

    /// <summary>
    /// Determines whether a TARGET Creature is immune to the invocation effects of this Rune.
    /// </summary>
    /// <remarks>This searches for a QEffect with an <see cref="QEffect.Id"/> equal to <see cref="ModData.QEffectIds.ImmuneToInvocation"/> and a <see cref="QEffect.Tag"/> matching this Rune.</remarks>
    /// <param name="target">The CREATURE to check.</param>
    /// <param name="rune">The RUNE to check.</param>
    /// <returns>(bool) Returns true if the immunity QEffect is present on the target.</returns>
    public static bool IsImmuneToThisInvocation(Creature target, Rune rune)
    {
        QEffect? thisRunesImmunity = target.QEffects.FirstOrDefault(qf =>
            qf.Id == ModData.QEffectIds.ImmuneToInvocation
            && qf.Tag == rune);
        return thisRunesImmunity != null;
    }
    
    /// <summary>
    /// Removes all invocation immunities from a creature that aren't set to persist for the entire encounter.
    /// </summary>
    /// <param name="cr">The <see cref="Creature"/> whose QEffects will be searched.</param>
    /// <returns>(bool) True if at least one QEffect with the <see cref="ModData.QEffectIds.ImmuneToInvocation"/> Id was removed, false otherwise.</returns>
    public static bool RemoveAllTemporaryImmunities(Creature cr)
    {
        int removals = cr.RemoveAllQEffects(qf =>
                qf.Id == ModData.QEffectIds.ImmuneToInvocation
                && qf.ExpiresAt != ExpirationCondition.Never // Exception for longer duration immunities
        );
        return (removals > 0);
    }
    
    #endregion

    #endregion

    #region Misc
    
    /// <summary>
    /// Moves a rune from its original owner to the new owner, with an optional DrawnOn target.
    /// </summary>
    /// <remarks>
    ///  <para>Use Case: the Transpose Etching feat which allows you to move a rune from one target to another.</para>
    /// <para>WARNING: Does no legality-checking. Just saves a few lines of code.</para></remarks>
    /// <param name="drawnRune">(DrawnRune) The rune to move.</param>
    /// <param name="newOwner">(Creature) The creature who will own the DrawnRune.</param>
    /// <param name="newDrawnOn">(Creature, DrawnRune, Item) The new "real" target from the newOwner to apply the DrawnRune to, such as an item wielded by the newOwner, the creature itself, or another DrawnRune.</param>
    public static /*async*/ void MoveRuneToTarget(DrawnRune drawnRune, Creature newOwner, object? newDrawnOn)
    {
        // Might need expanded functionality in the future.
        
        if (drawnRune.Owner != newOwner)
        {
            CommonRuneRules.RemoveDrawnRune(drawnRune);
            //drawnRune.Owner.RemoveAllQEffects(qf => qf == drawnRune);
            newOwner.AddQEffect(drawnRune);
        }
        
        if (newDrawnOn != null)
        {
            drawnRune.DrawnOn = newDrawnOn;
            if (newDrawnOn is DrawnRune dr)
                dr.AttachedDiacritic = drawnRune;
        }
    }

    /// <summary>
    /// Decider attempts to select drawn runes on any creatures in battle.
    /// </summary>
    /// <remarks>Does not require that the decider owns the runes. Does not require the rune not be disabled.</remarks>
    /// <param name="decider">The creature deciding which drawn rune to select.</param>
    /// <param name="possibleTargets">The list of creatures that can be chosen from.</param>
    /// <param name="illustration">The top-bar icon.</param>
    /// <param name="question">The top-bar string.</param>
    /// <param name="tooltipName">The name of the button in the context menu. Usually a verb such as "choose" followed by the rune's name.</param>
    /// <param name="tooltipText">The description widget displayed on hover next to the button option. Usually the rune's usage, passive, and invocation text.</param>
    /// <param name="passButtonCaption"></param>
    /// <param name="canBeCanceled">Whether you can right-click to cancel the request.</param>
    /// <param name="runeFilter">A function which filters out valid choices (such as drawn runes the decider owns, runes that aren't disabled, or preventing Transpose Etching from selecting a tattoo or runic reprisal trap).</param>
    /// <returns>The chosen drawn rune.</returns>
    public static async Task<DrawnRune?> ChooseADrawnRune(
        Creature decider,
        IEnumerable<Creature> possibleTargets,
        Illustration illustration,
        string question,
        Func<DrawnRune, string>? tooltipName,
        Func<DrawnRune, string>? tooltipText,
        string passButtonCaption = "Pass",
        bool canBeCanceled = false,
        Func<DrawnRune, bool>? runeFilter = null)
    {
        DrawnRune? chosenRune = null;
        List<Option> options = [new PassViaButtonOption(passButtonCaption)];
        if (canBeCanceled)
            options.Add(new CancelOption(true));
        possibleTargets.ForEach(cr =>
        {
            List<DrawnRune> runes = DrawnRune.GetDrawnRunes(null, cr, true);
            if (runeFilter != null)
                runes = runes.Where(runeFilter).ToList();
            runes.ForEach(dr =>
            {
                options.Add(new CreatureOption(
                    cr,
                    tooltipText?.Invoke(dr) ?? dr.Description ?? "{i}" + dr.DrawTrait.HumanizeTitleCase2() + "{/i}\n\n" + CommonRuneRules.CreateTraceActionDescription(// Action is used to get owner's level, so this is fine
                        dr.Rune, decider.Level,
                        withFlavorText: false),
                    async () => chosenRune = dr,
                    int.MinValue,
                    false)
                {
                    Illustration = dr.Illustration,
                    ContextMenuText = tooltipName?.Invoke(dr) ?? $"Choose {{Blue}}{dr.Rune.FullName}{{/Blue}}",
                });
            });
        });
        RequestResult requestResult = await decider.Battle.SendRequest(new AdvancedRequest(
            decider,
            question,
            options)
        {
            TopBarIcon = illustration,
            TopBarText = question,
        });
        if (requestResult.ChosenOption is PassViaButtonOption or CancelOption)
            return null;
        await requestResult.ChosenOption.Action();
        return chosenRune;

    }

    public static void RemoveAllOtherInstancesOf(Creature runesmith, RuneId runeId)
    {
        foreach (DrawnRune dr in DrawnRune
                     .GetAllDrawnRunes(runesmith, true)
                     .Where(dr => dr.Rune.Id == runeId))
        {
            CommonRuneRules.RemoveDrawnRune(dr);
        }
    }
    
    #endregion
}