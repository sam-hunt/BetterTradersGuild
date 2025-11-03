# RimWorld Styling System - Quick Reference

## Common Questions

### Is floor coloring vanilla or Ideology-gated?
**VANILLA** - but Ideology DLC significantly expands it

### Can you color floors in LayoutRoomDef directly?
**NO** - Reference a TerrainDef instead using `<floorTypes>`

### What XML tags control floor colors?
- `<color>(R,G,B)</color>` in TerrainDef - sets base color
- `<isPaintable>true</isPaintable>` - allows player painting
- `<dominantStyleCategory>StyleName</dominantStyleCategory>` - links to ideology style

### How do you specify floor colors in room definitions?

1. Create a TerrainDef with `<color>` tag in Defs XML
2. Reference that TerrainDef's `defName` in LayoutRoomDef's `<floorTypes>`
3. Example:
   ```xml
   <floorTypes>
     <li>TradersGuild_MetalFloor_Blue</li>
   </floorTypes>
   ```

---

## Concrete Examples

### Vanilla Steel Tile (always available)
```xml
<TerrainDef ParentName="TileMetalBase">
  <defName>MetalTile</defName>
  <label>steel tile</label>
  <color>(0.369, 0.369, 0.369)</color>  <!-- Dark gray -->
  <isPaintable>true</isPaintable>
</TerrainDef>
```

### Custom Traders Guild Floor
```xml
<TerrainDef ParentName="FloorBase">
  <defName>TradersGuild_MetalFloor_Blue</defName>
  <label>traders guild metal floor</label>
  <texturePath>Terrain/Surfaces/GenericFloorTile</texturePath>
  <color>(100, 120, 150)</color>  <!-- Sleek blue -->
  <isPaintable>true</isPaintable>
</TerrainDef>
```

### Using in Layout
```xml
<LayoutRoomDef>
  <defName>TradersGuild_CommandCenter</defName>
  <floorTypes>
    <li>TradersGuild_MetalFloor_Blue</li>
  </floorTypes>
</LayoutRoomDef>
```

---

## Color Formats

RimWorld accepts multiple color formats:

| Format | Example |
|--------|---------|
| **RGB (0-255)** | `<color>(140, 140, 140)</color>` |
| **RGBA (0-255)** | `<color>(140, 140, 140, 255)</color>` |
| **RGB (0-1.0)** | `<color>(0.369, 0.369, 0.369)</color>` |
| **RGBA (0-1.0)** | `<color>(1, 1, 1, 0.8)</color>` |

### Color Examples

| Material | Float (0-1.0) | RGB (0-255) |
|----------|---------------|-------------|
| **Steel/Tech** | `(0.369, 0.369, 0.369)` | `(94, 94, 94)` |
| **Wood** | - | `(108, 78, 55)` |
| **Gold** | `(0.65, 0.65, 0.35)` | `(166, 166, 89)` |
| **Silver** | `(0.45, 0.45, 0.45)` | `(115, 115, 115)` |
| **Sleek Blue** | `(0.39, 0.47, 0.59)` | `(100, 120, 150)` |
| **Chrome** | `(0.78, 0.82, 0.86)` | `(200, 210, 220)` |

---

## File Locations

### Vanilla Core
```
/RimWorld/Data/Core/Defs/TerrainDefs/Terrain_Floors.xml
```

### Ideology DLC
```
/RimWorld/Data/Ideology/Defs/TerrainDefs/Terrain_Floors.xml
/RimWorld/Data/Ideology/Defs/StyleCategoryDefs/StyleCategoryDefs.xml
/RimWorld/Data/Ideology/Defs/ThingStyleDefs/ThingStyleDefs.xml
```

### Odyssey DLC Examples
```
/RimWorld/Data/Odyssey/Defs/LayoutRoomDefs/LayoutRooms_OrbitalPlatform.xml
```

---

## Key Takeaways for Better Traders Guild Mod

1. ✅ Floors ARE colorable in vanilla RimWorld
2. ✅ No Ideology required for basic colored floors
3. ✅ Create custom TerrainDefs for distinctive guild aesthetic
4. ✅ Reference TerrainDefs by `defName` in LayoutRoomDef `<floorTypes>`
5. ✅ Use sleek blue/chrome colors to suggest high-tech orbital trading
6. ✅ Ideology is optional enhancement for style integration

### Recommendation

Create 2-3 custom floors (premium blue, chrome, etc.) and use them consistently across guild settlement layouts.
