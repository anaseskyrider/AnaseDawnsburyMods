using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.AndroidAncestry;

public static class ModData
{
    public const string IdPrepend = "AndroidAncestry.";
    
    /// <summary>
    /// When loading mod data, the initializer won't run on certain data forms, and so they must be loaded the long way around to prevent errors.
    /// </summary>
    /// <list type="bullet">
    /// <item><see cref="ActionId"/>s</item>
    /// <item><see cref="QEffectId"/>s</item>
    /// </list>
    public static void LoadData()
    {
        BooleanOptions.Initialize();
        QEffectIds.Initialize();
    }

    /// <summary>
    /// Registers the source enum to the game, or returns the original if it's already registered.
    /// </summary>
    /// <param name="technicalName">The technicalName string of the enum being registered.</param>
    /// <typeparam name="T">The enum being registered to.</typeparam>
    /// <returns>The newly registered enum.</returns>
    public static T SafelyRegister<T>(string technicalName) where T : struct, Enum
    {
        return ModManager.TryParse(technicalName, out T alreadyRegistered)
            ? alreadyRegistered
            : ModManager.RegisterEnumMember<T>(technicalName);
    }
    
    /// <summary>
    /// Keeps the options registered with <see cref="ModManager.RegisterBooleanSettingsOption"/>. To read the registered options, use <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string)"/>.
    /// </summary>
    public static class BooleanOptions
    {
        public static void Initialize()
        {
            RemoveNaniteSurgeAura = RegisterBooleanOption(
                IdPrepend+"RemoveNaniteSurgeAura",
                "Android: Remove Nanite Surge Glow",
                "Nanite Surge emits a temporary glow after the reaction is taken. This is purely cosmetic in Dawnsbury Days, and can be safely disabled by enabling this option.",
                false);
        }
        
        /// <summary>
        /// Functions as <see cref="ModManager.RegisterBooleanSettingsOption"/>, but also returns the technicalName.
        /// </summary>
        /// <returns>(string) The technical name for the option.</returns>
        public static string RegisterBooleanOption(
            string technicalName,
            string caption,
            string longDescription,
            bool defaultValue)
        {
            ModManager.RegisterBooleanSettingsOption(technicalName, caption, longDescription, defaultValue);
            return technicalName;
        }

        public static string RemoveNaniteSurgeAura = null!;
    }
    
    public static class FeatNames
    {
        #region Ancestry
        
        public static readonly FeatName AndroidAncestry = ModManager.RegisterFeatName(IdPrepend+"AndroidAncestry", "Android");
        
        #endregion
        
        #region Ancestry Features
        
        public static readonly FeatName Constructed = ModManager.RegisterFeatName(IdPrepend+"Constructed", "Constructed");
        public static readonly FeatName EmotionallyUnaware = ModManager.RegisterFeatName(IdPrepend+"EmotionallyUnaware", "Emotionally Unaware");
        public static readonly FeatName EnhancedSenses = ModManager.RegisterFeatName(IdPrepend+"EnhancedSenses", "Enhanced Senses");
        
        #endregion

        #region Heritages
        
        public static readonly FeatName ArtisanHeritage = ModManager.RegisterFeatName(IdPrepend+"ArtisanHeritage", "Artisan Android");
        public static readonly FeatName DeceiverHeritage = ModManager.RegisterFeatName(IdPrepend+"DeceiverHeritage", "Deceiver Android");
        public static readonly FeatName LaborerHeritage = ModManager.RegisterFeatName(IdPrepend+"LaborerHeritage", "Laborer Android");
        public static readonly FeatName PolymathHeritage = ModManager.RegisterFeatName(IdPrepend+"PolymathHeritage", "Polymath Android");
        public static readonly FeatName WarriorHeritage = ModManager.RegisterFeatName(IdPrepend+"WarriorHeritage", "Warrior Android");
        
        #endregion

        #region Ancestry Feats
        
