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
  Today it lists English (Source) and Simplified Chinese (Machine-assisted);
  every other target is still Planned.

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
- Second language root: the UMW-gated `BTG_SilverInlayMelee.*` entries
  (`WeaponTraitDef`) live under `1.6/Mods/UniqueMeleeWeapons/Languages/English/...`, a folder
  LoadFolders.xml loads only when Unique Melee Weapons is active — MayRequire
  is ignored on DefInjected entries, so the gate must be the folder. Their
  translations mirror that root (`1.6/Mods/UniqueMeleeWeapons/Languages/<Language>/...`), never
  the main `1.6` tree, or they would error at startup without UMW.
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
market value, silver). **Only Simplified Chinese has been grounded in this
repo so far** (2026-08-09) — its table below carries real BTG
trader/settlement vocabulary and is safe to build on. Every other language's
table is still **style/mechanics reference only**, inherited from the
weapon-mod siblings; ground that language's own trader/settlement terms
against the Core + Odyssey tars and record them here before relying on them.

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

#### Russian (from UWU PR #6 native review)

| English | Use | Never | Why |
|---|---|---|---|
| Cancel (button) | Отменить | Отмена | vanilla `Cancel`; buttons use infinitive verbs |
| report/inspect strings | noun phrases | finite verbs | matches inspect-pane convention |

The dropped rows (weapon `trait`, gun `charge`) and the mod-decided
WeaponCategoryDef labels are weapon-domain vocabulary with no equivalent in
this mod — see `../UniqueMeleeWeapons`'s skill if that ever changes. This
repo has not yet run a Russian generation pass; add xenogerm/xenotype rows
here once one lands.

#### Japanese (from the weapon-mod siblings' 2026-07 generation)

RimWorld's language folder is `Japanese` (tar: `Japanese (日本語).tar`).

Style rules discovered from the vanilla JP data (mandatory):

- Vanilla JP uses ASCII punctuation: `,` and `.` — never `、` or `。`.
- Descriptions/tooltips: polite です/ます form ending `.`; labels/buttons take
  no period.
- Quote injected def labels and cross-referenced UI labels with 「」. Suffixes
  and parentheticals take no leading space and use ASCII parens.
- DLC names stay in Latin script (Biotech, Royalty, Odyssey), as does MOD.

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Reset to defaults | キャンセル / リセット / デフォルトに戻す | | vanilla Keyed buttons |
| quality tiers | 壊れかけ/低品質/標準品/良品/秀品/名品/幻の一品 | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | 貿易商は高値で/低い価格でこれを買い取ります. | | Odyssey `GoldInlay`/`Ugly` descs — reuse verbatim; directly relevant to this mod's trader-price framing |

The rest of the weapon-mod Japanese glossary — weapon/tool/damage
vocabulary, the attributive-form (`の`/`な`-terminated) requirement for
`traitAdjectives`, the `[stuff_adjective]の[noun]` name-grammar composition,
and battle-log grammar — is specific to `RulePackDef` name generation and
melee combat text, which this mod has none of. See `../UniqueMeleeWeapons`
if that ever changes. This repo has not yet run a Japanese generation pass;
add xenogerm/xenotype rows here once one lands.

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
- An English em dash `—` becomes a **double** em dash ——, not a single one
  (vanilla: 这并不是古老的人类科技——而是一个机械族信标).
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
`BTG_Settings_ModName` is deliberately left as the Latin brand `Better
Traders Guild` (it is also injected into the Empire-fix dialog as a
colorized mod name).

`traitAdjectives` must be **bare attributive words** that read as a prefix on
a weapon noun (银/白银/闪耀/银白/精良 → 银白长剑), never a 的-terminated
phrase. The rest of the weapon-mod Simplified Chinese glossary —
weapon/tool/damage vocabulary and the name-grammar composition rules (的/之
linking, material compounding) — is specific to `RulePackDef` name
generation and melee combat text, which this mod has none of. See
`../UniqueMeleeWeapons` if that ever changes.

#### Korean (from the weapon-mod siblings' 2026-07 generation)

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
- The one safe unmarked case: a symbol that always resolves the same way (a
  fixed pronoun). Def labels, numbers, and any mod-coined term are never
  safe.
- A lint for this lives outside the repo checker (which is language-agnostic).

Other style rules discovered from the vanilla ko data (mandatory):

- ASCII punctuation (`.` `,`), never `。`. Descriptions/tooltips take polite
  formal `-습니다.`/`-입니다.`; labels, buttons and stat fragments take no
  trailing period.
