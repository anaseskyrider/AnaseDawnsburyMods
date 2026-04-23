using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
using Dawnsbury.Modding;
using HarmonyLib;

namespace Dawnsbury.Mods.LoresAndWeaknesses;

/// <summary>
/// Handles the registration and collection of Lore skills.
/// </summary>
public static class Lores
{
    /// <summary>
    /// All Lore skills that have been registered.
    /// </summary>
    public static readonly List<Lore> AllLores = [];
    
    /// <summary>
    /// All Lore skills that have been registered and that are public (<see cref="Lore.IsHidden"/> is false).
    /// </summary>
    public static IReadOnlyList<Lore> AllPublicLores => AllLores.Where(lore => !lore.IsHidden).ToList();

    /// <summary>
    /// An invisible unicode symbol that's added to the beginning of the humanized name of feats in order to push them to the bottom of the list.
    /// </summary>
    public const string DisplayOffset = "𝒵";

    internal static void Load()
    {
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
        
        Lores.RegisterNewLore(
            "Warfare Lore",
            $"""
            You have studied battlefields, tactics, and strategy.

            You can use this skill to {RecallWeakness.GetActionLink()} on martial creatures (wields martial weapons; or has a Reactive Strike or Shield Block feature).
            """,
            (_, target) =>
                target.ItemsHeldAtTheBeginningOfTheEncounter.Any(item =>
                    item.HasAnyTraits([Trait.Advanced, Trait.Martial]))
                || target.HasEffect(QEffectId.ShieldBlock)
                || target.HasEffect(QEffectId.AttackOfOpportunity));
        
        Lores.RegisterNewLore(
            "Undead Lore",
            $"""
             You have studied the nature of undead and the dark energies that animate their flesh and bind their souls to the material plane.

             You can use this skill to {RecallWeakness.GetActionLink()} on undead creatures.
             """,
            (_, target) =>
                target.HasTrait(Trait.Undead));
        
        Lores.RegisterNewLore(
            "Elemental Lore",
            $"""
             You have studied creatures from the elemental planes.

             You can use this skill to {RecallWeakness.GetActionLink()} on elemental creatures.
             """,
            (_, target) =>
                target.HasTrait(Trait.Elemental));
        
        Lores.RegisterNewLore(
            "Starborn Lore",
            $"""
            You have studied the starborn — the seven commanders of the demonic armada that has waged war on us for seven years. You read through every report that's come out of the Western Reaches and you learned everything there is to know — from their origins in the distant worlds they've scoured clean, through the strategy and tactics they employ on the battlefield, to their innate weaknesses which could be their downfall.

            You can use this skill to {RecallWeakness.GetActionLink()} on starborn creatures.
            """,
            (_, target) =>
                target.HasTrait(Trait.Starborn),
            true);
        
        // Add bonuses to the Outwit Ranger-subclass.
        Feat outwit = AllFeats.GetFeatByFeatName(FeatName.HuntersEdgeOutwit)
            .WithOnCreature(cr =>
            {
                cr.AddQEffect(new QEffect()
                {
                    Name = "[LORES AND WEAKNESSES: OUTWIT ADJUSTMENT]",
                    BonusToSkillChecks = (skill, action, target) => 
                        Ranger.HasPrey(action.Owner, action.Owner, target)
                        && action.ActionId == RecallWeakness.RWActionId
                            ? new Bonus(2, BonusType.Circumstance, "Hunter's Edge: Outwit")
                            : null,
                    YouAcquireQEffect = (qfThis, qfAcquired) =>
                    {
                        if (qfAcquired.Name == "Hunter\'s Edge: Outwit"
                            && qfAcquired.Description == "You have +2 to Deception, Intimidation and Stealth against your prey, and a +1 to AC against your prey.")
                            qfAcquired.Description = qfAcquired.Description?.Replace("Stealth", $"Stealth; as well as {RecallWeakness.GetActionLink()};");
                        return qfAcquired;
                    }
                });
            });
        outwit.RulesText = outwit.RulesText.Replace("Stealth checks", $"Stealth checks; as well as {RecallWeakness.GetActionLink()} checks;");
        outwit.Traits.Insert(0, ModData.Traits.ModName);

        // Update the skill-training feats.
        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            List<SkillSelectionFeat> skillFeats = AllFeats.All
                .Select(ft => ft as SkillSelectionFeat)
                .WhereNotNull()
                .ToList();
            foreach (SkillSelectionFeat sFeat in skillFeats)
            {
                Skill skill = sFeat.Skill;
                string name = skill.ToStringOrTechnical();
                string ability = skill.ToAbility().ToStringOrTechnical();
                Lore? lore = AllLores.FirstOrDefault(lore => lore.Skill == skill);
                
                string trainedIn = "You become trained in " + name + ".";
                if (sFeat.RulesText.IndexOf(trainedIn) != -1
                    && string.IsNullOrEmpty(sFeat.FlavorText))
                {
                    sFeat.RulesText = sFeat.RulesText.Replace(trainedIn + "\n\n", "");
                    sFeat.FlavorText = trainedIn;
                }

                string basedSkill = "This is " + ability.WithIndefiniteArticle() + "-based skill.";
                string addMod = "{i}(You add your " + ability + " modifier to checks using this skill.){/i}";
                if (sFeat.RulesText.IndexOf(basedSkill) != -1
                    && sFeat.RulesText.IndexOf(addMod) != -1)
                {
                    sFeat.RulesText = sFeat.RulesText.Replace(
                        basedSkill + " " + addMod,
                        $"{{b}}Ability{{/b}} {ability} {{i}}(add your {ability} modifier to checks with this skill){{/i}}"
                        + (lore is not null
                            ? "\n{b}Lore{/b} " + (lore.IsSpecific
                                ? "Specific {i}(add a +5 bonus to checks with this lore){/i}"
                                : "Unspecific {i}(add a +2 bonus to checks with this lore){/i}")
                            : null));
                }

                sFeat.RulesText = sFeat.RulesText.Replace(
                    "{b}Trick Magic Item{/b}",
                    AllFeats.GetFeatByFeatName(FeatName.TrickMagicItem)
                        .ToLink("Trick Magic Item"));

                sFeat.RulesText = sFeat.RulesText.Replace(
                    "{b}Battle Medicine{/b}",
                    AllFeats.GetFeatByFeatName(FeatName.BattleMedicine)
                        .ToLink("Battle Medicine"));
            }
        };
    }

    internal static IEnumerable<Feat> CreateFeats()
    {
        // Additional Lore
        Feat addLore = new TrueFeat(
                RecallWeakness.FNAdditionalLore,
                1,
                "Your knowledge has expanded to encompass a new field.",
                """
                Choose a Lore skill. You become trained in it. At 3rd, 7th, and 15th levels, you automatically increase your proficiency with that skill as appropriate for a character of that level.

                {b}Special{/b} You can select this feat more than once, choosing a different Lore skill each time.
                """,
                [Trait.General, Trait.Skill],
                [/*Lore feats are added automatically every time a lore is registered*/])
            .WithIllustration(IllustrationName.SepiaFeat);
        addLore.CanSelectMultipleTimes = true;
        yield return addLore;
    }

    /// <summary>
    /// Grants the Additional Lore feat for this particular lore. <see cref="Lore.IsHidden"/> must be false, as Additional Lore only allows for public lores.
    /// </summary>
    /// <exception cref="InvalidOperationException">Lore instance was not found in Additional Lore's subfeats, possibly due to being improperly registered and not added to <see cref="AllPublicLores"/>.</exception>
    public static void GrantAdditionalLore(CalculatedCharacterSheetValues values, Lore publicLore)
    {
        Feat addLore = AllFeats.GetFeatByFeatName(RecallWeakness.FNAdditionalLore);
        Feat? subLore = addLore.Subfeats!.FirstOrDefault(ft => ft.Tag == publicLore);
        if (subLore is null)
            throw new InvalidOperationException("Lore skill of the name " + publicLore.Name + " was not found in Additional Lore's Subfeats, possibly due to being improperly registered and not added to Lores.AllPublicLores.");
        values.GrantFeat(
            addLore.FeatName,
            subLore.FeatName);
    }

    /// <summary>
    /// Works as <see cref="CalculatedCharacterSheetValues.TrainInThisOrSubstitute"/>, but it adds <see cref="Lore.Trained"/> directly instead of finding it through a FeatName. Avoids errors caused in <see cref="Skills.SkillToFeat(Skill)"/> from trying to use the underlying proficiency feats for a hidden Lore, which aren't normally registered.
    /// </summary>
    /// <param name="values">This character sheet.</param>
    /// <param name="lore">The Lore to train in.</param>
    /// <param name="mustSubLore">If true, the substituted skill must be a lore skill.</param>
    public static void TrainInThisOrSubstitute(this CalculatedCharacterSheetValues values, Lore lore, bool mustSubLore = false)
    {
        Feat skillFeat = lore.Trained;
        if (values.HasFeat(skillFeat))
        {
            values.AddSelectionOption(new SingleFeatSelectionOption(
                    "SubstituteLoreSkillTrainingFor" + lore.Skill.ToStringOrTechnical(),
                    "Substitute lore skill for " + lore.Skill.HumanizeTitleCase2(),
                    -1,
                    ft =>
                        ft is SkillSelectionFeat ssf
                        && (!mustSubLore
                            || AllPublicLores.Select(pLore => pLore.Skill).Contains(ssf.Skill)))
                .WithIsOptional());
        }
        else
        {
            values.AddFeat(skillFeat, null);
        }
    }

    /// <summary>
    /// Registers a new lore skill. If the lore already exists, then the original registration is returned instead. If you wish to modify a lore you know is already registered, use <see cref="GetRegisteredLore"/>.
    /// </summary>
    /// <param name="name">The full name of the lore skill, with the word Lore included, written in Title Case. Such as, "Warfare Lore".</param>
    /// <param name="description">The description of the lore skill. It's a good idea to look at existing skills for examples of what to write.</param>
    /// <param name="validRecallTarget">A function which determines whether the PLAYER attempting to Recall Weakness against a TARGET can use this lore skill for the check.</param>
    /// <param name="isSpecific">Whether this lore is a specific lore, or an unspecific lore.</param>
    /// <param name="isHidden">If true, then this lore is hidden from normal selections and is instead only available to specific classes or features.</param>
    /// <param name="relevantAbility">The ability used for Recall Weakness with this skill (default: Ability.Intelligence).</param>
    /// <returns>The Lore you just registered, or the original one if it was already registered.</returns>
    public static Lore RegisterNewLore(
        string name,
        string description,
        Func<Creature,Creature,bool>? validRecallTarget,
        bool isSpecific = false,
        bool isHidden = false,
        Ability relevantAbility = Ability.Intelligence)
    {
        // Return if already registered
        if (AllLores.FirstOrDefault(lore => lore.Name == name) is { } found)
            return found;
        
        // Begin constructing lore
        Lore newLore = new Lore(name, description, relevantAbility, isSpecific, isHidden, validRecallTarget);
        
        // Add to the relevant lists before patched-functions try to find your lore.
        AllLores.Add(newLore);

        // Add feats which increase proficiency
        newLore.Trained = AddSkillFeat(Proficiency.Trained, null);
        newLore.Expert = AddSkillFeat(Proficiency.Expert, newLore.Trained.FeatName);
        newLore.Master = AddSkillFeat(Proficiency.Master, newLore.Expert.FeatName);
        newLore.Legendary = AddSkillFeat(Proficiency.Legendary, newLore.Master.FeatName);
        
        // Add to Additional Lore
        if (!newLore.IsHidden)
        {
            Feat additionalSubFeat = new Feat(
                    ModManager.TryParse(ModData.IdPrepend + "AdditionalLore." + newLore.Name, out FeatName addLore)
                        ? addLore
                        : ModManager.RegisterFeatName(
                            ModData.IdPrepend + "AdditionalLore." + newLore.Name,
                            DisplayOffset + newLore.Name),
                    "", "", [], null)
                .WithIllustration(IllustrationName.NarratorBook)
                .WithOnSheet(values =>
                {
                    values.GrantFeat(newLore.Trained.FeatName);
                    values.AddAtLevel(
                        3,
                        v3 =>
                            v3.GrantFeat(newLore.Expert.FeatName));
                    values.AddAtLevel(
                        7,
                        v7 =>
                            v7.GrantFeat(newLore.Master.FeatName));
                    values.AddAtLevel(
                        15,
                        v15 =>
                            v15.GrantFeat(newLore.Legendary.FeatName));
                });
            additionalSubFeat.WithPrerequisite(
                values =>
                    values.HasFeat(additionalSubFeat)
                    || values.GetProficiency(newLore.Trait) < Proficiency.Legendary,
                "You are already legendary in this Lore.");
            additionalSubFeat.WithRulesTextCreator(sheet =>
            {
                // Don't inform the user that they're trained and can still take the feat
                // if they're legendary (feat is useless),
                // if they're untrained (they aren't trained),
                // if their training comes from this feat.
                if (sheet.Calculated.GetProficiency(newLore.Trait) is var loreProf
                    && loreProf is Proficiency.Legendary or Proficiency.Untrained
                    || sheet.Calculated.AllFeats.Any(ft => ft == additionalSubFeat))
                    return newLore.Trained.RulesText;

                return newLore.Trained.RulesText +
                       $"\n\n{{icon:YellowWarning}} You are already {loreProf.HumanizeLowerCase2()} in this lore. You can still take this feat and gain automatic increases.";
            });
            
            // Enforce DisplayOffset behavior even if the FeatName was already registered
            additionalSubFeat.CustomName = DisplayOffset + newLore.Name;
            
            ModManager.AddFeat(additionalSubFeat, ModData.Traits.ModName);
            AllFeats.GetFeatByFeatName(RecallWeakness.FNAdditionalLore)
                .Subfeats
                !.Add(additionalSubFeat);
        }
        
        return newLore;

        // Create a skill-training or skill-increasing feat.
        // Gets an existing one, if possible.
        Feat AddSkillFeat(Proficiency prof, FeatName? previous)
        {
            Feat skillFeat = (prof == Proficiency.Trained
                    ? AllFeats.All.FirstOrDefault(ft => ft is SkillSelectionFeat ssf && ssf.Skill == newLore.Skill) ??
                      new SkillSelectionFeat(
                          ModManager.TryParse(name, out FeatName ssFN)
                            ? ssFN
                            : ModManager.RegisterFeatName(name, DisplayOffset + name),
                          newLore.Skill,
                          newLore.Trait)
                    : AllFeats.All.FirstOrDefault(ft => ft is SkillIncreaseFeat sif && sif.Skill == newLore.Skill && sif.TargetProficiency == prof) ??
                      new SkillIncreaseFeat(
                          ModManager.TryParse(prof.ToStringOrTechnical() + name, out FeatName siFN)
                            ? siFN
                            : ModManager.RegisterFeatName(
                                  prof.ToStringOrTechnical() + name,
                                  DisplayOffset + prof.ToStringOrTechnical() + " in " + name),
                          newLore.Skill,
                          newLore.Trait,
                          prof,
                          previous))
                .WithIllustration(IllustrationName.NarratorBook);
            
            // Enforce DisplayOffset behavior even if the FeatName was already registered
            skillFeat.CustomName = skillFeat is SkillSelectionFeat
                ? DisplayOffset + name
                : DisplayOffset + prof.ToStringOrTechnical() + " in " + name;
            
            skillFeat.Traits.Add(ModData.Traits.Lore);
            skillFeat.Traits.Sort((t1, t2) => t1.ToStringOrTechnical().CompareTo(t2.ToStringOrTechnical()));
            
            // If it already existed, tag it as now being modified by LoresAndWeaknesses
            if (AllFeats.AlreadyExists(skillFeat.FeatName))
            {
                skillFeat.Traits.Insert(0, ModData.Traits.ModName);
                skillFeat.Traits.Remove(Trait.Mod);
            }
            else if (!newLore.IsHidden)
                ModManager.AddFeat(skillFeat, ModData.Traits.ModName);

            return skillFeat;
        }
    }

    /// <summary>
    /// Gets a Lore skill that is already registered with this mod's functionality. Both arguments are optional, so you can use either one to find it.
    /// </summary>
    /// <param name="name">The humanized <see cref="Lore.Name"/> of the lore, such as "Warfare Lore".</param>
    /// <param name="loreSkill">The registered <see cref="Skill"/> enum of the lore.</param>
    /// <returns></returns>
    public static Lore? GetRegisteredLore(
        string? name,
        Skill? loreSkill)
    {
        return AllLores.FirstOrDefault(lore => lore.Name == name || lore.Skill == loreSkill);
    }
}

