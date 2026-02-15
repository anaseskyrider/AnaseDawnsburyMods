using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class ModData
{
    public const string IdPrepend = "MoreBasicActions.";
    
    public static void LoadData()
    {
        ActionIds.Initialize();
        BooleanOptions.Initialize();
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
        return ModManager.TryParse<T>(technicalName, out T alreadyRegistered)
            ? alreadyRegistered
            : ModManager.RegisterEnumMember<T>(technicalName);
    }

    public static class ActionIds
    {
        public static ActionId PrepareToAid;
        public static ActionId AidReaction;
        public static ActionId Ready;
        public static ActionId HelpUp;
        public static ActionId QuickRepair;
        public static ActionId LongJump;
        public static ActionId Reposition;
        
        public static void Initialize()
        {
            PrepareToAid = SafelyRegister<ActionId>("PrepareToAid");
            AidReaction = SafelyRegister<ActionId>("AidReaction");
            Ready = SafelyRegister<ActionId>("Ready");
            HelpUp = ModManager.RegisterEnumMember<ActionId>("HelpUp");
            QuickRepair = ModManager.RegisterEnumMember<ActionId>("QuickRepair");
            LongJump = ModManager.RegisterEnumMember<ActionId>("LongJump");
            Reposition = SafelyRegister<ActionId>("Reposition");
        }
    }
    
    /// <summary>
    /// Keeps the options registered with <see cref="ModManager.RegisterBooleanSettingsOption"/>. To read the registered options, use <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string)"/>.
    /// </summary>
    public static class BooleanOptions
    {
        /// <summary>Allow untrained Prepare to Aid actions.</summary>
        public static string UntrainedAid;
        /// <summary>Lower the DC for the Aid reaction.</summary>
        public static string AidDCIs15;
        /// <summary>Add the Drop Prone action to the action bar.</summary>
        public static string AllowDropProne;
        /// <summary>Makes the Help Up action not treat the target as moving.</summary>
        public static string HelpUpIsNotMove;
        /// <summary>Move the Aid and Ready actions into submenus.</summary>
        public static string AidAndReadyInSubmenus;
        
        public static void Initialize()
        {
            UntrainedAid = RegisterBooleanOption(
                IdPrepend+"UntrainedAid",
                "More Basic Actions: Untrained Prepare to Aid",
                "Enable untrained Prepare to Aid options when choosing what skills to prepare to aid.",
                false);
            AidDCIs15 = RegisterBooleanOption(
                IdPrepend+"AidDCIs15",
                "More Basic Actions: Reduce Aid DC",
                "The DC to Aid is normally 20. If enabled, the DC is reduced to 15 instead.",
                false);
            AllowDropProne = RegisterBooleanOption(
                IdPrepend+"AllowDropProne",
                "More Basic Actions: Allow Drop Prone",
                "Enabling this option will add the Drop Prone action to the action bar.",
                false);
            HelpUpIsNotMove = RegisterBooleanOption(
                IdPrepend+"HelpUpIsNotMove",
                "More Basic Actions: Help Up Doesn't Move Ally",
                "Helping an ally up from prone counts as you taking a manipulate action and the ally taking a move action. Enabling this action means the ally doesn't actually take the Stand Up action.",
                false);
            AidAndReadyInSubmenus = RegisterBooleanOption(
                IdPrepend+"AidAndReadyInSubmenus",
                "More Basic Actions: Move Aid and Ready to Other Actions",
                "Enabling this option will move the Aid and Ready menus to the Other Actions submenu.",
                false);
        }
        
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
        /// <summary>
        /// Does nothing except fixes my initializer-based assignments.
        /// </summary>
        public static bool FixInit = false;
        
        public static readonly FeatName CooperativeNature = ModManager.RegisterFeatName(IdPrepend+"Human.CooperativeNature", "Cooperative Nature");
        public static readonly FeatName QuickRepair = ModManager.RegisterFeatName(IdPrepend+"QuickRepair", "Quick Repair");
        public static readonly FeatName QuickJump = ModManager.RegisterFeatName(IdPrepend+"QuickJump", "Quick Jump");
        public static readonly FeatName GracefulLeaper = ModManager.RegisterFeatName(IdPrepend+"Acrobat.GracefulLeaper", "Graceful Leaper");
    }

    public static class Illustrations
    {
        public const string ModFolder = "MoreBasicActionsAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(ModFolder + "PatreonSunTransparent.png");
        public static readonly Illustration Aid = new ModdedIllustration(ModFolder + "protection.png");
        public static readonly Illustration Ready = new ModdedIllustration(ModFolder + "chronometer.png");
        public static readonly Illustration HelpUp = new ModdedIllustration(ModFolder + "helping-hand.png");
        public static readonly Illustration QuickRepair = IllustrationName.Adamantine;
        public static readonly Illustration LongJump = new ModdedIllustration(ModFolder + "jumping.png");
        public static readonly Illustration Reposition = new ModdedIllustration(ModFolder + "person (cropped).png");
    }
    
    public static class PossibilitySectionIds
    {
        public static PossibilitySectionId AidSkills;
        public static PossibilitySectionId AidAttacks;
        public static PossibilitySectionId Ready;
        
        public static void Initialize()
        {
            AidSkills = ModManager.RegisterEnumMember<PossibilitySectionId>("AidSkills");
            AidAttacks = ModManager.RegisterEnumMember<PossibilitySectionId>("AidAttacks");
            Ready = ModManager.RegisterEnumMember<PossibilitySectionId>("Ready");
        }
    }
    
    public static class QEffectIds
    {
        public static QEffectId PreparedToAid;
        public static QEffectId Readied;
        
        public static void Initialize()
        {
            PreparedToAid = ModManager.RegisterEnumMember<QEffectId>("Prepared to Aid");
            Readied = ModManager.RegisterEnumMember<QEffectId>("Readied");
        }
    }

    public static class SubmenuIds
    {
        public static SubmenuId PrepareToAid;
        public static SubmenuId Ready;
        
        public static void Initialize()
        {
            PrepareToAid = ModManager.RegisterEnumMember<SubmenuId>("PrepareToAid");
            Ready = ModManager.RegisterEnumMember<SubmenuId>("Ready");
        }
    }
    
    public static class Traits
    {
        public static readonly Trait MoreBasicActions = ModManager.RegisterTrait("MoreBasicActions", new TraitProperties("More Basic Actions", true));
        public static readonly Trait Brace = ModManager.RegisterTrait("Brace", new TraitProperties("Brace", true, "When you Ready to Strike an opponent that moves within your reach, until the start of your next turn Strikes made as part of a reaction with the brace weapon deal an additional 2 precision damage for each weapon damage die it has."));
        /// This attack is a reactive attack, but it has and contributes to MAP. (Used to differentiate regular Strikes from a Brace weapon with reaction Strikes). 
        public static readonly Trait ReactiveAttackWithMAP = ModManager.RegisterTrait("ReactiveAttackWithMap");
    }
}