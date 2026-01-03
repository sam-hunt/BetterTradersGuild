using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that spawns a Mech_Fabricor pawn in the room.
    ///
    /// PURPOSE:
    /// Adds thematic fabrication mechs to TradersGuild Workshop rooms.
    /// Fabricors are crafting-focused mechanoids that fit well in industrial spaces.
    ///
    /// TECHNICAL APPROACH:
    /// Follows vanilla RoomPart_SentryDrone pattern exactly.
    /// Requires Biotech DLC (Mech_Fabricor is Biotech-exclusive).
    /// </summary>
    public class RoomPart_MechFabricor : RoomPartWorker
    {
        private static PawnKindDef cachedMechKind;

        public RoomPart_MechFabricor(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // Require Biotech DLC
            if (!ModsConfig.BiotechActive)
                return;

            // Cache PawnKindDef lookup
            if (cachedMechKind == null)
            {
                cachedMechKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Fabricor");
                if (cachedMechKind == null)
                {
                    Log.ErrorOnce("[Better Traders Guild] Could not find PawnKindDef 'Mech_Fabricor'.", 94712386);
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
