using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep that spawns defensive prefabs outside each perimeter entrance door
    /// of TradersGuild settlements.
    ///
    /// Uses BTG_EntranceAutocannons prefab (11x5, odd dimensions for stable center).
    /// Prefab is designed for north-edge doors; GenStep rotates for other edges.
    ///
    /// ALGORITHM:
    /// 1. Get structure bounds from layout sketch
    /// 2. Iterate perimeter edge cells to find AncientBlastDoors
    /// 3. For each door, calculate center position 2 cells outward
    /// 4. Spawn prefab with rotation based on which edge the door is on
    /// </summary>
    public class GenStep_SpawnEntranceDefences : GenStep
    {
        /// <summary>
        /// Distance in cells from the door to place the prefab center (outward).
        /// </summary>
        private const int OutwardOffset = 2;

        /// <summary>
        /// Deterministic seed for this GenStep.
        /// </summary>
        public override int SeedPart => 847291006;

        /// <summary>
        /// Spawns defensive prefabs at each perimeter entrance door.
        /// </summary>
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null)
                return;

            // Get faction from settlement
            Settlement settlement = map.Parent as Settlement;
            Faction faction = settlement?.Faction;
            if (faction == null)
                return;

            // Get the prefab
            PrefabDef prefab = Prefabs.BTG_EntranceAutocannons;
            if (prefab == null)
                return;

            // Get structure bounds from layout sketch
            LayoutStructureSketch sketch = GetLayoutSketch(map);
            if (sketch == null)
                return;

            CellRect structureBounds = sketch.structureLayout.container;

            // Find all perimeter doors and spawn prefabs
            List<PerimeterDoorInfo> perimeterDoors = FindPerimeterDoors(map, structureBounds);

            foreach (PerimeterDoorInfo doorInfo in perimeterDoors)
                SpawnEntrancePrefab(map, prefab, doorInfo, faction);
        }

        /// <summary>
        /// Gets the first layout structure sketch from the map.
        /// </summary>
        private LayoutStructureSketch GetLayoutSketch(Map map)
        {
            if (map.layoutStructureSketches == null || map.layoutStructureSketches.Count == 0)
                return null;

            return map.layoutStructureSketches[0];
        }

        /// <summary>
        /// Finds all AncientBlastDoors on the structure's perimeter edge.
        /// </summary>
        private List<PerimeterDoorInfo> FindPerimeterDoors(Map map, CellRect structureBounds)
        {
            List<PerimeterDoorInfo> result = new List<PerimeterDoorInfo>();
            HashSet<IntVec3> processed = new HashSet<IntVec3>();

            foreach (IntVec3 cell in structureBounds.EdgeCells)
            {
                if (!cell.InBounds(map))
                    continue;

                if (processed.Contains(cell))
                    continue;

                Building edifice = cell.GetEdifice(map);
                if (edifice == null || edifice.def != Things.AncientBlastDoor)
                    continue;

                // Determine which edge this door is on
                CardinalEdge? edge = DetermineEdge(cell, structureBounds);
                if (!edge.HasValue)
                    continue;

                processed.Add(cell);
                result.Add(new PerimeterDoorInfo
                {
                    Position = cell,
                    Edge = edge.Value
                });
            }

            return result;
        }

        /// <summary>
        /// Determines which cardinal edge a cell is on within a rect.
        /// Returns null if the cell is on a corner (ambiguous).
        /// </summary>
        private CardinalEdge? DetermineEdge(IntVec3 cell, CellRect rect)
        {
            bool onNorth = cell.z == rect.maxZ;
            bool onSouth = cell.z == rect.minZ;
            bool onEast = cell.x == rect.maxX;
            bool onWest = cell.x == rect.minX;

            // Corner cells are ambiguous - skip them
            int edgeCount = (onNorth ? 1 : 0) + (onSouth ? 1 : 0) + (onEast ? 1 : 0) + (onWest ? 1 : 0);
            if (edgeCount != 1)
                return null;

            if (onNorth) return CardinalEdge.North;
            if (onSouth) return CardinalEdge.South;
            if (onEast) return CardinalEdge.East;
            if (onWest) return CardinalEdge.West;

            return null;
        }

        /// <summary>
        /// Spawns the entrance defense prefab at a perimeter door.
        /// Prefab is designed for north-edge doors; rotation adjusts for other edges.
        /// </summary>
        private void SpawnEntrancePrefab(Map map, PrefabDef prefab, PerimeterDoorInfo doorInfo, Faction faction)
        {
            // Calculate prefab center position (2 cells outward from door)
            IntVec3 outwardDir = GetOutwardDirection(doorInfo.Edge);
            IntVec3 prefabCenter = doorInfo.Position + (outwardDir * OutwardOffset);

            // Get rotation for this edge (prefab designed for North edge)
            Rot4 rotation = GetPrefabRotation(doorInfo.Edge);

            // Spawn prefab using center-based positioning with faction ownership
            PrefabUtility.SpawnPrefab(prefab, map, prefabCenter, rotation, faction);
        }

        /// <summary>
        /// Gets the direction vector pointing outward from an edge.
        /// </summary>
        private IntVec3 GetOutwardDirection(CardinalEdge edge)
        {
            switch (edge)
            {
                case CardinalEdge.North: return IntVec3.North;
                case CardinalEdge.South: return IntVec3.South;
                case CardinalEdge.East: return IntVec3.East;
                case CardinalEdge.West: return IntVec3.West;
                default: return IntVec3.Zero;
            }
        }

        /// <summary>
        /// Gets the rotation for the prefab based on which edge the door is on.
        /// Prefab is designed for north-edge doors (no rotation needed).
        /// </summary>
        private Rot4 GetPrefabRotation(CardinalEdge edge)
        {
            switch (edge)
            {
                case CardinalEdge.North: return Rot4.North; // No rotation
                case CardinalEdge.South: return Rot4.South; // 180 degrees
                case CardinalEdge.East: return Rot4.East;   // 90 degrees CW
                case CardinalEdge.West: return Rot4.West;   // 90 degrees CCW
                default: return Rot4.North;
            }
        }

        /// <summary>
        /// Information about a door on the structure perimeter.
        /// </summary>
        private struct PerimeterDoorInfo
        {
            public IntVec3 Position;
            public CardinalEdge Edge;
        }

        /// <summary>
        /// Cardinal edge of a rect.
        /// </summary>
        private enum CardinalEdge
        {
            North,  // maxZ edge
            South,  // minZ edge
            East,   // maxX edge
            West    // minX edge
        }
    }
}
