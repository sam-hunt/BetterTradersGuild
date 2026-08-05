using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that paints all instances of a terrain type with a custom color.
    //
    // XML-configurable parameters:
    // - terrain: TerrainDef to paint
    // - color: ColorDef to apply
    //
    // Example usage in GenStepDef:
    // <genStep Class="BetterTradersGuild.MapGeneration.GenStep_PaintTerrain">
    //   <terrain>MetalTile</terrain>
    //   <color>BTG_OrbitalSteel</color>
    // </genStep>
    //
    // SAFETY: Checks terrain.isPaintable before applying color.
    // Non-paintable terrain types are skipped with a warning.
    //
    // RENDERING: Calls map.mapDrawer.RegenerateEverythingNow() after painting
    // to flush cached terrain materials and show updated colors.
    public class GenStep_PaintTerrain : GenStep
    {
        // Terrain type to paint. Set via XML.
        public TerrainDef terrain;

        // Color to apply. Set via XML.
        public ColorDef color;

        // Deterministic seed for this GenStep.
        public override int SeedPart => 847291002;

        // Paints all instances of the specified terrain with the specified color.
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null || terrain == null || color == null)
                return;

            // Verify terrain is paintable
            if (!terrain.isPaintable)
            {
                Log.Warning($"[Better Traders Guild] {terrain.defName} terrain is not paintable (isPaintable=false). Cannot apply color.");
                return;
            }

            bool anyPainted = false;

            // Iterate through all cells and paint matching terrain
            foreach (IntVec3 cell in map.AllCells)
            {
                if (map.terrainGrid.TerrainAt(cell) == terrain)
                {
                    map.terrainGrid.SetTerrainColor(cell, color);
                    anyPainted = true;
                }
            }

            if (anyPainted)
            {
                // Regenerate map rendering to show updated colors
                map.mapDrawer.RegenerateEverythingNow();
            }
        }
    }
}
