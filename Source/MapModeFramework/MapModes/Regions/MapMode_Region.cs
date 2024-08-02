using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using Verse;
using Verse.Noise;

namespace MapModeFramework
{
    public abstract class MapMode_Region : MapMode_Cached
    {
        public override WorldLayer_MapMode WorldLayer => WorldLayer_MapMode_Region.Instance;
        public WorldLayer_MapMode_Region WorldLayerRegion => (WorldLayer_MapMode_Region)WorldLayer;

        public List<Region> regions = new List<Region>();
        public abstract Material RegionMaterial { get; }
        public virtual bool DoBorders => def.RegionProperties.doBorders;
        public virtual Material BorderMaterial { get; }
        public virtual float BorderWidth => def.RegionProperties.borderWidth;

        public MapMode_Region() { }
        public MapMode_Region(MapModeDef def) : base(def) { }

        public override void Initialize()
        {
            SetRegions();
        }

        protected override void StartCaching(CancellationToken token)
        {
            regions.ForEach(region =>
            {
                if (region.doBorders && region.borderMaterial != BaseContent.ClearMat)
                {
                    tilesToCache += region.GetBorders().Count;
                }
            });
            int regionsCount = regions.Count;
            for (int i = 0; i < regionsCount; i++)
            {
                token.ThrowIfCancellationRequested();
                Region region = regions[i];
                WorldLayerRegion.DoRegionBorders(region, null, this);
            }
        }

        protected override void DoCacheClearing()
        {
            Type genericType = GetType();
            Type[] typeArgs = genericType.GetGenericArguments();
            if (typeArgs.Length > 0)
            {
                Type cacheType = typeof(RegionCache<>).MakeGenericType(typeArgs[0]);
                MethodInfo removeMethod = cacheType.GetMethod("RemoveRegion", BindingFlags.Public | BindingFlags.Static);

                PropertyInfo listProperty = genericType.GetProperty("RegionList");
                if (listProperty.GetValue(this) is IEnumerable list && removeMethod != null)
                {
                    foreach (var item in list)
                    {
                        //Debug message, removed once this works
                        Core.Message($"Removed {item}");
                        removeMethod.Invoke(null, new[] { item });
                    }
                }
            }

            List<Region> regions = this.regions;
            int regionsCount = regions.Count;
            for (int i = 0; i < regionsCount; i++)
            {
                Region region = regions[i];
                EdgesCache.ClearRegionCache(region);
            }
        }

        public override void DoPreRegenerate()
        {
            base.DoPreRegenerate();
            SetRegions();
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
                    if (EnabledCaching)
                    {
                        RegionCache<T>.AddRegion(regionType, region);
                    }
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
