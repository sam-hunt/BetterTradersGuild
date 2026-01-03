using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;
using BetterTradersGuild.Helpers;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony postfix patch for GenStep_OrbitalPlatform.PostMapInitialized() to spawn
    /// sentry drones in TradersGuild settlements.
    ///
    /// PURPOSE:
    /// Spawns patrolling sentry drones that add dynamic challenge to TradersGuild settlements.
    /// Sentry drones patrol until they detect intruders, then switch to attack mode.
    ///
    /// TECHNICAL APPROACH:
    /// - Postfix runs after vanilla PostMapInitialized completes
    /// - Only affects TradersGuild settlements with custom layouts enabled
    /// - Uses player-configurable sentryDronePresence factor (0-200% of threat points)
    /// - Respects minimum threat points cap from PawnGroupMakerUtilityMinimumPoints
    /// - Creates a scaled SimpleCurve based on dronePresence factor
    /// - Calls vanilla BaseGenUtility.ScatterSentryDronesInMap for actual spawning
    ///
    /// SENTRY DRONE BEHAVIOR:
    /// - Patrol Mode: Wander around the map using JobGiver_SentryPatrol
    /// - Attack Mode: Triggered when enemies detected (65 cell radius, requires LoS)
    /// - Uses SentryDroneConstant ThinkTree with CompSentryDrone component
    /// - Creates more dynamic encounters vs all defenders rushing at once
    ///
    /// LEARNING NOTE (SimpleCurve Scaling):
    /// To apply a factor to threat points, we scale the X-axis of the curve.
    /// If dronePresence=0.25, we want 3000 points to behave like 750 points.
    /// We achieve this by scaling X values by 1/dronePresence (e.g., 3000 -> 12000).
    /// When vanilla evaluates 3000 against the scaled curve, it gets the reduced result.
    /// </summary>
    [HarmonyPatch(typeof(GenStep_OrbitalPlatform))]
    [HarmonyPatch("PostMapInitialized")]
    public static class GenStepOrbitalPlatformPostMapInitialized
    {
        /// <summary>
        /// Base curve that maps threat points to sentry drone count at 100% presence.
        /// X values will be scaled by 1/dronePresence to create the effective curve.
        ///
        /// At 100% presence (dronePresence=1.0):
        /// Points -> Drones:
        /// 0 -> 0 (no drones at zero points)
        /// 600 -> 2 (minimum meaningful patrol)
        /// 1200 -> 3 (light defense)
        /// 2400 -> 5 (standard defense at minimum cap)
        /// 4800 -> 8 (heavy defense for wealthy colonies)
        /// 9600 -> 12 (maximum defense)
        ///
        /// At 25% presence (dronePresence=0.25, default):
        /// 2400 points -> ~1-2 drones (X scaled to 9600)
        /// </summary>
        private static readonly CurvePoint[] BaseCurvePoints = new CurvePoint[]
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(600f, 2f),
            new CurvePoint(1200f, 3f),
            new CurvePoint(2400f, 5f),
            new CurvePoint(4800f, 8f),
            new CurvePoint(9600f, 12f)
        };

        /// <summary>
        /// Creates a scaled SimpleCurve based on dronePresence factor.
        /// Scaling the X-axis by 1/dronePresence effectively reduces drone count
        /// for the same threat points.
        /// </summary>
        private static SimpleCurve CreateScaledCurve(float dronePresence)
        {
            SimpleCurve scaledCurve = new SimpleCurve();
            float scaleFactor = 1f / dronePresence;

            foreach (CurvePoint point in BaseCurvePoints)
            {
                // Scale X values so higher dronePresence = more drones at same points
                scaledCurve.Add(new CurvePoint(point.x * scaleFactor, point.y));
            }

            return scaledCurve;
        }

        /// <summary>
        /// Harmony Postfix patch that runs after GenStep_OrbitalPlatform.PostMapInitialized().
        ///
        /// EXECUTION FLOW:
        /// 1. Check if Odyssey DLC is active (sentry drones require it)
        /// 2. Check if custom layouts feature enabled in mod settings
        /// 3. Check if sentry drone presence is greater than 0
        /// 4. Check if map parent is a TradersGuild settlement
        /// 5. Create scaled curve based on dronePresence setting
        /// 6. Spawn sentry drones using vanilla utility with scaled curve
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(GenStep_OrbitalPlatform __instance, Map map, GenStepParams parms)
        {
            // STEP 1: Check if Odyssey DLC is active (sentry drones require it)
            if (!ModsConfig.OdysseyActive)
            {
                return;
            }

            // STEP 2: Check if custom layouts feature enabled
            if (!BetterTradersGuildMod.Settings.useCustomLayouts)
            {
                return;
            }

            // STEP 3: Check if sentry drone presence is enabled
            float dronePresence = BetterTradersGuildMod.Settings.sentryDronePresence;
            if (dronePresence <= 0f)
            {
                return;
            }

            // STEP 4: Check if this is a TradersGuild settlement
            Settlement settlement = map?.Parent as Settlement;
            if (settlement == null || !TradersGuildHelper.IsTradersGuildSettlement(settlement))
            {
                return;
            }

            // STEP 5: Get faction for drone spawning
            Faction faction = settlement.Faction;
            if (faction == null)
            {
                Log.Warning("[Better Traders Guild] TradersGuild settlement has no faction, skipping sentry drone spawning.");
                return;
            }

            // STEP 6: Calculate effective threat points for logging
            // Note: We enforce minimum points via our scaled curve, not by modifying parms
            float actualPoints = parms.sitePart?.parms?.threatPoints ?? StorytellerUtility.DefaultThreatPointsNow(Find.World);
            float minimumPoints = PawnGroupMakerUtilityMinimumPoints.MinimumTradersGuildPoints;

            // STEP 7: Create scaled curve that accounts for dronePresence
            // Also adjust for minimum points by using the higher effective value
            float effectivePoints = System.Math.Max(actualPoints, minimumPoints);
            float adjustmentFactor = effectivePoints / System.Math.Max(actualPoints, 1f);
            float effectiveDronePresence = dronePresence * adjustmentFactor;
            SimpleCurve scaledCurve = CreateScaledCurve(effectiveDronePresence);

            // STEP 8: Spawn sentry drones using vanilla utility with our scaled curve
            BaseGenUtility.ScatterSentryDronesInMap(scaledCurve, map, faction, parms);

            // Calculate expected drone count for logging
            int expectedDrones = (int)scaledCurve.Evaluate(actualPoints);
            Log.Message($"[Better Traders Guild] Spawned sentry drones for TradersGuild settlement '{settlement.Name}': " +
                        $"actualPoints={actualPoints:F0}, minimumCap={minimumPoints:F0}, " +
                        $"dronePresence={dronePresence:P0}, adjustedPresence={effectiveDronePresence:P0}, " +
                        $"expectedDrones~{expectedDrones}");
        }
    }
}
