using System.IO;
using TiberiumDusk.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TiberiumDusk.Editor
{
    /// <summary>
    /// One-click project bootstrap: creates the main scene (game runner,
    /// camera, lights) and optionally switches the project to URP + linear
    /// color space. Everything in the scene is created procedurally at
    /// runtime, so the scene itself stays minimal.
    /// </summary>
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tiberium Dusk/Setup Project (scene + URP)")]
        public static void SetupEverything()
        {
            TrySetupUrp();
            CreateMainScene();
            EditorUtility.DisplayDialog("Tiberium Dusk",
                "Setup complete.\n\nScene: Assets/Scenes/Main.unity\nPress Play to run the Phase 2 sandbox.", "OK");
        }

        [MenuItem("Tiberium Dusk/Create Main Scene Only")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var game = new GameObject("Game");
            game.AddComponent<GameRunner>();
            game.AddComponent<UnitViewManager>();
            game.AddComponent<SelectionController>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CameraRig>();

            // Cold key light + subtle warm fill, per the style guide.
            var sun = new GameObject("Sun");
            var sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(0.75f, 0.82f, 0.95f);
            sunLight.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(55f, 210f, 0f);

            var fill = new GameObject("Fill");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.9f, 0.75f, 0.55f);
            fillLight.intensity = 0.25f;
            fill.transform.rotation = Quaternion.Euler(30f, 40f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.19f, 0.23f);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Tiberium Dusk: main scene created at " + ScenePath);
        }

        [MenuItem("Tiberium Dusk/Switch Project to URP")]
        public static void TrySetupUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                Debug.Log("Tiberium Dusk: a render pipeline asset is already assigned, skipping URP setup.");
                return;
            }

            try
            {
                Directory.CreateDirectory("Assets/Settings");

                var rendererData = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, "Assets/Settings/URP-Renderer.asset");

                var pipeline = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, "Assets/Settings/URP-Pipeline.asset");

                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                PlayerSettings.colorSpace = ColorSpace.Linear;
                AssetDatabase.SaveAssets();
                Debug.Log("Tiberium Dusk: URP assigned (linear color space).");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    "Tiberium Dusk: automatic URP setup failed — the sandbox still runs on the built-in pipeline.\n" +
                    "Manual path: Assets > Create > Rendering > URP Asset (with Universal Renderer), then assign it in " +
                    "Project Settings > Graphics. Error: " + e.Message);
            }
        }
    }
}
