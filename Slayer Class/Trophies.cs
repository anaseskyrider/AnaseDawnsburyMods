using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.ContextMenu;
using Dawnsbury.Display.Controls;
using Dawnsbury.Display.Notifications;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using SpiritDamage;

namespace Dawnsbury.Mods.SlayerClass;

public static class Trophies
{
    /// <summary>
    /// The item that stores other trophies.
    /// </summary>
    public static ItemName TrophyCase;
    
    /// <summary>
    /// The item that represents a trophy and bears any data modifications.
    /// </summary>
    public static ItemName TrophyItem;
    
    /// <summary>
    /// This ItemModification contains all the data associated with a trophy, allowing for easy serialization and deserialization. A <see cref="TrophyItem"/> instance should always have a TrophyModification instance, and should only have 1 of them.
    /// </summary>
    public static ItemModificationKind TrophyModification;
    
    /// <summary>
    /// Some hunting tools have a reinforced benefit which requires you to choose one damage type from the ones on the trophy when you Reinforce your Arsenal. This item modification tracks that chosen damage type. 
    /// </summary>
    public static ItemModificationKind ChosenDamageKindModification;

    public static readonly List<Trait> TraitBlacklist = [
        // Size traits
        /*Trait.Tiny,*/
        Trait.Small,
        Trait.Large,
        Trait.Huge,
        Trait.Gargantuan,
        Trait.Colossal5,
        Trait.Colossal6,
        Trait.Colossal7,
        Trait.Colossal8,
        // Rarity traits
        Trait.Uncommon,
        /*Trait.Rare,*/
        Trait.Unique,
        // Various technical traits
        Trait.AnimatedObject,
        Trait.AssumesDirectControl,
        Trait.BasicallyNeverWantsToMakeBasicUnarmedStrike,
        Trait.BecomesVisibleCorpseOnDeath,
        Trait.DoesNotBreathe,
        Trait.Farmer,
        Trait.Female,
        Trait.Homebrew,
        Trait.Indestructible,
        Trait.Male,
        Trait.MetalArmor,
        Trait.MetalArmorInvisible,
        Trait.Mod,
        Trait.MustSurvive,
        Trait.NativeOutsider,
        Trait.NeedNotSurvive,
        Trait.NeverSetsOccupant,
        Trait.NoDeathOverhead,
        Trait.NoDeathScream,
        Trait.NoPhysicalUnarmedAttack,
        Trait.Object,
        Trait.Outsider,
        Trait.PossessedChild,
        Trait.Pseudocreature,
        Trait.ThirdParty,
        Trait.Trap,
        Trait.UnimportantForVictoryCondition,
        // Nonsense traits to obviously exclude
        Trait.Summoned,
        Trait.NonSummonable,
        Trait.Conjuration,
        // Tradition traits (stored elsewhere on a trophy)
        Trait.Arcane,
        Trait.Divine,
        Trait.Primal,
        Trait.Occult,
    ];

    /// <summary>
    /// The string constants which make up the de/serialized data of a trophy.
    /// </summary>
    public static class DataConstants
    {
        // Design Note: The characters ':' and ',' are illegal to use for trophies.
        
        // Example tag:
        // - quarry*OrcWarrior_traits*Chaotic-Evil-Orc-MetalArmor_damagekinds*Slashing_traditions*Occult

        /// <summary>
        /// Begins the identifier and tag string of the trophy ItemModificationKind.
        /// </summary>
        public const string TROPHY_MODIFICATION = "trophy_";
        
        /// <summary>
        /// Separates each list.
        /// </summary>
        public const char LIST_SEPARATOR = '_';

        /// <summary>
        /// Separates items in a list.
        /// </summary>
        public const char ITEM_SEPARATOR = '-';
        
        /// <summary>
        /// The humanized creature name.
        /// </summary>
        public const string CREATURE_NAME = "quarry*";

        /// <summary>
        /// The creature's CreatureId.
        /// </summary>
        public const string CREATURE_ID = "crid*";

        /// <summary>
        /// The list of the trophy's traits.
        /// </summary>
        public const string TRAITS = "traits*";

        /// <summary>
        /// The list of the trophy's damage kinds.
        /// </summary>
        public const string DAMAGE_KINDS = "damagekinds*";

        /// <summary>
        /// The list of the trophy's associated traditions.
        /// </summary>
        public const string TRADITIONS = "tradition*";

        /// <summary>
        /// Begins a list of any other special tags.
        /// </summary>
        public const string TAGS = "tags*";

        /// <summary>
        /// The fog/smoke special vision.
        /// </summary>
        public const string TAGS_SMOKE_VISION = "SmokeVision";

        /// <summary>
        /// The All-Around Vision special vision.
        /// </summary>
        public const string TAGS_ALL_AROUND_VISION = "AllAroundVision";

