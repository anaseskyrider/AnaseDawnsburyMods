using System.ComponentModel.DataAnnotations;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Controls.Statblocks;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

public class RunicRepertoireTag
{
    #region Static Data

    /// <summary>
    /// Character sheet tag key which contains a runic repertoire.
    /// </summary>
    public const string RUNIC_REPERTOIRE = "RUNIC_REPERTOIRE";

    #endregion

    #region Static Functions

    /// <summary>
    /// Adds a runic repertoire to the character sheet, or gets an existing one.
    /// </summary>
    /// <param name="values">The calculated sheet at the current level being processed.</param>
    /// <param name="classOfOrigin">The class trait learning this repertoire.</param>
    /// <param name="etchLimit">The initial etch limit, or null if you can't etch runes. This uses <see cref="CalculatedCharacterSheetValues.CurrentLevel"/>.</param>
    /// <returns></returns>
    public static RunicRepertoireTag GetOrCreateRepertoire(
        CalculatedCharacterSheetValues values,
        Trait classOfOrigin,
        int? etchLimit = null)
    {
        RunicRepertoireTag? repertoire = GetRepertoire(values);
        if (repertoire is null)
        {
            repertoire = new RunicRepertoireTag()
            {
                ClassOfOrigin = classOfOrigin
            };
            if (etchLimit.HasValue)
                repertoire.IncreaseEtchLimit(values.CurrentLevel, etchLimit.Value);
            values.Tags.TryAdd(RUNIC_REPERTOIRE, repertoire);
        }
        return repertoire;
    }

    /// <summary>
    /// Get the runic repertoire stored on this creature's character sheet.
    /// </summary>
    public static RunicRepertoireTag? GetRepertoire(Creature runesmith)
    {
        return runesmith.PersistentCharacterSheet is null
            ? null
            : GetRepertoire(runesmith.PersistentCharacterSheet.Calculated);
    }

    /// <summary>
    /// Get the runic repertoire stored on this character sheet.
    /// </summary>
    public static RunicRepertoireTag? GetRepertoire(CalculatedCharacterSheetValues values)
    {
        return values.Tags.TryGetValueAs(RUNIC_REPERTOIRE, out RunicRepertoireTag? repertoire)
            ? repertoire
            : null;
    }

    public static void AddRuneSelectionOption(
        CalculatedCharacterSheetValues values,
        string key,
        string name,
        int runeLevel,
        int numberOfRunes)
    {
        // FeatlikeChoice
        /*for (int i=0; i<numberOfRunes; i++)
            values.AddSelectionOption(new LimitedTextSelectionOption(
                $"{key}{i}",
                name,
                level,
                RunesmithRunes.GetRuneChoices(level)));*/

        Func<Feat, bool> eligible = ft =>
            AllRunes.AllRuneFeats.Contains(ft)
            && ft.Tag is Rune rune
            && rune.BaseLevel <= runeLevel;
        
        SelectionOption opt = numberOfRunes > 1
            ? new MultipleFeatSelectionOption(
                key, name, values.CurrentLevel,
                eligible,
                numberOfRunes)
            : new SingleFeatSelectionOption(
                key, name, values.CurrentLevel,
                eligible);
        
        values.AddSelectionOption(opt.WithIsOptional());
    }

    #endregion

    #region Instance Data

    /// <summary>
    /// Functions similar to <see cref="SpellcastingSource.SpellcastingTradition"/>. This mod uses <see cref="ModData.Traits.Runesmith"/> as its source.
    /// </summary>
    public Trait ClassOfOrigin { get; private set; } = ModData.Traits.Runesmith;

    /// <summary>
    /// List of runes known by level.
    /// </summary>
    public readonly Dictionary<int, List<Rune>> RunesKnown = [];

    /// <summary>
    /// The organized list of etch limit increases at each level.
    /// </summary>
    /// <remarks>
    /// An etch limit is the maximum number of runes you can have etched before each encounter.
    /// </remarks>
    private readonly Dictionary<int, List<int>> _etchLimitIncreases = [];

    #endregion

    #region Instance Functions

    /// <summary>
    /// Adds a rune to your repertoire at the given class level.
    /// </summary>
    /// <param name="level">The character level that the rune was learned.</param>
    /// <param name="rune">The rune to learn.</param>
    public void AddRune(int level, Rune rune)
    {
        if (IsKnown(rune, level))
            return;
        RunesKnown.TryAdd(level, []);
        RunesKnown[level].Add(rune);
    }

    /// <summary>
    /// Whether the given Rune is known in this repertoire.
    /// </summary>
    public bool IsKnown(Rune rune, int upToLevel)
    {
        return this.RunesKnown
            .Where(learnedAt => learnedAt.Key <= upToLevel)
            .Any(learnedAt =>
                learnedAt.Value.Contains(rune));
    }

    /// <summary>
    /// Whether the given RuneId is known in this repertoire.
    /// </summary>
    public bool IsKnown(RuneId id, int upToLevel)
    {
        return this.RunesKnown
            .Where(learnedAt => learnedAt.Key <= upToLevel)
            .Any(learnedAt =>
                learnedAt.Value.Any(rune => rune.Id == id));
    }

