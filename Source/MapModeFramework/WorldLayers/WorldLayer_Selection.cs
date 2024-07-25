using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public abstract class WorldLayer_SelectionRegion : WorldLayer_MapMode
    {
        public new MapMode_Region CurrentMapMode => (MapMode_Region)base.CurrentMapMode;

        protected Region lastDrawnRegion = null;
        protected abstract Region Region { get; }
        public override bool Active => base.CurrentMapMode is MapMode_Region && CurrentMapMode?.WorldLayerClass == typeof(WorldLayer_MapMode_Region);
        public override bool ShouldRegenerate
        {
            get
            {
                if (!base.ShouldRegenerate)
                {
                    return Region != lastDrawnRegion;
                }
                return true;
            }
        }

        public override void DoMeshes()
        {
            if (Region == null)
            {
                lastDrawnRegion = null;
                return;
            }
            List<int> regionTiles = Region.tiles;
            Material material = GetMaterial(-1);
            for (int i = 0; i < regionTiles.Count; i++)
            {
                if (!ModCompatibility.DrawTile(regionTiles[i]))
                {
                    continue;
                }
                LayerSubMesh subMesh = GetSubMesh(material);
                TileUtilities.DrawTile(subMesh, regionTiles[i]);
            }
            lastDrawnRegion = Region;
        }
    }

    public class WorldLayer_MouseRegion : WorldLayer_SelectionRegion
    {
        protected override Region Region
        {
            get
            {
                if (Find.World.UI.selector.dragBox.IsValidAndActive || Find.WorldTargeter.IsTargeting || Find.ScreenshotModeHandler.Active)
                {
                    return null;
                }
                int mouseTile = GenWorld.MouseTile();
                if (!ModCompatibility.DrawTile(mouseTile))
                {
                    return null;
                }
                return CurrentMapMode.GetRegion(mouseTile);
            }
        }

        public override Material GetMaterial(int tile)
        {
            return Materials.matMouseRegion;
        }
    }

    public class WorldLayer_SelectedRegion : WorldLayer_SelectionRegion
    {
        public static WorldLayer_SelectedRegion Instance;
        protected override Region Region
        {
            get
            {
                if (!(CurrentMapMode is MapMode_Region mapModeRegion))
                {
                    return null;
                }
                if (mapModeRegion.def.RegionProperties.overrideSelector)
                {
                    return selectedRegion;
                }
                int selectedTile = Find.WorldSelector.selectedTile;
                if (!ModCompatibility.DrawTile(selectedTile))
                {
                    return null;
                }
                return CurrentMapMode.GetRegion(selectedTile);
            }
        }
        public Region SelectedRegion => Region;
        public Region selectedRegion;

        public WorldLayer_SelectedRegion() : base()
        {
            Instance = this;
        }

        public override Material GetMaterial(int tile)
        {
            return Materials.matSelectedRegion;
        }
    }
}
