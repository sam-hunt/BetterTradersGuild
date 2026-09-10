using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using UnityEngine;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Helper class for spawning plants in room generation.
    // Provides methods for populating plant pots and hydroponics basins with decorative or food plants.
    //
    // DESIGN: Two area-scanning entry points for different building types, both funnelling
    // into the single per-grower primitive TrySpawnPlantInGrower:
    // - SpawnPlantsInPlantPots: For decorative pots, grow zones, etc.
    // - SpawnPlantsInHydroponics: For hydroponics basins with hydroponic-compatible plants
    // Map-wide passes (GenStep_FillHydroponics) call the per-grower primitive directly.
    public static class RoomPlantHelper
    {
        // Spawns decorative or food plants in all plant pots within the search area.
        //
        // LEARNING NOTE: Plant pots use Building_PlantGrower (same as hydroponics basins).
        // Plants are NOT stored in a container - they spawn as separate Plant things at
        // the same cell as the plant pot. This is fundamentally different from bookcases
        // where items go into innerContainer.
        //
        // USAGE: Designed for reuse in any RoomContentsWorker. Call this AFTER base.FillRoom()
        // to spawn plants in pots placed by XML prefabs.
        // map: The map to spawn plants on
        // searchArea: Area to search for plant pots
        // plantDefs: List of plants to randomly choose from (null/empty = use pot's default)
        // growth: Growth percentage (0.0-1.0, where 1.0 = fully mature)
        public static void SpawnPlantsInPlantPots(Map map, CellRect searchArea, List<ThingDef> plantDefs, float growth)
        {
            // Filter out nulls from the list
            List<ThingDef> validPlants = plantDefs?.Where(p => p != null).ToList();

            // Delegate to single-plant version, randomly selecting for each pot
            SpawnPlantsInPlantPotsInternal(map, searchArea, validPlants, growth);
        }

        // Single-plant overload of SpawnPlantsInPlantPots.
        // plantDef: Plant to spawn (null = use pot's default via GetPlantDefToGrow)
        public static void SpawnPlantsInPlantPots(Map map, CellRect searchArea, ThingDef plantDef, float growth)
        {
            // Wrap single plant in a list (or null if no plant specified)
            List<ThingDef> plantList = plantDef != null ? new List<ThingDef> { plantDef } : null;
            SpawnPlantsInPlantPotsInternal(map, searchArea, plantList, growth);
        }

        // Internal implementation that handles both single and multiple plant types.
        private static void SpawnPlantsInPlantPotsInternal(Map map, CellRect searchArea, List<ThingDef> plantDefs, float growth)
        {
            growth = ClampGrowth(growth);
            bool hasPlantOptions = plantDefs?.Count > 0;

            // Find all plant growers (pots, hydroponics, ...) and spawn plants in them
            foreach (IntVec3 cell in searchArea.Cells)
            {
                if (!cell.InBounds(map)) continue;

                Building_PlantGrower grower = FindGrowerAt(map, cell, requireHydroponics: false);
                if (grower == null) continue;

                // Randomly select from provided options, else fall back to the pot's default
                ThingDef plantToSpawn = hasPlantOptions ? plantDefs.RandomElement() : null;
                TrySpawnPlantInGrower(map, grower, plantToSpawn, growth);
            }
        }

        // Spawns food or medicinal plants in all hydroponics basins within the search area.
        //
        // LEARNING NOTE: Hydroponics basins are Building_PlantGrower instances, but they require
        // plants with the "Hydroponic" sow tag. This method validates plant compatibility to prevent
        // spawning ground-only plants (like roses) in hydroponics.
        //
        // USAGE: Designed for reuse in any RoomContentsWorker with hydroponics. Call this AFTER
        // base.FillRoom() to spawn plants in basins placed by XML prefabs.
        //
        // Common hydroponic-compatible plants (vanilla corn is NOT one):
        // - Plant_Rice: Fast-growing food crop (3 days)
        // - Plant_Potato: Reliable food crop (5.5 days)
        // - Plant_Healroot: Medicinal herb (9 days)
        // - Plant_Strawberry: Food/beauty hybrid (4.6 days)
        // map: The map to spawn plants on
        // searchArea: Area to search for hydroponics basins
        // plantDef: Plant to spawn (null = use basin's default via GetPlantDefToGrow)
        // growth: Growth percentage (0.0-1.0, where 1.0 = fully mature)
        public static void SpawnPlantsInHydroponics(Map map, CellRect searchArea, ThingDef plantDef, float growth)
        {
            growth = ClampGrowth(growth);

            // Validate plant is hydroponic-compatible (if specified)
            if (plantDef != null && !IsHydroponicCompatible(plantDef))
            {
                Log.Error($"[Better Traders Guild] Plant '{plantDef.defName}' is not hydroponic-compatible (missing 'Hydroponic' sow tag). Skipping plant spawning.");
                return;
            }

            // Find all hydroponics basins and spawn plants in them
            foreach (IntVec3 cell in searchArea.Cells)
            {
                if (!cell.InBounds(map)) continue;

                Building_PlantGrower basin = FindGrowerAt(map, cell, requireHydroponics: true);
                if (basin == null) continue;

                TrySpawnPlantInGrower(map, basin, plantDef, growth);
            }
        }

        // Spawns plants in every empty cell of a single grower (plant pot, hydroponics
        // basin, ...), the primitive every area scan above reduces to. A grower's whole
        // footprint gets the same plant (a 4-cell basin grows one crop). Cells that already
        // hold a plant are skipped, so callers can layer passes safely. For a hydroponics
        // basin the plant is validated against the basin's sow tag (a warning, not an
        // error: the caller may be a map-wide pass with no room context to fix).
        // plantDef: Plant to spawn (null = the grower's own default via GetPlantDefToGrow)
        // growth: Growth percentage (0.0-1.0), clamped
        // Returns: number of plants spawned
        public static int TrySpawnPlantInGrower(Map map, Building_PlantGrower grower, ThingDef plantDef, float growth)
        {
            if (map == null || grower == null) return 0;

            ThingDef plantToSpawn = plantDef ?? grower.GetPlantDefToGrow();
            if (plantToSpawn == null)
            {
                Log.Warning($"[Better Traders Guild] Could not determine plant type for grower at {grower.Position}");
                return 0;
            }

            // Hydroponics basins only accept plants carrying the Hydroponic sow tag (in case
            // GetPlantDefToGrow returned something incompatible, or the caller didn't check)
            if (grower.def == Things.HydroponicsBasin && !IsHydroponicCompatible(plantToSpawn))
            {
                Log.Warning($"[Better Traders Guild] Plant '{plantToSpawn.defName}' is not hydroponic-compatible. Skipping basin at {grower.Position}");
                return 0;
            }

            growth = ClampGrowth(growth);
            int spawned = 0;

            foreach (IntVec3 cell in grower.OccupiedRect())
            {
                if (!cell.InBounds(map)) continue;

                // Check if plant already exists at this cell (skip if present)
                if (cell.GetPlant(map) != null) continue;

                // CRITICAL: Set plantDefToGrow on the grower BEFORE spawning the plant
                // This is the idiomatic approach (simulating what a pawn does when planting):
                // 1. Configure what the grower should grow
                // 2. Then spawn the actual plant
                // This ensures proper rendering (container scaling), pawn interactions
                // (harvest, cut) and UI inspection show the right plant type. Done lazily so
                // a grower that is already full keeps whatever it was set to.
                if (spawned == 0)
                    grower.SetPlantDefToGrow(plantToSpawn);

                // Create and spawn the plant at the same cell as the grower
                Plant plant = (Plant)ThingMaker.MakeThing(plantToSpawn, null);
                GenSpawn.Spawn(plant, cell, map);
                // Sown property defaults to false - correct for procedural spawning
                // this prevents blight, and raider targeting of their own base trash

                // Set growth to desired level (1.0 = fully mature, matching debug action)
                plant.Growth = growth;
                spawned++;
            }

            return spawned;
        }

        // Checks if a plant ThingDef is compatible with hydroponics basins.
        // A plant is hydroponic-compatible if it has the "Hydroponic" sow tag.
        public static bool IsHydroponicCompatible(ThingDef plantDef)
        {
            if (plantDef == null || plantDef.plant == null) return false;

            if (plantDef.plant.sowTags == null) return false;

            return plantDef.plant.sowTags.Contains("Hydroponic");
        }

        // The Building_PlantGrower at a cell, or null. With requireHydroponics only a
        // HydroponicsBasin matches; otherwise any grower (pots included) does.
        private static Building_PlantGrower FindGrowerAt(Map map, IntVec3 cell, bool requireHydroponics)
        {
            List<Thing> things = cell.GetThingList(map);
            if (things == null) return null;

            foreach (Thing thing in things)
            {
                if (thing is Building_PlantGrower grower
                    && (!requireHydroponics || grower.def == Things.HydroponicsBasin))
                    return grower;
            }
            return null;
        }

        private static float ClampGrowth(float growth)
        {
            if (growth < 0f || growth > 1f)
            {
                Log.Warning($"[Better Traders Guild] Invalid growth value {growth}, clamping to 0.0-1.0");
                return Mathf.Clamp01(growth);
            }
            return growth;
        }
    }
}
