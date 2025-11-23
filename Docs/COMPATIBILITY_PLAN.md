# Better Traders Guild × Vanilla Gravship Expanded - Compatibility Plan

**Date:** 2025-01-23
**Status:** Strategic Planning Document
**Decision:** Proceed with Phase 3 with mandatory optional features system

---

## Executive Summary

**Current Status (Chapter 1):** ✅ **COMPATIBLE** - No direct conflicts detected
**Future Risk (Chapter 3):** ⚠️ **MODERATE** - Potential conflicts if VE adds space station features
**Strategic Recommendation:** 🛡️ **PROCEED WITH CAUTION** - Make Phase 3 features optional

### Key Findings

- VE Gravship Chapter 1 focuses on gravship life support systems (oxygen, fuel, heat)
- No Harmony patch conflicts with Better Traders Guild's current implementation
- VE's roadmap includes Chapter 3 "space stations and deep space" which could conflict
- Better Traders Guild needs ~4 hours of safety work before continuing Phase 3
- Optional features system provides escape hatch for future conflicts

---

## VE Gravship Overview

### Chapter 1: Foundations (Current Release)

**Focus:** Gravship life support and management systems

**Core Systems:**

- Oxygen network (piping, scrubbers, tanks)
- Astrofuel refining and storage
- Structural heat management
- Power generation and distribution
- Gravship maintenance and upgrades
- Crew assignment systems

**Technical Scope:**

- 41.289 MB mod with extensive system overhauls
- 100+ Harmony patch files
- Custom GenStep classes for asteroid generation
- GenStep_AncientOrbitalPlatform for derelict stations
- World component for gravship controller state

**What Chapter 1 Does NOT Touch:**

- Settlement class (no patches)
- Settlement_TraderTracker (no patches)
- TradersGuild faction (no modifications)
- Existing faction settlements (no generation changes)
- Trader rotation systems (no interference)

**Compatibility Warning from VE:**

> "Any mod that modifies gravships will likely be incompatible. We're doing some pretty extensive changes to the system after all."

### Future Chapters (Roadmap)

**Chapter 2: Combat** (Next Release)

- Enemy gravships
- Orbital duels and combat
- Space warfare mechanics

**Chapter 3: Space Stations** ⚠️ **POTENTIAL CONFLICT ZONE**

- From Oskar_Potocki: "Space stations and deep space and the moon"
- No details on implementation approach
- Unknown if will modify existing faction settlements
- Unknown if will add custom layouts to TradersGuild

**Chapter 4: Deep Space**

- Retrofitting gravship into starship
- Journey into the stars
- Furthest from current scope

---

## Better Traders Guild - Current Implementation

### Phase 2 (Completed - No Conflicts)

**Patches Applied:**

- `Settlement.Visitable` - Enables peaceful visits
- `Settlement_TraderTracker.GetTraderKind` - Dynamic trader rotation
- `Settlement_TraderTracker.RegenerateStock` - Mid-regeneration flag
- `Settlement_TraderTracker.RegenerateStockAlignment` - Virtual schedule alignment
- `Settlement_TraderTracker.RegenerateStockEveryDays` - Custom rotation interval
- `Settlement.GetGizmos` - Visit buttons
- `Settlement.GetFloatMenuOptions` - Trade float menu
- `Settlement.GetInspectString` - Docked vessel display
- `WorldObject.RequiresSignalJammerToReach` - Signal jammer override
- `WorldGrid` methods - Caravan pathfinding to space

**Status:** ✅ Rock-solid, no VE conflicts

### Phase 3 (In Progress - Requires Safety Measures)

**Completed:**

- ✅ Phase 3.1: Custom BTG_OrbitalSettlement LayoutDef (18 room types)
- ✅ Phase 3.2: GenStep_OrbitalPlatform.Generate() patch with reflection

**Planned:**

- 🔮 Phase 3.3: Enhanced Pawn Generation
- 🔮 Phase 3.4: Dynamic Cargo Spawning (SymbolResolver)
- 🔮 Phase 3.5: Cargo Refresh System (MapComponent)

**Current Implementation Details:**