- Korean **uses spaces**, unlike JP/zh.
- Units attach with no space: `{0}시간`, `{0}일`, `{0}칸`. Some vanilla ko
  files carry a BOM; ours never do.

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Reset all | 취소 / 초기화 / 모두 초기화 | | Core Keyed |
| quality tiers | 끔찍/빈약/평범/상급/완벽/걸작/전설적 | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | 상인들이 더 높은 값을 쳐줍니다. / 상인들은 더 적은 돈을 쳐줍니다. | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim; directly relevant to this mod's trader-price framing |

**Cross-checked against PWU's own ko pass, landed the same day, independently
grounded** — worth keeping as a caution even though the specific rows are
weapon-domain: two rows genuinely diverged between sibling mods on the same
term (`mechanite`, `armor penetration`) because each was grounded against a
different tar subset. **Ground TSX's own Biotech-domain terms independently
against the Biotech tar rather than assuming a weapon-mod sibling's word for
an adjacent concept transfers.**

The rest of the weapon-mod Korean glossary — weapon/tool/damage vocabulary
and the extensive mod-decided trait-adjective list — is specific to melee
combat text, which this mod has none of. See `../UniqueMeleeWeapons` if that
ever changes. This repo has not yet run a Korean generation pass; add
xenogerm/xenotype rows here once one lands.

#### German (preseeded from PersonaWeaponsUnbound's 2026-07-28 generation,
extended across the weapon-mod siblings 2026-07-28)

Language folder is `German` (tar: `German (Deutsch).tar`).

Style rules from the vanilla de data (mandatory, applies to any Keyed
string regardless of mod domain):

- **ASCII single quotes** for cited def labels and UI labels — vanilla writes
  `Forschungsprojekt '{0}'`. Core+Royalty Keyed ship 140 single-quoted
  placeholders and **zero** German `„…"`. Never use `„ "`, `» «`, or curly
  quotes. Pawn names are not quoted.
- **En dash `–`, never em dash `—`** (20 vs 0). English source uses `—`, so
  every dash needs converting; `<!-- EN: -->` comments keep the English form
  verbatim.
- Ellipsis is ASCII `...` (74 in Core Keyed, `…` zero).
- Descriptions end with `.`; labels and buttons take none. Player-facing
  prose is informal **du** with imperatives, never Sie.

**Case is the German landmine, not gender** (decompile-verified:
`Verse.GrammarResolverSimple`, `LanguageWorker_German`, `LanguageWordInfo`).
`"key".Translate(args)` — i.e. any ordinary Keyed string, exactly what this
mod's settings window uses — reaches `GrammarResolverSimple`. Its `obj is
string` branch supports `{0_gender ? m : f : n}`, `{0_definite}`,
`{0_indefinite}`, `{0_plural}` on a plain string, resolving gender from the
word itself via `WordInfo/Gender/{Male,Female,Neuter,Other}.txt` (~2450
nouns in Core). But it implements **no `lookup` function**, so `{lookup:
{0}; decline; N}` — the only route to the 2457-row `decline.txt` case forms
— is unavailable there, and de's article helpers are nominative-only.
Gender is solvable, case is not: restructure any oblique slot (a sentence
needing a dative/accusative/genitive form of an injected label) rather than
guessing an article. A gender lookup that misses **defaults to masculine**
(`ResolveGender`'s `defaultGender`) — safe only for vanilla nouns in
nominative slots, never for a mod-coined label absent from the Gender
tables.

`PostProcessed` also rewrites a trailing English `'s` to `s` (or a bare `'`
after s/ß/z/x/ce) — a closing ASCII single quote immediately followed by
lowercase `s` is silently mangled, so never write `'{0}'s` in German prose.

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Confirm / Randomize | Abbrechen / Zurücksetzen / Bestätigen / Zufällig | | Core buttons |
| Reset to defaults / default | Auf Standard zurücksetzen / Standard | | Core `ResetBinding`, `Default` |
| None | Nichts | Keine | Core `None` |
| quality / tiers | Qualität / übel·schlecht·normal·gut·exzellent·meisterlich·legendär | | Core `Quality`, `QualityCategory_*` |
| "{0} quality or better" | `Qualität {0} oder besser` | | reshaped from Core `NormalQualityOrBetter` (pre-inflected, untemplatable) |

The rest of the weapon-mod German glossary — weapon/tool/damage vocabulary,
the `namerLabels`/`traitAdjectives` `|M|`/`|F|`/`|N|` gender-marker scheme
for `RulePackDef`s, the relic-name truncation rule, and the "never *print*
a `[X_definite]'s` genitive" battle-log lesson — is specific to
`RulePackDef` name generation and melee combat text, neither of which this
mod has (it ships no RulePackDefs). See `../UniqueMeleeWeapons` or
`../PersonaWeaponsUnbound` if that ever changes. This repo has not yet run
a German generation pass; add xenogerm/xenotype rows here once one lands.

