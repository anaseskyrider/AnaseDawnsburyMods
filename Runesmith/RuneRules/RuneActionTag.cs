using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Treasure;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

/// <summary>
/// The <see cref="CombatAction.Tag"/> which contains useful information about runes and targeting subroutines while actions are being executed, processed, and read.
/// </summary>
/// <param name="rune">The inner Rune which is being drawn or invoked.</param>
/// <param name="createdRune">The DrawnRune instance created by this action, if any.</param>
/// <param name="chosenCreature">The Creature targeted by this rune-drawing CombatAction, if any.</param>
/// <param name="chosenRune">The DrawnRune targeted by this invocation or diacritic-drawing action, if any.</param>
/// <param name="chosenItem">The Item targeted by this invocation or </param>
public class RuneActionTag(
    Rune rune,
    DrawnRune? createdRune = null,
    Creature? chosenCreature = null,
    DrawnRune? chosenRune = null,
    Item? chosenItem = null)
{
    public required CombatAction Owner { get; set; }
    
    /// <summary>
    /// The inner Rune which is being drawn or invoked.
    /// </summary>
    public Rune Rune { get; } = rune;
    
    /// <summary>
    /// The DrawnRune created by this rune-drawing CombatAction.
    /// </summary>
    public DrawnRune? CreatedDrawnRune { get; set; } = createdRune;
    
    /// <summary>
    /// The Creature targeted by this rune-drawing CombatAction.
    /// </summary>
    public Creature? ChosenCreature { get; set; } = chosenCreature;
    
    /// <summary>
    /// The DrawnRune targeted by this invocation or diacritic rune-drawing CombatAction.
    /// </summary>
    public DrawnRune? ChosenDrawnRune { get; set; } = chosenRune;
    
    /// <summary>
    /// The Item targeted by this rune-drawing CombatAction.
    /// </summary>
    public Item? ChosenItem { get; set; } = chosenItem;
    
    // Tile. Tiles? Whatever. For when the level 17 tile targeting rune exists.
    
    public bool IsEmpty =>
        this.CreatedDrawnRune is null
        && this.ChosenCreature is null
        && this.ChosenDrawnRune is null
        && this.ChosenItem is null;
}