using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Mechs
{
    // LordToil that assigns the BTG_MechDorm duty (return home, then wall-park dormant
    // self-charge) to every pawn in the lord, focused on the room centre. The focus
    // point is what HomeRoomArea uses to resolve the mech's room (and as the "return
    // home" target), and what RoomMechLordHelper matches on so multiple dorm mechs in
    // one room share a single lord.
    public class LordToil_MechDorm : LordToil
    {
        private IntVec3 point;

        // The room centre this dorm lord is anchored to. Used for lord matching.
        public IntVec3 Point => point;

        public LordToil_MechDorm(IntVec3 point)
        {
            this.point = point;
        }

        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                pawn.mindState.duty = new PawnDuty(Duties.BTG_MechDorm, point);
            }
        }
    }
}