#### Spanish (Castellano) (from the weapon-mod siblings' 2026-07-29 generation)

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

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Confirm / Default / None | `Cancelar` / `Restablecer` / `Confirmar` / `Por defecto` / `Ninguno` | | Core buttons |
| quality tiers | `horrible·mediocre·normal·bueno·excelente·obra maestra·legendaria` | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | `Los comerciantes pagarán más por ella.` / `… menos por ella.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim; directly relevant to this mod's trader-price framing |

The rest of the weapon-mod Spanish glossary — weapon/tool/damage
vocabulary, the `badass_concept`/`conceptF` parallel-symbol-family
technique for `RulePackDef` gender, `traitAdjectives`/`namerLabels` shape
rules, and quest-site vocabulary — is specific to name generation and melee
combat, which this mod has none of. See `../UniqueMeleeWeapons` if that
ever changes. This repo has not yet run a Spanish generation pass; add
xenogerm/xenotype rows here once one lands.

#### French (from the weapon-mod siblings' 2026-07-29 generation)

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
- **`IsVowel` includes `h`**, so the worker cannot tell *h muet* from
  *h aspiré* and elides both. Never place an elidable word directly before
  an h-initial noun without checking which kind it is.

`WithDefiniteArticle`/`WithIndefiniteArticle` are **overridden**, handling
`l'` before a vowel and `le`/`la` by gender directly — so `[X_definite]` is
reliable in French even in a plain Keyed string. `Pluralize` knows
`-al`→`-aux`, `-au`/`-eu`→`+x`, and leaves `s`/`x`/`z` alone.

Style rules from the vanilla fr data (mandatory):

- **Formality is `vous`, decisively** — 564 `vous` against **zero**
  `tu`/`Tu` in Core+DLC Keyed. This is the opposite of German and Spanish,
  both informal. Imperatives are the vous form (`Explorez`, `Faites
  attention`).
- **ASCII straight double quotes** for cited def labels — 356 ASCII `"`
  against 14 guillemets `«»` (inconsistently spaced) and **zero** curly `“`.
- **ASCII apostrophe `'`**, not `’` (1991 vs 65) — load-bearing, not
  cosmetic: the elision worker emits ASCII `'`, so a curly one would not
  match.
- **A space before `:` `;` `!` `?`**, per French typography — a **plain
  ASCII space**, not a no-break or narrow space.
- **Zero dashes.** An English `—` must be **reflowed**, as in Spanish and
  unlike German, which mandates `–`. Ellipsis is ASCII `...`.
- Descriptions end `.`; labels, buttons and stat fragments take none, and
  labels are lowercase noun phrases.

