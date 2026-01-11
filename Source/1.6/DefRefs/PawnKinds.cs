using RimWorld;
using Verse;

namespace BetterTradersGuild.DefRefs
{
    /// <summary>
    /// Centralized PawnKindDef references (both BTG custom and DLC-gated).
    /// </summary>
    [DefOf]
    public static class PawnKinds
    {
        // === BTG CUSTOM ===
        public static PawnKindDef TradersGuild_Child;
        public static PawnKindDef TradersGuild_Citizen;

        // === VANILLA PETS ===
        public static PawnKindDef Cat;
        public static PawnKindDef Husky;
        public static PawnKindDef LabradorRetriever;
        public static PawnKindDef YorkshireTerrier;

        // === BIOTECH MECHS ===
        [MayRequireBiotech]
        public static PawnKindDef Mech_Paramedic;

        [MayRequireBiotech]
        public static PawnKindDef Mech_Militor;

        [MayRequireBiotech]
        public static PawnKindDef Mech_Fabricor;

        [MayRequireBiotech]
        public static PawnKindDef Mech_Cleansweeper;

        [MayRequireBiotech]
        public static PawnKindDef Mech_Lifter;

        [MayRequireBiotech]
        public static PawnKindDef Mech_Agrihand;

        // === ANOMALY ===
        [MayRequireAnomaly]
        public static PawnKindDef ShamblerSwarmer;

        static PawnKinds() => DefOfHelper.EnsureInitializedInCtor(typeof(PawnKinds));
    }
}
