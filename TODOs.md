TODOs

Cleanup

- AI defense lord
  - when all food is exhausted, a defender uses an in-structure powered comms console to call in a cargo-pod food resupply to an in-structure room (pod bay → shuttle bay landing pad → comms center → dining → rec). Resupply-not-assault: keep the all-in assault off the table.
- AI defense lord for sheltering civilians, e.g. feed infants? flee?
- AI lord for paramedic to tend defender wounds/restore manipulation via surgery?
- Investigate report that shuttles attacks are allowed without signal jammer
- Guild base babies get hypothermia
- Investigate threat scaling of settlement defender group generation, seems a little low?
- Rare Subroom placement small room off-by-one?
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Narrow PlanetTile.LayerDef patch: the getter is extremely hot, so patching it taxes every access
  - patch only the caravan-formation/world-pathfinding methods that consult canFormCaravans for friendly TG space tiles instead
- Enable shuttle attacks once hostiles cleared?

Possible future features

- Smuggler nest elimination quest with TG settlement mapgen
  `/resume "smuggler-den-quest-btg"`

- Investigate mod Settlement Visit compatibility
- Investigate Simple Warrants fulfilment
- Timed reinforcement TG raid after 12~24 hours?

- Add trade/equivalence-focused storyteller?
- Mod integration: VREA maintenance room
- Mod integration: Choose where to land (independent traders scenario)
- Mod integration: VGE Faux plants in rooms/crew quarters customizations
- Mod integration: Knick knacks
- Mod integration: trader ships shuttles texture option?
- Mod integration: VE Brewing whisky shelf in Captain's quarters?
- Mod integration: Include UMW weapons in unique weapon pools?
