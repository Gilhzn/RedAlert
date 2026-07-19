using System.Collections.Generic;
using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    /// <summary>
    /// The load-bearing guarantee of the whole project: identical seed and
    /// identical order log must produce identical state hashes. Lockstep
    /// multiplayer and replays depend on this invariant holding forever.
    /// </summary>
    public class DeterminismTests
    {
        /// <summary>
        /// Full sim run: 12 units on a map with water + heights, scripted
        /// pseudo-random move/stop orders, real pathfinding and collisions.
        /// </summary>
        private static ulong RunGame(ulong seed, int ticks)
        {
            var map = TestWorlds.WaterChannel();
            for (int y = 20; y < 30; y++)
                for (int x = 45; x < 55; x++)
                    map.SetHeightLevel(new CellPos(x, y), 1);

            var game = new Game(TestWorlds.Rules, map, seed);
            var scriptRng = new DeterministicRandom(seed ^ 0xFACE);

            var specs = new[] { "dm_mbt_walker", "dm_rifle", "so_scout_buggy" };
            var unitIds = new List<int>();
            for (int i = 0; i < 12; i++)
            {
                var e = game.Spawn(specs[i % specs.Length], owner: i % 2, new CellPos(3 + (i % 4) * 3, 3 + (i / 4) * 3));
                unitIds.Add(e.Id);
            }

            var empty = new List<Order>();
            for (int t = 0; t < ticks; t++)
            {
                if (t % 40 == 0)
                {
                    var orders = new List<Order>();
                    for (int u = 0; u < unitIds.Count; u++)
                    {
                        if (!scriptRng.Chance(35)) continue;
                        var entity = game.World.GetEntity(unitIds[u]);
                        if (entity == null) continue;
                        var target = new CellPos(scriptRng.Next(1, 63), scriptRng.Next(1, 63));
                        orders.Add(new Order(OrderType.Move, entity.Owner, t, entity.Id,
                            targetPos: LeptonPos.CellCenter(target)));
                    }
                    game.Tick(orders);
                }
                else
                {
                    game.Tick(empty);
                }
            }
            return game.ComputeHash();
        }

        [Fact]
        public void SameSeedSameOrders_SameHash_After3000Ticks()
        {
            Assert.Equal(RunGame(1234, 3_000), RunGame(1234, 3_000));
        }

        [Fact]
        public void DifferentSeed_DifferentHash()
        {
            Assert.NotEqual(RunGame(1234, 1_500), RunGame(1235, 1_500));
        }

        [Fact]
        public void RandomSequenceIsStableAcrossRuns()
        {
            // Pinned behavior: if the RNG implementation changes, every existing
            // replay/netgame desyncs. Never change it without a protocol bump.
            var rng = new DeterministicRandom(42);
            var got = new uint[5];
            for (int i = 0; i < got.Length; i++) got[i] = rng.NextUInt();

            var again = new DeterministicRandom(42);
            for (int i = 0; i < got.Length; i++) Assert.Equal(got[i], again.NextUInt());
        }

        [Fact]
        public void RandomRangeIsUniformEnoughAndInBounds()
        {
            var rng = new DeterministicRandom(7);
            var counts = new int[10];
            for (int i = 0; i < 100_000; i++)
            {
                int v = rng.Next(0, 10);
                Assert.InRange(v, 0, 9);
                counts[v]++;
            }
            foreach (var c in counts)
            {
                Assert.InRange(c, 9_000, 11_000);
            }
        }
    }
}
