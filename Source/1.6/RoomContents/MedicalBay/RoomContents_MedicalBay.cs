using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.MedicalBay
{
    public class RoomContents_MedicalBay : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            foreach (CellRect roomRect in room.rects)
            {
                RoomEdgeConnector.ConnectBuildingsToConduitNetwork(map, roomRect, Things.Facility_VitalsCentre);
                MedicineShelfFiller.FillMedicineShelves(map, roomRect);
            }
        }
    }
}
