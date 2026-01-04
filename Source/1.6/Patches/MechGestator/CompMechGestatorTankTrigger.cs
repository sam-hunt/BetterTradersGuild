using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace BetterTradersGuild.Patches.MechGestatorPatches
{
    /// <summary>
    /// Harmony patch: Makes mechs from gestator tanks in TradersGuild settlements
    /// spawn belonging to the TradersGuild faction instead of hostile mechanoids.
    ///
    /// Without this patch, Ancient gestator tanks always spawn mechs belonging to
    /// Faction.OfMechanoids, which is hostile to everyone. This creates a confusing
    /// experience where the TradersGuild's own security mechs attack their owners.
    ///
    /// This patch intercepts the Trigger method and substitutes the TradersGuild
    /// faction when the gestator is located in a TradersGuild settlement map.
    /// </summary>
    [HarmonyPatch(typeof(CompMechGestatorTank), "Trigger")]
    public static class CompMechGestatorTankTrigger
    {
        /// <summary>
        /// Prefix that intercepts gestator triggering in TradersGuild settlements.
        /// If the gestator is in a TradersGuild settlement, we run our modified
        /// version and skip the original. Otherwise, vanilla behavior proceeds.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(CompMechGestatorTank __instance, Map map)
        {
            // Check if this gestator is in a TradersGuild settlement
            if (!TradersGuildHelper.IsMapInTradersGuildSettlement(map))
            {
                // Not in TradersGuild settlement - let vanilla handle it
                return true;
            }

            // Get TradersGuild faction
            Faction tradersGuild = TradersGuildHelper.GetTradersGuildFaction();
            if (tradersGuild == null)
            {
                // Faction not found - shouldn't happen, but fall back to vanilla
                Log.Warning("[Better Traders Guild] TradersGuild faction not found, using vanilla gestator behavior");
                return true;
            }

            // Run our modified trigger logic with TradersGuild faction
            TriggerWithFaction(__instance, map, tradersGuild);

            // Skip the original method
            return false;
        }

        /// <summary>
        /// Modified trigger logic that uses the specified faction instead of Faction.OfMechanoids.
        /// This is essentially a copy of the vanilla CompMechGestatorTank.Trigger method
        /// with faction substitutions.
        /// </summary>
        private static void TriggerWithFaction(CompMechGestatorTank comp, Map map, Faction faction)
        {
            // Access private fields via reflection
            var stateField = AccessTools.Field(typeof(CompMechGestatorTank), "state");
            var triggerRadiusField = AccessTools.Field(typeof(CompMechGestatorTank), "triggerRadius");

            // Get current state - if Empty (0), return early
            int currentState = (int)stateField.GetValue(comp);
            if (currentState == 0) // TankState.Empty
                return;

            // Set state to Empty
            comp.State = CompMechGestatorTank.TankState.Empty;

            // Get parent thing
            ThingWithComps parent = comp.parent;

            // Find a standable spawn position on the edge cells
            CellRect rect = GenAdj.OccupiedRect(parent).ExpandedBy(1);
            IntVec3 spawnPos = rect.EdgeCells
                .Where(c => c.Standable(map))
                .RandomElementWithFallback(IntVec3.Invalid);

            // Scatter gestation fluid around the tank
            ScatterDebrisUtility.ScatterFilthAroundThing(
                parent,
                map,
                Things.Filth_GestationFluid,
                new IntRange(2, 4),
                1,
                null);

            // If no valid spawn position, we're done
            if (!spawnPos.IsValid)
                return;

            // Get the CompProperties to access mech kind options
            // Access via base class props field and cast to specific type
            var props = (CompProperties_MechGestatorTank)comp.props;
            if (props.mechKindOptions == null || props.mechKindOptions.Count == 0)
                return;

            // Select random mech kind weighted by weight
            PawnKindDef mechKind = props.mechKindOptions
                .RandomElementByWeight(x => x.weight)
                .kindDef;

            // Generate pawn with TradersGuild faction instead of OfMechanoids
            Pawn mech = PawnGenerator.GeneratePawn(mechKind, faction);

            // Spawn the mech
            GenSpawn.Spawn(mech, spawnPos, map, WipeMode.Vanish);

            // Stun the mech briefly (180 ticks = 3 seconds)
            mech.stances?.stunner?.StunFor(180, null, false, true, false);

            // Find or create a lord for this faction's assault
            // Note: We use LordJob_AssaultColony but with TradersGuild faction
            // This means the mech will defend the station against intruders
            Lord lord = map.lordManager.lords.FirstOrDefault(l =>
                l.faction == faction &&
                l.LordJob is LordJob_AssaultColony);

            if (lord == null)
            {
                // Create new assault lord - the faction assaults hostile factions (i.e., player raiders)
                var lordJob = new LordJob_AssaultColony(faction, false, false, false, false, false, false, false);
                lord = LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { mech });
            }
            else
            {
                // Add to existing lord
                lord.AddPawn(mech);
            }

            // Show message (using vanilla message from props)
            if (!string.IsNullOrEmpty(props.triggeredMessage))
            {
                Messages.Message(
                    props.triggeredMessage.Formatted(mech.Named("PAWN")),
                    mech,
                    MessageTypes.NegativeEvent,
                    true);
            }

            // Play trigger sound
            props.triggerSound?.PlayOneShot(SoundInfo.InMap(parent));
        }
    }
}
