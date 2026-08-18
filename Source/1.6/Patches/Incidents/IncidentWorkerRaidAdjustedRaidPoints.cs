using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace BetterTradersGuild.Patches.Incidents
{
    // Harmony patch: Use full raid points at friendly Traders Guild settlement tiles.
    //
    // Space layers apply raidPointsFactor 0.85 to every raid/threat targeting a map on the layer,
    // and AdjustedRaidPoints is the only vanilla consumer of that field. Guild settlements are
    // dense, defended stations, so raids there should not get the empty-space discount. Undoing
    // the factor here preserves the behavior of the retired PlanetTile.LayerDef getter patch,
    // whose cloned layer def set raidPointsFactor=1.0.
    //
    // Deliberately friendly-TG-settlement only: the smugglers den quest site keeps the vanilla
    // discount. Smugglers keep a lower profile than the guild's stations, so raids that reach
    // them stay space-sized.
    //
    // Multiplying after the original ran is safe: the vanilla MinimumPoints clamp only enforces a
    // lower bound, and this correction only ever raises the value.
    [HarmonyPatch(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.AdjustedRaidPoints))]
    public static class IncidentWorkerRaidAdjustedRaidPoints
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result, IIncidentTarget target)
        {
            if (target?.Tile.Valid != true)
                return;

            PlanetTile tile = target.Tile;
            if (!TileHelper.IsFriendlyTradersGuildTile(tile))
                return;

            float factor = tile.LayerDef.raidPointsFactor;
            if (factor > 0f && factor != 1f)
                __result *= 1f / factor;
        }
    }
}
