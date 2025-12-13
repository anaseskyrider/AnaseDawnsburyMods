using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.AndroidAncestry;

/// <summary>
/// Anase's library of helpful code functions. Contains a wide array of broadly useful functions rather than specialized logic.
/// </summary>
/// <list type="bullet">
/// <item>v1.2: Added CreateSpellLink(SpellId, Trait, int). Refactored into Extension blocks.</item>
/// <item>v1.1: Added int.WithColor(), QEffect.With(), CombatAction.With(), Item.HasAllTraits, Item.HasAnyTraits.</item>
/// <item>v1.0: Initial.</item>
/// </list>
/// <value>v1.1</value>
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
        /// Returns whether the item has all the passed traits.
        /// </summary>
        public bool HasAllTraits(params Trait[] traits) =>
            item.Traits.All(traits.Contains);

        /// <summary>
        /// Returns whether the item has any of the passed traits.
        /// </summary>
        public bool HasAnyTraits(params Trait[] traits) =>
            item.Traits.Any(traits.Contains);
    }

    /// <summary>
    /// Adds color tags to the given string.
    /// </summary>
    /// <param name="text">The original string that this function extends off of.</param>
    /// <param name="color">The color, formatted as "Green", to be added to the string.</param>
    /// <returns></returns>
    public static string WithColor(this string text, string color)
    {
        color = color.Capitalize();
        return "{"+color+"}" + text + "{/"+color+"}";
    }
    
    /// <summary>
    /// Adds color tags to the given integer.
    /// </summary>
    /// <param name="number">The integer that this function extends off of.</param>
    /// <param name="color">The color, formatted as "Green", to be added to the string.</param>
    /// <returns></returns>
    public static string WithColor(this int number, string color)
    {
        color = color.Capitalize();
        return "{"+color+"}" + number + "{/"+color+"}";
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

    #endregion
}