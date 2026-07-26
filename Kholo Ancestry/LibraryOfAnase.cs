using System.Reflection;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.KholoAncestry;

/// <summary>
/// Anase's library of helpful code functions. Contains a wide array of broadly useful functions rather than specialized logic.
/// </summary>
/// <list type="bullet">
/// <item>v2.4: Add Feat.WithLevelPrereq(int), TrueFeat.WithLevelPrereq(int), and TrueFeat.With()..</item>
/// <item>v2.3: Add WithExtraTrait(int, Trait).</item>
/// <item>v2.2: Remove Trait.Mod automations in deference to new base game architecture for mod identifiers. Remove WithDisplayActionInOffenseSection. Add CombatAction.WithIllustration.</item>
/// <item>v2.1: Make GetCharacterSheetFromPartyMember into a static extension. Update unused SafelyRegisterEnumMember to newer versions of SafelyRegister from my individual projects, now called TryRegisterEnumMember.</item>
/// <item>v2.0: Added ClassFeature.FromFeat().</item>
/// <item>v1.9: SpellId.ToLink() now automatically lowercases and italicizes the caption. Added alternative Creature.HasEffect() overloads. Added QEffect.WithDescription().</item>
/// <item>v1.8: Added int.WithColor() and int.WithTag(). Made all WithColor and WithTag functions optionally apply colors when null. Fix StrikeCreature overload to not return false if not providing a validity function.</item>
/// <item>v1.7: Refactored string.ToColor, added string.WithTag() and string.WithLink(). Refactored some ToLink() functions and added more to various enums. Added Feat.With(). Added Defense.ToColor(). Add functions to filter valid Strike possibilities to CommonCombatActions.StrikeCreature() and .GetStrikePossibilities(). GetStrikePossibilities also now adds a thrown Strike for melee thrown weapons.</item>
/// <item>v1.6: Added Trait extensions: IsTraditionTrait(), TraditionTraitToColor(). Added Feat.ToLink(caption). Added Item.With(). Converted various overloads into instance and static extension blocks. Added more flexible CommonCombatActions.StrikeCreature overload. Added CombatAction.CreatePass and a parameter to OfferOptions2 that uses it. Added FilterAnyPossibility2 functions to allow seeing SubmenuPossibilities.</item>
/// <item>v1.5: Replaced error-prone params keywords with regular arrays. Added RefundReaction extensions. Added more robust PluralizeIf extension. Added ModManager extensions.</item>
/// <item>v1.4: Added Item.WithDescription(flavorText, rulesText).</item>
/// <item>v1.3: Added CombatAction.HasAllTraits, CombatAction.HasAnyTraits, OfferOptions2 with variants for ActionPossibility and Possibility.</item>
/// <item>v1.2: Added CreateSpellLink(SpellId, Trait, int). Refactored into Extension blocks.</item>
/// <item>v1.1: Added int.WithColor(), QEffect.With(), CombatAction.With(), Item.HasAllTraits, Item.HasAnyTraits.</item>
/// <item>v1.0: Initial.</item>
/// </list>
/// <value>v2.4</value>
public static class LibraryOfAnase
{
    extension(Creature cr)
    {
        /// <summary>
        /// Returns whether you have a QEffect of the given Id that meets the given condition. This is not as efficient as <see cref="Creature.HasEffect(QEffectId)"/>.
        /// </summary>
        public bool HasEffect(QEffectId id, Func<QEffect,bool> condition)
        {
            return cr.QEffects.Any(qf => qf.Id == id && condition(qf));
        }
        
        /// <summary>
        /// Returns whether you have a QEffect that meets the given condition. This is not as efficient as <see cref="Creature.HasEffect(QEffectId)"/>.
        /// </summary>
        public bool HasEffect(Func<QEffect,bool> condition)
        {
            return cr.QEffects.Any(condition);
        }
    }

