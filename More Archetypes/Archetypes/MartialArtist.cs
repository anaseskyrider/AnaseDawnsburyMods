using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class MartialArtist
{
    public const string STUMBLING_FEINT_ASK_KEY = "STUMBLING_FEINT_ASK";
    public const string STUMBLING_FEINT_ASK_OFFGUARD = "OFFGUARD";
    public const string STUMBLING_FEINT_ASK_ALWAYS = "ALWAYS";
    public const string STUMBLING_FEINT_ASK_NEVER = "NEVER";
    
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
        foreach (Feat ft in CreateBonusFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // TODO: Lv1: Rushing Goat Stance
        // Requires: Tian Xia Character Guide (already included in notice)
        
        // Lv4: Rushing Goat Stance for Martial Artist
        
        // Lv1: Stumbling Stance
        Feat stumblingStance = Monk.CreateMonkStance(
            ModData.FeatNames.StumblingStance,
            ModData.QEffectIds.StumblingStance,
            "You enter a seemingly unfocused stance that mimics the movements of the inebriated — bobbing, weaving, leaving false openings, and distracting your enemies from your true movements.",
            "While in this stance, you gain a +1 circumstance bonus to Deception checks to Feint; and if an enemy hits you with a melee Strike, it becomes {r:flat-footed}off-guard{/r} against the next stumbling swing Strike you make against it before the end of your next turn.",
            "You have a +1 circumstance bonus to Feint, melee Strikes against you make enemies off-guard to you, and can only make stumbling swing attacks.",
            () => new Item(ModData.Illustrations.StumblingStance, "stumbling swing", Trait.Agile, Trait.Backstabber, Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Unarmed)
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Bludgeoning)),
            true,
            qfStance =>
            {
                qfStance.BonusToSkillChecks = (skill, action, _) =>
                    skill is Skill.Deception
                    && action.ActionId == ActionId.Feint
                        ? new Bonus(1, BonusType.Circumstance, "Stumbling Stance")
                        : null;
                qfStance.YouAreDealtDamageEvent = async (_, dEvent) =>
                {
                    if (dEvent.CombatAction == null
                        || !dEvent.CombatAction.HasTrait(Trait.Melee)
                        || !dEvent.CombatAction.HasTrait(Trait.Strike))
                        return;

                    dEvent.Source.AddQEffect(new QEffect(
                        "Off-guard to stumbling swing",
                        $"You are off-guard (-2 circumstance penalty to AC) to the next stumbling swing attack made by {qfStance.Owner.ToColoredName()}.",
                        ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                        qfStance.Owner,
                        IllustrationName.Flatfooted)
                    {
                        CannotExpireThisTurn = true,
                        CountsAsADebuff = true,
                        IsFlatFootedTo = (_, _, action) =>
                            action?.Owner == qfStance.Owner
                            && action is { Item.Name: "stumbling swing" }
                                ? "stumbling stance"
                                : null,
                        AfterYouAreTargeted = async (qfThis2, action) =>
                        {
                            if (action is { Item.Name: "stumbling swing" })
                                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    });
                };
            },
            false)
            .WithPrerequisite(FeatName.Deception, "Trained in Deception");
        yield return stumblingStance;
        
        // Lv4: Stumbling Stance for Martial Artist
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.StumblingStance, Trait.MartialArtist, 4);

        // Lv1: Tiger Stance
        Feat tigerStance = Monk.CreateMonkStance(
            ModData.FeatNames.TigerStance,
            ModData.QEffectIds.TigerStance,
            "You enter the stance of a tiger.",
            "As long as your Speed is at least 20 feet while in Tiger Stance, you can Step 10 feet.",
            "You can Step 10 feet while your speed is 20+ feet, and can make tiger claw attacks.",
            () => new Item(IllustrationName.DragonClaws, "tiger claw", Trait.Agile, Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Unarmed)
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Slashing)),
            false,
            qfStance =>
            {
                qfStance.Tag = false;
                qfStance.StateCheck += qfThis =>
                {
                    if (qfThis.Owner.Speed >= 4)
                        qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                        {
                            Name = "[TIGER STANCE 10-FOOT STEP]",
                            Id = QEffectId.ElfStep
                        });
                };
                qfStance.AfterYouDealDamage = async (attacker, action, defender) =>
                {
                    if (action.CheckResult == CheckResult.CriticalSuccess)
                    {
                        string bleedAmount = "1d4" + (action.Tag is ActionId id && id == ModData.ActionIds.TigerSlash
                            ? $"+{attacker.Abilities.Strength}"
                            : null);
                        QEffect critBleed = QEffect.PersistentDamage(
                            bleedAmount,
                            DamageKind.Bleed);
                        critBleed.SourceAction = action;
                        defender.AddQEffect(critBleed);
                    }
                };
            },
            true);
        yield return tigerStance;
        
        // Lv4: Tiger Stance for Martial Artist
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.TigerStance, Trait.MartialArtist, 4);
        
        // TODO: Lv1: Twisting Petal Stance
        // Requires: Tian Xia Character Guide (already included in notice)
        
        // Lv4: Twisting Petal Stance for Martial Artist
        
        // TODO: Lv8: Adamantine Body
        // Requires: Tian Xia Character Guide (already included in notice)
        
        // Lv6: Stumbling Feint
        yield return new TrueFeat(
                ModData.FeatNames.StumblingFeint,
                6,
                "You lash out confusingly, with what seems to be a weak move, but instead allows you to unleash a dangerous flurry of blows upon your unsuspecting foe.",
                """
                {b}Requirements{/b} You are in Stumbling Stance.

                When you use Flurry of Blows, you can attempt a check to Feint as a {icon:FreeAction} free action just before the first Strike. On a success, instead of making the target {r:flat-footed}off-guard{/r} against your next attack, they become {r:flat-footed}off-guard{/r} against both attacks from the Flurry of Blows.
                """,
                [Trait.Monk])
            .WithPermanentQEffect(qfFeat =>
            {
                // Ability description
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " You can Feint {icon:FreeAction} before you Flurry of Blows. On a success, the target is {r:flat-footed}off-guard{/r} to both attacks.";

                // Ask to use Feint
                qfFeat.YouBeginActionReaction = (qfThis, beginStrike) =>
                {
                    // Must be in Stumbling Stance,
                    // must be making a Strike from FoB
                    // must be the first one this turn
                    if (!qfThis.Owner.HasEffect(ModData.QEffectIds.StumblingStance)
                        || !beginStrike.HasTrait(Trait.Strike)
                        || !beginStrike.HasTrait(Trait.FromFlurryOfBlows)
                        || qfThis.Owner.Actions.ActionHistoryThisTurn.Count(ca =>
                            ca.HasTrait(Trait.FromFlurryOfBlows)) > 0
                        || beginStrike.ChosenTargets.ChosenCreature is not { } target
                        || target.IsImmuneTo(Trait.Mental))
                        return null;

                    // Don't ask to Feint if
                    // - you haven't made a choice
                    // - you said never to do it
                    // - you said only not off-guard and they are already off-guard
                    if (GetAskMode(qfThis.Owner.PersistentCharacterSheet!.Calculated) is not { } mode
                        || mode == STUMBLING_FEINT_ASK_NEVER
                        || (mode == STUMBLING_FEINT_ASK_OFFGUARD
                            && (beginStrike.ChosenTargets.ChosenCreature?.IsFlatFootedTo(beginStrike.Owner,
                                beginStrike) ?? false)))
                        return null;
                    
                    CombatAction outerFeint = CombatManeuverPossibilities
                        .CreateFeintAction(qfThis.Owner);

                    // Formatted feat block of this ability
                    CombatAction stumblingFeint = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                IllustrationName.Feint,
                                IllustrationName.FlurryOfBlows),
                            "Stumbling Feint",
                            [ModData.ModTrait, Trait.Monk],
                            null!,
                            Target.Self())
                        .WithActionCost(0)
                        .WithDescription(
                            "You lash out confusingly, with what seems to be a weak move, but instead allows you to unleash a dangerous flurry of blows upon your unsuspecting foe.",
                            """
                            {b}Requirements{/b} You are in Stumbling Stance.

                            When you use Flurry of Blows, you can attempt a check to Feint as a {icon:FreeAction} free action just before the first Strike. On a success, instead of making the target {r:flat-footed}off-guard{/r} against your next attack, they become {r:flat-footed}off-guard{/r} against both attacks from the Flurry of Blows.
                            """)
                        .WithEffectOnSelf(async caster =>
                        {
                            // Inner feint to actually execute
                            CombatAction innerFeint = CombatManeuverPossibilities
                                .CreateFeintAction(qfThis.Owner)
                                .WithActionCost(0)
                                /*.With(ca => ca.Description = "{Blue}{b}Requirements{/b} You are in Stumbling Stance.\n{b}Trigger{/b} You use Flurry of Blows and are about to make the first Strike.{/Blue}\n\n" + ca.Description.Replace(
                                    "{b}Success{/b} The target is flat-footed against your next melee attack this turn.",
                                    "{Blue}{b}Success{/b} The target is {r:flat-footed}off-guard{/r} against both attacks from the Flurry of Blows this turn.{/Blue}"))*/
                                .WithExtraTrait(Trait.DoNotShowInCombatLog)
                                .WithEffectOnEachTarget(async (sFeint, caster2, target2, result) =>
                                {
                                    // Critical success is better, so reject that outcome
                                    if (result != CheckResult.Success)
                                        return;
                                    QEffect sFeintQf = new QEffect(ExpirationCondition.ExpiresAtEndOfSourcesTurn)
                                    {
                                        Id = ModData.QEffectIds.FlatFootedToStumblingFeint,
                                        Source = caster2,
                                        SourceAction = sFeint,
                                        IsFlatFootedTo = (_, _, flatAction) =>
                                            flatAction?.Owner == caster2
                                            && flatAction.HasTrait(Trait.FromFlurryOfBlows)
                                                ? "Stumbling Feint"
                                                : null,
                                    };
                                    target2.AddQEffect(sFeintQf);
                                    // Remove after FoB completes
                                    caster2.AddQEffect(new QEffect()
                                    {
                                        AfterYouTakeAction = async (qfRemove, fob) =>
                                        {
                                            if (fob.ActionId is not ActionId.FlurryOfBlows)
                                                return;
                                            qfRemove.ExpiresAt = ExpirationCondition.Immediately;
                                            sFeintQf.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    });
                                });
                            innerFeint.WithFullRename("Stumbling Feint");
                            
                            await caster.Battle.GameLoop.FullCast(innerFeint, ChosenTargets.CreateSingleTarget(target));
                        });

                    stumblingFeint.Description += "\n\n" + CombatActionExecution.BreakdownAttackForTooltip(
                        outerFeint, target).TooltipDescription;

                    ReactionOption reactOpt = ReactionOption.WrapFullcast(
                        stumblingFeint,
                        $"Attempt to {{b}}Feint{{/b}} {target.ToColoredBoldedName()}. On a success, they are {{r:flat-footed}}off-guard{{/r}} to both Strikes.")
                        .WithIsFreeAction()
                        .WithTriggerReason(
                            $"{qfThis.Owner.ToColoredBoldedName()} uses {beginStrike.Name.WithTag("b")} against {target.ToColoredBoldedName()} as part of {{b}}Flurry of Blows{{/b}}.");

                    return reactOpt;
                };

                // Add toggles
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId is not PossibilitySectionId.SkillActions)
                        return null;

                    CombatAction setGuard = CreateSet(
                        IllustrationName.Flatfooted,
                        "Not off-guard",
                        "Always ask use Stumbling Feint when the target isn't {r:flat-footed}off-guard{/r}.",
                        STUMBLING_FEINT_ASK_OFFGUARD);
                    CombatAction setOn = CreateSet(
                        ModData.Illustrations.CheckSymbol,
                        "Always",
                        "Always ask to use Stumbling Feint when you use Flurry of Blows.",
                        STUMBLING_FEINT_ASK_ALWAYS);
                    CombatAction setOff = CreateSet(
                        ModData.Illustrations.NoSymbol,
                        "Never",
                        "Never ask to use Stumbling Feint when you use Flurry of Blows.",
                        STUMBLING_FEINT_ASK_NEVER);
                    
                    List<CombatAction> actList = [setGuard, setOn, setOff];

                    return new SubmenuPossibility(
                        new SideBySideIllustration(
                            IllustrationName.Feint,
                            ModData.Illustrations.StumblingStance),
                        "Toggles: Stumbling Feint")
                    {
                        PossibilityGroup = Constants.POSSIBILITY_GROUP_TOGGLES,
                        Subsections = [ new PossibilitySection("Stumbling Feint")
                        {
                            Possibilities = actList.Select(ca =>
                                    new ActionPossibility(ca)
                                        .WithPossibilityGroup(Constants.POSSIBILITY_GROUP_TOGGLES))
                                .ToList()
                        }]
                    };

                    CombatAction CreateSet(Illustration corner, string subName, string description, string settingConst)
                    {
                        return new CombatAction(
                                qfThis.Owner,
                                new CornerIllustration(
                                    ModData.Illustrations.StumblingStance,
                                    corner,
                                    Direction.Southeast),
                                "Ask to Stumbling Feint: " + subName,
                                [ModData.ModTrait, Trait.DoesNotBreakStealth, Trait.DoesNotPreventDelay],
                                description,
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        GetAskMode(self.PersistentCharacterSheet!.Calculated) != settingConst
                                            ? null
                                            : "Current setting"))
                            .WithActionCost(0)
                            .WithSoundEffect(SfxName.OminousActivation)
                            .WithEffectOnSelf(self =>
                            {
                                if (!self.PersistentCharacterSheet!.Calculated.Tags.TryAdd(
                                        STUMBLING_FEINT_ASK_KEY,
                                        settingConst))
                                    self.PersistentCharacterSheet!.Calculated.Tags[STUMBLING_FEINT_ASK_KEY] = settingConst;
                            });
                    }
                };
                
                return;

                string? GetAskMode(CalculatedCharacterSheetValues values)
                {
                    string? mode = values.Tags.TryGetValueAs(
                        STUMBLING_FEINT_ASK_KEY,
                        out string? result)
                        ? result
                        : null;
                    return mode;
                }
            })
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new LimitedTextSelectionOption(
                    "StumblingFeintAsk",
                    ModData.Illustrations.StumblingStance.IllustrationAsIconString + " Ask to use Stumbling Feint",
                    SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                    [
                        new FeatlikeChoice(
                            "StumblingFeintAskOffGuard",
                            ((Illustration)IllustrationName.Flatfooted).IllustrationAsIconString + " Not off-guard")
                        {
                            TextCreator = () => $"Always ask to use {ModData.FeatNames.StumblingFeint.ToLink("Stumbling Feint")} when the target isn't {{r:flat-footed}}off-guard{{/r}}.",
                            Apply = values2 =>
                            {
                                values2.Tags.TryAdd(STUMBLING_FEINT_ASK_KEY, STUMBLING_FEINT_ASK_OFFGUARD);
                            }
                        },
                        new FeatlikeChoice(
                            "StumblingFeintAskAlways",
                            ModData.Illustrations.CheckSymbol.IllustrationAsIconString + " Always")
                        {
                            TextCreator = () => $"Always ask to use {ModData.FeatNames.StumblingFeint.ToLink("Stumbling Feint")} when you use Flurry of Blows.",
                            Apply = values2 =>
                            {
                                values2.Tags.TryAdd(STUMBLING_FEINT_ASK_KEY, STUMBLING_FEINT_ASK_ALWAYS);
                            }
                        },
                        new FeatlikeChoice(
                            "StumblingFeintAskNever",
                            ModData.Illustrations.NoSymbol.IllustrationAsIconString + " Never")
                        {
                            TextCreator = () => $"Never ask to use {ModData.FeatNames.StumblingFeint.ToLink("Stumbling Feint")} when you use Flurry of Blows.",
                            Apply = values2 =>
                            {
                                values2.Tags.TryAdd(STUMBLING_FEINT_ASK_KEY, STUMBLING_FEINT_ASK_NEVER);
                            }
                        }
                    ]));
            })
            .WithPrerequisite(
                values =>
                    values.HasFeat(FeatName.Monk)
                    || values.AllFeats.Any(ft => ft.FeatName.ToStringOrTechnical() == "FlurryOfBlows"),
                "You must have the Flurry of Blows feature.")
            .WithPrerequisite(
                ModData.FeatNames.StumblingStance,
                "Stumbling Stance");
        
        // Lv8: Stumbling Feint for Martial Artist
        // DOC: This isn't normally available to Martial Artist, but is made available anyway to round out level 8 stance-upgrade options, for the sake of enhancing options for class feats in a Free Archetype game.
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.StumblingFeint, Trait.MartialArtist, 8);
        
        // TODO: Lv6: Momentous Charge
        // Requires: Tian Xia Character Guide (already included in notice)
        
        // Lv8: Momentous Charge for Martial Artist
        
        // Lv6: Tiger Slash
        yield return new TrueFeat(
                ModData.FeatNames.TigerSlash,
                6,
                "You make a fierce swipe with both hands.",
                """
                {b}Requirements{/b} You are in Tiger Stance.

                Make a tiger claw Strike. It deals two extra weapon damage dice (three dice at level 14+), and you can {r}push{/r} the target 5 feet away as if you had successfully Shoved them. If the attack is a critical success and deals damage, add your Strength modifier to the persistent bleed damage from your tiger claw.
                """,
                [Trait.Monk])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                int additionalDice = qfFeat.Owner.Level >= 14 ? 3 : 2;
                int bonusBleed = qfFeat.Owner.Abilities.Strength;
                
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " " + (qfThis.Owner.HasEffect(ModData.QEffectIds.TigerStance) ? null : "{Red}(Must be in Tiger Stance){/Red} ") + $"Make a tiger claw Strike that deals {("+"+additionalDice).WithColor("Blue")} damage dice. On a hit, {{r}}push{{/r}} the target 5 feet (as if a successful Shove). On a crit, deal {("+"+bonusBleed).WithColor("Blue")} more persistent bleed damage.";
                
                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (item.Name != "tiger claw")
                        return null;
                    
                    StrikeModifiers newMods = new StrikeModifiers()
                    {
                        AdditionalWeaponDamageDice = additionalDice,
                        OnEachTarget = async (attacker, defender, result) =>
                        {
                            if (result < CheckResult.Success)
                                return;
                            
                            CombatAction pushAsIfShoved = CombatManeuverPossibilities
                                .CreateShoveAction(attacker, item)
                                .WithExtraTrait(0, ModData.ModTrait)
                                .WithExtraTrait(Trait.AttackDoesNotIncreaseMultipleAttackPenalty)
                                .WithExtraTrait(Trait.DoNotShowInCombatLog)
                                .WithExtraTrait(Trait.DoNotShowOverheadOfCheckResult)
                                .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
                                .WithActionCost(0)
                                .WithDescription("The target is {r:push}pushed{/r} 5 feet as if successfully Shoved.");
                            pushAsIfShoved.WithFullRename("Shove (5 feet)");
                            // Overwrite execution effect
                            pushAsIfShoved.EffectOnOneTarget = async (shove, caster, target, _) =>
                            {
                                caster.Battle.Log(target.Name + " is pushed 5 feet (as if a successful Shove).", shove.Name, shove.Description, shove.Traits);
                                await caster.PushCreature(target, 1);
                            };
                            // Does not roll, but fully executes the action
                            pushAsIfShoved.WithNoSaveFor((_, _) => true);
                            pushAsIfShoved.CheckResult = CheckResult.Success;

                            await pushAsIfShoved.Fullcast(defender);
                        },
                    };

                    CombatAction strike = qfFeat.Owner.CreateStrike(item, -1, newMods)
                        .WithIllustration(new SideBySideIllustration(
                            item.Illustration,
                            IllustrationName.BloodVendetta))
                        .WithActionCost(2)
                        .WithExtraTrait(Trait.Basic)
                        .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                            newMods,
                            weaponDieIncreased: true,
                            additionalSuccessText: "The target is pushed 5 feet (as if successfully Shoved).",
                            additionalCriticalSuccessText: $"As success, and the bleed damage from tiger claw is increased by {{b}}{bonusBleed}{{/b}}."))
                        .WithTag(ModData.ActionIds.TigerSlash); // Tiger Claw checks this tag for bonus bleed.
                    strike.WithFullRename("Tiger Slash");
                    strike.Traits = new Traits([ModData.ModTrait, ..strike.Traits.ToList()], strike);

                    return strike;
                };
            })
            .WithPrerequisite(
                ModData.FeatNames.TigerStance,
                "Tiger Stance");
        
        // Lv8: Tiger Slash for Martial Artist
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.TigerSlash, Trait.MartialArtist, 8);
        
        // TODO: Lv8: Scattering in Spring
        // Requires: Tian Xia Character Guide (already included in notice)
        
        // Lv10: Scattering in Spring for Martial Artist
        
        // TODO: Lv12: Five-gods Ram
        
        // Lv14: Five-gods Ram for Martial Artist
        
        // TODO: Lv14: Path of Iron
        
        // TODO: Lv12: Whirling in the Summer Storm
        // Requires: Tian Xia Character Guide (already included in notice)
        
        // Lv14: Whirling in the Summer Storm for Martial Artist
        
        // TODO: Lv18: Echoing Violence
    }

    public static IEnumerable<Feat> CreateBonusFeats()
    {
        // (not otherwise part of Martial Artist)
        // (thank The Matrix Dragon for requesting these)

        // Lv8: Wild Winds Initiate
        Func<SpellId,Creature?,int,bool,SpellInformation,CombatAction> wildWindsSpell = (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            Illustration icon = ModData.Illustrations.WildWindsStance;
            const string name = "Wild Winds Stance";
            const string shortDescription = "You have a +2 circumstance bonus to AC against ranged attacks and can make ranged wind crash Strikes from 30 feet away.";
            const string passiveBonus = "While in wild winds stance, you gain a +2 circumstance bonus to AC against ranged attacks.";
            string describedAttack = "Also, you gain an additional attack option:\n"
                + Monk.DescribeAttack(WeaponProducer());
            
            CombatAction wildWindsSpell = Spells.CreateModern(
                    icon,
                    name,
                    [ModData.ModTrait, Trait.Air, Trait.Focus, Trait.Manipulate, Trait.Monk, Trait.Stance, Trait.SomaticOnly],
                    "You take on the stance of the flowing winds, sending out waves of energy at a distance.",
                    $$"""
                      {b}Duration{/b} until you leave the stance

                      Enter a stance.

                      {{passiveBonus}}

                      {{describedAttack}}

                      Wind crash Strikes ignore concealment and all cover.
                      """,
                    Target.Self()
                        .WithAdditionalRestriction(ModData.CommonRequirements.StanceRestriction(ModData.QEffectIds.WildWindsStance)),
                    spellLevel,
                    null)
                // Spells don't get a stat block description
                //.WithShortDescription("Enter a stance where " + shortDescription.Uncapitalize())
                .WithActionCost(1)
                .WithSoundEffect(SfxName.AirSpell)
                .WithEffectOnSelf(async self =>
                {
                    QEffect stanceQF = KineticistCommonEffects.EnterStance(self, icon, name, shortDescription, ModData.QEffectIds.WildWindsStance);
                    stanceQF.AdditionalUnarmedStrike = WeaponProducer();
                    stanceQF.BonusToDefenses = (qfThis, action, def) =>
                        action != null
                        && action.HasTrait(Trait.Ranged)
                        && def is Defense.AC
                            ? new Bonus(2, BonusType.Circumstance, "Wild winds stance")
                            : null;
                });
            
            return wildWindsSpell;
            
            Item WeaponProducer() =>
                new Item(icon, "wind crash", ModData.ModTrait, Trait.Agile, Trait.Brawling, Trait.Nonlethal, Trait.Propulsive, Trait.Unarmed, Trait.UnaffectedByConcealment, Trait.IgnoreAllCover)
                    .WithDescription("{Blue}This ignores concealment and all cover.{/Blue}")
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning)
                        {
                            VfxStyle = new VfxStyle(1, ProjectileKind.Arrow, icon),
                            Sfx = SfxName.AirSpell
                        }
                        .WithMaximumRange(6));
        };
        if (ModManager.TryParse("WildWindsStance", out SpellId wildWindsId))
        {
            ModData.SpellIds.WildWindsStance = wildWindsId;
            ModManager.RegisterActionOnEachSpell(spell =>
            {
                if (spell.SpellId != wildWindsId
                    || spell.SpellId == SpellId.None)
                    return;

                CombatAction newSpell = wildWindsSpell(wildWindsId, spell.Owner, spell.SpellLevel, spell.Owner != null && spell.Owner.Battle != TBattle.Pseudobattle, spell.SpellInformation!);

                spell.Illustration = newSpell.Illustration;
                spell.Name = newSpell.Name;
                spell.Traits = newSpell.Traits;
                spell.Description = newSpell.Description;
                spell.Target = newSpell.Target;
                spell.ActionCost = newSpell.ActionCost;
                spell.SoundEffectName = newSpell.SoundEffectName;
                spell.EffectOnChosenTargets = newSpell.EffectOnChosenTargets;
            });
        }
        else
            ModData.SpellIds.WildWindsStance = ModManager.RegisterNewSpell("WildWindsStance", 4, wildWindsSpell);
        Feat wildWindsInitiate = CreateQiSpellFeat2(
            ModData.FeatNames.WildWindsInitiate,
            8,
            "You learn a mystical stance that lets you attack from a distance.",
            "While entering the stance is a qi spell, the wind crash Strikes the stance grants are not, so you can use them as often as you like while in the stance.",
            "Wild Winds Stance",
            ModData.SpellIds.WildWindsStance,
            ModData.Illustrations.WildWindsStance,
            true);
        yield return wildWindsInitiate;

        // Lv8: Tangled Forest Stance
        Feat tangledStance = (Monk.CreateMonkStance(
                ModData.FeatNames.TangledForestStance,
                ModData.QEffectIds.TangledForestStance,
                "You extend your arms like gnarled branches to interfere with your foes’ movements.",
                "Every enemy in your reach that tries to move away from you must succeed at a Reflex save, Acrobatics check, or Athletics check against your class DC or be {r}immobilized{/r} for that action. You can allow the enemy to move.",
                "You can make lashing branch attacks and can prevent enemies from moving away from you.",
                () => new Item(
                        IllustrationName.ProtectorTree, "lashing branch",
                        ModData.ModTrait, Trait.Agile, Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Unarmed)
                    .WithWeaponProperties(new WeaponProperties(
                        "1d8",
                        DamageKind.Slashing)),
                false,
                qfStance =>
                {
                    string immobilized = QEffectId.Immobilized.HumanizeTitleCase2().ToLower();
                    qfStance.Id = QEffectId.AttackOfOpportunity;
                    qfStance.WhenProvokedReactions = (qfThis, action) =>
                    {
                        // Must be provoking with their movement based action
                        if (action.Owner.AnimationData.LongMovement is not
                                { Path: { Count: > 0 } path, CombatAction: { } move}
                            || move != action
                            || action.Owner.WeaknessAndResistance.OtherImmunities.Contains(immobilized))
                            return null;
                        
                        (Tile _, Tile next) = GetMovement();

                        // Must be currently within the monk's reach and are moving to a
                        // tile that is further than you already are
                        int distanceToMonk = action.Owner.DistanceTo(qfStance.Owner);
                        if (distanceToMonk > qfStance.Owner.Space.NaturalReach
                            || next.DistanceTo(qfStance.Owner) <= distanceToMonk)
                            return null;
                        
                        // Roll Data
                        CalculatedNumber.CalculatedNumberProducer bestRoll = Checks.BestRoll(
                            Checks.SavingThrow(Defense.Reflex),
                            TaggedChecks.SkillCheck(Skill.Acrobatics, Skill.Athletics)
                                .CalculatedNumberProducer);
                        CalculatedNumber classDC = new CalculatedNumber(
                            qfStance.Owner.ClassDC(),
                            "Class DC",
                            []);
                        ActiveRollSpecification rollSpec = new ActiveRollSpecification(
                            bestRoll,
                            (_, _, _) => classDC);
                        CheckBreakdown breakdown = CombatActionExecution.BreakdownAttackForTooltip(
                            CombatAction.CreateSimple(action.Owner, "Tangled Forest Stance")
                                .WithActiveRollSpecification(rollSpec),
                            qfStance.Owner);

                        ReactionOption reactOpt = ReactionOption.CreateCustom(
                                $"Tangled Forest Stance {{b}}{{i}}({(breakdown.Misses + breakdown.CritMisses) * 5})%{{/i}}{{b}}",
                                "Attempt to {r:immobilized}immobilize{/r} their movement.",
                                ModData.Illustrations.TangledForestStance,
                                qfStance.Owner,
                                async () =>
                                {
                                    CheckBreakdownResult breakdownResult = new CheckBreakdownResult(breakdown);
                                    
                                    action.Owner.Overhead(
                                        breakdownResult.CheckResult.HumanizeTitleCase2(),
                                        Color.LightBlue,
                                        $"{action.Owner} rolls {breakdownResult.CheckResult.Greenify()} on Tangled Forest Stance.",
                                        "Tangled Forest Stance",
                                        breakdown.DescribeWithFinalRollTotal(breakdownResult));

                                    if (breakdownResult.CheckResult < CheckResult.Success)
                                    {
                                        action.Disrupted = true;
                                        action.Owner.Overhead(
                                            "movement disrupted",
                                            Color.Red,
                                            action.Owner + "'s movement ends because it was disrupted.");
                                    }
                                })
                            .WithDoesNotCountAsYourTriggerResponse()
                            .WithTriggerReason($"{action.Owner.ToColoredName()} uses {action.Name.WithTag("b")} to move away from you which provokes.");
                        
                        reactOpt.MouseOverStatblock = new LoglineStatblock(new LogLine(
                            qfStance.Owner.Battle.LogIndent, "",
                            "Tangled Forest Stance",
                            $"Every enemy in your reach that tries to move away from you must succeed at a Reflex save, Acrobatics check, or Athletics check against your class DC or be immobilized for that action. You can allow the enemy to move.\n\n{breakdown.TooltipDescription}",
                            new Traits([ModData.ModTrait, Trait.Monk, Trait.Stance]),
                            null));

                        return reactOpt;

                        (Tile Current, Tile Next) GetMovement()
                        {
                            int last = path.Count - 1; // 0th-index; count is 2 and last is [1] in [5, 6]
                            Tile currentTile = action.Owner.Space.TopLeftTile;
                            int curIndex = path.IndexOf(currentTile);
                            // If current is not in list, then movement just began, so use
                            // the first tile in the path. Otherwise, go up to the last tile
                            int nextIndex = curIndex == -1 ? 0 : Math.Min(curIndex + 1, last);
                            Tile nextTile = path[nextIndex];
                            return (action.Owner.Space.TopLeftTile, nextTile);
                        }
                    };
                },
                true) as TrueFeat)!
            .WithLevelPrereq(8);
        yield return tangledStance;
        
        // Lv8: Clinging Shadows Stance
        Func<SpellId,Creature?,int,bool,SpellInformation,CombatAction> clingingSpell = (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            Illustration icon = ModData.Illustrations.ClingingShadowsStance;
            const string name = "Clinging Shadows Stance";
            const string shortDescription = "You gain a +2 circumstance bonus to Grapple and for creatures to Escape you. You can make shadow grasp Strikes.";
            const string passiveBonus = "While in clinging shadows stance, you have a +2 circumstance bonus to Athletics checks to Grapple, and to the DC for creatures to Escape from you.";
            string describedAttack = "Also, you gain an additional attack option:\n"
                + Monk.DescribeAttack(WeaponProducer()).Replace("negative", "void").Replace("grapple.", "grapple, reach.");
            
            CombatAction clingingShadowsSpell = Spells.CreateModern(
                    icon,
                    name,
                    [ModData.ModTrait, Trait.Focus, Trait.Manipulate, Trait.Monk, Trait.Shadow, Trait.Stance, Trait.SomaticOnly],
                    "You adopt the stance of clinging shadows, shrouding your limbs in sticky smoke made of qi.",
                    $$"""
                      {b}Duration{/b} until you leave the stance

                      Enter a stance.

                      {{passiveBonus}}

                      {{describedAttack}}.
                      """,
                    Target.Self()
                        .WithAdditionalRestriction(ModData.CommonRequirements.StanceRestriction(ModData.QEffectIds.ClingingShadowsStance)),
                    spellLevel,
                    null)
                // Spells don't get a stat block appearance
                //.WithShortDescription("Enter a stance where " + shortDescription.Uncapitalize())
                .WithActionCost(1)
                // SFX I like:
                // - DeepNecromancy
                // - Necromancy
                // - Deafness
                // - Mistform
                // - DeathsCall
                .WithSoundEffect(SfxName.DeathsCall)
                .WithEffectOnSelf(async self =>
                {
                    QEffect stanceQF = KineticistCommonEffects.EnterStance(self, icon, name, shortDescription, ModData.QEffectIds.ClingingShadowsStance);
                    stanceQF.AdditionalUnarmedStrike = WeaponProducer();
                    stanceQF.BonusToSkillChecks = (skill, action, _) =>
                        skill is Skill.Athletics && action.ActionId is ActionId.Grapple
                            ? new Bonus(2, BonusType.Circumstance, "Clinging shadows stance")
                            : null;
                    stanceQF.BonusToDefenses = (_, action, _) =>
                        action?.ActionId is ActionId.Escape
                            ? new Bonus(2, BonusType.Circumstance, "Clinging shadows stance")
                            : null;
                });
            
            return clingingShadowsSpell;
            
            Item WeaponProducer() =>
                new Item(icon, "shadow grasp", Trait.Agile, Trait.Brawling, Trait.Grapple, Trait.Reach, Trait.Unarmed)
                    // SFX I like:
                    // - Necromancy
                    // - MajorNegative
                    //.WithSoundEffect(SfxName.MajorNegative) // Abandoned since these can play too rapidly, don't have an appropriately subtle shadowy sound.
                    .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Negative));
        };
        if (ModManager.TryParse("ClingingShadowsStance", out SpellId clingingId))
        {
            ModData.SpellIds.ClingingShadowsStance = clingingId;
            ModManager.RegisterActionOnEachSpell(spell =>
            {
                if (spell.SpellId != clingingId
                    || spell.SpellId == SpellId.None)
                    return;

                CombatAction newSpell = clingingSpell(clingingId, spell.Owner, spell.SpellLevel, spell.Owner != null && spell.Owner.Battle != TBattle.Pseudobattle, spell.SpellInformation!);

                spell.Illustration = newSpell.Illustration;
                spell.Name = newSpell.Name;
                spell.Traits = newSpell.Traits;
                spell.Description = newSpell.Description;
                spell.Target = newSpell.Target;
                spell.ActionCost = newSpell.ActionCost;
                spell.SoundEffectName = newSpell.SoundEffectName;
                spell.EffectOnChosenTargets = newSpell.EffectOnChosenTargets;
            });
        }
        else
            ModData.SpellIds.ClingingShadowsStance = ModManager.RegisterNewSpell("ClingingShadowsStance", 4, clingingSpell);
        Feat clingingShadowsInitiate = CreateQiSpellFeat2(
            ModData.FeatNames.ClingingShadowsInitiate,
            8,
            "You learn a mystical stance that transforms your qi into sticky smoke that shrouds your limbs.",
            "While entering the stance is a qi spell, the shadow grasp Strikes the stance grants are not, so you can use them as often as you like while in the stance.",
            "Clinging Shadows Stance",
            ModData.SpellIds.ClingingShadowsStance,
            ModData.Illustrations.ClingingShadowsStance,
            true);
        yield return clingingShadowsInitiate;
        
        // TODO: Lv14: Tangled Forest Rake
        
        // TODO: Lv14: Shadow's Web
    }

    /// Exists to resolve issues with needing to create feats that reference spells which haven't been added yet by the mod manager process.
    public static Feat CreateQiSpellFeat2(
        FeatName featName,
        int level,
        string flavorText,
        string rulesText,
        string spellName,
        SpellId spellId,
        Illustration icon,
        bool mustHaveFocusPool = false)
    {
        string description = "You gain the {i}" + spellName.ToLower() + "{/i} qi spell. " + (mustHaveFocusPool
            ? "Increase the number of focus points in your focus pool by 1, up to a maximum of 3."
            : "You also gain 1 focus point, up to a maximum of 3.");
        Feat kiSpellFeat = new TrueFeat(
                featName,
                level,
                flavorText,
                description + (rulesText[0] is '\\' && rulesText[1] is 'n' ? null : " ") + rulesText.TrimStart(),
                [Trait.Monk])
            .WithOnSheet(sheet =>
            {
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
                sheet.AddFocusSpellAndFocusPoint(Trait.Monk, Ability.Wisdom, spellId);
            })
            .WithRulesBlockForSpell(spellId, Trait.Monk)
            .WithIllustration(icon);
        if (mustHaveFocusPool)
            kiSpellFeat = kiSpellFeat.WithPrerequisite(
                sheet =>
                    sheet.FocusSpells.Any(spellByClass => spellByClass.Key is Trait.Monk && spellByClass.Value.Spells.Count > 0),
                "Must know at least one ki spell.");
        return kiSpellFeat;
    }
}