using System.Collections.Generic;
using BetterTradersGuild.WorldObjects;
using BetterTradersGuild.Helpers.MapGeneration;
using RimWorld;
using Verse;

namespace BetterTradersGuild.LayoutWorkers.Settlement
{
    /// <summary>
    /// Custom LayoutWorker for BTG Settlement orbital platforms.
    ///
    /// Extends vanilla LayoutWorker_OrbitalPlatform with pre-spawn hooks and
    /// infrastructure setup that must happen during structure generation.
    ///
    /// LIFECYCLE:
    /// 1. PRE-SPAWN: Initialize TradersGuildSettlementComponent for cargo tracking
    /// 2. BASE SPAWN: Vanilla orbital platform generation (walls, doors, rooms, contents)
    /// 3. POST-SPAWN INFRASTRUCTURE: Hidden conduits, pipe network tanks, valves
    ///
    /// NOTE: Post-spawn aesthetics and external infrastructure are handled by separate GenSteps
    /// which run AFTER all structure generation completes (including external landing pads):
    /// - GenStep_BTGReplaceTerrain (order 250): Replace AncientTile with MetalTile
    /// - GenStep_BTGPaintTerrain (order 255): Paint terrain with BTG_OrbitalSteel
    /// - GenStep_BTGLandingPadPipes (order 260): Extend VE pipes to landing pads
    /// - GenStep_BTGSetWallLampColor (order 265): Set WallLamp glow color
    /// - GenStep_BTGSentryDrones (order 705): Spawn sentry drones
    ///
    /// ARCHITECTURE:
    /// The LayoutWorker handles operations on things spawned by base.Spawn().
    /// The GenStep handles operations that need external features (landing pads, terrain)
    /// which are generated after the LayoutWorker returns.
    /// </summary>
    public class LayoutWorker_BTGSettlement : LayoutWorker_OrbitalPlatform
    {
        public LayoutWorker_BTGSettlement(LayoutDef def) : base(def)
        {
        }

        /// <summary>
        /// Main entry point - overrides vanilla Spawn with clear pre/post hooks.
        /// </summary>
        public override void Spawn(
            LayoutStructureSketch layoutStructureSketch,
            Map map,
            IntVec3 pos,
            float? threatPoints = null,
            List<Thing> allSpawnedThings = null,
            bool roofs = true,
            bool canReuseSketch = false,
            Faction faction = null)
        {
            // ═══════════════════════════════════════════════════════════════════
            // PRE-SPAWN: Settlement component initialization
            // ═══════════════════════════════════════════════════════════════════

            // Initialize cargo tracking component (only if cargo system enabled)
            // Note: Fully qualified to avoid namespace conflict with BetterTradersGuild.LayoutWorkers.Settlement
            RimWorld.Planet.Settlement settlement = map?.Parent as RimWorld.Planet.Settlement;
            if (settlement != null &&
                BetterTradersGuildMod.Settings.cargoInventoryPercentage > 0f &&
                settlement.GetComponent<TradersGuildSettlementComponent>() == null)
            {
                var component = new TradersGuildSettlementComponent();
                settlement.AllComps.Add(component);
                component.parent = settlement;
            }

            // ═══════════════════════════════════════════════════════════════════
            // BASE SPAWN: Vanilla orbital platform generation
            // (walls, doors, room layouts, RoomContentsWorkers, furniture)
            // ═══════════════════════════════════════════════════════════════════
            base.Spawn(layoutStructureSketch, map, pos, threatPoints, allSpawnedThings, roofs, canReuseSketch, faction);

            // ═══════════════════════════════════════════════════════════════════
            // POST-SPAWN INFRASTRUCTURE: Power and fluid networks
            // These operate on things spawned by base.Spawn(), so they work here.
            // ═══════════════════════════════════════════════════════════════════

            // Place hidden conduits and VE pipes under all walls
            LayoutConduitPlacer.PlaceHiddenConduits(map, layoutStructureSketch);

            // Fill VE pipe network tanks to operational levels
            PipeNetworkTankFiller.FillTanksOnMap(map);

            // Close all VE pipe valves and remove faction ownership (lockdown state)
            PipeValveHandler.CloseAllValvesAndClearFaction(map);

            // NOTE: The following are handled by separate GenSteps (order 250-705)
            // because they require external features that don't exist until after
            // the LayoutWorker returns:
            // - GenStep_BTGReplaceTerrain / GenStep_BTGPaintTerrain (terrain aesthetics)
            // - GenStep_BTGLandingPadPipes (needs external landing pads)
            // - GenStep_BTGSetWallLampColor (lighting aesthetics)
            // - GenStep_BTGSentryDrones (sentry drone spawning)
        }
    }
}
