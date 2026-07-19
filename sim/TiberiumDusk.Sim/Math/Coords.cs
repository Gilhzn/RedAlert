using System;

namespace TiberiumDusk.Sim.Math
{
    /// <summary>
    /// World-space position in leptons. Following the classic engine model:
    /// one map cell = 256×256 leptons, so all movement/range math stays integer.
    /// </summary>
    public readonly struct LeptonPos : IEquatable<LeptonPos>
    {
        public const int LeptonsPerCell = 256;
        /// <summary>Vertical leptons per terrain height level.</summary>
        public const int LeptonsPerHeightLevel = 128;

        public readonly int X;
        public readonly int Y;

        public LeptonPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public CellPos ToCell() => new CellPos(X / LeptonsPerCell, Y / LeptonsPerCell);

        public static LeptonPos CellCenter(CellPos cell) => new LeptonPos(
            cell.X * LeptonsPerCell + LeptonsPerCell / 2,
            cell.Y * LeptonsPerCell + LeptonsPerCell / 2);

        public static LeptonPos operator +(LeptonPos a, LeptonPos b) => new LeptonPos(a.X + b.X, a.Y + b.Y);
        public static LeptonPos operator -(LeptonPos a, LeptonPos b) => new LeptonPos(a.X - b.X, a.Y - b.Y);

        /// <summary>Exact squared distance in leptons² (long to avoid overflow on large maps).</summary>
        public long DistanceSquared(LeptonPos other)
        {
            long dx = X - other.X;
            long dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Integer distance in leptons (isqrt of the exact square) — deterministic,
        /// used for range checks. Prefer DistanceSquared when comparing against a constant.
        /// </summary>
        public int Distance(LeptonPos other) => IntSqrt(DistanceSquared(other));

        internal static int IntSqrt(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == 0) return 0;
            long x = value;
            long result = 0;
            long bit = 1L << 62;
            while (bit > x) bit >>= 2;
            while (bit != 0)
            {
                if (x >= result + bit)
                {
                    x -= result + bit;
                    result = (result >> 1) + bit;
                }
                else
                {
                    result >>= 1;
                }
                bit >>= 2;
            }
            return (int)result;
        }

        public bool Equals(LeptonPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is LeptonPos p && Equals(p);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public override string ToString() => $"L({X},{Y})";
    }

    /// <summary>Map grid cell coordinate.</summary>
    public readonly struct CellPos : IEquatable<CellPos>
    {
        public readonly int X;
        public readonly int Y;

        public CellPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static CellPos operator +(CellPos a, CellPos b) => new CellPos(a.X + b.X, a.Y + b.Y);

        /// <summary>Chebyshev distance in cells (diagonal counts as 1) — the classic RTS range metric helper.</summary>
        public int ChebyshevDistance(CellPos other)
        {
            int dx = System.Math.Abs(X - other.X);
            int dy = System.Math.Abs(Y - other.Y);
            return dx > dy ? dx : dy;
        }

        public bool Equals(CellPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is CellPos p && Equals(p);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public override string ToString() => $"C({X},{Y})";
    }
}
