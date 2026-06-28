using RimWorld;
using Verse.AI;

namespace BetterTradersGuild.DefRefs
{
    // Centralized DutyDef references for Better Traders Guild custom duties.
    [DefOf]
    public static class Duties
    {
        // Passive wander duty - mechs wander within 7 tiles of focus point
        // without actively seeking enemies. Used for expensive/specialized
        // mechs (Fabricor, Lifter).
        public static DutyDef BTG_WanderInArea;

        // Bounded defender duty - combat-engaged but constrained to the
        // settlement structure footprint. Target acquisition is filtered
        // by JobGiver_BTGDefendStructure so defenders never path outside the
        // structure to pursue intruders. Includes self-tend and forage
        // fallbacks for human defenders.
        public static DutyDef BTG_DefendStructure;

        // Paramedic-mech medic duty - room-bound triage. Priority order: emergency
        // tend the worst-bleeding defender in place, rescue downed defenders to an
        // in-room medical bed, routine tend (bed or floor, best in-room medicine),
        // then dormant self-charge when idle. Every action is confined to the mech's
        // MedicalBay (see MedicRoomBounds). Used by LordJob_MechMedic.
        public static DutyDef BTG_MechMedic;

        // Cleansweeper-mech janitor duty - room-bound. Priority order: clean the nearest
        // in-room filth and its cluster in one BTG_Clean job, then dormant self-charge near
        // the anchor point when none remains. Filth is searched only inside the one layout
        // room the mech was spawned into, matched by rect membership so a multi-rect room
        // works exactly (see CleanArea). Used by LordJob_MechClean.
        public static DutyDef BTG_MechClean;

        // Agrihand-mech farming duty - radius-bound, structure-confined greenhouse
        // tending. Priority order: harvest mature food crops from in-range hydroponics
        // basins, haul the (forbidden) produce to a nearby shelf, sow the basin crop
        // (rice) into the emptied cells, then dormant self-charge near the anchor point
        // when idle. Every action is confined to a moderate radius of the anchor and
        // never outside the settlement structure footprint (see FarmArea). Used by
        // LordJob_MechFarm.
        public static DutyDef BTG_MechFarm;

        static Duties() => DefOfHelper.EnsureInitializedInCtor(typeof(Duties));
    }
}
