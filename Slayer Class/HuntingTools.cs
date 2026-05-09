using System.ComponentModel;
using System.Reflection;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Controls;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using SpiritDamage;

namespace Dawnsbury.Mods.SlayerClass;

public static class HuntingTools
{
    #region Static Data

    /// <summary>
    /// Character sheet tag key which contains the list of tools known.
    /// </summary>
    public const string TOOLS_KNOWN_KEY = "HUNTING_TOOLS_KNOWN";
    /// <summary>
    /// Character sheet tag key to find the chosen ItemName runestone from your 7th-level bloodseeking blade specialized arsenal feature.
    /// </summary>
    public const string BLOODSEEKING_BLADE_RUNESTONE_KEY = "BLOODSEEKING_BLADE_SPECIALIZED_ARSENAL_RUNESTONE";
    /// <summary>
    /// Character sheet tag key to find the chosen consecration Trait from your consecrated panoply.
    /// </summary>
    public const string HUNTING_SPIKE_CONSECRATION_KEY = "HUNTING_SPIKE_CONSECRATION_KEY";
    /// <summary>
    /// Character sheet tag key to find the chosen material Trait from your 7th-level consecrated panoply specialized arsenal feature.
    /// </summary>
    public const string HUNTING_SPIKE_MATERIAL_KEY = "HUNTING_SPIKE_SPECIALIZED_ARSENAL_MATERIAL";

    #region ToolId

    public enum ToolId
    {
        None = 0,
        [Description("Bloodseeking Blade")]
        BloodseekingBlade = 1,
        [Description("Chymist's Vials")]
        ChymistsVials = 2,
        [Description("Consecrated Panoply")]
        ConsecratedPanoply = 3,
        [Description("Warded Mail")]
        WardedMail = 4,
        [Description("Adaptation Serums")]
        AdaptationSerums = 5,
        [Description("Repelling Shield")]
        RepellingShield = 6,
        [Description("Spiked Surcoat")]
        SpikedSurcoat = 7,
        [Description("Paired Bloodseeker")]
        PairedBloodseeker = 8,
        [Description("Spirit Oil")]
        SpiritOil = 9,
        [Description("Cure-all")]
        CureAll = 10,
        [Description("Spell Slates")]
        SpellSlates = 11,
        [Description("Catalyzing Flask")]
        CatalyzingFlask = 12,
        [Description("Spectral Lenses")]
        SpectralLenses = 13,
        [Description("Bloodburst Phial")]
        BloodburstPhial = 14,
    }
    
