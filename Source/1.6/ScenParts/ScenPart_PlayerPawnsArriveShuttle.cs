using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterTradersGuild.ScenParts
{
    public class ScenPart_PlayerPawnsArriveShuttle : ScenPart
    {
        public ThingDef shuttleDef;
        public float initialFuelPct = 0f;
        public ColorDef paintColor;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref shuttleDef, "shuttleDef");
            Scribe_Values.Look(ref initialFuelPct, "initialFuelPct");
            Scribe_Defs.Look(ref paintColor, "paintColor");
        }

        public override string Summary(Scenario scen)
        {
            return ScenSummaryList.SummaryWithList(
                scen,
                ScenPart_StartingThing_Defined.PlayerStartWithTag,
                ScenPart_StartingThing_Defined.PlayerStartWithIntro);
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == ScenPart_StartingThing_Defined.PlayerStartWithTag)
            {
                ThingDef resolved = shuttleDef ?? Things.PassengerShuttle;
                yield return resolved.label.CapitalizeFirst();
            }
        }

        public override void GenerateIntoMap(Map map)
        {
            if (Find.GameInitData == null)
                return;

            List<Thing> allThings = new List<Thing>();

            foreach (Pawn pawn in Find.GameInitData.startingAndOptionalPawns)
                allThings.Add(pawn);

            foreach (ScenPart part in Find.Scenario.AllParts)
                allThings.AddRange(part.PlayerStartingThings());

            foreach (Pawn pawn in Find.GameInitData.startingAndOptionalPawns)
            {
                foreach (ThingDefCount tdc in Find.GameInitData.startingPossessions[pawn])
                    allThings.Add(StartingPawnUtility.GenerateStartingPossession(tdc));
            }

            foreach (Thing thing in allThings)
            {
                if (thing.def.CanHaveFaction)
                    thing.SetFactionDirect(Faction.OfPlayer);
            }

            ThingDef resolvedShuttleDef = shuttleDef ?? Things.PassengerShuttle;
            Thing shipThing = ThingMaker.MakeThing(resolvedShuttleDef);
            shipThing.SetFactionDirect(Faction.OfPlayer);

            // Derive the TransportShipDef from the shuttle's CompProperties_Shuttle
            var shuttleProps = resolvedShuttleDef.comps?.OfType<CompProperties_Shuttle>().FirstOrDefault();
            TransportShipDef transportShipDef = shuttleProps?.shipDef
                ?? TransportShips.Ship_PassengerShuttle;

            TransportShip ship = TransportShipMaker.MakeTransportShip(
                transportShipDef,
                allThings,
                shipThing);

            // Try to land near PlayerStartSpot so the shuttle is in the initial camera viewport
            IntVec2 shuttleSize = resolvedShuttleDef.Size;
            IntVec3 landingSpot;
            if (MapGenerator.PlayerStartSpotValid
                && DropCellFinder.TryFindDropSpotNear(
                    MapGenerator.PlayerStartSpot, map, out landingSpot,
                    allowFogged: false, canRoofPunch: false,
                    allowIndoors: false, size: shuttleSize + new IntVec2(2, 2))
                && DropCellFinder.SkyfallerCanLandAt(landingSpot, map, shuttleSize, Faction.OfPlayer))
            {
                // landingSpot set by TryFindDropSpotNear
            }
            else
            {
                landingSpot = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
            }
            // Retarget the initial camera to the landing spot in case the shuttle
            // resolved far from the original PlayerStartSpot (e.g. crowded biomes).
            MapGenerator.PlayerStartSpot = landingSpot;

            ship.ArriveAt(landingSpot, map.Parent);

            // Set skyfaller rotation so the shuttle flies in from the west
            // PassengerShuttleIncoming derives its flight angle from Rot4, not the angle field
            Skyfaller skyfaller = map.listerThings.ThingsOfDef(
                transportShipDef.arrivingSkyfaller).OfType<Skyfaller>().FirstOrDefault();
            if (skyfaller != null)
                skyfaller.Rotation = Rot4.East;

            // Set shuttle building to face east when it lands
            shipThing.Rotation = Rot4.East;

            ship.AddJob(ShipJobDefOf.Unload);

            // Set fuel level on the shuttle Thing before it lands.
            // CompRefuelable.Initialize sets fuel = fuelCapacity * Props.initialFuelPercent,
            // which defaults to 0. We set the private field directly via Traverse to avoid
            // the difficulty multiplier baked into Refuel(float).
            if (initialFuelPct > 0f)
            {
                CompRefuelable fuelComp = shipThing.TryGetComp<CompRefuelable>();
                if (fuelComp != null)
                {
                    float targetFuel = fuelComp.Props.fuelCapacity * initialFuelPct;
                    Traverse.Create(fuelComp).Field("fuel").SetValue(targetFuel);
                }
            }

            // Apply paint color
            if (paintColor != null && shipThing is Building building)
            {
                building.ChangePaint(paintColor);
            }
        }
    }
}
