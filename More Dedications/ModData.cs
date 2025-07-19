using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications;

public static class ModData
{
    public static class Traits
    {
        public static readonly Trait MoreDedications = ModManager.RegisterTrait("MoreDedications", new TraitProperties("More Dedications", true));
            
        // Archetype Traits
        public static readonly Trait MaulerArchetype = ModManager.RegisterTrait("MoreDedications.Mauler", new TraitProperties("Mauler", true));
        public static readonly Trait BastionArchetype = ModManager.RegisterTrait("MoreDedications.Bastion", new TraitProperties("Bastion", true));
        public static readonly Trait MartialArtistArchetype = ModManager.RegisterTrait("MoreDedications.MartialArtist", new TraitProperties("Martial Artist", true));
        public static readonly Trait MarshalArchetype = ModManager.RegisterTrait("MoreDedications.Marshal", new TraitProperties("Marshal", true));
        public static readonly Trait BlessedOneArchetype = ModManager.RegisterTrait("MoreDedications.BlessedOne", new TraitProperties("Blessed One", true));
        public static readonly Trait ScoutArchetype = ModManager.RegisterTrait("MoreDedications.Scout", new TraitProperties("Scout", true));
    }
    
    public static class FeatNames
    {
        #region Mauler
        public static readonly FeatName ClearTheWay = ModManager.RegisterFeatName("MoreDedications.Archetype.Mauler.ClearTheWay", "Clear the Way");
        public static readonly FeatName ShovingSweep = ModManager.RegisterFeatName("MoreDedications.Archetype.Mauler.ShovingSweep", "Shoving Sweep");
        #endregion
        
        #region Archer
        public static readonly FeatName AdvancedBowTraining = ModManager.RegisterFeatName("MoreDedications.Archetype.Archer.AdvancedBowTraining", "Advanced Bow Training");
        public static readonly FeatName CrossbowTerror = ModManager.RegisterFeatName("MoreDedications.Archetype.Archer.CrossbowTerror", "Crossbow Terror");
        public static readonly FeatName FighterPartingShot = ModManager.RegisterFeatName("MoreDedications.Class.Fighter.PartingShot", "Parting Shot");
        public static readonly FeatName RangerRunningReload = ModManager.RegisterFeatName("MoreDedications.Class.Ranger.RunningReload", "Running Reload");
        public static readonly FeatName ArchersAim = ModManager.RegisterFeatName("MoreDedications.Archetype.Archer.ArchersAim", "Archer's Aim");
        #endregion
        
        #region Bastion
        public static readonly FeatName DisarmingBlock = ModManager.RegisterFeatName("MoreDedications.Archetype.Bastion.DisarmingBlock", "Disarming Block");
        public static readonly FeatName FighterShieldedStride = ModManager.RegisterFeatName("MoreDedications.Class.Fighter.ShieldedStride", "Shielded Stride");
        public static readonly FeatName FighterReflexiveShield = ModManager.RegisterFeatName("MoreDedications.Class.Fighter.ReflexiveShield", "Reflexive Shield");
        #endregion
        
