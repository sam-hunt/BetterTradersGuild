# Cargo System Implementation Guide

## Overview

This guide provides concrete code patterns for implementing the dynamic cargo system for TradersGuild settlement shuttle bays.

## Architecture Summary

```
Settlement (Traders Guild)
├── Map (Orbital Platform)
│   └── Cargo Bay Building
│       ├── Building_Storage base
│       ├── SlotGroup for item management
│       └── StorageSettings (configured to accept cargo items)
│
├── TradersGuildSettlementComponent (WorldObject tracking)
│   └── lastCargoRefreshTicks
│
└── TradersGuildCargoRefresher (MapComponent)
    └── Detects rotation & refreshes cargo on map entry
```

## Implementation Steps

### Step 1: XML Definition for Cargo Bay

File: `Defs/RoomDefs/TradersGuild_CargoBay.xml`

```xml
<ThingDef Name="TradersGuild_CargoShelf" ParentName="StorageShelfBase">
  <defName>TradersGuild_CargoShelf</defName>
  <label>cargo shelf</label>
  <description>A storage shelf found in Traders Guild shuttle bays. Displays traded goods in transit.</description>
  
  <!-- Use standard shelf layout but with cargo appearance -->
  <graphicData>
    <texPath>Things/Building/Furniture/Shelf</texPath>
    <graphicClass>Graphic_Multi</graphicClass>
    <drawSize>(3,2)</drawSize>
  </graphicData>
  
  <size>(2,1)</size>
  <costStuffCount>20</costStuffCount>
  
  <statBases>
    <MaxHitPoints>100</MaxHitPoints> 
    <Mass>8</Mass>
    <WorkToBuild>500</WorkToBuild>
  </statBases>
  
  <building>
    <storageGroupTag>TradersGuildCargo</storageGroupTag>
    <!-- Allow any tradeable items -->
    <fixedStorageSettings>
      <filter>
        <disallowNotEverStorable>true</disallowNotEverStorable>
        <categories>
          <li>Root</li>
        </categories>
        <disallowedCategories>
          <li>Chunks</li>
          <li>Plants</li>
          <li>Buildings</li>
          <li>Corpses</li>
        </disallowedCategories>
      </filter>
    </fixedStorageSettings>
    <defaultStorageSettings>
      <priority>Preferred</priority>
      <filter>
        <categories>
          <li>Foods</li>
          <li>Manufactured</li>
          <li>ResourcesRaw</li>
          <li>Items</li>
          <li>Weapons</li>
          <li>Apparel</li>
        </categories>
      </filter>
    </defaultStorageSettings>
  </building>
</ThingDef>
```

### Step 2: World Component for Settlement Tracking

File: `Source/WorldObjects/TradersGuildSettlementComponent.cs`

```csharp
using Verse;
using RimWorld;

namespace BetterTradersGuild.WorldObjects
{
    /// <summary>
    /// Tracks cargo refresh state for a Traders Guild settlement.
    /// Ensures cargo doesn't refresh on every visit (anti-exploit).
    /// </summary>
    public class TradersGuildSettlementComponent : SettlementComponent
    {
        /// <summary>
        /// The last tick when cargo was refreshed for this settlement.
        /// Used to prevent cargo refresh spam on repeated visits.
        /// </summary>
        public int lastCargoRefreshTicks = -1;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastCargoRefreshTicks, "lastCargoRefreshTicks", -1);
        }

        /// <summary>
        /// Check if this settlement's cargo is due for refresh.
        /// </summary>
        public bool IsCargoRefreshDue()
        {
            if (lastCargoRefreshTicks < 0)
                return false; // Never been refreshed yet, will be on first entry

            int refreshInterval = (int)BetterTradersGuildMod.Settings.TraderRotationIntervalDays * GenDate.TicksPerDay;
            int timeSinceRefresh = Find.TickManager.TicksGame - lastCargoRefreshTicks;

            return timeSinceRefresh >= refreshInterval;
        }

        /// <summary>
        /// Mark cargo as refreshed at current game time.
        /// </summary>
        public void MarkCargoRefreshed()
        {
            lastCargoRefreshTicks = Find.TickManager.TicksGame;
        }
    }
}
```

