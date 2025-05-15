using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        /////////////////
        // Mod Options //
        /////////////////
        ModManager.RegisterBooleanSettingsOption("RunesmithPlaytest.EsvadirOnEnemies",
            "Runesmith: Allow Tracing Esvadir On Enemies",
            "In Dawnsbury Days, the rune \"Esvadir, Rune of Whetstones\" is normally only traceable on allies because its passive effect increases the bearer's damage. Enabling this option allows you to trace Esvadir onto enemies anyway, for when you want to be able to immediately invoke the rune onto a second adjacent enemy before the end of your turn.",
            false);
        ModManager.RegisterBooleanSettingsOption("RunesmithPlaytest.OljinexOnEnemies",
            "Runesmith: Allow Tracing Oljinex On Enemies",
            "In Dawnsbury Days, the rune \"Oljinex, Rune of Cowards' Bane\" is normally only traceable on allies because its passive effect increases the bearer's defenses. Enabling this option allows you to trace Oljinex onto enemies anyway, for when you want to penalize the movemenet of the creatures around a shield-using enemy.",
            false);
        
        ////////////////
        // Load Calls //
        ////////////////
        ModTooltips.RegisterTooltips();
        ModItems.LoadItems();
        
        RunesmithClass.LoadClass();
        RunesmithArchetype.LoadArchetype();
        
        RunesmithRunes.LoadRunes();
        RunesmithFeats.CreateFeats();
    }
}