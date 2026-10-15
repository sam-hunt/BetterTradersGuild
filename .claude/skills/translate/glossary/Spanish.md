# Spanish (Castellano) — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07-29 pass. Family-shared mechanics
(LanguageWorker behavior, style/corpus rules, vanilla-grounded common
vocabulary) live in the `l10n/` submodule at `l10n/languages/Spanish.md` —
this file holds only what is specific to Better Traders Guild.

## Def-to-vanilla-template cross-reference

Odyssey es covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, both failure letters, the royal-favor letters) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` (difficulty note + the launch / view-planet sentences) | Odyssey `TheGravship` ScenarioDef |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |
| `BTG_CargoVaultHatch` (description shape, both `CompHackable` strings, the exit's ladder line) | Core `AncientHatch` / `AncientHatchExit` |

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | jefe | | Odyssey: TradersGuild=maestro comerciante, Salvagers=jefe — jefe is the neutral slot for a small crew, relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

**cargo vault** bóveda de carga (hatch variants escotilla segura de la bóveda de
carga / escotilla sellada… / salida de…), **shuttle bay** hangar de
transbordadores, **smuggler's den** guarida de contrabandistas (echoing Odyssey's
`InsectLair` = guarida de insectoides), **threat points** puntos de amenaza,
**orbital steel / rust** acero orbital / óxido, **silver (colour)** plateado,
**independent traders** comerciantes independientes, **Exiled Traders**
Comerciantes exiliados, **cargo claim** derecho de carga, **medbay** enfermería,
**docked vessel** Nave atracada, **docking bays** muelles de atraque,
**(Vanilla)** (Original), **entrenched defender AI** IA de defensores
atrincherados, **resupply** reabastecimiento. **salvager dropship** nave de desembarco de chatarreros (salvagers = Odyssey
`Salvagers.label` chatarreros; dropship = nave de desembarco, echoing Core's
`cápsula de desembarco`; the cooldown message uses the invariant frame
`{0} puede lanzarse de nuevo.` because the feminine noun would break the
GuildShuttle `está listo`), **stack limit** límite de pilas (`pila` = Core
`NoMergeTarget`), **wealth limit** límite de riqueza, **{0} silver** `{0} de
plata` (Core `{1} de plata`), **Unlimited** Ilimitado (Core `Unlimited`
verbatim). "TG maps" is spelled out as
**mapas del gremio** — a Spanish initialism would be opaque.
`BTG_Settings_ModName` is the localized Workshop title
`Gremio de comerciantes mejorado` and must stay in sync with the title line of
`.steamworkshop/Description/Spanish.txt` (see the CLAUDE.md localization note).

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

**`traitAdjectives` must be gender-invariant in Spanish, and this is a
mechanical constraint, not a style call.** Odyssey's `NamerUniqueWeapon`
rulePack **postposes** the adjective (`[weapon_type] [trait_adjective]`), and
`CompUniqueWeapon` feeds `weapon_type` from the *weapon's* `namerLabels` — an
unknown noun of unknown gender. Vanilla es dodges agreement entirely by
picking invariant forms for `GoldInlay` (`de oro`, `brillante`): a
prepositional phrase or an `-e`/`-ente` adjective. BTG's five silver
adjectives follow suit: de plata / de plata bruñida / brillante / reluciente /
noble. Never ship an `-o`/`-a` adjective here.

## Other notes — the `de el` hazard in `BTG_CargoVaultHatch.hackedMessage`

BTG's only string that injects a `_definite` symbol is
`BTG_CargoVaultHatch`'s `hackedMessage`, and the English ("bypassed the
security **on** {SUBJECT_…Def}") would land a `de` directly before it.
Rather than reach for the `{replace:}` scaffolding vanilla es uses elsewhere,
the sentence was rebuilt so the symbol is a plain **direct object**
("… ha burlado la seguridad y ha abierto {SUBJECT_…Def}."), which removes the
preposition entirely — the same move vanilla es makes in `AncientHatch`
("ha terminado de hackear {SUBJECT_labelNoParenthesisDef}."). Restructuring
beats `{replace:}` whenever the sentence allows it.
