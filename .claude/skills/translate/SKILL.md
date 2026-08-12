---
name: translate
description: Generate, update, or audit mod localization (Keyed and DefInjected) for a target language, grounded in vanilla + Odyssey RimWorld terminology for Better Traders Guild's Traders Guild / orbital-trading domain. Use when asked to add a language, update translations, or check translation freshness.
argument-hint: "[language, e.g. German | update | check]"
---

# Translate

Produce or refresh localization files for Better Traders Guild. English is
the source of truth; every other language derives from it.

## Non-negotiables

- **Run the checker first and last.** `python3 Scripts/check-translations.py`
  validates key sets, placeholders, DefInjected paths, staleness, and file
  hygiene deterministically. Never hand-derive anything it reports; never
  finish with it failing.
- **Community translations are owned by their contributors.** Update
  stale/missing keys in an existing language when asked, but do not rewrite a
  contributor's phrasing wholesale without the user's explicit direction.
- **Machine-assisted output is a first pass.** PRs and commits containing
  generated translations must say so and invite native-speaker review.
- **Keep the public roster current.** CONTRIBUTING.md's localization table
  (Planned / Machine-assisted / Native, plus credit) must be updated in the
  same commit whenever a language is added or a native review lands. The
  target roster lives there — consult it before proposing new languages.
  Today it lists English (Source) plus Simplified Chinese, Russian, Korean,
  German, Spanish, French, Brazilian Portuguese and Japanese — every one of
  them Machine-assisted, and nothing left Planned. A further language means
  adding a row, not filling one in.

## File map and conventions

- English Keyed source: `1.6/Languages/English/Keyed/BTG.xml` — a single
  file (currently ~109 lines) covering the mod settings window (trader
  rotation, custom layouts, cargo vault, sentry drones, defender AI,
  defender resupply, Salvagers raid weight), float-menu and inspect
  strings, the cargo vault hatch, one-time fix dialogs, and scenario
  game-start prose. Every key is `BTG_`-prefixed; about a third of the
  file (38 keys) is the `BTG_Settings*` block. There is no second Keyed
  file.
- **This mod ships its own Defs, so the DefInjected surface is non-empty.**
  `1.6/Languages/English/DefInjected/` already has ten def-type folders:
  `ColorDef`, `FactionDef`, `JobDef`, `MapGeneratorDef`, `PawnKindDef`,
  `QuestScriptDef`, `ScenPartDef`, `ScenarioDef`, `ThingDef` (currently just
  `CargoVault.xml`), and `WeaponTraitDef`. Most of these are labels and
  descriptions on defs BTG itself authors under `1.6/Defs/` (custom
  factions, jobs, map generators, pawn kinds, quest scripts, scen
  parts/scenarios, the cargo vault ThingDef, weapon traits) — this is real,
  growing translation surface, not a placeholder, and every language pass
  must cover it alongside Keyed.
- Target layout: `1.6/Languages/<Language>/Keyed/*.xml` and
  `1.6/Languages/<Language>/DefInjected/<DefTypeFolder>/*.xml`, mirroring
  the English tree folder-for-folder.
- Gated compat load roots are additional language roots: the UMW-gated
  `BTG_SilverInlayMelee.*` entries (`WeaponTraitDef`) live under
  `1.6/Mods/UniqueMeleeWeapons/Languages/<Language>/...`, and the
  Biotech-gated `BTG_ConfigureStartingPawnsXenotypes.label` (`ScenPartDef`)
  under `1.6/Mods/Biotech/Languages/<Language>/...` — folders
  LoadFolders.xml loads only when their mod/DLC is active, because
  MayRequire is ignored on DefInjected entries, so the gate must be the
  folder. Each gated def's translations mirror its own root, never the main
  `1.6` tree (that would be a startup error whenever the gate is inactive);
  the checker enforces the placement in both directions.
- `<DefTypeFolder>` must be the def's resolvable type name: bare for
  vanilla types, which is every folder BTG currently ships (`ColorDef`,
  `FactionDef`, `JobDef`, ... all resolve directly, no namespace prefix). A
  namespace-qualified folder (`BetterTradersGuild.<DefClass>`) would only
  be needed for a def whose *type* this mod itself defines in C# — none
  exist today, but the rule is decompile-verified and load-bearing, not
  organizational (see next bullet), so it's recorded here in advance rather
  than rediscovered later.
- **The type folder is load-bearing, not organizational** (decompile-verified,
  `Verse.LoadedLanguage`): RimWorld enumerates only the top-level directories
  under `DefInjected/` and resolves each directory *name* to the def type its
  files target. An `.xml` placed directly in `DefInjected/` is never loaded,
  and the checker likewise iterates only directories — a misplaced file fails
  silently on both sides, so never flatten the tree. *Inside* a type folder
  everything is free: file names are arbitrary and files are found
  recursively, so one bundled file per type vs one-def-per-file is pure
  preference (English mostly uses one bundled file per type, e.g.
  `Factions.xml`, `Jobs.xml`, except `ThingDef/CargoVault.xml`, named for
  the specific def). The loader even tolerates a pluralized folder name by
  retrying with the last character stripped — `ThingDefs` → `ThingDef` — but
  the checker does not; use exact type names.
- DefInjected keys are `DefName.field` paths — e.g. `TradersGuild.label` for
  a `FactionDef`, or `CargoVault.description` for the vault `ThingDef`.
  These keys must stay legal against the `Scripts/expected-injections.json`
  sidecar, a dump of every injection point the live game actually expects,
  regenerated by `Scripts/refresh-translation-expectations.py`.
  `Scripts/check-translations.py` enforces every language's DefInjected
  files against the sidecar's `required` subset per def type and fails on
  any cross-language drift — never hand-derive the expected key set by
  scanning `1.6/Defs/` yourself.
- **Some translatable fields can exist without ever appearing in this
  repo's own def XML** — this is the general lesson the sibling mods
  learned the hard way (inherited labels, comp-default strings, vanilla
  base-def fields reached only through a Harmony patch), and it applies
  here too: a `CompProperties_*` this mod adds in C# could in principle
  expose a translatable string without ever touching a def file. The
  authority for what actually needs translating is never a hand-maintained
  list, it's the `Scripts/expected-injections.json` sidecar (see above); new
  content of *any* shape forces a regen rather than a manifest edit.
- **The English DefInjected tree is NOT the translation surface — it is a
  strict subset of it.** The checker demands the sidecar's `required` set
  from every non-English language, but demands nothing of English (English
  is served by the def XML's own `<label>`/`<description>`, so its
  DefInjected files are validated-if-present reference material only). At
  the 2026-08-09 zh pass, 35 of the 70 required entries had **no** English
  DefInjected counterpart at all — whole def types (`SitePartDef`,
  `WorldObjectDef`), 11 of 12 `JobDef` reportStrings, the entire
  `BTG_SmugglersDen` quest, `FactionDef.leaderTitle` /
  `messageDefendersAttacking`, the `scenario.name` / `scenario.description`
  mirrors, and the `CompHackable` strings. **Enumerate from the sidecar, not
  from `1.6/Languages/English/`,** and take the English source text for
  those entries from the sidecar's `english` field (which is also what the
  checker compares `<!-- EN: -->` comments against, so sourcing EN comments
  from it programmatically makes drift impossible).
- **EN comment convention (required):** every translated entry carries the
  current English source directly above it:
  `<!-- EN: Reset to defaults -->` — this is how the checker detects
  staleness.
- Formatting: UTF-8 without BOM, LF endings, 2-space indent, final newline,
  root element `<LanguageData>`.
- Placeholders (`{0}`, `{1}`, named args) must match English exactly per key.
  Translator comments above placeholdered English keys explain what gets
  injected — e.g. a silver amount or a percentage — so the phrasing around
  them can be planned before translating.

## Terminology grounding (do not skip)

Every game term must match the official localization, not a plausible
translation. Sources, in order:

1. Vanilla language data:
   `"$RIMWORLD_PATH"/Data/<Expansion>/Languages/<Language> (<Native>).tar`
   (read entries with `tar -xOf`). Check **Core plus Odyssey** — Odyssey is
   this mod's required DLC and the expansion where every
   orbital-settlement, shuttle, cargo, and gravship term in vanilla's own
   localization lives. Neither Biotech nor Royalty matters here; those are
   other mods' domains.
2. This file's glossary below (lessons already learned — apply them).
3. If a term appears nowhere official, flag it in the PR for native review
   rather than inventing silently.

Terms that MUST be grounded before use: trader, orbital trader, settlement,
faction, goodwill, caravan, shuttle, cargo, hacking, and market-value
vocabulary ("Traders will pay more/less for it" and similar phrasing,
market value, silver). **All eight shipped languages — Simplified Chinese,
Russian, Korean, German, Spanish, French, Brazilian Portuguese and Japanese —
have been grounded in this repo** (2026-08-09 for zh, 2026-08-10 for the
other seven), so every table below carries real BTG trader/settlement
vocabulary and is safe to build on. **A ninth language starts from nothing**:
there is no inherited table to lean on, so ground its own trader/settlement
terms against the Core + Odyssey tars and record them here before relying on
them, exactly as each of the eight did.

**Dashes: no new usages.** A dash the English source does not have must not
appear in a translation. Reflow into that language's ordinary punctuation
(comma, colon, or a restructured sentence) instead. The bar for introducing one
is *both* an exact parallel in vanilla for that construction *and* a workaround
that would read less naturally — extremely occasionally that's satisfied;
consistent dash-per-paragraph prose never is. Rationale: heavy em-dash use is a
widely recognised tell-tale of LLM-generated text, it is rare in real
handwritten prose, and many players react disproportionately badly to it, so
the cost of a stray dash is far higher than the stylistic gain. Note this is
about *new* usage: mirroring a dash that vanilla itself puts in the same slot
is fine, and BTG's English currently has **zero** dashes in its entire
translation surface, so today the answer is always "reflow".

**The test is a density comparison, not taste** — measure the target
language's own vanilla rate and compare. At the 2026-08-10 audit BTG's
translations ran **3.4x–11.3x** vanilla's dash rate in every language that had
any, which is what "unnatural overuse" looks like numerically:

```python
# strip comments and tags, count dashes per 100k chars of vanilla values,
# then the same over 1.6/Languages/<lang>/ — flag anything above ~1x.
```

All 22 were reflowed; ru's two settings strings became the proper
`Чем меньше значение, тем больше…` correlative, which is better Russian than
the dash it replaced. Re-run the comparison after any generation pass.

