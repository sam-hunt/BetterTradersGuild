using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with Humanoid Alien Races (HAR)'s AlienRace.Comp_OutfitStandHAR.
    // BTG reflects into it to (a) suppress a NullReferenceException HAR throws during settlement
    // generation and (b) fix HAR's juvenile body-type selection on factionless outfit stands.
    //
    // Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent
    // when HAR isn't installed; a single Log.Warning when HAR IS present but its
    // API has shifted, so only affected users see it. Consumers reference the individual members
    // they need (the fixer needs CompType + BodyTypeProperty; the
    // crash-suppression patch needs CompType + PostSpawnSetupMethod).
    public static class HARIntegration
    {
        private const string CompTypeName = "AlienRace.Comp_OutfitStandHAR";
        private const string BodyTypePropName = "BodyType";
        private const string PostSpawnSetupMethodName = "PostSpawnSetup";

        // The HAR comp type, or null if HAR isn't loaded.
        public static readonly Type CompType;

        // HAR's BodyType property (validated to be a BodyTypeDef).
        public static readonly PropertyInfo BodyTypeProperty;

        // HAR's PostSpawnSetup(bool) override (Harmony target for crash suppression).
        public static readonly MethodInfo PostSpawnSetupMethod;

        // True only when HAR is loaded AND every reflected member resolved.
        public static bool Available =>
            CompType != null && BodyTypeProperty != null && PostSpawnSetupMethod != null;

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

                // Resolve the exact (bool) overload so a future HAR overload can't ambiguate it.
                PostSpawnSetupMethod = CompType.GetMethod(PostSpawnSetupMethodName,
                    BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] HAR outfit-stand reflection failed (generated outfit "
                    + "stands may reject adult apparel or log spawn errors): " + ex);
                return;
            }

            // HAR is present (type resolved) but a member drifted — warn the affected user only.
            if (CompType != null && !Available)
            {
                Log.Warning("[Better Traders Guild] Humanoid Alien Races active but its outfit-stand API ("
                    + CompTypeName + "." + BodyTypePropName + " / " + PostSpawnSetupMethodName + "(bool)) "
                    + "could not be resolved; generated outfit stands may reject adult apparel or log spawn "
                    + "errors. HAR API may have changed.");
            }
        }
    }
}
