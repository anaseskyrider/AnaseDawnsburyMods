using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Monsters.L5;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreShields;

public static class OldShields
{
    public static void ModifyOldFeats()
    {
        // Shield Block
        // The Shield Block feat now tracks whenever you raise a shield to therefore add the capacity to shield block as a separate QEffect.
        // The shield block behavior itself now handles a lot of internal behavior through harmony.
        Feat shieldBlock = AllFeats.GetFeatByFeatName(FeatName.ShieldBlock);
        shieldBlock.OnCreature = null;
        shieldBlock.WithPermanentQEffect(
            "",
            qfFeat =>
            {
                qfFeat.AfterYouAcquireEffect = async (qfThis, qfAcquired) =>
                {
                    if (qfAcquired.Id is not QEffectId.RaisingAShield)
                        return;
                    qfThis.Owner.AddQEffect(QEffect.ShieldBlock());
                };
            });

        // Quick Shield Block
        // The feat now uses the new ExtraReaction QEffect.
        Feat quickShieldBlock = AllFeats.GetFeatByFeatName(FeatName.QuickShieldBlock);
        quickShieldBlock.OnCreature = null;
        quickShieldBlock.WithPermanentQEffect(
            "The first time you use Shield Block each round is a {icon:FreeAction} free action rather than a reaction.",
            qfFeat =>
            {
                qfFeat.StartOfCombat = async qfThis =>
                {
                    qfThis.Owner.AddQEffect(ReactionsExpanded.ExtraReaction(
                        "Quick Shield Block",
                        "The first time you use Shield Block each round is a {icon:FreeAction} free action rather than a reaction.",
                        null,
                        action => action.ActionId == ModData.ActionIds.ShieldBlock));
                };
            });

        // Aggressive Block
        // The feat now prompts the user to respond whenever the Shield Block reaction is taken. This also FullCasts an action.
        Feat aggressiveBlock = AllFeats.GetFeatByFeatName(FeatName.AggressiveBlock);
        aggressiveBlock.RulesText = aggressiveBlock.RulesText.Replace("a melee attack of ", "");
        aggressiveBlock.OnCreature = null;
        aggressiveBlock.WithPermanentQEffect(
            "When you use Shield Block against an adjacent enemy, you can also push the attacker 5 feet. If you can't, the attacker is instead flat-footed until the start of your next turn.",
            qfFeat =>
            {
                qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                {
                    if (action.ActionId != ModData.ActionIds.ShieldBlock)
                        return;
                    Creature self = qfThis.Owner;
                    Creature? attacker = action.ChosenTargets.ChosenCreature;
                    if (attacker == null || !self.IsAdjacentTo(attacker) || !await self.Battle.AskForConfirmation(
                            self,
                            action.Item?.Illustration ?? IllustrationName.SteelShield,
                            $"{{b}}Aggressive Block{{/b}} {RulesBlock.GetIconTextFromNumberOfActions(0)}\nYou just used Shield Block. Push {{Blue}}{attacker}{{/Blue}} 5 feet away from you? {{i}}(They could become flat-footed if they cannot be moved.){{/i}}", "Push"))
                        return;
                    
                    CombatAction aggressiveAction = new CombatAction(
                            self,
                            new SideBySideIllustration(action.Illustration, IllustrationName.Shove),
                            "Aggressive Block",
                            [Trait.Fighter],
                            "{i}You push back as you block the attack, knocking your foe away or off balance.{/i}\n\n{b}Trigger{/b} You use the Shield Block reaction, and the opponent that triggered Shield Block is adjacent to you.\n\nThe triggering creature is automatically pushed 5 feet. If it can't be pushed away, it's instead flat-footed until the start of your next turn.",
                            Target.AdjacentCreature()
                                .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement()))
                        .WithActionCost(0)
                        .WithEffectOnEachTarget(async (action2, caster, target, result) =>
                        {
                            Tile previousPosition = target.Occupies;
                            Sfxs.Play(SfxName.Shove);
                            await caster.PushCreature(target, 1);
                            if (previousPosition == target.Occupies)
                                target.AddQEffect(QEffect.FlatFooted("Aggressive Block")
                                    .WithExpirationAtStartOfSourcesTurn(caster, 1));
                        });
                    
                    await self.Battle.GameLoop.FullCast(aggressiveAction, ChosenTargets.CreateSingleTarget(attacker));
                };
            });

