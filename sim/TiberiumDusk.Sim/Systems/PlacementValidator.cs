using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Structure placement rules: buildable flat land, free cells, no crystal,
    /// and within the adjacency radius of the owner's base (classic build-radius).
    /// </summary>
    public static class PlacementValidator
    {
        public static bool CanPlace(World world, int playerId, UnitSpec spec, CellPos origin,
            bool requireAdjacency = true)
        {
            var s = spec.Structure;
            if (s == null) return false;

            sbyte? height = null;
            for (int dy = 0; dy < s.FootprintH; dy++)
            {
                for (int dx = 0; dx < s.FootprintW; dx++)
                {
                    var cell = new CellPos(origin.X + dx, origin.Y + dy);
                    if (!world.Map.InBounds(cell)) return false;
                    if (!world.Rules.Lands[world.Map.Land(cell)].Buildable) return false;
                    if (world.OccupantOf(cell) != -1) return false;
                    if (world.Crystal.HasCrystal(cell)) return false;

                    var cellHeight = world.Map.HeightLevel(cell);
                    if (height == null) height = cellHeight;
                    else if (height != cellHeight) return false;   // must be flat
                }
            }

            return !requireAdjacency || IsNearBase(world, playerId, spec, origin);
        }

        private static bool IsNearBase(World world, int playerId, UnitSpec spec, CellPos origin)
        {
            int reach = spec.Structure.Adjacent;
            var entities = world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (!e.Alive || e.Owner != playerId || !e.Spec.IsStructure || !e.Spec.Structure.BaseNormal)
                    continue;
                if (RectDistance(origin, spec.Structure, e.HomeCell, e.Spec.Structure) <= reach)
                    return true;
            }
            return false;
        }

        /// <summary>Chebyshev gap between two footprint rectangles (0 = touching/overlapping).</summary>
        private static int RectDistance(CellPos aOrigin, StructureSpec a, CellPos bOrigin, StructureSpec b)
        {
            int axMax = aOrigin.X + a.FootprintW - 1;
            int ayMax = aOrigin.Y + a.FootprintH - 1;
            int bxMax = bOrigin.X + b.FootprintW - 1;
            int byMax = bOrigin.Y + b.FootprintH - 1;

            int dx = 0;
            if (bxMax < aOrigin.X) dx = aOrigin.X - bxMax;
            else if (bOrigin.X > axMax) dx = bOrigin.X - axMax;

            int dy = 0;
            if (byMax < aOrigin.Y) dy = aOrigin.Y - byMax;
            else if (bOrigin.Y > ayMax) dy = bOrigin.Y - ayMax;

            return dx > dy ? dx : dy;
        }
    }
}
