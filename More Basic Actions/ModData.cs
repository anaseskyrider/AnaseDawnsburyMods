using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class ModData
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
        public static readonly QEffectId Readied = ModManager.RegisterEnumMember<QEffectId>("Readied");
    }

    public static class ActionIds
    {
        public static readonly ActionId PrepareToAid = ModManager.RegisterEnumMember<ActionId>("PrepareToAid");
        public static readonly ActionId AidReaction = ModManager.RegisterEnumMember<ActionId>("AidReaction");
        public static readonly ActionId Ready = ModManager.RegisterEnumMember<ActionId>("Ready");
        public static readonly ActionId HelpUp = ModManager.RegisterEnumMember<ActionId>("HelpUp");
    };

    public static class Illustrations
    {
        public static readonly Illustration Aid = new ModdedIllustration("MoreBasicActionsAssets/protection.png");
        public static readonly Illustration Ready = new ModdedIllustration("MoreBasicActionsAssets/chronometer.png");
        public static readonly Illustration HelpUp = new ModdedIllustration("MoreBasicActionsAssets/helping-hand.png");
    };

    public static class SubmenuIds
    {
        public static readonly SubmenuId PrepareToAid = ModManager.RegisterEnumMember<SubmenuId>("PrepareToAid");
        public static readonly SubmenuId Ready = ModManager.RegisterEnumMember<SubmenuId>("Ready");
    };
    
    public static class PossibilitySectionIds
    {
        public static readonly PossibilitySectionId AidSkills = ModManager.RegisterEnumMember<PossibilitySectionId>("AidSkills");
        public static readonly PossibilitySectionId AidAttacks = ModManager.RegisterEnumMember<PossibilitySectionId>("AidAttacks");
        public static readonly PossibilitySectionId Ready = ModManager.RegisterEnumMember<PossibilitySectionId>("Ready");
    };
}