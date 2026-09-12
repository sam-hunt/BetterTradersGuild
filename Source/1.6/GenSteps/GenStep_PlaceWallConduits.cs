using BetterTradersGuild.Helpers.MapGeneration;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that lays a HiddenConduit under every wall and door cell inside the
    // structure's SpawnRect, unifying the map's power sources (LifeSupportUnits, VGE2
    // power sockets) into one grid so every powered fixture within connection range of
    // a wall (hydroponics basins, lamps, ...) is live. With includeHiddenPipes it also
    // lays the supported VE hidden pipes (HiddenPipeHelper) at the same cells, forming
    // the fluid network the tank/valve prefabs and the landing-pad pipe extension join.
    //
    // Shared by every BTG pipeline. It runs after the platform step, so all
    // RoomContentsWorkers have already filled their rooms: they wire interior fixtures
    // toward the room-rect edge (RoomEdgeConnector) and rely on this step to make the
    // wall cell itself live. Walking the whole SpawnRect (not the sketch's room edges)
    // also wires interior partitions the sketch never listed (vanilla NarrowHalls,
    // ShuttleBay required walls, subroom prefab walls), and works on prefab-built
    // stations (VGE2's Traders Guild station) that have no LayoutStructureSketch.
    //
    // Ordering: must precede BTG_ExtendLandingPadPipes (260), whose path search uses
    // the first existing hidden pipe as its goal.
    //
    // Only power fixtures change state. VGE2's defence emplacements carry no power comp,
    // so this does not alter the encounter's balance.
    public class GenStep_PlaceWallConduits : GenStep
    {
        // XML: also lay the VE hidden pipes. False on VGE2's station, which ships no
        // tanks or valves for them to feed.
        public bool includeHiddenPipes;

        public override int SeedPart => 604117253;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar("SpawnRect", out CellRect spawnRect))
            {
                Log.Error("[Better Traders Guild] GenStep_PlaceWallConduits tried to execute but no SpawnRect was found in the map generator. This CellRect must be set.");
                return;
            }

            LayoutConduitPlacer.PlaceHiddenConduits(map, spawnRect.ClipInsideMap(map).Cells, includeHiddenPipes);
        }
    }
}
