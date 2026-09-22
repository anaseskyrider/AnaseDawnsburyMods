global using CommonShieldRules = Dawnsbury.Mods.MoreShields.CommonShieldRules;

using System.ComponentModel;
using System.Reflection;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass;

public static class ModLoader
{
    /*public static bool MoreShieldsIsLoaded { get; set; }*/

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ////////////////
        // Load Calls //
        ////////////////
        ModData.LoadData();
        //ModItems.LoadItems(); // No need for Artisan's Hammer
        
        Runesmith.LoadClass();
        RunesmithArchetype.LoadArchetype();
        
        AllRunes.LoadRunes();
        ClassFeats.LoadFeats();
        
        ////////////////////////
        // Modify Stat Blocks //
        ////////////////////////
        int abilitiesIndex = CreatureStatblock.CreatureStatblockSectionGenerators
            .FindIndex(gen => gen.Name == "Abilities");
        CreatureStatblock.CreatureStatblockSectionGenerators
            .Insert(
                abilitiesIndex,
                new CreatureStatblockSectionGenerator(
                    "Runic repertoire",
                    RunicRepertoireTag.DescribeRunicRepertoire));
        
        ////////////////////////////
        // Inventory Rune Etching //
        ////////////////////////////
        // TODO: Delayed refactorization until full release version of Runesmith.
        //InventoryContextMenu.Options.Add(CommonRuneRules.GetEtchRuneOptions());

        // Update class language
        /*LoadOrder.AtEndOfLoadingSequence += () =>
        {
            // Some colorful code I felt like messing with :)
            /*foreach (Feat ft in AllFeats.All)
            {
                if (ft is ClassSelectionFeat classSelect)
                {
                    string className = classSelect.Name + " feat";
                    
                    classSelect.RulesText = classSelect.RulesText
                        .Replace("trained in", "{Blue}trained{/Blue} in")
                        .Replace("{b}Trained{/b}", "{b}{Blue}Trained{/Blue}{/b}")
                        .Replace("expert in", "{DarkMagenta}expert{/DarkMagenta} in")
                        .Replace("{b}Expert{/b}", "{b}{DarkMagenta}Expert{/DarkMagenta}{/b}")
                        .Replace("master in", "{DarkGoldenrod}master{/DarkGoldenrod} in")
                        .Replace("legendary in", "{Firebrick}legendary{/Firebrick} in")
                        .Replace("Ability boosts", "{ForestGreen}Ability boosts{/ForestGreen}")
                        .Replace("Attribute boosts", "{ForestGreen}Attribute boosts{/ForestGreen}")
                        .Replace("Skill increase", "{CornflowerBlue}Skill increase{/CornflowerBlue}")
                        .Replace("skill increase", "{CornflowerBlue}skill increase{/CornflowerBlue}")
                        .Replace("Ancestry feat", "{Maroon}Ancestry feat{/Maroon}")
                        .Replace("ancestry feat", "{Maroon}ancestry feat{/Maroon}")
                        .Replace(className, "{SandyBrown}"+className+"{/SandyBrown}");
                }
            }#1#

            /*MoreShieldsIsLoaded = AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(a =>
                    a.GetName().Name?.Contains("MoreShields") ?? false);#1#
        };*/
    }

    extension(RuneId id)
    {
        /// <summary>
        /// Gets the word, such as "Atryl", of this rune.
        /// </summary>
        public string ToWord() => id.ToStringOrTechnical();

        /// <summary>
        /// Gets the title, such as "Rune of Fire", of this rune.
        /// </summary>
        public string ToTitle()
        {
            Type type = id.GetType();
            FieldInfo? fieldInfo = type.GetField(id.ToString());
            if (fieldInfo == null)
                return id.ToString();
            DescriptionAttribute? attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attribute == null ? id.ToString() : attribute.Description;
        }

        public string ToFullName()
        {
            string word = id.ToWord();
            string title = id.ToTitle();
            return word + (title.Contains("Diacritic") ? "-" : null) + ", " + title;
        }
    }
}

// Kept just in case.
/*Option runeOption = Option.ChooseCreature( // Add an option with this creature for its rune.
    thisRune.Name,
    crWithRune.Key,
    async () =>
    {
        await thisRune.InvocationBehavior.Invoke(action, thisRune, self,
            crWithRune.Key, runeQf);
        Sfxs.Play(SfxName.DazzlingFlash);
    })
    .WithIllustration(thisRune.Illustration);
options.Add(runeOption);*/


/* QEffect Properties to utilize
 * .Key     for anti-stacking behavior
 * .AppliedThisStateCheck
 * .Hidden
 * .HideFromPortrait
 * .Tag
 * .UsedThisTurn
 * .Value
 * .Source
 * .SourceAction
 * .Owner
 */

 
/* "You can now use the many new methods in the CommonQuestions class to add dialogue and other player interactivity choices." */