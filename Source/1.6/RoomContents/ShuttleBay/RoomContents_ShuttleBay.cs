using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.ShuttleBay
{
    /// <summary>
    /// Custom RoomContentsWorker for Shuttle Bay.
    ///
    /// Spawns an L-shaped landing pad subroom with walls on front + right side only,
    /// similar to RoomContents_CommandersQuarters. The subroom can be placed in corners
    /// (preferred) or along edges (with procedural wall completion).
    ///
    /// Generation sequence:
    /// 1. Find best placement for landing pad (prefer corners, avoid walls with doors)
    /// 2. Calculate and store landing pad area for validation (prevents other prefab overlap)
    /// 3. Spawn landing pad prefab (VGE-enhanced or vanilla version)
    /// 4. Spawn required walls from PlacementCalculator (for edge/center placements)
    /// 5. Calculate cargo hatch position (center of largest free area)
    /// 6. Call base.FillRoom() for XML-defined prefabs (forklift, edge furniture)
    /// 7. Connect AncientSealedCrate markers to room edge with conduits
    /// 8. Apply partial roofing (roof all cells except landing pad area)
    /// 9. Spawn cargo vault hatch (secured entrance)
    ///
    /// LEARNING NOTE (Placement Timing):
    /// The landingPadRect and cargoHatchRect MUST be set BEFORE calling base.FillRoom()
    /// so that IsValidCellBase() can block XML-defined prefabs from spawning on them.
    /// This is the same pattern used in RoomContents_CommandersQuarters.
    /// </summary>
    public class RoomContents_ShuttleBay : RoomContentsWorker
    {
        /// <summary>
        /// Size of the landing pad prefab (10x10).
        /// </summary>
        private const int LANDING_PAD_PREFAB_SIZE = 10;

        /// <summary>
        /// Stores the landing pad rect to prevent XML-defined prefabs from spawning on it.
        /// Set BEFORE base.FillRoom() is called.
        /// </summary>
        private CellRect landingPadRect;

        /// <summary>
        /// Stores the cargo hatch rect to prevent XML-defined prefabs from spawning on it.
        /// Set BEFORE base.FillRoom() is called.
        /// </summary>
        private CellRect cargoHatchRect;

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Explicitly initialize rects to default (safety mechanism)
            // If placement fails, Width = 0, so IsValidCellBase won't block other prefabs
            this.landingPadRect = default;
            this.cargoHatchRect = default;

            if (room.rects == null || room.rects.Count == 0)
            {
                Log.Warning("[Better Traders Guild] ShuttleBay has no rects");
                base.FillRoom(map, room, faction, threatPoints);
                return;
            }

            CellRect roomRect = room.rects.First();

            // 1. Find best location for landing pad (prefer corners, avoid walls with doors)
            SubroomPlacementResult placement = SubroomPlacementHelper.FindBestPlacement(room, map, LANDING_PAD_PREFAB_SIZE);

            if (placement.IsValid)
            {
                // 2. Calculate and store landing pad area for validation (prevents other prefab overlap)
                this.landingPadRect = SubroomPlacementHelper.GetBlockingRect(placement.Position, placement.Rotation, LANDING_PAD_PREFAB_SIZE);

                // 3. Spawn landing pad prefab using PrefabUtility API
                SpawnLandingPadPrefab(map, placement);

                // 3b. Paint the PassengerShuttle to the faction's color
                PaintShuttleInLandingPad(map, faction);

                // 3c. Connect the shuttle to the chemfuel pipe network (VE Chemfuel Expanded)
                ConnectShuttleToPipeNetwork(map, roomRect);

                // 4. Spawn required walls from PlacementCalculator (consolidated wall spawning)
                // PlacementCalculator.RequiredWalls contains all walls needed for this placement type:
                // - Corner: empty list (room walls provide everything)
                // - Edge: one wall segment (left side)
                // - Center: two wall segments (back + left)
                if (placement.RequiredWalls != null && placement.RequiredWalls.Count > 0)
                {
                    VacWallSegmentSpawner.SpawnWallsWithBarriers(map, placement.RequiredWalls);
                }
            }
            else
            {
                // Log warning but CONTINUE (other prefabs still spawn for graceful degradation)
                Log.Warning($"[Better Traders Guild] Could not find valid placement for landing pad in ShuttleBay at {roomRect}");
                // landingPadRect remains default (Width = 0), so IsValidCellBase won't block other prefabs
            }

            // 5. Calculate cargo hatch position (center of largest free area, BEFORE base.FillRoom)
            this.cargoHatchRect = CargoVaultHatchSpawner.CalculateBlockingRect(map, roomRect, this.landingPadRect);

            // 6. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns forklift etc. even if landing pad failed
            //    Other prefabs will avoid landing pad and cargo hatch areas
            base.FillRoom(map, room, faction, threatPoints);

            // 6b. Prune LifeSupportUnits to keep only one outside the landing pad subroom
            //     XML spawns 4 to ensure at least one lands in the pressurized area
            PruneLifeSupportUnits(map, roomRect);

            // 7. Connect AncientSealedCrate marker to room edge with conduits
            if (Things.HiddenConduit != null)
            {
                var marker = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, Things.AncientSealedCrate).FirstOrDefault();
                if (marker != null)
                {
                    RoomEdgeConnector.ConnectToNearestEdge(map, marker.Position, roomRect, new List<ThingDef> { Things.HiddenConduit });
                }
            }

            // 8. Apply partial roofing (roof all cells except landing pad area)
            PartialRoofingHelper.ApplyRoofingWithExclusion(map, roomRect, this.landingPadRect);

            // 9. Spawn cargo vault hatch (secure vault entrance, center of largest free area)
            CargoVaultHatchSpawner.SpawnHatch(map, roomRect, this.landingPadRect);
        }

        /// <summary>
        /// Override to prevent XML-defined prefabs from spawning on the landing pad or cargo hatch.
        ///
        /// CRITICAL: This MUST block placement before spawning occurs. Post-spawn removal
        /// doesn't work because other prefabs overwrite landing pad furniture at the same cells,
        /// and removing them afterward leaves the landing pad furniture already destroyed.
        ///
        /// Called by base.FillRoom() during prefab placement validation.
        /// </summary>
        protected override bool IsValidCellBase(ThingDef thingDef, ThingDef stuffDef, IntVec3 c, LayoutRoom room, Map map)
        {
            // Block prefab placement in landing pad area (prevent furniture overwriting)
            if (this.landingPadRect.Width > 0 && this.landingPadRect.Contains(c))
                return false;

            // Block prefab placement in cargo hatch area (3x3 hatch needs clear space)
            if (this.cargoHatchRect.Width > 0 && this.cargoHatchRect.Contains(c))
                return false;

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Spawns the landing pad prefab using PrefabUtility API.
        /// The prefab is modified by XML patches when VGE is active (5x1 vac barriers instead of 1x1)
        /// or when Orca Shuttle mod is active (larger shuttle with repositioned coordinates).
        ///
        /// LEARNING NOTE: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning!
        /// The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        /// </summary>
        private void SpawnLandingPadPrefab(Map map, SubroomPlacementResult placement)
        {
            PrefabDef prefab = Prefabs.BTG_ShuttleLandingPad_Subroom;
            if (prefab == null) return;

            // Spawn the prefab at the specified CENTER position with rotation
            // IMPORTANT: placement.Position is the CENTER of the prefab, not the min corner!
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }

        /// <summary>
        /// Paints the shuttle in the landing pad area to the faction's color.
        /// Handles both vanilla PassengerShuttle and OrcaShuttle (when mod is active).
        /// Called immediately after prefab spawn so the shuttle exists on the map.
        /// </summary>
        private void PaintShuttleInLandingPad(Map map, Faction faction)
        {
            if (this.landingPadRect.Width == 0) return;
            if (faction == null) return;

            // Find the shuttle in the landing pad area (PassengerShuttle or OrcaShuttle)
            var furniture = PaintableFurnitureHelper.GetPaintableFurniture(map, this.landingPadRect);
            var shuttle = furniture.FirstOrDefault(b =>
                b.def == Things.PassengerShuttle ||
                (Things.OrcaShuttle != null && b.def == Things.OrcaShuttle));

            if (shuttle == null) return;
            PaintableFurnitureHelper.TryPaint(shuttle, faction.Color);
        }

        /// <summary>
        /// Connects the shuttle in the landing pad area to the room edge via chemfuel pipes.
        /// Does nothing if VE Chemfuel Expanded is not installed (VCHE_UndergroundChemfuelPipe will be null).
        /// </summary>
        private void ConnectShuttleToPipeNetwork(Map map, CellRect roomRect)
        {
            if (this.landingPadRect.Width == 0) return;
            if (Things.VCHE_UndergroundChemfuelPipe == null) return;

            // Find the shuttle in the landing pad area (PassengerShuttle or OrcaShuttle)
            var furniture = PaintableFurnitureHelper.GetPaintableFurniture(map, this.landingPadRect);
            var shuttle = furniture.FirstOrDefault(b =>
                b.def == Things.PassengerShuttle ||
                (Things.OrcaShuttle != null && b.def == Things.OrcaShuttle));

            if (shuttle == null) return;

            // Connect shuttle position to nearest room edge via underground chemfuel pipes
            RoomEdgeConnector.ConnectToNearestEdge(map, shuttle.Position, roomRect, Things.VCHE_UndergroundChemfuelPipe);
        }

        /// <summary>
        /// Prunes LifeSupportUnits to keep only one that is outside the landing pad subroom.
        /// The subroom is unroofed/exposed to space, so LifeSupportUnits shouldn't be there.
        /// XML spawns 4 units to ensure at least one lands in the pressurized area.
        /// </summary>
        private void PruneLifeSupportUnits(Map map, CellRect roomRect)
        {
            if (Things.LifeSupportUnit == null) return;

            // Find all LifeSupportUnits in the room (uses cell iteration, works for any faction)
            var units = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, Things.LifeSupportUnit);

            if (units.Count <= 1) return;

            // Find first unit outside the landing pad subroom (preferred)
            Building keepUnit = units.FirstOrDefault(b =>
                this.landingPadRect.Width == 0 || !this.landingPadRect.Contains(b.Position));

            // Fallback: keep the last one if all are in the subroom
            if (keepUnit == null)
                keepUnit = units.Last();

            // Despawn all others
            foreach (var unit in units)
            {
                if (unit != keepUnit)
                    unit.Destroy(DestroyMode.Vanish);
            }
        }

    }
}
