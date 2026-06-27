using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that sets the glow color of wall lamps on the map.
    //
    // XML-configurable parameters:
    // - wallLampDef: ThingDef of the wall lamp to modify
    // - glowColor: ColorInt for the glow color (r, g, b)
    //
    // Example usage in GenStepDef:
    // <genStep Class="BetterTradersGuild.MapGeneration.GenStep_SetWallLampColor">
    //   <wallLampDef>WallLamp</wallLampDef>
    //   <glowColor>(187, 187, 221)</glowColor>
    // </genStep>
    //
    // Used to give TradersGuild settlements a consistent blue-tinted lighting
    // aesthetic that matches AncientEmergencyLight_Blue.
    public class GenStep_SetWallLampColor : GenStep
    {
        // ThingDef of the wall lamp to modify. Set via XML.
        public ThingDef wallLampDef;

        // Glow color to apply to lamps. Set via XML.
        // Format: (r, g, b) where each value is 0-255.
        public ColorInt glowColor;

        // Deterministic seed for this GenStep.
        public override int SeedPart => 847291004;

        // Sets the glow color of all wall lamps matching wallLampDef.
        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null || wallLampDef == null)
                return;

            foreach (Thing lamp in map.listerThings.ThingsOfDef(wallLampDef))
            {
                CompGlower glower = lamp.TryGetComp<CompGlower>();
                if (glower != null)
                {
                    glower.GlowColor = glowColor;
                }
            }
        }
    }
}
