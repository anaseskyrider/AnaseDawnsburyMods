using System.Linq.Expressions;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

// TODO: Stacking DC penalties for repeat Aid.

/// <summary>
/// Contains all the logic for the Aid basic action. Any modder looking to make a feature that's compatible with Aiding should look for the <see cref="ModData.ActionIds.PrepareToAid"/> and <see cref="ModData.ActionIds.AidReaction"/> action IDs. By using <see cref="ModManager.TryParse(string technicalName, out T enumValue)"/>, it will be compatible regardless of load order. See <see cref="MoreBasicActions.LoadMod()"/> for usable code.
/// </summary>
public static class Aid
{
    public static readonly string BasicPrepareToAidDescription = "{i}You prepare to help your ally with a task outside your turn.{/i}\n\nChoose an adjacent ally or enemy. When that ally makes a skill check or attack roll while adjacent to you, or that enemy is targeted by an attack roll while adjacent to you, you can use the aid {icon:Reaction} reaction for that check as the trigger.";
    
    public static string BasicAidReactionDescription =>
        "{b}Aid{b} {icon:Reaction}\n{b}Trigger{/b} An ally is about to attempt a check, and you prepared to aid that ally's check.\n{b}Effect{/b} Attempt the same check you prepared to aid with a DC of " +
        AidDC() + "." + S.FourDegreesOfSuccess(
            "You grant your ally a +2 circumstance bonus to the triggering check. The bonus increases to +3 if you're a master with the check, or +4 if you're legendary.",
            "You grant your ally a +1 circumstance bonus to the triggering check.",
            "No effect.",
            "Your ally takes a -1 circumstance penalty to the triggering check.");

    public static void LoadAid()
    {
        // Add Prepare to Aid to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect aidLoader = new QEffect()
            {
                Name = "AidLoader",
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    PossibilitySectionId sectionId =
                        PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.AidInSubmenus)
                            ? PossibilitySectionId.OtherManeuvers
                            : PossibilitySectionId.SkillActions;
                    if (section.PossibilitySectionId != sectionId)
                        return null;
                    
                    SubmenuPossibility aidMenu = new SubmenuPossibility(
                        ModData.Illustrations.Aid,
                        "Prepare to Aid")
                    {
                        SubmenuId = ModData.SubmenuIds.PrepareToAid,
                        Subsections =
                        {
                            new PossibilitySection("Skill checks")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.AidSkills,
                                Possibilities = CreatePrepareToAidSkills(cr),
                            },
                            new PossibilitySection("Attack rolls")
                            {
                                PossibilitySectionId = ModData.PossibilitySectionIds.AidAttacks,
                                Possibilities = CreatePrepareToAidAttacks(cr),
                            },
                        },
                        SpellIfAny = new CombatAction(
                            cr,
                            ModData.Illustrations.Aid,
                            "Prepare to Aid",
                            [],
                            BasicPrepareToAidDescription+"\n\n"+BasicAidReactionDescription,
                            Target.AdjacentCreature()),
                    };

