using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using VanillaPsycastsExpanded.Skipmaster;
using Verse;

namespace SkipdoorDelivery
{
	public class CompProperties_TeleportBetweenStockpiles : CompProperties
	{
		public int tickRate;
		public float radius;

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

		public override void CompTick()
		{
			base.CompTick();
			if (Find.TickManager.TicksGame % Props.tickRate != 0) return;
			if (parent.Position.GetZone(parent.Map) is not Zone_Stockpile zone)
			{
				return;
			}

			var currentZonePriority = zone.GetStoreSettings().Priority;

			var targets = new List<SkipdoorZone>();
			foreach (var skipdoor in WorldComponent_SkipdoorManager.Instance.Skipdoors)
			{
				if (skipdoor.Position.GetZone(skipdoor.Map) is Zone_Stockpile stockpile &&
				    stockpile.GetStoreSettings().Priority > currentZonePriority)
				{
					targets.Add(new SkipdoorZone(skipdoor, stockpile));
				}
			}

			if (targets.Count == 0)
			{
				return;
			}

			targets.Sort();

			foreach (var thing in GenRadial
				         .RadialDistinctThingsAround(parent.Position, parent.Map, Props.radius, true)
				         .Where(t => t.def.category == ThingCategory.Item))
			{
				foreach (var target in targets)
				{
					var targetZone = target.zone;
					if (!targetZone.GetStoreSettings().AllowedToAccept(thing)) continue;

					var targetCells = targetZone.AllSlotCells().Where(cell =>
						StoreUtility.IsGoodStoreCell(cell, targetZone.Map, thing, null, parent.Faction)).ToList();

					if (!targetCells.Any()) continue;

					var targetCell = targetCells.RandomElement();
					thing.DeSpawn();
					GenPlace.TryPlaceThing(thing, targetCell, targetZone.Map, ThingPlaceMode.Near);
					break;
				}
			}
		}
	}
}