using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.Pathfinding;
using TiberiumDusk.Sim.Systems;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim
{
    /// <summary>
    /// The deterministic simulation entry point. The host calls <see cref="Tick"/>
    /// exactly <see cref="TicksPerSecond"/> times per game second with the orders
    /// scheduled for that tick. No other mutation path exists.
    /// </summary>
    public sealed class Game
    {
        public const int TicksPerSecond = 15;
        /// <summary>Move orders to the same destination from this many units share one flow field.</summary>
        public const int FlowFieldGroupThreshold = 3;

        public int CurrentTick { get; private set; }
        public DeterministicRandom Random { get; }
        public World World { get; }

        private readonly MovementSystem _movement;

        public Game(RulesData rules, MapData map, ulong seed)
        {
            Random = new DeterministicRandom(seed);
            World = new World(map, rules);
            _movement = new MovementSystem(World);
        }

        /// <summary>Direct spawn for scenario setup and tests; production systems arrive in Phase 3.</summary>
        public Entity Spawn(string specId, int owner, CellPos cell) =>
            World.Spawn(World.Rules.Unit(specId), owner, cell);

        public void Tick(IReadOnlyList<Order> orders)
        {
            ExecuteOrders(orders);
            _movement.Tick();
            CurrentTick++;
        }

        private void ExecuteOrders(IReadOnlyList<Order> orders)
        {
            // Group Move orders by destination so multi-unit moves share a flow field.
            Dictionary<long, List<Entity>> moveGroups = null;

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                switch (order.Type)
                {
                    case OrderType.Move:
                    {
                        var entity = World.GetEntity(order.EntityId);
                        if (entity == null || entity.Owner != order.PlayerId || entity.Spec.Mobile == null) break;
                        var targetCell = order.TargetPos.ToCell();
                        if (!World.Map.InBounds(targetCell)) break;

                        moveGroups ??= new Dictionary<long, List<Entity>>();
                        long key = ((long)World.Map.CellIndex(targetCell) << 3) | (int)entity.Spec.Mobile.Locomotor;
                        if (!moveGroups.TryGetValue(key, out var group))
                        {
                            group = new List<Entity>();
                            moveGroups.Add(key, group);
                        }
                        group.Add(entity);
                        break;
                    }
                    case OrderType.Stop:
                    {
                        var entity = World.GetEntity(order.EntityId);
                        if (entity != null && entity.Owner == order.PlayerId) _movement.OrderStop(entity);
                        break;
                    }
                }
            }

            if (moveGroups != null)
            {
                // Deterministic dispatch order: by group key.
                var keys = new List<long>(moveGroups.Keys);
                keys.Sort();
                foreach (var key in keys)
                {
                    var group = moveGroups[key];
                    var locomotor = (LocomotorId)(int)(key & 0x7);
                    int cellIndex = (int)(key >> 3);
                    var target = new CellPos(cellIndex % World.Map.Width, cellIndex / World.Map.Width);

                    if (group.Count >= FlowFieldGroupThreshold)
                    {
                        var flow = FlowField.Compute(World.Map, World.Rules, locomotor, target);
                        for (int i = 0; i < group.Count; i++) _movement.OrderMoveFlow(group[i], flow);
                    }
                    else
                    {
                        for (int i = 0; i < group.Count; i++) _movement.OrderMovePath(group[i], target);
                    }
                }
            }
        }

        /// <summary>State fingerprint for desync detection and determinism tests.</summary>
        public ulong ComputeHash()
        {
            var hash = StateHash.Create();
            hash.Add(CurrentTick);
            hash.Add(Random.State);
            World.AddToHash(ref hash);
            return hash.Value;
        }
    }
}