**`[X_possessive]` is structurally wrong in French.** Core
`Keyed/Grammar.xml` sets `Prohis`=`son`, `Proher`=`sa`, `Proits`=`son/sa` —
resolved from the **possessor's** gender — but French `son`/`sa` must agree
with the **possessed** noun. The symbol therefore keys off the wrong entity
no matter what; write the possessive literally instead (Core's own
`[RECIPIENT_possessive]de son travail` renders the broken "sonde son
travail", which is vanilla's own evidence not to use it).

| English | Use | Never | Why |
|---|---|---|---|
| quest / mod UI: Cancel / Reset / Reset to defaults / Default / None | `Annuler` / `Réinitialiser` / `Réinitialiser les valeurs par défaut` / `Par défaut` / `Aucune` | | Core buttons |
| quality tiers | `horrible·médiocre·normal·bon·excellent·merveille·légendaire` | | Core `QualityCategory_*` |
| Traders will pay more/less for it. | `Les commerçants en paieront un prix plus élevé.` / `Les commerçants en paieront moins cher.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim; directly relevant to this mod's trader-price framing |

The rest of the weapon-mod French glossary — weapon/tool/damage vocabulary,
the rule-level gender constraint technique for `RulePackDef`s
(`staggered(SUBJECT_gender==Female)->…`), `traitAdjectives`/`namerLabels`
shape rules, and quest-site vocabulary — is specific to name generation and
melee combat text, which this mod has none of. See
`../UniqueMeleeWeapons` if that ever changes. This repo has not yet run a
French generation pass; add xenogerm/xenotype rows here once one lands.

#### Brazilian Portuguese (from the weapon-mod siblings' 2026-07-29 generation)

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
  English `—`, as in es and fr — the opposite of de), ASCII ellipsis `...`
  and apostrophe `'`.
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

| English | Use | Never | Why |
|---|---|---|---|
| Cancel / Reset / Reset to defaults / Default / None / Confirm | `Cancelar` / `Redefinir` / `Restaurar padrão` / `Padrão` / `Nenhum` / `Aceitar` | `Confirmar` | Core buttons. `Confirm`=`Aceitar`, `ResetBinding`=`Restaurar padrão` |
| quality tiers | `horrível·pobre·normal·bom·excelente·obra-prima·lendário` | `ruim` for poor | Core `QualityCategory_*` |
| Traders will pay more/less for it. | `Comerciantes pagarão mais por ela.` / `Comerciantes pagarão menos por ela.` | | Odyssey `GoldInlay`/`Ugly` — reuse verbatim; directly relevant to this mod's trader-price framing |

The rest of the weapon-mod pt-BR glossary — weapon/tool/damage vocabulary,
the preposed-namer constraint that forces gender-invariant
`traitAdjectives`, the curated `Strings/Words/Nouns/Weapons.txt` corpus, and
quest-site vocabulary — is specific to name generation and melee combat
text, which this mod has none of. See `../UniqueMeleeWeapons` if that ever
changes. This repo has not yet run a Brazilian Portuguese generation pass;
add xenogerm/xenotype rows here once one lands.

### Cross-language lessons

- Wrap injected `{0}` def labels in the language's quote marks (JP 「{0}」,
  RU «{0}», zh-Hans "{0}") — injected labels never inflect, and quoting
  sidesteps case and agreement problems. **Korean is the exception, and
  porting the ja form actively breaks it**: ko solves the same problem
  mechanically with josa markers, and `FindLastChar` looks through only
  ASCII `'` `"` `)` to find the syllable that decides the particle. Curly
  `" "` and corner `「 」` are not skipped, so `「{0}」(을)를` silently ships an
  unresolved `(을)를`. Inject bare and mark the particle instead.
- **Check whether the worker contracts before writing any contraction
  scaffolding — the answer inverts between languages.** Spanish must fuse
  `de`+`el` by hand (in a rulepack or in any `.Translate()` call using
  `[X_definite]`); French must do the **opposite and write nothing**,
  because `LanguageWorker_French.PostProcessed` elides and fuses
  automatically, so hand-contracting would double-apply; Portuguese is the
  worst case, where contractions are mandatory and nothing supplies them at
  all — see the German/Spanish/French/pt-BR sections above for the
  specifics. Verify a vanilla pattern actually works before copying it;
  frequency is not correctness (both es and fr ship a demonstrably broken
  contraction in their own combat packs).
- **A "no hidden mechanics" worker is itself a finding, not a reason to
  skip the check.** Spanish's and Portuguese's workers impose few or no
  authoring requirements, but Portuguese's *absence* of a `PostProcessed`
  override is precisely what makes every contraction the author's problem.
  Read what the worker does **not** do as carefully as what it does, and
  note that languages can share one worker class (`PortugueseBrazilian` and
  `Portuguese` both use `LanguageWorker_Portuguese`).
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
  `NamedArgument` metadata is needed. It implements **no `lookup` function
  at all**, so `{lookup: {0}; decline; N}` and every case form it would
  produce are unavailable there. For inflecting languages that means
  gender is usually solvable and **case is not**: restructure so nothing
  has to agree with the injected label. See the German section above for
  worked rewrites.
- **The checker compares argument placeholders, not grammar constructs,
  and that distinction is deliberate.** `{0}`/`{PAWN_labelShort}`-style
  placeholders are supplied by the C# call site and must match English
  exactly; `{PAWN_gender ? o : a}` is inflection the target language needs
  and uninflected English never has. `Scripts/check-translations.py`
  excludes any `{...}` containing `?` before comparing (see the comment on
  `GRAMMAR_CONSTRUCT_RE`). Confirm the named argument actually exists at
  the call site before relying on one.
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
   file-map bullet above). Route the UMW-gated `BTG_SilverInlayMelee.*`
   entries to the compat root
   (`1.6/Mods/UniqueMeleeWeapons/Languages/<Language>/DefInjected/WeaponTraitDef/`);
   everything else goes in the main `1.6/Languages/<Language>/` tree, never
   the other way around — the checker cannot see which root an entry sits
   in, but the game can (a gated entry in the main tree is a startup error
   for users without UMW).
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
