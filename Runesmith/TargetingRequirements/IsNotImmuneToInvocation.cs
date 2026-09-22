using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class IsNotImmuneToInvocation(Rune rune) : CreatureTargetingRequirement
{
    public Rune Rune = rune;

    public string UnusableWhy => $"Is immune to {Rune.Id.ToWord()} invocations.";
    
    public override Usability Satisfied(Creature source, Creature target)
    {
        return CommonRuneRules.IsImmuneToThisInvocation(target, Rune)
            ? Usability.NotUsableOnThisCreature(UnusableWhy)
            : Usability.Usable;
    }
}