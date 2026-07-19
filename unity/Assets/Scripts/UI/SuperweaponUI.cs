using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Superweapon buttons (top-left): charge %, READY glow, click to enter
    /// targeting mode, click the map to fire. Also the ion-storm banner.
    /// </summary>
    public sealed class SuperweaponUI : MonoBehaviour
    {
        private GameRunner _runner;
        private int _targetingIndex = -1;

        private static readonly Color Phosphor = new Color(0.2f, 1f, 0.33f);

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
        }

        private string T(string key) => Loc.T(key);

        public bool IsTargeting => _targetingIndex >= 0;

        private void Update()
        {
            if (_runner == null || !_runner.Ready) return;
            if (_targetingIndex < 0) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                _targetingIndex = -1;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    var target = TerrainView.WorldToLepton(hit.point);
                    _runner.IssueSuperweapon(_targetingIndex, target);
                    _targetingIndex = -1;
                }
            }
        }

        private void OnGUI()
        {
            if (_runner == null || !_runner.Ready || !_runner.MatchStarted) return;
            var rules = _runner.Game.World.Rules;
            float y = 60f;

            for (int i = 0; i < rules.Superweapons.Length; i++)
            {
                var spec = rules.Superweapons[i];
                var power = _runner.Game.Superweapons.GetPower(GameRunner.LocalPlayerId, i);
                if (!power.Granted) continue;

                float fraction = Mathf.Clamp01((float)power.Charge / spec.ChargeTicks);
                bool ready = fraction >= 1f;
                string label = T($"sw.{spec.Id}");
                string suffix = ready ? "  ● READY" : $"  {(int)(fraction * 100)}%";

                GUI.color = ready ? Phosphor : new Color(0.6f, 0.7f, 0.6f);
                if (_targetingIndex == i) GUI.color = new Color(1f, 0.8f, 0.2f);
                if (GUI.Button(new Rect(12, y, 210, 30), label + suffix) && ready)
                {
                    _targetingIndex = _targetingIndex == i ? -1 : i;
                }

                // Charge sweep.
                GUI.color = new Color(0.2f, 1f, 0.33f, 0.25f);
                GUI.DrawTexture(new Rect(12, y, 210 * fraction, 30), Texture2D.whiteTexture);
                y += 36f;
            }

            if (_targetingIndex >= 0)
            {
                GUI.color = new Color(1f, 0.8f, 0.2f);
                GUI.Label(new Rect(12, y + 4, 400, 24), "SELECT TARGET  (Esc / RMB to cancel)");
            }

            // Ion storm banner.
            var storm = _runner.Game.IonStorm;
            if (storm.Phase == TiberiumDusk.Sim.Systems.StormPhase.Warning)
            {
                GUI.color = new Color(1f, 0.75f, 0.2f);
                GUI.Label(new Rect(Screen.width / 2f - 140, 8, 320, 26), "⚡ " + T("ui.storm_warning"));
            }
            else if (storm.Phase == TiberiumDusk.Sim.Systems.StormPhase.Active)
            {
                GUI.color = new Color(0.5f, 0.7f, 1f);
                GUI.Label(new Rect(Screen.width / 2f - 100, 8, 260, 26), "⚡ " + T("ui.storm_active"));

                // Whole-screen cold blue tint while the storm rages.
                GUI.color = new Color(0.2f, 0.3f, 0.7f, 0.18f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }
    }
}
