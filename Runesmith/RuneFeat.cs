using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class RuneFeat : Feat
{
    public Rune Rune { get; }

    public RuneFeat(
        FeatName featName,
        Rune rune)
        : base(
            featName,
            rune.FlavorText,
            rune.GetFullyFormattedDescription(false),
            new List<Trait>(rune.Traits),
            null)
    {
        this.Rune = rune;
        this.Traits.RemoveFirst(trait => trait == ModTraits.Runesmith); // Rune feat needs to not show up as a class feat option.
        this.WithPrerequisite(
            values => values.AllFeats.FirstOrDefault(feat => feat is RunicRepertoireFeat) != null,
            "You must have a runic repertoire.");
        this.WithOnSheet(values =>
        {
            RunicRepertoireFeat? foundRepertoire =
                values.AllFeats.FirstOrDefault(feat => feat is RunicRepertoireFeat) as RunicRepertoireFeat;
            foundRepertoire?.AddRune(this.Rune);
        });
        if (rune.Illustration != null)
            this.WithIllustration(rune.Illustration);
    }
}

