using BetterTradersGuild.DefRefs;
using Verse;

namespace BetterTradersGuild.RoomContents.ShuttleBay
{
    // Applies roofing to rooms with optional exclusion zones (open-sky areas).
    //
    // Used by rooms that need partial roofing:
    // - ShuttleBay: Landing pad area left unroofed for shuttle access
    // - Greenhouse: Could have skylight areas
    // - Any room requiring mixed indoor/outdoor cells
    public static class PartialRoofingHelper
    {
        // Applies roofing to a room, excluding cells within the specified rect.
        // map: The map to modify.
        // roomRect: The full room bounds to roof.
        // exclusionRect: Area to leave unroofed (open to sky). If Width == 0, roofs entire room.
        // roofDef: RoofDef to use (defaults to RoofConstructed).
        public static void ApplyRoofingWithExclusion(Map map, CellRect roomRect, CellRect exclusionRect, RoofDef roofDef = null)
        {
            roofDef = roofDef ?? Roofs.RoofConstructed;

            // If no exclusion rect, roof everything
            if (exclusionRect.Width == 0)
            {
                ApplyFullRoofing(map, roomRect, roofDef);
                return;
            }

            // Roof all cells EXCEPT those inside the exclusion rect
            foreach (IntVec3 cell in roomRect)
            {
                if (!cell.InBounds(map))
                    continue;

                if (!exclusionRect.Contains(cell))
                {
                    map.roofGrid.SetRoof(cell, roofDef);
                }
                // Cells inside exclusionRect are left unroofed (open to sky)
            }
        }

        // Applies roofing to an entire room (no exclusions).
        // map: The map to modify.
        // roomRect: The room bounds to roof.
        // roofDef: RoofDef to use (defaults to RoofConstructed).
        public static void ApplyFullRoofing(Map map, CellRect roomRect, RoofDef roofDef = null)
        {
            roofDef = roofDef ?? Roofs.RoofConstructed;

            foreach (IntVec3 cell in roomRect)
            {
                if (cell.InBounds(map))
                {
                    map.roofGrid.SetRoof(cell, roofDef);
                }
            }
        }

        // Removes roofing from a specific area (creates open-sky zone).
        // map: The map to modify.
        // areaRect: The area to unroof.
        public static void RemoveRoofing(Map map, CellRect areaRect)
        {
            foreach (IntVec3 cell in areaRect)
            {
                if (cell.InBounds(map))
                {
                    map.roofGrid.SetRoof(cell, null);
                }
            }
        }
    }
}
