using System;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with "Vanilla Gravship Expanded - Chapter 2" (VGE2), which ships
    // its own Traders Guild station: an unconditional XML replace of vanilla's
    // SettlementPlatform GenStepDef's genStep with VanillaGravshipExpanded2.GenStep_OrbitalPlatform_Custom
    // (a VEF StructureSet of 35x35 prefabs, no LayoutDef, no room workers).
    //
    // Strategy (Docs/VGE2_INTEGRATION.md section 3): when that station is live, BTG does not
    // fight it. The generator swap (MapParentMapGeneratorDef) routes Traders Guild
    // settlements to BTG_SettlementMapGenerator_VGE2, whose platform step IS vanilla's
    // (VGE2-replaced) SettlementPlatform def and whose remaining steps layer BTG's
    // additions on the prefab station: wall conduits, the cargo vault hatch, sown
    // hydroponics, stocked outfit stands. Garrison and defeat stay vanilla for now.
    //
    // Available requires the replace to actually be in effect, not merely the mod to be
    // active: if VGE2 ever drops or renames that patch there is no station to layer on, and
    // BTG's own generator is the right fallback. Consumed through a def instance, so like
    // UMWIntegration this resolves in an idempotent Resolve() invoked once per play-data
    // load from ReflectionVerification.VerifyAll (via BTGStartup.Run), never a static ctor.
    // Warns (once per load) only when VGE2 is active but the station patch is missing.
    public static class VGE2Integration
    {
        public const string PackageId = "vanillaexpanded.gravship2";

        private const string StationGenStepTypeName = "VanillaGravshipExpanded2.GenStep_OrbitalPlatform_Custom";
        private const string VanillaPlatformGenStepDefName = "SettlementPlatform";

        // True when the VGE2 mod is in the active mod list.
        public static bool ModActive { get; private set; }

        // True when VGE2's Traders Guild station replace is in effect on vanilla's
        // SettlementPlatform GenStepDef.
        public static bool StationPatchActive { get; private set; }

        // True only when VGE2 is active AND its station is what vanilla's settlement
        // platform step now generates.
        public static bool Available => ModActive && StationPatchActive;

        // Resolves (or re-resolves) against the current DefDatabase. Idempotent.
        public static void Resolve()
        {
            StationPatchActive = false;
            try
            {
                ModActive = ModsConfig.IsActive(PackageId);
                if (!ModActive)
                    return; // VGE2 not active - stay silent.

                GenStepDef platformStep = DefDatabase<GenStepDef>.GetNamedSilentFail(VanillaPlatformGenStepDefName);
                Type stepType = platformStep?.genStep?.GetType();
                StationPatchActive = stepType != null && stepType.FullName == StationGenStepTypeName;
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] 'Vanilla Gravship Expanded - Chapter 2' detection failed "
                    + "(Traders Guild settlements keep BTG's own map generator): " + ex);
                return;
            }

            if (!StationPatchActive)
            {
                Log.Warning("[Better Traders Guild] 'Vanilla Gravship Expanded - Chapter 2' is active but its "
                    + "Traders Guild station patch is not in effect on the SettlementPlatform GenStepDef "
                    + "(expected " + StationGenStepTypeName + "). Traders Guild settlements keep BTG's own "
                    + "map generator. VGE2 may have changed.");
            }
        }
    }
}
