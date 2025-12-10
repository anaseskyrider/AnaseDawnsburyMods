using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
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

    /// <summary>Gets whether a given creature is holding or wearing this shield. Returns false if the shield is held but stowed, or worn but not worn correctly.</summary>
    public static bool IsShieldWielded(Creature owner, Item shield)
    {
        return GetWieldedShields(owner).Contains(shield);
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

    /// <summary>Gets a list of shields currently raised by this creature.</summary>
    public static List<Item> GetRaisedShields(Creature owner)
    {
        return owner.QEffects
            .Where(qf => qf.Id is QEffectId.RaisingAShield)
            .Select(qf => qf.Tag as Item)
            .WhereNotNull()
            .ToList();
    }

    #endregion
    
    #region Shield Effects

    /// <summary>
    /// Adds an invisible QEffect which doesn't expire that adds the listed amount of bonus hardness to Shield Block reaction events.
    /// </summary>
    /// <param name="bonus">An untyped bonus to apply to all shield block events.</param>
    /// <param name="bonusSource">The bonus name.</param>
    /// <param name="type">(Default: untyped) The bonus type.</param>
    public static QEffect BonusToShieldHardness(int bonus, string bonusSource, BonusType type = BonusType.Untyped)
    {
        return BonusToShieldHardness((_,_,_,_) =>
            new Bonus(bonus, type, bonusSource));
    }

    /// <summary>
    /// Adds an invisible QEffect which doesn't expire that adds a Bonus to hardness to Shield Block reaction events.
    /// </summary>
    /// <param name="shouldApply">A lambda function which takes in the ATTACKER, the DAMAGESTUFF, the TARGET of the damage, and the one BLOCKING it. It returns the bonus to apply to the shield block event.</param>
    public static QEffect BonusToShieldHardness(Func<Creature,DamageStuff,Creature,Creature,Bonus?> shouldApply)
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.BonusToHardness,
            Tag = shouldApply,
        };
    }

    /// <summary>
    /// Gets the total bonuses to hardness for Shield Block events.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="dStuff"></param>
    /// <param name="target"></param>
    /// <param name="blocker"></param>
    /// <returns></returns>
    public static int GetShieldBlockHardnessBonuses(
        Creature attacker,
        DamageStuff dStuff,
        Creature target,
        Creature blocker)
    {
        List<Bonus?> bonuses = [];
        foreach (QEffect qf in blocker.QEffects.Where(qf => qf.Id == ModData.QEffectIds.BonusToHardness))
        {
            if (qf.Tag is Func<Creature, DamageStuff, Creature, Creature, Bonus?> bonusToHardness)
                bonuses.Add(bonusToHardness.Invoke(attacker, dStuff, target, blocker));
        }

        return Bonus.Sum(bonuses, false).BonusTotal;
    }
    
    #endregion

    // These functions relate to raising a shield.
    #region Raising a Shield

    /// <summary>
    /// Updated version of the local function contained in <see cref="Fighter.CreateRaiseShield"/>. Provides a simple CombatAction for applying <see cref="QEffect.RaisingAShield"/>. Applying the Shield Block functionality requires the patched <see cref="QEffect.ShieldBlock"/> QEffect, which checks when your shield is raised to add the YouAreDealtDamage logic.
    /// <seealso cref="ShieldPatches.PatchShieldBlock"/>
    /// </summary>
    /// <param name="self">The creature raising a shield.</param>
    /// <param name="shield">The shield being raised.</param>
    /// <param name="hasShieldBlock">(nullable) If you don't pass an override, this is calculated the same way the base CreateRaiseShield does it.</param>
    public static CombatAction CreateRaiseShieldCore(Creature self, Item shield, bool? hasShieldBlock)
    {
        hasShieldBlock ??= self.HasEffect(QEffectId.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
        int acBonus = (int)GetAC(shield)!; // Suppress. Only gets called on an item that is a shield.
        return new CombatAction(
                self,
                shield.Illustration,
                $"Raise {shield.BaseHumanName.ToLower()}",
                [Trait.Basic, Trait.DoNotShowOverheadOfActionName],
                null!,
                Target.Self((_,ai) => ai.GainBonusToAC(acBonus))
                    .WithAdditionalRestriction(self2 =>
                        self2.QEffects.Any(qf =>
                            qf.Id is QEffectId.RaisingAShield
                            && qf.Tag == shield)
                        ? "Already raised"
                        : null))
            .WithDescription(
                "You position your shield to protect yourself.",
                $"Until the start of your next turn, you gain a {{Blue}}+{acBonus}{{/Blue}} circumstance bonus to AC{((bool)hasShieldBlock ? " and can Shield Block {icon:Reaction}  with this shield" : null)}.")
            .WithItem(shield)
            .WithActionCost(shield.HasTrait(ModData.Traits.Hefty14) && self.Abilities.Strength < 2 ? 2 : 1)
            .WithActionId(ActionId.RaiseShield)
            .WithSoundEffect(SfxName.RaiseShield)
            .WithEffectOnEachTarget(async (action, caster, target, _) =>
            {
                bool isDevotedGuardian = action.Target is not SelfTarget;
                Fighter.RaiseShield(caster, shield, target, isDevotedGuardian);
            });
    }

    /// <summary>Gets your current possibilities and looks for any action with <see cref="ActionId.RaiseShield"/> and offers it as an option (if multiple are present).</summary>
    /// <param name="self">The Creature raising the shield.</param>
    /// <param name="onlyWhat">Additional filters on allowed actions, such as shields that wouldn't be able to cross a threshold with Reactive Shield.</param>
    /// <returns>(bool) Whether the creature has a <see cref="QEffectId.RaisingAShield"/> effect.</returns>
    public static async Task<bool> OfferToRaiseAShield(Creature self, Func<CombatAction, bool>? onlyWhat = null)
    {
        Possibilities raiseShields = self.Possibilities.Filter(ap =>
        {
            if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                return false;
            if (onlyWhat?.Invoke(ap.CombatAction) == false)
                return false;
            ap.CombatAction.ActionCost = 0;
            ap.RecalculateUsability();
            return true;
        });
        
        var active = self.Battle.ActiveCreature;
        self.Battle.ActiveCreature = self;
        self.Possibilities = raiseShields;
        
        List<Option> actions = await self.Battle.GameLoop.CreateActions(
            self,
            raiseShields,
            null);
        self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
        await self.Battle.GameLoop.OfferOptions(self, actions, true);
        
        self.Battle.ActiveCreature = active;
        
        return self.HasEffect(QEffectId.RaisingAShield);
    }

    #endregion

    // These functions relate to using Shield Block.
    #region Shield Block

    public static async Task<DamageModification?> OfferAndMakeShieldBlock(
        Creature attacker,
        Creature defender,
        DamageStuff dStuff,
        Creature blocker)
    {
        List<Item> raisedShields = GetRaisedShields(blocker);

        // Do nothing if no shield options are found
        if (raisedShields.Count == 0)
            return null;
        
        int? chosenItem = await blocker.Battle.AskToUseReaction(
            blocker,
            $"{{b}}Shield Block{{/b}} {{icon:Reaction}}\n{(blocker == defender ? "You are" : defender + " is")} about to be dealt {dStuff.Amount} damage by {{Blue}}{dStuff.Power?.Name}{{/Blue}}.\nBlock with {(raisedShields.Count > 1 ? "one of your shields" : "your shield")}?",
            ModData.Illustrations.ShieldBlock,
            [Trait.ShieldBlock],
            raisedShields
                .Select(shield =>
                {
                    string icon = shield.Illustration.IllustrationAsIconString;
                    int hardness = shield.Hardness + CommonShieldRules.GetShieldBlockHardnessBonuses(attacker, dStuff, defender, blocker);
                    int preventHowMuch = Math.Min(hardness, dStuff.Amount);
                    
                    return $"{icon} {shield.BaseHumanName} ({hardness.WithColor(preventHowMuch == dStuff.Amount ? "Green" : "Blue")})";
                })
                .ToArray());

        if (chosenItem is null)
            return null;
        
        Item shield = raisedShields[(int)chosenItem];
        int hardness = shield.Hardness + CommonShieldRules.GetShieldBlockHardnessBonuses(attacker, dStuff, defender, blocker);
        int preventHowMuch = Math.Min(hardness, dStuff.Amount);

        return await ShieldBlockYouAreDealtDamage(attacker, defender, blocker, shield.Name, hardness, preventHowMuch);
    }

    /// <summary>
    /// Performs a Shield Block logic execution, returning a damage modification. Unlike <see cref="Fighter.ShieldBlockYouAreDealtDamage"/>, this DOES NOT ASK TO USE YOUR REACTION. It simply executes Shield Block logic.
    /// </summary>
    /// <param name="attacker">The creature doing the damage stuff.</param>
    /// <param name="defender">The creature being targeted, who could be defended.</param>
    /// <param name="blocker">The creature doing the blocking.</param>
    /// <param name="shieldName">Usually the <see cref="Item.Name"/> of the shield.</param>
    /// <param name="finalHardness">The shield's final hardness, after bonuses have been added up.</param>
    /// <param name="preventHowMuch">The amount of damage actually prevented (the final hardness, but only up to the actual damage dealt).</param>
    /// <returns></returns>
    public static async Task<DamageModification?> ShieldBlockYouAreDealtDamage(
        Creature attacker,
        Creature defender,
        Creature blocker,
        string shieldName,
        int finalHardness,
        int preventHowMuch)
    {
        foreach (QEffect qf in blocker.QEffects.ToList())
            await qf.WhenYouUseShieldBlock.InvokeIfNotNull(qf, attacker, defender, preventHowMuch);
        
        // Enhanced overhead log information
        blocker.Overhead(
            "shield block", Color.White,
            blocker + " uses {b}Shield Block{/b} {icon:Reaction} to mitigate {b}" + preventHowMuch + "{/b} damage.",
            "Shield block {icon:Reaction}" + " (" + shieldName + ")",
            "{i}You snap your shield in place to ward off a blow.{/i}\n\n{b}Trigger{/b} While you have your shield raised, you would take damage from a physical attack.\n\nYour {Blue}" + shieldName + "{/Blue} prevents you from taking up to {Blue}" + finalHardness + "{/Blue} damage. You take any remaining damage.",
            new Traits([Trait.General]));
        
        // Adds an impact sound
        //Sfxs.Play(ModData.SfxNames.ShieldBlockWooodenImpact);
        
        return new ReduceDamageModification(preventHowMuch, "Shield block");
    }

    #endregion
}