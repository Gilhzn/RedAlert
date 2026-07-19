using System;

namespace TiberiumDusk.Sim.Math
{
    /// <summary>
    /// Deterministic 16.16 fixed-point number. The simulation must never use
    /// float/double: IEEE transcendental results differ across platforms and
    /// would desync lockstep clients.
    /// </summary>
    public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
    {
        public const int FractionalBits = 16;
        public const int One = 1 << FractionalBits;

        public readonly int Raw;

        private Fixed(int raw) => Raw = raw;

        public static Fixed FromRaw(int raw) => new Fixed(raw);
        public static Fixed FromInt(int value) => new Fixed(value << FractionalBits);
        /// <summary>value/denominator as a fixed-point fraction (e.g. Ratio(1,2) = 0.5).</summary>
        public static Fixed Ratio(int numerator, int denominator) =>
            new Fixed((int)(((long)numerator << FractionalBits) / denominator));

        public static readonly Fixed Zero = new Fixed(0);
        public static readonly Fixed Unit = new Fixed(One);

        public int ToIntFloor() => Raw >> FractionalBits;
        public int ToIntRound() => (Raw + (One >> 1)) >> FractionalBits;

        public static Fixed operator +(Fixed a, Fixed b) => new Fixed(a.Raw + b.Raw);
        public static Fixed operator -(Fixed a, Fixed b) => new Fixed(a.Raw - b.Raw);
        public static Fixed operator -(Fixed a) => new Fixed(-a.Raw);
        public static Fixed operator *(Fixed a, Fixed b) => new Fixed((int)(((long)a.Raw * b.Raw) >> FractionalBits));
        public static Fixed operator /(Fixed a, Fixed b) => new Fixed((int)(((long)a.Raw << FractionalBits) / b.Raw));
        public static Fixed operator *(Fixed a, int b) => new Fixed(a.Raw * b);
        public static Fixed operator /(Fixed a, int b) => new Fixed(a.Raw / b);

        public static bool operator <(Fixed a, Fixed b) => a.Raw < b.Raw;
        public static bool operator >(Fixed a, Fixed b) => a.Raw > b.Raw;
        public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;
        public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;
        public static bool operator ==(Fixed a, Fixed b) => a.Raw == b.Raw;
        public static bool operator !=(Fixed a, Fixed b) => a.Raw != b.Raw;

        public Fixed Abs() => new Fixed(Raw < 0 ? -Raw : Raw);

        /// <summary>Integer square root of the fixed value (result is fixed-point).</summary>
        public Fixed Sqrt()
        {
            if (Raw < 0) throw new ArgumentOutOfRangeException(nameof(Raw), "sqrt of negative Fixed");
            // sqrt(v * 2^16) = sqrt(v) * 2^8; shift left 16 first to keep precision.
            ulong value = (ulong)Raw << FractionalBits;
            ulong result = 0;
            ulong bit = 1UL << 62;
            while (bit > value) bit >>= 2;
            while (bit != 0)
            {
                if (value >= result + bit)
                {
                    value -= result + bit;
                    result = (result >> 1) + bit;
                }
                else
                {
                    result >>= 1;
                }
                bit >>= 2;
            }
            return new Fixed((int)result);
        }

        public bool Equals(Fixed other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is Fixed f && Equals(f);
        public override int GetHashCode() => Raw;
        public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);
        public override string ToString() => (Raw / (double)One).ToString("0.####");
    }
}
