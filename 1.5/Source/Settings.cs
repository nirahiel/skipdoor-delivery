using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SkipdoorDelivery {
    public class Settings : ModSettings {
        public bool gatesStartGlobal = true;

        public override void ExposeData() {
            Scribe_Values.Look(ref gatesStartGlobal, "gatesStartGlobal", true);
            base.ExposeData();
        }
    }
}
