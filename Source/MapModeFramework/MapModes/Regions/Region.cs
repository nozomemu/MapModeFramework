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
            return cachedBorders;
        }

        public bool HasTile(int tile)
        {
            return tiles.Contains(tile);
        }

        public void AddTile(int tile)
        {
            if (HasTile(tile))
            {
                return;
            }
            tiles.Add(tile);
            Notify_RegionChanged();
        }

        public void RemoveTile(int tile)
        {
            if (!HasTile(tile))
            {
                return;
            }
            tiles.Remove(tile);
            Notify_RegionChanged();
        }

        public void SetTiles(List<int> tiles)
        {
            this.tiles = tiles;
            Notify_RegionChanged();
        }

        public void Notify_RegionChanged()
        {
            cachedBorders = TileUtilities.GetBorderTiles(tiles);
            EdgesCache.ClearRegionCache(this);
            MapModeComponent.Instance.Notify_RegionChanged();
        }

        public string GetTooltip()
        {
            return tooltip;
        }
    }
}
