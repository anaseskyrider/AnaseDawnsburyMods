using Dawnsbury.Core;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

public class RuneIdAttribute(string title, int level) : Attribute
{
    /// <summary>
    /// The rune's title, such as "Rune of Fire".
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// The rune's illustration.
    /// </summary>
    public IllustrationName Icon { get; } = IllustrationName.YellowWarning;

    /// <summary>
    /// The rune's base level.
    /// </summary>
    public int Level { get; } = level;

    public RuneIdAttribute(string title, int level, IllustrationName icon) : this(title, level)
    {
        Icon = icon;
        Level = level;
    }
}