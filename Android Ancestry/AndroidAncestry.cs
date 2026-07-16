using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.AndroidAncestry;

public static class AndroidAncestry
{
    public static void LoadAncestry()
    {
        List<Feat> androidFeatures = [..CreateFeatures()];
        List<Feat> androidHeritages = [..CreateHeritages()];
        
        foreach (Feat ft in androidFeatures)
            ModManager.AddFeat(ft);
        
        Feat androidAncestry = new AncestrySelectionFeat(
                ModData.FeatNames.AndroidAncestry,
                """
                Technological wonders from another world, androids have synthetic bodies and living souls. Their dual nature makes them quick-thinking and calm under pressure, but comfortable in stillness and solitude.

                Androids tend to be logical introverts, rational and contemplative. Insatiably curious, with an urge to understand themselves and the world around them, androids place great value on intellectual pursuits. They have difficulty interpreting and expressing emotions, both in themselves and in others, which makes them seem distant and uncaring. While androids can forge emotional bonds, they find it more difficult to connect with non-androids.
                """,
                [ModData.Traits.Android, Trait.Humanoid],
                8, 5,
                [new EnforcedAbilityBoost(Ability.Dexterity), new EnforcedAbilityBoost(Ability.Intelligence), new FreeAbilityBoost()],
                androidHeritages)
            .WithAbilityFlaw(Ability.Charisma)
            .WithSpecialRules(
                """
                {b}Constructed{/b} {i}Your synthetic body resists ailments better than those of purely biological organisms.{/i} You gain a +1 circumstance bonus to saving throws against diseases and poisons.

                {b}Emotionally Unaware{/b} {i}You find it difficult to understand and express complex emotions.{/i} You take a –1 circumstance penalty to Diplomacy and Performance checks.
                
                {b}Enhanced Senses{/b} {i}You have enhanced sensory constructions.{/i} You gain a +1 circumstance bonus to checks made to Seek.
                """)
            .WithOnSheet(values =>
            {
                // Grant Ancestry Features
                androidFeatures.ForEach(ft =>
                {
                    values.GrantFeat(ft.FeatName);
                });
            });
        ModManager.AddFeat(androidAncestry);
    }

    public static IEnumerable<Feat> CreateFeatures()
    {
        // Constructed
        yield return new Feat(
                ModData.FeatNames.Constructed,
                "Your synthetic body resists ailments better than those of purely biological organisms.",
                "You gain a +1 circumstance bonus to saving throws against diseases and poisons." /* and radiation */,
                [], null)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.AddToDefenseBlock = qfThis =>
                    "{b}Constructed.{/b} You have a +1 circumstance bonus to saving throws against diseases and poisons." /*and radiation*/;
                qfFeat.BonusToDefenses = (qfThis, action, def) =>
                {
                    if (def is not (Defense.Reflex or Defense.Fortitude or Defense.Will))
                        return null;

                    if (action == null || !action.HasTrait(Trait.Disease) && !action.HasTrait(Trait.Poison))
                        return null;

                    return new Bonus(1, BonusType.Circumstance, "constructed");
                };
            });

        // Emotionally Unaware
        yield return new Feat(
                ModData.FeatNames.EmotionallyUnaware,
                "You find it difficult to understand and express complex emotions.",
                "You take a –1 circumstance penalty to Diplomacy and Performance checks." /* and on Perception checks to Sense Motive.*/,
                [], null)
            .WithPermanentQEffect(
                "You have a –1 circumstance penalty to Diplomacy and Performance checks." /* and on Perception checks to Sense Motive.*/,
                qfFeat =>
                {
                    qfFeat.BonusToSkills = skill => skill is Skill.Diplomacy or Skill.Performance
                        ? new Bonus(-1, BonusType.Circumstance, "Emotionally Unaware")
                        : null;
                });
        
        // Enhanced Senses (alternative to Low-Light Vision)
        yield return new Feat(
                ModData.FeatNames.EnhancedSenses,
                "You have enhanced sensory anatomy.",
                "You gain a +1 circumstance bonus to checks made to Seek.",
                [], null)
            .WithPermanentQEffect(
                "You gain a +1 circumstance bonus to checks made to Seek.",
                qfFeat =>
                {
                    qfFeat.BonusToAttackRolls = (qfThis, seek, defender) =>
                    {
                        if (defender == null || seek.ActionId != ActionId.Seek)
                            return null;
                        int amount = 1;
                        return new Bonus(amount, BonusType.Circumstance, "Enhanced senses");
                    };
                });
    }

    public static IEnumerable<Feat> CreateHeritages()
    {
        // Artisan
        yield return new HeritageSelectionFeat(
                ModData.FeatNames.ArtisanHeritage,
                "Your body was originally designed to create works of art, complex tools, or maintain advanced machinery, giving you insight into weaknesses and flaws.",
                $$"""
                You become trained in Crafting (or another skill if you're already trained in Crafting).

                {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Modding{/b} If the {i}Lores and Weaknesses{/i} mod is installed, you also gain its Dubious Knowledge skill feat.
                """)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
                if (AllFeats.All.FirstOrDefault(ft => ft.ToTechnicalName().Contains("DubiousKnowledge")) is {} dubKnow)
                {
                    values.AddFeat(dubKnow, null);
                }
                else if (AllFeats.All.FirstOrDefault(ft => ft.CustomName == "In-depth Weakness") is {} idWeakness)
                {
                    values.AddFeat(idWeakness, null);
                }
            });
        
        // Deceiver
        yield return new HeritageSelectionFeat(
                ModData.FeatNames.DeceiverHeritage,
                "Your body was augmented with processes and an appearance intended to manipulate humans more easily.",
                $"You become trained in Deception (or another skill if you're already trained in Deception), and you gain the {FeatName.LengthyDiversion.ToLink("Lengthy Diversion")} skill feat.")
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Deception);
                values.GrantFeat(FeatName.LengthyDiversion);
            });
        
        // Laborer
        yield return new HeritageSelectionFeat(
                ModData.FeatNames.LaborerHeritage,
                "Your body is adapted to endure physical hardships or perform hard labor for long periods of time.",
                $$"""
                You become trained in Athletics (or another skill if you're already trained in Athletics).

                {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Modding{/b} If the {i}Bundle of Backgrounds{/i} mod is installed, you also gain its Hefty Hauler skill feat.
                """)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Athletics);
                if (ModManager.TryParse("Hefty Hauler", out FeatName hHauler))
                    values.GrantFeat(hHauler);
            });
        
        // Polymath (Polyglot)
        yield return new HeritageSelectionFeat(
                ModData.FeatNames.PolymathHeritage,
                "You were preprogrammed with a multitude of mathematical proficiencies, likely to act as a calculator.",
                $$"""
                You become trained in Society (or another skill if you're already trained in Society).

                {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Modding{/b} If the {i}Bundle of Backgrounds{/i} mod is installed, you also gain its Fount of Knowledge skill feat.
                """)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Society);
                if (ModManager.TryParse("Fount of Knowledge", out FeatName foKnowledge))
                {
                    values.GrantFeat(foKnowledge);
                }
            });
        
        // Warrior
        yield return new HeritageSelectionFeat(
                ModData.FeatNames.WarriorHeritage,
                "Your body was originally created to function as a security officer or soldier, making you a naturally gifted warrior preprogrammed for combat.",
                "You're trained in all simple and martial weapons.")
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Simple, Proficiency.Trained);
                values.SetProficiency(Trait.Martial, Proficiency.Trained);
            });
    }
}