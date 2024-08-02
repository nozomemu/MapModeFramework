using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public static class TileUtilities
    {
        private static WorldGrid Grid => Find.WorldGrid;

        public static List<int> GetTileNeighbors(int tile)
        {
            List<int> neighbors = new List<int>();
            Grid.GetTileNeighbors(tile, neighbors);
            return neighbors;
        }

        public static List<Vector3> GetTileVertices(int tile)
        {
            List<Vector3> vertices = new List<Vector3>();
            Grid.GetTileVertices(tile, vertices);
            return vertices;
        }

        public static List<int> GetTileIndices(int tile)
        {
            List<int> indices = new List<int>();
            Grid.GetTileVerticesIndices(tile, indices);
            return indices;
        }

        public static List<Vector3> GetSharedVertices(int tile, int neighbor)
        {
            List<Vector3> tileVertices = GetTileVertices(tile);
            return GetTileVertices(neighbor).Where(x => tileVertices.Contains(x)).ToList();
        }

        public static void DerivePerpendicularVectors(Vector3 v1, Vector3 v2, float distance, out Vector3 v3, out Vector3 v4)
        {
            Vector3 perpendicular = Vector3.Cross(v2, v1).normalized * 0.05f * distance;
            v3 = v1 + perpendicular;
            v4 = v2 + perpendicular;
        }

        public static List<int> GetBorderTiles(List<int> tiles)
        {
            HashSet<int> tileSet = new HashSet<int>(tiles);
            List<int> borderTiles = new List<int>();
            for (int i = 0; i < tiles.Count; i++)
            {
                if (IsBorderTile(tiles[i], tileSet))
                {
                    borderTiles.Add(tiles[i]);
                }
            }
            return borderTiles;
        }

        public static bool IsBorderTile(int tile, HashSet<int> tiles)
        {
            List<int> neighbors = GetTileNeighbors(tile);
            return neighbors.Any(x => !tiles.Contains(x));
        }

        public static void DrawTile(LayerSubMesh subMesh, int tile)
        {
            List<Vector3> vertices = GetTileVertices(tile);
            int vertCount = vertices.Count;
            int subVerts = subMesh.verts.Count;
            for (int j = 0; j < vertCount; j++)
            {
                subMesh.verts.Add(vertices[j] + vertices[j].normalized * 0.012f);
                subMesh.uvs.Add((GenGeo.RegularPolygonVertexPosition(vertCount, j) + Vector2.one) / 2f);
                if (j < vertCount - 2)
                {
                    subMesh.tris.Add(subVerts + j + 2);
                    subMesh.tris.Add(subVerts + j + 1);
                    subMesh.tris.Add(subVerts);
                }
            }
        }
    }
}
