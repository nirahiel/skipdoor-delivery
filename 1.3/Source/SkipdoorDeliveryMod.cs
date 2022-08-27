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

			var targetZones = new List<Zone_Stockpile>();
			foreach (var skipdoor in WorldComponent_SkipdoorManager.Instance.Skipdoors)
			{
				if (skipdoor.Position.GetZone(skipdoor.Map) is Zone_Stockpile stockpile &&
				    stockpile.GetStoreSettings().Priority > currentZonePriority)
				{
					targetZones.Add(stockpile);
				}
			}

			if (targetZones.Count == 0)
			{
				return;
			}

			targetZones.Sort((z1, z2) => z2.GetStoreSettings().Priority.CompareTo(z1.GetStoreSettings().Priority));

			foreach (var thing in GenRadial
				         .RadialDistinctThingsAround(parent.Position, parent.Map, Props.radius, true)
				         .Where(t => t.def.category == ThingCategory.Item))
			{
				foreach (var targetZone in targetZones)
				{
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