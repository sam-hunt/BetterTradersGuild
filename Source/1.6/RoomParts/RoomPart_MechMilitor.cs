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
        private static PawnKindDef cachedMechKind;

        public RoomPart_MechMilitor(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // Require Biotech DLC
            if (!ModsConfig.BiotechActive)
                return;

            // Cache PawnKindDef lookup
            if (cachedMechKind == null)
            {
                cachedMechKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Militor");
                if (cachedMechKind == null)
                {
                    Log.ErrorOnce("[Better Traders Guild] Could not find PawnKindDef 'Mech_Militor'.", 94712388);
                    return;
                }
            }

            // Find standable cell in room
            if (!room.TryGetRandomCellInRoom(map, out IntVec3 cell, 0, 0,
                c => c.Standable(map), false))
            {
                return;
            }

            // Generate and spawn mech
            Pawn mech = PawnGenerator.GeneratePawn(cachedMechKind, faction);
            GenSpawn.Spawn(mech, cell, map, WipeMode.Vanish);
        }
    }
}
