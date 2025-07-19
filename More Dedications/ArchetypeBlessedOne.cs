using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations.AuraAnimations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Champion;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Kineticist;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.Archetypes;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreDedications;

public static class ArchetypeBlessedOne
{
    public static void LoadMod()
    {
        Feat blessedOneDedication = ArchetypeFeats.CreateAgnosticArchetypeDedication(
            ModData.Traits.BlessedOneArchetype,
            "Through luck or deed, heritage or heroics, you carry the blessing of a deity. This blessing manifests as the ability to heal wounds and remove harmful conditions, and exists independent of worship.",
            "You learn the " + AllSpells.CreateSpellLink(ChampionFocusSpells.LayOnHands, ModData.Traits.BlessedOneArchetype) + " champion focus spell. This feat grants a focus pool of 1 Focus Point, or an additional Focus Point if you already had one."/*+" Your focus spells from the blessed one archetype are divine spells."*/)
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Spell, Proficiency.Trained);
                
                // DD code safeguards allow you to learn a focus spell multiple times, so...
                if (values.FocusSpells.TryGetValue(Trait.Champion, out FocusSpells? champSpells) && champSpells.Spells.Any(spell => spell.SpellId == ChampionFocusSpells.LayOnHands))
                    values.FocusPointCount = Math.Min(values.FocusPointCount+1, 3);
                else
                    values.AddFocusSpellAndFocusPoint(
                        Trait.Champion, // "devotion spells" == champion spells, so, Champion trait instead of Blessed One.
                        Ability.Charisma,
                        ChampionFocusSpells.LayOnHands);
            });
        blessedOneDedication.Traits.Insert(0, ModData.Traits.MoreDedications);
        ModManager.AddFeat(blessedOneDedication);
        
        // Blessed Sacrifice
        var protectorsSacrifice = ModManager.RegisterNewSpell(
            "ProtectorsSacrifice",
            1,
            (spellId, spellcaster, spellLevel, inCombat, spellInformation) =>
            {
                int reduction = 3 * spellLevel;
                string description = $"{{b}}Trigger{{/b}} An ally within 30 feet takes damage.\n\nReduce the damage the triggering ally would take by {S.HeightenedVariable(reduction, 3)}. You redirect this damage to yourself, but your immunities, weaknesses, resistances and so on do not apply.\n\nYou aren't subject to any conditions or other effects of whatever damaged your ally (such as poison from a venomous bite). Your ally is still subject to those effects even if you redirect all of the triggering damage to yourself.";
                
                return Spells.CreateModern(
                    ModData.Illustrations.ProtectorsSacrifice,
                    "Protector's Sacrifice",
                    [ModData.Traits.MoreDedications, Trait.Uncommon, Trait.Cleric, Trait.Focus, Trait.SomaticOnly],
                    "You protect your ally by suffering in their stead.",
                    description/*
                        + S.HeightenText(spellLevel, 1, inCombat, "{b}Heightened (+1){/b} The damage you redirect increases by 3.")*/,
                    Target.Uncastable(),
                    spellLevel,
                    null)
                        .WithActionCost(-2)
                        .WithSoundEffect(SfxName.Healing) // TODO: Better sfx
                        .WithCastsAsAReaction((qfThis, spell, castable) =>
                        {
                            Creature cleric = qfThis.Owner;
                            
                            qfThis.AddGrantingOfTechnical(
                                cr => cr.FriendOfAndNotSelf(cleric) && cr.DistanceTo(cleric) <= 6,
                                qfTech =>
                                {
                                    Creature ally = qfTech.Owner;
                                    qfTech.YouAreDealtDamage = async (qfTech2, attacker, dStuff, defender) =>
                                    {
                                        if (!await cleric.AskToUseReaction(
                                                $"{{b}}Protector's Sacrifice {{icon:Reaction}}{{/b}}\n{ally} is about to take {dStuff.Amount} damage. Redirect {{b}}{reduction}{{/b}} of that damage to yourself?\n{{Red}}Focus Points: {cleric.Spellcasting?.FocusPoints ?? 0}{{/Red}}",
                                                ModData.Illustrations.ProtectorsSacrifice))
                                            return null;
                                        
                                        cleric.Spellcasting?.UseUpSpellcastingResources(spell);

                                        int taken = Math.Min(dStuff.Amount, reduction);
                                        
                                        cleric.TakeDamage(taken);
                                        cleric.Overhead(
                                            "-"+taken, Color.Red,
                                            $"{cleric.Name} redirects {taken} damage to themselves.", "Damage",
                                            $"{{b}}{reduction} of {dStuff.Amount}{{/b}} Protector's sacrifice\n{{b}}= {taken}{{/b}}\n\n{{b}}{taken}{{/b}} Total damage", true);

                                        return new ReduceDamageModification(reduction, "Protector's sacrifice");
                                    };
                                });
                        })
                        .WithHeighteningNumerical(spellLevel, 1, inCombat, 1, "The damage you redirect increases by 3.");
            });
        Feat blessedSacrifice = new TrueFeat(
            ModData.FeatNames.BlessedSacrifice,
            4,
            null,
            $"You gain the {AllSpells.CreateSpellLink(protectorsSacrifice, Trait.Champion)} domain spell as a devotion spell. Increase the number of Focus Points in your focus pool by 1.",
            [ModData.Traits.MoreDedications])
            .WithAvailableAsArchetypeFeat(ModData.Traits.BlessedOneArchetype)
            .WithOnSheet(values =>
            {
                // DD code safeguards allow you to learn a focus spell multiple times, so...
                if (values.FocusSpells.TryGetValue(Trait.Champion, out FocusSpells? champSpells) && champSpells.Spells.Any(spell => spell.SpellId == protectorsSacrifice))
                    values.FocusPointCount = Math.Min(values.FocusPointCount+1, 3);
                else
                    values.AddFocusSpellAndFocusPoint(
                        Trait.Champion, // "devotion spells" == champion spells, so, Champion trait instead of Blessed One.
                        Ability.Charisma,
                        protectorsSacrifice);
            });
        ModManager.AddFeat(blessedSacrifice);
        
        // Accelerating Touch
        ModManager.AddFeat(ArchetypeFeats.DuplicateFeatAsArchetypeFeat(
            Champion.AcceleratingTouchFeatName,
            ModData.Traits.BlessedOneArchetype,
            6));
        
        // NO MERCY??? :sob:
        
        // Blessed Spell
        
        // Invigorating Mercy
        
        // Greater Mercy (out of scope, but I'd probably have to add that too since it's lv8 for Champs)
    }
}