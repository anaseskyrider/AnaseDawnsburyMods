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
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
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
                ModData.FeatNames.AndroidLore,
                1,
                "You have a keen interest in the origins of your people.",
                "You gain the trained proficiency rank in Crafting and Thievery. If you would automatically become trained in one of those skills (from your background or class, for example), you instead become trained in a skill of your choice.",
                [ModData.Traits.AndroidAncestry])
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
                values.TrainInThisOrSubstitute(Skill.Thievery);
            });
        ModManager.AddFeat(androidLore);

        Feat cleansingSubroutine = new TrueFeat(
                ModData.FeatNames.CleansingSubroutine,
                1,
                "Your nanites help purge your body of harmful chemicals and toxins.",
                "Each time you succeed at a save against a poison affliction, you reduce its stage by 2. Each critical success you achieve against a poison affliction reduces its stage by 3.",    
                //"Each time you succeed at a Fortitude save against a poison affliction, you reduce its stage by 2, or by 1 against a virulent poison. Each critical success you achieve against a poison affliction reduces its stage by 3, or by 2 against a virulent poison.",
                [ModData.Traits.AndroidAncestry])
            .WithPermanentQEffect("Reduce poisons you save against by an additional stage.",
                qfFeat =>
                {
                    // PETR: Doesn't work in like a lot of cases. Any time RollSavingThrow is called with an action that doesn't have a .SavingThrow field simply won't call this.
                    // PETR: Similarly, AfterYourSavingThrow will lack saving throw data.
                    /*qfFeat.BeforeYourSavingThrow = async (qfThis, action, self) =>
                    {
                        qfThis.Id = action.SavingThrow!.Defense == Defense.Fortitude && action.HasTrait(Trait.Poison) /*&& AndroidAncestry.CanUseNanites(self)#1#
                            ? QEffectId.StrongBloodedDwarf
                            : QEffectId.Unspecified;
                    };*/
                    qfFeat.Id = QEffectId.StrongBloodedDwarf;
                });
        ModManager.AddFeat(cleansingSubroutine);
        
        Feat emotionless = new TrueFeat(
                ModData.FeatNames.Emotionless,
                1,
                "Your malfunctioning emotional processors make it difficult for you to feel strong emotions.",
                "You gain a +1 circumstance bonus to saving throws against emotion and fear effects. If you roll a success on a saving throw against an emotion or fear effect, you get a critical success instead.",
                [ModData.Traits.AndroidAncestry])
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
                ModData.FeatNames.InternalCompartment,
                1,
                "You can hide a small object inside a hollow cavity on one of your forearms.",
                "The first item you draw or replace each combat is a {icon:FreeAction} free action instead of the usual cost. The item must not require two hands to wield.",
                [ModData.Traits.AndroidAncestry])
            .WithPermanentQEffect("The first item you draw or replace is a free action.", qfFeat =>
            {
                qfFeat.Id = ModData.QEffectIds.InternalCompartment;
                qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                {
                    string name = action.Name.ToLower();
                    if ((!name.StartsWith("draw") && !name.StartsWith("replace")) || action.Item is { TwoHanded: true })
                        return;

                    qfThis.Owner.RemoveAllQEffects(qf => qf == qfThis);
                };
                
                ModManager.RegisterActionOnEachActionPossibility(action =>
                {
                    if (action.Owner.HasEffect(ModData.QEffectIds.InternalCompartment) && (action.Name.ToLower().StartsWith("draw") || action.Name.ToLower().StartsWith("replace")) && action.Item is not { TwoHanded: true })
                    {
                        action.Description = action.Description.Insert(0, "{Blue}{b}Internal Compartment{/b} Once per combat, this is a free action instead of the usual cost.{/Blue}\n\n");
                        action.ActionCost = 0;
                    }
                });
            });
        ModManager.AddFeat(internalCompartment);

        Feat naniteSurge = new TrueFeat(
                ModData.FeatNames.NaniteSurge,
                1,
                "You stimulate your nanites, forcing your body to temporarily increase its efficiency.",
                "{b}Frequency{/b} once per combat.\n{b}Trigger{/b} You attempt a skill check requiring three actions or fewer.\n\nYou gain a +2 status bonus to the triggering skill check. {i}(Cosmetic){/i} In addition, your circuitry glows, lighting a 10-foot emanation with dim light for 1 round.",
                [ModData.Traits.AndroidAncestry, Trait.Concentrate])
            .WithActionCost(-2)
            .WithPermanentQEffect("Once per combat, boost a skill check you're about to attempt.", qfFeat =>
            {
                qfFeat.BeforeYourActiveRoll = async (qfThis, action, target) =>
                {
                    Creature self = qfThis.Owner;
                    
                    if (action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is not { } skill)
                        return;
                    
                    if (self.HasEffect(ModData.QEffectIds.NaniteSurgeImmunity) || !AndroidAncestry.CanUseNanites(self))
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
                ModData.FeatNames.UltravisualAdaptation,
                1,
                "The nanites in your ocular processors have adapted to alternative visual spectra.",
                $"You can cast {AllSpells.CreateSpellLink(SpellId.SeeInvisibility, ModData.Traits.AndroidAncestry)} once per day as a 2nd-level arcane innate spell.",
                [ModData.Traits.AndroidAncestry])
            .WithIllustration(IllustrationName.SeeInvisibility)
            .WithRulesBlockForSpell(SpellId.SeeInvisibility, ModData.Traits.AndroidAncestry)
            .WithOnCreature(cr =>
            {
                SpellcastingSource source = cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, ModData.Traits.AndroidAncestry, Ability.Charisma, Trait.Arcane);
                source.WithSpells([SpellId.SeeInvisibility], 2);
                if (source.Spells.Find(ca => ca.SpellId == SpellId.SeeInvisibility) is { } seeInvis)
                {
                    // TODO: Does not work
                    seeInvis.WithExtraTrait(ModData.Traits.Nanites);
                    seeInvis.Name = "Blob";
                }
            });
        ModManager.AddFeat(ultravisualAdaptation);

        Feat proximityAlert = new TrueFeat(
            ModData.FeatNames.ProximityAlert,
            1,
            "You're unnaturally in tune with your surroundings and react instinctively to danger.",
            "You gain the incredible initiative general feat.",
            [ModData.Traits.AndroidAncestry])
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.IncredibleInitiative);
            })
            .WithEquivalent(values => values.HasFeat(FeatName.IncredibleInitiative));
        ModManager.AddFeat(proximityAlert);

        // BUG: Doesn't announce overheads? Nbd but it should probably look a lot like Bless.
        Feat radiantCircuitry = new TrueFeat(
            ModData.FeatNames.RadiantCircuitry,
            1,
            "Your biological circuitry emits light like a torch.",
            "You create a 20-foot emanation of light. Creatures in this emanation have a -1 circumstance penalty to Stealth checks.\n\nThe light shuts off when you take this action again or are knocked unconscious.",
            [ModData.Traits.AndroidAncestry])
            .WithActionCost(1)
            .WithPermanentQEffect("You can toggle a 20-foot emanation of light which penalizes Stealth.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction radiantAction = new CombatAction(
                        qfThis.Owner,
                        ModData.Illustrations.RadiantCircuitry,
                        "Radiant Circuitry " + (qfThis.Owner.HasEffect(ModData.QEffectIds.RadiantCircuitry) ? "(off)" : "(on)"),
                        [ModData.Traits.AndroidAncestry, Trait.Concentrate, Trait.Light, Trait.Basic],
                        "{i}Your biological circuitry emits light like a torch.{/i}\n\n"+"You create a 20-foot emanation of light. Creatures in this emanation have a -1 circumstance penalty to Stealth checks.\n\nThe light shuts off when you take this action again or are knocked unconscious.",
                        Target.Self())
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.PowerfulLight)
                        .WithEffectOnSelf(self =>
                        {
                            switch (self.HasEffect(ModData.QEffectIds.RadiantCircuitry))
                            {
                                case true:
                                    self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.RadiantCircuitry);
                                    break;
                                case false:
                                    QEffect radiantEffect = new QEffect(
                                            "Radiant Circuitry",
                                            "Creatures in a 20-foot emanation have a -1 circumstance penalty to Stealth checks.",
                                            ExpirationCondition.Never,
                                            self,
                                            ModData.Illustrations.RadiantCircuitry)
                                        {
                                            Id = ModData.QEffectIds.RadiantCircuitry,
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
                ModData.FeatNames.AdvancedTargetingSystem,
                5,
                "Your ocular processors are augmented with advanced targeting systems, which allow you to more easily pinpoint your enemy and read their movements.",
                $"You can cast {AllSpells.CreateSpellLink(SpellId.TrueStrike, ModData.Traits.AndroidAncestry)} once per day as a 1st-level arcane innate spell.",
                [ModData.Traits.AndroidAncestry])
            .WithIllustration(IllustrationName.TrueStrike)
            .WithRulesBlockForSpell(SpellId.TrueStrike, ModData.Traits.AndroidAncestry)
            .WithOnCreature(cr =>
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, ModData.Traits.AndroidAncestry, Ability.Charisma, Trait.Arcane)
                    .WithSpells([SpellId.TrueStrike], 1);
            });
        ModManager.AddFeat(advancedTargetingSystem);

        // PETR: Inoculation Subroutine. Disease afflictions are not well-supported at this time. Main issue is that AdjustValue is built for poisons with hard-coded integration with Strong-Blooded Dwarf, meaning there are unreliable integrations and an inaccessibility of stage reductions other than poisons.
        // If implemented, check for Nanite Shroud blockage.
        // Feat inoculationSubroutine = new TrueFeat(
        //     Enums.FeatNames.InoculationSubroutine,);
        // ModManager.AddFeat(inoculationSubroutine);

        Feat naniteShroud = new TrueFeat(
                ModData.FeatNames.NaniteShroud,
                5,
                "Your nanites fly out of your body, swarming around you in a cloud.",
                "{b}Frequency{/b} once per day\n\nYou become concealed for a number of rounds equal to half your level (you can't use this concealment to Hide or Sneak) or until you dismiss it.\n\nWhile Nanite Shroud is active, you can't use other abilities that require the use of your nanites {i}(any feat whose text mentions nanites){i}.",
                [ModData.Traits.AndroidAncestry, Trait.Concentrate])
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
                        [ModData.Traits.AndroidAncestry, Trait.Concentrate, Trait.Basic],
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
                                Tag = ModData.QEffectIds.NanitesUnusable,
                                Dismissable = true, // Homebrew
                                SpawnsAura = qfThis2 =>
                                    new MagicCircleAuraAnimation(IllustrationName.BaneCircle, Color.DarkSlateGray,
                                        0.5f), // TODO: improve this
                                PreventTakingAction = action =>
                                    action.HasTrait(ModData.Traits.Nanites) ||
                                    (action.SpellcastingSource?.ClassOfOrigin == ModData.Traits.AndroidAncestry && action.SpellId == SpellId.SeeInvisibility) ? "Blocked by nanite shroud" : null,
                                // Homebrew:
                                ProvideActionIntoPossibilitySection = (qfThis2, section) =>
                                    section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers
                                        ? new ActionPossibility(new CombatAction(self, IllustrationName.DismissAura,
                                            "Dismiss Nanite Shroud", [Trait.Concentrate, Trait.Basic], "Dismiss nanite shroud.",
                                            Target.Self()).WithEffectOnSelf(
                                            cr => qfThis2.ExpiresAt = ExpirationCondition.Immediately ))
                                        : null
                            };
                            self.RemoveAllQEffects(qf => qf.Traits.Contains(ModData.Traits.Nanites));
                            self.AddQEffect(naniteShroud);
                            self.PersistentUsedUpResources.UsedUpActions.Add("NaniteShroud");
                        });
                    
                    return new ActionPossibility(shroudAction, PossibilitySize.Full);
                };
            });
        ModManager.AddFeat(naniteShroud);

        Feat protectiveSubroutine = new TrueFeat(
                ModData.FeatNames.ProtectiveSubroutine,
                5,
                "Your nanites can augment your defenses.",
                "You can also activate Nanite Surge {icon:Reaction} when you attempt a saving throw. If you do, you gain a +2 status bonus to the triggering saving throw.",
                [ModData.Traits.AndroidAncestry])
            .WithPermanentQEffect("You can use Nanite Surge {icon:Reaction} with saving throws.", qfFeat =>
            {
                qfFeat.BeforeYourSavingThrow = async (qfThis, action, self) =>
                {
                    Defense save = action.SavingThrow!.Defense;
                    
                    if (self.HasEffect(ModData.QEffectIds.NaniteSurgeImmunity) || !AndroidAncestry.CanUseNanites(self))
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
            .WithPrerequisite(ModData.FeatNames.NaniteSurge,"Nanite Surge");
        ModManager.AddFeat(protectiveSubroutine);

        Feat deployableFins = new TrueFeat(
                ModData.FeatNames.DeployableFins,
                9,
                "Your body can internally store fins or other apparatuses which allows you to swim unimpeded, though they require constant maintenance.",
                "For the rest of the encounter, you gain swimming {i}(You can move across water as though it were solid ground){/i}.\n\n" +new ModdedIllustration(ModData.Illustrations.DDSunPath).IllustrationAsIconString + " This action only shows on maps which contain any water.",
                [ModData.Traits.AndroidAncestry, Trait.Concentrate])
            .WithActionCost(1)
            .WithPermanentQEffect("You gain swimming for the rest of the encounter.", qfFeat =>
            {
                qfFeat.ProvideContextualAction = qfThis =>
                {
                    if (qfThis.Owner.HasEffect(QEffectId.Swimming) || !qfThis.Owner.Battle.Map.AllTiles.Any(tile => tile.Kind is TileKind.Water or TileKind.ShallowWater))
                        return null;

                    CombatAction deployFins = new CombatAction(
                            qfThis.Owner,
                            IllustrationName.WaterWalk,
                            "Deploy Fins",
                            [ModData.Traits.AndroidAncestry, Trait.Concentrate],
                            "{i}Your body can internally store fins or other apparatuses which allows you to swim unimpeded, though they require constant maintenance.{/i}\n\nFor the rest of the encounter, you gain swimming {i}(You can move across water as though it were solid ground){/i}.",
                            Target.Self())
                        .WithSoundEffect(SfxName.DisableDevice)
                        .WithEffectOnSelf(async self =>
                        {
                            QEffect deployedFins = QEffect.Swimming();
                            deployedFins.Name = "Deployed Fins";
                            deployedFins.Source = self;
                            self.AddQEffect(deployedFins);
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        });

                    return new ActionPossibility(deployFins);
                };
            });
        ModManager.AddFeat(deployableFins);

        Feat offensiveSubroutine = new TrueFeat(
                ModData.FeatNames.OffensiveSubroutine,
                9,
                "Your nanites can augment your attacks.",
                "You can choose to activate Nanite Surge {icon:Reaction} when you attempt an attack roll, instead of when you attempt a skill check. If you do, you gain a +1 status bonus to the triggering attack roll.",
                [ModData.Traits.AndroidAncestry])
            .WithPermanentQEffect("You can use Nanite Surge {icon:Reaction} with attack rolls.", qfFeat =>
            {
                // TODO: Prevent trigger whenever you already have a +1 status bonus to the attack roll
                qfFeat.BeforeYourActiveRoll = async (qfThis, action, target) =>
                {
                    if (!action.HasTrait(Trait.Attack))
                        return;
                    
                    if (qfThis.Owner.HasEffect(ModData.QEffectIds.NaniteSurgeImmunity) || !AndroidAncestry.CanUseNanites(qfThis.Owner))
                        return;
                    
                    await AndroidAncestry.AskToUseNanitesReaction(
                        qfThis.Owner,
                        new QEffect(ExpirationCondition.Never)
                        {
                            BonusToAttackRolls = (qfThis2, action2, target2) =>
                            {
                                if (action2 != action)
                                    return null;
                                return new Bonus(1, BonusType.Status, "nanite surge");
                            },
                            AfterYouTakeAction = async (qfThis2, action2) => 
                            {
                                if (action2 == action)
                                    qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                            },
                        },
                        "Attack roll",
                        "+1 status bonus");
                };
            })
            .WithPrerequisite(ModData.FeatNames.NaniteSurge,"Nanite Surge");
        ModManager.AddFeat(offensiveSubroutine);

        Feat repairModule = new TrueFeat(
                ModData.FeatNames.RepairModule,
                9,
                "You trigger your body's repair programming, causing your body's nanites to heal your wounds.",
                "{b}Frequency{/b} once per day\n\nYou gain fast healing equal to half your level for 1 minute. While Repair Module is active, you can't use other abilities that require the use of your nanites {i}(any feat whose text mentions nanites){/i}. You can Dismiss this effect.",
                [ModData.Traits.AndroidAncestry, Trait.Concentrate])
            .WithActionCost(1)
            .WithPermanentQEffect("Gain fast healing for the rest of the encounter, blocking nanite abilities for the duration.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains("RepairModule"))
                            return null;

                        CombatAction repair = new CombatAction(
                                qfThis.Owner,
                                IllustrationName.YellowWarning,
                                "Repair Module",
                                [ModData.Traits.AndroidAncestry, Trait.Concentrate, Trait.Basic],
                                $"{{i}}You trigger your body's repair programming, causing your body's nanites to heal your wounds.{{/i}}\n\n{{b}}Frequency{{/b}} once per day\n\nYou gain fast healing equal to {{Blue}}{qfThis.Owner.Level/2}{{/Blue}} for 1 minute. While Repair Module is active, you can't use other abilities that require the use of your nanites {{i}}(any feat whose text mentions nanites){{/i}}. You can Dismiss this effect.",
                                Target.Self())
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.NaturalHealing)
                            .WithEffectOnSelf(async self =>
                            {
                                QEffect repairEffect = QEffect.FastHealing(self.Level / 2);
                                repairEffect.Name = "Repair Module";
                                repairEffect.Description +=
                                    "\n\nYou can't use other abilities that require the use of your nanites.";
                                repairEffect.Tag = ModData.QEffectIds.NanitesUnusable;
                                repairEffect.Dismissable = true; // Homebrew
                                repairEffect.PreventTakingAction = action =>
                                    action.HasTrait(ModData.Traits.Nanites) ||
                                    (action.SpellcastingSource?.ClassOfOrigin == ModData.Traits.AndroidAncestry &&
                                     action.SpellId == SpellId.SeeInvisibility)
                                        ? "Blocked by nanite shroud"
                                        : null;
                                // Homebrew:
                                repairEffect.ProvideActionIntoPossibilitySection = (qfThis2, section) =>
                                    section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers
                                        ? new ActionPossibility(new CombatAction(self, IllustrationName.DismissAura,
                                            "Dismiss Repair Module", [Trait.Concentrate, Trait.Basic], "Dismiss repair module.",
                                            Target.Self()).WithEffectOnSelf(
                                            cr => qfThis2.ExpiresAt = ExpirationCondition.Immediately))
                                        : null;
                                self.RemoveAllQEffects(qf => qf.Traits.Contains(ModData.Traits.Nanites));
                                self.AddQEffect(repairEffect);
                                self.PersistentUsedUpResources.UsedUpActions.Add("RepairModule");
                            });

                        return new ActionPossibility(repair);
                    };
                });
        ModManager.AddFeat(repairModule);

        Feat consistentSurge = new TrueFeat(
                ModData.FeatNames.ConsistentSurge,
                13,
                "Your nanites are incredibly effective, capable of improving your body's efficiency regularly.",
                "You can use Nanite Surge twice per encounter, rather than only once.",
                [ModData.Traits.AndroidAncestry])
            .WithPermanentQEffect("You can use Nanite Surge twice instead of once per encounter.", qfFeat =>
            {
                qfFeat.YouAcquireQEffect = (qfThis, qfNew) =>
                {
                    if (qfNew.Id != ModData.QEffectIds.NaniteSurgeImmunity)
                        return qfNew;
                    
                    qfThis.ExpiresAt = ExpirationCondition.Immediately;
                    return null;
                };
            });
        ModManager.AddFeat(consistentSurge);

        Feat reviveProtocol = new TrueFeat(
                ModData.FeatNames.RevivificationProtocol,
                13,
                "Your nanites are programmed to automatically revive you.",
                "{b}Frequency{/b} once per day\n{b}Trigger{/b} You have the dying condition and are about to attempt a recovery check.\n\nYou're restored to 1 Hit Point, lose the dying and unconscious conditions, and can act normally on this turn. You gain or increase the wounded condition as normal when losing the dying condition in this way.",
                [ModData.Traits.AndroidAncestry])
            .WithActionCost(0)
            .WithPermanentQEffect("Once per day, your nanites can avoid your dying.", qfFeat =>
            {
                qfFeat.StartOfYourPrimaryTurn = async (qfThis, self) =>
                {
                    QEffect? dyingEffect = self.QEffects.FirstOrDefault(qf => qf.Id == QEffectId.Dying);
                    
                    if (dyingEffect == null || self.PersistentUsedUpResources.UsedUpActions.Contains("RevivificationProtocol"))
                        return;
                    
                    if (await self.AskForConfirmation(IllustrationName.RenewedVigor, "{b}Revivification Protocol {icon:FreeAction}{/b}\nYou're about to attempt a check to recover from dying. Automatically recover and restore 1 Hit Point instead?\n{Red}{b}Frequency{/b} once per day.{/Red}", "Recover", "Roll normally"))
                    {
                        dyingEffect.ExpiresAt = ExpirationCondition.Immediately;
                        CombatAction reviveAction = new CombatAction(
                                self,
                                IllustrationName.RenewedVigor,
                                "Revivification Protocol",
                                [ModData.Traits.AndroidAncestry],
                                "{i}Your nanites are programmed to automatically revive you.{/i}\n\n{b}Frequency{/b} once per day\n{b}Trigger{/b} You have the dying condition and are about to attempt a recovery check.\n\nYou're restored to 1 Hit Point, lose the dying and unconscious conditions, and can act normally on this turn. You gain or increase the wounded condition as normal when losing the dying condition in this way.",
                                Target.Self())
                            .WithActionCost(0);
                        Sfxs.Play(SfxName.MinorHealing);
                        await self.HealAsync("1", reviveAction);
                        self.PersistentUsedUpResources.UsedUpActions.Add("RevivificationProtocol");
                    }
                };
            });
        ModManager.AddFeat(reviveProtocol);
    }
}