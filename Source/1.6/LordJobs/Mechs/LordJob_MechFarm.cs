using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Mechs
{
    // LordJob for agrihand mechs: anchors the mech to its greenhouse and drives the
    // farming behaviour tree (BTG_MechFarm duty) - harvest mature food crops in range,
    // haul the produce to a nearby shelf, sow rice into the emptied basin cells, then
    // dormant self-charge near the anchor point when there is nothing left to do (never
    // scanning or pathing outside the structure bounds).
    //
    // A distinct LordJob (rather than reusing LordJob_StayInArea) because a LordToil
    // assigns one duty to every pawn it owns: other passive mechs that share the room
    // must stay on their own LordJob_StayInArea lord and not be forced onto the farm
    // duty. RoomMechLordHelper keeps the lord types apart.
    public class LordJob_MechFarm : LordJob
    {
        private IntVec3 point;

        // Required for save/load serialization.
        public LordJob_MechFarm() { }

        public LordJob_MechFarm(IntVec3 point)
        {
            this.point = point;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();
            graph.AddToil(new LordToil_MechFarm(point));
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref point, "point");
        }
    }
}
