using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Mechs
{
    // LordJob that keeps pawns wandering near a point without aggressive behavior.
    //
    // PURPOSE:
    // Used for expensive/specialized TradersGuild mechs (Fabricor, Paramedic,
    // Cleansweeper, Agrihand) that should stay in their rooms but avoid combat
    // to prevent damage. Unlike LordJob_DefendPoint which actively seeks enemies,
    // this job only makes pawns wander near a point.
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
    public class LordJob_StayInArea : LordJob
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
