namespace TiberiumDusk.Sim.Math
{
    /// <summary>
    /// Seeded PCG-XSH-RR generator. Part of the hashed world state: any consumer
    /// outside the simulation using this instance would cause a lockstep desync,
    /// so the view layer must own its own cosmetic RNG.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;
        private readonly ulong _increment;

        public DeterministicRandom(ulong seed, ulong sequence = 54)
        {
            _increment = (sequence << 1) | 1UL;
            _state = 0;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = old * 6364136223846793005UL + _increment;
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << (-rot & 31));
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int Next(int minInclusive, int maxExclusive)
        {
            uint range = (uint)(maxExclusive - minInclusive);
            // Debiased modulo (Lemire-style rejection) for exact uniformity.
            uint threshold = (uint)(-range) % range;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                    return (int)(minInclusive + r % range);
            }
        }

        /// <summary>True with probability percent/100.</summary>
        public bool Chance(int percent) => Next(0, 100) < percent;

        /// <summary>Internal state, included in the world hash for desync detection.</summary>
        public ulong State => _state;
    }
}
