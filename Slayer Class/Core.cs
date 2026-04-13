using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Dawnsbury.Mods.LoresAndWeaknesses;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.SlayerClass;

public static class Core
{
    public static Lore MonsterLore { get; set; } = null!;

    public static void Load()
    {
        MonsterLore = Lores.RegisterNewLore(
            "Monster Lore",
            $"You can use this lore to {RecallWeakness.GetActionLink("Recall a Weakness {icon:Action}")} of non-humanoid creatures.",
            (_, d) =>
                !d.HasTrait(Trait.Humanoid),
            false,
            true);
        
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
    }
    
    public static IEnumerable<Feat> CreateFeats()
    {
        // Class Features
        // "I've separated them this way so that in the future when the class is
        // fully released, I can grant these individual features without repeating
        // code for the multiclass archetype later down the line."
        // - Anase Skyrider
        
        // On the Hunt
        Feat onTheHunt = new Feat(
                ModData.FeatNames.OnTheHunt,
                "Whether you’re facing your quarry or not, your slayer's instincts let you seize on any advantage, chasing the thrill of battle to victory.",
                """
                {b}Trigger{/b} You see your quarry be critically hit, or any creature within 60 feet be reduced to 0 Hit Points.
                
                You gain the quickened condition until the end of your next turn, and you can use the extra action only to Step, Stride, or use an action with the relentless trait.
                """,
                [],
                null)
            .WithPermanentQEffect(
                "When your quarry is crit or any creature within 60 feet is reduced to 0 HP, become quickened until the end of your next turn {i}(only to Step, Stride, or use relentless actions){/i}.",
                qfFeat =>
                {
                    // Quarry be critically hit
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            IsMyQuarry(qfFeat.Owner, cr),
                        qfTech =>
                        {
                            qfTech.Name = "[ON THE HUNT: QUARRY IS CRIT]";
                            qfTech.YouAreTargetedByARoll = async (qfThis, action, result) =>
                            {
                                if (result.CheckResult >= CheckResult.CriticalSuccess
                                    && action.HasTrait(Trait.Attack)
                                    && !action.HasTrait(Trait.AttackDoesNotTargetAC))
                                    await AskToGoOnTheHunt("Your quarry, {Blue}"+qfThis.Owner+"{/Blue}, has been critically hit");
                                return false;
                            };
                        });
                    
                    // Anyone within 60 feet going down
                    qfFeat.AddGrantingOfTechnical(
                        cr =>
                            cr != qfFeat.Owner
                            && cr.DistanceTo(qfFeat.Owner) <= (60/5),
                        qfTech =>
                        {
                            qfTech.Name = "[ON THE HUNT: WHEN YOU DIE]";
                            qfTech.WhenCreatureDiesAtStateCheckAsync = async qfThis =>
                            {
                                await AskToGoOnTheHunt("{Blue}"+qfThis.Owner+"{/Blue} has dropped to 0 HP");
                            };
                        });

                    return;
                    
                    async Task AskToGoOnTheHunt(string reason)
                    {
                        if (!await qfFeat.Owner.Battle.AskToUseReaction(
                                qfFeat.Owner,
                                $$"""
                                  {b}On The Hunt{/b} {icon:Reaction}
                                  {{reason}}. Become quickened until the end of your next turn? {i}(Only to Step, Stride, or use relentless actions.){/i}
                                  """,
                                ModData.Illustrations.OnTheHunt,
                                [ModData.Traits.Slayer, ModData.Traits.Relentless]))
                            return;
                        await GoOnTheHunt(qfFeat.Owner);
                    }
                });
        yield return onTheHunt;
        
