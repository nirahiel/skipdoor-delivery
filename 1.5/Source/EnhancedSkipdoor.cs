using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SkipdoorDelivery {
    public class EnhancedSkipdoor : Mod {

        private readonly Settings settings;

        public EnhancedSkipdoor(ModContentPack content) : base(content) {
            settings = GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            Listing_Standard listing = new();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "SD_GatesStartGlobal".Translate(),
                ref settings.gatesStartGlobal,
                "SD_GatesStartGlobalDesc".Translate()
            );
            
            listing.End();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() {
            return "Skipdoor Delivery";
        }
    }
}
