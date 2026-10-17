# TODOs

## Feature work

- Way more backstories?!

### VGE2 Integration (working doc + tracker: Docs/VGE2_INTEGRATION.md)

- `claude --add-dir ../VanillaExpanded/VanillaGravshipExpanded2 --add-dir ../VanillaExpanded/VanillaGravshipExpanded --add-dir ../VanillaExpanded/VanillaExpandedFramework`
- upstream VGE2 fix: GenSpawn_Spawn_Patch.Prefix hands oxygen packs/astrorigs to hostile newborns and children (BTG crib spawns) without checking the apparel's developmentalStageFilter, logging "tried to wear ... but is not allowed to"
- author/patch BTG side/corner prefabs for nursery/commander/crew-quarters/medbay
- does VGE need its own a l10n world as it does in shipcracker?
- scope chance of combat vacsuit on independent traders starting pawns possible?
- smuggler's den storeroom, armory and workshop doodads use ancient vanilla defs instead of cleaner maintained-look VGE variants. control room consoles too?
- refactor airlock defenses to appear similar to VGE2s?
- consider mechs/sentries/lords?
- Enable vault access needn't depend on custom mapgen, disables vault quest rewards on smugglers den quest
- VGE2 balance reset button (S18): second reset button shown only under VGE2, resets the settings that act on that encounter to VE-tuned defaults (best effort)

## Tidyup

- script a test to nail the exact wealth distributions of vanilla trader stock generators to determine whether our caps are tuned okay
- cargo vault cap: hold the spawned subset constant across relocks. Today relock merges the leftovers back into stock and the next hack re-caps the whole pool, so a capped vault tops back up from never-shown stock (fine as a crash fix; total lootable is unchanged). Fix = per-settlement record of the first hack's chosen entries (SettlementStockCache or world comp), later hacks spawn only from it, reset on trader rotation. Nuance: return must not merge (merge destroys the absorbed Thing and its ID) or must tag things with a marker comp; works on existing saves since a vault with no record seeds one on its next hack.
- update About.xml/steamworkshop description to note VGE2 compat
- update workshop page images
- recolor vanilla/suggested/default tags in mod settings ui?
- Smoke test with performance analyzer (particularly the post-mapgen spike)
- upgrade BTG TG settlement map size even without VGE2?
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Rare Subroom placement small room off-by-one?

### Upstream

- upstream l10n smoke engine: cross-reference errors raised by a BTG patch (DirectXmlCrossRefLoader stack, no mod frame) classify as `other`, so the 2026-10-15 prestige-armour MayRequire bug passed the gate; attribute unresolved-cross-ref errors by scanning active mods' Defs/Patches for the wanted defName
- upstream l10n: sidecar freshness check only compares label/description, so an English edit to a nested field (quest rulesStrings) never forces a regen
- upstream missing faction check on Building_AndroidStand.CannotUseNowReason(Pawn)

## Other mod integration

- Investigate Simple Warrants fulfilment
- Investigate mod Settlement Visit compatibility
- test `Choose where to land` on den quest site
- `Choose where to land` (independent traders scenario)
- VREA maintenance room
- Knick knacks
- VE Brewing whisky shelf in Captain's quarters?
- Include UMW weapons in unique weapon pools?
- Bind Biotech band nodes?
