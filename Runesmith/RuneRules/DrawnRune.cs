using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

/// <summary>
/// DrawnRunes are <see cref="QEffect"/>s which represent the application of a runesmith's runes.
/// </summary>
/// <remarks>
/// A DrawnRune corresponds to a single application of a rune on the battlefield, like in the tabletop, similar to how <see cref="CombatAction"/>s translate a <see cref="Spell"/> to its use in combat. When creating DrawnRunes, keep the following principles in mind:
/// <list type="bullet">
/// <item><see cref="QEffect.Source"/> and <see cref="QEffect.SourceAction"/> are the runesmith and the action they used to draw the rune, respectively.</item>
/// <item><see cref="QEffect.Owner"/> is the RUNE-BEARER.</item>
/// <item><see cref="DrawnOn"/> is the <see cref="Item"/> or <see cref="DrawnRune"/> true target of the rune.</item>
/// <item><see cref="DrawnRune.Rune"/> is the <see cref="RuneRules.Rune"/> that this is a drawn instance of. This houses lots of data for look-up, such as its <see cref="Rune.DrawProperties"/> and <see cref="Rune.InvocationProperties"/>.</item>
/// <item><see cref="QEffect.Traits"/> always possesses the [Rune, Runesmith, Magical] traits, as well as a trait identifying the way it was applied (Traced, Etched, Tattooed), and sometimes also a specific tradition trait. Use <see cref="Traditions"/> to get what traditions the application qualifies as, and <see cref="DrawTrait"/> to get this duration trait.</item>
/// <item>There should only be one DrawnRune for each drawing of a rune. As much as possible, it should have all the necessary properties, and apply other effects in an Ephemeral way. This allows that instance to be targeted, such as by Invoke Rune, and updated, moved around, or removed.</item>
/// <item>As much as possible, references should be passed as data to the DrawnRune and then referenced again later, rather than captured. This allows for effects to update the properties and fields of a DrawnRune instance.</item>
/// </list>
/// </remarks>
public class DrawnRune : QEffect
{
    #region Static Data

    public static readonly List<Trait> DrawTraits =
    [
        ModData.Traits.Tattooed,
        ModData.Traits.Etched,
        ModData.Traits.Traced,
    ];

    #endregion

    #region Fields (variables)
    
    /// <summary>
    /// The <see cref="Rune"/> represented by this QEffect.
    /// </summary>
    public Rune Rune { get; }

    /// <summary>
    /// Any object which abstractly represents the "true" target of the QEffect, what it's "actually" drawn on.
    /// </summary>
    /// <remarks>
    /// <para> The types are generally of: <see cref="Creature"/>, <see cref="DrawnRune"/> (used for diacritics), <see cref="Dawnsbury.Core.Mechanics.Treasure.Item"/> (used for weapons, unarmed strikes).</para>
    /// <para>Used by other parts of the mod to assist with behavior such as disabling a QEffect (without automated C# garbage collection causing problems) when no creature is holding the item that the rune is drawn onto.</para>
    /// </remarks>
    public object? DrawnOn { get; set; }
    
    /// <summary>
    /// The diacritic rune attached to this DrawnRune, if any.
    /// </summary>
    public DrawnRune? AttachedDiacritic { get; set; }

    /// <summary>
    /// See <see cref="Disabled"/> for functionality.
    /// </summary>
    private bool DisablePassive { get; set; }
    
    /// <summary>
    /// Get or set the function which generates a new description from the Item it's attached to.
    /// </summary>
    /// <list type="bullet">
    /// <item><see cref="DrawnRune"/>: This DrawnRune.</item>
    /// <item><see cref="Item"/>: The Item that this rune is drawn onto.</item>
    /// </list>
    /// <returns>(string) The new description of the rune effect.</returns>
    public Func<DrawnRune,Item,string>? ItemDescriptionGenerator { get; set; }
    
