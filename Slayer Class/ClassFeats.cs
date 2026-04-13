using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;

public static class ClassFeats
{
    public static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(
            ModManager.RegisterFeatName("SlayerEmptyFeat1", "No Feat"),
            1,
            "Do nothing.", "Temporary until more feats are implemented.",
            [ModData.Traits.Slayer]);
        for (int i = 2; i < 22; i+=2)
            yield return new TrueFeat(
                ModManager.RegisterFeatName("SlayerEmptyFeat"+i, "No Feat"),
                i,
                "Do nothing.", "Temporary until more feats are implemented.",
                [ModData.Traits.Slayer]);
    }
}