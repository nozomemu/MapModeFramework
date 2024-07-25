using System.Collections.Generic;
using UnityEngine;

namespace MapModeFramework
{
    public class Region
    {
        public string name;
        public List<int> tiles;

        public bool skipBody;
        public Material material;

        public bool doBorders;
        public Material borderMaterial;
        public float borderWidth;

        public string tooltip;

        public List<int> cachedBorders;
        public bool changed;

        public Region(string name, List<int> tiles, bool skipBody, Material material, bool doBorders = false, Material borderMaterial = null, float borderWidth = 1f, string tooltip = null)
        {
            this.name = name;
            this.tiles = tiles;
            this.skipBody = skipBody;
            this.material = material;
            this.doBorders = doBorders;
            this.borderMaterial = borderMaterial;
            this.borderWidth = borderWidth;
            cachedBorders = TileUtilities.GetBorderTiles(tiles);
            this.tooltip = tooltip;
        }

        public List<int> GetBorders()
        {
            if (changed)
            {
                Notify_RegionChanged();
            }
            return cachedBorders;
        }

        public void Notify_RegionChanged()
        {
            cachedBorders = TileUtilities.GetBorderTiles(tiles);
            EdgesCache.ClearRegionCache(this);
            changed = false;
        }

        public string GetTooltip()
        {
            return tooltip;
        }
    }
}
