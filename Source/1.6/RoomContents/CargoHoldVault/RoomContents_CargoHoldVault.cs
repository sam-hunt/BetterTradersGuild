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
            if (room.rects == null || room.rects.Count == 0)
                return;

            // Get bounding rect of all room rects for center calculation
            CellRect boundingRect = room.rects[0];
            for (int i = 1; i < room.rects.Count; i++)
                boundingRect = boundingRect.Encapsulate(room.rects[i]);

            // Calculate exit subroom placement BEFORE base.FillRoom so IsValidCellBase can block it
            CalculateExitSubroomPlacement(boundingRect);

            // Process XML prefabs (shelves, etc.) - IsValidCellBase will block the center
            base.FillRoom(map, room, faction, threatPoints);

            // Spawn exit subroom prefab in the calculated center position
            CellRect? spawnedRect = SpawnExitSubroom(map);
            if (spawnedRect == null)
            {
                Log.Warning("[BTG CargoVault] Failed to spawn exit subroom");
            }

            // Get trade stock (handles fallback to cached stock if settlement defeated)
            ThingOwner<Thing> stock = CargoVaultHelper.GetStock(map);
            if (stock == null || stock.Count == 0)
                return;

            // Select ALL cargo (removes from stock)
            List<Thing> cargo = CargoSelector.SelectCargo(stock);
            if (cargo.Count == 0)
                return;

            // Categorize into items and pawns
            CargoSelector.CategorizeItems(cargo, out List<Thing> items, out List<Pawn> pawns);

            // Get settlement ID for deterministic shelf placement
            // Uses fallback to cached ID if settlement was defeated
            int settlementID = CargoVaultHelper.GetSettlementId(map);

            // Track items that couldn't be spawned to return to trade inventory
            var unspawnedItems = new List<Thing>();

            // Spawn in each room rect
            foreach (CellRect roomRect in room.rects)
            {
                // Contract rect by 1 for floor spawning to avoid blocking doorways on room edges
                CellRect floorSpawnRect = roomRect.ContractedBy(1);

                // Items go on shelves first (deterministic placement), overflow to floor
                List<Thing> overflow = CargoSpawner.SpawnItemsOnShelves(map, roomRect, items, settlementID);
                if (overflow.Count > 0)
                {
                    List<Thing> floorUnspawned = CargoSpawner.SpawnOnFloor(map, floorSpawnRect, overflow, settlementID, exitSubroomRect);
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
                foreach (Thing item in unspawnedItems)
                {
                    stock.TryAdd(item, canMergeWithExistingStacks: true);
                }
            }
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
                return null;

            if (!exitSubroomSpawnPos.IsValid || !exitSubroomSpawnPos.InBounds(map))
                return null;

            PrefabUtility.SpawnPrefab(prefab, map, exitSubroomSpawnPos, Rot4.North, null);
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
