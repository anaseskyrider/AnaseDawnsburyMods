using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines;
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

public static class CommonRuneRules
{
    #region Formatted Descriptions
    /// <summary>
    /// Generates a description block for this rune's Trace actions.
    /// </summary>
    /// <param name="traceAction">The CombatAction to check against. Used for owner level.</param>
    /// <param name="rune">The rune being traced.</param>
    /// <param name="withFlavorText">Whether to include flavor text in the description (typically false for dropdown options).</param>
    /// <param name="prologueText">The paragraph to add at the top of the description (includes one line-break after).</param>
    /// <param name="afterFlavorText">The text to add at the end of the flavor text paragraph.</param>
    /// <param name="afterUsageText">The text to add at the end of the usage text paragraph.</param>
    /// <param name="afterPassiveText">The text to add at the end of the passive text paragraph.</param>
    /// <param name="afterInvocationText">The text to add at the end of the invocation text paragraph.</param>
    /// <param name="epilogueText">The paragraph to add at the bottom of the description (includes one line-break before).</param>
    /// <returns></returns>
    public static string CreateTraceActionDescription(
        CombatAction traceAction,
        Rune rune,
        bool withFlavorText = true,
        string? prologueText = null,
        string? afterFlavorText = null,
        string? afterUsageText = null,
        string? afterPassiveText = null,
        string? afterInvocationText = null,
        string? epilogueText = null)
    {
        int lvl = traceAction.Owner.Level;
        string usageText = rune.GetFormattedUsageText() + afterUsageText;
        string? flavorText = (withFlavorText ? rune.GetFormattedFlavorText() : null) + afterFlavorText;
        string passiveText = rune.PassiveTextWithHeightening(rune, lvl) + afterPassiveText;
        string? invocationText = rune.GetFormattedInvocationText(rune.InvocationTextWithHeightening(rune, lvl)) + afterInvocationText;
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
    /// <param name="rune">The rune to use.</param>
    /// <param name="withFlavorText">Whether to include <see cref="Rune.GetFormattedFlavorText"/> in the return.</param>
    /// <returns>(string) The full description with formatting.</returns>
    public static string GetFormattedFeatDescription(Rune rune, bool withFlavorText = true)
    {
        string description = 
            (withFlavorText ? rune.GetFormattedFlavorText() + "\n\n" : null) +
            rune.GetFormattedUsageText() + "\n\n" +
            rune.PassiveText +
            (rune.InvocationText != null ? "\n\n" + rune.GetFormattedInvocationText() : null) +
            (rune.LevelText != null ? "\n\n" + rune.GetFormattedLevelText() : null);
        return description;
    }

    public static string DescribeRunicRepertoire(Creature owner)
    {
        RunicRepertoireFeat? repertoire = RunicRepertoireFeat.GetRepertoireOnCreature(owner);
        if (repertoire == null)
            return "";
        List<string> traditions = [];
        if (owner.Skills.IsTrained(Skill.Arcana))
            traditions.Add("arcane");
        if (owner.Skills.IsTrained(Skill.Religion))
            traditions.Add("divine");
        if (owner.Skills.IsTrained(Skill.Occultism))
            traditions.Add("occult");
        if (owner.Skills.IsTrained(Skill.Nature))
            traditions.Add("primal");
        int DC = RunesmithClass.RunesmithDC(owner);
        int etchLim = repertoire.GetEtchLimit(owner.Level);
        string? tattoo = (owner.PersistentCharacterSheet?.Calculated.AllFeats
                .FirstOrDefault(ft => ft.FeatName.ToStringOrTechnical().Contains("FeatTattooed"))?.Tag as Rune)?.BaseName.ToLower() ?? null;
        string runesKnown = string.Join("; ",
            repertoire.GetRunesKnown(owner)
                .GroupBy(rune => rune.BaseLevel)
                .OrderByDescending(rg => rg.Key)
                .Select(rg =>
                {
                    string rank = "{b}" + rg.Key.Ordinalize2() + "{/b}";
                    string runes = string.Join(", ",
                        rg.GroupBy(rn => rn.Name)
                            .OrderBy(lg => lg.Key)
                            .Select(runes =>
                            {
                                Rune first = runes.First();
                                return first.Name.Substring(0, first.RuneId.ToStringOrTechnical().Length).ToLower();
                            }));
                    return rank + " {i}" + runes + "{/i}";
                })
        );
        return $"{{b}}Traditions{{/b}} {string.Join(", ", traditions)}\n{{b}}DC{{/b}} {DC}"
            + (etchLim > 0 ? $"; {{b}}etch limit{{/b}} {etchLim}" : null)
            + (tattoo != null ? $"\n{{b}}Tattoo{{/b}} {{i}}{tattoo}{{/i}}" : null)
            + (owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.RunicTattoo) ? " (faded)" : null)
            + $"\n{runesKnown}";
        
        /*
         // Kept old code just because
        int DC = RunesmithClass.RunesmithDC(owner);
        string stats = $"DC {DC};";
        int etchLimitNum = this.GetEtchLimit(owner.Level);
        string etchLimit = etchLimitNum > 0 ? $"{{b}}etch limit{{/b}} ({etchLimitNum} runes); " : "";
        string runesKnown = string.Join("; ",
            GetRunesKnown(owner)
                .GroupBy(rune => rune.BaseLevel)
                .OrderByDescending(rg => rg.Key)
                .Select(rg =>
                {
                    string rank = "{b}" + rg.Key.Ordinalize2() + "{/b}";
                    string runes = string.Join(", ",
                        rg.GroupBy(rn => rn.Name)
                            .OrderBy(lg => lg.Key)
                            .Select(runes =>
                            {
                                Rune first = runes.First();
                                return first.Name.Substring(0, first.RuneId.ToStringOrTechnical().Length);
                            }));
                    return rank + " {i}" + runes + "{/i}";
                })
        );
        string description = stats + " " + etchLimit + runesKnown;
        return description;
        */
    }
    #endregion

