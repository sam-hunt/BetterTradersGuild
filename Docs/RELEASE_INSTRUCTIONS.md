# GitHub Release Instructions

This document provides a generic, reusable workflow for creating GitHub releases of Better Traders Guild.

## Version Naming Conventions

Follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html) with pre-release identifiers:

**Pattern:** `MAJOR.MINOR.PATCH-prerelease.BUILD`

**Examples:**

- `0.1.0-alpha.1` - First alpha build of version 0.1.0
- `0.1.0-alpha.2` - Second alpha build (bug fixes/iterations)
- `0.1.0-beta.1` - First beta build (feature complete, testing phase)
- `0.1.0-rc.1` - First release candidate
- `0.1.0` - Stable release
- `0.1.1` - Patch release with bug fixes
- `0.2.0` - Minor release with new features

**Alpha iteration strategy:**

- Increment alpha number for bug fixes within the same version (e.g., `0.1.0-alpha.1` → `0.1.0-alpha.2`)
- Move to beta when feature-complete and ready for broader testing
- Reserve patch version increments for stable releases

## Pre-Release Checklist

Before starting the release process, verify:

- [ ] **CHANGELOG.md updated** with new version section documenting all changes
- [ ] **docs/ALPHA_TESTING_CHECKLIST.md updated** (for alpha releases) with version number and new test cases
- [ ] **Clean build completed** with 0 errors and 0 warnings
- [ ] **All changes committed** to git (working directory clean)
- [ ] **Git status verified** - no uncommitted changes

## Release Workflow

### Step 1: Update Documentation

**1. Update CHANGELOG.md**

Add a new section at the top following Keep a Changelog format:

```markdown
## [VERSION] - YYYY-MM-DD

### Overview

Brief description of the release.

### Added / Changed / Fixed / Removed

- Feature or fix description
- Another change

### Technical Details

Any relevant technical information.
```

**Example:**

```markdown
## [0.1.0-alpha.2] - 2025-11-09

### Overview

Patch release fixing two bugs discovered in alpha.1.

### Fixed

- Signal jammer logic for gift pods
- Invalid floor type in Nursery definition
```

**2. Update docs/ALPHA_TESTING_CHECKLIST.md (for alpha releases)**

Update version in title and add "What's New" section if needed.

**3. Verify About.xml**

Note: Do NOT add `<version>` tag to About.xml as RimWorld logs errors for unknown tags.

### Step 2: Build and Package

**1. Clean Build**

From WSL/Linux:

```bash
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild/Source"

# Clean previous build
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" \
  BetterTradersGuild.csproj /t:Clean /p:Configuration=Release

# Build release configuration
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" \
  BetterTradersGuild.csproj /p:Configuration=Release
```

Verify output shows `0 Error(s)` and `0 Warning(s)`.

**2. Create Release Package**

Package only runtime-required files directly from source (no staging needed):

```powershell
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

# PowerShell packaging script (run from WSL or Windows)
powershell.exe -Command "
\$ErrorActionPreference = 'Stop'

# Create temp zip directory structure
\$tempDir = New-Item -ItemType Directory -Force -Path 'temp_zip/BetterTradersGuild'

# Copy runtime files directly from source locations
Copy-Item -Recurse 'About' \"\$tempDir/About\"
Copy-Item -Recurse 'Defs' \"\$tempDir/Defs\"
Copy-Item -Recurse 'Patches' \"\$tempDir/Patches\"
Copy-Item 'docs/ALPHA_TESTING_CHECKLIST.md' \"\$tempDir/\"  # For alpha releases

# Create Assemblies folder and copy the Release DLL
New-Item -ItemType Directory -Force -Path \"\$tempDir/Assemblies\" | Out-Null
Copy-Item 'Source/bin/Release/BetterTradersGuild.dll' \"\$tempDir/Assemblies/\"

# Create ZIP
Compress-Archive -Path 'temp_zip/BetterTradersGuild' -DestinationPath 'BetterTradersGuild-{VERSION}.zip' -Force

# Cleanup
Remove-Item -Recurse -Force 'temp_zip'

Write-Host 'Package created successfully'
Get-Item 'BetterTradersGuild-{VERSION}.zip' | Select-Object Name, Length
"
```

**Example:**

```powershell
# Replace {VERSION} with actual version (e.g., v0.1.0-alpha.2)
Compress-Archive -Path 'temp_zip/BetterTradersGuild' -DestinationPath 'BetterTradersGuild-v0.1.0-alpha.2.zip' -Force
```

