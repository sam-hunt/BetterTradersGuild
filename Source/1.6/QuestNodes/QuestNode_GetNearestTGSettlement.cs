using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.QuestNodes
{
    /// <summary>
    /// Quest node that finds the nearest Traders Guild settlement for trade request quests.
    /// Filters settlements by faction, relations, and active trade request status.
    /// Uses 3D spherical distance for orbital settlements.
    /// </summary>
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
        public SlateRef<int> maxTileDistance = 64;

        protected override bool TestRunInt(Slate slate)
        {
            Map map = slate.Get<Map>("map");
            if (map == null)
                return false;

            Settlement settlement = FindNearestTGSettlement(map, slate);
            if (settlement == null)
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
        }

        private Settlement FindNearestTGSettlement(Map playerMap, Slate slate)
        {
            int playerTile = playerMap.Tile;
            int maxDist = maxTileDistance.GetValue(slate);
            bool allowActive = allowActiveTradeRequest.GetValue(slate);

            Settlement nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                // Must be TradersGuild faction
                if (!TradersGuildHelper.IsTradersGuildSettlement(settlement))
                    continue;

                // Must have peaceful relations
                if (!TradersGuildHelper.CanPeacefullyVisit(settlement.Faction))
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

                // Check max distance (use spherical distance for comparison)
                // Note: maxTileDistance is approximate - spherical distances may differ from tile counts
                if (dist > maxDist && dist < nearestDist)
                {
                    // Still consider if it's the closest, even if beyond maxDist
                    // This handles cases where all TG settlements are orbital (far in 3D)
                }

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

        /// <summary>
        /// Calculate 3D spherical distance between two world tiles.
        /// Uses WorldGrid.GetTileCenter for accurate planet surface positions.
        /// Works correctly for both surface and orbital tiles.
        /// </summary>
        private float GetSphericalDistance(int tile1, int tile2)
        {
            Vector3 pos1 = Find.WorldGrid.GetTileCenter(tile1);
            Vector3 pos2 = Find.WorldGrid.GetTileCenter(tile2);
            return Vector3.Distance(pos1, pos2);
        }
    }
}
