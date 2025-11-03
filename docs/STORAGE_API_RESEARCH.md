# RimWorld Container/Storage API Research

## Overview

RimWorld uses a sophisticated container architecture with two main patterns:
1. **Building-based storage** (Building_Storage) - For shelves, outfit stands, bookshelves
2. **Component-based storage** (CompThingContainer) - For individual items with containers

---

## 1. CONTAINER ARCHITECTURE

### 1.1 Building_Storage (Main Storage Building)

**Class:** `RimWorld.Building_Storage`
**Namespace:** RimWorld
**Inheritance:** `Verse.Building`
**Interfaces:** 
- `RimWorld.ISlotGroupParent`
- `RimWorld.IStoreSettingsParent`
- `RimWorld.IHaulDestination`
- `RimWorld.IStorageGroupMember`
- `RimWorld.IHaulEnroute`
- `Verse.ILoadReferenceable`

**Key Fields:**
```csharp
public RimWorld.StorageSettings settings;        // Storage configuration
public RimWorld.StorageGroup storageGroup;       // Group assignment
public string label;                              // Building label
public RimWorld.SlotGroup slotGroup;             // Slot management
```

**Key Properties:**
- `SlotGroup` - Returns the SlotGroup for item placement
- `StorageSettings` - Configures what items can be stored

### 1.2 Building_Casket (Specialized Container)

**Class:** `RimWorld.Building_Casket`
**Inheritance:** `Verse.Building`
**Interfaces:**
- `Verse.IThingHolder`
- `RimWorld.IOpenable`
- `RimWorld.ISearchableContents`

**Key Fields:**
```csharp
protected Verse.ThingOwner innerContainer;  // Contains items
protected bool contentsKnown;               // Known contents flag
public string openedSignal;                 // Signal on open
```

**Key Properties:**
- `HasAnyContents` - Check if container has items
- `ContainedThing` - Get first item in container
- `CanOpen` - Can this be opened?
- `SearchableContents` - Returns inner container for search

**Key Methods:**
- `Open()` - Open the casket (ejects contents)
- `EjectContents()` - Remove all items

### 1.3 Building_Bookcase (Specialized Storage)

**Class:** `RimWorld.Building_Bookcase`
**Inheritance:** `Verse.Building`
**Special:** Extends Building_Storage for book-only storage

**Key XML Properties:**
```xml
<storageGroupTag>Bookcase</storageGroupTag>
<maxItemsInCell>5</maxItemsInCell>
<fixedStorageSettings>
  <filter>
    <categories Inherit="False">
      <li>Books</li>  <!-- Only books -->
    </categories>
  </filter>
</fixedStorageSettings>
```

### 1.4 Building_OutfitStand (Specialized Storage)

**Class:** `RimWorld.Building_OutfitStand`
**Inheritance:** Likely extends Building_Storage
**Special:** Apparel-only storage stand

**Key XML Properties:**
```xml
<storageGroupTag>OutfitStand</storageGroupTag>
<fixedStorageSettings>
  <filter>
    <categories Inherit="False">
      <li>Apparel</li>
      <li>Weapons</li>
    </categories>
  </filter>
</fixedStorageSettings>
```

### 1.5 CompThingContainer (Component-Based Container)

**Class:** `RimWorld.CompThingContainer`
**Inheritance:** `Verse.ThingComp`
**Interfaces:**
- `Verse.IThingHolder`
- `RimWorld.ISearchableContents`

**Key Fields:**
```csharp
public Verse.ThingOwner innerContainer;  // Container holding items
```

**Key Properties:**
- `ContainedThing` - First item in container (or null)
- `Empty` - Is container empty?
- `Full` - Is container full?
- `TotalStackCount` - Stack count of items
- `SearchableContents` - Access to inner container

**Used For:**
- Genepack containers
- Egg boxes
- Other specialized item containers

---

## 2. THINGOWNER - Core Container System

**Class:** `Verse.ThingOwner`
**Inheritance:** Abstract base class
**Generic:** `ThingOwner<T>` where T is Thing

**Purpose:** Universal list management for items inside containers

### 2.1 ThingOwner Methods for Adding Items

