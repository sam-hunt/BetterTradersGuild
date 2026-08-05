using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.Patches.CaravanPatches
{
    // Harmony patch: Silence the "cannot form caravans" warning for shuttle arrivals at friendly
    // Traders Guild orbital tiles.
    //
    // CaravanMaker.MakeCaravan logs "Tried to create a caravan on a tile which belongs to a layer
    // which cannot form caravans." whenever startingTile.LayerDef.canFormCaravans is false. The
    // warning is advisory only (the caravan is created regardless), but it would fire on every
    // shuttle trade visit to a Traders Guild settlement. The transpiler ORs the field read with
    // the friendly-Traders-Guild tile check, so the warning still fires for genuinely
    // unexpected tiles.
    //
    // Degrades gracefully: if the anchor instruction is not found (RimWorld API change), the
    // method is left untouched and the only consequence is the harmless warning reappearing.
    [HarmonyPatch(typeof(CaravanMaker), nameof(CaravanMaker.MakeCaravan))]
    public static class CaravanMakerMakeCaravan
    {
        // Set by the transpiler when its anchor is found; checked at startup by
        // ReflectionVerification.VerifyAll after Harmony.PatchAll has run.
        private static bool anchorFound;

        public static void VerifyPatched()
        {
            if (!anchorFound)
                Log.Warning("[Better Traders Guild] CaravanMaker.MakeCaravan canFormCaravans read not found; "
                    + "harmless 'cannot form caravans' warnings will appear on shuttle arrivals at "
                    + "Traders Guild settlements. RimWorld API may have changed.");
        }

        // Replaces the value of `startingTile.LayerDef.canFormCaravans` on the stack with
        // `canFormCaravans || IsFriendlyTradersGuildTile(startingTile)`.
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo canFormCaravansField = AccessTools.Field(
                typeof(PlanetLayerDef), nameof(PlanetLayerDef.canFormCaravans));

            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (!anchorFound && instruction.LoadsField(canFormCaravansField))
                {
                    // MakeCaravan(pawns, faction, startingTile, addToWorldPawnsIfNotAlready)
                    // is static, so startingTile is argument 2.
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(
                        typeof(CaravanMakerMakeCaravan), nameof(AllowedOrFriendlyTradersGuild));
                    anchorFound = true;
                }
            }
        }

        private static bool AllowedOrFriendlyTradersGuild(bool layerAllows, PlanetTile tile)
        {
            return layerAllows || TileHelper.IsFriendlyTradersGuildTile(tile);
        }
    }
}
