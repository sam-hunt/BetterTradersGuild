using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Mechs
{
    // LordJob for utility mechs with no work behaviour yet (Lifter, Fabricor): anchors
    // the mech to its spawn room and drives the BTG_MechDorm duty - walk home if
    // displaced, then park against a room wall and dormant self-charge. Replaces the
    // old endless wander (LordJob_StayInArea) so these mechs read as docked equipment
    // rather than pacing the room forever; if they grow real duties later (like the
    // cleansweeper or agrihand did) this lord is the slot those duty trees go into.
    //
    // A distinct LordJob (rather than reusing LordJob_StayInArea) because a LordToil
    // assigns one duty to every pawn it owns: any mech still meant to wander must stay
    // on its own LordJob_StayInArea lord and not be forced onto the dorm duty.
    // RoomMechLordHelper keeps the lord types apart.
    public class LordJob_MechDorm : LordJob
    {
        private IntVec3 point;

        // Required for save/load serialization.
        public LordJob_MechDorm() { }

        public LordJob_MechDorm(IntVec3 point)
        {
            this.point = point;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_MechDorm(point));
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
        }
    }
}