```csharp
// Add single item
public int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = false);
public bool TryAdd(Thing item, bool canMergeWithExistingStacks = false);

// Add or merge
public int TryAddOrTransfer(Thing item, int count, bool canMergeWithExistingStacks = false);
public bool TryAddOrTransfer(Thing item, bool canMergeWithExistingStacks = false);

// Add multiple
public void TryAddRangeOrTransfer(IEnumerable<Thing> things, bool canMergeWithExistingStacks = false, bool destroyLeftover = false);

// Check capacity
public bool CanAcceptAnyOf(Thing item, bool canMergeWithExistingStacks = false);
public int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = false);
```

### 2.2 ThingOwner Properties

```csharp
public IThingHolder Owner { get; }              // Parent container
public int Count { get; }                       // Item count
public Thing Item[int index] { get; }           // Access by index
public bool Any { get; }                        // Has items?
public int TotalStackCount { get; }             // Total stack count
public string ContentsString { get; }           // Debug string
```

### 2.3 ThingOwner Management

```csharp
public void Clear();                            // Remove all
public void ClearAndDestroyContents(DestroyMode mode = Normal);
public int RemoveAll(Predicate<Thing> predicate);
public bool Remove(Thing item);
public bool Contains(Thing item);
public bool Contains(ThingDef def);
public bool Contains(ThingDef def, int minCount);
```

### 2.4 Constructor Variants

```csharp
// Default with no parent
public ThingOwner();

// With parent holder
public ThingOwner(IThingHolder owner);

// With settings
public ThingOwner(
    IThingHolder owner, 
    bool oneStackOnly = false, 
    LookMode contentsLookMode = Reference,
    bool removeContentsIfDestroyed = true
);
```

### 2.5 Transfer Operations

```csharp
public bool TryTransferToContainer(
    Thing item, 
    ThingOwner otherContainer, 
    bool canMergeWithExistingStacks = false
);

public int TryTransferToContainer(
    Thing item, 
    ThingOwner otherContainer, 
    int count, 
    bool canMergeWithExistingStacks = false
);

public void TryTransferAllToContainer(
    ThingOwner other, 
    bool canMergeWithExistingStacks = false
);
```

---

## 3. STORAGE SETTINGS SYSTEM

### 3.1 StorageSettings Class

**Class:** `RimWorld.StorageSettings`
**Parent:** IStoreSettingsParent interface

**Key Fields:**
```csharp
public IStoreSettingsParent owner;      // Owner reference
public ThingFilter filter;              // Item filtering
private StoragePriority priorityInt;    // Storage priority
```

**Key Properties:**
```csharp
public StoragePriority Priority { get; set; }
```

**Methods:**
```csharp
public bool AllowedToAccept(ThingDef def);
```

### 3.2 StoragePriority Enum

```csharp
enum StoragePriority
{
    Unstored = 0,
    Low = 1,
    Normal = 2,
    Preferred = 3,
    Important = 4,
    Critical = 5
}
```

### 3.3 ThingFilter System

**Purpose:** Categorize what items are allowed

**Categories:**
- Root (everything)
- Foods
- Manufactured
- ResourcesRaw
- Items
- Weapons
- Apparel
- BodyParts
- Books
- Plants
- Chunks
- Buildings

**Disallowed Examples:**
```xml
<disallowedCategories>
  <li>Chunks</li>
  <li>Plants</li>
  <li>Buildings</li>
</disallowedCategories>

<specialFiltersToDisallow>
  <li>AllowLargeCorpses</li>
  <li>AllowChildOnlyApparel</li>
</specialFiltersToDisallow>
```

### 3.4 IStoreSettingsParent Interface

**Methods:**
```csharp
StorageSettings GetStoreSettings();
StorageSettings GetParentStoreSettings();
void Notify_StorageSettingsChanged();
```

### 3.5 ISlotGroupParent Interface

**Key Methods:**
```csharp
bool IgnoreStoredThingsBeauty { get; }
IEnumerable<IntVec3> AllSlotCells();
List<IntVec3> AllSlotCellsList();
void Notify_ReceivedThing(Thing newItem);
void Notify_LostThing(Thing newItem);
string SlotYielderLabel();
string GroupingLabel { get; }
int GroupingOrder { get; }
SlotGroup GetSlotGroup();
```

---

## 4. XML DEFINITIONS

### 4.1 Shelf Definition Pattern

