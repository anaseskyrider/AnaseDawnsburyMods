using Dawnsbury.Audio;
using Dawnsbury.Campaign.Encounters;
using Dawnsbury.Campaign.Encounters.Tutorial;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.Animations.Movement;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Mechanics.Zoning;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.JustCrossTheBridge;

// TODO: Alternative triggers?
// TODO: Save-based trap distance and damage, scaled to difficulty (scale the damage and distance less, leveraging difficulty modifiers?)

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// A level 5 encounter which requires you to simply cross a bridge.
/// </summary>
/// <para>When launched off the bridge, the distance increases with difficulty, as does the distance you are pushed (the DC is unaffected).</para>
/// <para>
/// <list type="bullet">
///     <listheader>Difficulty Scale:</listheader>
///     <item>Easy (???): As Medium, but with extra modifiers to all creatures.</item>
///     <item>
///         <list type="bullet">
///             <listheader>Medium (Moderate, 80 XP):</listheader>
///             <item>15 XP: Kobold Dragon Mage</item>
///             <item>15 XP: Orc Warchief</item>
///             <item>15 XP: Orc Ranger 1 (weak)</item>
///             <item>15 XP: Orc Ranger 2 (weak)</item>
///             <item>20 XP: Giant Monitor Lizard 1 (elite)</item>
///             <item>= 80 XP</item>
///         </list>
///     </item>
///     <item>
///         <list type="bullet">
///             <listheader>Hard (Severe, 120 XP):</listheader>
///             <item>20 XP: Kobold Dragon Mage (elite)</item>
///             <item>20 XP: Orc Warchief (elite)</item>
///             <item>20 XP: Orc Ranger 1</item>
///             <item>20 XP: Orc Ranger 2</item>
///             <item>20 XP: Orc Ranger 3</item>
///             <item>20 XP: Giant Monitor Lizard 1 (elite)</item>
///             <item>= 120 XP</item>
///         </list>
///     </item>
///     <item>
///         <list type="bullet">
///             <listheader>Insane (Extreme, 160 XP):</listheader>
///             <item>20 XP: Kobold Dragon Mage (elite)</item>
///             <item>20 XP: Orc Warchief (elite)</item>
///             <item>20 XP: Orc Ranger 1</item>
///             <item>20 XP: Orc Ranger 2</item>
///             <item>20 XP: Orc Ranger 3</item>
///             <item>20 XP: Orc Ranger 4</item>
///             <item>20 XP: Giant Monitor Lizard 1 (elite)</item>
///             <item>20 XP: Giant Monitor Lizard 1 (elite)</item>
///             <item>= 160 XP</item>
///         </list>
///     </item>
/// </list>
/// </para>
public class JustCrossTheBridge : Encounter
{
    #region Constants

    /// <summary>
    /// The name of the QEffect that indicates you are swimming in the river and must make checks.
    /// </summary>
    public const string DifficultSwimEffectName = "Flowing river";
    /// <summary>
    /// The DC to successfully swim against the river.
    /// </summary>
    /// <seealso cref="DifficultSwim"/>
    public const int SwimDC = 20;
    public const int GrabEdgeDC = 15;
    
    /// <summary>
    /// The name of any QEffect which is part of a <see cref="Zone"/> that begins an encounter.
    /// </summary>
    /// <seealso cref="CreateEncounterTrigger"/>
    public const string BeginEncounterId = "[ENCOUNTER START TRIGGER]";

    #endregion

    #region Static Data

    /// <summary>
    /// The river pushes you 5 feet per round, and an additional 5 feet for each difficulty tier above Easy.
    /// </summary>
    public static int RiverSpeed => Math.Max(1, (int)PlayerProfile.Instance.Difficulty);

    #endregion

    #region Instance Data

    /// <summary>True if the only set of creatures in this encounter have been spawned.</summary>
    public bool InitialSpawnComplete { set; get; }
    
    /// <summary>True if the encounter has completed but a player remained in the river and spoken a dialog about leaving the river to win.</summary>
    public bool DeclaredMustLeaveRiver { set; get; }

    #region Regions

    /// <summary>The tiles corresponding to the river itself.</summary>
    public required List<Tile> RegionRiver;
    
    /// <summary>The tiles corresponding to the southern entrance to the river.</summary>
    public required List<Tile> RegionRiverEntrance;
    
    /// <summary>The tiles corresponding to the bridge.</summary>
    public required List<Tile> RegionBridge;
    
    /// <summary>The tiles corresponding to the waterfall.</summary>
    public required List<Tile> RegionWaterfall;
    
    /// <summary>The tiles that trigger the grabbable-log dialog interruption.</summary>
    public required List<Tile> RegionLogHint;
    
    /// <summary>The tiles from which you are in reach to grab the log.</summary>
    public required List<Tile> RegionLogGrabbable;
    
    /// <summary>The tiles that begin the encounter from the bridge trigger.</summary>
    public required List<Tile> RegionBridgeTrigger;
    
    /// <summary>The tiles that begin the encounter from the other-side trigger.</summary>
    public required List<Tile> RegionOtherSideTrigger;

    #endregion
    
    #region Zones

    public required Zone RiverZone;
    public required Zone LogHintZone;
    public required Zone WaterfallZone;

    #endregion

    #endregion
    
