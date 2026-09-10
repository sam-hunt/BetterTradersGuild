using BetterTradersGuild.Helpers.MapGeneration;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that lays a HiddenConduit under every wall and door cell inside the
    // structure's SpawnRect, unifying the map's power sources (LifeSupportUnits, VGE2
    // power sockets) into one grid so every powered fixture within connection range of
    // a wall (hydroponics basins, lamps, ...) is live.
    //
    // BTG's own layouts get this from LayoutConduitPlacer during structure generation.
    // Prefab-built stations (VGE2's Traders Guild station) have no LayoutStructureSketch
    // and ship no conduits, so this step supplies the same wiring after the platform
    // step from the rect it published. Conduits only: the VE hidden pipes BTG's layout
    // path also lays have no consumer on a prefab station.
    //
    // Only power fixtures change state. VGE2's defence emplacements carry no power comp,
    // so this does not alter the encounter's balance.
    public class GenStep_PlaceWallConduits : GenStep
    {
        public override int SeedPart => 604117253;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar("SpawnRect", out CellRect spawnRect))
            {
                Log.Error("[Better Traders Guild] GenStep_PlaceWallConduits tried to execute but no SpawnRect was found in the map generator. This CellRect must be set.");
                return;
            }

            LayoutConduitPlacer.PlaceHiddenConduits(map, spawnRect.ClipInsideMap(map).Cells, includeHiddenPipes: false);
        }
    }
}
