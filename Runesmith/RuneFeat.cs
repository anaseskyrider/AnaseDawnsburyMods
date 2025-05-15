using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RuneFeat : Feat
{
    public Rune Rune { get; }
    
    public int LevelLearnedAt { get; set; }

    public RuneFeat(
        FeatName featName,
        Rune rune)
        : base(
            featName,
            rune.FlavorText,
            rune.GetFormattedFeatDescription(false),
            new List<Trait>(rune.Traits),
            null)
    {
        this.Rune = rune;
        this.Traits.RemoveFirst(trait => trait == ModData.Traits.Runesmith); // Rune feat needs to not show up as a class feat option.
        this.WithPrerequisite(
            values => values.AllFeats.FirstOrDefault(feat => feat is RunicRepertoireFeat) != null,
            "You must have a runic repertoire.");
        this.WithOnSheet(values =>
        {
            // OBSOLETE
            /*RunicRepertoireFeat? foundRepertoire =
                values.AllFeats.FirstOrDefault(feat => feat is RunicRepertoireFeat) as RunicRepertoireFeat;
            foundRepertoire?.LearnRuneFeat(this, values.CurrentLevel);*/
            this.LevelLearnedAt = values.CurrentLevel;
        });
        this.WithIllustration(rune.Illustration);
    }
    
    /// <summary>
    /// Creates a RuneFeat representing the knowledge of a given <see cref="Rune"/> instance.
    /// </summary>
    /// <param name="technicalName">The technical name to be passed to the ModManager.</param>
    /// <param name="rune">The rune instance to turn into a feat.</param>
    /// <returns></returns>
    public static RuneFeat CreateRuneFeatFromRune(
        string technicalName,
        Rune rune)
    {
        RuneFeat runeFeat = new RuneFeat(
            ModManager.RegisterFeatName(technicalName, rune.Name),
            rune);
        return runeFeat;
    }
}

