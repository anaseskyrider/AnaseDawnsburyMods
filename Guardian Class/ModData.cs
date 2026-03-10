using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.GuardianClass;

public static class ModData
{
    public const string IdPrepend = "GuardianClass.";
    
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
        PossibilitySectionIds.Initialize();
        QEffectIds.Initialize();
        SubmenuIds.Initialize();
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
        public static ActionId Taunt;
        public static ActionId InterceptAttack;
        
        public static void Initialize()
        {
            Taunt = SafelyRegister<ActionId>("Taunt");
            InterceptAttack = SafelyRegister<ActionId>("InterceptAttack");
        }
    }

    public static class CommonQfKeys
    {
        /// <summary>This key includes the name of the Guardian who inflicted it -- search using this name + Creature.Name. Searching for the source of this effect also ensures that the creature is off-guard due to ignoring YOUR Taunt.</summary>
        public static readonly string OffGuardDueToTaunt = "TauntOffGuard";
        /// <summary>This key includes the name of the Guardian who inflicted it -- search using this name + Creature.Name. This QF should also include the Guardian as a Source.</summary>
        public static readonly string ShieldedAttrition = "ShieldedAttrition:";
    }

    public static class CommonReactionKeys
    {
        public const string ReactionTime = "ReactionTime";
    }

    public static class CommonRequirements
    {
        public static CreatureTargetingRequirement MustWearMediumOrHeavyArmor()
        {
            return new LegacyCreatureTargetingRequirement((a,_) =>
                IsWearingMediumOrHeavyArmor(a)
                    ? Usability.Usable
                    : Usability.NotUsable("must be wearing medium or heavy armor"));
        }

        public static CreatureTargetingRequirement OffGuardDueToMyTaunt()
        {
            return new LegacyCreatureTargetingRequirement((a, d) =>
                d.QEffects.Any(qf => qf.Key == CommonQfKeys.OffGuardDueToTaunt+a.Name)
                    ? Usability.Usable
                    : Usability.NotUsableOnThisCreature("Hasn't ignored your Taunt"));
        }

        public static bool IsWearingMediumOrHeavyArmor(Creature cr)
        {
            return (cr.Armor.Item is {} armor1
                    && (armor1.HasTrait(Trait.MediumArmor) || armor1.HasTrait(Trait.HeavyArmor)))
                   || (cr.BaseArmor is {} armor2
                       && (armor2.HasTrait(Trait.MediumArmor) || armor2.HasTrait(Trait.HeavyArmor)));
        }

        public static bool IsWearingHeavyArmor(Creature cr)
        {
            return (cr.Armor.Item is {} armor1
                    && armor1.HasTrait(Trait.HeavyArmor))
                   || (cr.BaseArmor is {} armor2
                       && armor2.HasTrait(Trait.HeavyArmor));
        }

        public static bool HasInterceptAttack(CalculatedCharacterSheetValues values)
        {
            return values.HasFeat(FeatNames.InterceptAttack) || values.HasFeat(FeatNames.GuardiansIntercept);
        }

        public static bool IsInterceptableDamageType(Creature guardian, KindedDamage kd)
        {
            return kd.DamageKind.IsPhysical()
                   || (guardian.HasFeat(FeatNames.EnergyInterceptor)
                       && kd.DamageKind.IsEnergy());
        }
    }
    
    public static class FeatNames
    {
        #region Class
        
        public static readonly FeatName GuardianClass = ModManager.RegisterFeatName(IdPrepend + "GuardianClass", "Guardian");
        
        #endregion

        #region Class Archetype
        
        public static FeatName GuardianDedication; // Data filled in GuardianArchetype.LoadArchetype().
        public static readonly FeatName GuardiansIntercept = ModManager.RegisterFeatName(IdPrepend + "GuardiansIntercept", "Guardian's Intercept");
        public static readonly FeatName ArmoredResistance = ModManager.RegisterFeatName(IdPrepend + "ArmoredResistance", "Armored Resistance");
        public static readonly FeatName IroncladFortitude = ModManager.RegisterFeatName(IdPrepend + "IroncladFortitude", "Ironclad Fortitude");

        #endregion
        
        #region Intercept Attack Toggles

        public static readonly FeatName InterceptToggleAlreadyReducedDamage = ModManager.RegisterFeatName(IdPrepend + "InterceptToggleAlreadyReducedDamage", "Already-reduced damage");
        public static readonly FeatName InterceptToggleCompanions = ModManager.RegisterFeatName(IdPrepend + "InterceptToggleCompanions", "Companions");
        public static readonly FeatName InterceptToggleCrits = ModManager.RegisterFeatName(IdPrepend + "InterceptToggleCrits", "Critical Hits");
        public static readonly FeatName InterceptToggleKO = ModManager.RegisterFeatName(IdPrepend + "InterceptToggleKO", "Knockout Hits");
        public static readonly FeatName InterceptToggleHits = ModManager.RegisterFeatName(IdPrepend + "InterceptToggleHits", "Normal Hits");
        public static readonly FeatName InterceptToggleSummons = ModManager.RegisterFeatName(IdPrepend + "InterceptToggleSummons", "Summons");
        
        #endregion
        
        #region Class Features
        public static readonly FeatName GuardiansArmor = ModManager.RegisterFeatName(IdPrepend + "GuardiansArmor", "Guardian's Armor");
        public static readonly FeatName Taunt = ModManager.RegisterFeatName(IdPrepend + "Taunt", "Taunt");
        public static readonly FeatName InterceptAttack = ModManager.RegisterFeatName(IdPrepend + "InterceptAttack", "Intercept Attack");
        public static readonly FeatName ToughToKill = ModManager.RegisterFeatName(IdPrepend + "ToughToKill", "Tough To Kill");
        public static readonly FeatName ReactionTime = ModManager.RegisterFeatName(IdPrepend + "ReactionTime", "Reaction Time");
        public static readonly FeatName GuardianMastery = ModManager.RegisterFeatName(IdPrepend + "GuardianMastery", "Guardian Mastery");
        #endregion
        
        #region Class Feats
        
        #region Level 1
        
        public static readonly FeatName Bodyguard = ModManager.RegisterFeatName(IdPrepend + "Bodyguard", "Bodyguard");
        public static readonly string BodyguardChargeChoice = IdPrepend + "BodyguardChargeChoice";
        public static readonly FeatName LargerThanLife = ModManager.RegisterFeatName(IdPrepend + "LargerThanLife", "Larger than Life");
        public static readonly FeatName LongDistanceTaunt = ModManager.RegisterFeatName(IdPrepend + "LongDistanceTaunt", "Long-distance Taunt");
        public static readonly FeatName PunishingShove = ModManager.RegisterFeatName(IdPrepend + "PunishingShove", "Punishing Shove");
        public static readonly FeatName ShieldWarfare = ModManager.RegisterFeatName(IdPrepend + "ShieldWarfare", "Shield Warfare");
        public static readonly FeatName ShoulderCheck = ModManager.RegisterFeatName(IdPrepend + "ShoulderCheck", "Shoulder Check");
        
        #endregion
        
        #region Level 2
        
        public static readonly FeatName CoveringStance = ModManager.RegisterFeatName(IdPrepend + "CoveringStance", "Covering Stance");
        public static readonly FeatName HamperingStance = ModManager.RegisterFeatName(IdPrepend + "HamperingStance", "Hampering Stance");
        public static readonly FeatName PhalanxFormation = ModManager.RegisterFeatName(IdPrepend + "PhalanxFormation", "Phalanx Formation");
        public static readonly FeatName RaiseHaft = ModManager.RegisterFeatName(IdPrepend + "RaiseHaft", "Raise Haft");
        public static readonly FeatName ShieldYourEyes = ModManager.RegisterFeatName(IdPrepend + "ShieldYourEyes", "Shield your Eyes");
        public static readonly FeatName ShieldingTaunt = ModManager.RegisterFeatName(IdPrepend + "ShieldingTaunt", "Shielding Taunt");
        public static readonly FeatName TauntingStrike = ModManager.RegisterFeatName(IdPrepend + "TauntingStrike", "Taunting Strike");
        
        #endregion
        
        #region Level 4
        
        public static readonly FeatName AreaArmor = ModManager.RegisterFeatName(IdPrepend + "AreaArmor", "Area Armor");
        public static readonly FeatName ArmoredCourage = ModManager.RegisterFeatName(IdPrepend + "ArmoredCourage", "Armored Courage");
        public static readonly FeatName EnergyInterceptor = ModManager.RegisterFeatName(IdPrepend + "EnergyInterceptor", "Energy Interceptor");
        public static readonly FeatName FlyingTackle = ModManager.RegisterFeatName(IdPrepend + "FlyingTackle", "FlyingTackle");
        public static readonly FeatName NotSoFast = ModManager.RegisterFeatName(IdPrepend + "NotSoFast", "Not so Fast");
        public static readonly FeatName ProudNail = ModManager.RegisterFeatName(IdPrepend + "ProudNail", "Proud Nail");
        public static readonly FeatName ShieldedAttrition = ModManager.RegisterFeatName(IdPrepend + "ShieldedAttrition", "Shielded Attrition");
        
        #endregion
        
        #region Level 6
        
        public static readonly FeatName DisarmingIntercept = ModManager.RegisterFeatName(IdPrepend + "DisarmingIntercept", "Disarming Intercept");
        public static readonly FeatName GuardedAdvance = ModManager.RegisterFeatName(IdPrepend + "GuardedAdvance", "Guarded Advance");
        public static readonly FeatName LockDown = ModManager.RegisterFeatName(IdPrepend + "LockDown", "Lock Down");
        public static readonly FeatName ReactiveStrike =
            ModManager.RegisterFeatName(IdPrepend + "ReactiveStrike", "Reactive Strike");
        // FeatName Reflexive Shield
        public static readonly FeatName RetaliatingRescue = ModManager.RegisterFeatName(IdPrepend + "RetaliatingRescue", "Retaliating Rescue");
        public static readonly FeatName RingTheirBell = ModManager.RegisterFeatName(IdPrepend + "RingTheirBell", "Ring their Bell");
        public static readonly FeatName StompGround = ModManager.RegisterFeatName(IdPrepend + "StompGround", "Stomp Ground");
        
        #endregion
        
        #region Level 8
        
        public static readonly FeatName GroupTaunt = ModManager.RegisterFeatName(IdPrepend + "GroupTaunt", "Group Taunt");
        public static readonly FeatName JuggernautCharge = ModManager.RegisterFeatName(IdPrepend + "JuggernautCharge", "Juggernaut Charge");
        public static readonly FeatName MightyBulwark = ModManager.RegisterFeatName(IdPrepend + "MightyBulwark", "Mighty Bulwark");
        public static readonly FeatName RepositioningBlock = ModManager.RegisterFeatName(IdPrepend + "RepositioningBlock", "Repositioning Block");
        public static readonly FeatName ShieldFromArrows = ModManager.RegisterFeatName(IdPrepend + "ShieldFromArrows", "Shield from Arrows");
        public static readonly FeatName ShieldWallop = ModManager.RegisterFeatName(IdPrepend + "ShieldWallop", "Shield Wallop");
        
        #endregion
        
        #region Level 10
        
        public static readonly FeatName BellyFlop = ModManager.RegisterFeatName(IdPrepend + "BellyFlop", "Belly Flop");
        public static readonly FeatName GetBehindMe = ModManager.RegisterFeatName(IdPrepend + "GetBehindMe", "Get Behind Me!");
        public static readonly FeatName MomentumStrike = ModManager.RegisterFeatName(IdPrepend + "MomentumStrike", "Momentum Strike");
        public static readonly FeatName ShieldSalvation = ModManager.RegisterFeatName(IdPrepend + "ShieldSalvation", "Shield Salvation");
        public static readonly FeatName SureFooted = ModManager.RegisterFeatName(IdPrepend + "SureFooted", "Sure-footed");
        public static readonly FeatName ToughCookie = ModManager.RegisterFeatName(IdPrepend + "ToughCookie", "Tough Cookie");
        
        #endregion
        
        #region Level 12
        
        public static readonly FeatName ArmorBreak = ModManager.RegisterFeatName(IdPrepend + "ArmorBreak", "Armor Break");
        public static readonly FeatName ArmoredCounterattack = ModManager.RegisterFeatName(IdPrepend + "ArmoredCounterattack", "Armored Counterattack");
        public static readonly FeatName DevastatingShieldWallop = ModManager.RegisterFeatName(IdPrepend + "DevastatingShieldWallop", "Devastating Shield Wallop");
        public static readonly FeatName ParagonsGuard = ModManager.RegisterFeatName(IdPrepend + "ParagonsGuard", "Paragon's Guard");
        public static readonly FeatName RightWhereYouWantThem = ModManager.RegisterFeatName(IdPrepend + "RightWhereYouWantThem", "Right Where You Want Them");
        public static readonly FeatName ScatteringCharge = ModManager.RegisterFeatName(IdPrepend + "ScatteringCharge", "Scattering Charge");
        public static readonly FeatName WeakeningAssault = ModManager.RegisterFeatName(IdPrepend + "WeakeningAssault", "Weakening Assault");
        
        #endregion
        
        #region Level 14
        
        public static readonly FeatName BlanketDefense = ModManager.RegisterFeatName(IdPrepend + "BlanketDefense", "Blanket Defense");
        public static readonly FeatName BloodyDenial = ModManager.RegisterFeatName(IdPrepend + "BloodyDenial", "Bloody Denial");
        public static readonly FeatName KeepUpTheGoodFight = ModManager.RegisterFeatName(IdPrepend + "KeepUpTheGoodFight", "Keep up the Good Fight");
        public static readonly FeatName OpeningStance = ModManager.RegisterFeatName(IdPrepend + "OpeningStance", "Opening Stance");
        
        #endregion
        
        #region Level 16
        
        public static readonly FeatName Clang = ModManager.RegisterFeatName(IdPrepend + "Clang", "Clang!");
        public static readonly FeatName Clobber = ModManager.RegisterFeatName(IdPrepend + "Clobber", "Clobber");
        public static readonly FeatName ImprovedReflexiveShield = ModManager.RegisterFeatName(IdPrepend + "ImprovedReflexiveShield", "Improved Reflexive Shield");
        public static readonly FeatName Never = ModManager.RegisterFeatName(IdPrepend + "Never!", "Never!");
        
        #endregion
        
        #region Level 18
        
        public static readonly FeatName DemolishDefenses = ModManager.RegisterFeatName(IdPrepend + "DemolishDefenses", "Demolish Defenses");
        public static readonly FeatName PerfectProtection = ModManager.RegisterFeatName(IdPrepend + "PerfectProtection", "Perfect Protection");
        public static readonly FeatName QuickVengeance = ModManager.RegisterFeatName(IdPrepend + "QuickVengeance", "Quick Vengeance");
        public static readonly FeatName ShieldFromSpells = ModManager.RegisterFeatName(IdPrepend + "ShieldFromSpells", "Shield From Spells");
        
        #endregion
        
        #region Level 20
        
        public static readonly FeatName BoundlessReprisals = ModManager.RegisterFeatName(IdPrepend + "BoundlessReprisals", "Boundless Reprisals");
        public static readonly FeatName GreatShieldMastery = ModManager.RegisterFeatName(IdPrepend + "GreatShieldMastery", "Great Shield Mastery");
        public static readonly FeatName UnyieldingForce = ModManager.RegisterFeatName(IdPrepend + "UnyieldingForce", "Unyielding Force");
        
        #endregion
        
        #endregion
    }

    public static class Illustrations
    {
        public const string ModFolder = "GuardianClassAssets/";

        #region Class Features
        
        public static readonly Illustration Taunt_1 = new ModdedIllustration(ModFolder+"intimidation_1.png");
        public static readonly Illustration Taunt_3 = new ModdedIllustration(ModFolder+"intimidation.png");
        public static readonly Illustration InterceptAttack = new ModdedIllustration(ModFolder+"intercept attack.png");
        
        #endregion
        
        #region Class Feats
        
        public static readonly Illustration ArmoredCourage = new ModdedIllustration(ModFolder+"armor-upgrade 2.png");
        public static readonly Illustration StompGround = new ModdedIllustration(ModFolder+"quake-stomp.png");
        public static readonly Illustration HamperingStance = new ModdedIllustration(ModFolder+"banana-peel + hot-surface.png");
        public static readonly Illustration LockDown = new ModdedIllustration(ModFolder+"foot-trip.png");
        public static readonly Illustration ToughCookie = IllustrationName.Enlarge;
        public static readonly Illustration ArmoredCounterattack = new CornerIllustration(InterceptAttack, IllustrationName.StarHit, Direction.Southeast);
        public static readonly Illustration KeepUpTheGoodFight = IllustrationName.WinningStreak;
        
        #endregion
        
        #region Misc
        
        public static readonly Illustration NoSymbol = new ModdedIllustration(ModFolder+"no symbol.png");
        public static readonly Illustration CheckSymbol = new ModdedIllustration(ModFolder+"check symbol.png");
        public static readonly Illustration DawnsburySun = new ModdedIllustration(ModFolder+"PatreonSunTransparent.png");
        
        #endregion
    }

    public static class PersistentActions
    {
        public const string ToughToKill = "ToughToKill";
        public const string ToughCookie = "ToughCookie";
    }
    
    public static class PossibilityGroups
    {
        public const string TauntActions = "Taunt Actions";
    }
    
    public static class PossibilitySectionIds
    {
        public static PossibilitySectionId BasicTaunts;
        public static PossibilitySectionId InterceptAttackToggles;
        
        public static void Initialize()
        {
            BasicTaunts = SafelyRegister<PossibilitySectionId>("BasicTaunts");
            InterceptAttackToggles = SafelyRegister<PossibilitySectionId>("InterceptAttackToggles");
        }
    }
    
    public static class QEffectIds
    {
        public static QEffectId TauntTarget;
        public static QEffectId ReactionTime;
        public static QEffectId BodyguardCharge;
        public static QEffectId HamperingStance;
        
        public static void Initialize()
        {
            TauntTarget = SafelyRegister<QEffectId>("TauntTarget");
            ReactionTime = SafelyRegister<QEffectId>("ReactionTime");
            BodyguardCharge = SafelyRegister<QEffectId>("Bodyguard's Charge");
            HamperingStance = SafelyRegister<QEffectId>("HamperingStance");
        }
    }
    
    public static class SfxNames
    {
        //public const SfxName TraceRune = SfxName.AncientDust;
        public static readonly Func<Creature, Trait[], SfxName> Taunt = (taunter, traits) =>
            traits.Contains(Trait.Auditory)
                ? taunter.HasTrait(Trait.Female) ? SfxName.Intimidate : SfxName.MaleIntimidate
                : traits.Contains(Trait.Visual)
                    ? SfxName.Feint
                    : SfxName.OpenPage;
    }
    
    public static class SubmenuIds
    {
        public static SubmenuId Taunt;
        
        public static void Initialize()
        {
            Taunt = SafelyRegister<SubmenuId>("Taunt");
        }
    }
    
    public static class Traits
    {
        public static readonly Trait ModSource = ModManager.RegisterModNameTrait("GuardianClass", "Guardian Class");
        
        /// <summary>Guardian class trait.</summary>
        public static readonly Trait Guardian = ModManager.RegisterTrait("Guardian", 
            new TraitProperties("Guardian", true) { IsClassTrait = true });
    
        #region Feats
        
        public static readonly Trait BodyguardCharge = ModManager.RegisterTrait("Bodyguard's Charge",
            new TraitProperties("Bodyguard's Charge", false));
        public static readonly Trait NotSoFastAttack = ModManager.RegisterTrait("NotSoFastAttack",
            new TraitProperties("NotSoFastAttack", false));
        
        #endregion
        
        #region Precombat Preparations
        
        public static readonly Trait InterceptAttackToggle = ModManager.RegisterTrait("InterceptAttackToggle", new TraitProperties("Intercept Attack Toggle", false));
        
        #endregion
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> CommonDamageTypesRemastered = RegisterTooltipInserter(
            IdPrepend + "Common.DamageTypesRemastered",
            """
            {b}Damage Types{/b}
            {i}Core rule{/i}
            Most damage falls into one of the following types:
            • {b}Physical{/b} bludgeoning, piercing, slashing; bleed.
            • {b}Energy{/b} acid, cold, electricity, fire, sonic; vitality, void.
            • {b}Spirit{/b} spirit.
            • {b}Mental{/b} mental.
            • {b}Poison{/b} poison.
            • {b}Precision{/b} precision.
            • {b}Precious Materials{/b} adamantine, cold iron, silver
            """);
        public static readonly Func<string, string> CommonWeaponSpec = RegisterTooltipInserter(
            IdPrepend + "Common.WeaponSpecialization",
            """
            {b}Weapon Specialization{/b}
            {i}Common class feature{/i}
            You deal 2 additional damage with weapons and unarmed attacks in which you are an expert; this damage increases to 3 if you're a master, and to 4 if you're legendary.
            """);
        public static readonly Func<string, string> CommonGreaterWeaponSpec = RegisterTooltipInserter(
            IdPrepend + "Common.GreaterWeaponSpecialization",
            """
            {b}Greater Weapon Specialization{/b}
            {i}Common class feature{/i}
            Your damage from weapon specialization increases to 4 with weapons and unarmed attacks in which you're expert, 6 if you're a master, and 8 if you're legendary.
            """);
        public static readonly Func<string, string> ActionTaunt = RegisterTooltipInserter(
            IdPrepend + "Actions.Taunt",
            """
            {b}Taunt{/b} {icon:Action}
            {i}Concentrate, (Visual or Auditory){/i}
            Choose an enemy within 30 feet to be your taunted enemy. If your taunted enemy takes a hostile action that includes at least one of your allies but doesn't include you, they take a –1 circumstance penalty to their attack rolls and DCs for that action, and they also become off-guard until the start of their next turn.

            Your enemy remains taunted until the start of your next turn, and you can have only one Taunt in effect at a time. Taunting a new enemy ends this effect on any current target.

            Taunt gains the auditory trait, visual trait, or both, depending on how you draw the target's attention.
            """);
        public static readonly Func<string, string> ActionInterceptAttack = RegisterTooltipInserter(
            IdPrepend + "Actions.InterceptAttack",
            """
            {b}Intercept Attack{/b} {icon:Reaction}
            {b}Trigger{/b} An ally within 10 feet of you takes physical damage.

            You can Step, but you must end your movement adjacent to the triggering ally. You take the damage instead of the triggering ally. Apply your own immunities, weaknesses, and resistances to the damage, not the ally's.

            {b}Special{/b} You can extend this ability to an ally within 15 feet of you if the damage comes from your taunted enemy. If this ally is farther than you can Step to reach, you can Stride instead of Stepping; you still must end the movement adjacent to your ally.
            """);
        public static readonly Func<string, string> ArmorResting = RegisterTooltipInserter(
            IdPrepend + "Feature.GuardiansArmorResting",
            """
            {b}Resting in Armor{/b}
            {i}Common rule{/i}
            Sleeping in armor is uncomfortable, and would lead to poor-quality sleep. Some encounters occur while the party is sleeping. If you aren't able to sleep in armor, you won't have your armor donned at the start of combat.
            """);
        public static readonly Func<string, string> FeatureToughToKill = RegisterTooltipInserter(
            IdPrepend + "Feature.ToughToKill",
            """
            {b}Tough to Kill{/b}
            {i}Level 3 Guardian feature{/i}
            You gain the Diehard general feat {i}(you should retrain it if you already have it){/i}. Additionally, the first time each day you'd be reduced to dying 3 or higher, you stay at dying 2 instead.
            """);
        public static readonly Func<string, string> FeatureReactionTime = RegisterTooltipInserter(
            IdPrepend + "Feature.ReactionTime",
            """
            {b}Reaction Time{/b}
            {i}Level 7 Guardian feature{/i}
            At the start of combat and for each of your turns, you gain an additional reaction that you can use only for reactions from guardian feats or class features (including Shield Block).
            """);
        public static readonly Func<string, string> FeatureBattleHardened = RegisterTooltipInserter(
            IdPrepend + "Feature.BattleHardened",
            """
            {b}Battle Hardened{/b}
            {i}Level 9 Guardian feature{/i}
            Your proficiency rank for Fortitude saves increases to master; when you roll a success on a Fortitude save, you get a critical success instead.
            """);
        public static readonly Func<string, string> FeatureUnyieldingResolve = RegisterTooltipInserter(
            IdPrepend + "Feature.UnyieldingResolve",
            """
            {b}Unyielding Resolve{/b}
            {i}Level 17 Guardian feature{/i}
            Your proficiency rank for Will saves increases to master; when you roll a success on a Will save, you get a critical success instead.
            """);
        public static readonly Func<string, string> FeatureGuardianMastery = RegisterTooltipInserter(
            IdPrepend + "Feature.GuardianMastery",
            """
            {b}Guardian Mastery{/b}
            {i}Level 19 Guardian feature{/i}
            While wearing armor, when you attempt a Reflex save, you can add your armor's item bonus to AC instead of your Dexterity modifier if it's higher; if your armor has the bulwark trait, increase this bonus by 1. If you get a success when you do this, you get a critical success instead.
            """);
        
        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }
    }
}