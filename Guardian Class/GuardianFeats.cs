using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
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
        // Add existing feats to Guardian
        FeatName[] accessibleFeats = [
            FeatName.ReactiveShield, // Lv 1
            FeatName.AggressiveBlock, // Lv 2
        ];
        foreach (FeatName ft in accessibleFeats)
            (AllFeats.GetFeatByFeatName(ft) as TrueFeat)!
                .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
        // Defensive Advance mod, Lv 1
        if (ModManager.TryParse("Defensive Advance", out FeatName defAdv))
            (AllFeats.GetFeatByFeatName(defAdv) as TrueFeat)!
                .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
        // Reflexive Shield, More Dedications, Lv 6
        if (ModManager.TryParse("MoreDedications.Class.Fighter.ReflexiveShield", out FeatName refShield))
            (AllFeats.GetFeatByFeatName(refShield) as TrueFeat)!
                .WithAllowsForAdditionalClassTrait(ModData.Traits.Guardian);
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
                    $"Choose {ModLoader.GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"} as your charge.")
                .WithRulesTextCreator(_ =>
                    $"Your Taunt's penalty will increase to -2 against {ModLoader.GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"}.")
                .WithIllustrationCreator(_ =>
                    ModLoader.GetCharacterSheetFromPartyMember(index)?.Illustration ?? ModData.Illustrations.Taunt)
                .WithTag(i)
                .WithPermanentQEffect(
                    $"The penalty for Taunt increases to -2 against {{Blue}}{ModLoader.GetCharacterSheetFromPartyMember(index)?.Name ?? "a chosen ally"}{{/Blue}}.",
                    qfFeat =>
                    {
                        qfFeat.StartOfCombat = async qfThis =>
                        {
                            if (ModLoader.GetCharacterSheetFromPartyMember(index) is {} hero
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
                    ModLoader.GetCharacterSheetFromPartyMember(index) != values.Sheet,
                    "Can't select yourself");
            ModManager.AddFeat(chargeChoice);
        }
        yield return new TrueFeat(
                ModData.FeatNames.Bodyguard,
                1,
                "You swear a vow to protect one of your allies at all costs, regardless of the risk this might pose to you.",
                "Choose one of your allies as your charge. When you Taunt, the penalty your taunted enemy takes increases to –2 against your charge.\n\n{b}Precombat preparations:{/b} You can choose which ally is your charge at any time outside combat.",
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
        // Larger Than Life?????????
        // Long-Distance Taunt
        yield return new TrueFeat(
            ModData.FeatNames.LongDistanceTaunt,
            1,
            "You can draw the wrath of your foes even at a great distance.",
            "When you use Taunt, you can choose a target within 120 feet.",
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
                    "The target is off-guard against melee attacks you attempt against it until the end of your next turn",
                    "The target is off-guard against the next melee attack you attempt against it before the end of your current turn.",
                    null,
                    "You are off-guard against melee attacks the target attempts against you until the end of your next turn."),
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    ModLoader.DisplaysAsOffenseAction(
                        qfFeat,
                        "Shoulder Check",
                        "Make a fist Strike that can make a foe off-guard.");
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        if (!item.HasTrait(Trait.Fist))
                            return null;

                        CombatAction sCheck = qfFeat.Owner.CreateStrike(item)
                            .WithName("Shoulder Check")
                            .WithExtraTrait(Trait.Basic)
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
                        sCheck.Description = StrikeRules.CreateBasicStrikeDescription4(
                            sCheck.StrikeModifiers,
                            additionalCriticalSuccessText: "The target is off-guard to your melee attacks until the end of your next turn",
                            additionalSuccessText: "The target is off-guard to your next melee attack this turn.",
                            additionalCriticalFailureText: "You are off-guard to the target's melee attacks until the end of your next turn.");
                        sCheck.Target = (sCheck.Target as CreatureTarget)!
                            .WithAdditionalConditionOnTargetCreature(
                                ModData.CommonRequirements.WearingMediumOrHeavyArmor());

                        return sCheck;
                    };
                });
        #endregion
        
        #region Level 2
        // Covering Stance
        // Hampering Stance
        // Phalanx Formation
        // Raise Haft ???
        // Shield Your Eyes (useless?)
        // Shielding Taunt
        yield return new TrueFeat(
                ModData.FeatNames.ShieldingTaunt,
                2,
                "By banging loudly on your shield, you get the attention of even the most stubborn of foes.",
                "Raise a Shield, and then Taunt a creature. Your Taunt gains the auditory trait.",
                [Trait.Flourish, ModData.Traits.Guardian, MoreShields.ModData.Traits.ShieldActionFeat])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Raise a Shield and make an auditory Taunt.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.Name != "Raise shield")
                            return null;
                        
                        Creature guardian = qfFeat.Owner;

                        if (MoreShields.NewShields.GetAvailableShields(guardian) is not { } shields)
                            return null;
                        if (shields.Count == 0)
                            return null;
                        if (shields.MaxBy(MoreShields.NewShields.GetShieldAC) is not { } shield)
                            return null;
                        
                        // Used for targeting logic
                        CombatAction aTaunt = GuardianClass.CreateTaunt(guardian, true, Trait.Auditory)
                            .WithActionCost(0);
                        
                        CombatAction shieldTaunt = new CombatAction(
                                qfFeat.Owner,
                                new SideBySideIllustration(shield.Illustration, ModData.Illustrations.Taunt),
                                "Shielding Taunt",
                                [Trait.Basic, Trait.DoNotShowOverheadOfActionName, Trait.Flourish, ModData.Traits.Guardian],
                                "{i}By banging loudly on your shield, you get the attention of even the most stubborn of foes.{/i}\n\nRaise a Shield, and then Taunt a creature. Your Taunt gains the auditory trait.",
                                aTaunt.Target)
                            .WithActionCost(1)
                            .WithEffectOnEachTarget(async (action, caster, target, result) =>
                            {
                                // Raise a shield
                                Possibilities possibilities = caster.Possibilities.Filter(ap =>
                                {
                                    if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                                        return false;
                                    ap.CombatAction.ActionCost = 0;
                                    ap.RecalculateUsability();
                                    return true;
                                });
                                List<Option> actions = await caster.Battle.GameLoop.CreateActions(
                                    caster,
                                    possibilities,
                                    null);
                                caster.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                                await caster.Battle.GameLoop.OfferOptions(caster, actions, true);
                                
                                // Used for actual execution
                                // Not doing it twice results in usage errors
                                CombatAction aTaunt2 = GuardianClass.CreateTaunt(guardian, true, Trait.Auditory)
                                    .WithActionCost(0);
                                await caster.Battle.GameLoop.FullCast(aTaunt2, ChosenTargets.CreateSingleTarget(target));
                            });

                        return (ActionPossibility)shieldTaunt;
                    };
                });
        // Taunting Strike
        yield return new TrueFeat(
                ModData.FeatNames.TauntingStrike,
                2,
                "The force of your blow causes your enemy to focus their attention on you.",
                "Make a Strike. Regardless of whether the Strike hits, you Taunt the target. Your Taunt gains the visual trait.",
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    ModLoader.DisplaysAsOffenseAction(
                        qfFeat,
                        "Taunting Strike",
                        "Make a Strike. Then make a visual Taunt.");
                    // The actual action
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        CombatAction tauntingStrike = qfFeat.Owner.CreateStrike(item)
                            .WithExtraTrait(Trait.Flourish)
                            .WithExtraTrait(ModData.Traits.Guardian)
                            .WithExtraTrait(Trait.Basic)
                            .WithEffectOnEachTarget(async (action, caster, target, result) =>
                            {
                                CombatAction taunt = GuardianClass.CreateTaunt(caster, true, Trait.Visual)
                                    .WithActionCost(0);
                                await caster.Battle.GameLoop.FullCast(taunt, ChosenTargets.CreateSingleTarget(target));
                            });
                        tauntingStrike.Name = "Taunting Strike";
                        tauntingStrike.Illustration = new SideBySideIllustration(
                            item.Illustration,
                            ModData.Illustrations.Taunt);
                        tauntingStrike.Description = StrikeRules.CreateBasicStrikeDescription4(
                            tauntingStrike.StrikeModifiers,
                            additionalAftertext: "Make a visual Taunt against the Strike's target.");
                        (tauntingStrike.Target as CreatureTarget)!
                            .WithAdditionalConditionOnTargetCreature((a, d) =>
                                d.IsImmuneTo(Trait.Visual)
                                    ? Usability.NotUsableOnThisCreature("Immune to visual")
                                    : Usability.Usable);
                        return tauntingStrike;
                    };
                });
        #endregion
        
        #region Level 4
        // Area Armor
        // Armored Courage
        yield return new TrueFeat(
                ModData.FeatNames.ArmoredCourage,
                4,
                "You take comfort in the safety of your armor.",
                "{b}Requirements{/b} You are wearing medium or heavy armor.\n{b}Frequency{/b} once per encounter\n\nYou gain a number of temporary Hit Points equal to your level, and you reduce your frightened condition value by 1.",
                [ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Once per encounter, gain temp HP equal to your level, and reduce your frightened by 1.",
                qfFeat =>
                {
                    qfFeat.Tag = false;
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Tag is true)
                            return null;
                        CombatAction courage = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.ArmoredCourage,
                                "Armored Courage",
                                [Trait.Basic, ModData.Traits.Guardian],
                                "{i}You take comfort in the safety of your armor.{/i}\n\n{b}Requirements{/b} You are wearing medium or heavy armor.\n{b}Frequency{/b} once per encounter\n\nYou gain {Blue}"+qfThis.Owner.Level+"{/Blue} temporary Hit Points. Reduce your frightened condition value by 1.",
                                Target.Self()
                                    .WithAdditionalRestriction(cr =>
                                        ModData.CommonRequirements.WearingMediumOrHeavyArmor()
                                            .Satisfied(cr, cr).UnusableReason))
                            .WithSoundEffect(SfxName.MinorAbjuration)
                            .WithEffectOnSelf(async self =>
                            {
                                qfThis.Tag = true;
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
                "You can use "+ModData.Tooltips.ActionInterceptAttack("Intercept Attack {icon:Reaction}")+" when an ally would take acid, cold, electricity, fire, or sonic damage, not only when they would take physical damage.",
                [ModData.Traits.Guardian])
            .WithPermanentQEffect(
                "You can use Intercept Attack to take energy damage, not just physical damage.",
                qfFeat => {})
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.InterceptAttack),
                "You must have the guardian's Intercept Attack feature.");
        // Flying Tackle
        // Not So Fast!
        // Proud Nail
        yield return new TrueFeat(
                ModData.FeatNames.ProudNail,
                4,
                "When a foe ignores your taunts, you make them pay.",
                "{b}Requirements{/b} Your taunted enemy is off-guard because it didn't target you or include you in an area effect.\n\nMake a melee Strike against your taunted enemy. If this Strike hits, you deal an extra die of weapon damage. If you're at least 10th level, increase this to two extra dice, and if you're at least 18th level, increase it to three extra dice.",
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    ModLoader.DisplaysAsOffenseAction(
                        qfFeat,
                        "Proud Nail",
                        "Strike a foe who is off-guard due to ignoring your Taunt, dealing extra damage.");
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        int lvl = qfFeat.Owner.Level;
                        
                        CombatAction proudNail = qfFeat.Owner.CreateStrike(
                                item,
                                -1,
                                new StrikeModifiers()
                                {
                                    AdditionalWeaponDamageDice = lvl >= 18 ? 3 : lvl >= 10 ? 2 : 1,
                                })
                            .WithName("Proud Nail")
                            .WithExtraTrait(Trait.Basic)
                            .WithExtraTrait(Trait.Flourish);
                        proudNail.Illustration = new SideBySideIllustration(
                            proudNail.Illustration,
                            IllustrationName.StarHit);
                        proudNail.Description = StrikeRules.CreateBasicStrikeDescription4(
                            proudNail.StrikeModifiers,
                            prologueText: "{b}Requirements{/b} Your taunted enemy is off-guard because it didn't target you or include you in an area effect.\n");
                        (proudNail.Target as CreatureTarget)! // Strikes always make CreatureTargets
                            .WithAdditionalConditionOnTargetCreature((a, d) => 
                                d.QEffects.Any(qf => qf.Key == ModData.CommonQFKeys.OffGuardDueToTaunt+a.Name)
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("Hasn't ignored your Taunt"));
                        
                        return proudNail;
                    };
                });
        // Shielded Attrition <---- High priority. QEffectId.IgnoreAoOWhenMoving.
        #endregion
        
        #region Level 6
        // Disarming Intercept
        // Guarded Advance
        yield return new TrueFeat(
                ModData.FeatNames.GuardedAdvance,
                6,
                "You slowly advance on the battlefield, taking utmost caution.",
                "You Raise a Shield and Step twice.",
                [ModData.Traits.Guardian, MoreShields.ModData.Traits.ShieldActionFeat])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Raise a Shield and Step twice.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.Name != "Raise shield")
                            return null;
                        
                        Creature guardian = qfFeat.Owner;

                        if (MoreShields.NewShields.GetAvailableShields(guardian) is not { } shields)
                            return null;
                        if (shields.Count == 0)
                            return null;
                        if (shields.MaxBy(MoreShields.NewShields.GetShieldAC) is not { } shield)
                            return null;
                        
                        CombatAction guardAdvance = new CombatAction(
                                qfFeat.Owner,
                                new SideBySideIllustration(shield.Illustration, IllustrationName.FleetStep),
                                "Guarded Advance",
                                [Trait.Basic, Trait.DoNotShowOverheadOfActionName, ModData.Traits.Guardian],
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
                                Possibilities possibilities = caster.Possibilities.Filter(ap =>
                                {
                                    if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                                        return false;
                                    ap.CombatAction.ActionCost = 0;
                                    ap.RecalculateUsability();
                                    return true;
                                });
                                List<Option> actions = await caster.Battle.GameLoop.CreateActions(
                                    caster,
                                    possibilities,
                                    null);
                                caster.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                                await caster.Battle.GameLoop.OfferOptions(caster, actions, true);
                                
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
        // Reactive Strike
        yield return new TrueFeat(
                ModData.FeatNames.ReactiveStrike,
                6,
                "You swat a foe who leaves themself open to retaliation.",
                "{b}Trigger{/b} A creature within your reach uses a manipulate action or move action, makes a ranged attack, or leaves a square during a move action it's using.\n\nMake a melee Strike against the triggering creature. If your attack is a critical hit and the trigger was a manipulate action, you disrupt that action. This Strike doesn't count toward your multiple attack penalty, and your multiple attack penalty doesn't apply to this Strike.",
                [ModData.Traits.Guardian])
            .WithActionCost(Constants.ACTION_COST_REACTION)
            .WithOnCreature(self =>
            {
                QEffect reactiveStrike = QEffect.AttackOfOpportunity();
                reactiveStrike.Name = reactiveStrike.Name?.Replace("Attack of Opportunity", "Reactive Strike");
                self.AddQEffect(reactiveStrike);
            })
            .WithEquivalent(values => values.AllFeats.Any(ft => ft.BaseName is "Attack of Opportunity" or "Reactive Strike" or "Opportunist"));
        // Reflexive Shield, from More Dedications, probably needs More Shields compatibility, oops I forgot about that one too
        // Retaliating Rescue <---- High priority
        yield return new TrueFeat(
                ModData.FeatNames.RetaliatingRescue,
                6,
                "When an ally is in danger, you can hustle to reach them and punish the foe threatening them.",
                "Stride up to your Speed. You must end this movement adjacent to an ally who is within an enemy’s reach. Then, you push your ally up to 5 feet (as normal for forced movement, this movement doesn't trigger reactions) and make a melee Strike against an enemy within your reach. If your ally was in that enemy's reach and your push moved them out of it, you gain a +2 circumstance bonus to your attack roll.",
                [ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    ModLoader.DisplaysAsOffenseAction(
                        qfFeat,
                        "Retaliating Rescue",
                        "Stride to an ally in danger, push them, and Strike.",
                        2);
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        // TODO: Check tile.Neighbors for logic efficiency
                        CombatAction rescue = new CombatAction(
                                qfFeat.Owner,
                                new SideBySideIllustration(IllustrationName.QuickenTime, IllustrationName.KineticRam),
                                "Retaliating Rescue",
                                [Trait.Basic, ModData.Traits.Guardian],
                                "{i}When an ally is in danger, you can hustle to reach them and punish the foe threatening them.{/i}\n\nStride up to your Speed. You must end this movement adjacent to an ally who is within an enemy’s reach. Then, you push your ally up to 5 feet (as normal for forced movement, this movement doesn't trigger reactions) and make a melee Strike against an enemy within your reach. If your ally was in that enemy's reach and your push moved them out of it, you gain a +2 circumstance bonus to your attack roll.",
                                Target.Tile(
                                        (self, t) =>
                                        {
                                            List<Creature> allies = self.Battle.AllCreatures
                                                .Where(self.FriendOfAndNotSelf)
                                                .ToList();
                                            List<Creature> enemies = self.Battle.AllCreatures
                                                .Where(self.EnemyOf)
                                                .ToList();
                                            return t.LooksFreeTo(self) // Tile is free to me
                                                   && allies
                                                       .Where(ally => ally.Occupies.IsAdjacentTo(t)) // Has adjacent allies
                                                       .Any(ally => 
                                                           enemies.Any(enemy => 
                                                               enemy.DistanceTo(ally) == (enemy.WieldsItem(Trait.Reach) ? 2 : 1))); // Who is in reach to any enemy
                                        },
                                        (_,_) => int.MinValue)
                                    .WithPathfindingGuidelines(cr =>
                                        new PathfindingDescription() { Squares = cr.Speed }))
                            .WithActionCost(2)
                            .WithEffectOnChosenTargets(async (action, caster, targets) =>
                            {
                                // Enact stride towards preselected tile
                                //caster.MoveToUsingEarlierFloodfill()
                                if (!await caster.StrideAsync("Choose where to Stride with Retaliating Rescue. (1/2)", strideTowards: targets.ChosenTile))
                                    action.RevertRequested = true;
                                
                                // Choose an ally to push
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
                                    caster.Battle.Log("Retaliating Rescue was converted to a simple Stride.");
                                    action.SpentActions = 1;
                                    action.RevertRequested = true;
                                    return;
                                }
                                
                                // Record who the ally was adjacent to before the push
                                List<Creature> adjacentFoes = caster.Battle.AllCreatures
                                    .Where(cr => cr.EnemyOf(caster) && cr.IsAdjacentTo(pushedAlly))
                                    .ToList();

                                // Push ally
                                Sfxs.Play(SfxName.Shove);
                                pushedAlly.Overhead("*Pushed*", Color.Black);
                                await caster.PushCreature(pushedAlly, 1);
                                
                                // Record who they are no longer adjacent to
                                List<Creature> bonusAgainstWho = adjacentFoes
                                    .Where(cr => !cr.IsAdjacentTo(pushedAlly))
                                    .ToList();
                                
                                // Apply bonus
                                QEffect bonusAgainst = new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                                {
                                    Name = "[RETALIATING RESCUE BONUS]",
                                    BonusToAttackRolls = (qfBonus, actionStrike, target) =>
                                        actionStrike.HasTrait(Trait.Attack)
                                        && target != null
                                        && bonusAgainstWho.Contains(target)
                                            ? new Bonus(2, BonusType.Circumstance, "Retaliating rescue")
                                            : null,
                                };
                                caster.AddQEffect(bonusAgainst);
                                
                                // Make Strike
                                await CommonCombatActions.StrikeAdjacentCreature(
                                    caster,
                                    adjacentFoes.Contains);

                                // Remove bonus
                                bonusAgainst.ExpiresAt = ExpirationCondition.Immediately;
                            });

                        return (ActionPossibility)rescue;
                    };
                });
        // Ring Their Bell
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
            .WithPermanentQEffect(
                "Force creatures within 5 feet to make a Reflex save to avoid becoming off-guard or falling prone.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction stomp = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.StompGround,
                                "Stomp Ground",
                                [Trait.Basic, ModData.Traits.Guardian],
                                "{i}You bring your booted foot down on the ground with enough force to rattle your foes.{/i}\n\nEach creature in a 5-foot emanation must attempt a Reflex saving throw against your class DC."+S.FourDegreesOfSuccess(
                                    "The creature is unaffected.",
                                    "The creature is off-guard until the end of your turn.",
                                    "The creature is knocked prone.",
                                    "The creature is knocked prone and takes 1d6 bludgeoning damage from the fall."),
                                Target.SelfExcludingEmanation(1))
                            .WithActionCost(2)
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
            "When you use Taunt, you can choose up to three targets within range, and you can have up to three taunted enemies at a time. Each time you Taunt, you can choose which enemies remain taunted and which the effect ends for. You must remain at or below this limit.",
            [ModData.Traits.Guardian]);
        // Juggernaut Charge
        yield return new TrueFeat(
                ModData.FeatNames.JuggernautCharge,
                8,
                "As you move forward in a rush, you put the weight of your armor behind an attack that can drag a foe with you.",
                "{b}Requirements{/b} You are wearing medium or heavy armor.\n\nYou Stride. If you end your movement within melee reach of at least one enemy, you can make a melee Strike against that enemy, then Stride again.\n\nIf your Strike hit and dealt damage, that enemy is pulled with you and is moved the same direction and distance as your second Stride.",
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(2)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    ModLoader.DisplaysAsOffenseAction(
                        qfFeat,
                        "Juggernaut Charge",
                        "Stride, make a melee Strike, and Stride again. On a hit, drag the target with you.",
                        2);
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction jugCharge = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(IllustrationName.FleetStep, IllustrationName.Grapple),
                                "Juggernaut Charge",
                                [Trait.Basic, Trait.Flourish, ModData.Traits.Guardian],
                                "{i}As you move forward in a rush, you put the weight of your armor behind an attack that can drag a foe with you.{/i}\n\n{b}Requirements{/b} You are wearing medium or heavy armor.\n\nYou Stride. If you end your movement within melee reach of at least one enemy, you can make a melee Strike against that enemy, then Stride again.\n\nIf your Strike hit and dealt damage, that enemy is pulled with you and is moved the same direction and distance as your second Stride.",
                                Target.Self()
                                    .WithAdditionalRestriction(cr =>
                                        ModData.CommonRequirements.WearingMediumOrHeavyArmor()
                                            .Satisfied(cr, cr).UnusableReason))
                            .WithActionCost(2)
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
                                QEffect dragBehavior = new QEffect()
                                {
                                    Name = "[JUGGERNAUT DRAG]",
                                    ExpiresAt = ExpirationCondition.ExpiresAtEndOfYourTurn, // Fallback
                                    Tag = false, // Play push sound once
                                    StateCheckWithVisibleChanges = async qfDrag =>
                                    {
                                        Creature dragger = qfDrag.Owner;
                                        Creature dragee = chosenCreature;

                                        if (dragger.AnimationData.LongMovement?.Path is { Count: > 0 } path
                                            && path.IndexOf(dragger.Occupies) is var myTile and > -1
                                            && path[Math.Max(0, myTile-1)] is {} theirTile
                                            && theirTile.IsTrulyGenuinelyFreeTo(dragee))
                                        {
                                            if (qfDrag.Tag is false)
                                                qfDrag.Tag = Sfxs.Play(SfxName.Shove) != null;
                                            await dragee.SingleTileMove(
                                                theirTile,
                                                null,
                                                new MovementStyle()
                                                {
                                                    Shifting = true,
                                                    ForcedMovement = true,
                                                });
                                        }
                                    },
                                };
                                if (chosenCreature.HP != hpBefore) // If dealt damage, then also drag
                                    self.AddQEffect(dragBehavior);
                                if (!await self.StrideAsync("Choose where to Stride with Juggernaut Charge. The target will be pulled along your movement path. (3/3)", allowPass: true))
                                /*if (!await self.Battle.GameLoop.FullCast(
                                        CommonCombatActions.StepByStepStride(self)
                                            .WithActionCost(0)))*/
                                {
                                    self.Battle.Log("Juggernaut Charge was converted to a simple Stride and simple Strike");
                                    action.Traits.Remove(Trait.Flourish);
                                }
                                dragBehavior.ExpiresAt = ExpirationCondition.Immediately;
                            });

                        return (ActionPossibility)jugCharge;

                        void Revert(CombatAction act, string? toWhat, int cost = 0)
                        {
                            if (toWhat != null)
                                act.Owner.Battle.Log($"Juggernaut Charge was converted to {toWhat}.");
                            act.SpentActions = cost;
                            act.RevertRequested = true;
                        }
                    };
                });
        // Mighty Bulwark???
        // Repositioning Block ????? More Basic Actions??? Hard-coded?
        // Shield From Arrows
        // Shield Wallop
        yield return new TrueFeat(
                ModData.FeatNames.ShieldWallop,
                8,
                "Attacks with your shield knock the sense out of your foes.",
                "{b}Requirements{/b} You are wielding a shield.\n\nMake a shield Strike. If you hit and deal damage, the target is stupefied 1 until the start of your next turn (stupefied 2 on a critical hit).\n\nIf your shield is a tower shield, fortress shield, or another shield that grants a higher circumstance bonus to AC when you Take Cover behind it, the creature is instead stupefied 2 if you hit and deal damage to it (stupefied 3 on a critical hit).",
                [Trait.Flourish, ModData.Traits.Guardian])
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    ModLoader.DisplaysAsOffenseAction(
                        qfFeat,
                        "Shield Wallop",
                        "Make a shield Strike that stupefies the target.");
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        if (!item.HasTrait(Trait.Shield))
                            return null;

                        int baseValue = item.HasTrait(MoreShields.ModData.Traits.CoverShield)
                            ? 2
                            : 1;
                        
                        // BUG: (Not tested) Will probably apply QF even if the hit doesn't deal damage.
                        CombatAction wallop = qfFeat.Owner.CreateStrike(item)
                            .WithName("Shield Wallop")
                            .WithExtraTrait(Trait.Basic)
                            .WithExtraTrait(Trait.Flourish)
                            .WithExtraTrait(ModData.Traits.Guardian)
                            .WithActionCost(1)
                            .WithEffectOnEachTarget(async (_, a, d, result) =>
                            {
                                if (result >= CheckResult.Success)
                                    d.AddQEffect(
                                        QEffect.Stupefied(result == CheckResult.CriticalSuccess ? baseValue+1 : baseValue)
                                            .WithExpirationAtStartOfSourcesTurn(a, 1));
                            });
                        wallop.Illustration = new SideBySideIllustration(
                            item.Illustration,
                            IllustrationName.BrainDrain);
                        wallop.Description = StrikeRules.CreateBasicStrikeDescription4(
                            wallop.StrikeModifiers,
                            additionalSuccessText: $"The target is stupefied {baseValue}.",
                            additionalCriticalSuccessText: $"The target is stupefied {baseValue+1}.");
                        
                        return wallop;
                    };
                });
        #endregion
    }
}