using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class HelpUp
{
    public static void LoadHelpUp()
    {
        // Option to treat helping up as the target not moving
        ModManager.RegisterBooleanSettingsOption("MoreBasicActions.HelpUpIsNotMove",
            "More Basic Actions: Help Up Doesn't Move Ally",
            "Helping an ally up from prone counts as you taking a manipulate action and the ally taking a move action. Enabling this action means the ally doesn't actually take the Stand Up action.",
            false);
        
        // Add Help Up to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect helpUpLoader = new QEffect()
            {
                ProvideContextualAction = qfThis =>
                {
                    if (!qfThis.Owner.Battle.AllCreatures.Any(cr =>
                            cr.IsAdjacentTo(qfThis.Owner) && cr.HasEffect(QEffectId.Prone)))
                        return null;
                    
                    return new ActionPossibility(CreateHelpUpAction(cr), PossibilitySize.Full);
                },
            };
            cr.AddQEffect(helpUpLoader);
        });
    }

    public static CombatAction CreateHelpUpAction(Creature owner)
    {
        bool doNotMove = PlayerProfile.Instance.IsBooleanOptionEnabled("MoreBasicActions.HelpUpIsNotMove");

        CombatAction helpUpAction = new CombatAction(
                owner, 
                ModData.Illustrations.HelpUp,
                "Help Up",
                [Trait.Manipulate],
                "{b}Requirements{/b} You have a free hand\n\nChoose an adjacent ally who is prone and is able to take move actions. " +
                (doNotMove
                    ? "That ally ceases being prone."
                    : "That ally Stands as a free action."),
                Target.AdjacentFriend()
                    .WithAdditionalConditionOnTargetCreature((a, d) =>
                    {
                        if (!a.HasFreeHand)
                            return Usability.NotUsable("must have a free hand");
                        if (!d.HasEffect(QEffectId.Prone))
                            return Usability.NotUsableOnThisCreature("not prone");
                        CombatAction standUp = CreateStandUpAction(d).WithActionCost(0);
                        if (!standUp.CanBeginToUse(d))
                            return Usability.NotUsableOnThisCreature("can't stand up");
                        return Usability.Usable;
                    }))
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.HelpUp)
            .WithEffectOnEachTarget(async (thisAction, caster, target, result) =>
            {
                if (doNotMove)
                {
                    target.RemoveAllQEffects(qf => qf.Id == QEffectId.Prone);
                    target.AnimationData.ChangeSize(target);
                    Sfxs.Play(SfxName.StandUp);
                }
                else
                {
                    CombatAction SimpleStand = CreateStandUpAction(target).WithActionCost(0);
                    await target.Battle.GameLoop.FullCast(SimpleStand, ChosenTargets.CreateSingleTarget(target));
                }
            });
        return helpUpAction;
    }

    public static CombatAction CreateStandUpAction(Creature owner)
    {
        // Code copied from decompilation.
        // Idk what LesserKipUp does, but it doesn't seem to exist.
        // Lmao pruned that.
        
        bool hasKipUp = owner.HasEffect(QEffectId.KipUp);
        
        Trait[] traits = [
            Trait.Basic,
            Trait.Move,
            hasKipUp ? Trait.ProvokesAfterActionCompletion : Trait.DoesNotProvoke,
        ];
        
        CombatAction standUp = new CombatAction(
                owner,
                IllustrationName.StandUp,
                hasKipUp ? "Kip up" : "Stand",
                traits,
                hasKipUp ? "{i}With a practiced maneuver, you smoothly bounce up.{/i}\n\nYou stand up from prone without provoking an attack of opportunity." : "You stand up from prone.",
                Target.Self((cr, a) =>
                    1.07374182E+09f))
            .WithActionCost(!hasKipUp ? 1 : 0)
            .WithActionId(ActionId.Stand)
            .WithEffectOnSelf(cr =>
            {
                cr.RemoveAllQEffects(qf => qf.Id == QEffectId.Prone);
                cr.AnimationData.ChangeSize(cr);
            })
            .WithSoundEffect(SfxName.StandUp);
        
        return standUp;
    }
}