```csharp
// GenStep_OrbitalPlatform patch uses REFLECTION
[HarmonyPrefix]
public static bool Prefix(GenStep_OrbitalPlatform __instance, Map map, ...)
{
    if (IsTradersGuildSettlement(map))
    {
        // Modify private layoutDef field via reflection
        FieldInfo layoutField = AccessTools.Field(typeof(GenStep_OrbitalPlatform), "layoutDef");
        layoutField.SetValue(__instance, customLayout);
    }
    return true; // Let vanilla Generate() run with modified layout
}
```

**Components Added:**

- `TradersGuildSettlementComponent` (WorldObjectComp)
  - Tracks `lastCargoRefreshTicks` for cargo system
  - Added to settlements during map generation
  - ⚠️ **Save compatibility risk if mod removed**

---

## Current Compatibility Assessment (Chapter 1)

### ✅ NO DIRECT CONFLICTS

**Reason 1: Different Code Paths**

VE Gravship uses **custom GenStep class**:

```csharp
// VE creates NEW GenStep, doesn't patch vanilla
public class GenStep_AncientOrbitalPlatform : GenStep_OrbitalPlatform
{
    // Used for derelict station generation
}
```

Better Traders Guild uses **Harmony patch**:

```csharp
// BTG patches vanilla GenStep_OrbitalPlatform
[HarmonyPatch(typeof(GenStep_OrbitalPlatform), "Generate")]
public static class GenStepOrbitalPlatformGenerate { ... }
```

**These operate independently:**

- VE's GenStep handles derelict stations (custom MapGeneratorDef)
- BTG's patch handles TradersGuild settlements only
- No overlap in execution paths

**Reason 2: No Settlement Patches**

VE Chapter 1 does NOT patch:

- Settlement class
- Settlement_TraderTracker
- Any faction definitions
- Trader rotation logic

**Reason 3: Faction Scope Separation**

- VE: Modifies gravship mechanics (system-wide)
- BTG: Modifies TradersGuild faction only (faction-specific)
- Different design philosophies, different scopes

### Load Order Considerations

**Current State:**

- BTG does not specify VE in `loadAfter`
- No explicit incompatibility declared
- Both mods can coexist

**Recommendation:**

```xml
<!-- Add to About.xml if conflicts emerge -->
<loadAfter>
  <li>vanillaexpanded.gravship</li>
</loadAfter>
```

This ensures BTG patches run after VE, giving BTG's faction-specific logic priority.

---

## Future Conflict Scenarios (Chapter 3)

### ⚠️ MODERATE RISK - HIGH UNCERTAINTY

**What We Know:**

- VE plans "space stations and deep space" content
- VE About.xml mentions "orbital fortresses"
- No public details on implementation
- VE follows "live service" development model (chapters release over time)

**What We Don't Know:**

- Whether VE will modify TradersGuild settlements
- Whether VE will add custom layouts to existing factions
- Whether VE will patch Settlement_TraderTracker
- Timeline for Chapter 3 release

### Scenario 1: VE Adds Space Station Generation for All Factions

**Risk Level:** 🔴 HIGH

**Conflict Type:** Harmony Patch Competition

**What Could Happen:**

```
BTG Prefix: Modifies layoutDef via reflection → vanilla runs
VE Patch:   Also tries to override layout → CONFLICT
```

**Impact:**

- Last-loaded mod wins (load order determines behavior)
- BTG's custom layout could be overridden by VE's system
- Unpredictable behavior if both modify same settlement
- Player confusion about which mod is active

**Mitigation Strategy:**

- Make BTG's GenStep patch optional via mod settings
- Add VE Gravship detection: skip patch if VE active + user chooses VE
- Log compatibility mode activation
- Document load order recommendations

**Code Example:**

```csharp
if (ModCompatibility.IsVEGravshipActive && !Settings.useCustomLayouts)
{
    Log.Message("[BTG] VE Gravship detected, skipping custom layout");
    return true; // Let VE handle it
}
```

### Scenario 2: VE Adds Trader Rotation Patches

**Risk Level:** 🟡 MEDIUM

**Conflict Type:** TraderTracker Patch Overlap

**What Could Happen:**