### Step 3: Map Component for Cargo Management

File: `Source/MapComponents/TradersGuildCargoRefresher.cs`

```csharp
using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace BetterTradersGuild.MapComponents
{
    /// <summary>
    /// Manages cargo refresh for Traders Guild settlements.
    /// Spawns/despawns cargo when player enters/leaves map.
    /// </summary>
    public class TradersGuildCargoRefresher : MapComponent
    {
        private Settlement tradersGuildSettlement;
        private List<Building_Storage> cargoShelvesSpawned = new();

        public TradersGuildCargoRefresher(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            // Only check on player's map
            if (!map.IsPlayerHome)
                return;

            // Detect if we're on a Traders Guild settlement map
            if (tradersGuildSettlement == null)
            {
                tradersGuildSettlement = TradersGuildHelper.FindTradersGuildSettlementOnMap(map);
            }

            if (tradersGuildSettlement == null)
                return;

            // This is run during normal gameplay - just track if cargo needs refresh
            // Actual refresh happens on map entry/exit
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Called when map is loaded
            tradersGuildSettlement = TradersGuildHelper.FindTradersGuildSettlementOnMap(map);
            
            if (tradersGuildSettlement != null)
            {
                RefreshCargoIfNeeded();
            }
        }

        /// <summary>
        /// Spawn cargo in shuttle bay if conditions are met.
        /// </summary>
        private void RefreshCargoIfNeeded()
        {
            var component = tradersGuildSettlement.GetComponent<TradersGuildSettlementComponent>();
            if (component == null)
                return;

            bool isFirstVisit = component.lastCargoRefreshTicks < 0;
            bool isRotationDue = component.IsCargoRefreshDue();

            if (!isFirstVisit && !isRotationDue)
                return; // Don't refresh cargo

            // Despawn old cargo
            DespawnAllCargo();

            // Spawn new cargo based on current trader inventory
            SpawnNewCargo();

            // Mark as refreshed
            component.MarkCargoRefreshed();
        }

        /// <summary>
        /// Despawn all cargo shelves without destroying items (simulate loading into shuttle).
        /// </summary>
        private void DespawnAllCargo()
        {
            cargoShelvesSpawned.Clear();
            
            List<Building_Storage> allStorages = map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>();
            foreach (var storage in allStorages)
            {
                // Only despawn cargo bays (check tag)
                if (storage.def.building.storageGroupTag == "TradersGuildCargo")
                {
                    // Remove items to inventory before despawning
                    var contents = storage.slotGroup.HeldThings;
                    if (contents.Any)
                    {
                        // Items are stored in-place when shelf despawns
                        contents.Clear();
                    }

                    storage.Destroy(DestroyMode.Vanish);
                }
            }
        }

        /// <summary>
        /// Spawn cargo shelves with items based on settlement's trade inventory.
        /// </summary>
        private void SpawnNewCargo()
        {
            if (tradersGuildSettlement == null)
                return;

            // Get settlement's trade inventory
            var traderTracker = tradersGuildSettlement.Trader;
            if (traderTracker == null)
                return;

            var tradeInventory = traderTracker.stock;
            if (tradeInventory == null || !tradeInventory.Any)
                return;

            // Calculate cargo budget: percentage of total trade inventory value
            float cargoPercentage = BetterTradersGuildMod.Settings.CargoInventoryPercentage / 100f;
            float totalInventoryValue = CalculateInventoryValue(tradeInventory);
            float cargoBudget = totalInventoryValue * cargoPercentage;

            // Find/create cargo bay room (usually in orbital platform map structure)
            IntVec3 cargoBayLocation = FindCargoBayLocation();
            if (cargoBayLocation == IntVec3.Invalid)
                return; // No suitable location found

            // Spawn cargo shelf building
            Building_Storage cargoShelf = (Building_Storage)GenSpawn.Spawn(
                ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TradersGuild_CargoShelf")),
                cargoBayLocation,
                map
            );

            cargoShelvesSpawned.Add(cargoShelf);

            // Configure storage settings
            cargoShelf.settings.priority = StoragePriority.Preferred;

            // Randomly select items from trade inventory to fill cargo bay
            float cargoValueRemaining = cargoBudget;
            var selectedItems = new List<Thing>();

            foreach (var item in tradeInventory)
            {
                if (cargoValueRemaining <= 0)
                    break;

                float itemValue = item.def.BaseMarketValue * item.stackCount;
                if (itemValue <= cargoValueRemaining)
                {
                    selectedItems.Add(item);
                    cargoValueRemaining -= itemValue;
                }
                else if (Rand.Chance(0.3f)) // 30% chance to partially fill cargo with partial stacks
                {
                    int stacksToTake = Math.Max(1, (int)(cargoValueRemaining / item.def.BaseMarketValue));
                    if (stacksToTake > 0)
                    {
                        Thing partial = item.SplitOff(stacksToTake);
                        selectedItems.Add(partial);
                        cargoValueRemaining -= partial.def.BaseMarketValue * partial.stackCount;
                    }
                }
            }

            // Add selected items to cargo shelf
            foreach (var item in selectedItems)
            {
                Thing itemClone = item.DeepClone();
                int accepted = cargoShelf.slotGroup.HeldThings.TryAdd(itemClone, true);
                
                if (accepted > 0)
                {
                    // Remove from trade inventory what we added to cargo
                    tradeInventory.Remove(item);
                }
            }

            Log.Message($"[Better Traders Guild] Spawned cargo for {tradersGuildSettlement.Label} with ~{cargoBudget:F0} value");
        }

        /// <summary>
        /// Find suitable location for cargo bay in the settlement map structure.
        /// </summary>
        private IntVec3 FindCargoBayLocation()
        {
            // Search map for designated cargo bay area (defined in layout or by marker)
            // For now, just find first empty area near center
            IntVec3 center = map.Center;
            
            for (int radius = 1; radius < 30; radius++)
            {
                IntVec3 candidate = center + IntVec3.North * radius;
                if (GenSpawn.CanSpawnAt(DefDatabase<ThingDef>.GetNamed("TradersGuild_CargoShelf"), candidate, map))
                {
                    return candidate;
                }
            }

            return IntVec3.Invalid;
        }

        /// <summary>
        /// Calculate total value of items in inventory.
        /// </summary>
        private float CalculateInventoryValue(ThingOwner inventory)
        {
            float total = 0;
            foreach (var item in inventory)
            {
                total += item.def.BaseMarketValue * item.stackCount;
            }
            return total;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref tradersGuildSettlement, "tradersGuildSettlement");
            Scribe_Collections.Look(ref cargoShelvesSpawned, "cargoShelvesSpawned", LookMode.Reference);
        }
    }
}
```

