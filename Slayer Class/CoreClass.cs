using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.SlayerClass;

public static class CoreClass
{
    public static void Load()
    {
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
        
        yield return new Feat(
                ModData.FeatNames.OnTheHunt,
                null, "", [], null)
            .WithPermanentQEffect(
                "When your quarry is crit or any creature within 60 feet is reduced to 0 HP, become quickened until the end of your next turn {i}(only to Step, Stride, or use relentless actions){/i}.",
                qfFeat =>
                {
                    qfFeat.Name += " {icon:Reaction}";
                    
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
        
        // TODO: Monster Lore feature
        yield return new Feat(
                ModData.FeatNames.MonsterLore,
                null, "", [], null);
        
        // Slayer's Arsenal
        yield return new Feat(
                ModData.FeatNames.SlayersArsenal,
                null, "", [], null)
            .WithOnSheet(values =>
            {
                values.AddSelectionOption(new SingleFeatSelectionOption(
                    "SlayersArsenal",
                    "First signature tool",
                    1,
                    ft =>
                        ft.HasTrait(ModData.Traits.HuntingTool)
                        && ft.Tag is HuntingTool { Kind: HuntingTools.ToolKind.Signature }));
            });
        
        // Mark Quarry
        yield return new Feat(
                ModData.FeatNames.MarkQuarry,
                null, "", [], null)
            .WithPermanentQEffect(
                "At the start of combat, mark a creature of your level or higher as your quarry.",
                qfFeat =>
                {
                    qfFeat.StartOfCombat = async qfThis =>
                    {
                        var enemies = qfThis.Owner.Battle.AllCreatures
                            .Where(cr =>
                                cr.Level >= qfThis.Owner.Level
                                && cr.EnemyOf(qfThis.Owner))
                            .ToList();
                        if (enemies.Count == 0)
                        {
                            qfThis.Owner.Overhead("*no quarry*", Color.Red);
                            return;
                        }
                        if (await qfThis.Owner.Battle.AskToChooseACreature(
                                qfThis.Owner,
                                qfThis.Owner.Battle.AllCreatures
                                    .Where(cr =>
                                        cr.Level >= qfThis.Owner.Level
                                        && cr.EnemyOf(qfThis.Owner)),
                                ModData.Illustrations.MarkQuarry,
                                "Choose a creature to mark as your quarry.",
                                "This creature becomes your quarry for the rest of the encounter.",
                                "Pass")
                            is not { } chosen)
                            return;

                        chosen.AddQEffect(MarkQuarry(qfThis.Owner));
                        Sfxs.Play(ModData.SfxNames.MarkQuarry);
                    };
                    qfFeat.BonusToSkillChecks = (skill, action, target) =>
                    {
                        if (target is null || !IsMyQuarry(qfFeat.Owner, target))
                            return null;

                        // TODO: Monster Hunter Lore
                        if (skill is not Skill.Society)
                            return null;

                        return new Bonus(2, BonusType.Circumstance, "Mark quarry");
                    };
                });
        
        yield return new Feat(
                ModData.FeatNames.ClaimTrophy,
                null, "", [], null)
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
                    Item? trophyCase = inv.AllItems.FirstOrDefault(item => item.ItemName == Trophy.TrophyCase);
                    if (trophyCase is null)
                    {
                        trophyCase = Items.CreateNew(Trophy.TrophyCase);
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

                    // If you don't have any trophies, add a starter trophy
                    Item? anyTrophy = inv.AllItems.FirstOrDefault(item => item.ItemName == Trophy.TrophyItem);
                    if (anyTrophy is null)
                    {
                        anyTrophy = Trophy.CreateStartingTrophy();
                        // And shove it in your trophy case
                        trophyCase.StoredItems.Add(anyTrophy);
                    }
                }
            })
            .WithPermanentQEffect(
                "You have a trophy case. At the end of an encounter, collect a trophy from any marked quarry you defeated.",
                qfFeat =>
                {
                    // TODO: Claim Trophy
                });
        
        // Slayer's Quarry, which grants Mark Quarry and Slayer's Arsenal
        yield return new Feat(
                ModData.FeatNames.SlayersQuarry,
                null, "", [], null)
            .WithOnSheet(values =>
            {
                values.GrantFeat(ModData.FeatNames.MarkQuarry);
                values.GrantFeat(ModData.FeatNames.ClaimTrophy);
            });
        
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
                // TODO: Class feature description
                """
                Lorem ipsum.
                """,
                null)
            .WithEffectiveClassFeatures(features => features
                .AddFeature(3, WellKnownClassFeature.ExpertInReflex)
                // TODO: Add Tip of the Tongue
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
                // TODO: Call "Slayer's Sight", add replacement for immediate reaction only for slayer stuff
                .AddFeature(7, WellKnownClassFeature.ExpertInPerception)
                .AddFeature(7, new ClassFeature(ModData.Tooltips.CommonWeaponSpec("Weapon specialization"))
                    .WithOnCreature((values, creature) =>
                        creature.AddQEffect(QEffect.WeaponSpecialization(values.Tags.ContainsKey("GREATER_WEAPON_SPECIALIZATION")))))
                .AddFeature(7, new ClassFeature(ModData.Tooltips.SpecializedArsenal("Specialized arsenal"))
                {
                    OnSheet = values => 
                        HuntingTools
                            .GetTools(values)
                            ?.FirstOrDefault(tool => tool.Kind is HuntingTools.ToolKind.Signature)
                            ?.AccessSpecialized = true
                })
                // TODO: Call "Persistent Focus" instead
                .AddFeature(9, WellKnownClassFeature.Resolve)
                .AddFeature(9, WellKnownClassFeature.ExpertInClassDC)
                .AddFeature(11, new ClassFeature(ModData.Tooltips.ExpandedArsenal("Expanded arsenal"))
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
                // TODO: Call "Natural Resilience"
                .AddFeature(11, WellKnownClassFeature.Juggernaut)
                .AddFeature(13, new ClassFeature("Master weapon proficiency", "You become master in simple weapons, martial weapons, and unarmed attacks.")
                {
                    OnSheet = values =>
                    {
                        values.IncreaseProficiency(5, Trait.Simple, Proficiency.Master);
                        values.IncreaseProficiency(5, Trait.Martial, Proficiency.Master);
                        values.IncreaseProficiency(5, Trait.Unarmed, Proficiency.Master);
                    }
                })
                // TODO: Call "Greater Persistent Focus" instead
                .AddFeature(15, WellKnownClassFeature.GreaterResolve)
                .AddFeature(15, new ClassFeature(ModData.Tooltips.CommonGreaterWeaponSpec("Greater weapon specialization"))
                {
                    OnSheet = values => values.Tags["GREATER_WEAPON_SPECIALIZATION"] = true,
                })
                .AddFeature(15, new ClassFeature(ModData.Tooltips.GreaterSpecializedArsenal("Greater specialized arsenal"))
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
            )
            .WithOnSheet(values =>
            {
                values.AddClassFeatOption("SlayerFeat1", ModData.Traits.Slayer, 1);
                values.GrantFeat(ModData.FeatNames.OnTheHunt);
                values.GrantFeat(ModData.FeatNames.MonsterLore);
                values.GrantFeat(ModData.FeatNames.SlayersArsenal);
                values.GrantFeat(ModData.FeatNames.SlayersQuarry);
            });
        yield return slayerClass;
    }

    public static QEffect MarkQuarry(Creature slayer)
    {
        return new QEffect(
            "Marked Quarry",
            $"You have been marked by {{Blue}}{slayer}{{/Blue}} as their quarry.\n\nThey gain a +2 circumstance bonus to Monster Lore and Society checks against you, as well as benefits from their hunting tools.\n\nAt the end of the encounter, a trophy will be extracted from you.",
            ExpirationCondition.Never,
            slayer,
            ModData.Illustrations.MarkQuarry)
        {
            Id = ModData.QEffectIds.MarkedQuarry,
            // TODO: Claim Trophy
        };
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
        slayer.Overhead(
            "On the Hunt " + icon,
            Color.Black,
            $"{slayer} goes {{b}}On the Hunt{{/b}} {icon}.",
            "On the Hunt {icon:Reaction}",
            """
            {i}You throw yourself into battle, redoubling your efforts.{/i}
            
            {b}Trigger{/b} You see your quarry be critically hit, or any creature within 60 feet be reduced to 0 Hit Points.
            
            You gain the quickened condition until the end of your next turn, and you can use the extra action only to Step, Stride, or use an action with the relentless trait.
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
        return onTheHunt;
    }
}