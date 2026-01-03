using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that spawns a Mech_Agrihand pawn in the room.
    ///
    /// PURPOSE:
    /// Adds agricultural mechs to TradersGuild Greenhouse rooms.
    /// Agrihands are plant-work focused mechanoids that fit well in growing spaces.
    ///
    /// TECHNICAL APPROACH:
    /// Follows vanilla RoomPart_SentryDrone pattern exactly.
    /// Requires Biotech DLC (Mech_Agrihand is Biotech-exclusive).
    /// </summary>
    public class RoomPart_MechAgrihand : RoomPartWorker
    {
        private static PawnKindDef cachedMechKind;

        public RoomPart_MechAgrihand(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // Require Biotech DLC
            if (!ModsConfig.BiotechActive)
                return;

            // Cache PawnKindDef lookup
            if (cachedMechKind == null)
            {
                cachedMechKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Agrihand");
                if (cachedMechKind == null)
                {
                    Log.ErrorOnce("[Better Traders Guild] Could not find PawnKindDef 'Mech_Agrihand'.", 94712389);
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
