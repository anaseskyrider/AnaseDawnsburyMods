using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.KholoAncestry;

public static class ModData
{
    public const string ID_PREPEND = "KholoAncestry.";

    public static Trait ModTrait;
    
    public static void LoadData()
    {
        ModTrait = ModManager.ModBeingLoadedTrait!.Value; // Known not null at this stage
        ActionIds.Initialize();
        QEffectIds.Initialize();
        TileQfIds.Initialize();
        
        ItemNames.SpiritThresher = ModManager.RegisterNewItemIntoTheShop(
            "SpiritThresher",
            iName => new Item(
                    iName,
                    Illustrations.SpiritThresher,
                    "spirit thresher",
                    0,
                    2,
                    [ModData.ModTrait, ModData.Traits.Kholo, Trait.Advanced, Trait.Flail, Trait.TwoHanded, Trait.Sweep, Trait.VersatileS])
                .WithDescription("{i}Bones, some solid and others splintered, are affixed to metal chains at the end of a long stick to form a powerful flail. Many kholo warriors insist the vicious crack the weapon makes as it strikes loosens fragments of the soul like husks struck from grains.{/i}")
                .WithMainTrait(ModData.Traits.SpiritThresher)
                .WithWeaponProperties(new WeaponProperties("1d12", DamageKind.Bludgeoning)));
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

    public static class ActionIds
    {
        public static ActionId AidReaction;
        
        public static void Initialize()
        {
            AidReaction = SafelyRegister<ActionId>("AidReaction");
        }
    }

    public static class FeatNames
    {
        #region Ancestry
        /// <summary>The FeatName of the <see cref="AncestrySelectionFeat"/> corresponding to the Kholo ancestry.</summary>
        public static readonly FeatName KholoAncestry = ModManager.RegisterFeatName(ID_PREPEND+"KholoAncestry", "Kholo");
        public static readonly FeatName KholoAnt = ModManager.RegisterFeatName(ID_PREPEND+"KholoAnt", "Ant Kholo");
        public static readonly FeatName KholoCave = ModManager.RegisterFeatName(ID_PREPEND+"KholoCave", "Cave Kholo");
        public static readonly FeatName KholoDog = ModManager.RegisterFeatName(ID_PREPEND+"KholoDog", "Dog Kholo");
        public static readonly FeatName KholoGreat = ModManager.RegisterFeatName(ID_PREPEND+"KholoGreat", "Great Kholo");
        public static readonly FeatName KholoSweetbreath = ModManager.RegisterFeatName(ID_PREPEND+"KholoSweetbreath", "Sweetbreath Kholo");
        public static readonly FeatName KholoWinter = ModManager.RegisterFeatName(ID_PREPEND+"KholoWinter", "Winter Kholo");
        public static readonly FeatName KholoWitch = ModManager.RegisterFeatName(ID_PREPEND+"KholoWitch", "Witch Kholo");
        #endregion
        
        #region Ancestry Features
        public static readonly FeatName EnhancedSenses = ModManager.RegisterFeatName(ID_PREPEND+"EnhancedSenses", "Enhanced Senses");
        public static readonly FeatName Bite = ModManager.RegisterFeatName(ID_PREPEND+"Bite", "Bite");
        #endregion
        
        #region Ancestry Feats
        public static readonly FeatName AskTheBones = ModManager.RegisterFeatName(ID_PREPEND+"AskTheBones", "Ask the Bones");
        public static readonly FeatName Crunch = ModManager.RegisterFeatName(ID_PREPEND+"Crunch", "Crunch");
        public static readonly FeatName FamiliarScent = ModManager.RegisterFeatName(ID_PREPEND+"FamiliarScent", "Scent");
        public static readonly FeatName HyenaFamiliar = ModManager.RegisterFeatName(ID_PREPEND+"HyenaFamiliar", "Hyena Familiar");
        public static readonly FeatName KholoLore = ModManager.RegisterFeatName(ID_PREPEND+"KholoLore", "Kholo Lore");
        public static readonly FeatName KholoWeaponFamiliarity = ModManager.RegisterFeatName(ID_PREPEND+"KholoWeaponFamiliarity", "Kholo Weapon Familiarity");
        public static readonly FeatName PackHunter = ModManager.RegisterFeatName(ID_PREPEND+"PackHunter", "Pack Hunter");
        public static readonly FeatName SensitiveNose = ModManager.RegisterFeatName(ID_PREPEND+"SensitiveNose", "Sensitive Nose");
        public static readonly FeatName AbsorbStrength = ModManager.RegisterFeatName(ID_PREPEND+"AbsorbStrength", "Absorb Strength");
        public static readonly FeatName AfflictionResistance = ModManager.RegisterFeatName(ID_PREPEND+"AfflictionResistance", "Affliction Resistance");
        public static readonly FeatName DistantCackle = ModManager.RegisterFeatName(ID_PREPEND+"DistantCackle", "Distant Cackle");
        public static readonly FeatName LefthandBlood = ModManager.RegisterFeatName(ID_PREPEND+"LefthandBlood", "Left-hand Blood");
        public static readonly FeatName PackStalker = ModManager.RegisterFeatName(ID_PREPEND+"PackStalker", "Pack Stalker");
        public static readonly FeatName RabidSprint = ModManager.RegisterFeatName(ID_PREPEND+"RabidSprint", "Rabid Sprint");
        public static readonly FeatName RighthandBlood = ModManager.RegisterFeatName(ID_PREPEND+"RighthandBlood", "Right-hand Blood");
        public static readonly FeatName AmbushHunter = ModManager.RegisterFeatName(ID_PREPEND+"AmbushHunter", "Ambush Hunter");
        public static readonly FeatName BreathLikeHoney = ModManager.RegisterFeatName(ID_PREPEND+"BreathLikeHoney", "Breath Like Honey");
        public static readonly FeatName GrandmothersWisdom = ModManager.RegisterFeatName(ID_PREPEND+"GrandmothersWisdom", "Grandmother's Wisdom");
        public static readonly FeatName LaughingKholo = ModManager.RegisterFeatName(ID_PREPEND+"LaughingKholo", "Laughing Kholo");
        public static readonly FeatName AncestorsRage = ModManager.RegisterFeatName(ID_PREPEND+"AncestorsRage", "Ancestor's Rage");
        public static readonly FeatName BonekeepersBane = ModManager.RegisterFeatName(ID_PREPEND+"BonekeepersBane", "Bonekeeper's Bane");
        public static readonly FeatName FirstToStrikeFirstToFall = ModManager.RegisterFeatName(ID_PREPEND+"FirstToStrikeFirstToFall", "First to Strike, First to Fall");
        public static readonly FeatName ImpalingBone = ModManager.RegisterFeatName(ID_PREPEND+"ImpalingBone", "Impaling Bone");
        public static readonly FeatName LegendaryLaugh = ModManager.RegisterFeatName(ID_PREPEND+"LegendaryLaugh", "Legendary Laugh");
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
        public const string AskTheBones = ID_PREPEND+"AskTheBones";
    }

    public static class QEffectIds
    {
        public static QEffectId AbsorbStrengthImmunity;
        public static QEffectId BonekeepersBaneStartOfTurn;
        public static QEffectId FirstToFall;
        
        public static void Initialize()
        {
            AbsorbStrengthImmunity = SafelyRegister<QEffectId>("AbsorbStrengthImmunity");
            BonekeepersBaneStartOfTurn = SafelyRegister<QEffectId>("BonekeepersBaneStartOfTurn");
            FirstToFall = SafelyRegister<QEffectId>("FirstToFall");
        }
    }

    public static class TileQfIds
    {
        public static TileQEffectId AbsorbStrengthCorpse;
        
        internal static void Initialize()
        {
            AbsorbStrengthCorpse = ModManager.SafelyRegisterEnumMember<TileQEffectId>("AbsorbStrengthCorpse");
        }
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> CommonWeaponFamiliarity = RegisterTooltipInserter(
            ID_PREPEND + "Common.WeaponFamiliarity",
            """
            {b}Weapon Familiarity{/b}
            {i}Common mechanic{/i}
            If you have familiarity with a group of weapons, that has the following effects:
            • You treat any martial weapons as if they were simple weapons.
            • You treat any advanced weapons as if they were martial weapons.
            
            Such features usually allow you to trigger critical specialization effects under certain conditions, such as reaching 5th-level or being an expert with the weapon.
            """);
        
        public static readonly Func<string, string> KholoWeapon = RegisterTooltipInserter(
            ID_PREPEND+"KholoWeapon",
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
        
        /// <summary>
        /// Trait from Deployable Familiars. Makes the feat into a deployable familiar feat.
        /// </summary>
        public static readonly Trait DeployableFamiliarFeat = ModManager.RegisterTrait("DeployableFamiliarFeat", new TraitProperties("Deployable Familiar Feat", false));
        
        /// <summary>The <see cref="ItemName.Flail"/> uses <see cref="Trait.Flail"/> as both its main trait and its weapon group, resulting in proficiency adjustment problems. This is added secretly to the Flail weapon to distinguish it from other flail-group weapons.</summary>
        public static readonly Trait FlailItself = ModManager.RegisterTrait(ID_PREPEND+"FlailItself", 
            new TraitProperties("FlailItself", false));
        
        public static readonly Trait SpiritThresher = ModManager.RegisterTrait("SpiritThresher", 
            new TraitProperties("Spirit Thresher", false));
    }
}