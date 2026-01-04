using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized PawnRelationDef references.
    /// </summary>
    [DefOf]
    public static class PawnRelations
    {
        public static PawnRelationDef Parent;

        static PawnRelations() => DefOfHelper.EnsureInitializedInCtor(typeof(PawnRelations));
    }
}
