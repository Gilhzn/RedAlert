using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.Pathfinding
{
    /// <summary>
    /// Deterministic grid A*. Ties broken by insertion sequence so expansion
    /// order is identical on every client. Returns waypoint cells from the cell
    /// AFTER start up to goal, or null when unreachable / search cap exceeded.
    /// </summary>
    public static class AStar
    {
        private const int MaxExpansions = 65536;

        /// <summary>
        /// Extra cost added for entering a cell currently occupied by another
        /// unit: steers repaths around blockers (they may vacate, so occupied
        /// cells are expensive rather than impassable).
        /// </summary>
        public const int OccupiedCellPenalty = PathCost.Cardinal * 16;

        public delegate int ExtraCost(CellPos cell);

        public static List<CellPos> FindPath(MapData map, RulesData rules, LocomotorId locomotor,
            CellPos start, CellPos goal, ExtraCost extraCost = null)
        {
            if (!map.InBounds(start) || !map.InBounds(goal)) return null;
            if (start.Equals(goal)) return new List<CellPos>();
            if (!rules.IsPassable(map.Land(goal), locomotor)) return null;

            int size = map.Width * map.Height;
            var gScore = new int[size];
            var cameFrom = new int[size];
            var closed = new bool[size];
            for (int i = 0; i < size; i++)
            {
                gScore[i] = int.MaxValue;
                cameFrom[i] = -1;
            }

            var open = new BinaryHeap();
            int startIdx = map.CellIndex(start);
            gScore[startIdx] = 0;
            open.Push(PathCost.Heuristic(start, goal), startIdx);

            int goalIdx = map.CellIndex(goal);
            int expansions = 0;

            while (open.Count > 0)
            {
                int currentIdx = open.Pop();
                if (closed[currentIdx]) continue;
                closed[currentIdx] = true;

                if (currentIdx == goalIdx)
                    return Reconstruct(map, cameFrom, goalIdx);

                if (++expansions > MaxExpansions) return null;

                var current = new CellPos(currentIdx % map.Width, currentIdx / map.Width);
                for (int n = 0; n < 8; n++)
                {
                    var next = new CellPos(current.X + PathCost.NeighborDx[n], current.Y + PathCost.NeighborDy[n]);
                    if (!map.InBounds(next)) continue;
                    int nextIdx = map.CellIndex(next);
                    if (closed[nextIdx]) continue;

                    int stepCost = PathCost.EnterCost(map, rules, locomotor, current, next, diagonal: n >= 4);
                    if (stepCost == PathCost.Unreachable) continue;
                    if (extraCost != null) stepCost += extraCost(next);

                    int tentative = gScore[currentIdx] + stepCost;
                    if (tentative < gScore[nextIdx])
                    {
                        gScore[nextIdx] = tentative;
                        cameFrom[nextIdx] = currentIdx;
                        open.Push(tentative + PathCost.Heuristic(next, goal), nextIdx);
                    }
                }
            }

            return null;
        }

        private static List<CellPos> Reconstruct(MapData map, int[] cameFrom, int goalIdx)
        {
            var path = new List<CellPos>();
            int idx = goalIdx;
            while (idx != -1)
            {
                path.Add(new CellPos(idx % map.Width, idx / map.Width));
                idx = cameFrom[idx];
            }
            path.Reverse();
            path.RemoveAt(0); // drop the start cell
            return path;
        }

        /// <summary>Min-heap keyed by f-score, ties broken by push sequence (determinism).</summary>
        private sealed class BinaryHeap
        {
            private long[] _keys = new long[256];   // (f << 24) | sequence
            private int[] _values = new int[256];
            private int _count;
            private int _sequence;

            public int Count => _count;

            public void Push(int f, int value)
            {
                if (_count == _keys.Length)
                {
                    System.Array.Resize(ref _keys, _count * 2);
                    System.Array.Resize(ref _values, _count * 2);
                }
                long key = ((long)f << 24) | (uint)(_sequence++ & 0xFFFFFF);
                int i = _count++;
                _keys[i] = key;
                _values[i] = value;
                while (i > 0)
                {
                    int parent = (i - 1) >> 1;
                    if (_keys[parent] <= _keys[i]) break;
                    Swap(parent, i);
                    i = parent;
                }
            }

            public int Pop()
            {
                int result = _values[0];
                _count--;
                _keys[0] = _keys[_count];
                _values[0] = _values[_count];
                int i = 0;
                while (true)
                {
                    int left = i * 2 + 1;
                    if (left >= _count) break;
                    int right = left + 1;
                    int smallest = (right < _count && _keys[right] < _keys[left]) ? right : left;
                    if (_keys[i] <= _keys[smallest]) break;
                    Swap(i, smallest);
                    i = smallest;
                }
                return result;
            }

            private void Swap(int a, int b)
            {
                (_keys[a], _keys[b]) = (_keys[b], _keys[a]);
                (_values[a], _values[b]) = (_values[b], _values[a]);
            }
        }
    }
}
