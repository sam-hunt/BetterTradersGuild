using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep for generating the cargo hold vault pocket map.
    ///
    /// This GenStep uses the proper RimWorld layout system:
    /// 1. Creates StructureGenParams with vault dimensions
    /// 2. Gets the LayoutDef's Worker (LayoutWorker_BTGCargoHold)
    /// 3. Calls GenerateStructureSketch() to create the layout
    /// 4. Calls Spawn() to place walls, floors, rooms, prefabs, and parts
    /// 5. Places external defense platforms connected by bridges
    ///
    /// The pocket map is 75x75 with the 25x25 vault structure centered,
    /// and 4 external 15x15 platforms connected by 3-wide bridges.
    /// </summary>
    public class GenStep_CargoHoldVault : GenStep
    {
        /// <summary>
        /// Vault structure size (walls + interior).
        /// The structure is centered on the larger pocket map.
        /// </summary>
        private const int VAULT_SIZE = 25;

        /// <summary>
        /// External platform size.
        /// </summary>
        private const int PLATFORM_SIZE = 15;

        /// <summary>
        /// Bridge width connecting vault to external platforms.
        /// </summary>
        private const int BRIDGE_WIDTH = 3;

        /// <summary>
        /// Gap between vault edge and platform edge (bridge length).
        /// </summary>
        private const int PLATFORM_GAP = 5;

        /// <summary>
        /// Seed for deterministic generation.
        /// </summary>
        public override int SeedPart => 738491625;

        /// <summary>
        /// Main generation method called during pocket map creation.
        /// Uses the layout system to generate the vault structure and contents.
        /// </summary>
        public override void Generate(Map map, GenStepParams parms)
        {
            // Define the vault area - centered on the map
            // For a 40x40 map with 20x20 vault, this places the vault at (10,10) to (29,29)
            CellRect vaultRect = CellRect.CenteredOn(map.Center, VAULT_SIZE, VAULT_SIZE);

            Log.Message($"[Better Traders Guild] GenStep_CargoHoldVault: Generating vault " +
                        $"(vault rect: {vaultRect}, map size: {map.Size.x}x{map.Size.z}, center: {map.Center})");

            // Create structure generation parameters
            StructureGenParams genParams = new StructureGenParams
            {
                size = new IntVec2(vaultRect.Width, vaultRect.Height)
            };

            // Get the layout definition - use parms.layout if provided, otherwise our default
            LayoutDef layoutDef = parms.layout ?? Layouts.BTG_CargoHoldVaultLayout;
            if (layoutDef == null)
            {
                Log.Error("[Better Traders Guild] GenStep_CargoHoldVault: BTG_CargoHoldVaultLayout not found!");
                return;
            }

            // Get the layout worker
            LayoutWorker worker = layoutDef.Worker;
            if (worker == null)
            {
                Log.Error("[Better Traders Guild] GenStep_CargoHoldVault: Layout worker is null!");
                return;
            }

            // Generate the structure sketch (defines rooms, walls, doors, etc.)
            LayoutStructureSketch sketch = worker.GenerateStructureSketch(genParams);
            if (sketch == null)
            {
                Log.Error("[Better Traders Guild] GenStep_CargoHoldVault: Failed to generate structure sketch!");
                return;
            }

            // Store sketch for later reference (used by various systems)
            map.layoutStructureSketches.Add(sketch);

            // Get threat points from site part if available (for scaling threats)
            float? threatPoints = null;
            if (parms.sitePart?.parms != null)
            {
                threatPoints = parms.sitePart.parms.points;
            }

            // Get the faction for ownership
            Faction tradersGuild = Find.FactionManager.FirstFactionOfDef(Factions.TradersGuild);

            // Spawn the structure on the map
            // The sketch uses coordinates from (0,0) to (size-1, size-1)
            // Spawn at vault rect's bottom-left corner to center it on the map
            IntVec3 spawnPos = new IntVec3(vaultRect.minX, 0, vaultRect.minZ);
            List<Thing> spawnedThings = new List<Thing>();
            worker.Spawn(
                sketch,
                map,
                spawnPos,  // Offset to center vault on map
                threatPoints,
                spawnedThings,
                false,  // clearRooms - don't clear existing things
                false,  // unfog - FogSpace GenStep will handle this
                tradersGuild
            );

            Log.Message($"[Better Traders Guild] GenStep_CargoHoldVault: Spawned {spawnedThings.Count} things");

            // Spawn external platforms and bridges
            SpawnExternalPlatforms(map, vaultRect, tradersGuild);

            // Spawn Gauss cannons at vault corners
            SpawnCornerCannons(map, vaultRect, tradersGuild);

            // Set player start spot in the center of the vault
            MapGenerator.PlayerStartSpot = vaultRect.CenterCell;
        }

        /// <summary>
        /// Spawns 4 external platforms at cardinal directions from the vault,
        /// connected by 3-wide bridges.
        /// </summary>
        private void SpawnExternalPlatforms(Map map, CellRect vaultRect, Faction faction)
        {
            PrefabDef platformPrefab = Prefabs.BTG_CargoHoldPlatform;
            if (platformPrefab == null)
            {
                Log.Warning("[Better Traders Guild] BTG_CargoHoldPlatform prefab not found, skipping external platforms");
                return;
            }

            TerrainDef bridgeTerrain = TerrainDefOf.OrbitalPlatform;
            int vaultCenterX = vaultRect.CenterCell.x;
            int vaultCenterZ = vaultRect.CenterCell.z;

            // North platform
            SpawnPlatformWithBridge(
                map, platformPrefab, bridgeTerrain, faction,
                new IntVec3(vaultCenterX, 0, vaultRect.maxZ + PLATFORM_GAP + PLATFORM_SIZE / 2 + 1),
                Rot4.North, vaultRect);

            // South platform
            SpawnPlatformWithBridge(
                map, platformPrefab, bridgeTerrain, faction,
                new IntVec3(vaultCenterX, 0, vaultRect.minZ - PLATFORM_GAP - PLATFORM_SIZE / 2 - 1),
                Rot4.South, vaultRect);

            // East platform
            SpawnPlatformWithBridge(
                map, platformPrefab, bridgeTerrain, faction,
                new IntVec3(vaultRect.maxX + PLATFORM_GAP + PLATFORM_SIZE / 2 + 1, 0, vaultCenterZ),
                Rot4.East, vaultRect);

            // West platform
            SpawnPlatformWithBridge(
                map, platformPrefab, bridgeTerrain, faction,
                new IntVec3(vaultRect.minX - PLATFORM_GAP - PLATFORM_SIZE / 2 - 1, 0, vaultCenterZ),
                Rot4.West, vaultRect);

            Log.Message("[Better Traders Guild] GenStep_CargoHoldVault: Spawned 4 external platforms with bridges");
        }

        /// <summary>
        /// Spawns a single platform at the given center position and draws a bridge to the vault.
        /// </summary>
        private void SpawnPlatformWithBridge(
            Map map,
            PrefabDef prefab,
            TerrainDef bridgeTerrain,
            Faction faction,
            IntVec3 platformCenter,
            Rot4 direction,
            CellRect vaultRect)
        {
            // Calculate platform rect centered on the given position
            CellRect platformRect = CellRect.CenteredOn(platformCenter, PLATFORM_SIZE, PLATFORM_SIZE);

            // Clamp to map bounds
            platformRect = platformRect.ClipInsideMap(map);

            // Spawn platform terrain first
            foreach (IntVec3 cell in platformRect)
            {
                if (cell.InBounds(map))
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.OrbitalPlatform);
                }
            }

            // Spawn the prefab at platform center
            // PrefabUtility expects the center position
            PrefabUtility.SpawnPrefab(
                prefab,
                map,
                platformCenter,
                Rot4.North,  // Keep prefab upright
                faction,
                null,  // spawnedThings
                null,  // thingOverride
                thing =>
                {
                    // Fill batteries to full charge
                    if (thing is Building_Battery battery)
                    {
                        battery.GetComp<CompPowerBattery>()?.AddEnergy(float.MaxValue);
                    }
                },
                false  // clearEdifices
            );

            // Draw bridge connecting vault to platform
            DrawBridge(map, bridgeTerrain, vaultRect, platformRect, direction);
        }

        /// <summary>
        /// Spawns Gauss cannons at the corners of the vault, similar to orbital platform settlements.
        /// Each cannon gets a circular platform pad (OrbitalPlatform outer, MetalTile inner).
        /// </summary>
        private void SpawnCornerCannons(Map map, CellRect vaultRect, Faction faction)
        {
            ThingDef cannonDef = ThingDefOf.GaussCannon;
            if (cannonDef == null)
            {
                Log.Warning("[Better Traders Guild] GaussCannon def not found, skipping corner cannons");
                return;
            }

            // Cannon is 9x9 and spawns from its CENTER (not bottom-left like most buildings)
            int halfSize = cannonDef.size.x / 2;  // 4

            // Distance from vault edge to cannon center
            int offset = 2;

            // Vanilla terrain radii (from GenStep_OrbitalMechhive.GenerateCannons):
            // Uses InHorDistOf (Chebyshev distance), not Euclidean
            // innerRadius = 5.9 for inner terrain (AncientTile in vanilla, MetalTile here)
            // outerRadius = 8.0 for outer terrain (MechanoidPlatform in vanilla, OrbitalPlatform here)
            const float innerRadius = 5.9f;
            const float outerRadius = 8.0f;
            const int checkRadius = 8;  // Slightly larger than outerRadius to ensure coverage

            // Corner center positions and rotations (facing outward from vault)
            // Since cannon spawns from center, these are the actual spawn positions
            var cornersWithRotation = new (IntVec3 center, Rot4 rot)[]
            {
                (new IntVec3(vaultRect.minX - offset - halfSize, 0, vaultRect.minZ - offset - halfSize), Rot4.South),  // SW
                (new IntVec3(vaultRect.maxX + offset + halfSize, 0, vaultRect.minZ - offset - halfSize), Rot4.East),   // SE
                (new IntVec3(vaultRect.minX - offset - halfSize, 0, vaultRect.maxZ + offset + halfSize), Rot4.West),   // NW
                (new IntVec3(vaultRect.maxX + offset + halfSize, 0, vaultRect.maxZ + offset + halfSize), Rot4.North),  // NE
            };

            foreach (var (center, rotation) in cornersWithRotation)
            {
                if (!center.InBounds(map)) continue;

                // Place terrain pads around cannon center using vanilla's InHorDistOf (Chebyshev distance)
                for (int dx = -checkRadius; dx <= checkRadius; dx++)
                {
                    for (int dz = -checkRadius; dz <= checkRadius; dz++)
                    {
                        IntVec3 cell = new IntVec3(center.x + dx, 0, center.z + dz);
                        if (!cell.InBounds(map)) continue;

                        // InHorDistOf uses Chebyshev distance, matching vanilla behavior exactly
                        if (cell.InHorDistOf(center, innerRadius))
                        {
                            // Inner zone - MetalTile
                            map.terrainGrid.SetTerrain(cell, TerrainDefOf.MetalTile);
                        }
                        else if (cell.InHorDistOf(center, outerRadius))
                        {
                            // Outer ring - OrbitalPlatform
                            map.terrainGrid.SetTerrain(cell, TerrainDefOf.OrbitalPlatform);
                        }
                    }
                }

                // Spawn the cannon at center position, facing outward
                Thing cannon = ThingMaker.MakeThing(cannonDef);
                cannon.SetFaction(faction);
                GenSpawn.Spawn(cannon, center, map, rotation);
            }

            Log.Message("[Better Traders Guild] GenStep_CargoHoldVault: Spawned 4 corner Gauss cannons");
        }

        /// <summary>
        /// Draws a 3-wide bridge of terrain connecting the vault to an external platform.
        /// </summary>
        private void DrawBridge(
            Map map,
            TerrainDef terrain,
            CellRect vaultRect,
            CellRect platformRect,
            Rot4 direction)
        {
            int bridgeHalfWidth = BRIDGE_WIDTH / 2;

            if (direction == Rot4.North)
            {
                // Vertical bridge from vault top to platform bottom
                int centerX = vaultRect.CenterCell.x;
                int startZ = vaultRect.maxZ + 1;
                int endZ = platformRect.minZ - 1;

                for (int z = startZ; z <= endZ; z++)
                {
                    for (int x = centerX - bridgeHalfWidth; x <= centerX + bridgeHalfWidth; x++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        if (cell.InBounds(map))
                        {
                            map.terrainGrid.SetTerrain(cell, terrain);
                        }
                    }
                }
            }
            else if (direction == Rot4.South)
            {
                // Vertical bridge from vault bottom to platform top
                int centerX = vaultRect.CenterCell.x;
                int startZ = platformRect.maxZ + 1;
                int endZ = vaultRect.minZ - 1;

                for (int z = startZ; z <= endZ; z++)
                {
                    for (int x = centerX - bridgeHalfWidth; x <= centerX + bridgeHalfWidth; x++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        if (cell.InBounds(map))
                        {
                            map.terrainGrid.SetTerrain(cell, terrain);
                        }
                    }
                }
            }
            else if (direction == Rot4.East)
            {
                // Horizontal bridge from vault right to platform left
                int centerZ = vaultRect.CenterCell.z;
                int startX = vaultRect.maxX + 1;
                int endX = platformRect.minX - 1;

                for (int x = startX; x <= endX; x++)
                {
                    for (int z = centerZ - bridgeHalfWidth; z <= centerZ + bridgeHalfWidth; z++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        if (cell.InBounds(map))
                        {
                            map.terrainGrid.SetTerrain(cell, terrain);
                        }
                    }
                }
            }
            else if (direction == Rot4.West)
            {
                // Horizontal bridge from vault left to platform right
                int centerZ = vaultRect.CenterCell.z;
                int startX = platformRect.maxX + 1;
                int endX = vaultRect.minX - 1;

                for (int x = startX; x <= endX; x++)
                {
                    for (int z = centerZ - bridgeHalfWidth; z <= centerZ + bridgeHalfWidth; z++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        if (cell.InBounds(map))
                        {
                            map.terrainGrid.SetTerrain(cell, terrain);
                        }
                    }
                }
            }
        }
    }
}
