# Japanese — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07 pass. Family-shared mechanics (LanguageWorker
behavior, style/corpus rules, vanilla-grounded common vocabulary) live in
the `l10n/` submodule at `l10n/languages/Japanese.md` — this file holds only
what is specific to Better Traders Guild.

## Def-to-vanilla-template cross-reference

Odyssey ja covers nearly everything BTG builds on, and four vanilla defs
are near-exact templates — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (description frame, `qualityInfo`, both failure-letter strings) | Core `TradeRequest` QuestScriptDef |
| `BTG_ExiledTraders` + `BTG_GameStartDialog_ExiledTraders` | Odyssey `TheGravship` ScenarioDef — the difficulty note, the gravlite sentence, the launch sentence, the `"惑星を見る"` sentence and the closing mechhive sentence are all verbatim |
| `BTG_CargoVaultHatch` (both `CompHackable` strings) | Odyssey `AncientHatch` |
| `BTG_SmugglersDen.description` ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | リーダー | | Core/Odyssey `GravshipCrew`, `Ancients`, `Insect` all use リーダー; Odyssey: TradersGuild=交易修士, Salvagers=ボス — relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

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

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

**`traitAdjectives` must be ATTRIBUTIVE forms that read as a prefix on an
unknown weapon noun** — vanilla ja's `GoldInlay` uses 金の / 黄金の, i.e.
の-terminated noun modifiers. な-terminated adjectival nouns and plain
attributive verbs (輝く) work identically. Japanese needs no agreement of any
kind, so unlike de/es/fr/pt-BR the weapon noun's identity never constrains the
choice. BTG's five silver adjectives are 銀の / 銀めっきの / 輝く / 白銀の / 精巧な.

## Other notes

**One Core ja `TradeRequest` string is wrong and was deliberately not
mirrored** (flagged in `QuestScriptDef/QuestScripts.xml` for native review):
`LetterTextFavorReceiver` reads `誰が[X]を持っていると信じるべきですか?` — "who
should we believe *holds* [X]?" — inverting the English, where the player picks
who *receives* the favor. BTG ships
`この取引の要求を満たした功績として,誰に[asker_faction_royalFavorLabel]を与えますか?`
instead. Same lesson pt-BR's section records from its own direction: frequency
is not correctness, and it applies to vanilla's own data.
