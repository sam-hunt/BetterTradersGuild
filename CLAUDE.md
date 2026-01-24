# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Better Traders Guild** is a RimWorld 1.6 mod that expands player interactions with the Traders Guild faction. Requires Odyssey DLC and Harmony.

**Key Features:**

- Peaceful shuttle caravan trading visits to orbital settlements when relations are good
- Dynamic orbital trader rotation system with deterministic schedules per settlement
- Custom settlement generation with specialized room types and focus on sense of inhabitation
- Hackable cargo vault system linking trade inventory to physical cargo

**Key Technologies:** C# (.NET Framework 4.7.2), Harmony library, RimWorld modding API, XML definitions

## Build Commands

```bash
# Build the mod (outputs to 1.6/Assemblies/)
dotnet build BetterTradersGuild.sln -c Release

# Build only the main project
dotnet build Source/1.6/BetterTradersGuild.csproj

# Run tests
dotnet test Tests/1.6/BetterTradersGuild.Tests.csproj
./Scripts/run-tests.sh  # Prefer this on WSL as dotnet test runner hangs

# Clean build artifacts
dotnet clean BetterTradersGuild.sln
```

The build system auto-detects the RimWorld installation path on Windows/Linux/Mac. For CI builds without RimWorld installed, it falls back to the `Krafs.Rimworld.Ref` NuGet package.

## Architecture

### Entry Point

`Source/1.6/Core/ModInitializer.cs` - Static constructor with `[StaticConstructorOnStartup]` auto-patches via Harmony attribute discovery. Logs initialization message with patch count.

**Settings Access:** `BetterTradersGuildMod.Settings` provides global access to mod configuration.

### Directory Structure

```
Source/1.6/
├── Core/           # ModInitializer, ModSettings
├── Patches/        # Harmony patches organized by target class
│   ├── Settlement/     # Trader rotation, peaceful access
│   ├── Caravan/        # Caravan mechanics
│   ├── MapGeneration/  # Custom settlement hooks
│   └── Hackable/       # Cargo vault hacking system
├── DefRefs/        # [DefOf] static constant classes
├── Helpers/        # Utility functions, TradersGuildHelper
│   ├── MapGeneration/  # Hidden pipe detection (VE Framework integration)
│   └── RoomContents/   # Placement calculators, shelf/plant helpers
├── GenSteps/       # Map generation steps
├── LayoutWorkers/  # Layout-specific generation, conduit/pipe placement
├── RoomContents/   # Room-specific spawners (RoomContents_<RoomName>.cs)
├── Comps/, Jobs/, LordJobs/, MapComponents/

1.6/Defs/           # XML definitions
├── LayoutRoomDefs/ # Custom room definitions
├── PrefabDefs/     # Furniture arrangement templates (subdirs by room type)
├── GenStepDefs/, MapGeneratorDefs/, StructureLayoutDefs/, ThingDefs/
└── Patches/        # XML patches (named by target, e.g., PawnKinds_*.xml)
```

### Key Patterns

**Harmony Patching:** All patches use `[HarmonyPatch]` attributes for automatic discovery. Patches are organized by target class in subdirectories under `Patches/`. Most are Postfix patches that check `TradersGuildHelper.IsTradersGuildSettlement()` before modifying behavior.

**Namespace Convention:** Use `*Patches` suffix for patch namespaces to avoid RimWorld type conflicts (e.g., `SettlementPatches`, `CaravanPatches`).

**DefOf Constants:** Static `[DefOf]` classes in `DefRefs/` provide compile-time safety for XML definitions (e.g., `Factions.TradersGuild`, `LayoutRooms.CommandersQuarters`).

**Room Contents Workers:** Each room type has a `RoomContents_[RoomName].cs` file that handles specialized furniture and pawn spawning using `Prefab` definitions.

### Map Generation Architecture

BTG uses a declarative, XML-driven approach for custom map generation:

- **MapGeneratorDef**: Defines the complete generation pipeline (list of GenSteps to run)
- **GenStepDef**: Individual step with order value (lower = runs first) and configurable parameters
- **SpaceMapGenerator**: Vanilla parent providing space map properties (`defaultUnderGridTerrain: Space`, `renderWorld: true`)
- **StructureLayoutDef**: Blueprint defining room arrangement, connections, and overall settlement shape
- **LayoutRoomDef**: Room type with dimensions, required furniture, and `RoomContentsWorker` class for spawning
- **PrefabDef**: Reusable furniture arrangement template placed by RoomContentsWorkers (e.g., desk+chair combos)

**BTG MapGeneratorDefs:**

| Def                          | Purpose                          | Parent              |
| ---------------------------- | -------------------------------- | ------------------- |
| `BTG_SettlementMapGenerator` | TradersGuild orbital settlements | `SpaceMapGenerator` |
| `BTG_CargoVaultMapGenerator` | Cargo vault pocket maps          | `SpaceMapGenerator` |

**BTG GenSteps (Settlement Pipeline, roughly in order):**

