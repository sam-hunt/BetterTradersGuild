using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.ShuttleBay
{
    /// <summary>
    /// Handles spawning of the cargo vault hatch in the ShuttleBay room.
    ///
    /// The hatch is a 3x3 hackable portal that leads to a secure cargo vault.
    /// It is placed in the exact center of the largest free rectangular region
    /// remaining after the shuttle landing pad area is excluded.
    ///
    /// IMPORTANT: SpawnHatch must be called BEFORE base.FillRoom() to ensure
    /// priority placement. At that point only the landing pad exists, so the
    /// hatch placement is guaranteed to succeed without needing fallback logic.
    ///
    /// RimWorld spawn behavior for 3x3 buildings:
    /// - GenSpawn.Spawn uses center-based positioning for odd-sized buildings
    /// - Position passed is the center cell (thing.Position)
    /// - OccupiedRect expands from center by (size-1)/2 = 1 cell in each direction
    ///
    /// When cargo vault is enabled: spawns BTG_CargoVaultHatch (hackable, relockable)
    /// When cargo vault is disabled: spawns BTG_CargoVaultHatch_Sealed (permanently sealed)
    /// </summary>
    public static class CargoVaultHatchSpawner
    {
        private const int HATCH_SIZE = 3;

        /// <summary>
        /// Finds the best position for the cargo hatch and spawns it.
        /// Returns the CellRect of the spawned hatch for blocking purposes, or default if spawn failed.
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="roomRect">The room's bounding rectangle.</param>
        /// <param name="excludedRect">Area to exclude (e.g., landing pad). Pass default/empty to exclude nothing.</param>
        /// <returns>The CellRect occupied by the hatch, or default if spawning failed.</returns>
        public static CellRect SpawnHatch(Map map, CellRect roomRect, CellRect excludedRect)
        {
            IntVec3 position = FindBestPosition(map, roomRect, excludedRect);
            if (!position.IsValid)
            {
                Log.Warning("[Better Traders Guild] Could not find valid position for cargo hatch in ShuttleBay.");
                return default;
            }

            // Choose hatch type based on cargo vault setting
            ThingDef hatchDef = BetterTradersGuildMod.Settings.enableCargoVault
                ? Things.BTG_CargoVaultHatch
                : Things.BTG_CargoVaultHatch_Sealed;

            if (hatchDef == null)
            {
                Log.Warning("[Better Traders Guild] Cargo vault hatch def not found.");
                return default;
            }

            Thing hatch = ThingMaker.MakeThing(hatchDef);
            GenSpawn.Spawn(hatch, position, map, WipeMode.VanishOrMoveAside);

            return GetBlockingRectFromCenter(position);
        }

        /// <summary>
        /// Calculates the blocking rect from a center position.
        /// Multi-cell things spawn from their center, so we expand outward.
        /// </summary>
        private static CellRect GetBlockingRectFromCenter(IntVec3 center)
        {
            int halfSize = HATCH_SIZE / 2;
            return new CellRect(center.x - halfSize, center.z - halfSize, HATCH_SIZE, HATCH_SIZE);
        }

        /// <summary>
        /// Finds the best position for a 3x3 hatch by centering it in the largest
        /// free rectangular region of the room.
        /// </summary>
        private static IntVec3 FindBestPosition(Map map, CellRect roomRect, CellRect excludedRect)
        {
            // Find the largest free region by checking candidate areas
            // The room is divided into potential regions based on the excluded rect position
            var freeRegions = FindFreeRegions(roomRect, excludedRect);

            // Sort by area descending to try largest regions first
            freeRegions.Sort((a, b) => b.Area.CompareTo(a.Area));

            foreach (var region in freeRegions)
            {
                IntVec3 position = FindCenteredPosition(map, region);
                if (position.IsValid)
                    return position;
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Finds rectangular free regions in the room by subtracting the excluded area.
        /// Returns up to 4 regions (above, below, left, right of excluded rect).
        /// </summary>
        private static List<CellRect> FindFreeRegions(CellRect roomRect, CellRect excludedRect)
        {
            var regions = new List<CellRect>();

            // Interior rect (1 cell margin from walls for hatch clearance)
            CellRect interior = roomRect.ContractedBy(1);

            if (excludedRect.Width == 0 || excludedRect.Height == 0)
            {
                // No exclusion - entire interior is free
                regions.Add(interior);
                return regions;
            }

            // Region to the LEFT of excluded area
            if (excludedRect.minX > interior.minX)
            {
                var leftRegion = new CellRect(
                    interior.minX,
                    interior.minZ,
                    excludedRect.minX - interior.minX,
                    interior.Height
                );
                if (leftRegion.Width >= HATCH_SIZE && leftRegion.Height >= HATCH_SIZE)
                    regions.Add(leftRegion);
            }

            // Region to the RIGHT of excluded area
            if (excludedRect.maxX < interior.maxX)
            {
                var rightRegion = new CellRect(
                    excludedRect.maxX + 1,
                    interior.minZ,
                    interior.maxX - excludedRect.maxX,
                    interior.Height
                );
                if (rightRegion.Width >= HATCH_SIZE && rightRegion.Height >= HATCH_SIZE)
                    regions.Add(rightRegion);
            }

            // Region BELOW excluded area (between left and right edges of excluded)
            if (excludedRect.minZ > interior.minZ)
            {
                var belowRegion = new CellRect(
                    excludedRect.minX,
                    interior.minZ,
                    excludedRect.Width,
                    excludedRect.minZ - interior.minZ
                );
                if (belowRegion.Width >= HATCH_SIZE && belowRegion.Height >= HATCH_SIZE)
                    regions.Add(belowRegion);
            }

            // Region ABOVE excluded area (between left and right edges of excluded)
            if (excludedRect.maxZ < interior.maxZ)
            {
                var aboveRegion = new CellRect(
                    excludedRect.minX,
                    excludedRect.maxZ + 1,
                    excludedRect.Width,
                    interior.maxZ - excludedRect.maxZ
                );
                if (aboveRegion.Width >= HATCH_SIZE && aboveRegion.Height >= HATCH_SIZE)
                    regions.Add(aboveRegion);
            }

            return regions;
        }

        /// <summary>
        /// Finds the centered position for the hatch within the given region.
        /// Since SpawnHatch is called before base.FillRoom(), the region is guaranteed
        /// to be clear (only the landing pad exists at this point).
        /// </summary>
        private static IntVec3 FindCenteredPosition(Map map, CellRect region)
        {
            IntVec3 center = region.CenterCell;
            CellRect hatchRect = GetBlockingRectFromCenter(center);

            // Validate all cells are clear (should always pass since we spawn before other prefabs)
            if (IsValidPlacement(map, hatchRect))
                return center;

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Checks if all cells in the rect are valid for hatch placement.
        /// Cells must be in bounds, not contain walls/doors/impassables.
        /// </summary>
        private static bool IsValidPlacement(Map map, CellRect rect)
        {
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                    return false;

                // Check for walls, doors, or other impassable buildings
                Building building = cell.GetEdifice(map);
                if (building != null)
                    return false;

                // Check terrain is standable
                if (!cell.Standable(map))
                    return false;
            }
            return true;
        }

    }
}
