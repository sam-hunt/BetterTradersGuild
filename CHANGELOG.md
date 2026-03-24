# Changelog

All notable changes to Better Traders Guild will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.5] - 2026-03-24

### Fixed

- Prevent Empire relation fix dialog from incorrectly appearing in new games where the player becomes hostile with the Empire through normal gameplay

## [1.0.4] - 2026-03-24

### Fixed

- Fix Empire being permanently hostile when using BTG scenarios with Royalty DLC active. Existing saves show a one-time dialog to reset relations
- Fix wallAttachment buildings not being cleared from corridor perimeter zones during settlement generation

## [1.0.3] - 2026-03-19

### Added

- Dynamic item pool discovery for armory, weapons, and books

### Fixed

- Defend against HAR outfit stand crashes during settlement generation
- Downgrade room placement failure from error to warning during BTG generation
- Clone orbit LayerDef instead of constructing from scratch
- Exclude life support units and wall lamps from corridor decoration

## [1.0.2] - 2026-03-18

### Added

- Randomly unlock ~50% of crew quarters subroom doors

### Changed

- Use CollarShirt instead of BasicShirt for crew quarters

### Fixed

- Filter out orbital traders whose faction is absent from the world
- Add Inherit=False to Citizen apparelRequired patch

## [1.0.1] - 2026-03-17

### Added

- Crew quarters shambler apparel tinted with faction color

### Fixed

- Checkpoint barricades blocking corridor airlock reserved zone
- ToolCabinets prefab disabled; re-enabled with conditional VFE Production patch
- Null player faction during world generation when using Layered Atmosphere and Orbit mod

## [1.0.0] - 2025-03-14

### Added

- Trade request quests from Traders Guild orbital settlements
- Independent Traders and Exiled Traders custom scenarios
- Scenario editor UI for all custom ScenParts
- VE Framework faction color tinting for spawned apparel
- Mod icon for loading screen
- Guaranteed SilverInlay trait on crew quarters unique weapons

### Changed

- Swapped airlock blast door and vacuum barrier placement to improve combat fairness
- Extracted translatable strings to language files for localization
- Moved armory outfit stand painting to PrefabDef colorDef system
- Switched trade request quest to use vanilla QuestPart

### Fixed

- Validate negotiator exists before opening trade dialog at TG settlements
- Trade request quest selection weight too low to appear reliably
- Missing MayRequire attributes for Biotech and Royalty DLC content
- Missing SilverInlay weapon trait definition and erroneous Biotech requirement on GoldInlay
- Spawned animal beds using Steel instead of Cloth

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

[1.0.5]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v1.0.5
[1.0.4]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v1.0.4
[1.0.3]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v1.0.3
[1.0.2]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v1.0.2
[1.0.1]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v1.0.1
[1.0.0]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v1.0.0
[0.2.0-beta.1]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.2.0-beta.1
[0.1.0-alpha.2]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.1.0-alpha.2
[0.1.0-alpha.1]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.1.0-alpha.1
