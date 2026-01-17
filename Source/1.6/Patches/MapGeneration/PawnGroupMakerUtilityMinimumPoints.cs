using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    /// <summary>
    /// Harmony prefix patch for PawnGroupMakerUtility.GeneratePawns() to apply
    /// threat point multiplier and enforce minimum points for TradersGuild settlements.
    ///
    /// PURPOSE:
    /// Vanilla calculates settlement defender points based on player colony wealth,
    /// which can result in very low points for early-game colonies. This causes
    /// MaxPawnCost filtering to exclude expensive pawns (Elite, Heavy, Slasher, Magister).
    ///
    /// SOLUTION:
    /// 1. Apply configurable multiplier (0.5x-3.0x) to scale threat points
    /// 2. Enforce configurable minimum points floor for TradersGuild settlements
    ///
    /// This ensures:
    /// - MaxPawnCost allows all pawn types to spawn (at default 2400, MaxPawnCost ~288)
    /// - Wealthy colonies still get scaled-up defenders (uses max of calculated vs minimum)
    /// - TradersGuild stations feel like a significant challenge matching their loot value
    /// - Players can adjust difficulty via multiplier
    ///
    /// EXAMPLE (with 1.5x multiplier, 2400 minimum):
    /// - Early colony (1000 points) -> 1500 after multiplier -> capped to 2400 minimum
    /// - Late colony (3000 points) -> 4500 after multiplier -> keeps 4500 (above minimum)
    ///
    /// REQUIREMENTS:
    /// - Only applies when custom layouts are enabled (to match increased loot value)
    /// - Multiplier configurable via mod settings (0.5x-3.0x, default 1.0x)
    /// - Minimum points configurable via mod settings (0-5000, default 2400)
    /// - Set minimum to 0 to disable floor and use multiplied value only
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
        /// Prefix that applies threat multiplier and enforces minimum points for TradersGuild settlements.
        /// Only applies when custom layouts are enabled (to match increased loot value).
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(PawnGroupMakerParms parms)
        {
            // Only apply when custom layouts are enabled (custom layouts have higher loot value)
            if (!BetterTradersGuildMod.Settings.useCustomLayouts) return;

            // Only affect TradersGuild faction
            if (parms?.faction?.def != Factions.TradersGuild) return;

            // Only affect Settlement group kind (not traders, caravans, etc.)
            if (parms.groupKind?.defName != "Settlement") return;

            // Step 1: Apply threat points multiplier (before minimum cap)
            float multiplier = BetterTradersGuildMod.Settings.threatPointsMultiplier;
            if (multiplier != 1.0f)
            {
                parms.points *= multiplier;
            }

            // Step 2: Enforce minimum points floor (after multiplier)
            float minimumPoints = BetterTradersGuildMod.Settings.minimumThreatPoints;
            if (minimumPoints > 0f && parms.points < minimumPoints)
            {
                parms.points = minimumPoints;
            }
        }
    }
}
