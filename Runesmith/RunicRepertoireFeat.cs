using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RunicRepertoireFeat : Feat
{
    #region Properties
    /*/// <summary>
    /// The creature who possesses this repertoire.
    /// </summary>
    public Creature Self { get; set; }*/
    
    /// <summary>
    /// A dictionary containing the level at which your Etch Limit was increased, and by how much.
    /// </summary>
    private Dictionary<int, int> EtchLimitIncreases { get; set; } = new();
    
    /// <summary>
    /// Functions similar to <see cref="SpellcastingSource.SpellcastingTradition"/>. This mod uses <see cref="Enums.Traits.Runesmith"/> as its source.
    /// </summary>
    public Trait ClassOfOrigin { get; set; }
    
    /*/// <summary>
    /// The <see cref="Ability"/> that this repertoire uses for its DCs.
    /// </summary>
    public Ability RunicAbility { get; set; }*/
    #endregion
    
    #region Initializer
    public RunicRepertoireFeat(
        FeatName featName,
        // Ability runicAbility,
        Trait classOfOrigin,
        int? etchLimit = null)
        : base(
            featName,
            null,
            "",
            [Enums.Traits.RunicRepertoire],
            null)
    {
        // this.RunicAbility = runicAbility;
        this.ClassOfOrigin = classOfOrigin;
        if (etchLimit != null)
            this.EtchLimitIncreases.Add(1, (int)etchLimit);
        
        // Description-maker
        this.WithPermanentQEffect("", qfFeat =>
        {
            qfFeat.Description = DescribeRepertoire(qfFeat.Owner);
            
            /*qfFeat.StartOfCombatBeforeOpeningCutscene = async qfThis =>
            {
                qfThis.Description = DescribeRepertoire(qfThis.Owner);
            };*/
            
            qfFeat.StateCheck += async qfThis =>
            {
                qfThis.Description = DescribeRepertoire(qfThis.Owner);
            };
        });
    }
    #endregion
    
    #region Get Runes
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
    
    #region Instance Methods
    
    /// <summary>
    /// Produces a detailed description of your runic repertoire, similar to spellcasting on a stat block.
    /// </summary>
    public string DescribeRepertoire(Creature owner)
    {
        int DC = RunesmithPlaytest.RunesmithDC(owner);
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
    }

    /// <summary>
    /// Add an increase at what level and by how much to your Etch Limit.
    /// </summary>
    public void IncreaseEtchLimit(int level, int amount)
    {
        if (amount < 1)
            return;
        
        if (!EtchLimitIncreases.TryAdd(level, amount))
            EtchLimitIncreases[level] = Math.Max(amount, EtchLimitIncreases[level]);
    }

    /// <summary>
    /// Gets your etch limit based on the given level.
    /// </summary>
    public int GetEtchLimit(int level)
    {
        int etchLimit = EtchLimitIncreases
            .Where(increase => increase.Key <= level)
            .Sum(increase => increase.Value);
        return etchLimit;
    }
    #endregion

    #region Get Repertoire
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