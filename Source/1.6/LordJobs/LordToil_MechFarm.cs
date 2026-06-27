using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    // LordToil that assigns the BTG_MechFarm duty (radius-bound, structure-confined
    // greenhouse tending) to every pawn in the lord, focused on the room centre. The
    // focus point is what FarmArea uses as the centre of the search radius and the
    // "return home" dormancy target, and what RoomMechLordHelper matches on so multiple
    // agrihands in one greenhouse share a single lord.
    public class LordToil_MechFarm : LordToil
    {
        private IntVec3 point;

        // The greenhouse centre this farm lord is anchored to. Used for lord matching.
        public IntVec3 Point => point;

        public LordToil_MechFarm(IntVec3 point)
        {
            this.point = point;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_MechFarm, point);
            }
        }
    }
}
