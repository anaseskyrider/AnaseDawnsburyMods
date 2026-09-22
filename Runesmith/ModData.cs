using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.RunesmithClass;

public static class ModData
{
    public const string ID_PREPEND = "RunesmithClass.";

    public static Trait ModTrait;
    
    public static void LoadData()
    {
        ModTrait = ModManager.ModBeingLoadedTrait!.Value; // Known not null at this stage
        ActionIds.Initialize();
        BooleanOptions.Initialize();
        PossibilitySectionIds.Initialize();
        QEffectIds.Initialize();
        SubmenuIds.Initialize();
    }

    public static class ActionIds
    {
        public static ActionId TraceRune;
        public static ActionId EtchRune;
        public static ActionId InvokeRune;
        
        public static void Initialize()
        {
            TraceRune = ModManager.RegisterEnumMember<ActionId>("TraceRune");
            EtchRune = ModManager.RegisterEnumMember<ActionId>("EtchRune");
            InvokeRune = ModManager.RegisterEnumMember<ActionId>("InvokeRune");
        }
    }
    
    /// <summary>
    /// "Added the ability for mods to add settings options with <see cref="ModManager.RegisterBooleanSettingsOption(string technicalName, string caption, string longDescription, bool default)"/> for registration API and <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string technicalName)"/> for reading API."
    /// </summary>
    public static class BooleanOptions
    {
        //public const string HideRuneDialogs = "RunesmithPlaytest.HideRuneDialogs"; // Unused, but kept just in case.
        public static string UnrestrictedTrace = null!;
        
        public static void Initialize()
        {
            UnrestrictedTrace = RegisterBooleanOption(
                ID_PREPEND+"UnrestrictedTrace",
                "Runesmith: Less Restrictive Rune Tracing",
                """
                Enabling this option removes protections against "bad decisions" with tracing certain runes on certain targets.

                The Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.

                This option is for those players.
                """,
                true);
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
    }
        
    public static class FeatNames
    {
        #region Class
        
        public static readonly FeatName RunesmithClass = ModManager.RegisterFeatName(ID_PREPEND+"RunesmithClass", "Runesmith");
        
        #endregion

        #region Class Features

        public static readonly FeatName TraceRune = ModManager.RegisterFeatName(ID_PREPEND+"TraceRune", "Trace Rune {icon:Action}–{icon:TwoActions}");
        
        public static readonly FeatName InvokeRune = ModManager.RegisterFeatName(ID_PREPEND+"InvokeRune", "Invoke Rune {icon:Action}");
        
        public static readonly FeatName EtchRune = ModManager.RegisterFeatName(ID_PREPEND+"EtchRune", "Etch Rune");
        
        public static readonly FeatName RunicCrafter = ModManager.RegisterFeatName(ID_PREPEND+"RunicCrafter", "Runic Crafter");
        
        public static readonly FeatName RunicOptimization = ModManager.RegisterFeatName(ID_PREPEND+"RunicOptimization", "Runic Optimization");
        
        public static readonly FeatName AssuredRunicCrafter = ModManager.RegisterFeatName(ID_PREPEND+"AssuredRunicCrafter", "Assured Runic Crafter");
        
        public static readonly FeatName GreaterRunicOptimization = ModManager.RegisterFeatName(ID_PREPEND+"GreaterRunicOptimization", "Greater Runic Optimization");
        
        #endregion

        #region Class Feats

        #region 1st-Level

        public static readonly FeatName BackupRunicEnhancement = ModManager.RegisterFeatName(
            "BackupRunicEnhancement",
            "Backup Runic Enhancement");
        
        public static readonly FeatName EngravingStrike = ModManager.RegisterFeatName(
            "EngravingStrike",
            "Engraving Strike");
        
        public static readonly FeatName GlyphFamiliar = ModManager.RegisterFeatName(
            "GlyphFamiliar",
            "Glyph Familiar");
        
        public static readonly FeatName RemoteDetonation = ModManager.RegisterFeatName(
            "RemoteDetonation",
            "Remote Detonation");
        
        public static readonly FeatName RuneWard = ModManager.RegisterFeatName(
            "RuneWard",
            "Rune Ward");
        
