# ParryLogic
Provides logic for generating and using Parry actions associated with the parry trait, without creating duplicate actions when used by multiple mods, and without directly modifying items to grant actions. Further documentation beyond this README can be found inside `ParryLogic.cs`.

All registered data including enums and illustrations are contained to the library.

### GreaterParry
The `GreaterParry()` function creates a `QEffect` that performs the effects of feats that grant the parry trait to certain weapons and increases the bonus for those weapons that already had it; such as with the Guardian feat Raise Haft, and the Warrior of Legend class archetype.

### Art (Public Domain)
This library comes with two icons (and two rotated variants). This art is provided by Lobot922 for use as public domain, no credits required.

I recommend using `new ModdedIllustration(<path to your asset folder> + "ParryT7.png")` and `new ModdedIllustration(<path to your asset folder> + "ParryT6.png")` for the second and third arguments of `ParryLogic.Load()`. This will create a SideBySideIllustration using a colorful icon and the weapon's illustration for the action, while the effect on your token will be a high-contrast black-and-white icon unique to parrying, in the same style as the game's raised shield icon.

If you prefer not to use any icon art for these actions, they can be null, which will use the weapon illustration for the action and effect icons.

as part of a `SideBySideIllustration` and `ParryT6.png` as the sole `Illustration` for the basic Parry `QEffect` when passing illustrations to `ParryLogic.Load()`.

## Installation
1. Add `ParryLogic.cs` to your mod project.
2. If using this library's art, add the images you wish to use to your mod's assets folder (see above for recommendations).
3. Call `ParryLogic.Load(arg1, arg2, arg3)` when loading your mod, such as directly in your `[DawnsburyDaysModMainMethod]` function.
4. The first argument is the `string` name of your mod, such as `"Extra Weapons"`.
5. The second argument is the `Illustration` icon to use in a `SideBySideIllustration` for the Parry action that is added to your action bar. If null, then just the weapon's `Illustration` will be used.
6. The third argument is the `Illustration` icon to use for the parrying effect added to a token. You can pass in any `Illustration`, so it doesn't have to be the sole icon if you want to use another type such as `SideBySideIllustration`.

### Examples
```c#
[DawnsburyDaysModMainMethod]
public static void LoadMod()
{
    ParryLogic.Load(
        "GuardianClass",
        new ModdedIllustration("GuardianClassAssets/ParryT7.png"),
        new ModdedIllustration("GuardianClassAssets/ParryT6.png"));
}
```
```c#
new TrueFeat(/*...*/)
    .WithOnCreature(creature =>
    {
        creature.AddQEffect(ParryLogic.GreaterParry(
            "Raise Haft",
            "Two-handed weapons gain the parry trait for you, or increase the bonus to +2 if they already have it.",
            (qfRaiseHaft, weapon) =>
                weapon.HasTrait(Trait.TwoHanded)));
    });
```
