using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;

public static class ModData
{
    public const string ID_PREPEND = "SlayerClass.";

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
        ItemGreaterGroups.Initialize();
        QEffectIds.Initialize();
        RuneKinds.Initialize();
    }

    public static class ActionIds
    {
        public static ActionId OnTheHunt;
        public static ActionId MarkQuarry;
        public static ActionId ArmoredShelter;
        public static ActionId HuntingSpike;
        
        public static void Initialize()
        {
            OnTheHunt = ModManager.SafelyRegisterEnumMember<ActionId>("OnTheHunt");
            MarkQuarry = ModManager.SafelyRegisterEnumMember<ActionId>("MarkQuarry");
            ArmoredShelter = ModManager.SafelyRegisterEnumMember<ActionId>("ArmoredShelter");
            HuntingSpike = ModManager.SafelyRegisterEnumMember<ActionId>("HuntingSpike");
        }
    }

    public static class CommonQfKeys
    {
        /// <summary>This key indicates a Quickened condition from the On the Hunt reaction.</summary>
        public const string ON_THE_HUNT = "OnTheHunt";
    }

    public static class FeatNames
    {
        #region Class
        
        public static readonly FeatName SlayerClass = ModManager.RegisterFeatName(ID_PREPEND+"SlayerClass", "Slayer");
        
        #endregion
        
        #region Class Features
        
        public static readonly FeatName OnTheHunt = ModManager.RegisterFeatName(ID_PREPEND+"OnTheHunt", "On the Hunt {icon:Reaction}");
        public static readonly FeatName MonsterLore = ModManager.RegisterFeatName(ID_PREPEND+"MonsterLore", "Monster Lore");
        public static readonly FeatName SlayersArsenal = ModManager.RegisterFeatName(ID_PREPEND+"SlayersArsenal", "Slayer's Arsenal");
        /// <summary>
        /// The Mark Quarry sub-feature of Slayer's Quarry
        /// </summary>
        public static readonly FeatName MarkQuarry = ModManager.RegisterFeatName(ID_PREPEND+"MarkQuarry", "Mark Quarry {icon:FreeAction}");
        /// <summary>
        /// The Claim Trophy sub-feature of Slayer's Quarry
        /// </summary>
        public static readonly FeatName ClaimTrophy = ModManager.RegisterFeatName(ID_PREPEND+"ClaimTrophy", "Claim Trophy");
        
        #endregion
        
        #region Class Feats

        #region 1st-Level

        public static readonly FeatName Bloodscent = ModManager.RegisterFeatName(ID_PREPEND+"Bloodscent", "Bloodscent");
        public static readonly FeatName CrossbowSlayer = ModManager.RegisterFeatName(ID_PREPEND+"CrossbowSlayer", "Crossbow Slayer");
        public static readonly FeatName DrinkAdaptationSerums = ModManager.RegisterFeatName(ID_PREPEND+"DrinkAdaptationSerums", "Drink Adaptation Serums");
        public static readonly FeatName RepellingShield = ModManager.RegisterFeatName(ID_PREPEND+"RepellingShield", "Repelling Shield");
        public static readonly FeatName SpikedSurcoat = ModManager.RegisterFeatName(ID_PREPEND+"SpikedSurcoat", "Spiked Surcoat");
        public static readonly FeatName SuddenPounce = ModManager.RegisterFeatName(ID_PREPEND+"SuddenPounce", "Sudden Pounce");
        public static readonly FeatName PairedBloodseeker = ModManager.RegisterFeatName(ID_PREPEND+"PairedBloodseeker", "Paired Bloodseeker");
        public static readonly FeatName PeculiarWeaponry = ModManager.RegisterFeatName(ID_PREPEND+"PeculiarWeaponry", "Peculiar Weaponry");

        #endregion

        #region 2nd-Level

        public static readonly FeatName InstantEnmity = ModManager.RegisterFeatName(ID_PREPEND+"InstantEnmity", "Instant Enmity");
        public static readonly FeatName PackSlayer = ModManager.RegisterFeatName(ID_PREPEND+"PackSlayer", "Pack Slayer");
        public static readonly FeatName PersonalizedGear = ModManager.RegisterFeatName(ID_PREPEND+"PersonalizedGear", "Personalized Gear");
        public static readonly FeatName SaltStone = ModManager.RegisterFeatName(ID_PREPEND+"SaltStone", "Salt Stone");
        public static readonly FeatName ShiftingHunt = ModManager.RegisterFeatName(ID_PREPEND+"ShiftingHunt", "Shifting Hunt");
        public static readonly FeatName SlayersTricks = ModManager.RegisterFeatName(ID_PREPEND+"SlayersTricks", "Slayer's Tricks");

        #endregion

        #region 4th-Level

        public static readonly FeatName ApplySpiritOil = ModManager.RegisterFeatName(ID_PREPEND+"ApplySpiritOil", "Apply Spirit Oil");
        public static readonly FeatName BloodForBlood = ModManager.RegisterFeatName(ID_PREPEND+"BloodForBlood", "Blood for Blood");
        public static readonly FeatName BloodRush = ModManager.RegisterFeatName(ID_PREPEND+"BloodRush", "Blood Rush");
        public static readonly FeatName Cureall = ModManager.RegisterFeatName(ID_PREPEND+"Cureall", "Cure-all");
        public static readonly FeatName ExpansivePanoply = ModManager.RegisterFeatName(ID_PREPEND+"ExpansivePanoply", "Expansive Panoply");

        #endregion

        #region 6th-Level

        public static readonly FeatName FinalFlourish = ModManager.RegisterFeatName(ID_PREPEND+"FinalFlourish", "Final Flourish");
        public static readonly FeatName RelentlessCounterstrike = ModManager.RegisterFeatName(ID_PREPEND+"RelentlessCounterstrike", "Relentless Counterstrike");
        public static readonly FeatName ShiftingCombination = ModManager.RegisterFeatName(ID_PREPEND+"ShiftingCombination", "Shifting Combination");
        public static readonly FeatName SpellSlates = ModManager.RegisterFeatName(ID_PREPEND+"SpellSlates", "Spell Slates");
        public static readonly FeatName WallOfWill = ModManager.RegisterFeatName(ID_PREPEND+"WallOfWill", "Wall of Will");

        #endregion

        #region 8th-Level

        public static readonly FeatName ArmoredFortress = ModManager.RegisterFeatName(ID_PREPEND+"ArmoredFortress", "Armored Fortress");
        public static readonly FeatName CatalyzingFlask = ModManager.RegisterFeatName(ID_PREPEND+"CatalyzingFlask", "Catalyzing Flask");
        public static readonly FeatName DefensiveHunt = ModManager.RegisterFeatName(ID_PREPEND+"DefensiveHunt", "Defensive Hunt");
        public static readonly FeatName FieldForgedTools = ModManager.RegisterFeatName(ID_PREPEND+"FieldForgedTools", "Field-forged Tools");

        #endregion

        #region 10th-Level

        public static readonly FeatName EagerHunter = ModManager.RegisterFeatName(ID_PREPEND+"EagerHunter", "Eager Hunter");
        public static readonly FeatName EndlessEnmity = ModManager.RegisterFeatName(ID_PREPEND+"EndlessEnmity", "Endless Enmity");
        public static readonly FeatName EverVigilant = ModManager.RegisterFeatName(ID_PREPEND+"EverVigilant", "Ever Vigilant");
        public static readonly FeatName ShareInsight = ModManager.RegisterFeatName(ID_PREPEND+"ShareInsight", "Share Insight");

        #endregion

        #region 12th-Level

        public static readonly FeatName DoubleQuarry = ModManager.RegisterFeatName(ID_PREPEND+"DoubleQuarry", "Double Quarry");
        public static readonly FeatName ExpandedSpellSlates = ModManager.RegisterFeatName(ID_PREPEND+"ExpandedSpellSlates", "Expanded Spell Slates");
        public static readonly FeatName GougingStrike = ModManager.RegisterFeatName(ID_PREPEND+"GougingStrike", "Gouging Strike");
        public static readonly FeatName SpectralLenses = ModManager.RegisterFeatName(ID_PREPEND+"SpectralLenses", "Spectral Lenses");

        #endregion

        #region 14th-Level

        public static readonly FeatName ArmBloodburstPhial = ModManager.RegisterFeatName(ID_PREPEND+"ArmBloodburstPhial", "Arm Bloodburst Phial");
        public static readonly FeatName OpenWound = ModManager.RegisterFeatName(ID_PREPEND+"OpenWound", "Open Wound");

        #endregion

        #region 16th-Level

        public static readonly FeatName ImpenetrableShelter = ModManager.RegisterFeatName(ID_PREPEND+"ImpenetrableShelter", "Impenetrable Shelter");
        public static readonly FeatName InfernoVial = ModManager.RegisterFeatName(ID_PREPEND+"InfernoVial", "Inferno Vial");
        public static readonly FeatName UnerringEdge = ModManager.RegisterFeatName(ID_PREPEND+"UnerringEdge", "Unerring Edge");
        public static readonly FeatName ViciousSpike = ModManager.RegisterFeatName(ID_PREPEND+"ViciousSpike", "Vicious Spike");

        #endregion

        #region 18th-Level

        public static readonly FeatName Obliterate = ModManager.RegisterFeatName(ID_PREPEND+"Obliterate", "Obliterate");
        public static readonly FeatName TerrifyingBloodlust = ModManager.RegisterFeatName(ID_PREPEND+"TerrifyingBloodlust", "Terrifying Bloodlust");

        #endregion

        #region 20th-Level

        public static readonly FeatName EternalHunt = ModManager.RegisterFeatName(ID_PREPEND+"EternalHunt", "Eternal Hunt");
        public static readonly FeatName UnboundHunt = ModManager.RegisterFeatName(ID_PREPEND+"UnboundHunt", "Unbound Hunt");

        #endregion
        
        #endregion
    }
 
    public static class Illustrations
    {
        public const string MOD_FOLDER = "SlayerClassAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(MOD_FOLDER + "PatreonSunTransparent.png");

        #region Hunting Tools

        public static readonly Illustration BloodseekingBlade = new ModdedIllustration(MOD_FOLDER + "dripping-blade.png");
        public static readonly Illustration ChymistsVials = new ModdedIllustration(MOD_FOLDER + "test-tube-rack.png");
        public static readonly Illustration ConsecratedPanoply = new ModdedIllustration(MOD_FOLDER + "gothic-cross.png");
        public static readonly Illustration HuntingSpike = new ModdedIllustration(MOD_FOLDER + "bone-knife.png");
        public static readonly Illustration WardedMail = new ModdedIllustration(MOD_FOLDER + "heart-armor.png");

        public static readonly Illustration RepellingShield = IllustrationName.ShieldSpell;

        #endregion

        #region Trophies

        public static readonly Illustration TrophyCase = IllustrationName.BagOfHolding1;
        public static readonly Illustration Trophy = IllustrationName.Trophy;

        #endregion

        #region Class Features

        public static readonly Illustration OnTheHunt = IllustrationName.HuntPrey;
        public static readonly Illustration MarkQuarry = IllustrationName.HuntPrey;

        #endregion
        
        #region Class Feats

        public static readonly Illustration InstantEnmity = IllustrationName.Rage;

        #endregion
    }

    public static class ItemGreaterGroups
    {
        public static ItemGreaterGroup ClassItems;
        
        public static void Initialize()
        {
            ClassItems = ModManager.SafelyRegisterEnumMember<ItemGreaterGroup>("Class Items");
        }
    }

    public static class PersistentActions
    {
        public const string INSTANT_ENMITY = "InstantEnmity";
    }

    public static class QEffectIds
    {
        public static QEffectId MarkedQuarry;
        public static QEffectId ArmoredShelter;
        public static QEffectId CrossbowSlayer;
        
        public static void Initialize()
        {
            MarkedQuarry = ModManager.SafelyRegisterEnumMember<QEffectId>("MarkedQuarry");
            ArmoredShelter = ModManager.SafelyRegisterEnumMember<QEffectId>("ArmoredShelter");
            CrossbowSlayer = ModManager.SafelyRegisterEnumMember<QEffectId>("CrossbowSlayer");
        }
    }

    public static class RuneKinds
    {
        public static RuneKind SlayerTrophy;
        
        public static void Initialize()
        {
            SlayerTrophy = ModManager.SafelyRegisterEnumMember<RuneKind>("SlayerTrophy");
        }
    }

    public static class SfxNames
    {
        public const SfxName MarkQuarry = SfxName.Hide;
        public const SfxName OnTheHunt = SfxName.Throw;
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> Relentless = RegisterTooltipInserter(
            ID_PREPEND + "Relentless",
            """
            {b}Relentless{/b}
            {i}Trait — slayer mechanic{i}
            Actions with the relentless trait are special techniques that slayers have trained to use on instinct.
            
            The quickened action you get from On the Hunt {icon:Reaction} can be used for any action with the relentless trait, including to supply 1 action to a 2+ action activity.
            """
            );
        
        public static readonly Func<string, string> Trophy = RegisterTooltipInserter(
            ID_PREPEND + "Trophy",
            """
            {b}Trophy{/b}
            {i}Slayer mechanic{/i}
            Trophies are items collected by a slayer when their marked quarry is defeated, and then attached to a hunting tool. They contain properties based on the creature that was slain, but the effects are determined by the Reinforced benefits entry of the hunting tool it's attached to.
            
            You can attach a trophy to a hunting tool by clicking-and-dragging it onto one.
            
            Trophies have the following properties:
            • {b}Traits{/b} It has all your quarry's traits, except rarity and size.
            • {b}Damage Types{/b} It has any damage types that your quarry could deal with its Strikes or non-spellcasting abilities, or it had immunity to.
            • {b}Traditions{/b} It's associated with your quarry's tradition trait and that of any spells it could cast. Its tradition is occult if it had neither.
            """);
        
        public static readonly Func<string, string> HuntingTool = RegisterTooltipInserter(
            ID_PREPEND + "HuntingTool",
            $$"""
            {b}Hunting Tool{/b}
            {i}Slayer mechanic{/i}
            Hunting tools are special adjustments designated to a single item you possess, empowering that item with additional features. You can do so by right-clicking an appropriate item in your inventory, such as a weapon for your bloodseeking blade, or armor for your warded mail, to designate that item as one of your hunting tools.
            
            A hunting tool can also be Reinforced by attaching a trophy to them, granting additional benefits based on the trophy's properties as described by that tool's Reinforced benefits.
            
            For items without an equivalent in {{ModData.Illustrations.DdSun.IllustrationAsIconString}} Dawnsbury Days, a unique item is provided for you so that you can attach trophies to them.
            """);
        
        public static readonly Func<string, string> ChymistPronunciation = RegisterTooltipInserter(
            ID_PREPEND + "ChymistPronunciation",
            """
            {b}Chymist{/b}
            {i}Etymology{/i}
            Archaic spelling of "chemist". Pronounced {i}KEM-ist{i}, sometimes {i}KIM-ist{/i}.
            """);
        
        public static readonly Func<string, string> ReinforcedBenefit = RegisterTooltipInserter(
            ID_PREPEND+"Reinforced",
            """
            {b}Reinforced{/b}
            {i}Slayer mechanic{/i}
            You gain this benefit when your hunting tool is reinforced with a trophy.
            """);

        public static readonly Func<string, string> TipOfTheTongue = RegisterTooltipInserter(
            ID_PREPEND+"TipOfTheTongue",
            """
            {b}Tip of the Tongue{/b}
            {i}Level 5 Slayer feature{/i}
            Your encyclopedic knowledge of monsters allows you to quickly recall basic information. You gain the Assurance and Automatic Knowledge skill feats for Monster Lore.
            """);
        
        public static readonly Func<string, string> SpecializedArsenal = RegisterTooltipInserter(
            ID_PREPEND+"SpecializedArsenal",
            """
            {b}Specialized Arsenal{/b}
            {i}Level 7 Slayer feature{/i}
            You gain the specialized arsenal benefit of your signature tool.
            """);
        
        public static readonly Func<string, string> PersistentFocus = RegisterTooltipInserter(
            ID_PREPEND+"PersistentFocus",
            """
            {b}Persistent Focus{/b}
            {i}Level 9 Slayer feature{/i}
            Your proficiency rank for Will saves increases to master; when you roll a success on a Will save, you get a critical success instead.
            """);
        
        public static readonly Func<string, string> ExpandedArsenal = RegisterTooltipInserter(
            ID_PREPEND+"ExpandedArsenal",
            """
            {b}Expanded Arsenal{/b}
            {i}Level 11 Slayer feature{/i}
            Choose a second signature tool. You gain its normal benefits, but not its specialized arsenal benefit.
            """);
        
        public static readonly Func<string, string> NaturalResilience = RegisterTooltipInserter(
            ID_PREPEND+"NaturalResilience",
            """
            {b}Natural Resilience{/b}
            {i}Level 11 Slayer feature{/i}
            Your proficiency rank for Fortitude saves increases to master; when you roll a success on a Fortitude save, you get a critical success instead.
            """);
        
        public static readonly Func<string, string> GreaterPersistentFocus = RegisterTooltipInserter(
            ID_PREPEND+"GreaterPersistentFocus",
            """
            {b}Greater Persistent Focus{/b}
            {i}Level 15 Slayer feature{/i}
            You become legendary in Will saves; when you roll a critical failure on a Will save, you get a failure instead instead; and when you roll a natural failure on a Will save against a damaging effect, you take half damage only.
            """);
        
        public static readonly Func<string, string> GreaterSpecializedArsenal = RegisterTooltipInserter(
            ID_PREPEND+"GreaterSpecializedArsenal",
            """
            {b}Greater Specialized Arsenal{/b}
            {i}Level 15 Slayer feature{/i}
            You gain the specialized arsenal benefit of your second signature tool.
            """);
        
        public static readonly Func<string, string> CommonWeaponSpec = RegisterTooltipInserter(
            ID_PREPEND + "Common.WeaponSpecialization",
            """
            {b}Weapon Specialization{/b}
            {i}Common class feature{/i}
            You deal 2 additional damage with weapons and unarmed attacks in which you are an expert; this damage increases to 3 if you're a master, and to 4 if you're legendary.
            """);
         
        public static readonly Func<string, string> CommonGreaterWeaponSpec = RegisterTooltipInserter(
            ID_PREPEND + "Common.GreaterWeaponSpecialization",
            """
            {b}Greater Weapon Specialization{/b}
            {i}Common class feature{/i}
            Your damage from weapon specialization increases to 4 with weapons and unarmed attacks in which you're expert, 6 if you're a master, and 8 if you're legendary.
            """);
         
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
 
    public static class Traits
    {
        #region Class
        
        public static readonly Trait Slayer = ModManager.RegisterTrait("Slayer", new TraitProperties("Slayer", true) { IsClassTrait = true });
         
        #endregion
    
        #region Features
        
        public static readonly Trait Relentless = ModManager.RegisterTrait(
            "Relentless",
            new TraitProperties(
                "Relentless",
                true,
                """
                Actions with the relentless trait are special techniques that slayers have trained to use on instinct.

                The quickened action you get from On the Hunt {icon:Reaction} can be used for any action with the relentless trait, including to supply 1 action to a 2+ action activity.
                """));

        public static readonly Trait Trophy = ModManager.RegisterTrait(
            "Trophy",
            new TraitProperties(
                "Trophy",
                true,
                """
                Trophies are items collected by a slayer when their marked quarry is defeated, and then attached to a hunting tool. They contain properties based on the creature that was slain, but the effects are determined by the Reinforced benefits entry of the hunting tool it's attached to.
                
                You can attach a trophy to a hunting tool by clicking-and-dragging it onto one.
                
                Trophies have the following properties:
                • {b}Traits{/b} It has all your quarry's traits, except rarity and size.
                • {b}Damage Types{/b} It has any damage types that your quarry could deal with its Strikes or non-spellcasting abilities, or it had immunity to.
                • {b}Traditions{/b} It's associated with your quarry's tradition trait and that of any spells it could cast. Its tradition is occult if it had neither.
                """));

        public static readonly Trait HuntingTool = ModManager.RegisterTrait(
            "HuntingTool", new TraitProperties("Hunting Tool", false));

        #endregion

        #region Technicals

        /// <summary>
        /// If a <see cref="QEffect"/> with the Id <see cref="QEffectIds.MarkedQuarry"/> has this trait, then it will not generate a trophy on death.
        /// </summary>
        public static readonly Trait DoNotClaimTrophy = ModManager.RegisterTrait("DoNotClaimTrophy", new TraitProperties("DoNotClaimTrophy", false));
        
        public static readonly Trait BloodseekingBlade = ModManager.RegisterTrait("Bloodseeking Blade", new TraitProperties("Bloodseeking Blade", true));
        
        /// <summary>
        /// This trait is for the feats which represent your choice of a rune for your bloodseeking blade's specialized arsenal feature.
        /// </summary>
        public static readonly Trait BloodseekingBladePropertyRune = ModManager.RegisterTrait("BloodseekingBladePropertyRune", new TraitProperties("BloodseekingBladePropertyRune", false));
        
        /// <summary>
        /// This trait is for the feats which represent your choice of the holy or unholy traits for your consecrated panoply.
        /// </summary>
        public static readonly Trait HuntingSpikeConsecration = ModManager.RegisterTrait("HuntingSpikeConsecration", new TraitProperties("HuntingSpikeConsecration", false));
        
        /// <summary>
        /// This trait is for the feats which represent your choice of a weapon material for your consecrated panoply's specialized arsenal feature.
        /// </summary>
        public static readonly Trait HuntingSpikeMaterial = ModManager.RegisterTrait("HuntingSpikeMaterial", new TraitProperties("HuntingSpikeMaterial", false));

        #endregion
    }
}