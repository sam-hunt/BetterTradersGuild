using Verse;

namespace BetterTradersGuild.MapComponents
{
    // Per-map cooldown gate for defender comms-console food resupply drops (the
    // last-resort hunger escalation past the in-structure forager). Keeps the whole
    // garrison from each radioing in a drop the moment they go hungry.
    //
    // State is deliberately minimal (the "lean scribe" constraint): just the tick of
    // the last drop. There is no per-map cap on total drops - the drop size scales with
    // the number of surviving humanlike defenders (see ResupplyDropUtility), so a siege
    // that whittles the garrison down naturally shrinks each resupply to nothing.
    // JobGiver_BTGCallResupply reads CanResupplyNow as a coarse pre-filter;
    // the JobDriver re-reads it at job COMPLETION and only the first caller to finish calls
    // RecordResupply and drops - later parallel callers (on other consoles)
    // find the cooldown active and abort. The check-then-record is race-free because toil
    // initActions never run concurrently (single-threaded tick loop).
    public class ResupplyDropTracker : MapComponent
    {
        private const int TicksPerHour = 2500;

        // -1 = no resupply has ever happened on this map, so the first call is allowed
        // immediately (a freshly besieged garrison shouldn't have to wait out a cooldown).
        private int lastResupplyTick = -1;

        public ResupplyDropTracker(Map map) : base(map) { }

        // True if a defender may call in a resupply right now: no drop yet, or the
        // cooldown (ModSettings, in hours) has elapsed since the last one.
        public bool CanResupplyNow
        {
            get
            {
                if (lastResupplyTick < 0)
                    return true;
                int cooldownTicks = BetterTradersGuildMod.Settings.resupplyCooldownHours * TicksPerHour;
                return Find.TickManager.TicksGame - lastResupplyTick >= cooldownTicks;
            }
        }

        // Records that a resupply drop has landed, starting the cooldown.
        public void RecordResupply()
        {
            lastResupplyTick = Find.TickManager.TicksGame;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastResupplyTick, "lastResupplyTick", -1);
        }
    }
}
