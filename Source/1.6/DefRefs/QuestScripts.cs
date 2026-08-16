using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    // Centralized QuestScriptDef references.
    [DefOf]
    public static class QuestScripts
    {
        public static QuestScriptDef BTG_SmugglersDen;

        static QuestScripts() => DefOfHelper.EnsureInitializedInCtor(typeof(QuestScripts));
    }
}
