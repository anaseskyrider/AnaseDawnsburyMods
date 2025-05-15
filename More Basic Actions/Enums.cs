using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class Enums
{
    public static class Traits
    {
        public static readonly Trait MoreBasicActions = ModManager.RegisterTrait("MoreBasicActions", new TraitProperties("More Basic Actions", true));
    }
    
    public static class FeatNames
    {
        public static readonly FeatName CooperativeNature = ModManager.RegisterFeatName("MoreBasicActions.Human.CooperativeNature", "Cooperative Nature");
    }
    
    public static class QEffectIds
    {
        public static readonly QEffectId PreparedToAid = ModManager.RegisterEnumMember<QEffectId>("Prepared to Aid");
    }

    public static class ActionIds
    {
        public static readonly ActionId PrepareToAid = ModManager.RegisterEnumMember<ActionId>("PrepareToAid");
        public static readonly ActionId AidReaction = ModManager.RegisterEnumMember<ActionId>("AidReaction");
    };

    public static class Illustrations
    {
        public static readonly Illustration Aid = IllustrationName.Reaction;
        //public static readonly Illustration QWE = new ModdedIllustration("MoreBasicActions/filename.png");
    };

    public static class SubmenuIds
    {
        public static readonly SubmenuId PrepareToAid = ModManager.RegisterEnumMember<SubmenuId>("PrepareToAid");
    };
    
    public static class PossibilitySectionIds
    {
        public static readonly PossibilitySectionId AidSkills = ModManager.RegisterEnumMember<PossibilitySectionId>("AidSkills");
        public static readonly PossibilitySectionId AidAttacks = ModManager.RegisterEnumMember<PossibilitySectionId>("AidAttacks");
    };
}