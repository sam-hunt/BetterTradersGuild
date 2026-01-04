using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.RoomContents.Nursery
{
    /// <summary>
    /// Paints nursery furniture with matching pastel colors.
    /// Colors match the checkered floor pattern for a cohesive nursery look.
    /// </summary>
    public static class RoomFurniturePastelPainter
    {
        /// <summary>
        /// Paints furniture in the room with pastel colors (pink, blue pastel, green pastel).
        /// Excludes walls and blast doors from painting.
        /// </summary>
        /// <param name="map">The map containing the room</param>
        /// <param name="roomRect">The room rectangle (will be contracted by 1 to exclude walls)</param>
        public static void PaintFurniture(Map map, CellRect roomRect)
        {
            List<ColorDef> pastelColors = new List<ColorDef>
            {
                Colors.Structure_Pink,
                Colors.Structure_BluePastel,
                Colors.Structure_GreenPastel
            }.Where(c => c != null).ToList();

            if (pastelColors.Count == 0)
                return;

            // Contract rect by 1 to exclude outer room walls
            CellRect interiorRect = roomRect.ContractedBy(1);
            List<Building> paintable = PaintableFurnitureHelper.GetPaintableFurniture(map, interiorRect)
                .Where(b => b.def != Things.OrbitalAncientFortifiedWall &&
                            b.def != Things.AncientBlastDoor)
                .ToList();

            for (int i = 0; i < paintable.Count; i++)
            {
                PaintableFurnitureHelper.TryPaint(paintable[i], pastelColors[i % pastelColors.Count]);
            }
        }
    }
}
