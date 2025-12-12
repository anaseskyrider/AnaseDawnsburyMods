using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        
        ////////////////////////
        // Updated Archetypes //
        ////////////////////////
        ArchetypeArcher.LoadArchetype();
        ArchetypeMedic.LoadArchetype();
        // TODO: Update Sentinel to add the resting-armor feat.
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        ArchetypeMauler.LoadArchetype();
        ArchetypeBastion.LoadArchetype();
        ArchetypeMartialArtist.LoadArchetype();
        ArchetypeMarshal.LoadArchetype();
        ArchetypeBlessedOne.LoadArchetype();
        ArchetypeScout.LoadArchetype();
        ArchetypeAssassin.LoadArchetype();
        ArchetypeDualWeaponWarrior.LoadArchetype();
    }
}