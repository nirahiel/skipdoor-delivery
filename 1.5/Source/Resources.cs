using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SkipdoorDelivery {
    [StaticConstructorOnStartup]
    public static class Resources {
        public static readonly Texture2D networkIcon = ContentFinder<Texture2D>.Get("SD/UI/Network", true);
    }
}