```xml
<ThingDef Name="StorageShelfBase" ParentName="ShelfBase" Abstract="True">
  <thingClass>Building_Storage</thingClass>
  
  <building>
    <storageGroupTag>Shelf</storageGroupTag>
    
    <!-- Fixed settings (what this ALWAYS allows) -->
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
        </disallowedCategories>
      </filter>
    </fixedStorageSettings>
    
    <!-- Default settings (player can customize) -->
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
          <li>BodyParts</li>
        </categories>
      </filter>
    </defaultStorageSettings>
  </building>
  
  <inspectorTabs>
    <li>ITab_Storage</li>
  </inspectorTabs>
</ThingDef>
```

### 4.2 Bookcase Definition Pattern

```xml
<ThingDef Abstract="True" Name="BookcaseBase" ParentName="ShelfBase">
  <thingClass>Building_Bookcase</thingClass>
  
  <building>
    <storageGroupTag>Bookcase</storageGroupTag>
    <maxItemsInCell>5</maxItemsInCell>
    <blueprintClass>Blueprint_StorageWithRoomHighlight</blueprintClass>
    <ignoreStoredThingsBeauty>false</ignoreStoredThingsBeauty>
    
    <fixedStorageSettings>
      <filter>
        <categories Inherit="False">
          <li>Books</li>  <!-- ONLY books -->
        </categories>
      </filter>
    </fixedStorageSettings>
    
    <defaultStorageSettings>
      <priority>Important</priority>
      <filter>
        <categories>
          <li>Books</li>
        </categories>
      </filter>
    </defaultStorageSettings>
  </building>
  
  <inspectorTabs>
    <li>ITab_ContentsBooks</li>
  </inspectorTabs>
</ThingDef>
```

### 4.3 Outfit Stand Definition Pattern

```xml
<ThingDef ParentName="FurnitureBase" Name="OutfitStandBase" Abstract="True">
  <thingClass>Building_OutfitStand</thingClass>
  <drawerType>RealtimeOnly</drawerType>
  
  <building>
    <blueprintClass>Blueprint_StorageWithRoomHighlight</blueprintClass>
    <ignoreStoredThingsBeauty>false</ignoreStoredThingsBeauty>
    
    <fixedStorageSettings>
      <filter>
        <disallowNotEverStorable>true</disallowNotEverStorable>
        <categories Inherit="False">
          <li>Apparel</li>
          <li>Weapons</li>
        </categories>
      </filter>
    </fixedStorageSettings>
    
    <defaultStorageSettings>
      <priority>Important</priority>
      <filter>
        <categories>
          <li>Apparel</li>
        </categories>
        <disallowedCategories>
          <li>ApparelUtility</li>
          <li>Weapons</li>
        </disallowedCategories>
      </filter>
    </defaultStorageSettings>
  </building>
  
  <inspectorTabs>
    <li>ITab_Storage</li>
    <li>ITab_ContentsOutfitStand</li>
  </inspectorTabs>
</ThingDef>
```

### 4.4 Key XML Properties

| Property | Purpose | Example |
|----------|---------|---------|
| `<thingClass>` | Which C# class to instantiate | `Building_Storage` |
| `<storageGroupTag>` | Grouping identifier | `Shelf`, `Bookcase`, `OutfitStand` |
| `<maxItemsInCell>` | Items per cell | `3` for shelf, `5` for bookcase |
| `<fixedStorageSettings>` | Immutable filters | Always required categories |
| `<defaultStorageSettings>` | Player-customizable defaults | Initial priority and filters |
| `<priority>` | Haul priority | `Unstored`, `Low`, `Normal`, `Preferred`, `Important`, `Critical` |
| `<filter>` | Item categorization | Allowed/disallowed thing defs |
| `<inspectorTabs>` | UI tabs in inspector | `ITab_Storage`, `ITab_ContentsBooks` |

---

## 5. MAP GENERATION INTEGRATION

### 5.1 Spawning Buildings with Items

**Pattern 1: Using GenSpawn**
```csharp
// Create building
Thing building = ThingMaker.MakeThing(buildingDef, stuff);
building = GenSpawn.Spawn(
    building, 
    position, 
    map, 
    rotation: Rot4.South,
    wipeMode: WipeMode.Decon
);
```

