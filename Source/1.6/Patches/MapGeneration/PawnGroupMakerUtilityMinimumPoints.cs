using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.MapGenerationPatches
{
    // Harmony prefix patch for PawnGroupMakerUtility.GeneratePawns() that optionally scales
    // the TradersGuild settlement garrison to the player's actual threat level, then applies
    // the configurable multiplier and minimum-points floor from mod settings.
    //
    // WHY VANILLA NEVER SCALES SETTLEMENT DEFENDERS:
    // GenStep_SettlementPawnsLoot passes no points to MapGenUtility.GeneratePawns, which
    // then rolls the flat MapGenUtility.DefaultPawnsPoints range (1150-1600) - storyteller,
    // difficulty and colony wealth are never consulted. (Classic ground settlements do the
    // same via SymbolResolver_Settlement.DefaultPawnsPoints; only quest-site outposts get
    // real threat points, from GenStepParams.sitePart.) So a late-game colony assaults the
    // same ~1400-point garrison a young colony would. That flat roll is intentional vanilla
    // design, not a bug - which is why the threat-level scaling here is opt-in
    // (scaleDefendersToThreatLevel, default off), preserving long-shipped BTG behaviour for
    // existing players. The multiplier and floor apply whether or not scaling is opted into.
    //
    // THE SCALING BASE MUST COME FROM THE WORLD, NOT THE MAP:
    // Same trap as the sentry drone fix (GenStep_SpawnSentryDrones): at generation time the
    // settlement map has no player pawns on it, so DefaultThreatPointsNow(map) floors to the
    // storyteller minimum. DefaultThreatPointsNow(Find.World) sums player wealth across all
    // maps and caravans, giving the storyteller's real threat level (difficulty-scaled and
    // capped by the storyteller's own curve).
    //
    // ORDER OF OPERATIONS:
    // 1. If scaleDefendersToThreatLevel: raise the flat vanilla roll to the world's threat
    //    points (max, so an early-game garrison never drops below the vanilla 1150-1600
    //    baseline)
    // 2. Apply the configurable multiplier (0.5x-3.0x, default 1.0x)
    // 3. Enforce the configurable minimum floor (0-5000, default 0 = disabled; 2400
    //    recommended so MaxPawnCost admits elite pawn types at low wealth)
    //
    // parms.points determines both the total pawn budget AND MaxPawnCost (which filters
    // expensive pawn kinds), so raising points increases count and unlocks stronger kinds.
    //
    // This is the single choke point for the garrison group: both GenStep_BTGSettlementPawns
    // and the vanilla GenStep_SettlementPawnsLoot opt-out path funnel through
    // PawnGroupMakerUtility.GeneratePawns with the Settlement group kind, so scaling applies
    // regardless of the entrenched-defenders setting. Only applies when custom layouts are
    // enabled (to match their increased loot value).
    //
    // The smuggler's den garrison (Salvagers faction) is deliberately outside the faction
    // gate below: its budget is quest-driven via SitePartParams.threatPoints (wealth curve
    // in Script_BTG_SmugglersDen), delivered through the linkWithSite GenStepDefs, so the
    // settlement-defender settings here never touch it.
    [HarmonyPatch(typeof(PawnGroupMakerUtility))]
    [HarmonyPatch("GeneratePawns")]
    public static class PawnGroupMakerUtilityMinimumPoints
    {
        [HarmonyPrefix]
        public static void Prefix(PawnGroupMakerParms parms)
        {
            // Only apply when custom layouts are enabled (custom layouts have higher loot value)
            if (!BetterTradersGuildMod.Settings.useCustomLayouts) return;

            // Only affect TradersGuild faction
            if (parms?.faction?.def != Factions.TradersGuild) return;

            // Only affect the Settlement group kind (the garrison), not traders/caravans/raids
            if (parms.groupKind != PawnGroupKindDefOf.Settlement) return;

            // Step 1 (opt-in): Scale the flat vanilla roll up to the world's real threat points
            if (BetterTradersGuildMod.Settings.scaleDefendersToThreatLevel)
            {
                float worldPoints = StorytellerUtility.DefaultThreatPointsNow(Find.World);
                if (worldPoints > parms.points)
                {
                    parms.points = worldPoints;
                }
            }

            // Step 2: Apply threat points multiplier
            float multiplier = BetterTradersGuildMod.Settings.threatPointsMultiplier;
            if (multiplier != 1.0f)
            {
                parms.points *= multiplier;
            }

            // Step 3: Enforce minimum points floor (after multiplier)
            float minimumPoints = BetterTradersGuildMod.Settings.minimumThreatPoints;
            if (minimumPoints > 0f && parms.points < minimumPoints)
            {
                parms.points = minimumPoints;
            }
        }
    }
}
