using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.KholoAncestry;

public static class ModData
{
    public const string IdPrepend = "KholoAncestry.";
    
    public static void LoadData()
    {
        // Register Mod Options
        /*ModManager.RegisterBooleanSettingsOption(ModData.BooleanOptions.UnrestrictedTrace,
            "Runesmith: Less Restrictive Rune Tracing",
            "Enabling this option removes protections against \"bad decisions\" with tracing certain runes on certain targets.\n\nThe Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.\n\nThis option is for those players.",
            true);*/
        
        ItemNames.SpiritThresher = ModManager.RegisterNewItemIntoTheShop(
            "SpiritThresher",
            iName => new Item(
                    iName,
                    Illustrations.SpiritThresher,
                    "spirit thresher",
                    0,
                    2,
                    [ModData.Traits.Kholo, Trait.Advanced, Trait.Flail, Trait.TwoHanded, Trait.Sweep, Trait.VersatileS])
                .WithDescription("{i}Bones, some solid and others splintered, are affixed to metal chains at the end of a long stick to form a powerful flail. Many kholo warriors insist the vicious crack the weapon makes as it strikes loosens fragments of the soul like husks struck from grains.{/i}")
                .WithMainTrait(ModData.Traits.SpiritThresher)
                .WithWeaponProperties(new WeaponProperties("1d12", DamageKind.Bludgeoning)));
        
        ////////////////
        // Action IDs //
        ////////////////
        // Ensures compatibility with other mods registering the same ID, regardless of load order.
        ActionIds.AidReaction = ModManager.TryParse("AidReaction", out ActionId aidReaction)
            ? aidReaction
            : ModManager.RegisterEnumMember<ActionId>("AidReaction");
    }

    public static class ActionIds
    {
        public static ActionId AidReaction;
    }

    public static class FeatNames
    {
        #region Ancestry
        /// <summary>The FeatName of the <see cref="AncestrySelectionFeat"/> corresponding to the Kholo ancestry.</summary>
        public static readonly FeatName KholoAncestry = ModManager.RegisterFeatName(IdPrepend+"KholoAncestry", "Kholo");
        public static readonly FeatName KholoAnt = ModManager.RegisterFeatName(IdPrepend+"KholoAnt", "Ant Kholo");
        public static readonly FeatName KholoCave = ModManager.RegisterFeatName(IdPrepend+"KholoCave", "Cave Kholo");
        public static readonly FeatName KholoDog = ModManager.RegisterFeatName(IdPrepend+"KholoDog", "Dog Kholo");
        public static readonly FeatName KholoGreat = ModManager.RegisterFeatName(IdPrepend+"KholoGreat", "Great Kholo");
        public static readonly FeatName KholoSweetbreath = ModManager.RegisterFeatName(IdPrepend+"KholoSweetbreath", "Sweetbreath Kholo");
        public static readonly FeatName KholoWinter = ModManager.RegisterFeatName(IdPrepend+"KholoWinter", "Winter Kholo");
        public static readonly FeatName KholoWitch = ModManager.RegisterFeatName(IdPrepend+"KholoWitch", "Witch Kholo");
        #endregion
        
        #region Ancestry Features
        public static readonly FeatName EnhancedSenses = ModManager.RegisterFeatName(IdPrepend+"EnhancedSenses", "Enhanced Senses");
        public static readonly FeatName Bite = ModManager.RegisterFeatName(IdPrepend+"Bite", "Bite");
        #endregion
        