        public static readonly FeatName RuneSinger = ModManager.RegisterFeatName(
            "RuneSinger",
            "Rune-Singer");
        
        public static readonly FeatName SeekTheHiddenGlyphs = ModManager.RegisterFeatName(
            "SeekTheHiddenGlyphs",
            "Seek the Hidden Glyphs");

        public static readonly FeatName SmithingWeapons = ModManager.RegisterFeatName(
            "SmithingWeapons",
            "Smithing Weapons");

        #endregion

        #region 2nd-Level

        public static readonly FeatName EnhancedGlyphFamiliar = ModManager.RegisterFeatName(
            "EnhancedGlyphFamiliar",
            "Enhanced Glyph Familiar");
        
        public static readonly FeatName FortifyingKnock = ModManager.RegisterFeatName(
            "FortifyingKnock",
            "Fortifying Knock");
        
        public static readonly FeatName InvisibleInk = ModManager.RegisterFeatName(
            "InvisibleInk",
            "Invisible Ink");
        
        public static readonly FeatName PatternFlight = ModManager.RegisterFeatName(
            "PatternFlight",
            "Pattern Flight");
        
        public static readonly FeatName RunicTattoo = ModManager.RegisterFeatName(
            "RunicTattoo",
            "Runic Tattoo");

        #endregion

        #region 4th-Level

        public static readonly FeatName ArtistsAttendance = ModManager.RegisterFeatName(
            "ArtistsAttendance",
            "Artist's Attendance");
        
        public static readonly FeatName GhostlyResonance = ModManager.RegisterFeatName(
            "GhostlyResonance",
            "Ghostly Resonance");

        public static readonly FeatName SongOfGloriousInvocation = ModManager.RegisterFeatName(
            "SongOfGloriousInvocation",
            "Song of Glorious Invocation");
        
        public static readonly FeatName TerrifyingInvocation = ModManager.RegisterFeatName(
            "TerrifyingInvocation",
            "Terrifying Invocation");
        
        public static readonly FeatName TransposeEtching = ModManager.RegisterFeatName(
            "TransposeEtching",
            "Transpose Etching");

        public static readonly FeatName WritingOnTheWall = ModManager.RegisterFeatName(
            "WritingOnTheWall",
            "Writing on the Wall");

        #endregion

        #region 6th-Level

        public static readonly FeatName DiacriticFluency = ModManager.RegisterFeatName(
            "DiacriticFluency",
            "Diacritic Fluency");
        
        public static readonly FeatName EngravingManeuver = ModManager.RegisterFeatName(
            "EngravingManeuver",
            "Engraving Maneuver");
        
        public static readonly FeatName RunicReprisal = ModManager.RegisterFeatName(
            "RunicReprisal",
            "Runic Reprisal");
        
        public static readonly FeatName TracingTrance = ModManager.RegisterFeatName(
            "TracingTrance",
            "Tracing Trance");
        
        public static readonly FeatName VitalCompoundInvocation = ModManager.RegisterFeatName(
            "VitalCompoundInvocation",
            "Vital Compound Invocation");
        
        public static readonly FeatName WordsFlyFree = ModManager.RegisterFeatName(
            "WordsFlyFree",
            "Words, Fly Free");

        #endregion

        #region 8th-Level

        public static readonly FeatName DrawnInVitalInk = ModManager.RegisterFeatName(
            "DrawnInVitalInk",
            "Drawn in Vital Ink");
        
        public static readonly FeatName EdifyingTrace = ModManager.RegisterFeatName(
            "EdifyingTrace",
            "Edifying Trace");
        
        public static readonly FeatName ElementalRevision = ModManager.RegisterFeatName(
            "ElementalRevision",
            "Elemental Revision");
        
        /*public static readonly FeatName ReadTheBones = ModManager.RegisterFeatName(
            "ReadTheBones",
            "Read the Bones");*/
        
        /*public static readonly FeatName EarlyAccess = ModManager.RegisterFeatName(
            "EarlyAccess",
            "Early Access");*/
        
        public static readonly FeatName SwipingTrace = ModManager.RegisterFeatName(
            "SwipingTrace",
            "Swiping Trace");

