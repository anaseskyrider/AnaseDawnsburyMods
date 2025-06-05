using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.StrategistSubclasses;

public static class ForensicMedicine
{
    public static Feat LoadSubclass()
    {
        Feat forensicMedicine = new Feat(
            ModData.FeatNames.ForensicMedicine,
            "You've learned that in most cases, especially murders, criminals tend to leave more evidence of the crime on the body than they'd like to believe. Information from bruising, bone fractures, blood spatters, and even the life cycle of carrion insects can provide valuable clues that help reconstruct the scene.",
            "You're trained in Medicine and gain the Battle Medicine skill feat. When you use Battle Medicine, on a success, the target recovers additional Hit Points equal to your level, and the target is temporarily immune for the rest of combat instead of 1 day.",
            [ModData.Traits.StrategistSubclasses],
            null)
            .WithOnSheet(values =>
            {
                values.GrantFeat(FeatName.Medicine);
                values.GrantFeat(FeatName.BattleMedicine);
            })
            .WithPermanentQEffect("Battle Medicine heals for more, and immunities to it only lasts for the encounter.",
                qfFeat =>
                {
                    qfFeat.YouBeginAction = async (qfThis, action) =>
                    {
                        if (!action.Name.Contains("Battle Medicine"))
                            return;
                        QEffect bonusHealing = new QEffect()
                        {
                            Name = "Forensic Bonus to Battle Medicine",
                            BonusToSelfHealing =
                                (qfThis2, action2) =>
                                {
                                    if (action2 != action)
                                        return null;
                                    qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                                    return action2 != action
                                        ? null
                                        : new Bonus(qfThis.Owner.Level, BonusType.Untyped, "Forensic Medicine");
                                },
                        };
                        action.ChosenTargets.ChosenCreature?.AddQEffect(bonusHealing);
                    };
                    qfFeat.AfterYouTakeAction = async (qfThis, action) =>
                    {
                        if (!action.Name.Contains("Battle Medicine"))
                            return;
                        QEffect temporaryImmunity = new QEffect()
                        {
                            // Debug Features
                            //Name = "Battle Medicine Immunity",
                            //Description = $"You are immune to Battle Medicine from {{Blue}}{qfThis.Owner.Name}{{/Blue}} for only the rest of the encounter.",
                            //Illustration = qfThis.Owner.Illustration,
                            EndOfCombat = async (qfThis2, won) =>
                            {
                                Creature healee = qfThis2.Owner;
                                Creature healer = qfThis.Owner;
                                // Default Battle Medicine
                                healee.PersistentUsedUpResources.UsedUpActions.Remove("BattleMedicineFrom:"+healer.Name);
                                // Dawnni Battle Medicine
                                healer.PersistentUsedUpResources.UsedUpActions.Remove("BattleMedicine:"+healee.Name);
                            },
                        };
                        action.ChosenTargets.ChosenCreature?.AddQEffect(temporaryImmunity);
                    };
                });
        ModManager.AddFeat(forensicMedicine);
        return forensicMedicine;
    }
}