using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Helpers.MapGeneration
{
    /// <summary>
    /// Fills VE Pipes network storage tanks to random levels on generated maps.
    ///
    /// PURPOSE:
    /// After map generation, tanks in VE pipe networks are empty. This helper fills
    /// them to appropriate levels to make the station feel operational:
    /// - Standard tanks: 20-50% (reasonable stockpile)
    /// - Oxygen tanks: 45-65% (life support critical, should be well-stocked)
    ///
    /// TECHNICAL APPROACH:
    /// Uses reflection to access PipeSystem.CompResourceStorage since VE Framework
    /// is an optional dependency. Gracefully handles cases where the mod isn't installed.
    ///
    /// LEARNING NOTE (Optional Mod Integration):
    /// Since VE Framework is not a hard dependency, we can't reference its types directly.
    /// Reflection allows graceful handling when the mod isn't installed - the method
    /// simply returns 0 tanks filled.
    /// </summary>
    public static class PipeNetworkTankFiller
    {
        /// <summary>
        /// Tank defNames to fill during post-generation, with their fill percentage ranges.
        /// Standard tanks: 20-50%, Oxygen tanks: 45-65% (life support critical).
        /// </summary>
        private static readonly Dictionary<string, (float minPct, float maxPct)> TankFillRanges = new Dictionary<string, (float, float)>
        {
            // VE Chemfuel tanks (20-50%)
            { "PS_ChemfuelTank", (0.20f, 0.50f) },
            { "PS_DeepchemTank", (0.20f, 0.50f) },
            // VE Nutrient Paste vat (20-50%)
            { "VNPE_NutrientPasteVat", (0.20f, 0.50f) },
            // VE Gravships oxygen tanks (45-65% - life support critical)
            { "VGE_LargeOxygenTank", (0.45f, 0.65f) },
            // VE Gravships astrofuel tank (20-50%)
            { "VGE_GiantAstrofuelTank", (0.20f, 0.50f) },
        };

        /// <summary>
        /// Cached Type reference for PipeSystem.CompResourceStorage.
        /// Null if VE Framework not installed.
        /// </summary>
        private static Type compResourceStorageType = null;

        /// <summary>
        /// Flag indicating whether CompResourceStorage type lookup has been attempted.
        /// </summary>
        private static bool compResourceStorageTypeInitialized = false;

        /// <summary>
        /// Fills VE pipe network tanks on the map to random levels.
        ///
        /// BEHAVIOR:
        /// - Finds all Things on map matching supported tank defNames
        /// - For each tank, gets CompResourceStorage via reflection
        /// - Calculates random fill amount within configured range
        /// - Calls AddResource to fill the tank
        /// </summary>
        /// <param name="map">The map containing tanks to fill</param>
        /// <returns>Number of tanks filled</returns>
        public static int FillTanksOnMap(Map map)
        {
            // Initialize CompResourceStorage type reference (lazy, once)
            if (!compResourceStorageTypeInitialized)
            {
                compResourceStorageType = GenTypes.GetTypeInAnyAssembly("PipeSystem.CompResourceStorage");
                compResourceStorageTypeInitialized = true;

                if (compResourceStorageType == null)
                {
                    // VE Framework not installed - this is fine
                    return 0;
                }
            }

            if (compResourceStorageType == null)
            {
                return 0;
            }

            int filledCount = 0;

            // Find all things on map that match our tank defNames
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing?.def == null)
                    continue;

                // Check if this is one of our supported tanks
                if (!TankFillRanges.TryGetValue(thing.def.defName, out var fillRange))
                    continue;

                // Must be a ThingWithComps to have comps
                ThingWithComps thingWithComps = thing as ThingWithComps;
                if (thingWithComps == null)
                    continue;

                // Get the CompResourceStorage comp via reflection
                ThingComp storageComp = null;
                foreach (ThingComp comp in thingWithComps.AllComps)
                {
                    if (compResourceStorageType.IsInstanceOfType(comp))
                    {
                        storageComp = comp;
                        break;
                    }
                }

                if (storageComp == null)
                    continue;

                // Get Props.storageCapacity via reflection
                // NOTE: Use DeclaredOnly to avoid AmbiguousMatchException - CompResourceStorage
                // declares its own Props property that hides the base ThingComp.Props
                PropertyInfo propsProperty = compResourceStorageType.GetProperty("Props",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (propsProperty == null)
                {
                    // Fallback: walk up the type hierarchy to find Props
                    Type currentType = compResourceStorageType.BaseType;
                    while (currentType != null && propsProperty == null)
                    {
                        propsProperty = currentType.GetProperty("Props",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        currentType = currentType.BaseType;
                    }
                }
                if (propsProperty == null)
                    continue;

                object props = propsProperty.GetValue(storageComp);
                if (props == null)
                    continue;

                FieldInfo capacityField = props.GetType().GetField("storageCapacity",
                    BindingFlags.Public | BindingFlags.Instance);
                if (capacityField == null)
                    continue;

                float storageCapacity = (float)capacityField.GetValue(props);

                // Calculate random fill amount within the specified range
                float fillPct = Rand.Range(fillRange.minPct, fillRange.maxPct);
                float fillAmount = storageCapacity * fillPct;

                // Call AddResource method via reflection
                MethodInfo addResourceMethod = compResourceStorageType.GetMethod("AddResource",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(float) },
                    null);

                if (addResourceMethod != null)
                {
                    addResourceMethod.Invoke(storageComp, new object[] { fillAmount });
                    filledCount++;
                }
            }

            return filledCount;
        }
    }
}
