using System;
using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.LayoutWorkers.Settlement
{
    // Custom LayoutWorker for BTG Settlement orbital platforms.
    //
    // Extends vanilla LayoutWorker_OrbitalPlatform with layout-time constraints
    // (a ShuttleBay-sized room) and an error guard around the spawn.
    //
    // Everything that operates on the spawned structure lives in GenSteps, which run
    // after GenStep_OrbitalPlatform returns (so after room contents, external landing
    // pads and the SpawnRect var exist): BTG_PlaceWallConduitsAndPipes (240),
    // BTG_PrimePipeNetworks (245), BTG_ReplaceTerrain/BTG_PaintTerrain (250/255),
    // BTG_ExtendLandingPadPipes (260), BTG_SetWallLampColor (265), the grower/stand
    // steps (310/315) and the defender steps (700+). See the pipeline defs under
    // 1.6/Defs/MapGeneratorDefs/.
    public class LayoutWorker_Settlement : LayoutWorker_OrbitalPlatform
    {
        // Context flag: true while BTG structure sketch generation is in progress.
        // Set by LayoutWorkerGenerateStructureSketch patch (Prefix/Finalizer).
        // Read by LayoutWorkerResolveRoomDefs transpiler to downgrade room placement errors.
        internal static bool IsGenerating;

        // Maximum attempts to generate a layout with a valid ShuttleBay room.
        // Based on testing, most layouts succeed on first attempt.
        private const int MaxLayoutAttempts = 10;

        // Minimum ShuttleBay room dimensions.
        // Room must fit both the 10x10 landing pad subroom AND the 3x3 cargo vault hatch.
        private const int MinShuttleBayWidth = 19;
        private const int MinShuttleBayHeight = 15;

        public LayoutWorker_Settlement(LayoutDef def) : base(def)
        {
        }

        // Override layout generation to ensure at least one room meets ShuttleBay size requirements.
        // Retries generation up to MaxLayoutAttempts times, then falls back to largest room.
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

        // Override important room assignment to enforce minimum ShuttleBay size.
        // Vanilla uses hardcoded 7x7 minimum which doesn't respect our size requirement.
        protected override void PostGraphsGenerated(StructureLayout layout, StructureGenParams parms)
        {
            // Don't call base - we're replacing the important room assignment logic entirely

            if (!parms.spawnImportantRoom)
                return;

            // Find largest room meeting size requirements (either orientation)
            var validRoom = layout.Rooms
                .Where(r => r.requiredDef == null)
                .Where(r => r.TryGetRectOfSize(MinShuttleBayWidth, MinShuttleBayHeight, out _) ||
                            r.TryGetRectOfSize(MinShuttleBayHeight, MinShuttleBayWidth, out _))
                .OrderByDescending(r => r.Area)
                .FirstOrDefault();

            if (validRoom != null)
            {
                validRoom.requiredDef = LayoutRooms.BTG_ShuttleBay;
                validRoom.noExteriorDoors = true;
                return;
            }

            // Fallback: use largest available room (shouldn't happen with retry logic)
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

        // Checks if the layout has any room meeting ShuttleBay size requirements (either orientation).
        private bool HasValidShuttleBayRoom(StructureLayout layout)
        {
            return layout.Rooms.Any(r =>
                r.requiredDef == null &&
                (r.TryGetRectOfSize(MinShuttleBayWidth, MinShuttleBayHeight, out _) ||
                 r.TryGetRectOfSize(MinShuttleBayHeight, MinShuttleBayWidth, out _)));
        }

        // Guards the vanilla spawn so a failing room worker leaves a partial structure
        // instead of aborting the whole platform GenStep.
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
            // Vanilla: walls, doors, floors, then every RoomContentsWorker
            try
            {
                base.Spawn(layoutStructureSketch, map, pos, threatPoints, allSpawnedThings, roofs, canReuseSketch, faction);
            }
            catch (Exception e)
            {
                Log.Error($"[Better Traders Guild] Error during settlement layout generation " +
                          $"(room contents may be incomplete): {e}");
            }
        }
    }
}
