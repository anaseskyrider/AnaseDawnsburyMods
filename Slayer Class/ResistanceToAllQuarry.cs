using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.SlayerClass;

/// <summary>
/// Functions as <see cref="ResistanceToAll"/>, except it doesn't care about Ghost Touch nor damage exceptions. It only cares about whether the attacker is your quarry.
/// </summary>
public class ResistanceToAllQuarry(int value, Creature self) : ResistanceToAll(value, false, [])
{
    public Creature Self { get; } = self;
    
    public override bool Matches(CombatAction? action, DamageKind damageKind)
    {
        return action?.Owner is not null && Slayer.IsMyQuarry(this.Self, action.Owner);
    }

    public override string ToString()
    {
        return $"all {this.Value.ToString()} (only your quarry)";
    }
}