using System.Collections.Generic;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.WorldModel;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class MovementTests
    {
        private static readonly List<Order> NoOrders = new List<Order>();

        private static Game NewGame(Map.MapData map = null, ulong seed = 42) =>
            new Game(TestWorlds.Rules, map ?? TestWorlds.Flat(), seed);

        private static Order MoveOrder(Entity entity, CellPos target) =>
            new Order(OrderType.Move, entity.Owner, 0, entity.Id,
                targetPos: LeptonPos.CellCenter(target));

        private static void RunTicks(Game game, int ticks)
        {
            for (int i = 0; i < ticks; i++) game.Tick(NoOrders);
        }

        [Fact]
        public void UnitDrivesToOrderedCellAndStops()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", owner: 0, new CellPos(5, 5));
            var target = new CellPos(20, 5);

            game.Tick(new List<Order> { MoveOrder(tank, target) });
            RunTicks(game, 900);

            Assert.Equal(target, tank.HomeCell);
            Assert.Equal(LeptonPos.CellCenter(target), tank.Pos);
            Assert.Equal(MoveMode.None, tank.Move.Mode);
            Assert.Equal(tank.Id, game.World.OccupantOf(target));
        }

        [Fact]
        public void FastUnitArrivesFasterThanSlowUnit()
        {
            var game = NewGame();
            var buggy = game.Spawn("so_scout_buggy", 0, new CellPos(5, 10));   // speed 10
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 20));     // speed 4

            game.Tick(new List<Order>
            {
                MoveOrder(buggy, new CellPos(40, 10)),
                MoveOrder(tank, new CellPos(40, 20)),
            });

            int buggyArrival = -1, tankArrival = -1;
            for (int t = 0; t < 2000 && (buggyArrival < 0 || tankArrival < 0); t++)
            {
                game.Tick(NoOrders);
                if (buggyArrival < 0 && buggy.Move.Mode == MoveMode.None) buggyArrival = t;
                if (tankArrival < 0 && tank.Move.Mode == MoveMode.None) tankArrival = t;
            }

            Assert.True(buggyArrival > 0 && tankArrival > 0, "both units must arrive");
            Assert.True(buggyArrival < tankArrival,
                $"buggy ({buggyArrival}) should beat tank ({tankArrival})");
        }

        [Fact]
        public void UnitFacesItsMovementDirection()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 5));
            game.Tick(new List<Order> { MoveOrder(tank, new CellPos(20, 5)) });
            RunTicks(game, 40);
            // Moving in +X: facing 0 (within rotation tolerance).
            int diff = Facing.Difference(tank.FacingValue, 0);
            Assert.InRange(System.Math.Abs(diff), 0, 4);
        }

        [Fact]
        public void TwoUnitsNeverShareACell()
        {
            var game = NewGame();
            var a = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 5));
            var b = game.Spawn("dm_mbt_walker", 0, new CellPos(25, 5));

            // Order them through each other.
            game.Tick(new List<Order>
            {
                MoveOrder(a, new CellPos(25, 6)),
                MoveOrder(b, new CellPos(5, 6)),
            });

            for (int t = 0; t < 1200; t++)
            {
                game.Tick(NoOrders);
                Assert.NotEqual(a.HomeCell, b.HomeCell);
            }
            Assert.Equal(MoveMode.None, a.Move.Mode);
            Assert.Equal(MoveMode.None, b.Move.Mode);
        }

        [Fact]
        public void MoveToOccupiedCellStopsAdjacent()
        {
            var game = NewGame();
            var blocker = game.Spawn("dm_mbt_walker", 0, new CellPos(20, 20));
            var mover = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 20));

            game.Tick(new List<Order> { MoveOrder(mover, new CellPos(20, 20)) });
            RunTicks(game, 1200);

            Assert.Equal(MoveMode.None, mover.Move.Mode);
            Assert.Equal(new CellPos(20, 20), blocker.HomeCell);
            Assert.InRange(mover.HomeCell.ChebyshevDistance(new CellPos(20, 20)), 1, 3);
        }

        [Fact]
        public void GroupMoveUsesFlowFieldAndEveryoneArrivesNearTarget()
        {
            var game = NewGame();
            var units = new List<Entity>();
            for (int i = 0; i < 5; i++)
            {
                units.Add(game.Spawn("dm_mbt_walker", 0, new CellPos(5, 10 + i * 2)));
            }
            var target = new CellPos(40, 14);

            var orders = new List<Order>();
            foreach (var u in units) orders.Add(MoveOrder(u, target));
            game.Tick(orders);

            // Threshold is 3+, so this group must be in Flow mode.
            Assert.All(units, u => Assert.Equal(MoveMode.Flow, u.Move.Mode));

            RunTicks(game, 2500);

            foreach (var u in units)
            {
                Assert.Equal(MoveMode.None, u.Move.Mode);
                Assert.InRange(u.HomeCell.ChebyshevDistance(target), 0, 3);
            }
        }

        [Fact]
        public void StopOrderHaltsUnitImmediately()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 5));
            game.Tick(new List<Order> { MoveOrder(tank, new CellPos(40, 5)) });
            RunTicks(game, 60);
            var posAtStop = tank.Pos;

            game.Tick(new List<Order> { new Order(OrderType.Stop, 0, 0, tank.Id) });
            RunTicks(game, 30);

            Assert.Equal(MoveMode.None, tank.Move.Mode);
            // Allowed to finish settling into the claimed cell, not to keep traveling.
            Assert.True(tank.Pos.Distance(posAtStop) <= 256, "unit kept moving after Stop");
        }

        [Fact]
        public void UnitCannotBeMovedByAnotherPlayer()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", owner: 0, new CellPos(5, 5));
            var order = new Order(OrderType.Move, playerId: 1, 0, tank.Id,
                targetPos: LeptonPos.CellCenter(new CellPos(20, 5)));
            game.Tick(new List<Order> { order });
            Assert.Equal(MoveMode.None, tank.Move.Mode);
        }
    }
}