        #endregion
        
        #region 10th-Level
        
        public static readonly FeatName ChainOfWords = ModManager.RegisterFeatName(
            "ChainOfWords",
            "Chain of Words");
        
        public static readonly FeatName ClashingCompoundInvocation = ModManager.RegisterFeatName(
            "ClashingCompoundInvocation",
            "Clashing Compound Invocation");
        
        public static readonly FeatName OverloadedAmmunition = ModManager.RegisterFeatName(
            "OverloadedAmmunition",
            "Overloaded Ammunition");
        
        public static readonly FeatName ProdigalRuneSinger = ModManager.RegisterFeatName(
            "ProdigalRuneSinger",
            "Prodigal Rune-Singer");
        
        public static readonly FeatName RunicCorrespondence = ModManager.RegisterFeatName(
            "RunicCorrespondence",
            "Runic Correspondence");
        
        #endregion

        #region 12th-Level

        public static readonly FeatName AstralCompoundInvocation = ModManager.RegisterFeatName(
            "AstralCompoundInvocation",
            "Astral Compound Invocation");
        
        public static readonly FeatName DistantInvocation = ModManager.RegisterFeatName(
            "DistantInvocation",
            "Distant Invocation");
        
        public static readonly FeatName ExpandedGlossary = ModManager.RegisterFeatName(
            "ExpandedGlossary",
            "Expanded Glossary");
        
        public static readonly FeatName OrbitingRunestone = ModManager.RegisterFeatName(
            "OrbitingRunestone",
            "Orbiting Runestone");

        #endregion

        #region 14th-Level

        public static readonly FeatName DanceOfBloodyInk = ModManager.RegisterFeatName(
            "DanceOfBloodyInk",
            "Dance of Bloody Ink");
        
        public static readonly FeatName DefineTheCanvas = ModManager.RegisterFeatName(
            "DefineTheCanvas",
            "Define the Canvas");
        
        public static readonly FeatName HengeGate = ModManager.RegisterFeatName(
            "HengeGate",
            "Henge Gate");
        
        public static readonly FeatName UnerringRunicAttraction = ModManager.RegisterFeatName(
            "UnerringRunicAttraction",
            "Unerring Runic Attraction");

        #endregion

        #region 16th-Level

        public static readonly FeatName ByYourName = ModManager.RegisterFeatName(
            "ByYourName",
            "By Your Name");
        
        public static readonly FeatName MazeOfRunes = ModManager.RegisterFeatName(
            "MazeOfRunes",
            "Maze of Runes");
        
        public static readonly FeatName ReturnUntoRunes = ModManager.RegisterFeatName(
            "ReturnUntoRunes",
            "Return unto Runes");
        
        public static readonly FeatName Runesight = ModManager.RegisterFeatName(
            "Runesight",
            "Runesight");

        #endregion

        #region 18th-Level

        public static readonly FeatName AnnihilatingCompoundInvocation = ModManager.RegisterFeatName(
            "AnnihilatingCompoundInvocation",
            "Annihilating Compound Invocation");
        
        public static readonly FeatName LivingLexicon = ModManager.RegisterFeatName(
            "LivingLexicon",
            "Living Lexicon");
        
        public static readonly FeatName UnboundedInvocations = ModManager.RegisterFeatName(
            "UnboundedInvocations",
            "Unbounded Invocations");

        #endregion

        #region 20th-Level

        public static readonly FeatName ForgeNewWord = ModManager.RegisterFeatName(
            "ForgeNewWord",
            "Forge New Word");
        
        public static readonly FeatName GenerationalRuneSinger = ModManager.RegisterFeatName(
            "GenerationalRuneSinger",
            "Generational Rune-Singer");
        
        public static readonly FeatName ShadesOfMeaning = ModManager.RegisterFeatName(
            "ShadesOfMeaning",
            "Shades of Meaning");

        public static readonly FeatName StoneForgeOfTheFirst = ModManager.RegisterFeatName(
            "StoneForgeOfTheFirst",
            "Stone Forge of the First");
        
        #endregion

        #endregion

        #region Multiclass

