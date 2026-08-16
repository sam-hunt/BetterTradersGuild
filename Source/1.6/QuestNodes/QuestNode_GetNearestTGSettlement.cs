using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.QuestNodes
{
    // Quest node that finds the nearest Traders Guild settlement for trade request quests.
    // Filters settlements by faction, relations, and active trade request status.
    // Uses 3D spherical distance for orbital settlements.
    public class QuestNode_GetNearestTGSettlement : QuestNode
    {
        // Output slot names (matching vanilla QuestNode_GetNearbySettlement pattern)
        [NoTranslate]
        public SlateRef<string> storeAs;

        [NoTranslate]
        public SlateRef<string> storeFactionLeaderAs;

        [NoTranslate]
        public SlateRef<string> storeFactionAs;

        // Configuration
        public SlateRef<bool> allowActiveTradeRequest = false;
        public SlateRef<bool> allowHostile = false;
        public SlateRef<int> maxTileDistance = 64;

        // Vanilla down-weights hostile quest-givers rather than excluding them
        // (Script_BanditCamp: hostileWeight 0.15 vs nonHostileWeight 1). A relative
        // weight can't apply with a single candidate faction, so approximate it as
        // an offer chance rolled once per generation attempt.
        private const float HostileOfferChance = 0.15f;

        protected override bool TestRunInt(Slate slate)
        {
            Map map = slate.Get<Map>("map");
            if (map == null)
                return false;

            Settlement settlement = FindNearestTGSettlement(map, slate);
            if (settlement == null)
                return false;

            if (settlement.Faction.HostileTo(Faction.OfPlayer) && !Rand.Chance(HostileOfferChance))
                return false;

            // IMPORTANT: Set slate values during TestRunInt so subsequent nodes can access them
            // This matches vanilla QuestNode_GetNearbySettlement behavior
            SetSlateValues(slate, settlement);
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Map map = slate.Get<Map>("map");

            if (map == null)
            {
                Log.Error("[Better Traders Guild] QuestNode_GetNearestTGSettlement: No map found in slate");
                return;
            }

            Settlement settlement = FindNearestTGSettlement(map, slate);
            if (settlement == null)
            {
                Log.Error("[Better Traders Guild] QuestNode_GetNearestTGSettlement: No eligible TG settlement found");
                return;
            }

            // Set slate values again during RunInt (matches vanilla pattern)
            SetSlateValues(slate, settlement);
        }

        private void SetSlateValues(Slate slate, Settlement settlement)
        {
            // Store settlement reference
            string settlementSlotName = storeAs.GetValue(slate);
            if (!string.IsNullOrEmpty(settlementSlotName))
                slate.Set(settlementSlotName, settlement);

            // Store faction leader reference
            string leaderSlotName = storeFactionLeaderAs.GetValue(slate);
            if (!string.IsNullOrEmpty(leaderSlotName) && settlement.Faction?.leader != null)
                slate.Set(leaderSlotName, settlement.Faction.leader);

            // Store faction directly (avoids needing QuestNode_GetFactionOf)
            string factionSlotName = storeFactionAs.GetValue(slate);
            if (!string.IsNullOrEmpty(factionSlotName) && settlement.Faction != null)
                slate.Set(factionSlotName, settlement.Faction);

            // Primitive slate vars become grammar constants at text resolution
            // (QuestGenUtility.AddSlateVar), so questDescriptionRules can branch on
            // askerFactionHostile==True/False for the hostile-giver description.
            slate.Set("askerFactionHostile", settlement.Faction.HostileTo(Faction.OfPlayer));
        }

        private Settlement FindNearestTGSettlement(Map playerMap, Slate slate)
        {
            PlanetTile playerTile = playerMap.Tile;
            int maxDist = maxTileDistance.GetValue(slate);
            bool allowActive = allowActiveTradeRequest.GetValue(slate);

            Settlement nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                // Must be TradersGuild faction
                if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                    continue;

                // Must have peaceful relations, unless the quest opts into hostile
                // givers (vanilla opportunity-site pattern, e.g. Script_BanditCamp).
                // Even then a relation row must exist (defs like pirates never get one).
                if (allowHostile.GetValue(slate)
                    ? !TradersGuildHelper.HasPlayerRelation(settlement.Faction)
                    : !TradersGuildHelper.CanPeacefullyVisit(settlement.Faction))
                    continue;

                // Check for existing active trade request (unless allowed)
                if (!allowActive)
                {
                    TradeRequestComp tradeRequest = settlement.GetComponent<TradeRequestComp>();
                    if (tradeRequest?.ActiveRequest == true)
                        continue;
                }

                // Calculate 3D spherical distance
                float dist = GetSphericalDistance(playerTile, settlement.Tile);

                if (dist < nearestDist)
                {
                    nearest = settlement;
                    nearestDist = dist;
                }
            }

            // Final distance check - if nearest is too far, return null
            // But be lenient for orbital settlements (multiply max by factor)
            float maxAllowedDist = maxDist * 2f; // Allow 2x max for orbital
            if (nearest != null && nearestDist > maxAllowedDist)
                return null;

            return nearest;
        }

        // Calculate 3D spherical distance between two world tiles.
        // Uses WorldGrid.GetTileCenter for accurate planet surface positions.
        // Takes PlanetTile (not int) so the layer survives: truncating to a tile id would
        // resolve the id on the surface layer and measure to the wrong point for orbital
        // settlements, especially with mods that add extra planet layers.
        private float GetSphericalDistance(PlanetTile tile1, PlanetTile tile2)
        {
            Vector3 pos1 = Find.WorldGrid.GetTileCenter(tile1);
            Vector3 pos2 = Find.WorldGrid.GetTileCenter(tile2);
            return Vector3.Distance(pos1, pos2);
        }
    }
}