    public JustCrossTheBridge(string mapFilename)
        : base("Just Cross the Bridge", mapFilename, null, 0)
    {
        // Fill out the map with content, make Tiled corrections
        this.ReplaceTriggerWithCinematic(
            TriggerName.StartOfEncounterBeforeStateCheck,
            async battle =>
            {
                RegionRiver = battle.Map.AllTiles
                    .Where(tile =>
                        tile.Kind is TileKind.ShallowWater
                        && tile.X > 1)
                    .ToList();
                RegionRiverEntrance = battle.Map
                    .TilesInRectangle(21,19, 23,19)
                    .ToList();
                RegionBridge = battle.Map
                    .TilesInRectangle(25,12, 26, 19)
                    .ToList();
                RegionWaterfall = battle.Map
                    .TilesInRectangle(0, 11, 1, 18)
                    .ToList();
                RegionLogHint = battle.Map
                    .TilesInRectangle(2, 11, 10, 18)
                    .ToList();
                RegionLogGrabbable = battle.Map
                    .TilesInRectangle(5, 11, 7, 14)
                    .ToList();
                RegionBridgeTrigger = battle.Map
                    .TilesInRectangle(25, 12, 26, 15)
                    .ToList();
                RegionOtherSideTrigger = battle.Map.AllTiles
                    .Where(tile =>
                        tile.Kind is not TileKind.ShallowWater
                        && tile.Y < 12)
                    .ToList();
                
                RiverZone = CreateRiverZone(battle, RiverSpeed, SwimDC);
                LogHintZone = CreateLogHintZone(battle);
                WaterfallZone = CreateWaterfallZone(battle);
                
                // Lizard1 is Elite on Medium
                battle.Map.Tiles[11, 13].CreatureSpawnOptions.EliteOnDifficulty = Difficulty.Easy;
                
                // Add chest contents
                Creature campChest = WithOpen(
                    battle.Map.Tiles[13, 4].PrimaryOccupant!,
                    async (caster, _) =>
                    {
                        Sfxs.Play(SfxName.ItemGet);
                        Item potion = Items.CreateNew(ItemName.PotionOfFlying);
                        if (caster.HasFreeHand && await caster.AskForConfirmation(
                                IllustrationName.PotionOfFlying,
                                "{b}Potion of flying{/b}\nWhere would you like to put this item?",
                                "{icon:DropItem} Hand",
                                "{icon:Inventory} Inventory"))
                            caster.AddHeldItem(potion);
                        else
                            CommonEnvironmentActions.PickUpItem(caster, potion);
                    });
                campChest.MainName = "Camp chest";

                Creature riverChest = WithOpen(
                    CreateChest(battle.Map.GetTile(3, 13)!),
                    async (caster, _) =>
                    {
                        Sfxs.Play(SfxName.ItemGet);
                        Item potion = Items.CreateNew(ItemName.PotionOfFlying);
                        if (caster.HasFreeHand && await caster.AskForConfirmation(
                                IllustrationName.PotionOfFlying,
                                "{b}Potion of flying{/b}\nWhere would you like to put this item?",
                                "{icon:DropItem} Hand",
                                "{icon:Inventory} Inventory"))
                            caster.AddHeldItem(potion);
                        else
                            CommonEnvironmentActions.PickUpItem(caster, potion);
                    },
                    // Add a free hand req so you don't accidentally end up without enough actions to save your life
                    (a, _) =>
                        a.HasFreeHand
                            ? Usability.Usable
                            : Usability.CommonReasons.YouMustHaveAFreeHandToOpenAChest);
                riverChest.MainName = "River chest";
                battle.SpawnCreature(riverChest, battle.Gaia, 3, 13);
                riverChest.DetectionStatus.Undetected = true;
                battle.Pseudocreature.AddQEffect(new QEffect()
                {
                    Name = "[UNDETECTED UNTIL CLOSE ENOUGH]",
                    StateCheck = qfThis =>
                    {
                        // If you're next to the chest or sufficiently close to the waterfall,
                        // the chest becomes permanently detected.
                        if (riverChest.Battle.AllCreatures
                            .Where(cr => cr != riverChest)
                            .Any(cr =>
                                cr.IsAdjacentTo(riverChest)
                                || cr.Space.AnyTile(tile => tile is { X: < 4, Y: > 10 and < 19 })))
                        {
                            qfThis.ExpiresAt = ExpirationCondition.Immediately;
                            riverChest.DetectionStatus.Undetected = false;
                        }
                        else
                            riverChest.DetectionStatus.Undetected = true;
                    }
                });
            });
        
        // Beginning encounter cinematics
        this.ReplaceTriggerWithCinematic(
            TriggerName.StartOfEncounter,
            async battle =>
            {
                Sfxs.BeginSong(Songname.UnderwaterTension);
                
                Creature annacoesta = battle.Cinematics.FindCharacter(CreatureId.Annacoesta);
                Creature scarlet = battle.Cinematics.FindCharacter(CreatureId.Scarlet);
                Creature tokdar = battle.Cinematics.FindCharacter(CreatureId.Tokdar);
                Creature saffi = battle.Cinematics.FindCharacter(CreatureId.Saffi);
                
                await LineAsync(tokdar, "Look, a bridge! We need to be careful, that's clearly a Chekhov's Gun. I bet it'll collapse as soon as we cross it.");
                
                await battle.Cinematics.MoveAsync(scarlet, 26, 21);
                Sfxs.BeginAmbienceNoise(ModLoader.RiverAmbiance);
                scarlet.Overhead("Seek", Color.Black);
                Sfxs.Play(SfxName.Hide);
                await battle.Cinematics.WaitATinyBit();
                await LineAsync(scarlet, "Nothing seems to be wrong with the construction. It should hold.");
                
                await battle.Cinematics.MoveAsync(saffi, 26, 22);
                await LineAsync(saffi, "Then we'll be fine... right?", true);
                
                await battle.Cinematics.MoveAsync(annacoesta, 25, 21);
                await LineAsync(annacoesta, "It can't hurt to be careful. We should prepare ourselves before we continue.", true);
                
                await battle.Cinematics.MoveAsync(tokdar, 25, 22);
                await LineAsync(tokdar, "You heard the Battle Leader! Get ready!", true, true);
                
                // Bridge encounter trigger
                Zone upperBridge = CreateEncounterTrigger(
                    battle,
                    RegionBridgeTrigger!,
                    async (battle2, cinema, zoneThis, crTrigger) =>
                    {
                        List<Creature> heroes = HeroesAndPals(battle2);
                        
                        // Slide heroes onto the bridge
                        foreach (Creature hero in heroes
                                     .Where(hero => !zoneThis.AffectedTiles.Contains(hero.Occupies)))
                        {
                            hero.Overhead("*railroaded*", Color.Red);
                            await cinema.MoveToButDoNotBumpAsync(hero, crTrigger.Occupies.Neighbours.Bottom!.Tile);
                        }
                        // Slide animals onto the bridge
                        
                        await cinema.WaitATinyBit();
                        battle2.SmartCenter(24,10);
                        await cinema.WaitATinyBit();
                        
                        // Spawn initial
                        Creature mage = cinema.SpawnSkipped(battle2.Map.Tiles[21, 8])!
                            .WithExtraTrait(Trait.Female); // Adds emote variance
                        Creature chief = cinema.SpawnSkipped(battle2.Map.Tiles[20, 8])!
                            .WithAIModification(ai =>
                            {
                                ai.OverrideDecision = BraceDecision();
                            });
                        
                        await cinema.WaitATinyBit();
                        
                        // Begin dialog
                        await cinema.MoveAsync(mage, 23, 10);
                        await cinema.MoveAsync(chief, 24, 10);
                        await LineAsync(chief, "You fools! You've stepped into our trap!", true);
                        await LineAsync(tokdar, "I knew it!", angry: true);
                        await LineAsync(mage, "You mean MY trap! Runes of Impact, activate!", angry: true);
                        
                        // Bridge traps
                        battle2.SmartCenter(25,15);
                        await cinema.WaitATinyBit();
                        Sfxs.Play(SfxName.OminousActivation);
                        string bridgeTrapId = "[BRIDGE TRAP]";
                        foreach (Tile tile in RegionBridge!)
                        {
                            tile.TileOverhead("*illusion fades*", Color.MediumPurple);
                            tile.AddQEffect(new TileQEffect()
                            {
                                Name = bridgeTrapId,
                                Illustration = IllustrationName.TrapOfSpellDestruction,
                            });
                        }
                        
                        await cinema.WaitABit();
                        await cinema.WaitABit();
                        await cinema.WaitABit();
                        await cinema.WaitABit();

                        await LineAsync(annacoesta, "Brace yourselves!", angry: true);

                        // Kaboom
                        CombatAction cinematicExplosion = new CombatAction(
                                crTrigger,
                                IllustrationName.TrapOfSpellDestruction,
                                "[BRIDGE TRAP]",
                                [Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName, Trait.DoNotShowOverheadOfCheckResult],
                                "",
                                Target.Burst(100, 4))
                            .WithSoundEffect(SfxName.Fireball)
                            .WithProjectileCone(IllustrationName.TrapOfSpellDestruction, 15, ProjectileKind.Cone)
                            .WithActionCost(0)
                            .WithEffectOnEachTarget(async (_, _, target, _) =>
                            {
                                // Take damage from explosion
                                await CommonSpellEffects.DealDirectDamage(
                                    null,
                                    DiceFormula.FromText("3d6"),
                                    target,
                                    CheckResult.Failure,
                                    DamageKind.Bludgeoning);
                            })
                            .WithEffectOnChosenTargets(async (_, _, _) =>
                            {
                                // Remove trap illustrations
                                foreach (Tile tile in RegionBridge!)
                                    tile.RemoveAllQEffects(qf => qf.Name == bridgeTrapId);
                        
                                // Toss into the water
                                const string cinematicFlightId = "[CINEMATIC FLIGHT]";
                                int offset = Math.Max(0, (int)PlayerProfile.Instance.Difficulty - 1) * 2;
                                
                                Dictionary<Creature, (int x, int y)> moves = [];
                                int i = 0;
                                int x = 22 - offset;
                                int y = 15;
                                foreach (Creature hero in heroes)
                                {
                                    moves.Add(
                                        hero.AddQEffect(new QEffect() // Add flying to get over the walls
                                        {
                                            Name = cinematicFlightId,
                                            Id = QEffectId.Flying,
                                        }),
                                        ((x - i % 2), y));
                                    y += i % 2; // Only go down every other iteration, forming a 2 by N column.
                                    i++;
                                }
                                
                                await MoveGroup(cinema, moves);
                                
                                Sfxs.Play(SfxName.ElementalBlastWater);
                                
                                foreach (Creature hero in heroes) // Remove flying after
                                    hero.RemoveAllQEffects(qf => qf.Name == cinematicFlightId);
                            });
                        ChosenTargets chosen = new ChosenTargets
                        {
                            ChosenPointOfOrigin = new Point(crTrigger.Space.CenterTile.X, crTrigger.Space.CenterTile.Y)
                        };
                        chosen.SetFromArea(
                            (BurstAreaTarget)cinematicExplosion.Target,
                            Areas.DetermineTiles(
                                (BurstAreaTarget)cinematicExplosion.Target,
                                chosen.ChosenPointOfOrigin)?.TargetedTiles ?? []);
                        await battle2.GameLoop.FullCast(cinematicExplosion, chosen);
                        
                        Sfxs.SlideIntoSong(Songname.HighTensionBegins);
                        await cinema.WaitABit();

                        // More dialog
                        await LineAsync(chief, "I hope you can swim,\"Heroes\"!");
                        battle2.SmartCenter(3, 17);
                        await cinema.WaitATinyBit();
                        await LineAsync(scarlet,
                            "This river is flowing toward the waterfall fast. I estimate instant death if we cross over. We have to get back to shore quickly!",
                            true);
                        await LineAsync(mage, "It's a good thing I planned for this, too!");
                        
                        battle2.SmartCenter(19, 15);
                    });

                // North-side encounter trigger
                Zone beyondBridge = CreateEncounterTrigger(
                    battle,
                    RegionOtherSideTrigger!,
                    async (battle2, cinema, zoneThis, crTrigger) =>
                    {
                        List<Creature> heroes = HeroesAndPals(battle2);
                        
                        battle2.SmartCenter(22,8);
                        await cinema.WaitATinyBit();
                        
                        // Spawn initial
                        Creature mage = cinema.SpawnSkipped(battle2.Map.Tiles[21, 8])!
                            .WithExtraTrait(Trait.Female); // Adds emote variance
                        Creature chief = cinema.SpawnSkipped(battle2.Map.Tiles[20, 8])!
                            .WithAIModification(ai =>
                            {
                                ai.OverrideDecision = BraceDecision();
                            });
                        
                        await cinema.WaitATinyBit();

                        // Begin cinematic
                        await LineAsync(mage, "Ahk! They saw through our trap!", true, true);
                        await LineAsync(tokdar, "I knew it!", angry: true);
                        await LineAsync(chief, "You mean YOUR trap!", angry: true);
                        await LineAsync(mage, "Whatever! Plan B!", true, true);
                        
                        // Cinematic grapple
                        await chief.FictitiousSingleTileMove(mage);
                        Sfxs.Play(SfxName.Grapple);
                        mage.Overhead("grappled", Color.Green);
                        QEffect cinematicGrabbed = QEffect.Grabbed(chief).WithExpirationNever();
                        cinematicGrabbed.DoNotShowUpOverhead = true;
                        mage.AddQEffect(cinematicGrabbed);
                        await chief.FictitiousSingleTileMove(chief);
                        await LineAsync(chief, "WHAT PLAN B!?", true, true);
                        
                        // Slide heroes up
                        foreach (Creature hero in heroes
                                     .Except([crTrigger]))
                        {
                            await cinema.MoveToButDoNotBumpAsync(hero, crTrigger.Occupies);
                        }
                        await LineAsync(saffi, "Uh... should we... say something?");
                        await LineAsync(annacoesta, "Never interrupt the enemy when they're making a mistake.", true);
                        
                        // Finish squabble
                        Sfxs.SlideIntoSong(Songname.HighTensionBegins);
                        await LineAsync(mage, "Just attack!");
                        await cinema.WaitATinyBit();
                        Sfxs.Play(SfxName.DropItem);
                        mage.RemoveAllQEffects(qf => qf.Id is QEffectId.Grabbed or QEffectId.Immobilized);
                        chief.Overhead("*release*", Color.WhiteSmoke);
                    });
            });
        
        this.Triggers[TriggerName.AllEnemiesDefeated] = async battle =>
        {
            if (InitialSpawnComplete)
            {
                List<Creature> heroes = battle.AllCreatures
                    .Where(cr => cr.OwningFaction.IsPlayer)
                    .ToList();
                if (heroes.All(hero => !RegionRiver!.Contains(hero.Occupies)))
                {
                    await battle.EndTheGame(
                        true,
                        DeclaredMustLeaveRiver
                            ? "You've escaped the river!"
                            : "You've defeated all enemies!");
                }
                else if (DeclaredMustLeaveRiver is false
                         && heroes.FirstOrDefault(hero =>
                             RegionRiver!.Contains(hero.Occupies)) is {} swimmer)
                {
                    await ShowQuickBubble(battle, swimmer, "The coast isn't clear just yet. I'll have to get to safety before it's over.");
                    DeclaredMustLeaveRiver = true;
                }
            }
        };
    }

