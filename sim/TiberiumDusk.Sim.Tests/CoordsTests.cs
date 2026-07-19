using TiberiumDusk.Sim.Math;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class CoordsTests
    {
        [Fact]
        public void CellAndLeptonConversionsRoundTrip()
        {
            var cell = new CellPos(10, 42);
            var center = LeptonPos.CellCenter(cell);
            Assert.Equal(cell, center.ToCell());
            Assert.Equal(10 * 256 + 128, center.X);
            Assert.Equal(42 * 256 + 128, center.Y);
        }

        [Fact]
        public void DistanceMatchesPythagoras()
        {
            var origin = new LeptonPos(0, 0);
            var p = new LeptonPos(300, 400);
            Assert.Equal(500, origin.Distance(p));
            Assert.Equal(250000, origin.DistanceSquared(p));
        }

        [Fact]
        public void DistanceSurvivesLargeMaps()
        {
            // Opposite corners of a 512×512-cell map (131072 leptons per side).
            var a = new LeptonPos(0, 0);
            var b = new LeptonPos(512 * 256, 512 * 256);
            long expectedSquared = 2L * 131072 * 131072;
            Assert.Equal(expectedSquared, a.DistanceSquared(b));
            Assert.Equal(185363, a.Distance(b)); // floor(sqrt(2) * 131072)
        }

        [Fact]
        public void ChebyshevDistanceCountsDiagonalAsOne()
        {
            var a = new CellPos(0, 0);
            Assert.Equal(1, a.ChebyshevDistance(new CellPos(1, 1)));
            Assert.Equal(5, a.ChebyshevDistance(new CellPos(5, 3)));
        }
    }
}
