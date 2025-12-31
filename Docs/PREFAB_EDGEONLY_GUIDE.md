# PrefabDef EdgeOnly Configuration Guide

This guide explains the differences between edgeOnly and non-edgeOnly prefabs in RimWorld's PrefabDef system, and how to transform between them.

## Table of Contents

1. [What is edgeOnly?](#what-is-edgeonly)
2. [Coordinate System Differences](#coordinate-system-differences)
3. [Wall Placement Requirements](#wall-placement-requirements)
4. [Transformation Process](#transformation-process)
5. [Common Pitfalls](#common-pitfalls)
6. [Vanilla Examples](#vanilla-examples)

---

## What is edgeOnly?

The `<edgeOnly>true</edgeOnly>` flag in a PrefabDef tells RimWorld's room generation system to place the prefab **against the edge of a room**, rather than anywhere within the room's interior.

### Non-EdgeOnly Prefabs

- Can spawn anywhere inside the room
- Includes all four walls (if applicable)
- Self-contained structure
- Examples: `DiningTable_Large`, `BilliardsTable`, `PokerTable`

### EdgeOnly Prefabs

- Must spawn against a room edge/wall
- **Reuses the room's wall as its back wall**
- Typically has walls on three sides (front + two sides)
- Examples: `Couch_Edge`, `Armchairs_Edge`, `Subroom_ElectricStove`, `Subroom_Crib`

---

## Coordinate System Differences

### Critical Understanding: Z-Axis Orientation

**For edgeOnly prefabs:**

- **z=0** is the **FRONT** of the prefab (opening into the room interior)
- **z=depth-1** is the **BACK** (against the room's wall)
- The prefab is "pushed against" the room wall at its back

**For non-edgeOnly prefabs:**

- z=0 is just one edge (no special meaning)
- All sides are equal - can rotate in any direction

### Visual Example: 7x7 Bedroom Prefab

```
Non-EdgeOnly (self-contained):
   0 1 2 3 4 5 6  (x-axis)
0  W W D W W W W    W = Wall
1  W . . . . . W    D = Door
2  W . . F . . W    F = Furniture
3  W . . . . . W    . = Floor
4  W . . B . . W    B = Bed
5  W . . . . . W
6  W W W W W W W

EdgeOnly (uses room wall):
   0 1 2 3 4 5 6  (x-axis)
0  W W D W W W W    Front (z=0) - Door opens to room
1  W . . . . . W    Interior space
2  W . . F . . W
3  W . . . . . W
4  W . . B . . W
5  W . . . . . W
6  . . . . . . .    Back (z=6) - NO WALL (room provides it)
   ^             ^
   |             |
 Side          Side
 walls         walls
```

---

## Wall Placement Requirements

### Non-EdgeOnly Prefabs

Include walls on **all four sides** if creating an enclosed space:

```xml
<OrbitalAncientFortifiedWall>
  <rects>
    <!-- Front wall (z=0) -->
    <li>(0,0,1,0)</li>
    <li>(3,0,6,0)</li>

    <!-- Left side (x=0) -->
    <li>(0,1,0,6)</li>

    <!-- Right side (x=6) -->
    <li>(6,1,6,6)</li>

    <!-- Back wall (z=6) -->
    <li>(1,6,5,6)</li>  <!-- ✓ INCLUDED -->
  </rects>
</OrbitalAncientFortifiedWall>
```

### EdgeOnly Prefabs

Include walls on **three sides only** (front + two sides). **DO NOT** include back wall:

```xml
<OrbitalAncientFortifiedWall>
  <rects>
    <!-- Front wall (z=0) with door opening -->
    <li>(0,0,1,0)</li>
    <li>(3,0,6,0)</li>

    <!-- Left side (x=0) -->
    <li>(0,1,0,6)</li>

    <!-- Right side (x=6) -->
    <li>(6,1,6,6)</li>

    <!-- Back wall (z=6) - OMITTED! Room provides this. -->
  </rects>
</OrbitalAncientFortifiedWall>
```

---

## Transformation Process

### Converting Non-EdgeOnly → EdgeOnly

**Steps:**

1. Add `<edgeOnly>true</edgeOnly>` after `<defName>`
2. Remove the back wall at z=depth-1
3. **DO NOT** change any item coordinates
4. **DO NOT** change the size
5. Update documentation comments

**Example:**

```xml
<!-- BEFORE: Non-EdgeOnly -->
<PrefabDef>
  <defName>MyPrefab</defName>
  <size>(7,7)</size>
  <things>
    <Door><position>(2, 0, 0)</position></Door>
    <RoyalBed><position>(4, 0, 5)</position></RoyalBed>
    <OrbitalAncientFortifiedWall>
      <rects>
        <li>(0,0,1,0)</li>
        <li>(3,0,6,0)</li>
        <li>(0,1,0,6)</li>
        <li>(6,1,6,6)</li>
        <li>(1,6,5,6)</li>  <!-- Back wall -->
      </rects>
    </OrbitalAncientFortifiedWall>
  </things>
</PrefabDef>

<!-- AFTER: EdgeOnly -->
<PrefabDef>
  <defName>MyPrefab</defName>
  <edgeOnly>true</edgeOnly>  <!-- ADDED -->
  <size>(7,7)</size>
  <things>
    <Door><position>(2, 0, 0)</position></Door>  <!-- Same -->
    <RoyalBed><position>(4, 0, 5)</position></RoyalBed>  <!-- Same -->
    <OrbitalAncientFortifiedWall>
      <rects>
        <li>(0,0,1,0)</li>
        <li>(3,0,6,0)</li>
        <li>(0,1,0,6)</li>
        <li>(6,1,6,6)</li>
        <!-- <li>(1,6,5,6)</li> REMOVED - Room provides this wall -->
      </rects>
    </OrbitalAncientFortifiedWall>
  </things>
</PrefabDef>
```

### Converting EdgeOnly → Non-EdgeOnly

**Steps:**

1. Remove `<edgeOnly>true</edgeOnly>` line
2. Add back wall at z=depth-1
3. **DO NOT** change any item coordinates
4. **DO NOT** change the size

**Example:**

```xml
<!-- BEFORE: EdgeOnly -->
<PrefabDef>
  <defName>MyPrefab</defName>
  <edgeOnly>true</edgeOnly>
  <size>(7,7)</size>
  <things>
    <Door><position>(2, 0, 0)</position></Door>
    <RoyalBed><position>(4, 0, 5)</position></RoyalBed>
    <OrbitalAncientFortifiedWall>
      <rects>
        <li>(0,0,1,0)</li>
        <li>(3,0,6,0)</li>
        <li>(0,1,0,6)</li>
        <li>(6,1,6,6)</li>
      </rects>
    </OrbitalAncientFortifiedWall>
  </things>
</PrefabDef>

<!-- AFTER: Non-EdgeOnly -->
<PrefabDef>
  <defName>MyPrefab</defName>
  <!-- <edgeOnly>true</edgeOnly> REMOVED -->
  <size>(7,7)</size>
  <things>
    <Door><position>(2, 0, 0)</position></Door>  <!-- Same -->
    <RoyalBed><position>(4, 0, 5)</position></RoyalBed>  <!-- Same -->
    <OrbitalAncientFortifiedWall>
      <rects>
        <li>(0,0,1,0)</li>
        <li>(3,0,6,0)</li>
        <li>(0,1,0,6)</li>
        <li>(6,1,6,6)</li>
        <li>(1,6,5,6)</li>  <!-- ADDED - Self-contained back wall -->
      </rects>
    </OrbitalAncientFortifiedWall>
  </things>
</PrefabDef>
```

---

## Common Pitfalls

### ❌ WRONG: Flipping Coordinates

**DO NOT** flip z-coordinates when converting! This is the most common mistake.

```xml
<!-- WRONG APPROACH -->
<RoyalBed><position>(4, 0, 5)</position></RoyalBed>
<!-- Convert to edgeOnly, then INCORRECTLY flip: -->
<RoyalBed><position>(4, 0, 1)</position></RoyalBed>  <!-- ❌ WRONG! -->
```

**Reason:** The coordinate system doesn't change between edgeOnly and non-edgeOnly. The z-axis always points in the same direction. What changes is:

1. Whether the prefab can only spawn at edges
2. Whether the back wall is included

### ❌ WRONG: Thinking z=0 is Reserved

**DO NOT** think z=0 is "reserved" for edge positioning in edgeOnly prefabs.

```xml
<!-- WRONG ASSUMPTION -->
<!-- "Items must start at z=1 because z=0 is reserved" -->
<Door><position>(2, 0, 1)</position></Door>  <!-- ❌ WRONG! -->
```

**Correct understanding:**

- z=0 is the **front** of the prefab
- Items CAN and SHOULD be placed at z=0 if they belong there (like doors)
- The room placement system positions the entire prefab, not individual items

### ❌ WRONG: Adding Extra Walls

**DO NOT** add walls on all four sides for edgeOnly prefabs:

```xml
<!-- WRONG -->
<edgeOnly>true</edgeOnly>
<OrbitalAncientFortifiedWall>
  <rects>
    <li>(0,0,1,0)</li>  <!-- Front -->
    <li>(3,0,6,0)</li>  <!-- Front -->
    <li>(0,1,0,6)</li>  <!-- Left side -->
    <li>(6,1,6,6)</li>  <!-- Right side -->
    <li>(1,6,5,6)</li>  <!-- ❌ Back wall - WRONG! -->
  </rects>
</OrbitalAncientFortifiedWall>
```

**Result:** The prefab will silently fail to spawn because the back wall collides with the room's existing wall.

### ✓ Correct EdgeOnly Pattern

```xml
<edgeOnly>true</edgeOnly>
<OrbitalAncientFortifiedWall>
  <rects>
    <li>(0,0,1,0)</li>  <!-- Front left -->
    <li>(3,0,6,0)</li>  <!-- Front right -->
    <li>(0,1,0,6)</li>  <!-- Left side -->
    <li>(6,1,6,6)</li>  <!-- Right side -->
    <!-- No back wall - room provides it ✓ -->
  </rects>
</OrbitalAncientFortifiedWall>
```

---

## Vanilla Examples

### Small EdgeOnly: Armchairs_Edge

**File:** `Data/Odyssey/Defs/PrefabDefs/CommonRoomPrefabs.xml`

```xml
<PrefabDef>
  <defName>Armchairs_Edge</defName>
  <edgeOnly>true</edgeOnly>
  <size>(2,1)</size>
  <things>
    <Armchair>
      <positions>
        <li>(0, 0, 0)</li>  <!-- Front row -->
        <li>(1, 0, 0)</li>
      </positions>
      <relativeRotation>Opposite</relativeRotation>  <!-- Face away from wall -->
      <stuff>Leather_Plain</stuff>
    </Armchair>
  </things>
</PrefabDef>
```

**Notes:**

- Size (2,1): 2 cells wide, 1 cell deep
- Only z=0 layer (front)
- No walls (just furniture against wall)
- Chairs face into the room (away from the back wall at z=1)

### Medium EdgeOnly: Subroom_ElectricStove

**File:** `Data/Odyssey/Defs/PrefabDefs/CommonRoomPrefabs.xml`

```xml
<PrefabDef>
  <defName>Subroom_ElectricStove</defName>
  <edgeOnly>true</edgeOnly>
  <size>(5,3)</size>
  <things>
    <OrbitalAncientFortifiedWall>
      <positions>
        <!-- Front wall with door opening at (2,0,0) -->
        <li>(0, 0, 0)</li>
        <li>(1, 0, 0)</li>
        <li>(3, 0, 0)</li>
        <li>(4, 0, 0)</li>
        <!-- Left side -->
        <li>(0, 0, 1)</li>
        <li>(0, 0, 2)</li>
        <!-- Right side -->
        <li>(4, 0, 1)</li>
        <li>(4, 0, 2)</li>
        <!-- No back wall at z=2 - room provides it -->
      </positions>
    </OrbitalAncientFortifiedWall>
    <Door>
      <position>(2,0,0)</position>
      <stuff>Steel</stuff>
    </Door>
    <ElectricStove>
      <position>(2,0,2)</position>  <!-- Back of subroom, against room wall -->
    </ElectricStove>
  </things>
</PrefabDef>
```

**Notes:**

- Size (5,3): 5 cells wide, 3 cells deep
- Walls on front (z=0) and sides (x=0, x=4)
- No back wall at z=2
- Stove positioned at back (z=2), against room wall

### Large EdgeOnly: Subroom_Crib

**File:** `Data/Odyssey/Defs/PrefabDefs/CommonRoomPrefabs.xml`

```xml
<PrefabDef MayRequire="Ludeon.RimWorld.Biotech">
  <defName>Subroom_Crib</defName>
  <edgeOnly>true</edgeOnly>
  <size>(5,4)</size>
  <things>
    <OrbitalAncientFortifiedWall>
      <positions>
        <!-- Front wall (z=0) -->
        <li>(0, 0, 0)</li>
        <li>(1, 0, 0)</li>
        <li>(3, 0, 0)</li>
        <!-- Left side (x=0) -->
        <li>(0, 0, 1)</li>
        <li>(0, 0, 2)</li>
        <li>(0, 0, 3)</li>
      </positions>
    </OrbitalAncientFortifiedWall>
    <Door>
      <position>(2,0,0)</position>
      <stuff>Steel</stuff>
    </Door>
    <Crib>
      <positions>
        <li>(1, 0, 1)</li>
        <li>(3, 0, 1)</li>
        <li>(1, 0, 3)</li>  <!-- Back row, near room wall -->
        <li>(3, 0, 3)</li>
      </positions>
      <relativeRotation>Opposite</relativeRotation>
    </Crib>
    <OrbitalAncientFortifiedWall>
      <positions>
        <!-- Right side (x=4) -->
        <li>(4, 0, 0)</li>
        <li>(4, 0, 1)</li>
        <li>(4, 0, 2)</li>
        <li>(4, 0, 3)</li>
        <!-- No back wall at z=3 - room provides it -->
      </positions>
    </OrbitalAncientFortifiedWall>
  </things>
  <terrain>
    <CarpetBrownLight>
      <rects>
        <li>(0,0,4,3)</li>  <!-- Full floor coverage including z=3 -->
      </rects>
    </CarpetBrownLight>
  </terrain>
</PrefabDef>
```

**Notes:**

- Size (5,4): 5 cells wide, 4 cells deep
- Front wall at z=0 with door
- Side walls extending from z=0 to z=3
- No back wall at z=3 (last row against room wall)
- Cribs positioned throughout, including at z=3

---

## Quick Reference Checklist

### When Creating EdgeOnly Prefab

- [ ] Add `<edgeOnly>true</edgeOnly>` after `<defName>`
- [ ] Include front wall at z=0 (with door opening if needed)
- [ ] Include side walls (x=0 and x=width-1) from z=0 to z=depth-1
- [ ] **DO NOT** include back wall at z=depth-1
- [ ] Position items anywhere from z=0 to z=depth-1
- [ ] Add comment explaining coordinate system

### When Converting EdgeOnly → Non-EdgeOnly

- [ ] Remove `<edgeOnly>true</edgeOnly>` line
- [ ] Add back wall at z=depth-1
- [ ] Verify item positions are still valid
- [ ] Update documentation comments

### When Converting Non-EdgeOnly → EdgeOnly

- [ ] Add `<edgeOnly>true</edgeOnly>` after `<defName>`
- [ ] Remove back wall at z=depth-1
- [ ] **DO NOT** change item coordinates
- [ ] Update documentation comments

---

## Troubleshooting

### Problem: Prefab doesn't spawn (silent failure)

**Possible causes:**

1. Back wall included in edgeOnly prefab (collision with room wall)
2. Prefab size too large for available room edges
3. Item coordinates outside prefab bounds
4. Wall rect syntax errors

**Solution:**

1. Check logs for placement attempts (use debug logging)
2. Verify back wall is removed for edgeOnly
3. Verify all item positions are within (0,0,0) to (width-1, 0, depth-1)
4. Test with larger room sizes

### Problem: Items facing wrong direction

**Possible cause:** Misunderstanding of coordinate system orientation

**Solution:**

- Remember z=0 is front (room interior) for edgeOnly
- Use `<relativeRotation>Opposite</relativeRotation>` to face items away from back wall
- Test different rotations (Clockwise, Counterclockwise, Opposite)

---

## Additional Resources

- **Vanilla PrefabDefs:** `RimWorld/Data/*/Defs/PrefabDefs/*.xml`
- **BaseGen Documentation:** RimWorld modding wiki
- **VEF PrefabDef Export:** Use Vanilla Expanded Framework's building export tool to study existing structures

---

**Version:** 1.0
**Last Updated:** 2025-11-03
**Author:** Better Traders Guild Development Team