        public static FeatName RunesmithDedication; // Value assigned later
        public static FeatName BasicRunicMagic; // Value assigned later
        public static FeatName AdvancedRunicMagic; // Value assigned later

        public static readonly FeatName TracingStudies = ModManager.RegisterFeatName(
            "TracingStudies",
            "Tracing Studies");

        public static readonly FeatName ExpandedRepertoire = ModManager.RegisterFeatName(
            "ExpandedRepertoire",
            "Expanded Repertoire");
        
        public static readonly FeatName RunicExpertise = ModManager.RegisterFeatName(
            "RunicExpertise",
            "Runic Expertise");
        
        public static readonly FeatName GreaterExpandedRepertoire = ModManager.RegisterFeatName(
            "GreaterExpandedRepertoire",
            "Greater Expanded Repertoire");

        #endregion
    }

    public static class FeatGroups
    {
        public static readonly FeatGroup Level1Rune = new FeatGroup("Level 1", 0);
        public static readonly FeatGroup Level5Rune = new FeatGroup("Level 5", 1);
        public static readonly FeatGroup Level9Rune = new FeatGroup("Level 9", 2);
        public static readonly FeatGroup Level13Rune = new FeatGroup("Level 13", 3);
        public static readonly FeatGroup Level17Rune = new FeatGroup("Level 17", 4);
    }

    public static class Illustrations
    {
        public const string MOD_FOLDER = "RunesmithClassAssets/";
        
        #region Class Features
        
        public static readonly Illustration TraceRune = new ModdedIllustration(MOD_FOLDER+"trace rune.png");
        public static readonly Illustration InvokeRune = new ModdedIllustration(MOD_FOLDER+"invoke rune.png");
        public static readonly Illustration EtchRune = new ModdedIllustration(MOD_FOLDER+"rune-stone.png");
        //public static readonly Illustration AssuredRunicCrafter = new ModdedIllustration(MOD_FOLDER+"handcraft.png");
        public static readonly Illustration AssuredRunicCrafter = new ModdedIllustration(MOD_FOLDER+"blacksmith.png");
        
        #endregion
        
        #region Feats
        
        public static readonly Illustration RuneWard = new ModdedIllustration(MOD_FOLDER+"shield.png");
        public static readonly Illustration TransposeEtching = new ModdedIllustration(MOD_FOLDER+"hand.png");
        public static readonly Illustration DrawnInVitalInk = new ModdedIllustration(MOD_FOLDER+"knife.png");
        public static readonly Illustration RuneSinger = new ModdedIllustration(MOD_FOLDER+"musical-note.png");
        
        #endregion
        
        #region Items
        
        public static readonly Illustration ArtisansHammer = new ModdedIllustration(MOD_FOLDER+"blacksmith.png");
        
        #endregion
        
        #region Misc
        
        public static readonly Illustration NoSymbol = new ModdedIllustration(MOD_FOLDER+"no symbol.png");
        public static readonly Illustration CheckSymbol = new ModdedIllustration(MOD_FOLDER+"check symbol.png");
        /// <summary>
        /// Used to indicate an information tooltip such as documented changes from tabletop.
        /// </summary>
        public static readonly Illustration InfoSymbol = new ModdedIllustration(MOD_FOLDER+"information_(raised).png");
        public static readonly Illustration DdSun = new ModdedIllustration(MOD_FOLDER+"PatreonSunTransparent.png");
        
        #endregion
    }

    public static class PersistentActions
    {
        public const string RUNESINGER = "RuneSinger";
        public const string RUNIC_TATTOO = "RunicTattoo";
        public static string SUN_DIACRITIC = OncePerCombatRune(RuneId.Sun);

        public static string OncePerCombatRune(RuneId runeId)
        {
            return $"InvokedRune:{runeId.ToWord()}";
        }

        public static bool RuneIsUsedUp(Creature runesmith, RuneId runeId)
        {
            return runesmith.PersistentUsedUpResources.UsedUpActions.Contains(OncePerCombatRune(runeId));
        }

        public static void UseUpRune(Creature runesmith, RuneId runeId)
        {
            runesmith.PersistentUsedUpResources.UsedUpActions.Add(OncePerCombatRune(runeId));
        }
    }
    
