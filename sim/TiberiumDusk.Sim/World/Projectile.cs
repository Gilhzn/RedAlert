using System.Collections.Generic;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.WorldModel
{
    /// <summary>An in-flight shot from a non-hitscan weapon.</summary>
    public struct Projectile
    {
        public int Id;
        public int WeaponIndex;
        public int AttackerId;
        /// <summary>Homing target entity, or -1 (then TargetPos is the impact point).</summary>
        public int TargetEntityId;
        public LeptonPos TargetPos;
        public LeptonPos Pos;
    }

    public sealed class ProjectileSet
    {
        private readonly List<Projectile> _list = new List<Projectile>();
        private int _nextId = 1;

        public IReadOnlyList<Projectile> All => _list;

        public void Spawn(int weaponIndex, int attackerId, int targetEntityId, LeptonPos targetPos, LeptonPos from)
        {
            _list.Add(new Projectile
            {
                Id = _nextId++,
                WeaponIndex = weaponIndex,
                AttackerId = attackerId,
                TargetEntityId = targetEntityId,
                TargetPos = targetPos,
                Pos = from,
            });
        }

        public void Update(int index, in Projectile projectile) => _list[index] = projectile;
        public void RemoveAt(int index) => _list.RemoveAt(index);
        public int Count => _list.Count;
        public Projectile this[int index] => _list[index];

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(_nextId);
            for (int i = 0; i < _list.Count; i++)
            {
                var p = _list[i];
                hash.Add(p.Id);
                hash.Add(p.WeaponIndex);
                hash.Add(p.AttackerId);
                hash.Add(p.TargetEntityId);
                hash.Add(p.TargetPos);
                hash.Add(p.Pos);
            }
        }
    }
}