/// <summary>
/// Each instance is a collection of related information to a lore, such as its string name, skill enum, the skill's in-game description, and so on.
/// </summary>
public class Lore
{
    #region Instance Data

    /// <summary>
    /// The name of the lore which includes the name of the lore with spaces, such as "Warfare Lore". This string is both the humanized name and technical name of the lore, and is equal to the output of <see cref="ModManager.ToStringOrTechnical(Dawnsbury.Core.Mechanics.Enumerations.Skill)"/>
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The description of the lore. It should tell you what creatures it can Recall Weaknesses for, or none if it doesn't interact with creatures on its own.
    /// </summary>
    /// <para>
    /// To modify a description for a Lore that's already been registered, see <see cref="WithNewDescription"/>.
    /// </para>
    public string Description { get; set; }

    /// <summary>
    /// The ability used when making checks to Recall Weakness with this skill. Most Lore skills use Intelligence.
    /// </summary>
    /// <para>
    /// To modify the ability for a Lore that's already been registered, see <see cref="WithAbility"/>.
    /// </para>
    public Ability RelevantAbility { get; set; }

    /// <summary>
    /// Specific Lores reduce the DC to checks to Recall Weakness by 5, while unspecific lores reduce them by 2. Most lores are unspecific.
    /// </summary>
    public bool IsSpecific { get; set; }
    
