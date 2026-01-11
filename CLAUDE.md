# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Better Traders Guild** is a RimWorld 1.6 mod that expands player interactions with the Traders Guild faction. The mod enables peaceful trading visits to Traders Guild bases in orbit, improves their map generation (planned), and adds additional goodwill quests/opportunities (planned).

**Key Technologies:**

- C# (.NET Framework 4.7.2)
- Harmony library for runtime patching
- RimWorld modding API
- XML definitions (for future phases)

**Dependencies:**

- RimWorld 1.6 (Odyssey DLC required)
- Harmony

## Build and Development Commands

### Building the Project

**Using .NET CLI (Recommended - works from project root):**

```bash
# From project root - builds both main project and test project
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

# Build solution (both projects)
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

**Using MSBuild directly (alternative):**

```bash
# Navigate to Source directory
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild/Source"

# Build in Debug mode (captures all errors and warnings)
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" BetterTradersGuild.csproj /p:Configuration=Debug

# Build in Release mode
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" BetterTradersGuild.csproj /p:Configuration=Release

# Clean build artifacts
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" BetterTradersGuild.csproj /t:Clean /p:Configuration=Debug
```

**From Windows (CMD/PowerShell):**

```bash
cd "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BetterTradersGuild"
dotnet build

# Or from Source directory
cd Source
msbuild BetterTradersGuild.csproj /p:Configuration=Debug
```

**Output:** Compiled DLL is placed in `Assemblies/BetterTradersGuild.dll`

**Build Output:** All compilation errors and warnings are visible directly in the terminal output with file paths, line numbers, and error codes (e.g., `CS0246`, `CS0219`).

### Testing

**Using bash script (Recommended for WSL):**

```bash
# From project root - builds and runs tests using VSTest.Console.exe
bash Scripts/run-tests.sh
```

The script:

- Builds the solution using `dotnet build`
- Runs tests using VSTest.Console.exe directly (much faster on WSL than `dotnet test`)
- Currently runs only `PlacementCalculatorTests` (other test files are excluded from the test project)

**Using .NET CLI (works on Windows, slow on WSL):**

```bash
# From project root
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

⚠️ **WSL Note:** `dotnet test` has timeout issues on WSL with .NET Framework 4.7.2 projects due to protocol negotiation failures between the .NET CLI (Linux) and testhost.exe (Windows). Use `./Scripts/run-tests.sh` instead for reliable test execution.

**Excluded Test Files:**

- `Tests/Tools/RegenerateDiagrams.cs` - Utility tool with Main method, not a test file
- `Tests/Helpers/DiagramGeneratorTests.cs` - Additional tests not currently in use

These are excluded via `<Compile Remove="..." />` in `Tests/BetterTradersGuild.Tests.csproj`.

**Manual Testing:**

- In-game testing is done in RimWorld with Dev Mode enabled
- The mod loads automatically from the Steam mods directory when RimWorld launches

### Project Structure

**Root Directory:**
- `About/` - Mod metadata (`About.xml` with dependencies, load order)
- `Assemblies/` - Compiled DLL output (auto-generated)
- `1.6/` - Versioned game content (RimWorld version-specific)
- `Source/` - C# source code (SDK-style project with auto-include)
- `Tests/` - XUnit test project
- `Scripts/` - Shell scripts for WSL development (build, test runners)
- `docs/` - Technical documentation and implementation guides

**Versioned Content (`1.6/`):**

RimWorld mods use version folders for compatibility. All XML content lives here:
- `Defs/` - Custom XML definitions organized by def type:
  - `LayoutDefs/` - Settlement layout definitions
  - `LayoutRoomDefs/` - Room definitions for layouts
  - `PrefabDefs/` - Furniture/object placement prefabs (subdirs by room type, e.g., `MessHall/`, `CrewQuarters/`)
  - `RoomPartDefs/` - Room part configurations
  - `ThingDefs/` - Custom thing definitions
  - `MapGeneration/` - Map generation configurations:
    - `MapGeneratorDefs/` - Custom MapGeneratorDef XMLs (one file per def)
    - `GenStepDefs/` - Custom GenStepDef XMLs (one file per def)
