using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.Comps
{
    // Attached beside CompHackable via XML patch. When the hack completes, the parent is
    // replaced in place by promoteTo: same cell, rotation and hit points, owned by the
    // hacker's faction. Used to turn VGE2's decorative VGE2_LockedShuttle into the working
    // BTG_GuildShuttle (1.6/Mods/VanillaGravshipExpanded2/).
    public class CompProperties_PromoteOnHack : CompProperties
    {
        public ThingDef promoteTo;

        public CompProperties_PromoteOnHack()
        {
            compClass = typeof(CompPromoteOnHack);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (promoteTo == null)
                yield return "CompProperties_PromoteOnHack on " + parentDef.defName + " has no promoteTo";
            else if (promoteTo.size != parentDef.size)
                yield return "CompProperties_PromoteOnHack on " + parentDef.defName + ": promoteTo " + promoteTo.defName
                    + " is " + promoteTo.size + " but the parent is " + parentDef.size + "; the swap keeps the footprint";
        }
    }

    public class CompPromoteOnHack : ThingComp
    {
        private bool promotionQueued;

        public CompProperties_PromoteOnHack Props => (CompProperties_PromoteOnHack)props;

        // CompHackable.OnHacked calls this on every comp of the parent, after the hack
        // message/letter and mid-way through the hacker's job tick. The swap is deferred to
        // the end of the frame so vanilla's OnHacked and the hack toil finish against a
        // still-spawned parent; the toil then ends on its own FailOnDespawned next tick.
        public override void Notify_Hacked(Pawn hacker = null)
        {
            base.Notify_Hacked(hacker);
            if (promotionQueued || Props.promoteTo == null) return;
            promotionQueued = true;

            Faction taker = hacker?.Faction ?? Faction.OfPlayerSilentFail;
            LongEventHandler.ExecuteWhenFinished(() => Promote(taker));
        }

        private void Promote(Faction taker)
        {
            if (parent.Destroyed || !parent.Spawned) return;

            Map map = parent.Map;
            IntVec3 position = parent.Position;
            Rot4 rotation = parent.Rotation;
            int hitPoints = parent.HitPoints;

            Thing promoted = ThingMaker.MakeThing(Props.promoteTo);
            promoted.HitPoints = Mathf.Clamp(hitPoints, 1, promoted.MaxHitPoints);
            if (taker != null && promoted.def.CanHaveFaction)
                promoted.SetFactionDirect(taker);

            // Vanish: no killedLeavings, no deconstruct refund for the shell being replaced.
            parent.Destroy(DestroyMode.Vanish);
            GenSpawn.Spawn(promoted, position, map, rotation);

            // Stealing a shuttle from a standing Traders Guild station is an attack.
            if (taker != null && taker.IsPlayer)
                HostileActHelper.PunishHostileActOnTradersGuildMap(map);
        }
    }
}
