using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
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
    /// - GenStep_ReplaceTerrain (order 250): Replace AncientTile with MetalTile
    /// - GenStep_PaintTerrain (order 255): Paint terrain with BTG_OrbitalSteel
    /// - GenStep_ExtendLandingPadPipes (order 260): Extend VE pipes to landing pads
    /// - GenStep_SetWallLampColor (order 265): Set WallLamp glow color
    /// - GenStep_SpawnSentryDrones (order 705): Spawn sentry drones
    ///
    /// ARCHITECTURE:
    /// The LayoutWorker handles operations on things spawned by base.Spawn().
    /// The GenStep handles operations that need external features (landing pads, terrain)
    /// which are generated after the LayoutWorker returns.
    /// </summary>
    public class LayoutWorker_Settlement : LayoutWorker_OrbitalPlatform
    {
        /// <summary>
        /// Maximum attempts to generate a layout with a valid ShuttleBay room.
        /// Based on testing, most layouts succeed on first attempt.
        /// </summary>
        private const int MaxLayoutAttempts = 10;

        /// <summary>
        /// Tiered size requirements for ShuttleBay room.
        /// Ordered from ideal to minimum acceptable.
        /// Room must fit both the 10x10 landing pad subroom AND the 3x3 cargo vault hatch.
        /// Format: (minWidth, minHeight, maxConnections)
        ///
        /// When maxConnections is <= 2, the Shuttle pad prefab subroom placement algorithm
        /// can guarantee an edge or corner placement, requiring less total room
        /// </summary>
        private static readonly (int minW, int minH, int maxConns)[] ShuttleBaySizeRequirements =
        {
            (19, 15, int.MaxValue),  // Fits hatch comfortably with corner/edge/floating pad placement
            (15, 19, int.MaxValue),
            (19, 13, 2),             // Fits hatch comfortably with corner/edge pad placement
            (13, 19, 2),
            (17, 13, 1),             // Fits hatch comfortably with corner placement
            (13, 17, 1),
        };

        public LayoutWorker_Settlement(LayoutDef def) : base(def)
        {
        }

        /// <summary>
        /// Override layout generation to ensure at least one room meets ShuttleBay size requirements.
        /// Retries generation up to MaxLayoutAttempts times, then falls back to largest room.
        /// </summary>
        protected override StructureLayout GetStructureLayout(StructureGenParams parms, CellRect rect)
        {
            for (int attempt = 0; attempt < MaxLayoutAttempts; attempt++)
            {
                var layout = base.GetStructureLayout(parms, rect);

                if (HasValidShuttleBayRoom(layout))
                    return layout;

                // Last attempt - use it regardless and log warning
                if (attempt == MaxLayoutAttempts - 1)
                {
                    Log.Warning("[BTG] Failed to generate layout with valid ShuttleBay room after " +
                                $"{MaxLayoutAttempts} attempts. Using largest available room.");
                    return layout;
                }
            }

            // Should never reach here, but satisfy compiler
            return base.GetStructureLayout(parms, rect);
        }

        /// <summary>
        /// Override important room assignment to use tiered size requirements.
        /// Vanilla uses hardcoded 7x7 minimum which doesn't respect our minSingleRectWidth/Height.
        /// </summary>
        protected override void PostGraphsGenerated(StructureLayout layout, StructureGenParams parms)
        {
            // Don't call base - we're replacing the important room assignment logic entirely
            // Base uses hardcoded 7x7 minimum which ignores our 18x15 requirement

            if (!parms.spawnImportantRoom)
                return;

            // Find best room for ShuttleBay using tiered requirements
            for (int i = 0; i < ShuttleBaySizeRequirements.Length; i++)
            {
                var (minW, minH, maxConns) = ShuttleBaySizeRequirements[i];

                var validRoom = layout.Rooms
                    .Where(r => r.requiredDef == null)
                    .Where(r => r.TryGetRectOfSize(minW, minH, out _))
                    .Where(r => maxConns == int.MaxValue || r.connections.Count <= maxConns)
                    .OrderByDescending(r => r.Area)
                    .FirstOrDefault();

                if (validRoom != null)
                {
                    validRoom.requiredDef = LayoutRooms.BTG_ShuttleBay;
                    validRoom.noExteriorDoors = true;
                    return;
                }
            }

            // Fallback: use largest available room
            var largestRoom = layout.Rooms
                .Where(r => r.requiredDef == null)
                .OrderByDescending(r => r.Area)
                .FirstOrDefault();

            if (largestRoom != null)
            {
                Log.Warning("[BTG] No room found meeting ShuttleBay size requirements. " +
                            "Using largest available room.");
                largestRoom.requiredDef = LayoutRooms.BTG_ShuttleBay;
                largestRoom.noExteriorDoors = true;
            }
        }

        /// <summary>
        /// Checks if the layout has any room meeting ShuttleBay size requirements.
        /// </summary>
        private bool HasValidShuttleBayRoom(StructureLayout layout)
        {
            foreach (var (minW, minH, maxConns) in ShuttleBaySizeRequirements)
            {
                bool hasValid = layout.Rooms.Any(r =>
                    r.requiredDef == null &&
                    r.TryGetRectOfSize(minW, minH, out _) &&
                    (maxConns == int.MaxValue || r.connections.Count <= maxConns));

                if (hasValid)
                    return true;
            }

            return false;
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
            // - GenStep_ReplaceTerrain / GenStep_PaintTerrain (terrain aesthetics)
            // - GenStep_ExtendLandingPadPipes (needs external landing pads)
            // - GenStep_SetWallLampColor (lighting aesthetics)
            // - GenStep_SpawnSentryDrones (sentry drone spawning)
        }
    }
}
