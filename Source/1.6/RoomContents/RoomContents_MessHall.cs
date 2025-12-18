using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for MessHall.
    ///
    /// Post-processes the mess hall to connect VFE Spacer interactive tables
    /// to the power grid via hidden conduits.
    ///
    /// LEARNING NOTE: This worker calls base.FillRoom() FIRST because the XML prefabs
    /// spawn the tables, and we need those to exist before we can connect them to power.
    /// The interactive table (Table_interactive_2x2c) is only present when VFE Spacer
    /// is installed (via patch), so this gracefully does nothing when VFE Spacer is absent.
    /// </summary>
    public class RoomContents_MessHall : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (tables, chairs, etc.),
        /// then connects VFE Spacer interactive tables to power if present.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // 2. Connect VFE Spacer interactive tables to power
            ConnectInteractiveTablesToConduitNetwork(map, roomRect);

            // 3. Spawn decorative daylilies in corner plant pots
            ThingDef daylily = DefDatabase<ThingDef>.GetNamed("Plant_Daylily", false);
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, daylily, growth: 1.0f);
        }

        /// <summary>
        /// Finds all VFE Spacer interactive tables in the room and runs hidden conduits
        /// from each to the nearest room edge, connecting them to the wall conduit network.
        /// Does nothing if VFE Spacer is not installed (no tables will be found).
        /// </summary>
        private void ConnectInteractiveTablesToConduitNetwork(Map map, CellRect roomRect)
        {
            ThingDef hiddenConduitDef = DefDatabase<ThingDef>.GetNamed("HiddenConduit", false);
            if (hiddenConduitDef == null)
                return;

            var interactiveTables = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, "Table_interactive_2x2c");

            foreach (Building table in interactiveTables)
            {
                RoomEdgeConnector.ConnectToNearestEdge(map, table.Position, roomRect, hiddenConduitDef);
            }
        }
    }
}
