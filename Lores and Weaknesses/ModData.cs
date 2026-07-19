using System;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.LoresAndWeaknesses;

public static class ModData
{
    public const string ID_PREPEND = "LoresAndWeaknesses.";

    public static Trait ModTrait;
    
        /// <summary>
    /// Loads all mod data. This should typically be called by a mod before anything else.
    /// </summary>
    /// <para>
    /// When registering mod data, certain data must be called through the execution of lines of code, rather than assigned in their initialization. The Initializer skips these data until they're first called, which can result in errors due to out of order registration calls (especially when another mod isn't using <see cref="ModManager.TryParse"/>).
    /// </para>
    /// <para>The following data forms are typically safe due to the way Dawnsbury Days loads mods (or because their initialization nearly always gets called before errors could arise): <see cref="FeatName"/>, <see cref="Illustration"/>, <see cref="Trait"/>, <see cref="SfxNames"/>, <see cref="SpellId"/>. Tooltips from <see cref="ModManager.RegisterInlineTooltip(string, string)"/> likely aren't safe to assign as part of the initializer, but they typically shouldn't be shared between mods either.
    /// </para>
    /// <para>
    /// In general, trigger the initializer by separating declaration and assignment for the following data forms:
    /// <list type="bullet">
    /// <item>All other enums (e.g. <see cref="ActionId"/>, <see cref="QEffectId"/>)</item>
    /// <item>Mod settings registered with <see cref="ModManager.RegisterBooleanSettingsOption"/></item>
    /// </list>
    /// </para>
    public static void LoadData()
    {
        ModTrait = ModManager.ModBeingLoadedTrait!.Value; // Known not null at this stage
    }

    public static class Traits
    {

        public static readonly Trait Lore = ModManager.RegisterTrait(
            ID_PREPEND + "Lore",
            new TraitProperties(
                "Lore",
                true,
                """
                A Lore skill represents knowledge on topics that are more specialized than a typical skill.
                
                They're primarily used with the {b}Recall Weakness{/b} {icon:Action} action: when targeting creatures appropriate for a Lore skill, a specific Lore grants a +5 bonus, while an unspecific Lore grants a +2 instead.
                """,
                false,
                Color.BurlyWood,
                false,
                true));
    }
}