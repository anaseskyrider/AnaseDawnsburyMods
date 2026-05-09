using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        Trophies.Load();
        Slayer.Load();
        ClassFeats.Load();
        HuntingTools.Load();
    }

    /*public static void TestingEnumerations()
    {
        Console.WriteLine();
        
        Console.WriteLine("Testing ToString(), ToStringFast(), ToStringOrTechnical(), HumanizeTitleCase2()...");
        /*Console.WriteLine("> Base (DK): " + DamageKind.Bludgeoning.ToString() + ", " + DamageKind.Bludgeoning.ToStringFast() + ", " + DamageKind.Bludgeoning.ToStringOrTechnical());#1#
        Console.WriteLine("> Base (Trait): " + Trait.ColdIron.ToString() + ", " + Trait.ColdIron.ToStringFast() + ", " + Trait.ColdIron.ToStringOrTechnical() + ", " + Trait.ColdIron.HumanizeTitleCase2());
        /*Console.WriteLine("> Modded (DK): " + DamageSpirit.Spirit.ToString() + ", " + DamageSpirit.Spirit.ToStringFast() + ", " + DamageSpirit.Spirit.ToStringOrTechnical());#1#
        Console.WriteLine("> Modded (Trait): " + ModData.Traits.HuntingTool.ToString() + ", " + ModData.Traits.HuntingTool.ToStringFast() + ", " + ModData.Traits.HuntingTool.ToStringOrTechnical() + ", " + ModData.Traits.HuntingTool.HumanizeTitleCase2());
        Console.WriteLine();
        Console.WriteLine("Testing modded enum parsing...");
        Console.WriteLine("> DamageSpirit.Spirit.ToString(): " + DamageSpirit.Spirit.ToString());
        Console.WriteLine(">> Enum.TryParse(DamageSpirit.Spirit.ToString(), out DamageKind spirit): " + Enum.TryParse(DamageSpirit.Spirit.ToString(), out DamageKind spirit1));
        Console.WriteLine("> DamageSpirit.Spirit.ToStringOrTechnical(): " + DamageSpirit.Spirit.ToStringOrTechnical());
        Console.WriteLine(">> Enum.TryParse(DamageSpirit.Spirit.ToStringOrTechnical(), out DamageKind spirit): " + Enum.TryParse(DamageSpirit.Spirit.ToStringOrTechnical(), out DamageKind spirit2));
        Console.WriteLine("> nameof(DamageSpirit.Spirit): " + DamageSpirit.Spirit.ToStringOrTechnical());
        Console.WriteLine(">> Enum.TryParse(nameof(DamageSpirit.Spirit), out DamageKind spirit): " + Enum.TryParse(nameof(DamageSpirit.Spirit), out DamageKind spirit3));
        Console.WriteLine("(Outputs: TryParse (bool), out.ToString(), out.ToStringOrTechnical())");
        Console.WriteLine("> ModManager.TryParse(\"Spirit\", out DamageKind spirit4): " + ModManager.TryParse("Spirit", out DamageKind spirit4) + ", " + spirit4.ToString() + ", " + spirit4.ToStringOrTechnical());
        Console.WriteLine("> ModManager.TryParse(DamageSpirit.Spirit.ToString(), out DamageKind spirit5): " + ModManager.TryParse(DamageSpirit.Spirit.ToString(), out DamageKind spirit5) + ", " + spirit5.ToString() + ", " + spirit5.ToStringOrTechnical());
        Console.WriteLine("> ModManager.TryParse(DamageSpirit.Spirit.ToStringOrTechnical(), out DamageKind spirit6): " + ModManager.TryParse(DamageSpirit.Spirit.ToStringOrTechnical(), out DamageKind spirit6) + ", " + spirit6.ToString() + ", " + spirit6.ToStringOrTechnical());
        
        Console.WriteLine();
        
        Console.WriteLine("Testing modded skill enum, Monster Lore...");
        Console.WriteLine("Testing ToString(), ToStringFast(), ToStringOrTechnical(), HumanizeTitleCase2()...");
        Console.WriteLine("> " + Slayer.MonsterLore.Skill.ToString() + ", " + Slayer.MonsterLore.Skill.ToStringFast() + ", " + Slayer.MonsterLore.Skill.ToStringOrTechnical() + ", " + Slayer.MonsterLore.Skill.HumanizeTitleCase2());
        
        Console.WriteLine();
        Console.WriteLine("Testing modded skill enum, Warfare Lore...");
        Console.WriteLine("Testing ToString(), ToStringFast(), ToStringOrTechnical(), HumanizeTitleCase2()...");
        Console.WriteLine("> " + LoresAndWeaknesses.Lores.GetRegisteredLore("Warfare Lore", null)!.Skill.ToString() + ", " + LoresAndWeaknesses.Lores.GetRegisteredLore("Warfare Lore", null)!.Skill.ToStringFast() + ", " + LoresAndWeaknesses.Lores.GetRegisteredLore("Warfare Lore", null)!.Skill.ToStringOrTechnical() + ", " + LoresAndWeaknesses.Lores.GetRegisteredLore("Warfare Lore", null)!.Skill.HumanizeTitleCase2());
        
        Console.WriteLine();
        
    }*/
}