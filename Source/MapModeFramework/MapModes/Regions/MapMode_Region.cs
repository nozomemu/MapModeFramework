using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public abstract class MapMode_Region : MapMode
    {
        public new WorldLayer_MapMode_Region WorldLayer => WorldLayer_MapMode_Region.Instance;
        public List<Region> regions = new List<Region>();
        public abstract Material RegionMaterial { get; }
        public virtual bool DoBorders => def.RegionProperties.doBorders;
        public virtual Material BorderMaterial { get; }
        public virtual float BorderWidth => def.RegionProperties.borderWidth;
        public bool cached;

        public MapMode_Region() { }
        public MapMode_Region(MapModeDef def) : base(def) { }

        public override void Initialize()
        {
            SetRegions();
        }

        public override void DoPreRegenerate()
        {
            base.DoPreRegenerate();
            SetRegions();
        }

        public override void MapModeOnGUI()
        {
            WorldLayer.OnGUI();
        }

        public Region GetRegion(int tile)
        {
            return regions.FirstOrDefault(x => x.tiles.Contains(tile));
        }

        public abstract void SetRegions();

        public override Material GetMaterial(int tile)
        {
            Region region = GetRegion(tile);
            if (region != null)
            {
                return region.material;
            }
            return base.GetMaterial(tile);
        }

        public override string GetTooltip(int tile)
        {
            Region region = GetRegion(tile);
            if (region == null)
            {
                return base.GetTooltip(tile);
            }
            if (!ModCompatibility.OverrideTooltip(tile, out string tooltip))
            {
                tooltip = region.GetTooltip();
            }
            return tooltip;
        }
    }

    public abstract class MapMode_GenericRegion<T> : MapMode_Region
    {
        public abstract List<T> RegionList { get; }

        public MapMode_GenericRegion() { }
        public MapMode_GenericRegion(MapModeDef def) : base(def) { }

        public override void SetRegions()
        {
            regions.Clear();
            foreach (T regionType in RegionList)
            {
                Region region = GetOrGenerateRegion(regionType);
                if (!regions.Contains(region))
                {
                    regions.Add(region);
                }
            }

            Region GetOrGenerateRegion(T regionType)
            {
                Region region = RegionCache<T>.GetRegion(regionType);
                if (region == null)
                {
                    region = GenerateRegion(regionType);
                    RegionCache<T>.AddRegion(regionType, region);
                }
                return region;
            }
        }

        public abstract Region GenerateRegion(T regionType);

        public virtual string GetRegionTooltip(T regionType)
        {
            return string.Empty;
        }
    }
}