**Note on Build Configurations:**

- **Debug builds** output to `../Assemblies/` for immediate RimWorld testing
- **Release builds** output to `Source/bin/Release/` for clean packaging
- Always build in Release configuration before creating packages

**Expected package structure:**

```
BetterTradersGuild/
├── About/
│   ├── About.xml
│   └── Preview.png
├── Assemblies/
│   └── BetterTradersGuild.dll
├── Defs/
│   ├── LayoutDefs/
│   ├── LayoutRoomDefs/
│   └── PrefabDefs/
├── Patches/
└── ALPHA_TESTING_CHECKLIST.md
```

### Step 3: Commit and Tag

**1. Stage all changes**

```bash
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

git add -A
git status  # Verify changes
```

**2. Create release commit**

Use a descriptive commit message following conventional commits:

```bash
git commit -m "{type}: {description}

{detailed changes if needed}"
```

**Examples:**

For major releases:

```bash
git commit -m "chore: Prepare v0.1.0-alpha.1 release

- Add version tracking to CHANGELOG.md
- Create comprehensive release documentation
- Generate mod preview image
- Clean build verified (0 errors, 0 warnings)"
```

For patch releases:

```bash
git commit -m "fix: Prepare v0.1.0-alpha.2 patch release

- Fix signal jammer logic for gift pods
- Fix invalid floor type in Nursery definition
- Update CHANGELOG.md and testing documentation"
```

**3. Push changes**

```bash
git push origin main
```

**4. Create and push tag**

```bash
# Create annotated tag
git tag -a {VERSION} -m "{Release title}

{Brief description of release highlights}"

# Push tag to GitHub
git push origin {VERSION}
```

**Examples:**

```bash
git tag -a v0.1.0-alpha.1 -m "Release v0.1.0-alpha.1 - Peaceful Trading Features

First alpha release with peaceful trading fully implemented."

git push origin v0.1.0-alpha.1
```

```bash
git tag -a v0.1.0-alpha.2 -m "Release v0.1.0-alpha.2 - Bug Fix Patch

Fixes signal jammer logic and floor type definition."

git push origin v0.1.0-alpha.2
```

### Step 4: Create GitHub Release

**Option A: Web Interface**

1. Navigate to: `https://github.com/sam-hunt/BetterTradersGuild/releases`
2. Click **"Draft a new release"**
3. Fill in details:
   - **Tag:** Select the tag you just pushed (e.g., `v0.1.0-alpha.2`)
   - **Release title:** `{VERSION} - {Title}`
   - **Description:** Use template below
   - **Pre-release:** ✅ Check for alpha/beta releases
   - **Attach binary:** Upload `BetterTradersGuild-{VERSION}.zip`
4. Click **"Publish release"**

**Option B: GitHub CLI** (if `gh` is installed)

```bash
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"

gh release create {VERSION} \
  --title "{VERSION} - {Title}" \
  --notes-file CHANGELOG.md \
  --prerelease \
  /tmp/release-staging/BetterTradersGuild-{VERSION}.zip
```

**Example:**

```bash
gh release create v0.1.0-alpha.2 \
  --title "v0.1.0-alpha.2 - Bug Fix Patch" \
  --notes-file CHANGELOG.md \
  --prerelease \
  /tmp/release-staging/BetterTradersGuild-v0.1.0-alpha.2.zip
```

### GitHub Release Description Template

**For Alpha Releases:**

```markdown
# Better Traders Guild {VERSION}

## 🚨 Alpha Release

This is an alpha release of Better Traders Guild. [Brief status summary]

## 🎯 What's New

[Highlight key changes - new features, bug fixes, etc.]

### [If applicable] Phase X: [Feature Name] (Status)

- Feature bullet points
- More details

## 📋 Alpha Limitations

**Known issues:**

- List any known bugs or incomplete features
- Reference CHANGELOG.md for details

## 📥 Installation

1. **Ensure you have Harmony installed** ([Steam Workshop](https://steamcommunity.com/workshop/filedetails/?id=2009463077))
2. **Download `BetterTradersGuild-{VERSION}.zip`** from this release
3. **Extract to your RimWorld Mods folder:**
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - Mac: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/`
   - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods/`
4. **Enable in-game:** Options → Mods → Check "Better Traders Guild"
5. **Load order:** Core → Harmony → Odyssey DLC → Better Traders Guild
6. **Restart RimWorld**

## 🧪 Testing

Please help test! See the included `ALPHA_TESTING_CHECKLIST.md` (in the package root) for a comprehensive testing guide.

## 📝 Requirements

- **RimWorld 1.6+**
- **Odyssey DLC** (required)
- **Harmony** (auto-downloaded from Steam)

## 🐛 Bug Reports

Found a bug? Please [open an issue](https://github.com/sam-hunt/BetterTradersGuild/issues) with:

- Your Player.log file
- Steps to reproduce
- Mod list
- Screenshots if applicable

## 📖 Full Changelog

See [CHANGELOG.md](https://github.com/sam-hunt/BetterTradersGuild/blob/main/CHANGELOG.md) for complete details.

---

**Save-game safe** - Can be added or removed from existing saves ✅
```

