using System.Reflection;
using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.SlayerClass;

/// <summary>
/// Anase's library of helpful code functions. Contains a wide array of broadly useful functions rather than specialized logic.
/// </summary>
/// <list type="bullet">
/// <item>v1.6: Added Trait extensions: IsTraditionTrait(), TraditionTraitToColor(). Added Feat.ToLink(caption). Added Item.With().</item>
/// <item>v1.5: Replaced error-prone params keywords with regular arrays. Added RefundReaction extensions. Added more robust PluralizeIf extension. Added ModManager extensions.</item>
/// <item>v1.4: Added Item.WithDescription(flavorText, rulesText).</item>
/// <item>v1.3: Added CombatAction.HasAllTraits, CombatAction.HasAnyTraits, OfferOptions2 with variants for ActionPossibility and Possibility.</item>
/// <item>v1.2: Added CreateSpellLink(SpellId, Trait, int). Refactored into Extension blocks.</item>
/// <item>v1.1: Added int.WithColor(), QEffect.With(), CombatAction.With(), Item.HasAllTraits, Item.HasAnyTraits.</item>
/// <item>v1.0: Initial.</item>
/// </list>
/// <value>v1.6</value>
public static class LibraryOfAnase
{
    #region Extensions

    extension(CombatAction caThis)
    {
        /// <summary>
        /// Runs any modifications to the CombatAction in one code block, similar to Zone.With().
        /// </summary>
        public CombatAction With(Action<CombatAction> changes)
        {
            changes.Invoke(caThis);
            return caThis;
        }
        
        /// <summary>
        /// Returns whether the CombatAction has all the passed traits.
        /// </summary>
        public bool HasAllTraits(Trait[] traits) =>
            caThis.Traits.All(traits.Contains);

        /// <summary>
        /// Returns whether the CombatAction has any of the passed traits.
        /// </summary>
        public bool HasAnyTraits(Trait[] traits) =>
            caThis.Traits.Any(traits.Contains);

        /// <summary>
        /// Adds an extra effect to an action that occurs when you both hit and deal at least 1 point of damage to a creature.
        /// </summary>
        /// <para>
        /// Only meaningfully works for actions which have an attack roll. This utilizes <see cref="CombatAction.WithPrologueEffectOnChosenTargetsBeforeRolls"/>, which has smart delegate combination (this code will execute after the previous behavior). If you need to overwrite this function before adding this functionality, first call
        /// <code>CombatAction.EffectOnChosenTargetsBeforeRolls = null;</code>
        /// before doing so.
        /// </para>
        /// <param name="doWhat">The code to execute once the action has hit and dealt damage. Uses the same parameters for this lambda as <see cref="QEffect.AfterYouDealDamage"/>.</param>
        /// <returns></returns>
        public CombatAction WithHitAndDealDamage(Func<Creature, CombatAction, Creature, Task> doWhat)
        {
            return caThis.WithPrologueEffectOnChosenTargetsBeforeRolls(async (innerAction, self, targets) =>
            {
                // Initialize to capture reference in scope
                QEffect doAfter = new QEffect()
                {
                    Name = "[AFTER YOU DEAL DAMAGE WITH: " + innerAction.Name + "]",
                    ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn, // Fallback
                };
                doAfter.AfterYouDealDamage = async (self2, innerAction2, target) =>
                {
                    if (innerAction2 != innerAction
                        || target != targets.ChosenCreature
                        || innerAction2.CheckResult < CheckResult.Success
                        || innerAction2.Item != caThis.Item)
                        return;

                    await doWhat.Invoke(self2, innerAction2, target);

                    doAfter.ExpiresAt = ExpirationCondition.Immediately;
                };
                self.AddQEffect(doAfter);
            });
        }
    }
    
    extension(QEffect qfThis)
    {
        /// <summary>
        /// Runs any modifications to the QEffect in one code block, similar to Zone.With().
        /// </summary>
        public QEffect With(Action<QEffect> changes)
        {
            changes.Invoke(qfThis);
            return qfThis;
        }

