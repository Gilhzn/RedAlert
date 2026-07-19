using System.Collections.Generic;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Classic RTS mouse control: left click / drag-box to select own units,
    /// right click to move (groups share a flow field sim-side), S to stop.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        private const float DragThresholdPixels = 8f;

        private GameRunner _runner;
        private UnitViewManager _units;
        private SidebarUI _sidebar;
        private SuperweaponUI _superweapons;
        private Camera _camera;

        private readonly HashSet<int> _selected = new HashSet<int>();
        private Vector3 _dragStart;
        private bool _dragging;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _units = FindFirstObjectByType<UnitViewManager>();
            _sidebar = FindFirstObjectByType<SidebarUI>();
            _superweapons = FindFirstObjectByType<SuperweaponUI>();
            _camera = Camera.main;
        }

        private void Update()
        {
            // The sidebar owns the mouse while placing a structure or hovered.
            if (_sidebar != null && (_sidebar.IsPlacing || _sidebar.IsPointerOverSidebar(Input.mousePosition)))
            {
                _dragging = false;
                return;
            }
            if (_superweapons != null && _superweapons.IsTargeting)
            {
                _dragging = false;
                return;
            }
            HandleSelection();
            HandleCommands();
        }

        public int SelectedCount => _selected.Count;

        private void HandleSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragStart = Input.mousePosition;
                _dragging = true;
            }

            if (_dragging && Input.GetMouseButtonUp(0))
            {
                _dragging = false;
                bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if ((Input.mousePosition - _dragStart).magnitude < DragThresholdPixels)
                {
                    ClickSelect(additive);
                }
                else
                {
                    BoxSelect(GetDragRect(), additive);
                }
                RefreshRings();
            }
        }

        private void ClickSelect(bool additive)
        {
            if (!additive) _selected.Clear();
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f))
            {
                var reference = hit.collider.GetComponentInParent<EntityRef>();
                if (reference != null && reference.Owner == GameRunner.LocalPlayerId)
                {
                    if (additive && _selected.Contains(reference.EntityId))
                        _selected.Remove(reference.EntityId);
                    else
                        _selected.Add(reference.EntityId);
                }
            }
        }

        private void BoxSelect(Rect screenRect, bool additive)
        {
            if (!additive) _selected.Clear();
            foreach (var pair in _units.Views)
            {
                var view = pair.Value;
                if (view.Owner != GameRunner.LocalPlayerId) continue;
                var screen = _camera.WorldToScreenPoint(view.transform.position);
                if (screen.z > 0f && screenRect.Contains(new Vector2(screen.x, screen.y)))
                {
                    _selected.Add(view.EntityId);
                }
            }
        }

        private void HandleCommands()
        {
            if (Input.GetMouseButtonDown(1) && _selected.Count > 0)
            {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    // Right-click on an enemy = attack (engineers capture); on ground = move.
                    var enemyRef = hit.collider.GetComponentInParent<EntityRef>();
                    if (enemyRef != null && enemyRef.Owner != GameRunner.LocalPlayerId)
                    {
                        foreach (var id in _selected)
                        {
                            _runner.IssueAttack(id, enemyRef.EntityId);
                        }
                    }
                    else
                    {
                        var target = TerrainView.WorldToLepton(hit.point);
                        bool attackMove = Input.GetKey(KeyCode.A);
                        foreach (var id in _selected)
                        {
                            if (attackMove) _runner.IssueAttackMove(id, target);
                            else _runner.IssueMove(id, target);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                foreach (var id in _selected)
                {
                    _runner.IssueStop(id);
                }
            }

            // Deploy (MCV → Construction Yard).
            if (Input.GetKeyDown(KeyCode.D))
            {
                foreach (var id in _selected)
                {
                    _runner.IssueDeploy(id);
                }
            }

            // Sell selected structures.
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                foreach (var id in _selected)
                {
                    _runner.IssueSell(id);
                }
            }
        }

        private void RefreshRings()
        {
            foreach (var pair in _units.Views)
            {
                pair.Value.SetSelected(_selected.Contains(pair.Key));
            }
        }

        private Rect GetDragRect()
        {
            var a = _dragStart;
            var b = Input.mousePosition;
            return Rect.MinMaxRect(
                Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private void OnGUI()
        {
            // Drag-box visual.
            if (_dragging && (Input.mousePosition - _dragStart).magnitude >= DragThresholdPixels)
            {
                var rect = GetDragRect();
                var guiRect = new Rect(rect.xMin, Screen.height - rect.yMax, rect.width, rect.height);
                GUI.color = new Color(0.2f, 1f, 0.33f, 0.15f);
                GUI.DrawTexture(guiRect, Texture2D.whiteTexture);
                GUI.color = new Color(0.2f, 1f, 0.33f, 0.9f);
                DrawRectBorder(guiRect, 2f);
                GUI.color = Color.white;
            }

            // Debug HUD (polished HUD arrives in Phase 8).
            GUI.color = new Color(0.2f, 1f, 0.33f);
            GUI.Label(new Rect(12, 8, 700, 22),
                Loc.T("hud.title") + $"   tick {_runner.Game.CurrentTick}   ({_selected.Count})");
            GUI.Label(new Rect(12, 30, 700, 22),
                Loc.T("hud.controls"));
            GUI.color = Color.white;
        }

        private static void DrawRectBorder(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        }
    }
}