    extension(CombatAction caThis)
    {
        /// <summary>
        /// Creates an action that represents passing an action, such as during an OfferReaction routine.
        /// </summary>
        public static CombatAction CreatePass(Creature owner, Action<CombatAction,Creature>? effectOnSelf)
        {
            return new CombatAction(
                    owner,
                    IllustrationName.EndTurn,
                    "Pass",
                    [Trait.Basic, Trait.UsableEvenWhenUnconsciousOrParalyzed, Trait.DoesNotPreventDelay],
                    "Do nothing.",
                    Target.Self())
                .WithActionCost(0)
                .WithEffectOnSelf(async (action, self) =>
                {
                    effectOnSelf?.Invoke(action, self);
                });
        }
        
        /// <summary>
        /// Runs any modifications to the CombatAction in one code block, similar to Zone.With().
        /// </summary>
        public CombatAction With(Action<CombatAction> changes)
        {
            changes.Invoke(caThis);
            return caThis;
        }

        /// <summary>
        /// Adds a trait at the specified position in the list.
        /// </summary>
        public CombatAction WithExtraTrait(int position, Trait trait)
        {
            List<Trait> traits = caThis.Traits.ToList();
            traits.Insert(position, trait);
            caThis.Traits = new Traits(traits, caThis);
            return caThis;
        }

