# RimWorld Styling System Research Report

## Summary

RimWorld's floor and furniture coloring/styling system is **VANILLA (Core game)**, though it was significantly expanded by the **Ideology DLC**. Styling is NOT an Ideology-exclusive feature, but Ideology adds many new styled floor and furniture options.

---

## 1. Styling System Overview

### Vanilla Core (Always Available)

RimWorld has had basic styling support since the Core game:
- **DrawStyleCategories**: Categories for UI/drawing purposes (lines, rectangles, etc.)
- **Basic color tags** on terrain and buildings via `<color>` tag
- **`isPaintable` property**: Allows players to paint terrain in different colors during gameplay
- **`dominantStyleCategory` tag**: Marks a building/terrain as belonging to a specific style

### Ideology Expansion (Ideology DLC)

Ideology adds a sophisticated styling system:
- **StyleCategoryDef**: Defines cultural/ideological styles (Hindu, Christian, Islamic, Morbid, Totemic, Spikecore, Techist, Animalist, Rustic, Buddhist)
- **ThingStyleDef**: Custom graphics and styling for individual objects within a style category
- **TerrainStyleDefs**: Styled floor and carpet options
- **addDesignators / addDesignatorGroups**: Adds custom floor/furniture designators when style is active

---

## 2. Key XML Tags for Styling

### TerrainDef Tags (For Floors)

| Tag | Type | Vanilla/Ideology | Description |
|-----|------|-----------------|-------------|
| `<color>` | RGB/RGBA | Both | Sets the base color of terrain. Format: `(R,G,B)` or `(R,G,B,A)` where 0-255 or 0-1 scale |
| `<isPaintable>` | bool | Both | If true, allows players to paint the terrain in different colors |
| `<dominantStyleCategory>` | string | Ideology | Links terrain to a StyleCategoryDef (e.g., "Morbid", "Totemic") |
| `<drawStyleCategory>` | string | Core | Category for UI styling (e.g., "Floors", "Walls") |
| `<designatorDropdown>` | string | Ideology | Groups multiple floor types in a dropdown menu |
| `<texturePath>` | string | Both | Path to the texture file |

### ThingDef Tags (For Floor Coverings)

```xml
<building>
  <useIdeoColor>true</useIdeoColor>  <!-- Applies ideology color to item -->
</building>
<comps>
  <li>
    <compClass>CompColorable</compClass>  <!-- Allows color customization -->
  </li>
</comps>
<dominantStyleCategory>Morbid</dominantStyleCategory>  <!-- Style category -->
```

---

## 3. Concrete Examples

### Example 1: Vanilla Steel Tile (No Ideology Required)

```xml
<TerrainDef ParentName="TileMetalBase">
  <defName>MetalTile</defName>
  <label>steel tile</label>
  <renderPrecedence>240</renderPrecedence>
  <color>(0.369, 0.369, 0.369)</color>  <!-- Dark gray -->
  <isPaintable>true</isPaintable>
  <statBases>
    <WorkToBuild>800</WorkToBuild>
    <Beauty>0</Beauty>
    <Cleanliness>0.2</Cleanliness>
  </statBases>
  <costList>
    <Steel>7</Steel>
  </costList>
</TerrainDef>
```

### Example 2: Ideology Morbid Stone Tile

```xml
<DesignatorDropdownGroupDef>
  <defName>Floor_Morbid_Stone</defName>
  <label>morbid tile</label>
</DesignatorDropdownGroupDef>

<TerrainDef ParentName="IdeoStoneTileBase" Name="MorbidStoneTile" Abstract="True">
  <description>Fine stone tiles in a morbid style.</description>
  <texturePath>Terrain/Surfaces/MorbidTile</texturePath>
  <designatorDropdown>Floor_Morbid_Stone</designatorDropdown>
  <dominantStyleCategory>Morbid</dominantStyleCategory>
</TerrainDef>

<TerrainDef ParentName="MorbidStoneTile">
  <defName>Tile_MorbidSandstone</defName>
  <label>morbid sandstone tile</label>
  <color>(147,127,118)</color>  <!-- Morbid brown -->
  <costList>
    <BlocksSandstone>20</BlocksSandstone>
  </costList>
</TerrainDef>
```

