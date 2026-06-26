using System.Reflection;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Helpers.Reflection
{
    /// <summary>
    /// Single owner for the reflection BTG performs against RimWorld's private
    /// <see cref="Settlement_TraderTracker"/> members. The trader-rotation and cargo-vault
    /// systems all read/write the same three members, so they are resolved once here rather
    /// than re-declared in every consumer (no name can drift apart across files).
    ///
    /// <para>Every consumer null-guards its use, so a missing member degrades to a no-op rather
    /// than throwing; <see cref="VerifyReflection"/> surfaces any drift at startup.</para>
    /// </summary>
    public static class TraderTrackerReflection
    {
        /// <summary>Private <c>ThingOwner&lt;Thing&gt; stock</c> field.</summary>
        public static readonly FieldInfo StockField = typeof(Settlement_TraderTracker)
            .GetField("stock", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>Private <c>int lastStockGenerationTicks</c> field.</summary>
        public static readonly FieldInfo LastStockGenerationTicksField = typeof(Settlement_TraderTracker)
            .GetField("lastStockGenerationTicks", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>Private parameterless <c>RegenerateStock()</c> method.</summary>
        public static readonly MethodInfo RegenerateStockMethod = typeof(Settlement_TraderTracker)
            .GetMethod("RegenerateStock", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Logs a targeted error for any member that failed to resolve. Called once at startup
        /// from <see cref="ReflectionVerification.VerifyAll"/>.
        /// </summary>
        public static void VerifyReflection()
        {
            if (StockField == null)
                Log.Error("[Better Traders Guild] Settlement_TraderTracker.stock field not found via reflection; "
                    + "trader stock freezing and cargo-vault inventory will not work. RimWorld API may have changed.");
            if (LastStockGenerationTicksField == null)
                Log.Error("[Better Traders Guild] Settlement_TraderTracker.lastStockGenerationTicks field not found via reflection; "
                    + "trader rotation scheduling and preview/visit consistency will break. RimWorld API may have changed.");
            if (RegenerateStockMethod == null)
                Log.Error("[Better Traders Guild] Settlement_TraderTracker.RegenerateStock() method not found via reflection; "
                    + "settlement stock will not be generated on map entry. RimWorld API may have changed.");
        }
    }
}
