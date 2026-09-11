using BetterTradersGuild.LayoutWorkers.Settlement;
using Verse;

namespace BetterTradersGuild.MapGeneration
{
    // GenStep that puts the VE pipe networks BTG's layouts build into their "occupied
    // station" state: storage tanks filled to operational levels
    // (PipeNetworkTankFiller) and valves closed with their faction cleared so the player
    // claims them room by room (PipeValveHandler).
    //
    // Runs after BTG_PlaceWallConduits has laid the hidden pipes and before
    // BTG_ExtendLandingPadPipes. Both helpers self-guard: no VE pipe mod installed
    // means no matching tank or valve defs and the step is a no-op, which is why the
    // GenStepDef carries no MayRequire (MapGeneratorDefs reference it by defName).
    public class GenStep_PrimePipeNetworks : GenStep
    {
        public override int SeedPart => 913350471;

        public override void Generate(Map map, GenStepParams parms)
        {
            PipeNetworkTankFiller.FillTanksOnMap(map);
            PipeValveHandler.CloseAllValvesAndClearFaction(map);
        }
    }
}
