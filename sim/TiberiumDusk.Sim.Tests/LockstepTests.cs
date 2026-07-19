using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TiberiumDusk.Net;
using TiberiumDusk.Server;
using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    /// <summary>
    /// The multiplayer milestone: two real clients, a real relay server, real
    /// WebSockets on loopback — identical simulations or bust.
    /// </summary>
    public class LockstepTests
    {
        private static int _nextPort = 18471;

        /// <summary>Both clients must build the EXACT same starting world from Start data.</summary>
        private static Game BuildNetGame(ulong seed, string[] factions)
        {
            var game = new Game(TestWorlds.Rules, TestWorlds.Flat(96), seed);
            for (int p = 0; p < factions.Length; p++)
            {
                game.SetPlayerFaction(p, factions[p]);
                int baseX = p == 0 ? 12 : 83;
                int baseY = p == 0 ? 12 : 83;
                game.Spawn("nx_mcv", p, new CellPos(baseX, baseY));
                game.Spawn(factions[p] == "serpent" ? "so_tick_tank" : "dm_mbt_walker",
                    p, new CellPos(baseX + 3, baseY));
            }
            return game;
        }

        private static async Task<(LockstepClient client, WebSocketTransport transport)> Connect(
            int port, string name, string faction)
        {
            var transport = new WebSocketTransport();
            await transport.ConnectAsync($"ws://127.0.0.1:{port}/");
            var client = new LockstepClient(transport);
            client.SendJoin(name, faction);
            return (client, transport);
        }

        private static async Task WaitFor(Func<bool> condition, int timeoutMs, string what)
        {
            int waited = 0;
            while (!condition())
            {
                if (waited > timeoutMs) throw new TimeoutException("timed out waiting for " + what);
                await Task.Delay(10);
                waited += 10;
            }
        }

        [Fact]
        public async Task TwoClientsPlayTheSameMatchOverRealSockets()
        {
            int port = _nextPort++;
            using var server = new RelayServer(port, playersToStart: 2, seed: 991199);
            server.Start();

            var (alice, aliceTransport) = await Connect(port, "alice", "dominion");
            var (bob, bobTransport) = await Connect(port, "bob", "serpent");
            using var aliceT = aliceTransport;
            using var bobT = bobTransport;

            // Both wait for Start, then build identical worlds and report ready.
            await WaitFor(() =>
            {
                alice.Pump(0);
                bob.Pump(0);
                return alice.Started && bob.Started;
            }, 5000, "match start");

            Assert.Equal(0, alice.LocalPlayerId);
            Assert.Equal(1, bob.LocalPlayerId);
            Assert.Equal(alice.Seed, bob.Seed);

            alice.AttachGame(BuildNetGame(alice.Seed, alice.Factions));
            bob.AttachGame(BuildNetGame(bob.Seed, bob.Factions));

            // Scripted play from both sides while pumping.
            bool aliceOrdered = false, bobOrdered = false;
            int waited = 0;
            // Generous budget: the full suite runs heavyweight AI tests in
            // parallel and can starve this loop of CPU.
            while ((alice.Game.CurrentTick < 600 || bob.Game.CurrentTick < 600) && waited < 120000)
            {
                alice.Pump(200);
                bob.Pump(200);

                if (!aliceOrdered && alice.Game.CurrentTick > 30)
                {
                    aliceOrdered = true;
                    // Alice moves her tank toward the middle.
                    foreach (var e in alice.Game.World.Entities)
                    {
                        if (e.Alive && e.Owner == 0 && e.Spec.Mobile != null && e.Spec.DeploysInto == null)
                        {
                            alice.Issue(new Order(OrderType.Move, 0, 0, e.Id,
                                targetPos: LeptonPos.CellCenter(new CellPos(48, 48))));
                        }
                    }
                }
                if (!bobOrdered && bob.Game.CurrentTick > 60)
                {
                    bobOrdered = true;
                    foreach (var e in bob.Game.World.Entities)
                    {
                        if (e.Alive && e.Owner == 1 && e.Spec.Mobile != null && e.Spec.DeploysInto == null)
                        {
                            bob.Issue(new Order(OrderType.AttackMove, 1, 0, e.Id,
                                targetPos: LeptonPos.CellCenter(new CellPos(48, 48))));
                        }
                    }
                }

                await Task.Delay(3);
                waited += 3;
            }

            Assert.True(alice.Game.CurrentTick >= 600, $"alice stalled at {alice.Game.CurrentTick}");
            Assert.True(bob.Game.CurrentTick >= 600, $"bob stalled at {bob.Game.CurrentTick}");

            // Let the slower one catch up to the same tick exactly.
            int target = System.Math.Max(alice.Game.CurrentTick, bob.Game.CurrentTick);
            await WaitFor(() =>
            {
                alice.Pump(target - alice.Game.CurrentTick);
                bob.Pump(target - bob.Game.CurrentTick);
                return alice.Game.CurrentTick == target && bob.Game.CurrentTick == target;
            }, 30000, "tick alignment");

            Assert.Equal(alice.Game.ComputeHash(), bob.Game.ComputeHash());
            Assert.False(alice.Desynced);
            Assert.False(bob.Desynced);
            Assert.False(server.DesyncDetected);
        }

        [Fact]
        public async Task MismatchedWorldsAreFlaggedAsDesync()
        {
            int port = _nextPort++;
            using var server = new RelayServer(port, playersToStart: 2, seed: 5);
            server.Start();

            var (alice, aliceTransport) = await Connect(port, "alice", "dominion");
            var (bob, bobTransport) = await Connect(port, "bob", "serpent");
            using var aliceT = aliceTransport;
            using var bobT = bobTransport;

            await WaitFor(() =>
            {
                alice.Pump(0);
                bob.Pump(0);
                return alice.Started && bob.Started;
            }, 5000, "match start");

            // Bob "cheats": builds a different world.
            alice.AttachGame(BuildNetGame(alice.Seed, alice.Factions));
            var bobGame = BuildNetGame(bob.Seed, bob.Factions);
            bobGame.Spawn("so_scout_buggy", 1, new CellPos(40, 40));   // extra unit
            bob.AttachGame(bobGame);

            int waited = 0;
            while (!alice.Desynced && !bob.Desynced && waited < 60000)
            {
                alice.Pump(40);
                bob.Pump(40);
                await Task.Delay(3);
                waited += 3;
            }

            Assert.True(server.DesyncDetected, "server must catch the hash mismatch");
            Assert.True(alice.Desynced || bob.Desynced, "clients must be notified");
        }
    }
}
