using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;
using static Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.BarbarianFeatsDb.AnimalInstinctFeat;

namespace Dawnsbury.Mods.MoreDedications;
public class MoreDedications
{
    public static Trait ModNameTrait;
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ///////////////
        // Mod Trait //
        ///////////////
        ModNameTrait = ModManager.RegisterTrait("MoreDedications", new TraitProperties("More Dedications", true));
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        ArchetypeMauler.LoadMod();
        ArchetypeArcher.LoadMod();
        ArchetypeBastion.LoadMod();
    }

    /// <summary>
    /// Creates a DawnniExpanded dedication feat.
    /// </summary>
    /// <param name="featName">See <see cref="TrueFeat"/>.</param>
    /// <param name="level">See <see cref="TrueFeat"/>.</param>
    /// <param name="flavorText">See <see cref="TrueFeat"/>.</param>
    /// <param name="rulesText">See <see cref="TrueFeat"/>.</param>
    /// <param name="traits">See <see cref="TrueFeat"/>. Contains the following traits: [MoreDedications][Archetype][Dedication]</param>
    /// <param name="subfeats">See <see cref="TrueFeat"/>.</param>
    /// <returns></returns>
    public static TrueFeat NewDedicationFeat(
        FeatName featName,
        int level,
        string? flavorText,
        string rulesText,
        Trait[]? traits = null,
        List<Feat>? subfeats = null)
    {
        traits ??= [];
        return new TrueFeat(featName, level, flavorText, rulesText,
            ((Trait[])[ModNameTrait, FeatArchetype.ArchetypeTrait, FeatArchetype.DedicationTrait]).Union(traits).ToArray(), subfeats);
    }

    public static TrueFeat NewArchetypeFeat(
        Feat dedication,
        FeatName featName,
        int level,
        string? flavorText,
        string rulesText,
        Trait[]? traits = null,
        List<Feat>? subfeats = null)
    {
        traits ??= [];
        TrueFeat newFeat = (new TrueFeat(featName, level, flavorText, rulesText,
                ((Trait[]) [ModNameTrait, FeatArchetype.ArchetypeTrait]).Union(traits).ToArray(), subfeats)
            .WithPrerequisite(dedication.FeatName, dedication.Name) as TrueFeat)!;
        return newFeat;
    }

    public static TrueFeat FeatAsArchetypeFeat(
        TrueFeat dedication,
        TrueFeat featToAdd,
        string technicalName,
        string dedicationBonusName,
        int? level)
    {
        var newTraits = featToAdd.Traits.Where(trait =>
            !trait.GetTraitProperties().IsClassTrait);
        TrueFeat newFeat = (NewArchetypeFeat(
                dedication,
                ModManager.RegisterFeatName(technicalName, $"{featToAdd.Name} ({dedicationBonusName})"),
                level ?? featToAdd.Level,
                featToAdd.FlavorText,
                featToAdd.RulesText,
                newTraits.ToArray(),
                featToAdd.Subfeats)
            .WithOnSheet(values =>
                values.GrantFeat(featToAdd.FeatName))
            .WithEquivalent(values => values.AllFeats.Contains(featToAdd)) as TrueFeat)!;
        return newFeat;
    }
}