### Example 3: LayoutRoomDef with Floor Specification

From `/RimWorld/Data/Odyssey/Defs/LayoutRoomDefs/LayoutRooms_OrbitalPlatform.xml`:

```xml
<LayoutRoomDef>
  <defName>OrbitalComputerRoom</defName>
  <minSingleRectWidth>8</minSingleRectWidth>
  <minSingleRectHeight>8</minSingleRectHeight>
  <prefabs>
    <AncientSecurityTerminal>
      <countPerHundredCells>2</countPerHundredCells>
    </AncientSecurityTerminal>
  </prefabs>
  <floorTypes>
    <li>AncientTile</li>  <!-- References TerrainDef by defName -->
  </floorTypes>
</LayoutRoomDef>
```

**IMPORTANT:** You cannot specify a color directly in LayoutRoomDef. The `<floorTypes>` references a TerrainDef, which contains the color definition.

### Example 4: Floor Covering with Colorable Component

```xml
<ThingDef ParentName="BuildingFloorCoveringMediumBase">
  <defName>RusticRug_Medium</defName>
  <label>rustic rug (medium)</label>
  <dominantStyleCategory>Rustic</dominantStyleCategory>
  <graphicData>
    <graphicClass>Graphic_Random</graphicClass>
    <texPath>Things/Building/FloorCoverings/RusticRugMedium</texPath>
    <drawSize>(3,3)</drawSize>
  </graphicData>
  <building>
    <useIdeoColor>true</useIdeoColor>  <!-- Use ideology colors -->
  </building>
  <comps>
    <li>
      <compClass>CompColorable</compClass>  <!-- Player can customize color -->
    </li>
  </comps>
  <costList>
    <Cloth>100</Cloth>
  </costList>
</ThingDef>
```

---

## 4. LayoutRoomDef Styling Pattern

**CANNOT do in LayoutRoomDef:**
- Direct color override (no `<floorColor>` or `<styleColor>` tag exists)
- Material composition for floors
- Custom style categories

**CAN do in LayoutRoomDef:**
- Specify which TerrainDef to use via `<floorTypes>`
- Set edge terrain via `<edgeTerrain>`
- Use existing styled floors from any loaded mod

**Correct Pattern:**

1. Define a TerrainDef with desired color in Defs XML
2. Reference that TerrainDef's defName in LayoutRoomDef's `<floorTypes>`
3. The terrain generator applies that floor to the room

Example:
```xml
<!-- File: Defs/TerrainDefs/TradersGuild_Floors.xml -->
<TerrainDef ParentName="FloorBase">
  <defName>TradersGuild_MetalFloor_Blue</defName>
  <label>traders guild metal floor</label>
  <texturePath>Terrain/Surfaces/GenericFloorTile</texturePath>
  <color>(100, 120, 150)</color>  <!-- Sleek blue -->
  <isPaintable>true</isPaintable>
</TerrainDef>

<!-- File: Defs/LayoutRoomDefs/TradersGuild_Layouts.xml -->
<LayoutRoomDef>
  <defName>TradersGuild_CommandCenter</defName>
  <floorTypes>
    <li>TradersGuild_MetalFloor_Blue</li>  <!-- Uses the blue metal floor -->
  </floorTypes>
</LayoutRoomDef>
```

---

## 5. Color Format Specifications

### Supported Color Formats

RimWorld accepts colors in these formats:

1. **RGB (0-255 scale):**
   ```xml
   <color>(140, 140, 140)</color>
   ```

2. **RGBA (0-255 scale):**
   ```xml
   <color>(140, 140, 140, 255)</color>
   ```

