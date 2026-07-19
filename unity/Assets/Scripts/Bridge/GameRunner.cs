using System.Collections.Generic;
using System.IO;
using System.Collections.Generic;
using TiberiumDusk.Balance;
using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Hosts the deterministic simulation inside Unity: fixed 15 Hz ticks with
    /// an accumulator, order queueing from input, and an interpolation alpha
    /// for the view. This is the ONLY place that calls Game.Tick.
    /// </summary>
    public sealed class GameRunner : MonoBehaviour
    {
        /// <summary>Local player id — 0 in skirmish, assigned by the relay in multiplayer.</summary>
        public static int LocalPlayerId = 0;
        private const float TickSeconds = 1f / Game.TicksPerSecond;

        public Game Game { get; private set; }
        /// <summary>0..1 progress between the last two ticks, for view interpolation.</summary>
        public float Alpha { get; private set; }
        /// <summary>False until the player presses START in the menu.</summary>
        public bool MatchStarted { get; private set; }
        /// <summary>Data loaded + world constructed (WebGL loads over HTTP, so this is async).</summary>
        public bool Ready { get; private set; }
        private readonly List<System.Action> _readyCallbacks = new List<System.Action>();
        /// <summary>Multiplayer state.</summary>
        public bool NetMode { get; private set; }
        public string NetStatus { get; private set; } = "";
        private WebSocketTransport _netTransport;
        private LockstepClient _netClient;
        private string _netFaction = "dominion";

        public event System.Action AfterTick;

        private readonly List<Order> _pendingOrders = new List<Order>();
        private float _accumulator;

        public TerrainView Terrain { get; private set; }

        private void Submit(Order order)
        {
            if (NetMode) _netClient?.Issue(order);
            else _pendingOrders.Add(order);
        }

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(BootstrapWebGL());
#else
            InitializeWithContent(GameDataLoader.ReadDirectory(ResolveDataDirectory()));
#endif
        }

        /// <summary>Run a callback once data + world exist (immediately if they already do).</summary>
        public void WhenReady(System.Action callback)
        {
            if (Ready) callback();
            else _readyCallbacks.Add(callback);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>WebGL: StreamingAssets is a URL — fetch every data file over HTTP.</summary>
        private System.Collections.IEnumerator BootstrapWebGL()
        {
            var files = new Dictionary<string, string>();
            foreach (var name in GameDataLoader.DataFiles)
            {
                using var request = UnityWebRequest.Get(
                    Application.streamingAssetsPath + "/data/" + name);
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"data fetch failed: {name}: {request.error}");
                    yield break;
                }
                files[name] = request.downloadHandler.text;
            }
            InitializeWithContent(files);
        }
