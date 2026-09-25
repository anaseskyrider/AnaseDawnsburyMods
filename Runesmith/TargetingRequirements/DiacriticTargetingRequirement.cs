using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class DiacriticTargetingRequirement : DrawnRuneTargetingRequirement
{
    public override Usability Satisfied(Creature source, DrawnRune dr)
    {
        if (dr.Rune.IsDiacriticRune)
            return Usability.NotUsableOnThisCreature("Can't draw on a diacritic");
        if (dr.AttachedDiacritic is not null)
            return Usability.NotUsableOnThisCreature("Already has diacritic");
        return Usability.Usable;
    }
}