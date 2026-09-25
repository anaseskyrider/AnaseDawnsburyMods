using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class LegacyItemTargetingRequirement(Func<Creature, Item, Usability> additionalConditionOnTarget)
    : ItemTargetingRequirement
{
    public Func<Creature, Item, Usability> AdditionalConditionOnTarget { get; } = additionalConditionOnTarget;

    public override Usability Satisfied(Creature source, Item item)
    {
        return this.AdditionalConditionOnTarget(source, item);
    }
}