using System.Runtime.CompilerServices;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Options.Reactive;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.MoreShields;

public static class CommonShieldRules
{
    public static readonly ConditionalWeakTable<Item, ShieldProperties> ShieldPropertyList = [];

    public class ShieldProperties
    {
        private int _maxHP;
        private int _hp;
        
        public required Item Shield { init; get; }
        
        public int HP
        {
            set => this._hp = Math.Clamp(value, 0, this.MaxHP);
            get => this._hp;
        }

        public int MaxHP
        {
            set => this._maxHP = Math.Max(0, value);
            get => this._maxHP + (this.IsShieldAlly ? this._maxHP / 2 : 0);
        }
        
        //public int Hardness => Shield.Hardness;
        //public int AC { set; get; }
        //public bool CanTakeCoverBehind { set; get; }

        /// <summary>
        /// If true, adds 50% to the shield's max HP.
        /// </summary>
        public bool IsShieldAlly
        {
            set
            {
                // Is set to true when it was not previously,
                // and isn't already broken (minimum of 2 due to
                // the next elif block)
                if (value && !field && this._hp > 1)
                {
                    // Add bonus HP
                    this.HP += this._maxHP / 2;
                }
                // Is set to false when it was not previously
                else if (!value && field && this._hp > 0)
                {
                    // Remove bonus HP, but not to the point of
                    // breaking it.
                    this.HP = Math.Max(this.HP - (this._maxHP / 2), 1);
                }
                
                // Update
                field = value;
            }
            get;
        }
    }

    extension(Item shield)
    {
        public Item AddShieldProperties(int maxHP)
        {
            ShieldProperties newProps = new ShieldProperties()
            {
                Shield = shield,
                MaxHP = maxHP,
                HP = maxHP,
            };
            CommonShieldRules.ShieldPropertyList.TryAdd(shield, newProps);
            return shield;
        }

        public ShieldProperties? GetShieldProperties()
        {
            return CommonShieldRules.ShieldPropertyList
                .TryGetValue(shield, out ShieldProperties? value)
                ? value
                : null;
        }
    }

    // These functions relate to shield items and their statistics.
    #region Shield Item Functions

    /// <summary>Gets a list of shields being wielded or worn by a creature.</summary>
    public static List<Item> GetWieldedShields(Creature owner)
    {
        List<Item> heldShields = owner.HeldItems
            .Where(it => it.HasTrait(Trait.Shield))
            .ToList();
        return owner.HasFreeHand ||
               owner.HeldItems.Any(held => !held.HasTrait(Trait.Weapon) && !held.HasTrait(Trait.Grapplee))
            ? heldShields
                .Union(owner.CarriedItems
                    .Where(it => it.HasTrait(Trait.Shield) && it.IsWorn))
                .ToList()
            : heldShields;
    }

    /// <summary>Gets whether a given creature is holding or wearing this shield. Returns false if the shield is held but stowed, or worn but not worn correctly.</summary>
    public static bool IsShieldWielded(Creature owner, Item shield)
    {
        return GetWieldedShields(owner).Contains(shield);
    }

    /// <summary>Gets the circumstance bonus to AC of an item, if it's a shield.</summary>
    public static int? GetAC(Item shield)
    {
        if (!shield.HasTrait(Trait.Shield)/* && !shield.HasTrait(Trait.AlwaysOfferShieldBlock)*/)
            return null;
        if (shield.HasTrait(ModData.Traits.HeavyShield))
            return 3;
        if (shield.HasTrait(ModData.Traits.MediumShield))
            return 2;
        if (shield.HasTrait(ModData.Traits.LightShield))
            return 1;
        return 2; // Fallback value.
    }

    /// <summary>Gets a list of shields currently raised by this creature.</summary>
    public static List<Item> GetRaisedShields(Creature owner)
    {
        return owner.QEffects
            .Where(qf => qf.Id is QEffectId.RaisingAShield)
            .Select(qf => qf.Tag as Item)
            .WhereNotNull()
            .ToList();
    }
    
    /// <summary>
    /// Gets a list of shields that are legally usable for Shield Block.
    /// </summary>
    public static List<Item> GetBlockableShields(Creature owner)
    {
        List<Item> blockables = GetRaisedShields(owner);
        if (owner.HasEffect(QEffectId.ShieldBlock))
            return blockables;
        else
            return blockables.Where(shield =>
                shield.HasTrait(Trait.AlwaysOfferShieldBlock))
                .ToList();
    }

    #endregion
    
    #region Shield Effects

