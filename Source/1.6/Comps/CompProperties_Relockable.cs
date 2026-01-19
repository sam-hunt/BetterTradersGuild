using Verse;

namespace BetterTradersGuild.Comps
{
    /// <summary>
    /// CompProperties for the relockable cargo vault hatch.
    /// Defines the UI elements for the relock gizmo.
    /// </summary>
    public class CompProperties_Relockable : CompProperties
    {
        /// <summary>
        /// Texture path for the relock button icon.
        /// </summary>
        public string relockTexPath;

        /// <summary>
        /// Label text for the relock button.
        /// </summary>
        public string relockCommandLabel;

        /// <summary>
        /// Description text for the relock button tooltip.
        /// </summary>
        public string relockCommandDesc;

        public CompProperties_Relockable()
        {
            compClass = typeof(CompRelockable);
        }
    }
}
