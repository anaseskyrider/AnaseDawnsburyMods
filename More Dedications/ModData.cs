using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;

public static class ModData
{
    public const string IdPrepend = "MoreDedications.";
    
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
        ActionIds.Initialize();
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

    public static class ActionIds
    {
        public static ActionId TigerSlash;
        
        public static void Initialize()
        {
            TigerSlash = SafelyRegister<ActionId>("TigerSlash");
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
    
    public static class FeatNames
    {
        // In the early days of this mod, there were attempts to avoid mod conflicts by using unique strings.
        // Changing these technical strings will result in breaking character sheets, so they will remain as is.
        // However, later feats will usually have stock-like strings, as most modders tend not to overlap on feats, as cross-mod compatibility has become easier, as the base game now handles shared feat strings by ignoring the mod.
        
        #region Mauler
        public static readonly FeatName ClearTheWay = ModManager.RegisterFeatName(IdPrepend+"Archetype.Mauler.ClearTheWay", "Clear the Way");
        public static readonly FeatName ShovingSweep = ModManager.RegisterFeatName(IdPrepend+"Archetype.Mauler.ShovingSweep", "Shoving Sweep");
        #endregion
        
        #region Archer
        public static readonly FeatName AdvancedBowTraining = ModManager.RegisterFeatName(IdPrepend+"Archetype.Archer.AdvancedBowTraining", "Advanced Bow Training");
        public static readonly FeatName CrossbowTerror = ModManager.RegisterFeatName(IdPrepend+"Archetype.Archer.CrossbowTerror", "Crossbow Terror");
        public static readonly FeatName FighterPartingShot = ModManager.RegisterFeatName(IdPrepend+"Class.Fighter.PartingShot", "Parting Shot");
        public static readonly FeatName RangerRunningReload = ModManager.RegisterFeatName(IdPrepend+"Class.Ranger.RunningReload", "Running Reload");
        public static readonly FeatName ArchersAim = ModManager.RegisterFeatName(IdPrepend+"Archetype.Archer.ArchersAim", "Archer's Aim");
        #endregion
        
        #region Bastion
        public static readonly FeatName DisarmingBlock = ModManager.RegisterFeatName(IdPrepend+"Archetype.Bastion.DisarmingBlock", "Disarming Block");
        public static readonly FeatName NimbleShieldHand = ModManager.RegisterFeatName(IdPrepend+"Archetype.Bastion.NimbleShieldHand", "Nimble Shield Hand");
        public static readonly FeatName FighterShieldedStride = ModManager.RegisterFeatName(IdPrepend+"Class.Fighter.ShieldedStride", "Shielded Stride");
        public static readonly FeatName FighterReflexiveShield = ModManager.RegisterFeatName(IdPrepend+"Class.Fighter.ReflexiveShield", "Reflexive Shield");
        #endregion
        
        #region Martial Artist
        public static readonly FeatName PowderPunchStance = ModManager.RegisterFeatName(IdPrepend+"Archetype.MartialArtist.PowderPunchStance", "Powder Punch Stance");
        public static readonly FeatName StumblingStance = ModManager.RegisterFeatName("StumblingStance", "Stumbling Stance");
        public static readonly FeatName TigerStance = ModManager.RegisterFeatName("TigerStance", "Tiger Stance");
        public static readonly FeatName FollowUpStrike = ModManager.RegisterFeatName(IdPrepend+"Archetype.MartialArtist.FollowUpStrike", "Follow-Up Strike");
        public static readonly FeatName ThunderClap = ModManager.RegisterFeatName(IdPrepend+"Archetype.MartialArtist.ThunderClap", "Thunder Clap");
        public static readonly FeatName GrievousBlow = ModManager.RegisterFeatName("GrievousBlow", "Grievous Blow");
        public static readonly FeatName StumblingFeint = ModManager.RegisterFeatName("StumblingFeint", "Stumbling Feint");
        public static readonly FeatName TigerSlash = ModManager.RegisterFeatName("TigerSlash", "Tiger Slash");
        #endregion
        
        #region Marshal
        public static readonly FeatName DreadMarshalStance = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.DreadMarshalStance", "Dread Marshal Stance");
        public static readonly FeatName InspiringMarshalStance = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.InspiringMarshalStance", "Inspiring Marshal Stance");
        public static readonly FeatName SteelYourself = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.SteelYourself", "Steel Yourself!");
        public static readonly FeatName RallyingCharge = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.RallyingCharge", "Rallying Charge");
        public static readonly FeatName BackToBack = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.BackToBack", "Back to Back");
        public static readonly FeatName ToBattle = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.ToBattle", "To Battle!");
        #endregion
        
        #region Blessed One
        public static readonly FeatName BlessedSacrifice = ModManager.RegisterFeatName(IdPrepend+"Archetype.BlessedOne.BlessedSacrifice", "Blessed Sacrifice");
        #endregion
        
        #region Scout
        public static readonly FeatName ScoutsWarning = ModManager.RegisterFeatName("ScoutsWarning", "Scout's Warning");
        public static readonly FeatName ScoutsCharge = ModManager.RegisterFeatName(IdPrepend+"Archetype.Scout.ScoutsCharge", "Scout's Charge");
        public static readonly FeatName TerrainScout = ModManager.RegisterFeatName(IdPrepend+"Archetype.Scout.TerrainScout", "Terrain Scout");
        public static readonly FeatName FleetingShadow = ModManager.RegisterFeatName(IdPrepend+"Archetype.Scout.FleetingShadow", "Fleeting Shadow");
        public static readonly FeatName ScoutsSpeed = ModManager.RegisterFeatName(IdPrepend+"Archetype.Scout.ScoutsSpeed", "Scout's Speed");
        public static readonly FeatName ScoutsPounce = ModManager.RegisterFeatName(IdPrepend+"Archetype.Scout.ScoutsPounce", "Scout's Pounce");
        #endregion
        
        #region Assassin
        public static readonly FeatName ExpertBackstabber = ModManager.RegisterFeatName(IdPrepend+"Archetype.Assassin.ExpertBackstabber", "Expert Backstabber");
        public static FeatName PoisonResistance; // Set later
        public static readonly FeatName SurpriseAttack = ModManager.RegisterFeatName(IdPrepend+"Archetype.Assassin.SurpriseAttack", "Surprise Attack");
        public static readonly FeatName PoisonWeapon = ModManager.RegisterFeatName("PoisonWeapon", "Poison Weapon");
        public static readonly FeatName ImprovedPoisonWeapon = ModManager.RegisterFeatName("ImprovedPoisonWeapon", "Improved Poison Weapon");
        #endregion

        #region Dual Weapon Warrior
        
        public static readonly FeatName DualThrower = ModManager.RegisterFeatName(IdPrepend+"Archetype.DualWeaponWarrior.DualThrower", "Dual Thrower");
        public static readonly FeatName FlensingSlice = ModManager.RegisterFeatName(IdPrepend+"Archetype.DualWeaponWarrior.FlensingSlice", "Flensing Slice");

        #endregion

        #region Medic

        public static readonly FeatName DoctorsVisitation = ModManager.RegisterFeatName("DoctorsVisitation", "Doctor's Visitation");
        public static FeatName TreatConditionSkillVariant;
        public static FeatName HolisticCareSkillVariant;

        #endregion
        
        #region Familiar Master
        
        public static readonly FeatName OverloadFamiliar = ModManager.RegisterFeatName(IdPrepend+"Archetype.FamiliarMaster.OverloadFamiliar", "Overload Familiar");
        public static readonly FeatName FastCommand = ModManager.RegisterFeatName(IdPrepend+"Archetype.FamiliarMaster.FastCommand", "Fast Command");
        public static readonly FeatName MutableFamiliar = ModManager.RegisterFeatName(IdPrepend+"Archetype.FamiliarMaster.MutableFamiliar", "Mutable Familiar");
        public static readonly FeatName IncredibleFamiliar = ModManager.RegisterFeatName(IdPrepend+"Archetype.FamiliarMaster.IncredibleFamiliar", "Incredible Familiar");
        
        #endregion
        
        #region Bonus Stances
        public static readonly FeatName StokedFlameStance = ModManager.RegisterFeatName("StokedFlameStance", "Stoked Flame Stance");
        public static readonly FeatName InnerFire = ModManager.RegisterFeatName("InnerFireSOM", "Inner Fire (SoM)");
        public static readonly FeatName WildWindsInitiate = ModManager.RegisterFeatName("WildWindsInitiate", "Wild Winds Initiate");
        public static readonly FeatName ClingingShadowsInitiate = ModManager.RegisterFeatName("ClingingShadowsInitiate", "Clinging Shadows Initiate");
        public static readonly FeatName TangledForestStance = ModManager.RegisterFeatName("TangledForestStance", "Tangled Forest Stance");
        public static readonly FeatName JellyfishStance = ModManager.RegisterFeatName("JellyfishStance", "Jellyfish Stance");
        #endregion
    }

    public static class Illustrations
    {
        public const string ModFolder = "MoreDedicationsAssets/";
        
        public static readonly Illustration DawnsburySun = new ModdedIllustration(ModFolder+"PatreonSunTransparent.png");
        public static readonly Illustration CheckSymbol = new ModdedIllustration(ModFolder+"check symbol.png");
        public static readonly Illustration NoSymbol = new ModdedIllustration(ModFolder+"no symbol.png");
        public static readonly Illustration PowderPunchStance = IllustrationName.AlchemistsFire;
        public static readonly Illustration StumblingStance = new ModdedIllustration(ModFolder+"calabash.png");
        public static readonly Illustration DreadMarshalStance = IllustrationName.HideousLaughter;
        public static readonly Illustration InspiringMarshalStance = IllustrationName.WinningStreak;
        public static readonly Illustration SteelYourself = new ModdedIllustration(ModFolder+"heartburn.png");
        public static readonly Illustration RallyingCharge = new SideBySideIllustration(IllustrationName.FleetStep, new ModdedIllustration(ModFolder+"heart-wings.png"));
        public static readonly Illustration ToBattle = new ModdedIllustration(ModFolder+"flying-flag.png");
        public static readonly Illustration ProtectorsSacrifice = new ModdedIllustration(ModFolder+"protector's-sacrifice.png");
        public static readonly Illustration WildWindsStance = IllustrationName.FourWinds;
        public static readonly Illustration ClingingShadowsStance = IllustrationName.BlackTentacles;
        public static readonly Illustration FlensingSlice = new ModdedIllustration(ModFolder+"FlensingSlice.png");
    }
    
    public static class PersistentActions
    {
        public const string PoisonWeaponCharge = "PoisonWeaponCharge";
        public const string OverloadFamiliar = "OverloadFamiliar";
        public const string FastCommand = "FastCommand";
    }
    
    public static class QEffectIds
    {
        // Bastion
        public static QEffectId NimbleShieldHand;
        
        // Martial Artist
        public static QEffectId PowderPunchStance;
        public static QEffectId StumblingStance;
        public static QEffectId TigerStance;
        public static QEffectId FlatFootedToStumblingFeint;
        
        // Marshal
        public static QEffectId MarshalsAuraProvider;
        public static QEffectId MarshalsAuraEffect;
        public static QEffectId DreadMarshalStance;
        public static QEffectId InspiringMarshalStance;
        
        // Assassin
        public static QEffectId MarkForDeathCaster;
        public static QEffectId MarkForDeathTarget;
        public static QEffectId ExpertBackstabber;
        
        // Dual-Weapon Warrior
        public static QEffectId FlenseCounter;
        public static QEffectId FlenseWeapons;
        
        // Bonus stances
        public static QEffectId StokedFlameStance;
        public static QEffectId WildWindsStance;
        public static QEffectId ClingingShadowsStance;
        public static QEffectId TangledForestStance;
        public static QEffectId JellyfishStance;
        
        // Misc
        public static QEffectId GreaterScoutActivity;
        
        public static void Initialize()
        {
            // Bastion
            NimbleShieldHand = SafelyRegister<QEffectId>("NimbleShieldHand");
            
            // Martial Artist
            PowderPunchStance = SafelyRegister<QEffectId>("Powder Punch Stance");
            StumblingStance = SafelyRegister<QEffectId>("Stumbling Stance");
            TigerStance = SafelyRegister<QEffectId>("Tiger Stance");
            FlatFootedToStumblingFeint = SafelyRegister<QEffectId>("FlatFootedToStumblingFeint");
            
            // Marshal
            MarshalsAuraProvider = SafelyRegister<QEffectId>("MarshalsAuraProvider");
            MarshalsAuraEffect = SafelyRegister<QEffectId>("Marshal's Aura");
            DreadMarshalStance = SafelyRegister<QEffectId>("Dread Marshal Stance");
            InspiringMarshalStance = SafelyRegister<QEffectId>("Inspiring Marshal Stance");
            
            // Assassin
            MarkForDeathCaster = SafelyRegister<QEffectId>("MarkForDeathCaster");
            MarkForDeathTarget = SafelyRegister<QEffectId>("MarkForDeathTarget");
            ExpertBackstabber = SafelyRegister<QEffectId>("ExpertBackstabber");
            
            // Dual-Weapon Warrior
            FlenseCounter = SafelyRegister<QEffectId>("FlenseCounter");
            FlenseWeapons = SafelyRegister<QEffectId>("FlenseWeapons");
            
            // Bonus stances
            StokedFlameStance = SafelyRegister<QEffectId>("Stoked Flame Stance");
            WildWindsStance = SafelyRegister<QEffectId>("Wild Winds Stance");
            ClingingShadowsStance = SafelyRegister<QEffectId>("Clinging Shadows Stance");
            TangledForestStance = SafelyRegister<QEffectId>("Tangled Forest Stance");
            JellyfishStance = SafelyRegister<QEffectId>("Jellyfish Stance");
            
            // Misc
            GreaterScoutActivity = SafelyRegister<QEffectId>("GreaterScoutActivity");
        }
    }
    
    public static class SpellIds
    {
        public static SpellId WildWindsStance { get; set; }
        public static SpellId ClingingShadowsStance { get; set; }
        public static SpellId ProtectorsSacrifice { get; set; }
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> LeveledDC = RegisterTooltipInserter(
            IdPrepend+"LevelBasedDC",
            "{b}Level-based DCs{/b}\nWhen a DC is based on your level, it uses one of the following values:\n{b}Level 1:{/b} 15\n{b}Level 2:{/b} 16\n{b}Level 3:{/b} 18\n{b}Level 4:{/b} 19\n{b}Level 5:{/b} 20\n{b}Level 6:{/b} 22\n{b}Level 7:{/b} 23\n{b}Level 8:{/b} 24\n{b}Level 9:{/b} 26");
        
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
        public static readonly Trait MoreDedications = ModManager.RegisterTrait("MoreDedications", new TraitProperties("More Dedications", true));
            
        // Archetype Traits
        public static readonly Trait MaulerArchetype = ModManager.RegisterTrait(IdPrepend+"Mauler", new TraitProperties("Mauler", true));
        public static readonly Trait BastionArchetype = ModManager.RegisterTrait(IdPrepend+"Bastion", new TraitProperties("Bastion", true));
        public static readonly Trait MartialArtistArchetype = ModManager.RegisterTrait(IdPrepend+"MartialArtist", new TraitProperties("Martial Artist", true));
        public static readonly Trait MarshalArchetype = ModManager.RegisterTrait(IdPrepend+"Marshal", new TraitProperties("Marshal", true));
        public static readonly Trait BlessedOneArchetype = ModManager.RegisterTrait(IdPrepend+"BlessedOne", new TraitProperties("Blessed One", true));
        public static readonly Trait ScoutArchetype = ModManager.RegisterTrait(IdPrepend+"Scout", new TraitProperties("Scout", true));
        public static readonly Trait AssassinArchetype = ModManager.RegisterTrait(IdPrepend+"Assassin", new TraitProperties("Assassin", true));
        public static readonly Trait DualWeaponWarriorArchetype = ModManager.RegisterTrait(IdPrepend+"DualWeaponWarrior", new TraitProperties("Dual-Weapon Warrior", true));
        public static readonly Trait FamiliarMasterArchetype = ModManager.RegisterTrait(IdPrepend+"FamiliarMaster", new TraitProperties("Familiar Master", true));
        
        // Other traits
        public static readonly Trait Shadow = ModManager.RegisterTrait("Shadow");
    }
}