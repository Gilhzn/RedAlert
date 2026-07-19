using System.Collections.Generic;
using TiberiumDusk.Balance;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Systems;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Phase 3 sidebar (IMGUI; the polished terminal HUD arrives in Phase 8):
    /// credits ticker, power bar, queue tabs, build buttons with progress and
    /// READY state, structure placement mode with a validity ghost.
    /// </summary>
    public sealed class SidebarUI : MonoBehaviour
    {
        private const float Width = 250f;

        private static readonly ProductionQueue[] Tabs =
            { ProductionQueue.Structure, ProductionQueue.Infantry, ProductionQueue.Vehicle };
        private static readonly string[] TabLocaleKeys = { "ui.tab.structures", "ui.tab.infantry", "ui.tab.vehicles" };

        private GameRunner _runner;
        private Dictionary<string, string> _locale;
        private int _activeTab;
        private float _displayedCredits;

        // Placement mode.
        private int _placingSpecIndex = -1;
        private GameObject _ghost;
        private Material _ghostValid, _ghostInvalid;

        private static readonly Color Phosphor = new Color(0.2f, 1f, 0.33f);
        private static readonly Color PanelDark = new Color(0.07f, 0.09f, 0.08f, 0.93f);

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _locale = LocaleLoader.Load(GameRunner.ResolveDataDirectory(), "en");
            _displayedCredits = _runner.Game.World.Players[GameRunner.LocalPlayerId].Credits;
            _ghostValid = MaterialFactory.Unlit(new Color(0.2f, 1f, 0.33f, 1f));
            _ghostInvalid = MaterialFactory.Unlit(new Color(1f, 0.25f, 0.2f, 1f));
        }

        private string T(string key) => _locale.TryGetValue(key, out var text) ? text : key;

        private void Update()
        {
            // Credit ticker: display value chases the real value.
            float real = _runner.Game.World.Players[GameRunner.LocalPlayerId].Credits;
            float speed = Mathf.Max(40f, Mathf.Abs(real - _displayedCredits) * 3f);
            _displayedCredits = Mathf.MoveTowards(_displayedCredits, real, speed * Time.deltaTime);

            UpdatePlacement();
        }

        public bool IsPointerOverSidebar(Vector3 mousePosition) => mousePosition.x > Screen.width - Width;
        public bool IsPlacing => _placingSpecIndex >= 0;

        // ---------- Placement mode ----------

        private void UpdatePlacement()
        {
            if (_placingSpecIndex < 0) return;

            var spec = _runner.Game.World.Rules.Units[_placingSpecIndex];
            var queue = _runner.Game.Production.GetQueue(GameRunner.LocalPlayerId, spec.Buildable.Queue);
            if (!queue.ReadyForPlacement || queue.ActiveSpecIndex != _placingSpecIndex)
            {
                // Placed (or canceled) — leave placement mode.
                ExitPlacement();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitPlacement();
                return;
            }

            var camera = Camera.main;
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 500f)) return;

            var s = spec.Structure;
            var origin = new CellPos(
                Mathf.FloorToInt(hit.point.x) - s.FootprintW / 2,
                Mathf.FloorToInt(hit.point.z) - s.FootprintH / 2);

            bool valid = PlacementValidator.CanPlace(
                _runner.Game.World, GameRunner.LocalPlayerId, spec, origin);

            EnsureGhost(s);
            float cx = origin.X + s.FootprintW * 0.5f;
            float cz = origin.Y + s.FootprintH * 0.5f;
            _ghost.transform.position = new Vector3(cx, _runner.Terrain.SurfaceHeight(cx, cz) + 0.06f, cz);
            _ghost.GetComponent<MeshRenderer>().sharedMaterial = valid ? _ghostValid : _ghostInvalid;

            if (valid && Input.GetMouseButtonDown(0) && !IsPointerOverSidebar(Input.mousePosition))
            {
                _runner.IssuePlaceStructure(_placingSpecIndex,
                    new LeptonPos(origin.X * LeptonPos.LeptonsPerCell + 1, origin.Y * LeptonPos.LeptonsPerCell + 1));
            }
        }

        private void EnsureGhost(StructureSpec s)
        {
            if (_ghost != null) return;
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(_ghost.GetComponent<Collider>());
            _ghost.transform.localScale = new Vector3(s.FootprintW * 0.98f, 0.1f, s.FootprintH * 0.98f);
        }

        private void ExitPlacement()
        {
            _placingSpecIndex = -1;
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }
        }

        // ---------- Sidebar ----------

        private void OnGUI()
        {
            var player = _runner.Game.World.Players[GameRunner.LocalPlayerId];
            var panel = new Rect(Screen.width - Width, 0, Width, Screen.height);

            GUI.color = PanelDark;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Phosphor;

            float y = 10f;
            GUI.Label(new Rect(panel.x + 12, y, Width - 24, 24),
                $"{T("ui.credits")}: {Mathf.RoundToInt(_displayedCredits)}");
            y += 26;

            // Power bar.
            GUI.Label(new Rect(panel.x + 12, y, Width - 24, 20),
                $"{T("ui.power")}: {player.PowerProduced} / {player.PowerDrained}");
            y += 22;
            var barRect = new Rect(panel.x + 12, y, Width - 24, 8);
            GUI.color = new Color(0.2f, 0.25f, 0.2f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            float usage = player.PowerProduced > 0
                ? Mathf.Clamp01((float)player.PowerDrained / player.PowerProduced)
                : (player.PowerDrained > 0 ? 1f : 0f);
            GUI.color = player.LowPower ? new Color(1f, 0.25f, 0.2f) : Phosphor;
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Max(usage, 0.02f), barRect.height),
                Texture2D.whiteTexture);
            y += 18;

            // Tabs.
            GUI.color = Color.white;
            float tabWidth = (Width - 24) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                GUI.color = i == _activeTab ? Phosphor : new Color(0.5f, 0.6f, 0.5f);
                if (GUI.Button(new Rect(panel.x + 12 + i * tabWidth, y, tabWidth - 2, 24), T(TabLocaleKeys[i])))
                {
                    _activeTab = i;
                }
            }
            y += 32;

            GUI.color = Color.white;
            DrawBuildList(panel, ref y, Tabs[_activeTab]);
        }

        private void DrawBuildList(Rect panel, ref float y, ProductionQueue queueClass)
        {
            var world = _runner.Game.World;
            var production = _runner.Game.Production;
            var queue = production.GetQueue(GameRunner.LocalPlayerId, queueClass);

            foreach (var spec in world.Rules.Units)
            {
                if (spec.Buildable == null || spec.Buildable.Queue != queueClass) continue;
                if (!production.CanBuild(GameRunner.LocalPlayerId, spec)) continue;

                var rect = new Rect(panel.x + 12, y, Width - 24, 36);
                bool isActive = queue.ActiveSpecIndex == spec.Index;
                float progress = 0f;
                if (isActive)
                {
                    long total = (long)spec.Buildable.Cost * 1000;
                    progress = total > 0 ? (float)queue.ProgressMilli / total : 0f;
                }

                string label = _locale.TryGetValue($"structure.{spec.Id}", out var name) ? name
                    : _locale.TryGetValue($"unit.{spec.Id}", out name) ? name : spec.Id;
                string suffix = isActive
                    ? (queue.ReadyForPlacement ? $"  [{T("ui.ready")}]" : $"  {(int)(progress * 100)}%")
                    : $"  ${spec.Buildable.Cost}";

                GUI.color = isActive && queue.ReadyForPlacement ? Phosphor : Color.white;
                if (GUI.Button(rect, label + suffix))
                {
                    OnItemClicked(spec, queue);
                }

                // Progress sweep over the button.
                if (isActive && !queue.ReadyForPlacement)
                {
                    GUI.color = new Color(0.2f, 1f, 0.33f, 0.25f);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * progress, rect.height),
                        Texture2D.whiteTexture);
                }

                // Cancel button for the active item.
                if (isActive)
                {
                    GUI.color = new Color(1f, 0.4f, 0.35f);
                    if (GUI.Button(new Rect(rect.xMax - 24, rect.y + 6, 22, 24), "X"))
                    {
                        _runner.IssueBuildCancel(spec.Index);
                        if (_placingSpecIndex == spec.Index) ExitPlacement();
                    }
                }

                GUI.color = Color.white;
                y += 40;
            }
        }

        private void OnItemClicked(UnitSpec spec, ProductionSystem.QueueState queue)
        {
            if (queue.ActiveSpecIndex == spec.Index && queue.ReadyForPlacement && spec.IsStructure)
            {
                _placingSpecIndex = spec.Index;   // enter placement mode
            }
            else
            {
                _runner.IssueBuildStart(spec.Index);
            }
        }
    }
}
