using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class TargetWearsArmorCreatureTargetingRequirement
    : CreatureTargetingRequirement
{
    public static string ReasoningIfNot => "Not wearing armor";

    public override Usability Satisfied(Creature source, Creature target)
    {
        return target.Armor.WearsArmor || target.Characteristics.WearsMetalArmor
            ? Usability.Usable
            : Usability.NotUsableOnThisCreature(ReasoningIfNot);
    }
}