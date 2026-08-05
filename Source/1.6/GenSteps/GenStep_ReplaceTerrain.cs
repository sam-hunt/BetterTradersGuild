using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that replaces all instances of one terrain type with another.
    //
    // XML-configurable parameters:
    // - oldTerrain: TerrainDef to replace
    // - newTerrain: TerrainDef to replace with
    //
    // Example usage in GenStepDef:
    // <genStep Class="BetterTradersGuild.MapGeneration.GenStep_ReplaceTerrain">
    //   <oldTerrain>AncientTile</oldTerrain>
    //   <newTerrain>MetalTile</newTerrain>
    // </genStep>
    //
    // PERFORMANCE: Full map iteration on 200x200 orbital platform (~40k cells)
    // takes less than 1ms due to cache locality and simple code path.
    public class GenStep_ReplaceTerrain : GenStep
    {
        // Terrain type to replace. Set via XML.
        public TerrainDef oldTerrain;

        // Terrain type to replace with. Set via XML.
        public TerrainDef newTerrain;

        // Deterministic seed for this GenStep.
        public override int SeedPart => 847291001;

        // Replaces all instances of oldTerrain with newTerrain across the entire map.
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null || oldTerrain == null || newTerrain == null)
                return;

            foreach (IntVec3 cell in map.AllCells)
            {
                if (map.terrainGrid.TerrainAt(cell) == oldTerrain)
                    map.terrainGrid.SetTerrain(cell, newTerrain);
            }
        }
    }
}
