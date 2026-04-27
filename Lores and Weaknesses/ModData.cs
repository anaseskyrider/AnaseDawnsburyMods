using System;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.LoresAndWeaknesses;

public static class ModData
{
    public const string IdPrepend = "LoresAndWeaknesses.";

    /// <summary>
    /// Registers the source enum to the game, or returns the original if it's already registered.
    /// </summary>
    /// <param name="technicalName">The technicalName string of the enum being registered.</param>
    /// <param name="displayName">The human readable name of the enum, if the type supports a humanized name.</param>
    /// <typeparam name="T">The enum being registered to.</typeparam>
    /// <returns>The newly registered enum.</returns>
    public static T SafelyRegister<T>(string technicalName, string? displayName = null) where T : struct, Enum
    {
        return (T)(ModManager.TryParse(technicalName, out T alreadyRegistered)
            ? alreadyRegistered
            : typeof(T) == typeof(FeatName)
                ? (Enum)ModManager.RegisterFeatName(technicalName, displayName)
                : ModManager.RegisterEnumMember<T>(technicalName));
    }

    public static class Traits
    {
        public static readonly Trait ModName = ModManager.RegisterModNameTrait(
            "LoresAndWeaknesses",
            "Lores and Weaknesses");

        public static readonly Trait Lore = ModManager.RegisterTrait(
            IdPrepend + "Lore",
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