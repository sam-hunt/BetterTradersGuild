using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that spawns patrolling sentry drones in TradersGuild settlements.
    //
    // No XML parameters - reads configuration from ModSettings:
    // - useCustomLayouts: Master toggle for BTG settlement features
    // - sentryDronePresence: Scale factor (0-200%) for drone count
    // - minimumThreatPoints: Minimum threat points for difficulty scaling
    //
    // Example usage in GenStepDef:
    // <genStep Class="BetterTradersGuild.MapGeneration.GenStep_SpawnSentryDrones" />
    //
    // SENTRY DRONE BEHAVIOR:
    // - Patrol Mode: Wander around the map using JobGiver_SentryPatrol
    // - Attack Mode: Triggered when enemies detected (65 cell radius, requires LoS)
    // - Uses SentryDroneConstant ThinkTree with CompSentryDrone component
    // - Creates more dynamic encounters vs all defenders rushing at once
    //
    // THREAT POINTS MUST COME FROM THE WORLD, NOT THE MAP:
    // Vanilla BaseGenUtility.ScatterSentryDronesInMap evaluates its count curve at
    // DefaultThreatPointsNow(map). But a freshly generated ENEMY settlement map has zero
    // player wealth - Map.PlayerWealthForStoryteller only counts player-faction pawns/gear
    // on the map, and the raiding party has not landed yet at generation time - so that
    // value floors to the storyteller minimum and the curve yields ~0 drones regardless of
    // the presence setting. That is why sentries had all but vanished.
    //
    // So we evaluate the count ourselves against the world's real threat points and hand
    // vanilla a CONSTANT curve: a flat curve returns the same count for any input, so
    // vanilla's map-based evaluation can no longer zero us out, while all of its placement
    // logic (valid rooms, standable cells, pawnkind, spawn) still runs unchanged.
    public class GenStep_SpawnSentryDrones : GenStep
    {
        // Base curve mapping threat points -> sentry drone count at 100% presence.
        // Evaluated at (effectivePoints * dronePresence); lowering presence lowers the
        // effective points fed in, equivalent to the previous 1/presence X-axis scaling
        // but evaluated at the correct (world) threat points.
        //
        // Points -> Drones at 100% presence:
        // 0 -> 0, 600 -> 2, 1200 -> 3, 2400 -> 5, 4800 -> 8, 9600 -> 12
        private static readonly SimpleCurve DroneCountFromPoints = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(600f, 2f),
            new CurvePoint(1200f, 3f),
            new CurvePoint(2400f, 5f),
            new CurvePoint(4800f, 8f),
            new CurvePoint(9600f, 12f)
        };

        // Deterministic seed for this GenStep.
        public override int SeedPart => 847291005;

        // Spawns sentry drones in TradersGuild settlements based on ModSettings.
        public override void Generate(Map map, GenStepParams parms)
        {
            // Sentry presence disabled: nothing to do.
            float dronePresence = BetterTradersGuildMod.Settings.sentryDronePresence;
            if (dronePresence <= 0f)
                return;

            // Parent faction (works for both Settlements and quest Sites).
            Faction faction = map?.Parent?.Faction;
            if (faction == null)
                return;

            // Real threat points from the world (falls back to the site's own value if this
            // is ever generated as a quest site). NOT DefaultThreatPointsNow(map) - see the
            // class comment for why the map's value is zero at generation time.
            float actualPoints = parms.sitePart?.parms?.threatPoints
                ?? StorytellerUtility.DefaultThreatPointsNow(Find.World);
            float minimumPoints = BetterTradersGuildMod.Settings.minimumThreatPoints;
            float effectivePoints = System.Math.Max(actualPoints, minimumPoints);

            // Intended count = base curve evaluated at the presence-scaled points.
            int droneCount = UnityEngine.Mathf.RoundToInt(DroneCountFromPoints.Evaluate(effectivePoints * dronePresence));
            if (droneCount <= 0)
                return;

            // Constant curve: vanilla evaluates it against the map's (zero-wealth) threat
            // points, but a flat curve returns droneCount for any input.
            SimpleCurve constantCurve = new SimpleCurve { new CurvePoint(0f, droneCount) };
            BaseGenUtility.ScatterSentryDronesInMap(constantCurve, map, faction, parms);
        }
    }
}
