using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.ControlCenter
{
    /// <summary>
    /// Custom RoomContentsWorker for Control Center.
    ///
    /// Spawns a server room subroom with an L-shaped prefab (front + right side walls only)
    /// that can be placed in corners (preferred) or along edges (with procedural wall completion).
    /// The main room contains consoles and terminals for station operations.
    /// </summary>
    public class RoomContents_ControlCenter : RoomContentsWorker
    {
        // Prefab size (6x6)
        private const int SERVER_ROOM_PREFAB_SIZE = 6;

        // Stores the server room area to prevent other prefabs from spawning there
        private CellRect serverRoomRect;

        /// <summary>
        /// Main room generation method. Orchestrates server room placement and calls base class
        /// to process XML-defined content (prefabs, scatter, parts) in remaining space.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Initialize serverRoomRect to default (safety mechanism)
            this.serverRoomRect = default;

            // 1. Find best location for server room (prefer corners, avoid walls with doors)
            SubroomPlacementResult placement = SubroomPlacementHelper.FindBestPlacement(room, map, SERVER_ROOM_PREFAB_SIZE);

            if (placement.IsValid)
            {
                // 2. Calculate and store server room area for validation (prevents overlap)
                this.serverRoomRect = SubroomPlacementHelper.GetBlockingRect(
                    placement.Position, placement.Rotation, SERVER_ROOM_PREFAB_SIZE);

                // 3. Spawn server room prefab using PrefabUtility API
                SpawnServerRoomPrefab(map, placement);

                // 4. Spawn required walls from PlacementCalculator
                SubroomPlacementHelper.SpawnWalls(map, placement.RequiredWalls);
            }
            else
            {
                CellRect firstRect = room.rects?.FirstOrDefault() ?? default;
                Log.Warning($"[Better Traders Guild] Could not find valid placement for server room in ControlCenter at {firstRect}");
            }

            // 5. Call base to process XML (prefabs, scatter, parts)
            base.FillRoom(map, room, faction, threatPoints);

            // 6. Connect Ship_ComputerCore to room edge (power)
            foreach (var computer in RoomEdgeConnector.FindBuildingsInRoom(map, this.serverRoomRect, Things.Ship_ComputerCore))
                RoomEdgeConnector.ConnectToNearestEdge(map, computer.Position, room.rects.First(), Things.HiddenConduit);
        }

        /// <summary>
        /// Override to prevent XML prefabs from spawning in server room area.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            if (this.serverRoomRect.Width > 0 && this.serverRoomRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Spawns the server room prefab using PrefabUtility API.
        /// </summary>
        private void SpawnServerRoomPrefab(Map map, SubroomPlacementResult placement)
        {
            PrefabDef prefab = Prefabs.BTG_ServerRacks_Subroom;
            if (prefab == null) return;

            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }
    }
}
