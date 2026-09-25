using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class LegacyRuneTargetingRequirement(Func<Creature, DrawnRune, Usability> additionalConditionOnTarget)
    : DrawnRuneTargetingRequirement
{
    public Func<Creature, DrawnRune, Usability> AdditionalConditionOnTarget { get; } = additionalConditionOnTarget;

    public override Usability Satisfied(Creature source, DrawnRune drawnRune)
    {
        return this.AdditionalConditionOnTarget(source, drawnRune);
    }
}