using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreShields;

public static class OldShields
{
    public static void ModifyOldFeats()
    {
        // Shield Warden
        // The feat now independently handles defending allies.
        Feat shieldWarden = AllFeats.GetFeatByFeatName(FeatName.ShieldWarden);
        shieldWarden.OnCreature = null;
        shieldWarden.WithPermanentQEffect(
            "You can use Shield Block to prevent damage to an adjacent ally.",
            qfFeat =>
            {
                qfFeat.Id = QEffectId.ShieldWarden;
                qfFeat.AddGrantingOfTechnical(
                    cr =>
                        cr.FriendOfAndNotSelf(qfFeat.Owner) && cr.IsAdjacentTo(qfFeat.Owner),
                    qfTech =>
                    {
                        qfTech.YouAreDealtDamage = async (qfThis, attacker, damageStuff, defender) =>
                        {
                            // Uses normal triggers for Shield Block
                            if ((!damageStuff.Kind.IsPhysical()
                                 || damageStuff.Power == null
                                 || !damageStuff.Power.HasTrait(Trait.Attack))
                                && !Magus.DoesSparklingTargeShieldBlockApply(damageStuff, qfFeat.Owner))
                                return null;

                            // TODO: Enhance stat block info from other SB triggers.
                            return await CommonShieldRules.OfferAndMakeShieldBlock(attacker, defender, damageStuff, qfFeat.Owner);
                        };
                    });
            });
        
        // Devoted Guardian
        // The feat now independently inserts its own action, and (homebrew) works with a Fortress Shield too. Description is also much more dynamic.
        Feat devotedGuardian = AllFeats.GetFeatByFeatName(Champion.DevotedGuardianFeatName);
        devotedGuardian.RulesText = devotedGuardian.RulesText.Replace("was a tower shield", "has the Cover Shield trait");
        devotedGuardian.Traits.Insert(0, ModData.Traits.MoreShields);
        devotedGuardian.Traits.Add(ModData.Traits.ShieldActionFeat);
        devotedGuardian.OnCreature = null;
        devotedGuardian.WithPermanentQEffect(
            "You can raise your shield so that it grants an AC bonus also to an adjacent ally.",
            qfFeat =>
            {
                qfFeat.Id = QEffectId.DevotedGuardian;
                qfFeat.ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    Item? shield = null;
                    bool isRaised = false;
                    // If this section is found, then this shield isn't raised
                    if (section.Name == "Raise shield")
                    {
                        var options = section.Filter(actPoss =>
                            actPoss.CombatAction.ActionId is ActionId.RaiseShield);
                        if ((options?.Possibilities.FirstOrDefault() as ActionPossibility) is { } raise)
                            shield = raise.CombatAction.Item;
                    }
                    // Provide the action directly to the item section, outside the submenu.
                    else if (section.PossibilitySectionId == PossibilitySectionId.ItemActions)
                    {
                        // Create this action only for the last shield that was raised with my last action
                        if (qfThis.Owner.HasEffect(QEffectId.RaisingAShield)
                            && qfThis.Owner.Actions.ActionHistoryThisTurn.LastOrDefault() is
                                { ActionId: ActionId.RaiseShield } raise
                            && !raise.Name.ToLower().Contains("devoted guardian"))
                        {
                            shield = raise.Item;
                            isRaised = true;
                        }
                    }

                    if (shield is null
                        || CommonShieldRules.GetAC(shield) is not { } acBonus )
                        return null;

                    int theirBonus = shield.HasTrait(ModData.Traits.CoverShield) ? 2 : 1;
                    bool hasShieldBlock = qfThis.Owner.HasEffect(QEffectId.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);

                    CombatAction raiseGuardian = CommonShieldRules
                        .CreateRaiseShieldCore(qfThis.Owner, shield, hasShieldBlock)
                        .With(ca =>
                        {
                            // Will apply Fighter.RaiseShield as Devoted Guardian on ally.
                            ca.Target = Target.AdjacentFriend();
                        })
                        .WithName("Devoted Guardian (" + shield.Name.ToLower().Capitalize() + ")")
                        .WithDescription(
                            "You adopt a wide stance, ready to defend both yourself and your chosen ward.",
                            "Choose an adjacent ally. Until the start of your next turn, "
                            + (isRaised
                                ? $"your ally gains a {{Blue}}+{theirBonus}{{/Blue}} circumstance bonus to AC"
                                : acBonus != theirBonus
                                    ? $"you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC and the ally gains a {{Blue}}+{theirBonus}{{/Blue}} circumstance bonus to AC"
                                    : $"both of you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC")
                            + (hasShieldBlock && !isRaised
                                ? ", and you can use the Shield Block {icon:Reaction} reaction"
                                : null)
                            + ".\n\nYour ally loses the bonus if they're no longer adjacent to you."
                            + (isRaised
                                ? "\n\n{icon:Action} {Green}(Last action was to Raise this Shield){/Green}"
                                : null))
                        .WithActionCost(isRaised ? 1 : 2)
                        .WithActionId(ActionId.None); // So that you can't use this when offered to raise a shield

                    return new ActionPossibility(raiseGuardian);
                };
            });
        
