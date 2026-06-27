using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    // Centralized LayoutRoomDef references for BTG custom rooms.
    // Auto-populated by RimWorld's [DefOf] system at startup.
    [DefOf]
    public static class LayoutRooms
    {
        // === BTG SETTLEMENT ROOMS ===

        // Shuttle bay room - large room containing landing pad subroom and cargo vault hatch.
        // Requires minimum 18x15 (ideal) or 17x14 (relaxed, with 1-2 doors only).
        public static LayoutRoomDef BTG_ShuttleBay;

        // Open-air cargo-pod launch bay. A defender resupply-drop target (open sky).
        public static LayoutRoomDef BTG_PodLaunchBay;

        // Dining room. A roofed defender resupply-drop fallback (pod punches the roof).
        public static LayoutRoomDef BTG_MessHall;

        // Comms/command center. A roofed defender resupply-drop fallback (pod punches the roof).
        public static LayoutRoomDef BTG_ControlCenter;

        // Medical bay. Confines the paramedic mech's tend/rescue/medicine search
        // (resolved via StructureRoomLocator in MedicRoomBounds) to this one room.
        public static LayoutRoomDef BTG_MedicalBay;

        // === BTG CARGO VAULT ROOMS ===

        // Cargo vault room - secure storage room within BTG_OrbitalCargoVault.
        // Contains exit portal, shelves, and turrets.
        public static LayoutRoomDef BTG_CargoVaultRoom;
    }
}
