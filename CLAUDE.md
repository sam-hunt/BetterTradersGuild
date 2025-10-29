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
- Vanilla Expanded Framework
- Vanilla Base Generation Expanded

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
├── Defs/                   # XML definitions (future: custom game content)
├── Patches/                # XML patches (future: runtime XML modifications)
├── Source/                 # C# source code (organized by concern)
│   ├── Core/               # Core mod initialization and settings
│   │   ├── ModInitializer.cs      # Harmony patching and startup
│   │   └── ModSettings.cs         # Mod configuration UI and settings
│   ├── Helpers/            # Utility classes
│   │   ├── TradersGuildHelper.cs     # Faction/settlement checking
│   │   ├── TileHelper.cs             # World map tile utilities
│   │   └── TradersGuildTraderRotation.cs  # Trader rotation timing logic
│   ├── Patches/            # Harmony patches organized by target type
│   │   ├── Settlement/            # Settlement-related patches (10 files)
│   │   │   ├── SettlementVisitable.cs
│   │   │   ├── SettlementGetCaravanGizmos.cs
│   │   │   ├── SettlementGetFloatMenuOptions.cs
│   │   │   ├── SettlementGetShuttleFloatMenuOptions.cs
│   │   │   ├── SettlementGetInspectString.cs
│   │   │   ├── SettlementTraderTrackerGetTraderKind.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStock.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStockEveryDays.cs
│   │   │   ├── SettlementTraderTrackerRegenerateStockAlignment.cs
│   │   ├── WorldGrid/             # WorldGrid patches (2 files)
│   │   ├── WorldObject/           # WorldObject patches (1 file)
│   │   ├── PlanetTile/            # PlanetTile patches (1 file)
│   │   ├── CaravanArrivalActions/ # CaravanArrivalAction patches (2 files)
│   │   └── Caravan/               # Caravan patches (1 file)
│   ├── BetterTradersGuild.csproj  # Visual Studio project file
│   └── Properties/
├── PHASE2_TRADING_PLAN.md  # Phase 2 detailed architecture and issue history
└── PLAN.md                 # Development roadmap and phase tracking
```

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
- Settlement ID offset (using prime multiplier) desynchronizes rotation across settlements
- Unvisited settlements show stable previews that match what they'll get when visited
- Rotation interval is player-configurable (5-30 days, default 15)

**Key Components:**
1. **TradersGuildTraderRotation helper** - Centralized timing logic
2. **Virtual schedule alignment** - Ensures preview matches first visit
3. **RegenerateStockEveryDays patch** - Custom intervals for TradersGuild
4. **Pending alignment tracking** - Distinguishes first-time from subsequent regenerations

**How It Works:**
- Unvisited: Shows trader based on `GetVirtualLastStockTicks(settlementID)`
- First visit: Aligns `lastStockGenerationTicks` to virtual value, restores after vanilla overwrite
- Subsequent: Uses normal regeneration with custom interval

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

### Future Phases (Not Yet Implemented)

- **Phase 3:** Enhanced map generation with Vanilla Base Generation Expanded
- **Phase 4:** Additional reputation systems (trade quotas, escort missions)
- **Phase 5:** ~~Mod settings UI~~ (completed early in Phase 2), final polish

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

4. **Common Issues:**
   - InvalidCastException in WorldGrid methods → Check tile type handling patches
   - Gizmo not appearing → Verify `Settlement.Visitable` returns true
   - Signal jammer blocking → Check `RequiresSignalJammerToReach` patch

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
