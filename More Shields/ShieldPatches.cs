
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks.Monsters.L5;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreShields;

/// <summary>
/// Uses Harmony to overhaul internal behavior related to raising a shield, shield blocking, and more. Listed alterations are as follows:
/// </summary>
public static class ShieldPatches
{
    /// <summary>Possibilities now includes the ability to get a worn shield, not just a held shield.</summary>
    [HarmonyPatch(
        typeof(Possibilities),
        nameof(Possibilities.Create),
        [typeof(Creature), typeof(PossibilitiesRegenerationSpecifics)])]
    internal static class PatchPossibilitiesCreation
    {
        internal static void Postfix(Creature self, Possibilities __instance, ref Possibilities __result)
        {
            // TODO: Reorder list if possible.
            PossibilitySection? mainActions = __result.Sections.FirstOrDefault(sect =>
                sect.PossibilitySectionId == PossibilitySectionId.MainActions);
            PossibilitySection? itemActions = __result.Sections.FirstOrDefault(sect =>
                sect.PossibilitySectionId == PossibilitySectionId.ItemActions);

            Traverse TravPoss = Traverse.Create(typeof(Possibilities));
            
            foreach (Item shield in NewShields.GetAvailableShields(self)
                         .Where(shield => shield.HasTrait(Trait.Worn)))
            {
                // Shield bash
                Traverse? CreateStrike = TravPoss.Method(
                    "CreateItemStrikePossibility",
                    new Type[] { typeof(Creature), typeof(Item) });
                if (shield.HasTrait(Trait.Weapon) && shield.HasTrait(Trait.Melee))
                {
                    Possibility shieldBash = (Possibility)CreateStrike.GetValue(self, shield);
                    mainActions?.AddPossibility(shieldBash);
                    mainActions?.CollapseReadyPossibilities();
                }
                
                // Raise shield
                if (self.HasEffect(QEffectId.RaisingAShield))
                    continue;
                Possibility raiseShield = Fighter.CreateRaiseShield(self, shield);
                itemActions?.AddPossibility(raiseShield);
                itemActions?.CollapseReadyPossibilities();
                var AddCustom = TravPoss.Method(
                    "AddIntoCustomSections",
                    new Type[] { typeof(List<PossibilitySection>), typeof(Creature) });
                if (raiseShield is SubmenuPossibility shieldMenu)
                    AddCustom.GetValue(shieldMenu.Subsections, self);
                //itemActions?.Possibilities.Insert(1, raiseShield);
            }
        }
    }
    
    /// <summary>Raise a Shield is now a dramatically simpler action that flexibly becomes a submenu with the <see cref="ModData.Traits.ShieldActionFeat"/> trait.</summary>
    [HarmonyPatch(typeof(Fighter), nameof(Fighter.CreateRaiseShield))]
    internal static class PatchCreateRaiseShield
    {
        internal static bool Prefix(Creature self, Item shield, ref Possibility __result)
        {
            // Uses the feat ID as an identifier instead of the QEffect Id.
            bool hasShieldBlock = self.HasFeat(FeatName.ShieldBlock) || shield.HasTrait(Trait.AlwaysOfferShieldBlock);
            
            // Create action
            CombatAction raiseShield = NewShields.CreateRaiseShieldCore(self, shield, hasShieldBlock);
            Possibility possibleShield = new ActionPossibility(raiseShield)
                .WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ITEM_IN_HAND);
            
            // Return possibility of action
            // Create submenu if any feat has ShieldActionFeat trait (those feats are now responsible for inserting their actions into the submenu).
            if (self.PersistentCharacterSheet?.Calculated.AllFeats.Any(ft => ft.HasTrait(ModData.Traits.ShieldActionFeat)) ?? false)
                __result = new SubmenuPossibility(shield.Illustration, "Raise shield")
                {
                    SpellIfAny = raiseShield,
                    Subsections = [
                        new PossibilitySection("Raise shield")
                        {
                            Possibilities = [possibleShield]
                        }
                    ],
                    PossibilityGroup = Constants.POSSIBILITY_GROUP_ITEM_IN_HAND,
                };
            else
                __result = possibleShield;
            
