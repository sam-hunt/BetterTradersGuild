using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Trade Showcase.
    ///
    /// Places spacer crate showcase prefabs with plant pots, then plants
    /// roses in the pots.
    /// </summary>
    public class RoomContents_TradeShowcase : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Calls base to process XML-defined prefabs,
        /// then plants roses in the pots.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base to process XML (prefabs, scatter, parts)
            //    This spawns the spacer crate showcases with plant pots
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Plant roses in all plant pots (spawned by prefabs above)
            //    CRITICAL: Must happen AFTER base.FillRoom() since pots are spawned by prefabs
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();
                ThingDef rosePlant = DefDatabase<ThingDef>.GetNamed("Plant_Rose", false);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, rosePlant, growth: 1.0f);
            }
        }
    }
}
