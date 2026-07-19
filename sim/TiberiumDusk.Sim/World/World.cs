using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Map;
using TiberiumDusk.Sim.Math;

namespace TiberiumDusk.Sim.WorldModel
{
    /// <summary>
    /// All mutable simulation state: entities + occupancy. Iteration is always
    /// in entity-id order (the list is append-only and ids are sequential).
    /// </summary>
    public sealed class World
    {
        public readonly MapData Map;
        public readonly RulesData Rules;

        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Dictionary<int, Entity> _byId = new Dictionary<int, Entity>();
        /// <summary>entityId per cell, or -1. Ground occupancy: one unit per cell (infantry sub-cells arrive later).</summary>
        private readonly int[] _occupancy;

        private int _nextEntityId = 1;

        public World(MapData map, RulesData rules)
        {
            Map = map;
            Rules = rules;
            _occupancy = new int[map.Width * map.Height];
            for (int i = 0; i < _occupancy.Length; i++) _occupancy[i] = -1;
        }

        public IReadOnlyList<Entity> Entities => _entities;

        public Entity GetEntity(int id) => _byId.TryGetValue(id, out var e) && e.Alive ? e : null;

        public int OccupantOf(CellPos cell) => _occupancy[Map.CellIndex(cell)];

        public bool IsCellFreeFor(CellPos cell, int entityId)
        {
            int occupant = _occupancy[Map.CellIndex(cell)];
            return occupant == -1 || occupant == entityId;
        }

        public void ClaimCell(CellPos cell, int entityId) => _occupancy[Map.CellIndex(cell)] = entityId;

        public void ReleaseCell(CellPos cell, int entityId)
        {
            int idx = Map.CellIndex(cell);
            if (_occupancy[idx] == entityId) _occupancy[idx] = -1;
        }

        public Entity Spawn(UnitSpec spec, int owner, CellPos cell)
        {
            var entity = new Entity
            {
                Id = _nextEntityId++,
                Spec = spec,
                Owner = owner,
                Pos = LeptonPos.CellCenter(cell),
                Hp = spec.Health.Max,
                HomeCell = cell,
            };
            _entities.Add(entity);
            _byId.Add(entity.Id, entity);
            ClaimCell(cell, entity.Id);
            return entity;
        }

        public void Kill(Entity entity)
        {
            if (!entity.Alive) return;
            entity.Alive = false;
            ReleaseCell(entity.HomeCell, entity.Id);
            if (entity.Move.HasClaim) ReleaseCell(entity.Move.ClaimedCell, entity.Id);
            entity.Move.Clear();
        }

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(_nextEntityId);
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Alive) _entities[i].AddToHash(ref hash);
            }
        }
    }
}
