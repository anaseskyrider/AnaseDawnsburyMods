using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Specific;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreDedications.Archetypes;

public static class FamiliarMaster
{
    public static void LoadArchetype()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Lv2: Dedication Feat
        Feat familiarMasterDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.FamiliarMasterArchetype,
                "You have forged a mystical bond with a creature. This might have involved complex rituals and invocations, such as meditating under the moon until something crept out of the forest. Or maybe you just did each other a good turn, such as rescuing the beast from a trap or a foe, and then being rescued in turn.",
                "You gain a {link:ClassFamiliar}combat familiar{/}. If you already have one, you gain the {link:DawnsburyEnhancedFamiliar}Enhanced Familiar{/} feat.")
            .WithOnSheet(values =>
            {
                values.GrantFeat(values.HasFeat(FeatName.ClassFamiliar)
                    ? FeatName.DawnsburyEnhancedFamiliar
                    : FeatName.ClassFamiliar);
            });
        familiarMasterDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        yield return familiarMasterDedication;
        
        // Lv4: Add Enhanced Familiar to Familiar Master
        yield return ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            FeatName.DawnsburyEnhancedFamiliar, ModData.Traits.FamiliarMasterArchetype, 4);

        // Lv4: Overload Familiar (homebrew ability, vaguely replacing Familiar Conduit)
        yield return new TrueFeat(
                ModData.FeatNames.OverloadFamiliar,
                4,
                "Your familiar's connection to magic can be tapped into for an explosive release.",
                """
                {b}Frequency{/b} once per day
                
                Each creature in a 5-foot emanation takes 2d8 Force damage (basic Fortitude save mitigates). The DC is your class DC or spell DC, whichever is higher. The damage increases by 1d8 at 5th level and every 2 levels thereafter.
                
                {b}Special{/b} This ability counts as a familiar action.
                """,
                [ModData.Traits.MoreDedications, Trait.Homebrew, Trait.Concentrate, Trait.Force, Trait.Magical])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.FamiliarMasterArchetype)
            .WithOnCreature((sheet, cr) =>
            {
                if (!sheet.Tags.TryGetValue("CombatFamiliar", out object? obj)
                    || obj is not FamiliarTag fTag)
                    return;
                
                if (!cr.HasEffect(QEffectId.FamiliarAbility))
                    cr.AddQEffect(new QEffect()
                    {
                        Id = QEffectId.FamiliarAbility,
                        ProvideMainAction = qfSelf =>
                        {
                            if (qfSelf.UsedThisTurn)
                                return null;
                            return new SubmenuPossibility(
                                fTag.IllustrationOrDefault,
                                fTag.FamiliarName ?? "Familiar")
                            {
                                Subsections = {
                                    new PossibilitySection("Familiar action")
                                    {
                                        PossibilitySectionId = PossibilitySectionId.FamiliarAbility
                                    }
                                }
                            }.WithPossibilityGroup("Abilities");
                        }
                    });

                int numDice = (int)Math.Ceiling((double)cr.Level / 2);
                
                cr.AddQEffect(new QEffect(
                    "Overload Familiar",
                    $"As {{icon:TwoActions}}two familiar actions, once per day, your familiar explodes in 5-foot emanation for {S.HeightenedVariable(numDice, 2)}d8 Force damage (basic Fortitude save).")
                {
                    ProvideActionIntoPossibilitySection = (qfThis, section) =>
                    {
                        if (section.PossibilitySectionId != PossibilitySectionId.FamiliarAbility)
                            return null;
                        
                        if (cr.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.OverloadFamiliar))
                            return null;

                        return (ActionPossibility)new CombatAction(
                                cr,
                                IllustrationName.LightningStorm,
                                "Overload Familiar",
                                [Trait.Basic, Trait.Concentrate, Trait.Force, Trait.Magical],
                                $$"""
                                   {i}Your familiar's connection to magic can be tapped into for an explosive release.{/i}

                                   {b}Frequency{/b} once per day

                                   Each creature in a 5-foot emanation takes {{S.HeightenedVariable(numDice, 2)}}d8 Force damage (basic Fortitude save mitigates).
                                   """,
                                Target.SelfExcludingEmanation(1))
                            .WithActionCost(2)
                            .WithSavingThrow(new SavingThrow(
                                Defense.Fortitude,
                                cr.ClassOrSpellDC()))
                            .WithSoundEffect(SfxName.FieryBurst)
                            .WithProjectileCone(VfxStyle.BasicProjectileCone(IllustrationName.LightningStorm))
                            .WithEffectOnEachTarget(async (action, _, target, result) =>
                            {
                                if (cr.FindQEffect(QEffectId.FamiliarAbility) is not {} abilityQf)
                                    return;
                                
                                abilityQf.UsedThisTurn = true;
                                cr.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.OverloadFamiliar);
                                
                                await CommonSpellEffects.DealBasicDamage(
                                    action, cr, target, result,
                                    numDice+"d8",
                                    DamageKind.Force);
                            });
                    }
                });
            });
        
        // Lv6: Fast Command (homebrew ability)
        yield return new TrueFeat(
                ModData.FeatNames.FastCommand,
                6,
                "A spark of strong connection can allow you to command your familiar with a mere thought.",
                """
                {b}Frequency{/b} once per day

                The next familiar action you take this turn costs 1 fewer action.
                """,
                [ModData.Traits.MoreDedications, Trait.Homebrew, Trait.Concentrate])
            .WithAvailableAsArchetypeFeat(ModData.Traits.FamiliarMasterArchetype)
            .WithActionCost(0)
            .WithPermanentQEffect(
                "Once per day, the next familiar action you take this turn costs 1 fewer action.",
                qfFeat =>
                {
                    qfFeat.ProvideSectionIntoSubmenu = (qfThis, submenuOuter) =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources.UsedUpActions
                            .Contains(ModData.PersistentActions.FastCommand))
                            return null;
                        
                        if (qfThis.Owner.PersistentCharacterSheet?.Calculated.Tags[Familiars.FAMILIAR_KEY]
                            is not FamiliarTag fTag)
                            return null;
                        
                        if (submenuOuter.Caption != (fTag.FamiliarName ?? "Familiar"))
                            return null;
                        
                        CombatAction fastCommand = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(IllustrationName.Haste, fTag.IllustrationOrDefault),
                                "Fast Command",
                                [ModData.Traits.MoreDedications, Trait.Concentrate],
                                """
                                {i}A spark of strong connection can allow you to command with your familiar with a mere thought.{/i}

                                {b}Frequency{/b} once per day

                                The next familiar action you take this turn costs 1 fewer action.
                                """,
                                Target.Self())
                            .WithActionCost(0)
                            .WithEffectOnEachTarget(async (_, _, target, _) =>
                            {
                                target.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfYourTurn)
                                {
                                    ModifyActionPossibility = (_, action) =>
                                    {
                                        if (action.ActionCost < 1)
                                            return;
                                        if (IsFamiliarAction(action))
                                            --action.ActionCost;
                                    }
                                });
                                
                                qfThis.Owner.PersistentUsedUpResources.UsedUpActions
                                    .Add(ModData.PersistentActions.FastCommand);

                                return;

                                // Searches through your possibilities to find this action in your familiar actions menu.
                                bool IsFamiliarAction(CombatAction familiarAction)
                                {
                                    SubmenuPossibility? familiarMenu = LookInPossibilities(
                                        familiarAction.Owner.Possibilities,
                                        poss =>
                                            poss is SubmenuPossibility submenu
                                            && submenu.Subsections.Any(sect => sect.Name.Contains("Familiar action")));
                                    
                                    return familiarMenu?.Filter(ap => ap.CombatAction.Name == familiarAction.Name)?.ActionCount > 0;

                                    SubmenuPossibility? LookInPossibilities(
                                        Possibilities posses,
                                        Func<Possibility, bool> keepOnlyWhat)
                                    {
                                        foreach (PossibilitySection section in posses.Sections)
                                        {
                                            SubmenuPossibility? submenu = LookInSection(section, keepOnlyWhat);
                                            if (submenu != null)
                                                return submenu;
                                        }

                                        return null;
                                    }

                                    SubmenuPossibility? LookInSection(
                                        PossibilitySection section,
                                        Func<Possibility, bool> keepOnlyWhat)
                                    {
                                        foreach (Possibility possibility in section.Possibilities)
                                        {
                                            if (possibility is not SubmenuPossibility submenu)
                                                continue;
                                            if (keepOnlyWhat(submenu) || LookInMenu(submenu, keepOnlyWhat) is not null)
                                                return submenu;
                                        }

                                        return null;
                                    }

                                    SubmenuPossibility? LookInMenu(
                                        SubmenuPossibility submenu,
                                        Func<Possibility, bool> keepOnlyWhat)
                                    {
                                        foreach (PossibilitySection section in submenu.Subsections)
                                        {
                                            SubmenuPossibility? submenuInner = LookInSection(section, keepOnlyWhat);
                                            if (submenuInner != null)
                                                return submenuInner;
                                        }

                                        return null;
                                    }
                                }
                            });

                        return new PossibilitySection("Fast command")
                        {
                            Possibilities =
                            [
                                (ActionPossibility)fastCommand
                            ]
                        };
                    };
                });
        
        // Lv8: Mutable Familiar
        yield return new TrueFeat(
                ModData.FeatNames.MutableFamiliar,
                8,
                "Your familiar's supernatural spirit has outgrown its corporeal body.",
                "You can change your familiar's abilities as a precombat preparation.",
                [ModData.Traits.MoreDedications, Trait.Rebalanced])
            .WithAvailableAsArchetypeFeat(ModData.Traits.FamiliarMasterArchetype)
            .WithOnSheet(values =>
            {
                // Update morning preparations to precombat preparations.
                values.AtEndOfRecalculationBeforeMorningPreparations += values2 =>
                {
                    if (values2.SelectionOptions.FirstOrDefault(opt => opt.Key.Contains("FamiliarAbilities")) is { } selections)
                    {
                        selections.OptionLevel = SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL;
                        values.HasPrecombatPreparations = true;
                    }
                };
            });

        // Lv10: Incredible Familiar
        yield return new TrueFeat(
                ModData.FeatNames.IncredibleFamiliar,
                10,
                "Your familiar is imbued with even more magic than other familiars.",
                "You can select two additional familiar abilities each day. This is cumulative with {link:DawnsburyEnhancedFamiliar}Enhanced Familiar{/}.",
                [ModData.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(ModData.Traits.FamiliarMasterArchetype)
            .WithPrerequisite(FeatName.DawnsburyEnhancedFamiliar, "Enhanced Familiar")
            .WithOnSheet(values =>
            {
                if (values.Tags[Familiars.FAMILIAR_KEY] is not FamiliarTag { } fTag)
                    return;
                fTag.FamiliarAbilities += 2;
            });
    }
}