**Count style over the whole tar, not just `Keyed/`.** The French pass found
two of this skill's own style rules inverted because they had been derived
from Core+DLC Keyed alone: French does use `—` (13 tree-wide, including the
one vanilla def BTG's scenario prose is modelled on), and it does use
guillemets for clicked UI commands (74 tree-wide, 62 of them in DefInjected).
DefInjected is where the *prose* lives — descriptions, letters, scenario
text — so a Keyed-only count systematically under-samples exactly the
register a mod's own descriptions are written in. Walk both, strip comments
first, and split the count per DLC when the two disagree (fr's curly
apostrophes are almost entirely legacy Core `BackstoryDef`s; Odyssey is
decisively ASCII).

### Glossary — shared across the mod family

The style rules, worker mechanics and cross-language lessons below were
learned across the weapon-mod siblings (`../UniqueMeleeWeapons`,
`../UniqueWeaponsUnbound`, `../PersonaWeaponsUnbound`) generating melee- and
gun-domain content, and this repo now joins that family. Everything about
*how a language's `LanguageWorker` behaves* — quoting conventions,
punctuation, formality, dash/ellipsis rules, Korean josa markers, German
case vs. gender, French elision, Spanish/Portuguese contraction — is
mechanical fact about RimWorld's translation engine, independent of whether
a mod is about weapons or xenogerms, and is reproduced below unchanged. What
does **not** carry over verbatim is the glossary *tables*: they were built
for melee-weapon vocabulary (weapon names, damage types, tool labels, trait
adjectives, quest vocabulary) this mod has no use for, since it ships no
RulePackDefs and generates no weapon or tool names. Each
table below keeps only the rows that are domain-independent (UI buttons,
quality tiers) or directly relevant to a trader mod (market-value/trader
phrasing); the dropped rows are still correct and native-reviewed — they
just live in the weapon-mod skills, which remain the source for that
vocabulary if a future feature ever needs it. Mirror a correction the other
direction too: if generating this mod's languages surfaces a fix to a truly
*shared* row (a button label, a punctuation rule), propagate it back into
the siblings.

#### Russian (grounded in this repo's 2026-08-10 generation pass, plus UWU PR #6 native review)

RimWorld's language folder is `Russian` (tar: `Russian (Русский).tar`).

**`LanguageWorker_Russian` overrides no `PostProcessed`** (decompile-verified) —
no elision, no contraction, no `'s` rewriting, and `WithDefiniteArticle` /
`WithIndefiniteArticle` fall through to the base (Core `Keyed/Grammar.xml`
sets `DefiniteArticle`/`IndefiniteArticle` empty and `DefiniteForm`/
`IndefiniteForm` to `{0}`), so `[X_definite]` is a pure passthrough and every
contraction lesson from de/es/fr/pt-BR is inapplicable. **Russian's difficulty
is case and numeral agreement, and the worker exposes a mechanism for each.**

- **`{N_numCase ? formOne : formSeveral : formMany}` — numeral agreement, and
  the one construct that *replaces* its placeholder.** `TotalNumCaseCount` is
  3, and `LanguageWorker.ResolveNumCase` returns `number + " " + form`, i.e.
  it **prints the number itself**. So Core ru renders an English `"{0} days"`
  as `{0_numCase ? день : дня : дней}` with **no separate `{0}`** — writing
  both would print the number twice. `GetFormForNumber` picks formOne for
  n%10==1, formSeveral for n%10 in 2–4, formMany otherwise, with the teens
  (n/10%10==1) forced to formMany. Use it wherever a slider or count is
  followed by a noun. Two caveats: it reaches the *string* branch too, where
  a non-integer yields `number + " " + formSeveral` — so an already-formatted
  decimal like `"3.2"` is better written plainly as `{2} дня` (decimals always
  take genitive singular in Russian anyway), which also sidesteps
  `float.TryParse` returning `""` on a culture mismatch. And a bare Latin unit
  never needs it: vanilla writes `{0} ч`, `{0} с`, `{0} Вт`.
  `Scripts/check-translations.py` understands this construct (see
  `NUM_CASE_RE`) and still fails a translation that drops the argument.
- **`{lookup: {N}; Case; I}` — case declension, and it works in plain Keyed
  strings.** `GrammarResolverSimple` parses a `{name: args}` span as a
  *function* call and hands it to `LanguageWorker.ResolveFunction`, which
  implements `lookup` and `replace` (decompile-verified — this contradicts
  the note in the German section below, which is wrong for 1.6). Russian
  overrides `TryLookUp` to read `WordInfo/Case.txt`, whose rows are
  `ном; род; дат; вин; твор; предл`, so **index 3 is accusative** and 1 is
  genitive. Core ships ~1575 rows and Odyssey 12 more. Vanilla uses it in
  DefInjected as well as Keyed: every `messageDefendersAttacking` is
  `{0} из фракции {1} атакуют ваших {lookup: {2}; Case; 3}.` and every
  SitePartDef approach string is `Напасть на {lookup: {0}; Case; 3}`.
  **A miss degrades gracefully** — `TryLookUp` returns the (lowercased) key
  unchanged — so a mod-coined label just stays nominative rather than erroring.
  Two limits worth knowing: the checker's placeholder comparison only sees a
  *nested* `{N}` (`{lookup: {2}; Case; 3}` is fine, `{lookup: [some_symbol];
  Case; 1}` reads as one spurious placeholder and fails), and a mod-coined
  label is never in `Case.txt` anyway — so when the argument is a `[symbol]`,
  restructure the sentence instead of reaching for `lookup`.

Style rules from the vanilla ru data (mandatory):

- **Guillemets `«…»`** for cited names and UI commands — vanilla writes
  `Задание провалено: «[resolvedQuestName]»` and `выберите «Просмотр планеты»`.
  Never `"` or `„…"`.
- **Em dash `—` is the most common of any language here (27.4 per 100k), but
  that still does not license introducing one** — see the no-new-dashes rule
  above; BTG's Russian ran 3.4x vanilla before its 9 were reflowed. Russian
  is the language where the temptation is strongest, because the dash is
  genuinely mandatory in a nominal sentence with an omitted copula and in
  verb gapping (`а сделка — повыгоднее`). Both have clean rewrites that are
  *better* Russian, not merely dash-free: use the `Чем …, тем …` correlative
  for an "X = Y" equivalence, and repeat the elided verb (`а сделка стала
  повыгоднее`) instead of gapping. Ellipsis is ASCII `...`. Descriptions end
  `.`; labels, buttons and stat fragments take none, and labels are lowercase
  noun phrases.
- **`ё` is written**, not folded to `е` (паёк, всё, ещё, налёт).
- **`reportString`s take no trailing period** and are 3rd-person present
  verbs — `убирает TargetA`, not a noun phrase and not `убирает TargetA.`
  (this refines the UWU PR #6 row below, which was about *inspect* strings).
- Units attach with a space and stay Cyrillic where vanilla has one:
  `{0} ч`, `{0} Вт`. Some vanilla ru files carry a BOM; ours never do.
- Formality is `вы`/`ваш` throughout.

| English | Use | Never | Why |
|---|---|---|---|
| Cancel (button) | Отменить | Отмена | vanilla `Cancel`; buttons use infinitive verbs |
| inspect strings | noun phrases | finite verbs | matches inspect-pane convention (but see reportStrings above) |
| Reset to defaults / Default / None / Reset | Восстановить по умолчанию / По умолчанию / Нет / Сбросить | | Core `RestoreToDefaultSettings`, `Default`, `None`, `Reset` |
| quality tiers | ужасно/плохо/нормально/хорошо/отлично/шедевр/легенда | | Core `QualityCategory_*` |
| traders guild | гильдия торговцев | торговая гильдия | Odyssey `TradersGuild.label` |
| salvagers | сталкеры | мародёры | Odyssey `Salvagers.label` (its *pawns* are пираты) |
| trader / orbital trader | торговец / орбитальный торговец | | Core; Odyssey orbital TraderKinds are **plural** (оптовые торговцы, торговцы экзотикой) |
| Traders will pay more/less for it. | Торговцы заплатят за него больше. / Торговцы дадут за него меньше. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | золотая/серебряная инкрустация | | Odyssey `GoldInlay.label` |
| leader (`leaderTitle`) | глава | | Odyssey: TradersGuild=магистр торговли, Salvagers=главарь; глава is the neutral slot |
| {0} from {1} are attacking your {2}. | {0} из фракции {1} атакуют ваших {lookup: {2}; Case; 3}. | | every Odyssey `FactionDef` — verbatim |
| Attack {0} / Attacking {0}. | Напасть на {lookup: {0}; Case; 3} / Нападает на {lookup: {0}; Case; 3} | | Core SitePartDef approach strings — verbatim, no trailing period |
| goodwill / caravan | репутация / караван | доброжелательность | Core `GoodwillTip` |
| shuttle | челнок | шаттл | Core `Shuttle.label` |
| gravship / gravlite panel / pilot console | гравикорабль / панель из гравлита / пульт пилота | | Odyssey |
| mechhive / orbital relay | мехрой / орбитальный ретранслятор | | Odyssey `TheGravship.description` |
| signal jammer / sentry drone / life support unit | глушитель сигналов / дрон-дозорный / блок жизнеобеспечения | | Odyssey |
| orbital platform / settlement platform | орбитальная платформа / платформа поселения | | Odyssey `OrbitalPlatform.label`, `SettlementPlatform.label` |
| orbital settlement / settlement | орбитальное поселение / поселение фракции | | Odyssey `SpaceSettlement.label`, Core `Settlement.label` |
| drop/transport pod | транспортная капсула | | Core `DropPodIncoming` |
| silver / market value / comms console / packaged survival meal | серебро / рыночная стоимость / консоль связи / сухой паёк | | Core |
| "of normal+ quality" / "(worth [X])" | качеством от нормального и выше / (стоимостью [X]) | | Core `TradeRequest` — verbatim |
| Quest failed: [resolvedQuestName] | Задание провалено: «[resolvedQuestName]» | | Core `TradeRequest` — verbatim |
| [faction_name] became hostile to you. | Фракция [faction_name] теперь враждебна к вам. | | Core `TradeRequest` — verbatim |
| "Note: This is a difficult scenario…" | Примечание: это сложный сценарий, не рекомендуется новичкам. | | Odyssey `TheGravship` — verbatim |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | Чтобы запустить гравикорабль, выберите пульт пилота, а затем команду запуска. / выберите «Просмотр планеты» | | Odyssey `TheGravship` GameStartDialog — verbatim |
| starting people (ScenPart) | людей в начале | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/tend/feed/hack/open) | убирает TargetA / спасает TargetA / лечит TargetA / скармливает TargetA TargetB / взламывает TargetA / открывает TargetA | | Core `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1. Odyssey's `Open`/`EnterTransporter` wrap TargetA in `{lookup: {TargetA}; Case; 3}` — **don't copy that**, the bare English `TargetA` has no braces so the checker reads the wrapper as an invented placeholder |

Mod-decided (no vanilla source — the rows most in need of native review):
**cargo vault** грузовое хранилище (hatch variants защищённый люк грузового
хранилища / запечатанный люк… / выход из…), **shuttle bay** ангар челноков,
**smuggler's den** логово контрабандистов, **threat points** очки угрозы,
**orbital steel / rust** орбитальная сталь / ржавый, **independent traders**
независимые торговцы, **Exiled Traders** Торговцы-изгнанники, **cargo claim**
право на груз, **medbay** медотсек, **docked vessel** пришвартованное судно,
**(Vanilla)** (как в оригинале). `BTG_Settings_ModName` is the localized Workshop title
`Улучшенная гильдия торговцев` and must stay in sync with the title line of
`.steamworkshop/Description/Russian.txt` (see the CLAUDE.md localization note).

`traitAdjectives` are **masculine nominative singular adjectives** in vanilla
ru (Odyssey `GoldInlay`: золотой, золоченый) — Russian cannot agree with an
unknown weapon noun's gender through `GrammarResolverSimple`, and vanilla
simply does not try. The dropped weapon-domain rows (`trait`, gun `charge`)
and the mod-decided WeaponCategoryDef labels live in `../UniqueMeleeWeapons`'s
skill if that vocabulary is ever needed here.

#### Japanese (grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07 pass)

RimWorld's language folder is `Japanese` (tar: `Japanese (日本語).tar`).

**There is no `LanguageWorker_Japanese`, and that absence is the finding that
shapes the pass** (verified 2026-08-10 against the 1.6 assembly's full typedef
list: workers ship for Catalan, Czech, Danish, Default, Dutch, English, French,
German, Hungarian, Italian, Korean, Norwegian, Portuguese, Romanian, Russian,
Spanish, Swedish and Turkish — Japanese is not among them). `LanguageInfo.xml`
declares no `languageWorkerClass` either, so the base `LanguageWorker` runs and
its `PostProcessed` only calls `MergeMultipleSpaces()`. No elision, no
contraction, no `'s` rewriting, no particles. **Nothing rewrites these strings,
so what is authored is what ships** — and equally, nothing will rescue a
malformed one. Japanese also needs no gender, number or case agreement, so
every hazard the de/es/fr/pt-BR sections are organized around simply does not
arise: BTG's `hackedMessage` keeps the English sentence shape with the injected
symbol in place, where all four of those languages had to restructure it. Read
this as pt-BR's lesson inverted — an absent worker there *created* the author's
problem, here it removes it.

Style rules from the vanilla ja data (mandatory). Counted 2026-08-10 over
Core+Odyssey, Keyed **and** DefInjected, comments stripped — 887k chars of
translated values:

- **ASCII `.` and `,`, never `。` or `、`** — and this is now verified
  tree-wide, not just in Keyed: **zero** 。 and **zero** 、 in all 887k chars,
  against 13,747 ASCII periods and 14,771 ASCII commas. Also zero full-width
  spaces `　` and (bar 2 stray Keyed instances) zero full-width parens — ASCII
  `(` `)` throughout, 603 of them.
- **Corner brackets 「」 and ASCII `"` are different slots, and the nearer
  analog decides.** 171 「」 ship tree-wide, but they mark quoted *text* —
  note contents, map inscriptions (`次の言葉が書かれている ——「[mapText]」`).
  For a **UI command the player clicks**, Odyssey ja's own `TheGravship`
  GameStartDialog writes ASCII double quotes: `ワールドマップで"惑星を見る"を選択し`.
  BTG's scenario dialogs are that exact slot, so they use `"…"`. This
  **corrects** the sibling-inherited rule that said 「」 for cross-referenced
  UI labels, and it is the opposite of zh, which reaches for 「」 in precisely
  this slot — do not generalize between the two CJK languages.
- **Dashes: 56 em dashes in 887k chars (6.3 per 100k), 22 of them doubled
  ——, and zero en dashes.** Most are one repeated `RulePackDef` idiom
  (treasure-map notes: `次の言葉が書かれている ——「[mapText]」`), so the real
  diversity is far below the raw count. Per the no-new-dashes rule, BTG's
  Japanese ships **zero**: the two ` - ` hyphen breaks in
  `BTG_GameStartDialog_IndependentTraders` were reflowed into ordinary clauses.
  Ellipsis is ASCII `...` (86) over `…` (14).
- **Units attach tight, with no per-unit split** — `{0}W` 5/0, `{0}%` 1/0,
  `{0}x` 11/1, `{0}日` 31/2, `{0}時間` 4, `{0}個` 9. This is simpler than
  fr/es/pt-BR, which each split percentages from watts; in ja one rule covers
  everything. Vanilla has no `{0}h` string at all, so the English form carries
  over unchanged.
- **Colons are per-slot, not per-rule.** ASCII `:` dominates (442 vs 111
  full-width), and `クエスト失敗: [resolvedQuestName]` uses it — but Odyssey's
  difficulty note is `注意：このシナリオは…` with a full-width one, and
  `内容物：重力コア` likewise. Copy the slot, don't apply a rule.
- Descriptions and tooltips take polite です/ます and end `.`; labels, buttons,
  section headers and `ScenPartDef` labels take none. DLC names stay in Latin
  script (Biotech, Royalty, Odyssey), as does MOD.
- **`reportString`s carry NO trailing period** where the English has one, and
  take the progressive 〜中 / 〜している form. This matches ru and ko, and is the
  opposite of de/es/fr/pt-BR, which all keep it.

**Odyssey ja covers nearly everything BTG builds on, and four vanilla defs are
near-exact templates** — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, both failure-letter strings) | Core `TradeRequest` QuestScriptDef |
| `BTG_ExiledTraders` + `BTG_GameStartDialog_ExiledTraders` | Odyssey `TheGravship` ScenarioDef — the difficulty note, the gravlite sentence, the launch sentence, the `"惑星を見る"` sentence and the closing mechhive sentence are all verbatim |
| `BTG_CargoVaultHatch` (both `CompHackable` strings) | Odyssey `AncientHatch` |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Confirm | キャンセル / リセット / 了承 | | Core buttons |
| Reset to defaults / Default / None | デフォルトに戻す / デフォルト / なし | | Core `RestoreToDefaultSettings`, `Default`, `None` |
| quality tiers | 壊れかけ/低品質/標準品/良品/秀品/名品/幻の一品 | | Core `QualityCategory_*` |
| "of normal+ quality" / "(worth [X])" | 標準品以上の品質の / (価値 [X]) | | Core `TradeRequest` — verbatim; note ja places the quality phrase BEFORE the item, where a Japanese attributive belongs |
| traders guild / guild member(s) | 商人ギルド / ギルドメンバー | 貿易ギルド | Odyssey `TradersGuild.*` (pawnSingular and pawnsPlural are identical — ja does not inflect for number) |
| salvagers | 略奪者 | 回収業者 | Odyssey `Salvagers.label` (its *pawns* are 宙族) |
| leader (`leaderTitle`) | リーダー | | Core/Odyssey `GravshipCrew`, `Ancients`, `Insect` all use リーダー; Odyssey: TradersGuild=交易修士, Salvagers=ボス |
| trader / merchant | 商人 / 貿易商 | | Odyssey `TradersGuild.description` uses 商人 for the guild's people, `GoldInlay.description` 貿易商 for the trade role |
| orbital trader | 軌道上の商人 | | Odyssey `TradersGuild.description` |
| Traders will pay more/less for it. | 貿易商は高値でこれを買い取ります. / 貿易商は低い価格でこれを買い取ります. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | 金の象眼 / 銀の象眼 | | Odyssey `GoldInlay.label` is a **noun phrase** (like es/pt-BR, unlike de/fr's participle) |
| {0} from {1} are attacking your {2}. | {1}の{0}は {2}を攻撃中です. | | every Odyssey `FactionDef` — verbatim, its internal double space included |
| Attack {0} / Attacking {0}. | {0}を攻撃 / {0}を攻撃中 | | Odyssey `Outpost` approach strings — verbatim; ja drops the English's trailing period on both |
| Quest failed: [resolvedQuestName] | クエスト失敗: [resolvedQuestName] | | Core `TradeRequest` — verbatim (quest = クエスト) |
| [faction_name] became hostile to you. | [faction_name]があなたのコロニーと敵対状態になりました. | | Core `TradeRequest` — verbatim |
| hostile to {0} | {0}と敵対関係 | | shaped from Core `QuestHostileTo` (`{0}と敵対`) |
| No capable negotiator | まともな交渉人がいません | | shaped from Core `CommandTradeFailNoNegotiator` |
| requires signal jammer | シグナルジャマーが必要 | | Odyssey `TransportPodDestinationRequiresSignalJammer` — verbatim |
| orbital platform / settlement platform | 軌道プラットフォーム / 入植用プラットフォーム | | Odyssey `OrbitalPlatform.label`, `SettlementPlatform.label` (a MapGeneratorDef, the same slot BTG's is) |
| orbital settlement / settlement | 軌道上の入植地 / 入植地 | | Odyssey `SpaceSettlement.label` |
| shuttle | シャトル | 宇宙往還機 | Odyssey `Shuttles.label` (passenger shuttle = 旅客シャトル) |
| drop pod vs transport pod vs cargo pod | ドロップポッド vs 輸送ポッド vs 貨物ポッド | | Core `DropPodIncoming`, `TransportPod`, `CargoPodCrash` — three distinct terms, don't merge |
| signal jammer / sentry drone / life support unit | シグナルジャマー / セントリードローン / 生命維持ユニット | | Odyssey |
| gravship / gravlite panel / pilot console | グラヴシップ / 重力軽量パネル / パイロットコンソール | | Odyssey (gravcore = 重力コア) |
| mechhive / orbital relay | メカハイブ / 軌道リレー | | Odyssey `Mechhive.label`, `OrbitalRelay.label` |
| goodwill / caravan / negotiator | 友好値 / キャラバン隊 / 交渉人 | 好感度 | Core `Goodwill`, `Caravan.label`, `Negotiator` |
| silver / steel / market value / comms console / packaged survival meal / vacuum | シルバー / スチール / 標準小売価格 / 通信機 / 非常用食品 / 真空 | | Core |
| garrison / outpost / safe / hatch / medbay / ship's hold | 駐屯地 / 前哨基地 / 金庫 / ハッチ / 医務室 / 船倉 | | Core `AncientGarrison`, Odyssey `Outpost`/`AncientSafe`/`AncientHatch`; 医務室 and 船倉 are vanilla ja words found elsewhere in the tree |
| colour labels | Weapon-family ColorDefs are **bare nouns** (金, 灰, 緑); Structure-family ones are 〜色 compounds (赤褐色, 淡い青色) or katakana | | Core `Structure_*` vs Odyssey `UniqueWeapon_*` — match the def's `colorType`, not a blanket rule |
| "Note: This is a difficult scenario…" | 注意：このシナリオは難易度が高く,始めたばかりのプレイヤーにはお勧めできません. | | Odyssey `TheGravship` — verbatim, full-width colon included |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | グラヴシップを打ち上げるには,パイロットコンソールを選択し,発射コマンドを選択してください. / ワールドマップで"惑星を見る"を選択し | | Odyssey `TheGravship` GameStartDialog — verbatim, ASCII double quotes included |
| starting people (ScenPart) | 開始時の人数 | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/tend/feed/hack/open/board) | TargetAを掃除中 / TargetAを救助中 / TargetAの看病中 / TargetAをTargetBに給仕中 / TargetAをハッキングしている / TargetAを開封中 / TargetAに乗り込んでいる | | Core+Odyssey `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1. **No trailing period**, and TargetA/TargetB stay bare |

**One Core ja `TradeRequest` string is wrong and was deliberately not
mirrored** (flagged in `QuestScriptDef/QuestScripts.xml` for native review):
`LetterTextFavorReceiver` reads `誰が[X]を持っていると信じるべきですか?` — "who
should we believe *holds* [X]?" — inverting the English, where the player picks
who *receives* the favor. BTG ships
`この取引の要求を満たした功績として,誰に[asker_faction_royalFavorLabel]を与えますか?`
instead. Same lesson pt-BR's section records from its own direction: frequency
is not correctness, and it applies to vanilla's own data.

Mod-decided (no vanilla source — the rows most in need of native review):
**cargo vault** 貨物保管庫 (hatch variants 貨物保管庫の保安ハッチ / 封鎖された貨物
保管庫のハッチ / 貨物保管庫の出口), **shuttle bay** シャトル格納庫, **smuggler's
den** 密輸業者の巣窟 (both 密輸業者 and 巣窟 are vanilla ja words, from Core
BackstoryDefs and the settlement namer `[townname_wordgen]の巣窟`), **threat
points** 脅威ポイント, **orbital steel / rust** 軌道スチール色 / 錆色, **silver
(colour)** 銀, **independent traders** 独立商人, **Exiled Traders**
追放された商人, **cargo claim** 貨物引換権, **docked vessel** ドッキング中の船,
**docking bays** ドッキングベイ, **orbital ring** 軌道リング, **(Vanilla)**
(バニラ) — the JP modding community's standard word for unmodded RimWorld,
**entrenched defender AI** 籠城型の防衛AI, **resupply** 補給, **fencing stolen
cargo** 積荷の横流し (vanilla ja's own phrase, Core BackstoryDef). "TG maps" is
spelled out as **ギルドのマップ** — a Japanese initialism would be opaque.
`BTG_Settings_ModName` is the localized Workshop title
`商人ギルド拡張MOD` and must stay in sync with the title line of
`.steamworkshop/Description/Japanese.txt` (see the CLAUDE.md localization note).

**`traitAdjectives` must be ATTRIBUTIVE forms that read as a prefix on an
unknown weapon noun** — vanilla ja's `GoldInlay` uses 金の / 黄金の, i.e.
の-terminated noun modifiers. な-terminated adjectival nouns and plain
attributive verbs (輝く) work identically. Japanese needs no agreement of any
kind, so unlike de/es/fr/pt-BR the weapon noun's identity never constrains the
choice. BTG's five silver adjectives are 銀の / 銀めっきの / 輝く / 白銀の / 精巧な.

The rest of the weapon-mod Japanese glossary — weapon/tool/damage vocabulary,
the `[stuff_adjective]の[noun]` name-grammar composition, and battle-log
grammar — is specific to `RulePackDef` name generation and melee combat text,
which this mod has none of. See `../UniqueMeleeWeapons` if that ever changes.

#### Simplified Chinese (grounded in this repo's 2026-08-09 generation pass)

RimWorld's language folder is `ChineseSimplified` (tar: `ChineseSimplified
(简体中文).tar`) — the mod's folder must match it exactly, whatever the
public roster calls the language.

Style rules discovered from the vanilla zh data (mandatory):

- Full-width punctuation in prose (，。、；：（）……); descriptions end with 。;
  labels and buttons carry no trailing period. Placeholders, digits and units
  stay ASCII. Vanilla labels use full-width parens: 锻造台（燃料）.
- Quote cited names in prose with full-width curly quotes — vanilla writes
  任务"{0}". Terse stat templates take no quotes ({0}伤害). **But UI commands
  the player must click take corner brackets** 「」: Odyssey's own game-start
  dialog writes 请选择飞船控制台，然后点击「发射」指令 and 使用「查看星球」.
  BTG's scenario dialogs are the same shape, so they follow the same rule —
  pick the nearer analog over the general one.
- Should an English em dash `—` ever need carrying over, it becomes a
  **double** em dash ——, never a single one (vanilla: 这并不是古老的人类科技——
  而是一个机械族信标). But per the no-new-dashes rule above, don't introduce
  one where English has none: `，` carries the same break, and BTG's zh ran
  6.5x vanilla's rate before its 4 were reflowed to `，`.
- Units attach with no space (`{0}天`, `{0}小时`), and a bare Latin unit
  suffix stays ASCII (`{0}W`, `{0}x`).
- Vanilla zh files can contain untranslated English values — vanilla
  incompleteness is not style guidance. Some vanilla zh files carry a BOM;
  ours never do.
- `LanguageWorker_ChineseSimplified` imposes no authoring requirements (no
  particles, no elision, no contraction) — zh's difficulty is terminology,
  not mechanics.

**The single biggest lever for this mod is that Odyssey already ships zh for
almost everything BTG builds on.** Four vanilla defs are near-exact templates
and were reused wholesale in the 2026-08-09 pass — check them first before
composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (quest name/description rules, both failure letters, the royal-favor letters) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` (scenario description + game-start dialog) | Odyssey `TheGravship` ScenarioDef |
| `SilverInlay` / `BTG_SilverInlayMelee` | Odyssey `GoldInlay` WeaponTraitDef |
| `BTG_SmugglersDen` SitePartDef description ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

| English | Use | Never | Why |
|---|---|---|---|
| quality tiers | 极差/较差/一般/良好/极佳/大师级/传奇级 | | Core `QualityCategory_*` |
| "of normal+ quality" | 一般品质以上的 | | Core `TradeRequest` quest rules |
| traders guild | 商会 | 贸易公会 | Odyssey `TradersGuild.label` |
| salvagers | 打捞者 | 拾荒者 | Odyssey `Salvagers.label` (its *pawns* are 海盗) |
| trader / merchant | 商人 | | Odyssey `GoldInlay.description` |
| orbital trader | 轨道贸易商 | | Core `CommsConsole.description` |
| Traders will pay more for it. | 商人会支付更高的价格。 | | Odyssey `GoldInlay` — verbatim; 压价收购 is the "pay less" counterpart (`Ugly`) |
| leader (`leaderTitle`) | 领袖 | | Odyssey `GravshipCrew.leaderTitle` |
| {0} from {1} are attacking your {2}. | 来自{1}的{0}正在攻击你的{2}。 | | every Odyssey `FactionDef` — verbatim |
| shuttle | 穿梭机 | | Odyssey |
| gravship / gravlite panel / pilot console | 逆重飞船 / 逆重板 / 飞船控制台 | | Odyssey (`PilotConsole.label`; the Keyed UI's 驾驶台 is a different slot) |
| drop/transport pod vs cargo pod | 运输舱 vs 货舱 | | Core `DropPodIncoming` / `CargoPodCrash` — distinct, don't merge |
| orbital platform | 轨道设施 | 轨道平台 | Odyssey `OrbitalPlatform.label` — so "settlement platform" → 定居点设施 |
| space settlement / settlement | 轨道定居点 / 派系定居点 | | Odyssey `SpaceSettlement.label`, Core `Settlement.label` |
| signal jammer | 信号干扰器 | | Odyssey |
| sentry drone | 哨兵无人机 | 哨戒无人机 | Odyssey `Drone_Sentry.label` |
| life support unit | 生命维持单元 | | Odyssey `LifeSupportUnit.label` |
| mechhive / orbital relay | 机械主巢 / 轨道中继站 | | Odyssey `TheGravship.description` (the namer's 机械巢 is a different slot) |
| goodwill / caravan / negotiator | 好感度 / 远行队 / 谈判者 | | Core |
| market value / silver / packaged survival meal / comms console | 市场价值 / 白银 / 包装生存食物 / 通讯台 | | Core |
| Reset to default(s) / Default / None | 重置为默认值 / 默认 / 无 | | Core `ResetBinding`, `Default`, `None` |
| Quest failed: [resolvedQuestName] | 任务失败：[resolvedQuestName] | | Core `TradeRequest` — verbatim |
| [faction_name] became hostile to you. | [faction_name]开始与你敌对了。 | | Core `TradeRequest` — verbatim |
| Attack {0} / Attacking {0}. | 进攻{0} / 正在进攻{0}。 | | Core site approach strings — verbatim |
| "Note: This is a difficult scenario…" | 注意：这是个高难度的剧本，不建议新玩家尝试。 | | Odyssey `TheGravship` — verbatim |
| reportStrings (clean/rescue/tend/feed/hack/open) | 清理TargetA。/ 救援TargetA。/ 治疗TargetA。/ 将TargetA喂给TargetB吃。/ 骇入TargetA。/ 打开TargetA。 | | Core `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1 |

Mod-decided (no vanilla source — the rows most in need of native review):
**cargo vault** 货物保险库 (hatch variants 货物保险库安全舱门 / 封闭舱门 / 出口),
**shuttle bay** 穿梭机库, **smuggler's den** 走私巢穴, **threat points**
威胁点数, **orbital steel / rust** 轨道钢 / 锈色, **independent traders**
独立商人, **Exiled Traders** 流放商人, **cargo claim** 货物提取权.
`BTG_Settings_ModName` is the localized Workshop title
`强化商会` and must stay in sync with the title line of
`.steamworkshop/Description/ChineseSimplified.txt` (see the CLAUDE.md localization note) (it is also injected into the Empire-fix dialog as a
colorized mod name).

`traitAdjectives` must be **bare attributive words** that read as a prefix on
a weapon noun (银/白银/闪耀/银白/精良 → 银白长剑), never a 的-terminated
phrase. The rest of the weapon-mod Simplified Chinese glossary —
weapon/tool/damage vocabulary and the name-grammar composition rules (的/之
linking, material compounding) — is specific to `RulePackDef` name
generation and melee combat text, which this mod has none of. See
`../UniqueMeleeWeapons` if that ever changes.

#### Korean (grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07 pass)

Language folder is `Korean` (tar: `Korean (한국어).tar`). Decompile-verified
why the paren-stripped name works: `LoadedLanguage` derives
`legacyFolderName` by cutting at `(`, and mod language dirs match on
*either* `folderName` or `legacyFolderName` — the same mechanism behind
`Japanese`.

**Josa (particle) markers are the one hard mechanical rule Korean adds, and
nothing else in this skill has an equivalent — and it applies to any Keyed
string, not just combat/rulepack text.** Korean particles are allomorphic:
the correct form depends on whether the previous syllable ends in a
consonant, which is unknowable when the preceding text is an injected value
(a silver amount, a def label, anything from `{0}`).
`Verse.LanguageWorker_Korean.ReplaceJosa` (decompile-verified) resolves
exactly eight tokens, and no others:

```
(이)가   (와)과   (을)를   (은)는   (아)야   (이)어   (으)로   (이)
```

- Every *allomorphic* particle following `{0}`, `[symbol]` or `[TOKEN_x]` MUST use
  a marker. `{0}(을)를 생성` is correct; `{0}를 생성` breaks on consonant-final
  labels. Only five distinctions inflect (은/는, 이/가, 을/를, 와/과, 으로/로);
  **`에`, `에서` and `의` are invariant** — write those bare after a placeholder.
- Never hand-roll `{0}을(를)` — the worker does not recognize it.
- **Spelling is exact, and `(와)과` is asymmetric.** For every token the paren
  holds the post-*consonant* form — except `(와)과`, where `JosaPatternPaired`
  maps to `("과","와")`, so the paren holds the post-*vowel* form.
- **A marker resolving off a digit is always wrong.** `HasJong()` falls back to
  `AlphabetEndPattern` = `{b,c,k,l,m,n,p,q,t}` for non-Korean chars, which has no
  digits, so a number always yields the vowel form — right for 2/4/5/9
  (이·사·오·구), wrong for 1(일) 3(삼) 6(육) 7(칠) 8(팔) 0(영). Phrase around it,
  never mark it — this matters directly for a settings window with numeric
  sliders (silver amounts, percentages).
- **Quoting interacts with resolution.** `FindLastChar` skips a preceding `"`,
  `'` or `)` to reach the real final character, so `"{0}"(을)를` resolves
  correctly. Curly `" "` and corner `「 」` are **not** skipped, so the token
  is returned unresolved and the raw `(은)는` shows on screen. Korean
  therefore needs no defensive quoting at all — josa does the job quoting
  does in ja/ru/zh.
- **Colour tags do NOT break a marker** (decompile-verified 2026-08-10, and
  worth knowing before restructuring around one): `ReplaceJosa` first runs
  `StripTags`, whose `TagOrNodeClosingPattern` = `(\(|<)\/\w+(\)|>)` removes
  *closing* tags only, so a `.Colorize()`d argument's trailing `</color>`
  is gone by the time `FindLastChar` looks back — the marker resolves off the
  value's real last syllable. (The surviving *opening* tag sits before the
  value and never matters.) A marker after a colorized arg is therefore safe
  in principle; BTG's Empire-fix dialog still anchors a literal noun (세력)
  after each of its three colorized args, because faction and mod names are
  arbitrary text in any script and a fixed particle simply cannot be wrong.
- **`reportString`s must carry no josa marker at all, and no trailing
  period.** `TargetA`/`TargetB` are substituted by `JobUtility`'s plain
  string `Replace` *after* the def value was post-processed at load, so a
  marker there resolves against the literal token text, not the label.
  Vanilla ko sidesteps it by using only invariant particles (`TargetB에게
  TargetA 먹여주는 중`) or none. The form is `~하는 중` / `~ 중` with **no**
  trailing period, where English has one.
- The one safe unmarked case: a symbol that always resolves the same way (a
  fixed pronoun). Def labels, numbers, and any mod-coined term are never
  safe.
- A lint for this lives outside the repo checker (which is language-agnostic);
  the 2026-08-10 pass ran an inline Python reimplementation of `ReplaceJosa`
  (~30 lines: `JosaPatternPaired`, `FindLastChar`, `HasJong`/
  `HasJongExceptRieul`, `StripTags`) over the resolved strings. Simulating
  beats eyeballing — rebuild it rather than trusting a read-through.

Other style rules discovered from the vanilla ko data (mandatory):

- ASCII punctuation (`.` `,`), never `。`. Descriptions/tooltips take polite
  formal `-습니다.`/`-입니다.`; labels, buttons and stat fragments take no
  trailing period.
- Korean **uses spaces**, unlike JP/zh.
- Units attach with no space: `{0}시간`, `{0}일`, `{0}칸`. Some vanilla ko
  files carry a BOM; ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset all | 취소 / 모두 초기화 | | Core Keyed |
| Reset to default / Restore defaults / Default / None | 기본값으로 재설정 / 기본값 복원 / 기본값 / 없음 | | Core `ResetBinding`, `RestoreToDefaultSettings`, `Default`, `None` |
| quality tiers | 끔찍/빈약/평범/상급/완벽/걸작/전설적 | | Core `QualityCategory_*` |
| "of normal+ quality" / "(worth [X])" | 평범 품질 이상의 / (가치: [X]) | | Core `TradeRequest` — verbatim |
| traders guild / guild member(s) | 교역 조합 / 조합원(들) | 상인 길드 | Odyssey `TradersGuild.*` |
| salvagers | 해적 인양단 | 인양자 | Odyssey `Salvagers.label` (its *pawns* are 우주 해적) |
| leader (`leaderTitle`) | 대표 | | Odyssey: TradersGuild=무역 감독관, Salvagers=단장; 대표 is the neutral slot for a small crew |
| trader / orbital trader | 상인 / 궤도 상인 | | Core, Odyssey `AsteroidLetterText` |
| bulk / exotic goods trader | 원자재 상선 / 희귀품 상선 | | Odyssey orbital `TraderKindDef`s (the *caravan* kinds are 상인, the orbital ones 상선) |
| Traders will pay more/less for it. | 상인들이 더 높은 값을 쳐줍니다. / 상인들은 더 적은 돈을 쳐줍니다. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | 금 상감 / 은 상감 | | Odyssey `GoldInlay.label` |
| {0} from {1} are attacking your {2}. | {1}의 {0}(이)가 당신의 {2}(을)를 공격하고 있습니다. | | every Odyssey `FactionDef` — verbatim |
| Attack {0} / Attacking {0}. | {0} 공격 / {0} 공격 중 | | Core site approach strings — verbatim, no trailing period |
| Quest failed: [resolvedQuestName] | 임무 실패: [resolvedQuestName] | | Core `TradeRequest` — verbatim (quest = 임무) |
| [faction_name] became hostile to you. | [faction_name](이)가 적대로 돌아섰습니다. | | Core `TradeRequest` — verbatim |
| orbital platform / settlement platform | 궤도 플랫폼 / 정착지 플랫폼 | | Odyssey `OrbitalPlatform.label`, `SettlementPlatform.label` |
| orbital settlement / settlement / colony | 궤도 정착지 / 정착지 / 정착지 | | Odyssey `SpaceSettlement.label`, Core `Settlement.label`, `PlayerColony` — colony and settlement share 정착지; disambiguate with 내 |
| shuttle | 왕복선 | 셔틀 | Core `Shuttle.label`, Odyssey `Shuttles.label` |
| transport/drop pod vs cargo pods | 수송 포드 vs 화물 낙하기 | | Core `DropPodIncoming*` / `CargoPodCrash` — distinct, don't merge |
| signal jammer / sentry drone / life support unit | 신호 교란기 / 센트리 드론 / 생명 유지 장치 | | Odyssey |
| gravship / gravlite panel / pilot console | 중력부양선 / 중력감응판 / 조종석 | | Odyssey |
| mechhive / orbital relay | 메카노이드 군락 / 궤도 중계기 | | Odyssey `TheGravship.description` |
| goodwill / negotiator / caravan | 우호도 / 협상가 / 상단 | 호감도 | Core `Goodwill`, `Negotiator`, `TradeRequest` |
| silver / market value / comms console / packaged survival meal | 은 / 시장 가치 / 통신기 / 보존 식량 | | Core |
| steel / vacuum / reinforcements / hatch / safe | 강철 / 진공 / 증원군 / 해치 / 금고 | | Core, Odyssey |
| colour labels | `~색` (은색, 회색) | | Core `ColorDef`s — `UniqueWeapon_Gray.label` is 회색, the same family as BTG's silver |
| "Note: This is a difficult scenario…" | 주의: 어려운 시나리오입니다. 초보자에게는 권장하지 않습니다. | | Odyssey `TheGravship` — verbatim |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | 중력부양선을 발사하려면 조종석을 선택한 후 발사 명령을 실행하세요. / 세계 지도에서 '행성 보기'를 선택하여 | | Odyssey `TheGravship` GameStartDialog — verbatim, except vanilla's curly ‘ ’ (which mirrored Odyssey's curly English) becomes ASCII, matching our ASCII English source |
| starting people (ScenPart) | 시작 캐릭터 | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/tend/feed/open) | TargetA 청소 중 / TargetA 구조 중 / TargetA 간호 중 / TargetB에게 TargetA 먹여주는 중 / TargetA 여는 중 | | Core `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1. Core's `Hack.reportString` writes `{TargetA} 해킹 중` — **drop the braces**, our English `TargetA` has none so the checker reads them as an invented placeholder (the exact trap ru hit with `{lookup:}`) |

Mod-decided (no vanilla source — the rows most in need of native review):
**cargo vault** 화물 금고 (hatch variants 보안 화물 금고 해치 / 밀폐된 화물 금고
해치 / 화물 금고 출구), **shuttle bay** 왕복선 격납고, **smuggler's den**
밀수업자 소굴, **threat points** 위협 지수, **orbital steel / rust** 궤도 강철색
/ 녹슨 색 (never 녹색 — that is *green*), **independent traders** 독립 상인,
**Exiled Traders** 추방된 상인, **cargo claim** 화물 인수권, **medbay** 의무실,
**docked vessel** 정박 중인 선박, **docking bays** 정박 구역, **(Vanilla)**
(기본값), **entrenched defender AI** 농성형 방어군 AI, **garrison** 수비대.
`BTG_Settings_ModName` is the localized Workshop title
`교역 조합 확장판` and must stay in sync with the title line of
`.steamworkshop/Description/Korean.txt` (see the CLAUDE.md localization note).

`traitAdjectives` are **bare attributive words prefixed onto an unknown weapon
noun** (Odyssey `GoldInlay`: 황금, 금빛) — never a `-의`/`-한` phrase that would
need agreement. BTG's five silver adjectives are 백은 / 은장 / 영롱한 / 은빛 /
정교한.

**Cross-checked against PWU's own ko pass, landed the same day, independently
grounded** — worth keeping as a caution even though the specific rows are
weapon-domain: two rows genuinely diverged between sibling mods on the same
term (`mechanite`, `armor penetration`) because each was grounded against a
different tar subset. **Ground this mod's own trader/orbital terms
independently against the Core + Odyssey tars rather than assuming a
weapon-mod sibling's word for an adjacent concept transfers.**

The rest of the weapon-mod Korean glossary — weapon/tool/damage vocabulary
and the extensive mod-decided trait-adjective list — is specific to melee
combat text, which this mod has none of. See `../UniqueMeleeWeapons` if that
ever changes.

#### German (grounded in this repo's 2026-08-10 generation pass, on top of
PersonaWeaponsUnbound's 2026-07-28 pass extended across the weapon-mod
siblings)

Language folder is `German` (tar: `German (Deutsch).tar`).

Style rules from the vanilla de data (mandatory, applies to any Keyed
string regardless of mod domain):

- **ASCII single quotes** for cited def labels and UI labels — vanilla writes
  `Forschungsprojekt '{0}'` and `Die Quest '{0}' erfordert …`. Core+Royalty
  Keyed ship 140 single-quoted placeholders and **zero** German `„…"`. Never
  use `„ "`, `» «`, or curly quotes. Pawn names are not quoted.
- **If a dash is genuinely unavoidable it is an en dash `–`, never an em dash
  `—`** (20 vs 0 in Core Keyed) — but per the no-new-dashes rule above,
  reflow instead. German's rate is 9.6/100k tree-wide, and the 2026-08-10
  audit found BTG's German at 6.8x that before its 7 dashes were reflowed to
  `:` and `,` (both of which German joins main clauses with quite happily,
  unlike English).
- Ellipsis is ASCII `...` (74 in Core Keyed, `…` zero).
- Descriptions end with `.`; labels and buttons take none. Player-facing
  prose is informal **du** with imperatives, never Sie — **except scenario
  prose, where Odyssey de addresses the crew as ihr/euch**
  (`TheGravship.description` and its GameStartDialog). BTG follows both: the
  settings window is du/dein, the two scenarios and their game-start dialogs
  are ihr/euch.
- **`reportString`s keep the trailing period and are third-person present
  verbs** — `entfernt TargetA.`, `füttert TargetB mit TargetA.` This is the
  opposite of ru and ko, which both drop the period; don't carry that habit
  over.
- Units: vanilla de writes percentages **tight** (`{0}%`) and hours tight
  (`{0}h`), but watts **spaced** (`{0} W`, 6 occurrences). Copy per unit
  rather than applying one rule.

**Case is the German landmine, not gender** (decompile-verified:
`Verse.GrammarResolverSimple`, `LanguageWorker_German`, `LanguageWordInfo`).
`"key".Translate(args)` — i.e. any ordinary Keyed string, exactly what this
mod's settings window uses — reaches `GrammarResolverSimple`. Its `obj is
string` branch supports `{0_gender ? m : f : n}`, `{0_definite}`,
`{0_indefinite}`, `{0_plural}` on a plain string, resolving gender from the
word itself via `WordInfo/Gender/{Male,Female,Neuter,Other}.txt` (~2450
nouns in Core). **Correction (2026-08-10, re-verified against the 1.6
assembly): it DOES implement `lookup`** — a `{name: args}` span parses as a
*function* call and reaches `LanguageWorker.ResolveFunction`, which handles
`lookup` and `replace`, so `{lookup: {0}; decline; N}` and the 2457-row
`decline.txt` case forms are available in a plain Keyed string after all.
(The Russian section above uses the same mechanism against `Case.txt`.) An
earlier sibling-repo note claimed otherwise. **The 2026-08-10 pass confirmed
the tables and their indexing:** both `WordInfo/decline.txt` (singular) and
`WordInfo/plural_decline.txt` (plural) exist in Core *and* Odyssey, sharing
the header `NOM;1_GEN;2_DAT;3_ACC;4_NOM_DEF;5_GEN_DEF;6_DAT_DEF;7_ACC_DEF` —
so index **3** is bare accusative and **7** is accusative-with-definite-
article. Vanilla's own two uses are worth copying verbatim: every site
approach string is `Greife {lookup: {0}; decline; 3} an` /
`Greift {lookup: {0}; decline; 3} an.`, and every `messageDefendersAttacking`
is `{0} der Fraktion '{1}' greifen deine {lookup: {2}; plural_decline; 7} an.`

**A German lookup miss is genuinely harmless, unlike Russian's**
(decompile-verified): `LanguageWorker_German` does **not** override
`TryLookUp`, so the base runs, and its miss branch returns `keyName` — the
*original*, not the lowercased probe key it built. A mod-coined label
therefore passes through with its capitalization intact and simply stays in
its base form. (`LanguageWorker_Russian` *does* override `TryLookUp` and
lowercases the key first, which is why the ru section warns about it — the
two languages genuinely differ here; don't generalize either way.) So the
vanilla construct is safe to reuse even for a coined label, and for a neuter
noun the miss is additionally indistinguishable from a hit, since German
neuter accusative equals nominative.

What remains true is
that de's article helpers are nominative-only and a `decline` miss falls back
to the key unchanged, so restructuring an oblique slot is still the safer
default when the injected label is mod-coined and absent from the table.
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
offen.` A gender lookup that misses **defaults to masculine**
(`ResolveGender`'s `defaultGender`) — safe only for vanilla nouns in
nominative slots, never for a mod-coined label absent from the Gender
tables.

`PostProcessed` also rewrites a trailing English `'s` to `s` (or a bare `'`
after s/ß/z/x/ce) — a closing ASCII single quote immediately followed by
lowercase `s` is silently mangled, so never write `'{0}'s` in German prose.

**Odyssey de covers nearly everything BTG builds on, and three vanilla defs
are near-exact templates** — check them first before composing anything new:
Core's `TradeRequest` QuestScriptDef (its description frame, the
`qualityInfo` fragment and all three letter strings were byte-identical
reuse for `BTG_TradeRequest`), Odyssey's `TheGravship` ScenarioDef (the
difficulty note and the launch / view-planet sentences), and Odyssey's
`GoldInlay` WeaponTraitDef (`SilverInlay` / `BTG_SilverInlayMelee`).

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Confirm / Randomize | Abbrechen / Zurücksetzen / Bestätigen / Zufällig | | Core buttons |
| Reset to defaults / default | Auf Standard zurücksetzen / Standard | | Core `ResetBinding`, `Default` |
| None | Nichts | Keine | Core `None` |
| quality / tiers | Qualität / übel·schlecht·normal·gut·exzellent·meisterlich·legendär | | Core `Quality`, `QualityCategory_*` |
| "{0} quality or better" | `Qualität {0} oder besser` | | reshaped from Core `NormalQualityOrBetter` (pre-inflected, untemplatable) |
| "of normal+ quality" / "(worth [X])" | in normaler Qualität oder besser / (Wert: [X]) | | Core `TradeRequest` — verbatim, trailing space included |
| traders guild / guild member(s) | Händlergilde / Gildenmitglied(er) | Handelsgilde | Odyssey `TradersGuild.*` |
| salvagers | Schrottpiraten | Berger | Odyssey `Salvagers.label` (its *pawns* are Piraten) |
| leader (`leaderTitle`) | Anführer | | Core `PlayerColony`/`GravshipCrew`; Odyssey: TradersGuild=Handelsmagnat, Salvagers=Boss — Anführer is the neutral slot |
| trader / orbital trader | Händler / Orbitalhändler | | Core `Silver`-era vocabulary; Odyssey `TradersGuild.description` |
| bulk / exotic goods trader | Großhändler / Händler exotischer Güter | | Core orbital `TraderKindDef`s |
| Traders will pay more/less for it. | Händler werden mehr dafür bezahlen. / … weniger dafür bezahlen. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | vergoldet / versilbert | Goldeinlage | Odyssey `GoldInlay.label` is a **participle**, not a noun phrase |
| {0} from {1} are attacking your {2}. | {0} der Fraktion '{1}' greifen deine {lookup: {2}; plural_decline; 7} an. | | every vanilla de `FactionDef` — verbatim |
| Attack {0} / Attacking {0}. | Greife {lookup: {0}; decline; 3} an / Greift {lookup: {0}; decline; 3} an. | | Core site approach strings — verbatim |
| Quest failed: [resolvedQuestName] | Quest gescheitert: [resolvedQuestName] | | Core `TradeRequest` — verbatim |
| [faction_name] became hostile to you. | Die Fraktion [faction_name] wurde dir gegenüber feindselig. | | Core `TradeRequest` — verbatim |
| Who should be credited with [X] …? | Wem soll die [X] für die Erfüllung des Handelsangebotes zuteilwerden? | | Core `TradeRequest` — verbatim |
| hostile to {0} | Feindliche Beziehungen zur Fraktion '{0}' | | shaped from Core `QuestHostileTo` |
| orbital platform / settlement platform | Orbitalplattform / Siedlungsplattform | | Odyssey `OrbitalPlatform.label`, `SettlementPlatform.label` |
| orbital settlement / settlement | orbitale Siedlung / Siedlung | | Odyssey `SpaceSettlement.label`, Core `Settlement.label` |
| shuttle | Raumfähre | Shuttle | Core `Shuttle.label`, Odyssey `Shuttles.label` (shuttle engine = Fährentriebwerk) |
| drop pod vs cargo pod | Landekapsel vs Vorratskapsel | | Core `DropPodIncoming` / `CargoPodCrash` — distinct, don't merge |
| signal jammer / sentry drone / life support unit | Signalstörer / Wächterdrohne / Lebenserhaltungseinheit | | Odyssey |
| gravship / gravlite panel / pilot console | Gravschiff / Gravlitplatte / Pilotenkonsole | | Odyssey |
| mechhive / orbital relay | Mechnest / Orbitalrelais | Mechbau | Odyssey `Mechhive.label`, `TheGravship.description` |
| goodwill / caravan / negotiator | Ruf / Karawane / Unterhändler | Wohlwollen | Core `Goodwill`, `Caravan.label`, `Negotiator` |
| silver / market value / comms console / packaged survival meal | Silber / Marktwert / Funkanlage / Überlebensration | | Core |
| steel / vacuum / safe / power output | Stahl / Vakuum / Tresor / Leistungsabgabe | | Core |
| colour labels | material colours are capitalized nouns (Gold, Jade, Silber); descriptive ones lowercase adjectives (grau, schwarz) | | Core + Odyssey `ColorDef`s |
| "Note: This is a difficult scenario…" | Hinweis: Dies ist ein schwieriges Szenario und wird neuen Spielern nicht empfohlen. | | Odyssey `TheGravship` — verbatim |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | Um das Gravschiff zu starten, selektiere die Pilotenkonsole und klicke auf 'Starten'. / klicke auf 'Planet anzeigen' | | Odyssey `TheGravship` GameStartDialog — verbatim, ASCII single quotes included |
| starting people (ScenPart) | Anzahl Startcharaktere | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/tend/feed/hack/open/board) | entfernt TargetA. / rettet TargetA. / behandelt TargetA. / füttert TargetB mit TargetA. / hackt TargetA. / öffnet TargetA. / betritt TargetA. | | Core `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1, trailing period and all |

Mod-decided (no vanilla source — the rows most in need of native review):
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

`traitAdjectives` are **bare attributive adjectives** in vanilla de (Odyssey
`GoldInlay`: golden, vergoldet) — no gender markers, because these defs feed
no `RulePackDef` name grammar here. BTG's five silver adjectives are silbern /
versilbert / glänzend / silberweiß / edel.

The rest of the weapon-mod German glossary — weapon/tool/damage vocabulary,
the `namerLabels`/`traitAdjectives` `|M|`/`|F|`/`|N|` gender-marker scheme
for `RulePackDef`s, the relic-name truncation rule, and the "never *print*
a `[X_definite]'s` genitive" battle-log lesson — is specific to
`RulePackDef` name generation and melee combat text, neither of which this
mod has (it ships no RulePackDefs). See `../UniqueMeleeWeapons` or
`../PersonaWeaponsUnbound` if that ever changes.

