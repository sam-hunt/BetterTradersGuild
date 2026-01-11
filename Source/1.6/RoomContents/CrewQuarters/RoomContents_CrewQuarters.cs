using System;
using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using static BetterTradersGuild.Helpers.RoomContents.PlacementCalculator;
using static BetterTradersGuild.RoomContents.CrewQuarters.SubroomPackingCalculator;

namespace BetterTradersGuild.RoomContents.CrewQuarters
{
    /// <summary>
    /// Custom RoomContentsWorker for Crew Quarters.
    ///
    /// Spawns multiple enclosed subrooms (private bedrooms) using the SubroomPackingCalculator.
    /// The algorithm divides the room into horizontal strips and packs subrooms into available space,
    /// avoiding door exclusion zones and creating shared walls between adjacent subrooms.
    ///
    /// Available prefab sizes: 3x4, 3x5, 4x4, 4x5 (width x depth)
    ///
    /// Layout pattern:
    /// - Room divided into horizontal strips based on prefab depths
    /// - Top strips face South, bottom strip faces North
    /// - Corridors between strips (1 cell if facing, 2 cells if backs)
    /// - Exclusion zones around doors (3 cells for N/S, 2 cells for E/W)
    ///
    /// Subroom customization is handled by separate helper classes in the CrewQuarters/ subfolder:
    /// - MeditationSpotCustomizer: Replaces meditation spots with various items/creatures
    /// - ShelfCustomizer: Adds items to shelves and replaces empty shelves
    /// - TableCustomizer: Adds items to small tables
    /// </summary>
    public class RoomContents_CrewQuarters : RoomContentsWorker
    {
        /// <summary>
        /// Available prefab widths (perpendicular to door direction).
        /// Subrooms are fit starting with minimum width, then expanded to fill waste.
        /// </summary>
        private static readonly List<int> AVAILABLE_WIDTHS = new List<int> { 3, 4 };

        /// <summary>
        /// Available prefab depths (parallel to door direction).
        /// Strip depth is determined by the smallest available depth that fits.
        /// </summary>
        private static readonly List<int> AVAILABLE_DEPTHS = new List<int> { 4, 5 };

        /// <summary>
        /// Stores all subroom areas to prevent other prefabs from spawning inside them.
        /// Populated during FillRoom and checked in IsValidCellBase.
        /// </summary>
        private List<CellRect> subroomRects = new List<CellRect>();

        /// <summary>
        /// Stores all waste filler areas to prevent other prefabs from spawning inside them.
        /// Populated during FillRoom and checked in IsValidCellBase.
        /// </summary>
        private List<CellRect> wasteFillerRects = new List<CellRect>();

        /// <summary>
        /// Main room generation method. Calculates subroom packing, spawns walls and prefabs,
        /// then calls base class to process XML-defined content (lockers, etc.) in corridors.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Reset subroom and waste filler tracking
            subroomRects.Clear();
            wasteFillerRects.Clear();

            if (room.rects == null || room.rects.Count == 0)
            {
                Log.Warning("[Better Traders Guild] CrewQuarters room has no rects");
                base.FillRoom(map, room, faction, threatPoints);
                return;
            }

            CellRect roomRect = room.rects.First();

            // Get edge blockers (doors) from the room
            List<DoorPosition> doors = RoomEdgeBlockersHelper.GetEdgeBlockers(room, map);

            // Convert CellRect to SimpleRect for the calculator
            SimpleRect simpleRoom = new SimpleRect
            {
                MinX = roomRect.minX,
                MinZ = roomRect.minZ,
                Width = roomRect.Width,
                Height = roomRect.Height
            };

            // Calculate optimal subroom packing
            var input = new SubroomPackingInput
            {
                Room = simpleRoom,
                Doors = doors,
                AvailableWidths = AVAILABLE_WIDTHS,
                AvailableDepths = AVAILABLE_DEPTHS
            };

