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
    public const string ID_PREPEND = "StrategistSubclasses.";

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
        QEffectIds.Initialize();
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

    public static class Illustrations
    {
        public const string MOD_FOLDER = "StrategistSubclassesAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(MOD_FOLDER+"PatreonSunTransparent.png");
        public static readonly Illustration ExpeditiousInspection = new ModdedIllustration(MOD_FOLDER+"searching.png");
    }
    
    public static class QEffectIds
    {
        public static QEffectId ExpeditiousInspection;
        
        public static void Initialize()
        {
            ExpeditiousInspection = ModManager.SafelyRegisterEnumMember<QEffectId>("ExpeditiousInspection");
        }
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> RecallWeakness = RegisterTooltipInserter(
            ID_PREPEND+"RecallWeakness",
            "Requires the {i}DawnniExpanded{/i} or {i}Lores and Weaknesses{/i} mod installed.");
        
        /// <summary>
        /// Registers a tooltip, then returns a function that can be used to insert the tooltip with any arbitrary text.
        /// </summary>
        /// <param name="tooltipName">The registered name of the tooltip.</param>
        /// <param name="tooltipDescription">The body text of the tooltip.</param>
        /// <returns>(Func[string, string]) A function which takes in the text to insert, and returns a tooltip with the passed text.</returns>
        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }
    }
}