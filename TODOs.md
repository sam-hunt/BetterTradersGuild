TODOs

Cleanup

- Investigate report that shuttles attacks are allowed without signal jammer
- Investigate threat scaling of settlement defender group generation, seems a little low?
- Rare Subroom placement small room off-by-one?
- Refactor subroom packing and subroom calculator use common centering derived from rect bounds, same as waste filler
- Narrow PlanetTile.LayerDef patch: the getter is extremely hot, so patching it taxes every access
  - patch only the caravan-formation/world-pathfinding methods that consult canFormCaravans for friendly TG space tiles instead
- Enable shuttle attacks once hostiles cleared?
- Split ModSettings per AAH pattern
- Remove unused language folders, contributers typically release a dependent mod to implement this instead
- Landing pad pipe extenders can fail to connect if they hit NarrowHalls
- Port runtime reflection safety check pattern from UMW/AAH

Features?

- AI defense lords
  - Foraged packaged meal pallets drop unforbidden meals
  - Check forager hacker node checks door isn't already hacked
  - New lord for sheltering civilians, e.g. feed infants? flee on nursery hack?
  - New lord for paramedic to tend defender wounds/restore manipulation via surgery?
  - Also drop reinforcements on resupply meal drops

- Smuggler nest elimination quest with TG settlement mapgen
  `/resume "smuggler-den-quest-btg"`

- Way more backstories?!

- Investigate mod Settlement Visit compatibility
- Investigate Simple Warrants fulfilment

- Add trade/equivalence-focused storyteller?
- Mod integration: VREA maintenance room
- Mod integration: Choose where to land (independent traders scenario)
- Mod integration: VGE Faux plants in rooms/crew quarters customizations
- Mod integration: Knick knacks
- Mod integration: trader ships shuttles texture option?
- Mod integration: VE Brewing whisky shelf in Captain's quarters?
- Mod integration: Include UMW weapons in unique weapon pools?
