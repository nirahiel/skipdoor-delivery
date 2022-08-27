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
			this.compClass = typeof(CompTeleportBetweenStockpiles);
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

			foreach (var t in GenRadial
				         .RadialDistinctThingsAround(this.parent.Position, this.parent.Map, Props.radius, true).ToList())
			{
				if (t.def.category != ThingCategory.Item) continue;

				var zones = targetZones.Where(x => ZoneCanAccept(x, t));
				if (zones.TryRandomElement(out var selectedZone))
				{
					var cell = selectedZone.AllSlotCells()
						.Where(x => StoreUtility.IsGoodStoreCell(x, zone.Map, t, null, this.parent.Faction)).RandomElement();
					t.DeSpawn();
					GenPlace.TryPlaceThing(t, cell, selectedZone.Map, ThingPlaceMode.Near);
				}
			}
		}

		public bool ZoneCanAccept(Zone_Stockpile zone, Thing t)
		{
			var storeSettings = zone.GetStoreSettings();
			if (!storeSettings.AllowedToAccept(t))
			{
				return false;
			}

			return zone.AllSlotCells().Where(x => StoreUtility.IsGoodStoreCell(x, zone.Map, t, null, this.parent.Faction))
				.Any();
		}
	}
}