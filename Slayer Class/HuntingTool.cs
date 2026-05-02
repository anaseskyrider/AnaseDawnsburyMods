using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.SlayerClass;

/// <summary>
/// General structure for hunting tools from the slayer class. Each instance is a unique hunting tool of a known ID.
/// </summary>
public class HuntingTool
{
    /// <summary>
    /// The humanized name of the hunting tool.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// The FeatName of the hunting tool. Value is set on instance construction based on the <see cref="Name"/>.
    /// </summary>
    public FeatName FeatName { get; private set; }
    
    /// <summary>
    /// The illustration of the hunting tool.
    /// </summary>
    public Illustration Icon { get; set; }
    
    /// <summary>
    /// For a CREATURE'S stat block, and whether they HAVE SPECIALIZED ARSENAL, this DESCRIPTION will appear on their stat block.
    /// </summary>
    public Func<Creature,bool,string> ShortDescription { get; set; }
    
    /// <summary>
    /// The ToolId of the hunting tool.
    /// </summary>
    public HuntingTools.ToolId Id { get; set; }
    
    /// <summary>
    /// The ToolKind of the hunting tool. A FirstSignature gains its specialized arsenal benefits at level 7, while a SecondSignature gains them at level 15.
    /// </summary>
    public HuntingTools.ToolKind Kind { get; set; }

    /// <summary>
    /// Organizes information on what items can be legally designated as this hunting tool. LegalityDescription should be short and simple, and singular. E.g. "simple or martial weapon", "armor", etc. If null, then the hunting tool is linked to a specific item and cannot be changed, or otherwise doesn't take up inventory space, such as the "alchemist's toolkit" for the chymist's vials. 
    /// </summary>
    public (string LegalityDescription, Func<CalculatedCharacterSheetValues,Item,bool> ItemValidator)? LegalItem { get; set; }
    
    /// <summary>
    /// If true, then this tool's specialized arsenal benefits are known.
    /// </summary>
    public bool AccessSpecialized { get; set; }

    /// <summary>
    /// Generates a feat from a signature tool with a source-like description format.
    /// </summary>
    /// <seealso cref="WithBasicToolFeatFunctionality"/>
    /// <param name="toolFlavor">The tool's general flavor.</param>
    /// <param name="toolRules">The tool's general rules, such as what items you can designate it to.</param>
    /// <param name="initialRules">The initial benefits. Must begin with the name of the benefit in bold, such as "{b}Bloody Fuller{/b} If your quarry...".</param>
    /// <param name="reinforcedRules">The Reinforced in-line mechanic. Do not include any bolded feature names.</param>
    /// <param name="slayingRules">The slaying technique. Must begin with the action's name, cost, and traits.</param>
    /// <param name="specializedRules">The specialized arsenal. Do not include any bolded feature names.</param>
    /// <returns>The <see cref="Feat"/> which grants this tool.</returns>
    public Feat ToSignatureToolFeat(
        string toolFlavor,
        string toolRules,
        string initialRules,
        string reinforcedRules,
        string slayingRules,
        string specializedRules)
    {
        return WithBasicToolFeatFunctionality(new Feat(
            this.FeatName,
            toolFlavor,
            $$"""
              {{toolRules.TrimEnd('.')}}.

              {b}Initial Benefit—{/b}{{initialRules.TrimEnd('.')}}; {{ModData.Tooltips.ReinforcedBenefit("Reinforced")}} {{reinforcedRules.TrimEnd('.')}}.

              {b}Slaying Technique—{/b}{{slayingRules.TrimEnd('.')}}.

              {b}Specialized Arsenal (7th){/b} {{specializedRules.TrimEnd('.')}}.
              """, [], null));
    }

    /// <summary>
    /// Generates a class <see cref="TrueFeat"/> appropriate for secondary tools.
    /// </summary>
    /// <seealso cref="WithBasicToolFeatFunctionality"/>
    /// <param name="level">The feat's level.</param>
    /// <param name="flavorText">See: <see cref="Feat.FlavorText"/>.</param>
    /// <param name="rulesText">See: <see cref="Feat.RulesText"/>.</param>
    /// <param name="traits">See: <see cref="Feat.Traits"/>. Always includes the Slayer trait.</param>
    /// <returns></returns>
    public TrueFeat ToSecondaryToolFeat(int level, string? flavorText, string rulesText, List<Trait> traits)
    {
        if (!traits.Contains(ModData.Traits.Slayer))
        {
            traits.Add(ModData.Traits.Slayer);
            traits = traits
                .OrderBy(trait => trait.ToStringOrTechnical())
                .ToList();
        }
        
        TrueFeat secondaryFeat = (TrueFeat)WithBasicToolFeatFunctionality(
            new TrueFeat(this.FeatName, level, flavorText, rulesText, traits.ToArray()));
        
        return secondaryFeat;
    }

