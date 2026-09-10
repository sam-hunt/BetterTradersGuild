TODOs

claude --add-dir ../VanillaExpanded/VanillaGravshipExpanded2 --add-dir ../VanillaExpanded/VanillaGravshipExpanded --add-dir ../VanillaExpanded/VanillaExpandedFramework

- cap trade vault spawned stacks at 200
- update workshop page images
- ensure smugglers guild vault hatch/quest reward icon uses new vault hatch textures?
- recolor vanilla/suggested/default tags in mod settings ui?
- Smoke test with performance analyzer (particularly the post-mapgen spike)
- test CWTL on den quest site
- sentry drone presence 5% increments
- Investigate Simple Warrants fulfilment

- VGE2 integration (working doc + tracker: Docs/VGE2_INTEGRATION.md)
  - visually test the VGE2_StationSystemRack swap in BTG_ServerRacks_Subroom (3x1 rack placed rotated to keep the 1x3 footprint; check facing and wall overlap)
  - play-test BTG_SowPlantGrowers on both paths:
    - BTG (settlement + den): room rules land (greenhouse rice shared / pots varied+young, medbay healroot+roses, commanders roses, crew quarters varied, other rooms one random decorative); no 'Plant growers' log warnings
    - BTG+VGE2: every station basin sown and powered; also the pending S6/S10 hatch/stands/conduit checks
  - eyeball BTG_StockOutfitStands on all three paths (deferred, batch with the above):
    - BTG settlement: armory stands all marine armour; airlock stands half vacsuit/half bare; crew quarters stands a mixed wardrobe (synthread shirt+pants, marine, vacsuit, recon, panthera body strap); no stray empty stand dressed unexpectedly
    - smuggler's den: same rooms; with VGE2 active every armory stand shows a VGE combat vacsuit instead of marine armour (LayoutRoomDef_Armory_VGE2.xml), and the den has several armories
    - BTG+VGE2 settlement: half the station's 14 steel stands hold a vacsuit, faction-tinted
  - refactor airlock defenses to appear similar to VGE2s?
  - map which of our mod settings affect VGE2 mapgen
  - author/patch BTG side/corner prefabs
  - consider mechs/sentries/lords
  - allow optionally
  - upgrade BTG TG settlement map size even without VGE2?
  - Enable vault access needn't depend on custom mapgen
  - Investigate/Activate VGE Gauss cannon code/other VGE TG mechanics
  - VGE2 TG/Salvager Shuttle texture replacement
  - Surface one-time mapgen options if VGE2 and BTG mapgen are both enabled:
    - Use BTG every time
    - Use VGE every time
    - Something in between
    - Ask every time
  - Defender overhaul opt-out toggle: spec ready in Docs/Specs/SPEC-defender-overhaul-toggle.md (settings-gated PatchOperationToggledSequence; lets players revert pawnkind rebalance + garrison weights to vanilla for VGE mapgen)

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

- upstream l10n: sidecar freshness check only compares label/description, so an
  English edit to a nested field (quest rulesStrings) never forces a regen
- upstream missing dynamic gene weapon texture for aptitude for unique weapon x
- upstream missing faction check on Building_AndroidStand.CannotUseNowReason(Pawn)