#### Spanish (Castellano) (grounded in this repo's 2026-08-10 generation pass, on
top of the weapon-mod siblings' 2026-07-29 pass)

RimWorld ships **two** Spanish languages: `Spanish (Español(Castellano)).tar` and
`SpanishLatin (Español(Latinoamérica)).tar`. The roster's "Spanish" means the
Castilian one, so the mod folder is `Spanish` (the parenthetical is stripped by
`legacyFolderName`, same mechanism as `Japanese`/`Korean`). A LatAm pass would be a
separate `SpanishLatin` folder, not an edit to this one.

`Verse.LanguageWorker_Spanish` is decompiled and **imposes no hidden
authoring requirements** — no `PostProcessed` override (unlike German), no
particle system (unlike Korean). It prepends `el/la/los/las` and
`un/una/unos/unas` from the word's gender, returns names unchanged, has
full `Pluralize` rules plus a `plural.txt` lookup, and renders ordinals
`N.º`. Notably it does **not** contract `de el`/`a el` — that is the
author's job (see below).

Style rules from the vanilla es data (mandatory):

- **ASCII straight double quotes** for cited def labels: vanilla writes
  `La misión se llama "{0}".` — 7689 ASCII `"` against **7** curly `“` and
  **zero** guillemets `«»`.
