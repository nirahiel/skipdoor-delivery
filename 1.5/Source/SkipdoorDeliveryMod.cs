using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VanillaPsycastsExpanded.Skipmaster;
using Verse;
using Verse.Sound;
using VFECore;

namespace SkipdoorDelivery {
    public class CompProperties_TeleportBetweenStockpiles : CompProperties
	{
		public int tickRate;
		public float radius;
		public float fleckScale = 0.5f;

		public CompProperties_TeleportBetweenStockpiles()
		{
			compClass = typeof(CompTeleportBetweenStockpiles);
		}
	}

	internal class SkipdoorZone : IComparable<SkipdoorZone>
	{
		public Skipdoor skipdoor;
		public Zone_Stockpile zone;

		public SkipdoorZone(Skipdoor door, Zone_Stockpile doorZone)
		{
			skipdoor = door;
			zone = doorZone;
		}

		public int CompareTo(SkipdoorZone other)
		{
			return zone.GetStoreSettings().Priority.CompareTo(other.zone.GetStoreSettings().Priority);
		}
	}

	public class CompTeleportBetweenStockpiles : ThingComp
	{
		public CompProperties_TeleportBetweenStockpiles Props => base.props as CompProperties_TeleportBetweenStockpiles;

        private bool isGlobalSkipdoor = LoadedModManager.GetMod<EnhancedSkipdoor>().GetSettings<Settings>().gatesStartGlobal;

        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Values.Look(ref isGlobalSkipdoor, "isGlobalSkipdoor");
        }

        public override void PostDrawExtraSelectionOverlays()
		{
			if (Props.radius > 0.1)
			{
				GenDraw.DrawRadiusRing(parent.Position, Props.radius);
			}
		}

