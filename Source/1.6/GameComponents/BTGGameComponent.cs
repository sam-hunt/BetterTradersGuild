using BetterTradersGuild.Patches.PawnNameColorUtilityPatches;
using Verse;

namespace BetterTradersGuild.GameComponents
{
    // Per-game lifecycle hooks. Instantiated automatically by GameComponentUtility
    // for every new or loaded game.
    public class BTGGameComponent : GameComponent
    {
        public BTGGameComponent(Game game)
        {
        }

        // Runs for both new and loaded games, after the world and maps are ready.
        // The survivor-label patch is process-global static state while defeat is
        // per-save: loading a save with a defeated map must re-apply it, and
        // switching to a save without one must remove it.
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            PawnNameColorUtilityPawnNameColorOf.Refresh();
        }
    }
}
