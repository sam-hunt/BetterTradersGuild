using RimWorld;
using Verse;
using Verse.AI;

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
    // Shooting positions are pinned inside the same rect union. The vanilla
    // JobGiver_AIDefendPoint cast-position request is bounded only by the duty
    // radius (60 cells of baseCenter), which includes a wide ring of vacuum
    // around the structure — so a defender that couldn't hit its (in-structure)
    // target from cover would happily walk out onto the open platform for a
    // firing angle, where the player guns it down. The override reissues the
    // vanilla request with a validator restricting candidate cells to the
    // structure footprint; if no in-structure cell can take the shot, the node
    // yields instead of sending the defender outside.
    //
    // Acquisition inherits vanilla NeedLOSToPawns, so this node only engages
    // targets the pawn can currently see; in-bounds hostiles out of sight are
    // handled by JobGiver_BTGHuntIntrudersInStructure directly below it.
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

        // Mirror of JobGiver_AIDefendPoint.TryFindShootingPosition with the
        // structure-bounds validator added. StructureBoundsCache.Contains is
        // permissive when no bounds are known, so the no-sketch fallback
        // degrades to exactly the vanilla request.
        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
        {
            Thing enemyTarget = pawn.mindState.enemyTarget;
            Verb verb = verbToUse ?? pawn.TryGetAttackVerb(enemyTarget, !pawn.IsColonist);
            if (verb == null)
            {
                dest = IntVec3.Invalid;
                return false;
            }

            Map map = pawn.Map;
            return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
            {
                caster = pawn,
                target = enemyTarget,
                verb = verb,
                maxRangeFromTarget = 9999f,
                locus = (IntVec3)pawn.mindState.duty.focus,
                maxRangeFromLocus = pawn.mindState.duty.radius,
                wantCoverFromTarget = verb.EffectiveRange > 7f,
                validator = c => StructureBoundsCache.Contains(map, c)
            }, out dest);
        }
    }
}
