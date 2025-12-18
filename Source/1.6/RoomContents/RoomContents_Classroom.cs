using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using BetterTradersGuild.Helpers.RoomContents;

namespace BetterTradersGuild.RoomContents
{
    /// <summary>
    /// Custom RoomContentsWorker for Classroom (Biotech DLC).
    ///
    /// Spawns textbooks in classroom bookshelves and ensures proper insertion into
    /// bookcase innerContainers for correct rendering and interaction.
    ///
    /// LEARNING NOTE: This demonstrates the reusable helper pattern. The RoomBookcaseHelper
    /// provides all the bookcase-fixing logic, so custom RoomContentsWorkers can focus on
    /// their unique generation features while getting standardized bookcase handling.
    /// </summary>
    public class RoomContents_Classroom : RoomContentsWorker
    {
        /// <summary>
        /// Main room generation method. Calls base to process XML-defined content
        /// (prefabs, scatter, parts), then fixes bookcase contents using the helper.
        /// </summary>
        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Call base to process XML (prefabs, scatter, parts)
            //    This spawns the classroom desks, blackboards, bookshelves, etc.
            base.FillRoom(map, room, faction, threatPoints);

            // 2. Post-processing: fix bookcases and spawn plants
            //    CRITICAL: This must happen AFTER base.FillRoom() since the bookshelves
            //    and plant pots are spawned by base.FillRoom() via XML prefabs and parts
            if (room.rects != null && room.rects.Count > 0)
            {
                CellRect roomRect = room.rects.First();

                // Fix bookcase contents (move textbooks from map into innerContainer)
                RoomBookcaseHelper.InsertBooksIntoBookcases(map, roomRect);

                // Spawn decorative daylilies in corner plant pots
                ThingDef daylily = DefDatabase<ThingDef>.GetNamed("Plant_Daylily", false);
                RoomPlantHelper.SpawnPlantsInPlantPots(map, roomRect, daylily, growth: 1.0f);
            }
        }
    }
}
