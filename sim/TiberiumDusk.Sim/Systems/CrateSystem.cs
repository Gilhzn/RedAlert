using System.Collections.Generic;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Supply crates: spawn at random free cells, picked up by the first mobile
    /// ground unit entering the cell. Rewards: money / veterancy / heal / trap.
    /// </summary>
    public sealed class CrateSystem
    {
        private readonly World _world;
        private readonly SuperweaponSystem _superweapons;
        private readonly DeterministicRandom _random;
        private readonly bool _enabled;
        private readonly List<CellPos> _crates = new List<CellPos>();
        private int _regenCountdown;

        /// <summary>View hooks.</summary>
        public event System.Action<CellPos> CrateSpawned;
        public event System.Action<CellPos> CratePicked;

        public CrateSystem(World world, SuperweaponSystem superweapons, DeterministicRandom random, bool enabled)
        {
            _world = world;
            _superweapons = superweapons;
            _random = random;
            _enabled = enabled;
            _regenCountdown = world.Rules.Special.CrateRegenTicks;
        }

        public IReadOnlyList<CellPos> Crates => _crates;

        public void Tick()
        {
            if (!_enabled) return;
            var special = _world.Rules.Special;

            if (--_regenCountdown <= 0)
            {
                _regenCountdown = special.CrateRegenTicks;
                if (_crates.Count < special.CrateMax) SpawnCrate();
            }

            // Pickup: any mobile ground unit standing on a crate cell.
            for (int i = _crates.Count - 1; i >= 0; i--)
            {
                int occupant = _world.OccupantOf(_crates[i]);
                if (occupant == -1) continue;
                var unit = _world.GetEntity(occupant);
                if (unit == null || unit.Spec.Mobile == null) continue;

                var cell = _crates[i];
                _crates.RemoveAt(i);
                GrantReward(unit);
                CratePicked?.Invoke(cell);
            }
        }

        private void SpawnCrate()
        {
            for (int attempt = 0; attempt < 32; attempt++)
            {
                var cell = new CellPos(
                    _random.Next(1, _world.Map.Width - 1),
                    _random.Next(1, _world.Map.Height - 1));
                if (_world.OccupantOf(cell) != -1) continue;
                if (!_world.Rules.Lands[_world.Map.Land(cell)].Buildable) continue;
                if (_world.Crystal.HasCrystal(cell)) continue;
                _crates.Add(cell);
                CrateSpawned?.Invoke(cell);
                return;
            }
        }

        private void GrantReward(Entity unit)
        {
            var special = _world.Rules.Special;
            int total = special.CrateSharesMoney + special.CrateSharesVeterancy
                + special.CrateSharesTrap + special.CrateSharesHeal;
            int roll = _random.Next(0, total);

            if ((roll -= special.CrateSharesMoney) < 0)
            {
                var player = _world.Players[unit.Owner];
                player.Credits = System.Math.Min(
                    player.Credits + special.CrateMoneyAmount, player.StorageCapacityCredits);
            }
            else if ((roll -= special.CrateSharesVeterancy) < 0)
            {
                if (unit.Rank < _world.Rules.Combat.VeteranMaxRank) unit.Rank++;
            }
            else if ((roll -= special.CrateSharesTrap) < 0)
            {
                _superweapons.AreaDamage(-1, unit.Pos, special.CrateTrapDamage, 256, "cluster_he");
            }
            else
            {
                unit.Hp = unit.Spec.Health.Max;
            }
        }

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(_regenCountdown);
            hash.Add(_crates.Count);
            for (int i = 0; i < _crates.Count; i++)
            {
                hash.Add(_crates[i].X);
                hash.Add(_crates[i].Y);
            }
        }
    }
}
