using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.WorldModel
{
    public enum CrystalType : byte
    {
        Green = 0,
        Blue = 1,
    }

    /// <summary>
    /// The harvestable crystal overlay: per-cell density (0..MaxDensity) and
    /// color. Grows denser and seeds neighbors over time (CrystalSystem).
    /// </summary>
    public sealed class CrystalField
    {
        private readonly MapData _map;
        private readonly byte[] _density;
        private readonly byte[] _type;

        public CrystalField(MapData map)
        {
            _map = map;
            _density = new byte[map.Width * map.Height];
            _type = new byte[map.Width * map.Height];
        }

        public int Density(CellPos cell) => _density[_map.CellIndex(cell)];
        public CrystalType TypeAt(CellPos cell) => (CrystalType)_type[_map.CellIndex(cell)];
        public bool HasCrystal(CellPos cell) => _density[_map.CellIndex(cell)] > 0;

        public void Set(CellPos cell, CrystalType type, int density)
        {
            int idx = _map.CellIndex(cell);
            _density[idx] = (byte)density;
            _type[idx] = (byte)type;
        }

        public void Grow(CellPos cell, int maxDensity)
        {
            int idx = _map.CellIndex(cell);
            if (_density[idx] > 0 && _density[idx] < maxDensity) _density[idx]++;
        }

        /// <summary>Removes one bail; returns its credit value, or 0 if the cell is empty.</summary>
        public int HarvestBail(CellPos cell, EconomyRules economy)
        {
            int idx = _map.CellIndex(cell);
            if (_density[idx] == 0) return 0;
            _density[idx]--;
            return _type[idx] == (byte)CrystalType.Blue ? economy.BlueBailValue : economy.GreenBailValue;
        }

        public void AddToHash(ref StateHash hash)
        {
            // Coarse but sufficient: hash a rolling FNV over both arrays.
            for (int i = 0; i < _density.Length; i++)
            {
                if (_density[i] != 0)
                {
                    hash.Add(i);
                    hash.Add(_density[i] | (_type[i] << 8));
                }
            }
        }
    }
}
