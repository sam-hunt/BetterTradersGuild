using System.Linq;
using RimWorld;
using Verse;

namespace BetterTradersGuild.GameComponents
{
    /// <summary>
    /// Retroactive fix for the Empire permanent hostility bug.
    ///
    /// Prior versions did not add BTG_IndependentTraders to the Empire's
    /// permanentEnemyToEveryoneExcept list. The XML patch (Faction_Empire_Royalty.xml)
    /// fixes new games, but existing saves still have -100 goodwill baked in.
    /// This component removes the permanent lock on first load, then shows a
    /// one-time dialog letting the player choose whether to reset goodwill to
    /// neutral or leave it at -100.
    /// </summary>
    public class GameComponent_EmpireRelationFix : GameComponent
    {
        private bool empireRelationFixed;

        public GameComponent_EmpireRelationFix(Game game) { }

        public override void StartedNewGame()
        {
            // New games have the XML patch active, so no fix is needed.
            // Set the flag to prevent the dialog showing if the player
            // later becomes hostile with the Empire through normal gameplay.
            empireRelationFixed = true;
        }

        public override void LoadedGame()
        {
            if (empireRelationFixed)
                return;

            // Only applies to games using our custom player faction
            if (Faction.OfPlayer?.def?.defName != "BTG_IndependentTraders")
            {
                empireRelationFixed = true;
                return;
            }

            // Find the Empire faction (Royalty DLC)
            Faction empire = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == "Empire");

            if (empire == null)
            {
                empireRelationFixed = true;
                return;
            }

            // Only fix if Empire is actually hostile (the bug symptom)
            if (empire.PlayerRelationKind != FactionRelationKind.Hostile)
            {
                empireRelationFixed = true;
                return;
            }

            // The XML patch has already removed the permanent lock via the FactionDef.
            // Now let the player decide whether to also reset goodwill.
            ShowFixDialog(empire);
        }

        private void ShowFixDialog(Faction empire)
        {
            string empireName = empire.Name.Colorize(ColoredText.FactionColor_Hostile);
            string playerName = Faction.OfPlayer.Name.Colorize(ColoredText.FactionColor_Neutral);

            Faction tgFaction = Find.FactionManager.FirstFactionOfDef(DefRefs.Factions.TradersGuild);
            string modName = "BTG_Settings_ModName".Translate()
                .Colorize(tgFaction?.Color ?? ColoredText.NameColor);

            Find.WindowStack.Add(new Dialog_MessageBox(
                "BTG_EmpireRelationFix_DialogBody".Translate(empireName, playerName, modName),
                buttonAText: "BTG_EmpireRelationFix_Neutral".Translate(),
                buttonAAction: () =>
                {
                    int current = empire.PlayerGoodwill;
                    empire.TryAffectGoodwillWith(
                        Faction.OfPlayer,
                        -current,
                        canSendMessage: false,
                        canSendHostilityLetter: false);
                    Log.Message(
                        $"[Better Traders Guild] Empire relation fix: " +
                        $"set {empire.Name} goodwill from {current} to 0");
                    empireRelationFixed = true;
                },
                buttonBText: "BTG_EmpireRelationFix_LeaveHostile".Translate(),
                buttonBAction: () =>
                {
                    Log.Message(
                        $"[Better Traders Guild] Empire relation fix: " +
                        $"player chose to leave {empire.Name} goodwill at {empire.PlayerGoodwill}");
                    empireRelationFixed = true;
                },
                title: "BTG_EmpireRelationFix_DialogTitle".Translate(modName)));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref empireRelationFixed, "empireRelationFixed", false);
        }
    }
}
