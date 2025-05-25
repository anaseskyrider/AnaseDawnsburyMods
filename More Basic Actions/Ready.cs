using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class Ready
{
    public static readonly string ReadyBasicDescription = "{i}You prepare to use an action that will occur outside your turn.{/i}\n\nChoose one of the given options, which include both a trigger and an action you take in response using your {icon:Reaction} reaction.\n\nIf you readied an attack, this attack {Red}applies your multiple attack penalty{/Red} from your turn.";
    public static readonly string ReadyBraceDescription = "";
    
    public static void LoadReady()
    {
        // TODO: "Readying an attack is useful under two circumstances. First, an enemy comes within reach/range. This would be easy to implement. Second, an enemy is made flat-footed or flanked (most relevant for rogues)." - Dinglebob
        
        // Add Prepare to Aid to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect readyLoader = new QEffect()
            {
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.SkillActions)
                        return null;
                    
                    SubmenuPossibility readyMenu = new SubmenuPossibility(
                        ModData.Illustrations.Ready,
                        "Ready",
                        PossibilitySize.Full)
                    {
                        SubmenuId = ModData.SubmenuIds.Ready,
                        Subsections =
                        {
                            new PossibilitySection("Ready")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.Ready,
                                Possibilities = [new ActionPossibility(CreateReadyBrace(cr), PossibilitySize.Full)],
                            },
                        },
                        SpellIfAny = new CombatAction(
                            cr,
                            ModData.Illustrations.Ready,
                            "Ready",
                            [Trait.Concentrate],
                            ReadyBasicDescription,
                            Target.Self()).WithActionCost(2),
                    };

                    return readyMenu;
                },
            };
            cr.AddQEffect(readyLoader);
        });
    }

    public static CombatAction CreateReadyBrace(Creature owner)
    {
        // In order for creatures to provoke reactions only when they enter your reach, two things need to occur.
        // 1. The creature completing a non-AoO-triggering movement action in your reach provokes a reaction.
        // 2. Creatures with complex movement involving moving in and out of your reach provoke a reaction before they would leave it.
        
        CombatAction braceAction = new CombatAction(
            owner,
            IllustrationName.TwoActions,
            "Ready (Brace)",
            [Trait.Concentrate, Trait.Basic],
            "You prepare to take the following {icon:Reaction} reaction:\n\n{b}Trigger{/b} A creature moves into your reach\n\nYou make a melee Strike against the triggering creature. This Strike {Red}uses your multiple attack penalty.{/Red}",
            Target.Self())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                // AoO tile icons are inaccurate. Nothing I can do to fix it.
                QEffect readiedBrace = new QEffect(
                    "Bracing",
                    "When an enemy moves within your reach, you can make a melee Strike against the triggering creature.",
                    ExpirationCondition.ExpiresAtStartOfYourTurn,
                    caster,
                    ModData.Illustrations.Ready)
                {
                    Value = caster.Actions.AttackedThisManyTimesThisTurn,
                    Id = QEffectId.AttackOfOpportunity, // Tells the game to attempt to asynchronously provoke mid-movement
                    Tag = new List<Creature>(), // Creatures queued to trigger reaction from movement
                    DoNotShowUpOverhead = true,
                    EndOfYourTurnBeneficialEffect = async (qfThis, self) =>
                    {
                        qfThis.Value = self.Actions.AttackedThisManyTimesThisTurn;
                    },
                    StateCheck = async qfThis =>
                    {
                        // Each state check, look for creatures currently in my reach. If that creature has a movement history in which the previous tile was outside my reach, then add it to a queue, using the AttackOfOpportunity built-in behavior to provoke reactions asynchronously.
                        // In addition, give it a QEffect that tells it to provoke a reaction from me when it completes that action inside my reach.

                        Creature self = qfThis.Owner;
                        
                        if (self.PrimaryWeapon == null)
                            return;
                        
                        int reach = self.PrimaryWeapon.HasTrait(Trait.Reach) ? 2 : 1;
                        List<Creature> provokeQueue = (qfThis.Tag as List<Creature>)!;
                        
                        // For each creature currently in my reach,
                        foreach (Creature cr in self.Battle.AllCreatures.Where(cr =>
                                     cr.DistanceTo(self) <= reach && cr != self))
                        {
                            // who is currently moving,
                            LongMovement? move = cr.AnimationData.LongMovement;
                            if (move is null || move.Path is null || move.Path.Count < 1)
                                continue;
                            
                            // and whose last movement was outside my reach,
                            int currentTileIndex = move.Path.IndexOf(cr.Occupies);
                            Tile previousTile = move.Path.Count > 1 && currentTileIndex > 0
                                ? move.Path[currentTileIndex-1]
                                : move.OriginalTile;
                            if (previousTile.DistanceTo(self.Occupies) <= reach)
                                continue;
                            
                            // add it to the provokeQueue in case it continues to exit that tile,
                            provokeQueue.Add(cr);
                            
                            // and give it a QEffect for when it stops in that tile.
                            cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                AfterYouTakeAction = async (qfThis2, action) =>
                                {
                                    if (action == move.CombatAction)
                                    {
                                        await ProvokeBraceReaction(qfThis.Owner, qfThis2.Owner, action, provokeQueue, qfThis.Value);
                                    }
                                }
                            });
                        }
                    },
                    WhenProvoked = async (qfThis, provokingAction) =>
                    {
                        List<Creature> provokeQueue = (qfThis.Tag as List<Creature>)!;
                        await ProvokeBraceReaction(qfThis.Owner, provokingAction.Owner, provokingAction, provokeQueue, qfThis.Value);
                    },
                };
                caster.AddQEffect(readiedBrace);
            });
        
        return braceAction;
    }

    public static async Task ProvokeBraceReaction(
        Creature reactor,
        Creature provoker,
        CombatAction? provokingAction,
        List<Creature> provokeQueue,
        int attacksMade = 0)
    {
        if (reactor.PrimaryWeapon == null
            || provokingAction == null
            || !provokingAction.HasTrait(Trait.Move)
            || provokingAction.TilesMoved == 0
            || provokingAction.ActionId == ActionId.Step
            || provokingAction.HasTrait(Trait.DoesNotProvoke))
            return;
                        
        if (!provokeQueue.Contains(provoker))
            return;
                        
        await OfferAndMakeReactiveStrike2(
            reactor,
            provoker,
            $"{{b}}Ready (Brace) {{icon:Reaction}}\n{{Blue}}{provoker.Name}{{/Blue}} enters your reach using {{Blue}}{provokingAction.Name}{{/Blue}}.\nMake a melee Strike?",
            "ready (brace)",
            1,
            attacksMade);
        provokeQueue.Remove(provokingAction.Owner);
    }
    
    public static async Task<CheckResult?> OfferAndMakeReactiveStrike2(
      Creature attacker,
      Creature target,
      string question,
      string overhead,
      int numberOfStrikes,
      int attacksMade)
    {
        List<CombatAction> possibleStrikes = attacker.MeleeWeapons
            .Select(CreateReactiveAttackFromWeapon)
            .Where(IsStrikeOk)
            .ToList();
        CombatAction? combatAction = attacker.PrimaryWeapon != null
            ? CreateReactiveAttackFromWeapon(attacker.PrimaryWeapon)
            : null;

        if (combatAction != null && !IsStrikeOk(combatAction))
          combatAction = null;

        if (possibleStrikes.Count == 0)
            return new CheckResult?();

        CombatAction? selectedStrike;
        bool flag;
        if (ShouldUseStrikeAsPrimary2(combatAction, attacker, target) && !PlayerProfile.Instance.AlwaysAllowReactiveStrikeOption)
        {
            selectedStrike = combatAction;
            flag = await attacker.Battle.AskToUseReaction(attacker, question);
        }
        else if (possibleStrikes.Count == 1 && !PlayerProfile.Instance.AlwaysAllowReactiveStrikeOption)
        {
            selectedStrike = possibleStrikes[0];
            flag = await attacker.Battle.AskToUseReaction(attacker, question);
        }
        else
        {
            int? useReaction = await attacker.Battle.AskToUseReaction(attacker, question, IllustrationName.Reaction, possibleStrikes.Select(strike =>
            {
                Item? obj1 = strike.Item;
                Item obj2;
                return "With " + (obj1 != null ? (!Items.TryGetItemTemplate(obj1.ItemName, out obj2) ? (!(obj1.Name == "fist") ? obj1.Illustration.IllustrationAsIconString + " " + obj1.Name : "{icon:Kick} kick") : (!(obj2.Name == "fist") ? obj1.Illustration.IllustrationAsIconString + " " + obj2.Name : "{icon:Kick} kick")) : "??");
            }).ToArray());
            flag = useReaction.HasValue;
            selectedStrike = useReaction.HasValue ? possibleStrikes[useReaction.Value] : null;
        }
      
        if (!flag || selectedStrike == null)
            return new CheckResult?();
      
        // Do not capture MAP
        //int map = attacker.Actions.AttackedThisManyTimesThisTurn;
      
        attacker.Occupies.Overhead(overhead, Color.White);
      
        CheckResult? bestCheckResult = new CheckResult?();
        for (int i = 0; i < numberOfStrikes; ++i)
        {
            CheckResult checkResult = await attacker.MakeStrike(selectedStrike, target);
            if (!bestCheckResult.HasValue)
            {
                bestCheckResult = new CheckResult?(checkResult);
            }
            else
            {
                int num = (int) checkResult;
                CheckResult? nullable = bestCheckResult;
                int valueOrDefault = (int) nullable.GetValueOrDefault();
                if (num > valueOrDefault & nullable.HasValue)
                    bestCheckResult = new CheckResult?(checkResult);
            }
        }
      
        // Do not restore MAP
        //attacker.Actions.AttackedThisManyTimesThisTurn = map;
      
        return bestCheckResult;

        CombatAction CreateReactiveAttackFromWeapon(Item weapon)
        {
            // Do not set any MAP value.
            CombatAction attackFromWeapon = attacker.CreateStrike(weapon, attacksMade)
                .WithActionCost(0);
            //attackFromWeapon.Traits.Add(Trait.AttackOfOpportunity);
            //attackFromWeapon.Traits.Add(Trait.ReactiveAttack);
            return attackFromWeapon;
        }

        bool IsStrikeOk(CombatAction strike)
        {
            return (bool) strike.CanBeginToUse(attacker)
                   && strike.Target is CreatureTarget target1
                   && (bool) target1.IsLegalTarget(attacker, target);
        }
    }
    
    private static bool ShouldUseStrikeAsPrimary2(
        CombatAction? primaryStrike,
        Creature attacker,
        Creature target)
    {
        if (primaryStrike == null)
            return false;
        Item? obj = primaryStrike.Item;
        if (obj == null)
            return true;
        WeaponProperties? weaponProperties = obj.WeaponProperties;
        return weaponProperties == null
               || !target.WeaknessAndResistance.Immunities.Contains(weaponProperties.DamageKind)
               && !target.WeaknessAndResistance.Resistances.Any(wp => wp.DamageKind == weaponProperties.DamageKind)
               && !obj.HasTrait(Trait.Shield)
               && !attacker.HasTrait(Trait.Monk);
    }
}