        #region Martial Artist
        public static readonly FeatName PowderPunchStance = ModManager.RegisterFeatName("MoreDedications.Archetype.MartialArtist.PowderPunchStance", "Powder Punch Stance");
        public static readonly FeatName StumblingStance = ModManager.RegisterFeatName("StumblingStance", "Stumbling Stance");
        public static readonly FeatName TigerStance = ModManager.RegisterFeatName("TigerStance", "Tiger Stance");
        public static readonly FeatName FollowUpStrike = ModManager.RegisterFeatName("MoreDedications.Archetype.MartialArtist.FollowUpStrike", "Follow-Up Strike");
        public static readonly FeatName ThunderClap = ModManager.RegisterFeatName("MoreDedications.Archetype.MartialArtist.ThunderClap", "Thunder Clap");
        public static readonly FeatName CraneFlutter = ModManager.RegisterFeatName("CraneFlutter", "Crane Flutter");
        public static readonly FeatName DragonRoar = ModManager.RegisterFeatName("DragonRoar", "Dragon Roar");
        public static readonly FeatName GorillaPound = ModManager.RegisterFeatName("GorillaPound", "Gorilla Pound");
        public static readonly FeatName GrievousBlow = ModManager.RegisterFeatName("GrievousBlow", "Grievous Blow");
        public static readonly FeatName MountainStronghold = ModManager.RegisterFeatName("MountainStronghold", "Mountain Stronghold");
        public static readonly FeatName StumblingFeint = ModManager.RegisterFeatName("StumblingFeint", "Stumbling Feint");
        public static readonly FeatName TigerSlash = ModManager.RegisterFeatName("TigerSlash", "Tiger Slash");
        public static readonly FeatName WolfDrag = ModManager.RegisterFeatName("WolfDrag", "Wolf Drag");
        #endregion
        
        #region Marshal
        public static readonly FeatName DreadMarshalStance = ModManager.RegisterFeatName("MoreDedications.Archetype.Marshal.DreadMarshalStance", "Dread Marshal Stance");
        public static readonly FeatName InspiringMarshalStance = ModManager.RegisterFeatName("MoreDedications.Archetype.Marshal.InspiringMarshalStance", "Inspiring Marshal Stance");
        public static readonly FeatName SteelYourself = ModManager.RegisterFeatName("MoreDedications.Archetype.Marshal.SteelYourself", "Steel Yourself!");
        public static readonly FeatName RallyingCharge = ModManager.RegisterFeatName("MoreDedications.Archetype.Marshal.RallyingCharge", "Rallying Charge");
        public static readonly FeatName ToBattle = ModManager.RegisterFeatName("MoreDedications.Archetype.Marshal.ToBattle", "To Battle!");
        #endregion
        
        #region Blessed One
        public static readonly FeatName BlessedSacrifice = ModManager.RegisterFeatName("MoreDedications.Archetype.BlessedOne.BlessedSacrifice", "Blessed Sacrifice");
        #endregion
        
        #region Scout
        public static readonly FeatName ScoutsWarning = ModManager.RegisterFeatName("ScoutsWarning", "Scout's Warning");
        public static readonly FeatName ScoutsCharge = ModManager.RegisterFeatName("MoreDedications.Archetype.Scout.ScoutsCharge", "Scout's Charge");
        public static readonly FeatName TerrainScout = ModManager.RegisterFeatName("MoreDedications.Archetype.Scout.TerrainScout", "Terrain Scout");
        public static readonly FeatName FleetingShadow = ModManager.RegisterFeatName("MoreDedications.Archetype.Scout.FleetingShadow", "Fleeting Shadow");
        public static readonly FeatName ScoutsSpeed = ModManager.RegisterFeatName("MoreDedications.Archetype.Scout.ScoutsSpeed", "Scout's Speed");
        public static readonly FeatName ScoutsPounce = ModManager.RegisterFeatName("MoreDedications.Archetype.Scout.ScoutsPounce", "Scout's Pounce");
        #endregion
        
        #region Bonus Stances
        public static readonly FeatName StokedFlameStance = ModManager.RegisterFeatName("StokedFlameStance", "Stoked Flame Stance");
        public static readonly FeatName InnerFire = ModManager.RegisterFeatName("InnerFireSOM", "Inner Fire (SoM)");
        public static readonly FeatName WildWindsInitiate = ModManager.RegisterFeatName("WildWindsInitiate", "Wild Winds Initiate");
        public static readonly FeatName TangledForestStance = ModManager.RegisterFeatName("TangledForestStance", "Tangled Forest Stance");
        public static readonly FeatName JellyfishStance = ModManager.RegisterFeatName("JellyfishStance", "Jellyfish Stance");
        #endregion
    }
    
