using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class Wrestler
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Lv4: Elbow Breaker
        yield return new TrueFeat(
                ModData.FeatNames.ElbowBreaker, 4,
                "You bend your opponent's body or limbs into agonizing positions that make it difficult for them to maintain their grip.",
                $$"""
                  {b}Requirements{/b} You have a creature grabbed or restrained.

                  Make an unarmed melee Strike against the creature you have grabbed or restrained. This Strike has the following additional effects.{{S.FourDegreesOfSuccess(
                      "You knock one held item out of the creature's grasp. It falls to the ground in the creature's space.",
                      "You weaken your target's grasp on the item. Further attempts to Disarm them of that item gain a +2 circumstance bonus, and they take a -2 circumstance penalty to attacks with the item. The effect is ended when the target Interacts to change their grip on the item.",
                      null,
                      null)}}
                  """,
                [])
            .WithAvailableAsArchetypeFeat(Trait.Wrestler)
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Make an unarmed melee Strike against a creature you have grabbed or restrained. A hit weakens their grasp on an item, and a crit knocks it out entirely.";

                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Unarmed))
                        return null;

                    StrikeModifiers mods = new StrikeModifiers()
                    {
                        OnEachTarget = async (caster, target, result) =>
                        {
                            if (result < CheckResult.Success)
                                return;

                            List<Item> disarmables = target.HeldItems
                                .Where(i => !i.HasTrait(Trait.Grapplee))
                                .ToList();

                            if (disarmables.Count == 0)
                                return;

                            Item? chosen;

                            if (disarmables.Count > 1)
                            {
                                chosen = disarmables[(await caster.AskForChoiceAmongButtons(
                                        IllustrationName.GenericCombatManeuver,
                                        $$"""
                                        {b}Elbow Breaker{/b} {icon:Action}
                                        Choose an item to {{(result > CheckResult.Success ? "{Green}disarm{/Green}" : "{Blue}weaken the grasp of{/Blue}")}}.
                                        """,
                                        disarmables
                                        .Select(i =>
                                            $"{i.Illustration.IllustrationAsIconString} {i.Name}")
                                        .ToArray()))
                                    .Index];
                            }
                            else
                              chosen = disarmables[0];

                            if (result == CheckResult.CriticalSuccess)
                            {
                                target.DropItem(chosen);
                                /*target.HeldItems.Remove(chosen);
                                target.Occupies.DropItem(chosen);*/
                                Sfxs.Play(SfxName.DropItem);
                            }
                            else
                                target.AddQEffect(new QEffect(
                                    "Weakened grasp",
                                    $"Attempts to Disarm you gain a +2 circumstance bonus, and your attacks with your {chosen.Illustration.IllustrationAsIconString} {chosen.Name.WithTag("Blue")} take a -2 circumstance penalty. You can remove this effect as an Interact action.",
                                    ExpirationCondition.Never,
                                    caster,
                                    IllustrationName.GenericCombatManeuver)
                                {
                                    Key = "Weakened grasp:" + disarmables.IndexOf(chosen),
                                    BonusToAttackRolls = (_, action, _) =>
                                        action.Item == chosen
                                            ? new Bonus(-2, BonusType.Circumstance, "Weakened grasp (Elbow Breaker)")
                                            : null,
                                    StateCheck = qfWeak =>
                                    {
                                        // Apply bonus to disarm against me
                                        qfWeak.Owner.Battle.AllCreatures.ForEach(cr =>
                                            cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                            {
                                                BonusToAttackRolls = (_, action, defender) =>
                                                    defender == target
                                                    && action.ActionId == ActionId.Disarm
                                                        ? new Bonus(2, BonusType.Circumstance,
                                                          "Weakened grasp (Elbow Breaker)")
                                                        : null
                                            }));
                                       
                                        // End early when the item is dropped
                                        if (!qfWeak.Owner.HeldItems.Contains(chosen))
                                            qfWeak.ExpiresAt = ExpirationCondition.Immediately;
                                    },
                                    ProvideContextualAction = qfThis =>
                                    {
                                        return new ActionPossibility(
                                                new CombatAction(
                                                        qfThis.Owner,
                                                        chosen.Illustration,
                                                        "Reassert Grip",
                                                        [Trait.Basic, Trait.Manipulate],
                                                        $"Remove the -2 circumstance penalty to your attack rolls with {chosen.Illustration.IllustrationAsIconString} {chosen.Name.WithTag("Blue")}.",
                                                        Target.Self())
                                                    .WithSoundEffect(SfxName.ItemAction)
                                                    .WithItem(chosen)
                                                    .WithEffectOnEachTarget(async (_, _, _, _) =>
                                                        qfThis.ExpiresAt = ExpirationCondition.Immediately))
                                            .WithPossibilityGroup("Remove debuff");
                                    },
                                    CountsAsADebuff = true
                                });
                        }
                    };

                    CombatAction strike = qfFeat.Owner.CreateStrike(item, -1, mods)
                        .WithIllustration(new SideBySideIllustration(
                            item.Illustration,
                            IllustrationName.Disarm))
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription2(
                            mods,
                            additionalSuccessText: "You knock one held item out of the creature's grasp. It falls to the ground in the creature's space.",
                            additionalCriticalSuccessText: "You weaken your target's grasp on the item. Further attempts to Disarm them of that item gain a +2 circumstance bonus, and they take a -2 circumstance penalty to attacks with the item. The effect is ended when the target Interacts to change their grip on the item."))
                        .WithActionId(ModData.ActionIds.ElbowBreaker);
                    strike.WithFullRename("Elbow Breaker");
                    strike.Traits = new Traits([ModData.ModTrait, ..strike.Traits.ToList()], strike);

                    ((CreatureTarget)strike.Target)
                        .WithAdditionalConditionOnTargetCreature((a, d) =>
                            d.QEffects.Any(qfFind =>
                                qfFind.Id == QEffectId.Grappled
                                && qfFind.Source == a)
                            ? Usability.Usable
                            : Usability.NotUsableOnThisCreature("Target is not grappled by you"))
                      .WithAdditionalConditionOnTargetCreature((_, d) =>
                            d.HeldItems.Any(i => !i.HasTrait(Trait.Grapplee))
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature("No held items"));
                    return strike;
                };
            });
        
        // Lv8: Running Tackle
        bool longJumpExists = ModManager.TryParse("LongJump", out ActionId longJump);
        bool quickJumpExists = ModManager.TryParse("MoreBasicActions.QuickJump", out FeatName quickJump);
        yield return new TrueFeat(
                ModData.FeatNames.RunningTackle, 8,
                "You charge, throwing your body at your foe in a vicious tackle.",
                longJumpExists
                    ? "Stride twice or make a Long Jump, then attempt to Grapple or Shove." /* If you made a Long Jump, you can make the Grapple or Shove at any point in the jump, but that ends your movement when you do.*/
                    : $$"""
                        Stride twice, then attempt to Grapple or Shove.

                        {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Modding{/b} If you have the {i}{link:https://steamcommunity.com/sharedfiles/filedetails/?id=3485625903}More Basic Actions{/}{/i} mod installed, you can also make a Long Jump instead of Striding twice.
                        """, /* You can Grapple or Shove at any point in the jump, ending your movement when you do.*/
                [])
            .WithAvailableAsArchetypeFeat(Trait.Wrestler)
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction tackleStride = CreateTackle(ActionId.Stride);

                    Possibility poss = longJumpExists
                        ? new SubmenuPossibility(
                            new SideBySideIllustration(
                                IllustrationName.FleetStep,
                                IllustrationName.GenericCombatManeuver),
                            "Running Tackle")
                        {
                            Subsections =
                            [
                                new PossibilitySection("Running Tackle")
                                {
                                    Possibilities =
                                    [
                                        new ActionPossibility(tackleStride),
                                        new ActionPossibility(CreateTackle(longJump))
                                    ]
                                }
                            ]
                        }
                        : new ActionPossibility(tackleStride);

                    return poss.WithPossibilityGroup("Wrestler");
                    
                    CombatAction CreateTackle(ActionId actionId)
                    {
                        bool isLongJump = longJumpExists && actionId == longJump;
                        return new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(
                                    isLongJump
                                        ? new ModdedIllustration("MoreBasicActionsAssets/" + "jumping.png")
                                        : IllustrationName.FleetStep,
                                    IllustrationName.GenericCombatManeuver),
                                $"Running Tackle{actionId switch {
                                    _ when isLongJump => " (Long Jump)",
                                    ActionId.Stride => " (Stride)",
                                    _ => ""
                                }}",
                                [ModData.ModTrait, Trait.Archetype],
                                $"{actionId switch {
                                    _ when isLongJump => "Make a Long Jump",
                                    ActionId.Stride => "Stride twice",
                                    _ => "???"
                                }}, then attempt to Grapple or Shove{(/*isLongJump ? " at any point during the jump. Doing so ends your movement early." :*/ ".")}",
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.WouldBeAbleToStride()
                                            ? null
                                            : "Can't move"))
                            .WithActionCost(2)
                            .WithEffectOnSelf(async (action, self) =>
                            {
                                int actionsSpentSoFar = 0;
                                if (isLongJump)
                                {
                                    // Reintroduce Stride if Quick Jump is in effect.
                                    if (quickJumpExists
                                        && self.HasFeat(quickJump))
                                    {
                                        await self.StrideAsync(
                                            "Choose where to Stride as part of Long Jump with Running Tackle. (1/3)",
                                            allowCancel: true);
                                    }
                                    
                                    // Code doesn't work. SingleTileMove, which is used for leaping,
                                    // doesn't trigger events.
                                    /*QEffect maneuverWhenJumping = new QEffect()
                                    {
                                        Name = "[RUNNING TACKLE: DO MANEUVER DURING JUMP]",
                                        Tag = null,
                                        // BUG: Does not trigger on a Leap due to SingleTileMove
                                        StateCheckWithVisibleChanges = async qfJumping =>
                                        {
                                            // If longmoving, then return if it's the wrong move action
                                            if (qfJumping.Owner.AnimationData.LongMovement is { } longMove)
                                            {
                                                if (longMove.CombatAction is { } move)
                                                {
                                                    // TODO: Test that I un-commented the part where it doesn't trigger on the action ID of Long Jump
                                                    // TODO: Test with Quick Jump
                                                    if (move.ActionId == ActionId.Stride
                                                        || move.ActionId == longJump)
                                                        return;
                                                }
                                                else
                                                    return;
                                            }
                                            // If not longmoving and not shortmoving, then return
                                            else if (qfJumping.Owner.AnimationData.ShortMovement is null)
                                                return;
                                            // If no options from this space, then return
                                            if (GetOptions() is not { } options
                                                     || options.Count < 2)
                                                return;
                                            // If already asked on this tile, then return
                                            else if (qfJumping.Tag is Tile tile
                                                     && tile == qfJumping.Owner.Space.TopLeftTile)
                                                return;
                                            // Cache this tile
                                            else
                                                qfJumping.Tag = qfJumping.Owner.Space.TopLeftTile;

                                            if (!await qfJumping.Owner.AskForConfirmation(
                                                    action.Illustration,
                                                    """
                                                    {b}Running Tackle (Long Jump){/b} {icon:TwoActions}
                                                    Attempt to Grapple or Shove? {Red}This will end your movement{/Red}.
                                                    """,
                                                    "Yes"))
                                                return;

                                            if (await ExecuteManeuver(options))
                                                qfJumping.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    };
                                    self.AddQEffect(maneuverWhenJumping);*/
                                    
                                    await self.OfferFilteredPossibilitiesAsFreeAction(
                                        ap => ap.CombatAction.ActionId == longJump,
                                        true);

                                    /*self.RemoveAllQEffects(qf => qf == maneuverWhenJumping);*/

                                    CombatAction? lastAction = self.Actions.ActionHistoryThisTurn.LastOrDefault();
                                    if (lastAction is not null
                                        && lastAction.ActionId != ActionId.Pass)
                                    {
                                        // Long Jump was partially completed
                                        actionsSpentSoFar = lastAction.ActionId == ActionId.Stride
                                            ? 1
                                            : 2;
                                    }
                                }
                                else if (actionId == ActionId.Stride)
                                {
                                    for (int i = 0; i < 2; i++)
                                        if (await self.StrideAsync(
                                                $"Choose where to Stride with Running Tackle. ({i+1}/3)",
                                                allowCancel: true))
                                            actionsSpentSoFar++;
                                }

                                switch (actionsSpentSoFar)
                                {
                                    case 0:
                                        action.RevertRequested = true;
                                        return;
                                    case 1:
                                        action.SpentActions = 1;
                                        self.Battle.Log("Running Tackle was converted to a simple Stride.");
                                        goto case 0;
                                    // You used an action as part of jumping.
                                    case 3:
                                        return;
                                }

                                await ExecuteManeuver(GetOptions());
                                
                                return;

                                List<Option> GetOptions()
                                {
                                    List<CombatAction> actions =
                                    [
                                        Possibilities.CreateGrapple(self),
                                        ..CombatManeuverPossibilities.GetAllOptions(CombatManeuverPossibilities.CreateShovePossibility(self)),
                                    ];

                                    List<Option> options = [ new PassViaButtonOption("Do not Grapple or Shove") ];
                                    actions.ForEach(act =>
                                        GameLoop.AddDirectUsageOnCreatureOptions(
                                            act.WithActionCost(0),
                                            options));

                                    return options;
                                }

                                // Perform Grapple or Shove. Reusable for movement triggers.
                                async Task<bool> ExecuteManeuver(List<Option> options)
                                {
                                    Option chosen = (await self.Battle.SendRequest(new AdvancedRequest(
                                            self,
                                            "Choose a maneuver to use as part of Running Tackle. (3/3)",
                                            options)
                                        {
                                            TopBarText = "Choose a maneuver to use as part of Running Tackle. (3/3)",
                                            TopBarIcon = action.Illustration
                                        }))
                                        .ChosenOption;

                                    await chosen.Action();

                                    return chosen is not PassViaButtonOption;
                                }
                            });
                    }
                };
            });

        // TODO: Lv8: Strangle
        //yield return new TrueFeat(
        //ModData.FeatNames.Strangle, 8,
        //"", "", [])
        //.WithAvailableAsArchetypeFeat(Trait.Wrestler)
        //.WithActionCost(1)

        // Lv8: Submission Hold
        yield return new TrueFeat(
                ModData.FeatNames.SubmissionHold, 8,
                "Your iron grip saps your opponent’s strength.",
                $$"""
                {b}Requirements{/b} You have a creature grabbed or restrained.
                
                Attempt an Athletics check to Grapple the required creature, with the following additional effects.{{S.FourDegreesOfSuccess(
                    "The target is {r}enfeebled 2{/r} until the end of its next turn and then is {r}enfeebled 1{/r} for the rest of the encounter.",
                    "The target is {r}enfeebled 1{/r} until the end of its next turn.",
                    null, null)}}
                """,
                [])
            .WithAvailableAsArchetypeFeat(Trait.Wrestler)
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Attempt to Grapple a creature you are holding, {r:enfeebled}enfeebling{/r} it on a success.";
                
                qfFeat.ProvideContextualAction = qfThis =>
                {
                    List<Possibility> helds = qfThis.Owner.HeldItems
                        .Select(item =>
                        {
                            if (item.Grapplee is null)
                                return null;

                            CombatAction subHold = new CombatAction(
                                    qfThis.Owner,
                                    ModData.Illustrations.SubmissionHold,
                                    "Submission Hold",
                                    [ModData.ModTrait, Trait.Archetype],
                                    null!,
                                    Target.Self())
                                .WithActionCost(1)
                                .WithDescription(
                                    "Your iron grip saps your opponent’s strength.",
                                    $$"""
                                      {b}Requirements{/b} You have a creature grabbed or restrained.
                                      
                                      Attempt an Athletics check to Grapple {{item.Grapplee.ToColoredBoldedName()}}, with the following additional effects.{{S.FourDegreesOfSuccess(
                                      "The target is {r}enfeebled 2{/r} until the end of its next turn and then is {r}enfeebled 1{/r} for the rest of the encounter.",
                                      "The target is {r}enfeebled 1{/r} until the end of its next turn.",
                                      null, null)}}
                                      """)
                                .WithEffectOnEachTarget(async (subHold, _,_,_) =>
                                {
                                    CombatAction grapple = Possibilities.CreateGrapple(qfThis.Owner)
                                        .WithActionCost(0);
                                    await grapple.Fullcast(item.Grapplee);
                                    
                                    if (grapple.CheckResult < CheckResult.Success)
                                        return;
                                    
                                    QEffect enfeebled = QEffect.Enfeebled(grapple.CheckResult == CheckResult.CriticalSuccess ? 2 : 1)
                                        .WithExpirationAtEndOfOwnersNextTurn()
                                        .WithSourceAction(subHold);

                                    if (grapple.CheckResult == CheckResult.CriticalSuccess)
                                        enfeebled.WhenExpires += qfEnf =>
                                        {
                                            qfEnf.Owner.AddQEffect(QEffect.Enfeebled(1)
                                                .WithSourceAction(subHold));
                                        };

                                    item.Grapplee.AddQEffect(enfeebled);
                                });

                            return new ActionPossibility(subHold)
                            {
                                Caption = "Submission Hold ("+item.Grapplee.Name+")"
                            };
                        })
                        .WhereNotNull()
                        .Cast<Possibility>()
                        .ToList();

                    return helds.Count switch
                    {
                        0 => null,
                        1 => helds.First(),
                        _ => new SubmenuPossibility(
                            ModData.Illustrations.SubmissionHold,
                            "Submission Hold")
                        {
                            Subsections = [new PossibilitySection("Submission Hold") { Possibilities = helds }]
                        }
                    };
                };
            });

        // TODO: Lv12: Inescapable Grasp
        //yield return new TrueFeat(
        //ModData.FeatNames.InescapableGrasp, 12,
        //"", "", [])
        //.WithAvailableAsArchetypeFeat(Trait.Wrestler)

        // TODO: Lv14: Form Lock
        //yield return new TrueFeat(
        //ModData.FeatNames.Strangle, 14,
        //"", "", [Trait.Attack, Trait.Monk])
        //.WithActionCost(1)

        // Lv14: Form Lock for Wrestler

        // Lv20: Godbreaker
        //yield return new TrueFeat(
        //ModData.FeatNames.Godbreaker, 20,
        //"", "", [])
        //.WithActionCost(3)

        // Lv20: Godbreaker for Wrestler
    }
}