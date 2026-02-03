using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    /// <summary>
    /// Helper for applying faction colors to spawned apparel when VEF is active.
    ///
    /// When Vanilla Expanded Framework is loaded, it colors pawn apparel with faction colors.
    /// This helper ensures apparel spawned on outfit stands (Armory, Corridor, CrewQuarters)
    /// matches the look of pawn-worn gear.
    /// </summary>
    public static class ApparelFactionColorHelper
    {
        private static bool? _vefActive;

        /// <summary>
        /// Returns true if Vanilla Expanded Framework is loaded.
        /// Cached after first check for performance.
        /// </summary>
        public static bool IsVEFActive => _vefActive ??
            (_vefActive = ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core")).Value;

        /// <summary>
        /// Applies faction color to apparel if VEF is active.
        /// Safe to call even if VEF is not loaded (will no-op).
        /// </summary>
        /// <param name="apparel">The apparel to color.</param>
        /// <param name="faction">The faction whose color to apply.</param>
        public static void TryApplyFactionColor(Apparel apparel, Faction faction)
        {
            if (!IsVEFActive || apparel == null || faction == null)
                return;

            apparel.SetColor(faction.Color);
        }
    }
}
