using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    /// <summary>
    /// LordJob for cleansweeper mechs: anchors the mech to its spawn room and drives the
    /// janitor behaviour tree (BTG_MechClean duty) - clean nearby filth within a moderate
    /// radius (never outside the structure bounds), then dormant self-charge near the
    /// anchor point when none remains.
    ///
    /// A distinct LordJob (rather than reusing LordJob_StayInArea) because a LordToil
    /// assigns one duty to every pawn it owns: other passive mechs that share the room
    /// must stay on their own LordJob_StayInArea lord and not be forced onto the clean
    /// duty. RoomMechLordHelper keeps the lord types apart.
    /// </summary>
    public class LordJob_MechClean : LordJob
    {
        private IntVec3 point;

        /// <summary>Required for save/load serialization.</summary>
        public LordJob_MechClean() { }

        public LordJob_MechClean(IntVec3 point)
        {
            this.point = point;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_MechClean(point));
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
        }
    }
}
