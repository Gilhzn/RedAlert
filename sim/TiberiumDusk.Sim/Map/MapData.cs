using System;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.Map
{
    /// <summary>
    /// The static terrain: per-cell land type and height level. Runtime overlays
    /// (tiberium, craters, buildings) live in World state, not here.
    /// </summary>
    public sealed class MapData
    {
        public readonly int Width;
        public readonly int Height;

        private readonly byte[] _land;
        private readonly sbyte[] _heightLevel;

        public MapData(int width, int height, byte defaultLand)
        {
            Width = width;
            Height = height;
            _land = new byte[width * height];
            _heightLevel = new sbyte[width * height];
            if (defaultLand != 0)
            {
                for (int i = 0; i < _land.Length; i++) _land[i] = defaultLand;
            }
        }

        public bool InBounds(CellPos cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

        public int CellIndex(CellPos cell) => cell.Y * Width + cell.X;

        public byte Land(CellPos cell) => _land[CellIndex(cell)];
        public sbyte HeightLevel(CellPos cell) => _heightLevel[CellIndex(cell)];

        public void SetLand(CellPos cell, byte land) => _land[CellIndex(cell)] = land;
        public void SetHeightLevel(CellPos cell, sbyte level) => _heightLevel[CellIndex(cell)] = level;

        public void FillLand(int x0, int y0, int x1, int y1, byte land)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    _land[y * Width + x] = land;
        }

        /// <summary>Units can climb/descend at most one height level between adjacent cells (ramps); more is a cliff face.</summary>
        public bool HeightPassable(CellPos from, CellPos to)
        {
            int diff = _heightLevel[CellIndex(to)] - _heightLevel[CellIndex(from)];
            return diff <= 1 && diff >= -1;
        }
    }
}
