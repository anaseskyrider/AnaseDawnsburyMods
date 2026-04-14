using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications.Archetypes;

public static class Wrestler
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in LoadFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> LoadFeats()
    {
        // Elbow Breaker
        yield return new TrueFeat(
            ModData.FeatNames.ElbowBreaker,
            4,
            "You bend your opponent's body or limbs into agonizing positions that make it difficult for them to maintain their grip.",
            $$"""
              {b}Requirements{/b} You have a creature grabbed or restrained.

              Make an unarmed melee Strike against the creature you have grabbed or restrained. This Strike has the following additional effects.{{S.FourDegreesOfSuccess(
                  "You knock one held item out of the creature's grasp. It falls to the ground in the creature's space.",
                  "You weaken your opponent's grasp on one held item. Until the start of their next turn, attempts to Disarm them of that item gain a +2 circumstance bonus, and they take a -2 circumstance penalty to attacks with the item.",
                  null,
                  null)}}
              """,
            [])
          .WithAvailableAsArchetypeFeat(Trait.Wrestler)
          .WithActionCost(1)
          .WithPermanentQEffect(
              null,
              qfFeat =>
              {
                  qfFeat.WithDisplayActionInOffenseSection(
                      "Elbow Breaker",
                      "Make an unarmed melee Strike against a creature you have grabbed or restrained. A hit weakens their grasp on an item, and a crit knocks it out entirely.");
                  
                  qfFeat.ProvideStrikeModifier = item =>
                  {
                      if (!item.HasTrait(Trait.Unarmed))
                          return null;

                      StrikeModifiers mods = new StrikeModifiers()
                      {
                          OnEachTarget = async (caster, target, result) =>
                          {
                              if (result < CheckResult.Success)
                                  return;

                              List<Item> disarmables = target.HeldItems
                                  .Where(i => !i.HasTrait(Trait.Grapplee))
                                  .ToList();

                              if (disarmables.Count == 0)
                                  return;

                              Item? chosen;

                              if (disarmables.Count > 1)
                              {
                                  chosen = disarmables[(await caster.AskForChoiceAmongButtons(
                                          IllustrationName.GenericCombatManeuver,
                                          $$"""
                                            {b}Elbow Breaker{/b} {icon:Action}
                                            Choose an item to {{(result > CheckResult.Success ? "{Green}disarm{/Green}" : "{Blue}weaken the grasp of{/Blue}")}}.
                                            """,
                                          disarmables
                                              .Select(i =>
                                                  $"{i.Illustration.IllustrationAsIconString} {i.Name}")
                                              .ToArray()))
                                      .Index];
                              }
                              else
                                  chosen = disarmables[0];

                              if (result == CheckResult.CriticalSuccess)
                              {
                                  target.HeldItems.Remove(chosen);
                                  target.Occupies.DropItem(chosen);
                              }
                              else
                                  target.AddQEffect(new QEffect(
                                      "Weakened grasp",
                                      "Attempts to disarm you gain a +2 circumstance bonus, and your attacks with this item take a -2 circumstance penalty.",
                                      ExpirationCondition.ExpiresAtStartOfYourTurn,
                                      caster,
                                      IllustrationName.GenericCombatManeuver)
                                  {
                                      Key = "Weakened grasp",
                                      BonusToAttackRolls = (_, action, _) =>
                                          action.Item == chosen
                                              ? new Bonus(-2, BonusType.Circumstance, "Weakened grasp (Disarm)")
                                              : null,
                                      StateCheck = qfWeak =>
                                          qfWeak.Owner.Battle.AllCreatures.ForEach(cr =>
                                              cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                              {
                                                  BonusToAttackRolls = (_, action, defender) =>
                                                      defender == target
                                                      && action.ActionId == ActionId.Disarm
                                                          ? new Bonus(2, BonusType.Circumstance,
                                                              "Weakened grasp (Disarm)")
                                                          : null
                                              }))
                                  });
                          }
                      };

                      CombatAction strike = qfFeat.Owner.CreateStrike(item, -1, mods)
                          .WithDescription(StrikeRules.CreateBasicStrikeDescription2(
                              mods,
                              additionalSuccessText: "You knock one held item out of the creature's grasp. It falls to the ground in the creature's space.",
                              additionalCriticalSuccessText: "You weaken your opponent's grasp on one held item. Until the start of their next turn, attempts to Disarm them of that item gain a +2 circumstance bonus, and they take a -2 circumstance penalty to attacks with the item."))
                          .WithExtraTrait(Trait.Basic);
                      strike.WithFullRename("Elbow Breaker");
                      strike.Illustration = new SideBySideIllustration(
                          strike.Illustration,
                          IllustrationName.Grapple);
                      strike.Traits = new Traits([ModData.Traits.ModName, ..strike.Traits], strike);

                      ((CreatureTarget)strike.Target)
                          .WithAdditionalConditionOnTargetCreature((a, d) =>
                              d.QEffects.Any(qfFind =>
                                  qfFind.Id == QEffectId.Grappled
                                  && qfFind.Source == a)
                                  ? Usability.Usable
                                  : Usability.NotUsableOnThisCreature("Target is not grappled by you"))
                          .WithAdditionalConditionOnTargetCreature((_, d) =>
                              d.HeldItems.Any(i => !i.HasTrait(Trait.Grapplee))
                                  ? Usability.Usable
                                  : Usability.NotUsableOnThisCreature("No held items"));
                      return strike;
                  };
              });
    }
}