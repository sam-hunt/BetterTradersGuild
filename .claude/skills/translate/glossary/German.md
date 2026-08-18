# German — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, on top of
PersonaWeaponsUnbound's 2026-07-28 pass extended across the weapon-mod
siblings. Family-shared mechanics (LanguageWorker behavior, style/corpus
rules, vanilla-grounded common vocabulary) live in
the `l10n/` submodule at `l10n/languages/German.md` — this file holds only
what is specific to Better Traders Guild.

## Def-to-vanilla-template cross-reference

Odyssey de covers nearly everything BTG builds on, and three vanilla defs
are near-exact templates — check them first before composing anything new:
Core's `TradeRequest` QuestScriptDef (its description frame, the
`qualityInfo` fragment and all three letter strings were byte-identical
reuse for `BTG_TradeRequest`), Odyssey's `TheGravship` ScenarioDef (the
difficulty note and the launch / view-planet sentences), and Odyssey's
`GoldInlay` WeaponTraitDef (`SilverInlay` / `BTG_SilverInlayMelee`).

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | Anführer | | Core `PlayerColony`/`GravshipCrew`; Odyssey: TradersGuild=Handelsmagnat, Salvagers=Boss — Anführer is the neutral slot, relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

**cargo vault** Frachttresor (hatch variants gesicherte Frachttresorluke /
versiegelte Frachttresorluke / Frachttresorausgang), **shuttle bay**
Fährenhangar, **smuggler's den** Schmugglernest (echoing Odyssey's Mechnest),
**threat points** Bedrohungspunkte, **orbital steel / rust** Orbitalstahl /
Rost, **independent traders** unabhängige Händler, **Exiled Traders**
Verbannte Händler, **cargo claim** Frachtanspruch, **medbay**
Krankenstation, **docked vessel** Angedocktes Schiff, **docking bays**
Andockbuchten, **(Vanilla)** (wie im Original), **entrenched defender AI**
Verschanzte Verteidiger-KI, **garrison** Garnison, **resupply** Nachschub.
"TG maps" is spelled out as **Gildenkarten** — a German initialism would be
opaque. `BTG_Settings_ModName` is the localized Workshop title
`Bessere Händlergilde` and must stay in sync with the title line of
`.steamworkshop/Description/German.txt` (see the CLAUDE.md localization note).

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

`traitAdjectives` are **bare attributive adjectives** in vanilla de (Odyssey
`GoldInlay`: golden, vergoldet) — no gender markers, because these defs feed
no `RulePackDef` name grammar here. BTG's five silver adjectives are silbern /
versilbert / glänzend / silberweiß / edel.

## Other notes — `BTG_CargoVaultHatch.hackedMessage`

**This bit is load-bearing, not theoretical:** `{SUBJECT_labelNoParenthesisDef}`
resolves through `WithDefiniteArticle`, which prepends a bare nominative
`der`/`die`/`das`, so an English source like *"… bypassed the security on
{SUBJECT_labelNoParenthesisDef}."* cannot be translated literally (`auf die
Luke` needs no case change but `von die Luke` is ungrammatical). Vanilla de
sidesteps it by dropping the symbol and writing the noun literally
(`… hat die antike Panzertür erfolgreich gehackt.`) — **which we cannot do,
since the checker enforces placeholder parity.** Rebuild the sentence so the
symbol lands in a nominative slot instead: BTG's `hackedMessage` is
`{HACKER_labelShort} hat das Sicherheitssystem umgangen – {SUBJECT_…Def} ist
offen.`
