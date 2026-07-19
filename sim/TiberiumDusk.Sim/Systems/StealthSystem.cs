using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Recomputes cloak state each tick: self-cloaking units (stealth tank)
    /// and allied cloak-generator fields. Firing or taking fire sets
    /// RecloakTicks (by CombatSystem), which suppresses the cloak until it
    /// runs out. Cloaked entities cannot be targeted by enemies (Phase 6 adds
    /// sensor detection).
    /// </summary>
    public sealed class StealthSystem
    {
        private readonly World _world;

        public StealthSystem(World world)
        {
            _world = world;
        }

        public void Tick()
        {
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.Alive) continue;

                if (entity.RecloakTicks > 0)
                {
                    entity.RecloakTicks--;
                    entity.IsCloaked = false;
                    continue;
                }

                bool cloaked = entity.Spec.Cloakable;
                if (!cloaked) cloaked = InsideAlliedCloakField(entity);
                entity.IsCloaked = cloaked;
            }

            ComputeSensorDetection();
        }

        /// <summary>Sensors (radars, arrays) reveal cloaked enemies within radius.</summary>
        private void ComputeSensorDetection()
        {
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Alive) entities[i].DetectedMask = 0;
            }
            for (int i = 0; i < entities.Count; i++)
            {
                var sensor = entities[i];
                if (!sensor.Alive || sensor.Spec.SensorRadiusLeptons <= 0) continue;
                long radiusSq = (long)sensor.Spec.SensorRadiusLeptons * sensor.Spec.SensorRadiusLeptons;
                byte bit = (byte)(1 << sensor.Owner);
                for (int j = 0; j < entities.Count; j++)
                {
                    var other = entities[j];
                    if (!other.Alive || other.Owner == sensor.Owner) continue;
                    if (other.Pos.DistanceSquared(sensor.Pos) <= radiusSq) other.DetectedMask |= bit;
                }
            }
        }

        private bool InsideAlliedCloakField(Entity entity)
        {
            // Cloak generators do not cloak themselves (classic weakness).
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var generator = entities[i];
                if (!generator.Alive || generator.Id == entity.Id) continue;
                if (generator.Owner != entity.Owner) continue;
                int radius = generator.Spec.CloakGeneratorLeptons;
                if (radius <= 0) continue;
                // Generators need power to project the veil.
                if (_world.Players[generator.Owner].LowPower) continue;
                if (entity.Pos.DistanceSquared(generator.Pos) <= (long)radius * radius) return true;
            }
            return false;
        }
    }
}
