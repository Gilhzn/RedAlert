using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.Pathfinding
{
    /// <summary>Shared cost model for A* and flow fields. All integer.</summary>
    public static class PathCost
    {
        public const int Cardinal = 256;          // cost units per cell at 100% speed
        public const int Diagonal = 362;          // ≈ 256·√2
        public const int Unreachable = int.MaxValue;

        /// <summary>8-neighborhood, cardinals first — index order is part of determinism.</summary>
        public static readonly int[] NeighborDx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        public static readonly int[] NeighborDy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>
        /// Cost of entering <paramref name="to"/> from <paramref name="from"/>,
        /// or Unreachable. Diagonal moves must not cut blocked corners.
        /// </summary>
        public static int EnterCost(MapData map, RulesData rules, LocomotorId locomotor,
            CellPos from, CellPos to, bool diagonal)
        {
            if (!map.InBounds(to)) return Unreachable;
            int speed = rules.SpeedPercent(map.Land(to), locomotor);
            if (speed <= 0) return Unreachable;
            if (!map.HeightPassable(from, to)) return Unreachable;

            if (diagonal)
            {
                // Both shared orthogonal cells must be enterable (no corner cutting).
                var side1 = new CellPos(from.X, to.Y);
                var side2 = new CellPos(to.X, from.Y);
                if (!IsEnterable(map, rules, locomotor, from, side1) ||
                    !IsEnterable(map, rules, locomotor, from, side2))
                    return Unreachable;
            }

            int baseCost = diagonal ? Diagonal : Cardinal;
            return baseCost * 100 / speed;
        }

        public static bool IsEnterable(MapData map, RulesData rules, LocomotorId locomotor, CellPos from, CellPos to)
        {
            return map.InBounds(to)
                && rules.IsPassable(map.Land(to), locomotor)
                && map.HeightPassable(from, to);
        }

        /// <summary>Admissible octile heuristic (assumes best possible 100% speed).</summary>
        public static int Heuristic(CellPos a, CellPos b)
        {
            int dx = a.X > b.X ? a.X - b.X : b.X - a.X;
            int dy = a.Y > b.Y ? a.Y - b.Y : b.Y - a.Y;
            int min = dx < dy ? dx : dy;
            int max = dx < dy ? dy : dx;
            return Cardinal * (max - min) + Diagonal * min;
        }
    }
}
