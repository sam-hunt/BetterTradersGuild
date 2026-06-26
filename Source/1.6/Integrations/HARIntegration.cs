using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Integrations
{
    /// <summary>
    /// Optional integration with Humanoid Alien Races (HAR)'s <c>AlienRace.Comp_OutfitStandHAR</c>.
    /// BTG reflects into it to (a) suppress a NullReferenceException HAR throws during settlement
    /// generation and (b) fix HAR's juvenile body-type selection on factionless outfit stands.
    ///
    /// <para>Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent
    /// when HAR isn't installed; a single <see cref="Log.Warning"/> when HAR IS present but its
    /// API has shifted, so only affected users see it. Consumers reference the individual members
    /// they need (the fixer needs <see cref="CompType"/> + <see cref="BodyTypeProperty"/>; the
    /// crash-suppression patch needs <see cref="CompType"/> + <see cref="PostSpawnSetupMethod"/>).</para>
    /// </summary>
    public static class HARIntegration
    {
        private const string CompTypeName = "AlienRace.Comp_OutfitStandHAR";
        private const string BodyTypePropName = "BodyType";
        private const string PostSpawnSetupMethodName = "PostSpawnSetup";

        /// <summary>The HAR comp type, or null if HAR isn't loaded.</summary>
        public static readonly Type CompType;

        /// <summary>HAR's <c>BodyType</c> property (validated to be a <see cref="BodyTypeDef"/>).</summary>
        public static readonly PropertyInfo BodyTypeProperty;

        /// <summary>HAR's <c>PostSpawnSetup(bool)</c> override (Harmony target for crash suppression).</summary>
        public static readonly MethodInfo PostSpawnSetupMethod;

        /// <summary>True only when HAR is loaded AND every reflected member resolved.</summary>
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