    public static List<Creature> HeroesAndPals(TBattle battle)
    {
        return battle.AllCreatures
            .Where(cr =>
                cr.OwningFaction.IsPlayer // Is player.
                || (cr.HasTrait(Trait.AnimalCompanion) // Or is animal companion,
                    && cr.QEffects.Any(qf =>
                        qf.Id == QEffectId.RangersCompanion // That is owned by a player
                        && (qf.Source?.OwningFaction.IsPlayer ?? false))))
            .ToList();
    }

    public Zone CreateEncounterTrigger(TBattle battle, List<Tile> region, Func<TBattle, Cinematics, Zone, Creature, Task> doCutscene)
    {
        return Zone.SpawnStaticAndApply(
            battle.Pseudocreature,
            region,
            zone =>
            {
                zone.ControllerQEffect.Name = BeginEncounterId;
                zone.AfterCreatureEntersOrMovesWithin = async cr =>
                {
                    if (!cr.OwningFaction.IsPlayer)
                        return;
                    
                    // Disrupt movement
                    if (cr.AnimationData.LongMovement?.CombatAction != null)
                        cr.AnimationData.LongMovement.CombatAction.Disrupted = true;
                    cr.Actions.ResetToFull(); // Restore actions

                    // Play cinematic
                    await battle.Cinematics.PlayCutscene(async cinema =>
                    {
                        // Add a bit of pacing delay
                        await cinema.WaitABit();
                        
                        // Do custom stuff
                        await doCutscene.Invoke(battle, cinema, zone, cr);
                        
                        // Finish-up creature spawns
                        List<Creature> alreadySpawned = battle.AllCreatures.ToList();
                        cinema.SpawnAllSkippedCreatures();
                        if (alreadySpawned.Count != battle.AllCreatures.Count) // Only do this if new ones spawned
                        {
                            Sfxs.Play(SfxName.Hide);
                            foreach (Creature spawned in battle.AllCreatures
                                         .Except(alreadySpawned))
                                spawned.Overhead("*reveal*", Color.LightBlue);
                            await cinema.WaitABit(); // Add delays for the sake of overheads
                            await cinema.WaitABit();
                        }
                        
                        // Let lizards swim
                        foreach (Creature lizard in battle.AllCreatures
                                     .Where(lizard => lizard.CreatureId is CreatureId.GiantMonitorLizard))
                            lizard.AddQEffect(QEffect.Swimming());
                        
                        await cinema.WaitABit();
                        
                        Sfxs.Play(SfxName.StartOfTurn, 0.1f);
                        InitialSpawnComplete = true;
                    });
                    
                    // Remove other encounter triggers
                    foreach (QEffect qf in battle.Pseudocreature.QEffects
                                 .Where(qf => qf.Name is BeginEncounterId))
                        qf.ExpiresAt = ExpirationCondition.Immediately;
                };
            });
    }

