using System.Collections.Generic;
using BetterTradersGuild.DefRefs;
using BetterTradersGuild.Helpers.RoomContents;
using RimWorld;
using Verse;

namespace BetterTradersGuild.RoomContents.MedicalBay
{
    /// <summary>
    /// Fills medical bay shelves with medicines and combat drugs.
    /// Contents match the original prefab definition:
    /// - Industrial medicine (8-10, guaranteed)
    /// - Ultratech medicine (6-8, 50% chance)
    /// - Luciferium (6-9, 50% chance)
    /// - Go-juice (6-9, guaranteed)
    /// - VRE Antibiotics (6-9, 50% chance, if mod present)
    /// </summary>
    public static class MedicineShelfFiller
    {
        /// <summary>
        /// Finds all 2-cell wide shelves in the room and fills them with medical supplies.
        /// </summary>
        public static void FillMedicineShelves(Map map, CellRect roomRect)
        {
            List<Building_Storage> shelves = RoomShelfHelper.GetShelvesInRoom(map, roomRect, Things.Shelf, 2);

            foreach (Building_Storage shelf in shelves)
            {
                FillShelfWithMedicalSupplies(map, shelf);
            }
        }

        private static void FillShelfWithMedicalSupplies(Map map, Building_Storage shelf)
        {
            // Industrial medicine - guaranteed (8-10)
            if (Things.MedicineIndustrial != null)
            {
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.MedicineIndustrial, Rand.RangeInclusive(8, 10));
            }

            // Ultratech medicine - 50% chance (6-8)
            if (Things.MedicineUltratech != null && Rand.Chance(0.5f))
            {
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.MedicineUltratech, Rand.RangeInclusive(6, 8));
            }

            // Luciferium - 50% chance (6-9)
            if (Things.Luciferium != null && Rand.Chance(0.5f))
            {
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.Luciferium, Rand.RangeInclusive(6, 9));
            }

            // Go-juice - guaranteed (6-9)
            if (Things.GoJuice != null)
            {
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.GoJuice, Rand.RangeInclusive(6, 9));
            }

            // VRE Antibiotics - 50% chance if mod present (6-9)
            if (Things.VRE_Antibiotics != null && Rand.Chance(0.5f))
            {
                RoomShelfHelper.AddItemsToShelf(map, shelf, Things.VRE_Antibiotics, Rand.RangeInclusive(6, 9));
            }
        }
    }
}
