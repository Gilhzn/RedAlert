using System.Collections.Generic;
using System.IO;
using TiberiumDusk.Balance;
using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Hosts the deterministic simulation inside Unity: fixed 15 Hz ticks with
    /// an accumulator, order queueing from input, and an interpolation alpha
    /// for the view. This is the ONLY place that calls Game.Tick.
    /// </summary>
    public sealed class GameRunner : MonoBehaviour
    {
        public const int LocalPlayerId = 0;
        private const float TickSeconds = 1f / Game.TicksPerSecond;

        public Game Game { get; private set; }
        /// <summary>0..1 progress between the last two ticks, for view interpolation.</summary>
        public float Alpha { get; private set; }
        /// <summary>False until the player presses START in the menu.</summary>
        public bool MatchStarted { get; private set; }

        public event System.Action AfterTick;

        private readonly List<Order> _pendingOrders = new List<Order>();
        private float _accumulator;

        public TerrainView Terrain { get; private set; }

        private void Awake()
        {
            Loc.Init(ResolveDataDirectory());
            var rules = RulesCompiler.CompileFromDirectory(ResolveDataDirectory());
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
        }

        /// <summary>Called by the main menu; the sim only ticks after this.</summary>
        public void StartMatch(TiberiumDusk.Sim.Systems.AIDifficulty difficulty)
        {
            if (MatchStarted) return;
            Game.AI.Enable(1, difficulty);
            MatchStarted = true;
        }

        private void Update()
        {
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

        public void IssueMove(int entityId, LeptonPos target)
        {
            _pendingOrders.Add(new Order(OrderType.Move, LocalPlayerId, Game.CurrentTick + 1,
                entityId, targetPos: target));
        }

        public void IssueStop(int entityId)
        {
            _pendingOrders.Add(new Order(OrderType.Stop, LocalPlayerId, Game.CurrentTick + 1, entityId));
        }

        public void IssueAttack(int entityId, int targetEntityId)
        {
            _pendingOrders.Add(new Order(OrderType.Attack, LocalPlayerId, Game.CurrentTick + 1,
                entityId, targetEntityId));
        }

        public void IssueAttackMove(int entityId, LeptonPos target)
        {
            _pendingOrders.Add(new Order(OrderType.AttackMove, LocalPlayerId, Game.CurrentTick + 1,
                entityId, targetPos: target));
        }

        public void IssueSuperweapon(int superweaponIndex, LeptonPos target)
        {
            _pendingOrders.Add(new Order(OrderType.UseSuperweapon, LocalPlayerId,
                Game.CurrentTick + 1, data: superweaponIndex, targetPos: target));
        }

        public void IssueDeploy(int entityId)
        {
            _pendingOrders.Add(new Order(OrderType.Deploy, LocalPlayerId, Game.CurrentTick + 1, entityId));
        }

        public void IssueSell(int entityId)
        {
            _pendingOrders.Add(new Order(OrderType.Sell, LocalPlayerId, Game.CurrentTick + 1, entityId));
        }

        public void IssueBuildStart(int specIndex)
        {
            _pendingOrders.Add(new Order(OrderType.BuildStart, LocalPlayerId, Game.CurrentTick + 1, data: specIndex));
        }

        public void IssueBuildCancel(int specIndex)
        {
            _pendingOrders.Add(new Order(OrderType.BuildCancel, LocalPlayerId, Game.CurrentTick + 1, data: specIndex));
        }

        public void IssuePlaceStructure(int specIndex, LeptonPos origin)
        {
            _pendingOrders.Add(new Order(OrderType.PlaceStructure, LocalPlayerId, Game.CurrentTick + 1,
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
