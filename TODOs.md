TODOs

- AI defense lords
  - Mech AI
    - Agrihand duty for plant pots?
    - Fake plants if VGE active
    - Test paramedic jump and feeding
  - Civilian AI
    - Abandoned baby warning for cared TG babies
    - No caretaker for crew quarters babies
    - LordJob: no hack/escape on starving (but saw a too-early escape once)
    - Investigate given up mental state for when sheltering civvies find no launchables or adult/pilot becomes downed.
  - Defender AI
    - entrenched defenders hunt within bounding box not structure bounds **(test this next)**
    - If shuttle pad is free, reinforcements land in shuttle?
    - Split BTG_ResupplyDropArrived key message by aid type

- Review mod settings page layout
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Narrow PlanetTile.LayerDef patch: the getter is extremely hot, so patching it taxes every access
  - patch only the caravan-formation/world-pathfinding methods that consult canFormCaravans for friendly TG space tiles instead
- Rare Subroom placement small room off-by-one?
- Investigate threat scaling of settlement defender group generation, seems a little low?
- Bind band nodes?

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