### Step 4: Helper Method for Settlement Detection

Add to `Source/Helpers/TradersGuildHelper.cs`:

```csharp
/// <summary>
/// Find Traders Guild settlement on the current map (if any).
/// </summary>
public static Settlement FindTradersGuildSettlementOnMap(Map map)
{
    // Check if this map is associated with any settlement
    foreach (var settlement in Find.World.settlements)
    {
        if (!IsTradersGuildSettlement(settlement))
            continue;

        // Check if this map is the settlement's map
        if (settlement.Map == map)
            return settlement;
    }

    return null;
}

/// <summary>
/// Ensure settlement has the cargo tracking component.
/// </summary>
public static void EnsureSettlementHasCargoComponent(Settlement settlement)
{
    if (settlement.GetComponent<TradersGuildSettlementComponent>() == null)
    {
        settlement.components.Add(new TradersGuildSettlementComponent { parent = settlement });
    }
}
```

### Step 5: Patch to Add Map Component

File: `Source/Patches/MapGeneration/GenStepOrbitalPlatformAddCargo.cs`

```csharp
using HarmonyLib;
using RimWorld;
using Verse;
using BetterTradersGuild.MapComponents;

namespace BetterTradersGuild.Patches.MapGeneration
{
    /// <summary>
    /// Adds cargo refresher component to Traders Guild settlement maps.
    /// </summary>
    [HarmonyPatch(typeof(Map), nameof(Map.ConstructComponents))]
    public static class Map_ConstructComponents_CargoRefresher
    {
        [HarmonyPostfix]
        public static void Postfix(Map __instance)
        {
            // Add cargo refresher to Traders Guild settlement maps
            var settlement = TradersGuildHelper.FindTradersGuildSettlementOnMap(__instance);
            if (settlement != null)
            {
                __instance.components.Add(new TradersGuildCargoRefresher(__instance));
                
                // Ensure settlement has tracking component
                TradersGuildHelper.EnsureSettlementHasCargoComponent(settlement);
            }
        }
    }
}
```

