using System.Collections.Generic;
using TiberiumDusk.Sim.WorldModel;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Combat presentation: in-flight projectile tracers, death explosions,
    /// and HP bars. All purely cosmetic — reads sim state, never writes.
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        private GameRunner _runner;
        private UnitViewManager _units;
        private Camera _camera;
        private Material _tracerMaterial;

        private readonly Dictionary<int, GameObject> _tracers = new Dictionary<int, GameObject>();
        private readonly List<int> _gone = new List<int>();
        private readonly List<(GameObject go, float age)> _explosions = new List<(GameObject, float)>();
        private readonly HashSet<int> _knownEntities = new HashSet<int>();

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _runner.WhenReady(InitAfterGame);
        }

        private void InitAfterGame()
        {
            _units = FindFirstObjectByType<UnitViewManager>();
            _camera = Camera.main;
            _tracerMaterial = MaterialFactory.Emissive(new Color(1f, 0.6f, 0.2f), new Color(1f, 0.75f, 0.3f));
            _runner.AfterTick += OnTick;
        }

        private void OnDestroy()
        {
            if (_runner != null) _runner.AfterTick -= OnTick;
        }

        private void OnTick()
        {
            SyncTracers();
            DetectDeaths();
        }

        private void SyncTracers()
        {
            var projectiles = _runner.Game.World.Projectiles;
            var seen = new HashSet<int>();
            for (int i = 0; i < projectiles.Count; i++)
            {
                var p = projectiles[i];
                seen.Add(p.Id);
                if (!_tracers.TryGetValue(p.Id, out var go))
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                    go.GetComponent<MeshRenderer>().sharedMaterial = _tracerMaterial;
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(transform, worldPositionStays: false);
                    _tracers.Add(p.Id, go);
                }
                var world = TerrainView.LeptonToWorld(p.Pos);
                world.y = _runner.Terrain.SurfaceHeight(world.x, world.z) + 0.45f;
                go.transform.position = world;
            }

            _gone.Clear();
            foreach (var pair in _tracers)
            {
                if (!seen.Contains(pair.Key)) _gone.Add(pair.Key);
            }
            foreach (var id in _gone)
            {
                SpawnExplosion(_tracers[id].transform.position, 0.5f);
                Destroy(_tracers[id]);
                _tracers.Remove(id);
            }
        }

        private void DetectDeaths()
        {
            var entities = _runner.Game.World.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (e.Alive)
                {
                    _knownEntities.Add(e.Id);
                }
                else if (_knownEntities.Remove(e.Id))
                {
                    var world = TerrainView.LeptonToWorld(e.Pos);
                    world.y = _runner.Terrain.SurfaceHeight(world.x, world.z) + 0.3f;
                    SpawnExplosion(world, e.Spec.IsStructure ? 2.2f : 1.0f);
                }
            }
        }

        private void SpawnExplosion(Vector3 position, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f) * size;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Emissive(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.6f, 0.15f));
            go.transform.SetParent(transform, worldPositionStays: true);
            _explosions.Add((go, size));
        }

        private void Update()
        {
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                var (go, size) = _explosions[i];
                float scale = go.transform.localScale.x + Time.deltaTime * 3.5f * size;
                go.transform.localScale = new Vector3(scale, scale, scale);
                if (scale > size)
                {
                    Destroy(go);
                    _explosions.RemoveAt(i);
                }
            }
        }

        private void OnGUI()
        {
            if (_runner == null || !_runner.Ready) return;
            // HP bars over damaged or selected units.
            var world = _runner.Game.World;
            foreach (var pair in _units.Views)
            {
                var entity = world.GetEntity(pair.Key);
                if (entity == null || entity.Hp >= entity.Spec.Health.Max) continue;

                var screen = _camera.WorldToScreenPoint(pair.Value.transform.position);
                if (screen.z <= 0f) continue;

                float width = entity.Spec.IsStructure ? 46f : 26f;
                var back = new Rect(screen.x - width / 2, Screen.height - screen.y - 26f, width, 4f);
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(back, Texture2D.whiteTexture);

                float ratio = Mathf.Clamp01((float)entity.Hp / entity.Spec.Health.Max);
                GUI.color = ratio > 0.5f ? new Color(0.25f, 0.95f, 0.3f)
                    : ratio > 0.25f ? new Color(0.95f, 0.85f, 0.2f)
                    : new Color(0.95f, 0.25f, 0.2f);
                GUI.DrawTexture(new Rect(back.x, back.y, back.width * ratio, back.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
    }
}
