using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// Generic RoomPartWorker that spawns a mech based on XML-defined parameters.
    ///
    /// PURPOSE:
    /// Single worker class that handles all mech types via parameterization.
    /// The mech type and behavior are specified in the RoomPart_MechDef XML.
    ///
    /// TECHNICAL APPROACH:
    /// - Casts def to RoomPart_MechDef to access pawnKindDef and behavior
    /// - Follows vanilla RoomPart_SentryDrone pattern for spawning
    /// - Uses RoomMechLordHelper to assign mechs to room-specific Lords
    ///
    /// USAGE:
    /// <![CDATA[
    /// <BetterTradersGuild.RoomParts.RoomPart_MechDef MayRequire="Ludeon.RimWorld.Biotech">
    ///   <defName>BTG_Mech_Cleansweeper</defName>
    ///   <workerClass>BetterTradersGuild.RoomParts.RoomPart_Mech</workerClass>
    ///   <pawnKindDef>Mech_Cleansweeper</pawnKindDef>
    ///   <behavior>Passive</behavior>
    /// </BetterTradersGuild.RoomParts.RoomPart_MechDef>
    /// ]]>
    /// </summary>
    public class RoomPart_Mech : RoomPartWorker
    {
        private RoomPart_MechDef Def => (RoomPart_MechDef)def;

        public RoomPart_Mech(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            if (Def.pawnKindDef == null)
                return;

            // Find standable cell in room
            if (!room.TryGetRandomCellInRoom(map, out IntVec3 cell, 0, 0, c => c.Standable(map), false))
                return;

            // Generate and spawn mech
            Pawn mech = PawnGenerator.GeneratePawn(Def.pawnKindDef, faction);
            GenSpawn.Spawn(mech, cell, map, WipeMode.Vanish);

            // Add to room's Lord with configured behavior
            RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, Def.behavior);
        }
    }
}
