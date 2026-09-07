using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with Humanoid Alien Races (HAR)'s AlienRace.Comp_OutfitStandHAR.
    // BTG reflects into it to fix HAR's juvenile body-type roll on human-race outfit stands
    // (OutfitStandHarFixer). The former crash-suppression Finalizer on PostSpawnSetup is gone:
    // HAR fixed the basicMemberKind NRE upstream (AlienRaces ae8412c, 2026-09-07) and BTG's
    // Faction_BasicMemberKind.xml already covered TradersGuild for older HAR builds.
    //
    // Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent
    // when HAR isn't installed; a single Log.Warning when HAR IS present but its
    // API has shifted, so only affected users see it.
    public static class HARIntegration
    {
        private const string CompTypeName = "AlienRace.Comp_OutfitStandHAR";
        private const string BodyTypePropName = "BodyType";

        // The HAR comp type, or null if HAR isn't loaded.
        public static readonly Type CompType;

        // HAR's BodyType property (validated to be a BodyTypeDef).
        public static readonly PropertyInfo BodyTypeProperty;

        // True only when HAR is loaded AND every reflected member resolved.
        public static bool Available => CompType != null && BodyTypeProperty != null;

        static HARIntegration()
        {
            try
            {
                CompType = GenTypes.GetTypeInAnyAssembly(CompTypeName);
                if (CompType == null)
                    return; // HAR not installed — stay silent.

                BodyTypeProperty = CompType.GetProperty(BodyTypePropName,
                    BindingFlags.Public | BindingFlags.Instance);
                // Validate the property type so a later SetValue(BodyTypeDef) can't throw.
                if (BodyTypeProperty != null && !typeof(BodyTypeDef).IsAssignableFrom(BodyTypeProperty.PropertyType))
                    BodyTypeProperty = null;
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] HAR outfit-stand reflection failed (generated outfit "
                    + "stands may reject adult apparel): " + ex);
                return;
            }

            // HAR is present (type resolved) but a member drifted — warn the affected user only.
            if (CompType != null && !Available)
            {
                Log.Warning("[Better Traders Guild] Humanoid Alien Races active but its outfit-stand API ("
                    + CompTypeName + "." + BodyTypePropName + ") could not be resolved; generated outfit "
                    + "stands may reject adult apparel. HAR API may have changed.");
            }
        }
    }
}