        /// <summary>
        /// Causes a QEffect to put an action in the Offense section of the creature stat block using the given action name, short description, and cost; but without listing any attack statistics. Useful for "metastrike" actions such as Power Attack, displaying them only once.
        /// </summary>
        public void WithDisplayActionInOffenseSection(string actionName, string shortDescription, int cost = 1)
        {
            qfThis.ProvideActionIntoPossibilitySection += (qfThis1, section) =>
            {
                // Inserts into invisible section
                if (section.PossibilitySectionId != PossibilitySectionId.InvisibleActions)
                    return null;
                CombatAction statBlockOnly = CombatAction.CreateSimple(
                        qfThis1.Owner,
                        actionName,
                        [])
                    .WithShortDescription(shortDescription)
                    .WithActionCost(cost);
                statBlockOnly.Illustration = IllustrationName.None;
                return new ActionPossibility(statBlockOnly);
            };
        }
    }

    extension(Item item)
    {
        /// <summary>
        /// Runs any modifications to the Item in one code block, similar to Zone.With().
        /// </summary>
        public Item With(Action<Item> changes)
        {
            changes.Invoke(item);
            return item;
        }
        
        /// <summary>
        /// Returns whether the item has all the passed traits.
        /// </summary>
        public bool HasAllTraits(Trait[] traits) =>
            item.Traits.All(traits.Contains);

        /// <summary>
        /// Returns whether the item has any of the passed traits.
        /// </summary>
        public bool HasAnyTraits(Trait[] traits) =>
            item.Traits.Any(traits.Contains);

        /// <summary>
        /// Adds flavor text to the item. If the flavorText or the rulesText is null, it won't add new lines.
        /// </summary>
        public Item WithDescription(string flavorText, string rulesText)
        {
            string newFlavor =
                (string.IsNullOrEmpty(flavorText) ? flavorText : "{i}" + flavorText + "{/i}")
                + (string.IsNullOrEmpty(rulesText) ? null : "\n\n");
            return item.WithDescription(newFlavor + rulesText);
        }
    }
    
    extension(Actions actions)
    {
        public bool RefundReaction(string question, Trait[] reactionTraits, bool refundAllMatching = false)
        {
            if (actions.GetType()
                    .GetField("creature", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(actions)
                is not Creature cr)
                return false;

            bool refunded = false;
            foreach (QEffect qf in cr.QEffects)
            {
                if (qf.OfferExtraReaction?.Invoke(qf, question, reactionTraits) is not { } keyword)
                    continue;
                if (!actions.ReactionsUsedUpThisRound.Contains(keyword))
                    continue;
                
                actions.ReactionsUsedUpThisRound.Remove(keyword);
                refunded = true;
                
                if (!refundAllMatching)
                    break;
            }

            return refunded;
        }

        public bool RefundReaction(string keyword)
        {
            if (!actions.ReactionsUsedUpThisRound.Contains(keyword))
                return false;
            
            actions.ReactionsUsedUpThisRound.Remove(keyword);
            return true;

        }
    }

    extension(Trait trait)
    {
        public bool IsTraditionTrait() =>
            trait is Trait.Arcane or Trait.Divine or Trait.Occult or Trait.Primal;
        
        public string TraditionTraitToColor()
        {
            switch (trait)
            {
                case Trait.Arcane:
                    return "DeepSkyBlue"; // Force damage color
                case Trait.Divine:
                    return "Goldenrod"; // Positive damage color
                case Trait.Occult:
                    return "Fuchsia"; // Mental damage color // "DarkOrchid" // "DarkViolet"
                case Trait.Primal:
                    return "Green";
                default:
                    return "Black";
            }
        }
    }

    extension(Feat feat)
    {
        public string ToLink(string caption)
        {
            return "{link:" + feat.ToTechnicalName() + "}" + caption + "{/}";
        }
    }

    extension(string text)
    {
        /// <summary>
        /// Adds color tags to the given string.
        /// </summary>
        /// <param name="color">The color, formatted as "Green", to be added to the string.</param>
        /// <returns></returns>
        public string WithColor(string color)
        {
            color = color.Capitalize();
            return "{"+color+"}" + text + "{/"+color+"}";
        }

