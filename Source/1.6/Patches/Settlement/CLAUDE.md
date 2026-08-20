# Settlement Patches — Trader Rotation and Cargo Vault Stock

These two subsystems have non-obvious ordering and lifecycle contracts. Read this before
touching any `SettlementTraderTracker*` patch, `SettlementMapGenerated`, or
`SettlementDefeatUtilityCheckDefeated` — and also before touching
`MapComponents/SettlementStockCache.cs` or `RoomContents/CargoHoldVault/CargoVaultHelper.cs`,
which collaborate with these patches from outside this directory.

## Trader Rotation System

The mod implements a virtual schedule system for trader rotation across TradersGuild settlements.

**Virtual Schedules:**

- Each settlement has a deterministic rotation schedule based on its ID
- Settlement ID offset (prime multiplier: 123457) desynchronizes rotation across settlements
- Unvisited settlements show stable previews that match what they'll get when visited
- Rotation interval is player-configurable (5-60 days, default 30)

**Core Helper: `TradersGuildTraderRotation.GetEffectiveLastStockTicks()`**

This is the single source of truth for determining which tick value to use for trader selection:

| Scenario                        | Input `storedLastStockTicks`        | Returns                   |
| ------------------------------- | ----------------------------------- | ------------------------- |
| Unvisited settlement            | `-1`                                | Virtual schedule tick     |
| Visited, rotation occurred      | `stored + interval <= currentTicks` | NEW virtual schedule tick |
| Visited, within rotation period | `stored + interval > currentTicks`  | Stored value (unchanged)  |

By using this helper in both preview (GetTraderKind) and stock generation (RegenerateStockAlignment), we ensure both paths produce the same trader type for the same rotation cycle.

**Three-Patch Architecture:**

The trader rotation system requires three Harmony patches working together:

1. **SettlementTraderTrackerGetTraderKind.cs** (Postfix on `TraderKind` getter)
   - Provides weighted random orbital trader selection using `Hash(settlementID, effectiveLastStockTicks)` as seed
   - **Priority 1**: Checks `TradersGuildWorldComponent` cache (populated after stock generation)
   - **Priority 2**: During mid-regeneration with alignment, uses pending alignment value
   - **Priority 3**: Falls back to `GetEffectiveLastStockTicks()` helper for preview or post-generation

2. **SettlementTraderTrackerRegenerateStock.cs** (Prefix/Postfix on `RegenerateStock()`)
   - **ESSENTIAL** - Sets thread-local flag during stock regeneration
   - Prefix runs at `Priority.High`: it decides whether regeneration runs at all (frozen stock
     while visiting), and that decision must land before the alignment prefix reads it
   - Postfix caches selected trader to `TradersGuildWorldComponent` for subsequent access
   - **CRITICAL ORDERING**: Must cache trader BEFORE clearing flag (see below)
   - Exposes `IsRegeneratingStock(settlementID)` for other patches

3. **SettlementTraderTrackerRegenerateStockAlignment.cs** (Prefix/Postfix on `RegenerateStock()`)
   - Aligns stock generation with virtual schedule for BOTH first-time AND rotation scenarios
   - Prefix: Calls `GetEffectiveLastStockTicks()`, sets up alignment if effective != stored
   - Prefix bails on `__runOriginal == false` (void prefixes still run after a skip), so a
     blocked regeneration never rewrites `lastStockGenerationTicks` without generating stock
   - Postfix: Restores aligned value after vanilla overwrites with TicksGame
   - Exposes `HasPendingAlignment(settlementID)` for other patches
   - Postfix ordering vs. patch 2's postfix is order-independent by design: once alignment is
     set up, the stored field and `pendingAlignments` agree on the same virtual tick, so the
     TraderKind getter resolves identically whichever postfix runs first

**Critical Problem #1: Stock/Dialog Desync**

Vanilla `RegenerateStock()` updates `lastStockGenerationTicks` at the END:

```
1. Stock cleared
2. TraderKind getter called (uses OLD lastStockTicks) → Selects Trader A
3. Stock generated for Trader A
4. lastStockGenerationTicks = TicksGame (NEW value)
5. Dialog opens → TraderKind getter (uses NEW lastStockTicks) → Selects Trader B
```

Result: Dialog shows Trader B title but has Trader A's inventory!

**Solution:** Two-part fix:

1. Alignment Prefix sets `lastStockGenerationTicks` to effective value and stores in `pendingAlignments`. TraderKind getter detects `IsRegeneratingStock` + `HasPendingAlignment` and uses the aligned value.
2. RegenerateStock Postfix caches the selected trader to `TradersGuildWorldComponent`. Subsequent TraderKind accesses check this cache first, bypassing recalculation entirely.

**Critical Ordering in Postfix:** The RegenerateStock Postfix must call `TraderKind` to cache the result BEFORE clearing the `IsRegeneratingStock` flag. If the flag is cleared first, the getter won't check `HasPendingAlignment()` and will use the wrong tick value, caching the wrong trader.

**Critical Problem #2: Preview/Visit Mismatch**

Without alignment, preview and stock generation use different seeds:

```
Preview: GetEffectiveLastStockTicks() → virtual schedule → Shows Exotic Trader
Visit: RegenerateStock() sets lastStockTicks = TicksGame → Different seed → Bulk Trader
```