```
BTG: Settlement_TraderTracker.GetTraderKind (orbital rotation)
VE:  Could patch same method for gravship docking logic
```

**Impact:**

- Multiple postfixes usually chain safely
- Risk if VE uses prefix with `return false`
- Virtual schedule system could break
- Inconsistent trader types between mods

**Mitigation Strategy:**

- Make trader rotation system optional
- Fallback to vanilla behavior if both active
- Test patch execution order
- Coordinate with VE team if conflicts detected

### Scenario 3: VE Adds TradersGuild-Specific Features

**Risk Level:** 🟠 LOW PROBABILITY, HIGH IMPACT

**Conflict Type:** Direct Feature Overlap

**What Could Happen:**

- Both mods enhance same faction
- Competing WorldObjectComps on settlements
- Duplicate UI elements (buttons, inspection strings)
- Conflicting gameplay mechanics

**Impact:**

- Feature duplication confuses players
- Save incompatibility if both add components
- Broken state if components conflict
- Steam Workshop complaints

**Mitigation Strategy:**

- Establish communication with VE team
- Consider partnership or feature coordination
- Offer to contribute TradersGuild enhancements to VE
- Clearly document feature ownership in Steam description

---

## Save Compatibility Concerns

### 🔴 HIGH RISK: TradersGuildSettlementComponent

**Component Structure:**

```csharp
public class TradersGuildSettlementComponent : WorldObjectComp
{
    private long lastCargoRefreshTicks = -1;

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref lastCargoRefreshTicks, "lastCargoRefreshTicks", -1L);
    }
}
```

**Risk:** Component added to Settlement.AllComps during generation

**Save Corruption Scenario:**

```
1. Player generates TradersGuild settlement with BTG active
2. TradersGuildSettlementComponent added, data saved
3. Player removes BTG mod
4. Load game → Scribe can't find component type → ERROR
```

**Severity:** Can cause save loading failures

**Mitigation:**

- Make component addition optional via mod settings
- Only add if `useDynamicCargo` enabled
- Add graceful null checks everywhere component accessed
- Document removal risks in mod description

**Safe Component Access Pattern:**

```csharp
public static TradersGuildSettlementComponent GetComponentSafe(Settlement settlement)
{
    try
    {
        return settlement?.GetComponent<TradersGuildSettlementComponent>();
    }
    catch (Exception ex)
    {
        Log.Warning($"[BTG] Component access failed: {ex.Message}");
        return null;
    }
}

// Usage with null safety
var component = GetComponentSafe(settlement);
if (component != null && component.ShouldRefreshCargo())
{
    RefreshCargo(settlement);
}
```

### 🟢 LOW RISK: Custom LayoutDef

**How It Works:**

1. Map generated with BTG_OrbitalSettlement layout
2. Map saved (contains concrete objects, not layout def reference)
3. Player removes BTG mod
4. Revisit map → Loads fine (objects already spawned and saved)

**Why It's Safe:**

- Maps are generated once and saved
- Layout defs used during generation only
- Generated objects persist independently
- No runtime dependency on mod

**Severity:** Minimal - maps persist after generation

### 🟢 VERY LOW RISK: Reflection-Based Field Modification

**What It Does:**

```csharp
FieldInfo layoutField = AccessTools.Field(typeof(GenStep_OrbitalPlatform), "layoutDef");
layoutField.SetValue(__instance, customLayout);
```

**Why It's Safe:**

- Reflection happens during generation only
- One-time operation, no persistent state
- No data saved to disk
- Vanilla takes over after field modified

**Severity:** None after generation completes

---

## Implementation Plan: Optional Features System

**Total Estimated Time:** ~4 hours
**Priority:** 🔥 **BLOCKING** - Must complete before Phase 3.3

### Phase 1: Mod Settings Infrastructure (1-2 hours)

**File:** `Source/Core/ModSettings.cs`

**Add Feature Toggles:**

