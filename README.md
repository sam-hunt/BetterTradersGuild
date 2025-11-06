# Better Traders Guild

> A RimWorld mod enhancing the Odyssey Traders Guild faction

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-blue.svg)](https://rimworldgame.com/)
[![Odyssey DLC](https://img.shields.io/badge/DLC-Odyssey%20Required-orange.svg)](https://store.steampowered.com/app/2380740/RimWorld__Odyssey/)
[![Version](https://img.shields.io/badge/Version-0.1.0--alpha.1-yellow.svg)](https://github.com/sam-hunt/BetterTradersGuild/releases)
[![Development Status](https://img.shields.io/badge/Status-Phase%202%20Complete-green.svg)](https://github.com/sam-hunt/BetterTradersGuild/blob/main/PLAN.md)

## 🚨 Alpha Release

This is an **alpha release** (v0.1.0-alpha.1) for early testing. Phase 2 (Peaceful Trading) is complete and fully functional. Phase 3 (Enhanced Settlement Generation) is partially implemented. See [Known Limitations](#alpha-limitations) below.

Transform the Traders Guild from hostile orbital raiders into valuable trading partners. Visit their orbital settlements peacefully for dynamic trading opportunities, or maintain the vanilla hostile relationship if you prefer the challenge. Your choice.

## About

In vanilla RimWorld, the Traders Guild faction spawns orbital settlements that are permanently hostile regardless of goodwill, and require signal jammers to reach. This mod unlocks peaceful interactions when you maintain good relations, allowing you to:

- **Trade directly** at their orbital bases via shuttle (or transport pod...)
- **Meet rotating orbital traders** across different settlements (4 vanilla, supports modded trader types by default)
- **Plan expeditions** using the virtual schedule system to preview which traders are docked when allied
- **Configure difficulty** by adjusting trader rotation intervals to match your playstyle with mod options

Vanilla gameplay is preserved for hostile relationships - you can still raid settlements via gravship (with a signal jammer), or by transport pod. This is purely an expansion of diplomatic options, not a gameplay overhaul.

## Features

### ✅ Phase 2: Peaceful Trading Visits (Complete)

- **Peaceful Access**: Visit Traders Guild orbital bases via shuttle or caravan when relations are Neutral or better
- **Dynamic Trader Rotation**: Each settlement rotates between 4 orbital trader types (Bulk Goods, Combat Supplier, Exotic, Pirate Merchant)
- **Virtual Schedule System**: Preview which trader is currently docked at unvisited settlements before you travel
- **Configurable Rotation**: Adjust trader rotation interval from 5-30 days (default: 15 days) in Mod Settings
- **Docked Vessel Display**: World map inspection cards show the currently docked trader type
- **Desynchronized Schedules**: Each settlement rotates independently based on its unique ID
- **Mod Compatibility**: Automatically supports any mods that add custom orbital trader types

### 🚧 Phase 3: Enhanced Settlement Generation (In Progress)

- Custom room layouts with modern aesthetics (Trade Shuttle Bay, Captain's Quarters, Command Center, Medical Bay, Barracks, etc.)
- Dynamic cargo system - items spawn from the settlement's actual trade inventory
- Realistic consequences: steal cargo and it's missing from trade stock, or sell items and find them in the cargo bay later
- Configurable cargo percentage (30-100%, default 60%)

### 🔮 Coming later

- Other DLC and popular community mod integrations
- Possible new quest site(s) and/or goodwill opportunities
- Quality-of-life improvements based on community feedback

See [PLAN.md](PLAN.md) for detailed development roadmap and technical architecture.

## Requirements

- **RimWorld 1.6** or later
- **Odyssey DLC** (required - this mod depends on Odyssey's orbital settlement system)
- **Harmony** (auto-downloaded from Steam Workshop if you don't have it)

## Installation

### Steam Workshop (Recommended)

_Coming soon to Steam Workshop_ - Subscribe and it will auto-download

### Manual Installation

1. Download the latest release from the [Releases](https://github.com/sam-hunt/BetterTradersGuild/releases) page
2. Extract the `BetterTradersGuild` folder to your RimWorld `Mods` directory:
   - **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - **Mac**: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
   - **Linux**: `~/.steam/steam/steamapps/common/RimWorld/Mods/`
3. Enable the mod in RimWorld's mod menu
4. Restart RimWorld

### Load Order

Place **Better Traders Guild** after the following mods:

1. Harmony
2. Core
3. All DLCs (especially Odyssey)

The mod should load last among Traders Guild-related mods to ensure compatibility.

## Configuration

Access mod settings via: **Options → Mod Settings → Better Traders Guild**

### Available Settings

- **Trader Rotation Interval** (5-30 days, default: 15)

  - How often settlements rotate their docked trader
  - Shorter intervals = more variety, less strategic planning
  - Longer intervals = more strategic scouting, fewer "check all settlements" trips

- **Cargo Bay Inventory Percentage** _(Phase 3 - Coming Soon)_
  - How much of trade inventory appears as lootable cargo (30-100%, default: 60%)

## Compatibility

### Save-Game Safety

✅ **Safe to add** to existing saves - traders will begin rotating immediately
✅ **Safe to remove** from saves - no permanent changes to world state

### Load Order Compatibility

- ✅ Compatible with mods that add custom orbital trader types (automatically detected)
- ⚠️ May conflict with other mods that heavily modify Traders Guild faction behavior (untested)

### Known Issues

- Space tile pathfinding may show minor performance overhead on large world maps (negligible in testing)

Report compatibility issues on the [Issues](https://github.com/sam-hunt/BetterTradersGuild/issues) page.

### Alpha Limitations

**Phase 3 (Enhanced Settlement Generation) is partially complete** in this alpha release. Current limitations:

- **Captain's Quarters Room** - Basic implementation working, but some edge cases unhandled:
  - Door detection algorithm incomplete (bedroom may occasionally block doorways)
  - Edge placement fallbacks need refinement
  - Cell marking verification incomplete
  - Billiards table clearance calculation needs improvement

- **Other Custom Rooms** - Fully defined but using vanilla generation until Captain's Quarters patterns are finalized

- **Dynamic Cargo System** - Designed but not yet implemented (planned for Phase 3 completion)

**What works in this alpha:**
- ✅ All Phase 2 peaceful trading features (fully complete and stable)
- ✅ Custom settlement layout system (18 room types defined)
- ✅ 10 custom prefabs for furniture arrangements
- ✅ Captain's Quarters basic generation (bedroom, bookcase, unique weapons)

Phase 3 will be completed in a future alpha release. Current implementation is functional but may have minor aesthetic issues in Captain's Quarters.

## FAQ

**Q: Can I still raid Traders Guild settlements?**
A: Yes! If relations are Hostile, settlements behave like vanilla (require signal jammer, but more heavily defended). This mod only adds peaceful options when relations are good.

**Q: Does this make the game easier?**
A: It adds convenience and player choice, but settlements still require good relations (built through gifts/quests) and travel resources (shuttles/caravans). The configurable rotation interval lets you tune difficulty - longer rotations require more strategic planning and impose more limited trade supplies.

**Q: What if I don't have the Odyssey DLC?**
A: This mod requires Odyssey and will not function without it. Odyssey introduces the Traders Guild faction and orbital settlements that this mod enhances.

**Q: Why isn't Trader Type X showing up?**
A: Trader types are selected randomly based on their `commonality` values in the game files. Some traders (like Pirate Merchant) are rarer than others. Visit more settlements or wait for rotations to see all types.

**Q: Can I change rotation intervals mid-save?**
A: Yes! Changes apply immediately. Existing settlements will respect the new interval on their next rotation.

**Q: Do I need to visit a settlement before traders start rotating?**
A: No - virtual schedules begin immediately. Unvisited settlements show accurate previews of their current trader type.

## Support & Contributing

### Bug Reports

Found a bug? Please report it on the [GitHub Issues](https://github.com/sam-hunt/BetterTradersGuild/issues) page with:

- RimWorld version and mod list
- Steps to reproduce
- Log file (`Player.log`) if you're getting errors
- Screenshots if applicable

### Feature Requests

Feature ideas are welcome! Check the [PLAN.md roadmap](PLAN.md) first to see if it's already planned.

### Contributing

This is a personal project, but contributions are welcome. Check [CLAUDE.md](CLAUDE.md) for development setup and architecture notes.

## Credits

**Author**: Sam Hunt ([@sam-hunt](https://github.com/sam-hunt))

**Built With**:

- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike - Runtime patching library
- RimWorld modding community resources and documentation

**Special Thanks**:

- Ludeon Studios for RimWorld and the modding API
- The RimWorld modding community for ideas, examples, and support
- [Claude Code](https://claude.com/claude-code) for wading through `monodis` output and writing my C#

