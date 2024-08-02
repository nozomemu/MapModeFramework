using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public override async Task BuildSubMeshes(CancellationToken token)
        {
            ready = false;
            bool interrupted = false;
            Dictionary<Material, List<int>> tileMaterials = new Dictionary<Material, List<int>>();
            Dictionary<Region, Dictionary<int, bool[]>> regionTileEdgesDrawInfo = new Dictionary<Region, Dictionary<int, bool[]>>();
            try
            {
                Regions.ForEach(region =>
                {
                    if (!region.skipBody && region.material != BaseContent.ClearMat)
                    {
                        WorldRegenHandler.tilesToPrepare += region.tiles.Count;
                    }
                    if (region.doBorders && region.borderMaterial != BaseContent.ClearMat)
                    {
                        WorldRegenHandler.tilesToPrepare += region.GetBorders().Count;
                    }
                });
                Task prepareMeshes = Task.Run(() => PrepareMeshes(tileMaterials, token), token);
                Task prepareBorders = Task.Run(() => PrepareBorders(regionTileEdgesDrawInfo, token), token);
                await Task.WhenAll(prepareMeshes, prepareBorders);
            }
            catch (OperationCanceledException)
            {
                Core.Message("Async regeneration interrupted");
                interrupted = true;
            }
            catch (Exception ex)
            {
                Core.Error($"Error during asynchronous regeneration of {this}: {ex.Message}");
            }
            finally
            {
                if (!interrupted)
                {
                    foreach (var (material, tiles) in tileMaterials)
                    {
                        tiles.ForEach(tile =>
                        {
                            LayerSubMesh subMesh = GetSubMesh(material);
                            TileUtilities.DrawTile(subMesh, tile);
                        });
                    }
                    int regionsCount = Regions.Count;
                    for (int i = 0; i < regionsCount; i++)
                    {
                        Region region = Regions[i];
                        if (!region.doBorders || region.borderMaterial == BaseContent.ClearMat)
                        {
                            continue;
                        }
                        DoRegionBorders(region, regionTileEdgesDrawInfo[region]);
                    }
                    FinalizeMesh(MeshParts.All);
                    MapModeComponent.Notify_RegenerationComplete(CurrentMapMode);
                }
                WorldRegenHandler.Notify_Finished();
                ready = true;
            }
        }

        public override void PrepareMeshes(Dictionary<Material, List<int>> tileMaterials, CancellationToken token)
        {
            int regionsCount = Regions.Count;
            for (int i = 0; i < regionsCount; i++)
            {
                token.ThrowIfCancellationRequested();
                Region region = Regions[i];
                if (region.skipBody || region.material == BaseContent.ClearMat)
                {
                    continue;
                }
                token.ThrowIfCancellationRequested();
                Material regionMaterial = region.material;
                List<int> regionTiles = region.tiles;
                int regionTilesCount = regionTiles.Count;
                for (int j = 0; j < regionTilesCount; j++)
                {
                    int tile = regionTiles[j];
                    token.ThrowIfCancellationRequested();
                    if (!ValidTile(tile, false))
                    {
                        continue;
                    }
                    token.ThrowIfCancellationRequested();
                    if (!tileMaterials.TryGetValue(regionMaterial, out List<int> tileList))
                    {
                        tileList = new List<int>();
                        tileMaterials[regionMaterial] = tileList;
                    }
                    tileList.Add(tile);
                    WorldRegenHandler.tilesPrepared++;
                }
            }
        }

        public void PrepareBorders(Dictionary<Region, Dictionary<int, bool[]>> regionTileEdgesDrawInfo, CancellationToken token)
        {
            int regionsCount = Regions.Count;
            for (int i = 0; i < regionsCount; i++)
            {
                token.ThrowIfCancellationRequested();
                Region region = Regions[i];
                if (!region.doBorders || region.borderMaterial == BaseContent.ClearMat)
                {
                    return;
                }
                token.ThrowIfCancellationRequested();
                Material borderMaterial = region.borderMaterial;
                List<int> regionTiles = region.tiles;
                List<int> borderTiles = region.GetBorders();
                int borderTilesCount = borderTiles.Count;
                bool excludeNeighbor(int x) => regionTiles.Contains(x) && !borderTiles.Contains(x);
                bool neighborIsContiguous(int x) => regionTiles.Contains(x) && borderTiles.Contains(x);
                for (int j = 0; j < borderTilesCount; j++)
                {
                    token.ThrowIfCancellationRequested();
                    int tile = borderTiles[j];
                    if (!regionTileEdgesDrawInfo.TryGetValue(region, out var tileEdgesDrawInfo))
                    {
                        tileEdgesDrawInfo = new Dictionary<int, bool[]>();
                        regionTileEdgesDrawInfo[region] = tileEdgesDrawInfo;
                    }
                    PrepareEdges(region, tile, excludeNeighbor, neighborIsContiguous, tileEdgesDrawInfo);
                    WorldRegenHandler.tilesPrepared++;
                }
            }
        }

        public virtual void DoRegionBorders(Region region, Dictionary<int, bool[]> tileEdgesDrawInfo, MapMode_Region cachingMapMode = null)
        {
            List<int> regionTiles = region.tiles;
            List<int> borders = region.GetBorders();
            bool excludeNeighbor(int x) => regionTiles.Contains(x) && !borders.Contains(x);
            bool neighborIsContiguous(int x) => regionTiles.Contains(x) && borders.Contains(x);
            for (int i = 0; i < borders.Count; i++)
            {
                int borderTile = borders[i];
                if (cachingMapMode != null)
                {
                    PrepareEdges(region, borderTile, excludeNeighbor, neighborIsContiguous, tileEdgesDrawInfo, cachingMapMode);
                    continue;
                }
                if (!ModCompatibility.DrawTile(borderTile))
                {
                    continue;
                }
                LayerSubMesh subMesh = GetSubMesh(region.borderMaterial);
                OutlineVertices(borderTile, tileEdgesDrawInfo[borderTile], subMesh, region.borderWidth);
            }
        }

        private void PrepareEdges(Region region, int tile, Predicate<int> excludeNeighbor, Predicate<int> neighborIsContiguous, Dictionary<int, bool[]> tileEdgesDrawInfo, MapMode_Region cachingMapMode = null)
        {
            List<Vector3> vertices = TileUtilities.GetTileVertices(tile);
            List<Tuple<Vector3, Vector3>> edges = new List<Tuple<Vector3, Vector3>>();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 next = vertices[(i + 1) % vertices.Count];
                edges.Add(new Tuple<Vector3, Vector3>(vertices[i], next));
            }
            bool[] drawEdges = EdgesCache.GetEdgeDrawInfo(region, tile);
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
                cachingMapMode ??= CurrentMapMode;
                if (cachingMapMode.EnabledCaching)
                {
                    EdgesCache.AddEdgeDrawInfo(region, tile, drawEdges);
                    cachingMapMode.tilesCached++;
                }
            }
            tileEdgesDrawInfo?.Add(tile, drawEdges);
        }

        public void OutlineVertices(int tile, bool[] drawEdges, LayerSubMesh subMesh, float borderWidth)
        {
            List<Vector3> vertices = TileUtilities.GetTileVertices(tile);
            List<Tuple<Vector3, Vector3>> edges = new List<Tuple<Vector3, Vector3>>();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 next = vertices[(i + 1) % vertices.Count];
                edges.Add(new Tuple<Vector3, Vector3>(vertices[i], next));
            }
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
    }
}
