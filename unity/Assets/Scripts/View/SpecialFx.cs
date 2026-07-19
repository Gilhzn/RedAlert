using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Phase 6 effects: superweapon impact flashes, ion-storm lightning bolts,
    /// and crate boxes on the map. Purely cosmetic, driven by sim events.
    /// </summary>
    public sealed class SpecialFx : MonoBehaviour
    {
        private GameRunner _runner;
        private CombatFx _combatFx;
        private Material _boltMaterial;
        private Material _crateMaterial;
        private readonly Dictionary<(int, int), GameObject> _crateViews =
            new Dictionary<(int, int), GameObject>();
        private readonly List<(GameObject go, float life)> _bolts = new List<(GameObject, float)>();

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _runner.WhenReady(InitAfterGame);
        }

        private void InitAfterGame()
        {
            _combatFx = FindFirstObjectByType<CombatFx>();
            _boltMaterial = MaterialFactory.Unlit(new Color(0.75f, 0.9f, 1f));
            _crateMaterial = MaterialFactory.Emissive(new Color(0.5f, 0.42f, 0.2f), new Color(0.9f, 0.75f, 0.3f));

            _runner.Game.Superweapons.Fired += OnSuperweaponFired;
            _runner.Game.IonStorm.Bolt += OnBolt;
            _runner.Game.Crates.CrateSpawned += OnCrateSpawned;
            _runner.Game.Crates.CratePicked += OnCratePicked;
        }

        private void OnSuperweaponFired(SuperweaponKind kind, LeptonPos pos)
        {
            var world = ToWorld(pos);
            // A column of light for the ion cannon, big blasts for the rest.
            if (kind == SuperweaponKind.IonCannon)
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(beam.GetComponent<Collider>());
                beam.transform.position = world + new Vector3(0f, 12f, 0f);
                beam.transform.localScale = new Vector3(1.4f, 12f, 1.4f);
                beam.GetComponent<MeshRenderer>().sharedMaterial =
                    MaterialFactory.Unlit(new Color(0.6f, 0.9f, 1f));
                _bolts.Add((beam, 0.6f));
            }
        }

        private void OnBolt(LeptonPos pos)
        {
            var world = ToWorld(pos);
            var bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(bolt.GetComponent<Collider>());
            bolt.transform.position = world + new Vector3(0f, 8f, 0f);
            bolt.transform.localScale = new Vector3(0.15f, 8f, 0.15f);
            bolt.GetComponent<MeshRenderer>().sharedMaterial = _boltMaterial;
            _bolts.Add((bolt, 0.25f));
        }

        private void OnCrateSpawned(CellPos cell)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(crate.GetComponent<Collider>());
            float cx = cell.X + 0.5f, cz = cell.Y + 0.5f;
            crate.transform.position = new Vector3(cx, _runner.Terrain.SurfaceHeight(cx, cz) + 0.2f, cz);
            crate.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            crate.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            crate.GetComponent<MeshRenderer>().sharedMaterial = _crateMaterial;
            crate.transform.SetParent(transform, worldPositionStays: true);
            _crateViews[(cell.X, cell.Y)] = crate;
        }

        private void OnCratePicked(CellPos cell)
        {
            if (_crateViews.TryGetValue((cell.X, cell.Y), out var go))
            {
                Destroy(go);
                _crateViews.Remove((cell.X, cell.Y));
            }
        }

        private Vector3 ToWorld(LeptonPos pos)
        {
            var world = TerrainView.LeptonToWorld(pos);
            world.y = _runner.Terrain.SurfaceHeight(world.x, world.z);
            return world;
        }

        private void Update()
        {
            for (int i = _bolts.Count - 1; i >= 0; i--)
            {
                var (go, life) = _bolts[i];
                life -= Time.deltaTime;
                if (life <= 0f)
                {
                    Destroy(go);
                    _bolts.RemoveAt(i);
                }
                else
                {
                    _bolts[i] = (go, life);
                }
            }
        }
    }
}