    /// <summary>
    /// Get or set the function which generates a new description from the DrawnRune it's attached to.
    /// </summary>
    /// <list type="bullet">
    /// <item><see cref="DrawnRune"/>: This diacritic DrawnRune.</item>
    /// <item><see cref="Item"/>: The DrawnRune that this diacritic rune is drawn onto.</item>
    /// </list>
    /// <returns>(string) The new description of the rune effect.</returns>
    public Func<DrawnRune,DrawnRune,string>? DiacriticDescriptionGenerator { get; set; }

    #endregion
    
    #region Initializers

    /// <summary>
    /// Create a temporary drawn instance of a <see cref="Rune"/> in combat.
    /// </summary>
    /// <remarks>
    /// The default duration is <see cref="ExpirationCondition.Ephemeral"/> and must be set later. The default illustration is from <see cref="Rune.Illustration"/>.
    /// </remarks>
    /// <param name="drawAction">The action which drew the rune.</param>
    /// <param name="rune">The base that this rune is a drawn instance of.</param>
    /// <param name="description">The description of this effect for display on a creature token.</param>
    /// <seealso cref="QEffect(string, string, ExpirationCondition, Creature?, Illustration?)"/>
    public DrawnRune(
        CombatAction drawAction,
        Rune rune,
        string description)
        : base(
            rune.FullName,
            description,
            ExpirationCondition.Ephemeral,
            drawAction.Owner,
            rune.Illustration)
    {
        this.SourceAction = drawAction;
        this.Rune = rune;
        this.Traits = new List<Trait>(rune.Traits);
        
        // If this rune doesn't already have a specific tradition...
        if (!this.Traits.Any(trait => trait.IsTraditionTrait()))
        {
            // Add the traditions of whatever skills you're trained in.
            if (this.Source!.Skills.IsTrained(Skill.Arcana))
                this.Traits.Add(Trait.Arcane);
            if (this.Source!.Skills.IsTrained(Skill.Religion))
                this.Traits.Add(Trait.Divine);
            if (this.Source!.Skills.IsTrained(Skill.Nature))
                this.Traits.Add(Trait.Primal);
            if (this.Source!.Skills.IsTrained(Skill.Occultism))
                this.Traits.Add(Trait.Occult);
        }
    }

    /// <summary>
    /// Creates a temporary drawn instance of a <see cref="Rune"/> in combat. This overload draws onto an item.
    /// </summary>
    /// <param name="drawAction">The action which drew the rune.</param>
    /// <param name="rune">The base that this rune is a drawn instance of.</param>
    /// <param name="itemDescriptionGenerator">The function that generates this description when the item changes. See: <see cref="ItemDescriptionGenerator"/></param>
    /// <param name="drawnOn">The item this was drawn on.</param>
    /// <seealso cref="QEffect(string, string, ExpirationCondition, Creature?, Illustration?)"/>
    public DrawnRune(
        CombatAction drawAction,
        Rune rune,
        Func<DrawnRune,Item,string> itemDescriptionGenerator,
        Item drawnOn)
        : this(
            drawAction,
            rune,
            "")
    {
        this.ItemDescriptionGenerator = itemDescriptionGenerator;
        this.Description = itemDescriptionGenerator(this, drawnOn);
        this.WithDrawnOnItem(drawnOn, null);
    }

    /// <summary>
    /// Creates a temporary drawn instance of a <see cref="Rune"/> in combat. This overload draws onto a rune.
    /// </summary>
    /// <param name="drawAction">The action which drew the rune.</param>
    /// <param name="rune">The base that this rune is a drawn instance of.</param>
    /// <param name="diacriticDescriptionGenerator">The function that generates this description when the rune it's drawn onto changes. See: <see cref="DiacriticDescriptionGenerator"/></param>
    /// <param name="drawnOn">The DrawnRune this was drawn on.</param>
    /// <seealso cref="QEffect(string, string, ExpirationCondition, Creature?, Illustration?)"/>
    public DrawnRune(
        CombatAction drawAction,
        Rune rune,
        Func<DrawnRune,DrawnRune,string> diacriticDescriptionGenerator,
        DrawnRune drawnOn)
        : this(
            drawAction,
            rune,
            "")
    {
        this.DiacriticDescriptionGenerator = diacriticDescriptionGenerator;
        this.Description = diacriticDescriptionGenerator(this, drawnOn);
        this.WithDrawnOnRune(drawnOn, null);
    }
    
