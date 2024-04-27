using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using VanillaPsycastsExpanded.Skipmaster;
using Verse;
using Verse.Sound;
using VFECore;
using HarmonyLib;
using UnityEngine;

namespace SkipdoorDelivery
{
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

		public override void PostDrawExtraSelectionOverlays()
		{
			if (Props.radius > 0.1)
			{
				GenDraw.DrawRadiusRing(parent.Position, Props.radius);
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			if (Find.TickManager.TicksGame % Props.tickRate != 0) return;

            // The current skipdoor doesn't require a stockpile anymore.
            // Let's find all skipdoors that aren't this one, and see if they have a stockpile.

            var targets = new List<SkipdoorZone>();
            foreach (var skipdoor in WorldComponent_DoorTeleporterManager.Instance.DoorTeleporters.OfType<Skipdoor>()) {
                if (skipdoor != parent && skipdoor.Position.GetZone(skipdoor.Map) is Zone_Stockpile stockpile) {
                    targets.Add(new SkipdoorZone(skipdoor, stockpile));
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
    }
}