| GenStep                     | Purpose                                                      |
| --------------------------- | ------------------------------------------------------------ |
| `BTG_SettlementPlatform`    | Core structure via `GenStep_OrbitalPlatform` with BTG layout |
| `BTG_SpawnEntranceDefences` | Spawn autocannons flanking perimeter entrances               |
| `BTG_ReplaceTerrain`        | Replace AncientTile → MetalTile                              |
| `BTG_PaintTerrain`          | Paint terrain with BTG_OrbitalSteel color                    |
| `BTG_ExtendLandingPadPipes` | Extend VE pipes to landing pads (graceful no-op if no VE)    |
| `BTG_SetWallLampColor`      | Set WallLamp glow to white/blue                              |
| `BTG_SettlementPawnsLoot`   | Pawn spawning (loot disabled via `lootMarketValue: 0~0`)     |
| `BTG_SpawnSentryDrones`     | Spawn additional sentry drones (uses ModSettings)            |

**Swapping MapGeneratorDef:** Patch `Settlement.MapGeneratorDef` property getter (not `MapParent` - `Settlement` overrides it).

### Trader Rotation System

The mod implements a virtual schedule system for trader rotation across TradersGuild settlements.

**Virtual Schedules:**

- Each settlement has a deterministic rotation schedule based on its ID
- Settlement ID offset (prime multiplier: 123457) desynchronizes rotation across settlements
- Unvisited settlements show stable previews that match what they'll get when visited
- Rotation interval is player-configurable (5-60 days, default 30)

**Three-Patch Architecture:**

The trader rotation system requires three Harmony patches working together:

1. **SettlementTraderTrackerGetTraderKind.cs** (Postfix on `TraderKind` getter)
   - Provides weighted random orbital trader selection
   - **Priority 1**: Checks `TradersGuildWorldComponent` cache (populated after stock generation)
   - **Priority 2**: Falls back to deterministic calculation using `Hash(settlementID, lastStockGenerationTicks)`
   - Checks flags from other patches to determine which tick value to use for calculation

2. **SettlementTraderTrackerRegenerateStock.cs** (Prefix/Postfix on `RegenerateStock()`)
   - **ESSENTIAL** - Sets thread-local flag during stock regeneration
   - Postfix caches selected trader to `TradersGuildWorldComponent` for subsequent access
   - **CRITICAL ORDERING**: Must cache trader BEFORE clearing flag (see below)
   - Exposes `IsRegeneratingStock(settlementID)` for other patches

3. **SettlementTraderTrackerRegenerateStockAlignment.cs** (Prefix/Postfix on `RegenerateStock()`)
   - Aligns first-visit stock generation with virtual preview schedule
   - Solves preview/visit mismatch problem (see below)
   - Exposes `HasPendingAlignment(settlementID)` for other patches

**Critical Problem #1: Stock/Dialog Desync**

Vanilla `RegenerateStock()` updates `lastStockGenerationTicks` at the END:

```
1. Stock cleared
2. TraderKind getter called (uses OLD lastStockTicks) → Selects Trader A
3. Stock generated for Trader A
4. lastStockGenerationTicks = TicksGame (NEW value)
5. Dialog opens → TraderKind getter (uses NEW lastStockTicks) → Selects Trader B
```

Result: Dialog shows Trader B title but has Trader A's inventory!

**Solution:** Two-part fix:
1. RegenerateStock Prefix sets `IsRegeneratingStock` flag. TraderKind getter detects flag and uses `Find.TickManager.TicksGame` (or aligned virtual ticks for first-time) so stock generation uses correct trader.
2. RegenerateStock Postfix caches the selected trader to `TradersGuildWorldComponent`. Subsequent TraderKind accesses check this cache first, bypassing recalculation entirely.

**Critical Ordering in Postfix:** The Postfix must call `TraderKind` to cache the result BEFORE clearing the `IsRegeneratingStock` flag. If the flag is cleared first, the getter won't check `HasPendingAlignment()` and will use the wrong tick value (vanilla's `TicksGame` instead of aligned ticks), caching the wrong trader.

**Critical Problem #2: Preview/Visit Mismatch**

Unvisited settlements use virtual schedules for preview, but first-visit generation uses `TicksGame`:

```
1. Preview calculates: GetVirtualLastStockTicks(ID) = -865481 → Shows Exotic Trader
2. Player visits → RegenerateStock() sets lastStockTicks = TicksGame = 12015
3. Different seeds → Shows Bulk Trader (broken trust!)
```

**Solution:** Alignment patch detects first-time generation (lastStockTicks == -1), pre-sets to virtual schedule, and restores after vanilla overwrites.

**Implementation Pattern:**

```csharp
// Check if custom layouts enabled
if (!BetterTradersGuildMod.Settings.useCustomLayouts)
    return true; // Use vanilla/other mod generation

// Check if cargo system enabled
if (BetterTradersGuildMod.Settings.cargoInventoryPercentage > 0f)
{
    // Add TradersGuildSettlementComponent
}
```

### Cargo Vault Stock Management

The cargo vault displays physical items from the settlement's trade inventory. Stock must be carefully managed across the settlement visit lifecycle.

**Stock Lifecycle:**

