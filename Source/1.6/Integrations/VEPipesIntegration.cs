using System;
using System.Reflection;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with Vanilla Expanded Framework's PipeSystem.CompResourceStorage.
    // BTG fills pipe-network tanks on generated maps so stations feel operational on arrival.
    //
    // Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent
    // when VE Pipes isn't installed; one Log.Warning when the type IS present but
    // its API has shifted.
    //
    // Note: the per-tank storageCapacity field lives on the runtime type of the comp's
    // Props object, so it can only be resolved per-instance and stays a runtime-guarded lookup in
    // the consumer. Everything resolvable without an instance is verified here at startup.
    public static class VEPipesIntegration
    {
        private const string CompTypeName = "PipeSystem.CompResourceStorage";
        private const string PropsPropName = "Props";
        private const string AddResourceMethodName = "AddResource";

        // The VE Pipes storage comp type, or null if VE Pipes isn't loaded.
        public static readonly Type CompType;

        // The comp's Props property (declared override, hiding base ThingComp.Props).
        public static readonly PropertyInfo PropsProperty;

        // The comp's AddResource(float) method.
        public static readonly MethodInfo AddResourceMethod;

        // True only when VE Pipes is loaded AND every reflected member resolved.
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

        // Resolves the Props property using DeclaredOnly to avoid an AmbiguousMatchException
        // (CompResourceStorage declares its own Props that hides the base ThingComp.Props), walking
        // up the hierarchy as a fallback.
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