**Pattern 2: For Building_Storage**
```csharp
var storage = (Building_Storage)GenSpawn.Spawn(
    ThingMaker.MakeThing(ThingDefOf.Shelf),
    position,
    map
);

// Configure storage settings
storage.settings.priority = StoragePriority.Important;
storage.settings.filter.SetDisallowAll();
storage.settings.filter.SetAllow(ThingDefOf.Steel, true);
```

### 5.2 Adding Items to Containers

**Pattern 1: Direct ThingOwner access**
```csharp
var storage = (Building_Storage)building;
ThingOwner innerContainer = ...; // Get from building

// Add item
int accepted = innerContainer.TryAdd(item, item.stackCount);
if (accepted < item.stackCount) {
    // Handle overflow
}
```

**Pattern 2: Using IThingHolder**
```csharp
var casket = (Building_Casket)building;
ThingOwner contents = casket.GetDirectlyHeldThings();
contents.TryAdd(item);
```

**Pattern 3: Component Container**
```csharp
CompThingContainer compContainer = building.GetComp<CompThingContainer>();
if (compContainer != null) {
    compContainer.innerContainer.TryAdd(item);
}
```

### 5.3 BaseGen Symbol Resolver Example

```csharp
public class SymbolResolver_StorageWithItems : SymbolResolver
{
    public override void Resolve(ResolveParams rp)
    {
        // Spawn the storage building
        Building_Storage storage = (Building_Storage)GenSpawn.Spawn(
            ThingMaker.MakeThing(ThingDefOf.Shelf),
            rp.rect.CenterCell,
            rp.map
        );
        
        // Configure storage
        storage.settings.filter.SetAllow(ThingDefOf.Steel, true);
        storage.settings.filter.SetAllow(ThingDefOf.Gold, true);
        
        // Add items
        Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 100;
        storage.slotGroup.HeldThings.TryAdd(steel);
        
        Thing gold = ThingMaker.MakeThing(ThingDefOf.Gold);
        gold.stackCount = 50;
        storage.slotGroup.HeldThings.TryAdd(gold);
    }
}
```

---

## 6. STORAGE DIFFERENCES

### 6.1 General Storage (Shelves)

- **Class:** Building_Storage
- **Capacity:** 3x normal ground storage per cell
- **Filter:** All categories except chunks/plants/buildings
- **Properties:**
  - `maxItemsInCell: 3`
  - `preventDeteriorationOnTop: true`
  - `ignoreStoredThingsBeauty: true`
- **Use Case:** General-purpose item storage

### 6.2 Bookcase Storage

- **Class:** Building_Bookcase
- **Capacity:** 5-10 books
- **Filter:** Books ONLY
- **Properties:**
  - `maxItemsInCell: 5`
  - `ignoreStoredThingsBeauty: false` (beauty affects beauty stat)
  - `drawGraphics: true` (books visible on shelf)
- **Special:** Provides bonus to academic work nearby
- **Use Case:** Book storage and display

### 6.3 Outfit Stand Storage

- **Class:** Building_OutfitStand
- **Capacity:** 1 outfit (multiple apparel pieces)
- **Filter:** Apparel only (no weapons/utilities typically)
- **Properties:**
  - Single outfit display
  - Shows clothes on mannequin
  - Quick-change functionality
- **Special:** UI tabs for outfit management
- **Use Case:** Apparel management and storage

### 6.4 Specialized Containers (CompThingContainer)

- **Class:** CompThingContainer (as ThingComp)
- **Parent:** Any building with the component
- **Capacity:** Configurable per component (stackLimit)
- **Use Cases:**
  - Genepack containers
  - Egg boxes
  - Cargo containers
- **Advantage:** Can be added to any building via XML comp definition

---

## 7. CODE PATTERNS

### 7.1 Creating and Populating Storage

```csharp
// Step 1: Create the building
var buildingDef = DefDatabase<ThingDef>.GetNamed("Shelf");
Building_Storage storage = (Building_Storage)GenSpawn.Spawn(
    ThingMaker.MakeThing(buildingDef, ThingDefOf.Steel),
    new IntVec3(10, 0, 10),
    map
);

// Step 2: Configure storage settings (optional)
storage.settings.priority = StoragePriority.Important;

// Step 3: Create items
Thing itemStack = ThingMaker.MakeThing(ThingDefOf.Steel);
itemStack.stackCount = 100;

// Step 4: Add to storage
storage.slotGroup.HeldThings.TryAdd(itemStack, true);

// Step 5: Verify
Log.Message($"Storage now contains: {storage.slotGroup.HeldThings.TotalStackCount}");
```

