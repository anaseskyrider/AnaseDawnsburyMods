using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;

namespace Dawnsbury.Mods.MoreShields;

public static class ReactionsExpanded
{
    /// <summary>
    /// Grants an additional reaction each round that can only be used on certain abilities. See <see cref="AskToUseReaction2"/> for how to attempt to use your bonus reaction even if your reaction is already expended.
    /// </summary>
    /// <param name="name">The name of the QEffect</param>
    /// <param name="description">The description of the QEffect</param>
    /// <param name="icon">The effect's Illustration, if any.</param>
    /// <param name="permission">A lambda function which returns TRUE if the taken CombatAction should refund your reaction. <see cref="CombatAction.ActionCost"/> must equal -2 or 0.</param>
    /// <param name="innate">Whether the QEffect is innate or not</param>
    public static QEffect ExtraReaction(string name, string description, Illustration? icon, Func<CombatAction, bool> permission, bool? innate = false)
    {
        QEffect extraReaction = new QEffect(
            name,
            description,
            ExpirationCondition.Never,
            null,
            icon)
        {
            Innate = innate ?? false,
            Id = ModData.QEffectIds.BonusReaction,
            Tag = permission, // Query this when checking if an action can be used for free instead of consuming your reaction. Done via AskToUseReaction2().
            YouBeginAction = async (qfThis, action) =>
            {
                if (CanRestoreReaction(qfThis, action))
                    RestoreReaction(qfThis, action);
            },
            AfterYouTakeAction = async (qfThis, action) =>
            {
                if (CanRestoreReaction(qfThis, action))
                    RestoreReaction(qfThis, action);
            },
        };
        return extraReaction;

        bool CanRestoreReaction(QEffect qf, CombatAction action)
        {
            if (qf.UsedThisTurn || !qf.Owner.Actions.IsReactionUsedUp)
                return false;
            if (action.ActionCost is not Constants.ACTION_COST_REACTION and not 0)
                return false;
            return permission.Invoke(action);
        }

        void RestoreReaction(QEffect qf, CombatAction action)
        {
            qf.Owner.Actions.RefundReaction();
            qf.UsedThisTurn = true;
        }
    }

    /// <summary>
    /// Similar to <see cref="TBattle.AskToUseReaction(Creature, string)"/> except that you can specify an Illustration as well what action you want to attempt to use with your reaction, and will instead offer to use it as a free action if you have a valid <see cref="ExtraReaction"/> QEffect.
    /// </summary>
    public static async Task<bool> AskToUseReaction2(
        TBattle battle,
        Creature reactingCreature,
        string question,
        CombatAction onWhat,
        Illustration? icon = null)
    {
        QEffect? freeReaction = reactingCreature.QEffects.FirstOrDefault(qf =>
            qf.Id == ModData.QEffectIds.BonusReaction && !qf.UsedThisTurn && (qf.Tag as Func<CombatAction, bool>)?.Invoke(onWhat) == true);
        
        if (freeReaction == null)
            return await battle.AskToUseReaction(reactingCreature, question, icon ?? IllustrationName.Reaction);
        
        bool used = await battle.AskForConfirmation(
            reactingCreature,
            icon ?? IllustrationName.FreeAction,
            question,
            RulesBlock.GetIconTextFromNumberOfActions(0) + " Take free action");
        freeReaction.UsedThisTurn = used;
        return used;

    }
}