using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreShields;

public static class CommonShieldRules
{
    // These functions relate to shield items and their statistics.
    #region Shield Item Functions

    /// <summary>Gets a list of shields being wielded or worn by a creature.</summary>
    public static List<Item> GetWieldedShields(Creature owner)
    {
        List<Item> heldShields = owner.HeldItems
            .Where(it => it.HasTrait(Trait.Shield))
            .ToList();
        return owner.HasFreeHand ||
               owner.HeldItems.Any(held => !held.HasTrait(Trait.Weapon) && !held.HasTrait(Trait.Grapplee))
            ? heldShields
                .Union(owner.CarriedItems
                    .Where(it => it.HasTrait(Trait.Shield) && it.IsWorn))
                .ToList()
            : heldShields;
    }

    /// <summary>Gets the circumstance bonus to AC of an item, if it's a shield.</summary>
    public static int? GetAC(Item shield)
    {
        if (!shield.HasTrait(Trait.Shield)/* && !shield.HasTrait(Trait.AlwaysOfferShieldBlock)*/)
            return null;
        if (shield.HasTrait(ModData.Traits.HeavyShield))
            return 3;
        if (shield.HasTrait(ModData.Traits.MediumShield))
            return 2;
        if (shield.HasTrait(ModData.Traits.LightShield))
            return 1;
        return 2; // Fallback value.
    }

    #endregion

    // These functions relate to raising a shield.
    #region Raising a Shield

    /// <summary>
    /// New version of the local function contained in <see cref="Fighter.CreateRaiseShield"/>. Provides a very simple action block for applying <see cref="QEffect.RaisingAShield"/>. Applying the Shield Block functionality requires the updated <see cref="FeatName.ShieldBlock"/> feat, which checks when your shield is raised to add the <see cref="QEffect.ShieldBlock"/> QEffect (see <see cref="ShieldPatches.PatchShieldBlock"/> for new functionality).
    /// </summary>
    /// <param name="self">The creature raising a shield.</param>
    /// <param name="shield">The shield being raised.</param>
    /// <param name="hasShieldBlock">You should pass Creature.HasFeat(FeatName.ShieldBlock) in most instances.</param>
    public static CombatAction CreateRaiseShieldCore(Creature self, Item shield, bool hasShieldBlock)
    {
        int acBonus = (int)GetAC(shield)!; // Suppress. Only gets called on an item that is a shield.
        return new CombatAction(
                self,
                shield.Illustration,
                $"Raise {shield.BaseHumanName.ToLower()}",
                [Trait.Basic, Trait.DoNotShowOverheadOfActionName],
                $"{{i}}You position your shield to protect yourself.{{/i}}\n\nUntil the start of your next turn, you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC"
                + (hasShieldBlock
                    ? (" and you can use the Shield Block " +
                       RulesBlock.GetIconTextFromNumberOfActions(Constants.ACTION_COST_REACTION)
                       + " reaction")
                    : null)
                + ".",
                Target.Self((_,ai) => ai.GainBonusToAC(acBonus)))
            .WithActionCost(shield.HasTrait(ModData.Traits.Hefty14) && self.Abilities.Strength < 2 ? 2 : 1)
            .WithActionId(ActionId.RaiseShield)
            .WithSoundEffect(SfxName.RaiseShield)
            .WithEffectOnEachTarget(async (_,caster,_,_) =>
            {
                QEffect qfRaisedShield = QEffect.RaisingAShield(hasShieldBlock);
                caster.AddQEffect(qfRaisedShield);
            });
    }

    /// <summary>Gets your current possibilities and looks for any action with <see cref="ActionId.RaiseShield"/> and offers it as an option (if multiple are present).</summary>
    /// <param name="self">The Creature raising the shield.</param>
    /// <returns>(bool) Whether the creature has a <see cref="QEffectId.RaisingAShield"/> effect.</returns>
    public static async Task<bool> OfferToRaiseAShield(Creature self)
    {
        Possibilities possibilities = self.Possibilities.Filter(ap =>
        {
            if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                return false;
            ap.CombatAction.ActionCost = 0;
            ap.RecalculateUsability();
            return true;
        });
        List<Option> actions = await self.Battle.GameLoop.CreateActions(
            self,
            possibilities,
            null);
        self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
        await self.Battle.GameLoop.OfferOptions(self, actions, true);
        return self.HasEffect(QEffectId.RaisingAShield);
    }

    #endregion

    // These functions relate to using Shield Block.
    #region Shield Block

