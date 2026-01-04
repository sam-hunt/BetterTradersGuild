using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that spawns a Mech_Militor pawn in the room.
    ///
    /// PURPOSE:
    /// Adds combat mechs to TradersGuild security rooms like MechRepairPost,
    /// MechSecurityPost, and Armory. Militors are light combat mechanoids.
    ///
    /// TECHNICAL APPROACH:
    /// Follows vanilla RoomPart_SentryDrone pattern exactly.
    /// Requires Biotech DLC (Mech_Militor is Biotech-exclusive).
    ///
    /// NOTE ON COUNT RANGES:
    /// When using countRange in XML (e.g., countRange 1~3), the room generation
    /// system calls FillRoom multiple times. Each call spawns one militor.
    /// </summary>
    public class RoomPart_MechMilitor : RoomPartWorker
    {
        public RoomPart_MechMilitor(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // Silent abort if Biotech not active (def will be null)
            if (PawnKinds.Mech_Militor == null)
                return;

            // Find standable cell in room
            if (!room.TryGetRandomCellInRoom(map, out IntVec3 cell, 0, 0,
                c => c.Standable(map), false))
            {
                return;
            }

            // Generate and spawn mech
            Pawn mech = PawnGenerator.GeneratePawn(PawnKinds.Mech_Militor, faction);
            GenSpawn.Spawn(mech, cell, map, WipeMode.Vanish);

            // Add to room's Lord with active defense behavior
            // Militors are combat mechs that should aggressively engage enemies
            RoomMechLordHelper.AddMechToRoomLord(mech, map, room, faction, MechRoomBehavior.Defend);
        }
    }
}
