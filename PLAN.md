# Better Traders Guild - Development Plan

## Project Overview

**Better Traders Guild** enhances player interactions with RimWorld's Traders Guild faction through three major feature phases: peaceful trading visits, enhanced map generation, and expanded reputation systems.

### Vision Statement

Transform the Traders Guild from a generic hostile orbital faction into a believable, thematically rich merchant civilization with:

- Peaceful trading opportunities at their orbital stations
- Visually and mechanically distinct settlement generation
- Dynamic trader rotation reflecting their merchant nature
- Risk/reward balance for hostile encounters

---

## Current Implementation Status

### ✅ Phase 1: Core Infrastructure (COMPLETED)

- Mod initialization and Harmony patching system
- Helper utilities for faction/settlement detection
- Project structure and build pipeline

### ✅ Phase 2: Enable Peaceful Trading Visits (COMPLETED)

- Shuttle/caravan visit gizmos for friendly Traders Guild settlements
- Faction relation checks and signal jammer overrides
- Caravan travel to space settlements enabled
- **Orbital Trader Rotation System** - Dynamic trader types with virtual schedules
  - Deterministic rotation based on settlement ID
  - Virtual schedule preview for unvisited settlements
  - Player-configurable rotation interval (5-30 days, default 15)
  - Three-patch architecture solving stock/dialog desync issues
  - Docked vessel display on world map inspection

### 🚧 Phase 3: Enhanced TradersGuild Settlement Generation (CURRENT)

Overhaul map generation for TradersGuild orbital platforms to reflect their identity as prosperous space merchants rather than abandoned ruins.

**Completed Sub-phases:**

- ✅ Phase 3.2: Harmony Patch for Layout Override - GenStep_OrbitalPlatform patching complete, TradersGuildSettlementComponent implemented

**Current Focus:**

- 🚧 Phase 3.1 (Revised): Creating comprehensive custom room layouts with modern aesthetics
  - 18 custom BTG_Orbital\* RoomDefs with metal tile flooring
  - 10 custom PrefabDefs for hospital equipment, furniture, and room-specific items
  - Biotech-gated nursery and classroom rooms
  - New room types: Armory, TradeShowcase, SecurityStation, Workshop, 🚧 **CaptainsQuarters (IN PROGRESS)**, CargoStorage
  - **CaptainsQuarters Status:**
    - ✅ Custom RoomContentsWorker with programmatic bedroom placement (basic corner placement working)
    - ✅ Book insertion system (automatically inserts books into bookcase containers)
    - ✅ Quality-based book spawning (1-4 books per small bookcase, excellent to legendary)
    - ✅ L-shaped secure bedroom subroom with AncientBlastDoor
    - ✅ Lounge area with dynamic furniture spawning around bedroom
    - ✅ **Unique weapon system:**
      - Spawns unique weapon on bedroom shelf with custom traits
      - Weighted random selection (Revolver, ChargeRifle, ChargeLance, BeamRepeater)
      - Quality: Excellent/Masterwork/Legendary (via QualityUtility.GenerateQualitySuper)
      - Three traits: weapon-specific primary + GoldInlay + random compatible
      - Reflection-based name/color regeneration after trait modification
    - ✅ **Bedroom placement algorithm (PlacementCalculator.cs):**
      - Door detection and avoidance for all corner placements
      - Edge placement fallback (all 4 walls: North, East, South, West) with procedural wall spawning
      - Center placement fallback with two-wall L-shaped enclosure
      - Priority system: corners → edges → center → invalid
      - Comprehensive test coverage: 11 passing tests in PlacementCalculatorTests.cs
    - 🚧 **Remaining Work:**
      - **Valid cell marking verification:**
        - Confirm IsValidCellBase prevents prefabs from overlapping bedroom walls
        - Previous test showed a prefab replacing bedroom corner wall once
      - **Billiards table clearance:**
        - Mark 1-tile area around billiards table as invalid for other prefabs
        - Pawns need clearance to use it; blocking breaks immersion
        - Prioritize billiards spawning early (bigger prefab needs space)
        - Investigate whether bedroom edge wall can be considered for lounge edgeOnly prefab placements
        - Increase minMaxRange of edgeOnly lounge prefabs

### 🔮 Phase 4: Reputation & Quest Systems (FUTURE)

- Trade quotas and reputation rewards
- Escort missions and diplomatic opportunities
- Dynamic faction relations

**Stretch Goals:**
- **Placement randomization** - Add variety to bedroom placement across different settlements
  - Randomize corner iteration order (clockwise vs counterclockwise)
  - Randomize initial rotation (North/East/South/West instead of always North)
  - Use settlement ID as seed for consistent but varied placement per station
  - Prevents every CaptainsQuarters from looking identical

---

## Phase 3: Enhanced Settlement Generation - Detailed Plan

### The Problem

**Current State:** TradersGuild settlements use the `OrbitalSettlementPlatform` LayoutDef, which generates:

- Deserted/ancient aesthetics (rusted walls, dead hydroponics, ancient blast doors)
- Minimal differentiation from abandoned platforms
- No thematic connection to their merchant identity
- Generic room configurations

**Lore Conflict:** Faction description states they are _"a loose coalition of orbital traders who prefer to live in the safety of their orbital platforms"_ over planet-side dangers, yet their bases look abandoned and decrepit.

### The Solution

Create custom map generation that reflects TradersGuild's identity while respecting RimWorld's map persistence mechanics:

1. **Thriving commercial infrastructure** - Modern aesthetics, functional systems, not abandoned
2. **Prosperous living conditions** - Functional hydroponics, comfortable quarters, medical bays
3. **High-tech infrastructure** - Command centers, server rooms, power generation
4. **Static base with dynamic cargo bay** - Station infrastructure is permanent, but shuttle bay cargo changes with trader rotation
5. **Balanced risk/reward** - Enhanced loot matched with stronger defenders

### Map Persistence Architecture (CRITICAL DESIGN CONSTRAINT)

**The Fundamental Problem:**

RimWorld maps are **generated once and saved permanently**. When a player returns to a settlement, the map is **loaded from disk**, not regenerated. This creates a critical constraint:

```
Timeline:
Day 1:  Map generates with Exotic trader cargo (art, luxury goods)
Day 2:  Player retreats without destroying settlement → Map saved
Day 15: Trader rotation occurs → Combat Supplier now docked
Day 16: Player returns → Map loads from save (STILL HAS EXOTIC CARGO)
```

**The Solution: Static Base + Dynamic Cargo Refresh**

We divide the settlement into two zones:

1. **Static Base Infrastructure (Never Changes)**

   - Room layouts, walls, doors, furniture are permanent
   - Reflects station's permanent capabilities
   - Modern aesthetics (not ancient/deserted)
   - Generic merchant station theme

2. **Dynamic Cargo Bay (Refreshes on Trader Rotation)**
   - ONE designated "Shuttle Bay" or "Cargo Transfer Area"
   - Items despawn/respawn when trader rotation occurs
   - Uses custom MapComponent to track cargo refresh state
   - Prevents exploit farming (only refreshes once per rotation)

**Why This Works:**

