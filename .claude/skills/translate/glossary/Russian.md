# Russian — Better Traders Guild glossary

Grounded in this repo's 2026-08-10 generation pass, plus UWU PR #6 native
review. Family-shared mechanics (LanguageWorker behavior, style/corpus
rules, vanilla-grounded common vocabulary) live in
the `l10n/` submodule at `l10n/languages/Russian.md` — this file holds only
what is specific to Better Traders Guild.

## BTG def usage

| English | Use | Never | Why |
|---|---|---|---|
| leader (`leaderTitle`) | глава | | Odyssey: TradersGuild=магистр торговли, Salvagers=главарь; глава is the neutral slot — relevant to `Script_BTG_SmugglersDen`'s `[asker_faction_leaderTitle]` token |

## Mod-coined terms (pending native review)

**cargo vault** грузовое хранилище (hatch variants защищённый люк грузового
хранилища / запечатанный люк… / выход из…), **shuttle bay** ангар челноков,
**smuggler's den** логово контрабандистов, **threat points** очки угрозы,
**orbital steel / rust** орбитальная сталь / ржавый, **independent traders**
независимые торговцы, **Exiled Traders** Торговцы-изгнанники, **cargo claim**
право на груз, **medbay** медотсек, **docked vessel** пришвартованное судно,
**(Vanilla)** (как в оригинале). `BTG_Settings_ModName` is the localized Workshop title
`Улучшенная гильдия торговцев` and must stay in sync with the title line of
`.steamworkshop/Description/Russian.txt` (see the CLAUDE.md localization note).

## Trait adjectives — `SilverInlay` / `BTG_SilverInlayMelee`

`traitAdjectives` are **masculine nominative singular adjectives** in vanilla
ru (Odyssey `GoldInlay`: золотой, золоченый) — Russian cannot agree with an
unknown weapon noun's gender through `GrammarResolverSimple`, and vanilla
simply does not try.

The five shipped values (from the DefInjected XML, both the `SilverInlay`
main-tree entries and the `BTG_SilverInlayMelee` UMW compat-root mirror):
серебряный (silver), посеребрённый (silvered), блестящий (gleaming),
серебристый (argent), изящный (fine).
