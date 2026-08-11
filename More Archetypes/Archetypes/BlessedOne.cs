using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
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
        
        // Accelerating Touch
        yield return ArchetypeFeats.SafelyDuplicateFeatAsArchetypeFeat(
            Champion.AcceleratingTouchFeatName, ModData.Traits.BlessedOne, 6);
        
        // Lv.6(4): Mercy (a difficult inclusion)
        
        // Lv.8: Blessed Spell (depends on Mercy)
        
        // Lv.10(8): Greater Mercy (depends on Mercy)
        
        /* Higher Level Feats
         * @12 Blessed Denial
         * @14 (really: 12) Affliction Mercy
         * @14 (really: 12) Amplifying Touch
         * @20 (really: 18) Rejuvenating Touch
         * @20 (really: 18) Ultimate Mercy
         */
    }
}