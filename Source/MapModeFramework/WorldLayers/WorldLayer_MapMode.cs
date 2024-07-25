using RimWorld;
using RimWorld.Planet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public abstract class WorldLayer_MapMode : WorldLayer
    {
        public MapModeComponent MapModeComponent => MapModeComponent.Instance;
        public MapMode CurrentMapMode => MapModeComponent.currentMapMode;
        public virtual bool Active => CurrentMapMode?.WorldLayerClass == GetType();

        public override bool ShouldRegenerate => MapModeComponent.regenerateNow;

        public override void Render()
        {
            if (!Active)
            {
                return;
            }
            base.Render();
        }

        public virtual void OnGUI()
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            if (Find.World.UI.selector.dragBox.IsValidAndActive || Find.WorldTargeter.IsTargeting || Find.ScreenshotModeHandler.Active)
            {
                return;
            }
            int mouseTile = GenWorld.MouseTile();
            if (mouseTile == -1)
            {
                return;
            }
            Vector2 screenPos = GenWorldUI.WorldToUIPosition(Find.WorldGrid.GetTileCenter(mouseTile)); ;
            Rect rect = new Rect(screenPos.x - 40f, screenPos.y - 40f, 80f, 80f);
            string tileLabel = GetTileLabel(mouseTile);
            if (MapModeComponent.drawSettings.displayLabels && !tileLabel.NullOrEmpty())
            {
                Widgets.Label(rect, tileLabel);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            string tileTooltip = GetTooltip(mouseTile);
            if (MapModeComponent.drawSettings.doTooltip && !tileTooltip.NullOrEmpty())
            {
                TipSignal tip = new TipSignal(() => tileTooltip, mouseTile);
                TooltipHandler.TipRegion(rect, tip);
            }
        }

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
            DoMeshes();
            FinalizeMesh(MeshParts.All);
            MapModeComponent.regenerateNow = false;
        }

        public virtual void DoMeshes()
        {
            WorldGrid grid = Find.WorldGrid;
            List<Tile> tiles = grid.tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].WaterCovered && !MapModeComponent.drawSettings.includeWater)
                {
                    continue;
                }
                if (!ModCompatibility.DrawTile(i))
                {
                    continue;
                }
                Material material = GetMaterial(i);
                if (material == BaseContent.ClearMat)
                {
                    continue;
                }
                LayerSubMesh subMesh = GetSubMesh(material);
                TileUtilities.DrawTile(subMesh, i);
            }
        }

        public virtual Material GetMaterial(int tile)
        {
            return null;
        }

        public virtual string GetTileLabel(int tile)
        {
            return string.Empty;
        }

        public virtual string GetTooltip(int tile)
        {
            return string.Empty;
        }
    }
}