**For Stable Releases:**

Adjust the template:

- Remove "🚨 Alpha Release" section
- Remove "Alpha Limitations" section
- Change "🧪 Testing" to focus on user feedback rather than bug hunting
- Keep installation, requirements, and bug reports sections

## Post-Release Checklist

After publishing the release:

- [ ] **Test the download** - Download ZIP from GitHub and verify it extracts correctly
- [ ] **Verify mod loads** - Test in clean RimWorld installation
- [ ] **Update any documentation** that references the latest release
- [ ] **Monitor GitHub Issues** for bug reports
- [ ] **Announce to testers** (for alpha releases) with link to release and testing checklist

## Troubleshooting

**Build fails with errors:**

- Check `Source/BetterTradersGuild.csproj` for correct DLL references
- Verify .NET Framework 4.7.2 is installed
- Check Player.log for compilation errors

**ZIP package too large:**

- Verify you're not including `Source/`, `docs/`, `.git/`, or other dev files
- Only runtime files should be included

**Tag already exists:**

- Delete local tag: `git tag -d {VERSION}`
- Delete remote tag: `git push origin --delete {VERSION}`
- Recreate with correct details

**GitHub release upload fails:**

- Verify ZIP file is under 2GB (should be ~200-500KB for this mod)
- Try using GitHub CLI (`gh`) instead of web interface
- Check internet connection stability

## Examples

### Example 1: Patch Release (v0.1.0-alpha.2)

```bash
# 1. Update docs
# Edit CHANGELOG.md, docs/ALPHA_TESTING_CHECKLIST.md

# 2. Build
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild/Source"
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" \
  BetterTradersGuild.csproj /t:Clean /p:Configuration=Release
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" \
  BetterTradersGuild.csproj /p:Configuration=Release

# 3. Package
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"
powershell.exe -Command "
\$tempDir = New-Item -ItemType Directory -Force -Path 'temp_zip/BetterTradersGuild'
Copy-Item -Recurse 'About' \"\$tempDir/About\"
Copy-Item -Recurse 'Defs' \"\$tempDir/Defs\"
Copy-Item -Recurse 'Patches' \"\$tempDir/Patches\"
Copy-Item 'docs/ALPHA_TESTING_CHECKLIST.md' \"\$tempDir/\"
New-Item -ItemType Directory -Force -Path \"\$tempDir/Assemblies\" | Out-Null
Copy-Item 'Source/bin/Release/BetterTradersGuild.dll' \"\$tempDir/Assemblies/\"
Compress-Archive -Path 'temp_zip/BetterTradersGuild' -DestinationPath 'BetterTradersGuild-v0.1.0-alpha.2.zip' -Force
Remove-Item -Recurse -Force 'temp_zip'
"

# 4. Commit and tag
cd "/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/BetterTradersGuild"
git add -A
git commit -m "fix: Prepare v0.1.0-alpha.2 patch release

- Fix signal jammer logic for gift pods
- Fix invalid floor type in Nursery definition
- Update CHANGELOG.md and testing documentation"
git push origin main
git tag -a v0.1.0-alpha.2 -m "Release v0.1.0-alpha.2 - Bug Fix Patch"
git push origin v0.1.0-alpha.2

# 5. Create GitHub release
gh release create v0.1.0-alpha.2 \
  --title "v0.1.0-alpha.2 - Bug Fix Patch" \
  --notes-file CHANGELOG.md \
  --prerelease \
  BetterTradersGuild-v0.1.0-alpha.2.zip
```

### Example 2: Major Feature Release (v0.2.0-alpha.1)

Same steps, but:

- Update version to `0.2.0-alpha.1` (minor version bump for new features)
- Commit message: `feat: Prepare v0.2.0-alpha.1 release`
- Tag message highlights new phase/features

---

**Remember:** This is a living document. Update it as the release process evolves!

