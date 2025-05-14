using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace AndroidAncestry;

public static class Enums
{
    public static class Traits
    {
        public static readonly Trait AndroidAncestry = ModManager.RegisterTrait("AndroidAncestry", new TraitProperties("Android", true) { IsAncestryTrait = true });
        
        public static readonly Trait Nanites = ModManager.RegisterTrait("Nanites", new TraitProperties("Nanites", false));
    }
    
    public static class FeatNames
    {
        #region Ancestry
        public static readonly FeatName AndroidAncestry = ModManager.RegisterFeatName("AndroidAncestry.AndroidAncestry", "Android");
        #endregion
        
        #region Ancestry Features
        public static readonly FeatName Constructed = ModManager.RegisterFeatName("AndroidAncestry.Constructed", "Constructed");
        
        public static readonly FeatName EmotionallyUnaware = ModManager.RegisterFeatName("AndroidAncestry.EmotionallyUnaware", "Emotionally Unaware");
        #endregion

        #region Heritages
        public static readonly FeatName ArtisanHeritage = ModManager.RegisterFeatName("AndroidAncestry.ArtisanHeritage", "Artisan Android");
        
        public static readonly FeatName DeceiverHeritage = ModManager.RegisterFeatName("AndroidAncestry.DeceiverHeritage", "Deceiver Android");
        
        public static readonly FeatName LaborerHeritage = ModManager.RegisterFeatName("AndroidAncestry.LaborerHeritage", "Laborer Android");
        
        public static readonly FeatName PolymathHeritage = ModManager.RegisterFeatName("AndroidAncestry.PolymathHeritage", "Polymath Android");
        
        public static readonly FeatName WarriorHeritage = ModManager.RegisterFeatName("AndroidAncestry.WarriorHeritage", "Warrior Android");
        #endregion

        #region Ancestry Feats
        public static readonly FeatName AndroidLore = ModManager.RegisterFeatName("AndroidAncestry.AndroidLore", "Android Lore");
        
        public static readonly FeatName CleansingSubroutine = ModManager.RegisterFeatName("AndroidAncestry.CleansingSubroutine", "Cleansing Subroutine");
        
        public static readonly FeatName Emotionless = ModManager.RegisterFeatName("AndroidAncestry.Emotionless", "Emotionless");
        
        public static readonly FeatName InternalCompartment = ModManager.RegisterFeatName("AndroidAncestry.InternalCompartment", "Internal Compartment");
        
        public static readonly FeatName NaniteSurge = ModManager.RegisterFeatName("AndroidAncestry.NaniteSurge", "Nanite Surge");
        
        public static readonly FeatName UltravisualAdaptation = ModManager.RegisterFeatName("AndroidAncestry.UltravisualAdaptation", "Ultravisual Adaptation");
        
        public static readonly FeatName ProximityAlert = ModManager.RegisterFeatName("AndroidAncestry.ProximityAlert", "Proximity Alert");
        
        public static readonly FeatName RadiantCircuitry = ModManager.RegisterFeatName("AndroidAncestry.RadiantCircuitry", "Radiant Circuitry");
        
        public static readonly FeatName AdvancedTargetingSystem = ModManager.RegisterFeatName("AndroidAncestry.AdvancedTargetingSystem", "Advanced Targeting System");
        
        public static readonly FeatName InoculationSubroutine = ModManager.RegisterFeatName("AndroidAncestry.InoculationSubroutine", "Inoculation Subroutine");
        
        public static readonly FeatName NaniteShroud = ModManager.RegisterFeatName("AndroidAncestry.NaniteShroud", "Nanite Shroud");
        
        public static readonly FeatName ProtectiveSubroutine = ModManager.RegisterFeatName("AndroidAncestry.ProtectiveSubroutine", "Protective Subroutine");
        #endregion
    }
    
    public static class QEffectIds
    {
        //public static readonly QEffectId Effectily = ModManager.RegisterEnumMember<QEffectId>("Include Spaces In This One");
        
        public static readonly QEffectId InternalCompartment = ModManager.RegisterEnumMember<QEffectId>("Internal Compartment");
        
        public static readonly QEffectId NaniteSurgeImmunity = ModManager.RegisterEnumMember<QEffectId>("Nanite Surge Immunity");
        
        public static readonly QEffectId RadiantCircuitry = ModManager.RegisterEnumMember<QEffectId>("Radiant Circuitry");
        
        public static readonly QEffectId NanitesUnusable = ModManager.RegisterEnumMember<QEffectId>("Nanites Unusable");
    }

    public static class ActionIds
    {
        //public static readonly ActionId Actively = ModManager.RegisterEnumMember<ActionId>("ActionName");
    };

    public static class Illustrations
    {
        public static readonly Illustration RadiantCircuitry = new ModdedIllustration("AndroidAncestryAssets/idea.png");
    };

    public static class SubmenuIds
    {
        //public static readonly SubmenuId Menuishily = ModManager.RegisterEnumMember<SubmenuId>("MenuName");
    };
    
    public static class PossibilitySectionIds
    {
        //public static readonly PossibilitySectionId Sectiony = ModManager.RegisterEnumMember<PossibilitySectionId>("SectionName");
    };
}