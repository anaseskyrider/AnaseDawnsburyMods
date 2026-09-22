using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass.TargetingRequirements;

public class IsARuneBearer(bool canBearAnyRune = false, bool includeDisabledRunes = false) : CreatureTargetingRequirement
{
    public const string UNUSABLE_WHY = "Not a rune-bearer";
    
    /// <summary>
    /// If true, then the creature need not bear the action owner's runes, just any runes.
    /// </summary>
    public bool CanBearAnyRune = canBearAnyRune;
    
    /// <summary>
    /// If true, then included disabled runes when looking for any runes.
    /// </summary>
    public bool IncludeDisabledRunes = includeDisabledRunes;
    
    public override Usability Satisfied(Creature source, Creature target)
    {
        return DrawnRune.IsARuneBearer(
            this.CanBearAnyRune ? null : source,
            target,
            this.IncludeDisabledRunes)
            ? Usability.Usable
            : Usability.NotUsableOnThisCreature(UNUSABLE_WHY);
    }
}