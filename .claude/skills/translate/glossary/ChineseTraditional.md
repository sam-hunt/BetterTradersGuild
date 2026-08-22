# Traditional Chinese — Better Traders Guild glossary

Grounded in this repo's 2026-08-22 generation pass (machine-assisted, awaiting
native review). Family-shared mechanics (LanguageWorker behavior, style/corpus
rules, vanilla-grounded common vocabulary) live in the `l10n/` submodule at
`l10n/languages/ChineseTraditional.md` — this file holds only what is specific
to Better Traders Guild.

**Not a conversion of the zh-Hans glossary.** Every term below was re-grounded
against the zh-Hant Core+Odyssey tars, and several came out different from
zh-Hans: Traders Guild is 商人公會 (not 商会), sentry drone is 哨衛無人機
(not 哨兵), shuttle is 太空梭 (not 穿梭機), leader is 領導者 (not 领袖),
job reportStrings take no trailing 。 (zh-Hans ends every one with 。), and
parentheses are ASCII `( )` (zh-Hans uses full-width （ ）).

## Def-to-vanilla-template cross-reference

Odyssey already ships zh-Hant for almost everything BTG builds on. Four vanilla
defs are near-exact templates and were reused wholesale in the 2026-08-22 pass —
check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (quest name/description rules, both failure letters, the royal-favor letters) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` (scenario description + game-start dialog) | Odyssey `TheGravship` ScenarioDef — the "Note:" paragraph and dialog ¶¶1(sent. 2-3), 2, 3 are verbatim |
| `SilverInlay` / `BTG_SilverInlayMelee` | Odyssey `GoldInlay` WeaponTraitDef (鑲金 → 鑲銀; description sentence 2 verbatim) |
| `BTG_SmugglersDen` SitePartDef description ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |
| `BTG_ConfigureStartingPawnsXenotypes.label` | Core `ConfigPage_ConfigureStartingPawns.label` 起始人口數, verbatim |
| `BTG_*.messageDefendersAttacking` | Odyssey `TradersGuild.messageDefendersAttacking`, verbatim |
| `BTG_Clean` / `BTG_Hack` / `BTG_Rescue` / `BTG_TendPatient` / `BTG_FeedPatient` / `BTG_OpenContainer` / `BTG_BoardLaunchable` reportStrings | Core `Clean` / `Hack` / `Rescue` / `TendPatient` / `FeedPatient` / `Open` / `EnterTransporter`, verbatim |

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | 領導者 | 領袖, 首領 | Odyssey `GravshipCrew.leaderTitle` — same English source |
| station (the physical structure: hatch/exit descriptions, vault inventory) | 軌道平台 | | Odyssey's orbital platform; keeps the two BTG station senses distinct |
| orbital station (the settlement entity, `[settlement_label]` slot) | 軌道據點 | | Odyssey `SpaceSettlement.label` |

Known vanilla awkwardness, reused anyway for consistency:
`BTG_TradeRequest...nodeIfChosenPawnSignalUsed.text.slateRef` reuses Core's
誰應該作為[asker_faction_royalFavorLabel]以完成此次交易任務？ — a loose reading of
"Who should be credited with X", but the English is byte-identical to Core's, so
diverging would show the player two phrasings for one vanilla string.

## Mod-coined terms (pending native review)

**cargo vault** 貨物保險庫 (hatch variants 貨物保險庫安全艙門 / 貨物保險庫密封艙門 /
貨物保險庫出口), **shuttle bay** 太空梭機庫, **smuggler's den** 走私巢穴,
**threat points** 威脅點數, **orbital steel / rust** 軌道鋼色 / 鐵鏽色
(vanilla zh-Hant ColorDefs suffix material names with 色), **independent
traders** 獨立商人, **Exiled Traders** 流放商人, **cargo claim** 貨物提取權,
**medbay** 醫療艙, **Black Market Station** (quest name) 黑市太空站, **docking
bay / roster** 泊位 / 停泊排班, **orbital ring** 軌道環, **trade frequency**
貿易頻道, **transponder** 應答機, **threshold** 門檻, **selection weight** 權重,
**contact (inside man)** 內線, **entrenched defender AI** 固守型守衛AI (AI set
solid, per Core's 選擇AI故事敘述者).

`BTG_Settings_ModName` is the localized Workshop title **強化商人公會** and must
stay in sync with the title line of
`.steamworkshop/Description/ChineseTraditional.txt` (see the CLAUDE.md
localization note); it is also injected into the Empire-fix dialog as a
colorized mod name. It carries 商人公會, Odyssey's own Traders Guild term, so
players searching the faction name find the mod.

## Quoting the one UI command

`BTG_GameStartDialog_ExiledTraders` renders 選擇「查看星球」 with corner brackets,
**deliberately deviating** from vanilla's 選擇“查看星球” in the string it otherwise
reuses verbatim: that curly pair is Odyssey's single `“` in 89k value-chars
(35 「 against it), so the outlier is the vanilla slip, not the rule.

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

`traitAdjectives` must be **bare attributive words** that read as a prefix on a
weapon noun (白銀/銀/閃亮/銀白/精良 → 銀白長劍), never a 的- or 之-terminated
phrase. Same rule Odyssey's own `GoldInlay` follows (黃金/金).
