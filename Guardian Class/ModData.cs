using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.GuardianClass;

// TODO: Cleanup ModData
public static class ModData
{
    public static void LoadData()
    {
        // Register Mod Options
        /*ModManager.RegisterBooleanSettingsOption(ModData.BooleanOptions.UnrestrictedTrace,
            "Runesmith: Less Restrictive Rune Tracing",
            "Enabling this option removes protections against \"bad decisions\" with tracing certain runes on certain targets.\n\nThe Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.\n\nThis option is for those players.",
            true);*/
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> CommonWeaponSpec = RegisterTooltipInserter(
            "Guardian.Common.WeaponSpecialization",
            "{b}Weapon Specialization{/b}\n{i}Common class feature{/i}\nYou deal 2 additional damage with weapons and unarmed attacks in which you are an expert; this damage increases to 3 if you're a master, and to 4 if you're legendary.");
        public static readonly Func<string, string> CommonGreaterWeaponSpec = RegisterTooltipInserter(
            "Guardian.Common.GreaterWeaponSpecialization",
            "{b}Greater Weapon Specialization{/b}\n{i}Common class feature{/i}\nYour damage from weapon specialization increases to 4 with weapons and unarmed attacks in which you're expert, 6 if you're a master, and 8 if you're legendary.");
        public static readonly Func<string, string> ActionTaunt = RegisterTooltipInserter(
            "Guardian.Actions.Taunt",
            "{b}Taunt{/b} {icon:Action}\n{i}Concentrate, (Visual or Auditory){/i}\nChoose an enemy within 30 feet to be your taunted enemy. If your taunted enemy takes a hostile action that includes at least one of your allies but doesn't include you, they take a –1 circumstance penalty to their attack rolls and DCs for that action, and they also become off-guard until the start of their next turn.\n\nYour enemy remains taunted until the start of your next turn, and you can have only one Taunt in effect at a time. Taunting a new enemy ends this effect on any current target.\n\nTaunt gains the auditory trait, visual trait, or both, depending on how you draw the target's attention.");
        public static readonly Func<string, string> ActionInterceptAttack = RegisterTooltipInserter(
            "Guardian.Actions.InterceptAttack",
            "{b}Intercept Attack{/b} {icon:Reaction}\n{b}Trigger{/b} An ally within 10 feet of you takes physical damage.\n\nYou can Step, but you must end your movement adjacent to the triggering ally. You take the damage instead of the triggering ally. Apply your own immunities, weaknesses, and resistances to the damage, not the ally's.\n\n{b}Special{/b} You can extend this ability to an ally within 15 feet of you if the damage comes from your taunted enemy. If this ally is farther than you can Step to reach, you can Stride instead of Stepping; you still must end the movement adjacent to your ally.");
        public static readonly Func<string, string> ArmorResting = RegisterTooltipInserter(
            "Guardian.Feature.GuardiansArmorResting",
            "{b}Resting in Armor{/b}\n{i}Common rule{/i}\nSleeping in armor is uncomfortable, and would lead to poor-quality sleep. Some encounters occur while the party is sleeping. If you aren't able to sleep in armor, you won't have your armor donned at the start of combat.");
        public static readonly Func<string, string> FeatureToughToKill = RegisterTooltipInserter(
            "Guardian.Feature.ToughToKill",
            "{b}Tough to Kill{/b}\n{i}Level 3 Guardian feature{/i}\nYou gain the Diehard general feat {i}(you should retrain it if you already have it){/i}. Additionally, the first time each day you'd be reduced to dying 3 or higher, you stay at dying 2 instead.");
        public static readonly Func<string, string> FeatureReactionTime = RegisterTooltipInserter(
            "Guardian.Feature.ReactionTime",
            "{b}Reaction Time{/b}\n{i}Level 7 Guardian feature{/i}\nAt the start of combat and for each of your turns, you gain an additional reaction that you can use only for reactions from guardian feats or class features (including Shield Block).");
        public static readonly Func<string, string> FeatureBattleHardened = RegisterTooltipInserter(
            "Guardian.Feature.BattleHardened",
            "{b}Battle Hardened{/b}\n{i}Level 9 Guardian feature{/i}\nYour proficiency rank for Fortitude saves increases to master; when you roll a success on a Fortitude save, you get a critical success instead.");
        public static readonly Func<string, string> FeatureUnyieldingResolve = RegisterTooltipInserter(
            "Guardian.Feature.UnyieldingResolve",
            "{b}Unyielding Resolve{/b}\n{i}Level 17 Guardian feature{/i}\nYour proficiency rank for Will saves increases to master; when you roll a success on a Will save, you get a critical success instead.");
        public static readonly Func<string, string> FeatureGuardianMastery = RegisterTooltipInserter(
            "Guardian.Feature.GuardianMastery",
            "{b}Guardian Mastery{/b}\n{i}Level 19 Guardian feature{/i}\nWhile wearing armor, when you attempt a Reflex save, you can add your armor's item bonus to AC instead of your Dexterity modifier if it's higher; if your armor has the bulwark trait, increase this bonus by 1. If you get a success when you do this, you get a critical success instead.");
        
        public static Func<string, string> RegisterTooltipInserter(string tooltipName, string tooltipDescription)
        {
            ModManager.RegisterInlineTooltip(tooltipName, tooltipDescription);
            return input => "{tooltip:" + tooltipName + "}" + input + "{/}";
        }
    }
    
