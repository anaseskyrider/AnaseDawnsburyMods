using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.MoreArchetypes.Archetypes;

namespace Dawnsbury.Mods.MoreArchetypes;
public static class ModLoader
{
    public const string DEDICATION_SPECIAL =
        "{b}Special{/b} After you take a dedication feat, you must take at least 2 more archetype feats from this dedication before you're allowed to take another dedication feat. {i}(Unless you're using the Free Archetype variant rule, in which case you may have up to 2 archetypes “open” this way.){/i}";
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModData.LoadData();
        
        Archer.Load();
        Assassin.Load();
        Bastion.Load();
        BlessedOne.Load();
        DualWeaponWarrior.Load();
        FamiliarMaster.Load();
        Marshal.Load();
        MartialArtist.Load();
        Mauler.Load();
        
        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            // Give remastered weapon proficiency to all classes.
            foreach (ClassSelectionFeat ft in AllFeats.All
                         .Where(ft => ft is ClassSelectionFeat)
                         .Cast<ClassSelectionFeat>()
                         .ToList())
            {
                switch (ft.ClassTrait)
                {
                    case Trait.Wizard:
                        ft.RulesText = ft.RulesText
                            .Replace(
                                "You're trained in the club, crossbow, dagger, heavy crossbow and the staff.",
                                "You're trained in all simple weapons.");
                        ft.OnSheet += values =>
                        {
                            values.SetProficiency(Trait.Simple, Proficiency.Trained);
                            values.IncreaseProficiency(11, Trait.Simple, Proficiency.Expert);
                        };
                        break;
                    case Trait.Rogue:
                        ft.RulesText = ft.RulesText
                            .Replace(
                                "You're trained in all simple weapons, as well as the rapier, shortbow, composite shortbow and shortsword.",
                                "You're trained in all simple and martial weapons.");
                        ft.OnSheet += values =>
                        {
                            values.SetProficiency(Trait.Martial, Proficiency.Trained);
                            values.IncreaseProficiency(5, Trait.Martial, Proficiency.Expert);
                            values.IncreaseProficiency(13, Trait.Martial, Proficiency.Master);
                        };
                        break;
                    case Trait.Bard:
                        ft.RulesText = ft.RulesText
                            .Replace(
                                "You're trained in all simple weapons, as well as the longsword, rapier, shortbow, composite shortbow, shortsword, and whip.",
                                "You're trained in all simple and martial weapons.");
                        ft.OnSheet += values =>
                        {
                            values.SetProficiency(Trait.Martial, Proficiency.Trained);
                            values.IncreaseProficiency(11, Trait.Martial, Proficiency.Expert);
                        };
                        break;
                }
            }
        };
    }

    extension(ModManager)
    {
        /// <summary>
        /// Add a feat, or replace it if it was already added by More Dedications.
        /// </summary>
        internal static void AddAndReplaceFeat(Feat newFeat)
        {
            // If feat is new,
            if (AllFeats.GetFeatByFeatNameOptional(newFeat.FeatName) is not {} oldFeat)
            {
                // Add it normally.
                ModManager.AddFeat(newFeat);
                return;
            }

            // If added by More Dedications,
            // (look for mod source trait)
            if (oldFeat.Traits.Any(t =>
                    t.ToStringOrTechnical() is { } name
                    && name.Contains("Mod")
                    && (name.Contains("MoreDedications") || name.Contains("More Dedications"))))
                // Replace it.
                AllFeats.ReplaceFeat(oldFeat, newFeat);
        }
    }

    extension(AllFeats)
    {
        /// <summary>
        /// Replaces an old Feat with a new Feat. Only works if both feats share the same FeatName, and if the old Feat came from AllFeats.
        /// </summary>
        internal static void ReplaceFeat(Feat oldFeat, Feat newFeat)
        {
            if (oldFeat.FeatName != newFeat.FeatName)
                return;
            
            if (!newFeat.Traits.Contains(ModData.ModTrait))
                newFeat.Traits.Insert(0, ModData.ModTrait);
            
            AllFeats.All[AllFeats.All.IndexOf(oldFeat)] = newFeat;
        }
    }

    extension(ArchetypeFeats)
    {
        /// <summary>
        /// Creates a dedication if it doesn't exist, or resets its behavior if it does.
        /// </summary>
        /// <remarks>The following properties are also set to null: OnSheet, OnCreature, ActionCost, Illustration, RulesTextCreator, Tag, ShowRulesBlockFor fields, InappropriateBecauseOfBadInventory,</remarks>
        /// <returns>The original feat after resetting a bunch of data, or a new dedication</returns>
        internal static TrueFeat CreateOrUpdateDedication(
            Trait archetype,
            string flavorText, 
            string rulesText, 
            List<Feat>? subfeats = null,
            Action<TrueFeat>? adjustIfExists = null)
        {
            TrueFeat dedication;
            if (ArchetypeFeats.GetDedicationFromArchetypeTrait(archetype) is { } exists)
            {
                dedication = exists;
                dedication.FlavorText = flavorText;
                dedication.RulesText = rulesText + "\n\n" + DEDICATION_SPECIAL;
                dedication.Subfeats = subfeats;
                
                // Null stuff
                dedication.OnSheet = null;
                dedication.OnSheet += values =>
                {
                    values.SetProficiency(archetype, Proficiency.Trained);
                    values.AdditionalClassTraits.Add(archetype);
                    values.NumberOfFeatsForDedication.TryAdd(archetype, 0);
                };
                dedication.OnCreature = null;
                dedication.OnCreature += (values, self) =>
                    self.Traits.Add(archetype);
                dedication.ActionCost = null;
                dedication.Illustration = IllustrationName.MulticlassFeat; // Default value
                dedication.IllustrationCreator = null;
                dedication.RulesTextCreator = null;
                dedication.Tag = null;
                dedication.ShowRulesBlockFor = SpellId.None;
                dedication.ShowRulesBlockForClassOfOrigin = null;
                dedication.ShowRulesBlockForSpellAtLevel = null;
                dedication.ShowRulesBlockForCombatAction = null;
                dedication.InappropriateBecauseOfBadInventory = null;
                
                // Adjust a dedication for anything not easily reset by the construction process,
                // such as altered feat prerequisites which can't simply start from an empty slate
                // due to the way that dedications start with a bunch of prereqs.
                adjustIfExists?.Invoke(dedication);
            }
            else
                dedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(archetype, flavorText, rulesText, subfeats);
            
            return dedication;
        }
        
        /// <summary>
        /// Update the functionality of a dedication feat.
        /// </summary>
        internal static TrueFeat UpdateExistingDedication(
            Trait archetype,
            string? flavorText = null,
            string? rulesText = null,
            Action<CalculatedCharacterSheetValues>? onSheet = null,
            Action<CalculatedCharacterSheetValues, Creature>? onCreature = null,
            List<Prerequisite>? newPrereqs = null)
        {
            TrueFeat? dedication = ArchetypeFeats.GetDedicationFromArchetypeTrait(archetype);
            
            if (dedication is null)
                throw new ArgumentException($"No dedication feat found for archetype of trait \"{archetype.ToStringOrTechnical()}\".");
            
            if (flavorText is not null)
                dedication.FlavorText = flavorText;

            if (rulesText is not null)
                dedication.RulesText = rulesText + "\n\n" + DEDICATION_SPECIAL;
            
            if (onSheet is not null)
            {
                dedication.OnSheet = null;
                dedication.OnSheet += values =>
                {
                    values.SetProficiency(archetype, Proficiency.Trained);
                    values.AdditionalClassTraits.Add(archetype);
                    values.NumberOfFeatsForDedication.TryAdd(archetype, 0);
                };
                dedication.OnSheet += onSheet;
            }

            if (onCreature is not null)
            {
                dedication.OnCreature = null;
                dedication.OnCreature += (values, self) =>
                    self.Traits.Add(archetype);
                dedication.OnCreature += onCreature;
            }

            if (newPrereqs is not null)
            {
                // TrueFeat constructor
                dedication.Prerequisites =
                [
                    new LevelPrerequisite(dedication.Level)
                ];

                if (dedication.Traits.Any(trait => trait.GetTraitProperties().IsAncestryTrait))
                {
                    dedication.Traits.Add(Trait.Ancestry);
                    List<Trait> ancestryTraits = dedication.Traits
                        .Where(trait => trait.GetTraitProperties().IsAncestryTrait)
                        .ToList();
                    dedication.Prerequisites.Add(new Prerequisite(
                        sheet => sheet.Ancestries.ContainsOneOf(ancestryTraits),
                        $"You must be {S.ConstructOrList(ancestryTraits.Select(ancestryTrait => ancestryTrait.HumanizeTitleCase2().WithIndefiniteArticle()))}."));
                }
                if (dedication.Traits.Contains(Trait.AllAncestries))
                    dedication.Traits.Add(Trait.Ancestry);
                if (dedication.Traits.Any(trait => trait.GetTraitProperties().IsClassTrait))
                {
                    dedication.Traits.Add(Trait.ClassFeat);
                    dedication.Prerequisites.Add(new ClassPrerequisite(dedication.Traits.Where(trait => trait.GetTraitProperties().IsClassTrait).ToList()));
                }
                if (dedication.Traits.Contains(Trait.Psyche))
                    dedication.Prerequisites.Add(new TrueClassPrerequisite(Trait.Psychic));
                
                // CreateDedication additions
                dedication.WithPrerequisite(
                        values => values.Class == null || values.Class.ClassTrait != archetype,
                        $"You must not be {archetype.HumanizeLowerCase2().WithIndefiniteArticle()}.")
                    .WithPrerequisite(
                        values =>
                        {
                            if (PlayerProfile.Instance.UnlimitedOpenArchetypes || !PlayerProfile.Instance.FreeArchetype)
                                return true;
                            int num2 = values.NumberOfFeatsForDedication.Count(ded =>
                                ded.Value < 2 && ded.Key != archetype);
                            bool flag = values.NumberOfFeatsForDedication.Any(ded =>
                                ded.Value >= 2 && ded.Key == archetype);
                            int num3 = (PlayerProfile.Instance.FreeArchetype ? 2 : 1) +
                                       (values.HasFeat(FeatName.Multitalented) ? 1 : 0);
                            return flag || num2 < num3;
                        },
                        "You already have two dedications open. You must finish a dedication by taking 2 archetype feats for that dedication before opening a third dedication.")
                    .WithPrerequisite(
                        values =>
                        {
                            if (PlayerProfile.Instance.UnlimitedOpenArchetypes || PlayerProfile.Instance.FreeArchetype)
                                return true;
                            int num4 = values.NumberOfFeatsForDedication.Count(ded =>
                                ded.Value < 2 && ded.Key != archetype);
                            bool flag = values.NumberOfFeatsForDedication.Any(ded =>
                                ded.Value >= 2 && ded.Key == archetype);
                            int num5 = (PlayerProfile.Instance.FreeArchetype ? 2 : 1) +
                                       (values.HasFeat(FeatName.Multitalented) ? 1 : 0);
                            return flag || num4 < num5;
                        },
                        "You already have a dedication open. You must finish your dedication by taking 2 archetype feats for that dedication before opening a second dedication.");
                
                dedication.Prerequisites.AddRange(newPrereqs);
            }
            
            if (!dedication.Traits.Contains(ModData.ModTrait))
                dedication.Traits.Insert(0, ModData.ModTrait);
            
            return dedication;
        }
        
        /// <summary>
        /// Retrieves the dedication for the archetype of a known Trait, if it exists.
        /// </summary>
        public static TrueFeat? GetDedicationFromArchetypeTrait(Trait archetype)
        {
            return AllFeats.GetFeatByFeatNameOrStringOptional(null, $"{archetype.ToStringOrTechnical()}Dedication") as TrueFeat;
        }
        
        /// <summary>
        /// Duplicates a feat for an archetype, without crashing due to registration errors if the feat already exists. To be coupled with ReplaceFeat().
        /// </summary>
        internal static TrueFeat SafelyDuplicateFeatAsArchetypeFeat(FeatName originalFeat, Trait archetypeTrait, int newLevel)
        {
            string duplicateTechnical = $"{originalFeat.ToStringOrTechnical()}ForArchetype{archetypeTrait.ToStringOrTechnical()}";
            
            // If not already added
            if ((AllFeats.GetFeatByFeatNameOrStringOptional(null, duplicateTechnical)
                as TrueFeat)
                is not { } oldDuplicate)
            {
                // Then duplicate normally
                return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(originalFeat, archetypeTrait, newLevel);
            }
            
            // Duplicate again
            TrueFeat newDuplicate = CommonFeatTemplates.CreateDuplicateFeat(
                originalFeat,
                oldDuplicate.FeatName, // Use name instead of registering
                newLevel);
            newDuplicate.Traits.RemoveAll(trait =>
                trait.GetTraitProperties().IsClassTrait);
            newDuplicate.Prerequisites.RemoveAll(prereq =>
                prereq is ClassPrerequisite);
            newDuplicate.WithAvailableAsArchetypeFeat(archetypeTrait);
            
            return newDuplicate;
        }
    }
}