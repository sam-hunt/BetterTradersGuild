using System.Reflection;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Integrations;
using Verse;

namespace BetterTradersGuild.LayoutWorkers.Settlement
{
    /// <summary>
    /// Fills VE Pipes network storage tanks to random levels on generated maps.
    ///
    /// PURPOSE:
    /// After map generation, tanks in VE pipe networks are empty. This helper fills
    /// them to appropriate levels to make the station feel operational:
    /// - Standard tanks: 20-50% (reasonable stockpile)
    /// - Oxygen tanks: 30-60% (life support critical, should be well-stocked)
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
        /// Fill percentage range for a tank type.
        /// </summary>
        private struct TankFillRange
        {
            public float MinPct;
            public float MaxPct;

            public TankFillRange(float minPct, float maxPct)
            {
                MinPct = minPct;
                MaxPct = maxPct;
            }
        }

        /// <summary>
        /// Standard fill range for most tanks (20-50%).
        /// </summary>
        private static readonly TankFillRange StandardFillRange = new TankFillRange(0.20f, 0.50f);

        /// <summary>
        /// Life support fill range for oxygen tanks (30-60% - critical systems).
        /// </summary>
        private static readonly TankFillRange LifeSupportFillRange = new TankFillRange(0.30f, 0.60f);

        /// <summary>
        /// Fills VE pipe network tanks on the map to random levels.
        /// Uses VEPipesIntegration to access PipeSystem.CompResourceStorage (optional mod).
        /// </summary>
        public static void FillTanksOnMap(Map map)
        {
            // No-op when VE Pipes isn't installed (or its API drifted — reported at startup).
            if (!VEPipesIntegration.Available) return;

            // VE Chemfuel tanks (standard fill)
            FillTanksOfDef(map, Things.PS_ChemfuelTank, StandardFillRange);

            // VE Nutrient Paste vat (standard fill)
            FillTanksOfDef(map, Things.VNPE_NutrientPasteVat, StandardFillRange);

            // VE Gravships oxygen tanks (life support critical - higher fill)
            FillTanksOfDef(map, Things.VGE_SmallOxygenTank, LifeSupportFillRange);

            // VE Gravships astrofuel tanks (standard fill)
            // ChemfuelTank is vanilla but patched by VGE to become small astrofuel tank
            FillTanksOfDef(map, Things.ChemfuelTank, StandardFillRange);
        }

        /// <summary>
        /// Fills all tanks of a specific ThingDef on the map.
        /// </summary>
        private static void FillTanksOfDef(Map map, ThingDef tankDef, TankFillRange fillRange)
        {
            if (tankDef == null) return;

            foreach (Thing thing in map.listerThings.ThingsOfDef(tankDef))
            {
                FillTank(thing, fillRange);
            }
        }

        /// <summary>
        /// Fills a single tank to a random level within the specified range.
        /// </summary>
        /// <param name="thing">The tank Thing</param>
        /// <param name="fillRange">The fill percentage range</param>
        /// <returns>True if tank was filled successfully</returns>
        private static bool FillTank(Thing thing, TankFillRange fillRange)
        {
            // Must be a ThingWithComps to have comps
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null)
                return false;

            // Find the CompResourceStorage comp on this tank (type resolved + verified at startup)
            ThingComp storageComp = null;
            foreach (ThingComp comp in thingWithComps.AllComps)
            {
                if (VEPipesIntegration.CompType.IsInstanceOfType(comp))
                {
                    storageComp = comp;
                    break;
                }
            }

            if (storageComp == null)
                return false;

            object props = VEPipesIntegration.PropsProperty.GetValue(storageComp);
            if (props == null)
                return false;

            // storageCapacity lives on the runtime type of the Props object, so it can only be
            // resolved per-instance (it stays a runtime-guarded lookup, not verified at startup).
            FieldInfo capacityField = props.GetType().GetField("storageCapacity",
                BindingFlags.Public | BindingFlags.Instance);
            if (capacityField == null)
                return false;

            float storageCapacity = (float)capacityField.GetValue(props);

            // Calculate random fill amount within the specified range
            float fillPct = Rand.Range(fillRange.MinPct, fillRange.MaxPct);
            float fillAmount = storageCapacity * fillPct;

            VEPipesIntegration.AddResourceMethod.Invoke(storageComp, new object[] { fillAmount });
            return true;
        }
    }
}
