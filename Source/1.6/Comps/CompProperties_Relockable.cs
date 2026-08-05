using Verse;

namespace BetterTradersGuild.Comps
{
    // CompProperties for the relockable cargo vault hatch.
    // Defines the UI elements for the relock gizmo.
    public class CompProperties_Relockable : CompProperties
    {
        // Texture path for the relock button icon.
        public string relockTexPath;

        // Label text for the relock button.
        public string relockCommandLabel;

        // Description text for the relock button tooltip.
        public string relockCommandDesc;

        public CompProperties_Relockable()
        {
            compClass = typeof(CompRelockable);
        }
    }
}
