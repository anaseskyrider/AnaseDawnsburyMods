using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class Enums
{
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
        
        /// <summary>
        /// A <see cref="DrawnRune"/> with this trait represents a QEffect which has been temporarily traced.
        /// </summary>
        public static readonly Trait Traced = ModManager.RegisterTrait("Traced",
            new TraitProperties("Traced", true,
                "A traced rune is drawn lightly in dust, light, or a similar fleeting medium. A runesmith can trace runes with the Trace Rune action, and it remains until the end of their next turn.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>
        /// A <see cref="DrawnRune"/> with this trait represents a QEffect which has been semi-permanently etched.
        /// </summary>
        public static readonly Trait Etched = ModManager.RegisterTrait("Etched",
            new TraitProperties("Etched", true,
                "An etched rune is carved, inked, or branded in. A runesmith's magic can sustain up to 2 etched runes at a time, or more at higher levels. Etched runes remain indefinitely until they're expended or removed.", //"Runes are etched before combat begins."
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>
        /// An action with this trait represents an invocation action. A QEffect with this trait represents the effects of an invocation, as opposed to an effect that is the result of a rune being applied.
        /// </summary>
        public static readonly Trait Invocation = ModManager.RegisterTrait("Invocation",
            new TraitProperties("Invocation", true,
                "An invocation action allows a runesmith to surge power through a rune by uttering its true name. Invocation requires you to be able to speak clearly in a strong voice and requires that you be within 30 feet of the target rune or runes unless another ability changes this. The target rune then fades away immediately after the action resolves.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>
        /// A technical <see cref="Trait"/> which is used for QEffects representing immunity to a specific rune, used in conjunction with a RuneId trait like <see cref="Atryl"/>.
        /// </summary>
        public static readonly Trait InvocationImmunity = ModManager.RegisterTrait("InvocationImmunity",
            new TraitProperties("InvocationImmunity", false) { RelevantOnlyForClass = Runesmith });
        
        public static readonly Trait Diacritic = ModManager.RegisterTrait("Diacritic",
            new TraitProperties("Diacritic", true,
                "A diacritic is a special type of rune that is not applied directly to a creature or object, but rather to another rune itself, modifying or empowering that base rune. A diacritic can never be applied by itself, and any effect that would remove or invoke the base rune always also removes or invokes the diacritic rune. A rune can have only one diacritic.",
                relevantForShortBlock: true) { RelevantOnlyForClass = Runesmith });
        
        /// <summary>
        /// An item with this trait doesn't count as occupying a hand for the purposes of tracing a rune.
        /// </summary>
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
        
        // Level 9 rune traits, for a future update.
        
        // Level 17 rune traits, for a future update.
        #endregion
    }
        
    public static class FeatNames
    {
        #region Class
        public static readonly FeatName RunesmithClass = ModManager.RegisterFeatName("RunesmithPlaytest.RunesmithClass", "Runesmith");
        #endregion

        #region Class Features
        public static readonly FeatName RunicRepertoire = ModManager.RegisterFeatName("RunesmithPlaytest.RunesmithRepertoire", null);

        public static readonly FeatName TraceRune = ModManager.RegisterFeatName("RunesmithPlaytest.TraceRune", "Trace Rune");
        
        public static readonly FeatName InvokeRune = ModManager.RegisterFeatName("RunesmithPlaytest.InvokeRune", "Invoke Rune");
        
        public static readonly FeatName EtchRune = ModManager.RegisterFeatName("RunesmithPlaytest.EtchRune", "Etch Rune");
        
        public static readonly FeatName RunicCrafter = ModManager.RegisterFeatName("RunesmithPlaytest.RunicCrafter", "Runic Crafter");
        #endregion
    }
    
    public static class QEffectIds
    {
        // public static readonly QEffectId PreparedToAid = ModManager.RegisterEnumMember<QEffectId>("Prepared to Aid");
    }

    public static class ActionIds
    {
        public static readonly ActionId TraceRune = ModManager.RegisterEnumMember<ActionId>("TraceRune");
        public static readonly ActionId InvokeRune = ModManager.RegisterEnumMember<ActionId>("InvokeRune");
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
        //public static readonly Illustration DawnsburySun = new ModdedIllustration("RunesmithAssets/PatreonSunTransparent.png");
        public static readonly string DawnsburySunPath = "RunesmithAssets/PatreonSunTransparent.png";
        #endregion
    
    }

    public static class SubmenuIds
    {
        public static readonly SubmenuId TraceRune = ModManager.RegisterEnumMember<SubmenuId>("TraceRune");
    }
    
    public static class PossibilitySectionIds
    {
        // public static readonly PossibilitySectionId AidSkills = ModManager.RegisterEnumMember<PossibilitySectionId>("AidSkills");
        // public static readonly PossibilitySectionId AidAttacks = ModManager.RegisterEnumMember<PossibilitySectionId>("AidAttacks");
    }
}