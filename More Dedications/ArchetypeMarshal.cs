using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeMarshal
{
    public static readonly Func<Creature, Creature, Usability> IsInMarshalAura = (_, defender) =>
        defender.HasEffect(ModData.QEffectIds.MarshalsAuraEffect)
            ? Usability.Usable
            : Usability.NotUsableOnThisCreature("not in marshal aura");
    
    public static void LoadMod()
    {
        Feat marshalDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
            ModData.Traits.MarshalArchetype,
            "Marshals are leaders, first and foremost. Marshals can come from any class or background, though they all share a willingness to sacrifice their own glory for the greater good of the team.",
            "Choose Diplomacy or Intimidation. You become trained in that skill or become an expert if you were already trained in it.\n\nIn addition, you're surrounded by a marshal's aura in a 10-foot emanation. Your aura has the emotion, mental, and visual traits and grants you and allies within the aura a +1 status bonus to saving throws against fear.")
            .WithOnSheet(values =>
            {
                List<FeatName> options =
                [
                    values.HasFeat(FeatName.Diplomacy)
                        ? FeatName.ExpertDiplomacy
                        : FeatName.Diplomacy,
                    values.HasFeat(FeatName.Intimidation)
                        ? FeatName.ExpertIntimidation
                        : FeatName.Intimidation
                ];
                values.AddSelectionOptionRightNow(new SingleFeatSelectionOption("Marshal.DedicationSkill", "Marshal skill", values.CurrentLevel, feat => options.Contains(feat.FeatName)));
            })
            .WithPermanentQEffect("You have a marshal's aura which protects allies from fear effects.",
                qfFeat =>
                {
                    qfFeat.Name = "Marshal's Aura";
                    qfFeat.Id = ModData.QEffectIds.MarshalsAuraProvider;
                    qfFeat.Tag = 2; // aura's range.
                    qfFeat.SpawnsAura = qfThis =>
                    {
                        float size = qfThis.Tag as int? ?? 0f;
                        return new MagicCircleAuraAnimation(IllustrationName.KineticistAuraCircle, Color.Azure, size)
                            { MaximumOpacity = 0.75f };
                    };
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            cr.FriendOf(qfFeat.Owner) && !cr.IsImmuneTo(Trait.Emotion) && !cr.IsImmuneTo(Trait.Mental) && !cr.IsImmuneTo(Trait.Visual) && cr.DistanceTo(qfFeat.Owner) <= (qfFeat.Tag as int? ?? 0),
                        qfTech =>
                        {
                            qfTech.Name = "Marshal's Aura";
                            qfTech.Description = "You have a +1 status bonus to saving throws against fear.";
                            qfTech.Illustration = IllustrationName.InspireCourage;
                            qfTech.Id = ModData.QEffectIds.MarshalsAuraEffect;
                            qfTech.BonusToDefenses = (qfThis, action, def) =>
                                def.IsSavingThrow() && action != null && action.HasTrait(Trait.Fear)
                                    ? new Bonus(1, BonusType.Status, "marshal's aura")
                                    : null;
                        });
                })
            .WithPrerequisite(values =>
                values.GetProficiency(Trait.Martial) > Proficiency.Untrained,
                "Must be trained in martial weapons")
            .WithPrerequisite(values =>
                values.HasFeat(FeatName.Diplomacy) || values.HasFeat(FeatName.Intimidation), "Must be trained in Diplomacy or Intimidation");
        ModManager.AddFeat(marshalDedication);
        
        // Dread Marshal Stance
        Feat dreadStance = new TrueFeat(
                ModData.FeatNames.DreadMarshalStance,
                4,
                "Putting on a grim face for the battle ahead, you encourage your allies to strike fear into their foes with vicious attacks.",
                "Attempt an Intimidation check. The DC is a standard-difficulty DC of your level. The effect depends on the result of your check."+S.FourDegreesOfSuccess(
                    "Your marshal's aura increases to a 20-foot emanation, and it grants you and allies a status bonus to damage rolls equal to the number of weapon damage dice of the unarmed attack or weapon you are wielding that has the most weapon damage dice. When you or an ally in the aura critically hits an enemy with a Strike, that enemy is frightened 1.",
                    "As critical success, but your aura's size doesn't increase.",
                    "You fail to enter the stance.",
                    "You fail to enter the stance and can't take this action again for the rest of the encounter."),
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Open, Trait.Stance])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MarshalArchetype)
            .WithPermanentQEffect("Enter a stance where your allies deal bonus damage and can frighten enemies with their Strikes.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.HasEffect(ModData.QEffectIds.DreadMarshalStance))
                        return null;

                    CombatAction enterStance = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.DreadMarshalStance,
                            "Dread Marshal Stance",
                            [Trait.Archetype, Trait.Open, Trait.Stance],
                            "{i}Putting on a grim face for the battle ahead, you encourage your allies to strike fear into their foes with vicious attacks.{/i}\n\n"
                            + $"Attempt a {ModData.Tooltips.LeveledDC("DC " + Checks.LevelBasedDC(qfThis.Owner.Level))} Intimidation check." +
                            S.FourDegreesOfSuccess(
                                "Your marshal's aura increases to a 20-foot emanation, and it grants you and allies a status bonus to damage rolls equal to the number of weapon damage dice of the unarmed attack or weapon you are wielding that has the most weapon damage dice. When you or an ally in the aura critically hits an enemy with a Strike, that enemy is frightened 1.",
                                "As critical success, but your aura's size doesn't increase.",
                                "You fail to enter the stance.",
                                "You fail to enter the stance and can't take this action again for the rest of the encounter."),
                            Target.Self())
                        .WithActionCost(1)
                        .WithActiveRollSpecification(
                            new ActiveRollSpecification(
                                TaggedChecks.SkillCheck(Skill.Intimidation),
                                new TaggedCalculatedNumberProducer((_, _, _) =>
                                    new CalculatedNumber(Checks.LevelBasedDC(qfThis.Owner.Level), "Level-based DC",
                                        []))))
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        {
                            switch (result)
                            {
                                case >= CheckResult.Success:
                                {
                                    // Increase the aura on a crit
                                    if (result == CheckResult.CriticalSuccess && caster.FindQEffect(ModData.QEffectIds.MarshalsAuraProvider) is { } aura)
                                    {
                                        aura.Tag = 4;
                                        aura.AssociatedAura?.MoveTo(aura.Tag as int? ?? 0f);
                                    }
                                
                                    // Normal effects
                                    QEffect dmStance = KineticistCommonEffects.EnterStance(
                                            qfThis.Owner,
                                            ModData.Illustrations.DreadMarshalStance,
                                            "Dread Marshal Stance",
                                            "You and all allies in your marshal aura have a status bonus to damage rolls equal to the number of weapon dice of your best unarmed attack or of a weapon you're wielding. On a critical hit with a Strike, the target is frightened 1.",
                                            ModData.QEffectIds.DreadMarshalStance)
                                        .AddGrantingOfTechnical(
                                            cr => cr.HasEffect(ModData.QEffectIds.MarshalsAuraEffect),
                                            qfTech =>
                                            {
                                                qfTech.Name = "Dread Marshal Aura";
                                                qfTech.Description = $"You have a status bonus to damage rolls equal to the number of weapon dice of {qfThis.Owner}'s best unarmed attack or of a weapon they're wielding. On a critical hit with a Strike, the target is frightened 1.";
                                                qfTech.Illustration = ModData.Illustrations.DreadMarshalStance;
                                                qfTech.YouDealDamageEvent = async (qfThis2, dEvent) =>
                                                {
                                                    // Must deal rollable damage
                                                    if (!dEvent.KindedDamages.Any(kd => kd.DiceFormula is {} df && df.ToString().Contains('d')))
                                                        return;
                                                    int amount = ((List<Item>)[..caster.HeldItems, caster.UnarmedStrike]).Max(item => item.WeaponProperties?.DamageDieCount ?? 1);
                                                    dEvent.Bonuses.Add(new Bonus(amount, BonusType.Status, "dread marshal aura"));
                                                };
                                                qfTech.AfterYouTakeActionAgainstTarget =
                                                    async (qfThis2, action, target2, result2) =>
                                                    {
                                                        if (action.HasTrait(Trait.Strike) && result2 == CheckResult.CriticalSuccess)
                                                            target2.AddQEffect(QEffect.Frightened(1));
                                                    };
                                                qfTech.StateCheckLayer = 1;
                                            });
                                    dmStance.WhenExpires = async qfThis2 =>
                                    {
                                        if (caster.FindQEffect(ModData.QEffectIds.MarshalsAuraProvider) is not
                                            { } aura) return;
                                        aura.Tag = 2;
                                        aura.AssociatedAura?.MoveTo(aura.Tag as int? ?? 0f);
                                    };
                                    dmStance.HideFromPortrait = true;
                                    break;
                                }
                                // Can't use again if critical failure
                                case CheckResult.CriticalFailure:
                                    qfThis.Owner.RemoveAllQEffects(qf => qf == qfThis);
                                    break;
                            }
                        });
                    return new ActionPossibility(enterStance)
                    {
                        PossibilityGroup = "Enter a stance"
                    };
                };
            })
            .WithPrerequisite(values => values.HasFeat(FeatName.Intimidation), "Must be trained in Intimidation");
        ModManager.AddFeat(dreadStance);
        
        // Inspiring Marshal Stance
        Feat inspiringStance = new TrueFeat(
                ModData.FeatNames.InspiringMarshalStance,
                4,
                "You become a brilliant example of dedication and poise in battle, encouraging your allies to follow suit.",
                "Attempt a Diplomacy check. The DC is a standard-difficulty DC of your level. The effect depends on the result of your check."+S.FourDegreesOfSuccess(
                    "Your marshal's aura increases to a 20-foot emanation and grants you and allies a +1 status bonus to attack rolls and saves against mental effects.",
                    "As critical success, but your aura's size doesn't increase.",
                    "You fail to enter the stance.",
                    "You fail to enter the stance and can't take this action again for the rest of the encounter."),
                [ModData.Traits.MoreDedications, Trait.Archetype, Trait.Open, Trait.Stance])
            .WithActionCost(1)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MarshalArchetype)
            .WithPermanentQEffect("Enter a stance where your allies deal bonus damage and can frighten enemies with their Strikes.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.HasEffect(ModData.QEffectIds.InspiringMarshalStance))
                        return null;

                    CombatAction enterStance = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.InspiringMarshalStance,
                            "Inspiring Marshal Stance",
                            [Trait.Archetype, Trait.Open, Trait.Stance],
                            "{i}You become a brilliant example of dedication and poise in battle, encouraging your allies to follow suit.{/i}\n\n"
                            + $"Attempt a {ModData.Tooltips.LeveledDC("DC " + Checks.LevelBasedDC(qfThis.Owner.Level))} Diplomacy check." +
                            S.FourDegreesOfSuccess(
                                "Your marshal's aura increases to a 20-foot emanation and grants you and allies a +1 status bonus to attack rolls and saves against mental effects.",
                                "As critical success, but your aura's size doesn't increase.",
                                "You fail to enter the stance.",
                                "You fail to enter the stance and can't take this action again for the rest of the encounter."),
                            Target.Self())
                        .WithActionCost(1)
                        .WithActiveRollSpecification(
                            new ActiveRollSpecification(
                                TaggedChecks.SkillCheck(Skill.Diplomacy),
                                new TaggedCalculatedNumberProducer((_, _, _) =>
                                    new CalculatedNumber(Checks.LevelBasedDC(qfThis.Owner.Level), "Level-based DC",
                                        []))))
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        {
                            switch (result)
                            {
                                case >= CheckResult.Success:
                                {
                                    // Increase the aura on a crit
                                    if (result == CheckResult.CriticalSuccess && caster.FindQEffect(ModData.QEffectIds.MarshalsAuraProvider) is { } aura)
                                    {
                                        aura.Tag = 4;
                                        aura.AssociatedAura?.MoveTo(aura.Tag as int? ?? 0f);
                                    }
                                
                                    // Normal effects
                                    QEffect dmStance = KineticistCommonEffects.EnterStance(
                                            qfThis.Owner,
                                            ModData.Illustrations.InspiringMarshalStance,
                                            "Inspiring Marshal Stance",
                                            "You and all allies in your marsha's aura gain a +1 status bonus to attack rolls and saves against mental effects.",
                                            ModData.QEffectIds.InspiringMarshalStance)
                                        .AddGrantingOfTechnical(
                                            cr => cr.HasEffect(ModData.QEffectIds.MarshalsAuraEffect),
                                            qfTech =>
                                            {
                                                qfTech.Name = "Inspiring Marshal Aura";
                                                qfTech.Description = $"You have a +1 status bonus to attack rolls and saves against mental effects.";
                                                qfTech.Illustration = ModData.Illustrations.InspiringMarshalStance;
                                                qfTech.BonusToAttackRolls = (_,action,_) =>
                                                    action.HasTrait(Trait.Attack) ? new Bonus(1, BonusType.Status, "inspiring marshal aura") : null;
                                                qfTech.BonusToDefenses = (qfThis2, action, def) =>
                                                    def.IsSavingThrow() && action != null && action.HasTrait(Trait.Mental)
                                                        ? new Bonus(1, BonusType.Status, "inspiring marshal aura")
                                                        : null;
                                                qfTech.StateCheckLayer = 1;
                                            });
                                    dmStance.WhenExpires = async qfThis2 =>
                                    {
                                        if (caster.FindQEffect(ModData.QEffectIds.MarshalsAuraProvider) is not
                                            { } aura) return;
                                        aura.Tag = 2;
                                        aura.AssociatedAura?.MoveTo(aura.Tag as int? ?? 0f);
                                    };
                                    dmStance.HideFromPortrait = true;
                                    break;
                                }
                                // Can't use again if critical failure
                                case CheckResult.CriticalFailure:
                                    qfThis.Owner.RemoveAllQEffects(qf => qf == qfThis);
                                    break;
                            }
                        });
                    return new ActionPossibility(enterStance)
                    {
                        PossibilityGroup = "Enter a stance"
                    };
                };
            })
            .WithPrerequisite(values => values.HasFeat(FeatName.Diplomacy), "Must be trained in Diplomacy");
        ModManager.AddFeat(inspiringStance);
        
        // Snap Out of It! (Marshal) @lv4
        // Can this even be implemented?
        
        // Steel Yourself!
        Feat steelYourself = new TrueFeat(
                ModData.FeatNames.SteelYourself,
                4,
                "You encourage an ally to toughen up, giving them a fighting chance.",
                "Choose one ally within your marshal's aura. The ally gains temporary Hit Points equal to your Charisma modifier, as well as a +2 circumstance bonus to Fortitude saves which lasts until the start of your next turn."/*"The ally gains temporary Hit Points equal to your Charisma modifier and a +2 circumstance bonus to Fortitude saves. Both benefits last until the start of your next turn."*/, // PETR: Temp HP rework?
                [ModData.Traits.MoreDedications, Trait.Auditory, Trait.Emotion, Trait.Mental])
            .WithAvailableAsArchetypeFeat(ModData.Traits.MarshalArchetype)
            .WithPermanentQEffect("You give temporary HP and bonuses to Fortitude saves to an ally in your marshal's aura.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction steelAction = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.SteelYourself,
                            "Steel Yourself",
                            [Trait.Auditory, Trait.Emotion, Trait.Mental],
                            "{i}You encourage an ally to toughen up, giving them a fighting chance.{/i}\n\n" +
                            $"Choose one ally within your marshal's aura. The ally gains {{b}}+{qfThis.Owner.Abilities.Charisma}{{/b}} temporary Hit Points, as well as a +2 circumstance bonus to Fortitude saves which lasts until the start of your next turn.",
                            Target.RangedFriend(GetMarshalAuraRange(qfThis.Owner))
                                .WithAdditionalConditionOnTargetCreature(IsInMarshalAura))
                            .WithActionCost(1)
                            .WithSoundEffect(qfThis.Owner.HasTrait(Trait.Female) ? SfxName.TripFemale : SfxName.TripMale)
                            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                            {
                                target.GainTemporaryHP(caster.Abilities.Charisma);
                                target.AddQEffect(
                                    new QEffect(
                                        "Steel Yourself",
                                        "You have a +2 circumstance bonus to Fortitude saving throws.",
                                        ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                        caster,
                                        ModData.Illustrations.SteelYourself)
                                    {
                                        BonusToDefenses = (qfThis2, action, def) =>
                                            def is Defense.Fortitude
                                                ? new Bonus(2, BonusType.Circumstance, "steel yourself")
                                                : null,
                                        DoNotShowUpOverhead = true,
                                    });
                            });
                        return new ActionPossibility(steelAction, PossibilitySize.Full);
                    };
                });
        ModManager.AddFeat(steelYourself);
        
        // Cadence Call @lv6
        // Can this even be implemented?
        
        // Rallying Charge @lv6
        Feat rallyingCharge = new TrueFeat(
                ModData.FeatNames.RallyingCharge,
                6,
                "Your fearless charge into battle reinvigorates your allies to carry on the fight.",
                "You Stride up to your Speed and make a melee Strike. If your Strike hits and damages an enemy, each ally within 60 feet"+/*" who saw you hit"+*/" gains temporary Hit Points equal to your Charisma modifier."/*+" These temporary Hit Points last until the start of your next turn."*/,
                [Trait.Open, Trait.Visual])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MarshalArchetype)
            .WithPermanentQEffect("", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction charge = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.RallyingCharge,
                            "Rallying Charge",
                            [Trait.Open, Trait.Visual],
                            "{/i}Your fearless charge into battle reinvigorates your allies to carry on the fight.{/i}\n\nYou Stride up to your Speed and make a melee Strike. If your Strike hits and damages an enemy, each ally within 60 feet"/*+" who saw you hit"*/+$" gains {{b}}+{qfThis.Owner.Abilities.Charisma}{{/b}} temporary Hit Points."/*+" These temporary Hit Points last until the start of your next turn."*/,
                            Target.Self())
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Footsteps)
                        .WithEffectOnSelf(async (thisAction, self) =>
                        {
                            if (!await self.StrideAsync("Choose where to Stride with Rallying Charge. You should end your movement within melee reach of an enemy. (1/2)", allowCancel: true))
                            {
                                thisAction.RevertRequested = true;
                            }
                            else
                            {
                                QEffect preStrikeBuff = new QEffect()
                                {
                                    AfterYouDealDamage = async (cr, action, target) =>
                                    {
                                        if (!action.HasTrait(Trait.Strike) || !action.HasTrait(Trait.Melee))
                                            return;
                                        
                                        cr.Battle.AllCreatures.ForEach(cr2 =>
                                        {
                                            if (!cr2.FriendOf(cr) || cr2.DistanceTo(cr) > 12)
                                                return;
                                            
                                            cr2.GainTemporaryHP(cr.Abilities.Charisma);
                                            cr2.Occupies.Overhead(cr.Abilities.Charisma.WithPlus(), Color.Aquamarine);
                                        });
                                    }
                                };
                                self.AddQEffect(preStrikeBuff);
                                
                                if (!await CommonCombatActions.StrikeAdjacentCreature(self, null, true))
                                {
                                    self.Battle.Log("Rallying Charge was converted to a simple Stride.");
                                    thisAction.SpentActions = 1;
                                    thisAction.RevertRequested = true;
                                }

                                preStrikeBuff.ExpiresAt = ExpirationCondition.Immediately;
                            };
                        });
                    return new ActionPossibility(charge, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(rallyingCharge);
        
        // Attack of Opportunity
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.AttackOfOpportunity,
            ModData.Traits.MarshalArchetype,
            8)
            .WithEquivalent(values => values.HasFeat(FeatName.Fighter)));

        // Back to Back @lv8
        // Can this even be implemented?

        // To Battle!
        Feat toBattle = new TrueFeat(
            ModData.FeatNames.ToBattle,
            8,
            "With a resounding cry, you rally your ally to the offensive.",
            "Choose one ally within your marshal's aura who has a reaction available. If you spend 1 action, that ally can use their reaction to immediately Stride. If you spend 2 actions, that ally can use their reaction to immediately Strike.",
            [ModData.Traits.MoreDedications, Trait.Auditory, Trait.Flourish])
            .WithActionCost(-3)
            .WithAvailableAsArchetypeFeat(ModData.Traits.MarshalArchetype)
            .WithPermanentQEffect("", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction toBattleAction = new CombatAction(
                        qfThis.Owner,
                        ModData.Illustrations.ToBattle,
                        "To Battle!",
                        [Trait.Auditory, Trait.Flourish],
                        "{i}With a resounding cry, you rally your ally to the offensive.{/i}\n\nChoose one ally within your marshal's aura who has a reaction available. If you spend 1 action, that ally can use their reaction to immediately Stride. If you spend 2 actions, that ally can use their reaction to immediately Strike.",
                        Target.DependsOnActionsSpent(
                            Target.RangedFriend(GetMarshalAuraRange(qfThis.Owner))
                                .WithAdditionalConditionOnTargetCreature(IsInMarshalAura),
                            Target.RangedFriend(GetMarshalAuraRange(qfThis.Owner))
                                .WithAdditionalConditionOnTargetCreature(IsInMarshalAura),
                            null))
                        .WithActionCost(-3)
                        .WithSoundEffect(qfThis.Owner.HasTrait(Trait.Female) ? SfxName.Intimidate : SfxName.MaleIntimidate)
                        .WithCreateVariantDescription((actionCost, _) =>
                        {
                            if (actionCost == 1)
                                return "Choose one ally within your marshal's aura who has a reaction available. That ally can use their reaction to immediately {Blue}Stride{/Blue}.";
                            if (actionCost == 2)
                                return "Choose one ally within your marshal's aura who has a reaction available. That ally can use their reaction to immediately {Blue}Strike{/Blue}.";
                            else
                                throw new ArgumentException("Unknown action cost.");
                        })
                        .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
                        {
                            if (thisAction.SpentActions == 1)
                            {
                                if (!await target.AskToUseReaction("{b}To Battle!{b}\nUse your reaction to Stride?"))
                                {
                                    thisAction.RevertRequested = true;
                                    return;
                                }
                                
                                if (!await target.StrideAsync("Choose where to Stride.", allowCancel: true))
                                {
                                    target.Actions.RefundReaction();
                                    thisAction.RevertRequested = true;
                                    return;
                                }
                            }
                            else if (thisAction.SpentActions == 2)
                            {
                                if (!await target.AskToUseReaction("{b}To Battle!{b}\nUse your reaction to Strike?"))
                                {
                                    thisAction.RevertRequested = true;
                                    return;
                                }
                                
                                if (!await CommonCombatActions.StrikeAnyCreature(target, null, allowCancel: true))
                                {
                                    target.Actions.RefundReaction();
                                    thisAction.RevertRequested = true;
                                    return;
                                }
                            }
                        });
                    Possibility battlePossibility = Possibilities.CreateSpellPossibility(toBattleAction);
                    battlePossibility.PossibilitySize = PossibilitySize.Full;
                    return battlePossibility;
                };
            });
        ModManager.AddFeat(toBattle);
    }

    public static int GetMarshalAuraRange(Creature marshal)
    {
        return marshal.FindQEffect(ModData.QEffectIds.MarshalsAuraProvider)?.Tag as int? ?? 2;
    }
}