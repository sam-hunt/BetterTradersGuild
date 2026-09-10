using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using UnityEngine;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // The per-grower plant spawning primitive shared by every planting pass.
    //
    // LEARNING NOTE: Plant pots and hydroponics basins are both Building_PlantGrower.
    // Plants are NOT stored in a container - they spawn as separate Plant things at the
    // grower's cells. This is fundamentally different from bookcases where items go into
    // innerContainer. Basins only accept plants with the "Hydroponic" sow tag (vanilla
    // corn lacks it; rice, potato, healroot and strawberry carry it).
    //
    // Which plant a grower gets is decided elsewhere: GenStep_SowPlantGrowers resolves
    // XML planting rules (PlantGrowerRule) per room and map-wide and calls
    // TrySpawnPlantInGrower for each assignment.
    public static class RoomPlantHelper
    {
        // Spawns plants in every empty cell of a single grower (plant pot, hydroponics
        // basin, ...). A grower's whole footprint gets the same plant (a 4-cell basin grows
        // one crop). Cells that already hold a plant are skipped, so callers can layer
        // passes safely. For a hydroponics basin the plant is validated against the basin's
        // sow tag (a warning, not an error: the caller is a map-wide pass with no room
        // context to fix).
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
