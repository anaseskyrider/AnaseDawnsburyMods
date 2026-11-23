using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.MoreShields;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class QuickRepair
{
    public static bool MoreShieldsIsLoaded { set; get; }

    public static string FeatUsages =
        $$"""
          • {b}Heal a Construct {icon:Action}{/b} Restore Hit Points to a construct within reach. This works as the {{FeatName.BattleMedicine.ToLink("Battle Medicine {icon:Action}")}} feat, except that the immunity to Quick Repair is not shared with Battle Medicine.
          • {b}Repair Armor {icon:Action}{/b} (requires expert in Crafting) Remove the broken armor condition, as imposed by the {{FeatName.SacrificeArmor.ToLink("Sacrifice Armor")}} feat, from yourself or an ally within reach with a DC 20 check. On any result, the armor becomes immune to this usage of Quick Repair for 1 day.
          • {b}Repair Shield {icon:ThreeActions}{/b} (requires {link:https://steamcommunity.com/sharedfiles/filedetails/?id=3538575665}More Shields{/}) Restore Hit Points to a shield within reach that is unattended or being wielded by you or an ally. The DC is a standard DC for the item's level, and restores Hit Points based on your check result and your proficiency with Crafting. This costs {icon:TwoActions} 2 actions if you are a master in Crafting, or {icon:Action} one action if you are legendary.
          """;
    
    public static void LoadFeat()
    {
        Feat repairFeat = new TrueFeat(
            ModData.FeatNames.QuickRepair, 1,
            "You can repair damage, even in combat.",
            $$"""
              You gain the {b}Quick Repair{/b} activity which can be used in three different ways as contextual actions.
              {{FeatUsages}}
              """,
            [Trait.General, Trait.Manipulate, Trait.Skill])
            .WithActionCost(-1)
            .WithPrerequisite(
                values => values.GetProficiency(Trait.Crafting) >= Proficiency.Trained,
                "You must be trained in Crafting.")
            .WithPermanentQEffect(
                "You can repair constructs and armor.",
                qfFeat =>
                {
                    // If MoreShields is loaded, add a shield-repairing option.
                    if (MoreShieldsIsLoaded)
                        qfFeat.Description = qfFeat.Description?.Replace("constructs and armor", "constructs, armor, and shields");
                    
                    qfFeat.ProvideContextualAction = qfThis =>
                    {
                        if (qfThis.Owner.Battle.AllCreatures
                                .Where(cr =>
                                    qfThis.Owner.FriendOf(cr)
                                    && qfThis.Owner.DistanceTo(cr) <= qfThis.Owner.Space.NaturalReach)
                                .ToList()
                            is not { } allies)
                            return null;

                        List<PossibilitySection> sections = [];
                        
                        Proficiency craftingTraining = qfThis.Owner.PersistentCharacterSheet?.Calculated.GetProficiency(Trait.Crafting) ?? Proficiency.Trained;

                        // Heal a construct
                        if (allies.Any(cr => cr.HasTrait(Trait.Construct) && cr.Damage > 0))
                        {
                            List<Possibility> repairLevels = [];
                            for (Proficiency i = Proficiency.Trained; i <= Proficiency.Legendary; i += 2)
                            {
                                if (i <= craftingTraining)
                                    repairLevels.Add((ActionPossibility) CreateQuickRepairForConstruct(qfThis.Owner, i, true));
                            }
                            
                            if (repairLevels.Count > 0)
                                sections.Add(new PossibilitySection("Heal a construct")
                                {
                                    Possibilities = repairLevels
                                });
                        }

                        // Repair armor
                        if (craftingTraining > Proficiency.Trained
                            && allies.Any(cr => cr.HasEffect(QEffectId.BrokenArmor)))
                        {
                            CombatAction repairArmor = CreateQuickRepairForArmor(qfThis.Owner);
                            sections.Add(new PossibilitySection("Repair armor")
                            {
                                Possibilities = [(ActionPossibility) repairArmor]
                            });
                        }
                        
                        // Repair Shield
                        if (MoreShieldsIsLoaded)
                        {
                            List<Possibility> repairShields = [];

                            foreach (Creature ally in allies)
                            {
                                List<Item> shields = ally
                                    .HeldItems
                                    .Concat(ally.CarriedItems
                                        .Where(item => item.IsWorn))
                                    .Where(OptionalDependencies.IsDamagedShield)
                                    .ToList();
                                repairShields.AddRange(shields
                                    .Select(shield => (ActionPossibility) CreateQuickRepairForShield(
                                        qfThis.Owner, ally, shield, craftingTraining, true))
                                    .ToList());
                            }
                            
                            repairShields.AddRange(qfThis.Owner.Battle.Map.AllTiles
                                .Where(tile =>
                                    tile.DistanceTo(qfThis.Owner) <= qfThis.Owner.Space.NaturalReach)
                                .SelectMany(tile => tile.DroppedItems)
                                .Where(OptionalDependencies.IsDamagedShield)
                                .Select(shield => (ActionPossibility) CreateQuickRepairForShield(
                                    qfThis.Owner, null, shield, craftingTraining, true))
                                .ToList());
                            
                            if (repairShields.Count > 0)
                                sections.Add(new PossibilitySection("Repair shield")
                                {
                                    Possibilities = repairShields
                                });
                        }

                        if (sections.Count == 0)
                            return null;

                        SubmenuPossibility repairs = new SubmenuPossibility(
                            ModData.Illustrations.QuickRepair,
                            "Quick Repair")
                        {
                            SpellIfAny = CreateQuickRepairForDisplay(qfThis.Owner),
                            Subsections = sections
                        };

                        return repairs;
                    };
                })
            // Use the construct-healing version as the example action.
            .WithRulesBlockForCombatAction(cr => CreateQuickRepairForConstruct(cr, Proficiency.Trained, false));
        ModManager.AddFeat(repairFeat);

        // Sacrifice Armor can be repaired.
        Feat sacArmor = AllFeats.GetFeatByFeatName(FeatName.SacrificeArmor);
        sacArmor.RulesText = sacArmor.RulesText.Replace(
            ", though there is no way to get rid of the status penalty until the encounter ends",
            "")
            + $"\n\n{ModData.Illustrations.DdSun.IllustrationAsIconString} {{b}}More Basic Actions{{/b}} You or an ally who has the {ModData.FeatNames.QuickRepair.ToLink("Quick Repair {icon:Action}")} feat can use a DC 20 check to repair your armor, removing the penalty on a success instead of restoring Hit Points.";
        sacArmor.Traits.Insert(0, ModData.ModTrait);
        
        // Track whether MoreShields is loaded.
        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            MoreShieldsIsLoaded = ModManager.TryParse("BrokenShield", out QEffectId _);
        };
    }

    internal static CombatAction CreateQuickRepairTemplate(Creature owner)
    {
        return new CombatAction(
                owner,
                ModData.Illustrations.QuickRepair,
                "Quick Repair",
                [ModData.ModTrait, Trait.General, Trait.Manipulate, Trait.Skill],
                "",
                Target.Self())
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.QuickRepair)
            .WithSoundEffect(SfxName.SwordStrike);
    }

    public static CombatAction CreateQuickRepairForDisplay(Creature owner)
    {
        return CreateQuickRepairTemplate(owner)
            .WithIllustration(ModData.Illustrations.QuickRepair)
            .WithActionCost(-1)
            .WithDescription(
                "You can repair damage, even in combat.",
                $$"""
                  Make a Crafting check. Choose one of the following effects, which determines the DC and result of your check.
                  {{FeatUsages}}
                  """);
    }

    public static CombatAction CreateQuickRepairForConstruct(Creature owner, Proficiency prof, bool inCombat)
    {
        DetermineBonusCreatureHealing(owner, prof, out int dc, out int bonusHealing);
        
        CombatAction repairAction = CreateQuickRepairTemplate(owner)
            .WithDescription(
                $$"""
                  {i}You can repair damage, even in combat.{/i}

                  {b}Range{/b} touch
                  {Blue}{b}Targets{/b} 1 damaged construct{/Blue}
                  {b}Requirements{/b} You must have a hand free.

                  Make a Crafting check ({{S.SkillBonus(owner, Skill.Crafting)}}, {{prof.HumanizeLowerCase2()}}) against DC {{(inCombat ? dc : 15)}}.{{S.FourDegreesOfSuccess(
                      $"The construct regains 4d8{(bonusHealing > 0 ? $"+{bonusHealing}".WithTag("b") : null)} HP.",
                      $"The construct regains 2d8{(bonusHealing > 0 ? $"+{bonusHealing}".WithTag("b") : null)} HP.",
                      null,
                      "The construct takes 1d8 bludgeoning damage.")}}

                  Regardless of your result, the target is then temporarily immune to your Quick Repair for the rest of the day.{{(inCombat ? null : "\n\nIf you are at least expert with Crafting, you can choose to make the check at a higher DC for additional HP restored. At expert, you can choose DC 20 for +10 HP, master can choose DC 30 for +30 HP, and legendary can choose DC 40 for +50 HP.")}}
                  """)
            .WithNewTarget(Target.AdjacentFriendOrSelf()
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    !a.HasFreeHand
                        ? Usability.CommonReasons.NoFreeHandForManeuver
                        : Usability.Usable)
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    d.PersistentUsedUpResources.UsedUpActions.Contains("QuickRepairFrom:" + a.Name)
                        ? Usability.NotUsableOnThisCreature("immune")
                        : Usability.Usable)
                .WithAdditionalConditionOnTargetCreature(new RequiredTraitCreatureTargetingRequirement(Trait.Construct))
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    d.Damage == 0
                        ? Usability.NotUsableOnThisCreature("healthy")
                        : Usability.Usable))
            .WithActiveRollSpecification(new ActiveRollSpecification(
                TaggedChecks.SkillCheck(Skill.Crafting),
                Checks.FlatDC(dc)))
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                if (!target.HasTrait(Trait.Construct))
                {
                    target.Battle.Log($"{target.ToColoredName()} receives no healing due to not being a construct.");
                    return;
                }
                
                target.PersistentUsedUpResources.UsedUpActions.Add("QuickRepairFrom:" + caster.Name);
                
                switch (result)
                {
                    case CheckResult.CriticalFailure:
                        await CommonSpellEffects.DealDirectDamage(
                            action,
                            DiceFormula.FromText("1d8", "Quick Repair (critical failure)"),
                            target,
                            CheckResult.Failure,
                            DamageKind.Bludgeoning); // Feels more appropriate than slashing
                        break;
                    case >= CheckResult.Success:
                        DiceFormula healingAmount = result == CheckResult.CriticalSuccess 
                            ? DiceFormula.FromText("4d8", "Quick Repair (critical success)")
                            : DiceFormula.FromText("2d8", "Quick Repair");
                        if (bonusHealing > 0)
                            healingAmount = healingAmount.Add(
                                DiceFormula.FromText(
                                    bonusHealing.ToString(),
                                    $"Quick Repair ({prof.HumanizeLowerCase2()})")); // Suppress. If prof is null, bonusHealing is 0.
                        await target.HealAsync(healingAmount, action);
                        Sfxs.Play(SfxName.Healing);
                        break;
                }
                
                caster.Battle.Log($"{target.Name} has become immune to being healed by {caster.Name}'s Quick Repair for 1 day.");
            });
        repairAction.WithFullRename($"Quick Repair (DC {dc})");
        
        return repairAction;
    }

    public static void DetermineBonusCreatureHealing(Creature owner, Proficiency prof, out int dc, out int bonusHealing)
    {
        switch (prof)
        {
            case Proficiency.Expert:
                bonusHealing = 10;
                dc = 20;
                break;
            case Proficiency.Master:
                bonusHealing = 30;
                dc = 30;
                break;
            case Proficiency.Legendary:
                bonusHealing = 50;
                dc = 40;
                break;
            default:
                bonusHealing = 0;
                dc = 15;
                break;
        }
    }
    
    public static void DetermineBonusItemHealing(Creature owner, Proficiency prof, out int bonusHealing)
    {
        switch (prof)
        {
            case Proficiency.Trained:
                bonusHealing = 5;
                break;
            case Proficiency.Expert:
                bonusHealing = 10;
                break;
            case Proficiency.Master:
                bonusHealing = 15;
                break;
            case Proficiency.Legendary:
                bonusHealing = 20;
                break;
            default:
                bonusHealing = 0;
                break;
        }
    }

    public static CombatAction CreateQuickRepairForArmor(Creature owner)
    {
        CombatAction repairAction = CreateQuickRepairTemplate(owner)
            .WithDescription(
                $$"""
                  {i}You can repair damage, even in combat.{/i}

                  {b}Range{/b} touch
                  {Blue}{b}Targets{/b} 1 creature with the broken armor condition{/Blue}
                  {b}Requirements{/b} You must have a hand free.

                  Make a Crafting check ({{S.SkillBonus(owner, Skill.Crafting)}}) against DC 20. On a success, the target loses the broken armor condition.

                  Regardless of your result, the target's armor cannot be repaired by any use of Quick Repair for the rest of the day.
                  """)
            .WithNewTarget(Target.AdjacentFriendOrSelf()
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    !a.HasFreeHand
                        ? Usability.CommonReasons.NoFreeHandForManeuver
                        : Usability.Usable)
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    d.PersistentUsedUpResources.UsedUpActions.Contains("QuickRepairArmorFrom:" + a.Name)
                        ? Usability.NotUsableOnThisCreature("immune")
                        : Usability.Usable)
                .WithAdditionalConditionOnTargetCreature((a, d) =>
                    d.HasEffect(QEffectId.BrokenArmor)
                        ? Usability.Usable
                        : Usability.NotUsableOnThisCreature("armor not broken")))
            .WithActiveRollSpecification(new ActiveRollSpecification(
                TaggedChecks.SkillCheck(Skill.Crafting),
                Checks.FlatDC(20)))
            .WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                target.PersistentUsedUpResources.UsedUpActions.Add("QuickRepairArmorFrom:" + caster.Name);
                
                if (target.HasEffect(QEffectId.BrokenArmor)
                    && result >= CheckResult.Success)
                {
                    target.RemoveAllQEffects(qf => qf.Id == QEffectId.BrokenArmor);
                    target.Battle.Log($"{target.ToColoredName()}'s broken armor has been repaired, and cannot be repaired by anyone's Quick Repair for 1 day.");
                }
            });
        repairAction.WithFullRename("Quick Repair (fix armor)");
        
        return repairAction;
    }

    public static CombatAction CreateQuickRepairForShield(Creature owner, Creature? wielder, Item shield, Proficiency prof, bool inCombat)
    {
        DetermineBonusItemHealing(owner, prof, out int bonusHealing);
        int healing = 5 + (inCombat ? bonusHealing : 0);
        int dc = Checks.LevelBasedDC(shield.Level);

        string? ownerDesc = wielder is not null ? $"{wielder.Illustration.IllustrationAsIconString}{wielder?.ToColoredName()}" : null;
        string shieldDesc = $"{shield.Illustration.IllustrationAsIconString}{shield.Name}";
        
        CombatAction repairAction = CreateQuickRepairTemplate(owner)
            .WithDescription(
                $$"""
                  {i}You can repair damage, even in combat.{/i}

                  {b}Range{/b} touch
                  {Blue}{b}Targets{/b} 1 damaged shield that is unattended or wielded by you or an ally{/Blue}
                  {b}Requirements{/b} You must have a hand free.

                  Make a Crafting check ({{S.SkillBonus(owner, Skill.Crafting)}}, {{prof.HumanizeLowerCase2()}}) against DC {{dc}} ({{ownerDesc?.AddIfNotNull(null, "'s ")}}{{shieldDesc.WithColor("Blue")}}).{{(MoreShieldsIsLoaded ? $"\n\n{OptionalDependencies.DescribeHP(shield)}" : null)}}{{S.FourDegreesOfSuccess(
                      $"The shield regains {S.HeightenedVariable(healing*2, 10)} HP.",
                      $"The shield regains {S.HeightenedVariable(healing, 5)} HP.",
                      null,
                      "The shield takes 2d6 damage.")}}
                  """)
            .WithNewTarget(
                wielder is null
                    ? Target.Tile((self, tile) =>
                        self.CanSee(tile)
                        && self.DistanceTo(tile) <= self.Space.NaturalReach
                        && tile.DroppedItems.Contains(shield))
                    : Target.AdjacentFriendOrSelf()
                        .WithAdditionalConditionOnTargetCreature((a, d) =>
                            !a.HasFreeHand
                                ? Usability.CommonReasons.NoFreeHandForManeuver
                                : Usability.Usable))
            .WithActiveRollSpecification(new ActiveRollSpecification(
                TaggedChecks.SkillCheck(Skill.Crafting),
                Checks.FlatDC(dc)))
            .WithActionCost(prof == Proficiency.Legendary ? 1 : prof == Proficiency.Master ? 2 : 3);
        repairAction.WithFullRename($"Quick Repair ({shield.ShortName})");

        if (MoreShieldsIsLoaded)
        {
            repairAction.WithEffectOnEachTarget(async (action, caster, target, result) =>
            {
                if (!MoreShieldsIsLoaded)
                    return;

                string source = $"{action.Name} ({result.HumanizeLowerCase2()})";

                switch (result)
                {
                    case CheckResult.CriticalFailure:
                        (int, string) damageRoll = DiceFormula.FromText("2d6", source).Roll();
                        OptionalDependencies.DamageShield(caster.Battle, wielder, shield, damageRoll.Item1, damageRoll.Item2);
                        break;
                    case >= CheckResult.Success:
                        int healingAmount = (5 + bonusHealing) * (result == CheckResult.CriticalSuccess ? 2 : 1);
                        OptionalDependencies.RepairShield(caster.Battle, wielder, shield, healingAmount, source);
                        Sfxs.Play(SfxName.Healing);
                        break;
                }
            });
        }
        
        return repairAction;
    }
}