    /// <summary>
    /// If true, this lore is unavailable to standard skill selection and must be granted from a feature directly, such as with <see cref="CalculatedCharacterSheetValues.TrainInThisOrSubstitute"/>.
    /// </summary>
    public bool IsHidden { get; set; }
    
    /// <summary>
    /// The registered <see cref="Skill"/> enum associated with this lore.
    /// </summary>
    public Skill Skill { get; }
    
    /// <summary>
    /// The <see cref="Trait"/> associated with this lore.
    /// </summary>
    public Trait Trait { get; }
    
    /// <summary>
    /// The <see cref="SkillSelectionFeat"/> that trains you in this lore.
    /// </summary>
    public Feat Trained { get; set; } = null!;

    /// <summary>
    /// The <see cref="SkillIncreaseFeat"/> that makes you Expert in this lore.
    /// </summary>
    public Feat Expert { get; set; } = null!;

    /// <summary>
    /// The <see cref="SkillIncreaseFeat"/> that makes you Master in this lore.
    /// </summary>
    public Feat Master { get; set; } = null!;

    /// <summary>
    /// The <see cref="SkillIncreaseFeat"/> that makes you Legendary in this lore.
    /// </summary>
    public Feat Legendary { get; set; } = null!;

    /// <summary>
    /// When the THINKER attempts to Recall a Weakness on the TARGET, this returns WHETHER this lore applies to that check.
    /// </summary>
    /// <para>
    /// If the function is added onto, such as with a `+=` assignment, then the first function to return true will apply to the creature.
    /// </para>
    public Func<Creature,Creature,bool>? ValidRecallTarget { get; set; }

