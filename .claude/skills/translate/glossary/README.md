# Glossary — BTG-specific terminology

These per-language files (`Russian.md`, `Japanese.md`, `ChineseSimplified.md`,
`Korean.md`, `German.md`, `Spanish.md`, `French.md`,
`PortugueseBrazilian.md`) hold everything about a language's translation
that is specific to Better Traders Guild: mod-coined terms (cargo vault,
smuggler's den, threat points, orbital steel/rust, and the like), the
localized Workshop title (`BTG_Settings_ModName`), BTG's def-to-vanilla-
template reuse map, `leaderTitle` slotting decisions, `SilverInlay` /
`BTG_SilverInlayMelee` trait-adjective choices, and worked phrasing
decisions tied to specific `BTG_` defs (e.g. the `hackedMessage`
restructurings forced by German/Spanish/French/Portuguese contraction
rules).

Family-shared, mod-independent findings — LanguageWorker mechanics, style
and corpus rules, and vanilla-grounded common vocabulary (trader,
settlement, goodwill, gravship, orbital platform, signal jammer, salvagers,
and so on) — live upstream in the `l10n/` submodule at
`l10n/languages/<Language>.md` (canonical checkout: `~/dev/rimworld-l10n`),
since they apply to any mod in the family, not just this one.

When a future translation pass coins a new BTG-specific term, record it
here. If a pass instead surfaces a correction to shared mechanics or
vocabulary, send that fix upstream to the l10n repo rather than duplicating
it here.
