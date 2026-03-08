using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Armory
{
    /// <summary>
    /// Handles outfit stand setup in the armory:
    /// - Spawns marine armor sets (power armor + helmet)
    /// </summary>
    public static class ArmoryOutfitStandHandler
    {
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
