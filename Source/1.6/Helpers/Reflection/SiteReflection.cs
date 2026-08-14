using System.Reflection;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    // Single owner for reflection against Site's private defeat latch.
    //
    // allEnemiesDefeatedSignalSent is set exactly once - on BTG dens by the
    // SiteCheckAllEnemiesDefeated prefix when the security-collapse threshold is met,
    // elsewhere by vanilla Site.CheckAllEnemiesDefeated - and it is scribed with the
    // save. That makes it the Site counterpart of the settlement DestroyedSettlement
    // reparent: a per-map, save-durable, engine-owned "this garrison is defeated" truth
    // that needs no defeat-time hook of our own.
    public static class SiteReflection
    {
        public static readonly FieldInfo AllEnemiesDefeatedSignalSentField = typeof(Site)
            .GetField("allEnemiesDefeatedSignalSent", BindingFlags.NonPublic | BindingFlags.Instance);

        // Whether the site has latched its all-enemies-defeated signal. Returns false
        // (defeat never detected) when the site is null or the reflection failed to
        // resolve, so callers degrade to pre-defeat behavior.
        public static bool AllEnemiesDefeatedSent(Site site)
        {
            if (site == null || AllEnemiesDefeatedSignalSentField == null)
                return false;

            return (bool)AllEnemiesDefeatedSignalSentField.GetValue(site);
        }

        // Latches the site's defeat signal; called by the SiteCheckAllEnemiesDefeated
        // postfix at the moment BTG's threshold fires. Vanilla only ever writes true, so
        // no value parameter. No-op when reflection failed - that patch then stays
        // passive rather than half-applying (send without latch would re-fire the quest
        // signal every world-object tick).
        public static void SetAllEnemiesDefeatedSent(Site site)
        {
            if (site == null || AllEnemiesDefeatedSignalSentField == null)
                return;

            AllEnemiesDefeatedSignalSentField.SetValue(site, true);
        }

        // Logs a targeted error if the member failed to resolve. Called once at startup
        // from ReflectionVerification.VerifyAll.
        public static void VerifyReflection()
        {
            if (AllEnemiesDefeatedSignalSentField == null)
                Log.Error("[Better Traders Guild] Site.allEnemiesDefeatedSignalSent field not found via reflection; "
                    + "smugglers den defeat falls back to vanilla's turret-gated check, and even once vanilla "
                    + "latches it the defenders will not stand down "
                    + "(no abandon-ship phase, the threat gate stays armed, and survivor name labels stay hostile). "
                    + "RimWorld API may have changed.");
        }
    }
}
