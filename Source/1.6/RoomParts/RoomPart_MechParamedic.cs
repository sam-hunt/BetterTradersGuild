using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomParts
{
    /// <summary>
    /// RoomPartWorker that spawns a Mech_Paramedic pawn in the room.
    ///
    /// PURPOSE:
    /// Adds thematic medical mechs to TradersGuild Medical Bay rooms.
    /// Paramedics are rescue/medical-focused mechanoids that fit well in healthcare spaces.
    ///
    /// TECHNICAL APPROACH:
    /// Follows vanilla RoomPart_SentryDrone pattern exactly.
    /// Requires Biotech DLC (Mech_Paramedic is Biotech-exclusive).
    /// </summary>
    public class RoomPart_MechParamedic : RoomPartWorker
    {
        private static PawnKindDef cachedMechKind;

        public RoomPart_MechParamedic(RoomPartDef def) : base(def) { }

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
        {
            // Require Biotech DLC
            if (!ModsConfig.BiotechActive)
                return;

            // Cache PawnKindDef lookup
            if (cachedMechKind == null)
            {
                cachedMechKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mech_Paramedic");
                if (cachedMechKind == null)
                {
                    Log.ErrorOnce("[Better Traders Guild] Could not find PawnKindDef 'Mech_Paramedic'.", 94712387);
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
