using BetterTradersGuild.DefRefs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    /// <summary>
    /// LordToil that assigns BTG_WanderInArea duty to all pawns.
    ///
    /// This toil makes pawns wander near a point without aggressive behavior.
    /// Used by LordJob_StayInArea for passive mechs.
    /// </summary>
    public class LordToil_WanderInArea : LordToil
    {
        private IntVec3 point;

        /// <summary>
        /// The center point that pawns wander around.
        /// Exposed for Lord matching in RoomMechLordHelper.
        /// </summary>
        public IntVec3 Point => point;

        // Parameterless constructor not needed - toils are recreated via LordJob.CreateGraph()

        /// <summary>
        /// Creates a new wander-in-area toil.
        /// </summary>
        /// <param name="point">The center point to wander near</param>
        public LordToil_WanderInArea(IntVec3 point)
        {
            this.point = point;
        }

        /// <summary>
        /// Assigns BTG_WanderInArea duty to all pawns in the lord.
        /// </summary>
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
