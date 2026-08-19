using RimWorld;

namespace BetterTradersGuild.DefRefs
{
    // Centralized QuestScriptDef references.
    [DefOf]
    public static class QuestScripts
    {
        public static QuestScriptDef BTG_SmugglersDen;
        public static QuestScriptDef BTG_TradeRequest;

        static QuestScripts() => DefOfHelper.EnsureInitializedInCtor(typeof(QuestScripts));
    }
}