    public static class QEffectIds
    {
        // Martial Artist
        public static readonly QEffectId PowderPunchStance = ModManager.RegisterEnumMember<QEffectId>("Powder Punch Stance");
        public static readonly QEffectId StumblingStance = ModManager.RegisterEnumMember<QEffectId>("Stumbling Stance");
        public static readonly QEffectId TigerStance = ModManager.RegisterEnumMember<QEffectId>("Tiger Stance");
        public static readonly QEffectId DragonRoarImmunity = ModManager.RegisterEnumMember<QEffectId>("Dragon Roar Immunity");
        public static readonly QEffectId MountainStronghold = ModManager.RegisterEnumMember<QEffectId>("Mountain Stronghold");
        public static readonly QEffectId FlatFootedToStumblingFeint = ModManager.RegisterEnumMember<QEffectId>("FlatFootedToStumblingFeint");
        
        // Marshal
        public static readonly QEffectId MarshalsAuraProvider = ModManager.RegisterEnumMember<QEffectId>("MarshalsAuraProvider");
        public static readonly QEffectId MarshalsAuraEffect = ModManager.RegisterEnumMember<QEffectId>("Marshal's Aura");
        public static readonly QEffectId DreadMarshalStance = ModManager.RegisterEnumMember<QEffectId>("Dread Marshal Stance");
        public static readonly QEffectId InspiringMarshalStance = ModManager.RegisterEnumMember<QEffectId>("Inspiring Marshal Stance");
        
        // Bonus stances
        public static readonly QEffectId StokedFlameStance = ModManager.RegisterEnumMember<QEffectId>("Stoked Flame Stance");
        public static readonly QEffectId WildWindsStance = ModManager.RegisterEnumMember<QEffectId>("Wild Winds Stance");
        public static readonly QEffectId TangledForestStance = ModManager.RegisterEnumMember<QEffectId>("Tangled Forest Stance");
        public static readonly QEffectId JellyfishStance = ModManager.RegisterEnumMember<QEffectId>("Jellyfish Stance");
        
        // Misc
        public static QEffectId GreaterScoutActivity;
    }

    public static class ActionIds
    {
        public static readonly ActionId CraneFlutter = ModManager.RegisterEnumMember<ActionId>("CraneFlutter");
        public static readonly ActionId DragonRoar = ModManager.RegisterEnumMember<ActionId>("DragonRoar");
        public static readonly ActionId TigerSlash = ModManager.RegisterEnumMember<ActionId>("TigerSlash");
    }

    public static class SpellIds
    {
        public static SpellId WildWindsStance { get; set; }
        public static SpellId ProtectorsSacrifice { get; set; }
    }

    public static class Illustrations
    {
        public static readonly string DawnsburySunPath = "MoreDedicationsAssets/PatreonSunTransparent.png";
        public static readonly Illustration PowderPunchStance = IllustrationName.AlchemistsFire;
        public static readonly Illustration StumblingStance = new ModdedIllustration("MoreDedicationsAssets/calabash.png");
        public static readonly Illustration DreadMarshalStance = IllustrationName.HideousLaughter;
        public static readonly Illustration InspiringMarshalStance = IllustrationName.WinningStreak;
        public static readonly Illustration SteelYourself = new ModdedIllustration("MoreDedicationsAssets/heartburn.png");
        public static readonly Illustration RallyingCharge = new SideBySideIllustration(IllustrationName.FleetStep, new ModdedIllustration("MoreDedicationsAssets/heart-wings.png"));
        public static readonly Illustration ToBattle = new ModdedIllustration("MoreDedicationsAssets/flying-flag.png");
        public static readonly Illustration ProtectorsSacrifice = new ModdedIllustration("MoreDedicationsAssets/protector's-sacrifice.png");
        public static readonly Illustration WildWindsStance = IllustrationName.FourWinds;
    }

    public static class Tooltips
    {
        public static readonly Func<string?, string> LeveledDC = inline => "{tooltip:MoreDedications.LevelBasedDC}" + inline + "{/}";
    }
}