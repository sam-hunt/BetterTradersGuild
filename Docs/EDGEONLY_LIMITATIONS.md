# EdgeOnly Prefab Limitations Research

## Summary

EdgeOnly XML prefabs are **not suitable** for creating enclosed subrooms with internal walls and special door types. This research documents the limitations discovered and how vanilla RimWorld handles similar scenarios.

## What EdgeOnly Is Designed For

EdgeOnly prefabs are designed for **small furniture items** placed against room walls:

### Vanilla EdgeOnly Examples

| PrefabDef | Size | Use Case |
|-----------|------|----------|
| `AncientLockers_Row` | 4x1 | Storage against wall |
| `IndustrialShelf_Edge` | 4x1 | Storage against wall |
| `AncientFilingCabinet_Edge` | 3x1 | Office furniture |
| `Armchairs_Edge` | 2x1 | Seating facing into room |
| `TVWithArmchairs` | 3x3 | Entertainment center |
| `AncientSecurityTerminal` | 1x1 | Computer terminal |
| `AncientSingleBed` | 2x2 | Bed against wall |

**Key characteristics:**
- Small footprint (typically ≤ 5 cells wide, ≤ 3 cells deep)
- No internal walls or doors
- Simple furniture arrangements
- Uses regular `<Door>`, never `<AncientBlastDoor>`

## What EdgeOnly Is NOT Designed For

### Subrooms with Internal Walls

Vanilla **never uses** edgeOnly XML prefabs for enclosed subrooms. Instead, vanilla uses:

1. **C# RoomContentsWorker classes** (programmatic generation)
   - `RoomContents_TransportRoom` - creates subrooms with walls and doors
   - `RoomContents_InnerCourtyardRooms` - creates interior room structures
   - Full control over wall placement, door types, rotation

2. **Non-edgeOnly XML prefabs** (if very small and simple)
   - `Subroom_ElectricStove` (5x3, edgeOnly=true) - kitchen alcove
   - `Subroom_Crib` (5x4, edgeOnly=true) - nursery alcove
   - Both use regular `<Door>`, not special door types
   - Both are small alcoves, not full enclosed rooms

## Observed Issues with EdgeOnly Subrooms

### Issue 1: Door Type Replacement

**Problem:** `<AncientBlastDoor>` specified in edgeOnly prefab gets replaced with regular `<Door>` during generation.

**Why:** EdgeOnly system may normalize door types to match room context or ensure compatibility with edge placement algorithm.

**Vanilla Evidence:** No vanilla edgeOnly prefabs use `<AncientBlastDoor>`. All use regular `<Door>` with `<stuff>Steel</stuff>`.

### Issue 2: Uncontrolled Door Spawning in Edge Walls

**Problem:** Room generation algorithm sometimes spawns doors from adjacent rooms into the edge wall that the prefab is placed against, creating unintended entrances behind the subroom.

**Example:**
```
┌─────────────────────┐
│ Captain's Quarters  │
│                     │
│  ┌─────────┐        │
│  │ Bedroom │        │
│ [D]        │        │  ← Door spawns in room edge wall
│  │        [D]       │     (behind bedroom, breaking immersion)
│  └─────────┘        │
│                     │
└─────────────────────┘
```

**Why:** EdgeOnly prefabs have no control over which specific edge wall they spawn against. The placement algorithm:
1. Finds valid edge segments
2. Randomly selects one
3. Places prefab
4. Cannot prevent room doors from being placed on that same wall

**Impact:**
- Bedrooms with multiple entrances (immersion breaking)
- Unintended shortcuts (gameplay balance issue)
- Defeats "secure room" design goal

### Issue 3: Wall-to-Wall Gap

**Problem:** Visual gap appears between prefab's back edge and the room's edge wall.

**Initial hypothesis:** Using `<rects>` instead of `<positions>` for wall definitions caused the gap.

**Testing result:** Converting to `<positions>` did not fix the gap. Root cause unclear, possibly related to how edgeOnly prefabs calculate their back-wall alignment.

