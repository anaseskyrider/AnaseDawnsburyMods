using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.GuardianClass;

/// <summary>Contains all methods for creating and loading the Guardian class, its features, and common actions and effects (Taunt, Intercept Attack).</summary>
public static class GuardianClass
{
    /// <summary>Creates and loads the Guardian class feat and class features.</summary>
    public static void LoadClass()
    {
        foreach (Feat feat in CreateFeatures())
            ModManager.AddFeat(feat);
        ModManager.AddFeat(CreateClassFeat());
    }

    /// <summary>Creates the Guardian class feat.</summary>
    public static Feat CreateClassFeat()
    {
        Feat classFeat = new ClassSelectionFeat(
                ModData.FeatNames.GuardianClass,
                "Death and danger from all manner of enemies threaten all that you and your companions hold dear. But you are the shield, the steel wall that holds back the tide of opposition. You're clad in armor you wear like a second skin and can angle it to protect yourself and your allies from damage and keep foes at bay.",
                ModData.Traits.Guardian,
                new EnforcedAbilityBoost(Ability.Strength),
                12,
                [Trait.Perception, Trait.Reflex, Trait.Athletics, Trait.Unarmed, Trait.Simple, Trait.Martial, Trait.UnarmoredDefense, Trait.Armor],
                [Trait.Fortitude, Trait.Will],
                3,
                "{b}1. Guardian's Armor.{/b} While wearing medium or heavy armor, you gain resistance to physical damage equal to 1 + half your level. In addition, you can "+ModData.Tooltips.ArmorResting("rest normally")+" while wearing medium and heavy armor."
                + "\r\n\r\n{b}2. Taunt.{/b} Often, the best way to protect your allies is to have the enemy want to attack you instead. You gain the "+ModData.Tooltips.ActionTaunt("Taunt {icon:Action}")+" action."
                + "\r\n\r\n{b}3. Intercept Attack.{/b} You keep your charges safe from harm, even if it means you\nget hurt yourself. You gain the "+ModData.Tooltips.ActionInterceptAttack("Intercept Attack {icon:Reaction}")+" reaction."
                + "\r\n\r\n{b}4. Shield block {icon:Reaction}.{/b} You can use your shield to reduce damage you take from attacks."
                + "\r\n\r\n{b}5. Guardian feat.{/b}",
                null)
            .WithClassFeatures(cf =>
            {
                cf.AddFeature(3, ModData.Tooltips.FeatureToughToKill("tough to kill"));
                cf.AddFeature(5, "Expert in defenses", "medium, heavy");
                cf.AddFeature(5, "Expert in weapons", "unarmed, simple, martial");
                cf.AddFeature(7, ModData.Tooltips.FeatureReactionTime("reaction time"));
                cf.AddFeature(7, WellKnownClassFeature.ExpertInReflex);
                cf.AddFeature(9, ModData.Tooltips.FeatureBattleHardened("battle hardened"));
                cf.AddFeature(9, WellKnownClassFeature.ExpertInClassDC);
                cf.AddFeature(11, "Master in defenses", "medium, heavy");
                cf.AddFeature(11, "Expert in other defenses", "unarmored, light");
                cf.AddFeature(11, ModData.Tooltips.CommonWeaponSpec("weapon specialization"));
                cf.AddFeature(13, "Master in weapons", "unarmed, simple, martial");
                cf.AddFeature(15, "Legendary in defenses", "medium, heavy");
                cf.AddFeature(17, ModData.Tooltips.CommonGreaterWeaponSpec("greater weapon specialization"));
                cf.AddFeature(17, ModData.Tooltips.FeatureUnyieldingResolve("unyielding resolve"));
                cf.AddFeature(19, ModData.Tooltips.FeatureGuardianMastery("guardian mastery"));
            })
            .WithOnSheet(values =>
            {
                #region Level 1 Features
                values.AddSelectionOption(new SingleFeatSelectionOption(
                    "GuardianFeat1",
                    "Guardian feat",
                    1,
                    ft =>
                        ft.HasTrait(ModData.Traits.Guardian)));
                values.GrantFeat(FeatName.ShieldBlock);
                values.GrantFeat(ModData.FeatNames.GuardiansArmor);
                values.GrantFeat(ModData.FeatNames.Taunt);
                values.GrantFeat(ModData.FeatNames.InterceptAttack);
                #endregion

                #region Higher Level Features
                values.AddAtLevel(3, values2 =>
                {
                    values2.GrantFeat(ModData.FeatNames.ToughToKill);
                });
                // I found a cool way to do this but I'm not going to risk refactors. Code kept in comments for future projects.
                /*// Level 5
                foreach (Trait trait in (Trait[])[Trait.MediumArmor, Trait.HeavyArmor, Trait.Simple, Trait.Martial, Trait.Unarmed])
                    values.IncreaseProficiency(5, trait, Proficiency.Expert);*/
                values.AddAtLevel(5, values2 =>
                {
                    values2.SetProficiency(Trait.MediumArmor, Proficiency.Expert);
                    values2.SetProficiency(Trait.HeavyArmor, Proficiency.Expert);
                    values2.SetProficiency(Trait.Simple, Proficiency.Expert);
                    values2.SetProficiency(Trait.Martial, Proficiency.Expert);
                    values2.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                });
                values.AddAtLevel(7, values2 =>
                {
                    values2.SetProficiency(Trait.Reflex, Proficiency.Expert);
                    values2.GrantFeat(ModData.FeatNames.ReactionTime);
                });
                values.AddAtLevel(9, values2 =>
                {
                    values2.SetProficiency(Trait.Fortitude, Proficiency.Master);
                    values2.SetProficiency(ModData.Traits.Guardian, Proficiency.Expert);
                });
                #endregion

                #region Post Game Content
                values.AddAtLevel(11, values2 =>
                {
                    values2.SetProficiency(Trait.UnarmoredDefense, Proficiency.Expert);
                    values2.SetProficiency(Trait.LightArmor, Proficiency.Expert);
                    values2.SetProficiency(Trait.MediumArmor, Proficiency.Master);
                    values2.SetProficiency(Trait.HeavyArmor, Proficiency.Master);
                });
                values.AddAtLevel(13, values2 =>
                {
                    values2.SetProficiency(Trait.Unarmed, Proficiency.Master);
                    values2.SetProficiency(Trait.Simple, Proficiency.Master);
                    values2.SetProficiency(Trait.Martial, Proficiency.Master);
                });
                values.AddAtLevel(15, values2 =>
                {
                    values2.SetProficiency(Trait.UnarmoredDefense, Proficiency.Master);
                    values2.SetProficiency(Trait.LightArmor, Proficiency.Master);
                    values2.SetProficiency(Trait.MediumArmor, Proficiency.Legendary);
                    values2.SetProficiency(Trait.HeavyArmor, Proficiency.Legendary);
                });
                values.AddAtLevel(17, values2 =>
                {
                    values2.SetProficiency(Trait.Will, Proficiency.Master);
                });
                values.AddAtLevel(19, values2 =>
                {
                    values2.SetProficiency(ModData.Traits.Guardian, Proficiency.Master);
                    values2.GrantFeat(ModData.FeatNames.GuardianMastery);
                });
                #endregion
            })
            .WithOnCreature(cr =>
            {
                #region Higher Level Features
                if (cr.Level >= 9)
                {
                    cr.AddQEffect(new QEffect(
                        "Battle Hardened",
                        "When you roll a success on a Fortitude save, you get a critical success instead.")
                    {
                        AdjustSavingThrowCheckResult = (effect, defense, action, checkResult) =>
                            defense != Defense.Fortitude || checkResult != CheckResult.Success
                                ? checkResult
                                : CheckResult.CriticalSuccess
                    });
                    // See WithOnSheet for the Master proficiency increase.
                }
                #endregion
                
                #region Post Game Content
                if (cr.Level >= 11)
                {
                    cr.AddQEffect(QEffect.WeaponSpecialization(cr.Level >= 17));
                }
                if (cr.Level >= 17)
                {
                    cr.AddQEffect(new QEffect(
                        "Unyielding Resolve",
                        "When you roll a success on a Will save, you get a critical success instead.")
                    {
                        AdjustSavingThrowCheckResult = (effect, defense, action, checkResult) =>
                            defense != Defense.Will || checkResult != CheckResult.Success
                                ? checkResult
                                : CheckResult.CriticalSuccess
                    });
                    // See WithOnSheet for the Master proficiency increase.
                }
                #endregion
            });
        classFeat.RulesText = classFeat.RulesText
            .Replace("Key ability", "Key attribute");
        return classFeat;
    }
    
