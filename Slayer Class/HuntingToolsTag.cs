using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Creatures;

namespace Dawnsbury.Mods.SlayerClass;

/// <summary>
/// Organizes information related to knowing hunting tools.
/// </summary>
/// <remarks>Each slayer has an instance of this tag, giving them instanced information about their access to their tools.</remarks>
public class HuntingToolsTag
{
    /// <summary>
    /// Character sheet tag key which contains the list of tools known.
    /// </summary>
    public const string TOOLS_KNOWN_KEY = "HUNTING_TOOLS_KNOWN";
    
    /// <summary>
    /// The tools known on this creature.
    /// </summary>
    public readonly List<HuntingTool> KnownTools = [];

    /// <summary>
    /// The tools that the creature has the Specialized Arsenal benefits of.
    /// </summary>
    public readonly List<HuntingTool> SpecializedArsenal = [];

    #region Instance Functions

    public void AddKnownTool(HuntingTool tool)
    {
        if (!KnownTools.Contains(tool))
            KnownTools.Add(tool);
    }

    public bool IsKnown(HuntingTool tool)
    {
        return KnownTools.Contains(tool);
    }

    public bool IsKnown(HuntingTools.ToolId toolId)
    {
        return KnownTools.Any(tool => tool.Id == toolId);
    }

    public HuntingTool? GetTool(HuntingTools.ToolId toolId)
    {
        return KnownTools.FirstOrDefault(tool => tool.Id == toolId);
    }

    public void AddSpecialized(Func<HuntingTool, bool> firstValidTool)
    {
        HuntingTool? firstTool = KnownTools.FirstOrDefault(firstValidTool);
        if (firstTool is not null)
            AddSpecialized(firstTool);
    }

    public void AddSpecialized(Func<HuntingToolsTag, HuntingTool, bool> firstValidTool)
    {
        HuntingTool? firstTool = KnownTools.FirstOrDefault(tool => firstValidTool(this, tool));
        if (firstTool is not null)
            AddSpecialized(firstTool);
    }

    public void AddSpecialized(HuntingTool? tool)
    {
        if (tool is null)
            return;
        if (!SpecializedArsenal.Contains(tool))
            SpecializedArsenal.Add(tool);
    }

    public void AddSpecialized(HuntingTools.ToolId toolId)
    {
        if (GetTool(toolId) is { } tool)
            if (!SpecializedArsenal.Contains(tool))
                SpecializedArsenal.Add(tool);
    }

    public bool IsSpecialized(HuntingTool tool)
    {
        return SpecializedArsenal.Contains(tool);
    }

    public bool IsSpecialized(HuntingTools.ToolId toolId)
    {
        return SpecializedArsenal.Any(tool => tool.Id == toolId);
    }

    #endregion

    #region Static Functions

    public static HuntingToolsTag AddKnownTool(CalculatedCharacterSheetValues values, HuntingTool tool)
    {
        HuntingToolsTag tag = GetTag(values) ?? new HuntingToolsTag();
        tag.AddKnownTool(tool);
        values.Tags.TryAdd(TOOLS_KNOWN_KEY, tag);
        return tag;
    }

    public static HuntingToolsTag? GetTag(Creature slayer)
    {
        return slayer.PersistentCharacterSheet is not null
            ? GetTag(slayer.PersistentCharacterSheet.Calculated)
            : null;
    }

    public static HuntingToolsTag? GetTag(CalculatedCharacterSheetValues values)
    {
        return values.Tags.TryGetValueAs(TOOLS_KNOWN_KEY, out HuntingToolsTag? tools)
            ? tools
            : null;
    }

    public static HuntingTool? GetTool(Creature slayer, HuntingTools.ToolId toolId)
    {
        return slayer.PersistentCharacterSheet is null
            ? null
            : GetTool(slayer.PersistentCharacterSheet.Calculated, toolId);
    }

    public static HuntingTool? GetTool(CalculatedCharacterSheetValues values, HuntingTools.ToolId toolId)
    {
        return GetTag(values)?.GetTool(toolId);
    }

    public static List<HuntingTool>? GetTools(Creature slayer)
    {
        return slayer.PersistentCharacterSheet is null
            ? null
            : GetTools(slayer.PersistentCharacterSheet.Calculated);
    }

    public static List<HuntingTool>? GetTools(CalculatedCharacterSheetValues values)
    {
        return GetTag(values)?.KnownTools.ToList();
    }

    #endregion
}