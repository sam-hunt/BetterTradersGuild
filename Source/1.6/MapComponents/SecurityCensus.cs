using RimWorld;
using Verse;

namespace BetterTradersGuild.MapComponents
{
    // MapComponent recording how much active security (garrison humans + sentry
    // drones) a BTG-managed map generated with. SecurityDefeatUtility compares the
    // live count against this baseline to decide when the defense has collapsed.
    //
    // Latched in MapGenerated, which runs exactly once per map, after every GenStep
    // (including the linkWithSite defender steps, orders 700/705, so the census sees
    // the full garrison no matter which GenStep spawned it) - and never on save
    // load, so a mid-raid load can't re-baseline to a depleted garrison. Stays -1
    // (unknown) on non-BTG maps and on maps generated before this component
    // existed; the evaluator then falls back to requiring zero active security.
    public class SecurityCensus : MapComponent
    {
        public const int Unknown = -1;

        private int initialSecurity = Unknown;

        public int InitialSecurity => initialSecurity;

        public SecurityCensus(Map map) : base(map)
        {
        }

        public override void MapGenerated()
        {
            Faction owner = TradersGuildHelper.GetBTGMapFaction(map);
            if (owner == null)
                return;

            initialSecurity = SecurityDefeatUtility.CountActiveSecurity(map, owner);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialSecurity, "initialSecurity", Unknown);
        }
    }
}
