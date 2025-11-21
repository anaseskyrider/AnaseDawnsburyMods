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
    
    public static void LoadData()
    {
        // QEffects
        QEffectIds.GreaterScoutActivity = ModManager.TryParse("GreaterScoutActivity", out QEffectId greaterScout)
            ? greaterScout
            : ModManager.RegisterEnumMember<QEffectId>("GreaterScoutActivity");
    }

    public static class ActionIds
    {
        public static readonly ActionId DragonRoar = ModManager.RegisterEnumMember<ActionId>("DragonRoar");
        public static readonly ActionId TigerSlash = ModManager.RegisterEnumMember<ActionId>("TigerSlash");
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
        public static readonly FeatName FighterShieldedStride = ModManager.RegisterFeatName(IdPrepend+"Class.Fighter.ShieldedStride", "Shielded Stride");
        public static readonly FeatName FighterReflexiveShield = ModManager.RegisterFeatName(IdPrepend+"Class.Fighter.ReflexiveShield", "Reflexive Shield");
        #endregion
        
        #region Martial Artist
        public static readonly FeatName PowderPunchStance = ModManager.RegisterFeatName(IdPrepend+"Archetype.MartialArtist.PowderPunchStance", "Powder Punch Stance");
        public static readonly FeatName StumblingStance = ModManager.RegisterFeatName("StumblingStance", "Stumbling Stance");
        public static readonly FeatName TigerStance = ModManager.RegisterFeatName("TigerStance", "Tiger Stance");
        public static readonly FeatName FollowUpStrike = ModManager.RegisterFeatName(IdPrepend+"Archetype.MartialArtist.FollowUpStrike", "Follow-Up Strike");
        public static readonly FeatName ThunderClap = ModManager.RegisterFeatName(IdPrepend+"Archetype.MartialArtist.ThunderClap", "Thunder Clap");
        public static readonly FeatName DragonRoar = ModManager.RegisterFeatName("DragonRoar", "Dragon Roar");
        public static readonly FeatName GrievousBlow = ModManager.RegisterFeatName("GrievousBlow", "Grievous Blow");
        public static readonly FeatName StumblingFeint = ModManager.RegisterFeatName("StumblingFeint", "Stumbling Feint");
        public static readonly FeatName TigerSlash = ModManager.RegisterFeatName("TigerSlash", "Tiger Slash");
        #endregion
        
        #region Marshal
        public static readonly FeatName DreadMarshalStance = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.DreadMarshalStance", "Dread Marshal Stance");
        public static readonly FeatName InspiringMarshalStance = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.InspiringMarshalStance", "Inspiring Marshal Stance");
        public static readonly FeatName SteelYourself = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.SteelYourself", "Steel Yourself!");
        public static readonly FeatName RallyingCharge = ModManager.RegisterFeatName(IdPrepend+"Archetype.Marshal.RallyingCharge", "Rallying Charge");
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
    }
    
    public static class QEffectIds
    {
        // Martial Artist
        public static readonly QEffectId PowderPunchStance = ModManager.RegisterEnumMember<QEffectId>("Powder Punch Stance");
        public static readonly QEffectId StumblingStance = ModManager.RegisterEnumMember<QEffectId>("Stumbling Stance");
        public static readonly QEffectId TigerStance = ModManager.RegisterEnumMember<QEffectId>("Tiger Stance");
        public static readonly QEffectId FlatFootedToStumblingFeint = ModManager.RegisterEnumMember<QEffectId>("FlatFootedToStumblingFeint");
        
        // Marshal
        public static readonly QEffectId MarshalsAuraProvider = ModManager.RegisterEnumMember<QEffectId>("MarshalsAuraProvider");
        public static readonly QEffectId MarshalsAuraEffect = ModManager.RegisterEnumMember<QEffectId>("Marshal's Aura");
        public static readonly QEffectId DreadMarshalStance = ModManager.RegisterEnumMember<QEffectId>("Dread Marshal Stance");
        public static readonly QEffectId InspiringMarshalStance = ModManager.RegisterEnumMember<QEffectId>("Inspiring Marshal Stance");
        
        // Assassin
        public static readonly QEffectId MarkForDeathCaster = ModManager.RegisterEnumMember<QEffectId>("MarkForDeathCaster");
        public static readonly QEffectId MarkForDeathTarget = ModManager.RegisterEnumMember<QEffectId>("MarkForDeathTarget");
        public static readonly QEffectId ExpertBackstabber = ModManager.RegisterEnumMember<QEffectId>("ExpertBackstabber");
        
        // Dual-Weapon Warrior
        public static readonly QEffectId FlenseCounter = ModManager.RegisterEnumMember<QEffectId>("FlenseCounter");
        public static readonly QEffectId FlenseWeapons = ModManager.RegisterEnumMember<QEffectId>("FlenseWeapons");
        
        // Bonus stances
        public static readonly QEffectId StokedFlameStance = ModManager.RegisterEnumMember<QEffectId>("Stoked Flame Stance");
        public static readonly QEffectId WildWindsStance = ModManager.RegisterEnumMember<QEffectId>("Wild Winds Stance");
        public static readonly QEffectId ClingingShadowsStance = ModManager.RegisterEnumMember<QEffectId>("Clinging Shadows Stance");
        public static readonly QEffectId TangledForestStance = ModManager.RegisterEnumMember<QEffectId>("Tangled Forest Stance");
        public static readonly QEffectId JellyfishStance = ModManager.RegisterEnumMember<QEffectId>("Jellyfish Stance");
        
        // Misc
        public static QEffectId GreaterScoutActivity;
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
        
        // Other traits
        public static readonly Trait Shadow = ModManager.RegisterTrait("Shadow");
    }
}