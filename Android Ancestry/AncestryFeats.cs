using System.Linq.Expressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace AndroidAncestry;

public static class AncestryFeats
{
    public static void LoadFeats()
    {
        Feat androidLore = new TrueFeat(
            Enums.FeatNames.AndroidLore,
            1,
            "You have a keen interest in the origins of your people.",
            "You gain the trained proficiency rank in Crafting and Thievery. If you would automatically become trained in one of those skills (from your background or class, for example), you instead become trained in a skill of your choice.",
            [Enums.Traits.AndroidAncestry])
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
                values.TrainInThisOrSubstitute(Skill.Thievery);
            });
        ModManager.AddFeat(androidLore);

        // TODO: Test this feat
        Feat cleansingSubroutine = new TrueFeat(
            Enums.FeatNames.CleansingSubroutine,
            1,
            "Your nanites help purge your body of harmful chemicals and toxins.",
            "Each time you succeed at a Fortitude save against a poison affliction, you reduce its stage by 2"/*, or by 1 against a virulent poison*/+". Each critical success you achieve against a poison affliction reduces its stage by 3"/*, or by 2 against a virulent poison*/+".",
            [Enums.Traits.AndroidAncestry])
            .WithPermanentQEffect("Reduce poisons you save against by an additional stage.",
                qfFeat =>
                {
                    // Wow, that was easy.
                    qfFeat.BeforeYourSavingThrow = async (qfThis, action, self) =>
                    {
                        qfFeat.Id = (action.SavingThrow!.Defense == Defense.Fortitude || !action.HasTrait(Trait.Poison) && AndroidAncestry.CanUseNanites(self))
                            ? QEffectId.StrongBloodedDwarf
                            : QEffectId.Unspecified;
                    };
                });
        ModManager.AddFeat(cleansingSubroutine);
        
        // TODO: Test this feat
        Feat emotionless = new TrueFeat(
            Enums.FeatNames.Emotionless,
            1,
            "Your malfunctioning emotional processors make it difficult for you to feel strong emotions.",
            "You gain a +1 circumstance bonus to saving throws against emotion and fear effects. If you roll a success on a saving throw against an emotion or fear effect, you get a critical success instead.",
            [Enums.Traits.AndroidAncestry])
            .WithPermanentQEffect("You're more likely to succeed and critically succeed at saving throws against emotion and fear effects.",
                qfFeat =>
                {
                    qfFeat.BonusToDefenses = (qfThis, action, defense) =>
                    {
                        if (action != null && (action.HasTrait(Trait.Emotion) || action.HasTrait(Trait.Fear)) && defense != Defense.AC)
                        {
                            return new Bonus(1, BonusType.Circumstance, "Emotionless");
                        }

                        return null;
                    };
                    qfFeat.AdjustSavingThrowCheckResult = (qfThis, defense, action, result) =>
                    {
                        if (result == CheckResult.Success && (action.HasTrait(Trait.Emotion) || action.HasTrait(Trait.Fear)))
                        {
                            return CheckResult.CriticalSuccess;
                        }

                        return result;
                    };
                });
        ModManager.AddFeat(emotionless);
        
        Feat internalCompartment = new TrueFeat(
            Enums.FeatNames.InternalCompartment,
            1,
            "You can hide a small object inside a hollow cavity on one of your forearms.",
            "The first item you draw or replace each combat is a {icon:FreeAction} free action instead of the usual cost. The item must not require two hands to wield.",
            [Enums.Traits.AndroidAncestry])
            .WithPermanentQEffect("The first item you draw or replace is a free action.", qfFeat =>
            {
                qfFeat.Id = Enums.QEffectIds.InternalCompartment;
                qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                {
                    string name = action.Name.ToLower();
                    if ((!name.StartsWith("draw") && !name.StartsWith("replace")) || action.Item is { TwoHanded: true })
                        return;

                    qfThis.Owner.RemoveAllQEffects(qf => qf == qfThis);
                };
                
                ModManager.RegisterActionOnEachActionPossibility(action =>
                {
                    if (action.Owner.HasEffect(Enums.QEffectIds.InternalCompartment) && (action.Name.ToLower().StartsWith("draw") || action.Name.ToLower().StartsWith("replace")) && action.Item is not { TwoHanded: true })
                    {
                        action.Description = action.Description.Insert(0, "{Blue}{b}Internal Compartment{/b} Once per combat, this is a free action instead of the usual cost.{/Blue}\n\n");
                        action.ActionCost = 0;
                    }
                });
            });
        ModManager.AddFeat(internalCompartment);

        Feat naniteSurge = new TrueFeat(
            Enums.FeatNames.NaniteSurge,
            1,
            "You stimulate your nanites, forcing your body to temporarily increase its efficiency.",
            "{b}Frequency{/b} once per combat.\n{b}Trigger{/b} You attempt a skill check requiring three actions or fewer.\n\nYou gain a +2 status bonus to the triggering skill check. {i}(Cosmetic){/i} In addition, your circuitry glows, lighting a 10-foot emanation with dim light for 1 round.",
            [Enums.Traits.AndroidAncestry, Trait.Concentrate])
            .WithActionCost(-2)
            .WithPermanentQEffect("As a reaction once per combat, boost a skill check you're about to attempt.", qfFeat =>
            {
                qfFeat.BeforeYourActiveRoll = async (qfThis, action, target) =>
                {
                    Creature self = qfThis.Owner;
                    
                    if (action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is not { } skill)
                        return;
                    
                    if (self.HasEffect(Enums.QEffectIds.NaniteSurgeImmunity) || !AndroidAncestry.CanUseNanites(self))
                        return;
                    
                    if (action.ActionCost is < 1 or > 3)
                        return;
                    
                    await AndroidAncestry.AskToUseNanitesReaction(
                        self,
                        new QEffect(ExpirationCondition.Never)
                        {
                            BonusToSkillChecks = (skill2, combatAction, target2) =>
                            {
                                if (combatAction != action || skill2 != skill)
                                    return null;
                                return new Bonus(2, BonusType.Status, "nanite surge");
                            },
                            AfterYouTakeAction = async (qfThis2, combatAction) =>
                            {
                                if (combatAction == action)
                                    qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                            },
                        },
                        skill.HumanizeTitleCase2()+" check",
                        "+2 status bonus");
                };
            });
        ModManager.AddFeat(naniteSurge);
        
        Feat ultravisualAdaptation = new TrueFeat(
            Enums.FeatNames.UltravisualAdaptation,
            1,
            "The nanites in your ocular processors have adapted to alternative visual spectra.",
            $"You can cast {AllSpells.CreateSpellLink(SpellId.SeeInvisibility, Enums.Traits.AndroidAncestry)} once per day as a 2nd-level arcane innate spell.",
            [Enums.Traits.AndroidAncestry])
            .WithIllustration(IllustrationName.SeeInvisibility)
            .WithRulesBlockForSpell(SpellId.SeeInvisibility, Enums.Traits.AndroidAncestry)
            .WithOnCreature(cr =>
            {
                SpellcastingSource source = cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, Enums.Traits.AndroidAncestry, Ability.Charisma, Trait.Arcane);
                source.WithSpells([SpellId.SeeInvisibility], 2);
                if (source.Spells.Find(ca => ca.SpellId == SpellId.SeeInvisibility) is { } seeInvis)
                {
                    // TODO: Does not work
                    seeInvis.WithExtraTrait(Enums.Traits.Nanites);
                    seeInvis.Name = "Blob";
                }
            });
        ModManager.AddFeat(ultravisualAdaptation);

        Feat proximityAlert = new TrueFeat(
            Enums.FeatNames.ProximityAlert,
            1,
            "You're unnaturally in tune with your surroundings and react instinctively to danger.",
            "You gain the incredible initiative general feat.",
            [Enums.Traits.AndroidAncestry])
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.IncredibleInitiative);
            })
            .WithEquivalent(values => values.HasFeat(FeatName.IncredibleInitiative));
        ModManager.AddFeat(proximityAlert);

        // BUG: Doesn't announce overheads? Nbd but it should probably look a lot like Bless.
        Feat radiantCircuitry = new TrueFeat(
            Enums.FeatNames.RadiantCircuitry,
            1,
            "Your biological circuitry emits light like a torch.",
            "You create a 20-foot emanation of light. Creatures in this emanation have a -1 circumstance penalty to Stealth checks.\n\nThe light shuts off when you take this action again or are knocked unconscious.",
            [Enums.Traits.AndroidAncestry])
            .WithActionCost(1)
            .WithPermanentQEffect("You can toggle a 20-foot emanation of light which penalizes Stealth.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction radiantAction = new CombatAction(
                        qfThis.Owner,
                        Enums.Illustrations.RadiantCircuitry,
                        "Radiant Circuitry " + (qfThis.Owner.HasEffect(Enums.QEffectIds.RadiantCircuitry) ? "(off)" : "(on)"),
                        [Enums.Traits.AndroidAncestry, Trait.Concentrate, Trait.Light, Trait.Basic],
                        "{i}Your biological circuitry emits light like a torch.{/i}\n\n"+"You create a 20-foot emanation of light. Creatures in this emanation have a -1 circumstance penalty to Stealth checks.\n\nThe light shuts off when you take this action again or are knocked unconscious.",
                        Target.Self())
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.PowerfulLight)
                        .WithEffectOnSelf(self =>
                        {
                            switch (self.HasEffect(Enums.QEffectIds.RadiantCircuitry))
                            {
                                case true:
                                    self.RemoveAllQEffects(qf => qf.Id == Enums.QEffectIds.RadiantCircuitry);
                                    break;
                                case false:
                                    QEffect radiantEffect = new QEffect(
                                            "Radiant Circuitry",
                                            "Creatures in a 20-foot emanation have a -1 circumstance penalty to Stealth checks.",
                                            ExpirationCondition.Never,
                                            self,
                                            Enums.Illustrations.RadiantCircuitry)
                                        {
                                            Id = Enums.QEffectIds.RadiantCircuitry,
                                            DoNotShowUpOverhead = true,
                                            SpawnsAura = qfThis2 => new MagicCircleAuraAnimation(IllustrationName.AngelicHaloCircle, Color.Gold, 4f),
                                            AfterYouAcquireEffect = async (qfThis2, qfAcquired) =>
                                            {
                                                if (qfAcquired.Id == QEffectId.Unconscious)
                                                {
                                                    qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                                    qfThis2.Owner.Battle.Log("  " + qfThis.Owner.Name + " loses radiant circuitry due to unconsciousness.");
                                                }
                                            },
                                
                                        }
                                        .AddGrantingOfTechnical(
                                            cr => cr.DistanceTo(self) <= 4 && !cr.HasEffect(QEffectId.OutOfCombat), qfTech =>
                                            {
                                                qfTech.Name = "Radiant Circuitry's Light";
                                                qfTech.Description = "You have a -1 circumstance penalty to Stealth checks.";
                                                qfTech.Illustration = IllustrationName.Light;
                                                qfTech.CountsAsADebuff = true;
                                                qfTech.BonusToSkills = skill => skill is Skill.Stealth ? new Bonus(-1, BonusType.Circumstance, "radiant circuitry") : null;
                                            });
                                    self.AddQEffect(radiantEffect);
                                    break;
                            }
                        });
                    
                    return new ActionPossibility(radiantAction, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(radiantCircuitry);

        Feat advancedTargetingSystem = new TrueFeat(
            Enums.FeatNames.AdvancedTargetingSystem,
            5,
            "Your ocular processors are augmented with advanced targeting systems, which allow you to more easily pinpoint your enemy and read their movements.",
            $"You can cast {AllSpells.CreateSpellLink(SpellId.TrueStrike, Enums.Traits.AndroidAncestry)} once per day as a 1st-level arcane innate spell.",
            [Enums.Traits.AndroidAncestry])
            .WithIllustration(IllustrationName.TrueStrike)
            .WithRulesBlockForSpell(SpellId.TrueStrike, Enums.Traits.AndroidAncestry)
            .WithOnCreature(cr =>
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, Enums.Traits.AndroidAncestry, Ability.Charisma, Trait.Arcane)
                    .WithSpells([SpellId.TrueStrike], 1);
            });
        ModManager.AddFeat(advancedTargetingSystem);

        // PETR: Inoculation Subroutine. Disease afflictions are not well-supported at this time. Main issue is that AdjustValue is built for poisons with hard-coded integration with Strong-Blooded Dwarf, meaning there are unreliable integrations and an inaccessibility of stage reductions other than poisons.
        // If implemented, check for Nanite Shroud blockage.
        // Feat inoculationSubroutine = new TrueFeat(
        //     Enums.FeatNames.InoculationSubroutine,);
        // ModManager.AddFeat(inoculationSubroutine);

        Feat naniteShroud = new TrueFeat(
            Enums.FeatNames.NaniteShroud,
            5,
            "Your nanites fly out of your body, swarming around you in a cloud.",
            "{b}Frequency{/b} once per day\n\nYou become concealed for a number of rounds equal to half your level (you can't use this concealment to Hide or Sneak) or until you dismiss it.\n\nWhile Nanite Shroud is active, you can't use other abilities that require the use of your nanites (any feat whose text mentions nanites).",
            [Enums.Traits.AndroidAncestry, Trait.Concentrate])
            .WithActionCost(2)
            .WithPermanentQEffect("Once per day, you can become concealed temporarily.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains("NaniteShroud"))
                        return null;
                    
                    CombatAction shroudAction = new CombatAction(
                        qfThis.Owner,
                        IllustrationName.ChillingDarkness, // TODO
                        "Nanite Shroud",
                        [Enums.Traits.AndroidAncestry, Trait.Concentrate, Trait.Basic],
                        "{i}Your nanites fly out of your body, swarming around you in a cloud.{/i}\n\n{b}Frequency{/b} once per day\n\nYou become concealed for {Blue}" + qfThis.Owner.Level/2 + "{/Blue} rounds (you can't use this concealment to Hide or Sneak) or until you dismiss it.\n\nWhile Nanite Shroud is active, you can't use other abilities that require the use of your nanites.",
                        Target.Self())
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.InvisibilityPoor)
                        .WithEffectOnSelf(self =>
                        {
                            QEffect naniteShroud = new QEffect(
                                "Nanite Shroud",
                                "You are concealed for a number of rounds equal to this effect's value.\n\nYou can't use this concealment to Hide or Sneak, and you can't use abilities that require the use of your nanites.",
                                ExpirationCondition.CountsDownAtStartOfSourcesTurn,
                                self,
                                IllustrationName.ChillingDarkness) // TODO
                            {
                                Value = qfThis.Owner.Level / 2,
                                Id = QEffectId.Blur,
                                Tag = Enums.QEffectIds.NanitesUnusable,
                                Dismissable = true, // Homebrew
                                SpawnsAura = qfThis2 =>
                                    new MagicCircleAuraAnimation(IllustrationName.BaneCircle, Color.DarkSlateGray,
                                        0.5f), // TODO: improve this
                                PreventTakingAction = action =>
                                    action.HasTrait(Enums.Traits.Nanites) ||
                                    (action.SpellcastingSource?.ClassOfOrigin == Enums.Traits.AndroidAncestry && action.SpellId == SpellId.SeeInvisibility) ? "Blocked by nanite shroud" : null,
                                // Homebrew:
                                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                                    section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers
                                        ? new ActionPossibility(new CombatAction(self, IllustrationName.DismissAura,
                                            "Dismiss Nanite Shroud", [Trait.Concentrate], "Dismiss nanite shroud.",
                                            Target.Self()).WithEffectOnSelf(
                                            cr => qfThis.ExpiresAt = ExpirationCondition.Immediately ))
                                        : null
                            };
                            self.AddQEffect(naniteShroud);
                            self.RemoveAllQEffects(qf => qf.Traits.Contains(Enums.Traits.Nanites));
                            self.PersistentUsedUpResources.UsedUpActions.Add("NaniteShroud");
                        });
                    
                    return new ActionPossibility(shroudAction, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(naniteShroud);

        Feat protectiveSubroutine = new TrueFeat(
            Enums.FeatNames.ProtectiveSubroutine,
            5,
            "Your nanites can augment your defenses.",
            "You can also activate Nanite Surge {icon:Reaction} when you attempt a saving throw. If you do, you gain a +2 status bonus to the triggering saving throw.",
            [Enums.Traits.AndroidAncestry])
            .WithPermanentQEffect("You can use Nanite Surge {icon:Reaction} with saving throws.", qfFeat =>
            {
                qfFeat.BeforeYourSavingThrow = async (qfThis, action, self) =>
                {
                    Defense save = action.SavingThrow!.Defense;
                    
                    if (self.HasEffect(Enums.QEffectIds.NaniteSurgeImmunity) || !AndroidAncestry.CanUseNanites(self))
                        return;
                    
                    await AndroidAncestry.AskToUseNanitesReaction(
                        self,
                        new QEffect(ExpirationCondition.Never)
                        {
                            BonusToDefenses = (qfThis2, combatAction, def) =>
                            {
                                if (combatAction != action || def != save)
                                    return null;
                                return new Bonus(2, BonusType.Status, "nanite surge");
                            },
                            AfterYouMakeSavingThrow = async (qfThis2, combatAction, result) => 
                            {
                                if (combatAction == action)
                                    qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                            },
                        },
                        save.HumanizeTitleCase2()+" saving throw",
                        "+2 status bonus");
                };
                
            })
            .WithPrerequisite(Enums.FeatNames.NaniteSurge,"Nanite Surge");
        ModManager.AddFeat(protectiveSubroutine);
    }

    
}