    /// <summary>
    /// Adds basic hunting tool feat functionality to a feat. The functionality of the tool must be created from subsequent WithOnSheet, WithPermanentQEffect, and/or WithOnCreature behavior.
    /// </summary>
    private Feat WithBasicToolFeatFunctionality(Feat toolFeat)
    {
        toolFeat.Traits.Add(ModData.Traits.HuntingTool);
        return toolFeat
            .WithIllustration(this.Icon)
            .WithTag(this)
            .WithOnSheet(values =>
            {
                this.AccessSpecialized = false; // Needed to avoid weird Free Play behavior
                HuntingTools.AddTool(values, this);
            });
    }

    /// <summary>
    /// Returns whether this <see cref="Item"/> is designated as this hunting tool instance.
    /// </summary>
    public bool IsMyTool(Item item)
    {
        return item.ItemModifications.Any(mod =>
            mod.Kind == HuntingTools.ToolDesignation
            && mod.Tag is string tagString
            && tagString == this.Id.ToStringOrTechnical());
    }

    /// <summary>
    /// Designate an item as this hunting tool instance. Removes all other designations, if any.
    /// </summary>
    public Item DesignateAsTool(Item item)
    {
        if (this.IsMyTool(item))
            return item;
        else
        {
            // Remove other tool designations from this item
            // An item can only be one Hunting Tool at a time
            foreach (ItemModification mod in item.ItemModifications
                         .Where(mod =>
                             mod.Kind == HuntingTools.ToolDesignation)
                         .ToList())
                item.WithoutModification(mod);
            return item.WithModification(
                ItemModification.Create("huntingToolDesignation_" + this.Id.ToStringOrTechnical()));
        }
    }

    /// <summary>
    /// Undesignate an item as this hunting tool instance.
    /// </summary>
    public Item UndesignateAsTool(Item item)
    {
        foreach (ItemModification mod in item.ItemModifications.ToList())
        {
            if (mod.Kind != HuntingTools.ToolDesignation)
                continue;
            if (mod.Tag is not string tagString)
                continue;
            if (tagString == this.Id.ToStringOrTechnical())
                item = item.WithoutModification(mod);
        }
        
        return item;
    }

    /// <summary>
    /// Constructs a new hunting tool. This isn't stored anywhere unless you turn it into a feat with one of the other methods.
    /// </summary>
    /// <param name="name">The name of the tool, such as "Bloodseeking Blade".</param>
    /// <param name="id">The tool's unique enumerated ID.</param>
    /// <param name="kind">Whether this is a signature tool or a secondary tool.</param>
    /// <param name="icon">The Illustration that represents this tool, seen in feats and tool selections and in some abilities that use the tool.</param>
    /// <param name="shortDescription">The summarized, multi-line description of this tool as it appears on a creature stat block.</param>
    /// <param name="legalItem">legalityDescription: The minimally-worded description of what items can be designated as this tool, such as in the sentence, "Designate this BLANK as your TOOL_NAME". The second parameter of this tuple is the function which enforces this validation.</param>
    public HuntingTool(
        string name,
        HuntingTools.ToolId id,
        HuntingTools.ToolKind kind,
        Illustration icon,
        Func<Creature,bool,string> shortDescription,
        (string legalityDescription, Func<CalculatedCharacterSheetValues,Item,bool> itemValidator)? legalItem)
    {
        this.Name = name;
        this.FeatName = ModManager.RegisterFeatName(
            ModData.IdPrepend + "HuntingTool." + this.Name.Replace(" ", ""),
            this.Name);
        this.Id = id;
        this.Kind = kind;
        this.Icon = icon;
        this.ShortDescription = shortDescription;
        this.LegalItem = legalItem;
    }
}