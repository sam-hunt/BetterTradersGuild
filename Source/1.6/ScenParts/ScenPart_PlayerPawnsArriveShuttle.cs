using System.Collections.Generic;
using System.Linq;
using BetterTradersGuild.DefRefs;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterTradersGuild.ScenParts
{
    public class ScenPart_PlayerPawnsArriveShuttle : ScenPart
    {
        public ThingDef shuttleDef;
        public float initialFuelPct = 0f;
        public ColorDef paintColor;

        private int fuelPctDisplay;
        private string fuelBuf;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            ThingDef resolved = shuttleDef ?? Things.PassengerShuttle;
            bool paintable = IsPaintable(resolved);
            float rows = paintable ? 3f : 2f;

            Rect rect = listing.GetScenPartRect(this, RowHeight * rows);
            Rect shuttleRect = new Rect(rect.x, rect.y, rect.width, RowHeight);
            Rect fuelRect = new Rect(rect.x, rect.y + RowHeight, rect.width, RowHeight);

            string shuttleLabel = IsLaunchable(resolved)
                ? resolved.LabelCap.ToString()
                : resolved.LabelCap + " (not launchable)";
            if (Widgets.ButtonText(shuttleRect, shuttleLabel))
            {
                FloatMenuUtility.MakeMenu(
                    PossibleShuttleDefs(),
                    d => IsLaunchable(d) ? d.LabelCap.ToString() : d.LabelCap + " (not launchable)",
                    d => delegate
                    {
                        shuttleDef = d;
                        if (!IsPaintable(d))
                            paintColor = null;
                    }
                );
            }

            // Fuel row - label in left column (vanilla pattern), numeric field full width
            Rect fuelLabelRect = new Rect(rect.x - 200f, fuelRect.y, 200f, RowHeight);
            fuelLabelRect.xMax -= 4f;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(fuelLabelRect, "Fuel %");
            Text.Anchor = TextAnchor.UpperLeft;
            fuelPctDisplay = Mathf.RoundToInt(initialFuelPct * 100f);
            Widgets.TextFieldNumeric(fuelRect, ref fuelPctDisplay, ref fuelBuf, 0f, 100f);
            initialFuelPct = fuelPctDisplay / 100f;

            // Paint row - only shown for paintable shuttles
            if (paintable)
            {
                Rect colorRect = new Rect(rect.x, rect.y + RowHeight * 2f, rect.width, RowHeight);
                Rect paintLabelRect = new Rect(rect.x - 200f, colorRect.y, 200f, RowHeight);
                paintLabelRect.xMax -= 4f;
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(paintLabelRect, "Paint");
                Text.Anchor = TextAnchor.UpperLeft;
                string colorLabel = paintColor != null ? paintColor.LabelCap.ToString() : "No paint";
                if (Widgets.ButtonText(colorRect, colorLabel))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    options.Add(new FloatMenuOption("No paint", delegate { paintColor = null; }));
                    foreach (ColorDef cd in DefDatabase<ColorDef>.AllDefs)
                    {
                        ColorDef local = cd;
                        options.Add(new FloatMenuOption(local.LabelCap, delegate { paintColor = local; }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
        }

        // Replaces vanilla's arrival method; both should not exist in the same scenario.
        public override bool CanCoexistWith(ScenPart other)
        {
            return !(other is ScenPart_PlayerPawnsArriveMethod);
        }

        public override void Randomize()
        {
            shuttleDef = PossibleShuttleDefs().RandomElementWithFallback();
            initialFuelPct = GenMath.RoundedHundredth(Rand.Range(0f, 1f));
            paintColor = IsPaintable(shuttleDef)
                ? DefDatabase<ColorDef>.AllDefs.RandomElementWithFallback()
                : null;
        }

        private IEnumerable<ThingDef> PossibleShuttleDefs()
        {
            return DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c is CompProperties_Shuttle)
                    && FindTransportShipDef(d) != null);
        }

        private static bool IsLaunchable(ThingDef d)
        {
            return d.comps != null && d.comps.Any(c => c is CompProperties_Launchable);
        }

        private static bool IsPaintable(ThingDef d)
        {
            return d?.building != null && d.building.paintable;
        }

        private static TransportShipDef FindTransportShipDef(ThingDef shuttleThingDef)
        {
            return DefDatabase<TransportShipDef>.AllDefs
                .FirstOrDefault(d => d.shipThing == shuttleThingDef);
        }

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

            // Find the TransportShipDef whose shipThing matches our shuttle.
            // Falls back to PassengerShuttle if the shuttle's DLC was removed after save.
            TransportShipDef transportShipDef = FindTransportShipDef(resolvedShuttleDef)
                ?? TransportShips.Ship_PassengerShuttle;
            if (transportShipDef.shipThing != resolvedShuttleDef)
            {
                resolvedShuttleDef = transportShipDef.shipThing;
                shipThing = ThingMaker.MakeThing(resolvedShuttleDef);
                shipThing.SetFactionDirect(Faction.OfPlayer);
            }

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

            // Orient the skyfaller and building to the shuttle's intended facing direction.
            // PassengerShuttle (3,5) has defaultPlacingRot=East; crashed shuttles (5,3)
            // are already sideways at default North. Using defaultPlacingRot handles both.
            Rot4 shuttleRot = resolvedShuttleDef.defaultPlacingRot;
            Skyfaller skyfaller = map.listerThings.ThingsOfDef(
                transportShipDef.arrivingSkyfaller).OfType<Skyfaller>().FirstOrDefault();
            if (skyfaller != null)
                skyfaller.Rotation = shuttleRot;

            shipThing.Rotation = shuttleRot;

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