    #endregion

    #region Callbacks
    
    // I have no idea what I want these to do just yet.
    
    // public Func<????>? BeforeApplyingRune { get; set; }
    
    // public Func<????>? AfterApplyingRune { get; set; }

    /// <summary>
    /// Happens before you are invoked or the rune you're attached to is invoked.
    /// </summary>
    /// <list type="bullet">
    /// <item><see cref="DrawnRune"/>: The drawn rune this lambda is being called on.</item>
    /// <item><see cref="CombatAction"/>: The action invoking the rune.</item>
    /// <item><see cref="DrawnRune"/>: The drawn rune that is about to be invoked.</item>
    /// </list>
    public Func<DrawnRune, CombatAction, DrawnRune, Task>? BeforeInvokingRune { get; set; }
    
    /// <summary>
    /// Happens after you are invoked or the rune you're attached to is invoked, before any runes are removed.
    /// </summary>
    /// <list type="bullet">
    /// <item><see cref="DrawnRune"/>: The drawn rune this lambda is being called on.</item>
    /// <item><see cref="CombatAction"/>: The action that invoked the rune.</item>
    /// <item><see cref="DrawnRune"/>: The drawn rune that was just invoked.</item>
    /// </list>
    public Func<DrawnRune, CombatAction, DrawnRune, Task>? AfterInvokingRune { get; set; }
    
    #endregion
    
    #region Properties (set get accessors)
    
    /// <summary>
    /// If true, the drawn rune doesn't execute its passive behavior, such as Atryl lowering fire resistance.
    /// </summary>
    /// <remarks>
    /// To set true/false, use <see cref="EnableRune"/> and <see cref="DisableRune"/>
    /// </remarks>
    public bool Disabled => this.Hidden || this.DisablePassive;

    /// <summary>
    /// Gets this DrawnRune's <see cref="DrawTraits"/> trait.
    /// </summary>
    /// <exception cref="DrawTrait">Must be a trait contained in <see cref="DrawTraits"/>.</exception>
    public Trait DrawTrait =>
        this.Traits.First(trait =>
            DrawTraits.Contains(trait));

    /// <summary>
    /// Gets this drawn rune's traditions.
    /// </summary>
    public List<Trait> Traditions =>
        this.Traits.Where(trait => trait.IsTraditionTrait()).ToList();

    public bool IsArcane => this.Traditions.Contains(Trait.Arcane);
    
    public bool IsDivine => this.Traditions.Contains(Trait.Divine);
    
    public bool IsPrimal => this.Traditions.Contains(Trait.Primal);
    
    public bool IsOccult => this.Traditions.Contains(Trait.Occult);

    #endregion

    #region Methods

    public static bool IsARuneBearer(Creature? runesmith, Creature runeBearer, bool includeDisabled = false)
    {
        return GetDrawnRunes(runesmith, runeBearer, includeDisabled).Count > 0;
    }

    /// <summary>
    /// Gets the list of DrawnRunes on the rune-bearing Creature.
    /// </summary>
    /// <param name="runesmith">(nullable) The creature who drew the runes. If null, provides all DrawnRunes.</param>
    /// <param name="runeBearer">The creature with runes drawn on them.</param>
    /// <param name="includeDisabled">If true, include disabled runes (such as for the Runic Reprisal feat).</param>
    public static List<DrawnRune> GetDrawnRunes(Creature? runesmith, Creature runeBearer, bool includeDisabled = false)
    {
        List<DrawnRune> drawnRunes = runeBearer.QEffects
            .Where(qf =>
                qf is DrawnRune dr
                && (runesmith == null || dr.Source == runesmith)
                && dr.Traits.Contains(ModData.Traits.Rune)
                && !dr.Traits.Contains(ModData.Traits.Invocation)
                && (!dr.Disabled || includeDisabled))
            .Cast<DrawnRune>()
            .ToList();
        return drawnRunes;
    }

