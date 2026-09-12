using BetterTradersGuild.Integrations;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Helper for applying faction colors to spawned apparel when VEF is active.
    //
    // When Vanilla Expanded Framework is loaded, it colors pawn apparel with faction colors.
    // This helper ensures apparel spawned on outfit stands (Armory, Corridor, CrewQuarters)
    // matches the look of pawn-worn gear.
    public static class ApparelFactionColorHelper
    {
        private static bool? _vefActive;

        // Returns true if Vanilla Expanded Framework is loaded.
        // Cached after first check for performance. Postfix-tolerant (ModDetection):
        // the plain ModsConfig.IsActive check this used until 2026-09-12 missed every
        // Steam Workshop install of VEF, so stand apparel stayed uncoloured for them.
        public static bool IsVEFActive => _vefActive ??
            (_vefActive = ModDetection.IsActive("OskarPotocki.VanillaFactionsExpanded.Core")).Value;

        // Applies faction color to apparel if VEF is active.
        // Safe to call even if VEF is not loaded (will no-op).
        // apparel: The apparel to color.
        // faction: The faction whose color to apply.
        public static void TryApplyFactionColor(Apparel apparel, Faction faction)
        {
            if (!IsVEFActive || apparel == null || faction == null)
                return;

            apparel.SetColor(faction.Color);
        }
    }
}
