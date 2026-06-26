using System;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Integrations
{
    /// <summary>
    /// Optional integration with Vanilla Expanded Framework's <c>PipeSystem.CompResourceStorage</c>.
    /// BTG fills pipe-network tanks on generated maps so stations feel operational on arrival.
    ///
    /// <para>Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent
    /// when VE Pipes isn't installed; one <see cref="Log.Warning"/> when the type IS present but
    /// its API has shifted.</para>
    ///
    /// <para>Note: the per-tank <c>storageCapacity</c> field lives on the runtime type of the comp's
    /// Props object, so it can only be resolved per-instance and stays a runtime-guarded lookup in
    /// the consumer. Everything resolvable without an instance is verified here at startup.</para>
    /// </summary>
    public static class VEPipesIntegration
    {
        private const string CompTypeName = "PipeSystem.CompResourceStorage";
        private const string PropsPropName = "Props";
        private const string AddResourceMethodName = "AddResource";

        /// <summary>The VE Pipes storage comp type, or null if VE Pipes isn't loaded.</summary>
        public static readonly Type CompType;

        /// <summary>The comp's <c>Props</c> property (declared override, hiding base ThingComp.Props).</summary>
        public static readonly PropertyInfo PropsProperty;

        /// <summary>The comp's <c>AddResource(float)</c> method.</summary>
        public static readonly MethodInfo AddResourceMethod;

        /// <summary>True only when VE Pipes is loaded AND every reflected member resolved.</summary>
        public static bool Available =>
            CompType != null && PropsProperty != null && AddResourceMethod != null;

        static VEPipesIntegration()
        {
            try
            {
                CompType = GenTypes.GetTypeInAnyAssembly(CompTypeName);
                if (CompType == null)
                    return; // VE Pipes not installed — stay silent.

                PropsProperty = ResolveProps(CompType);

                // Resolve the exact (float) overload so a future VE overload can't ambiguate it.
                AddResourceMethod = CompType.GetMethod(AddResourceMethodName,
                    BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] VE Pipes reflection failed (generated pipe-network "
                    + "tanks will spawn empty): " + ex);
                return;
            }

            if (CompType != null && !Available)
            {
                Log.Warning("[Better Traders Guild] VE Pipes (PipeSystem) active but its CompResourceStorage "
                    + "API (" + PropsPropName + " / " + AddResourceMethodName + "(float)) could not be resolved; "
                    + "generated pipe-network tanks will spawn empty. VE Framework API may have changed.");
            }
        }

        /// <summary>
        /// Resolves the <c>Props</c> property using DeclaredOnly to avoid an AmbiguousMatchException
        /// (CompResourceStorage declares its own Props that hides the base ThingComp.Props), walking
        /// up the hierarchy as a fallback.
        /// </summary>
        private static PropertyInfo ResolveProps(Type type)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                PropertyInfo p = t.GetProperty(PropsPropName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (p != null)
                    return p;
            }
            return null;
        }
    }
}
