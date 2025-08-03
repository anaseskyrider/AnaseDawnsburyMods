using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ////////////////
        // Load Calls //
        ////////////////
        ModData.LoadData();
        ModItems.LoadItems();
        
        RunesmithClass.LoadClass();
        RunesmithArchetype.LoadArchetype();
        
        RunesmithRunes.LoadRunes();
        RunesmithFeats.CreateFeats();
        
        ////////////////////////
        // Modify Stat Blocks //
        ////////////////////////
        int abilitiesIndex = CreatureStatblock.CreatureStatblockSectionGenerators.FindIndex(gen => gen.Name == "Abilities");
        CreatureStatblock.CreatureStatblockSectionGenerators.Insert(abilitiesIndex,
            new CreatureStatblockSectionGenerator("Runic repertoire", CommonRuneRules.DescribeRunicRepertoire));
    }
}