**Solution:** Alignment patch calls `GetEffectiveLastStockTicks()` and pre-sets the field to the effective value. After vanilla overwrites it with TicksGame, Postfix restores the aligned value.

**Critical Problem #3: Visited Settlement After Rotation**

Without proper handling, visited settlements after rotation would use stale stored values:

```
Previous visit: lastStockTicks = 500000 → Trader A
Time passes: 500000 + interval < currentTicks (rotation occurred)
Preview: Uses old stored 500000 → Still shows Trader A (wrong!)
Should show: NEW virtual schedule for current rotation cycle → Trader B
```

**Solution:** `GetEffectiveLastStockTicks()` detects rotation (stored + interval <= currentTicks) and returns NEW virtual schedule tick, ensuring both preview and regeneration use the same seed for the current rotation cycle.

**Two Stock Generation Flows:**

Both flows use the same alignment logic via the shared helper:

1. **Settlement Map Entry** (`SettlementMapGenerated.Postfix`)
   - Triggers when player enters settlement
   - Checks `GetEffectiveLastStockTicks()` to detect if rotation occurred while away
   - If rotated: clears stale stock, resets lastStockTicks to -1, triggers regeneration
   - Regeneration uses alignment patch → consistent with preview

2. **World Map Caravan Trading** (vanilla `RegenerateStock`)
   - Triggers when player trades via caravan without entering
   - Alignment patch calls `GetEffectiveLastStockTicks()` to determine effective value
   - Same logic handles both first-time and rotation scenarios

**Settings Change Handling:**

When the rotation interval setting changes:

- `TradersGuildWorldComponent.ScaleExpirationsForIntervalChange()` proportionally scales remaining time on all cached expiration ticks
- This preserves trader types while adjusting timing to match new interval
- Example: 30→15 day interval change, trader with 12 days remaining now has 6 days remaining

## Cargo Vault Stock Management

The cargo vault displays physical items from the settlement's trade inventory. Stock must be carefully managed across the settlement visit lifecycle.

**Stock Lifecycle:**

```
1. Player enters settlement → Map.FinalizeInit → Stock generated if null (SettlementMapGenerated patch)
2. While visiting → Stock frozen (RegenerateStock/TryDestroyStock patches block changes)
3. Player opens cargo vault → Items spawned from stock (CargoSelector removes from stock)
4. Vault locked (pawn relock action) → Remaining items returned to stock (CargoReturnHelper)
5. Vault hatch despawns (map unload) → Remaining items returned to stock (CargoReturnHelper)
6. Player defeats settlement → Stock transferred to MapComponent cache (CheckDefeated patch)
7. Post-defeat vault access → Uses cached stock (CargoVaultHelper fallback)
8. Post-defeat vault locked/despawns → Remaining items returned to cache (CargoReturnHelper)
```

**Stock Return:** Items left in the vault (not taken by player) are returned via `CargoReturnHelper.ReturnItemsToStock()` when the vault is locked or despawns. Pre-defeat, items return to trader stock; post-defeat, items return to `SettlementStockCache`. This is handled transparently by `CargoVaultHelper.GetStock()` checking `settlement.Destroyed`.

**Key Invariant:** Once `settlement.Map` is non-null, stock is guaranteed to exist and remains frozen until map unload or defeat.

**Four Coordinating Patches:**

| Patch                                    | Hook                             | Purpose                                              |
| ---------------------------------------- | -------------------------------- | ---------------------------------------------------- |
| `SettlementMapGenerated`                 | `Map.FinalizeInit` Postfix       | Ensures stock exists when map loads                  |
| `SettlementTraderTrackerRegenerateStock` | `RegenerateStock()` Prefix       | Blocks regeneration while map loaded                 |
| `SettlementTraderTrackerTryDestroyStock` | `TryDestroyStock()` Prefix       | Blocks destruction while map loaded OR during defeat |
| `SettlementDefeatUtilityCheckDefeated`   | `CheckDefeated()` Prefix+Postfix | Transfers stock to cache on confirmed defeat         |

**Critical Timing Issue - Defeat Processing:**

During `CheckDefeated()`, vanilla does:

1. Reparents map → `settlement.Map` becomes null
2. Calls `settlement.Destroy()` → triggers `TryDestroyStock()`

Problem: `TryDestroyStock` patch checked `settlement.Map != null`, which is now false, so stock gets destroyed before our Postfix can cache it.

**Solution:** Prefix/Postfix coordination via `settlementsBeingDefeated` HashSet:

- Prefix adds settlement ID to set
- `TryDestroyStock` blocks if ID in set (even when `Map` is null)
- Postfix transfers stock to `SettlementStockCache` MapComponent, removes from set

**Stock Access Helper (`CargoVaultHelper.GetStock`):**

```csharp
// Navigates: pocketMap → PocketMapParent → sourceMap → Settlement
if (settlement?.trader != null && !settlement.Destroyed)
    return traderStock;  // Normal path
else
    return cache.preservedStock;  // Fallback for defeated settlements
```

**Files:** `Patches/Settlement/Settlement*.cs`, `RoomContents/CargoHoldVault/CargoVaultHelper.cs`, `MapComponents/SettlementStockCache.cs`
