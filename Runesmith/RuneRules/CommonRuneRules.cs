using System.ComponentModel.DataAnnotations;
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
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Mods.RunesmithClass.TargetingRequirements;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

public static class CommonRuneRules
{
    public static class StatBlocks
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
        public static readonly (string FLAVOR, string RULES) ACTION_DESCRIPTION_ETCH_RUNE = (
            "An etched rune is carved, inked, or branded in, though this application does not damage the creature or item.",
            """
            At the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed.

            You can etch up to 2 runes, and you can etch an additional rune at levels 5, 9, 13, and 17.
            """
        );

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

    /// <summary>
        /// Generates a description block for this rune entry-by-entry.
        /// </summary>
        /// <remarks>All after-text can be a new paragraph. A space between sentences is only added if the string does not begin with linebreaks.</remarks>
        /// <param name="rune">The rune being described.</param>
        /// <param name="smithLevel">The level of the runesmith who knows this rune. If null, this is a non-combat statblock, such as for feats.</param>
        /// <param name="withFlavorText">Whether to include flavor text in the description (typically false for dropdown options).</param>
        /// <param name="afterFlavorText">The text to add at the end of the flavor text paragraph.</param>
        /// <param name="withUsageText">Whether to include the usage text in the description.</param>
        /// <param name="afterUsageText">The text to add at the end of the usage text paragraph.</param>
        /// <param name="withPassiveText">The text to add at the end </param>
        /// <param name="afterPassiveText">The text to add at the end of the passive text paragraph.</param>
        /// <param name="withInvocationText">Whether to include the invocation entry in the description.</param>
        /// <param name="afterInvocationText">The text to add at the end of the invocation text paragraph. This can be a new paragraph instead of a new sentence.</param>
        /// <param name="withLevelText">Whether to include the level entry in the description. This is always false when smithLevel is not null.</param>
        public static string CreateRuneDescription(
            Rune rune,
            int? smithLevel,
            bool withFlavorText = true,
            string? afterFlavorText = null,
            bool withUsageText = true,
            string? afterUsageText = null,
            bool withPassiveText = true,
            string? afterPassiveText = null,
            bool withInvocationText = true,
            string? afterInvocationText = null,
            bool withLevelText = true)
        {
            // Construct each entry of a rune stat block.
            // If the text is included, also add the after-text if it exists.
            // When adding the after-text, avoid adding empty whitespace and null text.
            // Allow for after-text that begins a new line after the entry instead of an extra space.
            
            string? flavor = withFlavorText
                ? rune.GetFlavorText(withFormatting: true)?.AddIfNotNull(null, afterFlavorText?.AddIfNotNull(afterFlavorText.StartsWith("\n") ? null : " ", null))
                : null;

            string? usage = withUsageText
                ? rune.DrawProperties.UsageTextWithFormatting.AddIfNotNull(null, afterUsageText?.AddIfNotNull(afterUsageText.StartsWith("\n") ? null : " ", null))
                : null;

            string? passive = withPassiveText
                ? (smithLevel.HasValue
                    ? rune.PassiveProperties.PassiveTextWithHeightening(rune, smithLevel.Value)
                    : rune.PassiveProperties.PassiveText).AddIfNotNull(null, afterPassiveText?.AddIfNotNull(afterPassiveText.StartsWith("\n") ? null : " ", null))
                : null;
            
            string? invocation = withInvocationText
                ? (smithLevel.HasValue
                    ? rune.InvocationProperties.InvocationTextWithFormattedHeightening?.Invoke(rune, smithLevel.Value)
                    : rune.InvocationProperties.InvocationTextWithFormatting)?.AddIfNotNull(null, afterInvocationText?.AddIfNotNull(afterInvocationText.StartsWith("\n") ? null : " ", null))
                : null;
            
            if (smithLevel is not null)
                withLevelText = false;

            string? level = withLevelText
                ? rune.GetFormattedLevelText()
                : null;
            
            return BuildRuneDescription(flavor, usage, passive, invocation, level);
        }

        public static string BuildRuneDescription(
            string? flavorText,
            string? usageText,
            string? passiveText,
            string? invocationText,
            string? levelText)
        {
            List<string> description = [];
            
            if (flavorText is not null)
            {
                if (!flavorText.StartsWith("{i}"))
                    flavorText.WithTag("i");
                description.Add(flavorText);
            }

            if (usageText is not null)
            {
                if (!usageText.StartsWith("{b}"))
                    usageText = "{b}Usage{/b} " + usageText;
                description.Add(usageText);
            }

            if (passiveText is not null)
                description.Add(passiveText);

            if (invocationText is not null)
            {
                if (!invocationText.StartsWith("{b}"))
                    invocationText = "{b}Invocation{/b} " + invocationText;
                description.Add(invocationText);
            }

            if (levelText is not null)
                description.Add(levelText);
            
            return string.Join("\n\n", description);
        }
        
        /// <summary>
        /// Builds an action description for drawing runes.
        /// </summary>
        /// <remarks>This function is built for actions that draw runes, such as Trace Rune, as well as for custom actions such as the ones provided by Define the Canvas.</remarks>
        public static string CreateDrawActionDescription(
            Rune rune,
            CombatAction drawAction,
            string? customUsageLine = null,
            string? customRangeLine = null)
        {
            return CreateDrawActionDescription(rune, drawAction.Owner.Level, drawAction.Target, customUsageLine, customRangeLine);
        }
        
        /// <summary>
        /// Builds an action description for drawing runes.
        /// </summary>
        /// <remarks>This function is built for actions that draw runes, such as Trace Rune, as well as for custom actions such as the ones provided by Define the Canvas.</remarks>
        public static string CreateDrawActionDescription(
            Rune rune,
            int smithLevel,
            Target? target,
            string? customUsageLine = null,
            string? customRangeLine = null)
        {
            string description = CommonRuneRules.StatBlocks.CreateRuneDescription(
                rune, smithLevel,
                withFlavorText: false,
                afterUsageText: (customRangeLine?.AddIfNotNull("{b}Range{/b} ", null)?.WithColor("Blue") ?? target?.ToDescription())?.AddIfNotNull("\n", null));
            if (customUsageLine is not null)
                description = description.Replace(
                    rune.DrawProperties.UsageText,
                    customUsageLine.WithColor("Blue"));
            return description;
        }

        public static CombatAction CreateTraceActionForDisplay(
            Creature runesmith,
            Rune rune)
        {
            CombatAction traceAction = new CombatAction(
                    runesmith,
                    rune.Illustration,
                    $"Trace Rune ({rune.WordName})",
                    [ModData.ModTrait, Trait.Concentrate, Trait.Magical, Trait.Manipulate, Trait.Runesmith],
                    CommonRuneRules.StatBlocks.CreateRuneDescription(
                        rune, runesmith.Level,
                        afterUsageText: "\n\n{icon:Action} The range is touch.\n{icon:TwoActions} The range is 30 feet."),
                    Target.Self())
                .WithActionCost(-3);
        
            return traceAction;
        }

