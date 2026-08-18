# French — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07-29 pass. Family-shared mechanics
(LanguageWorker behavior, style/corpus rules, vanilla-grounded common
vocabulary) live in the `l10n/` submodule at `l10n/languages/French.md` —
this file holds only what is specific to Better Traders Guild.

## Def-to-vanilla-template cross-reference

Odyssey fr covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, all three letter strings) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` + `BTG_GameStartDialog_ExiledTraders` | Odyssey `TheGravship` ScenarioDef — the difficulty note, the gravlite sentence, the launch sentence and the `« voir la planète »` sentence are all verbatim |
| `BTG_CargoVaultHatch` / `_Sealed` / `Exit` | Odyssey `AncientHatch` / `AncientHatchExit` |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | chef | | Core `PlayerColony`, Odyssey `GravshipCrew`/`Salvagers` all use chef; TradersGuild's own is maître du commerce — relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |
| smuggler | contrebandier | | Core `Orbital_PirateMerchant.label` — grounds BTG's own "smuggler's den" compound below |

## Mod-coined terms (pending native review)

**cargo vault** coffre à cargaison (hatch variants trappe sécurisée du coffre à
cargaison / trappe scellée… / sortie du…), **shuttle bay** hangar à navettes
(`hangar` appears nowhere in vanilla fr, but is a French word to begin with),
**smuggler's den** repaire de contrebandiers (echoing Odyssey's `InsectLair` =
repaire d'insectes), **threat points** points de menace, **orbital steel /
rust** acier orbital / rouille, **silver (colour)** argent, **independent
traders** commerçants indépendants, **Exiled Traders** Commerçants exilés,
**cargo claim** droit sur la cargaison, **medbay** infirmerie, **docked
vessel** Navire amarré, **docking bays** quais d'amarrage, **(Vanilla)**
(d'origine), **entrenched defender AI** IA de défenseurs retranchés,
**resupply** ravitaillement, **a hold full of silver** une cale pleine
d'argent (`soute` appears nowhere in vanilla fr; `cale` is the ship's-hold
sense). "TG maps" is spelled out as **cartes de la guilde** — a French
initialism would be opaque. `BTG_Settings_ModName` is the localized Workshop title
`Guilde des commerçants améliorée` and must stay in sync with the title line of
`.steamworkshop/Description/French.txt` (see the CLAUDE.md localization note).

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

**`traitAdjectives` follow vanilla fr's own shape for `GoldInlay`: a bare noun
(`or`) plus masculine-singular adjectives (`doré`).** French cannot agree with
an unknown weapon noun's gender here and vanilla does not try — Odyssey's
`NamerUniqueWeapon` fr postposes the adjective under a hardcoded masculine
`Le [weapon_type] [weapon_adjective]`, so masculine forms are consistent with
vanilla's own choice rather than a hazard. BTG's five silver adjectives are
argent / argenté / étincelant / d'argent / raffiné.

## Other notes — the `de le` hazard in `BTG_CargoVaultHatch.hackedMessage`

BTG's only string injecting a `_definite` symbol is
`BTG_CargoVaultHatch`'s `hackedMessage`, whose English ("bypassed the
security **on** {SUBJECT_…Def}") would land a `de` right before it — the one
preposition the worker breaks. The sentence was rebuilt so the symbol is a
plain **direct object** (`… a contourné la sécurité et ouvert {SUBJECT_…Def}.`),
which is also what vanilla fr does in `AncientHatch` (`a terminé de pirater
{SUBJECT_labelNoParenthesisDef}.`). Restructuring beats fighting the worker —
the same conclusion the de and es sections reach from their own directions.
