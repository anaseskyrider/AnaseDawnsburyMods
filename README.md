# AnaseDawnsburyMods
A compilation of all of my mods for Dawnsbury Days.

NOTICE: This repository is Unlicensed, but the content there-in observes the licensing requirements for OGL or ORC content. I am not a lawyer. Follow the terms and conditions of those licenses before attempting to use the content of this project.

## More Dedications
(Requires DawnniExpanded)  
(Requires beta-branch v3.0)

This mod adds more *Archetypes* to *DawnniExpanded*.

### Current Archetypes
- Archer *(Assisting Shot, Point-Blank Shot, Quick Draw, Advanced Bow Training, Crossbow Terror, Parting Shot, Double Shot, Running Reload, Archer's Aim, Triple Shot)*
- Mauler *(Knockdown, Power Attack, Clear the Way, Shoving Sweep)*

### Planned Archetypes (in order)
1. Bastion
2. Martial Artist
3. Marshal
4. *(More after that, if I can)*

### Class Feats
More Dedications also adds the following class feats which are not yet implemented in the base game (added as part of certain dedications):
- *(Fighter) Point-Blank Shot*
- *(Fighter) Parting Shot*
- *(Ranger) Running Reload*
- *(Fighter) Triple Shot*

### Other Tweaks
- *DawnniExpanded* currently does not support future-implemented or modded classes taking dedication/archetype feats with their class feats. *More Dedications* fixes this (see `MoreDedications.cs`).
- *Double Shot* currently doesn't use the *Flourish* trait internally. *Triple Shot* does, anticipating a future patch.

### Differences from Tabletop:
- Mauler/Clear the Way: This requires a weapon with the *Shove* trait, following the most-RAW interpretation of its CRB implementation. It would potentially require a different license for this mod to explicitly bypass the need to use a free hand, so I will NOT be changing this. (For the same reasons, these *Shoves* use your MAP as normal.)
- Mauler/Shoving Sweep: The *Shove* from this reaction disrupts a creature's movement action. This is base functionality of *Dawnsbury Days*. It's (very low) on my TODO to find an implementation that preserves the tabletop functionality.
- Archer/Quick Shot: This is *Dawnbury Days'* implementation of *Quick Draw*, instead of attempting to create a faithful *Quick Shot*, or a version of *Quick Draw* that only applies to certain weapons.
