using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep that sets the glow color of wall lamps on the map.
    ///
    /// XML-configurable parameters:
    /// - wallLampDef: ThingDef of the wall lamp to modify
    /// - glowColor: ColorInt for the glow color (r, g, b)
    ///
    /// Example usage in GenStepDef:
    /// <![CDATA[
    /// <genStep Class="BetterTradersGuild.MapGeneration.GenStep_BTGSetWallLampColor">
    ///   <wallLampDef>WallLamp</wallLampDef>
    ///   <glowColor>(187, 187, 221)</glowColor>
    /// </genStep>
    /// ]]>
    ///
    /// Used to give TradersGuild settlements a consistent blue-tinted lighting
    /// aesthetic that matches AncientEmergencyLight_Blue.
    /// </summary>
    public class GenStep_BTGSetWallLampColor : GenStep
    {
        /// <summary>
        /// ThingDef of the wall lamp to modify. Set via XML.
        /// </summary>
        public ThingDef wallLampDef;

        /// <summary>
        /// Glow color to apply to lamps. Set via XML.
        /// Format: (r, g, b) where each value is 0-255.
        /// </summary>
        public ColorInt glowColor;

        /// <summary>
        /// Deterministic seed for this GenStep.
        /// </summary>
        public override int SeedPart => 847291004;

        /// <summary>
        /// Sets the glow color of all wall lamps matching wallLampDef.
        /// </summary>
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
