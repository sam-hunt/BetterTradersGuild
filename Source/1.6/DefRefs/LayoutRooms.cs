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
        // === BTG CUSTOM ROOMS ===

        /// <summary>
        /// Cargo hold vault room - secure storage room within BTG_CargoHoldVaultLayout.
        /// Contains exit portal, shelves, and turrets.
        /// </summary>
        public static LayoutRoomDef BTG_CargoHoldVaultRoom;
    }
}
