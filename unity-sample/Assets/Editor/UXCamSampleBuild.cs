using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UXCamSample.Editor
{
    public static class UXCamSampleBuild
    {
        const string ScenePath = "Assets/Scenes/UXCamSample.unity";
        const string SampleComponent = "UXCamSDK.Samples.UXCamBasicIntegration, UXCam.Unity.Samples.BasicIntegration";
        const string BundleId = "com.uxcam.unitysample";

        [MenuItem("UXCam Sample/Create Sample Scene")]
        public static void CreateSampleScene()
        {
            var componentType = Type.GetType(SampleComponent);
            if (componentType == null)
                throw new InvalidOperationException(
                    "Import the \"Basic Integration\" sample first (Package Manager > UXCam > Samples), " +
                    "or copy com.uxcam.unity/Samples~/BasicIntegration into Assets/Samples.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("UXCam Sample", componentType);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("UXCam Sample/Build Android")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/uxcam-sample.apk");

        [MenuItem("UXCam Sample/Build iOS (Xcode project)")]
        public static void BuildIOS() => Build(BuildTarget.iOS, "Builds/iOS");

        static void Build(BuildTarget target, string path)
        {
            CreateSampleScene();

            var group = BuildPipeline.GetBuildTargetGroup(target);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(group), BundleId);
            PlayerSettings.companyName = "UXCam";
            PlayerSettings.productName = "UXCam Unity Sample";

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                targetGroup = group,
                locationPathName = path,
                options = BuildOptions.Development,
            });

            Debug.Log($"[UXCam Sample] {target} build {report.summary.result}: {path}");
        }
    }
}
