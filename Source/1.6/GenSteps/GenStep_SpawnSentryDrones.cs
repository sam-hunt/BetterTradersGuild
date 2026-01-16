using BetterTradersGuild.Helpers;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep that spawns patrolling sentry drones in TradersGuild settlements.
    ///
    /// No XML parameters - reads configuration from ModSettings:
    /// - useCustomLayouts: Master toggle for BTG settlement features
    /// - sentryDronePresence: Scale factor (0-200%) for drone count
    /// - minimumThreatPoints: Minimum threat points for difficulty scaling
    ///
    /// Example usage in GenStepDef:
    /// <![CDATA[
    /// <genStep Class="BetterTradersGuild.MapGeneration.GenStep_SpawnSentryDrones" />
    /// ]]>
    ///
    /// SENTRY DRONE BEHAVIOR:
    /// - Patrol Mode: Wander around the map using JobGiver_SentryPatrol
    /// - Attack Mode: Triggered when enemies detected (65 cell radius, requires LoS)
    /// - Uses SentryDroneConstant ThinkTree with CompSentryDrone component
    /// - Creates more dynamic encounters vs all defenders rushing at once
    ///
    /// CURVE SCALING:
    /// To apply a factor to threat points, we scale the X-axis of the curve.
    /// If dronePresence=0.25, we want 3000 points to behave like 750 points.
    /// We achieve this by scaling X values by 1/dronePresence (e.g., 3000 -> 12000).
    /// When vanilla evaluates 3000 against the scaled curve, it gets the reduced result.
    /// </summary>
    public class GenStep_SpawnSentryDrones : GenStep
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
        /// 2400 points -> ~2-3 drones (X scaled to 9600)
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
        /// Deterministic seed for this GenStep.
        /// </summary>
        public override int SeedPart => 847291005;

        /// <summary>
        /// Spawns sentry drones in TradersGuild settlements based on ModSettings.
        /// </summary>
        public override void Generate(Map map, GenStepParams parms)
        {
            // STEP 1: Check if sentry drone presence is enabled
            float dronePresence = BetterTradersGuildMod.Settings.sentryDronePresence;
            if (dronePresence <= 0f)
                return;

            // STEP 2: Get faction for drone spawning
            Settlement settlement = map?.Parent as Settlement;
            Faction faction = settlement?.Faction;
            if (faction == null)
                return;

            // STEP 3: Calculate effective threat points
            // Note: We enforce minimum points via our scaled curve, not by modifying parms
            float actualPoints = parms.sitePart?.parms?.threatPoints ?? StorytellerUtility.DefaultThreatPointsNow(Find.World);
            float minimumPoints = BetterTradersGuildMod.Settings.minimumThreatPoints;

            // STEP 4: Create scaled curve that accounts for dronePresence
            // Also adjust for minimum points by using the higher effective value
            float effectivePoints = System.Math.Max(actualPoints, minimumPoints);
            float adjustmentFactor = effectivePoints / System.Math.Max(actualPoints, 1f);
            float effectiveDronePresence = dronePresence * adjustmentFactor;
            SimpleCurve scaledCurve = CreateScaledCurve(effectiveDronePresence);

            // STEP 5: Spawn sentry drones using vanilla utility with our scaled curve
            BaseGenUtility.ScatterSentryDronesInMap(scaledCurve, map, faction, parms);
        }

        /// <summary>
        /// Creates a scaled SimpleCurve based on dronePresence factor.
        /// Scaling the X-axis by 1/dronePresence effectively reduces drone count
        /// for the same threat points.
        /// </summary>
        private SimpleCurve CreateScaledCurve(float dronePresence)
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
    }
}