3. **RGB (0-1.0 normalized scale):**
   ```xml
   <color>(0.369, 0.369, 0.369)</color>
   ```

4. **RGBA (0-1.0 normalized scale):**
   ```xml
   <pollutionColor>(1, 1, 1, 0.8)</pollutionColor>
   ```

**Note:** RimWorld automatically converts between scales; you can mix formats in the same file.

### Color Examples from Vanilla/Ideology

| Color | RGB | Use Case |
|-------|-----|----------|
| Steel/Sci-Fi | `(0.369, 0.369, 0.369)` | Modern/tech floors |
| Wood Brown | `(108, 78, 55)` | Wooden floors |
| Gold | `(0.65, 0.65, 0.35)` | Luxury floors |
| Silver | `(0.45, 0.45, 0.45)` | Premium metal |
| Morbid Brown | `(147, 127, 118)` | Dark/gloomy style |
| Totemic Brown | `(133, 97, 67)` | Wooden style |
| Concrete Gray | `(140, 140, 140)` | Industrial |

---

## 6. StyleCategoryDef (Ideology DLC)

### What It Does

StyleCategoryDef defines a complete ideological aesthetic including:
- Which buildings/furniture get styled versions
- Custom floor/carpet options
- UI icons and labels
- Sound and visual effects for rituals

### Example StyleCategoryDef (Morbid from Ideology)

```xml
<StyleCategoryDef>
  <defName>Morbid</defName>
  <label>morbid</label>
  <iconPath>UI/StyleCategories/Morbid</iconPath>
  <soundOngoingRitual>RitualSustainer_Morbid</soundOngoingRitual>
  <ritualVisualEffectDef>Morbid</ritualVisualEffectDef>
  <thingDefStyles>
    <li>
      <thingDef>Table1x2c</thingDef>
      <styleDef>Morbid_Table1x2c</styleDef>
    </li>
    <!-- Many more mappings... -->
  </thingDefStyles>
  <addDesignators>
    <li>MorbidSlab_Medium</li>
    <li>MorbidSlab_Broad</li>
  </addDesignators>
  <addDesignatorGroups>
    <li>Floor_Morbid_Stone</li>
    <li>Floor_Morbid_Carpet</li>
  </addDesignatorGroups>
</StyleCategoryDef>
```

---

## 7. File Locations Reference

### Vanilla Core Files
```
/RimWorld/Data/Core/Defs/TerrainDefs/Terrain_Floors.xml
/RimWorld/Data/Core/Defs/DrawStyleCategoryDefs/DrawStyleCategories.xml
/RimWorld/Data/Core/Defs/DrawStyleDefs/DrawStyles.xml
```

### Ideology DLC Files
```
/RimWorld/Data/Ideology/Defs/TerrainDefs/Terrain_Floors.xml
/RimWorld/Data/Ideology/Defs/StyleCategoryDefs/StyleCategoryDefs.xml
/RimWorld/Data/Ideology/Defs/ThingStyleDefs/ThingStyleDefs.xml
/RimWorld/Data/Ideology/Defs/ThingDefs_Misc/FloorCoverings.xml
```

### Odyssey DLC Examples
```
/RimWorld/Data/Odyssey/Defs/LayoutRoomDefs/LayoutRooms_OrbitalPlatform.xml
```

---

## 8. Recommendations for Better Traders Guild Mod

### Best Practice: Create Custom Colored Floors

1. **Create a TerrainDef file** for your mod:

