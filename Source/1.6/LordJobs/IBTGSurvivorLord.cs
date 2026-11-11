namespace BetterTradersGuild.LordJobs
{
    // Marker for LordJobs whose members are guaranteed non-hostile once their map's
    // garrison is defeated (TradersGuildHelper.IsPostDefeatMap): the defenders'
    // abandon-ship chain, the sheltering civilians, and the worker/passive mechs.
    //
    // The survivor label override (PawnNameColorUtilityPawnNameColorOf) uses it to
    // recolor only genuine survivors: pawns arriving in raid lords after defeat (a
    // stranded civilian's resupply call can summon a garrison-faction raid, and the
    // storyteller treats a gravship-landed den as a home map) never match and keep
    // their hostile label. Lordless garrison-faction
    // pawns (crib infants; permanently downed sentry drones) count as survivors
    // without needing the marker - nothing arrives on these maps lordless and hostile.
    public interface IBTGSurvivorLord
    {
    }
}
