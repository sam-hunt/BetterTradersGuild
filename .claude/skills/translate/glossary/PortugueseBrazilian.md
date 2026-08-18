# Brazilian Portuguese — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07-29 pass. Family-shared mechanics
(LanguageWorker behavior, style/corpus rules, vanilla-grounded common
vocabulary) live in
the `l10n/` submodule at `l10n/languages/PortugueseBrazilian.md` — this file
holds only what is specific to Better Traders Guild.

## Def-to-vanilla-template cross-reference

Odyssey pt-BR covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, all three letter strings) | Core `Script_TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` + `BTG_GameStartDialog_ExiledTraders` | Odyssey `TheGravship` ScenarioDef — the difficulty note, the gravlite sentence, the launch sentence and the `"exibir planeta"` sentence are all verbatim |
| `BTG_CargoVaultHatch` / `_Sealed` / `Exit` | Odyssey `AncientHatch` / `AncientHatchExit` |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | Líder | | Core/Odyssey `PlayerColony`/`GravshipCrew`; Odyssey: TradersGuild=Mestre Comercial, Salvagers=Chefe — Líder is the neutral slot, relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

**cargo vault** cofre de carga (hatch variants alçapão seguro do cofre de
carga / alçapão selado… / saída do…), **shuttle bay** hangar de ônibus
espaciais (`hangar` appears nowhere in vanilla pt-BR, but is a Portuguese word
to begin with), **smuggler's den** covil de contrabandistas (echoing Odyssey's
`InsectLair` = Covil de Insetos), **threat points** pontos de ameaça,
**orbital steel / rust** aço orbital / ferrugem, **silver (colour)**
prateado, **independent traders** Mercadores Independentes, **Exiled
Traders** Mercadores Exilados, **cargo claim** direito sobre a carga,
**medbay** enfermaria, **docked vessel** Nave atracada, **docking bays**
docas de atracação (built on Odyssey's own docking verb, "atracada"),
**(Vanilla)** (Original), **entrenched defender AI** IA de defensores
entrincheirados, **resupply** reabastecimento, **a hold full of silver** um
porão cheio de prata. "TG maps" is spelled out as **mapas da guilda** — a
Portuguese initialism would be opaque. `BTG_Settings_ModName` is the localized Workshop title
`Guilda dos Mercadores Aprimorada` and must stay in sync with the title line of
`.steamworkshop/Description/PortugueseBrazilian.txt` (see the CLAUDE.md localization note).

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

**`traitAdjectives` follow vanilla pt-BR's own shape for `GoldInlay`: a bare
noun (`ouro`) plus a masculine-singular adjective (`dourado`).** Odyssey's
`NamerUniqueWeapon` pt-BR **preposes** the adjective under a hardcoded
masculine `O [weapon_adjective] [weapon_type]`, so masculine forms are
consistent with vanilla's own choice rather than a hazard — the same
conclusion fr reaches from its own direction. BTG's five silver adjectives
are prata / prateado / reluzente / argênteo / refinado.

## Other notes

**Two Core pt-BR `reportString`s are wrong and were deliberately not
mirrored** (flagged in `JobDef/Jobs.xml` for native review): `TendPatient` is
`Cuidando de TargetA.` with a stray mid-string capital no other reportString
has (lowercased here), and `FeedPatient` is `levando TargetA para TargetB.`
— "carrying", which simply does not translate "feeding TargetA to TargetB"
(BTG ships `alimentando TargetB com TargetA.`). Frequency is not correctness
applies to vanilla's own data, not only to its contraction bugs.

**The contraction hazard in `BTG_CargoVaultHatch.hackedMessage`:** BTG's
only string injecting an article'd symbol is `BTG_CargoVaultHatch`'s
`hackedMessage`, whose English ("bypassed the security **on**
{SUBJECT_…Def}") would land a `de` right before it — and nothing in pt-BR
fuses it, so `de o cofre` would ship literally. The sentence was rebuilt so
the symbol is a plain **direct object** (`… burlou a segurança e abriu
{SUBJECT_labelNoParenthesisDef}.`), which is also what vanilla pt-BR does in
`AncientHatch` (`terminou de hackear {SUBJECT_labelNoParenthesisDef}.`).
Restructuring beats hedging — the same conclusion the de, es and fr sections
reach.
