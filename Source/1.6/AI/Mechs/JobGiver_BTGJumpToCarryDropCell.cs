using RimWorld;
using Verse;

namespace BetterTradersGuild.AI.Mechs
{
    // Longjump-toward-drop-cell think node for the floor-rescue carry; wired by the
    // ThinkTree_MechConstant.xml patch to BTG_CarryToMedbayFloor's TargetB. Vanilla
    // JobGiver_AIJumpToJobRescueTarget rejects bare-cell targets (it accepts only a
    // pawn, or a bed while carrying one), so the cell-targeted drop leg needs this
    // subclass. The carrying gate mirrors vanilla's bed case: jumping toward the
    // drop cell before the casualty is picked up would burn a launcher charge on a
    // flight the mech immediately walks back from.
    public class JobGiver_BTGJumpToCarryDropCell : JobGiver_AIJumpToJobTarget
    {
        public override bool CanJumpToTarget(Pawn pawn, LocalTargetInfo target)
        {
            return base.CanJumpToTarget(pawn, target) && pawn.carryTracker?.CarriedThing is Pawn;
        }
    }
}
