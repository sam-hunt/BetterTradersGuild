using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

// Static texture field requires this attribute - Unity assets must load on main thread

namespace BetterTradersGuild.Comps
{
    // CompProperties for CompForceClaimable.
    // No configuration needed - just attach to ThingDef via XML patch.
    public class CompProperties_ForceClaimable : CompProperties
    {
        public CompProperties_ForceClaimable()
        {
            compClass = typeof(CompForceClaimable);
        }
    }

    // ThingComp that adds a "Force Claim" gizmo to bypass vanilla claiming restrictions.
    //
    // PURPOSE:
    // Vanilla claiming is blocked map-wide when any hostile pawn is present. This prevents
    // room-by-room progression in BTG settlements where players may secure one area but not
    // the entire station.
    //
    // USAGE:
    // Attach via XML patch to any ThingDef that should be force-claimable:
    // <li Class="BetterTradersGuild.Comps.CompProperties_ForceClaimable"/>
    //
    // VISIBILITY:
    // Gizmo only appears when the building is NOT owned by the player faction.
    // Once claimed, the gizmo disappears (standard ownership applies).
    //
    // DESIGN NOTES:
    // - Designed for VE pipe valves in BTG settlements
    // - Generic enough to work on any building type
    // - No Harmony patches required - uses standard comp gizmo system
    [StaticConstructorOnStartup]
    public class CompForceClaimable : ThingComp
    {
        // Cached reference to the Claim icon texture.
        // Uses vanilla's claim icon for consistency.
        // Loaded in static constructor per RimWorld requirements.
        private static readonly Texture2D ClaimIcon;

        // Static constructor loads texture on main thread during startup.
        // Required by RimWorld for all Unity assets.
        static CompForceClaimable()
        {
            ClaimIcon = ContentFinder<Texture2D>.Get("UI/Designators/Claim", true);
        }

        // Returns the "Force Claim" gizmo when building is not player-owned.
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only show gizmo if not player-owned
            if (parent.Faction == Faction.OfPlayer)
            {
                yield break;
            }

            // Create Force Claim gizmo
            Command_Action forceClaimGizmo = new Command_Action
            {
                defaultLabel = "Force Claim",
                defaultDesc = "Bypass standard claiming rules and immediately claim this building. " +
                              "Normally, claiming is blocked while hostile pawns are anywhere on the map. " +
                              "This allows room-by-room progression through the station.",
                icon = ClaimIcon,
                action = delegate
                {
                    parent.SetFaction(Faction.OfPlayer);
                }
            };

            yield return forceClaimGizmo;
        }
    }
}
