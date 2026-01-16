using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized PrefabDef references. ALL prefabs used by BTG go here.
    /// Auto-populated by RimWorld's [DefOf] system at startup.
    ///
    /// This ensures compile-time validation of prefab names - if a prefab def
    /// is missing or misspelled, an error will be logged at game startup.
    ///
    /// For waste filler prefabs specifically, see WasteFillerPrefabSelector which
    /// handles dynamic selection with DLC checking at runtime.
    /// </summary>
    [DefOf]
    public static class Prefabs
    {
        // === CREW BED SUBROOMS (3x4, 3x5, 4x4, 4x5) ===
        public static PrefabDef BTG_CrewBedSubroom3x4;
        public static PrefabDef BTG_CrewBedSubroom3x5;
        public static PrefabDef BTG_CrewBedSubroom4x4;
        public static PrefabDef BTG_CrewBedSubroom4x5;

        // === CREW QUARTERS WASTE FILLERS ===
        // Base variants (always available)
        public static PrefabDef BTG_CrewQuartersIndustrialShelves1x4;
        public static PrefabDef BTG_CrewQuartersPlantPotRow1x4;
        public static PrefabDef BTG_CrewQuartersLockers1x5;
        public static PrefabDef BTG_CrewQuartersTable2x4;
        public static PrefabDef BTG_CrewQuartersCommSupplies2x5;

        // VFE Production variant
        [MayRequire("VanillaExpanded.VFEProduction")]
        public static PrefabDef BTG_CrewQuartersMiniKitchen1x4;

        // VNutrientE variant
        [MayRequire("VanillaExpanded.VNutrientE")]
        public static PrefabDef BTG_CrewQuartersPasteDiner2x5;

        // VRE Androids variants
        [MayRequire("vanillaracesexpanded.android")]
        public static PrefabDef BTG_CrewQuartersAndroidStands1x4;
        [MayRequire("vanillaracesexpanded.android")]
        public static PrefabDef BTG_CrewQuartersAndroidStands1x5;

        // === SHUTTLE BAY ===
        public static PrefabDef BTG_ShuttleLandingPad_Subroom;

        // === CONTROL CENTER ===
        public static PrefabDef BTG_ServerRacks_Subroom;

        // === NURSERY ===
        public static PrefabDef BTG_CribSubroom;

        // === CARGO VAULT ===
        public static PrefabDef BTG_PoweredSniperTurretArray;

        // === SETTLEMENT EXTERIOR ===
        public static PrefabDef BTG_EntranceAutocannons;

        // === OTHER ROOM PREFABS ===
        public static PrefabDef BTG_CommandersBedroom;
        public static PrefabDef BTG_CommandersBookshelf_Edge;
        public static PrefabDef BTG_ArmchairsWithPlantpot_Edge;
        public static PrefabDef BTG_FlatscreenTVRhinoLeather_Edge;
        public static PrefabDef BTG_HospitalBeds_Edge;
        public static PrefabDef BTG_MedicineShelf_Edge;
        public static PrefabDef BTG_BilliardsTable;
        public static PrefabDef BTG_ClassroomBookshelf_Edge;
        public static PrefabDef BTG_HydroponicHealroot;

        static Prefabs() => DefOfHelper.EnsureInitializedInCtor(typeof(Prefabs));
    }
}