### 7.2 Accessing Storage Contents

```csharp
// Get all things in storage
Building_Storage storage = building as Building_Storage;
if (storage != null) {
    var contents = storage.slotGroup.HeldThings;
    foreach (Thing thing in contents) {
        Log.Message($"Found: {thing.Label} x{thing.stackCount}");
    }
}

// Check specific thing
int steelCount = storage.slotGroup.HeldThings
    .FindAll(t => t.def == ThingDefOf.Steel)
    .Sum(t => t.stackCount);
```

### 7.3 Building_Casket Pattern

```csharp
Building_Casket casket = (Building_Casket)GenSpawn.Spawn(
    ThingMaker.MakeThing(ThingDefOf.CryptosleepCasket),
    position,
    map
);

// Add item
Thing item = ThingMaker.MakeThing(ThingDefOf.Gold);
casket.GetDirectlyHeldThings().TryAdd(item);

// Check contents
if (casket.HasAnyContents) {
    Thing stored = casket.ContainedThing;
    Log.Message($"Casket contains: {stored.Label}");
}
```

### 7.4 Component Container Pattern

```csharp
// Create building with component
Thing container = ThingMaker.MakeThing(containerDef);
CompThingContainer comp = container.GetComp<CompThingContainer>();

if (comp != null) {
    // Check state
    if (comp.Empty) {
        Log.Message("Container is empty");
    }
    
    if (!comp.Full) {
        // Add items
        Thing item = ThingMaker.MakeThing(itemDef);
        comp.innerContainer.TryAdd(item);
    }
    
    // Get info
    Log.Message($"Container has {comp.TotalStackCount} items");
}
```

---

## 8. KEY TAKEAWAYS FOR MOD DEVELOPMENT

### For TradersGuild Cargo Bay Implementation:

1. **Use Building_Storage base**: Extends existing functionality
   - Automatic slot management
   - Built-in haul system
   - Inspector tabs work automatically

2. **Storage Filter Configuration**:
   ```xml
   <!-- Allow specific trader type items -->
   <fixedStorageSettings>
     <filter>
       <categories>
         <li>Items</li>
         <li>Resources</li>
       </categories>
     </filter>
   </fixedStorageSettings>
   ```

3. **Spawn Pattern for Dynamic Cargo**:
   ```csharp
   // Generate cargo based on trader inventory
   Settlement settlement = GetSettlement();
   var tradeInventory = settlement.trader.stock;
   
   // Spawn storage building
   Building_Storage bay = SpawnCargoBay(location, map);
   
   // Add items from inventory
   foreach (Thing thing in SelectRandomItems(tradeInventory, cargoPercentage)) {
       bay.slotGroup.HeldThings.TryAdd(thing.DeepClone());
       tradeInventory.Remove(thing); // Remove from trade stock
   }
   ```

4. **Anti-Exploit Cargo Refresh**:
   - Track `lastCargoRefreshTicks` on settlement
   - Only refresh when trader rotates (not on each visit)
   - Despawn old → respawn new (consistent with theme)

5. **Map Persistence**:
   - Cargo NOT part of map generation
   - Added dynamically on map entry
   - Despawned/respawned on rotation

---

## 9. FILE PATHS & REFERENCES

**RimWorld DLL:**
- `/RimWorldWin64_Data/Managed/Assembly-CSharp.dll`

**Key Classes in DLL:**
- `RimWorld.Building_Storage`
- `RimWorld.Building_Casket`
- `RimWorld.Building_Bookcase`
- `RimWorld.CompThingContainer`
- `Verse.ThingOwner`
- `RimWorld.StorageSettings`

**Example XML Locations:**
- Core shelves: `Data/Core/Defs/ThingDefs_Buildings/Buildings_Furniture.xml`
- Odyssey outfit stands: `Data/Odyssey/Defs/ThingDefs_Buildings/Buildings_Furniture.xml`

