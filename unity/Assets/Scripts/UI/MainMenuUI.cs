using TiberiumDusk.Sim.Systems;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Pre-match menu in the terminal aesthetic: title, AI difficulty,
    /// language toggle, start. The sim doesn't tick until START is pressed.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        private GameRunner _runner;
        private AudioManager _audio;
        private int _difficulty = 1;   // 0 easy, 1 normal, 2 hard
        private string _serverUrl = "ws://localhost:7777/";
        private string _mpFaction = "dominion";

        private static readonly Color Phosphor = new Color(0.2f, 1f, 0.33f);

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _audio = FindFirstObjectByType<AudioManager>();
        }

        private void OnGUI()
        {
            if (_runner == null || _runner.MatchStarted) return;

            GUI.color = new Color(0.02f, 0.04f, 0.03f, 0.96f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float cx = Screen.width / 2f;
            float y = Screen.height * 0.22f;

            var title = new GUIStyle(GUI.skin.label) { fontSize = 52, alignment = TextAnchor.MiddleCenter };
            GUI.color = Phosphor;
            GUI.Label(new Rect(0, y, Screen.width, 70), Loc.T("menu.title"), title);
            y += 74;

            var sub = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(0.55f, 0.75f, 0.6f);
            GUI.Label(new Rect(0, y, Screen.width, 28), Loc.T("menu.subtitle"), sub);
            y += 64;

            // Difficulty selector.
            GUI.color = Phosphor;
            GUI.Label(new Rect(cx - 220, y, 200, 28), Loc.T("menu.difficulty"));
            string[] levels = { "menu.difficulty.easy", "menu.difficulty.normal", "menu.difficulty.hard" };
            for (int i = 0; i < 3; i++)
            {
                GUI.color = i == _difficulty ? Phosphor : new Color(0.45f, 0.55f, 0.45f);
                if (GUI.Button(new Rect(cx - 20 + i * 90, y, 84, 28), Loc.T(levels[i])))
                {
                    _difficulty = i;
                    _audio?.PlayClick();
                }
            }
            y += 44;

            // Language toggle.
            GUI.color = Phosphor;
            GUI.Label(new Rect(cx - 220, y, 200, 28), Loc.T("menu.language"));
            GUI.color = Loc.Language == "en" ? Phosphor : new Color(0.45f, 0.55f, 0.45f);
            if (GUI.Button(new Rect(cx - 20, y, 84, 28), "English"))
            {
                Loc.SetLanguage("en");
                _audio?.PlayClick();
            }
            GUI.color = Loc.Language == "he" ? Phosphor : new Color(0.45f, 0.55f, 0.45f);
            if (GUI.Button(new Rect(cx + 70, y, 84, 28), Loc.Bidi("עברית")))
            {
                Loc.SetLanguage("he");
                _audio?.PlayClick();
            }
            y += 70;

            GUI.color = Phosphor;
            var big = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            if (GUI.Button(new Rect(cx - 130, y, 260, 52), Loc.T("menu.start"), big))
            {
                _audio?.PlayClick();
                _runner.StartMatch((AIDifficulty)_difficulty);
            }
            y += 76;

            // ---- Multiplayer (LAN relay) ----
            GUI.color = new Color(0.55f, 0.75f, 0.6f);
            GUI.Label(new Rect(cx - 220, y, 440, 24), "MULTIPLAYER  —  dotnet run --project server/TiberiumDusk.Server");
            y += 26;
            GUI.color = Phosphor;
            _serverUrl = GUI.TextField(new Rect(cx - 220, y, 250, 26), _serverUrl);
            GUI.color = _mpFaction == "dominion" ? Phosphor : new Color(0.45f, 0.55f, 0.45f);
            if (GUI.Button(new Rect(cx + 36, y, 90, 26), "Dominion")) _mpFaction = "dominion";
            GUI.color = _mpFaction == "serpent" ? Phosphor : new Color(0.45f, 0.55f, 0.45f);
            if (GUI.Button(new Rect(cx + 130, y, 90, 26), "Serpent")) _mpFaction = "serpent";
            y += 32;
            GUI.color = Phosphor;
            if (GUI.Button(new Rect(cx - 220, y, 120, 30), "JOIN") && !_runner.NetMode)
            {
                _audio?.PlayClick();
                _runner.ConnectMultiplayer(_serverUrl, _mpFaction);
            }
            GUI.color = new Color(1f, 0.8f, 0.3f);
            GUI.Label(new Rect(cx - 90, y + 4, 420, 24), _runner.NetStatus);

            GUI.color = Color.white;
        }
    }
}
