using Dawnsbury.Core;

namespace Dawnsbury.Mods.RunesmithClass.RuneRules;

public enum RuneId
{
    None = 0,
    
    // Unused illustrations:
    // IllustrationName.UnderwaterRunestone
    

    #region Level 1 Runes

    [RuneId("Rune of Fire", 1, IllustrationName.FlamingRunestone)]
    Atryl = 1,
    [RuneId("Rune of Hold’s Bravery", 1, IllustrationName.ResilientRunestone)]
    Baruiel = 2,
    [RuneId("Rune of Perplexity", 1, IllustrationName.RunestoneImplacable)]
    Camonica = 3,
    [RuneId("Rune of Whetstones", 1, IllustrationName.WoundingRunestone)]
    Esvadir = 4,
    [RuneId("Rune of Dwarven Ramparts", 1, IllustrationName.ArmorPotencyRunestone)]
    Holtrik = 5,
    [RuneId("Rune of Appeal", 1, IllustrationName.BrilliantRunestone)]
    Ledria = 6,
    [RuneId("Rune of Frost", 1, IllustrationName.FrostRunestone)]
    Lyskel = 7,
    [RuneId("Rune of Impact", 1, IllustrationName.ThunderingRunestone)]
    Marssyl = 8,
    [RuneId("Rune of Cowards’ Bane", 1, IllustrationName.FearsomeRunestone)]
    Oljinex = 9,
    [RuneId("Rune of Illumination", 1, IllustrationName.DisruptingRunestone)]
    Pluuna = 10,
    [RuneId("Rune of Thunder", 1, IllustrationName.ShockRunestone)]
    Ranshu = 11,
    [RuneId("Rune of Bestial Might", 1, IllustrationName.GrievousRunestone)]
    Rehgog = 12,
    [RuneId("Rune of Preparedness", 1, IllustrationName.RunestoneAimAiding)]
    Sertum = 13,
    [RuneId("Rune of Corrosion", 1, IllustrationName.CorrosiveRunestone)]
    Thullax = 14,
    [RuneId("Rune of Vocabulary", 1, IllustrationName.ShiftingRunestone)]
    Tilus = 15,
    [RuneId("Rune of Homecoming", 1, IllustrationName.ReturningRunestone)]
    Zohk = 16,

    #endregion

    #region Level 5 Runes

    [RuneId("Diacritic Rune of Succession", 5, IllustrationName.RunestoneWinged)]
    Av = 17,
    [RuneId("Diacritic Rune of Expansion", 5, IllustrationName.RunestoneEnergyAdaptive)]
    En = 18,
    [RuneId("Diacritic Rune of Doubling", 5, IllustrationName.DoublingRings)]
    Fob = 19,
    [RuneId("Diacritic Rune of Mercy", 5, IllustrationName.Silver)]
    Kit = 20,
    [RuneId("Diacritic Rune of Continuum", 5, IllustrationName.RunestoneWinged)]
    Per = 21,
    [RuneId("Diacritic Rune of Preservation", 5, IllustrationName.RunestoneWinged)]
    Sun = 22,
    [RuneId("Diacritic Rune of Fundaments", 5, IllustrationName.Adamantine)]
    Ti = 23,
    [RuneId("Diacritic Rune of Intensity", 5, IllustrationName.DemolishingRunestone)]
    Ur = 24,

    #endregion

    #region Level 9 Runes

    [RuneId("Rune of Submersion", 9, IllustrationName.RunestoneSpellbreaking)]
    Astillu = 25,
    [RuneId("Rune of Leeching", 9, IllustrationName.KeenRunestone)]
    Cruonign = 26,
    [RuneId("Rune of Gravity", 9, IllustrationName.CrushingRunestone)]
    Feikris = 27,
    [RuneId("Rune of Partnership", 9, IllustrationName.AnarchicRunestone)]
    Germantria = 28,
    [RuneId("Rune of Observation", 9, IllustrationName.GhostTouchRunestone)]
    Ichelsu = 29,
    [RuneId("Rune of Dragon Fury", 9, IllustrationName.ElementalGem)]
    Jurroz = 30,
    [RuneId("Rune of Insulation", 9, IllustrationName.RunestoneQuenching)]
    Kojastri = 31,
    [RuneId("Rune of Inarticulateness", 9, IllustrationName.RunestoneDread)]
    Oraloq = 32,
    [RuneId("Rune of Transposition", 9, IllustrationName.RunestoneAdvancing)]
    Piteregrin = 33,
    [RuneId("Rune of Forlorn Sorrow", 9, IllustrationName.NightmareRunestone)]
    Trolistri = 34,
    [RuneId("Rune of Restraint", 9, IllustrationName.Fortification)]
    Ulgatus = 35,
    [RuneId("Rune of Remonstrance", 9, IllustrationName.RunestoneInvisibility)]
    Yudici = 36,

    #endregion

    #region Level 13 Runes

    [RuneId("Diacritic Rune of Phantasma", 13, IllustrationName.ImpactfulRunestone)]
    Eck = 37,
    [RuneId("Diacritic Rune of Corruption", 13, IllustrationName.UnholyRunestone)]
    Inth = 38,
    [RuneId("Diacritic Rune of Contingency", 13, IllustrationName.RunestoneAntimagic)]
    Nesh = 39,
    [RuneId("Diacritic Rune of Righteousness", 13, IllustrationName.HolyRunestone)]
    Sar = 40,

    #endregion

    #region Level 17 Runes

    [RuneId("the Elf-Gate Key", 17)]
    Aiuen = 41,
    [RuneId("the Poisoned Star", 17)]
    Ochygholl = 42,
    [RuneId("Seal of the Dead Vault", 17, IllustrationName.AxiomaticRunestone)]
    Rovan = 43,
    [RuneId("the Well of Virtues", 17)]
    Xinsala = 44,

    #endregion
}