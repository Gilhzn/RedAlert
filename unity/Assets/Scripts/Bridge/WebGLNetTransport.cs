using TiberiumDusk.Net;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// WebGL lockstep transport over the browser's native WebSocket via a
    /// .jslib bridge (Plugins/WebGL/TDWebSocket.jslib). Outgoing messages sent
    /// before the socket opens are buffered JS-side.
    /// </summary>
    public sealed class WebGLNetTransport : INetTransport
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int TDWS_Connect(string url);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int TDWS_State(int id);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void TDWS_Send(int id, byte[] data, int length);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int TDWS_PollSize(int id);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int TDWS_PollRead(int id, byte[] buffer, int maxLength);

        private int _id = -1;

        public void Connect(string url) => _id = TDWS_Connect(url);
        public bool Connected => _id >= 0 && TDWS_State(_id) == 1;

        public void Send(byte[] payload)
        {
            if (_id >= 0) TDWS_Send(_id, payload, payload.Length);
        }

        public byte[] Poll()
        {
            if (_id < 0) return null;
            int size = TDWS_PollSize(_id);
            if (size < 0) return null;
            var buffer = new byte[size];
            TDWS_PollRead(_id, buffer, size);
            return buffer;
        }
#else
        // Non-WebGL platforms use WebSocketTransport; this stub keeps the type compiling.
        public void Connect(string url) =>
            throw new System.PlatformNotSupportedException("WebGLNetTransport is WebGL-only");
        public bool Connected => false;
        public void Send(byte[] payload) { }
        public byte[] Poll() => null;
#endif
    }
}
