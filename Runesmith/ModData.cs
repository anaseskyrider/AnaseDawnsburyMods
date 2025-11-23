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

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class ModData
{
    public const string IdPrepend = "RunesmithPlaytest.";
    
    public static void LoadData()
    {
        // Runs the initializer, registering the option.
        BooleanOptions.UnrestrictedTrace = BooleanOptions.UnrestrictedTrace;
    }

    public static class ActionIds
    {
        public static readonly ActionId TraceRune = ModManager.RegisterEnumMember<ActionId>("TraceRune");
        public static readonly ActionId EtchRune = ModManager.RegisterEnumMember<ActionId>("EtchRune");
        public static readonly ActionId InvokeRune = ModManager.RegisterEnumMember<ActionId>("InvokeRune");
    }
    
    /// <summary>
    /// "Added the ability for mods to add settings options with <see cref="ModManager.RegisterBooleanSettingsOption(string technicalName, string caption, string longDescription, bool default)"/> for registration API and <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string technicalName)"/> for reading API."
    /// </summary>
    public static class BooleanOptions
    {
        //public const string HideRuneDialogs = "RunesmithPlaytest.HideRuneDialogs"; // Unused, but kept just in case.
        public static string UnrestrictedTrace = RegisterBooleanOption(
            IdPrepend+"UnrestrictedTrace",
            "Runesmith: Less Restrictive Rune Tracing",
            "Enabling this option removes protections against \"bad decisions\" with tracing certain runes on certain targets.\n\nThe Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.\n\nThis option is for those players.",
            true);
        
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
    
    public static class CommonRequirements
    {
        /// <summary>Returns whether the Creature has a hand free for the purposes of tracing runes.</summary>
        public static Usability IsRunesmithHandFree(Creature runesmith)
        {
            return runesmith.HasFreeHand
                   || runesmith.HeldItems.Any(item => item.HasTrait(ModData.Traits.CountsAsRunesmithFreeHand))
                   || runesmith.HasEffect(ModData.QEffectIds.RuneSinger)
                ? Usability.Usable
                : Usability.NotUsable("You must have a free hand to trace a rune");
        }
    }
        
    public static class FeatNames
    {
        #region Class
        public static readonly FeatName RunesmithClass = ModManager.RegisterFeatName(IdPrepend+"RunesmithClass", "Runesmith");
        #endregion

        #region Class Features
        public static readonly FeatName RunesmithRepertoire = ModManager.RegisterFeatName(IdPrepend+"RunesmithRepertoire", "Runic Repertoire");
        
        public static readonly FeatName DedicationRepertoire = ModManager.RegisterFeatName(IdPrepend+"DedicationRepertoire", "Runic Repertoire");

        public static readonly FeatName TraceRune = ModManager.RegisterFeatName(IdPrepend+"TraceRune", "Trace Rune");
        
        public static readonly FeatName InvokeRune = ModManager.RegisterFeatName(IdPrepend+"InvokeRune", "Invoke Rune");
        
        public static readonly FeatName EtchRune = ModManager.RegisterFeatName(IdPrepend+"EtchRune", "Etch Rune");
        
        public static readonly FeatName RunicCrafter = ModManager.RegisterFeatName(IdPrepend+"RunicCrafter", "Runic Crafter");
        
        public static readonly FeatName AssuredRunicCrafter = ModManager.RegisterFeatName(IdPrepend+"AssuredRunicCrafter", "Assured Runic Crafter");
        public static readonly string AssuredRunicCrafterChoice = IdPrepend+"AssuredRunicCrafterChoice";
        #endregion

        #region Class Feats
        public static readonly FeatName BackupRunicEnhancement = ModManager.RegisterFeatName(IdPrepend+"FeatBackupRunicEnhancement", "Backup Runic Enhancement");
        public static readonly FeatName EngravingStrike = ModManager.RegisterFeatName(IdPrepend+"FeatEngravingStrike", "Engraving Strike");
        public static readonly FeatName RemoteDetonation = ModManager.RegisterFeatName(IdPrepend+"FeatRemoteDetonation", "Remote Detonation");
        public static readonly FeatName RuneSinger = ModManager.RegisterFeatName(IdPrepend+"FeatRuneSinger", "Rune-Singer");
        public static readonly FeatName FortifyingKnock = ModManager.RegisterFeatName(IdPrepend+"FeatFortifyingKnock", "Fortifying Knock");
        public static readonly FeatName InvisibleInk = ModManager.RegisterFeatName(IdPrepend+"FeatInvisibleInk", "Invisible Ink");
        public static readonly FeatName RunicTattoo = ModManager.RegisterFeatName(IdPrepend+"FeatRunicTattoo", "Runic Tattoo");
        public static readonly FeatName SmithingWeaponsFamiliarity = ModManager.RegisterFeatName(IdPrepend+"FeatSmithingWeaponsFamiliarity", "Smithing Weapons Familiarity");
        public static readonly FeatName ArtistsAttendance = ModManager.RegisterFeatName(IdPrepend+"FeatArtistsAttendance", "Artist's Attendance");
        public static readonly FeatName GhostlyResonance = ModManager.RegisterFeatName(IdPrepend+"FeatGhostlyResonance", "Ghostly Resonance");
        public static readonly FeatName TerrifyingInvocation = ModManager.RegisterFeatName(IdPrepend+"FeatTerrifyingInvocation", "Terrifying Invocation");
        public static readonly FeatName TransposeEtching = ModManager.RegisterFeatName(IdPrepend+"FeatTransposeEtching", "Transpose Etching");
        public static readonly FeatName RunicReprisal = ModManager.RegisterFeatName(IdPrepend+"FeatRunicReprisal", "Runic Reprisal");
        public static readonly FeatName TracingTrance = ModManager.RegisterFeatName(IdPrepend+"FeatTracingTrance", "Tracing Trance");
        public static readonly FeatName VitalCompositeInvocation = ModManager.RegisterFeatName(IdPrepend+"FeatVitalCompositeInvocation", "Vital Composite Invocation");
        public static readonly FeatName WordsFlyFree = ModManager.RegisterFeatName(IdPrepend+"FeatWordsFlyFree", "Words, Fly Free");
        public static readonly FeatName DrawnInRed = ModManager.RegisterFeatName(IdPrepend+"FeatDrawnInRed", "Drawn In Red");
        public static readonly FeatName ElementalRevision = ModManager.RegisterFeatName(IdPrepend+"FeatElementalRevision", "Elemental Revision");
        public static readonly FeatName ReadTheBones = ModManager.RegisterFeatName(IdPrepend+"FeatReadTheBones", "Read the Bones");
        public static readonly FeatName EarlyAccess = ModManager.RegisterFeatName(IdPrepend+"EarlyAccess", "Early Access");
        #endregion
    }

    public static class FeatGroups
    {
        public static readonly FeatGroup Level1Rune = new FeatGroup("Level 1", 0);
        public static readonly FeatGroup Level9Rune = new FeatGroup("Level 9", 1);
        public static readonly FeatGroup Level17Rune = new FeatGroup("Level 17", 2);
    }

    public static class Illustrations
    {
        #region Class Features
        public static readonly Illustration TraceRune = new ModdedIllustration("RunesmithAssets/trace rune.png");
        public static readonly Illustration InvokeRune = new ModdedIllustration("RunesmithAssets/invoke rune.png");
        public static readonly Illustration EtchRune = new ModdedIllustration("RunesmithAssets/handcraft.png");
        #endregion
        #region Feats
        public static readonly Illustration TransposeEtching = new ModdedIllustration("RunesmithAssets/hand.png");
        public static readonly Illustration DrawnInRed = new ModdedIllustration("RunesmithAssets/knife.png");
        public static readonly Illustration RuneSinger = new ModdedIllustration("RunesmithAssets/musical-note.png");
        #endregion
        #region Items
        public static readonly Illustration ArtisansHammer = new ModdedIllustration("RunesmithAssets/blacksmith.png");
        #endregion
        #region Misc
        public static readonly Illustration NoSymbol = new ModdedIllustration("RunesmithAssets/no symbol.png");
        public static readonly Illustration CheckSymbol = new ModdedIllustration("RunesmithAssets/check symbol.png");
        public static readonly Illustration DawnsburySun = new ModdedIllustration("RunesmithAssets/PatreonSunTransparent.png");
        // TODO: Switch to singular modded illus reference now that it works consistently
        public static readonly string DawnsburySunPath = "RunesmithAssets/PatreonSunTransparent.png";
        #endregion
    }

    public static class PersistentActions
    {
        public const string RunicTattoo = "RunicTattoo";
        public const string ElementalRevision = "ElementalRevision";
        public const string SunDiacritic = "SunDiacritic";
    }
    
    public static class PossibilityGroups
    {
        public const string DrawingRunes = "Draw runes";
        public const string InvokingRunes = "Invoke runes";
    }
    
    public static class PossibilitySectionIds
    {
        public static readonly PossibilitySectionId RuneSinger = ModManager.RegisterEnumMember<PossibilitySectionId>("RuneSinger");
    }
    
    public static class QEffectIds // Technical names are often used directly for the readable name, write accordingly.
    {
        public static readonly QEffectId RuneSinger = ModManager.RegisterEnumMember<QEffectId>("Rune-Singer");
        public static readonly QEffectId RuneSingerCreator = ModManager.RegisterEnumMember<QEffectId>("RuneSingerCreator");
        /// The DrawnRune that is tattooed
        public static readonly QEffectId TattooedRune = ModManager.RegisterEnumMember<QEffectId>("TattooedRune");
        public static readonly QEffectId DrawnInRed = ModManager.RegisterEnumMember<QEffectId>("Drawn in Red");
        public static readonly QEffectId JurrozDamageTracker = ModManager.RegisterEnumMember<QEffectId>("JurrozDamageTracker");
    }

    public static class SfxNames
    {
        public const SfxName TraceRune = SfxName.AncientDust; // TODO: Consider alternative SFX for Trace Rune.
        // SfxName.AuraExpansion;
        public const SfxName InvokeRune = SfxName.DazzlingFlash; // TODO: Consider alternative SFX for Invoke Rune.
        public const SfxName EtchRune = SfxName.AttachRune; // Much more subtle than Trace Rune.
        public const SfxName InvokedAtryl = SfxName.FireRay;
        public const SfxName InvokedEsvadir = SfxName.RayOfFrost;
        public const SfxName InvokedMarssylShove = SfxName.Shove;
        public const SfxName InvokedOljinex = SfxName.Fear;
        public const SfxName InvokedPluuna = SfxName.MinorAbjuration;
        public const SfxName PassiveRanshu = SfxName.ElectricBlast; // SfxName(ElectricBlast == ShockingGrasp)???
        public const SfxName InvokedRanshu = SfxName.ElectricArc;
        public const SfxName InvokedSun = SfxName.AuraExpansion;
        public const SfxName InvokedZohk = SfxName.PhaseBolt;
        public const SfxName InvokedFeikris = SfxName.PhaseBolt;
        public const SfxName InvokedIchelsu = SfxName.MinorAbjuration;
        public const SfxName InvokedJurroz = SfxName.AirSpell;
        public const SfxName InvokedKojastri = SfxName.BoneSpray;
        public const SfxName InvokedTrolistri = SfxName.Fear;
        public const SfxName ToggleRuneSinger = SfxName.OminousActivation; //SfxName.AuraExpansion;
        public const SfxName SingRune = SfxName.Choir;
        public const SfxName TransposeEtchingStart = SfxName.OminousActivation;
        public const SfxName TransposeEtchingEnd = SfxName.GaleBlast;
        public const SfxName WordsFlyFree = SfxName.AncientDust; // Could be linked to Trace Rune but doesn't have to be.
        public const SfxName ElementalRevision = SfxName.ShieldSpell;
    }

    public static class SubmenuIds
    {
        public static readonly SubmenuId TraceRune = ModManager.RegisterEnumMember<SubmenuId>("TraceRune");
    }
    
    public static class Tooltips
    {
        public static readonly Func<string, string> TraitRune = RegisterTooltipInserter(
            IdPrepend+"Trait.Rune",
            "{b}Rune{/b}\n{i}Trait{/i}\nVarious magical effects can be applied through runes, and they're affected by things which also affect spells. Runes can be applied via etching or tracing. Etched runes are applied outside of combat and last indefinitely, while traced runes last only until the end of your next turn. Their effects, however, are the same. Several abilities refer to creatures bearing one of your runes, known as rune-bearers: this is any creature who has one of your runes applied to its body or to any gear it is holding.");
        public static readonly Func<string, string> TraitInvocation = RegisterTooltipInserter(
            IdPrepend+"Trait.Invocation",
            "{b}Invocation{/b}\n{i}Trait{/i}\nAn invocation action allows a runesmith to surge power through a rune by uttering its true name. Invocation requires you to be able to speak clearly in a strong voice and requires that you be within 30 feet of the target rune or runes unless another ability changes this. The target rune then fades away immediately after the action resolves.");
        public static readonly Func<string, string> ActionTraceRune = RegisterTooltipInserter(
            IdPrepend+"Action.TraceRune",
            "{b}Trace Rune {icon:Action}–{icon:TwoActions}{/b}\n{i}Concentrate, Magical, Manipulate{i}\n(Requires a free hand)\nYou apply one rune to an adjacent target matching the rune's Usage description. The rune remains until the end of your next turn. If you spend {icon:TwoActions} two actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.");
        public static readonly Func<string, string> ActionInvokeRune = RegisterTooltipInserter(
            IdPrepend+"Action.InvokeRune",
            "{b}Invoke Rune {icon:Action}{/b}\n{i}Invocation, Magical{i}\nYou utter the name of one or more of your runes within 30 feet. The rune blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed.\n\nYou can invoke any number of runes with a single Invoke Rune action, but creatures that would be affected by multiple copies of the same specific rune are {Red}affected only once{/Red}, as normal for duplicate effects.");
        public static readonly Func<string, string> ActionEtchRune = RegisterTooltipInserter(
            IdPrepend+"Action.EtchRune",
            "{b}Etch Rune{/b}\n{i}Out of combat ability{/i}\nAt the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to 2 runes, and you can etch an additional rune at levels 5, 9, 13, and 17.");
        public static readonly Func<string, string> FeatureTraditionSkill = RegisterTooltipInserter(
            IdPrepend+"Features.TraditionSkill",
            "{b}Traditions of Magic and Skills{/b}\nCertain skills belong to certain traditions of magic. The arcane tradition is related to the arcana skill. The divine tradition is related to the religion skill. The occult tradition is related to the occultism skill. The primal tradition is related to the nature skill.");
        public static readonly Func<string, string> FeatureRunicCrafter = RegisterTooltipInserter(
            IdPrepend+"Features.RunicCrafter",
            "{b}Runic Crafter{/b}\n{i}Level 2 Runesmith feature{/i}\nYour equipment gains the effects of the highest level fundamental armor and weapon runes for your level.");
        public static readonly Func<string, string> FeatureRunicOptimization = RegisterTooltipInserter(
            IdPrepend+"Features.RunicOptimization",
            "{b}Runic Optimization{/b}\n{i}Level 7 Runesmith feature{/i}\nYou deal an additional 2 damage with weapons and unarmed attacks in which you have expert proficiency. This damage increases to 3 if you're a master, and 4 if you're legendary.\n\nAt level 15, you gain Greater Runic Optimization, which increases these bonuses by 2.");
        public static readonly Func<string, string> FeatureAssuredRunicCrafter = RegisterTooltipInserter(
            IdPrepend+"Features.AssuredRunicCrafter",
            "{b}Assured Runic Crafter{/b}\n{i}Level 9 Runesmith feature{/i}\nYou can select one ally to gain the benefits of your Runic Crafter feature as a precombat preparation.");
        public static readonly Func<string, string> FeatureSmithsEndurance = RegisterTooltipInserter(
            IdPrepend+"Features.SmithsEndurance",
            "{b}Smith's Endurance{/b}\n{i}Level 11 Runesmith feature{/i}\nYour proficiency rank for Fortitude saves increases to master. When you roll a success on a Fortitude save, you get a critical success instead.");
        public static readonly Func<string, string> FeatsFortifyingKnock = RegisterTooltipInserter(
            IdPrepend+"Feats.FortifyingKnock",
            "{b}Fortifying Knock {icon:Action}{/b}\n{i}Runesmith{/i}\n(Requires you to wield a shield and have a free hand)\n(Usable once per round)\nIn one motion, you Raise a Shield and Trace a Rune on your shield.");
        public static readonly Func<string, string> FeatsRunicTattoo = RegisterTooltipInserter(
            IdPrepend+"Feats.RunicTattoo",
            "{b}Runic Tattoo{b}\n{i}Runesmith{/i}\nChoose one rune you know, which you apply as a tattoo to your body. The rune is etched at the beginning of combat and doesn't count toward your maximum limit of etched runes. You can invoke this rune like any of your other runes, but once invoked, the rune fades significantly and is drained of power until your next daily preparations.");
        public static readonly Func<string, string> FeatsWordsFlyFree = RegisterTooltipInserter(
            IdPrepend+"Feats.WordsFlyFree",
            "{b}Words, Fly Free {icon:Action}{/b}\n{i}Manipulate, Runesmith{/i}\n(Requires your Runic Tattoo isn't faded)\nYou fling your hand out, the rune from your Runic Tattoo flowing down it and flying through the air in a crescent. You trace the rune onto all creatures or objects within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.");
        public static readonly Func<string, string> MiscAllAroundVision = RegisterTooltipInserter(
            IdPrepend+"Misc.AllAroundVision",
            "{b}All-Around Vision{/b}\n{i}(Monster Ability){/i}\nThis monster can see in all directions simultaneously and therefore can't be flanked.");

        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }
    }
    
    public static class Traits
    {
        #region Class
        public static readonly Trait Runesmith = ModManager.RegisterTrait("Runesmith", 
            new TraitProperties("Runesmith", true) { IsClassTrait = true });
        #endregion
    
        #region Features
        public static readonly Trait RunicRepertoire = ModManager.RegisterTrait("Runic Repertoire",
            new TraitProperties("Runic Repertoire", false));
        #endregion
        
        #region Mechanics
        public static readonly Trait Rune = ModManager.RegisterTrait("Rune",
            new TraitProperties("Rune", true,
                "Various magical effects can be applied through runes, and they're affected by things which also affect spells. Runes can be applied via etching or tracing. Etched runes are applied outside of combat and last indefinitely, while traced runes last only until the end of your next turn. Their effects, however, are the same. Several abilities refer to creatures bearing one of your runes, known as rune-bearers: this is any creature who has one of your runes applied to its body or to any gear it is holding.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A <see cref="DrawnRune"/> with this trait represents a QEffect which has been temporarily traced.</summary>
        public static readonly Trait Traced = ModManager.RegisterTrait("Traced",
            new TraitProperties("Traced", true,
                "A traced rune is drawn lightly in dust, light, or a similar fleeting medium. A runesmith can trace runes with the Trace Rune action, and it remains until the end of their next turn.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A <see cref="DrawnRune"/> with this trait represents a QEffect which has been semi-permanently etched.</summary>
        public static readonly Trait Etched = ModManager.RegisterTrait("Etched",
            new TraitProperties("Etched", true,
                "An etched rune is carved, inked, or branded in. A runesmith's magic can sustain up to 2 etched runes at a time, or more at higher levels. Etched runes remain indefinitely until they're expended or removed.", //"Runes are etched before combat begins."
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A <see cref="DrawnRune"/> with this hidden technical trait represents a QEffect which has been tattooed via the Runic Tattoo feat.</summary>
        public static readonly Trait Tattooed = ModManager.RegisterTrait("Tattooed",
            new TraitProperties("Tattooed", false));
        
        /// <summary>A <see cref="DrawnRune"/> with this hidden technical trait represents a QEffect which has been traced via the Runic Reprisal feat.</summary>
        public static readonly Trait Reprised = ModManager.RegisterTrait("Reprised",
            new TraitProperties("Reprised", false));
        
        /// <summary>An action with this trait represents an invocation action. A QEffect with this trait represents the effects of an invocation, as opposed to an effect that is the result of a rune being applied.</summary>
        public static readonly Trait Invocation = ModManager.RegisterTrait("Invocation",
            new TraitProperties("Invocation", true,
                "An invocation action allows a runesmith to surge power through a rune by uttering its true name. Invocation requires you to be able to speak clearly in a strong voice and requires that you be within 30 feet of the target rune or runes unless another ability changes this. The target rune then fades away immediately after the action resolves.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        // TODO: May have long-term issues. Potentially refactor InvocationBehavior signature and related functionality to use the "Creature target" parameter as an optional "invoke against this guy regardless who the rune-bearer is" functionality.
        /// <summary>When a rune's invocation has an inaccessible subsidiary targeting action, such as Esvadir, then giving this trait to an invocation action will tell that subsidiary which creature to choose automatically.</summary>
        public static readonly Trait InvokeAgainstGivenTarget = ModManager.RegisterTrait("InvokeAgainstGivenTarget",
            new TraitProperties("InvokeAgainstGivenTarget", false)
                { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A technical <see cref="Trait"/> which is used for QEffects representing immunity to a specific rune, used in conjunction with a RuneId trait like <see cref="Atryl"/>.</summary>
        public static readonly Trait InvocationImmunity = ModManager.RegisterTrait("InvocationImmunity",
            new TraitProperties("InvocationImmunity", false) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>A diacritic rune is a rune that is drawn onto other runes.</summary>
        public static readonly Trait Diacritic = ModManager.RegisterTrait("Diacritic",
            new TraitProperties("Diacritic", true,
                "A diacritic is a special type of rune that is not applied directly to a creature or object, but rather to another rune itself, modifying or empowering that base rune. A diacritic can never be applied by itself, and any effect that would remove or invoke the base rune always also removes or invokes the diacritic rune. A rune can have only one diacritic.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>An item with this trait doesn't count as occupying a hand for the purposes of tracing a rune.</summary>
        public static readonly Trait CountsAsRunesmithFreeHand = ModManager.RegisterTrait("CountsAsRunesmithFreeHand",
            new TraitProperties("CountsAsRunesmithFreeHand", false));
        #endregion
        
        #region Items
        public static readonly Trait ArtisansHammer = ModManager.RegisterTrait("ArtisansHammer",
            new TraitProperties("Artisan's Hammer", false)
            { ProficiencyName = "Artisan's Hammer", });
        #endregion
        
        #region Rune Names
        // Rune-specific traits. Every rune is granted a trait unique to its type of instance.
        public static readonly Trait Atryl = ModManager.RegisterTrait("Atryl",
            new TraitProperties("Atryl", false));
        
        public static readonly Trait Esvadir = ModManager.RegisterTrait("Esvadir",
            new TraitProperties("Esvadir", false));
        
        public static readonly Trait Holtrik = ModManager.RegisterTrait("Holtrik",
            new TraitProperties("Holtrik", false));
        
        public static readonly Trait Marssyl = ModManager.RegisterTrait("Marssyl",
            new TraitProperties("Marssyl", false));
        
        public static readonly Trait Oljinex = ModManager.RegisterTrait("Oljinex",
            new TraitProperties("Oljinex", false));
        
        public static readonly Trait Pluuna = ModManager.RegisterTrait("Pluuna",
            new TraitProperties("Pluuna", false));
        
        public static readonly Trait Ranshu = ModManager.RegisterTrait("Ranshu",
            new TraitProperties("Ranshu", false));
        
        public static readonly Trait SunDiacritic = ModManager.RegisterTrait("Sun-",
            new TraitProperties("Sun-", false));
        
        public static readonly Trait UrDiacritic = ModManager.RegisterTrait("Ur-",
            new TraitProperties("Ur-", false));
        
        public static readonly Trait Zohk = ModManager.RegisterTrait("Zohk",
            new TraitProperties("Zohk", false));
        
        // Level 9 rune traits.
        public static readonly Trait EnDiacritic = ModManager.RegisterTrait("En-",
            new TraitProperties("En-", false));
        
        public static readonly Trait Feikris = ModManager.RegisterTrait("Feikris",
            new TraitProperties("Feikris", false));
        
        public static readonly Trait Ichelsu = ModManager.RegisterTrait("Ichelsu",
            new TraitProperties("Ichelsu", false));
        
        public static readonly Trait InthDiacritic = ModManager.RegisterTrait("Inth-",
            new TraitProperties("Inth-", false));
        
        public static readonly Trait Jurroz = ModManager.RegisterTrait("Jurroz",
            new TraitProperties("Jurroz", false));
        
        public static readonly Trait Kojastri = ModManager.RegisterTrait("Kojastri",
            new TraitProperties("Kojastri", false));
        
        public static readonly Trait Trolistri = ModManager.RegisterTrait("Trolistri",
            new TraitProperties("Trolistri", false));
        
        // Level 17 rune traits, for a future update.
        public static readonly Trait Aiuen = ModManager.RegisterTrait("Aiuen",
            new TraitProperties("Aiuen", false));
        
        public static readonly Trait Rovan = ModManager.RegisterTrait("Rovan",
            new TraitProperties("Rovan", false));
        #endregion
    }
}