        #region Ancestry Feats
        public static readonly FeatName AskTheBones = ModManager.RegisterFeatName(IdPrepend+"AskTheBones", "Ask the Bones");
        public static readonly FeatName Crunch = ModManager.RegisterFeatName(IdPrepend+"Crunch", "Crunch");
        public static readonly FeatName FamiliarScent = ModManager.RegisterFeatName(IdPrepend+"FamiliarScent", "Scent");
        public static readonly FeatName HyenaFamiliar = ModManager.RegisterFeatName(IdPrepend+"HyenaFamiliar", "Hyena Familiar");
        public static readonly FeatName KholoLore = ModManager.RegisterFeatName(IdPrepend+"KholoLore", "Kholo Lore");
        public static readonly FeatName KholoWeaponFamiliarity = ModManager.RegisterFeatName(IdPrepend+"KholoWeaponFamiliarity", "Kholo Weapon Familiarity");
        public static readonly FeatName PackHunter = ModManager.RegisterFeatName(IdPrepend+"PackHunter", "Pack Hunter");
        public static readonly FeatName SensitiveNose = ModManager.RegisterFeatName(IdPrepend+"SensitiveNose", "Sensitive Nose");
        public static readonly FeatName AbsorbStrength = ModManager.RegisterFeatName(IdPrepend+"AbsorbStrength", "Absorb Strength");
        public static readonly FeatName AfflictionResistance = ModManager.RegisterFeatName(IdPrepend+"AfflictionResistance", "Affliction Resistance");
        public static readonly FeatName DistantCackle = ModManager.RegisterFeatName(IdPrepend+"DistantCackle", "Distant Cackle");
        public static readonly FeatName LefthandBlood = ModManager.RegisterFeatName(IdPrepend+"LefthandBlood", "Left-hand Blood");
        public static readonly FeatName PackStalker = ModManager.RegisterFeatName(IdPrepend+"PackStalker", "Pack Stalker");
        public static readonly FeatName RabidSprint = ModManager.RegisterFeatName(IdPrepend+"RabidSprint", "Rabid Sprint");
        public static readonly FeatName RighthandBlood = ModManager.RegisterFeatName(IdPrepend+"RighthandBlood", "Right-hand Blood");
        public static readonly FeatName AmbushHunter = ModManager.RegisterFeatName(IdPrepend+"AmbushHunter", "Ambush Hunter");
        public static readonly FeatName BreathLikeHoney = ModManager.RegisterFeatName(IdPrepend+"BreathLikeHoney", "Breath Like Honey");
        public static readonly FeatName GrandmothersWisdom = ModManager.RegisterFeatName(IdPrepend+"GrandmothersWisdom", "Grandmother's Wisdom");
        public static readonly FeatName LaughingKholo = ModManager.RegisterFeatName(IdPrepend+"LaughingKholo", "Laughing Kholo");
        #endregion
    }

    public static class Illustrations
    {
        public const string ModFolder = "KholoAncestryAssets/";
        
        public static readonly Illustration DawnsburySun = new ModdedIllustration(ModFolder+"PatreonSunTransparent.png");
        public static readonly Illustration SpiritThresher = new ModdedIllustration(ModFolder+"spirit_thresher.png");
        public static readonly Illustration AbsorbStrengthMeat = new ModdedIllustration(ModFolder+"beef.png");
        public static readonly Illustration AbsorbStrengthMeatBigger = new ModdedIllustration(ModFolder+"beef (2).png");
        public static readonly Illustration HyenaFamiliar = new ModdedIllustration(ModFolder+"HyenaFamiliar.png");
    }

    public static class ItemNames
    {
        public static ItemName SpiritThresher;
    }

    public static class PersistentActions
    {
        public const string AskTheBones = IdPrepend+"AskTheBones";
    }

    public static class QEffectIds
    {
        public static readonly QEffectId AbsorbStrengthImmunity = ModManager.RegisterEnumMember<QEffectId>("AbsorbStrengthImmunity");
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> RecallWeakness = RegisterTooltipInserter(
            IdPrepend+"RecallWeakness",
            "{b}Recall Weakness{/b} {icon:Action}\n{i}(Requires the {/i}DawnniExpanded{i} mod){/i}\nYou attempt to recall a weakness of a foe to use to your advantage. Using a skill check that depends on the creature's type, you can give it a penalty to its next saving throw.");

        public static readonly Func<string, string> KholoWeapon = RegisterTooltipInserter(
            IdPrepend+"KholoWeapon",
            "{b}Kholo Weapon{/b}\nA kholo weapon is any weapon with the kholo trait, in addition to the flail, khopesh, mambele, and war flail.");
        
        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }
    }
    
    public static class Traits
    {
        /// <summary>The Trait corresponding to the Kholo ancestry.</summary>
        public static readonly Trait Kholo = ModManager.RegisterTrait("Kholo", 
            new TraitProperties("Kholo", true, "Kholos are hyena-headed humanoids who embrace practicality and pragmatism.")
                { IsAncestryTrait = true });
        
        /// <summary>The <see cref="ItemName.Flail"/> uses <see cref="Trait.Flail"/> as both its main trait and its weapon group, resulting in proficiency adjustment problems. This is added secretly to the Flail weapon to distinguish it from other flail-group weapons.</summary>
        public static readonly Trait FlailItself = ModManager.RegisterTrait(IdPrepend+"FlailItself", 
            new TraitProperties("FlailItself", false));
        
        public static readonly Trait SpiritThresher = ModManager.RegisterTrait("SpiritThresher", 
            new TraitProperties("Spirit Thresher", false));
    }
}