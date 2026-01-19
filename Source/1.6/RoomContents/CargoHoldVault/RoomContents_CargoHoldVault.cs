using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// RoomContentsWorker for the cargo vault room.
    /// Populates the room with items from the parent settlement's trade inventory.
    /// Uses weighted random selection (higher value = higher probability).
    /// Items are removed from trade inventory and spawned in the vault.
    ///
    /// Also spawns the exit subroom prefab (9x9 walled room with doors) in the center.
    /// The doors prevent wild animals from escaping through the portal.
    ///
    /// The exit subroom rect is calculated before base.FillRoom() and used in
    /// IsValidCellBase() to prevent XML prefabs (shelves) from spawning in the center.
    /// </summary>
    public class RoomContents_CargoVault : RoomContentsWorker
    {
        /// <summary>
        /// The rect where the exit subroom will be placed (9x9 center area).
        /// Calculated before FillRoom to block XML prefabs from spawning there.
        /// </summary>
        private CellRect exitSubroomRect;

        /// <summary>
        /// The spawn position for the exit subroom prefab.
        /// Stored alongside exitSubroomRect to avoid recalculation.
        /// </summary>
        private IntVec3 exitSubroomSpawnPos;

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            Log.Message("[BTG CargoVault] FillRoom called");

            if (room.rects == null || room.rects.Count == 0)
            {
                Log.Message("[BTG CargoVault] No room rects, exiting");
                return;
            }

            // Get bounding rect of all room rects for center calculation
            CellRect boundingRect = room.rects[0];
            for (int i = 1; i < room.rects.Count; i++)
                boundingRect = boundingRect.Encapsulate(room.rects[i]);

            // Calculate exit subroom placement BEFORE base.FillRoom so IsValidCellBase can block it
            CalculateExitSubroomPlacement(boundingRect);
            Log.Message($"[BTG CargoVault] Exit subroom exclusion rect: {exitSubroomRect}");

            // Process XML prefabs (shelves, etc.) - IsValidCellBase will block the center
            base.FillRoom(map, room, faction, threatPoints);

            // Spawn exit subroom prefab in the calculated center position
            CellRect? spawnedRect = SpawnExitSubroom(map);
            if (spawnedRect == null)
            {
                Log.Warning("[BTG CargoVault] Failed to spawn exit subroom");
            }

            // Navigate to parent settlement (may be null if defeated)
            Log.Message($"[BTG CargoVault] Map.Parent type: {map.Parent?.GetType().Name ?? "null"}");
            Settlement settlement = CargoVaultHelper.GetParentSettlement(map);
            if (settlement != null)
            {
                Log.Message($"[BTG CargoVault] Found settlement: {settlement.Label}");
            }
            else
            {
                Log.Message("[BTG CargoVault] No parent settlement (may be defeated), checking for cached stock");
            }

            // Get trade stock (handles fallback to cached stock if settlement defeated)
            ThingOwner<Thing> stock = CargoVaultHelper.GetStock(map);
            if (stock == null)
            {
                Log.Message("[BTG CargoVault] Stock is null, exiting");
                return;
            }
            if (stock.Count == 0)
            {
                Log.Message("[BTG CargoVault] Stock is empty, exiting");
                return;
            }
            Log.Message($"[BTG CargoVault] Stock has {stock.Count} items");

            // Select ALL cargo (removes from stock)
            List<Thing> cargo = CargoSelector.SelectCargo(stock);
            Log.Message($"[BTG CargoVault] Selected {cargo.Count} cargo items");

            if (cargo.Count == 0)
            {
                Log.Message("[BTG CargoVault] No cargo selected, exiting");
                return;
            }

            // Categorize into items and pawns
            CargoSelector.CategorizeItems(cargo, out List<Thing> items, out List<Pawn> pawns);
            Log.Message($"[BTG CargoVault] Categorized: {items.Count} items, {pawns.Count} pawns");

            // Get settlement ID for deterministic shelf placement
            // Uses fallback to cached ID if settlement was defeated
            int settlementID = CargoVaultHelper.GetSettlementId(map);

            // Track items that couldn't be spawned to return to trade inventory
            var unspawnedItems = new List<Thing>();

            // Spawn in each room rect
            foreach (CellRect roomRect in room.rects)
            {
                Log.Message($"[BTG CargoVault] Spawning in rect: {roomRect}");

                // Contract rect by 1 for floor spawning to avoid blocking doorways on room edges
                CellRect floorSpawnRect = roomRect.ContractedBy(1);

                // Items go on shelves first (deterministic placement), overflow to floor
                List<Thing> overflow = CargoSpawner.SpawnItemsOnShelves(map, roomRect, items, settlementID);
                Log.Message($"[BTG CargoVault] Shelf overflow: {overflow.Count} items");
                if (overflow.Count > 0)
                {
                    List<Thing> floorUnspawned = CargoSpawner.SpawnOnFloor(map, floorSpawnRect, overflow, exitSubroomRect);
                    unspawnedItems.AddRange(floorUnspawned);
                }

                // Pawns spawn on floor as factionless (see CargoSpawner.SpawnPawns for reasoning)
                // Unlike trader caravans, cargo vault pawns are "prisoners" who don't side with captors
                CargoSpawner.SpawnPawns(map, floorSpawnRect, pawns, exitSubroomRect);

                // Only spawn in first rect (cargo doesn't duplicate)
                break;
            }

            // Return any unspawned items to the trade inventory
            if (unspawnedItems.Count > 0)
            {
                Log.Message($"[BTG CargoVault] Returning {unspawnedItems.Count} unspawned items to trade inventory");
                foreach (Thing item in unspawnedItems)
                {
                    stock.TryAdd(item, canMergeWithExistingStacks: true);
                }
            }

            Log.Message("[BTG CargoVault] FillRoom completed");
        }

        /// <summary>
        /// Calculates the rect and spawn position for the exit subroom.
        /// Called before base.FillRoom() so IsValidCellBase can block XML prefabs from this area.
        /// Stores both exitSubroomRect and exitSubroomSpawnPos in class fields.
        /// </summary>
        private void CalculateExitSubroomPlacement(CellRect boundingRect)
        {
            PrefabDef prefab = Prefabs.BTG_CargoVaultExitSubroom;
            if (prefab == null)
            {
                Log.Warning("[BTG CargoVault] BTG_CargoVaultExitSubroom prefab not found for rect calculation");
                exitSubroomRect = default;
                exitSubroomSpawnPos = IntVec3.Invalid;
                return;
            }

            IntVec2 prefabSize = prefab.size;
            exitSubroomSpawnPos = SpawnPositionHelper.GetCenteredSpawnPosition(boundingRect, prefabSize, Rot4.North);
            exitSubroomRect = SpawnPositionHelper.GetOccupiedRect(exitSubroomSpawnPos, prefabSize, Rot4.North);
        }

        /// <summary>
        /// Spawns the exit subroom prefab at the pre-calculated position.
        /// </summary>
        /// <returns>The rect occupied by the subroom, or null if spawn failed</returns>
        private CellRect? SpawnExitSubroom(Map map)
        {
            PrefabDef prefab = Prefabs.BTG_CargoVaultExitSubroom;
            if (prefab == null)
            {
                Log.Warning("[BTG CargoVault] BTG_CargoVaultExitSubroom prefab not found");
                return null;
            }

            if (!exitSubroomSpawnPos.IsValid || !exitSubroomSpawnPos.InBounds(map))
            {
                Log.Warning($"[BTG CargoVault] Exit subroom spawn position {exitSubroomSpawnPos} invalid or out of bounds");
                return null;
            }

            PrefabUtility.SpawnPrefab(prefab, map, exitSubroomSpawnPos, Rot4.North, null);
            Log.Message($"[BTG CargoVault] Spawned exit subroom at {exitSubroomSpawnPos}");

            return exitSubroomRect;
        }

        /// <summary>
        /// Override IsValidCellBase to block XML prefabs (shelves, etc.) from spawning
        /// in the center area where the exit subroom will be placed.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block spawning in the exit subroom area
            if (exitSubroomRect.Width > 0 && exitSubroomRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }
    }
}
