using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Dawnsbury.Mods.DawnniExpanded;
using static Dawnsbury.Core.CharacterBuilder.FeatsDb.TrueFeatDb.BarbarianFeatsDb.AnimalInstinctFeat;

namespace Dawnsbury.Mods.MoreDedications;
public class MoreDedications
{
    public static Trait ModNameTrait;
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ///////////////
        // Mod Trait //
        ///////////////
        ModNameTrait = ModManager.RegisterTrait("MoreDedications", new TraitProperties("More Dedications", true));
        
        ////////////////////
        // New Archetypes //
        ////////////////////
        ArchetypeMauler.LoadMod();
        ArchetypeArcher.LoadMod();
    }
}