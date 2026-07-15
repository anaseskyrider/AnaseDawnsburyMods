using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Controls.Portraits;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.LoresAndWeaknesses;

namespace Dawnsbury.Mods.KholoAncestry;

public static class AncestryFeats
{
    public static void LoadFeats()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        #region Level 1
        
        // Ask The Bones
        yield return new TrueFeat(
                ModData.FeatNames.AskTheBones,
                1,
                "You keep the bones of a knowledgeable ancestor or friend to call upon for advice.",
                $$"""
                  {b}Frequency{/b} once per day

                  Attempt to {{RecallWeakness.GetActionLink()}} with a +1 circumstance bonus to your check.
                  """,
                [ModData.Traits.Kholo])
            .WithActionCost(0)
            .WithPermanentQEffect(
                "Recall Weakness with a +1 circumstance bonus to your check.",
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.AskTheBones))
                            return null;
                        
                        CombatAction ask = new CombatAction(
                                qfThis.Owner,
                                IllustrationName.ArmorOfBones,
                                "Ask the Bones",
                                [ModData.Traits.ModName, Trait.Basic, ModData.Traits.Kholo, Trait.DoesNotBreakStealth, Trait.UnaffectedByConcealment],
                                """
                                {i}You keep the bones of a knowledgeable ancestor or friend to call upon for advice.{/i}

                                {b}Frequency{/b} once per day

                                Attempt to Recall Weakness with a +1 circumstance bonus to your check.
                                """,
                                Target.Self())
                            .WithActionCost(0)
                            .WithEffectOnEachTarget(async (action, caster, target, result) =>
                            {
                                CombatAction recall = RecallWeakness.CreateRecallWeaknessAction(caster);
                                QEffect bonesBonus = new QEffect()
                                {
                                    Name = "[ASK THE BONES]",
                                    BonusToAttackRolls = (_, combatAction, _) =>
                                        combatAction == recall
                                            ? new Bonus(1, BonusType.Circumstance, "Ask the bones")
                                            : null,
                                };
                                caster.AddQEffect(bonesBonus);
                                if (await caster.Battle.GameLoop.FullCast(recall))
                                    qfThis.Owner.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.AskTheBones);
                                else
                                    action.RevertRequested = true;
                                bonesBonus.ExpiresAt = ExpirationCondition.Immediately;
                            });
                        
                        return (ActionPossibility)ask;
                    };
                });
        
        // Crunch
        // TODO: See if I can implement extra held grapples anyway.
        yield return new TrueFeat(
            ModData.FeatNames.Crunch,
            1,
            "Your jaws can crush bone and bite through armor.",
            "Your jaws unarmed attack deals 1d8 piercing damage instead of 1d6 and gains the versatile B trait.",
            [ModData.Traits.Kholo]);
        
        // Scent (familiar ability)
        yield return Familiars.CreateFamiliarAbility(
                ModData.FeatNames.FamiliarScent,
                "Your familiar has enhanced olfactory perception.",
                "You gain imprecise scent with a range of 15 feet, which means that creatures can't be undetected within the area while you are conscious.")
            .WithOnCreature(self =>
            {
                self.AddQEffect(ImpreciseScent(
                    "Scent",
                    "You detect creatures within 15 feet using your familiar's imprecise scent.",
                    3,
                    true
                ));
            });
        
        // Hyena Familiar
        yield return new TrueFeat(
                ModData.FeatNames.HyenaFamiliar,
                1,
                "Hyenas serve kholo as pets and trackers. Some kholos, such as yourself, draw the attention of smaller hyenas that are vessels for magical spirits.",
                $"You gain a hyena as a {FeatName.ClassFamiliar.ToLink("combat familiar")}. It always has the {ModData.FeatNames.FamiliarScent.ToLink("scent")} ability prepared, which counts against the number of familiar abilities it has.",
                [ModData.Traits.Kholo, ModData.Traits.DeployableFamiliarFeat])
            .WithIllustration(ModData.Illustrations.HyenaFamiliar)
            .WithEquivalent(values => values.Tags.ContainsKey(Familiars.FAMILIAR_KEY))
            .WithOnSheet(values =>
            {
                FamiliarTag hyena = new FamiliarTag()
                {
                    FamiliarAbilities = 1, // Fewer choices than normal due to pre-selected ability.
                    Illustration = ModData.Illustrations.HyenaFamiliar,
                };
                values.Tags[Familiars.FAMILIAR_KEY] = hyena;
                values.AddSelectionOptionRightNow(
                    new SingleFeatSelectionOption(
                            "FamiliarIllustrationDisplay",
                            "Show familiar",
                            -1,
                            ft => ft.HasTrait(Trait.FamiliarIllustrationDisplay))
                        .WithIsOptional());
                values.AddSelectionOptionRightNow(
                    new CompanionIdentitySelectionOption(
                            "FamiliarName",
                            "Familiar identity",
                            -1,
                            "You can name your familiar.\n\nIf you don't choose a name, it will be called {b}Hyena{/b}.",
                            "Hyena",
                            ModData.Illustrations.HyenaFamiliar,
                            [PortraitCategory.Familiars, PortraitCategory.AnimalCompanions, PortraitCategory.Custom],
                            (val, txt) =>
                            {
                                if (!val.Tags.TryGetValue(Familiars.FAMILIAR_KEY, out object? obj)
                                    || obj is not FamiliarTag famTag)
                                    return;
                                CompanionIdentitySelectionOption.SetFamiliarDataFromSection(famTag, txt);
                            })
                        .WithIsOptional());
                values.AtEndOfRecalculationBeforeMorningPreparations += values2 =>
                {
                    if (!values2.Tags.TryGetValue(Familiars.FAMILIAR_KEY, out object? obj)
                        || obj is not FamiliarTag famTag2)
                        return;
                    values2.HasMorningPreparations = true;
                    values2.AddSelectionOption(
                        new MultipleFeatSelectionOption(
                            "FamiliarAbilities",
                            "Familiar abilities",
                            SelectionOption.MORNING_PREPARATIONS_LEVEL,
                            (ft, values3) =>
                            {
                                if (!ft.HasTrait(Trait.CombatFamiliarAbility))
                                    return false;
                                if (ft.Tag is not Trait tag2
                                    || values3.AdditionalClassTraits.Contains(tag2))
                                    return true;
                                ClassSelectionFeat? classSelectionFeat = values3.Class;
                                return classSelectionFeat != null
                                       && classSelectionFeat.ClassTrait == tag2;
                            },
                            famTag2.FamiliarAbilities)
                        {
                            DoNotApplyEffectsInsteadOfRemovingThem = true
                        });
                };
                // Granted abilities
                values.GrantFeat(ModData.FeatNames.FamiliarScent);
            });

        // Kholo Lore
        yield return new TrueFeat(
                ModData.FeatNames.KholoLore,
                1,
                "You paid close attention to the senior hunters in your clan to learn their tricks.",
                $"""
                You gain the trained proficiency rank in Stealth and Survival. If you would automatically become trained in one of those skills (from your background or class, for example), you instead become trained in a skill of your choice.
                
                You gain the {RecallWeakness.FNAdditionalLore.ToLink("Additional Lore")} skill feat with Kholo Lore, a specific lore skill that can be used to {RecallWeakness.GetActionLink()} on kholos and similar ancestries.
                """,
                [ModData.Traits.Kholo])
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Stealth);
                values.TrainInThisOrSubstitute(Skill.Survival);
                values.TrainInThisOrSubstitute(Kholo.KholoLore);
                values.IncreaseProficiency(3, Kholo.KholoLore.Trait, Proficiency.Expert);
                values.IncreaseProficiency(7, Kholo.KholoLore.Trait, Proficiency.Master);
                values.IncreaseProficiency(15, Kholo.KholoLore.Trait, Proficiency.Legendary);
            });

        // Kholo Weapon Familiarity
        yield return new TrueFeat(
                ModData.FeatNames.KholoWeaponFamiliarity,
                1,
                "You gain greater access to weapons specific to your cultural lineage.",
                $"You have familiarity with {ModData.Tooltips.KholoWeapon("kholo weapons")} — for the purpose of proficiency, you use your proficiency with any simple weapon for simple kholo weapons, and you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.\n\nAt 5th level, whenever you get a critical hit with one of these weapons, you get its {{tooltip:criteffect}}critical specialization effect{{/}}.",
                [ModData.Traits.Kholo])
            .WithOnSheet(values =>
            {
                // Trained in simple kholo weapons, using however your class scales itself.
                foreach (Trait weapon in Kholo.KholoWeapons)
                {
                    values.Proficiencies.AutoupgradeAlongBestWeaponProficiency(
                        [Trait.Simple, weapon]);
                }

                // Martial -> Simple
                values.Proficiencies.AddProficiencyAdjustment(
                    traits => traits.Any(Kholo.KholoWeapons.Contains) && traits.Contains(Trait.Martial),
                    Trait.Simple);
                // Advanced -> Martial
                values.Proficiencies.AddProficiencyAdjustment(
                    traits => traits.Any(Kholo.KholoWeapons.Contains) && traits.Contains(Trait.Advanced),
                    Trait.Martial);
            })
            .WithPermanentQEffect(
                $"You have familiarity with {ModData.Tooltips.KholoWeapon("kholo weapons")}",
                qfFeat =>
                {
                    if (qfFeat.Owner.Level < 5)
                    {
                        qfFeat.Description += ".";
                        return;
                    }
                    qfFeat.Description += " {Blue}and they trigger {tooltip:criteffect}critical specialization effects{/}{/Blue}.";
                    qfFeat.YouHaveCriticalSpecialization = (_, item, _, _) =>
                        item.Traits.Any(Kholo.KholoWeapons.Contains);
                });
        
        // Pack Hunter
        // TODO: Ensure that a Gunslinger who uses Fake Out to aid the Pack Hunter gets a bonus.
        yield return new TrueFeat(
                ModData.FeatNames.PackHunter,
                1,
                "You were taught how to hunt as part of a pack.",
                $$"""
                  You gain a +2 circumstance bonus to checks to Aid, and your allies gain a +2 circumstance bonus to checks to Aid you.

                  {{ModData.Illustrations.DawnsburySun.IllustrationAsIconString}} {b}Modding{/b} Requires another mod which includes Aiding in some form.
                  """,
                [ModData.Traits.Kholo])
            .WithPermanentQEffect(
                "You have a +2 circumstance bonus on checks to Aid {icon:Reaction}, and your allies gain the same bonus to Aiding you.",
                qfFeat =>
                {
                    qfFeat.BonusToAttackRolls = (qfThis, action, defender) =>
                    {
                        if (action.Name.Contains("Aid Strike")
                            || action.ActionId == ModData.ActionIds.AidReaction)
                            return new Bonus(2, BonusType.Circumstance, "Pack Hunter");

                        return null;
                    };
                    qfFeat.AddGrantingOfTechnical(
                        cr => cr.FriendOfAndNotSelf(qfFeat.Owner),
                        qfTech =>
                        {
                            qfTech.BonusToAttackRolls = (qfThis, action, defender) =>
                            {
                                if ((action.Name.Contains("Aid Strike")
                                     || action.ActionId == ModData.ActionIds.AidReaction)
                                    && (defender == qfFeat.Owner
                                        || (action.Tag is CombatAction aidableAction
                                            && aidableAction.Owner == qfFeat.Owner)))
                                    return new Bonus(2, BonusType.Circumstance, "Pack Hunter");
                                
                                return null;
                            };
                        });
                });
        
        // Sensitive Nose
        yield return new TrueFeat(
                ModData.FeatNames.SensitiveNose,
                1,
                "Your large black nose isn't just for show. You can pick up on the faintest scents near you and track them down.",
                "You gain imprecise scent with a range of 30 feet, which means that creatures can't be undetected within the area while you are conscious.",
                [ModData.Traits.Kholo])
            .WithOnCreature(self =>
            {
                self.AddQEffect(ImpreciseScent(
                    "Sensitive Nose",
                    "You detect creatures within 30 feet using your imprecise scent.",
                    6
                ));
            });

        #endregion

        #region Level 5
        
        // Absorb Strength
        yield return new TrueFeat(
                ModData.FeatNames.AbsorbStrength,
                5,
                "You consume a piece of your enemy, absorbing their strength.",
                $$"""
                  {b}Frequency{/b} once per encounter
                  {b}Requirements{/b} You are adjacent to an enemy's {{ModData.Illustrations.AbsorbStrengthMeatBigger.IllustrationAsIconString}} corpse.

                  You gain temporary Hit Points equal to the enemy's level (minimum of 1).

                  {{ModData.Illustrations.AbsorbStrengthMeatBigger.IllustrationAsIconString}} {b}Corpses{/b} This feat leaves behind a piece of an enemy on death. These corpses don't occupy their space nor block line of sight, and can't be targeted in any way. Undead and constructs {Red}do not{/Red} leave behind a consumable corpse.
                  """,
                [ModData.Traits.Kholo])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Eat a piece of an enemy corpse to gain temp HP.",
                qfFeat =>
                {
                    qfFeat.AddGrantingOfTechnical(
                        cr => 
                            cr.EnemyOf(qfFeat.Owner) 
                            && !(cr.HasTrait(Trait.Construct) || cr.HasTrait(Trait.Undead) || cr.HasTrait(Trait.Object)),
                        qfTech =>
                        {
                            qfTech.Key = "KholoAbsorbStrength"; // Apply only once
                            qfTech.WhenCreatureDiesAtStateCheckAsync = async qfDie =>
                            {
                                // When a monster dies, place a tile effect that grants a state check ephemeral action to Kholos near the corpse.
                                Tile here = qfDie.Owner.Space.TopLeftTile;
                                int level = Math.Max(qfDie.Owner.Level, 1);
                                here.AddQEffect(new TileQEffect(here)
                                {
                                    Illustration = ModData.Illustrations.AbsorbStrengthMeat,
                                    StateCheck = qfCorpse =>
                                    {
                                        foreach (Creature cr in qfCorpse.Owner.Neighbours
                                                     .CreaturesPlusCreatureOnSelf
                                                     .Where(cr => cr.HasFeat(ModData.FeatNames.AbsorbStrength)))
                                        {
                                            cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                            {
                                                ProvideContextualAction = qfEat =>
                                                {
                                                    CombatAction absorb = new CombatAction(
                                                            qfEat.Owner,
                                                            IllustrationName.Jaws,
                                                            "Absorb Strength",
                                                            [ModData.Traits.Kholo],
                                                            "{i}You consume a piece of your enemy, absorbing their strength.{/i}\n\n{b}Frequency{/b} once per encounter\n{b}Requirements{/b} You are adjacent to an enemy's corpse.\n\nYou gain temporary Hit Points equal to the enemy's level.",
                                                            Target.Self()
                                                                .WithAdditionalRestriction(self =>
                                                                    self.HasEffect(ModData.QEffectIds.AbsorbStrengthImmunity) ? "Already used this encounter" : null))
                                                        .WithActionCost(1)
                                                        .WithSoundEffect(SfxName.GluttonBite)
                                                        .WithEffectOnSelf(async self =>
                                                        {
                                                            self.GainTemporaryHP(level);
                                                            self.AddQEffect(new QEffect()
                                                            {
                                                                Id = ModData.QEffectIds.AbsorbStrengthImmunity,
                                                            });
                                                            qfCorpse.ExpiresAt = ExpirationCondition.Immediately;
                                                        });
                                                    return (ActionPossibility)absorb;
                                                },
                                            });
                                        }
                                    }
                                });
                            };
                        });
                });

        // Affliction Resistance
        yield return new TrueFeat(
                ModData.FeatNames.AfflictionResistance,
                5,
                "Your diet has strengthened you against diseases and poisons.",
                "You gain a +1 circumstance bonus to saving throws against diseases and poisons. If you roll a success on a saving throw against a disease or poison, you get a critical success instead. If you have the {i}Juggernaut{/i} class feature, if you roll a critical failure on the save you get a failure instead.",
                // If you have a class feature such as {i}Juggernaut{/i} that would improve the save in this way,
                [ModData.Traits.Kholo])
            .WithPermanentQEffect(
                "You have a +1 circumstance bonus to saves against diseases and poisons. When you succeed on such a saving throw, you critically succeed instead",
                qfFeat =>
                {
                    bool isGreater = qfFeat.Owner.QEffects.Any(qf =>
                        qf.Name?.ToLower().Contains("juggernaut") ?? false);
                    qfFeat.Description += isGreater
                        ? ", and if you critically fail, you fail instead."
                        : ".";
                    qfFeat.BonusToDefenses = (_, action, def) =>
                        def.IsSavingThrow()
                        && action is not null
                        && (action.HasTrait(Trait.Disease) || action.HasTrait(Trait.Poison))
                            ? new Bonus(1, BonusType.Circumstance, "Affliction resistance")
                            : null; 
                    qfFeat.AdjustSavingThrowCheckResult = (_, _, action, result) =>
                    {
                        if (!(action.HasTrait(Trait.Disease) || action.HasTrait(Trait.Poison)))
                            return result;
                        if (result == CheckResult.Success)
                            return CheckResult.CriticalSuccess;
                        if (result == CheckResult.CriticalFailure && isGreater)
                            return CheckResult.Failure;
                        return result;
                    };
                });

        // Distant Cackle
        yield return new TrueFeat(
                ModData.FeatNames.DistantCackle,
                5,
                "It takes a very brave person to enter the laughter-haunted forest where you dwell.",
                $"You can cast {SpellId.Fear.ToLink("fear", ModData.Traits.Kholo, 1).WithTag("i")} once per day as a 1st-rank occult innate spell.",
                [ModData.Traits.Kholo])
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.KholoWitch),
                "Must have the witch kholo heritage.")
            .WithRulesBlockForSpell(SpellId.Fear, ModData.Traits.Kholo, 1)
            .WithOnCreature(self =>
            {
                self.GetOrCreateSpellcastingSource(
                        SpellcastingKind.Innate,
                        ModData.Traits.Kholo,
                        Ability.Charisma,
                        Trait.Occult)
                    .WithSpells([SpellId.Fear], 1);
            });

        // Left-hand Blood
        yield return new TrueFeat(
                ModData.FeatNames.LefthandBlood,
                5,
                "It's said that the flesh of the left side of a hyena is deadly and poisonous.",
                """
                {b}Frequency{/b} once per combat

                You deal 1 slashing damage to yourself to poison a weapon you are holding. If you hit with the weapon and deal damage, the target also takes 1d4 persistent poison damage. The poison on your weapon becomes inert after you hit, or at the end of your next turn, whichever comes first.
                """,
                [ModData.Traits.Kholo])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Once per combat, take 1 slashing damage to poison your next weapon hit this turn.",
                qfFeat =>
                {
                    qfFeat.Tag = false;
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.Tag is true)
                            return null;
                        return (ActionPossibility)new CombatAction(
                                qfThis.Owner,
                                IllustrationName.BloodVendetta,
                                "Left-hand Blood",
                                [ModData.Traits.Kholo],
                                null!,
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.HeldItems.Any(item => item.HasTrait(Trait.Weapon))
                                            ? null : "Must hold a weapon"))
                            .WithDescription(
                                "It's said that the flesh of the left side of a hyena is deadly and poisonous.",
                                "{b}Frequency{/b} once per combat\n\nYou deal 1 slashing damage to yourself to poison a weapon you are holding. If you hit with the weapon and deal damage, the target also takes 1d4 persistent poison damage. The poison on your weapon becomes inert after you hit, or at the end of your next turn, whichever comes first.")
                            .WithActionCost(1)
                            .WithEffectOnEachTarget(async (action, caster, _, _) =>
                            {
                                // Take damage
                                Sfxs.Play(SfxName.ImpactFlesh);
                                await CommonSpellEffects.DealDirectDamage(
                                    action,
                                    DiceFormula.FromText("1", "Left-hand blood"),
                                    caster,
                                    CheckResult.Success,
                                    DamageKind.Slashing);
                                
                                // Pick a weapon
                                Item poisonedWeapon;
                                switch (caster.HeldItems.Count(IsValidTarget))
                                {
                                    case 0:
                                        return;
                                    case 1:
                                        poisonedWeapon = caster.HeldItems.First(IsValidTarget);
                                        break;
                                    default:
                                        poisonedWeapon = await caster.Battle.AskForConfirmation(
                                            caster,
                                            IllustrationName.AlchemicalPoison,
                                            "Which weapon would you like to poison?",
                                            caster.HeldItems[0].Name,
                                            caster.HeldItems[1].Name)
                                            ? caster.HeldItems[0]
                                            : caster.HeldItems[1];
                                        break;
                                }

                                QEffect lhbQf = new QEffect(
                                        "Left-hand Blood",
                                        "The next time you hit and deal damage with " + poisonedWeapon.Name +
                                        ", the target takes 1d4 persistent poison damage.",
                                        ExpirationCondition.ExpiresAtEndOfYourTurn,
                                        caster,
                                        IllustrationName.AlchemicalPoison)
                                    {
                                        CountsAsABuff = true,
                                    }
                                    .WithExpirationAtEndOfOwnersNextTurn();
                                // Not part of constructor due to lack of expiration reference
                                lhbQf.AfterYouDealDamage = async (_, action2, defender) =>
                                {
                                    if (action2.Item == poisonedWeapon
                                        && action2.CheckResult >= CheckResult.Success
                                        && action2.HasTrait(Trait.Strike))
                                    {
                                        lhbQf.ExpiresAt = ExpirationCondition.Immediately;
                                        defender.AddQEffect(QEffect.PersistentDamage("1d4", DamageKind.Poison));
                                    }
                                };
                                caster.AddQEffect(lhbQf);

                                qfThis.Tag = true;
                            });
                    };
                    
                    return;

                    bool IsValidTarget(Item item)
                    {
                        return item.HasTrait(Trait.Weapon);
                    }
                });

        // Pack Stalker
        yield return new TrueFeat(
                ModData.FeatNames.PackStalker,
                5,
                "Ambushes are an honored kholo tradition.",
                $$"""
                  {b}Requirements{/b} {{ModData.Illustrations.DawnsburySun.IllustrationAsIconString}} You have the {i}Exploration Activities{/i} mod installed.

                  While you are Avoiding Notice, your allies also gain the benefits of the exploration activity. {i}(This effect applies after initiative is already determined, so your allies must still take the activity to roll using Stealth.){i}
                  """,
                [ModData.Traits.Kholo])
            .WithPermanentQEffect(
                "Your allies can also Avoid Notice when you do.",
                qfFeat =>
                {
                    qfFeat.StartOfCombatBeforeOpeningCutscene = async qfThis =>
                    {
                        if (!ModManager.TryParse("AvoidNotice", out FeatName avoidNotice)
                            || !qfThis.Owner.HasFeat(avoidNotice))
                            return;
                        qfThis.Owner.Battle.AllCreatures
                            .Where(cr => cr.FriendOfAndNotSelf(qfThis.Owner))
                            .ForEach(cr => cr.WithFeat(avoidNotice));
                    };
                });

        // Rabid Sprint
        yield return new TrueFeat(
                ModData.FeatNames.RabidSprint,
                5,
                "You run on all fours as fast as you can.",
                """
                {b}Requirements{/b} You have both your hands free.

                Stride three times.
                """,
                [ModData.Traits.Kholo])
            .WithActionCost(2)
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.KholoDog),
                "Must have the dog kholo heritage.")
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    qfFeat.ProvideMainAction = qfThis =>
                    {
                        return (ActionPossibility)new CombatAction(
                                qfThis.Owner,
                                IllustrationName.WarpStep,
                                "Rabid Sprint",
                                [Trait.Flourish, ModData.Traits.Kholo],
                                null!,
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.HeldItems.Count == 0
                                            ? null : "Hands must be empty"))
                            .WithDescription(
                                "You run on all fours as fast as you can.",
                                "{b}Requirements{/b} You have both your hands free.\n\nStride three times.")
                            .WithShortDescription("While your hands are free, Stride three times.")
                            .WithActionCost(2)
                            .WithEffectOnEachTarget(async (action, caster, _, _) =>
                            {
                                if (!await caster.StrideAsync("Choose where to Stride with Rabid Sprint. (1/3)", allowCancel: true))
                                    action.RevertRequested = true;
                                else if (!await caster.StrideAsync("Choose where to Stride with Rabid Sprint. (2/3)", allowPass: true))
                                {
                                    caster.Battle.Log("Rabid Sprint was converted to a simple Stride.");
                                    action.SpentActions = 1;
                                    action.RevertRequested = true;
                                }
                                else
                                    await caster.StrideAsync("Choose where to Stride with Rabid Sprint. (3/3)", allowPass: true);
                            });
                    };
                });

        // Right-hand Blood
        yield return new TrueFeat(
                ModData.FeatNames.RighthandBlood,
                5,
                "It's said that the flesh of the right side of a hyena can heal diseases.",
                "When you stabilize {icon:TwoActions} or staunch bleeding {icon:TwoActions}, you can deal 1 slashing damage to yourself to feed someone blood from your right side, gaining a +1 item bonus to your check.",
                [ModData.Traits.Kholo])
            .WithPermanentQEffect(
                "You can damage yourself to give yourself an item bonus to stabilize or stop bleeding.",
                qfFeat =>
                {
                    qfFeat.YouBeginAction = async (qfThis, action) =>
                    {
                        if (action.Name is not ("Stabilize" or "Staunch bleeding")
                            || (qfThis.Owner.HP + qfThis.Owner.TemporaryHP) <= 1
                            || !await qfThis.Owner.AskForConfirmation(
                                IllustrationName.BloodVendetta,
                                "{b}Right-hand Blood{/b}\nYou're about to use {Blue}"+action.Name+"{/Blue}. Use blood from your right side to gain a +1 item bonus to this check?",
                                "Take 1 slashing damage"))
                            return;
                        Sfxs.Play(SfxName.ImpactFlesh);
                        await CommonSpellEffects.DealDirectDamage(
                            null,
                            DiceFormula.FromText("1", "Right-hand blood"),
                            qfThis.Owner,
                            CheckResult.Success,
                            DamageKind.Slashing);
                        qfThis.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                        {
                            BonusToSkillChecks = (_, action2, _) => 
                                action2 == action
                                    ? new Bonus(1, BonusType.Item, "Right-hand blood")
                                    : null,
                            AfterYouTakeAction = async (qfThis2, action2) =>
                            {
                                if (action2 != action)
                                    return;
                                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                            }
                        });
                    };
                });

        #endregion

        #region Level 9

        // Ambush Hunter
        yield return new TrueFeat(
                ModData.FeatNames.AmbushHunter,
                9,
                "You are always searching for the perfect opportunity to ambush your enemies.",
                $$"""
                  {b}Requirements{/b} {{ModData.Illustrations.DawnsburySun.IllustrationAsIconString}} You have the {i}Exploration Activities{/i} mod installed.

                  You can perform the Scout exploration activity at the same time as the Avoid Notice exploration activity. {i}(Selecting either activity grants the benefits of the other.){/i}
                  """,
                [ModData.Traits.Kholo])
            .WithOnCreature(self =>
            {
                QEffect statBlockEntry = new QEffect("Ambush Hunter", "You can Avoid Notice and Scout at the same time.");
                if (!ModManager.TryParse("AvoidNotice", out FeatName avoidNotice)
                    || !ModManager.TryParse("ScoutActivity", out FeatName scout)
                    || (!self.HasFeat(avoidNotice) && !self.HasFeat(scout)))
                {
                    statBlockEntry.Description = new SimpleIllustration(IllustrationName.RedWarning).IllustrationAsIconString + " You can {Red}Avoid Notice{/Red} and {Red}Scout{/Red} at the same time.";
                }
                else
                {
                    if (self.HasFeat(avoidNotice) && !self.HasFeat(scout))
                        self.WithFeat(scout);
                    else if (self.HasFeat(scout) && !self.HasFeat(avoidNotice))
                        self.WithFeat(avoidNotice);
                }
                self.AddQEffect(statBlockEntry);
            });

        // Breath Like Honey
        // TODO: Consider upgrading into free always Making an Impression
        yield return new TrueFeat(
                ModData.FeatNames.BreathLikeHoney,
                9,
                "You smell of honey and savory things.",
                $"You can cast {SpellId.Soothe.ToLink("soothe", ModData.Traits.Kholo, 3).WithTag("i")} once per day as an occult innate spell, heightened to 3rd-rank.",
                [ModData.Traits.Kholo])
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.KholoSweetbreath),
                "Must have the sweetbreath kholo heritage.")
            .WithRulesBlockForSpell(SpellId.Soothe, ModData.Traits.Kholo, 3)
            .WithOnCreature(self =>
            {
                self.GetOrCreateSpellcastingSource(
                        SpellcastingKind.Innate,
                        ModData.Traits.Kholo,
                        Ability.Charisma,
                        Trait.Occult)
                    .WithSpells([SpellId.Soothe], 3);
            });

        // Grandmother's Wisdom
        yield return new TrueFeat(
                ModData.FeatNames.GrandmothersWisdom,
                9,
                "You carry the bones of your ancestors with you, who in turn watch over you.",
                $"You can cast {SpellId.DeflectCriticalHit.ToLink("deflect critical hit", ModData.Traits.Kholo, 3).WithTag("i")} once per day as a 3rd-rank occult innate spell.",
                [ModData.Traits.Kholo])
            .WithRulesBlockForSpell(SpellId.DeflectCriticalHit, ModData.Traits.Kholo, 3)
            .WithOnCreature(self =>
            {
                self.GetOrCreateSpellcastingSource(
                        SpellcastingKind.Innate,
                        ModData.Traits.Kholo,
                        Ability.Charisma,
                        Trait.Occult)
                    .WithSpells([SpellId.DeflectCriticalHit], 3);
            });

        // Laughing Kholo
        yield return new TrueFeat(
                ModData.FeatNames.LaughingKholo,
                9,
                "Your sinister giggle is a sound of warning and threat.",
                $"You gain the {FeatName.IntimidatingGlare.ToLink("Intimidating Glare")} and {FeatName.BattleCry.ToLink("Battle Cry")} skill feats.",
                [ModData.Traits.Kholo])
            .WithPrerequisite(
                values => values.HasFeat(FeatName.MasterIntimidation),
                "Must be a master in Intimidation.")
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.IntimidatingGlare);
                values.GrantFeat(FeatName.BattleCry);
            });

        #endregion

        #region Level 13

        // Ancestor's Rage
        // DOC: Reordered Animal Form list.
        ModManager.RegisterActionOnEachSpell(spell =>
        {
            if (spell.SpellId is not SpellId.AnimalForm
                || spell.Variants is null)
                return;
            // Reorder list to put the Wolf first, resolving the bug described further down.
            SpellVariant[] wolfFirst = spell.Variants
                .OrderBy(v => v.Name == "Wolf" ? 0 : 1)
                .ThenBy(v => spell.Variants.IndexOf(v))
                .ToArray();
            spell.WithVariants(wolfFirst);
        });
        yield return new TrueFeat(
                ModData.FeatNames.AncestorsRage,
                13,
                "You transform into an enormous, otherworldly hyena.",
                $"You can cast {SpellId.AnimalForm.ToLink("animal form", ModData.Traits.Kholo, 5)} (canine form only) once per day as a 5th-rank occult innate spell.",
                [ModData.Traits.Kholo])
            .WithRulesBlockForSpell(SpellId.AnimalForm, ModData.Traits.Kholo, 5)
            .WithOnCreature(self =>
            {
                self.GetOrCreateSpellcastingSource(
                        SpellcastingKind.Innate,
                        ModData.Traits.Kholo,
                        Ability.Charisma,
                        Trait.Occult)
                    .WithSpells([SpellId.AnimalForm], 5);
                self.AddQEffect(new QEffect()
                {
                    Name = "[KHOLO: ANCESTOR'S RAGE]",
                    ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.SpellId != SpellId.AnimalForm
                            || action.SpellcastingSource?.ClassOfOrigin != ModData.Traits.Kholo
                            || action.Variants?.FirstOrDefault(v => v.Name == "Wolf")
                                is not {} wolf)
                            return;
                        // BUG: This restriction doesn't seem to be recognized
                        // for the first variant in an action.
                        action.Target = Target.DependsOnSpellVariant(variant =>
                        {
                            SelfTarget formTarget = Target.Self();
                            if (variant.Id != "WOLF")
                                formTarget.WithAdditionalRestriction(_ => "Canine form only");
                            return formTarget;
                        });
                    }
                });
            });

        // Bonekeeper's Bane
        yield return new TrueFeat(
                ModData.FeatNames.BonekeepersBane,
                13,
                null,
                """
                Whenever an enemy starts its turn adjacent to you, it must attempt a Will saving throw against your class DC or spell DC {i}(whichever is highest){/i}. On a failure, the enemy takes a –1 status penalty to attack rolls and skill checks. This effect ends when they are no longer adjacent to you.

                Regardless of the result of its save, they are then immune to bonekeeper's bane.
                """,
                [ModData.Traits.Kholo])
            .WithPermanentQEffect(
                "",
                qfFeat =>
                {
                    qfFeat.AddGrantingOfTechnical(
                        cr => cr.EnemyOf(qfFeat.Owner) && cr.IsAdjacentTo(qfFeat.Owner),
                        qfTech =>
                        {
                            qfTech.Id = ModData.QEffectIds.BonekeepersBaneStartOfTurn;
                            qfTech.StartOfYourPrimaryTurn = async (qfTech2, self) =>
                            {
                                CombatAction boneBane = new CombatAction(qfFeat.Owner, IllustrationName.Bane,
                                        "Bonekeeper's Bane", [ModData.Traits.ModName, ModData.Traits.Kholo],
                                        "Whenever an enemy starts its turn adjacent to you, it must attempt a Will saving throw against your class DC or spell DC {i}(whichever is highest){/i}. On a failure, the enemy takes a –1 status penalty to attack rolls and skill checks. This effect ends when they are no longer adjacent to you.\n\nRegardless of the result of its save, they are then immune to bonekeeper's bane.",
                                        Target.Self())
                                    .WithSavingThrow(new SavingThrow(Defense.Will, _ => qfFeat.Owner.ClassOrSpellDC()));

                                if (await CommonSpellEffects.RollSavingThrowAsync(self, boneBane,
                                        boneBane.SavingThrow!) > CheckResult.Failure)
                                    return;

                                QEffect bane = new QEffect(
                                    "Bonekeeper's Bane",
                                    $"You have a {"-1 status penalty".WithColor("Red")} to attack rolls and skill checks. This effect ends if you cease being adjacent to {qfFeat.Owner.ToColoredName()}.",
                                    ExpirationCondition.Never,
                                    qfFeat.Owner,
                                    IllustrationName.Bane)
                                {
                                    SourceAction = boneBane,
                                    StateCheck = qfThis =>
                                    {
                                        if (!qfThis.Owner.IsAdjacentTo(qfThis.Source!))
                                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                                    },
                                    BonusToSkills = _ => new Bonus(-1, BonusType.Status, "Bonekeeper's bane"),
                                    BonusToAttackRolls = (_, action, _) =>
                                        action.HasTrait(Trait.Attack)
                                        // PETR: Deviation from tabletop makes skill attacks an attack roll
                                        /*&& action.ActiveRollSpecification is not null
                                        && action.ActiveRollSpecification.TaggedDetermineBonus.InvolvedSkill is null*/
                                            ? new Bonus(-1, BonusType.Status, "Bonekeeper's bane")
                                            : null
                                };
                                self.AddQEffect(bane);
                                self.AddQEffect(QEffect.ImmunityToCondition(ModData.QEffectIds.BonekeepersBaneStartOfTurn)
                                    .With(qf => qf.StateCheck = sc => sc.Owner.WeaknessAndResistance.OtherImmunities.Add("bonekeeper's bane")));
                            };
                        });
                });

        #endregion

        #region Level 17

        // First to Strike, First to Fall
        yield return new TrueFeat(
                ModData.FeatNames.FirstToStrikeFirstToFall,
                17,
                null,
                "Whenever you successfully Strike a creature that has not acted in the first round of combat, that creature is off-guard until the end of your next turn. If that creature is reduced to 0 Hit Points before the end of your next turn, you and all allies within 30 feet of the creature become quickened until the end of your next turn. You can use the extra action only to move or Strike.",
                [ModData.Traits.Kholo])
            .WithPermanentQEffect(
                "Striking creatures before they've acted in round 1, and killing them within 1 round (end of your turn) of that, makes you and all allies within 30 feet quickened (Move or Strike).",
                qfFeat =>
                {
                    qfFeat.StateCheck = qfFirstRound =>
                    {
                        // Only on round 1
                        if (qfFirstRound.Owner.Battle.RoundNumber != 1)
                            return;
                        // For each creature who hasn't acted yet
                        foreach (Creature unactor in qfFirstRound.Owner.Battle.AllCreatures
                                     .Where(cr => !cr.Actions.ActedThisEncounter && cr.EnemyOf(qfFirstRound.Owner) && !cr.HasEffect(ModData.QEffectIds.FirstToFall, qf => qf.Source == qfFirstRound.Owner)))
                            // Apply a visible effect that makes it easier to play around
                            unactor.AddQEffect(new QEffect(
                                "First to Strike...",
                                $"When {qfFirstRound.Owner.ToColoredName()} Strikes you, you gain an additional effect.\n\n{{b}}First to Strike...{{/b}} ends at the start of your first turn.",
                                ExpirationCondition.Ephemeral,
                                qfFirstRound.Owner,
                                IllustrationName.BlackFist)
                            {
                                DoNotShowUpOverhead = true,
                                AfterYouAreDealtDamageOfKind = async (attacker, action, _, defender) =>
                                {
                                    if (action.HasTrait(Trait.Strike)
                                        && action.CheckResult > CheckResult.Failure)
                                        defender.AddQEffect(new QEffect(
                                            "... First to Fall",
                                            $"You are off-guard. When you die, {qfFirstRound.Owner.ToColoredName()} and their allies who are within 30 feet of you become quickened (only to move or Strike).",
                                            ExpirationCondition.Never,
                                            attacker,
                                            IllustrationName.BlackFist)
                                        {
                                            Id = ModData.QEffectIds.FirstToFall,
                                            IsFlatFootedTo = (_, _, _) => "First to Strike, First to Fall",
                                            WhenCreatureDiesAtStateCheckAsync = async qfThis2 =>
                                            {
                                                foreach (Creature ally in qfThis2.Owner.Battle.AllCreatures
                                                             .Where(cr => cr.FriendOf(qfFirstRound.Owner) && qfThis2.Owner.DistanceTo(cr) <= 6))
                                                {
                                                    ally.AddQEffect(QEffect.Quickened(quick =>
                                                        quick.Traits.ContainsOneOf([Trait.Move, Trait.Strike]))
                                                        .WithExpirationAtEndOfSourcesNextTurn(qfFirstRound.Owner, true)
                                                        .WithDescription("You have an extra action each turn. You can only use it to move or Strike.")
                                                        .With(qf => qf.Key = "FirstToFallQuickened"));
                                                }
                                            }
                                        });
                                }
                            });
                    };
                });
        
        // Impaling Bone
        // PETR: Level 17 feat.
        // Impaling Bone is now a baseline spell.
        // Have to figure out how to make it not cold iron (easy),
        // and how to make it immobilize incorporeals (not easy).
        
        // Legendary Laugh
        yield return new TrueFeat(
                ModData.FeatNames.LegendaryLaugh,
                17,
                "Your laugher echoes in the minds of your enemies.",
                "You can Demoralize creatures up to 60 feet away. Whenever you successfully Demoralize a creature, it takes 3d8 mental damage (or 6d8 on a critical success).",
                [ModData.Traits.Kholo])
            .WithPrerequisite(ModData.FeatNames.LaughingKholo, "Laughing Kholo")
            .WithPermanentQEffect(
                "Increase Demoralize's range to 60 feet. Demoralize deals 3d8 mental damage (6d8 on a critical success).",
                qfFeat =>
                {
                    qfFeat.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId is not ActionId.Demoralize)
                            return;
                        if (action.Target is not CreatureTarget crTar)
                            return;
                        if (crTar.CreatureTargetingRequirements.FirstOrDefault(req =>
                                req is MaximumRangeCreatureTargetingRequirement) is not MaximumRangeCreatureTargetingRequirement range)
                            return;
                        range.Range = Math.Max(range.Range, 12);
                        action.Description = action.Description
                            .Replace("30 feet", "60 feet".WithColor("Blue"))
                            .Replace("1.", "1, {Blue}and takes 3d8 mental damage{/Blue}.")
                            .Replace("2.", "2, {Blue}and takes 6d8 mental damage{/Blue}.");
                    };
                    qfFeat.AfterYouTakeActionAgainstTarget = async (qfThis, action, target, result) =>
                    {
                        if (action.ActionId is not ActionId.Demoralize
                            || result < CheckResult.Success)
                            return;

                        int numDice = result == CheckResult.CriticalSuccess ? 6 : 3;
                        await CommonSpellEffects.DealDirectDamage(action, DiceFormula.FromText(numDice + "d6", "Legendary laugh" + (result == CheckResult.CriticalSuccess ? " (Critical success)" : null)), target, result, DamageKind.Mental);
                    };
                });

        #endregion
    }
    
    /// <summary>Gets your current possibilities and looks for any action with the "RecallWeaknessActionID" action ID and offers it as an option (if multiple are present).</summary>
    /// <param name="self">The Creature recalling weakness.</param>
    /// <returns>(bool) Whether any options were offered and taken (does not have to succeed the action).</returns>
    public static async Task<bool> OfferToRecallWeakness(Creature self)
    {
        if (!ModManager.TryParse("RecallWeaknessActionID", out ActionId recall))
            return false;
        
        // Find Recall Weakness
        CombatAction? recallWeakness = (self.Possibilities
                .Filter(ap =>
                {
                    if (ap.CombatAction.ActionId != recall)
                        return false;
                    ap.CombatAction.ActionCost = 0;
                    ap.RecalculateUsability();
                    return true;
                })
                .CreateActions(true)
                .FirstOrDefault()
            as CombatAction)
            ?.WithExtraTrait(Trait.DoNotShowOverheadOfActionName);

        if (recallWeakness is null)
            return false;

        // Create options
        List<Option> options = [];
        GameLoop.AddDirectUsageOnCreatureOptions(recallWeakness, options, true);

        if (options.Count == 0)
            return false;
        
        // Pick a target
        options.Add(new CancelOption(true));
        Option chosenOption = (await self.Battle.SendRequest(new AdvancedRequest(
            self,
            "Choose who to Recall Weakness against.",
            options)
        {
            TopBarIcon = IllustrationName.NarratorBook,
            TopBarText = "Choose who to Recall Weakness against, or right-click to cancel.",
        })).ChosenOption;

        if (chosenOption is CancelOption)
            return false;

        await chosenOption.Action();
        return true;
    }
    
    /// <summary>
    /// Creates an innate QEffect for imprecise scent, including a Sewers encounter easter egg.
    /// </summary>
    /// <param name="featName">The name of the ability on your stat block. (Has fallback null name)</param>
    /// <param name="featDescription">The short description of the ability on your stat block. (Has fallback null description)</param>
    /// <param name="range">The range of the imprecise scent in squares.</param>
    /// <param name="isFamiliar">Whether the ability comes from you or from your familiar (only affects the Sewers encounter easter egg dialog, has no mechanical impact).</param>
    /// <returns></returns>
    public static QEffect ImpreciseScent(string? featName, string? featDescription, int range, bool isFamiliar = false)
    {
        featName ??= "Imprecise Scent";
        featDescription ??= $"You detect creatures within {range * 5} feet using your imprecise scent.";
        return new QEffect(featName, featDescription)
        {
            Tag = range,
            StateCheck = qfThis =>
            {
                if (qfThis.Owner.HasEffect(QEffectId.Unconscious))
                    return;
                int innerRange = (int)qfThis.Tag!;
                qfThis.Owner.Battle.AllCreatures
                    .Where(cr =>
                        cr.EnemyOf(qfThis.Owner) && cr.DistanceTo(qfThis.Owner) <= innerRange)
                    .ForEach(cr => cr.DetectionStatus.Undetected = false);
            },
            StartOfCombat = async qfThis =>
            {
                if (!qfThis.Owner.Battle.Encounter.Name?.ToLower().Contains("sewers") ?? true)
                {
                    qfThis.StartOfCombat = null;
                    return;
                }

                int innerRange = (int)qfThis.Tag!;
                int reducedRange = innerRange / 2;
                qfThis.Description = qfThis.Description!.Replace(
                    (innerRange*5).ToString(),
                    "{Red}" + (reducedRange * 5) + "{/Red}"
                );
                qfThis.Tag = reducedRange;

                qfThis.Owner.AddQEffect(new QEffect()
                {
                    StartOfYourPrimaryTurn = async (qfThis2, self) =>
                    {
                        qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                        Sfxs.Play(SfxName.Unallowed);
                        await self.Battle.Cinematics.ShowQuickBubble(
                            self,
                            "{b}" + qfThis.Name + "{/b}\nThis foul smell is messing with my" + (isFamiliar ? " familiar's" : null) + " nose. I can only detect creatures using " + (isFamiliar ? "their" : "my") + " imprecise scent from {Red}" + (reducedRange * 5) + " feet away{/Red}.",
                            5000);
                    },
                });
            },
        };
    }
}