# Better Traders Guild

> A RimWorld mod enhancing the Odyssey Traders Guild faction

[![RimWorld](https://img.shields.io/badge/RimWorld-1.6-blue.svg)](https://rimworldgame.com/)
[![Odyssey DLC](https://img.shields.io/badge/DLC-Odyssey%20Required-orange.svg)](https://store.steampowered.com/app/2380740/RimWorld__Odyssey/)
[![Version](https://img.shields.io/badge/Version-0.2.0--beta-yellow.svg)](https://github.com/sam-hunt/BetterTradersGuild/releases)
[![Development Status](https://img.shields.io/badge/Status-Beta-green.svg)](https://github.com/sam-hunt/BetterTradersGuild/releases)

## About

In vanilla RimWorld, the Traders Guild are the faction behind orbital trader events and can be contacted via comms console - but their world map presence doesn't reflect this. Their orbital settlements can't be visited for trade like other faction bases, only attacked late-game via gravship. Settlement maps are relatively lifeless, despite supposedly hosting a coalition of wealthy merchants. There's little reason to interact with them.

This mod brings the Traders Guild in line with other factions:

- **Shuttle caravan trading** - Visit their orbital bases to trade, just like other faction settlements
- **Rotating orbital traders** - Since they lack faction-specific traders, settlements cycle through orbital trader types to simulate active trade operations
- **Overhauled map generation** - Settlements look inhabited and maintained, befitting wealthy merchants
- **Integrated cargo system** - Trade stock appears as physical cargo you can find and steal in the settlement

## Features

### Peaceful Trading

- **Peaceful Access**: Visit Traders Guild orbital bases via shuttle when relations are Neutral or better
- **Dynamic Trader Rotation**: Each settlement cycles through orbital trader types on a configurable schedule
- **Virtual Schedules**: Preview which trader is docked before traveling - what you see is what you get
- **Docked Vessel Display**: World map shows the currently docked trader type

### Custom Settlements

- **New Room Types**: Control Center, Crew Quarters, Cargo Hold, Commander's Lounge, Medbay, and more
- **Lived-In Aesthetics**: Settlements feel inhabited with appropriate furniture and crew
- **Dynamic Cargo System**: Physical cargo spawns from trade inventory - steal it and it's gone from their stock

## Requirements

- **RimWorld 1.6** or later
- **Odyssey DLC** (required - depends on Odyssey's TradersGuild faction)
- **Harmony** (auto-download from Steam Workshop if you don't have it)

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

## Compatibility

- **Safe to add** to existing saves
- **Safe to remove**, just don't be in the cargo vault or have shuttle caravans in the orbit world layer when you do.
- Fully compatible with any mods adding custom orbital trader types

## Contributing

Bug reports and feature requests welcome on [GitHub Issues](https://github.com/sam-hunt/BetterTradersGuild/issues).

For development setup, see [CLAUDE.md](CLAUDE.md).

## Credits

**Author**: Sam Hunt ([@sam-hunt](https://github.com/sam-hunt))

**Built With**:

- [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike - Runtime patching library
- RimWorld modding API, community examples

**Special Thanks**:

- [Ludeon Studios](https://ludeon.com) for RimWorld and modding API
- [The RimWorld modding community](https://steamcommunity.com/app/294100/workshop/) for inspiration and working examples
- [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013) for prefab creation tooling
- [Claude Code](https://claude.com/claude-code) for wading through `monodis` output and breathing C#