    /// <summary>
    /// Gets the list of runes which can be traced in combat.
    /// </summary>
    /// <param name="runesmith"></param>
    /// <returns></returns>
    public List<Rune> GetTraceableRunes(Creature runesmith)
    {
        return GetKnownRunes(runesmith, true)
            /*.Where(rune =>
                !rune.DrawProperties.IsEtchedOnly)*/
            .ToList();
    }

    /// <summary>
    /// Gets all known runes on the creature.
    /// </summary>
    /// <param name="runesmith">The runesmith with this repertoire. Runes beyond this creature's level are excluded.</param>
    /// <param name="includeTemporaryRunes">Include runes acquired temporarily during combat.</param>
    public List<Rune> GetKnownRunes(Creature runesmith, bool includeTemporaryRunes = true)
    {
        // Get permanently known runes
        List<Rune> runes = this.RunesKnown
            .Where(learnedAt => learnedAt.Key <= runesmith.Level)
            .SelectMany(learnedAt => learnedAt.Value)
            .ToList();
        
        // TODO: Temporary encounter runes on the smith, probably with like a QEffectId.

        return runes;
    }

    /// <summary>
    /// Gets all known runes on the sheet up to <see cref="CalculatedCharacterSheetValues.CurrentLevel"/>.
    /// </summary>
    public List<Rune> GetKnownRunes(CalculatedCharacterSheetValues values)
    {
        return this.RunesKnown
            .Where(learnedAt => learnedAt.Key <= values.CurrentLevel)
            .SelectMany(learnedAt => learnedAt.Value)
            .ToList();
    }

    /// <summary>
    /// Add an increase to your etch limit.
    /// </summary>
    /// <param name="level">The level to apply the etch limit.</param>
    /// <param name="amount">Increase the limit by how much.</param>
    public void IncreaseEtchLimit(
        int level,
        [Range(1,99)]
        int amount = 1)
    {
        this._etchLimitIncreases.TryAdd(level, []);
        this._etchLimitIncreases[level].Add(amount);
    }

    /// <summary>
    /// Get the number of maximum allowed etched runes before each encounter.
    /// </summary>
    /// <param name="runesmith">The runesmith with this repertoire. The limit is based on this creature's level.</param>
    public int GetEtchLimit(Creature runesmith)
    {
        return this._etchLimitIncreases
            .Where(increaseAt => increaseAt.Key <= runesmith.Level)
            .Sum(increaseAt => increaseAt.Value.Sum());
    }

    /// <summary>
    /// Get the formatted description of a runic repertoire entry for a <see cref="CreatureStatblockSectionGenerator"/>.
    /// </summary>
    public static string DescribeRunicRepertoire(Creature runesmith)
    {
        RunicRepertoireTag? repertoire = RunicRepertoireTag.GetRepertoire(runesmith);
        if (repertoire == null)
            return "";
        List<string> traditions = [];
        if (runesmith.Skills.IsTrained(Skill.Arcana))
            traditions.Add("arcane");
        if (runesmith.Skills.IsTrained(Skill.Religion))
            traditions.Add("divine");
        if (runesmith.Skills.IsTrained(Skill.Occultism))
            traditions.Add("occult");
        if (runesmith.Skills.IsTrained(Skill.Nature))
            traditions.Add("primal");
        int DC = runesmith.ClassDC(ModData.Traits.Runesmith);
        int etchLim = repertoire.GetEtchLimit(runesmith);
        string? tattoo = runesmith.PersistentCharacterSheet?.Calculated
            .GetTag<Rune>(ClassFeats.RUNIC_TATTOO_KEY)?
            .WordName.ToLower() ?? null;
        string runesKnown = string.Join("; ",
            repertoire.GetKnownRunes(runesmith)
                .GroupBy(rune => rune.BaseLevel)
                .OrderByDescending(rg => rg.Key)
                .Select(rg =>
                {
                    string rank = "{b}" + rg.Key.Ordinalize2() + "{/b}";
                    string runes = string.Join(", ",
                        rg.GroupBy(rn => rn.FullName)
                            .OrderBy(lg => lg.Key)
                            .Select(runes =>
                            {
                                Rune first = runes.First();
                                string word = first.Id.ToWord().ToLower();
                                if (first.IsDiacriticRune)
                                    word += "-";
                                return word.WithTag(ModData.PersistentActions
                                        .RuneIsUsedUp(runesmith, first.Id)
                                        ? "strike"
                                        : null);
                            }));
                    return rank + " {i}" + runes + "{/i}";
                })
        );
        return $"{ModData.Tooltips.RuleRuneTradition("Traditions")} {string.Join(", ", traditions)}\n{{b}}DC{{/b}} {DC}"
            + (etchLim > 0 ? $"; {{b}}etch limit{{/b}} {etchLim}" : null)
            // Link almost never works, so it's not worth having
            //+ (etchLim > 0 ? $"; {ModData.FeatNames.EtchRune.ToLink("etch limit").WithTag("b")} {etchLim}" : null)
            + (tattoo != null ? $"\n{{b}}Tattoo{{/b}} {{i}}{tattoo.WithTag(runesmith.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.RUNIC_TATTOO) ? "strike" : null)}{{/i}}" : null)
            + $"\n{runesKnown}";
    }

    #endregion
}