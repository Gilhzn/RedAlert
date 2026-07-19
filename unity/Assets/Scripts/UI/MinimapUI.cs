using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Radar minimap (bottom-left): terrain colors + shroud + unit dots.
    /// Needs an owned, powered radar — otherwise animated static, the classic
    /// way. Click to jump the camera.
    /// </summary>
    public sealed class MinimapUI : MonoBehaviour
    {
        private const float SizePixels = 168f;
        private const int SyncEveryTicks = 10;

        private GameRunner _runner;
        private CameraRig _camera;
        private Texture2D _texture;
        private Color32[] _terrainBase;
        private Color32[] _pixels;
        private int _tickCounter;
        private int _noiseSeed;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _camera = FindFirstObjectByType<CameraRig>();
            var map = _runner.Game.World.Map;
            _texture = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _pixels = new Color32[map.Width * map.Height];
            BakeTerrain();
            _runner.AfterTick += OnTick;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_runner != null) _runner.AfterTick -= OnTick;
        }

        private void BakeTerrain()
        {
            var world = _runner.Game.World;
            var map = world.Map;
            _terrainBase = new Color32[map.Width * map.Height];
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var land = world.Rules.Lands[map.Land(new CellPos(x, y))].Id;
                    Color32 color = land switch
                    {
                        "water" => new Color32(24, 44, 66, 255),
                        "road" => new Color32(70, 72, 76, 255),
                        "cliff" => new Color32(46, 48, 54, 255),
                        "rough" => new Color32(52, 56, 48, 255),
                        "ice" => new Color32(130, 148, 160, 255),
                        _ => new Color32(58, 66, 56, 255),
                    };
                    // Height shading.
                    int height = map.HeightLevel(new CellPos(x, y));
                    if (height > 0)
                    {
                        color.r = (byte)Mathf.Min(255, color.r + height * 18);
                        color.g = (byte)Mathf.Min(255, color.g + height * 18);
                        color.b = (byte)Mathf.Min(255, color.b + height * 14);
                    }
                    _terrainBase[y * map.Width + x] = color;
                }
            }
        }

        private bool RadarOnline()
        {
            var world = _runner.Game.World;
            if (world.Players[GameRunner.LocalPlayerId].LowPower) return false;
            foreach (var e in world.Entities)
            {
                if (e.Alive && e.Owner == GameRunner.LocalPlayerId
                    && (e.Spec.Id == "dm_radar" || e.Spec.Id == "so_radar"))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnTick()
        {
            if (++_tickCounter % SyncEveryTicks == 0) Refresh();
        }

        private void Refresh()
        {
            var world = _runner.Game.World;
            var map = world.Map;

            if (!RadarOnline())
            {
                // Animated static.
                _noiseSeed = _noiseSeed * 1103515245 + 12345;
                int noise = _noiseSeed;
                for (int i = 0; i < _pixels.Length; i++)
                {
                    noise = noise * 1103515245 + 12345;
                    byte v = (byte)(20 + ((noise >> 16) & 63));
                    _pixels[i] = new Color32(v, (byte)(v + 8), v, 255);
                }
            }
            else
            {
                var vision = _runner.Game.Vision;
                int player = GameRunner.LocalPlayerId;
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        int i = y * map.Width + x;
                        var cell = new CellPos(x, y);
                        if (!vision.IsExplored(player, cell))
                        {
                            _pixels[i] = new Color32(3, 5, 6, 255);
                            continue;
                        }
                        var color = _terrainBase[i];
                        if (world.Crystal.HasCrystal(cell))
                        {
                            color = world.Crystal.TypeAt(cell) == TiberiumDusk.Sim.WorldModel.CrystalType.Blue
                                ? new Color32(60, 150, 220, 255)
                                : new Color32(50, 190, 90, 255);
                        }
                        if (!vision.IsVisible(player, cell))
                        {
                            color.r = (byte)(color.r / 2);
                            color.g = (byte)(color.g / 2);
                            color.b = (byte)(color.b / 2);
                        }
                        _pixels[i] = color;
                    }
                }

                // Unit/structure dots (visible or own).
                foreach (var e in world.Entities)
                {
                    if (!e.Alive) continue;
                    var cell = e.HomeCell;
                    if (!map.InBounds(cell)) continue;
                    bool mine = e.Owner == GameRunner.LocalPlayerId;
                    if (!mine && !_runner.Game.Vision.IsVisible(GameRunner.LocalPlayerId, cell)) continue;
                    if (!mine && e.IsCloaked) continue;
                    var tint = UnitViewManager.PlayerColors[e.Owner % UnitViewManager.PlayerColors.Length];
                    var dot = new Color32((byte)(tint.r * 255), (byte)(tint.g * 255), (byte)(tint.b * 255), 255);
                    int size = e.Spec.IsStructure ? 2 : 1;
                    for (int dy = 0; dy < size; dy++)
                        for (int dx = 0; dx < size; dx++)
                        {
                            int px = cell.X + dx, py = cell.Y + dy;
                            if (px < map.Width && py < map.Height) _pixels[py * map.Width + px] = dot;
                        }
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void OnGUI()
        {
            var rect = new Rect(12, Screen.height - SizePixels - 12, SizePixels, SizePixels);

            GUI.color = new Color(0.07f, 0.09f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
            // Flip vertically: texture row 0 = map row 0 (north at top of the widget).
            GUI.DrawTexture(rect, _texture);

            // Click to move the camera.
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                var map = _runner.Game.World.Map;
                float u = (Event.current.mousePosition.x - rect.x) / rect.width;
                float v = 1f - (Event.current.mousePosition.y - rect.y) / rect.height;
                _camera.FocusOn(new Vector3(u * map.Width, 0f, v * map.Height));
                Event.current.Use();
            }
        }
    }
}
