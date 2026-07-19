using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TiberiumDusk.Net;
using TiberiumDusk.Sim.Orders;

namespace TiberiumDusk.Server
{
    /// <summary>
    /// Lockstep relay: accepts WebSocket clients, assigns player ids, starts
    /// the match when everyone is ready, then per net-turn collects every
    /// player's orders and broadcasts the combined bundle. Also cross-checks
    /// state hashes for desync detection. The server never simulates — the
    /// deterministic clients do; it only orders the order stream.
    /// </summary>
    public sealed class RelayServer : IDisposable
    {
        private sealed class ClientSlot
        {
            public int PlayerId;
            public string Name = "";
            public string Faction = "dominion";
            public WebSocket Socket;
            public bool Ready;
            public readonly Dictionary<int, List<Order>> PendingTurns = new Dictionary<int, List<Order>>();
            public readonly Dictionary<int, ulong> Hashes = new Dictionary<int, ulong>();
        }

        private readonly HttpListener _listener = new HttpListener();
        private readonly List<ClientSlot> _clients = new List<ClientSlot>();
        private readonly object _lock = new object();
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly int _playersToStart;
        private readonly ulong _seed;

        private bool _started;
        private int _nextBroadcastTurn = NetProtocol.DefaultOrderDelayTurns;
        public bool DesyncDetected { get; private set; }
        public int Port { get; }

        public RelayServer(int port, int playersToStart = 2, ulong? seed = null)
        {
            Port = port;
            _playersToStart = playersToStart;
            _seed = seed ?? (ulong)Guid.NewGuid().GetHashCode();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            _listener.Start();
            _ = Task.Run(AcceptLoop);
        }

        private async Task AcceptLoop()
        {
            while (!_cancel.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch
                {
                    return;   // listener closed
                }

                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                var wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                var slot = new ClientSlot { Socket = wsContext.WebSocket };
                lock (_lock)
                {
                    slot.PlayerId = _clients.Count;
                    _clients.Add(slot);
                }
                _ = Task.Run(() => ClientLoop(slot));
            }
        }

        private async Task ClientLoop(ClientSlot slot)
        {
            var buffer = new byte[64 * 1024];
            try
            {
                while (!_cancel.IsCancellationRequested && slot.Socket.State == WebSocketState.Open)
                {
                    int total = 0;
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await slot.Socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer, total, buffer.Length - total),
                            _cancel.Token).ConfigureAwait(false);
                        total += result.Count;
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;
                    var payload = new byte[total];
                    Array.Copy(buffer, payload, total);
                    HandleMessage(slot, payload);
                }
            }
            catch
            {
                // fallthrough to disconnect handling
            }

            lock (_lock)
            {
                Broadcast(NetProtocol.EncodePlayerLeft(slot.PlayerId));
            }
        }

        private void HandleMessage(ClientSlot slot, byte[] payload)
        {
            lock (_lock)
            {
                switch (NetProtocol.PeekType(payload))
                {
                    case MessageType.Join:
                    {
                        var (name, faction) = NetProtocol.DecodeJoin(payload);
                        slot.Name = name;
                        slot.Faction = faction;
                        SendTo(slot, NetProtocol.EncodeWelcome(slot.PlayerId));
                        TryStart();
                        break;
                    }
                    case MessageType.Ready:
                        slot.Ready = true;
                        break;
                    case MessageType.Orders:
                    {
                        var (turn, orders) = NetProtocol.DecodeOrdersOrTurn(payload);
                        // Trust nothing: relabel every order with the sender's id.
                        var sanitized = new List<Order>(orders.Count);
                        foreach (var o in orders)
                        {
                            sanitized.Add(new Order(o.Type, slot.PlayerId, o.ExecuteTick,
                                o.EntityId, o.TargetEntityId, o.TargetPos, o.Data));
                        }
                        slot.PendingTurns[turn] = sanitized;
                        TryBroadcastTurns();
                        break;
                    }
                    case MessageType.Hash:
                    {
                        var (turn, hash) = NetProtocol.DecodeHash(payload);
                        slot.Hashes[turn] = hash;
                        CheckDesync(turn);
                        break;
                    }
                }
            }
        }

        private void TryStart()
        {
            if (_started || _clients.Count < _playersToStart) return;
            _started = true;
            var factions = _clients.Select(c => c.Faction).ToArray();
            Broadcast(NetProtocol.EncodeStart(_seed, factions,
                NetProtocol.DefaultTurnLengthTicks, NetProtocol.DefaultOrderDelayTurns));
        }

        private void TryBroadcastTurns()
        {
            while (true)
            {
                int turn = _nextBroadcastTurn;
                if (_clients.Count < _playersToStart) return;
                foreach (var client in _clients)
                {
                    if (!client.PendingTurns.ContainsKey(turn)) return;   // still waiting
                }

                var combined = new List<Order>();
                foreach (var client in _clients.OrderBy(c => c.PlayerId))
                {
                    combined.AddRange(client.PendingTurns[turn]);
                    client.PendingTurns.Remove(turn);
                }
                Broadcast(NetProtocol.EncodeTurn(turn, combined));
                _nextBroadcastTurn++;
            }
        }

        private void CheckDesync(int turn)
        {
            ulong? reference = null;
            foreach (var client in _clients)
            {
                if (!client.Hashes.TryGetValue(turn, out var hash)) return;   // not all reported yet
                if (reference == null) reference = hash;
                else if (reference != hash)
                {
                    DesyncDetected = true;
                    Broadcast(NetProtocol.EncodeDesync(turn));
                    return;
                }
            }
            foreach (var client in _clients) client.Hashes.Remove(turn);
        }

        private void Broadcast(byte[] payload)
        {
            foreach (var client in _clients)
            {
                SendTo(client, payload);
            }
        }

        private void SendTo(ClientSlot slot, byte[] payload)
        {
            if (slot.Socket.State != WebSocketState.Open) return;
            try
            {
                slot.Socket.SendAsync(new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary, true, _cancel.Token).Wait(2000);
            }
            catch
            {
                // client is gone; disconnect handling will notice
            }
        }

        public void Dispose()
        {
            _cancel.Cancel();
            try { _listener.Stop(); } catch { }
        }
    }
}