    /// <summary>
    /// Creates a shield block action with a cost of 0. Doesn't do much mechanically. FullCast this action in order to trigger events that key off of taking the Shield Block reaction, such as free actions.
    /// </summary>
    /// <param name="owner">The creature doing the shield blocking.</param>
    /// <param name="shield">The shield being blocked with.</param>
    /// <param name="blockedAction">The action being defended against.</param>
    public static CombatAction CreateShieldBlock(Creature owner, Item shield, CombatAction? blockedAction)
    {
        return new CombatAction(
                owner,
                shield.Illustration,
                "Shield Block",
                [Trait.General, ModData.Traits.ReactiveAction, Trait.Basic, Trait.DoNotShowOverheadOfActionName, Trait.DoNotShowInCombatLog],
                $"{{i}}You snap your shield in place to ward off a blow.{{/i}}\n\n{{b}}Trigger{{/b}} While you have your shield raised, you would take damage from a physical attack.\n\nYour shield prevents you from taking up to {{Blue}}{shield.Hardness}{{/Blue}} damage. You take any remaining damage.",
                new CreatureTarget(
                    RangeKind.Ranged,
                    [ // No line of effect requirement
                        new MaximumRangeCreatureTargetingRequirement(99), // Usable across whole map
                    ],
                    (_,_,_) => int.MinValue))
            .WithItem(shield)
            .WithProjectileCone(VfxStyle.NoAnimation()) // WithItem adds an animation, this removes it.
            .WithTag(blockedAction)
            .WithActionId(ModData.ActionIds.ShieldBlock)
            //.WithSoundEffect(ModData.SfxNames.ShieldBlockWooodenImpact) // Plays too late.
            .WithActionCost(0);
    }

    /// <summary>
    /// Performs a Shield Block for a <see cref="QEffect.YouAreDealtDamage"/> event. Validity should be checked before calling this function (such as physical damage or Sparkling Targe requirements, or the Reflex handling for Reflexive Shield).
    /// </summary>
    /// <param name="attacker">The creature dealing the damage (from YouAreDealtDamage).</param>
    /// <param name="defender">The creature being targeted with damage (usually the YouAreDealtDamage defender).</param>
    /// <param name="dStuff">The DamageStuff from YouAreDealtDamage.</param>
    /// <param name="blocker">The creature shield blocking the damage (can be the YouAreDealtDamage defender, or another creature).</param>
    public static async Task<DamageModification?> BlockWithShield(Creature attacker, Creature defender, DamageStuff dStuff, Creature blocker)
    {
        Item? shield = GetWieldedShields(defender)
            .MaxBy(itm => itm.Hardness);
        if (shield is null)
            return null;
        CombatAction shieldBlock = CreateShieldBlock(defender, shield, dStuff.Power);
        
        // In the distant future, if I find a use-case: Temporary hardness increase QFs.
        int preventHowMuch = Math.Min(
            shield.Hardness
            + (defender.HasEffect(QEffectId.ShieldAlly) ? 2 : 0)
            + (Magus.DoesSparklingTargeShieldBlockApply(dStuff, defender) ? (defender.Level >= 15 ? 3 : defender.Level >= 7 ? 2 : 1) : 0),
            dStuff.Amount);
        
        if (!await ReactionsExpanded.AskToUseReaction2(
                defender.Battle,
                defender,
                $"{{b}}Shield Block{{/b}} {{icon:Reaction}}\n{(blocker == defender ? "You are" : defender + " is")} about to be dealt {dStuff.Amount} damage by {{Blue}}{dStuff.Power?.Name}{{/Blue}}.\nUse Shield Block to prevent {(preventHowMuch == dStuff.Amount ? "all" : preventHowMuch) + " of that damage?"}",
                shieldBlock))
            return null;
        
        // Use a delay so that the action will "complete" after the damage is reduced.
        // Alternative design if this doesn't work: Give the attacker a QF at the end of their next action which makes you FullCast shield block.
        blocker.AddQEffect(new QEffect()
        {
            Name = "Delayed Shield Block FullCast",
            StateCheckWithVisibleChanges = async qfThis2 =>
            {
                qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                await defender.Battle.GameLoop.FullCast(shieldBlock, ChosenTargets.CreateSingleTarget(attacker));
            }
        });
        blocker.Overhead(
            "shield block",
            Color.White,
            blocker + " uses {b}Shield Block{/b} to mitigate {b}" + preventHowMuch + "{/b} damage.",
            shieldBlock.Name +" {icon:Reaction}",
            shieldBlock.Description,
            shieldBlock.Traits);
        Sfxs.Play(ModData.SfxNames.ShieldBlockWooodenImpact);
        return new ReduceDamageModification(preventHowMuch, "Shield block");
    }

    #endregion
}