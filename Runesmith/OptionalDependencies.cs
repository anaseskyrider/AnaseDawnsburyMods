using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Mods.RunesmithClass.RuneRules;

namespace Dawnsbury.Mods.RunesmithClass;

/// <summary>
/// This class silos any references to optional assemblies, not calling them unless these mods are actually loaded by the user.
/// </summary>
public static class OptionalDependencies
{
    internal static void FinishEdifyingTrace(QEffect qfFeat)
    {
        qfFeat.AddToOffenseBlock = qfThis =>
            qfThis.Name!.WithTag("b") + " [flourish] Trace a Rune on an adjacent enemy, then Recall their Weakness. On a success, they take a -1 status penalty to your invocations.";

        qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenu) =>
        {
            if (submenu.SubmenuId != ModData.SubmenuIds.TraceRune)
                return null;

            CombatAction rwForShow = LoresAndWeaknesses.RecallWeakness.CreateRecallWeaknessAction(qfThis.Owner);
            
            CombatAction edify = new CombatAction(
                    qfThis.Owner,
                    new SideBySideIllustration(
                        ModData.Illustrations.TraceRune,
                        IllustrationName.NarratorBook),
                    "Edifying Trace",
                    [ModData.ModTrait, Trait.Flourish, Trait.Runesmith, Trait.ProxyAttack],
                    null!,
                    Target.AdjacentCreature()
                        .WithAdditionalConditionOnTargetCreature(
                            new EnemyCreatureTargetingRequirement())
                        .WithAdditionalConditionOnTargetCreature((a, d) =>
                        {
                            if (CombatAction.CreateSimple(a, "Concentrate, Manipulate", Trait.Concentrate, Trait.Manipulate)
                                    .CanBeginToUse(a) is { CanBeUsed: false } noSubActions)
                                return noSubActions;
                            if (rwForShow.CanBeginToUse(a) is { CanBeUsed: false } noRW)
                                return noRW;
                            return Usability.Usable;
                        }))
                .WithActionCost(1)
                .WithDescription(
                    "When you apply a rune to a foe, it reveals something about them to you.",
                    $$"""
                      {{ModData.FeatNames.TraceRune.ToLink("Trace a Rune")}} onto an adjacent enemy and then attempt a skill check to {{(ModLoader.RecallWeaknessFeat is not null ? ModLoader.RecallWeaknessFeat.Value.ToLink("Recall a Weakness") : "Recall a Weakness")}} on that target. If you succeed, you leverage this knowledge when you invoke any of your runes on that target; that target takes a –1 status penalty to saving throws against your invocations for the rest of the encounter.

                      If you use Edifying Trace on another enemy, the effect ends for the previous enemy.
                      """)
                .WithEffectOnEachTarget(async (action, self, target, _) =>
                {
                    if (await CommonRuneRules.TraceAnyRuneOnACreature(
                            self,
                            targetFilter: cr => cr == target,
                            canBeCanceled: true)
                        is null or CancelOption or PassViaButtonOption)
                    {
                        action.RevertRequested = true;
                        return;
                    }

                    CombatAction rw = LoresAndWeaknesses.RecallWeakness
                        .CreateRecallWeaknessAction(qfThis.Owner);

                    if (!await self.Battle.GameLoop
                            .FullCast(rw, ChosenTargets.CreateSingleTarget(target)))
                    {
                        action.Traits.Remove(Trait.Flourish);
                        self.Battle.Log("Edifying Trace converted to simple Trace a Rune.");
                    }
                    
                    // Remove existing effects
                    self.Battle.AllCreatures.ForEach(cr =>
                        cr.RemoveAllQEffects(qf =>
                            qf.Id == ModData.QEffectIds.EdifyingTraceEffect
                            && qf.Source == self));

                    if (rw.CheckResult < CheckResult.Success)
                        return;

                    // Apply effect on a success
                    QEffect edifyEffect = new QEffect(
                        "Edifying Trace",
                        $"You have a -1 status penalty to {self.ToColoredBoldedName()}'s invocations. This effect ends early if they use Edifying Trace again.")
                    {
                        Id = ModData.QEffectIds.EdifyingTraceEffect,
                        Source = self,
                        BonusToDefenses = (qfDebuff, defAction, def) =>
                            def.IsSavingThrow()
                            && defAction?.Owner == self
                            && defAction.HasTrait(ModData.Traits.Invocation)
                                ? new Bonus(-1, BonusType.Status, "Edifying Trace")
                                : null,
                    };
                    
                    target.AddQEffect(edifyEffect);
                });

            return new PossibilitySection("Edifying Trace")
            {
                Possibilities = [ new ActionPossibility(edify) ]
            };
        };
    }
}