    public static async Task LineAsync(Creature talker, string line, bool noCenter = false, bool angry = false)
    {
        Sfxs.Play(TalkNoise(talker, angry));
        await talker.Battle.Cinematics.LineAsync(talker, line, null, noCenter);
    }

    public static SfxName TalkNoise(Creature talker, bool? angry = false)
    {
        bool isFemale = talker.HasTrait(Trait.Female);
        return angry is true
            ? isFemale 
                ? SfxName.Intimidate
                : SfxName.MaleIntimidate
            : isFemale
                ? SfxName.HeroAlexHmmm
                : SfxName.SoldierHunterHmmm;
    }

    /// Waterfall death zone
    public Zone CreateWaterfallZone(TBattle battle)
    {
        return Zone.SpawnStaticAndApply(
            battle.Pseudocreature,
            RegionWaterfall,
            zone =>
            {
                const string edgeName = "Waterfall's Edge";
                zone.TileEffectCreator = tile => new TileQEffect(tile)
                {
                    //Illustration = IllustrationName.Hazard64,
                    StateCheck = tqf =>
                    {
                        tqf.TransformsTileIntoHazardousTerrain = true;
                    }
                };
                zone.AfterCreatureEntersOrMovesWithin = async cr =>
                {
                    if (cr.HasEffect(QEffectId.Flying)
                        || cr.HeldItems.Any(item =>
                            item.HasTrait(Trait.Grapplee)
                            && item.Grapplee!.Name == edgeName
                            && item.Grapplee!.HasTrait(Trait.NeverSetsOccupant)))
                        return;
                    
                    if (!await AskToGrabAnEdge(cr))
                        cr.Die();
                };
                zone.StateCheckOnEachCreatureInZone = (zThis, cr) =>
                {
                    if (cr.HasEffect(QEffectId.Flying) // Don't die when flying
                        || cr.HeldItems.Any(item =>
                            item.HasTrait(Trait.Grapplee)
                            && item.Grapplee!.Name == edgeName
                            && item.Grapplee!.HasTrait(Trait.NeverSetsOccupant)) // Don't die whole holding onto the edge
                        || cr.AnimationData.LongMovement is not null) // Don't die while you're in the process of moving
                        return;
                    
                    cr.Die();
                };

                async Task<bool> AskToGrabAnEdge(Creature cr)
                {
                    List<string> choices = [];
                    if (!cr.HasFreeHand)
                        choices.AddRange(cr.HeldItems.Select(item =>
                            item.Illustration.IllustrationAsIconString + item.Name));
                    else
                        choices.Add("{icon:Reaction} Take reaction");
                    if (await cr.Battle.AskToUseReaction(
                                cr,
                                "{b}Grab an Edge{/b} {icon:Reaction}\nYou're about to fall past the waterfall's edge. "
                                + (cr.HasFreeHand ? "Grab" : "Release {icon:FreeAction} an item, then grab")
                                + " on, potentially stopping your fatal fall?",
                                IllustrationName.CrashingWave2,
                                choices.ToArray())
                            is not {} choice // "Pass" choice is always last, but isn't included manually so is larger than the last index
                        || choice == choices.Count)
                    {
                        return false;
                    }
        
                    if (cr.AnimationData.LongMovement is { CombatAction: not null } move)
                        move.CombatAction.Disrupted = true;
        
                    if (!cr.HasFreeHand)
                    {
                        Sfxs.Play(SfxName.DropItem);
                        cr.DropItem(cr.HeldItems[choice]);
                    }

                    return await GrabAnEdge(cr, GrabEdgeDC, edgeName);
                }
            });
    }

