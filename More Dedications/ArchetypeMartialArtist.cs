using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
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
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeMartialArtist
{
    //public static readonly FeatName DedicationFeat = ModManager.RegisterFeatName("MoreDedications.Archetype.MartialArtist.Dedication", "Martial Artist Dedication");

    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
        foreach (Feat ft in CreateBonusFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Dedication Feat
        Feat martialArtistDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.MartialArtistArchetype,
                "You seek neither mysticism nor enlightenment, and you don't view this training as some greater path to wisdom. Yours is the way of the fist striking flesh, the hand turning aside the blade, and the devastating kick taking your enemy down.",
                "You have trained to use your fists as deadly weapons. The damage die for your fist unarmed attacks becomes 1d6 instead of 1d4; and all of your unarmed attacks lose the nonlethal trait.\n\nWhenever you gain a class feature that grants you expert or greater proficiency in certain weapons, you also gain that proficiency rank in all unarmed attacks.")
            .WithOnCreature(self =>
            {
                self.WithUnarmedStrike(Item.ImprovedFist());
                self.AddQEffect(new QEffect()
                {
                    Id = QEffectId.PowerfulFist
                });
            });
        martialArtistDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return martialArtistDedication;
        
        // Powder Punch Stance
        yield return new TrueFeat(
                ModData.FeatNames.PowderPunchStance,
                2,
                "You infuse your handwraps with black powder.",
                "On your first melee Strike each round with an unarmed attack"+/*", knuckle duster, or black powder knuckle duster"+*/", you deal an additional 1 fire damage. If you critically succeed at an attempt to Shove while in this stance, the target is pushed back an additional 5 feet.",
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Stance])
            .WithAvailableAsArchetypeFeat(ModData.Traits.MartialArtistArchetype)
            .WithActionCost(1)
            .WithIllustration(ModData.Illustrations.PowderPunchStance)
            .WithPermanentQEffect(
                "Your first melee Strike deals +1 fire damage, and critical Shoves push 5 more feet.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                            return null;

                        CombatAction enterStance = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.PowderPunchStance,
                                "Powder Punch Stance",
                                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Stance],
                                "{i}You infuse your handwraps with black powder.{/i}\n\n"+"On your first melee Strike each round with an unarmed attack"+/*", knuckle duster, or black powder knuckle duster"+*/", you deal an additional 1 fire damage. If you critically succeed at an attempt to Shove while in this stance, the target is pushed back an additional 5 feet.",
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.HasEffect(ModData.QEffectIds.PowderPunchStance) ? "You're already in this stance." : null))
                            .WithShortDescription("Enter a stance where " + "Your first melee Strike deals +1 fire damage, and critical Shoves push 5 more feet.".Uncapitalize())
                            .WithActionCost(1)
                            .WithEffectOnSelf(async self =>
                            {
                                QEffect ppStance = KineticistCommonEffects.EnterStance(
                                    self,
                                    ModData.Illustrations.PowderPunchStance,
                                    "Powder Punch Stance",
                                    "Your first melee Strike deals +1 fire damage, and critical Shoves push 5 more feet.",
                                    ModData.QEffectIds.PowderPunchStance);
                                ppStance.AddExtraStrikeDamage = (action, defender) =>
                                {
                                    if (!action.HasTrait(Trait.Unarmed) || qfFeat.UsedThisTurn == true)
                                        return null;

                                    qfFeat.UsedThisTurn = true;
                                    return (DiceFormula.FromText("1"), DamageKind.Fire);
                                };
                                ppStance.AfterYouTakeAction = async (qfThis2, action) =>
                                {
                                    if (action.ActionId != ActionId.Shove ||
                                        action.CheckResult < CheckResult.CriticalSuccess ||
                                        action.ChosenTargets.ChosenCreature is not { } chosen)
                                        return;
                                    
                                    // You've already shoved and possibly followed. If you're adjacent NOW,
                                    // you probably followed, so you'll KEEP following.
                                    // Code copied from basic Shove.
                                    bool wasAdjacent = qfThis2.Owner.IsAdjacentTo(chosen);
                                    Tile previousPosition = chosen.Occupies;
                                    await qfThis2.Owner.PushCreature(chosen, 1);
                                    Tile occupies = chosen.Occupies;
                                    if (wasAdjacent)
                                    {
                                        Point point = new Point(occupies.X - previousPosition.X,
                                            occupies.Y - previousPosition.Y);
                                        Tile yourNewPosition = self.Battle.Map.GetTile(self.Occupies.X + point.X,
                                            self.Occupies.Y + point.Y)!;
                                        ICombatAction? strideAction = self.Possibilities.CreateActions(false)
                                            .FirstOrDefault(act =>
                                                act.Action.ActionId == ActionId.Stride);
                                        ++self.Actions.ActionsLeft;
                                        bool flag3 = strideAction != null && (bool)strideAction.CanBeginToUse(self);
                                        --self.Actions.ActionsLeft;
                                        if (flag3)
                                        {
                                            await self.MoveTo(
                                                yourNewPosition,
                                                action,
                                                new MovementStyle()
                                                {
                                                    MaximumSquares = 100,
                                                    Shifting = true,
                                                    ShortestPath = true
                                                });
                                        }
                                    }
                                };
                            });
                        return new ActionPossibility(enterStance)
                            .WithPossibilityGroup("Enter a stance");
                    };
                });
        
        // Add Brawling Focus to Martial Artist
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.BrawlingFocus, ModData.Traits.MartialArtistArchetype, 4);

        // Add Existing stance feats to Martial Artist
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.CraneStance, ModData.Traits.MartialArtistArchetype, 4);
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.DragonStance, ModData.Traits.MartialArtistArchetype, 4);
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.GorillaStance, ModData.Traits.MartialArtistArchetype, 4);
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.MountainStance, ModData.Traits.MartialArtistArchetype, 4);
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.WolfStance, ModData.Traits.MartialArtistArchetype, 4);
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.GorillaPound, ModData.Traits.MartialArtistArchetype, 8);
        
        // Stumbling Stance
        Feat stumblingStance = CreateMonkStance2(
            ModData.FeatNames.StumblingStance,
            "Stumbling Stance",
            1,
            ModData.QEffectIds.StumblingStance,
            "You enter a seemingly unfocused stance that mimics the movements of the inebriated—bobbing, weaving, leaving false openings, and distracting your enemies from your true movements.",
            "While in this stance, you gain a +1 circumstance bonus to Deception checks to Feint; and if an enemy hits you with a melee Strike, it becomes flat-footed against the next stumbling swing Strike you make against it before the end of your next turn.",
            "You have a +1 to Feint, melee Strikes against you make enemies flat-footed to you, and can only make stumbling swing attacks.",
            () => new Item(ModData.Illustrations.StumblingStance, "stumbling swing",
                [Trait.Agile, Trait.Backstabber, Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Unarmed])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Bludgeoning)),
            true,
            qfStance =>
            {
                qfStance.BonusToSkillChecks = (skill, action, target) =>
                    skill is Skill.Deception && action.ActionId == ActionId.Feint
                        ? new Bonus(1, BonusType.Circumstance, "Stumbling Stance")
                        : null;
                qfStance.YouAreDealtDamageEvent = async (qfThis, dEvent) =>
                {
                    if (dEvent.CombatAction == null || !dEvent.CombatAction.HasTrait(Trait.Melee) ||
                        !dEvent.CombatAction.HasTrait(Trait.Strike))
                        return;

                    dEvent.Source.AddQEffect(new QEffect("Flat-footed to stumbling swing",
                        $"You are flat-footed to the next stumbling swing attack made by {{Blue}}{qfStance.Owner}{{/blue}}",
                        ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                        qfStance.Owner,
                        IllustrationName.Flatfooted)
                    {
                        CannotExpireThisTurn = true,
                        CountsAsADebuff = true,
                        IsFlatFootedTo = (qfThis2, creature, action) =>
                        {
                            if (creature != qfStance.Owner || action is not { Item.Name: "stumbling swing" })
                                return null;

                            return "stumbling stance";
                        },
                        YouAreDealtDamageEvent = async (qfThis2, dEvent2) =>
                        {
                            if (dEvent2.CombatAction is { Item.Name: "stumbling swing" })
                                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    });
                };
            },
            false)
            .WithPrerequisite(FeatName.Deception, "Trained in Deception");
        stumblingStance.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return stumblingStance;
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.StumblingStance, ModData.Traits.MartialArtistArchetype, 4);

        // Tiger Stance
        Feat tigerStance = CreateMonkStance2(
            ModData.FeatNames.TigerStance,
            "Tiger Stance",
            1,
            ModData.QEffectIds.TigerStance,
            "You enter the stance of a tiger.",
            "As long as your Speed is at least 20 feet while in Tiger Stance, you can Step again as a free action when you Step.",//you can Step 10 feet.",
            "You can Step as a free action after Stepping, and can make tiger claw attacks.",
            () => new Item(IllustrationName.DragonClaws, "tiger claw",
                    [Trait.Agile, Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Unarmed])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Slashing)),
            false,
            qfStance =>
            {
                qfStance.Tag = false;
                qfStance.StateCheck += qfThis =>
                {
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
                        QEffect critBleed = QEffect.PersistentDamage(bleedAmount,
                            DamageKind.Bleed);
                        critBleed.SourceAction = action;
                        defender.AddQEffect(critBleed);
                    }
                };
            },
            true);
        tigerStance.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return tigerStance;
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.TigerStance, ModData.Traits.MartialArtistArchetype, 4);

        // Follow-Up Strike
        yield return new TrueFeat(
                ModData.FeatNames.FollowUpStrike,
                6,
                "You have trained to use all parts of your body as a weapon, and when you miss with an attack, you can usually continue the attack with a different body part and still deal damage.",
                "{b}Requirements{/b} Your last action was a missed Strike with a melee unarmed attack.\n\nMake another Strike with a melee unarmed attack, using the same multiple attack penalty step as for the missed Strike, if any.\n\n" + new SimpleIllustration(IllustrationName.YellowWarning).IllustrationAsIconString + " {b}Limitation{/b} Pre-roll breakdown is an estimate. The final roll is accurate.",
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Flourish])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MartialArtistArchetype)
            .WithPermanentQEffect(
                "Make an unarmed Strike with your last multiple attack penalty step.",
                qfFeat =>
                {
                    qfFeat.YouBeginAction = async (qfThis, action) =>
                    {
                        qfThis.Tag = action;
                    };
                    qfFeat.EndOfYourTurnDetrimentalEffect = async (qfThis, creature) => { qfThis.Tag = null; };
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        // Item filter
                        if (!item.HasTrait(Trait.Melee) || !item.HasTrait(Trait.Unarmed))
                            return null;
                        
                        // Locals
                        CombatAction? action = qfFeat.Tag as CombatAction;
                        Item? actionItem = action?.Item;

                        // Combat Action
                        CombatAction followUpStrike = qfFeat.Owner.CreateStrike(item, qfFeat.Owner.Actions.AttackedThisManyTimesThisTurn-1);
                        followUpStrike.Name = "Follow-Up Strike";
                        followUpStrike.Illustration = new SideBySideIllustration(item.Illustration, IllustrationName.ExactingStrike);
                        followUpStrike.Description = StrikeRules.CreateBasicStrikeDescription4(
                            followUpStrike.StrikeModifiers,
                            additionalAttackRollText: "This uses the same multiple attack penalty step as your last Strike.");
                        (followUpStrike.Target as CreatureTarget)!.WithAdditionalConditionOnTargetCreature(
                            (attacker, defender) =>
                            {
                                if (action is { CheckResult: < CheckResult.Success }
                                    && actionItem != null
                                    && actionItem.HasTrait(Trait.Unarmed)
                                    && actionItem.HasTrait(Trait.Melee))
                                    return Usability.Usable;
                                else
                                    return Usability.NotUsable("Invalid last action");
                            });

                        return followUpStrike;
                    };
                });

        // Thunder Clap
        yield return new TrueFeat(
                ModData.FeatNames.ThunderClap,
                6,
                "You slam your hands together to unleash a deafening blast.",
                "{b}Requirements{/b} You are in Powder Punch Stance\n\nCreatures in a 15-foot cone take 3d6 sonic damage, with a basic Fortitude save against your class DC. Creatures that critically fail their save are also deafened for the rest of combat.\n\nYou can't use this ability again for 1d4 rounds as your hands recover from the thunderous vibrations.\n\nAt 8th level, and every 2 levels thereafter, the damage from Thunder Clap increases by 1d6.",
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Sonic])
            .WithAvailableAsArchetypeFeat(ModData.Traits.MartialArtistArchetype)
            .WithActionCost(2)
            .WithPermanentQEffect(
                "Deal basic sonic damage in a 15-foot cone.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                            return null;

                        if (qfThis.Owner.FindQEffect(ModData.QEffectIds.PowderPunchStance) == null)
                            return null;

                        int numDice = 3 + (qfThis.Owner.Level - 6)/2;
                        const IllustrationName clapIcon = IllustrationName.ThunderousStrike;
                        const string clapActionName = "Thunder Clap";

                        CombatAction clapAction = new CombatAction(
                                qfThis.Owner,
                                clapIcon,
                                clapActionName,
                                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Sonic],
                                "{i}You slam your hands together to unleash a deafening blast.{/i}\n\nCreatures in a 15-foot cone take "+S.HeightenedVariable(numDice, 3)+"d6"+" sonic damage, with a basic Fortitude save against your class DC. Creatures that critically fail their save are also deafened for the rest of combat.\n\nYou can't use this ability again for 1d4 rounds as your hands recover from the thunderous vibrations.",
                                Target.FifteenFootCone())
                            .WithSavingThrow(new SavingThrow(Defense.Fortitude, qfThis.Owner.ClassDC()))
                            .WithProjectileCone(VfxStyle.BasicProjectileCone(clapIcon))
                            .WithSoundEffect(SfxName.ElectricBlast)
                            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                            {
                                await CommonSpellEffects.DealBasicDamage(thisAction, caster, target, result, $"{numDice}d6", DamageKind.Sonic);
                                if (result == CheckResult.CriticalFailure)
                                    target.AddQEffect(QEffect.Deafened());
                            })
                            .WithEffectOnChosenTargets(async (attacker, defender) =>
                                attacker.AddQEffect(QEffect.Recharging(clapActionName)));

                        return new ActionPossibility(clapAction);
                    };
                })
            .WithPrerequisite(
                ModData.FeatNames.PowderPunchStance,
                "Powder Punch Stance");

        // Crane Flutter
        yield return new TrueFeat(
                ModData.FeatNames.CraneFlutter,
                6,
                "You interpose your arm between yourself and your opponent.",
                "{b}Trigger{/b} You are targeted with a melee attack by an attacker you can see.\n{b}Requirements{/b} You are in Crane Stance\n\nYour circumstance bonus to AC from Crane Stance increases to +3 against the triggering attack. If the attack misses you, you can immediately make a crane wing Strike against the attacker at a –2 penalty and with the Reach trait for this attack.",
                [ModData.Traits.MoreDedications, Trait.Monk])
            .WithActionCost(-2)
            .WithPermanentQEffect(
                "Against a melee attack, you can increase your AC and make a crane wing attack if it misses.",
                qfFeat =>
                {
                    qfFeat.YouAreTargeted = async (qfThis, provokingAction) =>
                    {
                        if (qfThis.Owner.FindQEffect(QEffectId.CraneStance) == null || !provokingAction.HasTrait(Trait.Melee) || !provokingAction.HasTrait(Trait.Attack) || provokingAction.Owner.DetectionStatus.UndetectedTo.Contains(qfThis.Owner) || provokingAction.Owner.DetectionStatus.HiddenTo.Contains(qfThis.Owner))
                            return;

                        Creature self = qfThis.Owner;
                        
                        if (await self.Battle.AskToUseReaction(self,
                                "{b}Crane Flutter {icon:Reaction}{\b}\nYou are targeted with a melee attack.\nIncrease AC by 2, and if the attack misses, make a crane wing Strike at -2?"))
                        {
                            self.Battle.Log($"{self} uses {{b}}Crane Flutter{{/b}}.", "Crane Flutter {icon:Reaction}", "Your circumstance bonus to AC from Crane Stance increases to +3 against the triggering attack. If the attack misses you, you can immediately make a crane wing Strike against the attacker at a –2 penalty and with the Reach trait for this attack.");
                            self.AddQEffect(new QEffect()
                            {
                                Tag = ModData.ActionIds.CraneFlutter,
                                BonusToDefenses = (qfThis2, combatAction, def) => def == Defense.AC && combatAction == provokingAction ? new Bonus(
                                3, BonusType.Circumstance, "Crane Flutter") : null,
                                AfterYouAreTargeted = async (qfThis2, combatAction) =>
                                {
                                    if (combatAction.CheckResult < CheckResult.Success)
                                    {
                                        // Crane Stance compels your unarmeds, so this should be guaranteed to be Crane Wing Attack.
                                        CombatAction wingAttack = self.CreateStrike(self.UnarmedStrike)
                                            .WithExtraTrait(Trait.Reach)
                                            .WithActionCost(0);
                                        wingAttack.StrikeModifiers.AdditionalBonusesToAttackRoll ??= [];
                                        wingAttack.StrikeModifiers.AdditionalBonusesToAttackRoll.Add(new Bonus(-2, BonusType.Untyped, "Crane Flutter"));
                                        await self.Battle.GameLoop.FullCast(wingAttack, ChosenTargets.CreateSingleTarget(combatAction.Owner));
                                    }
                                },
                            });
                        }
                    };
                })
            .WithPrerequisite(
                FeatName.CraneStance,
                "Crane Stance");
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.CraneFlutter, ModData.Traits.MartialArtistArchetype, 8);
        
        // Dragon Roar
        yield return new TrueFeat(
                ModData.FeatNames.DragonRoar,
                6,
                "You bellow, instilling fear in your enemies.",
                "{b}Requirements{/b} You are in Dragon Stance.\n\nEnemies within a 15-foot emanation must succeed at a Will save against your Intimidation DC or be frightened 1 (frightened 2 on a critical failure). When a creature frightened by the roar begins its turn adjacent to you, it can’t reduce its frightened value below 1 on that turn.\n\nYour first attack that hits a frightened creature after you roar and before the end of your next turn gains a +4 circumstance bonus to damage.\n\nAfter you use Dragon Roar, you can't use it again for 1d4 rounds. Its effects end immediately if you leave Dragon Stance. Creatures in the area of your roar are then temporarily immune for 1 minute.",
                [ModData.Traits.MoreDedications, Trait.Auditory, Trait.Emotion, Trait.Fear, Trait.Mental, Trait.Monk])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Frighten enemies in a 15-foot emanation (Will save), and do bonus damage to your next attack against a frightened enemy.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                            return null;

                        if (qfThis.Owner.FindQEffect(QEffectId.DragonStance) == null)
                            return null;

                        const string roarActionName = "Dragon Roar";
                        const IllustrationName roarIcon = IllustrationName.Fear;
                        
                        CombatAction roarAction = new CombatAction(
                                qfThis.Owner,
                                roarIcon,
                                roarActionName,
                                [ModData.Traits.MoreDedications, Trait.Auditory, Trait.Emotion, Trait.Fear, Trait.Mental, Trait.Monk],
                                "{/i}You bellow, instilling fear in your enemies.{/i}\n\n"+
                                "{b}Requirements{/b} You are in Dragon Stance.\n\nEnemies within a 15-foot emanation must succeed at a Will save against your Intimidation DC or be frightened 1 (frightened 2 on a critical failure). When a creature frightened by the roar begins its turn adjacent to you, it can't reduce its frightened value below 1 on that turn.\n\nYour first attack that hits a frightened creature after you roar and before the end of your next turn gains a +4 circumstance bonus to damage.\n\nAfter you use Dragon Roar, you can't use it again for 1d4 rounds. Its effects end immediately if you leave Dragon Stance. Creatures in the area of your roar are then temporarily immune for 1 minute.",
                                Target.SelfExcludingEmanation(3)
                                    .WithIncludeOnlyIf((target, cr) =>
                                    {
                                        bool isEnemy = cr.EnemyOf(qfThis.Owner);
                                        bool isImmune = cr.QEffects.Any(qf =>
                                            qf is { Id: QEffectId.ImmunityToTargeting, Tag: ActionId id, Source: { } src } &&
                                            id == ModData.ActionIds.DragonRoar && src == qfThis.Owner);
                                        return isEnemy && !isImmune;
                                    }))
                            .WithActionId(ModData.ActionIds.DragonRoar)
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.Fear)
                            .WithProjectileCone(VfxStyle.BasicProjectileCone(roarIcon))
                            .WithSavingThrow(new SavingThrow(Defense.Will, qfThis.Owner.Skills.Get(Skill.Intimidation)+10))
                            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                            {
                                int value = result < CheckResult.Success
                                    ? result == CheckResult.CriticalFailure ? 2 : 1
                                    : 0;
                                if (value > 0)
                                {
                                    QEffect specialFrightened = QEffect.Frightened(value);
                                    specialFrightened.Description += $"\n\n{{Blue}}Dragon Roar: Beginning your turn adjacent to {caster.Name} prevents this frightened from being removed that turn.{{/Blue}}";
                                    specialFrightened.StartOfYourPrimaryTurn = async (qfThis2, creature) =>
                                    {
                                        if (qfThis2.Owner.IsAdjacentTo(caster))
                                            qfThis2.Owner.AddQEffect(new QEffect(
                                                    "Frightened Won't Expire",
                                                    "Frightened from Dragon Roar won't expire this turn.",
                                                    ExpirationCondition.ExpiresAtEndOfYourTurn, // Decrements occur first; this holds until after Frightened is checked.
                                                    caster,
                                                    IllustrationName.DirgeOfDoom)
                                            {
                                                Id = QEffectId.DirgeOfDoomFrightenedSustainer,
                                            });
                                    };
                                    specialFrightened.Tag = roarActionName; // Used to remove fear early if stance ends.
                                    specialFrightened.Source = caster;
                                    target.AddQEffect(specialFrightened);
                                }

                                QEffect roarImmunity = QEffect.ImmunityToTargeting(ModData.ActionIds.DragonRoar, caster);
                                roarImmunity.Illustration = thisAction.Illustration;
                                roarImmunity.Key = caster.Name+ModData.QEffectIds.DragonRoarImmunity;
                                roarImmunity.DoNotShowUpOverhead = true;
                                target.AddQEffect(roarImmunity);
                            })
                            .WithEffectOnChosenTargets(async (attacker, defender) =>
                            {
                                attacker.AddQEffect(QEffect.Recharging(roarActionName));
                                attacker.AddQEffect(
                                    new QEffect(
                                        "Dragon Roar Benefits",
                                        "Your first attack that hits a frightened creature before the end of your next turn gains a +4 circumstance bonus to damage.", 
                                        ExpirationCondition.ExpiresAtEndOfYourTurn,
                                        attacker,
                                        roarIcon)
                                    {
                                        Tag = roarActionName, // Used to remove benefits early if stance ends.
                                        CannotExpireThisTurn = true,
                                        DoNotShowUpOverhead = true,
                                        BonusToDamage = (qfThis2, action, defender2) =>
                                        {
                                            if (defender2.HasEffect(QEffectId.Frightened) && action.HasTrait(Trait.Attack))
                                            {
                                                qfThis2.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                                return new Bonus(4, BonusType.Circumstance, "Dragon roar benefits");
                                            }
                                            return null;
                                        },
                                    });
                            });

                        return new ActionPossibility(roarAction);
                    };
                    qfFeat.StateCheck = qfThis =>
                    {
                        if (qfThis.Owner.FindQEffect(QEffectId.DragonStance) != null)
                            return;
                        bool removedAny = false;
                        foreach (Creature cr in qfThis.Owner.Battle.AllCreatures)
                        {
                            if (cr.RemoveAllQEffects(qf => qf.Tag is "Dragon Roar" && qf.Source == qfThis.Owner) > 0)
                                removedAny = true;
                        }
                        if (removedAny)
                            qfThis.Owner.Battle.Log($"{qfThis.Owner.Name}'s dragon roar effects removed due to stance ending.");
                    };
                })
            .WithPrerequisite(
                FeatName.DragonStance,
                "Dragon Stance");
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.DragonRoar, ModData.Traits.MartialArtistArchetype, 8);
        
        // Grievous Blow
        yield return new TrueFeat(
                ModData.FeatNames.GrievousBlow,
                8,
                "You know how to deliver focused, powerful blows that bypass your enemies' resistances.",
                "Make an unarmed melee Strike. This counts as two attacks when calculating your multiple attack penalty. If this Strike hits, you deal two extra weapon damage dice."+/*" If you are at least 18th level, increase this to three extra weapon damage dice."+*/"\n\nThis attack also ignores an amount of resistance to physical damage, or to a specific physical damage type, equal to your level.",
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Flourish])
            .WithActionCost(2)
            .WithPermanentQEffect("Make an unarmed melee Strike that deals extra damage and partially ignores resistances.",
                qfFeat =>
                {
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        if (!item.HasTrait(Trait.Unarmed) || !item.HasTrait(Trait.Melee))
                            return null;
                        
                        StrikeModifiers strikeMods = new StrikeModifiers()
                        {
                            AdditionalWeaponDamageDice = 2,
                            OnEachTarget = async (attacker, defender, result) =>
                            {
                                ++attacker.Actions.AttackedThisManyTimesThisTurn;
                            },
                        };

                        CombatAction strike = qfFeat.Owner.CreateStrike(item, strikeModifiers: strikeMods)
                            .WithName("Grievous Blow")
                            .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                                strikeMods,
                                weaponDieIncreased: true,
                                additionalAttackRollText: $"This attack ignores an amount of resistance to physical damage, or to a specific physical damage type, equal to {{b}}{qfFeat.Owner.Level}{{/b}}.",
                                additionalAftertext: "Your multiple attack penalty increases twice instead of just once."))
                            .WithActionCost(2)
                            .WithExtraTrait(Trait.Basic)
                            .WithExtraTrait(Trait.Archetype)
                            .WithExtraTrait(Trait.Flourish)
                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, caster, targets) =>
                            {
                                targets.ChosenCreature?.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                {
                                    YouAreDealtDamageEvent = async (qfThis, dEvent) => 
                                    {
                                        // Make sure the event is actually associated with this action.
                                        if (dEvent.CombatAction != action || dEvent.CombatAction.Item is not {} item2)
                                            return;
                                        
                                        // Look at only the physical damage that this action does.
                                        var relevantDamages = item2.DetermineDamageKinds()
                                            .Where(kind => kind.IsPhysical());

                                        // Get the applied resistances associated with this action.
                                        var relevantResistances = qfThis.Owner.WeaknessAndResistance.AppliedResistances
                                            .Where(res =>
                                                relevantDamages.Contains(res.DamageKind) || res is SpecialResistance { Name: "physical" })
                                            .ToArray();

                                        // Increase the damage dealt for each relevant resistance that was applied, up to caster level.
                                        relevantResistances.ForEach(res =>
                                        {
                                            int bypassAmount = Math.Min(res.Value, caster.Level);
                                            dEvent.ReduceBy(-bypassAmount, $"Bypass {bypassAmount} {res.DamageKind.HumanizeLowerCase2()} resistance (grievous blow)");
                                            dEvent.DamageEventDescription.Replace("--", "+");
                                        });
                                    },
                                });
                            });
                        strike.Illustration =
                            new SideBySideIllustration(strike.Illustration, IllustrationName.StarHit);

                        return strike;
                    };
                })
            .WithAvailableAsArchetypeFeat(ModData.Traits.MartialArtistArchetype);
        
        // Mountain Stronghold
        yield return new TrueFeat(
                ModData.FeatNames.MountainStronghold,
                6,
                "You focus on your connection to the earth and call upon the mountain to block attacks against you.",
                "{b}Requirements{/b} You are in Mountain Stance.\n\nYou gain a +2 circumstance bonus to AC until the beginning of your next turn." +
                "\n\n{b}Special{/b} If you have this feat, you can add up to +1 to your AC from your Dexterity modifier instead of none.",
                [ModData.Traits.MoreDedications, Trait.Monk])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "While in Mountain Stance, gain a +2 circumstance bonus to AC until next turn.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                            return null;

                        if (qfThis.Owner.FindQEffect(QEffectId.MountainStance) == null)
                            return null;

                        if (qfThis.Owner.FindQEffect(ModData.QEffectIds.MountainStronghold) != null)
                            return null;

                        CombatAction strongholdAction = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(IllustrationName.FallingStoneRock, IllustrationName.AncestralDefense),
                                "Mountain Stronghold",
                                [ModData.Traits.MoreDedications, Trait.Monk],
                                "{i}You focus on your connection to the earth and call upon the mountain to block attacks against you.{/i}\n\n" +
                                "{b}Requirements{/b} You are in Mountain Stance.\n\nYou gain a +2 circumstance bonus to AC until the beginning of your next turn.",
                                Target.Self())
                            .WithActionCost(1)
                            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                            {
                                QEffect strongholdBonus = new QEffect("Mountain Stronghold", "You have a +2 circumstance bonus to AC.", ExpirationCondition.ExpiresAtStartOfYourTurn, caster, thisAction.Illustration)
                                {
                                    Id = ModData.QEffectIds.MountainStronghold,
                                    BonusToDefenses = (qfThis2, action, def) => def == Defense.AC ? new Bonus(2, BonusType.Circumstance, "Mountain stronghold") : null
                                };
                                caster.AddQEffect(strongholdBonus);
                            });

                        return new ActionPossibility(strongholdAction);
                    };
                    qfFeat.AfterYouAcquireEffect = async (qfThis, qfAcquired) =>
                    {
                        if (qfAcquired.Id != QEffectId.MountainStance)
                            return;
                        
                        qfAcquired.LimitsDexterityBonusToAC = 1;
                        qfAcquired.Description = qfAcquired.Description?.Replace("but don't add your Dexterity to AC", "{Blue}with a +1 Dexterity modifier cap{/Blue}");
                        qfThis.Owner.RecalculateArmor();
                    };
                })
            .WithPrerequisite(
                FeatName.MountainStance,
                "Mountain Stance");
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.MountainStronghold, ModData.Traits.MartialArtistArchetype, 8);
        
        // Stumbling Feint
        // PETR: If levels extend beyond 8, account for dedication FoB. For now, class-check is sufficient.
        yield return new TrueFeat(
                ModData.FeatNames.StumblingFeint,
                6,
                "You lash out confusingly with what seems to be a weak move but instead allows you to unleash a dangerous flurry of blows upon your unsuspecting foe.",
                "{b}Requirements{/b} You are in Stumbling Stance.\n\nWhen you use Flurry of Blows, you can attempt a check to Feint as a free action just before the first Strike. On a success, instead of making the target flat-footed against your next attack, they become flat-footed against both attacks from the Flurry of Blows.",
                [ModData.Traits.MoreDedications, Trait.Monk])
            .WithPermanentQEffect(
                "Feint just before you use Flurry of Blows. The target is flat-footed to both blows.",
                qfFeat =>
                {
                    qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                            return null;

                        if (qfThis.Owner.FindQEffect(ModData.QEffectIds.StumblingStance) == null)
                            return null;
                        
                        CombatAction stumblingFeintAction = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(
                                    IllustrationName.Feint, IllustrationName.FlurryOfBlows),
                                "Stumbling Feint",
                                [ModData.Traits.MoreDedications, Trait.Monk, Trait.Flourish],
                                "{i}You lash out confusingly with what seems to be a weak move but instead allows you to unleash a dangerous flurry of blows upon your unsuspecting foe.{/i}\n\n{b}Requirements{/b} You are in Stumbling Stance.\n\nWhen you use Flurry of Blows, you can attempt a check to Feint as a free action just before the first Strike. On a success, instead of making the target flat-footed against your next attack, they become flat-footed against both attacks from the Flurry of Blows.",
                                Target.AdjacentCreature()//Target.Reach(qfThis.Owner.UnarmedStrike)
                                    .WithAdditionalConditionOnTargetCreature((attacker, defender) => attacker.HasEffect(ModData.QEffectIds.StumblingStance) ? Usability.Usable : Usability.NotUsable("requires stumbling stance"))
                                    .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement())
                                    .WithAdditionalConditionOnTargetCreature((attacker, defender) => defender.IsImmuneTo(Trait.Mental)
                                        ? Usability.NotUsableOnThisCreature("immune to mental")
                                        : Usability.Usable)
                                    .WithAdditionalConditionOnTargetCreature((self, defender) =>
                                    {
                                        var foundWeapon = self.MeleeWeapons.Any(weapon =>
                                            weapon.HasTrait(Trait.Unarmed) &&
                                            CommonRulesConditions.CouldMakeStrike(self, weapon));
                                        return foundWeapon ? Usability.Usable : Usability.NotUsable("There is no nearby enemy or you can't make attacks."); 
                                    }))
                            .WithActionCost(1)
                            //.WithActionId(ActionId.Feint)
                            .WithEffectOnEachTarget(async (_, caster, target, _) =>
                            {
                                // Feint
                                CombatAction feint = CombatManeuverPossibilities.CreateFeintAction(caster).WithActionCost(0);
                                await caster.Battle.GameLoop.FullCast(feint, ChosenTargets.CreateSingleTarget(target));
                                //await caster.FictitiousSingleTileMove(caster.Occupies);
                                
                                // BUG: If a player's attack is somehow interjected in the FoB loop, this would benefit.
                                if (feint.CheckResult >= CheckResult.Success)
                                {
                                    target.AddQEffect(new QEffect()
                                    {
                                        Id = ModData.QEffectIds.FlatFootedToStumblingFeint,
                                        IsFlatFootedTo = (qfThis2, creature, action) =>
                                            creature == caster ? "Stumbling Feint" : null,
                                    });
                                }
                                
                                // Stops it from fizzling without enough actions.
                                caster.Actions.ActionsLeft += 1;
                                
                                // Flurry of Blows
                                Possibilities foBs = Possibilities.Create(caster)
                                    .Filter( ap =>
                                    {
                                        if (ap.CombatAction.ActionId != ActionId.FlurryOfBlows)
                                            return false;
                                        ap.CombatAction.ActionCost = 0;
                                        ap.CombatAction.Traits.Remove(Trait.Flourish);
                                        //ap.CombatAction.WithExtraTrait(Trait.DoNotShowOverheadOfActionName); // Too much text spam.
                                        
                                        ap.RecalculateUsability();
                                        return true;
                                    });
                                List<Option> actions = await caster.Battle.GameLoop.CreateActions(caster, foBs, null);
                                await caster.Battle.GameLoop.OfferOptions(caster, actions, true);
                                
                                caster.Actions.ActionsLeft -= 1;

                                target.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.FlatFootedToStumblingFeint);
                            });
                        return new ActionPossibility(stumblingFeintAction);
                    };
                })
            .WithPrerequisite(
                FeatName.Monk,
                "Flurry of Blows")
            .WithPrerequisite(
                ModData.FeatNames.StumblingStance,
                "Stumbling Stance");
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.StumblingFeint, ModData.Traits.MartialArtistArchetype, 8);
        
        // Tiger Slash
        yield return new TrueFeat(
                ModData.FeatNames.TigerSlash,
                6,
                "You make a fierce swipe with both hands.",
                "{b}Requirements{/b} You are in Tiger Stance.\n\nMake a tiger claw Strike. It deals two extra weapon damage dice"/*+" (three extra dice if you’re 14th level or higher)"*/+", and you can push the target 5 feet away as if you had successfully Shoved them. If the attack is a critical success and deals damage, add your Strength modifier to the persistent bleed damage from your tiger claw.",
                [ModData.Traits.MoreDedications, Trait.Monk])
            .WithActionCost(2)
            .WithPermanentQEffect(
                "While in Tiger Stance, you can make a tiger claw strike that deals additional damage, automatically deals a successful Shove on a hit, and adds your Strength modifier to the persistent bleed damage from a critical hit.",
                qfFeat =>
                {
                    qfFeat.ProvideStrikeModifier = (item) =>
                    {
                        if (item.Name != "tiger claw")
                            return null;
                        
                        StrikeModifiers newMods = new StrikeModifiers()
                        {
                            AdditionalWeaponDamageDice = qfFeat.Owner.Level >= 14 ? 3 : 2,
                            OnEachTarget = async (attacker, defender, result) =>
                            {
                                if (result > CheckResult.Failure)
                                {
                                    // Tiger Claw manually checks for Tiger Slash
                                    /*if (result == CheckResult.CriticalSuccess)
                                    {
                                        
                                    }*/
                                    
                                    // CombatAction shoveAction = CombatManeuverPossibilities.CreateShoveAction(attacker, attacker.UnarmedStrike)
                                    //         .WithActionCost(0);
                                    
                                    CombatAction pushAsIfShoved = new CombatAction(
                                            attacker,
                                            IllustrationName.None,
                                            "Shove (5 feet)",
                                            [Trait.Basic, Trait.Attack, Trait.AttackDoesNotTargetAC, Trait.AttackDoesNotIncreaseMultipleAttackPenalty],
                                            "The target is pushed 5 feet as if successfully Shoved.",
                                            Target.Reach(item))
                                        .WithActionCost(0)
                                        .WithActionId(ActionId.Shove)
                                        .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics), TaggedChecks.DefenseDC(Defense.Fortitude)))
                                        .WithEffectOnEachTarget(async (_, caster, target, checkResult) =>
                                        {
                                            if (checkResult > CheckResult.Failure)
                                                await caster.PushCreature(target, 1);
                                        });

                                    // Automatic success behavior
                                    attacker.AddQEffect(new QEffect()
                                    {
                                        AdjustActiveRollCheckResult = (qfThis, action, target, result2) =>
                                        {
                                            if (action.ActionId != ActionId.Shove) 
                                                return result2;
                                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                            return CheckResult.Success;
                                        },
                                    });

                                    await attacker.Battle.GameLoop.FullCast(pushAsIfShoved,
                                        ChosenTargets.CreateSingleTarget(defender));
                                }
                            },
                        };

                        CombatAction strike = qfFeat.Owner.CreateStrike(item, -1, newMods)
                            .WithName("Tiger Slash")
                            .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                                newMods,
                                weaponDieIncreased: true,
                                additionalSuccessText: "The target is pushed as if successfully Shoved.",
                                additionalCriticalSuccessText: $"As success, and the bleed damage from tiger claw is increased by {{b}}{qfFeat.Owner.Abilities.Strength}{{/b}}."))
                            .WithActionCost(2)
                            .WithExtraTrait(Trait.Basic)
                            .WithTag(ModData.ActionIds.TigerSlash); // Tiger Claw checks this tag for bonus bleed.
                        strike.Illustration =
                            new SideBySideIllustration(strike.Illustration, IllustrationName.BloodVendetta);

                        return strike;
                    };
                })
            .WithPrerequisite(
                ModData.FeatNames.TigerStance,
                "Tiger Stance");
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.TigerSlash, ModData.Traits.MartialArtistArchetype, 8);
        
        // Wolf Drag
        yield return new TrueFeat(
                ModData.FeatNames.WolfDrag,
                6,
                "You rip your enemy off their feet.",
                "Make a wolf jaw Strike. Your wolf jaw gains the fatal d12 trait for this Strike, and if the attack succeeds, you knock the target prone.",
                [ModData.Traits.MoreDedications, Trait.Monk])
            .WithActionCost(2)
            .WithPermanentQEffect(
                "Make a wolf jaw strike with the fatal d12 trait that knocks the target prone on a success.",
                qfFeat =>
                {
                    qfFeat.ProvideStrikeModifier = (item) =>
                    {
                        if (item.Name != "wolf jaw")
                            return null;

                        StrikeModifiers newMods = new StrikeModifiers()
                        {
                            OnEachTarget = async (attacker, defender, result) =>
                            {
                                if (result > CheckResult.Failure)
                                    defender.AddQEffect(QEffect.Prone());
                            }
                        };

                        // Duplicating it is a null error for some god-forsaken reason.
                        //Item wolfJaw = item.Duplicate();
                        
                        CombatAction strike = qfFeat.Owner.CreateStrike(item, strikeModifiers: newMods)
                            .WithName("Wolf Drag")
                            .WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                                newMods,
                                additionalSuccessText: "The target is knocked prone.",
                                additionalCriticalSuccessText: "As success, and the attack gains the fatal d12 trait."))
                            .WithActionCost(2)
                            .WithExtraTrait(Trait.Basic)
                            .WithExtraTrait(Trait.FatalD12)
                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, self, targets) =>
                            {
                                action.Item!.Traits.Add(Trait.FatalD12);
                            });
                        strike.WithEffectOnChosenTargets(async (self, targets) =>
                            {
                                strike.Item!.Traits.Remove(Trait.FatalD12);
                            });
                        strike.Illustration = new SideBySideIllustration(
                            item.Illustration, IllustrationName.DropProne);

                        return strike;
                    };
                })
            .WithPrerequisite(
                FeatName.WolfStance,
                "Wolf Stance");
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.WolfDrag, ModData.Traits.MartialArtistArchetype, 8);
    }

    public static IEnumerable<Feat> CreateBonusFeats()
    {
        // (not otherwise part of Martial Artist)
        // (thank The Matrix Dragon for requesting these)
        
        // Stoked Flame Stance
        Feat stokedFlameStance = CreateMonkStance2(
            ModData.FeatNames.StokedFlameStance,
            "Stoked Flame Stance",
            1,
            ModData.QEffectIds.StokedFlameStance,
            "You enter a stance of fast, fiery movements.",
            "You gain a +5-foot status bonus to your Speed. This bonus is cumulative with the status bonus from incredible movement.\n\nIf you have access to the flashing sparks' {tooltip:criteffect}critical specialization effects{/}, the target also takes 1d4 persistent fire damage if your critical Strike dealt damage.",
            "You can make flashing spark attacks and gain a +5-foot status bonus to your Speed.",
            () => new Item(IllustrationName.WhirlingFlames, "flashing spark",
                    [Trait.Brawling, Trait.Forceful, Trait.Nonlethal, Trait.Sweep, Trait.Unarmed])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Slashing)),
            false,
            qfStance =>
            {
                qfStance.BonusToAllSpeeds = qfThis =>
                {
                    int bonusAmount = 1;
                    if (qfThis.Owner.QEffects.FirstOrDefault(qf => qf.Name is "Incredible movement") is { } move)
                        bonusAmount += move.BonusToAllSpeeds?.Invoke(move)?.Amount ?? 0;
                    return new Bonus(bonusAmount, BonusType.Status, "Stoked Flame Stance");
                };
                qfStance.AfterYouDealDamage = async (attacker, action, defender) =>
                {
                    if (action.CheckResult == CheckResult.CriticalSuccess)
                    {
                        if (action.Item == null)
                            return;
                        
                        bool canCritSpec = false;
                        foreach (QEffect qf in attacker.QEffects.Where(qf => qf.YouHaveCriticalSpecialization != null))
                        {
                            if (qf.YouHaveCriticalSpecialization!.Invoke(qf, action.Item, action, defender))
                            {
                                canCritSpec = true;
                                break;
                            }
                        }

                        if (!canCritSpec)
                            return;
                        
                        string fireAmount = "1d4";
                        QEffect critFire = QEffect.PersistentDamage(fireAmount,
                            DamageKind.Fire);
                        critFire.SourceAction = action;
                        defender.AddQEffect(critFire);
                    }
                };
            },
            true);
        stokedFlameStance.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return stokedFlameStance;
        
        yield return new TrueFeat(
                ModData.FeatNames.InnerFire,
                6,
                null,
                "While you're in Stoked Flame Stance, you have cold and fire resistance equal to half your level, and any creature that hits you with an unarmed attack, tries to Grab or Grapple you, or otherwise touches you takes fire damage equal to your Wisdom modifier (minimum 1). A creature can take this damage no more than once per turn.",
                [ModData.Traits.MoreDedications, Trait.Monk])
            .WithPermanentQEffect(
                "While in Stoked Flame Stance, you have cold and fire resistance, and creatures which touch you take fire damage.",
                qfFeat =>
                {
                    qfFeat.StateCheck = qfThis =>
                    {
                        if (!qfThis.Owner.HasEffect(ModData.QEffectIds.StokedFlameStance))
                            return;

                        int amount = qfThis.Owner.Level / 2;
                        qfThis.Owner.WeaknessAndResistance.AddResistance(DamageKind.Cold, amount);
                        qfThis.Owner.WeaknessAndResistance.AddResistance(DamageKind.Fire, amount);
                    };
                    qfFeat.AfterYouAreTargeted = async (qfThis, action) =>
                    {
                        if (
                            !(action.HasTrait(Trait.Unarmed) && action.CheckResult > CheckResult.Failure)
                            && !(action.ActionId is ActionId.Grapple/* || action.HasTrait(Trait.Grab)*/)
                            && !(action.Target is CreatureTarget ct &&
                                ct.CreatureTargetingRequirements.Any(req => req is AdjacencyCreatureTargetingRequirement))
                        )
                            return;
                        int amount = Math.Max(qfThis.Owner.Abilities.Wisdom, 1);
                        await CommonSpellEffects.DealDirectDamage(
                            CombatAction.CreateSimple(qfThis.Owner, "Inner Fire"),
                            DiceFormula.FromText(amount.ToString(), "Wisdom"),
                            action.Owner,
                            CheckResult.Success,
                            DamageKind.Fire);
                    };
                })
            .WithPrerequisite(
                ModData.FeatNames.StokedFlameStance,
                "Stoked Flame Stance");

        // Wild Winds Initiate
        ModData.SpellIds.WildWindsStance = ModManager.RegisterNewSpell(
            "WildWindsStance",
            4,
            (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                Illustration icon = ModData.Illustrations.WildWindsStance;
                const string name = "Wild Winds Stance";
                const string shortDescription = "You have a +2 circumstance bonus to AC against ranged attacks and can make wind crash Strikes from 30 feet away.";
                string passiveBonus = "While in wild winds stance, you gain a +2 circumstance bonus to AC against ranged attacks.";
                string describedAttack = "Also, you gain an additional attack option:\n"
                    + Monk.DescribeAttack(WeaponProducer())
                        .Replace("unaffectedbyconcealment,", "")
                        .Replace("  ", " ")
                        .Replace("ranged", "ranged (30 ft.)");
                CombatAction wildWindsSpell = Spells.CreateModern(
                        icon,
                        name,
                        [ModData.Traits.MoreDedications, Trait.Air, Trait.Evocation, Trait.Focus, Trait.Manipulate, Trait.Monk, Trait.Stance, Trait.SomaticOnly],
                        "You take on the stance of the flowing winds, sending out waves of energy at a distance.",
                        "{b}Duration{/b} until you leave the stance\n\nEnter a stance.\n\n" + passiveBonus + "\n\n" + describedAttack + "\n\nWind crash Strikes ignore concealment and all cover.",
                        Target.Self()
                            .WithAdditionalRestriction(ModData.CommonRequirements.StanceRestriction(ModData.QEffectIds.WildWindsStance)),
                        4,
                        null)
                    .WithShortDescription("Enter a stance where " + shortDescription.Uncapitalize())
                    .WithActionCost(1)
                    .WithSoundEffect(SfxName.AirSpell)
                    .WithEffectOnSelf(async self =>
                    {
                        QEffect stanceQF = KineticistCommonEffects.EnterStance(
                            self,
                            icon,
                            name,
                            shortDescription,
                            ModData.QEffectIds.WildWindsStance);
                        stanceQF.AdditionalUnarmedStrike = WeaponProducer();
                        stanceQF.BonusToDefenses = (qfThis, action, def) =>
                            action != null && action.HasTrait(Trait.Ranged) && def is Defense.AC
                                ? new Bonus(2, BonusType.Circumstance, "Wild winds stance")
                                : null;
                        stanceQF.BonusToAttackRolls = (qfThis, action, defender) =>
                        {
                            if (action.Item != qfThis.AdditionalUnarmedStrike
                                || !action.HasTrait(Trait.Strike)
                                || qfThis.Tag is true
                                || defender == null)
                                return null;

                            // Prevent infinite recursion
                            qfThis.Tag = true;

                            // Get all circumstance bonuses to AC on this attack
                            List<Bonus>? circumstances = action.ActiveRollSpecification
                                ?.TaggedDetermineDC
                                .CalculatedNumberProducer
                                .Invoke(action, action.Owner, defender)
                                .Bonuses
                                .Where(bonus => bonus is { BonusType: BonusType.Circumstance, Amount: > 0 })
                                .WhereNotNull()
                                .OrderBy(bonus => bonus.Amount)
                                .ToList();

                            qfThis.Tag = false;

                            // Return null if there's nothing to do
                            if (circumstances == null)
                                return null;

                            // Separate bonuses from cover from bonuses other circumstance bonuses
                            string[] coverStrings = ["lesser cover", "cover", "greater cover"];
                            int highestCover = 0;
                            int highestOther = 0;
                            foreach (Bonus bonus in circumstances)
                            {
                                if (coverStrings.Contains(bonus.BonusSource.ToLower()))
                                    highestCover = Math.Max(highestCover, bonus.Amount);
                                else
                                    highestOther = Math.Max(highestOther, bonus.Amount);
                            }
                            // If cover is higher, then only bypass it enough to equate to the other bonuses.
                            // Thus, subtract the lower "other"s from the cover bonuses.
                            int bypassCover = highestCover - highestOther;

                            // If the other bonuses were higher, then ignoring cover doesn't do anything,
                            // So return null here.
                            if (highestCover == 0 || bypassCover <= 0)
                                return null;

                            // Return a bonus which will equate to bypassing as much cover as exists,
                            // Up to the next highest non-cover bonuses.
                            return new Bonus(bypassCover, BonusType.Untyped, "Wild winds stance");
                        };
                    });
                return wildWindsSpell;
                
                Item WeaponProducer() =>
                    new Item(icon, "wind crash", [Trait.Agile, Trait.Brawling, Trait.Nonlethal, Trait.Propulsive, Trait.Unarmed, Trait.UnaffectedByConcealment])
                        .WithDescription("{Blue}This ignores cover and concealment.{/Blue}")
                        //.WithSoundEffect(SfxName.AirSpell)
                        .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning)
                            {
                                VfxStyle = new VfxStyle(1, ProjectileKind.Arrow, icon),
                                Sfx = SfxName.AirSpell,
                            }
                            .WithRangeIncrement(6)
                            .WithMaximumRange(6));
            });
        Feat wildWindsInitiate = CreateKiSpellFeat2(
            ModData.FeatNames.WildWindsInitiate,
            8,
            "You learn a mystical stance that lets you attack from a distance.",
            "While entering the stance is a ki spell, the wind crash Strikes the stance grants are not, so you can use them as often as you like while in the stance.\n\n{icon:YellowWarning} {b}Implementation{/b} These Strikes ignore cover by providing an untyped attack bonus. Some roll displays might seem odd due to other circumstance bonuses applying that aren't shown.",
            "Wild Winds Stance",
            ModData.SpellIds.WildWindsStance,
            ModData.Illustrations.WildWindsStance,
            true);
        wildWindsInitiate.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return wildWindsInitiate;

        // Jellyfish Stance
        Feat jellyfishStance = CreateMonkStance2(
            ModData.FeatNames.JellyfishStance,
            "Jellyfish Stance",
            8,
            ModData.QEffectIds.JellyfishStance,
            "You relax your posture and loosen your joints, allowing yourself to move with incredible fluidity.",
            "While in this stance, you gain a +2 circumstance bonus to Reflex saves and on checks to Escape." /*and Squeeze*/,
            "You can make stinging lash attacks and have a +2 circumstance bonus to Reflex saves and to Escape.",
            () => new Item(IllustrationName.OchreJelly, "stinging lash", [Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Reach, Trait.Unarmed])
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing)),
            false,
            qfStance =>
            {
                qfStance.BonusToDefenses = (qfThis, action, def) =>
                    def is Defense.Reflex ? new Bonus(2, BonusType.Circumstance, "Jellyfish stance") : null;
                qfStance.BonusToSkillChecks = (skill, action, target) =>
                    action.ActionId is ActionId.Escape ? new Bonus(2, BonusType.Circumstance, "Jellyfish stance") : null;
            });
        jellyfishStance.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return jellyfishStance;

        // Tangled Forest Stance
        Feat tangledStance = CreateMonkStance2(
            ModData.FeatNames.TangledForestStance,
            "Tangled Forest Stance",
            8,
            ModData.QEffectIds.TangledForestStance,
            "You extend your arms like gnarled branches to interfere with your foes’ movements.",
            "Every enemy in your reach that tries to move away from you must succeed at a Reflex save, Acrobatics check, or Athletics check against your class DC or be immobilized for that action. If you prefer, you can allow the enemy to move.",
            "You can make lashing branch attacks and can prevent enemies from moving away from you.",
            () => new Item(IllustrationName.ProtectorTree, "lashing branch", [Trait.Agile, Trait.Brawling, Trait.Finesse, Trait.Nonlethal, Trait.Unarmed])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Slashing)),
            false,
            qfStance =>
            {
                qfStance.AddGrantingOfTechnical(
                    cr => cr.EnemyOf(qfStance.Owner)
                          && cr.DistanceTo(qfStance.Owner) <= (qfStance.Owner.PrimaryWeaponIncludingRanged != null && qfStance.Owner.PrimaryWeaponIncludingRanged.HasTrait(Trait.Reach) ? 2 : 1),
                    qfTech =>
                    {
                        qfTech.Name = "Tangled Forest Interference";
                        qfTech.FizzleOutgoingActions = async (qfThis, action, strBuild) =>
                        {
                            Creature monk = qfStance.Owner;
                            Creature mover = qfThis.Owner;
                            
                            // TODO: only if moving further away
                            if (!action.HasTrait(Trait.Move)
                                || (action.ChosenTargets.ChosenTile is {} tile && monk.Occupies.DistanceTo(tile) <= monk.DistanceTo(mover)))
                                return false;

                            if (!await monk.AskForConfirmation(IllustrationName.ProtectorTree,
                                    "{b}Tangled Forest Stance{/b}\n{Blue}" + mover +
                                    "{/Blue} is attempting to move away from you. Allow them to pass?",
                                    "Disrupt",
                                    "Pass"))
                                return true;
                            
                            // Roll Data
                            CalculatedNumber.CalculatedNumberProducer bestRoll = Checks.BestRoll(
                                Checks.SavingThrow(Defense.Reflex),
                                TaggedChecks.SkillCheck(Skill.Acrobatics, Skill.Athletics).CalculatedNumberProducer);
                            CalculatedNumber classDC = new CalculatedNumber(
                                qfStance.Owner.ClassDC(),
                                "Class DC",
                                new List<Bonus?>());
                            ActiveRollSpecification rollSpec = new ActiveRollSpecification(
                                bestRoll,
                                (_, _, _) => classDC);
                            string name = "Tangled Forest Interference";
                            CheckBreakdown breakdown = CombatActionExecution.BreakdownAttackForTooltip(
                                CombatAction.CreateSimple(mover, name)
                                    .WithActiveRollSpecification(rollSpec),
                                monk);
                            CheckBreakdownResult breakdownResult = new CheckBreakdownResult(breakdown);
                            
                            // Code peeled out of RollCheck
                            //CheckResult result = CommonSpellEffects.RollCheck("Tangled Forest Interference", new ActiveRollSpecification(bestRoll, (_, _, _) => classDC), mover, monk);
                            
                            // Log data
                            //strBuild.AppendLine("{b}"+name+"{/b}");
                            strBuild.AppendLine("Every enemy in your reach that tries to move away from you must succeed at a Reflex save, Acrobatics check, or Athletics check against your class DC or be immobilized for that action. If you prefer, you can allow the enemy to move.\n");
                            strBuild.AppendLine(breakdown.DescribeWithFinalRollTotal(breakdownResult));
                            strBuild.AppendLine();
                            strBuild.Replace(action.Name, name); // Describe the effect being rolled against
                            
                            // PETR: Doesn't automatically log a success
                            if (breakdownResult.CheckResult >= CheckResult.Success)
                                action.Owner.Battle.Log(
                                    (rollSpec.TaggedDetermineBonus.InvolvedSkill != null ? "Check" : "Saving throw") + " passed.", name, strBuild.ToString());
                            
                            // Overhead
                            mover.Overhead(
                                breakdownResult.CheckResult.HumanizeTitleCase2(),
                                Color.LightBlue/*,
                                mover + " rolls " + breakdownResult.CheckResult.Greenify() + " on " + name + ".",
                                name,
                                breakdown.DescribeWithFinalRollTotal(breakdownResult)*/);
                            
                            // Result
                            return breakdownResult.CheckResult < CheckResult.Success;
                        };
                    });
            },
            true);
        tangledStance.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return tangledStance;
        
        // Clinging Shadows Stance
        ModData.SpellIds.ClingingShadowsStance = ModManager.RegisterNewSpell(
            "ClingingShadowsStance",
            4,
            (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                Illustration icon = ModData.Illustrations.ClingingShadowsStance;
                const string name = "Clinging Shadows Stance";
                const string shortDescription = "You gain a +2 circumstance bonus to Grapple and for creatures to Escape you.";
                string passiveBonus = "While in clinging shadows stance, you have a +2 circumstance bonus to Athletics checks to Grapple, and to the DC for creatures to Escape from you.";
                string describedAttack = "Also, you gain an additional attack option:\n"
                    + Monk.DescribeAttack(WeaponProducer())
                        .Replace("  ", " ");
                CombatAction clingingShadowsSpell = Spells.CreateModern(
                        icon,
                        name,
                        [ModData.Traits.MoreDedications, Trait.Evocation, Trait.Focus, Trait.Manipulate, Trait.Monk, ModData.Traits.Shadow, Trait.Stance, Trait.SomaticOnly],
                        "You adopt the stance of clinging shadows, shrouding your limbs in sticky smoke made of ki.",
                        "{b}Duration{/b} until you leave the stance\n\nEnter a stance.\n\n" + passiveBonus + "\n\n" + describedAttack + ".",
                        Target.Self()
                            .WithAdditionalRestriction(ModData.CommonRequirements.StanceRestriction(ModData.QEffectIds.ClingingShadowsStance)),
                        4,
                        null)
                    .WithShortDescription("Enter a stance where " + shortDescription.Uncapitalize())
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
                        QEffect stanceQF = KineticistCommonEffects.EnterStance(
                            self,
                            icon,
                            name,
                            shortDescription,
                            ModData.QEffectIds.ClingingShadowsStance);
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
                    new Item(icon, "shadow grasp", [Trait.Agile, Trait.Brawling, Trait.Grapple, Trait.Reach, Trait.Unarmed])
                        // SFX I like:
                        // - Necromancy
                        // - MajorNegative
                        //.WithSoundEffect(SfxName.MajorNegative) // Abandoned since these can play too rapidly, don't have an appropriately subtle shadowy sound.
                        .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Negative));
            });
        Feat clingingShadowsInitiate = CreateKiSpellFeat2(
            ModData.FeatNames.ClingingShadowsInitiate,
            8,
            "You learn a mystical stance that transforms your ki into sticky smoke that shrouds your limbs, clinging to everything you touch.",
            "While entering the stance is a ki spell, the shadow grasp Strikes the stance grants are not, so you can use them as often as you like while in the stance.",
            "Clinging Shadows Stance",
            ModData.SpellIds.ClingingShadowsStance,
            ModData.Illustrations.ClingingShadowsStance,
            true);
        clingingShadowsInitiate.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return clingingShadowsInitiate;
    }
    
    /// Exists to resolve issues with the way QEffectIds are referenced for generating strings, which don't work with modded QFids (unlike base game IDs).
    public static Feat CreateMonkStance2(
        FeatName featName,
        string displayName,
        int level,
        QEffectId stanceId,
        string flavorText,
        string passiveBonus,
        string shortStanceDescription,
        Func<Item>? weaponProducer,
        bool useNewAttackExclusively,
        Action<QEffect> additionalActionOnStance,
        bool requiresBeingUnarmored = true)
    {
        Item? weapon = weaponProducer?.Invoke() ?? null;
        Illustration icon = weapon?.Illustration ?? IllustrationName.Action;
        string description = "Enter a stance.\n\n" + passiveBonus + "\n\n";
        description = !useNewAttackExclusively ? description + "Also, you gain an additional attack option:\n" + Monk.DescribeAttack(weaponProducer()) : description + "Also, the only Strike you can make is this attack:\n" + Monk.DescribeAttack(weaponProducer());
        description = !requiresBeingUnarmored ? description + "\n\nUnlike most monk stances, you can enter this stance even if you're wearing armor." : description + "\n\nYou can't enter this stance if you're wearing armor.";
        Feat stanceFeat = new TrueFeat(
            featName,
            level,
            flavorText,
            description,
            [Trait.Monk, Trait.Stance])
            .WithActionCost(1)
            .WithPermanentQEffect(qf =>
                qf.ProvideActionIntoPossibilitySection = (qfSelf, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.MainActions)
                        return null;

                    CombatAction enterStance = new CombatAction(
                        qfSelf.Owner,
                        icon,
                        displayName, //featName.HumanizeTitleCase2(),
                        [Trait.Monk, Trait.Stance],
                        description,
                        Target.Self()
                            .WithAdditionalRestriction(self =>
                            {
                                if (self.HasEffect(stanceId))
                                    return "You're already in this stance.";
                                return self.Armor.WearsArmor & requiresBeingUnarmored
                                    ? "You're wearing armor."
                                    : null;
                            }))
                        {
                            ShortDescription = "Enter a stance where " + shortStanceDescription.Uncapitalize()
                        }
                        .WithActionCost(1)
                        .WithEffectOnSelf(async self =>
                        {
                            QEffect qeffect = KineticistCommonEffects.EnterStance(
                                self,
                                icon,
                                displayName, //featName.HumanizeTitleCase2(),
                                shortStanceDescription,
                                stanceId);
                            if (useNewAttackExclusively)
                            {
                                qeffect.PreventTakingAction = action =>
                                    !action.HasTrait(Trait.Strike) ||
                                    action.Item != null && action.Item.ItemName == weapon.ItemName
                                        ? null
                                        : "You can only make " + weapon.Name.ToLower() + " attacks.";
                                qeffect.StateCheck += qfThisStance =>
                                {
                                    if (weaponProducer != null)
                                        qfThisStance.Owner.ReplacementUnarmedStrike = weaponProducer();
                                    qfThisStance.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                    {
                                        Id = QEffectId.PreventsUseOfPrimaryWeaponAttacks
                                    });
                                };
                            }
                            else
                                qeffect.AdditionalUnarmedStrike = weaponProducer();

                            additionalActionOnStance(qeffect);
                        });
                    return new ActionPossibility(enterStance)
                    {
                        PossibilityGroup = "Enter a stance"
                    };
                })
            .WithIllustration(icon);
        return stanceFeat;
    }

    /// Exists to resolve issues with needing to create feats that reference spells which haven't been added yet by the mod manager process.
    public static Feat CreateKiSpellFeat2(
        FeatName featName,
        int level,
        string flavorText,
        string rulesText,
        string spellName,
        SpellId spellId,
        Illustration icon,
        bool mustHaveFocusPool = false)
    {
        string description = "You gain the {i}" + spellName.ToLower() + "{/i} ki spell. " + (mustHaveFocusPool
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