    /// <summary>
    /// Adds an invisible QEffect which doesn't expire that adds the listed amount of bonus hardness to Shield Block reaction events.
    /// </summary>
    /// <param name="bonus">An untyped bonus to apply to all shield block events.</param>
    /// <param name="bonusSource">The bonus name.</param>
    /// <param name="type">(Default: untyped) The bonus type.</param>
    public static QEffect BonusToShieldHardness(int bonus, string bonusSource, BonusType type = BonusType.Untyped)
    {
        return BonusToShieldHardness((_,_,_,_,_) =>
            new Bonus(bonus, type, bonusSource));
    }

    /// <summary>
    /// Adds an invisible QEffect which doesn't expire that adds a Bonus to hardness to Shield Block reaction events.
    /// </summary>
    /// <param name="shouldApply">A lambda function which takes in the ATTACKER, the DAMAGESTUFF, the TARGET of the damage, and the one BLOCKING it. It returns the bonus to apply to the shield block event.</param>
    public static QEffect BonusToShieldHardness(Func<Creature,DamageEvent,Creature,Creature,Item,Bonus?> shouldApply)
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.BonusToHardness,
            Tag = shouldApply,
        };
    }

    /// <summary>
    /// Gets the total bonuses to hardness for Shield Block events.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="dEvent"></param>
    /// <param name="target"></param>
    /// <param name="blocker"></param>
    /// <param name="shield"></param>
    /// <returns></returns>
    public static int GetShieldBlockHardnessBonuses(
        Creature attacker,
        DamageEvent dEvent,
        Creature target,
        Creature blocker,
        Item shield)
    {
        List<Bonus?> bonuses = [];
        foreach (QEffect qf in blocker.QEffects.Where(qf => qf.Id == ModData.QEffectIds.BonusToHardness))
        {
            if (qf.Tag is Func<Creature, DamageEvent, Creature, Creature, Item, Bonus?> bonusToHardness)
                bonuses.Add(bonusToHardness.Invoke(attacker, dEvent, target, blocker, shield));
        }

        return Bonus.Sum(bonuses, false).BonusTotal;
    }

    /// <summary>
    /// A broken shield cannot be raised nor used for Shield Block.
    /// </summary>
    /// <remarks>
    /// This is an Ephemeral effect. Add this as part of a <see cref="QEffect.StateCheck"/>.
    /// </remarks>
    /// <param name="shield">The shield that's broken.</param>
    public static QEffect BrokenShield(Item shield)
    {
        return new QEffect(
            "Broken shield",
            $"Your {shield.Illustration.IllustrationAsIconString}{shield.Name} shield is broken. You cannot Strike with it, raise it, nor Shield Block {{icon:Reaction}} with it.",
            ExpirationCondition.Ephemeral,
            null,
            IllustrationName.Broken)
        {
            Id = ModData.QEffectIds.BrokenShield,
            Tag = shield,
            PreventTakingAction = action =>
            {
                switch (action.ActionId)
                {
                    // Don't prevent all the basic Interact actions.
                    case ActionId.DrawItem or ActionId.DropItem
                        or ActionId.HandOverItem or ActionId.PickUpItem
                        or ActionId.ReplaceItemInHand or ActionId.StowItem
                        or ActionId.OpenADoor:
                    // Special rules compatibility
                    case ActionId.RaiseShield
                        when action.Owner.HasEffect(ModData.QEffectIds.CanRaiseBrokenShields):
                        return null;
                    default:
                        return action.Item == shield
                            ? "Broken shield"
                            : null;
                }
            },
            StateCheckLayer = 1,
            StateCheck = qfThis =>
            {
                qfThis.Owner.RemoveAllQEffects(qf =>
                    qf.Id is QEffectId.RaisingAShield
                    && qf.Tag == shield);
            }
        };
    }
    
    #endregion

    #region Shield Abilities

    /// <summary>
    /// Whether the BLOCKING CREATURE can block this DAMAGE EVENT.
    /// </summary>
    public static bool DoesShieldBlockApply(Creature blocker, DamageEvent dEvent)
    {
        DamageStuff dStuff = new DamageStuff(
            dEvent.TotalResolvedDamage,
            dEvent.CombatAction,
            dEvent.KindedDamages.First().DamageKind);
        
        return (dStuff.Kind.IsPhysical()
                && dStuff.Power != null
                && dStuff.Power.HasTrait(Trait.Attack)
                && dStuff.Power.ActionId != ActionId.Trip)
               || blocker.QEffects.Any(qff =>
                   qff.YourShieldBlockWorksAlsoAgainst?.Invoke(qff, dEvent) ?? false);
    }

    /// <summary>
    /// Does the Reflexive Shield feat apply to this action.
    /// </summary>
    /// <param name="power">The CombatAction being checked against, usually an action that imposes a saving throw.</param>
    public static bool DoesReflexiveShieldApply(CombatAction? power)
    {
        return power?.SavingThrow?.Defense is Defense.Reflex
               || power?.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense is Defense.Reflex;
    }
    
    /// <summary>
    /// Functions as <see cref="Magus.DoesSparklingTargeShieldBlockApply"/> but with a different overload and accepts magical Strikes (which CountsAsMagical currently excludes).
    /// </summary>
    public static bool DoesSparklingTargeShieldBlockApply(CombatAction? power, Creature magus)
    {
        return magus.HasEffect(QEffectId.SparklingTarge)
               && magus.HasEffect(QEffectId.ArcaneCascade)
               && power is { CountsAsMagical: true };
    }

    #endregion

    // These functions relate to raising a shield.
    #region Raising a Shield

    /// <summary>Gets your current possibilities and looks for any action with <see cref="ActionId.RaiseShield"/> and offers it as an option (if multiple are present).</summary>
    /// <param name="self">The Creature raising the shield.</param>
    /// <param name="onlyWhat">Additional filters on allowed actions, such as shields that wouldn't be able to cross a threshold with Reactive Shield.</param>
    /// <returns>(bool) Whether the creature has a <see cref="QEffectId.RaisingAShield"/> effect.</returns>
    public static async Task<bool> OfferToRaiseAShield(Creature self, Func<CombatAction, bool>? onlyWhat = null)
    {
        Possibilities raiseShields = self.Possibilities.Filter(ap =>
        {
            if (ap.CombatAction.ActionId != ActionId.RaiseShield)
                return false;
            if (onlyWhat?.Invoke(ap.CombatAction) == false)
                return false;
            ap.CombatAction.ActionCost = 0;
            ap.RecalculateUsability();
            return true;
        });
        
        var active = self.Battle.ActiveCreature;
        self.Battle.ActiveCreature = self;
        self.Possibilities = raiseShields;
        
        List<Option> actions = await self.Battle.GameLoop.CreateActions(
            self,
            raiseShields,
            null);
        self.Battle.GameLoopCallback.AfterActiveCreaturePossibilitiesRegenerated();
        await self.Battle.GameLoop.OfferOptions(self, actions, true);
        
        self.Battle.ActiveCreature = active;
        
        return self.HasEffect(QEffectId.RaisingAShield);
    }

    #endregion

    // These functions relate to using Shield Block.
    #region Shield Block
    
    /// <summary>
    /// This functions as <see cref="Fighter.ShieldBlockYouAreDealtDamageReaction"/>, except it no longer checks for stuff like whether damage is physical, and contains visual several enhancements. It uses a ReactionOption associated with a CombatAction for more detailed tooltip information.
    /// </summary>
    /// <param name="adjustReaction">If this Shield Block reaction is special in any meaningful way, adjust it here.</param>
    public static ReactionOptions ShieldBlockYouAreDealtDamageReaction2(
        // ReSharper disable once InvalidXmlDocComment
        DamageEvent damageEvent,
        // ReSharper disable once InvalidXmlDocComment
        Creature targetedCreature,
        // ReSharper disable once InvalidXmlDocComment
        Creature blockingCreature,
        // ReSharper disable once InvalidXmlDocComment
        Item shield,
        Action<ReactionOption>? adjustReaction = null)
    {
        DamageStuff damageStuff = new DamageStuff(
            damageEvent.TotalResolvedDamage,
            damageEvent.CombatAction,
            damageEvent.KindedDamages.First().DamageKind);

        int hardness = shield.Hardness + CommonShieldRules.GetShieldBlockHardnessBonuses(damageEvent.Source, damageEvent, targetedCreature, blockingCreature, shield);
        int preventHowMuch = Math.Min(hardness, damageStuff.Amount);

        CombatAction displayReaction = ShieldBlockAction(damageEvent, targetedCreature, blockingCreature, shield, hardness, preventHowMuch);

        // Commented out because I'm not currently using this function anyway.
        /*if (!displayReaction.CanBeginToUse(blockingCreature))
            return null;*/
        
        string whatDamage = DoesSparklingTargeShieldBlockApply(damageStuff.Power, blockingCreature)
            ? "{Blue}magical{/Blue} damage"
            : "damage";

        ReactionOption reaction = ReactionOption.CreateFromCombatActionCustom(
                displayReaction,
                $"{shield.Illustration.IllustrationAsIconString} Prevent {S.AllOrNumber(preventHowMuch, damageStuff.Amount)} of this {whatDamage}.",
                async () =>
                {
                    // Adds an impact sound
                    Sfxs.Play(ModData.SfxNames.ShieldBlockWoodenImpact);

                    foreach (QEffect qf in blockingCreature.QEffects.ToList())
                        await qf.WhenYouUseShieldBlock.InvokeIfNotNull(qf, damageEvent.Source, targetedCreature,
                            preventHowMuch);

                    // Touched up overhead:
                    // - add reaction symbol to overhead
                    // - add CombatAction-like description
                    blockingCreature.Overhead(
                        "Shield Block {icon:Reaction}", // Don't include the item name
                        Color.White,
                        $"{blockingCreature} uses {{b}}Shield Block{{/b}} {{icon:Reaction}} to mitigate {{b}}{preventHowMuch}{{/b}} damage.",
                        displayReaction.Name + " {icon:Reaction}",
                        displayReaction.Description,
                        displayReaction.Traits);

                    damageEvent.ReduceBy(preventHowMuch, "Shield block");
                    shield.GetShieldProperties()?.HP -= damageEvent.TotalResolvedDamage;
                })
            .WithTraits(Trait.ShieldBlock)
            .WithIsReaction();
        adjustReaction?.Invoke(reaction);
        
        return reaction;
    }

    /// <summary>
    /// Creates a Shield Block combat action used for display purposes only. The optional parameters let you specify the effects of a specific event.
    /// </summary>
    public static CombatAction ShieldBlockAction(DamageEvent dEvent, Creature target, Creature blocker, Item? shield,  int? hardness, int preventHowMuch)
    {
        CombatAction shieldBlock = new CombatAction(
                blocker,
                ModData.Illustrations.ShieldBlock,
                "Shield Block" + (shield is not null ? " ("  + shield.ShortName + ")" : ""),
                [ModData.ModTrait, Trait.General, Trait.ShieldBlock],
                $$"""
                  {i}You snap your shield in place to ward off a blow.{/i}

                  {b}Trigger{/b} While you have your shield raised, you would take damage from a physical attack.

                  Your {{(shield is not null ? shield.Illustration.IllustrationAsIconString + shield.Name.WithColor("Blue") : "shield")}} prevents you from taking up to {{hardness?.WithColor("Blue") ?? "the shield's hardness in"}} damage. You and the shield take any remaining damage, possibly breaking the shield.
                  """,
                Target.Self())
            .WithActionCost(-2)
            // Adds an impact sound
            .WithSoundEffect(ModData.SfxNames.ShieldBlockWoodenImpact)
            // Track the triggering damage event
            .WithTag(dEvent)
            .WithItem(shield!)
            .WithEffectOnSelf(async (action, self) =>
            {
                foreach (QEffect qf in blocker.QEffects.ToList())
                    await qf.WhenYouUseShieldBlock.InvokeIfNotNull(qf, dEvent.Source, target,
                        preventHowMuch);

                // Touched up overhead:
                // - add reaction symbol to overhead
                // - add CombatAction-like description
                /*blockingCreature.Overhead(
                    "Shield Block {icon:Reaction}", // Don't include the item name
                    Color.White,
                    $"{blockingCreature} uses {{b}}Shield Block{{/b}} {{icon:Reaction}} to mitigate {{b}}{preventHowMuch}{{/b}} damage.",
                    action.Name + " {icon:Reaction}",
                    action.Description,
                    action.Traits);*/

                // Reduce damage taken
                dEvent.ReduceBy(preventHowMuch, "Shield block");
                
                // Shield damage
                if (shield is not null
                    && !PlayerProfile.Instance.IsBooleanOptionEnabled(
                        ModData.BooleanOptions.ShieldsDontTakeDamage))
                {
                    ShieldProperties? shieldProps = shield.GetShieldProperties();
                    // Log the shield's taken damage
                    int? beforeHP = shieldProps?.HP;
                    if (beforeHP is not null)
                    {
                        blocker.Battle.Log(
                            $"{blocker.Name}'s {shield.Illustration.IllustrationAsIconString}{shield.Name} takes {dEvent.TotalResolvedDamage.WithTag("b")} damage.",
                            "Shield damage",
                            $$"""
                              {b}{{beforeHP}}{/b} Hit points {i}({{shield.Name}}){/i}
                              {b}-{{dEvent.TotalResolvedDamage}}{/b} Unblocked damage
                              {b}= {{Math.Max(0, (int)beforeHP - dEvent.TotalResolvedDamage)}} Remaining hit points{/b}
                              
                              {i}(Shield item damage can be disabled in {b}Settings / Mod settings{/b}.){/i}
                              """,
                            new Traits([ModData.ModTrait]));
                    }
                    
                    // Apply item damage
                    shieldProps?.HP -= dEvent.TotalResolvedDamage;
                    
                    // Announce broken shield event.
                    // Do not apply effect as that's handled by the item itself
                    // immediately after this in a StateCheck.
                    if (shieldProps?.HP is 0
                        && !blocker.HasEffect(ModData.QEffectIds.BrokenShield))
                    {
                        blocker.Overhead(
                            "*broken shield*",
                            Color.Red,
                            $"{blocker.Name}'s {shield.Illustration.IllustrationAsIconString}{shield.Name} breaks.");
                    }
                }
            });
        if (shield is not null)
            shieldBlock.WithItem(shield);
        return shieldBlock;
    }

    #endregion
}