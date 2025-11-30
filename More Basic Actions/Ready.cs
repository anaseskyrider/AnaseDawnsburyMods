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
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class Ready
{
    public static readonly string ReadyBasicDescription = "{i}You prepare to use an action that will occur outside your turn.{/i}\n\nChoose one of the given options, which include both a trigger and an action you take in response using your {icon:Reaction} reaction.\n\nIf you readied an attack, this attack {Red}applies your multiple attack penalty{/Red} from your turn.";
    
    public static void LoadReady()
    {
        // SilchasRuin — 2:59 AM
        // TODO: maybe make ranged attack when an enemy no longer has cover?
        // not sure how easy thatd be to program
        // TODO: also make ranged attack when enemy enters first range increment
        // maybe combine em
        
        // Add Prepare to Aid to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect readyLoader = new QEffect()
            {
                Name = "Ready Loader",
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    PossibilitySectionId sectionId =
                        PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.AidAndReadyInSubmenus)
                            ? PossibilitySectionId.OtherManeuvers
                            : PossibilitySectionId.SkillActions;
                    if (section.PossibilitySectionId != sectionId)
                        return null;
                    
                    SubmenuPossibility readyMenu = new SubmenuPossibility(
                        ModData.Illustrations.Ready,
                        "Ready")
                    {
                        SubmenuId = ModData.SubmenuIds.Ready,
                        Subsections =
                        {
                            new PossibilitySection("Ready")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.Ready,
                                Possibilities = [
                                    new ActionPossibility(CreateReadyBrace(cr)),
                                    new ActionPossibility(CreateReadySeize(cr)),
                                    new ActionPossibility(CreateReadyFootwork(cr)),
                                ],
                            },
                        },
                        SpellIfAny = new CombatAction(
                            cr,
                            ModData.Illustrations.Ready,
                            "Ready",
                            [ModData.Traits.MoreBasicActions, Trait.Concentrate],
                            ReadyBasicDescription,
                            Target.Self()).WithActionCost(2),
                    };

                    return readyMenu;
                },
            };
            cr.AddQEffect(readyLoader);
        });
    }

    public static CombatAction CreateReadyFootwork(Creature owner)
    {
        CombatAction footworkAction = new CombatAction(
                owner,
                IllustrationName.TwoActions,
                "Ready (Footwork)",
                [ModData.Traits.MoreBasicActions, Trait.DoNotShowInContextMenu, Trait.Concentrate, Trait.Basic],
                "You prepare to take the following {icon:Reaction} reaction:\n\n{b}Trigger{/b} An enemy ends a move action adjacent to you\n\nMake a Step or Stride.\n\nStep actions and other similar actions do not trigger this reaction.",
                Target.Self())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                QEffect readiedFootwork = new QEffect(
                    "Evading Footwork",
                    "When an enemy ends a move action adjacent to you, you can make a Step or Stride as a reaction.",
                    ExpirationCondition.ExpiresAtStartOfYourTurn,
                    caster,
                    ModData.Illustrations.Ready)
                {
                    DoNotShowUpOverhead = true,
                    Value = caster.Actions.AttackedThisManyTimesThisTurn,
                    EndOfYourTurnBeneficialEffect = async (qfThis, self) =>
                    {
                        qfThis.Value = self.Actions.AttackedThisManyTimesThisTurn;
                    },
                }
                .AddGrantingOfTechnical(cr => !cr.FriendOf(caster),
                    qfTech =>
                    {
                        qfTech.AfterYouTakeAction = async (qfThis, provokingAction) =>
                        {
                            if (!provokingAction.HasTrait(Trait.Move) || provokingAction.HasTrait(Trait.DoesNotProvoke) || provokingAction.ActionId == ActionId.Step || provokingAction.TilesMoved == 0 || !qfThis.Owner.IsAdjacentTo(caster))
                                return;

                            if (await caster.AskToUseReaction($"{{b}}Ready (Footwork) {{icon:Reaction}}{{/b}}\n{{Blue}}{qfThis.Owner.Name}{{/Blue}} has ended their {{Blue}}{provokingAction.Name}{{/Blue}} action adjacent to you.\nStep or Stride?"))
                            {
                                if (!await caster.StrideAsync("Make a Step or Stride.", allowStep: true, allowCancel: true, allowPass: true))
                                    caster.Actions.RefundReaction();
                            }
                        };
                    });
                caster.AddQEffect(readiedFootwork);
            });

        return footworkAction;
    }

    // BUG: Look at bizarre flanking triggers, reported by Erful
    public static CombatAction CreateReadySeize(Creature owner)
    {
        CombatAction seizeAction = new CombatAction(
                owner,
                IllustrationName.TwoActions,
                "Ready (Seize Opportunity)",
                [ModData.Traits.MoreBasicActions, Trait.DoNotShowInContextMenu, Trait.Concentrate, Trait.Basic],
                "You prepare to take the following {icon:Reaction} reaction:\n\n{b}Trigger{/b} An enemy within your range or your reach becomes flat-footed to you\n\nYou make a Strike against the triggering creature. This Strike {Red}uses your multiple attack penalty.{/Red}",
                Target.Self())
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                QEffect readiedSeize = new QEffect(
                    "Seeking Opportunity",
                    "When an enemy becomes flat-footed to you, you can Strike the triggering creature as a reaction.",
                    ExpirationCondition.ExpiresAtStartOfYourTurn,
                    caster,
                    ModData.Illustrations.Ready)
                {
                    DoNotShowUpOverhead = true,
                    Value = caster.Actions.AttackedThisManyTimesThisTurn,
                    EndOfYourTurnBeneficialEffect = async (qfThis, self) =>
                    {
                        qfThis.Value = self.Actions.AttackedThisManyTimesThisTurn;
                    },
                    StateCheckLayer = 1,
                    StateCheckWithVisibleChanges = async qfThis =>
                    {
                        Creature self = qfThis.Owner;
                        
                        if (self.PrimaryWeaponIncludingRanged == null)
                            return;
                        
                        List<Creature> provokeQueue = (qfThis.Tag as List<Creature>)!;

                        foreach (Creature cr in self.Battle.AllCreatures
                                     .Where(cr => !cr.FriendOf(self)))
                        {
                             if (cr.IsFlatfootedToBecause(self, null) == null
                                && !cr.QEffects.Any(qf =>
                                    qf.Id == QEffectId.FlankedBy && qf.Source == self))
                            {
                                provokeQueue.Remove(cr);
                                continue;
                            }

                            if (provokeQueue.Contains(cr))
                                continue;
                            
                            await OfferAndMakeReactiveStrike2(
                                self,
                                cr,
                                $"{{b}}Ready (Seize Opportunity) {{icon:Reaction}}{{/b}}\n{{Blue}}{cr.Name}{{/Blue}} has become flat-footed to you.\nMake a Strike?",
                                "*ready (seize opportunity)*",
                                1,
                                qfThis.Value,
                                false);
                            
                            provokeQueue.Add(cr);
                        }
                    },
                    Tag = caster.Battle.AllCreatures
                        .Where(cr =>
                            !cr.FriendOf(caster)
                            && cr.IsFlatfootedToBecause(caster, null) == null
                            && !cr.QEffects.Any(qf =>
                                qf.Id == QEffectId.FlankedBy && qf.Source == caster))
                        .ToList(), // Creatures who've been made off-guard since last reaction-prompt
                };
                caster.AddQEffect(readiedSeize);
            });

        return seizeAction;
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
                [ModData.Traits.MoreBasicActions, Trait.DoNotShowInContextMenu, Trait.Concentrate, Trait.Basic],
                "You prepare to take the following {icon:Reaction} reaction:\n\n{b}Trigger{/b} An enemy moves into your reach\n\nYou make a melee Strike against the triggering creature. This Strike {Red}uses your multiple attack penalty.{/Red}",
                Target.Self(/*(self, ai) => // Hopelessly nonfunctional experiment in getting AI to use Brace sometimes.
                {
                    // Mindless creatures do not use Ready actions.
                    if (ai.Tactic is Tactic.Mindless or Tactic.DoNothing or Tactic.PanickingChild)
                        return int.MinValue;
                    
                    // Do not take as first action, encourage normal option evaluations before falling back to Ready
                    int actionsLeft = self.Actions.TotalActionsLeft;
                    if (actionsLeft > 2)
                        return int.MinValue;
                    
                    // Won't take this action if it can't react since it requires a reaction to utilize
                    if (!self.Actions.CanTakeReaction())
                        return int.MinValue;
                    
                    // Will not be taken if this creature has both AoOs and Reach,
                    // because Brace becomes wasteful compared to other options.
                    if (self.HasEffect(QEffectId.AttackOfOpportunity) is {} opp && self.Space.ActualReach > 1)
                        return int.MinValue;
                    
                    // Will not be taken by creatures who have spells or ranged Strike options.
                    bool hasRangedOptions = self.Spellcasting is not null
                        || self.Weapons.Any(item => item.WeaponProperties is
                            { RangeIncrement: > 0, MaximumRange: > 0 });
                    if (hasRangedOptions)
                        return int.MinValue;
                    
                    // Will only be taken if there are enemies within a distance that could feasibly trigger Ready.
                    List<Creature> enemies = self.Battle.AllCreatures
                        .Where(self.EnemyOf)
                        .OrderBy(self.DistanceTo) // First creature is the closest
                        .ToList();
                    if (enemies.Count == 0)
                        return int.MinValue;
                    // // Don't Brace if I can Stride 1+ times and then Strike.
                    int distanceICanStrideAndStrike = (self.Speed * (actionsLeft - 1)) + self.Space.ActualReach;
                    if (enemies.Any(cr =>
                            self.DistanceToWith10FeetException(cr) <= distanceICanStrideAndStrike))
                        return int.MinValue;
                    // // Don't Brace if nobody can Stride up and enter my reach.
                    if (enemies.All(cr =>
                            cr.DistanceTo(self) > (cr.Speed * 3) + self.Space.ActualReach))
                        return int.MinValue;
                    
                    // The value of this action is based on the goodness of their default melee attack.
                    if (self.PrimaryWeapon == null)
                        return int.MinValue;

                    float mainStrike = self.CreateStrike(
                            self.PrimaryWeapon,
                            self.Actions.AttackedThisManyTimesThisTurn)
                        .TrueDamageFormula!
                        .ExpectedValueMinimumOne;

                    int distanceOverMinimum = enemies.First().DistanceToWith10FeetException(self) - distanceICanStrideAndStrike;
                    float distance_Modifier = -0.1f * distanceOverMinimum; // Reduced value if the creature isn't advancing
                    float AoO_Mult = opp ? 0.5f : 1f; // Reduced value if the creature has AoOs.

                    return mainStrike * AoO_Mult + distance_Modifier;
                }*/))
            .WithActionCost(2)
            .WithActionId(ModData.ActionIds.Ready)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                // AoO tile icons are inaccurate. Nothing I can do to fix it.
                QEffect readiedBrace = new QEffect(
                    "Bracing",
                    "When an enemy moves within your reach, you can make a melee Strike against the triggering creature as a reaction.",
                    ExpirationCondition.ExpiresAtStartOfYourTurn,
                    caster,
                    ModData.Illustrations.Ready)
                {
                    DoNotShowUpOverhead = true,
                    Value = caster.Actions.AttackedThisManyTimesThisTurn,
                    YouDealDamageWithStrike = (qfThis, action, formula, defender) =>
                    {
                        if (!defender.IsImmuneTo(Trait.PrecisionDamage)
                            && (action.Item?.HasTrait(ModData.Traits.Brace) ?? false)
                            && (action.HasTrait(ModData.Traits.ReactiveAttackWithMAP)
                            || action.HasTrait(Trait.AttackOfOpportunity)
                            || action.HasTrait(Trait.ReactiveAttack)))
                        {
                            int braceBonus = (action.Item.WeaponProperties?.DamageDieCount ?? 0) * 2;
                            if (braceBonus > 0)
                                return formula.Add(DiceFormula.FromText(
                                    braceBonus.ToString(),
                                    "Brace (precision)"));
                        }
                        return formula;
                    },
                    EndOfYourTurnBeneficialEffect = async (qfThis, self) =>
                    {
                        qfThis.Value = self.Actions.AttackedThisManyTimesThisTurn;
                    },
                    StateCheckWithVisibleChanges = async qfThis =>
                    {
                        // Each state check, look for creatures currently in my reach. If that creature has a movement history in which the previous tile was outside my reach, then it provokes a custom-built reaction attack.

                        Creature self = qfThis.Owner;
                        
                        if (self.PrimaryWeapon == null)
                            return;
                        
                        int reach = self.PrimaryWeapon.HasTrait(Trait.Reach) ? 2 : 1;
                        Dictionary<Creature,int> provokeQueue = (qfThis.Tag as Dictionary<Creature,int>)!;
                        
                        // For each enemy currently in my reach,
                        foreach (Creature cr in self.Battle.AllCreatures.Where(cr => !cr.FriendOf(self)))
                        {
                            if (cr.DistanceToWith10FeetException(self) > reach)
                            {
                                provokeQueue.Remove(cr);
                                continue;
                            }
                            
                            // who is currently moving,
                            LongMovement? move = cr.AnimationData.LongMovement;
                            if (move?.Path is null || move.Path.Count < 1 || move.CombatAction?.TilesMoved == 0)
                                continue;
                            
                            // and whose last movement was outside my reach,
                            int currentTileIndex = move.Path.IndexOf(cr.Occupies);
                            Tile previousTile = move.Path.Count > 1 && currentTileIndex > 0
                                ? move.Path[currentTileIndex-1]
                                : move.OriginalTile;
                            if (self.DistanceToWith10FeetException(previousTile) <= reach)
                                continue;
                            
                            // and didn't just prompt on the same movement,
                            if (provokeQueue.TryGetValue(cr, out int pathLength) && pathLength == move.Path.Count)
                                continue;
                            
                            // prompt a strike against it,
                            if (await ProvokeBraceReaction(qfThis.Owner, cr, move.CombatAction, qfThis.Value))
                                // and add it to the queue if it was actually prompted
                                provokeQueue[cr] = move.Path.Count;
                        }
                    },
                    Tag = new Dictionary<Creature,int>(), // used to prevent some double-prompts
                };
                caster.AddQEffect(readiedBrace);
            });
        
        return braceAction;
    }

    public static async Task<bool> ProvokeBraceReaction(
        Creature reactor,
        Creature provoker,
        CombatAction? provokingAction,
        int attacksMade = 0)
    {
        if (reactor.PrimaryWeapon == null
            || provokingAction == null
            || !provokingAction.HasTrait(Trait.Move)
            || provokingAction.TilesMoved == 0
            || provokingAction.ActionId == ActionId.Step
            || provokingAction.HasTrait(Trait.DoesNotProvoke))
            return false;
        
        await OfferAndMakeReactiveStrike2(
            reactor,
            provoker,
            $"{{b}}Ready (Brace) {{icon:Reaction}}{{/b}}\n{{Blue}}{provoker.Name}{{/Blue}} enters your reach using {{Blue}}{provokingAction.Name}{{/Blue}}.\nMake a melee Strike?",
            "*ready (brace)*",
            1,
            attacksMade);

        return true;
    }
    
    public static async Task<CheckResult?> OfferAndMakeReactiveStrike2(
      Creature attacker,
      Creature target,
      string question,
      string overhead,
      int numberOfStrikes,
      int attacksMade,
      bool meleeOnly = true)
    {
        IEnumerable<Item> listToUse = meleeOnly ? attacker.MeleeWeapons : attacker.Weapons;
        Item? primaryWeapon = meleeOnly ? attacker.PrimaryWeapon : attacker.PrimaryWeaponIncludingRanged;
        List<CombatAction> possibleStrikes = listToUse
            .Select(CreateReactiveAttackFromWeapon)
            .Where(IsStrikeOk)
            .ToList();
        CombatAction? combatAction = primaryWeapon != null
            ? CreateReactiveAttackFromWeapon(primaryWeapon)
            : null;

        if (combatAction != null && !IsStrikeOk(combatAction))
          combatAction = null;

        if (possibleStrikes.Count == 0)
            return null;

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
                return "With "
                       + (obj1 != null
                           ? !Items.TryGetItemTemplate(obj1.ItemName, out Item? obj2)
                               ? obj1.Name != "fist"
                                   ? obj1.Illustration.IllustrationAsIconString + " " + obj1.Name
                                   : "{icon:Kick} kick"
                               : obj2.Name != "fist"
                                   ? obj1.Illustration.IllustrationAsIconString + " " + obj2.Name
                                   : "{icon:Kick} kick" : "??");
            }).ToArray());
            flag = useReaction.HasValue;
            selectedStrike = useReaction.HasValue ? possibleStrikes[useReaction.Value] : null;
        }
        
        if (!flag || selectedStrike == null)
            return null;
        
        // Do not capture MAP
        //int map = attacker.Actions.AttackedThisManyTimesThisTurn;
        
        attacker.Overhead(overhead, Color.White);
        
        CheckResult? bestCheckResult = null;
        for (int i = 0; i < numberOfStrikes; ++i)
        {
            CheckResult checkResult = await attacker.MakeStrike(selectedStrike, target);
            if (!bestCheckResult.HasValue)
            {
                bestCheckResult = checkResult;
            }
            else
            {
                int num = (int) checkResult;
                CheckResult? nullable = bestCheckResult;
                int valueOrDefault = (int) nullable.GetValueOrDefault();
                if (num > valueOrDefault & nullable.HasValue)
                    bestCheckResult = checkResult;
            }
        }
      
        // Do not restore MAP
        //attacker.Actions.AttackedThisManyTimesThisTurn = map;
      
        return bestCheckResult;

        CombatAction CreateReactiveAttackFromWeapon(Item weapon)
        {
            // Do not set any MAP value.
            CombatAction attackFromWeapon = attacker.CreateStrike(weapon, attacksMade/*, 0*/)
                .WithActionCost(0);
            //attackFromWeapon.Traits.Add(Trait.AttackOfOpportunity);
            //attackFromWeapon.Traits.Add(Trait.ReactiveAttack);
            attackFromWeapon.Traits.Add(ModData.Traits.ReactiveAttackWithMAP);
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