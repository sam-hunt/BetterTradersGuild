using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    /// <summary>
    /// Single owner for reflection against RimWorld's private <see cref="CompHackable"/> state
    /// (<c>hacked</c> / <c>progress</c>). BTG flips this state directly to lock/unlock cargo-vault
    /// hatches and pre-unlock crew-quarters doors without re-running the hack minigame.
    /// </summary>
    public static class CompHackableReflection
    {
        /// <summary>Private <c>bool hacked</c> field.</summary>
        public static readonly FieldInfo HackedField = typeof(CompHackable)
            .GetField("hacked", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>Private <c>float progress</c> field.</summary>
        public static readonly FieldInfo ProgressField = typeof(CompHackable)
            .GetField("progress", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Sets a hackable's locked/unlocked state directly. Returns false (no-op) if the
        /// component is null or the reflection could not be resolved.
        /// </summary>
        public static bool TrySetHackedState(CompHackable hackable, bool hacked, float progress)
        {
            if (hackable == null || HackedField == null || ProgressField == null)
                return false;

            HackedField.SetValue(hackable, hacked);
            ProgressField.SetValue(hackable, progress);
            return true;
        }

        /// <summary>
        /// Logs a targeted error for any member that failed to resolve. Called once at startup
        /// from <see cref="ReflectionVerification.VerifyAll"/>.
        /// </summary>
        public static void VerifyReflection()
        {
            if (HackedField == null)
                Log.Error("[Better Traders Guild] CompHackable.hacked field not found via reflection; "
                    + "cargo-vault relocking and pre-unlocked crew-quarters doors will not work. RimWorld API may have changed.");
            if (ProgressField == null)
                Log.Error("[Better Traders Guild] CompHackable.progress field not found via reflection; "
                    + "cargo-vault relocking and pre-unlocked crew-quarters doors will not work. RimWorld API may have changed.");
        }
    }
}
