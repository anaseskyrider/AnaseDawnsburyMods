using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Mods.Template;

namespace Dawnsbury.Mods.WerecreatureDedication;

public static class ChangeShapeEffects
{
    public static QEffect WereShape(Creature owner, string name, Illustration icon, string key)
    {
        return new QEffect(
            name,
            "You are in a werecreature shape. This effect ends early if you're reduced to 0 Hit Points.",
            ExpirationCondition.Never,
            owner,
            icon)
        {
            Id = ModData.QEffectIds.WereShape,
            Traits = [Trait.Polymorph, Trait.Primal],
            Key = key,
            StateCheck = qfThis =>
            {
                if (qfThis.Owner.HP == 0)
                    qfThis.ExpiresAt = ExpirationCondition.Immediately;
            }
        };
    }
    public static QEffect AnimalShape(Creature owner)
    {
        QEffect animalShape = WereShape(
            owner,
            "Animal Shape",
            ModData.Illustrations.AnimalShape,
            "WerecreatureAnimalShape");
        animalShape.PreventTakingAction = action =>
        {
            if (action.Item is { } item)
                if (item.HasTrait(Trait.Weapon) && !item.HasTrait(Trait.Unarmed))
                    return "cannot use weapons";
                else if (item.HasTrait(Trait.Shield))
                    return "cannot use shields";
                else if (action.Owner.HeldItems.Contains(item))
                    return "cannot use held items";
            if (action.HasTrait(Trait.Manipulate))
                return "cannot take manipulates";
            return null;
        };
        return animalShape;
    }
    
    public static QEffect HybridShape(Creature owner)
    {
        return WereShape(
            owner,
            "Hybrid Shape",
            ModData.Illustrations.HybridShape,
            "WerecreatureHybridShape");
    }
}