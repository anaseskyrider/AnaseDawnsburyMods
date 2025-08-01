using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreShields;

public static class NewShields
{
    public static List<ItemName> AllNewShields = [
        ModData.ItemNames.Buckler,
        ModData.ItemNames.CastersTarge,
        ModData.ItemNames.HeavyRondache,
        ModData.ItemNames.MeteorShield,
        ModData.ItemNames.FortressShield,
    ];
    
    public static void LoadShields()
    {
        // Automates the creation of attack options for the thrown 30 ft trait.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            cr.AddQEffect(new QEffect()
            {
                Name = "Thrown30ftAutomator",
                ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(ModData.Traits.Thrown30Feet))
                        return null;

                    return StrikeRules.CreateStrike(cr, item, RangeKind.Ranged, -1, true);
                },
            });
        });
        
        // Create new shields
        ModData.ItemNames.Buckler = ModManager.RegisterNewItemIntoTheShop(
            "Buckler",
            iName => new Item(
                    iName,
                    ModData.Illustrations.Buckler,
                    "Buckler",
                    0,
                    1,
                    [ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.LightShield, Trait.Worn, ModData.Traits.WornShield])
                .WithMainTrait(ModData.Traits.Buckler)
                .WithDescription("This very small shield is a favorite of duelists and quick, lightly armored warriors. It's typically made of steel and strapped to your forearm.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(3));
        ModData.ItemNames.FortressShield = ModManager.RegisterNewItemIntoTheShop(
            "FortressShield",
            iName => new Item(
                    iName,
                    IllustrationName.TowerShield,
                    "Fortress Shield",
                    1, 20,
                    [ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.HeavyShield, ModData.Traits.CoverShield, ModData.Traits.Hefty14])
                .WithMainTrait(ModData.Traits.FortressShield)
                .WithDescription("Also known as portable walls, these thick and heavy shields are slightly larger than tower shields. Like tower shields, they're typically made from wood reinforced with metal, but many are made from larger amounts of metal or even stone.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(6));
        ModData.ItemNames.MeteorShield = ModManager.RegisterNewItemIntoTheShop(
            "MeteorShield",
            iName => new Item(
                    iName,
                    ModData.Illustrations.MeteorShield,
                    "Meteor Shield",
                    0,
                    4,
                    [ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.Thrown30Feet, ModData.Traits.MediumShield])
                .WithMainTrait(ModData.Traits.MeteorShield)
                .WithDescription("Meteor shields are specifically designed with throwing in mind. A meteor shield is made from thin steel and has quick-release straps, allowing for easy, long-distance throws.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning)
                {
                    VfxStyle = new VfxStyle(1, ProjectileKind.Arrow, IllustrationName.WoodenShieldBoss)
                })
                .WithAdditionalWeaponProperties(prop =>
                {
                    prop.ForcedMelee = true;
                    prop.Throwable = true;
                    prop.WithRangeIncrement(30 / 5);
                })
                .WithShieldProperties(4));
        ModData.ItemNames.HeavyRondache = ModManager.RegisterNewItemIntoTheShop(
            "HeavyRondache",
            iName => new Item(
                    iName,
                    ModData.Illustrations.Buckler,
                    "Heavy Rondache",
                    1,
                    5,
                    [ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.LightShield, Trait.Worn, ModData.Traits.WornShield])
                .WithMainTrait(ModData.Traits.HeavyRondache)
                .WithDescription("Similar in size to a buckler, this shield is intended to absorb blows instead of deflecting attacks. It features multiple layers of metal and is reinforced with additional wood.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(5));
        // TODO: Caster's Targe
        /*ModData.ItemNames.CastersTarge = ModManager.RegisterNewItemIntoTheShop(
            "CastersTarge",
            iName => new Item(
                    iName,
                    ModData.Illustrations.CastersTarge,
                    "Caster's Targe",
                    0,
                    2,
                    [ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, Trait.NonMetallic, ModData.Traits.LightShield])
                .WithMainTrait(ModData.Traits.CastersTarge)
                .WithDescription("This small shield is made from wood. It features a special panel of parchment along the inside surface that allows it to store a single spell scroll. This scroll can be activated from the shield while it is being held.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(3)
                .WithStoresItem((bag, heldItem) =>
                    !heldItem.HasTrait(Trait.Scroll) || bag.StoredItems.Count >= 1 ? "This shield can only hold 1 spell scroll." : null));*/
    }

    /// <summary>Gets a list of shields being wielded or worn by a creature.</summary>
    public static List<Item> GetAvailableShields(Creature owner)
    {
        List<Item> heldShields = owner.HeldItems
            .Where(it => it.HasTrait(Trait.Shield))
            .ToList();
        return owner.HasFreeHand ||
               owner.HeldItems.Any(held => !held.HasTrait(Trait.Weapon) && !held.HasTrait(Trait.Grapplee))
            ? heldShields
                .Union(owner.CarriedItems
                    .Where(it => it.HasTrait(Trait.Shield) && it.IsWorn))
                .ToList()
            : heldShields;
    }

    /// <summary>Gets the circumstance bonus to AC of an item, if it's a shield.</summary>
    public static int? GetShieldAC(Item shield)
    {
        if (!shield.HasTrait(Trait.Shield)/* && !shield.HasTrait(Trait.AlwaysOfferShieldBlock)*/)
            return null;
        if (shield.HasTrait(ModData.Traits.HeavyShield))
            return 3;
        if (shield.HasTrait(ModData.Traits.MediumShield))
            return 2;
        if (shield.HasTrait(ModData.Traits.LightShield))
            return 1;
        return 2; // Fallback value.
    }

    /// <summary>
    /// New version of the local function contained in <see cref="Fighter.CreateRaiseShield"/>.
    /// </summary>
    /// <param name="self">The creature raising a shield.</param>
    /// <param name="shield">The shield being raised.</param>
    /// <param name="hasShieldBlock">You should pass Creature.HasFeat(FeatName.ShieldBlock) in most instances.</param>
    public static CombatAction CreateRaiseShieldCore(Creature self, Item shield, bool hasShieldBlock)
    {
        int acBonus = (int)GetShieldAC(shield)!; // Suppress. Only gets called on an item that is a shield.
        return new CombatAction(
                self,
                shield.Illustration,
                $"Raise {shield.BaseHumanName.ToLower()}",
                [Trait.Basic, Trait.DoNotShowOverheadOfActionName],
                $"{{i}}You position your shield to protect yourself.{{/i}}\n\nUntil the start of your next turn, you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC"
                + (hasShieldBlock
                    ? (" and you can use the Shield Block " +
                       RulesBlock.GetIconTextFromNumberOfActions(Constants.ACTION_COST_REACTION)
                       + " reaction")
                    : null)
                + ".",
                Target.Self((_,ai) => ai.GainBonusToAC(acBonus)))
            .WithActionCost(shield.HasTrait(ModData.Traits.Hefty14) && self.Abilities.Strength < 2 ? 2 : 1)
            .WithActionId(ActionId.RaiseShield)
            .WithSoundEffect(SfxName.RaiseShield)
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                QEffect qfRaisedShield = QEffect.RaisingAShield(hasShieldBlock);
                caster.AddQEffect(qfRaisedShield);
            });
    }

    /// <summary>
    /// Creates a shield block action with a cost of 0. Doesn't do much mechanically. This gets FullCast in order to trigger events that key off of taking the Shield Block reaction.
    /// </summary>
    /// <param name="owner">The creature doing the shield blocking.</param>
    /// <param name="shield">The shield being blocked with.</param>
    /// <param name="blockedAction">The action being defended against.</param>
    public static CombatAction CreateShieldBlock(Creature owner, Item shield, CombatAction? blockedAction)
    {
        return new CombatAction(
                owner,
                shield.Illustration,
                "Shield Block",
                [Trait.General, ModData.Traits.ReactiveAction, Trait.Basic, Trait.DoNotShowOverheadOfActionName, Trait.DoNotShowInCombatLog],
                $"{{i}}You snap your shield in place to ward off a blow.{{/i}}\n\n{{b}}Trigger{{/b}} While you have your shield raised, you would take damage from a physical attack.\n\nYour shield prevents you from taking up to {{Blue}}{shield.Hardness}{{/Blue}} damage. You take any remaining damage.",
                new CreatureTarget(
                    RangeKind.Ranged,
                    [ // No line of effect requirement
                        new MaximumRangeCreatureTargetingRequirement(99), // Usable across whole map
                    ],
                    (_,_,_) => int.MinValue))
            .WithItem(shield)
            .WithProjectileCone(VfxStyle.NoAnimation()) // WithItem adds an animation, this removes it.
            .WithTag(blockedAction)
            .WithActionId(ModData.ActionIds.ShieldBlock)
            //.WithSoundEffect(ModData.SfxNames.ShieldBlockWooodenImpact) // Plays too late.
            .WithActionCost(0);
    }

    /// <summary>
    /// Performs a Shield Block for a <see cref="QEffect.YouAreDealtDamage"/> event.
    /// </summary>
    /// <param name="qfDealtDamage">The creature who owns the QEffect, and is being dealt damage to.</param>
    /// <param name="attacker">The creature dealing the damage (from YouAreDealtDamage).</param>
    /// <param name="dStuff">The DamageStuff from YouAreDealtDamage.</param>
    /// <param name="shieldBlocker">The creature shield blocking the damage (can be the YouAreDealtDamage defender, or another creature).</param>
    /// <returns></returns>
    public static async Task<DamageModification?> BlockWithShield(QEffect qfDealtDamage, Creature attacker, DamageStuff dStuff, Creature shieldBlocker)
    {
        QEffect? shieldBlock = shieldBlocker.FindQEffect(QEffectId.ShieldBlock);
        if (shieldBlock == null)
            return null;
        return await shieldBlock.YouAreDealtDamage?.Invoke(qfDealtDamage, attacker, dStuff, shieldBlocker)! ?? null;
    }
}