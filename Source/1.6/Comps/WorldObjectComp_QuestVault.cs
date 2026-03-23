using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Comps
{
    /// <summary>
    /// CompProperties for WorldObjectComp_QuestVault.
    /// Allows the comp to be attached via XML WorldObjectDef.
    /// </summary>
    public class WorldObjectCompProperties_QuestVault : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_QuestVault()
        {
            compClass = typeof(WorldObjectComp_QuestVault);
        }
    }

    /// <summary>
    /// WorldObjectComp attached to smuggler's den quest Sites.
    /// Stores the player's chosen trader type from the quest reward selection.
    ///
    /// This comp bridges the quest reward system and map generation:
    /// - QuestPart_SetVaultTraderKind writes the chosen trader defName on quest acceptance
    /// - GenStep_GenerateQuestVaultStock reads it during map generation to populate the vault
    /// - CargoHatchSpawner reads it to decide hackable vs sealed hatch
    /// </summary>
    public class WorldObjectComp_QuestVault : WorldObjectComp
    {
        /// <summary>
        /// DefName of the chosen TraderKindDef.
        /// null = goodwill was chosen (vault will be sealed).
        /// </summary>
        public string chosenTraderKindDefName;

        /// <summary>
        /// Resolves the stored defName to a TraderKindDef.
        /// Returns null if no trader was chosen or if the def no longer exists.
        /// </summary>
        public TraderKindDef ChosenTraderKind
        {
            get
            {
                if (string.IsNullOrEmpty(chosenTraderKindDefName))
                    return null;
                return DefDatabase<TraderKindDef>.GetNamedSilentFail(chosenTraderKindDefName);
            }
        }

        /// <summary>
        /// Whether the cargo vault should be hackable (true) or sealed (false).
        /// True when a trader type was chosen; false when goodwill was chosen.
        /// </summary>
        public bool HasCargoVault => !string.IsNullOrEmpty(chosenTraderKindDefName);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref chosenTraderKindDefName, "chosenTraderKindDefName");
        }
    }
}
