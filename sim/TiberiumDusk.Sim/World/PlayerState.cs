using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.WorldModel
{
    /// <summary>Per-player economy state. Power and storage are recomputed from owned structures.</summary>
    public sealed class PlayerState
    {
        public int PlayerId;
        public int Credits;
        public int PowerProduced;
        public int PowerDrained;
        /// <summary>Storage cap in credits: base treasury + silo/refinery bails × green value.</summary>
        public int StorageCapacityCredits;

        public bool LowPower => PowerDrained > PowerProduced;

        /// <summary>
        /// Production speed % under the current power balance (100 = full speed).
        /// Low power scales toward LowPowerWorstPercent as the deficit grows.
        /// </summary>
        public int BuildRatePercent(int worstPercent, int bestPercent)
        {
            if (!LowPower) return 100;
            if (PowerDrained <= 0) return 100;
            // Deficit severity: produced/drained in [0,1] mapped between worst..best.
            int ratio = PowerProduced * 100 / PowerDrained;
            int range = bestPercent - worstPercent;
            return worstPercent + range * ratio / 100;
        }

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(PlayerId);
            hash.Add(Credits);
            hash.Add(PowerProduced);
            hash.Add(PowerDrained);
            hash.Add(StorageCapacityCredits);
        }
    }
}
