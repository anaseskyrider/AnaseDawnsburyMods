using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.AndroidAncestry;

public static class AncestryFeats
{
    public static void LoadFeats()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        #region 1st-Level Feats

        // Android Lore
        yield return new TrueFeat(
                ModData.FeatNames.AndroidLore, 1,
                "You have a keen interest in the origins of your people.",
                "You gain the trained proficiency rank in Crafting and Thievery. If you would automatically become trained in one of those skills (from your background or class, for example), you instead become trained in a skill of your choice.",
                [ModData.Traits.Android])
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
                values.TrainInThisOrSubstitute(Skill.Thievery);
            });

        // Cleansing Subroutine
        yield return new TrueFeat(
                ModData.FeatNames.CleansingSubroutine, 1,
                "Your nanites help purge your body of harmful chemicals and toxins.",
                "Each time you succeed at a save against a poison affliction, you reduce its stage by 2. Each critical success you achieve against a poison affliction reduces its stage by 3.",    
                //"Each time you succeed at a Fortitude save against a poison affliction, you reduce its stage by 2, or by 1 against a virulent poison. Each critical success you achieve against a poison affliction reduces its stage by 3, or by 2 against a virulent poison.",
                [ModData.Traits.Android, ModData.Traits.Nanites])
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + ". Your nanites reduce poisons you save against by an additional stage.";
                // PETR: Doesn't work in like a lot of cases. Any time RollSavingThrow is called with an action that doesn't have a .SavingThrow field simply won't call this.
                // PETR: Similarly, AfterYourSavingThrow will lack saving throw data.
                /*qfFeat.BeforeYourSavingThrow = async (qfThis, action, self) =>
                {
                    qfThis.Id = action.SavingThrow!.Defense == Defense.Fortitude && action.HasTrait(Trait.Poison) /*&& AndroidAncestry.CanUseNanites(self)#1#
                        ? QEffectId.StrongBloodedDwarf
                        : QEffectId.Unspecified;
                };*/
                qfFeat.StateCheck = qfThis =>
                {
                    if (ModData.CommonRequirements.CanUseNanites(qfThis.Owner))
                        qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            { Id = QEffectId.StrongBloodedDwarf });
                };
            });

        // Emotionless
        yield return new TrueFeat(
                ModData.FeatNames.Emotionless, 1,
                "Your malfunctioning emotional processors make it difficult for you to feel strong emotions.",
                "You gain a +1 circumstance bonus to saving throws against emotion and fear effects. If you roll a success on a saving throw against an emotion or fear effect, you get a critical success instead.",
                [ModData.Traits.Android])
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + ". You have a +1 circumstance bonus to saves against emotion and fear. If you succeed such a save, you critically succeed instead.";
                qfFeat.BonusToDefenses = (qfThis, action, defense) =>
                {
                    if (action != null
                        && (action.HasTrait(Trait.Emotion)
                            || action.HasTrait(Trait.Fear))
                        && defense != Defense.AC)
                        return new Bonus(1, BonusType.Circumstance, "Emotionless");

                    return null;
                };
                qfFeat.AdjustSavingThrowCheckResult = (qfThis, defense, action, result) =>
                {
                    if (result == CheckResult.Success
                        && (action.HasTrait(Trait.Emotion)
                            || action.HasTrait(Trait.Fear)))
                        return CheckResult.CriticalSuccess;

                    return result;
                };
            });

        // Internal Compartment
        yield return new TrueFeat(
                ModData.FeatNames.InternalCompartment, 1,
                "You can hide a small object inside a hollow cavity on one of your forearms.",
                "The first item you draw or replace each combat is a {icon:FreeAction} free action instead of the usual cost. The item must not require two hands to wield.",
                [ModData.Traits.Android])
            .WithPermanentQEffect(
                "The first non-two-handed item you draw or replace is a free action.",
                qfFeat =>
                {
                    qfFeat.Id = ModData.QEffectIds.InternalCompartment;
                    qfFeat.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (!action.Owner.HasEffect(ModData.QEffectIds.InternalCompartment)
                            || action.ActionId is not (ActionId.DrawItem or ActionId.ReplaceItemInHand)
                            || (action.Item?.WieldedInTwoHands ?? false))
                            return;
                        
                        action.Description = action.Description.Insert(0, "{Blue}{b}Internal Compartment{/b} Once per encounter, this is a free action instead of the usual cost.{/Blue}\n\n");
                        action.ActionCost = 0;
                        action.WithEffectOnEachTarget(async (_,_,_,_) =>
                            qfThis.ExpiresAt = ExpirationCondition.Immediately);
                    };
                });

        // Nanite Surge
        yield return new TrueFeat(
                ModData.FeatNames.NaniteSurge, 1,
                "You stimulate your nanites, forcing your body to temporarily increase its efficiency.",
                """
                {b}Frequency{/b} Once per encounter.
                {b}Trigger{/b} You attempt a skill check requiring three actions or fewer.
                
                You gain a +2 status bonus to the triggering skill check. {i}(Cosmetic){/i} In addition, your circuitry glows, lighting a 10-foot emanation with dim light for 1 round.
                """,
                [ModData.Traits.Android, Trait.Concentrate, ModData.Traits.Nanites])
            .WithActionCost(-2)
            .WithPermanentQEffect(
                "(Once per encounter) Your nanites grant a +2 status bonus to a skill check you're about to attempt.",
                qfFeat =>
                {
                    qfFeat.Id = ModData.QEffectIds.NaniteSurge;
                    qfFeat.Value = 1; // Number of uses
                    qfFeat.HideValue = true;
                    
                    List<string> usages = ["a skill check (+2)"];
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.ProtectiveSubroutine))
                        usages.Add("a saving throw (+2)");
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.OffensiveSubroutine))
                        usages.Add("an attack roll (+1)");
                    if (usages.Count > 1)
                        qfFeat.Description =
                            $"(Once per encounter) Your nanites grant a status bonus to {S.ConstructOrList(usages)} you're about to attempt.";

                    qfFeat.YouBeginActionReaction = (qfThis, triggeringAction) =>
                    {
                        // ReactionCollection methods handle broader nanite usability.
                        if (triggeringAction.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is not { } skill
                            || triggeringAction.ActionCost is < 0 or > 3)
                            return null;
                        
                        ReactionOption? reactOpt = AskToUseNanitesReaction(
                            qfThis.Owner,
                            async (surge, caster) =>
                            {
                                caster.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                {
                                    Traits = [ModData.Traits.Nanites],
                                    BonusToSkillChecks = (skill2, triggeringAction2, target2) =>
                                    {
                                        if (triggeringAction2 != triggeringAction || skill2 != skill)
                                            return null;
                                        return new Bonus(2, BonusType.Status, "Nanite surge");
                                    },
                                    AfterYouTakeAction = async (qfThis2, triggeringAction2) =>
                                    {
                                        if (triggeringAction2 == triggeringAction)
                                            qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                    },
                                });
                            });

                        // Throws null warnings on implicit operator if not done this way.
                        // ReSharper disable once ConvertIfStatementToReturnStatement
                        // ReSharper disable once UseNullPropagation
                        if (reactOpt is null)
                            return null;

                        return reactOpt;
                    };
                });
        
        // Ultravisual Adaptation
        yield return new TrueFeat(
                ModData.FeatNames.UltravisualAdaptation, 1,
                "The nanites in your ocular processors have adapted to alternative visual spectra.",
                $"You can cast {SpellId.SeeInvisibility.ToLink("see invisibility", ModData.Traits.Android, 2)} once per day as a 2nd-level arcane innate spell.",
                [ModData.Traits.Android, Trait.Homebrew, ModData.Traits.Nanites])
            .WithIllustration(IllustrationName.SeeInvisibility)
            .WithRulesBlockForSpell(SpellId.SeeInvisibility, ModData.Traits.Android, 2)
            .WithOnCreature(cr =>
            {
                SpellcastingSource source = cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, ModData.Traits.Android, Ability.Charisma, Trait.Arcane);
                source.WithSpells([SpellId.SeeInvisibility], 2);
                if (source.Spells.Find(ca => ca.SpellId == SpellId.SeeInvisibility) is { } seeInvis)
                {
                    // TODO: Does not work
                    seeInvis.WithExtraTrait(ModData.Traits.Nanites);
                    seeInvis.Name = "Blob";
                }
            });

        // Proximity Alert
        yield return new TrueFeat(
                ModData.FeatNames.ProximityAlert, 1,
                "You're unnaturally in tune with your surroundings and react instinctively to danger.",
                $"You gain the {FeatName.IncredibleInitiative.ToLink("Incredible Initiative")} general feat.",
                [ModData.Traits.Android])
            .WithOnSheet(values => values.GrantFeat(FeatName.IncredibleInitiative))
            .WithEquivalent(values => values.HasFeat(FeatName.IncredibleInitiative));

        // Radiant Circuitry
        yield return new TrueFeat(
                ModData.FeatNames.RadiantCircuitry, 1,
                "Your biological circuitry emits light like a torch.",
                """
                You create a 20-foot emanation of light. Creatures in this emanation have a -1 circumstance penalty to Stealth checks.
                
                The light shuts off when you take this action again or are knocked unconscious.
                """,
                [ModData.Traits.Android])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "You can toggle a 20-foot emanation of light which penalizes Stealth.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction radiantAction = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.RadiantCircuitry,
                                "Radiant Circuitry " + (qfThis.Owner.HasEffect(ModData.QEffectIds.RadiantCircuitry) ? "(off)" : "(on)"),
                                [ModData.ModTrait, ModData.Traits.Android, Trait.Concentrate, Trait.Light, Trait.Basic],
                                """
                                {i}Your biological circuitry emits light like a torch.{/i}

                                You create a 20-foot emanation of light. Creatures in this emanation have a -1 circumstance penalty to Stealth checks.

                                The light shuts off when you take this action again or are knocked unconscious.
                                """,
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
                        
                        return new ActionPossibility(radiantAction);
                    };
                });

        #endregion

        #region 5th-Level Feats

        // Advanced Targeting System
        yield return new TrueFeat(
                ModData.FeatNames.AdvancedTargetingSystem, 5,
                "Your ocular processors are augmented with advanced targeting systems, which allow you to more easily pinpoint your enemy and read their movements.",
                $"You can cast {SpellId.TrueStrike.ToLink("true strike", ModData.Traits.Android, 1)} once per day as a 1st-level arcane innate spell.",
                [ModData.Traits.Android])
            .WithIllustration(IllustrationName.TrueStrike)
            .WithRulesBlockForSpell(SpellId.TrueStrike, ModData.Traits.Android, 1)
            .WithOnCreature(cr =>
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, ModData.Traits.Android, Ability.Charisma, Trait.Arcane)
                    .WithSpells([SpellId.TrueStrike], 1);
            });

        // PETR: Inoculation Subroutine. Disease afflictions are not well-supported at this time.
        // Main issue is that AdjustValue is built for poisons with hard-coded integration with Strong-Blooded Dwarf, meaning there are unreliable integrations and an inaccessibility of stage reductions other than poisons.
        // If implemented, check for Nanite Shroud blockage.
        /*yield return new TrueFeat(
            ModData.FeatNames.InoculationSubroutine, 5);*/

        // Nanite Shroud
        yield return new TrueFeat(
                ModData.FeatNames.NaniteShroud, 5,
                "Your nanites fly out of your body, swarming around you in a cloud.",
                """
                {b}Frequency{/b} Once per day.

                You become concealed for a number of rounds equal to half your level (you can't use this concealment to Hide or Sneak) or until you dismiss it.

                While Nanite Shroud is active, you can't use or benefit from other nanite feats.
                """,
                [ModData.Traits.Android, Trait.Concentrate, ModData.Traits.Nanites])
            .WithActionCost(2)
            .WithPermanentQEffect(qfFeat =>
            {
                int duration = qfFeat.Owner.Level / 2;
                
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + $" (Once per day) Become concealed for {{Blue}}{duration}{{/Blue}} rounds." ;
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.NANITE_SHROUD))
                        return null;
                    
                    CombatAction shroudAction = new CombatAction(
                            qfThis.Owner,
                            IllustrationName.ChillingDarkness,
                            "Nanite Shroud",
                            [ModData.ModTrait, ModData.Traits.Android, Trait.Concentrate, Trait.Basic],
                            $$"""
                              {i}Your nanites fly out of your body, swarming around you in a cloud.{/i}

                              {b}Frequency{/b} Once per day.

                              You become concealed for {Blue}{{qfThis.Owner.Level / 2}}{/Blue} rounds (you can't use this concealment to Hide or Sneak) or until you dismiss it.

                              While Nanite Shroud is active, you can't use other abilities that require the use of your nanites.
                              """,
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                    ModData.CommonRequirements.CanUseNanites(self)
                                    ? null : "Can't use nanites"))
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.InvisibilityPoor)
                        .WithEffectOnSelf(self =>
                        {
                            QEffect naniteShroud = new QEffect(
                                    "Nanite Shroud",
                                    "You are concealed for a number of rounds equal to this effect's value.\n\nYou can't use this concealment to Hide or Sneak.",
                                    ExpirationCondition.CountsDownAtStartOfSourcesTurn,
                                    self,
                                    IllustrationName.ChillingDarkness)
                                {
                                    Value = duration,
                                    Id = QEffectId.Blur,
                                    SpawnsAura = _ =>
                                        new MagicCircleAuraAnimation(IllustrationName.BaneCircle, Color.DarkSlateGray,
                                            0.5f)
                                };
                            naniteShroud.WithBlocksNanites();
                            self.AddQEffect(naniteShroud);
                            self.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.NANITE_SHROUD);
                        });
                    
                    return new ActionPossibility(shroudAction);
                };
            });

        // Protective Subroutine
        yield return new TrueFeat(
                ModData.FeatNames.ProtectiveSubroutine, 5,
                "Your nanites can augment your defenses.",
                $"You can also activate {ModData.FeatNames.NaniteSurge.ToLink("Nanite Surge {icon:Reaction}")} when you attempt a saving throw. If you do, you gain a +2 status bonus to the triggering saving throw.",
                [ModData.Traits.Android])
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddGrantingOfTechnical(
                    _ => true,
                    qfTech =>
                    {
                        qfTech.YouBeginActionReaction = (qfTech2, triggeringAction) =>
                        {
                            // ReactionCollection methods handle broader nanite usability.
                            if (!triggeringAction.ChosenTargets.GetAllTargetCreatures().Contains(qfFeat.Owner)
                                || triggeringAction.SavingThrow is null)
                                return null;

                            Defense save = triggeringAction.SavingThrow.Defense;
                        
                            ReactionOption? reactOpt = AskToUseNanitesReaction(
                                qfFeat.Owner,
                                async (surge, caster) =>
                                {
                                    caster.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                                    {
                                        Traits = [ModData.Traits.Nanites],
                                        BonusToDefenses = (_, triggeringAction2, def) =>
                                        {
                                            if (triggeringAction2 != triggeringAction || def != save)
                                                return null;
                                            return new Bonus(2, BonusType.Status, "Nanite surge");
                                        },
                                        AfterYouMakeSavingThrow = (qfThis2, triggeringAction2, _) => 
                                        {
                                            if (triggeringAction2 == triggeringAction)
                                                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                        },
                                    });
                                },
                                "Protective Subroutine",
                                "You attempt a saving throw.",
                                "You gain a +2 status bonus to the triggering saving throw.");

                            // Throws null warnings on implicit operator if not done this way.
                            // ReSharper disable once ConvertIfStatementToReturnStatement
                            // ReSharper disable once UseNullPropagation
                            if (reactOpt is null)
                                return null;

                            return reactOpt;
                        };
                    });
            })
            .WithPrerequisite(ModData.FeatNames.NaniteSurge,"Nanite Surge");

        #endregion

        #region 9th-Level Feats

        // Deployable Fins
        yield return new TrueFeat(
                ModData.FeatNames.DeployableFins, 9,
                "Your body can internally store fins or other apparatuses which allows you to swim unimpeded, though they require constant maintenance.",
                $$"""
                  For the rest of the encounter, you gain {r}swimming{/r}.

                  {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Implementation{/b} This action only shows on maps which contain any water.
                  """,
                [ModData.Traits.Android, Trait.Concentrate])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "You gain {r}swimming{/r} for the rest of the encounter.",
                qfFeat =>
                {
                    qfFeat.ProvideContextualAction = qfThis =>
                    {
                        if (qfThis.Owner.HasEffect(QEffectId.Swimming)
                            || !qfThis.Owner.Battle.Map.AllTiles.Any(tile =>
                                tile.Kind is TileKind.Water or TileKind.ShallowWater))
                            return null;

                        CombatAction deployFins = new CombatAction(
                                qfThis.Owner,
                                IllustrationName.WaterWalk,
                                "Deploy Fins",
                                [ModData.Traits.Android, Trait.Concentrate],
                                """
                                {i}Your body can internally store fins or other apparatuses which allows you to swim unimpeded, though they require constant maintenance.{/i}

                                For the rest of the encounter, you have {r}swimming{/r}.
                                """,
                                Target.Self())
                            .WithSoundEffect(SfxName.DisableDevice)
                            .WithEffectOnSelf(async (action, self) =>
                            {
                                QEffect deployedFins = QEffect.Swimming();
                                deployedFins.Name = "Deployed Fins";
                                deployedFins.Source = self;
                                deployedFins.Innate = false;
                                deployedFins.Illustration = action.Illustration;
                                self.AddQEffect(deployedFins);
                                qfThis.ExpiresAt = ExpirationCondition.Immediately;
                            });

                        return new ActionPossibility(deployFins);
                    };
                });

        // Offensive Subroutine
        yield return new TrueFeat(
                ModData.FeatNames.OffensiveSubroutine, 9,
                "Your nanites can augment your attacks.",
                $$"""
                You can choose to activate {{ModData.FeatNames.NaniteSurge.ToLink("Nanite Surge {icon:Reaction}")}} when you attempt an attack roll, instead of when you attempt a skill check. If you do, you gain a +1 status bonus to the triggering attack roll.
                """,
                [ModData.Traits.Android, ModData.Traits.Nanites])
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.YouBeginActionReaction = (qfThis, triggeringAction) =>
                {
                    // ReactionCollection methods handle broader nanite usability.
                    // In DD, an attack roll is any action with the Attack trait, but
                    // that's already handled by wider Skill Check trigger of the basic surge.
                    if (triggeringAction.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is not null
                        || !triggeringAction.HasTrait(Trait.Attack))
                        return null;
                    
                    // Already has +1 status
                    if (triggeringAction.ActiveRollSpecification?
                            .CalculateBonus(
                                triggeringAction,
                                triggeringAction.Owner,
                                triggeringAction.ChosenTargets.ChosenCreature)
                            .Bonuses.Any(b =>
                                b is { BonusType: BonusType.Status, Amount: >= 1 }) ?? false)
                        return null;
                    
                    ReactionOption? reactOpt = AskToUseNanitesReaction(
                        qfThis.Owner,
                        async (surge, caster) =>
                        {
                            caster.AddQEffect(new QEffect(ExpirationCondition.Never)
                            {
                                Traits = [ModData.Traits.Nanites],
                                BonusToAttackRolls = (qfThis2, action, target2) =>
                                    action == triggeringAction
                                        ? new Bonus(1, BonusType.Status, "nanite surge")
                                        : null,
                                AfterYouTakeAction = async (qfThis2, action) => 
                                {
                                    if (action == triggeringAction)
                                        qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                },
                            });
                        },
                        "Offensive Subroutine",
                        "You attempt an attack roll.",
                        "You gain a +1 status bonus to the triggering attack roll.");

                    // Throws null warnings on implicit operator if not done this way.
                    // ReSharper disable once ConvertIfStatementToReturnStatement
                    // ReSharper disable once UseNullPropagation
                    if (reactOpt is null)
                        return null;

                    return reactOpt;
                };
            })
            .WithPrerequisite(ModData.FeatNames.NaniteSurge,"Nanite Surge");

        // Repair Module
        yield return new TrueFeat(
                ModData.FeatNames.RepairModule, 9,
                "You trigger your body's repair programming, causing your body's nanites to heal your wounds.",
                """
                {b}Frequency{/b} Once per day.

                You gain {r}fast healing{/r} equal to half your level for 1 minute. While Repair Module is active, you can't use or benefit from other nanite feats. You can Dismiss this effect.
                """,
                [ModData.Traits.Android, Trait.Concentrate, ModData.Traits.Nanites])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " Gain {r}fast healing{/r} for the rest of the encounter, blocking other nanite feats for the duration.";
                
                qfFeat.ProvideMainAction = qfThis =>
                {
                    if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.REPAIR_MODULE))
                        return null;

                    int fastAmount = qfThis.Owner.Level / 2;

                    CombatAction repair = new CombatAction(
                            qfThis.Owner,
                            ModData.Illustrations.RepairModule,
                            "Repair Module",
                            [ModData.Traits.Android, Trait.Concentrate, ModData.Traits.Nanites, Trait.Basic],
                            $$"""
                              {i}You trigger your body's repair programming, causing your body's nanites to heal your wounds.{/i}

                              {b}Frequency{/b} Once per day.

                              You gain fast healing {Blue}{{fastAmount}}{/Blue} for 1 minute. While Repair Module is active, you can't use or benefit from other nanite feats. You can Dismiss this effect.
                              """,
                            Target.Self())
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.NaturalHealing)
                        .WithEffectOnSelf(async self =>
                        {
                            QEffect repairEffect = QEffect.FastHealing(self.Level / 2);
                            repairEffect.Name = "Repair Module";
                            repairEffect.WithBlocksNanites();
                            self.AddQEffect(repairEffect);
                            self.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.REPAIR_MODULE);
                        });

                    return new ActionPossibility(repair);
                };
            });

        #endregion
        
        #region 13th-Level Feats
        
        // Consistent Surge
        yield return new TrueFeat(
                ModData.FeatNames.ConsistentSurge, 13,
                "Your nanites are incredibly effective, capable of improving your body's efficiency regularly.",
                $"You can use {ModData.FeatNames.NaniteSurge.ToLink("Nanite Surge {icon:Reaction}")} twice per encounter, rather than only once.",
                [ModData.Traits.Android])
            .WithPermanentQEffect(qfFeat =>
            {
                QEffect? naniteSurge = qfFeat.Owner.FindQEffect(ModData.QEffectIds.NaniteSurge);
                if (naniteSurge is null)
                    return;
                naniteSurge.Value = 2;
                naniteSurge.Description = naniteSurge.Description!.Replace(
                    "(Once per encounter)", "(Twice per encounter)");
            });

        // Revivification Protocol
        yield return new TrueFeat(
                ModData.FeatNames.RevivificationProtocol, 13,
                "Your nanites are programmed to automatically revive you.",
                """
                {b}Frequency{/b} Once per day.
                {b}Trigger{/b} You have the dying condition and are about to attempt a recovery check.

                You're restored to 1 Hit Point, lose the dying and unconscious conditions, and can act normally on this turn. You gain or increase the wounded condition as normal when losing the dying condition in this way.
                """,
                [ModData.Traits.Android, ModData.Traits.Nanites])
            .WithActionCost(0)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " (Once per day) Your nanites can avoid your dying.".WithTag(qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.REVIVIFICATION_PROTOCOL) ? "{strike}" : null);
                
                qfFeat.StartOfYourPrimaryTurn = async (qfThis, self) =>
                {
                    QEffect? dyingEffect = self.QEffects.FirstOrDefault(qf => qf.Id == QEffectId.Dying);
                    
                    if (dyingEffect == null
                        || self.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.REVIVIFICATION_PROTOCOL))
                        return;
                    
                    CombatAction reviveAction = new CombatAction(
                            self,
                            IllustrationName.RenewedVigor,
                            "Revivification Protocol",
                            [ModData.Traits.Android, ModData.Traits.Nanites, Trait.UsableEvenWhenUnconsciousOrParalyzed],
                            """
                            {i}Your nanites are programmed to automatically revive you.{/i}

                            {b}Frequency{/b} Once per day.
                            {b}Trigger{/b} You have the dying condition and are about to attempt a recovery check.

                            You're restored to 1 Hit Point, lose the dying and unconscious conditions, and can act normally on this turn. You gain or increase the wounded condition as normal when losing the dying condition in this way.
                            """,
                            Target.Self())
                        .WithActionCost(0)
                        .WithSoundEffect(SfxName.MinorHealing)
                        .WithEffectOnSelf(async (revive, self2) =>
                        {
                            // Doesn't stop a recovery roll from occurring, but the roll has no impact.
                            self2.RemoveAllQEffects(qf => qf == dyingEffect);
                            await self2.HealAsync("1", revive);
                            self.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.REVIVIFICATION_PROTOCOL);
                        });

                    if (!reviveAction.CanBeginToUse(self))
                        return;
                    
                    if (await self.AskForConfirmation(
                            IllustrationName.RenewedVigor,
                            $$"""
                            {b}Revivification Protocol {icon:FreeAction}{/b}
                            {Red}{b}Frequency{/b} Once per day.{/Red}
                            You're about to attempt a check to recover from {r}dying{/r} {{dyingEffect.Value}}. Automatically recover and restore 1 Hit Point instead?
                            """,
                            "Recover",
                            "Roll normally"))
                    {
                        await self.Battle.GameLoop.FullCast(reviveAction);
                    }
                };
            });
        
        #endregion
    }

    /// <summary>
    /// Creates a <see cref="ReactionOption"/> for the Android's Nanite Surge feat reaction.
    /// </summary>
    /// <param name="android">The android taking the reaction.</param>
    /// <param name="effectOnSelf">The effect that occurs when executing the Nanite Surge <see cref="CombatAction"/>.</param>
    /// <param name="subName">See <see cref="NaniteSurge"/>.</param>
    /// <param name="trigger">See <see cref="NaniteSurge"/>.</param>
    /// <param name="effect">See <see cref="NaniteSurge"/>.</param>
    /// <returns></returns>
    public static ReactionOption? AskToUseNanitesReaction(
        Creature android,
        Func<CombatAction,Creature,Task> effectOnSelf,
        string? subName = null,
        string trigger = "You attempt a skill check requiring three actions or fewer.",
        string effect = "You gain a +2 status bonus to the triggering skill check.")
    {
        CombatAction naniteSurge = NaniteSurge(android, effectOnSelf, subName, trigger, effect);
        return AskToUseNanitesReaction(naniteSurge, effect);
    }

    /// <summary>
    /// See other overload.
    /// </summary>
    /// <param name="naniteSurge">The Nanite Surge CombatAction (see: <see cref="NaniteSurge"/>).</param>
    /// <param name="effectSummary">The <see cref="ReactionOption.EffectSummary"/>. Should be identical to the (string effect) from <see cref="AskToUseNanitesReaction(Creature,Func{CombatAction,Creature,Task}, string?, string, string)"/>. The phrase "You gain" is automatically shortened to "Gain".</param>
    /// <returns></returns>
    public static ReactionOption? AskToUseNanitesReaction(CombatAction naniteSurge, string effectSummary)
    {
        if (!naniteSurge.CanBeginToUse(naniteSurge.Owner))
            return null;

        ReactionOption reactOpt = ReactionOption.WrapFullcast(naniteSurge, effectSummary.Replace("You gain", "Gain"));
        
        // If you can use it more than once,
        // show how many uses are currently remaining in the ReactionOption's caption.
        if (naniteSurge.Owner.HasFeat(ModData.FeatNames.ConsistentSurge))
        {
            // Not null because it would have stopped before reaching this point if not.
            int uses = naniteSurge.Owner.FindQEffect(ModData.QEffectIds.NaniteSurge)!.Value;
            reactOpt.Caption += " (Uses: " + string.Concat(Enumerable.Repeat("{icon:spontaneousspellslot}", uses)) + ")";
        }

        return reactOpt;
    }

    /// <summary>
    /// Create CombatAction that represents Nanite Surge.
    /// </summary>
    /// <param name="android">The CombatAction owner.</param>
    /// <param name="effectOnSelf">The effect to execute, usually adding a QEffect.</param>
    /// <param name="subName">If not null, this is the name of an alternative use of Nanite Surge.</param>
    /// <param name="trigger">If not default, describe the new trigger. Include a "." at the end.</param>
    /// <param name="effect">If not default, describe the new effect.  Include a "." at the end.</param>
    /// <returns></returns>
    public static CombatAction NaniteSurge(
        Creature android,
        Func<CombatAction,Creature,Task> effectOnSelf,
        string? subName = null,
        string trigger = "You attempt a skill check requiring three actions or fewer.",
        string effect = "You gain a +2 status bonus to the triggering skill check.")
    {
        CombatAction naniteSurge = new CombatAction(
                android,
                ModData.Illustrations.NaniteSurge,
                "Nanite Surge" + (subName is null ? null : (" (" + subName + ")")),
                [ModData.ModTrait, ModData.Traits.Android, Trait.Concentrate, ModData.Traits.Nanites],
                $$"""
                  {i}You stimulate your nanites, forcing your body to temporarily increase its efficiency.{/i}

                  {b}Frequency{/b} {{(android.HasFeat(ModData.FeatNames.ConsistentSurge) ? "{Blue}Twice{/Blue}" : "Once")}} per encounter.
                  {b}Trigger{/b} {{trigger.WithColor(trigger == "You attempt a skill check requiring three actions or fewer." ? null : "Blue")}}

                  {{effect.WithColor(trigger == "You attempt a skill check requiring three actions or fewer." ? null : "Blue")}} {i}(Cosmetic){/i} In addition, your circuitry glows, lighting a 10-foot emanation with dim light for 1 round.
                  """,
                Target.Self()
                    .WithAdditionalRestriction(self =>
                    {
                        if (!ModData.CommonRequirements.CanUseNanites(self))
                            return "Can't use nanites";
                        if (!ModData.CommonRequirements.HasNaniteSurgeUses(self))
                            return "Nanite Surge used up";
                        return null;
                    }))
            .WithActionCost(-2)
            .WithActionId(ModData.ActionIds.NaniteSurge)
            .WithSoundEffect(SfxName.Guidance)
            .WithEffectOnSelf(async (surge, caster) =>
            {
                await effectOnSelf(surge, caster);
                
                // Optional light aura.
                caster.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                { 
                    SpawnsAura = _ =>
                    {
                        if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.RemoveNaniteSurgeAura))
                            return null;
                            
                        return new MagicCircleAuraAnimation(IllustrationName.AngelicHaloCircle, Color.Gold, 2f)
                        {
                            MaximumOpacity = 0.25f
                        };
                    }
                });
                
                // Consume use of Nanite Surge.
                // Can't reach this point and be null.
                QEffect qfSurge = caster.FindQEffect(ModData.QEffectIds.NaniteSurge)!;
                qfSurge.Value--;
                if (qfSurge.Value < 1)
                    qfSurge.Description = qfSurge.Description!.WithTag("strike");
            });
        
        return naniteSurge;
    }

    /// <summary>
    /// Adds nanite-effect behavior to this QEffect. 
    /// </summary>
    /// <remarks>
    /// Removes other nanite-traited QEffects and allowing this effect to be dismissed.
    /// </remarks>
    public static QEffect WithBlocksNanites(this QEffect qfToAdjust)
    {
        qfToAdjust.Description +=
            "\n\nYou can't use or benefit from other nanite feats.";
        qfToAdjust.StateCheck += qfHeal =>
            qfHeal.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
            {
                Id = ModData.QEffectIds.NanitesDisabled,
                PreventTakingAction = action =>
                    action.HasTrait(ModData.Traits.Nanites)
                    || (action.SpellcastingSource?.ClassOfOrigin == ModData.Traits.Android
                        && action.SpellId == SpellId.SeeInvisibility)
                        ? "Nanites in continuous use"
                        : null
            });
        // Don't apply if the effect doesn't persist.
        if (qfToAdjust.ExpiresAt != ExpirationCondition.Ephemeral
            && qfToAdjust.ExpiresAt != ExpirationCondition.EphemeralAtEndOfImmediateAction
            && qfToAdjust.ExpiresAt != ExpirationCondition.Immediately)
        {
            // Homebrew:
            qfToAdjust.Dismissable = true;
            qfToAdjust.ProvideActionIntoPossibilitySection = (qfThis2, section) =>
                section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers
                    ? new ActionPossibility(new CombatAction(
                            qfThis2.Owner,
                            IllustrationName.DismissAura,
                            "Dismiss " + qfThis2.Name,
                            [Trait.Concentrate, Trait.Basic],
                            "Dismiss " + qfThis2.Name!.ToLower(),
                            Target.Self())
                        .WithEffectOnSelf(_ => qfThis2.ExpiresAt = ExpirationCondition.Immediately))
                    : null;
        }
        qfToAdjust.WhenYouAcquireThis += qfThis =>
        {
            qfThis.Owner.RemoveAllQEffects(qf =>
                qf != qfThis
                && qf.Traits.Contains(ModData.Traits.Nanites));
        };

        return qfToAdjust;
    }
}