using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.Pathfinding;
using Xunit;

namespace TiberiumDusk.Sim.Tests
{
    public class PathfindingTests
    {
        [Fact]
        public void FindsStraightPathOnFlatGround()
        {
            var map = TestWorlds.Flat();
            var path = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Tracked,
                new CellPos(5, 5), new CellPos(15, 5));
            Assert.NotNull(path);
            Assert.Equal(10, path.Count);
            Assert.Equal(new CellPos(15, 5), path[path.Count - 1]);
        }

        [Fact]
        public void TrackedUnitRoutesAroundWaterThroughBridge()
        {
            var map = TestWorlds.WaterChannel();
            var path = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Tracked,
                new CellPos(10, 10), new CellPos(50, 10));
            Assert.NotNull(path);
            // Must pass through the bridge row (y=50) inside the channel.
            Assert.Contains(path, c => c.X >= 30 && c.X <= 33 && c.Y == 50);
            foreach (var cell in path)
            {
                Assert.NotEqual(TestWorlds.Land("water"), map.Land(cell));
            }
        }

        [Fact]
        public void HoverUnitCrossesWaterDirectly()
        {
            var map = TestWorlds.WaterChannel();
            var path = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Hover,
                new CellPos(10, 10), new CellPos(50, 10));
            Assert.NotNull(path);
            // Hover ignores the channel: near-straight path, nowhere near the bridge detour.
            Assert.True(path.Count <= 45, $"hover path unexpectedly long: {path.Count}");
        }

        [Fact]
        public void NoPathWhenChannelHasNoBridge()
        {
            var map = TestWorlds.Flat();
            map.FillLand(30, 0, 33, 63, TestWorlds.Land("water"));
            var path = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Tracked,
                new CellPos(10, 10), new CellPos(50, 10));
            Assert.Null(path);
        }

        [Fact]
        public void CliffHeightBlocksButRampPasses()
        {
            var map = TestWorlds.Flat();
            // Plateau at height 2 on x>=20, with a two-step ramp only at y=5 (x=19 h1).
            for (int y = 0; y < 64; y++)
                for (int x = 20; x < 64; x++)
                    map.SetHeightLevel(new CellPos(x, y), 2);
            map.SetHeightLevel(new CellPos(19, 5), 1);

            var blocked = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Tracked,
                new CellPos(10, 40), new CellPos(40, 40));
            var viaRamp = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Tracked,
                new CellPos(10, 40), new CellPos(40, 40));

            // Only route up is through the ramp cell at (19,5).
            Assert.NotNull(viaRamp);
            Assert.Contains(viaRamp, c => c.Equals(new CellPos(19, 5)));
            Assert.NotNull(blocked);
        }

        [Fact]
        public void DiagonalDoesNotCutBlockedCorners()
        {
            var map = TestWorlds.Flat();
            // Wall of cliff with a diagonal gap: (10,10) and (11,11) are cliff,
            // moving (10,11)->(11,10) diagonally would cut between them.
            map.SetLand(new CellPos(10, 10), TestWorlds.Land("cliff"));
            map.SetLand(new CellPos(11, 11), TestWorlds.Land("cliff"));

            var path = AStar.FindPath(map, TestWorlds.Rules, LocomotorId.Tracked,
                new CellPos(10, 11), new CellPos(11, 10));
            Assert.NotNull(path);
            Assert.True(path.Count >= 2, "corner-cutting diagonal must be forbidden");
        }

        [Fact]
        public void FlowFieldLeadsEveryReachableCellToDestination()
        {
            var map = TestWorlds.WaterChannel();
            var dest = new CellPos(50, 10);
            var flow = FlowField.Compute(map, TestWorlds.Rules, LocomotorId.Tracked, dest);

            // From several starts, walking the field must reach the destination.
            var starts = new[] { new CellPos(5, 5), new CellPos(10, 60), new CellPos(28, 10) };
            foreach (var start in starts)
            {
                var current = start;
                int steps = 0;
                while (!current.Equals(dest))
                {
                    var next = flow.NextCell(map, current);
                    Assert.True(next.HasValue, $"flow field dead-end at {current} from {start}");
                    current = next.Value;
                    Assert.True(++steps < 500, "flow field walk did not converge");
                }
            }
        }

        [Fact]
        public void FlowFieldMarksUnreachableCells()
        {
            var map = TestWorlds.Flat();
            map.FillLand(30, 0, 33, 63, TestWorlds.Land("water"));
            var flow = FlowField.Compute(map, TestWorlds.Rules, LocomotorId.Tracked, new CellPos(50, 10));
            Assert.Null(flow.NextCell(map, new CellPos(10, 10)));
        }
    }
}
