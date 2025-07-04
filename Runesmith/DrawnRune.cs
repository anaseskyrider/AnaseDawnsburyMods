using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;

namespace Dawnsbury.Mods.RunesmithPlaytest;

/// <summary>
/// <para>DrawnRunes are <see cref="QEffect"/>s which represent a runesmith's runes. They do so by having an additional field, Rune, which is a reference to a <see cref="Rune"/>.</para>
/// <para>When creating DrawnRunes, keep the following principles in mind:</para>
/// <list type="bullet">
/// <item>The RUNE-BEARER is the creature found at the <see cref="QEffect.Owner"/> field.</item>
/// <item>If the rune is meant to be drawn onto an ITEM or another DRAWNRUNE, the <see cref="DrawnOn"/> field stores that object reference.</item>
/// <item>The Runesmith, or caster, is the creature found at the <see cref="QEffect.Source"/> field.</item>
/// <item>The <see cref="Rune"/> field corresponds to a type of rune, such as Atryl. This acts like an ID, and a look-up for instance data from the Rune itself, such as its <see cref="Rune.Name"/> or its behavior like <see cref="Rune.InvocationBehavior"/>.</item>
/// <item>A DrawnRune corresponds to a single rune on the battlefield, like in the tabletop. Thus, only a single DrawnRune QEffect should be created for each rune, allowing that QEffect instance to be targeted by various abilities (including a Runesmith's Invoke Rune), and so should not be used for persistent effects created by a rune's invocation. TechnicalQFs which need to be applied to the entire battlefield should ensure that only one DrawnRune has all of its source Rune's traits as a result, especially the Rune trait specifically.</item>
/// <item>A DrawnRune is always initialized with the traits of its Rune, which means the [Rune, Runesmith, Magical] traits can always be found, in addition to the traits specific to that rune (such as Dwarf, for Holtrik).</item>
/// <item>QEffects which are not DrawnRunes might still have the Rune trait.</item>
/// </list>
/// </summary>
public class DrawnRune : QEffect
{
    #region Initializers
    public DrawnRune(
        Rune rune)
        : base()
    {
        this.Rune = rune;
        this.Traits = new List<Trait>(rune.Traits);
    }

    /// <summary>
    /// Uses ExpirationCondition.Ephemeral. Must be set later. Also uses the rune's illustration by default. See: <see cref="QEffect(string, string, ExpirationCondition, Creature?, Illustration?)"/>
    /// </summary>
    /// <param name="rune"></param>
    /// <param name="description"></param>
    /// <param name="source"></param>
    public DrawnRune(
        Rune rune,
        string description,
        Creature? source)
        : base(
            rune.Name,
            description,
            ExpirationCondition.Ephemeral,
            source,
            rune.Illustration)
    {
        this.Rune = rune;
        this.Traits = new List<Trait>(rune.Traits);
    }
    #endregion
    
    #region Properties
    /// <summary>
    /// The <see cref="Rune"/> represented by this QEffect.
    /// </summary>
    public Rune Rune { get; }

    public Trait? DrawTrait
    {
        get
        {
            bool isEtched = this.Traits.Contains(ModData.Traits.Etched);
            bool isTraced = this.Traits.Contains(ModData.Traits.Traced);
            if (isEtched && isTraced) // It's not supposed to be both, so return a null just in case.
                return null;
            else if (isEtched)
                return ModData.Traits.Etched;
            else if (isTraced)
                return ModData.Traits.Traced;
            else
                return null;
        }
        set => DrawTrait = value;
    }

    /// <summary>
    /// <para>Any object which could abstractly represent the "true" target of the QEffect, the thing that it is "actually" drawn on. The types are generally of: <see cref="Creature"/>, <see cref="DrawnRune"/> (used for diacritics), <see cref="Dawnsbury.Core.Mechanics.Treasure.Item"/> (used for weapons, unarmed strikes).</para>
    /// <para>Used by other parts of the mod to assist with behavior such as disabling a QEffect (without automated C# garbage collection causing problems) when no creature is holding the item that the rune is drawn onto.</para>
    /// </summary>
    public object? DrawnOn { get; set; }
    
    /// <summary>
    /// The diacritic rune attached to this DrawnRune, if any.
    /// </summary>
    public DrawnRune? AttachedDiacritic { get; set; }
    
