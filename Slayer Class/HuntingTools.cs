using System.ComponentModel;
using System.Reflection;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Controls;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;

public static class HuntingTools
{
        /// <summary>
    /// Character sheet tag key which contains the list of tools known.
    /// </summary>
    public const string ToolsKnownKey = "HUNTING_TOOLS_KNOWN";

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

    public static List<HuntingTool>? GetTools(CalculatedCharacterSheetValues values)
    {
        return values.Tags.TryGetValue(ToolsKnownKey, out var tools)
            ? tools as List<HuntingTool>
            : null;
    }

    public static List<HuntingTool>? GetTools(Creature cr)
    {
        if (cr.PersistentCharacterSheet?.Calculated is not { } values)
            return null;
        return HuntingTools.GetTools(values);
    }

    public static HuntingTool? GetTool(CalculatedCharacterSheetValues values, ToolId toolId)
    {
        return HuntingTools.GetTools(values)?.FirstOrDefault(tool => tool.Id == toolId);
    }

    public static HuntingTool? GetTool(Creature cr, ToolId toolId)
    {
        return cr.PersistentCharacterSheet?.Calculated is {} values
            ? HuntingTools.GetTools(values)?.FirstOrDefault(tool => tool.Id == toolId)
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
            values.Tags.Add(ToolsKnownKey, new List<HuntingTool>([tool]));
    }

