using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

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
        /// Each item type maps to a deterministic shelf based on settlement ID.
        /// Pre-filters items that can't be stored on shelves (minified buildings, chunks, etc.).
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <param name="items">Items to spawn on shelves</param>
        /// <param name="settlementID">Settlement ID for deterministic shelf assignment</param>
        /// <returns>Items that couldn't fit on shelves (overflow) or can't be stored on shelves</returns>
        public static List<Thing> SpawnItemsOnShelves(Map map, CellRect roomRect, List<Thing> items, int settlementID)
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

            // Clustered placement: same item types together, deterministic shelf per type
            List<Thing> shelfOverflow = CargoPlacementHelper.PlaceItemsClustered(
                map, shelfItems, shelves, settlementID);

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
        /// MinifiedThings are never split since they can't be properly cloned via ThingMaker.
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

                // MinifiedThings cannot be split - ThingMaker.MakeThing creates empty crates
                // This shouldn't happen since MinifiedThings have stackLimit=1, but be safe
                if (item is MinifiedThing)
                {
                    Log.Warning($"[Better Traders Guild] MinifiedThing with stackCount > 1 detected (ThingID: {item.ThingID}, count: {item.stackCount}). Not splitting.");
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
        /// Tracks used cells for non-stackable items to prevent spawn conflicts.
        /// Items are placed deterministically based on settlementID; pawns use random placement.
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <param name="things">Things to spawn on floor</param>
        /// <param name="settlementID">Settlement ID for deterministic placement</param>
        /// <param name="exclusionRect">Optional rect to exclude from spawning (e.g., exit subroom area)</param>
        /// <returns>List of things that couldn't be spawned (should be returned to trade inventory)</returns>
        public static List<Thing> SpawnOnFloor(Map map, CellRect roomRect, List<Thing> things, int settlementID, CellRect? exclusionRect = null)
        {
            var unspawned = new List<Thing>();

            if (things == null || things.Count == 0)
            {
                return unspawned;
            }

            List<IntVec3> validCells = GetValidFloorCells(map, roomRect, exclusionRect);

            if (validCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No valid floor cells in cargo vault - returning items to trade inventory");
                unspawned.AddRange(things);
                return unspawned;
            }

            // Track cells used by non-stackable items to avoid spawn conflicts
            HashSet<IntVec3> usedCells = new HashSet<IntVec3>();

            // Separate pawns from items - pawns use random placement, items use deterministic
            List<Thing> items = things.Where(t => !(t is Pawn)).ToList();
            List<Thing> pawns = things.Where(t => t is Pawn).ToList();

            // Spawn items deterministically - each item type gets a position based on its defName hash
            foreach (Thing thing in items)
            {
                Thing notSpawned = SpawnWithDeterministicPlacement(map, thing, validCells, usedCells, settlementID);
                if (notSpawned != null)
                    unspawned.Add(notSpawned);
            }

            // Spawn pawns with random placement (they move around anyway)
            foreach (Thing pawn in pawns)
            {
                IntVec3 cell = validCells.RandomElement();
                GenSpawn.Spawn(pawn, cell, map);
            }

            return unspawned;
        }

        /// <summary>
        /// Spawns a thing with deterministic cell selection based on defName hash.
        /// Each item type always maps to the same floor position regardless of what other items exist.
        /// Handles stack splitting for oversized stacks.
        /// Non-stackable items track used cells to avoid spawn conflicts.
        /// </summary>
        /// <returns>The thing if it couldn't be spawned (to return to trade inventory), null if spawned successfully</returns>
        private static Thing SpawnWithDeterministicPlacement(Map map, Thing thing, List<IntVec3> validCells, HashSet<IntVec3> usedCells, int settlementID)
        {
            // Get deterministic starting cell index based on item type and settlement
            int startIndex = GetDeterministicCellIndex(thing.def.defName, settlementID, validCells.Count);
            bool isNonStackable = thing.def.stackLimit == 1;

            // MinifiedThings cannot be split - ThingMaker.MakeThing creates empty crates
            // Always spawn the original object directly, tracking the cell
            if (thing is MinifiedThing)
            {
                IntVec3 cell = FindUnusedCellFromIndex(validCells, usedCells, startIndex);
                if (!cell.IsValid)
                {
                    Log.Warning($"[Better Traders Guild] No available cell for MinifiedThing {thing.Label} - returning to trade inventory");
                    return thing;
                }
                if (!GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near))
                {
                    Log.Warning($"[Better Traders Guild] Failed to place MinifiedThing {thing.Label} - returning to trade inventory");
                    return thing;
                }
                thing.SetForbidden(true, false);
                usedCells.Add(thing.Position); // Use actual position since TryPlaceThing may adjust
                return null;
            }

            int stackLimit = thing.def.stackLimit;
            int remaining = thing.stackCount;

            // First stack uses the original thing
            if (remaining <= stackLimit)
            {
                // Fits in one stack
                IntVec3 cell = isNonStackable
                    ? FindUnusedCellFromIndex(validCells, usedCells, startIndex)
                    : validCells[startIndex];
                if (!cell.IsValid)
                {
                    Log.Warning($"[Better Traders Guild] No available cell for {thing.Label} - returning to trade inventory");
                    return thing;
                }
                if (!GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near))
                {
                    Log.Warning($"[Better Traders Guild] Failed to place {thing.Label} - returning to trade inventory");
                    return thing;
                }
                thing.SetForbidden(true, false);
                if (isNonStackable)
                    usedCells.Add(thing.Position);
                return null;
            }

            // Need multiple stacks - use seeded Rand for additional stack placement
            int seed = Gen.HashCombineInt(thing.def.defName.GetHashCode(), settlementID);
            Rand.PushState(seed);
            try
            {
                // Set original thing to stackLimit and spawn it at the deterministic position
                thing.stackCount = stackLimit;
                remaining -= stackLimit;

                IntVec3 firstCell = isNonStackable
                    ? FindUnusedCellFromIndex(validCells, usedCells, startIndex)
                    : validCells[startIndex];
                if (!firstCell.IsValid)
                {
                    // Restore original stack count before returning
                    thing.stackCount = stackLimit + remaining;
                    Log.Warning($"[Better Traders Guild] No available cell for {thing.Label} - returning to trade inventory");
                    return thing;
                }
                if (!GenPlace.TryPlaceThing(thing, firstCell, map, ThingPlaceMode.Near))
                {
                    // Restore original stack count before returning
                    thing.stackCount = stackLimit + remaining;
                    Log.Warning($"[Better Traders Guild] Failed to place {thing.Label} - returning to trade inventory");
                    return thing;
                }
                thing.SetForbidden(true, false);
                if (isNonStackable)
                    usedCells.Add(thing.Position);

                // Spawn additional stacks for the remainder using seeded Rand
                int unspawnedCount = 0;
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

                    IntVec3 cell = isNonStackable
                        ? FindUnusedCellSeeded(validCells, usedCells)
                        : validCells[Rand.Range(0, validCells.Count)];
                    if (!cell.IsValid)
                    {
                        Log.Warning($"[Better Traders Guild] No available cell for {newStack.Label} stack - returning to trade inventory");
                        unspawnedCount += thisStack;
                        newStack.Destroy(DestroyMode.Vanish);
                        continue;
                    }
                    if (!GenPlace.TryPlaceThing(newStack, cell, map, ThingPlaceMode.Near))
                    {
                        Log.Warning($"[Better Traders Guild] Failed to place {newStack.Label} stack - returning to trade inventory");
                        unspawnedCount += thisStack;
                        newStack.Destroy(DestroyMode.Vanish);
                        continue;
                    }
                    newStack.SetForbidden(true, false);
                    if (isNonStackable)
                        usedCells.Add(newStack.Position);
                }

                // If some stacks couldn't spawn, create a consolidated item to return
                if (unspawnedCount > 0)
                {
                    Thing remainder = ThingMaker.MakeThing(thing.def, thing.Stuff);
                    remainder.stackCount = unspawnedCount;
                    CompQuality origQuality = thing.TryGetComp<CompQuality>();
                    CompQuality remQuality = remainder.TryGetComp<CompQuality>();
                    if (origQuality != null && remQuality != null)
                    {
                        remQuality.SetQuality(origQuality.Quality, ArtGenerationContext.Outsider);
                    }
                    return remainder;
                }

                return null;
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>
        /// Gets a deterministic cell index based on item type and settlement ID.
        /// Same item type + settlement always maps to the same starting cell.
        /// Uses prime-based bit mixing to avoid clustering from similar defName prefixes.
        /// </summary>
        private static int GetDeterministicCellIndex(string defName, int settlementID, int cellCount)
        {
            int hash = Gen.HashCombineInt(defName.GetHashCode(), settlementID);
            hash = ScrambleHash(hash);
            return Mathf.Abs(hash) % cellCount;
        }

        /// <summary>
        /// Scrambles a hash value using prime multiplication and bit mixing.
        /// This ensures better distribution when the source hash has clustering
        /// (e.g., similar string prefixes producing similar hash codes).
        /// Uses MurmurHash3 finalizer constants for good avalanche properties.
        /// </summary>
        private static int ScrambleHash(int hash)
        {
            unchecked
            {
                hash ^= hash >> 16;
                hash *= (int)0x85ebca6b;
                hash ^= hash >> 13;
                hash *= (int)0xc2b2ae35;
                hash ^= hash >> 16;
            }
            return hash;
        }

        /// <summary>
        /// Finds the nearest unused cell to the deterministic starting position.
        /// Returns IntVec3.Invalid if no cells are available.
        /// </summary>
        private static IntVec3 FindUnusedCellFromIndex(List<IntVec3> validCells, HashSet<IntVec3> usedCells, int startIndex)
        {
            IntVec3 targetCell = validCells[startIndex];

            // If the target cell is available, use it directly
            if (!usedCells.Contains(targetCell))
                return targetCell;

            // Find the nearest unused cell spatially
            IntVec3 closest = IntVec3.Invalid;
            float closestDist = float.MaxValue;

            foreach (IntVec3 cell in validCells)
            {
                if (usedCells.Contains(cell))
                    continue;

                float dist = cell.DistanceToSquared(targetCell);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = cell;
                }
            }

            return closest;
        }

        /// <summary>
        /// Finds an unused cell using RimWorld's seeded Rand (caller must push state).
        /// Returns IntVec3.Invalid if no cells are available.
        /// </summary>
        private static IntVec3 FindUnusedCellSeeded(List<IntVec3> validCells, HashSet<IntVec3> usedCells)
        {
            // Fast path: if few cells used, just try random selection
            if (usedCells.Count < validCells.Count / 2)
            {
                for (int attempts = 0; attempts < 20; attempts++)
                {
                    IntVec3 cell = validCells[Rand.Range(0, validCells.Count)];
                    if (!usedCells.Contains(cell))
                        return cell;
                }
            }

            // Fallback: find any unused cell (deterministic order from list)
            foreach (IntVec3 cell in validCells)
            {
                if (!usedCells.Contains(cell))
                    return cell;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Spawns pawns on the floor of the room as factionless/wild with wandering behavior.
        ///
        /// Unlike vanilla trader caravans where pawns belong to the trader faction with
        /// lord duties (follow, flee, defend), cargo vault pawns are spawned factionless:
        ///
        /// - The vault can only be reached via hostile raid (player is already hostile to TradersGuild)
        /// - Pawns are trapped in space and cannot flee the map
        /// - Captured pawns shouldn't auto-side with their captors (TradersGuild)
        /// - Factionless represents thematic "apathy" of being imprisoned in a space vault
        /// - Wild animals won't trigger auto-attack from player pawns
        ///
        /// A Lord with LordJob_WanderNest is assigned to prevent pawns from pathing to the
        /// exit portal. Instead, they wander aimlessly in the vault area.
        ///
        /// This creates emergent gameplay where captured pawns may be "liberated" by the player.
        /// </summary>
        /// <param name="map">The map to spawn on</param>
        /// <param name="roomRect">The room boundaries</param>
        /// <param name="pawns">Pawns to spawn</param>
        /// <param name="exclusionRect">Optional rect to exclude from spawning (e.g., exit subroom area)</param>
        public static void SpawnPawns(Map map, CellRect roomRect, List<Pawn> pawns, CellRect? exclusionRect = null)
        {
            if (pawns == null || pawns.Count == 0)
                return;

            List<IntVec3> validCells = GetValidFloorCells(map, roomRect, exclusionRect);

            if (validCells.Count == 0)
            {
                Log.Warning("[Better Traders Guild] No valid floor cells for pawns in cargo vault");
                foreach (Pawn pawn in pawns)
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                return;
            }

            List<Pawn> spawnedPawns = new List<Pawn>();

            foreach (Pawn pawn in pawns)
            {
                IntVec3 cell = validCells.RandomElement();
                GenSpawn.Spawn(pawn, cell, map);

                // Clear faction - pawns in the cargo vault are factionless
                if (pawn.Faction != null)
                    pawn.SetFaction(null);

                spawnedPawns.Add(pawn);
            }

            // Assign wandering Lord to prevent pawns from pathing to exit portal
            if (spawnedPawns.Count > 0)
                AssignWanderLord(map, spawnedPawns);
        }

        /// <summary>
        /// Assigns a wandering Lord to the spawned pawns to prevent them from
        /// immediately pathing to the exit portal.
        ///
        /// Uses LordJob_WanderNest which assigns DutyDefOf.WanderNest to all pawns,
        /// making them wander aimlessly in the area.
        /// </summary>
        private static void AssignWanderLord(Map map, List<Pawn> pawns)
        {
            // Use Faction.OfAncients for the Lord (base game faction, no DLC required)
            // This doesn't change the pawns' faction, just groups them under a Lord
            Faction lordFaction = Faction.OfAncients;

            LordJob lordJob = new LordJob_WanderNest();
            LordMaker.MakeNewLord(lordFaction, lordJob, map, pawns);
        }

        /// <summary>
        /// Gets valid floor cells for spawning (walkable, not blocked by buildings).
        /// </summary>
        /// <param name="map">The map to check</param>
        /// <param name="roomRect">The room boundaries (use ContractedBy(1) to exclude edges)</param>
        /// <param name="exclusionRect">Optional rect to exclude from valid cells (e.g., exit subroom area)</param>
        /// <returns>List of valid cells for floor spawning</returns>
        public static List<IntVec3> GetValidFloorCells(Map map, CellRect roomRect, CellRect? exclusionRect = null)
        {
            var validCells = new List<IntVec3>();

            foreach (IntVec3 cell in roomRect.Cells)
            {
                if (!cell.InBounds(map))
                    continue;

                // Skip cells in the exclusion rect (e.g., exit subroom)
                if (exclusionRect.HasValue && exclusionRect.Value.Contains(cell))
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
