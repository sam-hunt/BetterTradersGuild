using System.Collections.Generic;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    // Custom RoomPartDef that allows parameterizing mech spawning via XML.
    //
    // FIELDS:
    // - pawnKindDef: The PawnKindDef of the mech to spawn (e.g., Mech_Cleansweeper)
    // - behavior: The MechRoomBehavior (Defend or Passive)
    // - startingInventory: Things placed in the spawned mech's inventory
    //
    // USAGE IN XML:
    // <BetterTradersGuild.RoomParts.RoomPart_MechDef MayRequire="Ludeon.RimWorld.Biotech">
    //   <defName>BTG_Mech_Cleansweeper</defName>
    //   <workerClass>BetterTradersGuild.RoomParts.RoomPart_Mech</workerClass>
    //   <pawnKindDef>Mech_Cleansweeper</pawnKindDef>
    //   <behavior>Passive</behavior>
    //   <startingInventory>
    //     <MedicineUltratech>6</MedicineUltratech>
    //   </startingInventory>
    // </BetterTradersGuild.RoomParts.RoomPart_MechDef>
    //
    // This eliminates the need for separate worker classes per mech type.
    public class RoomPart_MechDef : RoomPartDef
    {
        // The PawnKindDef of the mech to spawn.
        public PawnKindDef pawnKindDef;

        // The behavior mode for the spawned mech (Defend or Passive).
        // Defaults to Passive for safety.
        public MechRoomBehavior behavior = MechRoomBehavior.Passive;

        // Things added to the mech's inventory at spawn (thingDef + count entries,
        // same <ThingDef>count</ThingDef> shorthand as costList). Used to give the
        // paramedic its medicine kit. Spawning per-kind here rather than patching
        // the vanilla PawnKindDef keeps player-gestated mechs unaffected. Note the
        // inventory drops as loot when the mech is killed.
        public List<ThingDefCountClass> startingInventory;
    }
}