- **Inverted opening marks are required**: `¿…?`, `¡…!` (168 / 433 in Core).
- **Zero dashes.** Core+DLC contain **no** em dashes and **no** en dashes, so
  an English `—` must be **reflowed**, not converted. This is the opposite
  of German, which mandates `–`.
- Ellipsis is ASCII `...`. Descriptions end `.`; labels, buttons and stat
  fragments take none, and labels are lowercase noun phrases.
- **Informal tú with imperatives**, decisively: Explora 12 / Explore 0,
  Asegúrate 41 / Asegúrese 0, `tu colonia` 61 / `su colonia` 3.

**`de el` → `del` and `a el` → `al` must be contracted by hand** whenever a
sentence places `de`/`a` directly before an injected `[X_definite]` symbol
(available even in a plain `.Translate()` call, not just a rulepack — see
the German note above on `GrammarResolverSimple`). Core es fixes this 89
times with the colour code baked into the search pattern:

```
{replace: de [RECIPIENT_definite]; "de &lt;color=#D09B61FF>el "-"&lt;color=#D09B61FF>del "}
{replace: a [RECIPIENT_definite]; "a &lt;color=#D09B61FF>el "-"&lt;color=#D09B61FF>al "}
```

Feminine (`de la pirata`) and named entities simply don't match and pass
through untouched, which is correct. **Core es also ships a shorter, buggy
variant** (`{replace: de [X]; ">el "-">del "}`, 20 uses in
`RulePacks_CombatRanged`) that leaves the literal `de ` outside the match
and renders "de del proyectil" — copy the full form only, or restructure so
no `de`/`a` precedes a `_definite` symbol.

