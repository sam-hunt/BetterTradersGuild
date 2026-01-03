using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.MapGeneration;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents.TransportRoom
{
    /// <summary>
    /// Custom RoomContentsWorker for Transport Room (Shuttle Bay).
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
    /// 5. Call base.FillRoom() for XML-defined prefabs (forklift, edge furniture)
    /// 6. Connect AncientSealedCrate markers to room edge with conduits
    /// 7. Apply partial roofing (roof all cells except landing pad area)
    ///
    /// LEARNING NOTE (Placement Timing):
    /// The landingPadRect MUST be set BEFORE calling base.FillRoom() so that
    /// IsValidCellBase() can block XML-defined prefabs from spawning on the landing pad.
    /// This is the same pattern used in RoomContents_CommandersQuarters.
    /// </summary>
    public class RoomContents_TransportRoom : RoomContentsWorker
    {
        /// <summary>
        /// Size of the landing pad prefab (10x10).
        /// </summary>
        private const int LANDING_PAD_PREFAB_SIZE = 10;

        /// <summary>
        /// Prefab defName for the landing pad structure.
        /// When VGE is active, an XML patch modifies this prefab to use 5x1 vac barriers.
        /// </summary>
        private const string LANDING_PAD_PREFAB_DEFNAME = "BTG_ShuttleLandingPad_Subroom";

        /// <summary>
        /// Stores the landing pad rect to prevent XML-defined prefabs from spawning on it.
        /// Set BEFORE base.FillRoom() is called.
        /// </summary>
        private CellRect landingPadRect;

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Explicitly initialize landingPadRect to default (safety mechanism)
            // If placement fails, Width = 0, so IsValidCellBase won't block other prefabs
            this.landingPadRect = default;

            if (room.rects == null || room.rects.Count == 0)
            {
                Log.Warning("[Better Traders Guild] TransportRoom has no rects");
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

                // 3b. Paint the PassengerShuttle to Marble color
                PaintShuttleInLandingPad(map);

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
                Log.Warning($"[Better Traders Guild] Could not find valid placement for landing pad in TransportRoom at {roomRect}");
                // landingPadRect remains default (Width = 0), so IsValidCellBase won't block other prefabs
            }

            // 5. Call base to process XML (prefabs, scatter, parts)
            //    ALWAYS runs - spawns forklift etc. even if landing pad failed
            //    Other prefabs will avoid landing pad area if landingPadRect.Width > 0
            base.FillRoom(map, room, faction, threatPoints);

            // 6. Connect AncientSealedCrate markers to room edge with conduits
            ConnectMarkersToEdge(map, roomRect);

            // 7. Apply partial roofing (roof all cells except landing pad area)
            PartialRoofingHelper.ApplyRoofingWithExclusion(map, roomRect, this.landingPadRect);
        }

        /// <summary>
        /// Override to prevent XML-defined prefabs from spawning on the landing pad.
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

            return base.IsValidCellBase(thingDef, stuffDef, c, room, map);
        }

        /// <summary>
        /// Spawns the landing pad prefab using PrefabUtility API.
        /// The prefab is modified by XML patches when VGE is active (5x1 vac barriers instead of 1x1).
        ///
        /// LEARNING NOTE: PrefabUtility.SpawnPrefab() uses CENTER-BASED positioning!
        /// The IntVec3 position parameter specifies the CENTER of the prefab, not the min corner.
        /// </summary>
        private void SpawnLandingPadPrefab(Map map, SubroomPlacementResult placement)
        {
            PrefabDef prefab = DefDatabase<PrefabDef>.GetNamed(LANDING_PAD_PREFAB_DEFNAME, false);

            if (prefab == null)
            {
                Log.Error($"[Better Traders Guild] Could not find PrefabDef '{LANDING_PAD_PREFAB_DEFNAME}'");
                return;
            }

            // Spawn the prefab at the specified CENTER position with rotation
            // IMPORTANT: placement.Position is the CENTER of the prefab, not the min corner!
            PrefabUtility.SpawnPrefab(prefab, map, placement.Position, placement.Rotation, null);

            Log.Message($"[Better Traders Guild] Spawned {LANDING_PAD_PREFAB_DEFNAME} at {placement.Position} rotation {placement.Rotation}");
        }

        /// <summary>
        /// Paints the PassengerShuttle in the landing pad area to Marble color.
        /// Called immediately after prefab spawn so the shuttle exists on the map.
        /// </summary>
        private void PaintShuttleInLandingPad(Map map)
        {
            if (this.landingPadRect.Width == 0) return;

            // Find the PassengerShuttle in the landing pad area
            var furniture = PaintableFurnitureHelper.GetPaintableFurniture(map, this.landingPadRect);
            var shuttle = furniture.FirstOrDefault(b => b.def.defName == "PassengerShuttle");

            if (shuttle == null) return;
            PaintableFurnitureHelper.TryPaint(shuttle, "Marble");
        }

        /// <summary>
        /// Connects AncientSealedCrate markers to room edge using hidden conduits.
        /// The crate serves as a marker for where conduit connections should originate.
        /// </summary>
        private void ConnectMarkersToEdge(Map map, CellRect roomRect)
        {
            ThingDef hiddenConduitDef = DefDatabase<ThingDef>.GetNamed("HiddenConduit", false);
            if (hiddenConduitDef == null)
            {
                Log.Warning("[Better Traders Guild] HiddenConduit def not found, cannot connect markers");
                return;
            }

            // Also get hidden pipe defs for VE mods
            var hiddenPipeDefs = HiddenPipeHelper.GetSupportedHiddenPipeDefs();
            List<ThingDef> infrastructureDefs = new List<ThingDef> { hiddenConduitDef };
            infrastructureDefs.AddRange(hiddenPipeDefs);

            // Find AncientSealedCrate markers in the room
            var markers = RoomEdgeConnector.FindBuildingsInRoom(map, roomRect, "AncientSealedCrate");

            foreach (Building marker in markers)
            {
                // Connect each marker to the nearest room edge
                int placed = RoomEdgeConnector.ConnectToNearestEdge(map, marker.Position, roomRect, infrastructureDefs);
                if (placed > 0)
                {
                    Log.Message($"[Better Traders Guild] Connected AncientSealedCrate at {marker.Position} to room edge ({placed} infrastructure cells)");
                }
            }
        }
    }
}
