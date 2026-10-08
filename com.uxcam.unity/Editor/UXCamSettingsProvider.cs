using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UXCamSDK.Editor
{
    internal static class UXCamSettingsProvider
    {
        internal const string SettingsPath = "Project/UXCam";
        internal const string DefaultAssetPath = "Assets/Resources/" + UXCamSettings.ResourcesPath + ".asset";

        static SerializedObject s_SerializedSettings;

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "UXCam",
                guiHandler = _ => OnGUI(),
                keywords = new HashSet<string>
                {
                    "UXCam", "App Key", "Session Recording", "Screen Tagging", "Occlusion", "Crash", "Analytics",
                },
            };
        }

        [MenuItem("Tools/UXCam/Settings", priority = 0)]
        static void OpenSettings() => SettingsService.OpenProjectSettings(SettingsPath);

        [MenuItem("Tools/UXCam/Documentation", priority = 20)]
        static void OpenDocumentation() => Application.OpenURL("https://developer.uxcam.com");

        internal static UXCamSettings GetOrCreateSettings()
        {
            var settings = UXCamSettings.Load();
            if (settings != null) return settings;

            Directory.CreateDirectory(Path.GetDirectoryName(DefaultAssetPath));
            settings = ScriptableObject.CreateInstance<UXCamSettings>();
            AssetDatabase.CreateAsset(settings, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        static void OnGUI()
        {
            var settings = UXCamSettings.Load();
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "UXCam settings let the plugin start automatically with your app key. Without them, call " +
                    "UXCam.StartWithConfiguration from your own code.", MessageType.Info);
                if (GUILayout.Button("Create UXCam Settings", GUILayout.Width(200)))
                    GetOrCreateSettings();
                return;
            }

            if (s_SerializedSettings == null || s_SerializedSettings.targetObject != settings)
                s_SerializedSettings = new SerializedObject(settings);

            s_SerializedSettings.Update();
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 260f;

            Section("App key");
            Field("m_AppKey", "App Key");
            Field("m_IOSAppKey", "iOS App Key (optional override)");
            Field("m_AndroidAppKey", "Android App Key (optional override)");
            if (string.IsNullOrWhiteSpace(settings.AppKey) &&
                (string.IsNullOrWhiteSpace(settings.IOSAppKey) || string.IsNullOrWhiteSpace(settings.AndroidAppKey)))
            {
                EditorGUILayout.HelpBox("Add your app key from the UXCam dashboard (Settings > App Key).", MessageType.Warning);
            }

            Section("Startup");
            Field("m_StartAutomatically", "Start Automatically");
            Field("m_OptIntoVideoRecording", "Opt Into Video Recording");
            if (!settings.OptIntoVideoRecording)
            {
                EditorGUILayout.HelpBox(
                    "iOS records no screen video until the device is opted in. Turn this on, or call " +
                    "UXCam.OptIntoVideoRecording() once the user consents. Android records video by default.",
                    MessageType.Warning);
            }
            Field("m_TagScenesAutomatically", "Tag Scenes as Screens");
            Field("m_CaptureUnhandledExceptions", "Report Unhandled C# Exceptions");
            if (!settings.StartAutomatically)
            {
                EditorGUILayout.HelpBox(
                    "Start UXCam from code, for example after the user consents:\n" +
                    "UXCam.StartWithConfiguration(UXCamSettings.Load().ToConfiguration(Application.platform));",
                    MessageType.None);
            }

            Section("Native SDK");
            Field("m_EnableMultiSessionRecord", "Multi-Session Record");
            Field("m_EnableCrashHandling", "Crash Handling");
            Field("m_EnableIntegrationLogging", "Integration Logging");
            Field("m_EnableImprovedScreenCapture", "Improved Screen Capture (iOS)");
            Field("m_EnableAdvancedGestureRecognition", "Advanced Gesture Recognition (iOS)");
            Field("m_EnableNetworkLogging", "Network Logging (iOS)");

            Section("Editor");
            Field("m_LogCallsInEditor", "Log Calls in the Editor");
            Field("m_PreviewOcclusionInEditor", "Preview Occlusion in Game View");

            EditorGUIUtility.labelWidth = previousLabelWidth;
            s_SerializedSettings.ApplyModifiedProperties();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                $"UXCam Unity plugin {UXCam.PluginVersion}. Native SDKs are resolved by the External Dependency Manager " +
                "from Editor/UXCamDependencies.xml.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Settings asset: " + AssetDatabase.GetAssetPath(settings), EditorStyles.miniLabel);
        }

        static void Section(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        static void Field(string propertyName, string label)
        {
            var property = s_SerializedSettings.FindProperty(propertyName);
            if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip));
        }
    }
}
