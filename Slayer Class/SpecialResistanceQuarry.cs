using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.SlayerClass;

public class SpecialResistanceQuarry : SpecialResistance
{
    /// <summary>
    /// Functions as <see cref="SpecialResistance"/>, except the given category only applies to your quarry.
    /// </summary>
    public SpecialResistanceQuarry(
        string name,
        Func<CombatAction?,DamageKind,bool> applicable,
        int value,
        string? exceptionsList,
        Creature self)
        : base(
            name,
            (ca, dk) =>
                ca?.Owner is not null
                && Core.IsMyQuarry(self, ca.Owner)
                && applicable(ca, dk),
            value,
            exceptionsList)
    {
        Self = self;
    }

    public Creature Self { get; }
    
    public override string ToString()
    {
        return $"{this.Name} {this.Value.ToString()} (only your quarry{(this.ExceptionList != null ? $"; except {this.ExceptionList}" : "")})";
    }

    public static void Add(WeaknessAndResistance weakRes, 
        string name,
        Func<CombatAction?,DamageKind,bool> applicable,
        int value,
        string? exceptionsList)
    {
        weakRes.Resistances.Add(new SpecialResistanceQuarry(name, applicable, DestructiveAuraModification(value, weakRes.Self), exceptionsList, weakRes.Self));
    }

    public static int DestructiveAuraModification(int value, Creature self)
    {
        QEffect? qeffect = self.FindQEffect(QEffectId.YourResistancesAreReducedBy2);
        return qeffect != null ? Math.Max(value - qeffect.Value, 0) : value;
    }
}