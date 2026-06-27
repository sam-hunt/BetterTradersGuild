using RimWorld;
using Verse.AI;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized DutyDef references for Better Traders Guild custom duties.
    /// </summary>
    [DefOf]
    public static class Duties
    {
        /// <summary>
        /// Passive wander duty - mechs wander within 7 tiles of focus point
        /// without actively seeking enemies. Used for expensive/specialized
        /// mechs (Fabricor, Lifter).
        /// </summary>
        public static DutyDef BTG_WanderInArea;

        /// <summary>
        /// Bounded defender duty - combat-engaged but constrained to the
        /// settlement structure footprint. Target acquisition is filtered
        /// by JobGiver_BTGDefendStructure so defenders never path outside the
        /// structure to pursue intruders. Includes self-tend and forage
        /// fallbacks for human defenders.
        /// </summary>
        public static DutyDef BTG_DefendStructure;

        /// <summary>
        /// Paramedic-mech medic duty - room-bound triage. Priority order: emergency
        /// tend the worst-bleeding defender in place, rescue downed defenders to an
        /// in-room medical bed, routine tend (bed or floor, best in-room medicine),
        /// then dormant self-charge when idle. Every action is confined to the mech's
        /// MedicalBay (see MedicRoomBounds). Used by LordJob_MechMedic.
        /// </summary>
        public static DutyDef BTG_MechMedic;

        /// <summary>
        /// Cleansweeper-mech janitor duty - radius-bound, structure-confined. Priority
        /// order: clean the nearest in-range filth and its cluster in one vanilla Clean
        /// job, then dormant self-charge near the anchor point when none remains. Filth
        /// is searched only within a moderate radius of the anchor and never outside the
        /// settlement structure footprint (see CleanArea). Used by LordJob_MechClean.
        /// </summary>
        public static DutyDef BTG_MechClean;

        /// <summary>
        /// Agrihand-mech farming duty - radius-bound, structure-confined greenhouse
        /// tending. Priority order: harvest mature food crops from in-range hydroponics
        /// basins, haul the (forbidden) produce to a nearby shelf, sow the basin crop
        /// (rice) into the emptied cells, then dormant self-charge near the anchor point
        /// when idle. Every action is confined to a moderate radius of the anchor and
        /// never outside the settlement structure footprint (see FarmArea). Used by
        /// LordJob_MechFarm.
        /// </summary>
        public static DutyDef BTG_MechFarm;

        static Duties() => DefOfHelper.EnsureInitializedInCtor(typeof(Duties));
    }
}