```csharp
public class BetterTradersGuildSettings : ModSettings
{
    // Existing settings
    public int traderRotationIntervalDays = 15;
    public float cargoInventoryPercentage = 0.60f;

    // NEW: Optional features
    public bool useCustomLayouts = true;     // Phase 3.1-3.2
    public bool useDynamicCargo = true;      // Phase 3.4-3.5
    public bool autoDisableOnConflict = true; // Safety toggle

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref traderRotationIntervalDays, "traderRotationIntervalDays", 15);
        Scribe_Values.Look(ref cargoInventoryPercentage, "cargoInventoryPercentage", 0.60f);

        // NEW
        Scribe_Values.Look(ref useCustomLayouts, "useCustomLayouts", true);
        Scribe_Values.Look(ref useDynamicCargo, "useDynamicCargo", true);
        Scribe_Values.Look(ref autoDisableOnConflict, "autoDisableOnConflict", true);
    }
}
```

**Add Settings UI:**

```csharp
public override void DoSettingsWindowContents(Rect inRect)
{
    Listing_Standard listingStandard = new Listing_Standard();
    listingStandard.Begin(inRect);

    // Core Features section
    listingStandard.Label("Core Features");
    listingStandard.Gap();

    // Trader rotation slider (existing)
    // ...

    listingStandard.Gap(24f);

    // NEW: Experimental Features section
    listingStandard.Label("Experimental Features");
    listingStandard.CheckboxLabeled(
        "Custom settlement layouts",
        ref Settings.useCustomLayouts,
        "TradersGuild bases use modern merchant aesthetics. May conflict with map generation mods."
    );

    // Dynamic cargo depends on custom layouts
    GUI.enabled = Settings.useCustomLayouts;
    listingStandard.CheckboxLabeled(
        "Dynamic cargo bay system",
        ref Settings.useDynamicCargo,
        "Shuttle bay cargo changes with trader rotation. Requires custom layouts."
    );
    GUI.enabled = true;

    // Cargo percentage slider (only if dynamic cargo enabled)
    if (Settings.useDynamicCargo)
    {
        // Existing cargo percentage slider
        // ...
    }

    listingStandard.Gap(24f);

    // Compatibility section
    listingStandard.Label("Compatibility");
    listingStandard.CheckboxLabeled(
        "Auto-disable features on conflict",
        ref Settings.autoDisableOnConflict,
        "Automatically skip problematic patches if incompatible mods detected."
    );

    // Show detected mods
    if (ModCompatibility.IsVEGravshipActive)
    {
        listingStandard.Label("Detected: Vanilla Gravship Expanded (Chapter 1)");
        if (!Settings.useCustomLayouts)
        {
            listingStandard.Label("  → Custom layouts disabled", -1f, "Using vanilla generation");
        }
    }

    listingStandard.End();
}
```

### Phase 2: VE Detection & Compatibility (30 minutes)

**New File:** `Source/Helpers/ModCompatibility.cs`

```csharp
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Detects and handles compatibility with other mods.
    /// </summary>
    public static class ModCompatibility
    {
        private static bool? _veGravshipActive = null;

        /// <summary>
        /// Checks if Vanilla Gravship Expanded is active.
        /// </summary>
        public static bool IsVEGravshipActive
        {
            get
            {
                if (_veGravshipActive == null)
                {
                    _veGravshipActive = ModsConfig.IsActive("vanillaexpanded.gravship");
                    if (_veGravshipActive.Value)
                    {
                        Log.Message("[Better Traders Guild] Vanilla Gravship Expanded detected");
                    }
                }
                return _veGravshipActive.Value;
            }
        }

        /// <summary>
        /// Checks if any conflicting mod is active.
        /// </summary>
        public static bool HasConflictingMod
        {
            get
            {
                // Currently no known conflicts
                // Future: Add checks for other space station mods
                return false;
            }
        }
    }
}
```

**New File:** `Source/Helpers/FeatureFlags.cs`

```csharp
using BetterTradersGuild.Core;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Centralized feature enablement checks.
    /// Combines mod settings with compatibility detection.
    /// </summary>
    public static class FeatureFlags
    {
        /// <summary>
        /// Custom settlement layouts enabled?
        /// </summary>
        public static bool CustomLayoutsEnabled
        {
            get
            {
                if (!BetterTradersGuildMod.Settings.useCustomLayouts)
                    return false;

                // If auto-disable on conflict enabled and conflict detected
                if (BetterTradersGuildMod.Settings.autoDisableOnConflict && ModCompatibility.HasConflictingMod)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Dynamic cargo system enabled?
        /// Requires custom layouts.
        /// </summary>
        public static bool DynamicCargoEnabled
        {
            get
            {
                if (!CustomLayoutsEnabled)
                    return false;

                return BetterTradersGuildMod.Settings.useDynamicCargo;
            }
        }
    }
}
```

