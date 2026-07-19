using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Classic shroud rendering: a fog mesh draped over the terrain, sampling
    /// a per-cell texture — black for unexplored, dimmed for explored-but-
    /// unseen, clear when in sight. Enemy units in hidden cells are handled
    /// by UnitView visibility, not here.
    /// </summary>
    public sealed class FogView : MonoBehaviour
    {
        private const float Lift = 0.07f;
        private const int SyncEveryTicks = 5;

        private GameRunner _runner;
        private Texture2D _fogTexture;
        private Color32[] _pixels;
        private int _tickCounter;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _runner.WhenReady(InitAfterGame);
        }

        private void InitAfterGame()
        {
            BuildDrapedMesh();
            _runner.AfterTick += OnTick;
            RefreshTexture();
        }

        private void OnDestroy()
        {
            if (_runner != null) _runner.AfterTick -= OnTick;
        }

        private void BuildDrapedMesh()
        {
            var map = _runner.Game.World.Map;
            int w = map.Width;
            int h = map.Height;

            var vertices = new Vector3[(w + 1) * (h + 1)];
            var uvs = new Vector2[vertices.Length];
            for (int z = 0; z <= h; z++)
            {
                for (int x = 0; x <= w; x++)
                {
                    int i = z * (w + 1) + x;
                    float cx = Mathf.Clamp(x, 0.01f, w - 0.01f);
                    float cz = Mathf.Clamp(z, 0.01f, h - 0.01f);
                    vertices[i] = new Vector3(x, _runner.Terrain.SurfaceHeight(cx, cz) + Lift, z);
                    uvs[i] = new Vector2((float)x / w, (float)z / h);
                }
            }
            var triangles = new int[w * h * 6];
            int t = 0;
            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = z * (w + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + w + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + w + 1;
                    triangles[t++] = i + w + 2;
                }
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(new System.Collections.Generic.List<Vector3>(vertices));
            mesh.SetUVs(0, new System.Collections.Generic.List<Vector2>(uvs));
            mesh.SetTriangles(new System.Collections.Generic.List<int>(triangles), 0);
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();

            _fogTexture = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false);
            _fogTexture.wrapMode = TextureWrapMode.Clamp;
            _pixels = new Color32[map.Width * map.Height];

            // Sprites/Default: transparent, texture-driven, works on every pipeline.
            var material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = _fogTexture;
            renderer.sharedMaterial = material;
        }

        private void OnTick()
        {
            if (++_tickCounter % SyncEveryTicks == 0) RefreshTexture();
        }

        private void RefreshTexture()
        {
            var map = _runner.Game.World.Map;
            var vision = _runner.Game.Vision;
            int player = GameRunner.LocalPlayerId;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = new CellPos(x, y);
                    int i = y * map.Width + x;
                    if (!vision.IsExplored(player, cell))
                        _pixels[i] = new Color32(2, 4, 6, 255);          // hard shroud
                    else if (!vision.IsVisible(player, cell))
                        _pixels[i] = new Color32(4, 8, 12, 120);         // dim fog
                    else
                        _pixels[i] = new Color32(0, 0, 0, 0);            // clear
                }
            }
            _fogTexture.SetPixels32(_pixels);
            _fogTexture.Apply(false);
        }
    }
}
