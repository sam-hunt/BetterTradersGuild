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
    /// Enforce a configurable minimum points for TradersGuild settlements. This ensures:
    /// - MaxPawnCost allows all pawn types to spawn (at default 2400, MaxPawnCost ~288)
    /// - Wealthy colonies still get scaled-up defenders (uses max of calculated vs minimum)
    /// - TradersGuild stations feel like a significant challenge matching their loot value
    ///
    /// EXAMPLE:
    /// - Early colony (1164 points) -> boosted to 2400 points (default)
    /// - Late colony (5000 points) -> keeps 5000 points (no change)
    ///
    /// REQUIREMENTS:
    /// - Only applies when custom layouts are enabled (to match increased loot value)
    /// - Minimum points configurable via mod settings (0-5000, default 2400)
    /// - Set to 0 in settings to disable and use vanilla wealth-based calculation
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
        /// Prefix that enforces minimum points for TradersGuild settlements.
        /// Only applies when custom layouts are enabled (to match increased loot value).
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(PawnGroupMakerParms parms)
        {
            // Only apply when custom layouts are enabled (custom layouts have higher loot value)
            if (!BetterTradersGuildMod.Settings.useCustomLayouts) return;

            // Get minimum points from settings (0 = disabled)
            float minimumPoints = BetterTradersGuildMod.Settings.minimumThreatPoints;
            if (minimumPoints <= 0f) return;

            // Only affect TradersGuild faction
            if (parms?.faction?.def != Factions.TradersGuild) return;

            // Only affect Settlement group kind (not traders, caravans, etc.)
            if (parms.groupKind?.defName != "Settlement") return;

            // Only boost if below minimum (preserve higher values from wealthy colonies)
            if (parms.points < minimumPoints)
            {
                float originalPoints = parms.points;
                parms.points = minimumPoints;
            }
        }
    }
}