    #endregion

    /// <summary>
    /// Don't create a lore directly. Instead, use <see cref="Lores.RegisterNewLore"/>.
    /// </summary>
    internal Lore(string name, string description, Ability relevantAbility, bool isSpecific, bool isHidden, Func<Creature,Creature,bool>? validRecallTarget)
    {
        this.Name = name;
        this.Description = description;
        this.Skill = ModManager.TryParse(name, out Skill alreadyRegistered)
            ? alreadyRegistered
            : ModManager.RegisterEnumMember<Skill>(name);
        this.Trait = ModManager.RegisterTrait(name, new TraitProperties(name, true));
        this.IsSpecific = isSpecific;
        this.IsHidden = isHidden;
        this.WithAbility(relevantAbility);
        this.ValidRecallTarget = validRecallTarget;
    }

    #region Instance Functions

    /// <summary>
    /// Add extra functionality, such as from another mod extending this lore.
    /// </summary>
    /// <param name="modName">The humanized name of the mod that is extending this lore. This will appear in bold at the start of a new line in the lore's skill description.</param>
    /// <param name="addedUsage">A description of the functionality being added, such as to state that certain feats grant additional benefits.</param>
    /// <param name="validRecallTarget">(Optional) An extra means of using this lore to Recall Weakness on a creature.</param>
    /// <returns></returns>
    public Lore WithExtraFunctionality(
        string modName,
        string addedUsage,
        Func<Creature, Creature, bool>? validRecallTarget)
    {
        this.WithNewDescription(this.Description + $"\n\n{{b}}{modName}{{/b}} {addedUsage}");
        if (validRecallTarget is not null)
            this.WithExtraRecallTarget(validRecallTarget);
        return this;
    }

