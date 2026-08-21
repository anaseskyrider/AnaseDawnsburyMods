using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
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
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.GuardianClass;

public static class ModData
{
    public const string ID_PREPEND = "GuardianClass.";

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
        PossibilitySectionIds.Initialize();
        QEffectIds.Initialize();
        SubmenuIds.Initialize();
    }

    public static class ActionIds
    {
        public static ActionId Taunt;
        public static ActionId InterceptAttack;
        
        public static void Initialize()
        {
            Taunt = ModManager.SafelyRegisterEnumMember<ActionId>("Taunt");
            InterceptAttack = ModManager.SafelyRegisterEnumMember<ActionId>("InterceptAttack");
        }
    }

    public static class CommonQfKeys
    {
        /// <summary>
        /// This key includes the name of the Guardian who inflicted it -- search using this string + Creature.Name.
        /// </summary>
        public const string TAUNTED_ENEMY = "Taunt";

        /// <summary>This key includes the name of the Guardian who inflicted it -- search using this name + Creature.Name. Searching for the source of this effect also ensures that the creature is off-guard due to ignoring YOUR Taunt.</summary>
        public const string OFF_GUARD_DUE_TO_TAUNT = "TauntOffGuard";

        /// <summary>This key includes the name of the Guardian who inflicted it -- search using this name + Creature.Name. This QF should also include the Guardian as a Source.</summary>
        public const string SHIELDED_ATTRITION = "ShieldedAttrition:";
    }

    public static class CommonReactionKeys
    {
        public const string REACTION_TIME = "ReactionTime";
    }

    public static class CommonRequirements
    {
        public static CreatureTargetingRequirement MustWearMediumOrHeavyArmor()
        {
            return new LegacyCreatureTargetingRequirement((a,_) =>
                IsWearingMediumOrHeavyArmor(a)
                    ? Usability.Usable
                    : Usability.NotUsable("Must be wearing medium or heavy armor"));
        }

        public static CreatureTargetingRequirement IsMyTauntedEnemy()
        {
            return new LegacyCreatureTargetingRequirement((a, d) =>
                d.QEffects.Any(qf =>
                    qf.Id == ModData.QEffectIds.TauntTarget
                    && qf.Key == ModData.CommonQfKeys.TAUNTED_ENEMY + a.Name)
                    ? Usability.Usable
                    : Usability.NotUsableOnThisCreature("Not your taunted enemy"));
        }

        public static CreatureTargetingRequirement OffGuardDueToMyTaunt()
        {
            return new LegacyCreatureTargetingRequirement((a, d) =>
                d.QEffects.Any(qf => qf.Key == CommonQfKeys.OFF_GUARD_DUE_TO_TAUNT+a.Name)
                    ? Usability.Usable
                    : Usability.NotUsableOnThisCreature("Hasn't ignored your Taunt"));
        }

        public static bool IsWearingMediumOrHeavyArmor(Creature cr)
        {
            return (cr.Armor.Item ?? cr.BaseArmor) is {} armor
                   && armor.Traits.ContainsOneOf([Trait.MediumArmor, Trait.HeavyArmor]);
        }

        public static Item? GetMediumOrHeavyArmor(Creature cr)
        {
            return (cr.Armor.Item ?? cr.BaseArmor) is { } armor
                   && armor.Traits.ContainsOneOf([Trait.MediumArmor, Trait.HeavyArmor])
                ? armor
                : null;
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
        
        public static readonly FeatName GuardianClass = ModManager.RegisterFeatName(ID_PREPEND + "GuardianClass", "Guardian");
        
        #endregion

        #region Class Archetype
        
        public static FeatName GuardianDedication; // Data filled in GuardianArchetype.LoadArchetype().
        public static readonly FeatName GuardiansIntercept = ModManager.RegisterFeatName(ID_PREPEND + "GuardiansIntercept", "Guardian's Intercept");
        public static readonly FeatName ArmoredResistance = ModManager.RegisterFeatName(ID_PREPEND + "ArmoredResistance", "Armored Resistance");
        public static readonly FeatName IroncladFortitude = ModManager.RegisterFeatName(ID_PREPEND + "IroncladFortitude", "Ironclad Fortitude");

        #endregion
        
        #region Intercept Attack Toggles

        public static readonly FeatName InterceptToggleAlreadyReducedDamage = ModManager.RegisterFeatName(ID_PREPEND + "InterceptToggleAlreadyReducedDamage", "Already-reduced damage");
        public static readonly FeatName InterceptToggleCompanions = ModManager.RegisterFeatName(ID_PREPEND + "InterceptToggleCompanions", "Companions");
        public static readonly FeatName InterceptToggleCrits = ModManager.RegisterFeatName(ID_PREPEND + "InterceptToggleCrits", "Critical Hits");
        public static readonly FeatName InterceptToggleKO = ModManager.RegisterFeatName(ID_PREPEND + "InterceptToggleKO", "Knockout Hits");
        public static readonly FeatName InterceptToggleHits = ModManager.RegisterFeatName(ID_PREPEND + "InterceptToggleHits", "Normal Hits");
        public static readonly FeatName InterceptToggleSummons = ModManager.RegisterFeatName(ID_PREPEND + "InterceptToggleSummons", "Summons");
        
        #endregion
        
        #region Class Features
        public static readonly FeatName GuardiansArmor = ModManager.RegisterFeatName(ID_PREPEND + "GuardiansArmor", "Guardian's Armor");
        public static readonly FeatName Taunt = ModManager.RegisterFeatName(ID_PREPEND + "Taunt", "Taunt {icon:Action}");
        public static readonly FeatName InterceptAttack = ModManager.RegisterFeatName(ID_PREPEND + "InterceptAttack", "Intercept Attack {icon:Reaction}");
        public static readonly FeatName ToughToKill = ModManager.RegisterFeatName(ID_PREPEND + "ToughToKill", "Tough To Kill");
        public static readonly FeatName ReactionTime = ModManager.RegisterFeatName(ID_PREPEND + "ReactionTime", "Reaction Time");
        public static readonly FeatName BattleHardened = ModManager.RegisterFeatName(ID_PREPEND + "BattleHardened", "Battle Hardened");
        public static readonly FeatName UnyieldingResolve = ModManager.RegisterFeatName(ID_PREPEND + "UnyieldingResolve", "Unyielding Resolve");
        public static readonly FeatName GuardianMastery = ModManager.RegisterFeatName(ID_PREPEND + "GuardianMastery", "Guardian Mastery");
        #endregion
        
        #region Class Feats
        
        #region Level 1
        
        public static readonly FeatName Bodyguard = ModManager.RegisterFeatName(ID_PREPEND + "Bodyguard", "Bodyguard");
        public static readonly string BodyguardChargeChoice = ID_PREPEND + "BodyguardChargeChoice";
        public static readonly FeatName LargerThanLife = ModManager.RegisterFeatName(ID_PREPEND + "LargerThanLife", "Larger than Life");
        public static readonly FeatName LongDistanceTaunt = ModManager.RegisterFeatName(ID_PREPEND + "LongDistanceTaunt", "Long-distance Taunt");
        public static readonly FeatName PunishingShove = ModManager.RegisterFeatName(ID_PREPEND + "PunishingShove", "Punishing Shove");
        public static readonly FeatName ShieldWarfare = ModManager.RegisterFeatName(ID_PREPEND + "ShieldWarfare", "Shield Warfare");
        public static readonly FeatName ShoulderCheck = ModManager.RegisterFeatName(ID_PREPEND + "ShoulderCheck", "Shoulder Check");
        
        #endregion
        
        #region Level 2
        
        public static readonly FeatName CoveringStance = ModManager.RegisterFeatName(ID_PREPEND + "CoveringStance", "Covering Stance");
        public static readonly FeatName HamperingStance = ModManager.RegisterFeatName(ID_PREPEND + "HamperingStance", "Hampering Stance");
        public static readonly FeatName PhalanxFormation = ModManager.RegisterFeatName(ID_PREPEND + "PhalanxFormation", "Phalanx Formation");
        public static readonly FeatName RaiseHaft = ModManager.RegisterFeatName(ID_PREPEND + "RaiseHaft", "Raise Haft");
        public static readonly FeatName ShieldYourEyes = ModManager.RegisterFeatName(ID_PREPEND + "ShieldYourEyes", "Shield your Eyes");
        public static readonly FeatName ShieldingTaunt = ModManager.RegisterFeatName(ID_PREPEND + "ShieldingTaunt", "Shielding Taunt");
        public static readonly FeatName TauntingStrike = ModManager.RegisterFeatName(ID_PREPEND + "TauntingStrike", "Taunting Strike");
        
        #endregion
        
        #region Level 4
        
        public static readonly FeatName AreaArmor = ModManager.RegisterFeatName(ID_PREPEND + "AreaArmor", "Area Armor");
        public static readonly FeatName ArmoredCourage = ModManager.RegisterFeatName(ID_PREPEND + "ArmoredCourage", "Armored Courage");
        public static readonly FeatName EnergyInterceptor = ModManager.RegisterFeatName(ID_PREPEND + "EnergyInterceptor", "Energy Interceptor");
        public static readonly FeatName FlyingTackle = ModManager.RegisterFeatName(ID_PREPEND + "FlyingTackle", "Flying Tackle");
        public static readonly FeatName NotSoFast = ModManager.RegisterFeatName(ID_PREPEND + "NotSoFast", "Not so Fast!");
        public static readonly FeatName ProudNail = ModManager.RegisterFeatName(ID_PREPEND + "ProudNail", "Proud Nail");
        public static readonly FeatName ShieldedAttrition = ModManager.RegisterFeatName(ID_PREPEND + "ShieldedAttrition", "Shielded Attrition");
        
        #endregion
        
        #region Level 6
        
        public static readonly FeatName DisarmingIntercept = ModManager.RegisterFeatName(ID_PREPEND + "DisarmingIntercept", "Disarming Intercept");
        public static readonly FeatName GuardedAdvance = ModManager.RegisterFeatName(ID_PREPEND + "GuardedAdvance", "Guarded Advance");
        public static readonly FeatName LockDown = ModManager.RegisterFeatName(ID_PREPEND + "LockDown", "Lock Down");
        public static readonly FeatName ReactiveStrike =
            ModManager.RegisterFeatName(ID_PREPEND + "ReactiveStrike", "Reactive Strike");
        // FeatName Reflexive Shield
        public static readonly FeatName RetaliatingRescue = ModManager.RegisterFeatName(ID_PREPEND + "RetaliatingRescue", "Retaliating Rescue");
        public static readonly FeatName RingTheirBell = ModManager.RegisterFeatName(ID_PREPEND + "RingTheirBell", "Ring their Bell");
        public static readonly FeatName StompGround = ModManager.RegisterFeatName(ID_PREPEND + "StompGround", "Stomp Ground");
        
        #endregion
        
        #region Level 8
        
        public static readonly FeatName GroupTaunt = ModManager.RegisterFeatName(ID_PREPEND + "GroupTaunt", "Group Taunt");
        public static readonly FeatName JuggernautCharge = ModManager.RegisterFeatName(ID_PREPEND + "JuggernautCharge", "Juggernaut Charge");
        public static readonly FeatName RepositioningBlock = ModManager.RegisterFeatName(ID_PREPEND + "RepositioningBlock", "Repositioning Block");
        public static readonly FeatName ShieldFromArrows = ModManager.RegisterFeatName(ID_PREPEND + "ShieldFromArrows", "Shield from Arrows");
        public static readonly FeatName ShieldWallop = ModManager.RegisterFeatName(ID_PREPEND + "ShieldWallop", "Shield Wallop");
        
        #endregion
        
        #region Level 10
        
        public static readonly FeatName BellyFlop = ModManager.RegisterFeatName(ID_PREPEND + "BellyFlop", "Belly Flop");
        public static readonly FeatName GetBehindMe = ModManager.RegisterFeatName(ID_PREPEND + "GetBehindMe", "Get Behind Me!");
        public static readonly FeatName MomentumStrike = ModManager.RegisterFeatName(ID_PREPEND + "MomentumStrike", "Momentum Strike");
        public static readonly FeatName ShieldSalvation = ModManager.RegisterFeatName(ID_PREPEND + "ShieldSalvation", "Shield Salvation");
        public static readonly FeatName SureFooted = ModManager.RegisterFeatName(ID_PREPEND + "SureFooted", "Sure-Footed");
        public static readonly FeatName ToughCookie = ModManager.RegisterFeatName(ID_PREPEND + "ToughCookie", "Tough Cookie");
        
        #endregion
        
        #region Level 12
        
        public static readonly FeatName ArmorBreak = ModManager.RegisterFeatName(ID_PREPEND + "ArmorBreak", "Armor Break");
        public static readonly FeatName ArmoredCounterattack = ModManager.RegisterFeatName(ID_PREPEND + "ArmoredCounterattack", "Armored Counterattack");
        public static readonly FeatName DevastatingShieldWallop = ModManager.RegisterFeatName(ID_PREPEND + "DevastatingShieldWallop", "Devastating Shield Wallop");
        public static readonly FeatName ParagonsGuard = ModManager.RegisterFeatName(ID_PREPEND + "ParagonsGuard", "Paragon's Guard");
        public static readonly FeatName RightWhereYouWantThem = ModManager.RegisterFeatName(ID_PREPEND + "RightWhereYouWantThem", "Right Where You Want Them");
        public static readonly FeatName ScatteringCharge = ModManager.RegisterFeatName(ID_PREPEND + "ScatteringCharge", "Scattering Charge");
        public static readonly FeatName WeakeningAssault = ModManager.RegisterFeatName(ID_PREPEND + "WeakeningAssault", "Weakening Assault");
        
        #endregion
        
        #region Level 14
        
        public static readonly FeatName BlanketDefense = ModManager.RegisterFeatName(ID_PREPEND + "BlanketDefense", "Blanket Defense");
        public static readonly FeatName BloodyDenial = ModManager.RegisterFeatName(ID_PREPEND + "BloodyDenial", "Bloody Denial");
        public static readonly FeatName KeepUpTheGoodFight = ModManager.RegisterFeatName(ID_PREPEND + "KeepUpTheGoodFight", "Keep up the Good Fight");
        public static readonly FeatName OpeningStance = ModManager.RegisterFeatName(ID_PREPEND + "OpeningStance", "Opening Stance");
        
        #endregion
        
        #region Level 16
        
        public static readonly FeatName Clang = ModManager.RegisterFeatName(ID_PREPEND + "Clang", "Clang!");
        public static readonly FeatName Clobber = ModManager.RegisterFeatName(ID_PREPEND + "Clobber", "Clobber");
        public static readonly FeatName ImprovedReflexiveShield = ModManager.RegisterFeatName(ID_PREPEND + "ImprovedReflexiveShield", "Improved Reflexive Shield");
        public static readonly FeatName Never = ModManager.RegisterFeatName(ID_PREPEND + "Never!", "Never!");
        
        #endregion
        
        #region Level 18
        
        public static readonly FeatName DemolishDefenses = ModManager.RegisterFeatName(ID_PREPEND + "DemolishDefenses", "Demolish Defenses");
        public static readonly FeatName PerfectProtection = ModManager.RegisterFeatName(ID_PREPEND + "PerfectProtection", "Perfect Protection");
        public static readonly FeatName QuickVengeance = ModManager.RegisterFeatName(ID_PREPEND + "QuickVengeance", "Quick Vengeance");
        public static readonly FeatName ShieldFromSpells = ModManager.RegisterFeatName(ID_PREPEND + "ShieldFromSpells", "Shield From Spells");
        
        #endregion
        
        #region Level 20
        
        public static readonly FeatName BoundlessReprisals = ModManager.RegisterFeatName(ID_PREPEND + "BoundlessReprisals", "Boundless Reprisals");
        public static readonly FeatName GreatShieldMastery = ModManager.RegisterFeatName(ID_PREPEND + "GreatShieldMastery", "Great Shield Mastery");
        public static readonly FeatName UnyieldingForce = ModManager.RegisterFeatName(ID_PREPEND + "UnyieldingForce", "Unyielding Force");
        
        #endregion
        
        #endregion
    }

    public static class Illustrations
    {
        public const string MOD_FOLDER = "GuardianClassAssets/";

        #region Class Features
        
        public static readonly Illustration Taunt_1 = new ModdedIllustration(MOD_FOLDER+"intimidation_1.png");
        public static readonly Illustration Taunt_3 = new ModdedIllustration(MOD_FOLDER+"intimidation.png");
        public static readonly Illustration InterceptAttack = new ModdedIllustration(MOD_FOLDER+"intercept attack.png");
        public static readonly Illustration ToughToKill = IllustrationName.WinningStreak;
        
        #endregion
        
        #region Class Feats
        
        public static readonly Illustration ArmoredCourage = new ModdedIllustration(MOD_FOLDER+"armor-upgrade 2.png");
        public static readonly Illustration StompGround = new ModdedIllustration(MOD_FOLDER+"quake-stomp.png");
        public static readonly Illustration CoveringStance = IllustrationName.Protection;
        public static readonly Illustration HamperingStance = new ModdedIllustration(MOD_FOLDER+"banana-peel + hot-surface.png");
        public static readonly Illustration LockDown = new ModdedIllustration(MOD_FOLDER+"foot-trip.png");
        public static readonly Illustration GetBehindMe = IllustrationName.FleetStep;
        public static readonly Illustration ToughCookie = IllustrationName.Enlarge;
        public static readonly Illustration ArmoredCounterattack = new CornerIllustration(InterceptAttack, IllustrationName.StarHit, Direction.Southeast);
        public static readonly Illustration ScatteringCharge = new SideBySideIllustration(IllustrationName.FleetStep, IllustrationName.Shove);
        public static readonly Illustration WeakeningAssault = new CornerIllustration(new SideBySideIllustration(IllustrationName.Swipe, IllustrationName.Swipe), IllustrationName.Enfeebled, Direction.Southeast);
        public static readonly Illustration KeepUpTheGoodFight = IllustrationName.WinningStreak;
        public static readonly Illustration OpeningStance = IllustrationName.RemoveConfusion;
        
        #endregion
        
        #region Misc
        
        public static readonly Illustration NoSymbol = new ModdedIllustration(MOD_FOLDER+"no symbol.png");
        public static readonly Illustration CheckSymbol = new ModdedIllustration(MOD_FOLDER+"check symbol.png");
        /// <summary>
        /// Used to indicate an information tooltip such as documented changes from tabletop.
        /// </summary>
        public static readonly Illustration InfoSymbol = new ModdedIllustration(MOD_FOLDER+"information_(raised).png");
        public static readonly Illustration DdSun = new ModdedIllustration(MOD_FOLDER+"PatreonSunTransparent.png");
        
        #endregion
    }

    public static class PersistentActions
    {
        public const string TOUGH_TO_KILL = "ToughToKill";
        public const string TOUGH_COOKIE = "ToughCookie";
        public const string GUARDIANS_INTERCEPT = "GuardiansIntercept";
    }
    
    public static class PossibilityGroups
    {
        public const string TAUNT_ACTIONS = "Taunt Actions";
    }
    
    public static class PossibilitySectionIds
    {
        public static PossibilitySectionId BasicTaunts;
        public static PossibilitySectionId TauntActivities;
        public static PossibilitySectionId InterceptAttackToggles;
        
        public static void Initialize()
        {
            BasicTaunts = ModManager.SafelyRegisterEnumMember<PossibilitySectionId>("BasicTaunts");
            TauntActivities = ModManager.SafelyRegisterEnumMember<PossibilitySectionId>("TauntActivities");
            InterceptAttackToggles = ModManager.SafelyRegisterEnumMember<PossibilitySectionId>("InterceptAttackToggles");
        }
    }
    
    public static class QEffectIds
    {
        public static QEffectId TauntTarget;
        public static QEffectId ReactionTime;
        public static QEffectId BodyguardCharge;
        public static QEffectId CoveringStance;
        public static QEffectId HamperingStance;
        
        public static void Initialize()
        {
            TauntTarget = ModManager.SafelyRegisterEnumMember<QEffectId>("TauntTarget");
            ReactionTime = ModManager.SafelyRegisterEnumMember<QEffectId>("ReactionTime");
            BodyguardCharge = ModManager.SafelyRegisterEnumMember<QEffectId>("Bodyguard's Charge");
            CoveringStance = ModManager.SafelyRegisterEnumMember<QEffectId>("CoveringStance");
            HamperingStance = ModManager.SafelyRegisterEnumMember<QEffectId>("HamperingStance");
        }
    }
    
    public static class SfxNames
    {
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
            Taunt = ModManager.SafelyRegisterEnumMember<SubmenuId>("Taunt");
        }
    }
    
    public static class Traits
    {
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
            ID_PREPEND + "Common.DamageTypesRemastered",
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
        public static readonly Func<string, string> ArmorResting = RegisterTooltipInserter(
            ID_PREPEND + "Feature.GuardiansArmorResting",
            """
            {b}Resting in Armor{/b}
            {i}Common rule{/i}
            Sleeping in armor is uncomfortable, and would lead to poor-quality sleep. Some encounters occur while the party is sleeping. If you aren't able to sleep in armor, you won't have your armor donned at the start of combat.
            """);
        
        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }
    }
}