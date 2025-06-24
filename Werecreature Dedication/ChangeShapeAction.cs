using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Mods.Template;

namespace Dawnsbury.Mods.WerecreatureDedication;

public static class ChangeShapeAction
{
    public const string UnspecificBenefits = "You gain a movement speed, unarmed attacks, and potentially other abilities based on your werecreature type. These unarmed attacks are in the brawling group.";
    /// Submenu Possibility for the werecreature's Change Shape action.
    public static SubmenuPossibility WereShapeMenu(Creature owner, string? benefits)
    {
        PossibilitySection shapeSection = new PossibilitySection("Change Shape")
        {
            PossibilitySectionId = ModData.PossibilitySectionIds.WereShape,
            Possibilities = [
                WereShapePossibility(owner, ModData.Traits.Werecreature, benefits),
                WereShapePossibility(owner, Trait.Animal, benefits),
                WereShapePossibility(owner, Trait.Human, benefits),
            ],
        };
        SubmenuPossibility wereShapeOptions = new SubmenuPossibility(
            ModData.Illustrations.WereShape,
            "Change Shape (werecreature)")
        {
            SpellIfAny = ChangeShapeMenuAction(owner, benefits),
            SubmenuId = ModData.SubmenuIds.WereShape,
            Subsections = [ shapeSection ],
        };
        return wereShapeOptions;
    }
    
    /// <summary>This action doesn't perform any function except to be a display for feats and the submenu possibility.</summary>
    public static CombatAction ChangeShapeMenuAction(Creature owner, string? benefits = null)
    {
        return new CombatAction(
                owner,
                ModData.Illustrations.WereShape,
                "Change Shape",
                [Trait.Basic, Trait.Concentrate, Trait.Polymorph, Trait.Primal],
                "You transform into your hybrid or animal shape. Your equipment transforms with you and continues to provide bonuses, but your animal shape cannot use weapons, shields, or other held items, and cannot use manipulate actions.\n\n"+(benefits ?? UnspecificBenefits)+"\n\nYou can Dismiss the effect to return to your humanoid shape, and you resume your humanoid shape automatically if you're reduced to 0 Hit Points.",
                Target.Self())
            .WithActionCost(1);
    }

    /// <summary>Generate the action possibility corresponding to one of the change shape variations.</summary>
    /// <param name="owner">The creature owning this action.</param>
    /// <param name="type">Trait.Human corresponds to dismissing the shape. ModData.Trait.Werecreature corresponds to the hybrid shape. Trait.Animal corresponds to the animal shape.</param>
    /// <param name="benefits">A description of the effects of your specific werecreature type. Usually passed automatically by the werecreature type feat which grants your change shape action.</param>
    /// <returns></returns>
    public static ActionPossibility WereShapePossibility(Creature owner, Trait type, string? benefits)
    {
        if (type == Trait.Human)
            return new ActionPossibility(HumanShape(owner));
        else if (type == Trait.Animal)
            return new ActionPossibility(AnimalShape(owner, benefits));
        else if (type == ModData.Traits.Werecreature)
            return new ActionPossibility(HybridShape(owner, benefits));
        throw new ArgumentException("Type must be a Trait of values human, animal, or werecreature", nameof(type));
    }

    public static CombatAction HumanShape(Creature owner)
    {
        return new CombatAction(
                owner,
                ModData.Illustrations.HumanShape,
                "Dismiss Shape",
                [Trait.Basic, Trait.Concentrate, Trait.Polymorph, Trait.Primal],
                "You Dismiss your werecreature shape to return to your humanoid form.",
                Target.Self()
                    .WithAdditionalRestriction(self =>
                        self.HasEffect(ModData.QEffectIds.WereShape)
                            ? null
                            : "not in a werecreature shape"))
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.WereShape)
            .WithSoundEffect(ModData.SfxNames.HumanShape(owner))
            .WithEffectOnSelf(async self =>
            {
                self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.WereShape);
            });
    }
    
    public static CombatAction AnimalShape(Creature owner, string? benefits)
    {
        return new CombatAction(
                owner,
                ModData.Illustrations.AnimalShape,
                "Animal Shape",
                [Trait.Basic, Trait.Concentrate, Trait.Polymorph, Trait.Primal],
                "You transform into your animal shape. Your equipment transforms with you and continues to provide bonuses. Your shape ends early if you're reduced to 0 Hit Points.\n\n" +
                (benefits ?? UnspecificBenefits) + "\n\n{b}Animal Shape{/b} You cannot use weapons, shields, or other held items, and cannot use manipulate actions.",
                Target.Self()
                    .WithAdditionalRestriction(self =>
                    {
                        QEffect? wereShape = self.FindQEffect(ModData.QEffectIds.WereShape);
                        if (wereShape is { Key: "WerecreatureAnimalShape" })
                            return "already in animal shape";
                        return null;
                    }))
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.WereShape)
            .WithSoundEffect(ModData.SfxNames.AnimalShape)
            .WithEffectOnSelf(async self =>
            {
                self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.WereShape);
                self.AddQEffect(ChangeShapeEffects.AnimalShape(self));
            });
    }
    
    public static CombatAction HybridShape(Creature owner, string? benefits)
    {
        return new CombatAction(
                owner,
                ModData.Illustrations.HybridShape,
                "Hybrid Shape",
                [Trait.Basic, Trait.Concentrate, Trait.Polymorph, Trait.Primal],
                "You transform into your hybrid shape. Your equipment transforms with you and continues to provide bonuses. Your shape ends early if you're reduced to 0 Hit Points.\n\n" + (benefits ?? UnspecificBenefits),
                Target.Self()
                    .WithAdditionalRestriction(self =>
                    {
                        QEffect? wereShape = self.FindQEffect(ModData.QEffectIds.WereShape);
                        if (wereShape is { Key: "WerecreatureHybridShape" })
                            return "already in hybrid shape";
                        return null;
                    }))
            .WithActionCost(1)
            .WithActionId(ModData.ActionIds.WereShape)
            .WithSoundEffect(ModData.SfxNames.HybridShape)
            .WithEffectOnSelf(async self =>
            {
                self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.WereShape);
                self.AddQEffect(ChangeShapeEffects.HybridShape(self));
            });
    }
}