- **Lore-accurate:** Station infrastructure is permanent; traders bring temporary cargo
- **Technically sound:** Respects map persistence while enabling trader-specific loot
- **Phase 2 compatible:** Trader rotation still matters for cargo contents
- **Anti-exploit:** Cargo only refreshes when `lastStockGenerationTicks` changes

---

## Research Summary: Map Generation Systems

### Vanilla RimWorld Architecture

**MapGeneratorDef System:**

- `SettlementPlatform` MapGeneratorDef uses `GenStep_OrbitalPlatform` (line 82-91, `SpaceMapGenerator.xml`)
- GenStep references `StructureLayoutDef` (e.g., `OrbitalSettlementPlatform`)
- LayoutDefs define: room types, wall/door materials, terrain, corridors, attachments

**StructureLayoutDef System:**

- `OrbitalSettlementPlatform` (line 159, `Layouts_OrbitalPlatform.xml`) inherits from `OrbitalAncientPlatformBase`
- Defines roomDefs with count ranges (e.g., `<OrbitalStoreroom>2</OrbitalStoreroom>`)
- Vanilla handles room generation, pathing, door placement automatically
- Room contents populated by vanilla logic (randomized furniture/decorations)

**Key File Paths:**

```
/Data/Odyssey/Defs/MapGeneration/SpaceMapGenerator.xml
/Data/Odyssey/Defs/LayoutDefs/Layouts_OrbitalPlatform.xml
/Data/Odyssey/Defs/FactionDefs/Factions_Misc.xml (TradersGuild definition)
/Data/Odyssey/Defs/WorldObjectDefs/WorldObjects.xml (SpaceSettlement definition)
```

In addition to the above, I've proactively installed xmllint, and created some xsd files based on the core vanilla and official DLC def files using relaxng's trang library.
While there's always a chance of false positives or false negatives as they aren't based on the source code the defs were generated from,
you may find these useful as a quick smoketest for iterating on our defs before we attempt to load them into the game (much slower).
You can find these schema files at `/home/shunt/dev/RimworldXsd/schemas/1.6.4566_rev606/`.

**BaseGen SymbolResolver System:**

- C# classes in `RimWorld.BaseGen` namespace
- Examples: `SymbolResolver_AncientComplex`, `SymbolResolver_Settlement`
- Custom resolvers allow precise control over room contents, furniture placement, pawn spawning

### Vanilla Expanded Framework (VEF)

**Findings:**

- VEF is a code library providing shared behaviors for VE mod series
- Includes "building export tool" (needs further investigation)
- GitHub wiki contains documentation (not fully explored)
- **NOT a map generation toolkit** - just provides utilities

**Decision:** Removed VEF dependency - not needed for Phase 3.

### Vanilla Base Generation Expanded

**Findings:**

- Finished asset library for procedural faction base generation
- Affects factions deriving from vanilla definitions
- **No modding APIs exposed** - "all or nothing" package
- Creates faction-specific aesthetics (tribal farms, pirate bunkers, etc.)

**Decision:** Removed VBGE dependency - not useful for custom TradersGuild layouts.

---

## Technical Architecture

### Component Overview

```
Phase 3 Systems:
┌──────────────────────────────────────────────────────────────────┐
│  Phase 2: World Map Layer (Existing)                             │
│  • Settlement inspection shows docked trader type                │
│  • Virtual schedule determines current trader                    │
│  • lastStockGenerationTicks tracked in Settlement_TraderTracker  │
└────────────────┬─────────────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  First Visit: Map Generation                                     │
│  • GenStep_OrbitalPlatform.Generate() called                     │
│  • Harmony Patch: Detect TradersGuild faction                    │
│  • Override layoutDef → TradersGuild_OrbitalSettlement           │
│  • Generate static base rooms (permanent structure)              │
│  • Generate shuttle bay with current trader cargo               │
│  • Initialize TradersGuildSettlementComponent.lastCargoRefresh   │
└────────────────┬─────────────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────────────┐
│  Revisit: Cargo Refresh System (NEW)                             │
│  • Player enters existing map                                    │
│  • TradersGuildCargoRefresher MapComponent checks:               │
│    - Is lastCargoRefreshTicks < lastStockGenerationTicks?        │
│    - If YES: Trader rotation occurred while player was away      │
│  • Despawn old cargo items in shuttle bay                        │
│  • Query new trader type from Phase 2 system                     │
│  • Spawn new trader-specific cargo                               │
│  • Update lastCargoRefreshTicks to prevent re-farming            │
└──────────────────────────────────────────────────────────────────┘

Key Components:

1. TradersGuild_OrbitalSettlement LayoutDef (XML)
   └── Defines static room structure, modern aesthetics

2. SymbolResolver_TradersGuildShuttleBay (C#)
   └── Spawns trader-specific cargo based on current trader type

3. TradersGuildSettlementComponent (C# WorldObjectComp)
   └── Tracks lastCargoRefreshTicks for anti-exploit protection

4. TradersGuildCargoRefresher (C# MapComponent)
   └── Detects trader rotation, triggers cargo despawn/respawn
```

### Key Integration Points

**Phase 2 → Phase 3 Connection:**

- Virtual schedule system (`TradersGuildTraderRotation.GetVirtualLastStockTicks()`) determines trader type
- Map generation reads same settlement ID and tick data
- **Ensures preview consistency** - What you see on world map matches generated map
- Trader-themed rooms/storage use same `TraderKindDef` as Phase 2 trading system

**Trader Type Query Logic:**

```csharp
// In SymbolResolver - query current trader
Settlement settlement = /* get from map generation context */;
int settlementID = settlement.ID;
long virtualTicks = TradersGuildTraderRotation.GetVirtualLastStockTicks(settlementID);
int seed = Gen.HashCombineInt(settlementID, virtualTicks);
TraderKindDef traderKind = GetWeightedRandomTrader(faction.orbitalTraderKinds, seed);
// Use traderKind to spawn themed inventory
```

---

## Thematic Design: Rooms & Features

This section defines the **static base structure** (permanent rooms) and **dynamic cargo bay** (refreshes on trader rotation).

### Design Philosophy

**Static Infrastructure:**

- All room structures, walls, doors, and base furniture are permanent
- Reflects the station's core operational capabilities
- Modern, functional aesthetics (not ancient/deserted)
- Same layout for all TradersGuild settlements

**Dynamic Cargo:**

- ONE shuttle bay with trader-specific items
- Cargo refreshes when trader rotation occurs
- Prevents exploit farming via anti-refresh tracking
- Future: Category-based item top-ups in other rooms (e.g., medicine in medical bay)

---

### Static Base Rooms (Permanent Structure)

#### 1. Command Center (Computer Room)

**Based on vanilla:** `OrbitalComputerRoom`

**Purpose:** Central operations hub for station management and communications

**Permanent Contents:**

- Comms consoles (how they contact players)
- Server racks and computer terminals
- Good-quality furniture (chairs, tables)
- Wall-mounted screens
- Climate control
- Ancient security terminals

**Aesthetics:** Modern, functional, well-lit

#### 2. Medical Bay (Custom Room)

**New RoomDef:** `TradersGuild_MedicalBay`

