using System.ComponentModel.DataAnnotations;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Selections.Selected;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.LoresAndWeaknesses;
using Microsoft.Xna.Framework;
using CommonShieldRules = Dawnsbury.Mods.MoreShields.CommonShieldRules;

namespace Dawnsbury.Mods.SlayerClass;

public static class ClassFeats
{
    public static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        for (int i = 16; i <= 20; i+=2)
            yield return new TrueFeat(
                ModManager.RegisterFeatName("SlayerEmptyFeat"+i, "No Feat"),
                i,
                "Do nothing.", "Temporary until more feats are implemented.",
                [ModData.Traits.Slayer]);
        
        // Common references
        Feat markQuarry = AllFeats.GetFeatByFeatName(ModData.FeatNames.MarkQuarry);
        Feat claimTrophy = AllFeats.GetFeatByFeatName(ModData.FeatNames.ClaimTrophy);
        Feat onTheHunt = AllFeats.GetFeatByFeatName(ModData.FeatNames.OnTheHunt);

        #region 1st-Level

        // Bloodscent
        yield return new TrueFeat(
                ModData.FeatNames.Bloodscent, 1,
                "With a glance, you can judge how close your target is to falling.",
                $"The {RecallWeakness.GetActionLink()} action gains the {ModData.Tooltips.Relentless("relentless")} trait for you. You can also use Recall Weakness as a {{icon:FreeAction}} free action if the target is your quarry or is taking persistent bleed damage.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "Recall Weakness is relentless, and can be used as a free action against your quarry or bleeding targets.",
                qfFeat =>
                {
                    qfFeat.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId != RecallWeakness.RWActionId)
                            return;
                        action.WithExtraTrait(ModData.Traits.Relentless);
                    };
                    qfFeat.ProvideContextualAction = qfThis =>
                    {
                        List<Creature> freeActionTargets = qfThis.Owner.Battle.AllCreatures
                            .Where(cr =>
                                Slayer.IsMyQuarry(qfThis.Owner, cr)
                                || cr.QEffects.Any(qf =>
                                    qf.Id == QEffectId.PersistentDamage
                                    && qf.GetPersistentDamageKind() == DamageKind.Bleed))
                            .ToList();
                        
                        if (freeActionTargets.Count == 0)
                            return null;

                        CombatAction recall = RecallWeakness.CreateRecallWeaknessAction(qfThis.Owner)
                            .WithExtraTrait(Trait.Basic)
                            .WithActionCost(0);
                        recall.WithFullRename("Bloodscent");
                        recall.Description = recall.Description.Replace(
                            "a foe within range",
                            "a foe within range {Blue}who is your quarry or is taking persistent bleed damage{/Blue}");
                        recall.Illustration = new CornerIllustration(
                            IllustrationName.NarratorBook,
                            IllustrationName.PersistentBleed,
                            Direction.Northeast);
                        ((CreatureTarget)recall.Target).WithAdditionalConditionOnTargetCreature((a, d) =>
                            freeActionTargets.Contains(d)
                                ? Usability.Usable
                                : Usability.NotUsableOnThisCreature("Not your quarry nor bleeding"));
                        recall.Traits = new Traits([ModData.ModTrait, ..recall.Traits.ToList()], recall);

                        return new ActionPossibility(recall);
                    };
                })
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                values.FinalAbilityScores.TotalModifier(Ability.Intelligence) >= 2
                || values.FinalAbilityScores.TotalModifier(Ability.Wisdom) >= 2
                    ? null
                    : "It's recommended you have an Intelligence or Wisdom modifier of at least +2 to use this feat reliably.");
        
        // Crossbow Slayer
        yield return new TrueFeat(
                ModData.FeatNames.CrossbowSlayer, 1,
                "You find that a crossbow's versatility is the perfect companion to your own, and you eagerly reload it to get back in the fight.",
                $$"""
                  Reloading gains the {{ModData.Tooltips.Relentless("relentless")}} trait for you.
                  
                  {b}Special{/b} If you have a consecrated panoply signature tool, you can load a hunting spike into a crossbow when you reload it. The next time you use Hunting Spike, its thrown trait uses the crossbow’s range increment.
                  """,
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "Reloading gains the relentless trait.",
                qfFeat =>
                {
                    qfFeat.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId is ActionId.Reload)
                            action.WithExtraTrait(ModData.Traits.Relentless);
                    };

                    if (HuntingToolsTag.GetTool(qfFeat.Owner, ToolId.ConsecratedPanoply)
                        is not { } panoply)
                        return;
                    
                    qfFeat.Description += " You can load a hunting spike into a crossbow to increase the range of your next throwable {b}Hunting Spike {icon:Action}{/b} to its range increment.";

                    qfFeat.StartOfCombat = async qfThis =>
                    {
                        foreach (Item weapon in qfThis.Owner.HeldItems
                                     .Where(item =>
                                         item.HasTrait(Trait.Crossbow)
                                         && item.WeaponProperties!.RangeIncrement > 0))
                            qfThis.Owner.AddQEffect(CrossbowSlayer(weapon));
                    };

                    qfFeat.ProvideActionsIntoPossibilitySection = (qfThis, section) =>
                    {
                        List<Possibility> possibilities = [];
                        if (section.PossibilitySectionId is not PossibilitySectionId.ItemActions)
                            return possibilities;
                        
                        foreach (Item weapon in qfThis.Owner.HeldItems
                                     .Where(item =>
                                         item.HasTrait(Trait.Crossbow)
                                         && item.WeaponProperties!.RangeIncrement > 0
                                         && item.EphemeralItemProperties.NeedsReload))
                        {
                            CombatAction reload = qfThis.Owner.CreateReload(weapon)
                                .WithDescription("Load a hunting spike into the weapon. The next time you use {b}Hunting Spike {icon:Action}{/b}, its thrown range increases to " + weapon.WeaponProperties!.RangeIncrement * 5 + " feet.");
                            reload.WithFullRename(reload.Name.Replace("Reload", "Crossbow Slayer"));
                            reload.WithEffectOnChosenTargets(async (self, _) =>
                            {
                                if (!weapon.EphemeralItemProperties.NeedsReload)
                                    self.AddQEffect(CrossbowSlayer(weapon));
                            });
                            possibilities.Add(new ActionPossibility(reload));
                        }

                        return possibilities;
                    };
                    
                    return;

                    QEffect CrossbowSlayer(Item weapon) => new QEffect()
                    {
                        Name = "Crossbow Slayer",
                        Description = "You have a hunting spike loaded into your " + weapon.ToString().WithColor("Blue") + ". Your next {b}Hunting Spike {icon:Action}{/b} with a throwable weapon uses the crossbow's range increment and expends its ammo.\n\nThis effect ends early if you Strike with the crossbow.",
                        Illustration = weapon.Illustration,
                        Id = ModData.QEffectIds.CrossbowSlayer,
                        Tag = weapon,
                        DoNotShowUpOverhead = true,
                        AfterYouTakeAction = async (qfXBS, action) =>
                        {
                            if (!action.HasTrait(Trait.Strike))
                                return;
                            if (action.Item == weapon)
                                qfXBS.ExpiresAt = ExpirationCondition.Immediately;
                            if (action.Item!.Name.ToLower().Contains("hunting spike"))
                            {
                                qfXBS.ExpiresAt = ExpirationCondition.Immediately;
                                if (weapon.WeaponProperties!.RepeatingMagazineSize is not null)
                                {
                                    weapon.EphemeralItemProperties.AmmunitionLeftInMagazine--;
                                    if (weapon.EphemeralItemProperties.AmmunitionLeftInMagazine == 0)
                                        weapon.EphemeralItemProperties.NeedsReload = true;
                                }
                                else
                                    weapon.EphemeralItemProperties.NeedsReload = true;
                            }
                        }
                    };
                })
            .WithInappropriateBecauseOfBadInventory((values, inventory) =>
                FeatInventoryRequirements.RequiresOne(
                    inventory,
                    item =>
                        item.HasTrait(Trait.Ranged)
                        && !item.HasTrait(Trait.Consumable)
                        && (item.HasTrait(Trait.Reload1) || item.HasTrait(Trait.Reload2)), "a ranged weapon that requires reloading"));
        
        // Drink Adaptation Serums
        
        // Repelling Shield
        yield return new HuntingTool(
                "Repelling Shield",
                ToolId.RepellingShield,
                ToolKind.Secondary,
                ModData.Illustrations.RepellingShield,
                (self, tool, iTool, trophy, data, isSpecialized) =>
                {
                    List<DamageKind> kinds = data?.Kinds ?? [];
                    string kindDescription = kinds.Count > 0
                        ? S.ConstructOrList(
                            kinds
                                .Where(kind => !kind.IsPhysical())
                                .Select(kind =>
                                    kind.ToStringOrTechnical().WithColor(kind.DamageKindToColor())),
                            "and")
                        : "any of the trophy's damage types";
                    return $$"""
                           While this tool is raised, you gain a +2 circumstance bonus to Reflex saves against AoE from your quarry.
                           
                           {b}Reinforced{/b} You can Shield Block with this tool against attacks that deal {{kindDescription}} damage.
                           """;
                },
                (
                    "shield",
                    (_, item) => item.HasTrait(Trait.Shield)
                ))
            .ToSecondaryToolFeat(
                1,
                null,
                $$"""
                 You gain the {{AllFeats.GetFeatByFeatName(FeatName.ShieldBlock).ToLink("Shield Block {icon:Reaction}")}} general feat, and a repelling shield as a secondary tool. You can designate any shield as your repelling shield when you Reinforce your Arsenal.
                 
                 While your repelling shield is raised, you gain a +2 circumstance bonus to Reflex saving throws against area effects created by your quarry.
                 {b}Reinforced{/b} You can Shield Block with your repelling shield in response to taking any of the trophy's damage types from an attack, in addition to physical damage.
                 """,
                [ModData.Traits.Slayer])
            .WithOnSheet(values => values.GrantFeat(FeatName.ShieldBlock))
            .WithOnCreatureHuntingTool(
                ToolId.RepellingShield,
                (repShield, _,_,_, qfTool) =>
                {
                    qfTool.ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId != ActionId.RaiseShield
                            || action.Item is null
                            || !repShield.IsMyTool(action.Item))
                            return;

                        action.Description += "\n\n{b}Repelling Shield{/b} You also gain a +2 circumstance bonus to Reflex saving throws against area effects created by your quarry.".WithColor("Blue");

                        if (HuntingTools.GetMyItemTool(qfThis.Owner, repShield) is not {} iShield
                            || HuntingTools.GetTrophyDataOnItemTool(iShield) is not {} data
                            || data.Kinds.Count == 0)
                            return;
                            
                        action.Description += $"\n\n{{b}}Reinforced{{/b}} You can Shield Block with your repelling shield in response to any attack that deals {S.ConstructOrList(data.Kinds.Select(dk => dk.ToStringOrTechnical()))} damage.".WithColor("Blue");
                    };
                    qfTool.BonusToDefenses = (qfThis, action, def) =>
                        def is Defense.Reflex
                        && (action?.ChosenTargets.ChosenTile is not null || action?.ChosenTargets.ChosenTiles.Count > 0)
                        && HuntingTools.GetMyItemTool(qfThis.Owner, repShield) is {} iShield
                        && CommonShieldRules.GetRaisedShields(qfThis.Owner).Contains(iShield)
                        && Slayer.IsMyQuarry(qfThis.Owner, action.Owner)
                            ? new Bonus(2, BonusType.Circumstance, "Repelling shield", true)
                            : null;
                    qfTool.YourShieldBlockWorksAlsoAgainst = (qfThis, dEvent) =>
                        dEvent.CombatAction is { } action
                        && action.HasTrait(Trait.Attack)
                        && action.ActionId != ActionId.Trip
                        && HuntingTools.GetMyItemTool(qfThis.Owner, repShield) is {} iShield
                        && (qfThis.Owner.HasFeat(FeatName.ReactiveShield)
                            || CommonShieldRules.GetBlockableShields(qfThis.Owner).Contains(iShield))
                        && HuntingTools.GetTrophyDataOnItemTool(iShield) is {} data
                        && dEvent.KindedDamages.Any(kd => data.Kinds.Contains(kd.DamageKind));

                    return qfTool;
                })
            .With(feat =>
            {
                // "SlayerClass.HuntingTool.RepellingShield"
                ModData.FeatNames.RepellingShield = feat.FeatName;
            })
            .WithInappropriateBecauseOfBadInventory(FeatInventoryRequirements.RequiresShield);
        
        // Spiked Surcoat
        
        // Sudden Pounce
        // Requires More Basic Actions
        
        // Paired Bloodseeker
        yield return new HuntingTool(
                "Paired Bloodseeker",
                ToolId.PairedBloodseeker,
                ToolKind.Secondary,
                ModData.Illustrations.PairedBloodseeker,
                (self, tool, iTool, trophy, data, isSpecialized) =>
                {
                    string? ignoreAmount = iTool is not null ? (1 + iTool.WeaponProperties!.DamageDieCount).WithColor("Blue") : null;
                    DamageKind? chosenDk = trophy is not null ? Trophies.GetChosenDamageKind(trophy) : null;
                    string damageType = chosenDk is not null
                        ? (" " + chosenDk.Value.ToStringOrTechnical().WithColor(chosenDk.Value.DamageKindToColor() ) + " ")
                        : " ";
                    return $$"""
                             {b}Bloody Fuller{/b} Against your quarry, you ignore {{(ignoreAmount is null ? "an amount" : ignoreAmount + " points")}} of {{(isSpecialized ? "{Blue}any{/Blue}" : "physical")}} resistance to this tool's damage{{(ignoreAmount is null ? " equal to 1 + the number of weapon damage dice" : null)}}.
                             {b}Reinforced{/b} Your first Strike with this tool deals {Blue}{{(self.Level >= 19 ? 3 : self.Level >= 11 ? 2 : 1)}}d4{/Blue} additional{{damageType}}damage.{{(chosenDk is null ? " The type is chosen from the reinforcing trophy." : null)}}
                             """
                           + (isSpecialized
                               ? $"\n{{b}}Specialized{{/b}} This tool has {{tooltip:criteffect}}critical specialization effects{{/}}, and gains the effects of a {(self.PersistentCharacterSheet?.Calculated.GetTagOrNull<ItemName>(HuntingTools.PAIRED_BLOODSEEKER_RUNESTONE_KEY) is {} rune ? $"{{i}}{rune.ToLink(Items.GetItemTemplate(rune).RuneProperties!.Prefix)}{{/i}} property rune" : "property rune you choose when you Reinforce your Arsenal")}."
                               : null);
                },
                (
                    "simple or martial one-handed weapon",
                    (values, item) =>
                        item.HasAnyTraits([Trait.Simple, Trait.Martial])
                        && !item.HasTrait(Trait.TwoHanded)
                ))
            .ToSecondaryToolFeat(
                1,
                "Whether you carry two identical weapons or a useful sidearm, your signature weapon is paired, threatening your quarry with a storm of blows.",
                $$"""
                  You gain a paired bloodseeker as a secondary tool. You can designate any one-handed simple or martial weapon as your paired bloodseeker when you Reinforce your Arsenal.
                  
                  Your paired bloodseeker gains the initial benefit of your bloodseeking blade signature tool, except that you roll d4s instead of d6s for the additional damage. It also gains the specialized arsenal benefit when your bloodseeking blade signature tool does. You can use Honed Strike, or any other ability that requires you to wield or Strike with a bloodseeking blade signature tool, with your paired bloodseeker instead.
                  """,
                [ModData.Traits.Slayer])
            .WithOnCreatureBloodseeking(
                ToolId.PairedBloodseeker,
                HuntingTools.PAIRED_BLOODSEEKER_RUNESTONE_KEY,
                Dice.D4)
            .With(feat =>
            {
                // "SlayerClass.HuntingTool.PairedBloodseeker"
                ModData.FeatNames.PairedBloodseeker = feat.FeatName;
            })
            .WithPrerequisite(
                values => HuntingToolsTag.GetTool(values, ToolId.BloodseekingBlade) is not null,
                "You must know the bloodseeking blade signature tool.")
            .WithInappropriateBecauseOfBadInventory((_, inventory) => FeatInventoryRequirements.RequiresOne(
                inventory,
                item =>
                    (item.HasTrait(Trait.Simple) || item.HasTrait(Trait.Martial))
                    && !item.HasTrait(Trait.TwoHanded),
                "a simple or martial one-handed weapon"));
        
        // Peculiar Weaponry
        yield return new TrueFeat(
                ModData.FeatNames.PeculiarWeaponry, 1,
                "You specialize in an unusual weapon, whether a common soldier's armament or a unique tool few can use.",
                """
                If your bloodseeking blade signature tool is a simple weapon, increase its damage die size by one step.

                Your bloodseeking blade signature tool can be an advanced weapon, in addition to simple or martial, and you treat any advanced weapon you've designated as your signature tool as if it were a martial weapon for the purposes of proficiency.
                """,
                [ModData.Traits.Slayer])
            .WithPrerequisite(
                values => HuntingToolsTag.GetTool(values, ToolId.BloodseekingBlade) is not null,
                "You must know the bloodseeking blade signature tool.")
            .WithOnSheet(values =>
            {
                values.Proficiencies.Autoupgrade(
                    [Trait.Martial],
                    [Trait.Advanced, ModData.Traits.BloodseekingBlade]);
            })
            .WithPermanentQEffect(
                "The damage die of simple bloodseeking blades increases by one step. You can have advanced bloodseeking blades, and they use your martial proficiency.",
                qfFeat =>
                {
                    qfFeat.IncreaseItemDamageDie = (qfThis, item) =>
                        //HuntingTools.GetToolId(item) == ToolId.BloodseekingBlade
                        item.HasTrait(ModData.Traits.BloodseekingBlade)
                        && item.HasTrait(Trait.Simple);
                })
            .WithInappropriateBecauseOfBadInventory((_, inventory) => FeatInventoryRequirements.RequiresOne(
                inventory,
                item =>
                    (item.HasTrait(Trait.Simple) && item.WeaponProperties?.DamageDieSize < 12)
                    || item.HasTrait(Trait.Advanced),
                "a simple weapon with a damage die no bigger than a d10, or an advanced weapon"));

        #endregion

        #region 2nd-Level

        // Instant Enmity
        yield return new TrueFeat(
                ModData.FeatNames.InstantEnmity, 2,
                "You focus your hunt on an unexpected but loathsome foe.",
                $$"""
                  {b}Frequency{/b} Once per day.
                  {b}Trigger{/b} You see a creature of your level or higher take a hostile action against you or one of your allies.

                  The triggering creature becomes your {{markQuarry.ToLink("quarry")}} for the rest of the encounter, replacing any quarry you currently have (if any) until it dies. {Red}You can't Claim a Trophy{/Red} from a quarry you mark this way.
                  """,
                [ModData.Traits.Slayer])
            .WithActionCost(-2)
            .WithPermanentQEffect(
                "{Green}Once per day{/Green}, you can mark a creature taking hostile actions against your party as your quarry, replacing any existing quarry. You can't Claim their Trophy.",
                qfFeat =>
                {
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.EndlessEnmity))
                    {
                        qfFeat.Description = qfFeat.Description!.Replace(
                            "Once per day",
                            "Once per encounter");
                        qfFeat.Owner.PersistentUsedUpResources.UsedUpActions
                            .Remove(ModData.PersistentActions.INSTANT_ENMITY);
                    }
                    if (qfFeat.Owner.PersistentUsedUpResources.UsedUpActions
                        .Contains(ModData.PersistentActions.INSTANT_ENMITY))
                        qfFeat.Description = qfFeat.Description!.Replace(
                            "Green}",
                            "Red}");
                    
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            cr.EnemyOf(qfFeat.Owner)
                            && cr.Level >= qfFeat.Owner.Level,
                        qfEnmity =>
                        {
                            qfEnmity.AfterYouTakeActionAgainstTarget = async (qfEnmity2, action, target, _) =>
                            {
                                // Only trigger against allies
                                if (!target.FriendOf(qfFeat.Owner)
                                    || qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.INSTANT_ENMITY))
                                    return;
                                
                                if (!await qfFeat.Owner.Battle.AskToUseReaction(
                                        qfFeat.Owner,
                                        $$"""
                                          {b}Instant Enmity{/b}
                                          {b}Frequency{/b} Once per day
                                          {{qfEnmity2.Owner.ToString().WithColor("Blue")}} took a hostile action against {{(target == qfFeat.Owner ? "you" : target)}}. Temporarily mark them as your quarry?
                                          """,
                                        ModData.Illustrations.InstantEnmity,
                                        [ModData.Traits.Slayer]))
                                    return;
                                
                                // Use up limited usage
                                qfFeat.Owner.PersistentUsedUpResources.UsedUpActions
                                    .Add(ModData.PersistentActions.INSTANT_ENMITY);
                                qfFeat.Description = qfFeat.Description!.Replace(
                                    "Green}",
                                    "Red}");
                                
                                // Store all previous quarry and end the effect
                                List<(Creature, QEffect)> previousQuarry = qfFeat.Owner.Battle.AllCreatures
                                    .Where(cr => Slayer.IsMyQuarry(qfFeat.Owner, cr))
                                    .Select(cr => (
                                        cr,
                                        cr.QEffects.First(qf =>
                                            qf.Id == ModData.QEffectIds.MarkedQuarry && qf.Source == qfFeat.Owner)))
                                    .ToList();
                                previousQuarry.ForEach(tuple => tuple.Item2.ExpiresAt = ExpirationCondition.Immediately);
                                
                                // Mark the new creature
                                qfEnmity2.Owner.AddQEffect(Slayer.MarkQuarry(qfFeat.Owner, true)
                                    .With(qf =>
                                    {
                                        // When the Instant quarry dies, restore the old quarry.
                                        qf.WhenCreatureDiesAtStateCheckAsync += async _ =>
                                        {
                                            previousQuarry.ForEach(tuple =>
                                            {
                                                tuple.Item2.ExpiresAt = ExpirationCondition.Never;
                                                tuple.Item1.AddQEffect(tuple.Item2);
                                            });
                                        };
                                    }));
                                
                                // Play sounds and log the action
                                Sfxs.Play(ModData.SfxNames.MarkQuarry);
                                Feat instantEnmity = AllFeats.GetFeatByFeatName(ModData.FeatNames.InstantEnmity);
                                qfEnmity2.Owner.Battle.Log(
                                    $"{{Blue}}{qfFeat.Owner}'s{{/Blue}} uses {{b}}Instant Enmity{{/b}} {{icon:Reaction}} to treat {qfEnmity2.Owner} as their quarry.",
                                    "Instant Enmity {icon:Reaction}",
                                    "{i}" + instantEnmity.FlavorText + "{/i}\n\n" + instantEnmity.RulesText,
                                    new Traits([..instantEnmity.Traits.ToList()]));
                            };
                        });
                });
        
        // Pack Slayer
        yield return new TrueFeat(
                ModData.FeatNames.PackSlayer, 2,
                "You know that even lesser monsters make for worthy prey in enough numbers.",
                $"You can {markQuarry.ToLink("Mark as your Quarry")} a group of at least three creatures that share a name, even if their level is lower than yours. You can only {claimTrophy.ToLink("Claim a Trophy")} from this group once.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You can mark lower-level groups of 3+ as your quarry.",
                _ => { });
        
        // Personalized Gear
        
        // Salt Stone
        yield return new TrueFeat(
                ModData.FeatNames.SaltStone, 2,
                $"You draw your salt stone, a small block of dried magical compounds, and scrape it along a weapon you’re holding{ModData.Tooltips.SaltStoneHolding}.",
                $$"""
                {b}Requirements{/b} You have a free hand.
                
                For the rest of the encounter, that weapon gains the effects of a {{ItemName.GhostTouchRunestone.ToLink("ghost touch").WithTag("i")}} rune. If your quarry has regeneration, the weapon also deactivates your quarry’s regeneration as if it dealt damage of the appropriate type.
                
                {{ModData.Illustrations.DdSun.IllustrationAsIconString}} {b}Contextual Action{/b} This becomes available only when the encounter contains an enemy with an incorporeal resistance or regeneration.
                """,
                [Trait.Manipulate, ModData.Traits.Relentless, ModData.Traits.Slayer])
            .WithActionCost(1)
            .WithPermanentQEffect(
                "Draw a stone and apply it to a weapon you're holding, granting the effects of a ghost touch rune and allowing it to disable regeneration on your quarry.",
                qfFeat =>
                {
                    qfFeat.ProvideContextualAction = qfThis =>
                    {
                        if (!qfThis.Owner.Battle.AllCreatures
                                .Where(cr => cr.EnemyOf(qfThis.Owner))
                                .Any(cr =>
                                    (cr.HasEffect(QEffectId.Regeneration)
                                     && Slayer.IsMyQuarry(qfThis.Owner, cr))
                                    || cr.WeaknessAndResistance.Resistances.Any(resist =>
                                        resist is ResistanceToAll resAll
                                        && resAll.ToString().Contains("ghost touch"))))
                            return null;

                        CombatAction saltStone = new CombatAction(
                                qfThis.Owner,
                                ModData.Illustrations.SaltStone,
                                "Salt Stone",
                                [ModData.ModTrait, Trait.Manipulate, ModData.Traits.Relentless, ModData.Traits.Slayer],
                                null!,
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.HasFreeHand
                                            ? null
                                            : Usability.CommonReasons.NoFreeHand.UnusableReason)
                                    .WithAdditionalRestriction(self =>
                                        self.HeldItems.Any(IsValidWeapon)
                                            ? null
                                            : "No weapons to apply to."))
                            .WithDescription(
                                "You draw your salt stone, a small block of dried magical compounds, and scrape it along a weapon you’re holding.",
                                $$"""
                                  {b}Requirements{/b} You have a free hand.

                                  For the rest of the encounter, that weapon gains the effects of a {{ItemName.GhostTouchRunestone.ToLink("ghost touch").WithTag("i")}} rune. If your quarry has regeneration, the weapon also deactivates your quarry’s regeneration as if it dealt damage of the appropriate type.
                                  """)
                            .WithEffectOnSelf(async (action, caster) =>
                            {
                                Item? chosenWeapon = await caster.AskForChoiceAmongItems(
                                    action.Illustration,
                                    """
                                    {b}Salt Stone {icon:Action}{/b}
                                    Choose a weapon to gain the following benefits:
                                    • The effects of the ghost touch property rune.
                                    • Disables the regeneration of your quarry.
                                    """,
                                    caster.HeldItems
                                        .Where(IsValidWeapon)
                                        .ToList(),
                                    true);

                                if (chosenWeapon is null)
                                {
                                    action.RevertRequested = true;
                                    return;
                                }

                                chosenWeapon.Traits.Add(Trait.GhostTouch);
                                chosenWeapon.StateCheckWhenWielded += (wielder, item) =>
                                {
                                    wielder.AddQEffect(new QEffect(
                                        $"Salt Stone ({chosenWeapon.Name})",
                                        $"Your {{Blue}}{chosenWeapon.Name}{{/Blue}} has the effects of the ghost touch property rune, and can disable the regeneration of your quarry.",
                                        ExpirationCondition.Ephemeral,
                                        caster,
                                        action.Illustration)
                                    {
                                        Id = ModData.QEffectIds.SaltStoneBuff,
                                        CountsAsABuff = true,
                                        Tag = chosenWeapon,
                                        AfterYouDealDamageAgainstPrimaryTargetQ = async (_, combatAction, _, defender, result, _) =>
                                        {
                                            if (result >= CheckResult.Success
                                                && combatAction.HasTrait(Trait.Strike)
                                                && combatAction.Item == chosenWeapon
                                                && Slayer.IsMyQuarry(caster, defender)
                                                && defender.HasEffect(QEffectId.Regeneration))
                                            {
                                                defender.RemoveAllQEffects(qf =>
                                                    qf.Id == QEffectId.RegenerationPreventsDeath);
                                                defender.AddQEffect(new QEffect(
                                                    "Regeneration deactivated",
                                                    "This creature can't regenerate for 1 round.",
                                                    IllustrationName.RegenerationDisabled)
                                                {
                                                    Id = QEffectId.RegenerationDeactivated,
                                                    Key = "RegenerationDeactivated"
                                                }.WithExpirationInOneRound(defender.Battle));
                                            }
                                        }
                                    });
                                };
                            });

                        //QEffectId.RegenerationDeactivated

                        return new ActionPossibility(saltStone);
                        
                        bool IsValidWeapon(Item weapon)
                        {
                            return 
                                weapon.HasTrait(Trait.Weapon)
                                && !qfThis.Owner.QEffects.Any(qf =>
                                    qf.Id == ModData.QEffectIds.SaltStoneBuff
                                    && qf.Tag == weapon);
                        }
                    };
                });
        
        // Shifting Hunt
        
        // Slayer's Tricks
        yield return new TrueFeat(
                ModData.FeatNames.SlayersTricks, 2,
                "You’ve learned a few simple magical tricks to supplement your tools in a pinch.",
                $$"""
                    You gain two common occult cantrips as innate spells. Your spellcasting attribute modifier for these spells and any other spells you gain from slayer feats is Wisdom{{ModData.Tooltips.SlayersTricksAbility}}, rather than Charisma. Casting a Spell gains the relentless trait for you, as long as the spell you cast came from a slayer feat.
                    
                    {b}Special{/b} If you have a consecrated panoply signature tool, you can choose divine spells rather than occult spells for this feat and for any other slayer feats that allow you to choose innate spells.
                    """,
                [ModData.Traits.Slayer])
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Spell, Proficiency.Trained);
                values.InnateSpells.GetOrCreate(
                    ModData.Traits.Slayer,
                    () => new InnateSpells(Trait.Occult));
                
                bool hasPanoply = HuntingToolsTag.GetTag(values)
                    ?.IsKnown(ToolId.ConsecratedPanoply) == true;
                
                values.AddSlayerSpellOption(
                    "SlayersTricksCantrips1",
                    "Slayer's Tricks cantrip 1",
                    0,
                    hasPanoply);
                values.AddSlayerSpellOption(
                    "SlayersTricksCantrips2",
                    "Slayer's Tricks cantrip 2",
                    0,
                    hasPanoply);
            })
            .WithOnCreature(self =>
            {
                self.AddQEffect(new QEffect()
                {
                    Name = "[SLAYER: SLAYER'S TRICKS, MODIFY SPELLCASTING SOURCE]",
                    StartOfCombatBeforeOpeningCutscene = async qfThis =>
                    {
                        // Modify slayer spells to:
                        // - use Wisdom.
                        // - be Relentless.
                        foreach (SpellcastingSource source in qfThis.Owner.Spellcasting
                                     ?.Sources
                                     .Where(IsSlayerSource)
                                     .ToList() ?? [])
                        {

                            source.SpellcastingAbility = Ability.Wisdom;
                        }
                    },
                    ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (IsSlayerSpell(action))
                            action.WithExtraTrait(ModData.Traits.Relentless);
                    }
                });
            });

        #endregion

        #region 4th-Level
        
        // Apply Spirit Oil

        // Blood for Blood
        yield return new TrueFeat(
                ModData.FeatNames.BloodForBlood, 4,
                "You viciously return your foe’s attack, reinvigorating yourself with your vengeance.",
                """
                {b}Requirements{/b} A creature critically hit you with an attack since the end of your previous turn.

                Strike the required creature. On a hit, you gain temporary Hit Points equal to your level.
                """,
                [Trait.Flourish, Trait.Rebalanced, ModData.Traits.Slayer])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                int levelTemp = qfFeat.Owner.Level;
                
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + $" [flourish] Strike a foe who critically hit you since your last turn. On a hit, gain {{Blue}}{levelTemp}{{/Blue}} temp HP.";

                qfFeat.Tag = new List<Creature>();
                qfFeat.AfterYouTakeDamage = async (qfThis, amount, _, action, isCritical) =>
                {
                    if (!isCritical
                        || action is not { Owner: {} foe }
                        || !action.HasTrait(Trait.Attack)
                        || !foe.EnemyOf(qfThis.Owner)
                        || foe == qfThis.Owner.Battle.Pseudocreature)
                        return;

                    List<Creature> crits = (List<Creature>)qfThis.Tag!;
                    crits.Add(foe);
                    qfThis.Owner.Battle.Log($"{qfThis.Owner} can use {{b}}Blood for Blood {{icon:Action}}{{/b}} against {action.Owner}.");
                };
                qfFeat.EndOfYourTurnDetrimentalEffect = async (qfThis, _) =>
                {
                    List<Creature> crits = (List<Creature>)qfThis.Tag!;
                    crits.Clear();
                };
                qfFeat.ProvideContextualAction = qfThis =>
                {
                    List<Creature> crits = (List<Creature>)qfThis.Tag!;
                    if (crits.Count == 0)
                        return null;

                    CombatAction bloodReply = new CombatAction(
                            qfThis.Owner,
                            new SideBySideIllustration(
                                IllustrationName.DeflectCriticalHit,
                                IllustrationName.Shortsword),
                            "Blood for Blood",
                            [ModData.ModTrait, Trait.Flourish, ModData.Traits.Slayer, Trait.Basic],
                            null!,
                            Target.Self())
                        .WithDescription(
                            "You viciously return your foe’s attack, reinvigorating yourself with your vengeance.",
                            $$"""
                              {b}Requirements{/b} A creature critically hit you with an attack since the end of your previous turn.

                              Strike the required creature. On a hit, you gain {Blue}{{levelTemp}}{/Blue} temporary Hit Points.
                              """)
                        .WithEffectOnSelf(async (action, caster) =>
                        {
                            if (!await CommonCombatActions.StrikeCreature(
                                    caster,
                                    null,
                                    strike =>
                                        strike.WithEffectOnEachTarget(async (_, _, _, result) =>
                                        {
                                            if (result < CheckResult.Success)
                                                return;
                                            caster.GainTemporaryHP(levelTemp);
                                        }),
                                    crits.Contains,
                                    action.Illustration,
                                    null,
                                    true,
                                    "Pass"))
                            {
                                action.RevertRequested = true;
                            }
                        });

                    return new ActionPossibility(bloodReply);
                };
            });
        
        // Blood Rush
        yield return new TrueFeat(
                ModData.FeatNames.BloodRush, 4,
                "The adrenaline of entering combat with your target pushes you forward.",
                $$"""
                {b}Trigger{/b} You roll initiative and have a quarry.

                You go {{onTheHunt.ToLink("On the Hunt")}} as a free action.
                """,
                [ModData.Traits.Slayer])
            .WithActionCost(0)
            .WithPermanentQEffect(
                "If you roll initiative and have a quarry: Go On the Hunt.",
                qfFeat =>
                {
                    qfFeat.StartOfCombatReaction = qfThis =>
                    {
                        Feat bloodRush = AllFeats.GetFeatByFeatName(ModData.FeatNames.BloodRush);
                        CombatAction rushAct = new CombatAction(
                                qfThis.Owner,
                                IllustrationName.Rage,
                                "Blood Rush",
                                [ModData.Traits.Slayer],
                                null!,
                                Target.Self()
                                    .WithAdditionalRestriction(self =>
                                        self.Battle.AllCreatures.Any(cr => Slayer.IsMyQuarry(self, cr))
                                        ? null
                                        : "No quarry"))
                            .WithDescription(bloodRush.FlavorText, bloodRush.RulesText)
                            .WithActionCost(0)
                            .WithEffectOnSelf(async self =>
                                await Slayer.GoOnTheHunt(self, true));

                        ReactionOption rushReact = ReactionOption.CreateFromCombatActionCustom(
                            rushAct,
                            "Go On the Hunt as a {icon:FreeAction} free action.",
                            async () => await qfThis.Owner.Battle.GameLoop.FullCast(rushAct));
                        rushReact.Caption += " {icon:FreeAction}"; // BUG: Doesn't seem to work
                        
                        return ((SelfTarget) rushAct.Target).CanBeginToUse(qfThis.Owner)
                            ? (ReactionOptions) rushReact
                            : null;
                    };
                    /*qfFeat.StartOfCombatAfterInitiativeOrderIsSetUp = async qfThis =>
                    {
                        if (qfThis.Owner.Battle.AllCreatures.All(cr => !Slayer.IsMyQuarry(qfThis.Owner, cr)))
                            return;
                        
                        Feat bloodRush = AllFeats.GetFeatByFeatName(ModData.FeatNames.BloodRush);
                        CombatAction rushAct = new CombatAction(
                                qfThis.Owner,
                                IllustrationName.Rage,
                                "Blood Rush",
                                [ModData.Traits.Slayer],
                                null!,
                                Target.Self())
                            .WithDescription(
                                bloodRush.FlavorText,
                                bloodRush.RulesText)
                            .WithActionCost(0)
                            .WithEffectOnSelf(async self =>
                                await Slayer.GoOnTheHunt(self, true));

                        await qfThis.Owner.Battle.GameLoop.FullCast(rushAct);
                    };*/
                });
        
        // Cure-all
        
        // Expansive Panoply
        yield return new TrueFeat(
                ModData.FeatNames.ExpansivePanoply, 4,
                "While a short, sharp piece of metal is a remarkably versatile tool, you know that it is not appropriate for every occasion.",
                // clubs, darts, or shortswords.
                $"When you use {{b}}Hunting Spike {{icon:Action}}{{/b}}, you can draw and Strike with spikes that function as {ItemName.Club.ToLink("clubs")} or {ItemName.Shortsword.ToLink("shortswords")}, rather than {ItemName.Dagger.ToLink("daggers")}.",
                [ModData.Traits.Slayer])
            .WithPrerequisite(
                values => HuntingToolsTag.GetTool(values, ToolId.ConsecratedPanoply) is not null,
                "You must know the consecrated panoply signature tool.")
            .WithPermanentQEffect(
                "Your hunting spikes can also be clubs or shortswords.",
                _ => {});

        #endregion

        #region 6th-Level
        
        // Final Flourish
        yield return new TrueFeat(
                ModData.FeatNames.FinalFlourish, 6,
                "With a showy flourish, you flick blood off your blade or rearm your weapon, invigorating yourself.",
                """
                {b}Requirements{/b} You reduced a creature to 0 Hit Points this turn.

                You gain temporary Hit Points equal to half your level. You can Interact to swap weapons or reload a weapon you're wielding.
                """,
                [Trait.Flourish, ModData.Traits.Slayer])
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    qfFeat.AfterYouDealDamage = async (self, action, target) =>
                    {
                        if (target.HP > 0)
                            return;
                        qfFeat.UsedThisTurn = true;
                    };
                    qfFeat.ProvideContextualAction = qfThis =>
                    {
                        if (!qfThis.UsedThisTurn
                            || qfThis.Owner.Battle.CreatureControllingInitiative != qfThis.Owner)
                            return null;

                        int tempGain = qfThis.Owner.Level / 2;

                        CombatAction ff = new CombatAction(
                                qfThis.Owner,
                                new SideBySideIllustration(
                                    IllustrationName.Swipe,
                                    IllustrationName.Heal),
                                "Final Flourish",
                                [ModData.ModTrait, Trait.Flourish, ModData.Traits.Slayer],
                                null!,
                                Target.Self())
                            .WithDescription(
                                "With a showy flourish, you flick blood off your blade or rearm your weapon, invigorating yourself.",
                                $$"""
                                {b}Requirements{/b} You reduced a creature to 0 Hit Points this turn.

                                You gain {Blue}{{tempGain}}{/Blue} temporary Hit Points. You can Interact to swap weapons or reload a weapon you're wielding.
                                """)
                            .WithEffectOnSelf(async (action, self) =>
                            {
                                self.GainTemporaryHP(tempGain);
                                
                                // Work-around for the added restriction on Replace actions that precalculates the action cost.
                                QEffect tempFix = new QEffect() { Name = "TEMPORARY", Id = QEffectId.Valet };
                                self.AddQEffect(tempFix);
                                
                                Possibilities poss = Possibilities
                                    .Create(self)
                                    // Keep only reloads and the inventory
                                    .FilterAnyPossibility2(poss =>
                                    {
                                        if (poss is ActionPossibility { CombatAction.ActionId: ActionId.Reload })
                                            return true;
                                        if (poss is SubmenuPossibility { } menu)
                                        {
                                            if (menu.Caption is "Both hands" or "Left hand"
                                                && self.HeldItems.Count != 0
                                                && self.HeldItems[0].HasTrait(Trait.Weapon))
                                                return true;
                                            if (menu.Caption is "Right hand"
                                                && self.HeldItems.Count > 1
                                                && self.HeldItems[1].HasTrait(Trait.Weapon))
                                                return true;
                                        }
                                        return false;
                                    })
                                    // Keep only reloads and swaps
                                    .Filter(ap =>
                                        (ap.CombatAction.Item?.HasTrait(Trait.Weapon) ?? false)
                                        && ap.CombatAction.ActionId is ActionId.Reload or ActionId.ReplaceItemInHand);
                                poss.CannotPass = false;
                                poss.Sections.Add(new PossibilitySection("Pass")
                                {
                                    Possibilities = [new ActionPossibility(CombatAction.CreatePass(self, null))]
                                });
            
                                Creature? active = self.Battle.ActiveCreature;
                                self.Battle.ActiveCreature = self;
                                self.Possibilities = poss;
            
                                List<Option> actions = await self.Battle.GameLoop.CreateActions(
                                    self,
                                    poss,
                                    null);
                                self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                                await self.Battle.GameLoop.OfferOptions(self, actions, true);
            
                                self.Battle.ActiveCreature = active;
                                
                                tempFix.ExpiresAt = ExpirationCondition.Immediately;
                            });

                        return new ActionPossibility(ff);
                    };
                });
        
        // Relentless Counterstrike
        
        // Shifting Combination
        
        // Spell Slates
        // DOC: Reinforced benefit changed to "traditions" to make sense of the plurality of tradition associations written into the core rules of a trophy's properties.
        yield return new HuntingTool(
                "Spell Slates",
                ToolId.SpellSlates,
                ToolKind.Secondary,
                ModData.Illustrations.SpellSlates,
                (self, tool, iTool, trophy, data, isSpecialized) =>
                {
                    // Passive benefit
                    bool hasPanoply = HuntingToolsTag.GetTag(self)
                        ?.IsKnown(ToolId.ConsecratedPanoply) == true;
                    string passive =
                        $"You gain a {S.ConstructOrList(
                            ((IEnumerable<string?>)["1st-", "2nd-", (self.Level >= 8 ? "3rd-" : null)]).WhereNotNull(),
                            "and")}rank {"Occult".WithColor(Trait.Occult.TraditionTraitToColor())} {(hasPanoply ? $"or {"Divine".WithColor(Trait.Divine.TraditionTraitToColor())} " : null)}innate spell. You can cast each once per day.";
                    
                    // Reinforced benefit
                    int maxRank = self.PersistentCharacterSheet?.Calculated.InnateSpells
                        .GetOrCreate(
                            ModData.Traits.Slayer,
                            () => new InnateSpells(Trait.Occult))
                        .SpellsKnown
                        .Max(spell => spell.SpellLevel) ?? 0;
                    string rank = maxRank > 0
                        ? $"{maxRank.Ordinalize2()}-rank".WithColor("Blue")
                        : "the highest-rank innate slayer-feat spell you have";
                    
                    List<string>? traditionList = trophy is not null
                        ? data?.Traditions
                            ?.Select(trait =>
                                trait.HumanizeTitleCase2()
                                    .WithColor(trait.TraditionTraitToColor()))
                            .ToList()
                        : null;
                    string reinforced = $"{{b}}Reinforced{{/b}} You gain an additional innate spell of up to {rank}. This spell must be of the {(traditionList is not null
                        ? S.ConstructOrList(traditionList) + " tradition".PluralizeIf(null, "s", traditionList.Count)
                        : "trophy's traditions")}. You can swap this choice when you Reinforce your Arsenal.";
                    
                    return
                        $"""
                         {passive}
                         {reinforced}
                         """;
                },
                (
                    "set of prepared charms or runes",
                    (_, item) => item.ItemName == HuntingTools.SpellSlates
                ))
            .ToSecondaryToolFeat(
                6,
                "You’ve learned how to expand your magical tricks with a set of specially prepared charms or runes.",
                $$"""
                You gain a set of spell slates as a secondary tool, which are a worn item. You can designate this item as your spell slates when you Reinforce your Arsenal.

                When you gain this feat, choose a 1st-rank and a 2nd-rank occult spell to gain as innate spells. At 8th level, choose a 3rd-rank spell as well. You can cast each of these spells once per day.

                {b}Reinforced{/b} You gain an additional common innate spell of a rank equal to or less than the highest-rank innate spell you have from slayer feats, which you can cast once per day. This additional spell must be of the trophy's traditions{{ModData.Tooltips.SpellSlatesTraditions}}, and you can swap it for a different spell with the same restrictions when you Reinforce your Arsenal. {i}(This swap doesn't affect the restriction on casting the spell once per day.){/i}
                """,
                [ModData.Traits.Slayer])
            .WithFreeInventoryItem(HuntingTools.SpellSlates)
            .WithOnCreatureHuntingTool(
                ToolId.SpellSlates,
                (slates, iSlates, _, _, qfTool) =>
                {
                    /*if (self.PersistentCharacterSheet is null)
                        return null;*/
                    
                    // The slates are worn and can't move around or be destroyed,
                    // so it's safe to return if the item isn't found.
                    // Consequently, it's also safe to reuse all of the above data references.
                    if (iSlates is null)
                        return null;
                    
                    // If the bonus spell was expended,
                    // then remove other spells even if your choice was swapped.
                    qfTool.StartOfCombatBeforeOpeningCutscene = async qfThis =>
                    {
                        if (qfThis.Owner.PersistentUsedUpResources
                            .GetSpellcasting(ModData.Traits.ReinforcedSlateSpell)
                            .PreparedSpellsUsedUp
                            .Any(spellsOfRank =>
                                spellsOfRank.Count > 0))
                        {
                            SpellcastingSource? source = qfThis.Owner.Spellcasting
                                ?.GetSourceByOrigin(ModData.Traits.ReinforcedSlateSpell);
                            if (source is null)
                                return;
                            foreach (CombatAction action in source.Spells.ToList())
                                source.Spellcasting.UseUpSpellcastingResources(action);
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        }
                    };

                    return qfTool;

                    /*if (self.PersistentUsedUpResources
                            .GetSpellcasting(ModData.Traits.Slayer)
                            .PreparedSpellsUsedUp[reinforcedSpell.SpellLevel]
                            .Any(spell =>
                                spell.CombatActionSpell == reinforcedSpell.CombatActionSpell)
                        || self.PersistentUsedUpResources.UsedUpActions
                            .Contains(ModData.PersistentActions.REINFORCED_SPELL))
                    {
                        string toolName = slates.Id.GetNameFromToolId();
                        self.AddQEffect(HuntingTools.ToolWarning(
                            false, "EXPENDED SPELL",
                            $"""
                             Your {toolName.WithTag("b")} has a spell chosen from its reinforced benefits, but that spell has been expended.

                             This is normal. You can only cast the spell from your {toolName.WithTag("b")} once per day, regardless of whether you've changed your choice of spell.
                             """));
                        return;
                    }*/

                    /*self.AddQEffect(new QEffect()
                    {
                        Name = "[SLAYER SPELL SLATES REINFORCED SPELL]",
                        AfterYouExpendSpellcastingResources = (qfThis, action) =>
                        {
                            if (GetSuperSpell(action) == GetSuperSpell(reinforcedSpell.CombatActionSpell))
                                self.PersistentUsedUpResources.UsedUpActions
                                    .Add(ModData.PersistentActions.REINFORCED_SPELL);
                            
                            return;
                            
                            CombatAction GetSuperSpell(CombatAction spellAction)
                            {
                                if (spellAction.Superspell is not null
                                    && spellAction.Superspell != spellAction)
                                    return GetSuperSpell(spellAction.Superspell);
                                return spellAction;
                            }
                        }
                    });*/
                },
                [
                    (self, slates, _, _, _) =>
                    {
                        // Find selected bonus spell
                        Spell? reinforcedSpell = (self.PersistentCharacterSheet?.SelectedFeats
                                .FirstOrDefault(choice =>
                                    choice.Value is SpellSelectedChoice
                                    && choice.Key.Contains("SpellSlatesReinforcedSpell"))
                                .Value as SpellSelectedChoice)
                            ?.Choices.FirstOrDefault();
                    
                        if (reinforcedSpell is not null)
                            return null;

                        // If no bonus spell is selected, warn the user.
                        // This shouldn't be possible, but it's a good fallback.
                        return HuntingTools.ToolWarning(
                            true, "REINFORCED SPELL",
                            $$"""
                              Your {{slates.Id.GetNameFromToolId().WithTag("b")}} has a trophy reinforcing it, but you did not select an additional spell.

                              This error shouldn't be possible, as selecting a reinforced spell is not an optional choice. If you see this error, please report it immediately to the {link:https://steamcommunity.com/sharedfiles/filedetails/?id=3715781137}Slayer Class{/} mod page.
                              """);
                    }
                ])
            .WithOnSheet(values =>
            {
                // Skipped proficiency due to Slayer's Tricks prerequisite.
                // Create Reinforced Spell source.
                
                bool hasPanoply = HuntingToolsTag.GetTag(values)
                    ?.IsKnown(ToolId.ConsecratedPanoply) == true;
                
                values.AddSlayerSpellOption(
                    "SpellSlatesSpell1",
                    "Spell Slates rank 1 spell",
                    1,
                    hasPanoply);
                values.AddSlayerSpellOption(
                    "SpellSlatesSpell2",
                    "Spell Slates rank 2 spell",
                    2,
                    hasPanoply);
                values.AddAtLevel(8, values8 =>
                {
                    values8.AddSlayerSpellOption(
                        "SpellSlatesSpell3",
                        "Spell Slates rank 3 spell",
                        3,
                        hasPanoply);
                });
                
                // Reinforced benefits: create innate list
                values.InnateSpells.GetOrCreate(
                    ModData.Traits.ReinforcedSlateSpell,
                    () => new InnateSpells(Trait.Occult));

                // Reinforced benefits: select spell before combat
                values.AtEndOfRecalculationBeforeMorningPreparations += valuesBefore =>
                {
                    // Choose an inventory to query for preparations.
                    // If you're a campaign character, use that inventory.
                    // Otherwise, look through every builder-level inventory to
                    // find the last one that has a Spell Slates item with a trophy.
                    // This solution also avoids creating empty inventories.
                    Inventory inv = valuesBefore.Sheet.IsCampaignCharacter
                        ? valuesBefore.Sheet.CampaignInventory
                        : valuesBefore.Sheet.InventoriesByLevel
                            .LastOrDefault(kvp =>
                                HuntingTools.GetFullHuntingToolData(valuesBefore, kvp.Value, ToolId.SpellSlates).TrophyData?.Traditions is not null)
                            .Value
                          ?? valuesBefore.Sheet.Inventory;
                    
                    (HuntingTool? slates, Item? iSlates, Item? trophy, TrophyData? data) =
                        HuntingTools.GetFullHuntingToolData(valuesBefore, inv, ToolId.SpellSlates);
                    if (slates is null || iSlates is null || trophy is null
                        || data?.Traditions is not { } traditions)
                        return;
                    
                    int maxRank = valuesBefore.InnateSpells.GetOrCreate(
                            ModData.Traits.Slayer,
                            () => new InnateSpells(Trait.Occult))
                        .SpellsKnown
                        .Max(spell => spell.SpellLevel);

                    // This isn't meant to allow cantrips.
                    if (maxRank < 1)
                        return;
                    
                    valuesBefore.AddSlayerSpellOption(
                        "SpellSlatesReinforcedSpell",
                        "Spell Slates reinforced spell",
                        maxRank,
                        traditions, 
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                        alternativeSource: ModData.Traits.ReinforcedSlateSpell);
                };
            })
            .With(feat =>
            {
                // "SlayerClass.HuntingTool.SpellSlates"
                ModData.FeatNames.SpellSlates = feat.FeatName;
            })
            .WithPrerequisite(values =>
                values.HasFeat(ModData.FeatNames.SlayersTricks),
                "Slayer's Tricks");
        
        // Wall of Will

        #endregion

        #region 8th-Level
        
        // Armored Fortress
        
        // Catalyzing Flask
        yield return new HuntingTool(
            "Catalyzing Flask",
            ToolId.CatalyzingFlask,
            ToolKind.Secondary,
            ModData.Illustrations.CatalyzingFlask,
            (self, tool, iTool, trophy, data, isSpecialized) =>
            {
                bool usedUp = self.QEffects.Any(qf =>
                    qf.Id == ModData.QEffectIds.CatalyzingFlaskGranter
                    && qf.UsedUpPermanently);
                string passive =
                    $"(Once per encounter) Activating your {iTool?.ProsaicName.Replace("reinforced ", "").WithTag("Blue") ?? "catalyzing flask"} won't permanently consume it."
                        .WithTag(usedUp ? "strike" : null);
                    
                List<Defense>? saves = data?.GetHighestSaves();
                string savesDesc = saves is not null
                    ? S.ConstructOrList(saves.Select(save => save.ToStringOrTechnical().WithTag("Blue"))) + " saving throws"
                    : "whichever saving throw is the trophy's highest" ;
                string reinforced = $"{{b}}Reinforced{{/b}} When you drink or administer from the flask, the target gains a +1 status bonus to {savesDesc} for the rest of the encounter.";

                return
                    $$"""
                      {{passive}}
                      {{reinforced}}
                      """;
            },
            (
                "alchemical elixir",
                (values, item) => item.HasTrait(Trait.Alchemical) && item.HasTrait(Trait.Elixir)
            ))
            .WithNicknameProperties(true)
            .ToSecondaryToolFeat(
                8,
                "You possess a special alchemical vial that reacts with the monster parts within the fluid to produce more and fortify its power.",
                """
                You gain a catalyzing flask as a secondary tool. You can designate any alchemical elixir of your level or lower as your catalyzing flask when you Reinforce your Arsenal.
                
                Once per encounter, you can Activate the elixir it contains without consuming it. Activating it again fully consumes the elixir.
                
                {b}Reinforced{/b} When you Activate the elixir within your catalyzing flask, you also a gain a +1 status bonus to Fortitude, Reflex, or Will saves. The save is whichever was the highest saving throw of the creature the trophy was claimed from. This bonus lasts for the rest of the encounter.
                """,
                [Trait.Rebalanced, ModData.Traits.Slayer])
            .WithOnCreatureHuntingTool(
                ToolId.CatalyzingFlask,
                (flask, _, _, _, qfTool) =>
                {
                    qfTool.Id = ModData.QEffectIds.CatalyzingFlaskGranter;
                    qfTool.UsedUpPermanently = false; // is true when the flask is activated
                    qfTool.ModifyActionPossibility = (qfFlask, action) =>
                    {
                        if (HuntingTools.GetMyItemTool(qfFlask.Owner, ToolId.CatalyzingFlask)
                                is not { } iFlask
                            || HuntingTools.GetTrophyDataOnItemTool(iFlask)
                                is not { } data
                            || action.Item != iFlask)
                            return;

                        List<Defense>? saves = data.GetHighestSaves();

                        if (action.ActionId is ActionId.Drink)
                            action.EffectOnChosenTargets = async (drink, self2, _) =>
                                await Drink(drink, drink.Item!, self2, self2);
                        else if (action.ActionId is ActionId.Administer)
                            action.EffectOnOneTarget = async (drink, self2, target, _) =>
                                await Drink(drink, drink.Item!, self2, target);
                        else
                            return;

                        action.WithFullRename(action.ActionId.ToStringOrTechnical() + " from Flask");
                        action.Traits.Remove(Trait.Consumable);
                        if (!qfFlask.UsedUpPermanently)
                            action.Description += "\n\n{Blue}{b}Catalyzing Flask{/b}{/Blue} (Once per encounter) Activating this elixir won't permanently consume it.";
                        if (saves?.Count > 0)
                            action.Description += $"\n\n{{Blue}}{{b}}Reinforced{{/b}}{{/Blue}} You gain a +1 status bonus to {S.ConstructOrList(saves.Select(save => save.ToStringOrTechnical()))} saving throws for the rest of the encounter.";

                        return;
                            
                        async Task Drink(CombatAction activate, Item item, Creature user, Creature target)
                        {
                            // Apply drinkable effects
                            #pragma warning disable CS0618 // Type or member is obsolete
                            Action<CombatAction, Creature>? drinkableEffect = item.DrinkableEffect;
                            #pragma warning disable CS0618 // Type or member is obsolete
                            drinkableEffect?.Invoke(activate, target);
                            await item.WhenYouDrink.InvokeIfNotNull(activate, target);
                                
                            Sfxs.Play(SfxName.PotionUse2);
                                
                            // Free usage
                            if (qfFlask.UsedUpPermanently)
                                user.HeldItems.Remove(item);
                            else 
                                qfFlask.UsedUpPermanently = true; // Item is not consumed once per encounter
                                
                            foreach (QEffect qf in target.QEffects)
                                await qf.AfterYouDrink.InvokeIfNotNull(qf, item, activate);
                                
                            // Reinforced benefits
                            if (saves.Count > 0)
                            {
                                Defense save;
                                if (saves.Count > 1)
                                {
                                    ChoiceButtonOption choice = await user.AskForChoiceAmongButtons(
                                        flask.Icon,
                                        """
                                        {b}Catalyzing Flask{/b}
                                        Choose a saving throw to gain a +1 status bonus in for the rest of the encounter.
                                        """,
                                        [
                                            ..saves
                                                .Select(def => def.ToStringOrTechnical())
                                        ]);

                                    save = saves[choice.Index];
                                }
                                else
                                    save = saves.First();

                                target.AddQEffect(new QEffect(
                                    $"Catalyzing Flask ({save.ToStringOrTechnical()})",
                                    $"You have a +1 status bonus to {save.ToStringOrTechnical()} saves for the rest of the encounter.",
                                    flask.Icon)
                                {
                                    BonusToDefenses = (_,_, def) =>
                                        def == save
                                            ? new Bonus(1, BonusType.Status, "Catalyzing flask")
                                            : null
                                });
                            }
                        }
                    };

                    return qfTool;
                },
                [
                    (_, tool, _, _, data) =>
                    {
                        if (data is null)
                            return null;
                        if (data.GetHighestSaves().Count == 0)
                            return HuntingTools.ToolWarning(
                                true, "HIGHEST SAVE",
                                $$"""
                                  Your {{tool.Id.GetNameFromToolId().WithTag("b")}} has a trophy reinforcing it, but the trophy contains no highest saving throws.

                                  This isn't supposed to be possible, as even inert object creatures have saving throw statistics in Dawnsbury Days, and a tied saving throw was implemented in the game as an additional option to the slayer. If you see this error, please report it immediately to the {link:https://steamcommunity.com/sharedfiles/filedetails/?id=3715781137}Slayer Class{/} mod page.
                                  """);
                        return null;
                    }
                ])
            .With(feat =>
            {
                // "SlayerClass.HuntingTool.CatalyzingFlask"
                ModData.FeatNames.CatalyzingFlask = feat.FeatName;
            });
        
        // Defensive Hunt
        yield return new TrueFeat(
                ModData.FeatNames.DefensiveHunt, 8,
                "Even in a moment of danger, you turn weakness into opportunity.",
                $$"""
                  {b}Trigger{/b} You are critically hit by your quarry.

                  You go {{onTheHunt.ToLink("On the Hunt")}} as a {icon:FreeAction} free action.
                  """,
                [ModData.Traits.Slayer])
            .WithActionCost(-2)
            .WithPermanentQEffect(
                "When your quarry critically hits you, go On the Hunt {icon:FreeAction}.",
                qfFeat =>
                {
                    qfFeat.AfterYouAreTargeted = async (qfThis, action) =>
                    {
                        if (action.CheckResult == CheckResult.CriticalSuccess
                            && action.HasTrait(Trait.Attack)
                            && action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is null
                            && await qfThis.Owner.Battle.AskToUseReaction(
                                qfThis.Owner,
                                """
                                {b}Defensive Hunt{/b} {icon:Reaction}
                                You've been {Red}critically hit{/Red} by your quarry.
                                """,
                                ModData.Illustrations.OnTheHunt,
                                [ModData.Traits.Slayer],
                                ["Go On the Hunt {icon:FreeAction}"]) == 0)
                            await Slayer.GoOnTheHunt(qfThis.Owner, true);
                    };
                });
        
        // Field-forged Tools
        // This doesn't really have any value, especially with precombat Reinforcement.

        #endregion

        #region 10th-Level

        // Eager Hunter
        yield return new TrueFeat(
                ModData.FeatNames.EagerHunter, 10,
                "You are so eager to reach your prey that every opening propels you forward.",
                $"When you go {onTheHunt.ToLink("On the Hunt")}, you can Step toward the nearest enemy as a {{icon:FreeAction}} free action.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You can Step {icon:FreeAction} towards the nearest enemy after you go On the Hunt.",
                qfFeat =>
                {
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (action.ActionId != ModData.ActionIds.OnTheHunt)
                            return;

                        // Get all my enemies,
                        // taking only the closest ones to me,
                        // and getting their spaces.
                        List<Tile> enemies = qfThis.Owner.Battle.AllCreatures
                            .Where(qfThis.Owner.EnemyOf)
                            .GroupBy(qfThis.Owner.DistanceTo)
                            .OrderBy(grp => grp.Key)
                            .First()
                            .SelectMany(cr => cr.Space.Tiles)
                            .Distinct() // Just in case creatures ever share tiles
                            .ToList();
                        
                        await qfThis.Owner.StrideOrStepAdvancedAsync(
                            "Choose where to Step that's closer to an enemy as part of Eager Hunter.",
                            true, null, true, true, false, null, null,
                            stepTo => enemies.Any(enemy =>
                                stepTo.DistanceTo(enemy) <= qfThis.Owner.DistanceTo(enemy)));
                    };
                });
        
        // Endless Enmity
        yield return new TrueFeat(
                ModData.FeatNames.EndlessEnmity, 10,
                "You are always ready to face a creature that harms you or your allies.",
                "The frequency of Instant Enmity is reduced to once per encounter.",
                [ModData.Traits.Slayer])
            .WithPrerequisite(
                ModData.FeatNames.InstantEnmity,
                "Instant Enmity");
        
        // Ever Vigilant
        yield return new TrueFeat(
                ModData.FeatNames.EverVigilant, 10,
                "You can pursue your prey even when distracted.",
                "You gain an additional reaction each round that can be used only to go On the Hunt.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You have an additional reaction each round to go On the Hunt.",
                qfFeat =>
                {
                    qfFeat.OfferExtraReaction = (qfThis, question, traits) =>
                        question.ToLower().Contains("on the hunt")
                            ? "Ever Vigilant"
                            : null;
                });
        
        // Share Insight

        #endregion

        #region 12th-Level

        // Double Quarry
        yield return new TrueFeat(
                ModData.FeatNames.DoubleQuarry, 12,
                "Your improved preparations allow you to ready your tools for two foes at once.",
                $"You can {markQuarry.ToLink("Mark a Quarry")} twice at the beginning of combat.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You can Mark a Quarry twice at the beginning of combat.",
                qfFeat => { });
        
        // Expanded Spell Slates
        yield return new TrueFeat(
                ModData.FeatNames.ExpandedSpellSlates, 12,
                "You have further expanded your collection of magical tricks.",
                """
                You gain additional innate occult spells that you can cast each once per day.
                • Immediately: 4th-rank
                • 14th level: 5th-rank
                • 16th level: 6th-rank
                """,
                [ModData.Traits.Slayer])
            .WithOnSheet(values =>
            {
                bool hasPanoply = HuntingToolsTag.GetTag(values)
                    ?.IsKnown(ToolId.ConsecratedPanoply) == true;
                
                values.AddSlayerSpellOption(
                    "SpellSlatesSpell4",
                    "Spell Slates level 4 spell",
                    4,
                    hasPanoply);
                values.AddAtLevel(14, values14 =>
                {
                    values14.AddSlayerSpellOption(
                        "SpellSlatesSpell5",
                        "Spell Slates level 5 spell",
                        5,
                        hasPanoply);
                });
                values.AddAtLevel(16, values16 =>
                {
                    values16.AddSlayerSpellOption(
                        "SpellSlatesSpell6",
                        "Spell Slates level 6 spell",
                        6,
                        hasPanoply);
                });
            })
            .WithPrerequisite(
                ModData.FeatNames.SpellSlates,
                "Spell Slates");
        
        // Gouging Strike
        yield return new TrueFeat(
                ModData.FeatNames.GougingStrike, 12,
                "You twist your weapon, gouging your prey deeply and making them vulnerable.",
                "Make a melee Strike. If this Strike hits, you deal an additional die of persistent bleed damage with the same die size as the Strike’s weapon damage dice. The target gains weakness 3 to physical damage until the start of your next turn or until it is no longer taking this persistent bleed damage, whichever comes first.",
                [Trait.Flourish, ModData.Traits.Slayer])
            .WithActionCost(1)
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.AddToOffenseBlock = qfThis =>
                    qfThis.Name!.WithTag("b") + " [flourish] Make a melee Strike. On a hit, deal an additional die of the weapon's dice as persistent bleed damage, and the target gains weakness 3 to physical damage.";

                qfFeat.ProvideStrikeModifier = item =>
                {
                    if (!item.HasTrait(Trait.Melee)
                        || item.WeaponProperties is null)
                        return null;

                    string damageDice = $"1d{item.WeaponProperties.DamageDieSize}";
                    float expectedValue = DiceFormula.FromText(damageDice, null).ExpectedValue;

                    CombatAction gouge = StrikeRules.CreateStrike(
                            qfFeat.Owner,
                            item,
                            RangeKind.Melee,
                            -1)
                        .WithStrikeNameAndIllustrationChange(
                            "Gouging Strike",
                            IllustrationName.BloodVendetta,
                            false)
                        .WithExtraTrait(Trait.Flourish)
                        .WithExtraTrait(0, ModData.Traits.Slayer)
                        .WithAdjustTarget<CreatureTarget>(tar => tar
                            // Deny action if even a crit can't deal enough to overwrite existing bleed.
                            .WithAdditionalConditionOnTargetCreature((_, d) =>
                                NewBleedIsBetter(expectedValue*2, d)
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("Crit isn't stronger than existing bleed"))
                            .WithAdditionalConditionOnTargetCreature((_, d) =>
                                d.WeaknessAndResistance.Immunities.Contains(DamageKind.Bleed)
                                    ? Usability.NotUsableOnThisCreature("Immune to bleed")
                                    : Usability.Usable))
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            if (result < CheckResult.Success)
                                return;
                            
                            // Custom overload which adds source information and returns the applied effect.
                            QEffect? bleed = await CommonSpellEffects.DealAttackRollPersistentDamage(spell, target, result, damageDice, DamageKind.Bleed);

                            if (bleed is null
                                || !target.HasEffect(bleed))
                            {
                                caster.Battle.Log("Target failed to take persistent bleed damage from Gouging Strike.");
                                return;
                            }

                            target.AddQEffect(new QEffect(
                                "Gouged",
                                """
                                You have weakness 3 to physical damage.

                                This ends early if you stop bleeding.
                                
                                """, // Extra line-break for the automated expiration text.
                                ExpirationCondition.ExpiresAtStartOfSourcesTurn,
                                caster,
                                IllustrationName.BloodVendetta)
                            {
                                SourceAction = spell,
                                StateCheck = qfGouge =>
                                {
                                    if (!qfGouge.Owner.HasEffect(bleed))
                                    {
                                        qfGouge.Owner.Battle.Log(
                                            "Gouged effect ends early due to loss of Gouging Strike's persistent bleed damage.",
                                            "Gouging Strike",
                                            """
                                            {i}You twist your weapon, gouging your prey deeply and making them vulnerable.{/i}

                                            Make a melee Strike. If this Strike hits, you deal an additional die of persistent bleed damage with the same die size as the Strike’s weapon damage dice. The target gains weakness 3 to physical damage until the start of your next turn or until it is no longer taking this persistent bleed damage, whichever comes first.
                                            """,
                                            new Traits([ModData.ModTrait, ModData.Traits.Slayer]));
                                        qfGouge.ExpiresAt = ExpirationCondition.Immediately;
                                    }
                                    else
                                        qfGouge.Owner.WeaknessAndResistance
                                            .AddSpecialWeakness(new SpecialResistance(
                                                "physical",
                                                (ca, kind) => kind.IsPhysical(),
                                                3,
                                                null));
                                }
                            });
                        })
                        .With(ca =>
                        {
                            ca.WithDescription(StrikeRules.CreateBasicStrikeDescription4(
                                ca.StrikeModifiers,
                                additionalSuccessText: $"You also deal 1d{item.WeaponProperties!.DamageDieSize} persistent bleed damage. While bleeding or until the start of your next turn, the target gains weakness 3 to physical damage."));
                        });

                    return gouge;

                    // Returns true if the expectedBleed damage is better than any existing bleed (or if none exists).
                    bool NewBleedIsBetter(float expectedBleed, Creature target)
                    {
                        // When a persistent damage effect is applied, it is
                        // only kept if its damage is greater than the existing effect.
                        // So if the effect doesn't exist, this is false, so return true instead.
                        // If the effect does exist but its damage is greater-than-or-equal to
                        // the new bleed, then this returns true, so return false.
                        return !target.HasEffect(qf =>
                            qf.Id is QEffectId.PersistentDamage
                            && qf.GetPersistentDamageKind() is DamageKind.Bleed
                            && (float)qf.Tag! >= expectedBleed);
                    }
                };
            });
        
        // Spectral Lenses

        #endregion

        #region 14th-Level
        
        // Arm Bloodburst Phial
        yield return new HuntingTool(
            "Bloodburst Phial",
            ToolId.BloodburstPhial,
            ToolKind.Secondary,
            ModData.Illustrations.BloodburstPhial,
            (slayer, tool, iTool, trophy, data, isSpecialized) =>
            {
                bool usedUp = slayer.QEffects.Any(qf =>
                    qf.Id == ModData.QEffectIds.ArmBloodburstPhialGranter
                    && qf.UsedUpPermanently);
                
                string persistentDamage = (slayer.Level >= 17 ? 14 : 12) + "d6";
                string action = $"{{b}}Arm Phial {{icon:Action}}{{/b}} [manipulate, relentless] " + $"(Once per encounter) Choose a held weapon to add bonus damage to, or make a bomb. Once this turn, the phial deal an additional {persistentDamage} persistent bleed damage and 12 persistent bleed splash damage.".WithTag(usedUp ? "strike" : null);

                DamageKind? chosenKind = trophy is not null ? Trophies.GetChosenDamageKind(trophy) : null;
                string? kindString = chosenKind?.ToStringOrTechnical().WithColor(chosenKind.Value.DamageKindToColor()) ?? null;
                string reinforced = $"{{b}}Reinforced{{/b}} Your phial deals an additional 1d6 {(chosenKind is null ? "damage and 1 splash damage of one of the trophy's non-physical damage types" : $"{kindString} damage and 1 {kindString} splash damage")}.";
                
                return $"{action}\n{reinforced}";
            },
            (
                "ampoule of volatile monster blood",
                (values, item) =>
                    item.ItemName == HuntingTools.BloodburstPhial)
            )
            .ToSecondaryToolFeat(
                14,
                "This ampoule of volatile monster blood is designed to detonate when attached to a weapon.",
                """
                You gain a bloodburst phial as a secondary tool, which is a worn item. You can designate this item as your bloodburst phial when you Reinforce your Arsenal.

                You gain the {b}Arm Bloodburst Phial {icon:Action}{/b} action, which allows you to attach the phial to a weapon or prepare it as a bomb.
                """,
                [ModData.Traits.Slayer])
            .WithRulesBlockForCombatAction(self =>
                ArmPhialActionForTooltips(self, false, null))
            .WithFreeInventoryItem(HuntingTools.BloodburstPhial)
            .WithOnCreatureHuntingTool(
                ToolId.BloodburstPhial,
                (tool, iPhial, trophy, data, qfTool) =>
                {
                    // The phial is worn and can't move around or be destroyed,
                    // so it's safe to return if the item isn't found.
                    // Consequently, it's also safe to reuse all of the above data references.
                    if (iPhial is null)
                        return null;

                    DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                    
                    qfTool.Id = ModData.QEffectIds.ArmBloodburstPhialGranter;
                    qfTool.ProvideMainAction = qfThis =>
                    {
                        if (qfThis.UsedUpPermanently)
                            return null;
                            
                        return new SubmenuPossibility(
                            ModData.Illustrations.BloodburstPhial,
                            "Arm Bloodburst Phial")
                        {
                            SpellIfAny = ArmPhialActionForTooltips(qfThis.Owner, true, chosenKind),
                            Subsections = [
                                new PossibilitySection("Arm Bloodburst Phial")
                                {
                                    Possibilities = [
                                        new ActionPossibility(ArmPhialAction(qfThis, false, chosenKind))
                                        {
                                            Caption = "Weapon"
                                        },
                                        new ActionPossibility(ArmPhialAction(qfThis, true, chosenKind))
                                        {
                                            Caption = "Bomb"
                                        },
                                    ]
                                }
                                
                            ]
                        };
                    };

                    return qfTool;
                },
                [
                    (_, tool, _, trophy, data) =>
                    {
                        // Extra check against data being null because if there isn't
                        // a chosen kind, there probably isn't any data either, and
                        // I don't want excess warnings in those cases.
                        if (trophy is null || data is null)
                            return null;
                        if (Trophies.GetChosenDamageKind(trophy) is not null)
                            return null;
                        return HuntingTools.ToolWarning(
                            false,
                            "TROPHY DAMAGE TYPE",
                            $"""
                             Your {tool.Id.GetNameFromToolId().WithTag("b")} has a trophy reinforcing it, but no damage type was chosen.

                             This might have been an accident. Ensure that you have reinforced the tool with a damage type. To do so, while in the inventory screen, right-click the designated item with an attached trophy, and click the damage type you want to gain its reinforced benefits for.
                             """);
                    }
                ])
            .With(feat =>
            {
                // "SlayerClass.HuntingTool.BloodburstPhial"
                ModData.FeatNames.ArmBloodburstPhial = feat.FeatName;
            });

        // Open Wound
        yield return new TrueFeat(
                ModData.FeatNames.OpenWound, 14,
                "Your weapons can always find your prey's wounds, guiding your hands.",
                "Creatures that are taking persistent bleed damage are {r:flat-footed}off-guard{/r} to you.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "Creatures who are persistently bleeding are {r:flat-footed}off-guard{/r} to you.",
                qfFeat =>
                {
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            cr.EnemyOf(qfFeat.Owner)
                            && cr.QEffects.Any(qf =>
                                qf.Id is QEffectId.PersistentDamage
                                && qf.GetPersistentDamageKind() is DamageKind.Bleed),
                        qfTech =>
                        {
                            qfTech.IsFlatFootedTo = (_, attacker, _) =>
                                attacker == qfFeat.Owner
                                    ? "Open wound"
                                    : null;
                        });
                });

        #endregion

        #region 16th-Level
        
        // Impenetrable Shelter
        
        // Inferno Vial
        
        // Unerring Edge
        
        // Vicious Spike

        #endregion

        #region 18th-Level
        
        // Obliterate
        
        // Terrifying Bloodlust

        #endregion

        #region 20th-Level

        // Eternal Hunt
        
        // Unbound Hunt

        #endregion
    }

    public static bool IsSlayerSpell(CombatAction action)
    {
        if (action.SpellcastingSource is null)
            return false;
        return IsSlayerSource(action.SpellcastingSource);
    }

    public static bool IsSlayerSource(SpellcastingSource source)
    {
        Trait origin = source.ClassOfOrigin;
        return
            origin == ModData.Traits.Slayer
            || origin == ModData.Traits.ReinforcedSlateSpell;
    }

    extension(CalculatedCharacterSheetValues values)
    {
        /// <summary>
        /// Adds an innate spell selection of the standard tradition options from a slayer feat.
        /// </summary>
        /// <param name="key">The selection option key.</param>
        /// <param name="name">The selection option display name.</param>
        /// <param name="maxRank">The maximum level of chooseable spell. If greater than 0, cantrips are filtered out.</param>
        /// <param name="hasPanoply">If false, you can only select occult spells. If true, you can select occult or divine spells.</param>
        /// <param name="allowLowerRanks">If true, spells lower than maxRank can be selected.</param>
        /// <param name="alternativeSource">If this spell option has a source other than <see cref="ModData.Traits.Slayer"/>, this is that source trait.</param>
        internal void AddSlayerSpellOption(
            string key,
            string name,
            [Range(0,10)]
            int maxRank,
            bool hasPanoply,
            bool allowLowerRanks = false,
            Trait? alternativeSource = null)
        {
            List<Func<Spell,bool>> spellFilters = [];
            
            // Filter out cantrips if it's a non-cantrip choice
            if (maxRank > 0)
            {
                spellFilters.Add(spell => !spell.HasTrait(Trait.Cantrip));
                // Add requirement for exactly this spell level if disallowing lower ranks
                if (!allowLowerRanks)
                    spellFilters.Add(spell => spell.MinimumSpellLevel == maxRank);
            }
            // Allow to include occult or divine if you have consecrated panoply
            if (hasPanoply)
                spellFilters.Add(spell => spell.HasTrait(Trait.Occult) || spell.HasTrait(Trait.Divine));
            else
                spellFilters.Add(spell => spell.HasTrait(Trait.Occult));
        
            // AddSelectionOptionRightNow
            values.AddSelectionOption(new AddInnateSpellOption(
                key,
                name,
                -1,
                alternativeSource ?? ModData.Traits.Slayer,
                maxRank,
                spell => spellFilters.All(filter => filter(spell))));
        }

        /// <summary>
        /// Adds an innate spell selection of a specific tradition from a slayer feat.
        /// </summary>
        /// <param name="key">The selection option key.</param>
        /// <param name="name">The selection option display name.</param>
        /// <param name="maxRank">The maximum level of chooseable spell. This overload is not meant to allow cantrips, but will handle it without triggering exceptions.</param>
        /// <param name="traditions">The allowed traditions of the spell option.</param>
        /// <param name="optionLevel">The <see cref="SelectionOption.OptionLevel"/> to gain the spell at. Default is -1.</param>
        /// <param name="allowLowerRanks">If true, spells lower than maxRank can be selected.</param>
        /// <param name="alternativeSource">If this spell option has a source other than <see cref="ModData.Traits.Slayer"/>, this is that source trait.</param>
        /// <exception cref="Exception">tradition must be Trait.Arcane, Trait.Divine, Trait.Occult, or Trait.Primal.</exception>
        internal void AddSlayerSpellOption(
            string key,
            string name,
            [Range(1,10)]
            int maxRank,
            [AllowedValues(Trait.Arcane, Trait.Divine, Trait.Occult, Trait.Primal)]
            List<Trait> traditions,
            int optionLevel = -1,
            bool allowLowerRanks = true,
            Trait? alternativeSource = null)
        {
            if (traditions.Any(trait => !trait.IsTraditionTrait()))
                throw new Exception("One of the Traits in traditions is not Trait.Arcane, Trait.Divine, Trait.Occult, or Trait.Primal.");
            
            List<Func<Spell,bool>> spellFilters = [
                spell => !spell.HasTrait(Trait.Cantrip),
                spell => traditions.Any(spell.HasTrait) 
            ];
            
            // Filter out cantrips if it's a non-cantrip choice
            if (maxRank > 0)
                spellFilters.Add(spell => !spell.HasTrait(Trait.Cantrip));
            // Add requirement for exactly this spell level if disallowing lower ranks
            else if (!allowLowerRanks)
                spellFilters.Add(spell => spell.MinimumSpellLevel == maxRank);
        
            // AddSelectionOptionRightNow
            values.AddSelectionOption(new AddInnateSpellOption(
                key,
                name,
                optionLevel,
                alternativeSource ?? ModData.Traits.Slayer,
                maxRank,
                spell => spellFilters.All(filter => filter(spell))));
        }
    }

    public static CombatAction ArmPhialActionForTooltips(Creature slayer, bool inCombat, DamageKind? reinforcedDamage)
    {
        string persistentDamage = slayer.Level >= 17
            ? "14d6"
            : "12d6";
        const string splashDamage = "12";

        string? bonusTypeName = reinforcedDamage
            ?.ToStringOrTechnical()
            .ToLower()
            .WithColor(reinforcedDamage.Value.DamageKindToColor());
        
        return new CombatAction(
                slayer,
                ModData.Illustrations.BloodburstPhial,
                "Arm Bloodburst Phial",
                [ModData.ModTrait, Trait.Alchemical, Trait.Manipulate, ModData.Traits.Relentless, ModData.Traits.Slayer],
                $$"""
                  {i}You prepare your bloodburst phial to explode.{/i}
                  
                  {b}Frequency{/b} Once per encounter.
                  
                  Choose how to arm your phial.
                  • {b}Weapon{/b} Choose a weapon you're wielding to arm. If that weapon is a ranged weapon, you can also Reload it as part of this action. The weapon gains the splash trait and deals the phial's damage as additional damage on the next Strike you make with it this turn.
                  • {b}Bomb{/b} (requires a free hand) The phial becomes a temporary alchemical bomb in your hand that you can Strike with once this turn, dealing the listed damage.
                  
                  The phial deals {{persistentDamage}} persistent bleed damage and {{splashDamage}} persistent bleed splash damage (you are immune to this splash damage){{(inCombat && reinforcedDamage.HasValue
                      ? $", plus an additional 1d6 {bonusTypeName} damage and 1 {bonusTypeName} splash damage"
                      : ".\n\n{b}Reinforced{/b} Your bloodburst phial deals an additional 1d6 damage and 1 splash damage of one of the trophy's non-physical damage types")}}.
                  """,
                Target.Self())
            .WithActionCost(1);
    }

    public static CombatAction ArmPhialAction(QEffect phialQf, bool isBomb, DamageKind? reinforcedDamage)
    {
        Creature slayer = phialQf.Owner;
        string persistentDamage = (slayer.Level >= 17 ? 14 : 12) + "d6";
        const int persistentSplashDamage = 12;
        string? bonusTypeName = reinforcedDamage
            ?.ToStringOrTechnical()
            .ToLower()
            .WithColor(reinforcedDamage.Value.DamageKindToColor());
        
        CombatAction armPhial = new CombatAction(
                slayer,
                ModData.Illustrations.BloodburstPhial,
                $"Arm Bloodburst Phial ({(isBomb ? "bomb" : "weapon")})",
                [ModData.ModTrait, Trait.Alchemical, Trait.Manipulate, ModData.Traits.Relentless, ModData.Traits.Slayer],
                $$"""
                {i}You prepare your bloodburst phial to explode.{/i}
                
                {b}Frequency{/b} Once per encounter.
                {{(isBomb
                    ? "{b}Requirements{/b} You have a free hand.\n\nArm your phial as an alchemical bomb.\n\nUntil the end of this turn, it deals"
                    : "\nChoose a weapon you're holding to arm. If that weapon is a ranged weapon, you can also Reload it as part of this action.\n\nYour next Strike with the weapon this turn gains the splash trait and deals an additional")}} {{persistentDamage}} persistent bleed damage and {{persistentSplashDamage}} persistent bleed splash damage (you are immune to this splash damage){{(reinforcedDamage.HasValue
                    ? $", plus an additional 1d6 {bonusTypeName} damage and 1 {bonusTypeName} splash damage"
                    : ".\n\n{b}Reinforced{/b} Your bloodburst phial deals an additional 1d6 damage and 1 splash damage of one of the trophy's non-physical damage types")}}.
                """,
                Target.Self())
            .WithActionCost(1)
            .WithSoundEffect(SfxName.ItemAction)
            .WithEffectOnSelf(async (arm, self) =>
            {
                // This is either the bomb item manager, or the strike-buffer.
                // Either way, this effect also applies the persistent splash damage.
                QEffect armQf;
                Item? phial;
                
                if (isBomb)
                {
                    phial = AlchemicalItems.CreateBomb(
                        HuntingTools.BloodburstPhial,
                        arm.Illustration,
                        "bloodburst phial",
                        [ModData.Traits.Slayer],
                        "0",
                        DamageKind.Bleed, // Used for base and persistent kind
                        SfxName.Throw,
                        persistentDamage, // the Xd6 persistent damage on the target
                        wp =>
                        {
                            // If it exists, add the trophy's 1d6 direct and 1 splash damage.
                            if (reinforcedDamage.HasValue)
                            {
                                wp.DamageKind = reinforcedDamage.Value;
                                wp.DamageDieCount = 1; // 1d6 direct
                                wp.DamageDieSize = 6; // 1d6 direct
                                wp.AdditionalSplashDamageFormula = "1"; // 1 splash
                                wp.AdditionalSplashDamageKind = reinforcedDamage.Value; // 1 splash
                            }
                            // Otherwise, null. It only deals the direct persistent and splash persistent damage.
                            else
                            {
                                wp.DamageKind = DamageKind.Untyped;
                                wp.AdditionalSplashDamageFormula = null;
                                wp.AdditionalSplashDamageKind = DamageKind.Untyped;
                            }
                        });
                    phial.ProsaicName = "bloodburst phial"; // Undo item tier subname
                    phial.ShortName = "bloodburst phial";
                    phial.Traits.Insert(0, ModData.ModTrait);
                    phial.Price = 0;
                    // If no instant (direct and splash) damage from trophy exists,
                    // then this won't attempt to deal 0 damage with applied modifiers.
                    // It will still deal the additional Xd6 persistent bleed, though.
                    if (!reinforcedDamage.HasValue)
                        phial.Traits.Add(Trait.ThisNaturalAttackDealsNoDamage);
                    armQf = QEffect.EvaporateItemAtStartOfYourNextTurn(self, phial)
                        .WithExpirationAtEndOfThisTurn();
                    self.AddHeldItem(phial);
                }
                else
                {
                    phial = await self.AskForChoiceAmongItems(
                        arm.Illustration,
                        "{b}Arm Bloodburst Phial {icon:Action}{/b}\nChoose a weapon to arm with your phial for the rest of this turn.",
                        self.Weapons
                            .Where(item =>
                                !item.HasTrait(Trait.Unarmed)
                                && !item.HasTrait(Trait.Bomb))
                            .ToList(),
                        true);
                    
                    if (phial is null)
                    {
                        arm.RevertRequested = true;
                        return;
                    }
                    
                    List<string> damages =
                    [
                        $"{persistentDamage} persistent bleed damage",
                        $"{persistentSplashDamage} persistent bleed splash damage",
                    ];
                    if (reinforcedDamage.HasValue)
                    {
                        damages.Add($"1d6 {bonusTypeName} damage");
                        damages.Add($"1 {bonusTypeName} splash damage");
                    }
                    
                    armQf = new QEffect(
                        $"Armed Phial ({phial.Name})",
                        $"""
                         Yur next Strike with your {phial.Name} gains the splash trait and deals an additional {S.ConstructOrList(damages, "and")}.

                         You are immune to this splash damage.
                         """,
                        ExpirationCondition.ExpiresAtEndOfYourTurn,
                        self,
                        arm.Illustration);
                    
                }
                
                armQf.With(qf =>
                {
                    qf.Tag = phial;
                    // Immunity to all damage dealt by your own bomb.
                    qf.YouAreDealtDamageEvent = async (qfArm, dEvent) =>
                    {
                        if (dEvent.CombatAction?.Item != phial)
                            return;
                        //dEvent.KindedDamages.ForEach(kd => kd.ResolvedDamage = 0);
                        dEvent.ReduceBy(dEvent.TotalResolvedDamage, "Immune to splash from bloodburst phial");
                    };
                    // Clean up strike
                    qf.AdjustStrikeAction = (qfArm, action) =>
                    {
                        if (action.Item != phial)
                            return;

                        // Deal 12 persistent splash after all other damage resolves
                        action.WithEffectOnChosenTargets(async (strike, caster, targets) =>
                        {
                            CheckResult result = strike.CheckResult;
                            
                            // Is splash, so don't deal on a fumble
                            if (result < CheckResult.Failure)
                                return;
                            
                            Creature target = targets.ChosenCreature!;
                            int range = await DetermineSplashRadius();
                            
                            // Play splash animation.
                            // Skip for a bomb with an existing animation.
                            if (!reinforcedDamage.HasValue || !strike.HasTrait(Trait.Bomb))
                            {
                                List<Tile> splashedTiles = target.Battle.Map.AllTiles
                                    .Where(tl =>
                                        target.DistanceTo(tl) <= range
                                        && tl.PrimaryOccupant != target)
                                    .ToList();
                                List<Particle> projectiles = [];
                                foreach (Tile splashedTile in splashedTiles)
                                {
                                    projectiles.AddRange(self.Battle.SpawnOvercreatureProjectileParticles(
                                        10, target, splashedTile, Color.White, arm.Illustration));
                                }
                                await self.Battle.WaitForProjectiles(projectiles);
                            }

                            // Deal 12 persistent splash damage
                            await PerformSplashAreaDamage(
                                strike,
                                target,
                                range,
                                persistentSplashDamage.ToString(),
                                "Splash damage (bloodburst phial)",
                                DamageKind.Bleed,
                                true);

                            // Remove effect
                            qfArm.ExpiresAt = ExpirationCondition.Immediately;
                        });

                        // Adjust buffed weapon to add phial damage.
                        // - Xd6 persistent bleed damage
                        // - 1 typed splash damage
                        if (!action.HasTrait(Trait.Bomb))
                        {
                            if (!action.HasTrait(Trait.Splash))
                                action.WithExtraTrait(Trait.Splash);
                            action.WithEffectOnEachTarget(async (strike, caster, target, result) =>
                            {
                                // Do nothing on a fumble
                                if (result < CheckResult.Failure)
                                    return;
                                
                                // Do Xd6 persistent bleed damage to the target
                                await CommonSpellEffects.DealAttackRollPersistentDamage(
                                    target,
                                    result,
                                    persistentDamage,
                                    DamageKind.Bleed);
                                
                                // Do 1 typed splash damage
                                if (reinforcedDamage.HasValue)
                                    await PerformSplashAreaDamage(
                                        strike,
                                        target,
                                        await DetermineSplashRadius(),
                                        "1",
                                        "Splash damage (bloodburst phial)",
                                        reinforcedDamage.Value,
                                        false);
                            });
                        }

                        if (action.SoundEffectName is SfxName.Throw
                            || action.Item.WeaponSuccessfulHitSfxName is SfxName.Throw
                            || action.Item.WeaponProperties!.Sfx is SfxName.Throw
                            || !action.Item.HasTrait(Trait.Bomb))
                        {
                            // Play sound effect immediately on hit because
                            // the action Sfx was Throw.
                            action.EffectOnOneTarget = Delegates.SmartCombineDelegates(
                                async (_, _, _, _) =>
                                    Sfxs.Play(SfxName.RayOfFrost),
                                action.EffectOnOneTarget!);
                        }
                        
                        if (action.Item.HasTrait(Trait.Bomb))
                        {
                            // Fix description for the bomb iteration
                            List<string> damages =
                            [
                                $"{persistentDamage} persistent bleed damage",
                                $"{persistentSplashDamage} persistent bleed splash damage",
                            ];
                            if (action.Description.Contains("Success"))
                            {
                                action.Description = action.Description.Replace(
                                    ".\n{b}Critical",
                                    $", plus {persistentSplashDamage} persistent bleed splash damage.\n{{b}}Critical");
                            }
                            else
                            {
                                action.Description +=
                                    $"{{b}}Success{{/b}} You deal {string.Join(", plus ", damages)}."
                                    + "\n{b}Critical success{/b} Double damage.";
                            }
                        }
                    };

                    // Adjust buffed weapon to add phial damage.
                    // - 1d6 typed damage
                    if (!phial.HasTrait(Trait.Bomb) && reinforcedDamage.HasValue)
                    {
                        qf.AddExtraStrikeDamage = (strike, target) =>
                        {
                            if (strike.Item != phial)
                                return null;
                            
                            return (
                                DiceFormula.FromText(
                                    "1d6",
                                    "Reinforced bloodburst phial"),
                                reinforcedDamage.Value);
                        };
                    }

                    return;

                    // Can be expanded on as necessary
                    async Task<int> DetermineSplashRadius() => 1;

                    async Task PerformSplashAreaDamage(
                        CombatAction strike,
                        Creature strikeTarget,
                        int range,
                        string damageExpression,
                        string? source,
                        DamageKind kind,
                        bool isPersistent)
                    {
                        CheckResult result = strike.CheckResult;

                        if (result < CheckResult.Failure)
                            return;
                        
                        // Set splash targets.
                        List<Creature> splashTargets;
                        if (result < CheckResult.Success)
                        {
                            // Only affect target
                            splashTargets = [strikeTarget];
                        }
                        else
                        {
                            // Get other creatures. Not full splash integration.
                            splashTargets = strikeTarget.Battle.AllCreatures
                                .Where(cr =>
                                    cr.DistanceTo(strikeTarget) <= range
                                    // Target was already hit with Xd6.
                                    && cr != strikeTarget
                                    // You are immune to your splash
                                    && cr != strike.Owner)
                                .ToList();
                        }

                        // Deal persistent damage
                        foreach (Creature cr in splashTargets)
                        {
                            if (isPersistent)
                            {
                                await CommonSpellEffects.DealAttackRollPersistentDamage(
                                    cr,
                                    // This is splash persistent, so it's not doubled
                                    result > CheckResult.Success ? CheckResult.Success : result,
                                    damageExpression,
                                    kind);
                                cr.Battle.Log($"{cr.Name} takes {damageExpression.WithTag("b")} persistent {kind.ToStringOrTechnical().ToLower()} damage.");
                            }
                            else
                                await CommonSpellEffects.DealTrueDirectSplashDamage(
                                    strike,
                                    DiceFormula.FromText(damageExpression, source),
                                    cr,
                                    kind);
                        }
                    }
                });
                
                self.AddQEffect(armQf);

                // Only added so that I could trigger it later, not for it to trigger twice.
                phialQf.UsedUpPermanently = true;
            });

        if (isBomb)
            armPhial.WithAdjustTarget<SelfTarget>(tar => tar
                .WithAdditionalRestriction(self =>
                    self.HasFreeHand
                        ? null
                        : Usability.CommonReasons.NoFreeHand.UnusableReason));
        
        return armPhial;
    }
}