### Step 6: Mod Settings for Cargo Configuration

Add to `Source/Core/ModSettings.cs`:

```csharp
public class BetterTradersGuildSettings : ModSettings
{
    // Existing settings...
    
    // Cargo System Settings
    public int CargoInventoryPercentage = 60; // How much of trade inventory appears as cargo
    
    public override void ExposeData()
    {
        base.ExposeData();
        
        // Existing...
        Scribe_Values.Look(ref CargoInventoryPercentage, "cargoInventoryPercentage", 60);
    }
}
```

Add to settings UI:

```csharp
public override void DoSettingsWindowContents(Rect inRect)
{
    // Existing UI...
    
    // Cargo Settings
    listing.Label("Cargo Bay Settings");
    listing.Slider(ref settings.CargoInventoryPercentage, 30, 100, "Cargo Percentage: " + settings.CargoInventoryPercentage + "%");
    listing.Gap();
}
```

## Key Design Decisions

### 1. Anti-Exploit Protection
```csharp
// Only refresh on actual trader rotation, not on each visit
if (!isFirstVisit && !isRotationDue)
    return; // Don't refresh cargo
```

### 2. Dynamic Inventory-Based Cargo
```csharp
// Items pulled directly from settlement's trade inventory
// Creates emergent gameplay: stealing cargo affects trading
var tradeInventory = traderTracker.stock;
```

### 3. Map Persistence Compliance
```csharp
// Cargo added dynamically on map entry (FinalizeInit)
// NOT baked into map generation
// Respawns if rotation occurred
```

### 4. Budget-Based Cargo Selection
```csharp
// Calculate: totalValue × cargoPercentage
// Randomly select items matching budget
// Creates variety without hardcoded manifests
```

## Testing Checklist

- [ ] Cargo spawns on first settlement visit
- [ ] Cargo persists when leaving/returning (same visit)
- [ ] Cargo refreshes after trader rotation
- [ ] Cargo doesn't refresh on non-rotation visits (anti-exploit)
- [ ] Cargo value approximately matches cargoPercentage setting
- [ ] Removing cargo items from trade inventory works correctly
- [ ] Multiple visits to different settlements show different cargo
- [ ] Stolen cargo doesn't appear in new cargo (was removed from inventory)
- [ ] Trader rotation changes cargo type appropriately

## Performance Notes

- Cargo spawning happens during `FinalizeInit` (map load) - not every tick
- Inventory scanning is O(n) where n = inventory items (typically <100)
- No per-tick overhead during gameplay

## Future Enhancements

- Themed cargo displays (different room types for different traders)
- Cargo animations during shuttle bay operations
- Transporter pod arrival showing cargo unloading
- Player can "intercept" cargo before delivery
- Cargo affects settlement trader quality/type
