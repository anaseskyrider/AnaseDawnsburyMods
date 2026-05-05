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
    /// The list of all the Lore skills that have been registered.
    /// </summary>
    public static readonly List<Lore> AllLores = [];
    /// <summary>
    /// Gets from <see cref="AllLores"/>, the Lores that have been registered and that are public (<see cref="Lore.IsHidden"/> is false).
    /// </summary>
    public static IReadOnlyList<Lore> AllPublicLores => AllLores.Where(lore => !lore.IsHidden).ToList();
    /// <summary>
    /// An invisible unicode symbol that's added to the beginning of the humanized name of feats in order to push them to the bottom of the list.
    /// </summary>
    public const string DisplayOffset = "𝒵";

    /*public static FeatName TrainedLoreCategory;
    public static FeatName ExpertLoreCategory;
    public static FeatName MasterLoreCategory;
    public static FeatName LegendaryLoreCategory;*/

    #region Loading Procedures

    internal static void Load()
    {
        /*TrainedLoreCategory = ModData.SafelyRegister<FeatName>(
            "Lores",
            "Trained Lores");
        ExpertLoreCategory = ModData.SafelyRegister<FeatName>(
            "ExpertLores",
            "Expert Lores");
        MasterLoreCategory = ModData.SafelyRegister<FeatName>(
            "MasterLores",
            "Master Lores");
        LegendaryLoreCategory = ModData.SafelyRegister<FeatName>(
            "LegendaryLores",
            "Legendary Lores");*/
        
        foreach (Feat ft in CreateFeats())
            ModManager.AddFeat(ft, ModData.Traits.ModName);
        RegisterLores();
        AdjustFeatsAndFeatures();
        
        // Optional Dependency: New Skill Feats and Items
        if (ModManager.TryParse("Assurance", out FeatName _))
            OptionalDependencies.LoadAssuranceLores();
    }

    internal static IEnumerable<Feat> CreateFeats()
    {
        /*// Trained Category
        yield return NewCategoryLore(TrainedLoreCategory, Proficiency.Trained);
        // Expert Category
        yield return NewCategoryLore(ExpertLoreCategory, Proficiency.Expert);
        // Master Category
        yield return NewCategoryLore(MasterLoreCategory, Proficiency.Master);
        // Legendary Category
        yield return NewCategoryLore(LegendaryLoreCategory, Proficiency.Legendary);*/
        
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

        /*Feat NewCategoryLore(FeatName featName, Proficiency prof)
        {
            /*Feat category = prof > Proficiency.Trained
                ? new SkillIncreaseFeat(featName, Skill.Athletics, Trait.Athletics, prof, null)
                : new SkillSelectionFeat(featName, Skill.Athletics, Trait.Athletics);#1#
            Feat category = prof > Proficiency.Trained
                ? new SkillIncreaseFeat(featName, Skill.Athletics, Trait.Athletics, prof, null)
                : new SkillSelectionFeat(featName, Skill.Athletics, Trait.Athletics);
            category.OnSheet = null;
            category.OnCreature = null;
            category.RulesText = "Choose a Lore skill to become " + prof.HumanizeLowerCase2() + " in.";
            category.CanSelectMultipleTimes = true;
            category.Subfeats = [];
            return category;
        }*/
    }

    internal static void RegisterLores()
    {
        // Warfare Lore
        RegisterNewLore(
            "Warfare Lore",
            $"""
             You have studied battlefields, tactics, and strategy.

             You can use this skill to {RecallWeakness.GetActionLink()} on martial creatures (wields martial weapons; or has a Reactive Strike or Shield Block feature).
             """,
            (_, target) =>
                // TODO: Does not work. ORC RANGER to test.
                target.ItemsHeldAtTheBeginningOfTheEncounter.Any(item =>
                    item.HasAnyTraits([Trait.Advanced, Trait.Martial]))
                || target.HasEffect(QEffectId.ShieldBlock)
                || target.HasEffect(QEffectId.AttackOfOpportunity));
        
        // Undead Lore
        RegisterNewLore(
            "Undead Lore",
            $"""
             You have studied the nature of undead and the dark energies that animate their flesh and bind their souls to the material plane.

             You can use this skill to {RecallWeakness.GetActionLink()} on undead creatures.
             """,
            (_, target) =>
                target.HasTrait(Trait.Undead));
        
        // Elemental Lore
        RegisterNewLore(
            "Elemental Lore",
            $"""
             You have studied creatures from the elemental planes.

             You can use this skill to {RecallWeakness.GetActionLink()} on elemental creatures.
             """,
            (_, target) =>
                target.HasTrait(Trait.Elemental));
        
        // Starborn Lore
        RegisterNewLore(
            "Starborn Lore",
            $"""
             You have studied the starborn — the seven commanders of the demonic armada that has waged war on us for seven years. You read through every report that's come out of the Western Reaches and you learned everything there is to know — from their origins in the distant worlds they've scoured clean, through the strategy and tactics they employ on the battlefield, to their innate weaknesses which could be their downfall.

             You can use this skill to {RecallWeakness.GetActionLink()} on starborn creatures.
             """,
            (_, target) =>
                target.HasTrait(Trait.Starborn),
            true);
        
        // Maritime Lore (credits: SilchasRuin, Junabell)
        RegisterNewLore(
            "Maritime Lore",
            $"""
            You have studied the seas and waterways of the world, learning about the creatures that call them home.

            You can use this skill to {RecallWeakness.GetActionLink()} on amphibious, aquatic, and water creatures.
            """,
            (_, target) => target.Traits.ContainsOneOf([Trait.Amphibious, Trait.Aquatic, Trait.Water]));
    }

    internal static void AdjustFeatsAndFeatures()
    {
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
            foreach (SkillSelectionFeat sFeat in AllFeats.All
                         .Select(ft => ft as SkillSelectionFeat)
                         .WhereNotNull())
                AdjustFeat(sFeat, sFeat.Skill);
            
            foreach (Lore lore in AllLores)
                AdjustFeat(lore.Trained, lore.Skill);

            return;

            void AdjustFeat(Feat feat, Skill skill)
            {
                string name = skill.ToStringOrTechnical();
                string ability = skill.ToAbility().ToStringOrTechnical();
                Lore? lore = feat.Tag as Lore;
                
                string trainedIn = "You become trained in " + name + ".";
                if (feat.RulesText.IndexOf(trainedIn) != -1
                    && string.IsNullOrEmpty(feat.FlavorText))
                {
                    feat.RulesText = feat.RulesText.Replace(trainedIn + "\n\n", "");
                    feat.FlavorText = trainedIn;
                }

                string basedSkill = "This is " + ability.WithIndefiniteArticle() + "-based skill.";
                string addMod = "{i}(You add your " + ability + " modifier to checks using this skill.){/i}";
                if (feat.RulesText.IndexOf(basedSkill) != -1
                    && feat.RulesText.IndexOf(addMod) != -1)
                {
                    feat.RulesText = feat.RulesText.Replace(
                        basedSkill + " " + addMod,
                        $"{{b}}Ability{{/b}} {ability} {{i}}(add your {ability} modifier to checks with this skill){{/i}}"
                        + (lore is not null
                            ? "\n{b}Lore{/b} " + (lore.IsSpecific
                                ? "Specific {i}(add a +5 bonus to checks with this lore){/i}"
                                : "Unspecific {i}(add a +2 bonus to checks with this lore){/i}")
                            : null));
                }

                feat.RulesText = feat.RulesText.Replace(
                    "{b}Trick Magic Item{/b}",
                    AllFeats.GetFeatByFeatName(FeatName.TrickMagicItem)
                        .ToLink("Trick Magic Item"));

                feat.RulesText = feat.RulesText.Replace(
                    "{b}Battle Medicine{/b}",
                    AllFeats.GetFeatByFeatName(FeatName.BattleMedicine)
                        .ToLink("Battle Medicine"));
            }
        };
    }

    #endregion

    #region User Functions

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
    /// Works as <see cref="CalculatedCharacterSheetValues.TrainInThisOrSubstitute"/>, and you can determine if the substitution must be a Lore or can be any skill.
    /// </summary>
    /// <param name="values">This character sheet.</param>
    /// <param name="lore">The Lore to train in.</param>
    /// <param name="mustSubLore">Whether the substituted skill must be a lore skill.</param>
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
    /// Registers a new lore skill.
    /// </summary>
    /// <remarks>
    /// If you wish to find or modify a lore you know is already registered, use <see cref="GetRegisteredLore"/>.
    /// </remarks>
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
        
        // Add to the list before patched-functions try to find your lore.
        AllLores.Add(newLore);

        // Add feats which increase proficiency
        newLore.Trained = RegisterSkillFeat(newLore, Proficiency.Trained, null);
        newLore.Expert = RegisterSkillFeat(newLore, Proficiency.Expert, newLore.Trained.FeatName);
        newLore.Master = RegisterSkillFeat(newLore, Proficiency.Master, newLore.Expert.FeatName);
        newLore.Legendary = RegisterSkillFeat(newLore, Proficiency.Legendary, newLore.Master.FeatName);
        
        // Add to Additional Lore
        if (!newLore.IsHidden)
            RegisterAdditionalLoreSubfeat(newLore);
        
        return newLore;
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

    #endregion

    #region Private Functions
        
    /// <summary>
    /// Create a skill-increasing feat.
    /// </summary>
    internal static Feat RegisterSkillFeat(Lore lore, Proficiency prof, FeatName? previous)
    {
        if (prof > Proficiency.Trained && previous is null)
            throw new Exception("Proficiency cannot be greater than Trained without a previous feat it's increased from.");

        string technicalName = lore.Name;
        string displayName = lore.Name;
        string featDescription;
        FeatGroup group;
        if (prof > Proficiency.Trained)
        {
            technicalName = prof.ToStringOrTechnical() + technicalName;
            displayName = DisplayOffset + prof.ToStringOrTechnical() + " in " + displayName;
            featDescription = IncreaseDescription(lore.Skill);
            group = prof switch
            {
                Proficiency.Expert => FeatGroup.SkillExpertise,
                Proficiency.Master => FeatGroup.SkillMastery,
                Proficiency.Legendary => FeatGroup.SkillLegend,
                _ => throw new ArgumentOutOfRangeException(nameof(prof), prof, null)
            };
        }
        else
        {
            displayName = DisplayOffset + displayName;
            featDescription = SelectionDescription(lore.Skill);
            group = FeatGroup.SkillTraining;
        }
        FeatName featName = ModManager.TryParse(technicalName, out FeatName fn)
            ? fn
            : ModManager.RegisterFeatName(technicalName, displayName);

        Feat skillFeat;
        if (lore.IsHidden)
        {
            skillFeat = new Feat(featName, "", featDescription, [ModData.Traits.Lore], null)
                .WithOnSheet(values => values.SetProficiency(lore.Trait, prof))
                .WithOnCreature((sheet, cr) =>
                    cr.Skills.Set(
                        lore.Skill,
                        sheet.FinalAbilityScores.TotalModifier(Skills.GetSkillAbility(lore.Skill)) +
                        sheet.GetProficiency(lore.Trait).ToNumber(cr.Level)));
            skillFeat.FeatGroup = group;
            if (previous.HasValue)
                skillFeat.WithPrerequisite(
                    values => values.HasFeat(previous.Value),
                    $"You must have {previous.HumanizeTitleCase2()}.");
        }
        else
        {
            skillFeat = prof == Proficiency.Trained
                ? AllFeats.All.FirstOrDefault(ft => ft is SkillSelectionFeat ssf && ssf.Skill == lore.Skill) ??
                  new SkillSelectionFeat(featName, lore.Skill, lore.Trait)
                : AllFeats.All.FirstOrDefault(ft => ft is SkillIncreaseFeat sif && sif.Skill == lore.Skill && sif.TargetProficiency == prof) ??
                  new SkillIncreaseFeat(featName, lore.Skill, lore.Trait, prof, previous);
            
            // Enforce DisplayOffset behavior even if the FeatName was already registered
            skillFeat.CustomName = displayName;
            
            skillFeat.Traits.Add(ModData.Traits.Lore);
        }

        skillFeat
            .WithIllustration(IllustrationName.NarratorBook)
            .WithTag(lore);
        
        // If it already existed, tag it as now being modified by LoresAndWeaknesses
        if (AllFeats.AlreadyExists(skillFeat.FeatName))
        {
            skillFeat.Traits.Insert(0, ModData.Traits.ModName);
            skillFeat.Traits.Remove(Trait.Mod);
        }
        else
        {
            ModManager.AddFeat(skillFeat, ModData.Traits.ModName);
            /*if (!lore.IsHidden)
                if (prof == Proficiency.Trained)
                    AllFeats.GetFeatByFeatName(Lores.TrainedLoreCategory).Subfeats!.Add(skillFeat);
                else if (prof == Proficiency.Expert)
                    AllFeats.GetFeatByFeatName(Lores.ExpertLoreCategory).Subfeats!.Add(skillFeat);
                else if (prof == Proficiency.Master)
                    AllFeats.GetFeatByFeatName(Lores.MasterLoreCategory).Subfeats!.Add(skillFeat);
                else if (prof == Proficiency.Legendary)
                    AllFeats.GetFeatByFeatName(Lores.LegendaryLoreCategory).Subfeats!.Add(skillFeat);*/
        }

        return skillFeat;

        string SelectionDescription(Skill skill) =>
            $$"""
              You become trained in {{skill.HumanizeTitleCase2()}}.

              This is {{Skills.GetSkillAbility(skill).ToString().WithIndefiniteArticle()}}-based skill. {i}(You add your {{Skills.GetSkillAbility(skill).ToString()}} modifier to checks using this skill.){/i}

              {{Skills.GetSkillDescription(skill)}}
              """;

        string IncreaseDescription(Skill skill) =>
            $"""
             You become {prof.HumanizeLowerCase2()} in {skill.HumanizeTitleCase2()}, which increases your proficiency bonus to {skill.HumanizeTitleCase2()} skill checks by an additional +2.

             {Skills.GetSkillDescription(skill)}
             """;
    }

    /// <summary>
    /// Creates and adds to Additional Lore the subfeat that governs this Lore skill.
    /// </summary>
    internal static Feat RegisterAdditionalLoreSubfeat(Lore lore)
    {
        Feat additionalSubFeat = new Feat(
                ModManager.TryParse(ModData.IdPrepend + "AdditionalLore." + lore.Name, out FeatName addLore)
                    ? addLore
                    : ModManager.RegisterFeatName(
                        ModData.IdPrepend + "AdditionalLore." + lore.Name,
                        DisplayOffset + lore.Name),
                "", "", [], null)
            .WithIllustration(IllustrationName.NarratorBook)
            .WithTag(lore)
            .WithOnSheet(values =>
            {
                values.GrantFeat(lore.Trained.FeatName);
                values.AddAtLevel(
                    3,
                    v3 =>
                        v3.GrantFeat(lore.Expert.FeatName));
                values.AddAtLevel(
                    7,
                    v7 =>
                        v7.GrantFeat(lore.Master.FeatName));
                values.AddAtLevel(
                    15,
                    v15 =>
                        v15.GrantFeat(lore.Legendary.FeatName));
            });
        additionalSubFeat.WithPrerequisite(
            values =>
                values.HasFeat(additionalSubFeat)
                || values.GetProficiency(lore.Trait) < Proficiency.Legendary,
            "You are already legendary in this Lore.");
        additionalSubFeat.WithRulesTextCreator(sheet =>
        {
            // Don't inform the user that they're trained and can still take the feat
            // if they're legendary (feat is useless),
            // if they're untrained (they aren't trained),
            // if their training comes from this feat.
            if (sheet.Calculated.GetProficiency(lore.Trait) is var loreProf
                && loreProf is Proficiency.Legendary or Proficiency.Untrained
                || sheet.Calculated.AllFeats.Any(ft => ft == additionalSubFeat))
                return lore.Trained.RulesText;

            return lore.Trained.RulesText +
                   $"\n\n{{icon:YellowWarning}} You are already {loreProf.HumanizeLowerCase2()} in this lore. You can still take this feat and gain automatic increases.";
        });
        
        // Enforce DisplayOffset behavior even if the FeatName was already registered
        additionalSubFeat.CustomName = DisplayOffset + lore.Name;
        
        ModManager.AddFeat(additionalSubFeat, ModData.Traits.ModName);
        AllFeats.GetFeatByFeatName(RecallWeakness.FNAdditionalLore)
            .Subfeats
            !.Add(additionalSubFeat);
        
        return additionalSubFeat;
    }

    #endregion
}

