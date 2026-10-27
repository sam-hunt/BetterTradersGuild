using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.DebugToolsPatches
{
    // Harmony patch: DebugToolsSpawning.SpawnWorldObject (private dev-mode world tool)
    //
    // Vanilla bug fix. The "Spawn world object" tool makes any WorldObjectDef with
    // MakeWorldObject and adds it without ever calling SetFaction. Settlement-class
    // defs (Settlement, the Odyssey SpaceSettlement) cannot exist factionless:
    // Settlement.ExpandingIcon and Material dereference Faction, so the world map
    // logs an "Error while drawing Settlement" NRE every frame, and the object has no
    // name, trader or owner either. Players reached for it to add Traders Guild
    // orbital settlements (decompile-verified identical on 1.6 and 1.7.4990).
    //
    // The postfix only swaps the click action of Settlement-class nodes in the
    // returned list: it resolves the clicked tile on the selected layer (see
    // MouseTileOnSelectedLayer; vanilla's pick is surface-only), validates it with
    // TileFinder.IsValidTileForNewSettlement (not vanilla's caravan-pathing gate),
    // lists the factions that may live on that tile's layer, then builds the settlement the way vanilla's own
    // "Spawn random faction base" does (faction, tile, generated name). The faction
    // list is built at click time on purpose: Dialog_Debug caches the node tree for
    // the whole play-data load, so a submenu built ahead of time would keep the
    // previous save's Faction instances after loading another game.
    [HarmonyPatch]
    public static class DebugToolsSpawningSpawnWorldObject
    {
        private static readonly MethodInfo Target = AccessTools.Method(
            typeof(DebugToolsSpawning), "SpawnWorldObject");

        // The return type is checked too: Harmony throws from PatchAll (aborting every later
        // patch class) when the method's return type can't be assigned to __result, which a
        // vanilla refactor to a yield-based IEnumerable would trigger.
        private static bool TargetValid =>
            Target != null && typeof(List<DebugActionNode>).IsAssignableFrom(Target.ReturnType);

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return TargetValid;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return Target;
        }

        public static void VerifyPatched()
        {
            if (!TargetValid)
                Log.Warning("[Better Traders Guild] DebugToolsSpawning.SpawnWorldObject method not found via "
                    + "reflection, or no longer returns List<DebugActionNode>; the dev-mode Spawn world object tool will keep spawning factionless "
                    + "settlements that throw drawing errors. RimWorld API may have changed.");
        }

        [HarmonyPostfix]
        public static void Postfix(List<DebugActionNode> __result)
        {
            if (__result == null)
                return;

            foreach (DebugActionNode node in __result)
            {
                // Vanilla labels each node with its def's defName.
                WorldObjectDef def = DefDatabase<WorldObjectDef>.GetNamedSilentFail(node.label);
                if (def?.worldObjectClass == null || !typeof(Settlement).IsAssignableFrom(def.worldObjectClass))
                    continue;

                node.action = () => SpawnSettlementAtMouse(def);
            }
        }

        private static void SpawnSettlementAtMouse(WorldObjectDef def)
        {
            PlanetTile tile = MouseTileOnSelectedLayer();
            if (!tile.Valid)
            {
                Messages.Message("No tile under the mouse", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            // Vanilla gates this tool on Find.World.Impassable, a caravan pathing test;
            // settlements get the placement rule world generation and settling use
            // instead, which also says why a tile is refused.
            StringBuilder reason = new StringBuilder();
            if (!TileFinder.IsValidTileForNewSettlement(tile, reason))
            {
                Messages.Message(reason.Length > 0 ? reason.ToString() : "Invalid tile for a settlement",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            List<Faction> factions = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && !f.Hidden && !f.defeated && CanExistOnLayer(tile.Layer, f.def))
                .OrderBy(f => f.Name)
                .ToList();
            if (factions.Count == 0)
            {
                Messages.Message("No faction can have a settlement on this layer", MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            List<DebugMenuOption> options = factions
                .Select(faction => new DebugMenuOption(faction.Name + " (" + faction.def.defName + ")",
                    DebugMenuOptionMode.Action, () => SpawnSettlement(def, faction, tile)))
                .ToList();
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        // Only the surface draws a raycastable terrain mesh: the Orbit PlanetLayerDef lists
        // just WorldDrawLayer_ClosestTile, so no orbit tile ever owns a collider and
        // GenWorld.MouseTile is always Invalid while orbit is selected (the reason vanilla's
        // tool, and its "Impassable" message, never worked there). Vanilla itself never maps
        // a mouse position onto a non-surface layer (WorldTargeter's closest tile is
        // origin-based), so this does what VEF's layered spawner does: raycast against the
        // surface, whose colliders stay active on every layer, then take the selected
        // layer's closest tile. Flipping PlanetLayer.Selected is what makes the surface
        // mesh raycastable; it clears the world selection, which the click already did.
        private static PlanetTile MouseTileOnSelectedLayer()
        {
            PlanetLayer layer = PlanetLayer.Selected;
            PlanetTile tile = GenWorld.MouseTile();
            if (tile.Valid || layer == Find.WorldGrid.Surface)
                return tile;

            PlanetTile surfaceTile;
            PlanetLayer.Selected = Find.WorldGrid.Surface;
            try
            {
                WorldTerrainColliderManager.EnsureRaycastCollidersUpdated();
                surfaceTile = GenWorld.TileAt(UI.MousePositionOnUI);
            }
            finally
            {
                PlanetLayer.Selected = layer;
                WorldTerrainColliderManager.EnsureRaycastCollidersUpdated();
            }

            return surfaceTile.Valid ? layer.GetClosestTile_NewTemp(surfaceTile) : PlanetTile.Invalid;
        }

        private static void SpawnSettlement(WorldObjectDef def, Faction faction, PlanetTile tile)
        {
            Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(def);
            settlement.SetFaction(faction);
            settlement.Tile = tile;
            settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
            Find.WorldObjects.Add(settlement);
        }

        // Mirrors the private FactionGenerator.CanExistOnLayer, the rule world generation
        // uses to place factions: off the root surface a faction needs an explicit whitelist.
        private static bool CanExistOnLayer(PlanetLayer layer, FactionDef def)
        {
            if (!def.layerBlacklist.NullOrEmpty() && def.layerBlacklist.Contains(layer.Def))
                return false;
            if (!def.layerWhitelist.NullOrEmpty() || !layer.IsRootSurface)
                return def.layerWhitelist.Contains(layer.Def);
            return true;
        }
    }
}