        /// <summary>
        /// Sets the Illustration of the CombatAction.
        /// </summary>
        public CombatAction WithIllustration(Illustration icon)
        {
            caThis.Illustration = icon;
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

        public QEffect WithDescription(string newDescription)
        {
            qfThis.Description = newDescription;
            return qfThis;
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

        /// <summary>
        /// Outputs a link to this item.
        /// </summary>
        /// <param name="caption">The caption of the link, such as "dagger".</param>
        public string ToLink(string caption)
        {
            return item.ItemName.ToLink(caption);
        }
    }

    extension(ItemName itemName)
    {
        /// <summary>
        /// Outputs a link to this item.
        /// </summary>
        /// <param name="caption">The caption of the link, such as "dagger".</param>
        public string ToLink(string caption)
        {
            return caption.WithLink(itemName.ToStringOrTechnical());
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
        /// <summary>
        /// Runs any modifications to the Feat in one code block, similar to Zone.With().
        /// </summary>
        public Feat With(Action<Feat> changes)
        {
            changes.Invoke(feat);
            return feat;
        }

        /// <summary>
        /// Updates a feat to use a new level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public Feat WithLevelPrereq(int level)
        {
            feat.LevelIfAny = level;
            feat.Prerequisites.RemoveAll(req => req is LevelPrerequisite);
            feat.Prerequisites.Insert(0, new LevelPrerequisite(level));
            return feat;
        }
        
        /// <summary>
        /// Outputs a link to this feat.
        /// </summary>
        /// <param name="caption">The caption of the link, such as "Shield Block {icon:Reaction}".</param>
        public string ToLink(string caption)
        {
            return feat.FeatName.ToLink(caption);
        }
    }

    extension(TrueFeat feat)
    {
        /// <summary>
        /// Runs any modifications to the TrueFeat in one code block, similar to Zone.With().
        /// </summary>
        public TrueFeat With(Action<Feat> changes)
        {
            changes.Invoke(feat);
            return feat;
        }

        /// <summary>
        /// Updates a feat to use a new level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public TrueFeat WithLevelPrereq(int level)
        {
            feat.LevelIfAny = level;
            feat.Prerequisites.RemoveAll(req => req is LevelPrerequisite);
            feat.Prerequisites.Insert(0, new LevelPrerequisite(level));
            return feat;
        }
    }

    extension(FeatName featName)
    {
        /// <summary>
        /// Outputs a link to this feat.
        /// </summary>
        /// <param name="caption">The caption of the link, such as "Shield Block {icon:Reaction}".</param>
        public string ToLink(string caption)
        {
            return caption.WithLink(featName.ToStringOrTechnical());
        }
    }

    extension(ClassFeature)
    {
        /// <summary>
        /// This creates a class feature whose caption links to a Feat link block, with no details. It grants the feat and subFeat (if any).
        /// </summary>
        public static ClassFeature FromFeat(FeatName featName, FeatName? subFeat = null, bool titleCase = true)
        {
            Feat feat = AllFeats.GetFeatByFeatName(featName);
            string name = titleCase
                ? feat.Name
                : feat.Name.ToLower();
            return new ClassFeature(featName.ToLink(name))
            {
                OnSheet = values => values.GrantFeat(featName, subFeat)
            };
        }
    }

    extension(Defense def)
    {
        public string ToColor()
        {
            switch (def)
            {
                case Defense.AC:
                    return nameof(Color.DimGray);
                case Defense.Reflex:
                    return nameof(Color.Goldenrod);
                case Defense.Fortitude:
                    return nameof(Color.Green);
                case Defense.Will:
                    return nameof(Color.Fuchsia);
                default:
                    return "Black";
            }
        }
    }

    extension(ModManager)
    {
        /// <summary>
        /// Attempts to register the source enum to the game.
        /// </summary>
        /// <param name="technicalName">The technicalName string of the enum being registered. If registering a trait, this is the displayName, according to the parameter specifications of <see cref="ModManager.RegisterTrait"/>.</param>
        /// <param name="extraParams">An array of optional parameters. For a <see cref="FeatName"/>, the first parameter is a human-readable display name. For a <see cref="Trait"/>, the first parameter is a <see cref="TraitProperties"/>.</param>
        /// <param name="enumValue">The enum member you registered, which might already exist for that name and type.</param>
        /// <typeparam name="T">The enum type such as <see cref="SpellId"/>, <see cref="FeatName"/>, or <see cref="QEffectId"/>.</typeparam>
        /// <returns>Whether the enum was already registered.</returns>
        public static bool TryRegisterEnumMember<T>(string technicalName, object[]? extraParams, out T enumValue) where T : struct, Enum
        {
            bool alreadyRegistered = ModManager.TryParse(technicalName, out T oldRegistration);
            if (alreadyRegistered)
                enumValue = oldRegistration;
            else
            {
                Type type = typeof(T);
                if (type == typeof(FeatName))
                    enumValue = (T)(Enum)ModManager.RegisterFeatName(technicalName, (string?)extraParams?[0]);
                else if (type == typeof(Trait))
                    enumValue = (T)(Enum)ModManager.RegisterTrait(technicalName, (TraitProperties?)extraParams?[0]);
                else
                    enumValue = ModManager.RegisterEnumMember<T>(technicalName);
            }

            return alreadyRegistered;
        }

        /// <summary>
        /// Registers the source enum to the game, or returns the original if it's already registered.
        /// </summary>
        /// <param name="technicalName">The technicalName string of the enum being registered. If registering a trait, this is the displayName, according to the parameter specifications of <see cref="ModManager.RegisterTrait"/>.</param>
        /// <param name="extraParams">An array of optional parameters. For a <see cref="FeatName"/>, the first parameter is a human-readable display name. For a <see cref="Trait"/>, the first parameter is a <see cref="TraitProperties"/>.</param>
        /// <typeparam name="T">The enum type such as <see cref="SpellId"/>, <see cref="FeatName"/>, or <see cref="QEffectId"/>.</typeparam>
        /// <returns>The newly registered enum.</returns>
        public static T SafelyRegisterEnumMember<T>(string technicalName, object[]? extraParams = null) where T : struct, Enum
        {
            bool alreadyRegistered = ModManager.TryParse(technicalName, out T oldRegistration);
            
            if (alreadyRegistered)
                return oldRegistration;
            
            Type type = typeof(T);
            if (type == typeof(FeatName))
                return (T)(Enum)ModManager.RegisterFeatName(technicalName, (string?)extraParams?[0]);
            if (type == typeof(Trait))
                return (T)(Enum)ModManager.RegisterTrait(technicalName, (TraitProperties?)extraParams?[0]);
            else
                return ModManager.RegisterEnumMember<T>(technicalName);
        }
    }

    extension(CommonCombatActions)
    {
        /// <summary>
        /// Functions as <see cref="CommonCombatActions.StrikeCreature(Creature, Func{Creature,bool}?, bool, string?, bool)"/> except you can overwrite the topbar's icon and question, modify each Strike as it's being generated, and filter Strikes.
        /// </summary>
        public static async Task<bool> StrikeCreature(
            Creature self,
            Func<CombatAction, bool>? isValidStrike,
            Action<CombatAction>? adjustStrike,
            Func<Creature, bool>? isValidTarget,
            Illustration? topBarIcon,
            string? topBarText,
            bool allowCancel,
            string? allowPass)
        {
            List<Option> possibilities = CommonCombatActions.GetStrikePossibilities(self, isValidStrike, adjustStrike, isValidTarget);
            if (allowCancel)
                possibilities.Add(new CancelOption(true));
            else if (allowPass != null)
                possibilities.Add(new PassViaButtonOption(allowPass));
            if (possibilities.Count <= 0)
                return false;
            if (possibilities.Count == 1)
            {
                await possibilities[0].Action();
                return possibilities[0] is not CancelOption && possibilities[0] is not PassViaButtonOption;
            }
            RequestResult result = await self.Battle.SendRequest(new AdvancedRequest(
                self,
                $"{topBarText ?? "Choose a creature to Strike"}{(allowCancel ? " or right-click to cancel." : "")}.",
                possibilities)
            {
                TopBarText = topBarText ?? "Choose a creature to Strike.",
                TopBarIcon = topBarIcon ?? IllustrationName.Fist
            });
            await result.ChosenOption.Action();
            return result.ChosenOption is not CancelOption && result.ChosenOption is not PassViaButtonOption;
        }
        
        /// <summary>
        /// Functions as <see cref="CommonCombatActions.GetStrikePossibilities(Creature, bool, Func{Creature,bool}?)"/> except you can modify each Strike as it's being generated, and filter Strikes.
        /// </summary>
        public static List<Option> GetStrikePossibilities(
            Creature self,
            Func<CombatAction, bool>? isValidStrike,
            Action<CombatAction>? adjustStrike,
            Func<Creature, bool>? isValidTarget)
        {
            List<Option> options = [];
            foreach (Item item in self.Weapons)
            {
                CombatAction strike = StrikeRules.CreateStrike(
                        self,
                        item,
                        item.HasTrait(Trait.Ranged)
                            ? RangeKind.Ranged
                            : RangeKind.Melee,
                        -1);
                FilterAndAdd(strike);
                // If this is a melee weapon that can be thrown, add another possibility
                if (item.HasTrait(Trait.Melee) && item.WeaponProperties!.Throwable)
                {
                    CombatAction thrown = StrikeRules.CreateStrike(
                        self,
                        item,
                        RangeKind.Ranged,
                        -1,
                        true);
                    FilterAndAdd(thrown);
                }
            }
            return options;

            void FilterAndAdd(CombatAction strike)
            {
                strike.WithActionCost(0);
                if (strike.Item!.HasTrait(Trait.Ranged))
                    strike.WithSoundEffect(strike.SoundEffectName ?? SfxName.Bow);
                if (isValidStrike?.Invoke(strike) is false)
                    return;
                adjustStrike?.Invoke(strike);
                if (isValidTarget != null)
                    ((CreatureTarget) strike.Target).CreatureTargetingRequirements.Add(new LegacyCreatureTargetingRequirement((a, d) =>
                        !isValidTarget(d)
                            ? Usability.NotUsableOnThisCreature("excluded")
                            : Usability.Usable));
                GameLoop.AddDirectUsageOnCreatureOptions(strike, options);
            }
        }
    }

    extension(AllSpells)
    {
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
    }

    extension(SpellId id)
    {
        /// <summary>
        /// Outputs a link to this spell.
        /// </summary>
        /// <param name="caption">The caption of the link, such as "fireball" or "5th-level fireball". Spell names should be in lower-case.</param>
        /// <param name="classOfOrigin">The class origin of this spell, if any.</param>
        /// <param name="spellLevel">The specific level of the spell, if any.</param>
        public string ToLink(string caption, Trait? classOfOrigin, int? spellLevel)
        {
            string?[] parameters = [classOfOrigin?.ToStringOrTechnical(), spellLevel?.ToString()];
            return caption.ToLower().WithLink(id.ToStringOrTechnical(), parameters.WhereNotNull().ToArray()).WithTag("i");
        }
    }

    extension(GameLoop gl)
    {
        /// <summary>
        /// Consolidates code commonly seen when using <see cref="GameLoop.OfferOptions(Creature, List{Option}, bool)"/>, with extra handling for when OfferOptions is used off-turn.
        /// </summary>
        public async Task OfferOptions2(Creature self, Func<ActionPossibility, bool> filter, bool canPass = false)
        {
            Possibilities poss = Possibilities
                .Create(self)
                .Filter(ap =>
                {
                    ap.CombatAction.ActionCost = 0;
                    if (!filter.Invoke(ap.CombatAction))
                        return false;
                    ap.RecalculateUsability();
                    return true;
                });
            poss.CannotPass = canPass;
            if (canPass)
                poss.Sections.Add(new PossibilitySection("Pass")
                {
                    Possibilities = [new ActionPossibility(CombatAction.CreatePass(self, null))]
                });
            
            Creature? active = self.Battle.ActiveCreature;
            self.Battle.ActiveCreature = self;
            self.Possibilities = poss;
            
            List<Option> actions = await gl.CreateActions(
                self,
                poss,
                null);
            self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
            await gl.OfferOptions(self, actions, true);
            
            self.Battle.ActiveCreature = active;
        }

        /// <summary>
        /// Consolidates code commonly seen when using <see cref="GameLoop.OfferOptions(Creature, List{Option}, bool)"/>, with extra handling for when OfferOptions is used off-turn.
        /// </summary>
        public async Task OfferOptions2(Creature self, Func<Possibility, bool> filter, bool canPass = false)
        {
            Possibilities poss = Possibilities
                .Create(self)
                .FilterAnyPossibility2(poss =>
                {
                    if (!filter.Invoke(poss))
                        return false;
                    return true;
                });
            poss.CannotPass = canPass;
            if (canPass)
                poss.Sections.Add(new PossibilitySection("Pass")
                {
                    Possibilities = [new ActionPossibility(CombatAction.CreatePass(self, null))]
                });
            
            Creature? active = self.Battle.ActiveCreature;
            self.Battle.ActiveCreature = self;
            self.Possibilities = poss;
            
            List<Option> actions = await gl.CreateActions(
                self,
                poss,
                null);
            self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
            await gl.OfferOptions(self, actions, true);
            
            self.Battle.ActiveCreature = active;
        }
    }

    extension(Possibilities poss)
    {
        public Possibilities FilterAnyPossibility2(Func<Possibility, bool> keepOnlyWhat)
        {
            // Constructor is private. Use expensive work-around for an empty list.
            //Possibilities filtered = new Possibilities();
            Possibilities filtered = poss.FilterAnyPossibility(_ => false);
            foreach (var section in poss.Sections)
            {
                var filtered2 = section.FilterAnyPossibility2(keepOnlyWhat);
                if (filtered2 != null) filtered.Sections.Add(filtered2);
            }

            return filtered;
        }
    }

    extension(PossibilitySection sect)
    {
        /// <summary>
        /// Works as <see cref="PossibilitySection.FilterAnyPossibility"/> except the condition is also ran on the submenu, allowing it to be returned even if any possibilities it contains are empty.
        /// </summary>
        public PossibilitySection? FilterAnyPossibility2(Func<Possibility, bool> keepOnlyWhat)
        {
            PossibilitySection filtered = new PossibilitySection(sect.Name);
            foreach (var possibility in sect.Possibilities)
            {
                if (possibility is SubmenuPossibility submenuPossibility)
                {
                    var filtered2 = submenuPossibility.FilterAnyPossibility2(keepOnlyWhat);
                    if (filtered2 != null)
                        filtered.Possibilities.Add(filtered2);
                }
                else if (keepOnlyWhat(possibility))
                    filtered.Possibilities.Add(possibility);
            }

            return filtered.Possibilities.Any() ? filtered : null;
        }
    }

    extension(SubmenuPossibility subPoss)
    {
        /// <summary>
        /// Works as <see cref="SubmenuPossibility.FilterAnyPossibility"/> except the condition is also ran on the submenu, allowing it to be returned even if any possibilities it contains are empty.
        /// </summary>
        public SubmenuPossibility? FilterAnyPossibility2(Func<Possibility, bool> keepOnlyWhat)
        {
            var filtered = new SubmenuPossibility(subPoss.Illustration, subPoss.Caption, subPoss.PossibilitySize);
            bool keepWholeMenu = keepOnlyWhat(subPoss);
            foreach (var subsection in subPoss.Subsections)
            {
                var filtered2 = subsection.FilterAnyPossibility2(keepWholeMenu ? _ => true : keepOnlyWhat);
                if (filtered2 != null)
                    filtered.Subsections.Add(filtered2);
            }

            if (filtered.Subsections.Count != 0)
                return filtered;
            return null;
        }
    }
    
    extension(Cinematics cinema)
    {
        /// <summary>
        /// Functions as <see cref="Cinematics.ShowQuickBubble"/> but with a timed duration parameter. Useful for quick bubbles that need to display for a short duration without a voice line.
        /// </summary>
        public async Task ShowQuickBubble(Creature speaker, string text, int milliseconds = 5000)
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
    }
    
    extension(CheckBreakdownResult resultBreakdown)
    {
        public int DetermineCircumstanceBonusThresholdNeededToUpgrade()
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
    }

    extension(string text)
    {
        /// <summary>
        /// Surrounds a given string with color tags.
        /// </summary>
        /// <param name="color">The color, formatted as "Green", to be added to the string. If null, returns original string.</param>
        public string WithColor(string? color)
        {
            return color is null ? text : text.WithTag(color.Capitalize());
        }

        /// <summary>
        /// Surrounds a string with any arbitrary tag. 
        /// </summary>
        /// <param name="tag">The tag to be added to the string, such as "i". If null, returns original string.</param>
        public string WithTag(string? tag)
        {
            return tag is null ? text : "{" + tag + "}" + text + "{/" + tag + "}";
        }

        /// <summary>
        /// Surrounds a given humanized string with a basic link tag.
        /// </summary>
        /// <param name="link">The link technical name such as "MinorHealingPotion".</param>
        /// <param name="parameters">If this link has parameters like a spell's class or level, this is those parameters.</param>
        public string WithLink(string link, string[]? parameters = null)
        {
            return "{link:" + link + (parameters is not null ? string.Join("", parameters.Select(para => ":" + para)) : null) + "}" + text + "{/}";
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
        /// <param name="color">The color, formatted as "Green", to be added to the string. If null, returns int as a string.</param>
        /// <returns></returns>
        public string WithColor(string? color)
        {
            return color is null ? number.ToString() : number.ToString().WithColor(color);
        }

        /// <summary>
        /// Surrounds a string with any arbitrary tag. 
        /// </summary>
        /// <param name="tag">The tag to be added to the string, such as "i". If null, returns int as a string.</param>
        public string WithTag(string? tag)
        {
            return tag is null ? number.ToString() : "{" + tag + "}" + number + "{/" + tag + "}";
        }
    }

    extension(CharacterSheet)
    {
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
    }
}