    /// <summary>If true, the drawn rune doesn't execute its passive behavior, such as Atryl lowering fire resistance. To set true/false, use <see cref="EnableRune"/> and <see cref="DisableRune"/></summary>
    public bool Disabled => this.Hidden || this.DisablePassive;

    /// <summary>See <see cref="Disabled"/> for functionality.</summary>
    private bool DisablePassive { get; set; }
    #endregion

    #region Callbacks
    // I have no idea what I want these to do just yet.
    
    // public Func<????>? BeforeApplyingRune { get; set; }
    
    // public Func<????>? AfterApplyingRune { get; set; }

    /// <summary>
    /// Happens before any DrawnRune is invoked.
    /// </summary>
    /// <param name="DrawnRune">The DrawnRune this lambda is being called on.</param>
    /// <param name="CombatAction">The CombatAction invoking the rune.</param>
    /// <param name="DrawnRune">The DrawnRune that is about to be invoked.</param>
    public Func<DrawnRune, CombatAction, DrawnRune, Task>? BeforeInvokingRune { get; set; }
    
    /// <summary>
    /// Happens after any DrawnRune is invoked.
    /// </summary>
    /// <param name="DrawnRune">The DrawnRune this lambda is being called on.</param>
    /// <param name="CombatAction">The CombatAction that invoked the rune.</param>
    /// <param name="DrawnRune">The DrawnRune that was just invoked.</param>
    public Func<DrawnRune, CombatAction, DrawnRune, Task>? AfterInvokingRune { get; set; }
    #endregion
    
    #region Property Methods
    /// <summary>
    /// Applies the supplied Etched or Traced trait, and modifies the duration of the QEffect according to Etched and Traced behavior.
    /// </summary>
    /// <param name="drawTrait">The <see cref="ModData.Traits.Etched"/> or <see cref="ModData.Traits.Traced"/> trait.</param>
    /// <returns></returns>
    public DrawnRune WithDrawDuration(Trait drawTrait)
    {
        return drawTrait == ModData.Traits.Etched ? this.WithIsEtched() : (drawTrait == ModData.Traits.Traced ? this.WithIsTraced() : this);
    }

    public DrawnRune WithIsEtched()
    {
        this.Traits.Remove(ModData.Traits.Traced);
        this.Traits.Add(ModData.Traits.Etched);
        this.ExpiresAt = ExpirationCondition.Never;
        this.Description += "\n\n{i}{Blue}Etched: lasts until the end of combat.{/Blue}{/i}";
        return this;
    }

    public DrawnRune WithIsTraced()
    {
        this.Traits.Remove(ModData.Traits.Etched);
        this.Traits.Add(ModData.Traits.Traced);
        this.ExpiresAt = ExpirationCondition.ExpiresAtEndOfSourcesTurn;
        this.CannotExpireThisTurn = true;
        this.Description += "\n\n{i}{Blue}Traced: lasts until the end of " + this.Source?.Name + "'s next turn.{/Blue}{/i}";
        return this;
    }

    /// <summary>
    /// <para>Adds a state check to the DrawnRune which checks if the holder of the ITEM changes to a different creature. This moves the DrawnRune from the old QEffect Owner to the new one, but ONLY IF a new item-holder is found.</para>
    /// <para>The DrawnRune's <see cref="QEffect.Tag"/> field given a bool, which equals true if any creature is holding the item, or false if no creature is wielding it. Any behavior from a DrawnRune that should be disabled while the item isn't being held should check against this Tag. (You can still check the owner's held items.)</para>
    /// <para>(This is necessary because if the QEffect ceases to have an owner, or be stored elsewhere, it is garbage-collected by C#.)</para>
    /// </summary>
    /// <param name="item">The <see cref="Item"/> that the DrawnRune follows.</param>
    /// <returns>(DrawnRune)</returns>
    public DrawnRune WithItemRegulator(Item item)
    {
        this.DrawnOn = item;
        this.Tag = false;
        this.StateCheck += /*async*/ qfSelf =>
        {
            if (this.DrawnOn is not Item itemTarget) // Don't bother, if the item disappears or is a different target type
                return;
            
            this.Tag = false; // Reset, check if held
            
            // Loop through every creature in battle to find out who holds the item, moving the QEffect when someone else picks it up.
            foreach (Creature allCreature in qfSelf.Owner.Battle.AllCreatures)
            {
                if (allCreature.HeldItems.Contains(itemTarget))
                {
                    this.Tag = true; // is HeldByAnyone
                    this.Name = $"{this.Rune.Name} ({itemTarget.Name})"; // Recalculate its name in case the target has changed -- e.g. Transpose Rune from Tok'dar's shield to his battleaxe.
                    
                    if (allCreature == this.Owner) // Don't move if they already have the QF.
                        continue; 
                    
                    /*await*/ CommonRuneRules.MoveRuneToTarget(this, allCreature, this.DrawnOn); // Move to the new creature without changing the item target.
                    break;
                }
            }
            
            // Automate disabling and hiding the rune if the item isn't found on anyone
            if (this.Tag is not bool tag)
                return;
            if (tag == true)
                this.EnableRune(true);
            else
                this.DisableRune(false);
        };
        return this;
    }
    
