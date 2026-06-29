TODOs

- Review mod settings page layout
- Enable shuttle attacks once hostiles cleared?
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Narrow PlanetTile.LayerDef patch: the getter is extremely hot, so patching it taxes every access
  - patch only the caravan-formation/world-pathfinding methods that consult canFormCaravans for friendly TG space tiles instead
- Rare Subroom placement small room off-by-one?
- Investigate threat scaling of settlement defender group generation, seems a little low?
- Bind band nodes?
- Crew bedroom animals should have names

- AI defense lords
  - Also drop reinforcements on resupply meal drops (or land in shuttle if pad is free)
  - Test mech ai doesn't load without biotech
  - Test paramedic rescue AI, civilian ai
  - Paramedic rescue jump support
  - Investigate given up mental state when sheltering civvies find no launchables or adult/pilot becomes downed.

- Smuggler nest elimination quest with TG settlement mapgen
  `/resume "smuggler-den-quest-btg"`

- Way more backstories?!

- Investigate mod Settlement Visit compatibility
- Investigate Simple Warrants fulfilment
- Investigate report that shuttles attacks are allowed without signal jammer? (can't seem to repro)

- Add trade/equivalence-focused storyteller?
- Mod integration: VREA maintenance room
- Mod integration: Choose where to land (independent traders scenario)
- Mod integration: VGE Faux plants in rooms/crew quarters customizations
- Mod integration: Knick knacks
- Mod integration: trader ships shuttles texture option?
- Mod integration: VE Brewing whisky shelf in Captain's quarters?
- Mod integration: Include UMW weapons in unique weapon pools?
