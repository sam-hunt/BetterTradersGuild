using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Helpers
{
    // Idempotent world-pawn registration for cargo-vault / stock-cache pawns.
    //
    // Vanilla WorldPawns.PassToWorld hard-errors ("Tried to pass pawn X to world,
    // but it's already here.") and no-ops when the pawn is already registered.
    // BTG's stock pawns are registered as world pawns at generation (KeepForever)
    // and flow through several return / re-stock paths that each want to guarantee
    // registration, so an unguarded PassToWorld spams that error. Guarding on
    // Contains makes the call safe to repeat.
    //
    // By design these pawns stay in the world pawn pool after their parent map is
    // cleaned up: a TG settlement the player left (without destroying) can still be
    // traded with at the world level and must keep showing its stock pawns, and any
    // pawn the player may have seen must persist. The count is small and bounded
    // (TG settlements are spawned once at game start; smuggler's den quests are
    // rare), so leaving them for the game to recycle later via WorldPawnGC is fine.
    public static class WorldPawnRegistrar
    {
        // Registers pawn as a world pawn if it isn't one already. Safe to call
        // repeatedly. The caller must have despawned the pawn first (PassToWorld
        // rejects spawned pawns); this only guards the already-registered case.
        public static void EnsureWorldPawn(Pawn pawn, PawnDiscardDecideMode mode)
        {
            if (pawn == null || Find.WorldPawns.Contains(pawn))
                return;

            Find.WorldPawns.PassToWorld(pawn, mode);
        }
    }
}