    /// <summary>
    /// Behaves identically to <see cref="WithItemRegulator"/> but with extra functionality to allow for the possibility of being <see cref="DrawnOn"/> to a creature's Unarmed Strikes.
    /// </summary>
    public DrawnRune WithItemOrUnarmedRegulator(Item item)
    {
        this.DrawnOn = item;
        this.Tag = false; // (bool) HeldByAnyone. Check against this tag if the code segments need to not apply to its owner (the QF only moves itself when a new holder is found).
        this.StateCheck += /*async*/ qfSelf =>
        {
            if (this.DrawnOn is not Item itemTarget) 
                return;

            this.Tag = false; // Reset, check if held
            
            if (itemTarget.HasTrait(Trait.Unarmed))
            {
                this.Tag = true; // is HeldByAnyone
                this.Name = $"{this.Rune.Name} (unarmed strikes)"; // Recalculate its name in case the target has changed -- e.g. Transpose Rune from steel shield to any bludgeoning unarmed item
                /* Don't try to move the rune. It has to be moved by an external effect. */
            }
            else
            {
                // Loop through every creature in battle to find out who holds the item, moving the QEffect when someone else picks it up.
                foreach (Creature allCreature in qfSelf.Owner.Battle.AllCreatures)
                {
                    if (allCreature.HeldItems.Contains(itemTarget))
                    {
                        this.Tag = true; // is HeldByAnyone
                        this.Name = $"{this.Rune.Name} ({itemTarget.Name})"; // Recalculate its name in case the target has changed -- e.g. Transpose Rune from Tok'dar's shield to his battleaxe, or from one creature's unarmed strike to another.
                    
                        if (allCreature == this.Owner) // Don't move if they already have the QF.
                            continue;

                        /*await*/ CommonRuneRules.MoveRuneToTarget(this, allCreature, this.DrawnOn); // Move to the new creature without changing the item target.
                        break;
                    }
                }
            }
            
            // Automate disabling and hiding the rune if the item isn't found on anyone
            if (this.Tag is not bool tag)
                return;
            if (tag == true)
                this.EnableRune(true);
            else
                this.DisableRune(false);
        };
        return this;
    }

    public DrawnRune WithDiacriticRegulator(DrawnRune rune)
    {
        this.DrawnOn = rune;
        this.Tag = false;
        this.StateCheck += /*async*/ qfSelf =>
        {
            if (this.DrawnOn is not DrawnRune runeTarget)
            {
                this.ExpiresAt = ExpirationCondition.Immediately; // Unlike other regulators, if this is null, it needs to be deleted immediately.
                return;
            }

            this.Tag = false; // Reset, check if exists
            
            // Loop through every creature in battle to find out who has the base rune, moving the QEffect when someone else has it.
            foreach (Creature allCreature in qfSelf.Owner.Battle.AllCreatures)
            {
                if (allCreature.QEffects.Contains(runeTarget))
                {
                    this.Tag = true;

                    if (allCreature == this.Owner) // Don't move if they already have the QF.
                        continue;
                    
                    /*await*/ CommonRuneRules.MoveRuneToTarget(this, allCreature, runeTarget);
                    break;
                }
            }
            
            // Automate disabling and hiding the rune if the rune isn't found on anyone
            if (this.Tag is not bool tag)
                return;
            if (tag == true)
            {
                this.EnableRune(true);
                runeTarget.AttachedDiacritic = this;
            }
            else
                this.DisableRune(false);
        };
        this.AfterInvokingRune += async (drThis, sourceAction, drInvoked) =>
        {
            if (drInvoked == drThis.DrawnOn)
            {
                drThis.DrawnOn = null;
                drThis.ExpiresAt = ExpirationCondition.Immediately;
                drInvoked.AttachedDiacritic = null;
            }
        };
        
        return this;
    }
    #endregion

