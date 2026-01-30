using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.RoomContents.CargoVault;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    /// <summary>
    /// GenStep for generating the cargo vault pocket map.
    ///
    /// This GenStep uses the proper RimWorld layout system:
    /// 1. Creates StructureGenParams with vault dimensions
    /// 2. Gets the LayoutDef's Worker (LayoutWorker_CargoVault)
    /// 3. Calls GenerateStructureSketch() to create the layout
    /// 4. Calls Spawn() to place walls, floors, rooms, prefabs, and parts
    /// 5. Places sniper turret arrays on external platforms connected by bridges
    /// 6. Places Gauss cannons at vault corners
    ///
    /// The pocket map is 75x75 with the 25x25 vault structure centered,
    /// and 4 sniper turret arrays on 15x15 platforms connected by 3-wide bridges.
    /// </summary>
    public class GenStep_CargoVaultPlatform : GenStep
    {
        /// <summary>
        /// Vault structure size (walls + interior).
        /// The structure is centered on the larger pocket map.
        /// </summary>
        private const int VAULT_SIZE = 25;

        /// <summary>
        /// Turret array platform size (terrain area for each sniper turret array).
        /// </summary>
        private const int TURRET_ARRAY_PLATFORM_SIZE = 15;

        /// <summary>
        /// Bridge width connecting vault to turret array platforms.
        /// </summary>
        private const int BRIDGE_WIDTH = 3;

        /// <summary>
        /// Gap between vault edge and turret array platform edge (bridge length).
        /// </summary>
        private const int TURRET_ARRAY_GAP = 5;

        /// <summary>
        /// Temperature for roofed rooms in degrees Celsius.
        /// Defaults to 20°C (comfortable room temperature).
        /// Required for Orbit biome maps where outdoor temp is -75°C.
        /// </summary>
        public float temperature = 20f;

        /// <summary>
        /// Seed for deterministic generation.
        /// </summary>
        public override int SeedPart => 738491625;

        /// <summary>
        /// Main generation method called during pocket map creation.
        /// Uses the layout system to generate the vault structure and contents.
        /// Uses deterministic seeding based on settlement ID for consistent layout.
        /// </summary>
        public override void Generate(Map map, GenStepParams parms)
        {
            // Get settlement ID for deterministic seeding
            // Uses fallback to cached ID if settlement was defeated
            int settlementSeed = CargoVaultHelper.GetSettlementId(map);

            // Push deterministic seed based on settlement ID and SeedPart
            Rand.PushState(Gen.HashCombineInt(settlementSeed, SeedPart));
            try
            {
                GenerateInternal(map, parms);
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>
        /// Internal generation logic, called within deterministic random state.
        /// </summary>
        private void GenerateInternal(Map map, GenStepParams parms)
        {
            // Define the vault area - centered on the map
            // For a 40x40 map with 20x20 vault, this places the vault at (10,10) to (29,29)
            CellRect vaultRect = CellRect.CenteredOn(map.Center, VAULT_SIZE, VAULT_SIZE);

            // Create structure generation parameters
            StructureGenParams genParams = new StructureGenParams
            {
                size = new IntVec2(vaultRect.Width, vaultRect.Height)
            };

            // Get the layout definition - use parms.layout if provided, otherwise our default
            LayoutDef layoutDef = parms.layout ?? Layouts.BTG_OrbitalCargoVault;
            if (layoutDef == null) return;

            // Get the layout worker
            LayoutWorker worker = layoutDef.Worker;
            if (worker == null) return;

            // Generate the structure sketch (defines rooms, walls, doors, etc.)
            LayoutStructureSketch sketch = worker.GenerateStructureSketch(genParams);
            if (sketch == null)
            {
                Log.Error("[Better Traders Guild] GenStep_CargoVaultPlatform: Failed to generate structure sketch!");
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

            // Spawn sniper turret arrays on external platforms connected by bridges
            SpawnTurretArrays(map, vaultRect, tradersGuild);

            // Spawn Gauss cannons at vault corners
            SpawnCornerCannons(map, vaultRect, tradersGuild);

            // Set player start spot in the center of the vault
            MapGenerator.PlayerStartSpot = vaultRect.CenterCell;
        }

        /// <summary>
        /// Spawns 4 sniper turret arrays at cardinal directions from the vault,
        /// each on its own platform connected by a 3-wide bridge.
        /// </summary>
        private void SpawnTurretArrays(Map map, CellRect vaultRect, Faction faction)
        {
            PrefabDef turretArrayPrefab = Prefabs.BTG_PoweredSniperTurretArray;
            if (turretArrayPrefab == null)
            {
                Log.Warning("[Better Traders Guild] BTG_PoweredSniperTurretArray prefab not found, skipping turret arrays");
                return;
            }

            TerrainDef bridgeTerrain = TerrainDefOf.OrbitalPlatform;
            int vaultCenterX = vaultRect.CenterCell.x;
            int vaultCenterZ = vaultRect.CenterCell.z;

            // North turret array
            SpawnTurretArrayWithBridge(
                map, turretArrayPrefab, bridgeTerrain, faction,
                new IntVec3(vaultCenterX, 0, vaultRect.maxZ + TURRET_ARRAY_GAP + TURRET_ARRAY_PLATFORM_SIZE / 2 + 1),
                Rot4.North, vaultRect);

            // South turret array
            SpawnTurretArrayWithBridge(
                map, turretArrayPrefab, bridgeTerrain, faction,
                new IntVec3(vaultCenterX, 0, vaultRect.minZ - TURRET_ARRAY_GAP - TURRET_ARRAY_PLATFORM_SIZE / 2 - 1),
                Rot4.South, vaultRect);

            // East turret array
            SpawnTurretArrayWithBridge(
                map, turretArrayPrefab, bridgeTerrain, faction,
                new IntVec3(vaultRect.maxX + TURRET_ARRAY_GAP + TURRET_ARRAY_PLATFORM_SIZE / 2 + 1, 0, vaultCenterZ),
                Rot4.East, vaultRect);

            // West turret array
            SpawnTurretArrayWithBridge(
                map, turretArrayPrefab, bridgeTerrain, faction,
                new IntVec3(vaultRect.minX - TURRET_ARRAY_GAP - TURRET_ARRAY_PLATFORM_SIZE / 2 - 1, 0, vaultCenterZ),
                Rot4.West, vaultRect);

        }

        /// <summary>
        /// Spawns a sniper turret array on its own platform at the given center position,
        /// and draws a bridge connecting it to the vault.
        /// </summary>
        private void SpawnTurretArrayWithBridge(
            Map map,
            PrefabDef turretArrayPrefab,
            TerrainDef bridgeTerrain,
            Faction faction,
            IntVec3 arrayCenter,
            Rot4 direction,
            CellRect vaultRect)
        {
            // Calculate platform rect centered on the turret array position
            CellRect platformRect = CellRect.CenteredOn(arrayCenter, TURRET_ARRAY_PLATFORM_SIZE, TURRET_ARRAY_PLATFORM_SIZE);

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

            // Spawn the turret array prefab at the center of its platform
            PrefabUtility.SpawnPrefab(
                turretArrayPrefab,
                map,
                arrayCenter,
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

            // Draw bridge connecting vault to turret array platform
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
            // outerRadius = 6.9 for outer terrain (MechanoidPlatform in vanilla, OrbitalPlatform here)
            const float innerRadius = 5.9f;
            const float outerRadius = 6.9f;
            const int checkRadius = 7;  // Slightly larger than outerRadius to ensure coverage

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

        }

        /// <summary>
        /// Called after map generation completes.
        /// Sets room temperature and clears vacuum for the enclosed vault.
        /// Without this, roofed rooms on Orbit biome start at -75°C in vacuum.
        /// </summary>
        public override void PostMapInitialized(Map map, GenStepParams parms)
        {
            LayoutDef layoutDef = Layouts.BTG_OrbitalCargoVault;
            if (layoutDef != null)
            {
                MapGenUtility.SetMapRoomTemperature(map, layoutDef, temperature);
            }
        }

        /// <summary>
        /// Draws a 3-wide bridge of terrain connecting the vault to a turret array platform.
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
