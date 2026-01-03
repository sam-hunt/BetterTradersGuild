using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents.Nursery
{
    /// <summary>
    /// Custom RoomContentsWorker for Nursery rooms (Biotech DLC).
    ///
    /// Spawns a crib subroom prefab with an L-shaped wall configuration
    /// that can be placed in corners (preferred) or along edges (with procedural wall completion).
    ///
    /// Uses the same placement strategy as Commander's Quarters:
    /// - Corner placement (preferred): Uses 2 room walls, no additional walls needed
    /// - Edge placement (fallback): Uses 1 room wall, spawns 1 side wall
    /// - Center placement (last resort): Uses 0 room walls, spawns 2 walls (back + left)
    ///
    /// Post-generation customization is handled by helper classes:
    /// - CivilianSpawner: Spawns caretaker and children in the subroom
    /// - ShelfPopulator: Adds baby food and meals to shelves
    /// - FurniturePainter: Paints furniture with pastel colors
    /// </summary>
    public class RoomContents_Nursery : RoomContentsWorker
    {
        // Prefab actual size (6×6) - the content defined in XML
        private const int CRIB_SUBROOM_SIZE = 6;

        // Prefab defName for the crib subroom structure
        private const string CRIB_PREFAB_DEFNAME = "BTG_CribSubroom";

        // Stores the crib subroom area to prevent other prefabs from spawning there
        private CellRect cribSubroomRect;

        /// <summary>
        /// Main room generation method. Orchestrates crib subroom placement and calls base class
        /// to process XML-defined content (prefabs, scatter, parts) in remaining space.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Explicitly initialize cribSubroomRect to default (safety mechanism)
            // If placement fails, Width = 0, so IsValidCellBase won't block other prefabs
            this.cribSubroomRect = default;

            // 0. Apply checkered floor pattern using pastel carpets
            //    Must happen BEFORE base.FillRoom() which may apply uniform flooring
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                List<string> floorTypes = new List<string> { "CarpetPink", "CarpetBluePastel", "CarpetGreenPastel" };
                CheckeredFloorHelper.ApplyCheckeredFloor(map, roomRect, floorTypes);
            }

            // 1. Find best location for crib subroom (prefer corners, avoid walls with doors)
            SubroomPlacementResult placement = SubroomPlacementHelper.FindBestPlacement(room, map, CRIB_SUBROOM_SIZE);

            if (placement.IsValid)
            {
                // 2. Calculate and store subroom area for validation (prevents other prefab overlap)
                this.cribSubroomRect = SubroomPlacementHelper.GetBlockingRect(
                    placement.Position, placement.Rotation, CRIB_SUBROOM_SIZE);

                // 3. Spawn crib subroom prefab using PrefabUtility API
                SpawnCribSubroomPrefab(map, placement);

                // 4. Spawn required walls from PlacementCalculator
                SubroomPlacementHelper.SpawnWalls(map, placement.RequiredWalls);

                // 5. Spawn civilians sheltering in the nursery (behind blast door)
                // These represent non-combatants who have locked themselves in for safety
                CivilianSpawner.SpawnShelteringCivilians(map, faction, this.cribSubroomRect);

                // 6. Populate nursery shelf with baby food and survival meals
                ShelfPopulator.PopulateNurseryShelf(map, this.cribSubroomRect);
            }
            else
            {
                // Log warning but CONTINUE (other prefabs still spawn for graceful degradation)
                CellRect firstRect = room.rects?.FirstOrDefault() ?? default;
                Log.Warning($"[Better Traders Guild] Could not find valid placement for Crib Subroom in Nursery at {firstRect}");
                // NO RETURN - continue to spawn other room furniture
            }

            // 7. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns other nursery furniture even if subroom failed
            //    Other prefabs will avoid subroom area if cribSubroomRect.Width > 0
            base.FillRoom(map, room, faction, threatPoints);

            // 8. Post-processing: Paint furniture with matching pastel colors
            //    Colors match the checkered floor pattern for a cohesive nursery look
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                FurniturePainter.PaintFurniture(map, roomRect);
            }

            // 9. Post-processing: Spawn daylilies in plant pots
            //    CRITICAL: This must happen AFTER base.FillRoom() since plant pots
            //    are spawned by XML prefabs in base.FillRoom()
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Spawn daylilies in plant pots
                ThingDef daylily = DefDatabase<ThingDef>.GetNamed("Plant_Daylily", false);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, daylily, growth: 1.0f);
            }
        }

        /// <summary>
        /// Override to prevent other prefabs from spawning in crib subroom area.
        ///
        /// CRITICAL: This MUST block placement before spawning occurs. Post-spawn removal
        /// doesn't work because other prefabs overwrite subroom furniture at the same cells,
        /// and removing them afterward leaves the subroom furniture already destroyed.
        ///
        /// Called by base.FillRoom() during prefab placement validation.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block prefab placement in crib subroom area (prevent furniture overwriting)
            if (this.cribSubroomRect.Width > 0 && this.cribSubroomRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Spawns the crib subroom prefab using PrefabUtility API.
        /// The prefab contains the L-shaped walls, door, cribs, and end table.
        ///
        /// IMPORTANT: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning.
        /// The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        /// </summary>
        private void SpawnCribSubroomPrefab(Map map, SubroomPlacementResult placement)
        {
            PrefabDef prefab = DefDatabase<PrefabDef>.GetNamed(CRIB_PREFAB_DEFNAME, true);

            if (prefab == null)
            {
                Log.Error($"[Better Traders Guild] Could not find PrefabDef '{CRIB_PREFAB_DEFNAME}'");
                return;
            }

            // Spawn the prefab at the specified CENTER position with rotation
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }
    }
}