            SubroomPackingResult result = SubroomPackingCalculator.Calculate(input);

            if (result.Subrooms == null || result.Subrooms.Count == 0)
            {
                Log.Warning($"[Better Traders Guild] SubroomPackingCalculator returned no subrooms for room at {roomRect}");
                base.FillRoom(map, room, faction, threatPoints);
                return;
            }

            // 1. Spawn walls first (shared walls between subrooms, enclosing walls)
            if (result.Walls != null && result.Walls.Count > 0)
            {
                SpawnWallsFromSegments(map, result.Walls);
            }

            // 2. Spawn each subroom prefab and track their areas
            foreach (var subroom in result.Subrooms)
            {
                SpawnSubroomPrefab(map, subroom);

                // Calculate world-space bounds for the subroom
                var (worldWidth, worldHeight) = GetRotatedDimensions(subroom.Width, subroom.Depth, subroom.Rotation);
                CellRect subroomRect = new CellRect(subroom.MinX, subroom.MinZ, worldWidth, worldHeight);
                subroomRects.Add(subroomRect);
            }

            // 2b. Spawn waste filler prefabs in areas adjacent to exclusion zones
            if (result.WasteFillers != null)
            {
                foreach (var wasteFiller in result.WasteFillers)
                {
                    SpawnWasteFillerPrefab(map, wasteFiller);

                    // Track waste filler area to prevent XML content from spawning inside
                    CellRect wasteRect = new CellRect(wasteFiller.MinX, wasteFiller.MinZ, wasteFiller.Width, wasteFiller.Depth);
                    wasteFillerRects.Add(wasteRect);
                }
            }

            // 3. Apply random carpet colors to each subroom
            // Each subroom gets its own color from a curated neutral/muted palette
            SubroomCarpetCustomizer.Customize(map, subroomRects);

            // 4. Connect interior door rows to power grid
            // Middle strips with exclusion zones at both ends may have "floating island" subrooms
            // disconnected from the wall-based power grid. Running conduits along door rows
            // ensures all subrooms have power connectivity.
            CorridorPowerConnector.ConnectInteriorDoorRows(map, roomRect);

            // 5. Call base to process XML-defined content (lockers spawn in corridors/exclusion zones)
            base.FillRoom(map, room, faction, threatPoints);

            // 6. Customize subrooms with random items, furniture, and pawns
            // Run customizers in order - meditation spots may add plant pots which get plants later
            MeditationSpotCustomizer.Customize(map, subroomRects, faction);
            ShelfCustomizer.CustomizeSmallShelves(map, subroomRects);
            ShelfCustomizer.CustomizeEmptyShelves(map, subroomRects, faction);
            TableCustomizer.Customize(map, subroomRects);