        // Shield Warden
        // The feat now independently handles defending allies.
        Feat shieldWarden = AllFeats.GetFeatByFeatName(FeatName.ShieldWarden);
        shieldWarden.OnCreature = null;
        shieldWarden.WithPermanentQEffect(
            "You can use Shield Block to prevent damage to an adjacent ally.",
            qfFeat =>
            {
                qfFeat.AddGrantingOfTechnical(
                    cr =>
                        cr.FriendOfAndNotSelf(qfFeat.Owner) && cr.IsAdjacentTo(qfFeat.Owner),
                    qfTech =>
                    {
                        qfTech.Id = QEffectId.DevotedGuardian; // The potentially-defended creature how has this effect ID, instead of the feat owner.
                        qfTech.YouAreDealtDamage = async (qfThis, attacker, damageStuff, defender) =>
                            await CommonShieldRules.BlockWithShield(attacker, defender, damageStuff, qfFeat.Owner);
                    });
            });
        
        // Devoted Guardian
        // The feat now independently inserts its own action, and (homebrew) works with a Fortress Shield too.
        Feat devotedGuardian = AllFeats.GetFeatByFeatName(Champion.DevotedGuardianFeatName);
        devotedGuardian.RulesText = devotedGuardian.RulesText.Replace("was a tower shield", "has the Cover Shield trait");
        devotedGuardian.Traits.Add(ModData.Traits.ShieldActionFeat);
        devotedGuardian.OnCreature = null;
        devotedGuardian.WithPermanentQEffect(
            "You can raise your shield so that it grants an AC bonus also to an adjacent ally.",
            qfFeat =>
            {
                qfFeat.Id = QEffectId.DevotedGuardian;
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    // Uses & instead of && so that "isRaised" is initialized.
                    if (section.Name != "Raise shield" &
                        !(qfThis.Owner.HasEffect(QEffectId.RaisingAShield) is var isRaised
                          && isRaised
                          && section.PossibilitySectionId == PossibilitySectionId.ItemActions
                          && qfThis.Owner.Actions.ActionHistoryThisTurn.LastOrDefault() is { ActionId: ActionId.RaiseShield } raise
                          && !raise.Name.Contains("Devoted Guardian")))
                        return null;
                    
                    if (CommonShieldRules.GetWieldedShields(qfThis.Owner) is not {} shields
                        || shields.Count == 0
                        || shields.MaxBy(CommonShieldRules.GetAC) is not { } shield
                        || CommonShieldRules.GetAC(shield) is not { } shieldAC)
                        return null;

                    int theirBonus = shield.HasTrait(ModData.Traits.CoverShield) ? 2 : 1;

                    CombatAction raiseGuardian = CommonShieldRules.CreateRaiseShieldCore(
                            qfThis.Owner,
                            shield,
                            qfThis.Owner.HasFeat(FeatName.ShieldBlock))
                        .WithActionCost(isRaised ? 1 : 2)
                        .WithEffectOnEachTarget(async (action, caster, target, result) =>
                        {
                            target.AddQEffect(new QEffect(
                                "Devoted Guardian",
                                $"You have a +{theirBonus} circumstance bonus to AC as long as you're adjacent to {{Blue}}{caster}{{/Blue}}.",
                                ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                caster,
                                shield.Illustration)
                            {
                                BonusToDefenses = (_,_,defense) =>
                                    defense != Defense.AC ? null : new Bonus(theirBonus, BonusType.Circumstance, "Devoted Guardian"),
                                StateCheck = qfSelf =>
                                {
                                    if (caster.IsAdjacentTo(qfSelf.Owner))
                                        return;
                                    qfSelf.ExpiresAt = ExpirationCondition.Immediately;
                                }
                            });
                        });
                    raiseGuardian.Name += " (Devoted Guardian)";
                    raiseGuardian.Description = raiseGuardian.Description
                        .Replace("Until the start", "Choose an adjacent ally. Until the start")
                        .Replace(
                            $"you gain a {{Blue}}+{shieldAC}{{/Blue}} circumstance bonus to AC",
                            shieldAC != theirBonus
                                ? $"you gain a {{Blue}}+{shieldAC}{{/Blue}} circumstance bonus to AC and the ally gains a {{Blue}}+{theirBonus}{{/Blue}} circumstance bonus to AC"
                                : $"both of you gain a {{Blue}}+{shieldAC}{{/Blue}} circumstance bonus to AC")
                        .Insert(raiseGuardian.Description.Length, " Your ally loses the bonus if they're no longer adjacent to you.") // it's -> they're :thumbsup:
                        + (isRaised ? "\n\n{icon:Action} {Green}(Last action was to Raise a Shield){/Green}" : null);
                    raiseGuardian.Target = Target.AdjacentFriend();

                    return new ActionPossibility(raiseGuardian);
                };
            });
        
        // Disarming Block (More Dedications, Bastion Dedication, modded)
        Feat? disarmingBlock = AllFeats.All.FirstOrDefault(feat => feat.Name.Contains("Disarming Block"));
        if (disarmingBlock is not null)
        {
            disarmingBlock.OnCreature = null;
            disarmingBlock.WithPermanentQEffect(
                "You attempt to Disarm melee attackers when you Shield Block.",
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.ActionId != ModData.ActionIds.ShieldBlock)
                            return;
                        
                        Creature self = qfThis.Owner;
                        Creature? attacker = action.ChosenTargets.ChosenCreature;
                        CombatAction? blockedAction = action.Tag as CombatAction;
                        
                        if (attacker == null
                            || !self.IsAdjacentTo(attacker) // Attacker has to be adjacent to disarm it
                            || blockedAction == null // Has to have SOME kind of damage info
                            || !blockedAction.HasTrait(Trait.Melee) // Has to be a melee strike
                            || !blockedAction.HasTrait(Trait.Strike)
                            || attacker.HeldItems.Count == 0 // Has to be with a disarmable item
                            || !await self.Battle.AskForConfirmation(
                                self,
                                action.Item?.Illustration ?? IllustrationName.SteelShield,
                                $"{{b}}Disarming Block{{/b}} {RulesBlock.GetIconTextFromNumberOfActions(0)}\nYou just used Shield Block. Attempt to Disarm {{Blue}}{attacker}{{/Blue}}?", "Disarm"))
                            return;
                        
                        CombatAction disarmingAction = new CombatAction(
                                self,
                                new SideBySideIllustration(action.Illustration, IllustrationName.Disarm),
                                "Disarming Block",
                                [Trait.Archetype],
                                "{i}With deft and practiced movement, you block at an angle to potentially dislodge the weapon.{/i}\n\n{b}Trigger{/b} You Shield Block a melee Strike made with a held weapon.\n\nYou attempt to Disarm the creature whose attack you blocked of the weapon they attacked you with. You can do so even if you don't have a hand free.",
                                Target.AdjacentCreature()
                                    .WithAdditionalConditionOnTargetCreature(new EnemyCreatureTargetingRequirement()))
                            .WithActionCost(0)
                            .WithEffectOnEachTarget(async (action2, caster, target, result) =>
                            {
                                int oldMAP = caster.Actions.AttackedThisManyTimesThisTurn;
                                caster.Actions.AttackedThisManyTimesThisTurn = 0;
                                if (caster.HeldItems.FirstOrDefault(item => item.HasTrait(Trait.Disarm)) != null)
                                {
                                    Item disarmWeapon =
                                        caster.HeldItems.FirstOrDefault(item => item.HasTrait(Trait.Disarm))!;
                                    CombatAction specialDisarm = CombatManeuverPossibilities
                                        .CreateDisarmAction(caster, disarmWeapon)
                                        .WithActionCost(0);
                                    await caster.Battle.GameLoop.FullCast(specialDisarm,
                                        ChosenTargets.CreateSingleTarget(target));
                                }
                                else
                                {
                                    CheckResult disarmResult = CommonSpellEffects.RollCheck(
                                        "Disarming Block",
                                        new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Athletics),
                                            TaggedChecks.DefenseDC(Defense.Reflex)),
                                        caster,
                                        target);
                                    Sfxs.Play(self.HasTrait(Trait.Female) ? SfxName.TripFemale : SfxName.TripMale);
                                    await CommonAbilityEffects.Disarm(
                                        action2, // Don't assign an item to the action, it'll try to drop it if you crit fail.
                                        caster,
                                        target,
                                        disarmResult);
                                }
                                caster.Actions.AttackedThisManyTimesThisTurn = oldMAP;
                            });
                        
                        await self.Battle.GameLoop.FullCast(disarmingAction, ChosenTargets.CreateSingleTarget(attacker));
                    };
                });
        }
        
        // Reflexive Shield (More Dedications, Bastion Dedication, modded)
        Feat? reflexiveShield = AllFeats.All.FirstOrDefault(feat => feat.Name.Contains("Reflexive Shield"));
        if (reflexiveShield is not null)
        {
            reflexiveShield.OnCreature = null;
            reflexiveShield.WithPermanentQEffect(
                "Raise a Shield benefits your Reflex saves. If you have Shield Block, you can block any damage from a Reflex save.",
                qfFeat =>
                {
                    // Apply shield AC to Reflex saves.
                    qfFeat.BonusToDefenses = (qfThis, action, defense) =>
                    {
                        Creature defender = qfThis.Owner;
                        
                        if (!defender.HasEffect(QEffectId.ShieldBlock))
                            return null;
                        if (defense != Defense.Reflex)
                            return null;

                        List<Item> shields = CommonShieldRules.GetWieldedShields(defender);
                        Item? bestShield = shields.MaxBy(CommonShieldRules.GetAC);
                        if (bestShield is null)
                            return null;

                        int acBonus =
                            bestShield.HasTrait(ModData.Traits.CoverShield)
                            && defender.HasEffect(QEffectId.TakingCover)
                            ? 4
                            : CommonShieldRules.GetAC(bestShield) ?? 0;

                        return new Bonus(
                            acBonus,
                            BonusType.Circumstance,
                            "raised shield" + (acBonus == 4 ? " in cover" : null));
                    };

                    // Shield Block a Reflex save
                    qfFeat.YouAreDealtDamage = async (qfThis, attacker, dealt, defender) =>
                    {
                        if (dealt.Power is not { } action
                            || (action.SavingThrow is not { Defense: Defense.Reflex }
                                && (action.ActiveRollSpecification is not { } rollSpec
                                    || rollSpec.TaggedDetermineDC.InvolvedDefense != Defense.Reflex)))
                            return null;

                        return await CommonShieldRules.BlockWithShield(attacker, defender, dealt, defender);
                    };
                });
        }
    }
    
    public static void ModifyOldShields()
    {
        // Modify items when created
        ModManager.RegisterActionOnEachItem(item =>
        {
            Item modified = ShieldAlterations(item);
            Item replaced = ReplaceSturdyShield(modified);
            return replaced;
        });
    }

    public static Item ShieldAlterations(Item item)
    {
        // Shields only
        if (!item.HasTrait(Trait.Shield))
            return item;

        if (item.HasTrait(Trait.WoodenShield) || item.HasTrait(Trait.SteelShield) && !item.HasTrait(ModData.Traits.MediumShield))
            item.Traits.Add(ModData.Traits.MediumShield);
        if (item.HasTrait(Trait.TowerShield) && !item.HasTrait(ModData.Traits.MediumShield))
        {
            item.Traits.Add(ModData.Traits.MediumShield);
            item.Traits.Add(ModData.Traits.CoverShield);
        }

        return item;
    }

    public static Item ReplaceSturdyShield(Item item)
    {
        switch (item.ItemName)
        {
            case ItemName.SturdyShield8:
                return ShieldModifications.CreatePlatedShield(ItemName.SteelShield, ModData.ItemNames.SturdyShieldPlatingMinor);
            case ItemName.SturdyShield10:
                return ShieldModifications.CreatePlatedShield(ItemName.SteelShield, ModData.ItemNames.SturdyShieldPlatingLesser);
            case ItemName.SturdyShield13:
                return ShieldModifications.CreatePlatedShield(ItemName.SteelShield, ModData.ItemNames.SturdyShieldPlatingModerate);
            default:
                return item;
        }
    }
}