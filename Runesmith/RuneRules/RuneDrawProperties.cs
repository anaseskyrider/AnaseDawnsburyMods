using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.IO;
using Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

/// <summary>
/// The properties of a <see cref="Rune"/> as it relates to drawing that rune.
/// </summary>
/// <param name="usageText">The description of the rune's usage entry without sentence case formatting and without trailing punctuation or spaces.</param>
public class RuneDrawProperties(
    string usageText)
{
    public Rune Self { get; internal set; } = null!;
    

    #region Text Entry

    /// <summary>
    /// Gets or sets the unformatted usage description of the rune.
    /// </summary>
    /// <remarks>
    /// This should not be in sentence case, and should lack punctuation.
    /// </remarks>
    /// <example>"drawn on a shield"</example>
    /// <value>
    /// The description of the rune's usage entry without sentence case formatting and without trailing punctuation or spaces.
    /// </value>
    /// <seealso cref="UsageTextWithFormatting"/>
    public string UsageText
    {
        get;
        set
        {
            string input = value;
            input = input.Uncapitalize();
            input = input.TrimEnd('.', ',', ' ');
            field = input;
        }
    } = usageText;

    /// <summary>
    /// Gets the rune's usage entry with formatting.
    /// </summary>
    /// <seealso cref="UsageText"/>
    public string UsageTextWithFormatting =>
        $"{{b}}Usage{{/b}} {this.UsageText}.";

    #endregion

    #region Targeting

    /// <summary>
    /// The requirements necessary to draw this rune.
    /// </summary>
    /// <remarks>
    /// This should correlate to the Usage entry of a rune's description.
    /// </remarks>
    public List<CreatureTargetingRequirement> ActionTargetingRequirements { get; } = [];
    
    /// <summary>
    /// Whether this rune can be applied by YOU onto the TARGET.
    /// </summary>
    public Usability IsLegalTarget(Creature runesmith, Creature target)
    {
        foreach (CreatureTargetingRequirement req in ActionTargetingRequirements)
        {
            // Return the first unusable reason found
            if (req.Satisfied(runesmith, target)
                is { CanBeUsed: false } usable)
                return usable;
        }

        return Usability.Usable;
    }
    
    /// <summary>
    /// Constructs and adds an additional <see cref="LegacyCreatureTargetingRequirement"/>.
    /// </summary>
    public RuneDrawProperties WithAdditionalRequirement(
        Func<Creature, Creature, Usability> additionalConditionOnTarget)
    {
        return this.WithAdditionalRequirement(new LegacyCreatureTargetingRequirement(additionalConditionOnTarget));
    }
    
    /// <summary>
    /// Adds an additional <see cref="CreatureTargetingRequirement"/>.
    /// </summary>
    public RuneDrawProperties WithAdditionalRequirement(
        CreatureTargetingRequirement creatureTargetingRequirement)
    {
        this.ActionTargetingRequirements.Add(creatureTargetingRequirement);
        return this;
    }

    /// <summary>
    /// Adds an <see cref="EnemyCreatureTargetingRequirement"/> to drawing this rune.
    /// </summary>
    /// <remarks>
    /// If <see cref="ModData.BooleanOptions.UnrestrictedTrace"/> is true, this has no effect.
    /// </remarks>
    public RuneDrawProperties WithEnemyRequirement(bool hasAllyUseCases = false)
    {
        if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.UnrestrictedTrace)
            && hasAllyUseCases)
            return this;

        return this.WithAdditionalRequirement(new EnemyCreatureTargetingRequirement());
    }

    /// <summary>
    /// Adds an <see cref="FriendOrSelfCreatureTargetingRequirement"/> to drawing this rune.
    /// </summary>
    /// <remarks>
    /// If <see cref="ModData.BooleanOptions.UnrestrictedTrace"/> is true, then haseEnemyUseCases must be also be true for this function to have no effect (such as Esvadir, which buffs Strikes, but damages someone other than the bearer).
    /// </remarks>
    /// <param name="hasEnemyUseCases">If true, then this rune has use-cases where you'd want to trace it on an enemy despite being a beneficial effect. Unrestricted Trace will still restrict this rune if this is false.</param>
    /// <param name="excludeSelf">If true, this is a <see cref="FriendCreatureTargetingRequirement"/> instead.</param>
    public RuneDrawProperties WithAllyRequirement(bool hasEnemyUseCases = false, bool excludeSelf = false)
    {
        if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.UnrestrictedTrace)
            && hasEnemyUseCases)
            return this;

        return this.WithAdditionalRequirement(excludeSelf
            ? new FriendCreatureTargetingRequirement()
            : new FriendOrSelfCreatureTargetingRequirement());
    }

    /// <summary>
    /// Adds a <see cref="TargetWieldsItemCreatureTargetingRequirement"/> that requires the target to hold at least one legal item.
    /// </summary>
    /// <param name="legalItem">This function returns true if the item meets the requirements.</param>
    /// <param name="unusableWhy">The reason why the target isn't legal.</param>
    public RuneDrawProperties WithHoldsItemRequirement(
        Func<Item,bool> legalItem,
        string unusableWhy = "no weapon")
    {
        return this.WithAdditionalRequirement(
            new TargetWieldsItemCreatureTargetingRequirement(legalItem, unusableWhy));
    }

    /// <summary>
    /// Adds a <see cref="TargetWearsArmorCreatureTargetingRequirement"/> that requires the target to be wearing armor.
    /// </summary>
    /// <remarks>
    /// Works on players and NPCs alike, with limitations. For NPCs, this only cares if they're wearing metal armor or are made of metal. Otherwise, they're armorless.
    /// </remarks>
    public RuneDrawProperties WithWearsArmorRequirement()
    {
        return this.WithAdditionalRequirement(
            new TargetWearsArmorCreatureTargetingRequirement());
    }

    /// <summary>
    /// Adds multiple requirements necessary for a diacritic rune to draw onto a base rune. This does not require a target DrawnRune to be drawn by you.
    /// </summary>
    /// <param name="additionalRuneRequirement">Additional requirements, if any. Returns null if usable, or returns the reason why-not.</param>
    public RuneDrawProperties WithDiacriticTargetingRequirements(Func<DrawnRune, string?>? additionalRuneRequirement = null)
    {
        return this.WithAdditionalRequirement((a, d) =>
        {
            List<DrawnRune> drawnRunes = DrawnRune
                .GetDrawnRunes(null, d)
                .Where(dr => dr.AttachedDiacritic is null)
                .ToList();
            
            // Must be a rune-bearer
            if (drawnRunes.Count == 0)
                return Usability.NotUsableOnThisCreature("not a rune-bearer");
            
            // Must have non-diacritic runes.
            drawnRunes = drawnRunes
                .Where(dr => !dr.Rune.IsDiacriticRune)
                .ToList();
            if (drawnRunes.Count == 0)
                return Usability.NotUsableOnThisCreature("no non-diacritic runes");
            
            // Must have runes without attached diacritics.
            drawnRunes = drawnRunes
                .Where(dr => dr.AttachedDiacritic is null)
                .ToList();
            if (drawnRunes.Count == 0)
                return Usability.NotUsableOnThisCreature("diacritics already present");

            // Must meet any additional requirements
            if (additionalRuneRequirement != null)
            {
                string? unusableWhy = drawnRunes
                    .Select(additionalRuneRequirement)
                    .FirstOrDefault();
                if (unusableWhy != null)
                    return Usability.NotUsableOnThisCreature(unusableWhy);
            }
            
            return Usability.Usable;
        });
    }

    public RuneDrawProperties WithInvokeableOncePerCombatRequirement(RuneId runeId)
    {
        return this.WithAdditionalRequirement((a, d) =>
            ModData.PersistentActions.RuneIsUsedUp(a, runeId)
                ? Usability.NotUsable("Already invoked this combat")
                : Usability.Usable);
    }

    /*/// <summary>
    /// Adds the requirement that this rune can only be etched at the start of combat, not traced.
    /// </summary>
    public RuneDrawProperties WithEtchedOnlyTargetingRequirements()
    {
        return this.WithAdditionalRequirement(new EtchedAtStartOfCombatRequirement());
    }*/

    #endregion

    #region Additional Properties

    /*/// <summary>
    /// Gets whether the rune can only be etched at the start of combat.
    /// </summary>
    public bool IsEtchedOnly => this.TargetingRequirements.Any(req =>
        req is EtchedAtStartOfCombatRequirement);*/

    public bool IsDrawnOnAnyItem => this.ActionTargetingRequirements.Any(req =>
        req is TargetWieldsItemCreatureTargetingRequirement
        or TargetWearsArmorCreatureTargetingRequirement);

    #endregion

    /// <summary>
    /// As <see cref="IsDrawnOnThisItem(Item)"/>, constructing a new <see cref="Item"/> with the given trait to test.
    /// </summary>
    public bool IsDrawnOnThisItem(Trait itemTrait)
    {
        Item testItem = new Item(IllustrationName.YellowWarning, "[TEST ITEM]", itemTrait);
        return IsDrawnOnThisItem(testItem);
    }

    /// <summary>
    /// Checks whether this rune can be drawn onto this item.
    /// </summary>
    /// <param name="testItem">An item to test against.</param>
    /// <returns>True, if the item has a <see cref="TargetWieldsItemCreatureTargetingRequirement"/> and this item meets its requirement. False otherwise.</returns>
    public bool IsDrawnOnThisItem(Item testItem)
    {
        bool result = false;
        foreach (CreatureTargetingRequirement req in ActionTargetingRequirements)
        {
            if (req is TargetWieldsItemCreatureTargetingRequirement itemReq)
            {
                result = true;
                if (!itemReq.ItemRequirement(testItem))
                {
                    result = false;
                    break;
                }
            }
            else if (testItem.IsWorn && req is TargetWieldsAnItemCreatureTargetingRequirement itemReq2)
            {
                result = true;
            }
            else if (testItem.HasTrait(Trait.Armor) && req is TargetWearsArmorCreatureTargetingRequirement itemReq3)
            {
                result = true;
            }
        }

        return result;
    }

    /// <summary>
    /// Execute an action that makes miscellaneous changes.
    /// </summary>
    public RuneDrawProperties WithAdjustments(Action<RuneDrawProperties> adjustments)
    {
        adjustments(this);
        return this;
    }
}