using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
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
/// <param name="drawnOnCreature">Whether this rune is drawn on a creature and has CreatureTargetingRequirements.</param>
/// <param name="drawnOnItem">Whether this rune is drawn on an item and has ItemTargetingRequirements.</param>
/// <param name="drawnOnRune">Whether this rune is drawn on another rune and has DrawnRuneTargetingRequirements.</param>
/// <param name="drawnOnTile">Whether this rune is drawn on a tile and has ...Requirements.</param>
public class RuneDrawProperties(
    string usageText,
    bool drawnOnCreature = false,
    bool drawnOnItem = false,
    bool drawnOnRune = false,
    bool drawnOnTile = false) // PETR: Tile targeting (level 17 rune)
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
    /// If true, then this rune has <see cref="Option"/>s to target creatures.
    /// </summary>
    public bool TargetsCreatures { get; private set; } = drawnOnCreature;

    /// <summary>
    /// If true, then this rune has <see cref="Option"/>s to target items.
    /// </summary>
    public bool TargetsItems { get; private set; } = drawnOnItem;

    /// <summary>
    /// If true, then this rune has <see cref="Option"/>s to target DrawnRunes.
    /// </summary>
    public bool TargetsRunes { get; private set; } = drawnOnRune;
    
    /// <summary>
    /// If true, then this rune has <see cref="Option"/>s to target spaces.
    /// </summary>
    public bool TargetsTiles { get; private set; } = drawnOnTile;

    /// <summary>
    /// Gets the list of requirements to draw this rune on a Creature, if any.
    /// </summary>
    public List<CreatureTargetingRequirement> CreatureTargetingRequirements { get; } = [];
    
    /// <summary>
    /// Gets the list of requirements to draw this rune on an item, if any.
    /// </summary>
    public List<ItemTargetingRequirement> ItemTargetingRequirements { get; } = [];
    
    /// <summary>
    /// Gets the list of requirements to draw this rune on an item, if any.
    /// </summary>
    public List<DrawnRuneTargetingRequirement> RuneTargetingRequirements { get; } = [];
    
    public async Task<(Item? ChosenItem, DrawnRune? ChosenRune)?> ChooseRuneSubtargets(Creature decider, List<Creature> chosenCreatures)
    {
        // If this only targets a creature, then immediately move on.
        // (This is a temporary implementation, which can be changed
        // if the base Target of a draw action is ever anything other than
        // a CreatureTarget)
        if (this.TargetsCreatures
            && !(this.TargetsItems || this.TargetsRunes)
            && chosenCreatures.LastOrDefault() is not null)
        {
            return (null, null);
        }

        string tooltip = CommonRuneRules.CreateTraceActionDescription(
            this.Self,
            decider.Level,
            withFlavorText: false,
            withUsageText: false);
        
        // Disabled for now. Trace actions still only target creatures.
        /*List<Creature> creatures = caster.Battle
            .AllCreatures
            .Where(cr =>
                cr.DistanceTo(caster) <= rangeToTarget)
            .ToList();*/
        /*List<Tile> tiles = caster.Battle.Map
            .AllTiles
            .Where(tile => tile.DistanceTo(caster) <= rangeToTarget)
            .ToList();*/

        Item? chosenItem = null;
        DrawnRune? chosenRune = null;
        List<Creature> creatures = chosenCreatures.ToList();
        List<Option> options = [];
        
        /*// Creature targeting
        if (drawProps.TargetsCreatures)
        {
            options.AddRange(
                creatures
                    .Where(cr =>
                        drawProps.IsLegalTarget(caster, cr))
                    .Select(cr => new CreatureOption(
                        cr,
                        $"Trace {tag.Rune.Id.ToWord()} on {cr.Name}",
                        async () => tag.ChosenCreature = cr,
                        0f, // TODO: Goodness
                        false)
                        {
                            Illustration = new SideBySideIllustration(
                                tag.Rune.Illustration,
                                Constants.ActionCountToIllustration(drawAction.ActionCost)),
                            //ContextMenuText = ,
                        }
                        .WithTooltip(tooltip)
                    )
                );
        }*/
        
        // Item targeting
        if (this.TargetsItems)
        {
            // Add items from creatures
            foreach (Creature cr in creatures)
            {
                List<Item> items = CommonRuneRules.GetCommonTargetableItems(cr);
                options.AddRange(items
                    .Where(item => this.IsLegalTarget(decider, item))
                    .Select(item => TargetItemOnCreature(cr, item))
                );
            }

            // Add items from tiles
            // (Disabled until QEffects have non-creature persistence mechanisms)
            /*foreach (Tile tile in tiles)
            {
                List<Item> items = tile.DroppedItems.ToList();
                options.AddRange(items
                    .Where(item => drawProps.IsLegalTarget(caster, item))
                    .Select(item => new TileOption(
                            tile,
                            $"Trace {tag.Rune.Id.ToWord()} on dropped {item.Name}",
                            async () => tag.ChosenItem = item,
                            0f,
                            false)
                        {
                            Illustration = new SideBySideIllustration(
                                tag.Rune.Illustration,
                                Constants.ActionCountToIllustration(drawAction.ActionCost)),
                        }
                        .WithTooltip(tooltip)
                    )
                );
            }*/
        }
        // DrawnRune targeting
        // (A rune never targets both an item and a drawn rune)
        else if (this.TargetsRunes)
        {
            // Add runes from creatures
            foreach (Creature cr in creatures)
            {
                // Diacritics can target non-owned runes
                List<DrawnRune> drawnRunes = DrawnRune.GetDrawnRunes(null,cr);
                options.AddRange(drawnRunes
                    .Where(dr => this.IsLegalTarget(decider, dr))
                    .Select(dr => TargetRuneOnCreature(cr, dr))
                );
            }
        }

        if (options.Count == 0)
            return null;

        Option chosenOption;

        // If no confirmation
        if ((this.TargetsCreatures
             || this.TargetsItems
             || this.TargetsRunes)
            && options.Count == 1)
        {
            chosenOption = options.First();
        }
        else
        {
            options.Add(new CancelOption(true));
            options.Add(new PassViaButtonOption("Revert"));

            string question =
                $"Choose {(this.TargetsItems ? "item" : this.TargetsRunes ? "rune" : this.TargetsTiles ? "space" : "???").WithIndefiniteArticle()} to draw on.";
            
            chosenOption = (await decider.Battle.SendRequest(new AdvancedRequest(
                decider,
                question,
                options)
            {
                TopBarIcon = Self.Illustration,
                TopBarText = question
            })).ChosenOption;
            
            Sfxs.Play(SfxName.OminousActivation);
        }
        
        if (chosenOption is CancelOption or PassViaButtonOption)
            return null;

        await chosenOption.Action();

        return (chosenItem, chosenRune);
        
        CreatureOption TargetItemOnCreature(Creature cr, Item item)
        {
            return (new CreatureOption(
                    cr,
                    item.Name,
                    async () => chosenItem = item,
                    0f,
                    false)
                {
                    Illustration = item.Illustration
                }
                .WithTooltip(tooltip) as CreatureOption)!;
        }

        CreatureOption TargetRuneOnCreature(Creature cr, DrawnRune dr)
        {
            return (new CreatureOption(
                    cr,
                    dr.Name ?? "[NO TEXT FOUND]",
                    async () => chosenRune = dr,
                    0f,
                    false)
                {
                    Illustration = dr.Illustration
                }
                .WithTooltip(tooltip) as CreatureOption)!;
        }
    }
    
    /// <summary>
    /// Whether this rune can be applied by YOU onto the CREATURE.
    /// </summary>
    public Usability IsLegalTarget(Creature runesmith, Creature target)
    {
        if (!this.TargetsCreatures)
            return Usability.NotUsable("Does not target creatures");
        
        foreach (var req in this.CreatureTargetingRequirements)
        {
            // Return the first unusable reason found
            if (req.Satisfied(runesmith, target)
                is { CanBeUsed: false } usable)
                return usable;
        }

        return Usability.Usable;
    }

    /// <summary>
    /// Whether this rune can be applied by YOU onto the ITEM.
    /// </summary>
    public Usability IsLegalTarget(Creature runesmith, Item item)
    {
        if (!this.TargetsItems)
            return Usability.NotUsable("Does not target items");
        
        foreach (var req in this.ItemTargetingRequirements)
        {
            // Return the first unusable reason found
            if (req.Satisfied(runesmith, item)
                is { CanBeUsed: false } usable)
                return usable;
        }

        return Usability.Usable;
    }

    /// <summary>
    /// Whether this rune can be applied by YOU onto the ITEM.
    /// </summary>
    public Usability IsLegalTarget(Creature runesmith, DrawnRune drawnRune)
    {
        if (!this.TargetsRunes)
            return Usability.NotUsable("Does not target other runes");
        
        foreach (var req in this.RuneTargetingRequirements)
        {
            // Return the first unusable reason found
            if (req.Satisfied(runesmith, drawnRune)
                is { CanBeUsed: false } usable)
                return usable;
        }

        return Usability.Usable;
    }
    
    /// <summary>
    /// Constructs and adds an additional <see cref="LegacyCreatureTargetingRequirement"/>.
    /// </summary>
    public RuneDrawProperties WithCreatureRequirement(
        Func<Creature, Creature, Usability> additionalConditionOnTarget)
    {
        return this.WithCreatureRequirement(new LegacyCreatureTargetingRequirement(additionalConditionOnTarget));
    }
    
    /// <summary>
    /// Adds an additional <see cref="CreatureTargetingRequirement"/>.
    /// </summary>
    public RuneDrawProperties WithCreatureRequirement(
        CreatureTargetingRequirement creatureTargetingRequirement)
    {
        this.CreatureTargetingRequirements.Add(creatureTargetingRequirement);
        return this;
    }

    /// <summary>
    /// Adds an <see cref="EnemyCreatureTargetingRequirement"/> to drawing this rune.
    /// </summary>
    /// <remarks>
    /// If <see cref="ModData.BooleanOptions.UnrestrictedTrace"/> is true, this has no effect.
    /// </remarks>
    public RuneDrawProperties WithEnemyRequirement()
    {
        if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.UnrestrictedTrace))
            return this;

        return this.WithCreatureRequirement(new EnemyCreatureTargetingRequirement());
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

        return this.WithCreatureRequirement(excludeSelf
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
        /*this.CreatureTargetingRequirements.Add(new TargetWieldsItemCreatureTargetingRequirement(legalItem, unusableWhy));*/

        this.ItemTargetingRequirements.Add(new LegacyItemTargetingRequirement((a, i) =>
            legalItem(i)
                ? Usability.Usable
                : Usability.NotUsableOnThisCreature(unusableWhy)));
        return this;
    }

    /// <summary>
    /// Adds a <see cref="TargetWearsArmorCreatureTargetingRequirement"/> that requires the target to be wearing armor.
    /// </summary>
    /// <remarks>
    /// Works on players and NPCs alike, with limitations. For NPCs, this only cares if they're wearing metal armor or are made of metal. Otherwise, they're armorless.
    /// </remarks>
    public RuneDrawProperties WithWearsArmorRequirement()
    {
        return this.WithCreatureRequirement(
            new TargetWearsArmorCreatureTargetingRequirement());
    }

    /// <summary>
    /// Adds the basic requirements for a diacritic rune to draw onto another rune, such as the target rune not being a diacritic and not having a diacritic already attached. This does not require a target DrawnRune to be drawn by you.
    /// </summary>
    public RuneDrawProperties WithDiacriticTargetingRequirements()
    {
        this.WithCreatureRequirement(new TargetIsARuneBearer(true));
        this.RuneTargetingRequirements.Add(new DiacriticTargetingRequirement());
        return this;
    }

    public RuneDrawProperties WithBaseRuneDealsDamage(bool includeMarssyl = false)
    {
        this.RuneTargetingRequirements.Add(new LegacyRuneTargetingRequirement((a, dr) =>
            dr.Rune.InvocationProperties.DealsDamage
            || (includeMarssyl && dr.Rune.Id == RuneId.Marssyl)
                ? Usability.Usable
                : Usability.NotUsableOnThisCreature("Base rune doesn't deal damage")));
        return this;
    }

    public RuneDrawProperties WithBaseRuneIsNotArea()
    {
        this.RuneTargetingRequirements.Add(new LegacyRuneTargetingRequirement((a, dr) =>
            dr.Rune.InvocationProperties.AffectsArea
                ? Usability.NotUsableOnThisCreature("Base rune already affects an area")
                : Usability.Usable));
        return this;
    }

    public RuneDrawProperties WithInvokeableOncePerCombatRequirement(RuneId runeId)
    {
        return this.WithCreatureRequirement((a, d) =>
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
    
    public bool IsDrawnOnlyOnCreatures =>
        this.TargetsCreatures
        && !this.TargetsItems
        && !this.TargetsRunes
        /*&& !this.CreatureTargetingRequirements.Any(req =>
            req is TargetWieldsItemCreatureTargetingRequirement
                or TargetWearsArmorCreatureTargetingRequirement)*/;

    public bool IsDrawnOnlyOnItems =>
        (this.TargetsItems /*|| this.CreatureTargetingRequirements.Any(req =>
            req is TargetWieldsItemCreatureTargetingRequirement
                or TargetWearsArmorCreatureTargetingRequirement)*/)
        && !this.TargetsCreatures
        && !this.TargetsRunes;
    
    public bool IsDrawnOnlyOnRunes =>
        this.TargetsRunes
        && !this.TargetsCreatures
        && !this.TargetsItems
        && !this.CreatureTargetingRequirements.Any(req =>
            req is TargetWieldsItemCreatureTargetingRequirement
                or TargetWearsArmorCreatureTargetingRequirement);

    #endregion

    /// <summary>
    /// Checks whether this rune can be drawn onto this item.
    /// </summary>
    /// <param name="runesmith">The creature drawing this rune.</param>
    /// <param name="itemTrait">An trait you expect to be used for a rune's item requirements. A new item is created with this trait to check against.</param>
    public bool IsDrawnOnThisItem(Creature runesmith, Trait itemTrait)
    {
        Item testItem = new Item(IllustrationName.YellowWarning, "[TEST ITEM]", itemTrait);
        return this.IsLegalTarget(runesmith, testItem);
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