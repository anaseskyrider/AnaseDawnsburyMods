using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreShields;

public static class ModData
{
    public const string IdPrepend = "MoreShields.";
    
    public static void LoadData()
    {
        // Trait Modification //
        TraitProperties tShieldProperties = Trait.TowerShield.GetTraitProperties();
        TraitExtensions.TraitProperties[Trait.TowerShield] = new TraitProperties(
            tShieldProperties.HumanizedName,
            false, /*tShieldProperties.Relevant,*/
            null,
            tShieldProperties.RelevantForShortBlock);
        
        TraitProperties shieldProperties = Trait.Shield.GetTraitProperties();
        TraitExtensions.TraitProperties[Trait.Shield] = new TraitProperties(
            shieldProperties.HumanizedName,
            shieldProperties.Relevant,
            $"You can use this item to {Tooltips.ActionRaiseAShield("Raise a Shield {icon:Action}")}. If you score a crit and have unlocked {{tooltip:criteffect}}critical specialization effects{{/}} for this weapon: Push the target 5 feet.",
            shieldProperties.RelevantForShortBlock,
            shieldProperties.BackgroundColor,
            shieldProperties.WhiteForeground,
            shieldProperties.IsFeatOnlyTrait);
        
        // RuneKind registration //
        RuneKinds.ShieldPlating = ModManager.TryParse("ShieldPlating", out RuneKind shieldPlating)
            ? shieldPlating
            : ModManager.RegisterEnumMember<RuneKind>("ShieldPlating");
    }

    public static class Illustrations
    {
        public const string ModFolder = "MoreShieldsAssets/";
        
        public static readonly Illustration ShieldBlock = new ModdedIllustration(ModFolder+"shield_block.png");
        public static readonly Illustration ReactiveShield = new ModdedIllustration(ModFolder+"reactive_shield.png");
        
        public static readonly Illustration Buckler = new ModdedIllustration(ModFolder+"buckler.png");
        public static readonly Illustration MeteorShield = new ModdedIllustration(ModFolder+"frisbee2.png");
        public static readonly Illustration FortressShield = new ModdedIllustration(ModFolder+"shield.png");
        public static readonly Illustration HeavyRondache = new ModdedIllustration(ModFolder+"buckler 2.png");
        public static readonly Illustration CastersTarge = new ModdedIllustration(ModFolder+"shield (2).png");
        public static readonly Illustration ShieldPlating = new ModdedIllustration(ModFolder+"steel.png");
        public static readonly Illustration ShieldAugmentation = new ModdedIllustration(ModFolder+"repair.png");
    }

    public static class ItemGreaterGroups
    {
        public static readonly ItemGreaterGroup ShieldModifications = ModManager.RegisterEnumMember<ItemGreaterGroup>("Shield modifications");
        public static readonly ItemGreaterGroup PlatedShields = ModManager.RegisterEnumMember<ItemGreaterGroup>("Plated shields");
    }

    public static class ItemNames
    {
        #region Shields
        
        public static ItemName Buckler;
        public static ItemName CastersTarge; // TV
        public static ItemName HeavyRondache; // TV
        public static ItemName MeteorShield; // TV
        public static ItemName FortressShield; // TV
        
        #endregion
        
        #region Item Modifications
        
        public static ItemName SturdyShieldPlatingMinor;
        public static ItemName SturdyShieldPlatingLesser;
        public static ItemName SturdyShieldPlatingModerate;
        public static ItemName SturdyShieldPlatingGreater;
        public static ItemName SturdyShieldPlatingMajor;
        public static ItemName SturdyShieldPlatingSupreme;

        public static ItemName ShieldAugmentationBackswing; // GB
        public static ItemName ShieldAugmentationForceful; // GB
        public static ItemName ShieldAugmentationManeuverable; // GB
        public static ItemName ShieldAugmentationVersatile; // GB

        #endregion
    }

    public static class QEffectIds
    {
        public static readonly QEffectId BonusToHardness = ModManager.RegisterEnumMember<QEffectId>("BonusToHardness");
        public static readonly QEffectId CastersTargeUsed = ModManager.RegisterEnumMember<QEffectId>("CastersTargeUsed");
    }

    public static class RuneKinds
    {
        public static RuneKind ShieldPlating;
        public static readonly RuneKind ShieldAugmentation = ModManager.RegisterEnumMember<RuneKind>("ShieldAugmentation");
    }
    
