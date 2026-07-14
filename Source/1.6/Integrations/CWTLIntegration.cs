using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Integrations
{
    // Optional integration with "Choose where to land" (CWTL).
    //
    // CWTL ships its own attack arrival action, TransportersArrivalAction_CWTLAttackSettlement, with
    // a private copy of CanAttack that never calls vanilla's. BTG's source-gate on vanilla
    // TransportersArrivalAction_AttackSettlement.CanAttack therefore misses it entirely, leaving
    // CWTL's "Attack X (Specify a landing spot)" option a functional bypass of the signal-jammer
    // attack ban on BOTH the shuttle and transport-pod flows (CWTL's CanAttack returns accepted for
    // a Traders Guild settlement). This integration resolves CWTL's CanAttack so
    // CWTLAttackSettlementCanAttack can postfix it exactly like the vanilla gate - rejecting it at
    // StillValid and tagging the float-menu label so the cosmetic disablers grey it out.
    //
    // Self-reports drift at startup (Pattern B, ported from UniqueWeaponsUnbound): silent when CWTL
    // isn't installed; a single Log.Warning when CWTL IS present but its API has shifted.
    public static class CWTLIntegration
    {
        private const string ActionTypeName = "ChooseWhereToLand.TransportersArrivalAction_CWTLAttackSettlement";
        private const string CanAttackMethodName = "CanAttack";

        // CWTL's attack arrival action type, or null if CWTL isn't loaded.
        public static readonly Type ActionType;

        // CWTL's static CanAttack(IEnumerable<IThingHolder>, Settlement) - the Harmony target.
        public static readonly MethodInfo CanAttackMethod;

        // True only when CWTL is loaded AND its CanAttack resolved.
        public static bool Available => ActionType != null && CanAttackMethod != null;

        static CWTLIntegration()
        {
            try
            {
                ActionType = GenTypes.GetTypeInAnyAssembly(ActionTypeName);
                if (ActionType == null)
                    return; // CWTL not installed — stay silent.

                // Resolve the exact (pods, settlement) overload so a future CWTL overload can't
                // ambiguate it.
                CanAttackMethod = ActionType.GetMethod(CanAttackMethodName,
                    BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(IEnumerable<IThingHolder>), typeof(Settlement) }, null);
            }
            catch (Exception ex)
            {
                Log.Warning("[Better Traders Guild] 'Choose where to land' reflection failed (its transport-pod "
                    + "attack option on Traders Guild settlements will not be blocked): " + ex);
                return;
            }

            // CWTL is present (type resolved) but CanAttack drifted — warn the affected user only.
            if (ActionType != null && !Available)
            {
                Log.Warning("[Better Traders Guild] 'Choose where to land' active but its "
                    + ActionTypeName + "." + CanAttackMethodName + "(pods, settlement) could not be resolved; "
                    + "its transport-pod/shuttle attack option on Traders Guild settlements will not be blocked. "
                    + "The mod's API may have changed.");
            }
        }
    }
}
