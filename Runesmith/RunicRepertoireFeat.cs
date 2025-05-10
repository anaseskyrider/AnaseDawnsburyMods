using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunicRepertoireFeat : Feat
{
    #region Initializer
    public RunicRepertoireFeat(
        FeatName featName,
        string? flavorText,
        string rulesText,
        List<Trait>? traits,
        Trait source)
        : base(
            featName,
            flavorText,
            rulesText,
            traits ?? [],
            null)
    {
        if (!this.Traits.Contains(Enums.Traits.RunicRepertoire))
            this.Traits.Add(Enums.Traits.RunicRepertoire);
        this.Source = source;
        this.EtchLimit = 2;
    }
    #endregion
    
    #region Properties
    /// <summary>
    /// The number of runes which can be Etched at once.
    /// </summary>
    public int EtchLimit { get; set; }
    
    /// <summary>
    /// A trait representing the source of the runic repertoire, such as a class trait, or a trait unique to an archetype. No enforcement mechanisms exist to stop a character from having multiple runic repertoires from the same source, so use with prudence. This mod uses <see cref="Enums.Traits.Runesmith"/> as its source.
    /// </summary>
    public Trait Source { get; set; }
    #endregion

    #region Instance Methods    
    /// <summary>
    /// Gets the list of runes known up to the given creature's current level. To get runes at higher levels, use <see cref="CharacterSheet.ToCreature"/>.
    /// </summary>
    /// <returns></returns>
    public List<Rune> GetRunesKnown(Creature cr)
    {
        if (cr.PersistentCharacterSheet != null)
            return this.GetRunesKnown(cr.PersistentCharacterSheet.Calculated);
        else
            return new List<Rune>();
    }
    
    /// <summary>
    /// Gets the list of runes known on the character sheet. To get runes at higher levels, use <see cref="CharacterSheet.ToCreature"/>.
    /// </summary>
    public List<Rune> GetRunesKnown(CalculatedCharacterSheetValues values)
    {
        List<Rune> runesKnown = new List<Rune>();
        foreach (Feat feat in values.AllFeats)
        {
            if (feat is RuneFeat runeFeat)
                runesKnown.Add(runeFeat.Rune);
        }
        return runesKnown;
    }

    /// <summary>
    /// Checks whether a CREATURE knows a given RUNE. To check for runes at higher levels, use <see cref="CharacterSheet.ToCreature"/>.
    /// </summary>
    /// <returns>(bool) True if the rune was found, or false if not.</returns>
    public bool KnowsRune(Creature cr, Rune rune)
    {
        return this.KnowsRune(cr.PersistentCharacterSheet.Calculated, rune);
    }
    
    /// <summary>
    /// Checks whether a CHARACTER SHEET knows a given RUNE. To check for runes at higher levels, use <see cref="CharacterSheet.ToCreature"/>.
    /// </summary>
    /// <returns>(bool) True if the rune was found, or false if not.</returns>
    public bool KnowsRune(CalculatedCharacterSheetValues values, Rune rune)
    {
        List<Rune> runesKnown = this.GetRunesKnown(values);
        return runesKnown.Contains(rune);
    }

    /// <summary>
    /// Acts as <see cref="KnowsRune(Creature, Rune)"/> for a list of runes. To check for runes at higher levels, use <see cref="CharacterSheet.ToCreature"/>.
    /// </summary>
    /// <returns>(bool) True if all the runes were found, or false if not.</returns>
    public bool KnowsRunes(Creature cr, List<Rune> runes)
    {
        return this.KnowsRunes(cr.PersistentCharacterSheet.Calculated, runes);
    }

    /// <summary>
    /// Acts as <see cref="KnowsRune(CalculatedCharacterSheetValues, Rune"/> for a list of runes. To check for runes at higher levels, use <see cref="CharacterSheet.ToCreature"/>.
    /// </summary>
    /// <returns>(bool) True if all the runes were found, or false if not.</returns>
    public bool KnowsRunes(CalculatedCharacterSheetValues values, List<Rune> runes)
    {
        List<Rune> runesKnown = this.GetRunesKnown(values);
        foreach (Rune rune in runes)
        {
            if (!runesKnown.Contains(rune))
                return false;
        }
        
        return true;
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Gets the RunicRepertoireFeat known by the CREATURE.
    /// </summary>
    public static RunicRepertoireFeat? GetRepertoireOnCreature(Creature cr)
    {
        RunicRepertoireFeat? repertoire = cr.PersistentCharacterSheet?.Calculated.AllFeats.FirstOrDefault(ft => ft is RunicRepertoireFeat ) as RunicRepertoireFeat;
        return repertoire;
    }
    
    /// <summary>
    /// Gets the RunicRepertoireFeat known by the CHARACTER SHEET.
    /// </summary>
    public static RunicRepertoireFeat? GetRepertoireOnSheet(CalculatedCharacterSheetValues values)
    {
        RunicRepertoireFeat? repertoire = values.AllFeats.FirstOrDefault(ft => ft is RunicRepertoireFeat ) as RunicRepertoireFeat;
        return repertoire;
    }
    #endregion

    #region Obsolete Runes Known Code
    
    // Dev Commentary: No longer used but kept because debugging hell. It may be possible to call CalculatedCharacterSheetValues.AtEndOfRecalculation to provide a method for recalculating runes known, thus eliminating problems related to adding runes without ever removing them. However, that is a very big brain trial and error thing, and I finally got Runic Tattoo to work while iterating through the present feats of the sheet at any level I desire, so *insert inappropriate bodily noises here*.
    
    /*
    private Dictionary<int, List<Rune>> RunesKnown { get; set; } = [];
    
        public List<Rune> GetRunesKnown(CalculatedCharacterSheetValues values, int? maxLevel = null)
    {
        List<Rune> runesKnown = new List<Rune>();
        
        // Removed. To get different levels, 
        if (maxLevel == null) // Look inside feats acquired at the level the check is being performed, which might be lower than the current level.
        {
            foreach (Feat feat in values.AllFeats)
            {
                if (feat is RuneFeat runeFeat)
                    runesKnown.Add(runeFeat.Rune);
            }
        }
        else // Look through the stored list of runes acquired, which might include runes not necessarily known at this time.
        {
            foreach (var runeAtLevels in this.RunesKnown)
            {
                if (runeAtLevels.Key > maxLevel)
                    continue;
                runesKnown.AddRange(runeAtLevels.Value);
            }
        }
        foreach (Feat feat in values.AllFeats)
        {
            if (feat is RuneFeat runeFeat)
                if (maxLevel == null || runeFeat.LevelLearnedAt <= maxLevel)
                    runesKnown.Add(runeFeat.Rune);
        }
        return runesKnown;
    }
    
    public bool LearnRuneFeat(RuneFeat runeFeat, int levelLearned) // OBSOLETE
    {
        // Check if this feat is learned at any level:
        foreach (var runesAtThisLevel in this.RunesKnown)
        {
            if (runesAtThisLevel.Value.Contains(runeFeat.Rune))
                return false; // - Rune is already learned.
        }
        // - Rune wasn't already learned.
        // Add it to the repertoire
        if (!this.RunesKnown.TryGetValue(levelLearned, out List<Rune>? runesAtLearnLevel))
            this.RunesKnown.Add(levelLearned, []);
        this.RunesKnown[levelLearned].Add(runeFeat.Rune);
        // And tell it what level it was learned at.
        runeFeat.LevelLearnedAt = levelLearned;
        return true;
    }
    */
    
    #endregion
}