        /// <summary>
        /// Pluralizes a word if count is greater than 1. Example: <code>"octop".PluralizeIf("us", "odes", numOctopus)</code>This will return the string "octopus" when numOctopus==1, or return "octopodes" otherwise.
        /// </summary>
        /// <param name="addSingular">The singular characters to add (if any) to the initial string.</param>
        /// <param name="addPlural">The plural characters to add to the initial string.</param>
        /// <param name="count">The quantity to compare to when determining if this word is plural or not.</param>
        /// <returns>The initial string with addSingular or addPlural added to the end of the string.</returns>
        public string PluralizeIf(string? addSingular, string addPlural, int count)
        {
            return text + (count == 1 ? addSingular : addPlural);
        }
    }

    extension(int number)
    {
        /// <summary>
        /// Adds color tags to the given integer.
        /// </summary>
        /// <param name="color">The color, formatted as "Green", to be added to the string.</param>
        /// <returns></returns>
        public string WithColor(string color)
        {
            color = color.Capitalize();
            return "{"+color+"}" + number + "{/"+color+"}";
        }
    }
    
    /// <summary>
    /// Functions as <see cref="Cinematics.ShowQuickBubble"/> but with a timed duration parameter. Useful for quick bubbles that need to display for a short duration without a voice line.
    /// </summary>
    public static async Task ShowQuickBubble(this Cinematics cinema, Creature speaker, string text, int milliseconds = 5000)
    {
        cinema.TutorialBubble = new TutorialBubble(
            speaker.Illustration,
            SubtitleModification.Replace(text),
            null);
        speaker.Battle.Log("{b}"+speaker.Name+":{/b} "+text);
        await speaker.Battle.SendRequest(new SleepRequest(milliseconds)
        {
            CanBeClickedThrough = true
        });
        cinema.TutorialBubble = null;
    }
    
    
    public static int DetermineCircumstanceBonusThresholdNeededToUpgrade(this CheckBreakdownResult resultBreakdown)
    {
        CheckBreakdown breakdown = resultBreakdown.CheckBreakdown;
        int rollTotal = breakdown.TotalCheckBonus + resultBreakdown.D20Roll;
        CheckResult result = CheckResult.CriticalSuccess;
        int thresholdToUpgrade = 1000;
        // Is not crit
        if (rollTotal < breakdown.TotalDC + 10)
        {
            result = CheckResult.Success;
            thresholdToUpgrade = (breakdown.TotalDC + 10) - rollTotal;
        }
        // Is failure
        if (rollTotal < breakdown.TotalDC)
        {
            result = CheckResult.Failure;
            thresholdToUpgrade = (breakdown.TotalDC) - rollTotal;
        }
        // Is fumble
        if (rollTotal <= breakdown.TotalDC - 10)
        {
            result = CheckResult.CriticalFailure;
            thresholdToUpgrade = (breakdown.TotalDC - 9) - rollTotal;
        }
        // Is nat-1
        if (resultBreakdown.D20Roll == 1)
        {
            if (result == CheckResult.CriticalFailure)
                thresholdToUpgrade += 10;
        }
        if (resultBreakdown.CheckBreakdown.DefenseBonuses == null
            || resultBreakdown.CheckBreakdown.DefenseBonuses.Count == 0)
            return thresholdToUpgrade;
        int num = resultBreakdown.CheckBreakdown.DefenseBonuses.Max(sb =>
            sb is not { BonusType: BonusType.Circumstance }
            || sb.Amount <= 0
                ? 0
                : sb.Amount);
        return thresholdToUpgrade + num;
    }

    extension(ModManager)
    {
        /// <summary>
        /// Creates a custom "Mod" trait which indicates which mod the traited content comes from. This trait is visible with a basic description that uses your humanized mod name.
        /// </summary>
        /// <param name="modTechnicalName">The technicalName of the mod such as "MoreDedications". The final technical name of this trait will be "Mod:MoreDedications".</param>
        /// <param name="modName">The humanized name of the mod such as "More Dedications".</param>
        public static Trait RegisterModNameTrait(string modTechnicalName, string modName)
        {
            return ModManager.RegisterTrait(
                "Mod:" + modTechnicalName,
                new TraitProperties(
                    "Mod",
                    true,
                    "This content comes from or is modified by {b}" + modName + "{/b}.",
                    false,
                    Color.LightSteelBlue,
                    false,
                    false));
        }
        
