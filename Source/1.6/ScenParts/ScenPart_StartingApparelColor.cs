using System.Collections.Generic;
using System.Linq;
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

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect rect = listing.GetScenPartRect(this, RowHeight);
            string label = ColorDefForCurrentColor()?.LabelCap.ToString() ?? "Custom color";
            if (Widgets.ButtonText(rect, label))
            {
                FloatMenuUtility.MakeMenu(
                    DefDatabase<ColorDef>.AllDefs,
                    cd => cd.LabelCap,
                    cd => delegate { color = cd.color; }
                );
            }
        }

        public override void Randomize()
        {
            ColorDef randomColor = DefDatabase<ColorDef>.AllDefs.RandomElementWithFallback();
            if (randomColor != null)
                color = randomColor.color;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref color, "color");
        }

        private ColorDef ColorDefForCurrentColor()
        {
            return DefDatabase<ColorDef>.AllDefs
                .FirstOrDefault(cd => cd.color == color);
        }

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
