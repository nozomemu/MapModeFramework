using RimWorld;
using System.Collections;
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
        public override bool Active => base.CurrentMapMode is MapMode_Region && CurrentMapMode.WorldLayer.Regenerated;
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
        protected override bool RegenerateAsynchronously => false;

        public override IEnumerable Regenerate()
        {
            foreach (object item in base.Regenerate())
            {
                yield return item;
            }
            if (!Active)
            {
                yield break;
            }
            DrawSelectorTiles();
            FinalizeMesh(MeshParts.All);
            MapModeComponent.Notify_RegenerationComplete(null);
        }

        //Relatively straightforward, no need to prepare asynchronously (though it does still lag when hovering over/selecting large regions, e.g. oceans)
        public void DrawSelectorTiles()
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
