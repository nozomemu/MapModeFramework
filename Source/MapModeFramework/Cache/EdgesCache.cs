using System.Collections.Generic;

namespace MapModeFramework
{
    public static class EdgesCache
    {
        public static Dictionary<Region, Dictionary<int, bool[]>> cachedEdges = new Dictionary<Region, Dictionary<int, bool[]>>();

        public static bool[] GetEdgeDrawInfo(Region region, int tile)
        {
            if (cachedEdges.TryGetValue(region, out Dictionary<int, bool[]> tileEdgeDrawInfo))
            {
                if (tileEdgeDrawInfo.TryGetValue(tile, out bool[] edgeDrawInfo))
                {
                    return edgeDrawInfo;
                }
            }
            return null;
        }

        public static void AddEdgeDrawInfo(Region region, int tile, bool[] edgeDrawInfo)
        {
            if (!cachedEdges.TryGetValue(region, out Dictionary<int, bool[]> tileEdgeDrawInfo))
            {
                tileEdgeDrawInfo = new Dictionary<int, bool[]>();
                cachedEdges.Add(region, tileEdgeDrawInfo);
            }
            tileEdgeDrawInfo.Add(tile, edgeDrawInfo);
        }

        public static void ClearRegionCache(Region region)
        {
            cachedEdges.Remove(region);
        }
    }
}
