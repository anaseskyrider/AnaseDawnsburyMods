using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
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
                qfFeat.AddGrantingOfTechnical(
                    cr =>
                        cr.FriendOfAndNotSelf(qfFeat.Owner) && cr.IsAdjacentTo(qfFeat.Owner),
                    qfTech =>
                    {
                        qfTech.Id = QEffectId.DevotedGuardian; // The potentially-defended creature how has this effect ID, instead of the feat owner.
                        qfTech.YouAreDealtDamage = async (qfThis, attacker, damageStuff, defender) =>
                            await CommonShieldRules.OfferAndReactWithShieldBlock(attacker, defender, damageStuff, qfFeat.Owner);
                    });
            });
        
        // Devoted Guardian
        // The feat now independently inserts its own action, and (homebrew) works with a Fortress Shield too. Description is also much more dynamic.
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
                        .WithName("Devoted Guardian ("+shield.Name.ToLower().Capitalize()+")")
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
                        .WithActionId(ActionId.None) // So that you can't use this when offered to raise a shield
                        .WithEffectOnEachTarget(async (_, caster, target, _) =>
                        {
                            target.AddQEffect(new QEffect(
                                "Devoted Guardian",
                                $"You have a +{theirBonus} circumstance bonus to AC as long as you're adjacent to {{Blue}}{caster}{{/Blue}}.",
                                ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                caster,
                                shield.Illustration)
                            {
                                CountsAsABuff = true,
                                BonusToDefenses = (_,_,defense) =>
                                    defense != Defense.AC
                                        ? null
                                        : new Bonus(theirBonus, BonusType.Circumstance, "Devoted Guardian"),
                                StateCheck = qfSelf =>
                                {
                                    if (caster.IsAdjacentTo(qfSelf.Owner))
                                        return;
                                    qfSelf.ExpiresAt = ExpirationCondition.Immediately;
                                }
                            });
                        });
                    raiseGuardian.Target = Target.AdjacentFriend();

                    return new ActionPossibility(raiseGuardian);
                };
            });
        
        // Reflexive Shield (More Dedications, Bastion Dedication, modded)
        Feat? reflexiveShield = AllFeats.All.FirstOrDefault(feat => feat.Name.Contains("Reflexive Shield"));
        if (reflexiveShield is not null)
        {
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
                            || !defender.HasEffect(QEffectId.RaisingAShield))
                            return null;

                        // Get the best currently raised shield
                        List<Item> shields = CommonShieldRules.GetRaisedShields(defender);
                        Item? bestShield = shields.MaxBy(CommonShieldRules.GetAC);
                        if (bestShield is null)
                            return null;

                        bool takingCover = defender.HasEffect(QEffectId.TakingCover);

                        // Use a higher bonus for the nearly-impossible circumstance you have a better AC from one shield but also have a lower-AC cover shield raised
                        int acBonus =
                            shields.Any(shield => shield.HasTrait(ModData.Traits.CoverShield))
                            && takingCover
                            ? 4
                            : CommonShieldRules.GetAC(bestShield) ?? 0;

                        return new Bonus(
                            acBonus,
                            BonusType.Circumstance,
                            "raised shield" + (takingCover ? " in cover" : null));
                    };

                    // Shield Block a Reflex save
                    qfFeat.YouAreDealtDamage = async (qfThis, attacker, dStuff, defender) =>
                    {
                        if (dStuff.Power?.SavingThrow?.Defense is not Defense.Reflex
                            && dStuff.Power?.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense is not Defense.Reflex)
                            return null;

                        return await CommonShieldRules.OfferAndReactWithShieldBlock(attacker, defender, dStuff, defender);
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