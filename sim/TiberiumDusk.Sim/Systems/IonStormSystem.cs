using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;

namespace TiberiumDusk.Sim.Systems
{
    public enum StormPhase : byte
    {
        Calm = 0,
        Warning = 1,
        Active = 2,
    }

    /// <summary>
    /// Ion storms: scheduled with a warning phase, then a raging phase during
    /// which aircraft are destroyed (unless docked near a pad), superweapon
    /// charging pauses, and random lightning bolts strike the map.
    /// </summary>
    public sealed class IonStormSystem
    {
        private readonly World _world;
        private readonly SuperweaponSystem _superweapons;
        private readonly CombatSystem _combat;
        private readonly DeterministicRandom _random;
        private readonly bool _enabled;

        public StormPhase Phase { get; private set; }
        /// <summary>Ticks left in the current phase.</summary>
        public int PhaseTicksLeft { get; private set; }

        /// <summary>View hook for bolt strikes.</summary>
        public event System.Action<LeptonPos> Bolt;

        public IonStormSystem(World world, SuperweaponSystem superweapons, CombatSystem combat,
            DeterministicRandom random, bool enabled)
        {
            _world = world;
            _superweapons = superweapons;
            _combat = combat;
            _random = random;
            _enabled = enabled;
            if (enabled) ScheduleNext();
        }

        public bool IsActive => Phase == StormPhase.Active;

        private void ScheduleNext()
        {
            Phase = StormPhase.Calm;
            var special = _world.Rules.Special;
            PhaseTicksLeft = _random.Next(special.StormMinIntervalTicks, special.StormMaxIntervalTicks + 1);
        }

        public void Tick()
        {
            if (!_enabled) return;
            var special = _world.Rules.Special;

            if (--PhaseTicksLeft > 0)
            {
                if (Phase == StormPhase.Active) StormEffects(special);
                return;
            }

            switch (Phase)
            {
                case StormPhase.Calm:
                    Phase = StormPhase.Warning;
                    PhaseTicksLeft = special.StormWarningTicks;
                    break;
                case StormPhase.Warning:
                    Phase = StormPhase.Active;
                    PhaseTicksLeft = special.StormDurationTicks;
                    OnStormStart();
                    break;
                case StormPhase.Active:
                    _superweapons.ChargingPaused = false;
                    ScheduleNext();
                    break;
            }
        }

        private void OnStormStart()
        {
            _superweapons.ChargingPaused = true;

            // Aircraft caught in the sky are destroyed; those docked at a pad survive.
            foreach (var e in _world.Entities)
            {
                if (!e.Alive || !e.Spec.IsAircraft) continue;
                if (IsDockedAtPad(e)) continue;
                _world.Kill(e);
            }
        }

        private bool IsDockedAtPad(Entity aircraft)
        {
            foreach (var pad in _world.Entities)
            {
                if (!pad.Alive || pad.Owner != aircraft.Owner || !pad.Spec.IsAircraftPad) continue;
                if (aircraft.Pos.DistanceSquared(pad.Pos) <= 384L * 384L) return true;
            }
            return false;
        }

        private void StormEffects(SpecialRules special)
        {
            if (_world.Rules.Special.StormBoltEveryTicks <= 0) return;
            if (PhaseTicksLeft % special.StormBoltEveryTicks != 0) return;
            if (!_random.Chance(special.StormBoltChancePercent)) return;

            // 90% random cell, 10% aimed at a random entity (per original flavor).
            LeptonPos strike;
            var entities = _world.Entities;
            if (entities.Count > 0 && _random.Chance(10))
            {
                var victim = entities[_random.Next(0, entities.Count)];
                strike = victim.Pos;
            }
            else
            {
                strike = new LeptonPos(
                    _random.Next(0, _world.Map.Width * 256),
                    _random.Next(0, _world.Map.Height * 256));
            }

            _superweapons.AreaDamage(-1, strike, special.StormBoltDamage,
                special.StormBoltRadiusLeptons, "ion_blast");
            Bolt?.Invoke(strike);
        }

        public void AddToHash(ref StateHash hash)
        {
            hash.Add((int)Phase);
            hash.Add(PhaseTicksLeft);
        }
    }
}
