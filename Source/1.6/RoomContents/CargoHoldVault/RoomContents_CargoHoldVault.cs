using System.Collections.Generic;
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
    /// </summary>
    public class RoomContents_CargoVault : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            Log.Message("[BTG CargoVault] FillRoom called");

            // Process XML prefabs first (shelves, turrets, exit portal)
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
            {
                Log.Message("[BTG CargoVault] No room rects, exiting");
                return;
            }

            // Check if cargo spawning is enabled
            float cargoPercent = BetterTradersGuildMod.Settings.cargoInventoryPercentage;
            Log.Message($"[BTG CargoVault] Cargo percent: {cargoPercent}");
            if (cargoPercent <= 0f)
            {
                Log.Message("[BTG CargoVault] Cargo disabled (0%), exiting");
                return;
            }

            // Navigate to parent settlement
            Log.Message($"[BTG CargoVault] Map.Parent type: {map.Parent?.GetType().Name ?? "null"}");
            Settlement settlement = CargoVaultHelper.GetParentSettlement(map);
            if (settlement == null)
            {
                Log.Message("[BTG CargoVault] No parent settlement found, exiting");
                return;
            }
            Log.Message($"[BTG CargoVault] Found settlement: {settlement.Label}");

            // Get trade stock
            ThingOwner<Thing> stock = CargoVaultHelper.GetStock(settlement);
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

            // Select cargo using weighted random (removes from stock)
            List<Thing> cargo = CargoSelector.SelectCargo(stock, cargoPercent);
            Log.Message($"[BTG CargoVault] Selected {cargo.Count} cargo items");

            if (cargo.Count == 0)
            {
                Log.Message("[BTG CargoVault] No cargo selected, exiting");
                return;
            }

            // Categorize into items and pawns
            CargoSelector.CategorizeItems(cargo, out List<Thing> items, out List<Pawn> pawns);
            Log.Message($"[BTG CargoVault] Categorized: {items.Count} items, {pawns.Count} pawns");

            // Spawn in each room rect
            foreach (CellRect roomRect in room.rects)
            {
                Log.Message($"[BTG CargoVault] Spawning in rect: {roomRect}");

                // Items go on shelves first, overflow to floor
                List<Thing> overflow = CargoSpawner.SpawnItemsOnShelves(map, roomRect, items);
                Log.Message($"[BTG CargoVault] Shelf overflow: {overflow.Count} items");
                if (overflow.Count > 0)
                {
                    CargoSpawner.SpawnOnFloor(map, roomRect, overflow);
                }

                // Pawns always spawn on floor
                CargoSpawner.SpawnPawns(map, roomRect, pawns);

                // Only spawn in first rect (cargo doesn't duplicate)
                break;
            }

            Log.Message("[BTG CargoVault] FillRoom completed");
        }
    }
}
