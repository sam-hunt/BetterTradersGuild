# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the mod (outputs to 1.6/Assemblies/)
dotnet build BetterTradersGuild.sln -c Release

# Build only the main project
dotnet build Source/1.6/BetterTradersGuild.csproj

# Run tests
dotnet test Tests/1.6/BetterTradersGuild.Tests.csproj
./run-tests.sh  # WSL as test runner dotnet test runner hangs

# Clean build artifacts
dotnet clean BetterTradersGuild.sln
```

The build system auto-detects the RimWorld installation path on Windows/Linux/Mac. For CI builds without RimWorld installed, it falls back to the `Krafs.Rimworld.Ref` NuGet package.

## Project Overview

A RimWorld mod that enhances the Odyssey DLC's Traders Guild faction. Requires RimWorld 1.6 with Odyssey DLC.

**Key Features:**

- Peaceful shuttle caravan trading visits to orbital settlements when relations are good
- Dynamic orbital trader rotation system with deterministic schedules per settlement
- Custom settlement generation with specialized room types and focus on sense of inhabitation
- Hackable cargo vault system linking trade inventory to physical cargo

## Architecture

### Entry Point

`Source/1.6/Core/ModInitializer.cs` - Static constructor with `[StaticConstructorOnStartup]` auto-patches via Harmony attribute discovery.

### Directory Structure

```
Source/1.6/
├── Core/           # ModInitializer, ModSettings
├── Patches/        # Harmony patches organized by target class
│   ├── Settlement/     # Trader rotation, peaceful access
│   ├── Caravan/        # Caravan mechanics
│   └── MapGeneration/  # Custom settlement hooks
├── DefRefs/        # [DefOf] static constant classes
├── Helpers/        # Utility functions, TradersGuildHelper
├── GenSteps/       # Map generation steps
├── LayoutWorkers/  # Layout-specific generation
├── RoomContents/   # Room-specific spawners
└── Comps/, Jobs/, LordJobs/, MapComponents/

1.6/Defs/           # XML definitions
├── LayoutRoomDefs/ # custom room definitions
├── PrefabDefs/     # Furniture arrangement templates
└── GenStepDefs/, ThingDefs/, etc.
```

### Key Patterns

**Harmony Patching:** All patches use `[HarmonyPatch]` attributes for automatic discovery. Patches are organized by target class in subdirectories under `Patches/`. Most are Postfix patches that check `TradersGuildHelper.IsTradersGuildSettlement()` before modifying behavior.

**DefOf Constants:** Static `[DefOf]` classes in `DefRefs/` provide compile-time safety for XML definitions (e.g., `Factions.TradersGuild`, `LayoutRooms.CommandersQuarters`).

**Virtual Trader Schedule:** `TradersGuildTraderRotation` calculates trader types deterministically using settlement ID-based offsets, enabling preview of unvisited settlement traders.

**Room Contents Workers:** Each room type has a `RoomContents_[RoomName].cs` file that handles specialized furniture and pawn spawning using `Prefab` definitions.

### Testing

XUnit tests in `Tests/1.6/` validate spatial algorithms (placement calculators, subroom packing). Tests use ASCII diagram visualization for room layouts.

## Commit Convention

Uses Conventional Commits. Run `/commit-msg` to draft messages for staged changes.

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`

Format:

```
type: Imperative description (no period)

Optional body explaining "why" not "what".
```