    /// <summary>
    /// Replaces <see cref="ValidRecallTarget"/> with a new function. If you want to add a function instead of removing it, use <see cref="WithExtraRecallTarget"/>.
    /// </summary>
    public Lore WithRecallTarget(Func<Creature, Creature, bool> validRecallTarget)
    {
        this.ValidRecallTarget = validRecallTarget;
        return this;
    }

    /// <summary>
    /// Adds a new function to <see cref="ValidRecallTarget"/>, offering another method for a creature to be a valid target for Recall Weakness that might not normally exist for a lore that's already been registered. To replace the function instead, use <see cref="WithRecallTarget"/>.
    /// </summary>
    public Lore WithExtraRecallTarget(Func<Creature, Creature, bool> validRecallTarget)
    {
        this.ValidRecallTarget += validRecallTarget;
        return this;
    }

    /// <summary>
    /// Replaces <see cref="Description"/> with a new string. This also edits all existing <see cref="SkillSelectionFeat"/>s to reflect the new change.
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    public Lore WithNewDescription(string description)
    {
        foreach (SkillSelectionFeat skillFeat in AllFeats.All
                     .Select(ft => ft as SkillSelectionFeat)
                     .WhereNotNull()
                     .ToList())
            if (skillFeat.Skill == this.Skill)
                skillFeat.RulesText = skillFeat.RulesText.Replace(this.Description, description);
        this.Description = description;
        return this;
    }
    
