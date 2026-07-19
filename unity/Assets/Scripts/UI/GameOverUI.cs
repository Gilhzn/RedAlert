using System.IO;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>Victory/defeat overlay + replay autosave when the match ends.</summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        private GameRunner _runner;
        private int _winner = -1;
        private string _replayPath;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _runner.WhenReady(InitAfterGame);
        }

        private void InitAfterGame()
        {
            _runner.Game.GameEnded += OnGameEnded;
        }

        private void OnGameEnded(int winner)
        {
            _winner = winner;
            if (_runner.Game.Recorder != null)
            {
                try
                {
                    _replayPath = Path.Combine(Application.persistentDataPath,
                        $"replay_{System.DateTime.Now:yyyyMMdd_HHmmss}.rdt");
                    File.WriteAllBytes(_replayPath, _runner.Game.Recorder.Serialize());
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("replay save failed: " + e.Message);
                }
            }
        }

        private void OnGUI()
        {
            if (_winner < 0) return;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            bool victory = _winner == GameRunner.LocalPlayerId;
            GUI.color = victory ? new Color(0.2f, 1f, 0.33f) : new Color(1f, 0.25f, 0.2f);
            var style = new GUIStyle(GUI.skin.label) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height / 2f - 80, Screen.width, 90),
                victory ? Loc.T("ui.victory") : Loc.T("ui.defeat"), style);

            GUI.color = Color.white;
            var small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            if (_replayPath != null)
            {
                GUI.Label(new Rect(0, Screen.height / 2f + 20, Screen.width, 30),
                    Loc.T("ui.replay_saved") + " " + _replayPath, small);
            }
            GUI.Label(new Rect(0, Screen.height / 2f + 50, Screen.width, 30),
                Loc.T("ui.exit_hint"), small);
        }
    }
}
