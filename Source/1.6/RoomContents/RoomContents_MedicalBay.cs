using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Medical Bay.
    ///
    /// Spawns healroot plants in the hydroponics basin with varied growth stages.
    /// Healroot is a medicinal herb (9-day grow time) that's hydroponic-compatible,
    /// making it perfect for a medical facility's on-site medicine production.
    ///
    /// LEARNING NOTE: Like the Hydroponics room, this worker calls base.FillRoom() FIRST
    /// because the XML prefabs spawn the hydroponics basin (BTG_HydroponicHealroot),
    /// and we need it to exist before we can populate it with plants.
    /// </summary>
    public class RoomContents_MedicalBay : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (hospital beds, medicine shelves,
        /// hydroponics basin), then populates the basin with healroot plants at varied growth stages.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs (beds, shelves, hydroponics basin, etc.)
            //    IMPORTANT: We need the basin to exist before we can spawn plants in it
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Spawn healroot plants in all hydroponics basins with varied growth
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Get healroot plant definition
                ThingDef healrootPlant = DefDatabase<ThingDef>.GetNamed("Plant_Healroot", false);

                // Spawn healroot with random growth variation (0.7-1.0) for realistic appearance
                // The medical bay typically has 1 basin, but this will work for any number
                float growth = Rand.Range(0.7f, 1.0f);

                RoomPlantHelper.SpawnPlantsInHydroponics(map, roomRect, healrootPlant, growth);
            }
        }
    }
}