### Phase 3: Make GenStep Patch Optional (1 hour)

**File:** `Source/Patches/MapGeneration/GenStepOrbitalPlatformGenerate.cs`

**Update Prefix Method:**

```csharp
[HarmonyPrefix]
public static bool Prefix(GenStep_OrbitalPlatform __instance, Map map, GenStepParams parms)
{
    // Check if custom layouts feature enabled
    if (!FeatureFlags.CustomLayoutsEnabled)
    {
        // Feature disabled - use vanilla generation
        return true;
    }

    // Rest of existing logic...
    if (!IsTradersGuildSettlement(map))
        return true;

    // Check for VE Gravship compatibility mode
    if (ModCompatibility.IsVEGravshipActive && BetterTradersGuildMod.Settings.autoDisableOnConflict)
    {
        Log.Message("[Better Traders Guild] VE Gravship detected with auto-disable - using vanilla generation");
        return true;
    }

    // Modify layoutDef via reflection (existing code)
    // ...
}
```

**Update Component Addition:**

**File:** `Source/Patches/MapGeneration/GenStepOrbitalPlatformGenerate.cs`

```csharp
private static void AddSettlementComponent(Settlement settlement)
{
    // Only add component if dynamic cargo enabled
    if (!FeatureFlags.DynamicCargoEnabled)
    {
        Log.Message($"[Better Traders Guild] Dynamic cargo disabled - skipping component for {settlement.Name}");
        return;
    }

    // Check if component already exists
    if (settlement.GetComponent<TradersGuildSettlementComponent>() != null)
        return;

    // Add component
    var component = new TradersGuildSettlementComponent();
    settlement.AllComps.Add(component);
    component.parent = settlement;

    Log.Message($"[Better Traders Guild] Added cargo tracking component to {settlement.Name}");
}
```

**Add Null-Safe Component Access:**

**New File:** `Source/Helpers/ComponentHelper.cs`

```csharp
using RimWorld.Planet;
using BetterTradersGuild.WorldObjects;
using Verse;

namespace BetterTradersGuild.Helpers
{
    /// <summary>
    /// Safe component access helpers.
    /// </summary>
    public static class ComponentHelper
    {
        /// <summary>
        /// Gets TradersGuild component safely.
        /// Returns null if component not found or feature disabled.
        /// </summary>
        public static TradersGuildSettlementComponent GetComponentSafe(Settlement settlement)
        {
            if (settlement == null)
                return null;

            if (!FeatureFlags.DynamicCargoEnabled)
                return null;

            try
            {
                return settlement.GetComponent<TradersGuildSettlementComponent>();
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Better Traders Guild] Failed to get component from {settlement.Name}: {ex.Message}");
                return null;
            }
        }
    }
}
```

### Phase 4: Documentation & Testing (30 minutes)

**Update Files:**

1. **CLAUDE.md** - Add compatibility section:

   - Note about VE Gravship detection
   - Explanation of optional features system
   - Load order recommendations

2. **About.xml** - Add loadAfter (if needed):

   ```xml
   <loadAfter>
     <li>vanillaexpanded.gravship</li>
   </loadAfter>
   ```

3. **README.md** - Add compatibility notice:
   - VE Gravship compatibility status
   - How to enable/disable features
   - Where to report conflicts

**Testing Scenarios:**

1. **Features Enabled (Normal Operation)**:

   - Generate TradersGuild settlement with custom layout
   - Verify component added
   - Check mod settings UI

2. **Features Disabled (Fallback Mode)**:

   - Disable custom layouts in settings
   - Generate TradersGuild settlement
   - Verify vanilla layout used
   - Verify no component added

