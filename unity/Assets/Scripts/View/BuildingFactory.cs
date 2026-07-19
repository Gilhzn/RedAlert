using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Builds textured structure bodies from Resources/BuildingRecipes.json:
    /// each structure is a list of parts (box / cyl / wedge) with UVs into
    /// Resources/Textures/structure_atlas.png. Emissive parts become separate
    /// glow children. Recipes are authored in cell units, origin at the
    /// structure's ground center.
    /// </summary>
    public static class BuildingFactory
    {
        private const float UvInsetPx = 6f;

        private static bool _loadTried;
        private static JObject _structures;
        private static JObject _regions;
        private static float _cell = 128f, _grid = 8f;
        private static Material _atlasMaterial;

        private static void EnsureLoaded()
        {
            if (_loadTried) return;
            _loadTried = true;
            var text = Resources.Load<TextAsset>("BuildingRecipes");
            var atlas = Resources.Load<Texture2D>("Textures/structure_atlas");
            if (text == null || atlas == null) return;

            var root = JObject.Parse(text.text);
            _structures = (JObject)root["structures"];
            var atlasInfo = (JObject)root["atlas"];
            _regions = (JObject)atlasInfo["regions"];
            _cell = (float)atlasInfo["cell"];
            _grid = (float)atlasInfo["grid"];

            atlas.filterMode = FilterMode.Point;   // crisp painted-pixel look
            _atlasMaterial = MaterialFactory.Textured(atlas);
        }

        /// <summary>
        /// Try to build the recipe body for a structure. Returns the hull
        /// GameObject (carrying the mesh + a BoxCollider for picking), or null
        /// when no recipe exists. Extra glow parts are added as siblings under
        /// <paramref name="parent"/>.
        /// </summary>
        public static GameObject TryBuild(string specId, string faction, Transform parent, out float height)
        {
            EnsureLoaded();
            height = 1f;
            if (_structures == null || !(_structures[specId] is JArray parts)) return null;

            var builder = new MeshAccumulator();
            var glowParts = new List<(Mesh mesh, Color color)>();
            float maxY = 0.5f;

            foreach (var token in parts)
            {
                var part = (JObject)token;
                string emissive = (string)part["emissive"];
                var target = builder;
                if (emissive != null) target = new MeshAccumulator();

                string tex = ResolveTexture((string)part["tex"], faction);
                string top = (string)part["top"] is string t ? ResolveTexture(t, faction) : tex;
                var position = ReadVec(part["pos"]);
                var rotation = part["rot"] != null
                    ? Quaternion.Euler(ReadVec(part["rot"]))
                    : Quaternion.identity;

                string shape = (string)part["shape"];
                float partTop;
                if (shape == "cyl")
                {
                    float r = (float)part["r"], h = (float)part["h"];
                    target.AddCylinder(position, rotation, r, h, Region(tex), Region(top));
                    partTop = position.y + h;
                }
                else if (shape == "wedge")
                {
                    var size = ReadVec(part["size"]);
                    target.AddWedge(position, rotation, size, Region(tex));
                    partTop = position.y + size.y;
                }
                else
                {
                    var size = ReadVec(part["size"]);
                    target.AddBox(position, rotation, size, Region(tex), Region(top));
                    partTop = position.y + size.y;
                }
                if (partTop > maxY) maxY = partTop;

                if (emissive != null)
                {
                    ColorUtility.TryParseHtmlString(emissive, out var glowColor);
                    glowParts.Add((target.ToMesh(), glowColor));
                }
            }

            var hull = new GameObject("hull");
            hull.transform.SetParent(parent, worldPositionStays: false);
            hull.AddComponent<MeshFilter>().mesh = builder.ToMesh();
            hull.AddComponent<MeshRenderer>().sharedMaterial = _atlasMaterial;

            foreach (var (mesh, color) in glowParts)
            {
                var glow = new GameObject("glow");
                glow.transform.SetParent(parent, worldPositionStays: false);
                glow.AddComponent<MeshFilter>().mesh = mesh;
                glow.AddComponent<MeshRenderer>().sharedMaterial =
                    MaterialFactory.Emissive(color * 0.3f, color);
            }

            height = maxY;
            return hull;
        }

        private static string ResolveTexture(string name, string faction)
        {
            if (name != "owner") return name;
            return faction == "serpent" ? "red" : "gold";
        }

        private static Vector3 ReadVec(JToken token)
        {
            var arr = (JArray)token;
            return new Vector3((float)arr[0], (float)arr[1], (float)arr[2]);
        }

        /// <summary>Atlas UV rect for a named region, inset to avoid bleeding.</summary>
        private static Rect Region(string name)
        {
            var cellIndex = (JArray)_regions[name];
            float ix = (float)cellIndex[0], iy = (float)cellIndex[1];
            float atlasSize = _cell * _grid;
            float inset = UvInsetPx / atlasSize;
            float u0 = ix / _grid + inset;
            float u1 = (ix + 1f) / _grid - inset;
            // Image rows grow downward; UV v grows upward.
            float v0 = 1f - (iy + 1f) / _grid + inset;
            float v1 = 1f - iy / _grid - inset;
            return Rect.MinMaxRect(u0, v0, u1, v1);
        }

        /// <summary>Accumulates flat-shaded faces (unshared verts) with atlas UVs.</summary>
        private sealed class MeshAccumulator
        {
            private readonly List<Vector3> _verts = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _tris = new List<int>();

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(_verts);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uv)
            {
                int i = _verts.Count;
                _verts.Add(a); _verts.Add(b); _verts.Add(c); _verts.Add(d);
                _uvs.Add(new Vector2(uv.xMin, uv.yMin));
                _uvs.Add(new Vector2(uv.xMax, uv.yMin));
                _uvs.Add(new Vector2(uv.xMax, uv.yMax));
                _uvs.Add(new Vector2(uv.xMin, uv.yMax));
                _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 1);
                _tris.Add(i); _tris.Add(i + 3); _tris.Add(i + 2);
            }

            private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Rect uv)
            {
                int i = _verts.Count;
                _verts.Add(a); _verts.Add(b); _verts.Add(c);
                _uvs.Add(new Vector2(uv.xMin, uv.yMin));
                _uvs.Add(new Vector2(uv.xMax, uv.yMin));
                _uvs.Add(new Vector2(uv.center.x, uv.yMax));
                _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 1);
            }

            /// <summary>Box: pos = bottom-center; four sides + top (no bottom).</summary>
            public void AddBox(Vector3 pos, Quaternion rot, Vector3 size, Rect side, Rect top)
            {
                float hx = size.x / 2f, hz = size.z / 2f, h = size.y;
                Vector3 T(float x, float y, float z) => pos + rot * new Vector3(x, y, z);

                var b0 = T(-hx, 0, -hz); var b1 = T(hx, 0, -hz);
                var b2 = T(hx, 0, hz);   var b3 = T(-hx, 0, hz);
                var t0 = T(-hx, h, -hz); var t1 = T(hx, h, -hz);
                var t2 = T(hx, h, hz);   var t3 = T(-hx, h, hz);

                AddQuad(b1, b0, t0, t1, side);   // front  (-z, faces viewer)
                AddQuad(b3, b2, t2, t3, side);   // back   (+z)
                AddQuad(b0, b3, t3, t0, side);   // left   (-x)
                AddQuad(b2, b1, t1, t2, side);   // right  (+x)
                AddQuad(t0, t3, t2, t1, top);    // top    (+y)
            }

            /// <summary>Upright cylinder: pos = bottom-center.</summary>
            public void AddCylinder(Vector3 pos, Quaternion rot, float radius, float height, Rect side, Rect top, int segments = 12)
            {
                var bottomRing = new Vector3[segments];
                var topRing = new Vector3[segments];
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    var radial = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    bottomRing[i] = pos + rot * radial;
                    topRing[i] = pos + rot * (radial + Vector3.up * height);
                }
                for (int i = 0; i < segments; i++)
                {
                    int j = (i + 1) % segments;
                    // Slice of the side region per segment.
                    float u0 = Mathf.Lerp(side.xMin, side.xMax, (float)i / segments);
                    float u1 = Mathf.Lerp(side.xMin, side.xMax, (float)(i + 1) / segments);
                    var slice = Rect.MinMaxRect(u0, side.yMin, u1, side.yMax);
                    AddQuad(bottomRing[j], bottomRing[i], topRing[i], topRing[j], slice);
                }
                // Top cap fan.
                var center = pos + rot * (Vector3.up * height);
                for (int i = 0; i < segments; i++)
                {
                    int j = (i + 1) % segments;
                    AddTriangle(topRing[i], topRing[j], center, top);
                }
            }

            /// <summary>Wedge ramp: pos = bottom-center, slope rises toward +Z.</summary>
            public void AddWedge(Vector3 pos, Quaternion rot, Vector3 size, Rect uv)
            {
                float hx = size.x / 2f, hz = size.z / 2f, h = size.y;
                Vector3 T(float x, float y, float z) => pos + rot * new Vector3(x, y, z);

                var f0 = T(-hx, 0, -hz); var f1 = T(hx, 0, -hz);      // low front edge
                var b0 = T(-hx, 0, hz);  var b1 = T(hx, 0, hz);       // back bottom
                var t0 = T(-hx, h, hz);  var t1 = T(hx, h, hz);       // back top

                AddQuad(f0, f1, t1, t0, uv);       // slope
                AddQuad(b1, b0, t0, t1, uv);       // back face
                AddTriangle(f1, b1, t1, uv);       // right side
                AddTriangle(b0, f0, t0, uv);       // left side
            }
        }
    }
}
