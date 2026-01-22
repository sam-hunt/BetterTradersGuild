using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Workshop
{
    public class RoomContents_Workshop : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            foreach (CellRect roomRect in room.rects)
            {
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, growth: 1.0f);
            }
        }
    }
}
