using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

//using Dawnsbury.Mods.DawnniExpanded;

namespace Dawnsbury.Mods.MoreDedications.Archetypes;

public static class Archer
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Quick Shot: Add Quick Draw to Archer Dedication
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.QuickDraw, Trait.Archer, 4);
        
        // Crossbow Terror
        // DEPRECATED (remaster)
        // Improve existing feat. Add damage die stacking prevention, shorten stat block description.
        TrueFeat cbTerror = (AllFeats.GetFeatByFeatName(FeatName.CrossbowTerror) as TrueFeat)!;
        cbTerror.RulesText += " As normal, this damage die increase can't be combined with other abilities that alter the weapon damage die (such as the ranger feat Crossbow Ace).";
        cbTerror.WithOnCreature(self =>
        {
            QEffect? qfTerror = self.QEffects.FirstOrDefault(qf =>
                (qf.Name?.Contains("Crossbow Terror") ?? false)
                && qf.IncreaseItemDamageDie is not null);
            qfTerror?.Description = "You have a +2 circumstance bonus to damage with crossbows, and increment the damage die of simple crossbows by one step.";
            qfTerror?.IncreaseItemDamageDie = (qfThis, item) =>
            {
                if (!item.HasTrait(Trait.Crossbow) || !item.HasTrait(Trait.Simple))
                    return false;
                    
                foreach (QEffect qfInLoop in qfThis.Owner.QEffects)
                {
                    if (qfInLoop != qfThis
                        && qfInLoop.IncreaseItemDamageDie?.Invoke(qfInLoop, item) == true)
                        return false;
                }
                return true;
            };
        });
        
        // Parting Shot
        yield return new TrueFeat(
                ModData.FeatNames.FighterPartingShot,
                4,
                "You jump back and fire a quick shot that catches your opponent off guard.",
                """
                {b}Requirements{/b} You are wielding a loaded ranged weapon or a ranged weapon without reload 1 or reload 2.

                You Step and then make a ranged Strike with the required weapon. Your target is flat-footed against the attack.
                """,
                [Trait.Fighter])
            .WithActionCost(2)
            .WithPermanentQEffect(
                "You jump back and fire a quick shot that catches your opponent off guard.",
                qfFeat =>
                {
                    // TODO: Refactor clunky strike modifier for modernity.
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        if (!item.HasTrait(Trait.Ranged)
                            || ((item.HasTrait(Trait.Reload1)
                                 || item.HasTrait(Trait.Reload2))
                                && item.EphemeralItemProperties.NeedsReload))
                            return null;
                        CombatAction basicStrike = qfFeat.Owner.CreateStrike(item).WithActionCost(0);
                        CombatAction partingShot = new CombatAction(
                                qfFeat.Owner,
                                new SideBySideIllustration(IllustrationName.Walk, item.Illustration),
                                "Parting Shot",
                                [ModData.Traits.ModName, Trait.Fighter, Trait.Basic],
                                StrikeRules.CreateBasicStrikeDescription3(basicStrike.StrikeModifiers, additionalAttackRollText: "You Step before you Strike. Your target is flat-footed against the attack."),
                                Target.Self())
                            .WithActionCost(2)
                            .WithSoundEffect(SfxName.Footsteps)
                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (action, caster, _) =>
                            {
                                if (!await caster.StepAsync("Choose where to Step with Parting Shot.", allowCancel: true, allowPass: true))
                                {
                                    action.RevertRequested = true;
                                }
                                else
                                {
                                    QEffect temporarilyFlatFooted = new QEffect()
                                    {
                                        IsFlatFootedTo = (_, attacker, _) =>
                                            attacker != caster ? null : "Parting Shot" 
                                    }.WithExpirationNever();
                                    caster.Battle.AllCreatures.ForEach(cr => cr.AddQEffect(temporarilyFlatFooted));
                                    await caster.Battle.GameLoop.FullCast(basicStrike);
                                    caster.Battle.AllCreatures.ForEach(cr => cr.RemoveAllQEffects(qfToRemove => qfToRemove == temporarilyFlatFooted));
                                }
                            })
                            .WithTargetingTooltip((power, target, index) =>
                                power.Description);
                        
                        return partingShot;
                    };
                });
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.FighterPartingShot, Trait.Archer, 6);
        
        // TODO: Staggering Fire (lv6)
        
        // TODO: Custom tooltip text to pre-apply blindfight
        // Archer's Aim
        yield return new TrueFeat(
                ModData.FeatNames.ArchersAim,
                8,
                "You slow down, focus, and take a careful shot.",
                "Make a ranged Strike with a weapon in the bow weapon group. You gain a +2 circumstance bonus to the attack roll and ignore the target's concealed condition. If the target is hidden, reduce the flat check from being hidden from 11 to 5.",
                [Trait.Concentrate])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(Trait.Archer)
            .WithPermanentQEffect(
                "You can make a careful shot.",
                qfFeat =>
                {
                    const string actionName = "Archer's Aim";
                    qfFeat.ProvideStrikeModifier = item =>
                    {
                        if (!item.HasTrait(Trait.Ranged) || !item.HasTrait(Trait.Bow))
                            return null;
                        
                        StrikeModifiers newMods = new StrikeModifiers()
                        {
                            HuntersAim = true,
                            AdditionalBonusesToAttackRoll = [new Bonus(2, BonusType.Circumstance, "Archer's Aim")],
                            // Remove BlindFight after strike is made.
                            OnEachTarget = async (attacker, _, _) =>
                            {
                                attacker.RemoveAllQEffects(qf => qf.Tag is actionName);
                            },
                        };
                        
                        CombatAction strike = qfFeat.Owner.CreateStrike(item, -1, newMods)
                            .WithName(actionName)
                            .WithDescription(StrikeRules.CreateBasicStrikeDescription2(
                                newMods,
                                """
                                You gain a +2 circumstance bonus to the attack roll, ignore the target's concealed condition, and reduce flat checks due to hidden to 5.

                                (NOTE: Accuracy preview against hidden creatures doesn't use a lower DC.)
                                """))
                            .WithActionCost(2)
                            // Apply BlindFight before strike is made.
                            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (_, self, _) =>
                            {
                                self.AddQEffect(new QEffect() { Id = QEffectId.BlindFight, Tag = actionName});
                            });
                        strike.Illustration = new SideBySideIllustration(
                            strike.Illustration, IllustrationName.TargetSheet);
                        strike.Traits = new Traits([ModData.Traits.ModName, ..strike.Traits], strike);
                        
                        return strike;
                    };
                });
        
        // PETR: Mobile Shot Stance (too high level for this archetype at this time)
        
        /* Higher level feats
         * @10 (really: 8) Mobile Shot Stance
         * @10 (ORC) Unobstructed Shot.
         * @18 (really: 16) Multishot Stance
         */
    }
}