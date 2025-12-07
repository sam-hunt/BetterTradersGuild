using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Hydroponics Bay.
    ///
    /// Spawns rice plants in all hydroponics basins with varied growth stages for a realistic
    /// "active cultivation" appearance. Rice is a fast-growing food crop (3 days) that's
    /// hydroponic-compatible and makes sense for a space station's food production.
    ///
    /// LEARNING NOTE: Unlike Captain's Quarters, this worker calls base.FillRoom() FIRST
    /// because the XML prefabs spawn the hydroponics basins, and we need those to exist
    /// before we can populate them with plants.
    /// </summary>
    public class RoomContents_Hydroponics : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (hydroponics basins, shelves),
        /// then populates basins with rice plants at varied growth stages.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (hydroponics basins, shelves, etc.)
            //    IMPORTANT: We need basins to exist before we can spawn plants in them
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Spawn rice plants in all hydroponics basins with varied growth
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Get rice plant definition
                ThingDef ricePlant = DefDatabase<ThingDef>.GetNamed("Plant_Rice", false);

                // Spawn rice with random growth variation (0.7-1.0) for realistic appearance
                // Each basin gets its own growth value for visual variety
                float growth = Rand.Range(0.7f, 1.0f);

                RoomPlantHelper.SpawnPlantsInHydroponics(map, roomRect, ricePlant, growth);
            }
        }
    }
}
