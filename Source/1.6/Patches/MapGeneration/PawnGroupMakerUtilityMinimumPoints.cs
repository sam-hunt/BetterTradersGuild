using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony prefix patch for PawnGroupMakerUtility.GeneratePawns() to enforce
    /// a minimum point budget for TradersGuild settlement pawn generation.
    ///
    /// PURPOSE:
    /// Vanilla calculates settlement defender points based on player colony wealth,
    /// which can result in very low points for early-game colonies. This causes
    /// MaxPawnCost filtering to exclude expensive pawns (Elite, Heavy, Slasher, Magister).
    ///
    /// SOLUTION:
    /// Enforce a minimum of 3000 points for TradersGuild settlements. This ensures:
    /// - MaxPawnCost of ~357 (12% of 3000) allows all pawn types to spawn
    /// - Wealthy colonies still get scaled-up defenders (uses max of calculated vs minimum)
    /// - TradersGuild stations always feel like a significant challenge
    ///
    /// EXAMPLE:
    /// - Early colony (1164 points) -> boosted to 3000 points
    /// - Late colony (5000 points) -> keeps 5000 points (no change)
    ///
    /// LEARNING NOTE:
    /// PawnGroupMakerParms.points determines both total pawn budget AND MaxPawnCost
    /// (which filters expensive pawns). By boosting points, we increase both the
    /// number of pawns AND allow expensive pawn types to spawn.
    /// </summary>
    [HarmonyPatch(typeof(PawnGroupMakerUtility))]
    [HarmonyPatch("GeneratePawns")]
    [HarmonyPriority(Priority.High)] // Run before our logging patch
    public static class PawnGroupMakerUtilityMinimumPoints
    {
        /// <summary>
        /// Minimum points for TradersGuild settlement pawn generation.
        /// So the encounter has a minimum difficulty threshold to compensate
        /// for higher base loot value
        /// </summary>
        public const float MinimumTradersGuildPoints = 2400f;

        /// <summary>
        /// Prefix that enforces minimum points for TradersGuild settlements.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(PawnGroupMakerParms parms)
        {
            // Only affect TradersGuild faction
            if (parms?.faction?.def != Factions.TradersGuild)
            {
                return;
            }

            // Only affect Settlement group kind (not traders, caravans, etc.)
            if (parms.groupKind?.defName != "Settlement")
            {
                return;
            }

            // Only boost if below minimum (preserve higher values from wealthy colonies)
            if (parms.points < MinimumTradersGuildPoints)
            {
                float originalPoints = parms.points;
                parms.points = MinimumTradersGuildPoints;

                Log.Message($"[Better Traders Guild] Boosted TradersGuild settlement points from {originalPoints:F1} to {MinimumTradersGuildPoints:F1} (minimum threshold).");
            }
        }
    }
}