    #region Immunities
    /// <summary>
    /// Creates and applies an immunity against this rune's invocation effects to a given creature. This QEffect needs to be removed manually with <see cref="RemoveAllImmunities"/> at the end of any activity with subsidiary invocation actions.
    /// </summary>
    /// <param name="invokeTarget">The <see cref="Creature"/> to become immune to this rune's invocation.</param>
    /// <param name="rune">The rune whose invocation to become immune to.</param>
    /// <returns>(<see cref="QEffect"/>) The immunity which was applied to the target.</returns>
    public static QEffect ApplyImmunity(Creature invokeTarget, Rune rune)
    {
        QEffect runeInvocationImmunity = new QEffect()
        {
            Name = "Invocation Immunity: " + rune.Name,
            Description = "Cannot be affected by another instance of this invocation until the end of this action.",
            Illustration = new SuperimposedIllustration(rune.Illustration, ModData.Illustrations.NoSymbol),
            Tag = rune,
            Traits = [ModData.Traits.InvocationImmunity, rune.RuneId], // ImmunityQFs are identified by these traits.
            ExpiresAt = ExpirationCondition.ExpiresAtEndOfAnyTurn, // This QF is supposed to be removed when the activity making invokeActions completes. This is a back-up safety for developer-error.
            DoNotShowUpOverhead = true,
        };
        invokeTarget.AddQEffect(runeInvocationImmunity);
        return runeInvocationImmunity;
    }

    /// <summary>
    /// Determines whether a TARGET Creature is immune to the invocation effects of this Rune by searching for a QEffect with the <see cref="ModData.Traits.InvocationImmunity"/> trait and a trait matching this Rune's <see cref="Rune.RuneId"/>.
    /// </summary>
    /// <param name="target">The CREATURE to check.</param>
    /// <param name="rune">The RUNE to check.</param>
    /// <returns>(bool) Returns true if the immunity QEffect is present on the TARGET.</returns>
    public static bool IsImmuneToThisInvocation(Creature target, Rune rune)
    {
        QEffect? thisRunesImmunity = target.QEffects.FirstOrDefault(qfToFind =>
            qfToFind.Traits.Contains(ModData.Traits.InvocationImmunity) &&
            qfToFind.Traits.Contains(rune.RuneId));
        return thisRunesImmunity != null;
    }
    
