using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.ScenParts
{
    /// <summary>
    /// ScenPart that recolors starting pawns' shell and overhead apparel to a
    /// specific color. Runs at pawn generation time so the color is visible on
    /// the pawn selection/preview screen.
    /// </summary>
    public class ScenPart_StartingApparelColor : ScenPart
    {
        public Color color;

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            if (context != PawnGenerationContext.PlayerStarter)
                return;

            if (pawn.apparel == null)
                return;

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                ApparelLayerDef lastLayer = apparel.def.apparel?.LastLayer;
                if (lastLayer == ApparelLayerDefOf.Shell ||
                    lastLayer == ApparelLayerDefOf.Overhead)
                {
                    apparel.SetColor(color);
                }
            }
        }
    }
}
