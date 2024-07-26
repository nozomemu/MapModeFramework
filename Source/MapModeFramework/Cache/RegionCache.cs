using System.Collections.Generic;

namespace MapModeFramework
{
    public static class RegionCache<T>
    {
        public static Dictionary<T, Region> cachedRegions = new Dictionary<T, Region>();

        public static Region GetRegion(T key)
        {
            if (cachedRegions.TryGetValue(key, out Region region))
            {
                return region;
            }
            return null;
        }

        public static void AddRegion(T key, Region region)
        {
            cachedRegions.Add(key, region);
        }

        public static void RemoveRegion(T key)
        {
            cachedRegions.Remove(key);
            MapModeComponent.Instance.Notify_RegionChanged();
        }
    }
}
