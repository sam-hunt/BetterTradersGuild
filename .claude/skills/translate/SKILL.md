---
name: translate
description: Generate, update, or audit mod localization (Keyed and DefInjected) for a target language, grounded in vanilla + Odyssey RimWorld terminology for Better Traders Guild's Traders Guild / orbital-trading domain. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Better Traders Guild. English is
the source of truth; every other language derives from it.

**The family-wide process lives in the `l10n/` submodule — load these first,
and only these** (progressive disclosure; if `l10n/` is empty, run
`git submodule update --init`):

- `l10n/process.md` — non-negotiables, file/format conventions, terminology
  grounding method, and the generation / update / audit workflows. This is
  the workflow authority; follow it step by step.
- `l10n/languages/<Language>.md` — the target language's engine mechanics,
  style rules, and vanilla-grounded common vocabulary. Read ONLY the target
  language's file.
- `glossary/<Language>.md` (beside this file) — this mod's own coined-term
  table for the target language. Read it in the same pass.
- `l10n/lessons.md` — cross-language lessons; read when generating a new
  language, skim otherwise.
- `l10n/workshop.md` — Steam Workshop description/title conventions;
  `.steamworkshop/README.md` names this mod's anchor term and title-coupling
  key (`BTG_Settings_ModName`).

**Where learnings land:** mod-independent findings (engine mechanics, a
language's grammar rule, corpus style facts) go in the `l10n/` submodule —
edit the canonical checkout at `~/dev/rimworld-l10n`, commit there, then bump
the pin here. Mod-specific findings (coined terms, phrasing decisions) go in
`glossary/<Language>.md`.

## This mod's translation surface

- English Keyed source: `1.6/Languages/English/Keyed/BTG.xml` — a single
  file covering the mod settings window, float-menu and inspect strings, the
  cargo vault hatch, one-time fix dialogs, and scenario game-start prose.
  Every key is `BTG_`-prefixed. There is no second Keyed file.
- **This mod ships its own Defs, so the DefInjected surface is non-empty and
  growing** — def-type folders under `1.6/Languages/English/DefInjected/`
  currently include `ColorDef`, `FactionDef`, `JobDef`, `MapGeneratorDef`,
  `PawnKindDef`, `QuestScriptDef`, `ScenPartDef`, `ScenarioDef`, `ThingDef`,
  and `WeaponTraitDef`. Every language pass must cover DefInjected alongside
  Keyed — and remember (per `l10n/process.md`) the English DefInjected tree
  is a strict subset of the surface: enumerate from the
  `Scripts/expected-injections.json` sidecar.
- **Gated compat load roots:** the UMW-gated `BTG_SilverInlayMelee.*`
  entries (`WeaponTraitDef`) live under
  `1.6/Mods/UniqueMeleeWeapons/Languages/<Language>/...`, and the
  Biotech-gated `BTG_ConfigureStartingPawnsXenotypes.label` (`ScenPartDef`)
  under `1.6/Mods/Biotech/Languages/<Language>/...`. Route each gated def's
  translations to its own root, never the main `1.6` tree; the checker
  enforces this both ways.
- All def types this mod currently ships resolve bare (no namespace-prefixed
  DefInjected folders needed today).

## This mod's grounding domain

Domain DLC: **Odyssey** (plus Core). Ground against the Core + Odyssey tars;
Biotech and Royalty are other mods' domains (Biotech matters here only for
the one gated ScenPart above). Terms that MUST be grounded before use:
trader, orbital trader, settlement, faction, goodwill, caravan, shuttle,
cargo, hacking, and market-value vocabulary — the vanilla-grounded answers
live in `l10n/languages/<Language>.md`; this mod's coined terms (cargo
vault, smuggler's den, threat points, ...) live in `glossary/<Language>.md`.
All eight shipped languages have grounded tables; a ninth language starts
from nothing and gets its terms grounded and recorded per `l10n/process.md`.

## Workflows

Follow `l10n/process.md`'s Initial generation / Update pass / Audit-only
workflows verbatim. This mod's specifics on top:

- The checker: `python3 Scripts/check-translations.py` (`--strict` for new
  languages). Sidecar regen: `python3
  Scripts/refresh-translation-expectations.py` (game must be closed; drives
  the deployed L10nProbe).
- Compat-root routing per the surface section above; the checker's
  missing-entry errors name the root a translation belongs under.
- The public roster (and credits) is CONTRIBUTING.md's localization table —
  update it in the same commit as any language addition or native review.
