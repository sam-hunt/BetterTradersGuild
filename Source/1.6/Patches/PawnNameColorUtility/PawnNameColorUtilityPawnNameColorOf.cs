using System.Collections.Generic;
using System.Reflection;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.LordJobs;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.Patches.PawnNameColorUtilityPatches
{
    // Manually lifecycled Harmony patch: PawnNameColorUtility.PawnNameColorOf
    //
    // Post-defeat garrison survivors keep their faction (clearing it broke every
    // same-faction contract: beds, tuck-in, feeding), so the label resolver still
    // paints them hostile red even though the defeat trigger guarantees they are
    // non-hostile (escaping or stranded). This postfix recolors them with vanilla's
    // neutral palette so they read like any neutral pawn, at both zoom extremes -
    // the resolver also feeds the zoomed-out dot highlights (SilhouetteUtility).
    //
    // NOT attribute-discovered: PatchAll must not apply this at startup. The resolver
    // runs per visible pawn per GUI repaint, on every map, for the whole game - and
    // defeated-with-survivors maps exist for a sliver of that time. Refresh() applies
    // the postfix only while one exists and unpatches otherwise, so the other 99% of
    // gametime carries not even a branch.
    //
    // Correctness never depends on that lifecycle - it is purely a perf optimization.
    // The per-pawn predicate re-derives everything from engine state (the pawn's OWN
    // map's defeat truth), so concurrent maps in different states resolve correctly:
    // a pre-defeat map's defenders stay red while another map's survivors show
    // neutral. Both lifecycle failure directions are benign: applied with no
    // qualifying map means a few dead branches per visible pawn; missing means
    // vanilla red labels.
    public static class PawnNameColorUtilityPawnNameColorOf
    {
        // Vanilla ColorBaseNeutral, used only if the ColorsNeutral reflection fails.
        private static readonly Color FallbackNeutral = new Color(0.4f, 0.85f, 0.9f);

        // Vanilla's private neutral palette; indexing it by randomKey below renders a
        // survivor exactly as it would look if its faction were neutral to the player.
        private static readonly FieldInfo ColorsNeutralField = typeof(PawnNameColorUtility)
            .GetField("ColorsNeutral", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly List<Color> colorsNeutral =
            ColorsNeutralField?.GetValue(null) as List<Color>;

        private static readonly MethodInfo TargetMethod = AccessTools.Method(
            typeof(PawnNameColorUtility), nameof(PawnNameColorUtility.PawnNameColorOf));

        private static bool applied;

        // Recomputes the desired patch state from world truth - never a refcount, so
        // call sites (settlement defeat, den defeat latch, map removal, game init)
        // cannot drift a counter. ignoredMap excludes a map mid-removal, which can
        // still appear in Find.Maps when MapRemoved fires.
        public static void Refresh(Map ignoredMap = null)
        {
            if (Current.Game != null && AnyDefeatedMapWithSurvivors(ignoredMap))
                ApplyPatch();
            else
                RemovePatch();
        }

        // Survivors can also all die AFTER the last Refresh with no event firing; the
        // patch then idles (predicate never matches) until map removal unpatches it.
        private static bool AnyDefeatedMapWithSurvivors(Map ignoredMap)
        {
            List<Map> maps = Find.Maps;
            if (maps == null)
                return false;

            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == ignoredMap || !TradersGuildHelper.IsPostDefeatMap(map))
                    continue;

                // IsPostDefeatMap already vetted the parent: a BTG den Site, or a
                // DestroyedSettlement - only TG settlements among those hold TG pawns.
                Faction owner = map.Parent is Site site
                    ? site.Faction
                    : Find.FactionManager.FirstFactionOfDef(Factions.TradersGuild);

                if (owner != null && map.mapPawns.SpawnedPawnsInFaction(owner).Count > 0)
                    return true;
            }

            return false;
        }

        private static void ApplyPatch()
        {
            if (applied || TargetMethod == null)
                return;

            BetterTradersGuildMod.Harmony.Patch(TargetMethod, postfix: new HarmonyMethod(
                typeof(PawnNameColorUtilityPawnNameColorOf), nameof(Postfix)));
            applied = true;
        }

        private static void RemovePatch()
        {
            if (!applied)
                return;

            // Removes only patches registered under our id; other mods' stay intact.
            BetterTradersGuildMod.Harmony.Unpatch(
                TargetMethod, HarmonyPatchType.Postfix, BetterTradersGuildMod.Harmony.Id);
            applied = false;
        }

        // Hot path while applied: per visible pawn per GUI repaint on the current map,
        // whichever map that is - hence cheapest, most selective checks first, and the
        // defeat/faction logic inlined rather than resolving a Faction instance per call.
        public static void Postfix(Pawn pawn, ref Color __result)
        {
            Faction pawnFaction = pawn.Faction;
            if (pawnFaction == null)
                return;

            Map map = pawn.Map;
            if (map == null)
                return;

            MapParent parent = map.Parent;
            if (parent is DestroyedSettlement)
            {
                if (pawnFaction.def != Factions.TradersGuild)
                    return;
            }
            else if (parent is Site site && site.def == WorldObjects.BTG_SmugglersDenSite)
            {
                if (pawnFaction != site.Faction || !SiteReflection.AllEnemiesDefeatedSent(site))
                    return;
            }
            else
            {
                return;
            }

            // Preserve vanilla precedence: mental state, prisoner and slave colors all
            // resolve before the hostile-faction branch this override replaces.
            if (pawn.MentalStateDef != null || pawn.IsPrisoner || pawn.IsSlave)
                return;

            // Genuine survivors only - see IBTGSurvivorLord. Pawns arriving in raid
            // lords after defeat (den TimedDetectionRaids reinforcements) stay red.
            Lord lord = pawn.GetLord();
            if (lord != null && !(lord.LordJob is IBTGSurvivorLord))
                return;

            __result = colorsNeutral?.Count > 0
                ? colorsNeutral[pawnFaction.randomKey % colorsNeutral.Count]
                : FallbackNeutral;
        }

        // Logs a targeted error for any member that failed to resolve. Called once at
        // startup from ReflectionVerification.VerifyAll.
        public static void VerifyReflection()
        {
            if (TargetMethod == null)
                Log.Error("[Better Traders Guild] PawnNameColorUtility.PawnNameColorOf method not found via reflection; "
                    + "post-defeat survivor name labels will stay hostile red. RimWorld API may have changed.");
            if (colorsNeutral == null || colorsNeutral.Count == 0)
                Log.Error("[Better Traders Guild] PawnNameColorUtility.ColorsNeutral field not found via reflection; "
                    + "post-defeat survivor name labels will use a fixed neutral color instead of the "
                    + "faction-shifted vanilla neutral. RimWorld API may have changed.");
        }
    }
}
