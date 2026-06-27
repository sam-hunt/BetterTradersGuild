using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    // LordToil that assigns the BTG_MechClean duty (radius-bound, structure-confined
    // filth cleaning) to every pawn in the lord, focused on the room centre. The focus
    // point is what CleanArea uses as the centre of the filth radius and the "return
    // home" dormancy target, and what RoomMechLordHelper matches on so multiple
    // cleansweepers in one room share a single lord.
    public class LordToil_MechClean : LordToil
    {
        private IntVec3 point;

        // The room centre this clean lord is anchored to. Used for lord matching.
        public IntVec3 Point => point;

        public LordToil_MechClean(IntVec3 point)
        {
            this.point = point;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_MechClean, point);
            }
        }
    }
}
