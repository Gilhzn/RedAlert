using System.Collections.Generic;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;

namespace TiberiumDusk.Sim
{
    /// <summary>
    /// The deterministic simulation entry point. Fixed-tick: the host calls
    /// <see cref="Tick"/> exactly <see cref="TicksPerSecond"/> times per game
    /// second, passing the orders scheduled for that tick. No other mutation
    /// path exists. (World model, systems and entities arrive in Phase 1 —
    /// this skeleton pins down the contract and the determinism guarantees.)
    /// </summary>
    public sealed class Game
    {
        public const int TicksPerSecond = 15;

        public int CurrentTick { get; private set; }
        public DeterministicRandom Random { get; }

        private readonly List<Order> _executedOrders = new List<Order>();

        public Game(ulong seed)
        {
            Random = new DeterministicRandom(seed);
        }

        public void Tick(IReadOnlyList<Order> orders)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                Execute(orders[i]);
            }
            CurrentTick++;
        }

        private void Execute(in Order order)
        {
            // Phase 1+: dispatch to systems. For now, record for hash coverage
            // so the order pipeline itself is under determinism tests.
            _executedOrders.Add(order);
        }

        /// <summary>State fingerprint for desync detection and determinism tests.</summary>
        public ulong ComputeHash()
        {
            var hash = StateHash.Create();
            hash.Add(CurrentTick);
            hash.Add(Random.State);
            hash.Add(_executedOrders.Count);
            for (int i = 0; i < _executedOrders.Count; i++)
            {
                var o = _executedOrders[i];
                hash.Add((int)o.Type);
                hash.Add(o.PlayerId);
                hash.Add(o.ExecuteTick);
                hash.Add(o.EntityId);
                hash.Add(o.TargetEntityId);
                hash.Add(o.TargetPos);
                hash.Add(o.Data);
            }
            return hash.Value;
        }
    }
}
