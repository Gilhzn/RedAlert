namespace TiberiumDusk.Sim.Math
{
    /// <summary>
    /// Incremental FNV-1a 64-bit hasher over simulation state. Clients exchange
    /// these hashes periodically to detect lockstep desync, and tests use them
    /// to assert determinism.
    /// </summary>
    public struct StateHash
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private ulong _hash;

        public static StateHash Create() => new StateHash { _hash = OffsetBasis };

        public void Add(int value)
        {
            unchecked
            {
                for (int i = 0; i < 4; i++)
                {
                    _hash = (_hash ^ (byte)(value >> (i * 8))) * Prime;
                }
            }
        }

        public void Add(long value)
        {
            Add((int)value);
            Add((int)(value >> 32));
        }

        public void Add(ulong value) => Add((long)value);

        public void Add(Fixed value) => Add(value.Raw);

        public void Add(LeptonPos pos)
        {
            Add(pos.X);
            Add(pos.Y);
        }

        public ulong Value => _hash;
    }
}
