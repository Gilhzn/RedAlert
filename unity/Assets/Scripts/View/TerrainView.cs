using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Builds a 3D mesh from MapData: one quad per cell, vertex heights smoothed
    /// across corners (natural ramps), one submesh per land type so each land
    /// gets its own material color. Scale: 1 cell = 1 world unit, 1 height
    /// level = 0.5 world units. Adds a MeshCollider for picking and for placing
    /// units on the surface.
    /// </summary>
    public sealed class TerrainView : MonoBehaviour
    {
        public const float CellSize = 1f;
        public const float HeightLevelSize = 0.5f;
        public const float LeptonsToWorld = CellSize / LeptonPos.LeptonsPerCell;

        private MapData _map;
        private float[,] _cornerHeights; // (width+1) x (height+1)

        // Style-guide palette: cold desaturated world (docs/research §A2).
        private static readonly Dictionary<string, Color> LandColors = new Dictionary<string, Color>
        {
            { "clear",   new Color(0.29f, 0.33f, 0.28f) },  // olive-grey earth
            { "rough",   new Color(0.24f, 0.26f, 0.23f) },
            { "road",    new Color(0.34f, 0.35f, 0.37f) },  // asphalt grey
            { "water",   new Color(0.13f, 0.22f, 0.32f) },  // dark cold water
            { "cliff",   new Color(0.22f, 0.23f, 0.26f) },  // rock
            { "crystal", new Color(0.12f, 0.45f, 0.22f) },  // toxic green ground
            { "ice",     new Color(0.62f, 0.70f, 0.76f) },
        };

        public static TerrainView Build(MapData map, RulesData rules, Transform parent)
        {
            var go = new GameObject("Terrain");
            go.transform.SetParent(parent, worldPositionStays: false);
            var view = go.AddComponent<TerrainView>();
            view.BuildMesh(map, rules);
            return view;
        }

        private void BuildMesh(MapData map, RulesData rules)
        {
            _map = map;
            int w = map.Width;
            int h = map.Height;

            ComputeCornerHeights();

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            // One triangle list per land type index.
            var submeshTriangles = new List<int>[rules.Lands.Length];
            for (int i = 0; i < submeshTriangles.Length; i++) submeshTriangles[i] = new List<int>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int baseIndex = vertices.Count;
                    // Four unique vertices per cell (flat shading friendly, simple).
                    vertices.Add(new Vector3(x, _cornerHeights[x, y] * HeightLevelSize, y));
                    vertices.Add(new Vector3(x + 1, _cornerHeights[x + 1, y] * HeightLevelSize, y));
                    vertices.Add(new Vector3(x, _cornerHeights[x, y + 1] * HeightLevelSize, y + 1));
                    vertices.Add(new Vector3(x + 1, _cornerHeights[x + 1, y + 1] * HeightLevelSize, y + 1));
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, 1));
                    uvs.Add(new Vector2(1, 1));

                    var triangles = submeshTriangles[map.Land(new CellPos(x, y))];
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            var mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                subMeshCount = submeshTriangles.Length,
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            for (int i = 0; i < submeshTriangles.Length; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            var materials = new Material[rules.Lands.Length];
            for (int i = 0; i < rules.Lands.Length; i++)
            {
                materials[i] = MaterialFactory.Solid(
                    LandColors.TryGetValue(rules.Lands[i].Id, out var color)
                        ? color
                        : new Color(0.5f, 0.2f, 0.5f));
            }
            renderer.sharedMaterials = materials;

            gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void ComputeCornerHeights()
        {
            int w = _map.Width;
            int h = _map.Height;
            _cornerHeights = new float[w + 1, h + 1];
            for (int cy = 0; cy <= h; cy++)
            {
                for (int cx = 0; cx <= w; cx++)
                {
                    // Corner height = max of the up-to-4 adjacent cell levels: keeps
                    // plateau tops flat and pushes the slope into the lower cells.
                    float best = 0;
                    for (int dy = -1; dy <= 0; dy++)
                    {
                        for (int dx = -1; dx <= 0; dx++)
                        {
                            var cell = new CellPos(cx + dx, cy + dy);
                            if (_map.InBounds(cell))
                            {
                                best = Mathf.Max(best, _map.HeightLevel(cell));
                            }
                        }
                    }
                    _cornerHeights[cx, cy] = best;
                }
            }
        }

        /// <summary>World-space surface height at (x, z), bilinear across the cell's corners.</summary>
        public float SurfaceHeight(float x, float z)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt(x), 0, _map.Width - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt(z), 0, _map.Height - 1);
            float fx = Mathf.Clamp01(x - cx);
            float fz = Mathf.Clamp01(z - cz);
            float bottom = Mathf.Lerp(_cornerHeights[cx, cz], _cornerHeights[cx + 1, cz], fx);
            float top = Mathf.Lerp(_cornerHeights[cx, cz + 1], _cornerHeights[cx + 1, cz + 1], fx);
            return Mathf.Lerp(bottom, top, fz) * HeightLevelSize;
        }

        public static Vector3 LeptonToWorld(LeptonPos pos) =>
            new Vector3(pos.X * LeptonsToWorld, 0f, pos.Y * LeptonsToWorld);

        public static LeptonPos WorldToLepton(Vector3 world) =>
            new LeptonPos(Mathf.RoundToInt(world.x / LeptonsToWorld), Mathf.RoundToInt(world.z / LeptonsToWorld));
    }
}