**Vanilla approach:** Small edgeOnly prefabs (shelves, terminals) are 1 cell deep and don't have this issue. Larger enclosed subrooms use RoomContentsWorker, avoiding the problem entirely.

### Issue 4: Scale Limitations

**Observation:** Vanilla edgeOnly subrooms are very small:
- `Subroom_ElectricStove`: 5x3 (15 cells)
- `Subroom_Crib`: 5x4 (20 cells)

**Attempted:** `BTG_CaptainsBedroom_Edge`: 7x7 (49 cells) - 2.5x larger than largest vanilla example

**Hypothesis:** EdgeOnly system may not be designed to handle larger structures, leading to placement and validation issues.

## How Vanilla Handles Complex Subrooms

### RoomContents_TransportRoom (OrbitalTransportRoom)

**Approach:** C# RoomContentsWorker programmatically generates subroom

**XML Definition:**
```xml
<LayoutRoomDef>
  <defName>OrbitalTransportRoom</defName>
  <roomContentsWorkerType>RoomContents_TransportRoom</roomContentsWorkerType>
  <requiresSingleRectRoom>true</requiresSingleRectRoom>
  <noRoof>true</noRoof>
  <minSingleRectWidth>11</minSingleRectWidth>
  <minSingleRectHeight>11</minSingleRectHeight>
  <!-- NO prefabs element - all done in C# -->
</LayoutRoomDef>
```

**C# Implementation (simplified):**
```csharp
public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
{
    // 1. Calculate subroom rectangle
    CellRect subroom = GetSubroomRect(map, room);

    // 2. Spawn walls cell-by-cell
    foreach (IntVec3 wallCell in GetWallCells(subroom))
    {
        GenSpawn.Spawn(ThingMaker.MakeThing(WallDef), wallCell, map);
    }

    // 3. Spawn door (with full control over type and position)
    Thing door = ThingMaker.MakeThing(DoorDef);
    GenSpawn.Spawn(door, doorCell, map, doorRotation);

    // 4. Fill subroom interior
    FillSubroom(map, room, subroom);

    // 5. Mark subroom as occupied
    this.subroom = subroom;

    // 6. Process XML (shelves spawn in remaining space)
    base.FillRoom(map, room, faction, threatPoints);
}

protected override bool IsValidCellBase(IntVec3 c, LayoutRoom room)
{
    // Prevent XML prefabs from spawning in subroom
    if (this.subroom.Contains(c))
        return false;

    return base.IsValidCellBase(c, room);
}
```

**Advantages:**
- Full control over door type (can use AncientBlastDoor)
- Full control over placement (can choose which wall)
- Can avoid walls with existing doors
- No gap issues (precise cell-by-cell placement)
- Coexists with XML prefabs (remaining space)

## Compatibility with RoomContentsWorker and XML

**Key Finding:** RoomContentsWorker and XML room features are **complementary**, not mutually exclusive.

### Vanilla Examples Using Both

**AncientOrbitalCorridorDefense:**
```xml
<LayoutRoomDef ParentName="AncientOrbitalCorridor">
  <defName>AncientOrbitalCorridorDefense</defName>
  <roomContentsWorkerType>RoomContents_Orbital_Corridor</roomContentsWorkerType>

  <prefabs>  <!-- ✓ Still has XML prefabs! -->
    <CorridorBarricade>
      <countPerHundredCells>1</countPerHundredCells>
      <minMaxRange>1~3</minMaxRange>
    </CorridorBarricade>
  </prefabs>
</LayoutRoomDef>
```

**InnerCourtyard:**
```xml
<LayoutRoomDef>
  <defName>InnerCourtyard</defName>
  <roomContentsWorkerType>RoomContents_InnerCourtyard</roomContentsWorkerType>

  <floorTypes>  <!-- ✓ Still processes floor types -->
    <li>AncientTile</li>
  </floorTypes>
</LayoutRoomDef>
```

### Execution Flow

