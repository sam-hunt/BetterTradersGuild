using System.Reflection;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Integrations;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.Patches.BuildingAndroidStandPatches
{
    // Harmony patch: VREAndroids.Building_AndroidStand.CannotUseNowReason (optional mod;
    // resolved in VREAIntegration, Prepare silently skips when VREA isn't loaded).
    //
    // Makes Traders Guild-owned android stands (crew-quarters waste fillers) refuse
    // visiting colony androids. VREA's automatic charge job never checks bed faction
    // (rationale and call-graph in VREAIntegration's header), so without this a
    // reachable settlement stand gets auto-used and auto-assigned by player androids.
    // Returning a reason here makes FindStandFor skip the stand and keep scanning,
    // exactly like any other unusable stand, and also covers the float-menu reason.
    //
    // Scoped to Traders Guild-owned stands: stands owned by any other faction (or none)
    // keep VREA's vanilla behavior everywhere else, so the patch can't disturb other
    // mods' maps. The faction test mirrors vanilla RestUtility.IsValidBedFor's traveler
    // gate, with the same prisoner/slave/host-faction escapes.
    //
    // Cold path in practice: runs at job-giver cadence and the hot case (a colony
    // android at home, stand faction == pawn faction) exits on the second compare.
    [HarmonyPatch]
    public static class BuildingAndroidStandCannotUseNowReason
    {
        // DEFERRED: applied by DeferredModPatches.ApplyAll (post-defs), never the ctor-time
        // PatchAll — detouring another mod's method runs its declaring type's cctor, and
        // VREA's cctor contents are outside our control (see DeferredModPatches for the
        // CWTL incident).
        [HarmonyPrepare]
        public static bool Prepare()
        {
            return DeferredModPatches.PassActive && VREAIntegration.Available;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return VREAIntegration.CannotUseNowReasonMethod;
        }

        [HarmonyPostfix]
        public static void Postfix(Thing __instance, Pawn selPawn, ref string __result)
        {
            // Already unusable for a VREA/vanilla reason.
            if (__result != null)
                return;

            Faction standFaction = __instance.Faction;
            if (standFaction == null || standFaction == selPawn.Faction)
                return;

            if (standFaction.def != Factions.TradersGuild)
                return;

            GuestStatus? guest = selPawn.GuestStatus;
            if (guest == GuestStatus.Prisoner || guest == GuestStatus.Slave)
                return;
            if (selPawn.HostFaction == standFaction)
                return;

            // VREA's own generic refusal string, so no new Keyed entry is needed and
            // the reason stays translated wherever VREA is.
            __result = "VREA.CannotUse".Translate();
        }
    }
}
