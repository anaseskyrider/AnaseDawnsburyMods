using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;
public class MoreDedications
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ///////////////
        // Mod Trait //
        ///////////////
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        ArchetypeMauler.LoadMod();
        ArchetypeArcher.LoadMod();
        ArchetypeBastion.LoadMod();
        ArchetypeMartialArtist.LoadMod();
    }
}