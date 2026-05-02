using System.Reflection;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.LoresAndWeaknesses;

namespace Dawnsbury.Mods.SlayerClass;

public static class ClassFeats
{
    public static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        yield return new TrueFeat(
            ModManager.RegisterFeatName("SlayerEmptyFeat1", "No Feat"),
            1,
            "Do nothing.", "Temporary until more feats are implemented.",
            [ModData.Traits.Slayer]);
        for (int i = 2; i < 22; i+=2)
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
                ModData.FeatNames.Bloodscent,
                1,
                "With a glance, you can judge how close your target is to falling.",
                $"The {RecallWeakness.GetActionLink()} action gains the relentless trait for you. You can also use Recall Weakness as a {{icon:FreeAction}} free action if the target is your quarry or is taking persistent bleed damage.",
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
                        recall.Traits = new Traits([ModData.Traits.ModName, ..recall.Traits], recall);

                        return new ActionPossibility(recall);
                    };
                });
        
        // Crossbow Slayer
        yield return new TrueFeat(
                ModData.FeatNames.CrossbowSlayer,
                1,
                "You find that a crossbow's versatility is the perfect companion to your own, and you eagerly reload it to get back in the fight.",
                """
                Reloading gains the relentless trait for you.
                
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

                    if (HuntingTools.GetTool(qfFeat.Owner, HuntingTools.ToolId.ConsecratedPanoply)
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
                });
        
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
                        HuntingTools.GetToolId(item) is HuntingTools.ToolId.RepellingShield);
                    Item? trophy = shield is not null ? Trophies.GetTrophy(shield) : null;
                    List<DamageKind>? kinds = trophy is not null
                        ? Trophies.GetTrophyData(trophy)?.Kinds
                        : [];
                    string kindDescription = kinds?.Count > 0
                        ? S.ConstructOrList(
                            kinds.Select(kind =>
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
                QEffect repellQF = new QEffect()
                {
                    // Debugging identifier
                    Name = "[HUNTING TOOL: REPELLING SHIELD]",
                    BonusToDefenses = (qfThis, action, def) =>
                        HuntingTools.GetTool(qfThis.Owner, HuntingTools.ToolId.RepellingShield) is {} shield
                        && MoreShields.CommonShieldRules.GetRaisedShields(qfThis.Owner).Any(shield.IsMyTool)
                        && action?.Owner is {} foe
                        && Slayer.IsMyQuarry(qfThis.Owner, foe)
                        && def is Defense.Reflex
                        && (action.ChosenTargets.ChosenTile is not null || action.ChosenTargets.ChosenTiles.Count > 0)
                            ? new Bonus(2, BonusType.Circumstance, "Repelling shield", true)
                            : null,
                    YouAreDealtDamageEvent = async (qfThis, @event) =>
                    {
                        // Use regular Shield Block for physical triggers
                        if (@event.KindedDamages[0].DamageKind.IsPhysical())
                            return;
                        if (HuntingTools.GetTool(qfThis.Owner, HuntingTools.ToolId.RepellingShield) is not { } shield
                            || MoreShields.CommonShieldRules.GetRaisedShields(qfThis.Owner).FirstOrDefault(shield.IsMyTool) is not {} iShield
                            || @event.CombatAction is not { } action
                            || !action.HasTrait(Trait.Attack)
                            || action.ActionId == ActionId.Trip
                            || Trophies.GetTrophy(iShield) is not {} trophy
                            || Trophies.GetTrophyData(trophy)?.Kinds is not { } kinds
                            || !@event.KindedDamages.Any(kd => kinds.Contains(kd.DamageKind)))
                            return;
                        
                        (await MoreShields.CommonShieldRules.OfferAndMakeShieldBlock(
                                @event.Source,
                                @event.TargetCreature,
                                new DamageStuff(
                                    @event.KindedDamages.Sum(n => n.ResolvedDamage),
                                    @event.CombatAction,
                                    @event.KindedDamages[0].DamageKind),
                                @event.TargetCreature,
                                shield.IsMyTool))
                            ?.Apply(@event);
                    },
                };
                self.AddQEffect(repellQF);
            });
        
        // Spiked Surcoat
        
        // Sudden Pounce
        
        // Paired Bloodseeker
        
        // Peculiar Weaponry
        yield return new TrueFeat(
                    ModData.FeatNames.PeculiarWeaponry,
                    1,
                    "You specialize in an unusual weapon, whether a common soldier's armament or a unique tool few can use.",
                    $$"""
                    If your bloodseeking blade signature tool is a simple weapon, increase its damage die size by one step.

                    Your bloodseeking blade signature tool can be an advanced weapon, in addition to simple or martial, and you treat any advanced weapon you've designated as your signature tool as if it were a martial weapon for the purposes of proficiency {i}({{ModData.Illustrations.DdSun.IllustrationAsIconString}} your proficiency won't display in your inventory, but works in combat){/i}.
                    """,
                    [ModData.Traits.Slayer])
            .WithPrerequisite(
                values => HuntingTools.GetTool(values, HuntingTools.ToolId.BloodseekingBlade) is not null,
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
                    if (HuntingTools.GetTool(qfFeat.Owner, HuntingTools.ToolId.BloodseekingBlade)
                        is not { } blade)
                        return;
                        
                    if (qfFeat.Owner.AllItems.FirstOrDefault(blade.IsMyTool) is {} bladeItem
                        && !bladeItem.Traits.Contains(ModData.Traits.BloodseekingBlade))
                        bladeItem.Traits.Add(ModData.Traits.BloodseekingBlade);

                    qfFeat.IncreaseItemDamageDie = (qfThis, item) =>
                        blade.IsMyTool(item) && item.HasTrait(Trait.Simple);
                });

        #endregion

        #region 2nd-Level

        // Instant Enmity
        // DOC: Since you can usually Mark a Quarry, this restores your previous marks once the Instant one dies.
        yield return new TrueFeat(
                ModData.FeatNames.InstantEnmity,
                2,
                "You focus your hunt on an unexpected but loathsome foe.",
                """
                {b}Frequency{/b} once per day
                {b}Trigger{/b} You see a creature of your level or higher take a hostile action against you or one of your allies.

                The triggering creature becomes your quarry for the rest of the encounter, replacing any quarry you currently have (if any) until it dies. {Red}You can't Claim a Trophy{/Red} from a quarry you mark this way.
                """,
                [ModData.Traits.Slayer])
            .WithActionCost(-2)
            .WithPermanentQEffect(
                "{Green}Once per day{/Green}, you can mark a creature taking hostile actions against your party as your quarry, replacing any existing quarry. You can't Claim their Trophy.",
                qfFeat =>
                {
                    if (qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.InstantEnmity))
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
                                    || qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Contains(ModData.PersistentActions.InstantEnmity))
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
                                qfFeat.Owner.PersistentUsedUpResources.UsedUpActions.Add(ModData.PersistentActions.InstantEnmity);
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
                                    new Traits([..instantEnmity.Traits]));
                            };
                        });
                });
        
        // Pack Slayer
        yield return new TrueFeat(
                ModData.FeatNames.PackSlayer,
                2,
                "You know that even lesser monsters make for worthy prey in enough numbers.",
                $"You can {markQuarry.ToLink("Mark as your Quarry")} a group of at least three creatures that share a name, even if their level is lower than yours. You can only {claimTrophy.ToLink("Claim a Trophy")} from this group once.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You can mark lower-level groups of 3+ as your quarry.",
                qfFeat => { });
        
        // Personalized Gear
        
        // Salt Stone
        
        // Shifting Hunt
        
        // Slayer's Tricks
        /*yield return new TrueFeat(
                ModData.FeatNames.SlayersTricks,
                2,
                );*/

        #endregion

        #region 4th-Level
        
        // Apply Spirit Oil

        // Blood for Blood
        yield return new TrueFeat(
                ModData.FeatNames.BloodForBlood,
                4,
                "You viciously return your foe’s attack, reinvigorating yourself with your vengeance.",
                """
                {b}Requirements{/b} A creature critically hit you with an attack since the end of your previous turn.

                Strike the required creature. On a hit, you gain temporary Hit Points equal to your level.
                """,
                [Trait.Flourish, Trait.Rebalanced, ModData.Traits.Slayer])
            .WithActionCost(1)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    int levelTemp = qfFeat.Owner.Level;
                    
                    qfFeat.WithDisplayActionInOffenseSection(
                        "Blood for Blood",
                        $"[flourish] Strike a foe who critically hit you last round. If you hit, you gain {{Blue}}{levelTemp}{{/Blue}} temp HP.");

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
                                [ModData.Traits.ModName, Trait.Flourish, ModData.Traits.Slayer, Trait.Basic],
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
                                        crits.Contains,
                                        strike =>
                                            strike.WithEffectOnEachTarget(async (_, _, _, result) =>
                                            {
                                                if (result < CheckResult.Success)
                                                    return;
                                                caster.GainTemporaryHP(levelTemp);
                                            }),
                                        action.Illustration,
                                        null,
                                        true,
                                        "Pass",
                                        false))
                                {
                                    action.RevertRequested = true;
                                }
                            });

                        return new ActionPossibility(bloodReply);
                    };
                });
        
        // Blood Rush
        yield return new TrueFeat(
                ModData.FeatNames.BloodRush,
                4,
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
                ModData.FeatNames.ExpansivePanoply,
                4,
                "While a short, sharp piece of metal is a remarkably versatile tool, you know that it is not appropriate for every occasion.",
                // clubs, darts, or shortswords.
                $"When you use {{b}}Hunting Spike {{icon:Action}}{{/b}}, you can draw and Strike with spikes that function as {ItemName.Club.ToLink("clubs")} or {ItemName.Shortsword.ToLink("shortswords")}, rather than {ItemName.Dagger.ToLink("daggers")}.",
                [ModData.Traits.Slayer])
            .WithPrerequisite(
                values => HuntingTools.GetTool(values, HuntingTools.ToolId.ConsecratedPanoply) is not null,
                "You must know the consecrated panoply signature tool.")
            .WithPermanentQEffect(
                "Your hunting spikes can also be clubs or shortswords.",
                qfFeat => {});

        #endregion

        #region 6th-Level
        
        // Final Flourish
        yield return new TrueFeat(
                ModData.FeatNames.FinalFlourish,
                6,
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
                                [ModData.Traits.ModName, Trait.Flourish, ModData.Traits.Slayer],
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
                ModData.FeatNames.DefensiveHunt,
                8,
                "Even in a moment of danger, you turn weakness into opportunity.",
                """
                {b}Trigger{/b} You are critically hit by your quarry.

                You go On the Hunt as a {icon:FreeAction} free action.
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
                ModData.FeatNames.EagerHunter,
                10,
                "You are so eager to reach your prey that every opening propels you forward.",
                "When you go On the Hunt, you can Step toward the nearest enemy as a free action.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You can Step after you go On the Hunt.",
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
                            "Choose where to Step.",
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
                ModData.FeatNames.DoubleQuarry,
                12,
                "Your improved preparations allow you to ready your tools for two foes at once.",
                $"You can {markQuarry.ToLink("Mark a Quarry")} twice at the beginning of combat.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "You can Mark a Quarry twice at the beginning of combat.",
                qfFeat => { });

        #endregion

        #region 14th-Level

        // Open Wound
        yield return new TrueFeat(
                ModData.FeatNames.OpenWound,
                14,
                "Your weapons can always find your prey's wounds, guiding your hands.",
                "Creatures that are taking persistent bleed damage are off-guard to you.",
                [ModData.Traits.Slayer])
            .WithPermanentQEffect(
                "Creatures who are persistently bleeding are off-guard to you.",
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



        #endregion

        #region 18th-Level



        #endregion

        #region 20th-Level



        #endregion
    }
}