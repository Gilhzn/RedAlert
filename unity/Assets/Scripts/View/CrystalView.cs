using System.Collections.Generic;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Renders the crystal overlay: one emissive shard cluster per non-empty
    /// cell, scaled by density. Cheap dirty-scan sync every few ticks.
    /// </summary>
    public sealed class CrystalView : MonoBehaviour
    {
        private const int SyncEveryTicks = 5;

        private GameRunner _runner;
        private Material _greenMaterial;
        private Material _blueMaterial;
        private readonly Dictionary<int, (GameObject go, int density)> _cells =
            new Dictionary<int, (GameObject, int)>();
        private int _tickCounter;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _greenMaterial = MaterialFactory.Emissive(new Color(0.12f, 0.55f, 0.25f), new Color(0.22f, 1f, 0.42f));
            _blueMaterial = MaterialFactory.Emissive(new Color(0.15f, 0.35f, 0.60f), new Color(0.30f, 0.78f, 1f));
            _runner.AfterTick += OnTick;
            SyncAll();
        }

        private void OnDestroy()
        {
            if (_runner != null) _runner.AfterTick -= OnTick;
        }

        private void OnTick()
        {
            if (++_tickCounter % SyncEveryTicks == 0) SyncAll();
        }

        private void SyncAll()
        {
            var world = _runner.Game.World;
            var map = world.Map;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = new CellPos(x, y);
                    int index = map.CellIndex(cell);
                    int density = world.Crystal.Density(cell);
                    // Shroud hides undiscovered fields (the shards poke above the fog mesh).
                    if (!_runner.Game.Vision.IsExplored(GameRunner.LocalPlayerId, cell)) density = 0;
                    bool tracked = _cells.TryGetValue(index, out var entry);

                    if (density == 0)
                    {
                        if (tracked)
                        {
                            Destroy(entry.go);
                            _cells.Remove(index);
                        }
                        continue;
                    }

                    if (!tracked)
                    {
                        entry = (CreateShards(cell, world.Crystal.TypeAt(cell)), -1);
                        _cells[index] = entry;
                    }
                    if (entry.density != density)
                    {
                        float scale = 0.25f + 0.75f * density / 11f;
                        entry.go.transform.localScale = new Vector3(scale, scale, scale);
                        _cells[index] = (entry.go, density);
                    }
                }
            }
        }

        private GameObject CreateShards(CellPos cell, CrystalType type)
        {
            var cluster = new GameObject($"crystal_{cell.X}_{cell.Y}");
            cluster.transform.SetParent(transform, worldPositionStays: false);
            var material = type == CrystalType.Blue ? _blueMaterial : _greenMaterial;

            // Three tilted shards per cell — reads as a crystal patch from RTS zoom.
            for (int i = 0; i < 3; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.transform.SetParent(cluster.transform, worldPositionStays: false);
                float ox = (i - 1) * 0.22f;
                float oz = (i % 2 == 0) ? 0.15f : -0.12f;
                shard.transform.localPosition = new Vector3(ox, 0.18f, oz);
                shard.transform.localScale = new Vector3(0.16f, 0.45f, 0.16f);
                shard.transform.localRotation = Quaternion.Euler(i * 9f - 8f, i * 47f, 12f - i * 11f);
                shard.GetComponent<MeshRenderer>().sharedMaterial = material;
                Destroy(shard.GetComponent<Collider>());
            }

            float cx = cell.X + 0.5f;
            float cz = cell.Y + 0.5f;
            cluster.transform.position = new Vector3(cx, _runner.Terrain.SurfaceHeight(cx, cz), cz);
            return cluster;
        }
    }
}
