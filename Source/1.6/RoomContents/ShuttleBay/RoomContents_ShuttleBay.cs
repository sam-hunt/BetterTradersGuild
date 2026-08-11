using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.ShuttleBay
{
    // Custom RoomContentsWorker for Shuttle Bay.
    //
    // Spawns an L-shaped landing pad subroom with walls on front + right side only,
    // similar to RoomContents_CommandersQuarters. The subroom can be placed in corners
    // (preferred) or along edges (with procedural wall completion).
    //
    // Generation sequence:
    // 1. Find best placement for landing pad (prefer corners, avoid walls with doors)
    // 2. Calculate and store landing pad area for validation (prevents other prefab overlap)
    // 3. Spawn landing pad prefab (VGE-enhanced or vanilla version)
    // 4. Spawn required walls from PlacementCalculator (for edge/center placements)
    // 5. Calculate cargo hatch position (center of largest free area)
    // 6. Call base.FillRoom() for XML-defined prefabs (forklift, edge furniture)
    // 7. Connect AncientSealedCrate markers to room edge with conduits
    // 8. Apply partial roofing (roof all cells except landing pad area)
    // 9. Spawn cargo vault hatch (secured entrance)
    //
    // LEARNING NOTE (Placement Timing):
    // The landingPadRect and cargoHatchRect MUST be set BEFORE calling base.FillRoom()
    // so that IsValidCellBase() can block XML-defined prefabs from spawning on them.
    // This is the same pattern used in RoomContents_CommandersQuarters.
    public class RoomContents_ShuttleBay : RoomContentsWorker
    {
        // Size of the landing pad prefab (10x10).
        private const int LANDING_PAD_PREFAB_SIZE = 10;

        // Number of LifeSupportUnits to keep in the pressurized area (outside the landing pad).
        // Several are kept so the large bay holds temperature and can repressurize quickly.
        private const int LIFE_SUPPORT_UNITS_TO_KEEP = 3;

        // Stores the landing pad rect to prevent XML-defined prefabs from spawning on it.
        // Set BEFORE base.FillRoom() is called.
        private CellRect landingPadRect;

        // Stores the cargo hatch rect to prevent XML-defined prefabs from spawning on it.
        // Set BEFORE base.FillRoom() is called.
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

            // Use first rect for subroom placement (algorithm limitation)
            CellRect primaryRect = room.rects.First();

            // 1. Find best location for landing pad (prefer corners, avoid walls with doors)
            SubroomPlacementResult placement = SubroomPlacementHelper.FindBestPlacement(room, map, LANDING_PAD_PREFAB_SIZE);

            if (placement.IsValid)
            {
                // 2. Calculate and store landing pad area for validation (prevents other prefab overlap)
                this.landingPadRect = SubroomPlacementHelper.GetBlockingRect(placement.Position, placement.Rotation, LANDING_PAD_PREFAB_SIZE);

                // 3. Spawn landing pad prefab using PrefabUtility API
                SpawnLandingPadPrefab(map, placement);

                // 3a. Normalize the shuttle to always face east (the prefab machinery
                //     rotates contained things along with the pad placement)
                EnsureShuttleFacesEast(map);

                // 3b. Paint the PassengerShuttle to match the owning faction's color
                PaintShuttleInLandingPad(map, faction);

                // 3c. Connect the shuttle to the chemfuel pipe network (VE Chemfuel Expanded)
                // Landing pad is placed in the first rect, so use that for edge connection
                ConnectShuttleToPipeNetwork(map, primaryRect);

                // 4. Spawn required walls from PlacementCalculator (consolidated wall spawning)
                // PlacementCalculator.RequiredWalls contains all walls needed for this placement type:
                // - Corner: empty list (room walls provide everything)
                // - Edge: one wall segment (left side)
                // - Center: two wall segments (back + left)
                if (placement.RequiredWalls?.Count > 0)
                {
                    VacWallSegmentSpawner.SpawnWallsWithBarriers(map, placement.RequiredWalls);
                }
            }
            else
            {
                // Log warning but CONTINUE (other prefabs still spawn for graceful degradation)
                Log.Warning($"[Better Traders Guild] Could not find valid placement for landing pad in ShuttleBay at {primaryRect}");
                // landingPadRect remains default (Width = 0), so IsValidCellBase won't block other prefabs
            }

            // 5. Spawn cargo vault hatch BEFORE base.FillRoom() (priority placement, center of largest free area)
            //    At this point only the landing pad exists, so hatch placement is guaranteed to succeed
            //    Use primary rect for hatch placement (subroom algorithm limitation)
            this.cargoHatchRect = CargoVaultHatchSpawner.SpawnHatch(map, primaryRect, this.landingPadRect);

            // 6. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns forklift etc. even if landing pad failed
            //    Other prefabs will avoid landing pad and cargo hatch areas (hatch is now a physical building)
            base.FillRoom(map, room, faction, threatPoints);

            // 6b. Prune LifeSupportUnits: the XML over-spawns (6) so enough land in the
            //     pressurized area. Remove any inside the unroofed landing pad (vacuum -
            //     they heat nothing) and cap the pressurized-area count at a few units.
            PruneLifeSupportUnits(map, room);

            // 7. Connect AncientSealedCrate marker to room edge with conduits (search all rects)
            if (Things.HiddenConduit != null)
            {
                foreach (CellRect roomRect in room.rects)
                {
                    var marker = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, Things.AncientSealedCrate).FirstOrDefault();
                    if (marker != null)
                    {
                        RoomEdgeConnector.ConnectToNearestEdge(map, marker.Position, roomRect, new List<ThingDef> { Things.HiddenConduit });
                    }
                }
            }

            // 8. Apply partial roofing (roof all cells except landing pad area) - all rects
            foreach (CellRect roomRect in room.rects)
            {
                PartialRoofingHelper.ApplyRoofingWithExclusion(map, roomRect, this.landingPadRect);
            }
        }

        // Override to prevent XML-defined prefabs from spawning on the landing pad or cargo hatch.
        //
        // CRITICAL: This MUST block placement before spawning occurs. Post-spawn removal
        // doesn't work because other prefabs overwrite landing pad furniture at the same cells,
        // and removing them afterward leaves the landing pad furniture already destroyed.
        //
        // Called by base.FillRoom() during prefab placement validation.
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

        // Spawns the landing pad prefab using PrefabUtility API.
        // The prefab is modified by XML patches when VGE is active (5x1 vac barriers instead of 1x1).
        //
        // LEARNING NOTE: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning!
        // The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        private void SpawnLandingPadPrefab(Map map, SubroomPlacementResult placement)
        {
            PrefabDef prefab = Prefabs.BTG_ShuttleLandingPad_Subroom;
            if (prefab == null) return;

            // Spawn the prefab at the specified CENTER position with rotation
            // IMPORTANT: placement.Position is the CENTER of the prefab, not the min corner!
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);
        }

        // Finds the shuttle spawned by the landing pad prefab, or null if the pad
        // failed to place or the shuttle is missing.
        private Building FindShuttleInLandingPad(Map map)
        {
            if (this.landingPadRect.Width == 0) return null;

            var furniture = PaintableFurnitureHelper.GetPaintableFurniture(map, this.landingPadRect);
            return furniture.FirstOrDefault(b => b.def == Things.PassengerShuttle);
        }

        // The prefab machinery spawns the shuttle rotated along with the pad placement,
        // but the shuttle should always face east. Respawns it in place facing east.
        //
        // Safe for the 3x5 shuttle's rotated footprint: the prefab positions it at the
        // center of the pad's 7x7 interior, so the east-facing 5x3 footprint keeps at
        // least one cell of clearance from the pad walls under every prefab rotation.
        private void EnsureShuttleFacesEast(Map map)
        {
            Building shuttle = FindShuttleInLandingPad(map);
            if (shuttle == null || shuttle.Rotation == Rot4.East) return;

            IntVec3 center = shuttle.Position;
            shuttle.DeSpawn();
            GenSpawn.Spawn(shuttle, center, map, Rot4.East);
        }

        // Paints the shuttle in the landing pad area to match the owning faction's color:
        // the nearest paintable structure ColorDef to faction.Color (exact BTG_Rust for
        // TradersGuild, Structure_RedPastel for the smugglers den's Salvagers). Skips
        // painting on faction-less maps, leaving the vanilla shuttle look.
        // Called immediately after prefab spawn so the shuttle exists on the map.
        private void PaintShuttleInLandingPad(Map map, Faction faction)
        {
            if (faction == null) return;

            Building shuttle = FindShuttleInLandingPad(map);
            if (shuttle == null) return;

            PaintableFurnitureHelper.TryPaint(shuttle, PaintableFurnitureHelper.NearestStructureColor(faction.Color));
        }

        // Connects the shuttle in the landing pad area to the room edge via chemfuel pipes.
        // Does nothing if VE Chemfuel Expanded is not installed (VCHE_UndergroundChemfuelPipe will be null).
        private void ConnectShuttleToPipeNetwork(Map map, CellRect roomRect)
        {
            if (Things.VCHE_UndergroundChemfuelPipe == null) return;

            Building shuttle = FindShuttleInLandingPad(map);
            if (shuttle == null) return;

            // Connect shuttle position to nearest room edge via underground chemfuel pipes
            RoomEdgeConnector.ConnectToNearestEdge(map, shuttle.Position, roomRect, Things.VCHE_UndergroundChemfuelPipe);
        }

        // Prunes LifeSupportUnits down to a few in the pressurized area (outside the landing
        // pad subroom). The XML over-spawns (6) so enough land outside the pad; this removes
        // the surplus. The landing pad is unroofed/exposed to space, so any unit inside it is
        // always removed (it heats nothing in vacuum). Up to LIFE_SUPPORT_UNITS_TO_KEEP units
        // are retained outside the pad so the large bay holds temperature and can repressurize
        // quickly if the vac barriers are breached.
        private void PruneLifeSupportUnits(Map map, LayoutRoom room)
        {
            if (Things.LifeSupportUnit == null) return;

            // Collect every LifeSupportUnit across all room rects (dedup in case rects overlap)
            var units = new List<Building>();
            foreach (CellRect roomRect in room.rects)
            {
                foreach (Building unit in RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, Things.LifeSupportUnit))
                {
                    if (!units.Contains(unit))
                        units.Add(unit);
                }
            }

            // Partition into pressurized-area units and those inside the unroofed landing pad
            var outside = new List<Building>();
            var insidePad = new List<Building>();
            foreach (Building unit in units)
            {
                if (this.landingPadRect.Width > 0 && this.landingPadRect.Contains(unit.Position))
                    insidePad.Add(unit);
                else
                    outside.Add(unit);
            }

            // Always remove units inside the unroofed pad (vacuum - they heat nothing)
            foreach (Building unit in insidePad)
                unit.Destroy(DestroyMode.Vanish);

            // Cap the pressurized-area units, removing any beyond the keep count
            for (int i = LIFE_SUPPORT_UNITS_TO_KEEP; i < outside.Count; i++)
                outside[i].Destroy(DestroyMode.Vanish);
        }

    }
}
