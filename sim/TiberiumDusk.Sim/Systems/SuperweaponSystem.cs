using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    /// <summary>
    /// Superweapon charge + firing. A power exists for a player while a
    /// granting structure is alive; charge accumulates each tick (paused on
    /// low power or ion storm), and a UseSuperweapon order at full charge
    /// executes the effect and resets the timer.
    /// </summary>
    public sealed class SuperweaponSystem
    {
        public sealed class PowerState
        {
            public int Charge;
            public bool Granted;
            public void AddToHash(ref StateHash hash)
            {
                hash.Add(Charge);
                hash.Add(Granted ? 1 : 0);
            }
        }

        /// <summary>An active hunter-seeker drone and its chosen victim.</summary>
        private struct SeekerMission
        {
            public int DroneId;
            public int TargetId;
            public int SuperweaponIndex;
        }

        private readonly World _world;
        private readonly MovementSystem _movement;
        private readonly CombatSystem _combat;
        private readonly DeterministicRandom _random;
        /// <summary>[player][superweapon]</summary>
        private readonly PowerState[][] _powers;
        private readonly List<SeekerMission> _seekers = new List<SeekerMission>();

        /// <summary>View hook: superweapon impacts for effects (deterministic; view-only consumption).</summary>
        public event System.Action<SuperweaponKind, LeptonPos> Fired;

        public bool ChargingPaused;   // set by IonStormSystem

        public SuperweaponSystem(World world, MovementSystem movement, CombatSystem combat,
            DeterministicRandom random)
        {
            _world = world;
            _movement = movement;
            _combat = combat;
            _random = random;
            _powers = new PowerState[World.MaxPlayers][];
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                _powers[p] = new PowerState[world.Rules.Superweapons.Length];
                for (int s = 0; s < _powers[p].Length; s++) _powers[p][s] = new PowerState();
            }
        }

        public PowerState GetPower(int playerId, int superweaponIndex) => _powers[playerId][superweaponIndex];

        public void Tick()
        {
            var superweapons = _world.Rules.Superweapons;
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                var player = _world.Players[p];
                for (int s = 0; s < superweapons.Length; s++)
                {
                    var power = _powers[p][s];
                    power.Granted = HasGrantingStructure(p, superweapons[s].GrantedBy);
                    if (!power.Granted)
                    {
                        power.Charge = 0;
                        continue;
                    }
                    if (ChargingPaused || player.LowPower) continue;
                    if (power.Charge < superweapons[s].ChargeTicks) power.Charge++;
                }
            }

            StepSeekers();
        }

        private bool HasGrantingStructure(int playerId, string grantedBy)
        {
            if (grantedBy.StartsWith("any:"))
            {
                foreach (var option in grantedBy.Substring(4).Split('|'))
                {
                    if (_world.OwnsBlueprint(playerId, option)) return true;
                }
                return false;
            }
            return _world.OwnsBlueprint(playerId, grantedBy);
        }

        public void TryFire(int playerId, int superweaponIndex, LeptonPos target)
        {
            if (superweaponIndex < 0 || superweaponIndex >= _world.Rules.Superweapons.Length) return;
            var spec = _world.Rules.Superweapons[superweaponIndex];
            var power = _powers[playerId][superweaponIndex];
            // Check the granting structure live (orders execute before this tick's charge pass).
            if (!HasGrantingStructure(playerId, spec.GrantedBy)) return;
            if (power.Charge < spec.ChargeTicks || ChargingPaused) return;

            switch (spec.Kind)
            {
                case SuperweaponKind.IonCannon:
                    FireIonCannon(playerId, spec, target);
                    break;
                case SuperweaponKind.ClusterMissile:
                    FireClusterMissile(playerId, spec, target);
                    break;
                case SuperweaponKind.EmpBlast:
                    if (!FireEmp(playerId, spec, target)) return;   // out of cannon range — keep charge
                    break;
                case SuperweaponKind.HunterSeeker:
                    if (!LaunchSeeker(playerId, spec)) return;      // no valid target — keep charge
                    break;
            }

            power.Charge = 0;
            Fired?.Invoke(spec.Kind, target);
        }

        private void FireIonCannon(int playerId, SuperweaponSpec spec, LeptonPos target)
        {
            AreaDamage(playerId, target, spec.Damage, spec.RadiusLeptons, "ion_blast");
        }

        private void FireClusterMissile(int playerId, SuperweaponSpec spec, LeptonPos target)
        {
            // One warhead at center + a ring of scattered bomblets.
            AreaDamage(playerId, target, spec.Damage, spec.RadiusLeptons / 2, "cluster_he");
            for (int i = 1; i < spec.ClusterCount; i++)
            {
                var offset = new LeptonPos(
                    target.X + _random.Next(-spec.RadiusLeptons, spec.RadiusLeptons + 1),
                    target.Y + _random.Next(-spec.RadiusLeptons, spec.RadiusLeptons + 1));
                AreaDamage(playerId, offset, spec.Damage, spec.RadiusLeptons / 3, "cluster_he");
            }
        }

        private bool FireEmp(int playerId, SuperweaponSpec spec, LeptonPos target)
        {
            // Must be within range of one of the player's EMP cannons.
            bool inRange = false;
            foreach (var e in _world.Entities)
            {
                if (!e.Alive || e.Owner != playerId || e.Spec.Id != "nx_emp_cannon") continue;
                if (e.Pos.DistanceSquared(target) <= (long)spec.RangeLeptons * spec.RangeLeptons)
                {
                    inRange = true;
                    break;
                }
            }
            if (!inRange) return false;

            long radiusSq = (long)spec.RadiusLeptons * spec.RadiusLeptons;
            foreach (var victim in _world.Entities)
            {
                if (!victim.Alive) continue;
                if (victim.Pos.DistanceSquared(target) > radiusSq) continue;
                bool mechanical = victim.Spec.IsStructure || victim.Spec.IsCyborg
                    || (victim.Spec.Mobile != null && victim.Spec.Mobile.Locomotor != LocomotorId.Foot);
                if (!mechanical) continue;
                if (spec.DurationTicks > victim.DisabledTicks) victim.DisabledTicks = spec.DurationTicks;
            }
            return true;
        }

        private bool LaunchSeeker(int playerId, SuperweaponSpec spec)
        {
            var target = PickSeekerTarget(playerId);
            if (target == null) return false;

            // Emerge near any own structure (or map corner as fallback).
            var origin = new CellPos(1, 1);
            foreach (var e in _world.Entities)
            {
                if (e.Alive && e.Owner == playerId && e.Spec.IsStructure)
                {
                    origin = e.HomeCell;
                    break;
                }
            }

            var drone = _world.Spawn(_world.Rules.Unit(spec.DroneUnit), playerId, origin);
            _seekers.Add(new SeekerMission
            {
                DroneId = drone.Id,
                TargetId = target.Id,
                SuperweaponIndex = spec.Index,
            });
            return true;
        }

        /// <summary>High-value target: prefers enemy structures, falls back to units.</summary>
        private Entity PickSeekerTarget(int playerId)
        {
            var structures = new List<Entity>();
            var units = new List<Entity>();
            foreach (var e in _world.Entities)
            {
                if (!e.Alive || e.Owner == playerId) continue;
                if (e.Spec.IsStructure) structures.Add(e);
                else units.Add(e);
            }
            var pool = structures.Count > 0 ? structures : units;
            if (pool.Count == 0) return null;
            return pool[_random.Next(0, pool.Count)];
        }

        private void StepSeekers()
        {
            for (int i = _seekers.Count - 1; i >= 0; i--)
            {
                var mission = _seekers[i];
                var drone = _world.GetEntity(mission.DroneId);
                if (drone == null)
                {
                    _seekers.RemoveAt(i);
                    continue;
                }

                var target = _world.GetEntity(mission.TargetId);
                if (target == null)
                {
                    target = PickSeekerTarget(drone.Owner);
                    if (target == null)
                    {
                        _world.Kill(drone);
                        _seekers.RemoveAt(i);
                        continue;
                    }
                    mission.TargetId = target.Id;
                    _seekers[i] = mission;
                }

                long distSq = drone.Pos.DistanceSquared(target.Pos);
                if (distSq <= 192L * 192L)
                {
                    var spec = _world.Rules.Superweapons[mission.SuperweaponIndex];
                    AreaDamage(drone.Owner, target.Pos, spec.Damage, 256, "seeker_blast");
                    _world.Kill(drone);
                    _seekers.RemoveAt(i);
                    Fired?.Invoke(SuperweaponKind.HunterSeeker, target.Pos);
                }
                else if (drone.Move.Mode == MoveMode.None || !drone.Move.Target.Equals(target.HomeCell))
                {
                    _movement.OrderMovePath(drone, target.HomeCell);
                }
            }
        }

        /// <summary>Area damage helper using a synthesized weapon over a named warhead.</summary>
        public void AreaDamage(int playerId, LeptonPos center, int damage, int radiusLeptons, string warheadId)
        {
            int warheadIndex = -1;
            for (int i = 0; i < _world.Rules.Warheads.Length; i++)
            {
                if (_world.Rules.Warheads[i].Id == warheadId) warheadIndex = i;
            }
            if (warheadIndex < 0) return;

            var weapon = new WeaponSpec
            {
                Id = "__superweapon",
                Index = -1,
                Damage = damage,
                WarheadIndex = warheadIndex,
            };
            // Direct-hit every entity in radius (full damage), spread handles the rest.
            long radiusSq = (long)radiusLeptons * radiusLeptons;
            var direct = new List<int>();
            foreach (var e in _world.Entities)
            {
                if (e.Alive && e.Pos.DistanceSquared(center) <= radiusSq) direct.Add(e.Id);
            }
            foreach (var id in direct)
            {
                _combat.ApplyDamage(weapon, attackerId: -1, directTargetId: id,
                    _world.GetEntity(id)?.Pos ?? center);
            }
            // Plus falloff around the center for anything just outside.
            _combat.ApplyDamage(weapon, attackerId: -1, directTargetId: -1, center);
        }

        public void AddToHash(ref StateHash hash)
        {
            for (int p = 0; p < World.MaxPlayers; p++)
            {
                for (int s = 0; s < _powers[p].Length; s++) _powers[p][s].AddToHash(ref hash);
            }
            hash.Add(_seekers.Count);
            for (int i = 0; i < _seekers.Count; i++)
            {
                hash.Add(_seekers[i].DroneId);
                hash.Add(_seekers[i].TargetId);
            }
        }
    }
}