        /// <summary>
        /// The highest saving throw. This tag can appear more than once, with a different save each time. The Defense is added directly to the end of this constant (implicit invocation of ToString()).
        /// </summary>
        public const string TAGS_HIGHEST_SAVE = "HighestSave";
    }

    public static void Load()
    {
        TrophyCase = ModManager.RegisterNewItemIntoTheShop(
            ModData.IdPrepend + "TrophyCase",
            iN => new Item(
                    iN, 
                    ModData.Illustrations.TrophyCase,
                    "trophy case",
                    0, 0,
                    ModData.Traits.ModName, ModData.Traits.Slayer, Trait.CannotBeHeldInHands)
                .WithDescription(
                    "This case of light bulk is used by slayers to hold their unused trophies.",
                    """
                    A slayer's trophy case can hold up to 5 trophies. Out-of-combat, you can drag-and-drop trophies into the case to save inventory space.

                    You can get one or more starting trophies from the shop.
                    """)
                .WithItemGreaterGroup(ModData.ItemGreaterGroups.ClassItems)
                .WithItemGroup("Slayer")
                .WithStoresItem((tCase, trophy) =>
                {
                    if (!trophy.HasTrait(ModData.Traits.Trophy))
                        return "You can only store trophies in a trophy case";
                    if (tCase.StoredItems.Count >= 5)
                        return "You can only hold 5 trophies at a time";
                    return null;
                }));
        
        TrophyItem = ModManager.RegisterNewItemIntoTheShop(
            ModData.IdPrepend + "Trophy",
            iN => new Item(
                    iN,
                    ModData.Illustrations.Trophy,
                    "trophy",
                    0, 0,
                    ModData.Traits.ModName, ModData.Traits.Trophy, Trait.DoNotAddToShop, Trait.CannotBeHeldInHands)
                /*.WithDescription(
                    "The nature of your trophies varies depending on your quarry, but each has certain common characteristics, determined by the creature from which it was claimed.",
                    "You can Reinforce your Arsenal by dragging-and-dropping this trophy onto one of your hunting tools. Other slayer abilities refer to the following properties when determining the effects of being reinforced with this trophy:")*/
                .WithRuneProperties(new RuneProperties(
                        "reinforced",
                        ModData.RuneKinds.SlayerTrophy,
                        "The nature of your trophies varies depending on your quarry, but each has certain common characteristics, determined by the creature from which it was claimed.",
                        "Your hunting tool gains benefits from your features based on this trophy's properties.",
                        (rune, baseItem) =>
                        {
                            
                        })
                    .WithCanBeAppliedTo((rune, baseItem) =>
                        HuntingTools.IsATool(baseItem)
                            ? null
                            : "You can only attach trophies to items designated as one of your hunting tools.")));
        
        TrophyModification = ItemModifications.RegisterItemModification(
            "trophy",
            mod =>
                DataConstants.TROPHY_MODIFICATION + mod.Tag,
            (tag, modKind) =>
            {
                // Not my modification
                if (!tag.StartsWith(DataConstants.TROPHY_MODIFICATION))
                    return null;
                string subTag = tag[DataConstants.TROPHY_MODIFICATION.Length..];
                var tagData = TrophyStringToData(subTag);
                string dataText = $"\n\n{{b}}Origin{{/b}} {tagData.Name}";
                if (tagData.Traits is not null)
                    dataText += "\n{b}Traits{/b} " + string.Join(
                        ", ",
                        tagData.Traits.Select(t => t.ToStringOrTechnical()));
                if (tagData.Kinds is not null)
                    dataText += "\n{b}Damage Types{/b} " + string.Join(
                        ", ",
                        tagData.Kinds.Select(kind =>
                            kind.ToStringOrTechnical().WithColor(kind.DamageKindToColor())));
                if (tagData.Traditions is not null)
                    dataText += "\n{b}Traditions{/b} " + string.Join(", ", tagData.Traditions.Select(trad =>
                        trad.ToStringOrTechnical().WithColor(trad.TraditionTraitToColor())));
                if (tagData.Tags is not null)
                {
                    // Remove all the save-defense tags
                    List<string> saveTags = tagData.Tags
                        .Where(td => td.Contains(DataConstants.TAGS_HIGHEST_SAVE))
                        .ToList();
                    List<string> humanizedTags = tagData.Tags
                        .Except(saveTags)
                        .Select(TrophyDataTagToString)
                        .ToList();
                    // If there are any, add them back with a more pleasant English sentence structure
                    if (saveTags.Count > 0)
                    {
                        string saveList =
                            "highest save"
                                .PluralizeIf(" is ", "s are ", saveTags.Count)
                            + S.ConstructOrList(
                                saveTags.Select(td =>
                                    TrophyDataTagToString(td).Replace("highest save is ", "")),
                                "and");
                        // Use a semi-colon to separate this list
                        if (humanizedTags.Count > saveTags.Count
                            && saveTags.Count > 3)
                            saveList = "; " + saveList;
                        humanizedTags.Add(saveList);
                    }

                    string propString = string.Join(", ", humanizedTags)
                        .Replace(", ;", ";"); // Fix added semi-colon
                    
                    dataText += "\n{b}Other Properties{/b} " + propString;
                }
                return new ItemModification(modKind)
                {
                    Tag = subTag,
                    ModifyItem = item =>
                    {
                        item.Description += dataText;
                        if (tagData.Traits is not null)
                            item.Traits.AddRange(tagData.Traits);
                    },
                    UnmodifyItem = item =>
                    {
                        item.Description = item.Description!.Replace(dataText, "");
                        tagData.Traits?.ForEach(t => item.Traits.Remove(t));
                    }
                };
            });

        ChosenDamageKindModification = ItemModifications.RegisterItemModification(
            "chosenDamageKind_",
            mod =>
                "chosenDamageKind_" + ((DamageKind)mod.Tag!).ToString(),
            (tag, modKind) =>
            {
                if (!tag.StartsWith("chosenDamageKind_"))
                    return null;
                string subTag = tag["chosenDamageKind_".Length..];
                if (subTag == "Spirit")
                    return null;
                DamageKind chosenKind = DamageKind.Parse(subTag);
                string bonusText = "\n\n{b}Chosen Damage Type{/b} " + chosenKind.ToStringOrTechnical().WithColor(chosenKind.DamageKindToColor());
                return new ItemModification(modKind)
                {
                    Tag = chosenKind,
                    ModifyItem = item =>
                    {
                        item.Description += bonusText;
                    },
                    UnmodifyItem = item =>
                    {
                        item.Description = item.Description!.Replace(bonusText, "");
                    }
                };
            });
        
        // Trophy modifications
        InventoryContextMenu.Options.Add(new InventoryContextMenuOption((slot, item, inv) =>
        {
            // Options are only to modify a trophy
            if (slot.Item is null || item is null || slot.CharacterSheet is null
                || HuntingTools.GetTools(slot.CharacterSheet.Calculated) is null)
                return null;

            bool itemIsTrophyItself = item.HasTrait(ModData.Traits.Trophy);
            Item? trophy = itemIsTrophyItself
                ? item
                : Trophies.GetTrophy(item);

            if (trophy is null)
                return null;
            
            List<ContextMenuItem> options = [];
            HuntingTools.ToolId? specificTool = HuntingTools.GetToolId(item);
            
            // Damage Kind selections (all):
            // - (Signature) Bloodseeking Blade
            // - (Signature) Warded Mail
            // - (Secondary) Paired Bloodseeker, as Bloodseeking Blade
            // - (Secondary) Spirit Oil
            if (itemIsTrophyItself
                || specificTool is HuntingTools.ToolId.BloodseekingBlade or HuntingTools.ToolId.WardedMail or HuntingTools.ToolId.PairedBloodseeker or HuntingTools.ToolId.SpiritOil)
            {
                DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                foreach (DamageKind dk in Trophies.GetTrophyData(trophy)?.Kinds ?? [])
                    SetDamageKind(dk, dk == chosenKind);
            }
            
            // Damage Kind selections (non-physical):
            // - (Signature) Chymist's Vials (only non-physical types)
            // - (Secondary) Bloodburst Phial (only non-physical types)
            if (itemIsTrophyItself
                || specificTool is HuntingTools.ToolId.ChymistsVials or HuntingTools.ToolId.BloodburstPhial)
            {
                DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                foreach (DamageKind dk in Trophies.GetTrophyData(trophy)?.Kinds ?? [])
                    SetDamageKind(dk, dk == chosenKind, true);
            }
            
            // When the user is right-clicking a trophy, only allow them to remove ItemModifications.
            // They must be right-clicking on a hunting tool with an attached trophy to set choices.
            if (itemIsTrophyItself)
                options.RemoveAll(opt => opt.Name.StartsWith("Reinforce"));
            
            return options.Count > 0
                ? options.ToArray()
                : null;

            void SetDamageKind(DamageKind dk, bool alreadySelected, bool noPhysical = false)
            {
                string select = (alreadySelected ? "Unr" : "R") + "einforce";
                string dName = dk.ToStringOrTechnical();
                
                options.Add(new ContextMenuItem(
                    IllustrationName.PersistentDamage,
                    select + ": " + dName + " damage",
                    select + " this Arsenal with " + dName.ToLower() + $" damage for the purposes of this tool's reinforced benefits that require you to choose a{(noPhysical ? " non-physical " : " ")}damage type.",
                    () =>
                    {
                        Trophies.SetChosenDamageKind(trophy, alreadySelected ? null : dk);
                        /*if (!itemIsTrophyItself)
                        {
                            Item newBaseItem = RunestoneRules.RecreateWithUnattachedSubitem(item, trophy, true);
                            RunestoneRules.AddRuneTo(trophy, newBaseItem);
                            //RunestoneRules.AttachSubitem(trophy, newBaseItem);
                            //item = newBaseItem;
                            slot.ReplaceSelf(newBaseItem);
                        }*/
                        /*foreach (GamePhase phase in Root.PhaseStack)
                        {
                            if (phase is CharacterBuilderPhase characterBuilderPhase)
                                characterBuilderPhase.RefreshPlan();
                        }*/
                        /*if (!itemIsTrophyItself && specificTool.HasValue)
                        {
                            HuntingTool tool = tools.First(tool => tool.Id == specificTool.Value);
                            tool.UndesignateAsTool(item);
                            tool.DesignateAsTool(item);
                        }*/
                        Sfxs.Play(SfxName.ReactionQuestion);
                        Toasts.CreateNew(
                            "{b}Technical Limitation{/b}\nYou must detach and reattach this trophy to apply your changes.",
                            Color.FromNonPremultiplied(252, 199, 214, byte.MaxValue),
                            Color.Black,
                            Root.Mouse_NewState_Update.Position + new Point(0, 0));
                    }));
            }
        }));

        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            List<Item> startingTrophies =
            [
                CreateTrophy(TrophyDataToString(
                        "{i}something unspeakable{/i}",
                        CreatureId.None,
                        [Trait.Aberration],
                        [DamageKind.Cold, DamageKind.Piercing],
                        [Trait.Occult],
                        [DataConstants.TAGS_HIGHEST_SAVE+Defense.Reflex]))
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (horrifying aberration)"))),
                CreateTrophy(TrophyDataToString(
                        "{i}an electric beast{/i}",
                        CreatureId.None,
                        [Trait.Beast, Trait.Electricity],
                        [DamageKind.Electricity, DamageKind.Slashing],
                        [Trait.Primal],
                        [DataConstants.TAGS_HIGHEST_SAVE+Defense.Reflex]))
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (sparking beast)"))),
                CreateTrophy(TrophyDataToString(
                        "{i}a fiery dragon{/i}",
                        CreatureId.None,
                        [Trait.Dragon, Trait.Fire],
                        [DamageKind.Fire, DamageKind.Piercing],
                        [Trait.Arcane],
                        [DataConstants.TAGS_HIGHEST_SAVE+Defense.Fortitude]))
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (flame dragon)"))),
                CreateTrophy(TrophyDataToString(
                        "{i}an icy giant{/i}",
                        CreatureId.None,
                        [Trait.Cold, Trait.Giant, Trait.Humanoid],
                        [DamageKind.Bludgeoning, DamageKind.Cold],
                        [Trait.Primal],
                        [DataConstants.TAGS_HIGHEST_SAVE+Defense.Fortitude]))
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (frost giant)"))),
                CreateTrophy(TrophyDataToString(
                        "{i}a ghostly undead{/i}",
                        CreatureId.None,
                        [Trait.Ghost, Trait.Incorporeal, Trait.Spirit, Trait.Undead, UnholyTrait.Unholy],
                        [DamageKind.Bludgeoning, DamageSpirit.Spirit],
                        [Trait.Divine],
                        [DataConstants.TAGS_HIGHEST_SAVE+Defense.Will]))
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (ghostly undead)"))),
            ];
            foreach (Item trophy in startingTrophies)
                trophy
                    //.With(item => item.Traits.Add(Trait.DoNotAddToCampaignShop))
                    .WithItemGreaterGroup(ModData.ItemGreaterGroups.ClassItems)
                    .WithItemGroup("Slayer");
            Items.ShopItems.AddRange(startingTrophies);
        };

