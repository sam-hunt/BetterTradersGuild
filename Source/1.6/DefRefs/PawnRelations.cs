using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    // Centralized PawnRelationDef references.
    [DefOf]
    public static class PawnRelations
    {
        public static PawnRelationDef Parent;

        static PawnRelations() => DefOfHelper.EnsureInitializedInCtor(typeof(PawnRelations));
    }
}
