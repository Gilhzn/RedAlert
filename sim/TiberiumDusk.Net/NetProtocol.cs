using System;
using System.Collections.Generic;
using System.IO;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;

namespace TiberiumDusk.Net
{
    public enum MessageType : byte
    {
        Join = 1,        // C→S: name, faction
        Welcome = 2,     // S→C: playerId
        Start = 3,       // S→C: seed, playerCount, factions, turnLength, orderDelay
        Ready = 4,       // C→S: loaded, ready to tick
        Orders = 5,      // C→S: turn + local orders
        Turn = 6,        // S→C: turn + everyone's orders (execution schedule)
        Hash = 7,        // C→S: turn + state hash
        Desync = 8,      // S→C: turn where hashes diverged
        PlayerLeft = 9,  // S→C
    }

    /// <summary>
    /// Binary lockstep protocol shared by the relay server, the Unity client,
    /// and the headless test clients. One WebSocket message = one payload here.
    /// </summary>
    public static class NetProtocol
    {
        public const int DefaultTurnLengthTicks = 4;    // ~267 ms per net turn at 15 tps
        public const int DefaultOrderDelayTurns = 2;    // orders sent for turn T+2
        public const int HashReportEveryTurns = 8;

        // ---------- encode ----------

        public static byte[] EncodeJoin(string name, string faction)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Join);
            w.Write(name);
            w.Write(faction);
            return s.ToArray();
        }

        public static byte[] EncodeWelcome(int playerId)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Welcome);
            w.Write(playerId);
            return s.ToArray();
        }

        public static byte[] EncodeStart(ulong seed, string[] factions, int turnLength, int orderDelay)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Start);
            w.Write(seed);
            w.Write(factions.Length);
            foreach (var f in factions) w.Write(f ?? "dominion");
            w.Write(turnLength);
            w.Write(orderDelay);
            return s.ToArray();
        }

        public static byte[] EncodeReady()
        {
            return new[] { (byte)MessageType.Ready };
        }

        public static byte[] EncodeOrders(int turn, IReadOnlyList<Order> orders)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Orders);
            w.Write(turn);
            WriteOrders(w, orders);
            return s.ToArray();
        }

        public static byte[] EncodeTurn(int turn, IReadOnlyList<Order> orders)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Turn);
            w.Write(turn);
            WriteOrders(w, orders);
            return s.ToArray();
        }

        public static byte[] EncodeHash(int turn, ulong hash)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Hash);
            w.Write(turn);
            w.Write(hash);
            return s.ToArray();
        }

        public static byte[] EncodeDesync(int turn)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.Desync);
            w.Write(turn);
            return s.ToArray();
        }

        public static byte[] EncodePlayerLeft(int playerId)
        {
            using var s = new MemoryStream();
            using var w = new BinaryWriter(s);
            w.Write((byte)MessageType.PlayerLeft);
            w.Write(playerId);
            return s.ToArray();
        }

        private static void WriteOrders(BinaryWriter w, IReadOnlyList<Order> orders)
        {
            w.Write(orders.Count);
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                w.Write((byte)o.Type);
                w.Write(o.PlayerId);
                w.Write(o.ExecuteTick);
                w.Write(o.EntityId);
                w.Write(o.TargetEntityId);
                w.Write(o.TargetPos.X);
                w.Write(o.TargetPos.Y);
                w.Write(o.Data);
            }
        }

        // ---------- decode ----------

        public static MessageType PeekType(byte[] payload) => (MessageType)payload[0];

        public static (string name, string faction) DecodeJoin(byte[] payload)
        {
            using var r = Reader(payload);
            return (r.ReadString(), r.ReadString());
        }

        public static int DecodeWelcome(byte[] payload)
        {
            using var r = Reader(payload);
            return r.ReadInt32();
        }

        public static (ulong seed, string[] factions, int turnLength, int orderDelay) DecodeStart(byte[] payload)
        {
            using var r = Reader(payload);
            ulong seed = r.ReadUInt64();
            var factions = new string[r.ReadInt32()];
            for (int i = 0; i < factions.Length; i++) factions[i] = r.ReadString();
            return (seed, factions, r.ReadInt32(), r.ReadInt32());
        }

        public static (int turn, List<Order> orders) DecodeOrdersOrTurn(byte[] payload)
        {
            using var r = Reader(payload);
            int turn = r.ReadInt32();
            int count = r.ReadInt32();
            var orders = new List<Order>(count);
            for (int i = 0; i < count; i++)
            {
                var type = (OrderType)r.ReadByte();
                int playerId = r.ReadInt32();
                int executeTick = r.ReadInt32();
                int entityId = r.ReadInt32();
                int targetEntityId = r.ReadInt32();
                var pos = new LeptonPos(r.ReadInt32(), r.ReadInt32());
                int data = r.ReadInt32();
                orders.Add(new Order(type, playerId, executeTick, entityId, targetEntityId, pos, data));
            }
            return (turn, orders);
        }

        public static (int turn, ulong hash) DecodeHash(byte[] payload)
        {
            using var r = Reader(payload);
            return (r.ReadInt32(), r.ReadUInt64());
        }

        public static int DecodeDesync(byte[] payload)
        {
            using var r = Reader(payload);
            return r.ReadInt32();
        }

        private static BinaryReader Reader(byte[] payload)
        {
            var stream = new MemoryStream(payload);
            stream.ReadByte();   // skip type
            return new BinaryReader(stream);
        }
    }
}
