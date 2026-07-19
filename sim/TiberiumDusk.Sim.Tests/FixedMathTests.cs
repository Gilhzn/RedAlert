using TiberiumDusk.Sim.Math;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class FixedMathTests
    {
        [Fact]
        public void BasicArithmetic()
        {
            var a = Fixed.FromInt(6);
            var b = Fixed.FromInt(4);
            Assert.Equal(Fixed.FromInt(10), a + b);
            Assert.Equal(Fixed.FromInt(2), a - b);
            Assert.Equal(Fixed.FromInt(24), a * b);
            Assert.Equal(Fixed.Ratio(3, 2), a / b);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 4)]
        [InlineData(7, 10)]
        public void RatioRoundTripsWithinTruncationError(int num, int den)
        {
            // Ratio truncates toward zero, so r*den may fall short of num by
            // less than den raw units (e.g. 0.7 has no exact binary form).
            var r = Fixed.Ratio(num, den);
            int error = Fixed.FromInt(num).Raw - (r * den).Raw;
            Assert.InRange(error, 0, den - 1);
        }

        [Fact]
        public void MultiplicationDoesNotOverflowAtMapScale()
        {
            // 512-cell map edge = 131072 leptons; squared fits in the long path.
            var big = Fixed.FromInt(20000);
            var product = big * Fixed.Ratio(1, 2);
            Assert.Equal(Fixed.FromInt(10000), product);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(144)]
        [InlineData(10000)]
        public void SqrtOfPerfectSquares(int square)
        {
            var root = Fixed.FromInt(square).Sqrt();
            Assert.Equal(Fixed.FromInt(square), root * root);
        }

        [Fact]
        public void SqrtIsMonotonic()
        {
            var prev = Fixed.Zero;
            for (int i = 1; i < 500; i++)
            {
                var current = Fixed.FromInt(i).Sqrt();
                Assert.True(current > prev);
                prev = current;
            }
        }

        [Fact]
        public void ToIntRoundsHalfUp()
        {
            Assert.Equal(2, Fixed.Ratio(3, 2).ToIntRound());
            Assert.Equal(1, Fixed.Ratio(3, 2).ToIntFloor());
            Assert.Equal(1, Fixed.Ratio(149, 100).ToIntRound());
        }
    }
}
