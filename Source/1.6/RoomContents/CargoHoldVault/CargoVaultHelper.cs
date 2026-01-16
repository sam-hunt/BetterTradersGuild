using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BetterTradersGuild.RoomContents.CargoVault
{
    /// <summary>
    /// Helper class for cargo vault operations.
    /// Provides navigation from pocket maps to parent settlements and stock access.
    /// </summary>
    public static class CargoVaultHelper
    {
        /// <summary>
        /// Cached reflection access to Settlement_TraderTracker.stock private field
        /// </summary>
        private static readonly FieldInfo stockField = typeof(Settlement_TraderTracker)
            .GetField("stock", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Cached reflection access to Settlement_TraderTracker.RegenerateStock() protected method
        /// </summary>
        private static readonly MethodInfo regenerateStockMethod = typeof(Settlement_TraderTracker)
            .GetMethod("RegenerateStock", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Navigates from a pocket map to its parent settlement.
        /// The pocket map's Parent is a PocketMapParent, which has a sourceMap
        /// that belongs to the actual Settlement.
        /// </summary>
        /// <param name="pocketMap">The pocket map (cargo vault map)</param>
        /// <returns>The parent Settlement, or null if not found</returns>
        public static Settlement GetParentSettlement(Map pocketMap)
        {
            if (pocketMap?.Parent is PocketMapParent pocketParent)
            {
                Map sourceMap = pocketParent.sourceMap;
                return sourceMap?.Parent as Settlement;
            }
            return null;
        }

        /// <summary>
        /// Gets the source map (settlement map) from a pocket map.
        /// </summary>
        /// <param name="pocketMap">The pocket map (cargo vault map)</param>
        /// <returns>The settlement map, or null if not found</returns>
        public static Map GetSettlementMap(Map pocketMap)
        {
            if (pocketMap?.Parent is PocketMapParent pocketParent)
            {
                return pocketParent.sourceMap;
            }
            return null;
        }

        /// <summary>
        /// Gets the trade stock from a settlement's trader tracker using reflection.
        /// </summary>
        /// <param name="tracker">The settlement's trader tracker</param>
        /// <returns>The stock ThingOwner, or null if reflection fails</returns>
        /// <remarks>
        /// LEARNING NOTE: Settlement_TraderTracker.stock is private, requiring reflection.
        /// This pattern is also used in SettlementTraderTrackerRegenerateStock.cs.
        /// </remarks>
        public static ThingOwner<Thing> GetStock(Settlement_TraderTracker tracker)
        {
            if (tracker == null || stockField == null)
            {
                return null;
            }
            return stockField.GetValue(tracker) as ThingOwner<Thing>;
        }

        /// <summary>
        /// Gets the trade stock from a settlement.
        /// Convenience method that handles null checks.
        /// Will trigger stock generation if not already generated.
        /// </summary>
        /// <param name="settlement">The settlement</param>
        /// <returns>The stock ThingOwner, or null if not available</returns>
        public static ThingOwner<Thing> GetStock(Settlement settlement)
        {
            if (settlement == null)
            {
                Log.Message("[BTG CargoVault] GetStock: settlement is null");
                return null;
            }

            if (settlement.trader == null)
            {
                Log.Message("[BTG CargoVault] GetStock: settlement.trader is null");
                return null;
            }

            Log.Message($"[BTG CargoVault] GetStock: stockField is {(stockField != null ? "valid" : "NULL")}");

            // Check TraderKind - required for stock generation
            TraderKindDef traderKind = settlement.trader.TraderKind;
            Log.Message($"[BTG CargoVault] GetStock: TraderKind is {traderKind?.defName ?? "NULL"}");

            // Check if stock needs to be generated
            ThingOwner<Thing> existingStock = GetStock(settlement.trader);
            if (existingStock == null)
            {
                Log.Message("[BTG CargoVault] GetStock: Stock not yet generated, attempting RegenerateStock...");

                if (traderKind == null)
                {
                    Log.Warning("[BTG CargoVault] GetStock: Cannot generate stock - TraderKind is null");
                    return null;
                }

                if (regenerateStockMethod == null)
                {
                    Log.Warning("[BTG CargoVault] GetStock: Cannot generate stock - RegenerateStock method not found via reflection");
                    return null;
                }

                try
                {
                    regenerateStockMethod.Invoke(settlement.trader, null);
                    Log.Message("[BTG CargoVault] GetStock: RegenerateStock completed");
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[BTG CargoVault] GetStock: Exception during RegenerateStock: {ex.Message}");
                    return null;
                }
            }

            ThingOwner<Thing> stock = GetStock(settlement.trader);
            Log.Message($"[BTG CargoVault] GetStock: Reflection returned {(stock != null ? $"{stock.Count} items" : "null")}");
            return stock;
        }
    }
}
