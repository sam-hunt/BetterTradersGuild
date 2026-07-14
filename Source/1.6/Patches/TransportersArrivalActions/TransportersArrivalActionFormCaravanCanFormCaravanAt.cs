using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.TransportersArrivalActionPatches
{
    // Harmony patch: Allow shuttle arrivals to form caravans at friendly Traders Guild orbital tiles.
    //
    // Space layers set canFormCaravans=false, and CanFormCaravanAt is the single vanilla gate that
    // consults it on the shuttle trade-visit flow: TransportersArrivalAction_Trade and
    // _VisitSettlement inherit StillValid from _FormCaravan, which returns
    // CanFormCaravanAt(pods, destinationTile). If it fails on arrival,
    // TravellingTransporters.Arrived discards the arrival action, finds no fallback for a
    // settlement tile, and banishes every pawn aboard.
    //
    // This replaces the retired PlanetTile.LayerDef getter patch, which satisfied the same check by
    // swapping in a cloned layer def with canFormCaravans=true. Patching the one consumer instead of
    // the hot getter keeps the real def visible to everything else (incident/arrival whitelists,
    // def-identity comparisons in other mods, world UI tabs).
    [HarmonyPatch(typeof(TransportersArrivalAction_FormCaravan),
        nameof(TransportersArrivalAction_FormCaravan.CanFormCaravanAt))]
    public static class TransportersArrivalActionFormCaravanCanFormCaravanAt
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, IEnumerable<IThingHolder> pods, PlanetTile tile)
        {
            if (__result)
                return;

            if (!TileHelper.IsFriendlyTradersGuildTile(tile))
                return;

            // Only override the layer's canFormCaravans veto: the vanilla prerequisites it was
            // AND-ed with (a pawn aboard who can own a caravan, passable tile) still apply.
            __result = TransportersArrivalActionUtility.AnyPotentialCaravanOwner(pods, Faction.OfPlayer)
                && !Find.World.Impassable(tile);
        }
    }
}
