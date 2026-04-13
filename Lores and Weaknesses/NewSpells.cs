using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.LoresAndWeaknesses;

public static class NewSpells
{
    public static SpellId Hypercognition { get; set; }
    
    public static void Load()
    {
        Hypercognition = ModManager.RegisterNewSpell(
            "Hypercognition",
            3,
            (id, caster, rank, inCombat, info) =>
            {
                return Spells.CreateModern(
                        IllustrationName.TrueSeeing,
                        "Hypercognition",
                        [ModData.Traits.ModName, Trait.Concentrate, Trait.Divination, Trait.Occult, Trait.VerbalOnly, Trait.DoesNotRequireAttackRollOrSavingThrow],
                        "You rapidly catalog and collate information relevant to your current situation.",
                        $"You can instantly use up to 6 {RecallWeakness.GetActionLink()} actions as part of Casting this Spell. For these actions, you can't use any special abilities, reactions, or free actions that trigger when you Recall a Weakness.",
                        Target.Self()
                        /*Target.MultipleCreatureTargets(
                            6,
                            () =>
                            {
                                Proficiency? perception = caster?.Proficiencies.Get(Trait.Perception);
                                bool hasGlance = caster?.PersistentCharacterSheet?.Calculated.AllFeats.Any(ft =>
                                    ft is { FeatName: FeatName.CustomFeat, Name: RecallWeakness.SlightestGlanceWeaknessId }) ?? false;
                                // 30 ft normally, 60 with glance, 120 with glance and expert perception
                                int range = hasGlance
                                    ? perception > Proficiency.Expert 
                                        ? 24 // 120 feet
                                        : 12 // 60 feet
                                    : 6; // 30 feet
            
                                CreatureTarget crTar = RecallWeakness.RecallWeaknessTarget(
                                    range,
                                    hasGlance && perception >= Proficiency.Legendary);

                                return crTar;
                            })*/,
                        rank,
                        null)
                    .WithActionCost(1)
                    .WithSoundEffect(SfxName.Mental)
                    .WithEffectOnEachTarget(async (spell, caster2, target, _) =>
                    {
                        for (int i=0; i<6; i++)
                        {
                            CombatAction recall = RecallWeakness.CreateRecallWeaknessAction(caster2)
                                .WithActionCost(0)
                                .WithActionId(ActionId.None) // Reduce triggers
                                .WithExtraTrait(Trait.DoNotShowOverheadOfActionName); // Reduce spam
                            recall.WithFullRename("Hypercognition"); // Reduce triggers

                            List<Option> options = [];
                            GameLoop.AddDirectUsageOnCreatureOptions(recall, options, true);

                            Option chosen;
                            
                            if (i == 0)
                                options.Add(new CancelOption(true));

                            if (i == 0 && options.Count == 1)
                                chosen = options[0];
                            else
                                chosen = (await caster2.Battle.SendRequest(
                                    new AdvancedRequest(caster2, "Choose target for Recall Weakness.", options)
                                    {
                                        TopBarText = $"Choose target for Recall Weakness{(i == 0 ? " or right-click to cancel" : null)}. ({i + 1}/6)",
                                        TopBarIcon = spell.Illustration
                                    })).ChosenOption;
                            
                            await chosen.Action();
                            
                            if (chosen is CancelOption && i == 0)
                            {
                                spell.RevertRequested = true;
                                break;
                            }
                        }
                    });
            });
    }
}