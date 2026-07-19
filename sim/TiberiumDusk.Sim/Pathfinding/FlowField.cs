using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.Pathfinding
{
    /// <summary>
    /// Group-movement flow field: one Dijkstra integration from the destination,
    /// then every unit in the group reads its next step in O(1) per tick.
    /// Handles congestion better than N independent A* paths.
    /// </summary>
    public sealed class FlowField
    {
        public const byte NoDirection = 255;

        public readonly CellPos Destination;
        public readonly LocomotorId Locomotor;

        private readonly byte[] _direction; // neighbor index into PathCost.NeighborDx/Dy, or NoDirection

        private FlowField(CellPos destination, LocomotorId locomotor, int size)
        {
            Destination = destination;
            Locomotor = locomotor;
            _direction = new byte[size];
            for (int i = 0; i < size; i++) _direction[i] = NoDirection;
        }

        /// <summary>Next cell toward the destination from <paramref name="cell"/>, or null if unreachable/at goal.</summary>
        public CellPos? NextCell(MapData map, CellPos cell)
        {
            byte dir = _direction[map.CellIndex(cell)];
            if (dir == NoDirection) return null;
            return new CellPos(cell.X + PathCost.NeighborDx[dir], cell.Y + PathCost.NeighborDy[dir]);
        }

        public static FlowField Compute(MapData map, RulesData rules, LocomotorId locomotor, CellPos destination)
        {
            int size = map.Width * map.Height;
            var field = new FlowField(destination, locomotor, size);
            var integration = new int[size];
            for (int i = 0; i < size; i++) integration[i] = PathCost.Unreachable;

            if (!map.InBounds(destination) || !rules.IsPassable(map.Land(destination), locomotor))
                return field;

            // Dijkstra flood out from the destination.
            var queue = new SortedSet<long>();
            int destIdx = map.CellIndex(destination);
            integration[destIdx] = 0;
            queue.Add(Key(0, destIdx));

            while (queue.Count > 0)
            {
                long minKey = queue.Min;
                queue.Remove(minKey);
                int currentIdx = (int)(minKey & 0xFFFFFF);
                int currentCost = (int)(minKey >> 24);
                if (currentCost > integration[currentIdx]) continue;

                var current = new CellPos(currentIdx % map.Width, currentIdx / map.Width);
                for (int n = 0; n < 8; n++)
                {
                    var next = new CellPos(current.X + PathCost.NeighborDx[n], current.Y + PathCost.NeighborDy[n]);
                    if (!map.InBounds(next)) continue;
                    // Cost of moving next→current (unit walks toward destination).
                    int stepCost = PathCost.EnterCost(map, rules, locomotor, next, current, diagonal: n >= 4);
                    if (stepCost == PathCost.Unreachable) continue;

                    int nextIdx = map.CellIndex(next);
                    int tentative = currentCost + stepCost;
                    if (tentative < integration[nextIdx])
                    {
                        if (integration[nextIdx] != PathCost.Unreachable)
                            queue.Remove(Key(integration[nextIdx], nextIdx));
                        integration[nextIdx] = tentative;
                        queue.Add(Key(tentative, nextIdx));
                    }
                }
            }

            // Direction per cell = neighbor with lowest integration (corner-cut checked).
            for (int idx = 0; idx < size; idx++)
            {
                if (integration[idx] == PathCost.Unreachable || idx == destIdx) continue;
                var cell = new CellPos(idx % map.Width, idx / map.Width);
                int best = integration[idx];
                byte bestDir = NoDirection;
                for (int n = 0; n < 8; n++)
                {
                    var next = new CellPos(cell.X + PathCost.NeighborDx[n], cell.Y + PathCost.NeighborDy[n]);
                    if (!map.InBounds(next)) continue;
                    if (PathCost.EnterCost(map, rules, locomotor, cell, next, diagonal: n >= 4) == PathCost.Unreachable)
                        continue;
                    int cost = integration[map.CellIndex(next)];
                    if (cost < best)
                    {
                        best = cost;
                        bestDir = (byte)n;
                    }
                }
                field._direction[idx] = bestDir;
            }

            return field;
        }

        private static long Key(int cost, int index) => ((long)cost << 24) | (uint)index;
    }
}
