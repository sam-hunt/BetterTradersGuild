using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    // Places hidden conduits and VE pipes under walls after map generation.
    //
    // PURPOSE:
    // Creates a station-wide power and resource network by placing hidden infrastructure
    // under all walls and doors of the generated structure. This ensures all electrical
    // devices are connected to power sources (LifeSupportUnits) and VE pipe networks are
    // unified.
    //
    // ORDERING:
    // The sole caller is GenStep_PlaceWallConduits, which runs after the platform step,
    // i.e. after every RoomContentsWorker has filled its room. Room workers therefore
    // never see these conduits; they wire interior fixtures toward the room-rect edge
    // (RoomEdgeConnector) and rely on this pass to make the wall cell itself live.
    public static class LayoutConduitPlacer
    {
        // Places a HiddenConduit under every wall and door cell among the candidate
        // cells (other cells are skipped, so a whole rect can be passed). Optionally also
        // spawns the VE hidden pipes. Each def is checked separately, so a prefab that
        // already embeds a HiddenConduit (subroom blast doors) still gets its pipes.
        // Returns: Number of conduit positions placed
        public static int PlaceHiddenConduits(Map map, IEnumerable<IntVec3> candidateCells, bool includeHiddenPipes)
        {
            IReadOnlyList<ThingDef> hiddenPipeDefs = includeHiddenPipes
                ? HiddenPipeHelper.GetSupportedHiddenPipeDefs()
                : System.Array.Empty<ThingDef>();

            int placedCount = 0;

            // Candidate cells may repeat (rooms share walls)
            HashSet<IntVec3> processedCells = new HashSet<IntVec3>();

            foreach (IntVec3 cell in candidateCells)
            {
                if (!processedCells.Add(cell))
                    continue;

                // Bounds check (defensive)
                if (!cell.InBounds(map))
                    continue;

                // Check what's at this cell
                Building edifice = cell.GetEdifice(map);
                if (edifice == null)
                    continue;

                // Only place conduits under walls and doors
                if (!edifice.def.IsDoor && edifice.def.building?.isPlaceOverableWall != true)
                    continue;

                if (!HasThingOfDef(map, cell, Things.HiddenConduit))
                {
                    Thing conduit = ThingMaker.MakeThing(Things.HiddenConduit);
                    GenSpawn.Spawn(conduit, cell, map);
                    placedCount++;
                }

                // Also spawn any VE hidden pipes at this location
                foreach (ThingDef hiddenPipeDef in hiddenPipeDefs)
                {
                    if (HasThingOfDef(map, cell, hiddenPipeDef))
                        continue;

                    Thing pipe = ThingMaker.MakeThing(hiddenPipeDef);
                    GenSpawn.Spawn(pipe, cell, map);
                }
            }

            return placedCount;
        }

        private static bool HasThingOfDef(Map map, IntVec3 cell, ThingDef def)
        {
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing.def == def)
                    return true;
            }
            return false;
        }
    }
}
