using System.Linq;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.Classroom
{
    public class RoomContents_Classroom : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            foreach (CellRect roomRect in room.rects)
            {
                RoomBookcaseHelper.InsertBooksIntoBookcases(map, roomRect);
            }
        }
    }
}
