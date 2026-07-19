using System.Collections.Generic;
using TiberiumDusk.Sim;
using TiberiumDusk.Sim.Orders;

namespace TiberiumDusk.Net
{
    /// <summary>Transport abstraction: WebSocket on desktop, JS bridge on WebGL, in-memory in tests.</summary>
    public interface INetTransport
    {
        void Send(byte[] payload);
        /// <summary>Dequeue the next received payload, or null. Pumped from the game thread.</summary>
        byte[] Poll();
    }

    /// <summary>
    /// Delay-based lockstep engine around a deterministic Game. The sim only
    /// advances through ticks whose net-turn bundle has arrived — identical
    /// order streams on every client mean identical worlds.
    /// </summary>
    public sealed class LockstepClient
    {
        public int LocalPlayerId { get; private set; } = -1;
        public bool Started { get; private set; }
        public bool Desynced { get; private set; }
        public ulong Seed { get; private set; }
        public string[] Factions { get; private set; }
        public int TurnLength { get; private set; } = NetProtocol.DefaultTurnLengthTicks;
        public int OrderDelay { get; private set; } = NetProtocol.DefaultOrderDelayTurns;

        private readonly INetTransport _transport;
        private Game _game;
        private readonly List<Order> _localPending = new List<Order>();
        private readonly Dictionary<int, List<Order>> _turnBundles = new Dictionary<int, List<Order>>();
        private static readonly List<Order> Empty = new List<Order>();
        private int _sentThroughTurn = -1;

        public LockstepClient(INetTransport transport)
        {
            _transport = transport;
        }

        public Game Game => _game;

        public void SendJoin(string name, string faction) =>
            _transport.Send(NetProtocol.EncodeJoin(name, faction));

        /// <summary>Call once the Start message arrived and the local Game is constructed.</summary>
        public void AttachGame(Game game)
        {
            _game = game;
            _transport.Send(NetProtocol.EncodeReady());
        }

        /// <summary>Queue a local player order for the next outgoing turn.</summary>
        public void Issue(Order order) => _localPending.Add(order);

        /// <summary>Pump network + advance the sim as far as arrived bundles allow. Returns ticks advanced.</summary>
        public int Pump(int maxTicks)
        {
            Drain();
            if (!Started || _game == null || Desynced) return 0;

            int advanced = 0;
            while (advanced < maxTicks)
            {
                int tick = _game.CurrentTick;
                int turn = tick / TurnLength;

                // At each turn boundary, ship local orders scheduled for turn+delay.
                if (tick % TurnLength == 0 && _sentThroughTurn < turn)
                {
                    _sentThroughTurn = turn;
                    _transport.Send(NetProtocol.EncodeOrders(turn + OrderDelay, _localPending));
                    _localPending.Clear();

                    if (turn % NetProtocol.HashReportEveryTurns == 0)
                    {
                        _transport.Send(NetProtocol.EncodeHash(turn, _game.ComputeHash()));
                    }
                }

                // The first tick of a turn needs that turn's bundle.
                IReadOnlyList<Order> orders = Empty;
                if (tick % TurnLength == 0)
                {
                    if (turn < OrderDelay)
                    {
                        // Bootstrap turns are implicitly empty everywhere.
                    }
                    else if (_turnBundles.TryGetValue(turn, out var bundle))
                    {
                        orders = bundle;
                        _turnBundles.Remove(turn);
                    }
                    else
                    {
                        break;   // waiting on the network — stall deterministically
                    }
                }

                _game.Tick(orders);
                advanced++;
                Drain();
            }
            return advanced;
        }

        private void Drain()
        {
            byte[] payload;
            while ((payload = _transport.Poll()) != null)
            {
                switch (NetProtocol.PeekType(payload))
                {
                    case MessageType.Welcome:
                        LocalPlayerId = NetProtocol.DecodeWelcome(payload);
                        break;
                    case MessageType.Start:
                    {
                        var (seed, factions, turnLength, orderDelay) = NetProtocol.DecodeStart(payload);
                        Seed = seed;
                        Factions = factions;
                        TurnLength = turnLength;
                        OrderDelay = orderDelay;
                        Started = true;
                        break;
                    }
                    case MessageType.Turn:
                    {
                        var (turn, orders) = NetProtocol.DecodeOrdersOrTurn(payload);
                        _turnBundles[turn] = orders;
                        break;
                    }
                    case MessageType.Desync:
                        Desynced = true;
                        break;
                }
            }
        }
    }
}
