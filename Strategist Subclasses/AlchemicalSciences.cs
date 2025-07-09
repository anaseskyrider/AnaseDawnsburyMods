using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.StrategistSubclasses;

public static class AlchemicalSciences
{
    public static Feat LoadSubclass()
    {
        Feat alchemicalDiscovery = new TrueFeat(
            ModData.FeatNames.AlchemicalDiscoveries,
            4,
            "You've devoted extra time in the lab to improve your knowledge of alchemy.",
            "You can use the {i}Bundle of Backgrounds'{/i} Concoct Poultice twice per encounter, instead of once.",
            [ModData.Traits.StrategistSubclasses, Trait.Investigator])
            .WithPrerequisite(values => values.HasFeat(ModData.FeatNames.AlchemicalSciences), "Must have the alchemical sciences strategist subclass.")
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                {
                    if (action.Name.Contains("Concoct Poultice"))
                    {
                        qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Remove("Concoct Poultice");
                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                    }
                };
            });
        ModManager.AddFeat(alchemicalDiscovery);
        
        Feat alchemicalSciences = new Feat(
                ModData.FeatNames.AlchemicalSciences,
                "Your methodology emphasizes chemical and alchemical analysis, collecting information from unusual particles and fluids found on the scene. You possess enough alchemical know-how to whip up a few tinctures to help you with your cases.",
                "You're trained in Crafting.\n\n" + new ModdedIllustration(ModData.Illustrations.DDSunPath).IllustrationAsIconString + " {b}Modding{/b} If the {i}Bundle of Backgrounds{/i} mod is installed, you also gain its Concoct Poultice action, which you can use once per encounter instead of once per day.",
                [ModData.Traits.StrategistSubclasses],
                [])
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
            })
            .WithOnCreature((values, cr) =>
            {
                if (ModManager.TryParse("Herbalist", out FeatName herbalist))
                {
                    if (!values.HasFeat(herbalist))
                        AllFeats.GetFeatByFeatName(herbalist).OnCreature?.Invoke(values, cr);
                    
                    cr.AddQEffect(new QEffect()
                    {
                        Name = "More Poultices",
                        EndOfCombat = async (qfThis2, won) =>
                        {
                            qfThis2.Owner.PersistentUsedUpResources.UsedUpActions.Remove("Concoct Poultice");
                        }
                    });
                }
            });
        ModManager.AddFeat(alchemicalSciences);
        return alchemicalSciences;
    }
}