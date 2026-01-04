using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;
using UnityEngine;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Helper class for spawning plants in room generation.
    /// Provides methods for populating plant pots and hydroponics basins with decorative or food plants.
    ///
    /// DESIGN: Separated into two methods for different building types:
    /// - SpawnPlantsInPlantPots: For decorative pots, grow zones, etc.
    /// - SpawnPlantsInHydroponics: For hydroponics basins with hydroponic-compatible plants
    /// </summary>
    public static class RoomPlantHelper
    {
        /// <summary>
        /// Spawns decorative or food plants in all plant pots within the search area.
        ///
        /// LEARNING NOTE: Plant pots use Building_PlantGrower (same as hydroponics basins).
        /// Plants are NOT stored in a container - they spawn as separate Plant things at
        /// the same cell as the plant pot. This is fundamentally different from bookcases
        /// where items go into innerContainer.
        ///
        /// USAGE: Designed for reuse in any RoomContentsWorker. Call this AFTER base.FillRoom()
        /// to spawn plants in pots placed by XML prefabs.
        /// </summary>
        /// <param name="map">The map to spawn plants on</param>
        /// <param name="searchArea">Area to search for plant pots</param>
        /// <param name="plantDefs">List of plants to randomly choose from (null/empty = use pot's default)</param>
        /// <param name="growth">Growth percentage (0.0-1.0, where 1.0 = fully mature)</param>
        public static void SpawnPlantsInPlantPots(Map map, CellRect searchArea, List<ThingDef> plantDefs, float growth)
        {
            // Filter out nulls from the list
            List<ThingDef> validPlants = plantDefs?.Where(p => p != null).ToList();

            // Delegate to single-plant version, randomly selecting for each pot
            SpawnPlantsInPlantPotsInternal(map, searchArea, validPlants, growth);
        }

        /// <summary>
        /// Spawns decorative or food plants in all plant pots within the search area.
        ///
        /// LEARNING NOTE: Plant pots use Building_PlantGrower (same as hydroponics basins).
        /// Plants are NOT stored in a container - they spawn as separate Plant things at
        /// the same cell as the plant pot. This is fundamentally different from bookcases
        /// where items go into innerContainer.
        ///
        /// USAGE: Designed for reuse in any RoomContentsWorker. Call this AFTER base.FillRoom()
        /// to spawn plants in pots placed by XML prefabs.
        /// </summary>
        /// <param name="map">The map to spawn plants on</param>
        /// <param name="searchArea">Area to search for plant pots</param>
        /// <param name="plantDef">Plant to spawn (null = use pot's default via GetPlantDefToGrow)</param>
        /// <param name="growth">Growth percentage (0.0-1.0, where 1.0 = fully mature)</param>
        public static void SpawnPlantsInPlantPots(Map map, CellRect searchArea, ThingDef plantDef, float growth)
        {
            // Wrap single plant in a list (or null if no plant specified)
            List<ThingDef> plantList = plantDef != null ? new List<ThingDef> { plantDef } : null;
            SpawnPlantsInPlantPotsInternal(map, searchArea, plantList, growth);
        }

        /// <summary>
        /// Internal implementation that handles both single and multiple plant types.
        /// </summary>
        private static void SpawnPlantsInPlantPotsInternal(Map map, CellRect searchArea, List<ThingDef> plantDefs, float growth)
        {
            // Validate growth parameter
            if (growth < 0f || growth > 1f)
            {
                Log.Warning($"[Better Traders Guild] Invalid growth value {growth}, clamping to 0.0-1.0");
                growth = Mathf.Clamp01(growth);
            }

            bool hasPlantOptions = plantDefs != null && plantDefs.Count > 0;

            // Find all plant pots and spawn plants in them
            foreach (IntVec3 cell in searchArea.Cells)
            {
                if (!cell.InBounds(map)) continue;

                // Find plant pot at this cell
                Building_PlantGrower plantPot = null;
                List<Thing> things = cell.GetThingList(map);
                if (things != null)
                {
                    foreach (Thing thing in things)
                    {
                        // Match any Building_PlantGrower (includes PlantPot, hydroponics, etc.)
                        if (thing is Building_PlantGrower grower)
                        {
                            plantPot = grower;
                            break;
                        }
                    }
                }

                if (plantPot == null) continue;

                // Check if plant already exists at this cell (skip if present)
                Plant existingPlant = cell.GetPlant(map);
                if (existingPlant != null) continue;

                // Determine which plant to spawn
                ThingDef plantToSpawn;
                if (hasPlantOptions)
                {
                    // Randomly select from provided options
                    plantToSpawn = plantDefs.RandomElement();
                }
                else
                {
                    // Fall back to pot's default
                    plantToSpawn = plantPot.GetPlantDefToGrow();
                }

                if (plantToSpawn == null)
                {
                    Log.Warning($"[Better Traders Guild] Could not determine plant type for pot at {cell}");
                    continue;
                }

                // CRITICAL: Set plantDefToGrow on the grower BEFORE spawning the plant
                // This tells the grower what it's supposed to be growing, which affects:
                // - Plant rendering (proper scaling for containers vs. ground)
                // - Pawn interactions (harvest, cut, etc.)
                // - UI inspection (shows correct plant type)
                plantPot.SetPlantDefToGrow(plantToSpawn);

                // Create and spawn the plant at the same cell as the pot
                Plant plant = (Plant)ThingMaker.MakeThing(plantToSpawn, null);
                GenSpawn.Spawn(plant, cell, map);
                // Sown property defaults to false - correct for procedural spawning
                // this prevents blight, and raider targeting of their own base trash

                // Set growth to desired level (1.0 = fully mature, matching debug action)
                plant.Growth = growth;

                // OPTIONAL: Uncomment to add age variation for visual interest
                // if (plant.def.plant.LimitedLifespan)
                // {
                //     int maxAge = (int)(plant.def.plant.LifespanDays * 60000);
                //     plant.Age = Rand.Range(0, maxAge / 2); // Random age up to half lifespan
                // }
            }
        }

        /// <summary>
        /// Spawns food or medicinal plants in all hydroponics basins within the search area.
        ///
        /// LEARNING NOTE: Hydroponics basins are Building_PlantGrower instances, but they require
        /// plants with the "Hydroponic" sow tag. This method validates plant compatibility to prevent
        /// spawning ground-only plants (like roses) in hydroponics.
        ///
        /// USAGE: Designed for reuse in any RoomContentsWorker with hydroponics. Call this AFTER
        /// base.FillRoom() to spawn plants in basins placed by XML prefabs.
        ///
        /// Common hydroponic-compatible plants:
        /// - Plant_Rice: Fast-growing food crop (3 days)
        /// - Plant_Potato: Reliable food crop (5.5 days)
        /// - Plant_Corn: High-yield food crop (14 days)
        /// - Plant_Healroot: Medicinal herb (9 days)
        /// - Plant_Strawberry: Food/beauty hybrid (4.6 days)
        /// </summary>
        /// <param name="map">The map to spawn plants on</param>
        /// <param name="searchArea">Area to search for hydroponics basins</param>
        /// <param name="plantDef">Plant to spawn (null = use basin's default via GetPlantDefToGrow)</param>
        /// <param name="growth">Growth percentage (0.0-1.0, where 1.0 = fully mature)</param>
        public static void SpawnPlantsInHydroponics(Map map, CellRect searchArea, ThingDef plantDef, float growth)
        {
            // Validate growth parameter
            if (growth < 0f || growth > 1f)
            {
                Log.Warning($"[Better Traders Guild] Invalid growth value {growth}, clamping to 0.0-1.0");
                growth = Mathf.Clamp01(growth);
            }

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

                // Find hydroponics basin at this cell
                Building_PlantGrower hydroponicsBasin = null;
                List<Thing> things = cell.GetThingList(map);
                if (things != null)
                {
                    foreach (Thing thing in things)
                    {
                        // Match hydroponics basins specifically
                        if (thing is Building_PlantGrower grower && grower.def == Things.HydroponicsBasin)
                        {
                            hydroponicsBasin = grower;
                            break;
                        }
                    }
                }

                if (hydroponicsBasin == null) continue;

                // Check if plant already exists at this cell (skip if present)
                Plant existingPlant = cell.GetPlant(map);
                if (existingPlant != null) continue;

                // Determine which plant to spawn
                ThingDef plantToSpawn = plantDef ?? hydroponicsBasin.GetPlantDefToGrow();
                if (plantToSpawn == null)
                {
                    Log.Warning($"[Better Traders Guild] Could not determine plant type for hydroponics basin at {cell}");
                    continue;
                }

                // Double-check compatibility (in case GetPlantDefToGrow returned incompatible plant)
                if (!IsHydroponicCompatible(plantToSpawn))
                {
                    Log.Warning($"[Better Traders Guild] Plant '{plantToSpawn.defName}' is not hydroponic-compatible. Skipping basin at {cell}");
                    continue;
                }

                // CRITICAL: Set plantDefToGrow on the basin BEFORE spawning the plant
                // This is the idiomatic approach (simulating what a pawn does when planting):
                // 1. Configure what the grower should grow
                // 2. Then spawn the actual plant
                // This ensures proper rendering, interaction, and UI display
                hydroponicsBasin.SetPlantDefToGrow(plantToSpawn);

                // Create and spawn the plant at the same cell as the basin
                Plant plant = (Plant)ThingMaker.MakeThing(plantToSpawn, null);
                GenSpawn.Spawn(plant, cell, map);

                // Set growth to desired level
                plant.Growth = growth;
            }
        }

        /// <summary>
        /// Checks if a plant ThingDef is compatible with hydroponics basins.
        /// A plant is hydroponic-compatible if it has the "Hydroponic" sow tag.
        /// </summary>
        /// <param name="plantDef">The plant ThingDef to check</param>
        /// <returns>True if plant can grow in hydroponics, false otherwise</returns>
        private static bool IsHydroponicCompatible(ThingDef plantDef)
        {
            if (plantDef == null || plantDef.plant == null) return false;

            if (plantDef.plant.sowTags == null) return false;

            return plantDef.plant.sowTags.Contains("Hydroponic");
        }
    }
}
