using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class TargetWieldsItemCreatureTargetingRequirement(
    Func<Item, bool> itemRequirement,
    string reasoningIfNot)
    : CreatureTargetingRequirement
{
    public Func<Item, bool> ItemRequirement { get; } = itemRequirement;

    public string ReasoningIfNot { get; } = reasoningIfNot;

    public override Usability Satisfied(Creature source, Creature target)
    {
        return target.HeldItems
            .Union(target.Weapons)
            .Any(item => ItemRequirement(item))
            ? Usability.Usable
            : Usability.NotUsableOnThisCreature(ReasoningIfNot);
    }
}