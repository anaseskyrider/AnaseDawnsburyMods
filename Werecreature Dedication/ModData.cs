using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Template;

public static class ModData
{
    // Trait and feat names will be confusing. The name of the mod is "Werecreature Dedication", so technical strings will begin with that.
    // Following from there, it's easier to follow along as to what the technical string actually is, given the data type (Trait or FeatName) and what follows after the first part.
    // Otherwise, I am breaking form, from my other mods, with how I name these, just to avoid confusing myself lol. Still avoiding using greedy technical string names, in case others decide to maintain this mod, or include it as part of a larger Howl of the Wild content-mod initiative.
    public static class Traits
    {
        /// The name of the mod.
        public static readonly Trait ModName = ModManager.RegisterTrait("WerecreatureDedication", new TraitProperties("Werecreature Mod", true));
            
        /// Used as an archetype trait as well as trait in general
        public static readonly Trait Werecreature = ModManager.RegisterTrait("WerecreatureDedication.Werecreature", new TraitProperties("Werecreature", true));
        
        /// Feats with this trait are the werecreature type, such as werebat and werebear.
        public static readonly Trait WerecreatureType = ModManager.RegisterTrait("WerecreatureDedication.Werecreature", new TraitProperties("Werecreature", true));

        public static readonly Trait Rare = ModManager.RegisterTrait("Rare",
            new TraitProperties("Rare", true) { BackgroundColor = Color.Navy, WhiteForeground = true });
    }
    
    public static class FeatNames
    {
        // Change Shape feat
        public static readonly FeatName WereShape = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.WereShape", "Change Shape (werecreature)");
        // Werecreature Types
        public static readonly FeatName Werebat = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Werebat", "Werebat");
        public static readonly FeatName Werebear = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Werebear", "Werebear");
        public static readonly FeatName Wereboar = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Wereboar", "Wereboar");
        public static readonly FeatName Werecrocodile = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Werecrocodile", "Werecrocodile");
        public static readonly FeatName Weremoose = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Weremoose", "Weremoose");
        public static readonly FeatName Wererat = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Wererat", "Wererat");
        public static readonly FeatName Wereshark = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Wereshark", "Wereshark");
        public static readonly FeatName Weretiger = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Weretiger", "Weretiger");
        public static readonly FeatName Werewolf = ModManager.RegisterFeatName("WerecreatureDedication.CreatureTypes.Werewolf", "Werewolf");
        
        // Archetype feats
        public static readonly FeatName AnimalFleetness = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.AnimalFleetness", "Animal Fleetness");
        // Beastkin support?
        //public static readonly FeatName BeastkinResilience = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.BeastkinResilience", "Beastkin Resilience");
        // Senses don't do anything?
        //public static readonly FeatName FeralSenses = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.FeralSenses", "Feral Senses");
        public static readonly FeatName AntlerRush = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.AntlerRush", "Antler Rush");
        public static readonly FeatName BearHug = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.BearHug", "Bear Hug");
        public static readonly FeatName DeathRoll = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.DeathRoll", "Death Roll");
        //public static readonly FeatName Echolocation = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.Echolocation", "Echolocation");
        public static readonly FeatName FearfulSymmetry = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.FearfulSymmetry", "Fearful Symmetry");
        public static readonly FeatName FeedingFrenzy = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.FeedingFrenzy", "Feeding Frenzy");
        public static readonly FeatName PackAttack = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.PackAttack", "Pack Attack");
        public static readonly FeatName PlagueRat = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.PlagueRat", "Plague Rat");
        public static readonly FeatName RushingBoar = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.RushingBoar", "Rushing Boar");
        public static readonly FeatName CorneredAnimal = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.CorneredAnimal", "Cornered Animal");
        public static readonly FeatName FeralMending = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.FeralMending", "Feral Mending");
        public static readonly FeatName TerrifyingTransformation = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.TerrifyingTransformation", "Terrifying Transformation");
        //public static readonly FeatName YouDontSmellRight = ModManager.RegisterFeatName("WerecreatureDedication.Archetype.YouDontSmellRight", "You Don't Smell Right");
    }
    
    public static class QEffectIds
    {
        public static readonly QEffectId WereShape = ModManager.RegisterEnumMember<QEffectId>("WerecreatureDedication.WereShape");
    }

    public static class ActionIds
    {
        public static readonly ActionId WereShape = ModManager.RegisterEnumMember<ActionId>("WerecreatureDedication.WereShape");
    }

    public static class Illustrations
    {
        public static readonly Illustration WereShape = IllustrationName.Action;
        public static readonly Illustration HybridShape = new ModdedIllustration("WerecreatureDedicationAssets/halloween.png");
        public static readonly Illustration AnimalShape = new ModdedIllustration("WerecreatureDedicationAssets/wolf.png");
        public static readonly Illustration HumanShape = IllustrationName.Action;
        //public static readonly Illustration StumblingStance = new ModdedIllustration("MoreDedicationsAssets/calabash.png");
    }

    public static class SubmenuIds
    {
        public static readonly SubmenuId WereShape = ModManager.RegisterEnumMember<SubmenuId>("WerecreatureDedication.WereShape");
    }
    
    public static class PossibilitySectionIds
    {
        public static readonly PossibilitySectionId WereShape = ModManager.RegisterEnumMember<PossibilitySectionId>("WerecreatureDedication.WereShape");
    }

    public static class SfxNames
    {
        public const SfxName HybridShape = SfxName.BeastRoar;
        public const SfxName AnimalShape = SfxName.BeastRoar;
        public static SfxName HumanShape(Creature cr)
        {
            return cr.HasTrait(Trait.Female) ? SfxName.HeroAlexHmmm : SfxName.SoldierHunterHmmm;
        }
    }

    public static class Tooltips
    {
        //public static readonly Func<string?, string> LeveledDC = inline => "{tooltip:MoreDedications.LevelBasedDC}" + inline + "{/}";
    }
}