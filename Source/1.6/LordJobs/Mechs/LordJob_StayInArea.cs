using BetterTradersGuild.LordJobs.Mechs;
using Verse;
using Verse.AI.Group;

// NOTE: this class must stay in the BetterTradersGuild.LordJobs namespace even though the
// file lives under LordJobs/Mechs/. Lords scribe their LordJob by full type name, so v1.0.8
// saves contain the literal string "BetterTradersGuild.LordJobs.LordJob_StayInArea"; moving
// the namespace would null the lord on load (error burst, mechs dropped from their lord).
namespace BetterTradersGuild.LordJobs
{
    // LordJob that keeps pawns wandering near a point without aggressive behavior.
    //
    // PURPOSE:
    // The Passive behavior mode: stay in the room, avoid combat, visibly wander.
    // Currently unused - every specialized mech has moved to a narrower lord
    // (Paramedic/Cleansweeper/Agrihand to their duty lords, Lifter/Fabricor to
    // LordJob_MechDorm) - but kept both for future roaming mechs and so saves
    // made mid-visit under the old behavior still load. Unlike LordJob_DefendPoint
    // which actively seeks enemies, this job only makes pawns wander near a point.
    //
    // BEHAVIOR:
    // - Pawns wander within ~7 tiles of the stay point (via BTG_WanderInArea duty)
    // - No aggressive enemy-seeking behavior
    // - Pawns still self-defend via ThinkTree fallback if directly attacked
    //
    // TECHNICAL NOTES:
    // Uses LordToil_WanderInArea which assigns BTG_WanderInArea duty.
    // The duty's thinkNode only contains JobGiver_WanderNearDutyLocation,
    // so pawns stay busy wandering and don't fall through to aggressive
    // ThinkTree behaviors.
    public class LordJob_StayInArea : LordJob, IBTGSurvivorLord
    {
        private IntVec3 point;

        // Required for save/load serialization.
        public LordJob_StayInArea() { }

        // Creates a new stay-in-area lord job.
        // point: The center point to stay near
        public LordJob_StayInArea(IntVec3 point)
        {
            this.point = point;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_WanderInArea(point));
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
        }
    }
}