#endif

        private void InitializeWithContent(Dictionary<string, string> content)
        {
            Loc.InitFromContent(content);
            var rules = RulesCompiler.CompileFromContent(content);
            var map = DemoMap.Build(rules);
            var settings = new TiberiumDusk.Sim.Data.GameSettings
            {
                IonStormsEnabled = true,
                CratesEnabled = true,
            };
            Game = new Game(rules, map, seed: 20260719UL, settings);
            Game.Recorder = new ReplayLog { Seed = 20260719UL, IonStormsEnabled = true, CratesEnabled = true };
            DemoMap.SpawnUnits(Game);

            Terrain = TerrainView.Build(map, rules, transform);

            Ready = true;
            foreach (var callback in _readyCallbacks) callback();
            _readyCallbacks.Clear();
        }

        /// <summary>Called by the main menu; the sim only ticks after this.</summary>
        public void StartMatch(TiberiumDusk.Sim.Systems.AIDifficulty difficulty)
        {
            if (MatchStarted) return;
            Game.AI.Enable(1, difficulty);
            MatchStarted = true;
        }

        /// <summary>Multiplayer: connect to a relay and wait for the match to start.</summary>
        public async void ConnectMultiplayer(string url, string faction)
        {
            if (MatchStarted || NetMode) return;
            NetMode = true;
            _netFaction = faction;
            NetStatus = "connecting...";
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var webglTransport = new WebGLNetTransport();
                webglTransport.Connect(url);
                _netClient = new LockstepClient(webglTransport);
                await System.Threading.Tasks.Task.CompletedTask;
#else
                _netTransport = new WebSocketTransport();
                await _netTransport.ConnectAsync(url);
                _netClient = new LockstepClient(_netTransport);
#endif
                _netClient.SendJoin("player", faction);
                NetStatus = "waiting for players...";
            }
            catch (System.Exception e)
            {
                NetStatus = "connection failed: " + e.Message;
                NetMode = false;
            }
        }

        /// <summary>Both clients must build the exact same starting world.</summary>
        private Game BuildNetGame(ulong seed, string[] factions)
        {
            var rules = Game.World.Rules;   // same compiled rules
            var map = DemoMap.Build(rules);
            var netGame = new Game(rules, map, seed);
            DemoMap.SpawnNetUnits(netGame, factions);
            return netGame;
        }

        private void Update()
        {
            if (NetMode)
            {
                UpdateNet();
                return;
            }
            if (!MatchStarted) return;
            _accumulator += Time.deltaTime;
            while (_accumulator >= TickSeconds)
            {
                _accumulator -= TickSeconds;
                Game.Tick(_pendingOrders);
                _pendingOrders.Clear();
                AfterTick?.Invoke();
            }
            Alpha = _accumulator / TickSeconds;
        }

        private void UpdateNet()
        {
            if (_netClient == null) return;

            if (!MatchStarted)
            {
                _netClient.Pump(0);
                if (_netClient.Started && _netClient.Game == null)
                {
                    LocalPlayerId = _netClient.LocalPlayerId;
                    var netGame = BuildNetGame(_netClient.Seed, _netClient.Factions);
                    _netClient.AttachGame(netGame);
                    Game = netGame;
                    MatchStarted = true;
                    NetStatus = "";
                }
                return;
            }

            if (_netClient.Desynced)
            {
                NetStatus = "DESYNC — match aborted";
                return;
            }

            _accumulator += Time.deltaTime;
            while (_accumulator >= TickSeconds)
            {
                _accumulator -= TickSeconds;
                if (_netClient.Pump(1) == 1)
                {
                    AfterTick?.Invoke();
                }
                else
                {
                    // Waiting on the network: don't bank time, or we'd fast-forward.
                    _accumulator = 0f;
                    break;
                }
            }
            Alpha = _accumulator / TickSeconds;
        }

        public void IssueMove(int entityId, LeptonPos target)
        {
            Submit(new Order(OrderType.Move, LocalPlayerId, Game.CurrentTick + 1,
                entityId, targetPos: target));
        }

        public void IssueStop(int entityId)
        {
            Submit(new Order(OrderType.Stop, LocalPlayerId, Game.CurrentTick + 1, entityId));
        }

        public void IssueAttack(int entityId, int targetEntityId)
        {
            Submit(new Order(OrderType.Attack, LocalPlayerId, Game.CurrentTick + 1,
                entityId, targetEntityId));
        }

        public void IssueAttackMove(int entityId, LeptonPos target)
        {
            Submit(new Order(OrderType.AttackMove, LocalPlayerId, Game.CurrentTick + 1,
                entityId, targetPos: target));
        }

        public void IssueSuperweapon(int superweaponIndex, LeptonPos target)
        {
            Submit(new Order(OrderType.UseSuperweapon, LocalPlayerId,
                Game.CurrentTick + 1, data: superweaponIndex, targetPos: target));
        }

        public void IssueDeploy(int entityId)
        {
            Submit(new Order(OrderType.Deploy, LocalPlayerId, Game.CurrentTick + 1, entityId));
        }

        public void IssueSell(int entityId)
        {
            Submit(new Order(OrderType.Sell, LocalPlayerId, Game.CurrentTick + 1, entityId));
        }

        public void IssueBuildStart(int specIndex)
        {
            Submit(new Order(OrderType.BuildStart, LocalPlayerId, Game.CurrentTick + 1, data: specIndex));
        }

        public void IssueBuildCancel(int specIndex)
        {
            Submit(new Order(OrderType.BuildCancel, LocalPlayerId, Game.CurrentTick + 1, data: specIndex));
        }

        public void IssuePlaceStructure(int specIndex, LeptonPos origin)
        {
            Submit(new Order(OrderType.PlaceStructure, LocalPlayerId, Game.CurrentTick + 1,
                data: specIndex, targetPos: origin));
        }

        /// <summary>
        /// data/ lives at the repo root. In the editor we read it directly; in
        /// player builds it is copied into StreamingAssets (build hook, Phase 10).
        /// </summary>
        public static string ResolveDataDirectory()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
#else
            return Path.Combine(Application.streamingAssetsPath, "data");
#endif
        }
    }
}