3. **VE Gravship Present (Compatibility Mode)**:
   - Install VE Gravship (or simulate with ModsConfig check)
   - Enable auto-disable
   - Verify features skip automatically
   - Check logs for compatibility messages

---

## Recommended Mod Settings (Default Configuration)

### For Most Players (Recommended)

```
✓ Custom settlement layouts          (ON)
✓ Dynamic cargo bay system           (ON)
✓ Auto-disable features on conflict  (ON)
Cargo percentage: 60%
```

This provides full features while maintaining safety.

### For Maximum Compatibility

```
✗ Custom settlement layouts          (OFF)
✗ Dynamic cargo bay system           (OFF)
✓ Auto-disable features on conflict  (ON)
```

Phase 2 features still work (peaceful trading, trader rotation).

### For Developers/Testers

```
✓ Custom settlement layouts          (ON)
✓ Dynamic cargo bay system           (ON)
✗ Auto-disable features on conflict  (OFF)
```

Forces features on for testing, even with potential conflicts.

---

## Monitoring & Response Plan

### Information Sources to Monitor

1. **VE Gravship Steam Workshop**

   - URL: https://steamcommunity.com/sharedfiles/filedetails/?id=3609835606
   - Check: Weekly for update announcements
   - Focus: Chapter 2/3 release dates, feature descriptions

2. **VE Gravship GitHub**

   - URL: https://github.com/Vanilla-Expanded/VanillaGravshipExpanded
   - Check: Commits to Settlement patches, GenStep changes
   - Focus: Technical implementation details

3. **Oskar Potocki's Patreon**

   - URL: https://www.patreon.com/OskarPotocki
   - Check: Dev blogs about Chapter 3 progress
   - Focus: Space station features, timeline

4. **RimWorld Modding Discord**

   - Server: Official RimWorld Discord
   - Check: #modding channel for compatibility discussions
   - Focus: Community reports of conflicts

5. **Steam Workshop Comments**
   - Location: Better Traders Guild workshop page
   - Check: Daily during first week after VE releases
   - Focus: Player bug reports, conflict reports

### Trigger Events & Actions

| Event                               | Action                                                 | Timeline        |
| ----------------------------------- | ------------------------------------------------------ | --------------- |
| VE announces Chapter 3 features     | Review feature list, assess conflicts, contact VE team | Within 24 hours |
| Player reports BTG+VE conflict      | Investigate, reproduce, add compatibility patch        | Within 48 hours |
| VE patches GenStep_OrbitalPlatform  | Test interaction, update compatibility mode            | Within 1 week   |
| VE patches Settlement_TraderTracker | Evaluate patch order, test trader rotation             | Within 1 week   |
| Steam comment about incompatibility | Quick response with workaround, update docs            | Within 12 hours |
| VE Chapter 3 release                | Full compatibility test, update COMPATIBILITY_PLAN.md  | Day 1           |

### Communication Strategy

**If Conflict Detected:**

1. Post Steam Workshop comment acknowledging issue
2. Create GitHub issue (if public repo)
3. Message VE team via Discord/Reddit
4. Offer to create compatibility patch
5. Update mod description with workaround

**If VE Wants to Collaborate:**

1. Share Better Traders Guild codebase
2. Coordinate feature development
3. Consider merging features into VE framework
4. Credit both teams appropriately

---

## Decision Points

### Should We Continue Phase 3?

**✅ YES - Proceed with mandatory safety changes**

**Rationale:**

