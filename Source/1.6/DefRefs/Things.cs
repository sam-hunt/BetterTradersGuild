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
        // === VANILLA/CORE/ODYSSEY ===
        public static ThingDef Steel;
        public static ThingDef Gold;
        public static ThingDef Silver;
        public static ThingDef ComponentIndustrial;
        public static ThingDef ComponentSpacer;
        public static ThingDef MedicineIndustrial;
        public static ThingDef MedicineUltratech;
        public static ThingDef Luciferium;
        public static ThingDef GoJuice;
        public static ThingDef Synthread;
        public static ThingDef Beer;
        public static ThingDef Chocolate;
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
        public static ThingDef AncientBox_ComponentIndustrial;
        public static ThingDef AncientBox_ComponentSpacer;
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
        public static ThingDef Filth_Trash;
        public static ThingDef Filth_MoldyUniform;      // TODO: Confirm this is core? ideo?
        public static ThingDef Gun_ChargeRifle;
        public static ThingDef Gun_ChargeLance;
        public static ThingDef Apparel_ShieldBelt;
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
        public static ThingDef Apparel_SmokepopBelt;

        // === BTG CUSTOM BUILDINGS ===
        public static ThingDef BTG_CargoVaultHatch;
        public static ThingDef BTG_CargoVaultHatch_Sealed;
        public static ThingDef BTG_CargoVaultExit;

        // === ODYSSEY DLC (always available - BTG hard-depends on Odyssey) ===
        public static ThingDef HunterDroneTrap;
        public static ThingDef WaspDroneTrap;
        public static ThingDef OrbitalAncientFortifiedWall;
        public static ThingDef AncientShipBeacon;
        public static ThingDef PassengerShuttle;
        public static ThingDef OxygenPump;
        public static ThingDef LifeSupportUnit;
        public static ThingDef AncientPlantPot;

        // === ORCA SHUTTLE MOD ===
        [MayRequire("smallmine.HeavyShuttle")]
        public static ThingDef OrcaShuttle;

        // === ROYALTY DLC ===
        [MayRequireRoyalty]
        public static ThingDef ShipLandingBeacon;
        [MayRequireRoyalty]
        public static ThingDef Apparel_Gunlink;
        [MayRequireRoyalty]
        public static ThingDef MeditationSpot;

        // === BIOTECH DLC ===
        [MayRequireBiotech]
        public static ThingDef Crib;
        [MayRequireBiotech]
        public static ThingDef BabyFood;
        [MayRequireBiotech]
        public static ThingDef Filth_GestationFluid;

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
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_VacBarrierQuintuple;

        // === VFE MEDICAL MODULE ===
        [MayRequire("VanillaExpanded.VFEMedical")]
        public static ThingDef Facility_VitalsCentre;

        // === VE CHEMFUEL ===
        [MayRequire("VanillaExpanded.VChemfuelE")]
        public static ThingDef PS_ChemfuelTank;
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
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_OxygenPipe;
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_AstrofuelPipe;
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_HiddenOxygenPipe;
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_HiddenAstrofuelPipe;
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_OxygenValve;
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_AstrofuelValve;
        [MayRequire("vanillaexpanded.gravship")]
        public static ThingDef VGE_SmallOxygenTank;

        // === VRE GENIE ===
        [MayRequire("vanillaracesexpanded.genie")]
        public static ThingDef VRE_Antibiotics;

        static Things() => DefOfHelper.EnsureInitializedInCtor(typeof(Things));
    }
}
