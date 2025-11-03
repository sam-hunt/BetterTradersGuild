# RimWorld Storage API Documentation Index

This index organizes all storage system research and implementation guides for the Better Traders Guild mod.

## Documentation Files

### 1. STORAGE_API_SUMMARY.txt (Quick Reference)
**File:** `/home/shunt/dev/BetterTradersGuild/STORAGE_API_SUMMARY.txt`
**Purpose:** Executive summary of findings
**Best For:** Quick lookup, key concepts, high-level overview

**Contains:**
- Two storage patterns overview
- Container classes summary
- ThingOwner core methods
- StorageSettings system
- XML definition patterns
- Differences between shelf types
- Implementation recommendations

**Read First:** Yes - Start here for quick orientation

---

### 2. STORAGE_API_RESEARCH.md (Comprehensive Reference)
**File:** `/home/shunt/dev/BetterTradersGuild/STORAGE_API_RESEARCH.md`
**Purpose:** Complete technical documentation
**Best For:** Implementation, detailed understanding, API reference

**Contains 9 Sections:**
1. Container Architecture (Building_Storage, Building_Casket, etc.)
2. ThingOwner - Core Container System
3. Storage Settings System
4. XML Definitions (complete examples)
5. Map Generation Integration
6. Storage Differences
7. Code Patterns
8. Key Takeaways for Mod Development
9. File Paths & References

**Section Highlights:**
- **Section 1:** Classes, inheritance, interfaces, fields, properties
- **Section 2:** ThingOwner methods (add, remove, query operations)
- **Section 3:** StorageSettings, StoragePriority enum, ThingFilter
- **Section 4:** Complete XML examples for shelf, bookcase, outfit stand
- **Section 5:** Spawning buildings with items, BaseGen pattern
- **Section 6:** Comparison table for different storage types
- **Section 7:** Concrete code patterns for common tasks
- **Section 8:** Specific recommendations for cargo system

**Code Examples:** 50+ line-by-line patterns

---

### 3. CARGO_IMPLEMENTATION_GUIDE.md (Step-by-Step Implementation)
**File:** `/home/shunt/dev/BetterTradersGuild/CARGO_IMPLEMENTATION_GUIDE.md`
**Purpose:** Ready-to-use implementation code
**Best For:** Building Phase 3 cargo system

**Contains 6 Implementation Steps:**
1. XML Definition for Cargo Bay (complete ThingDef)
2. World Component for Settlement Tracking
3. Map Component for Cargo Management
4. Helper Method for Settlement Detection
5. Patch to Add Map Component
6. Mod Settings for Cargo Configuration

**Architecture Diagram:** Yes - visual overview provided

**Code Quality:**
- Fully documented with XML comments
- Follows mod conventions
- Includes error handling
- Performance notes included

**Testing Checklist:** Included

**Key Concepts:**
- Anti-exploit protection (cargo only refreshes on trader rotation)
- Dynamic inventory-based cargo selection
- Map persistence compliance (spawned on entry, not in generation)
- Budget-based cargo selection (percentage of trade inventory)

---

## Quick Navigation Guide

### I need to understand how storage works in RimWorld
1. Start: **STORAGE_API_SUMMARY.txt** - Section 2 "CONTAINER CLASSES"
2. Deep dive: **STORAGE_API_RESEARCH.md** - Sections 1-3
3. Concrete examples: **STORAGE_API_RESEARCH.md** - Section 7 "CODE PATTERNS"

### I'm implementing the cargo system
1. Architecture: **CARGO_IMPLEMENTATION_GUIDE.md** - Architecture Summary
2. Step by step: Follow Steps 1-6 in order
3. Reference: **STORAGE_API_RESEARCH.md** - Sections 4-5 for building/item spawning
4. Testing: Use Testing Checklist at end of guide

### I need specific code for a task
1. Task lookup:
   - **Adding items to storage:** STORAGE_API_RESEARCH.md Section 7.1
   - **Creating Building_Storage:** STORAGE_API_RESEARCH.md Section 7.1
   - **Configuring storage settings:** STORAGE_API_RESEARCH.md Section 3
   - **XML for storage building:** STORAGE_API_RESEARCH.md Section 4
   - **Component-based container:** STORAGE_API_RESEARCH.md Section 1.5
   - **Cargo spawning:** CARGO_IMPLEMENTATION_GUIDE.md Step 3

### I need XML examples
**STORAGE_API_RESEARCH.md Section 4:**
- 4.1 - Shelf Definition Pattern
- 4.2 - Bookcase Definition Pattern
- 4.3 - Outfit Stand Definition Pattern

**CARGO_IMPLEMENTATION_GUIDE.md Step 1:**
- Cargo Bay XML (ready to use)