                    return aidMenu;
                },
            };
            
            cr.AddQEffect(aidLoader);
        });
        
        TrueFeat cooperativeNature = new TrueFeat(
            ModData.FeatNames.CooperativeNature,
            1,
            "The short human life span lends perspective and has taught you from a young age to set aside differences and work with others to achieve greatness.",
            "You gain a +4 circumstance bonus on checks to Aid {icon:Reaction}.",
            [ModData.Traits.MoreBasicActions, Trait.Human])
            .WithPermanentQEffect("You have a permanent +4 circumstance bonus on checks to Aid {icon:Reaction}.", qfFeat =>
            {
                qfFeat.BonusToAttackRolls = (qfThis, action, defender) =>
                {
                    if (action.Name.Contains("Aid Strike") || action.ActionId == ModData.ActionIds.AidReaction)
                        return new Bonus(4, BonusType.Circumstance, "Cooperative Nature");

                    return null;
                };
            });
        ModManager.AddFeat(cooperativeNature);
    }

    public static List<Possibility> CreatePrepareToAidSkills(Creature owner)
    {
        List<Possibility> possibilities = [];
        foreach (Skill skill in Skills.AllSkills)
        {
            if (!PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.UntrainedAid) && owner.Proficiencies.Get(Skills.SkillToTrait(skill)) == Proficiency.Untrained)
                continue;
            ActionPossibility? skillAid = CreatePrepareToAidPossibility(owner, skill);
            if (skillAid != null)
            {
                skillAid.Caption = skill.ToString();
                possibilities.Add(skillAid);
            }
        }
        return possibilities;
    }
    
    public static List<Possibility> CreatePrepareToAidAttacks(Creature owner)
    {
        List<Possibility> possibilities = [];
        ActionPossibility? attackAid = CreatePrepareToAidPossibility(owner);
        if (attackAid != null)
        {
            attackAid.Caption = "Attack roll";
            possibilities.Add(attackAid);
        }
        return possibilities;
    }
    
    public static ActionPossibility? CreatePrepareToAidPossibility(Creature owner, Skill? skill = null)
    {
        CombatAction? prepareToAidAction = CreatePrepareToAidAction(owner, skill);
        if (prepareToAidAction == null)
            return null;
        ActionPossibility possibility = new ActionPossibility(prepareToAidAction, PossibilitySize.Full);
        return possibility;

    }
    
    /// <summary>
    /// Creates a "Prepare to Aid" CombatAction. If no skill is given, the check being aided is an Attack roll, and the owner's Strike possibility with the highest proficiency rank is used to aid it.
    /// </summary>
    /// <param name="owner">The creature preparing to aid.</param>
    /// <param name="skill">(nullable) The skill to aid, and to roll to attempt to aid.</param>
    /// <returns></returns>
    public static CombatAction? CreatePrepareToAidAction(Creature owner, Skill? skill = null)
    {
        // Initialize vars
        string checkName;
        Proficiency rank;
        CombatAction? mostProficientAttack = null;
        CombatAction prepareToAidAction = new CombatAction(
                owner,
                IllustrationName.Action,
                "INCOMPLETE TEXT",
                [Trait.DoNotShowOverheadOfActionName, Trait.DoNotShowInContextMenu, Trait.Basic],
                "INCOMPLETE TEXT",
                skill != null ? Target.AdjacentFriend() : Target.AdjacentCreature())
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.PrepareToAid)
            .WithSoundEffect(SfxName.OpenPage);
        
        // Skill or attack check
        if (skill != null)
        {
            checkName = skill.HumanizeTitleCase2();
            rank = owner.Proficiencies.Get(Skills.SkillToTrait((Skill)skill));
            prepareToAidAction = prepareToAidAction
                .WithTag(skill);
        }
        else if (owner.Possibilities != null &&
                 owner.Possibilities
                     .Filter(ap => ap.CombatAction.HasTrait(Trait.Strike))
                     .CreateActions(false) is { Count: > 0 } strikeList)
        {
            Proficiency highestAnyProficiency = strikeList
                .Max(ica => owner.Proficiencies.Get(ica.Action.Item?.Traits ?? []));
            List<CombatAction> mostProficientAttacks = strikeList
                .FindAll(ica =>
                    owner.Proficiencies.Get(ica.Action.Item?.Traits ?? []) == highestAnyProficiency &&
                    ica.Action.ActiveRollSpecification != null)
                .Select(ica => ica.Action)
                .ToList();
            mostProficientAttack = mostProficientAttacks.FirstOrDefault();
            if (mostProficientAttack != null && mostProficientAttack.Item != null)
            {
                rank = highestAnyProficiency;
                checkName = "Attack";
                prepareToAidAction = prepareToAidAction
                    .WithTag(mostProficientAttack);
            }
            else
                return null;
        }
        else
            return null;
        
        // Fill out the rest
        prepareToAidAction.Name = $"Prepare to Aid ({checkName})";
        prepareToAidAction.Description = CreatePrepareToAidDescription(rank, checkName);
        prepareToAidAction = prepareToAidAction
            .WithTargetingTooltip((thisAction, target, index) =>
            {
                CombatAction checkBreakdownAction = CreateAidReaction(thisAction.Owner, thisAction.Tag!, CombatAction.CreateSimple(target, ""));
                CheckBreakdown breakdown = CombatActionExecution.BreakdownAttackForTooltip(checkBreakdownAction, target);
                string tooltip = breakdown.TooltipDescription;
                return tooltip;
            })
            .WithPrologueEffectOnChosenTargetsBeforeRolls(async (thisAction, aider, targets) =>
            {
                if (targets.ChosenCreature is not { } aidee)
                    return;

                bool isEnemy = !aider.FriendOf(aidee);

                QEffect preparedToAid = CreatePrepareToAidQEffect(aider, aidee, isEnemy, thisAction.Tag!);
                QEffect canBeAided = CreateAidQEffect(preparedToAid, isEnemy, thisAction.Tag!);
                
                aider.AddQEffect(preparedToAid);
                aidee.AddQEffect(canBeAided);
            });
        
        return prepareToAidAction;
    }

    public static QEffect CreatePrepareToAidQEffect(Creature aider, Creature aidee, bool isEnemy, object check)
    {
        string checkName = check is Skill aidSkill ? aidSkill.HumanizeTitleCase2()+" check" : "Attack roll";
            
        QEffect preparedEffect = new QEffect(
            "Prepared to Aid",
            $"If you're adjacent to {{Blue}}{aidee.Name}{{/Blue}} when they " + (isEnemy ? "are attacked," : $"make {AorAn(checkName)} {{Blue}}{checkName}{{/Blue}},") + " you can aid {icon:Reaction} their check.",
            ExpirationCondition.ExpiresAtStartOfYourTurn,
            aider,
            ModData.Illustrations.Aid)
        {
            Id = ModData.QEffectIds.PreparedToAid,
            Tag = check,
        };
        
        return preparedEffect;
    }

    public static QEffect CreateAidQEffect(QEffect preparation, bool isEnemy, object check)
    {
        QEffect aidEffect = new QEffect(
            "Recieving Aid",
            "[No Description Given]",
            ExpirationCondition.ExpiresAtStartOfSourcesTurn,
            preparation.Source,
            null)
        {
            Tag = preparation,
        };
        switch (isEnemy)
        {
            case true:
                aidEffect.YouAreTargeted = async (qfThis, aidableAction) =>
                {
                    if (
                        qfThis.Source is not { } aider2 // Aid provider must still exist
                        || aider2 == aidableAction.Owner // Aid provider cannot aid itself
                        || !qfThis.Owner.IsAdjacentTo(aider2) // Aid provider must be adjacent to enemy
                        || !aidableAction.HasTrait(Trait.Attack) // Must be an attack
                        || aidableAction.ActiveRollSpecification is not { } rollSpec // Must have a roll spec
                        || rollSpec.TaggedDetermineBonus.InvolvedSkill != null // Must not be a skill check
                        || aidableAction.ActionId == ModData.ActionIds.PrepareToAid // Must not a preparation action
                        || qfThis.Tag is not QEffect preparation2 // Source must actually still be preparing to aid that action
                    )
                        return;

                    if (await aider2.AskToUseReaction(
                            $"{{b}}Aid {{icon:Reaction}}{{/b}}\n{qfThis.Owner.Name} is about to attacked. Attempt to aid the attack roll?"))
                    {
                        await RollAidReaction(aider2, aidableAction.Owner, check, aidableAction);
                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        preparation2.ExpiresAt = ExpirationCondition.Immediately;
                    }
                };
                break;
            case false:
                aidEffect.BeforeYourActiveRoll = async (qfThis, aidableAction, defender) =>
                {
                    if (
                        qfThis.Source is not { } aider // Aid provider must still exist
                        || !qfThis.Owner.FriendOf(aider) // Self must be friend of source
                        || !qfThis.Owner.IsAdjacentTo(aider) // Self must be adjacent to aider
                        || qfThis.Tag is not QEffect preparation2
                        || (preparation2.Tag is Skill aidSkill &&
                            aidableAction.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill != aidSkill)
                        || (preparation2.Tag is CombatAction aidAttack && !aidAttack.HasTrait(Trait.Attack))
                        || preparation2.Tag == null
                    )
                        return;

                    string checkName = preparation2.Tag is Skill aidSkill2 ? aidSkill2.HumanizeTitleCase2()+ " check" : "Attack roll";

                    if (await aider.AskToUseReaction(
                            $"{{b}}Aid {{icon:Reaction}}{{/b}}\n{qfThis.Owner.Name} is about to make {AorAn(checkName)} {checkName}. Attempt to aid their check?"))
                    {
                        await RollAidReaction(aider, qfThis.Owner, check, aidableAction);
                        qfThis.ExpiresAt = ExpirationCondition.Immediately;
                        preparation2.ExpiresAt = ExpirationCondition.Immediately;
                    }
                };
                break;
        }

        return aidEffect;
    }

    /// <summary>
    /// Performs an Aid reaction.
    /// </summary>
    /// <param name="aider">The creature reacting.</param>
    /// <param name="aidee">The creature benefiting from the aid.</param>
    /// <param name="check">A Skill or a CombatAction for a strike (must have an ActiveRollSpecification)</param>
    /// <param name="aidableAction">The action being aided.</param>
    public static async Task<bool> RollAidReaction(Creature aider, Creature aidee, object check, CombatAction aidableAction)
    {
        // Target ally with skill check, or target enemy with attack check.
        Creature target = (check is Skill ? aidee : aidableAction.ChosenTargets.ChosenCreature) ?? aidee;
        return await aider.Battle.GameLoop.FullCast(CreateAidReaction(aider, check, aidableAction),
            ChosenTargets.CreateSingleTarget(target));
    }

    /// <summary>
    /// Not to be executed on its own. Instead, pass this as an argument to <see cref="CommonSpellEffects.RollCheck(CombatAction, Creature)"/>.
    /// </summary>
    /// <param name="aider">The creature taking the reaction.</param>
    /// <param name="check">The type of check being made to react with. Can only be of type Skill or CombatAction.</param>
    /// <param name="aidableAction">The specific action which is allowed to be aided. This is acquired at the moment of reaction.</param>
    /// <returns></returns>
    public static CombatAction CreateAidReaction(Creature aider, object check, CombatAction aidableAction)
    {
        CombatAction aidReaction;
        Proficiency rank;
        switch (check)
        {
            case Skill skill:
                rank = aider.Proficiencies.Get(Skills.SkillToTrait(skill));
                aidReaction = new CombatAction(
                        aider,
                        IllustrationName.None,
                        $"Aid ({skill.HumanizeTitleCase2()})",
                        [],
                        CreateAidReactionDescription(rank).Replace("{b}Aid{b} {icon:Reaction}\n", ""),
                        Target.AdjacentCreature())
                    .WithActionCost(0)
                    .WithActionId(ModData.ActionIds.AidReaction)
                    .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(skill), Checks.FlatDC(AidDC())));
                break;
            case CombatAction attack:
                if (attack.Item == null)
                    throw new NullReferenceException("aidableAction cannot have a null Item field");
                rank = aider.Proficiencies.Get(attack.Item.Traits);
                aidReaction = aider.CreateStrike(attack.Item)
                    .WithActionCost(0)
                    .WithExtraTrait(Trait.ReactiveAttack)
                    .WithActionId(ModData.ActionIds.AidReaction)
                    .WithItem(attack.Item)
                    .WithActiveRollSpecification(new ActiveRollSpecification(Checks.Attack(attack.Item, 0), Checks.FlatDC(AidDC())));
                    //.WithSoundEffect(aider.HasTrait(Trait.Female) ? SfxName.Intimidate : SfxName.MaleIntimidate);
                aidReaction.Name = $"Aid ({attack.Item.Name})";
                aidReaction.Description = CreateAidReactionDescription(rank).Replace("{b}Aid{b} {icon:Reaction}\n", "");
                aidReaction.ProjectileKind = ProjectileKind.None;
                aidReaction.ChosenTargets = aidableAction.ChosenTargets;
                aidReaction.EffectOnOneTarget = null;
                aidReaction.EffectOnChosenTargets = null;
                (aidReaction.Target as CreatureTarget)!.CreatureTargetingRequirements.RemoveAll(ctr => ctr is MeleeReachCreatureTargetingRequirement);
                break;
            default:
                throw new ArgumentException("Invalid check type");
        }
        
        aidReaction
            .WithSoundEffect(SfxName.Grapple) // BUG: doesn't seem to replace SFX.
            .WithEffectOnEachTarget(async (thisAction, aider, aidee, result) =>
        {
            if (result == CheckResult.Failure)
                return;
            
            int bonus = result switch
            {
                CheckResult.CriticalSuccess => CriticalBonusFromProficiency(rank),
                CheckResult.Success => 1,
                _ => -1 // Crit fail
            };
        
            aidableAction.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
            {
                BonusToAttackRolls = (qfThis, aidedAction, defender) =>
                    aidedAction == aidableAction
                        ? new Bonus(bonus, BonusType.Circumstance, $"Aid {result.HumanizeLowerCase2()}")
                        : null,
            });
        });
        
        return aidReaction;
    }
    
    /// <summary>
    /// Generates the full rules string for the Prepare to Aid action card.
    /// </summary>
    /// <param name="rank">The proficiency rank of the skill or attack being used when aiding.</param>
    /// <param name="checkName">The name of the skill, or the string "Attack".</param>
    /// <returns></returns>
    public static string CreatePrepareToAidDescription(Proficiency rank, string checkName)
    {
        bool isAttack = checkName is "Attack";
        string flavorText = "{i}You prepare to help your ally with a task outside your turn.{/i}";
        string rulesText = $"Choose an adjacent ally{(isAttack ? " or enemy" : null)}. When that ally makes {AorAn(checkName)} {{Blue}}{checkName}{{/Blue}} check while adjacent to you,{(isAttack ? " or that enemy is targeted by an attack while adjacent to you," : null)} you can use the aid {{icon:Reaction}} reaction for that check as the trigger.";
        return flavorText + "\n\n" + rulesText + "\n\n" + CreateAidReactionDescription(rank);
    }

    /// <summary>
    /// Generates the full rules string for the Aid reaction card.
    /// </summary>
    /// <param name="rank">The proficiency rank of the skill or attack being used when aiding.</param>
    public static string CreateAidReactionDescription(Proficiency rank)
    {
        return BasicAidReactionDescription.Replace("You grant your ally a +2 circumstance bonus to the triggering check. The bonus increases to +3 if you're a master with the check, or +4 if you're legendary.", $"{{Blue}}({rank.ToString()}){{/Blue}} You grant your ally a +{CriticalBonusFromProficiency(rank)} circumstance bonus to the triggering check.");
    }
    
    public static int CriticalBonusFromProficiency(Proficiency proficiency)
    {
        switch (proficiency)
        {
            case Proficiency.Legendary:
                return 4;
            case Proficiency.Master:
                return 3;
            default:
                return 2;
        }
    }

    public static string AorAn(string check)
    {
        //check.WithIndefiniteArticle();
        switch (check.ToUpper()[0])
        {
            case 'A':
                return "an";
            case 'I':
                return "an";
            case 'O':
                return "an";
            default:
                return "a";
        }
    }

    public static int AidDC()
    {
        return PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.AidDCIs15) ? 15 : 20;
    }
}