// Browser WebSocket bridge for the lockstep transport (WebGL builds).
mergeInto(LibraryManager.library, {
  TDWS_Connect: function (urlPtr) {
    if (!Module.TDWS) Module.TDWS = { sockets: [], queues: [], pending: [] };
    var url = UTF8ToString(urlPtr);
    var id = Module.TDWS.sockets.length;
    var ws = new WebSocket(url);
    ws.binaryType = 'arraybuffer';
    Module.TDWS.queues[id] = [];
    Module.TDWS.pending[id] = [];
    ws.onmessage = function (e) { Module.TDWS.queues[id].push(new Uint8Array(e.data)); };
    ws.onopen = function () {
      var p = Module.TDWS.pending[id];
      while (p.length) ws.send(p.shift());
    };
    Module.TDWS.sockets[id] = ws;
    return id;
  },
  TDWS_State: function (id) {
    var ws = Module.TDWS && Module.TDWS.sockets[id];
    return ws ? ws.readyState : 3;
  },
  TDWS_Send: function (id, ptr, len) {
    var data = HEAPU8.slice(ptr, ptr + len);
    var ws = Module.TDWS.sockets[id];
    if (!ws) return;
    if (ws.readyState === 1) ws.send(data);
    else Module.TDWS.pending[id].push(data);
  },
  TDWS_PollSize: function (id) {
    var q = Module.TDWS && Module.TDWS.queues[id];
    return (q && q.length) ? q[0].length : -1;
  },
  TDWS_PollRead: function (id, ptr, maxLen) {
    var q = Module.TDWS && Module.TDWS.queues[id];
    if (!q || !q.length) return -1;
    var msg = q.shift();
    var n = Math.min(msg.length, maxLen);
    HEAPU8.set(msg.subarray(0, n), ptr);
    return n;
  }
});
