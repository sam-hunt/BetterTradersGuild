using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep that replaces all instances of one terrain type with another.
    ///
    /// XML-configurable parameters:
    /// - oldTerrain: TerrainDef to replace
    /// - newTerrain: TerrainDef to replace with
    ///
    /// Example usage in GenStepDef:
    /// <![CDATA[
    /// <genStep Class="BetterTradersGuild.MapGeneration.GenStep_BTGReplaceTerrain">
    ///   <oldTerrain>AncientTile</oldTerrain>
    ///   <newTerrain>MetalTile</newTerrain>
    /// </genStep>
    /// ]]>
    ///
    /// PERFORMANCE: Full map iteration on 200x200 orbital platform (~40k cells)
    /// takes less than 1ms due to cache locality and simple code path.
    /// </summary>
    public class GenStep_BTGReplaceTerrain : GenStep
    {
        /// <summary>
        /// Terrain type to replace. Set via XML.
        /// </summary>
        public TerrainDef oldTerrain;

        /// <summary>
        /// Terrain type to replace with. Set via XML.
        /// </summary>
        public TerrainDef newTerrain;

        /// <summary>
        /// Deterministic seed for this GenStep.
        /// </summary>
        public override int SeedPart => 847291001;

        /// <summary>
        /// Replaces all instances of oldTerrain with newTerrain across the entire map.
        /// </summary>
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
