using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.StatBlocks;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.KholoAncestry;

public static class KholoAncestry
{
    public static List<Trait> KholoWeapons = [
        ModData.Traits.Kholo,
        ModData.Traits.FlailItself,
    ];
    
    public static void LoadAncestry()
    {
        if (ModManager.TryParse("Khopesh", out Trait khopesh))
            KholoWeapons.Add(khopesh);
        if (ModManager.TryParse("Mambele", out Trait mambele))
            KholoWeapons.Add(mambele);
        if (ModManager.TryParse("WarFlail", out Trait warFlail))
            KholoWeapons.Add(warFlail);
        
        LoadFeatures();
        
        Feat kholoAncestry = new AncestrySelectionFeat(
                ModData.FeatNames.KholoAncestry,
                "Kholos are hyena-headed humanoids who embrace practicality and pragmatism. They have bad reputations as brutal raiders and demon-worshipers. Many believe that kholos are witches, cannibals, and worse. The truth is more complex. Kholos are eminently practical and pragmatic hunters and raiders. To them, honor is just another word for pointless risk.",
                [Trait.Humanoid, ModData.Traits.Kholo],
                8, (25/5),
                [new EnforcedAbilityBoost(Ability.Strength), new EnforcedAbilityBoost(Ability.Intelligence), new FreeAbilityBoost()],
                [..CreateHeritages()])
            .WithAbilityFlaw(Ability.Wisdom)
            .WithSpecialRules(
                "{b}Enhanced Senses{/b} {i}You have enhanced sensory anatomy.{/i} You gain a +1 circumstance bonus to checks made to Seek.\n\n"
                + "{b}Bite{/b} {i}Your sharp teeth and powerful jaws are fearsome weapons.{/i} You have a jaws unarmed attack that deals 1d6 piercing damage. Your jaws are in the brawling group.")
            .WithOnSheet(values =>
            {
                values.GrantFeat(ModData.FeatNames.EnhancedSenses);
                values.GrantFeat(ModData.FeatNames.Bite);
            });
        ModManager.AddFeat(kholoAncestry);
    }