        /// <summary>
        /// As <see cref="ModManager.AddFeat"/>, but it removes the Mod trait and adds your mod's specific trait.
        /// </summary>
        /// <param name="newFeat">The feat to register.</param>
        /// <param name="modName">The mod-source trait to replace the "Mod" trait with.</param>
        public static void AddFeat(Feat newFeat, Trait modName)
        {
            ModManager.AddFeat(newFeat);
            newFeat.Traits.Remove(Trait.Mod);
            newFeat.Traits.Insert(0, modName);
        }
        
        /// <summary>
        /// As <see cref="ModManager.AddFeat"/>, but it registers the given strings as a mod-source trait and replaces the "Mod" trait with the new trait.
        /// </summary>
        /// <seealso cref="AddFeat(Feat, Trait)"/>
        /// <seealso cref="RegisterModNameTrait(string, string)"/>
        /// <param name="newFeat"></param>
        /// <param name="modTechnicalName"></param>
        /// <param name="modName"></param>
        public static void AddFeat(Feat newFeat, string modTechnicalName, string modName)
        {
            Trait modTrait = ModManager.RegisterModNameTrait(modTechnicalName, modName);
            ModManager.AddFeat(newFeat, modTrait);
        }
    }

    #endregion

    #region Statics
    
    /// <summary>
    /// Alternative overload for <see cref="AllSpells.CreateSpellLink"/> which includes the spell's level.
    /// </summary>
    public static string CreateSpellLink(SpellId spell, Trait classOfOrigin, int spellLevel)
    {
        Spell template = AllSpells.CreateModernSpellTemplate(spell, classOfOrigin, spellLevel);
        string str = template.CombatActionSpell.SpellInformation != null
            ? ":" + template.CombatActionSpell.SpellInformation.ClassOfOrigin.ToStringOrTechnical() + ":" + spellLevel
            : "";
        return $"{{i}}{{link:{template.SpellId.ToStringOrTechnical()}{str}}}{template.Name.ToLower()}{{/link}}{{/i}}";
    }

    /// <summary>
    /// If a character sheet is available at the execution time of this function, it will return a character sheet of a party member either during campaign play or in free encounter play.
    /// </summary>
    /// <param name="index">The 0th-indexed party member.</param>
    public static CharacterSheet? GetCharacterSheetFromPartyMember(int index)
    {
        CharacterSheet? hero = null;
        if (CampaignState.Instance is { } campaign)
            hero = campaign.Heroes[index].CharacterSheet;
        else if (CharacterLibrary.Instance is { } library)
            hero = library.SelectedRandomEncounterParty[index];
        return hero;
    }

    /// <summary>
    /// Consolidates code commonly seen when using <see cref="GameLoop.OfferOptions"/>, with extra handling for when OfferOptions is used off-turn.
    /// </summary>
    public static async Task OfferOptions2(Creature self, Func<ActionPossibility, bool> filter)
    {
        Possibilities poss =  Possibilities
            .Create(self)
            .Filter(ap =>
            {
                ap.CombatAction.ActionCost = 0;
                if (!filter.Invoke(ap.CombatAction))
                    return false;
                ap.RecalculateUsability();
                return true;
            });
        
        Creature? active = self.Battle.ActiveCreature;
        self.Battle.ActiveCreature = self;
        self.Possibilities = poss;
        
        List<Option> actions = await self.Battle.GameLoop.CreateActions(
            self,
            poss,
            null);
        self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
        await self.Battle.GameLoop.OfferOptions(self, actions, true);
        
        self.Battle.ActiveCreature = active;
    }

    /// <summary>
    /// Consolidates code commonly seen when using <see cref="GameLoop.OfferOptions"/>, with extra handling for when OfferOptions is used off-turn.
    /// </summary>
    public static async Task OfferOptions2(Creature self, Func<Possibility, bool> filter)
    {
        Possibilities poss =  Possibilities
            .Create(self)
            .FilterAnyPossibility(poss =>
            {
                if (!filter.Invoke(poss))
                    return false;
                return true;
            });
        
        Creature? active = self.Battle.ActiveCreature;
        self.Battle.ActiveCreature = self;
        self.Possibilities = poss;
        
        List<Option> actions = await self.Battle.GameLoop.CreateActions(
            self,
            poss,
            null);
        self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
        await self.Battle.GameLoop.OfferOptions(self, actions, true);
        
        self.Battle.ActiveCreature = active;
    }

    #endregion
}