    public static class SfxNames
    {
        public static readonly SfxName ShieldBlockWooodenImpact = ModManager.RegisterNewSoundEffect("MoreShieldsAssets/impactwood14 (quieter).mp3.flac");
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> ActionRaiseAShield = RegisterTooltipInserter(
            IdPrepend+"Actions.RaiseAShield",
            "{b}Raise a Shield {icon:Action}{/b}\n(Requires you wield a shield)\nYou position your shield to protect yourself. When you have Raised a Shield, you gain its listed circumstance bonus to AC. Your shield remains raised until the start of your next turn.");

        public static readonly Func<string, string> ActionShieldBlock = RegisterTooltipInserter(
            IdPrepend+"Actions.ShieldBlock",
            "{b}Shield Block {icon:Reaction}{/b}\n{i}Level 1 General feat{/i}\n{b}Trigger{/b} While you have your shield raised, you would take damage from a physical attack.\n{b}Effect{/b} Your shield prevents you from taking an amount of damage up to the shield’s Hardness. You take any remaining damage.\n\n{icon:YellowWarning} You must learn this feat to use Shield Block. Some characters, such as fighters, start with it for free.");
        
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
        /// Name of the mod.
        public static readonly Trait MoreShields = ModManager.RegisterTrait("MoreShields",
            new TraitProperties("More Shields", true));
        
        /// Hidden technical trait. A <see cref="CombatAction"/> with an ActionCost of 0 is treated as a reaction for the purposes of granting additional reactions.
        public static readonly Trait ReactiveAction = ModManager.RegisterTrait("ReactiveAction",
            new TraitProperties("Reactive Action", false));
        
        /// Hidden technical trait. A light shield grants +1 to AC when raised.
        public static readonly Trait LightShield = ModManager.RegisterTrait("LightShield",
            new TraitProperties("Light Shield", false));
        
        /// Hidden technical trait. A medium shield grants a +2 to AC when raised.
        public static readonly Trait MediumShield = ModManager.RegisterTrait("MediumShield",
            new TraitProperties("Medium Shield", false));
        
        /// Hidden technical trait. A heavy shield grants a +2 to AC when raised.
        public static readonly Trait HeavyShield = ModManager.RegisterTrait("HeavyShield",
            new TraitProperties("Heavy Shield", false));
        
        /// A cover shield allows you to take cover behind it to gain a +4 to AC.
        public static readonly Trait CoverShield = ModManager.RegisterTrait("CoverShield",
            new TraitProperties("Cover Shield", true, "You can Take Cover if this shield is raised. Doing so increases the shield's bonus to AC to +4."));
        
        /// A worn shield doesn't occupy a hand, but is usually also a light shield and cannot always be raised.
        public static readonly Trait WornShield = ModManager.RegisterTrait("WornShield",
            new TraitProperties("Worn Shield", true, "This shield doesn't occupy your hands, but you can't raise it unless you have a free hand or have a hand that's not wielding a weapon."));

        /// <summary>
        /// Hidden technical trait. A feat with this trait tells the action possibility generator to make a menu for Raise a Shield instead of just generating the action directly, allowing items to be added to the "Raise shield" menu.
        /// </summary>
        public static readonly Trait ShieldActionFeat = ModManager.RegisterTrait("Shield Action Feat",
            new TraitProperties("Shield Action Feat", false));
            
        public static readonly Trait Hefty14 = ModManager.RegisterTrait("Hefty14", 
            new TraitProperties("Hefty 14", true, "Raising a Shield with the Hefty trait takes more effort, costing an extra action if your Strength score is below the number with the trait."));
        
        #region Items
        public static readonly Trait FortressShield = ModManager.RegisterTrait("FortressShield",
            new TraitProperties("Fortress Shield", false/*true, "You have an additional -10 speed penalty when wielding this shield."*/));
        public static readonly Trait Buckler = ModManager.RegisterTrait("Buckler",
            new TraitProperties("Buckler", false/*true, "This shield doesn't occupy your hands, but you can't raise it unless you have a free hand or have a hand that's not wielding a weapon."*/));
        public static readonly Trait MeteorShield = ModManager.RegisterTrait("MeteorShield",
            new TraitProperties("Meteor Shield", false/*true, "This shield compromises some of its sturdiness to gain the Thrown 30 ft. trait."*/));
        public static readonly Trait HeavyRondache = ModManager.RegisterTrait("HeavyRondache",
            new TraitProperties("Heavy Rondache", false/*true, "This shield compromises some of its sturdiness to gain the Thrown 30 ft. trait."*/));
        public static readonly Trait CastersTarge = ModManager.RegisterTrait("CastersTarge",
            new TraitProperties("Caster's Targe", false/*true, "This shield compromises some of its sturdiness to gain the Thrown 30 ft. trait."*/));
        #endregion
    }
}