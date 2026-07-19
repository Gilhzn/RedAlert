using System.Collections.Generic;
using System.IO;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Orders;

namespace TiberiumDusk.Sim
{
    /// <summary>
    /// A replay is just the seed + settings + the full order log — the
    /// deterministic sim reproduces the entire match from it. The same format
    /// doubles as the lockstep network payload later.
    /// </summary>
    public sealed class ReplayLog
    {
        private const uint Magic = 0x54444C52;   // "RLDT"
        private const int Version = 1;

        public ulong Seed;
        public bool IonStormsEnabled;
        public bool CratesEnabled;
        public readonly string[] Factions = new string[8];
        private readonly SortedDictionary<int, List<Order>> _orders = new SortedDictionary<int, List<Order>>();

        public int LastTick { get; private set; }

        public void Record(int tick, IReadOnlyList<Order> orders)
        {
            if (tick > LastTick) LastTick = tick;
            if (orders.Count == 0) return;
            if (!_orders.TryGetValue(tick, out var list))
            {
                list = new List<Order>();
                _orders.Add(tick, list);
            }
            list.AddRange(orders);
        }

        private static readonly List<Order> Empty = new List<Order>();

        public IReadOnlyList<Order> OrdersFor(int tick) =>
            _orders.TryGetValue(tick, out var list) ? list : (IReadOnlyList<Order>)Empty;

        public byte[] Serialize()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(Seed);
            writer.Write(IonStormsEnabled);
            writer.Write(CratesEnabled);
            for (int i = 0; i < Factions.Length; i++) writer.Write(Factions[i] ?? "");
            writer.Write(LastTick);
            writer.Write(_orders.Count);
            foreach (var pair in _orders)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Count);
                foreach (var order in pair.Value)
                {
                    writer.Write((byte)order.Type);
                    writer.Write(order.PlayerId);
                    writer.Write(order.ExecuteTick);
                    writer.Write(order.EntityId);
                    writer.Write(order.TargetEntityId);
                    writer.Write(order.TargetPos.X);
                    writer.Write(order.TargetPos.Y);
                    writer.Write(order.Data);
                }
            }
            writer.Flush();
            return stream.ToArray();
        }

        public static ReplayLog Deserialize(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != Magic) throw new InvalidDataException("not a replay file");
            int version = reader.ReadInt32();
            if (version != Version) throw new InvalidDataException($"unsupported replay version {version}");

            var replay = new ReplayLog
            {
                Seed = reader.ReadUInt64(),
                IonStormsEnabled = reader.ReadBoolean(),
                CratesEnabled = reader.ReadBoolean(),
            };
            for (int i = 0; i < replay.Factions.Length; i++) replay.Factions[i] = reader.ReadString();
            int lastTick = reader.ReadInt32();
            int tickCount = reader.ReadInt32();
            for (int t = 0; t < tickCount; t++)
            {
                int tick = reader.ReadInt32();
                int count = reader.ReadInt32();
                var list = new List<Order>(count);
                for (int i = 0; i < count; i++)
                {
                    var type = (OrderType)reader.ReadByte();
                    int playerId = reader.ReadInt32();
                    int executeTick = reader.ReadInt32();
                    int entityId = reader.ReadInt32();
                    int targetEntityId = reader.ReadInt32();
                    var pos = new LeptonPos(reader.ReadInt32(), reader.ReadInt32());
                    int data = reader.ReadInt32();
                    list.Add(new Order(type, playerId, executeTick, entityId, targetEntityId, pos, data));
                }
                replay._orders.Add(tick, list);
            }
            replay.LastTick = lastTick;
            return replay;
        }
    }
}
