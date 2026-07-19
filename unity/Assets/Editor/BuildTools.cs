using System.IO;
using TiberiumDusk.Balance;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TiberiumDusk.Editor
{
    /// <summary>
    /// Build pipeline: copies repo data/ into StreamingAssets before every
    /// build (players can't read outside their sandbox) and offers one-click
    /// WebGL / desktop builds with sane settings.
    /// </summary>
    public sealed class BuildTools : IPreprocessBuildWithReport
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => CopyDataToStreamingAssets();

        [MenuItem("Tiberium Dusk/Copy data to StreamingAssets")]
        public static void CopyDataToStreamingAssets()
        {
            string repoData = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
            string target = Path.Combine(Application.dataPath, "StreamingAssets", "data");
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);

            foreach (var name in GameDataLoader.DataFiles)
            {
                string source = Path.Combine(repoData, name.Replace('/', Path.DirectorySeparatorChar));
                string destination = Path.Combine(target, name.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination);
            }
            AssetDatabase.Refresh();
            Debug.Log("Tiberium Dusk: data copied to StreamingAssets.");
        }

        [MenuItem("Tiberium Dusk/Build/WebGL")]
        public static void BuildWebGL()
        {
            CopyDataToStreamingAssets();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            Build(BuildTarget.WebGL, "builds/webgl");
        }

        [MenuItem("Tiberium Dusk/Build/Windows")]
        public static void BuildWindows()
        {
            CopyDataToStreamingAssets();
            Build(BuildTarget.StandaloneWindows64, "builds/windows/TiberiumDusk.exe");
        }

        [MenuItem("Tiberium Dusk/Build/Linux")]
        public static void BuildLinux()
        {
            CopyDataToStreamingAssets();
            Build(BuildTarget.StandaloneLinux64, "builds/linux/TiberiumDusk");
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Tiberium Dusk build: {report.summary.result} → {outputPath} " +
                      $"({report.summary.totalSize / (1024 * 1024)} MB)");
        }
    }
}
