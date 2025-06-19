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
        ModManager.RegisterBooleanSettingsOption(ModData.BooleanOptions.UnrestrictedTrace,
            "Runesmith: Less Restrictive Rune Tracing",
            "Enabling this option removes protections against \"bad decisions\" with tracing certain runes on certain targets.\n\nThe Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.\n\nThis option is for those players.",
            true);
        
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