**Purpose:** Healthcare facility for station crew

**Permanent Contents:**

- Hospital beds (2-4 depending on size)
- Vitals monitors
- Medicine storage (industrial medicine - standard)
- Medical equipment (IV stands, tool cabinets)
- Sterile tile flooring

**Future Enhancement:** Exotic traders could top-up with glitterworld medicine

#### 3. Barracks (Living Quarters)

**Based on vanilla:** `OrbitalBarracks` (but non-deserted variant)

**Purpose:** Crew quarters and guard accommodations

**Permanent Contents:**

- Comfortable beds (NOT ancient/rusted variants)
- Lockers and dressers
- Recreation items (TV, chess tables)
- Weapon racks
- Decent-quality furniture

#### 4. Hydroponics Bay

**Based on vanilla:** `OrbitalHydroponics` (functional, not deserted)

**Purpose:** Food production for station population

**Permanent Contents:**

- **FUNCTIONAL** hydroponics basins with growing crops
- Sun lamps
- Drug lab table (vanilla includes this)
- Shelves for supplies
- Clean, maintained appearance

**Key Difference from Vanilla:** NOT the "\_Deserted" variant (no corpses, functional systems)

#### 5. Dining Hall

**Based on vanilla:** `OrbitalDiningRoom`

**Purpose:** Communal eating and food preparation

**Permanent Contents:**

- Large dining tables with stools
- Electric stove (subroom)
- Food shelves
- Survival meal pallets
- Decorative sculptures (small)

#### 6. Recreation Room

**Based on vanilla:** `RecRoom`

**Purpose:** Crew recreation and morale

**Permanent Contents:**

- Billiards table, poker table, chess tables
- Flatscreen TV with seating
- Couches and armchairs
- Locker banks

#### 7. General Storerooms (2-3 rooms)

**Based on vanilla:** `OrbitalStoreroom`

**Purpose:** General supplies and equipment storage

**Permanent Contents:**

- Industrial shelves (edge and row configurations)
- Ancient spacer crates with mixed loot
- Component crates
- Mining charges, firefoam poppers
- Organized storage zones

**Note:** Contains standard merchant supplies (silver, components, basic goods)

---

### Dynamic Shuttle Bay (Trader-Specific Cargo)

This is the **ONLY room with dynamic content** that changes when trader rotation occurs. All other rooms remain static.

**Based on vanilla:** `OrbitalTransportRoom` (large open area, no roof)

**Room Structure (Permanent):**

- Large open bay (11x11+ cells minimum)
- No roof (open to space)
- Permanent shuttle landing pad/markings
- Designated cargo spawn zones (tagged for despawn/respawn)

**Cargo Refresh Mechanics:**

- On first generation: Spawns cargo matching current trader type
- On revisit after trader rotation: Despawns old cargo, spawns new cargo
- Tracked via `TradersGuildSettlementComponent.lastCargoRefreshTicks`
- Anti-exploit: Only refreshes once per trader rotation interval

**Dynamic Cargo Generation System:**

Instead of hardcoded item lists, cargo is **dynamically generated from the settlement's actual trade inventory**. This approach provides:

✅ **Automatic Mod Compatibility** - Works with any trader type from any mod
✅ **Trade/Cargo Consistency** - Stolen/spawned cargo reduces available trade inventory
✅ **Emergent Gameplay** - Sell items → attack → steal them back!
✅ **No Maintenance** - No need to update manifests when mods change

**How It Works:**

1. **On Map Generation:**

   - Settlement's trade inventory is generated (lazy generation if needed)
   - Calculate cargo budget: `totalInventoryValue × cargoPercentage` (60% default, configurable in mod settings)
   - Randomly select items from trade inventory until budget met
   - Remove selected items from trade inventory
   - Spawn cargo in shuttle bay

2. **On Cargo Refresh (After Rotation):**
   - Vanilla regenerates trade inventory (normal behavior)
   - Old cargo despawned (NOT restored to trade inventory - it was "sold")
   - New cargo selected from new trade inventory
   - New items removed from trade inventory
   - Cargo spawned

**Example Scenarios:**

**Scenario: Bulk Goods Trader**

- Trade inventory: 500 steel, 100 plasteel, 50 components, 80 wood, 30 textiles
- Cargo (60%): ~300 steel, 60 plasteel, 30 components, 48 wood, 18 textiles spawned
- Remaining trade inventory: 200 steel, 40 plasteel, 20 components, 32 wood, 12 textiles

**Scenario: Player Sells Yayo Then Attacks**

- Player sells 1000 yayo to Pirate Merchant trader
- Player attacks settlement
- Cargo bay contains: organs, drugs, **600 yayo** (from player!), stolen tech
- Player can steal back their own yayo!

**Scenario: Player Steals Cargo Then Trades**

- Player attacks, steals 50% of cargo, retreats
- Player improves goodwill and trades peacefully
- Trade inventory is reduced (stolen items missing)
- Realistic outcome!

**What Appears in Cargo Bay:**

The exact items depend on vanilla's (or modded) stock generators for each trader type, but for example:

- **Bulk Goods:** Building materials, raw resources, textiles
- **Combat Supplier:** Weapons, armor, explosives, shield belts
- **Exotic:** Artwork, exotic animals, luxury goods, rare materials, social drugs
- **Pirate Merchant:** Organs, slaves, hard drugs, stolen tech, bionics

Since cargo comes from actual trade inventory, it automatically reflects the trader's specialization without hardcoding.

---

### Elite Pawn Class Design

**Spawn Logic:**

- **1 guaranteed elite** per settlement (like faction leader)
- **10-15% of remaining defenders** are elite
- Scale with raid points (larger bases = more elites)

**Equipment Standards:**

- **VERIFY:** Vacuum resistance requirements met
- **VERIFY:** Are there existing PawnKindDefs we can use?
- **VERIFY:** TradersGuild faction tech level is spacer
- **VERIFY:** TradersGuild faction xenotype ratios (with biotech)

**Standard TradersGuild Defender:**

- **Weapons:** Assault rifle, charge rifle, chain shotgun (normal-good quality)
- **Armor:** Recon armor, marine armor (normal-good quality)
- **Bionics:** 30% chance of basic bionic (bionic eye, bionic arm)
- **Apparel:** Spacer tech level clothing (normal-good quality)

**Elite TradersGuild Defender:**

- **Weapons:** charge rifle, charge lance, minigun, monosword (good-excellent quality)
- **Armor:** Recon armor, marine armor, cataphract armor (good-excellent quality)
- **Bionics:** 50% chance of basic bionic, 50% chance of advanced bionic (bionic heart, lung, stomach, spine)
- **Apparel:** Spacer tech level clothing (good-excellent quality)
- **Special:** May carry shield belts, jump packs etc

**PawnKindDef Suggestions:**

- Create `TradersGuild_Elite` PawnKindDef
- Higher `combatPower` value
- Guaranteed spawns in important rooms (Command Center, Armory, Shuttle Bay)?

---

## Configuration & Balance

### Mod Settings UI

**New Settings (Phase 3):**

