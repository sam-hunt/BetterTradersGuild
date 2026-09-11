TODOs

claude --add-dir ../VanillaExpanded/VanillaGravshipExpanded2 --add-dir ../VanillaExpanded/VanillaGravshipExpanded --add-dir ../VanillaExpanded/VanillaExpandedFramework

- cap trade vault spawned stacks at 200
- Investigate Simple Warrants fulfilment
- update workshop page images
- ensure smugglers guild vault hatch/quest reward icon uses new vault hatch textures?
- recolor vanilla/suggested/default tags in mod settings ui?
- Smoke test with performance analyzer (particularly the post-mapgen spike)
- test CWTL on den quest site
- sentry drone presence 5% increments (seems unrounded?)
- upgrade BTG TG settlement map size even without VGE2?

- VGE2 integration (working doc + tracker: Docs/VGE2_INTEGRATION.md)
  - visually test the VGE2_StationSystemRack swap in BTG_ServerRacks_Subroom (3x1 rack placed rotated to keep the 1x3 footprint; check facing and wall overlap)
  - play-test BTG_SowPlantGrowers on both paths:
    - BTG (settlement + den): room rules land (greenhouse rice shared / pots varied+young, medbay healroot+roses, commanders roses, crew quarters varied, other rooms one random decorative); no 'Plant growers' log warnings
    - BTG+VGE2: every station basin sown and powered; also the pending S6/S10 hatch/stands/conduit checks
  - eyeball BTG_StockOutfitStands on all three paths (deferred, batch with the above):
    - BTG settlement: armory stands all marine armour; airlock stands half vacsuit/half bare; crew quarters stands a mixed wardrobe (synthread shirt+pants, marine, vacsuit, recon, panthera body strap); no stray empty stand dressed unexpectedly
    - smuggler's den: same rooms; with VGE2 active every armory stand shows a VGE combat vacsuit instead of marine armour (LayoutRoomDef_Armory_VGE2.xml), and the den has several armories
    - BTG+VGE2 settlement: half the station's 14 steel stands hold a vacsuit, faction-tinted
  - eyeball power and pipe nets after the GenStep move (batch with the above): settlement + den one grid, tanks filled, valves closed/unowned, landing pad pipes still join the network; VGE2 station basins/lamps powered
  - refactor airlock defenses to appear similar to VGE2s?
  - map which of our mod settings affect VGE2 mapgen — surveyed 2026-09-11: on VGE2 TG settlements only useCustomLayouts, enableCargoVault and the life-support power setting act; the whole Garrison & Combat section (entrenched defenders, resupply, threat scaling, sentry drones, security defeat fraction) is den-only there, but the EN descriptions still say custom-layout settlements
    - release gate: reword those EN descriptions to carve out VGE2 settlements (batch with S7 and the stale-translation pass)
  - author/patch BTG side/corner prefabs
  - consider mechs/sentries/lords
  - Enable vault access needn't depend on custom mapgen
  - eyeball the den under VGE2 (batch with the above): 2 of 4 corner cannons are live VGE_EnemyGaussCannons owned by the den, jammer stun message on landing, they fire at the parked ship once it ends
  - ~~Investigate other VGE TG mechanic interactions: defeat trigger, vault stock preservation~~ surveyed 2026-09-11, no active bug; outcomes in Docs/VGE2_INTEGRATION.md section 7
  - test S9 before release: hack a locked shuttle on a VGE2 station (lockout fires; promotion keeps cell/rotation/HP, player-owned, launches; TG turns hostile); guild shuttle shows in the scenario editor shuttle picker; an Independent Traders start lands in it unpainted
  - optional: den landing pad spawns the unpainted guild shuttle instead of the painted PassengerShuttle under VGE2
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

- upstream l10n: sidecar freshness check only compares label/description, so an English edit to a nested field (quest rulesStrings) never forces a regen
- upstream missing faction check on Building_AndroidStand.CannotUseNowReason(Pawn)
