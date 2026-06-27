using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs
{
    /// <summary>
    /// LordJob for paramedic mechs: anchors the mech to its MedicalBay and drives the
    /// medic behaviour tree (BTG_MechMedic duty) - emergency tend, rescue downed
    /// defenders to in-room beds, routine tend, dormant self-charge when idle.
    ///
    /// A distinct LordJob (rather than reusing LordJob_StayInArea) because a LordToil
    /// assigns one duty to every pawn it owns: passive wander mechs that share the same
    /// room (e.g. a cleansweeper) must stay on their own LordJob_StayInArea lord and not
    /// be forced onto the medic duty. RoomMechLordHelper keeps the two lord types apart.
    /// </summary>
    public class LordJob_MechMedic : LordJob
    {
        private IntVec3 point;

        /// <summary>Required for save/load serialization.</summary>
        public LordJob_MechMedic() { }

        public LordJob_MechMedic(IntVec3 point)
        {
            this.point = point;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_MechMedic(point));
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
        }
    }
}