            // 7. Post-processing: spawn plants in any plant pots (random mix of decorative plants)
            // Query all plants with "Decorative" sowTag - this automatically supports mod-added plants
            var decorativePlants = DefDatabase<ThingDef>.AllDefs
                .Where(p => p.plant?.sowTags?.Contains("Decorative") == true)
                .ToList();
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, decorativePlants, growth: 1.0f);
        }

        /// <summary>
        /// Override to prevent XML-defined prefabs (like lockers) from spawning inside subrooms or waste fillers.
        /// Called by base.FillRoom() during prefab placement validation.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block placement inside any subroom area
            foreach (var rect in subroomRects)
            {
                if (rect.Contains(c))
                    return false;
            }

            // Block placement inside any waste filler area
            foreach (var rect in wasteFillerRects)
            {
                if (rect.Contains(c))
                    return false;
            }

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Spawns a subroom prefab at the calculated position with appropriate rotation.
        ///
        /// LEARNING NOTE: PrefabUtility.SpawnPrefab uses center-based positioning.
        /// SubroomPlacement provides corner coordinates (MinX, MinZ), so we calculate
        /// the center using the CenterX/CenterZ properties which account for prefab size.
        /// </summary>
        private void SpawnSubroomPrefab(Map map, SubroomPlacement subroom)
        {
            // Get the prefab definition
            PrefabDef prefab = DefDatabase<PrefabDef>.GetNamed(subroom.PrefabDefName, false);

            if (prefab == null)
            {
                Log.Warning($"[Better Traders Guild] Could not find PrefabDef '{subroom.PrefabDefName}'");
                return;
            }

            // Calculate center position for spawning
            // The SubroomPlacement has MinX/MinZ (corner) and Width/Depth
            // CenterX/CenterZ calculate the center from these values
            IntVec3 centerPos = new IntVec3(subroom.CenterX, 0, subroom.CenterZ);

            // Convert rotation to Rot4
            Rot4 rotation = subroom.Rotation.AsRot4();

            // Spawn the prefab
            PrefabUtility.SpawnPrefab(prefab, map, centerPos, rotation, null);
        }

        /// <summary>
        /// Spawns a waste filler prefab at the calculated position.
        ///
        /// Waste fillers are decorative prefabs placed in 1-2 cell wide areas
        /// between subrooms and exclusion zones. Uses WasteFillerPrefabSelector
        /// to dynamically select from available prefabs of the correct size,
        /// automatically supporting DLC-dependent variants.
        /// </summary>
        private void SpawnWasteFillerPrefab(Map map, WasteFillerPlacement wasteFiller)
        {
            // Use the selector to pick a random prefab of the correct size
            PrefabDef prefab = WasteFillerPrefabSelector.SelectPrefab(wasteFiller.Width, wasteFiller.Depth);

            if (prefab == null)
            {
                // No prefab available for this size - silently skip
                // This is normal if no variants have been defined for this size
                return;
            }

            // Calculate center position for spawning
            IntVec3 centerPos = new IntVec3(wasteFiller.CenterX, 0, wasteFiller.CenterZ);

            // Convert rotation to Rot4
            // North = no rotation (face East), South = 180° (face West)
            Rot4 rotation = wasteFiller.Rotation.AsRot4();

            // Spawn the prefab
            PrefabUtility.SpawnPrefab(prefab, map, centerPos, rotation, null);
        }

        /// <summary>
        /// Spawns walls from the SubroomPackingCalculator's wall segments.
        /// These include shared walls between adjacent subrooms and enclosing walls
        /// that separate subrooms from corridors and exclusion zones.
        /// </summary>
        private void SpawnWallsFromSegments(Map map, List<WallSegment> walls)
        {
            ThingDef wallDef = Things.OrbitalAncientFortifiedWall;

            foreach (var wall in walls)
            {
                // Determine if vertical or horizontal wall
                if (wall.StartX == wall.EndX)
                {
                    // Vertical wall - iterate Z
                    for (int z = Math.Min(wall.StartZ, wall.EndZ); z <= Math.Max(wall.StartZ, wall.EndZ); z++)
                    {
                        SpawnWallCell(map, new IntVec3(wall.StartX, 0, z), wallDef);
                    }
                }
                else
                {
                    // Horizontal wall - iterate X
                    for (int x = Math.Min(wall.StartX, wall.EndX); x <= Math.Max(wall.StartX, wall.EndX); x++)
                    {
                        SpawnWallCell(map, new IntVec3(x, 0, wall.StartZ), wallDef);
                    }
                }
            }
        }

        /// <summary>
        /// Spawns a single wall cell if the location is valid and empty.
        /// </summary>
        private void SpawnWallCell(Map map, IntVec3 cell, ThingDef wallDef)
        {
            if (!cell.InBounds(map))
                return;

            // Don't overwrite existing edifices (doors, walls)
            Building existingEdifice = cell.GetEdifice(map);
            if (existingEdifice != null)
                return;

            Thing wallThing = ThingMaker.MakeThing(wallDef);
            GenSpawn.Spawn(wallThing, cell, map);
        }
    }
}