    /// <summary>
    /// Attempt to grab an edge using acrobatics, athletics, or reflex. (Homebrew) On a critical success, you can Stride 5 feet, and you can Release as part of the reaction.
    /// </summary>
    /// <param name="grabber">The creature making the check and grabbing the edge.</param>
    /// <param name="DC">The DC to beat to grab the edge.</param>
    /// <param name="heldEdgeName">The name of the edge being held, used as a held item. This string is used as an ID.</param>
    /// <returns>Whether the edge was grabbed, even if you Stride afterward.</returns>
    public static async Task<bool> GrabAnEdge(Creature grabber, int DC, string heldEdgeName)
    {
        TaggedCalculatedNumberProducer best = TaggedChecks.BestRoll(
            TaggedChecks.SkillCheck(Skill.Athletics),
            TaggedChecks.SkillCheck(Skill.Acrobatics),
            Checks.SavingThrow(Defense.Reflex));

        CombatAction grabAnEdge = new CombatAction(
                grabber,
                IllustrationName.None,
                "Grab an Edge",
                [Trait.Manipulate],
                $$"""
                {i}When you fall off or past an edge or other handhold, you can try to grab it, potentially stopping your fall.{/i}

                {b}Trigger{/b} You fall from or past an edge or handhold.
                {b}Requirements{/b} You have a free hand

                You must succeed at your choice of an Athletics or Acrobatics check or a Reflex save, with a DC of {Blue}{{GrabEdgeDC}}{/Blue}.
                """
                + S.FourDegreesOfSuccess(
                    "You grab the edge and can Stride 5 feet.",
                    "You grab the edge.",
                    "You fall as normal.",
                    null),
                Target.Self())
            .WithActionCost(0)
            .WithSoundEffect(SfxName.Grapple)
            .WithEffectOnEachTarget(async (_, _, _, result) =>
            {
                switch (result)
                {
                    case CheckResult.CriticalSuccess:
                        grabber.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                        {
                            Id = QEffectId.IgnoresDifficultTerrain
                        });
                        if (!await grabber.StrideAsync(
                                "Choose where to Stride with a critical Grab an Edge.",
                                maximumFiveFeet: true,
                                allowPass: true))
                            goto case CheckResult.Success;
                        return /*true*/;
                    case CheckResult.Success:
                        Creature edge = Creature
                            .CreateSimpleCreature(heldEdgeName)
                            .WithExtraTrait(Trait.NeverSetsOccupant);
                        edge.Illustration = IllustrationName.CrashingWave2;
                        await CommonAbilityEffects.Grapple(grabber, edge);
                        grabber.AddQEffect(new QEffect() // Work-around for "Release" not working
                        {
                            Name = "[GRABBED EDGE RELEASE FIXER]",
                            Tag = edge,
                            StateCheck = qfThis =>
                            {
                                if (qfThis.Owner.HeldItems.All(item => item.Grapplee != (qfThis.Tag as Creature)!))
                                    qfThis.ExpiresAt = ExpirationCondition.Immediately;
                            },
                            AfterYouTakeAction = async (qfThis, action) =>
                            {
                                Item? grappledEdge =
                                    qfThis.Owner.HeldItems.FirstOrDefault(item =>
                                        item.Grapplee == (qfThis.Tag as Creature)!);
                                if (grappledEdge is not null && action.Name == $"Release {grappledEdge.Name}")
                                {
                                    qfThis.Owner.DropItem(grappledEdge);
                                    grappledEdge.Grapplee!.RemoveAllQEffects(qf =>
                                        qf.Id == QEffectId.Grappled && qf.Source == qfThis.Owner);
                                }
                            }
                        });
                        return /*true*/;
                    default:
                        return /*false*/;
                }
            });

        if (best.InvolvedSkill is not null)
            grabAnEdge.WithActiveRollSpecification(new ActiveRollSpecification(
                best,
                Checks.FlatDC(DC)));
        else
            grabAnEdge.WithSavingThrow(new SavingThrow(Defense.Reflex, DC));

        await grabber.Battle.GameLoop.FullCast(grabAnEdge);

