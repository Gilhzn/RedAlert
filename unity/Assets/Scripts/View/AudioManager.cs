using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Plays the procedural SFX pack (Assets/Resources/Audio) off sim events:
    /// weapon shots by type, deaths, superweapons, storm bolts, production
    /// chimes, and a base-under-attack alarm.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private GameRunner _runner;
        private AudioSource _source2d;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private readonly HashSet<int> _known = new HashSet<int>();
        private bool _structureReadyLastTick;
        private int _ownStructureHpLast;
        private float _alarmCooldownUntil;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _runner.WhenReady(InitAfterGame);
        }

        private void InitAfterGame()
        {
            _source2d = gameObject.AddComponent<AudioSource>();
            _source2d.spatialBlend = 0f;

            foreach (var name in new[]
            {
                "shot_cannon", "shot_rifle", "shot_laser", "shot_missile", "explosion",
                "explosion_big", "chime_ready", "build_place", "alarm", "click", "thunder", "ion_fire",
            })
            {
                var clip = Resources.Load<AudioClip>("Audio/" + name);
                if (clip != null) _clips[name] = clip;
            }

            _runner.Game.Combat.WeaponFired += OnWeaponFired;
            _runner.Game.Superweapons.Fired += OnSuperweapon;
            _runner.Game.IonStorm.Bolt += OnBolt;
            _runner.AfterTick += OnTick;
        }

        private void Play(string name, float volume = 0.5f, float minInterval = 0.05f)
        {
            if (!_clips.TryGetValue(name, out var clip)) return;
            if (_lastPlayed.TryGetValue(name, out float last) && Time.time - last < minInterval) return;
            _lastPlayed[name] = Time.time;
            _source2d.PlayOneShot(clip, volume);
        }

        private void OnWeaponFired(int weaponIndex, LeptonPos pos)
        {
            var weapon = _runner.Game.World.Rules.Weapons[weaponIndex];
            string clip =
                weapon.Id.Contains("laser") || weapon.Id.Contains("obelisk") || weapon.Id.Contains("plasma")
                    ? "shot_laser"
                : weapon.Id.Contains("missile") || weapon.Id.Contains("rocket") || weapon.Id.Contains("aa_")
                    ? "shot_missile"
                : weapon.Damage >= 60 ? "shot_cannon"
                : "shot_rifle";
            Play(clip, 0.25f, 0.06f);
        }

        private void OnSuperweapon(SuperweaponKind kind, LeptonPos pos)
        {
            Play("ion_fire", 0.7f);
            Play("explosion_big", 0.8f, 0.2f);
        }

        private void OnBolt(LeptonPos pos) => Play("thunder", 0.4f, 0.6f);

        private void OnTick()
        {
            var world = _runner.Game.World;

            // Death booms.
            foreach (var e in world.Entities)
            {
                if (e.Alive)
                {
                    _known.Add(e.Id);
                }
                else if (_known.Remove(e.Id))
                {
                    Play(e.Spec.IsStructure ? "explosion_big" : "explosion",
                        e.Spec.IsStructure ? 0.7f : 0.4f, 0.08f);
                }
            }

            // Construction-ready chime.
            var queue = _runner.Game.Production.GetQueue(
                GameRunner.LocalPlayerId, ProductionQueue.Structure);
            if (queue.ReadyForPlacement && !_structureReadyLastTick) Play("chime_ready", 0.6f);
            _structureReadyLastTick = queue.ReadyForPlacement;

            // Base-under-attack alarm: own structures losing HP.
            int hpSum = 0;
            foreach (var e in world.Entities)
            {
                if (e.Alive && e.Owner == GameRunner.LocalPlayerId && e.Spec.IsStructure) hpSum += e.Hp;
            }
            if (hpSum < _ownStructureHpLast && Time.time > _alarmCooldownUntil)
            {
                Play("alarm", 0.55f);
                _alarmCooldownUntil = Time.time + 12f;
            }
            _ownStructureHpLast = hpSum;
        }

        /// <summary>UI hooks.</summary>
        public void PlayClick() => Play("click", 0.4f);
        public void PlayPlace() => Play("build_place", 0.6f);
    }
}