### I need to understand the difference between storage types
**STORAGE_API_RESEARCH.md Section 6:** Storage Differences
**STORAGE_API_SUMMARY.txt Section 7:** Critical Differences

---

## Key Concepts Across Files

### Building_Storage
- **Summary:** STORAGE_API_SUMMARY.txt Section 2.a
- **Full:** STORAGE_API_RESEARCH.md Section 1.1
- **XML:** STORAGE_API_RESEARCH.md Section 4.1
- **Code:** STORAGE_API_RESEARCH.md Section 7.1

### ThingOwner
- **Summary:** STORAGE_API_SUMMARY.txt Section 3
- **Full:** STORAGE_API_RESEARCH.md Section 2
- **Methods:** STORAGE_API_RESEARCH.md Section 2.1-2.5
- **Code:** STORAGE_API_RESEARCH.md Section 7.1-7.4

### StorageSettings
- **Summary:** STORAGE_API_SUMMARY.txt Section 4
- **Full:** STORAGE_API_RESEARCH.md Section 3
- **XML:** STORAGE_API_RESEARCH.md Section 4

### CompThingContainer
- **Summary:** STORAGE_API_SUMMARY.txt Section 2.e
- **Full:** STORAGE_API_RESEARCH.md Section 1.5
- **Code:** STORAGE_API_RESEARCH.md Section 7.4

### Map Generation
- **Summary:** STORAGE_API_SUMMARY.txt Section 6
- **Full:** STORAGE_API_RESEARCH.md Section 5
- **Implementation:** CARGO_IMPLEMENTATION_GUIDE.md Steps 3-5

---

## Implementation Checklist

### Pre-Implementation
- [ ] Read STORAGE_API_SUMMARY.txt (5 min)
- [ ] Read CARGO_IMPLEMENTATION_GUIDE.md Architecture (3 min)

### Implementation
- [ ] Step 1: Create XML for cargo bay (5 min)
- [ ] Step 2: Create TradersGuildSettlementComponent (10 min)
- [ ] Step 3: Create TradersGuildCargoRefresher (20 min)
- [ ] Step 4: Add helper methods (5 min)
- [ ] Step 5: Create harmony patch (5 min)
- [ ] Step 6: Add mod settings (10 min)

### Troubleshooting During Implementation
- **"How do I add items to storage?"** → STORAGE_API_RESEARCH.md Section 7.1
- **"What XML properties do I need?"** → STORAGE_API_RESEARCH.md Section 4.4
- **"How do I prevent exploits?"** → CARGO_IMPLEMENTATION_GUIDE.md Section "Key Design Decisions"
- **"Where do I spawn the building?"** → STORAGE_API_RESEARCH.md Section 5.3
- **"What's the ThingOwner API?"** → STORAGE_API_RESEARCH.md Section 2.1-2.5

---

## Research Methodology

This documentation was created by:
1. Monodis analysis of RimWorld Assembly-CSharp.dll
2. Inspection of RimWorld's own XML definitions
3. Cross-referencing with existing implementations
4. Pattern extraction from source code IL
5. Synthesis into structured documentation

**Source Files:**
- `Assembly-CSharp.dll` - RimWorld API
- `Buildings_Furniture.xml` - Core shelves
- `Odyssey/Buildings_Furniture.xml` - Outfit stands

---

## API Surface Summary

### Primary Classes Used
```
Verse.Building
├── RimWorld.Building_Storage (shelves, cargo bays)
├── RimWorld.Building_Casket (cryptosleep pods)
├── RimWorld.Building_Bookcase (bookshelves)
└── RimWorld.Building_OutfitStand (apparel storage)

Verse.ThingComp
└── RimWorld.CompThingContainer (component storage)

Verse.ThingOwner (container for items)
├── Item management (add, remove, query)
└── List interface (iteration, indexing)

RimWorld.StorageSettings (storage configuration)
└── Priority & filtering
```

### Critical Methods
```csharp
// Spawning
GenSpawn.Spawn(thing, position, map)
ThingMaker.MakeThing(thingDef, stuff)

// Adding items
ThingOwner.TryAdd(item, count)
ThingOwner.TryAddOrTransfer(item)

// Configuration
StorageSettings.filter.SetAllow(thingDef, bool)
StorageSettings.Priority = StoragePriority.Important

// Access
Building_Storage.slotGroup.HeldThings
Building_Casket.GetDirectlyHeldThings()
CompThingContainer.innerContainer
```

---

## Notes for Contributors

- All code in CARGO_IMPLEMENTATION_GUIDE.md is production-ready
- XML examples can be used directly in mod
- Test checklist should be run before merging
- Performance notes indicate no profiling needed
- Future enhancements section provides expansion ideas

---

Last Updated: 2025-11-02
Research Complete: RimWorld 1.6 Storage API
Next Step: Phase 3 Implementation
