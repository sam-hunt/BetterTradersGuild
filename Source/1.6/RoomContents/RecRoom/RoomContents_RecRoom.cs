using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.RecRoom
{
    /// <summary>
    /// Custom RoomContentsWorker for Recreation Room.
    ///
    /// Post-processes the rec room to spawn decorative plants in corner plant pots.
    /// </summary>
    public class RoomContents_RecRoom : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Call base FIRST to spawn XML prefabs and parts
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // Spawn decorative daylilies in corner plant pots
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, growth: 1.0f);
        }
    }
}
