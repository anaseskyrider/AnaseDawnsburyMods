using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.AndroidAncestry;

public static class ModData
{
    public const string ID_PREPEND = "AndroidAncestry.";

    public static Trait ModTrait;

    /// <summary>
    /// Loads all mod data. This should typically be called by a mod before anything else.
    /// </summary>
    /// <para>
    /// When registering mod data, certain data must be called through the execution of lines of code, rather than assigned in their initialization. The Initializer skips these data until they're first called, which can result in errors due to out of order registration calls (especially when another mod isn't using <see cref="ModManager.TryParse"/>).
    /// </para>
    /// <para>The following data forms are typically safe due to the way Dawnsbury Days loads mods (or because their initialization nearly always gets called before errors could arise): <see cref="FeatName"/>, <see cref="Illustration"/>, <see cref="Trait"/>, <see cref="SfxNames"/>, <see cref="SpellId"/>. Tooltips from <see cref="ModManager.RegisterInlineTooltip(string, string)"/> likely aren't safe to assign as part of the initializer, but they typically shouldn't be shared between mods either.
    /// </para>
    /// <para>
    /// In general, trigger the initializer by separating declaration and assignment for the following data forms:
    /// <list type="bullet">
    /// <item>All other enums (e.g. <see cref="ActionId"/>, <see cref="QEffectId"/>)</item>
    /// <item>Mod settings registered with <see cref="ModManager.RegisterBooleanSettingsOption"/></item>
    /// </list>
    /// </para>
    public static void LoadData()
    {
        ModTrait = ModManager.ModBeingLoadedTrait!.Value; // Known not null at this stage
        ActionIds.Initialize();
        BooleanOptions.Initialize();
        QEffectIds.Initialize();
    }

    public static class ActionIds
    {
        public static ActionId NaniteSurge;
        
        internal static void Initialize()
        {
            NaniteSurge = ModManager.SafelyRegisterEnumMember<ActionId>("NaniteSurge");
        }
    }
    
