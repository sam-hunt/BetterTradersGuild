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

**From WSL (Linux/WSL2):**

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
cd "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BetterTradersGuild\Source"
msbuild BetterTradersGuild.csproj /p:Configuration=Debug
```

**Output:** Compiled DLL is placed in `../Assemblies/BetterTradersGuild.dll`

**Build Output:** All compilation errors and warnings are visible directly in the terminal output with file paths, line numbers, and error codes (e.g., `CS0246`, `CS0219`).

### Testing

- No automated tests currently exist
- Testing is done manually in RimWorld with Dev Mode enabled
- The mod loads automatically from the Steam mods directory when RimWorld launches

### Project Structure

```
BetterTradersGuild/
├── About/
│   └── About.xml           # Mod metadata, dependencies, load order
├── Assemblies/             # Compiled DLL output directory
│   └── BetterTradersGuild.dll
├── Defs/                   # XML definitions for game content
│   ├── LayoutDefs/         # Phase 3: Custom settlement layout
│   │   └── BTG_OrbitalSettlement.xml
│   ├── LayoutRoomDefs/     # Phase 3: Custom room definitions (18 files)
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
│   └── PrefabDefs/         # Phase 3: Custom prefabs (10 files)
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
├── Patches/                # XML patches (empty - reserved for future use)
├── Source/                 # C# source code (organized by concern)
│   ├── Core/               # Core mod initialization and settings
│   │   ├── ModInitializer.cs      # Harmony patching and startup
│   │   └── ModSettings.cs         # Mod configuration UI and settings
│   ├── Helpers/            # Utility classes
│   │   ├── TradersGuildHelper.cs     # Faction/settlement checking
│   │   ├── TileHelper.cs             # World map tile utilities
│   │   └── TradersGuildTraderRotation.cs  # Trader rotation timing logic
│   ├── Patches/            # Harmony patches organized by target type
│   │   ├── Settlement/            # Settlement-related patches (9 files)
│   │   │   ├── SettlementVisitable.cs
│   │   │   ├── SettlementGetCaravanGizmos.cs
│   │   │   ├── SettlementGetFloatMenuOptions.cs
│   │   │   ├── SettlementGetShuttleFloatMenuOptions.cs
│   │   │   ├── SettlementGetInspectString.cs
│   │   │   ├── SettlementTraderTrackerGetTraderKind.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStock.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStockEveryDays.cs
│   │   │   └── SettlementTraderTrackerRegenerateStockAlignment.cs
│   │   ├── MapGeneration/         # Phase 3: Map generation patches (1 file)
│   │   │   └── GenStepOrbitalPlatformGenerate.cs
│   │   ├── WorldGrid/             # WorldGrid patches (2 files)
│   │   │   ├── WorldGridFindMostReasonableAdjacentTile.cs
│   │   │   └── WorldGridGetRoadMovementDifficulty.cs
│   │   ├── WorldObject/           # WorldObject patches (1 file)
│   │   │   └── WorldObjectRequiresSignalJammer.cs
│   │   ├── PlanetTile/            # PlanetTile patches (1 file)
│   │   │   └── PlanetTileLayerDef.cs
│   │   ├── CaravanArrivalActions/ # CaravanArrivalAction patches (2 files)
│   │   │   ├── CaravanArrivalActionAttackGetFloatMenuOptions.cs
│   │   │   └── CaravanArrivalActionTradeGetFloatMenuOptions.cs
│   │   ├── Caravan/               # Caravan patches (1 file)
│   │   │   └── CaravanGetGizmos.cs
│   │   └── Debug/                 # Debug logging patches (1 file)
│   │       └── RoomContentsWorkerFillRoom.cs
│   ├── RoomContents/       # Phase 3: Custom room generation workers
│   │   └── RoomContents_CaptainsQuarters.cs  # 🚧 IN PROGRESS
│   ├── WorldObjects/       # Phase 3: World object components
│   │   └── TradersGuildSettlementComponent.cs  # Cargo refresh tracking
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   ├── BetterTradersGuild.csproj  # SDK-style project file
│   └── BetterTradersGuild.sln     # Visual Studio solution
├── docs/                   # Technical documentation (9 files)
│   ├── CAPTAINS_QUARTERS_IMPLEMENTATION.md
│   ├── CARGO_IMPLEMENTATION_GUIDE.md
│   ├── EDGEONLY_LIMITATIONS.md
│   ├── LAYOUT_CONSTRAINTS_README.md
│   ├── PREFAB_EDGEONLY_GUIDE.md
│   ├── STORAGE_API_RESEARCH.md
│   ├── STORAGE_API_SUMMARY.txt
│   ├── STORAGE_DOCUMENTATION_INDEX.md
│   └── STYLING_QUICK_REF.md
├── CLAUDE.md               # Developer guidance (THIS FILE)
├── PLAN.md                 # Development roadmap and phase tracking
└── README.md               # GitHub repository landing page
```

**Note:** Files marked "🚧 IN PROGRESS" are functional but have incomplete features.

## Architecture and Key Concepts

### Code Organization

The codebase is organized into three main areas:

1. **Core/** - Mod initialization and settings

   - `ModInitializer.cs` - Applies Harmony patches on startup
   - `ModSettings.cs` - Configuration UI (planned for Phase 6)

2. **Helpers/** - Reusable utility classes

   - `TradersGuildHelper.cs` - Faction and settlement validation
   - `TileHelper.cs` - World map tile utilities

3. **Patches/** - Harmony patches organized by target type
   - Each subdirectory groups patches by RimWorld class (e.g., `Settlement/`, `Caravan/`)
   - Namespaces use `*Patches` suffix to avoid conflicts with RimWorld types (e.g., `BetterTradersGuild.Patches.SettlementPatches`)

**Important:** Namespace conflicts can occur when patch namespaces match RimWorld type names. Always use the `*Patches` suffix pattern (e.g., `SettlementPatches`, `CaravanPatches`) to avoid compilation errors.

### Mod Initialization

The mod uses `[StaticConstructorOnStartup]` attribute on the `BetterTradersGuildMod` class to automatically initialize when RimWorld loads. The static constructor applies all Harmony patches defined in the assembly and logs a single initialization message with the total patch count (e.g., `"[Better Traders Guild] Mod initialized with 17 Harmony patches applied."`).

**Settings Access:** The `BetterTradersGuildMod.Settings` static property provides global access to mod configuration throughout patches and helpers.

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

Helper classes are located in `Source/Helpers/` and provide reusable utility functions:

**TradersGuildHelper** (`Helpers/TradersGuildHelper.cs`) - Centralized faction/settlement checking:

- `IsTradersGuildSettlement(Settlement)` - Checks if settlement belongs to Traders Guild
- `CanPeacefullyVisit(Faction)` - Validates non-hostile relations

**TileHelper** (`Helpers/TileHelper.cs`) - World map tile utilities:

- `IsFriendlyTradersGuildTile(PlanetTile)` - Combined check for friendly Traders Guild at tile

**TradersGuildTraderRotation** (`Helpers/TradersGuildTraderRotation.cs`) - Trader rotation timing and scheduling:

- `GetRotationIntervalTicks()` - Returns player-configured rotation interval in ticks
- `GetVirtualLastStockTicks(settlementID)` - Calculates stable, settlement-specific virtual rotation schedule
- `GetNextRestockTick(settlementID)` - Calculates when settlement should next regenerate stock
- `ShouldRegenerateNow(settlement, currentLastStockTicks)` - Checks if stock regeneration is due

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
├── ONE shuttle bay (OrbitalTransportRoom)
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

**Mod Settings:**

- **Cargo Bay Inventory Percentage** (30-100%, default 60%) - How much of trade inventory appears as cargo

**Technical Notes:**

- Uses `Settlement_TraderTracker.stock` for inventory access
- Triggers lazy stock generation: `TryGenerateStock()`
- Cargo items tagged for despawn (ThingComp or region marking)
- Map persistence constraint documented in PLAN.md "Common Pitfalls"
- See PLAN.md Phase 3 section for detailed implementation phases

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
2. **Check logs at:** `%APPDATA%\..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
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

Harmony patches are organized in `Source/Patches/` by the RimWorld type they patch:

- `Settlement/` - Patches targeting `RimWorld.Planet.Settlement` methods
- `Caravan/` - Patches targeting `RimWorld.Planet.Caravan` methods
- `WorldGrid/` - Patches targeting `RimWorld.Planet.WorldGrid` methods
- `WorldObject/` - Patches targeting `RimWorld.Planet.WorldObject` methods
- `PlanetTile/` - Patches targeting `RimWorld.Planet.PlanetTile` methods
- `CaravanArrivalActions/` - Patches targeting `CaravanArrivalAction*` classes

Each file contains a single patch class named descriptively after the method it patches (e.g., `SettlementVisitable` patches `Settlement.Visitable`).

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