    public static class PossibilityGroups
    {
        public const string DRAWING_RUNES = "Draw runes";
        public const string INVOKING_RUNES = "Invoke runes";
    }
    
    public static class PossibilitySectionIds
    {
        public static PossibilitySectionId RuneSinger;
        public static PossibilitySectionId FortifyingKnock;
        public static PossibilitySectionId RunicReprisal;
        
        public static void Initialize()
        {
            RuneSinger = ModManager.SafelyRegisterEnumMember<PossibilitySectionId>("RuneSinger");
            FortifyingKnock = ModManager.SafelyRegisterEnumMember<PossibilitySectionId>("FortifyingKnock");
            RunicReprisal = ModManager.SafelyRegisterEnumMember<PossibilitySectionId>("RunicReprisal");
        }
    }
    
    public static class QEffectIds
    {
        /// <summary>
        /// A creature with this effect is immune to the invocation of the <see cref="Rune"/> stored in its <see cref="QEffect.Tag"/>.
        /// </summary>
        public static QEffectId ImmuneToInvocation;
        public static QEffectId RunicCrafter;
        public static QEffectId RuneSinger;
        public static QEffectId RuneSingerCreator;
        /// The DrawnRune that is tattooed
        public static QEffectId TattooedRune;
        public static QEffectId DrawnInVitalInk;
        public static  QEffectId JurrozDamageTracker;
        
        public static void Initialize()
        {
            ImmuneToInvocation = ModManager.RegisterEnumMember<QEffectId>("ImmuneToInvocation");
            RunicCrafter = ModManager.RegisterEnumMember<QEffectId>("RunicCrafter");
            RuneSinger = ModManager.RegisterEnumMember<QEffectId>("Rune-Singer");
            RuneSingerCreator = ModManager.RegisterEnumMember<QEffectId>("RuneSingerCreator");
            TattooedRune = ModManager.RegisterEnumMember<QEffectId>("TattooedRune");
            DrawnInVitalInk = ModManager.RegisterEnumMember<QEffectId>("DrawnInVitalInk");
            JurrozDamageTracker = ModManager.RegisterEnumMember<QEffectId>("JurrozDamageTracker");
        }
    }

    public static class SfxNames
    {
        // was AncientDust
        public const SfxName TRACE_RUNE = SfxName.Cast4;
        // was DazzlingFlash
        public const SfxName INVOKE_RUNE = SfxName.AuraExpansion;
        public const SfxName ETCH_RUNE = SfxName.AttachRune;
        public const SfxName INVOKED_ATRYL = SfxName.FireRay;
        public const SfxName INVOKED_ESVADIR = SfxName.RayOfFrost;
        public const SfxName INVOKED_MARSSYL_SHOVE = SfxName.Shove;
        public const SfxName INVOKED_OLJINEX = SfxName.Fear;
        public const SfxName INVOKED_PLUUNA = SfxName.MinorAbjuration;
        // SfxName(ElectricBlast == ShockingGrasp)???
        public const SfxName PASSIVE_RANSHU = SfxName.ElectricBlast;
        public const SfxName INVOKED_RANSHU = SfxName.ElectricArc;
        // Was AuraExpansion
        public const SfxName INVOKED_SUN = SfxName.DazzlingFlash;
        public const SfxName INVOKED_ZOHK = SfxName.PhaseBolt;
        public const SfxName INVOKED_FEIKRIS = SfxName.PhaseBolt;
        public const SfxName INVOKED_ICHELSU = SfxName.MinorAbjuration;
        public const SfxName INVOKED_JURROZ = SfxName.AirSpell;
        public const SfxName INVOKED_KOJASTRI = SfxName.BoneSpray;
        public const SfxName INVOKED_TROLISTRI = SfxName.Fear;
        // SfxName.AuraExpansion;
        public const SfxName TOGGLE_RUNE_SINGER = SfxName.OminousActivation;
        public const SfxName SING_RUNE = SfxName.Choir;
        public const SfxName TRANSPOSE_ETCHING_START = SfxName.OminousActivation;
        public const SfxName TRANSPOSE_ETCHING_END = SfxName.GaleBlast;
        public const SfxName WORDS_FLY_FREE = SfxName.AncientDust; // Could be linked to Trace Rune but doesn't have to be.
        public const SfxName ELEMENTAL_REVISION = SfxName.ShieldSpell;
    }

