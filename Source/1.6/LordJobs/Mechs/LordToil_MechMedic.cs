using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Mechs
{
    // LordToil that assigns the BTG_MechMedic duty (room-bound triage) to every pawn
    // in the lord, focused on the room centre. The focus point is what MedicRoomBounds
    // uses as a stable anchor to resolve the medbay rects, and what RoomMechLordHelper
    // matches on so multiple medics in one room share a single lord.
    public class LordToil_MechMedic : LordToil
    {
        private IntVec3 point;

        // The room centre this medic lord is anchored to. Used for lord matching.
        public IntVec3 Point => point;

        public LordToil_MechMedic(IntVec3 point)
        {
            this.point = point;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_MechMedic, point);
            }
        }
    }
}