    public static bool IsATool(Item item)
    {
        return item.ItemModifications.Any(mod =>
            mod.Kind == ToolDesignation);
    }

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
                        if (item.Nickname is null)
                            item.Nickname = Enum.Parse<ToolId>(subTag).GetNameFromToolId().ToLower();
                    },
                    UnmodifyItem = item =>
                    {
                        string nickname = Enum.Parse<ToolId>(subTag).GetNameFromToolId().ToLower();
                        if (item.Nickname == nickname)
                            item.Nickname = null;
                    }
                };
            });
        
        // Inventory context buttons
        InventoryContextMenu.Options.Add(new InventoryContextMenuOption((slot, item, inv) =>
        {
            if (slot.Item is null || item is null)
                return null;
            if (slot.CharacterSheet is null
                || HuntingTools.GetTools(slot.CharacterSheet.Calculated) is not { } tools)
                return null;

            List<ContextMenuItem> options = [];
            
            foreach (HuntingTool tool in tools)
            {
                if (!tool.LegalItem.HasValue
                    || !tool.LegalItem.Value.ItemValidator.Invoke(item))
                    continue;
                ToggleDesignation(tool, tool.LegalItem.Value.LegalityDescription);
            }
            
            return options.Count > 0
                ? options.ToArray()
                : null;

            void ToggleDesignation(HuntingTool tool, string thisWhat)
            {
                Item? previousDesignation = inv?.AllItems.FirstOrDefault(tool.IsMyTool);

                string designate = (previousDesignation == item ? "Und" : "D") + "esignate";
                
                options.Add(new ContextMenuItem(
                    tool.Icon,
                    designate + " as " + tool.Name,
                    $"{(previousDesignation is not null && previousDesignation != item
                        ? "{i}(This will undesignate " + previousDesignation.Name + ".){/i}\n\n"
                        : null)}{designate} this {thisWhat} as your {tool.Name}, granting you special benefits when using it. Only one item can be designated at a time.",
                    () =>
                    {
                        if (tool.IsMyTool(item))
                            tool.UndesignateAsTool(item);
                        else
                            tool.DesignateAsTool(item);
                    }));
            }
        }));
        
        // Stat block display of hunting tools
        var generators = CreatureStatblock.CreatureStatblockSectionGenerators;
        if (generators.FindIndex(blockGen =>
                blockGen.Name == "Spellcasting")
            is {} casting)
            CreatureStatblock.CreatureStatblockSectionGenerators.Insert(
                casting,
                new CreatureStatblockSectionGenerator(
                    "Hunting tools",
                    self =>
                    {
                        if (HuntingTools.GetTools(self) is not {} tools
                            || tools.Count == 0)
                            return null;
                        tools.Sort((x, y) => x.Kind.CompareTo(y.Kind));
                        return string.Join("\n\n", tools.Select(tool =>
                            $"{{b}}{tool.Name}{{/b}} ({tool.Kind.ToStringOrTechnical().ToLower()} tool)\n    {tool.ShortDescription.Invoke(self, tool.AccessSpecialized).Replace("\n", "\n    ")}"));
                    }));
        
        // All hunting tools
        foreach (Feat ft in CreateHuntingTools())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> CreateHuntingTools()
    {
        yield return new HuntingTool(
                "Bloodseeking Blade",
                ToolId.BloodseekingBlade,
                ToolKind.Signature,
                ModData.Illustrations.BloodseekingBlade,
                (self, isSpecialized) =>
                $$"""
                {b}Bloody Fuller{/b} Against your quarry, you ignore an amount of {{(isSpecialized ? "{Blue}any{/Blue}" : "physical") }} resistance to this tool's damage equal to 1 + the number of weapon damage dice.
                {b}Reinforced{/b} Your first Strike with this tool deals {Blue}{{(self.Level >= 19 ? 3 : self.Level >= 11 ? 2 : 1)}}d6{/Blue} additional damage. The type is chosen from the reinforcing trophy.
                {b}Honed Strike {icon:TwoActions}{/b} [concentrate, relentless] Strike using this tool with a +2 circumstance bonus and ignore the Concealed condition.
                """
                + (isSpecialized ? "\n{b}Specialized{/b} You have {tooltip:criteffect}crit spec{/} with the tool. Gain the effects of a certain property rune when you Reinforce your Arsenal." : null),
                (
                    "simple or martial weapon",
                    item => item.HasAnyTraits([Trait.Simple, Trait.Martial])
                ))
            .ToSignatureToolFeat(
                "You have an even closer connection to your weapon than most slayers.",
                "You can designate a simple or martial weapon as a bloodseeking blade when you Reinforce your Arsenal, gaining the following benefits.",
                "{b}Bloody Fuller{/b} If your quarry is resistant to physical damage dealt by this tool, you ignore an amount of that resistance equal to 1 + the number of weapon damage dice.",
                "Your first Strike with this tool each turn deals 1d6 additional damage of one of the trophy’s damage types, chosen when you Reinforce your Arsenal. The extra damage increases to 2d6 at 11th level and to 3d6 at 19th level.",
                "{b}Honed Strike {icon:TwoActions}{/b} (concentrate, relentless) You center yourself and calm your breathing, then strike. Make a Strike with your bloodseeking blade signature tool. You gain a +2 circumstance bonus to your attack roll and ignore the target’s Concealed condition (but not the Hidden condition).",
                "You gain {tooltip:criteffect}critical specialization effects{/tooltip} with this tool. Bloody Fuller's ignored-resistances applies to all damage from this tool. You also gain an extra {i}fearsome{/i}, {i}returning{/i}, or {i}shifting{/i} rune, chosen when you Reinforce your Arsenal.")
            .WithOnCreature(self =>
            {
                int numDice = self.Level >= 19
                    ? 3
                    : self.Level >= 11
                        ? 2
                        : 1;
                
                // TODO: Initial Benefit
                // TODO: Reinforced Benefit
                
                QEffect bladeQf = new QEffect()
                {
                    Description =
                        $"Your first Strike with your bloodseeking blade (hit or miss) deals {{Blue}}{numDice}d6{{/Blue}} more damage.",
                    // Reinforced benefit
                    AddExtraKindedDamageOnStrike = (action, target) =>
                    {
                        if (action.Item is null)
                            return null;
                        if (action.Owner.Actions.ActionHistoryThisTurn.Any(act =>
                                act.HasTrait(Trait.Strike)))
                            return null;
                        if (HuntingTools.GetTool(action.Owner, HuntingTools.ToolId.BloodseekingBlade)
                                is not { } blade
                            || !blade.IsReinforced(action.Item))
                            return null;
                        return null;
                        return new KindedDamage(DiceFormula.FromText(numDice + "d6"), DamageKind.Fire);
                    },
                    ProvideStrikeModifier = item =>
                    {
                        if (!HuntingTools.GetTool(self, HuntingTools.ToolId.BloodseekingBlade)?.IsMyTool(item) ?? false)
                            return null;

                        CombatAction honedStrike = self.CreateStrike(item)
                            .WithActionCost(2)
                            .WithExtraTrait(Trait.Basic)
                            .WithExtraTrait(Trait.Concentrate)
                            .WithExtraTrait(ModData.Traits.Relentless)
                            .With(ca =>
                            {
                                ca.Illustration = new SideBySideIllustration(ca.Illustration, IllustrationName.TargetSheet);
                                ca.Description = StrikeRules.CreateBasicStrikeDescription2(ca.StrikeModifiers, "You gain a +2 circumstance bonus to the attack roll and ignore your target's Concealed condition (but not the Hidden condition).");
                                ca.StrikeModifiers.HuntersAim = true;
                                ca.StrikeModifiers.AdditionalBonusesToAttackRoll = [
                                    new Bonus(2, BonusType.Circumstance, "Honed strike")
                                ];
                            });
                        honedStrike.WithFullRename("Honed Strike");
                        honedStrike.ShortDescription += ", and ignore the Concealed condition";

                        return honedStrike;
                    }
                };

                self.AddQEffect(bladeQf);
            })
            .WithOnSheet(values =>
            {
                if (HuntingTools.GetTool(values, ToolId.BloodseekingBlade) is not { } blade)
                    return;
                /*values.AddSelectionOption(new SingleFeatSelectionOption(
                    "BloodseekingBladeDamage",
                    "Bloodseeking Blade Damage Type",
                    SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                    ft => 
                    ));*/
            });
    }
}