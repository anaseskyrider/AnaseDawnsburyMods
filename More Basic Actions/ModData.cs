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

    public static class ActionIds
    {
        public static ActionId PrepareToAid;
        public static ActionId AidReaction;
        public static readonly ActionId Ready = ModManager.RegisterEnumMember<ActionId>("Ready");
        public static readonly ActionId HelpUp = ModManager.RegisterEnumMember<ActionId>("HelpUp");
        public static readonly ActionId QuickRepair = ModManager.RegisterEnumMember<ActionId>("QuickRepair");
        public static readonly ActionId LongJump = ModManager.RegisterEnumMember<ActionId>("LongJump");
        public static ActionId Reposition;
    }
    
    /// <summary>
    /// Keeps the options registered with <see cref="ModManager.RegisterBooleanSettingsOption"/>. To read the registered options, use <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string)"/>.
    /// </summary>
    public static class BooleanOptions
    {
        /// <summary>Allow untrained Prepare to Aid actions.</summary>
        public static readonly string UntrainedAid = RegisterBooleanOption(
            IdPrepend+"UntrainedAid",
            "More Basic Actions: Untrained Prepare to Aid",
            "Enable untrained Prepare to Aid options when choosing what skills to prepare to aid.",
            false);
        /// <summary>Lower the DC for the Aid reaction.</summary>
        public static readonly string AidDCIs15 = RegisterBooleanOption(
            IdPrepend+"AidDCIs15",
            "More Basic Actions: Reduce Aid DC",
            "The DC to Aid is normally 20. If enabled, the DC is reduced to 15 instead.",
            false);
        /// <summary>Move the Prepare to Aid into submenus.</summary>
        public static readonly string AidInSubmenus = RegisterBooleanOption(
            IdPrepend+"AidInSubmenus",
            "More Basic Actions: Move Aid to Other Actions",
            "Enabling this option will move the Aid menu to the Other Actions submenu.",
            false);
        /// <summary>Add the Drop Prone action to the action bar.</summary>
        public static readonly string AllowDropProne = RegisterBooleanOption(
            IdPrepend+"AllowDropProne",
            "More Basic Actions: Allow Drop Prone",
            "Enabling this option will add the Drop Prone action to the action bar.",
            false);
        /// <summary>Makes the Help Up action not treat the target as moving.</summary>
        public static readonly string HelpUpIsNotMove = RegisterBooleanOption(
            IdPrepend+"HelpUpIsNotMove",
            "More Basic Actions: Help Up Doesn't Move Ally",
            "Helping an ally up from prone counts as you taking a manipulate action and the ally taking a move action. Enabling this action means the ally doesn't actually take the Stand Up action.",
            false);
        /// <summary>Move the Ready actions into submenus.</summary>
        public static readonly string ReadyInSubmenus = RegisterBooleanOption(
            IdPrepend+"ReadyInSubmenus",
            "More Basic Actions: Move Ready to Other Actions",
            "Enabling this option will move the Ready menu to the Other Actions submenu.",
            false);
        
        /// <summary>
        /// Functions as <see cref="ModManager.RegisterBooleanSettingsOption"/>, but also returns the technicalName.
        /// </summary>
        /// <returns>(string) The technical name for the option.</returns>
        public static string RegisterBooleanOption(
            string technicalName,
            string caption,
            string longDescription,
            bool defaultValue)
        {
            ModManager.RegisterBooleanSettingsOption(technicalName, caption, longDescription, defaultValue);
            return technicalName;
        }
    }
    
    public static class FeatNames
    {
        public static readonly FeatName CooperativeNature = ModManager.RegisterFeatName(IdPrepend+"Human.CooperativeNature", "Cooperative Nature");
        public static readonly FeatName QuickRepair = ModManager.RegisterFeatName(IdPrepend+"QuickRepair", "Quick Repair");
    }

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
    }
    
    public static class PossibilitySectionIds
    {
        public static readonly PossibilitySectionId AidSkills = ModManager.RegisterEnumMember<PossibilitySectionId>("AidSkills");
        public static readonly PossibilitySectionId AidAttacks = ModManager.RegisterEnumMember<PossibilitySectionId>("AidAttacks");
        public static readonly PossibilitySectionId Ready = ModManager.RegisterEnumMember<PossibilitySectionId>("Ready");
    }
    
    public static class QEffectIds
    {
        public static readonly QEffectId PreparedToAid = ModManager.RegisterEnumMember<QEffectId>("Prepared to Aid");
        public static readonly QEffectId Readied = ModManager.RegisterEnumMember<QEffectId>("Readied");
    }

    public static class SubmenuIds
    {
        public static readonly SubmenuId PrepareToAid = ModManager.RegisterEnumMember<SubmenuId>("PrepareToAid");
        public static readonly SubmenuId Ready = ModManager.RegisterEnumMember<SubmenuId>("Ready");
    }
    
    public static class Traits
    {
        public static readonly Trait MoreBasicActions = ModManager.RegisterTrait("MoreBasicActions", new TraitProperties("More Basic Actions", true));
        public static readonly Trait Brace = ModManager.RegisterTrait("Brace", new TraitProperties("Brace", true, "When you Ready to Strike an opponent that moves within your reach, until the start of your next turn Strikes made as part of a reaction with the brace weapon deal an additional 2 precision damage for each weapon damage die it has."));
        /// This attack is a reactive attack, but it has and contributes to MAP. (Used to differentiate regular Strikes from a Brace weapon with reaction Strikes). 
        public static readonly Trait ReactiveAttackWithMAP = ModManager.RegisterTrait("ReactiveAttackWithMap");
    }
}