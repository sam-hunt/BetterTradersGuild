using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Mechs
{
    // LordToil that assigns BTG_WanderInArea duty to all pawns.
    //
    // This toil makes pawns wander near a point without aggressive behavior.
    // Used by LordJob_StayInArea for passive mechs.
    public class LordToil_WanderInArea : LordToil
    {
        private IntVec3 point;

        // The center point that pawns wander around.
        // Exposed for Lord matching in RoomMechLordHelper.
        public IntVec3 Point => point;

        // Parameterless constructor not needed - toils are recreated via LordJob.CreateGraph()

        // Creates a new wander-in-area toil.
        // point: The center point to wander near
        public LordToil_WanderInArea(IntVec3 point)
        {
            this.point = point;
        }

        // Assigns BTG_WanderInArea duty to all pawns in the lord.
        public override void UpdateAllDuties()
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null)
                    continue;

                // Assign our custom passive wander duty with focus on the stay point
                pawn.mindState.duty = new PawnDuty(Duties.BTG_WanderInArea, point);
            }
        }

        // Note: No ExposeData override needed - LordToil doesn't serialize directly.
        // The point is serialized via LordJob_StayInArea.ExposeData(), and when
        // CreateGraph() runs on load, it recreates this toil with the correct point.
    }
}