- `Patches/` - XML patches that modify other mods/vanilla (named by target, e.g., `PawnKinds_*.xml`)

**Source Code (`Source/1.6/`):**

Organized by concern with domain-specific subdirectories:
- `Core/` - Mod initialization (`ModInitializer.cs`) and settings (`ModSettings.cs`)
- `DefRefs/` - **Static DefOf-style classes** for cached def references (e.g., `Things.cs`, `PawnKinds.cs`, `Prefabs.cs`). Each file contains a static class with `[DefOf]` attribute or manual `DefDatabase<T>.GetNamed()` lookups.
- `Helpers/` - Reusable utilities grouped by domain:
  - `MapGeneration/` - Hidden pipe detection helpers (VE Framework integration)
  - `RoomContents/` - Placement calculators, shelf/plant/bookcase helpers
  - Root helpers for faction checking, tile utilities, trader rotation
- `Patches/` - Harmony patches grouped by target RimWorld type (e.g., `Settlement/`, `MapGeneration/`, `Caravan/`). One patch class per file, named after the method patched.
- `RoomContents/` - Custom `RoomContentsWorker` implementations (named `RoomContents_<RoomName>.cs`)
- `RoomParts/` - Custom `RoomPartWorker` implementations
- `MapGeneration/` - Custom GenStep classes (XML-configurable terrain, lighting, pipes, drones)
- `LayoutWorkers/` - Custom LayoutWorker and settlement generation helpers (conduit placement, pipe networks)
- `WorldObjects/` - World object components

**Naming Conventions:**
- Patch files: `<ClassName><MethodName>.cs` (e.g., `SettlementVisitable.cs`)
- Room workers: `RoomContents_<RoomDefName>.cs`
- DefRefs: Plural noun matching the def type (e.g., `Things.cs`, `Terrains.cs`)
- PrefabDefs: `<Description>_Edge.xml` suffix indicates edge-placement prefabs

**Tests (`Tests/`):**
- Mirrors source structure where applicable
- `Helpers/` - Test utilities (diagram generation)
- Excluded files configured in `.csproj` (utilities with `Main` methods)

## Architecture and Key Concepts

### Code Organization

The codebase is organized into these main areas:

1. **Core/** - Mod initialization and settings (Harmony patching, configuration UI)

2. **DefRefs/** - Static cached references to game definitions. Each file corresponds to a def type (Things, PawnKinds, Terrains, etc.) and provides strongly-typed access via `DefDatabase<T>.GetNamed()` lookups or `[DefOf]` attributes.

3. **Helpers/** - Reusable utility classes organized by domain:
   - Root helpers for faction/settlement checking, world tile utilities, trader rotation logic
   - `MapGeneration/` - Hidden pipe detection (VE Framework integration)
   - `RoomContents/` - Placement calculation, shelf/plant/bookcase population

4. **Patches/** - Harmony patches organized by target RimWorld type. Each subdirectory groups patches by the class they modify (e.g., `Settlement/`, `MapGeneration/`). Namespaces use `*Patches` suffix to avoid conflicts with RimWorld types.

5. **RoomContents/** - Custom `RoomContentsWorker` implementations that run after room generation to populate furniture, spawn items, and customize room contents.

6. **RoomParts/** - Custom `RoomPartWorker` implementations for room-level spawning logic.

7. **MapGeneration/** - Custom GenStep classes (XML-configurable terrain, lighting, pipes, drones).

8. **LayoutWorkers/** - Custom LayoutWorker and settlement generation helpers.

**Important:** Namespace conflicts can occur when patch namespaces match RimWorld type names. Always use the `*Patches` suffix pattern (e.g., `SettlementPatches`, `CaravanPatches`) to avoid compilation errors.

### Mod Initialization

The mod uses `[StaticConstructorOnStartup]` attribute on the `BetterTradersGuildMod` class to automatically initialize when RimWorld loads. The static constructor applies all Harmony patches defined in the assembly and logs a single initialization message with the total patch count (e.g., `"[Better Traders Guild] Mod initialized with 17 Harmony patches applied."`).

**Settings Access:** The `BetterTradersGuildMod.Settings` static property provides global access to mod configuration throughout patches and helpers.

### Map Generation Architecture

BTG uses a declarative, XML-driven approach for custom map generation:

**Core Concepts:**

- **MapGeneratorDef**: Defines the complete generation pipeline for a map type (list of GenSteps to run)
- **GenStepDef**: Individual generation step with order value (lower = runs first) and configurable parameters
- **SpaceMapGenerator**: Vanilla parent def providing space map properties (`defaultUnderGridTerrain: Space`, `renderWorld: true`, etc.)

**BTG MapGeneratorDefs:**

| Def | Purpose | Parent |
|-----|---------|--------|
| `BTG_SettlementMapGenerator` | TradersGuild orbital settlements | `SpaceMapGenerator` |
| `BTG_CargoHold` | Cargo hold pocket maps | `SpaceMapGenerator` |

**BTG GenSteps (Settlement Pipeline):**

| GenStep | Order | Purpose |
|---------|-------|---------|
| `BTG_SettlementPlatform` | 200 | Core structure via `GenStep_OrbitalPlatform` with BTG layout |
| `BTG_ReplaceTerrain` | 250 | Replace AncientTile → MetalTile |
| `BTG_PaintTerrain` | 255 | Paint terrain with BTG_OrbitalSteel color |
| `BTG_LandingPadPipes` | 260 | Extend VE pipes to landing pads (graceful no-op if no VE) |
| `BTG_SetWallLampColor` | 265 | Set WallLamp glow to blue |
| `BTG_SettlementPawnsLoot` | 700 | Pawn spawning (loot disabled via `lootMarketValue: 0~0`) |
| `BTG_SentryDrones` | 705 | Sentry drone spawning (uses ModSettings) |

**Swapping MapGeneratorDef:**

To use a custom MapGeneratorDef for TradersGuild settlements, we patch `Settlement.MapGeneratorDef` property getter:

```csharp
[HarmonyPatch(typeof(Settlement))]
[HarmonyPatch(nameof(Settlement.MapGeneratorDef), MethodType.Getter)]
```

**IMPORTANT:** `Settlement` overrides `MapParent.MapGeneratorDef`, so patching `MapParent` won't work - must patch `Settlement` directly.

**SpaceMapGenerator Inheritance:**

Space maps inherit from `SpaceMapGenerator` to get:
- `defaultUnderGridTerrain: Space` - fills void areas automatically
- `renderWorld: true` - planet renders behind space terrain
- `disableCallAid: true`, `disableMapClippers: true`, `ignoreAreaRevealedLetter: true`

Child defs only need to specify `genSteps` and any overrides (e.g., `pocketMapProperties` for pocket maps).

### Trader Rotation System

The mod implements a sophisticated virtual schedule system for trader rotation:

**Virtual Schedules:**

- Each settlement has a deterministic rotation schedule based on its ID
- Settlement ID offset (using prime multiplier: 123457) desynchronizes rotation across settlements
- Unvisited settlements show stable previews that match what they'll get when visited
- Rotation interval is player-configurable (5-30 days, default 15)

**Three-Patch Architecture:**

The trader rotation system requires three Harmony patches working together to solve critical synchronization issues:

1. **SettlementTraderTrackerGetTraderKind.cs** (Postfix on `TraderKind` getter)

   - Provides weighted random orbital trader selection
   - Uses deterministic seed: `Hash(settlementID, lastStockGenerationTicks)`
   - Checks flags from other patches to determine which tick value to use
   - Implements caching to avoid recalculation every frame

2. **SettlementTraderTrackerRegenerateStock.cs** (Prefix/Postfix on `RegenerateStock()`)

   - **ESSENTIAL** - Sets thread-local flag during stock regeneration
   - Solves stock/dialog desync problem (see below)
   - Cannot be removed without breaking trader type consistency
   - Exposes `IsRegeneratingStock(settlementID)` for other patches

3. **SettlementTraderTrackerRegenerateStockAlignment.cs** (Prefix/Postfix on `RegenerateStock()`)
   - Aligns first-visit stock generation with virtual preview schedule
   - Solves preview/visit mismatch problem (see below)
   - Exposes `HasPendingAlignment(settlementID)` for other patches
   - Overrides vanilla's `lastStockGenerationTicks = TicksGame` behavior

**Critical Problem #1: Stock/Dialog Desync**

Vanilla `RegenerateStock()` updates `lastStockGenerationTicks` at the END of execution:

```
1. Stock cleared
2. TraderKind getter called (uses OLD lastStockTicks) → Selects Trader A
3. Stock generated for Trader A
4. lastStockGenerationTicks = TicksGame (NEW value)
5. Dialog opens → TraderKind getter called (uses NEW lastStockTicks) → Selects Trader B
```

Result: Dialog shows Trader B title but has Trader A's inventory!

**Solution:** The RegenerateStock patch sets a flag during execution. The TraderKind getter detects this flag and uses `Find.TickManager.TicksGame` (the future value) to ensure both calls select the same trader.

**Critical Problem #2: Preview/Visit Mismatch**

Unvisited settlements use virtual schedules for preview, but first-visit generation uses `TicksGame`:

```
1. Preview calculates: GetVirtualLastStockTicks(ID) = -865481 → Shows Exotic Trader
2. Player visits → RegenerateStock() sets lastStockTicks = TicksGame = 12015
3. Different seeds → Shows Bulk Trader (broken trust!)
```

**Solution:** The Alignment patch detects first-time generation (lastStockTicks == -1), pre-sets the value to virtual schedule, and restores it after vanilla overwrites it with TicksGame.

**How It Works:**

- **Unvisited:** TraderKind getter uses `GetVirtualLastStockTicks(settlementID)` (stable, no flickering)
- **First visit:** Alignment patch aligns to virtual value → TraderKind getter uses aligned value → stock generates → alignment restored
- **Subsequent:** RegenerateStock flag active → TraderKind getter uses `TicksGame` → allows rotation while maintaining sync

### Harmony Patching Strategy

This mod uses **Postfix patches** primarily, with strategic **Prefix patches** where needed:

**Postfix patches** (most common):

- Less invasive and more compatible with other mods
- Allows modifying return values via `ref` parameters
- Can yield additional results in enumerable methods (like adding gizmos)

**Prefix patches** (used for):

- Setting flags before method execution (`RegenerateStock` patches)
- Aligning values before vanilla logic runs (`RegenerateStockAlignment`)
- Never skip original method execution (always return `true` or void)

**Key Patching Targets:**

1. `Settlement.Visitable` - Makes Traders Guild bases visitable with good relations, bypassing signal jammer restrictions
2. `Settlement.GetGizmos` - Adds "Visit for Trade" button to Traders Guild settlements
3. `WorldObject.RequiresSignalJammerToReach` - Overrides signal jammer requirement for friendly visits
4. `PlanetTile.LayerDef` - Allows caravan formation to space-layer settlements
5. `WorldGrid` methods - Prevents InvalidCastException errors when pathfinding to space tiles

### RimWorld-Specific Patterns

**Gizmos (UI Buttons):**

- Created via `Command_Action` class
- Yielded in `GetGizmos()` postfix patches
- Include label, description, icon, action delegate, and disabled state

**World Map Interactions:**

- Settlements exist on the world map as `Settlement` objects
- Player caravans travel between tiles using `Dialog_FormCaravan`
- Space settlements require special handling due to `PlanetLayerDef.canFormCaravans` restrictions

**Faction Relations:**

- Use `Faction.PlayerRelationKind` enum (Hostile, Neutral, Ally)
- Traders Guild faction defName: `"TradersGuild"`
- Relations checked via `TradersGuildHelper` utility class

### Current Implementation Status (Phase 2)

**Phase 2: Enable Peaceful Trading Visits** - ✅ **COMPLETED**

All Phase 2 objectives have been successfully implemented and tested:

- ✅ Shuttle/caravan visit gizmos implemented
- ✅ Faction relation checks working
- ✅ Signal jammer overrides functional
- ✅ Caravan travel to space settlements enabled
- ✅ Float menu trade options functional
- ✅ **Orbital Trader System fully functional** - TradersGuild settlements dynamically use orbital trader types with proper stock/dialog synchronization

**Orbital Trading System Architecture:**

TradersGuild settlements reuse existing orbital trader types (`Orbital_BulkGoods`, `Orbital_CombatSupplier`, `Orbital_Exotic`, `Orbital_PirateMerchant`) from the faction's `orbitalTraderKinds` definition. This approach:

- **Lore-Friendly:** Aligns with TradersGuild being "orbital traders who form an orbital marketplace"
- **Mod-Compatible:** Automatically supports mods that add custom orbital trader types
- **Dynamic:** Trader type randomly rotates when settlement restocks (every few days)
- **Vanilla-Powered:** Leverages all existing stock generation, pricing, and trading mechanics

**Implementation Details:**

1. **TraderKind Assignment** (`SettlementTraderTrackerGetTraderKind.cs`) - Postfix on `Settlement_TraderTracker.TraderKind` getter with virtual schedule support for unvisited settlements
2. **Rotation Timing** (`TradersGuildTraderRotation.cs`) - Centralized helper calculating settlement-specific virtual schedules with ID-based offsets
3. **Mid-Regeneration Detection** (`SettlementTraderTrackerRegenerateStock.cs`) - Prefix/Postfix on `RegenerateStock()` sets flag during execution
4. **Virtual Schedule Alignment** (`SettlementTraderTrackerRegenerateStockAlignment.cs`) - Aligns first-visit stock generation with virtual preview schedule
5. **Custom Rotation Interval** (`SettlementTraderTrackerRegenerateStockEveryDays.cs`) - Overrides regeneration interval for TradersGuild settlements
6. **Docked Vessel Display** (`SettlementGetInspectString.cs`) - Shows current trader type on world map inspection cards
7. **Player Configuration** (`ModSettings.cs`) - Configurable rotation interval slider (5-30 days, default 15)

**Technical Notes:**

- Space tiles (`PlanetLayerDef.isSpace = true`) normally don't support caravans; solved via runtime `PlanetLayerDef` modification
- `WorldGrid` method patches prevent InvalidCastException when pathfinding to space tiles
- Uses reflection to access private `lastStockGenerationTicks` field in `Settlement_TraderTracker`
- Thread-local flag systems for mid-regeneration detection and alignment tracking
- Settlement ID-based offset (prime multiplier: 123457) desynchronizes trader rotation across settlements
- Virtual schedule alignment ensures preview trader matches first visit
- Negative tick protection prevents early-game edge cases
- Weighted trader selection respects `TraderKindDef.CalculatedCommonality` (commonality × population curve)
- See `PHASE2_TRADING_PLAN.md` for detailed architecture documentation and complete issue resolution history

**Player-Facing Features:**

- **Docked Vessel Display:** Shows current trader type on world map inspection cards (always visible)
- **Configurable Rotation:** Mod Options → "Better Traders Guild" → Slider (5-30 days, default 15)
- **Virtual Schedules:** Unvisited settlements show stable, accurate trader previews
- **Exploration Incentive:** Players can scout the world for specific trader types instead of using comms console

### Helper Classes

Helpers are organized in `Source/1.6/Helpers/` by domain. Key patterns:

**Root Helpers** - Faction/settlement checking, world tile utilities, trader rotation scheduling. The `TradersGuildTraderRotation` helper provides the virtual schedule system for trader rotation.

**MapGeneration Helpers** (`Helpers/MapGeneration/`) - Hidden pipe detection for VE Framework integration.

**LayoutWorker Helpers** (`LayoutWorkers/Settlement/`) - Used by LayoutWorker_BTGSettlement for conduit/pipe placement, tank filling, valve handling, and landing pad pipe extension.

**RoomContents Helpers** (`Helpers/RoomContents/`) - Used by `RoomContentsWorker` implementations for:
- Placement calculation (pure logic, fully unit tested)
- Furniture population (shelves, bookcases, plant pots, outfit stands)
- Post-generation fixups (weapon name regeneration, content customization)

### Current Implementation Status (Phase 3)

**Phase 3: Enhanced Settlement Generation** - 🚧 **IN PROGRESS**

Overhauling TradersGuild settlement map generation to reflect their identity as prosperous space merchants with dynamic cargo that changes based on trader rotation.

**Key Design Constraint - Map Persistence:**

RimWorld maps are **generated once and saved permanently**. When players revisit a settlement, the map loads from disk (not regenerated). This creates a critical architectural constraint:

- ❌ **Can't do:** Change room layouts/structures based on trader type (would break on revisit)
- ✅ **Solution:** Static base infrastructure + ONE dynamic cargo bay that refreshes on trader rotation

**Architecture Overview:**

```
Static Base (Never Changes):
├── Custom LayoutDef using vanilla RoomDefs
├── Modern aesthetics (not ancient/deserted)
├── Command Center, Medical Bay, Barracks, Hydroponics, etc.
└── All furniture/structure is permanent

Dynamic Cargo Bay (Refreshes on Rotation):
├── ONE shuttle bay (ShuttleBay)
├── Cargo pulled from settlement's trade inventory (~60%)
├── Items removed from trade inventory for balance
├── On revisit after rotation: despawn old → spawn new
└── Anti-exploit: only refreshes when trader rotates
```

**Dynamic Inventory-Based Cargo System:**

Instead of hardcoded item lists, cargo is **dynamically generated from the settlement's actual trade inventory**:

1. **On Map Generation:**

   - Settlement trade inventory generated (lazy if needed)
   - Calculate budget: `totalInventoryValue × cargoPercentage` (60% default)
   - Randomly select items from trade inventory
   - Remove selected items from trade inventory
   - Spawn cargo in shuttle bay

2. **On Cargo Refresh (After Rotation):**
   - Vanilla regenerates trade inventory (normal behavior)
   - Old cargo despawned (NOT restored - it was "sold")
   - New cargo selected from new trade inventory
   - Cargo spawned

**Emergent Gameplay Examples:**

- **Sell yayo → attack → steal it back:** If player sells 100 yayo and then attacks, that yayo appears in cargo bay
- **Steal cargo → trade peacefully:** Stolen items are missing from trade inventory
- **Multiple raids:** Each revisit after rotation shows new cargo matching new trader type

**Implementation Components:**

1. **TradersGuild_OrbitalSettlement** (`Defs/LayoutDefs/`) - Custom LayoutDef using vanilla RoomDefs with modern aesthetics
2. **SymbolResolver_TradersGuildShuttleBay** (`Source/BaseGen/`) - Spawns cargo from trade inventory
3. **TradersGuildSettlementComponent** (`Source/WorldObjects/`) - Tracks `lastCargoRefreshTicks` for anti-exploit
4. **TradersGuildCargoRefresher** (`Source/MapComponents/`) - Detects rotation, despawns/respawns cargo on map entry

**Key Advantages:**

- ✅ **Automatic mod compatibility** - Works with any trader type
- ✅ **No hardcoded manifests** - Uses vanilla stock generation
- ✅ **Trade/cargo consistency** - Actions have realistic consequences
- ✅ **Lore-accurate** - Station infrastructure permanent, cargo temporary

**Technical Notes:**

- Uses `Settlement_TraderTracker.stock` for inventory access
- Triggers lazy stock generation: `TryGenerateStock()`
- Cargo items tagged for despawn (ThingComp or region marking)
- Map persistence constraint documented in PLAN.md "Common Pitfalls"
- See PLAN.md Phase 3 section for detailed implementation phases

### Optional Features System

**Phase 3 features are optional and controllable via mod settings.** This provides:

- Player control over which features to enable
- Load order flexibility (no automatic mod detection)
- Save compatibility protection (component only added when cargo enabled)
- Graceful coexistence with other map generation mods

**Mod Settings (Options → Mod Settings → Better Traders Guild):**

1. **Use custom settlement layouts** (bool, default: true)
   - Controls Phase 3.1-3.2 custom BTG_OrbitalSettlement generation
   - When disabled: TradersGuild settlements use vanilla/other mod layouts
   - Settings access: `BetterTradersGuildMod.Settings.useCustomLayouts`

2. **Use enhanced pawn generation** (bool, default: true)
   - Controls Phase 3.3 specialized crew member spawning
   - Grayed out in UI if custom layouts disabled (requires custom rooms)
   - Settings access: `BetterTradersGuildMod.Settings.useEnhancedPawnGeneration`

3. **Cargo bay inventory percentage** (float, 0-100%, default: 60%)
   - Controls Phase 3.4-3.5 dynamic cargo spawning
   - 0% = disabled (no cargo spawns, no TradersGuildSettlementComponent added)
   - Grayed out in UI if custom layouts disabled (requires shuttle bay room)
   - Settings access: `BetterTradersGuildMod.Settings.cargoInventoryPercentage`

**Implementation Pattern:**

Throughout the codebase, use direct settings access (no helper classes):

```csharp
// In GenStep patch - check if custom layouts enabled
if (!BetterTradersGuildMod.Settings.useCustomLayouts)
    return true; // Use vanilla/other mod generation

// In component addition - check if cargo system enabled
if (BetterTradersGuildMod.Settings.cargoInventoryPercentage > 0f)
{
    // Add TradersGuildSettlementComponent
}
```

**Save Compatibility:**

- Custom layouts: LOW RISK - Maps generated once and saved as concrete objects
- Settlement component: HIGH RISK - Only added when cargo percentage > 0 to minimize save corruption if mod removed
- Reflection-based field modification: VERY LOW RISK - One-time operation during generation

For detailed compatibility analysis, see `Docs/COMPATIBILITY_PLAN.md`.

### Future Phases (Not Yet Implemented)

- **Phase 4:** Additional reputation systems (trade quotas, escort missions)
- **Phase 5:** Final polish and documentation

### Development Environment

**Prerequisites:**

- Visual Studio (or MSBuild)
- .NET Framework 4.7.2 SDK
- RimWorld installation at: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\`

**DLL References (relative paths from mod directory):**

- `../../Prepatcher/Assemblies/0Harmony.dll` - Harmony library
- `../../../RimWorldWin64_Data/Managed/Assembly-CSharp.dll` - RimWorld core
- `../../../RimWorldWin64_Data/Managed/UnityEngine*.dll` - Unity engine

**Important:** All DLL references have `<Private>False</Private>` to prevent copying to output directory.

**Project File Format:**

This project uses **SDK-style .csproj format** (introduced with .NET Core, backported to Framework):

- **Auto-includes all `.cs` files** - No manual file listing required
- **Shorter, cleaner syntax** - Modern MSBuild features
- **NuGet restore required** - First build requires `/t:Restore` (automatic in subsequent builds)

```bash
# First time building after cloning
MSBuild.exe BetterTradersGuild.csproj /t:Restore

# Normal builds
MSBuild.exe BetterTradersGuild.csproj /p:Configuration=Debug
```

**Key benefit:** When you create a new `.cs` file, it's automatically included in compilation - no .csproj editing needed!

**WSL Setup (Linux/WSL2):**

If developing from WSL and using `monodis` for API introspection, there is a one-time step required to disable Mono's `cli` binary format handler to allow WSL to execute Windows executables like MSBuild and PowerShell:

```bash
sudo update-binfmts --disable cli
```

**What this does:** Mono's `cli` handler intercepts Windows PE executables (files with "MZ" magic bytes) and tries to run them through Mono's .NET runtime. This conflicts with WSL's native Windows executable interop. Disabling it:

- ✅ Allows MSBuild.exe and other Windows tools to run from WSL
- ✅ `monodis` and other Mono tools continue to work normally (call them explicitly with `mono`)
- ✅ Only affects automatic execution of .exe files; does not impact system functionality

**Verification:** After disabling, you should be able to run `powershell.exe -Command "Write-Host 'Test'"` successfully from WSL.

### Code Style and Conventions

- **File Organization:** One class per file, organized by concern (Core, Helpers, Patches)
- **Patch Files:** Named after the method they patch (e.g., `SettlementVisitable.cs` for `Settlement.Visitable`)
- **Namespaces:** Use `*Patches` suffix for patch namespaces to avoid RimWorld type conflicts
- **Comments:** Extensive XML documentation comments on all classes and methods, including "LEARNING NOTE" comments explaining RimWorld/Harmony patterns
- **Naming:** PascalCase for all public members, descriptive names reflecting RimWorld terminology
- **Null Safety:** Explicit null checks before accessing game objects (settlements, factions, maps)
- **Logging:** Use `Log.Message("[Better Traders Guild] ...")` for mod-specific logs

### Debugging Tips

1. **Enable RimWorld Dev Mode:** Settings → Dev Mode → Logging enables detailed logs
2. **Check logs at:**
   - **Windows:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   - **WSL:** `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log` (glob pattern - avoids needing to know Windows username)
   - **Linux (Steam):** `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
   - `Player-prev.log` in same directory contains previous session's log
3. **Inspect RimWorld API with monodis:**

   ```bash
   # From WSL - Disassemble a DLL to see available classes/methods
   monodis --assembly "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll"

   # Search for specific class definitions
   monodis "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" | grep "class.*Settlement"

   # View method signatures for a specific type
   monodis --output=output.txt "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll"
   ```

### Working with Harmony Patches

**File Organization:**

Harmony patches are in `Source/1.6/Patches/`, organized into subdirectories by the RimWorld type they target (e.g., `Settlement/`, `MapGeneration/`, `Caravan/`). Each file contains one patch class named after the method it patches (e.g., `SettlementVisitable.cs` patches `Settlement.Visitable`).

**Postfix Patch Pattern:**

```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.MethodName))]
public static class TargetClass_MethodName_Postfix
{
    [HarmonyPostfix]
    public static void Postfix(TargetClass __instance, ref ReturnType __result)
    {
        // __instance: the object the method was called on
        // __result: the return value (modifiable with ref)
        // Modify __result to change behavior
    }
}
```

**Prefix Patch Pattern (for skipping original):**

```csharp
[HarmonyPrefix]
public static bool Prefix(ref ReturnType __result)
{
    __result = newValue;
    return false; // Skip original method
}
```

### References and Resources

- **PLAN.md** - Detailed development roadmap with phase breakdown
- **About.xml** - Mod metadata, load order, and dependencies
- RimWorld modding wiki: https://rimworldwiki.com/wiki/Modding_Tutorials
- Harmony documentation: https://harmony.pardeike.net/
