using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public abstract class ItemTargetingRequirement
{
    public abstract Usability Satisfied(Creature source, Item item);
}