using Newtonsoft.Json.Linq;
using TiberiumDusk.Sim.Data;
using UnityEngine;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Drop-in external model support. Put a model file (FBX / OBJ / glTF via
    /// glTFast / .blend) at Assets/Resources/Models/&lt;blueprint id&gt; —
    /// e.g. Models/nx_harvester.fbx — and it replaces that unit's or
    /// structure's procedural body automatically:
    ///   • auto-scaled so its ground footprint matches the blueprint's size
    ///   • grounded so its lowest point sits on y=0
    ///   • a child named "turret" (any case) becomes the rotating turret
    /// Optional per-model tuning in Resources/ModelOverrides.json:
    ///   { "nx_harvester": { "scale": 1.2, "rotY": 180, "y": 0.05 } }
    /// (scale multiplies the auto-fit; rotY is a yaw correction in degrees.)
    /// </summary>
    public static class ModelLibrary
    {
        private static bool _overridesLoaded;
        private static JObject _overrides;

        private static JObject OverridesFor(string specId)
        {
            if (!_overridesLoaded)
            {
                _overridesLoaded = true;
                var text = Resources.Load<TextAsset>("ModelOverrides");
                if (text != null && !string.IsNullOrEmpty(text.text))
                {
                    try { _overrides = JObject.Parse(text.text); }
                    catch { _overrides = null; }
                }
            }
            return _overrides?[specId] as JObject;
        }

        /// <summary>
        /// Instantiate the external model for a blueprint, or null when none
        /// exists. The returned object carries the fitted body;
        /// <paramref name="turret"/> is its turret child if the model has one.
        /// </summary>
        public static GameObject TryInstantiate(UnitSpec spec, Transform parent, out Transform turret)
        {
            turret = null;
            var prefab = Resources.Load<GameObject>("Models/" + spec.Id);
            if (prefab == null) return null;

            var instance = Object.Instantiate(prefab, parent, worldPositionStays: false);
            instance.name = "model";

            // Measure combined renderer bounds (parent sits at origin,
            // identity rotation, while bodies are built).
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Object.Destroy(instance); return null; }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var tuning = OverridesFor(spec.Id);
            float userScale = tuning?["scale"] != null ? (float)tuning["scale"] : 1f;
            float rotY = tuning?["rotY"] != null ? (float)tuning["rotY"] : 0f;
            float yOffset = tuning?["y"] != null ? (float)tuning["y"] : 0f;

            // Optional "texture": force-bind a colormap onto every renderer —
            // makes single-texture packs (KayKit etc.) work regardless of how
            // the FBX importer resolved materials.
            if (tuning?["texture"] != null)
            {
                var tex = Resources.Load<Texture2D>((string)tuning["texture"]);
                if (tex != null)
                {
                    var mat = MaterialFactory.Textured(tex);
                    foreach (var r in renderers)
                    {
                        var slots = r.sharedMaterials;
                        if (slots == null || slots.Length == 0) { r.sharedMaterial = mat; continue; }
                        for (int i = 0; i < slots.Length; i++) slots[i] = mat;
                        r.sharedMaterials = slots;
                    }
                }
            }

            // Auto-fit: ground footprint → target size for the blueprint.
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint < 0.0001f) footprint = 1f;
            float scale = TargetSize(spec) / footprint * userScale;
            instance.transform.localScale = new Vector3(scale, scale, scale);
            instance.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            // Re-measure after scaling to sit the model on the ground.
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.localPosition = new Vector3(
                -bounds.center.x, -bounds.min.y + yOffset, -bounds.center.z);

            foreach (var child in instance.GetComponentsInChildren<Transform>())
            {
                if (child != instance.transform &&
                    child.name.ToLowerInvariant().Contains("turret"))
                {
                    turret = child;
                    break;
                }
            }
            return instance;
        }

        /// <summary>Desired ground footprint (world units) per blueprint kind.</summary>
        private static float TargetSize(UnitSpec spec)
        {
            if (spec.IsStructure)
                return Mathf.Max(spec.Structure.FootprintW, spec.Structure.FootprintH) * 0.95f;
            if (spec.Mobile != null && spec.Mobile.Locomotor == LocomotorId.Foot) return 0.4f;
            if (spec.IsAircraft) return 1.0f;
            if (spec.Harvester != null || spec.Id.Contains("mcv")) return 1.0f;
            return 0.85f;
        }

        /// <summary>Box collider sized to the fitted model, for mouse picking.</summary>
        public static void FitCollider(GameObject root, GameObject modelInstance)
        {
            var renderers = modelInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center - root.transform.position;
            box.size = bounds.size;
        }
    }
}
