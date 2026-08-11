using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreArchetypes.Archetypes;

public static class BlessedOne
{
    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddAndReplaceFeat(ft);
    }

    public static IEnumerable<Feat> CreateFeats()
    {
        // Lv2: Blessed One Dedication
        // ArchetypeFeats.CreateOrUpdateDedication
        Feat blessedDed = ArchetypeFeats.CreateAgnosticArchetypeDedication(
                ModData.Traits.BlessedOne,
                "Through luck or deed, heritage or heroics, you carry the blessing of a deity. This blessing manifests as the ability to heal wounds and remove harmful conditions, and exists independent of any worship.",
                $"You learn the {ChampionFocusSpells.LayOnHands.ToLink("lay on hands", Trait.Champion, null)} focus spell as a champion. This feat grants a focus pool of 1 Focus Point, or an additional Focus Point if you already had one." /*+" Your focus spells from the blessed one archetype are divine spells."*/)
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Spell, Proficiency.Trained);
                
                // DD code safeguards allow you to learn a focus spell multiple times, so...
                if (values.FocusSpells.TryGetValue(Trait.Champion, out FocusSpells? champSpells)
                    && champSpells.Spells.Any(spell => spell.SpellId == ChampionFocusSpells.LayOnHands))
                    values.FocusPointCount = Math.Min(values.FocusPointCount+1, 3);
                else
                    values.AddFocusSpellAndFocusPoint(
                        Trait.Champion, // "devotion spells" == champion spells, so, Champion trait instead of Blessed One.
                        Ability.Charisma,
                        ChampionFocusSpells.LayOnHands);
            });
        ModData.FeatNames.BlessedOneDedication = blessedDed.FeatName;
        yield return blessedDed;
        
        // Protector's Sacrifice spell
        Func<SpellId,Creature?,int,bool,SpellInformation,CombatAction> createSpellInstance = (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
        {
            int reduction = 3 * spellLevel;
            string description =
                $$"""
                  {b}Trigger{/b} An ally within 30 feet takes damage.

                  Reduce the damage the triggering ally would take by {{(inCombat ? S.HeightenedVariable(reduction, 3) : 3)}}. You redirect this damage to yourself, but your immunities, weaknesses, resistances and so on do not apply.

                  You aren't subject to any conditions or other effects of whatever damaged your ally (such as poison from a venomous bite). Your ally is still subject to those effects even if you redirect all of the triggering damage to yourself.
                  """;
            
            return Spells.CreateModern(
                ModData.Illustrations.ProtectorsSacrifice,
                "Protector's Sacrifice",
                [ModData.ModTrait, Trait.Uncommon, Trait.Cleric, Trait.Focus, Trait.SomaticOnly],
                "You protect your ally by suffering in their stead.",
                description,
                Target.Uncastable(),
                spellLevel,
                null)
                    .WithActionCost(-2)
                    .WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The damage you redirect increases by 3.")
                    .WithCastsAsAReaction((qfThis, spell, castable) =>
                    {
                        // Toggle the reaction spam.
                        bool isOn = true;
                        qfThis.ProvideActionIntoPossibilitySection = (qfThis2, section) =>
                        {
                            if (section.PossibilitySectionId != PossibilitySectionId.SkillActions)
                                return null;
                            
                            return new ActionPossibility(new CombatAction(
                                        qfThis2.Owner,
                                        new CornerIllustration(ModData.Illustrations.ProtectorsSacrifice, isOn ? ModData.Illustrations.NoSymbol : ModData.Illustrations.CheckSymbol, Direction.Southeast),
                                        (isOn ? "Disable" : "Enable") + " Protector's Sacrifice",
                                        [],
                                        (isOn ? "Never ask" : "Always ask") + " to use {link:ProtectorsSacrifice}protector's sacrifice{/}.\n\n{i}(This setting does not persist between encounters.){/i}",
                                        Target.Self())
                                    .WithActionCost(0)
                                    .WithEffectOnSelf(async _ =>
                                    {
                                        isOn = !isOn;
                                    }))
                                .WithPossibilityGroup(Constants.POSSIBILITY_GROUP_TOGGLES);
                        };
                        
                        Creature cleric = qfThis.Owner;
                        qfThis.AddGrantingOfTechnical(
                            cr =>
                                cr.FriendOfAndNotSelf(cleric)
                                && cr.DistanceTo(cleric) <= 6,
                            qfTech =>
                            {
                                Creature ally = qfTech.Owner;
                                qfTech.YouAreDealtDamageReaction = (qfTech2, dEvent) =>
                                {
                                    if (!castable()
                                        || !isOn
                                        || cleric.HasLineOfEffectTo(ally) >= CoverKind.Blocked)
                                        return null;

                                    CombatAction sacrifice = new CombatAction(
                                            cleric,
                                            ModData.Illustrations.ProtectorsSacrifice,
                                            "Protector's Sacrifice",
                                            [ModData.ModTrait, Trait.Uncommon, Trait.Cleric, Trait.Focus, Trait.SomaticOnly, Trait.Manipulate, Trait.Spell],
                                            spell.Description,
                                            Target.RangedFriend(6))
                                        .WithActionCost(-2)
                                        .WithSoundEffect(SfxName.Abjuration)
                                        .WithTag(dEvent) // Store the targeted damage event
                                        .WithEffectOnEachTarget(async (_, caster, _, _) =>
                                        {
                                            // Not sure where in the base game's code it's happening but focus points are already being expended.
                                            //caster.Spellcasting?.UseUpSpellcastingResources(spell);

                                            int taken = Math.Min(dEvent.TotalResolvedDamage, reduction);
                                            cleric.TakeDamage(taken);
                                            
                                            cleric.Overhead(
                                                "-"+taken, Color.Red,
                                                $"{cleric.Name} redirects {taken} damage to themselves.",
                                                "Damage",
                                                $$"""
                                                  {b}{{reduction}} of {{dEvent.TotalResolvedDamage}}{/b} Protector's sacrifice
                                                  {b}= {{taken}}{/b}

                                                  {b}{{taken}}{/b} Total damage
                                                  """);
                                            
                                            dEvent.ReduceBy(reduction, "Protector's sacrifice");
                                        });

                                    if (!sacrifice.CanBeginToUse(cleric))
                                        return null;

                                    ReactionOption reactOpt = ReactionOption.WrapFullcastWithChosenTargets(
                                            sacrifice,
                                            ChosenTargets.CreateSingleTarget(ally),
                                            $"Redirect up to {{b}}{reduction}{{/b}} points of damage to yourself.");
                                        // Not needed due to -2 cost
                                        //.WithIsReaction();

                                    // Add some resource tracking
                                    reactOpt.Caption = reactOpt.Caption.Replace(
                                        "{/b}",
                                        "{/b} (FP: " + string.Concat(Enumerable.Repeat("{icon:spontaneousspellslot}", cleric.Spellcasting?.FocusPoints ?? 0)) + ")");

                                    return reactOpt;
                                };
                            });
                    });
        };
        if (ModManager.TryParse("ProtectorsSacrifice", out SpellId protector))
        {
            ModData.SpellIds.ProtectorsSacrifice = protector;
            ModManager.RegisterActionOnEachSpell(spell =>
            {
                if (spell.SpellId != protector
                    || spell.SpellId == SpellId.None)
                    return;

                CombatAction newSpell = createSpellInstance(protector, spell.Owner, spell.SpellLevel,
                    spell.Owner != null && spell.Owner.Battle != TBattle.Pseudobattle, spell.SpellInformation!);

                spell.Illustration = newSpell.Illustration;
                spell.Name = newSpell.Name;
                spell.Traits = newSpell.Traits;
                spell.Description = newSpell.Description;
                spell.Target = newSpell.Target;
                spell.ActionCost = newSpell.ActionCost;
                spell.WithCastsAsAReaction(newSpell.CastsAsAReaction!);
            });
        }
        else
            ModData.SpellIds.ProtectorsSacrifice = ModManager.RegisterNewSpell("ProtectorsSacrifice", 1, createSpellInstance);
        
        // Lv4: Blessed Sacrifice
        yield return new TrueFeat(
                ModData.FeatNames.BlessedSacrifice, 4,
                null,
                $"You gain the {AllSpells.CreateSpellLink(ModData.SpellIds.ProtectorsSacrifice, Trait.Champion)} domain spell as a devotion spell. Increase the number of Focus Points in your focus pool by 1.",
                [])
            .WithAvailableAsArchetypeFeat(ModData.Traits.BlessedOne)
            .WithOnSheet(values =>
            {
                // DD code safeguards allow you to learn a focus spell multiple times, so...
                if (values.FocusSpells.TryGetValue(Trait.Champion, out FocusSpells? champSpells)
                    && champSpells.Spells.Any(spell =>
                        spell.SpellId == ModData.SpellIds.ProtectorsSacrifice))
                {
                    values.FocusPointCount = Math.Min(values.FocusPointCount + 1, 3);
                }
                else
                    values.AddFocusSpellAndFocusPoint(
                        Trait.Champion, // "devotion spells" == champion spells, so, Champion trait instead of Blessed One.
                        Ability.Charisma,
                        ModData.SpellIds.ProtectorsSacrifice);
            });
        
        // Lv4: Mercy
        yield return new TrueFeat(
                ModData.FeatNames.Mercy, 4,
                "Your touch soothes the body or mind.",
                $$"""
                You can cast {{ChampionFocusSpells.LayOnHands.ToLink("lay on hands", Trait.Champion, null)}} targeting a living creature using 2 actions instead of 1. If you do, you can attempt to counteract one condition of your choice affecting the target. When you select this feat, choose an option which determines the conditions you can counteract.

                {b}Special{/b} You can select this feat up to three times. Each time, choose a different type of mercy and add its options to those you can choose when you cast a 2-action lay on hands.
                """,
                [Trait.Champion],
                CreateMercyOptions())
            {
                // Uses custom Special message instead of generic one.
                #pragma warning disable CS0612 // Type or member is obsolete
                CanSelectMultipleTimes = true,
                #pragma warning restore CS0612 // Type or member is obsolete
            }
            .WithPrerequisite(
                values => values.FocusSpells.Any(kvp =>
                    kvp.Value.Spells.Any(spell =>
                        spell.SpellId == ChampionFocusSpells.LayOnHands)),
                "You must know the {i}lay on hands{/i} focus spell.")
            .WithOnCreature(self =>
            {
                List<Feat>? mercies = self.PersistentCharacterSheet?.Calculated.AllFeats
                    .Where(ft =>
                        ft.HasTrait(ModData.Traits.MercyOption))
                    .ToList();

                if (mercies is null)
                    return;

                bool hasGreaterMercy = self.HasFeat(ModData.FeatNames.GreaterMercy);
                
                List<string> conditions = mercies
                    .Select(ft => (ft.Tag as MercyTag)!)
                    .SelectMany(tag =>
                    {
                        List<QEffectId> ids = tag.BaseConditions;
                        if (hasGreaterMercy)
                        {
                            ids.AddRange(tag.GreaterConditions);
                            if (self.Level >= 12)
                                ids.AddRange(tag.GreaterConditions12);
                            if (self.Level >= 16)
                                ids.AddRange(tag.GreaterConditions16);
                        }
                        return ids;
                    })
                    // TODO: Remove Grappled when changes from other TODOs are made.
                    .Where(effect => effect is not QEffectId.Grappled)
                    .Distinct()
                    .Select(effect => effect.ToStringOrTechnical().ToLower())
                    .ToList();
                
                conditions.Sort((qf1, qf2) =>
                    string.Compare(qf1, qf2, StringComparison.Ordinal));

                // Add once
                if (self.HasEffect(qf => qf.Name == "Mercy"))
                    return;
                
                self.AddQEffect(new QEffect(
                    "Mercy",
                    "You can cast {i}lay on hands{/i} as {icon:TwoActions} 2 actions to also counteract the " + S.ConstructOrList(conditions, "and") + " conditions."));
            });
        
        // Lv6: Mercy for Blessed One
        TrueFeat mercyForBlessedOne = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.Mercy, ModData.Traits.BlessedOne, 6);
        ModData.FeatNames.MercyForBlessedOne = mercyForBlessedOne.FeatName;
        yield return mercyForBlessedOne;

        // Lv.8: Blessed Spell (depends on Mercy)

        // Lv.8: Greater Mercy
        yield return new TrueFeat(
                ModData.FeatNames.GreaterMercy, 8,
                "Your faith enhances your ability to remove conditions.",
                """
                Add the following options to the list of conditions you can counteract for any type of mercy you can grant.
                • {b}Mercy of the Body{/b} {r}drained{/r}, {r}slowed{/r}; (16th level) {r}stunned{/r}.
                • {b}Mercy of Grace{/b} {r}immobilized{/r}, {r}restrained{/r}, {r}slowed{/r}; (12th level) {r}petrified{/r}; (16th level) {r}stunned{/r}.
                • {b}Mercy of the Mind{/b} {r}confused{/r}, {r}controlled{/r}, {r}slowed{/r}; (16th level) {r}doomed{/r}, {r}stunned{/r}.
                """,
                [Trait.Champion])
            .WithPrerequisite(ModData.FeatNames.Mercy, "Mercy");

        // Lv.10: Greater Mercy for Blessed One
        TrueFeat gMercyForBlessedOne = ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            ModData.FeatNames.GreaterMercy, ModData.Traits.BlessedOne, 6);
        ModData.FeatNames.GreaterMercyForBlessedOne = gMercyForBlessedOne.FeatName;
        yield return gMercyForBlessedOne;

        /* Higher Level Feats
         * @12 Blessed Denial
         * @14 (really: 12) Affliction Mercy
         * @14 (really: 12) Amplifying Touch
         * @20 (really: 18) Rejuvenating Touch
         * @20 (really: 18) Ultimate Mercy
         */
    }

    /// <summary>
    /// Creates a new list of the standard 3 Mercy options.
    /// </summary>
    /// <remarks>This also fills out the FeatName content of ModData.</remarks>
    public static List<Feat> CreateMercyOptions()
    {
        List<Feat> subFeats = [];
        
        // Mercy of the Body
        if (MercyOption(
                "the Body",
                IllustrationName.RemoveConfusion,
                [QEffectId.Blinded, QEffectId.Dazzled, QEffectId.Deafened, QEffectId.Enfeebled, QEffectId.Sickened],
                [QEffectId.Drained, QEffectId.Slowed],
                [],
                [QEffectId.Stunned])
            is { } theBody)
        {
            subFeats.Add(theBody);
            ModData.FeatNames.MercyOfTheBody = theBody.FeatName;
        }
        
        // Mercy of Grace
        if (MercyOption(
                "Grace",
                IllustrationName.FreedomOfMovement,
                // TODO: Remove Grappled, target it indirectly. See TODO in GetMercyOptions().
                [QEffectId.Clumsy, QEffectId.Grabbed, QEffectId.Grappled, QEffectId.Paralyzed],
                [QEffectId.Immobilized, QEffectId.Restrained, QEffectId.Slowed],
                [QEffectId.Petrified],
                [QEffectId.Stunned])
            is { } grace)
        {
            subFeats.Add(grace);
            ModData.FeatNames.MercyOfGrace = grace.FeatName;
        }
        
        // Mercy of the Mind
        if (MercyOption(
                "the Mind",
                IllustrationName.SeeInvisibility,
                [QEffectId.Fleeing, QEffectId.Frightened, QEffectId.Stupefied],
                [QEffectId.Confused, QEffectId.Controlled, QEffectId.Slowed],
                [],
                [QEffectId.Doomed, QEffectId.Stunned])
            is { } theMind)
        {
            subFeats.Add(theMind);
            ModData.FeatNames.MercyOfTheMind = theMind.FeatName;
        }
        
        return subFeats;
    }

    /// <summary>
    /// Creates a Mercy option.
    /// </summary>
    /// <param name="mercyOfWhat">The Mercy to be added in the form of "the Body" and "Grace".</param>
    /// <param name="emblem">The emblem of the metamagic provider.</param>
    /// <param name="conditions">The list of conditions to be targeted by Mercy.</param>
    /// <param name="conditions8">The conditions used for Greater Mercy.</param>
    /// <param name="conditions12">The conditions used for Greater Mercy at level 12.</param>
    /// <param name="conditions16">The conditions used for Greater Mercy at level 16.</param>
    /// <returns>The subfeat option for Mercy. If the feat already exists, then this returns null.</returns>
    public static Feat? MercyOption(
        string mercyOfWhat,
        IllustrationName emblem,
        List<QEffectId> conditions,
        List<QEffectId> conditions8,
        List<QEffectId> conditions12,
        List<QEffectId> conditions16)
    {
        string technicalName = "MercyOf" + mercyOfWhat;
        string humanizedName = "Mercy of " + mercyOfWhat;
        if (AllFeats.AllNamesSet.Contains(technicalName))
            return null;

        string conditionsList = ListConditions(conditions);
        
        Feat mercy = new Feat(
                ModManager.RegisterFeatName(
                    technicalName,
                    humanizedName),
                null,
                $"You can use {ModData.FeatNames.Mercy.ToLink("Mercy")} to counteract one of the following conditions:\n{conditionsList}.",
                [ModData.Traits.MercyOption],
                null)
            .WithIllustration(emblem)
            .WithPermanentQEffect(null, qfFeat =>
            {
                // Testing conditions
                /*qfFeat.ProvideActionsIntoPossibilitySection = (qfThis, section) =>
                {
                    if (section.PossibilitySectionId != PossibilitySectionId.ContextualActions)
                        return [];

                    return finalConditions
                        .Select(condition => new CombatAction(
                            qfThis.Owner,
                            IllustrationName.YellowWarning,
                            "Inflict " + condition.ToStringOrTechnical(),
                            [],
                            $"Inflict {condition.ToStringOrTechnical()} 5 for 5 rounds.",
                            Target.RangedCreature(99))
                            .WithActionCost(0)
                            .WithEffectOnEachTarget(async (spell, caster, target, result) => target.AddQEffect(new QEffect()
                            {
                                Name = condition.ToStringOrTechnical(),
                                Illustration = IllustrationName.YellowWarning,
                                ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
                                RoundsLeft = 5,
                                Id = condition,
                                Source = caster,
                                SourceAction = spell,
                                Value = 5,
                            })))
                        .Select(action => new ActionPossibility(action).WithPossibilityGroup("Mercy Conditions"))
                        .ToList();
                };*/

                List<QEffectId> finalConditions = conditions.ToList();
                if (qfFeat.Owner.HasFeat(ModData.FeatNames.GreaterMercy))
                {
                    finalConditions.AddRange(conditions8);
                    if (qfFeat.Owner.Level >= 12)
                        finalConditions.AddRange(conditions12);
                    if (qfFeat.Owner.Level >= 16)
                        finalConditions.AddRange(conditions16);
                }
                finalConditions = finalConditions.Distinct().ToList();
                string conditionsListFinal = ListConditions(finalConditions);
                
                qfFeat.MetamagicProvider = new MetamagicProvider(
                    humanizedName,
                    emblem,
                    true,
                    spell =>
                    {
                        if (spell.SpellId != ChampionFocusSpells.LayOnHands
                            || spell.ActionCost != 1)
                            return null;
                        
                        Spell duplicate = Spell.DuplicateSpell(spell);
                        CombatAction mercy = duplicate.CombatActionSpell;

                        mercy.Name = humanizedName;
                        mercy.Target = Target.AdjacentCreatureOrSelf()
                            .WithAdditionalConditionOnTargetCreature((a, d) =>
                                a.FriendOf(d)
                                    ? Usability.Usable
                                    : Usability.CommonReasons.TargetIsNotAlly)
                            .WithAdditionalConditionOnTargetCreature((_, d) =>
                                GetMercyOptions(d, finalConditions).Count != 0
                                    ? Usability.Usable
                                    : Usability.NotUsableOnThisCreature("not-sick"))
                            .WithOverriddenTargetLine("1 living ally", false);
                        mercy
                            .WithActionCost(2)
                            .WithDescription(mercy.Description.Replace(
                                $"If the target is an undead enemy, it takes {S.HeightenedVariable(mercy.SpellLevel, 1)}d6 positive damage (basic Fortitude save mitigates). If it fails the save, it also takes a –2 status penalty to AC for 1 round.",
                                $"{humanizedName.WithTag("b").WithColor("Blue")} Also attempt to counteract one of the following conditions:\n{conditionsListFinal}."))
                            .WithTargetingTooltip((mercy_2, target, _) =>
                            {
                                List<QEffect> options = GetMercyOptions(target, finalConditions);

                                List<string> chances = options
                                    .Select(qf =>
                                    {
                                        CombatAction sourceAction = qf.SourceAction!;

                                        SavingThrow? savingThrow = sourceAction.SavingThrow;
                                        int flatDC = savingThrow == null
                                            ? sourceAction.SpellcastingSource?.GetSpellSaveDC(sourceAction)
                                              ?? sourceAction.Owner.ClassOrSpellDC()
                                            : savingThrow.DC(sourceAction.Owner);

                                        CombatAction counteract = new CombatAction(
                                                mercy_2.Owner,
                                                IllustrationName.SpellImmunity,
                                                mercy_2.Name,
                                                mercy_2.Traits.ToArray(),
                                                mercy_2.Description,
                                                Target.Self())
                                            .WithSpellcastingSource(mercy_2.SpellcastingSource)
                                            .WithActiveRollSpecification(new ActiveRollSpecification(
                                                TaggedChecks.SpellAttack(),
                                                Checks.FlatDC(flatDC)));

                                        return
                                            $"{(qf.Name ?? "Unknown").WithTag("b")}\n{CombatActionExecution.BreakdownAttackForTooltip(
                                                    counteract, sourceAction.Owner)
                                                .TooltipDescription}";
                                    })
                                    .ToList();

                                return string.Join("\n\n", chances);
                            })
                            .WithSavingThrow(null)
                            .WithEffectOnEachTarget(async (action, caster, target, _) =>
                            {
                                if (!target.IsLivingCreature)
                                {
                                    action.RevertRequested = true;
                                    return;
                                }

                                List<(QEffect Condition, int Chance)> options =
                                    GetMercyOptions(target, finalConditions)
                                    .Select(qf =>
                                        (qf, Counteracting.DetermineSuccessChance(action, qf.SourceAction!, TaggedChecks.SpellAttack())))
                                    .Where(qf => qf.Item2 > 0)
                                    .ToList();

                                QEffect? chosen;

                                if (options.Count < 2)
                                    chosen = options.FirstOrDefault().Condition;
                                else
                                {
                                    int index = (await caster.AskForChoiceAmongButtons(
                                            action.Illustration,
                                            $$"""
                                             {{humanizedName.WithTag("b")}} {icon:TwoActions}
                                             Choose a condition to attempt to {tooltip:counteract}counteract{/} on {{target.ToColoredName()}}.
                                             """,
                                            options.Select(qf =>
                                                $"{(qf.Condition.Illustration is not null ? $"{qf.Condition.Illustration.IllustrationAsIconString} " : null)}{qf.Condition.Name} ({qf.Chance}%)").ToArray()))
                                        .Index;
                                    // If pass, return
                                    if (index > (options.Count - 1))
                                        return;
                                    chosen = options[index].Condition;
                                }
                                
                                if (chosen is null)
                                {
                                    action.RevertRequested = true;
                                    return;
                                }

                                if (Counteracting.CounteractAndLog(
                                        action,
                                        chosen.SourceAction!,
                                        target,
                                        TaggedChecks.SpellAttack()))
                                    chosen.ExpiresAt = ExpirationCondition.Immediately;
                            });
                        
                        return mercy;
                    });
            })
            .WithTag(new MercyTag(conditions, conditions8, conditions12, conditions16));

        return mercy;

        string ListConditions(List<QEffectId> list)
        {
            return string.Join(
                "\n",
                list
                    .Where(cond => cond is not QEffectId.Grappled)
                    .Select(cond =>
                        $"• {{r}}{cond.ToStringOrTechnical().ToLower()}{{/r}}"));
        }
    }

    public static List<QEffect> GetMercyOptions(Creature target, List<QEffectId> conditions)
    {
        List<QEffect> options = target.QEffects
            .Where(qf => conditions.Contains(qf.Id))
            .Where(Healable)
            .ToList();
        
        // TODO: Remove Grappled from Mercy lists, then add in explicit handling for allowing ephemeral grabbed/restrained that deletes grappled. This would need to collaborate with Healable, which immediately denies a QEffect if it's ephemeral.
        
        // Don't include grappled if it's actually restrained,
        // and you can't affect restrained
        if (options.Any(qf => qf.Id == QEffectId.Restrained)
            && !conditions.Contains(QEffectId.Restrained))
            options.RemoveAll(qf =>
                qf.Id == QEffectId.Grappled);
        
        return options;
    }
    
    public static bool Healable(QEffect qf)
    {
        if (qf.ExpiresAt == ExpirationCondition.Ephemeral)
            return false;
        if (qf.Id is QEffectId.Grabbed or QEffectId.Restrained or QEffectId.Grappled)
            return true;
        return qf.SourceAction is not null;
    }

    public class MercyTag()
    {
        public readonly List<QEffectId> BaseConditions = [];
        public readonly List<QEffectId> GreaterConditions = [];
        public readonly List<QEffectId> GreaterConditions12 = [];
        public readonly List<QEffectId> GreaterConditions16 = [];

        public MercyTag(List<QEffectId> @base, List<QEffectId> greater, List<QEffectId> greater12,
            List<QEffectId> greater16) : this()
        {
            BaseConditions = @base;
            GreaterConditions = greater;
            GreaterConditions12 = greater12;
            GreaterConditions16 = greater16;
        }
    }
}