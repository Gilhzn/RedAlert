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

        public event System.Action AfterTick;

        private readonly List<Order> _pendingOrders = new List<Order>();
        private float _accumulator;

        public TerrainView Terrain { get; private set; }

        private void Awake()
        {
            var rules = RulesCompiler.CompileFromDirectory(ResolveDataDirectory());
            var map = DemoMap.Build(rules);
            Game = new Game(rules, map, seed: 20260719UL);
            DemoMap.SpawnUnits(Game);

            Terrain = TerrainView.Build(map, rules, transform);
        }

        private void Update()
        {
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