    /// <summary>Enumerable function to create feats for the Guardian's features.</summary>
    public static IEnumerable<Feat> CreateFeatures()
    {
        // Guardian's Armor
        yield return new TrueFeat(
                ModData.FeatNames.GuardiansArmor,
                1,
                "Even when you are struck, your armor protects you from some harm.",
                "While wearing medium or heavy armor, you gain resistance to physical damage equal to 1 + half your level.\n\nIn addition, you can rest normally while wearing medium and heavy armor.",
                [])
            .WithPermanentQEffect("While wearing medium or heavy armor, you resist an amount of physical damage equal to 1 + half your level. You can also rest normally in all armor", qfFeat =>
            {
                qfFeat.Description = $"While wearing medium or heavy armor, you resist {{Blue}}{1 + (qfFeat.Owner.Level / 2)}{{/Blue}} physical damage. You can also rest normally in all armor.";
                qfFeat.StateCheck = self =>
                {
                    if (self.Owner.Armor.Item is not { } item ||
                        (!item.HasTrait(Trait.MediumArmor) && !item.HasTrait(Trait.HeavyArmor)))
                        return;
                    int amount = 1 + (self.Owner.Level / 2);
                    self.Owner.WeaknessAndResistance.AddSpecialResistance(
                        "physical",
                        (_, dk) => dk.IsPhysical(),
                        amount,
                        null);
                };
                qfFeat.StartOfCombatBeforeOpeningCutscene = async qfThis =>
                {
                    qfThis.Tag = qfThis.Owner.BaseArmor;
                };
                qfFeat.StartOfCombat = async qfThis =>
                {
                    if (qfThis.Owner.BaseArmor is null && qfThis.Tag is Item { ArmorProperties: not null } tagItem)
                    {
                        qfThis.Owner.BaseArmor = tagItem;
                        // TODO: Comfort trait
                        // Heavy armor is replaced with padded armor?
                        if ((tagItem.HasTrait(Trait.MediumArmor) || tagItem.HasTrait(Trait.HeavyArmor)) && qfThis.Owner.FindQEffect(QEffectId.SpeakAboutMissingArmor) is { } s2e3_armor)
                            s2e3_armor.StartOfYourPrimaryTurn = async (effect, self) =>
                            {
                                if (!self.Actions.CanTakeActions())
                                    return;
                                effect.ExpiresAt = ExpirationCondition.Immediately;
                                /*await self.Battle.Cinematics.ShowQuickBubble(
                                    self,
                                    "It's a good thing I can sleep in my armor. Now to just pick up my weapons.",
                                    null);*/
                                string dialog = "{Green}{b}Guardian's Armor!{b}{/Green}\nIt's a good thing I can sleep in my armor. Now to pick up my weapons.";
                                self.Battle.Cinematics.TutorialBubble = new TutorialBubble(
                                    self.Illustration,
                                    SubtitleModification.Replace(dialog),
                                    null);
                                self.Battle.Log("{b}"+self.Name+":{/b} "+dialog);
                                await self.Battle.SendRequest(ModLoader.NewSleepRequest(5000));
                                self.Battle.Cinematics.TutorialBubble = null;
                            };
                    }
                };
            });
        // Taunt
        yield return new TrueFeat(
                ModData.FeatNames.Taunt,
                1,
                "With an attention-grabbing gesture, noise, cutting remark, or threatening shout, you attempt to draw an enemy to you instead of your allies. Even mindless creatures are drawn to your taunts.",
                "Choose an enemy within 30 feet to be your taunted enemy. If your taunted enemy takes a hostile action that includes at least one of your allies but doesn't include you, they take a –1 circumstance penalty to their attack rolls and DCs for that action, and they also become off-guard until the start of their next turn.\n\nYour enemy remains taunted until the start of your next turn, and you can have only one Taunt in effect at a time. Taunting a new enemy ends this effect on any current target.\n\nTaunt gains the auditory trait, visual trait, or both, depending on how you draw the target's attention.",
                [Trait.Concentrate])
            .WithActionCost(1)
            .WithPermanentQEffect("Designate a taunted enemy within 30 feet. They take penalties if they take hostile actions which don't include you.",
                qfFeat =>
                {
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.GroupTaunt))
                        qfFeat.Description = qfFeat.Description?.Replace(
                            "a taunted enemy",
                            "{Blue}up to 3{/Blue} taunted enemies");
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.LongDistanceTaunt))
                        qfFeat.Description = qfFeat.Description?.Replace(
                            "30 feet",
                            "{Blue}120 feet{/Blue}");
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        CombatAction visualTaunt = CreateTaunt(qfThis.Owner, false, Trait.Visual);
                        visualTaunt.ContextMenuName = "Taunt (Visual)";
                        CombatAction audibleTaunt = CreateTaunt(qfThis.Owner, false, Trait.Auditory);
                        audibleTaunt.ContextMenuName = "Taunt (Auditory)";
                        
                        return new SubmenuPossibility(
                            ModData.Illustrations.Taunt,
                            "Taunt")
                        {
                            SubmenuId = ModData.SubmenuIds.Taunt,
                            PossibilityGroup = ModData.PossibilityGroups.TauntActions,
                            SpellIfAny = CreateTaunt(qfThis.Owner),
                            Subsections = [
                                new PossibilitySection("Taunt")
                                {
                                    PossibilitySectionId = ModData.PossibilitySectionIds.BasicTaunts,
                                    Possibilities = [
                                        new ActionPossibility(audibleTaunt){Caption = "Auditory"},
                                        new ActionPossibility(visualTaunt){Caption = "Visual"},
                                    ]
                                }
                            ]
                        };
                    };
                });
        // Intercept Attack
        yield return new TrueFeat(
                ModData.FeatNames.InterceptAttack,
                1,
                "You fling yourself in the way of oncoming harm to protect an ally.",
                "You can Step, but you must end your movement adjacent to the triggering ally. You take the damage instead of the triggering ally. Apply your own immunities, weaknesses, and resistances to the damage, not the ally's.\n\n{b}Special{/b} You can extend this ability to an ally within 15 feet of you if the damage comes from your taunted enemy. If this ally is farther than you can Step to reach, you can Stride instead of Stepping; you still must end the movement adjacent to your ally.",
                [])
            .WithActionCost(-2)
            .WithPermanentQEffect("Take damage for an adjacent ally, Stepping towards the ally if necessary, or Striding if the attacker is your taunted enemy.",
                qfFeat =>
                {
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.GuardiansIntercept))
                        qfFeat.Description = "{Green}(once per combat){/Green} " + qfFeat.Description;
                    const int interceptRange = 3;
                    qfFeat.AddGrantingOfTechnical(cr =>
                            qfFeat.Owner.FriendOfAndNotSelf(cr)
                            && cr.DistanceTo(qfFeat.Owner) <= interceptRange,
                        qfTech =>
                        {
                            qfTech.YouAreDealtDamageEvent = async (qfTech2, @event) =>
                            {
                                Creature guardian = qfFeat.Owner;
                                Creature ally = qfTech2.Owner;
                                Creature attacker = @event.Source;
                                bool isCritical = @event.CheckResult is CheckResult.CriticalSuccess or CheckResult.CriticalFailure;
                                
                                CombatAction interceptAttack = CreateInterceptAttack(
                                    guardian,
                                    attacker,
                                    @event,
                                    !guardian.Actions.ReactionsUsedUpThisRound.Contains(ModData.CommonReactionKeys.ReactionTime));
                                
                                if (!interceptAttack.CanBeginToUse(qfFeat.Owner))
                                    return;
                                
                                if (await guardian.Battle.AskToUseReaction(
                                        guardian,
                                        $"{{b}}Intercept Attack{{/b}} {{icon:Reaction}}\n{{Blue}}{attacker}{{/Blue}} is about to deal {(isCritical ? "{Red}critical{/Red} " : null)}damage to {{Blue}}{ally}{{/Blue}}. Take the damage instead?",
                                        ModData.Illustrations.InterceptAttack,
                                        [ModData.Traits.Guardian]))
                                {
                                    await guardian.Battle.GameLoop.FullCast(
                                        interceptAttack, ChosenTargets.CreateSingleTarget(ally));
                                    if (guardian.HasFeat(ModData.FeatNames.GuardiansIntercept))
                                        qfFeat.Description = qfFeat.Description?
                                            .Replace("Green}", "Red}");
                                }
                            };
                        });
                });
        // Tough to Kill
        yield return new TrueFeat(
                ModData.FeatNames.ToughToKill,
                3,
                "The protectiveness of your armor ensures that even if you fall, you take longer to die.",
                "You gain the Diehard general feat {i}(you should retrain it if you already have it){/i}. Additionally, the first time each day you'd be reduced to dying 3 or higher, you stay at dying 2 instead.",
                [])
            .WithOnSheet(values =>
            {
                if (values.HasFeat(FeatName.Diehard))
                    values.AddSelectionOption(new SingleFeatSelectionOption(
                        "RetrainDieHard",
                        "Retrain die hard",
                        values.CurrentLevel,
                        ft => ft.HasTrait(Trait.General)));
                else
                    values.GrantFeat(FeatName.Diehard);
            })
            .WithPermanentQEffect("The {Green}first time{/Green} each day you reach dying 3+, you stay at dying 2.",
                qfFeat =>
                {
                    // Update description to red /*Remove if found*/
                    if (qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.ToughToKill))
                        //qfFeat.ExpiresAt = ExpirationCondition.Immediately;
                        qfFeat.Description = qfFeat.Description?.Replace("{Green}first time{/Green}", "{Red}first time{/Red}");
                    // If you somehow jump straight to death:
                    qfFeat.PreventDeathDueToDyingAsync = async (qfThis, qfDying) =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.ToughToKill))
                            return false;
                        StayingAlive(qfDying);
                        return true;
                    };
                    // Everything else:
                    qfFeat.StateCheck = qfThis =>
                    {
                        if (qfThis.Owner.FindQEffect(QEffectId.Dying) is not { } qfDying)
                            return;
                        if (qfDying.Value < 3)
                            return;
                        if (!qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.ToughToKill))
                            StayingAlive(qfDying);
                    };
                    return;

                    void StayingAlive(QEffect qfDying) // Ah! Ha! Ha! Ha!
                    {
                        qfDying.Value = 2;
                        qfDying.Owner.PersistentUsedUpResources.UsedUpActions
                            .Add(ModData.PersistentActions.ToughToKill);
                        qfDying.Owner.Overhead(
                            "Tough to Kill!!",
                            Color.Lime,
                            $"{qfDying.Owner} remains at dying 2 due to tough to kill!",
                            "Tough To Kill",
                            "{i}The protectiveness of your armor ensures that even if you fall, you take longer to die.{/i}\n\nYou gain the Diehard general feat. Additionally, the first time each day you'd be reduced to dying 3 or higher, you stay at dying 2 instead.",
                            new Traits([ModData.Traits.Guardian]));
                        //qfFeat.ExpiresAt = ExpirationCondition.Immediately;
                        qfFeat.Description = qfFeat.Description?.Replace("{Green}first time{/Green}", "{Red}first time{/Red}");
                    }
                });
        // Reaction Time
        yield return new TrueFeat(
                ModData.FeatNames.ReactionTime,
                7,
                "You're always on the lookout for danger and can react to it in an instant.",
                // Wording altered slightly since multiclasses cannot acquire Ever Ready, nor Reaction Time.
                "At the start of combat and for each of your turns, you gain an additional reaction that you can use only for reactions from guardian feats or class features (including Shield Block).",
                [])
            .WithPermanentQEffect("You gain an additional reaction you can use for guardian feats and features (including Shield Block).",
                qfFeat =>
                {
                    qfFeat.Id = ModData.QEffectIds.ReactionTime;
                    qfFeat.OfferExtraReaction = (qfThis, questionText, reactionTraits) => 
                        reactionTraits.ContainsOneOf([
                            ModData.Traits.Guardian,
                            Trait.AttackOfOpportunity,
                            Trait.ShieldBlock,
                        ]) 
                            ? ModData.CommonReactionKeys.ReactionTime
                            : null;
                });
        // Guardian Mastery
        yield return new TrueFeat(
                ModData.FeatNames.GuardianMastery,
                19,
                "You are known for your suit of armor more than the person inside.",
                "While wearing armor, when you attempt a Reflex save, you can add your armor's item bonus to AC instead of your Dexterity modifier if it's higher; if your armor has the bulwark trait, increase this bonus by 1. If you get a success when you do this, you get a critical success instead.",
                /*"While wearing armor, when you attempt a Reflex save to avoid a damaging effect, such as a fireball, you can add your armor's item bonus to AC instead of your Dexterity modifier; if your armor has the bulwark trait, increase this bonus by 1. If you get a success when you do this, you get a critical success instead."*/
                [])
            .WithPermanentQEffect("{b}Requires{/b} wearing armor; {b}Effect{/b} You use your armor's AC instead of your Dexterity for Reflex saves (+1 more with bulwark). Additionally, if you succeed on a Reflex save, you critically succeed instead.",
                qfFeat =>
                {
                    qfFeat.StateCheck = qfThis =>
                    {
                        qfThis.Tag = qfThis.Owner.BaseArmor is { ArmorProperties: not null };
                    };
                    qfFeat.AdjustSavingThrowCheckResult = (qfThis, def,_, result) =>
                        def != Defense.Reflex || result != CheckResult.Success || qfThis.Owner.Armor.Item == null
                            ? result
                            : CheckResult.CriticalSuccess;
                    qfFeat.BonusToDefenses = (qfThis, action, def) =>
                    {
                        if (def is not Defense.Reflex || qfThis.Tag is not true)
                            return null;
                        Creature guardian = qfThis.Owner;
                        Armor armor = qfThis.Owner.Armor;
                        int dexterity = guardian.PersistentCharacterSheet != null
                            ? guardian.PersistentCharacterSheet.Calculated.FinalAbilityScores.TotalModifier(Ability.Dexterity)
                            : guardian.Abilities.Dexterity;
                        bool hasBulwark = armor.Item != null && armor.Item.HasTrait(Trait.Bulwark);
                        bool hasMightyBulwark = ModManager.TryParse("MightyBulwark", out QEffectId mightyBulwark) && guardian.HasEffect(mightyBulwark); // TODO: Mighty Bulwark
                        int finalDex = Math.Max(
                            dexterity,
                            hasBulwark ? (hasMightyBulwark ? 4 : 3) : -99);
                        int AC = armor.ItemBonus + (hasBulwark ? 1 : 0); // Includes base AC plus increases
                        int amount = Math.Max(0, AC - finalDex);
                        return new Bonus(amount, BonusType.Untyped, "Armor's AC");
                    };
                });
    }

    /// <summary>
    /// Creates the Taunt <see cref="CombatAction"/>.
    /// </summary>
    /// <param name="owner">The <see cref="Creature"/> that owns this action.</param>
    /// <param name="oneCreatureOnly">If TRUE, then this Taunt only affects one creature, even if it could affect more due to some other ability.</param>
    /// <param name="extraTraits">Extra <see cref="Trait"/>s, if any, to add to the action. This will usually be <see cref="Trait.Auditory"/> and <see cref="Trait.Visual"/>.</param>
    /// <returns></returns>
    public static CombatAction CreateTaunt(Creature owner, bool oneCreatureOnly = false, params Trait[] extraTraits)
    {
        bool hasGroupTaunt = owner.HasFeat(ModData.FeatNames.GroupTaunt);
        int tauntLimit = hasGroupTaunt ? 3 : 1;
        int distance = (owner.HasFeat(ModData.FeatNames.LongDistanceTaunt) ? 120 : 30) / 5;
        Target tauntTargeting = Target.MultipleCreatureTargets(
            !oneCreatureOnly && hasGroupTaunt ? tauntLimit : 1,
            () => extraTraits.Contains(Trait.Visual)
                ? Target.Ranged(distance)
                : Target.Distance(distance));
        return new CombatAction(
                owner,
                ModData.Illustrations.Taunt,
                "Taunt",
                [Trait.Basic, ModData.Traits.Guardian, Trait.Concentrate, Trait.DoNotShowOverheadOfActionName, ..extraTraits],
                "{i}With an attention-grabbing gesture, noise, cutting remark, or threatening shout, you attempt to draw an enemy to you instead of your allies. Even mindless creatures are drawn to your taunts.{/i}"
                    + $"\n\n{{b}}Range{{b}} {distance*5} feet\n\nThe target becomes your taunted enemy. If they take a hostile action that includes at least one of your allies but doesn't include you, they take a –1 circumstance penalty to their attack rolls and DCs for that action, and they also become off-guard until the start of their next turn.\n\nYour enemy remains taunted until the start of your next turn, and you can have only one Taunt in effect at a time. Taunting a new enemy ends this effect on any current target."
                    + (extraTraits.Length == 0 ? "\n\nTaunt gains the auditory trait, visual trait, or both, depending on how you draw the target's attention." : null),
                tauntTargeting)
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.Taunt)
            .WithSoundEffect(ModData.SfxNames.Taunt(owner, extraTraits))
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                target.AddQEffect(TauntedTarget(caster, target));
            })
            .WithEffectOnChosenTargets(async (caster, targets) =>
            { 
                // Handle when you have too many new taunted creatures
                List<Creature> newTaunts = targets.ChosenCreatures;
                List<Creature> oldTaunts = caster.Battle.AllCreatures
                    .Except(newTaunts)
                    .Where(cr =>
                        cr.FindQEffect(ModData.QEffectIds.TauntTarget) is { } taunt
                        && taunt.Source == caster)
                    .ToList();
                int tauntCount = oldTaunts.Count + newTaunts.Count;
                int overLimit = Math.Max(0, tauntCount - tauntLimit);
                bool passed = false;
                for (int i = 0; i < overLimit; i++)
                {
                    if (oldTaunts.Count == 0)
                        break;
                    if (passed || oldTaunts.Count == overLimit-i)
                    {
                        oldTaunts.Remove(RemoveMyTaunt(caster, oldTaunts[0]));
                        continue;
                    }
                    Creature? chosenCreature = await caster.Battle.AskToChooseACreature(
                        caster,
                        oldTaunts,
                        ModData.Illustrations.Taunt,
                        $"Choose a creature to cease Taunting. (1/{overLimit})",
                        "Remove {Blue}Taunted{/Blue}.",
                        "Pick Anyone");
                    if (chosenCreature == null)
                        passed = true;
                    else
                        oldTaunts.Remove(RemoveMyTaunt(caster, chosenCreature));
                }

                return;
                
                Creature RemoveMyTaunt(Creature taunter, Creature tauntee)
                {
                    tauntee.RemoveAllQEffects(qf =>
                        qf.Id == ModData.QEffectIds.TauntTarget
                        && qf.Source == taunter);
                    return tauntee;
                }
            });
    }

    /// <summary>The "taunted enemy" QEffect.</summary>
    public static QEffect TauntedTarget(Creature taunter, Creature tauntee)
    {
        return new QEffect(
            "Taunted",
            $"Hostile actions that don't include {{Blue}}{taunter.Name}{{/Blue}} take a -1 circumstance penalty to any of its attack rolls and DCs, and you become off-guard until the start of your next turn.",
            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
            taunter,
            ModData.Illustrations.Taunt)
        {
            Id = ModData.QEffectIds.TauntTarget,
            Key = "Taunt"+taunter.Name, // Discard duplicates for each source
            CountsAsADebuff = true,
            BonusToAttackRolls = (qfThis, action, target) =>
            {
                if (!action.HasTrait(Trait.Attack))
                    return null;
                if (action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill != null)
                    return null;
                if (!ActionTriggersTaunt(action, taunter))
                    return null;
                return TauntPenalty(target);
            },
            BonusToSpellSaveDCsForSpecificSpell = (qfThis2, action) =>
            {
                if (!ActionTriggersTaunt(action, taunter))
                    return null;
                return TauntPenalty(action.ChosenTargets.ChosenCreatures
                    .Union(action.ChosenTargets.AllCreaturesInArea)
                    .FirstOrDefault(cr => cr.QEffects.Any(qf =>
                        qf.Id == ModData.QEffectIds.BodyguardCharge && qf.Source == taunter)));
            },
            YouBeginAction = async (qfThis, action) =>
            {
                if (!ActionTriggersTaunt(action, taunter))
                    return;
                QEffect offguard = QEffect.FlatFooted("Taunt");
                offguard.ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn;
                offguard.Source = taunter;
                offguard.Key = ModData.CommonQfKeys.OffGuardDueToTaunt+taunter;
                qfThis.Owner.AddQEffect(offguard);
                
                // PETR: Temporary fix until all CombatActions which have a SavingThrow will let you apply DC penalties.
                if (!action.HasTrait(Trait.Spell))
                {
                    action.WithBonusToSave((action2, attacker, defender) =>
                        TauntPenalty(
                            defender,
                            BonusType.Untyped,
                            "Untyped (Taunt, temporary solution)",
                            true));
                }
            }
        };

        Bonus TauntPenalty(Creature? target, BonusType type = BonusType.Circumstance, string name = "Taunt", bool invert = false)
        {
            int amount;
            if (taunter.HasFeat(ModData.FeatNames.Bodyguard)
                && (target?.QEffects.Any(qf =>
                    qf.Id == ModData.QEffectIds.BodyguardCharge && qf.Source == taunter) ?? false))
                amount = -2;
            else
                amount = -1;
            return new Bonus(amount * (invert?-1:1), BonusType.Circumstance, name, invert ? null : true);
        }

        bool ActionTriggersTaunt(CombatAction action, Creature guardian)
        {
            var chosenCr = action.ChosenTargets.ChosenCreatures;
            var chosenArea = action.ChosenTargets.AllCreaturesInArea;
            return
                (!chosenCr.Contains(guardian) && chosenCr.Any(guardian.FriendOf))
                || (!chosenArea.Contains(guardian) && chosenArea.Any(guardian.FriendOf));
        }
    }

    /// <summary>
    /// Creates a CombatAction with an action cost of 0 (used as a reaction) for a given damage event. This action should be used with <see cref="GameLoop.FullCast(CombatAction)"/> in order to apply its effects. Because this is a reaction, the target is supplied when creating the action; do not use it with the alternative overload (<see cref="GameLoop.FullCast(CombatAction, ChosenTargets)"/>).
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="ally"></param>
    /// <param name="attacker"></param>
    /// <param name="dEvent"></param>
    /// <param name="refundBonusReaction">An additional parameter specifying whether to restore ReactionTime when refunding your reaction.</param>
    public static CombatAction CreateInterceptAttack(Creature owner, Creature attacker, DamageEvent dEvent, bool refundBonusReaction = false)
    {
        const int interceptRange = 3;
        bool canStride = attacker.HasEffect(ModData.QEffectIds.TauntTarget);
        int stepSpeed = owner.HasEffect(QEffectId.ElfStep) ? 2 : 1;
        
        CombatAction interceptAttack = new CombatAction(
                owner,
            ModData.Illustrations.InterceptAttack,
            "Intercept Attack",
            [Trait.Basic, ModData.Traits.Guardian, Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName],
            "{i}You fling yourself in the way of oncoming harm to protect an ally.{/i}\n\nYou can Step, but you must end your movement adjacent to the triggering ally. You take the damage instead of the triggering ally. Apply your own immunities, weaknesses, and resistances to the damage, not the ally's.\n\n{b}Special{/b} You can extend this ability to an ally within 15 feet of you if the damage comes from your taunted enemy. If this ally is farther than you can Step to reach, you can Stride instead of Stepping; you still must end the movement adjacent to your ally.",
            Target.RangedFriend(interceptRange - (canStride ? 0 : 1))
                .WithAdditionalConditionOnTargetCreature((a,d) =>
                {
                    if (attacker == owner.Battle.Pseudocreature)
                        return Usability.NotUsable("Pseudocreature");
                    if (d != dEvent.TargetCreature)
                        return Usability.NotUsableOnThisCreature("Not the target of Intercept Attack");
                    if (!dEvent.KindedDamages.Any(kd =>
                            ModData.CommonRequirements.IsInterceptableDamageType(a, kd)))
                        return Usability.NotUsable("Damage does not trigger Intercept Attack");
                    if (!a.IsAdjacentTo(d)
                        && GetLegalTiles(a, d, canStride).Count == 0)
                        return Usability.NotUsableOnThisCreature("Nowhere to move");
                    return Usability.Usable;
                }))
        .WithActionCost(0)
        .WithActionId(ModData.ActionIds.InterceptAttack)
        .WithTag(dEvent) // Store the damage event
        .WithEffectOnEachTarget(async (action, self, ally, _) =>
        {
            // Pick a tile
            string question = $"Choose where to Step{(canStride ? " or Stride" : null)} with Intercept Attack or right-click to continue. You must end your movement adjacent to the triggering ally.";
            Tile? chosenTile = await self.Battle.AskToChooseATile(
                self,
                Pathfinding.Floodfill(
                        self, self.Battle, new PathfindingDescription()
                        {
                            Squares = self.Speed,
                            Style =
                            {
                                MaximumSquares = self.Speed,
                                PermitsStep = true
                            }
                        })
                    .Where(tile => IsLegalTile(tile, self, ally, canStride)),
                ModData.Illustrations.InterceptAttack,
                question, "Move here",
                true, true,
                "Don\'t step" + (canStride ? " or stride" : null));
            // Step/Stride to that tile
            if (chosenTile == null)
            {
                if (!self.IsAdjacentTo(ally))
                {
                    string log = "reaction";
                    if (refundBonusReaction)
                    {
                        self.Actions.ReactionsUsedUpThisRound.Remove(ModData.CommonReactionKeys.ReactionTime);
                        log = "bonus " + log;
                    }
                    else
                        self.Actions.RefundReaction();
                    self.Battle.Log(log.Capitalize() + " refunded: no square chosen, and not adjacent to ally.");
                    return;
                }
            }
            else
                await self.StrideAsync(question, allowStep: true, strideTowards: chosenTile);
                
            DamageEvent interceptedDamage = new DamageEvent(
                dEvent.CombatAction,
                owner,
                dEvent.CheckResult,
                [..dEvent.KindedDamages])
            {
                IsSplashDamage = dEvent.IsSplashDamage,
                //DoubleDamage = dEvent.DoubleDamage, // Doubles twice
                //HalveDamage = dEvent.HalveDamage, // Halves twice
                //Bonuses = @event.Bonuses, // Recalculates
            };
            self.Overhead(
                "intercept attack",
                Color.White,
                self + " {b}Intercepts{/b} the {b}Attack{/b} against " + ally + ".",
                action.Name +" {icon:Reaction}",
                action.Description,
                action.Traits);
            await CommonSpellEffects.DealDirectDamage(interceptedDamage);
            dEvent.ReduceBy(dEvent.KindedDamages.Sum(kd => kd.ResolvedDamage), "Intercept attack");
        });
        
        return interceptAttack;

        List<Tile> GetLegalTiles(Creature guardian, Creature ally_2, bool canStride_2)
        {
            return guardian.Battle.Map.AllTiles
                .Where(tile => IsLegalTile(tile, guardian, ally_2, canStride_2))
                .ToList();
        }

        bool IsLegalTile(Tile tile, Creature guardian, Creature ally_2, bool canStride_2)
        {
            return tile.IsAdjacentTo(ally_2.Occupies)
                    && tile.LooksFreeTo(guardian)
                    && (canStride_2 || guardian.HasEffect(QEffectId.FeatherStep) ||
                        !tile.CountsAsNonignoredDifficultTerrainFor(guardian))
                    && guardian.DistanceTo(tile) <= (canStride_2 ? guardian.Speed : stepSpeed);
        }
    }
}