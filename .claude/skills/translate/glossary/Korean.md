# Korean — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, on top of the
weapon-mod siblings' 2026-07 pass. Family-shared mechanics (LanguageWorker
behavior, style/corpus rules, vanilla-grounded common vocabulary) live in
the `l10n/` submodule at `l10n/languages/Korean.md` — this file holds only
what is specific to Better Traders Guild.

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | 대표 | | Odyssey: TradersGuild=무역 감독관, Salvagers=단장; 대표 is the neutral slot for a small crew — relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

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

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

`traitAdjectives` are **bare attributive words prefixed onto an unknown weapon
noun** (Odyssey `GoldInlay`: 황금, 금빛) — never a `-의`/`-한` phrase that would
need agreement. BTG's five silver adjectives are 백은 / 은장 / 영롱한 / 은빛 /
정교한.

## Other notes

**Cross-checked against PWU's own ko pass, landed the same day, independently
grounded** — worth keeping as a caution even though the specific rows there
are weapon-domain: two rows genuinely diverged between sibling mods on the
same term (`mechanite`, `armor penetration`) because each was grounded
against a different tar subset. **Ground this mod's own trader/orbital
terms independently against the Core + Odyssey tars rather than assuming a
weapon-mod sibling's word for an adjacent concept transfers.**