    /// <summary>
    /// Removes all invocation immunities from a creature.
    /// </summary>
    /// <param name="cr">The <see cref="Creature"/> whose QEffects will be searched.</param>
    /// <returns>(bool) True if at least one QEffect with the <see cref="ModData.Traits.InvocationImmunity"/> trait was removed, false otherwise.</returns>
    public static bool RemoveAllImmunities(Creature cr)
    {
        int removals = cr.RemoveAllQEffects(
            qf =>
                qf.Traits.Contains(ModData.Traits.InvocationImmunity)
        );
        return (removals > 0);
    }
    #endregion

    #region CombatActions
    /// <summary>
    /// Creates a generic CombatAction which when executed, calls <see cref="DrawRuneOnTarget"/> on each target using this Rune.
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
        // Determine range to target (logic maybe expanded later)
        int rangeToTarget = range ?? 6;

        // Determine Target Properties
        CreatureTarget adjacentTarget = Target.AdjacentCreatureOrSelf();
        CreatureTarget rangedTarget = Target.RangedCreature(rangeToTarget);
        DependsOnActionsSpentTarget varyTarget = Target.DependsOnActionsSpent(
            adjacentTarget,
            rangedTarget,
            null! /*This shouldn't be possible, so this should ideally throw some kind of exception*/);

        // Add extra usage requirements
        foreach (Target tar in varyTarget.Targets)
        {
            if (tar is not CreatureTarget crTar)
                continue;
            crTar.WithAdditionalConditionOnTargetCreature( // Free hand
                (attacker, defender) =>
                    RunesmithClass.IsRunesmithHandFree(attacker)
                        ? Usability.Usable
                        : Usability.NotUsable("You must have a free hand to trace a rune"));
            crTar.WithAdditionalConditionOnTargetCreature(rune.UsageCondition);
        }
        
        // Determine traits
        Trait[] traits = [
                ..rune.Traits,
                Trait.Magical,
                Trait.Spell // <- Should apply magic immunity.
            ];
        
