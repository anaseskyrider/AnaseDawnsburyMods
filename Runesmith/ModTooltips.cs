using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class ModTooltips
{
    public static readonly Func<string, string> TraitRune = input => "{tooltip:Runesmith.Trait.Rune}"+input+"{/}";
    public static readonly Func<string, string> TraitInvocation = input => "{tooltip:Runesmith.Trait.Invocation}"+input+"{/}";
    public static readonly Func<string, string> ActionTraceRune = input => "{tooltip:Runesmith.Action.TraceRune}"+input+"{/}";
    public static readonly Func<string, string> ActionInvokeRune = input => "{tooltip:Runesmith.Action.InvokeRune}"+input+"{/}";
    public static readonly Func<string, string> ActionEtchRune = input => "{tooltip:Runesmith.Action.EtchRune}"+input+"{/}";
    public static readonly Func<string, string> FeatureTraditionSkill = input => "{tooltip:Runesmith.Features.TraditionSkill}"+input+"{/}";
    public static readonly Func<string, string> FeatureRunicCrafter = input => "{tooltip:Runesmith.Features.RunicCrafter}"+input+"{/}";
    public static readonly Func<string, string> FeatureRunicOptimization = input => "{tooltip:Runesmith.Features.RunicOptimization}"+input+"{/}";
    public static readonly Func<string, string> FeatureAssuredRunicCrafter = input => "{tooltip:Runesmith.Features.AssuredRunicCrafter}"+input+"{/}";
    public static readonly Func<string, string> FeatureSmithsEndurance = input => "{tooltip:Runesmith.Features.SmithsEndurance}"+input+"{/}";
    public static readonly Func<string, string> FeatsFortifyingKnock = input => "{tooltip:Runesmith.Feats.FortifyingKnock}"+input+"{/}";
    public static readonly Func<string, string> FeatsRunicTattoo = input => "{tooltip:Runesmith.Feats.RunicTattoo}"+input+"{/}";
    public static readonly Func<string, string> FeatsWordsFlyFree = input => "{tooltip:Runesmith.Feats.WordsFlyFree}"+input+"{/}";
    
    public static void RegisterTooltips()
    {
        ////////////
        // Traits //
        ////////////
        ModManager.RegisterInlineTooltip(
            "Runesmith.Trait.Rune",
            "{b}Rune{/b}\n{i}Trait{/i}\nVarious magical effects can be applied through runes, and they're affected by things which also affect spells. Runes can be applied via etching or tracing. Etched runes are applied outside of combat and last indefinitely, while traced runes last only until the end of your next turn. Their effects, however, are the same. Several abilities refer to creatures bearing one of your runes, known as rune-bearers: this is any creature who has one of your runes applied to its body or to any gear it is holding.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Trait.Invocation",
            "{b}Invocation{/b}\n{i}Trait{/i}\nAn invocation action allows a runesmith to surge power through a rune by uttering its true name. Invocation requires you to be able to speak clearly in a strong voice and requires that you be within 30 feet of the target rune or runes unless another ability changes this. The target rune then fades away immediately after the action resolves.");
        
        /////////////
        // Actions //
        /////////////
        ModManager.RegisterInlineTooltip(
            "Runesmith.Action.TraceRune",
            "{b}Trace Rune {icon:Action}–{icon:TwoActions}{/b}\n{i}Concentrate, Magical, Manipulate{i}\n(Requires a free hand)\nYou apply one rune to an adjacent target matching the rune's Usage description. The rune remains until the end of your next turn. If you spend {icon:TwoActions} two actions to Trace a Rune, you draw the rune in the air and it appears on a target within 30 feet. You can have any number of runes applied in this way.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Action.InvokeRune",
            "{b}Invoke Rune {icon:Action}{/b}\n{i}Invocation, Magical{i}\nYou utter the name of one or more of your runes within 30 feet. The rune blazes with power, applying the effect in its Invocation entry. The rune then fades away, its task completed.\n\nYou can invoke any number of runes with a single Invoke Rune action, but creatures that would be affected by multiple copies of the same specific rune are {Red}affected only once{/Red}, as normal for duplicate effects.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Action.EtchRune",
            "{b}Etch Rune{/b}\n{i}Out of combat ability{/i}\nAt the beginning of combat, you etch runes on yourself or your allies. Your etched runes remain until the end of combat, or until they're expended or removed. You can etch up to 2 runes, and you can etch an additional rune at levels 5, 9, 13, and 17.");
        
        ////////////////////
        // Class Features //
        ////////////////////
        ModManager.RegisterInlineTooltip(
            "Runesmith.Features.RunicCrafter",
            "{b}Runic Crafter{/b}\n{i}Level 2 Runesmith feature{/i}\nYour equipment gains the effects of the highest level fundamental armor and weapon runes for your level.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Features.RunicOptimization",
            "{b}Runic Optimization{/b}\n{i}Level 7 Runesmith feature{/i}\nYou deal an additional 2 damage with weapons and unarmed attacks in which you have expert proficiency. This damage increases to 3 if you're a master, and 4 if you're legendary.\n\nAt level 15, you gain Greater Runic Optimization, which increases these bonuses by 2.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Features.TraditionSkill",
            "{b}Traditions of Magic and Skills{/b}\nCertain skills belong to certain traditions of magic. The arcane tradition is related to the arcana skill. The divine tradition is related to the religion skill. The occult tradition is related to the occultism skill. The primal tradition is related to the nature skill.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Features.AssuredRunicCrafter",
            "{b}Assured Runic Crafter{/b}\n{i}Level 9 Runesmith feature{/i}\nYou can select one ally to gain the benefits of your Runic Crafter feature as a precombat preparation.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Features.SmithsEndurance",
            "{b}Smith's Endurance{/b}\n{i}Level 11 Runesmith feature{/i}\nYour proficiency rank for Fortitude saves increases to master. When you roll a success on a Fortitude save, you get a critical success instead.");
        
        /////////////////
        // Class Feats //
        /////////////////
        ModManager.RegisterInlineTooltip(
            "Runesmith.Feats.FortifyingKnock",
            "{b}Fortifying Knock {icon:Action}{/b}\n{i}Runesmith{/i}\n(Requires you to wield a shield and have a free hand)\n(Usable once per round)\nIn one motion, you Raise a Shield and Trace a Rune on your shield.");
        
        ModManager.RegisterInlineTooltip(
            "Runesmith.Feats.RunicTattoo",
            "{b}Runic Tattoo{b}\n{i}Runesmith{/i}\nChoose one rune you know, which you apply as a tattoo to your body. The rune is etched at the beginning of combat and doesn't count toward your maximum limit of etched runes. You can invoke this rune like any of your other runes, but once invoked, the rune fades significantly and is drained of power until your next daily preparations.");
         
        ModManager.RegisterInlineTooltip(
            "Runesmith.Feats.WordsFlyFree",
            "{b}Words, Fly Free {icon:Action}{/b}\n{i}Manipulate, Runesmith{/i}\n(Requires your Runic Tattoo isn't faded)\nYou fling your hand out, the rune from your Runic Tattoo flowing down it and flying through the air in a crescent. You trace the rune onto all creatures or objects within a 15-foot cone that match the rune's usage requirement. The rune then returns to you, faded.");
        
        //////////
        // Misc //
        //////////
        ModManager.RegisterInlineTooltip("Runesmith.Misc.AllAroundVision",
            "{b}All-Around Vision{/b}\n{i}(Monster Ability){/i}\nThis monster can see in all directions simultaneously and therefore can't be flanked.");
    }
}