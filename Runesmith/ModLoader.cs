using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Display.Controls.Statblocks;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.RunesmithPlaytest;

public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ////////////////
        // Load Calls //
        ////////////////
        ModData.LoadData();
        ModItems.LoadItems();
        
        RunesmithClass.LoadClass();
        RunesmithArchetype.LoadArchetype();
        
        RunesmithRunes.LoadRunes();
        RunesmithFeats.CreateFeats();
        
        ////////////////////////
        // Modify Stat Blocks //
        ////////////////////////
        int abilitiesIndex = CreatureStatblock.CreatureStatblockSectionGenerators.FindIndex(gen => gen.Name == "Abilities");
        CreatureStatblock.CreatureStatblockSectionGenerators.Insert(abilitiesIndex,
            new CreatureStatblockSectionGenerator("Runic repertoire", CommonRuneRules.DescribeRunicRepertoire));

        // Update class language
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            Feat? runesmithClass = AllFeats.All.FirstOrDefault(ft => ft.FeatName == ModData.FeatNames.RunesmithClass);
            runesmithClass!.RulesText = runesmithClass.RulesText.Replace("Ability boosts", "Attribute boosts");

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
            }*/
        };
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