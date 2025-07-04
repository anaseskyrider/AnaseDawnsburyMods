using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.StrategistSubclasses;

public static class ModData
{
    public static class Traits
    {
        // Mod Trait
        public static readonly Trait StrategistSubclasses = ModManager.RegisterTrait("StrategistSubclasses", new TraitProperties("Strategist Subclasses", true));
    }
    
    public static class FeatNames
    {
        #region Subclasses
        public static readonly FeatName AlchemicalSciences = ModManager.RegisterFeatName("StrategistSubclassses.AlchemicalSciences", "Alchemical Sciences");
        public static readonly FeatName Empiricism = ModManager.RegisterFeatName("StrategistSubclassses.Empiricism", "Empiricism");
        public static readonly FeatName ForensicMedicine = ModManager.RegisterFeatName("StrategistSubclassses.ForensicMedicine", "Forensic Medicine");
        public static readonly FeatName Interrogation = ModManager.RegisterFeatName("StrategistSubclassses.Interrogation", "Interrogation");
        #endregion
        #region Feats
        public static readonly FeatName AlchemicalDiscoveries = ModManager.RegisterFeatName("StrategistSubclassses.AlchemicalDiscoveries", "Alchemical Discoveries");
        #endregion
    }
    
    public static class QEffectIds
    {
        public static readonly QEffectId ExpeditiousInspection = ModManager.RegisterEnumMember<QEffectId>("Expeditious Inspection");
    }

    public static class ActionIds
    {
        //public static readonly ActionId JKL = ModManager.RegisterEnumMember<ActionId>("JKL");
    };

    public static class Illustrations
    {
        //public static readonly Illustration DawnsburySun = new ModdedIllustration("StrategistSubclassesAssets/PatreonSunTransparent.png");
        public static readonly string DDSunPath = "StrategistSubclassesAssets/PatreonSunTransparent.png";
        public static readonly Illustration ExpeditiousInspection = new ModdedIllustration("StrategistSubclassesAssets/searching.png");
    }

    public static class Tooltips
    {
        public static readonly Func<string?, string> RecallWeakness = inline => "{tooltip:StrategistSubclasses.RecallWeakness}" + inline + "{/}";
    }
}