        #if DEBUG
        /*ModManager.RegisterActionOnEachCreature(cr =>
        {
            if (cr.PersistentCharacterSheet?.Class?.ClassTrait != ModData.Traits.Slayer)
                return;
            
            cr.AddQEffect(new QEffect()
            {
                ProvideContextualAction = qfThis =>
                {
                    return new SubmenuPossibility(
                        ModData.Illustrations.OnTheHunt,
                        "Slayer Cheats")
                    {
                        Subsections = [
                            new PossibilitySection("Slayer Cheats")
                            {
                                Possibilities = [
                                    (ActionPossibility) new CombatAction(
                                            qfThis.Owner,
                                            ModData.Illustrations.MarkQuarry,
                                            "Mark Quarry",
                                            [Trait.Concentrate, ModData.Traits.Slayer, Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName],
                                            null!,
                                            new CreatureTarget(RangeKind.Ranged, [
                                                new EnemyCreatureTargetingRequirement(),
                                                // Not undetected
                                                new LegacyCreatureTargetingRequirement((a,d) =>
                                                    d.DetectionStatus.IsUndetectedTo(a)
                                                        ? Usability.NotUsableOnThisCreature("Undetected") : Usability.Usable),
                                                // Not already my quarry
                                                new LegacyCreatureTargetingRequirement((a,d) =>
                                                    Slayer.IsMyQuarry(a, d)
                                                        ? Usability.NotUsableOnThisCreature("Already my quarry")
                                                        : Usability.Usable),
                                            ], (_, _, them) => them.Level))
                                        //.WithDescription(markQuarry.FlavorText, markQuarry.RulesText)
                                        .WithActionId(ModData.ActionIds.MarkQuarry)
                                        .WithActionCost(0)
                                        .WithSoundEffect(ModData.SfxNames.MarkQuarry)
                                        .WithTargetingTooltip((_, _, _) =>
                                            "Mark this creature as your quarry.")
                                        .WithEffectOnEachTarget(async (action, caster, target, _) =>
                                        {
                                            target.AddQEffect(Slayer.MarkQuarry(caster));
                                    
                                            // Prettier log flavor
                                            qfThis.Owner.Battle.Log(
                                                $"{qfThis.Owner} {{b}}Marks{{/b}} {{Blue}}{target}{{/Blue}} as their {{b}}Quarry{{/b}}.",
                                                "Mark Quarry {icon:FreeAction}",
                                                action.Description,
                                                action.Traits);
                                        }),
                                    (ActionPossibility) new CombatAction(
                                            qfThis.Owner,
                                            IllustrationName.Trophy,
                                            "Create Trophy",
                                            [Trait.Basic, Trait.AlwaysHits, Trait.UnaffectedByConcealment],
                                            """
                                            Summons and kills all registered creatures in order to generate trophies.

                                            Summons an immortal training dummy in order to avoid an immediate encounter-win from killing the sea serpent.
                                            
                                            You can also summon the target creature to inspect its stat block for validating its trophy's properties.
                                            """,
                                            Target.Self())
                                        .WithActionCost(0)
                                        .WithEffectOnSelf(async caster =>
                                        {
                                            PropertyInfo? prop = typeof(ModManager).GetProperty("ModdedCreatureFactories", BindingFlags.Static | BindingFlags.NonPublic);
                                            object? dictValue = prop?.GetValue(null);
                        
                                            if (dictValue is not Dictionary<string, Func<Encounter?, Tile, Creature>> creatures)
                                                return;
                                            
                                            Creature immortalDummy = TrainingDummy.CreateTrainingDummy(caster.Battle.Encounter)
                                                .AddQEffect(new QEffect(){StateCheck = qf => qf.Owner.DeathScheduledForNextStateCheck = false});
                                            caster.Battle.SpawnCreature(immortalDummy, caster.Battle.Enemy, caster.Space.TopLeftTile);

                                            List<Item> allTrophies = [];
                                            Tile spawnPoint = caster.Battle.Map.AllTiles.First(t =>
                                                t.IsTrulyGenuinelyFreeToEveryCreature &&
                                                caster.Battle.AllCreatures.All(allCr => allCr.DistanceTo(t) > 2));

                                            foreach (var kvp in creatures)
                                            {
                                                Creature trophyCr =
                                                    kvp.Value(caster.Battle.Encounter, caster.Space.TopLeftTile)
                                                    .WithExtraTrait(Trait.NeedNotSurvive);
                                                if (trophyCr.Traits.ContainsOneOf([
                                                        Trait.Object, Trait.IllusoryObject, Trait.Trap
                                                    ]))
                                                    continue;
                                                trophyCr.RecalculateArmor();
                                                trophyCr.RecalculateLandSpeedAndInitiative();
                                                
                                                caster.Battle.SpawnCreature(trophyCr, caster.OwningFaction, spawnPoint);
                                                
                                                trophyCr.RegeneratePossibilities();
                                                trophyCr.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
                                                
                                                Item trophy = CreateTrophy(trophyCr);
                                                trophy.Illustration = trophyCr.Illustration;
                                                trophy.Level = trophyCr.Level;
                                                trophy.Tag = kvp;
                                                trophy.ProsaicName = trophyCr.Name;
                                                allTrophies.Add(trophy);
                                                
                                                await trophyCr.DieFastAndWithoutAnimation();
                                            }
                                                
                                            RequestResult requestResult = await caster.Battle.SendRequest(
                                                new ComboBoxInputRequest<Item>(
                                                    caster,
                                                    "What trophy to spawn?",
                                                    IllustrationName.SummonElemental,
                                                    "Fulltext search...",
                                                    allTrophies
                                                        .OrderBy(item => item.Level)
                                                        .ThenBy(item => item.Name)
                                                        .ToArray(),
                                                    item => new ComboBoxInformation(
                                                        item.Illustration,
                                                        item.Name,
                                                        item.Level.ToString(),
                                                        item.GetItemDescriptionWithoutUsability(),
                                                        item.Name,
                                                        item.Traits.ToList()),
                                                    item => "Spawn " + item.Name,
                                                    "Cancel"));
                                            
                                            if (requestResult.ChosenOption is ComboBoxInputOption<Item> chosenOption2)
                                            {
                                                if (chosenOption2.SelectedObject.Tag is KeyValuePair<string,Func<Encounter?,Tile,Creature>> kvp)
                                                {
                                                    Creature crSpawn = kvp.Value(caster.Battle.Encounter, caster.Space.TopLeftTile);
                                                    caster.Battle.SpawnCreature(crSpawn, caster.Battle.Enemy,
                                                        caster.Space.TopLeftTile);
                                                }
                                            }
                                            
                                            return;
                                        })
                                ]
                            }
                        ]
                    };
                }
            });
        });*/
        #endif
    }

    #region String Parsing and De/Serialization

    /// <summary>
    /// Turns a given set of data into a string. Used for creating new instances of <see cref="TrophyModification"/>.
    /// </summary>
    /// <param name="name">The name of the creature this trophy came from. If null or empty, value is "Unknown".</param>
    /// <param name="id">The creature's CreatureId.</param>
    /// <param name="traits">The list of traits on this trophy.</param>
    /// <param name="kinds">The list of damage types on this trophy.</param>
    /// <param name="traditions">The list of traditions on this trophy. Must include at least one tradition.</param>
    /// <param name="tags">The list of additional miscellaneous tags, such as features it has or other singular properties of like its highest modifier.</param>
    /// <returns>The final data-string of the trophy, to be prepended with "trophy_" or added to the ItemModification's Tag.</returns>
    private static string TrophyDataToString(string name, CreatureId id, List<Trait>? traits, List<DamageKind>? kinds, List<Trait>? traditions, List<string>? tags)
    {
        // Collect all relevant data
        List<string> data = [];
        
        // Name
        string finalName = DataConstants.CREATURE_NAME + (string.IsNullOrEmpty(name) ? "Unknown" : name);
        data.Add(finalName);
        
        // Creature Id
        string finalId = DataConstants.CREATURE_ID + id.ToString();
        data.Add(finalId);
        
        // Traits
        List<Trait> exceptTraits = [Trait.Small, Trait.Large, Trait.Huge, Trait.Gargantuan, Trait.Colossal5, Trait.Colossal6, Trait.Colossal7, Trait.Colossal8, Trait.Uncommon, Trait.Unique];
        List<Trait>? filteredTraits = traits?.ToList();
        filteredTraits?.RemoveAll(t => exceptTraits.Contains(t));
        if (filteredTraits?.Count > 0)
        {
            string finalTraits =
                DataConstants.TRAITS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    filteredTraits.Select(t => t.ToString()));
            data.Add(finalTraits);
        }
        
        // Damage kinds
        if (kinds?.Count > 0)
        {
            string finalKinds =
                DataConstants.DAMAGE_KINDS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    kinds.Select(t => t.ToString()));
            data.Add(finalKinds);
        }

        // Magical traditions
        if (traditions?.Count > 0)
        {
            string finalTraditions =
                DataConstants.TRADITIONS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    traditions.Select(t => t.ToString()));
            data.Add(finalTraditions);
        }
        
        // Special tags
        if (tags?.Count > 0)
        {
            string finalTags =
                DataConstants.TAGS
                + string.Join(
                    DataConstants.ITEM_SEPARATOR,
                    tags);
            data.Add(finalTags);
        }
        
        // Combine data into a final string
        string finalDataString = string.Join(DataConstants.LIST_SEPARATOR, data);
        return finalDataString;
    }
    
    /// <summary>
    /// Parses a given trophy data-string and turns it into usable data.
    /// </summary>
    /// <param name="trophyTag">The data-string of the trophy (the string without "trophy_").</param>
    /// <returns>A tuple containing all the trophy's properties.</returns>
    private static (string? Name, CreatureId? Id, List<Trait>? Traits, List<DamageKind>? Kinds, List<Trait>? Traditions, List<string>? Tags)
        TrophyStringToData(string trophyTag)
    {
        // Example tag:
        // - quarry*Orc Warrior_crid*OrcWarrior_traits*Chaotic-Evil-Orc-MetalArmor_damagekinds*Slashing_traditions*Occult_Tags*HighestSaveFortitude

        string[] lists = trophyTag.Split(DataConstants.LIST_SEPARATOR);
        
        /*Regex.Replace(
            lists[0]["quarry*".Length..],
            @"(?<=[a-z0-9])(?=[A-Z])",
            " ");*/
        string finalName = GetString(DataConstants.CREATURE_NAME) ?? "Unknown";
        
        CreatureId finalId = Enum.Parse<CreatureId>(GetString(DataConstants.CREATURE_ID) ?? "None");

        List<Trait>? finalTraits = GetString(DataConstants.TRAITS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .Select(Trait.Parse)
            .ToList();
        
        List<DamageKind>? finalKinds = GetString(DataConstants.DAMAGE_KINDS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .Select(DamageKind.Parse)
            .ToList();
        
        List<Trait>? finalTraditions = GetString(DataConstants.TRADITIONS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .Select(Trait.Parse)
            .ToList();
        
        List<string>? finalTags = GetString(DataConstants.TAGS)
            ?.Split(DataConstants.ITEM_SEPARATOR)
            .ToList();

        return (finalName, finalId, finalTraits, finalKinds, finalTraditions, finalTags);

        string? GetString(string header)
        {
            string? list = lists.FirstOrDefault(list => list.Contains(header));

            if (list is null || list.Length <= header.Length)
                return null;
            
            return list.Substring(header.Length);
        }
    }
    
    /// <summary>
    /// Turns a creature into its associated trophy data-string.
    /// </summary>
    private static string CreatureToTrophyString(Creature cr)
    {
        List<Trait> traits = cr.Traits
            .Except(TraitBlacklist)
            .ToList();
        
        List<Trait> traditions = [
            ..cr.Traits.Where(trait =>
                trait is Trait.Arcane or Trait.Divine or Trait.Primal or Trait.Occult),
            ..cr.Spellcasting?.Sources.Select(src =>
                src.SpellcastingTradition) ?? []
        ];
        traditions.RemoveDuplicates();
        if (traditions.Count == 0)
            traditions.Add(Trait.Occult);

        List<DamageKind> types = [
            // Actions
            ..cr.Possibilities
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                ?.Filter(ap =>
                {
                    // Exclude: Spells, most item-y attacks
                    if (ap.CombatAction.Traits.ContainsOneOf([Trait.Spell, Trait.Bomb, Trait.Elixir, Trait.Potion]))
                        return false;
                    // Exclude some basic buttons
                    if (ap.CombatAction.ActionId is ActionId.Trip or ActionId.DrawItem or ActionId.DropItem or ActionId.PickUpItem or ActionId.Delay)
                        return false;
                    // Exclude more basic and specific abilities with damage-words.
                    if (ap.CombatAction.Name.ToLower() is "drop prone" or "reposition" or "deal 1000 damage" or "fire bomb" or "enter badger rage" or "death-stealing gaze")
                        return false;
                    // Allow everything else.
                    return true;
                })
                .CreateActions(false)
                .SelectMany(ica =>
                {
                    List<DamageKind> kinds = [];
                    if (ica.Action.Item is { WeaponProperties: {} props } item)
                    {
                        kinds.AddRange(item.DetermineDamageKinds());
                        kinds.AddRange(props.AdditionalDamage.Select(set => set.Item2));
                        if (props.AdditionalSplashDamageFormula is not null)
                            kinds.Add(props.AdditionalSplashDamageKind);
                    }
                    else
                    {
                        kinds.AddRange(GetKindsInDescription(ica.Action.Description));
                    }
                    
                    return kinds;
                }) ?? [],
            
            // QEffects
            ..cr.QEffects
                .SelectMany(qf =>
                {
                    // Ignore effects that can't be parsed with
                    if (qf.Description is null)
                        return [];
                    // Ignore effects that aren't about dealing typed damage to other creatures
                    // This also excludes afflictions
                    if (qf.Name?.ToLower() is { } name
                        && new List<string>(["immunity", "resistance", "weakness", "regeneration", "vulnerability", "resilience", "poison", "venom", "rot", "badger rage", "head regrowth", "split", "death-stealing gaze", "aversion", "healing"])
                            .Any(name.Contains))
                        return [];
                    return GetKindsInDescription(qf.Description);
                }),
            
            // Immunities
            ..cr.WeaknessAndResistance.Immunities.Where(dk =>
                dk is not DamageKind.Bleed and not DamageKind.Untyped)
        ];
        types.RemoveDuplicates();
        types = types
            .OrderBy(dk => dk.ToString())
            .ToList();

        // Handle various stat block observations as extra tags
        List<string> tags = [];
        if (cr.QEffects.Any(qf =>
                qf is { Id: QEffectId.AllAroundVision, ExpiresAt: ExpirationCondition.Never, Dispellable: null }))
            tags.Add(DataConstants.TAGS_ALL_AROUND_VISION);
        if (cr.QEffects.Any(qf =>
                qf is { Id: QEffectId.FogVision, ExpiresAt: ExpirationCondition.Never, Dispellable: null }))
            tags.Add(DataConstants.TAGS_SMOKE_VISION);
        List<Defense> saves = [Defense.Fortitude, Defense.Reflex, Defense.Will];
        tags.AddRange(saves
            .GroupBy(def => cr.Defenses.GetBaseValue(def))
            .OrderByDescending(grp => grp.Key)
            .First()
            .Select(def => DataConstants.TAGS_HIGHEST_SAVE + def)
        );
        
        return TrophyDataToString(
            cr.BaseName,
            cr.CreatureId,
            traits.Count > 0 ? traits : null,
            types.Count > 0 ? types : null,
            traditions.Count > 0 ? traditions : null,
            tags.Count > 0 ? tags : null);
        
        List<DamageKind> GetKindsInDescription(string desc)
        {
            List<DamageKind> kinds = [];
            string valid = desc.ToLower();
            string[] allWords = valid.Split([' ', '.', ',', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
            string[] noBeforeWords = ["persistent", "life"];
            string[] noAfterWords = ["energy", "resistance", "resistant", "weakness", "effect", "effects", "curse"];
            foreach (DamageKind kind in DamageKind.GetValues())
            {
                bool found = false;
                if (kind is DamageKind.Bleed)
                    continue;
                string dkStr = kind.ToStringOrTechnical().ToLower();
                for (int i=0; i < allWords.Length; i++)
                {
                    // Must be found
                    if (allWords[i] != dkStr)
                        continue;
                    // Check previous word
                    if (i > 0)
                    {
                        string previous = allWords[i-1];
                        if (noBeforeWords.Contains(previous))
                            continue;
                        // skip errors from certain list-sentences
                        if (previous is "and" or "or")
                        {
                            // "persistent X (and|or) Y" for Y
                            if (i > 2 && allWords[i-3] == "persistent")
                                continue;
                            // "malice and evil" for evil
                            if (i > 1 && allWords[i-2] == "malice")
                                continue;
                        }
                    }
                    // Check next word
                    if (i < allWords.Length-1 && noAfterWords.Contains(allWords[i + 1]))
                        continue;
                    found = true;
                }
                if (found)
                    kinds.Add(kind);
            }

            return kinds;
        }
    }

    /// <summary>
    /// Gets the humanized name, description, or entry of a value stored in <see cref="DataConstants.TAGS"/>. This gets a portion of the data from <see cref="ItemModification.Tag"/>, unrelated to the whole tag itself.
    /// </summary>
    public static string TrophyDataTagToString(string tag)
    {
        if (tag == DataConstants.TAGS_ALL_AROUND_VISION)
            return "All-Around Vision";
        if (tag == DataConstants.TAGS_SMOKE_VISION)
            return "Smoke Vision";
        if (tag.Contains(DataConstants.TAGS_HIGHEST_SAVE))
            return Enum.TryParse(tag[DataConstants.TAGS_HIGHEST_SAVE.Length..], true, out Defense defense)
                ? "highest save is " + defense.ToStringOrTechnical().WithColor(defense.ToColor())
                : throw new Exception("Unknown Defense for Data Tag HighestSave: " + tag);
        throw new Exception("Unknown Trophy Data Tag: " + tag);
    }

    #endregion

    #region Creating Trophies

    private static Item CreateTrophy(string trophyData)
    {
        if (trophyData.StartsWith(DataConstants.TROPHY_MODIFICATION))
            trophyData = trophyData.Remove(0, DataConstants.TROPHY_MODIFICATION.Length);
        Item trophy = Items.CreateNew(Trophies.TrophyItem)
            .WithModification(ItemModification.Create(DataConstants.TROPHY_MODIFICATION + trophyData));
        return trophy;
    }

    public static Item CreateTrophy(Creature cr)
    {
        return CreateTrophy(CreatureToTrophyString(cr));
    }

    #endregion

    #region Getting Trophies

    public static Item? GetTrophy(Item item)
    {
        return item.ActiveRunes.FirstOrDefault(r => r.HasTrait(ModData.Traits.Trophy));
    }
    
    public static (string? Name, CreatureId? Id, List<Trait>? Traits, List<DamageKind>? Kinds, List<Trait>? Traditions, List<string>? Tags)?
        GetTrophyData(Item trophy)
    {
        ItemModification? trophyMod = trophy.ItemModifications.FirstOrDefault(mod => mod.Kind == TrophyModification);
        if (trophyMod is null || trophyMod.Tag is not string tagString)
            return null;
        return TrophyStringToData(tagString);
    }

    #endregion

    #region Modifying Trophies

    public static DamageKind? GetChosenDamageKind(Item trophy)
    {
        var chosenDamage = trophy.ItemModifications.FirstOrDefault(mod =>
            mod.Kind == ChosenDamageKindModification);
        return chosenDamage?.Tag is DamageKind tag
            ? tag
            : null;
    }

    /// <summary>
    /// Applies a <see cref="ChosenDamageKindModification"/> to the trophy of the given <see cref="DamageKind"/>. Removes all other such modifications (only 1 at a time).
    /// </summary>
    public static void SetChosenDamageKind(Item trophy, DamageKind? kind)
    {
        foreach (ItemModification mod in trophy.ItemModifications
                     .Where(mod =>
                         mod.Kind == ChosenDamageKindModification)
                     .ToList())
            trophy.WithoutModification(mod);
        
        if (kind is not null)
            trophy.WithModification(ItemModification.Create("chosenDamageKind_" + kind.Value.ToString()));
    }

    #endregion
}