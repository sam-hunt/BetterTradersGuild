using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Helper class for finding and painting furniture during room generation.
    ///
    /// Provides two separate concerns:
    /// 1. GetPaintableFurniture() - Finds all Buildings in a room (furniture is derived from Building)
    /// 2. TryPaint() - Paints a single piece of furniture with a specified color
    ///
    /// USAGE: Call GetPaintableFurniture() AFTER base.FillRoom() completes,
    /// since furniture is spawned by base class and prefabs.
    ///
    /// NOTE: Uses Building.ChangePaint(ColorDef) - the vanilla API for painting buildings/furniture.
    /// </summary>
    public static class PaintableFurnitureHelper
    {
        /// <summary>
        /// Gets all paintable furniture (Buildings) in the specified rect.
        /// Uses Distinct() to handle multi-cell furniture that spans multiple cells.
        ///
        /// Buildings can be painted via Building.ChangePaint(ColorDef).
        /// </summary>
        /// <param name="map">The map containing the furniture</param>
        /// <param name="rect">The rectangular area to search for furniture</param>
        /// <returns>List of Buildings, or empty list if none found</returns>
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

        /// <summary>
        /// Attempts to paint a piece of furniture with the specified ColorDef.
        ///
        /// Uses Building.ChangePaint() which is the vanilla API for painting buildings/furniture.
        /// </summary>
        /// <param name="thing">The thing to paint (must be a Building)</param>
        /// <param name="colorDef">The ColorDef to apply</param>
        /// <returns>True if successfully painted, false if not a Building</returns>
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

        /// <summary>
        /// Attempts to paint a piece of furniture with a raw Color value.
        ///
        /// Uses CompColorable.SetColor() for arbitrary colors (e.g., faction colors).
        /// Unlike ChangePaint(ColorDef), this allows any color, not just predefined ColorDefs.
        /// </summary>
        /// <param name="thing">The thing to paint (must be a Building with CompColorable)</param>
        /// <param name="color">The Color to apply</param>
        /// <returns>True if successfully painted, false if not colorable</returns>
        public static bool TryPaint(Thing thing, Color color)
        {
            if (thing == null)
            {
                return false;
            }

            var comp = thing.TryGetComp<CompColorable>();
            if (comp != null)
            {
                comp.SetColor(color);
                return true;
            }

            return false;
        }
    }
}
