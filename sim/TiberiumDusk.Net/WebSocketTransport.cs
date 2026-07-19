using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TiberiumDusk.Net
{
    /// <summary>
    /// Desktop/editor transport over ClientWebSocket: a background receive loop
    /// queues payloads; the game thread polls. (WebGL gets a JS-bridge
    /// implementation of INetTransport in the release phase.)
    /// </summary>
    public sealed class WebSocketTransport : INetTransport, IDisposable
    {
        private readonly ClientWebSocket _socket = new ClientWebSocket();
        private readonly ConcurrentQueue<byte[]> _inbox = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> _outbox = new ConcurrentQueue<byte[]>();
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private Task _receiveLoop;
        private Task _sendLoop;

        public bool Connected => _socket.State == WebSocketState.Open;
        public string LastError { get; private set; }

        public async Task ConnectAsync(string url)
        {
            await _socket.ConnectAsync(new Uri(url), _cancel.Token).ConfigureAwait(false);
            _receiveLoop = Task.Run(ReceiveLoop);
            _sendLoop = Task.Run(SendLoop);
        }

        public void Send(byte[] payload) => _outbox.Enqueue(payload);

        public byte[] Poll() => _inbox.TryDequeue(out var payload) ? payload : null;

        private async Task SendLoop()
        {
            try
            {
                while (!_cancel.IsCancellationRequested)
                {
                    if (_outbox.TryDequeue(out var payload))
                    {
                        await _socket.SendAsync(new ArraySegment<byte>(payload),
                            WebSocketMessageType.Binary, true, _cancel.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(2, _cancel.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[64 * 1024];
            try
            {
                while (!_cancel.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    int total = 0;
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer, total, buffer.Length - total),
                            _cancel.Token).ConfigureAwait(false);
                        total += result.Count;
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;
                    var payload = new byte[total];
                    Array.Copy(buffer, payload, total);
                    _inbox.Enqueue(payload);
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }
        }

        public void Dispose()
        {
            _cancel.Cancel();
            try { _socket.Dispose(); } catch { }
        }
    }
}
