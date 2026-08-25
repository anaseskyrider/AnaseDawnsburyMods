using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.LoresAndWeaknesses;
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
        for (int i = 10; i <= 20; i+=2)
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

                    if (HuntingToolsTag.GetTool(qfFeat.Owner, HuntingTools.ToolId.ConsecratedPanoply)
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
                HuntingTools.ToolId.RepellingShield,
                HuntingTools.ToolKind.Secondary,
                ModData.Illustrations.RepellingShield,
                (self, _) =>
                {
                    var inventory = self.HeldItems
                        .Concat(self.CarriedItems)
                        .Append(self.BaseArmor ?? self.Armor.Item ?? null)
                        .WhereNotNull()
                        .ToList();
                    Item? shield = inventory.FirstOrDefault(item =>
                        HuntingTool.GetToolId(item) is HuntingTools.ToolId.RepellingShield);
                    Item? trophy = shield is not null ? Trophies.GetTrophy(shield) : null;
                    List<DamageKind>? kinds = trophy is not null
                        ? Trophies.GetTrophyData(trophy)?.Kinds
                        : [];
                    string kindDescription = kinds?.Count > 0
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
            .WithOnCreature(self =>
            {
                (HuntingTool? repShield, Item? iShield, Item? trophy, var trophyData) =
                    HuntingTools.GetFullHuntingToolData(self, HuntingTools.ToolId.RepellingShield);
                if (repShield is null || iShield is null)
                    return;
                
                QEffect repellQF = new QEffect()
                {
                    // Debugging identifier
                    Name = "[HUNTING TOOL: REPELLING SHIELD]",
                    ModifyActionPossibility = (qfThis, action) =>
                    {
                        if (action.ActionId != ActionId.RaiseShield
                            || action.Item is null
                            || !repShield.IsMyTool(action.Item))
                            return;

                        action.Description += "\n\n{b}Repelling Shield{/b} You also gain a +2 circumstance bonus to Reflex saving throws against area effects created by your quarry.".WithColor("Blue");

                        if (trophyData?.Kinds is null || trophyData.Value.Kinds.Count == 0)
                            return;
                        
                        action.Description += $"\n\n{{b}}Reinforced{{/b}} You can Shield Block with your repelling shield in response to any attack that deals {S.ConstructOrList(trophyData.Value.Kinds.Select(dk => dk.ToStringOrTechnical()))} damage.".WithColor("Blue");
                    },
                    BonusToDefenses = (qfThis, action, def) =>
                        def is Defense.Reflex
                        && (action?.ChosenTargets.ChosenTile is not null || action?.ChosenTargets.ChosenTiles.Count > 0)
                        && CommonShieldRules.GetRaisedShields(qfThis.Owner).Contains(iShield)
                        && Slayer.IsMyQuarry(qfThis.Owner, action.Owner)
                            ? new Bonus(2, BonusType.Circumstance, "Repelling shield", true)
                            : null
                };

                if (trophy is null || trophyData?.Kinds is null)
                    return;

                repellQF.YourShieldBlockWorksAlsoAgainst = (qfThis, dEvent) =>
                    dEvent.CombatAction is { } action
                    && action.HasTrait(Trait.Attack)
                    && action.ActionId != ActionId.Trip
                    && (qfThis.Owner.HasFeat(FeatName.ReactiveShield)
                        || CommonShieldRules.GetBlockableShields(qfThis.Owner).Contains(iShield))
                    && dEvent.KindedDamages.Any(kd => trophyData.Value.Kinds.Contains(kd.DamageKind));
                
                self.AddQEffect(repellQF);
            })
            .WithInappropriateBecauseOfBadInventory(FeatInventoryRequirements.RequiresShield);
        
        // Spiked Surcoat
        
        // Sudden Pounce
        // Requires More Basic Actions
        
        // Paired Bloodseeker
        yield return new HuntingTool(
                "Paired Bloodseeker",
                HuntingTools.ToolId.PairedBloodseeker,
                HuntingTools.ToolKind.Secondary,
                ModData.Illustrations.BloodseekingBlade,
                (self, isSpecialized) =>
                {
                    //var inventory = self.PersistentCharacterSheet?.Inventory.AllItems;
                    var inventory = self.HeldItems
                        .Concat(self.CarriedItems)
                        .Append(self.BaseArmor ?? self.Armor.Item ?? null)
                        .WhereNotNull()
                        .ToList();
                    Item? pair = inventory.FirstOrDefault(item =>
                            HuntingTool.GetToolId(item) is HuntingTools.ToolId.PairedBloodseeker);
                    string? ignoreAmount = pair is not null ? (1 + pair.WeaponProperties!.DamageDieCount).WithColor("Blue") : null;
                    Item? trophy = pair is not null ? Trophies.GetTrophy(pair) : null;
                    DamageKind? chosenDk = trophy is not null ? Trophies.GetChosenDamageKind(trophy) : null;
                    string damageType = chosenDk is not null
                        ? (" " + chosenDk.Value.ToStringOrTechnical().WithColor(chosenDk.Value.DamageKindToColor() ) + " ")
                        : " ";
                    return $$"""
                             {b}Bloody Fuller{/b} Against your quarry, you ignore {{(ignoreAmount is null ? "an amount" : ignoreAmount + " points")}} of {{(isSpecialized ? "{Blue}any{/Blue}" : "physical")}} resistance to this tool's damage{{(ignoreAmount is null ? " equal to 1 + the number of weapon damage dice" : null)}}.
                             {b}Reinforced{/b} Your first Strike with this tool deals {Blue}{{(self.Level >= 19 ? 3 : self.Level >= 11 ? 2 : 1)}}d4{/Blue} additional{{damageType}}damage.{{(chosenDk is null ? " The type is chosen from the reinforcing trophy." : null)}}
                             """
                           + (isSpecialized
                               ? $"\n{{b}}Specialized{{/b}} This tool has {{tooltip:criteffect}}critical specialization effects{{/}}, and gains the effects of a {(self.PersistentCharacterSheet?.Calculated.GetTagOrNull<ItemName>(HuntingTools.PAIRED_BLOODSEEKER_RUNESTONE_KEY) is {} rune ? ("{i}" + Items.GetItemTemplate(rune).RuneProperties!.Prefix + "{/i} property rune").WithColor("Blue") : "property rune you choose when you Reinforce your Arsenal")}."
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
                HuntingTools.ToolId.PairedBloodseeker,
                HuntingTools.PAIRED_BLOODSEEKER_RUNESTONE_KEY,
                Dice.D4)
            .WithPrerequisite(
                values => HuntingToolsTag.GetTool(values, HuntingTools.ToolId.BloodseekingBlade) is not null,
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
                    $$"""
                    If your bloodseeking blade signature tool is a simple weapon, increase its damage die size by one step.

                    Your bloodseeking blade signature tool can be an advanced weapon, in addition to simple or martial, and you treat any advanced weapon you've designated as your signature tool as if it were a martial weapon for the purposes of proficiency {i}({{ModData.Illustrations.DdSun.IllustrationAsIconString}} your proficiency won't display in your inventory, but works in combat){/i}.
                    """,
                    [ModData.Traits.Slayer])
            .WithPrerequisite(
                values => HuntingToolsTag.GetTool(values, HuntingTools.ToolId.BloodseekingBlade) is not null,
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
                    if (HuntingToolsTag.GetTool(qfFeat.Owner, HuntingTools.ToolId.BloodseekingBlade)
                        is not { } blade)
                        return;
                        
                    if (qfFeat.Owner.AllItems.FirstOrDefault(blade.IsMyTool) is {} bladeItem
                        && !bladeItem.Traits.Contains(ModData.Traits.BloodseekingBlade))
                        bladeItem.Traits.Add(ModData.Traits.BloodseekingBlade);

                    qfFeat.IncreaseItemDamageDie = (qfThis, item) =>
                        blade.IsMyTool(item) && item.HasTrait(Trait.Simple);
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
                    if (qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.INSTANT_ENMITY))
                        qfFeat.Description = qfFeat.Description!.Replace(
                            "{Green}Once per day{/Green}",
                            "{Red}Once per day{/Red}");
                    
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
                                qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.INSTANT_ENMITY);
                                qfFeat.Description = qfFeat.Description!.Replace(
                                    "{Green}Once per day{/Green}",
                                    "{Red}Once per day{/Red}");
                                
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
                $"You draw your salt stone, a small block of dried magical compounds, and scrape it along a weapon you’re holding{ModData.Tooltips.SaltStoneHolding(ModData.Illustrations.InfoSymbol.IllustrationAsIconString)}.",
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
                """
                    You gain two common occult cantrips as innate spells. Your spellcasting attribute modifier for these spells and any other spells you gain from slayer feats is Wisdom, rather than Charisma. Casting a Spell gains the relentless trait for you, as long as the spell you cast came from a slayer feat.
                    
                    {b}Special{/b} If you have a consecrated panoply signature tool, you can choose divine spells rather than occult spells for this feat and for any other slayer feats that allow you to choose innate spells.
                    """,
                    [ModData.Traits.Slayer])
            .WithPermanentQEffect(qfFeat =>
            {
                qfFeat.ModifyActionPossibility = (qfThis, action) =>
                {
                    if (action.SpellcastingSource?.ClassOfOrigin != ModData.Traits.Slayer)
                        return;
                    
                    action.WithExtraTrait(ModData.Traits.Relentless);
                    action.SpellcastingSource.SpellcastingAbility = Ability.Wisdom;
                };
            })
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Spell, Proficiency.Trained);
                values.InnateSpells.GetOrCreate(
                    ModData.Traits.Slayer,
                    () => new InnateSpells(Trait.Occult));
                
                bool hasPanoply = HuntingToolsTag.GetTag(values)
                    ?.IsKnown(HuntingTools.ToolId.ConsecratedPanoply) == true;
                
                values.AddSelectionOption(new AddInnateSpellOption(
                    "SlayersTricksCantrips1",
                    "Slayer's Tricks cantrip 1",
                    -1,
                    ModData.Traits.Slayer,
                    0,
                    spell =>
                        spell.HasTrait(Trait.Occult)
                        || (hasPanoply && spell.HasTrait(Trait.Divine))));
                values.AddSelectionOption(new AddInnateSpellOption(
                    "SlayersTricksCantrips2",
                    "Slayer's Tricks cantrip 2",
                    -1,
                    ModData.Traits.Slayer,
                    0,
                    spell =>
                        spell.HasTrait(Trait.Occult)
                        || (hasPanoply && spell.HasTrait(Trait.Divine))));
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
                values => HuntingToolsTag.GetTool(values, HuntingTools.ToolId.ConsecratedPanoply) is not null,
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
        
        // Wall of Will

        #endregion

        #region 8th-Level
        
        // Armored Fortress
        
        // Catalyzing Flask
        
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
        
        // Ever Vigilant
        
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
        
        // Gouging Strike
        
        // Spectral Lenses

        #endregion

        #region 14th-Level
        
        // Arm Bloodburst Phial

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
}