```
1. Player enters settlement → Map.FinalizeInit → Stock generated if null (SettlementMapGenerated patch)
2. While visiting → Stock frozen (RegenerateStock/TryDestroyStock patches block changes)
3. Player opens cargo vault → Items spawned from stock (CargoSelector removes from stock)
4. Vault locked (pawn relock action) → Remaining items returned to stock (CargoReturnHelper)
5. Vault hatch despawns (map unload) → Remaining items returned to stock (CargoReturnHelper)
6. Player defeats settlement → Stock transferred to MapComponent cache (CheckDefeated patch)
7. Post-defeat vault access → Uses cached stock (CargoVaultHelper fallback)
8. Post-defeat vault locked/despawns → Remaining items returned to cache (CargoReturnHelper)
```

**Stock Return:** Items left in the vault (not taken by player) are returned via `CargoReturnHelper.ReturnItemsToStock()` when the vault is locked or despawns. Pre-defeat, items return to trader stock; post-defeat, items return to `SettlementStockCache`. This is handled transparently by `CargoVaultHelper.GetStock()` checking `settlement.Destroyed`.

**Key Invariant:** Once `settlement.Map` is non-null, stock is guaranteed to exist and remains frozen until map unload or defeat.

**Four Coordinating Patches:**

| Patch                                    | Hook                             | Purpose                                              |
| ---------------------------------------- | -------------------------------- | ---------------------------------------------------- |
| `SettlementMapGenerated`                 | `Map.FinalizeInit` Postfix       | Ensures stock exists when map loads                  |
| `SettlementTraderTrackerRegenerateStock` | `RegenerateStock()` Prefix       | Blocks regeneration while map loaded                 |
| `SettlementTraderTrackerTryDestroyStock` | `TryDestroyStock()` Prefix       | Blocks destruction while map loaded OR during defeat |
| `SettlementDefeatUtilityCheckDefeated`   | `CheckDefeated()` Prefix+Postfix | Transfers stock to cache on confirmed defeat         |

**Critical Timing Issue - Defeat Processing:**

During `CheckDefeated()`, vanilla does:

1. Reparents map → `settlement.Map` becomes null
2. Calls `settlement.Destroy()` → triggers `TryDestroyStock()`

Problem: `TryDestroyStock` patch checked `settlement.Map != null`, which is now false, so stock gets destroyed before our Postfix can cache it.

**Solution:** Prefix/Postfix coordination via `settlementsBeingDefeated` HashSet:

- Prefix adds settlement ID to set
- `TryDestroyStock` blocks if ID in set (even when `Map` is null)
- Postfix transfers stock to `SettlementStockCache` MapComponent, removes from set

**Stock Access Helper (`CargoVaultHelper.GetStock`):**

```csharp
// Navigates: pocketMap → PocketMapParent → sourceMap → Settlement
if (settlement?.trader != null && !settlement.Destroyed)
    return traderStock;  // Normal path
else
    return cache.preservedStock;  // Fallback for defeated settlements
```

**Files:** `Patches/Settlement/Settlement*.cs`, `RoomContents/CargoHoldVault/CargoVaultHelper.cs`, `MapComponents/SettlementStockCache.cs`

### Salvagers Raid Weight System

Two tightly-coupled patches in `Patches/Incidents/` boost Salvagers raid probability on TG maps:

1. **PawnGroupMakerUtilityTryGetRandomFactionForCombatPawnGroupWeighted.cs** - Sets `RaidFactionSelectionContext.IsOnTradersGuildMap` flag
2. **FactionDefRaidCommonalityFromPoints.cs** - Reads flag, multiplies Salvagers weight by `ModSettings.salvagersRaidWeightMultiplier`

The context flag pattern is required because `RaidCommonalityFromPoints` has no map parameter.

### Testing

XUnit tests in `Tests/1.6/` validate spatial algorithms (placement calculators, subroom packing). Tests use ASCII diagram visualization for room layouts.

**WSL Note:** `dotnet test` has timeout issues on WSL with .NET Framework 4.7.2. Use `./Scripts/run-tests.sh` instead.

**Excluded Test Files:** `Tests/Tools/RegenerateDiagrams.cs` (utility), `Tests/Helpers/DiagramGeneratorTests.cs` - excluded via `<Compile Remove="..." />`.

## Debugging

1. **Enable RimWorld Dev Mode:** Settings → Dev Mode → Logging
2. **Log locations:**
   - **Windows:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
   - **WSL:** `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
3. **Logging:** Use `Log.Message("[Better Traders Guild] ...")` for mod-specific logs
4. **Inspect RimWorld API:** `monodis "/mnt/c/.../RimWorldWin64_Data/Managed/Assembly-CSharp.dll"`

## Harmony Patch Examples

**Postfix Pattern:**

```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.MethodName))]
public static class TargetClass_MethodName_Postfix
{
    [HarmonyPostfix]
    public static void Postfix(TargetClass __instance, ref ReturnType __result)
    {
        // __instance: object method was called on
        // __result: return value (modifiable with ref)
    }
}
```

**Prefix Pattern (for skipping original):**

```csharp
[HarmonyPrefix]
public static bool Prefix(ref ReturnType __result)
{
    __result = newValue;
    return false; // Skip original method
}
```