    public static string GetNameFromToolId (this ToolId value)
    {
        Type type = value.GetType();
        FieldInfo? fieldInfo = type.GetField(value.ToString());
        if (fieldInfo == null)
            return value.ToString();
        DescriptionAttribute? attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) as DescriptionAttribute;
        return attribute == null ? value.ToString() : attribute.Description;
    }

    #endregion

    /// <summary>
    /// Hunting tools are either signature tools or are secondary tools. Kind is mostly used to filter choices like your signature tools at levels 1 and 11, as well as for granting the specialized arsenal features based on order of chosen tool.
    /// </summary>
    public enum ToolKind
    {
        None = 0,
        Signature = 1,
        Secondary = 2,
    }

    /// <summary>
    /// An item with this modification has been designated as a specific hunting tool, as determined by its tag data.
    /// </summary>
    public static ItemModificationKind ToolDesignation;

    public static ItemName ChymistsVials;

    public static ItemName ConsecratedPanoply;

    #endregion

    #region Static Tool Methods

    public static List<HuntingTool>? GetTools(CalculatedCharacterSheetValues values)
    {
        return values.Tags.TryGetValue(TOOLS_KNOWN_KEY, out var tools)
            ? tools as List<HuntingTool>
            : null;
    }

    public static List<HuntingTool>? GetTools(Creature cr)
    {
        if (cr.PersistentCharacterSheet?.Calculated is not { } values)
            return null;
        return GetTools(values);
    }

    public static HuntingTool? GetTool(CalculatedCharacterSheetValues values, ToolId toolId)
    {
        return GetTools(values)?.FirstOrDefault(tool => tool.Id == toolId);
    }

    public static HuntingTool? GetTool(Creature cr, ToolId toolId)
    {
        return cr.PersistentCharacterSheet?.Calculated is {} values
            ? GetTools(values)?.FirstOrDefault(tool => tool.Id == toolId)
            : null;
    }

    /// <summary>
    /// Add a hunting tool if it's not already known. Create stored list of hunting tools if it doesn't exist.
    /// </summary>
    public static void AddTool(CalculatedCharacterSheetValues values, HuntingTool tool)
    {
        if (GetTools(values) is { } tools
            && tools.All(innerTool => innerTool.Id != tool.Id))
            tools.Add(tool);
        else
            values.Tags.Add(TOOLS_KNOWN_KEY, new List<HuntingTool>([tool]));
    }

    public static bool IsATool(Item item)
    {
        return item.ItemModifications.Any(mod =>
            mod.Kind == ToolDesignation);
    }

    public static ToolId? GetToolId(Item item)
    {
        return item.ItemModifications
            .FirstOrDefault(mod =>
                mod.Kind == ToolDesignation)
            ?.Tag is string tag
            ? Enum.Parse<ToolId>(tag)
            : null;
    }

    #endregion

    public static void Load()
    {
        // Construct enum data
        ToolDesignation = ItemModifications.RegisterItemModification(
            "huntingToolDesignation",
            mod => "huntingToolDesignation_" + mod.Tag,
            (tag, kind) =>
            {
                // Not my modification
                if (!tag.StartsWith("huntingToolDesignation_"))
                    return null;
                string subTag = tag["huntingToolDesignation_".Length..];
                return new ItemModification(kind)
                {
                    Tag = subTag,
                    ModifyItem = item =>
                    {
                        item.Nickname ??= Enum.Parse<ToolId>(subTag).GetNameFromToolId().ToLower();
                    },
                    UnmodifyItem = item =>
                    {
                        string nickname = Enum.Parse<ToolId>(subTag).GetNameFromToolId().ToLower();
                        if (item.Nickname == nickname)
                            item.Nickname = null;
                    }
                };
            });
        
        /*ChymistsVials = ModManager.RegisterNewItemIntoTheShop(
            "")*/

        ConsecratedPanoply = ModManager.RegisterNewItemIntoTheShop(
            ModData.IdPrepend + "ConsecratedPanoply",
            iName => new Item(
                    iName,
                    ModData.Illustrations.ConsecratedPanoply,
                    "consecrated panoply",
                    0, 0,
                    [ModData.Traits.ModName, ModData.Traits.Slayer, Trait.Worn])
                .WithDescription(
                    "This harness or coat contains a seemingly endless array of charms and consecrated weapons, whether worn openly or in hidden pockets, and their blessings protect you and skewer your prey in equal measure.",
                    "A slayer can designate this item as their consecrated panoply signature tool and reinforce it with trophies.")
                .WithItemGreaterGroup(ModData.ItemGreaterGroups.ClassItems)
                .WithItemGroup("Slayer"));
        
        // Hunting Tool designations
        InventoryContextMenu.Options.Add(new InventoryContextMenuOption((slot, item, inv) =>
        {
            if (slot.Item is null || item is null)
                return null;
            if (slot.CharacterSheet is null
                || GetTools(slot.CharacterSheet.Calculated) is not { } tools)
                return null;

            List<ContextMenuItem> options = [];
            
            foreach (HuntingTool tool in tools)
            {
                if (!tool.LegalItem.HasValue
                    || !tool.LegalItem.Value.ItemValidator.Invoke(slot.CharacterSheet.Calculated, item))
                    continue;
                ToggleDesignation(tool, tool.LegalItem.Value.LegalityDescription);
            }
            
            return options.Count > 0
                ? options.ToArray()
                : null;

            void ToggleDesignation(HuntingTool tool, string thisWhat)
            {
                Item? previousDesignation = inv?.AllItems.FirstOrDefault(tool.IsMyTool);

                string designate = $"{(previousDesignation == item ? "Und" : "D")}esignate";
                
                options.Add(new ContextMenuItem(
                    tool.Icon,
                    designate + " as " + tool.Name,
                    $"{(previousDesignation is not null && previousDesignation != item
                        ? "{i}(This will undesignate " + previousDesignation.Name + ".){/i}\n\n"
                        : null)}{designate} this {thisWhat} as your {tool.Name}, granting you special benefits when using it. Only one item can be designated as your {tool.Name} at a time.",
                    () =>
                    {
                        if (tool.IsMyTool(item))
                            tool.UndesignateAsTool(item);
                        else
                        {
                            foreach (Item otherItem in inv?.AllItems ?? [])
                                tool.UndesignateAsTool(otherItem);
                            tool.DesignateAsTool(item);
                        }
                    }));
            }
        }));
        
        // Stat block display of hunting tools
        var generators = CreatureStatblock.CreatureStatblockSectionGenerators;
        if (generators.FindIndex(blockGen =>
                blockGen.Name == "Spellcasting")
            is var casting)
            CreatureStatblock.CreatureStatblockSectionGenerators.Insert(
                casting,
                new CreatureStatblockSectionGenerator(
                    "Hunting tools",
                    self =>
                    {
                        if (GetTools(self) is not {} tools
                            || tools.Count == 0)
                            return null;
                        tools.Sort((x, y) => x.Kind.CompareTo(y.Kind));
                        return string.Join("\n\n", tools.Select(tool =>
                            $$"""
                              {b}{{tool.Name}}{/b} ({{tool.Kind.ToStringOrTechnical().ToLower()}} tool)
                                  {{tool.ShortDescription.Invoke(self, tool.AccessSpecialized).Replace("\n", "\n    ")}}
                              """));
                    }));
        
        // All hunting tools
        foreach (Feat ft in CreateSignatureTools())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> CreateSignatureTools()
    {
        // Bloodseeking Blade, specialized arsenal free rune
        List<ItemName> bladeRunes = [ItemName.FearsomeRunestone, ItemName.ReturningRunestone, ItemName.ShiftingRunestone];
        foreach (ItemName rune in bladeRunes)
        {
            Item itemTemplate = Items.GetItemTemplate(rune);
            RuneProperties runeProperties = itemTemplate.RuneProperties!;
            yield return new Feat(
                    ModManager.RegisterFeatName(
                        ModData.IdPrepend + "BloodseekingBladePropertyRune." + itemTemplate.Name,
                        $"{{i}}{runeProperties.Prefix.Capitalize()}{{/i}} property rune"),
                    runeProperties.FlavorText,
                    $"At the start of an encounter, your bloodseeking blade gains the effects of the {rune.ToLink(runeProperties.Prefix).WithTag("i")} property rune. This doesn't count against the number of property runes the weapon may have.",
                    [..itemTemplate.Traits, ModData.Traits.BloodseekingBladePropertyRune],
                    null)
                .WithIllustration(itemTemplate.Illustration)
                .WithLevel(itemTemplate.Level)
                .WithOnSheet(values =>
                    values.Tags[BLOODSEEKING_BLADE_RUNESTONE_KEY] = rune);
        }
        
        // Bloodseeking Blade
        yield return new HuntingTool(
                "Bloodseeking Blade",
                ToolId.BloodseekingBlade,
                ToolKind.Signature,
                ModData.Illustrations.BloodseekingBlade,
                (self, isSpecialized) =>
                {
                    //var inventory = self.PersistentCharacterSheet?.Inventory.AllItems;
                    var inventory = self.HeldItems
                        .Concat(self.CarriedItems)
                        .Append(self.BaseArmor ?? self.Armor.Item ?? null)
                        .WhereNotNull()
                        .ToList();
                    Item? blade = inventory.FirstOrDefault(item =>
                            GetToolId(item) is ToolId.BloodseekingBlade);
                    string? ignoreAmount = blade is not null ? (1 + blade.WeaponProperties!.DamageDieCount).WithColor("Blue") : null;
                    Item? trophy = blade is not null ? Trophies.GetTrophy(blade) : null;
                    DamageKind? chosenDk = trophy is not null ? Trophies.GetChosenDamageKind(trophy) : null;
                    string damageType = chosenDk is not null
                        ? (" " + chosenDk.Value.ToStringOrTechnical().WithColor(chosenDk.Value.DamageKindToColor() ) + " ")
                        : " ";
                    return $$"""
                             {b}Bloody Fuller{/b} Against your quarry, you ignore {{(ignoreAmount is null ? "an amount" : ignoreAmount + " points")}} of {{(isSpecialized ? "{Blue}any{/Blue}" : "physical")}} resistance to this tool's damage{{(ignoreAmount is null ? " equal to 1 + the number of weapon damage dice" : null)}}.
                             {b}Reinforced{/b} Your first Strike with this tool deals {Blue}{{(self.Level >= 19 ? 3 : self.Level >= 11 ? 2 : 1)}}d6{/Blue} additional{{damageType}}damage.{{(chosenDk is null ? " The type is chosen from the reinforcing trophy." : null)}}
                             {b}Honed Strike {icon:TwoActions}{/b} [concentrate, relentless] Strike using this tool with a +2 circumstance bonus and ignore the Concealed condition.
                             """
                           + (isSpecialized
                               ? $"\n{{b}}Specialized{{/b}} This tool has {{tooltip:criteffect}}critical specialization effects{{/}}, and gains the effects of a {(self.PersistentCharacterSheet?.Calculated.GetTagOrNull<ItemName>(BLOODSEEKING_BLADE_RUNESTONE_KEY) is {} rune ? ("{i}" + Items.GetItemTemplate(rune).RuneProperties!.Prefix + "{/i} property rune").WithColor("Blue") : "property rune you choose when you Reinforce your Arsenal")}."
                               : null);
                },
                (
                    "simple or martial weapon",
                    (values, item) => item.HasAnyTraits([Trait.Simple, Trait.Martial]) || (values.HasFeat(ModData.FeatNames.PeculiarWeaponry) && item.HasTrait(Trait.Advanced))
                ))
            .ToSignatureToolFeat(
                "You have an even closer connection to your weapon than most slayers.",
                "You can designate a simple or martial weapon as a bloodseeking blade when you Reinforce your Arsenal, gaining the following benefits.",
                "{b}Bloody Fuller{/b} If your quarry is resistant to physical damage dealt by this tool, you ignore an amount of that resistance equal to 1 + the number of weapon damage dice.",
                "Your first Strike with this tool each turn deals 1d6 additional damage of one of the trophy’s damage types, chosen when you Reinforce your Arsenal. The extra damage increases to 2d6 at 11th level and to 3d6 at 19th level.",
                "{b}Honed Strike {icon:TwoActions}{/b} (concentrate, relentless) You center yourself and calm your breathing, then strike. Make a Strike with your bloodseeking blade signature tool. You gain a +2 circumstance bonus to your attack roll and ignore the target’s Concealed condition (but not the Hidden condition).",
                "You gain {tooltip:criteffect}critical specialization effects{/tooltip} with this tool. Bloody Fuller's ignored-resistances applies to all damage from this tool. You also gain an extra {i}fearsome{/i}, {i}returning{/i}, or {i}shifting{/i} rune, chosen when you Reinforce your Arsenal.")
            .WithOnSheet(values =>
            {
                values.AtEndOfRecalculationBeforeMorningPreparations += values2 =>
                {
                    if (GetTool(values2, ToolId.BloodseekingBlade) is not { } blade)
                        return;
                    if (blade.AccessSpecialized)
                    {
                        values.AddSelectionOption(new SingleFeatSelectionOption(
                            "BloodseekingBladePropertyRune",
                            "Bloodseeking Blade property rune",
                            SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                            ft => ft.HasTrait(ModData.Traits.BloodseekingBladePropertyRune)));
                    }
                };
            })
            .WithOnCreature(self =>
            {
                QEffect bladeQf = new QEffect()
                {
                    // Debugging identifier
                    Name = "[HUNTING TOOL: BLOODSEEKING BLADE]",
                    
                    // Initial Benefit:
                    // Bypassing resistances doesn't have access to the calculated damage event.
                    // As a result, it's not possible to only apply once to the greatest resistance.
                    // This implementation only ever bypasses at most the amount you can bypass for
                    // a single action that's getting resisted.
                    
                    // Use the value to track the amount resisted so far
                    HideValue = true,
                    IgnoreAmountOfResistanceAgainstYourActions = (qfBlade, action, dk, defender, resist) =>
                    {
                        // Damage dealt by Strikes with my bloodseeking blade
                        if (!action.HasTrait(Trait.Strike)
                            || action.Item is null
                            || !Slayer.IsMyQuarry(qfBlade.Owner, defender)
                            || HuntingTools.GetTool(qfBlade.Owner, ToolId.BloodseekingBlade)
                                is not { } blade
                            || !blade.IsMyTool(action.Item))
                            return 0;
                        
                        // Must be physical if you don't have the specialized benefit
                        if (!blade.AccessSpecialized && !dk.IsPhysical())
                            return 0;

                        // The amount I want to bypass
                        int bypass = 1 + action.Item.WeaponProperties!.DamageDieCount;
                        
                        // The Tag stores the action being processed so that it doesn't over-apply
                        if (qfBlade.Tag is not CombatAction { } taggedAction
                            || taggedAction != action)
                        {
                            qfBlade.Tag = action; // Start resisting this action
                            qfBlade.Value = 0; // None resisted so far
                        }
                        else
                            // Reduced by the amount bypassed so far
                            bypass = Math.Max(bypass - qfBlade.Value, 0);
                        
                        // Increment the total resisted, capped to the most that was resisted this time
                        qfBlade.Value += Math.Min(bypass, resist);
                        
                        return bypass;
                    },
                    // Reinforced benefit
                    StartOfYourPrimaryTurn = async (qfBlade, me) =>
                    {
                        if (HuntingTools.GetTool(qfBlade.Owner, ToolId.BloodseekingBlade)
                            is not { } blade)
                            return;
                        int numDice = self.Level >= 19 ? 3 : self.Level >= 11 ? 2 : 1;
                        me.AddQEffect(new QEffect(
                            "Reinforced Fuller",
                            $"The first Strike with your bloodseeking blade deals an extra {numDice + "d6"} damage.",
                            ExpirationCondition.ExpiresAtStartOfYourTurn,
                            me,
                            ModData.Illustrations.BloodseekingBlade)
                        {
                            AddExtraKindedDamageOnStrike = (action, target) =>
                            {
                                if (action.Item is null
                                    || !blade.IsMyTool(action.Item))
                                    return null;
                                if (Trophies.GetTrophy(action.Item) is not { } trophy)
                                {
                                    action.Owner.Overhead("*no trophy*", Color.Red, "Trophy not found on your bloodseeking blade.");
                                    return null;
                                }
                                if (Trophies.GetChosenDamageKind(trophy) is not { } chosenKind)
                                {
                                    action.Owner.Overhead("*no trophy*", Color.Red, "No reinforced damage found on your bloodseeking blade's trophy.");
                                    return null;
                                }
                                return new KindedDamage(DiceFormula.FromText(numDice + "d6", "Bloody fuller—Reinforced trophy"), chosenKind);
                            },
                            AfterYouTakeAction = async (qfFuller, action) =>
                            {
                                if (action.HasTrait(Trait.Strike)
                                    && action.Item is not null
                                    && blade.IsMyTool(action.Item))
                                    qfFuller.ExpiresAt = ExpirationCondition.Immediately;
                            } 
                        });
                    },
                    // Slaying technique
                    ProvideStrikeModifier = item =>
                    {
                        if (!GetTool(self, ToolId.BloodseekingBlade)?.IsMyTool(item) ?? false)
                            return null;

                        CombatAction honedStrike = self.CreateStrike(item)
                            .WithActionCost(2)
                            .WithExtraTrait(Trait.Basic)
                            .WithExtraTrait(Trait.Concentrate)
                            .WithExtraTrait(ModData.Traits.Relentless)
                            .With(ca =>
                            {
                                ca.Illustration = new SideBySideIllustration(ModData.Illustrations.BloodseekingBlade, IllustrationName.TargetSheet);
                                ca.Description = StrikeRules.CreateBasicStrikeDescription2(ca.StrikeModifiers, "You gain a +2 circumstance bonus to the attack roll and ignore your target's Concealed condition (but not the Hidden condition).");
                                ca.StrikeModifiers.HuntersAim = true;
                                ca.StrikeModifiers.AdditionalBonusesToAttackRoll = [
                                    new Bonus(2, BonusType.Circumstance, "Honed strike")
                                ];
                                ca.Traits = new Traits([ModData.Traits.ModName, ..ca.Traits], ca);
                            });
                        honedStrike.WithFullRename("Honed Strike");
                        honedStrike.ShortDescription += ", and ignore the Concealed condition";

                        return honedStrike;
                    },
                    // Specialized Arsenal, critical specialization
                    YouHaveCriticalSpecialization = (qfThis, item, action, defender) =>
                        GetTool(qfThis.Owner, ToolId.BloodseekingBlade) is {} blade
                        && blade.IsMyTool(item)
                        && blade.AccessSpecialized,
                    // Specialized Arsenal, free rune
                    StartOfCombat = async qfThis =>
                    {
                        if (!(qfThis.Owner.PersistentCharacterSheet?.Calculated.Tags.TryGetValue(
                                BLOODSEEKING_BLADE_RUNESTONE_KEY, out object? tryRune) ?? false)
                            || tryRune is not ItemName iRune)
                            return;
                        
                        foreach (Item heldItem in qfThis.Owner.HeldItems)
                        {
                            Item rune = Items.CreateNew(iRune);
                            RuneProperties runeProperties = rune.RuneProperties!;
                            if (runeProperties.CanBeAppliedTo?.Invoke(rune, heldItem) == null)
                            {
                                heldItem.Runes.Add(rune);
                                runeProperties.ApplyRuneOntoItem(rune, heldItem);
                            }
                        }
                    }
                };

                self.AddQEffect(bladeQf);
            });
        
        // Chymist's Vials
        // TODO: Finish chymist
        /*yield return new HuntingTool(
                "Chymist's Vials",
                ToolId.ChymistsVials,
                ToolKind.Signature,
                ModData.Illustrations.ChymistsVials,
                (self, isSpecialized) =>
                {
                    return "";
                },
                (
                    "alchemical toolkit",
                    item => false // Uses a specific item
                ))
            .ToSignatureToolFeat(
                "You believe that a battle is won well before it begins, and you use your knowledge of alchemy and herbalism to guarantee victory.",
                $"You gain a special toolkit which acts as your {ModData.Tooltips.ChymistPronunciation("chymist's vials")} and grants the following benefits.",
                "",
                "",
                "",
                "");*/

        // Consecrated Panoply, choose holy or unholy
        List<Trait> consecrationTraits = [HolyTrait.Holy, UnholyTrait.Unholy];
        foreach (Trait trait in consecrationTraits)
        {
            string traitName = trait.ToStringOrTechnical();
            yield return new Feat(
                    ModManager.RegisterFeatName(
                        ModData.IdPrepend + "HuntingSpikeConsecration." + traitName,
                        traitName),
                    null,
                    $"Your hunting spikes gain the {traitName.ToLower()} trait.",
                    [ModData.Traits.HuntingSpikeConsecration, trait],
                    null)
                .WithOnSheet(values =>
                    values.Tags[HUNTING_SPIKE_CONSECRATION_KEY] = trait);
        }
        
        // Consecrated Panoply, specialized arsenal material
        var spikeMaterials = new List<(ItemName Material, Trait Trait, int Level)>
        {
            (ItemName.ColdIron, Trait.ColdIron, 7),
            (ItemName.Silver, Trait.Silver, 7),
            (ItemName.Adamantine, Trait.Adamantine, 13),
        };
        foreach ((ItemName material, Trait trait, int level) in spikeMaterials)
        {
            Item itemTemplate = Items.GetItemTemplate(material);
            RuneProperties runeProperties = itemTemplate.RuneProperties!;
            yield return new Feat(
                    ModManager.RegisterFeatName(
                        ModData.IdPrepend + "HuntingSpikeMaterial." + itemTemplate.Name,
                        $"{{i}}{runeProperties.Prefix.Capitalize()}{{/i}} material"),
                    runeProperties.FlavorText,
                    $"Whenever you draw a hunting spike from your consecrated panoply, it gains the effects of the {material.ToLink(runeProperties.Prefix).WithTag("i")} material.",
                    [..itemTemplate.Traits, ModData.Traits.HuntingSpikeMaterial],
                    null)
                .WithIllustration(itemTemplate.Illustration)
                .WithLevel(level)
                .WithOnSheet(values =>
                    values.Tags[HUNTING_SPIKE_MATERIAL_KEY] = trait);
        }
        
        // Consecrated Panoply
        yield return new HuntingTool(
                "Consecrated Panoply",
                ToolId.ConsecratedPanoply,
                ToolKind.Signature,
                ModData.Illustrations.ConsecratedPanoply,
                (self, isSpecialized) =>
                {
                    //var inventory = self.PersistentCharacterSheet?.Inventory.AllItems;
                    var inventory = self.HeldItems
                        .Concat(self.CarriedItems)
                        .Append(self.BaseArmor ?? self.Armor.Item ?? null)
                        .WhereNotNull()
                        .ToList();
                    Item? panoplyItem = inventory.FirstOrDefault(item =>
                        GetToolId(item) is ToolId.ConsecratedPanoply);
                    Item? trophy = panoplyItem is not null ? Trophies.GetTrophy(panoplyItem) : null;
                    var data = trophy is not null ? Trophies.GetTrophyData(trophy) : null;

                    string worn = "worn".WithColor(panoplyItem is null ? "Red" : "Green");
                    var traitsList = data?.Traits?
                        .Select(t => t.HumanizeLowerCase2().WithColor("Blue"))
                        .ToList() ?? [];
                    string traits = data?.Traits is null
                        ? "creatures with any of the trophy's traits"
                        : S.ConstructOrList(traitsList) + " creatures";
                    var traditionsList = data?.Traditions?
                        .Select(t =>
                            t.HumanizeLowerCase2().WithColor(t.TraditionTraitToColor()))
                        .ToList() ?? [];
                    string traditions = data?.Traditions is null
                        ? "spells of any of the trophy's traditions"
                        : S.ConstructOrList(traditionsList) + " spells";
                    int? potency = self.Level switch
                    {
                        >= 16 => 3,
                        >= 10 => 2,
                        >= 2 => 1,
                        _ => null
                    };
                    string? striking = self.Level switch
                    {
                        >= 19 => "major striking",
                        >= 12 => "greater striking",
                        >= 4 => "striking",
                        _ => null
                    };
                    int numDice = self.Level switch
                    {
                        >= 19 => 4,
                        >= 12 => 3,
                        >= 4 => 2,
                        _ => 1
                    };
                    string runeDescription = (potency is not null ? "+" + potency + " " : null) + striking;
                    if (!string.IsNullOrEmpty(runeDescription))
                        runeDescription = "{i}{Blue}" + runeDescription + "{/Blue}{/i} ";
                    Trait? consecration = self.PersistentCharacterSheet?.Calculated
                        .GetTagOrNull<Trait>(HUNTING_SPIKE_CONSECRATION_KEY);
                    Trait? material = self.PersistentCharacterSheet?.Calculated
                        .GetTagOrNull<Trait>(HUNTING_SPIKE_MATERIAL_KEY);
                    string traitDescription =
                        consecration?.ToStringOrTechnical().ToLower()
                        + (consecration is not null && material is not null ? ", " : null)
                        + material?.HumanizeLowerCase2();
                    if (!string.IsNullOrEmpty(traitDescription))
                        traitDescription = " {Blue}(" + traitDescription + "){/Blue}";
                    string? arsenal = null;
                    if (isSpecialized)
                    {
                        arsenal += $"\n{{b}}Specialized{{/b}} Your hunting spikes deal {numDice} persistent {"bleed".WithColor(DamageKind.Bleed.DamageKindToColor())} damage, and can be cold iron or silver.";
                        if (self.Level >= 13)
                            arsenal = arsenal
                                .Replace("damage,", $" and {numDice} {"spirit".WithColor(DamageSpirit.Spirit.DamageKindToColor())} damage;")
                                .Replace("cold iron or silver", "cold iron, silver, or adamantine");
                        if (self.Level >= 19)
                            arsenal += " You draw and Strike twice with Hunting Spike.";
                    }
                    
                    return $$"""
                             {b}Safeguard Charms{/b} If {{worn}}, you gain a +1 status bonus to saves against your quarry.
                             {b}Reinforced{/b} You also gain this bonus (+2 for your quarry) against {{traits}}, and against {{traditions}}.
                             {b}Hunting Spike {icon:Action}{/b} [manipulate, relentless] If you have a free hand, draw and Strike with one of your {{runeDescription}}hunting spikes{{traitDescription}}.
                             """ + arsenal;
                },
                (
                    "harness or coat",
                    (_, item) => item.ItemName == ConsecratedPanoply
                ))
            .ToSignatureToolFeat(
                "Whether you worship a deity or not, you know that their trappings are anathema to many of the creatures you hunt.",
                "You gain an item you can designate as your consecrated panoply when you Reinforce your Arsenal, gaining the following benefits while worn.",
                "{b}Safeguard Charms{/b} While wearing this signature tool, the consecrated charms within protect you from harm. You gain a +1 status bonus to saving throws against your quarry.",
                "You also gain this bonus against creatures with any of the trophy's traits and against spells of any of the trophy's traditions. This increases to +2 against your quarry.",
                $"{{b}}Hunting Spike {{icon:Action}}{{/b}} (manipulate, relentless) {{b}}Requirements{{/b}} You have a free hand; {{b}}Effect{{/b}} You draw and Strike with one of your endless {ModData.Illustrations.HuntingSpike.IllustrationAsIconString} hunting spikes (these function as {ItemName.Dagger.ToLink("daggers")}). Your hunting spikes have either the holy or unholy trait, chosen when you Reinforce your Arsenal. Your panoply imbues the effects of the best fundamental runes for your level onto your spikes.",
                $"Your hunting spikes deal an additional 1 persistent {"bleed".WithColor(DamageKind.Bleed.DamageKindToColor())} damage per weapon damage die. In addition, they are treated as either cold iron or silver, chosen when you Reinforce your Arsenal. At 13th level, your spikes deal 1 additional {"spirit".WithColor(DamageSpirit.Spirit.DamageKindToColor())} damage per weapon damage die, and you can choose adamantine instead of cold iron or silver. At 19th level, you can draw and Strike twice when you use the Hunting Spike activity.")
            .WithExtraTrait(Trait.Rebalanced)
            .WithFreeInventoryItem(ConsecratedPanoply)
            .WithOnSheet(values =>
            {
                values.AtEndOfRecalculationBeforeMorningPreparations += values2 =>
                {
                    if (GetTool(values2, ToolId.ConsecratedPanoply) is not { } panoplyTool)
                        return;
                    
                    // Slaying Technique, technically
                    values.AddSelectionOption(new SingleFeatSelectionOption(
                        "HuntingSpikeConsecration",
                        "Hunting Spike consecration",
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                        ft => ft.HasTrait(ModData.Traits.HuntingSpikeConsecration)));
                    
                    // Specialized benefit
                    if (panoplyTool.AccessSpecialized)
                    {
                        values.AddSelectionOption(new SingleFeatSelectionOption(
                            "HuntingSpikeMaterial",
                            "Hunting Spike special material",
                            SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                            ft => ft.HasTrait(ModData.Traits.HuntingSpikeMaterial) && ft.LevelIfAny <= values2.CurrentLevel));
                    }
                };
            })
            .WithOnCreature(self =>
            {
                // Determination can be made on load since the item can't move anywhere after combat starts
                if (HuntingTools.GetTool(self, ToolId.ConsecratedPanoply) is not { } panoplyTool
                    || self.CarriedItems.FirstOrDefault(panoplyTool.IsMyTool) is not {} panoplyItem
                    || Trophies.GetTrophy(panoplyItem) is not {} trophy
                    || Trophies.GetTrophyData(trophy) is not {} data)
                    return;
                
                QEffect panoplyQf = new QEffect()
                {
                    // Debugging identifier
                    Name = "[HUNTING TOOL: CONSECRATED PANOPLY]",
                    // Initial Benefit
                    BonusToDefenses = (qfThis, action, def) =>
                    {
                        if (action?.Owner is null
                            || !def.IsSavingThrow())
                            return null;

                        int bonus = 0;
                        string subtitle = "";
                        
                        // Quarry
                        if (Slayer.IsMyQuarry(qfThis.Owner, action.Owner))
                        {
                            bonus++;
                            subtitle += "quarry";
                        }
                        
                        // Reinforced Benefit
                        if (action.Owner.Traits.ContainsOneOf([..data.Traits, ..data.Traditions])
                            || (action.HasTrait(Trait.Spell) && data.Traditions.Contains(action.SpellcastingSource!.SpellcastingTradition)))
                        {
                            bonus++;
                            if (subtitle is not "")
                                subtitle += ", ";
                            subtitle += "reinforced";
                        }

                        return bonus > 0
                            ? new Bonus(bonus, BonusType.Status, $"Consecrated panoply ({subtitle})")
                            : null;
                    },
                    ProvideMainAction = qfThis =>
                    {
                        Trait? consecration = self.PersistentCharacterSheet?.Calculated
                            .GetTagOrNull<Trait>(HUNTING_SPIKE_CONSECRATION_KEY);
                        Trait? material = self.PersistentCharacterSheet?.Calculated
                            .GetTagOrNull<Trait>(HUNTING_SPIKE_MATERIAL_KEY);
                        Item displaySpike = CreateHuntingSpike(qfThis.Owner, ItemName.Dagger, consecration, material, panoplyTool.AccessSpecialized);
                        string? striking = displaySpike.WeaponProperties!.DamageDieCount switch
                        {
                            >= 4 => "major striking",
                            >= 3 => "greater striking",
                            >= 2 => "striking",
                            _ => null
                        };
                        string runeDescription = (displaySpike.WeaponProperties!.ItemBonus > 0 ? "+" + displaySpike.WeaponProperties!.ItemBonus + " " : null) + striking;
                        if (!string.IsNullOrEmpty(runeDescription))
                            runeDescription = " {i}{Blue}" + runeDescription + "{/Blue}{/i} ";
                        string traitDescription =
                            consecration?.ToStringOrTechnical().ToLower()
                            + (consecration is not null && material is not null ? ", " : null)
                            + material?.HumanizeLowerCase2();
                        if (!string.IsNullOrEmpty(traitDescription))
                            traitDescription = " {Blue}(" + traitDescription + "){/Blue}";

                        return qfThis.Owner.HasFeat(ModData.FeatNames.ExpansivePanoply)
                            ? new SubmenuPossibility(
                                ModData.Illustrations.HuntingSpike,
                                "Hunting Spike")
                            {
                                SpellIfAny = HuntingSpikeAction(null, null),
                                Subsections =
                                [
                                    new PossibilitySection("Base Items")
                                    {
                                        Possibilities = new List<ItemName>([ItemName.Dagger, ItemName.Shortsword, ItemName.Club])
                                            .Select(Possibility (iName) =>
                                                new ActionPossibility(HuntingSpikeAction(iName,
                                                    iName.ToStringOrTechnical())))
                                            .ToList(),
                                    }
                                ]
                            }
                            : new ActionPossibility(HuntingSpikeAction(ItemName.Dagger, "Dagger"));
                        
                        CombatAction HuntingSpikeAction(ItemName? weapon, string? subtitle)
                        {
                            bool isThrowable = weapon is ItemName.Dagger or ItemName.Club;

                            string? itemDescription = weapon is not null
                                ? $"\n\n{{b}}{subtitle}{{/b}} {Monk.DescribeAttack(Items.CreateNew(weapon.Value)).Replace("wizardweapon", null).Replace("rogueweapon", null).Replace("bardweapon", null).Replace(", ,", ",").Replace(", ,", ",")}"
                                : null;
                            
                            return new CombatAction(
                                    qfThis.Owner,
                                    ModData.Illustrations.HuntingSpike,
                                    "Hunting Spike" + (subtitle is not null ? " (" + subtitle + ")" : null),
                                    [ModData.Traits.ModName, Trait.Manipulate, ModData.Traits.Relentless, Trait.Basic],
                                    $$"""
                                    {b}Requirements{/b} You have a free hand

                                    You draw and Strike with one of your{{runeDescription}}hunting spikes{{traitDescription}}.{{(panoplyTool.AccessSpecialized && self.Level >= 19 ? " {Blue}You can do this twice.{/Blue}" : null)}}{{itemDescription}}
                                    """,
                                    Target.Self()
                                        .WithAdditionalRestriction(self2 =>
                                            self2.HasFreeHand
                                            || (isThrowable 
                                                && self2.QEffects.Any(qf =>
                                                    qf.Id == ModData.QEffectIds.CrossbowSlayer &&
                                                    self2.HeldItems.Any(item => item == qf.Tag)))
                                                ? null
                                                : "No free hand"))
                                .WithActionCost(1)
                                .WithActionId(ModData.ActionIds.HuntingSpike)
                                .WithEffectOnSelf(async self2 =>
                                {
                                    if (weapon is null)
                                        return;

                                    await DoActivity();
                                    
                                    if (panoplyTool.AccessSpecialized && self.Level >= 19)
                                        await DoActivity();

                                    return;
                                    
                                    async Task DoActivity()
                                    {
                                        Item huntingSpike = CreateHuntingSpike(self2, weapon.Value, consecration, material, panoplyTool.AccessSpecialized);
                                        
                                        // Increase its thrown range, if any
                                        if (self2.FindQEffect(ModData.QEffectIds.CrossbowSlayer) is var xbs
                                            && xbs is not null
                                            && huntingSpike.WeaponProperties!.Throwable)
                                            huntingSpike.WeaponProperties!
                                                .WithThrownXFeet(Math.Max(
                                                    ((Item)xbs.Tag!).WeaponProperties!.RangeIncrement,
                                                    huntingSpike.WeaponProperties!.RangeIncrement));

                                        // Cache helds and add the spike
                                        List<Item> helds = self2.HeldItems.ToList();
                                        if (xbs is not null)
                                        {
                                            self2.HeldItems.Clear();
                                        }
                                        self2.AddHeldItem(huntingSpike);
                                        
                                        await self2.Battle.GameLoop.StateCheck();
                                        
                                        await CommonCombatActions.StrikeCreature(
                                            self2,
                                            strike =>
                                            {
                                                if (!(strike.Item?.Name.ToLower().Contains("hunting spike") ?? false))
                                                    return false;
                                                if (xbs is not null && strike.StrikeModifiers.CalculatedRangeKind is RangeKind.Melee)
                                                    return false;
                                                return true;
                                            },
                                            strike =>
                                            {
                                                // This is a thrown strike
                                                if (strike.Item!.WeaponProperties!.Throwable
                                                    && strike.StrikeModifiers.CalculatedRangeKind is RangeKind.Ranged)
                                                {
                                                    // End it early if possible
                                                    strike.WithEffectOnSelf(async _ =>
                                                    {
                                                        xbs?.ExpiresAt = ExpirationCondition.Immediately;
                                                    });
                                                }
                                            },
                                            null,
                                            ModData.Illustrations.HuntingSpike,
                                            null, true, "Pass");

                                        // Restore helds if necessary
                                        if (xbs is not null)
                                            self2.HeldItems.AddRange(helds);
                                        // End later if necessary
                                        xbs?.ExpiresAt = ExpirationCondition.Immediately;
                                    }
                                });
                        }
                    },
                };
                self.AddQEffect(panoplyQf);
            });
        
        // Warded Mail
        yield return new HuntingTool(
                "Warded Mail",
                ToolId.WardedMail,
                ToolKind.Signature,
                ModData.Illustrations.WardedMail,
                (self, isSpecialized) =>
                {
                    var inventory = self.HeldItems
                        .Concat(self.CarriedItems)
                        .Append(self.BaseArmor ?? self.Armor.Item ?? null)
                        .WhereNotNull()
                        .ToList();
                    Item? mail = inventory.FirstOrDefault(item =>
                        GetToolId(item) is ToolId.WardedMail);
                    
                    // Initial Benefit
                    int? quarryResistance = 2 + mail?.ArmorProperties?.ItemBonus;
                    string resistWhat = isSpecialized ? "{Blue}all{/Blue} your quarry's damage" : "your quarry's physical damage";
                    string iB =
                        $"{{b}}Fortified Plate{{/b}} {"Wearing".WithColor(mail is not null && (mail == self.BaseArmor || mail == self.Armor.Item) ? "Green" : "Red")} this tool grants " + (quarryResistance is null
                            ? $"resistance to {resistWhat} equal to 2 + the armor's potency rune value."
                            : $"{quarryResistance.Value.WithColor("Blue")} resistance to {resistWhat}.");
                    
                    // Reinforced
                    Item? trophy = mail is not null ? Trophies.GetTrophy(mail) : null;
                    DamageKind? chosenDk = trophy is not null ? Trophies.GetChosenDamageKind(trophy) : null;
                    string reinf = $"{{b}}Reinforced{{/b}} You gain {(1 + self.Level / 2).WithColor("Blue")} resistance to " + (chosenDk is null
                        ? "a damage type you choose from the reinforcing trophy."
                        : $"{chosenDk.Value.ToStringOrTechnical().WithColor(chosenDk.Value.DamageKindToColor())} damage.");
                    
                    // Slaying Technique
                    string slayTech =
                        "{b}Armored Shelter {icon:Action}{/b} [relentless] Position your worn warded mail to gain a +2 circumstance bonus to AC as well as Reflex against area effects. Lasts until the end of your next turn, you move, or you make an attack.";
                    
                    // Specialized Arsenal
                    Trait? armorCat = mail?.Traits.FirstOrDefault(trait =>
                        trait is Trait.HeavyArmor or Trait.MediumArmor);
                    Trait? armorGrp = mail?.Traits.FirstOrDefault(trait =>
                        trait is Trait.Composite or Trait.Leather or Trait.Plate or Trait.Chain);
                    string? armorString = armorCat is not null && armorGrp is not null
                        ? (armorCat.Value.ToStringOrTechnical().Replace("Armor", "").ToLower() + " " +
                           armorGrp.Value.ToStringOrTechnical().ToLower()).WithColor("Blue") + " "
                        : null;
                    string specArs = $"{{b}}Specialized{{/b}} This tool has {armorString}{{tooltip:armoreffect}}armor specialization effects{{/}}.";
                    
                    return $"""
                             {iB}
                             {reinf}
                             {slayTech}{(isSpecialized ? "\n" + specArs : null)}
                             """;
                },
                (
                    "armor",
                    (_, item) => item.HasTrait(Trait.Armor)
                ))
            .ToSignatureToolFeat(
                "Your armor isn’t just protection: it fits you perfectly, matching your movements and warding off your quarry.",
                "You become trained in heavy armor, and its proficiency increases when your proficiency with medium armor does. You can designate a suit of armor as your warded mail when you Reinforce your Arsenal, gaining the following benefits.",
                "{b}Fortified Plate{/b} While wearing this signature tool, you gain resistance to physical damage dealt by your quarry equal to 2 + the value of the armor's potency rune.",
                "You also gain resistance equal to 1 + half your level to one of the trophy’s damage types, chosen when you Reinforce your Arsenal.",
                "{b}Armored Shelter {icon:Action}{/b} (relentless) {b}Requirements{/b} You are wearing your warded mail; {b}Effect{/b} You position your armor to better protect you. You gain a +2 circumstance bonus to your AC and to Reflex saves against area effects until the end of your next turn or until you move from your current space or use an attack action, whichever comes first.",
                "You gain access to the {tooltip:armoreffect}armor specialization effects{/} of this signature tool, and the initial benefit grants resistance to all damage dealt by your quarry.")
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.HeavyArmor, Proficiency.Trained);
                values.Proficiencies.Autoupgrade(
                [
                    Trait.MediumArmor,
                    Trait.Armor
                    ],
                [Trait.HeavyArmor]);
            })
            .WithOnCreature(self =>
            {
                QEffect mailQf = new QEffect()
                {
                    // Debugging identifier
                    Name = "[HUNTING TOOL: WARDED MAIL]",
                    // Resistances; Initial Benefit, Reinforced, Specialized Arsenal
                    StateCheck = qfThis =>
                    {
                        if (GetTool(qfThis.Owner, ToolId.WardedMail)
                            is not { } mail
                            || qfThis.Owner.Armor.Item is not { } armor
                            || !mail.IsMyTool(armor))
                            return;
                        
                        // Initial Benefit, Specialized Arsenal
                        int ibAmount = 2 + armor.ArmorProperties!.ItemBonus;
                        if (mail.AccessSpecialized)
                        {
                            // Resist All to quarry
                            qfThis.Owner.WeaknessAndResistance.AddSpecialResistance(
                                new ResistanceToAllQuarry(ibAmount, qfThis.Owner));

                            #region Armor Specialization

                            if (armor.HasTrait(Trait.MediumArmor) || armor.HasTrait(Trait.HeavyArmor))
                            {
                                if (armor.HasTrait(Trait.Composite))
                                    qfThis.Owner.WeaknessAndResistance.AddResistance(DamageKind.Piercing, 1 + armor.ArmorProperties.ItemBonus);
                                if (armor.HasTrait(Trait.Leather))
                                    qfThis.Owner.WeaknessAndResistance.AddResistance(
                                        DamageKind.Bludgeoning,
                                        1 + armor.ArmorProperties.ItemBonus);
                                if (armor.HasTrait(Trait.Plate))
                                    qfThis.Owner.WeaknessAndResistance.AddResistance(
                                        DamageKind.Slashing,
                                        (armor.HasTrait(Trait.HeavyArmor) ? 2 : 1) + armor.ArmorProperties.ItemBonus);
                                if (armor.HasTrait(Trait.Chain))
                                    qfThis.Owner.AddQEffect(new QEffect()
                                    {
                                        YouAreDealtDamageEvent = async (_, dEvent) =>
                                        {
                                            if (dEvent.CheckResult != CheckResult.CriticalSuccess)
                                                return;
                                            dEvent.ReduceBy(4 + armor.ArmorProperties.ItemBonus,
                                                "Chain armor specialization effect");
                                        }
                                    });
                            }

                            #endregion
                        }
                        else
                            qfThis.Owner.WeaknessAndResistance.AddSpecialResistance(
                                new SpecialResistanceQuarry(
                                    "physical",
                                    (_, kind) => kind.IsPhysical(),
                                    ibAmount,
                                    null,
                                    qfThis.Owner));
                        
                        // Reinforced 
                        if (Trophies.GetTrophy(armor) is { } trophy
                            && Trophies.GetChosenDamageKind(trophy) is { } chosenKind)
                        {
                            int reinfAmount = 1 + (qfThis.Owner.Level / 2);
                            qfThis.Owner.WeaknessAndResistance.AddResistance(chosenKind, reinfAmount);
                        }
                    },
                    // Slaying Technique
                    ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.ItemActions
                            || GetTool(qfThis.Owner, ToolId.WardedMail)
                                is not { } mail
                            || qfThis.Owner.Armor.Item is not { } armor
                            || !mail.IsMyTool(armor)
                            || qfThis.Owner.HasEffect(ModData.QEffectIds.ArmoredShelter))
                            return null;
                        
                        return (ActionPossibility) new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.WardedMail,
                                "Armored Shelter",
                                [Trait.Basic, ModData.Traits.ModName, ModData.Traits.Relentless],
                                """
                                {i}You position your armor to better protect you.{/i}

                                You gain a +2 circumstance bonus to your AC and to Reflex saves against area effects until the end of your next turn or until you move from your current space or use an attack action, whichever comes first.
                                """,
                                Target.Self())
                            .WithActionId(ModData.ActionIds.ArmoredShelter)
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.RaiseShield)
                            .WithEffectOnSelf(async self2 =>
                            {
                                QEffect shelter = new QEffect(
                                        "Armored Shelter",
                                        "You have a +2 circumstance bonus to your AC and to Reflex saves against area effects. Ends early if you move from your current space or use an attack action.",
                                        ExpirationCondition.Never,
                                        self2,
                                        ModData.Illustrations.WardedMail)
                                    {
                                        Tag = self2.Space.TopLeftTile,
                                        Id = ModData.QEffectIds.ArmoredShelter,
                                        BonusToDefenses = (_, action, def) =>
                                        {
                                            if (def is Defense.AC)
                                                return ShelterBonus();
                                            if (def is Defense.Reflex && action?.Target is AreaTarget)
                                                return ShelterBonus();
                                            return null;
                                            
                                            Bonus ShelterBonus () => new Bonus(2, BonusType.Circumstance, "Armored shelter", true);
                                        },
                                        AfterYouTakeAction = async (qfShelter, action) =>
                                        {
                                            if (action.HasTrait(Trait.Attack))
                                                qfShelter.ExpiresAt = ExpirationCondition.Immediately;
                                        },
                                        StateCheck = qfShelter =>
                                        {
                                            if (!ReferenceEquals(qfShelter.Tag, qfShelter.Owner.Space.TopLeftTile))
                                                qfShelter.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    }
                                    .WithExpirationAtEndOfSourcesNextTurn(self2, true);
                                self2.AddQEffect(shelter);
                            });
                    }
                };

                self.AddQEffect(mailQf);
            });
    }

    public static Item CreateHuntingSpike(Creature slayer, ItemName baseWeapon, Trait? consecration = null, Trait? material = null, bool isSpecialized = false)
    {
        Item spike = Items.CreateNew(baseWeapon);
        spike.ProsaicName = "hunting spike (" + spike.BaseItemName.ToStringOrTechnical().ToLower() + ")";
        spike.Illustration = ModData.Illustrations.HuntingSpike;
        spike.Price = 0;
        spike.Traits.Add(Trait.EncounterEphemeral);
        spike.Traits.Add(Trait.HandEphemeral);
        if (consecration is not null)
            spike.Traits.Add(consecration.Value);
        if (material is not null)
            spike.Traits.Add(material.Value);
        spike.WeaponProperties!.ItemBonus = slayer.Level switch
        {
            >= 16 => 3,
            >= 10 => 2,
            >= 2 => 1,
            _ => 0
        };
        spike.WeaponProperties!.DamageDieCount = slayer.Level switch
        {
            >= 19 => 4,
            >= 12 => 3,
            >= 4 => 2,
            _ => 1
        };
        if (isSpecialized)
        {
            if (slayer.Level >= 7)
                spike.WeaponProperties!.WithAdditionalPersistentDamage(spike.WeaponProperties!.DamageDieCount.ToString(), DamageKind.Bleed);
            if (slayer.Level >= 13)
                spike.WeaponProperties!.WithAdditionalDamage(spike.WeaponProperties!.DamageDieCount.ToString(), DamageSpirit.Spirit);
        }
        return spike;
    }
    
    /// <summary>
    /// Appends OnSheet behavior which adds the given item to your inventory if it doesn't exist.
    /// </summary>
    internal static Feat WithFreeInventoryItem(this Feat toolFeat, ItemName freeItem)
    {
        return toolFeat
            .WithOnSheet(values =>
            {
                // Create collection of all inventories, campaign and free play
                var inventories = new Dictionary<int, Inventory>(values.Sheet.InventoriesByLevel)
                {
                    { 0, values.Sheet.CampaignInventory }
                };
                foreach (var (level, inv) in inventories)
                {
                    // If you don't have the free item, add it
                    Item? firstFreeItem = inv.AllItems.FirstOrDefault(item => item.ItemName == freeItem);
                    
                    if (firstFreeItem is not null)
                        continue;
                    
                    firstFreeItem = Items.CreateNew(freeItem);
                    if (inv.IsEmpty
                        && inventories
                                .Where(kvp => kvp.Key >= 1 && kvp.Key < level && !kvp.Value.IsEmpty)
                                .Select(kvp => kvp.Value)
                                .LastOrDefault()
                            is {} prevInv)
                    {
                        inv.BecomeFrom(prevInv);
                    }
                    inv.AddAtEndOfBackpack(firstFreeItem);
                }
            });
    }

    internal static Feat WithExtraTrait(this Feat toolFeat, Trait trait)
    {
        toolFeat.Traits.Add(trait);
        return toolFeat;
    }
}