        private IEnumerable<IntVec3> RadialCells => GenRadial.RadialCellsAround(parent.Position, Props.radius, useCenter: true);

        
        public override void CompTick()
		{
			base.CompTick();
			if (Find.TickManager.TicksGame % Props.tickRate != 0) return;

            // The current skipdoor doesn't require a stockpile anymore.
            // Let's find all skipdoors that aren't this one, and see if they have a stockpile.

            var targets = new List<SkipdoorZone>();
            foreach (var skipdoor in WorldComponent_DoorTeleporterManager.Instance.DoorTeleporters.OfType<Skipdoor>()) {
                if (skipdoor != parent &&
                    (skipdoor.Map == parent.Map || (skipdoor.GetComp<CompTeleportBetweenStockpiles>().isGlobalSkipdoor && isGlobalSkipdoor))) {
                    if (skipdoor.Position.GetZone(skipdoor.Map) is Zone_Stockpile stockpile) {
                        targets.Add(new SkipdoorZone(skipdoor, stockpile));
                    }
                }
            }

            if (targets.Count == 0) {
                return;
            }

            targets.Sort();

            var skipExits = new HashSet<Skipdoor>();
            var cellEntries = new HashSet<IntVec3>();
            var mapExits = new Dictionary<Map, HashSet<IntVec3>>();

            Zone_Stockpile skipdoor_zone = null;

            if (parent.Position.GetZone(parent.Map) is Zone_Stockpile zone) {
                skipdoor_zone = zone;
            }

            // For things in that skipdoor's radius, teleports them if the destination is a better place for it
            foreach (var thing in GenRadial
                         .RadialDistinctThingsAround(parent.Position, parent.Map, Props.radius, true)
                         .Where(t => t.def.category == ThingCategory.Item)) {
                
                // Consider all items, whether they're in a stockpile or not
                foreach (var target in targets) {
                    var targetZone = target.zone;
                    // If the target zone can't accept that thing, ignore it
                    if (!targetZone.GetStoreSettings().AllowedToAccept(thing)) continue;

                    // Now the target zone can accept that thing, but there are specific cases.
                    // Do not transfer if the item is in a zone, and that zone accepts the item, and that zone's priority is higher than or equal to the other zone
                    // Do not transfer if the item's haul destination exists but isn't a zone (ignore storage racks and such)

                    var currentHaulDestination = StoreUtility.CurrentHaulDestinationOf(thing);
                    if (currentHaulDestination is Zone_Stockpile
                        && currentHaulDestination.GetStoreSettings().AllowedToAccept(thing)
                        && currentHaulDestination.GetStoreSettings().Priority >= targetZone.GetStoreSettings().Priority) continue;
                    if (currentHaulDestination != null && currentHaulDestination is not Zone_Stockpile) continue;

                    // find all cells that can potentially accept this item
                    var targetCells = targetZone.AllSlotCells().Where(cell =>
                        StoreUtility.IsGoodStoreCell(cell, targetZone.Map, thing, null, parent.Faction)).ToList();

                    if (!targetCells.Any()) continue;

                    var skipdoorCell = target.skipdoor.Position;
                    targetCells.Sort((c1, c2) => c1.DistanceTo(skipdoorCell).CompareTo(c2.DistanceTo(skipdoorCell)));

                    cellEntries.Add(thing.Position);
                    thing.DeSpawn();
                    GenPlace.TryPlaceThing(thing, targetCells.First(), targetZone.Map, ThingPlaceMode.Near);
                    skipExits.Add(target.skipdoor);

                    if (!mapExits.ContainsKey(targetZone.Map)) {
                        mapExits[targetZone.Map] = new HashSet<IntVec3>();
                    }

                    mapExits[targetZone.Map].Add(targetCells.First());
                    break;
                }
            }

            if (skipExits.Any(skipdoor => skipdoor.Map != parent.Map)) {
                SoundDefOf.Psycast_Skip_Entry.PlayOneShot(parent);
            }

            foreach (var skipExit in skipExits) {
                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(skipExit);
            }

            foreach (var cellEntry in cellEntries) {
                FleckMaker.Static(cellEntry, parent.Map, FleckDefOf.PsycastSkipFlashEntry, Props.fleckScale);
            }

            foreach (var mapExit in mapExits) {
                foreach (var cellExit in mapExit.Value) {
                    FleckMaker.Static(cellExit, mapExit.Key, FleckDefOf.PsycastSkipInnerExit, Props.fleckScale);
                    FleckMaker.Static(cellExit, mapExit.Key, FleckDefOf.PsycastSkipOuterRingExit, Props.fleckScale);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            yield return new Command_Action {
                defaultLabel = "SD_CreateStockpile".Translate(),
                defaultDesc = "SD_CreateStockpileDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile"),
                action = CreateSkipdoorStockpile
            };
            yield return new Command_Toggle {
                icon = Resources.networkIcon,
                isActive = () => isGlobalSkipdoor,
                activateIfAmbiguous = false,
                defaultLabel = "SD_GlobalModeToggle".Translate(),
                defaultDesc = isGlobalSkipdoor ? "SD_GlobalModeGlobalDesc".Translate() : "SD_GlobalModeLocalDesc".Translate(),
                toggleAction = () => {
                    isGlobalSkipdoor = !isGlobalSkipdoor;
                }
            };
        }

        private static AcceptanceReport IsZoneableCell(IntVec3 c, Map map) {
            if (!c.InBounds(map) || c.Fogged(map)) {
                return false;
            }
            if (c.InNoZoneEdgeArea(map)) {
                return "TooCloseToMapEdge".Translate();
            }
            foreach (Thing item in map.thingGrid.ThingsAt(c)) {
                if (!item.def.CanOverlapZones) {
                    return false;
                }
            }
            return true;
        }

        private void CreateSkipdoorStockpile() {
            List<Skipdoor> selectedSkipdoors = Find.Selector.SelectedObjects.OfType<Skipdoor>().ToList();
            Zone_Stockpile stockpile = null;
            if (parent.Map.zoneManager.ZoneAt(parent.Position) != null) { // There's already a stockpile under that skipdoor
                if (parent.Map.zoneManager.ZoneAt(parent.Position) is Zone_Stockpile) {
                    stockpile = parent.Map.zoneManager.ZoneAt(parent.Position) as Zone_Stockpile;
                }
            }
            if (stockpile == null) {
                //Create a new stockpile
                stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, parent.Map.zoneManager);
                parent.Map.zoneManager.RegisterZone(stockpile);
            }
            // Find all the cells in that stockpile's area. If each cell is not in a stockpile already, add it to the current stockpile.
            // If it is in a stockpile that's not our new stockpile, add it to a list of stockpiles to merge.
            List<Zone_Stockpile> stockpilesToMerge = new();
            parent.Map.floodFiller.FloodFill(parent.Position, delegate (IntVec3 c) {
                Zone_Stockpile existing;
                if ((existing = (parent.Map.zoneManager.ZoneAt(c) as Zone_Stockpile)) != null) {
                    if (existing != stockpile && !stockpilesToMerge.Contains(existing)) {
                        stockpilesToMerge.Add(existing);
                    }
                }
                return selectedSkipdoors.Any((Skipdoor door) => door.GetComp<CompTeleportBetweenStockpiles>().RadialCells.Contains(c)) && parent.Map.zoneManager.ZoneAt(c) == null && (bool)IsZoneableCell(c, parent.Map);
            }, delegate (IntVec3 c) {
                stockpile.AddCell(c);
            });
            if (stockpilesToMerge.Count > 0) {
                foreach(Zone_Stockpile zone_Stockpile in stockpilesToMerge) {
                    if (zone_Stockpile.settings.Priority > stockpile.settings.Priority) {
                        stockpile.settings.Priority = zone_Stockpile.settings.Priority;
                    }
                    MergeQualities(stockpile.settings.filter, zone_Stockpile.settings.filter.AllowedQualityLevels);
                    MergeHitPoints(stockpile.settings.filter, zone_Stockpile.settings.filter.AllowedHitPointsPercents);
                    MergeAllowedDefs(stockpile.settings.filter, zone_Stockpile.settings.filter.AllowedThingDefs);
                    List<IntVec3> cells = zone_Stockpile.Cells.ToList();
                    zone_Stockpile.Delete();
                    foreach (IntVec3 cell in cells) {
                        stockpile.AddCell(cell);
                    }
                }
            }

        }

        private void MergeQualities(ThingFilter filter, QualityRange otherRange) {
            if (!filter.AllowedQualityLevels.Includes(otherRange.min)) {
                filter.AllowedQualityLevels = new QualityRange(otherRange.min, filter.AllowedQualityLevels.max);
            }
            if (!filter.AllowedQualityLevels.Includes(otherRange.max)) {
                filter.AllowedQualityLevels = new QualityRange(filter.AllowedQualityLevels.min, otherRange.max);
            }
        }
        private void MergeHitPoints(ThingFilter filter, FloatRange otherRange) {
            if (!filter.AllowedHitPointsPercents.Includes(otherRange.min)) {
                filter.AllowedHitPointsPercents = new FloatRange(otherRange.min, filter.allowedHitPointsPercents.max);
            }
            if (!filter.AllowedHitPointsPercents.Includes(otherRange.max)) {
                filter.AllowedHitPointsPercents = new FloatRange(filter.allowedHitPointsPercents.min, otherRange.max);
            }
        }

        private void MergeAllowedDefs(ThingFilter filter, IEnumerable<ThingDef> allowedDefs) {
            foreach(ThingDef def in allowedDefs) {
                filter.SetAllow(def, true);
            }
        }
    }
}