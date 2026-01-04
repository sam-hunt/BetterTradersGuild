using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.TradeShowcase
{
    /// <summary>
    /// Custom RoomContentsWorker for Trade Showcase.
    ///
    /// Places spacer crate showcase prefabs with plant pots, then plants
    /// roses in the pots.
    /// </summary>
    public class RoomContents_TradeShowcase : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Call base to process XML (prefabs, scatter, parts)
            base.FillRoom(map, room, faction, threatPoints);

            // Plant roses in all plant pots (spawned by prefabs above)
            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Rose, growth: 1.0f);
        }
    }
}
