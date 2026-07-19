using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Aircraft logistics: when out of ammo (or idle and not full) fly to the
    /// nearest allied landing pad and rearm one ammo point per ReloadTicksPerAmmo.
    /// </summary>
    public sealed class AircraftSystem
    {
        private readonly World _world;
        private readonly MovementSystem _movement;
        /// <summary>Reload progress per entity id (transient timers, hashed via Ammo changes).</summary>
        private readonly System.Collections.Generic.Dictionary<int, int> _reloadTimers =
            new System.Collections.Generic.Dictionary<int, int>();

        public AircraftSystem(World world, MovementSystem movement)
        {
            _world = world;
            _movement = movement;
        }

        public void Tick()
        {
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.Alive || entity.Spec.AircraftAmmo <= 0) continue;
                Step(entity);
            }
        }

        private void Step(Entity entity)
        {
            if (entity.Ammo >= entity.Spec.AircraftAmmo)
            {
                _reloadTimers.Remove(entity.Id);
                return;
            }

            // Busy fighting with remaining ammo — let it fight.
            if (entity.Ammo > 0 && entity.AttackTargetId != -1) return;

            var pad = FindNearestPad(entity);
            if (pad == null) return;

            var padCell = new CellPos(pad.HomeCell.X, pad.HomeCell.Y);
            long distSq = entity.Pos.DistanceSquared(pad.Pos);
            const long dockRangeSq = 384L * 384L;   // 1.5 cells

            if (distSq > dockRangeSq)
            {
                if (entity.Move.Mode == MoveMode.None)
                {
                    _movement.OrderMovePath(entity, padCell);
                }
                _reloadTimers.Remove(entity.Id);
                return;
            }

            // Hovering over the pad: rearm.
            _reloadTimers.TryGetValue(entity.Id, out int timer);
            timer++;
            if (timer >= _world.Rules.Combat.ReloadTicksPerAmmo)
            {
                timer = 0;
                entity.Ammo++;
            }
            _reloadTimers[entity.Id] = timer;
        }

        private Entity FindNearestPad(Entity aircraft)
        {
            Entity best = null;
            long bestDist = long.MaxValue;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (!e.Alive || e.Owner != aircraft.Owner || !e.Spec.IsAircraftPad) continue;
                long distSq = aircraft.Pos.DistanceSquared(e.Pos);
                if (distSq < bestDist)
                {
                    bestDist = distSq;
                    best = e;
                }
            }
            return best;
        }
    }
}
