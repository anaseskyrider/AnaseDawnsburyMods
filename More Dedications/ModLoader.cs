using Dawnsbury.Core.Mechanics;
using Dawnsbury.Modding;
using Dawnsbury.Mods.MoreDedications.Archetypes;

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
        Archer.LoadArchetype();
        Medic.LoadArchetype();
        Wrestler.LoadArchetype();
        // TODO: Update Sentinel to add the resting-armor feat.
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        Mauler.LoadArchetype();
        Bastion.LoadArchetype();
        MartialArtist.LoadArchetype();
        Marshal.LoadArchetype();
        BlessedOne.LoadArchetype();
        Scout.LoadArchetype();
        Assassin.LoadArchetype();
        DualWeaponWarrior.LoadArchetype();
        FamiliarMaster.LoadArchetype();
    }
}