TODOs

claude --add-dir ../VanillaExpanded/VanillaGravshipExpanded2 --add-dir ../VanillaExpanded/VanillaGravshipExpanded --add-dir ../VanillaExpanded/VanillaExpandedFramework

- eyeball the cargo vault stack/wealth caps in-game (sliders, Unlimited notch, dev-mode trim log, trimmed vault looks proportional, relock returns stock)
- cargo vault cap: hold the spawned subset constant across relocks. Today relock merges the leftovers back into stock and the next hack re-caps the whole pool, so a capped vault tops back up from never-shown stock (fine as a crash fix; total lootable is unchanged). Fix = per-settlement record of the first hack's chosen entries (SettlementStockCache or world comp), later hacks spawn only from it, reset on trader rotation. Nuance: return must not merge (merge destroys the absorbed Thing and its ID) or must tag things with a marker comp; works on existing saves since a vault with no record seeds one on its next hack.
- Investigate Simple Warrants fulfilment

- update About.xml/steamworkshop description to note VGE2 compat
- update workshop page images
- recolor vanilla/suggested/default tags in mod settings ui?
- Smoke test with performance analyzer (particularly the post-mapgen spike)
- test CWTL on den quest site
- upgrade BTG TG settlement map size even without VGE2?

- VGE2 integration (working doc + tracker: Docs/VGE2_INTEGRATION.md)
  - scope chance of combat vacsuit on independent traders starting pawns possible?
  - smuggler's den storeroom, armory and workshop doodads use ancient vanilla defs instead of cleaner maintained-look VGE variants. control room consoles too?
  - refactor airlock defenses to appear similar to VGE2s?
  - author/patch BTG side/corner prefabs
  - consider mechs/sentries/lords
  - Enable vault access needn't depend on custom mapgen, disables vault quest rewards on smugglers den quest
  - VGE2 balance reset button (S18): second reset button shown only under VGE2, resets the settings that act on that encounter to VE-tuned defaults (best effort)

- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Rare Subroom placement small room off-by-one?
- Bind band nodes?

- Way more backstories?!

- Investigate mod Settlement Visit compatibility

- Add trade/equivalence-focused storyteller?
- Mod integration: VREA maintenance room
- Mod integration: Choose where to land (independent traders scenario)
- Mod integration: Knick knacks
- Mod integration: VE Brewing whisky shelf in Captain's quarters?
- Mod integration: Include UMW weapons in unique weapon pools?

- propagate the Steam-postfix-tolerant mod detection helper (Integrations/ModDetection.cs, BTG commit f60e7a0) to the other mods and the template repo: replace every ModsConfig.IsActive(string) in C# with it (exact match misses `id_steam` Workshop installs; MayRequire/IfModActive were never affected). Known sites: UniqueWeaponsUnbound AlphaArmouryIntegration.cs and VanillaSkillsExpandedIntegration.cs; grep the rest. Clean session.
- upstream l10n: sidecar freshness check only compares label/description, so an English edit to a nested field (quest rulesStrings) never forces a regen
- upstream missing faction check on Building_AndroidStand.CannotUseNowReason(Pawn)