        public static string InvokeActionDescription()
        {
            return
                $"{ACTION_DESCRIPTION_INVOKE_RUNE.FLAVOR.WithTag("i")}\n\n{ACTION_DESCRIPTION_INVOKE_RUNE.RULES}";
        }
    }

    #region Drawing Runes

    /// <summary>
    /// Get all items on this creature commonly used for the Usage requirements of a rune.
    /// </summary>
    /// <param name="itemBearer"></param>
    /// <returns></returns>
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
    /// Create the Target instance appropriate for the action version of a Trace Rune action.
    /// </summary>
    /// <param name="rune">The Rune being drawn.</param>
    /// <param name="actionVersion">The 1- or 2-action Trace Rune. 0-action is permitted but can behave strangely in some cases.</param>
    /// <param name="range">(Default: 6) The range of the action if different from standard. This is not used for 1-action versions of Trace Rune which is based on reach.</param>
    /// <returns></returns>
    public static CreatureTarget CreateDrawActionTarget(
        Rune rune,
        [Range(0, 2)]
        int actionVersion = 0,
        int? range = 6)
    {
        #region Determine Range

        // Determine range to target (logic maybe expanded later)
        int rangeToTarget = range ?? 6;

        #endregion

        // 0-action versions usually behave strangely, such as affecting yourself
        // or affecting at a custom range with a strange animation, so use the
        // ranged version for 0-action Trace Runes. This is also good for effects
        // such as Engraving Strike because it avoids too much back-and-forth wiggles.
        CreatureTarget creatureTarget = actionVersion == 1
            ? Target.AdjacentCreatureOrSelf()
            : Target.RangedCreature(rangeToTarget);

        // Add extra usage requirements.
        // Don't add the requirement directly, as any changes to the requirement
        // will affect the rune as a whole. Checking requirements specific to
        // the rune should be done by inspecting the rune itself, not any of its
        // actions.
        
        // Wrap a new requirement for each creature requirement
        foreach (var crReq in rune.DrawProperties.CreatureTargetingRequirements)
            creatureTarget.WithAdditionalConditionOnTargetCreature((a, d) =>
                crReq.Satisfied(a, d));

        // Create a new requirement that requires any one item
        // to meet all item requirements.
        if (rune.DrawProperties.ItemTargetingRequirements.Count > 0)
            creatureTarget.WithAdditionalConditionOnTargetCreature((a, d) =>
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
            creatureTarget.WithAdditionalConditionOnTargetCreature((a, d) =>
            {
                List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null, d/*, true*/);
                List<Usability> usability = drawnRunes
                    .Select(dr => rune.DrawProperties.IsLegalTarget(a, dr))
                    .ToList();

                return usability.Any(use => use.CanBeUsed)
                    ? Usability.Usable
                    : (usability.FirstOrDefault(use => !use.CanBeUsed) ?? Usability.NotUsableOnThisCreature("No valid rune targets"));
            });

        return creatureTarget;
    }

    /// <summary>
    /// Creates a generic CombatAction that executes <see cref="DrawRuneOnCreature"/> on each target using this Rune.
    /// </summary>
    /// <param name="runesmith">The creature (Runesmith) who is using this action.</param>
    /// <param name="rune">The rune to create an action for.</param>
    /// <param name="actions">The number of actions for this variant. If actions==-3, a 1-2 action variable target is used. If actions==1, an adjacent target is used. If actions==2, a ranged target is used (6 tiles). Otherwise, a Self target is used. The action cost can still be altered afterward (such as for use in subsidiary actions).</param>
    /// <param name="range">The range (in tiles) to use for the 2-action version. Default is 6.</param>
    /// <param name="actionName">If this action is something other than "Draw Rune" such as "Trace Rune" for the basic action, or "Canvas Rune" for Define the Canvas, this is that name. The word-name of the rune is automatically added after (e.g. "Canvas Rune (Atryl)"), and the word "Rune" is removed for context menu names (e.g. "Canvas Atryl").</param>
    /// <param name="castsOrUses">The value of <see cref="CombatAction.CastsOrUses"/>.</param>
    /// <returns>(CombatAction) The action which draws the given rune on the target.</returns>
    internal static CombatAction CreateDrawAction(
        Creature runesmith,
        Rune rune,
        int actions = 0,
        int? range = 6,
        string actionName = "Draw Rune",
        string castsOrUses = "draws")
    {
        // Create action
        CombatAction drawRune = new CombatAction(
                runesmith,
                rune.Illustration,
                $"{actionName} ({rune.WordName})",
                [
                    ModData.ModTrait,
                    ..rune.Traits,
                    //Trait.Magical, // <- Gained from rune's traits
                    Trait.Spell, // <- Should apply magic immunity.
                    Trait.CountsAsSpellForImmunities, // <- Should also help.
                    Trait.SomaticOnly, // <- Avoids swallow whole suffocation.
                ],
                null!,
                CreateDrawActionTarget(rune, actions, range))
            .WithActionCost(actions)
            .WithSoundEffect(ModData.SfxNames.TRACE_RUNE)
            .WithCastsOrUses(castsOrUses.ToLower())
            .WithRuneTag(rune) // Only contains Rune before execution, then DrawnRune after.
            // Execute subtargeting routine, collect chosen targets
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (drawAction, caster, chosenTargets) =>
            {
                if (drawAction.Tag is not RuneActionTag tag
                    // Let diacritics and item-runes handle their subtargeting if this is drawing on everything in the whole area
                    || drawAction.Target is AreaTarget
                    || drawAction.Target is MultipleCreatureTargetsTarget)
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

                object? subTarget = (object?)tag.ChosenItem ?? tag.ChosenDrawnRune ?? null;
                
                if (await CommonRuneRules.DrawRuneOnCreature(
                        drawAction, target, tag.Rune,
                        subTarget)
                    is { } newDrawnRunes
                    && newDrawnRunes.LastOrDefault() is {} newDrawnRune)
                    tag.CreatedDrawnRune = newDrawnRune;
                else
                {
                    drawAction.RevertRequested = true;
                    return;
                }
                
                CommonRuneRules.LogNewRune(caster, newDrawnRune.Rune, target, subTarget);
            })
            // The context menu tooltips for this action should include a slimmed down
            // full description of this rune including any saving throws.
            .WithTargetingTooltip((drawAction, target, index) =>
            {
                string description = CommonRuneRules.StatBlocks.CreateRuneDescription(
                    rune, runesmith.Level,
                    withFlavorText: false,
                    // Add range description
                    afterUsageText: drawAction.HasTrait(Trait.Etched)
                        ? "\n{Blue}{b}Etched{/b} This rune lasts until the end of combat unless invoked.{/Blue}"
                        : drawAction.Target.ToDescription()?.AddIfNotNull("\n", null));

                if (rune.InvocationProperties is { Defense: not null, HideBreakdownTooltip: false }
                    && !(target.FriendOf(drawAction.Owner) && rune.InvocationProperties.HideBreakdownTooltipForAllies))
                {
                    description +=
                        $"\n\n{{b}}Invocation Breakdown{{/b}}\n{CombatActionExecution.BreakdownSavingThrowForTooltip(
                                drawAction, target,
                                new SavingThrow(
                                    rune.InvocationProperties.Defense!.Value,
                                    drawAction.Owner.ClassDC(Trait.Runesmith)))
                            .TooltipDescription}";
                }
                
                return description;
            })
            .With(drawAction =>
            {
                drawAction.Description = CommonRuneRules.StatBlocks.CreateDrawActionDescription(rune, drawAction);
                
                // Combat log asks for the action count and then queries .ShortName.
                // ShortName itself queries .shortName, then .ContextMenuName, then .Name.
                // The action symbol is never included if it's a free action. By doing this,
                // the log should look something like:
                // > Aldis Ronan traces &Atryl.
                drawAction.ShortName = rune.Illustration.IllustrationAsIconString + rune.WordName;
                // Shorten the context menu name.
                drawAction.ContextMenuName = $"{(actionName.EndsWith(" Rune") ? actionName[..^" Rune".Length] : actionName)} {rune.WordName}";

                #region Goodness
                
                // Add an expectation of future damage from this rune when invoked.
                // Invoke actions do not themselves directly call saving throws.
                // This code is an attempt to evaluate accordingly.
                Func<Rune, int, (string DiceExpression, DamageKind Kind)>? damageGetter = rune.InvocationProperties.GetKindedDamage;
                Func<Target, Creature, Creature, float>? addGood = rune.DrawProperties.AdditionalGoodness;
                if (damageGetter is not null || addGood is not null)
                {
                    Func<Target, Creature, Creature, float> drawGoodness = (tar, self, defender) =>
                    {
                        // AI avoids tracing this rune again if the target already has it.
                        if (defender.HasEffect(qf =>
                                qf is DrawnRune dr && dr.Rune.Id == rune.Id))
                            return AIConstants.BASICALLY_NEVER;

                        // Start at 0. No particular value.
                        float value = 0;
                        
                        if (addGood is not null)
                            value += addGood(tar, self, defender);
                        
                        // If deals damage
                        if (damageGetter is not null)
                        {
                            float invocationDmgValue = DiceFormula.FromText(
                                    damageGetter(rune, runesmith.Level).DiceExpression,
                                    rune.FullName) 
                                .ExpectedValue;

                            if (rune.InvocationProperties.Defense is { } def)
                                invocationDmgValue = self.AI.DealDamageWithBasicSave(
                                    tar.OwnerAction,
                                    new SavingThrow(def, self.ClassDC(ModData.Traits.Runesmith)),
                                    defender.Defenses.GetSavingThrow(def, tar.OwnerAction).Bonus,
                                    defender,
                                    invocationDmgValue);

                            value += invocationDmgValue;
                        }

                        // Tracing it has a slight nudge down compared to invoking it.
                        value -= 0.1f;
                        
                        return value;
                    };
                    
                    // Prioritize the rune's invocation or avoiding debuffs on allies.
                    if (rune.InvocationProperties.Defense is not null
                        || rune.InvocationProperties.DealsDamage
                        || rune.PassiveProperties.PassiveEffectIsDebuff)
                        drawAction.WithGoodnessAgainstEnemy(drawGoodness);
                    else
                        drawAction.WithGoodness(drawGoodness);
                }
                
                #endregion
            });
        
        return drawRune;
    }

    /// <summary>Creates a variant of <see cref="CreateDrawAction"/> with modified mechanics for Etching a Rune, such as the <see cref="ModData.Traits.Etched"/> trait and a map-sized range limit, and only applying runes to allies.
    /// </summary>
    /// <param name="runesmith">The creature (Runesmith) who is using this action.</param>
    /// <param name="rune">The rune to create an etch action for.</param>
    /// <returns>(CombatAction) The action which etches the given rune on the target.</returns>
    public static CombatAction CreateEtchAction(
        Creature runesmith,
        Rune rune)
    {
        CombatAction etchAction = CommonRuneRules
            .CreateDrawAction(
                runesmith, rune, 2,
                // Usable across whole map
                99,
                "Etch Rune", "etches")
            .WithIllustration(new CornerIllustration(
                rune.Illustration,
                ModData.Illustrations.EtchRune,
                Direction.Southeast))
            .WithActionCost(0)
            .WithExtraTrait(ModData.Traits.Etched)
            .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
            // Recreate the same description, just with text after the usage text.
            .WithDescription(CommonRuneRules.StatBlocks.CreateRuneDescription(
                rune, runesmith.Level,
                withFlavorText: false,
                afterUsageText: "\n{Blue}{b}Etched{/b} This rune lasts until the end of combat unless invoked.{/Blue}"))
            .WithSoundEffect(ModData.SfxNames.ETCH_RUNE)
            .WithActionId(ModData.ActionIds.EtchRune)
            // Custom targeting; this "technically" happened "before" combat.
            .WithAdjustTarget<CreatureTarget>(crTar =>
            {
                crTar.CreatureTargetingRequirements.RemoveAll(req =>
                    req is UnblockedLineOfEffectCreatureTargetingRequirement
                        or NaturalReachCreatureTargetingRequirement
                        or AdjacencyCreatureTargetingRequirement);
                // You could only pre-etch on you or your allies
                crTar.CreatureTargetingRequirements
                    .Add(new FriendOrSelfCreatureTargetingRequirement());
            });
        
        return etchAction;
    }

    /// <summary>
    /// Creates a version of <see cref="CreateDrawAction"/> designed for Tracing runes.
    /// </summary>
    /// <remarks>
    /// This adds <see cref="ModData.Traits.Traced"/>, <see cref="Trait.Concentrate"/>, and <see cref="Trait.Manipulate"/>.
    /// </remarks>
    /// <param name="runesmith">The runesmith tracing the rune.</param>
    /// <param name="rune">The rune being traced.</param>
    /// <param name="actionVersion">The 1 action or 2 action version of Trace Rune.</param>
    /// <param name="overrideRange">If this version has a specific range, this is that range. Otherwise, the range is calculated from the action version and with your feats.</param>
    /// <param name="actionName">See: <see cref="CreateDrawAction"/>.</param>
    /// <param name="castsOrUses">See: <see cref="CreateDrawAction"/>.</param>
    public static CombatAction CreateTraceAction(
        Creature runesmith,
        Rune rune,
        int actionVersion = 0,
        int? overrideRange = null,
        string actionName = "Trace Rune",
        string castsOrUses = "traces")
    {
        bool hasRuneSinger = runesmith.HasEffect(ModData.QEffectIds.RuneSinger);
        bool isGenerational = runesmith.HasFeat(ModData.FeatNames.GenerationalRuneSinger);
        
        // Determine range to target.
        // Uses given range, or increased range if not specified.
        int rangeToTarget = overrideRange ?? (isGenerational ? 12 : 6);
        
        CombatAction traceRune = CreateDrawAction(
                runesmith, rune,
                // Make it the 2-action version with Rune-Singer
                hasRuneSinger ? 2 : actionVersion,
                overrideRange, actionName, castsOrUses)
            // Revert the cost back, regardless of what version was made.
            .WithActionCost(actionVersion)
            .WithExtraTrait(Trait.Concentrate)
            .WithExtraTrait(Trait.Manipulate)
            .WithExtraTrait(ModData.Traits.Traced)
            .WithActionId(ModData.ActionIds.TraceRune);
        
        if (actionVersion != 1) // Isn't the melee one
            //traceRune.WithProjectileCone(VfxStyle.BasicProjectileCone(rune.Illustration));
            traceRune.WithProjectileCone(rune.Illustration, 1, ProjectileKind.Arrow);

        // Modify according to Rune-Singer
        if (hasRuneSinger)
        {
            traceRune = traceRune
                .WithName(traceRune.Name.Contains("Trace Rune")
                    ? traceRune.Name.Replace("Trace", "Sing")
                    : traceRune.Name.Replace("Rune", "& Sing"))
                .WithActionCost(actionVersion == 0 ? 0 : 1)
                .WithDescription(traceRune.Description.Replace(
                    "{b}Range{/b} 30 feet",
                    $"{{Blue}}{{b}}Range{{/b}} {rangeToTarget*5} feet{{/Blue}}\n{{Blue}}{{b}}Frequency{{/b}} Once per combat.{{/Blue}}"))
                .WithSoundEffect(ModData.SfxNames.SING_RUNE)
                .WithCastsOrUses("sings");
            traceRune.ContextMenuName = traceRune.ContextMenuName?.Contains("Trace") == true
                ? traceRune.ContextMenuName?.Replace("Trace", "Sing")
                : traceRune.ContextMenuName?.Replace(rune.WordName, $"& Sing {rune.WordName}");
            //drawRuneAction.Illustration = new SideBySideIllustration(drawRuneAction.Illustration, ModData.Illustrations.RuneSinger);
            traceRune.Traits.Remove(Trait.Manipulate);
            if (!runesmith.HasFeat(ModData.FeatNames.GenerationalRuneSinger))
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

    /// <summary>
    /// The core routine that draws a rune on a creature. All actions wrap around this function.
    /// </summary>
    /// <remarks>This checks for target legality unless intentionally bypassed, and determines the duration and duration-trait (Traced, Etched, Tattooed) from the action's traits automatically.</remarks>
    /// <param name="drawAction">The CombatAction which is applying the rune. This action should have either the <see cref="ModData.Traits.Traced"/> or the <see cref="ModData.Traits.Etched"/> traits to determine the duration of the effect being applied.</param>
    /// <param name="target">The Creature the rune is applying to. Runes only exist as QEffects, so a creature must exist to be the owner of the effect, even if it applies to an item.</param>
    /// <param name="rune">The Rune to draw as a DrawnRune.</param>
    /// <param name="alternativeTarget">Some runes affect targets other than creatures, such as an item wielded by the target creature. If this is of type <see cref="Creature"/>, then this replaces the target parameter and a null value is given to DrawnRuneCreator's subTarget. If this is of any other type, then this value is passed to DrawnRuneCreator as the subTarget.</param>
    /// <param name="ignoreUsageRequirements">(Default: false) If false, then the DrawnRune is applied only when its Rune's <see cref="RuneDrawProperties.IsLegalTarget"/> is usable for the target. For example, Runic Reprisal allows you to apply a damaging rune to your shield, taking none of the passive effects, but allowing it to be invoked on a creature when they Shield Block.</param>
    /// <param name="doNotApplyImmediately">If true, then this rune won't be applied to the target, and must be applied manually later.</param>
    /// <returns>(DrawnRune?) The runes which were drawn as part of this routine, or null if empty.</returns>
    public static async Task<List<DrawnRune>?> DrawRuneOnCreature(
        CombatAction drawAction,
        Creature target,
        Rune rune,
        object? alternativeTarget = null,
        bool ignoreUsageRequirements = false,
        bool doNotApplyImmediately = false)
    {
        if (rune.PassiveProperties.DrawnRuneCreator == null)
            return null;
        
        // Apply the QF if
        // - ignoreTargetRestrictions is true,
        // - or the rune's draw properties target requirements is legal for this target.
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

        List<DrawnRune>? newDrawnRunes = await rune.PassiveProperties.DrawnRuneCreator.Invoke(
            drawAction,
            rune,
            // If the alt target is a creature, pass that in, otherwise use the original.
            alternativeTarget as Creature ?? target,
            // If the alt target is a non-creature (such as Item), pass that in.
            alternativeTarget is Creature ? null : alternativeTarget);

        if (newDrawnRunes == null)
            return null;

        foreach (DrawnRune dr in newDrawnRunes)
        {
            // Determine the way the rune is being applied.
            dr.WithDrawDuration(DrawnRune.GetDrawTraitFromList(drawAction.Traits) ?? ModData.Traits.Traced);

            if (dr.DrawnOn is DrawnRune { AttachedDiacritic: null } drawnOnto)
                drawnOnto.AttachedDiacritic = dr;
            
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
            
            if (!doNotApplyImmediately
                && !target.QEffects.Contains(dr))
            {
                if (dr.Rune.PassiveProperties.OnlyOneInExistenceAtATime)
                    CommonRuneRules.RemoveAllOtherInstancesOf(drawAction.Owner, dr.Rune.Id);
                target.AddQEffect(dr);
            }
        }
        
        return newDrawnRunes;
    }

    /// <summary>
    /// This routine encapsulates choosing a rune on a creature and tracing it with a free Trace Rune action.
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
    public static async Task<Option?> AskToTraceARune(
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
            CombatAction traceRune = CommonRuneRules
                .CreateTraceAction(
                    runesmith,
                    rune,
                    // Use a touch animation if the range is 1
                    overrideRange == 1 ? 1 : 2,
                    overrideRange)
                .WithActionCost(0);
            adjustAction?.Invoke(traceRune);
            GameLoop.AddDirectUsageOnCreatureOptions(traceRune, options);
        }
        
        // Remove options if a target is specified
        if (targetFilter != null)
            options.RemoveAll(option =>
                option is CreatureOption crOpt
                && !targetFilter.Invoke(crOpt.Creature));
        
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
    /// <param name="sourceAction">The action which is drawing this rune. If this action has an <see cref="AreaTarget"/> or <see cref="MultipleCreatureTargetsTarget"/>, then it will draw on all items and return the last one instead.</param>
    /// <param name="runesmith">The runesmith drawing this rune.</param>
    /// <param name="itemHolder">The creature holding the items to choose from.</param>
    /// <param name="itemFilter">Which items are allowed to be drawn on.</param>
    /// <param name="runeCreator">The function which creates a rune attached to this item.</param>
    /// <param name="question">The question to ask the runesmith. E.g., (esvadir) "Choose a weapon or unarmed attack whose Strikes will deal additional persistent bleed damage."</param>
    /// <param name="rune">The root rune being drawn of.</param>
    public static async Task<List<DrawnRune>?> ChooseAnItemToDrawOn(
        CombatAction sourceAction,
        Creature runesmith,
        Creature itemHolder,
        Func<Item, bool> itemFilter,
        Func<Item?,DrawnRune?> runeCreator,
        string question,
        Rune rune)
    {
        List<Item> validItems = CommonRuneRules
            .GetCommonTargetableItems(itemHolder)
            .Where(itemFilter)
            .ToList();

        List<DrawnRune> drawings = [];
        
        // If the source action has an area targeting, then this is drawing
        // on all targets within range. Therefor, draw on all items too.
        if (sourceAction.Target is AreaTarget or MultipleCreatureTargetsTarget)
            drawings.AddRange(validItems
                .Select(runeCreator)
                .WhereNotNull());
        else if (await runesmith.AskForChoiceAmongItems(
                         rune.Illustration,
                         $$"""
                           {b}{{sourceAction.Name}}{/b}
                           {{question}}
                           """,
                         validItems,
                         false)
                     is {} chosenItem
                 && runeCreator(chosenItem) is {} drawnRune)
            drawings.Add(drawnRune);

        return drawings.Count == 0 ? null : drawings;
    }

    /// <summary>
    /// Filter for all DrawnRunes on this target and ask which rune to draw the given diacritic onto.
    /// </summary>
    /// <param name="sourceAction">The action which is drawing this rune. If this action has an <see cref="AreaTarget"/> or <see cref="MultipleCreatureTargetsTarget"/>, then it will draw on all items and return the last one instead.</param>
    /// <param name="runesmith">The runesmith drawing this rune.</param>
    /// <param name="runeBearer">The creature bearing the runes to choose from.</param>
    /// <param name="targetRuneFilter">Which DrawnRunes are allowed to be drawn on.</param>
    /// <param name="runeCreator">The function which creates a rune attached to this item.</param>
    /// <param name="question">The question to ask the runesmith. E.g., (esvadir) "Choose a weapon or unarmed attack whose Strikes will deal additional persistent bleed damage."</param>
    /// <param name="rune">The root rune being drawn of.</param>
    public static async Task<List<DrawnRune>?> ChooseARuneToDrawOn(
        CombatAction sourceAction,
        Creature runesmith,
        Creature runeBearer,
        Func<DrawnRune, bool> targetRuneFilter,
        Func<DrawnRune?,DrawnRune?> runeCreator,
        string question,
        Rune rune)
    {
        List<DrawnRune> validDrawnRunes = DrawnRune
            .GetDrawnRunes(runesmith, runeBearer)
            .Where(dr =>
                dr.AttachedDiacritic is null
                && !dr.Rune.IsDiacriticRune
                && targetRuneFilter.Invoke(dr))
            .ToList();
        
        List<DrawnRune> drawings = [];
        
        // If the source action has an area targeting, then this is drawing
        // on all targets within range. Therefor, draw on all runes too.
        if (sourceAction.Target is AreaTarget or MultipleCreatureTargetsTarget)
            drawings.AddRange(validDrawnRunes
                .Select(runeCreator)
                .WhereNotNull());
        else
        {
            DrawnRune? chosenRune;
            
            List<string> choices = validDrawnRunes
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
                chosenRune = validDrawnRunes.First();
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
                    chosenRune = validDrawnRunes[choice.Index];
            }

            DrawnRune? drawnRune = runeCreator(chosenRune);
            if (drawnRune is not null)
                drawings.Add(drawnRune);
        }
        
        return drawings.Count == 0 ? null : drawings;
    }

    #endregion

    #region Invoking Runes

    public static async Task<ChosenTargets?> GetInvocationTargets(
        CombatAction invokeAction,
        DrawnRune drawnRune,
        ChosenTargets? overrideTargets = null)
    {
        ChosenTargets? chosenTargets;
        if (drawnRune.Rune.InvocationProperties.DetermineChosenTargets is not null)
            chosenTargets = await drawnRune.Rune.InvocationProperties.DetermineChosenTargets(
                invokeAction, drawnRune, overrideTargets);
        else
            chosenTargets = overrideTargets ?? ChosenTargets.CreateSingleTarget(drawnRune.Owner);

        return chosenTargets;
    }

    /// <summary>
    /// Creates the CombatAction wrapper which executes the targeting and effect behavior of a rune's invocation.
    /// </summary>
    /// <param name="runesmith">The Creature invoking the rune.</param>
    /// <param name="drawnRune">The DrawnRune to be invoked.</param>
    /// <param name="overrideRange">The range of this specific invocation. If a value isn't given, the range is 30 feet and can be increased by runesmith effects and features.</param>
    /// <param name="immediatelyRemoveImmunity">If true, then <see cref="WithImmediatelyRemovesImmunity"/> is called on the new CombatAction.</param>
    /// <param name="requiresTargetHasDrawnRune">If true, this action can only be used against creatures who own the supplied runeTarget.</param>
    /// <param name="skipInvocationAnimation">Whether to skip the standard invocation animation. Used for feats with custom animations.</param>
    /// <returns></returns>
    public static CombatAction? CreateInvokeAction(
        Creature runesmith,
        DrawnRune drawnRune,
        int? overrideRange = null,
        bool immediatelyRemoveImmunity = false,
        bool requiresTargetHasDrawnRune = true,
        bool skipInvocationAnimation = false)
    {
        Rune rune = drawnRune.Rune;
        if (rune.InvocationProperties.EffectOnOneTarget == null)
            return null;
        Trait drawTrait = drawnRune.DrawTrait;

        #region Range

        // Increase range with Distant Invocation
        int finalRange = overrideRange ?? GetInvocationRange(runesmith, 6).Range;

        #endregion

        #region Traits

        List<Trait> traits = [
            ModData.ModTrait,
            ..rune.Traits,
            ModData.Traits.Invocation,
            Trait.UnaffectedByConcealment,
            Trait.Spell, // <- Should apply magic immunity.
            Trait.CountsAsSpellForImmunities, // <- Should also help.
            Trait.DoNotShowOverheadOfActionName, // Has its own overhead animation
            Trait.DoNotShowInCombatLog, // Has its own log event
        ];

        #endregion

        #region Target and requirements

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

        #endregion

        CombatAction invokeThisRune = new CombatAction(
                runesmith,
                rune.Illustration,
                $"Invoke Rune ({rune.WordName})",
                traits.ToArray(),
                // Use the rune stat block for the description instead of
                // the template 2-invocation activity description.
                CommonRuneRules.StatBlocks.CreateRuneDescription(
                    rune, runesmith.Level,
                    withFlavorText: false,
                    withUsageText: false,
                    withPassiveText: false),
                invokeTarget)
            .WithActionCost(0)
            .WithCastsOrUses("invokes")
            .WithSoundEffect(ModData.SfxNames.INVOKE_RUNE)
            .WithActionId(ModData.ActionIds.InvokeRune)
            .WithRuneTag(drawnRune.Rune, chosenRune: drawnRune)
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (invokeAction, caster, targets) =>
            {
                if (invokeAction.Tag is not RuneActionTag tag
                    || tag.ChosenDrawnRune is null)
                {
                    invokeAction.RevertRequested = true;
                    return;
                }

                if (targets.ChosenCreature is null)
                    return;

                if (skipInvocationAnimation)
                    return;
                
                // Animation on runesmith
                await PlayInvocationOverheadAnimation(caster, tag.ChosenDrawnRune);
                
                // Animation on rune-bearer
                await PlayInvocationSplashAnimation(targets.ChosenCreature!, rune.Illustration);
            })
            .WithEffectOnEachTarget(async (invokeAction, caster, target, _) =>
            {
                if (invokeAction.Tag is not RuneActionTag tag
                    || tag.ChosenDrawnRune is null
                    || invokeAction.RevertRequested)
                {
                    invokeAction.RevertRequested = true;
                    return;
                }

                // Get chosen targets
                ChosenTargets? chosenTargets = await CommonRuneRules.GetInvocationTargets(
                    invokeAction, tag.ChosenDrawnRune);

                DrawnRune? cachedDiacritic = tag.ChosenDrawnRune.AttachedDiacritic;
                
                // Apply invocation effects
                if (chosenTargets is null
                    || !await CommonRuneRules.InvokeDrawnRune(
                        invokeAction, tag.ChosenDrawnRune, chosenTargets))
                {
                    invokeAction.RevertRequested = true;
                    return;
                }

                // Log invocation
                /*CommonRuneRules.LogInvokedRune(caster, tag.ChosenDrawnRune, target);
                if (cachedDiacritic is not null)
                    CommonRuneRules.LogInvokedRune(caster, cachedDiacritic, target);*/
            })
            .WithTargetingTooltip((invokeAction, target, index) =>
            {
                if (invokeAction.Tag is not RuneActionTag tag
                    || tag.ChosenDrawnRune is null)
                    return "";
                
                // Help identify which among multiple runes you're targeting by
                // including the name of the specific rune and its duration trait,
                // in addition to the invocation entry from the action description.
                string description =
                    $"{DrawnRune.DrawTraitToDescription(drawTrait)}{tag.ChosenDrawnRune.TraditionsToDescription()}{invokeAction.Description.Replace("{b}Invocation{/b} ", "")}";
                
                if (rune.InvocationProperties is { Defense: {} def, HideBreakdownTooltip: false }
                    && !(target.FriendOf(invokeAction.Owner)
                        && rune.InvocationProperties.HideBreakdownTooltipForAllies))
                {
                    string tooltip = CombatActionExecution.BreakdownSavingThrowForTooltip(
                            invokeAction,
                            target,
                            new SavingThrow(
                                def,
                                runesmith.ClassDC(ModData.Traits.Runesmith)))
                        .TooltipDescription;
                    description += "\n\n" + tooltip;
                }

                return description;
            });
        
        // Combat log asks for the action count and then queries .ShortName.
        // ShortName itself queries .shortName, then .ContextMenuName, then .Name.
        // The action symbol is never included if it's a free action. By doing this,
        // the log should look something like:
        // > Aldis Ronan invokes &Atryl.
        invokeThisRune.ShortName = rune.Illustration.IllustrationAsIconString + rune.WordName;
        // Shorten the context menu name.
        invokeThisRune.ContextMenuName = $"Invoke {rune.WordName}";

        if (immediatelyRemoveImmunity)
            invokeThisRune = CommonRuneRules.WithImmediatelyRemovesImmunity(invokeThisRune);

        #region Goodness

        // Add an expectation of future damage from this rune when invoked.
        // Invoke actions do not themselves directly call saving throws.
        // This code is an attempt to evaluate accordingly.
        Func<Rune, int, (string DiceExpression, DamageKind Kind)>? damageGetter = rune.InvocationProperties.GetKindedDamage;
        Func<Target, Creature, Creature, float>? addGood = rune.InvocationProperties.AdditionalGoodness;
        if (damageGetter is not null || addGood is not null)
        {
            Func<Target, Creature, Creature, float> invokeGoodness = (tar, self, defender) =>
            {
                // AI avoids invoking this rune if the target isn't a bearer.
                if (!defender.HasEffect(qf =>
                        qf is DrawnRune dr && dr.Rune.Id == rune.Id))
                    return AIConstants.NEVER;

                // Start at 0. No particular value.
                float value = 0;

                if (addGood is not null)
                    value += addGood(tar, self, defender);

                // If deals damage
                if (damageGetter is not null)
                {
                    float invocationDmgValue = DiceFormula.FromText(
                            damageGetter(rune, runesmith.Level).DiceExpression,
                            rune.FullName)
                        .ExpectedValue;

                    if (rune.InvocationProperties.Defense is { } def2)
                        invocationDmgValue = self.AI.DealDamageWithBasicSave(
                            tar.OwnerAction,
                            new SavingThrow(def2, self.ClassDC(ModData.Traits.Runesmith)),
                            defender.Defenses.GetSavingThrow(def2, tar.OwnerAction).Bonus,
                            defender,
                            invocationDmgValue);

                    value += invocationDmgValue;
                }

                return value;
            };

            // Prioritize the rune's invocation or avoiding debuffs on allies.
            if (rune.InvocationProperties.Defense is not null
                || rune.InvocationProperties.DealsDamage
                || rune.PassiveProperties.PassiveEffectIsDebuff)
                invokeThisRune.WithGoodnessAgainstEnemy(invokeGoodness);
            else
                invokeThisRune.WithGoodness(invokeGoodness);
        }

        #endregion

        return invokeThisRune;
    }

    #region Invocation Animations

    public static string RuneToOverhead(DrawnRune drawnRune)
    {
        string word =
            $"{{b}}{drawnRune.ToWordString()}!{{/b}}";
        if (drawnRune.AttachedDiacritic is not null)
            word =
                $"{{b}}{drawnRune.AttachedDiacritic.ToWordString()}-{{/b}}{word}";
        return word;
    }

    public static async Task PlayInvocationOverheadAnimation(Creature runesmith, DrawnRune drawnRune)
    {
        runesmith.Overhead(RuneToOverhead(drawnRune), Color.MediumPurple);
    }

    public static async Task PlayGroupInvocationOverheadAnimation(Creature runesmith, List<DrawnRune> drawnRunes)
    {
        foreach (DrawnRune dr in drawnRunes)
        {
            await PlayInvocationOverheadAnimation(runesmith, dr);
            await new SleepRequest(250);
        }
    }

    public static async Task PlayInvocationSplashAnimation(Creature target, Illustration runeIcon)
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

    public static async Task PlayGroupInvocationSplashAnimation(TBattle battle, List<(Creature Target, Illustration runeIcon)> animations)
    {
        int numParticles = 6; // was 10
        List<Particle> projectiles = [];
        foreach ((Creature target, Illustration runeIcon) in animations)
        {
            List<Tile> splashedTiles = battle.Map.AllTiles
                .Where(tl => target.DistanceTo(tl) <= 0)
                .ToList();
            foreach (Tile splashedTile in splashedTiles)
            {
                projectiles.AddRange(battle.SpawnOvercreatureProjectileParticles(
                    numParticles, target, splashedTile, Color.White, runeIcon));
            }
        }
        await battle.WaitForProjectiles(projectiles);
    }

    #endregion
    
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
    /// Adds to the WithEffectOnChosenTargets behavior to remove immunities to invocations at the end of the action.
    /// </summary>
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
    /// The core routine that invokes a rune -- making saves and the like, and removing the rune.
    /// </summary>
    /// <param name="sourceAction">The action invoking this rune. This is read by <see cref="RuneInvocationProperties.EffectOnOneTarget"/>.</param>
    /// <param name="drawnRune">The rune being invoked, which will be removed from its owner.</param>
    /// <param name="chosenTargets">The chosen targets of the invocation, as determined by <see cref="GetInvocationTargets"/>. Calling that function is preferred over a direct supplying of ChosenTargets because it plays additional animations and sound effects.</param>
    /// <returns>Whether the invocation was successfully completed.</returns>
    public static async Task<bool> InvokeDrawnRune(
        CombatAction sourceAction,
        DrawnRune drawnRune,
        ChosenTargets? chosenTargets)
    {
        if (drawnRune.Rune.InvocationProperties.EffectOnOneTarget == null
            || drawnRune.Hidden)
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

        // Allow for this to be aborted early.
        if (sourceAction.RevertRequested)
            return false;
        
        if (drawnRune.Rune.InvocationProperties.SoundEffectBeforeInvocation is {} sfxBefore)
            Sfxs.Play(sfxBefore);
        
        // Invoke the rune
        List<Creature> affectedCreatures = [];
        foreach (Creature chosenCreature in (chosenTargets?.ChosenCreatures ?? [])
                 .Where(cr =>
                     !CommonRuneRules.IsImmuneToThisInvocation(cr, drawnRune.Rune)))
        {
            List<Creature>? affected = await drawnRune.Rune.InvocationProperties
                .EffectOnOneTarget(
                    sourceAction,
                    drawnRune,
                    chosenCreature);
            if (affected is not null)
                affectedCreatures.Add(chosenCreature);
        }
        
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
        DrawnRune? diacritic = drawnRune.AttachedDiacritic;
        CommonRuneRules.LogInvokedRune(sourceAction.Owner, drawnRune, drawnRune.Owner, drawnRune.DrawnOn);
        CommonRuneRules.RemoveDrawnRune(drawnRune);
        if (diacritic is not null)
        {
            CommonRuneRules.LogInvokedRune(sourceAction.Owner, diacritic, diacritic.Owner, diacritic.DrawnOn);
            CommonRuneRules.RemoveDrawnRune(diacritic);
        }

        return true;
    }

    /// <summary>
    /// Perform the effects of an invocation against a single creature target.
    /// </summary>
    /// <remarks>
    /// This causes the invocation to only affect the target, regardless of the number of targets it normally effects. This essentially skips most subtargeting routines whilst still playing all the expected animations and sound effects, or altering the animations as appropriate. Use this for effects such as Runic Reprisal or Orbiting Runestone.
    /// </remarks>
    /// <param name="runesmith">The creature invoking the rune.</param>
    /// <param name="drawnRune">The rune being invoked.</param>
    /// <param name="target">The single creature target of the rune's invocation effects.</param>
    /// <param name="customName">If the invocation action has a custom name, this is that name.</param>
    public static async Task<bool> InvokeDrawnRuneAgainstTarget(
        Creature runesmith,
        DrawnRune drawnRune,
        Creature target,
        string? customName = null)
    {
        // Create action
        CombatAction? invokeAction = CommonRuneRules.CreateInvokeAction(
                runesmith, drawnRune,
                immediatelyRemoveImmunity: true,
                requiresTargetHasDrawnRune: false)
            ?.WithActionCost(0);

        if (invokeAction is null)
            return false;

        if (customName is not null)
            invokeAction.WithName(customName);
        
        // Overwrite the execution to use the specific target instead of
        // the rune-bearer.
        invokeAction.EffectOnOneTarget = async (invokeAction2, _, _, _) =>
        {
            // Execute sub-targeting routine
            if (await CommonRuneRules.GetInvocationTargets(
                    invokeAction2,
                    drawnRune,
                    // Use the action's chosen targets, which is set
                    // to just the target creature during full-casting.
                    invokeAction2.ChosenTargets)
                is not { } chosenSubTargets
                || !await CommonRuneRules.InvokeDrawnRune(
                    invokeAction2,
                    drawnRune,
                    chosenSubTargets))
            {
                invokeAction2.RevertRequested = true;
                return;
            }
        };

        // Move them back, so the invoke animation looks good
        await target.FictitiousSingleTileMoveBack();

        // Execute the action for all the bells and whistles.
        return await runesmith.Battle.GameLoop.FullCast(
            invokeAction,
            // Pass in the single target as the chosen targets.
            ChosenTargets.CreateSingleTarget(target));
    }

    /// <summary>
    /// Generate and choose options for invoking a single rune.
    /// </summary>
    /// <param name="caster">The Creature invoking the DrawnRune.</param>
    /// <param name="targetFilter">(nullable) A lambda which filters what rune-bearing creatures you're allowed to invoke a rune from.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to invoke.</param>
    /// <param name="adjustInvocation">Adjustments to make to each invocation action as they're being created.</param>
    /// <param name="overrideRange">If this invocation occurs at a specific range, this is that range. Otherwise, use default rules for determining the range.</param>
    /// <param name="canBeCanceled">Whether the attempt to invoke the rune can be canceled.</param>
    /// <param name="passText">String to use for the pass text. If no value is given, you cannot pass.</param>
    /// <param name="additionalTopText">Additional text to display after "Choose a rune to invoke."</param>
    /// <param name="skipInvocationAnimation">Whether to skip the standard invocation animation. Used for feats with custom animations.</param>
    /// <param name="skipConfirmation">If true, skip confirming the invocation if only one option is available.</param>
    /// <returns>(bool) False if the action was canceled or passed, otherwise true.</returns>
    public static async Task<bool> ChooseARuneToInvoke(
        Creature caster,
        Func<Creature, bool>? targetFilter = null,
        Func<Rune, bool>? runeFilter = null,
        Action<CombatAction>? adjustInvocation = null,
        int? overrideRange = null,
        bool? canBeCanceled = false,
        string? passText = null,
        string? additionalTopText = null,
        bool skipInvocationAnimation = false,
        bool skipConfirmation = false)
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
                            ? CommonRuneRules.CreateInvokeAction(caster, dr, overrideRange.Value, skipInvocationAnimation: skipInvocationAnimation)
                            : CommonRuneRules.CreateInvokeAction(caster, dr, skipInvocationAnimation: skipInvocationAnimation))
                        ?.WithActionCost(0);
                    
                    if (newInvokeAction == null)
                        continue;
                    
                    adjustInvocation?.Invoke(newInvokeAction);

                    // Capture new options
                    List<Option> newOptions = [];
                    GameLoop.AddDirectUsageOnCreatureOptions(newInvokeAction, newOptions);
                    
                    // Modify them for their actual goodness value
                    newOptions.ForEach(opt =>
                    {
                        float? goodness = (newInvokeAction.Target as CreatureTarget)?
                            .CreatureGoodness.Invoke(
                                newInvokeAction.Target,
                                newInvokeAction.Owner,
                                (opt as CreatureOption)?.Creature ?? cr);
                        if (goodness is null)
                            return;
                        opt.AiUsefulness.MainActionUsefulness = goodness.Value;
                    });
                    
                    // Add them
                    options.AddRange(newOptions);
                }
            }
        }
        
        // Remove options if a target is specified
        if (targetFilter != null)
            options.RemoveAll(option =>
                option is CreatureOption crOpt
                && !targetFilter(crOpt.Creature));
        
        // Add bells and whistles to options
        if (options.Count == 0)
            return false;
        // Ask to confirm
        if (options.Count > 1 || !skipConfirmation)
        {
            if (canBeCanceled == true)
                options.Add(new CancelOption(true));
            if (passText is not null)
                options.Add(new PassViaButtonOption(passText));
        }
        
        // Pick a target
        string topBarText =
            "Choose a rune to invoke"
            + (canBeCanceled == true ? " or right-click to cancel" : null)
            + "."
            + additionalTopText;
        Option chosenOption = 
            options.Count == 1 && skipConfirmation
                ? options[0]
                : (await caster.Battle.SendRequest( // Send a request to pick an option
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

    /// <summary>
    /// Perform a saving throw against this invocation, and apply damage (if any).
    /// </summary>
    /// <param name="invokeAction">The action invoking this rune.</param>
    /// <param name="invokedRune">The rune being invoked.</param>
    /// <param name="effectTarget">The target making the save.</param>
    /// <param name="onResult">What to do, other than taking damage, depending on the result.</param>
    /// <param name="skipDamage">If invokedRune.Rune.InvocationProperties.GetKindedDamage is not null, then this must be false to skip dealing damage as part of this saving throw.</param>
    /// <param name="knownResult">If a CheckResult was determined outside this action, then this is that result instead of making a new saving throw.</param>
    /// <returns>The CheckResult of the saving throw that was made.</returns>
    /// <exception cref="NullReferenceException">invokedRune.Rune.InvocationProperties.Defense must not be null.</exception>
    public static async Task<CheckResult> SaveAgainstInvocation(
        CombatAction invokeAction,
        DrawnRune invokedRune,
        Creature effectTarget,
        Func<CombatAction, Creature, CheckResult, Task>? onResult = null,
        bool skipDamage = false,
        CheckResult? knownResult = null)
    {
        if (invokedRune.Rune.InvocationProperties.Defense is null)
            throw new NullReferenceException($"Saving throw for invocation of {invokedRune.Rune.WordName} was attempted, but no saving throw defense was found. Use InvocationProperties.WithDefense(Defense) to set a defense for this rune's invocations.");
        
        CheckResult result = knownResult ?? await CommonSpellEffects.RollSavingThrowAsync(
            effectTarget,
            invokeAction,
            invokedRune.Rune.InvocationProperties.Defense.Value,
            invokeAction.Owner.ClassDC(ModData.Traits.Runesmith));

        if (!skipDamage
            && invokedRune.Rune.InvocationProperties.GetKindedDamage is {} getKindedDamage )
        {
            var damage = getKindedDamage.Invoke(
                invokedRune.Rune,
                invokedRune.Source!.Level);
            await CommonSpellEffects.DealBasicDamage(
                invokeAction, invokeAction.Owner,
                effectTarget, result,
                damage.DiceExpression,
                damage.Kind);
        }

        onResult?.Invoke(invokeAction, effectTarget, result);
        
        return result;
    }

    /// <summary>
    /// Executes an inner action for invocations that have complex targeting or area effects.
    /// </summary>
    /// <remarks>
    /// This is useful for generating animations and sound effects in a manner expected of performing actions in combat, e.g. only playing a sound when confirming an area rather than when performing the initial invocation or after all animations play.
    /// </remarks>
    /// <param name="outerInvoke">The outer action invoking this rune.</param>
    /// <param name="invokedRune">The rune being invoked.</param>
    /// <param name="target">The inner action's Target. Always includes a requirement that a creature is not immune to this invocation. If this is an <see cref="AreaTarget"/>, the alternate origin is the rune-bearer.</param>
    /// <param name="includeRuneBearer">Whether to include the rune-bearer as a valid target of the effect.</param>
    /// <param name="soundEffect">The sound effect to play on confirmation, if any.</param>
    /// <param name="finalAdjustments">Changes to make to the inner invocation.</param>
    /// <param name="overrideTargets">If this action has a predetermined set of creature or tile targets, then FullCast with these prechosen targets instead of deciding.</param>
    /// <returns>The list of affected creatures.</returns>
    public static async Task<ChosenTargets?> GetInnerInvokeTargets(
        CombatAction outerInvoke,
        DrawnRune invokedRune,
        Target target,
        bool includeRuneBearer,
        SfxName? soundEffect,
        Action<CombatAction>? finalAdjustments = null,
        ChosenTargets? overrideTargets = null)
    {
        // Automate target behavior
        switch (target)
        {
            // Area targets always emit from the rune-bearer
            // and cannot affect anyone immune to the invocation.
            case AreaTarget arTar:
            {
                arTar.WithIncludeOnlyIf((tar, cr) =>
                    !CommonRuneRules.IsImmuneToThisInvocation(cr, invokedRune.Rune));

                arTar.WithAlternateCreatureOfOrigin(invokedRune.Owner);
                
                if (arTar is EmanationTarget emTar)
                {
                    if (!includeRuneBearer)
                        emTar.WithIncludeOnlyIf((tar, cr) =>
                            cr != invokedRune.Owner);
                }
                break;
            }
            // Creature targets cannot affect anyone immune to the
            // invocation.
            case CreatureTarget crTar:
            {
                crTar.AlternateTileOfOrigin = invokedRune.Owner.Space.CenterTile;
                crTar.WithAdditionalConditionOnTargetCreature(
                    new IsNotImmuneToInvocation(invokedRune.Rune));

                if (!includeRuneBearer)
                    crTar.WithAdditionalConditionOnTargetCreature((a, d) =>
                        d == invokedRune.Owner
                            ? Usability.NotUsableOnThisCreature("Cannot target rune-bearer")
                            : Usability.Usable);
                break;
            }
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
            .WithSoundEffect(soundEffect!)
            .WithProjectileCone(
                VfxStyle.BasicProjectileCone(invokedRune.Rune.Illustration));

        if (outerInvoke.Tag is RuneActionTag outerTag)
            innerInvoke.WithRuneTag(outerTag.Rune, chosenRune: outerTag.ChosenDrawnRune);
        
        finalAdjustments?.Invoke(innerInvoke);
        
        bool result = overrideTargets is null
            ? await outerInvoke.Owner.Battle.GameLoop.FullCast(innerInvoke)
            : await outerInvoke.Owner.Battle.GameLoop.FullCast(
                innerInvoke,
                overrideTargets);

        if (!result)
        {
            outerInvoke.RevertRequested = true;
            return null;
        }
        
        return innerInvoke.ChosenTargets;
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
            Key = "InvocationImmunity:" + rune.WordName,
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
    /// Removes all temporary invocation immunities from a creature.
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
            if (drawnRune.Rune.PassiveProperties.OnlyOneInExistenceAtATime)
                CommonRuneRules.RemoveAllOtherInstancesOf(drawnRune.Source!, drawnRune.Rune.Id);
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
    public static async Task<(DrawnRune? ChosenRune, Option ChosenOption)> ChooseADrawnRune(
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
                    tooltipText?.Invoke(dr) ?? dr.Description ?? "{i}" + dr.DrawTrait.HumanizeTitleCase2() + "{/i}\n\n" + CommonRuneRules.StatBlocks.CreateRuneDescription(
                        // Action is used to get owner's level, so this is fine
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
            return (null, requestResult.ChosenOption);
        await requestResult.ChosenOption.Action();
        return (chosenRune, requestResult.ChosenOption);

    }

    /// <summary>
    /// Removes all other instances of a rune with this ID created by this runesmith.
    /// </summary>
    /// <remarks>Used for runes that you are only allowed 1 instance of, such as some diacritic runes.</remarks>
    public static void RemoveAllOtherInstancesOf(Creature runesmith, RuneId runeId)
    {
        foreach (DrawnRune dr in DrawnRune
                     .GetAllDrawnRunes(runesmith, true)
                     .Where(dr => dr.Rune.Id == runeId))
        {
            CommonRuneRules.RemoveDrawnRune(dr);
        }
    }

    /// <summary>
    /// Adds a combat log entry for the rune which was just created.
    /// </summary>
    /// <param name="runesmith">The runesmith that created the rune.</param>
    /// <param name="newRune">The rune being logged.</param>
    /// <param name="target">The creature bearing the rune.</param>
    /// <param name="subTarget">The subtarget of the rune, such as an item, if any.</param>
    public static void LogNewRune(Creature runesmith, Rune newRune, Creature target,
        object? subTarget = null)
    {
        string whom = target.Name;
        string? whom2 = subTarget switch
        {
            Item subItem => $"{subItem.Illustration.IllustrationAsIconString}{subItem.ShortName}",
            DrawnRune subRune => $"{subRune.Illustration!.IllustrationAsIconString}{subRune.Rune.WordName}",
            _ => null
        };
        string rune = $"{newRune.Illustration.IllustrationAsIconString}{newRune.WordName}";
        
        runesmith.Battle.Log(
            $"{whom}{(whom2 is not null ? $"'s {whom2}" : null)} gains {rune}.",
            newRune.FullName,
            CommonRuneRules.StatBlocks.CreateRuneDescription(newRune, runesmith.Level),
            new Traits(newRune.Traits));
    }

    /// <summary>
    /// Adds a combat log entry for the rune which was just invoked.
    /// </summary>
    /// <param name="runesmith">The runesmith that created the rune.</param>
    /// <param name="invokedRune">The rune being logged.</param>
    /// <param name="target">The creature bearing the rune.</param>
    /// <param name="subTarget">The subtarget of the rune, such as an item, if any.</param>
    public static void LogInvokedRune(Creature runesmith, DrawnRune invokedRune, Creature target,
        object? subTarget = null)
    {
        string whom = target.Name;
        string? whom2 = subTarget switch
        {
            Item subItem => $"{subItem.Illustration.IllustrationAsIconString}{subItem.ShortName}",
            DrawnRune subRune => subRune.ToWordString(),
            _ => null
        };
        string rune = invokedRune.ToWordString();
        
        runesmith.Battle.Log(
            $"{whom}{(whom2 is not null ? $"'s {whom2}" : null)} loses {rune}.",
            invokedRune.Rune.FullName,
            CommonRuneRules.StatBlocks.CreateRuneDescription(invokedRune.Rune, runesmith.Level),
            new Traits(invokedRune.Traits));

        /*if (invokedRune.AttachedDiacritic is { } diacritic)
            LogInvokedRune(runesmith, diacritic, target, subTarget);*/
    }
    
    #endregion
}