        switch (grabAnEdge.CheckResult)
        {
            case CheckResult.CriticalSuccess:
            case CheckResult.Success:
                return true;
            default:
                return false;
        }
    }
    
    /// Flowing river zone
    public Zone CreateRiverZone(TBattle battle, int riverSpeed, int swimDC)
    {
        return Zone.SpawnStaticAndApply(
            battle.Pseudocreature,
            RegionRiver,
            zone =>
            {
                zone.AfterCreatureEndsItsTurnHere = async cr =>
                {
                    if (cr.HasEffect(QEffectId.Flying))
                        return;
                    if (cr.HasEffect(QEffectId.Immobilized))
                    {
                        cr.Overhead("*held in place*", Color.Black);
                        return;
                    }
                    await PushLeft(cr, riverSpeed, RegionWaterfall);
                };
                zone.StateCheckOnEachCreatureInZone = (zThis, cr) =>
                {
                    cr.AddQEffect(DifficultSwim(riverSpeed, swimDC).WithExpirationEphemeral());
                };
                // Block this entrance to the river so that NPCs will stand by or switch to ranged attacks
                zone.TileEffectCreator = tile =>
                {
                    // Only block the entrance
                    if (!RegionRiverEntrance.Contains(tile))
                        return null;
                    
                    return new TileQEffect(tile)
                    {
                        // Allow creatures to pass into or out of the river if no players are present
                        // This will allow the monitor lizards to follow out of the river.
                        StateCheck = tQf =>
                        {
                            tQf.TransformsTileIntoUnenterableTerrainForNonflyingEnemiesOnly =
                                RegionRiver.Any(tile2 =>
                                    tile2.PrimaryOccupant?.OwningFaction.IsPlayer is true);
                        },
                    };
                };
            });
    }

    /// Grabable log zone
    public Zone CreateLogHintZone(TBattle battle)
    {
        return Zone.SpawnStaticAndApply(
            battle.Pseudocreature,
            RegionLogHint,
            zone =>
            {
                QEffect zQf = zone.ControllerQEffect;
                
                // Dialog bubble
                zQf.Tag = false; // Display hint dialog only once
                zone.AfterCreatureEntersOrMovesWithin = async cr =>
                {
                    if (zQf.Tag is true
                        || !cr.OwningFaction.IsPlayer)
                        return;
                    await ShowQuickBubble(
                        battle, cr,
                        "That log is hanging just above the water! I bet I could hold onto it with a "
                            + (cr.HasFreeHand
                                ? "{Green}free hand{/Green}"
                                : "{Red}free hand{/Red}")
                            + "!");
                    zQf.Tag = true;
                };

                // Tree log
                zone.StateCheckOnEachCreatureInZone = (zone1, cr) =>
                {
                    if (!RegionLogGrabbable.Contains(cr.Occupies))
                        return;
                    cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                    {
                        Name = "[LOG GRAB GRANTER]",
                        ProvideContextualAction = qfThis =>
                        {
                            if (qfThis.Owner.HeldItems.Any(item =>
                                    item.HasTrait(Trait.Grapplee)
                                    && item.Grapplee!.Name == "Tree Log"
                                    && item.Grapplee!.HasTrait(Trait.NeverSetsOccupant)))
                                return null;
                            
                            CombatAction grabLog = new CombatAction(
                                    qfThis.Owner,
                                    IllustrationName.Tree3,
                                    "Grab onto log",
                                    [Trait.Basic],
                                    "{b}Requirements{/b} You have a free hand\n\nGrab onto the log with your hand. While holding onto the log, you will not flow down the river.",
                                    Target.Self()
                                        .WithAdditionalRestriction(self =>
                                        {
                                            if (!self.HasFreeHand)
                                                return Usability.CommonReasons.NoFreeHandForManeuver.UnusableReason;
                                            return null;
                                        }))
                                .WithActionCost(1)
                                .WithEffectOnSelf(async self =>
                                {
                                    Creature log = Creature
                                        .CreateSimpleCreature("Tree Log")
                                        .WithExtraTrait(Trait.NeverSetsOccupant);
                                    log.Illustration = IllustrationName.Tree1;
                                    self.AddHeldItem(Item.Grappling(log));
                                });
                            
                            return new ActionPossibility(grabLog);
                        },
                    });
                };
            });
    }

    /// Will attempt to move around obstacles and players, and will stop early if necessary
    public static async Task PushLeft(Creature pushee, int distance, List<Tile> waterfall)
    {
        // Don't push if you're holding onto the log
        if (pushee.HeldItems.Any(item =>
                item.HasTrait(Trait.Grapplee)
                && item.Grapplee!.Name == "Tree Log"
                && item.Grapplee!.HasTrait(Trait.NeverSetsOccupant)))
        {
            pushee.Overhead("*holding on*", Color.Black);
            return;
        }
        
        // Instead of pushing, go from the right side to the left side of the bridge
        if (pushee.Occupies.X == 28)
        {
            pushee.Overhead("*no layered maps*", Color.Black);
            await CommonSpellEffects.Teleport(
                pushee,
                pushee.Battle.Map.GetTile(23, pushee.Occupies.Y)!);
            pushee.Overhead("*no layered maps*", Color.Black);
            return;
        }

        List<Tile> leftTiles = pushee.Battle.Map.AllTiles
            .Where(tile =>
                    tile.X > 0 // Fall zone is 2 tiles wide, don't push into the left-most column
                    && tile.X <= pushee.Occupies.X // Must be to the left of me
                    && pushee.DistanceTo(tile) <= distance // Cannot be too far away
                    && tile.IsTrulyGenuinelyFreeTo(pushee) // Must be free to me
                    //&& !(tile.X <= 2 && Math.Abs(tile.Y - pushee.Occupies.Y) > 0) // Don't slide me up/down just because I reached the end
                )
            // Prefer the tile with the lowest vertical deviation,
            // but the greatest distance.
            .OrderBy(tile =>
                Math.Abs(tile.Y - pushee.Occupies.Y)
                + (distance - tile.DistanceTo(pushee)))
            .ToList();

        // Choose a tile that is the closest to any death zone tile.
        Tile? chosenTile = leftTiles.MinBy(tile => waterfall.Select(fall => fall.DistanceTo(tile)).Min());

        if (chosenTile != null)
        {
            // Save every grapplee that we have before getting pushed down the river.
            List<(Creature grapplee, bool isRestrained)> grapplees = pushee.HeldItems
                .Where(item => item.Grapplee is not null)
                .Select(item =>
                {
                    Creature grapplee = item.Grapplee!;
                    // Is restrained if it had the condition and contained our name in the description.
                    bool isRestrained = grapplee.QEffects.Any(qf =>
                        qf.Id == QEffectId.Restrained
                        && (qf.Description?.Contains(pushee.ToString()) ?? false));
                    return (grapplee, isRestrained);
                })
                .ToList();
            
            // Push me
            await pushee.MoveTo(
                chosenTile,
                null,
                new MovementStyle()
                {
                    Shifting = true,
                    ShortestPath = true,
                    ForcedMovement = true,
                    MaximumSquares = 100
                });
            
            // Push and regrapple every creature we had before
            foreach ((Creature grapplee, bool isRestrained) target in grapplees)
            {
                await PushLeft(target.grapplee, distance, waterfall);
                await ReGrapple(pushee, target.grapplee, target.isRestrained);
            }
            
            // Original push logic, before adding grapplee-dragging.
            /*await pushee.MoveTo(
                chosenTile,
                null,
                new MovementStyle()
                {
                    Shifting = true,
                    ShortestPath = true,
                    ForcedMovement = true,
                    MaximumSquares = 100
                });*/

            // Botched attempt to move targets as a group.
            // Attempt discarded due to unresolvable issues around zone async events not firing.
            /*var grapplees = pushee.HeldItems
                .Where(item => item.Grapplee is not null)
                .ToList();*/
            
            /*await new GroupMovement([
                    MovementOrder.Create(pushee, chosenTile),
                    ..grapplees.Select(item =>
                    {
                        int xDiff = pushee.Space.TopLeftTile.X - item.Grapplee!.Space.TopLeftTile.X;
                        int yDiff = pushee.Space.TopLeftTile.Y - item.Grapplee!.Space.TopLeftTile.Y;
                        return MovementOrder.Create(pushee.Battle, item.Grapplee!, chosenTile.X - xDiff, chosenTile.Y - yDiff);
                    })
                ])
                .Execute();*/
            /*await DoGroupMove(new GroupMovement([
                    MovementOrder.Create(pushee, chosenTile),
                    ..grapplees.Select(item =>
                    {
                        int xDiff = pushee.Space.TopLeftTile.X - item.Grapplee!.Space.TopLeftTile.X;
                        int yDiff = pushee.Space.TopLeftTile.Y - item.Grapplee!.Space.TopLeftTile.Y;
                        return MovementOrder.Create(pushee.Battle, item.Grapplee!, chosenTile.X - xDiff, chosenTile.Y - yDiff);
                    })
                ]));*/
        }

        return;

        // Same as CommonAbilityEffects.Grapple, with fewer interactions like Steam achievements and Crushing Grab.
        async Task ReGrapple(Creature grappler, Creature target, bool restrainInsteadOfGrab = false)
        {
            if (target.HP <= 0)
                return;

            if (!grappler.HeldItems.Any(hi => hi.HasTrait(Trait.Grapplee) && hi.Grapplee == target))
                grappler.HeldItems.Add(Item.Grappling(target));
    
            target.AddQEffect(new QEffect(
                "Grappled",
                "not shown",
                ExpirationCondition.ExpiresAtEndOfSourcesTurn,
                grappler,
                IllustrationName.None)
            {
                Id = QEffectId.Grappled,
                CountsAsBeneficialToSource = true,
                CannotExpireThisTurn = true,
                Tag = new GrappleTag()
                {
                    Reach = GrappleTag.GetGrappleReach(grappler)
                },
                WhenExpires = qfGrappled =>
                    qfGrappled.Source!.HeldItems.RemoveAll(hi => hi.Grapplee == qfGrappled.Owner),
                WhenMonsterDies = qfGrappled =>
                    qfGrappled.Source!.HeldItems.RemoveAll(hi => hi.Grapplee == qfGrappled.Owner),
                StateCheck = qfGrappled =>
                {
                    if (!qfGrappled.Source!.Actions.CanTakeActions()
                        || !((GrappleTag)qfGrappled.Tag!).WithinReach(qfGrappled))
                    {
                        qfGrappled.ExpiresAt = ExpirationCondition.Immediately;
                        qfGrappled.Source.HeldItems.RemoveAll(hi => hi.Grapplee == qfGrappled.Owner);
                    }
                    else if (restrainInsteadOfGrab)
                        qfGrappled.Owner.AddQEffect(QEffect.Restrained(grappler));
                    else
                        qfGrappled.Owner.AddQEffect(QEffect.Grabbed(grappler));
                }
            });
        }

        /*async Task DoGroupMove(GroupMovement group)
        {
            if (group.MovementOrders.Length == 0)
                return;

            TBattle battle = group.MovementOrders[0].Mover.Battle;
            bool hasPath = false;
            foreach (MovementOrder movementOrder in group.MovementOrders)
            {
                IList<Tile>? path = Pathfinding.GetPath(
                    movementOrder.Mover,
                    movementOrder.Target,
                    movementOrder.Mover.Battle,
                    new PathfindingDescription()
                    {
                        Squares = 1000,
                        Style = new MovementStyle()
                        {
                            Shifting = true,
                            ForcedMovement = true
                        }
                    });
                movementOrder.Path = path;
                if (path != null)
                    hasPath = true;
            }

            if (!hasPath)
                group.MovementOrders[0].Mover.Overhead(
                    "no path",
                    Color.Red,
                    "No free path exists for your party to move to the destination.");
            while (group.MovementOrders.Any(order => order.Incomplete))
            {
                MovementOrder[] array = group.MovementOrders
                    .Where(mo => mo.Incomplete)
                    .ToArray();
                List<MovementOrder> movingThisIteration = [];
                List<MovementOrder> list = array.ToList();
                bool flag2 = false;
                bool flag3;
                do
                {
                    flag3 = false;
                    foreach (MovementOrder movementOrder in list.ToList())
                    {
                        Tile? nextTile = movementOrder.NextTile;
                        if (nextTile != null && (nextTile.PrimaryOccupant == null || nextTile.AdditionalOccupant == null))
                        {
                            battle.SmartCenterCreatureIfNotVisible(movementOrder.Mover);
                            battle.SmartCenterTileIfNotVisible(nextTile);
                            movingThisIteration.Add(movementOrder);
                            Vector2 actualPosition = movementOrder.Mover.AnimationData.ActualPosition;
                            movementOrder.Mover.TranslateTo(nextTile);
                            movementOrder.Mover.AnimationData.ActualPosition = actualPosition;
                            movementOrder.Mover.AnimationData.ShortMovement = new ShortMovement(movementOrder.Mover, nextTile);
                            list.Remove(movementOrder);
                            flag3 = true;
                            flag2 = true;
                        }
                    }
                } while (flag3);

                if (flag2)
                {
                    if (!battle.Cinematics.SkipThroughMovement)
                    {
                        if (battle.Cinematics is not { Cutscene: true }
                            || !battle.Cinematics.Skipping)
                        {
                            RequestResult requestResult =
                                await battle.SendRequest(new WaitForMovementToCompleteRequest());
                            bool flag4;
                            switch (requestResult.ChosenOption)
                            {
                                case NextLineOption:
                                case SkipCutsceneOption:
                                    flag4 = true;
                                    break;
                                default:
                                    flag4 = false;
                                    break;
                            }

                            if (flag4)
                            {
                                battle.Cinematics.SkipThroughMovement = true;
                                if (battle.Cinematics.Cutscene && requestResult.ChosenOption is SkipCutsceneOption)
                                    battle.Cinematics.Skipping = true;
                            }
                        }
                    }

                    CombatAction? groupMovement =
                        !battle.CombatActionsInProgressStack.TryPeek(out CombatAction? result) ? null : result;
                    foreach (MovementOrder movementOrder in movingThisIteration)
                    {
                        if (movementOrder.Mover.Space.TopLeftTile == movementOrder.NextTile)
                        {
                            movementOrder.Mover.AnimationData.ShortMovement = null;
                            Tile nextTile = movementOrder.NextTile;
                            movementOrder.Mover.AnimationData.ActualPosition = new Vector2(nextTile.X, nextTile.Y);
                            ++movementOrder.PathIndex;
                            if (movementOrder.Mover.Space.PreviousTile != null
                                && groupMovement != null
                                && DecisionPoints.ReplayingAtLeast(6))
                                await movementOrder.Mover.AfterSingleTileMove(
                                    nextTile,
                                    movementOrder.Mover.Space.PreviousTile,
                                    movementOrder.Mover.Space.PreviousTile.TilesToTheBottomRight(
                                            movementOrder.Mover.Space.SizeInSquares)
                                        .ToList(),
                                    groupMovement, new MovementStyle()
                                    {
                                        Shifting = true
                                    });
                        }
                    }

                    if (groupMovement is { Disrupted: true })
                        break;
                }
                else
                    break;
            }

            MovementOrder[] movementOrderArray = group.MovementOrders;
            foreach (MovementOrder movementOrder in movementOrderArray)
            {
                if (movementOrder.Mover.Space.AnyTile(tl => tl.AdditionalOccupant == movementOrder.Mover))
                {
                    if (movementOrder.Mover.Space.Tiles.All(tl => tl.PrimaryOccupant == null))
                        movementOrder.Mover.TranslateTo(movementOrder.Mover.Space.TopLeftTile);
                    else
                    {
                        Tile shuntOffTile = movementOrder.Mover.Space.TopLeftTile.GetShuntoffTile(movementOrder.Mover);
                        //await movementOrder.Mover.SingleTileMove(shuntOffTile, null);
                        await movementOrder.Mover.MoveTo(shuntOffTile, null, new MovementStyle()
                        {
                            Shifting = true,
                            ShortestPath = true,
                            ForcedMovement = true,
                            MaximumSquares = 100
                        });
                    }
                }
            }

            battle.Cinematics.SkipThroughMovement = false;
        }*/
    }

    /// Handles the logic for swim checks
    public static QEffect DifficultSwim(int pushDistance, int DC)
    {
        return new QEffect()
        {
            Illustration = IllustrationName.ElementWater,
            Name = DifficultSwimEffectName,
            Description = "You are swimming in a flowing river.\n• At the end of your turn, you are pushed {Blue}" + pushDistance * 5 + "{/Blue} feet towards the waterfall.\n• Any time you attempt to move while swimming, you must make a DC " + DC + " Athletics check to determine the distance you travel.",
            // Remove immediately if you can fly
            StateCheck = qfThis =>
            {
                if (qfThis.Owner.HasEffect(QEffectId.Flying))
                    qfThis.ExpiresAt = ExpirationCondition.Immediately;
            },
            // Apply movement penalties when you begin to move
            YouBeginAction = async (qfThis, action) =>
            {
                if (!action.HasTrait(Trait.Move)
                    || action.ChosenTargets.ChosenTile == null)
                    return;

                qfThis.Owner.AddQEffect(new QEffect()
                {
                    Name = "[SWIM SPEED MODULATION]",
                    Tag = qfThis.Owner.Occupies,
                    AdjustActiveRollCheckResult = (qfThis2, action2, _, result) =>
                    {
                        if (action2.Name != "Swim against the river"
                            || !qfThis2.Owner.HasEffect(QEffectId.Swimming))
                            return result;
                        return result.ImproveByOneStep();
                    },
                    AfterYouTakeAction = async (qfThis2, action2) =>
                    {
                        // Remove after finishing the move
                        if (action == action2)
                            qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                    },
                    // Disrupt early, as if difficult terrain
                    // This is run on your movement if the Swim check was not a crit
                    StateCheck = qfThis2 =>
                    {
                        if (qfThis2.Owner.AnimationData.LongMovement is not { CombatAction: {} action2 }
                            || qfThis2.Tag is not Tile start)
                            return;
                        // Don't try to half the movement twice, if you're already impeded by not swimming
                        if (!qfThis2.Owner.HasEffect(QEffectId.Swimming)
                            && !qfThis2.Owner.HasEffect(QEffectId.CountsAllTerrainAsDifficultTerrain)
                            && !qfThis2.Owner.HasEffect(QEffectId.IgnoresDifficultTerrain))
                            return;
                        if (start.DistanceTo(qfThis.Owner.Occupies) >= qfThis2.Owner.Speed / 2)
                            action2.Disrupted = true;
                    },
                });
                
                qfThis.Owner.RecalculateLandSpeedAndInitiative();
            },
            // Make a check to move
            FizzleOutgoingActions = async (qfThis, action, strBuilder) =>
            {
                if (!action.HasTrait(Trait.Move)
                    || action.ChosenTargets.ChosenTile == null)
                    return false;

                CombatAction swim = new CombatAction(
                        qfThis.Owner,
                        IllustrationName.None,
                        "Swim against the river",
                        [],
                        "",
                        Target.Self())
                    .WithDescription(
                        "Ocean currents, flowing rivers, and similar moving water impede movement.",
                        "When attempting to move through this kind of terrain, you must make a DC {Blue}"+DC+"{/Blue} Athletics check (" + S.SkillBonus(qfThis.Owner, Skill.Athletics) + "). If you have a swim speed, you get a result that is one degree of success better."
                            + S.FourDegreesOfSuccess(
                                "You move normally.",
                                "You move, treating the water as difficult terrain if it wasn't already.",
                                "You don't move.", null))
                    .WithActionCost(0)
                    .WithActiveRollSpecification(new ActiveRollSpecification(
                        TaggedChecks.SkillCheck(Skill.Athletics),
                        Checks.FlatDC(DC)));
                
                CheckBreakdown breakdown = CombatActionExecution.BreakdownAttack(swim, qfThis.Owner);
                CheckBreakdownResult breakdownResult = new CheckBreakdownResult(breakdown);
                
                // Remove movement penalties if the Swim check was a crit
                if (breakdownResult.CheckResult == CheckResult.CriticalSuccess
                    && qfThis.Owner.QEffects.FirstOrDefault(qf =>
                        qf.Name == "[SWIM SPEED MODULATION]") is {} riverSpeed)
                    riverSpeed.ExpiresAt = ExpirationCondition.Immediately;

                bool isFailure = breakdownResult.CheckResult <= CheckResult.Failure;
                if (isFailure)
                {
                    qfThis.Owner.Overhead(
                        breakdownResult.CheckResult.HumanizeTitleCase2(),
                        Color.WhiteSmoke);
                    strBuilder
                        .AppendLine("{b}Swim against the river:{/b}")
                        .AppendLine(swim.Description)
                        .AppendLine()
                        .AppendLine(breakdown.DescribeWithFinalRollTotal(breakdownResult))
                        .AppendLine();
                }
                else
                    qfThis.Owner.Overhead(
                        breakdownResult.CheckResult.HumanizeTitleCase2(),
                        Color.WhiteSmoke,
                        swim.Owner + " rolls " + breakdownResult.CheckResult.Greenify() + " on " + swim.Name + ".",
                        swim.Name,
                        swim.Description + "\n\n" + breakdown.DescribeWithFinalRollTotal(breakdownResult) + "\n\n");

                return isFailure;
            },
        };
    }

    /// Functions as <see cref="Cinematics.ShowQuickBubble"/> but with a timed duration parameter and no voice line.
    public static async Task ShowQuickBubble(TBattle battle, Creature speaker, string text, int milliseconds = 5000)
    {
        battle.Cinematics.TutorialBubble = new TutorialBubble(
            speaker.Illustration,
            SubtitleModification.Replace(text),
            null);
        battle.Log("{b}"+speaker.Name+":{/b} "+text);
        await battle.SendRequest(new SleepRequest(milliseconds)
        {
            CanBeClickedThrough = true
        });
        battle.Cinematics.TutorialBubble = null;
    }

    public Func<AI, List<Option>, Option?>? BraceDecision()
    {
        if (!ModManager.TryParse("Ready", out ActionId ready))
            return null;
        return (ai, options) =>
        {
            // Don't Brace if I'm in the water
            if (IsInWater(ai.Self))
                return null;
            // Don't Brace if I'm not near the entrance
            if (!RegionRiverEntrance.Any(tile =>
                    ai.Self.IsAdjacentTo(tile)))
                return null;
            // Don't Brace more than once this turn
            if (ai.Self.Actions.ActionHistoryThisTurn.Any(act =>
                    act.ActionId == ready))
                return null;
            
            List<Creature> enemies = ai.Self.Battle.AllCreatures
                .Where(ai.Self.EnemyOf)
                .ToList();
            // Don't Brace if there aren't any enemies in the water
            if (!enemies.Any(IsInWater))
                return null;
            // Don't Brace if there are any enemies in my reach
            if (enemies.Any(enemy =>
                    ai.Self.DistanceToWith10FeetException(enemy) <= ai.Self.Space.ActualReach))
                return null;
            
            // Brace
            Option? brace = options.FirstOrDefault(opt =>
                opt is CombatActionOption { CombatAction: { } act }
                && act.ActionId == ready
                && act.Name.ToLower().Contains("brace"));
            return brace;
        };
        
        bool IsInWater(Creature cr)
        {
            return cr.QEffects.Any(qf => qf.Name == DifficultSwimEffectName);
        }
    }

    public static async Task MoveGroup(Cinematics cinema, Dictionary<Creature, (int x, int y)> heroes)
    {
        if (heroes.Count == 0)
            throw new Exception("Size of 'Dictionary<Creature, (int x, int y)> heroes' for MoveGroup is 0");
        
        CombatAction moveWrapper = new CombatAction(
                heroes.First().Key,
                IllustrationName.None,
                "[GROUP MOVE]",
                [Trait.DoNotShowInCombatLog, Trait.DoNotShowOverheadOfActionName],
                "",
                Target.Self())
            .WithEffectOnSelf(async cr =>
            {
                Sfxs.Play(SfxName.Shove);
                await new GroupMovement(heroes
                        .Select(hero =>
                            MovementOrder.Create(hero.Key.Battle, hero.Key, hero.Value.x, hero.Value.y))
                        .ToArray())
                    .Execute();
            });
        
        // It doesn't matter who executes it, it just needs to be executed.
        await heroes.First().Key.Battle.GameLoop.FullCast(moveWrapper);
    }

    // Creature (caster), Creature (chest)
    public static Creature WithOpen(
        Creature chest,
        Func<Creature, Creature, Task> doWhat,
        Func<Creature,Creature,Usability>? addedReq = null,
        string actionName = "Open")
    {
        if (chest.CreatureId != CreatureId.Chest)
            return chest;
        return chest.AddQEffect(new QEffect(
                "Openable",
                "An adjacent creature can open this chest.")
            .AddAllowActionOnSelf(
                QEffectId.OpenAChest,
                QEffectId.Chest,
                cr =>
                    new ActionPossibility(
                        new CombatAction(
                                cr,
                                IllustrationName.Chest256,
                                actionName,
                                [Trait.Manipulate, Trait.Basic, Trait.BypassesOutOfCombat],
                                "Open a chest and collect its contents.",
                                Target.Touch())
                            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                            {
                                target.RemoveAllQEffects(qf => qf.Id == QEffectId.Chest);
                                target.Illustration = IllustrationName.ChestOpen;
                                
                                Sfxs.Play(SfxName.OpenChest);
                                //Sfxs.Play(SfxName.ItemGet);

                                await doWhat.Invoke(caster, target);
                            }))
                    .WithPossibilityGroup("Interactions"),
                addedReq));
    }
}