    /// <summary>
    /// Keeps the options registered with <see cref="ModManager.RegisterBooleanSettingsOption"/>. To read the registered options, use <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string)"/>.
    /// </summary>
    public static class BooleanOptions
    {
        public static void Initialize()
        {
            RemoveNaniteSurgeAura = RegisterBooleanOption(
                ID_PREPEND+"RemoveNaniteSurgeAura",
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

    public static class CommonRequirements
    {
        public static bool CanUseNanites(Creature android)
        {
            return android.QEffects.All(qf => qf.Id != QEffectIds.NanitesDisabled);
        }

        public static bool HasNaniteSurgeUses(Creature android)
        {
            if (!CanUseNanites(android))
                return false;
            int uses = android.FindQEffect(QEffectIds.NaniteSurge)?.Value ?? 0;
            return uses > 0;
        }
    }
    
    public static class FeatNames
    {
        #region Ancestry
        
        public static readonly FeatName AndroidAncestry = ModManager.RegisterFeatName(ID_PREPEND+"AndroidAncestry", "Android");
        
        #endregion
        
        #region Ancestry Features
        
        public static readonly FeatName Constructed = ModManager.RegisterFeatName(ID_PREPEND+"Constructed", "Constructed");
        public static readonly FeatName EmotionallyUnaware = ModManager.RegisterFeatName(ID_PREPEND+"EmotionallyUnaware", "Emotionally Unaware");
        public static readonly FeatName EnhancedSenses = ModManager.RegisterFeatName(ID_PREPEND+"EnhancedSenses", "Enhanced Senses");
        
        #endregion

        #region Heritages
        
        public static readonly FeatName ArtisanHeritage = ModManager.RegisterFeatName(ID_PREPEND+"ArtisanHeritage", "Artisan Android");
        public static readonly FeatName DeceiverHeritage = ModManager.RegisterFeatName(ID_PREPEND+"DeceiverHeritage", "Deceiver Android");
        public static readonly FeatName LaborerHeritage = ModManager.RegisterFeatName(ID_PREPEND+"LaborerHeritage", "Laborer Android");
        public static readonly FeatName PolymathHeritage = ModManager.RegisterFeatName(ID_PREPEND+"PolymathHeritage", "Polymath Android");
        public static readonly FeatName WarriorHeritage = ModManager.RegisterFeatName(ID_PREPEND+"WarriorHeritage", "Warrior Android");
        
        #endregion

        #region Ancestry Feats
        
        public static readonly FeatName AndroidLore = ModManager.RegisterFeatName(ID_PREPEND+"AndroidLore", "Android Lore");
        public static readonly FeatName CleansingSubroutine = ModManager.RegisterFeatName(ID_PREPEND+"CleansingSubroutine", "Cleansing Subroutine");
        public static readonly FeatName Emotionless = ModManager.RegisterFeatName(ID_PREPEND+"Emotionless", "Emotionless");
        public static readonly FeatName InternalCompartment = ModManager.RegisterFeatName(ID_PREPEND+"InternalCompartment", "Internal Compartment");
        public static readonly FeatName NaniteSurge = ModManager.RegisterFeatName(ID_PREPEND+"NaniteSurge", "Nanite Surge");
        public static readonly FeatName UltravisualAdaptation = ModManager.RegisterFeatName(ID_PREPEND+"UltravisualAdaptation", "Ultravisual Adaptation");
        public static readonly FeatName ProximityAlert = ModManager.RegisterFeatName(ID_PREPEND+"ProximityAlert", "Proximity Alert");
        public static readonly FeatName RadiantCircuitry = ModManager.RegisterFeatName(ID_PREPEND+"RadiantCircuitry", "Radiant Circuitry");
        public static readonly FeatName AdvancedTargetingSystem = ModManager.RegisterFeatName(ID_PREPEND+"AdvancedTargetingSystem", "Advanced Targeting System");
        public static readonly FeatName InoculationSubroutine = ModManager.RegisterFeatName(ID_PREPEND+"InoculationSubroutine", "Inoculation Subroutine");
        public static readonly FeatName NaniteShroud = ModManager.RegisterFeatName(ID_PREPEND+"NaniteShroud", "Nanite Shroud");
        public static readonly FeatName ProtectiveSubroutine = ModManager.RegisterFeatName(ID_PREPEND+"ProtectiveSubroutine", "Protective Subroutine");
        public static readonly FeatName DeployableFins = ModManager.RegisterFeatName(ID_PREPEND+"DeployableFins", "Deployable Fins");
        public static readonly FeatName OffensiveSubroutine = ModManager.RegisterFeatName(ID_PREPEND+"OffensiveSubroutine", "Offensive Subroutine");
        public static readonly FeatName RepairModule = ModManager.RegisterFeatName(ID_PREPEND+"RepairModule", "Repair Module");
        public static readonly FeatName ConsistentSurge = ModManager.RegisterFeatName(ID_PREPEND+"ConsistentSurge", "Consistent Surge");
        public static readonly FeatName RevivificationProtocol = ModManager.RegisterFeatName(ID_PREPEND+"RevivificationProtocol", "Revivification Protocol");
        
        #endregion
    }

    public static class Illustrations
    {
        public const string MOD_FOLDER = "AndroidAncestryAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(MOD_FOLDER+"PatreonSunTransparent.png");
        public static readonly Illustration NaniteSurge = new ModdedIllustration(MOD_FOLDER+"flash.png");
        public static readonly Illustration RadiantCircuitry = new ModdedIllustration(MOD_FOLDER+"idea.png");
        public static readonly Illustration RepairModule = new ModdedIllustration(MOD_FOLDER+"maintenance.png");
    }

    public static class PersistentActions
    {
        public const string NANITE_SHROUD = "NANITE_SHROUD";
        public const string REPAIR_MODULE = "REPAIR_MODULE";
        public const string REVIVIFICATION_PROTOCOL = "REVIVIFICATION_PROTOCOL";
    }
    
    public static class QEffectIds
    {
        public static QEffectId InternalCompartment;
        public static QEffectId NaniteSurge;
        public static QEffectId NanitesDisabled;
        public static QEffectId RadiantCircuitry;
        
        public static void Initialize()
        {
            InternalCompartment = ModManager.SafelyRegisterEnumMember<QEffectId>("InternalCompartment");
            NaniteSurge = ModManager.SafelyRegisterEnumMember<QEffectId>("NaniteSurge");
            NanitesDisabled = ModManager.SafelyRegisterEnumMember<QEffectId>("NanitesDisabled");
            RadiantCircuitry = ModManager.SafelyRegisterEnumMember<QEffectId>("RadiantCircuitry");
        }
    }
    
    public static class Traits
    {
        public static readonly Trait Android = ModManager.RegisterTrait("Android", new TraitProperties("Android", true) { IsAncestryTrait = true });
        
        public static readonly Trait Nanites = ModManager.RegisterTrait("Nanites", new TraitProperties("Nanites", true, "{i}Android bodies contain microscopic nanites, transported by fluid too watery to be blood, that manage their organic processes.{/i}"));
    }
}