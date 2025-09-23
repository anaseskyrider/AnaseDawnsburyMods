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
    public const string IdPrepend = "MoreBasicActions.";
    
    public static void LoadData()
    {
        ////////////////
        // Action IDs //
        ////////////////
        // Ensures compatibility with other mods registering the same ID, regardless of load order.
        ActionIds.PrepareToAid = ModManager.TryParse("PrepareToAid", out ActionId prepareAid)
            ? prepareAid
            : ModManager.RegisterEnumMember<ActionId>("PrepareToAid");
        
        ActionIds.AidReaction = ModManager.TryParse("AidReaction", out ActionId aidReaction)
            ? aidReaction
            : ModManager.RegisterEnumMember<ActionId>("AidReaction");
        
        ActionIds.Reposition = ModManager.TryParse("Reposition", out ActionId reposition)
            ? reposition
            : ModManager.RegisterEnumMember<ActionId>("Reposition");
    }
    
    public static class Traits
    {
        public static readonly Trait MoreBasicActions = ModManager.RegisterTrait("MoreBasicActions", new TraitProperties("More Basic Actions", true));
        public static readonly Trait Brace = ModManager.RegisterTrait("Brace", new TraitProperties("Brace", true, "When you Ready to Strike an opponent that moves within your reach, until the start of your next turn Strikes made as part of a reaction with the brace weapon deal an additional 2 precision damage for each weapon damage die it has."));
        /// This attack is a reactive attack but it has and contributes to MAP. (Used to differentiate regular Strikes from a Brace weapon with reaction Strikes). 
        public static readonly Trait ReactiveAttackWithMAP = ModManager.RegisterTrait("ReactiveAttackWithMap");
    }
    
    public static class FeatNames
    {
        public static readonly FeatName CooperativeNature = ModManager.RegisterFeatName(IdPrepend+"Human.CooperativeNature", "Cooperative Nature");
        public static readonly FeatName QuickRepair = ModManager.RegisterFeatName(IdPrepend+"QuickRepair", "Quick Repair");
    }
    
    public static class QEffectIds
    {
        public static readonly QEffectId PreparedToAid = ModManager.RegisterEnumMember<QEffectId>("Prepared to Aid");
        public static readonly QEffectId Readied = ModManager.RegisterEnumMember<QEffectId>("Readied");
    }

    public static class ActionIds
    {
        public static ActionId PrepareToAid;
        public static ActionId AidReaction;
        public static readonly ActionId Ready = ModManager.RegisterEnumMember<ActionId>("Ready");
        public static readonly ActionId HelpUp = ModManager.RegisterEnumMember<ActionId>("HelpUp");
        public static readonly ActionId QuickRepair = ModManager.RegisterEnumMember<ActionId>("QuickRepair");
        public static readonly ActionId LongJump = ModManager.RegisterEnumMember<ActionId>("LongJump");
        public static ActionId Reposition;
    };

    public static class Illustrations
    {
        public static readonly string DDSunPath = "MoreBasicActionsAssets/PatreonSunTransparent.png";
        //new ModdedIllustration(ModData.Illustrations.DawnsburySunPath).IllustrationAsIconString
        public static readonly Illustration Aid = new ModdedIllustration("MoreBasicActionsAssets/protection.png");
        public static readonly Illustration Ready = new ModdedIllustration("MoreBasicActionsAssets/chronometer.png");
        public static readonly Illustration HelpUp = new ModdedIllustration("MoreBasicActionsAssets/helping-hand.png");
        //new ModdedIllustration("MoreBasicActionsAssets/helping-hand.png");
        public static readonly Illustration QuickRepair = IllustrationName.Adamantine;
        public static readonly Illustration Reposition = new ModdedIllustration("MoreBasicActionsAssets/person (cropped).png");
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