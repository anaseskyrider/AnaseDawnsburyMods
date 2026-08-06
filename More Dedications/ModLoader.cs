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
        
        ////////////////////////
        // Updated Archetypes //
        ////////////////////////
        Archer.LoadArchetype();
        MartialArtist.LoadArchetype();
        Medic.LoadArchetype();
        Wrestler.LoadArchetype();
        // TODO: Update Sentinel to add the resting-armor feat.
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        Mauler.LoadArchetype();
        Bastion.LoadArchetype();
        Marshal.LoadArchetype();
        BlessedOne.LoadArchetype();
        Scout.LoadArchetype();
        Assassin.LoadArchetype();
        DualWeaponWarrior.LoadArchetype();
        FamiliarMaster.LoadArchetype();
        // TODO: Student of Perfection
        // TODO: Staff Acrobat
        // TODO: Poisoner
        // TODO: Sniping Duo
        // TODO: Bounty Hunter
        // TODO: Chronoskimmer
        // TODO: Curse Maelstrom
        // TODO: Eldritch Researcher
        // TODO: Gladiator
        // TODO: Mind Smith
        // TODO: Pistol Phenom
        // TODO: Shadowcaster
        
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