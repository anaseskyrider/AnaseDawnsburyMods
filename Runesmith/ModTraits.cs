using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public class ModTraits
{
    public static Trait Runesmith = ModManager.RegisterTrait(
        "Runesmith", 
        new TraitProperties("Runesmith", true) {
            IsClassTrait = true
        });
    
    public static Trait RunicRepertoire = ModManager.RegisterTrait(
        "Runic Repertoire",
        new TraitProperties("Runic Repertoire", false));
    
    public static Trait Rune = ModManager.RegisterTrait(
        "Rune",
        new TraitProperties("Rune", true,
            "A runesmith doesn't cast spells, but they can apply various magical effects through runes. Runes can be applied via etching or tracing. Etched runes are applied outside of combat and last indefinitely, while traced runes last only until the end of your next turn. Their effects, however, are the same. Several abilities refer to creatures bearing one of your runes, known as rune-bearers: this is any creature who has one of your runes applied to its body or to any gear it is holding.",
            relevantForShortBlock: true)
        {
            RelevantOnlyForClass = Runesmith
        }
    );
    public static Trait Traced = ModManager.RegisterTrait(
        "Traced",
        new TraitProperties("Traced", true,
            "A traced rune is drawn lightly in dust, light, or a similar fleeting medium. A runesmith can trace runes with the Trace Rune action, and it remains until the end of their next turn.",
            relevantForShortBlock: true)
        {
            RelevantOnlyForClass = Runesmith
        }
    );
    public static Trait Etched = ModManager.RegisterTrait(
        "Etched",
        new TraitProperties("Etched", true,
            "An etched rune is carved, inked, or branded in. A runesmith's magic can sustain up to 2 etched runes at a time, or more at higher levels. Etched runes remain indefinitely until they're expended or removed.", //"Runes are etched before combat begins."
            relevantForShortBlock: true)
        {
            RelevantOnlyForClass = Runesmith
        }
    );
    /// <summary>
    /// An action with this trait represents an invocation action. A QEffect with this trait represents the effects of an invocation, as opposed to an effect that is the result of a rune being applied.
    /// </summary>
    public static Trait Invocation = ModManager.RegisterTrait(
        "Invocation",
        new TraitProperties("Invocation", true,
            "An invocation action allows a runesmith to surge power through a rune by uttering its true name. Invocation requires you to be able to speak clearly in a strong voice and requires that you be within 30 feet of the target rune or runes unless another ability changes this. The target rune then fades away immediately after the action resolves.",
            relevantForShortBlock: true)
        {
            RelevantOnlyForClass = Runesmith
        }
    );
    /// <summary>
    /// A technical <see cref="Trait"/> which is used for QEffects representing immunity to a specific rune, used in conjunction with a RuneId trait like <see cref="Atryl"/>.
    /// </summary>
    public static Trait InvocationImmunity = ModManager.RegisterTrait(
        "InvocationImmunity",
        new TraitProperties("InvocationImmunity", false)
        {
            RelevantOnlyForClass = Runesmith
        }
    );
    public static Trait Diacritic = ModManager.RegisterTrait(
        "Diacritic",
        new TraitProperties("Diacritic", true,
            "A diacritic is a special type of rune that is not applied directly to a creature or object, but rather to another rune itself, modifying or empowering that base rune. A diacritic can never be applied by itself, and any effect that would remove or invoke the base rune always also removes or invokes the diacritic rune. A rune can have only one diacritic.",
            relevantForShortBlock: true)
        {
            RelevantOnlyForClass = Runesmith
        }
    );
    
    // Rune-specific traits. Every rune is granted a trait unique to its type of instance.
    public static Trait Atryl = ModManager.RegisterTrait(
        "Atryl",
        new TraitProperties("Atryl", false)
    );
    
    public static Trait Esvadir = ModManager.RegisterTrait(
        "Esvadir",
        new TraitProperties("Esvadir", false)
    );
    
    public static Trait Holtrik = ModManager.RegisterTrait(
        "Holtrik",
        new TraitProperties("Holtrik", false)
    );
    
    public static Trait Marssyl = ModManager.RegisterTrait(
        "Marssyl",
        new TraitProperties("Marssyl", false)
    );
    
    public static Trait Oljinex = ModManager.RegisterTrait(
        "Oljinex",
        new TraitProperties("Oljinex", false)
    );
    
    public static Trait Pluuna = ModManager.RegisterTrait(
        "Pluuna",
        new TraitProperties("Pluuna", false)
    );
    
    public static Trait Ranshu = ModManager.RegisterTrait(
        "Ranshu",
        new TraitProperties("Ranshu", false)
    );
    
    public static Trait SunDiacritic = ModManager.RegisterTrait(
        "Sun-",
        new TraitProperties("Sun-", false)
    );
    
    public static Trait UrDiacritic = ModManager.RegisterTrait(
        "Ur-",
        new TraitProperties("Ur-", false)
    );
    
    public static Trait Zohk = ModManager.RegisterTrait(
        "Zohk",
        new TraitProperties("Zohk", false)
    );
    
    // Level 9 rune traits, for a future update.
    
    // Level 17 rune traits, for a future update.
}