    public static void LoadFeatures()
    {
        Feat enhancedSenses = new Feat(
                ModData.FeatNames.EnhancedSenses,
                "You have enhanced sensory anatomy.",
                "You gain a +1 circumstance bonus to checks made to Seek.",
                [], null)
            .WithPermanentQEffect(
                "You gain a +1 circumstance bonus to checks made to Seek.",
                qfFeat =>
                {
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.KholoCave))
                        qfFeat.Description = qfFeat.Description?.Replace("+1", "{Blue}+2{/Blue}");
                    qfFeat.BonusToAttackRolls = (qfThis, seek, defender) =>
                    {
                        if (defender == null || seek.ActionId != ActionId.Seek)
                            return null;
                        int amount = qfThis.Owner.HasFeat(ModData.FeatNames.KholoCave) ? 2 : 1;
                        return new Bonus(amount, BonusType.Circumstance, "Enhanced senses");
                    };
                });
        ModManager.AddFeat(enhancedSenses);

        Feat bite = new Feat(
                ModData.FeatNames.Bite,
                "Your sharp teeth and powerful jaws are fearsome weapons.",
                "You have a jaws unarmed attack that deals 1d6 piercing damage. Your jaws are in the brawling group.",
                [], null)
            .WithPermanentQEffect(
                null,
                qfFeat =>
                {
                    Trait[] traits = [];
                    if (qfFeat.Owner.HasFeat(ModData.FeatNames.Crunch))
                        traits = traits.Append(Trait.VersatileB).ToArray();
                    qfFeat.AdditionalUnarmedStrike = NaturalWeapons.Create(
                        NaturalWeaponKind.Jaws,
                        "1d" + (qfFeat.Owner.HasFeat(ModData.FeatNames.Crunch) ? 8 : 6),
                        DamageKind.Piercing,
                        traits);
                });
        ModManager.AddFeat(bite);
    }

    public static IEnumerable<Feat> CreateHeritages()
    {
        // Ant Kholo
        yield return new Feat(
                ModData.FeatNames.KholoAnt,
                "You're a sharp-featured, big-eared kholo about 3 feet tall. Many are skeptical that you are in fact a kholo.",
                "You are trained in Deception.\n\n"
                    + ModData.Illustrations.DawnsburySun.IllustrationAsIconString
                    + " {b}Modding{/b} If the {i}Exploration Activities{/i} mod is installed, you also gain its Deceptive Approach skill feat without needing to meet its prerequisites.",
                [Trait.Mod], null)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Deception);
                if (ModManager.TryParse("DeceptiveApproach", out FeatName approach))
                    values.GrantFeat(approach);
            });
        // Cave Kholo
        yield return new Feat(
                ModData.FeatNames.KholoCave,
                "Storytellers spin ancient tales claiming that kholo lived in caves and underground before most of your kind ventured into the light. You're a throwback to these ancients, with a broad chest and markings that resemble short black slashes instead of spots. Your eyes are developed to see perfectly in the dark, a valuable advantage to your clan.",
                "Increase the circumstance bonus from your enhanced senses feature to a +2 instead of a +1. You also gain this bonus to saves against effects with the Light trait.",
                [Trait.Mod], null)
            .WithPermanentQEffect(
                "You have a +2 circumstance bonus to saves against light effects.",
                qfFeat =>
                {
                    qfFeat.BonusToDefenses = (_, action, def) =>
                        (action?.HasTrait(Trait.Light) ?? false) && def.IsSavingThrow()
                            ? new Bonus(2, BonusType.Circumstance, "Cave kholo")
                            : null;
                });
        // Dog Kholo
        yield return new Feat(
                ModData.FeatNames.KholoDog,
                "You're a nimble-bodied kholo with a prehistoric, almost dog-like build, who moves like a quadruped but fights like a biped.",
                "While you have both hands free, your Speed increases to 30 feet as you run on all fours.",
                [Trait.Mod], null)
            .WithOnCreature(self =>
                ++self.BaseSpeed);
        // Great Kholo
        yield return new Feat(
                ModData.FeatNames.KholoGreat,
                "You're an imposing, powerful kholo, with tawny fur and brown spots on your hide.",
                "You gain 10 Hit Points from your ancestry instead of 8 and gain a +1 circumstance bonus to Athletics checks to Reposition, Shove, or Trip.",
                [Trait.Mod], null)
            .WithOnCreature(cr =>
                cr.MaxHP += 2)
            .WithPermanentQEffect(
                "You gain a +1 circumstance bonus to Athletics checks to Reposition, Shove, or Trip.",
                qfFeat =>
                {
                    qfFeat.BonusToAttackRolls = (qfThis, action, defender) =>
                    {
                        if (action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill != Skill.Athletics)
                            return null;

                        if (action.ActionId is ActionId.Shove or ActionId.Trip
                            || (ModManager.TryParse("Reposition", out ActionId repo)
                                && action.ActionId == repo)
                            || (ModManager.TryParse("FC_Reposition", out ActionId commanderRepo)
                                && action.ActionId == commanderRepo))
                            return new Bonus(1, BonusType.Circumstance, "Great kholo");
                        else
                            return null;
                    };
                });
        // Sweetbreath Kholo
        yield return new Feat(
                ModData.FeatNames.KholoSweetbreath,
                "You're a striped, pale-furred kholo with oddly pleasant breath, which you can use to entrance your prey.",
                "You are trained in Diplomacy.\n\n"
                + ModData.Illustrations.DawnsburySun.IllustrationAsIconString
                + " {b}Modding{/b} If the {i}Exploration Activities{/i} mod is installed, you also gain its Glad-Hand skill feat without needing to meet its prerequisites.",
                [Trait.Mod], null)
            .WithOnSheet(values =>
            {
                values.TrainInThisOrSubstitute(Skill.Diplomacy);
                if (ModManager.TryParse("GladHand", out FeatName approach))
                    values.GrantFeat(approach);
            });
        // Winter Kholo
        yield return new Feat(
                ModData.FeatNames.KholoWinter,
                "You're a hardy kholo covered in thick, tufted fur that makes you able to survive in the harsh winters of the colder territories.",
                "You gain cold resistance equal to half your level (minimum 1).",
                [Trait.Mod], null)
            .WithOnCreature(self =>
            {
                self.AddQEffect(QEffect.DamageResistance(DamageKind.Cold, Math.Max(1, self.Level / 2)));
            });
        // Witch Kholo
        yield return new Feat(
                ModData.FeatNames.KholoWitch,
                "You're a shaggy, dark-furred kholo capable of making some truly uncanny sounds.",
                "Choose any one occult cantrip. You can cast it at-will as an innate spell. In addition, you gain a +1 circumstance bonus to checks to Create a Diversion, as your voice lends to your distractions.",
                [Trait.Mod], null)
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.Spell, Proficiency.Trained);
                values.InnateSpells.GetOrCreate(
                    ModData.Traits.Kholo,
                    () => new InnateSpells(Trait.Occult));
                values.AddSelectionOptionRightNow(
                    new AddInnateSpellOption(
                        "ExtraKholoCantrip",
                        "Occult cantrip",
                        -1,
                        ModData.Traits.Kholo,
                        0,
                        spell => spell.HasTrait(Trait.Occult)));
            })
            .WithPermanentQEffect(
                "You have a +1 circumstance bonus to Create a Diversion.",
                qfFeat =>
                {
                    qfFeat.BonusToSkillChecks = (_, action, _) =>
                        action.ActionId is ActionId.CreateADiversion
                            ? new Bonus(1, BonusType.Circumstance, "Witch kholo")
                            : null;
                });
    }
}