        // Monster Lore
        Feat monsterLore = new Feat(
                ModData.FeatNames.MonsterLore,
                "Your training and experience means you have comprehensive knowledge about dangerous monsters, and you can often correctly deduce information about even creatures that are new to you.",
                $"You become trained in Monster Lore, a special Lore you can use to {RecallWeakness.GetActionLink("Recall a Weakness {icon:Action}")} of non-humanoid creatures. This increases to expert proficiency at 3rd level, to master at 7th level, and legendary at 15th level.",
                [],
                null)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(MonsterLore);
                //values.SetProficiency(MonsterLore.Trait, Proficiency.Trained);
                //values.IncreaseProficiency(1, MonsterLore.Trait, Proficiency.Trained);
                values.IncreaseProficiency(3, MonsterLore.Trait, Proficiency.Expert);
                values.IncreaseProficiency(7, MonsterLore.Trait, Proficiency.Master);
                values.IncreaseProficiency(15, MonsterLore.Trait, Proficiency.Legendary);
            });
        yield return monsterLore;
        
        // Slayer's Arsenal
        Feat slayersArsenal = new Feat(
                ModData.FeatNames.SlayersArsenal,
                "You have an arsenal of hunting tools that you specially prepare to target your current quarry.",
                $"You gain a signature tool, an especially powerful {ModData.Tooltips.HuntingTool("hunting tool")} for which you have particular affinity, granting all of its 1st-level benefits. At level 7, you also gain its Specialized Arsenal benefit.",
                [],
                null)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption(
                    "SlayersArsenal",
                    "First signature tool",
                    1,
                    ft =>
                        ft.HasTrait(ModData.Traits.HuntingTool)
                        && ft.Tag is HuntingTool { Kind: HuntingTools.ToolKind.Signature }));
                
                // Remove illegal designations
                values.AtEndOfRecalculation += values2 =>
                {
                    // Get all the tools I know
                    List<HuntingTool>? tools = HuntingTools.GetTools(values);
                    
                    // Find all the items I have that are hunting tools
                    Inventory inv = values2.Sheet.IsCampaignCharacter
                        ? values2.Sheet.CampaignInventory
                        : values2.Sheet.Inventory;
                    List<Item> designations = inv.AllItems
                        .Where(HuntingTools.IsATool)
                        .ToList();
                    
                    // Ignore all the items I have that are hunting tools I know
                    if (tools is not null)
                        foreach (Item iTool in designations.ToList())
                        {
                            if (tools.Any(hTool => hTool.IsMyTool(iTool)))
                                designations.Remove(iTool);
                        }
                    
                    // Remove whatever is left (items that are tools I don't know)
                    foreach (Item iTool in designations.ToList())
                    {
                        iTool.WithoutModification(HuntingTools.ToolDesignation);
                        // TODO: Safely replace the de-designated tool with a de-trophied copy
                        /*foreach (Item trophy in iTool.Runes
                                     .Where(r => r.RuneProperties!.RuneKind == ModData.RuneKinds.SlayerTrophy)
                                     .ToList())
                        {
                            Item recreation = RunestoneRules.RecreateWithUnattachedSubitem(iTool, trophy, true);
                            if (inv.RemoveFirstInventoryItem(i => i == iTool) is { } removed)
                            {
                                inv.AddAtEndOfBackpack(recreation);
                                inv.AddAtEndOfBackpack(trophy);
                            }
                        }*/
                    }
                };
            });
        yield return slayersArsenal;
        
        // Mark Quarry
        Feat markQuarry = new Feat(
                ModData.FeatNames.MarkQuarry,
                "You are especially effective when you carefully research and track a worthy target.",
                $"At the start of combat, mark a creature of your level or higher as your quarry. You can only have one quarry at a time. Against them, you gain a +2 circumstance bonus to {RecallWeakness.GetActionLink("Recall a Weakness {icon:Action}")} with Society and Monster Lore. You also gain additional benefits against your quarry from your other feats and features.",
                [ModData.Traits.ModName, Trait.Concentrate],
                null)
            .WithPermanentQEffect(
                "At the start of combat, mark a creature of your level or higher as your quarry.",
                qfFeat =>
                {
                    if (Settings.CurrentDifficulty <= Difficulty.Easy)
                        qfFeat.Description += " {Blue}{b}Easy:{/b} The creature can be 1 level lower than you.{/Blue}";
                    qfFeat.StartOfCombat = async qfThis =>
                    {
                        if (qfThis.Owner.QEffects.Any(qf =>
                                qf.PreventTakingAction?.Invoke(
                                    CombatAction.CreateSimple(qfThis.Owner, "Mark Quarry", Trait.Concentrate))
                                    is not null))
                        {
                            qfThis.Owner.Overhead("*no quarry*", Color.Red, qfThis.Owner + " is unable to {b}Mark a Quarry{/b} due to being unable to take concentrate actions.");
                            return;
                        }
                        
                        int levelReq = qfThis.Owner.Level - (Settings.CurrentDifficulty <= Difficulty.Easy ? 1 : 0);
                        var enemies = qfThis.Owner.Battle.AllCreatures
                            .Where(cr =>
                                cr.Level >= levelReq
                                && cr.EnemyOf(qfThis.Owner)
                                && !cr.DetectionStatus.IsUndetectedTo(qfThis.Owner))
                            .ToList();
                        if (enemies.Count == 0)
                        {
                            qfThis.Owner.Overhead("*no quarry*", Color.Red, qfThis.Owner + " has no quarry to mark.");
                            return;
                        }
                        if (await qfThis.Owner.Battle.AskToChooseACreature(
                                qfThis.Owner,
                                enemies,
                                ModData.Illustrations.MarkQuarry,
                                "Choose a creature to mark as your quarry.",
                                "This creature becomes your quarry for the rest of the encounter.",
                                "Pass")
                            is not { } chosen)
                            return;

                        chosen.AddQEffect(MarkQuarry(qfThis.Owner));
                        Sfxs.Play(ModData.SfxNames.MarkQuarry);
                        Feat markQuarry = AllFeats.GetFeatByFeatName(ModData.FeatNames.MarkQuarry);
                        qfThis.Owner.Battle.Log(
                            $"{qfThis.Owner} {{b}}Marks{{/b}} {{Blue}}{chosen}{{/Blue}} as their {{b}}Quarry{{/b}}.",
                            "Mark Quarry {icon:FreeAction}",
                            "{i}" + markQuarry.FlavorText + "{/i}\n\n" + markQuarry.RulesText,
                            new Traits([..markQuarry.Traits, ModData.Traits.Slayer]));
                    };
                    qfFeat.BonusToSkillChecks = (skill, action, target) =>
                        target is not null
                        && IsMyQuarry(qfFeat.Owner, target)
                        && action.ActionId == RecallWeakness.RWActionId
                        && (skill == Skill.Society || skill == MonsterLore.Skill)
                            ? new Bonus(2, BonusType.Circumstance, "Mark quarry")
                            : null;
                });
        yield return markQuarry;
        
        // Claim Trophy
        Feat claimTrophy = new Feat(
                ModData.FeatNames.ClaimTrophy,
                "When your quarry finally falls, you can swiftly claim a trophy and use it to reinforce your own tools, readying yourself for the next hunt.",
                $"At the end of an encounter, you claim a {ModData.Tooltips.Trophy("trophy")} from your quarry. You also gain a trophy case, an item that stores up to 5 unused trophies.",
                [],
                null)
            .WithOnSheet(values =>
            {
                // Create collection of all inventories, campaign and free play
                var inventories = new Dictionary<int, Inventory>(/*values.Sheet.InventoriesByLevel*/)
                {
                    { 0, values.Sheet.CampaignInventory },
                    { 1, values.Sheet.InventoriesByLevel[1] }
                };
                foreach (var (level, inv) in inventories)
                {
                    // If you don't have a trophy case, add one
                    Item? trophyCase = inv.AllItems.FirstOrDefault(item => item.ItemName == Trophies.TrophyCase);
                    if (trophyCase is null)
                    {
                        trophyCase = Items.CreateNew(Trophies.TrophyCase);
                        /*if (inv.IsEmpty
                            && inventories
                                .Where(kvp => kvp.Key >= 1 && kvp.Key < level && !kvp.Value.IsEmpty)
                                .Select(kvp => kvp.Value)
                                .LastOrDefault()
                            is {} prevInv)
                        {
                            inv.BecomeFrom(prevInv);
                        }*/
                        inv.AddAtEndOfBackpack(trophyCase);
                    }

                    // If you don't have any trophies, add a reminder note
                    Item? startingTrophy = inv.AllItems.FirstOrDefault(item =>
                        item.Name == "To my dear slayer," || item.ItemName == Trophies.TrophyItem);
                    if (startingTrophy is null)
                    {
                        Item note = new Item(
                            ItemName.SpellScroll,
                            new ScrollIllustration(IllustrationName.Scroll, ModData.Illustrations.DdSun),
                            "To my dear slayer,",
                            0, 0, [])
                            .WithDescription(
                                """
                                {i}Your tutor has packed a note, ensuring good health and safety on your adventures.{/i}

                                "Don't forget to bring a starting trophy! Slayers are nothing without their trophies!" - your tutor

                                {i}They're quite doting.{/i}
                                
                                {Red}Visit the shop to buy one of the available starting trophies and reinforce your hunting tools.{/Red}
                                """);
                        trophyCase.StoredItems.Add(note);
                    }
                }
            })
            .WithPermanentQEffect("You have a trophy case. At the end of an encounter, claim a trophy from your quarry.", _ => { });
        yield return claimTrophy;
        
        // Class
        Feat slayerClass = new ClassSelectionFeat(
                ModData.FeatNames.SlayerClass,
                "The world is full of dangerous and mighty beings, but slayers know that no threat is unbeatable. You could be a trapper in pursuit of rarer game a brave defender of the weak, or a dogged pursuer of a hated nemesis; whatever your reasons, few are more skilled than you at hunting singular and deadly foes.\n\nEquipped with an arsenal of specialized tools, the spoils of your previous hunts, and your indomitable spirit, you’re always more prepared for each new quarry than the last.",
                ModData.Traits.Slayer,
                new LimitedAbilityBoost(Ability.Strength, Ability.Dexterity),
                10,
                [Trait.Reflex, Trait.Survival, Trait.Simple, Trait.Martial, Trait.Unarmed, Trait.LightArmor, Trait.MediumArmor, Trait.UnarmoredDefense],
                [Trait.Perception, Trait.Fortitude, Trait.Will],
                4,
                $$"""
                {b}Monster Lore{/b} {{monsterLore.RulesText}}
                
                {b}Mark Quarry{/b} {icon:FreeAction} (concentrate, slayer) {{markQuarry.FlavorText}} {{markQuarry.RulesText}}
                
                {b}On the Hunt{/b} {icon:Reaction} (slayer) {{onTheHunt.FlavorText}} You gain the {{onTheHunt.ToLink("On the Hunt {icon:Reaction}")}} reaction.
                
                {b}Claim Trophy{/b} {{claimTrophy.FlavorText}} {{claimTrophy.RulesText}}
                
                {b}Slayer's Arsenal{/b} {{slayersArsenal.FlavorText}} {{slayersArsenal.RulesText}}
                
                {b}Reinforce Arsenal{/b} Out of combat, you can right-click appropriate items to designate them as one of your known hunting tools; and can attach and detach a trophy from a hunting tool to grant it additional benefits, as described by the Reinforced benefits of the tool.
                """,
                null)
            .WithEffectiveClassFeatures(features => features
                .AddFeature(3, WellKnownClassFeature.ExpertInReflex)
                // TODO: Add Tip of the Tongue
                // Your encyclopedic knowledge of monsters allows you to quickly recall basic information. You gain the Assurance and Automatic Knowledge skill feats for Monster Lore.
                /*.AddFeature(5, "Tip of the Tongue", "")*/
                .AddFeature(5, new ClassFeature("Expert weapon proficiency", "You become expert in simple weapons, martial weapons, and unarmed attacks.")
                {
                    OnSheet = values =>
                    {
                        values.IncreaseProficiency(5, Trait.Simple, Proficiency.Expert);
                        values.IncreaseProficiency(5, Trait.Martial, Proficiency.Expert);
                        values.IncreaseProficiency(5, Trait.Unarmed, Proficiency.Expert);
                    }
                })
                .AddFeature(7, WellKnownClassFeature.ExpertInPerception)
                .AddFeature(7, new ClassFeature(ModData.Tooltips.CommonWeaponSpec("Weapon Specialization"))
                    .WithOnCreature((values, creature) =>
                        creature.AddQEffect(QEffect.WeaponSpecialization(values.Tags.ContainsKey("GREATER_WEAPON_SPECIALIZATION")))))
                .AddFeature(7, new ClassFeature(ModData.Tooltips.SpecializedArsenal("Specialized Arsenal"))
                {
                    OnSheet = values => 
                        HuntingTools
                            .GetTools(values)
                            ?.FirstOrDefault(tool => tool.Kind is HuntingTools.ToolKind.Signature)
                            ?.AccessSpecialized = true
                })
                .AddFeature(9, new ClassFeature(ModData.Tooltips.PersistentFocus("Persistent Focus"))
                    .WithOnSheet(values =>
                        values.SetProficiency(Trait.Will, Proficiency.Master))
                    .WithOnCreature((sheet, creature) =>
                        CommonCharacterFeatures.AddEvasion(sheet, creature, "Persistent Focus", Defense.Will)))
                .AddFeature(9, WellKnownClassFeature.ExpertInClassDC)
                .AddFeature(11, new ClassFeature(ModData.Tooltips.ExpandedArsenal("Expanded Arsenal"))
                {
                    OnSheet = values =>
                    {
                        values.AddSelectionOption(new SingleFeatSelectionOption(
                            "ExpandedArsenal",
                            "Second signature tool",
                            11,
                            ft => 
                                ft.HasTrait(ModData.Traits.HuntingTool)
                                && ft.Tag is HuntingTool { Kind: HuntingTools.ToolKind.Signature }));
                    }
                })
                .AddFeature(11, WellKnownClassFeature.ExpertInUnarmoredDefenseAndLightArmorAndMediumArmor)
                .AddFeature(11, new ClassFeature(ModData.Tooltips.NaturalResilience("Natural Resilience"))
                    .WithOnSheet(values =>
                        values.SetProficiency(Trait.Fortitude, Proficiency.Master))
                    .WithOnCreature((sheet, creature) =>
                        CommonCharacterFeatures.AddEvasion(sheet, creature, "Natural Resilience", Defense.Fortitude)))
                .AddFeature(13, new ClassFeature("Master weapon proficiency", "You become master in simple weapons, martial weapons, and unarmed attacks.")
                {
                    OnSheet = values =>
                    {
                        values.IncreaseProficiency(5, Trait.Simple, Proficiency.Master);
                        values.IncreaseProficiency(5, Trait.Martial, Proficiency.Master);
                        values.IncreaseProficiency(5, Trait.Unarmed, Proficiency.Master);
                    }
                })
                .AddFeature(15, new ClassFeature(ModData.Tooltips.GreaterPersistentFocus("Greater Persistent Focus"))
                    .WithOnSheet(values =>
                    {
                        values.SetProficiency(Trait.Will, Proficiency.Legendary);
                        values.Tags["GREATER_RESOLVE"] = true;
                    }))
                .AddFeature(15, new ClassFeature(ModData.Tooltips.CommonGreaterWeaponSpec("Greater Weapon Specialization"))
                {
                    OnSheet = values => values.Tags["GREATER_WEAPON_SPECIALIZATION"] = true,
                })
                .AddFeature(15, new ClassFeature(ModData.Tooltips.GreaterSpecializedArsenal("Greater Specialized Arsenal"))
                {
                    OnSheet = values => 
                        HuntingTools
                            .GetTools(values)
                            ?.FirstOrDefault(tool =>
                                tool.Kind is HuntingTools.ToolKind.Signature
                                && !tool.AccessSpecialized)
                            ?.AccessSpecialized = true
                })
                .AddFeature(17, WellKnownClassFeature.LegendaryInPerception)
                .AddFeature(17, WellKnownClassFeature.MasterInClassDC)
                .AddFeature(19, WellKnownClassFeature.MasterInUnarmoredDefenseAndLightArmorAndMediumArmor)
                // TODO: Add "Fated Foe"
                .AddFeature(19, new ClassFeature("Fated Foe", ModData.Illustrations.DdSun.IllustrationAsIconString + " {b}NYI{/b}") { KeepCapitalization = true }))
            .WithOnSheet(values =>
            {
                values.AddClassFeatOption("SlayerFeat1", ModData.Traits.Slayer, 1);
                values.GrantFeat(ModData.FeatNames.MonsterLore);
                values.GrantFeat(ModData.FeatNames.OnTheHunt);
                values.GrantFeat(ModData.FeatNames.MarkQuarry);
                values.GrantFeat(ModData.FeatNames.ClaimTrophy);
                values.GrantFeat(ModData.FeatNames.SlayersArsenal);
            });
        yield return slayerClass;
    }

    public static QEffect MarkQuarry(Creature slayer, bool doNotClaimTrophy = false)
    {
        return new QEffect(
            "Marked Quarry",
            $"You have been marked by {{Blue}}{slayer}{{/Blue}} as their quarry.\n\nThey gain a +2 circumstance bonus to Monster Lore and Society checks against you, as well as benefits from their hunting tools.\n\nAt the end of the encounter, a trophy will be extracted from you.",
            ExpirationCondition.Never,
            slayer,
            ModData.Illustrations.MarkQuarry)
        {
            Id = ModData.QEffectIds.MarkedQuarry,
            Traits = doNotClaimTrophy ? [ModData.Traits.DoNotClaimTrophy] : [],
            WhenCreatureDiesAtStateCheckAsync = async qfThis =>
            {
                if (qfThis.Traits.Contains(ModData.Traits.DoNotClaimTrophy))
                    return;
                Item newTrophy = Trophies.CreateTrophy(qfThis.Owner);
                qfThis.Owner.Battle.Encounter.Rewards.Add(newTrophy);
                slayer.Battle.CampaignState?.CommonLoot.Add(newTrophy);
            }
        }
        .With(qfThis =>
        {
            if (HuntingTools.GetTool(slayer, HuntingTools.ToolId.BloodseekingBlade) is not null)
                qfThis.YouAreDealtDamageEvent = async (_, dEvent) =>
                {
                    // Must deal damage to your Quarry by Striking with
                    // an Item that is your Bloodseeking Blade.
                    if (dEvent.CombatAction is not { Item: not null } action
                        || !action.HasTrait(Trait.Strike)
                        || !Core.IsMyQuarry(dEvent.Source, dEvent.TargetCreature)
                        || HuntingTools.GetTool(dEvent.Source, HuntingTools.ToolId.BloodseekingBlade)
                            is not { } blade
                        || !blade.IsMyTool(action.Item))
                        return;

                    // Find what resistances (if any) were applied to your Strike with the tool.
                    var ignorableKinds = dEvent.TargetCreature.WeaknessAndResistance
                        .DamageKindsWithAppliedResistance
                        .OrderByDescending(dict => dict.Value)
                        .ToDictionary();

                    // If you don't have the specialized arsenal of this tool, ignore only physical kinds.
                    if (!blade.AccessSpecialized)
                        ignorableKinds = ignorableKinds
                            .Where(dict => dict.Key.IsPhysical())
                            .ToDictionary();

                    // Return early if there are none, avoid null errors.
                    if (ignorableKinds.Count == 0)
                        return;

                    // Ignore an amount equal to 1 + numDice.
                    // Use lowest of our amount, or the amount actually applied.
                    var ignoredKind = ignorableKinds.FirstOrDefault();
                    int ignoreAmount = 1 + action.Item.WeaponProperties!.DamageDieCount;
                    int finalAmount = Math.Min(ignoredKind.Value, ignoreAmount);

                    // I don't know why, but in some cases you'll see like [Slashing, 0].
                    // This resolves that and any similar cases.
                    if (finalAmount == 0)
                        return;

                    // Apply ignore, similar to ReduceBy.
                    dEvent.KindedDamages.First(kd => kd.DamageKind == ignoredKind.Key).ResolvedDamage += finalAmount;
                    dEvent.DamageEventDescription.AppendLine($"{{b}}+{finalAmount.ToString()}{{/b}} Ignore resistance to {ignoredKind.Key.ToStringOrTechnical().ToLower()} (Bloody fuller)");
                    /*dEvent.ReduceBy(finalAmount * -1, "Bloody fuller");*/
                };
        });
    }

    public static bool IsMyQuarry(Creature slayer, Creature target)
    {
        return target.QEffects.Any(qf =>
            qf.Id == ModData.QEffectIds.MarkedQuarry && qf.Source == slayer);
    }

    public static async Task GoOnTheHunt(Creature slayer, bool isFreeAction = false)
    {
        slayer.AddQEffect(OnTheHunt(slayer));
        string icon = $"{{icon:{(isFreeAction ? "FreeAction" : "Reaction")}}}";
        Feat onTheHunt = AllFeats.GetFeatByFeatName(ModData.FeatNames.OnTheHunt);
        slayer.Overhead(
            "On the Hunt " + icon,
            Color.Black,
            $"{slayer} goes {{b}}On the Hunt{{/b}} {icon}.",
            onTheHunt.Name,
            $$"""
            {i}{{onTheHunt.FlavorText}}{/i}
            
            {{onTheHunt.RulesText}}
            """,
            new Traits([ModData.Traits.Slayer]));
        Sfxs.Play(ModData.SfxNames.OnTheHunt);
    }

    public static QEffect OnTheHunt(Creature slayer)
    {
        QEffect onTheHunt = QEffect.Quickened(ca =>
                ca.ActionId is ActionId.Step or ActionId.Stride or ActionId.StepByStepStride ||
                ca.HasTrait(ModData.Traits.Relentless))
            .WithExpirationAtEndOfSourcesNextTurn(slayer, true);
        onTheHunt.DoNotShowUpOverhead = true;
        onTheHunt.Description =
            "You have an extra action each turn. It can only be used to Step, Stride, or use an action with the relentless trait.";
        onTheHunt.Key = "OnTheHunt";
        return onTheHunt;
    }
}