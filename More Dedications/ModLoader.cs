using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.MoreDedications.Archetypes;

namespace Dawnsbury.Mods.MoreDedications;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        
        //////////////////////////
        // No Longer Maintained //
        //////////////////////////
        Assassin.LoadArchetype();
        Archer.LoadArchetype();
        Bastion.LoadArchetype();
        BlessedOne.LoadArchetype();
        DualWeaponWarrior.LoadArchetype();
        FamiliarMaster.LoadArchetype();
        Marshal.LoadArchetype();
        MartialArtist.LoadArchetype();
        Mauler.LoadArchetype();
        Medic.LoadArchetype();
        Scout.LoadArchetype();
        Wrestler.LoadArchetype();
        
        //////////////////////////
        // Partially Maintained //
        //////////////////////////
        // TODO: Organize archetypes with maintained legacy feats here.
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        // TODO: Eldritch Researcher (Hands of the Devil)
        // TODO: Harrower (Stolen Fate Player's Guide)
        // TODO: Shadowcaster (Secrets of Magic)
        // TODO: Staff Acrobat (The Show Must Go On)
        // TODO: Student of Perfection (World Guide)
        
        //////////////////////////////////
        // Replace Old FeatName Strings //
        //////////////////////////////////
        ModData.FeatNames.ReplaceOldFeatNames();
    }

    extension(ModManager)
    {
        internal static void AddFeatIfNew(Feat? newFeat)
        {
            // If feat already exists, skip it
            if (newFeat is null
                || AllFeats.GetFeatByFeatNameOptional(newFeat.FeatName) is not null)
                return;
            
            ModManager.AddFeat(newFeat);
        }
    }

    extension(ArchetypeFeats)
    {
        /// <summary>
        /// Attempt to create an agnostic archetype dedication if it doesn't exist, otherwise return null.
        /// </summary>
        internal static (TrueFeat? Feat, FeatName featName) TryCreateAgnosticArchetypeDedication(
            Trait archetype, string flavorText, string rulesText, List<Feat>? subfeats)
        {
            string technicalName = $"{archetype.ToStringOrTechnical()}Dedication";
            if (AllFeats.GetFeatByFeatNameOrStringOptional(null, technicalName) is { } found)
            {
                return (null, found.FeatName);
            }
            else
            {
                TrueFeat newFeat = ArchetypeFeats.CreateAgnosticArchetypeDedication(archetype, flavorText, rulesText, subfeats);
                return (newFeat, newFeat.FeatName);
            }
        }

        /// <summary>
        /// Attempt to duplicate a feat for an archetype if it doesn't exist, otherwise return null.
        /// </summary>
        internal static (TrueFeat? Feat, FeatName featName) TryDuplicateFeatAsArchetypeFeat(
            FeatName originalFeat, Trait archetypeTrait, int newLevel)
        {
            string technicalName = $"{originalFeat.ToStringOrTechnical()}ForArchetype{archetypeTrait.ToStringOrTechnical()}";
            if (AllFeats.GetFeatByFeatNameOrStringOptional(null, technicalName) is { } found)
            {
                return (null, found.FeatName);
            }
            else
            {
                TrueFeat newFeat = ArchetypeFeats.DuplicateFeatAsArchetypeFeat(originalFeat, archetypeTrait, newLevel);
                return (newFeat, newFeat.FeatName);
            }
        }
    }
}