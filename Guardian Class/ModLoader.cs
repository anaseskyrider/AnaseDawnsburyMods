using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.GuardianClass;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ////////////////
        // Load Calls //
        ////////////////
        ModData.LoadData();
        ParryLogic.Load(
            "GuardianClass",
            new ModdedIllustration(ModData.Illustrations.MOD_FOLDER+"ParryT7.png"),
            new ModdedIllustration(ModData.Illustrations.MOD_FOLDER+"ParryT6.png"));
        GuardianClass.LoadClass();
        GuardianFeats.LoadFeats();
        GuardianArchetype.LoadArchetype();
        
        // Update class language
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            // Adjust class text
            Feat? guardianClass = AllFeats.All.FirstOrDefault(ft => ft.FeatName == ModData.FeatNames.GuardianClass);
            guardianClass!.RulesText = guardianClass.RulesText.Replace("Ability boosts", "Attribute boosts");
            
            // Add modded feats
            foreach (TrueFeat feat in AllFeats.All
                         .Where(ft => ft is TrueFeat)
                         .Cast<TrueFeat>()
                         .Where(tf =>
                             (tf.Name.Contains("Reflexive Shield")) // Will include Improved Reflexive Shield
                             && !tf.HasTrait(Trait.Archetype)))
                feat.WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
            /*if (ModManager.TryParse("MoreDedications.Class.Fighter.ReflexiveShield", out FeatName refShield))
                (AllFeats.GetFeatByFeatName(refShield) as TrueFeat)!
                    .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);*/
        };
    }
}