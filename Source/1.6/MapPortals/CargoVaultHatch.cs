using System.Collections.Generic;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // Custom MapPortal class for the cargo vault hatch.
    //
    // Unlike AncientHatch, this class does NOT add extra GenSteps to the pocket map.
    // All generation is controlled entirely by the MapGeneratorDef (BTG_CargoVault)
    // and its specified GenSteps.
    //
    // This prevents vanilla's AncientStockpile generation from interfering with
    // our custom BTG_CargoVault GenStep.
    //
    // Additionally, this class handles auto-relock when the settlement map unloads
    // by overriding DeSpawn to clean up the pocket map and reset hackable state.
    public class CargoVaultHatch : MapPortal
    {
        // Override GetExtraGenSteps to return empty - we don't want any extra steps.
        // All pocket map generation is handled by BTG_CargoVault GenStep in our MapGeneratorDef
        // specified in the MapGeneratorDef.
        protected override IEnumerable<GenStepWithParams> GetExtraGenSteps()
        {
            yield break;
        }

        // Override IsEnterable to check CompHackable status before allowing entry.
        // This prevents pawns from entering the vault before hacking is complete.
        // Mirrors the behavior of vanilla AncientHatch.
        public override bool IsEnterable(out string reason)
        {
            CompHackable hackable = this.GetComp<CompHackable>();
            if (hackable != null && !hackable.IsHacked)
            {
                reason = "Locked".Translate();
                return false;
            }
            return base.IsEnterable(out reason);
        }

        // Override GetGizmos to hide the "View pocket map" gizmo when the hatch is locked.
        // The base MapPortal class provides a gizmo to view/select the pocket map, but this
        // should only be visible when the hatch has been hacked (unlocked).
        public override IEnumerable<Gizmo> GetGizmos()
        {
            CompHackable hackable = this.GetComp<CompHackable>();
            bool isLocked = hackable != null && !hackable.IsHacked;

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                // Filter out the "View" gizmo when locked
                // The base MapPortal class provides SelectPocketMapTileGizmo which shows as "View [label]"
                if (isLocked && gizmo is Command_Action command)
                {
                    // The view gizmo typically has a label starting with "View" or contains the portal label
                    // Check if this is the pocket map viewing gizmo by checking if pocket map exists
                    // and the gizmo label suggests it's for viewing
                    if (command.defaultLabel != null &&
                        (command.defaultLabel.Contains("View") || command.defaultLabel.Contains(this.Label)))
                    {
                        // Skip this gizmo when locked
                        continue;
                    }
                }

                yield return gizmo;
            }
        }

        // Override DeSpawn to clean up the pocket map when the settlement map unloads.
        // This implements the auto-relock behavior: when the player leaves the settlement,
        // any pocket map is cleaned up and the hatch returns to its locked state.
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            // Before despawning, clean up pocket map and reset hackable state
            CleanupPocketMap();

            base.DeSpawn(mode);
        }

        // Cleans up the pocket map by returning items to stock and destroying the map.
        // Also resets the hackable state so the hatch will be locked on next visit.
        private void CleanupPocketMap()
        {
            Map pocketMap = this.PocketMap;
            if (pocketMap == null)
                return;

            // Return items to settlement stock
            CargoReturnHelper.ReturnItemsToStock(pocketMap);

            // Destroy pocket map
            PocketMapUtility.DestroyPocketMap(pocketMap);

            // Reset hackable state so hatch is locked on next visit
            ResetHackableState();
        }

        // Resets the CompHackable component to its initial locked state.
        // Uses reflection to access private fields.
        private void ResetHackableState()
        {
            CompHackable hackable = this.GetComp<CompHackable>();
            if (!CompHackableReflection.TrySetHackedState(hackable, hacked: false, progress: 0f))
                Log.Warning("[BTG] CargoVaultHatch.ResetHackableState: Could not reset hackable state (reflection unavailable)");
        }
    }
}
