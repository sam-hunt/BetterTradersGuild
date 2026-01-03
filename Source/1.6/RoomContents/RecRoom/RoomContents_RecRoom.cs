using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents.RecRoom
{
    /// <summary>
    /// Custom RoomContentsWorker for Recreation Room.
    ///
    /// Post-processes the rec room to spawn decorative plants in corner plant pots.
    /// </summary>
    public class RoomContents_RecRoom : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Spawns XML-defined prefabs (tables, seating, etc.),
        /// then spawns decorative plants in corner plant pots.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base FIRST to spawn XML prefabs and parts
            base.FillRoom(map, room, faction, threatPoints);

            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // 2. Spawn decorative daylilies in corner plant pots
            ThingDef daylily = DefDatabase<ThingDef>.GetNamed("Plant_Daylily", false);
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, daylily, growth: 1.0f);
        }
    }
}