1. **Raid Point Multiplier**

   - Label: "TradersGuild Settlement Difficulty"
   - Description: "Multiplier for enemy strength when attacking TradersGuild settlements. Higher values mean more/stronger defenders to match the increased loot."
   - Range: 1.0x to 3.0x
   - Default: 1.5x
   - Step: 0.1x

2. **Weapon Biocoding Chance**

   - Label: "Weapon Biocoding Percentage"
   - Description: "Percentage of TradersGuild weapons that are biocoded to their owners. Biocoded items cause mood penalties for unauthorized users."
   - Range: 0% to 100%
   - Default: 50%
   - Step: 5%

3. **Armor Biocoding Chance**

   - Label: "Armor/Apparel Biocoding Percentage"
   - Description: "Percentage of TradersGuild armor and apparel that is biocoded. Separate from weapons for finer balance control."
   - Range: 0% to 100%
   - Default: 30%
   - Step: 5%

4. **Cargo Bay Inventory Percentage**
   - Label: "Cargo Bay Inventory Percentage"
   - Description: "Percentage of the trader's stock that spawns as cargo in the shuttle bay during map generation and after trader rotations. Items spawned as cargo are removed from the trade inventory for balance. Higher values mean more loot when raiding but less available for peaceful trading."
   - Range: 30% to 100%
   - Default: 60%
   - Step: 5%

**Existing Settings (Phase 2):**

- Trader Rotation Interval (5-30 days, default 15)

### Risk/Reward Balance Philosophy

**Increased Risk:**

- 1.5x raid points = ~50% more defenders
- Elite pawns with superior gear
- Better positioning (defensive structures)
- Biocoding reduces usability of looted items

**Increased Reward:**

