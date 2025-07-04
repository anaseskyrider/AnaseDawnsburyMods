# AnaseDawnsburyMods
A compilation of all of my mods for Dawnsbury Days.

⚠ The description in this repo is usually out of date. The most up to date descriptions of each mod are found at their Workshop pages. ⚠

STATEMENT OF INTENT: This repository, other than the licensed content it contains, is public domain (see Unlicense). Follow the terms and conditions of those other licenses located in the associated mod folders before attempting to use the content of this project, which I do not possess the rights to commit to public domain.

### Links
- [Steam Workshop: More Dedications](https://steamcommunity.com/sharedfiles/filedetails/?id=3447019566)
- [Steam Workshop: Runesmith](https://steamcommunity.com/sharedfiles/filedetails/?id=3460180524)
- [Steam Workshop: More Basic Actions](https://steamcommunity.com/sharedfiles/filedetails/?id=3485625903)
- [Ko-Fi](https://ko-fi.com/anaseskyrider) (tips always appreciated)
- I go by @AnaseSkyrider on Discord and most other places.

## More Dedications

This mod adds more **Archetypes** and expands existing Archetypes.

### Current Archetypes
- **Bastion** Agile Shield Grip, Disarming Block, Shielded Stride, Reflexive Shield
- **Marshal** Dread Marshal Stance, Inspiring Marshal Stance, Steel Yourself!, Rallying Charge, Attack of Opportunity, To Battle!
- **Martial Artist** Powder Punch Stance, Brawling Focus, Crane Stance, Dragon Stance, Gorilla Stance, Mountain Stance, Wolf Stance, Stumbling Stance, Tiger Stance, Follow-Up Strike, Thunder Clap, Crane Flutter, Dragon Roar, Gorilla Pound, Grievous Blow, Mountain Stronghold, Stumbling Feint, Tiger Slash, Wolf Drag
- **Mauler** Knockdown, Power Attack, Clear the Way, Shoving Sweep

### Expanded Archetypes
- **Archer** Quick Draw, Advanced Bow Training, Crossbow Terror, Parting Shot, Running Reload, Archer's Aim

### Planned Archetypes (in order)
1. ~~Bastion~~
2. ~~Martial Artist~~
3. ~~Marshal~~
4. *(More after that, if I can)*

### Class Feats
More Dedications also adds the following class feats which are not yet implemented in the base game (added as part of certain dedications):
- (Fighter) Parting Shot, Shielded Stride, Reflexive Shield
- (Ranger) Running Reload
- (Monk) Crane Flutter, Dragon Roar, Gorilla Pound, Mountain Stronghold, Stumbling Feint, Stumbling Stance, Tiger Slash, Tiger Stance, Wolf Drag

This mod also adds the following feats which are not part of any archetype (might be removed and repackaged in a separate mod at a later date):
- (Monk) Stoked Flame Stance

### Differences from Tabletop:
- **Archer/Quick Shot:** This is Dawnsbury Days' implementation of Quick Draw.
- **Martial Artist/Stoked Flame Stance:** For technical reasons, the alternative crit spec effect applies on top of the usual crit spec, but does reduced persistent damage (1d4 instead of 1d6), similar to Tiger Stance. Access to crit spec is still required, unlike Tiger Stance.
Martial Artist/Tiger Stance: Step functionality works very slightly differently. Any time you take a Step action, you can Step again as a free action. (This should allow it to combine with any action or reaction which Steps.)
- **Mauler/Clear the Way:** This requires a weapon with the Shove trait, following the most-RAW interpretation of its CRB implementation. For licensing reasons, I will NOT be changing this. (For the same reasons, these Shoves use your MAP as normal.)
- **Mauler/Shoving Sweep:** The Shove from this reaction disrupts a creature's movement action. This is base functionality of Dawnsbury Days. It's (very low) on my TODO to find an implementation that preserves the tabletop functionality.

## Runesmith
Adds the *Runesmith* class from the Impossible Playtest, a warrior scholar who wields a repertoire of runic magic.

### How Runesmith Works
In a nutshell, the Runesmith is a martial class mixed with a **Runic Repertoire**. During combat, you temporarily **Trace** these runes onto the rune's target (e.g. a creature or a weapon), which confers passive effects like "Reduced fire resistance". As a separate action, you can **Invoke** one or more runes to activate effects like "The rune-bearer makes a Fortitude save vs fire damage", which consumes the rune in the process.

These runes can be Traced an unlimited number of times, so the Runesmith is about mixing Strikes with maximizing the passive and invocation effects of their runes. Runes are to the Runesmith what Impulses are to the Kineticist.

_Disclaimer: the Runesmith's runes are unrelated to the runes from the shop. You are not tracing a +1 Weapon Potency rune in the middle of combat._

### Class Features
**Key Attribute** Intelligence  
**Perception** trained  
**Offenses** up to martial weapons  
**Defenses** 8 HP, up to medium armor; trained Reflex, expert Fortitude & Will  
**Skills** 2 + Crafting + 1 of Arcana/Nature/Occultism/Religion + Intelligence  

**Level 1:** Runic Repertoire (4 runes), Trace Rune, Invoke Rune, Etch Rune (2 runes), Runesmith feat, Shield Block  
**Level 2:** Runesmith feat, Runic Crafter  
**Level 3:** General feat, skill increase, additional rune known (level 1)  
**Level 4:** Runesmith feat  
**Level 5:** Attribute boosts, ancestry feat, skill increase, Smith's Weapon Expertise (expert in unarmed/simple/martial weapons), additional rune known (level 1), Etch Rune (3 runes)  
**Level 6:** Runesmith feat  
**Level 7:** General feat, Runic Optimization, additional rune known (level 1)  
**Level 8:** Runesmith feat  

### Class Feats
- **(1st)** Backup Runic Enhancement, Engraving Strike, Remote Detonation, Rune-Singer
- **(2nd)** Fortifying Knock, Invisible Ink, Runic Tattoo, Smithing Weapons Familiarity
- **(4th)** Artist's Attendance, Ghostly Resonance, Terrifying Invocation, Transpose Etching
- **(6th)** Runic Reprisal, Tracing Trance, Vital Composite Invocation, "Words, Fly Free"
- **(8th)** Drawn In Red, Elemental Revision, Read the Bones, (Homebrew) Early Access

### Runesmith's Runes
- **(1st)** Atryl, Rune of Fire; Esvadir, Rune of Whetstones; Holtrik, Rune of Dwarven Ramparts; Marssyl, Rune of Impact; Oljinex, Rune of Cowards' Bane; Pluuna, Rune of Illumination; Ranshu, Rune of Thunder; Sun-, Diacritic Rune of Preservation; Ur-, Diacritic Rune of Intensity; Zohk, Rune of Homecoming
- **(9th)** (accessible with a homebrew feat) En-, Diacritic Rune of Expansion; Feikris, Rune of Gravity; Jurroz, Rune of Dragon Fury; Kojastri, Rune of Insulation

### Differences from Tabletop
In general, if a small element of a feature doesn't exist in DD, then that text is removed for simplicity and clarity.

#### Features
- **Etching Runes:** This is a free activity at the start of combat, targeting only yourself and your allies.
- **Runic Crafter:** Crafting isn't a thing. So instead, you automatically gain the effects of the highest level fundamental weapon and armor runes for your level. Just the item bonuses, not the actual runes, so you still want Weapon Potency for property runes. At level 9, the Assured Runic Crafter feature allows you to choose an ally before combat begins to gain the benefits of this feature.
- **Runic Magic - Tradition Traits:** Manually assigning traits is clunky. For ease of play, feats like Ghostly Resonance require that either the rune has the right trait, or that you simply have the right tradition-skill for generic runes (essentially, this is ephemerally and retroactively giving your drawn runes upwards of multiple tradition traits).
- **Runic Optimization:** Functions as weapon specialization. Greater Runic Optimization provides +2 damage instead of 2x the original damage, as normal.
- **Trace Rune:** Because Trace Rune requires a free hand, any activity which includes Trace Rune also requires a free hand unless stated otherwise (I've made it explicit throughout the other feats).

#### Feats
- **Artist's Attendance:** My interpretation: you qualify as a rune-bearer within your reach, and your reach can be determined by any source of reach. This might not be intended behavior by Paizo, but it's playtest.
- **Backup Runic Enhancement:** The tradition is always arcane, for ease of implementation. Lemme know if that causes issues.
- **Remote Detonation:** This is constructed as a meta-strike just like Engraving Strike. Doesn't affect mechanics, just improves UEX.
- **Elemental Revision:** Too difficult (if at all possible) implement accurately -- so like Battle Medicine, the creature becomes immune to Elemental Revision for a day. Yes, you CAN waste actions to trading items around to avoid this.
- **Engraving Strike:** The wording implies the only runes that could be traced are those targeting creatures. I intentionally allow you to also Trace onto any of its items. Due to the lack of free-hand weapons, this is also buffed to be usable with any unarmed strike.
- **Invisible Ink:** Conceal an Object can't exist in the game. This is a 2-action activity to Trace a Rune and Hide/Sneak. Tracing a Rune also can no longer break stealth.
- **Read the Bones:** Augury can't exist in the game. This is a +1 status bonus to initiative.

#### Specific Runes
- Runes which are drawn on armor are instead drawn on a creature.
- **Atryl, Rune of Fire:** Due to lack of mechanical utility, this rune cannot be drawn on items.
- **Ranshu, Rune of Thunder:** Due to lack of mechanical utility, this rune cannot be drawn on items.
- **Kojastri, Rune of Insulation:** In spite of the playtest wording, I've made it so you can't damage yourself by using touch abilities with yourself on this rune. Your allies still can take damage, though.
- **Oljinex, Rune of Coward's Bane:** Due to technical limitations, the invocation imposes a DC 5 flat check on move actions -- DC 11 if it's to move away from the bearer. Its +1 AC status bonus currently applies to any ranged attack, not just physical (pending update).
- **Pluuna, Rune of Illumination:** The game doesn't do lightning mechanics. The existence of the light (both the passive and invocation effect) makes the bearer unable to be Undetected (they can still be Hidden).
- **Zohk, Rune of Homecoming:** The passive effect is a bespoke Stride used during your turn, rather than a benefit to any Stride.

### Attributions:
- <a href="https://www.flaticon.com/free-icons/feather" title="feather icons">Feather icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/rune" title="rune icons">Rune icons created by bearicons - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/rune" title="rune icons">Rune icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/comment" title="comment icons">Comment icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/wow" title="wow icons">Wow icons created by Vitaly Gorbachev - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/handmade" title="handmade icons">Handmade icons created by GOWI - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/knife" title="knife icons">Knife icons created by kerismaker - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/musical-note" title="musical note icons">Musical note icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/hammer" title="hammer icons">Hammer icons created by Smashicons - Flaticon</a>

## More Basic Actions
Adds more basic actions for characters to use.

### Prepare to Aid
**(Includes the Cooperative Nature human feat)**
Spend an action and select either an adjacent ally who's attack or skill check you want to aid, or select an adjacent enemy whose incoming attacks you'd like to aid, and what check you're preparing to aid.

When the trigger occurs, you make the same check against a flat DC, providing a bonus to the triggering roll depending on the results and your proficiency with the check.

_(Mod options allow you to adjust the DC)_

### Ready
Spend two actions to prepare to use one of the premade reactions. Options include:
- Brace: When a creature enters your reach, make a melee Strike against that creature.

### Help Up
When an adjacent ally is prone, you can spend an action to let them Stand as a free action.

_(Mod options allow you to adjust whether the ally actually takes a move action, or simply ceases to be prone)_

### Attributions
- <a href="https://www.flaticon.com/free-icons/help" title="help icons">Help icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/protection" title="protection icons">Protection icons created by Freepik - Flaticon</a>
- <a href="https://www.flaticon.com/free-icons/reaction" title="reaction icons">Reaction icons created by Freepik - Flaticon</a>
