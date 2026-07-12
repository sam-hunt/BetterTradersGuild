TODOs

- Review mod settings page layout
- Can players land shuttles on the destroyed settlement once defenders cleared?
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Narrow PlanetTile.LayerDef patch: the getter is extremely hot, so patching it taxes every access
  - patch only the caravan-formation/world-pathfinding methods that consult canFormCaravans for friendly TG space tiles instead
- Rare Subroom placement small room off-by-one?
- Investigate threat scaling of settlement defender group generation, seems a little low?
- Bind band nodes?

- AI defense lords
  - Test mech ai doesn't error on load without biotech
  - Test paramedic rescue AI, civilian ai, entrenched defender combat ai
  - Paramedics rescue jump support
  - Investigate given up mental state for when sheltering civvies find no launchables or adult/pilot becomes downed.

- If shuttle pad is free, reinforcements land in shuttle?

- Smuggler nest elimination quest with TG settlement mapgen
  `/resume "smuggler-den-quest-btg"`

- Layered Atmosphere and Orbit incompatibility: player reported TG bases didn't spawn

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