- Phase 2 is rock-solid (no conflicts)
- Phase 3 conflicts are theoretical (Chapter 3 doesn't exist yet)
- Optional features provide escape hatch
- Better Traders Guild occupies different niche (faction-specific)
- VE's Chapter 2 (combat) comes first, giving us reaction time

**Required Before Phase 3.3:**

- [ ] Implement optional features system (~4 hours)
- [ ] Add VE detection (~30 minutes)
- [ ] Test both enabled/disabled modes (~30 minutes)
- [ ] Update documentation (~30 minutes)

**Total Overhead:** ~5.5 hours for critical safety net

### Alternative Options Considered

**Option A: Pause Phase 3 Until Chapter 3 Released**

- ❌ Could wait months/years
- ❌ Loses development momentum
- ❌ No guarantee VE will conflict
- ✅ Zero risk of wasted effort

**Decision:** Rejected - too conservative

**Option B: Proceed Without Safety Measures**

- ✅ Faster development
- ✅ Full creative freedom
- ❌ Players stuck if conflicts emerge
- ❌ Save corruption risk
- ❌ Bad mod ecosystem citizenship

**Decision:** Rejected - too risky

**Option C: Declare Incompatibility with VE**

- ✅ Simple solution
- ✅ Clear player expectations
- ❌ Alienates VE user base
- ❌ Bad for mod ecosystem
- ❌ Unnecessary at this time

**Decision:** Rejected - premature

**Option D: Proceed with Optional Features (CHOSEN)**

- ✅ Balances development speed with safety
- ✅ Provides player choice
- ✅ Good mod citizenship
- ✅ Future-proof against conflicts
- ⚠️ Small overhead (~5.5 hours)

**Decision:** ✅ **APPROVED** - Best balance of factors

---

## Post-Implementation Checklist

### Before Releasing Phase 3.3-3.5

- [ ] Optional features system implemented
- [ ] VE Gravship detection working
- [ ] Both enabled/disabled modes tested
- [ ] Component null safety verified
- [ ] Documentation updated (CLAUDE.md, README.md)
- [ ] Steam Workshop description includes compatibility info
- [ ] Load order recommendations tested
- [ ] Fallback to vanilla generation tested
- [ ] No errors in log when features disabled

### After VE Chapter 3 Release

- [ ] Full compatibility test with Chapter 3
- [ ] Test all three trigger scenarios (generation, rotation, conflict)
- [ ] Update COMPATIBILITY_PLAN.md with actual conflicts (if any)
- [ ] Contact VE team if conflicts detected
- [ ] Post Steam Workshop update about compatibility status
- [ ] Update loadAfter if needed
- [ ] Create compatibility patch if needed

---

## References

### External Resources

- **VE Gravship Steam Workshop:** https://steamcommunity.com/sharedfiles/filedetails/?id=3609835606
- **VE Gravship GitHub:** https://github.com/Vanilla-Expanded/VanillaGravshipExpanded
- **Reddit Release Thread:** https://www.reddit.com/r/RimWorld/comments/1p3ascy/vanilla_gravship_expanded_is_out_now_link_in_the/
- **Oskar Potocki Comment (Chapter 3 Hint):** Same Reddit thread

### Internal Documents

- **PLAN.md** - Development roadmap
- **CLAUDE.md** - Developer guidance
- **README.md** - User-facing documentation
- **Docs/CAPTAINS_QUARTERS_IMPLEMENTATION.md** - Phase 3.1 technical details

### Code Locations

- **GenStep Patch:** `Source/Patches/MapGeneration/GenStepOrbitalPlatformGenerate.cs`
- **Settlement Component:** `Source/WorldObjects/TradersGuildSettlementComponent.cs`
- **Trader Rotation:** `Source/Patches/Settlement/SettlementTraderTrackerGetTraderKind.cs`
- **Mod Settings:** `Source/Core/ModSettings.cs`

---

## Conclusion

Better Traders Guild and Vanilla Gravship Expanded are **currently compatible** with no direct conflicts. However, VE's planned Chapter 3 "space stations" content poses **moderate future risk**.

The strategic solution is to **proceed with Phase 3 development while implementing a mandatory optional features system** (~5.5 hours overhead). This provides:

- ✅ Player escape hatch if conflicts emerge
- ✅ Save compatibility protection
- ✅ Good mod ecosystem citizenship
- ✅ Future-proof against VE updates
- ✅ Development can continue safely

**Bottom Line:** The cargo system (Phase 3.4-3.5) is worth building - just build it defensively with optional features.

**Next Steps:**

1. Implement optional features system (this document serves as spec)
2. Test both enabled/disabled modes
3. Continue Phase 3.3 (Enhanced Pawn Generation)
4. Monitor VE releases for Chapter 3 announcements

---

**Document Status:** Living Document - Update after VE releases
**Last Updated:** 2025-01-23
**Next Review:** After VE Gravship Chapter 2 or Chapter 3 release

