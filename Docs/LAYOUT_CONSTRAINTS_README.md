# Layout Room Constraints System

## Overview

This document explains the custom C# system that extends RimWorld's `LayoutRoomDef` with missing constraints:
- `maxSingleRectWidth` / `maxSingleRectHeight`: Maximum room dimensions
- `maxAdjRooms`: Maximum adjacent connections (limits doors)

## Why This Is Needed

RimWorld 1.6's layout generation has these limitations:
1. ✅ Supports `minSingleRectWidth/Height` (minimum room size)
2. ✅ Supports `minAdjRooms` (minimum connections)
3. ❌ No support for **maximum** constraints in XML

This means you cannot natively limit:
- How large rooms can grow
- How many doors a room can have

## Implementation Strategy

Since we cannot modify RimWorld's core classes directly, we use **modExtensions** to store custom data and **Harmony patches** to enforce it:

###1. DefModExtension (Data Storage)

`LayoutRoomExtension.cs` stores the custom constraints:
```csharp
public class LayoutRoomExtension : DefModExtension
{
    public int maxSingleRectWidth = 999;
    public int maxSingleRectHeight = 999;
    public int maxAdjRooms = 999;
}
```

### 2. XML Definition

In your `LayoutRoomDef`:
```xml
<LayoutRoomDef>
  <defName>BTG_OrbitalCaptainsQuarters</defName>
  <minSingleRectWidth>6</minSingleRectWidth>
  <minSingleRectHeight>6</minSingleRectHeight>

  <modExtensions>
    <li Class="BetterTradersGuild.LayoutRoomExtension">
      <maxSingleRectWidth>8</maxSingleRectWidth>
      <maxSingleRectHeight>8</maxSingleRectHeight>
      <maxAdjRooms>1</maxAdjRooms>
    </li>
  </modExtensions>
</LayoutRoomDef>
```

### 3. Harmony Patch (Enforcement)

**NOTE:** The current implementation in `LayoutRoomConstraints.cs` requires access to RimWorld's internal layout structures. Due to API complexity, this approach may need adjustment based on actual available methods.

**Alternative Simpler Approach:**
If the post-processing approach doesn't work, you can:
1. Accept that vanilla doesn't support max constraints
2. Use `minSingleRectWidth/Height` to indirectly control size
3. Use `requiresSingleRectRoom=true` + `canMergeWithAdjacentRoom=false` to limit complexity

## Current Status

✅ **XML structure created** - modExtensions properly defined
✅ **C# classes written** - LayoutRoomExtension + patches
⚠️ **Compilation needed** - May require API adjustments

## Testing Required

1. Verify patch compiles with correct RimWorld API calls
2. Test in-game to confirm constraints are enforced
3. Check dev logs for constraint enforcement messages

## Difficulty Assessment

**Effort Level: Medium (3-4 hours)**

- ✅ Easy: XML modExtension setup
- ✅ Easy: DefModExtension class
- ⚠️ Medium: Finding correct Harmony patch points
- ⚠️ Medium: Testing and iteration

The main challenge is RimWorld's layout generation happens in complex, interconnected methods that aren't well-documented. You may need to:
- Use ILSpy/dnSpy to decompile Assembly-CSharp.dll
- Trace through layout generation code
- Find the exact methods that determine room size and connections
- Adjust patches accordingly

## Alternative: Accept Vanilla Limitations

If C# proves too complex, **the XML-only approach is perfectly valid**:

```xml
<LayoutRoomDef>
  <defName>BTG_OrbitalCaptainsQuarters</defName>
  <minSingleRectWidth>6</minSingleRectWidth>
  <minSingleRectHeight>6</minSingleRectHeight>
  <requiresSingleRectRoom>true</requiresSingleRectRoom>
  <canMergeWithAdjacentRoom>false</canMergeWithAdjacentRoom>
  <minAdjRooms>1</minAdjRooms>
</LayoutRoomDef>
```

This gives you:
- ✅ Minimum size enforcement
- ✅ Single rectangle shape
- ✅ No merging with neighbors
- ✅ At least 1 connection
- ⚠️ No **maximum** constraints (rooms may be larger or have more doors)

**Trade-off:** Less control, but zero C# complexity.
