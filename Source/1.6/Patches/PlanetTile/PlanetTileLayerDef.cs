using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.PlanetTilePatches
{
    /// <summary>
    /// Harmony patch: Allow caravans to reach friendly Traders Guild settlements in space
    /// Modifies the LayerDef check for caravan formation
    /// </summary>
    [HarmonyPatch(typeof(PlanetTile), nameof(PlanetTile.LayerDef), MethodType.Getter)]
    [StaticConstructorOnStartup]
    public static class PlanetTileLayerDef
    {
        private static readonly MethodInfo MemberwiseCloneMethod =
            AccessTools.Method(typeof(object), "MemberwiseClone");

        /// <summary>
        /// Maps each space <see cref="PlanetLayerDef"/> that forbids caravans to a caravan-enabled
        /// clone of itself.
        /// </summary>
        /// <remarks>
        /// Built once at startup from the loaded defs - the set of space layers is global and fixed
        /// for the session, so it never varies per world/save and does not belong on a WorldComponent.
        /// At runtime the hot LayerDef getter only reads this dictionary (no allocation, mutation, or
        /// locking), which keeps concurrent reads from RimWorld's path/render threads safe.
        ///
        /// Keyed per source def rather than collapsed into a single shared clone: a mod can introduce
        /// more than one space layer (vanilla Odyssey has just one, "Orbit"), and each must retain its
        /// own raidPointsFactor / whitelist / rangeDistanceFactor instead of inheriting whichever layer
        /// happened to be requested first.
        /// </remarks>
        private static readonly Dictionary<PlanetLayerDef, PlanetLayerDef> caravanEnabledClones =
            new Dictionary<PlanetLayerDef, PlanetLayerDef>();

        static PlanetTileLayerDef()
        {
            foreach (PlanetLayerDef layer in DefDatabase<PlanetLayerDef>.AllDefsListForReading)
            {
                // Only space layers that don't already allow caravans need a caravan-enabled variant.
                if (!layer.isSpace || layer.canFormCaravans)
                    continue;

                // Clone the layer, preserving all vanilla properties (e.g. rangeDistanceFactor=20
                // prevents proximity goodwill penalties, onlyAllowWhitelist* filters inappropriate
                // incidents/arrivals/quests) and only overriding the caravan-relevant fields.
                PlanetLayerDef clone = (PlanetLayerDef)MemberwiseCloneMethod.Invoke(layer, null);
                clone.defName = layer.defName + "_BTG";
                clone.canFormCaravans = true;    // This is the key change for trade visits
                clone.raidPointsFactor = 1.0f;   // Default for space is 0.85
                caravanEnabledClones[layer] = clone;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(PlanetTile __instance, ref PlanetLayerDef __result)
        {
            // Only care about space layers that don't allow caravans
            if (__result == null || __result.canFormCaravans || !__result.isSpace)
                return;

            // Check if this is a friendly Traders Guild tile
            if (!TileHelper.IsFriendlyTradersGuildTile(__instance))
                return;

            // Swap in the caravan-enabled clone of THIS specific space layer (not a single shared
            // clone), so multi-space-layer setups keep each layer's own properties. Leaves __result
            // untouched if the layer somehow wasn't catalogued at startup.
            if (caravanEnabledClones.TryGetValue(__result, out PlanetLayerDef caravanEnabled))
                __result = caravanEnabled;
        }
    }
}