- Shuttle Bay with trader-specific inventory (potentially 10k-50k+ silver value)
- Themed rooms with specialized loot
- High-quality base infrastructure (can be salvaged)
- Elite pawn gear (if you can remove biocoding or don't mind penalties)

**Player Decision Matrix:**

```
Low Goodwill + Strong Colony = Attack for massive loot (endgame raid)
High Goodwill = Peaceful trading (Phase 2 features)
Mid Goodwill + Desperate = Risk/reward decision (player choice)
```

---

## Implementation Phases (Simplified Approach)

### Phase 3.1: Static Base Layout (XML) ⭐ REVISED DESIGN

**Goal:** Create custom LayoutDef with custom BTG_Orbital\* RoomDefs featuring modern aesthetics (metal tiles, functional equipment)

**Design Principles:**

- **Modern Floors:** Metal tiles (MetalTile) instead of ancient tiles for cleaner aesthetic
- **Functional Equipment:** Modern, maintained furniture and equipment (not ancient/rusted)
- **No Cryptosleep Ambushes:** Remove AncientCryptosleepRoom_Hostile (doesn't fit active settlement theme)
- **Room Co-location:** Accept vanilla procedural placement (focus on individual room excellence)
- **Naming Convention:** `BTG_Orbital[RoomType]` pattern for clarity and uniqueness

**Tasks:**

1. **Create `Defs/LayoutDefs/BTG_LayoutDefs.xml`**

   - Define `BTG_OrbitalSettlement` StructureLayoutDef
   - Inherit from `OrbitalAncientPlatformBase` (same walls/doors as vanilla)
   - Reference CUSTOM BTG_Orbital\* RoomDefs:
     - `BTG_OrbitalComputerRoom` (command center)
     - `BTG_OrbitalMedicalBay` (medical facility)
     - `BTG_OrbitalBarracks` (crew quarters)
     - `BTG_OrbitalHydroponics` (food production)
     - `BTG_OrbitalDiningRoom` (dining hall)
     - `BTG_OrbitalRecRoom` (recreation)
     - `BTG_OrbitalNursery` (childcare - Biotech gated)
     - `BTG_OrbitalClassroom` (education - Biotech gated)
     - `BTG_OrbitalArmory` (weapons/armor storage)
     - `BTG_OrbitalTradeShowcase` (display valuable goods)
     - `BTG_OrbitalSecurityStation` (guard post/monitors)
     - `BTG_OrbitalWorkshop` (crafting/repairs)
     - `BTG_OrbitalCaptainsQuarters` (luxury leader room)
     - `BTG_OrbitalCargoStorage` (shipping containers/pallets)
     - `BTG_OrbitalStoreroom` (2 instances - general storage)
     - `BTG_OrbitalTransportRoom` (shuttle bay for dynamic cargo)

2. **Create custom RoomDefs** (`Defs/LayoutRoomDefs/BTG_RoomDefs.xml`)

   **Core Rooms (iterate room-by-room as we work on each):**

   - `BTG_OrbitalMedicalBay`:

     - All sterile tile flooring (no edge terrain)
     - Custom prefabs: hospital bed pairs with vitals monitors
     - Medicine shelves with spawned medicine
     - Pot plants for ambiance
     - Minimal threats (low priority target)

   - `BTG_OrbitalComputerRoom`:

     - Metal tile flooring
     - Computer terminals, comms consoles
     - Server racks, wall monitors
     - Good quality furniture

   - `BTG_OrbitalBarracks`:

     - Carpet flooring
     - Modern beds (not ancient variants)
     - Lockers, dressers, end tables, weapon racks
     - Recreation items (TV, chess)

   - `BTG_OrbitalHydroponics`:

     - Metal tile flooring
     - Functional hydroponics with growing crops
     - Sun lamps, drug lab
     - Clean, maintained appearance

   - `BTG_OrbitalDiningRoom`:

     - Fine carpet flooring
     - Large dining tables with chairs
     - Survival meal storage
     - Kitchen prefab with Electric stove, food shelves, sterile tile flooring

   - `BTG_OrbitalRecRoom`:

     - Fine carpet flooring
     - Billiards, poker, chess tables
     - Flatscreen TV with seating
     - Comfortable furniture

   - `BTG_OrbitalNursery` (MayRequire="Ludeon.RimWorld.Biotech"):

     - Carpet flooring
     - Cribs, toys, play equipment
     - Safe, child-friendly layout

   - `BTG_OrbitalClassroom` (MayRequire="Ludeon.RimWorld.Biotech"):

     - Carpet flooring
     - School desks and chairs, blackboards, flatscreen TV
     - Learning materials, bookshelves with textbooks

   - `BTG_OrbitalArmory`:

     - Metal tile flooring
     - Weapon racks, armor stand with power armor
     - Ammunition storage (Mortar shells? something else)
     - Security equipment

   - `BTG_OrbitalTradeShowcase`:

     - Small floor area
     - Fine carpet flooring with gold tile edge flooring
     - Display pedestals prefab with valuable items
     - Merchant's pride and joy

   - `BTG_OrbitalSecurityStation`:

     - Metal tile flooring
     - Security terminals, CRT TV, weapon cache, lockers
     - Guard post furniture

   - `BTG_OrbitalWorkshop`:

     - Metal tile flooring
     - Crafting benches for repairs
     - Tool storage, component shelves

   - `BTG_OrbitalCaptainsQuarters`: 🚧 **IN PROGRESS** (functional but needs improvements)

     - **Custom RoomContentsWorker** - Programmatic bedroom placement with lounge area
     - Fine carpet flooring throughout
     - Medium floor area (12x10 minimum)
     - **Secure bedroom subroom:**
       - ✅ 7x7 L-shaped prefab spawned in NE corner (hardcoded for testing)
       - 🚧 **TODO:** Door detection + edge/center fallback placement
       - ✅ AncientBlastDoor entrance (hackable)
       - ✅ Royal bed (vacstone, excellent), dresser, end table
       - ✅ Animal bed for bonded pet, life support unit
       - ✅ Ancient safe, flatscreen TV, potted plants
       - 🚧 **TODO:** Small shelf with unique weapon (type TBD, research gold inlay trait)
     - **Lounge area (around bedroom):**
       - ✅ 1-3 small bookshelves with 1-4 quality-based books each
       - ✅ **Book insertion system:** Books automatically inserted into bookcase containers via post-spawn fixup
       - ✅ Flatscreen TV with wolf leather couch
       - ✅ Armchairs with decorative potplants
       - ✅ Billiards table, vacstone sculptures
       - 🚧 **TODO:** Billiards table needs 1-tile clearance marking (prevent blocking pawn interaction)
       - 🚧 **TODO:** Verify IsValidCellBase prevents prefab overlap with bedroom walls
     - Excellent quality furniture throughout
     - Minimal threat (VIP quarters)

   - `BTG_OrbitalCargoStorage`:

     - Metal tile flooring
     - Shipping containers, pallets
     - Different aesthetic from storeroom

   - `BTG_OrbitalStoreroom` (2 instances):

     - Metal tile flooring
     - Industrial shelves
     - Mixed loot crates

   - `BTG_OrbitalTransportRoom`:
     - Huge floor area
     - Dual room split by wall and vac barrier:
       - Half unroofed, open to space, landing pad (vanilla prefab?), shelf with chemfuel
       - Half roofed, open to corridor, many shelves for items (Cargo staging area (Phase 3.4))

3. **Create custom PrefabDefs** (`Defs/PrefabDefs/BTG_PrefabDefs.xml`)

   - `BTG_HospitalBedPair`: 2 hospital beds parallel, separated by vitals monitor, perpendicular to wall
   - `BTG_MedicineShelf`: Shelf with industrial medicine spawned
   - Additional prefabs as needed for other rooms

4. **Test generation**
   - Verify layout generates with custom rooms
   - Check metal tile flooring throughout
   - Verify modern, functional aesthetic
   - Test Biotech room gating (nursery/classroom only spawn if DLC active)

**Estimated Time:** 3-5 days (more rooms, custom prefabs)
**Dependencies:** None (pure XML)
**Success Criteria:**

- Clean, modern-looking base with metal tiles
- All custom rooms generate correctly
- Biotech rooms conditionally spawn
- No ancient/deserted aesthetics

---

### Phase 3.2: Harmony Patch for Layout Override

**Goal:** Make TradersGuild settlements use custom LayoutDef

**Tasks:**

1. **Create `Source/Patches/MapGeneration/GenStepOrbitalPlatformGenerate.cs`**

   - Prefix patch on `GenStep_OrbitalPlatform.Generate()`
   - Detect TradersGuild faction
   - Override `layoutDef` → `TradersGuild_OrbitalSettlement`
   - Log patch activity

2. **Initialize settlement component**

   - Create `Source/WorldObjects/TradersGuildSettlementComponent.cs`
   - Tracks `lastCargoRefreshTicks` for cargo refresh system
   - Add component to settlements on generation

3. **Test**
   - Verify custom layout used for TradersGuild only
   - Other factions unaffected

**Estimated Time:** 1 day
**Dependencies:** Phase 3.1 complete
**Success Criteria:** Custom layout applies to TradersGuild settlements

---

### Phase 3.3: Enhanced Pawn Generation

**Goal:** Improve TradersGuild defender gear quality (quick win before tackling cargo system)

**Tasks:**

1. **Update existing PawnKindDefs** (or create enhanced variants)

   - Improve gear quality ranges to good-excellent
   - Add basic bionics (10% chance)
   - Better weapon selection (remove weak weapons)
   - Consider adding elite variant (higher combatPower)

2. **Balance testing**
   - Ensure defenders are challenging but fair
   - Match difficulty to anticipated cargo loot value

**Estimated Time:** 1-2 days
**Dependencies:** Phase 3.2 complete
**Success Criteria:** Defenders have improved gear, feel appropriately challenging

---

### Phase 3.4: Shuttle Bay Cargo Spawning (Initial Generation) ⭐ KEY FEATURE

**Goal:** Spawn cargo from settlement's trade inventory into shuttle bay

**Tasks:**

1. **Create `Source/BaseGen/SymbolResolver_TradersGuildShuttleBay.cs`**

   - Access settlement's `Settlement_TraderTracker` stock
   - Trigger lazy stock generation if needed: `TryGenerateStock()`
   - Calculate cargo value budget:
     ```csharp
     float cargoPercentage = ModSettings.cargoInventoryPercentage; // 60% default
     float totalInventoryValue = CalculateInventoryValue(settlement.trader.stock);
     float cargoBudgetValue = totalInventoryValue * cargoPercentage;
     ```
   - Select items from trade inventory until budget reached:
     - Random selection (or prioritize by category diversity)
     - Preserve stack integrity (don't split stacks if possible)
     - Remove selected items from trade inventory
   - Spawn selected items in shuttle bay
   - Tag cargo items for later despawn (custom ThingComp or region marker)

2. **Add mod setting for cargo percentage**

   - "Cargo Bay Inventory Percentage" slider (30-100%, default 60%)
   - Description: "Percentage of trader's stock that appears as cargo in shuttle bay. Removed from trade inventory for balance."

3. **Hook SymbolResolver to OrbitalTransportRoom**

   - Update LayoutDef XML to use custom resolver
   - Alternatively: Harmony postfix on vanilla OrbitalTransportRoom resolver

4. **Test dynamic cargo generation**
   - Verify cargo spawns from actual trade inventory
   - Confirm items removed from trade inventory
   - Test with all 4 vanilla traders
   - Test with modded traders (if available)
   - Verify quantities feel balanced

**Estimated Time:** 3-4 days
**Dependencies:** Phase 3.3 complete
**Success Criteria:** Shuttle bay contains items from trade inventory; trade inventory reduced accordingly

**Key Advantages:**

- ✅ Automatic mod compatibility (uses any trader's stock)
- ✅ Consistency: stolen cargo = reduced trade inventory
- ✅ Emergent gameplay: sell items, attack, steal them back!
- ✅ No hardcoded item lists needed

---

### Phase 3.5: Cargo Refresh System (Map Revisit) ⭐ CRITICAL FEATURE

**Goal:** Despawn old cargo and spawn new cargo when player revisits after trader rotation

**Tasks:**

1. **Create `Source/MapComponents/TradersGuildCargoRefresher.cs`**

   - MapComponent that runs on map entry
   - Check if `lastCargoRefreshTicks < lastStockGenerationTicks`
   - If true: Trader rotation occurred while player away
   - Despawn logic:
     - Find shuttle bay region/room
     - Despawn all tagged cargo items
     - Clean up any animals/pawns that were cargo
     - **Crucially: Do NOT restore items to trade inventory** (they were already "sold" as cargo)
   - Respawn logic:
     - Settlement has regenerated stock since last visit (vanilla behavior)
     - Call SymbolResolver logic to spawn new cargo from current trade inventory
     - Remove selected items from trade inventory (same as Phase 3.4)
   - Update `lastCargoRefreshTicks = lastStockGenerationTicks`

2. **Implement cargo tagging system**

   - Option A: Custom ThingComp marking cargo items
   - Option B: Store cargo item IDs in settlement component
   - Option C: Use room region to identify cargo zone
   - Choose based on simplicity vs reliability

3. **Anti-exploit protection**

   - Ensure refresh only happens when rotation occurs
   - Prevent farming by repeatedly entering/exiting
   - Log refresh events for debugging

4. **Test cargo refresh scenarios**
   - **Scenario 1:** Generate map with Exotic trader → leave → wait 15+ days → re-enter → verify new cargo from new trader type
   - **Scenario 2:** Trade with settlement → attack → verify traded items NOT in cargo bay
   - **Scenario 3:** Sell yayo → attack immediately → verify yayo IS in cargo bay
   - **Scenario 4:** Steal half the cargo → retreat → improve goodwill → trade → verify stolen items missing from trade inventory
   - Test multiple rotation cycles (3+)

**Estimated Time:** 3-4 days
**Dependencies:** Phase 3.4 complete
**Success Criteria:** Cargo refreshes correctly; trade inventory consistency maintained

**Key Design Note:**

- Old cargo is NOT restored to trade inventory on despawn (it was "sold" when spawned)
- New cargo comes from current trade inventory (which vanilla regenerated)
- This creates emergent gameplay: player actions affect both cargo AND trade inventory

---

### Phase 3.6: Biocoding System (Optional)

**Goal:** Apply biocoding to some defender gear to balance loot value

**Tasks:**

1. **Research biocoding** (Biotech DLC feature)

   - Check if player has Biotech DLC
   - Investigate `CompBiocodable` usage

2. **Implement biocoding application**

   - Harmony postfix on pawn gear generation
   - Apply to 30-50% of weapons/armor
   - Add mod settings for configuration

3. **Test**
   - Verify mood penalties work
   - Ensure configurable via settings

**Estimated Time:** 1-2 days
**Dependencies:** Phase 3.5 complete (optional)
**Success Criteria:** Biocoding reduces loot value appropriately

---

### Phase 3.7: Balance & Polish

**Goal:** Final testing, bug fixes, and documentation

**Tasks:**

1. **Comprehensive testing**

   - Test all 4 vanilla trader types
   - Test cargo refresh after multiple rotations
   - Verify no exploit opportunities
   - Test mod compatibility e.g. modded trader type

2. **Balance adjustments**

   - Tune cargo quantities
   - Adjust defender strength if needed
   - Consider raid points multiplier (1.5x default)

3. **Bug fixes**

   - Address generation errors
   - Fix cargo despawn/respawn issues
   - Resolve any performance problems

4. **Documentation**
   - Update CLAUDE.md with Phase 3 details
   - Document cargo refresh system
   - Update PLAN.md status

**Estimated Time:** 2-3 days
**Dependencies:** Phase 3.4 complete (3.5-3.6 optional)
**Success Criteria:** Phase 3 feature-complete, stable, polished

---

## Future Stretch Goals (Lowest Priority)

### Stretch Goal 1: Themed Freezer Contents

**Description:** Freezer contains food items from trader's inventory categories

**Implementation:**

- Query trader type's `stockGenerators` for food categories
- Exotic trader: Lavish meals, insect jelly, ambrosia
- Bulk trader: Pemmican, packaged survival meals
- Combat supplier: MREs, survival rations
- Pirate merchant: Mixed quality food, some spoiled?

**Estimated Time:** 1 day
**Dependencies:** Phase 3.7 complete

---

### Stretch Goal 2: Faction Leader Presence

**Description:** TradersGuild "Magister of Trade" present in some settlements

**Implementation:**

- 10-20% chance of faction leader being at settlement
- Spawns in Command Center or Luxury Suite
- Has best gear, unique name
- Killing has reputation consequences

**Estimated Time:** 1-2 days
**Dependencies:** Phase 3.8 complete

---

### Stretch Goal 3: Mod Content Integration

**Description:** Detect and integrate content from other mods

**Examples:**

- **VE Chemfuel:** Add fuel tank room with valves to pipes to landing areas
- **VE Androids:** Add androids to xenotype ratios, add neutroamine tank room with valves to pipes to landing areas
- **VE Nutrient Paste:** Add nutrient paste room with valves to pipes to landing areas and dining rooms to paste dispensers
- **VE Furniture:** Use spacer-tier furniture options instead
- **Automated Hydroponics:** Replace hydroponics room contents with Automated Hydroponics?
- **Combat Extended:** Adjust ammo types in Combat Supplier armory (lowest priority)

**Implementation:**

- Use `ModsConfig.IsActive("ModPackageId")` checks
- Conditional XML patches or C# logic
- Document compatible mods in About.xml

**Estimated Time:** Variable (1-2 days per mod)
**Dependencies:** Phase 3 complete

---

### Stretch Goal 4: Pocket Map Cargo Vault

**Description:** Implement pocket map vault for main cargo storage with hackable door access

**Rationale:**

- More immersive cargo storage (secured vault separate from main deck)
- Prevents animals/slaves from wandering off (pocket maps are pawn-only entry)
- Clear inventory sync mechanism on map exit
- Adds gameplay depth (hackable security door)

**Implementation:**

1. **Pocket Map Generation:**

   - Large single room with shelving
   - Spawn ~60% of trade inventory on shelves (majority of Phase 3.4 cargo)
   - Animals, slaves, prisoners spawn in pocket map (can't escape)

2. **Main Deck Integration:**

   - Hackable vault door in shuttle bay (Odyssey mechanic)
   - Smaller random cargo on main deck (~10-20% of total)
   - Thematic: some cargo lost in chaos regardless of theft

3. **Inventory Sync on Map Exit:**

   - Items left on shelves in pocket map = restored to trade inventory
   - Non-shelf items (player dropped/moved) = NOT restored
   - Prisoner beds remain (slave quarters)
   - All other items removed from trade inventory

4. **Anti-Exploit:**
   - Pocket map contents refresh on trader rotation (same as main deck)
   - Track lastCargoRefreshTicks per settlement

**Estimated Time:** 1 week (complex feature)
**Dependencies:** Phase 3.5 complete, pocket map research
**Technical Challenges:**

- Pocket map generation from SymbolResolver
- Item-to-shelf association tracking
- Syncing pocket map state with settlement trader inventory
- Handling edge cases (shelf destruction, pawn death)

---

### Stretch Goal 5: Dynamic Combat - Reinforcement Events

**Description:** Add reinforcement mechanics for more dynamic settlement combat

**Rationale:**

- Prevents all defenders from rushing to breach point
- More interesting tactical scenarios
- Fits TradersGuild theme (calling for backup)

**Implementation Options:**

1. **Transport Pod Reinforcements:**

   - Trigger when 50% defenders killed OR 5 minutes elapsed
   - Additional defenders drop via transport pods (scaled by raid points etc)
   - Land near player or strategic locations
   - Uses existing vanilla mechanics

2. **Patrol Groups:**

   - Some defenders patrol specific zones (won't rush breach)
   - Player must clear room-by-room vs single killbox fight
   - Custom pawn AI behaviors

3. **Alarm System:**
   - Silent alarm triggered on breach detection
   - Escalating waves of reinforcements
   - Optional: player can hack security station to disable

**Estimated Time:** 3-5 days (depending on complexity)
**Dependencies:** Phase 3.3 complete (Enhanced Pawn Generation)

---

### Stretch Goal 6: Defensive Structures

**Description:** Add defensive fortifications to hostile settlements

**Implementation:**

- Sandbag barriers in corridors
- Barricades near entrance points
- Defensive turret positions (manned or automated)
- Kill boxes or choke points
- Reflects that defenders are expecting attack

**Estimated Time:** 2-3 days
**Dependencies:** Phase 3 complete

---

### Stretch Goal 5: Space Environment Hazards

**Description:** Add environmental challenges beyond combat

**Examples:**

- Hull breach events (sections exposed to vacuum)
- Damaged life support systems (temperature extremes)
- Explosive decompression traps
- Artificial gravity fluctuations (movement speed debuffs)

**Implementation:**

- Custom incident defs
- Environmental effects during combat
- Terrain hazards
- High complexity, significant testing needed

**Estimated Time:** 1 week+
**Dependencies:** Phase 3 complete, extensive testing

---

## Reference Data

### Key Vanilla Files

**Map Generation:**

```
/Data/Odyssey/Defs/MapGeneration/SpaceMapGenerator.xml
  - SettlementPlatform MapGeneratorDef (line 72-79)
  - GenStep_OrbitalPlatform definition (line 82-91)

/Data/Odyssey/Defs/LayoutDefs/Layouts_OrbitalPlatform.xml
  - OrbitalSettlementPlatform LayoutDef (line 159-194)
  - Room definitions and counts
```

**Faction Definitions:**

```
/Data/Odyssey/Defs/FactionDefs/Factions_Misc.xml
  - TradersGuild FactionDef (line 4-148)
  - Pawn groups, trader kinds, tech level

/Data/Odyssey/Defs/PawnKinds/PawnKinds_TradersGuild.xml
  - Existing pawn kinds for faction
```

**World Objects:**

```
/Data/Odyssey/Defs/WorldObjectDefs/WorldObjects.xml
  - SpaceSettlement definition (line 43-55)
  - MapGenerator reference
```

### Vanilla Orbital Trader Types

**From TradersGuild FactionDef (line 100-105):**

```xml
<orbitalTraderKinds>
  <li>Orbital_BulkGoods</li>
  <li>Orbital_CombatSupplier</li>
  <li>Orbital_Exotic</li>
  <li>Orbital_PirateMerchant</li>
</orbitalTraderKinds>
```

**Trader Characteristics:**

- **Orbital_BulkGoods:** Building materials, textiles, basic resources
- **Orbital_CombatSupplier:** Weapons, armor, combat equipment
- **Orbital_Exotic:** Artwork, rare items, luxury goods, exotic creatures
- **Orbital_PirateMerchant:** Contraband, organs, slaves, drugs

### RimWorld APIs & Classes

**Map Generation:**

- `GenStep_OrbitalPlatform` - Primary generation class
- `StructureLayoutDef` - XML definition for base layouts
- `RoomDef` - XML definition for individual rooms
- `SymbolResolver` - C# classes for precise generation control
- `BaseGen` - Namespace containing generation utilities

**Item Spawning:**

- `ThingMaker.MakeThing()` - Create items
- `GenSpawn.Spawn()` - Place items on map
- `StockGenerator` - Defines trader inventory categories

**Pawn Generation:**

- `PawnKindDef` - Defines pawn types, gear, stats
- `PawnGenerator.GeneratePawn()` - Create pawns
- `CompBiocodable` - Biocoding component (Biotech DLC)

**Faction System:**

- `Faction` - Faction instance
- `Settlement` - World map settlement object
- `Settlement_TraderTracker` - Manages trader state

**Harmony Patching:**

- `[HarmonyPatch]` - Attribute for marking patches
- `[HarmonyPrefix]` - Runs before original method
- `[HarmonyPostfix]` - Runs after original method

### Mod File Structure

```
BetterTradersGuild/
├── About/
│   └── About.xml           # Mod metadata (Harmony + Odyssey DLC dependencies)
├── Assemblies/
│   └── BetterTradersGuild.dll  # Compiled mod DLL
├── Defs/
│   ├── LayoutDefs/
│   │   └── BTG_OrbitalSettlement.xml  # Custom settlement layout (Phase 3)
│   ├── LayoutRoomDefs/         # Custom room definitions (18 files, Phase 3)
│   │   ├── BTG_OrbitalArmory.xml
│   │   ├── BTG_OrbitalBarracks.xml
│   │   ├── BTG_OrbitalCaptainsQuarters.xml  # 🚧 IN PROGRESS
│   │   ├── BTG_OrbitalCargoStorage.xml
│   │   ├── BTG_OrbitalClassroom.xml
│   │   ├── BTG_OrbitalComputerRoom.xml
│   │   ├── BTG_OrbitalCorridor.xml
│   │   ├── BTG_OrbitalDiningRoom.xml
│   │   ├── BTG_OrbitalHydroponics.xml
│   │   ├── BTG_OrbitalMedicalBay.xml
│   │   ├── BTG_OrbitalNursery.xml
│   │   ├── BTG_OrbitalRecRoom.xml
│   │   ├── BTG_OrbitalSecurityStation.xml
│   │   ├── BTG_OrbitalStoreroom.xml
│   │   ├── BTG_OrbitalTradeShowcase.xml
│   │   ├── BTG_OrbitalTransportRoom.xml
│   │   └── BTG_OrbitalWorkshop.xml
│   └── PrefabDefs/             # Custom prefab definitions (10 files, Phase 3)
│       ├── BTG_ArmchairsWithPlantpot_Edge.xml
│       ├── BTG_BarracksBeds_Edge.xml
│       ├── BTG_BilliardsTable.xml
│       ├── BTG_CaptainsBedroom.xml
│       ├── BTG_CaptainsBookshelf_Edge.xml
│       ├── BTG_ClassroomBookshelf.xml
│       ├── BTG_FlatscreenTelevisionWolfLeather_Edge.xml
│       ├── BTG_HospitalBeds_Edge.xml
│       ├── BTG_HydroponicHealroot.xml
│       └── BTG_MedicineShelf_Edge.xml
├── Patches/                    # Empty (reserved for XML patches)
├── Source/
│   ├── Core/
│   │   ├── ModInitializer.cs       # Harmony patching and startup
│   │   └── ModSettings.cs          # Mod configuration UI
│   ├── Helpers/
│   │   ├── TileHelper.cs                   # World map tile utilities
│   │   ├── TradersGuildHelper.cs           # Faction/settlement checking
│   │   └── TradersGuildTraderRotation.cs   # Trader rotation timing logic
│   ├── Patches/                # Harmony patches (organized by target type)
│   │   ├── Caravan/
│   │   │   └── CaravanGetGizmos.cs
│   │   ├── CaravanArrivalActions/
│   │   │   ├── CaravanArrivalActionAttackGetFloatMenuOptions.cs
│   │   │   └── CaravanArrivalActionTradeGetFloatMenuOptions.cs
│   │   ├── Debug/
│   │   │   └── RoomContentsWorkerFillRoom.cs  # Debug logging for map gen
│   │   ├── MapGeneration/
│   │   │   └── GenStepOrbitalPlatformGenerate.cs  # Phase 3 layout override
│   │   ├── PlanetTile/
│   │   │   └── PlanetTileLayerDef.cs
│   │   ├── Settlement/         # Phase 2 trading system patches (9 files)
│   │   │   ├── SettlementGetCaravanGizmos.cs
│   │   │   ├── SettlementGetFloatMenuOptions.cs
│   │   │   ├── SettlementGetInspectString.cs
│   │   │   ├── SettlementGetShuttleFloatMenuOptions.cs
│   │   │   ├── SettlementTraderTrackerGetTraderKind.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStock.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStockAlignment.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStockEveryDays.cs
│   │   │   └── SettlementVisitable.cs
│   │   ├── WorldGrid/
│   │   │   ├── WorldGridFindMostReasonableAdjacentTile.cs
│   │   │   └── WorldGridGetRoadMovementDifficulty.cs
│   │   └── WorldObject/
│   │       └── WorldObjectRequiresSignalJammer.cs
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   ├── RoomContents/           # Custom room generation workers (Phase 3)
│   │   └── RoomContents_CaptainsQuarters.cs  # 🚧 IN PROGRESS
│   ├── WorldObjects/           # World object components (Phase 3)
│   │   └── TradersGuildSettlementComponent.cs  # Cargo refresh tracking
│   ├── BetterTradersGuild.csproj  # SDK-style project file
│   └── BetterTradersGuild.sln     # Visual Studio solution
├── docs/                       # Technical documentation
│   ├── CAPTAINS_QUARTERS_IMPLEMENTATION.md
│   ├── CARGO_IMPLEMENTATION_GUIDE.md
│   ├── EDGEONLY_LIMITATIONS.md
│   ├── LAYOUT_CONSTRAINTS_README.md
│   ├── PREFAB_EDGEONLY_GUIDE.md
│   ├── STORAGE_API_RESEARCH.md
│   ├── STORAGE_API_SUMMARY.txt
│   ├── STORAGE_DOCUMENTATION_INDEX.md
│   ├── STYLING_QUICK_REF.md
│   └── STYLING_RESEARCH.md
├── CLAUDE.md                   # Developer guidance for Claude Code
├── PLAN.md                     # Development roadmap (THIS FILE)
└── README.md                   # GitHub repository landing page
```

**Note:** Files marked "🚧 IN PROGRESS" are functional but have incomplete features. See individual file comments or implementation docs for details.

---

## Development Notes

### Design Principles

1. **Thematic Consistency:** Everything should reinforce TradersGuild's identity as prosperous space merchants
2. **Preview Consistency:** Generated map must match world map preview trader type (Phase 2 integration)
3. **Mod Compatibility:** Use vanilla systems where possible, avoid breaking other mods
4. **Configurability:** Let players tune difficulty/biocoding to their preference
5. **Dynamic Support:** System should work with modded traders automatically

### Common Pitfalls to Avoid

- **MAP PERSISTENCE CONSTRAINT** - Never change room structure/layout based on trader type; maps are saved permanently and load from disk on revisit
- **Don't break Phase 2 features** - Peaceful trading visits must still work
- **Don't hardcode trader types** - Use `TraderKindDef` data for mod compatibility
- **Don't ignore vacuum environment** - Verify gear/pawns work in space
- **Don't make loot game-breaking** - Balance against increased difficulty
- **Don't forget cargo refresh tracking** - Must prevent exploit farming via lastCargoRefreshTicks
- **Don't forget performance** - Large inventory spawns can lag, optimize

### Testing Checklist

- [ ] All 4 vanilla trader types spawn correct cargo
- [ ] World map preview matches initial generated map cargo
- [ ] Cargo refreshes correctly on revisit after rotation
- [ ] Cargo does NOT refresh on revisit without rotation (anti-exploit)
- [ ] Multiple rotations work correctly (test 3+ cycles)
- [ ] Peaceful trading still works (Phase 2 features)
- [ ] Base layout uses modern aesthetics (not ancient/deserted)
- [ ] Shuttle bay cargo is balanced and thematic
- [ ] No errors in RimWorld log files
- [ ] Compatible with other popular mods (test sample)
- [ ] Performance is acceptable (cargo despawn/respawn is fast)

---

## Summary: Implementation Order

**Phase 3 Breakdown (7 sub-phases):**

1. 🚧 **Static Base Layout (XML)** - 1-2 days - **START HERE**

   - Create custom LayoutDef using vanilla RoomDefs
   - Modern aesthetics (not ancient/deserted)

2. 🔮 **Harmony Layout Override** - 1 day

   - Patch map generation to use custom layout
   - Initialize settlement component for cargo tracking

3. 🔮 **Enhanced Pawns** (Quick Win) - 1-2 days

   - Improve defender gear quality
   - Add elite variants
   - **Moved earlier for quick visible results**

4. 🔮 **Shuttle Bay Cargo Spawning** - 3-4 days - **KEY FEATURE**

   - **Dynamic inventory-based cargo generation**
   - Pull ~60% of trade inventory into cargo bay
   - Remove items from trade inventory for balance
   - Tag items for later despawn

5. 🔮 **Cargo Refresh System** - 3-4 days - **CRITICAL FEATURE**

   - Despawn/respawn cargo on map revisit after rotation
   - Pull from regenerated trade inventory
   - Anti-exploit protection
   - Emergent gameplay: stolen cargo = reduced trade inventory

6. 🔮 **Biocoding** (Optional) - 1-2 days

   - Apply biocoding to balance loot

7. 🔮 **Balance & Polish** - 2-3 days
   - Testing, bug fixes, documentation

**Total Estimated Time:** 1-2 weeks for core features (phases 1-5), 2-3 weeks with optional features

**Key Design Decisions:**

- **Dynamic cargo from trade inventory** - No hardcoded manifests, automatic mod compatibility
- **Trade/cargo consistency** - Stolen items reduce trade inventory, sold items appear in cargo
- **Static base + ONE dynamic bay** - No trader-specific themed rooms
- **Cargo refresh system** - Main technical complexity, enables trader rotation to matter

**Stretch Goals (lowest priority):**

- Category-based item top-ups in other rooms (e.g., medicine in medical bay)
- Faction Leader presence in some settlements
- Defensive structures (sandbags, barricades)
- Mod content integration (VE Furniture, Alpha Animals, etc.)

---

_This plan document is a living document and will be updated as Phase 3 progresses. Last updated: [Current Date]_