        // Shield Ally
        // The feat now adds a bonus to hardness that the game can broadly detect.
        Feat shieldAlly = AllFeats.GetFeatByFeatName(Champion.ShieldAllyFeatName);
        shieldAlly.Traits.Insert(0, ModData.Traits.MoreShields);
        shieldAlly.OnCreature = null;
        shieldAlly
            .WithPermanentQEffect(
                "When you prevent damage with Shield Block, you prevent 2 more damage.",
                qfFeat =>
                {
                    qfFeat.Id = QEffectId.ShieldAlly;
                })
            .WithOnCreature(self =>
            {
                self.AddQEffect(CommonShieldRules.BonusToShieldHardness(2, "Shield ally"));
            });
        
        // Sparkling Targe
        // The subclass now adds a bonus to hardness that the game can broadly detect.
        // The bonus also increases to 3 at level 15.
        Feat sparklingTarge = AllFeats.GetFeatByFeatName(FeatName.SparklingTarge);
        sparklingTarge.Traits.Insert(0, ModData.Traits.MoreShields);
        sparklingTarge.OnCreature += (_, self) =>
        {
            self.AddQEffect(CommonShieldRules.BonusToShieldHardness((_, stuff, _, blocker) =>
            {
                if (!Magus.DoesSparklingTargeShieldBlockApply(stuff, blocker))
                    return null;
                return new Bonus(
                    blocker.Level >= 15 ? 3 : blocker.Level >= 7 ? 2 : 1,
                    BonusType.Untyped,
                    "Sparkling targe");
            }));
        };
        
        // Reflexive Shield (More Dedications, Bastion Dedication, modded)
        Feat? reflexiveShield = AllFeats.All.FirstOrDefault(feat => feat.Name.Contains("Reflexive Shield"));
        if (reflexiveShield is not null)
        {
            reflexiveShield.Traits.Insert(0, ModData.Traits.MoreShields);
            reflexiveShield.OnCreature = null;
            reflexiveShield.WithPermanentQEffect(
                "Raise a Shield benefits your Reflex saves. If you have Shield Block, you can block any damage from a Reflex save.",
                qfFeat =>
                {
                    // Apply best shield AC to Reflex saves.
                    qfFeat.BonusToDefenses = (qfThis, _, def) =>
                    {
                        Creature defender = qfThis.Owner;
                        
                        if (def != Defense.Reflex
                            || CommonShieldRules.GetRaisedShields(defender) is not { Count: > 0 } shields
                            || shields.MaxBy(CommonShieldRules.GetAC) is not {} bestShield)
                            return null;

                        bool takingCover = defender.HasEffect(QEffectId.TakingCover)
                            && shields.Any(shield => shield.HasTrait(ModData.Traits.CoverShield));

                        // Use a higher bonus for the nearly-impossible circumstance you have a better AC from one shield but also have a lower-AC cover shield raised
                        int acBonus = takingCover
                            ? 4
                            : CommonShieldRules.GetAC(bestShield) ?? 0;

                        return new Bonus(acBonus, BonusType.Circumstance, "raised shield" + (takingCover ? " in cover" : null));
                    };

                    // Shield Block a Reflex save
                    qfFeat.YouAreDealtDamage = async (qfThis, attacker, dStuff, defender) =>
                        CommonShieldRules.DoesReflexiveShieldApply(dStuff.Power)
                            ? await CommonShieldRules.OfferAndMakeShieldBlock(attacker, defender, dStuff, defender)
                            : null;
                    
                    // Delayed in case of feat load orders
                    qfFeat.StartOfCombatAfterInitiativeOrderIsSetUp = async qfThis =>
                    {
                        // Fire once
                        qfThis.StartOfCombatAfterInitiativeOrderIsSetUp = null;
                        
                        // Shield Warden compatibility
                        if (qfFeat.Owner.HasEffect(QEffectId.ShieldWarden))
                        {
                            qfFeat.AddGrantingOfTechnical(
                                ally =>
                                    ally.FriendOfAndNotSelf(qfFeat.Owner) && ally.IsAdjacentTo(qfFeat.Owner),
                                qfAlly =>
                                    qfAlly.YouAreDealtDamage = async (_, attacker, dStuff, defender) =>
                                        CommonShieldRules.DoesReflexiveShieldApply(dStuff.Power)
                                            ? await CommonShieldRules.OfferAndMakeShieldBlock(attacker, defender,
                                                dStuff, qfFeat.Owner)
                                            : null);
                        }
                    };
                });
        }


                    {
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