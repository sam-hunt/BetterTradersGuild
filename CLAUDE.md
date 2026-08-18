# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Better Traders Guild** is a RimWorld 1.6 mod that expands player interactions with the Traders Guild faction. Requires Odyssey DLC and Harmony.

**Key Features:**

- Peaceful shuttle caravan trading visits to orbital settlements when relations are good
- Dynamic orbital trader rotation system with deterministic schedules per settlement
- Custom settlement generation with specialized room types and focus on sense of inhabitation
- Hackable cargo vault system linking trade inventory to physical cargo

## Build Commands

```bash
# Build the mod (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build BetterTradersGuild.sln -c Release

# Build only the main project (also triggers the deploy)
dotnet build Source/1.6/BetterTradersGuild.csproj

# Run tests
dotnet test Tests/1.6/BetterTradersGuild.Tests.csproj

# Clean build artifacts
dotnet clean BetterTradersGuild.sln

# Stage the mod into an arbitrary folder (used by CI; same manifest as the local deploy)
dotnet build Source/1.6/BetterTradersGuild.csproj -c Release \
  -t:StageMod -p:StageDir=/path/to/output/BetterTradersGuild
```

The build system auto-detects the RimWorld installation path on Windows/Linux/Mac (including WSL targeting a Windows install). For CI builds without RimWorld installed, it falls back to the `Krafs.Rimworld.Ref` NuGet package.

### Deployment

The repo lives in `~/dev/BetterTradersGuild`, separate from the RimWorld Mods folder. Every local build redeploys automatically and atomically — there is no separate clean step to remember.

- **Single source of truth:** what ships is the `_ModFiles` ItemGroup in the `StageMod` target (`Source/1.6/BetterTradersGuild.csproj`) — the only place to edit the manifest. It whitelists by file type per content folder (`About`, `Assemblies`, `Defs`, `Patches`, `Languages`, plus `Textures`/`Sounds` if ever added), matched at the root, under any version folder, and (for `Defs`/`Languages`) under gated compat load roots (`<version>/Mods/<Mod Name>/`, see LoadFolders.xml), so a new content folder of an existing type deploys automatically and only a brand-new file type needs a new line. Only game-loaded types are listed (e.g. `.xml`), so stray dev notes (`README.md`, `RESEARCH.md`) never ship.
- **Self-cleaning:** `StageMod` wipes `$(StageDir)` and recopies from source, so renamed/deleted files never linger. The post-build `DeployToModFolder` target calls it with `StageDir = $RIMWORLD_PATH/Mods/BetterTradersGuild` (only when a local RimWorld install is detected).
- **CI reuses the same target:** `.github/workflows/release.yml` invokes `StageMod` with `-p:StageDir=<release dir>` instead of its own `cp` list, so the release zip can't drift from the local deploy. Triggers on `v*.*.*` tags.
- **Stop hook (`.claude/hooks/sync-mod.sh`, gitignored/local-only):** after each turn, rebuilds+redeploys only when mod source/content actually changed (doc-only turns are a fast no-op) and warns on build failure rather than leaving a stale DLL. Mechanism details are in the script's own header.

**WSL Setup:** Requires `RIMWORLD_PATH` env var in `~/.bashrc` pointing to the Windows RimWorld install (e.g., `/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`). The csproj auto-detects `RimWorldWin64_Data` when the Linux data folder isn't found.

## Architecture

### Entry Point

`Source/1.6/Core/ModInitializer.cs` - Static constructor with `[StaticConstructorOnStartup]` auto-patches via Harmony attribute discovery. Logs initialization message with patch count.

**Settings Access:** `BetterTradersGuildMod.Settings` provides global access to mod configuration.

### Key Patterns

**Harmony Patching:** All patches use `[HarmonyPatch]` attributes for automatic discovery. Patches are organized by target class in subdirectories under `Patches/`. Most are Postfix patches that check `TradersGuildHelper.IsTradersGuildSettlement()` before modifying behavior.

