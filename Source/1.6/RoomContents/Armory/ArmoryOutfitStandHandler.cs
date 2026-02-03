using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Armory
{
    /// <summary>
    /// Handles outfit stand setup in the armory:
    /// - Paints stands with BTG_OrbitalSteel color
    /// - Spawns marine armor sets (power armor + helmet)
    /// </summary>
    public static class ArmoryOutfitStandHandler
    {
        /// <summary>
        /// Finds all outfit stands in the room and paints them with BTG_OrbitalSteel color.
        /// </summary>
        public static void PaintOutfitStands(Map map, CellRect roomRect)
        {
            if (Things.Building_OutfitStand == null || Colors.BTG_OrbitalSteel == null)
                return;

            List<Thing> outfitStands = roomRect.Cells
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map))
                .Where(t => t.def == Things.Building_OutfitStand)
                .Distinct()
                .ToList();

            foreach (Thing stand in outfitStands)
                PaintableFurnitureHelper.TryPaint(stand, Colors.BTG_OrbitalSteel);
        }

        /// <summary>
        /// Spawns marine armor and helmet into outfit stands.
        /// </summary>
        /// <param name="map">The map to spawn on.</param>
        /// <param name="roomRect">The room rectangle to search for outfit stands.</param>
        /// <param name="faction">The faction for VEF faction color tinting.</param>
        public static void SpawnMarineArmorInOutfitStands(Map map, CellRect roomRect, Faction faction)
        {
            List<ThingDef> marineArmorSet = new List<ThingDef>();

            if (Things.Apparel_PowerArmor != null)
                marineArmorSet.Add(Things.Apparel_PowerArmor);

            if (Things.Apparel_PowerArmorHelmet != null)
                marineArmorSet.Add(Things.Apparel_PowerArmorHelmet);

            if (marineArmorSet.Count == 0)
                return;

            RoomOutfitStandHelper.SpawnApparelInOutfitStands(
                map,
                roomRect,
                marineArmorSet,
                QualityCategory.Normal,
                QualityCategory.Excellent,
                faction);
        }
    }
}
