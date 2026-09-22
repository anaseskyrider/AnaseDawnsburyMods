using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class EtchedAtStartOfCombatRequirement : CreatureTargetingRequirement
{
    public const string UNUSABLE_WHY = "Can only be etched";
    
    public override Usability Satisfied(Creature source, Creature target)
    {
        return source.Battle.RoundNumber < 1
            ? Usability.Usable
            : Usability.NotUsable(UNUSABLE_WHY);
    }
}