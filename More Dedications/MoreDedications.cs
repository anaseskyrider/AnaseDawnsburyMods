using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications;
public static class MoreDedications
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // Tooltips
        ModManager.RegisterInlineTooltip("MoreDedications.LevelBasedDC",
            "{b}Level-based DCs{/b}\nWhen a DC is based on your level, it uses one of the following values:\n{b}Level 1:{/b} 15\n{b}Level 2:{/b} 16\n{b}Level 3:{/b} 18\n{b}Level 4:{/b} 19\n{b}Level 5:{/b} 20\n{b}Level 6:{/b} 22\n{b}Level 7:{/b} 23\n{b}Level 8:{/b} 24");
        
        // TryParse
        if (ModManager.TryParse("GreaterScoutActivity", out QEffectId greaterScout))
            ModData.QEffectIds.GreaterScoutActivity = greaterScout;
        else
            ModData.QEffectIds.GreaterScoutActivity = ModManager.RegisterEnumMember<QEffectId>("GreaterScoutActivity");
        
        ////////////////////////
        // Updated Archetypes //
        ////////////////////////
        ArchetypeArcher.LoadMod();
        // TODO: Update Sentinel to add the resting-armor feat.
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        ArchetypeMauler.LoadMod();
        ArchetypeBastion.LoadMod();
        ArchetypeMartialArtist.LoadMod();
        ArchetypeMarshal.LoadMod();
        ArchetypeBlessedOne.LoadMod();
        ArchetypeScout.LoadMod();
        ArchetypeAssassin.LoadMod();
    }
}