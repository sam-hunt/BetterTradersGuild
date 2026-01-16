using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized LayoutRoomDef references for BTG custom rooms.
    /// Auto-populated by RimWorld's [DefOf] system at startup.
    /// </summary>
    [DefOf]
    public static class LayoutRooms
    {
        // === BTG SETTLEMENT ROOMS ===

        /// <summary>
        /// Shuttle bay room - large room containing landing pad subroom and cargo vault hatch.
        /// Requires minimum 18x15 (ideal) or 17x14 (relaxed, with 1-2 doors only).
        /// </summary>
        public static LayoutRoomDef BTG_ShuttleBay;

        // === BTG CARGO VAULT ROOMS ===

        /// <summary>
        /// Cargo vault room - secure storage room within BTG_OrbitalCargoVault.
        /// Contains exit portal, shelves, and turrets.
        /// </summary>
        public static LayoutRoomDef BTG_CargoVaultRoom;
    }
}
