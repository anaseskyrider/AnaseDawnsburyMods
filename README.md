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

## Runesmith
This mod adds the *Runesmith* class from the Impossible Playtest, a new warrior scholar who wields the root of all magic: the rune.

(Requires: beta-branch v3.0)

### Class Features
Runesmith has 8 HP per level; is trained in Perception, Reflex, weapons up to martial, armor up to medium, Crafting, a tradition skill, and 2 additional skills (before Intelligence); is expert in Fortitude and Will; and uses Intelligence as its Key Attribute. Runesmith has the following features:

Level 1: Runic Repertoire (4 runes), Trace Rune, Invoke Rune, Etch Rune (2 runes), Runesmith feat, Shield block.
Level 2: Runesmith Feat, (NYI)Runic Crafter
Level 3: General feat, skill increase, additional level 1 rune known
Level 4: Runesmith feat
Level 5: Attribute boosts, ancestry feat, skill increase, smith's weapon expertise (expert in simple weapons, martial weapons, and unarmed attacks), additional level 1 rune known, additional maximum etched rune).
Level 6: Runesmith feat
Level 7: General feat

### Class Feats
- **(1st)** Backup Runic Enhancement, Engraving Strike
- **(2nd)** Fortifying Knock, Smithing Weapons Familiarity (needs mods that add advanced weapons)
- **(4th)** Terrifying Invocation, Transpose Etching.
- Also included are some dummy feats that don't do anything, in case they're needed, to be removed at a later date.

### Runesmith's Runes
- **(1st)** Atryl, Rune of Fire; Esvadir, Rune of Whetstones; Holtrik, Rune of Dwarven Ramparts; Marssyl, Rune of Impact; Zohk, Rune of Homecoming

### Differences from Tabletop
- (class feat) Backup Runic Enhancement: The tradition is always arcane.
- (class feat) Engraving Strike: Because the target of the strike is a creature, the wording of Engraving Strike implies the only runes that could be traced are those which target a creature. I've ruled that you can include any of its equipment as a target for tracing runes.
- (class feature) Applying Runes - Etched: In lieu of an easy means of etching a runesmith's runes before combat to your allies or their equipment, this is a free activity at the start of combat, targeting only yourself and your allies up to a very large distance away. This is a buff, since you can study the encounter before deciding what runes to etch.
- (class feature) Runic Crafter: Not yet implemented. The Craft activity doesn't exist in Dawnsbury, but in ideal circumstances, Crafting gives you more of an item you otherwise might not have access to. So I'd like this to somehow translate into free fundamental runes for just the runesmith. Something like, "Any armor worn, and weapons wielded, by the runesmith gains the benefits of the highest level rune available". Another implementation (if possible) might be a discount on runes with a runesmith in the party.
- (class feature) Runic Magic - Tradition Traits: I didn't implement any means of assigning a tradition trait to runes which lack one. The plan for feats like Ghostly Resonance is to require that either the rune has the right trait, or that you have the right skills for non-tradition runes (it wouldn't actually apply a trait to the effect). If you can come up with a scenario in the base game where the ability to manually assign any tradition to each application of a rune like Pluuna would be beneficial, I'll start looking for an implementation.
- (class feature) Runic Optimization: I might make it tabletop-accurate, but for now, it's just regular weapon specialization without any conditions.
- (class feature) Trace Rune:	Because Trace Rune requires a free hand, any activity which includes Trace Rune also requires a free hand unless otherwise stated by the feature (and this is a somewhat contentious part of the playtest feedback for Runesmith, so I've made it explicit where it comes up, like with Fortifying Knock and Engraving Strike).
- (runesmith rune) Zohk, Rune of Homecoming: The exact text of the conditions for the speed bonus is tricky or even impossible to implement in Dawnsbury Days. The current implementation is to grant a special Stride action which includes the status bonus and filters out any squares that are further away from the caster than you are when you use the action. I do have alternative ideas, including one that might be closer to tabletop, potentially for a future update.

Attributions:
- <a href="https://www.flaticon.com/free-icons/feather" title="feather icons">Feather icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/rune" title="rune icons">Rune icons created by bearicons - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/rune" title="rune icons">Rune icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/comment" title="comment icons">Comment icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/wow" title="wow icons">Wow icons created by Vitaly Gorbachev - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/handmade" title="handmade icons">Handmade icons created by GOWI - Flaticon</a>
