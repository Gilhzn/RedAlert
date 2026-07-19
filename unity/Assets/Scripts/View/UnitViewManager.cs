using System.Collections.Generic;
using TiberiumDusk.Sim.Data;
using TiberiumDusk.Sim.Math;
using TiberiumDusk.Sim.WorldModel;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Keeps one UnitView per living sim entity. Placeholder Phase 2 bodies:
    /// primitives colored by owner. Real models arrive in Phase 5.
    /// </summary>
    public sealed class UnitViewManager : MonoBehaviour
    {
        public static readonly Color[] PlayerColors =
        {
            new Color(0.85f, 0.64f, 0.25f),   // player 0: Dominion amber
            new Color(0.76f, 0.07f, 0.12f),   // player 1: Serpent crimson
            new Color(0.25f, 0.55f, 0.85f),
            new Color(0.45f, 0.75f, 0.35f),
        };

        private GameRunner _runner;
        private readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();
        private readonly List<int> _toRemove = new List<int>();

        public IReadOnlyDictionary<int, UnitView> Views => _views;

        private void Start()
        {
            _runner = FindFirstObjectByType<GameRunner>();
            _runner.AfterTick += SyncWithSim;
            SyncWithSim();
        }

        private void OnDestroy()
        {
            if (_runner != null) _runner.AfterTick -= SyncWithSim;
        }

        private void SyncWithSim()
        {
            var entities = _runner.Game.World.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.Alive) continue;
                if (!_views.TryGetValue(entity.Id, out var view))
                {
                    view = UnitView.Create(entity, _runner, transform);
                    _views.Add(entity.Id, view);
                }
                view.PushSimState(entity);
            }

            _toRemove.Clear();
            foreach (var pair in _views)
            {
                if (_runner.Game.World.GetEntity(pair.Key) == null) _toRemove.Add(pair.Key);
            }
            foreach (var id in _toRemove)
            {
                Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }
    }

    /// <summary>One rendered unit: interpolates between the last two sim ticks.</summary>
    public sealed class UnitView : MonoBehaviour
    {
        public int EntityId { get; private set; }
        public int Owner { get; private set; }

        private GameRunner _runner;
        private Vector3 _prevPos, _currPos;
        private Quaternion _prevRot, _currRot;
        private Quaternion _prevTurretRot, _currTurretRot;
        private Transform _turret;
        private GameObject _selectionRing;
        private bool _isAircraft;
        private bool _cloaked;
        private readonly System.Collections.Generic.List<MeshRenderer> _renderers =
            new System.Collections.Generic.List<MeshRenderer>();

        public static UnitView Create(Entity entity, GameRunner runner, Transform parent)
        {
            var root = new GameObject($"unit_{entity.Id}_{entity.Spec.Id}");
            root.transform.SetParent(parent, worldPositionStays: false);
            var view = root.AddComponent<UnitView>();
            view.EntityId = entity.Id;
            view.Owner = entity.Owner;
            view._runner = runner;
            view._isAircraft = entity.Spec.IsAircraft;
            view.BuildBody(entity);
            view.CollectRenderers();

            var start = view.ComputeWorldPos(entity.Pos);
            var rot = FacingToRotation(entity.FacingValue);
            view._prevPos = view._currPos = start;
            view._prevRot = view._currRot = rot;
            root.transform.SetPositionAndRotation(start, rot);
            return view;
        }

        private void BuildBody(Entity entity)
        {
            var color = UnitViewManager.PlayerColors[entity.Owner % UnitViewManager.PlayerColors.Length];
            bool isInfantry = entity.Spec.Mobile != null && entity.Spec.Mobile.Locomotor == LocomotorId.Foot;

            if (entity.Spec.IsStructure)
            {
                BuildStructureBody(entity, color);
                return;
            }

            GameObject body;
            if (isInfantry)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
                body.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.localScale = new Vector3(0.55f, 0.28f, 0.75f);
                body.transform.localPosition = new Vector3(0f, 0.2f, 0f);

                // Turret as a child of the ROOT (not the hull) so the sim's
                // independent turret facing can drive it directly.
                if (entity.Spec.Turreted)
                {
                    var turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    turret.transform.SetParent(transform, worldPositionStays: false);
                    turret.transform.localScale = new Vector3(0.32f, 0.14f, 0.5f);
                    turret.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                    turret.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Solid(color * 0.75f);
                    Destroy(turret.GetComponent<Collider>());
                    _turret = turret.transform;
                }
            }

            body.transform.SetParent(transform, worldPositionStays: false);
            body.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Solid(color);

            // The body collider is what mouse picking hits; tag it with our id.
            var reference = body.AddComponent<EntityRef>();
            reference.EntityId = EntityId;
            reference.Owner = Owner;

            // Selection ring (hidden until selected).
            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.transform.SetParent(transform, worldPositionStays: false);
            _selectionRing.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
            _selectionRing.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            _selectionRing.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Solid(new Color(0.2f, 1f, 0.33f)); // phosphor green
            Destroy(_selectionRing.GetComponent<Collider>());
            _selectionRing.SetActive(false);
        }

        private void BuildStructureBody(Entity entity, Color ownerColor)
        {
            var s = entity.Spec.Structure;

            // Main hull: a slab sized to the footprint, tinted by structure role.
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.transform.SetParent(transform, worldPositionStays: false);
            hull.transform.localScale = new Vector3(s.FootprintW * 0.92f, 0.55f, s.FootprintH * 0.92f);
            hull.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            hull.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Solid(new Color(0.32f, 0.34f, 0.36f));

            // Owner-colored trim block on top so allegiance reads at a glance.
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.transform.SetParent(transform, worldPositionStays: false);
            trim.transform.localScale = new Vector3(s.FootprintW * 0.5f, 0.35f, s.FootprintH * 0.5f);
            trim.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Solid(ownerColor);
            Destroy(trim.GetComponent<Collider>());

            var reference = hull.AddComponent<EntityRef>();
            reference.EntityId = EntityId;
            reference.Owner = Owner;

            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.transform.SetParent(transform, worldPositionStays: false);
            float ringSize = Mathf.Max(s.FootprintW, s.FootprintH) * 1.05f;
            _selectionRing.transform.localScale = new Vector3(ringSize, 0.01f, ringSize);
            _selectionRing.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            _selectionRing.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Solid(new Color(0.2f, 1f, 0.33f));
            Destroy(_selectionRing.GetComponent<Collider>());
            _selectionRing.SetActive(false);
        }

        public void SetSelected(bool selected) => _selectionRing.SetActive(selected);

        public void PushSimState(Entity entity)
        {
            ApplyVisibility(entity);
            _prevPos = _currPos;
            _prevRot = _currRot;
            _currPos = ComputeWorldPos(entity.Pos);
            _currRot = FacingToRotation(entity.FacingValue);
            if (_turret != null)
            {
                _prevTurretRot = _currTurretRot;
                _currTurretRot = FacingToRotation(entity.TurretFacing);
            }
        }

        private Vector3 ComputeWorldPos(LeptonPos pos)
        {
            var world = TerrainView.LeptonToWorld(pos);
            world.y = _runner.Terrain.SurfaceHeight(world.x, world.z);
            if (_isAircraft) world.y += 2.4f;   // flight altitude (visual only)
            return world;
        }

        private void CollectRenderers()
        {
            _renderers.Clear();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                if (_selectionRing == null || renderer.gameObject != _selectionRing)
                    _renderers.Add(renderer);
            }
        }

        /// <summary>Cloak + fog presentation: enemies vanish when cloaked or unseen.</summary>
        private void ApplyVisibility(Entity entity)
        {
            bool enemy = Owner != GameRunner.LocalPlayerId;
            bool hiddenByFog = enemy
                && !_runner.Game.Vision.IsVisible(GameRunner.LocalPlayerId, entity.HomeCell);
            bool cloakHidden = entity.IsCloaked && enemy;
            bool hidden = hiddenByFog || cloakHidden;

            if (_cloaked != hidden)
            {
                _cloaked = hidden;
                foreach (var renderer in _renderers)
                {
                    renderer.enabled = !hidden;
                }
            }
            float scale = entity.IsCloaked && !enemy ? 0.8f : 1f;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private static Quaternion FacingToRotation(byte facing)
        {
            // Sim facing: 0 = +X, increasing toward +Y(=world +Z), 256 steps.
            float radians = facing * (2f * Mathf.PI / 256f);
            var direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
            return Quaternion.LookRotation(direction, Vector3.up);
        }

        private void Update()
        {
            float alpha = _runner.Alpha;
            transform.SetPositionAndRotation(
                Vector3.Lerp(_prevPos, _currPos, alpha),
                Quaternion.Slerp(_prevRot, _currRot, alpha));
            if (_turret != null)
            {
                // World-space turret facing, independent of hull rotation.
                _turret.rotation = Quaternion.Slerp(_prevTurretRot, _currTurretRot, alpha);
            }
        }
    }
}
