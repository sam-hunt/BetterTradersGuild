using System.Reflection;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    // Single owner for reflection against RimWorld's private CompHackable state
    // (hacked / progress). BTG flips this state directly to lock/unlock cargo-vault
    // hatches and pre-unlock crew-quarters doors without re-running the hack minigame.
    public static class CompHackableReflection
    {
        // Private bool hacked field.
        public static readonly FieldInfo HackedField = typeof(CompHackable)
            .GetField("hacked", BindingFlags.NonPublic | BindingFlags.Instance);

        // Private float progress field.
        public static readonly FieldInfo ProgressField = typeof(CompHackable)
            .GetField("progress", BindingFlags.NonPublic | BindingFlags.Instance);

        // Sets a hackable's locked/unlocked state directly. Returns false (no-op) if the
        // component is null or the reflection could not be resolved.
        public static bool TrySetHackedState(CompHackable hackable, bool hacked, float progress)
        {
            if (hackable == null || HackedField == null || ProgressField == null)
                return false;

            HackedField.SetValue(hackable, hacked);
            ProgressField.SetValue(hackable, progress);
            return true;
        }

        // Logs a targeted error for any member that failed to resolve. Called once at startup
        // from ReflectionVerification.VerifyAll.
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