**`[RECIPIENT_possessive]` resolves to `su` and has NO plural form** — Core
`Keyed/Grammar.xml` sets `Prohis`/`Proher`/`Proits` all to `su`. Since
Spanish `su` agrees in number with the *possessed* noun, the symbol is only
safe before a **singular** noun. Use the definite article for plurals
instead.

**Units are per-unit, not one rule** (counted over Core+Odyssey Keyed):
percentages tight (`{0}%`), `x` tight (`{0}x`), but watts spaced (`{0} W`,
as in `PowerConnectedRateStored`) and days/hours spaced (`{0} días`). Copy
per unit rather than generalizing.

**Odyssey es covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates** — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, both failure letters, the royal-favor letters) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` (difficulty note + the launch / view-planet sentences) | Odyssey `TheGravship` ScenarioDef |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |
| `BTG_CargoVaultHatch` (description shape, both `CompHackable` strings, the exit's ladder line) | Core `AncientHatch` / `AncientHatchExit` |

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Confirm / Default / None | `Cancelar` / `Restablecer` / `Confirmar` / `Por defecto` / `Ninguno` | | Core buttons |
| quality tiers | `horrible·mediocre·normal·bueno·excelente·obra maestra·legendaria` | | Core `QualityCategory_*` |
| "of normal+ quality" / "(worth [X])" | de calidad normal o superior / (valor [X]) | | Core `TradeRequest` — verbatim, trailing space included |
| traders guild / guild member(s) | gremio de comerciantes / miembro(s) del gremio | gremio comercial | Odyssey `TradersGuild.*` |
| salvagers | chatarreros | carroñeros | Odyssey `Salvagers.label` (its *pawns* are piratas; carroñeros is the descriptive word inside its own description) |
| leader (`leaderTitle`) | jefe | | Odyssey: TradersGuild=maestro comerciante, Salvagers=jefe — jefe is the neutral slot for a small crew |
| trader / orbital trader | comerciante / comerciante orbital | mercader | Core, Odyssey `AsteroidLetterText` |
| bulk / exotic goods trader | mayorista / comerciante de productos exóticos | | Core orbital `TraderKindDef`s |
| Traders will pay more/less for it. | Los comerciantes pagarán más por ella. / … menos por ella. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | incrustación de oro / incrustación de plata | | Odyssey `GoldInlay.label` is a **noun phrase** (unlike de's participle) |
| {0} from {1} are attacking your {2}. | {0} de {1} están atacando tu {2}. | | every Odyssey `FactionDef` — verbatim; note the bare `de {1}`, no "de la facción" |
| Attack {0} / Attacking {0}. | Atacar {0} / Atacando {0}. | | Odyssey `Outpost` approach strings — verbatim |
| Quest failed: [resolvedQuestName] | Misión fallida: [resolvedQuestName] | | Core `TradeRequest` — verbatim (quest = misión) |
| [faction_name] became hostile to you. | La facción [faction_name] se ha vuelto hostil. | | Core `TradeRequest` — verbatim |
| Who should be credited with [X] …? | ¿A quién se le debe acreditar con [X] por cumplir con la solicitud de intercambio? | | Core `TradeRequest` — verbatim |
| hostile to {0} | Relaciones hostiles con {0} | | shaped from Core `QuestHostileTo` ("hostil hacia {0}") |
| No capable negotiator | No hay ningún negociador capaz | | shaped from Core `CommandTradeFailNoNegotiator` |
| orbital platform / settlement platform | plataforma orbital / plataforma de asentamiento | | Odyssey `OrbitalPlatform.label`, `SettlementPlatform.label` |
| orbital settlement / settlement / colony | asentamiento orbital / asentamiento / colonia | | Odyssey `SpaceSettlement.label`; Core `Settlement.label` is **colonia**, but `TradeRequest` prose says "un asentamiento cercano" — asentamiento is the prose word for a faction settlement |
| shuttle | transbordador | lanzadera | Core `Shuttle.label` ("transbordador imperial"), Odyssey `AsteroidLetterText` |
| transport/drop pod vs cargo pod | cápsula de transporte / cápsula de desembarco vs cápsula de carga | | Core `TransportPod`, `DropPodIncoming`, `CargoPodCrash` — distinct, don't merge |
| signal jammer / sentry drone / life support unit | inhibidor de señales / dron centinela / unidad de soporte vital | | Odyssey (Core's older singular "inhibidor de señal" loses to Odyssey's `SpaceSettlement.description`) |
| gravship / gravlite panel / pilot console | gravinave / panel de gravilita / consola de piloto | gravnave | Odyssey `TheGravship` |
| mechhive / orbital relay | mecacolmena / repetidor orbital | | Odyssey `TheGravship.description` (the MapGeneratorDef's "retransmisor orbital" is a different slot) |
| goodwill / caravan / negotiator | reputación / caravana / negociador | buena voluntad | Core `Goodwill`, `Caravan.label`, `Negotiator` |
| raid | asalto | incursión | Core `RaidEnemy.label`, Keyed `Raid` (incursión is reserved for `RaidFriendly`) |
| silver / market value / comms console / packaged survival meal | plata / valor de mercado / consola de comunicaciones / raciones de supervivencia envasadas | | Core |
| steel / vacuum / safe / reinforcements / hatch / vault | acero / vacío / caja fuerte / refuerzos / escotilla / bóveda | | Core, Odyssey (`AncientHatch`, `AncientSafe`; bóveda is the sealed-valuables sense) |
| garrison / outpost | guarnición / puesto de avanzada | | Core `AncientGarrison.label`, `Outpost.description` |
| colour labels | lowercase, gender-invariant where possible: dorado, gris, jade | | Core + Odyssey `ColorDef`s — `UniqueWeapon_Gold.label` is dorado, the family BTG's silver joins |
| "Note: This is a difficult scenario…" | Nota: Este es un escenario difícil. No se recomienda para jugadores sin experiencia. | | Odyssey `TheGravship` — verbatim (vanilla splits the English single sentence in two) |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | Para lanzar la gravinave, selecciona la consola de piloto y luego elige el comando de lanzamiento. / selecciona "ver planeta" | | Odyssey `TheGravship` GameStartDialog — verbatim, ASCII double quotes included |
| starting people (ScenPart) | personas iniciales | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/tend/feed/hack/open/board) | limpiando TargetA. / rescatando a TargetA. / tratando a TargetA. / alimentando a TargetB con TargetA. / hackeando TargetA. / abriendo TargetA. / entrando en TargetA. | | Core `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1. **Keep the trailing period** (unlike ru/ko), and note the personal "a" appears before *animate* targets only |