    public static class SubmenuIds
    {
        public static SubmenuId TraceRune;
        public static SubmenuId FortifyingKnock;
        
        public static void Initialize()
        {
            TraceRune = ModManager.SafelyRegisterEnumMember<SubmenuId>("TraceRune");
            FortifyingKnock = ModManager.SafelyRegisterEnumMember<SubmenuId>("FortifyingKnock");
        }
    }
    
    public static class Tooltips
    {
        #region Traits

        public static readonly Func<string, string> TraitRune = RegisterTooltipInserter(
            ID_PREPEND+"Trait.Rune",
            $$"""
              {b}Rune{/b}
              {i}Trait{/i}

              {{CommonRuneRules.TRAIT_DESCRIPTION_RUNE}}
              """,
            true);
        
        public static readonly Func<string, string> TraitInvocation = RegisterTooltipInserter(
            ID_PREPEND+"Trait.Invocation",
            $$"""
              {b}Invocation{/b}
              {i}Trait{/i}

              {{CommonRuneRules.TRAIT_DESCRIPTION_INVOCATION}}
              """,
            true);
        
        public static readonly Func<string, string> TraitDiacritic = RegisterTooltipInserter(
            ID_PREPEND+"Trait.Diacritic",
            $$"""
              {b}Diacritic{/b}
              {i}Trait{/i}

              {{CommonRuneRules.TRAIT_DESCRIPTION_DIACRITIC}}
              """,
            true);

        #endregion

        #region Rule Mechanics

        public static readonly Func<string, string> RuleRuneTradition = RegisterTooltipInserter(
            ID_PREPEND+"Misc.RuneTradition",
            $$"""
              {b}Runesmith Rune Traditions{/b}
              {i}Deviation from tabletop{/i}

              {{CommonRuneRules.TRAIT_DESCRIPTION_RUNE_TRADITION}}
              """,
            true);

        #endregion

        #region Features

        public static readonly Func<string, string> FeatureForgedEndurance = RegisterTooltipInserter(
            ID_PREPEND+"Features.ForgedEndurance",
            """
            {b}Forged Endurance{/b}
            {i}Level 11 Runesmith feature{/i}

            {i}Your body is toughened by long days immersed in crafting, without break for food or drink.{/i}

            Your proficiency rank for Fortitude saves increases to master. When you roll a success on a Fortitude save, you get a critical success instead.
            """);
        
        public static readonly Func<string, string> MiscAllAroundVision = RegisterTooltipInserter(
            ID_PREPEND+"Misc.AllAroundVision",
            """
            {b}All-Around Vision{/b}
            {i}Monster ability{/i}

            This monster can see in all directions simultaneously and therefore can't be flanked.
            """);

        #endregion

        #region Info Tooltips

        public static readonly string InfoEngravingStrikeTarget = RegisterInfoTooltip(
            ID_PREPEND + "EngravingStrikeTarget",
            """
            {b}Engraving Strike {icon:Action}{/b}
            {i}Rules clarification{/i}
            
            The target of the Strike is a creature. Runes which are drawn onto non-creature targets, such as {i}Esvadir{/i} which is drawn onto a weapon or unarmed attack, cannot be used with Engraving Strike.
            """);

        public static readonly string InfoRunicTattooRestrictions = RegisterInfoTooltip(
            ID_PREPEND + "RunicTattooRestrictions",
            """
            {b}Runic Tattoo{/b}
            {i}Runes clarification{/i}
            
            Runic Tattoo applies a rune to your body. This means you can't benefit from runes which are drawn onto items or attacks, limiting your options to runes that are drawn on creatures.
            
            Runes with exclusively harmful passive effects to only the rune-bearer are also excluded.
            """);

        public static readonly string InfoArtistsAttendanceSelfBearer = RegisterInfoTooltip(
            ID_PREPEND + "ArtistsAttendanceSelfBearer",
            """
            {b}Artist's Attendance {icon:TwoActions}{/b}
            {i}Rules interpretation{/i}
            
            If you are bearing one of your runes, then you also qualify as a creature who is bearing one of your runes within your reach, allowing you to Trace a Rune on yourself.
            """);