    public static class BooleanOptions
    {
        /* Added the ability for mods to add settings options with  ModManager.RegisterBooleanSettingsOption(string technicalName, string caption, string longDescription, bool default) for registration API and PlayerProfile.Instance.IsBooleanOptionEnabled(string technicalName) for reading API.

        You can now use the many new methods in the CommonQuestions class to add dialogue and other player interactivity choices. */
        
        //public const string UnrestrictedTrace = "RunesmithPlaytest.UnrestrictedTrace";
    }
    
    public static class PossibilityGroups
    {
        public const string TauntActions = "Taunt Actions";
    }

    public static class PersistentActions
    {
        public const string ToughToKill = "ToughToKill";
    }
    
    public static class Traits
    {
        #region Class
        public static readonly Trait Guardian = ModManager.RegisterTrait("Guardian", 
            new TraitProperties("Guardian", true) { IsClassTrait = true });
        #endregion
    
        #region Features
        
        #endregion
    
        #region Feats
        public static readonly Trait BodyguardCharge = ModManager.RegisterTrait("Bodyguard's Charge",
            new TraitProperties("Bodyguard's Charge", false));
        #endregion
    }
    
    public static class FeatNames
    {
        #region Class
        public static readonly FeatName GuardianClass = ModManager.RegisterFeatName("GuardianClass.GuardianClass", "Guardian");
        #endregion
        
        #region Class Features
        public static readonly FeatName GuardiansArmor = ModManager.RegisterFeatName("GuardianClass.GuardiansArmor", "Guardian's Armor");
        public static readonly FeatName Taunt = ModManager.RegisterFeatName("GuardianClass.Taunt", "Taunt");
        public static readonly FeatName InterceptAttack = ModManager.RegisterFeatName("GuardianClass.InterceptAttack", "Intercept Attack");
        public static readonly FeatName ToughToKill = ModManager.RegisterFeatName("GuardianClass.ToughToKill", "Tough To Kill");
        public static readonly FeatName ReactionTime = ModManager.RegisterFeatName("GuardianClass.ReactionTime", "Reaction Time");
        public static readonly FeatName GuardianMastery = ModManager.RegisterFeatName("GuardianClass.GuardianMastery", "Guardian Mastery");
        #endregion
        