/// <summary>
/// Each instance is a collection of related information to a lore, such as its string name, skill enum, the skill's in-game description, and so on.
/// </summary>
public class Lore
{
    // Assign these values on instance creation. Can safely use set accessor after initial registration.
    private bool _isHidden;
    private string _description;

    #region Instance Data

    /// <summary>
    /// Gets the name of the lore.
    /// </summary>
    /// <remarks>
    /// Lore names should always include the word Lore with spaces, such as "Warfare Lore". This string is both the humanized name and technical name of the lore, and is equal to the output of <see cref="ModManager.ToStringOrTechnical(Dawnsbury.Core.Mechanics.Enumerations.Skill)"/>.
    /// </remarks>
    public string Name { get; }
    
    /// <summary>
    /// Gets or sets the description of the lore.
    /// </summary>
    /// <remarks>
    /// A good description should tell you what creatures it can Recall Weaknesses for, or none if it doesn't interact with creatures on its own. Setting this value also edits <see cref="Trained"/> to reflect the new change.
    /// </remarks>
    public string Description {
        get => _description;
        set
        {
            this.Trained.RulesText = this.Trained.RulesText.Replace(_description, value);
            _description = value;
        }}

    /// <summary>
    /// Gets or sets the ability that's used when making checks to Recall Weakness with this skill.
    /// </summary>
    /// <remarks>
    /// Most Lore skills use Intelligence.
    /// </remarks>
    public Ability RelevantAbility {
        get;
        set
        {
            if (field == value)
                return;
            
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
                dict[this.Skill] = value;
            }
        
            field = value;
        }}

    /// <summary>
    /// Gets or sets whether this Lore is specific.
    /// </summary>
    /// <remarks>
    /// Most lores are unspecific. Lores reduce the DC to checks to Recall Weakness by 5, while unspecific lores reduce them by 2.
    /// </remarks>
    public bool IsSpecific { get; set; }
    
    /// <summary>
    /// Gets or sets whether this lore is hidden from standard skill selections.
    /// </summary>
    /// <remarks>
    /// If true, this lore can only be granted from a feature directly, such as with the variant overload, <see cref="Lores.TrainInThisOrSubstitute"/>.
    /// </remarks>
    public bool IsHidden {
        get => _isHidden;
        set
        {
            if (_isHidden == value)
                return;
        
            if (value) // this lore is hidden
            {
                /*AllFeats.GetFeatByFeatName(Lores.TrainedLoreCategory).Subfeats!.Remove(this.Trained);
                AllFeats.GetFeatByFeatName(Lores.ExpertLoreCategory).Subfeats!.Remove(this.Expert);
                AllFeats.GetFeatByFeatName(Lores.MasterLoreCategory).Subfeats!.Remove(this.Master);
                AllFeats.GetFeatByFeatName(Lores.LegendaryLoreCategory).Subfeats!.Remove(this.Legendary);*/
                AllFeats.GetFeatByFeatName(RecallWeakness.FNAdditionalLore).Subfeats!.RemoveAll(ft => ft.Tag == this);
            }
            else // this lore is not hidden
            {
                /*AllFeats.GetFeatByFeatName(Lores.TrainedLoreCategory).Subfeats!.Add(this.Trained);
                AllFeats.GetFeatByFeatName(Lores.ExpertLoreCategory).Subfeats!.Add(this.Expert);
                AllFeats.GetFeatByFeatName(Lores.MasterLoreCategory).Subfeats!.Add(this.Master);
                AllFeats.GetFeatByFeatName(Lores.LegendaryLoreCategory).Subfeats!.Add(this.Legendary);*/
                Lores.RegisterAdditionalLoreSubfeat(this);
            }

            _isHidden = value;
            
            this.Trained = Lores.RegisterSkillFeat(this, Proficiency.Trained, null);
            this.Expert = Lores.RegisterSkillFeat(this, Proficiency.Expert, this.Trained.FeatName);
            this.Master = Lores.RegisterSkillFeat(this, Proficiency.Master, this.Expert.FeatName);
            this.Legendary = Lores.RegisterSkillFeat(this, Proficiency.Legendary, this.Master.FeatName);
        }}
    
    /// <summary>
    /// Gets the registered <see cref="Skill"/> enum associated with this lore.
    /// </summary>
    public Skill Skill { get; }
    
    /// <summary>
    /// Gets the <see cref="Trait"/> enum associated with this lore.
    /// </summary>
    public Trait Trait { get; }
    
    /// <summary>
    /// Gets or sets the <see cref="Feat"/> that trains you in this lore.
    /// </summary>
    public Feat Trained { get; internal set; } = null!;

    /// <summary>
    /// Gets or sets the <see cref="Feat"/> that makes you Expert in this lore.
    /// </summary>
    public Feat Expert { get; internal set; } = null!;

    /// <summary>
    /// Gets or sets the <see cref="Feat"/> that makes you Master in this lore.
    /// </summary>
    public Feat Master { get; internal set; } = null!;

    /// <summary>
    /// Gets or sets the <see cref="Feat"/> that makes you Legendary in this lore.
    /// </summary>
    public Feat Legendary { get; internal set; } = null!;

    /// <summary>
    /// (Get;set;) When the THINKER attempts to Recall a Weakness on the TARGET, this returns WHETHER this lore applies to that check.
    /// </summary>
    /// <remarks>
    /// If the function is added onto, such as with a `+=` assignment, then the first function to return true will apply to the creature.
    /// </remarks>
    public Func<Creature,Creature,bool>? ValidRecallTarget { get; set; }

    #endregion

    /// <summary>
    /// Don't create a lore directly. Instead, use <see cref="Lores.RegisterNewLore"/>.
    /// </summary>
    internal Lore(string name, string description, Ability relevantAbility, bool isSpecific, bool isHidden, Func<Creature,Creature,bool>? validRecallTarget)
    {
        this.Name = name;
        this._description = description;
        this.Skill = ModManager.TryParse(name, out Skill alreadyRegistered)
            ? alreadyRegistered
            : ModManager.RegisterEnumMember<Skill>(name);
        this.Trait = ModManager.RegisterTrait(name, new TraitProperties(name, true));
        this.IsSpecific = isSpecific;
        this._isHidden = isHidden;
        this.RelevantAbility = relevantAbility;
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
        this.Description = this.Description + $"\n\n{{b}}{modName}{{/b}} {addedUsage}";
        if (validRecallTarget is not null)
            this.ValidRecallTarget += validRecallTarget;
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
    /// Replaces <see cref="Description"/> with a new string.
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    public Lore WithNewDescription(string description)
    {
        this.Description = description;
        return this;
    }
    
    /// <summary>
    /// Updates this Lore to use a new ability.
    /// </summary>
    public Lore WithAbility(Ability ability)
    {
        this.RelevantAbility = ability;
        return this;
    }

    /// <summary>
    /// Sets whether the Lore is hidden or not.
    /// </summary>
    public Lore WithHiddenState(bool isHidden)
    {
        this.IsHidden = isHidden;
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