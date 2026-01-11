using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.CommandersQuarters
{
    /// <summary>
    /// Custom RoomContentsWorker for Commander's Quarters.
    ///
    /// Spawns a secure bedroom subroom with an L-shaped prefab (front + right side walls only)
    /// that can be placed in corners (preferred) or along edges (with procedural wall completion).
    ///
    /// LEARNING NOTE: RoomContentsWorkers provide programmatic control over room generation,
    /// working alongside XML definitions. The three-phase system (PreFillRooms, FillRoom, PostFillRooms)
    /// allows custom structures to coexist with XML-defined prefabs, scatter items, and parts.
    /// </summary>
    public class RoomContents_CommandersQuarters : RoomContentsWorker
    {
        // Prefab actual size (6×6) - the content defined in XML
        private const int BEDROOM_PREFAB_SIZE = 6;

        // Stores the bedroom area to prevent other prefabs from spawning there
        private CellRect bedroomRect;

        /// <summary>
        /// Main room generation method. Orchestrates bedroom placement and calls base class
        /// to process XML-defined content (prefabs, scatter, parts) in remaining space.
        ///
        /// LEARNING NOTE: Call base.FillRoom() AFTER custom structure spawning to allow
        /// XML prefabs to spawn in the remaining valid space (controlled via IsValidCellBase).
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Explicitly initialize bedroomRect to default (safety mechanism)
            // If bedroom placement fails, Width = 0, so IsValidCellBase won't block lounge prefabs
            this.bedroomRect = default;

            // 1. Find best location for bedroom (prefer corners, avoid walls with doors)
            SubroomPlacementResult placement = SubroomPlacementHelper.FindBestPlacement(room, map, BEDROOM_PREFAB_SIZE);

            if (placement.IsValid)
            {
                // 2. Calculate and store bedroom area for validation (prevents lounge overlap)
                this.bedroomRect = SubroomPlacementHelper.GetBlockingRect(
                    placement.Position, placement.Rotation, BEDROOM_PREFAB_SIZE);

                // 3. Spawn bedroom prefab using PrefabUtility API
                SpawnBedroomPrefab(map, placement);

                // 4. Spawn unique weapon on shelf in bedroom
                CommandersWeaponSpawner.SpawnUniqueWeaponOnShelf(map, room, this.bedroomRect);

                // 5. Spawn required walls from PlacementCalculator
                SubroomPlacementHelper.SpawnWalls(map, placement.RequiredWalls);

                // 6. Spawn commander's pet at the animal bed
                SpawnPetAtAnimalBed(map, this.bedroomRect);
            }
            else
            {
                // Log warning but CONTINUE (lounge still spawns for graceful degradation)
                CellRect firstRect = room.rects?.FirstOrDefault() ?? default;
                Log.Warning($"[Better Traders Guild] Could not find valid placement for Commander's bedroom in room at {firstRect}");
                // NO RETURN - continue to spawn lounge furniture
            }

            // 7. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns lounge even if bedroom failed
            //    Lounge prefabs will avoid bedroom area if bedroomRect.Width > 0
            base.FillRoom(map, room, faction, threatPoints);

            // 8. Post-processing: Fix bookcase contents and spawn plants
            //    CRITICAL: This must happen AFTER base.FillRoom() since lounge
            //    bookshelves are spawned by base.FillRoom()
            //    ALWAYS runs - fixes books even if bedroom placement failed
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                RoomBookcaseHelper.InsertBooksIntoBookcases(map, roomRect);

                // 9. Spawn decorative plants (roses) in all plant pots
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Rose, growth: 1.0f);

                // 10. Connect VFE Spacer air purifier to power (does nothing if VFE Spacer not installed)
                RoomEdgeConnector.ConnectBuildingsToConduitNetwork(map, roomRect, Things.VFES_AirPurifier);
            }
        }

        /// <summary>
        /// Override to prevent lounge prefabs from spawning in bedroom area.
        ///
        /// CRITICAL: This MUST block placement before spawning occurs. Post-spawn removal
        /// doesn't work because lounge prefabs overwrite bedroom furniture at the same cells,
        /// and removing them afterward leaves the bedroom furniture already destroyed.
        ///
        /// Called by base.FillRoom() during prefab placement validation.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block lounge prefab placement in bedroom area (prevent furniture overwriting)
            if (this.bedroomRect.Width > 0 && this.bedroomRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Spawns the bedroom prefab using PrefabUtility API.
        /// The prefab contains the L-shaped walls (front + right side), furniture, and AncientBlastDoor.
        ///
        /// LEARNING NOTE: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning!
        /// The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        /// For a 6×6 prefab, the center is at (localX=3, localZ=3), and the prefab extends
        /// ±3 cells in each direction from that center point.
        /// </summary>
        private void SpawnBedroomPrefab(Map map, SubroomPlacementResult placement)
        {
            PrefabDef prefab = Prefabs.BTG_CommandersBedroom;
            if (prefab == null) return;

            // Spawn the prefab at the specified CENTER position with rotation
            // IMPORTANT: placement.Position is the CENTER of the 6×6 prefab, not the min corner!
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }

        /// <summary>
        /// Spawns a random pet (cat or dog) at the animal bed location in the bedroom.
        /// Searches the bedroom rect for the AnimalBed spawned by the prefab.
        /// </summary>
        private void SpawnPetAtAnimalBed(Map map, CellRect bedroomRect)
        {
            if (Things.AnimalBed == null)
                return;

            // Find the animal bed in the bedroom
            foreach (IntVec3 cell in bedroomRect)
            {
                if (!cell.InBounds(map)) continue;

                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing.def == Things.AnimalBed)
                    {
                        RoomPetHelper.SpawnPetAtPosition(map, thing.Position);
                        return;
                    }
                }
            }
        }
    }
}