Mod-decided (no vanilla source — the rows most in need of native review):
**cargo vault** bóveda de carga (hatch variants escotilla segura de la bóveda de
carga / escotilla sellada… / salida de…), **shuttle bay** hangar de
transbordadores, **smuggler's den** guarida de contrabandistas (echoing Odyssey's
`InsectLair` = guarida de insectoides), **threat points** puntos de amenaza,
**orbital steel / rust** acero orbital / óxido, **silver (colour)** plateado,
**independent traders** comerciantes independientes, **Exiled Traders**
Comerciantes exiliados, **cargo claim** derecho de carga, **medbay** enfermería,
**docked vessel** Nave atracada, **docking bays** muelles de atraque,
**(Vanilla)** (Original), **entrenched defender AI** IA de defensores
atrincherados, **resupply** reabastecimiento. "TG maps" is spelled out as
**mapas del gremio** — a Spanish initialism would be opaque.
`BTG_Settings_ModName` is the localized Workshop title
`Gremio de comerciantes mejorado` and must stay in sync with the title line of
`.steamworkshop/Description/Spanish.txt` (see the CLAUDE.md localization note).

**`traitAdjectives` must be gender-invariant in Spanish, and this is a
mechanical constraint, not a style call.** Odyssey's `NamerUniqueWeapon`
rulePack **postposes** the adjective (`[weapon_type] [trait_adjective]`), and
`CompUniqueWeapon` feeds `weapon_type` from the *weapon's* `namerLabels` — an
unknown noun of unknown gender. Vanilla es dodges agreement entirely by
picking invariant forms for `GoldInlay` (`de oro`, `brillante`): a
prepositional phrase or an `-e`/`-ente` adjective. BTG's five silver
adjectives follow suit: de plata / de plata bruñida / brillante / reluciente /
noble. Never ship an `-o`/`-a` adjective here.

The rest of the weapon-mod Spanish glossary — weapon/tool/damage
vocabulary, the `badass_concept`/`conceptF` parallel-symbol-family
technique for `RulePackDef` gender, and quest-site vocabulary — is specific
to name generation and melee combat, which this mod has none of. See
`../UniqueMeleeWeapons` if that ever changes.

**Note on the `de el` hazard in practice:** BTG's only string that injects a
`_definite` symbol is `BTG_CargoVaultHatch`'s `hackedMessage`, and the
English ("bypassed the security **on** {SUBJECT_…Def}") would land a `de`
directly before it. Rather than reach for the `{replace:}` scaffolding above,
the sentence was rebuilt so the symbol is a plain **direct object**
("… ha burlado la seguridad y ha abierto {SUBJECT_…Def}."), which removes the
preposition entirely — the same move vanilla es makes in `AncientHatch`
("ha terminado de hackear {SUBJECT_labelNoParenthesisDef}."). Restructuring
beats `{replace:}` whenever the sentence allows it.

#### French (grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07-29 pass)

Language folder is `French` (tar: `French (Français).tar`).

**`LanguageWorker_French` rewrites every string, and this is the finding
that shapes everything else** (decompile-verified) — including plain
`.Translate()` Keyed strings, not just rulepacks. Its `PostProcessed` runs
five regexes in order:

```
ElisionE   \b(ce|de|je|le|me|ne|se|te|que|quoique|lorsque) + vowel   → c' d' j' l' m' n' s' t' qu' ...
ElisionLa  \bla + vowel                                             → l'
ElisionSi  \bsi il(s)                                               → s'il(s)
DeLe       \bde le(s)                                               → de / des
ALe        \bà le(s)                                                → au / aux
```

**So French is the inverse of Spanish: never hand-contract.** Write `de` /
`le` / `la` plainly and the worker fixes it. Two traps in it:

- **`de le` becomes `de`, not `du`.** Group 2 captures only `e`/`es`, so
  `de les X` correctly yields "des X" but `de le X` yields "de X" — a
  vanilla bug, not guidance to imitate; restructure so the entity is a
  subject, or use an agent phrase — **`par [X_definite]` never contracts**
  and is the clean escape.
- **`IsVowel` includes `h`** — and also `æ`/`œ` (re-verified 2026-08-10
  against the 1.6 assembly) — so the worker cannot tell *h muet* from
  *h aspiré* and elides both. Never place an elidable word directly before
  an h-initial noun without checking which kind it is.
- **`à le`/`à les` DO fuse correctly** (`ReplaceALe` maps them to `au`/`aux`,
  and `à la` never matches), so `à` is a safe preposition to write before a
  `_definite` symbol. `de` is the *only* broken one — don't generalize the
  `de le` trap into a fear of all prepositions.

**`PostProcessed` runs at load, before argument substitution, so elision
never fires across a placeholder.** `de {0}` / `de [settlement_label]` sees a
literal `{`/`[` — not a vowel — and ships unelided. Vanilla fr's own
`TradeRequest.questNameRules` is the tell: every rule picks a preposition that
never needs contracting (`pour`, `avec`, `à [X]`), and BTG's quest names
follow suit. This is the practical reason to restructure rather than trust the
worker whenever an injected symbol is involved.

`WithDefiniteArticle`/`WithIndefiniteArticle` are **overridden**, handling
`l'` before a vowel and `le`/`la` by gender directly — so `[X_definite]` is
reliable in French even in a plain Keyed string. `Pluralize` knows
`-al`→`-aux`, `-au`/`-eu`→`+x`, and leaves `s`/`x`/`z` alone. There is **no
`TryLookUp` override**, and French ships only `WordInfo/Gender` — no
`decline.txt`/`Case.txt` — so the `{lookup: …}` function the ru and de
sections rely on has nothing to read here and is simply unusable.

Style rules from the vanilla fr data (mandatory):

- **Formality is `vous`, decisively** — 564 `vous` against **zero**
  `tu`/`Tu` in Core+DLC Keyed. This is the opposite of German and Spanish,
  both informal. Imperatives are the vous form (`Explorez`, `Faites
  attention`).
- **ASCII straight double quotes** for cited def labels — 332 ASCII `"` and
  **zero** curly `“`/`”` across the whole tree.
- **But guillemets `« … »` for a UI command the player must click**, with a
  **plain ASCII space** inside each (61 occurrences, against 2 with U+202F).
  74 `«` ship tree-wide, 62 of them in DefInjected — this is a real
  convention, not noise, and Odyssey fr's `TheGravship` GameStartDialog
  writes `sélectionnez « voir la planète »`, the exact analog of BTG's
  scenario dialogs. **The three counts above correct an earlier
  Keyed-only reading of this section that reported "14 guillemets,
  inconsistently spaced" and concluded ASCII everywhere.** Same
  nearer-analog rule as zh, which reaches for 「」 in precisely this slot.
- **ASCII apostrophe `'`**, not `’` — load-bearing, not cosmetic: the elision
  worker emits ASCII `'`, so a curly one would not match. Tree-wide it is
  6896 vs 1881, but the split is per-DLC and worth knowing: Core carries
  1848 of the curly ones (legacy `BackstoryDef` prose), while **Odyssey is
  decisively ASCII, 1629 vs 37** — so the DLC whose vocabulary this mod
  builds on agrees with the rule.
- **A space before `:` `!` `?`**, per French typography — a **plain ASCII
  space**, not a no-break or narrow space (3656 plain vs 9 U+00A0 before a
  colon). `;` can't be counted from the raw XML (`&lt;`/`&gt;` entities
  swamp it); prefer a period and sidestep the question.
