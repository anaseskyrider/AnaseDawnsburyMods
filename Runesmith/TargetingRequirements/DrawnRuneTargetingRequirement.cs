using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public abstract class DrawnRuneTargetingRequirement
{
    public abstract Usability Satisfied(Creature source, DrawnRune drawnRune);
}