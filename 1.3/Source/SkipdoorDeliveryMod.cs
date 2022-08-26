using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
            if (Find.TickManager.TicksGame % Props.tickRate == 0)
            {
                var dict = new Dictionary<ThingWithComps, Zone_Stockpile>();
                foreach (var skipdoor in WorldComponent_SkipdoorManager.Instance.Skipdoors)
                {
                    var stockpile = skipdoor.Position.GetZone(skipdoor.Map) as Zone_Stockpile;
                    if (stockpile != null)
                    {
                        dict[skipdoor] = stockpile;
                    }
                }

                var ownZone = dict.TryGetValue(this.parent, out var zone) ? zone : null;
                foreach (var t in GenRadial.RadialDistinctThingsAround(this.parent.Position, this.parent.Map, Props.radius, true).ToList())
                {
                    if (t.def.category == ThingCategory.Item)
                    {
                        var zones = dict.Where(x => ZoneCanAccept(x.Value, t) && (ownZone is null 
                        || x.Value.GetStoreSettings().Priority > ownZone.GetStoreSettings().Priority))
                            .OrderByDescending(x => x.Value.GetStoreSettings().Priority).ToList();
                        if (zones.TryRandomElement(out var selectedZone))
                        {
                            var cell = selectedZone.Value.AllSlotCells().Where(x => StoreUtility.IsGoodStoreCell(x, zone.Map, t, null, this.parent.Faction)).RandomElement();
                            t.DeSpawn();
                            GenPlace.TryPlaceThing(t, cell, selectedZone.Value.Map, ThingPlaceMode.Near);
                        }
                    }
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
            return zone.AllSlotCells().Where(x => StoreUtility.IsGoodStoreCell(x, zone.Map, t, null, this.parent.Faction)).Any();
        }
    }
}
