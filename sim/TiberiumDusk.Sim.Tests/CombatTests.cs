using System.Collections.Generic;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.WorldModel;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class CombatTests
    {
        private static readonly List<Order> NoOrders = new List<Order>();

        private static Game NewGame(ulong seed = 5) => new Game(TestWorlds.Rules, TestWorlds.Flat(), seed);

        private static void RunTicks(Game game, int ticks)
        {
            for (int i = 0; i < ticks; i++) game.Tick(NoOrders);
        }

        private static Order AttackOrder(Entity attacker, Entity target) =>
            new Order(OrderType.Attack, attacker.Owner, 0, attacker.Id, target.Id);

        [Fact]
        public void TankKillsInfantryOnAttackOrder()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(10, 10));
            var victim = game.Spawn("dm_rifle", 1, new CellPos(14, 10));
            victim.AutoEngage = false;

            game.Tick(new List<Order> { AttackOrder(tank, victim) });
            RunTicks(game, 800);

            Assert.Null(game.World.GetEntity(victim.Id));
        }

        [Fact]
        public void VersesTableMakesRiflesWeakAgainstHeavyArmor()
        {
            // Rifle (SA warhead, 25% vs heavy) vs tank hull should take far
            // longer than the reverse fight. Simulate both duels separately.
            int TicksToKill(string attackerSpec, string victimSpec)
            {
                var game = NewGame();
                var attacker = game.Spawn(attackerSpec, 0, new CellPos(10, 10));
                var victim = game.Spawn(victimSpec, 1, new CellPos(13, 10));
                victim.AutoEngage = false;
                game.Tick(new List<Order> { AttackOrder(attacker, victim) });
                for (int t = 0; t < 6000; t++)
                {
                    game.Tick(NoOrders);
                    if (game.World.GetEntity(victim.Id) == null) return t;
                }
                return int.MaxValue;
            }

            int rifleVsTank = TicksToKill("dm_rifle", "dm_mbt_walker");
            int tankVsRifle = TicksToKill("dm_mbt_walker", "dm_rifle");
            Assert.True(tankVsRifle < rifleVsTank / 3,
                $"armor classes should matter (tank kills rifle in {tankVsRifle}, rifle kills tank in {rifleVsTank})");
        }

        [Fact]
        public void OutOfRangeAttackerChasesTarget()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 10));
            var victim = game.Spawn("dm_rifle", 1, new CellPos(40, 10));
            victim.AutoEngage = false;

            game.Tick(new List<Order> { AttackOrder(tank, victim) });
            RunTicks(game, 2500);

            Assert.Null(game.World.GetEntity(victim.Id));
        }

        [Fact]
        public void GuardingUnitAutoAcquiresEnemyInSight()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(10, 10));   // sight 6
            var intruder = game.Spawn("dm_rifle", 1, new CellPos(14, 10));
            intruder.AutoEngage = false;

            RunTicks(game, 600);   // no explicit orders at all

            Assert.Null(game.World.GetEntity(intruder.Id));
        }

        [Fact]
        public void GuardTowerDefendsItself()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_guard_tower"), 0, new CellPos(10, 10));
            var attacker = game.Spawn("dm_rifle", 1, new CellPos(13, 10));
            attacker.AutoEngage = false;

            RunTicks(game, 900);

            Assert.Null(game.World.GetEntity(attacker.Id));
        }

        [Fact]
        public void StructuresCanBeDestroyed()
        {
            var game = NewGame();
            var plant = game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 1, new CellPos(12, 10));
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(8, 10));

            game.Tick(new List<Order> { AttackOrder(tank, plant) });
            RunTicks(game, 3000);

            Assert.Null(game.World.GetEntity(plant.Id));
            Assert.Equal(-1, game.World.OccupantOf(new CellPos(12, 10)));
        }

        [Fact]
        public void AttackMoveEngagesThenContinues()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 10));
            var blocker = game.Spawn("dm_rifle", 1, new CellPos(20, 10));
            blocker.AutoEngage = false;
            var dest = new CellPos(40, 10);

            game.Tick(new List<Order>
            {
                new Order(OrderType.AttackMove, 0, 0, tank.Id, targetPos: LeptonPos.CellCenter(dest))
            });
            RunTicks(game, 4000);

            Assert.Null(game.World.GetEntity(blocker.Id));          // engaged on the way
            Assert.Equal(dest, tank.HomeCell);                      // then continued
        }

        [Fact]
        public void EmpDisablesVehiclesButNotInfantry()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 1, new CellPos(12, 10));
            var soldier = game.Spawn("dm_rifle", 1, new CellPos(13, 10));
            tank.AutoEngage = false;
            soldier.AutoEngage = false;

            var weapon = System.Array.Find(TestWorlds.Rules.Weapons, w => w.Id == "test_emp");
            game.Combat.ApplyDamage(weapon, attackerId: -1, directTargetId: tank.Id, tank.Pos);

            Assert.True(tank.DisabledTicks > 0, "vehicle must be EMP-disabled");
            Assert.Equal(0, soldier.DisabledTicks);
            Assert.Equal(tank.Spec.Health.Max, tank.Hp);   // EMP does no damage

            // Disabled tank ignores move orders until it recovers.
            game.Tick(new List<Order>
            {
                new Order(OrderType.Move, 1, 0, tank.Id, targetPos: LeptonPos.CellCenter(new CellPos(30, 10)))
            });
            RunTicks(game, 30);
            Assert.Equal(new CellPos(12, 10), tank.HomeCell);

            RunTicks(game, 400);   // recovery
            Assert.Equal(0, tank.DisabledTicks);
        }

        [Fact]
        public void EngineerCapturesEnemyStructure()
        {
            var game = NewGame();
            var plant = game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 1, new CellPos(20, 10));
            plant.Hp = 300;   // damaged
            var engineer = game.Spawn("dm_engineer", 0, new CellPos(10, 10));

            game.Tick(new List<Order> { new Order(OrderType.Attack, 0, 0, engineer.Id, plant.Id) });
            RunTicks(game, 1500);

            Assert.Equal(0, plant.Owner);                       // captured
            Assert.Equal(plant.Spec.Health.Max, plant.Hp);      // restored
            Assert.Null(game.World.GetEntity(engineer.Id));     // consumed
        }

        [Fact]
        public void CrusherRollsOverEnemyInfantryInItsPath()
        {
            var game = NewGame();
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(5, 10));
            // Fence of enemy infantry directly on the path.
            var i1 = game.Spawn("dm_rifle", 1, new CellPos(10, 10));
            i1.AutoEngage = false;
            tank.AutoEngage = false;

            game.Tick(new List<Order>
            {
                new Order(OrderType.Move, 0, 0, tank.Id, targetPos: LeptonPos.CellCenter(new CellPos(15, 10)))
            });
            RunTicks(game, 800);

            Assert.Equal(new CellPos(15, 10), tank.HomeCell);
        }

        [Fact]
        public void VeterancyRankIncreasesAfterEnoughKills()
        {
            var game = NewGame();
            var buggy = game.Spawn("so_scout_buggy", 0, new CellPos(10, 10));   // cost 500 → rank at 5000 value
            buggy.CombatXp = 4900;

            var victim = game.Spawn("dm_rifle", 1, new CellPos(13, 10));   // cost 120
            victim.AutoEngage = false;
            game.Tick(new List<Order> { AttackOrder(buggy, victim) });
            RunTicks(game, 400);

            Assert.Null(game.World.GetEntity(victim.Id));
            Assert.Equal(1, buggy.Rank);
        }

        [Fact]
        public void CombatScenarioIsDeterministic()
        {
            ulong Run()
            {
                var game = NewGame(1717);
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_guard_tower"), 0, new CellPos(20, 20));
                for (int i = 0; i < 4; i++)
                {
                    game.Spawn("dm_mbt_walker", 0, new CellPos(10 + i * 2, 10));
                    game.Spawn("so_scout_buggy", 1, new CellPos(10 + i * 2, 30));
                    game.Spawn("dm_rifle", 1, new CellPos(11 + i * 2, 31));
                }
                var orders = new List<Order>();
                foreach (var e in game.World.Entities)
                {
                    if (e.Spec.Mobile != null)
                    {
                        orders.Add(new Order(OrderType.AttackMove, e.Owner, 0, e.Id,
                            targetPos: LeptonPos.CellCenter(new CellPos(20, e.Owner == 0 ? 32 : 8))));
                    }
                }
                game.Tick(orders);
                RunTicks(game, 3000);
                return game.ComputeHash();
            }

            Assert.Equal(Run(), Run());
        }
    }
}
