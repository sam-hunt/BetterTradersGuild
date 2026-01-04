using System.Linq;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace BetterTradersGuild.RoomContents.Classroom
{
    /// <summary>
    /// Custom RoomContentsWorker for Classroom (Biotech DLC).
    ///
    /// Spawns textbooks in classroom bookshelves and ensures proper insertion into
    /// bookcase innerContainers for correct rendering and interaction.
    /// </summary>
    public class RoomContents_Classroom : RoomContentsWorker
    {
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // Call base to process XML (prefabs, scatter, parts)
            base.FillRoom(map, room, faction, threatPoints);

            // Post-processing: fix bookcases and spawn plants
            if (room.rects == null || room.rects.Count == 0)
                return;

            CellRect roomRect = room.rects.First();

            // Fix bookcase contents (move textbooks from map into innerContainer)
            RoomBookcaseHelper.InsertBooksIntoBookcases(map, roomRect);

            // Spawn decorative daylilies in corner plant pots
            RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, Things.Plant_Daylily, growth: 1.0f);
        }
    }
}
