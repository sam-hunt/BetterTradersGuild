using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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

        // Finds the paintable structure ColorDef nearest to the given color (squared RGB
        // distance). Lets generated furniture match a faction's color even when no ColorDef
        // exists for it exactly - e.g. Salvagers (0.75, 0.5, 0.5) resolves to the vanilla
        // Structure_RedPastel, while TradersGuild resolves to its exact match BTG_Rust.
        // Returns null if no Structure ColorDefs are loaded.
        public static ColorDef NearestStructureColor(Color color)
        {
            ColorDef best = null;
            float bestDistance = float.MaxValue;

            foreach (ColorDef def in DefDatabase<ColorDef>.AllDefsListForReading)
            {
                if (def.colorType != ColorType.Structure)
                    continue;

                Color c = def.color;
                float distance = ((c.r - color.r) * (c.r - color.r))
                    + ((c.g - color.g) * (c.g - color.g))
                    + ((c.b - color.b) * (c.b - color.b));

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = def;
                }
            }

            return best;
        }
    }
}