    #region Instance Methods
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
    /// Creates a template QEffect to be used any time a persistent effect from a rune's invocation would be left behind.
    /// </summary>
    /// <param name="description">The QEffect's description.</param>
    /// <param name="expiresAt">When the QEffect expires.</param>
    /// <returns></returns>
    public QEffect NewInvocationEffect(
        string description,
        ExpirationCondition expiresAt)
    {
        QEffect invokedEffect = new QEffect()
        {
            Name = $"Invoked {this.Rune.Name}",
            Description = description,
            Illustration = new SuperimposedIllustration(this.Illustration ?? IllustrationName.None, ModData.Illustrations.CheckSymbol),
            Traits = [ModData.Traits.Invocation],
            Source = this.Source,
            ExpiresAt = expiresAt,
        };
        return invokedEffect;
    }
    #endregion
    
    #region Magic Tradition Methods
    /// <summary>
    /// Provides a list of traits corresponding to the four traditions.
    /// </summary>
    public List<Trait> GetTraditionTraits()
    {
        List<Trait> traditionTraits = [];
        foreach (Trait trait in (Trait[])[Trait.Arcana, Trait.Divine, Trait.Primal, Trait.Occult])
        {
            if (this.Traits.Contains(trait))
                traditionTraits.Add(trait);
        }
        return traditionTraits;
    }

    /// <summary>
    /// Checks whether the DrawnRune has any tradition traits.
    /// </summary>
    public bool HasTraditionTraits()
    {
        return this.Traits.Any(trait => trait is Trait.Arcane or Trait.Divine or Trait.Primal or Trait.Occult);
    }
    
    /// <summary>
    /// Checks whether the DrawnRune is Arcane, or could be.
    /// </summary>
    public bool IsArcaneRune()
    {
        return this.Traits.Any(trait => trait is Trait.Arcane) 
               || (this.Source != null && this.Source.Skills.IsTrained(Skill.Arcana) 
                   && this.GetTraditionTraits().Count == 0);
    }
    
    /// <summary>
    /// Checks whether the DrawnRune is Divine, or could be.
    /// </summary>
    public bool IsDivineRune()
    {
        return this.Traits.Any(trait => trait is Trait.Divine) 
               || (this.Source != null && this.Source.Skills.IsTrained(Skill.Religion) 
                   && this.GetTraditionTraits().Count == 0);
    }

    /// <summary>
    /// Checks whether the DrawnRune is Primal, or could be.
    /// </summary>
    public bool IsPrimalRune()
    {
        return this.Traits.Any(trait => trait is Trait.Primal) 
               || (this.Source != null && this.Source.Skills.IsTrained(Skill.Nature) 
                   && this.GetTraditionTraits().Count == 0);
    }
    
    /// <summary>
    /// Checks whether the DrawnRune is Occult, or could be.
    /// </summary>
    public bool IsOccultRune()
    {
        return this.Traits.Any(trait => trait is Trait.Occult) 
               || (this.Source != null && this.Source.Skills.IsTrained(Skill.Occultism) 
                   && this.GetTraditionTraits().Count == 0);
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Gets the list of DrawnRunes on the given potential rune-bearer.
    /// </summary>
    /// <param name="caster">(nullable) The creature who drew the runes. If null, provides all DrawnRunes.</param>
    /// <param name="runeBearer">The creature with runes drawn on them.</param>
    /// <returns></returns>
    public static List<DrawnRune> GetDrawnRunes(Creature? caster, Creature runeBearer)
    {
        List<DrawnRune> drawnRunes = runeBearer.QEffects
            .Where(qf =>
                qf is DrawnRune dr && 
                ((caster != null && dr.Source == caster) || caster == null) &&
                dr.Traits.Contains(ModData.Traits.Rune) &&
                !dr.Traits.Contains(ModData.Traits.Invocation))
            .Cast<DrawnRune>()
            .ToList();
        return drawnRunes;
    }
    #endregion
}