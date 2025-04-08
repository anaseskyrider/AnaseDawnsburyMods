using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunicRepertoireFeat : Feat
{
    public List<Rune> RunesKnown { get; set; } = [];

    public int EtchLimit { get; set; }
    
    /// <summary>
    /// A trait representing the source of the runic repertoire, such as a class trait, or a trait unique to an archetype. No enforcement mechanisms exist to stop a character from having multiple runic repertoires from the same source, so use with prudence. This mod uses <see cref="ModTraits.Runesmith"/> as its source.
    /// </summary>
    public Trait Source { get; set; }

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
        if (!this.Traits.Contains(ModTraits.RunicRepertoire))
            this.Traits.Add(ModTraits.RunicRepertoire);
        this.Source = source;
        this.EtchLimit = 2;
    }

    /// <summary>
    /// Adds a rune to this repertoire. 
    /// </summary>
    /// <param name="rune">The <see cref="Rune"/> to add.</param>
    /// <returns>(bool) True if the rune was added, or false if it already existed.</returns>
    public bool AddRune(Rune rune)
    {
        if (this.RunesKnown.Contains(rune))
            return false;
        this.RunesKnown.Add(rune);
        return true;

    }

    /// <summary>
    /// Checks whether a given rune exists in the repertoire.
    /// </summary>
    /// <param name="rune">The <see cref="Rune"/> to look for.</param>
    /// <returns>(bool) True if the rune was found, or false if not.</returns>
    public bool HasRune(Rune rune)
    {
        Rune? foundRune = this.RunesKnown.FirstOrDefault(runeInList => runeInList.Equals(rune));
        return foundRune != null;
    }

    public static RunicRepertoireFeat? GetRepertoireOnCreature(Creature cr)
    {
        RunicRepertoireFeat? repertoire = cr.PersistentCharacterSheet?.Calculated.AllFeats.FirstOrDefault(ft => ft is RunicRepertoireFeat ) as RunicRepertoireFeat;
        return repertoire;
    }
    
    public static RunicRepertoireFeat? GetRepertoireOnSheet(CharacterSheet sheet)
    {
        RunicRepertoireFeat? repertoire = sheet.Calculated.AllFeats.FirstOrDefault(ft => ft is RunicRepertoireFeat ) as RunicRepertoireFeat;
        return repertoire;
    }
}