        #region Class Feats
        public static readonly FeatName Bodyguard = ModManager.RegisterFeatName("GuardianClass.Bodyguard", "Bodyguard");
        public static readonly string BodyguardChargeChoice = "GuardianClass.BodyguardChargeChoice";
        public static readonly FeatName LargerThanLife = ModManager.RegisterFeatName("GuardianClass.LargerThanLife", "Larger Than Life");
        public static readonly FeatName LongDistanceTaunt = ModManager.RegisterFeatName("GuardianClass.LongDistanceTaunt", "Long-Distance Taunt");
        public static readonly FeatName PunishingShove = ModManager.RegisterFeatName("GuardianClass.PunishingShove", "Punishing Shove");
        public static readonly FeatName ShieldWarfare = ModManager.RegisterFeatName("GuardianClass.ShieldWarfare", "Shield Warfare");
        public static readonly FeatName ShoulderCheck = ModManager.RegisterFeatName("GuardianClass.ShoulderCheck", "Shoulder Check");
        // FeatName AggressiveBlock
        public static readonly FeatName CoveringStance = ModManager.RegisterFeatName("GuardianClass.CoveringStance", "Covering Stance");
        public static readonly FeatName HamperingStance = ModManager.RegisterFeatName("GuardianClass.HamperingStance", "Hampering Stance");
        public static readonly FeatName PhalanxFormation = ModManager.RegisterFeatName("GuardianClass.PhalanxFormation", "Phalanx Formation");
        public static readonly FeatName RaiseHaft = ModManager.RegisterFeatName("GuardianClass.RaiseHaft", "Raise Haft");
        public static readonly FeatName ShieldYourEyes = ModManager.RegisterFeatName("GuardianClass.ShieldYourEyes", "Shield Your Eyes");
        public static readonly FeatName ShieldingTaunt = ModManager.RegisterFeatName("GuardianClass.ShieldingTaunt", "Shielding Taunt");
        public static readonly FeatName TauntingStrike = ModManager.RegisterFeatName("GuardianClass.TauntingStrike", "Taunting Strike");
        public static readonly FeatName AreaArmor = ModManager.RegisterFeatName("GuardianClass.AreaArmor", "Area Armor");
        public static readonly FeatName ArmoredCourage = ModManager.RegisterFeatName("GuardianClass.ArmoredCourage", "Armored Courage");
        public static readonly FeatName EnergyInterceptor = ModManager.RegisterFeatName("GuardianClass.EnergyInterceptor", "Energy Interceptor");
        public static readonly FeatName FlyingTackle = ModManager.RegisterFeatName("GuardianClass.FlyingTackle", "FlyingTackle");
        public static readonly FeatName NotSoFast = ModManager.RegisterFeatName("GuardianClass.NotSoFast", "Not So Fast");
        public static readonly FeatName ProudNail = ModManager.RegisterFeatName("GuardianClass.ProudNail", "Proud Nail");
        public static readonly FeatName ShieldedAttrition = ModManager.RegisterFeatName("GuardianClass.ShieldedAttrition", "Shielded Attrition");
        public static readonly FeatName DisarmingIntercept = ModManager.RegisterFeatName("GuardianClass.DisarmingIntercept", "Disarming Intercept");
        public static readonly FeatName GuardedAdvance = ModManager.RegisterFeatName("GuardianClass.GuardedAdvance", "Guarded Advance");
        public static readonly FeatName LockDown = ModManager.RegisterFeatName("GuardianClass.LockDown", "Lock Down");
        public static readonly FeatName ReactiveStrike =
            ModManager.RegisterFeatName("GuardianClass.ReactiveStrike", "Reactive Strike");
        // FeatName Reflexive Shield
        public static readonly FeatName RetaliatingRescue = ModManager.RegisterFeatName("GuardianClass.RetaliatingRescue", "Retaliating Rescue");
        public static readonly FeatName RingTheirBell = ModManager.RegisterFeatName("GuardianClass.RingTheirBell", "Ring Their Bell");
        public static readonly FeatName StompGround = ModManager.RegisterFeatName("GuardianClass.StompGround", "Stomp Ground");
        public static readonly FeatName GroupTaunt = ModManager.RegisterFeatName("GuardianClass.GroupTaunt", "Group Taunt");
        public static readonly FeatName JuggernautCharge = ModManager.RegisterFeatName("GuardianClass.JuggernautCharge", "Juggernaut Charge");
        public static readonly FeatName MightyBulwark = ModManager.RegisterFeatName("GuardianClass.MightyBulwark", "Mighty Bulwark");
        public static readonly FeatName RepositioningBlock = ModManager.RegisterFeatName("GuardianClass.RepositioningBlock", "Repositioning Block");
        public static readonly FeatName ShieldFromArrows = ModManager.RegisterFeatName("GuardianClass.ShieldFromArrows", "Shield From Arrows");
        public static readonly FeatName ShieldWallop = ModManager.RegisterFeatName("GuardianClass.ShieldWallop", "Shield Wallop");
        #endregion
    }
    
    public static class QEffectIds
    {
        public static readonly QEffectId TauntTarget = ModManager.RegisterEnumMember<QEffectId>("TauntTarget");
        public static readonly QEffectId BodyguardCharge = ModManager.RegisterEnumMember<QEffectId>("Bodyguard's Charge");
    }

    public static class CommonQFKeys
    {
        /// <summary>This key includes the name of the Guardian who inflicted it -- search using this name + Creature.Name. Searching for the source of this effect also ensures that the creature is off-guard due to ignoring YOUR Taunt.</summary>
        public static readonly string OffGuardDueToTaunt = "TauntOffGuard";
    }

    public static class ActionIds
    {
        public static readonly ActionId Taunt = ModManager.RegisterEnumMember<ActionId>("Taunt");
        public static readonly ActionId InterceptAttack = ModManager.RegisterEnumMember<ActionId>("InterceptAttack");
    }

    public static class Illustrations
    {
        public static readonly Illustration Taunt = new ModdedIllustration("GuardianClassAssets/intimidation.png");
        public static readonly Illustration InterceptAttack = new ModdedIllustration("GuardianClassAssets/card-exchange.png");
        public static readonly Illustration ArmoredCourage = new ModdedIllustration("GuardianClassAssets/armor-upgrade 2.png");
        public static readonly Illustration StompGround = new ModdedIllustration("GuardianClassAssets/quake-stomp.png");
        public static readonly string DawnsburySunPath = "GuardianClassAssets/PatreonSunTransparent.png";
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
        public static readonly SubmenuId Taunt = ModManager.RegisterEnumMember<SubmenuId>("Taunt");
    }
    
    public static class PossibilitySectionIds
    {
        public static readonly PossibilitySectionId BasicTaunts = ModManager.RegisterEnumMember<PossibilitySectionId>("BasicTaunts");
    }

    public static class CommonRequirements
    {
        public static CreatureTargetingRequirement WearingMediumOrHeavyArmor()
        {
            return new LegacyCreatureTargetingRequirement((a,_) =>
                (a.BaseArmor?.HasTrait(Trait.MediumArmor) ?? false) || (a.BaseArmor?.HasTrait(Trait.HeavyArmor) ?? false)
                    ? Usability.Usable
                    : Usability.NotUsable("must be wearing medium or heavy armor"));
        }
    }
}