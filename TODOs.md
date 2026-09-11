TODOs

claude --add-dir ../VanillaExpanded/VanillaGravshipExpanded2 --add-dir ../VanillaExpanded/VanillaGravshipExpanded --add-dir ../VanillaExpanded/VanillaExpandedFramework

- cap trade vault spawned stacks at 200
- Investigate Simple Warrants fulfilment
- update workshop page images
- revert smugglers guild vault hatch quest reward icons to use old/vanilla vault hatch textures. our newer clean ones are difficult to make out when it's scaled down to the size the quest dialog does.
- recolor vanilla/suggested/default tags in mod settings ui?
- Smoke test with performance analyzer (particularly the post-mapgen spike)
- test CWTL on den quest site
- sentry drone presence 5% increments (seems unrounded?)
- upgrade BTG TG settlement map size even without VGE2?

- VGE2 integration (working doc + tracker: Docs/VGE2_INTEGRATION.md)
  - combat vacsuits patch for pawnkinds (incl salvagers on smuggler's den?)
  - smuggler's den shuttle same treatment as guild shuttle but with salvager dropship
  - smuggler's den storeroom, armory and workshop doodads. control room consoles?
  - refactor airlock defenses to appear similar to VGE2s?
  - map which of our mod settings affect VGE2 mapgen — surveyed 2026-09-11: on VGE2 TG settlements only useCustomLayouts, enableCargoVault and the life-support power setting act; the whole Garrison & Combat section (entrenched defenders, resupply, threat scaling, sentry drones, security defeat fraction) is den-only there, but the EN descriptions still say custom-layout settlements
    - release gate: reword those EN descriptions to carve out VGE2 settlements (batch with S7 and the stale-translation pass)
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
- Mod integration: trader ships shuttles texture option?
- Mod integration: VE Brewing whisky shelf in Captain's quarters?
- Mod integration: Include UMW weapons in unique weapon pools?

- upstream l10n: sidecar freshness check only compares label/description, so an English edit to a nested field (quest rulesStrings) never forces a regen
- upstream missing faction check on Building_AndroidStand.CannotUseNowReason(Pawn)
