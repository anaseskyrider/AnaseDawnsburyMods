using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Text;
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
        ModData.ItemNames.CastersTarge,
    ];
    
    /// <summary>Load new shields into Dawnsbury Days. Called by <see cref="ModLoader"/>.</summary>
    public static void LoadShields()
    {
        // Create new shields
        ModData.ItemNames.Buckler = ModManager.RegisterNewItemIntoTheShop(
            "Buckler",
            iName => new Item(
                    iName,
                    ModData.Illustrations.Buckler,
                    "buckler",
                    0, 1,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.LightShield, Trait.Worn, ModData.Traits.WornShield)
                .WithMainTrait(ModData.Traits.Buckler)
                .WithDescription("This very small shield is a favorite of duelists and quick, lightly armored warriors. It's typically made of steel and strapped to your forearm.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(3));
        ModData.ItemNames.FortressShield = ModManager.RegisterNewItemIntoTheShop(
            "FortressShield",
            iName => new Item(
                    iName,
                    ModData.Illustrations.FortressShield,
                    "fortress shield",
                    1, 20,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.HeavyShield, ModData.Traits.CoverShield, ModData.Traits.Hefty14)
                .WithMainTrait(ModData.Traits.FortressShield)
                .WithDescription("Also known as portable walls, these thick and heavy shields are slightly larger than tower shields. Like tower shields, they're typically made from wood reinforced with metal, but many are made from larger amounts of metal or even stone.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(6));
        ModData.ItemNames.MeteorShield = ModManager.RegisterNewItemIntoTheShop(
            "MeteorShield",
            iName => new Item(
                    iName,
                    ModData.Illustrations.MeteorShield,
                    "meteor shield",
                    0, 4,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, Trait.Thrown30Feet, ModData.Traits.MediumShield)
                .WithMainTrait(ModData.Traits.MeteorShield)
                .WithDescription("Meteor shields are specifically designed with throwing in mind. A meteor shield is made from thin steel and has quick-release straps, allowing for easy, long-distance throws.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning)
                {
                    VfxStyle = new VfxStyle(1, ProjectileKind.Arrow, IllustrationName.WoodenShieldBoss)
                })
                .WithShieldProperties(4));
        ModData.ItemNames.HeavyRondache = ModManager.RegisterNewItemIntoTheShop(
            "HeavyRondache",
            iName => new Item(
                    iName,
                    ModData.Illustrations.HeavyRondache,
                    "heavy rondache",
                    1, 5,
                    ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, ModData.Traits.LightShield, Trait.Worn, ModData.Traits.WornShield)
                .WithMainTrait(ModData.Traits.HeavyRondache)
                .WithDescription("Similar in size to a buckler, this shield is intended to absorb blows instead of deflecting attacks. It features multiple layers of metal and is reinforced with additional wood.")
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                .WithShieldProperties(5));
        ModData.ItemNames.CastersTarge = ModManager.RegisterNewItemIntoTheShop(
            "CastersTarge",
            iName =>
            {
                // Balance knobs //
                const int scrollLimit = 3; // Normally: 1
                const bool mustHaveFreeHand = true; // Normally: true
                const bool oncePerCombatOnly = true; // Normally: once per targe, per downtime of crafting
                const int level = 1; // Normally: 0
                const int price = 8; // Normally: 2
                const int hrd = 3; // Normally: 3
                Trait ac = ModData.Traits.LightShield; // Normally: LightShield
                
                // Item //
                Item targe = new Item(
                        iName,
                        ModData.Illustrations.CastersTarge,
                        "caster's targe",
                        level, price,
                        ModData.Traits.MoreShields, Trait.Shield, Trait.Martial, Trait.NonMetallic,
                        ac)
                    .WithMainTrait(ModData.Traits.CastersTarge)
                    .WithDescription(
                        $"This small shield is made from wood. It features a special panel of parchment along the inside surface that allows it to store {scrollLimit} spell {S.PluralizeIf("scroll", scrollLimit)}.\n\nYou must have a free hand to activate a scroll, and can only do so once per encounter.")
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                    .WithShieldProperties(hrd)
                    .WithStoresItem((targe, scroll) =>
                        !scroll.HasTrait(Trait.Scroll) || scroll.ScrollProperties is null ||
                        targe.StoredItems.Count >= scrollLimit
                            ? "This shield can only hold up to 3 spell scrolls."
                            : null);
                // Accessing from stored inventory
                targe.StateCheckWhenWielded = (self, targe2) =>
                {
                    if (targe2.StoredItems.Count == 0)
                        return;
                    
                    // Usable once per combat
                    if (oncePerCombatOnly && self.QEffects.Any(qf =>
                            qf.Id == ModData.QEffectIds.CastersTargeUsed && qf.Tag == targe2))
                        return;

                    /*self.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                    {
                        Name = "[CASTER'S TARGE SCROLL MENU]",
                        Tag = targe2,
                        Key = "CastersTarge", // If wielding multiple, will only create one menu
                        ProvideActionIntoPossibilitySection = (qfThis, section) =>
                        {
                            if (section.PossibilitySectionId != PossibilitySectionId.ItemActions)
                                return null;
                                
                            return new SubmenuPossibility(targe2.Illustration, "Targe scrolls")
                            {
                                Subsections = [
                                    new PossibilitySection("Caster's targe")
                                    {
                                        Possibilities = []
                                    }
                                ]
                            };
                        },
                    });*/
                    
                    foreach (Item scroll in targe2.StoredItems
                                 .Where(item => item.ScrollProperties?.Spell is not null))
                        self.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                        {
                            Name = "[CASTER'S TARGE SCROLL SPELL]",
                            Tag = scroll,
                            // Free hand requirement
                            PreventTakingAction = action =>
                            {
                                if (action.CastFromScroll != scroll)
                                    return null;
                                if (!mustHaveFreeHand || action.Owner.HasFreeHand)
                                    return null;
                                return "You must have a free hand to use a scroll from a caster's targe";
                            },
                            // Add scroll actions
                            ProvideActionIntoPossibilitySection = (qfThis, section) =>
                            {
                                if (section.PossibilitySectionId != PossibilitySectionId.ItemActions)
                                    return null;
                                Possibility scrollPoss = ScrollPossibility(qfThis.Owner, scroll);
                                scrollPoss.Caption = "Caster's targe (" + scrollPoss.Caption + ")";
                                //scrollPoss.Illustration = new SideBySideIllustration(targe2.Illustration, scrollPoss.Illustration);
                                scrollPoss.PossibilityGroup = "Targe scrolls";
                                return scrollPoss;

                                /*if (section.Name != "Caster's targe")
                                    return null;
                                Possibility scrollPoss = ScrollPossibility(qfThis.Owner, scroll);
                                return scrollPoss;*/
                            },
                            // Add 1/combat limitation
                            AfterYouTakeAction = async (qfThis, action) =>
                            {
                                if (oncePerCombatOnly && action.CastFromScroll == scroll)
                                {
                                    ConsumeScroll(action, targe2.StoredItems);
                                    // Usable once per combat
                                    qfThis.Owner.AddQEffect(new QEffect()
                                    {
                                        Id = ModData.QEffectIds.CastersTargeUsed,
                                        Tag = targe2
                                    });
                                }
                            },
                        });
                };
                return targe;
            });
    }

    public static Possibility ScrollPossibility(Creature self, Item scroll)
    {
        scroll.ScrollProperties!.Spell = scroll.ScrollProperties.Spell.Duplicate(self, scroll.ScrollProperties.Spell.SpellLevel, true);
        CombatAction itemSpell = scroll.ScrollProperties.Spell.CombatActionSpell;
        itemSpell.CastFromScroll = scroll;
        itemSpell.SpellcastingSource = null;
        if (self.Spellcasting != null)
            itemSpell.SpellcastingSource = self.Spellcasting.Sources
                .Where(src =>
                    src.ClassOfOrigin != Trait.UsesTrickMagicItem)
                .FirstOrDefault(source =>
                {
                    if (!itemSpell.Traits.Contains(source.SpellcastingTradition)
                        && !source.AdditionalSpellsOnThisClassList.Contains(itemSpell.SpellId))
                        return false;
                    return self.PersistentCharacterSheet == null
                           || self.PersistentCharacterSheet.Calculated.SpellTraditionsKnown.Contains(source.SpellcastingTradition);
                  });
        if (itemSpell.SpellcastingSource == null)
        {
            CharacterSheet? persistentCharacterSheet = itemSpell.Owner.PersistentCharacterSheet;
            if (persistentCharacterSheet != null
                && !persistentCharacterSheet.CanUse(scroll)
                && self.HasEffect(QEffectId.TrickMagicItem)
                && itemSpell.ActionCost != 3
                && self.Spellcasting != null)
            {
                CommonSpellEffects.IncreaseActionCostByOne(itemSpell);
                itemSpell.Description += "\n\n{b}Uses Trick Magic Item.{/b} The spell costs 1 more action that normal, and you must succeed at a skill check to activate it.";
                itemSpell.SpellcastingSource = self.Spellcasting.GetSourceByOrigin(Trait.UsesTrickMagicItem);
                itemSpell.Traits.Add(Trait.UsesTrickMagicItem);
            }
        }
        if (self.HasEffect(QEffectId.KineticActivation))
            itemSpell.Traits.Add(Trait.Kineticist);
        itemSpell.SpellcastingSource ??= SpellcastingSource.EmptySource;
        Possibility spellPossibility = Possibilities.CreateSpellPossibility(scroll.ScrollProperties.Spell.CombatActionSpell);
        spellPossibility.Illustration = scroll.Illustration;
        spellPossibility.Caption = scroll.Name;
        spellPossibility.PossibilitySize = PossibilitySize.Full;
        spellPossibility.PossibilityGroup = "Use item";
        return spellPossibility;
    }

    public static void ConsumeScroll(CombatAction action, List<Item> inventory)
    {
        if (action.CastFromScroll!.HasTrait(Trait.Consumable))
            inventory.Remove(action.CastFromScroll);
        else if (action.CastFromScroll.HasTrait(Trait.ItemUsableOncePerDay))
            action.CastFromScroll.UseUp();
    }
}