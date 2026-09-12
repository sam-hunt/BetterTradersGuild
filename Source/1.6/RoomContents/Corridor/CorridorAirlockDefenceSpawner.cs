using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Corridor
{
    // Finds AncientBlastDoors on corridor rect edges and replaces each with an
    // airlock defense prefab (airlock + autocannons). The prefab's outfit stands
    // are stocked later by BTG_StockOutfitStands from the corridor def's
    // OutfitStandRuleExtension (Corridor.xml).
    //
    // Must run BEFORE base room content placement so the airlock space is
    // occupied and unavailable for other prefab placement.
    public static class CorridorAirlockDefenceSpawner
    {
        public static void SpawnAirlockDefences(Map map, LayoutRoom room, Faction faction)
        {
            PrefabDef prefab = Prefabs.BTG_AirlockDefences;
            if (prefab == null || room.rects == null)
                return;

            HashSet<IntVec3> processed = new HashSet<IntVec3>();

            foreach (CellRect rect in room.rects)
            {
                foreach (IntVec3 cell in rect.EdgeCells)
                {
                    if (!cell.InBounds(map) || processed.Contains(cell))
                        continue;

                    Building edifice = cell.GetEdifice(map);
                    if (edifice == null || edifice.def != Things.AncientBlastDoor)
                        continue;

                    Rot4? rotation = GetEdgeRotation(cell, rect);
                    if (!rotation.HasValue)
                        continue;

                    processed.Add(cell);
                    edifice.Destroy();
                    PrefabUtility.SpawnPrefab(prefab, map, cell, rotation.Value, faction);
                }
            }
        }

        // Returns the prefab rotation based on which edge of the rect the cell is on.
        // Returns null for corner cells (ambiguous edge).
        private static Rot4? GetEdgeRotation(IntVec3 cell, CellRect rect)
        {
            bool onNorth = cell.z == rect.maxZ;
            bool onSouth = cell.z == rect.minZ;
            bool onEast = cell.x == rect.maxX;
            bool onWest = cell.x == rect.minX;

            int edgeCount = (onNorth ? 1 : 0) + (onSouth ? 1 : 0) + (onEast ? 1 : 0) + (onWest ? 1 : 0);
            if (edgeCount != 1)
                return null;

            if (onNorth) return Rot4.North;
            if (onSouth) return Rot4.South;
            if (onEast) return Rot4.East;
            if (onWest) return Rot4.West;

            return null;
        }
    }
}
