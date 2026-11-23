using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Mods.MoreShields;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class OptionalDependencies
{
    public static bool IsDamagedShield(Item shield)
    {
        var props = shield.GetShieldProperties();
        return props?.HP < props?.MaxHP;
    }
    
    public static void RepairShield(TBattle battle, Creature? wielder, Item shield, int healing, string source)
    {
        CommonShieldRules.LogShieldHealing(battle, wielder, shield, healing, source, out MoreShields.CommonShieldRules.ShieldProperties? props);
        props?.HP += healing;
    }

    public static void DamageShield(TBattle battle, Creature? wielder, Item shield, int damage, string appendLines)
    {
        int reduceableAmount = Math.Min(damage, shield.Hardness);
        int finalDamage = Math.Max(0, damage - reduceableAmount);
        appendLines = $"{{b}}-{{/b}}{appendLines}\n{{b}}{(reduceableAmount).WithPlus()}{{/b}} Hardness";
        CommonShieldRules.LogShieldDamage(battle, wielder, shield, finalDamage, appendLines, out MoreShields.CommonShieldRules.ShieldProperties? props);
        int beforeHP = props?.HP ?? 0;
        props?.HP -= finalDamage;

        if (props?.HP <= 0 && beforeHP > 0)
        {
            CommonShieldRules.LogBrokenShield(battle, wielder, shield);
        }
    }

    public static string DescribeHP(Item shield)
    {
        var props = shield.GetShieldProperties();
        if (props is null)
            return "";
        return $"{{b}}Current HP:{{/b}} {props.HP}/{props.MaxHP}";
    }
}