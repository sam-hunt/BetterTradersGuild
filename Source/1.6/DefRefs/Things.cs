using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized ThingDef references. ALL defs used by BTG go here - vanilla, DLC, mod, and custom.
    /// Auto-populated by RimWorld's [DefOf] system at startup.
    /// Unified syntax: always use Things.* regardless of def source.
    /// </summary>
    [DefOf]
    public static class Things
    {
        // === VANILLA/CORE ===
        public static ThingDef Steel;
        public static ThingDef Gold;
        public static ThingDef Silver;
        public static ThingDef ComponentIndustrial;
        public static ThingDef ComponentSpacer;
        public static ThingDef MedicineIndustrial;
        public static ThingDef MedicineUltratech;
        public static ThingDef Synthread;
        public static ThingDef Beer;
        public static ThingDef HiddenConduit;
        public static ThingDef Heater;
        public static ThingDef WallLamp;
        public static ThingDef AncientBlastDoor;
        public static ThingDef Shelf;
        public static ThingDef ShelfSmall;
        public static ThingDef BookcaseSmall;
        public static ThingDef ChessTable;
        public static ThingDef SculptureSmall;
        public static ThingDef AncientSafe;
        public static ThingDef AncientSealedCrate;
        public static ThingDef MeditationSpot;
        public static ThingDef Novel;
        public static ThingDef Table1x2c;
        public static ThingDef MealSurvivalPack;
        public static ThingDef RawBerries;
        public static ThingDef Chemfuel;
        public static ThingDef ChemfuelTank;
        public static ThingDef RawCorn;
        public static ThingDef Cloth;
        public static ThingDef Building_OutfitStand;
        public static ThingDef GameOfUrBoard;
        public static ThingDef HorseshoesPin;
        public static ThingDef PlantPot;
        public static ThingDef HydroponicsBasin;
        public static ThingDef AnimalBed;
        public static ThingDef Kibble;
        public static ThingDef VacBarrier;
        public static ThingDef Filth_GestationFluid;
        public static ThingDef Filth_Trash;
        public static ThingDef Filth_MoldyUniform;
        public static ThingDef Gun_ChargeRifle;
        public static ThingDef Gun_ChargeLance;
        public static ThingDef Apparel_ShieldBelt;
        public static ThingDef Apparel_Gunlink;
        public static ThingDef Apparel_PowerArmor;
        public static ThingDef Apparel_PowerArmorHelmet;
        public static ThingDef Shell_HighExplosive;
        public static ThingDef Shell_AntigrainWarhead;
        public static ThingDef Leather_Panthera;
        public static ThingDef TransportPod;
        public static ThingDef MalfunctioningTransportPod;
        public static ThingDef SunLamp;
        public static ThingDef Ship_ComputerCore;
        public static ThingDef FirefoamPopper;
        public static ThingDef Facility_VitalsCentre;

        // Turrets
        public static ThingDef AncientSecurityTurret;
        public static ThingDef Turret_MiniTurret;

        // Plants
        public static ThingDef Plant_Rose;
        public static ThingDef Plant_Healroot;
        public static ThingDef Plant_Daylily;
        public static ThingDef Plant_Rice;
        public static ThingDef Plant_Potato;

        // Unique Weapons
        public static ThingDef Gun_ChargeRifle_Unique;
        public static ThingDef Gun_ChargeLance_Unique;
        public static ThingDef Gun_BeamRepeater_Unique;
        public static ThingDef Gun_Revolver_Unique;

        // Apparel
        public static ThingDef Apparel_BasicShirt;
        public static ThingDef Apparel_Pants;
        public static ThingDef Apparel_Vacsuit;
        public static ThingDef Apparel_VacsuitHelmet;
        public static ThingDef Apparel_ArmorRecon;
        public static ThingDef Apparel_ArmorHelmetRecon;

        // === BTG CUSTOM BUILDINGS ===
        public static ThingDef BTG_CargoHoldHatch;
        public static ThingDef BTG_CargoHoldExit;

        // === ODYSSEY DLC (always available - BTG hard-depends on Odyssey) ===
        public static ThingDef HunterDroneTrap;
        public static ThingDef WaspDroneTrap;
        public static ThingDef OrbitalAncientFortifiedWall;
        public static ThingDef AncientShipBeacon;
        public static ThingDef PassengerShuttle;
        public static ThingDef OxygenPump;
        public static ThingDef LifeSupportUnit;

        // === ORCA SHUTTLE MOD ===
        [MayRequire("smallmine.HeavyShuttle")]
        public static ThingDef OrcaShuttle;

        // === ROYALTY DLC ===
        [MayRequireRoyalty]
        public static ThingDef ShipLandingBeacon;

        // === BIOTECH DLC ===
        [MayRequireBiotech]
        public static ThingDef Crib;

        [MayRequireBiotech]
        public static ThingDef BabyFood;

        // === ANOMALY DLC ===
        [MayRequireAnomaly]
        public static ThingDef ScrapCubeSculpture;

        [MayRequireAnomaly]
        public static ThingDef GoldenCube;

        // === IDEOLOGY DLC ===
        [MayRequireIdeology]
        public static ThingDef Apparel_BodyStrap;

        // === VFE SPACER MODULE ===
        [MayRequire("VanillaExpanded.VFESpacerModule")]
        public static ThingDef Table_interactive_1x1c;

        [MayRequire("VanillaExpanded.VFESpacerModule")]
        public static ThingDef Table_interactive_2x2c;

        [MayRequire("VanillaExpanded.VFESpacerModule")]
        public static ThingDef VFES_AirPurifier;

        [MayRequire("VanillaExpanded.VFESpacerModule")]
        public static ThingDef VGE_VacBarrierQuintuple;

        // === VE PIPE SYSTEM (shared by chemfuel/nutrient/etc) ===
        [MayRequire("VanillaExpanded.VChemfuelE")]
        public static ThingDef PS_ChemfuelTank;

        // === VE CHEMFUEL ===
        [MayRequire("VanillaExpanded.VChemfuelE")]
        public static ThingDef VCHE_ChemfuelPipe;

        [MayRequire("VanillaExpanded.VChemfuelE")]
        public static ThingDef VCHE_UndergroundChemfuelPipe;

        [MayRequire("VanillaExpanded.VChemfuelE")]
        public static ThingDef VCHE_ChemfuelValve;

        // === VE NUTRIENT PASTE ===
        [MayRequire("VanillaExpanded.VNutrientE")]
        public static ThingDef VNPE_NutrientPastePipe;

        [MayRequire("VanillaExpanded.VNutrientE")]
        public static ThingDef VNPE_UndergroundNutrientPastePipe;

        [MayRequire("VanillaExpanded.VNutrientE")]
        public static ThingDef VNPE_NutrientPasteValve;

        [MayRequire("VanillaExpanded.VNutrientE")]
        public static ThingDef VNPE_NutrientPasteVat;

        // === VE GRAVSHIPS ===
        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_OxygenPipe;

        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_AstrofuelPipe;

        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_HiddenOxygenPipe;

        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_HiddenAstrofuelPipe;

        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_OxygenValve;

        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_AstrofuelValve;

        [MayRequire("VanillaExpanded.VGravships")]
        public static ThingDef VGE_SmallOxygenTank;

        static Things() => DefOfHelper.EnsureInitializedInCtor(typeof(Things));
    }
}
