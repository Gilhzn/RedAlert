using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.Systems;
using TiberiumDusk.Sim.WorldModel;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class EconomyTests
    {
        private static readonly List<Order> NoOrders = new List<Order>();

        private static Game NewGame(ulong seed = 7) => new Game(TestWorlds.Rules, TestWorlds.Flat(), seed);

        private static void RunTicks(Game game, int ticks)
        {
            for (int i = 0; i < ticks; i++) game.Tick(NoOrders);
        }

        private static Order BuildOrder(int player, string specId) =>
            new Order(OrderType.BuildStart, player, 0, data: TestWorlds.Rules.UnitIndex(specId));

        private static Order PlaceOrder(int player, string specId, CellPos origin) =>
            new Order(OrderType.PlaceStructure, player, 0, data: TestWorlds.Rules.UnitIndex(specId),
                targetPos: new LeptonPos(origin.X * 256 + 1, origin.Y * 256 + 1));

        // ---------- Harvesting ----------

        [Fact]
        public void HarvesterCollectsAndUnloadsCredits()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_refinery"), 0, new CellPos(10, 5));
            var harvester = game.Spawn("dm_harvester", 0, new CellPos(12, 10));

            // A small green field nearby.
            for (int x = 16; x <= 19; x++)
                for (int y = 8; y <= 11; y++)
                    game.World.Crystal.Set(new CellPos(x, y), CrystalType.Green, 5);

            int before = game.World.Players[0].Credits;
            RunTicks(game, 3000);

            Assert.True(game.World.Players[0].Credits > before,
                $"harvester produced no income (credits {game.World.Players[0].Credits}, phase {harvester.Harvest.Phase})");
        }

        [Fact]
        public void HarvestedIncomeMatchesBailValues()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_refinery"), 0, new CellPos(10, 5));
            game.Spawn("dm_harvester", 0, new CellPos(12, 10));
            // Exactly 4 green bails on one cell, nothing else on the map.
            game.World.Crystal.Set(new CellPos(14, 10), CrystalType.Green, 4);

            int before = game.World.Players[0].Credits;
            RunTicks(game, 2500);

            int gained = game.World.Players[0].Credits - before;
            Assert.Equal(4 * TestWorlds.Rules.Economy.GreenBailValue, gained);
        }

        [Fact]
        public void StorageCapCapsRefinedIncome()
        {
            var game = NewGame();
            var player = game.World.Players[0];
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_refinery"), 0, new CellPos(10, 5));
            player.Credits = player.StorageCapacityCredits;   // already full

            game.Spawn("dm_harvester", 0, new CellPos(12, 10));
            game.World.Crystal.Set(new CellPos(14, 10), CrystalType.Green, 6);

            RunTicks(game, 2500);
            Assert.Equal(player.StorageCapacityCredits, player.Credits);   // overflow lost
        }

        // ---------- Production ----------

        [Fact]
        public void BuildsPowerPlantAndPlacesIt()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
            int before = game.World.Players[0].Credits;

            game.Tick(new List<Order> { BuildOrder(0, "dm_power_plant") });

            var queue = game.Production.GetQueue(0, ProductionQueue.Structure);
            int cost = TestWorlds.Rules.Unit("dm_power_plant").Buildable.Cost;
            int expectedTicks = cost * TestWorlds.Rules.Economy.BuildTicksPerThousandCost / 1000;
            RunTicks(game, expectedTicks + 10);

            Assert.True(queue.ReadyForPlacement, "power plant should be ready");
            Assert.Equal(before - cost, game.World.Players[0].Credits);

            game.Tick(new List<Order> { PlaceOrder(0, "dm_power_plant", new CellPos(9, 5)) });
            Assert.True(game.World.OwnsBlueprint(0, "dm_power_plant"));
            Assert.True(game.World.Players[0].PowerProduced >= 100);
        }

        [Fact]
        public void PlacementOutsideBaseRadiusRejected()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
            game.Tick(new List<Order> { BuildOrder(0, "dm_power_plant") });
            RunTicks(game, 300);
            Assert.True(game.Production.GetQueue(0, ProductionQueue.Structure).ReadyForPlacement);

            // Far across the map — outside adjacency.
            game.Tick(new List<Order> { PlaceOrder(0, "dm_power_plant", new CellPos(40, 40)) });
            Assert.False(game.World.OwnsBlueprint(0, "dm_power_plant"));
            // Still ready — the player can pick a valid spot.
            Assert.True(game.Production.GetQueue(0, ProductionQueue.Structure).ReadyForPlacement);

            game.Tick(new List<Order> { PlaceOrder(0, "dm_power_plant", new CellPos(9, 5)) });
            Assert.True(game.World.OwnsBlueprint(0, "dm_power_plant"));
        }

        [Fact]
        public void PrerequisitesGateProduction()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
            // Refinery requires power plant — must be rejected.
            game.Tick(new List<Order> { BuildOrder(0, "dm_refinery") });
            Assert.Equal(-1, game.Production.GetQueue(0, ProductionQueue.Structure).ActiveSpecIndex);
        }

        [Fact]
        public void LowPowerSlowsConstruction()
        {
            int TicksToReady(bool withPower)
            {
                var game = NewGame();
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
                if (withPower)
                    game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(9, 5));
                // A drain structure to force deficit when no plant exists.
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_barracks"), 0, new CellPos(5, 9));

                game.Tick(new List<Order> { BuildOrder(0, "dm_silo") });
                // Silo requires refinery... use barracks-buildable instead: build power plant.
                game.Tick(new List<Order> { BuildOrder(0, "dm_power_plant") });

                var queue = game.Production.GetQueue(0, ProductionQueue.Structure);
                for (int t = 0; t < 5000; t++)
                {
                    game.Tick(NoOrders);
                    if (queue.ReadyForPlacement) return t;
                }
                return -1;
            }

            int fast = TicksToReady(withPower: true);
            int slow = TicksToReady(withPower: false);
            Assert.True(fast > 0 && slow > 0, $"builds must finish (fast={fast}, slow={slow})");
            Assert.True(slow > fast * 3 / 2, $"low power must slow builds (fast={fast}, slow={slow})");
        }

        [Fact]
        public void CancelRefundsSpentCredits()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
            int before = game.World.Players[0].Credits;

            game.Tick(new List<Order> { BuildOrder(0, "dm_power_plant") });
            RunTicks(game, 50);
            Assert.True(game.World.Players[0].Credits < before, "progressive payment should have started");

            game.Tick(new List<Order>
            {
                new Order(OrderType.BuildCancel, 0, 0, data: TestWorlds.Rules.UnitIndex("dm_power_plant"))
            });
            Assert.Equal(before, game.World.Players[0].Credits);
        }

        [Fact]
        public void FactoryProducesUnitAtExit()
        {
            var game = NewGame();
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(9, 5));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_refinery"), 0, new CellPos(5, 9));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_barracks"), 0, new CellPos(9, 8));
            game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_factory"), 0, new CellPos(12, 5));

            int unitsBefore = CountMobiles(game);
            game.Tick(new List<Order> { BuildOrder(0, "so_scout_buggy") });
            RunTicks(game, 500);

            Assert.Equal(unitsBefore + 1, CountMobiles(game));
        }

        [Fact]
        public void McvDeploysIntoConstructionYard()
        {
            var game = NewGame();
            var mcv = game.Spawn("dm_mcv", 0, new CellPos(20, 20));
            game.Tick(new List<Order> { new Order(OrderType.Deploy, 0, 0, mcv.Id) });

            Assert.Null(game.World.GetEntity(mcv.Id));
            Assert.True(game.World.OwnsBlueprint(0, "dm_conyard"));
        }

        [Fact]
        public void McvCannotDeployWhenFootprintOverlapsWater()
        {
            // Water channel at x=30..33; MCV at (29,10) → 3x3 footprint origin
            // (28,9) spans x28..30 and hits the water column.
            var game = new Game(TestWorlds.Rules, TestWorlds.WaterChannel(), 7);
            var mcv = game.Spawn("dm_mcv", 0, new CellPos(29, 10));

            game.Tick(new List<Order> { new Order(OrderType.Deploy, 0, 0, mcv.Id) });

            Assert.NotNull(game.World.GetEntity(mcv.Id));   // deploy refused, MCV intact
            Assert.False(game.World.OwnsBlueprint(0, "dm_conyard"));
            Assert.Equal(mcv.Id, game.World.OccupantOf(new CellPos(29, 10)));   // cell claim restored
        }

        [Fact]
        public void SellRefundsHalfAndFreesCells()
        {
            var game = NewGame();
            var plant = game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(9, 5));
            int before = game.World.Players[0].Credits;

            game.Tick(new List<Order> { new Order(OrderType.Sell, 0, 0, plant.Id) });

            int cost = TestWorlds.Rules.Unit("dm_power_plant").Buildable.Cost;
            Assert.Equal(before + cost / 2, game.World.Players[0].Credits);
            Assert.Equal(-1, game.World.OccupantOf(new CellPos(9, 5)));
            Assert.Null(game.World.GetEntity(plant.Id));
        }

        // ---------- Crystal life ----------

        [Fact]
        public void CrystalGrowsAndSpreadsOverTime()
        {
            var game = NewGame();
            game.World.Crystal.Set(new CellPos(30, 30), CrystalType.Green, 8);

            int CountCells()
            {
                int count = 0;
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                        if (game.World.Crystal.HasCrystal(new CellPos(x, y))) count++;
                return count;
            }

            int before = CountCells();
            RunTicks(game, TestWorlds.Rules.Economy.GrowthIntervalTicks * 6 + 10);
            Assert.True(CountCells() > before, "crystal should spread to neighboring cells");
        }

        [Fact]
        public void CrystalHurtsInfantryButNotVehicles()
        {
            var game = NewGame();
            var soldier = game.Spawn("dm_rifle_infantry", 0, new CellPos(20, 20));
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(25, 20));
            game.World.Crystal.Set(new CellPos(20, 20), CrystalType.Green, 11);
            game.World.Crystal.Set(new CellPos(25, 20), CrystalType.Green, 11);

            RunTicks(game, 200);

            Assert.True(soldier.Hp < soldier.Spec.Health.Max, "infantry must take crystal damage");
            Assert.Equal(tank.Spec.Health.Max, tank.Hp);
        }

        // ---------- Determinism ----------

        [Fact]
        public void EconomyScenarioIsDeterministic()
        {
            ulong Run()
            {
                var game = NewGame(99);
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_conyard"), 0, new CellPos(5, 5));
                game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_refinery"), 0, new CellPos(10, 5));
                game.Spawn("dm_harvester", 0, new CellPos(12, 10));
                for (int x = 16; x <= 20; x++)
                    for (int y = 8; y <= 12; y++)
                        game.World.Crystal.Set(new CellPos(x, y), CrystalType.Green, 7);

                game.Tick(new List<Order> { BuildOrder(0, "dm_power_plant") });
                RunTicks(game, 2600);
                if (game.Production.GetQueue(0, ProductionQueue.Structure).ReadyForPlacement)
                    game.Tick(new List<Order> { PlaceOrder(0, "dm_power_plant", new CellPos(5, 9)) });
                RunTicks(game, 1200);
                return game.ComputeHash();
            }

            Assert.Equal(Run(), Run());
        }

        private static int CountMobiles(Game game)
        {
            int count = 0;
            foreach (var e in game.World.Entities)
                if (e.Alive && e.Spec.Mobile != null) count++;
            return count;
        }
    }
}
