using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterTradersGuild.AI
{
    // The reinforcement half of the defender comms-console resupply: when a starving
    // garrison calls in on a comms console (JobDriver_BTGCallResupply), the same radio call
    // can also summon a drop-pod raid of settlement reinforcements, gated on the
    // resupplyTriggersRaid setting.
    //
    // We delegate to the vanilla RaidEnemy incident worker for all the idiomatic raid
    // machinery — Combat pawn-group generation, the LordJob_AssaultColony raid lord (a
    // normal hunt-the-colony lord, NOT the settlement's bounded DefendStructure/DefendBase
    // AI), the default threat-point calculation, and the ThreatBig red letter that pauses
    // the game per the player's settings. We only override what the worker resolves wrongly
    // on our maps:
    //
    //   * Faction — forced to the garrison's own faction. The worker keeps a pre-set
    //     faction only when it is hostile to the player; a non-hostile one is silently
    //     swapped for a different random hostile faction (and would arrive via
    //     LordJob_AssistColony as allies). We guard on hostility up front, so the
    //     reinforcements are always the settlement's own troops assaulting the player, and
    //     no raid fires during a peaceful visit where defenders can simply go hungry.
    //
    // Two variants, chosen by a small on-top chance:
    //
    //   * Standard (common) — EdgeDrop. We leave parms.spawnCenter unset so the vanilla
    //     EdgeDrop worker resolves it via FindRaidDropCenterDistant. On these orbital maps
    //     that lands on the open outer platform: the ring/pads around the structure are
    //     unroofed MetalTile (walkable, not vacuum-exposed, outside the room bounds), and
    //     the finder only accepts unroofed, outdoor, non-vacuum cells reachable to the
    //     base. So reinforcements set down on the platform without punching a structure
    //     roof, then advance inward. Full points.
    //
    //   * "Right on top of you" (rare) — CenterDrop. Here vanilla would center on the
    //     settlement's OWN pawns rather than the player, so we pre-set the spawn center next
    //     to a player pawn; that lands (and punches the roof) right over the player. Its
    //     0.5x points curve makes it a smaller group.
    internal static class ResupplyRaidUtility
    {
        // Fraction of successful resupply calls that summon the rarer center-drop variant
        // landing on top of the player instead of the standard edge-drop reinforcements.
        private const float OnTopOfYouChance = 0.15f;

        // Fires the reinforcement raid if enabled and the settlement is currently hostile.
        // Returns true only when the raid was actually initiated (so the caller can share
        // one resupply cooldown between this and the meal drop).
        public static bool TryTriggerReinforcementRaid(Map map, Faction faction)
        {
            if (!BetterTradersGuildMod.Settings.resupplyTriggersRaid)
                return false;
            if (map == null || faction == null)
                return false;

            // Only the settlement's own, currently-hostile faction reinforces (see class
            // remarks): a non-hostile faction would be swapped out by the raid worker, and a
            // peaceful visit should never spawn a raid.
            if (!faction.HostileTo(Faction.OfPlayer))
                return false;

            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = faction,
                points = StorytellerUtility.DefaultThreatPointsNow(map),
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                forced = true,
            };

            if (Rand.Chance(OnTopOfYouChance) && TryFindCellNearPlayer(map, out IntVec3 nearPlayer))
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                parms.spawnCenter = nearPlayer;
            }
            else
            {
                // Leave spawnCenter unset - the EdgeDrop worker resolves it onto the open
                // outer platform (see class remarks).
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
            }

            return IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
        }

        // Picks a landable cell next to a spawned player pawn for the center-drop variant,
        // preferring the player's colonists. Returns false (falling the caller back to the
        // standard variant) when the player has no spawned pawns or no nearby cell can take
        // a pod - the pod cell finder honours vacuum, thick roofs and blockers.
        private static bool TryFindCellNearPlayer(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            List<Pawn> anchors = map.mapPawns.FreeColonistsSpawned;
            if (anchors.NullOrEmpty())
                anchors = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            if (anchors.NullOrEmpty())
                return false;

            Pawn anchor = anchors.RandomElement();
            return DropCellFinder.TryFindDropSpotNear(anchor.Position, map, out cell,
                allowFogged: true, canRoofPunch: true, allowIndoors: true);
        }
    }
}
