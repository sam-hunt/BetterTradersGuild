using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that spawns a Mech_Lifter pawn in the room.
    ///
    /// PURPOSE:
    /// Adds thematic hauling mechs to TradersGuild settlement rooms like
    /// Storeroom and ShuttleBay where heavy cargo handling is expected.
    ///
    /// TECHNICAL APPROACH:
    /// Follows vanilla RoomPart_SentryDrone pattern exactly.
    /// Requires Biotech DLC (Mech_Lifter is Biotech-exclusive).
    /// </summary>
    public class RoomPart_MechLifter : RoomPartWorker
    {
        public RoomPart_MechLifter(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // Silent abort if Biotech not active (def will be null)
            if (PawnKinds.Mech_Lifter == null)
                return;

            // Find standable cell in room
            if (!room.TryGetRandomCellInRoom(map, out IntVec3 cell, 0, 0,
                c => c.Standable(map), false))
            {
                return;
            }

            // Generate and spawn mech
            Pawn mech = PawnGenerator.GeneratePawn(PawnKinds.Mech_Lifter, faction);
            GenSpawn.Spawn(mech, cell, map, WipeMode.Vanish);

            // Add to room's Lord with passive behavior (wander only, self-defend)
            // Lifters are utility mechs that shouldn't aggressively engage enemies
            RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Passive);
        }
    }
}
