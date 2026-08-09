TODOs

- AI defense lords
  - Civilian AI
    - Test no escape route
    - Test shelter escape on external door hack / vacuum breach (new triggers)
    - Test shelter-scoped baby carrying (walkers ignore crew quarters infants)
    - Test caretaker's UMW knife upgrade
  - Defender AI
    - If shuttle pad is free and at least one outer door hacked, reinforcements land in shuttle?
    - Test defender weapon recovery
    - Test defender duty recovery after base destroyed with/without escape route (do they leave infants behind?)

- VGE2 integration
  - Defender overhaul opt-out toggle: spec ready in Docs/Specs/SPEC-defender-overhaul-toggle.md (settings-gated PatchOperationToggledSequence; lets players revert pawnkind rebalance + garrison weights to vanilla for VGE mapgen)
  - Investigate/Activate VGE Gauss cannon code/other VGE TG mechanics
  - Surface one-time mapgen options if VGE2 and BTG mapgen are both enabled:
    - Use BTG every time
    - Use VGE every time
    - Something in between
    - Ask every time

- Review mod settings page layout
- Settlement spawn count scale?
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Rare Subroom placement small room off-by-one?
- Bind band nodes?

- Way more backstories?!

- Investigate mod Settlement Visit compatibility
- Investigate Simple Warrants fulfilment
- Biotech-gated l10n exclusion follow-up: BTG_ConfigureStartingPawnsXenotypes (ScenPartDef, MayRequire Biotech) keeps its English DefInjected entry commented out as a documented exclusion — the compat load root pattern now gives it a proper home (1.6/Mods/Biotech/ with an IfModActive ludeon.rimworld.biotech entry, uncomment the entry there, add Biotech to the probe roster in Scripts/refresh-translation-expectations.py, regen the sidecar)

- Add trade/equivalence-focused storyteller?
- Mod integration: VREA maintenance room
- Mod integration: Choose where to land (independent traders scenario)
- Mod integration: Knick knacks
- Mod integration: trader ships shuttles texture option?
- Mod integration: VE Brewing whisky shelf in Captain's quarters?
- Mod integration: Include UMW weapons in unique weapon pools?

---

Uncommitted — sibling-mod audit prompt (paste into a clean session in this repo):

> Audit our sibling RimWorld mods for the localization/gating gaps found and fixed in BetterTradersGuild on 2026-08-09 (reference implementations: BTG commits d9af1f0, 7de4368, c557a73, 4be8e8b; conventions written up in CLAUDE.md under Localization and Deployment). Candidates in ~/dev: UniqueWeaponsUnbound, UniqueMeleeWeapons, PersonaWeaponsUnbound, TradersStockXenogerms, ArchotechAndroidHardware, ArchotechThumb, BionicThumbGuild, and RimworldModTemplate (so future mods inherit the fixes); skip any without XML content. For each mod check, and port fixes where they apply:
>
> 1. MayRequire attributes on DefInjected entries. The DefInjected loader ignores XML attributes entirely, so such entries log found-no-def startup errors whenever the gating mod/DLC is absent (this bug shipped in BTG's WeaponTraits.xml). Fix: move the entries — and optionally the gated defs themselves, dropping their now-redundant MayRequire — into a LoadFolders-gated compat root: version-folder/Mods/ModName/ containing its own Defs/ and Languages/, loaded via an IfModActive entry in LoadFolders.xml. The compat root must sit BESIDE the well-known folders, never inside Defs/ or Languages/, which load recursively and unconditionally at any depth.
> 2. If the mod has BTG-style translation tooling (a check-translations.py with an expected-injections.json sidecar): English DefInjected files are likely read only as reference and never validated — BTG's was. Port BTG's check_english_definjected pass (every English entry must be a legal, non-duplicate injection point whose text matches the sidecar English), and if a compat root was added, extend the checker's lang_roots/defs_dirs globs and the deploy manifest (StageMod _ModFiles) to match star/Mods/star paths, as BTG did. Also port BTG's multi-root language model (collect_keyed/check_language, 2026-08-09): a language is the union of its dirs across every load root, grouped by folder name and checked as one merged unit the way the game loads it — checking a compat root standalone would wrongly demand the full Keyed set and every other def's injections from it. Duplicate keys must be reported across roots AND within a file via dict membership on the key, not path identity (BTG's first cut used `first is not path`, which can never fire when both occurrences share a file).
> 3. Deliberately excluded/commented-out DefInjected entries for optionally-gated defs (BTG's Biotech ScenPart precedent) that a compat root could now make translatable — list them as follow-ups rather than acting.
> 4. If the mod has a translate skill: check whether its generation workflow enumerates the DefInjected surface from the English DefInjected tree. In BTG that tree turned out to be a strict subset of the required surface (35 of the sidecar's 70 required entries had no English counterpart — English is served by the def XML's own label/description, so English DefInjected files are validated-if-present reference only). BTG's skill now instructs: enumerate from the sidecar's required entries, and source the EN comments from each entry's english field (the exact text the checker compares against, so programmatic sourcing makes drift impossible). Port that instruction wherever the same trap exists.
> 5. Known checker blind spot, deliberately not yet fixed anywhere: the checker cannot tell WHICH root a DefInjected entry lives in, so a gated def's injections placed in the main unconditional tree pass the checker but error at startup for users without the gating mod (and main-tree entries placed in a compat root silently vanish when the gate is closed). Skill prose warns about it; a deterministic check would map defName -> owning load root from the Defs scan and demand each language's entry live under the matching root. If BTG lands that check, port it here too.
>
> Verify claims against each mod's actual loader usage before editing; run each mod's own checker/tests/build before committing, one mod per commit, no pushes.
