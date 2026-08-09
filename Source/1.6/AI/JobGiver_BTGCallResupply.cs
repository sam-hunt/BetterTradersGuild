using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.MapComponents;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.AI
{
    // Last-resort hunger escalation for bounded defenders: a starving defender walks to a
    // powered in-structure comms console and calls in resupply - a survival-meal cargo-pod
    // drop and, optionally, a reinforcement raid (ResupplyRaidUtility). Both come from the
    // garrison's own off-map network (the wider guild for settlements, the Salvagers
    // faction for the smugglers den), so when that network is gone - last guild settlement
    // standing, Salvagers eliminated - the call is never made: no one is left to answer.
    //
    // Sits BELOW the forager (JobGiver_BTGForageInStructure) in the duty think tree, so it
    // only fires once every in-structure food source - carried rations, floor items, meal
    // pallets, paste taps - has run dry. Gated one rung past the forager's UrgentlyHungry
    // (this node requires Starving), so a defender exhausts real food before radioing out.
    //
    // Everything stays inside the structure: the console is found within the rect union and
    // the drop (ResupplyDropUtility) lands in an in-structure room, so a player can't bait a
    // defender out by sieging. The two call outcomes are independent, so the job is only
    // worth giving when at least one is currently possible: a landable drop cell, OR a raid
    // that can actually fire (ResupplyRaidUtility.CanTriggerNow) - the raid uses vanilla
    // EdgeDrop and needs no in-structure cell of its own. The per-map cooldown
    // (ResupplyDropTracker) is the real rate limit, and it is enforced authoritatively at job
    // COMPLETION (in the JobDriver), not here - several consoles can exist, so multiple
    // defenders may call in parallel, and only the first to finish records the cooldown
    // while the rest abort. The CanResupplyNow check below is just a coarse filter so pawns
    // don't path to a console while clearly on cooldown. Mechs never reach this node (no
    // food need).
    public class JobGiver_BTGCallResupply : ThinkNode_JobGiver
    {
        public HungerCategory minCategory = HungerCategory.Starving;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_BTGCallResupply)base.DeepCopy(resolve);
            copy.minCategory = minCategory;
            return copy;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            // Behavior disabled, or pawn isn't starving / can't operate a console.
            if (!BetterTradersGuildMod.Settings.enableResupply
                || BetterTradersGuildMod.Settings.resupplyMealsPerDefender <= 0)
                return null;
            Need_Food need = pawn.needs?.food;
            if (need == null || (int)need.CurCategory < (int)minCategory)
                return null;
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;

            // Coarse cooldown filter so pawns don't bother pathing to a console while clearly
            // on cooldown. The authoritative gate is re-checked at completion (JobDriver), so
            // parallel callers on different consoles resolve first-to-complete-wins there.
            ResupplyDropTracker tracker = pawn.Map.GetComponent<ResupplyDropTracker>();
            if (tracker?.CanResupplyNow != true)
                return null;

            // The garrison's off-map network is what answers the call: if no one is left
            // out there to send meals or reinforcements, the console call is pointless.
            if (!TradersGuildHelper.ResupplyNetworkExists(pawn.Map))
                return null;

            // The two call outcomes are independent (see class remarks), so the call is
            // only worth making if at least one of them can actually happen: somewhere to
            // land the meal pod, or a raid that can currently fire. dropCell may come back
            // IntVec3.Invalid here - that's fine, the JobDriver re-finds it at drop time.
            bool dropCellExists = ResupplyDropUtility.TryFindDropCell(pawn.Map, out IntVec3 dropCell);
            bool raidCurrentlyPossible = ResupplyRaidUtility.CanTriggerNow(pawn.Map, pawn.Faction);
            if (!dropCellExists && !raidCurrentlyPossible)
                return null;

            Building_CommsConsole console = FindUsableConsole(pawn);
            if (console == null)
                return null;

            return JobMaker.MakeJob(Jobs.BTG_CallResupply, console, dropCell);
        }

        // Nearest powered, reachable, unreserved comms console inside the structure.
        // Detected via the vanilla base type, so any comms-console subclass is covered.
        private static Building_CommsConsole FindUsableConsole(Pawn pawn)
        {
            Map map = pawn.Map;
            // Starving => willing to cross deadly terrain to reach the console, matching the
            // forager's danger tolerance at this hunger level.
            Danger maxDanger = Danger.Deadly;
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);

            Building_CommsConsole best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (!(buildings[i] is Building_CommsConsole console))
                    continue;
                if (!StructureBoundsCache.Contains(map, console.Position))
                    continue;
                // CanUseCommsNow = powered AND no solar flare. No colonist/faction gate
                // lives here (those are in the player float-menu path we never touch).
                if (!console.CanUseCommsNow)
                    continue;
                if (console.IsForbidden(pawn))
                    continue;
                if (!pawn.CanReserveAndReach(console, PathEndMode.InteractionCell, maxDanger))
                    continue;

                float distSq = (pawn.Position - console.Position).LengthHorizontalSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = console;
                }
            }
            return best;
        }
    }
}
