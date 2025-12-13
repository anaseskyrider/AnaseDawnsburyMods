using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
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
                [ModData.Traits.AndroidAncestry, Trait.Humanoid],
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
            .WithPermanentQEffect(
                "You have a +1 circumstance bonus to saving throws against diseases and poisons." /*and radiation*/,
                qfFeat =>
                {
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

                {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Modding{/b} If the {i}DawnniExpanded{/i} mod is installed, you also gain its In-depth Weakness general feat, even if you don't meet the prerequisites.
                """)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Crafting);
                if (AllFeats.All.FirstOrDefault(ft => ft.CustomName == "In-depth Weakness") is {} idWeakness)
                    //if (ModManager.TryParse("In-depth Weakness", out FeatName idWeakness))
                {
                    values.AddFeat(idWeakness, null);
                }
            });
        
        // Deceiver
        yield return new HeritageSelectionFeat(
                ModData.FeatNames.DeceiverHeritage,
                "Your body was augmented with processes and an appearance intended to manipulate humans more easily.",
                "You become trained in Deception (or another skill if you're already trained in Deception), and you gain the {link:LengthyDiversion}Lengthy Diversion{/} skill feat.")
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

    public static bool CanUseNanites(Creature cr)
    {
        return cr.QEffects.All(qf => (qf.Tag is QEffectId id ? id : QEffectId.Unspecified) != ModData.QEffectIds.NanitesUnusable);
    }

    /// <summary>
    /// Ask to use Nanite Surge reaction.
    /// </summary>
    /// <param name="reactor">The creature taking the reaction.</param>
    /// <param name="benefit">The QEffect to be applied if the reaction is successfully taken.</param>
    /// <param name="aboutToRollWhat">The string which follows immediately after "You're about to make a ". Variables should be "SKILL_NAME check", "SAVE_NAME saving throw", or "Attack roll".</param>
    /// <param name="gainWhatBenefit">The string which follows immediately after "Add a ". Variables should be something like "+2 status bonus".</param>
    /// <returns></returns>
    public static async Task<bool> AskToUseNanitesReaction(Creature reactor, QEffect benefit, string aboutToRollWhat, string gainWhatBenefit)
    {
        string question = $"{{b}}Nanite Surge {{icon:Reaction}}{{b}}\nYou're about to roll {aboutToRollWhat.WithIndefiniteArticle()}. Add a {gainWhatBenefit} to the roll?\n{{Red}}{{b}}Frequency{{/b}} once per combat.{{Red}}";

        if (!CanUseNanites(reactor))
            return false;
        
        if (!await reactor.Battle.AskToUseReaction(reactor, question, IllustrationName.ArcaneCascade)) 
            return false;
        
        // Use hidden action with traits to provoke reactions.
        CombatAction nanitePhantom = CombatAction.CreateSimple(reactor, "[Phantom nanite surge CombatAction]", ModData.Traits.AndroidAncestry, Trait.Concentrate, ModData.Traits.Nanites)
            .WithActionCost(0)
            .WithExtraTrait(Trait.DoNotShowInCombatLog)
            .WithExtraTrait(Trait.DoNotShowOverheadOfActionName)
            .WithExtraTrait(Trait.Basic)
            .WithSoundEffect(SfxName.Guidance)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                // Automatically has Target.Self() from helper function.
                caster.AddQEffect(benefit);
                caster.AddQEffect(new QEffect() 
                    { Id = ModData.QEffectIds.NaniteSurgeImmunity });
                caster.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtStartOfYourTurn)
                    { 
                        SpawnsAura = qfThis =>
                        {
                            if (PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.RemoveNaniteSurgeAura))
                                return null;
                            
                            return new MagicCircleAuraAnimation(IllustrationName.AngelicHaloCircle, Color.Gold, 2f)
                            {
                                MaximumOpacity = 0.25f
                            };
                        }
                    });
            });
        
        return await reactor.Battle.GameLoop.FullCast(nanitePhantom);
    }
}