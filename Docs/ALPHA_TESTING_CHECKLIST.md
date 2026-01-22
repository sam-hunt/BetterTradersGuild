# Alpha Testing Checklist - Better Traders Guild v0.1.0-alpha.2

Thank you for testing the Better Traders Guild alpha! This checklist will help ensure comprehensive testing of all implemented features.

## What's New in Alpha.2

This patch release fixes two bugs from alpha.1:

- **Fixed:** Signal jammer logic now allows gift pods to reach TradersGuild settlements even when hostile (vanilla behavior for repairing relations)
- **Fixed:** Corrected invalid floor type in Nursery room definition (`CarpetBlueLight` → `CarpetBluePastel`)

## Pre-Testing Setup

### Installation Verification

- [ ] **Harmony is installed** (check Mod Manager or [download from Steam](https://steamcommunity.com/workshop/filedetails/?id=2009463077))
- [ ] **Odyssey DLC is owned and enabled** (required dependency)
- [ ] **Mod extracted to correct location:**
  - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BetterTradersGuild\`
  - Mac: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/BetterTradersGuild/`
  - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods/BetterTradersGuild/`
- [ ] **Load order correct:** Core → Harmony → Odyssey DLC → Better Traders Guild
- [ ] **RimWorld restarted** after enabling mod
- [ ] **No red errors in mod list** on startup

### Test Environment

**RimWorld Version:** \***\*\_\_\_\*\***
**Mods Installed (list any besides BTG):** \***\*\_\_\_\*\***
**Save Type:** [ ] New game [ ] Existing save
**Date Started Testing:** \***\*\_\_\_\*\***

---

## Peaceful Trading Features (Priority Testing)

### 1. Basic Settlement Visitation

**Goal:** Verify you can visit Traders Guild settlements peacefully.

- [ ] **Find Traders Guild settlements on world map** (they appear as orange orbital settlements)
- [ ] **Check faction relations:** Options → Factions → Traders Guild (should show goodwill value)
- [ ] **Verify settlement inspection string** shows "Docked vessel: [Trader Type]"
- [ ] **Visit with good relations:**
  - [ ] Relations: Neutral or better (0+ goodwill)
  - [ ] "Visit for Trade" gizmo appears on settlement
  - [ ] Can form shuttle to visit
  - [ ] Can form caravan to visit (if you prefer)
- [ ] **Cannot visit with hostile relations:**
  - [ ] Relations: Hostile (negative goodwill)
  - [ ] "Visit for Trade" gizmo does NOT appear
  - [ ] Settlement still requires signal jammer for hostile approach

**Notes:** \***\*\_\_\_\*\***

### 2. Gift Pod Mechanics (New in Alpha.2)

**Goal:** Verify gift pods can reach hostile TradersGuild settlements.

- [ ] **Make TradersGuild hostile** (attack them or let goodwill decay to negative)
- [ ] **Prepare a transport pod** with gift items
- [ ] **Target a hostile TradersGuild settlement** with the pod
- [ ] **Pod should reach successfully** (no signal jammer requirement)
- [ ] **Goodwill should increase** (standard gift mechanics)

**Notes:** \***\*\_\_\_\*\***

### 3. Trading Dialog

**Goal:** Verify trade interactions work correctly.

- [ ] **Trade dialog opens** when arriving at settlement
- [ ] **Trader type matches** inspection string (e.g., if it said "Orbital Exotic Goods Trader", the dialog title should match)
- [ ] **Inventory looks appropriate** for trader type:
  - Bulk Goods: Large quantities, common items
  - Combat Supplier: Weapons, armor, combat gear
  - Exotic: Rare items, artifacts, luxury goods
  - Pirate Merchant: Mixed/questionable goods
- [ ] **Can buy items** successfully
- [ ] **Can sell items** successfully
- [ ] **Silver transfers correctly**

**Notes:** \***\*\_\_\_\*\***

### 4. Trader Rotation System

**Goal:** Verify traders rotate dynamically over time.

- [ ] **Note initial trader type** at a specific settlement
- [ ] **Wait for rotation interval** (default 15 days in-game)
  - Tip: Speed up time with "4" key (Ultra Speed)
  - Or change interval in Mod Settings
- [ ] **Trader type changes** after interval passes
- [ ] **New inventory generated** (different items than before)
- [ ] **Inspection string updates** to show new trader

**Notes:** \***\*\_\_\_\*\***

### 5. Virtual Schedule System

**Goal:** Verify unvisited settlements show accurate trader previews.

- [ ] **Find an unvisited Traders Guild settlement**
- [ ] **Note the "Docked vessel" type** in inspection string (DO NOT VISIT YET)
- [ ] **Now visit the settlement**
- [ ] **Trader in trade dialog matches** the inspection string preview
- [ ] **Test with multiple settlements** (previews should be stable and accurate)

**Notes:** \***\*\_\_\_\*\***

### 6. Desynchronized Schedules

**Goal:** Verify settlements rotate independently.

- [ ] **Check 3-4 different Traders Guild settlements** at the same time
- [ ] **Confirm they show different trader types** (not all the same)
- [ ] **Wait for one rotation interval**
- [ ] **Not all settlements change simultaneously** (each has its own schedule)

**Notes:** \***\*\_\_\_\*\***

### 7. Mod Settings Configuration

**Goal:** Verify configuration options work.

- [ ] **Open Mod Settings:** Options → Mod Settings → Better Traders Guild
- [ ] **Trader Rotation Interval slider present**
- [ ] **Change interval to 5 days** → Save and close
- [ ] **Verify rotation happens after 5 days** (not default 15)
- [ ] **Change interval to 30 days** → Test again
- [ ] **Settings persist** after restarting RimWorld

**Notes:** \***\*\_\_\_\*\***

---

## Enhanced Settlement Generation (Lower Priority)

### 8. Settlement Map Generation

**Goal:** Check custom settlement layouts.

- [ ] **Enter a Traders Guild settlement map** (either peaceful visit or raid)
- [ ] **Layout appears modern/orbital** (not ancient/deserted)
- [ ] **Custom rooms present:**
  - Look for: Command Center, Medical Bay, Barracks, Trade Showcase, etc.
  - Any rooms stand out as obviously custom?
- [ ] **Commander's Quarters identified** (should have a bedroom, bookshelves, unique weapon)

**Notes:** \***\*\_\_\_\*\***

### 9. Known Settlement Generation Limitations (Don't Report These)

These are **expected** limitations in this alpha:

- **Commander's Quarters:** Bedroom might occasionally block doorways
- **Commander's Quarters:** Some furniture may be placed in suboptimal locations
- **Other rooms:** Using vanilla generation algorithms (will be customized later)
- **Dynamic cargo:** Not implemented yet (items don't spawn from trade inventory)

---

## Compatibility & Stability Testing

### 10. Performance & Errors

- [ ] **No red error messages** in dev mode console (press ` key)
- [ ] **Check Player.log** for errors after testing:
  - Location: `%APPDATA%\..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
  - Search for "Better Traders Guild" or "exception"
- [ ] **No noticeable performance issues** (FPS drops, stuttering)
- [ ] **Worldmap pathfinding works** (caravans can path to space settlements)

**Errors Found:** \***\*\_\_\_\*\***

### 11. Save-Game Safety

- [ ] **Save the game** with mod enabled
- [ ] **Reload the save** → Verify everything still works
- [ ] **Disable the mod** → Reload save
- [ ] **Save still loads** without errors (save-safe removal test)
- [ ] **Re-enable mod** → Verify functionality restored

**Notes:** \***\*\_\_\_\*\***

### 12. Mod Compatibility (If Applicable)

If you're testing with other mods:

- [ ] **List other mods installed:** \***\*\_\_\_\*\***
- [ ] **Any conflicts or errors?** \***\*\_\_\_\*\***
- [ ] **Trade still works normally?** \***\*\_\_\_\*\***

---

## Bug Reporting

If you encounter any issues, please report them on [GitHub Issues](https://github.com/sam-hunt/BetterTradersGuild/issues) with:

### Required Information:

1. **Bug description** (what happened vs. what you expected)
2. **Steps to reproduce** (how to make it happen again)
3. **Your Player.log file** (from `%APPDATA%\..\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`)
4. **Mod list** (all mods you have installed)
5. **Screenshots** (if relevant)

### Example Bug Report Template:

```markdown
**Bug:** Trader rotation not working

**Expected:** Trader should change after 15 days
**Actual:** Same trader for 30+ days

**Steps to reproduce:**

1. Visit Traders Guild settlement
2. Note trader type
3. Wait 15 in-game days
4. Return to settlement
5. Same trader still present

**Mods:** Better Traders Guild, Harmony, [list others]

**Log:** [Attach Player.log]

**Screenshots:** [Attach if helpful]
```

---

## Feature Requests & Feedback

Feel free to suggest improvements or new features! Things to consider:

- **What worked well?** \***\*\_\_\_\*\***
- **What was confusing?** \***\*\_\_\_\*\***
- **What would you like to see added?** \***\*\_\_\_\*\***
- **How's the balance/difficulty?** \***\*\_\_\_\*\***

---

## Thank You!

Your testing helps make this mod better! Once you've completed testing:

1. **Share your findings** (GitHub Issues, Discord, or direct message)
2. **Note any dealbreaker bugs** (mod unusable vs. minor annoyances)
3. **Rate the experience** (would you recommend this alpha to others?)

**Overall Experience:** [ ] Great [ ] Good [ ] Needs Work [ ] Broken
**Would Recommend:** [ ] Yes [ ] Maybe [ ] No

**Additional Comments:** \***\*\_\_\_\*\***
