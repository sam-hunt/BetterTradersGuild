# Changelog

All notable changes to Better Traders Guild will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-alpha.2] - 2025-11-09

### Overview

Patch release fixing two bugs discovered in alpha.1. No new features added.

### Fixed

- **Signal Jammer Logic** - Gift transport pods can now reach TradersGuild settlements even when hostile, preserving vanilla behavior for repairing relations through gifts
  - Previously: Signal jammer override only worked for non-hostile relations
  - Now: Transport pods bypass signal jammer regardless of hostility (too small for weapons platforms to target)
  - Note: Peaceful trading visits still require non-hostile relations (working as intended)
- **Nursery Floor Type** - Corrected invalid floor type definition in BTG_OrbitalNursery.xml
  - Changed: `CarpetBlueLight` → `CarpetBluePastel` (valid RimWorld floor type)
  - This prevents red error on mod load

---

## [0.1.0-alpha.1] - 2025-11-06

### Overview

First alpha release! Phase 2 (Peaceful Trading) is complete and fully functional. Phase 3 (Enhanced Settlement Generation) is in progress.

### Added - Phase 2: Peaceful Trading ✅ COMPLETE

#### Core Trading Features

- **Peaceful Settlement Visits** - Visit Traders Guild orbital bases via shuttle or caravan when you have good relations (Neutral or better)
- **Signal Jammer Override** - No longer need signal jammers to visit friendly Traders Guild bases
- **World Map Gizmos** - "Visit for Trade" buttons appear on friendly Traders Guild settlements
- **Space Travel Support** - Caravans can now path to space-layer settlements (previously blocked by game engine)

#### Dynamic Orbital Trader System

- **Trader Rotation** - Traders Guild settlements dynamically rotate between 4 orbital trader types:
  - Orbital Bulk Goods Trader (common goods, large quantities)
  - Orbital Combat Supplier (weapons, armor, combat gear)
  - Orbital Exotic Goods Trader (rare items, artifacts, luxury goods)
  - Orbital Pirate Merchant (questionable goods, stolen items)
- **Virtual Schedules** - Unvisited settlements show stable, accurate trader previews based on deterministic schedules
- **Settlement-Specific Rotation** - Each settlement has its own rotation offset, preventing all bases from syncing
- **Configurable Interval** - Set trader rotation frequency in Mod Options (5-30 days, default 15 days)
- **Docked Vessel Display** - World map inspection shows current trader type (e.g., "Docked vessel: Orbital Exotic Goods Trader")

#### Technical Features

- **Save-Game Safe** - Can be added or removed from existing saves without corruption
- **Mod Compatible** - Automatically supports mods that add custom orbital trader types
- **Weighted Selection** - Trader types selected based on `TraderKindDef.CalculatedCommonality`

### Added - Phase 3: Enhanced Settlement Generation 🚧 IN PROGRESS

#### Completed Components

- **Custom Layout System** - TradersGuild_OrbitalSettlement layout with 18 specialized room types
- **18 Room Definitions** - Command Center, Armory, Medical Bay, Barracks, Hydroponics, Workshop, Cargo Storage, Dining Room, Recreation Room, Security Station, Classroom, Computer Room, Storeroom, Trade Showcase, Transport Room, Nursery, Corridors, Commander's Quarters
- **10 Custom Prefabs** - Specialized furniture arrangements (hospital beds, medicine shelves, classroom bookshelves, billiards table, captain's bedroom, etc.)
- **Commander's Quarters (Partial)** - Custom room generation with programmatic bedroom placement, bookcase insertion, and unique weapon generation

#### Known Phase 3 Limitations

- Commander's Quarters bedroom placement needs edge detection improvements
- Door detection algorithm incomplete
- Some prefab placement edge cases unhandled
- Cell marking verification incomplete
- Billiards table clearance calculation needs refinement

### Technical Details

**Build Information:**

- RimWorld Version: 1.6 (Odyssey DLC required)
- .NET Framework: 4.7.2
- Harmony Version: 2.3.3+
- Mod Version: 0.1.0-alpha.1

**Harmony Patches Applied:** 17 total

- Settlement patches: 9
- World grid patches: 2
- World object patches: 1
- Planet tile patches: 1
- Caravan arrival action patches: 2
- Caravan patches: 1
- Map generation patches: 1

**Performance:**

- Minimal performance impact (patches only affect Traders Guild settlements)
- Trader rotation calculations cached to prevent frame drops
- No continuous background processing

### Installation

1. **Install Harmony** (required dependency)

   - Subscribe on Steam Workshop: [Harmony](https://steamcommunity.com/workshop/filedetails/?id=2009463077)
   - Or download from [GitHub Releases](https://github.com/pardeike/HarmonyRimWorld/releases/latest)

2. **Install Better Traders Guild**

   - Download `BetterTradersGuild-v0.1.0-alpha.1.zip` from [GitHub Releases](https://github.com/sam-hunt/BetterTradersGuild/releases)
   - Extract to your RimWorld Mods folder:
     - **Windows:** `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
     - **Mac:** `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
     - **Linux:** `~/.steam/steam/steamapps/common/RimWorld/Mods/`

3. **Enable in-game**
   - Launch RimWorld
   - Options → Mods → Check "Better Traders Guild"
   - Ensure load order: Core → Harmony → Odyssey DLC → Better Traders Guild
   - Restart RimWorld

### Configuration

Access mod settings via: **Options → Mod Settings → Better Traders Guild**

- **Trader Rotation Interval** (5-30 days, default: 15 days)
  - How often settlements regenerate stock and rotate trader types
  - Lower = more variety, higher = more time to plan visits

### Known Issues

1. **Phase 3 Incomplete** - Custom settlement generation has partial implementation (see limitations above)
2. **First-Time Visit Delay** - Settlement stock generation may take 1-2 seconds on first visit (vanilla behavior)
3. **Trader Preview Sync** - Very rare edge case where trader preview may temporarily show incorrect type after quick saves/loads (resolves on next tick)

### Testing Checklist

For alpha testers, please verify:

- [ ] Can visit Traders Guild bases with good relations (Neutral/Ally)
- [ ] Trade dialog opens correctly with orbital trader inventory
- [ ] Trader type rotates after configured interval (default 15 days)
- [ ] Docked vessel displays correctly on world map inspection
- [ ] Mod settings slider changes trader rotation interval
- [ ] No errors in Player.log related to Better Traders Guild
- [ ] Can add/remove mod from existing saves without corruption

### Feedback

Please report bugs and feedback via:

- GitHub Issues: https://github.com/sam-hunt/BetterTradersGuild/issues
- Include your Player.log file (located at `%APPDATA%\..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`)
- Describe steps to reproduce
- Mention any conflicting mods

### Credits

- **Author:** Sam Hunt
- **Powered by:** Harmony 2.3.3+ by Andreas Pardeike
- **RimWorld:** Ludeon Studios

---

[0.1.0-alpha.2]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.1.0-alpha.2
[0.1.0-alpha.1]: https://github.com/sam-hunt/BetterTradersGuild/releases/tag/v0.1.0-alpha.1