        public static readonly FeatName AndroidLore = ModManager.RegisterFeatName(IdPrepend+"AndroidLore", "Android Lore");
        public static readonly FeatName CleansingSubroutine = ModManager.RegisterFeatName(IdPrepend+"CleansingSubroutine", "Cleansing Subroutine");
        public static readonly FeatName Emotionless = ModManager.RegisterFeatName(IdPrepend+"Emotionless", "Emotionless");
        public static readonly FeatName InternalCompartment = ModManager.RegisterFeatName(IdPrepend+"InternalCompartment", "Internal Compartment");
        public static readonly FeatName NaniteSurge = ModManager.RegisterFeatName(IdPrepend+"NaniteSurge", "Nanite Surge");
        public static readonly FeatName UltravisualAdaptation = ModManager.RegisterFeatName(IdPrepend+"UltravisualAdaptation", "Ultravisual Adaptation");
        public static readonly FeatName ProximityAlert = ModManager.RegisterFeatName(IdPrepend+"ProximityAlert", "Proximity Alert");
        public static readonly FeatName RadiantCircuitry = ModManager.RegisterFeatName(IdPrepend+"RadiantCircuitry", "Radiant Circuitry");
        public static readonly FeatName AdvancedTargetingSystem = ModManager.RegisterFeatName(IdPrepend+"AdvancedTargetingSystem", "Advanced Targeting System");
        public static readonly FeatName InoculationSubroutine = ModManager.RegisterFeatName(IdPrepend+"InoculationSubroutine", "Inoculation Subroutine");
        public static readonly FeatName NaniteShroud = ModManager.RegisterFeatName(IdPrepend+"NaniteShroud", "Nanite Shroud");
        public static readonly FeatName ProtectiveSubroutine = ModManager.RegisterFeatName(IdPrepend+"ProtectiveSubroutine", "Protective Subroutine");
        public static readonly FeatName DeployableFins = ModManager.RegisterFeatName(IdPrepend+"DeployableFins", "Deployable Fins");
        public static readonly FeatName OffensiveSubroutine = ModManager.RegisterFeatName(IdPrepend+"OffensiveSubroutine", "Offensive Subroutine");
        public static readonly FeatName RepairModule = ModManager.RegisterFeatName(IdPrepend+"RepairModule", "Repair Module");
        public static readonly FeatName ConsistentSurge = ModManager.RegisterFeatName(IdPrepend+"ConsistentSurge", "Consistent Surge");
        public static readonly FeatName RevivificationProtocol = ModManager.RegisterFeatName(IdPrepend+"RevivificationProtocol", "Revivification Protocol");
        
        #endregion
    }

    public static class Illustrations
    {
        public const string ModFolder = "AndroidAncestryAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(ModFolder+"PatreonSunTransparent.png");
        public static readonly Illustration RadiantCircuitry = new ModdedIllustration(ModFolder+"idea.png");
    };
    
    public static class QEffectIds
    {
        public static void Initialize()
        {
            InternalCompartment = SafelyRegister<QEffectId>("Internal Compartment");
            NaniteSurgeImmunity = SafelyRegister<QEffectId>("Nanite Surge Immunity");
            RadiantCircuitry = SafelyRegister<QEffectId>("Radiant Circuitry");
            NanitesUnusable = SafelyRegister<QEffectId>("Nanites Unusable");
        }
        
        public static QEffectId InternalCompartment;
        public static QEffectId NaniteSurgeImmunity;
        public static QEffectId RadiantCircuitry;
        public static QEffectId NanitesUnusable;
    }
    
    public static class Traits
    {
        public static readonly Trait AndroidAncestry = ModManager.RegisterTrait("AndroidAncestry", new TraitProperties("Android", true) { IsAncestryTrait = true });
        
        public static readonly Trait Nanites = ModManager.RegisterTrait("Nanites", new TraitProperties("Nanites", false));
    }
}