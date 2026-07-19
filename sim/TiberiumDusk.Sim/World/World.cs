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
        public const int MaxPlayers = 8;

        public readonly MapData Map;
        public readonly RulesData Rules;
        public readonly CrystalField Crystal;
        public readonly PlayerState[] Players;

        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Dictionary<int, Entity> _byId = new Dictionary<int, Entity>();
        /// <summary>entityId per cell, or -1. Ground occupancy: one unit per cell (infantry sub-cells arrive later).</summary>
        private readonly int[] _occupancy;

        private int _nextEntityId = 1;

        public World(MapData map, RulesData rules)
        {
            Map = map;
            Rules = rules;
            Crystal = new CrystalField(map);
            Players = new PlayerState[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                Players[i] = new PlayerState
                {
                    PlayerId = i,
                    Credits = rules.Economy.StartingCredits,
                };
            }
            RecomputePlayerAggregates();
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
            if (spec.IsStructure) return SpawnStructure(spec, owner, cell);

            var entity = new Entity
            {
                Id = _nextEntityId++,
                Spec = spec,
                Owner = owner,
                Pos = LeptonPos.CellCenter(cell),
                Hp = spec.Health.Max,
                HomeCell = cell,
            };
            if (spec.Harvester != null) entity.Harvest = new HarvesterState();
            _entities.Add(entity);
            _byId.Add(entity.Id, entity);
            ClaimCell(cell, entity.Id);
            return entity;
        }

        /// <summary>Places a structure at footprint origin (does NOT validate — use PlacementValidator first).</summary>
        public Entity SpawnStructure(UnitSpec spec, int owner, CellPos origin)
        {
            var s = spec.Structure;
            var entity = new Entity
            {
                Id = _nextEntityId++,
                Spec = spec,
                Owner = owner,
                Pos = new LeptonPos(
                    origin.X * LeptonPos.LeptonsPerCell + s.FootprintW * LeptonPos.LeptonsPerCell / 2,
                    origin.Y * LeptonPos.LeptonsPerCell + s.FootprintH * LeptonPos.LeptonsPerCell / 2),
                Hp = spec.Health.Max,
                HomeCell = origin,
            };
            _entities.Add(entity);
            _byId.Add(entity.Id, entity);
            for (int dy = 0; dy < s.FootprintH; dy++)
                for (int dx = 0; dx < s.FootprintW; dx++)
                    ClaimCell(new CellPos(origin.X + dx, origin.Y + dy), entity.Id);
            RecomputePlayerAggregates();
            return entity;
        }

        public void Kill(Entity entity)
        {
            if (!entity.Alive) return;
            entity.Alive = false;
            if (entity.Spec.IsStructure)
            {
                var s = entity.Spec.Structure;
                for (int dy = 0; dy < s.FootprintH; dy++)
                    for (int dx = 0; dx < s.FootprintW; dx++)
                        ReleaseCell(new CellPos(entity.HomeCell.X + dx, entity.HomeCell.Y + dy), entity.Id);
                RecomputePlayerAggregates();
            }
            else
            {
                ReleaseCell(entity.HomeCell, entity.Id);
                if (entity.Move.HasClaim) ReleaseCell(entity.Move.ClaimedCell, entity.Id);
            }
            entity.Move.Clear();
        }

        /// <summary>Recomputes power and storage capacity from owned structures (call after spawn/kill/capture).</summary>
        public void RecomputePlayerAggregates()
        {
            foreach (var player in Players)
            {
                player.PowerProduced = 0;
                player.PowerDrained = 0;
                player.StorageCapacityCredits = Rules.Economy.StartingCredits;
            }
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (!e.Alive || !e.Spec.IsStructure) continue;
                var player = Players[e.Owner];
                int power = e.Spec.Structure.Power;
                if (power >= 0) player.PowerProduced += power;
                else player.PowerDrained -= power;
                player.StorageCapacityCredits += e.Spec.StorageBails * Rules.Economy.GreenBailValue;
            }
        }

        /// <summary>Does the player own a living structure/unit of this blueprint? (prerequisite checks)</summary>
        public bool OwnsBlueprint(int playerId, string blueprintId)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e.Alive && e.Owner == playerId && e.Spec.Id == blueprintId) return true;
            }
            return false;
        }

        /// <summary>First alive structure of the player that hosts the given production queue; null if none.</summary>
        public Entity FindPrimaryFactory(int playerId, ProductionQueue queue)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (!e.Alive || e.Owner != playerId || !e.Spec.IsStructure) continue;
                if (ProducesQueue(e.Spec, queue)) return e;
            }
            return null;
        }

        public static bool ProducesQueue(UnitSpec spec, ProductionQueue queue) =>
            spec.ProductionQueues != null && System.Array.IndexOf(spec.ProductionQueues, queue) >= 0;

        public void AddToHash(ref StateHash hash)
        {
            hash.Add(_nextEntityId);
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Alive) _entities[i].AddToHash(ref hash);
            }
            foreach (var player in Players) player.AddToHash(ref hash);
            Crystal.AddToHash(ref hash);
        }
    }
}
