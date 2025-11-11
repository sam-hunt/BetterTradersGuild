# External Platform Generation Research

## Overview

This document details the research into how RimWorld generates the **external elements** of orbital settlements - the outdoor walkable platform terrain, Gauss cannon placements, landing pads, and AncientShipBeacons. This investigation was motivated by the goal of replacing ancient/degraded aesthetic elements with modern variants to match the Better Traders Guild's theme of prosperous, maintained trading stations.

## Key Components

### 1. GenStep_OrbitalPlatform Architecture

The `GenStep_OrbitalPlatform` class (C# in RimWorld assembly) orchestrates external settlement generation:

```csharp
void Generate(Map map, GenStepParams parms)
{
    // 1. Generate main platform structure
    CellRect platformRect = GeneratePlatform(map, faction, threatPoints);

    // 2. Generate surrounding platform elements (33% ring, 33% large platforms, 33% small platforms)
    if (Rand.Chance(0.33f))
        DoRing(map, platformRect);
    else if (Rand.Chance(0.5f))
        DoLargePlatforms(map, platformRect);
    else
        DoSmallPlatforms(map, platformRect);

    // 3. Spawn Gauss cannons at corners
    SpawnCannons(map, platformRect.ExpandedBy(6));

    // 4. Spawn exterior prefabs (crashed shuttles, barricade turrets, etc.)
    SpawnExteriorPrefabs(map, platformRect.ExpandedBy(6), faction);
}
```

**Key Configuration Fields (XML-defined in GenStepDef):**

- `platformTerrain` - TerrainDef for main walkable platform (e.g., `OrbitalPlatform`)
- `cannonDef` - ThingDef for corner cannons (e.g., `GaussCannon`)
- `exteriorPrefabs` - List of PrefabDef + count ranges for external structures
- `layoutDef` - LayoutDef for internal room structure
- `orbitalDebrisDef` - Visual debris effect
- `fogOfWarColor` - Fog of war tint color
- `temperature` - Map temperature
- `spawnSentryDrones` - Whether to spawn sentry drones

### 2. Gauss Cannon Placement and Ancient Tiles

**Location:** `GenStep_OrbitalPlatform.SpawnCannons(Map map, CellRect rect)`

**How it works:**

1. Spawns one cannon at each corner of the platform (4 total)
2. For each cannon:
   - Creates a landing pad area (rect around cannon position, size = cannon size + 4)
   - **Places `AncientTile` terrain** in a 0.6 radius around the cannon center
   - Places `platformTerrain` in a 0.5 radius (platform connection)
   - Draws a line of `platformTerrain` from cannon to platform center
   - Spawns the cannon ThingDef at the corner position

**Critical Finding:** The ancient tiles around cannons are **hardcoded** to `TerrainDefOf::AncientTile` in the C# code:

```csharp
// From disassembled IL code:
ldsfld class Verse.TerrainDef RimWorld.TerrainDefOf::AncientTile
callvirt instance void class Verse.TerrainGrid::SetTerrain(valuetype Verse.IntVec3, class Verse.TerrainDef)
```

**Configuration for TradersGuild settlements:**

```xml
<GenStepDef>
  <defName>SettlementPlatform</defName>
  <order>200</order>
  <genStep Class="GenStep_OrbitalPlatform">
    <platformTerrain>OrbitalPlatform</platformTerrain>
    <useSiteFaction>true</useSiteFaction>
    <layoutDef>OrbitalSettlementPlatform</layoutDef>
    <cannonDef>GaussCannon</cannonDef>  <!-- THIS defines the cannon type -->
    <temperature>20</temperature>
  </genStep>
</GenStepDef>
```

### 3. Landing Pad Prefabs and AncientShipBeacon

**Location:** `Data/Odyssey/Defs/PrefabDefs/AncientExteriorPrefabs.xml`

Two landing pad sizes exist as prefab definitions:

**Large Landing Pad (`Exterior_AncientLaunchSite_LaunchPad_Large`):**

```xml
<PrefabDef>
  <defName>Exterior_AncientLaunchSite_LaunchPad_Large</defName>
  <size>(13,13)</size>
  <things>
    <AncientShipBeacon>
      <rects>
        <li>(0,0,0,0)</li>    <!-- 4 beacons at corners -->
        <li>(12,0,12,0)</li>
        <li>(0,12,0,12)</li>
        <li>(12,12,12,12)</li>
      </rects>
    </AncientShipBeacon>
  </things>
  <terrain>
    <AncientTile>  <!-- Border terrain -->
      <rects>
        <li>(0,0,12,0)</li>   <!-- Top edge -->
        <li>(0,1,0,12)</li>   <!-- Left edge -->
        <li>(12,1,12,12)</li> <!-- Right edge -->
        <li>(1,12,11,12)</li> <!-- Bottom edge -->
      </rects>
    </AncientTile>
    <AncientConcrete>  <!-- Interior terrain -->
      <rects>
        <li>(1,1,11,11)</li>
      </rects>
    </AncientConcrete>
  </terrain>
  <prefabs>
    <!-- Nested prefab that spawns AncientTransportPods with 30% chance -->
    <Exterior_AncientLaunchSite_LaunchPad_GridEntry>
      <positions>
        <li>(2, 0, 2)</li>  <!-- 9 grid positions -->
        <li>(6, 0, 2)</li>
        <!-- ... -->
      </positions>
    </Exterior_AncientLaunchSite_LaunchPad_GridEntry>
  </prefabs>
</PrefabDef>
```

**Small Landing Pad (`Exterior_AncientLaunchSite_LaunchPad_Small`):**
- Same structure, but 9x9 instead of 13x13
- 4 AncientShipBeacons at corners
- AncientTile border, AncientConcrete interior

**Key Insight:** AncientShipBeacon is NOT gated behind Royalty DLC - it's defined in `Data/Core/Defs/ThingDefs_Buildings/Buildings_Exotic.xml` and available in base game.

### 4. Exterior Prefabs System

**Location:** `GenStep_OrbitalPlatform.SpawnExteriorPrefabs(Map map, CellRect rect, Faction faction)`

**How it works:**

1. Iterates through each `PrefabRange` in the `exteriorPrefabs` list
2. For each prefab type, spawns random count based on `countRange`
3. Tries to find valid spawn location:
   - Must be within the expanded platform rect
   - Must be space terrain (not platform)
   - Orients prefab toward platform center
4. Calls `PrefabUtility.SpawnPrefab()` to instantiate the prefab

**Example configuration (OrbitalAncientPlatform):**

```xml
<GenStepDef>
  <defName>OrbitalAncientPlatform</defName>
  <linkWithSite>OrbitalAncientPlatform</linkWithSite>
  <order>200</order>
  <genStep Class="GenStep_OrbitalPlatform">
    <factionDef>AncientsHostile</factionDef>
    <platformTerrain>OrbitalPlatform</platformTerrain>
    <layoutDef>OrbitalAncientPlatform</layoutDef>
    <orbitalDebrisDef>Manmade</orbitalDebrisDef>
    <temperature>20</temperature>
    <spawnSentryDrones>true</spawnSentryDrones>
    <exteriorPrefabs>
      <CrashedShuttle>0~1</CrashedShuttle>       <!-- 0-1 crashed shuttles -->
      <BaricadeTurret>2~5</BaricadeTurret>       <!-- 2-5 barricade turret emplacements -->
    </exteriorPrefabs>
  </genStep>
</GenStepDef>
```

**Notable Prefabs:**

- **CrashedShuttle** - 8x5 crashed shuttle with blood/debris filth
- **BaricadeTurret** - 6x3 barricade emplacement with 2 `AncientSecurityTurret` (75% chance each)
- **BloodBlast** - 5x5 blood and blast mark filth
- **Exterior_AncientBunker** - 11x11 bunker with sandbags and turrets
- **Exterior_OrbitalRuins*** - Various ruined structures with `AncientTile` terrain and walls

## Customization Options for Better Traders Guild

### Option 1: Patch GenStepDef (XML Override) ✅ EASY

**What we can do:**
- Override `cannonDef` to replace GaussCannon with a modern turret
- Override `exteriorPrefabs` to use modern prefab variants
- Set `spawnSentryDrones` to false (no ancient drones)

**What we CAN'T do:**
- Remove hardcoded `AncientTile` around cannons (requires Harmony patch)
- Change landing pad terrain in existing prefabs (requires new prefab definitions)

**Implementation:**

```xml
<!-- Better Traders Guild GenStepDef override -->
<GenStepDef>
  <defName>SettlementPlatform</defName>
  <genStep Class="GenStep_OrbitalPlatform">
    <platformTerrain>OrbitalPlatform</platformTerrain>
    <useSiteFaction>true</useSiteFaction>
    <layoutDef>BTG_OrbitalSettlement</layoutDef>  <!-- Our custom layout -->

    <!-- Option A: Keep Gauss cannons but accept ancient tiles -->
    <cannonDef>GaussCannon</cannonDef>

    <!-- Option B: Use different turret (e.g., Mini-turret) -->
    <!-- <cannonDef>Turret_MiniTurret</cannonDef> -->

    <!-- Option C: No cannons at all -->
    <!-- <cannonDef IsNull="True" /> -->

    <temperature>20</temperature>

    <!-- Modern exterior prefabs instead of ancient ones -->
    <exteriorPrefabs>
      <BTG_ModernShuttleBay>1~2</BTG_ModernShuttleBay>
      <BTG_DefenseTurret>2~4</BTG_DefenseTurret>
    </exteriorPrefabs>
  </genStep>
</GenStepDef>
```

### Option 2: Harmony Patch SpawnCannons() ⚠️ MODERATE DIFFICULTY

**Goal:** Replace hardcoded `AncientTile` with `OrbitalMetalTile` (or similar modern variant)

**Approach:** Transpiler patch to replace the TerrainDef reference

```csharp
[HarmonyPatch(typeof(GenStep_OrbitalPlatform), "SpawnCannons")]
public static class GenStepOrbitalPlatformSpawnCannons_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Find: ldsfld class Verse.TerrainDef RimWorld.TerrainDefOf::AncientTile
        // Replace with: ldsfld class Verse.TerrainDef CustomTerrainDefOf::OrbitalMetalTile

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].LoadsField(AccessTools.Field(typeof(TerrainDefOf), nameof(TerrainDefOf.AncientTile))))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(typeof(CustomTerrainDefOf), "OrbitalMetalTile"));
            }
        }

        return codes;
    }
}
```

**Requires:**
1. Define custom TerrainDef `OrbitalMetalTile` in XML
2. Create DefOf class for code access
3. Implement Transpiler patch (advanced Harmony usage)

### Option 3: Create Modern Landing Pad Prefabs ✅ EASY

**Goal:** Replace `AncientTile` borders with modern metal tiles, modern beacons

**Implementation:**

```xml
<!-- BTG_ModernLandingPad_Large.xml -->
<PrefabDef>
  <defName>BTG_ModernLandingPad_Large</defName>
  <size>(13,13)</size>
  <things>
    <!-- Modern ship beacons (use vanilla or create custom) -->
    <ShipLandingBeacon>  <!-- Vanilla Royalty def, or create custom -->
      <rects>
        <li>(0,0,0,0)</li>
        <li>(12,0,12,0)</li>
        <li>(0,12,0,12)</li>
        <li>(12,12,12,12)</li>
      </rects>
    </ShipLandingBeacon>

    <!-- Optional: Power conduits for modern aesthetic -->
    <PowerConduit>
      <rects>
        <li>(6,0,6,12)</li>  <!-- Vertical conduit -->
      </rects>
    </PowerConduit>
  </things>
  <terrain>
    <!-- Modern metal tile border instead of AncientTile -->
    <OrbitalMetalTile>  <!-- Custom terrain or use existing like Steel Tile -->
      <rects>
        <li>(0,0,12,0)</li>
        <li>(0,1,0,12)</li>
        <li>(12,1,12,12)</li>
        <li>(1,12,11,12)</li>
      </rects>
    </OrbitalMetalTile>

    <!-- Modern flooring interior -->
    <OrbitalPlatform>  <!-- Or SteelTile, or custom modern concrete -->
      <rects>
        <li>(1,1,11,11)</li>
      </rects>
    </OrbitalPlatform>
  </terrain>
</PrefabDef>
```

**Note:** `ShipLandingBeacon` is the player-craftable version (requires Royalty DLC). For non-Royalty compatibility, could use `AncientShipBeacon` as-is (it's base game), or create a custom modern variant by copying the def.

### Option 4: Replace Gauss Cannons Entirely ✅ EASY

**Problem:** Gauss cannons are inherently "ancient tech" aesthetic

**Solution 1: Use Modern Turrets**

```xml
<GenStepDef>
  <defName>SettlementPlatform</defName>
  <genStep Class="GenStep_OrbitalPlatform">
    <!-- Replace with autocannon turrets -->
    <cannonDef>Turret_Autocannon</cannonDef>

    <!-- Or mini-turrets for less threatening aesthetic -->
    <!-- <cannonDef>Turret_MiniTurret</cannonDef> -->
  </genStep>
</GenStepDef>
```

**Pros:**
- No Harmony patches needed
- Still provides defensive capability
- Turrets can be powered via conduits (modern aesthetic)

**Cons:**
- Turrets still spawn with `AncientTile` radius (hardcoded)
- Different balance implications (turrets vs Gauss cannons)

**Solution 2: No Cannons**

```xml
<GenStepDef>
  <defName>SettlementPlatform</defName>
  <genStep Class="GenStep_OrbitalPlatform">
    <cannonDef IsNull="True" />  <!-- Null value = no cannons -->
  </genStep>
</GenStepDef>
```

**Pros:**
- No ancient tiles spawned
- Simplifies external appearance
- Aligns with peaceful trading station theme

**Cons:**
- Removes defensive structures (may feel less realistic)
- No visual interest at platform corners

### Option 5: Custom Terrain + Painting (FUTURE) 🎨 COMPLEX

**Goal:** Allow `AncientTile` to spawn, but paint it with `BTG_OrbitalSteel` color

**Approach:** Post-processing pass after generation

```csharp
[HarmonyPatch(typeof(GenStep_OrbitalPlatform), "Generate")]
public static class GenStepOrbitalPlatformGenerate_Postfix
{
    [HarmonyPostfix]
    public static void Postfix(Map map)
    {
        // Only apply to TradersGuild settlements
        if (!TradersGuildHelper.IsTradersGuildSettlement(map.Parent as Settlement))
            return;

        // Find all AncientTile terrain
        foreach (IntVec3 cell in map.AllCells)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(cell);
            if (terrain == TerrainDefOf.AncientTile)
            {
                // Option A: Replace with modern tile
                map.terrainGrid.SetTerrain(cell, CustomTerrainDefOf.OrbitalMetalTile);

                // Option B: Apply color overlay (if terrain supports it)
                // Apply colorization to existing tile
            }
        }
    }
}
```

**Note:** RimWorld terrain doesn't natively support colorization like walls do. Would need to create multiple terrain defs with different color variants.

## Recommendations

### Immediate Implementation (Phase 3)

1. **Override `cannonDef` to null** - Simplest solution, no ancient tiles
   - XML-only change to GenStepDef
   - Aligns with peaceful trading theme
   - Can revisit later with modern turret option

2. **Create modern landing pad prefabs** - Replace ancient aesthetics
   - New PrefabDefs: `BTG_ModernLandingPad_Large`, `BTG_ModernLandingPad_Small`
   - Use `OrbitalPlatform` or custom modern metal tile for borders
   - Use `ShipLandingBeacon` (Royalty) or create custom modern beacon variant
   - Include power conduits for modern aesthetic

3. **Create modern exterior prefabs** - Replace ancient debris
   - `BTG_ModernShuttleBay` - Clean shuttle parking area with beacon
   - `BTG_DefenseTurret` - Modern turret emplacements (optional)
   - `BTG_CargoContainer` - Modern shipping containers instead of ancient crates

### Future Enhancements (Phase 4+)

1. **Harmony transpiler patch** - Replace hardcoded `AncientTile` with modern variant
   - Requires advanced Harmony knowledge
   - Allows using turrets/cannons without ancient aesthetic

2. **Custom terrain variants** - Multiple color options for metal tiles
   - Create terrain defs: `OrbitalMetalTile`, `OrbitalMetalTile_Steel`, `OrbitalMetalTile_Painted`
   - Apply color painter from `BTG_Colors.xml`

3. **Dynamic prefab selection** - Faction-aware prefab spawning
   - Patch `SpawnExteriorPrefabs` to check faction
   - Spawn modern prefabs for friendly factions, ancient for hostiles

## Technical Reference

### Key Files and Locations

**RimWorld Assembly (`Assembly-CSharp.dll`):**
- `RimWorld.GenStep_OrbitalPlatform` - Main generation class
  - `Generate()` - Entry point
  - `SpawnCannons()` - Cannon placement + ancient tiles (HARDCODED)
  - `SpawnExteriorPrefabs()` - Prefab spawning
  - `GeneratePlatform()` - Main platform structure
  - `DoRing()`, `DoLargePlatforms()`, `DoSmallPlatforms()` - Surrounding structures

**XML Definitions:**
- `Data/Odyssey/Defs/MapGeneration/SpaceMapGenerator.xml` - GenStepDef configs
- `Data/Odyssey/Defs/PrefabDefs/AncientExteriorPrefabs.xml` - Exterior prefab library
- `Data/Odyssey/Defs/LayoutDefs/Layouts_OrbitalPlatform.xml` - LayoutDef configs
- `Data/Core/Defs/ThingDefs_Buildings/Buildings_Exotic.xml` - GaussCannon, AncientShipBeacon

### Field Definitions

**GenStep_OrbitalPlatform fields (from decompiled IL):**

```csharp
public class GenStep_OrbitalPlatform : GenStep
{
    private FactionDef factionDef;
    private LayoutDef layoutDef;
    private bool useSiteFaction;
    private Nullable<float> temperature;
    private bool spawnSentryDrones;
    private ThingDef cannonDef;  // ← THE CANNON!
    private TerrainDef platformTerrain;
    private OrbitalDebrisDef orbitalDebrisDef;
    private ColorInt fogOfWarColor;
    private List<PrefabRange> exteriorPrefabs;  // ← EXTERNAL STRUCTURES!

    // Static ranges for procedural generation
    private static readonly IntRange LargeDockRange;
    private static readonly IntRange SmallPlatformRange;
    private static readonly IntRange SmallPlatformSizeRange;
    private static readonly IntRange SmallPlatformDistanceRange;
    private static readonly IntRange SizeRange;
    private static readonly IntRange LargeLandingAreaWidthRange;
    private static readonly IntRange LargeLandingAreaHeightRange;
    public static readonly IntRange LandingPadBorderLumpLengthRange;
    public static readonly IntRange LandingPadBorderLumpOffsetRange;
    private static readonly SimpleCurve SentryCountFromPointsCurve;
}
```

**PrefabRange structure:**

```csharp
public class PrefabRange
{
    public PrefabDef prefab;
    public IntRange countRange;
}
```

## Conclusion

The external generation system is **highly configurable via XML** for most aesthetic changes:
- ✅ Cannon types can be changed or removed
- ✅ Exterior prefabs can be replaced with custom modern variants
- ✅ Landing pad prefabs can be created with modern aesthetics
- ⚠️ Ancient tiles around cannons are hardcoded (requires Harmony patch to change)

**Recommended approach for Better Traders Guild:**
1. XML-only changes first (remove cannons, create modern prefabs)
2. Reassess after testing whether Harmony patch for cannon tiles is worth the complexity
3. Consider modern turrets as compromise (accept ancient tiles as "landing pad reinforcement")
