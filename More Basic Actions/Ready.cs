using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Alchemy;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.ReactiveAttacks;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class Ready
{
    public static readonly List<ReadyTrigger> Triggers = [];
    public static readonly List<ReadyResponse> Responses = [];

    public static void LoadReady()
    {
        CreateTriggers();
        CreateResponses();
        AddReadyToEveryCreature();
    }

    public static void CreateTriggers()
    {
        Triggers.Add(new ReadyTrigger(
            "Brace",
            "An enemy moves into your reach",
            typeof(Creature),
            null,
            (trigger, response, brace) =>
            {
                brace.AddGrantingOfTechnical(
                    (qfThis, cr) => cr.EnemyOf(qfThis.Owner),
                    qfTech =>
                    {
                        qfTech.AfterYouMoveOneSquare = async (qfTech2, action, style, before, after) =>
                        {
                            if (action is null
                                || style is null
                                || !action.HasTrait(Trait.Move)
                                || action.HasTrait(Trait.DoesNotProvoke)
                                || brace.Owner.DistanceTo(qfTech.Owner) > brace.Owner.Space.ActualReach)
                                return;

                            await response.Response.Invoke(new ReadyEvent(trigger, response, brace)
                            {
                                Creatures = [ qfTech.Owner ],
                                Actions = [ action ]
                            });
                        };
                    });

                if (response.Name.Contains("Strike"))
                {
                    brace.Owner.AddQEffect(new QEffect(
                        "Bracing",
                        "Your Strikes with brace weapons deal an additional 2 precision damage for each weapon damage die.\n\nThis effect doesn't apply during your turn.\n\n",
                        ExpirationCondition.ExpiresAtStartOfYourTurn,
                        brace.Owner,
                        ModData.Illustrations.Ready)
                    {
                        YourStrikeMayDealPrecisionDamage = (qfThis, action, defender) =>
                        {
                            if (!action.HasTrait(ModData.Traits.Brace)
                                || qfThis.Owner.Battle.ActiveCreature == qfThis.Owner)
                                return null;

                            int braceBonus = (action.Item?.WeaponProperties?.DamageDieCount ?? 0) * 2;
                            if (braceBonus > 0)
                                return DiceFormula.FromText(
                                    braceBonus.ToString(),
                                    "Brace (precision)");

                            return null;
                        }
                    });
                }
            }));
        
        Triggers.Add(new ReadyTrigger(
            "Footwork",
            "An enemy ends a move action adjacent to you, and that action wasn't Step or a similar action",
            typeof(Creature),
            null,
            (trigger, response, footwork) =>
            {
                footwork.AddGrantingOfTechnical(
                    (qfThis, cr) => cr.EnemyOf(qfThis.Owner),
                    qfTech =>
                    {
                        qfTech.AfterYouTakeAction = async (qfThis, action) =>
                        {
                            if (!action.HasTrait(Trait.Move)
                                || action.HasTrait(Trait.DoesNotProvoke)
                                || action.ActionId == ActionId.Step
                                || action.TilesMoved == 0
                                || !qfThis.Owner.IsAdjacentTo(footwork.Owner))
                                return;
                            
                            await response.Response.Invoke(new ReadyEvent(trigger, response, footwork)
                            {
                                Creatures = [ action.Owner ],
                                Actions = [ action ]
                            });
                        };
                    });
            }));
        
        Triggers.Add(new ReadyTrigger(
            "Hold",
            "An enemy enters the maximum range or the first range increment of a ranged attack you have",
            typeof(Creature),
            response => response.Name == "Triggered Strike", // Hold only makes sense in the context of a Strike.
            (trigger, response, hold) =>
            {
                hold.AddGrantingOfTechnical(
                    cr => cr.EnemyOf(hold.Owner),
                    qfTech =>
                    {
                        qfTech.AfterYouMoveOneSquare = async (qfTech2, action, style, previous, next) =>
                        {
                            if (action is null || style is null)
                                return;
                            
                            List<Item> rangedAttacks = GetRangedAttacks(hold.Owner);
                            
                            if (rangedAttacks.Count == 0)
                                return;
                            
                            List<Item> promptWeapons = rangedAttacks
                                .Where(weapon =>
                                {
                                    int range = weapon.WeaponProperties!.RangeIncrement > 0
                                        ? weapon.WeaponProperties!.RangeIncrement
                                        : weapon.WeaponProperties!.MaximumRange;
                                    // New tile is in range
                                    if (hold.Owner.DistanceTo(next) > range)
                                        return false;
                                    // Old tile was not in range
                                    if (hold.Owner.DistanceTo(previous) <= range)
                                        return false;
                                    return true;
                                })
                                .ToList();

                            if (promptWeapons.Count <= 0)
                                return;

                            QEffect allowRangedAttacks = new QEffect() { Id = QEffectId.MobileShot };
                            hold.Owner.AddQEffect(allowRangedAttacks);
                            
                            await response.Response.Invoke(new ReadyEvent(trigger, response, hold)
                            {
                                Creatures = [ qfTech.Owner ],
                                Tag = promptWeapons
                            });

                            hold.Owner.RemoveAllQEffects(qf => qf == allowRangedAttacks);
                        };
                    });

                return;

                List<Item> GetRangedAttacks(Creature cr)
                {
                    return cr.Weapons.Where(wep =>
                            wep.HasTrait(Trait.Ranged)
                            && wep.WeaponProperties is not null
                            && (wep.WeaponProperties.RangeIncrement > 0
                                || wep.WeaponProperties.MaximumRange > 0))
                        .ToList();
                }
            }));
        
        Triggers.Add(new ReadyTrigger(
            "Seize Opportunity",
            "An enemy within your range or reach becomes {r}flat-footed{/r} to you",
            typeof(Creature),
            null,
            (trigger, response, seize) =>
            {
                seize.Tag = seize.Owner.Battle.AllCreatures
                    .Where(cr =>
                        cr.EnemyOf(seize.Owner)
                        && cr.IsFlatfootedToBecause(seize.Owner, null) == null
                        && !cr.Cache.FlankedBy.Contains(seize.Owner))
                    .ToList(); // Creatures who've been made flat-footed since last reaction-prompt

                seize.StateCheckLayer = 1;
                seize.StateCheckWithVisibleChanges = async qfThis =>
                {
                    Creature self = qfThis.Owner;

                    if (self.PrimaryWeaponIncludingRanged == null)
                        return;

                    List<Creature> provokeQueue = (qfThis.Tag as List<Creature>)!;

                    foreach (Creature cr in self.Battle.AllCreatures
                                 .Where(cr => cr.EnemyOf(self))
                                 .ToList())
                    {
                        if (cr.IsFlatfootedToBecause(self, null) == null
                            && !cr.Cache.FlankedBy.Contains(self))
                        {
                            provokeQueue.Remove(cr);
                            continue;
                        }

                        if (provokeQueue.Contains(cr))
                            continue;

                        await response.Response.Invoke(new ReadyEvent(trigger, response, seize)
                        {
                            Creatures = [ cr ]
                        });

                        provokeQueue.Add(cr);
                    }
                };
            }));

        Triggers.Add(new ReadyTrigger(
            "Wide Open",
            "An enemy exits cover or lowers their shield",
            typeof(Creature),
            null,
            (trigger, response, wide) =>
            {
                // Creatures who don't have cover or a shield
                wide.Tag = wide.Owner.Battle.AllCreatures
                    .Where(wide.Owner.EnemyOf)
                    .Where(cr => !HasCoverOrShield(wide.Owner, cr))
                    .ToList();
                
                wide.StateCheckLayer = 1;
                wide.StateCheckWithVisibleChanges = async qfThis =>
                {
                    if (qfThis.Owner.PrimaryWeaponIncludingRanged == null)
                        return;

                    // List of creatures who don't have cover or a shield
                    List<Creature> provokeQueue = (qfThis.Tag as List<Creature>)!;

                    foreach (Creature cr in qfThis.Owner.Battle.AllCreatures
                                 .Where(qfThis.Owner.EnemyOf)
                                 .ToList())
                    {
                        if (HasCoverOrShield(qfThis.Owner, cr))
                        {
                            provokeQueue.Remove(cr);
                            continue;
                        }

                        if (provokeQueue.Contains(cr))
                            continue;

                        await response.Response.Invoke(new ReadyEvent(trigger, response, wide)
                        {
                            Creatures = [ cr ]
                        });

                        provokeQueue.Add(cr);
                    }
                };
                
                return;

                bool HasCoverOrShield(Creature me, Creature cr)
                {
                    return
                        me.HasLineOfEffectTo(cr) > CoverKind.None
                        || cr.Defenses.DetermineDefenseBonuses(
                                me,
                                me.PrimaryWeapon is not null
                                    ? me.CreateStrike(me.PrimaryWeapon)
                                    : null,
                                Defense.AC,
                                cr)
                            .Any(bonus =>
                                bonus?.BonusType is BonusType.Circumstance
                                && bonus.BonusSource.ToLower() is {} lower
                                && (lower.Contains("shield") || lower.Contains("cover")));
                }
            }));
    }

    public static void CreateResponses()
    {
        Responses.Add(new ReadyResponse(
            "Strike",
            "Strike the triggering enemy.\n\nThis Strike {Red}uses your multiple attack penalty{/Red}",
            typeof(Creature),
            trigger => trigger.TriggeringObject == typeof(Creature),
            async readyEvent =>
            {
                if (readyEvent.Creatures?.FirstOrDefault() is not { } triggeringCreature
                    || triggeringCreature.FriendOf(readyEvent.Self))
                    return;

                // Include ranged attacks
                QEffect allowRangedAttacks = new QEffect() { Id = QEffectId.MobileShot };
                readyEvent.Self.AddQEffect(allowRangedAttacks);
                
                await OfferReadyReactions(
                    readyEvent.Self,
                    triggeringCreature.ToColoredName(),
                    readyEvent.Trigger.Description,
                    CommonCombatActions.GetPossibleReactiveStrikes(
                        new ReactiveAttackSpecification(
                            readyEvent.Self,
                            triggeringCreature,
                            "Ready!",
                            1,
                            [])
                        {
                            ModifyEachStrike = strike => strike.WithActiveRollSpecification(
                                new ActiveRollSpecification(
                                    Checks.Attack(strike.Item!, readyEvent.Effect.Value),
                                    TaggedChecks.DefenseDC(Defense.AC)))
                        }));
                
                readyEvent.Self.RemoveAllQEffects(qf => qf == allowRangedAttacks);
            }));
        
        Responses.Add(new ReadyResponse(
            "Triggered Strike",
            "Strike the triggering enemy with the triggering attack",
            typeof(Creature),
            trigger => trigger.Name == "Hold", // Hold is the only one that uses this format.
            async readyEvent =>
            {
                if (readyEvent.Creatures?.FirstOrDefault() is not { } triggeringCreature
                    || triggeringCreature.FriendOf(readyEvent.Self)
                    || readyEvent.Tag is not List<Item> triggeringWeapons)
                    return;
                if (triggeringCreature.FriendOf(readyEvent.Self))
                    return;

                // Include ranged attacks
                QEffect allowRangedAttacks = new QEffect() { Id = QEffectId.MobileShot };
                readyEvent.Self.AddQEffect(allowRangedAttacks);
                
                await OfferReadyReactions(
                    readyEvent.Self,
                    triggeringCreature.ToColoredName(),
                    readyEvent.Trigger.Description,
                    CommonCombatActions.GetPossibleReactiveStrikes(
                        new ReactiveAttackSpecification(
                            readyEvent.Self,
                            triggeringCreature,
                            "Ready!",
                            1,
                            [])
                        {
                            IsWeaponPermissible = triggeringWeapons.Contains,
                            ModifyEachStrike = strike => strike.WithActiveRollSpecification(
                                new ActiveRollSpecification(
                                    Checks.Attack(strike.Item!, readyEvent.Effect.Value),
                                    TaggedChecks.DefenseDC(Defense.AC)))
                        })
                        ?.ToList());
                
                readyEvent.Self.RemoveAllQEffects(qf => qf == allowRangedAttacks);
            }));
        
        Responses.Add(new ReadyResponse(
            "Evade",
            "Step or Stride",
            null,
            null,
            async readyEvent =>
            {
                await OfferReadyReactions(
                    readyEvent.Self,
                    readyEvent.Creatures?.FirstOrDefault() is { } triggeringCreature
                        ? triggeringCreature.ToColoredName()
                        : null,
                    readyEvent.Trigger.Description,
                    [
                        ReactionOption.CreateCustom(
                                readyEvent.Response.Name,
                                $"{readyEvent.Response.Description}.",
                                IllustrationName.FleetStep,
                                readyEvent.Self,
                                async () =>
                                {
                                    if (!await readyEvent.Self.StrideAsync(
                                        "Make a Step or Stride.",
                                        allowStep: true,
                                        allowCancel: true,
                                        allowPass: true))
                                        readyEvent.Self.Actions.RefundReaction();
                                })
                            .WithIsReaction()
                    ]);
            }));
        
        Responses.Add(new ReadyResponse(
            "Maneuver",
            "Grapple, Reposition, Shove, or Trip the triggering enemy",
            typeof(Creature),
            trigger => trigger.TriggeringObject == typeof(Creature),
            async readyEvent =>
            {
                if (readyEvent.Creatures?.FirstOrDefault() is not { } triggeringCreature
                    || triggeringCreature.FriendOf(readyEvent.Self))
                    return;

                List<Option> options = [];
                
                List<CombatAction> actions = CombatManeuverPossibilities
                    .GetAllShoveGrappleAndTripOptions(readyEvent.Self)
                    .Append(Reposition.CreateReposition(readyEvent.Self))
                    .Select(action => action.WithActionCost(0))
                    .ToList();
                
                foreach (CombatAction action
                         in actions)
                    GameLoop.AddDirectUsageOnCreatureOptions(action, options, false);

                options.RemoveAll(opt =>
                    opt is CreatureOption crOpt
                    && crOpt.Creature != triggeringCreature);

                if (options.Count == 0)
                    return;
                
                options.Add(new CancelOption(true));

                await OfferReadyReactions(
                    readyEvent.Self,
                    triggeringCreature.ToColoredName(),
                    readyEvent.Trigger.Description,
                    [
                        ReactionOption.CreateCustom(
                                readyEvent.Response.Name,
                                $"{readyEvent.Response.Description}.",
                                IllustrationName.GenericCombatManeuver,
                                readyEvent.Self,
                                async () =>
                                {
                                    Option chosen = (await readyEvent.Self.Battle.SendRequest(new AdvancedRequest(
                                        readyEvent.Self,
                                        "Use a maneuver against the triggering enemy, or right-click to cancel.",
                                        options)
                                    {
                                        TopBarIcon = IllustrationName.GenericCombatManeuver,
                                        TopBarText =
                                            "Use a maneuver against the triggering enemy, or right-click to cancel.",
                                    })).ChosenOption;

                                    if (chosen is CancelOption)
                                        readyEvent.Self.Actions.RefundReaction();
                                    else
                                        await chosen.Action();
                                })
                            .WithIsReaction()
                    ]);
            }));
    }
    
    public static void AddReadyToEveryCreature()
    {
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            if (cr.HasTrait(Trait.Mindless))
                return;
            
            QEffect readyLoader = new QEffect()
            {
                Name = "Ready Loader",
                Key = "ReadyLoader",
                Value = 2,
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    PossibilitySectionId sectionId =
                        PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.AidAndReadyInSubmenus)
                            ? PossibilitySectionId.OtherManeuvers
                            : PossibilitySectionId.SkillActions;
                    if (section.PossibilitySectionId != sectionId)
                        return null;

                    // For each trigger,
                    List<Possibility> triggers = Triggers
                        .Select(Possibility (trigger) =>
                        {
                            CombatAction triggerAct = TriggerDisplay(qfThis.Owner, trigger);
                            triggerAct.WithName($"Ready ({trigger.Name})");
                            //triggerAct.WithFullRename($"Ready ({trigger.Name})");
                            
                            // create responses using that trigger,
                            List<Possibility> responses = Responses
                                // as long as they're compatible.
                                .Where(response =>
                                    trigger.ResponseFilter?.Invoke(response) is not false
                                    && response.TriggerFilter?.Invoke(trigger) is not false)
                                .Select(Possibility (response) =>
                                {
                                    CombatAction responseAct = ResponseAction(
                                        triggerAct,
                                        response);
                                    responseAct.WithName($"Ready ({trigger.Name}) ({response.Name})");
                                    //responseAct.WithFullRename($"Ready ({trigger.Name}) ({response.Name})");
                                    return new ActionPossibility(responseAct)
                                    {
                                        Caption = response.Name
                                    };
                                })
                                .ToList();
                            
                            // Create a submenu of that trigger,
                            return new SubmenuPossibility(
                                ModData.Illustrations.Ready,
                                trigger.Name)
                            {
                                Subsections =
                                [
                                    // with a row of responses,
                                    new PossibilitySection("Responses")
                                    {
                                        PossibilitySectionId = ModData.PossibilitySectionIds.ReadyResponses,
                                        // and fill it with those responses.
                                        Possibilities = responses
                                    }
                                ],
                                SpellIfAny = triggerAct
                            };
                        })
                        .ToList();
                    
                    // Ready menu.
                    SubmenuPossibility readyMenu = new SubmenuPossibility(
                        ModData.Illustrations.Ready,
                        "Ready")
                    {
                        SubmenuId = ModData.SubmenuIds.Ready,
                        Subsections =
                        [
                            // Row of triggers.
                            new PossibilitySection("Triggers")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.ReadyTriggers,
                                Possibilities = triggers,
                            }
                        ],
                        SpellIfAny = ReadyMenuDisplay(qfThis.Owner),
                    };

                    return readyMenu;
                },
            };
            cr.AddQEffect(readyLoader);
        });
    }

    /// <summary>
    /// Returns a Ready action for display in a SubmenuPossibility.
    /// </summary>
    public static CombatAction ReadyMenuDisplay(Creature self)
    {
        return new CombatAction(
                self,
                ModData.Illustrations.Ready,
                "Ready",
                [ModData.ModTrait, Trait.Concentrate],
                """
                {i}You prepare to use an action that will occur outside your turn.{/i}

                Choose a trigger and a response you will take using your {icon:Reaction} reaction.

                If you readied an attack, this attack {Red}applies your multiple attack penalty{/Red} from your turn.
                """,
                Target.Self())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready);
    }

    /// <summary>
    /// Returns a Trigger action for display in the initial triggers for Ready.
    /// </summary>
    public static CombatAction TriggerDisplay(Creature self, ReadyTrigger trigger)
    {
        return new CombatAction(
                self,
                ModData.Illustrations.Ready,
                trigger.Name,
                [ModData.ModTrait, Trait.Concentrate],
                $$"""
                  {i}You prepare to use an action that will occur outside your turn.{/i}

                  {b}Trigger{/b} {{trigger.Description}}.

                  ...
                  """,
                Target.Self())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready)
            .WithTag(trigger);
    }

    /// <summary>
    /// Returns a Response action combined with a Trigger action. This action actually executes 
    /// </summary>
    public static CombatAction ResponseAction(CombatAction triggerAction, ReadyResponse response)
    {
        return new CombatAction(
                triggerAction.Owner,
                ModData.Illustrations.Ready,
                response.Name,
                [ModData.ModTrait, Trait.Concentrate, Trait.DoNotShowOverheadOfActionName],
                $$"""
                  {i}You prepare to use an action that will occur outside your turn.{/i}

                  {b}Trigger{/b} {{(triggerAction.Tag as ReadyTrigger)!.Description}}.

                  {{response.Description}}.
                  """,
                Target.Self())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready)
            .WithTag(new ReadyTag((triggerAction.Tag as ReadyTrigger)!, response))
            .WithEffectOnSelf(async (action, self) =>
            {
                // Doing this so that it's possible to modify the instance of the tag before execution occurs
                ReadyTag tag = (action.Tag as ReadyTag)!;
                
                QEffect setup = new QEffect
                {
                    Name = $"Ready ({tag.Trigger.Name}) ({tag.Response.Name})",
                    Owner = self, // Set this early so it's accessible during construction
                    Description = $"{{b}}Trigger{{/b}} {tag.Trigger.Description}.\n{{b}}Response{{/b}} {tag.Response.Description}.\n",
                    Illustration = action.Illustration,
                    Id = ModData.QEffectIds.Readied,
                    ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
                    // Track MAP
                    Value = self.Actions.AttackedThisManyTimesThisTurn,
                    HideValue = true,
                    EndOfYourTurnBeneficialEffect = async (qfThis, self2) =>
                    {
                        qfThis.Value = self2.Actions.AttackedThisManyTimesThisTurn;
                    },
                };
                tag.Trigger.Effect.Invoke(tag.Trigger, tag.Response, setup);
                self.AddQEffect(setup);
            });
    }

    /// <summary>
    /// A truncated version of <see cref="CommonQuestions.OfferReactionsAsync(ReactionRequestStyle)"/>.
    /// </summary>
    /// <remarks>
    /// This standardizes the display text formatting for the reaction popup. Only collects reactions once and only from the one executing a readied reaction.
    /// </remarks>
    /// <param name="readier">The creature executing their readied reaction.</param>
    /// <param name="whoTriggered">If a creature triggered this reaction, use <see cref="Creature.ToColoredName"/>. Otherwise, use null.</param>
    /// <param name="triggerDescription">Use <see cref="ReadyTrigger.Description"/>.</param>
    /// <param name="reactions">The potentially-null list of reaction options to use.</param>
    public static async Task OfferReadyReactions(Creature readier, string? whoTriggered, string triggerDescription, List<ReactionOption>? reactions)
    {
        await CommonQuestions.OfferReactionsAsync(new ReactionRequestStyle(
            readier.Battle,
            () =>
                $$"""
                  {{(whoTriggered is not null ? $"{whoTriggered} triggered your Readied action" : "Your Readied action was triggered")}}:
                  {b}Trigger{/b} {{triggerDescription}}.
                  """,
            false,
            readier,
            () =>
                reactions is null ? [] : new ReactionOptions(reactions)));
    }

    public class ReadyTag(ReadyTrigger trigger, ReadyResponse response)
    {
        public ReadyTrigger Trigger { get; set; } = trigger;
        public ReadyResponse Response { get; set; } = response;
    }

    public class ReadyTrigger(string name, string description, Type? triggeringObject, Func<ReadyResponse, bool>? responseFilter, Action<ReadyTrigger, ReadyResponse, QEffect> effect)
    {
        /// <summary>
        /// The short, thematic name of the trigger, such as "Seize Opportunity".
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The Trigger entry of an action stat block.
        /// </summary>
        public string Description { get; } = description;

        /// <summary>
        /// The primary triggering object's type.
        /// </summary>
        public Type? TriggeringObject { get; } = triggeringObject;

        /// <summary>
        /// A filter for what kinds of responses are valid for this trigger.
        /// </summary>
        public Func<ReadyResponse, bool>? ResponseFilter { get; } = responseFilter;

        /// <summary>
        /// The constructor for a QEffect that will execute a ReadyResponse. A default effect is passed in, and then modified by this specific trigger.
        /// </summary>
        public Action<ReadyTrigger, ReadyResponse, QEffect> Effect { get; } = effect;

    }

    public class ReadyResponse(string name, string description, Type? triggeringObject, Func<ReadyTrigger, bool>? triggerFilter, Func<ReadyEvent, Task> response)
    {
        /// <summary>
        /// The short, literal description of the response, such as "Strike" or "Stride away".
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The description of the actions to take in response to the trigger.
        /// </summary>
        public string Description { get; } = description;

        /// <summary>
        /// The primary triggering object's type. This type must match with a ReadyTrigger to create a valid combination.
        /// </summary>
        public Type? TriggeringObject { get; } = triggeringObject;

        /// <summary>
        /// A filter for what kinds of responses are valid for this trigger.
        /// </summary>
        public Func<ReadyTrigger, bool>? TriggerFilter { get; } = triggerFilter;

        /// <summary>
        /// The asynchronous event to execute. Includes the connected ReadyTrigger and ReadyResponse. The QEffect is from the trigger, the Creature is yourself, and the object is any triggering data type (usually the triggering creature).
        /// </summary>
        public Func<ReadyEvent, Task> Response { get; } = response;
    }

    public class ReadyEvent(ReadyTrigger trigger, ReadyResponse response, QEffect qfReady)
    {
        public ReadyTrigger Trigger { get; } = trigger;
        
        public ReadyResponse Response { get; } = response;

        /// <summary>
        /// The Ready QEffect that is part of the reaction event.
        /// </summary>
        public QEffect Effect { get; } = qfReady;

        /// <summary>
        /// The Creature taking the Readied reaction.
        /// </summary>
        public Creature Self { get; } = qfReady.Owner;
        
        /// <summary>
        /// For an event involving a creature who triggers a reaction, do not include yourself. Enemies are listed first, then allies.
        /// </summary>
        public List<Creature>? Creatures { get; init; }
        
        /// <summary>
        /// For an event that involves an action triggering a reaction, these are those actions (virtually never more than one).
        /// </summary>
        public List<CombatAction>? Actions { get; init; }
        
        /// <summary>
        /// Arbitrary data to share from a Trigger to a Response.
        /// </summary>
        public object? Tag { get; init; }
    }
}