            // Always overwrite the function.
            return false;
        }
    }

    /// <summary>Raise a Shield now derives its circumstance bonus to AC from its type of shield, and uses a generalized <see cref="ModData.Traits.CoverShield"/> trait instead of <see cref="Trait.TowerShield"/> when determining if the shield has a greater bonus.</summary>
    [HarmonyPatch(typeof(QEffect), nameof(QEffect.RaisingAShield))]
    internal static class PatchRaisingAShield
    {
        internal static void Postfix(bool shieldBlock, ref QEffect __result)
        {
            __result.StateCheck = qfThis =>
            {
                if (NewShields.GetAvailableShields(qfThis.Owner) is { Count: > 0 } shields)
                {
                    qfThis.Description = qfThis.Description?.Replace(
                        "+2",
                        "+" + shields.Max(NewShields.GetShieldAC));
                    return;
                }
                qfThis.ExpiresAt = ExpirationCondition.Immediately;
            };
            __result.BonusToDefenses = (qfThis, attackAction, targetDefense) =>
            {
                // Unchanged behavior
                if (targetDefense != Defense.AC
                    && (!qfThis.Owner.HasEffect(QEffectId.SparklingTarge)
                        || !qfThis.Owner.HasEffect(QEffectId.ArcaneCascade)
                        || !targetDefense.IsSavingThrow()
                        || attackAction == null
                        || !attackAction.HasTrait(Trait.Spell)))
                    return null;
                // Finds best shield
                List<Item> shields = NewShields.GetAvailableShields(qfThis.Owner);
                if (shields.Count == 0 || shields.MaxBy(NewShields.GetShieldAC) is not { } shield
                    || NewShields.GetShieldAC(shield) is not { } shieldAC)
                    return null;
                // Checks if it's a CoverShield instead of a TowerShield.
                return shield.HasTrait(ModData.Traits.CoverShield) && qfThis.Owner.HasEffect(QEffectId.TakingCover)
                    ? new Bonus(4, BonusType.Circumstance, "raised shield in cover")
                    : new Bonus(shieldAC, BonusType.Circumstance, "raised shield");
            };
        }
    }

    /// <summary>The ShieldBlock QEffect is now the effect that handles triggering your reaction. Executes a slightly-delayed FullCast of <see cref="NewShields.CreateShieldBlock"/>, and upgrades Sparkling Targe to scale with Greater Weapon Specialization.</summary>
    [HarmonyPatch(typeof(QEffect), nameof(QEffect.ShieldBlock))]
    internal static class PatchShieldBlock
    {
        internal static void Postfix(ref QEffect __result)
        {
            __result.Innate = false;
            __result.StateCheck = qfThis =>
            {
                if (NewShields.GetAvailableShields(qfThis.Owner).Count > 0)
                    return;
                qfThis.ExpiresAt = ExpirationCondition.Immediately;
            };
            // qfThis.Owner is the creature receiving damage reduction.
            // defender is the creature reducing the damage.
            // ShieldWarden passes the shield-user as the defender to the effect-owner.
            __result.YouAreDealtDamage = async (qfThis, attacker, damageStuff, defender) =>
            {
                if ((!damageStuff.Kind.IsPhysical()
                     || damageStuff.Power == null
                     || !damageStuff.Power.HasTrait(Trait.Attack))
                    && !Magus.DoesSparklingTargeShieldBlockApply(damageStuff, defender))
                    return null;
                
                Item shield = NewShields.GetAvailableShields(defender).MaxBy(itm => itm.Hardness)!; // Suppressed. This cannot normally be called without having a shield.
                CombatAction shieldBlock = NewShields.CreateShieldBlock(defender, shield, damageStuff.Power);
                
                int preventHowMuch = Math.Min(
                    shield.Hardness
                        + (defender.HasEffect(QEffectId.ShieldAlly) ? 2 : 0)
                        + (Magus.DoesSparklingTargeShieldBlockApply(damageStuff, defender) ? (defender.Level >= 15 ? 3 : defender.Level >= 7 ? 2 : 1) : 0),
                    damageStuff.Amount);
                
                if (!await ReactionsExpanded.AskToUseReaction2(
                        defender.Battle,
                        defender,
                        $"{{b}}Shield Block{{/b}} {{icon:Reaction}}\n{(qfThis.Owner == defender ? "You are" : qfThis.Owner + " is")} about to be dealt {damageStuff.Amount} damage by {{Blue}}{damageStuff.Power?.Name}{{/Blue}}.\nUse Shield Block to prevent {(preventHowMuch == damageStuff.Amount ? "all" : preventHowMuch) + " of that damage?"}",
                        shieldBlock))
                    return null;
                
                // Use a delay so that the action will "complete" after the damage is reduced.
                // Alternative design if this doesn't work: Give the attacker a QF at the end of their next action which makes you FullCast shield block.
                defender.AddQEffect(new QEffect()
                {
                    Name = "Delayed Shield Block FullCast",
                    StateCheckWithVisibleChanges = async qfThis2 =>
                    {
                        qfThis2.ExpiresAt = ExpirationCondition.Immediately;
                        await defender.Battle.GameLoop.FullCast(shieldBlock, ChosenTargets.CreateSingleTarget(attacker));
                    }
                });
                defender.Overhead(
                    "shield block",
                    Color.White,
                    defender + " uses {b}Shield Block{/b} to mitigate {b}" + preventHowMuch + "{/b} damage.",
                    shieldBlock.Name +" {icon:Reaction}",
                    shieldBlock.Description,
                    shieldBlock.Traits);
                Sfxs.Play(ModData.SfxNames.ShieldBlockWooodenImpact);
                return new ReduceDamageModification(preventHowMuch, "Shield block");
            };
        }
    }

    /// Speed calculation now uses the worst of Fortress Shield or Tower Shield.
    [HarmonyPatch(typeof(Creature), nameof(Creature.RecalculateLandSpeedAndInitiative))]
    internal static class PatchTowerShieldSpeedPenalty
    {
        internal static void Postfix(Creature __instance/*, ref int ___Speed*/)
        {
            Traverse Speed = Traverse.Create(__instance).Property("Speed");
            
            bool unburdenedIron = __instance.HasEffect(QEffectId.UnburdenedIron);
            bool hasTowerShield = __instance.HeldItems.Any(itm => itm.HasTrait(Trait.TowerShield));
            bool hasFortressShield = __instance.HeldItems.Any(itm => itm.HasTrait(ModData.Traits.FortressShield));
            
            int worstPenalty = hasFortressShield ? -2 : hasTowerShield ? -1 : 0;
            int finalPenalty = unburdenedIron ? Math.Min(worstPenalty+1, 0) : worstPenalty;
            
            if (hasTowerShield && !unburdenedIron && Speed.GetValue() is int value1)
                Speed.SetValue(value1 + 1); // reverse the original Tower Shield penalty
            if (Speed.GetValue() is int value2)
                Speed.SetValue(value2 + finalPenalty); // Apply final penalty
        }
    }

    /// Improves the item description of shields.
    [HarmonyPatch(typeof(RulesBlock), nameof(RulesBlock.GetItemDescriptionWithoutUsability))]
    internal static class PatchShieldDescriptions
    {
        internal static void Postfix(Item item, ref string __result)
        {
            // Formatted hardness
            string hardness = "{b}Hardness{/b} " + item.Hardness + "\n";
            
            // If the item has no hardness, skip.
            int index = __result.IndexOf(hardness);
            if (index == -1)
                return;

            // Formatted AC
            int? acBonus = NewShields.GetShieldAC(item); // Suppressed. Would never execute unless it was a shield.
            string? acString = acBonus != null ? "{b}AC{/b} +" + acBonus + (item.HasTrait(ModData.Traits.CoverShield) ? " (+4)" : null) + "\n" : null;
            
            // Formatted speed penalty
            int speedPenalty = item.HasTrait(ModData.Traits.FortressShield)
                ? -2
                : item.HasTrait(Trait.TowerShield)
                    ? -1
                    : 0;
            string? speedString = speedPenalty < 0
                ? "{b}Speed Penalty{/b} " + speedPenalty*5 + " ft.\n"
                : null;

            // New format
            string[] details = [];
            if (acString != null)
                details = details.Append(acString).ToArray();
            details = details.Append(hardness).ToArray();
            if (speedString != null)
                details = details.Append(speedString).ToArray();

            __result = __result.Replace(hardness, details.Join(null, ""));
        }
    }

    /// Ensures the Doorwarden will still work with the new Aggressive Block.
    [HarmonyPatch(typeof(Doorwarden), nameof(Doorwarden.Create))]
    internal static class PatchDoorwarden
    {
        internal static void Postfix(Creature __result)
        {
            __result.RemoveAllQEffects(qf => qf.Id is QEffectId.AggressiveBlock);
            __result.WithFeat(FeatName.AggressiveBlock);
        }
    }
}