    /// <summary>
    /// Updates this Lore to use a new ability.
    /// </summary>
    public Lore WithAbility(Ability ability)
    {
        // Use reflection to add this lore's associated ability to a hidden dictionary
        Type skills = typeof(Skills);
        var myObject = new Skills();
        FieldInfo? relField = skills.GetField("relevantAbility", BindingFlags.Static | BindingFlags.NonPublic);
        if (relField != null)
        {
            if (relField.GetValue(myObject) is not IDictionary<Skill, Ability> dict)
            {
                dict = new Dictionary<Skill, Ability>();
                relField.SetValue(myObject, dict);
            }
            dict[this.Skill] = ability;
        }
        
        this.RelevantAbility = ability;
        
        return this;
    }

    #endregion
}

#region Harmony Patches for extra skills

[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillDescription))]
internal static class PatchSkillDescription
{
    // ReSharper disable once InconsistentNaming
    internal static bool Prefix(Skill skill, ref string __result)
    {
        if (Lores.AllLores.FirstOrDefault(lore1 => lore1.Skill == skill) is not { } lore2)
            return true;
        
        __result = lore2.Description;
        return false;
    }
}

[HarmonyPatch(typeof(Skills), nameof(Skills.SkillToTrait))]
internal static class PatchSkillToTrait
{
    // ReSharper disable once InconsistentNaming
    internal static bool Prefix(Skill skill, ref Trait __result)
    {
        if (Lores.AllLores.FirstOrDefault(lore1 => lore1.Skill == skill) is not { } lore2)
            return true;
        
        __result = lore2.Trait;
        return false;
    }
}