        // Create action
        CombatAction drawRuneAction = new CombatAction(
                owner,
                rune.Illustration,
                "Draw " + rune.Name,
                traits,
                "ERROR: INCOMPLETE DESCRIPTION",
                actions switch
                {
                    2 => rangedTarget,
                    1 => adjacentTarget,
                    -3 => varyTarget,
                    _ => Target.Self().WithAdditionalRestriction(self =>
                        RunesmithClass.IsRunesmithHandFree(self) ? null : "You must have a free hand to trace a rune")
                })
            .WithTag(rune)
            .WithActionCost(actions)
            .WithSoundEffect(ModData.SfxNames.TraceRune)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                Rune actionRune = (thisAction.Tag as Rune)!;
                if (await CommonRuneRules.DrawRuneOnTarget(thisAction, caster, target, actionRune) == null)
                    thisAction.RevertRequested = true;
            });
        
        return drawRuneAction;
    }
    
    /// <summary>Returns a version of <see cref="CreateDrawAction"/> with modified mechanics for Tracing a Rune, such as the <see cref="ModData.Traits.Traced"/> and Manipulate traits.</summary>
    public static CombatAction? CreateTraceAction(
        Creature owner,
        Rune rune,
        int actions = 0,
        int? range = 6)
    {
        if (rune.DrawTechnicalTraits.Contains(ModData.Traits.Etched))
            return null;
        
        bool hasRuneSinger = owner.HasEffect(ModData.QEffectIds.RuneSinger);
        
        if (hasRuneSinger && actions == 1)
            actions = 2; // Make it the 2-action version instead
        
        // Determine range to target (logic maybe expanded later)
        int rangeToTarget = range ?? 6;
        
        CombatAction traceRune = CreateDrawAction(owner, rune, actions, range)
            .WithExtraTrait(Trait.Concentrate)
            .WithExtraTrait(Trait.Manipulate)
            .WithExtraTrait(ModData.Traits.Traced)
            .WithActionId(ModData.ActionIds.TraceRune);
        traceRune.Name = $"Trace {rune.Name}";
        
        if (actions != 1) // Isn't the melee one
            traceRune.WithProjectileCone(VfxStyle.BasicProjectileCone(rune.Illustration));
        
        if (actions == -3)
            traceRune.WithCreateVariantDescription((actions2, spellVariant) =>
            { // Just having this gives the variant range information.
                return actions2 switch
                {
                    //1 => this.CreateTraceActionDescription(drawRuneAction, withFlavorText:false),
                    //2 => this.CreateTraceActionDescription(drawRuneAction, withFlavorText:false),
                    _ => CommonRuneRules.CreateTraceActionDescription(traceRune, rune, withFlavorText:false)
                };
            });
        
        // Determine description based on actions preset
        switch (actions)
        {
            case -3:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(traceRune, rune, afterUsageText:$"\n\n{{icon:Action}} The range is touch.\n{{icon:TwoActions}} The range is {rangeToTarget*5} feet.");
                break;
            case 1:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(traceRune, rune, prologueText:"{b}Range{/b} touch\n");
                break;
            case 2:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(traceRune, rune, prologueText:$"{{b}}Range{{/b}} {rangeToTarget*5} feet\n");
                break;
            default:
                traceRune.Description = CommonRuneRules.CreateTraceActionDescription(traceRune, rune, prologueText:"{b}Range{/b} self\n");
                break;
        }

        // Modify according to Rune-Singer
        if (hasRuneSinger)
        {
            traceRune = traceRune
                .WithActionCost(actions == 0 ? 0 : 1)
                .WithSoundEffect(ModData.SfxNames.SingRune)
                .WithEffectOnSelf(self =>
                {
                    self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.RuneSinger || qf.Id == ModData.QEffectIds.RuneSingerCreator);
                });
            traceRune.Name = traceRune.Name.Replace("Trace", "Sing");
            traceRune.Description = traceRune.Description.Replace("{b}Range{/b} 30 feet", "{Blue}{b}Range{/b} 30 feet{/Blue}\n{Blue}{b}Frequency{/b} Once per combat{/Blue}");
            //drawRuneAction.Illustration = new SideBySideIllustration(drawRuneAction.Illustration, ModData.Illustrations.RuneSinger);
            traceRune.Traits.Remove(Trait.Manipulate);
        }
        
        return traceRune;
    }

    /// <summary>Creates a variant of <see cref="CreateDrawAction"/> with modified mechanics for Etching a Rune, such as the <see cref="ModData.Traits.Etched"/> trait and a map-sized range limit, and only applying runes to allies.
    /// </summary>
    /// <param name="owner">The creature (Runesmith) who is using this action.</param>
    /// <param name="rune">The rune to create an etch action for.</param>
    /// <returns>(CombatAction) The action which etches the given rune on the target.</returns>
    public static CombatAction CreateEtchAction(
        Creature owner,
        Rune rune)
    {
        CombatAction etchAction = CommonRuneRules.CreateDrawAction(owner, rune, 2)
            .WithActionId(ModData.ActionIds.EtchRune)
            .WithActionCost(0)
            .WithExtraTrait(ModData.Traits.Etched)
            .WithSoundEffect(ModData.SfxNames.EtchRune);
        etchAction.Name = $"Etch {rune.Name}";
        etchAction.Description = CommonRuneRules.CreateTraceActionDescription(etchAction, rune, false, prologueText:"{Blue}Etched: lasts until the end of combat.{/Blue}\n");
        
        // Usable across the whole map
        etchAction.Target = Target.RangedFriend(99); // BUG: Is blocked by line of effect. I don't currently know a way around this.
        // Do this again since we just replaced the target.
        (etchAction.Target as CreatureTarget)!.WithAdditionalConditionOnTargetCreature(rune.UsageCondition);
        // Don't add a free hand requirement; this "technically" happened "before" combat.
        
        return etchAction;
    }

    /// <summary>
    /// Creates and returns the CombatAction wrapper which executes the InvocationBehavior of the given runeTarget.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which executes this internal action.</param>
    /// <param name="caster">The Creature invoking the rune.</param>
    /// <param name="runeTarget">The DrawnRune to be invoked.</param>
    /// <param name="rune">The rune representing the drawn rune.</param>
    /// <param name="range">The range to the creature target. If no range is specified, the range is 30 feet.</param>
    /// <param name="immediatelyRemoveImmunity">If true, then <see cref="WithImmediatelyRemovesImmunity"/> is called on the new CombatAction.</param>
    /// <param name="requiresTargetHasDrawnRune">If true, this action can only be used against creatures who own the supplied runeTarget.</param>
    /// <returns></returns>
    public static CombatAction? CreateInvokeAction(
        CombatAction? sourceAction,
        Creature caster,
        DrawnRune runeTarget,
        Rune rune,
        int range = 6,
        bool immediatelyRemoveImmunity = false,
        bool requiresTargetHasDrawnRune = true)
    {
        if (rune.InvocationBehavior == null)
            return null;

        Trait drawTrait = runeTarget.DrawTrait ?? Trait.None;
        string initialDescription = $"{{b}}{runeTarget.Name}{{/b}}\n"
                                    + (runeTarget.Description!.Contains("Tattoo") 
                                        ? "{i}Tattooed{/i}\n" 
                                        : $"{{i}}{drawTrait.ToStringOrTechnical()}{{/i}}\n");

        Trait[] traits = rune.Traits.ToArray().Concat(
            [
                ModData.Traits.Invocation,
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
                    : Usability.NotUsableOnThisCreature($"{rune.Name} not applied");
            });
        
        CombatAction invokeThisRune = new CombatAction(
                caster,
                rune.Illustration,
                "Invoke " + rune.Name,
                traits,
                initialDescription + (rune.InvocationTextWithHeightening(rune, caster.Level) ?? "[No invocation entry]"),
                invokeTarget)
            .WithTag(rune)
            .WithActionId(ModData.ActionIds.InvokeRune)
            .WithActionCost(0)
            .WithProjectileCone(VfxStyle.BasicProjectileCone(rune.Illustration))
            .WithSoundEffect(ModData.SfxNames.InvokeRune)
            .WithEffectOnEachTarget(async (thisInvokeAction, caster2, target, result) =>
            {
                await CommonRuneRules.InvokeDrawnRune(thisInvokeAction, caster2, target, runeTarget);
            });

        // Saving Throw Tooltip Creator
        Trait saveTrait =
            rune.InvokeTechnicalTraits.FirstOrDefault(trait => trait is Trait.Reflex or Trait.Fortitude or Trait.Will);
        if (!rune.InvokeTechnicalTraits.Contains(Trait.DoesNotRequireAttackRollOrSavingThrow) && saveTrait != Trait.None)
        {
            Defense def = saveTrait switch
            {
                Trait.Reflex => Defense.Reflex,
                Trait.Fortitude => Defense.Fortitude,
                Trait.Will => Defense.Will,
                _ => throw new ArgumentOutOfRangeException()
            };
            invokeThisRune.WithTargetingTooltip((thisInvokeAction, target, index) =>
            {
                string tooltip = CombatActionExecution.BreakdownSavingThrowForTooltip(thisInvokeAction, target,
                    new SavingThrow(def, RunesmithClass.RunesmithDC(caster))).TooltipDescription;
                return initialDescription
                       + rune.GetFormattedInvocationText(rune.InvocationTextWithHeightening(rune, caster.Level))
                       + "\n" + tooltip;
            });
        }

        if (immediatelyRemoveImmunity)
        {
            invokeThisRune = CommonRuneRules.WithImmediatelyRemovesImmunity(invokeThisRune);
        }

        return invokeThisRune;
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
                CommonRuneRules.RemoveAllImmunities(cr));
        });
        
        return anyCombatAction;
    }
    #endregion

    #region Drawing Runes
    /// <summary>
    /// The CASTER uses an ACTION to apply the RUNE's <see cref="Rune.NewDrawnRune"/> to the TARGET, which might IGNORE targeting restrictions.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which is applying the rune. This action should have either the <see cref="ModData.Traits.Traced"/> or the <see cref="ModData.Traits.Etched"/> traits to determine the duration of the effect being applied.</param>
    /// <param name="caster">The Creature applying the rune.</param>
    /// <param name="target">The Creature the rune is applying to.</param>
    /// <param name="rune">The rune to draw.</param>
    /// <param name="ignoreUsageRequirements">(Default: false) If false, then the DrawnRune is applied only when its Rune's <see cref="Rune.UsageCondition"/> is valid for the target. This is true for cases like the Runic Reprisal feat which allows a Runesmith to apply any damaging rune to their shield, taking none of the passive effects, but allowing it to be invoked on a creature when they Shield Block.</param>
    /// <returns>(bool) True if the effect was successfully applied to the target, false otherwise.</returns>
    public static async Task<DrawnRune?> DrawRuneOnTarget(
        CombatAction sourceAction,
        Creature caster,
        Creature target,
        Rune rune,
        bool ignoreUsageRequirements = false)
    {
        // Apply the QF if "ignoreTargetRestrictions is True", or "UsageCondition isn't null, and it returns Usable on the target".
        if (!ignoreUsageRequirements &&
            (rune.UsageCondition.Invoke(caster, target) != Usability.Usable)) 
            return null;

        DrawnRune? qfToApply = null;
        if (rune.NewDrawnRune != null)
            qfToApply = await rune.NewDrawnRune.Invoke(sourceAction, caster, target, rune);

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
        if (sourceAction.HasTrait(ModData.Traits.Etched))
            qfToApply = qfToApply.WithIsEtched();
        else if (sourceAction.HasTrait(ModData.Traits.Traced))
            qfToApply = qfToApply.WithIsTraced();
        
        target.AddQEffect(qfToApply);

        return qfToApply;
    }
    
    public static async Task<DrawnRune?> PickARuneToDrawOnTarget(
        CombatAction? sourceAction,
        Creature caster,
        Creature target,
        int? range = 6,
        Func<Rune, bool>? runeFilter = null,
        bool? canBeCanceled = false)
    {
        // Get available runes
        RunicRepertoireFeat? repertoireFeat = RunicRepertoireFeat.GetRepertoireOnCreature(caster);
        if (repertoireFeat == null)
            return null;
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in repertoireFeat.GetRunesKnown(caster))
        {
            if (runeFilter != null && runeFilter.Invoke(rune) != true)
                continue;
            CombatAction? traceThisRuneAction = CommonRuneRules.CreateTraceAction(caster, rune, 2, range)?
                .WithActionCost(0);
            if (traceThisRuneAction == null)
                continue;
            traceThisRuneAction.Description = CommonRuneRules.CreateTraceActionDescription(traceThisRuneAction, rune, withFlavorText:false);
            GameLoop.AddDirectUsageOnCreatureOptions(
                traceThisRuneAction, // Use at normal range.
                options,
                false);
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
                TopBarIcon = ModData.Illustrations.TraceRune,
            })).ChosenOption;
        
        // Do stuff based on specific type of choice
        switch (chosenOption)
        {
            case CreatureOption:
                break;
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
    /// The CASTING creature uses the SOURCE combat action to attempt to DRAW a rune on a CREATURE.
    /// </summary>
    /// <param name="sourceAction">The <see cref="CombatAction"/> which is attempting to draw the rune.</param>
    /// <param name="caster">The <see cref="Creature"/> drawing the rune.</param>
    /// <param name="targetFilter">(nullable) A lambda which returns true if the Creature is a valid option to target.</param>
    /// <param name="range">(default: 6) if a target is not provided, this range is used for tracing a rune.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to draw.</param>
    /// <param name="canBeCanceled">Whether the attempt to draw the rune can be canceled.</param>
    public static async Task<bool> PickACreatureAndDrawARune(
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
            return false;
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in repertoireFeat.GetRunesKnown(caster))
        {
            if (runeFilter != null && runeFilter.Invoke(rune) != true)
                continue;
            CombatAction? traceThisRuneAction = CommonRuneRules.CreateTraceAction(caster, rune, 2, range)?
                .WithActionCost(0);
            if (traceThisRuneAction == null)
                continue;
            traceThisRuneAction.Description = CommonRuneRules.CreateTraceActionDescription(traceThisRuneAction, rune, withFlavorText:false);
            GameLoop.AddDirectUsageOnCreatureOptions(traceThisRuneAction, options, false);
        }
        
        // Remove options if a target is specified
        if (targetFilter != null)
            options.RemoveAll(option =>
                option is CreatureOption crOpt && !targetFilter.Invoke(crOpt.Creature));
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return false;
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
                TopBarIcon = ModData.Illustrations.TraceRune,
            })).ChosenOption;
        
        // Do stuff based on specific type of choice
        switch (chosenOption)
        {
            case CreatureOption:
                break;
            case CancelOption:
                if (sourceAction != null)
                    sourceAction.RevertRequested = true;
                return false;
            case PassViaButtonOption:
                return true;
        }
        
        // Execute chosen option
        await chosenOption.Action();
        return true;
    }
    #endregion

    #region Invoking Runes
    /// <summary>
    /// Removes a given DrawnRune from its owner, if it corresponds to a DrawnRune created by an instance of this Rune.
    /// </summary>
    /// <param name="runeToRemove">The DrawnRune to be removed from its Owner.</param>
    /// <param name="rune">The RUNE to check against.</param>
    /// <returns>(bool) True if the DrawnRune was removed, false otherwise.</returns>
    public static bool RemoveDrawnRune(DrawnRune runeToRemove, Rune rune)
    {
        int removals = runeToRemove.Owner.RemoveAllQEffects(
            qfToRemove =>
            {
                if (qfToRemove != runeToRemove || runeToRemove.Rune != rune)
                    return false;
                
                runeToRemove.DrawnOn = null;
                return true;
            });
        return removals > 0;
    }
    
    public static async Task InvokeDrawnRune(
        CombatAction sourceAction,
        Creature caster,
        Creature runeBearer,
        DrawnRune runeToInvoke)
    {
        Rune thisRune = runeToInvoke.Rune;
        
        if (thisRune.InvocationBehavior == null || runeToInvoke.Hidden)
            return;
        
        foreach (Creature cr in caster.Battle.AllCreatures)
        {
            List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null, cr);
            foreach (DrawnRune dr in drawnRunes)
            {
                if (dr.BeforeInvokingRune == null)
                    continue;
                var tasks = dr.BeforeInvokingRune.GetInvocationList()
                    .Cast<Func<DrawnRune, CombatAction, DrawnRune, Task>>()
                    .Select(d =>
                        d.Invoke(dr, sourceAction, runeToInvoke));
                await Task.WhenAll(tasks);
                    
                /*foreach (Func<DrawnRune, CombatAction, DrawnRune, Task>? func in dr.BeforeInvokingRune
                             .GetInvocationList())
                    await func.Invoke(dr, sourceAction, runeToInvoke);*/
            }
        }

        if (sourceAction.RevertRequested == true)
            return;
        
        await thisRune.InvocationBehavior.Invoke(sourceAction, thisRune, caster, runeBearer, runeToInvoke);
        
        // Called manually since it's just been removed from all creatures
        if (runeToInvoke.AfterInvokingRune != null)
        {
            var tasks = runeToInvoke.AfterInvokingRune.GetInvocationList()
                .Cast<Func<DrawnRune, CombatAction, DrawnRune, Task>>()
                .Select(d =>
                    d.Invoke(runeToInvoke, sourceAction, runeToInvoke));
            await Task.WhenAll(tasks);
        }
        
        foreach (Creature cr in caster.Battle.AllCreatures)
        {
            List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null, cr);
            foreach (DrawnRune dr in drawnRunes)
            {
                if (dr.AfterInvokingRune == null)
                    continue;
                var tasks = dr.AfterInvokingRune.GetInvocationList()
                    .Cast<Func<DrawnRune, CombatAction, DrawnRune, Task>>()
                    .Select(d =>
                        d.Invoke(dr, sourceAction, runeToInvoke));
                await Task.WhenAll(tasks);
            }
        }
    }

    /// <summary>
    /// The CASTING creature uses the SOURCE combat action to INVOKE a DrawnRune on the TARGET creature.
    /// </summary>
    /// <param name="sourceAction">The CombatAction which invoked the rune.</param>
    /// <param name="caster">The Creature invoking the DrawnRune.</param>
    /// <param name="target">The Creature whose DrawnRune will be invoked. If null, you'll be asked to select a Creature with a DrawnRune.</param>
    /// <param name="runeFilter">(nullable) A lambda which returns true if the Rune is a valid option to invoke.</param>
    /// <param name="canBeCanceled">Whether the attempt to invoke the rune can be canceled.</param>
    /// <param name="passText">String to use for the pass text. Default value is " Confirm no trace action ".</param>
    /// <param name="additionalTopText">Additional text to display after "Choose a rune to invoke."</param>
    /// <returns>(bool) False if the action was canceled or passed, otherwise true.</returns>
    public static async Task<bool> PickARuneToInvokeOnTarget(
        CombatAction sourceAction,
        Creature caster,
        Creature? target = null,
        Func<Rune, bool>? runeFilter = null,
        bool? canBeCanceled = false,
        string? passText = null,
        string? additionalTopText = null)
    {
        // Get available runes
        RunicRepertoireFeat? repertoireFeat = RunicRepertoireFeat.GetRepertoireOnCreature(caster);
        if (repertoireFeat == null)
            return true;
        
        // Generate options
        List<Option> options = [];
        foreach (Rune rune in repertoireFeat.GetRunesKnown(caster).Where(rune => runeFilter == null || runeFilter.Invoke(rune) == true))
        {
            caster.Battle.AllCreatures.ForEach(cr =>
            {
                DrawnRune.GetDrawnRunes(caster, cr)
                    .Where(dr => dr.Rune == rune && !dr.Disabled)
                    .ToList()
                    .ForEach(dr =>
                    {
                        CombatAction? newInvokeAction =
                            CommonRuneRules.CreateInvokeAction(sourceAction, caster, dr, rune)
                                ?.WithActionCost(0); // Use at normal range.
                        if (newInvokeAction != null)
                            GameLoop.AddDirectUsageOnCreatureOptions(newInvokeAction, options, false);
                    });
            });
        }
        
        // Remove options if a target is specified
        if (target != null)
            options.RemoveAll(option =>
                option is CreatureOption crOpt && crOpt.Creature != target);
        
        // Add bells and whistles to options
        if (options.Count <= 0)
            return true;
        if (canBeCanceled == true)
            options.Add(new CancelOption(true));
        options.Add(new PassViaButtonOption(passText ?? " Confirm no trace action "));
        
        // Pick a target
        string topBarText = "Choose a rune to invoke"
                            + (target != null ? $" on {target.Name}" : null)
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
                sourceAction.RevertRequested = true;
                return false;
            case PassViaButtonOption:
                return false;
        }
        
        // Execute chosen option
        await chosenOption.Action();
        return true;

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
    #endregion

    #region Misc
    /// <summary>
    /// <para>Sets its DrawnOn to the new DrawnOn and moves the DrawnRune from its old Owner to its new Owner.</para>
    /// <para>Use Case: the Transpose Etching feat which allows you to move a rune from one target to another.</para>
    /// <para>WARNING: Does no legality-checking. Just saves a few lines of code.</para>
    /// </summary>
    /// <param name="rune">(DrawnRune) The rune to move.</param>
    /// <param name="newOwner">(Creature) The creature who will own the DrawnRune.</param>
    /// <param name="newDrawnOn">(Creature, DrawnRune, Item) The new "real" target from the newOwner to apply the DrawnRune to, such as an item wielded by the newOwner, the creature itself, or another DrawnRune.</param>
    public static async void MoveRuneToTarget(DrawnRune rune, Creature newOwner, object? newDrawnOn)
    {
        // Might need expanded functionality in the future.
        
        if (newDrawnOn != null)
        {
            rune.DrawnOn = newDrawnOn;
            if (newDrawnOn is DrawnRune dr)
                dr.AttachedDiacritic = rune;
        }
        if (rune.Owner != newOwner)
        {
            rune.Owner.RemoveAllQEffects(qf => qf == rune);
            newOwner.AddQEffect(rune);
        }
    }
    
    /// <summary>
    /// Decider attempts to select drawn runes on any creatures in battle. Does not require that the decider owns the runes.
    /// </summary>
    /// <param name="decider">The creature deciding which drawn rune to select.</param>
    /// <param name="possibleTargets">The list of creatures that can be chosen from.</param>
    /// <param name="illustration">The top-bar icon.</param>
    /// <param name="question">The top-bar string.</param>
    /// <param name="passButtonCaption"></param>
    /// <param name="canBeCanceled">Whether you can right-click to cancel the request.</param>
    /// <param name="runeFilter">A function which filters out valid choices (such as drawn runes the decider owns, or preventing Transpose Etching from selecting a tattoo or runic reprisal trap).</param>
    /// <returns></returns>
    public static async Task<DrawnRune?> AskToChooseADrawnRune(
        Creature decider,
        IEnumerable<Creature> possibleTargets,
        Illustration illustration,
        string question,
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
            List<DrawnRune> runes = DrawnRune.GetDrawnRunes(null, cr);
            if (runeFilter != null)
                runes = runes.Where(runeFilter).ToList();
            runes.ForEach(dr =>
            {
                options.Add(new CreatureOption(
                    cr,
                    $"Pick up {{Blue}}{dr.Rune.Name}{{/Blue}}",
                    async () => chosenRune = dr,
                    int.MinValue,
                    false)
                {
                    Illustration = dr.Illustration,
                    ContextMenuText = dr.Rune.Name,
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
}