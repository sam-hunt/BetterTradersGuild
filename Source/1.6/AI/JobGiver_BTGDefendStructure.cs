using RimWorld;
using Verse;

namespace BetterTradersGuild.AI
{
    // Defender JobGiver that filters target acquisition to the union of room
    // rects in the settlement structure layout. Defenders never path outside
    // the structure to pursue intruders; targets outside the union are
    // invisible to the scan, and a target that walks out of bounds is
    // dropped on the next think tick (JobGiver_AIDefendPoint does not set
    // chaseTarget, so UpdateEnemyTarget will release it once
    // ExtraTargetValidator rejects it).
    //
    // Falls back to allowing all targets if the map has no layout sketch
    // (defensive guard against unusual map generation paths).
    public class JobGiver_BTGDefendStructure : JobGiver_AIDefendPoint
    {
        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            if (!base.ExtraTargetValidator(pawn, target))
                return false;

            return StructureBoundsCache.Contains(pawn.Map, target.Position);
        }
    }
}
