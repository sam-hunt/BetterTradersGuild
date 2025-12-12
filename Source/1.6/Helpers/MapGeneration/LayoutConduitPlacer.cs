using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    /// <summary>
    /// Places hidden conduits and VE pipes under walls during layout generation.
    ///
    /// PURPOSE:
    /// Creates a station-wide power and resource network by placing hidden infrastructure
    /// under all walls in the generated structure. This ensures all electrical devices
    /// are connected to power sources (LifeSupportUnits) and VE pipe networks are unified.
    ///
    /// TECHNICAL APPROACH:
    /// - Iterates room rects' edge cells (walls are on rect edges, not interior)
    /// - O(perimeter) instead of O(area) - efficient for large structures
    /// - Places HiddenConduit under walls/doors (vanilla power network)
    /// - Also places any VE hidden pipes from HiddenPipeHelper
    ///
    /// LEARNING NOTE (Room Rect Edges):
    /// Room rects INCLUDE their walls. The edge cells of each rect correspond to
    /// the room's walls, so we iterate just the edges instead of checking every cell.
    /// For a 20x20 room: edges = 76 cells vs interior = 400 cells (5x fewer checks).
    /// </summary>
    public static class LayoutConduitPlacer
    {
        /// <summary>
        /// Places hidden conduits (and VE hidden pipes) under all wall and door cells.
        ///
        /// BEHAVIOR:
        /// - HiddenConduit under all walls and doors (invisible, clean aesthetics)
        /// - Also spawns any VE hidden pipes at same locations
        /// - Tracks processed cells to avoid duplicates at shared walls
        /// </summary>
        /// <param name="map">The map being generated</param>
        /// <param name="sketch">The LayoutStructureSketch containing structure data</param>
        /// <returns>Number of conduit positions placed</returns>
        public static int PlaceHiddenConduits(Map map, LayoutStructureSketch sketch)
        {
            StructureLayout layout = sketch.structureLayout;

            ThingDef hiddenConduitDef = DefDatabase<ThingDef>.GetNamed("HiddenConduit");
            IReadOnlyList<ThingDef> hiddenPipeDefs = HiddenPipeHelper.GetSupportedHiddenPipeDefs();

            int placedCount = 0;

            // Track cells we've already processed (rooms can share walls)
            HashSet<IntVec3> processedCells = new HashSet<IntVec3>();

            // Iterate through all rooms in the structure
            foreach (LayoutRoom room in layout.Rooms)
            {
                if (room.rects == null)
                    continue;

                // Iterate through all rects in the room (corridors have multiple rects)
                foreach (CellRect rect in room.rects)
                {
                    // Iterate edge cells only (walls are on edges, not interior)
                    foreach (IntVec3 edgeCell in rect.EdgeCells)
                    {
                        // Skip if already processed (shared walls between rooms)
                        if (!processedCells.Add(edgeCell))
                            continue;

                        // Bounds check (defensive)
                        if (!edgeCell.InBounds(map))
                            continue;

                        // Check what's at this edge cell
                        Building edifice = edgeCell.GetEdifice(map);
                        if (edifice == null)
                            continue;

                        // Only place conduits under walls and doors
                        if (!edifice.def.IsDoor && edifice.def.building?.isPlaceOverableWall != true)
                            continue;

                        // Create and spawn the conduit
                        Thing conduit = ThingMaker.MakeThing(hiddenConduitDef);
                        GenSpawn.Spawn(conduit, edgeCell, map);
                        placedCount++;

                        // Also spawn any VE hidden pipes at this location
                        foreach (ThingDef hiddenPipeDef in hiddenPipeDefs)
                        {
                            Thing pipe = ThingMaker.MakeThing(hiddenPipeDef);
                            GenSpawn.Spawn(pipe, edgeCell, map);
                        }
                    }
                }
            }

            return placedCount;
        }
    }
}
