using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Medical Bay.
    ///
    /// Post-processes the medical bay to:
    /// - Spawn healroot plants in the hydroponics basin with varied growth stages.
    ///   Healroot is a medicinal herb (9-day grow time) that's hydroponic-compatible,
    ///   making it perfect for a medical facility's on-site medicine production.
    /// - Connect VFE Medical VitalsCentre to power via hidden conduits.
    ///
    /// LEARNING NOTE: Like the Hydroponics room, this worker calls base.FillRoom() FIRST
    /// because the XML prefabs spawn the hydroponics basin and VitalsCentre,
    /// and we need them to exist before we can process them.
    /// </summary>
    public class RoomContents_MedicalBay : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (hospital beds, medicine shelves,
        /// hydroponics basin, VitalsCentre), then populates the basin with healroot plants at varied
        /// growth stages and connects VitalsCentre to power.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (beds, shelves, hydroponics basin, VitalsCentre, etc.)
            //    IMPORTANT: We need the containers/buildings to exist before we can process them
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // 2. Spawn healroot plants in all hydroponics basins with varied growth
            SpawnHealrootInHydroponics(map, roomRect);

            // 3. Spawn decorative roses in corner plant pots
            SpawnRosesInPlantPots(map, roomRect);

            // 4. Connect VFE Medical VitalsCentre to power
            ConnectVitalsCentreToConduitNetwork(map, roomRect);
        }

        /// <summary>
        /// Spawns healroot plants in all hydroponics basins in the room.
        /// </summary>
        private void SpawnHealrootInHydroponics(Map map, CellRect roomRect)
        {
            // Get healroot plant definition
            ThingDef healrootPlant = DefDatabase<ThingDef>.GetNamed("Plant_Healroot", false);

            // Spawn healroot with random growth variation (0.7-1.0) for realistic appearance
            // The medical bay typically has 1 basin, but this will work for any number
            float growth = Rand.Range(0.7f, 1.0f);

            RoomPlantHelper.SpawnPlantsInHydroponics(map, roomRect, healrootPlant, growth);
        }

        /// <summary>
        /// Spawns decorative roses in all plant pots in the room.
        /// Plant pots are placed by the BTG_PlantPot_Corner room part in corners.
        /// </summary>
        private void SpawnRosesInPlantPots(Map map, CellRect roomRect)
        {
            ThingDef rosePlant = DefDatabase<ThingDef>.GetNamed("Plant_Rose", false);
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, rosePlant, growth: 1.0f);
        }

        /// <summary>
        /// Finds all VFE Medical VitalsCentre buildings in the room and runs hidden conduits
        /// from each to the nearest room edge, connecting them to the wall conduit network.
        /// Does nothing if VFE Medical is not installed (no VitalsCentres will be found).
        /// </summary>
        private void ConnectVitalsCentreToConduitNetwork(Map map, CellRect roomRect)
        {
            ThingDef hiddenConduitDef = DefDatabase<ThingDef>.GetNamed("HiddenConduit", false);
            if (hiddenConduitDef == null)
                return;

            var vitalsCentres = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, "Facility_VitalsCentre");

            foreach (Building centre in vitalsCentres)
            {
                RoomEdgeConnector.ConnectToNearestEdge(map, centre.Position, roomRect, hiddenConduitDef);
            }
        }
    }
}
