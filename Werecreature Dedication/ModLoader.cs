using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Modding;
using Dawnsbury.Mods.Template;

namespace Dawnsbury.Mods.WerecreatureDedication;
public static class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // Tooltips
        //ModManager.RegisterInlineTooltip("MoreDedications.LevelBasedDC",
            //"{b}Level-based DCs{/b}\nWhen a DC is based on your level, it uses one of the following values:\n{b}Level 1:{/b} 15\n{b}Level 2:{/b} 16\n{b}Level 3:{/b} 18\n{b}Level 4:{/b} 19\n{b}Level 5:{/b} 20\n{b}Level 6:{/b} 22\n{b}Level 7:{/b} 23\n{b}Level 8:{/b} 24");
        
        LoadDedication();
        LoadArchetypeFeats();
    }

    public static void LoadDedication()
    {
        // PETR: Doesn't grant the beast trait, and without a basic werecreature trait, nothing actually interacts with this.
        Feat dedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.Werecreature,
                "While many werecreatures have little control over their own transformations, formal training or an exceptionally strong will has enabled you to exert a degree of mastery over the beast that rages within you.",
                "You're a werecreature, able to shift between your humanoid shape, an animal shape, and a monstrous hybrid of the two. You gain the werecreature trait. Choose your werecreature type. You gain the Toughness feat. You gain the Change Shape action.",
                /*"You're a werecreature, able to shift between your humanoid shape, an animal shape, and a monstrous hybrid of the two. You gain the beast and werecreature traits. Choose your werecreature type. You gain the Toughness feat but also a weakness to silver equal to half your level. You gain the Change Shape action. On the night of the full moon, you automatically use Change Shape to assume your hybrid shape, and you can't voluntarily activate or dismiss Change Shape until sunrise.\n\n{b}Special{/b} If you're a beastkin, you can use unarmed attacks from your hybrid shape while you're in your werecreature hybrid shape. These forms are otherwise separate."*/
                [..LoadWerecreatureTypes()])
            .WithRulesBlockForCombatAction(cr => ChangeShapeAction.ChangeShapeMenuAction(cr, null))
            .WithOnSheet(values =>
            {
                // Failed attempts to add the beast trait to the character.
                //values.Ancestries.Add(Trait.Beast);
                
                values.GrantFeat(FeatName.Toughness);
            })
            .WithOnCreature((values, cr) =>
            {
                // PETR: Silver material.
                //cr.AddQEffect(QEffect.DamageWeakness(Trait.Silver, cr.Level/2));
                
                // Failed attempts to add the beast trait to the character.
                //values.AdditionalClassTraits.Add(Trait.Beast);
                //cr.Traits.Add(Trait.Beast);
            });
        dedication.Traits.Insert(0, ModData.Traits.Rare);
        ModManager.AddFeat(dedication);
    }

    public static IEnumerable<Feat> LoadWerecreatureTypes()
    {
        // Werebat: 10 feet, fly 15 feet; Fangs; 1d8 piercing; --; When Flying due to the fly speed granted by this form, you must begin and end your movement on a solid surface or immediately fall
        Feat werebat = CreateWereTypeFeat(
            ModData.FeatNames.Werebat,
            "Werebat",
            (15/5),
            _ => {},
            () => new Item(IllustrationName.DragonClaws, "Fangs", [Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)),
            null,
            false,
            true);
        ModManager.AddFeat(werebat);
        yield return werebat;
        
        // Werebear: 25 feet; Jaws/Claw; 1d8 piercing/1d6 slashing; --/Agile; --
        Feat werebear = CreateWereTypeFeat(
            ModData.FeatNames.Werebear,
            "Werebear",
            (25/5),
            _ => {},
            () => new Item(IllustrationName.Jaws, "Jaws", [Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)),
            () => new Item(IllustrationName.DragonClaws, "Claw", [Trait.Agile, Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing)));
        ModManager.AddFeat(werebear);
        yield return werebear;
        
        // Wereboar: 30 feet; Tusk; 1d8 slashing; Sweep; --
        Feat wereboar = CreateWereTypeFeat(
            ModData.FeatNames.Wereboar,
            "Wereboar",
            (30/5),
            _ => {},
            () => new Item(IllustrationName.Jaws, "Tusk", [Trait.Brawling, Trait.Sweep, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)));
        ModManager.AddFeat(wereboar);
        yield return wereboar;
        
        // Werecrocodile: 25 feet, swim 15 feet; Jaws; 1d8 piercing; Grapple; You can hold your breath for 2 hours in animal or hybrid shape
        Feat werecrocodile = CreateWereTypeFeat(
            ModData.FeatNames.Werecrocodile,
            "Werecrocodile",
            (25/5),
            qfFeat =>
                {
                    qfFeat.BonusToSkillChecks = (skill, action, target) =>
                        action.ActionId is ActionId.Grapple && skill is Skill.Athletics && qfFeat.Owner.FindQEffect(ModData.QEffectIds.WereShape) is { Key: "WerecreatureAnimalShape" }
                            ? new Bonus(1, BonusType.Circumstance, "werecrocodile")
                            : null;
                },
            () => new Item(IllustrationName.Jaws, "Jaws", [Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)),
            null,
            true,
            false,
            "You have a +1 circumstance bonus to Athletics checks made to Grapple while in animal shape.");
        ModManager.AddFeat(werecrocodile);
        yield return werecrocodile;
        
        // Weremoose: 25 feet; Antler; 1d8 piercing; Shove; --
        Feat weremoose = CreateWereTypeFeat(
            ModData.FeatNames.Weremoose,
            "Weremoose",
            (25/5),
            _ => {},
            () => new Item(IllustrationName.Jaws, "Antler", [Trait.Brawling, Trait.Shove, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)));
        ModManager.AddFeat(weremoose);
        yield return weremoose;
        
        // Wererat: 25 feet; Jaws/Claw; 1d6 piercing/1d4 slashing; Finesse/Agile, Finesse; Your animal shape is Small in size.
        Feat wererat = CreateWereTypeFeat(
            ModData.FeatNames.Wererat,
            "Wererat",
            (25/5),
            _ => {},
            () => new Item(IllustrationName.Jaws, "Jaws", [Trait.Brawling, Trait.Finesse, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing)),
            () => new Item(IllustrationName.DragonClaws, "Claw", [Trait.Agile, Trait.Brawling, Trait.Finesse, Trait.Unarmed, Trait.Polymorph])
                .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Slashing)));
        ModManager.AddFeat(wererat);
        yield return wererat;
        
        // Wereshark: 15 feet, swim 25 feet; Jaws; 1d8 piercing; Grapple; Your hybrid shape gains the amphibious trait. In your animal shape, you lose your land Speed and your Swim speed increases to 35 feet, and you gain the aquatic trait.
        Feat wereshark = CreateWereTypeFeat(
            ModData.FeatNames.Wereshark,
            "Wereshark",
            (25/5),
            qfFeat =>
            {
                qfFeat.BonusToAllSpeeds = qfThis =>
                {
                    if (qfFeat.Owner.FindQEffect(ModData.QEffectIds.WereShape) is { Key: "WerecreatureAnimalShape" } &&
                        (qfThis.Owner.HasEffect(QEffectId.AquaticCombat) ||
                         qfThis.Owner.Occupies.Kind is TileKind.ShallowWater or TileKind.Water))
                        return new Bonus(2, BonusType.Untyped, "wereshark");
                    return null;
                };
            },
            () => new Item(IllustrationName.Jaws, "Jaws", [Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)),
            null,
            true,
            false,
            "While swimming in your animal shape, your speed increases to 35 feet.");
        ModManager.AddFeat(wereshark);
        yield return wereshark;
        
        // Weretiger: 25 feet; Jaws/Claw; 1d8 piercing/1d6 slashing; --/Agile; --
        Feat weretiger = CreateWereTypeFeat(
            ModData.FeatNames.Weretiger,
            "Weretiger",
            (25/5),
            _ => {},
            () => new Item(IllustrationName.Jaws, "Jaws", [Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)),
            () => new Item(IllustrationName.DragonClaws, "Claw", [Trait.Agile, Trait.Brawling, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Slashing)));
        ModManager.AddFeat(weretiger);
        yield return weretiger;
        
        // Werewolf: 30 feet; Jaws; 1d8 piercing; Trip; --
        Feat werewolf = CreateWereTypeFeat(
            ModData.FeatNames.Werewolf,
            "Werewolf",
            (30/5),
            _ => {},
            () => new Item(IllustrationName.Jaws, "Jaws", [Trait.Brawling, Trait.Trip, Trait.Unarmed, Trait.Polymorph, Trait.Magical])
                .WithWeaponProperties(new WeaponProperties("1d8", DamageKind.Piercing)));
        ModManager.AddFeat(werewolf);
        yield return werewolf;
    }
    
    public static Feat CreateWereTypeFeat(
            FeatName featName,
            string wereName, // "Werebat", "Werewolf"
            int speed,
            Action<QEffect> adjustFeat,
            Func<Item> firstAttack,
            Func<Item>? secondAttack = null,
            bool swimming = false,
            bool flying = false,
            string? special = null) // Should end in a period.
        {
            Item attack1 = firstAttack.Invoke();
            Item? attack2 = secondAttack?.Invoke() ?? null;
            string shapeBenefits =
                $"{{b}}Speed{{/b}} {speed * 5} feet{(flying ? ", flying" : swimming ? ", swimming" : null)}."
                + "\n{b}Unarmed attacks{/b} You gain " + (attack2 != null ? "two" : "one") + " attack" +
                (attack2 != null ? "s" : null) + ":"
                + DescribeAttack(attack1)
                + (attack2 != null ? DescribeAttack(attack2) : null)
                + (special != null ? "\n{b}Special{/b} " + special : null);
            return new Feat(
                    featName,
                    null,
                    $"You are a {wereName.ToLower()}. While in your animal or hybrid shape, you gain the following benefits:\n\n{shapeBenefits}",
                    [ModData.Traits.WerecreatureType],
                    [])
                .WithIllustration(attack1.Illustration)
                .WithTag(featName)
                .WithOnSheet(values =>
                {
                    values.Tags.Add("WerecreatureType", featName);
                })
                .WithEquivalent(values => // Extra prevention against multiple werecreature types, and the errors that could induce.
                    values.AllFeatNames.Any(ftName => ftName.ToStringOrTechnical().Contains("WerecreatureType")))
                .WithPermanentQEffect($"You can Change Shape {{icon:Action}} into your {wereName.ToLower()} hybrid or animal shape (can be Dismissed).", qfFeat =>
                {
                    qfFeat.Name = $"Werecreature ({wereName})";
                    qfFeat.ProvideMainAction = qfThis =>
                        ChangeShapeAction.WereShapeMenu(qfThis.Owner, shapeBenefits);
                    qfFeat.StateCheck += qfThis =>
                    {
                        if (!qfThis.Owner.HasEffect(ModData.QEffectIds.WereShape))
                            return;
                        
                        qfThis.Owner.AddQEffect(new QEffect()
                        {
                            ExpiresAt = ExpirationCondition.Ephemeral,
                            AdditionalUnarmedStrike = attack1
                        });
                        if (secondAttack != null)
                            qfThis.Owner.AddQEffect(new QEffect()
                            {
                                ExpiresAt = ExpirationCondition.Ephemeral,
                                AdditionalUnarmedStrike = attack2
                            });

                        if (flying)
                            qfThis.Owner.AddQEffect(QEffect.Flying().WithExpirationEphemeral());
                        if (swimming)
                            qfThis.Owner.AddQEffect(QEffect.Swimming().WithExpirationEphemeral());
                    };
                    qfFeat.BonusToAllSpeeds = qfThis =>
                    {
                        if (!qfThis.Owner.HasEffect(ModData.QEffectIds.WereShape))
                            return null;
                        int trueBaseSpeed = qfThis.Owner.PersistentCharacterSheet?.Ancestry?.Speed ?? (25/5);
                        int newBaseSpeed = speed - trueBaseSpeed;
                        return new Bonus(newBaseSpeed, BonusType.Untyped, "werecreature");
                    };
                    
                    adjustFeat(qfFeat);
                });
            
            string DescribeAttack(Item attack)
            {
                string icon = attack.Illustration.IllustrationAsIconString;
                string attackName = attack.Name.ToLower();
                string attackDamage = attack.WeaponProperties!.Damage;
                string attackKind = attack.WeaponProperties!.DamageKind.HumanizeLowerCase2();
                var processedTraits = attack.Traits.ToList();
                processedTraits.Remove(Trait.Polymorph);
                processedTraits.Remove(Trait.Weapon);
                processedTraits.Remove(Trait.Melee);
                string attackTraits = string.Join(", ", processedTraits).ToLower();
                //attackTraits = attackTraits.Remove(attackTraits.Length - 1); // Remove trailing comma
                return $"\n{icon} {attackName}: {attackDamage} {attackKind}, {attackTraits}.";
            }
        }

    public static void LoadArchetypeFeats()
    {
        Feat animalFleetness = new TrueFeat(
                ModData.FeatNames.AnimalFleetness,
                4,
                "You're adept at using your animal shape's natural means of locomotion.",
                "While you're in animal shape, the Speeds granted by that shape increases by 10 feet.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.Werecreature)
            .WithPermanentQEffect(null, qfFeat =>
            {
                qfFeat.BonusToAllSpeeds = qfThis =>
                {
                    if (qfThis.Owner.FindQEffect(ModData.QEffectIds.WereShape) is { Key: "WerecreatureAnimalShape" })
                        return new Bonus(2, BonusType.Untyped, "animal fleetness");
                    return null;
                };
            });
        ModManager.AddFeat(animalFleetness);
        
        // beastkin resilience // lmao no
        
        // feral senses // hmm maybe
        
        // antler rush
        Feat antlerRush = new TrueFeat(
                ModData.FeatNames.AntlerRush,
                6,
                null,
                "{b}Requirements{/b} You're in your weremoose animal or hybrid shape.\n\nStride twice. If you end your movement within your antlers' reach of an enemy, you can Disarm, Shove, or Strike with your antlers.",
                [Trait.Flourish])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Werecreature)
            .WithPrerequisite(ModData.FeatNames.Weremoose, "Weremoose")
            .WithPermanentQEffect("Stride twice. Then make a Disarm, Shove, or Strike with your antlers.", qfFeat =>
            {
                qfFeat.ProvideMainAction = qfThis =>
                {
                    CombatAction antlerRush = new CombatAction(
                            qfThis.Owner,
                            IllustrationName.FleetStep,
                            "Antler Rush",
                            [Trait.Archetype, Trait.Flourish, Trait.Basic],
                            "{b}Requirements{/b} You're in your weremoose animal or hybrid shape.\n\nStride twice. If you end your movement within your antlers' reach of an enemy, you can Disarm, Shove, or Strike with your antlers.",
                            Target.Self()
                                .WithAdditionalRestriction(self =>
                                    self.HasEffect(ModData.QEffectIds.WereShape)
                                        ? null
                                        : "No animal or hybrid shape"))
                        .WithActionCost(2)
                        .WithEffectOnSelf(async (action, self) =>
                        {
                            if (!await self.StrideAsync(
                                    "Choose where to Stride with Antler Rush. (1/2)",
                                    allowCancel: true))
                                action.RevertRequested = true;
                            else if (!await self.StrideAsync(
                                         "Choose where to Stride with Antler Rush. You should end your movement within melee reach of an enemy. (2/2)",
                                         allowPass: true /*, passText: "Abort and convert to simple Stride"*/))
                            {
                                self.Battle.Log("Antler Rush was converted to a simple Stride.");
                                action.SpentActions = 1;
                                action.RevertRequested = true;
                            }
                            else
                            {
                                // Strike
                                List<Option> options = CommonCombatActions.GetStrikePossibilities(self, cr => cr.IsAdjacentTo(self));
                                options.RemoveAll(opt => !opt.Text.ToLower().Contains("antler"));
                                // Disarm
                                foreach (CombatAction disarm in CombatManeuverPossibilities.GetAllOptions(CombatManeuverPossibilities.CreateDisarmPossibility(self)))
                                    GameLoop.AddDirectUsageOnCreatureOptions(disarm.WithActionCost(0), options, true);
                                // Shove
                                foreach (CombatAction shove in CombatManeuverPossibilities.GetAllOptions(CombatManeuverPossibilities.CreateShovePossibility(self)))
                                    GameLoop.AddDirectUsageOnCreatureOptions(shove.WithActionCost(0), options, true);
                                options.Add(new PassViaButtonOption("Pass"));
                                AdvancedRequest request = new AdvancedRequest(self,
                                    "Choose a creature.", options)
                                {
                                    TopBarIcon = IllustrationName.Jaws,
                                    TopBarText = "Choose a creature to Disarm, Shove, or Strike with your antlers.",
                                };
                                Option chosenOption = (await self.Battle.SendRequest(request)).ChosenOption;
                                switch (chosenOption)
                                {
                                    case CreatureOption:
                                        break;
                                    case PassViaButtonOption:
                                        return;
                                }

                                await chosenOption.Action();
                            }
                        });
                    return new ActionPossibility(antlerRush);
                };
            });
        ModManager.AddFeat(antlerRush);

        // bear hug
        Feat bearHug = new TrueFeat(
                ModData.FeatNames.BearHug,
                6,
                "You hug your opponent.",
                "{b}Requirements{/b} You're in your werebear animal or hybrid shape, and your last action was a successful claw Strike.\n\nYou make another claw Strike against the same target. If this Strike hits, the target is grabbed.",
                [])
            .WithActionCost(2)
            .WithAvailableAsArchetypeFeat(ModData.Traits.Werecreature)
            .WithPrerequisite(ModData.FeatNames.Werebear, "Werebear")
            .WithPermanentQEffect("You can follow a claw Strike with another claw Strike that grabs on a hit.",
                qfFeat =>
                {
                    qfFeat.YouBeginAction = async (qfThis, action) =>
                    {
                        // Track last action as werebear claw strike
                        // Set qfThis.Tag = true;
                    };
                    qfFeat.ProvideMainAction = qfThis =>
                    {

                        CombatAction hugAction = new CombatAction(
                                qfThis.Owner,
                                IllustrationName.DragonClaws,
                                "Bear Hug",
                                [Trait.Archetype, Trait.Basic],
                                "",
                                Target.ReachWithWeaponOfTrait());
                        return null;
                    };
                });
        ModManager.AddFeat(bearHug);

        // death roll

        // echolocation // hmm maybe

        // fearful symmetry

        // feeding frenzy

        // pack attack

        // plague rat

        // rushing boar

        // cornered animal

        // feral mending

        // terrifying transformation

        // you don't smell right // maybe useful just for extending feral sense, if that's made to do anything

        // PETR: At level 10 with Dire Growth, due to the lack of size category mechanics, call out wererat specifically.
    }
}