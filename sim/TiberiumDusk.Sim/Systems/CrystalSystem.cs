using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Crystal field life: periodic growth passes (cells densify, dense cells
    /// seed clear neighbors) and field damage to unshielded infantry.
    /// </summary>
    public sealed class CrystalSystem
    {
        private readonly World _world;
        private readonly DeterministicRandom _random;
        private int _growthCountdown;
        private int _damageCountdown;

        public CrystalSystem(World world, DeterministicRandom random)
        {
            _world = world;
            _random = random;
            _growthCountdown = world.Rules.Economy.GrowthIntervalTicks;
            _damageCountdown = world.Rules.Economy.CrystalDamageIntervalTicks;
        }

        public void Tick()
        {
            if (--_growthCountdown <= 0)
            {
                _growthCountdown = _world.Rules.Economy.GrowthIntervalTicks;
                GrowthPass();
            }
            if (--_damageCountdown <= 0)
            {
                _damageCountdown = _world.Rules.Economy.CrystalDamageIntervalTicks;
                DamagePass();
            }
        }

        private void GrowthPass()
        {
            var economy = _world.Rules.Economy;
            var map = _world.Map;
            var crystal = _world.Crystal;

            // Snapshot pass over the grid in row-major order (deterministic).
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = new CellPos(x, y);
                    int density = crystal.Density(cell);
                    if (density == 0) continue;

                    if (density < economy.GrowDensityThreshold && _random.Chance(economy.GrowthChancePercent))
                    {
                        crystal.Grow(cell, economy.MaxDensity);
                    }

                    if (density > economy.SeedDensityThreshold && _random.Chance(economy.SpreadChancePercent))
                    {
                        SeedNeighbor(cell, crystal.TypeAt(cell));
                    }
                }
            }
        }

        private void SeedNeighbor(CellPos from, CrystalType type)
        {
            // One random free, growable neighbor.
            int start = _random.Next(0, 8);
            for (int n = 0; n < 8; n++)
            {
                int dir = (start + n) % 8;
                var cell = new CellPos(
                    from.X + Pathfinding.PathCost.NeighborDx[dir],
                    from.Y + Pathfinding.PathCost.NeighborDy[dir]);
                if (!_world.Map.InBounds(cell)) continue;
                if (_world.Crystal.HasCrystal(cell)) continue;
                var land = _world.Rules.Lands[_world.Map.Land(cell)];
                // Crystal takes root on open ground only.
                if (!land.Buildable) continue;
                if (_world.OccupantOf(cell) != -1) continue;

                _world.Crystal.Set(cell, type, 1);
                return;
            }
        }

        private void DamagePass()
        {
            var economy = _world.Rules.Economy;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.Alive || !entity.Spec.CrystalVulnerable) continue;
                if (!_world.Crystal.HasCrystal(entity.HomeCell)) continue;

                entity.Hp -= economy.CrystalDamageHp;
                if (entity.Hp <= 0)
                {
                    _world.Kill(entity);
                }
            }
        }
    }
}
