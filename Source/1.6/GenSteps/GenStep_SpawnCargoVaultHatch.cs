using BetterTradersGuild.Helpers.MapGeneration;
using BetterTradersGuild.RoomContents.ShuttleBay;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that places the cargo vault hatch on a station whose rooms BTG did not lay
    // out (VGE2's Traders Guild station), where no ShuttleBay room worker exists to place
    // it. The hatch is the trade-to-cargo link: SettlementStockCache and the vault's
    // stock lookup (CargoVaultHelper.GetStock) navigate pocket map -> parent -> source
    // map -> Settlement, so nothing about the vault depends on which generator built the
    // surface map.
    //
    // Placement: StationHatchPlacer picks the clear 3x3 spot with a one-cell clear ring,
    // roofed, on heavy-affordance floor, nearest the station centre. A cell is clear when
    // it holds no edifice, item or pawn; under-floor conduits are allowed. No spot at
    // all logs a warning and the map simply has no vault (the trade dialog is unaffected).
    //
    // Runs before the pawn step (order 700) so no defender spawns under the hatch, and
    // after the platform step (200) whose SpawnRect bounds the search.
    //
    // The hackable-vs-sealed choice (enableCargoVault, quest override) lives in
    // CargoVaultHatchSpawner and is shared with the ShuttleBay path.
    public class GenStep_SpawnCargoVaultHatch : GenStep
    {
        private const int Clearance = 1;

        public override int SeedPart => 1450912667;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.TryGetVar("SpawnRect", out CellRect spawnRect))
            {
                Log.Error("[Better Traders Guild] GenStep_SpawnCargoVaultHatch tried to execute but no SpawnRect was found in the map generator. This CellRect must be set.");
                return;
            }

            CellRect search = spawnRect.ClipInsideMap(map);
            IntVec3 center = StationHatchPlacer.FindBestCenter(
                search, search.CenterCell, CargoVaultHatchSpawner.HATCH_SIZE, Clearance,
                cell => IsClearFloor(map, cell));

            if (!center.IsValid)
            {
                Log.Warning("[Better Traders Guild] GenStep_SpawnCargoVaultHatch found no clear 5x5 roofed floor inside the station; this settlement has no cargo vault hatch.");
                return;
            }

            CargoVaultHatchSpawner.SpawnHatchAt(map, center);
        }

        // Roofed, standable, heavy-affordance floor holding nothing but under-floor
        // transmitters (conduits) or filth.
        private static bool IsClearFloor(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map) || !cell.Roofed(map) || !cell.Standable(map))
                return false;

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain?.affordances == null || !terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy))
                return false;

            foreach (Thing thing in cell.GetThingList(map))
            {
                switch (thing.def.category)
                {
                    case ThingCategory.Pawn:
                    case ThingCategory.Item:
                        return false;
                    case ThingCategory.Building:
                        // Conduits sit under the floor and coexist with anything; every
                        // other building (edifice or not) is an obstacle or a fixture.
                        if (thing.def.building?.isEdifice != false || !thing.def.EverTransmitsPower)
                            return false;
                        break;
                }
            }
            return true;
        }
    }
}
