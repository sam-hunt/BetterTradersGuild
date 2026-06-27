using System.Collections.Generic;
using System.Text;
using BetterTradersGuild.Helpers.Reflection;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterTradersGuild.Comps
{
    // Component that allows a hackable portal to be relocked after use.
    // Unlike CompSealable (permanent seal), this allows the hatch to be
    // hacked again, creating a reusable "hack -> loot -> relock" cycle.
    //
    // When relocked:
    // 1. All items in the pocket map are returned to settlement trade inventory
    // 2. The pocket map is destroyed
    // 3. CompHackable state is reset (hacked=false, progress=0)
    //
    // This enables players to raid the cargo vault multiple times, with
    // the vault contents refreshing based on the settlement's trade inventory.
    public class CompRelockable : ThingComp
    {
        public CompProperties_Relockable Props => (CompProperties_Relockable)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only show relock button when hatch is unlocked (hacked)
            CompHackable hackable = parent.GetComp<CompHackable>();
            if (hackable == null || !hackable.IsHacked)
                yield break;

            // Only show when not on player's home map
            if (parent.Map != null && parent.Map.IsPlayerHome)
                yield break;

            yield return new Command_Action
            {
                defaultLabel = Props.relockCommandLabel ?? "BTG_CargoVaultHatch_RelockLabel".Translate(),
                defaultDesc = Props.relockCommandDesc ?? "BTG_CargoVaultHatch_RelockDesc".Translate(),
                icon = !string.IsNullOrEmpty(Props.relockTexPath)
                    ? ContentFinder<Texture2D>.Get(Props.relockTexPath)
                    : null,
                action = () => BeginRelockTargeting()
            };
        }

        // Provides float menu options when a pawn right-clicks on the hatch.
        // Mirrors vanilla CompSealable behavior for consistency.
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            // Only show relock option when hatch is unlocked (hacked)
            CompHackable hackable = parent.GetComp<CompHackable>();
            if (hackable == null || !hackable.IsHacked)
                yield break;

            // Only show when not on player's home map
            if (parent.Map != null && parent.Map.IsPlayerHome)
                yield break;

            // Check if pawn can do the job
            if (!selPawn.IsColonistPlayerControlled)
                yield break;

            if (selPawn.Downed || selPawn.Dead)
                yield break;

            string label = Props.relockCommandLabel ?? "BTG_CargoVaultHatch_RelockLabel".Translate();

            yield return new FloatMenuOption(label, delegate
            {
                TryAssignRelockJobWithWarning(selPawn);
            });
        }

        // Begins pawn targeting for the relock action.
        // Similar to vanilla's CompSealable targeting behavior.
        private void BeginRelockTargeting()
        {
            // Set up targeting parameters for colonists
            TargetingParameters targetParams = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                canTargetLocations = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = (TargetInfo target) =>
                {
                    if (!target.HasThing)
                        return false;

                    Pawn pawn = target.Thing as Pawn;
                    if (pawn == null)
                        return false;

                    // Must be a colonist
                    if (!pawn.IsColonistPlayerControlled)
                        return false;

                    // Must be capable of doing the job
                    if (pawn.Downed || pawn.Dead)
                        return false;

                    return true;
                }
            };

            // Begin targeting - when pawn is selected, check for pawns to capture and show warning
            Find.Targeter.BeginTargeting(targetParams, delegate(LocalTargetInfo target)
            {
                Pawn selectedPawn = target.Thing as Pawn;
                if (selectedPawn != null)
                {
                    TryAssignRelockJobWithWarning(selectedPawn);
                }
            });
        }

        // Checks for pawns in the pocket map and shows a warning dialog if any would be captured.
        // If no pawns would be captured, or if the player confirms, assigns the relock job.
        private void TryAssignRelockJobWithWarning(Pawn worker)
        {
            MapPortal portal = parent as MapPortal;
            if (portal == null)
            {
                AssignRelockJob(worker);
                return;
            }

            Map pocketMap = portal.PocketMap;
            if (pocketMap == null)
            {
                AssignRelockJob(worker);
                return;
            }

            // Find player pawns that would be left behind (excluding the worker)
            List<Pawn> colonistsToCapture = new List<Pawn>();
            List<Pawn> animalsToCapture = new List<Pawn>();

            foreach (Pawn pawn in pocketMap.mapPawns.AllPawns)
            {
                // Only warn about player faction pawns (colonists, mechs, animals)
                if (pawn.Faction != Faction.OfPlayer)
                    continue;

                // Skip the worker who will be doing the relocking (they'll leave first)
                if (pawn == worker)
                    continue;

                // Categorize by type
                if (pawn.RaceProps.Animal)
                {
                    animalsToCapture.Add(pawn);
                }
                else
                {
                    colonistsToCapture.Add(pawn);
                }
            }

            // If no pawns would be captured, proceed without warning
            if (colonistsToCapture.Count == 0 && animalsToCapture.Count == 0)
            {
                AssignRelockJob(worker);
                return;
            }

            // Build warning message (matching vanilla CompSealable format)
            StringBuilder warningText = new StringBuilder();

            if (colonistsToCapture.Count > 0)
            {
                // Format: "Warning: The following people will be left behind:\n"
                warningText.Append("Warning".Translate() + ": " + "PeopleWillBeLeftBehind".Translate() + ":\n");
                foreach (Pawn p in colonistsToCapture)
                {
                    warningText.Append("  - " + p.NameFullColored.Resolve() + "\n");
                }
            }

            if (animalsToCapture.Count > 0)
            {
                if (warningText.Length > 0)
                    warningText.Append("\n");

                // Format: "Warning: The following will be left behind:\n"
                warningText.Append("Warning".Translate() + ": " + "AnimalsWillBeLeftBehind".Translate() + ":\n");
                foreach (Pawn p in animalsToCapture)
                {
                    warningText.Append("  - " + p.NameFullColored.Resolve() + "\n");
                }
            }

            // Show confirmation dialog
            Dialog_MessageBox dialog = Dialog_MessageBox.CreateConfirmation(
                warningText.ToString().TrimEnd(),
                delegate
                {
                    AssignRelockJob(worker);
                },
                destructive: true
            );
            Find.WindowStack.Add(dialog);
        }

        // Assigns the BTG_Relock job to the selected pawn.
        private void AssignRelockJob(Pawn pawn)
        {
            Job job = JobMaker.MakeJob(DefRefs.Jobs.BTG_Relock, parent);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        // Relocks the hatch by:
        // 1. Returning all items in the pocket map to settlement stock
        // 2. Destroying the pocket map
        // 3. Resetting CompHackable state
        public void Relock()
        {
            MapPortal portal = parent as MapPortal;
            if (portal == null)
            {
                Log.Warning("[BTG] CompRelockable.Relock: parent is not a MapPortal");
                return;
            }

            // 1. Return items to settlement stock and destroy pocket map
            Map pocketMap = portal.PocketMap;
            if (pocketMap != null)
            {
                CargoReturnHelper.ReturnItemsToStock(pocketMap);
                PocketMapUtility.DestroyPocketMap(pocketMap);
            }

            // 2. Reset CompHackable state via reflection
            ResetHackableState();
        }

        // Resets the CompHackable component to its initial locked state.
        // Uses reflection to access private fields.
        private void ResetHackableState()
        {
            CompHackable hackable = parent.GetComp<CompHackable>();
            if (hackable == null)
            {
                Log.Warning("[BTG] CompRelockable.ResetHackableState: No CompHackable found");
                return;
            }

            if (!CompHackableReflection.TrySetHackedState(hackable, hacked: false, progress: 0f))
                Log.Warning("[BTG] CompRelockable.ResetHackableState: Could not reset hackable state (reflection unavailable)");
        }
    }
}
