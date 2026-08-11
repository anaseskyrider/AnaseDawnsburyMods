using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.MoreShields;

namespace Dawnsbury.Mods.MoreArchetypes;

public static class ModData
{
    public const string ID_PREPEND = "MoreArchetypes.";

    public static Trait ModTrait;

    /// <summary>
    /// Loads all mod data. This should typically be called by a mod before anything else.
    /// </summary>
    /// <para>
    /// When registering mod data, certain data must be called through the execution of lines of code, rather than assigned in their initialization. The Initializer skips these data until they're first called, which can result in errors due to out of order registration calls (especially when another mod isn't using <see cref="ModManager.TryParse"/>).
    /// </para>
    /// <para>The following data forms are typically safe due to the way Dawnsbury Days loads mods (or because their initialization nearly always gets called before errors could arise): <see cref="FeatName"/>, <see cref="Illustration"/>, <see cref="Trait"/>, <see cref="MoreShields.ModData.SfxNames"/>, <see cref="SpellId"/>. Tooltips from <see cref="ModManager.RegisterInlineTooltip(string, string)"/> likely aren't safe to assign as part of the initializer, but they typically shouldn't be shared between mods either.
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
        QEffectIds.Initialize();
    }

    public static class ActionIds
    {
        // Martial Artist
        public static ActionId TigerSlash;
        
        // Marshal
        public static ActionId DreadMarshalStance;
        public static ActionId InspiringMarshalStance;
        //public static ActionId StrategistStance;
        
        // Wrestler
        public static ActionId ElbowBreaker;
        
        public static void Initialize()
        {
            TigerSlash = ModManager.SafelyRegisterEnumMember<ActionId>("TigerSlash");
            DreadMarshalStance = ModManager.SafelyRegisterEnumMember<ActionId>("DreadMarshalStance");
            InspiringMarshalStance = ModManager.SafelyRegisterEnumMember<ActionId>("InspiringMarshalStance");
            //StrategistStance = ModManager.SafelyRegisterEnumMember<ActionId>("StrategistStance");
            ElbowBreaker = ModManager.SafelyRegisterEnumMember<ActionId>("ElbowBreaker");
        }
    }

    public static class CommonRequirements
    {
        public static Func<Creature, string?> StanceRestriction(QEffectId stanceId)
        {
            return self =>
            {
                if (self.HasEffect(stanceId))
                    return "You're already in this stance.";
                return null;
            };
        }
    }

    public static class FeatGroups
    {
        public static readonly FeatGroup Archetypes = new FeatGroup("Archetypes", 5);
    }

    public static class FeatNames
    {
        #region Archer

        public static FeatName ArcherDedication;
        public static readonly FeatName CrossbowAceRemastered = ModManager.SafelyRegisterEnumMember<FeatName>(
            "CrossbowAceRemastered",
            ["Crossbow Ace"]);
        public static readonly FeatName CrossbowTerrorRemastered = ModManager.SafelyRegisterEnumMember<FeatName>(
            "CrossbowTerrorRemastered",
            ["Crossbow Terror (Remastered)"]);
        public static readonly FeatName PartingShot = ModManager.SafelyRegisterEnumMember<FeatName>(
            "PartingShot",
            ["Parting Shot"]);
        public static FeatName PartingShotForArchetypeArcher;
        public static readonly FeatName UnobstructedShot = ModManager.SafelyRegisterEnumMember<FeatName>(
            "UnobstructedShot",
            ["Unobstructed Shot"]);

        #endregion

        #region Assassin

        public static FeatName AssassinDedication;
        public static readonly FeatName ExpertBackstabber = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ExpertBackstabber",
            ["Expert Backstabber"]);
        public static readonly FeatName SurpriseAttack = ModManager.SafelyRegisterEnumMember<FeatName>(
            "SurpriseAttack",
            ["Surprise Attack"]);
        public static readonly FeatName PoisonWeapon = ModManager.SafelyRegisterEnumMember<FeatName>(
            "PoisonWeapon",
            ["Surprise Attack"]);
        public static readonly FeatName ImprovedPoisonWeapon = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ImprovedPoisonWeapon",
            ["Improved Poison Weapon"]);
        public static readonly FeatName Assassinate = ModManager.SafelyRegisterEnumMember<FeatName>(
            "Assassinate",
            ["Assassinate"]);

        #endregion

        #region Bastion

        public static FeatName BastionDedication;
        public static readonly FeatName DisarmingBlock = ModManager.SafelyRegisterEnumMember<FeatName>(
            "DisarmingBlock",
            ["Disarming Block"]);
        public static readonly FeatName NimbleShieldHand = ModManager.SafelyRegisterEnumMember<FeatName>(
            "NimbleShieldHand",
            ["Nimble Shield Hand"]);
        public static readonly FeatName ShieldedStride = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ShieldedStride",
            ["Shielded Stride"]);
        public static readonly FeatName ReflexiveShield = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ReflexiveShield",
            ["Reflexive Shield"]);

        #endregion

        #region Blessed One

        public static FeatName BlessedOneDedication;
        public static readonly FeatName BlessedSacrifice = ModManager.SafelyRegisterEnumMember<FeatName>(
            "BlessedSacrifice",
            ["Blessed Sacrifice"]);

        #endregion

        #region Dual-Weapon Warrior

        public static FeatName DualWeaponWarriorDedication;
        public static readonly FeatName DualThrower = ModManager.SafelyRegisterEnumMember<FeatName>(
            "DualThrower",
            ["Dual Thrower"]);
        public static readonly FeatName FlensingSlice = ModManager.SafelyRegisterEnumMember<FeatName>(
            "FlensingSlice",
            ["Flensing Slice"]);
        public static readonly FeatName DualWeaponBlitz = ModManager.SafelyRegisterEnumMember<FeatName>(
            "DualWeaponBlitz",
            ["Dual-Weapon Blitz"]);
        public static readonly FeatName DualOnslaught = ModManager.SafelyRegisterEnumMember<FeatName>(
            "DualOnslaught",
            ["Dual Onslaught"]);

        #endregion

        #region Familiar Master
        
        public static FeatName FamiliarMasterDedication;
        public static readonly FeatName OverloadFamiliar = ModManager.SafelyRegisterEnumMember<FeatName>(
            "OverloadFamiliar",
            ["Overload Familiar"]);
        public static readonly FeatName FastCommand = ModManager.SafelyRegisterEnumMember<FeatName>(
            "FastCommand",
            ["Fast Command"]);
        public static readonly FeatName MutableFamiliar = ModManager.SafelyRegisterEnumMember<FeatName>(
            "MutableFamiliar",
            ["Mutable Familiar"]);
        public static readonly FeatName IncredibleFamiliar = ModManager.SafelyRegisterEnumMember<FeatName>(
            "IncredibleFamiliar",
            ["Incredible Familiar"]);

        #endregion
        
        #region Marshal

        public static FeatName MarshalDedication;
        public static readonly FeatName DreadMarshalStance = ModManager.SafelyRegisterEnumMember<FeatName>(
            "DreadMarshalStance",
            ["Dread Marshal Stance"]);
        public static readonly FeatName InspiringMarshalStance = ModManager.SafelyRegisterEnumMember<FeatName>(
            "InspiringMarshalStance",
            ["Inspiring Marshal Stance"]);
        public static readonly FeatName SteelYourself = ModManager.SafelyRegisterEnumMember<FeatName>(
            "SteelYourself",
            ["Steel Yourself!"]);
        /*public static readonly FeatName StrategistStance = ModManager.SafelyRegisterEnumMember<FeatName>(
            "StrategistStance",
            ["Strategist Stance"]);*/
        public static readonly FeatName BoomingPresence = ModManager.SafelyRegisterEnumMember<FeatName>(
            "BoomingPresence",
            ["Booming Presence"]);
        public static readonly FeatName CadenceCall = ModManager.SafelyRegisterEnumMember<FeatName>(
            "CadenceCall",
            ["Cadence Call"]);
        public static readonly FeatName RallyingCharge = ModManager.SafelyRegisterEnumMember<FeatName>(
            "RallyingCharge",
            ["Rallying Charge"]);
        public static readonly FeatName BackToBack = ModManager.SafelyRegisterEnumMember<FeatName>(
            "BackToBack",
            ["Back to Back"]);
        /*public static readonly FeatName KnowYourEnemy = ModManager.SafelyRegisterEnumMember<FeatName>(
            "KnowYourEnemy",
            ["Know Your Enemy"]);*/
        public static readonly FeatName ToBattle = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ToBattle",
            ["To Battle!"]);
        /*public static readonly FeatName FormUp = ModManager.SafelyRegisterEnumMember<FeatName>(
            "FormUp",
            ["Form Up!"]);*/
        public static readonly FeatName ToppleFoe = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ToppleFoe",
            ["Topple Foe"]);
        public static readonly FeatName CoordinatedCharge = ModManager.SafelyRegisterEnumMember<FeatName>(
            "CoordinatedCharge",
            ["Coordinated Charge"]);
        /*public static readonly FeatName GeneralsGambit = ModManager.SafelyRegisterEnumMember<FeatName>(
            "GeneralsGambit",
            ["General's Gambit"]);*/
        public static readonly FeatName TacticalCadence = ModManager.SafelyRegisterEnumMember<FeatName>(
            "TacticalCadence",
            ["Tactical Cadence"]);
        /*public static readonly FeatName TargetOfOpportunity = ModManager.SafelyRegisterEnumMember<FeatName>(
            "TargetOfOpportunity",
            ["Target of Opportunity"]);*/
        
        #endregion
        
        #region Martial Artist
        
        public static readonly FeatName StumblingStance = ModManager.SafelyRegisterEnumMember<FeatName>(
            "StumblingStance",
            ["Stumbling Stance"]);
        public static readonly FeatName TigerStance = ModManager.SafelyRegisterEnumMember<FeatName>(
            "TigerStance",
            ["Tiger Stance"]);
        public static readonly FeatName StumblingFeint = ModManager.SafelyRegisterEnumMember<FeatName>(
            "StumblingFeint",
            ["Stumbling Feint"]);
        public static readonly FeatName TigerSlash = ModManager.SafelyRegisterEnumMember<FeatName>(
            "TigerSlash",
            ["Tiger Slash"]);
        
        #endregion
        
        #region Mauler

        public static FeatName MaulerDedication;
        public static FeatName SlamDownForMauler;
        public static FeatName ViciousSwingForMauler;
        public static readonly FeatName ClearTheWay = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ClearTheWay",
            ["Clear the Way"]);
        public static readonly FeatName ShovingSweep = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ShovingSweep",
            ["Shoving Sweep"]);
        public static FeatName CrashingSlamForMauler;
        /*public static readonly FeatName HammerQuake = ModManager.SafelyRegisterEnumMember<FeatName>(
            "HammerQuake",
            ["Hammer Quake"]);
        public static readonly FeatName AvalancheStrike = ModManager.SafelyRegisterEnumMember<FeatName>(
            "AvalancheStrike",
            ["Avalanche Strike"]);*/
        
        #endregion

        #region Medic

        public static FeatName MedicDedication;
        public static readonly FeatName DoctorsVisitation = ModManager.SafelyRegisterEnumMember<FeatName>(
            "DoctorsVisitation",
            ["Doctor's Visitation"]);
        public static FeatName TreatConditionSkillVariant;
        public static FeatName HolisticCareSkillVariant;

        #endregion
        
        #region Scout

        public static FeatName ScoutDedication;
        /// <summary>
        /// Avoid Notice and Scout feat for Exploration Activities. This feat is given a mod-style enum to avoid conflicts with identical options from other sources.
        /// </summary>
        public static readonly FeatName AvoidNoticeAndScout = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ScoutArchetype.AvoidNoticeAndScout",
            ["Avoid Notice and Scout"]);
        public static readonly FeatName ScoutsWarning = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ScoutsWarning",
            ["Scout's Warning"]);
        public static readonly FeatName ScoutsCharge = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ScoutsCharge",
            ["Scout's Charge"]);
        /*public static readonly FeatName TerrainScout = ModManager.SafelyRegisterEnumMember<FeatName>(
            "TerrainScout",
            ["Terrain Scout"]);*/
        public static readonly FeatName FleetingShadow = ModManager.SafelyRegisterEnumMember<FeatName>(
            "FleetingShadow",
            ["Fleeting Shadow"]);
        public static readonly FeatName ScoutsSpeed = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ScoutsSpeed",
            ["Scout's Speed"]);
        public static readonly FeatName ScoutsPounce = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ScoutsPounce",
            ["Scout's Pounce"]);
        public static FeatName CamouflageForScout;
        
        #endregion

        #region Wrestler

        public static readonly FeatName ElbowBreaker = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ElbowBreaker",
            ["Elbow Breaker"]);
        public static readonly FeatName RunningTackle = ModManager.SafelyRegisterEnumMember<FeatName>(
            "RunningTackle",
            ["Running Tackle"]);
        /*public static readonly FeatName Strangle = ModManager.SafelyRegisterEnumMember<FeatName>(
            "Strangle",
            ["Strangle"]);*/
        public static readonly FeatName SubmissionHold = ModManager.SafelyRegisterEnumMember<FeatName>(
            "SubmissionHold",
            ["Submission Hold"]);
        /*public static readonly FeatName InescapableGrasp = ModManager.SafelyRegisterEnumMember<FeatName>(
            "InescapableGrasp",
            ["Inescapable Grasp"]);
        public static readonly FeatName FormLock = ModManager.SafelyRegisterEnumMember<FeatName>(
            "FormLock",
            ["Form Lock"]);
        public static readonly FeatName Godbreaker = ModManager.SafelyRegisterEnumMember<FeatName>(
            "Godbreaker",
            ["Godbreaker"]);*/

        #endregion
        
        #region Bonus Stances
        
        public static readonly FeatName WildWindsInitiate = ModManager.SafelyRegisterEnumMember<FeatName>(
            "WildWindsInitiate",
            ["Wild Winds Initiate"]);
        public static readonly FeatName ClingingShadowsInitiate = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ClingingShadowsInitiate",
            ["Clinging Shadows Initiate"]);
        public static readonly FeatName TangledForestStance = ModManager.SafelyRegisterEnumMember<FeatName>(
            "TangledForestStance",
            ["Tangled Forest Stance"]);
        
        #endregion

        #region Bonus Skill Feats

        public static readonly FeatName ContinualRecovery = ModManager.SafelyRegisterEnumMember<FeatName>(
            "ContinualRecovery",
            ["Continual Recovery"]);
        public static readonly FeatName WardMedic = ModManager.SafelyRegisterEnumMember<FeatName>(
            "WardMedic",
            ["Ward Medic"]);

        #endregion
    }

    public static class Illustrations
    {
        public const string MOD_FOLDER = "MoreArchetypesAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(MOD_FOLDER + "PatreonSunTransparent.png");
        public static readonly Illustration CheckSymbol = new ModdedIllustration(MOD_FOLDER+"check symbol.png");
        public static readonly Illustration NoSymbol = new ModdedIllustration(MOD_FOLDER+"no symbol.png");

        #region Assassin

        public static readonly Illustration MarkedForDeath = IllustrationName.RequiemOfDeath;

        #endregion
        
        #region Blessed One

        public static readonly Illustration ProtectorsSacrifice = new ModdedIllustration(MOD_FOLDER+"protector's-sacrifice.png");

        #endregion
        
        #region Dual-Weapon Warrior

        public static readonly Illustration FlensingSlice = new ModdedIllustration(MOD_FOLDER+"FlensingSlice.png");
        public static readonly Illustration DualWeaponBlitz = new SideBySideIllustration(
            IllustrationName.FleetStep,
            IllustrationName.Swords);

        #endregion

        #region Martial Artist

        public static readonly Illustration StumblingStance = new ModdedIllustration(MOD_FOLDER+"calabash.png");
        public static readonly Illustration WildWindsStance = IllustrationName.FourWinds;
        public static readonly Illustration ClingingShadowsStance = IllustrationName.BlackTentacles;

        #endregion

        #region Marshal

        public static readonly Illustration DreadMarshalStance = IllustrationName.HideousLaughter;
        public static readonly Illustration InspiringMarshalStance = IllustrationName.WinningStreak;
        public static readonly Illustration SteelYourself = new ModdedIllustration(MOD_FOLDER+"heartburn.png");
        public static readonly Illustration RallyingCharge = new SideBySideIllustration(IllustrationName.FleetStep, new ModdedIllustration(MOD_FOLDER+"heart-wings.png"));
        public static readonly Illustration ToBattle = new ModdedIllustration(MOD_FOLDER+"flying-flag.png");

        #endregion

        #region Wrestler

        public static readonly Illustration SubmissionHold = new CornerIllustration(
            IllustrationName.Grapple,
            IllustrationName.Enfeebled,
            Direction.Southeast);

        #endregion

        #region Bonus Stances

        public static readonly Illustration TangledForestStance = IllustrationName.ProtectorTree;

        #endregion
    }

    public static class PersistentActions
    {
        #region Assassin

        public const string POISON_WEAPON_CHARGE = "PoisonWeaponCharge";

        #endregion

        #region Familiar Master

        public const string OVERLOAD_FAMILIAR = "OverloadFamiliar";
        public const string FAST_COMMAND = "FastCommand";

        #endregion
    }

    public static class PossibilityGroups
    {
        public const string MARSHAL = "Marshal";
    }

    public static class QEffectIds
    {
        // Assassin
        public static QEffectId MarkedForDeathTarget;
        
        // Dual-Weapon Warrior
        public static QEffectId FlenseCounter;
        public static QEffectId FlenseWeapons;
        public static QEffectId MovementCounter;
        
        // Marshal
        public static QEffectId MarshalsAuraProvider;
        public static QEffectId MarshalsAuraEffect;
        public static QEffectId DreadMarshalStance;
        public static QEffectId InspiringMarshalStance;
        
        // Martial Artist
        public static QEffectId StumblingStance;
        public static QEffectId TigerStance;
        public static QEffectId FlatFootedToStumblingFeint;
        
        // Bonus stances
        public static QEffectId WildWindsStance;
        public static QEffectId ClingingShadowsStance;
        public static QEffectId TangledForestStance;
        
        // Misc
        public static QEffectId GreaterScoutActivity;
        
        internal static void Initialize()
        {
            // Assassin
            MarkedForDeathTarget = ModManager.SafelyRegisterEnumMember<QEffectId>("MarkedForDeathTarget");
            
            // Dual-Weapon Warrior
            FlenseCounter = ModManager.SafelyRegisterEnumMember<QEffectId>("FlenseCounter");
            FlenseWeapons = ModManager.SafelyRegisterEnumMember<QEffectId>("FlenseWeapons");
            MovementCounter = ModManager.SafelyRegisterEnumMember<QEffectId>("MovementCounter");
            
            // Marshal
            MarshalsAuraProvider = ModManager.SafelyRegisterEnumMember<QEffectId>("MarshalsAuraProvider");
            MarshalsAuraEffect = ModManager.SafelyRegisterEnumMember<QEffectId>("MarshalsAura");
            DreadMarshalStance = ModManager.SafelyRegisterEnumMember<QEffectId>("DreadMarshalStance");
            InspiringMarshalStance = ModManager.SafelyRegisterEnumMember<QEffectId>("InspiringMarshalStance");
            
            // Martial Artist
            StumblingStance = ModManager.SafelyRegisterEnumMember<QEffectId>("Stumbling Stance");
            TigerStance = ModManager.SafelyRegisterEnumMember<QEffectId>("Tiger Stance");
            FlatFootedToStumblingFeint = ModManager.SafelyRegisterEnumMember<QEffectId>("FlatFootedToStumblingFeint");
            
            // Bonus stances
            WildWindsStance = ModManager.SafelyRegisterEnumMember<QEffectId>("WildWindsStance");
            ClingingShadowsStance = ModManager.SafelyRegisterEnumMember<QEffectId>("ClingingShadowsStance");
            TangledForestStance = ModManager.SafelyRegisterEnumMember<QEffectId>("TangledForestStance");
            
            // Misc
            GreaterScoutActivity = ModManager.SafelyRegisterEnumMember<QEffectId>("GreaterScoutActivity");
        }
    }
    
    public static class SpellIds
    {
        // Blessed One
        public static SpellId ProtectorsSacrifice { get; set; }
        
        // Bonus stances
        public static SpellId WildWindsStance { get; set; }
        public static SpellId ClingingShadowsStance { get; set; }
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> LeveledDC = RegisterTooltipInserter(
            ID_PREPEND + "LevelBasedDC",
            """
            {b}Level-based DCs{/b}
            When a DC is based on your level, it uses one of the following values:
            {b}Level 1:{/b} 15
            {b}Level 2:{/b} 16
            {b}Level 3:{/b} 18
            {b}Level 4:{/b} 19
            {b}Level 5:{/b} 20
            {b}Level 6:{/b} 22
            {b}Level 7:{/b} 23
            {b}Level 8:{/b} 24
            {b}Level 9:{/b} 26
            {b}Level 10:{/b} 27
            {b}Level 11:{/b} 28
            {b}Level 12:{/b} 30
            {b}Level 13:{/b} 31
            {b}Level 14:{/b} 32
            {b}Level 15:{/b} 34
            {b}Level 16:{/b} 35
            {b}Level 17:{/b} 36
            {b}Level 18:{/b} 38
            {b}Level 19:{/b} 39
            {b}Level 20:{/b} 40
            """);
        
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

        public static readonly Func<string, string> InjuryPoison = RegisterTooltipInserter(
            ID_PREPEND + "InjuryPoison",
            """
            {b}Injury Poison{/b}
            {i}(Common item mechanic){/i}
            An injury poison is applied to a weapon that deals piercing or slashing damage, exposing the target of an attack to the poison on a hit.

            On a critical miss, the poison wears off and is wasted.
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
        /// <summary>
        /// Assassin archetype
        /// </summary>
        public static readonly Trait Assassin = ModManager.RegisterTrait("Assassin", new TraitProperties("Assassin", true));
        
        /// <summary>
        /// Bastion archetype
        /// </summary>
        public static readonly Trait Bastion = ModManager.RegisterTrait("Bastion", new TraitProperties("Bastion", true));
        
        /// <summary>
        /// Blessed One archetype
        /// </summary>
        public static readonly Trait BlessedOne = ModManager.RegisterTrait("BlessedOne", new TraitProperties("Blessed One", true));
        
        /// <summary>
        /// Dual-Weapon Warrior archetype
        /// </summary>
        public static readonly Trait DualWeaponWarrior = ModManager.RegisterTrait("DualWeaponWarrior", new TraitProperties("Dual-Weapon Warrior", true));
        
        /// <summary>
        /// Familiar Master archetype
        /// </summary>
        public static readonly Trait FamiliarMaster = ModManager.RegisterTrait("FamiliarMaster", new TraitProperties("Familiar Master", true));
        
        /// <summary>
        /// Marshal archetype
        /// </summary>
        public static readonly Trait Marshal = ModManager.RegisterTrait("Marshal", new TraitProperties("Marshal", true));
        
        /// <summary>
        /// Mauler archetype
        /// </summary>
        public static readonly Trait Mauler = ModManager.RegisterTrait("Mauler", new TraitProperties("Mauler", true));
        
        /// <summary>
        /// Scout archetype
        /// </summary>
        public static readonly Trait Scout = ModManager.RegisterTrait("Scout", new TraitProperties("Scout", true));
        
        /// <summary>
        /// Wrestler archetype
        /// </summary>
        public static readonly Trait Wrestler = ModManager.RegisterTrait("Wrestler", new TraitProperties("Wrestler", true));
    }
}