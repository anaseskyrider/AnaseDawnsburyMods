using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.ReactiveAttacks;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.GuardianClass;

public static class GuardianFeats
{
    public static void LoadFeats()
    {
        // Create Guardian feats
        foreach (Feat feat in CreateFeats())
            ModManager.AddFeat(feat);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        #region Level 1
        
        // Bodyguard
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            Feat chargeChoice = new Feat(
                    ModManager.RegisterFeatName(ModData.FeatNames.BodyguardChargeChoice + (i + 1),
                        "Player Character " + (i + 1)),
                    null,
                    "",
                    [ModData.Traits.BodyguardCharge],
                    null)
                .WithNameCreator(_ =>
                    $"Choose {CharacterSheet.GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"} as your charge.")
                .WithRulesTextCreator(_ =>
                    $"Your Taunt's penalty will increase to -2 against {CharacterSheet.GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"}.")
                .WithIllustrationCreator(_ =>
                    CharacterSheet.GetCharacterSheetFromPartyMember(index)?.Illustration ?? ModData.Illustrations.Taunt_1)
                .WithTag(i)
                .WithPermanentQEffect(
                    $"The penalty for Taunt increases to -2 against {{Blue}}{CharacterSheet.GetCharacterSheetFromPartyMember(index)?.Name ?? "a chosen ally"}{{/Blue}}.",
                    qfFeat =>
                    {
                        qfFeat.StartOfCombat = async qfThis =>
                        {
                            if (CharacterSheet.GetCharacterSheetFromPartyMember(index) is {} hero
                                && qfThis.Owner.Battle.AllCreatures.FirstOrDefault(cr2 =>
                                    cr2 != qfThis.Owner &&
                                    cr2.PersistentCharacterSheet == hero) is { } chosenCreature)
                            {
                                QEffect charge = new QEffect()
                                {
                                    Name = "[Bodyguard's Charge]",
                                    Description = $"The penalty to {{Blue}}{qfFeat.Owner}{{/Blue}}'s Taunt increases to -2 against you.",
                                    Illustration = IllustrationName.SunderShield,
                                    Id = ModData.QEffectIds.BodyguardCharge,
                                    Source = qfFeat.Owner,
                                    DoNotShowUpOverhead = true,
                                };
                                chosenCreature.AddQEffect(charge);
                            }
                        };
                    })
                .WithPrerequisite(values => // Can't select yourself
                    CharacterSheet.GetCharacterSheetFromPartyMember(index) != values.Sheet,
                    "Can't select yourself");
            yield return chargeChoice;
        }
        yield return new TrueFeat(
                ModData.FeatNames.Bodyguard,
                1,
                "You swear a vow to protect one of your allies at all costs, regardless of the risk this might pose to you.",
                $$"""
                Choose one of your allies as your charge. When you {{ModData.FeatNames.Taunt.ToLink("Taunt")}}, the penalty your taunted enemy takes increases to –2 against your charge.

                {b}Precombat preparations:{/b} You can choose which ally is your charge at any time outside combat.
                """,
                [ModData.Traits.Guardian])
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption(
                        "GuardianClass.BodyguardCharge",
                        "Bodyguard's Charge",
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                        ft => ft.HasTrait(ModData.Traits.BodyguardCharge))
                    .WithIsOptional());
            });
        
        // Defensive Advance mod, Lv 1
        if (ModManager.TryParse("Defensive Advance", out FeatName defAdv))
            (AllFeats.GetFeatByFeatName(defAdv) as TrueFeat)!
                .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
        
        // Larger than Life
        yield return new TrueFeat(
                ModData.FeatNames.LargerThanLife,
                1,
                "When you're clad in the heaviest of armors, you have an outsized presence.",
                """
                {b}Requirements{/b} You're wearing heavy armor

                You're treated as one size larger for the purposes of targeting other creatures with the Disarm, Grapple, Reposition, Shove, and Trip actions {i}(This is cumulative with {link:TitanWrestler}Titan Wrestler{/}, but not with legendary Athletics){/i}.

                Similarly, you're treated as one size larger for the purposes of creatures targeting you with those same actions, as well as with Swallow Whole.
                """,
                [ModData.Traits.Guardian])
            .WithPermanentQEffect(
                "You're one size larger for the purposes of combat maneuvers.",
                qfFeat =>
                {
                    qfFeat.StateCheck = qfThis =>
                    {
                        if (ModData.CommonRequirements.IsWearingHeavyArmor(qfThis.Owner))
                        {
                            qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                Id = qfThis.Owner.HasEffect(QEffectId.TitanWrestler)
                                    ? QEffectId.TitanWrestlerLegendary
                                    : QEffectId.TitanWrestler
                            });
                            qfThis.Description = "You're one size larger for the purposes of combat maneuvers.";
                        }
                        else
                        {
                            qfThis.Description = "{Red}(Requires heavy armor){/Red} You're one size larger for the purposes of combat maneuvers.";
                        }
                    };
                    qfFeat.PreventTargetingBy = action =>
                    {
                        if (!ModData.CommonRequirements.IsWearingHeavyArmor(qfFeat.Owner)
                            || !(IsManeuver(action) || action.Name.ToLower() is "swallow whole"))
                            return null;

                        if (action.Target is not CreatureTarget crTar)
                            return null;

                        qfFeat.Owner.Space.Size += 1;
                        foreach (var req in crTar.CreatureTargetingRequirements)
                        {
                            if (req is TargetMustBeAtMostSizeCreatureTargetingRequirement
                                    or TargetMustNotBeTwoSizesAboveYouCreatureTargetingRequirement
                                && !req.Satisfied(action.Owner, qfFeat.Owner))
                            {
                                qfFeat.Owner.Space.Size -= 1;
                                return "Larger than life";
                            }
                        }
                        qfFeat.Owner.Space.Size -= 1;
                        
                        return null;
                    };
                    
                    return;

                    bool IsManeuver(CombatAction action)
                    {
                        if (ModManager.TryParse("Reposition", out ActionId reposition)
                            && action.ActionId == reposition)
                            return true;
                        return action.ActionId is ActionId.Disarm or ActionId.Grapple or ActionId.Shove
                            or ActionId.Trip;
                    }
                });
        
        // Long-distance Taunt
        yield return new TrueFeat(
            ModData.FeatNames.LongDistanceTaunt,
            1,
            "You can draw the wrath of your foes even at a great distance.",
            $"When you use {ModData.FeatNames.Taunt.ToLink("Taunt")}, you can choose a target within 120 feet.",
            [ModData.Traits.Guardian]);
        
        // Punishing Shove
        yield return new TrueFeat(
                ModData.FeatNames.PunishingShove,
                1,
                "When you push a foe away, you put the entire force of your armored form into it.",
                "When you successfully Shove a creature, that creature takes an amount of bludgeoning damage equal to your Strength modifier (or double that amount on a critical success). This damage increases by 2 when you become an expert in Athletics, 6 when you become a master, and 12 when you become legendary.",
                [ModData.Traits.Guardian])
            .WithPermanentQEffect(
                "Your Shoves also deal bludgeoning damage.",
                qfFeat =>
                {
                    // BUG: Proficiency doesn't seem to work at this point in the creature construction. Use sheet I guess.
                    /*qfFeat.Description = qfFeat.Description!.Replace(
                        "deal",
                        "deal {b}"
                        + (qfFeat.Owner.Abilities.Strength
                           + qfFeat.Owner.Proficiencies.Get(Trait.Athletics) switch
                           {
                               >= Proficiency.Legendary => 12,
                               >= Proficiency.Master => 6,
                               >= Proficiency.Expert => 2,
                               _ => 0
                           })
                        + "{/b}");*/
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.ActionId != ActionId.Shove || action.CheckResult < CheckResult.Success)
                            return;

                        foreach (Creature cr in action.ChosenTargets.ChosenCreatures)
                        {
                            Proficiency athletics = action.Owner.Proficiencies.Get(Trait.Athletics);
                            int dmg = action.Owner.Abilities.Strength + (athletics >= Proficiency.Legendary ? 12 : athletics >= Proficiency.Master ? 6 : athletics >= Proficiency.Expert ? 2 : 0);
                            string source = "Punishing shove";
                            if (action.CheckResult >= CheckResult.CriticalSuccess)
                            {
                                dmg *= 2;
                                source += " (critical success)";
                            }
                            await CommonSpellEffects.DealDirectDamage(
                                action,
                                DiceFormula.FromText(dmg.ToString(), source),
                                cr,
                                action.CheckResult,
                                DamageKind.Bludgeoning);
                        }
                    };
                });
        
        // Reactive Shield
        (AllFeats.GetFeatByFeatName(FeatName.ReactiveShield) as TrueFeat)!
            .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
        
        // Shield Warfare
        yield return new TrueFeat(
                ModData.FeatNames.ShieldWarfare,
                1,
                "You know how to use shields offensively far better than most.",
                "Increase the weapon damage die of any shield attack by one step.",
                [ModData.Traits.Guardian])
            .WithPermanentQEffect("Increase the damage die of shield attacks.", qfFeat =>
            {
                qfFeat.IncreaseItemDamageDie = (qfThis, item) =>
                {
                    // Shields only
                    if (!item.HasTrait(Trait.Shield))
                        return false;
                    // Don't stack with other increases
                    if (qfThis.Owner.QEffects.Any(qf =>
                            qf != qfThis
                            && qf.IncreaseItemDamageDie?.Invoke(qf, item) == true))
                        return false;
                    return true;
                };
            });
        
        // Shoulder Check
        yield return new TrueFeat(
                ModData.FeatNames.ShoulderCheck,
                1,
                "You hit a foe with your armor to throw them off balance.",
                // fist, kick, gauntlet, or spiked gauntlet.
                "Make a Strike with a fist or kick. The Strike gains the following additional results." + S.FourDegreesOfSuccess(
                    "The target is off-guard against melee attacks you attempt against it until the end of your next turn.",
                    "The target is off-guard against the next melee attack you attempt against it before the end of your current turn.",
                    null,
                    "You are off-guard against melee attacks the target attempts against you until the end of your next turn."),
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + " [flourish] Make a fist Strike that can knock off-guard.";
                qfFeat.Id = QEffectId.AlwaysShowedUnarmedStrike;
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Fist))
                        return null;

                    CombatAction sCheck = qfFeat.Owner.CreateStrike(item)
                        .WithName("Shoulder Check")
                        //.WithExtraTrait(Trait.Basic)
                        .WithExtraTrait(ModData.Traits.Guardian)
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            if (result == CheckResult.Failure)
                                return;
                            
                            Creature? applyTo = null;
                            const string reason = "Shoulder check";
                            QEffect checkEffect = QEffect.FlatFooted(reason);
                            checkEffect.IsFlatFootedTo = (qfThis, cr, action) =>
                            {
                                if (cr != qfThis.Source
                                    || action == null
                                    || !action.HasTrait(Trait.Melee)
                                    || !action.HasTrait(Trait.Attack))
                                    return null;
                                if (result == CheckResult.Success) // Remove after this valid action completes
                                    action.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn) {
                                        WhenExpires = _ =>
                                            qfThis.ExpiresAt = ExpirationCondition.Immediately, 
                                        AfterYouTakeAction = async (qfThis2, action2) =>
                                        {
                                            if (action2 == action)
                                                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                        }});
                                return reason;
                            };
                            
                            // Add effect to target
                            if (result >= CheckResult.Success)
                            {
                                checkEffect.Source = caster;
                                // Increase the duration
                                if (result == CheckResult.CriticalSuccess)
                                    checkEffect.WithExpirationAtEndOfSourcesNextTurn(caster, false);
                                else
                                    checkEffect.ExpiresAt = ExpirationCondition.ExpiresAtEndOfSourcesTurn;
                                applyTo = target;
                            }
                            // Add effect to self
                            else if (result == CheckResult.CriticalFailure)
                            {
                                checkEffect.Source = target;
                                checkEffect.WithExpirationAtEndOfOwnerTurn();
                                checkEffect.CannotExpireThisTurn = true;
                                applyTo = caster;
                            }

                            applyTo?.AddQEffect(checkEffect);
                        });
                    sCheck.Traits = new Traits([ModData.ModTrait, ..sCheck.Traits.ToList()], sCheck);
                    sCheck.Description = StrikeRules.CreateBasicStrikeDescription4(
                        sCheck.StrikeModifiers,
                        additionalCriticalSuccessText: "The target is off-guard to your melee attacks until the end of your next turn",
                        additionalSuccessText: "The target is off-guard to your next melee attack this turn.",
                        additionalCriticalFailureText: "You are off-guard to the target's melee attacks until the end of your next turn.");
                    sCheck.Target = (sCheck.Target as CreatureTarget)!
                        .WithAdditionalConditionOnTargetCreature(
                            ModData.CommonRequirements.MustWearMediumOrHeavyArmor());

                    return sCheck;
                };
            });
        
        #endregion
        
        #region Level 2
        
        // Aggressive Block
        (AllFeats.GetFeatByFeatName(FeatName.AggressiveBlock) as TrueFeat)!
            .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
        
        // Covering Stance
        //// Might not be possible without asterisks.
        /*yield return new TrueFeat(
                ModData.FeatNames.CoveringStance,
                2,
                "Your very presence on the field of battle protects nearby allies from harm.",
                "At the end of each of your turns while you're in this stance, choose one ally adjacent to you to gain lesser cover until the start of your next turn.\n\nThat ally loses this benefit if they move to a space that is no longer adjacent to you at any point during their move.\n\nIf you Intercept an Attack that would harm the ally you're covering, that ally can Step as a free action after your reaction is complete.",
                [ModData.Traits.Guardian]);*/
        
        // Hampering Stance
        yield return new TrueFeat(
                ModData.FeatNames.HamperingStance,
                2,
                "You make it difficult for enemies to move past you.",
                "While you are in this stance, squares in a 5-foot emanation are difficult terrain for your enemies.",
                [Trait.Aura, ModData.Traits.Guardian, Trait.Stance])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                    new ActionPossibility(
                        new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.HamperingStance,
                                "Hampering Stance",
                                [ModData.ModTrait, Trait.Aura, ModData.Traits.Guardian, Trait.Stance],
                                "",
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.HasEffect(ModData.QEffectIds.HamperingStance)
                                        ? "You're already in this stance." : null))
                            .WithDescription(
                                "You make it difficult for enemies to move past you.",
                                "While you are in this stance, squares in a 5-foot emanation are difficult terrain for your enemies.")
                            .WithShortDescription("Enter a stance that makes adjacent squares into difficult terrain for your enemies")
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.StandUp)
                            .WithEffectOnEachTarget(async (_, caster, _, _) =>
                            {
                                QEffect stance = KineticistCommonEffects.EnterStance(
                                    caster,
                                    ModData.Illustrations.HamperingStance,
                                    "Hampering Stance",
                                    "Squares adjacent to you are difficult terrain for your enemies.",
                                    ModData.QEffectIds.HamperingStance);
                                Zone terrain = Zone.Spawn(stance, ZoneAttachment.Aura(1))
                                    .With(zone =>
                                    {
                                        zone.TileEffectCreator = tile =>
                                            new TileQEffect(tile)
                                            {
                                                Illustration = ((IReadOnlyList<IllustrationName>)
                                                [
                                                    IllustrationName.Rubble,
                                                    IllustrationName.Rubble2
                                                ]).GetRandomVisualOnly(),
                                                StateCheck = tQf =>
                                                    tile.DifficultTerrainToComputerControlledCreatures = true
                                            };
                                    });
                            }));
            });
        
        // Phalanx Formation
        yield return new TrueFeat(
                ModData.FeatNames.PhalanxFormation,
                2,
                "You know how to clear a line of fire for your allies.",
                "Allies within 10 feet of you ignore lesser cover.",
                [ModData.Traits.Guardian, Trait.Rebalanced])
            .WithPermanentQEffectAndSameRulesText(qfFeat =>
            {
                qfFeat.AddGrantingOfTechnical(
                    cr =>
                        cr.FriendOfAndNotSelf(qfFeat.Owner)
                        && cr.DistanceTo(qfFeat.Owner) <= 2,
                    qfTech =>
                    {
                        qfTech.Tag = false; // Loop only once
                        qfTech.BonusToAttackRolls = (qfThis, action, target) =>
                        {
                            if (!action.HasTrait(Trait.Attack)
                                || action.HasTrait(Trait.AttackDoesNotTargetAC)
                                || action.ActiveRollSpecification is null
                                || qfThis.Tag is true
                                || target is null)
                                return null;

                            qfThis.Tag = true;

                            // Get all circumstance bonuses to AC on this attack
                            List<Bonus> circumstances = action.ActiveRollSpecification
                                .TaggedDetermineDC
                                .CalculatedNumberProducer
                                .Invoke(action, action.Owner, target)
                                .Bonuses
                                .Where(bonus => bonus is { BonusType: BonusType.Circumstance, Amount: > 0 })
                                .WhereNotNull()
                                .ToList();

                            qfThis.Tag = false;

                            if (circumstances.Count == 0)
                                return null;
                            
                            // The only +1 bonus must be from lesser cover
                            if (!circumstances.All(bonus =>
                                    bonus.Amount == 1
                                    && bonus.BonusSource.ToLower() == "lesser cover"))
                                return null;

                            return new Bonus(1, BonusType.Untyped, "Phalanx formation");
                        };
                    });
            });
        
        // Raise Haft
        yield return new TrueFeat(
                ModData.FeatNames.RaiseHaft,
                2,
                "You know how to use the haft of larger weapons to block your enemies' attacks.",
                "Two-handed weapons you wield gain the parry trait. If the weapon already has the parry trait, you increase the circumstance bonus to AC it provides to +2."
                    + "\n\n" + ModData.Illustrations.DawnsburySun.IllustrationAsIconString + " {b}Modding{/b} This benefits more with mods which add parry weapons.",
                [ModData.Traits.Guardian])
            .WithOnCreature(self =>
            {
                self.AddQEffect(ParryLogic.GreaterParry(
                    "Raise Haft",
                    "Two-handed weapons gain the parry trait for you, or increase the bonus to +2 if they already have it.",
                    (_, weapon) =>
                        weapon.HasTrait(Trait.TwoHanded)));
            });
        
        // Shield your Eyes (useless?)
        /*yield return new TrueFeat(
                ModData.FeatNames.ShieldYourEyes,
                2,
                "You reflexively place your shield between your eyes and visual dangers.",
                "While your shield is raised, you gain a +2 circumstance bonus to all defenses against effects with the light or visual trait. If you critically fail your save against such an effect while your shield is raised, you fail instead. Likewise, if such an effect critically succeeds against your DC, it's a success instead.",
                [ModData.Traits.Guardian]);*/
        
        // Shielding Taunt
        yield return new TrueFeat(
                ModData.FeatNames.ShieldingTaunt,
                2,
                "By banging loudly on your shield, you get the attention of even the most stubborn of foes.",
                $"Raise a Shield, and then {ModData.FeatNames.Taunt.ToLink("Taunt")} a creature. Your Taunt gains the auditory trait.",
                [Trait.Flourish, ModData.Traits.Guardian, MoreShields.ModData.Traits.ShieldActionFeat])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] Raise a Shield and make an auditory Taunt.";
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.Name != "Raise shield"
                        && section.PossibilitySectionId != ModData.PossibilitySectionIds.TauntActivities)
                        return null;
                    
                    Creature guardian = qfFeat.Owner;

                    if (MoreShields.CommonShieldRules.GetWieldedShields(guardian) is not { } shields)
                        return null;
                    if (shields.Count == 0)
                        return null;
                    if (shields.MaxBy(MoreShields.CommonShieldRules.GetAC) is not { } shield)
                        return null;
                    
                    // Used for targeting logic
                    CombatAction aTaunt = GuardianClass.CreateTaunt(guardian, true, Trait.Auditory)
                        .WithActionCost(0);
                    
                    CombatAction shieldTaunt = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(shield.Illustration, ModData.Illustrations.Taunt_1),
                            "Shielding Taunt",
                            "{i}By banging loudly on your shield, you get the attention of even the most stubborn of foes.{/i}\n\nRaise a Shield, and then Taunt a creature. Your Taunt gains the auditory trait.",
                            [ModData.ModTrait, Trait.DoNotShowOverheadOfActionName, Trait.UnaffectedByConcealment, Trait.Flourish, ModData.Traits.Guardian],
                            aTaunt.Target)
                        .WithActionCost(1)
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            // Raise a shield
                            await MoreShields.CommonShieldRules.OfferToRaiseAShield(caster);
                            
                            // Used for actual execution
                            // Not doing it twice results in usage errors
                            CombatAction aTaunt2 = GuardianClass.CreateTaunt(guardian, true, Trait.Auditory)
                                .WithActionCost(0);
                            await caster.Battle.GameLoop.FullCast(aTaunt2, ChosenTargets.CreateSingleTarget(target));
                        });
                    
                    if (section.Name == "Raise shield")
                        shieldTaunt.Traits.Add(Trait.DoNotShowInContextMenu);

                    return (ActionPossibility)shieldTaunt;
                };
            });
        
        // Taunting Strike
        yield return new TrueFeat(
                ModData.FeatNames.TauntingStrike,
                2,
                "The force of your blow causes your enemy to focus their attention on you.",
                $"Make a Strike. Regardless of whether the Strike hits, you {ModData.FeatNames.Taunt.ToLink("Taunt")} the target. Your Taunt gains the visual trait.",
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] Strike and visually Taunt a creature.";
                // The actual action
                qfFeat.ProvideStrikeModifier = item =>
                    CreateTauntingStrike(item, false);
                qfFeat.Owner.AddQEffect(new QEffect()
                {
                    Name = "[TAUNTING STRIKE THROWN VARIANT GRANTER]",
                    ProvideStrikeModifier = item =>
                        item.WeaponProperties!.ForcedMelee && item.WeaponProperties!.Throwable
                            ? CreateTauntingStrike(item, true)
                            : null
                });
                
                return;

                CombatAction CreateTauntingStrike(Item item, bool isThrown)
                {
                    CombatAction tauntingStrike = StrikeRules
                        .CreateStrike(
                            qfFeat.Owner,
                            item,
                            isThrown || item.HasTrait(Trait.Ranged)
                                ? RangeKind.Ranged
                                : RangeKind.Melee,
                            -1,
                            isThrown)
                        .WithName("Taunting Strike" + (isThrown ? " (Thrown)" : null))
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(ModData.Traits.Guardian)
                        //.WithExtraTrait(Trait.Basic)
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            CombatAction taunt = GuardianClass.CreateTaunt(caster, true, Trait.Visual)
                                .WithActionCost(0);
                            await caster.Battle.GameLoop.FullCast(taunt, ChosenTargets.CreateSingleTarget(target));
                        });
                    tauntingStrike.Illustration = new SideBySideIllustration(
                        item.Illustration,
                        ModData.Illustrations.Taunt_1);
                    tauntingStrike.Traits = new Traits([ModData.Traits.ModName, ..tauntingStrike.Traits.ToList()], tauntingStrike);
                    tauntingStrike.Description = StrikeRules.CreateBasicStrikeDescription4(
                        tauntingStrike.StrikeModifiers,
                        additionalAftertext: "Make a visual Taunt against the Strike's target.");
                    (tauntingStrike.Target as CreatureTarget)!
                        .WithAdditionalConditionOnTargetCreature((a, d) => 
                            a.DistanceTo(d) > (a.HasFeat(ModData.FeatNames.LongDistanceTaunt) ? 24 : 6)
                                ? Usability.CommonReasons.TargetOutOfRange
                                : Usability.Usable)
                        .WithAdditionalConditionOnTargetCreature((a, d) =>
                            d.IsImmuneTo(Trait.Visual)
                                ? Usability.NotUsableOnThisCreature("Immune to visual")
                                : Usability.Usable);
                    return tauntingStrike;
                }
            });
        
        #endregion
        
        #region Level 4
        
        // Area Armor
        yield return new TrueFeat(
                ModData.FeatNames.AreaArmor,
                4,
                "The armor you wear protects you and shelters your allies against explosions and other large-scale assaults.",
                "While you're wearing medium or heavy armor, allies adjacent to you gain a +1 circumstance bonus to Reflex saves against area effects. If you're a master in the armor, the bonus is +2 instead.",
                [ModData.Traits.Guardian])
            .WithPermanentQEffect(
                "Adjacent allies get a bonus to Reflex saves against area effects.",
                qfFeat =>
                {
                    qfFeat.Tag = 0;
                    qfFeat.StateCheck = qfThis =>
                    {
                        if (ModData.CommonRequirements.GetMediumOrHeavyArmor(qfThis.Owner) is {} armor)
                        {
                            int bonus = qfThis.Owner.Proficiencies.Get(armor.Traits) >= Proficiency.Master ? 2 : 1;
                            qfThis.Tag = bonus;
                            qfThis.Description = $"Adjacent allies get a {$"+{bonus}".WithColor(bonus > 1 ? "Blue" : null)} circumstance bonus to Reflex saves against area effects.";
                        }
                        else
                        {
                            qfThis.Tag = 0;
                            qfThis.Description = "{Red}(Requires medium or heavy armor){/Red} Adjacent allies get a bonus to Reflex saves against area effects.";
                        }
                    };
                    qfFeat.AddGrantingOfTechnical(
                        (qfThis, cr) =>
                            cr.IsAdjacentTo(qfThis.Owner)
                            && cr.FriendOfAndNotSelf(qfThis.Owner),
                        qfTech =>
                        {
                            qfTech.BonusToDefenses = (qfThis, action, def) =>
                                def is Defense.Reflex
                                && action?.Target is AreaTarget
                                && qfThis.Tag is int bonus and > 0
                                    ? new Bonus(bonus, BonusType.Circumstance, "Area armor")
                                    : null;
                        });
                });
        
        // Armored Courage
        yield return new TrueFeat(
                ModData.FeatNames.ArmoredCourage,
                4,
                "You take comfort in the safety of your armor.",
                """
                {b}Requirements{/b} You are wearing medium or heavy armor.
                {b}Frequency{/b} once per encounter

                You gain a number of temporary Hit Points equal to your level, and you reduce your frightened condition value by 1.
                """,
                [ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.Tag = false;
                qfFeat.AddToDefenseBlock = qfThis =>
                {
                    string req = ModData.CommonRequirements.IsWearingMediumOrHeavyArmor(qfThis.Owner)
                        ? " "
                        : " {Red}(Must be wearing medium or heavy armor){/Red} ";
                    string desc = $"Once per encounter, gain {{Blue}}{qfThis.Owner.Level}{{/Blue}} temp HP and reduce your frightened by 1.";
                    return qfThis.Name!.WithTag("b") + req + desc.WithTag(qfThis.Tag is true ? "strike" : null);
                };
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Tag is true)
                        return null;
                    CombatAction courage = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.ArmoredCourage,
                            "Armored Courage",
                            [Trait.Basic, ModData.ModTrait, ModData.Traits.Guardian],
                            $$"""
                              {i}You take comfort in the safety of your armor.{/i}

                              {b}Requirements{/b} You are wearing medium or heavy armor.
                              {b}Frequency{/b} once per encounter

                              You gain {Blue}{{qfThis.Owner.Level}}{/Blue} temporary Hit Points. Reduce your frightened condition value by 1.
                              """,
                            Target.Self()
                                .WithAdditionalRestriction(cr =>
                                    ModData.CommonRequirements.MustWearMediumOrHeavyArmor()
                                        .Satisfied(cr, cr).UnusableReason))
                        .WithSoundEffect(SfxName.MinorAbjuration)
                        .WithEffectOnSelf(async self =>
                        {
                            qfThis.Tag = true;
                            qfThis.Description = qfThis.Description?.Replace(
                                "{Green}Once per encounter{/Green}",
                                "{Red}Once per encounter{/Red}");
                            self.GainTemporaryHP(self.Level);
                            if (self.FindQEffect(QEffectId.Frightened) is { } frightened)
                                Fighter.ReduceFrightenedValueOfFrightened(self, frightened);
                        });

                    return (ActionPossibility)courage;
                };
            });
        
        // Energy Interceptor
        yield return new TrueFeat(
                ModData.FeatNames.EnergyInterceptor,
                4,
                "Though other guardians understand how to anticipate the flow of martial combat, you predict blasts of magical lightning, blazing trap runes, and more.",
                $"You can use {ModData.FeatNames.InterceptAttack.ToLink("Intercept Attack {icon:Reaction}")} when an ally would take acid, cold, electricity, fire, or sonic damage, not only when they would take physical damage.",
                [ModData.Traits.Guardian])
            .WithPrerequisite(
                ModData.CommonRequirements.HasInterceptAttack,
                "You must have the guardian's Intercept Attack feature.");
        
        // Flying Tackle
        //// Not sure if will include due to reliance on More Basic Actions, and weak implementation
        
        // Not so Fast!
        yield return new TrueFeat(
                ModData.FeatNames.NotSoFast,
                4,
                "You lash out when foes try to get past you, possibly stopping them in their tracks.",
                """
                {b}Requirements{/b} You are in Hampering Stance.
                {b}Trigger{/b} A creature within your reach leaves a square during a move action it's using.

                Make a melee Strike against the triggering creature. The Strike gains the following additional results.
                """
                + S.FourDegreesOfSuccess(
                    "The target's movement is disrupted.",
                    "The target takes a –10-foot circumstance penalty to its Speed for the rest of its triggering movement. This penalty might cause the triggering creature's movement to end immediately based on its affected Speed.",
                    "As success, but the target instead takes a –5-foot circumstance penalty to its Speed.",
                    "The target is unaffected."),
                [ModData.Traits.Guardian])
            .WithActionCost(-2)
            .WithPrerequisite(ModData.FeatNames.HamperingStance, "Hampering Stance")
            .WithOnCreature(self =>
            {
                AttackOfOpportunityMechanics mechanics = new AttackOfOpportunityMechanics()
                {
                    Name = "Not so Fast!", // QuestionText doesn't ask about the name
                    Description = "While in Hampering Stance, creatures who leave a square in your reach provoke a reaction to Strike and slow them down.",
                    RestrictToOnlyAgainstWhom = (qfReact, _) =>
                        qfReact.Owner.HasEffect(ModData.QEffectIds.HamperingStance),
                    OverheadName = "*not so fast!*",
                    StandStill = true,
                    StrikeAndReactionTraits = [ModData.Traits.Guardian, ModData.Traits.NotSoFastAttack, Trait.ReactiveAttack],
                    NumberOfStrikes = 1,
                };
                QEffect notSoFast = AttackOfOpportunityMechanics.AttackOfOpportunity(mechanics);
                notSoFast.Name = "Not so Fast! {icon:Reaction}"; // PETR: Fix missing space before the pip
                notSoFast.Innate = false;
                notSoFast.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") +
                    $" {(!qfThis.Owner.HasEffect(ModData.QEffectIds.HamperingStance) ? "{Red}(Must be in Hampering Stance){/Red} " : null)}Creatures who leave a square in your reach provoke a reaction to Strike and slow them down.";
                var oldProvoke = notSoFast.WhenProvoked;
                notSoFast.WhenProvoked = async (qfThis, action) =>
                {
                    // Must be exiting a square, not just any move action.
                    if (action.TilesMoved == 0)
                        return;
                    await oldProvoke!.Invoke(qfThis, action);
                };
                notSoFast.AfterYouTakeAction = async (qfThis, action) =>
                {
                    if (!action.HasTrait(ModData.Traits.NotSoFastAttack))
                        return;

                    Creature provoker = action.ChosenTargets.ChosenCreature!;
                    
                    // Determine move disruption result
                    int pen = 1;
                    switch (action.CheckResult)
                    {
                        //
                        // Disrupt on a crit success is handled by StandStill = true
                        //
                        case CheckResult.Success:
                            pen = 2;
                            goto case CheckResult.Failure;
                        case CheckResult.Failure:
                            // Apply the speed penalty
                            QEffect speedPen = QEffect.PenaltyToSpeed(pen, BonusType.Circumstance);
                            speedPen.ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn;
                            speedPen.StateCheck += qfPen =>
                            {
                                if (qfPen.Owner.AnimationData.LongMovement is null)
                                    qfPen.ExpiresAt = ExpirationCondition.Immediately;
                            };
                            provoker.AddQEffect(speedPen);

                            // Determine disruption
                            if (provoker.AnimationData.LongMovement is { Path: not null } move)
                            {
                                // Calculate the cost of every possible movement along its original path.
                                IList<Tile>? recalculated = null;
                                foreach (Tile tile in move.Path)
                                {
                                    IList<Tile> truncPath = move.Path
                                        .Take(move.Path.IndexOf(tile) + 1)
                                        .ToList();
                                    int cost = CostOfPath(provoker, move.OriginalTile, truncPath);
                                    if (cost <= provoker.Speed)
                                        recalculated = truncPath; // Store the last path it could move to
                                    else
                                        break;
                                }

                                // Don't do anything if it errors
                                if (recalculated is null)
                                    break;

                                // Disrupt immediately if
                                if (!recalculated.Contains(provoker.Occupies) // They're already too far along
                                    || ReferenceEquals(recalculated.LastOrDefault(),
                                        provoker.Occupies)) // Can't move further
                                    action.Disrupted = true;
                                // Otherwise, disrupt when they reach their new furthest intended tile
                                else if (recalculated.Last() is { } last
                                         && !ReferenceEquals(last, move.Path.Last()))
                                {
                                    speedPen.StateCheck += qfPen =>
                                    {
                                        if (ReferenceEquals(qfPen.Owner.Occupies, last)) // Reaches the last tile
                                        {
                                            action.Disrupted = true;
                                            qfPen.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    };
                                }
                            }

                            break;
                    }
                };
                self.AddQEffect(notSoFast);

                return;
                
                // Gets the movement cost for a MOVER who begins at the START tile and follows it along a PATH. Uses LongMovement.OriginalTile and LongMovement.Path.
                int CostOfPath(Creature mover, Tile start, IList<Tile> path)
                {
                    int move = 0;
                    var diagonals = 0;
                    for (var index = 0; index < path.Count; index++)
                    {
                        Tile tile = path[index];
                        var tiles = path.ToList();
                        if (tile.GetWalkDifficulty(mover) >= 1)
                            move += tile.GetWalkDifficulty(mover);
                        switch (index)
                        {
                            case >= 1 when tiles.Count > 1:
                                if (Equals(tile.Neighbours.BottomLeft?.Tile,
                                        tiles[index - 1]) ||
                                    Equals(tile.Neighbours.BottomRight?.Tile,
                                        tiles[index - 1]) ||
                                    Equals(tile.Neighbours.TopLeft?.Tile,
                                        tiles[index - 1]) ||
                                    Equals(tile.Neighbours.TopRight?.Tile,
                                        tiles[index - 1]))
                                    diagonals += 1;
                                break;
                            case 0 when tiles.Count > 1:
                                if (Equals(tile.Neighbours.BottomLeft?.Tile,
                                        start) ||
                                    Equals(tile.Neighbours.BottomRight?.Tile,
                                        start) ||
                                    Equals(tile.Neighbours.TopLeft?.Tile,
                                        start) ||
                                    Equals(tile.Neighbours.TopRight?.Tile,
                                        start))
                                    diagonals += 1;
                                break;
                        }
                    }

                    if (diagonals > 1)
                        move += diagonals / 2;

                    return move;
                }
            });
        
        // Proud Nail
        yield return new TrueFeat(
                ModData.FeatNames.ProudNail,
                4,
                "When a foe ignores your taunts, you make them pay.",
                $$"""
                {b}Requirements{/b} Your {{ModData.FeatNames.Taunt.ToLink("taunted enemy")}} is off-guard because it didn't target you or include you in an area effect.

                Make a melee Strike against your taunted enemy. If this Strike hits, you deal an extra die of weapon damage. If you're at least 10th level, increase this to two extra dice, and if you're at least 18th level, increase it to three extra dice.
                """,
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] Strike a taunted enemy who ignored your Taunt, dealing extra damage.";
                qfFeat.ProvideStrikeModifier = item =>
                {
                    int lvl = qfFeat.Owner.Level;

                    StrikeModifiers newMods = new StrikeModifiers()
                    {
                        AdditionalWeaponDamageDice = lvl >= 18 ? 3 : lvl >= 10 ? 2 : 1,
                    };
                    CombatAction proudNail = qfFeat.Owner.CreateStrike(item, -1, newMods)
                        .WithName("Proud Nail")
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            newMods,
                            prologueText: "{b}Requirements{/b} Your taunted enemy is off-guard because it didn't target you or include you in an area effect.\n"))
                        //.WithExtraTrait(Trait.Basic)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(ModData.Traits.Guardian);
                    proudNail.Traits = new Traits([ModData.ModTrait, ..proudNail.Traits.ToList()],
                        proudNail);
                    proudNail.Illustration = new SideBySideIllustration(
                        proudNail.Illustration, IllustrationName.StarHit);
                    ((CreatureTarget)proudNail.Target) // Strikes always make CreatureTargets
                        .WithAdditionalConditionOnTargetCreature(
                            ModData.CommonRequirements.IsMyTauntedEnemy())
                        .WithAdditionalConditionOnTargetCreature(
                            ModData.CommonRequirements.OffGuardDueToMyTaunt());
                    
                    return proudNail;
                };
            });
        
        // Shielded Attrition
        yield return new TrueFeat(
                ModData.FeatNames.ShieldedAttrition,
                4,
                "You provoke attacks from foes that might otherwise stop your allies from moving.",
                """
                {b}Requirements{/b} You are wielding a shield.

                Raise your Shield, then Stride up to half your Speed. This movement triggers enemies' reactions as normal. Each enemy who reacted to your movement is unable to react to your allies' movement until the start of your next turn (even if they've since regained their reaction).
                """,
                [ModData.Traits.Guardian, MoreShields.ModData.Traits.ShieldActionFeat])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") +
                    " Raise a Shield, Stride half your speed, and deny reactions to your allies' movement until the start of your next turn.";
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.Name != "Raise shield")
                        return null;
                    
                    Item? shield = MoreShields.CommonShieldRules
                        .GetWieldedShields(qfThis.Owner)
                        .FirstOrDefault();
                    
                    CombatAction shieldedAttrition = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                shield?.Illustration ?? IllustrationName.SteelShield,
                                IllustrationName.FleetStep),
                            "Shielded Attrition",
                            [ModData.ModTrait, ModData.Traits.Guardian],
                            null!,
                            Target.Self()
                                // In strict hypothesis, this restriction should never get called
                                .WithAdditionalRestriction(_ =>
                                    shield is null
                                        ? "Must be wielding a shield"
                                        : null))
                        .WithDescription(
                            "You provoke attacks from foes that might otherwise stop your allies from moving.",
                            "Raise your Shield, then Stride up to half your Speed. This movement triggers enemies' reactions as normal. Each enemy who reacted to your movement is unable to react to your allies' movement until the start of your next turn (even if they've since regained their reaction).")
                        .WithActionCost(1)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            CombatAction pathStride = CommonCombatActions.StepByStepStride(self)
                                .WithActionCost(0);
                            pathStride.EffectOnChosenTargets = null;
                            pathStride.WithEffectOnChosenTargets(async (action2, self2, targets) =>
                            {
                                await MoreShields.CommonShieldRules.OfferToRaiseAShield(self2);
                                await self2.MoveToUsingStepByStepPath(
                                    targets.ChosenTiles,
                                    action2,
                                    new MovementStyle()
                                    {
                                        MaximumSquares = 1000
                                    });
                            });
                            self.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                            {
                                Name = "[SHIELDED ATTRITION META]",
                                // Half speed
                                BonusToAllSpeeds = _ =>
                                    new Bonus(
                                        -(int)Math.Round(self.Speed / 2f, MidpointRounding.ToPositiveInfinity),
                                        BonusType.Untyped, 
                                        "Shielded attrition"),
                                // Remove after completion
                                AfterYouTakeAction = async (qfThis2, action2) =>
                                {
                                    if (action2 == pathStride)
                                        qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                },
                                AfterYouAreTargeted = async (qfThis2, action2) =>
                                {
                                    if (!action2.HasTrait(Trait.ReactiveAttack))
                                        return;

                                    QEffect cantReact = new QEffect(
                                            "Shielded Attrition",
                                            "You can't react to the movement of {Blue}" + self.Name + "'s{/Blue} allies.",
                                            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                            self,
                                            IllustrationName.DawnsburyThree)
                                    {
                                        RoundsLeft = 1,
                                        // Sometimes applies twice, idk why, this helps with that
                                        Key = ModData.CommonQfKeys.SHIELDED_ATTRITION+self.Name,
                                    }
                                    .AddGrantingOfTechnical(
                                        self.FriendOfAndNotSelf,
                                        qfTech =>
                                        {
                                            qfTech.PreventTargetingBy = ca =>
                                            {
                                                if (ca.Owner != action2.Owner)
                                                    return null;
                                                if (!ca.HasTrait(Trait.ReactiveAttack))
                                                    return null;
                                                if (qfTech.Owner.AnimationData.LongMovement is null)
                                                    return null;
                                                return "Shielded attrition";
                                            };
                                        });

                                    action2.Owner.AddQEffect(cantReact);
                                },
                            });
                            if (!await self.Battle.GameLoop.FullCast(pathStride))
                                action.RevertRequested = true;
                        });

                    return (ActionPossibility) shieldedAttrition;
                };
            });
        
        #endregion
        
        #region Level 6
        
        // Disarming Intercept
        yield return new TrueFeat(
                ModData.FeatNames.DisarmingIntercept,
                6,
                "When you catch a weapon in your armor, you can move your body to wrench it from your foe's grasp.",
                $$"""
                {b}Trigger{/b} You {{ModData.FeatNames.InterceptAttack.ToLink("Intercept an Attack")}} that was made with a melee weapon by a creature you're adjacent to.

                After Intercepting the Attack, attempt to Disarm the weapon used for that attack. You don't need to have a hand free, and you gain an item bonus to the Athletics check equal to the value of your armor's potency rune.
                """,
                [ModData.Traits.Guardian])
            .WithActionCost(0)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " When you Intercept a melee Attack, you can attempt to Disarm the attacker.";
                qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                {
                    if (action.ActionId != ModData.ActionIds.InterceptAttack)
                        return;

                    if ((action.Tag as DamageEvent)?.CombatAction is not { } interceptedAttack)
                        return;
                        
                    // Has to be a melee strike with a disarmable item
                    if (!interceptedAttack.HasTrait(Trait.Melee) // Melee
                        || !interceptedAttack.HasTrait(Trait.Strike) // Strike
                        || interceptedAttack.Item is null // With a disarmable item
                        || !interceptedAttack.Owner.IsAdjacentTo(qfThis.Owner)) // Who's adjacent
                        return;
                    
                    // Store MAP
                    int oldMAP = qfThis.Owner.Actions.AttackedThisManyTimesThisTurn;
                    qfThis.Owner.Actions.AttackedThisManyTimesThisTurn = 0;

                    // Use disarm weapon, or use free hand
                    Item maneuverWeapon = qfThis.Owner.HeldItems.FirstOrDefault(item =>
                        item.HasTrait(Trait.Disarm))
                                          ?? qfThis.Owner.UnarmedStrike;
                    CombatAction disarm = CombatManeuverPossibilities
                        .CreateDisarmAction(qfThis.Owner, maneuverWeapon)
                        .WithActionCost(0);
                    // Remove free hand requirement by rebuilding targeting
                    disarm.Target = Target.Reach(maneuverWeapon)
                        .WithAdditionalConditionOnTargetCreature(new TargetWieldsAnItemCreatureTargetingRequirement());
                    
                    // Execute Disarm
                    qfThis.Owner.AddQEffect(new QEffect()
                    {
                        BonusToSkillChecks = (skill, action2, target) =>
                            skill is Skill.Athletics
                            && action2 == disarm
                            && action2.Owner.BaseArmor?.ArmorProperties?.ItemBonus is { } potency
                                ? new Bonus(potency, BonusType.Item, "Armor potency")
                                : null,
                        AfterYouTakeAction = async (qfThis2, action2) =>
                        {
                            if (action2 == disarm)
                                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                        },
                    });
                    await qfThis.Owner.Battle.GameLoop.FullCast(disarm,
                        ChosenTargets.CreateSingleTarget(interceptedAttack.Owner));
                    
                    // Restore MAP
                    qfThis.Owner.Actions.AttackedThisManyTimesThisTurn = oldMAP;
                };
            })
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.InterceptAttack),
                "You must have the Intercept Attack feature.");
        
        // Guarded Advance
        yield return new TrueFeat(
                ModData.FeatNames.GuardedAdvance,
                6,
                "You slowly advance on the battlefield, taking utmost caution.",
                "You Raise a Shield and Step twice.",
                [ModData.Traits.Guardian, MoreShields.ModData.Traits.ShieldActionFeat])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Raise a Shield and Step twice.";
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.Name != "Raise shield")
                        return null;
                    
                    Creature guardian = qfFeat.Owner;

                    if (MoreShields.CommonShieldRules.GetWieldedShields(guardian) is not { } shields)
                        return null;
                    if (shields.Count == 0)
                        return null;
                    if (shields.MaxBy(MoreShields.CommonShieldRules.GetAC) is not { } shield)
                        return null;
                    
                    CombatAction guardAdvance = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(shield.Illustration, IllustrationName.FleetStep),
                            "Guarded Advance",
                            [Trait.DoNotShowOverheadOfActionName, ModData.Traits.ModName, ModData.Traits.Guardian],
                            "{i}You slowly advance on the battlefield, taking utmost caution.{/i}\n\nYou Raise a Shield and Step twice.",
                            Target.Self()
                                .WithAdditionalRestriction(cr =>
                                {
                                    if (cr.HasEffect(QEffectId.Immobilized))
                                        return "Immobilized";
                                    if (!CommonCombatActions.StepByStepStride(cr).CanBeginToUse(cr))
                                        return "Can't move";
                                    List<Tile> tiles = cr.Battle.Map.AllTiles
                                        .Where(tile =>
                                            tile.IsAdjacentTo(cr.Occupies)
                                            && tile.LooksFreeTo(cr))
                                        .ToList();
                                    if (tiles.Count == 0)
                                        return "No open spaces";
                                    if (!cr.HasEffect(QEffectId.FeatherStep)
                                        && tiles.All(tile =>
                                                tile.CountsAsNonignoredDifficultTerrainFor(cr)))
                                        return "Can't Step anywhere";
                                    return null;
                                }))
                        .WithActionCost(1)
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            // Raise a shield
                            await MoreShields.CommonShieldRules.OfferToRaiseAShield(caster);
                            
                            await caster.StepAsync(
                                "Choose where to Step with Guarded Advance, or right-click to cancel. (1/2)",
                                true,
                                true);
                            await caster.StepAsync(
                                "Choose where to Step with Guarded Advance, or right-click to cancel. (2/2)",
                                true,
                                true);
                        });
                    
                    return (ActionPossibility)guardAdvance;
                };
            });
        
        // Lock Down
        yield return new TrueFeat(
                ModData.FeatNames.LockDown,
                6,
                "You attack an enemy to ensure they can't move beyond your reach.",
                """
                {b}Requirements{/b} You are in Hampering Stance.

                Strike an enemy within your reach. If you hit and deal damage, that enemy must make a DC 5 flat check to successfully use move actions, or DC 11 if the action is to move beyond the reach of the weapon or unarmed attack you used for the Strike.

                This effect lasts until the beginning of your next turn, until you move, or until you use that weapon or unarmed attack to make another attack, whichever comes first.
                """,
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + $" [flourish] {(!qfThis.Owner.HasEffect(ModData.QEffectIds.HamperingStance) ? "{Red}(Must be in Hampering Stance){/Red} " : " ")}Strike a creature, inhibiting their movement for 1 round, unless you move or Strike with that weapon again.";
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee))
                        return null;
                    
                    int reach = item.HasTrait(Trait.Reach) ? 2 : 1;
                    StrikeModifiers newMods = new StrikeModifiers(){ };

                    CombatAction lockDown = qfFeat.Owner
                        .CreateStrike(item, -1, newMods)
                        .WithName("Lock Down")
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            newMods,
                            additionalAttackRollText:
                            "If you hit and deal damage, the target must make a DC 5 flat check to successfully use move actions, or DC 11 if the action is to move to a space beyond the reach of the weapon or unarmed attack you used for the Strike.\n\nThis effect lasts until the beginning of your next turn, until you move, or until you use that weapon or unarmed attack to make another attack, whichever comes first."))
                        .WithExtraTrait(Trait.Basic)
                        .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(ModData.Traits.Guardian)
                        .WithHitAndDealDamage(async (caster, action, target) =>
                        {
                            QEffect lockDownPenalty = new QEffect(
                                "Locked Down",
                                "If you attempt a move action, you must succeed at a DC 5 flat check or it is lost. If the move action is to move to a space away from {Blue}" +
                                caster + "{/Blue}, the DC is 11.",
                                ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                caster,
                                ModData.Illustrations.LockDown)
                            {
                                FizzleOutgoingActions = async (qfThis, action2, builder) =>
                                {
                                    if (!action2.HasTrait(Trait.Move) ||
                                        action2.ChosenTargets.ChosenTile is null)
                                        return false;

                                    int dc = action2.ChosenTargets.ChosenTile.DistanceTo(caster) > reach
                                        ? 11
                                        : 5;

                                    (CheckResult, string) result = Checks.RollFlatCheck(dc);

                                    builder.AppendLine($"Use move action while locked down: {result.Item2}" +
                                                       $"\n\n{{b}}{dc} DC breakdown:\n5{{/b}} Flat DC");
                                    if (dc == 11)
                                        builder.AppendLine("{b}{Red}+6{/Red}{/b} moved further away");

                                    if (result.Item1 < CheckResult.Success)
                                        return true;

                                    // Certain basic actions don't reach the code block where this log is announced,
                                    // so this manually announces them anyway.
                                    if (action2.ActionId is ActionId.Stride or ActionId.Step
                                        or ActionId.StepByStepStride)
                                        action2.Owner.Battle.Log(
                                            "Flat check passed.",
                                            action2.Name,
                                            builder.ToString());

                                    return false;
                                }
                            };
                            target.AddQEffect(lockDownPenalty);

                            QEffect lockDownRequirements = new QEffect(
                                "Locking Down",
                                "Until the start of your next turn or until you move or attack with your {Blue}" +
                                item.Name + "{/Blue}, you have locked down {Blue}" + target + "{/Blue}.",
                                ExpirationCondition.ExpiresAtStartOfYourTurn,
                                caster,
                                ModData.Illustrations.LockDown)
                            {
                                DoNotShowUpOverhead = true,
                                AfterYouTakeAction = async (qfThis, action2) =>
                                {
                                    if (action2 == action)
                                        return;
                                    if (!action2.HasTrait(Trait.Move)
                                        && !(action2.HasTrait(Trait.Attack) && action2.Item == item))
                                        return;

                                    lockDownPenalty.ExpiresAt = ExpirationCondition.Immediately;
                                    qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                }
                            };
                            caster.AddQEffect(lockDownRequirements);
                        });
                    lockDown.Traits = new Traits([ModData.ModTrait, ..lockDown.Traits.ToList()], lockDown);
                    lockDown.Illustration = new SideBySideIllustration(
                        item.Illustration, ModData.Illustrations.LockDown);
                    ((CreatureTarget)lockDown.Target).WithAdditionalConditionOnTargetCreature((a, d) =>
                    {
                        if (!a.HasEffect(ModData.QEffectIds.HamperingStance))
                            return Usability.NotUsable("Must be in Hampering Stance");
                        return Usability.Usable;
                    });
                    
                    return lockDown;
                };
            })
            .WithPrerequisite(ModData.FeatNames.HamperingStance, "Hampering Stance");
        
        // Reactive Strike
        yield return new TrueFeat(
                ModData.FeatNames.ReactiveStrike,
                6,
                "You swat a foe who leaves themself open to retaliation.",
                """
                {b}Trigger{/b} A creature within your reach uses a manipulate action or move action, makes a ranged attack, or leaves a square during a move action it's using.

                Make a melee Strike against the triggering creature. If your attack is a critical hit and the trigger was a manipulate action, you disrupt that action. This Strike doesn't count toward your multiple attack penalty, and your multiple attack penalty doesn't apply to this Strike.
                """,
                [ModData.Traits.Guardian])
            .WithActionCost(Constants.ACTION_COST_REACTION)
            .WithOnCreature(self =>
            {
                QEffect reactiveStrike = QEffect.AttackOfOpportunity();
                reactiveStrike.Name = reactiveStrike.Name?
                    .Replace("Attack of Opportunity", "Reactive Strike")
                    .Replace("e{", "e {"); // PETR: Fix missing space
                self.AddQEffect(reactiveStrike);
            })
            .WithEquivalent(values => values.AllFeats.Any(ft => ft.BaseName is "Attack of Opportunity" or "Reactive Strike" or "Opportunist"));
        
        // Reflexive Shield, modded feat, Lv 6
        // Added in ModLoader as a post action load.
        
        // Retaliating Rescue
        yield return new TrueFeat(
                ModData.FeatNames.RetaliatingRescue,
                6,
                "When an ally is in danger, you can hustle to reach them and punish the foe threatening them.",
                "Stride up to your Speed. You must end this movement adjacent to an ally who is within an enemy's reach. Then, you push your ally up to 5 feet (as normal for forced movement, this movement doesn't trigger reactions) and make a melee Strike against an enemy within your reach. If your ally was in that enemy's reach and your push moved them out of it, you gain a +2 circumstance bonus to your attack roll.",
                [ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    // TODO: Check tile.Neighbors for logic efficiency
                    CombatAction rescue = new CombatAction(
                            qfFeat.Owner,
                            new SideBySideIllustration(IllustrationName.QuickenTime, IllustrationName.KineticRam),
                            "Retaliating Rescue",
                            [ModData.ModTrait, ModData.Traits.Guardian],
                            """
                            {i}When an ally is in danger, you can hustle to reach them and punish the foe threatening them.{/i}

                            Stride up to your Speed. You must end this movement adjacent to an ally who is within an enemy's reach. Then, you push your ally up to 5 feet (as normal for forced movement, this movement doesn't trigger reactions) and make a melee Strike against an enemy within your reach. If your ally was in that enemy's reach and your push moved them out of it, you gain a +2 circumstance bonus to your attack roll.
                            """,
                            Target.Tile(
                                    (self, t) =>
                                    {
                                        // Must be free to me
                                        if (!t.LooksFreeTo(self))
                                            return false;
                                        
                                        // Must have allies in enemy reach
                                        if (GetAlliesInEnemyReach(self) is not { } alliesInEnemyReach
                                            || alliesInEnemyReach.Count == 0)
                                            return false;
                                        
                                        // Must be adjacent to an ally.
                                        if (!alliesInEnemyReach.Any(ally =>
                                                ally.Space.GetNeighbours().Contains(t)))
                                            return false;

                                        return true;
                                    },
                                    (_,_) => int.MinValue)
                                .WithPathfindingGuidelines(cr =>
                                    new PathfindingDescription() { Squares = cr.Speed }))
                        .WithActionCost(2)
                        .WithShortDescription("Stride to an ally in reach of an enemy, push your ally, and Strike.")
                        .WithEffectOnChosenTargets(async (action, caster, targets) =>
                        {
                            // Enact stride towards preselected tile
                            //caster.MoveToUsingEarlierFloodfill()
                            if (!await caster.StrideAsync("Choose where to Stride with Retaliating Rescue. (1/2)", strideTowards: targets.ChosenTile))
                                action.RevertRequested = true;
                            
                            // Choose an adjacent ally to push
                            Creature? pushedAlly = await caster.Battle.AskToChooseACreature(
                                caster,
                                caster.Battle.AllCreatures
                                    .Where(cr => cr.FriendOfAndNotSelf(caster) && cr.IsAdjacentTo(caster)),
                                IllustrationName.Shove,
                                "Choose an ally to push 5 feet. For each enemy your ally is no longer in reach of, your attack gains a +2 circumstance bonus.",
                                "Push 5 feet directly away.",
                                "Abort and convert to simple Stride");

                            if (pushedAlly == null)
                            {
                                caster.Battle.Log("No ally pushed. Retaliating Rescue was converted to a simple Stride.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                                return;
                            }
                            
                            // Record who the ally was adjacent to before the push
                            List<Creature> enemiesInReachOfAlly = caster.Battle.AllCreatures
                                .Where(enemy =>
                                    enemy.EnemyOf(caster)
                                    && OneIsInReachOfTwo(pushedAlly, enemy))
                                .ToList();

                            if (enemiesInReachOfAlly.Count == 0)
                            {
                                caster.Battle.Log("Ally is not within reach of any enemies. Retaliating Rescue was converted to a simple Stride.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                                return;
                            }

                            // Push ally
                            Sfxs.Play(SfxName.Shove);
                            pushedAlly.Overhead("*Pushed*", Color.Black);
                            await caster.PushCreature(pushedAlly, 1);
                            
                            // Record who they are no longer adjacent to
                            List<Creature> bonusAgainstWho = enemiesInReachOfAlly
                                .Where(enemy =>
                                    !OneIsInReachOfTwo(pushedAlly, enemy))
                                .ToList();

                            string pushLog = caster + " pushes " + pushedAlly + " 5 feet";
                            if (bonusAgainstWho.Count > 0)
                                pushLog += " and gains a +2 circumstance bonus to Strike " + S.ConstructOrList(bonusAgainstWho.Select(cr => cr.Name), "and");
                            pushLog += ".";
                            caster.Battle.Log(pushLog);
                            
                            // Apply bonus
                            QEffect bonusAgainst = new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                            {
                                Name = "[RETALIATING RESCUE BONUS]",
                                BonusToAttackRolls = (qfBonus, actionStrike, target) =>
                                    actionStrike.HasTrait(Trait.Attack)
                                    && actionStrike.HasTrait(Trait.Strike)
                                    && target != null
                                    && bonusAgainstWho.Contains(target)
                                        ? new Bonus(2, BonusType.Circumstance, "Retaliating rescue")
                                        : null,
                            };
                            caster.AddQEffect(bonusAgainst);
                            
                            // Make Strike
                            await CommonCombatActions.StrikeCreature(caster, null, false, "Pass", true);

                            // Remove bonus
                            bonusAgainst.ExpiresAt = ExpirationCondition.Immediately;
                        });

                    return (ActionPossibility)rescue;
                };
                return;

                bool OneIsInReachOfTwo(Creature cr1, Creature cr2)
                {
                    return cr2.DistanceToWith10FeetException(cr1) <= cr2.Space.ActualReach;
                }
                
                List<Creature> GetAlliesInEnemyReach(Creature self, List<Creature>? enemies = null)
                {
                    enemies ??= self.Battle.AllCreatures
                        .Where(self.EnemyOf)
                        .ToList();
                    return self.Battle.AllCreatures
                        .Where(self.FriendOfAndNotSelf)
                        .Where(ally =>
                            enemies.Any(enemy => OneIsInReachOfTwo(ally, enemy)))
                        .ToList();
                }
            });
        
        // Ring their Bell
        yield return new TrueFeat(
                ModData.FeatNames.RingTheirBell,
                6,
                "Using your armor, you pummel a foe that isn't focused on you in the head or face to stagger them.",
                $$"""
                {b}Requirements{/b} You are wearing medium or heavy armor, and your {{ModData.FeatNames.Taunt.ToLink("taunted enemy")}} is off-guard because it didn't target you or include you in an area effect.

                Make a Strike with a fist or kick against your taunted enemy. If the Strike hits and deals damage, the creature must attempt a Fortitude save against your class DC {i}(this is an incapacitation effect){/i}.
                """
                + S.FourDegreesOfSuccess(
                        "The creature is unaffected.",
                        "The creature is stunned 1.",
                        "The creature is stunned 2.",
                        "The creature is stunned 3."),
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + " [flourish] Strike a taunted enemy who ignored your Taunt, stunning them.";
                qfFeat.Id = QEffectId.AlwaysShowedUnarmedStrike;
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (item.ItemName is not ItemName.Fist)
                        return null;
                    
                    StrikeModifiers newMods = new StrikeModifiers() { };
                    CombatAction ringTheirBell = qfFeat.Owner
                        .CreateStrike(item, -1, newMods)
                        .WithName("Ring their Bell")
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            newMods,
                            prologueText: "{b}Requirements{/b} You are wearing medium or heavy armor, and your taunted enemy is off-guard because it didn't target you or include you in an area effect.\n",
                            additionalSuccessText: "If you deal damage, the creature must attempt a Fortitude save against your class DC {i}(this is an incapacitation effect){/i}.",
                            additionalCriticalSuccessText: "As success.",
                            additionalAftertext: S.FourDegreesOfSuccess(
                                "The creature is unaffected.",
                                "The creature is stunned 1.",
                                "The creature is stunned 2.",
                                "The creature is stunned 3.")))
                        //.WithExtraTrait(Trait.Basic)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(ModData.Traits.Guardian)
                        .WithHitAndDealDamage(async (caster, action, target) =>
                        {
                            action.Traits.Add(Trait.Incapacitation);
                            CheckResult result = await CommonSpellEffects.RollSavingThrowAsync(
                                target,
                                action,
                                Defense.Fortitude,
                                caster.ClassDC(ModData.Traits.Guardian));
                            action.Traits.Remove(Trait.Incapacitation);
                            int? value = null;
                            switch (result)
                            {
                                case CheckResult.Success:
                                    value = 1;
                                    goto case CheckResult.CriticalFailure;
                                case CheckResult.Failure:
                                    value = 2;
                                    goto case CheckResult.CriticalFailure;
                                case CheckResult.CriticalFailure:
                                    value ??= 3;
                                    QEffect stunned = QEffect.Stunned((int)value);
                                    target.AddQEffect(stunned);
                                    break;
                            }
                        });
                    ringTheirBell.Traits = new Traits([ModData.ModTrait, ..ringTheirBell.Traits.ToList()], ringTheirBell);
                    ringTheirBell.Illustration = new SideBySideIllustration(
                        ringTheirBell.Illustration, IllustrationName.Stunned);
                    ((CreatureTarget)ringTheirBell.Target) // Strikes always make CreatureTargets
                        .WithAdditionalConditionOnTargetCreature(
                            ModData.CommonRequirements.MustWearMediumOrHeavyArmor())
                        .WithAdditionalConditionOnTargetCreature(
                            ModData.CommonRequirements.IsMyTauntedEnemy())
                        .WithAdditionalConditionOnTargetCreature(
                            ModData.CommonRequirements.OffGuardDueToMyTaunt());
                    
                    return ringTheirBell;
                };
            });
        
        // Stomp Ground
        yield return new TrueFeat(
                ModData.FeatNames.StompGround,
                6,
                "You bring your booted foot down on the ground with enough force to rattle your foes.",
                "Each creature in a 5-foot emanation must attempt a Reflex saving throw against your class DC."+S.FourDegreesOfSuccess(
                    "The creature is unaffected.",
                    "The creature is off-guard until the end of your turn.",
                    "The creature is knocked prone.",
                    "The creature is knocked prone and takes 1d6 bludgeoning damage from the fall."),
                [ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction stomp = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.StompGround,
                            "Stomp Ground",
                            [ModData.ModTrait, ModData.Traits.Guardian],
                            """
                            {i}You bring your booted foot down on the ground with enough force to rattle your foes.{/i}

                            Each creature in a 5-foot emanation must attempt a Reflex saving throw against your class DC.
                            """+S.FourDegreesOfSuccess(
                                "The creature is unaffected.",
                                "The creature is off-guard until the end of your turn.",
                                "The creature is knocked prone.",
                                "The creature is knocked prone and takes 1d6 bludgeoning damage from the fall."),
                            Target.SelfExcludingEmanation(1))
                        .WithActionCost(2)
                        .WithShortDescription("Creatures within 5 feet must make a Reflex save against becoming off-guard or falling prone.")
                        .WithSoundEffect(SfxName.ElementalBlastEarth)
                        .WithSavingThrow(new SavingThrow(
                            Defense.Reflex,
                            cr => cr!.ClassDC(ModData.Traits.Guardian)))
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            switch (result)
                            {
                                case CheckResult.CriticalSuccess:
                                    return;
                                case CheckResult.Success:
                                    QEffect stompSuccess = QEffect.FlatFooted("Stomp ground");
                                    stompSuccess.ExpiresAt = ExpirationCondition.ExpiresAtEndOfSourcesTurn;
                                    stompSuccess.Source = caster;
                                    target.AddQEffect(stompSuccess);
                                    return;
                                case CheckResult.Failure:
                                    await target.FallProne();
                                    return;
                                case CheckResult.CriticalFailure:
                                    await target.FallProne();
                                    await CommonSpellEffects.DealDirectDamage(
                                        action,
                                        DiceFormula.FromText("1d6", "Stomp ground (critical failure)"),
                                        target,
                                        result, // CritFail or CritSuccess works.
                                        DamageKind.Bludgeoning);
                                    return;
                            }
                        });
                    
                    return (ActionPossibility)stomp;
                };
            });
        
        #endregion
        
        #region Level 8
        
        // Group Taunt
        yield return new TrueFeat(
            ModData.FeatNames.GroupTaunt,
            8,
            "Your taunts draw the attention of multiple enemies at once.",
            $"When you use {ModData.FeatNames.Taunt.ToLink("Taunt")}, you can choose up to three targets within range, and you can have up to three taunted enemies at a time. Each time you Taunt, you can choose which enemies remain taunted and which the effect ends for. You must remain at or below this limit.",
            [ModData.Traits.Guardian]);
        
        // Juggernaut Charge
        yield return new TrueFeat(
                ModData.FeatNames.JuggernautCharge,
                8,
                "As you move forward in a rush, you put the weight of your armor behind an attack that can drag a foe with you.",
                """
                {b}Requirements{/b} You are wearing medium or heavy armor.

                You Stride. If you end your movement within melee reach of at least one enemy, you can make a melee Strike against that enemy, then Stride again.

                If your Strike hit and dealt damage, that enemy is pulled with you and follows the same path as your second Stride.
                """,
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction jugCharge = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(IllustrationName.FleetStep, IllustrationName.Grapple),
                            "Juggernaut Charge",
                            [ModData.ModTrait, Trait.Flourish, ModData.Traits.Guardian],
                            """
                            {i}As you move forward in a rush, you put the weight of your armor behind an attack that can drag a foe with you.{/i}

                            {b}Requirements{/b} You are wearing medium or heavy armor.

                            You Stride. If you end your movement within melee reach of at least one enemy, you can make a melee Strike against that enemy, then Stride again.

                            If your Strike hit and dealt damage, that enemy is pulled with you and follows the same path as your second Stride.
                            """,
                            Target.Self()
                                .WithAdditionalRestriction(cr =>
                                    ModData.CommonRequirements.MustWearMediumOrHeavyArmor()
                                        .Satisfied(cr, cr).UnusableReason))
                        .WithActionCost(2)
                        .WithShortDescription("Stride, make a melee Strike, and Stride again. On a hit, drag the target with you.")
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            // (1/3) Stride
                            if (!await self.StrideAsync("Choose where to Stride with Juggernaut Charge, or right-click to cancel. You should end your movement within melee reach of an enemy. (1/3)", allowCancel:true))
                            {
                                Revert(action, null);
                                return;
                            }
                            
                            // (2/3) Strike
                            List<Option> options = [];
                            Creature? chosenCreature = null;
                            int hpBefore = -1;
                            foreach (Item wep in self.MeleeWeapons)
                                GameLoop.AddDirectUsageOnCreatureOptions(
                                    self.CreateStrike(wep).WithActionCost(0),
                                    options, true);

                            if (options.Count == 0)
                            {
                                Revert(action, "a simple Stride", 1);
                                return;
                            }
                            
                            Option chosenOption;
                            if (options.Count > 1) // If lots of options, ask to pick one
                            {
                                options.Add(new CancelOption(true));
                                options.Add(new PassViaButtonOption("Abort and convert to simple Stride"));
                                chosenOption = (await self.Battle.SendRequest(
                                    new AdvancedRequest(self, "Choose a creature to Strike.", options)
                                    {
                                        TopBarText = "Choose a creature to Strike or right-click to cancel. (2/3)",
                                        TopBarIcon = IllustrationName.StarHit,
                                    })).ChosenOption;
                            }
                            else
                                chosenOption = options[0];

                            switch (chosenOption)
                            {
                                case CreatureOption crOption:
                                    chosenCreature = crOption.Creature;
                                    hpBefore = chosenCreature.HP;
                                    break;
                                case PassViaButtonOption:
                                case CancelOption:
                                    Revert(action, "a simple Stride", 1);
                                    return;
                            }

                            await chosenOption.Action();
                            
                            if (chosenCreature == null) // Didn't strike
                            {
                                Revert(action, "a simple Stride", 1);
                                return;
                            }

                            // (3/3) Stride 2 (Electric Boogaloo)
                            IList<Tile> longestPath = [];
                            Tile startTile = self.Space.TopLeftTile;
                            QEffect dragBehavior = new QEffect()
                            {
                                Name = "[JUGGERNAUT DRAG]",
                                ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn, // Fallback
                                // Capture next longest move path in case it updates multiple times.
                                StateCheck = qfDrag =>
                                {
                                    if (qfDrag.Owner.AnimationData.LongMovement?.Path is { Count: > 0 } path
                                        && path.Count > longestPath.Count)
                                        longestPath = path;
                                },
                                // Move the target after we finish moving
                                AfterYouTakeAction = async (qfDrag, actionStride) =>
                                {
                                    if (actionStride.ActionId is not ActionId.Stride
                                            and not ActionId.StepByStepStride
                                        || longestPath.Count == 0)
                                        return;

                                    longestPath.Insert(0, startTile);
                                    foreach (Tile tile in self.Space.Tiles)
                                        longestPath.Remove(tile);
                                    
                                    chosenCreature.AnimationData.LongMovement = new LongMovement(
                                        chosenCreature,
                                        longestPath,
                                        new MovementStyle()
                                        {
                                            Shifting = true,
                                            ForcedMovement = true,
                                            IgnoresUnevenTerrain = true,
                                            MaximumSquares = 100,
                                            Insubstantial = true,
                                        }, null
                                        /*CombatAction.CreateSimple(chosenCreature, "Stride")
                                            .WithExtraTrait(Trait.Move)
                                            .WithActionId(ActionId.Stride)*/);
                                    
                                    await chosenCreature.AnimationData.LongMovement.Execute();
                                },
                            };
                            
                            if (chosenCreature.HP != hpBefore) // If dealt damage, then also drag
                                self.AddQEffect(dragBehavior);
                            
                            if (!await self.StrideAsync("Choose where to Stride with Juggernaut Charge. The target will be pulled along your movement path. (3/3)", allowPass: true))
                            /*if (!await self.Battle.GameLoop.FullCast(
                                    CommonCombatActions.StepByStepStride(self)
                                        .WithActionCost(0)))*/
                            {
                                self.Battle.Log("Juggernaut Charge was converted to a simple Stride and Strike");
                                action.Traits.Remove(Trait.Flourish);
                            }
                            
                            dragBehavior.ExpiresAt = ExpirationCondition.Immediately;
                        });

                    return new ActionPossibility(jugCharge);

                    void Revert(CombatAction act, string? toWhat, int cost = 0)
                    {
                        if (toWhat != null)
                            act.Owner.Battle.Log($"Juggernaut Charge was converted to {toWhat}.");
                        act.SpentActions = cost;
                        act.RevertRequested = true;
                    }
                };
            });
        
        // Mighty Bulwark
        Feat mightyBulwark = (AllFeats.GetFeatByFeatName(FeatName.MightyBulwark) as TrueFeat)!
            .WithLevel(8);
        LevelPrerequisite levelReq = mightyBulwark.Prerequisites.OfType<LevelPrerequisite>().First();
        mightyBulwark.Prerequisites.Remove(levelReq);
        mightyBulwark.Prerequisites.Add(new LevelPrerequisite(8));
        mightyBulwark.Traits.Add(ModData.Traits.Guardian);
        mightyBulwark.Prerequisites.Add(new ClassPrerequisite([ModData.Traits.Guardian]));
        
        // Repositioning Block ????? More Basic Actions??? Hard-coded?
        
        // Shield from Arrows
        
        // Shield Wallop
        yield return new TrueFeat(
                ModData.FeatNames.ShieldWallop,
                8,
                "Attacks with your shield knock the sense out of your foes.",
                """
                {b}Requirements{/b} You are wielding a shield.

                Make a shield Strike. If you hit and deal damage, the target is stupefied 1 until the start of your next turn (stupefied 2 on a critical hit).

                If your shield is a tower shield, fortress shield, or another shield that grants a higher circumstance bonus to AC when you Take Cover behind it, the creature is instead stupefied 2 if you hit and deal damage to it (stupefied 3 on a critical hit).
                """,
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + " [flourish] Make a shield Strike that stupefies the target.";
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Shield))
                        return null;

                    int baseValue = item.HasTrait(MoreShields.ModData.Traits.CoverShield)
                        ? 2
                        : 1;
                    
                    StrikeModifiers newMods = new StrikeModifiers() { };
                    CombatAction wallop = qfFeat.Owner
                        .CreateStrike(item, -1, newMods)
                        .WithName("Shield Wallop")
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            newMods,
                            additionalSuccessText: $"The target is stupefied {baseValue}.",
                            additionalCriticalSuccessText: $"The target is stupefied {baseValue+1}."))
                        //.WithExtraTrait(Trait.Basic)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(ModData.Traits.Guardian)
                        .WithActionCost(1)
                        .WithHitAndDealDamage(async (caster, action, target) =>
                        {
                            if (action.CheckResult >= CheckResult.Success)
                                target.AddQEffect(
                                    QEffect.Stupefied(action.CheckResult == CheckResult.CriticalSuccess ? baseValue+1 : baseValue)
                                        .WithExpirationAtStartOfSourcesTurn(caster, 1));
                        });
                    wallop.Traits = new Traits([ModData.Traits.ModName, ..wallop.Traits.ToList()], wallop);
                    wallop.Illustration = new SideBySideIllustration(
                        item.Illustration,
                        IllustrationName.BrainDrain);
                    
                    return wallop;
                };
            });
        
        #endregion
        
        #region Level 10
        
        // Belly Flop
        
        // Get Behind Me!
        // DOC: You can choose not to move the ally further away.
        yield return new TrueFeat(
                ModData.FeatNames.GetBehindMe,
                10,
                "When saving your allies from harm, you push them behind you to better protect them.",
                $"When you use {ModData.FeatNames.InterceptAttack.ToLink("Intercept Attack")} to protect an ally, you can move that ally up to 10 feet to an unoccupied space that's within your reach. This movement doesn't trigger reactions.",
                [ModData.Traits.Guardian])
            .WithPermanentQEffect(
                "When you Intercept Attack, you can move the triggering ally up to 10 feet.",
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.ActionId != ModData.ActionIds.InterceptAttack)
                            return;
                        Creature ally = action.ChosenTargets.ChosenCreature!;
                        if (await qfThis.Owner.Battle.AskToChooseATile(
                                qfThis.Owner,
                                qfThis.Owner.Battle.Map.AllTiles
                                    .Where(tile =>
                                        tile.LooksFreeTo(ally)
                                        && tile.DistanceTo(qfThis.Owner) <= qfThis.Owner.Space.NaturalReach
                                        && ally.DistanceTo(tile) <= 2),
                                ModData.Illustrations.GetBehindMe,
                                $"Choose a space to move {ally.ToColoredName()} to.",
                                "Lorem ipsum.",
                                true, true,
                                ally)
                            is not { } chosenTile)
                            return;
                        await ally.MoveTo(chosenTile, null, new MovementStyle()
                        {
                            ForcedMovement = true,
                            IgnoresUnevenTerrain = true,
                            MaximumSquares = 99,
                            Shifting = true,
                            ShortestPath = true,
                        });
                    };
                });
        
        // Momentum Strike
        
        // Shield Salvation
        
        // Sure-Footed
        
        // Tough Cookie
        yield return new TrueFeat(
                ModData.FeatNames.ToughCookie,
                10,
                "Though you've taken a lot of punishment, you aren't easily brought down.",
                """
                {b}Frequency{/b} once per day
                {b}Requirements{/b} Your current Hit Points are at half your maximum or less.
                
                You gain a number of temporary Hit Points equal to half your maximum Hit Points.
                """,
                [ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + $" {"(Once per day) If you're at 1/2 max HP or less, gain that much temp HP".WithTag(qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.TOUGH_COOKIE) ? "strike" : null)}.";
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions
                        .Contains(ModData.PersistentActions.TOUGH_COOKIE))
                        return null;

                    return (ActionPossibility) new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.ToughCookie,
                            "Tough Cookie",
                            [ModData.ModTrait, ModData.Traits.Guardian, Trait.Basic],
                            null!,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                    self.HP > (self.MaxHP / 2)
                                        ? "Not at 1/2 your HP"
                                        : null))
                        .WithDescription(
                            "Though you've taken a lot of punishment, you aren't easily brought down.",
                            """
                            {b}Frequency{/b} once per day
                            {b}Requirements{/b} Your current Hit Points are at half your maximum or less.

                            You gain a number of temporary Hit Points equal to half your maximum Hit Points.
                            """)
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.MinorHealing)
                        .WithEffectOnSelf(self =>
                        {
                            self.GainTemporaryHP(self.MaxHP / 2);
                            self.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.TOUGH_COOKIE);
                        });
                };
            });

        #endregion

        #region Level 12

        // Armor Break

        // Armored Counterattack
        yield return new TrueFeat(
                ModData.FeatNames.ArmoredCounterattack,
                12,
                "With the might of your armor behind you, you hit back at a foe who would dare try to hurt your allies.",
                $$"""
                {b}Trigger{/b} You use {{ModData.FeatNames.InterceptAttack.ToLink("Intercept Attack")}} against a melee Strike and are adjacent to the creature that made the Strike.
                
                After Intercepting the Attack, make your own Strike against the triggering enemy. If your Strike hits, you {{ModData.FeatNames.Taunt.ToLink("Taunt")}} the target; this Taunt gains the visual trait.
                """,
                [ModData.Traits.Guardian])
            .WithActionCost(0)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + " After Intercepting a melee Strike from an adjacent attacker, Strike and visually Taunt them.";
                qfFeat.AfterYouTakeActionReaction = (qfThis, action) =>
                {
                    if (action.ActionId != ModData.ActionIds.InterceptAttack
                        || action.Tag is not DamageEvent dEvent
                        || dEvent.CombatAction?.Owner is not {} attacker
                        || !dEvent.CombatAction.Traits.Contains(Trait.Strike)
                        || !dEvent.CombatAction.Traits.Contains(Trait.Melee)
                        || !qfThis.Owner.IsAdjacentTo(attacker))
                        return null;

                    bool immuneToVisual = attacker.IsImmuneTo(Trait.Visual);
                    
                    CombatAction armedCount = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.ArmoredCounterattack,
                            "Armored Counterattack",
                            [ModData.ModTrait, ModData.Traits.Guardian],
                            """
                            {i}With the might of your armor behind you, you hit back at a foe who would dare try to hurt your allies.{/i}

                            {b}Trigger{/b} You use Intercept Attack against a melee Strike and are adjacent to the creature that made the Strike.

                            After Intercepting the Attack, make your own Strike against the triggering enemy. If your Strike hits, you Taunt the target; this Taunt gains the visual trait.
                            """,
                            Target.Self())
                        .WithActionCost(0)
                        .WithEffectOnEachTarget(async (armedCount, _, _, _) =>
                        {
                            // Custom overload
                            if (!await CommonCombatActions.StrikeCreature(
                                    qfThis.Owner,
                                    //isValidStrike
                                    strike => strike.HasTrait(Trait.Melee),
                                    //adjustStrike
                                    strike => strike
                                        .WithEffectOnEachTarget(async (_, striker, target2, result) =>
                                        {
                                            if (result > CheckResult.Failure)
                                                if (immuneToVisual)
                                                    striker.Battle.Log("Target is immune to visual Taunts.");
                                                else
                                                    await striker.Battle.GameLoop.FullCast(
                                                        GuardianClass
                                                            .CreateTaunt(striker, true, Trait.Visual)
                                                            .WithActionCost(0),
                                                        ChosenTargets.CreateSingleTarget(target2));
                                        })
                                        .WithExtraTrait(Trait.ReactiveAttack),
                                    // isValidTarget
                                    cr => cr == attacker,
                                    null, null, true, null))
                                armedCount.RevertRequested = true;
                        });
                    
                    ReactionOption reactOpt = ReactionOption.WrapFullcast(
                            armedCount,
                            $"Strike {attacker.ToColoredName()}, then {(immuneToVisual ? ("visually Taunt".WithTag("s") + " {Red}(immune to visual effects){/Red}") : "visually Taunt")} them on a hit.")
                        .WithIsFreeAction()
                        .WithTriggerReason(qfThis.Owner.ToColoredBoldedName() + " used Intercept Attack from an adjacent enemy's melee Strike.");

                    return reactOpt;
                };
            })
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.InterceptAttack),
                "You must have the Intercept Attack feature");

        // Devastating Shield Wallop

        // Paragon's Guard

        // Right Where You Want Them

        // Scattering Charge
        yield return new TrueFeat(
                ModData.FeatNames.ScatteringCharge,
                12,
                "You charge into a group of enemies to send them flying.",
                """
                    {b}Requirements{/b} You are wearing medium or heavy armor.

                    Stride up to your Speed. At the end of your movement, you can Shove up to three creatures within your reach. You don't need a hand free to do so. You attempt a separate Athletics check for each one; each attempt counts toward your multiple attack penalty, but the penalty doesn't increase until after you've made all the attempts. Regardless of your results, you can’t Stride to follow any of the targets.
                    """,
                    [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(3)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction scatter = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.ScatteringCharge,
                            "Scattering Charge",
                            [ModData.ModTrait, Trait.Flourish, ModData.Traits.Guardian],
                            null!,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                    CombatAction.CreateSimple(self, "Stride")
                                        .WithActionId(ActionId.Stride)
                                        .CanBeginToUse(self)
                                            ? null
                                            : "Can't Stride"))
                        .WithDescription(
                            "You charge into a group of enemies to send them flying.",
                            """
                            {b}Requirements{/b} You are wearing medium or heavy armor.

                            Stride up to your Speed. At the end of your movement, you can Shove up to three creatures within your reach. You don't need a hand free to do so. You attempt a separate Athletics check for each one; each attempt counts toward your multiple attack penalty, but the penalty doesn't increase until after you've made all the attempts. Regardless of your results, you can’t Stride to follow any of the targets.
                            """)
                        .WithShortDescription("Stride, then Shove up to three enemies.")
                        .WithActionCost(3)
                        .WithEffectOnEachTarget(async (action, caster, _, _) =>
                        {
                            if (!await caster.StrideOrStepAsync("Choose where to Stride with Scattering Charge. You should end your movement within your reach of multiple enemies.", allowCancel: true))
                                action.RevertRequested = true;
                            else
                            {
                                int mapBefore = caster.Actions.AttackedThisManyTimesThisTurn;
                                int mapCounting = mapBefore;
                                List<Creature> alreadyShoved = [];
                                QEffect preventStride = new QEffect()
                                {
                                    Name = "[SCATTERING CHARGE: PREVENT STRIDE]",
                                    PreventTakingAction = action2 =>
                                        action2.ActionId is ActionId.Stride
                                            ? "Scattering charge" : null,
                                };
                                caster.AddQEffect(preventStride);
                                for (int i = 0; i < 3; ++i)
                                {
                                    await caster.Battle.GameLoop.StateCheck();
                                    caster.Actions.AttackedThisManyTimesThisTurn = mapBefore;
                                    
                                    List<Option> options = [];
                                    foreach (CombatAction shove in CombatManeuverPossibilities.GetAllOptions(
                                                 CombatManeuverPossibilities.CreateShovePossibility(caster)))
                                    {
                                        // PETR: If this later causes bugs, it might be because of the addition of another requirement, or because the free hand requirement became a dedicated object
                                        ((CreatureTarget)shove.Target).CreatureTargetingRequirements.RemoveAll(req =>
                                            req is LegacyCreatureTargetingRequirement);
                                        ((CreatureTarget)shove.Target).WithAdditionalConditionOnTargetCreature((_,d) =>
                                            alreadyShoved.Contains(d) ? Usability.NotUsableOnThisCreature("Already Shoved") : Usability.Usable);
                                        shove
                                            .WithActionCost(0)
                                            .WithEffectOnChosenTargets(async (self, chosen) =>
                                            {
                                                if (chosen.ChosenCreature is not null)
                                                    alreadyShoved.Add(chosen.ChosenCreature);
                                            });
                                        GameLoop.AddDirectUsageOnCreatureOptions(shove, options);
                                    }
                                    
                                    if (options.Count <= 0)
                                        continue;
                                    
                                    Option chosenOption;
                                    if (options.Count >= 2 || i == 0)
                                    {
                                        if (i == 0)
                                            options.Add(new CancelOption(true));
                                        chosenOption = (await caster.Battle.SendRequest(new AdvancedRequest(caster, "Choose a creature to Shove.", options)
                                        {
                                            TopBarText = $"Choose a creature to Shove. ({i+1}/3)",
                                            TopBarIcon = IllustrationName.Shove
                                        })).ChosenOption;
                                    }
                                    else
                                        chosenOption = options[0];
                                    if (chosenOption is CancelOption)
                                    {
                                        action.RevertRequested = true;
                                        return;
                                    }
                                    await chosenOption.Action();
                                    ++mapCounting;
                                }
                                caster.Actions.AttackedThisManyTimesThisTurn = mapCounting;
                                preventStride.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        });

                    return new ActionPossibility(scatter);
                };
            });

        // Weakening Assault
        yield return new TrueFeat(
                ModData.FeatNames.WeakeningAssault,
                12,
                "With a barrage of blows, you diminish an enemy's strength.",
                $"Strike an enemy affected by your {ModData.FeatNames.Taunt.ToLink("Taunt")} twice. If either Strike hits, the target is enfeebled 1 (3 if both Strikes hit) until the beginning of your next turn.",
                [ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction assault = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.WeakeningAssault,
                            "Weakening Assault",
                            [ModData.ModTrait, ModData.Traits.Guardian],
                            null!,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                {
                                    bool usable = false;
                                    var tauntedReq = ModData.CommonRequirements.IsMyTauntedEnemy();
                                    var offGuardReq = ModData.CommonRequirements.OffGuardDueToMyTaunt();
                                    if (self.Battle.AllCreatures.Any(cr =>
                                            tauntedReq.Satisfied(self, cr)
                                            || offGuardReq.Satisfied(self, cr)))
                                        usable = true;
                                    return usable ? null : "No-one affected by my taunt";
                                }))
                        .WithActionCost(2)
                        .WithDescription(
                            "With a barrage of blows, you diminish an enemy's strength.",
                            $"Strike an enemy affected by your {ModData.FeatNames.Taunt.ToLink("Taunt")} twice. If either Strike hits, the target is enfeebled 1 until the beginning of your next turn. If both Strikes hit, the target is enfeebled 3 instead.")
                        .WithShortDescription("Strike an enemy affected by your Taunt twice and make them enfeebled 1 or 3.")
                        .WithEffectOnEachTarget(async (action, caster, _, _) =>
                        {
                            int hits = 0;
                            Creature? chosen = null;
                            Action<CombatAction> adjustStrike = strike =>
                            {
                                CreatureTargetingRequirement tauntedReq = ModData.CommonRequirements.IsMyTauntedEnemy();
                                CreatureTargetingRequirement offGuardReq = ModData.CommonRequirements.OffGuardDueToMyTaunt();
                                ((CreatureTarget)strike.Target).WithAdditionalConditionOnTargetCreature((a, d) =>
                                    tauntedReq.Satisfied(a, d)
                                    || offGuardReq.Satisfied(a, d)
                                        ? Usability.Usable
                                        : Usability.NotUsableOnThisCreature("Not affected by my taunt"));
                                strike.WithEffectOnEachTarget(async (_, caster2, target, result) =>
                                {
                                    chosen = target;
                                    if (result < CheckResult.Success)
                                        return;
                                    target.AddQEffect(QEffect.Enfeebled(hits > 0 ? 3 : 1)
                                        .WithExpirationAtStartOfSourcesTurn(caster2, 1));
                                    hits++;
                                });
                            };
                            if (!await CommonCombatActions.StrikeCreature(caster, null, adjustStrike, null, action.Illustration, "Choose a creature to Strike with Weakening Assault. (1/2)", true, "Cancel"))
                            {
                                action.RevertRequested = true;
                                return;
                            }
                            await CommonCombatActions.StrikeCreature(caster, null, adjustStrike, cr => cr == chosen, action.Illustration, "Choose a creature to Strike with Weakening Assault. (2/2)", true, "Pass");
                        });

                    return new ActionPossibility(assault);
                };
            });

        #endregion

        #region Level 14

        // Blanket Defense

        // Bloody Denial

        // Keep Up The Good Fight
        yield return new TrueFeat(
                ModData.FeatNames.KeepUpTheGoodFight,
                14,
                "Your commitment to protecting others keeps you going, even against insurmountable odds.",
                """
                {b}Frequency{/b} once per encounter
                {b}Trigger{/b} An enemy reduces you to 0 Hit Points but doesn't kill you.
                
                Instead of being knocked out, you're reduced to 1 Hit Point. You increase your wounded value by 1 and gain a number of temporary Hit Points equal to your level.
                """,
                [ModData.Traits.Guardian])
            .WithActionCost(-2)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.Tag = false; // Usable once per encounter
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b")
                    + $" (Once per encounter) When an enemy reduces you to 0 HP, you remain at 1 HP, gain 1 wounded, and gain {qfThis.Owner.Level.WithColor("Blue")} temp HP.".WithTag(qfThis.Tag is true ? "strike" : null);
                qfFeat.YouAreDealtLethalDamage = async (qfThis, attacker, dStuff, you) =>
                {
                    if (DamageWouldKillYou(dStuff, you)
                        || qfThis.Tag is false)
                        return null;
                    
                    int wounded = you.QEffects.FirstOrDefault(qf => qf.Id == QEffectId.Wounded)?.Value ?? 0;
                    if (!await you.Battle.AskToUseReaction(
                            you,
                            $$"""
                            {b}Keep up the Good Fight{/b} {icon:Reaction}
                            You're about to be reduced to 0 HP. Remain at 1 HP, become wounded {{(wounded + 1).ToString()}}, and gain {Blue}{{you.Level}}{/Blue} temp HP?
                            """,
                            ModData.Illustrations.KeepUpTheGoodFight,
                            [ModData.Traits.Guardian]))
                        return null;
                    you.Overhead(
                        "Keep up the Good Fight!!",
                        Color.Red,
                        you + " resists dying through {b}Keep up the Good Fight{/b} {icon:Reaction}!",
                        "Keep up the Good Fight {icon:Reaction}",
                        """
                        {i}Your commitment to protecting others keeps you going, even against insurmountable odds.{/i}
                        
                        {b}Frequency{/b} once per encounter
                        {b}Trigger{/b} An enemy reduces you to 0 Hit Points but doesn't kill you.

                        Instead of being knocked out, you're reduced to 1 Hit Point. You increase your wounded value by 1 and gain a number of temporary Hit Points equal to your level.
                        """);
                    you.IncreaseWounded();
                    qfThis.Tag = true;

                    return new SetToTargetNumberModification(you.HP - 1, "Keep up the Good Fight!!");
                };
                
                return;

                bool DamageWouldKillYou(DamageStuff dStuff, Creature you)
                {
                    // Massive Damage rule
                    if (dStuff.Amount >= you.MaxHPMinusDrained * 2)
                        return true;
                    // You would die if 1+Wounded is equal to Dying
                    if ((you.FindQEffect(QEffectId.Wounded)?.Value ?? 0) + 1 >= DeathRules.GetMaximumDying(you))
                        return true;
                    return false;
                }
            });

        // Opening Stance
        (AllFeats.GetFeatByFeatName(FeatName.StanceSavantFighter) as TrueFeat)!
            .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
        
        #endregion

        #region Level 16

        // Clang!

        // Clobber

        // Improved Reflexive Shield

        // Never!

        #endregion

        #region Level 18

        // Demolish Defenses

        // Perfect Protection

        // Quick Vengeance

        // Shield from Spells

        #endregion

        #region Level 20

        // Boundless Reprisals

        // Great Shield Mastery

        // Unyielding Force

        #endregion
    }
}