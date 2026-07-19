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
        public ProductionSystem Production { get; }
        public CombatSystem Combat { get; }

        private readonly MovementSystem _movement;
        private readonly HarvesterSystem _harvesters;
        private readonly CrystalSystem _crystal;

        public Game(RulesData rules, MapData map, ulong seed)
        {
            Random = new DeterministicRandom(seed);
            World = new World(map, rules);
            _movement = new MovementSystem(World);
            Production = new ProductionSystem(World, _movement);
            _harvesters = new HarvesterSystem(World, _movement);
            _crystal = new CrystalSystem(World, Random);
            Combat = new CombatSystem(World, _movement);
        }

        /// <summary>Direct spawn for scenario setup and tests.</summary>
        public Entity Spawn(string specId, int owner, CellPos cell) =>
            World.Spawn(World.Rules.Unit(specId), owner, cell);

        public void Tick(IReadOnlyList<Order> orders)
        {
            ExecuteOrders(orders);
            Production.Tick();
            _harvesters.Tick();
            Combat.Tick();
            _movement.Tick();
            _crystal.Tick();
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
                        if (entity != null && entity.Owner == order.PlayerId)
                        {
                            _movement.OrderStop(entity);
                            Combat.ClearCombatOrders(entity);
                        }
                        break;
                    }
                    case OrderType.Attack:
                    {
                        var entity = World.GetEntity(order.EntityId);
                        var target = World.GetEntity(order.TargetEntityId);
                        if (entity != null && target != null && entity.Owner == order.PlayerId
                            && target.Owner != order.PlayerId)
                        {
                            // Engineers "attack" enemy structures by capturing them.
                            if (entity.Spec.CanCapture && target.Spec.IsStructure)
                                Combat.OrderCapture(entity, target);
                            else
                                Combat.OrderAttack(entity, target);
                        }
                        break;
                    }
                    case OrderType.AttackMove:
                    {
                        var entity = World.GetEntity(order.EntityId);
                        if (entity != null && entity.Owner == order.PlayerId)
                        {
                            var cell = order.TargetPos.ToCell();
                            if (World.Map.InBounds(cell)) Combat.OrderAttackMove(entity, cell);
                        }
                        break;
                    }
                    case OrderType.BuildStart:
                    {
                        if (order.Data >= 0 && order.Data < World.Rules.Units.Length)
                            Production.StartBuild(order.PlayerId, order.Data);
                        break;
                    }
                    case OrderType.BuildCancel:
                    {
                        if (order.Data >= 0 && order.Data < World.Rules.Units.Length)
                            Production.CancelBuild(order.PlayerId, order.Data);
                        break;
                    }
                    case OrderType.PlaceStructure:
                    {
                        if (order.Data >= 0 && order.Data < World.Rules.Units.Length)
                            Production.TryPlace(order.PlayerId, order.Data, order.TargetPos.ToCell());
                        break;
                    }
                    case OrderType.Sell:
                    {
                        var entity = World.GetEntity(order.EntityId);
                        if (entity != null && entity.Owner == order.PlayerId && entity.Spec.IsStructure)
                        {
                            var player = World.Players[order.PlayerId];
                            player.Credits += entity.Spec.Buildable != null
                                ? entity.Spec.Buildable.Cost * World.Rules.Economy.SellRefundPercent / 100
                                : 0;
                            World.Kill(entity);
                        }
                        break;
                    }
                    case OrderType.Deploy:
                    {
                        var entity = World.GetEntity(order.EntityId);
                        if (entity != null && entity.Owner == order.PlayerId && entity.Spec.DeploysInto != null)
                        {
                            TryDeploy(entity);
                        }
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

                    // A plain Move disengages combat/capture missions.
                    for (int i = 0; i < group.Count; i++) Combat.ClearCombatOrders(group[i]);

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

        /// <summary>MCV-style deploy: the unit vanishes and its structure appears centered on it.</summary>
        private void TryDeploy(Entity entity)
        {
            var spec = World.Rules.Unit(entity.Spec.DeploysInto);
            var s = spec.Structure;
            var origin = new CellPos(
                entity.HomeCell.X - s.FootprintW / 2,
                entity.HomeCell.Y - s.FootprintH / 2);

            // Vacate our own cell for the validation, restore on failure.
            World.ReleaseCell(entity.HomeCell, entity.Id);
            if (entity.Move.HasClaim) World.ReleaseCell(entity.Move.ClaimedCell, entity.Id);

            // A deploying MCV needs no base adjacency — it IS the base.
            if (PlacementValidator.CanPlace(World, entity.Owner, spec, origin, requireAdjacency: false))
            {
                World.Kill(entity);
                World.SpawnStructure(spec, entity.Owner, origin);
            }
            else
            {
                World.ClaimCell(entity.HomeCell, entity.Id);
                if (entity.Move.HasClaim) World.ClaimCell(entity.Move.ClaimedCell, entity.Id);
            }
        }

        /// <summary>State fingerprint for desync detection and determinism tests.</summary>
        public ulong ComputeHash()
        {
            var hash = StateHash.Create();
            hash.Add(CurrentTick);
            hash.Add(Random.State);
            World.AddToHash(ref hash);
            Production.AddToHash(ref hash);
            return hash.Value;
        }
    }
}
