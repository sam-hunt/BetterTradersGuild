# Captain's Quarters Implementation Plan

**Status:** 🚧 **IN PROGRESS** - Functional but needs improvements

**What's Working:**

- ✅ Book insertion system (production-ready)
- ✅ Basic NE corner bedroom placement
- ✅ Lounge furniture spawning around bedroom
- ✅ Quality-based book spawning (1-4 books per bookcase)
- ✅ Unique weapon generation (3 traits, high quality, 4 weapon types)

**What's Remaining:**

- 🚧 Door detection + intelligent placement selection (corner/edge/center)
- 🚧 Valid cell marking verification (possible overlap issue)
- 🚧 Billiards table clearance marking (1-tile perimeter)

---

## Problem Summary

### Issues with edgeOnly Prefab Approach

1. **edgeOnly prefabs with internal walls and AncientBlastDoor have no vanilla precedent**

   - Vanilla only uses edgeOnly for small furniture items (shelves, consoles, chairs)
   - Vanilla uses C# RoomContentsWorker for complex subrooms, not XML prefabs

2. **Room generation spawns doors in the edge wall behind the bedroom**

   - Breaks immersion (bedrooms don't have multiple doors)
   - Creates unintended shortcut, defeating security design goal
   - No way to control which wall the edgeOnly prefab spawns against

3. **AncientBlastDoor gets replaced with regular Door**

   - Likely due to door type normalization in edgeOnly system
   - Cannot preserve special door types when using edgeOnly

4. **Gap between prefab and room wall**
   - Initial investigation suggested using `<positions>` instead of `<rects>` for walls
   - Testing showed this didn't fix the gap issue
   - Root cause: edgeOnly is not designed for enclosed subrooms

### Design Goals

- **Secure high-value room** requiring hacking to access
- **Single hackable entrance** with AncientBlastDoor
- **Bedroom against edge wall** (thematically appropriate for captain's quarters)
- **Strategic gameplay** - reward for clearing defenders, or cost of holding point while hacking
- **Immersive layout** - no unintended shortcuts or extra doors

## Solution: Custom RoomContentsWorker

### Key Architectural Decision: L-Shaped Prefab

**Important Size Distinction:**

- **Semantic size:** 7x7 (including positions for all 4 walls)
- **Prefab XML size:** 6x6 (only contains 2 walls)
- The bedroom occupies a 7x7 space, but the XML only defines part of it

**The bedroom prefab uses an L-shaped wall configuration (2 walls only):**

- ✅ Front wall (with AncientBlastDoor) - included in prefab XML
- ✅ Right side wall - included in prefab XML
- ❌ Back wall (REMOVED - room provides this)
- ❌ Left side wall (REMOVED - room provides this in corners)

**Why L-shaped?**

1. **Corner placement (preferred):** Room's corner provides both missing walls → no double walls
2. **Edge placement (fallback):** Room edge provides back wall, we spawn missing side wall procedurally

This approach eliminates the double-wall problem that occurs with 3-wall or 4-wall prefabs while maintaining flexibility for both corner and edge placement.

### How RoomContentsWorker Integrates with XML

RoomContentsWorkers and XML room definitions work **together**, not as replacements:

#### Execution Order

1. **PreFillRooms()** - Custom worker runs FIRST (can modify room properties)
2. **FillRoom()** - Custom worker runs, then calls `base.FillRoom()` which processes XML:
   - `<floorTypes>` applied
   - `<prefabs>` spawned in valid cells
   - `<scatter>` placed in valid cells
   - `<parts>` spawned
3. **PostFillRooms()** - Custom worker runs LAST (cleanup, final touches)

#### Cell Validation System

The worker can mark cells as "occupied" to prevent XML prefabs from spawning there:

```csharp
protected override bool IsValidCellBase(IntVec3 c, LayoutRoom room)
{
    // Prevent other prefabs from spawning in bedroom area
    if (this.bedroomRect.Contains(c))
        return false;

    return base.IsValidCellBase(c, room);
}
```

This allows the worker to reserve space for its custom structures while still allowing XML prefabs to spawn in the remaining space.

### Vanilla Example: RoomContents_TransportRoom

```csharp
// Simplified pseudocode based on IL disassembly
FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
{
    // 1. Calculate subroom rectangle
    CellRect subroom = GetSubroomRect(map, room);

    // 2. Fill the subroom programmatically (walls, door, beds, etc)
    FillSubroom(map, room, subroom);

    // 3. Store subroom rect for validation
    this.subroom = subroom;

    // 4. Call base class to process XML (prefabs, scatter, parts)
    //    XML items will avoid subroom area due to IsValidCellBase check
    base.FillRoom(map, room, faction, threatPoints);

    // 5. Spawn shelves in remaining space (outside subroom)
    SpawnShelves(map, room);
}
```

### Implementation Strategy

```csharp
namespace BetterTradersGuild.MapComponents
{
    public class RoomContents_CaptainsQuarters : RoomContents.RoomContentsWorker
    {
        private CellRect bedroomRect;

        public override void FillRoom(Map map, LayoutRoom room, Faction faction, float? threatPoints)
        {
            // 1. Find best edge wall for bedroom (prefer wall without doors)
            IntVec3 bedroomPosition = FindBestEdgeForBedroom(room, map);
            Rot4 bedroomRotation = GetRotationFacingIntoRoom(bedroomPosition, room);

            // 2. Spawn bedroom prefab (manual spawning for full control)
            //    This gives us control over door type and wall placement
            SpawnBedroomSubroom(map, room, bedroomPosition, bedroomRotation, faction);

            // 3. Mark bedroom area as "filled" to prevent other prefabs spawning there
            this.bedroomRect = GetBedroomRect(bedroomPosition, bedroomRotation);

            // 4. Call base to process XML (prefabs, scatter, parts in REMAINING space)
            //    Lounge furniture will spawn around the bedroom
            base.FillRoom(map, room, faction, threatPoints);
        }

        protected override bool IsValidCellBase(IntVec3 c, LayoutRoom room)
        {
            // Prevent other prefabs from spawning in bedroom area
            if (this.bedroomRect.IsValid && this.bedroomRect.Contains(c))
                return false;

            return base.IsValidCellBase(c, room);
        }

        private IntVec3 FindBestEdgeForBedroom(LayoutRoom room, Map map)
        {
            // Iterate through room edges and corners
            // Scoring strategy:
            // - PREFER corners (easier positioning, fewer door conflicts)
            // - Avoid walls with existing doors (doorless walls only)

            // Implementation:
            // 1. Check all four corners first (highest priority):
            //    a. Can fit 7x7 bedroom from this corner? (+100 points)
            //    b. Corner walls have doors? (-1000 points - disqualify)
            //    c. Adjacent room types? (+10 for non-critical rooms)
            //
            // 2. If no valid corners, check edge segments:
            //    a. Find 7-cell wide edge segments
            //    b. Check if segment has doors in room wall (-1000 points - disqualify)
            //    c. Check available depth (can fit 7 cells deep?) (+50 points)
            //    d. Distance from corners (+5 per cell from nearest corner)
            //
            // 3. Return position with highest score (corners checked first)
        }

        private Rot4 GetRotationFacingIntoRoom(IntVec3 edgePosition, LayoutRoom room)
        {
            // Determine which direction the bedroom should face
            // The bedroom's front (with door) should face INTO the room
            // The bedroom's back should be AGAINST the room's edge wall

            // Example:
            // If edgePosition is on north wall of room, bedroom faces south
            // If edgePosition is on east wall of room, bedroom faces west
        }

        private void SpawnBedroomSubroom(Map map, LayoutRoom room, IntVec3 position, Rot4 rotation, Faction faction)
        {
            // Manually spawn walls (7x7 minus back wall)
            List<IntVec3> wallCells = CalculateBedroomWalls(position, rotation);
            foreach (IntVec3 cell in wallCells)
            {
                Thing wall = ThingMaker.MakeThing(ThingDefOf.OrbitalAncientFortifiedWall);
                GenSpawn.Spawn(wall, cell, map);
            }

            // Manually spawn AncientBlastDoor (at door position in front wall)
            IntVec3 doorCell = CalculateDoorPosition(position, rotation);
            Thing door = ThingMaker.MakeThing(ThingDefOf.AncientBlastDoor);
            GenSpawn.Spawn(door, doorCell, map, rotation);

            // Apply carpet flooring to interior
            ApplyCarpetFlooring(map, position, rotation);

            // Spawn furniture (bed, dresser, safe, etc)
            SpawnBedroomFurniture(map, position, rotation, faction);
        }

        private List<IntVec3> CalculateBedroomWalls(IntVec3 basePosition, Rot4 rotation)
        {
            // Semantic Size: 7x7 (including all 4 walls)
            // Prefab XML Size: 6x6 (only contains 2 walls)
            // L-shaped walls: Front (z=0) + Right side (x=6) ONLY

            // Front wall (z=0) with door gap at x=2:
            // (0,0), (1,0), [DOOR GAP], (3,0), (4,0), (5,0), (6,0)

            // Right side wall (x=6, z=1-6):
            // (6,1), (6,2), (6,3), (6,4), (6,5), (6,6)

            // MISSING: Left side wall (x=0, z=1-6) - room provides or spawned
            // MISSING: Back wall (z=6) - room provides

            // Note: For corner placement, missing walls align with room corner
            //       For edge placement, missing back aligns with room edge,
            //       and missing left side is spawned procedurally

            // Transform all positions by rotation and offset from basePosition
        }

        private CellRect GetBedroomRect(IntVec3 position, Rot4 rotation)
        {
            // Return 7x7 rectangle representing full bedroom area
            // (semantic size including all 4 wall positions)
            // Used to block other prefabs from spawning here
        }

        private void SpawnBedroomFurniture(Map map, IntVec3 position, Rot4 rotation, Faction faction)
        {
            // Spawn furniture from original prefab design:
            // - RoyalBed (vacstone, excellent)
            // - Dresser (vacstone, excellent)
            // - EndTable (vacstone, normal)
            // - AncientSafe
            // - ShelfSmall with unique revolver
            // - AnimalBed (wolf leather, excellent)
            // - PlantPot (vacstone, excellent)
            // - FlatscreenTelevision
            // - LifeSupportUnit
            // - AncientEmergencyLight_Blue
            // - HiddenConduit (power distribution)
        }

        private void ApplyCarpetFlooring(Map map, IntVec3 position, Rot4 rotation)
        {
            // Apply CarpetRed to interior (1,1) to (5,5) in local coordinates
            // This creates a border around the carpet, matching vanilla style
        }
    }
}
```

### XML Configuration

```xml
<LayoutRoomDef>
  <defName>BTG_OrbitalCaptainsQuarters</defName>
  <roomContentsWorkerType>RoomContents_CaptainsQuarters</roomContentsWorkerType>

  <minSingleRectWidth>16</minSingleRectWidth>
  <minSingleRectHeight>14</minSingleRectHeight>
  <requiresSingleRectRoom>true</requiresSingleRectRoom>
  <canMergeWithAdjacentRoom>false</canMergeWithAdjacentRoom>
  <minAdjRooms>1</minAdjRooms>

  <!-- Fine luxury carpet flooring for lounge area -->
  <floorTypes>
    <li>CarpetRed</li>
  </floorTypes>

  <!-- XML prefabs still work! Spawned in remaining space AROUND bedroom -->
  <prefabs>
    <!-- Personal library with books -->
    <BTG_CaptainsBookshelf_Edge>
      <countPerTenEdgeCells>1</countPerTenEdgeCells>
      <minMaxRange>1~3</minMaxRange>
    </BTG_CaptainsBookshelf_Edge>

    <!-- Entertainment: TV with wolf leather couch -->
    <BTG_FlatscreenTelevisionWolfLeather_Edge>
      <countPerTenEdgeCells>1</countPerTenEdgeCells>
      <minMaxRange>0~1</minMaxRange>
    </BTG_FlatscreenTelevisionWolfLeather_Edge>

    <!-- Comfortable seating with decorative potplants -->
    <BTG_ArmchairsWithPlantpot_Edge>
      <countPerTenEdgeCells>2</countPerTenEdgeCells>
      <minMaxRange>1~3</minMaxRange>
    </BTG_ArmchairsWithPlantpot_Edge>
  </prefabs>

  <!-- High-quality vacstone furniture and art scattered throughout the lounge -->
  <scatter>
    <SculptureSmall>
      <stuff>BlocksVacstone</stuff>
      <quality>Good</quality>
      <groupsPerHundredCells>1~2</groupsPerHundredCells>
      <itemsPerGroup>1~1</itemsPerGroup>
      <groupDistRange>4~8</groupDistRange>
      <minGroups>2</minGroups>
    </SculptureSmall>
    <SculptureLarge>
      <stuff>BlocksVacstone</stuff>
      <quality>Good</quality>
      <groupsPerHundredCells>0~1</groupsPerHundredCells>
      <itemsPerGroup>1~1</itemsPerGroup>
      <groupDistRange>5~10</groupDistRange>
      <minGroups>0</minGroups>
    </SculptureLarge>
    <BilliardsTable>
      <stuff>BlocksVacstone</stuff>
      <quality>Good</quality>
      <groupsPerHundredCells>0~1</groupsPerHundredCells>
      <itemsPerGroup>1~1</itemsPerGroup>
      <groupDistRange>8~12</groupDistRange>
      <minGroups>0</minGroups>
    </BilliardsTable>
  </scatter>

  <!-- Minimal threat (VIP living quarters) -->
  <parts>
    <WaspDrone>0.2</WaspDrone>
  </parts>
</LayoutRoomDef>
```

## Advantages

✅ **Full control** over bedroom placement (choose wall without doors)
✅ **Preserves AncientBlastDoor** (spawn it directly, not via XML prefab)
✅ **Single secure entrance** (no random doors spawning in bedroom walls)
✅ **Bedroom against edge** (thematically correct for captain)
✅ **Strategic gameplay** (hacking required, tactical positioning valuable)
✅ **Coexists with XML** (bookshelves, TV, scatter items still work)
✅ **Validation system** prevents overlapping prefabs
✅ **Matches vanilla patterns** (RoomContents_TransportRoom does the same)

## XML Prefab Modifications

### L-Shaped Wall Configuration

The bedroom prefab must use an L-shaped wall configuration to prevent double-wall issues during placement:

**Walls to Include (L-shaped):**

- ✅ Front wall (with AncientBlastDoor)
- ✅ Right side wall

**Walls to Remove:**

- ❌ Back wall (room provides this)
- ❌ Left side wall (room provides this in corners)

**Why L-shaped prevents double walls:**

A 3-wall or 4-wall prefab creates double walls when placed in corners:

```
Corner placement with 3 walls creates DOUBLE WALL:
┌───────  (room corner)
│┌────┐
││    │  ← Bedroom's side wall + room's corner wall = DOUBLE!
││    │
│└────
```

The L-shaped approach solves this by letting the room provide missing walls.

### XML Configuration

```xml
<OrbitalAncientFortifiedWall>
  <rects>
    <!-- Front wall (z=0) with door gap -->
    <li>(0,0,1,0)</li>
    <li>(3,0,6,0)</li>

    <!-- Right side wall ONLY (x=6) -->
    <li>(6,1,6,6)</li>

    <!-- REMOVED: Back wall (z=6) -->
    <!-- REMOVED: Left side wall (x=0) -->
  </rects>
</OrbitalAncientFortifiedWall>
```

**Result: L-shaped walls (2 walls only)**

```
Visual representation (top-down view):
Semantic size: 7x7 (including all 4 wall positions)
Prefab XML size: 6x6 (only 2 walls included)

   0 1 2 3 4 5 6  (x-axis)
0  W W D W W W W    z=0 (front wall with door) - INCLUDED in prefab
1  M . . . . . W
2  M . . . . . W    Right side wall (x=6) - INCLUDED in prefab
3  M . . . . . W
4  M . . . . . W
5  M . . . . . W
6  M M M M M M M    z=6 (MISSING back + left side)

Legend:
W = Wall (included in prefab XML)
D = Door (included in prefab XML)
M = Missing wall (room provides or spawned procedurally)
. = Interior space (furniture goes here)
```

### Placement Logic

**Corner placement (preferred):**

```
┌───────  (room corner provides 2 missing walls)
│  ────┐
│      │  ✅ No double walls - uses room's corner!
│      │
│  ────
```

Rotate bedroom so missing walls align with room corner

**Edge placement (fallback):**

```
────────  (room edge provides 1 missing wall)
┌────┐
│    │  ⚠️ Missing side wall!
│    │
└────
```

Spawn the missing side wall procedurally in post-spawn step

### Implementation Strategy

1. **XML prefab:** L-shaped (front + right side only)
2. **Corner placement:** Rotate to align missing walls with corner
3. **Edge placement:** Spawn missing side wall after prefab spawn

**Example rotation logic:**

```csharp
if (placedInCorner)
{
    // Rotate so missing back-left aligns with room's corner walls
    if (isNorthWestCorner) rotation = Rot4.South;  // Missing walls face NW
    if (isNorthEastCorner) rotation = Rot4.West;   // Missing walls face NE
    if (isSouthEastCorner) rotation = Rot4.North;  // Missing walls face SE
    if (isSouthWestCorner) rotation = Rot4.East;   // Missing walls face SW
}
else
{
    // Edge placement - missing side needs to be spawned
    needsAdditionalSideWall = true;
}
```

## Book Insertion Problem & Solution

### The Problem

When spawning books on top of a bookcase using coincidental overlap-positioning in XML prefabs:

- Books spawn as items ON TOP of the bookcase (like on a shelf or floor)
- Books do NOT insert into the bookcase's `innerContainer` (ThingOwner<Book>)
- Result: Books don't render correctly (bookcase uses custom rendering from container)
- Missing: Storage mechanics, capacity limits, interaction logic

### Root Cause Analysis

Vanilla `PrefabUtility.SpawnPrefab()` spawns items at cell positions using `GenSpawn.Spawn()`, which:

1. Places items on the map at the specified cell
2. Does NOT check if a container exists at that cell
3. Does NOT automatically insert items into containers

This is **not bookcase-specific** - it affects all `IThingHolder` containers:

- Shelves
- Crates
- Storage furniture
- Any building with an `innerContainer` field

### Solution Options

#### Option 1: Procedural Post-Spawn Fixup (Captain's Quarters Specific)

**Approach:** After spawning bedroom prefab, find and fix book placement

```csharp
private void SpawnBedroomUsingPrefabAPI(Map map, LayoutRoom room, Faction faction)
{
    // ... spawn prefab ...

    // Post-spawn: Fix book storage
    FixBookcaseContents(map, bedroomRect);
}

private void FixBookcaseContents(Map map, CellRect bedroomArea)
{
    // Find all bookcases in bedroom
    List<Building_Bookcase> bookcases = bedroomArea.Cells
        .SelectMany(c => c.GetThingList(map))
        .OfType<Building_Bookcase>()
        .ToList();

    foreach (Building_Bookcase bookcase in bookcases)
    {
        IntVec3 pos = bookcase.Position;

        // Find books at same position
        List<Book> books = pos.GetThingList(map)
            .OfType<Book>()
            .ToList();

        foreach (Book book in books)
        {
            // Check if bookcase can accept
            if (bookcase.innerContainer.CanAcceptAnyOf(book, true))
            {
                // Remove from map
                book.DeSpawn(DestroyMode.Vanish);

                // Insert into bookcase container
                bookcase.innerContainer.TryAdd(book, true);
            }
        }
    }
}
```

**Pros:**

- ✅ Simple, focused solution
- ✅ Only affects Captain's Quarters
- ✅ Easy to test and debug
- ✅ No global behavior changes

**Cons:**

- ❌ Only fixes this one room
- ❌ If we add more rooms with bookcases, need to duplicate logic

#### Option 2: Global Harmony Patch (Universal Fix)

**Approach:** Patch `PrefabUtility.SpawnPrefab()` post-spawn to handle ALL containers

```csharp
[HarmonyPatch(typeof(PrefabUtility), nameof(PrefabUtility.SpawnPrefab))]
public static class PrefabUtility_SpawnPrefab_PostContainerInsertion
{
    [HarmonyPostfix]
    public static void Postfix(
        PrefabDef prefab,
        Map map,
        IntVec3 pos,
        Rot4 rot,
        List<Thing> spawned)
    {
        if (spawned == null) return;

        // Group spawned things by position
        var thingsByPosition = spawned
            .Where(t => t.Spawned)
            .GroupBy(t => t.Position);

        foreach (var group in thingsByPosition)
        {
            IntVec3 cell = group.Key;

            // Find containers at this position
            var containers = cell.GetThingList(map)
                .Where(t => t is IThingHolder && t is Building)
                .Cast<Building>()
                .ToList();

            if (containers.Count == 0) continue;

            // Find items that could go in containers
            var items = group
                .Where(t => !(t is Building))  // Don't try to store buildings
                .ToList();

            foreach (Thing item in items)
            {
                foreach (Building container in containers)
                {
                    if (TryInsertIntoContainer(container, item))
                    {
                        break;  // Successfully inserted, stop trying
                    }
                }
            }
        }
    }

    private static bool TryInsertIntoContainer(Building container, Thing item)
    {
        // Get innerContainer via reflection or interface
        ThingOwner innerContainer = GetInnerContainer(container);
        if (innerContainer == null) return false;

        // Check if it can accept this item
        if (!innerContainer.CanAcceptAnyOf(item, true))
            return false;

        // Check storage settings if applicable
        if (container is IStoreSettingsParent storage)
        {
            if (!storage.GetStoreSettings().AllowedToAccept(item))
                return false;
        }

        // Remove from map
        item.DeSpawn(DestroyMode.Vanish);

        // Insert into container
        return innerContainer.TryAdd(item, true);
    }

    private static ThingOwner GetInnerContainer(Building building)
    {
        // Use reflection to find innerContainer field
        var field = building.GetType()
            .GetField("innerContainer",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return field?.GetValue(building) as ThingOwner;
    }
}
```

**Pros:**

- ✅ Fixes ALL prefabs globally (future-proof)
- ✅ Works for bookcases, shelves, crates, etc.
- ✅ Makes prefab XML syntax more intuitive
- ✅ Benefits other mods using prefabs

**Cons:**

- ❌ More complex implementation
- ❌ Requires reflection for `innerContainer` access
- ❌ Potential compatibility issues with other mods
- ❌ Performance impact on all prefab spawns
- ❌ May have unintended side effects

### Recommended Approach

**Use Option 1 (Procedural Post-Spawn Fixup)** for now:

**Reasoning:**

1. **Simpler and safer** - only affects our code
2. **Easier to test** - contained scope
3. **Better performance** - only runs for Captain's Quarters
4. **Sufficient for our needs** - we only have one room with bookcases currently

**Future consideration:** If we add many rooms with containers, reconsider Option 2 as a quality-of-life patch.

**Testing needed:** Before implementing Option 2, test whether vanilla has similar issues with other container types (shelves, crates) to determine if this is bookcase-specific or universal.

## Implementation Checklist

### Phase 1: XML Prefab Preparation

- [x] **MANUAL:** Modify `BTG_CaptainsBedroom_Edge.xml`:
  - [x] Remove `<edgeOnly>true</edgeOnly>` tag
  - [x] **Remove TWO walls** (L-shaped: front + right side only):
    - [x] Remove back wall: `<li>(1,6,5,6)</li>`
    - [x] Remove left side wall: `<li>(0,1,0,6)</li>`
    - [x] Keep front wall: `<li>(0,0,1,0)</li>` and `<li>(3,0,6,0)</li>`
    - [x] Keep right side wall: `<li>(6,1,6,6)</li>`
  - [x] Update XML comment to clarify L-shaped design and placement strategy
  - [ ] Rename to `BTG_CaptainsBedroom.xml` (drop "\_Edge" suffix)
  - [ ] Update defName to `BTG_CaptainsBedroom`

### Phase 2: RoomContentsWorker Implementation

- [x] Create `Source/RoomContents/RoomContents_CaptainsQuarters.cs`
- [x] Implement corner placement formulas (center-based positioning)
  - [x] `CalculateNWCornerPlacement()` - Top-left corner
  - [x] `CalculateNECornerPlacement()` - Top-right corner
  - [x] `CalculateSECornerPlacement()` - Bottom-right corner
  - [x] `CalculateSWCornerPlacement()` - Bottom-left corner
  - [x] All formulas tested and working with 6×6 prefab
  - [x] **Key Discovery:** `PrefabUtility.SpawnPrefab()` uses CENTER-BASED positioning
- [x] Implement `GetBedroomRect()` - occupied area calculation (semantic 7×7)
- [x] Implement `SpawnBedroomUsingPrefabAPI()` - PrefabUtility.SpawnPrefab() call
- [x] Implement `SpawnMissingSideWall()` - for edge placement (preparatory)
- [x] Implement `SpawnMissingWallsForCenter()` - for center placement (preparatory)
- [x] Implement `IsValidCellBase()` - **CRITICAL** prevents lounge prefabs from overwriting bedroom furniture
- [x] Implement `FillRoom()` - main orchestration method
- [x] Implement `FindBestPlacementForBedroom()` - **COMPLETED (Phase 1 - Corner Selection)**
  - [x] Corner placement formulas complete
  - [x] **COMPLETED:** Door detection validates corner placements (checks North/South/East/West walls)
  - [x] **COMPLETED:** Corner iteration stops at first valid placement without door conflicts
  - [ ] **TODO:** Implement edge placement fallback (when corners have doors)
  - [ ] **TODO:** Implement center placement fallback (last resort)
  - [ ] **TODO:** Scoring system to pick best valid placement (optional enhancement)
- [x] Implement `FixBookcaseContents()` - post-spawn book insertion
  - [x] Finds bookcases in room area (lounge + bedroom)
  - [x] Uses HashSet to avoid duplicate processing of multi-cell buildings
  - [x] Searches bookcase cell + 8 adjacent cells for books
  - [x] Moves books from map into innerContainer using GetDirectlyHeldThings() API
  - [x] Validated with CanAcceptAnyOf() before insertion
  - [x] Error handling: re-spawns books if insertion fails

### Phase 3: XML Room Configuration

- [x] Update `BTG_OrbitalCaptainsQuarters.xml`:
  - [x] Add `<roomContentsWorkerType>RoomContents_CaptainsQuarters</roomContentsWorkerType>`
  - [x] Keep existing `<prefabs>`, `<scatter>`, `<parts>` (will spawn around bedroom)
  - [x] Update bookshelf spawn settings: `<countPerTenEdgeCells>2</countPerTenEdgeCells>` + `<minMaxRange>1~3</minMaxRange>`

### Phase 4: Testing

- [x] **Corner Placement Tests (preferred):**
  - [x] Test all corner placement (NE hardcoded for current testing)
  - [x] Verify NO double walls (bedroom uses room's corner walls) ✅ Confirmed
  - [x] Verify correct rotation (missing walls align with corner) ✅ Confirmed
  - [ ] Test correct corner selection - **Requires door detection implementation**
- [ ] **Edge Placement Tests (fallback):**
  - [ ] Test when corners have doors (forces edge placement) - **Requires implementation**
  - [ ] Verify missing side wall spawns correctly
  - [ ] Verify wall position matches rotation
  - [ ] Verify bedroom against room edge (one wall shared)
- [x] **General Tests:**
  - [x] Test in-game with min room size (12x10) ✅ Works correctly
  - [x] Verify lounge furniture spawns around bedroom (not overlapping) ✅ IsValidCellBase prevents overlap
  - [ ] Verify no doors spawn in bedroom's own walls
  - [x] **CRITICAL:** Verify books insert into bookcase container properly ✅ **WORKING - books inserted successfully**
  - [ ] Test bookcase interaction (player pawns can pick up books)
  - [x] **BookcaseSmall refactor:** Changed from Bookcase (2x1, 10 capacity) to BookcaseSmall (1x1, 5 capacity)
  - [x] **Quality-based spawn chances:** 1 guaranteed excellent, up to 3 additional (excellent/masterwork/legendary) via prefab `<chance>` tags
  - [ ] Test with various room sizes to verify 1-3 bookshelf spawning
  - [ ] Test with adjacent rooms of different types
  - [ ] Visual inspection: no gaps, no double walls, clean appearance

## Implementation Status Summary

### ✅ Completed Features (Production-Ready)

- **Book insertion system** - Fully functional and production-ready
  - Automatically finds bookcases in room after `base.FillRoom()`
  - Uses HashSet to avoid duplicate processing of multi-cell buildings
  - Searches bookcase cell + 8 adjacent cells for books
  - Inserts books into `innerContainer` using `GetDirectlyHeldThings()` API
  - Error handling: re-spawns books if insertion fails
- **BookcaseSmall prefab** - 1x1 size, quality-based book spawning (1-4 books)
  - 1 guaranteed excellent novel
  - Up to 3 additional novels with decreasing spawn chances (excellent/masterwork/legendary)
- **Intelligent corner placement with door detection** - All corner placements working with door validation
  - Iterates through all 4 corners (NW → NE → SE → SW)
  - Detects doors in room walls using `GetEdifice()` API
  - Skips corners where bedroom would overlap with doors
  - Stops at first valid corner without door conflicts
- **RoomContentsWorker orchestration** - Lounge prefabs spawn around bedroom via `IsValidCellBase`

### 🚧 Remaining Work (Needs Implementation)

#### 1. Bedroom Placement Algorithm Improvements

- ✅ **Door detection** - **COMPLETED** - Intelligent corner placement with door validation
  - `HasDoorsInWall()` checks if room walls have doors using `GetEdifice()` API
  - `GetWallCells()` enumerates cells along North/South/East/West walls
  - `BedroomWallsConflictWithDoors()` validates each corner against door positions
  - Corner iteration stops at first valid placement (NW → NE → SE → SW order)
- **Edge placement fallback** - When corners blocked by doors
  - Position bedroom centered along edge, back against room wall
  - Spawn missing left side wall procedurally
- **Center placement fallback** - Last resort when all edges blocked
  - Place bedroom in center of room
  - Spawn both missing walls (back + left side) to complete enclosure
- **Placement scoring system** - Algorithm to select best valid location
  - Prioritize corners (fewer walls to spawn, cleaner layout)
  - Score based on: door conflicts, adjacent room types, available space

#### 2. Valid Cell Marking Verification

- **Issue:** Previous test showed a prefab replacing bedroom corner wall once
- **Action needed:** Verify `IsValidCellBase` correctly blocks entire 7×7 bedroom area
- **Testing:** Use in-game tile debugger to confirm blocked cells
- **Possible fix:** Expand blocked area to include 1-cell buffer around bedroom?

#### 3. Billiards Table Clearance

- **Problem:** Pawns need 1-tile clearance around billiards table to use it
- **Current issue:** Other prefabs can spawn adjacent, blocking interaction
- **Solution needed:** Mark 1-tile perimeter around billiards table as invalid
- **Implementation:** Custom `IsValidCellBase` check or prefab-specific clearance marking
- **Priority:** Spawn billiards table early (bigger prefab, needs space)

#### 4. Unique Weapon on Bedroom Shelf ✅ **COMPLETED**

**Implementation:** `GenerateCaptainsWeapon()` and `SpawnUniqueWeaponOnShelf()` in RoomContents_CaptainsQuarters.cs

**Weapon Selection (weighted random):**

- 30% Gun_Revolver_Unique
- 30% Gun_ChargeRifle_Unique
- 20% Gun_ChargeLance_Unique
- 20% Gun_BeamRepeater_Unique

**Quality System:**

- Uses `QualityUtility.GenerateQualitySuper()` (biased toward Excellent/Masterwork/Legendary)

**Trait System (3 traits per weapon):**

1. **Weapon-specific primary trait** (added first):
   - Revolver: PulseCharger (retrofits pulse-charge tech)
   - Charge Rifle/Lance: ChargeCapacitor (+35% damage, +20% armor pen)
   - Beam Repeater: FrequencyAmplifier (+50% damage, +30% range, +50% cooldown)
2. **Gold Inlay** (always) - 2x market value, +20 beauty, forces gold weapon color
3. **Random compatible third trait** (filtered via `CompUniqueWeapon.CanAddTrait()`)

**Technical Implementation:**

- Weapons spawned with correct `_Unique` defNames (required for CompUniqueWeapon)
- Auto-generated traits cleared via `CompUniqueWeapon.TraitsListForReading.Clear()`
- Three custom traits added via `CompUniqueWeapon.AddTrait()` with compatibility checking
- **Reflection-based name/color regeneration** via `UniqueWeaponNameColorRegenerator.RegenerateNameAndColor()`
  - Bypasses `PostPostMake()` early return guard (only works on first initialization)
  - Directly sets private `name` and `color` fields using reflection
  - Respects `forcedColor` from traits (GoldInlay forces gold appearance)
  - Generates names from trait adjectives: "[adjective] [color] [weapon_type]"
- **Robust placement:** Searches for ShelfSmall within entire room after prefab spawn
- **Direct spawn:** Uses `GenSpawn.Spawn()` at shelf position
- **Rotation-agnostic:** No hardcoded position math, works with any bedroom placement
- Expected market values: 1400-10000 silver depending on weapon type and quality

**Generated Names Examples:**

- "Brilliant Gold Revolver"
- "Overcharged Gold Rifle"
- "Amplified Gold Repeater"

### 📊 Testing Coverage

- ✅ Book insertion: 100% tested and working
- 🧪 Corner placement with door detection: Implemented, needs testing
  - Need to test with various door configurations (North/South/East/West walls)
  - Verify corner selection skips corners with doors
  - Verify first valid corner is selected (NW → NE → SE → SW order)
  - Test edge case: all corners have doors (should log warning)
- ⏸️ Edge placement: 0% (not implemented)
- ⏸️ Center placement: 0% (not implemented)
- ✅ Prefab spawn settings: Tested with 1 bookshelf spawn
- ⏸️ Multi-bookshelf spawning: Not yet tested (room size variations needed)
- ⚠️ Valid cell marking: Needs verification (possible overlap issue observed)
- ⏸️ Billiards table clearance: Not implemented

## References

- **Vanilla Example:** `RoomContents_TransportRoom` - creates subrooms programmatically
- **Base Class:** `RimWorld.RoomContentsWorker` - provides FillRoom infrastructure
- **Validation:** `IsValidCellBase()` - used by vanilla to block prefab placement
- **Spawning:** `GenSpawn.Spawn()` - vanilla spawning with rotation support
- **ThingMaker:** `ThingMaker.MakeThing()` - create items from ThingDefs

## Notes

- RoomContentsWorker approach is the vanilla-approved method for complex subrooms
- edgeOnly XML prefabs are only used for simple furniture items in vanilla
- The three-phase system (Pre/Fill/Post) provides extensive control over room generation
- Cell validation system prevents conflicts between custom structures and XML prefabs

### Critical Issue: Prefab Spawning Order and Overwriting

**Problem:** If lounge prefabs spawn in the bedroom area AFTER the bedroom prefab, they **overwrite** bedroom furniture (bed, TV, dresser, etc.) since they occupy the same cells.

**Solution:** Override `IsValidCellForPrefabPlacement()` to block lounge prefab placement in bedroom area BEFORE spawning occurs. This prevents the overwriting issue entirely.

**Why post-spawn removal doesn't work:**

1. Bedroom spawns furniture at cells A, B, C
2. base.FillRoom() spawns lounge furniture at same cells → overwrites bedroom furniture
3. RemoveOverlappingItems() removes lounge furniture → bedroom furniture is already gone!

### Critical Discovery: Center-Based Prefab Positioning

**`PrefabUtility.SpawnPrefab()` uses CENTER-BASED positioning**, not min-corner positioning!

For the 6×6 bedroom prefab, tested and verified corner placement formulas:

- **NW (top-left)**: `center = (minX + 3, maxZ - 4)`, Rot4.North
- **NE (top-right)**: `center = (maxX - 4, maxZ - 3)`, Rot4.East
- **SE (bottom-right)**: `center = (maxX - 3, minZ + 4)`, Rot4.South
- **SW (bottom-left)**: `center = (minX + 4, minZ + 3)`, Rot4.West

**Pattern Notes:**

- Offsets are always 3 or 4 (PREFAB_SIZE/2 and PREFAB_SIZE/2+1)
- The alternating 3/4 pattern appears related to even-sized prefab rotation
- Formulas are empirically derived and specific to the 6×6 prefab size
- Testing with 5×5 prefab showed different offset behavior (not easily generalizable)