**Prefix discipline:** Prefer Postfix. A Prefix should only set flags or pre-align state, returning void/true. Skipping the original (`return false`) is a mod-compat hazard — other mods' transpilers and the original's side effects silently die with it — so before writing one, check whether an additive Postfix can express the change (vanilla's condition being a strict subset of ours is what made the den defeat check postfix-able). When a skip is genuinely unavoidable because vanilla's effect must not happen (e.g. `TryDestroyStock` while a settlement map is loaded), scope it tightly to BTG-owned defs/state and preserve any vanilla effects other patches may rely on.

**Private patch targets:** resolve via a cached `AccessTools.Method` with `[HarmonyPrepare]`/`[HarmonyTargetMethod]`, never a string-named attribute: on API drift, Prepare skips just that one patch instead of `PatchAll` throwing and aborting every later patch, and a `VerifyPatched()` hook wired into `ReflectionVerification.VerifyAll()` reports the drift at startup (example: `SiteCheckAllEnemiesDefeated`).

**Namespace Convention:** Use `*Patches` suffix for patch namespaces to avoid RimWorld type conflicts (e.g., `SettlementPatches`, `CaravanPatches`).

**DefOf Constants:** Static `[DefOf]` classes in `DefRefs/` provide compile-time safety for XML definitions (e.g., `Factions.TradersGuild`, `LayoutRooms.CommandersQuarters`).

**Room Contents Workers:** Each room type has a `RoomContents_[RoomName].cs` file that handles specialized furniture and pawn spawning using `Prefab` definitions.

**Comments:** In `Source/`, use plain `//` comments only — do **not** write XML doc comments (`///`, `<summary>`, `<param>`, etc.); nothing consumes them there, so they add ceremony without benefit. `Tests/` is exempt: XML doc comments are fine there (they read cleanly on the test helpers and are the most likely place to adopt a tool that parses them).

**No `?.`/`??` on Unity objects:** Never use null propagation or null coalescing on receivers deriving from `UnityEngine.Object` (`Material`, `Texture`, `RenderTexture`, `GameObject`, ...). Unity overloads `==` so destroyed objects compare equal to null; `?.` bypasses the overload with a raw reference check and then throws `MissingReferenceException` on the member access. Use explicit `== null`/`!= null` guards for those types. Verse types (`Thing`, `Pawn`, `ThingComp`, defs) are plain classes where `?.` is fine. Enforced at build time by UNT0007/UNT0008 (Microsoft.Unity.Analyzers). Corollary: never bulk-apply Roslynator's RCS1146 (use conditional access) fixer to Unity-typed receivers; see the note in `.editorconfig`.

### Reflection Verification

BTG reaches private RimWorld members and optional-mod APIs by string-named reflection. To catch API drift at startup instead of as a silent runtime failure (the `lifeSupportUnitPowerOutput` bug), every reflection dependency is self-checked when the game loads. Pattern ported from the sister mod `UniqueWeaponsUnbound`.

- **Single trigger, not a registry:** `ReflectionVerification.VerifyAll()` (`Core/`) runs once from `BetterTradersGuildMod`'s static ctor, right after `Harmony.PatchAll()`. Each looked-up member name still lives in exactly **one** owner — `VerifyAll()` only triggers the checks, so nothing is declared twice or can drift apart.
- **Base-game lookups (Pattern A):** the owning class caches its `FieldInfo`/`MethodInfo` in `static readonly` fields and exposes `public static void VerifyReflection()`, which `Log.Error`s a message naming the member, the user-visible consequence, and "RimWorld API may have changed." Shared base-game members live in single owners under `Helpers/Reflection/` (`TraderTrackerReflection`, `CompHackableReflection`, `RefuelableReflection`); single-consumer ones verify in-place. Every runtime caller still null-guards, so a missing member degrades to a no-op rather than throwing.
- **Optional-mod integrations (Pattern B):** dedicated classes under `Integrations/` (`HARIntegration`, `VEPipesIntegration`) resolve their type/members in a `try/catch` static ctor, expose `static bool Available`, and `Log.Warning` **only when the mod is detected present but a member failed to resolve** (silent when the mod isn't installed). `VerifyAll()` forces each static ctor via `_ = X.Available;`.
- **Adding a new reflection site:** put the lookup in the relevant owner (or a new `Helpers/Reflection/` / `Integrations/` class), add its `VerifyReflection()`/`Available`, and call it from `VerifyAll()`. Note: checks must run against the real game DLL — the `Krafs.Rimworld.Ref` package strips private members, so they can't be unit-tested.

### Map Generation Architecture

BTG uses a declarative, XML-driven approach for custom map generation.

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

### Trader Rotation and Cargo Vault Stock

Both subsystems carry non-obvious ordering and lifecycle contracts (virtual rotation schedules,
stock/dialog desync, defeat-time stock handoff). The full write-up lives in
`Source/1.6/Patches/Settlement/CLAUDE.md`, which loads automatically when working in that
directory. Read it before touching `MapComponents/SettlementStockCache.cs` or
`RoomContents/CargoHoldVault/CargoVaultHelper.cs` too, since those collaborate from outside it.

### Salvagers Raid Weight System

Two tightly-coupled patches in `Patches/Incidents/` boost Salvagers raid probability on TG maps:

1. **PawnGroupMakerUtilityTryGetRandomFactionForCombatPawnGroupWeighted.cs** - Sets `RaidFactionSelectionContext.IsOnTradersGuildMap` flag
2. **FactionDefRaidCommonalityFromPoints.cs** - Reads flag, multiplies Salvagers weight by `ModSettings.salvagersRaidWeightMultiplier`

The context flag pattern is required because `RaidCommonalityFromPoints` has no map parameter.

### Testing

XUnit tests in `Tests/1.6/` validate spatial algorithms (placement calculators, subroom packing). Tests use ASCII diagram visualization for room layouts. Run with `dotnet test Tests/1.6/BetterTradersGuild.Tests.csproj`.

**Excluded Test Files:** `Tests/Tools/RegenerateDiagrams.cs` (utility), `Tests/Helpers/DiagramGeneratorTests.cs` - excluded via `<Compile Remove="..." />`.

Run tests natively from WSL — never build from the Windows toolchain: it corrupts the WSL-side incremental state (shared `obj/` seen under different path roots). The test csproj copies the live install's runtime DLLs beside the test DLL so tests may load Verse types (see the Assembly-CSharp-firstpass comment there — mono resolves field types eagerly where the Windows CLR is lazy); `OutputPath` is Release-gated so Debug `dotnet test` builds can't overwrite the shipped DLL in `1.6/Assemblies`.

### Localization

English is the source of truth: `1.6/Languages/English/Keyed/BTG.xml` (`BTG_` prefix) plus a real DefInjected surface under `1.6/Languages/English/DefInjected/<DefType>/`. This mod's facts and coined-term glossaries live in the `translate` skill (`glossary/<Language>.md` beside it); the contributor-facing rules and the language roster live in `CONTRIBUTING.md`.

- **Shared l10n toolkit (`l10n/` submodule):** the family-wide translation process, per-language mechanics references, cross-language lessons, Workshop conventions, and the checker/refresh script engines live in the `rimworld-l10n` repo, consumed here as the `l10n/` git submodule (canonical working checkout: `~/dev/rimworld-l10n`). `Scripts/check-translations.py` and `Scripts/refresh-translation-expectations.py` are thin per-repo config shims over its engines. If `l10n/` is empty, run `git submodule update --init`. Never edit `l10n/` in place here: mod-independent learnings go upstream in the canonical checkout, then the pin is bumped in each consuming repo; mod-specific learnings go in this repo's skill/glossary.

- **Optional-mod content:** MayRequire is honored on defs and patch nodes but IGNORED on DefInjected entries, so content whose strings depend on an optional mod or DLC ships from a LoadFolders-gated compat root (`1.6/Mods/<Name>/` with its own `Defs`/`Languages` inside; currently `UniqueMeleeWeapons` for the silver-inlay melee trait and `Biotech` for the xenotype ScenPart). Compat roots must sit beside the well-known folders, never inside one — anything under `1.6/Defs/**` or `1.6/Languages/**` loads unconditionally at any depth.

- **Workshop title coupling:** each language's `BTG_Settings_ModName` Keyed value is the localized Steam Workshop title and must equal the title line (line 1) of `.steamworkshop/Description/<Language>.txt` — always change the two together (English keeps `Better Traders Guild` in both).

- **Checker:** `python3 Scripts/check-translations.py --strict` validates key parity, placeholders, DefInjected legality and load-root placement, staleness (EN comments), and file hygiene. CI's release gate runs it non-strict.
- **Sidecar:** `Scripts/expected-injections.json` is the authority for legal DefInjected keys. Regenerate with `python3 Scripts/refresh-translation-expectations.py` — it boots RimWorld (game must be closed) with a pinned mod list via the L10nProbe dev mod (source lives at `l10n/probe/`; build/deploy it only from the canonical `~/dev/rimworld-l10n` checkout — a submodule copy refuses to deploy by design), then restores `ModsConfig.xml`.
- **Probe DLC set:** the probe boots with Biotech and Odyssey active (`CANONICAL_ACTIVE_MODS` in the refresh script); the checker's `REQUIRED_DLCS` rejects a sidecar generated without either, since gated defs would drop out of the dump and their shipped translations would turn illegal.
- **Policy:** translation generation passes run only on explicit request (they are token-expensive). Infra/tooling changes are always fine.