- **Dashes exist in French but are vanishingly rare, so introduce none.**
  Counted over the whole Core+Odyssey tree with comments stripped: **13 em
  dashes, 15 en dashes in 1.79M characters** — a rate of 1.6 per 100k, the
  lowest of any language here that has any at all. `—` is a parenthetical
  break (Odyssey's `TheGravship.description`) and `–` a bullet marker
  (`  – Recherche débloquée :`). This corrects an earlier "zero dashes"
  claim derived from Keyed alone — the correction is that they are *rare*,
  **not** that they are available: at 1.6/100k, adding even one to a short
  string blows past vanilla's rate, which is exactly what the first draft of
  this pass did (2 dashes = 11.3x vanilla) before they were reflowed. Use
  `:` for an appositive summary and `,` for a contrastive clause, as the
  shipped `BTG_GameStartDialog_IndependentTraders` now does. Ellipsis is
  ASCII `...`.
- **Units are per-unit, not one rule** (counted over Core+Odyssey): `%`
  **tight** (`{0}%`), but `W` and `h` **spaced** (`} W` 5× / `}W` 0×,
  `} h` 14× / `}h` 1×), and `jours` spaced. Vanilla has no trailing
  multiplier suffix at all — its `{0}x` occurrences are all counted
  quantities (`{0}x chemfuel`) — so keep the English's tight `{0}x`.
- Descriptions end `.`; labels, buttons and stat fragments take none, and
  labels are lowercase noun phrases.
- **`reportString`s are third-person present verbs and KEEP the trailing
  period** (`nettoie TargetA.`), like de and es and unlike ru/ko.

**`[X_possessive]` is structurally wrong in French.** Core
`Keyed/Grammar.xml` sets `Prohis`=`son`, `Proher`=`sa`, `Proits`=`son/sa` —
resolved from the **possessor's** gender — but French `son`/`sa` must agree
with the **possessed** noun. The symbol therefore keys off the wrong entity
no matter what; write the possessive literally instead (Core's own
`[RECIPIENT_possessive]de son travail` renders the broken "sonde son
travail", which is vanilla's own evidence not to use it).

**Odyssey fr covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates** — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, all three letter strings) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` + `BTG_GameStartDialog_ExiledTraders` | Odyssey `TheGravship` ScenarioDef — the difficulty note, the gravlite sentence, the launch sentence and the `« voir la planète »` sentence are all verbatim |
| `BTG_CargoVaultHatch` / `_Sealed` / `Exit` | Odyssey `AncientHatch` / `AncientHatchExit` |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Confirm | `Annuler` / `Réinitialiser` / `Confirmer` | | Core buttons |
| Reset to defaults / Default / None | `Réinitialiser les valeurs par défaut` / `Par défaut` / `Aucune` | | Core `ResetBinding`, `Default`, `None` |
| quality tiers | `horrible·médiocre·normal·bon·excellent·merveille·légendaire` | | Core `QualityCategory_*` |
| "of normal+ quality" / "(worth [X])" | de qualité normale ou mieux / (valant [X]) | | Core `TradeRequest` — verbatim, trailing space included |
| traders guild / guild member(s) | guilde des commerçants / membre(s) de la guilde | guilde commerciale | Odyssey `TradersGuild.*` |
| salvagers | récupérateurs | charognards | Odyssey `Salvagers.label` (its *pawns* are pirates; charognards is the descriptive word inside its own description) |
| leader (`leaderTitle`) | chef | | Core `PlayerColony`, Odyssey `GravshipCrew`/`Salvagers` all use chef; TradersGuild's own is maître du commerce |
| trader / orbital trader | commerçant / commerçant orbital | marchand | Core, Odyssey `TradersGuild.description` |
| bulk / exotic goods trader | grossiste / vendeur de produits exotiques | | Core orbital `TraderKindDef`s |
| smuggler | contrebandier | | Core `Orbital_PirateMerchant.label` |
| Traders will pay more/less for it. | Les commerçants en paieront un prix plus élevé. / … en paieront moins cher. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | incrusté d'or / incrusté d'argent | incrustation d'or | Odyssey `GoldInlay.label` is a **participle**, like de's and unlike es's noun phrase |
| {0} from {1} are attacking your {2}. | {0} de {1} attaquent votre {2}. | | every Odyssey `FactionDef` — verbatim (Core `PlayerColony`'s "Les {0} … vos {2}" is the player-side variant) |
| Attack {0} / Attacking {0}. | Attaquer {0} / Attaque {0}. | | Core `Outpost`/`BanditCamp` approach strings — verbatim |
| Quest failed: [resolvedQuestName] | Quête échouée : [resolvedQuestName] | | Core `TradeRequest` — verbatim (quest = quête) |
| [faction_name] became hostile to you. | la faction [faction_name] vous est devenue hostile. | | Core `TradeRequest` — verbatim, lowercase opener and all |
| Who should be credited with [X] …? | Qui doit recevoir la faveur [X] pour avoir satisfait cette commande ? | | Core `TradeRequest` — verbatim |
| No capable negotiator | Aucun négociateur en état de négocier | | shaped from Core `CommandTradeFailNoNegotiator` |
| orbital platform / settlement platform | plateforme orbitale / plateforme d'installation | | Odyssey `OrbitalPlatform.label`, `SettlementPlatform.label` (a MapGeneratorDef, the same slot BTG's is) |
| orbital settlement / settlement | colonie orbitale / base de faction | | Odyssey `SpaceSettlement.label`, Core `Settlement.label` — but "colonie" is also the player's, so lean on context |
| shuttle | navette | navette spatiale | Core `Shuttle.label`, Odyssey `Shuttles.label` |
| drop pod vs cargo pod | capsule de largage vs capsule de cargo | | Core `DropPodIncoming` / `LetterLabelCargoPodCrash` — distinct, don't merge |
| signal jammer / sentry drone / life support unit | brouilleur de signal / drone sentinelle / unité de survie | | Odyssey |
| gravship / gravlite panel / pilot console | vaisseau gravitationnel / panneau de gravlite / console de pilotage | gravnavire | Odyssey |
| mechhive / orbital relay | ruche mécanoïde / relais orbital | | Odyssey `Mechhive.label`, `OrbitalRelay.label` |
| goodwill / caravan / negotiator | bonne entente / caravane / marchand | bonne volonté | Core `Goodwill`, `Caravan.label`, `Negotiator` — note the Keyed `Negotiator` slot is *marchand*, while the trade-fail message says *négociateur* |
| hatch / safe / vault | trappe / coffre-fort / coffre | | Odyssey `AncientHatch.description`, `AncientSafe.label` |
| garrison / outpost | garnison / avant-poste | | Core `AncientGarrison.label`, `Outpost.label` |
| market value / silver / steel / comms console / packaged survival meal / vacuum | valeur marchande / argent / acier / console de com / ration de survie / vide | | Core (the Keyed `MarketValue` slot is *Prix de base*, a different one — use the StatDef label) |
| reinforcements / raid | renforts / raid | | Core `MessageMechanoidsReinforcementsDrop`, Keyed `Raid` |
| colour labels | lowercase bare nouns/adjectives: or, gris, jade, calcaire | | Core + Odyssey `ColorDef`s — `UniqueWeapon_Gold.label` is **or**, reusing the resource noun, which is the family BTG's silver joins |
| "Note: This is a difficult scenario…" | Remarque : Il s'agit d'un scénario difficile et il n'est pas recommandé pour les nouveaux joueurs. | | Odyssey `TheGravship` — verbatim |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | Pour lancer le vaisseau, sélectionnez la console de pilotage, puis sélectionnez la commande de lancement. / sélectionnez « voir la planète » | | Odyssey `TheGravship` GameStartDialog — verbatim, guillemets included |
| starting people (ScenPart) | colons de départ | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/tend/feed/hack/open/board) | nettoie TargetA. / porte secours à TargetA. / soigne TargetA. / nourrit le patient TargetB avec TargetA. / pirate TargetA. / ouvre TargetA. / entre dans TargetA. | | Core `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1, trailing period and all. Note `FeedPatient` inserts "le patient" that the English has no word for |

Mod-decided (no vanilla source — the rows most in need of native review):
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

**`traitAdjectives` follow vanilla fr's own shape for `GoldInlay`: a bare noun
(`or`) plus masculine-singular adjectives (`doré`).** French cannot agree with
an unknown weapon noun's gender here and vanilla does not try — Odyssey's
`NamerUniqueWeapon` fr postposes the adjective under a hardcoded masculine
`Le [weapon_type] [weapon_adjective]`, so masculine forms are consistent with
vanilla's own choice rather than a hazard. BTG's five silver adjectives are
argent / argenté / étincelant / d'argent / raffiné.

**The `de le` hazard in practice:** BTG's only string injecting a `_definite`
symbol is `BTG_CargoVaultHatch`'s `hackedMessage`, whose English ("bypassed the
security **on** {SUBJECT_…Def}") would land a `de` right before it — the one
preposition the worker breaks. The sentence was rebuilt so the symbol is a
plain **direct object** (`… a contourné la sécurité et ouvert {SUBJECT_…Def}.`),
which is also what vanilla fr does in `AncientHatch` (`a terminé de pirater
{SUBJECT_labelNoParenthesisDef}.`). Restructuring beats fighting the worker —
the same conclusion the de and es sections reach from their own directions.

The rest of the weapon-mod French glossary — weapon/tool/damage vocabulary,
the rule-level gender constraint technique for `RulePackDef`s
(`staggered(SUBJECT_gender==Female)->…`), `traitAdjectives`/`namerLabels`
shape rules, and quest-site vocabulary — is specific to name generation and
melee combat text, which this mod has none of. See
`../UniqueMeleeWeapons` if that ever changes.

#### Brazilian Portuguese (grounded in this repo's 2026-08-10 generation pass, on
top of the weapon-mod siblings' 2026-07-29 pass)

Language folder is **`PortugueseBrazilian`** (tar: `PortugueseBrazilian
(Português Brasileiro).tar`). RimWorld ships European `Portuguese` as a
*separate* language; a pt-PT pass would be its own folder.
`LanguageInfo.xml` declares `languageWorkerClass`
**`LanguageWorker_Portuguese`** — the two languages share one worker.

**The worker does almost nothing, and that is the finding that shapes
everything else** (decompile-verified). It overrides **only**
`WithIndefiniteArticle` and `WithDefiniteArticle` (prepending `o `/`a `/`os
`/`as `, `um `/`uma `/`uns `/`umas ` by gender). It has **no
`PostProcessed` override**, so the base `LanguageWorker.PostProcessed`
runs — and that only calls `MergeMultipleSpaces()`. No elision, no
contraction, no `'s` rewriting, no particles.

**So Portuguese is the hard case: its contractions are orthographically
mandatory and nothing supplies them.** `de`+`o`=`do`, `de`+`a`=`da`,
`em`+`o`=`no`, `em`+`a`=`na`, `a`+`o`=`ao`, `a`+`a`=`à`, `por`+`o`=`pelo`
(plus every plural). Consequences, relevant to any Keyed prose that injects
a definite-article'd label, not only rulepacks:

- **Never write `de` / `em` / `a` / `por` directly before a `[X_definite]`
  symbol.** `_definite` prepends a bare `o `, nothing fuses it, and the
  literal **"de o pirata"** ships — and **vanilla pt-BR ships exactly this
  bug** in its own combat packs. Frequency is not correctness.
- **The clean escapes are `com`, `para`, `contra`, `sem`, `sobre`,
  `entre`** — none contract with the article. Otherwise restructure so the
  entity is a subject.
- **The idiomatic vanilla technique is to use the bare `[X_label]` and
  write the contracted article yourself, hedged**: Core's ranged pack
  writes `do(a) [INITIATOR_label]`.
- There are **zero `{replace:}` blocks** anywhere in pt-BR's rulepacks —
  don't invent one; restructure instead.

Style rules from the vanilla pt-BR data (mandatory):

- **ASCII straight double quotes**, **zero em/en dashes** (reflow an
  English `—`, as in es — the opposite of de and fr, which each keep one),
  ASCII ellipsis `...` and apostrophe `'`. Counted tree-wide with comments
  stripped at the 2026-08-10 pass: Core 1.20M chars carries **0** em, **0**
  en, 202 ASCII `"`, 1 curly pair, 1 `…`; Odyssey 209k carries **0** em,
  **2** en, 3 curly apostrophes. This is the *lowest* dash profile of any
  language here, so BTG's pt-BR ships zero — the two hyphen-dashes in
  `BTG_GameStartDialog_IndependentTraders` were reflowed to `:` and `,`.
- **Cited UI commands take those same ASCII double quotes**, not corner
  brackets or guillemets: Odyssey pt-BR's own `TheGravship` GameStartDialog
  writes `selecione "exibir planeta"`, the exact analog of BTG's scenario
  dialogs.
- **Units are per-unit, not one rule** (counted over Core+Odyssey): `%`
  **tight** (`{0}%`), but `W` and `h` **spaced** (`} W` 5× / `}W` 0×,
  `} h` 6× / `}h` 1×) and `dias` spaced (`} dias` 27×). `x` is mixed in
  vanilla (11 tight / 6 spaced), so keep the English's tight `{0}x`.
- **No space before `:` `;` `!` `?`** — the exact opposite of French, and
  the two languages are otherwise close enough that this is an easy
  cross-contamination.
- No `¿`/`¡` — that is Spanish only.
- **Formality is `você`, decisively** — imperatives take the você form
  (`Clique`, `Selecione`, `Escolha`, `Certifique-se`, `Faça`).
- Descriptions end `.`; labels, buttons and stat fragments take none, and
  labels are lowercase.

**Gender hedging is a distinct technique from every other language here,
and pt-BR applies it to the surface text itself**, pervasively — articles,
participles, contractions and possessives alike get a literal **`(a)`**:
`O(a)`, `um(a)`, `do(a)`, `pelo(a)`. A `.Translate()` / templated string
instead takes the inline resolver split (`{PAWN_gender ? o : a}`); which
shape applies depends on whether the string is plain Keyed prose (literal
`(a)`) or a resolver-fed template (inline split) — check the field, not a
blanket rule.

**`[X_possessive]` is unusable here too, for a different reason than
French.** Core `Keyed/Grammar.xml` sets `Prohis`=`o`, `Proher`=`a`,
`Proits`=`o(a)` — a bare **definite article**, not a possessive pronoun,
keyed off the **possessor's** gender while Portuguese must agree with the
**possessed** noun. Write the possessive literally, as French does, though
for a distinct underlying reason — check `Keyed/Grammar.xml`'s actual
values rather than assuming the symbol inflects.

**Casing is per def type in vanilla pt-BR, and it is the one convention that
differs most from every other language here** — most of which use lowercase
noun phrases across the board. Counted at the 2026-08-10 pass: `FactionDef`
labels, pawn nouns and `leaderTitle`s are **Title Case** (`Guilda dos
Mercadores`, `Membro da Guilda`, `Mestre Comercial`); so are `PawnKindDef`
(`Cidadão da Guilda`), `MapGeneratorDef` (`Plataforma de Assentamento`,
`Covil de Insetos`) and `ScenPartDef` (`Pessoas Iniciais`, `Método de
Chegada`). But `SitePartDef` (`posto avançado`), `WorldObjectDef`
(`assentamento orbital`), `ColorDef` (`dourado`, `cinza`) and most `ThingDef`
labels (`entrada do estoque ancestral`, `console de comunicação`) are
**lowercase**. Match the def type, not a blanket rule.

**Odyssey pt-BR covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates** — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, all three letter strings) | Core `Script_TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` + `BTG_GameStartDialog_ExiledTraders` | Odyssey `TheGravship` ScenarioDef — the difficulty note, the gravlite sentence, the launch sentence and the `"exibir planeta"` sentence are all verbatim |
| `BTG_CargoVaultHatch` / `_Sealed` / `Exit` | Odyssey `AncientHatch` / `AncientHatchExit` |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Reset to defaults / Default / None / Confirm | `Cancelar` / `Redefinir` / `Restaurar padrão(es)` / `Padrão` / `Nenhum` / `Aceitar` | `Confirmar` | Core buttons. `Confirm`=`Aceitar`, `ResetBinding`=`Restaurar padrão`, `RestoreToDefaultSettings`=`Restaurar Padrões` |
| quality tiers | `horrível·pobre·normal·bom·excelente·obra-prima·lendário` | `ruim` for poor | Core `QualityCategory_*` |
| "of normal+ quality" / "(worth [X])" | de qualidade normal+ / (Custando [X]) | | Core `TradeRequest` — verbatim, trailing space and vanilla's mid-sentence capital included |
| traders guild / guild member(s) | Guilda dos Mercadores / Membro(s) da Guilda | Guilda Comercial | Odyssey `TradersGuild.*` |
| salvagers | Salteadores | Saqueadores | Odyssey `Salvagers.label` (its *pawns* are Piratas; *saqueadores* is the descriptive word inside its own description) |
| leader (`leaderTitle`) | Líder | | Core/Odyssey `PlayerColony`/`GravshipCrew`; Odyssey: TradersGuild=Mestre Comercial, Salvagers=Chefe — Líder is the neutral slot |
| trader / merchant | comerciante / mercador | | Odyssey `TradersGuild.description` uses both: "comerciantes orbitais" for the trade role, "mercadores" for the guild's people |
| bulk / exotic goods trader | comerciantes de produtos variados / comerciantes de produtos exóticos | | Core orbital `TraderKindDef`s — **plural**, like ru |
| Traders will pay more/less for it. | Comerciantes pagarão mais por ela. / Comerciantes pagarão menos por ela. | | Odyssey `GoldInlay`/`Ugly` — verbatim |
| gold/silver inlay | incrustação de ouro / incrustação de prata | | Odyssey `GoldInlay.label` is a **noun phrase** (like es, unlike de/fr's participle) |
| {0} from {1} are attacking your {2}. | {0} de {1} estão atacando seu(s) {2}. | | every Odyssey `FactionDef` — verbatim, literal `(s)` number hedge included |
| Attack {0} / Attacking {0}. | Ataque {0} / Atacando {0}. | | Core `Outpost` approach strings — verbatim |
| Quest failed: [resolvedQuestName] | A missão falhou: [resolvedQuestName] | | Core `TradeRequest` — verbatim (quest = missão) |
| [faction_name] became hostile to you. | [faction_name] tornou-se hostil a você. | | Core `TradeRequest` — verbatim |
| Who should be credited with [X] …? | Quem deve ser creditado com [asker_faction_royalFavorLabel] de favor, por atender à solicitação de negociação? | | Core `TradeRequest` — verbatim |
| No capable negotiator | Nenhum negociador capaz | | shaped from Core `CommandTradeFailNoNegotiator` |
| orbital platform / settlement platform | plataforma orbital / Plataforma de Assentamento | | Odyssey `OrbitalPlatform.label` (lowercase WorldObjectDef), `SettlementPlatform.label` (Title Case MapGeneratorDef) |
| orbital settlement / settlement | assentamento orbital / assentamento | colônia | Odyssey `SpaceSettlement.label`, Core `Settlement.label` — colônia is the *player's* |
| shuttle | ônibus espacial | transporte | Core `Shuttle.description`, Odyssey `Shuttles.label`; Core's `Shuttle.label` "transporte imperial" is the Royalty-specific def |
| transport/drop pod vs cargo pod | cápsula de transporte vs cápsula de carga | | Core `TransportPod`/`DropPodIncoming` vs `LetterLabelCargoPodCrash` — distinct, don't merge |
| signal jammer / sentry drone / life support unit | bloqueador de sinal / drone sentinela / unidade de suporte de vida | | Odyssey |
| gravship / gravlite panel / pilot console | gravinave / painel de gravilita / console do piloto | | Odyssey `TheGravship` |
| mechhive / orbital relay | mecholmeia / retransmissor orbital | | Odyssey `Mechhive.label`, `OrbitalRelay.label` |
| goodwill / caravan / negotiator | Boa vontade / caravana / negociador | reputação | Core `Goodwill`, `Caravan.label`, `Negotiator` — pt-BR keeps the literal "boa vontade" that de/es/fr all reject |
| raid / reinforcements | invasão / reforços | | Core `Raid`, `RaidEnemy.label`, `MessageMechanoidsReinforcementsDrop` |
| hatch / safe / garrison / outpost | alçapão / cofre / guarnição / posto avançado | escotilha | Odyssey `AncientHatch.description`, `AncientSafe.label`; Core `AncientGarrison.label`, `Outpost.label` |
| silver / steel / market value / comms console / packaged survival meal / vacuum | prata / aço / valor de mercado / console de comunicação / refeição de sobrevivência embalada / vácuo | | Core |
| colour labels | lowercase adjectives: dourado, cinza, jade | | Core + Odyssey `ColorDef`s — `UniqueWeapon_Gold.label` is **dourado**, the family BTG's silver joins |
| "Note: This is a difficult scenario…" | Nota: Este é um cenário difícil e não é recomendado para novos jogadores. | | Odyssey `TheGravship` — verbatim |
| "To launch the gravship, select the pilot console…" / "select 'view planet'" | Para lançar a gravinave, selecione o console do piloto e depois comande o lançamento. / selecione "exibir planeta" | | Odyssey `TheGravship` GameStartDialog — verbatim, ASCII double quotes included |
| starting people (ScenPart) | Pessoas Iniciais | | Core `ConfigPage_ConfigureStartingPawns.label` — identical English source, reuse verbatim |
| reportStrings (clean/rescue/hack/open/board) | limpando TargetA. / resgatando TargetA. / hackeando TargetA. / abrindo TargetA. / entrando em TargetA. | | Core+Odyssey `JobDef`s — verbatim; BTG's NPC-safe `BTG_*` copies reuse them 1:1. Gerund phrases that **KEEP the trailing period** (like de/es/fr, unlike ru/ko) |

**Two Core pt-BR `reportString`s are wrong and were deliberately not
mirrored** (flagged in `JobDef/Jobs.xml` for native review): `TendPatient` is
`Cuidando de TargetA.` with a stray mid-string capital no other reportString
has (lowercased here), and `FeedPatient` is `levando TargetA para TargetB.`
— "carrying", which simply does not translate "feeding TargetA to TargetB"
(BTG ships `alimentando TargetB com TargetA.`). Frequency is not correctness
applies to vanilla's own data, not only to its contraction bugs.

Mod-decided (no vanilla source — the rows most in need of native review):
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

**`traitAdjectives` follow vanilla pt-BR's own shape for `GoldInlay`: a bare
noun (`ouro`) plus a masculine-singular adjective (`dourado`).** Odyssey's
`NamerUniqueWeapon` pt-BR **preposes** the adjective under a hardcoded
masculine `O [weapon_adjective] [weapon_type]`, so masculine forms are
consistent with vanilla's own choice rather than a hazard — the same
conclusion fr reaches from its own direction. BTG's five silver adjectives
are prata / prateado / reluzente / argênteo / refinado.

**The contraction hazard in practice:** BTG's only string injecting an
article'd symbol is `BTG_CargoVaultHatch`'s `hackedMessage`, whose English
("bypassed the security **on** {SUBJECT_…Def}") would land a `de` right
before it — and nothing in pt-BR fuses it, so `de o cofre` would ship
literally. The sentence was rebuilt so the symbol is a plain **direct
object** (`… burlou a segurança e abriu {SUBJECT_labelNoParenthesisDef}.`),
which is also what vanilla pt-BR does in `AncientHatch` (`terminou de hackear
{SUBJECT_labelNoParenthesisDef}.`). Restructuring beats hedging — the same
conclusion the de, es and fr sections reach.

The rest of the weapon-mod pt-BR glossary — weapon/tool/damage vocabulary,
the curated `Strings/Words/Nouns/Weapons.txt` corpus, and quest-site
vocabulary — is specific to name generation and melee combat text, which
this mod has none of. See `../UniqueMeleeWeapons` if that ever changes.

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans "{0}") — injected labels never inflect, and quoting
  sidesteps case and agreement problems. **But the quote mark is per *slot*,
  not per language**: ja's 「」 marks quoted text (note contents, inscriptions)
  while a UI command the player clicks takes ASCII `"…"`, and zh reaches for
  「」 in exactly that second slot. Same two brackets, opposite assignments —
  find the nearest vanilla analog rather than porting a sibling CJK rule.
  **Korean is a harder exception, and
  porting the ja form actively breaks it**: ko solves the same problem
  mechanically with josa markers, and `FindLastChar` looks through only
  ASCII `'` `"` `)` to find the syllable that decides the particle. Curly
  `" "` and corner `「 」` are not skipped, so `「{0}」(을)를` silently ships an
  unresolved `(을)를`. Inject bare and mark the particle instead.
