using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class MapMode_Features : MapMode_GenericRegion<WorldFeature>
    {
        public override List<WorldFeature> RegionList => Find.WorldFeatures.features;
        public override Material RegionMaterial => BaseContent.ClearMat;
        public override Material BorderMaterial => Materials.matFeaturesBorder;

        public MapMode_Features() { }
        public MapMode_Features(MapModeDef def) : base(def) { }

        public override Region GenerateRegion(WorldFeature feature)
        {
            bool isOcean = feature.def.defName.ToLower().Contains("ocean");
            Region region = new Region(feature.name, feature.Tiles.ToList(), isOcean, RegionMaterial, DoBorders, BorderMaterial, BorderWidth, GetRegionTooltip(feature));
            return region;
        }

        public override string GetRegionTooltip(WorldFeature feature)
        {
            return string.Format("{0}\n{1}: {2}", feature.name, "MMF.Type".Translate(), $"MMF.WorldFeatures.{feature.def.defName}".Translate());
        }
    }
}
