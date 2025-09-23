using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreBasicActions;

public static class DropProne
{
    public static void LoadDropProne()
    {
        // Add Drop Prone to every creature.
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            QEffect dropProneLoader = new QEffect()
            {
                Name = "Drop Prone Loader",
                ProvideActionIntoPossibilitySection = (qfThis, section) =>
                {
                    if (!PlayerProfile.Instance.IsBooleanOptionEnabled(ModData.BooleanOptions.AllowDropProne))
                        return null;
                    if (section.PossibilitySectionId != PossibilitySectionId.Movement)
                        return null;
                    // Must not already be prone
                    if (qfThis.Owner.HasEffect(QEffectId.Prone))
                        return null;
                    
                    return new ActionPossibility(DropProneAction(qfThis.Owner));
                },
                // Remove persistent acid and fire damage by dropping prone
                ProvideContextualAction = qfThis =>
                {
                    Creature self = qfThis.Owner;
                    
                    // Drop and roll
                    if (self.QEffects
                        .Where(qf => qf.Id == QEffectId.PersistentDamage)
                        .Any(qf => qf.GetPersistentDamageKind() is DamageKind.Fire or DamageKind.Acid))
                    {
                        CombatAction dropAndRoll = DropProneAction(self);
                        dropAndRoll.Name = "Drop and roll";
                        dropAndRoll.Illustration = new SideBySideIllustration(IllustrationName.PersistentFire,
                            IllustrationName.DropProne
                        );
                        dropAndRoll.Description =
                            "{b}Requirements{/b} You have persistent fire or acid damage.\n\nYou fall prone and roll a recovery check to end any persistent acid and fire damage you have. {i}(Can be repeated if you are already prone, without having to Stand.){/i}\n\nIf you're standing or swimming in water, you automatically succeed instead.";

                        return (ActionPossibility)dropAndRoll;
                    }

                    // Some other context
                    /*if ()
                    {
                        
                    }*/

                    return null;

                } 
            };
            cr.AddQEffect(dropProneLoader);
            
            // DEBUG
            /*cr.AddQEffect(new QEffect()
            {
                ProvideMainAction = qfThis => (ActionPossibility)new CombatAction(qfThis.Owner, IllustrationName.YellowWarning, "Set Fire", [], "", Target.RangedCreature(99))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (_, _, target, _) =>
                    {
                        target.AddQEffect(QEffect.PersistentDamage("1d4", DamageKind.Fire));
                    })
            });
            cr.AddQEffect(new QEffect()
            {
                ProvideMainAction = qfThis => (ActionPossibility)new CombatAction(qfThis.Owner, IllustrationName.YellowWarning, "Set Acid", [], "", Target.RangedCreature(99))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (_, _, target, _) =>
                    {
                        target.AddQEffect(QEffect.PersistentDamage("1d4", DamageKind.Acid));
                    })
            });
            cr.AddQEffect(new QEffect()
            {
                ProvideMainAction = qfThis => (ActionPossibility)new CombatAction(qfThis.Owner, IllustrationName.YellowWarning, "Swimming", [], "", Target.RangedCreature(99))
                    .WithActionCost(0)
                    .WithEffectOnEachTarget(async (_, _, target, _) =>
                    {
                        target.AddQEffect(QEffect.Swimming());
                    })
            });*/
        });
    }

    public static CombatAction DropProneAction(Creature owner)
    {
        return new CombatAction(
                owner,
                IllustrationName.DropProne,
                "Drop prone",
                [Trait.Move, Trait.ProvokesAfterActionCompletion, Trait.Basic],
                "You fall prone.\n\nThis is {Red}largely disadvantageous{/Red} but situationally useful:\n"
                + "\n• {b}Hit the deck.{/b} You can Take Cover {icon:Action} while prone to gain greater cover against ranged attacks."
                + "\n• {b}Drop and roll.{/b} Make a recovery check to end persistent acid and fire damage. If you're standing or swimming in water, you automatically succeed instead. {i}(Can be repeated if you are already prone, without having to Stand.){/i}",
                Target.Self())
            .WithSoundEffect(SfxName.DropProne)
            // Allows you to repeat the action when prone
            .WithActionId(owner.HasEffect(QEffectId.Prone) ? ActionId.Crawl : ActionId.None)
            .WithEffectOnSelf(async self =>
            {
                // Fall Prone
                await self.FallProne();
                
                // End persistent fire and acid
                if (self.QEffects.Where(qf =>
                        qf.Id is QEffectId.PersistentDamage
                        && qf.GetPersistentDamageKind() is DamageKind.Acid or DamageKind.Fire)
                    .ToList()
                    is { Count: > 0 } persistentDamages)
                {
                    if (self.Occupies.Kind is TileKind.Water or TileKind.ShallowWater
                        || self.HasEffect(QEffectId.AquaticCombat))
                    {
                        self.RemoveAllQEffects(persistentDamages.Contains);
                        self.Overhead("recovered", Color.Lime, $"{self} automatically recovers from persistent {S.ConstructOrList(persistentDamages.Select(qf => qf.GetPersistentDamageKind().ToStringOrTechnical().ToLower()), "and")} damage");
                    }
                    else
                    {
                        self.QEffects
                            .Where(qf => qf.Id == QEffectId.PersistentDamage)
                            .ForEach(qf =>
                            {
                                if (qf.GetPersistentDamageKind() is DamageKind.Fire or DamageKind.Acid)
                                    qf.RollPersistentDamageRecoveryCheck(false);
                            });
                    }
                }
            });
    }
}