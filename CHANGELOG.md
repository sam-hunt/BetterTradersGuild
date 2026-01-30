# Changelog

All notable changes to Better Traders Guild will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0-beta.1] - 2025-01-31

### Added

- Localization support with full English language files
- Custom map generation system with 18 specialized room types and 66 prefabs
- Cargo vault system linking trade inventory to physical hackable cargo
- Autocannon defenses at settlement perimeter exits
- Mech spawning with Lord system for settlement defense
- Transport room, expanded crew quarters with enclosed bedroom subrooms
- Mess hall expansion with larger layout and double dining tables
- Prefab density scaling and freestanding furniture placement
- Cargo vault relock system with auto-cleanup for hatches
- Trade inventory preservation after settlement defeat
- Workshop room enhancements with materials, safety equipment, and atmosphere
- Configurable Life Support Unit power output
- Room filth scatter for lived-in atmosphere
- Sentry drone spawning with configurable count

### Changed

- Overhauled trader rotation system with deterministic virtual schedules
- Expanded trader pool from 4 base types to all orbital traders, including mod-added
- Overhauled mod settings UI with expanded configuration options
- Extended trader rotation interval range to 5-60 days (was 5-30)
- Reorganized Defs directory structure for maintainability
- Centralized DefRefs system with DefOf classes
- Renamed cargo hold to cargo vault throughout
- Refined stock management to prevent mid-visit regeneration

### Fixed

- Space tile caravan crashes when pathing to orbital settlements
- DLC gating for mod integration content (Anomaly, VE Framework)
- Cargo vault edge cases during settlement defeat
- Wall overwriting door in CommandersBedroom prefab
- Landing pad pipes not connecting to actual structure walls
- Middle strip wall and exclusion zone calculations in crew quarters subroom packing
- Room generation parameter balance issues
- PlantPot RoomPartDef missing stuffDef causing MakeThing errors

## [0.1.0-alpha.2] - 2025-11-09

### Fixed

- Gift transport pods can now reach Traders Guild settlements even when hostile, preserving vanilla behavior for repairing relations
- Corrected invalid floor type in BTG_OrbitalNursery.xml (`CarpetBlueLight` → `CarpetBluePastel`)

## [0.1.0-alpha.1] - 2025-11-06

### Added

- Peaceful settlement visits to Traders Guild orbital bases (requires Neutral or better relations)
- Signal jammer bypass for friendly Traders Guild settlements
- "Visit for Trade" world map gizmos on friendly settlements
- Caravan pathing to space-layer settlements
- Dynamic trader rotation system with 4 orbital trader types (Bulk Goods, Combat Supplier, Exotic Goods, Pirate Merchant)
- Settlement-specific rotation schedules with configurable interval (5-30 days)
- Docked vessel display on world map inspection
- Custom settlement layout system with 18 room types and 10 prefabs

### Known Issues

- Settlement generation is WIP

[0.2.0-beta.1]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.2.0-beta.1
[0.1.0-alpha.2]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.1.0-alpha.2
[0.1.0-alpha.1]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.1.0-alpha.1
