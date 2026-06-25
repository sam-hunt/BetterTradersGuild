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

        /// <summary>Open-air cargo-pod launch bay. A defender resupply-drop target (open sky).</summary>
        public static LayoutRoomDef BTG_PodLaunchBay;

        /// <summary>Dining room. A roofed defender resupply-drop fallback (pod punches the roof).</summary>
        public static LayoutRoomDef BTG_MessHall;

        /// <summary>Comms/command center. A roofed defender resupply-drop fallback (pod punches the roof).</summary>
        public static LayoutRoomDef BTG_ControlCenter;

        // === BTG CARGO VAULT ROOMS ===

        /// <summary>
        /// Cargo vault room - secure storage room within BTG_OrbitalCargoVault.
        /// Contains exit portal, shelves, and turrets.
        /// </summary>
        public static LayoutRoomDef BTG_CargoVaultRoom;
    }
}