```
1. LayoutRoomDef.PreResolveContents()
   └─> RoomContentsWorker.PreFillRooms()

2. LayoutRoomDef.ResolveContents()
   ├─> RoomContentsWorker.FillRoom()
   │   ├─> Custom logic (spawn subroom, etc.)
   │   └─> base.FillRoom() ← Processes XML:
   │       ├─> Apply <floorTypes>
   │       ├─> Spawn <prefabs> (respects IsValidCellBase)
   │       ├─> Place <scatter> (respects IsValidCellBase)
   │       └─> Spawn <parts>

3. LayoutRoomDef.PostResolveContents()
   └─> RoomContentsWorker.PostFillRooms()
```

**Cell Validation:**
- Worker overrides `IsValidCellBase()` to mark occupied cells
- XML prefab placement checks `IsValidCellBase()` before spawning
- Result: Worker structures and XML prefabs coexist without overlap

## Recommendations

### When to Use EdgeOnly XML Prefabs

✅ **Use for:**
- Small furniture items (≤ 5x3 cells)
- No internal walls or doors
- Simple arrangements (shelves, consoles, beds)
- Regular door types only
- Against any available wall (no preference)

### When to Use RoomContentsWorker

✅ **Use for:**
- Enclosed subrooms with internal walls
- Special door types (AncientBlastDoor, etc.)
- Large structures (> 5x3 cells)
- Precise placement control (avoiding doors, corners)
- Complex furniture arrangements
- Need to mark occupied areas

### When to Use Non-EdgeOnly XML Prefabs

✅ **Use for:**
- Free-standing structures (not against walls)
- Self-contained rooms (4 walls)
- Can be placed anywhere in room interior
- Medium complexity

## Wall Definition Best Practices

### For EdgeOnly Prefabs

**Always use `<positions>` for walls in edgeOnly prefabs:**
```xml
<PrefabDef>
  <defName>MyEdgePrefab</defName>
  <edgeOnly>true</edgeOnly>
  <size>(5,3)</size>
  <things>
    <AncientFortifiedWall>
      <positions>  <!-- ✓ Use positions, not rects -->
        <!-- Front wall -->
        <li>(0, 0, 0)</li>
        <li>(1, 0, 0)</li>
        <!-- Door gap at (2, 0, 0) -->
        <li>(3, 0, 0)</li>
        <li>(4, 0, 0)</li>

        <!-- Side walls -->
        <li>(0, 0, 1)</li>
        <li>(0, 0, 2)</li>
        <li>(4, 0, 1)</li>
        <li>(4, 0, 2)</li>

        <!-- NO back wall at z=2 -->
      </positions>
    </AncientFortifiedWall>
  </things>
</PrefabDef>
```

**Why:** All vanilla edgeOnly prefabs with walls use `<positions>`, not `<rects>`.

### For RoomContentsWorker

**Spawn walls cell-by-cell:**
```csharp
foreach (IntVec3 wallCell in wallPositions)
{
    Thing wall = ThingMaker.MakeThing(ThingDefOf.OrbitalAncientFortifiedWall);
    GenSpawn.Spawn(wall, wallCell, map);
}
```

**Why:** Precise control over every wall cell, matching vanilla RoomContents_TransportRoom approach.

## Conclusion

EdgeOnly XML prefabs are a lightweight system for simple furniture placement. Complex subrooms with internal walls and special doors require RoomContentsWorker classes for proper control and vanilla-compatible behavior.

**For the Captain's Quarters bedroom:**
- ❌ EdgeOnly XML prefab - incompatible with design goals
- ✅ RoomContentsWorker - provides necessary control and follows vanilla patterns

## References

- See `CAPTAINS_QUARTERS_IMPLEMENTATION.md` for implementation plan
- See `PREFAB_EDGEONLY_GUIDE.md` for edgeOnly coordinate system details
- Vanilla examples:
  - `RoomContents_TransportRoom` - `/Data/Odyssey/Defs/LayoutRoomDefs/LayoutRooms_OrbitalPlatform.xml` (line 199)
  - `Subroom_ElectricStove` - `/Data/Odyssey/Defs/PrefabDefs/CommonRoomPrefabs.xml` (line 188)
  - `Subroom_Crib` - `/Data/Odyssey/Defs/PrefabDefs/CommonRoomPrefabs.xml` (line 225)
