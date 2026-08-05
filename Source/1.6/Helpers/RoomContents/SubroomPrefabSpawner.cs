using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.RoomContents
{
    // Spawns a hand-placed subroom prefab with the settlement faction applied to its beds
    // and nothing else.
    //
    // Beds must carry the faction: RestUtility.IsValidBedFor rejects any bed whose faction
    // differs from the traveler's, so factionless beds are invisible to FindBedFor - the
    // nursery caretaker could never tuck infants back into cribs (SafePlaceForBaby degraded
    // to "leave it on the floor") and rest givers that reuse FindBedFor never chose beds.
    // Prefabs placed through the XML LayoutRoomDef pipeline already get the faction from
    // vanilla RoomContentsWorker; only these hand-spawned subrooms missed it.
    //
    // The rest of the prefab must stay factionless: the subroom's AncientBlastDoor is meant
    // to be hacked open, and Building_Door.PawnCanOpen refuses hostile-faction doors even
    // after they are unlocked, so a faction door would stay impassable to the player.
    public static class SubroomPrefabSpawner
    {
        public static void SpawnWithFactionBeds(PrefabDef prefab, Map map, IntVec3 pos, Rot4 rot, Faction faction)
        {
            PrefabUtility.SpawnPrefab(prefab, map, pos, rot, null,
                onSpawned: thing =>
                {
                    if (faction != null && thing is Building_Bed)
                        thing.SetFaction(faction);
                });
        }
    }
}
