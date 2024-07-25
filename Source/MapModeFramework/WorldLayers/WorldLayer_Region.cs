using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class WorldLayer_MapMode_Region : WorldLayer_MapMode
    {
        public static WorldLayer_MapMode_Region Instance;

        public new MapMode_Region CurrentMapMode => (MapMode_Region)base.CurrentMapMode;
        public List<Region> Regions => CurrentMapMode.regions;
        public override bool Active => base.Active && CurrentMapMode is MapMode_Region;

        public WorldLayer_MapMode_Region() : base()
        {
            Instance = this;
        }

        public override void DoMeshes()
        {
            for (int i = 0; i < Regions.Count; i++)
            {
                Region region = Regions[i];
                if (!region.skipBody && region.material != BaseContent.ClearMat)
                {
                    DoRegionTiles(region);
                }
                if (region.doBorders && region.borderMaterial != BaseContent.ClearMat)
                {
                    DoRegionBorders(region);
                }
            }
        }

        public virtual void DoRegionTiles(Region region)
        {
            WorldGrid grid = Find.WorldGrid;
            List<int> regionTiles = region.tiles;
            for (int i = 0; i < regionTiles.Count; i++)
            {
                if (grid[regionTiles[i]].WaterCovered && !MapModeComponent.drawSettings.includeWater)
                {
                    continue;
                }
                if (!ModCompatibility.DrawTile(regionTiles[i]))
                {
                    continue;
                }
                LayerSubMesh subMesh = GetSubMesh(region.material);
                TileUtilities.DrawTile(subMesh, regionTiles[i]);
            }
        }

        public virtual void DoRegionBorders(Region region, bool caching = false)
        {
            List<int> regionTiles = region.tiles;
            List<int> borders = region.GetBorders();
            for (int i = 0; i < borders.Count; i++)
            {
                bool excludeNeighbor(int x) => regionTiles.Contains(x) && !borders.Contains(x);
                bool neighborIsContiguous(int x) => regionTiles.Contains(x) && borders.Contains(x);
                if (caching)
                {
                    PrepareEdges(region, borders[i], excludeNeighbor, neighborIsContiguous, out _, out _);
                    continue;
                }
                if (!ModCompatibility.DrawTile(borders[i]))
                {
                    continue;
                }
                LayerSubMesh subMesh = GetSubMesh(region.borderMaterial);
                OutlineVertices(region, borders[i], excludeNeighbor, neighborIsContiguous, subMesh, region.borderWidth);
            }
        }

        //Cache edges since this is where the regeneration bottlenecks at higher world coverages
        private void PrepareEdges(Region region, int tile, Predicate<int> excludeNeighbor, Predicate<int> neighborIsContiguous, out List<Tuple<Vector3, Vector3>> edges, out bool[] drawEdges)
        {
            List<Vector3> vertices = TileUtilities.GetTileVertices(tile);
            edges = new List<Tuple<Vector3, Vector3>>();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 next = vertices[(i + 1) % vertices.Count];
                edges.Add(new Tuple<Vector3, Vector3>(vertices[i], next));
            }
            drawEdges = EdgesCache.GetEdgeDrawInfo(region, tile);
            if (drawEdges == null)
            {
                drawEdges = new bool[edges.Count];
                Array.Fill(drawEdges, true);

                List<int> neighbors = TileUtilities.GetTileNeighbors(tile);
                HashSet<Vector3> edgeVertices = new HashSet<Vector3>();
                int neighborsCount = neighbors.Count;
                for (int i = 0; i < neighborsCount; i++)
                {
                    List<Vector3> sharedVerts = TileUtilities.GetSharedVertices(tile, neighbors[i]);
                    edgeVertices.Clear();
                    int sharedVertsCount = sharedVerts.Count;
                    for (int j = 0; j < sharedVertsCount; j++)
                    {
                        edgeVertices.Add(sharedVerts[j]);
                    }
                    int edgeCount = edges.Count;
                    if (excludeNeighbor(neighbors[i]))
                    {
                        for (int j = 0; j < edgeCount; j++)
                        {
                            var edge = edges[j];
                            if (edgeVertices.Contains(edge.Item1) || edgeVertices.Contains(edge.Item2))
                            {
                                drawEdges[j] = false;
                            }
                        }
                    }
                    else if (neighborIsContiguous(neighbors[i]))
                    {
                        for (int j = 0; j < edgeCount; j++)
                        {
                            var edge = edges[j];
                            if (edgeVertices.Contains(edge.Item1) && edgeVertices.Contains(edge.Item2))
                            {
                                drawEdges[j] = false;
                                break;
                            }
                        }
                    }
                }
                EdgesCache.AddEdgeDrawInfo(region, tile, drawEdges);
            }
        }

        public void OutlineVertices(Region region, int tile, Predicate<int> excludeNeighbor, Predicate<int> neighborIsContiguous, LayerSubMesh subMesh, float borderWidth)
        {
            PrepareEdges(region, tile, excludeNeighbor, neighborIsContiguous, out var edges, out var drawEdges);
            int edgeCount = edges.Count;
            for (int i = 0; i < edgeCount; i++)
            {
                if (!drawEdges[i])
                {
                    continue;
                }
                var edge = edges[i];
                int count = subMesh.verts.Count;
                TileUtilities.DerivePerpendicularVectors(edge.Item1, edge.Item2, borderWidth, out var v1, out var v2);
                subMesh.verts.Add(v1); //0
                subMesh.verts.Add(v2); //1
                subMesh.verts.Add(edge.Item1); //2
                subMesh.verts.Add(edge.Item2); //3

                subMesh.tris.Add(count);
                subMesh.tris.Add(count + 1);
                subMesh.tris.Add(count + 2);
                subMesh.tris.Add(count + 2);
                subMesh.tris.Add(count + 1);
                subMesh.tris.Add(count + 3);
            }
        }

        public override Material GetMaterial(int tile)
        {
            Region region = Regions.FirstOrDefault(x => x.tiles.Contains(tile));
            if (region == null)
            {
                return BaseContent.ClearMat;
            }
            return region.material;
        }

        public override string GetTooltip(int tile)
        {
            Region region = Regions.FirstOrDefault(x => x.tiles.Contains(tile));
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
}
