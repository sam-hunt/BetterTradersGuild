# Simplified Chinese — Better Traders Guild glossary

Grounded in this repo's 2026-08-09 generation pass. Family-shared mechanics
(LanguageWorker behavior, style/corpus rules, vanilla-grounded common
vocabulary) live in
the `l10n/` submodule at `l10n/languages/ChineseSimplified.md` — this file
holds only what is specific to Better Traders Guild.

## Def-to-vanilla-template cross-reference

Odyssey already ships zh for almost everything BTG builds on. Four vanilla
defs are near-exact templates and were reused wholesale in the 2026-08-09
pass — check them first before composing anything new:

| BTG content | Vanilla template |
|---|---|
| `BTG_TradeRequest` (quest name/description rules, both failure letters, the royal-favor letters) | Core `TradeRequest` QuestScriptDef — 4 of its required slateRefs are byte-identical reuse |
| `BTG_ExiledTraders` (scenario description + game-start dialog) | Odyssey `TheGravship` ScenarioDef |
| `SilverInlay` / `BTG_SilverInlayMelee` | Odyssey `GoldInlay` WeaponTraitDef |
| `BTG_SmugglersDen` SitePartDef description ¶2 | Odyssey `SpaceSettlement.description` ¶2, verbatim |

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | 领袖 | | Odyssey `GravshipCrew.leaderTitle` — relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

**cargo vault** 货物保险库 (hatch variants 货物保险库安全舱门 / 封闭舱门 / 出口),
**shuttle bay** 穿梭机库, **smuggler's den** 走私巢穴, **threat points**
威胁点数, **orbital steel / rust** 轨道钢 / 锈色, **independent traders**
独立商人, **Exiled Traders** 流放商人, **cargo claim** 货物提取权.
`BTG_Settings_ModName` is the localized Workshop title
`强化商会` and must stay in sync with the title line of
`.steamworkshop/Description/ChineseSimplified.txt` (see the CLAUDE.md localization note) (it is also injected into the Empire-fix dialog as a
colorized mod name).

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

`traitAdjectives` must be **bare attributive words** that read as a prefix on
a weapon noun (银/白银/闪耀/银白/精良 → 银白长剑), never a 的-terminated
phrase.