        public static readonly string InfoTransposeEtchingName = RegisterInfoTooltip(
            ID_PREPEND + "TransposeEtchingName",
            """
            {b}Transpose Etching {icon:Action}{/b}
            {i}Rules clarification{/i}

            Despite the name "Etching", this can be used on traced runes as well.
            """);

        #endregion

        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription, bool wide = false)
        {
            if (wide)
                ModManager.RegisterWideInlineTooltip(tooltipName, tooltipDescription);
            else
                ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }

        /// <summary>
        /// Registers a tooltip as <see cref="RegisterTooltipInserter"/> for use with tooltip info icons.
        /// </summary>
        /// <param name="tooltipName">The registered name of the tooltip.</param>
        /// <param name="tooltipDescription">The body text of the tooltip.</param>
        /// <returns>The tooltip string tag, with a specific arbitrary string: the <see cref="ModData.Illustrations.InfoSymbol"/> illustration.</returns>
        public static string RegisterInfoTooltip(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return "{tooltip:" + tooltipName + "}" + ModData.Illustrations.InfoSymbol.IllustrationAsIconString + "{/}";
        }
    }
    
    public static class Traits
    {
        #region Class
        
        public static readonly Trait Runesmith = ModManager.RegisterTrait("Runesmith", 
            new TraitProperties("Runesmith", true) { IsClassTrait = true });
        
        #endregion
        
        #region Mechanics
        
        public static readonly Trait Rune = ModManager.RegisterTrait("Rune",
            new TraitProperties("Rune", true,
                CommonRuneRules.TRAIT_DESCRIPTION_RUNE,
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A <see cref="DrawnRune"/> with this trait represents a QEffect which has been temporarily traced.</summary>
        public static readonly Trait Traced = ModManager.RegisterTrait("Traced",
            new TraitProperties("Traced", true,
                "A traced rune is drawn lightly in dust, light, or a similar fleeting medium. A runesmith can trace runes with the Trace Rune action, and it remains until the end of their next turn.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith, BackgroundColor = Color.BurlyWood });
        
        /// <summary>A <see cref="DrawnRune"/> with this trait represents a QEffect which has been semi-permanently etched.</summary>
        public static readonly Trait Etched = ModManager.RegisterTrait("Etched",
            new TraitProperties("Etched", true,
                "An etched rune is carved, inked, or branded in. A runesmith's magic can sustain up to 2 etched runes at a time, or more at higher levels. Etched runes remain indefinitely until they're expended or removed.", //"Runes are etched before combat begins."
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith, BackgroundColor = Color.BurlyWood });
        
        /// <summary>A <see cref="DrawnRune"/> with this hidden technical trait represents a QEffect which has been tattooed via the Runic Tattoo feat.</summary>
        public static readonly Trait Tattooed = ModManager.RegisterTrait("Tattooed",
            new TraitProperties("Tattooed", false) { BackgroundColor = Color.BurlyWood });
        
        /// <summary>A <see cref="DrawnRune"/> with this hidden technical trait represents a QEffect which has been traced via the Runic Reprisal feat.</summary>
        public static readonly Trait Reprised = ModManager.RegisterTrait("Reprised",
            new TraitProperties("Reprised", false) { BackgroundColor = Color.BurlyWood });
        
        /// <summary>An action with this trait represents an invocation action. A QEffect with this trait represents the effects of an invocation, as opposed to an effect that is the result of a rune being applied.</summary>
        public static readonly Trait Invocation = ModManager.RegisterTrait("Invocation",
            new TraitProperties("Invocation", true,
                CommonRuneRules.TRAIT_DESCRIPTION_INVOCATION,
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A diacritic rune is a rune that is drawn onto other runes.</summary>
        public static readonly Trait Diacritic = ModManager.RegisterTrait("Diacritic",
            new TraitProperties("Diacritic", true,
                CommonRuneRules.TRAIT_DESCRIPTION_DIACRITIC,
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        #endregion
    }
}