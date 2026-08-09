# Contributing

Thanks for your interest in improving Better Traders Guild! Bug reports,
suggestions and pull requests are welcome.

## Localization

| Language | Status | Credit |
| -------- | ------ | ------ |
| English  | Source | —      |

Translations for any language RimWorld supports are welcome. See "Contributing
a translation" below for the conventions to follow.

Statuses: **Source** (the authoritative English strings), **Machine-assisted**
(generated with terminology grounded against the official RimWorld
localization; awaiting native review), **Native** (written or reviewed by a
native speaker), **Planned** (not started — contributions welcome).

### Contributing a translation

- Files live under `1.6/Languages/<Language>/` (`Keyed/` and `DefInjected/`),
  mirroring the structure of `1.6/Languages/English/`.
- Every translated entry carries the current English source in a comment
  directly above it, e.g. `<!-- EN: Reset to defaults -->` — this is how stale
  translations are detected when the English changes.
- Placeholders (`{0}`, `{1}`, ...) must match the English exactly.
- This mod ships its own Defs, so translations cover both Keyed and
  DefInjected content. English DefInjected sources live under
  `1.6/Languages/English/DefInjected/<DefType>/`, and Keyed strings live in
  `1.6/Languages/English/Keyed/BTG.xml`, keyed with the `BTG_` prefix.
- Exception: entries for content gated on Unique Melee Weapons live under
  `1.6_UMW/Languages/<Language>/...` (a LoadFolders-gated root that only
  loads when that mod is active — MayRequire is ignored on DefInjected
  entries, so the folder is the gate). Translate them there, mirroring
  `1.6_UMW/Languages/English/`, never in the main `1.6` tree.
- Formatting: UTF-8 without BOM, LF line endings, 2-space indent.
- Validate before opening a PR:

  ```bash
  python3 Scripts/check-translations.py --strict
  ```

  It checks key coverage, placeholders, DefInjected paths, staleness, and
  file hygiene.

- Improving a machine-assisted language? Corrections from native speakers
  are gladly merged, no matter how small.
