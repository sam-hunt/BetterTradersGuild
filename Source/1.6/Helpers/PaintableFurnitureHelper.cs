using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers
{
    // Helper class for finding and painting furniture during room generation.
    //
    // Provides two separate concerns:
    // 1. GetPaintableFurniture() - Finds all Buildings in a room (furniture is derived from Building)
    // 2. TryPaint() - Paints a single piece of furniture with a specified color
    //
    // USAGE: Call GetPaintableFurniture() AFTER base.FillRoom() completes,
    // since furniture is spawned by base class and prefabs.
    //
    // NOTE: Uses Building.ChangePaint(ColorDef) - the vanilla API for painting buildings/furniture.
    public static class PaintableFurnitureHelper
    {
        // Gets all paintable furniture (Buildings) in the specified rect.
        // Uses Distinct() to handle multi-cell furniture that spans multiple cells.
        //
        // Buildings can be painted via Building.ChangePaint(ColorDef).
        // map: The map containing the furniture
        // rect: The rectangular area to search for furniture
        // Returns: List of Buildings, or empty list if none found
        public static List<Building> GetPaintableFurniture(Map map, CellRect rect)
        {
            if (map == null)
            {
                return new List<Building>();
            }

            return rect.Cells
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map))
                .OfType<Building>()
                .Distinct()
                .ToList();
        }

        // Attempts to paint a piece of furniture with the specified ColorDef.
        //
        // Uses Building.ChangePaint() which is the vanilla API for painting buildings/furniture.
        // thing: The thing to paint (must be a Building)
        // colorDef: The ColorDef to apply
        // Returns: True if successfully painted, false if not a Building
        public static bool TryPaint(Thing thing, ColorDef colorDef)
        {
            if (thing == null || colorDef == null)
            {
                return false;
            }

            if (thing is Building building)
            {
                building.ChangePaint(colorDef);
                return true;
            }

            return false;
        }
    }
}