    public static List<DrawnRune> GetAllDrawnRunes(Creature runesmith, bool includeDisabled = false)
    {
        return runesmith.Battle.AllCreatures
            .SelectMany(cr =>
                DrawnRune.GetDrawnRunes(runesmith, cr, includeDisabled))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Is this QEffect an invokeable rune.
    /// </summary>
    /// <param name="runesmith">(nullable) The creature who drew the runes. If null, a source is not required.</param>
    /// <param name="qf">The QEffect which must be a DrawnRune.</param>
    /// <param name="includeDisabled">If true, include disabled runes (such as for the Runic Reprisal feat).</param>
    public static bool IsInvokeableRune(Creature? runesmith, QEffect qf, bool includeDisabled = false)
    {
        return qf is DrawnRune dr
               && (runesmith is null || dr.Source == runesmith)
               && dr.Traits.Contains(ModData.Traits.Rune) // with the rune trait,
               && !dr.Traits.Contains(ModData.Traits.Invocation) // but not the invocation trait,
               && !dr.Traits.Contains(ModData.Traits.Diacritic) // nor is a diacritic rune.
               && (!dr.Disabled || includeDisabled)
               && (dr.Rune.InvocationProperties.TargetingRequirements.Count == 0
                   || runesmith is null
                   || runesmith.Battle.AllCreatures.Any(cr =>
                       dr.Rune.InvocationProperties.IsLegalTarget(runesmith, cr)));
    }
    
    /// <summary>
    /// Gets the tooltip description of this trait as a draw duration trait.
    /// </summary>
    public static string DrawTraitToDescription(Trait trait) =>
        trait.ToStringOrTechnical().WithTag("i").WithColor("Firebrick") + "\n";

    /// <summary>
    /// Gets the draw duration trait from the list, if any.
    /// </summary>
    /// <seealso cref="DrawTraits"/>
    public static Trait? GetDrawTraitFromList(IEnumerable<Trait> traits)
    {
        Trait? drawTrait = traits.FirstOrDefault(trait =>
            trait == ModData.Traits.Tattooed
            || trait == ModData.Traits.Etched
            || trait == ModData.Traits.Traced);

        return drawTrait;
    }

    public string TraditionsToDescription() =>
        string.Join(", ", this.Traditions.Select(trait => trait.ToStringOrTechnical())).WithTag("i")
            .WithColor("Firebrick") + "\n";

    public void UpdateDescription()
    {
        const string tattoo =
            "\n\n{i}{Blue}If invoked, this rune won't be available until your next daily preparations.{/Blue}{/i}";
        
        foreach (Trait trait in DrawTraits)
        {
            this.Description = this.Description!
                .Replace(DrawTraitToDescription(trait), "")
                .Replace(TraditionsToDescription(), "")
                .Replace(tattoo, "");
        }

        Trait drawTrait = this.DrawTrait;
        
        this.Description =
            $"{DrawTraitToDescription(drawTrait)}{TraditionsToDescription()}{this.Description}";
        
        if (drawTrait == ModData.Traits.Tattooed)
            this.Description += tattoo;
    }
    
    /// <summary>
    /// Sets the rune as disabled and optionally retains the icon on the creature portrait.
    /// </summary>
    /// <param name="showRuneOnCreature"></param>
    /// <seealso cref="Disabled"/>
    public void DisableRune(bool showRuneOnCreature = false)
    {
        this.DisablePassive = true;
        if (!showRuneOnCreature)
            this.HideFromPortrait = true;
    }

    /// <summary>
    /// Sets the rune as enabled and optionally keeps the icon hidden on the creature portrait.
    /// </summary>
    /// <param name="showRuneOnCreature"></param>
    /// <seealso cref="Disabled"/>
    public void EnableRune(bool showRuneOnCreature = true)
    {
        this.DisablePassive = false;
        if (showRuneOnCreature)
            this.HideFromPortrait = false;
    }

    /// <summary>
    /// Gets whether this instance is the first instance among multiple drawn runes of this kind.
    /// </summary>
    /// <remarks>
    /// Used to determine whether an effect should apply more than once by only applying for the first instance.
    /// </remarks>
    public bool IsFirstInstanceOf()
    {
        return this.Owner.QEffects.FirstOrDefault(qf =>
            qf is DrawnRune dr
            && dr.Rune.Id == this.Rune.Id) == this;
    }

    public new DrawnRune WithTrait(Trait trait)
    {
        return ((this as QEffect).WithTrait(trait) as DrawnRune)!;
    }
    
    /// <summary>
    /// Applies the supplied Etched or Traced trait, and modifies the duration of the QEffect according to Etched and Traced behavior.
    /// </summary>
    /// <param name="newTrait">The <see cref="ModData.Traits.Etched"/> or <see cref="ModData.Traits.Traced"/> trait.</param>
    /// <returns></returns>
    public DrawnRune WithDrawDuration(Trait? newTrait)
    {
        if (!newTrait.HasValue)
            return this;
        
        foreach (Trait drawTrait in DrawTraits)
            this.Traits.Remove(drawTrait);
        
        this.Traits.Add(newTrait.Value);

        if (newTrait.Value == ModData.Traits.Tattooed
            || newTrait.Value == ModData.Traits.Etched)
            this.ExpiresAt = ExpirationCondition.Never;
        else if (newTrait.Value == ModData.Traits.Traced)
            this.WithExpirationAtEndOfSourcesNextTurn(this.Source!, true);

        UpdateDescription();

        return this;
    }

    /// <summary>
    /// Regulates an item to behave as if it's attached to an Item instead of a Creature.
    /// </summary>
    /// <remarks>
    /// This uses a StateCheck, so it's ideal to not overwrite the state and instead add onto it.
    /// </remarks>
    /// <param name="drawTarget">The item being drawn onto.</param>
    /// <param name="newDescriptionGenerator">If this function is called for any reason without using the DrawnRune constructor that requires a <see cref="ItemDescriptionGenerator"/>, this is that new generator.</param>
    private DrawnRune WithDrawnOnItem(Item drawTarget, Func<DrawnRune,Item,string>? newDescriptionGenerator)
    {
        this.DrawnOn = drawTarget;
        if (newDescriptionGenerator is not null)
            this.ItemDescriptionGenerator = newDescriptionGenerator;
        this.StateCheck += qfThis =>
        {
            DrawnRune drThis = (qfThis as DrawnRune)!;
            
            // Find who is "holding" this item or unarmed attack
            Creature? holder = null;
            if (drThis.DrawnOn is Item drawnOn)
            {
                // Regenerate the description in case the rune's been moved to a new item.
                // (This shouldn't be null, but it's a solid fallback in case it is)
                drThis.Description = this.ItemDescriptionGenerator?.Invoke(drThis, drawnOn) ?? "[NO DESCRIPTION]";
                drThis.UpdateDescription();

                // Find the first creature who possesses this item.
                holder = drThis.Owner.Battle.AllCreatures
                    // Always put the owner of the effect first to avoid identical unarmeds moving around.
                    // This works because `true` evaluates as higher than `false` and doesn't alter the rest.
                    .OrderByDescending(cr => cr == drThis.Owner)
                    .FirstOrDefault(cr =>
                        cr.AllItems
                            .Union(cr.Weapons)
                            .Union([cr.BaseArmor])
                            .WhereNotNull()
                            .Any(item =>
                                ItemIsSameAsSource(item, drawnOn)));
                
                // If the item moved to a new handler, move the DrawnRune to that creature.
                if (holder is not null
                    && holder != drThis.Owner)
                {
                    CommonRuneRules.MoveRuneToTarget(drThis, holder, drawnOn);
                }
            }
            
            // Disable the rune if it's not in play
            if (holder is not null)
                this.EnableRune(true);
            else
                this.DisableRune(false);
        };
        
        return this;

        bool ItemIsSameAsSource(Item potentialItem, Item drawnOn)
        {
            if (drawnOn.HasTrait(Trait.Unarmed))
            {
                if (potentialItem == drawnOn
                    || (drawnOn.ItemName is not ItemName.None
                        && potentialItem.ItemName == drawnOn.ItemName)
                    || potentialItem.Name == drawnOn.Name
                    || potentialItem.BaseHumanName == drawnOn.BaseHumanName
                    || potentialItem.ProsaicName == drawnOn.ProsaicName)
                    return true;
            }
            else if (potentialItem == drawnOn)
                return true;

            return false;
        }
    }
    
    public DrawnRune WithDrawnOnRune(DrawnRune rune, Func<DrawnRune,DrawnRune,string>? newDescriptionGenerator)
    {
        this.DrawnOn = rune;
        if (newDescriptionGenerator is not null)
            DiacriticDescriptionGenerator = newDescriptionGenerator;
        this.StateCheck += qfThis =>
        {
            DrawnRune drThis = (qfThis as DrawnRune)!;
            
            // Find who is "bearing" this rune
            Creature? bearer = null;
            if (drThis.DrawnOn is DrawnRune drawnOn)
            {
                // Regenerate the description in case the diacritic's been moved to a new rune.
                // (This shouldn't be null, but it's a solid fallback in case it is)
                drThis.Description = this.DiacriticDescriptionGenerator?.Invoke(drThis, drawnOn) ?? "[NO DESCRIPTION]";
                drThis.UpdateDescription();

                // Find the first creature who possesses this item.
                bearer = drThis.Owner.Battle.AllCreatures
                    // Always put the owner of the effect first to avoid identical unarmeds moving around.
                    // This works because `true` evaluates as higher than `false` and doesn't alter the rest.
                    .OrderByDescending(cr => cr == drThis.Owner)
                    .FirstOrDefault(cr => cr.QEffects.Contains(drawnOn));
                
                // If the rune moved to a new handler, move the diacritic DrawnRune to that creature.
                if (bearer is not null
                    && bearer != drThis.Owner)
                {
                    CommonRuneRules.MoveRuneToTarget(drThis, bearer, drawnOn);
                }
            }
            // Unlike other regulators, if this is null, it needs to be deleted immediately.
            else
            {
                drThis.ExpiresAt = ExpirationCondition.Immediately;
                return;
            }
            
            // Disable the rune if it's not in play
            if (bearer is not null)
                this.EnableRune(true);
            else
                this.DisableRune(false);
        };
        
        return this;
    }

    /// <summary>
    /// Creates a template QEffect to be used any time a persistent effect from a rune's invocation would be left behind.
    /// </summary>
    /// <param name="description">The QEffect's description.</param>
    /// <param name="expiresAt">When the QEffect expires.</param>
    /// <param name="adjustQf">Adjustments to make to the effect.</param>
    public QEffect NewInvocationEffect(
        string description,
        ExpirationCondition expiresAt,
        Action<QEffect>? adjustQf)
    {
        QEffect invokedEffect = new QEffect()
        {
            Name = $"Invoked {this.Rune.FullName}",
            Description = description,
            Illustration = new SuperimposedIllustration(
                this.Illustration ?? IllustrationName.None,
                ModData.Illustrations.CheckSymbol),
            Traits = [ModData.Traits.Invocation],
            Source = this.Source,
            ExpiresAt = expiresAt,
        };
        adjustQf?.Invoke(invokedEffect);
        return invokedEffect;
    }

    #endregion
}