[HarmonyPatch(typeof(Skills), nameof(Skills.TraitToSkill))]
internal static class PatchTraitToSkill
{
    // ReSharper disable once InconsistentNaming
    internal static bool Prefix(Trait skill, ref Skill? __result)
    {
        if (Lores.AllLores.FirstOrDefault(lore1 => lore1.Trait == skill) is not { } lore2)
            return true;
        
        __result = lore2.Skill;
        return false;
    }
}

[HarmonyPatch(typeof(Skills))]
[HarmonyPatch(nameof(Skills.SkillToFeat), typeof(Skill))]
internal static class PatchSkillToFeat
{
    // ReSharper disable once InconsistentNaming
    internal static bool Prefix(Skill skill, ref FeatName __result)
    {
        if (Lores.AllLores.FirstOrDefault(lore1 => lore1.Skill == skill) is not { } lore2)
            return true;
        if (lore2.IsHidden)
            throw new InvalidOperationException("Tried to convert a Lore's Skill to a FeatName in Skills.SkillToFeat(Skill). Hidden lores do not register their feats, so their FeatNames can't be used directly. Try increasing proficiency directly, using Lores.TrainInThisOrSubstitute(this CalculatedCharacterSheetValues, Lore), or using CalculatedCharacterSheetValues.AddFeat(Feat, Feat?) instead.");
        
        __result = lore2.Trained.FeatName;
        return false;
    }
}

[HarmonyPatch(typeof(Skills))]
[HarmonyPatch(nameof(Skills.SkillToFeat), typeof(Skill), typeof(Proficiency))]
internal static class PatchSkillToFeat2
{
    // ReSharper disable once InconsistentNaming
    internal static bool Prefix(Skill skill, Proficiency proficiency, ref FeatName __result)
    {
        if (Lores.AllLores.FirstOrDefault(lore1 => lore1.Skill == skill) is not { } lore2)
            return true;
        if (lore2.IsHidden)
            throw new InvalidOperationException("Tried to convert a Lore's Skill to a FeatName in Skills.SkillToFeat(Skill, Proficiency). Hidden lores do not register their feats, so their FeatNames can't be used directly. Try increasing proficiency directly, using Lores.TrainInThisOrSubstitute(this CalculatedCharacterSheetValues values, Lore lore), or using CalculatedCharacterSheetValues.AddFeat(Feat mainFeat, Feat? subfeat) instead.");

        __result = proficiency switch
        {
            Proficiency.Trained => lore2.Trained.FeatName,
            Proficiency.Expert => lore2.Expert.FeatName,
            Proficiency.Master => lore2.Master.FeatName,
            Proficiency.Legendary => lore2.Legendary.FeatName,
            _ => throw new ArgumentOutOfRangeException(nameof(proficiency), proficiency, null)
        };
        return false;
    }
}

#endregion