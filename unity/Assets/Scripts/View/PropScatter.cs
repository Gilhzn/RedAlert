using TiberiumDusk.Sim.Math;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Visual-only map decoration from external kits: drop any models
    /// (battlefield packs, ruins, rocks, sandbags…) into
    /// Assets/Resources/MapProps/ and they are scattered across the map —
    /// deterministically seeded, on free cells only (no crystal, no
    /// occupants, away from base starts), auto-scaled and grounded.
    /// Props never touch the simulation: no colliders, no pathing impact.
    /// </summary>
    public sealed class PropScatter : MonoBehaviour
    {
        /// <summary>Approximate fraction of map cells that get a prop.</summary>
        public const float Density = 0.012f;
        public const float MinScale = 0.5f;   // in cells
        public const float MaxScale = 1.6f;

        private void Start()
        {
            var runner = FindFirstObjectByType<GameRunner>();
            if (runner != null) runner.WhenReady(() => Scatter(runner));
        }

        private void Scatter(GameRunner runner)
        {
            var prefabs = Resources.LoadAll<GameObject>("MapProps");
            if (prefabs == null || prefabs.Length == 0) return;

            var world = runner.Game.World;
            var map = world.Map;
            int target = (int)(map.Width * map.Height * Density);
            // Deterministic placement: same map ⇒ same decoration.
            var rng = new System.Random(map.Width * 73856093 ^ map.Height * 19349663);

            int placed = 0;
            for (int attempt = 0; attempt < target * 8 && placed < target; attempt++)
            {
                var cell = new CellPos(rng.Next(map.Width), rng.Next(map.Height));
                if (!map.InBounds(cell)) continue;
                if (world.OccupantOf(cell) != 0) continue;
                if (world.Crystal.HasCrystal(cell)) continue;
                if (NearStructure(world, cell, 4)) continue;

                var prefab = prefabs[rng.Next(prefabs.Length)];
                var prop = Instantiate(prefab, transform, worldPositionStays: false);
                prop.name = "prop_" + prefab.name;

                var renderers = prop.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) { Destroy(prop); continue; }
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                float footprint = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.z), 0.0001f);
                float targetSize = Mathf.Lerp(MinScale, MaxScale, (float)rng.NextDouble());
                float scale = targetSize / footprint;
                prop.transform.localScale = new Vector3(scale, scale, scale);
                prop.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                // Re-measure to sit the prop on the terrain surface.
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                float cx = cell.X + 0.5f, cz = cell.Y + 0.5f;
                float ground = runner.Terrain != null ? runner.Terrain.SurfaceHeight(cx, cz) : 0f;
                prop.transform.position = new Vector3(
                    cx - bounds.center.x + prop.transform.position.x,
                    ground - bounds.min.y + prop.transform.position.y,
                    cz - bounds.center.z + prop.transform.position.z);

                foreach (var col in prop.GetComponentsInChildren<Collider>()) Destroy(col);
                placed++;
            }
        }

        private static bool NearStructure(TiberiumDusk.Sim.WorldModel.World world, CellPos cell, int radius)
        {
            var entities = world.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (!e.Alive || e.Spec.Structure == null) continue;
                var home = e.HomeCell;
                if (System.Math.Abs(home.X - cell.X) <= radius + e.Spec.Structure.FootprintW &&
                    System.Math.Abs(home.Y - cell.Y) <= radius + e.Spec.Structure.FootprintH)
                    return true;
            }
            return false;
        }
    }
}
