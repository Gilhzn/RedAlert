using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using TiberiumDusk.Sim.Systems;
using TiberiumDusk.Sim.WorldModel;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class SkirmishTests
    {
        private static readonly List<Order> NoOrders = new List<Order>();

        private static void RunTicks(Game game, int ticks)
        {
            for (int i = 0; i < ticks; i++) game.Tick(NoOrders);
        }

        /// <summary>Symmetric skirmish start: MCV + escort + crystal field per player.</summary>
        private static Game NewSkirmish(ulong seed, AIDifficulty? aiBoth = null)
        {
            var map = TestWorlds.Flat(96);
            var game = new Game(TestWorlds.Rules, map, seed);
            game.SetPlayerFaction(0, "dominion");
            game.SetPlayerFaction(1, "serpent");

            game.Spawn("nx_mcv", 0, new CellPos(12, 12));
            game.Spawn("dm_mbt_walker", 0, new CellPos(15, 12));
            game.Spawn("nx_mcv", 1, new CellPos(83, 83));
            game.Spawn("so_scout_buggy", 1, new CellPos(80, 83));

            for (int p = 0; p < 2; p++)
            {
                int cx = p == 0 ? 20 : 75;
                int cy = p == 0 ? 20 : 75;
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                        game.World.Crystal.Set(new CellPos(cx + dx, cy + dy), CrystalType.Green, 9);
            }

            if (aiBoth != null)
            {
                game.AI.Enable(0, aiBoth.Value);
                game.AI.Enable(1, aiBoth.Value);
            }
            return game;
        }

        // ---------- Vision ----------

        [Fact]
        public void ShroudStartsBlackAndScoutingRevealsIt()
        {
            var game = NewSkirmish(3);
            RunTicks(game, 10);

            Assert.True(game.Vision.IsExplored(0, new CellPos(12, 12)), "own start must be revealed");
            Assert.False(game.Vision.IsExplored(0, new CellPos(83, 83)), "enemy base must start shrouded");

            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(15, 15));
            game.Tick(new List<Order>
            {
                new Order(OrderType.Move, 0, 0, tank.Id, targetPos: LeptonPos.CellCenter(new CellPos(80, 80)))
            });
            RunTicks(game, 6000);

            Assert.True(game.Vision.IsExplored(0, new CellPos(80, 80)), "scouting must lift the shroud");
        }

        [Fact]
        public void VisibilityFadesWhenUnitsLeave()
        {
            var game = NewSkirmish(3);
            var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(40, 40));
            RunTicks(game, 10);
            Assert.True(game.Vision.IsVisible(0, new CellPos(40, 40)));

            game.Tick(new List<Order>
            {
                new Order(OrderType.Move, 0, 0, tank.Id, targetPos: LeptonPos.CellCenter(new CellPos(12, 16)))
            });
            RunTicks(game, 1500);

            Assert.False(game.Vision.IsVisible(0, new CellPos(40, 40)), "no eyes there anymore");
            Assert.True(game.Vision.IsExplored(0, new CellPos(40, 40)), "but it stays explored");
        }

        // ---------- Victory ----------

        [Fact]
        public void EliminatingAllEnemyAssetsWinsTheMatch()
        {
            var game = new Game(TestWorlds.Rules, TestWorlds.Flat(), 9);
            int winner = -1;
            game.GameEnded += w => winner = w;

            game.Spawn("dm_mbt_walker", 0, new CellPos(10, 10));
            var lastEnemy = game.Spawn("dm_rifle", 1, new CellPos(14, 10));
            lastEnemy.AutoEngage = false;

            RunTicks(game, 900);

            Assert.True(game.IsGameOver, "match should end when a side is wiped out");
            Assert.Equal(0, game.WinnerPlayerId);
            Assert.Equal(0, winner);
        }

        [Fact]
        public void MatchContinuesWhileBothSidesLive()
        {
            var game = NewSkirmish(9);
            RunTicks(game, 600);
            Assert.False(game.IsGameOver);
        }

        // ---------- AI ----------

        [Fact]
        public void AiDeploysBaseEconomyAndArmy()
        {
            var game = NewSkirmish(41);
            game.AI.Enable(1, AIDifficulty.Normal);

            RunTicks(game, 12000);   // ~13 game-minutes

            Assert.True(game.World.OwnsBlueprint(1, "nx_conyard"), "AI must deploy its MCV");
            Assert.True(game.World.OwnsBlueprint(1, "so_power_plant"), "AI must build power");
            Assert.True(game.World.OwnsBlueprint(1, "nx_refinery"), "AI must build a refinery");
            Assert.True(game.World.OwnsBlueprint(1, "so_hand"), "AI must build infantry production");
            Assert.True(game.World.OwnsBlueprint(1, "so_factory"), "AI must build a war factory");

            int combatants = 0;
            foreach (var e in game.World.Entities)
            {
                if (e.Alive && e.Owner == 1 && !e.Spec.IsStructure
                    && e.Spec.WeaponIndex >= 0 && e.Spec.Harvester == null)
                {
                    combatants++;
                }
            }
            Assert.True(combatants >= 3, $"AI should have trained an army (has {combatants})");
        }

        [Fact]
        public void AiAttacksTheHumanPlayer()
        {
            var game = NewSkirmish(43);
            game.AI.Enable(1, AIDifficulty.Hard);
            // Human-side bait: a lone structure near the middle.
            var bait = game.World.SpawnStructure(TestWorlds.Rules.Unit("dm_power_plant"), 0, new CellPos(46, 46));

            RunTicks(game, 22000);

            Assert.True(game.World.GetEntity(bait.Id) == null || bait.Hp < bait.Spec.Health.Max,
                "hard AI should have assaulted the exposed structure by now");
        }

        [Fact]
        public void AiVersusAiFightsToTheDeathOrBuildsRealBases()
        {
            var game = NewSkirmish(1010, AIDifficulty.Normal);
            int entityPeak = 0;
            for (int t = 0; t < 30000 && !game.IsGameOver; t++)
            {
                game.Tick(NoOrders);
                if ((t & 1023) == 0)
                {
                    int alive = 0;
                    foreach (var e in game.World.Entities)
                        if (e.Alive) alive++;
                    if (alive > entityPeak) entityPeak = alive;
                }
            }

            // A real match happened: bases grew into armies, and blood was spilled
            // (one side may already be rubble — that's the point of the game).
            int deaths = 0;
            foreach (var e in game.World.Entities)
                if (!e.Alive) deaths++;

            Assert.True(entityPeak >= 25, $"the match should have grown real bases/armies (peak {entityPeak})");
            Assert.True(deaths >= 5 || game.IsGameOver,
                $"the AIs should actually fight (deaths {deaths}, gameOver {game.IsGameOver})");
        }

        [Fact]
        public void AiSkirmishIsDeterministic()
        {
            ulong Run()
            {
                var game = NewSkirmish(777, AIDifficulty.Normal);
                RunTicks(game, 9000);
                return game.ComputeHash();
            }

            Assert.Equal(Run(), Run());
        }

        // ---------- Replay ----------

        [Fact]
        public void ReplayReproducesTheExactMatch()
        {
            ReplayLog recorded;
            ulong liveHash;
            {
                var game = NewSkirmish(2024);
                game.Recorder = new ReplayLog { Seed = 2024 };
                var tank = game.Spawn("dm_mbt_walker", 0, new CellPos(30, 30));
                var enemy = game.Spawn("so_tick_tank", 1, new CellPos(50, 50));

                for (int t = 0; t < 2000; t++)
                {
                    var orders = new List<Order>();
                    if (t == 10)
                    {
                        orders.Add(new Order(OrderType.Move, 0, t, tank.Id,
                            targetPos: LeptonPos.CellCenter(new CellPos(45, 45))));
                    }
                    if (t == 300)
                    {
                        orders.Add(new Order(OrderType.Attack, 0, t, tank.Id, enemy.Id));
                    }
                    game.Tick(orders);
                }
                liveHash = game.ComputeHash();
                recorded = game.Recorder;
            }

            // Serialize → deserialize → replay from scratch.
            var loaded = ReplayLog.Deserialize(recorded.Serialize());
            Assert.Equal(2024UL, loaded.Seed);

            var replayGame = NewSkirmish(loaded.Seed);
            replayGame.Spawn("dm_mbt_walker", 0, new CellPos(30, 30));
            replayGame.Spawn("so_tick_tank", 1, new CellPos(50, 50));
            for (int t = 0; t < 2000; t++)
            {
                replayGame.Tick(loaded.OrdersFor(t));
            }

            Assert.Equal(liveHash, replayGame.ComputeHash());
        }
    }
}
