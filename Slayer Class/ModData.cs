using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;

public static class ModData
{
    public const string IdPrepend = "SlayerClass.";
 
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
        BooleanOptions.Initialize();
        ItemGreaterGroups.Initialize();
        PossibilitySectionIds.Initialize();
        QEffectIds.Initialize();
        RuneKinds.Initialize();
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

    // TODO: Unused
    public static class ActionIds
    {
        //public static ActionId CraneFlutter = null!;
        
        public static void Initialize()
        {
            //CraneFlutter = SafelyRegister<ActionId>("CraneFlutter");
        }
    }

    // TODO: Unused
    /// <summary>
    /// Keeps the options registered with <see cref="ModManager.RegisterBooleanSettingsOption"/>. To read the registered options, use <see cref="PlayerProfile.Instance.IsBooleanOptionEnabled(string)"/>.
    /// </summary>
    public static class BooleanOptions
    {
        //public static string UnrestrictedTrace = null!;
        
        public static void Initialize()
        {
            /*UnrestrictedTrace = RegisterBooleanOption(
                IdPrepend+"UnrestrictedTrace",
                "Runesmith: Less Restrictive Rune Tracing",
                "Enabling this option removes protections against \"bad decisions\" ith tracing certain runes on certain targets.\n\nThe Runesmith is a class on the more advanced end of tactics and creativity. For example, you might want to trace Esvadir onto an enemy because you're about to invoke it onto a different, adjacent enemy. Or you might trace Atryl on yourself as a 3rd action so that you can move it with Transpose Etching (just 1 action) on your next turn, because you're a ranged build.\n\nThis option is for those players.",
                 true);*/
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

    // TODO: Unused
    public static class CommonQfKeys
    {
        /// <summary>This key includes the name of the Guardian who inflicted it -- search using this name + Creature.Name. Searching for the source of this effect also ensures that the creature is off-guard due to ignoring YOUR Taunt.</summary>
        //public static readonly string OffGuardDueToTaunt = "TauntOffGuard";
 
        public static string PersistentDamageKey(DamageKind kind)
        {
            return "PersistentDamage:" + kind.ToStringOrTechnical();
        }
    }
 
    // TODO: Unused
    public static class CommonReactionKeys
    {
        //public const string ReactionTime = "ReactionTime";
    }
 
    // TODO: Unused
    public static class CommonRequirements
    {
        /*public static CreatureTargetingRequirement WearingMediumOrHeavyArmor()
        {
            return new LegacyCreatureTargetingRequirement((a,_) =>
                (a.BaseArmor?.HasTrait(Trait.MediumArmor) ?? false) || (a.BaseArmor?.HasTrait(Trait.HeavyArmor) ?? false)
                    ? Usability.Usable
                    : Usability.NotUsable("must be wearing medium or heavy armor"));
        }*/
        
        /*public static string? WhyCannotEnterRage(Creature self)
        {
            if (self.HasEffect(QEffectId.Fatigued))
                return "You can't rage while fatigued.";
            if (self.HasEffect(QEffectId.Rage))
                return "You're already raging.";
            return !self.HasEffect(QEffectId.HasRagedThisEncounter) || self.HasEffect(QEffectId.SecondWind) ? null : "You already raged this encounter.";
        }*/
    }

    // TODO: Unused
    public static class FeatGroups
    {
        //public static readonly FeatGroup FamiliarAbilities = new FeatGroup("Familiar Abilities", 0);
    }

    public static class FeatNames
    {
        #region Class
        
        public static readonly FeatName SlayerClass = ModManager.RegisterFeatName(IdPrepend+"SlayerClass", "Slayer");
        
        #endregion
        
        #region Class Features
        
        public static readonly FeatName OnTheHunt = ModManager.RegisterFeatName(IdPrepend+"OnTheHunt", "On the Hunt");
        public static readonly FeatName MonsterLore = ModManager.RegisterFeatName(IdPrepend+"MonsterLore", "Monster Lore");
        public static readonly FeatName SlayersArsenal = ModManager.RegisterFeatName(IdPrepend+"SlayersArsenal", "Slayer's Arsenal");
        public static readonly FeatName SlayersQuarry = ModManager.RegisterFeatName(IdPrepend+"SlayersQuarry", "Slayer's Quarry");
        /// <summary>
        /// The Mark Quarry sub-feature of Slayer's Quarry
        /// </summary>
        public static readonly FeatName MarkQuarry = ModManager.RegisterFeatName(IdPrepend+"MarkQuarry", "Mark Quarry");
        /// <summary>
        /// The Claim Trophy sub-feature of Slayer's Quarry
        /// </summary>
        public static readonly FeatName ClaimTrophy = ModManager.RegisterFeatName(IdPrepend+"ClaimTrophy", "Claim Trophy");
        
        #endregion
        
        #region Class Feats
        
        
        
        #endregion
    }
 
    // TODO: Unused
    public static class Illustrations
    {
        public const string ModFolder = "SlayerClassAssets/";
        
        public static readonly Illustration DdSun = new ModdedIllustration(ModFolder+"PatreonSunTransparent.png");

        public static readonly Illustration BloodseekingBlade = IllustrationName.Greataxe;
        public static readonly Illustration TrophyCase = IllustrationName.BagOfHolding1;
        public static readonly Illustration Trophy = IllustrationName.Trophy;
        public static readonly Illustration OnTheHunt = IllustrationName.HuntPrey;
        public static readonly Illustration MarkQuarry = IllustrationName.HuntPrey;
    }

    // TODO: Unused
    public static class ItemGreaterGroups
    {
        public static void Initialize()
        {
            //PlatedShields = SafelyRegister<ItemGreaterGroup>("Plated shields");
        }
        
        //public static ItemGreaterGroup PlatedShields = null!;
    }

    // TODO: Unused
    public static class PersistentActions
    {
        //public const string RunicTattoo = "RunicTattoo";
    }

    // TODO: Unused
    public static class PossibilityGroups
    {
        //public const string DrawingRunes = "Draw runes";
    }

    // TODO: Unused
    public static class PossibilitySectionIds
    {
        //public static PossibilitySectionId RuneSinger;
        
        public static void Initialize()
        {
            //RuneSinger = SafelyRegister<PossibilitySectionId>("RuneSinger");
        }
    }

    public static class QEffectIds
    {
        public static QEffectId MarkedQuarry;
        
        public static void Initialize()
        {
            MarkedQuarry = SafelyRegister<QEffectId>("MarkedQuarry");
        }
    }

    public static class RuneKinds
    {
        public static RuneKind SlayerTrophy;
        
        public static void Initialize()
        {
            SlayerTrophy = SafelyRegister<RuneKind>("SlayerTrophy");
        }
    }

    public static class SfxNames
    {
        public const SfxName MarkQuarry = SfxName.Hide;
        public const SfxName OnTheHunt = SfxName.Throw;
    }

    // TODO: Unused
    public static class SubmenuIds
    {
        //public static SubmenuId TraceRune = null!;
        
        public static void Initialize()
        {
            //TraceRune = SafelyRegister<SubmenuId>("TraceRune");
        }
    }

    public static class Tooltips
    {
        public static readonly Func<string, string> ReinforcedBenefit = RegisterTooltipInserter(
            IdPrepend+"Reinforced",
            "You gain this benefit when your hunting tool is reinforced with a trophy.");
        
        public static readonly Func<string, string> SpecializedArsenal = RegisterTooltipInserter(
            IdPrepend+"SpecializedArsenal",
            "You gain the specialized arsenal benefit of your signature tool.");
        
        public static readonly Func<string, string> ExpandedArsenal = RegisterTooltipInserter(
            IdPrepend+"ExpandedArsenal",
            "Choose a second signature tool. You gain its normal benefits, but not its specialized arsenal benefit.");
        
        public static readonly Func<string, string> GreaterSpecializedArsenal = RegisterTooltipInserter(
            IdPrepend+"GreaterSpecializedArsenal",
            "You gain the specialized arsenal benefit of your second signature tool.");
        
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
        public static readonly Trait ModName = ModManager.RegisterModNameTrait("SlayerClass", "Slayer Class");
         
        /*public static readonly Trait Rare = ModManager.RegisterTrait("Rare",
            new TraitProperties("Rare", true) { BackgroundColor = Color.Navy, WhiteForeground = true });*/
         
        #region Class
        
        public static readonly Trait Slayer = ModManager.RegisterTrait("Slayer", 
            new TraitProperties("Slayer", true) { IsClassTrait = true });
         
        #endregion
    
        #region Features
        
        public static readonly Trait Relentless = ModManager.RegisterTrait(
            "Relentless",
            new TraitProperties(
                "Relentless",
                true,
                "Actions with the relentless trait are special techniques that you have trained to use on instinct. You can use any action with the relentless trait with the extra action you get from On the Hunt (in addition to one or more of your normal actions, in the case of 2- or 3-action activities with the relentless trait)."));

        public static readonly Trait Trophy = ModManager.RegisterTrait(
            "Trophy",
            new TraitProperties(
                "Trophy",
                true,
                """
                Trophies are items that are used to Reinforce your Arsenal. You do so by clicking-and-dragging the trophy onto a hunting tool, adding additional benefits to the item.
                
                You gain a trophy when you slay your quarry, and it has the following properties:
                • {b}Traits{/b} It has all your quarry's traits, but not rarity or size.
                • {b}Damage Types{/b} It has any damage types that your quarry could deal with its Strikes or non-spellcasting abilities, or it had immunity to.
                • {b}Traditions{/b} It's associated with your quarry's tradition trait and that of any spells it could cast. Its tradition is occult if it had neither.
                """));

        public static readonly Trait HuntingTool = ModManager.RegisterTrait(
            "HuntingTool",
            new TraitProperties("Hunting Tool", false));

        #endregion
    }
}