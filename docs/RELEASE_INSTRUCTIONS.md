# GitHub Release Instructions for v0.1.0-alpha.1

## Pre-Release Checklist

- [x] About.xml updated with version 0.1.0-alpha.1
- [x] Preview.png created (920x920 placeholder)
- [x] CHANGELOG.md created and comprehensive
- [x] README.md updated with alpha status and limitations
- [x] Clean build completed (0 errors, 0 warnings)
- [x] Release package created: BetterTradersGuild-v0.1.0-alpha.1.zip (223 KB)

## Release Package Location

The release package is ready at:

```
/tmp/BetterTradersGuild-v0.1.0-alpha.1.zip
```

## Creating the GitHub Release

### Step 1: Commit and Push Changes

First, commit all the release preparation changes:

```bash
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

# Stage all changes
git add -A

# Create release commit
git commit -m "chore: Prepare v0.1.0-alpha.1 release

- Add version to About.xml
- Create CHANGELOG.md with comprehensive release notes
- Update README.md with alpha status and limitations
- Generate Preview.png placeholder
- Clean build verified (0 errors, 0 warnings)
"

# Push to GitHub
git push origin main
```

### Step 2: Create Git Tag

```bash
# Create annotated tag for the release
git tag -a v0.1.0-alpha.1 -m "Release v0.1.0-alpha.1 - Phase 2 Trading Features

First alpha release with peaceful trading fully implemented.
See CHANGELOG.md for detailed release notes."

# Push tag to GitHub
git push origin v0.1.0-alpha.1
```

### Step 3: Create GitHub Release via Web Interface

1. **Navigate to your repository** on GitHub
2. **Click "Releases"** in the right sidebar (or go to `https://github.com/yourusername/BetterTradersGuild/releases`)
3. **Click "Draft a new release"**
4. **Fill in release details:**

   **Tag:** `v0.1.0-alpha.1` (select existing tag you just pushed)

   **Release title:** `v0.1.0-alpha.1 - Phase 2 Trading Features (Alpha)`

   **Description:** Copy from the template below

   **Pre-release checkbox:** ✅ **Check "This is a pre-release"**

   **Attach binary:** Upload `BetterTradersGuild-v0.1.0-alpha.1.zip`

5. **Click "Publish release"**

### GitHub Release Description Template

```markdown
# Better Traders Guild v0.1.0-alpha.1

## 🚨 Alpha Release

This is the first alpha release of Better Traders Guild! **Phase 2 (Peaceful Trading)** is complete and fully functional. **Phase 3 (Enhanced Settlement Generation)** is partially implemented.

## 🎯 What's New

### Phase 2: Peaceful Trading (Complete)

- **Visit Traders Guild Bases** - Travel via shuttle or caravan when relations are good
- **Dynamic Orbital Trader Rotation** - 4+ trader types rotate every few days
- **Virtual Schedules** - Preview which trader is docked before visiting
- **Configurable Rotation** - Adjust trader rotation interval (5-30 days)
- **Docked Vessel Display** - See current trader on world map
- **Mod Compatible** - Automatically supports custom orbital trader types

### Phase 3: Enhanced Settlement Generation (In Progress)

- Custom settlement layouts with 18 room types
- 10 custom prefabs for furniture arrangements
- Captain's Quarters with unique weapon generation (partial)

## 📋 Alpha Limitations

**Phase 3 is incomplete** - See [CHANGELOG.md](https://github.com/yourusername/BetterTradersGuild/blob/main/CHANGELOG.md) for detailed list of known limitations.

**What's fully working:**

- ✅ All peaceful trading features (Phase 2)
- ✅ Custom settlement layouts
- ✅ Basic Captain's Quarters generation

## 📥 Installation

1. **Ensure you have Harmony installed** ([Steam Workshop](https://steamcommunity.com/workshop/filedetails/?id=2009463077))
2. **Download `BetterTradersGuild-v0.1.0-alpha.1.zip`** from this release
3. **Extract to your RimWorld Mods folder:**
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - Mac: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
   - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods/`
4. **Enable in-game:** Options → Mods → Check "Better Traders Guild"
5. **Load order:** Core → Harmony → Odyssey DLC → Better Traders Guild
6. **Restart RimWorld**

## 🧪 Testing Checklist

Please help test by verifying:

- [ ] Can visit Traders Guild bases with good relations
- [ ] Trade dialog opens with orbital trader inventory
- [ ] Trader type rotates after configured interval
- [ ] Docked vessel displays on world map inspection
- [ ] Mod settings work (trader rotation slider)
- [ ] No errors in Player.log
- [ ] Can add/remove from existing saves

## 📝 Requirements

- **RimWorld 1.6+**
- **Odyssey DLC** (required)
- **Harmony** (auto-downloaded from Steam)

## 🐛 Bug Reports

Found a bug? Please [open an issue](https://github.com/yourusername/BetterTradersGuild/issues) with:

- Your Player.log file
- Steps to reproduce
- Mod list
- Screenshots if applicable

## 🙏 Credits

- **Author:** Sam Hunt
- **Powered by:** Harmony 2.3.3+ by Andreas Pardeike
- **RimWorld:** Ludeon Studios

## 📖 Full Changelog

See [CHANGELOG.md](https://github.com/yourusername/BetterTradersGuild/blob/main/CHANGELOG.md) for complete details.

---

**Save-game safe** - Can be added or removed from existing saves ✅
```

### Step 4: Update README.md Links

After publishing the release, update any placeholder links in README.md to point to the actual release:

```bash
# Edit README.md to replace GitHub username placeholders
# Replace "yourusername" with your actual GitHub username
# Replace release URL placeholders
```

### Step 5: Announce to Testers

Send your friend:

1. Link to the GitHub release
2. Link to ALPHA_TESTING_CHECKLIST.md (created next)
3. Instructions to report issues via GitHub Issues

## Alternative: Using GitHub CLI

If you have `gh` CLI installed:

```bash
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

# Create release
gh release create v0.1.0-alpha.1 \
  --title "v0.1.0-alpha.1 - Phase 2 Trading Features (Alpha)" \
  --notes-file CHANGELOG.md \
  --prerelease \
  BetterTradersGuild-v0.1.0-alpha.1.zip
```

## Post-Release

After publishing:

1. **Test the download** - Download from GitHub and verify it extracts correctly
2. **Update any documentation** that references the release
3. **Monitor GitHub Issues** for bug reports
4. **Thank your testers!** 🎉