- **Check whether the worker contracts before writing any contraction
  scaffolding — the answer inverts between languages.** Spanish must fuse
  `de`+`el` by hand (in a rulepack or in any `.Translate()` call using
  `[X_definite]`); French needs **no scaffolding at all**, because
  `LanguageWorker_French.PostProcessed` elides and fuses automatically;
  Portuguese is the worst case, where contractions are mandatory and nothing
  supplies them at all — see the German/Spanish/French/pt-BR sections above
  for the specifics. **French does not "double-apply", though** (verified
  2026-08-10 by reimplementing its five regexes and running the whole BTG fr
  tree through them: zero rewrites): the regexes match only the
  *uncontracted* forms, so an already-elided `l'accès` or a hand-written `du`
  passes through untouched — which is exactly how vanilla fr authors its own
  data. The worker is a safety net for text assembled at runtime, not a
  reason to write unnatural French; and because it runs at *load*, before
  argument substitution, it can never help across a `{0}` or `[symbol]`
  anyway. Verify a vanilla pattern actually works before copying it;
  frequency is not correctness (both es and fr ship a demonstrably broken
  contraction in their own combat packs).
- **A "no hidden mechanics" worker is itself a finding, not a reason to
  skip the check — and a language may have no worker at all.** Spanish's and
  Portuguese's workers impose few or no authoring requirements, but
  Portuguese's *absence* of a `PostProcessed` override is precisely what
  makes every contraction the author's problem. **Japanese goes further: no
  `LanguageWorker_Japanese` exists** (verified against the assembly's full
  typedef list, and `LanguageInfo.xml` declares no `languageWorkerClass`), so
  the base worker runs and only merges repeated spaces. The *same* absence
  cuts opposite ways in the two languages — it creates the author's problem
  in pt-BR and removes it in ja — because what matters is whether the
  language's own grammar needs the rewriting, not whether the hook is
  missing. Confirm a worker's existence by enumerating the types, not by
  assuming a major language has one, and note that languages can share one
  worker class (`PortugueseBrazilian` and `Portuguese` both use
  `LanguageWorker_Portuguese`).
- **The possessive symbol (`[X_possessive]`/`Prohis`/`Proher`/`Proits`) has
  a different correct answer per language, so never generalize one.**
  Korean drops it, German keeps and inflects it inline, Spanish keeps it
  only before a singular noun, French and Portuguese both must write the
  possessive literally, for two different underlying reasons. Check
  `Keyed/Grammar.xml`'s actual values for the target language rather than
  assuming the symbol inflects.
- **A def field's official label can differ across the def *types* that
  share its name or concept**, and translating from the wrong one is an
  easy, invisible error (es Core's DamageDef `Stab`=`apuñalamiento` vs
  HediffDef `Stab`=`puñalada`, for instance — see the weapon-mod skills for
  the full pattern). This mod patches both a `StatDef` (`MarketValue`,
  `SellPriceFactor`) and a `ThingDef` (`Xenogerm`) — if either ever grows a
  translatable field, confirm which def *type*'s official label you're
  grounding against, not just the term.
- **When two vanilla files disagree, prefer the nearer analog, not the
  more central one.** es Core's generic ColorDefs render purple `morado`,
  but Odyssey's own colour defs — same def type, same purpose — render it
  `púrpura`. For this mod that means: if Biotech's own xenotype/gene Keyed
  data and Core's generic item/trader vocabulary ever disagree on a term,
  Biotech wins.
- **Don't spend a vanilla word on the wrong slot.** Map any concept this
  mod needs against vanilla's existing usage of that word *first* (e.g.
  don't reuse a word Biotech already spends on a specific gene or xenotype
  concept for something else), and coin only for what's genuinely left
  over.
- **Distinguish comment occurrences from value occurrences when mining the
  tar.** Grepping a symbol across a language's files counts English
  `<!-- EN: -->` text too, which can invert the conclusion about whether a
  symbol is actually used in translated values. Strip comments before
  counting.
- **Check for a `LanguageWorker_<Language>` before generating.** It
  post-processes every string, so it can impose authoring requirements no
  amount of reading the vanilla data will reveal as *mandatory* — Korean's
  josa markers are invisible until you find `ReplaceJosa`. Decompile it:
  `ilspycmd "$RIMWORLD_PATH/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" -t
  "Verse.LanguageWorker_<Language>"`. Languages with heavy inflection
  (Russian, Polish, Turkish, Czech, German) are the ones to check first. **A
  worker can also do work *for* you**, which is just as important to
  know — French's elides and contracts automatically, so the correct
  authoring there is to write the uncontracted form and leave it alone.
- **Simulate the worker rather than reasoning about it.** Its regexes are
  short enough to reimplement in a few lines of Python, and running your
  actual strings through them catches what eyeballing does not.
- **Know which resolver your strings actually reach** (decompile-verified).
  `"key".Translate(args)` — every Keyed string this mod has — goes to
  `Verse.GrammarResolverSimple`, *not* the full rulepack `GrammarResolver`,
  and the two support different things. On a plain `string` arg
  `GrammarResolverSimple` gives you `{N_gender ? … : … : …}`,
  `{N_definite}`, `{N_indefinite}`, `{N_plural}` and the pronoun family —
  gender is looked up from the word itself via `LanguageWordInfo`, so no
  `NamedArgument` metadata is needed. **It also implements the `lookup` and
  `replace` *functions*** — a `{name: args}` span (note the colon) is parsed
  as a function call and dispatched to `LanguageWorker.ResolveFunction`, so
  `{lookup: {0}; Case; 3}` / `{lookup: {0}; decline; N}` reach the target
  language's `TryLookUp` and its `WordInfo` tables from a plain Keyed string.
  This corrects an earlier note in the German section (now amended in
  place) that said case forms were unreachable there. What is genuinely unreachable
  is anything the *rulepack* resolver adds on top. A lookup miss returns the
  key unchanged rather than erroring, so the mechanism is safe to use and
  degrades to nominative — but a mod-coined label is never in the table, so
  restructuring is still right when the injected value is one of ours.
- **A `lookup` miss does not degrade identically across languages — check
  whether the worker overrides `TryLookUp`** (decompile-verified 2026-08-10).
  The base implementation lowercases only its internal probe key and returns
  the caller's `keyName` untouched on a miss, so a mod-coined label keeps its
  capitalization (this is German's behaviour — it overrides only the article
  helpers, `PostProcessed`, `OrdinalNumber`, `Pluralize` and
  `PostProcessThingLabelForRelic`). `LanguageWorker_Russian` *does* override
  `TryLookUp` and lowercases the key before the lookup, so a ru miss can come
  back lowercased. Same construct, different failure mode.
- **The checker compares argument placeholders, not grammar constructs,
  and that distinction is deliberate.** `{0}`/`{PAWN_labelShort}`-style
  placeholders are supplied by the C# call site and must match English
  exactly; `{PAWN_gender ? o : a}` is inflection the target language needs
  and uninflected English never has. `Scripts/check-translations.py`
  excludes any `{...}` containing `?` before comparing (see the comment on
  `GRAMMAR_CONSTRUCT_RE`). Confirm the named argument actually exists at
  the call site before relying on one. **Two constructs are special-cased
  and both are worth knowing before you write one:** `{N_numCase ? … : … : …}`
  is rewritten back to `{N}` first, because it is the only ?-construct that
  prints its argument and therefore legitimately *replaces* the bare
  placeholder (see `NUM_CASE_RE`); and `{lookup: …}` is **not** handled — a
  nested `{lookup: {2}; Case; 3}` compares correctly only because the regex
  finds the inner `{2}`, while `{lookup: [some_symbol]; Case; 1}` reads as one
  invented placeholder and fails. Restructure rather than fight it.
- When an English string is reworded, refresh the EN comments in every
  language **in the same commit** — the checker reports the mismatch as
  STALE either way, but batching avoids churn.
- Coined vanilla terms may be a portmanteau in one language and a plain
  word in another — always check, never extrapolate between languages.
- Mod-coined terms recur across Keyed prose that restates them. When
  generation is chunked across files or subagents, reconcile those terms
  across the whole language before committing.

The RulePackDef-specific lessons the weapon-mod siblings also carry — which
part of speech a `traitAdjectives`/`namerLabels`-style field needs per
language, the several techniques for solving name-grammar gender (German's
inline markers, Spanish's parallel symbol families, French's rule-level
constraints, Portuguese's literal hedge), and material-neutral
trait-adjective phrasing — do not apply here, since this mod ships no
RulePackDefs and generates no names. See `../UniqueMeleeWeapons`,
`../UniqueWeaponsUnbound`, or `../PersonaWeaponsUnbound` if that ever
changes.

## Workflows

### Initial generation (`/translate <Language>`)

1. Run the checker; confirm English itself is clean.
2. Enumerate the target key set: every Keyed key in
   `1.6/Languages/English/Keyed/BTG.xml`, plus every `required` DefInjected
   entry in the `Scripts/expected-injections.json` sidecar, taking the
   English source text from each entry's `english` field — NOT from the
   English DefInjected tree, which covers only half the surface (see the
   file-map bullet above). Route each gated def's entries to its own compat
   root (`BTG_SilverInlayMelee.*` under
   `1.6/Mods/UniqueMeleeWeapons/Languages/<Language>/...`,
   `BTG_ConfigureStartingPawnsXenotypes.label` under
   `1.6/Mods/Biotech/Languages/<Language>/...`); everything else goes in
   the main `1.6/Languages/<Language>/` tree. The checker enforces this
   both ways — an entry must live in the load root that declares its def —
   and its missing-entry errors name the root a translation belongs under.
3. Extract the vanilla tar for the target language into the scratchpad;
   build a term list for the grounded terms above (Core + Odyssey).
4. Translate via subagent(s) carrying: the glossary, the vanilla term list,
   the EN-comment requirement, placeholder rules, and formatting rules.
   Cover both Keyed and every DefInjected folder — a pass that only does
   Keyed is incomplete for this mod.
5. Run the checker (`--strict` for new languages); fix everything, including
   any DefInjected folder the checker flags as missing or out of sync with
   `Scripts/expected-injections.json`.
6. Review the diff yourself before committing. Commit message and PR text
   must state machine-assisted origin and invite native review.

### Update pass (`/translate update`)

1. Run the checker; it lists missing keys and stale entries per language,
   across both Keyed and DefInjected.
2. Translate only that delta, refreshing each entry's EN comment. Keep each
   DefInjected def-type folder in sync with its English counterpart —
   adding, renaming, or dropping a def in one language's folder without
   mirroring the change everywhere is exactly what the checker's sidecar
   comparison is meant to catch.
3. Leave correct existing entries untouched. Re-run the checker.

### Audit only (`/translate check`)

Run the checker and report; change nothing.

## Optional in-game verification

RimWorld Dev Mode offers "Save translation report" and "clean up translation
files" (Verse.LanguageReportGenerator / TranslationFilesCleaner). These need a
running game with the mod loaded — useful as a final QA pass, not a
substitute for the checker.