```xml
<!-- Defs/TerrainDefs/TradersGuild_Floors.xml -->
<Defs>
  <!-- Base parent for all TradersGuild floors -->
  <TerrainDef ParentName="FloorBase" Name="TradersGuildFloorBase" Abstract="True">
    <isPaintable>true</isPaintable>
    <renderPrecedence>240</renderPrecedence>
    <statBases>
      <WorkToBuild>1000</WorkToBuild>
      <Beauty>2</Beauty>
      <Cleanliness>0.3</Cleanliness>
    </statBases>
  </TerrainDef>

  <!-- Premium traders guild metal floor -->
  <TerrainDef ParentName="TradersGuildFloorBase">
    <defName>TradersGuild_MetalFloor_Premium</defName>
    <label>premium traders guild metal floor</label>
    <texturePath>Terrain/Surfaces/GenericFloorTile</texturePath>
    <color>(100, 120, 150)</color>  <!-- Sleek blue -->
    <costList>
      <Steel>10</Steel>
    </costList>
  </TerrainDef>

  <!-- High-tech platform floor -->
  <TerrainDef ParentName="TradersGuildFloorBase">
    <defName>TradersGuild_MetalFloor_Chrome</defName>
    <label>chrome traders guild floor</label>
    <texturePath>Terrain/Surfaces/GenericFloorTile</texturePath>
    <color>(200, 210, 220)</color>  <!-- Bright silver -->
    <costList>
      <Steel>12</Steel>
    </costList>
  </TerrainDef>
</Defs>
```

2. **Reference in LayoutRoomDef**:

```xml
<!-- Defs/LayoutRoomDefs/TradersGuild_Layouts.xml -->
<LayoutRoomDef>
  <defName>TradersGuild_CommandCenter</defName>
  <requiresSingleRectRoom>true</requiresSingleRectRoom>
  <floorTypes>
    <li>TradersGuild_MetalFloor_Premium</li>
  </floorTypes>
  <!-- other properties... -->
</LayoutRoomDef>

<LayoutRoomDef>
  <defName>TradersGuild_CargoBay</defName>
  <requiresSingleRectRoom>true</requiresSingleRectRoom>
  <floorTypes>
    <li>TradersGuild_MetalFloor_Chrome</li>
  </floorTypes>
  <!-- other properties... -->
</LayoutRoomDef>
```

### Alternative: Use Existing Vanilla Floors

If you want variety, mix vanilla floors:
```xml
<floorTypes>
  <li>MetalTile</li>          <!-- Vanilla steel, always available -->
  <li>SilverTile</li>         <!-- Vanilla silver, always available -->
</floorTypes>
```

Or conditionally use Ideology floors:
```xml
<floorTypes>
  <li>MetalTile</li>                    <!-- Fallback -->
  <li MayRequire="Ludeon.RimWorld.Ideology">Tile_TotemicSandstone</li>
</floorTypes>
```

---

## 9. Key Findings Summary

| Feature | Vanilla? | Ideology? | Implementation |
|---------|----------|-----------|-----------------|
| Floor coloring | YES | Enhanced | `<color>` in TerrainDef |
| Floor styling | PARTIAL | YES | Via StyleCategoryDef |
| Paintable floors | YES | YES | `<isPaintable>true</isPaintable>` |
| Style categories | BASIC | FULL | StyleCategoryDef (Ideology DLC) |
| Colored furniture | YES | Enhanced | Via `<dominantStyleCategory>` |
| LayoutRoomDef color override | NO | NO | Use TerrainDef reference instead |
| Colorable components | YES | YES | `CompColorable` component |

---

## 10. Conclusion

**For your Better Traders Guild mod:**

1. **Floors ARE colorable in vanilla RimWorld** via TerrainDef's `<color>` tag
2. **You cannot add colors directly in LayoutRoomDef** - reference a TerrainDef instead
3. **Create custom styled floors** for your mod to give Traders Guild settlements a distinctive appearance
4. **No Ideology requirement needed** for basic colored floors
5. **Ideology compatibility** is a bonus for players who have the DLC

**Recommended approach:** Create a small set of distinctive TradersGuild floor types (sleek blue/chrome metal) and reference them in your room layouts. This gives your settlements a cohesive, futuristic aesthetic that fits the orbital trader theme.
