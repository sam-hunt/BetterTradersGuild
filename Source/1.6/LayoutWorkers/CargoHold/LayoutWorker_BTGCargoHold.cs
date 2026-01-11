using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using RimWorld;
using Verse;

namespace BetterTradersGuild.LayoutWorkers.CargoHold
{
    /// <summary>
    /// Custom LayoutWorker for the cargo hold pocket map.
    ///
    /// Creates a simple single-room enclosed structure with:
    /// - Walls around the perimeter
    /// - AncientBlastDoor in the middle of each wall (4 doors for bridge access)
    /// - Floor tiles inside
    /// - Single room for contents (shelves, turrets, exit portal)
    ///
    /// This is specifically designed for small pocket maps where
    /// LayoutWorker_AncientStockpile's multi-room dungeon generation
    /// is overkill and fails due to size constraints.
    ///
    /// Extends LayoutWorker_Structure which handles:
    /// - Wall/door/floor sketch generation from our layout
    /// - Room content resolution (prefabs, parts, RoomContentsWorkers)
    /// - Proper spawning with faction ownership
    /// </summary>
    public class LayoutWorker_BTGCargoHold : LayoutWorker_Structure
    {
        public LayoutWorker_BTGCargoHold(LayoutDef def) : base(def)
        {
        }

        /// <summary>
        /// Creates the structure layout - a simple rectangular room with walls and a door.
        /// This is called by the base class during GenerateStructureSketch().
        /// </summary>
        protected override StructureLayout GetStructureLayout(StructureGenParams parms, CellRect rect)
        {
            // Create a new LayoutStructureSketch to hold our layout
            LayoutStructureSketch sketch = parms.sketch ?? new LayoutStructureSketch();
            StructureLayout layout = new StructureLayout(sketch, rect);

            // Add walls around the entire perimeter
            foreach (IntVec3 cell in rect.EdgeCells)
                layout.Add(cell, RoomLayoutCellType.Wall);

            // Add doors in the middle of each wall (for bridge access)
            int centerX = rect.CenterCell.x;
            int centerZ = rect.CenterCell.z;
            layout.Add(new IntVec3(centerX, 0, rect.maxZ), RoomLayoutCellType.Door);
            layout.Add(new IntVec3(centerX, 0, rect.minZ), RoomLayoutCellType.Door);
            layout.Add(new IntVec3(rect.maxX, 0, centerZ), RoomLayoutCellType.Door);
            layout.Add(new IntVec3(rect.minX, 0, centerZ), RoomLayoutCellType.Door);

            // Add floor inside the walls
            CellRect interiorRect = rect.ContractedBy(1);
            foreach (IntVec3 cell in interiorRect)
                layout.Add(cell, RoomLayoutCellType.Floor);

            // Create a single room covering the full structure (including walls)
            // Room rects are inclusive of edge wall cells - the layout system uses
            // RoomLayoutCellType to distinguish walls from floors
            List<CellRect> roomRects = new List<CellRect> { rect };

            // Get the room def for this vault
            LayoutRoomDef roomDef = LayoutRooms.BTG_CargoHoldVaultRoom;
            if (roomDef == null)
                roomDef = DefDatabase<LayoutRoomDef>.AllDefsListForReading.FirstOrFallback();

            // Add the room with its required def
            layout.AddRoom(roomRects, roomDef);

            return layout;
        }
    }
}
