using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BetterTradersGuild.LordJobs.Civilians
{
    // Drops the baby out of any lord member's arms when that member is mid-bottle-feed, so
    // the job-ending action that follows on the same transition can't chain vanilla's feed
    // finalizer. JobDriver_FeedBaby installs a SetFinalizerJob lambda that is evaluated at
    // job END and returns MakeBringBabyToSafetyJob only while the baby is still in the
    // feeder's arms; Pawn_JobTracker.EndCurrentJob starts that finalizer DIRECTLY (the think
    // tree and the freshly assigned duty are never consulted), which marched a feeder to a
    // crib to retuck the baby right as the family was supposed to bolt for the launchables.
    // Dropping first makes the lambda return null: the baby lands at the feeder's feet,
    // spawned and visible to the escape carry scan, and the carry giver picks it straight
    // back up. Must run BEFORE the job-ending action on its transition (post actions execute
    // in the order added).
    //
    // Strictly feed-scoped on purpose: BTG_CarryBabyToLaunchable sets carryThingAfterJob so
    // an interrupted ferry KEEPS its baby in arms - a blanket drop-carried-babies here would
    // undo that.
    public class TransitionAction_BTGDropMidFeedBabies : TransitionAction
    {
        public override void DoAction(Transition trans)
        {
            List<Pawn> ownedPawns = trans.target.lord.ownedPawns;
            for (int i = 0; i < ownedPawns.Count; i++)
                DropBabyIfMidFeed(ownedPawns[i]);
        }

        // Shared with TransitionAction_BTGEndAdultJobs, whose edges (escape <-> defend, with
        // stranded as a defend source) can also force-end a feed.
        public static void DropBabyIfMidFeed(Pawn pawn)
        {
            if (!(pawn?.jobs?.curDriver is JobDriver_FeedBaby))
                return;
            if (!(pawn.carryTracker?.CarriedThing is Pawn) || !pawn.Spawned)
                return;
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        }
    }
}
