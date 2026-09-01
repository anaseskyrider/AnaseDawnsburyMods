using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display;
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

    public static void Load()
    {
        TrophyCase = ModManager.RegisterNewItemIntoTheShop(
            ModData.ID_PREPEND + "TrophyCase",
            iN => new Item(
                    iN, 
                    ModData.Illustrations.TrophyCase,
                    "trophy case",
                    0, 0,
                    ModData.ModTrait, ModData.Traits.Slayer, Trait.CannotBeHeldInHands)
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
            ModData.ID_PREPEND + "Trophy",
            iN => new Item(
                    iN,
                    ModData.Illustrations.Trophy,
                    "trophy",
                    0, 0,
                    ModData.ModTrait, ModData.Traits.Trophy, Trait.DoNotAddToShop, Trait.CannotBeHeldInHands)
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
                        HuntingTool.IsATool(baseItem)
                            ? null
                            : "You can only attach trophies to items designated as one of your hunting tools.")));
        
        TrophyModification = ItemModifications.RegisterItemModification(
            "trophy",
            mod =>
                TrophyData.DataConstants.TROPHY_MODIFICATION + mod.Tag,
            (tagString, modKind) =>
            {
                // Not my modification
                if (!tagString.StartsWith(TrophyData.DataConstants.TROPHY_MODIFICATION))
                    return null;
                string dataString = tagString[TrophyData.DataConstants.TROPHY_MODIFICATION.Length..];
                TrophyData data = TrophyData.FromDataString(dataString);
                string dataText = $"\n\n{{b}}Origin{{/b}} {data.Name}";
                if (data.Traits.Count > 0)
                    dataText += "\n{b}Traits{/b} " + string.Join(
                        ", ",
                        data.Traits.Select(t => t.HumanizeTitleCase2()));
                if (data.Kinds.Count > 0)
                    dataText += "\n{b}Damage Types{/b} " + string.Join(
                        ", ",
                        data.Kinds.Select(kind =>
                            kind.HumanizeTitleCase2().WithColor(kind.DamageKindToColor())));
                if (data.Traditions.Count > 0)
                    dataText += "\n{b}Traditions{/b} " + string.Join(", ", data.Traditions.Select(trad =>
                        trad.HumanizeTitleCase2().WithColor(trad.TraditionTraitToColor())));
                if (data.Tags.Count > 0)
                {
                    // Remove all the save-defense tags
                    List<string> saveTags = data.Tags
                        .Where(td => td.Contains(TrophyData.DataConstants.TAGS_HIGHEST_SAVE))
                        .ToList();
                    List<string> humanizedTags = data.Tags
                        .Except(saveTags)
                        .Select(TrophyData.TrophyDataTagToString)
                        .ToList();
                    // If there are any, add them back with a more pleasant English sentence structure
                    if (saveTags.Count > 0)
                    {
                        string saveList =
                            "highest save"
                                .PluralizeIf(" is ", "s are ", saveTags.Count)
                            + S.ConstructOrList(
                                saveTags.Select(td =>
                                    TrophyData.TrophyDataTagToString(td).Replace("highest save is ", "")),
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
                    Tag = dataString,
                    ModifyItem = item =>
                    {
                        item.Description += dataText;
                        if (data.Traits.Count > 0)
                            item.Traits.AddRange(data.Traits);
                    },
                    UnmodifyItem = item =>
                    {
                        item.Description = item.Description!.Replace(dataText, "");
                        data.Traits?.ForEach(t => item.Traits.Remove(t));
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
                || HuntingToolsTag.GetTools(slot.CharacterSheet.Calculated) is null)
                return null;

            bool itemIsTrophyItself = item.HasTrait(ModData.Traits.Trophy);
            Item? trophy = itemIsTrophyItself
                ? item
                : Trophies.GetTrophy(item);

            if (trophy is null)
                return null;
            
            List<ContextMenuItem> options = [];
            ToolId? specificTool = HuntingTool.GetToolId(item);
            
            // Damage Kind selections (all):
            // - (Signature) Bloodseeking Blade
            // - (Signature) Warded Mail
            // - (Secondary) Paired Bloodseeker, as Bloodseeking Blade
            // - (Secondary) Spirit Oil
            if (itemIsTrophyItself
                || specificTool is ToolId.BloodseekingBlade
                    or ToolId.WardedMail
                    or ToolId.PairedBloodseeker
                    or ToolId.SpiritOil)
            {
                DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                foreach (DamageKind dk in ((TrophyData?)trophy)?.Kinds ?? [])
                    SetDamageKind(dk, dk == chosenKind);
            }
            
            // Damage Kind selections (non-physical):
            // - (Signature) Chymist's Vials (only non-physical types)
            // - (Secondary) Bloodburst Phial (only non-physical types)
            if (itemIsTrophyItself
                || specificTool is ToolId.ChymistsVials
                    or ToolId.BloodburstPhial)
            {
                DamageKind? chosenKind = Trophies.GetChosenDamageKind(trophy);
                foreach (DamageKind dk in ((TrophyData?)trophy)?.Kinds.Where(kind => !kind.IsPhysical()) ?? [])
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

        // Add starting trophies
        LoadOrder.AtEndOfLoadingSequence += () =>
        {
            List<Item> startingTrophies =
            [
                CreateTrophy(new TrophyData(
                            "{i}something unspeakable{/i}",
                            CreatureId.None,
                            [Trait.Aberration],
                            [DamageKind.Cold, DamageKind.Piercing],
                            [Trait.Occult],
                            [TrophyData.DataConstants.TAGS_HIGHEST_SAVE+Defense.Reflex])
                        .ToDataString())
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (horrifying aberration)"))),
                CreateTrophy(new TrophyData(
                            "{i}an electric beast{/i}",
                            CreatureId.None,
                            [Trait.Beast, Trait.Electricity],
                            [DamageKind.Electricity, DamageKind.Slashing],
                            [Trait.Primal],
                            [TrophyData.DataConstants.TAGS_HIGHEST_SAVE+Defense.Reflex])
                        .ToDataString())
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (sparking beast)"))),
                CreateTrophy(new TrophyData(
                            "{i}a fiery dragon{/i}",
                            CreatureId.None,
                            [Trait.Dragon, Trait.Fire],
                            [DamageKind.Fire, DamageKind.Piercing],
                            [Trait.Arcane],
                            [TrophyData.DataConstants.TAGS_HIGHEST_SAVE+Defense.Fortitude])
                        .ToDataString())
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (flame dragon)"))),
                CreateTrophy(new TrophyData(
                            "{i}an icy giant{/i}",
                            CreatureId.None,
                            [Trait.Cold, Trait.Giant, Trait.Humanoid],
                            [DamageKind.Bludgeoning, DamageKind.Cold],
                            [Trait.Primal],
                            [TrophyData.DataConstants.TAGS_HIGHEST_SAVE+Defense.Fortitude])
                        .ToDataString())
                    .With(item => item.WithModification(ItemRenaming.CreateRenameModification("starting trophy (frost giant)"))),
                CreateTrophy(new TrophyData(
                            "{i}a ghostly undead{/i}",
                            CreatureId.None,
                            [Trait.Ghost, Trait.Incorporeal, Trait.Spirit, Trait.Undead, UnholyTrait.Unholy],
                            [DamageKind.Bludgeoning, DamageSpirit.Spirit],
                            [Trait.Divine],
                            [TrophyData.DataConstants.TAGS_HIGHEST_SAVE+Defense.Will])
                        .ToDataString())
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

    #region Creating Trophies

    /// <summary>
    /// Creates a trophy from a string.
    /// </summary>
    /// <param name="trophyData">The data string (the serializable string containing the trophy's data) or tag string (the data string with "trophy_" prepended).</param>
    /// <returns></returns>
    private static Item CreateTrophy(string trophyData)
    {
        if (trophyData.StartsWith(TrophyData.DataConstants.TROPHY_MODIFICATION))
            trophyData = trophyData.Remove(0, TrophyData.DataConstants.TROPHY_MODIFICATION.Length);
        Item trophy = Items.CreateNew(Trophies.TrophyItem)
            .WithModification(ItemModification.Create(TrophyData.DataConstants.TROPHY_MODIFICATION + trophyData));
        return trophy;
    }

    /// <summary>
    /// Creates a trophy item from a creature.
    /// </summary>
    public static Item CreateTrophy(Creature cr)
    {
        return CreateTrophy(TrophyData.FromCreature(cr).ToDataString());
    }

    #endregion

    #region Getting Trophies

    public static Item? GetTrophy(Item item)
    {
        return item.ActiveRunes.FirstOrDefault(r => r.HasTrait(ModData.Traits.Trophy));
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