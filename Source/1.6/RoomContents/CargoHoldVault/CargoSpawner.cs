using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// Handles spawning cargo items in the cargo vault.
    /// Items go on shelves first (clustered by type), overflow and pawns go on floor.
    /// </summary>
    public static class CargoSpawner
    {
        /// <summary>
        /// Spawns items on shelves in the room using clustered placement.
        /// Items of the same type are placed adjacently on shelves.
        /// Each item type starts at a random shelf to spread cargo across the room.
        /// Pre-filters items that can't be stored on shelves (minified buildings, chunks, etc.).
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <param name="items">Items to spawn on shelves</param>
        /// <returns>Items that couldn't fit on shelves (overflow) or can't be stored on shelves</returns>
        public static List<Thing> SpawnItemsOnShelves(Map map, CellRect roomRect, List<Thing> items)
        {
            if (items == null || items.Count == 0)
            {
                return new List<Thing>();
            }

            // Get all storage buildings in the room (shelves, industrial shelves, etc.)
            List<Building_Storage> shelves = GetAllStorageInRoom(map, roomRect);

            if (shelves.Count == 0)
            {
                // No shelves - everything goes to floor
                return new List<Thing>(items);
            }

            // Pre-split oversized stacks into shelf-compatible stacks
            List<Thing> splitItems = SplitOversizedStacks(items);

            // Categorize: shelf-compatible vs floor-only
            CargoPlacementHelper.CategorizeForPlacement(
                splitItems, shelves,
                out List<Thing> shelfItems,
                out List<Thing> floorItems);

            // Clustered placement: same item types together, each type starts at random shelf
            List<Thing> shelfOverflow = CargoPlacementHelper.PlaceItemsClustered(
                map, shelfItems, shelves);

            // Combine floor-only + shelf overflow
            floorItems.AddRange(shelfOverflow);
            return floorItems;
        }

        /// <summary>
        /// Gets all storage buildings in the room (shelves, industrial shelves, etc.).
        /// </summary>
        /// <param name="map">The map to search</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <returns>List of all storage buildings in the room</returns>
        private static List<Building_Storage> GetAllStorageInRoom(Map map, CellRect roomRect)
        {
            var storage = new HashSet<Building_Storage>();

            foreach (IntVec3 cell in roomRect.Cells)
            {
                if (!cell.InBounds(map))
                    continue;

                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Building_Storage bs)
                        storage.Add(bs);
                }
            }

            return storage.ToList();
        }

        /// <summary>
        /// Splits items with stackCount > stackLimit into multiple Things.
        /// </summary>
        private static List<Thing> SplitOversizedStacks(List<Thing> items)
        {
            var result = new List<Thing>();

            foreach (Thing item in items)
            {
                int stackLimit = item.def.stackLimit;

                if (item.stackCount <= stackLimit)
                {
                    // Already fits in one stack
                    result.Add(item);
                    continue;
                }

                // Split into multiple stacks
                int remaining = item.stackCount;

                // First stack uses the original thing
                item.stackCount = stackLimit;
                remaining -= stackLimit;
                result.Add(item);

                // Create additional stacks for the remainder
                while (remaining > 0)
                {
                    int thisStack = UnityEngine.Mathf.Min(remaining, stackLimit);
                    remaining -= thisStack;

                    Thing newStack = ThingMaker.MakeThing(item.def, item.Stuff);
                    newStack.stackCount = thisStack;

                    // Copy quality if applicable
                    CompQuality origQuality = item.TryGetComp<CompQuality>();
                    CompQuality newQuality = newStack.TryGetComp<CompQuality>();
                    if (origQuality != null && newQuality != null)
                    {
                        newQuality.SetQuality(origQuality.Quality, ArtGenerationContext.Outsider);
                    }

                    result.Add(newStack);
                }
            }

            return result;
        }

        /// <summary>
        /// Spawns things on the floor of the room.
        /// Used for pawns and items that couldn't fit on shelves.
        /// Handles oversized stacks by splitting into multiple spawns.
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <param name="things">Things to spawn on floor</param>
        public static void SpawnOnFloor(Map map, CellRect roomRect, List<Thing> things)
        {
            if (things == null || things.Count == 0)
            {
                return;
            }

            List<IntVec3> validCells = GetValidFloorCells(map, roomRect);

            if (validCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No valid floor cells in cargo vault");
                // Destroy things to prevent leaks
                foreach (Thing thing in things)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
                return;
            }

            foreach (Thing thing in things)
            {
                // Spawn with stack splitting for oversized stacks
                SpawnWithStackSplitting(map, thing, validCells);
            }
        }

        /// <summary>
        /// Spawns a thing, splitting into multiple stacks if stackCount exceeds stackLimit.
        /// </summary>
        private static void SpawnWithStackSplitting(Map map, Thing thing, List<IntVec3> validCells)
        {
            if (thing is Pawn)
            {
                // Pawns don't stack, just spawn directly
                IntVec3 cell = validCells.RandomElement();
                GenSpawn.Spawn(thing, cell, map);
                return;
            }

            int stackLimit = thing.def.stackLimit;
            int remaining = thing.stackCount;

            // First stack uses the original thing
            if (remaining <= stackLimit)
            {
                // Fits in one stack
                IntVec3 cell = validCells.RandomElement();
                GenSpawn.Spawn(thing, cell, map);
                thing.SetForbidden(true, false);
                return;
            }

            // Need multiple stacks
            // Set original thing to stackLimit and spawn it
            thing.stackCount = stackLimit;
            remaining -= stackLimit;

            IntVec3 firstCell = validCells.RandomElement();
            GenSpawn.Spawn(thing, firstCell, map);
            thing.SetForbidden(true, false);

            // Spawn additional stacks for the remainder
            while (remaining > 0)
            {
                int thisStack = UnityEngine.Mathf.Min(remaining, stackLimit);
                remaining -= thisStack;

                Thing newStack = ThingMaker.MakeThing(thing.def, thing.Stuff);
                newStack.stackCount = thisStack;

                // Copy quality if applicable
                CompQuality origQuality = thing.TryGetComp<CompQuality>();
                CompQuality newQuality = newStack.TryGetComp<CompQuality>();
                if (origQuality != null && newQuality != null)
                {
                    newQuality.SetQuality(origQuality.Quality, ArtGenerationContext.Outsider);
                }

                IntVec3 cell = validCells.RandomElement();
                GenSpawn.Spawn(newStack, cell, map);
                newStack.SetForbidden(true, false);
            }
        }

        /// <summary>
        /// Spawns pawns on the floor of the room.
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <param name="pawns">Pawns to spawn</param>
        public static void SpawnPawns(Map map, CellRect roomRect, List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            List<IntVec3> validCells = GetValidFloorCells(map, roomRect);

            if (validCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No valid floor cells for pawns in cargo vault");
                // Pass pawns to world to prevent them from being lost
                foreach (Pawn pawn in pawns)
                {
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                }
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                IntVec3 cell = validCells.RandomElement();
                GenSpawn.Spawn(pawn, cell, map);
            }
        }

        /// <summary>
        /// Gets valid floor cells for spawning (walkable, not blocked by buildings).
        /// </summary>
        /// <param name="map">The map to check</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <returns>List of valid cells for floor spawning</returns>
        public static List<IntVec3> GetValidFloorCells(Map map, CellRect roomRect)
        {
            var validCells = new List<IntVec3>();

            foreach (IntVec3 cell in roomRect.Cells)
            {
                if (!cell.InBounds(map))
                    continue;

                // Must be walkable
                if (!cell.Walkable(map))
                    continue;

                // Check for blocking buildings (but allow shelves, storage)
                Building building = cell.GetEdifice(map);
                if (building != null && !building.def.passability.HasFlag(Traversability.Standable))
                {
                    // Skip cells with impassable buildings
                    if (building.def.passability == Traversability.Impassable)
                        continue;
                }

                validCells.Add(cell);
            }

            return validCells;
        }
    }
}
