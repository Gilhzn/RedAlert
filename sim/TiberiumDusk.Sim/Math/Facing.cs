namespace TiberiumDusk.Sim.Math
{
    /// <summary>
    /// 256-step facing math (classic engine convention): 0 = +X, increasing
    /// toward +Y, wrapping at 256. Integer arctangent via a 33-entry LUT —
    /// deterministic across platforms, unlike Math.Atan2.
    /// </summary>
    public static class Facing
    {
        // atanLut[i] = round(atan(i/32) * 128 / π) — angle units for slope i/32 within one octant.
        private static readonly byte[] AtanLut =
        {
            0, 1, 3, 4, 5, 6, 8, 9, 10, 11, 12, 13, 15, 16, 17, 18,
            19, 20, 21, 22, 23, 24, 25, 25, 26, 27, 28, 29, 29, 30, 31, 31, 32
        };

        /// <summary>Facing (0..255) of the vector (dx, dy). Returns fallback for the zero vector.</summary>
        public static byte FromVector(int dx, int dy, byte fallback = 0)
        {
            if (dx == 0 && dy == 0) return fallback;

            int adx = dx < 0 ? -dx : dx;
            int ady = dy < 0 ? -dy : dy;

            int octantAngle;
            if (adx >= ady)
            {
                octantAngle = AtanLut[(int)(((long)ady * 32 + adx / 2) / adx)];
            }
            else
            {
                octantAngle = 64 - AtanLut[(int)(((long)adx * 32 + ady / 2) / ady)];
            }

            int angle;
            if (dx >= 0)
                angle = dy >= 0 ? octantAngle : 256 - octantAngle;
            else
                angle = dy >= 0 ? 128 - octantAngle : 128 + octantAngle;

            return (byte)angle;
        }

        /// <summary>Signed shortest difference desired−current in [-128, 127].</summary>
        public static int Difference(byte current, byte desired)
        {
            int diff = (desired - current) & 0xFF;
            return diff > 128 ? diff - 256 : diff;
        }

        /// <summary>Rotate current toward desired by at most rot steps.</summary>
        public static byte TurnToward(byte current, byte desired, int rot)
        {
            int diff = Difference(current, desired);
            if (diff >= -rot && diff <= rot) return desired;
            return (byte)(current + (diff > 0 ? rot : -rot));
        }
    }
}
