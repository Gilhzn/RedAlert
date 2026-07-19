using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Targeting, turrets, firing, projectiles, damage (armor Verses + spread
    /// falloff), EMP paralysis, veterancy, engineer capture. Deaths go through
    /// World.Kill; the killer is credited with XP.
    /// </summary>
    public sealed class CombatSystem
    {
        private readonly World _world;
        private readonly MovementSystem _movement;

        public CombatSystem(World world, MovementSystem movement)
        {
            _world = world;
            _movement = movement;
        }

        // ---------- Orders ----------

        public void OrderAttack(Entity attacker, Entity target)
        {
            if (attacker.Spec.WeaponIndex < 0) return;
            attacker.AttackTargetId = target.Id;
            attacker.AttackMovePending = false;
        }

        public void OrderAttackMove(Entity entity, CellPos destination)
        {
            entity.AttackMoveDest = destination;
            entity.AttackMovePending = true;
            entity.AttackTargetId = -1;
            if (entity.Spec.Mobile != null) _movement.OrderMovePath(entity, destination);
        }

        public void OrderCapture(Entity engineer, Entity target)
        {
            if (!engineer.Spec.CanCapture || !target.Spec.IsStructure) return;
            engineer.CaptureTargetId = target.Id;
            _movement.OrderMovePath(engineer, target.HomeCell);
        }

        public void ClearCombatOrders(Entity entity)
        {
            entity.AttackTargetId = -1;
            entity.AttackMovePending = false;
            entity.CaptureTargetId = -1;
        }

        // ---------- Tick ----------

        public void Tick()
        {
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.Alive) continue;

                if (entity.DisabledTicks > 0)
                {
                    entity.DisabledTicks--;
                    if (entity.Spec.Mobile != null && entity.Move.Mode != MoveMode.None)
                        _movement.OrderStop(entity);
                    continue;
                }

                if (entity.WeaponCooldown > 0) entity.WeaponCooldown--;

                if (entity.CaptureTargetId != -1)
                {
                    StepCapture(entity);
                    continue;
                }

                if (entity.Spec.WeaponIndex >= 0) StepCombat(entity);
            }

            StepProjectiles();
        }

        private void StepCombat(Entity entity)
        {
            var target = _world.GetEntity(entity.AttackTargetId);

            // Auto-acquire when idle/attack-moving and unengaged.
            if (target == null)
            {
                entity.AttackTargetId = -1;
                if (entity.AutoEngage && (entity.Move.Mode == MoveMode.None || entity.AttackMovePending))
                {
                    target = FindTargetInSight(entity);
                    if (target != null) entity.AttackTargetId = target.Id;
                }
                if (target == null)
                {
                    ResumeAttackMove(entity);
                    return;
                }
            }

            var weapon = _world.Rules.Weapons[entity.Spec.WeaponIndex];
            long rangeSq = (long)weapon.RangeLeptons * weapon.RangeLeptons;
            long distSq = entity.Pos.DistanceSquared(TargetAimPos(target));

            if (distSq > rangeSq)
            {
                // Chase (mobile units only). Structures just wait for range.
                if (entity.Spec.Mobile != null && entity.Move.Mode == MoveMode.None)
                {
                    _movement.OrderMovePath(entity, target.HomeCell);
                }
                // Lost far beyond sight → drop target.
                long sightSq = (long)entity.Spec.SightLeptons * entity.Spec.SightLeptons;
                if (distSq > sightSq * 4) entity.AttackTargetId = -1;
                return;
            }

            // In range: stop and engage.
            if (entity.Spec.Mobile != null && entity.Move.Mode != MoveMode.None)
            {
                _movement.OrderStop(entity);
            }

            // Face the target: turret if present, else hull.
            var aim = TargetAimPos(target);
            byte desired = Facing.FromVector(aim.X - entity.Pos.X, aim.Y - entity.Pos.Y, entity.FacingValue);
            int rot = entity.Spec.Turreted
                ? (entity.Spec.TurretRot > 0 ? entity.Spec.TurretRot : 6)
                : (entity.Spec.Mobile?.Rot ?? 8);
            if (entity.Spec.Turreted)
            {
                entity.TurretFacing = Facing.TurnToward(entity.TurretFacing, desired, rot);
                if (System.Math.Abs(Facing.Difference(entity.TurretFacing, desired)) > 6) return;
            }
            else if (entity.Spec.Mobile != null)
            {
                entity.FacingValue = Facing.TurnToward(entity.FacingValue, desired, rot);
                if (System.Math.Abs(Facing.Difference(entity.FacingValue, desired)) > 6) return;
            }

            if (entity.WeaponCooldown > 0) return;
            entity.WeaponCooldown = weapon.Rof;

            if (weapon.Projectile == ProjectileKind.Instant)
            {
                ApplyDamage(weapon, entity.Id, target.Id, aim);
            }
            else
            {
                _world.Projectiles.Spawn(weapon.Index, entity.Id, target.Id, aim, entity.Pos);
            }
        }

        private void ResumeAttackMove(Entity entity)
        {
            if (entity.AttackMovePending && entity.Spec.Mobile != null && entity.Move.Mode == MoveMode.None)
            {
                if (entity.HomeCell.Equals(entity.AttackMoveDest))
                {
                    entity.AttackMovePending = false;
                }
                else
                {
                    _movement.OrderMovePath(entity, entity.AttackMoveDest);
                }
            }
        }

        private Entity FindTargetInSight(Entity entity)
        {
            long sightSq = (long)entity.Spec.SightLeptons * entity.Spec.SightLeptons;
            Entity best = null;
            long bestDist = long.MaxValue;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var candidate = entities[i];
                if (!candidate.Alive || candidate.Owner == entity.Owner) continue;
                long distSq = entity.Pos.DistanceSquared(TargetAimPos(candidate));
                if (distSq <= sightSq && distSq < bestDist)
                {
                    bestDist = distSq;
                    best = candidate;
                }
            }
            return best;
        }

        private static LeptonPos TargetAimPos(Entity target) => target.Pos;

        private void StepCapture(Entity engineer)
        {
            var target = _world.GetEntity(engineer.CaptureTargetId);
            if (target == null || target.Owner == engineer.Owner)
            {
                engineer.CaptureTargetId = -1;
                return;
            }

            // Adjacent to the footprint → capture (engineer consumed, classic TS).
            var s = target.Spec.Structure;
            int dx = System.Math.Max(target.HomeCell.X - engineer.HomeCell.X,
                engineer.HomeCell.X - (target.HomeCell.X + s.FootprintW - 1));
            int dy = System.Math.Max(target.HomeCell.Y - engineer.HomeCell.Y,
                engineer.HomeCell.Y - (target.HomeCell.Y + s.FootprintH - 1));
            if (System.Math.Max(dx, dy) <= 1)
            {
                target.Owner = engineer.Owner;
                target.Hp = target.Spec.Health.Max;   // engineer restores it, classic
                _world.RecomputePlayerAggregates();
                _world.Kill(engineer);
            }
            else if (engineer.Move.Mode == MoveMode.None)
            {
                _movement.OrderMovePath(engineer, target.HomeCell);
            }
        }

        // ---------- Projectiles & damage ----------

        private void StepProjectiles()
        {
            var projectiles = _world.Projectiles;
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var p = projectiles[i];
                var weapon = _world.Rules.Weapons[p.WeaponIndex];

                // Homing: chase the target's live position.
                var target = _world.GetEntity(p.TargetEntityId);
                if (target != null) p.TargetPos = target.Pos;

                int dx = p.TargetPos.X - p.Pos.X;
                int dy = p.TargetPos.Y - p.Pos.Y;
                long dist = LeptonPos.IntSqrt((long)dx * dx + (long)dy * dy);

                if (dist <= weapon.ProjectileSpeed)
                {
                    projectiles.RemoveAt(i);
                    ApplyDamage(weapon, p.AttackerId, target?.Id ?? -1, p.TargetPos);
                }
                else
                {
                    p.Pos = new LeptonPos(
                        p.Pos.X + (int)(dx * weapon.ProjectileSpeed / dist),
                        p.Pos.Y + (int)(dy * weapon.ProjectileSpeed / dist));
                    projectiles.Update(i, p);
                }
            }
        }

        public void ApplyDamage(WeaponSpec weapon, int attackerId, int directTargetId, LeptonPos impact)
        {
            var warhead = _world.Rules.Warheads[weapon.WarheadIndex];
            var attacker = _world.GetEntity(attackerId);

            if (warhead.EmpEffect)
            {
                ApplyEmp(weapon, warhead, impact);
                return;
            }

            int baseDamage = weapon.Damage;
            if (attacker != null && attacker.Rank > 0)
            {
                baseDamage += baseDamage * _world.Rules.Combat.VeteranDamagePercent * attacker.Rank / 100;
            }

            int spreadLeptons = warhead.Spread * _world.Rules.Combat.SpreadStepLeptons;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var victim = entities[i];
                if (!victim.Alive) continue;

                long distSq = victim.Pos.DistanceSquared(impact);
                bool direct = victim.Id == directTargetId;
                if (!direct && spreadLeptons <= 0) continue;
                if (!direct && distSq > (long)spreadLeptons * spreadLeptons) continue;

                int damage = baseDamage;
                if (!direct)
                {
                    // Halve per spread step from the impact point.
                    int steps = (int)(LeptonPos.IntSqrt(distSq) / _world.Rules.Combat.SpreadStepLeptons);
                    for (int s = 0; s < steps && damage > 0; s++) damage /= 2;
                }

                damage = damage * warhead.Verses[(int)victim.Spec.Health.Armor] / 100;
                if (victim.Rank > 0)
                {
                    damage = damage * 100 / (100 + _world.Rules.Combat.VeteranArmorPercent * victim.Rank);
                }
                if (damage <= 0) continue;

                victim.Hp -= damage;
                if (victim.Hp <= 0)
                {
                    OnKill(attacker, victim);
                }
            }
        }

        private void ApplyEmp(WeaponSpec weapon, WarheadSpec warhead, LeptonPos impact)
        {
            int radius = warhead.Spread * _world.Rules.Combat.SpreadStepLeptons;
            long radiusSq = (long)radius * radius;
            var entities = _world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var victim = entities[i];
                if (!victim.Alive) continue;
                if (victim.Pos.DistanceSquared(impact) > radiusSq) continue;

                // EMP hits machines: vehicles and structures. Organic infantry immune.
                bool mechanical = victim.Spec.IsStructure
                    || (victim.Spec.Mobile != null && victim.Spec.Mobile.Locomotor != LocomotorId.Foot);
                if (!mechanical) continue;

                if (weapon.Damage > victim.DisabledTicks) victim.DisabledTicks = weapon.Damage;
            }
        }

        private void OnKill(Entity attacker, Entity victim)
        {
            if (attacker != null && attacker.Alive && attacker.Owner != victim.Owner)
            {
                int value = victim.Spec.Buildable?.Cost ?? 0;
                attacker.CombatXp += value;
                int ownCost = attacker.Spec.Buildable?.Cost ?? 1000;
                long threshold = (long)ownCost * _world.Rules.Combat.VeteranRatioPercent / 100;
                int newRank = threshold > 0 ? (int)System.Math.Min(attacker.CombatXp / threshold,
                    _world.Rules.Combat.VeteranMaxRank) : 0;
                if (newRank > attacker.Rank) attacker.Rank = newRank;
            }
            _world.Kill(victim);
        }
    }
}
