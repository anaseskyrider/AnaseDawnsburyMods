using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
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
            
            // TODO: (1) Inspect for mod of origin
            List<Feat> allFeats = AllFeats.All
                .Where(ft => ft.ModOfOrigin is not null)
                .ToList();
            
            // TODO: (2) Test evaluation logic
            // Remove old feats.
            if (/*PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.RemoveOldFeats)*/ false)
            {
                List<Feat> moreDeds = AllFeats.All
                    .Where(ft => ft.ModOfOrigin?.Contains("MoreDedications") ?? false)
                    .ToList();
                List<Feat> moreArchs = AllFeats.All
                    .Where(ft => ft.ModOfOrigin?.Contains("MoreArchetypes") ?? false)
                    .ToList();
                // For each MoreDedications feat,
                foreach (Feat ded in moreDeds)
                {
                    if (moreArchs.Any(arch => arch.BaseName == ded.BaseName))
                    {
                        // TODO: (3) Unlock deletion when sure this will work
                        //AllFeats.All.Remove(ded);
                    }
                    // Blacklist
                    if (ded.BaseName.Contains("Advanced Bow Training"))
                    {
                        // TODO: (3) Unlock deletion when sure this will work
                        //AllFeats.All.Remove(ded);
                    }
                }
            }
        };
    }

    public static TrueFeat? GetDedication(Trait archetype)
    {
        return GetDedication(archetype.ToStringOrTechnical() + "Dedication");
    }

    public static TrueFeat? GetDedication(string featName)
    {
        return AllFeats.GetFeatByFeatNameOrStringOptional(null, featName) as TrueFeat;
    }

    internal static TrueFeat ReplaceDedicationBehavior(
        this TrueFeat feat,
        Trait archetype,
        string? flavorText = null,
        string? rulesText = null,
        Action<CalculatedCharacterSheetValues>? onSheet = null,
        Action<CalculatedCharacterSheetValues, Creature>? onCreature = null,
        List<Prerequisite>? newPrereqs = null)
    {
        if (flavorText is not null)
            feat.FlavorText = flavorText;

        if (rulesText is not null)
            feat.RulesText = rulesText + "\n\n" + DEDICATION_SPECIAL;
        
        if (onSheet is not null)
        {
            feat.OnSheet = null;
            feat.OnSheet += values =>
            {
                values.SetProficiency(archetype, Proficiency.Trained);
                values.AdditionalClassTraits.Add(archetype);
                values.NumberOfFeatsForDedication.TryAdd(archetype, 0);
            };
            feat.OnSheet += onSheet;
        }

        if (onCreature is not null)
        {
            feat.OnCreature = null;
            feat.OnCreature += (values, self) =>
                self.Traits.Add(archetype);
            feat.OnCreature += onCreature;
        }

        if (newPrereqs is not null)
        {
            // TrueFeat constructor
            feat.Prerequisites =
            [
                new LevelPrerequisite(feat.Level)
            ];

            if (feat.Traits.Any(trait => trait.GetTraitProperties().IsAncestryTrait))
            {
                feat.Traits.Add(Trait.Ancestry);
                List<Trait> ancestryTraits = feat.Traits
                    .Where(trait => trait.GetTraitProperties().IsAncestryTrait)
                    .ToList();
                feat.Prerequisites.Add(new Prerequisite(
                    sheet => sheet.Ancestries.ContainsOneOf(ancestryTraits),
                    $"You must be {S.ConstructOrList(ancestryTraits.Select(ancestryTrait => ancestryTrait.HumanizeTitleCase2().WithIndefiniteArticle()))}."));
            }
            if (feat.Traits.Contains(Trait.AllAncestries))
                feat.Traits.Add(Trait.Ancestry);
            if (feat.Traits.Any(trait => trait.GetTraitProperties().IsClassTrait))
            {
                feat.Traits.Add(Trait.ClassFeat);
                feat.Prerequisites.Add(new ClassPrerequisite(feat.Traits.Where(trait => trait.GetTraitProperties().IsClassTrait).ToList()));
            }
            if (feat.Traits.Contains(Trait.Psyche))
                feat.Prerequisites.Add(new TrueClassPrerequisite(Trait.Psychic));
            
            // CreateDedication additions
            feat.WithPrerequisite(
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
            
            feat.Prerequisites.AddRange(newPrereqs);
        }
        
        if (!feat.Traits.Contains(ModData.ModTrait))
            feat.Traits.Insert(0, ModData.ModTrait);
        
        return feat;
    }

    /// <summary>
    /// Duplicates a feat for an archetype. If it already exists: add this mod's source trait and return null. If it doesn't exist: duplicate it and let loading procedures register it while <see cref="LibraryOfAnase.extension(ModManager).AddFeat(Feat, Trait)"/> adds the source trait.
    /// </summary>
    internal static Feat? SafelyDuplicateAsArchetype(FeatName originalFeat, Trait archetypeTrait, int newLevel)
    {
        if (ModLoader.GetDuplicatedAsArchetype(originalFeat, archetypeTrait) is {} alreadyDuplicated)
        {
            alreadyDuplicated.Traits.Insert(0, ModData.ModTrait);
            return null;
        }
        else
            return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(originalFeat, archetypeTrait, newLevel);
    }

    private static Feat? GetDuplicatedAsArchetype(FeatName? originalFeat, Trait archetypeTrait)
    {
        if (originalFeat is null)
            return null;
        string duplicateTechnicalName = $"{originalFeat.Value.ToStringOrTechnical()}ForArchetype{archetypeTrait.ToStringOrTechnical()}";
        return AllFeats.GetFeatByFeatNameOrStringOptional(null, duplicateTechnicalName);
    }
}